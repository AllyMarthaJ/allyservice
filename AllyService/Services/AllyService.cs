using Grpc.Core;

namespace AllyService.Services; 

public class AllyService : Ally.AllyBase {
    private readonly Greeter.GreeterClient _gClient;
    private readonly Ally.AllyClient _aClient;

    public AllyService(Greeter.GreeterClient gClient, Ally.AllyClient aClient) {
        this._gClient = gClient;
        this._aClient = aClient;
    }

    public override async Task<AllyReply> SayHello(AllyRequest request, ServerCallContext context) {
        var greeterResponse = await this._gClient.InvokeHelloAsync(
            new HelloRequest() {
                Name = "gay"
            }
        );
        return new AllyReply() {
            Message = greeterResponse.Message
        };
    }
}