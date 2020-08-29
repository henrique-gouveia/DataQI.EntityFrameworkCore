using System;
using DataQI.Commons.Query;
using DataQI.Commons.Query.Support;

using DataQI.EntityFrameworkCore.Query.Support;

namespace DataQI.EntityFrameworkCore.Query.Extensions
{
    public static class EntityCriterionExtensions
    {
        public static string GetCommandTemplate(this ICriterion criterion)
        {
            switch (criterion.GetWhereOperator())
            {
                case WhereOperator.Between:
                    return "{0} >= @{1} And {0} <= @{2}";
                case WhereOperator.Containing:
                case WhereOperator.Like:
                    return "{0}.Contains(@{1})";
                case WhereOperator.EndingWith:
                    return "{0}.EndsWith(@{1})";
                case WhereOperator.StartingWith:
                    return "{0}.StartsWith(@{1})";
                case WhereOperator.GreaterThan:
                    return "{0} > @{1}";
                case WhereOperator.GreaterThanEqual:
                    return "{0} >= @{1}";
                case WhereOperator.In:
                    return "@{0}.Contains({1})";
                case WhereOperator.LessThan:
                    return "{0} < @{1}";
                case WhereOperator.LessThanEqual:
                    return "{0} <= @{1}";
                case WhereOperator.Not:
                    return "";
                case WhereOperator.Null:
                    return "{0} == null";
                case WhereOperator.And:
                case WhereOperator.Or:
                    return "({0})";
                default:
                    return "{0} == @{1}";
            }
        }

        public static string GetLogicalOperator(this ICriterion criterion)
        {
            switch (criterion.GetWhereOperator())
            {
                case WhereOperator.And:
                    return " && ";
                case WhereOperator.Or:
                    return " || ";
                default:
                    throw new NotImplementedException();
            }
        }

        public static string GetNotOperator(this ICriterion criterion)
        {
            switch (criterion.GetWhereOperator())
            {
                case WhereOperator.Between:
                case WhereOperator.StartingWith:
                case WhereOperator.EndingWith:
                case WhereOperator.Containing:
                case WhereOperator.Like:
                case WhereOperator.In:
                case WhereOperator.Null:
                case WhereOperator.Equal:
                    return "!";
                default:
                    return "";
            }
        }

        public static IEntityExpressionBuilder GetExpressionBuilder(this ICriterion criterion)
        {
            switch (criterion.GetWhereOperator())
            {
                case WhereOperator.Between:
                    return new EntityBetweenExpression(criterion as BetweenExpression);
                case WhereOperator.In:
                    return new EntityInExpression(criterion as InExpression);
                case WhereOperator.Not:
                    return new EntityNotExpression(criterion as NotExpression);
                case WhereOperator.Null:
                    return new EntityNullExpression(criterion as NullExpression);
                case WhereOperator.And:
                case WhereOperator.Or:
                    return new EntityJunctionExpression(criterion as Junction);
                default:
                    return new EntitySimpleExpression(criterion as SimpleExpression);
            }
        }
    }
}