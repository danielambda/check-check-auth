using System.IdentityModel.Tokens.Jwt;
using Microsoft.IdentityModel.Tokens;

namespace CheckCheckAuth;

public static class TokenService {
  public static string GenerateToken(string base64Secret, DateTime expirationTime, User user) =>
    GenerateToken(base64Secret, expirationTime, user.UserId, user.Username);

  public static string GenerateToken(
    string base64Secret,
    DateTime expirationTime,
    Guid userId,
    string? username
  ) {
    var tokenHandler = new JwtSecurityTokenHandler();
    var dat = new Dictionary<string, object>()
      { ["userId"] = userId.ToString() };
    if (username is not null) dat["username"] = username;

    return tokenHandler.WriteToken(
      tokenHandler.CreateToken(new SecurityTokenDescriptor {
        Expires = expirationTime,
        Claims = new Dictionary<string, object>() {
          { "dat", dat }
        },
        SigningCredentials = new(
          new SymmetricSecurityKey(Convert.FromBase64String(base64Secret)),
          SecurityAlgorithms.HmacSha256
        )
      })
    );
  }
}
