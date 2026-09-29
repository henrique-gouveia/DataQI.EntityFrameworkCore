namespace DataQI.EntityFrameworkCore.Query
{
    public struct EntityCommand
    {
        public EntityCommand(string command, object[] values, string orderBy = null)
        {
            Command = command;
            Values = values;
            OrderBy = orderBy;
        }

        public string Command { get; private set; }

        public object[] Values { get; private set; }

        public string OrderBy { get; private set; }
    }
}