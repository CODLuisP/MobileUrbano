using System.Collections.Generic;
using System.Threading.Tasks;
using VelsatBackendAPI.Model;
using VelsatMobile.Model;

namespace VelsatBackendAPI.Data.Services;

public interface IDatosCargainicialService
{
	Task<DatosCargainicial> ObtenerDatosCargaInicialAsync(string login);

	Task<IEnumerable<SimplifiedDevice>> SimplifiedList(string login);

	Task<DatosVehiculo> ObtenerDatosVehiculoAsync(string login, string placa);
}
