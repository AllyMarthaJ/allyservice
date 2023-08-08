using System.Net;
using AllyService;
using AllyService.Events;
using AllyService.Services;
using Grpc.Net.ClientFactory;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);
builder.WebHost.ConfigureKestrel(options => {
    options.Listen(IPAddress.Loopback, 5000);
    options.Listen(IPAddress.Loopback, 5001, listenOptions => { listenOptions.UseHttps(); });
});

// Add services to the container.
builder.Services.AddGrpc(o => {
}).AddJsonTranscoding(o => { });
builder.Services.AddGrpcSwagger();
builder.Services.AddSwaggerGen(opts => {
    opts.SwaggerDoc("v1", new OpenApiInfo {
        Title = "Ally's API",
        Version = "v1"
    });

    var path = Path.Combine(AppContext.BaseDirectory, "AllyService.xml");
    opts.IncludeXmlComments(path);
    opts.IncludeGrpcXmlComments(path, true);
});

Action<GrpcClientFactoryOptions> factoryClientOptions =
    o => o.Address = new Uri("https://localhost:5001");

builder.Services.AddGrpcClient<Greeter.GreeterClient>(factoryClientOptions);
builder.Services.AddGrpcClient<Ally.AllyClient>(factoryClientOptions);

builder.Services.AddSingleton<SubscriptionManager>();
builder.Services.AddSingleton<EventFactory<HelloEvent>>();

builder.Services.AddControllers();

builder.Services.AddCors((options) =>
    options.AddPolicy(name: "corsPolicy",
        policy  =>
        {
            policy.WithOrigins("http://localhost:3000")
                .AllowAnyHeader()
                .AllowAnyMethod();
        })
);

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI(opts => {
    opts.SwaggerEndpoint(
        "/swagger/v1/swagger.json",
        "Ally's API v1"
    );
});

// Configure the HTTP request pipeline.
app.MapGrpcService<GreeterService>();
app.MapGrpcService<AllyService.Services.AllyService>();
app.MapGrpcService<EventSubscriptionService>();
app.UseRouting();
app.UseCors("corsPolicy");
app.UseEndpoints(endpoints => {
    endpoints.MapControllers();
});
app.MapGet("/",
    () =>
        "Sup nerds.");

app.Run();