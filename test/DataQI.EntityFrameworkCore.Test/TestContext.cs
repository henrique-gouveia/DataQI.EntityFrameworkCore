using System.Linq;
using Microsoft.EntityFrameworkCore;
using DataQI.EntityFrameworkCore.Test.Repository.Persons;

namespace DataQI.EntityFrameworkCore.Test
{
    public class TestContext : DbContext
    {
        public TestContext(DbContextOptions options) : base(options)
        {
            
        }

        public void ClearPersons()
        {
            Persons.RemoveRange(Persons.ToList());
        }

        public DbSet<Person> Persons { get; private set; }
    }
}