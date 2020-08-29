using System;
using System.Linq;
using System.Collections.Concurrent;

using Microsoft.EntityFrameworkCore;

using DataQI.Commons.Util;

namespace DataQI.EntityFrameworkCore.Extensions
{
    public static class DbContextExtensions
    {
        private static readonly ConcurrentDictionary<Type, string[]> KeyPropertiesByEntityType = new ConcurrentDictionary<Type, string[]>();

        public static TKey KeyOf<TEntity, TKey>(this DbContext context, TEntity entity) where TEntity : class
        {
            var keyParts = KeyOf(context, entity);
            if (keyParts.Length > 1)
            {
                throw new InvalidOperationException($"Key is composite and has '{keyParts.Length}' parts.");
            }

            return (TKey) keyParts[0];
        }        

        public static object[] KeyOf<TEntity>(this DbContext context, TEntity entity) where TEntity : class
        {
            Assert.NotNull(entity, $"{nameof(entity)} must not be null");

            var entry = context.Entry(entity);
            var keyProperties = KeyPropertiesByEntityType.GetOrAdd(
                entity.GetType(),
                t => entry.Metadata.FindPrimaryKey().Properties.Select(property => property.Name).ToArray());

            var keyParts = keyProperties
                .Select(propertyName => entry.Property(propertyName).CurrentValue)
                .ToArray();

            return keyParts;
        }
    }
}