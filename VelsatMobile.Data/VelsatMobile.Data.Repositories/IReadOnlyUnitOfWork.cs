using System;
using VelsatBackendAPI.Data.Repositories;
using VelsatBackendAPI.Data.Services;

namespace VelsatMobile.Data.Repositories;

public interface IReadOnlyUnitOfWork : IDisposable
{
	IDatosCargainicialService DatosCargainicialService { get; }

	IServidorRepository ServidorRepository { get; }

	IHistoricosRepository HistoricosRepository { get; }

	IKilometrosRepository KilometrosRepository { get; }

	IUserRepository UserRepository { get; }

	IAplicativoRepository AplicativoRepository { get; }
}
