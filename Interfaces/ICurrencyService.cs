using CurrencyConverterAPI.Models;

namespace CurrencyConverterAPI.Interfaces
{
    public interface ICurrencyService
    {
        Task<ExchangeRate> GetLatestRatesAsync(string baseCurrency);
        Task<ConversionResponse> ConvertCurrencyAsync(ConversionRequest request);
        Task<IEnumerable<HistoricalRate>> GetHistoricalRatesAsync(string baseCurrency, DateTime startDate, DateTime endDate, int page, int pageSize);
    }

}
