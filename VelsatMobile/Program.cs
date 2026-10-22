using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using MySql.Data.MySqlClient;
using VelsatBackendAPI.Data;
using VelsatBackendAPI.Data.Repositories;
using VelsatMobile.Data.Repositories;
using VelsatMobile.Jobs;
using VelsatMobile.Services.FirebaseService;

[CompilerGenerated]
internal class Program
{
	private static void Main(string[] args)
	{
		WebApplicationBuilder webApplicationBuilder = WebApplication.CreateBuilder(args);
		webApplicationBuilder.Configuration.AddJsonFile("appsettings.json", optional: true);
		webApplicationBuilder.Configuration.AddEnvironmentVariables();
		string value = webApplicationBuilder.Configuration.GetSection("settings").GetSection("secretkey").Value;
		byte[] keyBytes = Encoding.UTF8.GetBytes(value);
		webApplicationBuilder.Services.AddAuthentication(delegate(AuthenticationOptions config)
		{
			config.DefaultAuthenticateScheme = "Bearer";
			config.DefaultChallengeScheme = "Bearer";
		}).AddJwtBearer(delegate(JwtBearerOptions config)
		{
			config.RequireHttpsMetadata = false;
			config.SaveToken = true;
			config.TokenValidationParameters = new TokenValidationParameters
			{
				ValidateIssuerSigningKey = true,
				IssuerSigningKey = new SymmetricSecurityKey(keyBytes),
				ValidateIssuer = false,
				ValidateAudience = false
			};
		});
		webApplicationBuilder.Services.AddControllers().AddJsonOptions(delegate(JsonOptions options)
		{
			options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.Never;
		});
		webApplicationBuilder.Services.AddEndpointsApiExplorer();
		webApplicationBuilder.Services.AddSwaggerGen();
		string connectionString = webApplicationBuilder.Configuration.GetConnectionString("DefaultConnection");
		string connectionString2 = webApplicationBuilder.Configuration.GetConnectionString("SecondConnection");
		if (string.IsNullOrEmpty(connectionString))
		{
			throw new InvalidOperationException("❌ DefaultConnection string not found in configuration");
		}
		Console.WriteLine("✅ DefaultConnection configurado correctamente");
		VelsatBackendAPI.Data.MySqlConfiguration implementationInstance = new VelsatBackendAPI.Data.MySqlConfiguration(connectionString, connectionString2);
		webApplicationBuilder.Services.AddSingleton(implementationInstance);
		webApplicationBuilder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
		webApplicationBuilder.Services.AddScoped<IReadOnlyUnitOfWork, ReadOnlyUnitOfWork>();
		webApplicationBuilder.Services.AddHttpClient<IFirebaseService, FirebaseService>();
		webApplicationBuilder.Services.AddHostedService<DocumentosNotificacionJob>();
		webApplicationBuilder.Services.AddCors(delegate(CorsOptions options)
		{
			options.AddPolicy("AllowSpecificOrigin", delegate(CorsPolicyBuilder builder)
			{
				builder.AllowAnyMethod().AllowAnyHeader().AllowCredentials()
					.SetIsOriginAllowed((string origin) => true);
			});
		});
		webApplicationBuilder.Services.AddDistributedMemoryCache();
		webApplicationBuilder.Services.AddSession(delegate(SessionOptions options)
		{
			options.IdleTimeout = TimeSpan.FromHours(1.0);
		});
		WebApplication app = webApplicationBuilder.Build();
		int num = 0;
		bool flag = false;
		while (!flag && num < 3)
		{
			try
			{
				num++;
				MySqlConnection.ClearAllPools();
				flag = true;
				ILogger<Program> requiredService = app.Services.GetRequiredService<ILogger<Program>>();
				requiredService.LogInformation("✅ [Startup] Pools de MySQL limpiados correctamente (intento {Attempt})", num);
				Process currentProcess = Process.GetCurrentProcess();
				requiredService.LogInformation("\ud83d\udcca [Startup] Process ID: {ProcessId} | Start Time: {StartTime}", currentProcess.Id, currentProcess.StartTime);
			}
			catch (Exception ex)
			{
				ILogger<Program> requiredService2 = app.Services.GetRequiredService<ILogger<Program>>();
				requiredService2.LogWarning(ex, "⚠\ufe0f [Startup] Error limpiando pools (intento {Attempt}/{MaxAttempts}): {Message}", num, 3, ex.Message);
				if (num < 3)
				{
					Thread.Sleep(1000 * num);
				}
			}
		}
		if (!flag)
		{
			ILogger<Program> requiredService3 = app.Services.GetRequiredService<ILogger<Program>>();
			requiredService3.LogError("❌ [Startup] No se pudieron limpiar los pools después de 3 intentos");
		}
		IHostApplicationLifetime requiredService4 = app.Services.GetRequiredService<IHostApplicationLifetime>();
		requiredService4.ApplicationStopping.Register(delegate
		{
			ILogger<Program> requiredService6 = app.Services.GetRequiredService<ILogger<Program>>();
			requiredService6.LogWarning("⚠\ufe0f [Shutdown] Aplicación deteniéndose (reciclaje de IIS detectado)...");
			try
			{
				Process currentProcess2 = Process.GetCurrentProcess();
				requiredService6.LogInformation("\ud83d\udcca [Shutdown] Process ID: {ProcessId} | Uptime: {Uptime} minutos", currentProcess2.Id, (DateTime.Now - currentProcess2.StartTime).TotalMinutes);
				requiredService6.LogInformation("⏳ [Shutdown] Esperando 2 segundos para requests activos...");
				Thread.Sleep(2000);
				MySqlConnection.ClearAllPools();
				requiredService6.LogInformation("✅ [Shutdown] Pools de MySQL limpiados correctamente");
				requiredService6.LogInformation("\ud83e\uddf9 [Shutdown] Ejecutando garbage collection...");
				GC.Collect();
				GC.WaitForPendingFinalizers();
				GC.Collect();
				requiredService6.LogInformation("✅ [Shutdown] Limpieza completada correctamente");
			}
			catch (Exception ex2)
			{
				requiredService6.LogError(ex2, "❌ [Shutdown] Error durante limpieza: {Message}", ex2.Message);
			}
		});
		requiredService4.ApplicationStopped.Register(delegate
		{
			try
			{
				ILogger<Program> requiredService6 = app.Services.GetRequiredService<ILogger<Program>>();
				requiredService6.LogWarning("\ud83d\uded1 [Shutdown] Aplicación detenida completamente");
			}
			catch
			{
				Console.WriteLine("\ud83d\uded1 [Shutdown] Aplicación detenida completamente");
			}
		});
		app.UseSwagger();
		app.UseSwaggerUI();
		app.UseHttpsRedirection();
		app.UseRouting();
		app.UseAuthentication();
		app.UseAuthorization();
		app.UseCors("AllowSpecificOrigin");
		app.UseSession();
		app.MapControllers();
		ILogger<Program> requiredService5 = app.Services.GetRequiredService<ILogger<Program>>();
		requiredService5.LogInformation("\ud83d\ude80 [Startup] Aplicación iniciada correctamente");
		app.Run();
	}
}
