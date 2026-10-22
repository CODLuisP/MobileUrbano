using System.Collections.Generic;
using System.Threading.Tasks;
using VelsatMobile.Model;
using VelsatMobile.Model.NotificacionesMobile;

namespace VelsatMobile.Data.Repositories;

public interface IAplicativoRepository
{
	Task<IEnumerable<Unidad>> GetUnidadesUsuario(string accountID);

	Task<IEnumerable<Notificaciones>> GetNotifications(string accountID);

	Task<double> GetKilometersDay(string deviceID);

	Task<IEnumerable<Notificaciones>> GetNotifDevice(string accountID, string deviceID);

	Task<List<Documento>> DocumentosPorVencer(string accountID);

	Task<bool> SaveFCMTokenAsync(string userId, string fcmToken, string platform);

	Task<IEnumerable<UserFCMToken>> GetAllUserFCMTokensAsync();
}
