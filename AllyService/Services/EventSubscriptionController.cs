using System.Text.Json;
using AllyService.Events;
using Microsoft.AspNetCore.Mvc;

namespace AllyService.Services; 

public class EventSubscriptionController : Controller {
    private readonly EventFactory<HelloEvent> helloEventFactory;
    private readonly ILogger<EventSubscriptionController> logger;
    private readonly SubscriptionManager manager;

    public EventSubscriptionController(EventFactory<HelloEvent> helloEventFactory, ILogger<EventSubscriptionController> logger, SubscriptionManager manager) {
        this.helloEventFactory = helloEventFactory;
        this.logger = logger;
        this.manager = manager;
    }
    
    // Hacks: We can't use the event stream to send back data via
    // gRPC route because JSON transcoding is, well, JSON, not an
    // event stream.
    [HttpGet("/v1/sse")]
    public async Task<JsonResult> Listen(
        [FromQuery(Name = "id")] string? clientSubscriptionId, 
        CancellationToken cancellationToken
    ) {
        if (clientSubscriptionId == null) {
            return new JsonResult("clientSubscriptionId was not provided.") {
                StatusCode = 400
            };
        }
        // TODO: Verify clientSubscriptionId belongs to the authenticated
        // principal.
        var hasSubscription = this.manager.Subscriptions.TryGetValue(clientSubscriptionId, out var subscriptions) && 
                              subscriptions.Count > 0;
        if (!hasSubscription) {
            return new JsonResult("No subscriptions found for provided clientSubscriptionId.") {
                StatusCode = 400
            };
        }
        
        // Before ever sending content back, let's first set the headers.
        // This ensures no one prematurely closes the connection 
        // because oH nO iT'S nOt aN eVEnT sTReAM
        this.Response.Headers.Add("Content-Type", "text/event-stream");
        this.Response.Headers.Add("Cache-Control", "no-cache");

        foreach (var subscription in subscriptions!) {
            switch (subscription.Name) {
                case "HelloEvent":
                    this.helloEventFactory.Subscribe(
                        clientSubscriptionId, 
                        async (ev) => await this.onEvent(ev, this.helloEventFactory, clientSubscriptionId)
                    );
                    break;
            }
        }

        do {
            hasSubscription = this.manager.Subscriptions.ContainsKey(clientSubscriptionId) && 
                              this.manager.Subscriptions[clientSubscriptionId].Count > 0;
            
            await Task.Delay(500, cancellationToken);
        } while (!cancellationToken.IsCancellationRequested && hasSubscription);
        
        foreach (var subscription in subscriptions!) {
            switch (subscription.Name) {
                case "HelloEvent":
                    this.helloEventFactory.Unsubscribe(clientSubscriptionId);
                    break;
            }
        }
        
        this.logger.LogInformation("Subscriber {0} unsubscribed from SSE", clientSubscriptionId);

        return new JsonResult("Success") { StatusCode = 200 };
    }
    
    private async Task onEvent<T>(T ev, EventFactory<T> factory, string id) {
        try {
            this.Response.Headers.TryAdd("Content-Type", "text/event-stream");
            this.Response.Headers.TryAdd("Cache-Control", "no-cache");
                    
            await this.Response.WriteAsync("event: message" + "\n");
            await this.Response.WriteAsync("data:" + JsonSerializer.Serialize(ev) + "\n\n");
            await this.Response.Body.FlushAsync();
        }
        catch (OperationCanceledException ex) {
            // The next failed event should unsubscribe itself.
            factory.Unsubscribe(id);
            // We should attempt to remove this from the client's subscribed
            // events as well.
            if (this.manager.Subscriptions.TryGetValue(id, out var subscription)) {
                subscription.Remove(typeof(T));
            }
        }
    }
}