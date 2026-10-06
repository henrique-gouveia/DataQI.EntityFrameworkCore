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

        private readonly ParameterExpression parameter;

        private EntityPredicateVisitor(ParameterExpression parameter)
        {
            this.parameter = parameter;
        }

        public Expression Visit(Comparison comparison)
        {
            var property = Property(comparison.PropertyName);
            var value = Constant(comparison.Value, property.Type);

            switch (comparison.Kind)
            {
                case ComparisonKind.GreaterThan: return Expression.GreaterThan(property, value);
                case ComparisonKind.GreaterThanEqual: return Expression.GreaterThanOrEqual(property, value);
                case ComparisonKind.LessThan: return Expression.LessThan(property, value);
                case ComparisonKind.LessThanEqual: return Expression.LessThanOrEqual(property, value);
                case ComparisonKind.Equal:
                default: return Expression.Equal(property, value);
            }
        }

        public Expression Visit(Between between)
        {
            var property = Property(between.PropertyName);
            var starts = Constant(between.Starts, property.Type);
            var ends = Constant(between.Ends, property.Type);

            return Expression.AndAlso(
                Expression.GreaterThanOrEqual(property, starts),
                Expression.LessThanOrEqual(property, ends));
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

            return Expression.Call(containsMethod, Expression.Constant(typedValues), property);
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
            var value = Expression.Constant(textMatch.Value);

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

        private static ConstantExpression Constant(object value, Type targetType)
            => Expression.Constant(ValueCoercion.To(value, targetType), targetType);

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
