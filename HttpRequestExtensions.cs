namespace CheckCheckAuth;

public static class HttpRequestExtensions {
  public static bool HasQueryFlag(this HttpRequest req, string flag) =>
    req.Query.ContainsKey(flag);
}
