using Microsoft.EntityFrameworkCore;
using DietitianClinic.DataAccess.Context;
using DietitianClinic.API.Extensions;
using DietitianClinic.API.Middleware;
using DietitianClinic.API.Hubs;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Linq;
using DietitianClinic.Entity.Models;
using Serilog;
using Serilog.Sinks.Elasticsearch;
using Serilog.Debugging;
using Sentry;

// ── Serilog iç hatalarını stderr'e yönlendir ──
SelfLog.Enable(TextWriter.Synchronized(Console.Error));

// ── Bootstrap logger: Uygulama başlamadan önceki hataları yakala ───────
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    // ── Sentry Hata Takibi ─────────────────────────────────────────────
    builder.WebHost.UseSentry(o =>
    {
        o.Dsn = builder.Configuration["Sentry:Dsn"] ?? string.Empty;
        o.TracesSampleRate = 1.0;
        o.SendDefaultPii = false;
        o.Environment = builder.Environment.EnvironmentName;
        o.Release = "fitrehber@1.0.0";
        // DSN boşsa Sentry devre dışı kalır, uygulama etkilenmez
    });

    // ── Serilog Yapılandırması ──────────────────────────────────────────
    builder.Host.UseSerilog((context, services, config) =>
    {
        var elasticUrl = context.Configuration["Elasticsearch:Url"] ?? "http://localhost:9200";
        var indexFormat = context.Configuration["Elasticsearch:IndexFormat"] ?? "fitrehber-logs-{0:yyyy.MM.dd}";
        var appName = context.Configuration["ApiSettings:ApiName"] ?? "FitRehber";
        var env = context.HostingEnvironment.EnvironmentName;

        config
            .ReadFrom.Configuration(context.Configuration)
            .ReadFrom.Services(services)
            .Enrich.FromLogContext()
            .Enrich.WithMachineName()
            .Enrich.WithProcessId()
            .Enrich.WithThreadId()
            .Enrich.WithProperty("Application", appName)
            .Enrich.WithProperty("Environment", env)
            .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {SourceContext}: {Message:lj}{NewLine}{Exception}")
            .WriteTo.File(path: "logs/app-.log", rollingInterval: RollingInterval.Day, retainedFileCountLimit: 7)
            .WriteTo.Elasticsearch(new ElasticsearchSinkOptions(new Uri(elasticUrl))
            {
                AutoRegisterTemplate = true,
                AutoRegisterTemplateVersion = AutoRegisterTemplateVersion.ESv7,
                IndexFormat = indexFormat,
                NumberOfReplicas = 0,
                NumberOfShards = 1,
                EmitEventFailure = EmitEventFailureHandling.WriteToSelfLog | EmitEventFailureHandling.RaiseCallback,
                FailureCallback = (logEvent, ex) => Console.Error.WriteLine($"[Serilog-ES] Log gönderilemedi: {ex?.Message}")
            });
    });

    // ── Servis Kayıtları ────────────────────────────────────────────────
    builder.Services.AddDbContext<DietitianClinicDbContext>(options =>
    {
        var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
        options.UseSqlServer(connectionString, sqlServerOptions =>
        {
            sqlServerOptions.MigrationsAssembly("DietitianClinic.DataAccess");
            sqlServerOptions.CommandTimeout(30);
            sqlServerOptions.EnableRetryOnFailure(maxRetryCount: 5);
        });
    });

    builder.Services.AddControllers();
    var loginPermitLimit  = builder.Configuration.GetValue<int>("RateLimiting:LoginPermitLimit",  30);
    var loginWindowMinutes = builder.Configuration.GetValue<int>("RateLimiting:LoginWindowMinutes", 5);
    builder.Services.AddRateLimiter(options =>
    {
        options.AddPolicy("login", context =>
            System.Threading.RateLimiting.RateLimitPartition.GetFixedWindowLimiter(
                partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                factory: _ => new System.Threading.RateLimiting.FixedWindowRateLimiterOptions
                {
                    PermitLimit = loginPermitLimit,
                    Window = TimeSpan.FromMinutes(loginWindowMinutes),
                    QueueProcessingOrder = System.Threading.RateLimiting.QueueProcessingOrder.OldestFirst,
                    QueueLimit = 0
                }));
        options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    });

    builder.Services.AddJwtAuthentication(builder.Configuration);
    builder.Services.AddBusinessServices();
    builder.Services.AddCorsPolicy();
    builder.Services.AddSwaggerConfiguration();
    builder.Services.AddSignalR();
    builder.Services.AddHttpClient();
    builder.Services.AddHostedService<DietitianClinic.API.Services.AppointmentReminderService>();
    builder.Services.AddRedisCache(builder.Configuration);

    var app = builder.Build();

    // ── Veritabanı ve Seed İşlemleri ────────────────────────────────────
    using (var scope = app.Services.CreateScope())
    {
        try
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<DietitianClinicDbContext>();
            dbContext.Database.ExecuteSqlRaw(@"
                IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[Messages]') AND type in (N'U'))
                BEGIN
                    CREATE TABLE [dbo].[Messages](
                        [Id] [int] IDENTITY(1,1) NOT NULL,
                        [SenderId] [int] NOT NULL,
                        [ReceiverId] [int] NOT NULL,
                        [Content] [nvarchar](2000) NOT NULL,
                        [IsRead] [bit] NOT NULL DEFAULT(0),
                        [AttachmentUrl] [nvarchar](500) NULL,
                        [AttachmentName] [nvarchar](255) NULL,
                        [AttachmentType] [nvarchar](20) NULL,
                        [CreatedDate] [datetime2](7) NOT NULL,
                        [ModifiedDate] [datetime2](7) NULL,
                        [DeletedDate] [datetime2](7) NULL,
                        [IsDeleted] [bit] NOT NULL DEFAULT(0),
                      CONSTRAINT [PK_Messages] PRIMARY KEY CLUSTERED ([Id] ASC)
                    )
                END
                IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Messages_SenderId_ReceiverId' AND object_id = OBJECT_ID('Messages'))
                BEGIN
                    CREATE INDEX IX_Messages_SenderId_ReceiverId ON Messages(SenderId, ReceiverId);
                END
            ");

            dbContext.Database.ExecuteSqlRaw(@"
                IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('Patients') AND name = 'EmailNotificationsEnabled')
                    ALTER TABLE Patients ADD EmailNotificationsEnabled bit NOT NULL DEFAULT 1;
            ");

            dbContext.Database.ExecuteSqlRaw(@"
                IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[DailyTrackings]') AND type = N'U')
                BEGIN
                    CREATE TABLE [dbo].[DailyTrackings](
                        [Id] [int] IDENTITY(1,1) NOT NULL,
                        [PatientId] [int] NOT NULL,
                        [TrackingDate] [date] NOT NULL,
                        [WaterLiters] [float] NULL,
                        [StepsCount] [int] NULL,
                        [CreatedDate] [datetime2](7) NOT NULL CONSTRAINT [DF_DailyTrackings_CreatedDate] DEFAULT (GETUTCDATE()),
                        CONSTRAINT [PK_DailyTrackings] PRIMARY KEY CLUSTERED ([Id] ASC),
                        CONSTRAINT [UQ_DailyTrackings_Patient_Date] UNIQUE ([PatientId], [TrackingDate])
                    )
                END
            ");

            dbContext.Database.Migrate();

            var userService = scope.ServiceProvider.GetRequiredService<DietitianClinic.Business.Services.UserService>();
            var patientService = scope.ServiceProvider.GetRequiredService<DietitianClinic.Business.Services.PatientService>();

            if (!dbContext.Users.Any(u => u.Role == DietitianClinic.Entity.Models.UserRole.Admin))
            {
                userService.RegisterUserAsync("Admin", "Kullanici", "admin@fitrehber.com", "Admin123!", "05550000001", string.Empty, string.Empty, DietitianClinic.Entity.Models.UserRole.Admin.ToString()).GetAwaiter().GetResult();
            }

            if (!dbContext.Users.Any(u => u.Role == DietitianClinic.Entity.Models.UserRole.Dietitian))
            {
                userService.RegisterUserAsync("Demo", "Diyetisyen", "diyetisyen@fitrehber.com", "Dietitian@123", "05550000002", string.Empty, string.Empty, DietitianClinic.Entity.Models.UserRole.Dietitian.ToString()).GetAwaiter().GetResult();
            }

            if (!dbContext.Users.Any(u => u.Role == DietitianClinic.Entity.Models.UserRole.Patient))
            {
                var existingUser = dbContext.Users.FirstOrDefault(u => u.Email == "patient@example.com");
                if (existingUser == null)
                {
                    var patientUserId = userService.RegisterUserAsync("Demo", "Hasta", "patient@example.com", "Patient123!", "05550000000", string.Empty, string.Empty, DietitianClinic.Entity.Models.UserRole.Patient.ToString()).GetAwaiter().GetResult();
                    patientService.CreatePatientAsync(new DietitianClinic.Entity.Models.Patient
                    {
                        FirstName = "Demo", LastName = "Hasta", Email = "patient@example.com", Phone = "05550000000", BirthDate = new DateTime(1990, 1, 1), Gender = DietitianClinic.Entity.Models.Gender.Other, Address = "Demo adres", City = "Demo sehir"
                    }, patientUserId).GetAwaiter().GetResult();
                }
            }
        }
        catch (Exception ex)
        {
            app.Logger.LogWarning(ex, "Veritabani baslangic adimlari atlandi.");
        }
    }

    // ── Middleware'ler ──────────────────────────────────────────────────
    // CORS en başta olmalı — exception handler'dan önce register edilmezse
    // hata response'larına CORS başlıkları eklenemez.
    app.UseCors("AllowSpecificOrigin");
    app.UseSerilogRequestLogging(opts => { opts.MessageTemplate = "HTTP {RequestMethod} {RequestPath} → {StatusCode} ({Elapsed:0.0}ms)"; });
    app.UseMiddleware<ExceptionHandlingMiddleware>();
    app.UseStaticFiles();

    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI(options => { options.SwaggerEndpoint("/swagger/v1/swagger.json", "Dietitian Clinic API v1"); });
    }

    app.UseRateLimiter();
    app.UseAuthentication();
    app.UseAuthorization();

    // ── Endpoint'ler ────────────────────────────────────────────────────
    app.MapGet("/api/dashboard/summary", async (ClaimsPrincipal user, DietitianClinicDbContext dbContext) => {
        var currentUserIdRaw = user.FindFirst(JwtRegisteredClaimNames.Sub)?.Value ?? user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var isDietitian = user.IsInRole(UserRole.Dietitian.ToString()) || user.IsInRole("1");
        var currentUserId = int.TryParse(currentUserIdRaw, out var parsedUserId) ? parsedUserId : (int?)null;
        
        var today = DateTime.Today;
        var tomorrow = today.AddDays(1);
        var now = DateTime.Now;

        IQueryable<Patient> patientsQuery = dbContext.Patients.Where(p => !p.IsDeleted);
        IQueryable<Appointment> appointmentsQuery = dbContext.Appointments.Include(a => a.Patient).Where(a => !a.IsDeleted);
        IQueryable<MealPlan> mealPlansQuery = dbContext.MealPlans.Where(m => !m.IsDeleted);

        if (isDietitian && currentUserId.HasValue)
        {
            patientsQuery = patientsQuery.Where(p => p.UserId == currentUserId.Value);
            appointmentsQuery = appointmentsQuery.Where(a => a.UserId == currentUserId.Value);
            mealPlansQuery = mealPlansQuery.Where(m => m.UserId == currentUserId.Value);
        }

        var summary = new
        {
            totalDietitians = isDietitian ? 0 : await dbContext.Users.CountAsync(u => !u.IsDeleted && u.Role == UserRole.Dietitian),
            totalPatients = await patientsQuery.CountAsync(),
            totalAppointments = await appointmentsQuery.CountAsync(),
            todayAppointments = await appointmentsQuery.CountAsync(a => a.AppointmentDate >= today && a.AppointmentDate < tomorrow),
            upcomingAppointments = await appointmentsQuery.CountAsync(a => a.AppointmentDate >= now),
            pastAppointments = await appointmentsQuery.CountAsync(a => a.AppointmentDate < now),
            activeMealPlans = await mealPlansQuery.CountAsync(m => m.Status == MealPlanStatus.Active),
            todaySchedule = await appointmentsQuery.Where(a => a.AppointmentDate >= today && a.AppointmentDate < tomorrow).OrderBy(a => a.AppointmentDate).Select(a => new { a.Id, patientName = a.Patient.FirstName + " " + a.Patient.LastName, a.AppointmentDate, a.DurationInMinutes, status = (int)a.Status }).ToListAsync()
        };
        return Results.Ok(summary);
    }).RequireAuthorization();

    app.MapHub<ChatHub>("/hubs/chat");
    app.MapControllers();

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Uygulama baslatilirken kritik hata!");
}
finally
{
    Log.CloseAndFlush();
}