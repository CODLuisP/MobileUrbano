using System.ComponentModel.DataAnnotations;

namespace VelsatBackendAPI.Model;

public class LoginRequest
{
	[Required(ErrorMessage = "El campo usuario es obligatorio")]
	public string Login { get; set; }

	[Required(ErrorMessage = "El campo contraseña es obligatorio")]
	public string Clave { get; set; }

	public char Tipo { get; set; }
}
