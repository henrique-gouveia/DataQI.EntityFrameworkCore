using System;
using System.Linq;
using System.Collections.Concurrent;

using Microsoft.EntityFrameworkCore;

using DataQI.Commons.Util;

namespace DataQI.EntityFrameworkCore.Extensions
{
    /// <summary>Provides extension methods for <see cref="DbContext"/>.</summary>
    public static class DbContextExtensions
    {
        private static readonly ConcurrentDictionary<Type, string[]> KeyPropertiesByEntityType = new ConcurrentDictionary<Type, string[]>();

        /// <summary>Gets the value of the single-column primary key of an entity.</summary>
        /// <typeparam name="TEntity">The entity type.</typeparam>
        /// <typeparam name="TKey">The type of the key property.</typeparam>
        /// <param name="context">The context that maps <typeparamref name="TEntity"/>.</param>
        /// <param name="entity">The entity to read the key from; must not be <c>null</c>.</param>
        /// <returns>The current value of the key property.</returns>
        /// <exception cref="System.ArgumentException"><paramref name="entity"/> is <c>null</c>.</exception>
        /// <exception cref="InvalidOperationException">The primary key is composite.</exception>
        public static TKey KeyOf<TEntity, TKey>(this DbContext context, TEntity entity) where TEntity : class
        {
            var keyParts = KeyOf(context, entity);
            if (keyParts.Length > 1)
            {
                throw new InvalidOperationException($"Key is composite and has '{keyParts.Length}' parts.");
            }

            return (TKey) keyParts[0];
        }        

        /// <summary>Gets the current values of the primary key of an entity.</summary>
        /// <typeparam name="TEntity">The entity type.</typeparam>
        /// <param name="context">The context that maps <typeparamref name="TEntity"/>.</param>
        /// <param name="entity">The entity to read the key from; must not be <c>null</c>.</param>
        /// <returns>The key values, one per key property, in key order. The key property names are cached per runtime type of <paramref name="entity"/>.</returns>
        /// <exception cref="System.ArgumentException"><paramref name="entity"/> is <c>null</c>.</exception>
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