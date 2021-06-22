using Google.Protobuf.WellKnownTypes;

using Grpc.Core;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

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
    private readonly ILogger<Weather> _logger;
    private readonly IConfiguration _config;
    private readonly string API_KEY;
    public Weather(IHttpClientFactory httpClientFactory, IConfiguration config, ILogger<Weather> logger)
    {
      _logger = logger;
      _config = config;
      _httpClientFactory = httpClientFactory;
      API_KEY = _config["_OpenWeatherAPI"];

    }

    public override async Task<WeatherResponse> GetCurrentWeather(GetCurrentWeatherRequest request, ServerCallContext context)
    {
      var httpClient = _httpClientFactory.CreateClient();
      Temprature temperatures = await GetCurrentWeeatherAsunc(request, API_KEY, httpClient);

      return new WeatherResponse 
      {
        Temperature = temperatures!.Main.Temp,
        FeelsLike = temperatures.Main.FeelsLike,
        Time = Timestamp.FromDateTime(DateTime.UtcNow),
        City = request.City
      };
    }

    public override async Task GetCurrentWeatherStream(GetCurrentWeatherRequest request, IServerStreamWriter<WeatherResponse> responseStream, ServerCallContext context)
    {
      var httpClient = _httpClientFactory.CreateClient();
      for (var i = 0; i < 30; i++)
      {
        if (context.CancellationToken.IsCancellationRequested)
        {
          _logger.LogInformation("Canceled by the user");
          break;
        }
        Temprature temperatures = await GetCurrentWeeatherAsunc(request, API_KEY, httpClient);

        await responseStream.WriteAsync(
          new WeatherResponse
          {
            Temperature = temperatures!.Main.Temp,
            FeelsLike = temperatures.Main.FeelsLike,
            Time = Timestamp.FromDateTime(DateTime.UtcNow),
            City = request.City
          }) ;
            await Task.Delay(1000);
      }
 
    }



    public override async Task<MultiWeatherResponse> GetMultiCurrentWeatherStream(IAsyncStreamReader<GetCurrentWeatherForCityRequest> requestStream, ServerCallContext context)
    {
      var httpClient = _httpClientFactory.CreateClient();
      var response = new MultiWeatherResponse
      {
        Weather = { }
      };
      await foreach (var request in requestStream.ReadAllAsync())
      {
        var temperatures = await GetCurrentWeeatherAsunc(request, API_KEY, httpClient);
        response.Weather.Add(
          new WeatherResponse
          {
            Temperature = temperatures!.Main.Temp,
            FeelsLike = temperatures.Main.FeelsLike,
            Time = Timestamp.FromDateTime(DateTime.UtcNow),
            City = request.City
          });
      }
      return response;
    }

    private async Task<Temprature> GetCurrentWeeatherAsunc(GetCurrentWeatherForCityRequest request, string aPI_KEY, HttpClient httpClient)
    {
      var responseText = await httpClient.GetStringAsync($"http://api.openweathermap.org/data/2.5/weather?q={request.City}&appid={API_KEY}&units={request.Units}");

      var temperatures = JsonSerializer.Deserialize<Temprature>(responseText);
      return temperatures;
    }

    private static async Task<Temprature> GetCurrentWeeatherAsunc(GetCurrentWeatherRequest request, string API_KEY, HttpClient httpClient)
    {
      var responseText = await httpClient.GetStringAsync($"http://api.openweathermap.org/data/2.5/weather?q={request.City}&appid={API_KEY}&units={request.Units}");

      var temperatures = JsonSerializer.Deserialize<Temprature>(responseText);
      return temperatures;
    }
  }
}
