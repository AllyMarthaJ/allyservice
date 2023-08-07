using AllyService.Events;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;

namespace AllyService.Services;

public class AllyService : Ally.AllyBase {
    // Logger
    private readonly ILogger<AllyService> _logger;
    
    // RPC clients
    private readonly Greeter.GreeterClient _gClient;
    private readonly Ally.AllyClient _aClient;
    
    // Events
    private readonly EventFactory<HelloEvent> _helloEventFactory;

    public AllyService(ILogger<AllyService> logger, Greeter.GreeterClient gClient, Ally.AllyClient aClient,
        EventFactory<HelloEvent> helloEventFactory) {
        this._logger = logger;
        this._gClient = gClient;
        this._aClient = aClient;
        this._helloEventFactory = helloEventFactory;
    }

    public override async Task<AllyReply> SayHello(AllyRequest request, ServerCallContext context) {
        var greeterResponse = await this._gClient.InvokeHelloAsync(
            new HelloRequest() {
                Name = "gay"
            }
        );

        var reply = new AllyReply() {
            Message = greeterResponse.Message
        };

        await this._helloEventFactory.Trigger(new HelloEvent() { Reply = reply });

        return reply;
    }

    public override async Task Subscribe(Empty _, IServerStreamWriter<HelloSubscription> responseStream,
        ServerCallContext context) {
        this._helloEventFactory.Subscribe("global-ident", async (ev) => {
            await responseStream.WriteAsync(new HelloSubscription()
                { Message = $"Received message; {ev.Reply.Message}" });
        });

        while (!context.CancellationToken.IsCancellationRequested) {
            await Task.Delay(500);
        }
        
        this._helloEventFactory.Unsubscribe("global-ident");
    }
}