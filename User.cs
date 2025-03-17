namespace CheckCheckAuth;

public record User(
  Guid UserId,
  string Username,
  long? TgUserId = null,
  string? TgUsername = null
);

