using System;

namespace VelsatMobile.Model.NotificacionesMobile;

public class Documento
{
	public int Id { get; set; }

	public string AccountID { get; set; }

	public string DeviceID { get; set; }

	public string Tipo_documento { get; set; }

	public string Nombre_documento { get; set; }

	public string? Archivo_url { get; set; }

	public DateTime? Fecha_vencimiento { get; set; }

	public string? Observaciones { get; set; }

	public string Estado
	{
		get
		{
			if (!Fecha_vencimiento.HasValue)
			{
				return "Sin Fecha";
			}
			DateTime date = DateTime.UtcNow.AddHours(-5.0).Date;
			int days = (Fecha_vencimiento.Value.Date - date).Days;
			if (days < 0)
			{
				return "Vencido";
			}
			if (days <= 15)
			{
				return "Por Vencer";
			}
			if (days <= 30)
			{
				return "Próximo a Vencer";
			}
			return "Vigente";
		}
	}
}
