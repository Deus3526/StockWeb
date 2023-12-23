using Microsoft.EntityFrameworkCore;
using System.Collections.Concurrent;

namespace StockWeb.Extensions
{
    public static class EnumerableExtensions
    {
        public static async Task<ConcurrentDictionary<TKey, TValue>> ToConcurrentDictionaryAsync<TSource, TKey, TValue>(
                this IQueryable<TSource> source,
                Func<TSource, TKey> keySelector,
                Func<TSource, TValue> valueSelector)
                where TKey : notnull
        {
            var dictionary = await source.ToDictionaryAsync(keySelector, valueSelector);
            return new ConcurrentDictionary<TKey, TValue>(dictionary);
        }
    }
}
