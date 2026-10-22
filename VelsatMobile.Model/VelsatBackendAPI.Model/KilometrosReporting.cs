using System.Collections.Generic;

namespace VelsatBackendAPI.Model;

public class KilometrosReporting
{
	public List<KilometrosRecorridos> ListaKilometros { get; set; } = new List<KilometrosRecorridos>();

	public string Mensaje { get; set; }
}
