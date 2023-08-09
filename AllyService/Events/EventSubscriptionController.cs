using System.Text.Json;
using AllyService.Events;
using Microsoft.AspNetCore.Mvc;

namespace AllyService.Events; 

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
    public async Task<IActionResult> Listen(
        [FromQuery(Name = "id")] string? clientSubscriptionId, 
        CancellationToken cancellationToken
    ) {
        if (clientSubscriptionId == null) {
            return this.BadRequest("clientSubscriptionId was not provided");
        }
        
        // TODO: Verify clientSubscriptionId claim belongs to the authenticated
        // principal.
        var hasSubscription = this.manager.Subscriptions.TryGetValue(clientSubscriptionId, out var subscriptions) && 
                              subscriptions.Count > 0;
        if (!hasSubscription) {
            return this.BadRequest("No subscriptions found for provided clientSubscriptionId");
        }
        
        this.Response.Headers.TryAdd("Content-Type", "text/event-stream");
        this.Response.Headers.TryAdd("Cache-Control", "no-cache");

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
            await Task.Delay(10, CancellationToken.None);
        } while (!cancellationToken.IsCancellationRequested && hasSubscription);
        
        // Purge the subscriptions.
        foreach (var subscription in subscriptions!) {
            switch (subscription.Name) {
                case "HelloEvent":
                    this.helloEventFactory.Unsubscribe(clientSubscriptionId);
                    break;
            }
        }

        this.manager.Subscriptions.Remove(clientSubscriptionId);
        
        this.logger.LogInformation("Closing SSE request for client {clientSubscriptionId}",
            clientSubscriptionId);

        return new EmptyResult();
    }
    
    private async Task onEvent<T>(T ev, EventFactory<T> factory, string id) {
        try {
            this.Response.Headers.TryAdd("Content-Type", "text/event-stream");
            this.Response.Headers.TryAdd("Cache-Control", "no-cache");
                    
            await this.Response.WriteAsync("event: message" + "\n");
            await this.Response.WriteAsync("data:" + JsonSerializer.Serialize(ev) + "\n\n");
            await this.Response.Body.FlushAsync();
        }
        catch (Exception ex) {
            // The next failed event should unsubscribe itself.
            factory.Unsubscribe(id);
            
            // We should attempt to remove this from the client's subscribed
            // events as well.
            if (this.manager.Subscriptions.TryGetValue(id, out var subscription)) {
                subscription.Remove(typeof(T));
            }
            
            this.logger.LogInformation("Client {id} closed SSE connection", id);
        }
    }
}