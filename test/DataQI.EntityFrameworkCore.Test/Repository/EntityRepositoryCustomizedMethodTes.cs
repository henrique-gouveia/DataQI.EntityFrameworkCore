using System;
using System.Collections.Generic;
using System.Linq;

using ExpectedObjects;
using Xunit;

using DataQI.EntityFrameworkCore.Test.Fixtures;
using DataQI.EntityFrameworkCore.Test.Repository.Employees;

namespace DataQI.EntityFrameworkCore.Test.Repository
{
    public class EntityRepositoryCustomizedMethodTest : IClassFixture<DbFixture>, IDisposable
    {
        private readonly TestContext employeeContext;

        private readonly IEmployeeRepository employeeRepository;

        public EntityRepositoryCustomizedMethodTest(DbFixture fixture)
        {
            employeeContext = fixture.EmployeeContext;
            employeeRepository = fixture.EmployeeRepository;
        }

        [Theory]
        [InlineData("Production")]
        [InlineData("Sales")]
        public void TestInsertDepartment(string name)
        {
            var countBefore = employeeContext.Departments.Count();
            var countExpected = ++countBefore;

            var department = new Department(name);
            employeeRepository.InsertDepartment(department);

            employeeContext.SaveChanges();

            Assert.True(department.Id > 0);
            Assert.Equal(countExpected, employeeContext.Departments.Count());
        }

        [Fact]
        public void TestFindByDepartmentName()
        {
            var employeeList = InsertTestEmployeesList();
            var employeeEnumerator = employeeList.GetEnumerator();

            while (employeeEnumerator.MoveNext())
            {
                var employee = employeeEnumerator.Current;
                var employeesExpected = employeeList.Where(e => e.Department.Name.StartsWith(employee.Department.Name));

                var products = employeeRepository.FindByDepartmentName(employee.Department.Name);

                employeesExpected.ToExpectedObject().ShouldMatch(products);
            }
        }

        private IList<Employee> InsertTestEmployeesList()
        {
            var productionDepartment = new Department("Production");
            var salesDepartment = new Department("Sales");

            employeeRepository.InsertDepartment(productionDepartment);
            employeeRepository.InsertDepartment(salesDepartment);
            employeeContext.SaveChanges();

            var employees = new List<Employee>()
            {
                EmployeeBuilder.NewInstance().SetDepartment(productionDepartment).Build(),
                EmployeeBuilder.NewInstance().SetDepartment(productionDepartment).Build(),
                EmployeeBuilder.NewInstance().SetDepartment(productionDepartment).Build(),
                EmployeeBuilder.NewInstance().SetDepartment(salesDepartment).Build(),
                EmployeeBuilder.NewInstance().SetDepartment(salesDepartment).Build(),
            };

            employees.ForEach(o => InsertTestEmployee(o));

            return employees;
        }

        private void InsertTestEmployee(Employee employee)
        {
            employeeRepository.Save(employee);
            employeeContext.SaveChanges();

            Assert.True(employeeRepository.Exists(employee.Id));
        }

        #region IDisposable Support
        private bool disposedValue = false;

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                employeeContext.ClearEmployess();
                employeeContext.ClearDepartments();
                employeeContext.SaveChanges();

                disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
        }
        #endregion
    }
}
