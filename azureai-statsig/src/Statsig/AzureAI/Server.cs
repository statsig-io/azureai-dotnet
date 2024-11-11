using Statsig.Server;

namespace Statsig.AzureAI;


public static class Server
{
  public static async Task<InitializeResult> Initialize(
    string statsigServerKey,
    StatsigOptions? options = null
  ) {
    return await StatsigServer.Initialize(statsigServerKey, options);
  }

  public static ModelClient GetModelClientFromEndpoint(
    string endpoint,
    string apiKey
  ) {
    return new ModelClient(endpoint, apiKey);
  }

  public static ModelClient GetModelClient(
    string dynamicConfigId,
    string? defaultEndpoint = null,
    string? defaultApiKey = null
  ) {
    var user = Utils.GetStatsigUser();
    var config = StatsigServer.GetConfigSync(user, dynamicConfigId);
    var endpoint = config.Get("endpoint", defaultEndpoint);
    var apiKey = config.Get("key", defaultApiKey);
    var defaults = config.Get(
      "completion_defaults",
      new Dictionary<string, string>()
    );

    if (string.IsNullOrEmpty(endpoint) || string.IsNullOrEmpty(apiKey)) {
      throw new InvalidOperationException(
        "Endpoint and API key are not specified in the config"
      );
    }

    return new ModelClient(endpoint, apiKey, defaults);
  }

  public static ServerDriver? GetStatsigServer() {
    return StatsigServer._singleDriver;
  }

  public static async Task Shutdown() {
    await StatsigServer.Shutdown();
  }
}
