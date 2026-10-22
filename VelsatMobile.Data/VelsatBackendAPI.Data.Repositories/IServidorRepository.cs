using System.Threading.Tasks;
using VelsatBackendAPI.Model;

namespace VelsatBackendAPI.Data.Repositories;

public interface IServidorRepository
{
	Task<Servidor> GetServidor(string accountID);
}
