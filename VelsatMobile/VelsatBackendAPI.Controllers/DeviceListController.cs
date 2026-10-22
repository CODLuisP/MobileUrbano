using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using VelsatBackendAPI.Model;
using VelsatMobile.Data.Repositories;

namespace VelsatBackendApi.Controllers;

[Route("api/[controller]")]
[ApiController]
public class DeviceListController : ControllerBase
{
	private readonly IReadOnlyUnitOfWork _readOnlyUow;

	public DeviceListController(IReadOnlyUnitOfWork readOnlyUow)
	{
		_readOnlyUow = readOnlyUow;
	}

	[HttpGet("{username}")]
	public async Task<IActionResult> ObtenerDatosCargaInicial(string username)
	{
		DatosCargainicial datosCargainicial = await ObtenerDatosCargaDesdeBD(username);
		DateTime now = DateTime.Now;
		datosCargainicial.FechaActual = now;
		return Ok(datosCargainicial);
	}

	private async Task<DatosCargainicial> ObtenerDatosCargaDesdeBD(string username)
	{
		return await _readOnlyUow.DatosCargainicialService.ObtenerDatosCargaInicialAsync(username);
	}

	[HttpGet("simplified/{username}")]
	public async Task<IActionResult> SimplifiedList(string username)
	{
		return Ok(await _readOnlyUow.DatosCargainicialService.SimplifiedList(username));
	}
}
