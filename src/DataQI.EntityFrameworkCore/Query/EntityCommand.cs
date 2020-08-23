namespace DataQI.EntityFrameworkCore.Query
{
    public struct EntityCommand
    {
        public EntityCommand(string command, object[] values)
        {
            Command = command;
            Values = values;   
        }

        public string Command { get; private set; }

        public object[] Values { get; private set; }   
    }
}