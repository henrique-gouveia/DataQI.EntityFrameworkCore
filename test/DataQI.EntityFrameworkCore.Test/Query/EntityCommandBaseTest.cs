using System.Collections.Generic;
using DataQI.EntityFrameworkCore.Query;
using ExpectedObjects;
using Xunit;

namespace DataQI.EntityFrameworkCore.Test.Query
{
    public abstract class EntityCommandBaseTest
    {
        protected object[] Parameters(params object[] parameters)
            => parameters;

        protected void AssertCommand(string commandExpected, object parametersExpected, EntityCommand command)
        {
            Assert.Equal(commandExpected, command.Command);
            parametersExpected.ToExpectedObject().ShouldMatch(command.Values);
        }            
    }
}