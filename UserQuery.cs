using System.Diagnostics;
using Microsoft.EntityFrameworkCore;

namespace CheckCheckAuth;

abstract record UserQuery {
  public static UserQuery? Parse(string query) => query.Split(':') switch {
    ["user-id", var userIdStr] when Guid.TryParse(userIdStr, out var userId) =>
      new UserUserIdQuery(userId),
    ["username", var username] =>
      new UserUsernameQuery(username),
    ["tg-user-id", var tgUserIdStr] when long.TryParse(tgUserIdStr, out var tgUserId) =>
      new UserTgUserIdQuery(tgUserId),
    ["tg-username", var tgUsername] =>
      new UserTgUsernameQuery(tgUsername),
    [var userIdStr] when Guid.TryParse(userIdStr, out var userId) =>
      new UserUserIdQuery(userId),
    _ => null
  };

  public async Task<User?> Execute(DbSet<User> users) => this switch {
    UserUserIdQuery(var userId) =>
      await users.FindAsync(userId),
    UserUsernameQuery(var username) =>
      await users.FirstOrDefaultAsync(u => u.Username == username),
    UserTgUserIdQuery(var tgUserId) =>
      await users.FirstOrDefaultAsync(u => u.TgUserId == tgUserId),
    UserTgUsernameQuery(var tgUsername) =>
      await users.FirstOrDefaultAsync(u => u.TgUsername == tgUsername),
    _ => throw new UnreachableException()
  };
}

record UserUserIdQuery(Guid UserId) : UserQuery;
record UserUsernameQuery(string Username) : UserQuery;
record UserTgUserIdQuery(long TgUserId) : UserQuery;
record UserTgUsernameQuery(string TgUsername) : UserQuery;
