using System.Text;
using authorization;
using CardsService;
using GameLogic.Interfaces;
using GameLogic.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using WebAPI;
using WebAPI.API.V1;
using WebAPI.Rooms;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddControllers();
builder.Services.AddSingleton<CardsService.IThemesService, ThemesService>();
builder.Services.AddSingleton<IVotingService, VotingService>();
builder.Services.AddSingleton<IGameService, GameService>();
builder.Services.AddTransient<IParser, ThemesJsonParser>();
builder.Services.AddTransient<IRegistrationService, RegistrationService>();
builder.Services.AddTransient<ILoginService, LoginService>();
builder.Services.AddTransient<IGetUser, GetUserService>();
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddSingleton<ProfileStore>();
builder.Services.AddSingleton<RoomCoordinator>();
builder.Services.AddSingleton<BotService.IDecisionMaker, BotService.RuleBasedDecisionMaker>();
builder.Services.AddHostedService<TurnWorker>();
builder.Services.AddSignalR();
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = 429;
    options.AddPolicy("sensitive", context => RateLimitPartition.GetFixedWindowLimiter(
        context.Connection.RemoteIpAddress?.ToString() ?? "unknown", _ => new FixedWindowRateLimiterOptions
        { PermitLimit = 20, Window = TimeSpan.FromMinutes(1), QueueLimit = 0 }));
});
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.AddSecurityDefinition("bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Description = "JWT token",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT"
    });

    options.AddSecurityRequirement(document => new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecuritySchemeReference("bearer", document),
            new List<string>()
        }
    });
});
var jwtSettings = builder.Configuration.GetSection("Jwt");
var secretKey = jwtSettings["Key"] ?? throw new InvalidOperationException("Jwt:Key is required");

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
}).AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters()
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ClockSkew = TimeSpan.Zero,
        ValidIssuer = jwtSettings["Issuer"],
        ValidAudience = jwtSettings["Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey))
    };
    options.Events = new JwtBearerEvents
    {
        OnMessageReceived = context =>
        {
            var accessToken = context.Request.Query["access_token"];
            var path = context.HttpContext.Request.Path;
            if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/room_hub"))
            {
                context.Token = accessToken;
            }

            return Task.CompletedTask;
        }
    };
});
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddAuthorization();
var app = builder.Build();
app.Use(async (context, next) =>
{
    try { await next(context); }
    catch (RoomRuleException error)
    {
        context.Response.StatusCode = error.StatusCode;
        await context.Response.WriteAsJsonAsync(new { message = error.Message });
    }
});
app.UseAuthentication();
app.UseAuthorization();
app.UseRateLimiter();
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(); 
}
app.UseDefaultFiles();
app.UseStaticFiles();
app.MapControllers();
// Explicit page endpoints keep the root fallback from swallowing directory URLs.
foreach (var page in new[] { "room-list", "room", "voting" })
{
    var file = Path.Combine(app.Environment.WebRootPath, page, "index.html");
    app.MapGet("/" + page, () => Results.File(file, "text/html; charset=utf-8"));
}
app.MapFallbackToFile("index.html");
app.MapHub<RoomHub>("/room_hub", options => options.CloseOnAuthenticationExpiration = true);
app.Run();
