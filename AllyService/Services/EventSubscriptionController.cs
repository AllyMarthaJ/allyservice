using System.Text.Json;
using AllyService.Events;
using Microsoft.AspNetCore.Mvc;

namespace AllyService.Services; 

public class EventSubscriptionController : Controller {
    private readonly EventFactory<HelloEvent> helloEventFactory;
    private readonly ILogger<EventSubscriptionController> logger;

    public EventSubscriptionController(EventFactory<HelloEvent> helloEventFactory, ILogger<EventSubscriptionController> logger) {
        this.helloEventFactory = helloEventFactory;
        this.logger = logger;
    }

    [Route("/v1/sse")]
    public async Task Subscribe() {
        var rnd = new Random();
        var ident = $"global-ident-{rnd.Next(1000)}";

        this.logger.LogInformation("Received subscription from {0}", ident);

        this.Response.Headers.Add("Content-Type", "text/event-stream");
        this.Response.Headers.Add("Cache-Control", "no-cache");

        var subscribed = true;
        
        this.helloEventFactory.Subscribe(ident,
            async ev => {
                var helloSubscriptionType = new HelloSubscription()
                    { Message = $"Received message; {ev.Reply.Message}" };

                var response = JsonSerializer.Serialize(helloSubscriptionType);
                
                this.logger.LogInformation("Writing to subscriber {0}", ident);

                try {
                    this.Response.Headers.TryAdd("Content-Type", "text/event-stream");
                    this.Response.Headers.TryAdd("Cache-Control", "no-cache");
                    
                    await this.Response.WriteAsync("event: message" + "\n");
                    await this.Response.WriteAsync("data:" + response + "\n\n");
                    await this.Response.Body.FlushAsync();
                }
                catch (OperationCanceledException ex) {
                    subscribed = false;
                }
            });

        while (subscribed) {
            await Task.Delay(500);
        }
        
        this.logger.LogInformation("Subscriber {0} unsubscribed from SSE", ident);
        this.helloEventFactory.Unsubscribe(ident);
    }
}