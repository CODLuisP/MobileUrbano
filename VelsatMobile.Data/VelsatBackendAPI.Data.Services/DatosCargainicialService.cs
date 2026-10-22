using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using VelsatBackendAPI.Model;
using VelsatMobile.Model;

namespace VelsatBackendAPI.Data.Services;

public class DatosCargainicialService : IDatosCargainicialService
{
	private readonly IDbConnection _defaultConnection;

	private readonly IDbTransaction _defaultTransaction;

	private List<string> _deviceIds;

	private string _currentLogin;

	public DatosCargainicialService(IDbConnection defaultConnection, IDbTransaction defaultTransaction)
	{
		_defaultConnection = defaultConnection;
		_defaultTransaction = defaultTransaction;
	}

	private async Task<List<string>> GetDeviceIdsAsync(string login)
	{
		if (_deviceIds != null && _currentLogin == login)
		{
			return _deviceIds;
		}
		_deviceIds = (await _defaultConnection.QueryAsync<string>("\r\n                SELECT DISTINCT deviceID \r\n                FROM (\r\n                    SELECT deviceID FROM device WHERE accountID = @Login \r\n                    UNION ALL \r\n                    SELECT deviceID FROM urbano.deviceuser WHERE userID = @Login AND Status = 1\r\n                ) AS combined_devices", new
		{
			Login = login
		}, _defaultTransaction)).ToList();
		_currentLogin = login;
		return _deviceIds;
	}

	public async Task<DatosCargainicial> ObtenerDatosCargaInicialAsync(string login)
	{
		List<string> list = await GetDeviceIdsAsync(login);
		if (!list.Any())
		{
			return new DatosCargainicial
			{
				FechaActual = DateTime.Now,
				DatosDevice = new List<Device>()
			};
		}
		List<Device> datosDevice = (await _defaultConnection.QueryAsync<Device>("\r\n                SELECT deviceID, lastGPSTimestamp, lastValidLatitude, lastValidLongitude, \r\n                       lastOdometerKM, direccion, lastValidHeading, lastValidSpeed \r\n                FROM device \r\n                WHERE deviceID IN @DeviceIDs", new
		{
			DeviceIDs = list
		}, _defaultTransaction)).ToList();
		return new DatosCargainicial
		{
			FechaActual = DateTime.Now,
			DatosDevice = datosDevice
		};
	}

	public async Task<IEnumerable<SimplifiedDevice>> SimplifiedList(string login)
	{
		List<string> list = await GetDeviceIdsAsync(login);
		if (!list.Any())
		{
			return new List<SimplifiedDevice>();
		}
		return await _defaultConnection.QueryAsync<SimplifiedDevice>("\r\n                SELECT DISTINCT d.deviceID, d.lastValidLatitude, d.lastValidLongitude, \r\n                       d.lastValidSpeed, d.direccion \r\n                FROM device d \r\n                WHERE d.deviceID IN @DeviceIds", new
		{
			DeviceIds = list
		}, _defaultTransaction);
	}

	public async Task<DatosVehiculo> ObtenerDatosVehiculoAsync(string login, string placa)
	{
		List<string> list = await GetDeviceIdsAsync(login);
		if (!list.Any())
		{
			return new DatosVehiculo
			{
				FechaActual = DateTime.Now,
				Vehiculo = null
			};
		}
		Device vehiculo = await _defaultConnection.QueryFirstOrDefaultAsync<Device>("\r\n                SELECT deviceID, lastGPSTimestamp, lastValidLatitude, lastValidLongitude, \r\n                       lastValidHeading, lastValidSpeed, lastOdometerKM, direccion \r\n                FROM device \r\n                WHERE deviceID IN @DeviceIDs AND deviceID = @Placa", new
		{
			DeviceIDs = list,
			Placa = placa
		}, _defaultTransaction);
		return new DatosVehiculo
		{
			FechaActual = DateTime.Now,
			Vehiculo = vehiculo
		};
	}

	public void ClearDeviceIdsCache()
	{
		_deviceIds = null;
		_currentLogin = null;
	}
}
