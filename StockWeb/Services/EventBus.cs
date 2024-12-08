using System.Collections.Concurrent;

namespace StockWeb.Services
{
    public class EventBus
    {
        public EventBus()
        {
        }
        private readonly ConcurrentDictionary<Type, HashSet<Delegate>> _handlersDic = new();
        public Task PublishAsync<TEvent>(TEvent eventData) where TEvent : BaseEvent
        {
            if (eventData == null)
                throw new ArgumentNullException(nameof(eventData));

            if (!_handlersDic.TryGetValue(typeof(TEvent), out var handlers))
                return Task.CompletedTask;

            var tasks = handlers
                .Select(handler => ((Func<TEvent, Task>)handler)(eventData)).ToArray();

            return Task.CompletedTask;
        }

        public void Subscribe<TEvent>(Func<TEvent, Task> handler) where TEvent : BaseEvent
        {
            if (handler == null)
                throw new ArgumentNullException(nameof(handler));

            var handlers = _handlersDic.GetOrAdd(typeof(TEvent), _ => new HashSet<Delegate>());

            lock (handlers)
            {
                handlers.Add(handler);
            }
        }

        public void Unsubscribe<TEvent>(Func<TEvent, Task> handler) where TEvent : BaseEvent
        {
            if (handler == null)
                throw new ArgumentNullException(nameof(handler));

            if (_handlersDic.TryGetValue(typeof(TEvent), out var handlers))
            {
                lock (handlers)
                {
                    handlers.Remove(handler);

                    if (handlers.Count == 0)
                    {
                        _handlersDic.TryRemove(typeof(TEvent), out _);
                    }
                }
            }
        }
    }


    public abstract class BaseEvent
    {
    }

    public class UpdateDayInfoEvent : BaseEvent
    {
        public DateOnly Date {  get; set; }
    }
}
