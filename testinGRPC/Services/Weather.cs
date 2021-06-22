using Grpc.Core;

using Microsoft.Extensions.Configuration;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

using testinGRPC.Contracts;
using testinGRPC.Protos;

namespace testinGRPC.Services
{
  public class Weather : WeatherService.WeatherServiceBase
  {
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _config;
    public Weather(IHttpClientFactory httpClientFactory, IConfiguration config)
    {
      _config = config;
      _httpClientFactory = httpClientFactory;
    }

    public override async Task<WeatherResponse> GetCurrentWeather(GetCurrentWeatherRequest request, ServerCallContext context)
    {
      var API_KEY = _config["_OpenWeatherAPI"];
      var httpClient = _httpClientFactory.CreateClient();
      var responseText =await httpClient.GetStringAsync($"http://api.openweathermap.org/data/2.5/weather?q={request.City}&appid={API_KEY}&units={request.Units}");

      var temperatures = JsonSerializer.Deserialize<Temprature>(responseText);

      return new WeatherResponse{
        Temperature = temperatures!.Main.Temp,
        FeelsLike = temperatures.Main.FeelsLike
      };
    }
  }
}
