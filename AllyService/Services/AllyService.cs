using System.Text.Json;
using AllyService.Events;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;

namespace AllyService.Services;

public class AllyService : Ally.AllyBase {
    // RPC clients
    private readonly Greeter.GreeterClient greeterClient;
    private readonly Ally.AllyClient selfClient;

    // Events
    private readonly EventFactory<HelloEvent> helloEventFactory;

    // Logger
    private readonly ILogger<AllyService> logger;

    public AllyService(ILogger<AllyService> logger, Greeter.GreeterClient gClient, Ally.AllyClient aClient,
        EventFactory<HelloEvent> helloEventFactory) {
        this.logger = logger;
        this.greeterClient = gClient;
        this.selfClient = aClient;
        this.helloEventFactory = helloEventFactory;
    }

    public override async Task<AllyResponse> SayHello(AllyRequest request, ServerCallContext context) {
        var rnd = new Random();
        
        var greeterResponse = await this.greeterClient.InvokeHelloAsync(
            new HelloRequest {
                Name = "gay" + rnd.Next(1000)
            }
        );

        var reply = new AllyResponse {
            Message = greeterResponse.Message
        };

        await this.helloEventFactory.Trigger(new HelloEvent { Reply = reply });

        return reply;
    }
}