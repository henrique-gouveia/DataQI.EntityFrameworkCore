namespace DataQI.EntityFrameworkCore.Query
{
    public interface IEntityCommandBuilder
    {
        IEntityCommandBuilder AddExpression(IEntityExpressionBuilder expression);

        string AddExpressionValue(object value);

        EntityCommand Build();
    }
}