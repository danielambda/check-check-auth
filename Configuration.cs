namespace CheckCheckAuth;

public static class ConfigurationExtensions {
  public static WebApplicationBuilder AddConfiguration(this WebApplicationBuilder builder) {
      builder.Configuration.AddEnvironmentVariables();
      builder.Services
        .Configure<JwtSettings>(builder.Configuration.GetSection("Jwt"))
        .Configure<BackendClientSettings>(builder.Configuration.GetSection("BackendClient"));

      if (string.IsNullOrEmpty(builder.Configuration["ApiKey"]))
        throw new InvalidOperationException("ApiKey is not configured");

      return builder;
  }
}

public class JwtSettings {
  public string Secret { get; set; } = default!;
  public int ExpirySeconds { get; set; }

  public void Deconstruct(out string secret, out int expirySeconds) {
    secret = Secret;
    expirySeconds = ExpirySeconds;
  }
}

public class BackendClientSettings() {
  public string BaseAddress { get; set; } = default!;
}
