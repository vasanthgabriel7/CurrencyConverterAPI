namespace CurrencyConverterAPI.Models
{
    public class ExchangeRate
    {
        public string BaseCurrency { get; set; }
        public Dictionary<string, decimal> Rates { get; set; }
    }

}
