using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Google.Apis.Auth.OAuth2;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace VelsatMobile.Services.FirebaseService;

public class FirebaseService : IFirebaseService
{
	private readonly IConfiguration _configuration;

	private readonly HttpClient _httpClient;

	private readonly ILogger<FirebaseService> _logger;

	public FirebaseService(IConfiguration configuration, HttpClient httpClient, ILogger<FirebaseService> logger)
	{
		_configuration = configuration;
		_httpClient = httpClient;
		_logger = logger;
	}

	public async Task<bool> SendPushNotificationAsync(string fcmToken, string titulo, string mensaje)
	{
		_ = 2;
		try
		{
			string text = await GetAccessTokenAsync();
			StringContent content = new StringContent(JsonSerializer.Serialize(new
			{
				message = new
				{
					token = fcmToken,
					notification = new
					{
						title = titulo,
						body = mensaje
					}
				}
			}), Encoding.UTF8, "application/json");
			_httpClient.DefaultRequestHeaders.Clear();
			_httpClient.DefaultRequestHeaders.Add("Authorization", "Bearer " + text);
			string text2 = _configuration["Firebase:ProjectId"];
			string requestUri = "https://fcm.googleapis.com/v1/projects/" + text2 + "/messages:send";
			HttpResponseMessage httpResponseMessage = await _httpClient.PostAsync(requestUri, content);
			if (!httpResponseMessage.IsSuccessStatusCode)
			{
				string text3 = await httpResponseMessage.Content.ReadAsStringAsync();
				_logger.LogError("Firebase error: " + text3);
				return false;
			}
			return true;
		}
		catch (Exception ex)
		{
			_logger.LogError("Error enviando notificación Firebase: " + ex.Message);
			return false;
		}
	}

	private async Task<string> GetAccessTokenAsync()
	{
		GoogleCredential googleCredential;
		IConfigurationSection credentialsSection = _configuration.GetSection("Firebase:Credentials");
		if (credentialsSection.Exists())
		{
			// Credenciales embebidas en appsettings.json (Firebase:Credentials)
			Dictionary<string, string> credentials = new Dictionary<string, string>();
			foreach (IConfigurationSection child in credentialsSection.GetChildren())
			{
				credentials[child.Key] = child.Value;
			}
			googleCredential = GoogleCredential.FromJson(JsonSerializer.Serialize(credentials)).CreateScoped("https://www.googleapis.com/auth/firebase.messaging");
		}
		else
		{
			string text = _configuration["Firebase:CredentialsPath"];
			if (!Path.IsPathRooted(text))
			{
				text = Path.Combine(AppContext.BaseDirectory, text);
			}
			using FileStream stream = new FileStream(text, FileMode.Open, FileAccess.Read);
			googleCredential = GoogleCredential.FromStream(stream).CreateScoped("https://www.googleapis.com/auth/firebase.messaging");
		}
		return await googleCredential.UnderlyingCredential.GetAccessTokenForRequestAsync();
	}
}
