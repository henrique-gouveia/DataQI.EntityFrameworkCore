namespace DataQI.EntityFrameworkCore.Query
{
    public interface IEntityExpressionBuilder
    {
        string Build(IEntityCommandBuilder commandBuilder);
    }
}