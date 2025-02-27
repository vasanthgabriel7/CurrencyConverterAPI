using CurrencyConverterAPI.Interfaces;
using CurrencyConverterAPI.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CurrencyConverterAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [ApiVersion("1.0")]
    [Authorize]
    public class CurrencyController : ControllerBase
    {
        private readonly ICurrencyService _currencyService;
        private readonly ILogger<CurrencyController> _logger;

        public CurrencyController(ICurrencyService currencyService, ILogger<CurrencyController> logger)
        {
            _currencyService = currencyService;
            _logger = logger;
        }

        /// <summary>
        /// Endpoint to retrieve the latest exchange rates for a specified base currency.
        /// This endpoint is accessible to both User and Admin roles.
        /// </summary>
        /// <param name="baseCurrency">The base currency for which exchange rates are requested.</param>
        /// <returns>An <see cref="IActionResult"/> containing the exchange rates or an error message.</returns>
        [HttpGet("latest/{baseCurrency}")]
        [Authorize(Roles = "User,Admin")]  // Allow access to both User and Admin roles
        public async Task<IActionResult> GetLatestExchangeRates(string baseCurrency)
        {
            try
            {
                var rates = await _currencyService.GetLatestRatesAsync(baseCurrency);
                return Ok(rates);
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning("Bad request: {Message}", ex.Message);
                return BadRequest(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError("Internal server error: {Message}", ex.Message);
                return StatusCode(500, new { error = "An error occurred while fetching exchange rates." });
            }
        }

        /// <summary>
        /// Endpoint to convert a specified amount from one currency to another.
        /// This endpoint is accessible to Admin role only.
        /// </summary>
        /// <param name="request">The conversion request containing the amount and currencies involved in the conversion.</param>
        /// <returns>An <see cref="IActionResult"/> containing the conversion result or an error message.</returns>
        [HttpPost("convert")]
        [Authorize(Roles = "Admin")]  // Allow access only to Admin role
        public async Task<IActionResult> ConvertCurrency([FromBody] ConversionRequest request)
        {
            try
            {
                _logger.LogInformation("Received currency conversion request: {Amount} {FromCurrency} to {ToCurrency}",
                    request.Amount, request.FromCurrency, request.ToCurrency);

                var result = await _currencyService.ConvertCurrencyAsync(request);
                _logger.LogInformation("Conversion successful: {Amount} {FromCurrency} to {ToCurrency} = {ConvertedAmount}",
                    request.Amount, request.FromCurrency, request.ToCurrency, result.Amount);

                return Ok(result);
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning("Currency conversion failed for {FromCurrency} to {ToCurrency}: {Message}",
                    request.FromCurrency, request.ToCurrency, ex.Message);
                return BadRequest(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Internal server error during currency conversion for {FromCurrency} to {ToCurrency}",
                    request.FromCurrency, request.ToCurrency);
                return StatusCode(500, new { error = "An error occurred while converting currency." });
            }
        }

        /// <summary>
        /// Endpoint to retrieve historical exchange rates for a specified base currency within a date range.
        /// This endpoint is accessible to Admin role only.
        /// </summary>
        /// <param name="baseCurrency">The base currency for which historical rates are requested.</param>
        /// <param name="startDate">The start date of the historical rates.</param>
        /// <param name="endDate">The end date of the historical rates.</param>
        /// <param name="page">The page number to retrieve (for pagination).</param>
        /// <param name="pageSize">The number of records per page (for pagination).</param>
        /// <returns>An <see cref="IActionResult"/> containing the historical exchange rates or an error message.</returns>
        [HttpGet("history")]
        [Authorize(Roles = "Admin")]  // Allow access only to Admin role
        public async Task<IActionResult> GetHistoricalRates([FromQuery] string baseCurrency, [FromQuery] DateTime startDate, [FromQuery] DateTime endDate, [FromQuery] int page = 1, [FromQuery] int pageSize = 50)
        {
            try
            {
                _logger.LogInformation("Received request for historical exchange rates for {BaseCurrency} from {StartDate} to {EndDate}",
                    baseCurrency, startDate, endDate);

                if (startDate > endDate)
                {
                    _logger.LogWarning("Start date cannot be later than end date: {StartDate} > {EndDate}", startDate, endDate);
                    return BadRequest(new { error = "Start date cannot be later than end date." });
                }

                var historicalRates = await _currencyService.GetHistoricalRatesAsync(baseCurrency, startDate, endDate, page, pageSize);

                if (historicalRates == null || !historicalRates.Any())
                {
                    _logger.LogWarning("No historical data found for {BaseCurrency} from {StartDate} to {EndDate}",
                        baseCurrency, startDate, endDate);
                    return NotFound(new { error = "No historical data found for the given dates." });
                }

                _logger.LogInformation("Successfully fetched {Count} historical exchange rates for {BaseCurrency} from {StartDate} to {EndDate}",
                    historicalRates.Count(), baseCurrency, startDate, endDate);

                return Ok(historicalRates);
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning("Bad request for historical rates: {Message}", ex.Message);
                return BadRequest(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Internal server error while fetching historical exchange rates for {BaseCurrency} from {StartDate} to {EndDate}",
                    baseCurrency, startDate, endDate);
                return StatusCode(500, new { error = "An error occurred while fetching historical rates." });
            }
        }
    }
}
