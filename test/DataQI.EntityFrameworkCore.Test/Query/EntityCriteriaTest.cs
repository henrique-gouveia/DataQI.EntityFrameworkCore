using System;
using DataQI.Commons.Query.Support;
using DataQI.EntityFrameworkCore.Query.Support;
using Xunit;

namespace DataQI.EntityFrameworkCore.Test.Query
{
    public class EntityCriteriaTest : EntityCommandBaseTest
    {
        private readonly EntityCriteria criteria;

        public EntityCriteriaTest()
        {
            criteria = new EntityCriteria();
        }

        [Fact]
        public void TestRejectsAddNullCriterion()
        {
            var exception = Assert.Throws<ArgumentException>(() =>
                new EntityCriteria().Add(null));
            var exceptionMessage = exception.GetBaseException().Message;

            Assert.IsType<ArgumentException>(exception.GetBaseException());
            Assert.Equal("Criterion must not be null", exceptionMessage);
        }

        [Fact]
        public void TestBuildCommandSimpleExpression()
        {
            var findByParametersExpected = Parameters("fake name");

            var firstNameCriterion = Restrictions.Equal("FirstName", findByParametersExpected[0]);

            criteria.Add(firstNameCriterion);
            var command = criteria.BuildCommand();

            AssertCommand("FirstName == @0", findByParametersExpected, command);
        }

        [Fact]
        public void TestBuildCommandComposedExpressionCorrectly()
        {
            var findByParametersExpected = Parameters("Fake First Name", "Fake Last Name");

            var firstNameCriterion = Restrictions.Equal("FirstName", findByParametersExpected[0]);
            var lastNameCriterion = Restrictions.Equal("LastName", findByParametersExpected[1]);

            criteria
                .Add(firstNameCriterion)
                .Add(lastNameCriterion);
            var command = criteria.BuildCommand();

            AssertCommand("FirstName == @0 && LastName == @1", findByParametersExpected, command);
        }

        [Fact]
        public void TestBuildSimpleConjunctionWithSimpleExpressionCorrectly()
        {
            var findByParametersExpected = Parameters("fake name");

            var junction = Restrictions
                .Conjunction()
                .Add(Restrictions.Equal("FirstName", findByParametersExpected[0]));

            criteria.Add(junction);
            var command = criteria.BuildCommand();

            AssertCommand("(FirstName == @0)", findByParametersExpected, command);
        }

        [Fact]
        public void TestBuildSimpleDisjunctionWithSimpleExpressionCorrectly()
        {
            var findByParametersExpected = Parameters("fake name");

            var junction = Restrictions
                .Disjunction()
                .Add(Restrictions.Equal("FirstName", findByParametersExpected[0]));

            criteria.Add(junction);
            var command = criteria.BuildCommand();

            AssertCommand("(FirstName == @0)", findByParametersExpected, command);
        }

        [Fact]
        public void TestBuildComposedConjunctionWithSimpleExpressionCorrectly()
        {
            var findByParametersExpected = Parameters("Fake First Name", "Fake Last Name");

            var firtNameCriterion = Restrictions.Equal("FirstName", findByParametersExpected[0]);
            var lastNameCriterion = Restrictions.Equal("LastName", findByParametersExpected[1]);    

            var junction1 = Restrictions
                .Conjunction()
                .Add(firtNameCriterion);

            var junction2 = Restrictions
                .Conjunction()
                .Add(lastNameCriterion);

            criteria.Add(junction1).Add(junction2);
            var command = criteria.BuildCommand();

            AssertCommand("(FirstName == @0) && (LastName == @1)", findByParametersExpected, command);
        }

        [Fact]
        public void TestBuildComposedDisjunctionWithSimpleExpressionCorrectly()
        {
            var findByParametersExpected = Parameters("Fake First Name", "Fake Last Name");

            var firtNameCriterion = Restrictions.Equal("FirstName", findByParametersExpected[0]);
            var lastNameCriterion = Restrictions.Equal("LastName", findByParametersExpected[1]);    

            var junction1 = Restrictions
                .Disjunction()
                .Add(firtNameCriterion);

            var junction2 = Restrictions
                .Disjunction()
                .Add(lastNameCriterion);

            criteria.Add(junction1).Add(junction2);
            var command = criteria.BuildCommand();

            AssertCommand("(FirstName == @0) && (LastName == @1)", findByParametersExpected, command);
        }
    }
}