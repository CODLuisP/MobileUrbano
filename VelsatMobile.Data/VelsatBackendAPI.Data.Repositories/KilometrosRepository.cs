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
			List<Historicos> list = _defaultConnection.Query<Historicos>("select tabla from historicos where timeini<=@FechafinUnix and timefin>=@FechainiUnix", new
			{
				FechainiUnix = resultadosCalculoDias.UnixFechaInicio,
				FechafinUnix = resultadosCalculoDias.UnixFechaFin
			}, _defaultTransaction).ToList();
			KilometrosReporting kilometrosReporting = new KilometrosReporting
			{
				ListaKilometros = new List<KilometrosRecorridos>()
			};
			if (list.Count == 0)
			{
				string sql = "select deviceID, MAX(odometerKM) AS maximo, MIN(odometerKM) AS minimo, (MAX(odometerKM) - MIN(odometerKM)) as kilometros from eventdata where accountID = @AccountID and deviceID = @DeviceID and timestamp between @FechainiUnix and @FechafinUnix group by deviceID";
				kilometrosReporting.ListaKilometros = _defaultConnection.Query<KilometrosRecorridos>(sql, new
				{
					AccountID = accountID,
					DeviceID = deviceID,
					FechainiUnix = resultadosCalculoDias.UnixFechaInicio,
					FechafinUnix = resultadosCalculoDias.UnixFechaFin
				}, _defaultTransaction).ToList();
				for (int i = 0; i < kilometrosReporting.ListaKilometros.Count; i++)
				{
					kilometrosReporting.ListaKilometros[i].Item = i + 1;
				}
			}
			else
			{
				foreach (Historicos item in list)
				{
					string tabla = item.Tabla;
					string sql2 = "select deviceID, MAX(odometerKM) AS maximo, MIN(odometerKM) AS minimo, (MAX(odometerKM) - MIN(odometerKM)) as kilometros from " + tabla + " where accountID = @AccountID and deviceID = @DeviceID and timestamp between @FechainiUnix and @FechafinUnix group by deviceID";
					List<KilometrosRecorridos> collection = _secondConnection.Query<KilometrosRecorridos>(sql2, new
					{
						AccountID = accountID,
						DeviceID = deviceID,
						FechainiUnix = resultadosCalculoDias.UnixFechaInicio,
						FechafinUnix = resultadosCalculoDias.UnixFechaFin
					}, _secondTransaction).ToList();
					kilometrosReporting.ListaKilometros.AddRange(collection);
				}
				for (int j = 0; j < kilometrosReporting.ListaKilometros.Count; j++)
				{
					kilometrosReporting.ListaKilometros[j].Item = j + 1;
				}
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
			List<Historicos> list = _defaultConnection.Query<Historicos>("select tabla from historicos where timeini <= @FechafinUnix and timefin >= @FechainiUnix", new
			{
				FechainiUnix = resultadosCalculoDias.UnixFechaInicio,
				FechafinUnix = resultadosCalculoDias.UnixFechaFin
			}, _defaultTransaction).ToList();
			if (list.Count == 0)
			{
				string sql = "select deviceID, MAX(odometerKM) AS maximo, MIN(odometerKM) AS minimo, (MAX(odometerKM) - MIN(odometerKM)) as kilometros from eventdata where deviceID in (select deviceID from device where accountID=@AccountID) and timestamp between @FechainiUnix and @FechafinUnix group by deviceID";
				DynamicParameters dynamicParameters = new DynamicParameters();
				dynamicParameters.Add("FechainiUnix", resultadosCalculoDias.UnixFechaInicio);
				dynamicParameters.Add("FechafinUnix", resultadosCalculoDias.UnixFechaFin);
				dynamicParameters.Add("AccountID", accountID);
				kilometrosReporting.ListaKilometros = _defaultConnection.Query<KilometrosRecorridos>(sql, dynamicParameters, _defaultTransaction).ToList();
			}
			else
			{
				foreach (Historicos item in list)
				{
					string tabla = item.Tabla;
					string sql2 = "select deviceID, MAX(odometerKM) AS maximo, MIN(odometerKM) AS minimo, (MAX(odometerKM) - MIN(odometerKM)) as kilometros from " + tabla + " where deviceID in (select deviceID from device where accountID=@AccountID) and timestamp between @FechainiUnix and @FechafinUnix group by deviceID";
					DynamicParameters dynamicParameters2 = new DynamicParameters();
					dynamicParameters2.Add("FechainiUnix", resultadosCalculoDias.UnixFechaInicio);
					dynamicParameters2.Add("FechafinUnix", resultadosCalculoDias.UnixFechaFin);
					dynamicParameters2.Add("AccountID", accountID);
					List<KilometrosRecorridos> collection = _secondConnection.Query<KilometrosRecorridos>(sql2, dynamicParameters2, _secondTransaction).ToList();
					kilometrosReporting.ListaKilometros.AddRange(collection);
				}
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
