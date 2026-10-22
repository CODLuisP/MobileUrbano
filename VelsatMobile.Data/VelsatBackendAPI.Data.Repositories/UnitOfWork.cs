using System;
using System.Data;
using System.Threading;
using MySql.Data.MySqlClient;
using VelsatBackendAPI.Data.Services;
using VelsatMobile.Data.Repositories;

namespace VelsatBackendAPI.Data.Repositories;

public class UnitOfWork : IUnitOfWork, IDisposable
{
	private readonly string _defaultConnectionString;

	private readonly string _secondConnectionString;

	private MySqlConnection _defaultConnection;

	private MySqlTransaction _defaultTransaction;

	private MySqlConnection _secondConnection;

	private MySqlTransaction _secondTransaction;

	private readonly Lazy<IUserRepository> _userRepository;

	private readonly Lazy<IDatosCargainicialService> _datosCargaInicialService;

	private readonly Lazy<IHistoricosRepository> _historicosRepository;

	private readonly Lazy<IKilometrosRepository> _kilometrosRepository;

	private readonly Lazy<IServidorRepository> _servidorRepository;

	private readonly Lazy<IAplicativoRepository> _aplicativoRepository;

	private bool _disposed;

	private bool _committed;

	private readonly object _lockObject = new object();

	private MySqlConnection DefaultConnection
	{
		get
		{
			ValidateNotDisposedOrCommitted();
			if (_defaultConnection == null)
			{
				lock (_lockObject)
				{
					if (_defaultConnection == null)
					{
						_defaultConnection = OpenConnectionWithRetry(_defaultConnectionString, "DEFAULT (con transacción)");
						_defaultTransaction = _defaultConnection.BeginTransaction();
					}
				}
			}
			return _defaultConnection;
		}
	}

	private MySqlConnection SecondConnection
	{
		get
		{
			ValidateNotDisposedOrCommitted();
			if (_secondConnection == null)
			{
				lock (_lockObject)
				{
					if (_secondConnection == null)
					{
						_secondConnection = OpenConnectionWithRetry(_secondConnectionString, "SECOND (con transacción)");
						_secondTransaction = _secondConnection.BeginTransaction();
					}
				}
			}
			return _secondConnection;
		}
	}

	public IUserRepository UserRepository
	{
		get
		{
			ValidateNotDisposedOrCommitted();
			return _userRepository.Value;
		}
	}

	public IDatosCargainicialService DatosCargainicialService
	{
		get
		{
			ValidateNotDisposedOrCommitted();
			return _datosCargaInicialService.Value;
		}
	}

	public IServidorRepository ServidorRepository
	{
		get
		{
			ValidateNotDisposedOrCommitted();
			return _servidorRepository.Value;
		}
	}

	public IAplicativoRepository AplicativoRepository
	{
		get
		{
			ValidateNotDisposedOrCommitted();
			return _aplicativoRepository.Value;
		}
	}

	public IHistoricosRepository HistoricosRepository
	{
		get
		{
			ValidateNotDisposedOrCommitted();
			return _historicosRepository.Value;
		}
	}

	public IKilometrosRepository KilometrosRepository
	{
		get
		{
			ValidateNotDisposedOrCommitted();
			return _kilometrosRepository.Value;
		}
	}

	public UnitOfWork(MySqlConfiguration configuration)
	{
		_defaultConnectionString = configuration.DefaultConnection ?? throw new ArgumentNullException("DefaultConnection");
		_secondConnectionString = configuration.SecondConnection ?? throw new ArgumentNullException("SecondConnection");
		_userRepository = new Lazy<IUserRepository>(() => new UserRepository(DefaultConnection, _defaultTransaction));
		_datosCargaInicialService = new Lazy<IDatosCargainicialService>(() => new DatosCargainicialService(DefaultConnection, _defaultTransaction));
		_historicosRepository = new Lazy<IHistoricosRepository>(() => new HistoricosRepository(DefaultConnection, SecondConnection, _defaultTransaction, _secondTransaction));
		_kilometrosRepository = new Lazy<IKilometrosRepository>(() => new KilometrosRepository(DefaultConnection, SecondConnection, _defaultTransaction, _secondTransaction));
		_servidorRepository = new Lazy<IServidorRepository>(() => new ServidorRepository(DefaultConnection, _defaultTransaction));
		_aplicativoRepository = new Lazy<IAplicativoRepository>(() => new AplicativoRepository(DefaultConnection, _defaultTransaction));
	}

	private void ValidateNotDisposedOrCommitted()
	{
		if (_disposed)
		{
			throw new ObjectDisposedException("UnitOfWork", "No se puede usar un UnitOfWork que ya ha sido liberado. Crea una nueva instancia.");
		}
		if (_committed)
		{
			throw new InvalidOperationException("Este UnitOfWork ya fue confirmado con SaveChanges(). Crea una nueva instancia para realizar más operaciones.");
		}
	}

	private MySqlConnection OpenConnectionWithRetry(string connectionString, string connectionName, int maxRetries = 5)
	{
		Exception innerException = null;
		for (int i = 0; i < maxRetries; i++)
		{
			try
			{
				MySqlConnection mySqlConnection = new MySqlConnection(connectionString);
				mySqlConnection.Open();
				using (MySqlCommand mySqlCommand = new MySqlCommand("SET NAMES utf8mb4 COLLATE utf8mb4_unicode_ci", mySqlConnection))
				{
					mySqlCommand.ExecuteNonQuery();
				}
				return mySqlConnection;
			}
			catch (ArgumentException ex) when (ex.Message.Contains("An item with the same key has already been added"))
			{
				innerException = ex;
				if (i < maxRetries - 1)
				{
					Thread.Sleep(10 * (int)Math.Pow(2.0, i));
					try
					{
						MySqlConnection.ClearPool(new MySqlConnection(connectionString));
					}
					catch
					{
					}
				}
			}
			catch (Exception)
			{
				throw;
			}
		}
		throw new InvalidOperationException($"No se pudo abrir la conexión {connectionName} después de {maxRetries} intentos. Pool de conexiones MySQL posiblemente corrupto.", innerException);
	}

	public void SaveChanges()
	{
		ValidateNotDisposedOrCommitted();
		lock (_lockObject)
		{
			try
			{
				_defaultTransaction?.Commit();
				_secondTransaction?.Commit();
				_committed = true;
			}
			catch
			{
				try
				{
					_defaultTransaction?.Rollback();
				}
				catch
				{
				}
				try
				{
					_secondTransaction?.Rollback();
				}
				catch
				{
				}
				throw;
			}
			finally
			{
				DisposeTransactionsAndConnections();
			}
		}
	}

	private void DisposeTransactionsAndConnections()
	{
		if (_defaultTransaction != null)
		{
			_defaultTransaction.Dispose();
			_defaultTransaction = null;
		}
		if (_secondTransaction != null)
		{
			_secondTransaction.Dispose();
			_secondTransaction = null;
		}
		if (_defaultConnection != null)
		{
			try
			{
				if (_defaultConnection.State == ConnectionState.Open)
				{
					_defaultConnection.Close();
				}
				_defaultConnection.Dispose();
			}
			catch
			{
			}
			finally
			{
				_defaultConnection = null;
			}
		}
		if (_secondConnection == null)
		{
			return;
		}
		try
		{
			if (_secondConnection.State == ConnectionState.Open)
			{
				_secondConnection.Close();
			}
			_secondConnection.Dispose();
		}
		catch
		{
		}
		finally
		{
			_secondConnection = null;
		}
	}

	public void Dispose()
	{
		Dispose(disposing: true);
	}

	protected virtual void Dispose(bool disposing)
	{
		if (_disposed || !disposing)
		{
			return;
		}
		lock (_lockObject)
		{
			try
			{
				if (!_committed)
				{
					try
					{
						if (_defaultTransaction != null && _defaultTransaction.Connection != null)
						{
							_defaultTransaction.Rollback();
						}
					}
					catch (Exception)
					{
					}
					try
					{
						if (_secondTransaction != null && _secondTransaction.Connection != null)
						{
							_secondTransaction.Rollback();
						}
					}
					catch (Exception)
					{
					}
				}
				DisposeTransactionsAndConnections();
				DisposeRepositories();
			}
			catch (Exception)
			{
			}
			finally
			{
				_disposed = true;
			}
		}
	}

	private void DisposeRepositories()
	{
		TryDisposeRepository(_userRepository);
		TryDisposeRepository(_datosCargaInicialService);
		TryDisposeRepository(_historicosRepository);
		TryDisposeRepository(_kilometrosRepository);
		TryDisposeRepository(_servidorRepository);
		TryDisposeRepository(_aplicativoRepository);
	}

	private void TryDisposeRepository<T>(Lazy<T> lazyRepo)
	{
		if (lazyRepo != null && lazyRepo.IsValueCreated && lazyRepo.Value is IDisposable disposable)
		{
			try
			{
				disposable.Dispose();
			}
			catch
			{
			}
		}
	}
}
