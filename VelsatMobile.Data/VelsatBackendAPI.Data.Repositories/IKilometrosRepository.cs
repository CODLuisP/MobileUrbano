using System.Threading.Tasks;
using VelsatBackendAPI.Model;

namespace VelsatBackendAPI.Data.Repositories;

public interface IKilometrosRepository
{
	Task<KilometrosReporting> GetKmReporting(string fechaini, string fechafin, string deviceID, string accountID);

	Task<KilometrosReporting> GetAllKmReporting(string fechaini, string fechafin, string accountID);
}
