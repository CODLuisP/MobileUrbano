using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using VelsatBackendAPI.Model;
using VelsatMobile.Data.Repositories;

namespace VelsatBackendAPI.Controllers;

[Route("api/[controller]")]
[ApiController]
public class LoginController : ControllerBase
{
	private readonly IReadOnlyUnitOfWork _readOnlyUow;

	private readonly string secretkey;

	public LoginController(IReadOnlyUnitOfWork readOnlyUow, IConfiguration config)
	{
		_readOnlyUow = readOnlyUow;
		secretkey = config.GetSection("settings").GetSection("secretkey").Value;
	}

	[HttpPost("MobileLogin")]
	public async Task<IActionResult> Login([FromBody] LoginRequest request)
	{
		if (request == null || string.IsNullOrEmpty(request.Login) || string.IsNullOrEmpty(request.Clave))
		{
			return BadRequest("Los campos están vacíos");
		}
		if (request.Tipo != 'n' && request.Tipo != 'c' && request.Tipo != 'p')
		{
			return BadRequest("Tipo de usuario no válido");
		}
		Account account = await _readOnlyUow.UserRepository.ValidateUser(request.Login, request.Clave, request.Tipo);
		if (account != null)
		{
			string token = GenerateLoginToken(account);
			return StatusCode(200, new
			{
				Token = token,
				Username = request.Login,
				Account = new { account.AccountID, account.Description, account.Codigo }
			});
		}
		return StatusCode(401);
	}

	private string GenerateLoginToken(Account account)
	{
		byte[] bytes = Encoding.ASCII.GetBytes(secretkey);
		ClaimsIdentity claimsIdentity = new ClaimsIdentity();
		claimsIdentity.AddClaim(new Claim("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier", account.AccountID));
		SecurityTokenDescriptor tokenDescriptor = new SecurityTokenDescriptor
		{
			Subject = claimsIdentity,
			Expires = DateTime.UtcNow.AddHours(4.0),
			SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(bytes), "http://www.w3.org/2001/04/xmldsig-more#hmac-sha256")
		};
		JwtSecurityTokenHandler jwtSecurityTokenHandler = new JwtSecurityTokenHandler();
		SecurityToken token = jwtSecurityTokenHandler.CreateToken(tokenDescriptor);
		return jwtSecurityTokenHandler.WriteToken(token);
	}
}
