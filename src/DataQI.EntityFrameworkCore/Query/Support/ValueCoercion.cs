using System;

namespace DataQI.EntityFrameworkCore.Query.Support
{
    internal static class ValueCoercion
    {
        public static object To(object raw, Type targetType)
        {
            if (raw == null) return null;

            var underlyingType = Nullable.GetUnderlyingType(targetType) ?? targetType;
            if (underlyingType.IsInstanceOfType(raw)) return raw;

            return Convert.ChangeType(raw, underlyingType);
        }
    }
}
