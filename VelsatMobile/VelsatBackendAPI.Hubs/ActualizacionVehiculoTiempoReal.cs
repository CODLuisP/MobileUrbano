using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using VelsatBackendAPI.Data.Repositories;
using VelsatMobile.Model;

namespace VelsatBackendAPI.Hubs;

public class ActualizacionVehiculoTiempoReal : Hub
{
	private readonly IServiceScopeFactory _serviceScopeFactory;

	private readonly IHubContext<ActualizacionVehiculoTiempoReal> _hubContext;

	private static readonly Dictionary<string, Timer> _vehicleTimers = new Dictionary<string, Timer>();

	private static readonly object _lockObject = new object();

	public ActualizacionVehiculoTiempoReal(IServiceScopeFactory serviceScopeFactory, IHubContext<ActualizacionVehiculoTiempoReal> hubContext)
	{
		_serviceScopeFactory = serviceScopeFactory;
		_hubContext = hubContext;
	}

	public async Task UnirGrupoVehiculo()
	{
		string username = GetUsernameFromRoute();
		string placa = GetPlacaFromRoute();
		if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(placa))
		{
			await base.Clients.Caller.SendAsync("Error", "Username o placa no encontrados en la ruta");
			return;
		}
		try
		{
			string groupName = username + "_" + placa;
			await base.Groups.AddToGroupAsync(base.Context.ConnectionId, groupName);
			IniciarTimerVehiculo(username, placa);
			await base.Clients.Caller.SendAsync("ConectadoExitosamente", new { username, placa });
			Console.WriteLine("[DEBUG] Usuario " + username + " se unió al tracking del vehículo " + placa);
		}
		catch (Exception ex)
		{
			Console.WriteLine("[ERROR] Error uniendo al grupo del vehículo: " + ex.Message);
			await base.Clients.Caller.SendAsync("Error", "Error al unirse al grupo: " + ex.Message);
		}
	}

	public async Task DejarGrupoVehiculo()
	{
		string username = GetUsernameFromRoute();
		string placa = GetPlacaFromRoute();
		if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(placa))
		{
			return;
		}
		try
		{
			string groupName = username + "_" + placa;
			await base.Groups.RemoveFromGroupAsync(base.Context.ConnectionId, groupName);
			DetenerTimerVehiculo(username, placa);
			Console.WriteLine("[DEBUG] Usuario " + username + " dejó el tracking del vehículo " + placa);
		}
		catch (Exception ex)
		{
			Console.WriteLine("[ERROR] Error dejando el grupo del vehículo: " + ex.Message);
		}
	}

	public override async Task OnConnectedAsync()
	{
		string usernameFromRoute = GetUsernameFromRoute();
		string placaFromRoute = GetPlacaFromRoute();
		Console.WriteLine($"[DEBUG] Cliente conectado: {base.Context.ConnectionId}, Username: {usernameFromRoute}, Placa: {placaFromRoute}");
		if (!string.IsNullOrEmpty(usernameFromRoute) && !string.IsNullOrEmpty(placaFromRoute))
		{
			await UnirGrupoVehiculo();
		}
		await base.OnConnectedAsync();
	}

	public override async Task OnDisconnectedAsync(Exception? exception)
	{
		string usernameFromRoute = GetUsernameFromRoute();
		string placaFromRoute = GetPlacaFromRoute();
		if (!string.IsNullOrEmpty(usernameFromRoute) && !string.IsNullOrEmpty(placaFromRoute))
		{
			DetenerTimerVehiculo(usernameFromRoute, placaFromRoute);
			Console.WriteLine($"[DEBUG] Usuario {usernameFromRoute} desconectado del vehículo {placaFromRoute}, timer detenido");
		}
		await base.OnDisconnectedAsync(exception);
	}

	private string GetUsernameFromRoute()
	{
		try
		{
			HttpContext httpContext = base.Context.GetHttpContext();
			if (httpContext != null && httpContext.Request.RouteValues.TryGetValue("username", out object value))
			{
				return value?.ToString() ?? string.Empty;
			}
			return string.Empty;
		}
		catch (Exception ex)
		{
			Console.WriteLine("[ERROR] Error obteniendo username de la ruta: " + ex.Message);
			return string.Empty;
		}
	}

	private string GetPlacaFromRoute()
	{
		try
		{
			HttpContext httpContext = base.Context.GetHttpContext();
			if (httpContext != null && httpContext.Request.RouteValues.TryGetValue("placa", out object value))
			{
				return value?.ToString() ?? string.Empty;
			}
			return string.Empty;
		}
		catch (Exception ex)
		{
			Console.WriteLine("[ERROR] Error obteniendo placa de la ruta: " + ex.Message);
			return string.Empty;
		}
	}

	private void IniciarTimerVehiculo(string username, string placa)
	{
		if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(placa))
		{
			return;
		}
		string key = username + "_" + placa;
		lock (_lockObject)
		{
			if (_vehicleTimers.ContainsKey(key))
			{
				_vehicleTimers[key].Dispose();
				_vehicleTimers.Remove(key);
			}
			Timer value = new Timer(async delegate
			{
				await EnviarDatosVehiculo(username, placa);
			}, null, TimeSpan.FromSeconds(1.0), TimeSpan.FromSeconds(5.0));
			_vehicleTimers[key] = value;
		}
		Console.WriteLine("[DEBUG] Timer iniciado para vehículo: " + placa + " del usuario: " + username);
	}

	private void DetenerTimerVehiculo(string username, string placa)
	{
		if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(placa))
		{
			return;
		}
		string key = username + "_" + placa;
		lock (_lockObject)
		{
			if (_vehicleTimers.ContainsKey(key))
			{
				_vehicleTimers[key].Dispose();
				_vehicleTimers.Remove(key);
				Console.WriteLine("[DEBUG] Timer detenido para vehículo: " + placa);
			}
		}
	}

	private async Task EnviarDatosVehiculo(string username, string placa)
	{
		using IServiceScope scope = _serviceScopeFactory.CreateScope();
		_ = 1;
		try
		{
			Console.WriteLine("[DEBUG] Obteniendo datos del vehículo " + placa + " para usuario: " + username);
			IUnitOfWork requiredService = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
			DatosVehiculo datosVehiculo = await requiredService.DatosCargainicialService.ObtenerDatosVehiculoAsync(username, placa);
			datosVehiculo.FechaActual = DateTime.Now;
			Console.WriteLine("[DEBUG] Datos obtenidos del vehículo " + placa);
			string groupName = username + "_" + placa;
			await _hubContext.Clients.Group(groupName).SendAsync("ActualizarDatosVehiculo", datosVehiculo);
			Console.WriteLine("[DEBUG] Datos enviados exitosamente para vehículo: " + placa);
		}
		catch (Exception ex)
		{
			Console.WriteLine("[ERROR] Error enviando datos del vehículo " + placa + ": " + ex.Message);
			if (ex.Message.Contains("disposed") || ex.Message.Contains("ObjectDisposed"))
			{
				Console.WriteLine("[WARNING] Deteniendo timer para vehículo " + placa + " debido a disposed objects");
				DetenerTimerVehiculo(username, placa);
			}
		}
	}
}
