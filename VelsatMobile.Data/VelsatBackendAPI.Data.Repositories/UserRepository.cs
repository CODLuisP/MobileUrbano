using System;
using System.Data;
using System.Threading.Tasks;
using Dapper;
using VelsatBackendAPI.Model;

namespace VelsatBackendAPI.Data.Repositories;

public class UserRepository : IUserRepository
{
	private readonly IDbConnection _defaultConnection;

	private readonly IDbTransaction _defaultTransaction;

	public UserRepository(IDbConnection defaultConnection, IDbTransaction defaultTransaction)
	{
		_defaultConnection = defaultConnection;
		_defaultTransaction = defaultTransaction;
	}

	public async Task<Account> GetDetails(string accountID, char tipo)
	{
		string sql = tipo switch
		{
			'n' => "\r\n                    SELECT description, ruc, contactEmail, contactPhone \r\n                    FROM usuarios \r\n                    WHERE accountID = @AccountID", 
			'p' => "\r\n                    SELECT apellidos, dni, telefono, codlan, empresa \r\n                    FROM cliente \r\n                    WHERE codlan = @AccountID", 
			'c' => "\r\n                    SELECT apellidos, nombres, dni, telefono, login \r\n                    FROM taxi \r\n                    WHERE login = @AccountID", 
			_ => throw new ArgumentException("Tipo de usuario no válido", "tipo"), 
		};
		return await _defaultConnection.QueryFirstOrDefaultAsync<Account>(sql, new
		{
			AccountID = accountID
		}, _defaultTransaction);
	}

	public async Task<bool> UpdateUser(Account account, char tipo)
	{
		string sql = tipo switch
		{
			'n' => "\r\n                    UPDATE usuarios \r\n                    SET description = @Description, contactEmail = @ContactEmail, \r\n                        contactPhone = @ContactPhone \r\n                    WHERE accountID = @AccountID", 
			'p' => "\r\n                    UPDATE cliente \r\n                    SET apellidos = @Apellidos, codlan = @Codlan, telefono = @Telefono \r\n                    WHERE codlan = @AccountID", 
			'c' => "\r\n                    UPDATE taxi \r\n                    SET apellidos = @Apellidos, login = @Login, telefono = @Telefono, nombres = NULL\r\n                    WHERE login = @AccountID", 
			_ => throw new ArgumentException("Tipo de usuario no válido", "tipo"), 
		};
		var param = new { account.Description, account.ContactEmail, account.ContactPhone, account.Apellidos, account.Codlan, account.Telefono, account.Login, account.AccountID };
		return await _defaultConnection.ExecuteAsync(sql, param, _defaultTransaction) > 0;
	}

	public async Task<bool> UpdatePassword(string username, string password, char tipo)
	{
		string sql = tipo switch
		{
			'n' => "UPDATE usuarios SET password = @Password WHERE accountID = @AccountID", 
			'p' => "UPDATE cliente SET clave = @Clave WHERE codlan = @AccountID", 
			'c' => "UPDATE taxi SET clave = @Clave WHERE login = @AccountID", 
			_ => throw new ArgumentException("Tipo de usuario no válido", "tipo"), 
		};
		var param = new
		{
			AccountID = username,
			Password = password,
			Clave = password
		};
		return await _defaultConnection.ExecuteAsync(sql, param, _defaultTransaction) > 0;
	}

	public async Task<Account> ValidateUser(string login, string clave, char tipo)
	{
		string sql = tipo switch
		{
			'n' => "\r\n                    SELECT accountID, password, description \r\n                    FROM usuarios \r\n                    WHERE accountID = @Login AND password = @Clave", 
			'p' => "\r\n                    SELECT codcliente AS Codigo, codlan AS AccountID, clave AS Password, \r\n                           apellidos AS Description \r\n                    FROM cliente \r\n                    WHERE codlan = @Login AND clave = @Clave AND estadocuenta = 'A'", 
			'c' => "\r\n                    SELECT codtaxi AS Codigo, login AS AccountID, clave AS Password, \r\n                           CONCAT(apellidos, ' ', nombres) AS Description \r\n                    FROM taxi \r\n                    WHERE login = @Login AND clave = @Clave AND estado = 'A'", 
			_ => throw new ArgumentException("Tipo de usuario no válido", "tipo"), 
		};
		return await _defaultConnection.QueryFirstOrDefaultAsync<Account>(sql, new
		{
			Login = login,
			Clave = clave
		}, _defaultTransaction);
	}
}
