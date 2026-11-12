using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Dapper;
using VelsatBackendAPI.Model;

namespace VelsatBackendAPI.Data.Repositories;

public class KilometrosRepository : IKilometrosRepository
{
	public class ResultadosCalculoDias
	{
		public double NumDias { get; set; }

		public int UnixFechaInicio { get; set; }

		public int UnixFechaFin { get; set; }
	}

	public class ResultDate
	{
		public string dateStart { get; set; }

		public string dateEnd { get; set; }
	}

	private readonly IDbConnection _defaultConnection;

	private readonly IDbConnection _secondConnection;

	private readonly IDbTransaction _defaultTransaction;

	private readonly IDbTransaction _secondTransaction;

	public KilometrosRepository(IDbConnection defaultConnection, IDbConnection secondConnection, IDbTransaction defaulttransaction, IDbTransaction secondTransaction)
	{
		_defaultConnection = defaultConnection;
		_secondConnection = secondConnection;
		_defaultTransaction = defaulttransaction;
		_secondTransaction = secondTransaction;
	}

	public async Task<KilometrosReporting> GetKmReporting(string fechaini, string fechafin, string deviceID, string accountID)
	{
		ResultDate resultDate = FormatDate(fechaini, fechafin);
		fechaini = resultDate.dateStart;
		fechafin = resultDate.dateEnd;
		ResultadosCalculoDias resultadosCalculoDias = CalcularDias(fechaini, fechafin);
		if (resultadosCalculoDias.NumDias <= 5.0)
		{
			// La depuración diaria mueve a historicos, con un retraso de 7 días, el registro
			// del día calendario correspondiente (hoy - 7). Todo lo posterior a ese día
			// sigue en eventdata.
			int unixLimite = GetUnixLimiteDepuracion();
			List<KilometrosRecorridos> lista;
			if (resultadosCalculoDias.UnixFechaFin <= unixLimite)
			{
				lista = ConsultarHistoricosKm(accountID, deviceID, resultadosCalculoDias.UnixFechaInicio, resultadosCalculoDias.UnixFechaFin);
			}
			else if (resultadosCalculoDias.UnixFechaInicio > unixLimite)
			{
				lista = ConsultarEventDataKm(accountID, deviceID, resultadosCalculoDias.UnixFechaInicio, resultadosCalculoDias.UnixFechaFin);
			}
			else
			{
				// El rango cruza el límite: el máximo y mínimo de odómetro se combinan por dispositivo.
				lista = CombinarKilometros(ConsultarHistoricosKm(accountID, deviceID, resultadosCalculoDias.UnixFechaInicio, unixLimite), ConsultarEventDataKm(accountID, deviceID, unixLimite + 1, resultadosCalculoDias.UnixFechaFin));
			}
			KilometrosReporting kilometrosReporting = new KilometrosReporting
			{
				ListaKilometros = lista
			};
			for (int i = 0; i < kilometrosReporting.ListaKilometros.Count; i++)
			{
				kilometrosReporting.ListaKilometros[i].Item = i + 1;
			}
			if (kilometrosReporting.ListaKilometros.Count == 0 || kilometrosReporting.ListaKilometros == null)
			{
				return new KilometrosReporting
				{
					Mensaje = "No se encontro datos disponible en el rango de fechas ingresado"
				};
			}
			return kilometrosReporting;
		}
		return new KilometrosReporting
		{
			Mensaje = "La diferencia entre las fechas es mayor a 5 días; seleccione otra fechas"
		};
	}

	public async Task<KilometrosReporting> GetAllKmReporting(string fechaini, string fechafin, string accountID)
	{
		ResultDate resultDate = FormatDate(fechaini, fechafin);
		fechaini = resultDate.dateStart;
		fechafin = resultDate.dateEnd;
		ResultadosCalculoDias resultadosCalculoDias = CalcularDias(fechaini, fechafin);
		double numDias = resultadosCalculoDias.NumDias;
		KilometrosReporting kilometrosReporting = new KilometrosReporting
		{
			ListaKilometros = new List<KilometrosRecorridos>()
		};
		if (numDias <= 5.0)
		{
			int unixLimite = GetUnixLimiteDepuracion();
			if (resultadosCalculoDias.UnixFechaFin <= unixLimite)
			{
				kilometrosReporting.ListaKilometros = ConsultarHistoricosKmAll(accountID, resultadosCalculoDias.UnixFechaInicio, resultadosCalculoDias.UnixFechaFin);
			}
			else if (resultadosCalculoDias.UnixFechaInicio > unixLimite)
			{
				kilometrosReporting.ListaKilometros = ConsultarEventDataKmAll(accountID, resultadosCalculoDias.UnixFechaInicio, resultadosCalculoDias.UnixFechaFin);
			}
			else
			{
				kilometrosReporting.ListaKilometros = CombinarKilometros(ConsultarHistoricosKmAll(accountID, resultadosCalculoDias.UnixFechaInicio, unixLimite), ConsultarEventDataKmAll(accountID, unixLimite + 1, resultadosCalculoDias.UnixFechaFin));
			}
			for (int i = 0; i < kilometrosReporting.ListaKilometros.Count; i++)
			{
				kilometrosReporting.ListaKilometros[i].Item = i + 1;
			}
			if (kilometrosReporting.ListaKilometros.Count == 0)
			{
				return new KilometrosReporting
				{
					Mensaje = "No se encontró datos disponible en el rango de fechas ingresado"
				};
			}
			return kilometrosReporting;
		}
		return new KilometrosReporting
		{
			Mensaje = "La diferencia entre las fechas es mayor a 5 días; seleccione otra fechas"
		};
	}

	private int GetUnixLimiteDepuracion()
	{
		DateTime dateTime = DateTime.Now.Date.AddDays(-7.0);
		return DateUnix(dateTime.ToString("dd/MM/yyyy") + " 23:59");
	}

	private List<KilometrosRecorridos> ConsultarHistoricosKm(string accountID, string deviceID, int unixFechaInicio, int unixFechaFin)
	{
		List<Historicos> list = _defaultConnection.Query<Historicos>("select tabla from historicos where timeini<=@FechafinUnix and timefin>=@FechainiUnix", new
		{
			FechainiUnix = unixFechaInicio,
			FechafinUnix = unixFechaFin
		}, _defaultTransaction).ToList();
		if (list.Count == 0)
		{
			return ConsultarEventDataKm(accountID, deviceID, unixFechaInicio, unixFechaFin);
		}
		List<KilometrosRecorridos> list2 = new List<KilometrosRecorridos>();
		foreach (Historicos item in list)
		{
			string sql = "select deviceID, MAX(odometerKM) AS maximo, MIN(odometerKM) AS minimo, (MAX(odometerKM) - MIN(odometerKM)) as kilometros from " + item.Tabla + " where accountID = @AccountID and deviceID = @DeviceID and timestamp between @FechainiUnix and @FechafinUnix group by deviceID";
			list2.AddRange(_secondConnection.Query<KilometrosRecorridos>(sql, new
			{
				AccountID = accountID,
				DeviceID = deviceID,
				FechainiUnix = unixFechaInicio,
				FechafinUnix = unixFechaFin
			}, _secondTransaction).ToList());
		}
		return list2;
	}

	private List<KilometrosRecorridos> ConsultarEventDataKm(string accountID, string deviceID, int unixFechaInicio, int unixFechaFin)
	{
		string sql = "select deviceID, MAX(odometerKM) AS maximo, MIN(odometerKM) AS minimo, (MAX(odometerKM) - MIN(odometerKM)) as kilometros from eventdata where accountID = @AccountID and deviceID = @DeviceID and timestamp between @FechainiUnix and @FechafinUnix group by deviceID";
		return _defaultConnection.Query<KilometrosRecorridos>(sql, new
		{
			AccountID = accountID,
			DeviceID = deviceID,
			FechainiUnix = unixFechaInicio,
			FechafinUnix = unixFechaFin
		}, _defaultTransaction).ToList();
	}

	private List<KilometrosRecorridos> ConsultarHistoricosKmAll(string accountID, int unixFechaInicio, int unixFechaFin)
	{
		List<Historicos> list = _defaultConnection.Query<Historicos>("select tabla from historicos where timeini <= @FechafinUnix and timefin >= @FechainiUnix", new
		{
			FechainiUnix = unixFechaInicio,
			FechafinUnix = unixFechaFin
		}, _defaultTransaction).ToList();
		if (list.Count == 0)
		{
			return ConsultarEventDataKmAll(accountID, unixFechaInicio, unixFechaFin);
		}
		List<KilometrosRecorridos> list2 = new List<KilometrosRecorridos>();
		foreach (Historicos item in list)
		{
			string sql = "select deviceID, MAX(odometerKM) AS maximo, MIN(odometerKM) AS minimo, (MAX(odometerKM) - MIN(odometerKM)) as kilometros from " + item.Tabla + " where deviceID in (select deviceID from device where accountID=@AccountID) and timestamp between @FechainiUnix and @FechafinUnix group by deviceID";
			DynamicParameters dynamicParameters = new DynamicParameters();
			dynamicParameters.Add("FechainiUnix", unixFechaInicio);
			dynamicParameters.Add("FechafinUnix", unixFechaFin);
			dynamicParameters.Add("AccountID", accountID);
			list2.AddRange(_secondConnection.Query<KilometrosRecorridos>(sql, dynamicParameters, _secondTransaction).ToList());
		}
		return list2;
	}

	private List<KilometrosRecorridos> ConsultarEventDataKmAll(string accountID, int unixFechaInicio, int unixFechaFin)
	{
		string sql = "select deviceID, MAX(odometerKM) AS maximo, MIN(odometerKM) AS minimo, (MAX(odometerKM) - MIN(odometerKM)) as kilometros from eventdata where deviceID in (select deviceID from device where accountID=@AccountID) and timestamp between @FechainiUnix and @FechafinUnix group by deviceID";
		DynamicParameters dynamicParameters = new DynamicParameters();
		dynamicParameters.Add("FechainiUnix", unixFechaInicio);
		dynamicParameters.Add("FechafinUnix", unixFechaFin);
		dynamicParameters.Add("AccountID", accountID);
		return _defaultConnection.Query<KilometrosRecorridos>(sql, dynamicParameters, _defaultTransaction).ToList();
	}

	private List<KilometrosRecorridos> CombinarKilometros(List<KilometrosRecorridos> a, List<KilometrosRecorridos> b)
	{
		List<KilometrosRecorridos> list = new List<KilometrosRecorridos>();
		foreach (IGrouping<string, KilometrosRecorridos> item in a.Concat(b).GroupBy((KilometrosRecorridos k) => k.DeviceId))
		{
			double num = item.Max((KilometrosRecorridos k) => k.Maximo);
			double num2 = item.Min((KilometrosRecorridos k) => k.Minimo);
			list.Add(new KilometrosRecorridos
			{
				DeviceId = item.Key,
				Maximo = num,
				Minimo = num2,
				Kilometros = num - num2
			});
		}
		return list;
	}

	public int DateUnix(string fecha)
	{
		fecha = WebUtility.UrlDecode(fecha);
		return (int)(DateTime.ParseExact(fecha, "dd/MM/yyyy HH:mm", CultureInfo.InvariantCulture).ToUniversalTime() - new DateTime(1970, 1, 1)).TotalSeconds;
	}

	public ResultadosCalculoDias CalcularDias(string fechaI, string fechaF)
	{
		int num = DateUnix(fechaI);
		int num2 = DateUnix(fechaF);
		double numDias = (double)(num2 - num) / 86400.0;
		return new ResultadosCalculoDias
		{
			NumDias = numDias,
			UnixFechaInicio = num,
			UnixFechaFin = num2
		};
	}

	public ResultDate FormatDate(string dateS, string dateE)
	{
		DateTime.TryParse(dateS, out var result);
		DateTime.TryParse(dateE, out var result2);
		string dateStart = result.ToString("dd/MM/yyyy HH:mm");
		string dateEnd = result2.ToString("dd/MM/yyyy HH:mm");
		return new ResultDate
		{
			dateStart = dateStart,
			dateEnd = dateEnd
		};
	}
}
