using Microsoft.Extensions.DependencyInjection;
using DietitianClinic.Business.Interfaces;
using DietitianClinic.DataAccess.Repositories;
using DietitianClinic.API.Services;

namespace DietitianClinic.API.Extensions
{
    /// <summary>
    /// Dependency Injection extension'ları
    /// </summary>
    public static class ServiceExtensions
    {
        /// <summary>
        /// Business layer services'i DI container'a ekle
        /// </summary>
        public static IServiceCollection AddBusinessServices(this IServiceCollection services)
        {
            // Unit of Work
            services.AddScoped<DietitianClinic.DataAccess.Repositories.IUnitOfWork, DietitianClinic.DataAccess.Repositories.UnitOfWork>();

            // Services
            services.AddScoped<DietitianClinic.Business.Services.UserService>();
            services.AddScoped<DietitianClinic.Business.Services.PatientService>();
            services.AddScoped<DietitianClinic.Business.Interfaces.ITokenService, DietitianClinic.Business.Services.TokenService>();
            services.AddScoped<DietitianClinic.Business.Interfaces.IPasswordService, DietitianClinic.Business.Services.PasswordService>();

            // Şifre sıfırlama: kod/token durumu uygulama boyunca paylaşılmalı → Singleton
            services.AddSingleton<PasswordResetService>();
            // E-posta gönderici
            services.AddScoped<EmailService>();

            return services;
        }

        /// <summary>
        /// CORS ayarlarını configure et
        /// </summary>
        public static IServiceCollection AddCorsPolicy(this IServiceCollection services)
        {
            services.AddCors(options =>
            {
                options.AddPolicy("AllowSpecificOrigin", builder =>
                {
                    builder
                        .AllowAnyOrigin()
                        .AllowAnyMethod()
                        .AllowAnyHeader();
                    // Production'da bunu değiştir
                    // builder
                    //     .WithOrigins("https://yourapp.com")
                    //     .AllowAnyMethod()
                    //     .AllowAnyHeader();
                });
            });

            return services;
        }

        /// <summary>
        /// JWT Authentication'ı configure et
        /// </summary>
        public static IServiceCollection AddJwtAuthentication(this IServiceCollection services, IConfiguration configuration)
        {
            var jwtSettings = configuration.GetSection("Jwt");

            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new Microsoft.IdentityModel.Tokens.TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = jwtSettings["Issuer"],
                    ValidAudience = jwtSettings["Audience"],
                    IssuerSigningKey = new Microsoft.IdentityModel.Tokens.SymmetricSecurityKey(
                        System.Text.Encoding.UTF8.GetBytes(jwtSettings["SecretKey"]))
                };

                options.Events = new Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerEvents
                {
                    OnMessageReceived = context =>
                    {
                        var accessToken = context.Request.Query["access_token"];
                        var path = context.HttpContext.Request.Path;
                        if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/hubs"))
                        {
                            context.Token = accessToken;
                        }
                        return Task.CompletedTask;
                    }
                };
            });

            return services;
        }

        /// <summary>
        /// Swagger/OpenAPI ayarlarını configure et
        /// </summary>
        public static IServiceCollection AddSwaggerConfiguration(this IServiceCollection services)
        {
            services.AddSwaggerGen(options =>
            {
                options.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
                {
                    Title = "Dietitian Clinic Automation API",
                    Version = "v1.0",
                    Description = "ASP.NET Core Web API ile geliştirilmiş, profesyonel Diyetisyen Kliniği Otomasyon sistemi.",
                    Contact = new Microsoft.OpenApi.Models.OpenApiContact
                    {
                        Name = "API Support",
                        Email = "support@dietitianclinic.com"
                    }
                });

                // JWT Bearer token desteği
                var securityScheme = new Microsoft.OpenApi.Models.OpenApiSecurityScheme
                {
                    Name = "JWT Authentication",
                    Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
                    Scheme = "bearer",
                    BearerFormat = "JWT",
                    In = Microsoft.OpenApi.Models.ParameterLocation.Header,
                    Description = "JWT Authorization header'ında Bearer token yazın."
                };

                options.AddSecurityDefinition("Bearer", securityScheme);

                var securityRequirement = new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
                {
                    {
                        new Microsoft.OpenApi.Models.OpenApiSecurityScheme
                        {
                            Reference = new Microsoft.OpenApi.Models.OpenApiReference
                            {
                                Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                                Id = "Bearer"
                            }
                        },
                        new string[] { }
                    }
                };

                options.AddSecurityRequirement(securityRequirement);

                // XML yorum desteği
                var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
                var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
                if (File.Exists(xmlPath))
                {
                    options.IncludeXmlComments(xmlPath);
                }
            });

            return services;
        }
    }
}
