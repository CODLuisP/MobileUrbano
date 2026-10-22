using System;
using System.Collections.Generic;

namespace VelsatBackendAPI.Model;

public class DatosCargainicial
{
	public DateTime FechaActual { get; set; }

	public List<Device> DatosDevice { get; set; } = new List<Device>();
}
