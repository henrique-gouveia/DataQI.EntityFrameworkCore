namespace DataQI.EntityFrameworkCore.Benchmarks.Support;

public static class DatabaseEnsurer
{
    public static void Execute()
    {
        using var connection = ConnectionFactory.NewConnection();
        using var commad = connection.CreateCommand();
        commad.CommandText = @"
If (Object_Id('entities') Is Null)
Begin
	Create Table Entities
	(
		[id] bigint identity primary key, 
		[name] varchar(max) not null, 
		[Created_at] datetime not null
	);
End
";
        commad.ExecuteNonQuery();
    }
}