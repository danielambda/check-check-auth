using CheckCheckAuth;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using static Microsoft.AspNetCore.Http.Results;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddEnvironmentVariables();

builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection("Jwt"));
builder.Services.Configure<BackendClientSettings>(builder.Configuration.GetSection("BackendClient"));

var jwtSettings = builder.Configuration.GetSection("Jwt").Get<JwtSettings>();
if (string.IsNullOrEmpty(jwtSettings?.Secret))
  throw new InvalidOperationException("Jwt:Secret is not configured");

if (string.IsNullOrEmpty(builder.Configuration["ApiKey"]))
  throw new InvalidOperationException("ApiKey is not configured");

builder.Services.AddDbContext<AppDbContext>(options =>
  options.UseNpgsql(builder.Configuration.GetConnectionString("PostgreSQL"))
);

builder.Services.AddAuthentication(x => {
  x.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
  x.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
  x.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
}).AddJwtBearer(x => {
  x.TokenValidationParameters = new() {
    IssuerSigningKey = new SymmetricSecurityKey(Convert.FromBase64String(jwtSettings.Secret)),
    ValidateLifetime = true,
    ValidateIssuer = false,
    ValidateAudience = false,
  };
});

builder.Services.AddAuthorization();

builder.Services.AddHttpClient<BackendClient>((serviceProvider, client) => {
  var apiSettings = serviceProvider.GetRequiredService<IOptions<BackendClientSettings>>().Value;
  client.BaseAddress = new Uri(apiSettings.BaseAddress);
});

var app = builder.Build();

app.UseAuthentication();
app.UseAuthorization();

app.MapPost("/auth/telegram", async (
  [FromHeader(Name = "x-api-key")] string apiKey,
  [FromBody] TgAuthData data,
  [FromServices] AppDbContext dbContext,
  [FromServices] IOptions<JwtSettings> jwtOptions,
  [FromServices] IConfiguration configuration,
  [FromServices] BackendClient backendClient,
  HttpRequest request
) => {
  if (string.IsNullOrWhiteSpace(data.tgUsername))
    return BadRequest("Empty username");

  if (apiKey != configuration["ApiKey"]) return Unauthorized();

  var (jwtSecret, jwtExpirySeconds) = jwtOptions.Value;
  var expirationTime = DateTime.UtcNow.AddSeconds(jwtExpirySeconds);

  var users = dbContext.Users;
  var (tgUserId, tgUsername) = data;

  if (await users.FirstOrDefaultAsync(u => u.TgUserId == tgUserId || u.TgUsername == tgUsername)
      is { UserId: var userId, Username: var username }) {
    return Ok(new {
      Token = TokenService.GenerateToken(jwtSecret, expirationTime, userId, username),
      ExpirationTime = expirationTime
    });
  }

  var user = new User(Guid.NewGuid(), tgUsername, tgUserId, tgUsername);
  var token = TokenService.GenerateToken(jwtSecret, expirationTime, user);

  switch (await backendClient.CreateMeAsync(token)) {
    case CreateMeResult.BadRequest(var err):
      return BadRequest(err);
    case CreateMeResult.Unauthorized(var err):
      return Unauthorized();
    case CreateMeResult.Error(var err):
      return InternalServerError(err);
  }

  await users.AddAsync(user);
  await dbContext.SaveChangesAsync();

  return Ok(new {
    Token = token,
    ExpirationTime = expirationTime
  });
});

app.MapGet("users/{q}", async (
  [FromRoute] string q,
  [FromServices] AppDbContext dbCtx
) => UserQuery.Parse(q) is not {} query ? BadRequest("Invalid user query format")
     : await query.Execute(dbCtx.Users) is {} user ? Ok(user)
     : NotFound("") // to make it serialize into JSON
).RequireAuthorization();

app.Run();

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

record TgAuthData(long tgUserId, string tgUsername);
