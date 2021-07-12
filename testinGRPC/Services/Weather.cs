using Google.Protobuf.WellKnownTypes;

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
    private readonly string API_KEY;
    private readonly HttpClient httpClient;

    public Weather(IHttpClientFactory httpClientFactory, IConfiguration config)
    {
      _config = config;
      _httpClientFactory = httpClientFactory;
      API_KEY = _config["_OpenWeatherAPI"]; // shhh, it's a secret!
      httpClient = _httpClientFactory.CreateClient();

    }
    // Unary
    public override async Task<WeatherResponse> GetCurrentWeather(GetCurrentWeatherForCityRequest request, ServerCallContext context)
    {
      var temperatures = await GetCurrentTemperaturesAsync(request);

      return new WeatherResponse
      {
        Temperature = temperatures!.Main.Temp,
        FeelsLike = temperatures.Main.FeelsLike,
        TimeStamp = Timestamp.FromDateTime(DateTime.UtcNow),
        City = request.City,
        Units = request.Units
      };
    }
    // Server Streaming
    public override async Task GetCurrentWeatherStream(GetCurrentWeatherForCityRequest request, IServerStreamWriter<WeatherResponse> responseStream, ServerCallContext context)
    {
      for (int i = 0; i < 30; i++)
      {
        if (context.CancellationToken.IsCancellationRequested)
          break; // can add a logger here but don't wanna bother...
        var temperatures = await GetCurrentTemperaturesAsync(request);
        await responseStream.WriteAsync(new WeatherResponse
        {
          Temperature = temperatures!.Main.Temp,
          FeelsLike = temperatures.Main.FeelsLike,
          TimeStamp = Timestamp.FromDateTime(DateTime.UtcNow),
          City = request.City,
          Units = request.Units
        });
        await Task.Delay(1000);
      }
    }

    // Client Streaming
    public override async Task<MultiWeatherWeatherResponse> GetMultiCurrentWeatherStream(IAsyncStreamReader<GetCurrentWeatherForCityRequest> requestStream, ServerCallContext context)
    {
      var response = new MultiWeatherWeatherResponse
      {
        Weather = { }
      };

      await foreach (var request in requestStream.ReadAllAsync())
      {
        var temperatures = await GetCurrentTemperaturesAsync(request);
        response.Weather.Add(new WeatherResponse
        {
          Temperature = temperatures!.Main.Temp,
          FeelsLike = temperatures.Main.FeelsLike,
          TimeStamp = Timestamp.FromDateTime(DateTime.UtcNow),
          City = request.City,
          Units = request.Units
        });
      }
      return response;

    }
  

    private async Task<Temprature> GetCurrentTemperaturesAsync(GetCurrentWeatherForCityRequest request)
    {
      var responseText = await httpClient.GetStringAsync($"http://api.openweathermap.org/data/2.5/weather?q={request.City}&appid={API_KEY}&units={request.Units}");

      var temperatures = JsonSerializer.Deserialize<Temprature>(responseText);
      return temperatures;
    }
  }
}
