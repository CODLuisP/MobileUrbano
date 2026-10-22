using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using VelsatBackendAPI.Data.Repositories;
using VelsatMobile.Data.Repositories;
using VelsatMobile.Model;
using VelsatMobile.Model.NotificacionesMobile;

namespace VelsatMobile.Controllers;

[Route("api/[controller]")]
[ApiController]
public class AplicativoController : Controller
{
	private readonly IReadOnlyUnitOfWork _readOnlyUow;

	private readonly IUnitOfWork _uow;

	public AplicativoController(IReadOnlyUnitOfWork readOnlyUow, IUnitOfWork uow)
	{
		_readOnlyUow = readOnlyUow;
		_uow = uow;
	}

	[HttpGet("unidades/{accountID}")]
	public async Task<IActionResult> GetUnidadesUsuario(string accountID)
	{
		try
		{
			if (string.IsNullOrWhiteSpace(accountID))
			{
				return BadRequest("El accountID es requerido");
			}
			IEnumerable<Unidad> enumerable = await _readOnlyUow.AplicativoRepository.GetUnidadesUsuario(accountID);
			if (enumerable == null || !enumerable.Any())
			{
				return NotFound("No se encontraron unidades para el día de hoy");
			}
			return Ok(enumerable);
		}
		catch (Exception ex)
		{
			return StatusCode(500, "Error interno del servidor: " + ex.Message);
		}
	}

	[HttpGet("notifications/{accountID}")]
	public async Task<IActionResult> GetNotifications(string accountID)
	{
		try
		{
			if (string.IsNullOrWhiteSpace(accountID))
			{
				return BadRequest("El accountID es requerido");
			}
			IEnumerable<Notificaciones> enumerable = await _readOnlyUow.AplicativoRepository.GetNotifications(accountID);
			if (enumerable == null || !enumerable.Any())
			{
				return NotFound("No se encontraron notificaciones para el día de hoy");
			}
			return Ok(enumerable);
		}
		catch (Exception ex)
		{
			return StatusCode(500, "Error interno del servidor: " + ex.Message);
		}
	}

	[HttpGet("notifications/{accountID}/{deviceID}")]
	public async Task<IActionResult> GetNotifDevice(string accountID, string deviceID)
	{
		try
		{
			if (string.IsNullOrWhiteSpace(accountID) || string.IsNullOrWhiteSpace(deviceID))
			{
				return BadRequest("El accountID y deviceID es requerido");
			}
			IEnumerable<Notificaciones> enumerable = await _readOnlyUow.AplicativoRepository.GetNotifDevice(accountID, deviceID);
			if (enumerable == null || !enumerable.Any())
			{
				return NotFound("No se encontraron notificaciones para el día de hoy");
			}
			return Ok(enumerable);
		}
		catch (Exception ex)
		{
			return StatusCode(500, "Error interno del servidor: " + ex.Message);
		}
	}

	[HttpGet("vehiculo/{login}/{placa}")]
	public async Task<IActionResult> ObtenerDatosVehiculo(string login, string placa)
	{
		try
		{
			if (string.IsNullOrWhiteSpace(login) || string.IsNullOrWhiteSpace(placa))
			{
				return BadRequest("El login y la placa son requeridos");
			}
			DatosVehiculo datosVehiculo = await _readOnlyUow.DatosCargainicialService.ObtenerDatosVehiculoAsync(login, placa);
			if (datosVehiculo.Vehiculo == null)
			{
				return NotFound("No se encontró el vehículo o no pertenece al usuario");
			}
			return Ok(datosVehiculo);
		}
		catch (Exception ex)
		{
			return StatusCode(500, "Error interno del servidor: " + ex.Message);
		}
	}

	[HttpPost("save-token")]
	public async Task<IActionResult> SaveToken([FromBody] UserFCMToken request)
	{
		try
		{
			if (request == null)
			{
				return BadRequest("Los datos son requeridos.");
			}
			if (string.IsNullOrWhiteSpace(request.UserId))
			{
				return BadRequest("El campo userId es obligatorio.");
			}
			if (string.IsNullOrWhiteSpace(request.FCMToken))
			{
				return BadRequest("El campo fcmtoken es obligatorio.");
			}
			if (string.IsNullOrWhiteSpace(request.Platform) || (request.Platform != "android" && request.Platform != "ios"))
			{
				return BadRequest("El campo Platform debe ser 'android' o 'ios'.");
			}
			bool flag = await _uow.AplicativoRepository.SaveFCMTokenAsync(request.UserId, request.FCMToken, request.Platform);
			_uow.SaveChanges();
			if (!flag)
			{
				return StatusCode(500, new
				{
					success = false,
					message = "No se pudo guardar el token."
				});
			}
			return Ok(new
			{
				success = true,
				message = "Token FCM guardado correctamente."
			});
		}
		catch (Exception ex)
		{
			return StatusCode(500, new
			{
				success = false,
				message = "Error interno del servidor: " + ex.Message
			});
		}
	}
}
