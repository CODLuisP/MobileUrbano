using System.Threading.Tasks;
using VelsatBackendAPI.Model;

namespace VelsatBackendAPI.Data.Repositories;

public interface IUserRepository
{
	Task<Account> GetDetails(string accountID, char tipo);

	Task<bool> UpdateUser(Account account, char tipo);

	Task<bool> UpdatePassword(string accountID, string password, char tipo);

	Task<Account> ValidateUser(string accountID, string password, char tipo);
}
