namespace Statsig.AzureAI;

public static class Utils {
  public static StatsigUser GetStatsigUser(StatsigUser? user = null) {
    var newUser = user ?? new StatsigUser();
    newUser.AddCustomID("sdk_type", "azureai-dotnet");
    return newUser;
  }
}