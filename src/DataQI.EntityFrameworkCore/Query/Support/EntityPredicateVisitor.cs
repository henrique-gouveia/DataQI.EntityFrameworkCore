using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

using DataQI.Commons.Query;
using DataQI.Commons.Query.Ast;

namespace DataQI.EntityFrameworkCore.Query.Support
{
    internal sealed class EntityPredicateVisitor<TEntity> : ICriterionVisitor<Expression>
    {
        private static readonly MethodInfo StringContainsMethod =
            typeof(string).GetMethod("Contains", new[] { typeof(string) });
        private static readonly MethodInfo StringStartsWithMethod =
            typeof(string).GetMethod("StartsWith", new[] { typeof(string) });
        private static readonly MethodInfo StringEndsWithMethod =
            typeof(string).GetMethod("EndsWith", new[] { typeof(string) });
        private static readonly MethodInfo StringCompareMethod =
            typeof(string).GetMethod("Compare", new[] { typeof(string), typeof(string) });

        private readonly ParameterExpression parameter;

        private EntityPredicateVisitor(ParameterExpression parameter)
        {
            this.parameter = parameter;
        }

        public Expression Visit(Comparison comparison)
        {
            var property = Property(comparison.PropertyName);

            switch (comparison.Kind)
            {
                case ComparisonKind.GreaterThan: return RelationalComparison(ExpressionType.GreaterThan, property, comparison.Value);
                case ComparisonKind.GreaterThanEqual: return RelationalComparison(ExpressionType.GreaterThanOrEqual, property, comparison.Value);
                case ComparisonKind.LessThan: return RelationalComparison(ExpressionType.LessThan, property, comparison.Value);
                case ComparisonKind.LessThanEqual: return RelationalComparison(ExpressionType.LessThanOrEqual, property, comparison.Value);
                case ComparisonKind.Equal:
                default: return Expression.Equal(property, CapturedValue(comparison.Value, property.Type));
            }
        }

        public Expression Visit(Between between)
        {
            var property = Property(between.PropertyName);

            return Expression.AndAlso(
                RelationalComparison(ExpressionType.GreaterThanOrEqual, property, between.Starts),
                RelationalComparison(ExpressionType.LessThanOrEqual, property, between.Ends));
        }

        public Expression Visit(In inCriterion)
        {
            var property = Property(inCriterion.PropertyName);
            var elementType = property.Type;

            var typedValues = Array.CreateInstance(elementType, inCriterion.Values.Length);
            for (int i = 0; i < inCriterion.Values.Length; i++)
                typedValues.SetValue(ValueCoercion.To(inCriterion.Values[i], elementType), i);

            var containsMethod = typeof(Enumerable)
                .GetMethods()
                .First(m => m.Name == "Contains" && m.GetParameters().Length == 2)
                .MakeGenericMethod(elementType);

            return Expression.Call(containsMethod, CapturedValue(typedValues, typedValues.GetType()), property);
        }

        public Expression Visit(IsNull isNull)
        {
            var property = Property(isNull.PropertyName);
            var isNullable = Nullable.GetUnderlyingType(property.Type) != null || !property.Type.IsValueType;

            if (!isNullable)
                throw new InvalidOperationException(
                    $"Property '{isNull.PropertyName}' is of non-nullable type '{property.Type.Name}' and can never be null.");

            return Expression.Equal(property, Expression.Constant(null, property.Type));
        }

        public Expression Visit(TextMatch textMatch)
        {
            var property = Property(textMatch.PropertyName);
            var value = CapturedValue(textMatch.Value, typeof(string));

            switch (textMatch.Kind)
            {
                case TextMatchKind.StartingWith: return Expression.Call(property, StringStartsWithMethod, value);
                case TextMatchKind.EndingWith: return Expression.Call(property, StringEndsWithMethod, value);
                case TextMatchKind.Containing:
                case TextMatchKind.Like:
                default: return Expression.Call(property, StringContainsMethod, value);
            }
        }

        public Expression Visit(Not not)
        {
            return Expression.Not(not.Inner.Accept(this));
        }

        public Expression Visit(Junction junction)
        {
            if (junction.Members.Count == 0)
                throw new InvalidOperationException($"Junction '{junction.Kind}' must contain at least one criterion.");

            Expression result = null;
            foreach (var member in junction.Members)
            {
                var memberExpression = member.Accept(this);
                if (result == null)
                    result = memberExpression;
                else
                    result = junction.Kind == LogicalKind.And
                        ? Expression.AndAlso(result, memberExpression)
                        : Expression.OrElse(result, memberExpression);
            }

            return result;
        }

        private MemberExpression Property(string propertyName) => Expression.Property(parameter, propertyName);

        private static Expression RelationalComparison(ExpressionType kind, Expression property, object value)
        {
            if (property.Type == typeof(string) && value == null)
                throw new ArgumentException("String comparison value must not be null.", nameof(value));

            var left = property;
            Expression right = CapturedValue(value, property.Type);
            if (property.Type == typeof(string))
            {
                left = Expression.Call(StringCompareMethod, property, right);
                right = Expression.Constant(0);
            }

            return Expression.MakeBinary(kind, left, right);
        }

        private static MemberExpression CapturedValue(object value, Type targetType)
        {
            var coercedValue = ValueCoercion.To(value, targetType);
            if (coercedValue == null && targetType.IsValueType && Nullable.GetUnderlyingType(targetType) == null)
                throw new ArgumentException($"Value must not be null for type '{targetType.Name}'.", nameof(value));

            var holderType = typeof(ValueHolder<>).MakeGenericType(targetType);
            var holder = Activator.CreateInstance(holderType, new object[] { coercedValue });

            return Expression.Property(Expression.Constant(holder, holderType), nameof(ValueHolder<object>.Value));
        }

        public static Expression<Func<TEntity, bool>> BuildPredicate(ICriteria criteria)
        {
            var parameter = Expression.Parameter(typeof(TEntity), "e");
            var visitor = new EntityPredicateVisitor<TEntity>(parameter);

            Expression body = null;
            foreach (var criterion in criteria.Criterions)
            {
                var criterionExpression = criterion.Accept(visitor);
                body = body == null ? criterionExpression : Expression.AndAlso(body, criterionExpression);
            }

            if (body == null)
                body = Expression.Constant(true);

            return Expression.Lambda<Func<TEntity, bool>>(body, parameter);
        }
    }
}
