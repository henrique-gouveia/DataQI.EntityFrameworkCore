using System.Collections.Generic;
using DataQI.EntityFrameworkCore.Repository;

namespace DataQI.EntityFrameworkCore.Test.Repository.Employees
{
    public interface IEmployeeRepository : IEntityRepository<Employee, int>
    {
        void InsertDepartment(Department department);
        IEnumerable<Employee> FindByDepartmentName(string name);
    }
}
