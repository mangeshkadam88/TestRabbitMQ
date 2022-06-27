using Entities;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure
{
    public class HashesContext : DbContext
    {
        //public HashesContext(DbContextOptions<HashesContext> options) : base(options) { }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlServer(@"Data Source=(localdb)\MSSQLLocalDB;Initial Catalog=hashes;Integrated Security=True;Connect Timeout=30;Encrypt=False;TrustServerCertificate=False;ApplicationIntent=ReadWrite;MultiSubnetFailover=False");
        }

        public DbSet<Hashes> hash { get; set; }
    }
}