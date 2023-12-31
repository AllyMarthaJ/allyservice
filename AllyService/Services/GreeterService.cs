using Grpc.Core;

namespace AllyService.Services;

public class GreeterService : Greeter.GreeterBase {
    private readonly ILogger<GreeterService> logger;
    private readonly Greeter.GreeterClient selfClient;

    public GreeterService(ILogger<GreeterService> logger, Greeter.GreeterClient client) {
        this.logger = logger;
        this.selfClient = client;
    }

    public override Task<HelloReply> SayHello(HelloRequest request, ServerCallContext context) {
        return Task.FromResult(
            new HelloReply {
                Message = "Hello " + request.Name
            }
        );
    }

    public override async Task<HelloReply> InvokeHello(HelloRequest request, ServerCallContext context) {
        return this.selfClient.SayHello(request);
    }
}