using System.Net;

namespace CheckCheckAuth;

public class BackendClient {
  private readonly HttpClient _httpClient;

  public BackendClient(HttpClient httpClient) =>
    _httpClient = httpClient;

  public async Task<CreateMeResult> CreateMeAsync(string jwtToken) {
    try {
      var requestMessage = new HttpRequestMessage(HttpMethod.Post, "/me");

      requestMessage.Headers.Authorization = new("Bearer", jwtToken);

      var response = await _httpClient.SendAsync(requestMessage);

      return response.StatusCode switch {
        HttpStatusCode.Created =>
          new CreateMeResult.Success(),

        HttpStatusCode.BadRequest =>
          new CreateMeResult.BadRequest(await response.Content.ReadAsStringAsync()),

        _ => new CreateMeResult.Error($"Unexpected status code: {response.StatusCode}")
      };
    } catch (HttpRequestException ex) {
      return new CreateMeResult.Error($"Request failed: {ex.Message}");
    }
  }
}

public abstract record CreateMeResult {
  public sealed record Success : CreateMeResult;
  public sealed record BadRequest(string ErrorMessage) : CreateMeResult;
  public sealed record Unauthorized(string Message) : CreateMeResult;
  public sealed record Error(string Message) : CreateMeResult;
}
