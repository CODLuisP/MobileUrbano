using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using VelsatBackendAPI.Data.Repositories;
using VelsatBackendAPI.Model;

namespace VelsatBackendAPI.Hubs;

public class ActualizacionTiempoReal : Hub
{
	private readonly IServiceScopeFactory _serviceScopeFactory;

	private readonly IHubContext<ActualizacionTiempoReal> _hubContext;

	private static readonly Dictionary<string, Timer> _userTimers = new Dictionary<string, Timer>();

	private static readonly object _lockObject = new object();

	public ActualizacionTiempoReal(IServiceScopeFactory serviceScopeFactory, IHubContext<ActualizacionTiempoReal> hubContext)
	{
		_serviceScopeFactory = serviceScopeFactory;
		_hubContext = hubContext;
	}

	public async Task UnirGrupo()
	{
		string username = GetUsernameFromRoute();
		if (string.IsNullOrEmpty(username))
		{
			await base.Clients.Caller.SendAsync("Error", "Username no encontrado en la ruta");
			return;
		}
		try
		{
			await base.Groups.AddToGroupAsync(base.Context.ConnectionId, username);
			IniciarTimer(username);
			await base.Clients.Caller.SendAsync("ConectadoExitosamente", username);
			Console.WriteLine("[DEBUG] Usuario " + username + " se unió al grupo desde la ruta");
		}
		catch (Exception ex)
		{
			Console.WriteLine("[ERROR] Error uniendo al grupo: " + ex.Message);
			await base.Clients.Caller.SendAsync("Error", "Error al unirse al grupo: " + ex.Message);
		}
	}

	public async Task DejarGrupo()
	{
		string username = GetUsernameFromRoute();
		if (string.IsNullOrEmpty(username))
		{
			return;
		}
		try
		{
			await base.Groups.RemoveFromGroupAsync(base.Context.ConnectionId, username);
			DetenerTimer(username);
			Console.WriteLine("[DEBUG] Usuario " + username + " dejó el grupo");
		}
		catch (Exception ex)
		{
			Console.WriteLine("[ERROR] Error dejando el grupo: " + ex.Message);
		}
	}

	public override async Task OnConnectedAsync()
	{
		string usernameFromRoute = GetUsernameFromRoute();
		Console.WriteLine("[DEBUG] Cliente conectado: " + base.Context.ConnectionId + ", Username: " + usernameFromRoute);
		if (!string.IsNullOrEmpty(usernameFromRoute))
		{
			await UnirGrupo();
		}
		await base.OnConnectedAsync();
	}

	public override async Task OnDisconnectedAsync(Exception? exception)
	{
		string usernameFromRoute = GetUsernameFromRoute();
		if (!string.IsNullOrEmpty(usernameFromRoute))
		{
			DetenerTimer(usernameFromRoute);
			Console.WriteLine("[DEBUG] Usuario " + usernameFromRoute + " desconectado, timer detenido");
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

	private void IniciarTimer(string username)
	{
		if (string.IsNullOrEmpty(username))
		{
			return;
		}
		lock (_lockObject)
		{
			if (_userTimers.ContainsKey(username))
			{
				_userTimers[username].Dispose();
				_userTimers.Remove(username);
			}
			Timer value = new Timer(async delegate
			{
				await EnviarDatosDirectamente(username);
			}, null, TimeSpan.FromSeconds(1.0), TimeSpan.FromSeconds(5.0));
			_userTimers[username] = value;
		}
		Console.WriteLine("[DEBUG] Timer iniciado para: " + username);
	}

	private void DetenerTimer(string username)
	{
		if (string.IsNullOrEmpty(username))
		{
			return;
		}
		lock (_lockObject)
		{
			if (_userTimers.ContainsKey(username))
			{
				_userTimers[username].Dispose();
				_userTimers.Remove(username);
				Console.WriteLine("[DEBUG] Timer detenido para: " + username);
			}
		}
	}

	private async Task EnviarDatosDirectamente(string username)
	{
		using IServiceScope scope = _serviceScopeFactory.CreateScope();
		_ = 1;
		try
		{
			Console.WriteLine("[DEBUG] Obteniendo datos para: " + username);
			IUnitOfWork requiredService = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
			DatosCargainicial datosCargainicial = await requiredService.DatosCargainicialService.ObtenerDatosCargaInicialAsync(username);
			datosCargainicial.FechaActual = DateTime.Now;
			Console.WriteLine($"[DEBUG] Datos obtenidos para {username}: {datosCargainicial.DatosDevice?.Count ?? 0} dispositivos");
			await _hubContext.Clients.Group(username).SendAsync("ActualizarDatos", datosCargainicial);
			Console.WriteLine("[DEBUG] Datos enviados exitosamente para: " + username);
		}
		catch (Exception ex)
		{
			Console.WriteLine("[ERROR] Error enviando datos para " + username + ": " + ex.Message);
			if (ex.Message.Contains("disposed") || ex.Message.Contains("ObjectDisposed"))
			{
				Console.WriteLine("[WARNING] Deteniendo timer para " + username + " debido a disposed objects");
				DetenerTimer(username);
			}
		}
	}
}
