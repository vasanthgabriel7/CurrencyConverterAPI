namespace CurrencyConverterAPI.Models
{
    public class ExchangeRateResponse
    {
        public string Base { get; set; } = "EUR";
        public DateTime Date { get; set; }
        public Dictionary<string, decimal> Rates { get; set; } = new();
    }
}
