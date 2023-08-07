using AllyService.Events;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;

namespace AllyService.Services;

public class AllyService : Ally.AllyBase {
    private readonly ILogger<AllyService> _logger;
    private readonly Greeter.GreeterClient _gClient;
    private readonly Ally.AllyClient _aClient;
    private readonly EventFactory<HelloEvent> _allyEventPool;

    public AllyService(ILogger<AllyService> logger, Greeter.GreeterClient gClient, Ally.AllyClient aClient,
        EventFactory<HelloEvent> allyEventPool) {
        this._logger = logger;
        this._gClient = gClient;
        this._aClient = aClient;
        this._allyEventPool = allyEventPool;
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

        this._allyEventPool.Trigger(new HelloEvent() { Reply = reply });

        return reply;
    }

    public override async Task SubscribeToRandomMessages(Empty _, IServerStreamWriter<SubscribeResponse> responseStream,
        ServerCallContext context) {
        this._allyEventPool.Subscribe("global-ident", async (ev) => {
            await responseStream.WriteAsync(new SubscribeResponse()
                { Message = $"Received message; {ev.Reply.Message}" });
        });

        while (!context.CancellationToken.IsCancellationRequested) {
            await Task.Delay(500);
        }
        
        this._allyEventPool.Unsubscribe("global-ident");
    }
}