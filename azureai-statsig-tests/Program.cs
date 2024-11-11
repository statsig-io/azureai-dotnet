namespace Statsig.AzureAI.Tests;

class TestProgram {
  DotEnv env = new DotEnv();
  
  static void Main(string[] args) {
    var p = new TestProgram();
    p.RunTests().Wait();
  }

  public async Task RunTests() {
    env.Load();
    await Server.Initialize(env["STATSIG_SERVER_KEY"]);
    // await TestComplete();
    // await TestCompleteWithDynamicConfig();
    // await TestStreamComplete();
    // await TestGetModelInfo();
    await TestGetEmbeddings();
    await Server.Shutdown();
  }

  public async Task TestComplete() {
    var client = Server.GetModelClientFromEndpoint(
      env["DEPLOYMENT_ENDPOINT_URL"],
      env["DEPLOYMENT_KEY"]
    );
    var completion = await client.Complete(
      "You are a helpful assistant that speaks like a pirate",
      "How do you train a parrot in 10 easy steps?"
    );
    Console.WriteLine(completion);
  }

  public async Task TestCompleteWithDynamicConfig() {
    var client = Server.GetModelClient("gpt-4o-mini");
    var completion = await client.Complete(
      "You are a helpful assistant that speaks like a pirate",
      "How do you train a parrot in 10 easy steps?"
    );
    Console.WriteLine(completion);
  }

  public async Task TestStreamComplete() {
    var client = Server.GetModelClient("gpt-4o-mini");
    var completion = await client.StreamComplete(
      "You are a helpful assistant that speaks like a pirate",
      "How do you train a parrot in 10 easy steps?"
    );
    
    await foreach (var update in completion) {
      if (!string.IsNullOrEmpty(update.ContentUpdate)) {
        Console.Write(update.ContentUpdate);
      }
    }
  }

  public async Task TestGetModelInfo() {
    var client = Server.GetModelClientFromEndpoint(
      env["DEPLOYMENT_ENDPOINT_URL"],
      env["DEPLOYMENT_KEY"]
    );
    var info = await client.GetModelInfo();
    Console.WriteLine(info.ModelName);
    Console.WriteLine(info.ModelProviderName);
  }

  public async Task TestGetEmbeddings() {
    var client = Server.GetModelClientFromEndpoint(
      env["DEPLOYMENT_ENDPOINT_URL"],
      env["DEPLOYMENT_KEY"]
    );
    var embedding = await client.GetEmbeddings(["Hello, world!", "Goodbye, world!"]);
    Console.WriteLine(embedding.First().ToArray());
    Console.WriteLine(embedding.Last().ToArray());
  }
}
