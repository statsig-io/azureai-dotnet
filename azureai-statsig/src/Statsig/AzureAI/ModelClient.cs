using System.Net;
using Azure;
using Azure.AI.Inference;
using Statsig.Server;

namespace Statsig.AzureAI;

public class ModelClient
{
  private Uri apiEndpoint;
  private AzureKeyCredential keyCredential;
  private Dictionary<string, string>? defaults;
  private ChatCompletionsClient? completionsClient;
  private EmbeddingsClient? embeddingsClient;

  public ModelClient(
    string endpoint,
    string apiKey,
    Dictionary<string, string>? defaults = null
  ) {
    this.apiEndpoint = new Uri(endpoint);
    this.keyCredential = new AzureKeyCredential(apiKey);
    this.defaults = defaults;
  }

  public async Task<string> Complete(
    string systemMessage,
    string userMessage,
    StatsigUser? user = null
  ) {
    var messages = new List<ChatRequestMessage> {
      new ChatRequestSystemMessage(systemMessage),
      new ChatRequestUserMessage(userMessage)
    };
    return await this.Complete(messages);
  }

  public async Task<string> Complete(
    IList<ChatRequestMessage> messages,
    StatsigUser? user = null
  ) {
    var options = GetDefaultOptions();
    options.Messages = messages;

    var ic = LogInvoke(user, "complete");
    var response = await this.GetCompletionsClient().CompleteAsync(options);
    var rawResponse = response.GetRawResponse();
    if (rawResponse.Status != (int)HttpStatusCode.OK) {
      throw new InvalidOperationException(
        $"Failed to complete the chat request: {rawResponse.ReasonPhrase}"
      );
    }
    
    var value = response.Value;
    LogUsage(
      user,
      "complete",
      new Dictionary<string, string> {
        { "model", value.Model },
        { "completion_tokens", value.Usage.CompletionTokens.ToString() },
        { "prompt_tokens", value.Usage.PromptTokens.ToString() },
        { "total_tokens", value.Usage.TotalTokens.ToString() },
        { "created", value.Created.ToString() },
      },
      ic
    );
    return response.Value.Content;
  }

  public async Task<IAsyncEnumerable<StreamingChatCompletionsUpdate>> StreamComplete(
    string systemMessage,
    string userMessage,
    StatsigUser? user = null
  ) {
    var messages = new List<ChatRequestMessage> {
      new ChatRequestSystemMessage(systemMessage),
      new ChatRequestUserMessage(userMessage)
    };
    return await this.StreamComplete(messages);
  }

  public async Task<IAsyncEnumerable<StreamingChatCompletionsUpdate>> StreamComplete(
    IList<ChatRequestMessage> messages,
    StatsigUser? user = null
  ) {
    var options = GetDefaultOptions();
    options.Messages = messages;

    var ic = LogInvoke(user, "stream");
    var response = await this.GetCompletionsClient().CompleteStreamingAsync(options);
    var rawResponse = response.GetRawResponse();
    if (rawResponse.Status != (int)HttpStatusCode.OK) {
      throw new InvalidOperationException(
        $"Failed to stream the chat request: {rawResponse.ReasonPhrase}"
      );
    }
    
    LogUsage(user, "stream_begin", null, ic);
    
    // TODO: Log stream_end event
    return response.EnumerateValues();
  }

  public async Task<ModelInfo> GetModelInfo(StatsigUser? user = null) {
    var ic = LogInvoke(user, "getInfo");
    var response = await this.GetCompletionsClient().GetModelInfoAsync();
    var rawResponse = response.GetRawResponse();
    if (rawResponse.Status != (int)HttpStatusCode.OK) {
      throw new InvalidOperationException(
        $"Failed to get the model info: {rawResponse.ReasonPhrase}"
      );
    }

    var value = response.Value;
    LogUsage(
      user,
      "get_model_info",
      new Dictionary<string, string> {
        { "model_name", value.ModelName },
        { "model_provider_name", value.ModelProviderName },
        { "model_type", value.ModelType.ToString() },
      },
      ic
    );
    return response.Value;
  }

  public async Task<IEnumerable<BinaryData>> GetEmbeddings(
    IEnumerable<string> input,
    StatsigUser? user = null
  ) {
    var options = new EmbeddingsOptions(input);

    var ic = LogInvoke(user, "getEmbeddings");
    var response = await this.GetEmbeddingsClient().EmbedAsync(options);
    var rawResponse = response.GetRawResponse();
    if (rawResponse.Status != (int)HttpStatusCode.OK) {
      throw new InvalidOperationException(
        $"Failed to embed the text: {rawResponse.ReasonPhrase}"
      );
    }
    
    var value = response.Value;
    LogUsage(
      user,
      "getEmbeddings",
      new Dictionary<string, string> {
        { "model", value.Model },
        { "prompt_tokens", value.Usage.PromptTokens.ToString() },
        { "total_tokens", value.Usage.TotalTokens.ToString() },
        { "embedding_length", value.Data.Count.ToString() },
      },
      ic
    );
    var data = response.Value.Data;
    return data.Select(d => d.Embedding);
  }

  private ChatCompletionsClient GetCompletionsClient() {
    if (this.completionsClient == null) {
      this.completionsClient = new ChatCompletionsClient(
        this.apiEndpoint,
        this.keyCredential
      );
    }
    return this.completionsClient;
  }

  private EmbeddingsClient GetEmbeddingsClient() {
    if (this.embeddingsClient == null) {
      this.embeddingsClient = new EmbeddingsClient(
        this.apiEndpoint,
        this.keyCredential
      );
    }
    return this.embeddingsClient;
  }

  private ChatCompletionsOptions GetDefaultOptions() {
    var options = new ChatCompletionsOptions();
    if (this.defaults == null) {
      return options;
    }

    foreach (var (key, value) in this.defaults) {
      switch (key) {
        case "temperature":
          var temperature = 0.0f;
          if (float.TryParse(value, out temperature)) {
            options.Temperature = temperature;
          }
          break;
        case "max_tokens":
          var maxTokens = 0;
          if (int.TryParse(value, out maxTokens) && maxTokens > 0) {
            options.MaxTokens = maxTokens;
          }
          break;
        case "top_p":
          var topP = 0.0f;
          if (float.TryParse(value, out topP)) {
            options.NucleusSamplingFactor = topP;
          }
          break;
        case "frequency_penalty":
          var frequencyPenalty = 0.0f;
          if (float.TryParse(value, out frequencyPenalty)) {
            options.FrequencyPenalty = frequencyPenalty;
          }
          break;
        case "presence_penalty":
          var presencePenalty = 0.0f;
          if (float.TryParse(value, out presencePenalty)) {
            options.PresencePenalty = presencePenalty;
          }
          break;
        case "stop":
          // TODO: Implement stop words
          break;
        case "seed":
          var seed = 0;
          if (int.TryParse(value, out seed)) {
            options.Seed = seed;
          }
          break;
      }
    }
    return options;
  }

  private InvokeContext LogInvoke(
    StatsigUser? user,
    string method
  ) {
    var su = user ?? new StatsigUser();
    su.AddCustomID("sdk_type", "azureai-dotnet");

    var metadata = new Dictionary<string, string> {
      { "sdk_type", "azureai-dotnet" }
    };

    StatsigServer.LogEvent(
      su,
      "invoke",
      method,
      metadata
    );
    return new InvokeContext {
      InvokeTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
    };
  }

  private void LogUsage(
    StatsigUser? user,
    string method,
    Dictionary<string, string>? usage = null,
    InvokeContext? context = null
  ) {
    var su = user ?? new StatsigUser();
    su.AddCustomID("sdk_type", "azureai-dotnet");

    var metadata = usage ?? new Dictionary<string, string>();
    metadata.Add("sdk_type", "azureai-dotnet");
    if (context != null) {
      metadata.Add(
        "latency_ms",
        (DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - context.InvokeTime).ToString()
      );
    }

    StatsigServer.LogEvent(
      su,
      "usage",
      method,
      metadata
    );
  }
}
