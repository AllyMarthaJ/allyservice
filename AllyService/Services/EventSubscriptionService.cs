using System.Text.Json;
using AllyService.Events;
using Google.Protobuf.Collections;
using Grpc.Core;

namespace AllyService.Services; 

public class EventSubscriptionService : Subscription.SubscriptionBase {
    private readonly ILogger<EventSubscriptionService> logger;
    private readonly SubscriptionManager manager;

    public EventSubscriptionService(ILogger<EventSubscriptionService> logger, SubscriptionManager manager) {
        this.logger = logger;
        this.manager = manager;
    }
    
    public override Task<SubscriptionResponse> Subscribe(SubscriptionRequest request, ServerCallContext context) {
        // Subscribing should be an authenticated 
        // operation, which means we should generate a clientId 
        // if the principal doesn't yet have it, with principal name included.
        // For all subscriptions, we should ensure permissions checks.
        var clientId = Guid.NewGuid().ToString();

        var subscriptions = new List<Type>();

        if (request.Subscriptions.Count == 0) {
           throw new RpcException(new Status(StatusCode.InvalidArgument,"No subscriptions requested."));
        }

        foreach (var subscription in request.Subscriptions) {
            switch (subscription.TypeCase) {
                case SubscriptionType.TypeOneofCase.HelloSubscription:
                    subscriptions.Add(typeof(HelloEvent));
                    break;
                case SubscriptionType.TypeOneofCase.None:
                default:
                    break;
            }
        }

        this.manager.Subscriptions.TryAdd(clientId, subscriptions);

        this.logger.LogInformation("Subscription added {0}", clientId);

        var response = new SubscriptionResponse() { ClientSubscriptionId = clientId };
        response.Subscriptions.AddRange(request.Subscriptions);
        
        return Task.FromResult(response);
    }
}