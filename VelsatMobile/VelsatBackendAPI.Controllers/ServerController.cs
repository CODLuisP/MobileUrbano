using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using VelsatBackendAPI.Model;
using VelsatMobile.Data.Repositories;

namespace VelsatBackendAPI.Controllers;

[Route("api/[controller]")]
[ApiController]
public class ServerController : ControllerBase
{
	private readonly IReadOnlyUnitOfWork _readOnlyUow;

	public ServerController(IReadOnlyUnitOfWork readOnlyUow)
	{
		_readOnlyUow = readOnlyUow;
	}

	[HttpGet("MobileServer/{accountID}")]
	public async Task<IActionResult> GetServidor(string accountID)
	{
		Servidor servidor = await _readOnlyUow.ServidorRepository.GetServidor(accountID);
		if (servidor == null)
		{
			return Ok(new
			{
				mensaje = "usuario incorrecto"
			});
		}
		return Ok(servidor);
	}
}
