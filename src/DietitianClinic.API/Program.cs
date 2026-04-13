using Microsoft.EntityFrameworkCore;
using DietitianClinic.DataAccess.Context;
using DietitianClinic.API.Extensions;
using DietitianClinic.API.Middleware;
using DietitianClinic.API.Hubs;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Linq;
using DietitianClinic.Entity.Models;

var builder = WebApplication.CreateBuilder(args);


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

builder.Services.AddRateLimiter(options =>
{
    options.AddPolicy("login", context =>
        System.Threading.RateLimiting.RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new System.Threading.RateLimiting.FixedWindowRateLimiterOptions
            {
                PermitLimit = 10,
                Window = TimeSpan.FromMinutes(5),
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

builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    try
    {
        var dbContext = scope.ServiceProvider.GetRequiredService<DietitianClinicDbContext>();

        try {
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
        } catch { }

        try {
            dbContext.Database.ExecuteSqlRaw(@"
                IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('Patients') AND name = 'EmailNotificationsEnabled')
                    ALTER TABLE Patients ADD EmailNotificationsEnabled bit NOT NULL DEFAULT 1;
            ");
        } catch { }

        try {
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
        } catch { }

        dbContext.Database.Migrate();

        var userService = scope.ServiceProvider.GetRequiredService<DietitianClinic.Business.Services.UserService>();
        var patientService = scope.ServiceProvider.GetRequiredService<DietitianClinic.Business.Services.PatientService>();

        if (!dbContext.Users.Any(u => u.Role == DietitianClinic.Entity.Models.UserRole.Admin))
        {
            try
            {
                userService.RegisterUserAsync(
                    "Admin", "Kullanici", "admin@fitrehber.com", "Admin123!", "05550000001",
                    string.Empty, string.Empty, DietitianClinic.Entity.Models.UserRole.Admin.ToString()).GetAwaiter().GetResult();
            }
            catch { }
        }

        if (!dbContext.Users.Any(u => u.Role == DietitianClinic.Entity.Models.UserRole.Patient))
        {
            try
            {
                var existingUser = dbContext.Users.FirstOrDefault(u => u.Email == "patient@example.com");
                if (existingUser == null)
                {
                    var patientUserId = userService.RegisterUserAsync(
                        "Demo", "Hasta", "patient@example.com", "Patient123!", "05550000000", string.Empty, string.Empty, DietitianClinic.Entity.Models.UserRole.Patient.ToString()).GetAwaiter().GetResult();

                    patientService.CreatePatientAsync(new DietitianClinic.Entity.Models.Patient
                    {
                        FirstName = "Demo",
                        LastName = "Hasta",
                        Email = "patient@example.com",
                        Phone = "05550000000",
                        BirthDate = new DateTime(1990, 1, 1),
                        Gender = DietitianClinic.Entity.Models.Gender.Other,
                        Address = "Demo adres",
                        City = "Demo sehir",
                        MedicalHistory = "",
                        Allergies = "",
                        Notes = ""
                    }, patientUserId).GetAwaiter().GetResult();
                }
            }
            catch
            {
            }
        }
    }
    catch (Exception ex)
    {
        app.Logger.LogWarning(ex, "Veritabani baslangic adimlari atlandi. Uygulama veritabani olmadan aciliyor.");
        try { System.IO.File.WriteAllText("migration-error.txt", ex.ToString()); } catch {}
    }
}


app.UseMiddleware<ExceptionHandlingMiddleware>();

app.UseCors("AllowSpecificOrigin");

app.UseStaticFiles();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "Dietitian Clinic API v1");
        options.RoutePrefix = "swagger";
    });
}

app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/api/dashboard/summary", async (ClaimsPrincipal user, DietitianClinicDbContext dbContext) =>
{
    var currentUserIdRaw = user.FindFirst(JwtRegisteredClaimNames.Sub)?.Value
        ?? user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    var isDietitian = user.IsInRole(UserRole.Dietitian.ToString()) || user.IsInRole("1");
    var currentUserId = int.TryParse(currentUserIdRaw, out var parsedUserId) ? parsedUserId : (int?)null;

    var today = DateTime.Today;
    var tomorrow = today.AddDays(1);
    var now = DateTime.Now;

    IQueryable<Patient> patientsQuery = dbContext.Patients.Where(p => !p.IsDeleted);
    IQueryable<Appointment> appointmentsQuery = dbContext.Appointments
        .Include(a => a.Patient)
        .Where(a => !a.IsDeleted);
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
        todaySchedule = await appointmentsQuery
            .Where(a => a.AppointmentDate >= today && a.AppointmentDate < tomorrow)
            .OrderBy(a => a.AppointmentDate)
            .Select(a => new
            {
                a.Id,
                patientName = a.Patient.FirstName + " " + a.Patient.LastName,
                a.AppointmentDate,
                a.DurationInMinutes,
                status = (int)a.Status
            })
            .ToListAsync()
    };

    return Results.Ok(summary);
}).RequireAuthorization();

app.MapHub<ChatHub>("/hubs/chat");

app.MapControllers();

app.Run();
