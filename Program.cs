using auth_worker;
using Auth_Worker.Application.Postgres;
using Auth_Worker.Application.Redis;
using Auth_Worker.Infrastructure;
using StackExchange.Redis;

var builder = Host.CreateApplicationBuilder(args);

var postgresConnectionString = builder.Configuration.GetConnectionString("Postgres")
    ?? throw new InvalidOperationException("Connection string 'Postgres' was not found.");

var redisConnectionString = builder.Configuration.GetConnectionString("Redis")
    ?? throw new InvalidOperationException("Connection string 'Redis' was not found.");

builder.Services.AddNpgsqlDataSource(postgresConnectionString);

builder.Services.AddSingleton<IConnectionMultiplexer>(_ =>
{
    var redisConfiguration = ConfigurationOptions.Parse(redisConnectionString);

    redisConfiguration.AbortOnConnectFail = false;
    redisConfiguration.ConnectRetry = 3;
    redisConfiguration.ConnectTimeout = 5000;
    redisConfiguration.SyncTimeout = 5000;
    redisConfiguration.AsyncTimeout = 5000;

    return ConnectionMultiplexer.Connect(redisConfiguration);
});

builder.Services.AddSingleton<ConnectivityState>();
builder.Services.AddSingleton<IRedisService, RedisService>();
builder.Services.AddSingleton<IPostgresService, PostgresService>();

builder.Services.AddHostedService<Worker>();

var host = builder.Build();
host.Run();
