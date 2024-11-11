namespace Statsig.AzureAI.Tests;

using System;
using System.IO;

public class DotEnv {
  private Dictionary<string, string> _env = new Dictionary<string, string>();
  public void Load() {
    var root = Directory.GetCurrentDirectory();
    var dotenv = Path.Combine(root, ".env.local");
    if (!File.Exists(dotenv)) {
      return;
    }

    foreach (var line in File.ReadAllLines(dotenv)) {
      var parts = line.Split('=', StringSplitOptions.RemoveEmptyEntries);
      if (parts.Length != 2)
        continue;

      _env[parts[0]] = parts[1];
    }
  }

  public string this[string key] {
    get {
      if (_env.ContainsKey(key)) {
        return _env[key];
      }

      var value = Environment.GetEnvironmentVariable(key);
      if (value != null) {
        _env[key] = value;
      }

      return value ?? "";
    }
  }
}