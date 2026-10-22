using System;
using System.Data;
using System.Threading;
using MySql.Data.MySqlClient;
using VelsatBackendAPI.Data;
using VelsatBackendAPI.Data.Repositories;
using VelsatBackendAPI.Data.Services;

namespace VelsatMobile.Data.Repositories;

public class ReadOnlyUnitOfWork : IReadOnlyUnitOfWork, IDisposable
{
	private readonly string _defaultConnectionString;

	private readonly string _secondConnectionString;

	private MySqlConnection _defaultConnection;

	private MySqlConnection _secondConnection;

	private readonly Lazy<IDatosCargainicialService> _datosCargaInicialService;

	private readonly Lazy<IUserRepository> _userRepository;

	private readonly Lazy<IServidorRepository> _servidorRepository;

	private readonly Lazy<IHistoricosRepository> _historicosRepository;

	private readonly Lazy<IKilometrosRepository> _kilometrosRepository;

	private readonly Lazy<IAplicativoRepository> _aplicativoRepository;

	private bool _disposed;

	private readonly object _lockObject = new object();

	private MySqlConnection DefaultConnection
	{
		get
		{
			if (_disposed)
			{
				throw new ObjectDisposedException("ReadOnlyUnitOfWork");
			}
			lock (_lockObject)
			{
				if (_defaultConnection == null || _defaultConnection.State == ConnectionState.Closed || _defaultConnection.State == ConnectionState.Broken)
				{
					if (_defaultConnection != null)
					{
						try
						{
							_defaultConnection.Close();
							_defaultConnection.Dispose();
						}
						catch
						{
						}
						_defaultConnection = null;
					}
					_defaultConnection = OpenConnectionWithRetry(_defaultConnectionString, "DEFAULT");
				}
				else if (_defaultConnection.State == ConnectionState.Connecting || _defaultConnection.State == ConnectionState.Executing || _defaultConnection.State == ConnectionState.Fetching)
				{
					Thread.Sleep(100);
				}
				return _defaultConnection;
			}
		}
	}

	private MySqlConnection SecondConnection
	{
		get
		{
			if (_disposed)
			{
				throw new ObjectDisposedException("ReadOnlyUnitOfWork");
			}
			lock (_lockObject)
			{
				if (_secondConnection == null || _secondConnection.State == ConnectionState.Closed || _secondConnection.State == ConnectionState.Broken)
				{
					if (_secondConnection != null)
					{
						try
						{
							_secondConnection.Close();
							_secondConnection.Dispose();
						}
						catch
						{
						}
						_secondConnection = null;
					}
					_secondConnection = OpenConnectionWithRetry(_secondConnectionString, "SECOND");
				}
				else if (_secondConnection.State == ConnectionState.Connecting || _secondConnection.State == ConnectionState.Executing || _secondConnection.State == ConnectionState.Fetching)
				{
					Thread.Sleep(100);
				}
				return _secondConnection;
			}
		}
	}

	public IDatosCargainicialService DatosCargainicialService
	{
		get
		{
			if (_disposed)
			{
				throw new ObjectDisposedException("ReadOnlyUnitOfWork");
			}
			return _datosCargaInicialService.Value;
		}
	}

	public IUserRepository UserRepository
	{
		get
		{
			if (_disposed)
			{
				throw new ObjectDisposedException("ReadOnlyUnitOfWork");
			}
			return _userRepository.Value;
		}
	}

	public IServidorRepository ServidorRepository
	{
		get
		{
			if (_disposed)
			{
				throw new ObjectDisposedException("ReadOnlyUnitOfWork");
			}
			return _servidorRepository.Value;
		}
	}

	public IHistoricosRepository HistoricosRepository
	{
		get
		{
			if (_disposed)
			{
				throw new ObjectDisposedException("ReadOnlyUnitOfWork");
			}
			return _historicosRepository.Value;
		}
	}

	public IKilometrosRepository KilometrosRepository
	{
		get
		{
			if (_disposed)
			{
				throw new ObjectDisposedException("ReadOnlyUnitOfWork");
			}
			return _kilometrosRepository.Value;
		}
	}

	public IAplicativoRepository AplicativoRepository
	{
		get
		{
			if (_disposed)
			{
				throw new ObjectDisposedException("ReadOnlyUnitOfWork");
			}
			return _aplicativoRepository.Value;
		}
	}

	public ReadOnlyUnitOfWork(VelsatBackendAPI.Data.MySqlConfiguration configuration)
	{
		_defaultConnectionString = configuration.DefaultConnection ?? throw new ArgumentNullException("DefaultConnection");
		_secondConnectionString = configuration.SecondConnection ?? throw new ArgumentNullException("SecondConnection");
		_datosCargaInicialService = new Lazy<IDatosCargainicialService>(() => new DatosCargainicialService(DefaultConnection, null));
		_servidorRepository = new Lazy<IServidorRepository>(() => new ServidorRepository(DefaultConnection, null));
		_historicosRepository = new Lazy<IHistoricosRepository>(() => new HistoricosRepository(DefaultConnection, SecondConnection, null, null));
		_kilometrosRepository = new Lazy<IKilometrosRepository>(() => new KilometrosRepository(DefaultConnection, SecondConnection, null, null));
		_userRepository = new Lazy<IUserRepository>(() => new UserRepository(DefaultConnection, null));
		_aplicativoRepository = new Lazy<IAplicativoRepository>(() => new AplicativoRepository(DefaultConnection, null));
	}

	private MySqlConnection OpenConnectionWithRetry(string connectionString, string connectionName, int maxRetries = 3)
	{
		if (string.IsNullOrWhiteSpace(connectionString))
		{
			throw new ArgumentException("Connection string para " + connectionName + " es null o vacío");
		}
		Exception ex = null;
		int num = 0;
		while (num < maxRetries)
		{
			MySqlConnection mySqlConnection = null;
			try
			{
				num++;
				mySqlConnection = new MySqlConnection(connectionString);
				if (mySqlConnection == null)
				{
					throw new InvalidOperationException("MySqlConnection para " + connectionName + " es null");
				}
				mySqlConnection.Open();
				if (mySqlConnection.State != ConnectionState.Open)
				{
					throw new InvalidOperationException($"Conexión {connectionName} en estado {mySqlConnection.State}, esperaba Open");
				}
				using (MySqlCommand mySqlCommand = new MySqlCommand("SET NAMES utf8mb4 COLLATE utf8mb4_unicode_ci; SELECT 1", mySqlConnection))
				{
					mySqlCommand.CommandTimeout = 5;
					object obj = mySqlCommand.ExecuteScalar();
					if (obj == null || Convert.ToInt32(obj) != 1)
					{
						throw new InvalidOperationException("Test de conexión " + connectionName + " falló");
					}
				}
				return mySqlConnection;
			}
			catch (MySqlException ex2)
			{
				ex = ex2;
				try
				{
					mySqlConnection?.Close();
					mySqlConnection?.Dispose();
				}
				catch
				{
				}
				if ((ex2.Number != 1042 && ex2.Number != 1053 && ex2.Number != 1129 && ex2.Number != 2003 && ex2.Number != 2006 && ex2.Number != 2013) || num >= maxRetries)
				{
					break;
				}
				goto IL_01f8;
			}
			catch (ArgumentException ex3) when (ex3.Message.Contains("An item with the same key has already been added"))
			{
				ex = ex3;
				try
				{
					mySqlConnection?.Close();
					mySqlConnection?.Dispose();
				}
				catch
				{
				}
				goto IL_01f8;
			}
			catch (NullReferenceException ex4)
			{
				ex = ex4;
				try
				{
					mySqlConnection?.Close();
					mySqlConnection?.Dispose();
				}
				catch
				{
				}
				goto IL_01f8;
			}
			catch (Exception ex5)
			{
				ex = ex5;
				try
				{
					mySqlConnection?.Close();
					mySqlConnection?.Dispose();
				}
				catch
				{
				}
				goto IL_01f8;
			}
			IL_01f8:
			if (num < maxRetries)
			{
				Thread.Sleep(500 * num);
				try
				{
					MySqlConnection.ClearAllPools();
				}
				catch (Exception)
				{
				}
			}
		}
		throw new InvalidOperationException($"❌ No se pudo abrir conexión {connectionName} después de {maxRetries} intentos. Último error: {ex?.GetType().Name} - {ex?.Message}", ex);
	}

	public void Dispose()
	{
		if (_disposed)
		{
			return;
		}
		lock (_lockObject)
		{
			try
			{
				if (_defaultConnection != null)
				{
					try
					{
						_ = _defaultConnection.ServerThread;
						if (_defaultConnection.State == ConnectionState.Open)
						{
							_defaultConnection.Close();
						}
						_defaultConnection.Dispose();
					}
					catch (Exception)
					{
					}
				}
				if (_secondConnection != null)
				{
					try
					{
						_ = _secondConnection.ServerThread;
						if (_secondConnection.State == ConnectionState.Open)
						{
							_secondConnection.Close();
						}
						_secondConnection.Dispose();
					}
					catch (Exception)
					{
					}
				}
				if (_datosCargaInicialService.IsValueCreated && _datosCargaInicialService.Value is IDisposable disposable)
				{
					disposable.Dispose();
				}
			}
			catch (Exception)
			{
			}
			finally
			{
				_defaultConnection = null;
				_secondConnection = null;
				_disposed = true;
			}
		}
	}
}
