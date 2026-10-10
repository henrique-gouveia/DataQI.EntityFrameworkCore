using System.Collections.Generic;
using System.Linq;

using Microsoft.EntityFrameworkCore;

using DataQI.Commons.Util;
using DataQI.EntityFrameworkCore.Repository.Support;

namespace DataQI.EntityFrameworkCore.Test.Repository.Employees
{
    public class EmployeeRepository : EntityRepository<Employee, int>
    {
        public EmployeeRepository(DbContext context) : base(context)
        { }

        public void InsertDepartment(Department department)
        {
            Assert.NotNull(department, "Department must not be null");
            context.Add(department);
        }

        public IEnumerable<Employee> FindByDepartmentName(string name)
        {
            var employees = context
                .Set<Employee>()
                .Include(e => e.Department)
                .Where(employee => employee.Department.Name == name)
                .AsNoTracking()
                .ToList();

            return employees;
        }
    }
}
