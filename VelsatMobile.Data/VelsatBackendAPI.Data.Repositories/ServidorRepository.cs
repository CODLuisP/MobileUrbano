using System.Data;
using System.Threading.Tasks;
using Dapper;
using VelsatBackendAPI.Model;

namespace VelsatBackendAPI.Data.Repositories;

public class ServidorRepository : IServidorRepository
{
	private readonly IDbConnection _defaultConnection;

	private readonly IDbTransaction _defaultTransaction;

	public ServidorRepository(IDbConnection defaultconnection, IDbTransaction defaulttransaction)
	{
		_defaultConnection = defaultconnection;
		_defaultTransaction = defaulttransaction;
	}

	public async Task<Servidor> GetServidor(string accountID)
	{
		return await _defaultConnection.QueryFirstOrDefaultAsync<Servidor>("Select * from servermobile where loginusu = @AccountID", new
		{
			AccountID = accountID
		}, _defaultTransaction);
	}
}
