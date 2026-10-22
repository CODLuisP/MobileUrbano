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

public class HistoricosRepository : IHistoricosRepository
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

	public HistoricosRepository(IDbConnection defaultConnection, IDbConnection secondConnection, IDbTransaction defaulttransaction, IDbTransaction secondTransaction)
	{
		_defaultConnection = defaultConnection;
		_secondConnection = secondConnection;
		_defaultTransaction = defaulttransaction;
		_secondTransaction = secondTransaction;
	}

	public async Task<DatosReporting> GetDataReporting(string fechaini, string fechafin, string deviceID, string accountID)
	{
		string text = _defaultConnection.QueryFirstOrDefault<string>("SELECT accountID FROM device WHERE deviceID = @DeviceID", new
		{
			DeviceID = deviceID
		}, _defaultTransaction);
		if (!string.IsNullOrEmpty(text))
		{
			accountID = text;
		}
		else if (string.IsNullOrEmpty(accountID))
		{
			return new DatosReporting
			{
				Mensaje = "No se encontró información para el deviceID: " + deviceID
			};
		}
		ResultDate resultDate = FormatDate(fechaini, fechafin);
		fechaini = resultDate.dateStart;
		fechafin = resultDate.dateEnd;
		ResultadosCalculoDias resultadosCalculoDias = CalcularDias(fechaini, fechafin);
		if (resultadosCalculoDias.NumDias <= 3.0)
		{
			List<Historicos> list = _defaultConnection.Query<Historicos>("SELECT tabla FROM historicos WHERE timeini <= @FechafinUnix AND timefin >= @FechainiUnix", new
			{
				FechainiUnix = resultadosCalculoDias.UnixFechaInicio,
				FechafinUnix = resultadosCalculoDias.UnixFechaFin
			}, _defaultTransaction).ToList();
			DatosReporting datosReporting = new DatosReporting
			{
				ListaTablas = new List<TablasReporting>()
			};
			if (list.Count == 0)
			{
				string sql = "\r\n                        SELECT deviceID, timestamp, speedKPH, longitude, latitude, odometerKM, address \r\n                        FROM eventdata \r\n                        WHERE accountID = @AccountID \r\n                          AND deviceID = @DeviceID \r\n                          AND timestamp BETWEEN @FechainiUnix AND @FechafinUnix\r\n                        ORDER BY timestamp";
				datosReporting.ListaTablas = _defaultConnection.Query<TablasReporting>(sql, new
				{
					AccountID = accountID,
					DeviceID = deviceID,
					FechainiUnix = resultadosCalculoDias.UnixFechaInicio,
					FechafinUnix = resultadosCalculoDias.UnixFechaFin
				}, _defaultTransaction).ToList();
				for (int i = 0; i < datosReporting.ListaTablas.Count; i++)
				{
					datosReporting.ListaTablas[i].Item = i + 1;
				}
			}
			else
			{
				foreach (Historicos item in list)
				{
					string tabla = item.Tabla;
					string sql2 = "\r\n                            SELECT deviceID, timestamp, speedKPH, longitude, latitude, odometerKM, address \r\n                            FROM " + tabla + " \r\n                            WHERE accountID = @AccountID \r\n                              AND deviceID = @DeviceID \r\n                              AND timestamp BETWEEN @FechainiUnix AND @FechafinUnix\r\n                            ORDER BY timestamp";
					List<TablasReporting> collection = _secondConnection.Query<TablasReporting>(sql2, new
					{
						AccountID = accountID,
						DeviceID = deviceID,
						FechainiUnix = resultadosCalculoDias.UnixFechaInicio,
						FechafinUnix = resultadosCalculoDias.UnixFechaFin
					}, _secondTransaction).ToList();
					datosReporting.ListaTablas.AddRange(collection);
				}
				for (int j = 0; j < datosReporting.ListaTablas.Count; j++)
				{
					datosReporting.ListaTablas[j].Item = j + 1;
				}
			}
			if (datosReporting.ListaTablas.Count == 0 || datosReporting.ListaTablas == null)
			{
				return new DatosReporting
				{
					Mensaje = "No se encontró datos disponible en el rango de fechas ingresado"
				};
			}
			return datosReporting;
		}
		return new DatosReporting
		{
			Mensaje = "La diferencia entre las fechas es mayor a 3 días; seleccione otras fechas"
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

	public async Task<List<SpeedReporting>> GetSpeedData(string fechaini, string fechafin, string deviceID, double speedKPH, string accountID)
	{
		DatosReporting datosReporting = await GetDataReporting(fechaini, fechafin, deviceID, accountID);
		List<SpeedReporting> list = new List<SpeedReporting>();
		if (datosReporting != null && datosReporting.ListaTablas.Count > 0)
		{
			for (int i = 0; i < datosReporting.ListaTablas.Count - 1; i++)
			{
				if (datosReporting.ListaTablas[i].SpeedKPH >= speedKPH)
				{
					SpeedReporting item = new SpeedReporting
					{
						Item = list.Count + 1,
						SpeedKPH = datosReporting.ListaTablas[i].SpeedKPH,
						Date = datosReporting.ListaTablas[i].Fecha,
						Time = datosReporting.ListaTablas[i].Hora,
						Latitude = datosReporting.ListaTablas[i].Latitude,
						Longitude = datosReporting.ListaTablas[i].Longitude,
						Address = datosReporting.ListaTablas[i].Address
					};
					list.Add(item);
				}
			}
		}
		return list;
	}

	public async Task<List<StopsReporting>> GetStopData(string fechaini, string fechafin, string deviceID, string accountID)
	{
		DatosReporting datosReporting = await GetDataReporting(fechaini, fechafin, deviceID, accountID);
		List<StopsReporting> list = new List<StopsReporting>();
		if (datosReporting != null && datosReporting.ListaTablas.Count > 0)
		{
			int num = 0;
			for (int i = 0; i < datosReporting.ListaTablas.Count - 1; i++)
			{
				if (datosReporting.ListaTablas[i].SpeedKPH == 0.0 && datosReporting.ListaTablas[i + 1].SpeedKPH == 0.0)
				{
					num++;
					int num2 = datosReporting.ListaTablas.Count - 1;
					if (i + 1 == num2 && num > 0)
					{
						int num3 = i + 1;
						StopsReporting stopsReporting = new StopsReporting
						{
							Item = list.Count + 1,
							StartDate = datosReporting.ListaTablas[num3 - num].Fecha,
							StartTime = datosReporting.ListaTablas[num3 - num].Hora,
							EndDate = datosReporting.ListaTablas[num3].Fecha,
							EndTime = datosReporting.ListaTablas[num3].Hora,
							Longitude = datosReporting.ListaTablas[num3 - num].Longitude,
							Latitude = datosReporting.ListaTablas[num3 - num].Latitude,
							Address = datosReporting.ListaTablas[num3].Address,
							TimeStampIni = datosReporting.ListaTablas[num3 - num].Timestamp,
							TimeStampEnd = datosReporting.ListaTablas[num3].Timestamp
						};
						int num4 = stopsReporting.TimeStampEnd - stopsReporting.TimeStampIni;
						int value = num4 / 3600;
						int value2 = num4 % 3600 / 60;
						int value3 = num4 % 60;
						string totalTime = $"{value:D2}H:{value2:D2}M:{value3:D2}S";
						stopsReporting.TotalTime = totalTime;
						list.Add(stopsReporting);
						num = 0;
					}
				}
				else if (num > 0)
				{
					StopsReporting stopsReporting2 = new StopsReporting
					{
						Item = list.Count + 1,
						StartDate = datosReporting.ListaTablas[i - num].Fecha,
						StartTime = datosReporting.ListaTablas[i - num].Hora,
						EndDate = datosReporting.ListaTablas[i].Fecha,
						EndTime = datosReporting.ListaTablas[i].Hora,
						Longitude = datosReporting.ListaTablas[i - num].Longitude,
						Latitude = datosReporting.ListaTablas[i - num].Latitude,
						Address = datosReporting.ListaTablas[i].Address,
						TimeStampIni = datosReporting.ListaTablas[i - num].Timestamp,
						TimeStampEnd = datosReporting.ListaTablas[i].Timestamp
					};
					int num5 = stopsReporting2.TimeStampEnd - stopsReporting2.TimeStampIni;
					int value4 = num5 / 3600;
					int value5 = num5 % 3600 / 60;
					int value6 = num5 % 60;
					string totalTime2 = $"{value4:D2}H:{value5:D2}M:{value6:D2}S";
					stopsReporting2.TotalTime = totalTime2;
					list.Add(stopsReporting2);
					num = 0;
				}
			}
		}
		return list;
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

	public async Task<List<RouteDetails>> GetRouteDetails(string fechaini, string fechafin, string deviceID, string accountID)
	{
		DatosReporting datosReporting = await GetDataReporting(fechaini, fechafin, deviceID, accountID);
		List<RouteDetails> list = new List<RouteDetails>();
		if (datosReporting != null && datosReporting.ListaTablas.Count > 0)
		{
			int num = 0;
			int num2 = datosReporting.ListaTablas.Count - 1;
			for (int i = 0; i < datosReporting.ListaTablas.Count - 1; i++)
			{
				if (datosReporting.ListaTablas[i].SpeedKPH == 0.0 && datosReporting.ListaTablas[i + 1].SpeedKPH == 0.0)
				{
					num++;
					if (i + 1 == num2 && num > 0)
					{
						int num3 = i + 1;
						RouteDetails item = CreateRouteDetails(datosReporting.ListaTablas[num3 - num]);
						list.Add(item);
						num = 0;
					}
					continue;
				}
				if (num == 0)
				{
					RouteDetails item2 = CreateRouteDetails(datosReporting.ListaTablas[i]);
					list.Add(item2);
					num = 0;
				}
				if (num > 0)
				{
					RouteDetails item3 = CreateRouteDetails(datosReporting.ListaTablas[i - num]);
					list.Add(item3);
					num = 0;
				}
				if (i + 1 == num2 && datosReporting.ListaTablas[num2].SpeedKPH > 0.0)
				{
					RouteDetails item4 = CreateRouteDetails(datosReporting.ListaTablas[num2]);
					list.Add(item4);
					num = 0;
				}
			}
		}
		return list;
	}

	private RouteDetails CreateRouteDetails(TablasReporting gpsData)
	{
		return new RouteDetails
		{
			Date = gpsData.Fecha,
			Time = gpsData.Hora,
			Speed = gpsData.SpeedKPH,
			Longitude = gpsData.Longitude,
			Latitude = gpsData.Latitude
		};
	}

	public string UserName(string deviceID)
	{
		string accountId = _defaultConnection.QueryFirstOrDefault<string>("select accountID from device where deviceID = @DeviceID", new
		{
			DeviceID = deviceID
		}, _defaultTransaction);
		return _defaultConnection.QueryFirstOrDefault<string>("Select description from account where accountID = @AccountId", new
		{
			AccountId = accountId
		}, _defaultTransaction);
	}

	public async Task<List<string>> DeviceFilterSedapal(string rutadefault)
	{
		string sql = "Select deviceID from device where accountID = 'sedapal' and rutadefault = @Rutadefault";
		var param = new
		{
			Rutadefault = rutadefault
		};
		return (await _defaultConnection.QueryAsync<string>(sql, param, _defaultTransaction)).ToList();
	}
}
