using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using VelsatBackendAPI.Data.Repositories;
using VelsatMobile.Data.Repositories;
using VelsatMobile.Model.NotificacionesMobile;
using VelsatMobile.Services.FirebaseService;

namespace VelsatMobile.Jobs;

public class DocumentosNotificacionJob : BackgroundService
{
	private readonly IServiceScopeFactory _scopeFactory;

	private readonly IFirebaseService _firebaseService;

	private readonly ILogger<DocumentosNotificacionJob> _logger;

	private readonly TimeSpan[] _horariosNotificacion = new TimeSpan[2]
	{
		new TimeSpan(7, 0, 0),
		new TimeSpan(18, 0, 0)
	};

	public DocumentosNotificacionJob(IServiceScopeFactory scopeFactory, IFirebaseService firebaseService, ILogger<DocumentosNotificacionJob> logger)
	{
		_scopeFactory = scopeFactory;
		_firebaseService = firebaseService;
		_logger = logger;
	}

	protected override async Task ExecuteAsync(CancellationToken stoppingToken)
	{
		_logger.LogInformation("DocumentosNotificacionJob iniciado.");
		while (!stoppingToken.IsCancellationRequested)
		{
			DateTime dateTime = DateTime.UtcNow.AddHours(-5.0);
			DateTime dateTime2 = CalcularProximaEjecucion(dateTime);
			TimeSpan delay = dateTime2 - dateTime;
			_logger.LogInformation($"Próxima notificación: {dateTime2:dd/MM/yyyy HH:mm}");
			await Task.Delay(delay, stoppingToken);
			await EnviarNotificacionesAsync();
		}
	}

	private DateTime CalcularProximaEjecucion(DateTime ahora)
	{
		foreach (TimeSpan item in _horariosNotificacion.OrderBy((TimeSpan h) => h))
		{
			DateTime dateTime = ahora.Date.Add(item);
			if (dateTime > ahora)
			{
				return dateTime;
			}
		}
		return ahora.Date.AddDays(1.0).Add(_horariosNotificacion.Min());
	}

	private async Task EnviarNotificacionesAsync()
	{
		_ = 2;
		try
		{
			using IServiceScope scope = _scopeFactory.CreateScope();
			IReadOnlyUnitOfWork readOnlyUow = scope.ServiceProvider.GetRequiredService<IReadOnlyUnitOfWork>();
			IUnitOfWork requiredService = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
			IEnumerable<UserFCMToken> enumerable = await requiredService.AplicativoRepository.GetAllUserFCMTokensAsync();
			_logger.LogInformation($"Procesando {enumerable.Count()} usuarios...");
			foreach (UserFCMToken usuario in enumerable)
			{
				try
				{
					List<Documento> list = await readOnlyUow.AplicativoRepository.DocumentosPorVencer(usuario.UserId);
					if (list != null && list.Any())
					{
						list.Count((Documento d) => d.Estado == "Vencido");
						list.Count((Documento d) => d.Estado == "Por Vencer");
						list.Count((Documento d) => d.Estado == "Próximo a Vencer");
						string titulo = "Documentos - Velsat";
						string mensaje = ArmarMensaje(list);
						if (await _firebaseService.SendPushNotificationAsync(usuario.FCMToken, titulo, mensaje))
						{
							_logger.LogInformation("Notificación enviada a " + usuario.UserId);
						}
						else
						{
							_logger.LogWarning("Fallo notificación para " + usuario.UserId);
						}
					}
				}
				catch (Exception ex)
				{
					_logger.LogError("Error procesando usuario " + usuario.UserId + ": " + ex.Message);
				}
			}
		}
		catch (Exception ex2)
		{
			_logger.LogError("Error general en job: " + ex2.Message);
		}
	}

	private string ArmarMensaje(IEnumerable<Documento> documentos)
	{
		List<Documento> list = documentos.ToList();
		int count = list.Count;
		if (count == 1)
		{
			Documento documento = list.First();
			return documento.Estado switch
			{
				"Vencido" => "Tu " + documento.Nombre_documento + " está vencido. Actualízalo.", 
				"Por Vencer" => "Tu " + documento.Nombre_documento + " vence en menos de 15 días.", 
				"Próximo a Vencer" => "Tu " + documento.Nombre_documento + " vence en menos de 30 días.", 
				_ => "Revisa tu " + documento.Nombre_documento + ".", 
			};
		}
		int num = list.Count((Documento d) => d.Estado == "Vencido");
		int num2 = list.Count((Documento d) => d.Estado == "Por Vencer");
		int num3 = list.Count((Documento d) => d.Estado == "Próximo a Vencer");
		List<string> list2 = new List<string>();
		if (num > 0)
		{
			list2.Add($"{num} vencido(s)");
		}
		if (num2 > 0)
		{
			list2.Add($"{num2} por vencer (≤15 días)");
		}
		if (num3 > 0)
		{
			list2.Add($"{num3} próximo(s) a vencer (≤30 días)");
		}
		return $"Tienes {count} documentos: {string.Join(", ", list2)}. Revísalos.";
	}
}
