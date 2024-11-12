# Statsig Azure AI

Azure AI library with a built-in Statsig SDK.

Statsig helps you move faster with Feature Gates (Feature Flags) and Dynamic Configs. It also allows you to run A/B tests to validate your new features and understand their impact on your KPIs. If you're new to Statsig, create an account at [statsig.com](https://www.statsig.com).

## Getting Started

1. Install the library `dotnet add package StatsigAzureAI`
2. Initialize the main AzureAI interface along with the internal Statsig service

```c#
using Statsig;
using Statsig.AzureAI;
var options = new StatsigOptions(environment: new StatsigEnvironment(EnvironmentTier.Development));
await Server.Initialize(<STATSIG_SERVER_KEY>, options);
```

3. Create the AzureAI inference client

```c#
var client = Server.GetModelClientFromEndpoint(
    <DEPLOYMENT_ENDPOINT_URL>,
    <DEPLOYMENT_KEY>
);
```

Optionally, use a Statsig Dynamic Config to provide default configurations

```c#
var client = Server.GetModelClient("gpt-4o-mini", <DEPLOYMENT_ENDPOINT_URL>, <DEPLOYMENT_KEY>);
```

4. Call the API

```c#
var completion = await client.Complete(
    "You are a helpful assistant that speaks like a pirate",
    "How do you train a parrot in 10 easy steps?"
);
```

## References

- Statsig SDK [documentation](https://docs.statsig.com/server/dotnetSDK/)
