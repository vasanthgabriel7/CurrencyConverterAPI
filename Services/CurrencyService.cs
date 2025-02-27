using CurrencyConverterAPI.Interfaces;
using CurrencyConverterAPI.Models;

namespace CurrencyConverterAPI.Services
{
    public class CurrencyService : ICurrencyService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ICacheService _cacheService;
        private readonly ILogger<CurrencyService> _logger;

        public CurrencyService(
            IHttpClientFactory httpClientFactory,
            ICacheService cacheService,
            ILogger<CurrencyService> logger)
        {
            _httpClientFactory = httpClientFactory;
            _cacheService = cacheService;
            _logger = logger;
        }

        /// <summary>
        /// Retrieves the latest exchange rates for the specified base currency.
        /// It checks the cache first and if no data is found, fetches from the external API and caches the result.
        /// </summary>
        /// <param name="baseCurrency">The base currency for which exchange rates are required.</param>
        /// <returns>An <see cref="ExchangeRate"/> object containing the latest exchange rates for the base currency.</returns>
        public async Task<ExchangeRate> GetLatestRatesAsync(string baseCurrency)
        {
            var cacheKey = $"latest-{baseCurrency}";
            var cachedRates = await _cacheService.GetAsync<ExchangeRate>(cacheKey);

            if (cachedRates != null)
            {
                _logger.LogInformation("Cache hit: Retrieved latest exchange rates for {BaseCurrency}", baseCurrency);
                return cachedRates;
            }

            try
            {
                _logger.LogInformation("Cache miss: Fetching latest exchange rates for {BaseCurrency}", baseCurrency);

                var client = _httpClientFactory.CreateClient();
                var response = await client.GetAsync($"https://api.frankfurter.app/latest?base={baseCurrency}");

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("Invalid currency provided: {BaseCurrency}. API returned {StatusCode}",
                        baseCurrency, response.StatusCode);

                    if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                        throw new ArgumentException($"Invalid currency: '{baseCurrency}' is not a valid currency code.");

                    response.EnsureSuccessStatusCode();
                }

                var rates = await response.Content.ReadFromJsonAsync<ExchangeRate>();

                if (rates == null)
                {
                    _logger.LogWarning("No exchange rates found for {BaseCurrency}", baseCurrency);
                    throw new InvalidOperationException("No exchange rates found.");
                }

                await _cacheService.SetAsync(cacheKey, rates, TimeSpan.FromMinutes(5));
                _logger.LogInformation("Successfully cached latest exchange rates for {BaseCurrency}", baseCurrency);

                return rates;
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning("Invalid currency error: {Message}", ex.Message);
                throw;
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "HTTP error while fetching rates for {BaseCurrency}", baseCurrency);
                throw new Exception("Failed to retrieve exchange rates. Please try again later.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error fetching latest exchange rates for {BaseCurrency}", baseCurrency);
                throw new Exception("An unexpected error occurred.");
            }
        }

        /// <summary>
        /// Converts a specified amount from one currency to another using the latest exchange rates.
        /// </summary>
        /// <param name="request">A <see cref="ConversionRequest"/> object containing the amount and currencies involved in the conversion.</param>
        /// <returns>A <see cref="ConversionResponse"/> object containing the converted amount.</returns>
        public async Task<ConversionResponse> ConvertCurrencyAsync(ConversionRequest request)
        {
            _logger.LogInformation("Received currency conversion request: {Amount} {FromCurrency} to {ToCurrency}",
                request.Amount, request.FromCurrency, request.ToCurrency);

            request.FromCurrency = request.FromCurrency.ToUpper();
            request.ToCurrency = request.ToCurrency.ToUpper();

            // Check for unsupported currencies
            var unsupportedCurrencies = new[] { "TRY", "PLN", "THB", "MXN" };
            if (unsupportedCurrencies.Contains(request.FromCurrency) || unsupportedCurrencies.Contains(request.ToCurrency))
            {
                _logger.LogWarning("Unsupported currency in conversion request: {FromCurrency} or {ToCurrency}",
                    request.FromCurrency, request.ToCurrency);
                throw new ArgumentException("Currency not supported.");
            }

            try
            {
                var client = _httpClientFactory.CreateClient();
                var conversionResult = await client.GetFromJsonAsync<ConversionResponse>($"https://api.frankfurter.app/latest?base={request.FromCurrency}");

                if (conversionResult == null || !conversionResult.Rates.ContainsKey(request.ToCurrency))
                {
                    _logger.LogWarning("Conversion rate not found for {FromCurrency} to {ToCurrency}", request.FromCurrency, request.ToCurrency);
                    throw new InvalidOperationException("Conversion rate not found.");
                }

                var convertedAmount = request.Amount * conversionResult.Rates[request.ToCurrency];

                _logger.LogInformation("Successfully converted {Amount} {FromCurrency} to {ConvertedAmount} {ToCurrency}",
                    request.Amount, request.FromCurrency, convertedAmount, request.ToCurrency);

                return new ConversionResponse
                {
                    Amount = convertedAmount
                };
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning("Error occurred while converting currency: {Message}", ex.Message);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while converting currency from {FromCurrency} to {ToCurrency}", request.FromCurrency, request.ToCurrency);
                throw;
            }
        }

        /// <summary>
        /// Retrieves historical exchange rates for a specified base currency within a given date range.
        /// </summary>
        /// <param name="baseCurrency">The base currency for which historical rates are required.</param>
        /// <param name="startDate">The start date of the historical rates.</param>
        /// <param name="endDate">The end date of the historical rates.</param>
        /// <param name="page">The page number to retrieve (used for pagination).</param>
        /// <param name="pageSize">The number of records per page (used for pagination).</param>
        /// <returns>A list of <see cref="HistoricalRate"/> objects containing historical exchange rates for the specified period.</returns>
        public async Task<IEnumerable<HistoricalRate>> GetHistoricalRatesAsync(string baseCurrency, DateTime startDate, DateTime endDate, int page, int pageSize)
        {
            var cacheKey = $"historical-{baseCurrency}-{startDate:yyyy-MM-dd}-{endDate:yyyy-MM-dd}";
            var cachedRates = await _cacheService.GetAsync<IEnumerable<HistoricalRate>>(cacheKey);

            if (cachedRates != null)
            {
                _logger.LogInformation("Cache hit: Retrieved historical exchange rates for {BaseCurrency} from {StartDate} to {EndDate}",
                    baseCurrency, startDate, endDate);
                return cachedRates.Skip((page - 1) * pageSize).Take(pageSize);
            }

            try
            {
                _logger.LogInformation("Cache miss: Fetching historical exchange rates for {BaseCurrency} from {StartDate} to {EndDate}",
                    baseCurrency, startDate, endDate);

                var client = _httpClientFactory.CreateClient();
                var historicalRates = new List<HistoricalRate>();

                for (var date = startDate; date <= endDate; date = date.AddDays(1))
                {
                    var rate = await client.GetFromJsonAsync<HistoricalRate>($"https://api.frankfurter.app/{date:yyyy-MM-dd}?base={baseCurrency}");

                    if (rate != null)
                    {
                        historicalRates.Add(rate);
                    }
                    else
                    {
                        _logger.LogWarning("No exchange rate found for {BaseCurrency} on {Date}", baseCurrency, date);
                    }
                }

                if (!historicalRates.Any())
                {
                    _logger.LogWarning("No historical exchange rates found for {BaseCurrency} from {StartDate} to {EndDate}",
                        baseCurrency, startDate, endDate);
                    throw new InvalidOperationException("No historical exchange rates available.");
                }

                await _cacheService.SetAsync(cacheKey, historicalRates, TimeSpan.FromMinutes(10));
                _logger.LogInformation("Successfully cached historical exchange rates for {BaseCurrency}", baseCurrency);

                return historicalRates.Skip((page - 1) * pageSize).Take(pageSize);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while fetching historical exchange rates for {BaseCurrency} from {StartDate} to {EndDate}",
                    baseCurrency, startDate, endDate);
                throw;
            }
        }
    }
}
