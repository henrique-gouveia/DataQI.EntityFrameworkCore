using System;

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
    }
}
