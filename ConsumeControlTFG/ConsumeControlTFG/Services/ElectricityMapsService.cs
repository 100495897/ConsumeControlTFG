using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using ConsumeControlTFG.Models;

namespace ConsumeControlTFG.Services
{
    public class ElectricityMapsService
    {
        private readonly HttpClient _httpClient = new HttpClient();
        private string ApiKey = System.Configuration.ConfigurationManager.AppSettings["ElectricityMapsApiKey"] ?? string.Empty;
        public string ZonaActual { get; set; } = "ES";

        public async Task<ResultadoEmisiones> GetCarbonIntensityAsync()
        {
            //Gestiona la llamada a la API externa para obtener el gCO2/kWh actual de la zona seleccionada
            if (string.IsNullOrWhiteSpace(ApiKey) || ApiKey == "TU_CLAVE_API")
            {
                return new ResultadoEmisiones { Valor = 434.0, EsReal = false };
            }

            try
            {
                string url = $"https://api.electricitymap.org/v3/carbon-intensity/latest?zone={ZonaActual}";
                _httpClient.DefaultRequestHeaders.Clear();
                _httpClient.DefaultRequestHeaders.Add("auth-token", ApiKey);

                var response = await _httpClient.GetFromJsonAsync<CarbonIntensityResponse>(url);

                if (response != null && response.CarbonIntensity > 0)
                {
                    return new ResultadoEmisiones { Valor = response.CarbonIntensity, EsReal = true };
                }
            }
            catch { /* Error de red o API Key inválida */ }

            return new ResultadoEmisiones { Valor = 434.0, EsReal = false };
        }
    }

    public class ResultadoEmisiones
    {
        public double Valor { get; set; }
        public bool EsReal { get; set; }
    }

}