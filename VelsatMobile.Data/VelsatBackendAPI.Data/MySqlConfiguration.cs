using System;
using MySql.Data.MySqlClient;

namespace VelsatBackendAPI.Data;

public class MySqlConfiguration
{
	public string DefaultConnection { get; set; }

	public string SecondConnection { get; set; }

	public MySqlConfiguration(string defaultConnection, string secondConnection)
	{
		if (string.IsNullOrWhiteSpace(defaultConnection))
		{
			throw new ArgumentNullException("defaultConnection", "La cadena de conexión por defecto no puede estar vacía");
		}
		if (string.IsNullOrWhiteSpace(secondConnection))
		{
			throw new ArgumentNullException("secondConnection", "La cadena de conexión secundaria no puede estar vacía");
		}
		DefaultConnection = NormalizeConnectionString(defaultConnection);
		SecondConnection = NormalizeConnectionString(secondConnection);
	}

	private static string NormalizeConnectionString(string connectionString)
	{
		try
		{
			return new MySqlConnectionStringBuilder(connectionString).ConnectionString;
		}
		catch (ArgumentException ex)
		{
			throw new InvalidOperationException("Error al normalizar la cadena de conexión. Verifica que sea válida. Error: " + ex.Message, ex);
		}
		catch (Exception ex2)
		{
			throw new InvalidOperationException("Error inesperado al procesar la cadena de conexión: " + ex2.Message, ex2);
		}
	}
}
