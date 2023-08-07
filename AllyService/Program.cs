using System.IO;
using System.Net;
using System.Reflection;
using AllyService;
using AllyService.Events;
using AllyService.Services;
using Grpc.Core;
using Grpc.Net.ClientFactory;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);
builder.WebHost.ConfigureKestrel((options) => {
    options.Listen(IPAddress.Loopback, 5000);
    options.Listen(IPAddress.Loopback, 5001, listenOptions => { listenOptions.UseHttps(); });
});

// Additional configuration is required to successfully run gRPC on macOS.
// For instructions on how to configure Kestrel and gRPC clients on macOS, visit https://go.microsoft.com/fwlink/?linkid=2099682

// Add services to the container.
builder.Services.AddGrpc((o) => {
    o.Interceptors.Add<StreamingInterceptor>();
}).AddJsonTranscoding((o) => {
    
});
builder.Services.AddGrpcSwagger();
builder.Services.AddSwaggerGen((opts) => {
    opts.SwaggerDoc("v1", new OpenApiInfo() {
        Title = "Ally's API" , 
        Version = "v1"
    });

    var path = Path.Combine(System.AppContext.BaseDirectory, "AllyService.xml");
    opts.IncludeXmlComments(path);
    opts.IncludeGrpcXmlComments(path, includeControllerXmlComments: true);
});

Action<GrpcClientFactoryOptions> factoryClientOptions = 
    (o) => o.Address = new Uri("https://localhost:5001");

builder.Services.AddGrpcClient<Greeter.GreeterClient>(factoryClientOptions);
builder.Services.AddGrpcClient<Ally.AllyClient>(factoryClientOptions);
builder.Services.AddSingleton<EventFactory<HelloEvent>>();

var app = builder.Build();
app.UseSwagger();
app.UseSwaggerUI((opts) => {
    opts.SwaggerEndpoint(
        "/swagger/v1/swagger.json", 
        "Ally's API v1"
    );
});

// Configure the HTTP request pipeline.
app.MapGrpcService<GreeterService>();
app.MapGrpcService<AllyService.Services.AllyService>();
app.MapGet("/",
    () =>
        "Sup nerds.");

app.Run();