using System.Collections.Generic;

namespace VelsatBackendAPI.Model;

public class DatosReporting
{
	public List<TablasReporting> ListaTablas { get; set; } = new List<TablasReporting>();

	public string Mensaje { get; set; }

	public int TotalPages { get; set; }
}
