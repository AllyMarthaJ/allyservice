namespace AllyService.Events; 

public class SubscriptionManager {
    public Dictionary<string, List<Type>> Subscriptions { get; } = new();
}