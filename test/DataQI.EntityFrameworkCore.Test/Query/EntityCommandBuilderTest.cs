using DataQI.Commons.Query.Support;

using DataQI.EntityFrameworkCore.Query.Extensions;
using DataQI.EntityFrameworkCore.Query.Support;

using Xunit;

namespace DataQI.EntityFrameworkCore.Test.Query
{
    public class EntityCommandBuilderTest : EntityCommandBaseTest
    {
        private readonly EntityCommandBuilder commandBuilder;

        public EntityCommandBuilderTest()
        {
            this.commandBuilder = new EntityCommandBuilder();
        }

        [Fact]
        public void TestBuildSimpleExpressionCorrectly()
        {
            var findByParametersExpected = Parameters("fake name");
            var firstNameCriterion = Restrictions.Equal("FirstName", findByParametersExpected[0]);

            commandBuilder.AddExpression(firstNameCriterion.GetExpressionBuilder());

            AssertCommand("FirstName == @0", findByParametersExpected, commandBuilder.Build());
        }

        [Fact]
        public void TestBuildComposedExpressionCorrectly()
        {
            var findByParametersExpected = Parameters("Fake First Name", "Fake Last Name");

            var firstNameCriterion = Restrictions.Equal("FirstName", findByParametersExpected[0]);
            var lastNameCriterion = Restrictions.Equal("LastName", findByParametersExpected[1]);

            commandBuilder
                .AddExpression(firstNameCriterion.GetExpressionBuilder())
                .AddExpression(lastNameCriterion.GetExpressionBuilder());

            AssertCommand("FirstName == @0 && LastName == @1", findByParametersExpected, commandBuilder.Build());
        }

        [Fact]
        public void TestBuildSimpleConjunctionWithSimpleExpressionCorrectly()
        {
            var findByParametersExpected = Parameters("fake name");

            var junction = Restrictions
                .Conjunction()
                .Add(Restrictions.Equal("FirstName", findByParametersExpected[0]));

            commandBuilder.AddExpression(junction.GetExpressionBuilder());

            AssertCommand("(FirstName == @0)", findByParametersExpected, commandBuilder.Build());
        }

        [Fact]
        public void TestBuildSimpleDisjunctionWithSimpleExpressionCorrectly()
        {
            var findByParametersExpected = Parameters("fake name");

            var junction = Restrictions
                .Disjunction()
                .Add(Restrictions.Equal("FirstName", findByParametersExpected[0]));

            commandBuilder.AddExpression(junction.GetExpressionBuilder());

            AssertCommand("(FirstName == @0)", findByParametersExpected, commandBuilder.Build());
        }

        [Fact]
        public void TestBuildComposedConjunctionWithSimpleExpressionCorrectly()
        {
            var findByParametersExpected = Parameters("Fake First Name", "Fake Last Name");

            var firstNameCriterion = Restrictions.Equal("FirstName", findByParametersExpected[0]);
            var lastNameCriterion = Restrictions.Equal("LastName", findByParametersExpected[1]);    

            var junction1 = Restrictions
                .Conjunction()
                .Add(firstNameCriterion);

            var junction2 = Restrictions
                .Conjunction()
                .Add(lastNameCriterion);

            commandBuilder
                .AddExpression(junction1.GetExpressionBuilder())
                .AddExpression(junction2.GetExpressionBuilder());

            AssertCommand("(FirstName == @0) && (LastName == @1)", findByParametersExpected, commandBuilder.Build());
        }

        [Fact]
        public void TestBuildComposedDisjunctionWithSimpleExpressionCorrectly()
        {
            var findByParametersExpected = Parameters("Fake First Name", "Fake Last Name");

            var firstNameCriterion = Restrictions.Equal("FirstName", findByParametersExpected[0]);
            var lastNameCriterion = Restrictions.Equal("LastName", findByParametersExpected[1]);    

            var junction1 = Restrictions
                .Disjunction()
                .Add(firstNameCriterion);

            var junction2 = Restrictions
                .Disjunction()
                .Add(lastNameCriterion);

            commandBuilder
                .AddExpression(junction1.GetExpressionBuilder())
                .AddExpression(junction2.GetExpressionBuilder());

            AssertCommand("(FirstName == @0) && (LastName == @1)", findByParametersExpected, commandBuilder.Build());
        }
    }
}