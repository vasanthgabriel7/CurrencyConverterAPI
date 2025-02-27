namespace CurrencyConverterAPI.Models
{
    public class ConversionResponse
    {
        public decimal Amount { get; set; }
        public Dictionary<string, decimal> Rates { get; set; }
    }

}
