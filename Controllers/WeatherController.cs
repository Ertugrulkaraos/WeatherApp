using Microsoft.AspNetCore.Mvc;
using WeatherApp.Models;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using System.Linq;
using System;

namespace WeatherApp.Controllers
{
    public class WeatherController : Controller
    {
        private readonly IConfiguration _configuration;
        private readonly HttpClient _httpClient = new HttpClient();

        public WeatherController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        // =========================
        // NORMAL SAYFA (MVC)
        // =========================
        [HttpGet]
        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Index(string city)
        {
            if (string.IsNullOrWhiteSpace(city))
            {
                return View(new WeatherViewModel
                {
                    ErrorMessage = "Lütfen bir şehir adı gir."
                });
            }

            string apiKey = _configuration["WeatherApi:ApiKey"];

            string weatherUrl =
                $"https://api.openweathermap.org/data/2.5/weather?q={city}&units=metric&lang=tr&appid={apiKey}";

            string forecastUrl =
                $"https://api.openweathermap.org/data/2.5/forecast?q={city}&units=metric&lang=tr&appid={apiKey}";

            // === Anlık hava ===
            var response = await _httpClient.GetAsync(weatherUrl);
            if (!response.IsSuccessStatusCode)
            {
                return View(new WeatherViewModel
                {
                    ErrorMessage = "Şehir bulunamadı"
                });
            }

            var json = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);

            var icon = doc.RootElement
                .GetProperty("weather")[0]
                .GetProperty("icon")
                .GetString();

            var model = new WeatherViewModel
            {
                City = doc.RootElement.GetProperty("name").GetString(),
                Temperature = doc.RootElement.GetProperty("main").GetProperty("temp").GetDouble(),
                Humidity = doc.RootElement.GetProperty("main").GetProperty("humidity").GetInt32(),
                Description = doc.RootElement.GetProperty("weather")[0].GetProperty("description").GetString(),
                Icon = icon,
                IsNight = icon.EndsWith("n")
            };

            // === 5 Günlük tahmin ===
            var forecastResponse = await _httpClient.GetAsync(forecastUrl);

            if (forecastResponse.IsSuccessStatusCode)
            {
                var forecastJson = await forecastResponse.Content.ReadAsStringAsync();
                using var forecastDoc = JsonDocument.Parse(forecastJson);

                var list = forecastDoc.RootElement.GetProperty("list");

                var dailyItems = list.EnumerateArray()
                    .Where(x => x.GetProperty("dt_txt").GetString().Contains("12:00:00"))
                    .Take(5);

                foreach (var item in dailyItems)
                {
                    model.Forecasts.Add(new DailyForecast
                    {
                        Date = DateTime.Parse(item.GetProperty("dt_txt").GetString())
                                       .ToString("dd MMM"),
                        Temp = item.GetProperty("main").GetProperty("temp").GetDouble(),
                        Icon = item.GetProperty("weather")[0].GetProperty("icon").GetString()
                    });
                }
            }

            return View(model);
        }

        // =========================
        // AJAX (SAYFA YENİLENMEZ)
        // =========================
        [HttpPost]
        public async Task<IActionResult> AjaxWeather(string city)
        {
            if (string.IsNullOrWhiteSpace(city))
                return BadRequest();

            string apiKey = _configuration["WeatherApi:ApiKey"];
            string url =
                $"https://api.openweathermap.org/data/2.5/weather?q={city}&units=metric&lang=tr&appid={apiKey}";

            var response = await _httpClient.GetAsync(url);
            if (!response.IsSuccessStatusCode)
                return BadRequest();

            var json = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);

            var model = new WeatherViewModel
            {
                City = doc.RootElement.GetProperty("name").GetString(),
                Temperature = doc.RootElement.GetProperty("main").GetProperty("temp").GetDouble(),
                Humidity = doc.RootElement.GetProperty("main").GetProperty("humidity").GetInt32(),
                Description = doc.RootElement.GetProperty("weather")[0].GetProperty("description").GetString(),
                Icon = doc.RootElement.GetProperty("weather")[0].GetProperty("icon").GetString()
            };

            return PartialView("_WeatherResult", model);
        }
    }
}
