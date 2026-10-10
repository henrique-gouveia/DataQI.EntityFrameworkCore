using System;
using System.Globalization;

namespace DataQI.EntityFrameworkCore.Query.Support
{
    internal static class ValueCoercion
    {
        public static object To(object raw, Type targetType)
        {
            if (raw == null) return null;

            var underlyingType = Nullable.GetUnderlyingType(targetType) ?? targetType;
            if (underlyingType.IsInstanceOfType(raw)) return raw;

            if (underlyingType.IsEnum)
            {
                var numericType = Enum.GetUnderlyingType(underlyingType);
                var numericValue = Convert.ChangeType(raw, numericType, CultureInfo.InvariantCulture);
                return Enum.ToObject(underlyingType, numericValue);
            }

            if (underlyingType == typeof(Guid) && raw is string guidText)
                return Guid.Parse(guidText);

            if (underlyingType == typeof(DateTimeOffset))
            {
                if (raw is DateTime dateTime)
                {
                    if (dateTime.Kind != DateTimeKind.Utc)
                        throw new ArgumentException("DateTime value must be UTC to convert to DateTimeOffset.", nameof(raw));

                    return new DateTimeOffset(dateTime);
                }

                if (raw is string dateTimeText)
                {
                    var parsed = DateTime.Parse(dateTimeText, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind);
                    if (parsed.Kind == DateTimeKind.Unspecified)
                        throw new FormatException("DateTimeOffset value must include an explicit UTC offset.");

                    return DateTimeOffset.Parse(dateTimeText, CultureInfo.InvariantCulture);
                }
            }

            return Convert.ChangeType(raw, underlyingType, CultureInfo.InvariantCulture);
        }
    }
}
