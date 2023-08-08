namespace AllyService.Events;

public class EventFactory<T> {
    private readonly Dictionary<string, Func<T, Task>> subscribers = new();

    public virtual void Subscribe(string ident, Func<T, Task> subscription) {
        this.subscribers.TryAdd(ident, subscription);
    }

    public virtual void Unsubscribe(string ident) {
        this.subscribers.Remove(ident);
    }

    public virtual async Task Trigger(T ev) {
        foreach (var (_, callback) in this.subscribers) await callback(ev);
    }
}