using System.Collections.Concurrent;

namespace StockWeb.Extensions
{
    public static class EnumerableExtensions
    {
        public static ConcurrentBag<T> ToConcurrentBag<T>(this IEnumerable<T> source)
        {
            return new ConcurrentBag<T>(source);
        }
    }
}
