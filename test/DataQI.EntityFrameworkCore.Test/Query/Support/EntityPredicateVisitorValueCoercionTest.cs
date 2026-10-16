using System;
using System.Globalization;

using DataQI.Commons.Query.Support;
using DataQI.EntityFrameworkCore.Query.Support;

using Xunit;

namespace DataQI.EntityFrameworkCore.Test.Query.Support
{
    public class EntityPredicateVisitorValueCoercionTest
    {
        private readonly Guid externalId = new Guid("a88b71e0-8bd0-4b15-a7b2-e1f569c0b394");
        private readonly DateTimeOffset timestamp = new DateTimeOffset(2026, 10, 9, 12, 30, 0, TimeSpan.FromHours(-3));

        [Theory]
        [InlineData("Status")]
        [InlineData("NullableStatus")]
        public void TestComparisonCoercesEnumNumber(string propertyName)
        {
            var criteria = new Criteria().Add(Restrictions.Equal(propertyName, 1));
            var predicate = EntityPredicateVisitor<CoercionEntity>.BuildPredicate(criteria).Compile();

            Assert.True(predicate(NewEntity()));
            Assert.False(predicate(new CoercionEntity()));
        }

        [Theory]
        [InlineData("ExternalId")]
        [InlineData("NullableExternalId")]
        public void TestComparisonCoercesGuidString(string propertyName)
        {
            var criteria = new Criteria().Add(Restrictions.Equal(propertyName, externalId.ToString()));
            var predicate = EntityPredicateVisitor<CoercionEntity>.BuildPredicate(criteria).Compile();

            Assert.True(predicate(NewEntity()));
            Assert.False(predicate(new CoercionEntity()));
        }

        [Theory]
        [InlineData("Timestamp")]
        [InlineData("NullableTimestamp")]
        public void TestComparisonCoercesDateTimeOffsetString(string propertyName)
        {
            var criteria = new Criteria().Add(Restrictions.Equal(propertyName, "2026-10-09T12:30:00-03:00"));
            var predicate = EntityPredicateVisitor<CoercionEntity>.BuildPredicate(criteria).Compile();

            Assert.True(predicate(NewEntity()));
            Assert.False(predicate(new CoercionEntity()));
        }

        [Theory]
        [InlineData("Status")]
        [InlineData("NullableStatus")]
        public void TestInCoercesEnumNumbers(string propertyName)
        {
            var criteria = new Criteria().Add(Restrictions.In(propertyName, new object[] { 1 }));
            var predicate = EntityPredicateVisitor<CoercionEntity>.BuildPredicate(criteria).Compile();

            Assert.True(predicate(NewEntity()));
            Assert.False(predicate(new CoercionEntity()));
        }

        [Theory]
        [InlineData("ExternalId")]
        [InlineData("NullableExternalId")]
        public void TestInCoercesGuidStrings(string propertyName)
        {
            var criteria = new Criteria().Add(Restrictions.In(propertyName, new object[] { externalId.ToString() }));
            var predicate = EntityPredicateVisitor<CoercionEntity>.BuildPredicate(criteria).Compile();

            Assert.True(predicate(NewEntity()));
            Assert.False(predicate(new CoercionEntity()));
        }

        [Theory]
        [InlineData("Timestamp")]
        [InlineData("NullableTimestamp")]
        public void TestInCoercesDateTimeOffsetStrings(string propertyName)
        {
            var criteria = new Criteria().Add(Restrictions.In(propertyName, new object[] { "2026-10-09T12:30:00-03:00" }));
            var predicate = EntityPredicateVisitor<CoercionEntity>.BuildPredicate(criteria).Compile();

            Assert.True(predicate(NewEntity()));
            Assert.False(predicate(new CoercionEntity()));
        }

        [Theory]
        [InlineData("Timestamp")]
        [InlineData("NullableTimestamp")]
        public void TestBetweenCoercesUtcDateTimeBounds(string propertyName)
        {
            var starts = timestamp.UtcDateTime;
            var ends = starts.AddDays(1);
            var criteria = new Criteria().Add(Restrictions.Between(propertyName, starts, ends));
            var predicate = EntityPredicateVisitor<CoercionEntity>.BuildPredicate(criteria).Compile();
            var lastEntity = NewEntity();
            lastEntity.Timestamp = timestamp.AddDays(1);
            lastEntity.NullableTimestamp = lastEntity.Timestamp;

            Assert.True(predicate(NewEntity()));
            Assert.True(predicate(lastEntity));
            Assert.False(predicate(new CoercionEntity()));
        }

        [Theory]
        [InlineData("Amount")]
        [InlineData("NullableAmount")]
        public void TestComparisonCoercesDecimalUsingInvariantCulture(string propertyName)
        {
            var previousCulture = CultureInfo.CurrentCulture;
            try
            {
                CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("pt-BR");
                var criteria = new Criteria().Add(Restrictions.Equal(propertyName, "1.5"));
                var predicate = EntityPredicateVisitor<CoercionEntity>.BuildPredicate(criteria).Compile();

                Assert.True(predicate(NewEntity()));
                Assert.False(predicate(new CoercionEntity()));
            }
            finally
            {
                CultureInfo.CurrentCulture = previousCulture;
            }
        }

        [Theory]
        [InlineData("NullableStatus")]
        [InlineData("NullableExternalId")]
        [InlineData("NullableTimestamp")]
        public void TestNullableComparisonStillMatchesNull(string propertyName)
        {
            var criteria = new Criteria().Add(Restrictions.Equal(propertyName, null));
            var predicate = EntityPredicateVisitor<CoercionEntity>.BuildPredicate(criteria).Compile();

            Assert.True(predicate(new CoercionEntity()));
            Assert.False(predicate(NewEntity()));
        }

        [Theory]
        [InlineData("Status", "Active", false)]
        [InlineData("Status", "Active", true)]
        [InlineData("ExternalId", "not-a-guid", false)]
        [InlineData("ExternalId", "not-a-guid", true)]
        [InlineData("Timestamp", "not-a-timestamp", false)]
        [InlineData("Timestamp", "not-a-timestamp", true)]
        [InlineData("Amount", "not-a-number", false)]
        [InlineData("Amount", "not-a-number", true)]
        public void TestInvalidValueFormatPreservesFormatException(string propertyName, string value, bool useIn)
        {
            var criteria = new Criteria().Add(useIn
                ? Restrictions.In(propertyName, new object[] { value })
                : Restrictions.Equal(propertyName, value));

            Assert.Throws<FormatException>(() => EntityPredicateVisitor<CoercionEntity>.BuildPredicate(criteria));
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void TestNumericValueOutOfRangePreservesOverflowException(bool useIn)
        {
            var value = "79228162514264337593543950336";
            var criteria = new Criteria().Add(useIn
                ? Restrictions.In("Amount", new object[] { value })
                : Restrictions.Equal("Amount", value));

            Assert.Throws<OverflowException>(() => EntityPredicateVisitor<CoercionEntity>.BuildPredicate(criteria));
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void TestUnsupportedValueConversionPreservesInvalidCastException(bool useIn)
        {
            var criteria = new Criteria().Add(useIn
                ? Restrictions.In("ExternalId", new object[] { 1 })
                : Restrictions.Equal("ExternalId", 1));

            Assert.Throws<InvalidCastException>(() => EntityPredicateVisitor<CoercionEntity>.BuildPredicate(criteria));
        }

        [Theory]
        [InlineData(DateTimeKind.Unspecified)]
        [InlineData(DateTimeKind.Local)]
        public void TestNonUtcDateTimePreservesArgumentException(DateTimeKind kind)
        {
            var value = new DateTime(2026, 10, 9, 12, 30, 0, kind);
            var criteria = new Criteria().Add(Restrictions.Equal("Timestamp", value));

            var exception = Assert.Throws<ArgumentException>(() =>
                EntityPredicateVisitor<CoercionEntity>.BuildPredicate(criteria));

            Assert.Equal("raw", exception.ParamName);
        }

        private CoercionEntity NewEntity()
        {
            return new CoercionEntity
            {
                Status = EntityStatus.Active,
                NullableStatus = EntityStatus.Active,
                ExternalId = externalId,
                NullableExternalId = externalId,
                Timestamp = timestamp,
                NullableTimestamp = timestamp,
                Amount = 1.5m,
                NullableAmount = 1.5m
            };
        }

        private enum EntityStatus
        {
            Inactive,
            Active
        }

        private sealed class CoercionEntity
        {
            public EntityStatus Status { get; set; }
            public EntityStatus? NullableStatus { get; set; }
            public Guid ExternalId { get; set; }
            public Guid? NullableExternalId { get; set; }
            public DateTimeOffset Timestamp { get; set; }
            public DateTimeOffset? NullableTimestamp { get; set; }
            public decimal Amount { get; set; }
            public decimal? NullableAmount { get; set; }
        }
    }
}
