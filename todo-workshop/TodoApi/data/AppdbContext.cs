using Microsoft.EntityFrameworkCore;
using TodoApi.Model;

namespace TodoApi.data;

public class AppdbContext : DbContext
{
    public AppdbContext(DbContextOptions<AppdbContext> options) : base(options)
    {

    }

    public DbSet<TodoItem> TodoItems { get; set; }
}
            