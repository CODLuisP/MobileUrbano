using System.Threading.Tasks;

namespace VelsatMobile.Services.FirebaseService;

public interface IFirebaseService
{
	Task<bool> SendPushNotificationAsync(string fcmToken, string titulo, string mensaje);
}
