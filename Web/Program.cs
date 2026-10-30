using System.Text;
using authorization;
using CardsService;
using GameLogic.Interfaces;
using GameLogic.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using RoomService;
using WebAPI;
using WebAPI.API.V1;
using Serilog;
using DbConnection;
using ImageService;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddControllers();
builder.Services.AddSingleton<IRoomService, RoomService.RoomService>();
builder.Services.AddSingleton<IOupService, OupService>();
builder.Services.AddSingleton<IBotFactory>(provider =>
{
    var config = provider.GetRequiredService<IConfiguration>();
    string apiKey = config["OpenRouterApiKey"];
    return new BotFactory(apiKey);
}); 
builder.Services.AddTransient<ILobbyService, LobbyService>();
builder.Services.AddTransient<CardsService.IThemesService, ThemesService>();
builder.Services.AddTransient<IVotingService, VotingService>();
builder.Services.AddTransient<IGameService, GameService>();
builder.Services.AddTransient<IGameWorker, GameWorker>();
builder.Services.AddTransient<IThemesRepository, ThemesDbRepository>();
builder.Services.AddTransient<IRegistrationService, RegistrationService>();
builder.Services.AddTransient<ILoginService, LoginService>();
builder.Services.AddTransient<IGetUser, GetUserService>();
builder.Services.AddTransient<IGameStatsService, GameStatsService>();
builder.Services.AddSingleton<ITurnStorage, TurnStorage>();
builder.Services.AddScoped<IGameWorker, GameWorker>();
builder.Services.AddTransient<IRepository<ImageModel>, ImageDbRepository>();
builder.Services.AddTransient<FileHasher>();
builder.Services.AddTransient<ImageProcessor>();
builder.Services.AddTransient<ImageProviderFactory>();
builder.Services.AddTransient<IProvider<ImageModel>, LocalImageProvider>();
builder.Services.AddTransient<IProvider<ImageModel>, UrlImageProvider>();
builder.Services.AddDistributedMemoryCache();
builder.Services.AddHostedService<TurnWorker>();
builder.Services.AddHostedService<RoomCleanupWorker>();
builder.Services.AddTransient(sp =>
{
    var factory = new DbConnectionFactory
    {
        logger = sp.GetRequiredService<ILogger<DbConnectionFactory>>()
    };
    return factory;
});

builder.Services.AddSignalR();
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
var secretKey = jwtSettings["Key"];

builder.Host.UseSerilog((context, services, configuration) => configuration
    .ReadFrom.Configuration(context.Configuration));

var authentication = builder.Services.AddAuthentication(options =>
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
}).AddCookie(ExternalAuthentication.CookieScheme, options =>
{
    options.Cookie.Name = "GameSpy.ExternalLogin";
    options.Cookie.Path = "/api/v1/auth";
    options.Cookie.HttpOnly = true;
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
    options.ExpireTimeSpan = TimeSpan.FromMinutes(5);
    options.SlidingExpiration = false;
});

var yandex = builder.Configuration.GetSection("Authentication:Yandex");
if (!string.IsNullOrWhiteSpace(yandex["ClientId"]) && !string.IsNullOrWhiteSpace(yandex["ClientSecret"]))
{
    authentication.AddYandex(options =>
    {
        options.ClientId = yandex["ClientId"]!;
        options.ClientSecret = yandex["ClientSecret"]!;
        options.SignInScheme = ExternalAuthentication.CookieScheme;
        options.CallbackPath = "/api/v1/auth/signin-callback/Yandex";
    });
}

var vkontakte = builder.Configuration.GetSection("Authentication:Vkontakte");
if (!string.IsNullOrWhiteSpace(vkontakte["ClientId"]) && !string.IsNullOrWhiteSpace(vkontakte["ClientSecret"]))
{
    authentication.AddVkontakte(options =>
    {
        options.ClientId = vkontakte["ClientId"]!;
        options.ClientSecret = vkontakte["ClientSecret"]!;
        options.SignInScheme = ExternalAuthentication.CookieScheme;
        options.CallbackPath = "/api/v1/auth/signin-callback/Vkontakte";
    });
}
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddAuthorization();
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.KnownIPNetworks.Clear();
    options.KnownProxies.Clear();
});
var app = builder.Build();
try
{
    UserStorage.Initialize();
    ThemeStorage.Initialize();
    ImageStorage.Initialize();
}
catch (Exception exception)
{
    app.Logger.LogCritical(exception, "Could not initialize users, themes and images in PostgreSQL. Check DB_TYPE and CONNECTION_STRING.");
    throw;
}
app.UseForwardedHeaders();
app.UseAuthentication();
app.UseAuthorization();
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
app.UseStaticFiles();
app.MapControllers();
app.MapFallbackToFile("index.html");
app.MapHub<RoomHub>("/room_hub");
app.Run();
