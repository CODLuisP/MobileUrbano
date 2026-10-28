using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using VelsatMobile.Model;
using VelsatMobile.Model.NotificacionesMobile;

namespace VelsatMobile.Data.Repositories;

public class AplicativoRepository : IAplicativoRepository
{
	private readonly IDbConnection _defaultConnection;

	private IDbTransaction _defaultTransaction;

	public AplicativoRepository(IDbConnection defaultconnection, IDbTransaction defaulttransaction)
	{
		_defaultConnection = defaultconnection;
		_defaultTransaction = defaulttransaction;
	}

	public async Task<IEnumerable<Unidad>> GetUnidadesUsuario(string accountID)
	{
		string sql = "\r\n        SELECT DISTINCT deviceID \r\n        FROM (\r\n            SELECT deviceID FROM device WHERE accountID = @AccountID \r\n            UNION ALL \r\n            SELECT deviceID FROM deviceuser WHERE userID = @AccountID AND Status = 1\r\n        ) AS combined_devices";
		return await _defaultConnection.QueryAsync<Unidad>(sql, new
		{
			AccountID = accountID
		});
	}

	public async Task<IEnumerable<Notificaciones>> GetNotifications(string accountID)
	{
		string sql = "Select accountID, deviceID, timestamp, statusCode, latitude, longitude, speedKPH from eventdata where accountID = @AccountID and timestamp >= UNIX_TIMESTAMP(CURDATE()) and statusCode IN (61714, 61715) order by timestamp DESC";
		return await _defaultConnection.QueryAsync<Notificaciones>(sql, new
		{
			AccountID = accountID
		});
	}

	public async Task<double> GetKilometersDay(string deviceID)
	{
		string sql = "SELECT (MAX(odometerKM) - MIN(odometerKM)) as km FROM eventdata WHERE deviceID = @DeviceID AND timestamp >= UNIX_TIMESTAMP(CURDATE())";
		return (await _defaultConnection.QueryFirstOrDefaultAsync<double?>(sql, new
		{
			DeviceID = deviceID
		})).GetValueOrDefault();
	}

	public async Task<IEnumerable<Notificaciones>> GetNotifDevice(string accountID, string deviceID)
	{
		string sql = "Select accountID, deviceID, timestamp, statusCode, latitude, longitude, speedKPH from eventdata where accountID = @AccountID and deviceID = @DeviceID and timestamp >= UNIX_TIMESTAMP(CURDATE()) and statusCode IN (61714, 61715) order by timestamp DESC";
		return await _defaultConnection.QueryAsync<Notificaciones>(sql, new
		{
			AccountID = accountID,
			DeviceID = deviceID
		});
	}

	public async Task<List<Documento>> DocumentosPorVencer(string accountID)
	{
		DateTime fechaLimite = DateTime.UtcNow.AddHours(-5.0).Date.AddDays(30.0);
		try
		{
			return (await _defaultConnection.QueryAsync<Documento>("SELECT * FROM documentos WHERE Fecha_vencimiento IS NOT NULL AND Fecha_vencimiento <= @FechaLimite AND accountID = @AccountID ORDER BY Fecha_vencimiento ASC", new
			{
				FechaLimite = fechaLimite,
				AccountID = accountID
			}, _defaultTransaction)).ToList();
		}
		catch (Exception)
		{
			Console.WriteLine("Error al obtener documentos próximos a vencer");
			return new List<Documento>();
		}
	}

	public async Task<bool> SaveFCMTokenAsync(string userId, string fcmToken, string platform)
	{
		string sql = "SELECT COUNT(1) FROM UserFCMTokens \r\n                        WHERE UserId = @UserId AND FCMToken = @FCMToken";
		if (await _defaultConnection.ExecuteScalarAsync<int>(sql, new
		{
			UserId = userId,
			FCMToken = fcmToken
		}, _defaultTransaction) > 0)
		{
			return true;
		}
		string sql2 = "INSERT INTO UserFCMTokens \r\n                         (UserId, FCMToken, Platform, CreatedAt, UpdatedAt)\r\n                         VALUES (@UserId, @FCMToken, @Platform, NOW(), NOW())";
		return await _defaultConnection.ExecuteAsync(sql2, new
		{
			UserId = userId,
			FCMToken = fcmToken,
			Platform = platform
		}, _defaultTransaction) > 0;
	}

	public async Task<IEnumerable<UserFCMToken>> GetAllUserFCMTokensAsync()
	{
		string sql = "SELECT Id, userId, fcmtoken, platform, createdAt, updatedAt FROM userfcmtokens";
		return await _defaultConnection.QueryAsync<UserFCMToken>(sql, null, _defaultTransaction);
	}
}
