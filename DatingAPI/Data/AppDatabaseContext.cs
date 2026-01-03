using DatingContracts;
using Microsoft.EntityFrameworkCore;

namespace DatingAPI.Data
{
    public class AppDatabaseContext : DbContext
    {
        public AppDatabaseContext(DbContextOptions<AppDatabaseContext> options) : base(options) { }

        public DbSet<UserDto> Users { get; set; }
    }
}