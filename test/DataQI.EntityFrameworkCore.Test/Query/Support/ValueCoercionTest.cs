using System;
using System.Globalization;

using DataQI.EntityFrameworkCore.Query.Support;

using Xunit;

namespace DataQI.EntityFrameworkCore.Test.Query.Support
{
    public class ValueCoercionTest
    {
        [Fact]
        public void TestReturnsNullForNullInput()
        {
            Assert.Null(ValueCoercion.To(null, typeof(decimal)));
        }

        [Fact]
        public void TestReturnsSameValueWhenAlreadyAssignable()
        {
            var result = ValueCoercion.To("Adams", typeof(string));
            Assert.Equal("Adams", result);
        }

        [Fact]
        public void TestConvertsIntToDecimal()
        {
            var result = ValueCoercion.To(30, typeof(decimal));
            Assert.IsType<decimal>(result);
            Assert.Equal(30m, result);
        }

        [Fact]
        public void TestConvertsToNullableUnderlyingType()
        {
            var result = ValueCoercion.To(30, typeof(int?));
            Assert.IsType<int>(result);
            Assert.Equal(30, result);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void TestConvertsIntToEnum(bool nullable)
        {
            var targetType = nullable ? typeof(EntityStatus?) : typeof(EntityStatus);
            var result = ValueCoercion.To(1, targetType);

            Assert.Equal(EntityStatus.Active, Assert.IsType<EntityStatus>(result));
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void TestConvertsNumericStringToEnum(bool nullable)
        {
            var targetType = nullable ? typeof(EntityStatus?) : typeof(EntityStatus);
            var result = ValueCoercion.To("1", targetType);

            Assert.Equal(EntityStatus.Active, Assert.IsType<EntityStatus>(result));
        }

        [Theory]
        [InlineData("Active", false)]
        [InlineData("Active", true)]
        [InlineData("Inactive", false)]
        [InlineData("Inactive", true)]
        public void TestRejectsEnumNameString(string value, bool nullable)
        {
            var targetType = nullable ? typeof(EntityStatus?) : typeof(EntityStatus);

            Assert.Throws<FormatException>(() => ValueCoercion.To(value, targetType));
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void TestConvertsStringToGuid(bool nullable)
        {
            var expected = new Guid("a88b71e0-8bd0-4b15-a7b2-e1f569c0b394");
            var targetType = nullable ? typeof(Guid?) : typeof(Guid);
            var result = ValueCoercion.To(expected.ToString(), targetType);

            Assert.Equal(expected, Assert.IsType<Guid>(result));
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void TestConvertsStringToDateTimeOffset(bool nullable)
        {
            var expected = new DateTimeOffset(2026, 10, 9, 12, 30, 0, TimeSpan.FromHours(-3));
            var targetType = nullable ? typeof(DateTimeOffset?) : typeof(DateTimeOffset);
            var result = ValueCoercion.To("2026-10-09T12:30:00-03:00", targetType);
            var actual = Assert.IsType<DateTimeOffset>(result);

            Assert.Equal(expected, actual);
            Assert.Equal(expected.Offset, actual.Offset);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void TestConvertsUtcDateTimeToDateTimeOffset(bool nullable)
        {
            var value = new DateTime(2026, 10, 9, 15, 30, 0, DateTimeKind.Utc);
            var targetType = nullable ? typeof(DateTimeOffset?) : typeof(DateTimeOffset);
            var result = ValueCoercion.To(value, targetType);
            var actual = Assert.IsType<DateTimeOffset>(result);

            Assert.Equal(new DateTimeOffset(value), actual);
            Assert.Equal(TimeSpan.Zero, actual.Offset);
        }

        [Theory]
        [InlineData("en-US")]
        [InlineData("pt-BR")]
        public void TestConvertsDecimalUsingInvariantCulture(string cultureName)
        {
            var previousCulture = CultureInfo.CurrentCulture;
            try
            {
                CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo(cultureName);
                var result = ValueCoercion.To("1.5", typeof(decimal));

                Assert.Equal(1.5m, Assert.IsType<decimal>(result));
            }
            finally
            {
                CultureInfo.CurrentCulture = previousCulture;
            }
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void TestConvertsLongToEnumWithLongUnderlyingType(bool nullable)
        {
            var targetType = nullable ? typeof(LongStatus?) : typeof(LongStatus);
            var result = ValueCoercion.To(6000000000L, targetType);

            Assert.Equal(LongStatus.Active, Assert.IsType<LongStatus>(result));
        }

        [Fact]
        public void TestConvertsNumericFlagCombination()
        {
            var result = ValueCoercion.To(3, typeof(EntityFlags));

            Assert.Equal(EntityFlags.Read | EntityFlags.Write, Assert.IsType<EntityFlags>(result));
        }

        [Fact]
        public void TestRejectsEnumUnderlyingTypeOverflow()
        {
            Assert.Throws<OverflowException>(() => ValueCoercion.To(256, typeof(ByteStatus)));
        }

        [Theory]
        [InlineData("")]
        [InlineData("not-a-guid")]
        public void TestRejectsInvalidGuidString(string value)
        {
            Assert.Throws<FormatException>(() => ValueCoercion.To(value, typeof(Guid)));
        }

        [Theory]
        [InlineData("2026-10-09T15:30:00Z", 15, 0, 0)]
        [InlineData("2026-10-09T15:30:00.1234567Z", 15, 0, 1234567)]
        [InlineData("2026-10-09T12:30:00+05:30", 12, 330, 0)]
        [InlineData("2026-10-09T12:30:00.1-03:00", 12, -180, 1000000)]
        [InlineData("2026-10-09T15:30Z", 15, 0, 0)]
        [InlineData("2026-10-09T12:30-03:00", 12, -180, 0)]
        public void TestConvertsExplicitTimestampPreservingOffsetAndPrecision(
            string value, int hour, int offsetMinutes, int fractionTicks)
        {
            var expected = new DateTimeOffset(2026, 10, 9, hour, 30, 0, TimeSpan.FromMinutes(offsetMinutes))
                .AddTicks(fractionTicks);
            var result = Assert.IsType<DateTimeOffset>(ValueCoercion.To(value, typeof(DateTimeOffset)));

            Assert.Equal(expected, result);
            Assert.Equal(expected.Offset, result.Offset);
        }

        [Theory]
        [InlineData("pt-BR")]
        [InlineData("en-US")]
        public void TestConvertsTimestampUsingInvariantCulture(string cultureName)
        {
            var previousCulture = CultureInfo.CurrentCulture;
            try
            {
                CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo(cultureName);
                var result = ValueCoercion.To("2026-10-09T15:30:00Z", typeof(DateTimeOffset));

                Assert.Equal(new DateTimeOffset(2026, 10, 9, 15, 30, 0, TimeSpan.Zero), result);
            }
            finally
            {
                CultureInfo.CurrentCulture = previousCulture;
            }
        }

        [Theory]
        [InlineData("2026-10-09T12:30:00")]
        [InlineData("2026-10-09T12:30:00.1234567")]
        [InlineData("2026-10-09")]
        [InlineData("not-a-timestamp")]
        public void TestRejectsTimestampWithoutTimezoneOrInvalidFormat(string value)
        {
            Assert.Throws<FormatException>(() => ValueCoercion.To(value, typeof(DateTimeOffset)));
        }

        [Theory]
        [InlineData("2026-10-09T12:30:00")]
        [InlineData("2026-10-09T12:30:00.1234567")]
        [InlineData("2026-10-09T12:30")]
        [InlineData("2026-10-09")]
        public void TestCoercionRejectsImplicitOffsetAcceptedByDateTimeOffsetParser(string value)
        {
            var parsed = DateTimeOffset.Parse(value, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind);

            Assert.Equal(TimeZoneInfo.Local.GetUtcOffset(parsed.DateTime), parsed.Offset);
            var exception = Assert.Throws<FormatException>(() => ValueCoercion.To(value, typeof(DateTimeOffset)));
            Assert.Equal("DateTimeOffset value must include an explicit UTC offset.", exception.Message);
        }

        [Theory]
        [InlineData("2026-10-09T12:30:00+15:00")]
        [InlineData("2026-10-09T12:30:00+25:00")]
        public void TestRejectsInvalidTimestampOffset(string value)
        {
            Assert.Throws<FormatException>(() => ValueCoercion.To(value, typeof(DateTimeOffset)));
        }

        [Theory]
        [InlineData(DateTimeKind.Unspecified)]
        [InlineData(DateTimeKind.Local)]
        public void TestRejectsDateTimeThatIsNotUtc(DateTimeKind kind)
        {
            var value = new DateTime(2026, 10, 9, 12, 30, 0, kind);

            var exception = Assert.Throws<ArgumentException>(() => ValueCoercion.To(value, typeof(DateTimeOffset)));

            Assert.Equal("raw", exception.ParamName);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void TestReturnsSameEnumWhenAlreadyAssignable(bool nullable)
        {
            object value = EntityStatus.Active;
            var targetType = nullable ? typeof(EntityStatus?) : typeof(EntityStatus);

            Assert.Same(value, ValueCoercion.To(value, targetType));
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void TestReturnsSameGuidWhenAlreadyAssignable(bool nullable)
        {
            object value = new Guid("a88b71e0-8bd0-4b15-a7b2-e1f569c0b394");
            var targetType = nullable ? typeof(Guid?) : typeof(Guid);

            Assert.Same(value, ValueCoercion.To(value, targetType));
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void TestReturnsSameDateTimeOffsetWhenAlreadyAssignable(bool nullable)
        {
            object value = new DateTimeOffset(2026, 10, 9, 12, 30, 0, TimeSpan.FromHours(-3));
            var targetType = nullable ? typeof(DateTimeOffset?) : typeof(DateTimeOffset);

            Assert.Same(value, ValueCoercion.To(value, targetType));
        }

        [Theory]
        [InlineData(typeof(EntityStatus?))]
        [InlineData(typeof(Guid?))]
        [InlineData(typeof(DateTimeOffset?))]
        public void TestReturnsNullForNullableConversion(Type targetType)
        {
            Assert.Null(ValueCoercion.To(null, targetType));
        }

        public enum LongStatus : long
        {
            Active = 6000000000L
        }

        [Flags]
        public enum EntityFlags
        {
            Read = 1,
            Write = 2
        }

        public enum ByteStatus : byte
        {
            Active = 1
        }

        public enum EntityStatus
        {
            Inactive,
            Active
        }
    }
}
