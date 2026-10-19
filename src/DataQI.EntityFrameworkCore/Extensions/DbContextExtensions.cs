using System;
using System.Collections.Generic;
using System.Linq;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

using DataQI.Commons.Util;

namespace DataQI.EntityFrameworkCore.Extensions
{
    /// <summary>Provides extension methods for <see cref="DbContext"/>.</summary>
    internal static class DbContextExtensions
    {
        /// <summary>Gets the value of the single-column primary key of an entity.</summary>
        /// <typeparam name="TEntity">The entity type.</typeparam>
        /// <typeparam name="TKey">The type of the key property.</typeparam>
        /// <param name="context">The context that maps <typeparamref name="TEntity"/>.</param>
        /// <param name="entity">The entity to read the key from; must not be <c>null</c>.</param>
        /// <returns>The current value of the key property.</returns>
        /// <exception cref="System.ArgumentException"><paramref name="entity"/> is <c>null</c>.</exception>
        /// <exception cref="InvalidOperationException">The primary key is composite.</exception>
        internal static TKey KeyOf<TEntity, TKey>(this DbContext context, TEntity entity) where TEntity : class
        {
            Assert.NotNull(entity, $"{nameof(entity)} must not be null");

            var entry = context.Entry(entity);
            var keyParts = entry.Metadata.FindPrimaryKey()?.Properties
                .Select(property => entry.Property(property.Name).CurrentValue)
                .ToArray();

            if (keyParts?.Length > 1)
                throw new InvalidOperationException($"Key is composite and has '{keyParts.Length}' parts.");

            return (TKey)keyParts?[0];
        }

        /// <summary>Gets the name and CLR type of each primary key property of an entity type, in key order.</summary>
        /// <typeparam name="TEntity">The entity type.</typeparam>
        /// <param name="context">The context that maps <typeparamref name="TEntity"/>.</param>
        /// <returns>The key properties, or an empty list when the entity type has no primary key.</returns>
        internal static IReadOnlyList<(string Name, Type ClrType)> KeyProperties<TEntity>(this DbContext context)
            => context.Model.FindEntityType(typeof(TEntity)).KeyProperties();

        /// <summary>Gets the primary key properties from already resolved entity metadata.</summary>
        internal static IReadOnlyList<(string Name, Type ClrType)> KeyProperties(this IEntityType entityType)
        {
            var key = entityType?.FindPrimaryKey();
            return key == null
                ? Array.Empty<(string, Type)>()
                : key.Properties.Select(p => (p.Name, p.ClrType)).ToArray();
        }
    }
}
