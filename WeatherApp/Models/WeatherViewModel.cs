namespace WeatherApp.Models
{
    public class WeatherViewModel
    {
        public string City { get; set; }
        public double Temperature { get; set; }
        public string Description { get; set; }
        public int Humidity { get; set; }
        public string Icon { get; set; }
        public bool IsNight { get; set; }
        public string ErrorMessage { get; set; }

        public List<DailyForecast> Forecasts { get; set; } = new();

    }


}
