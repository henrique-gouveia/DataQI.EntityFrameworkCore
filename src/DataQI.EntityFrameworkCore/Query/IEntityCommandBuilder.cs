using DataQI.Commons.Query;

namespace DataQI.EntityFrameworkCore.Query
{
    public interface IEntityCommandBuilder
    {
        IEntityCommandBuilder AddExpression(IEntityExpressionBuilder expression);

        string AddExpressionValue(object value);

        IEntityCommandBuilder AddOrder(IOrderCriterion order);

        EntityCommand Build();
    }
}