namespace DataQI.EntityFrameworkCore.Query.Support
{
    internal sealed class ValueHolder<T>
    {
        public ValueHolder(T value)
        {
            Value = value;
        }

        public T Value { get; }
    }
}
