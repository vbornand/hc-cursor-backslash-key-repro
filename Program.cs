using GreenDonut.Data;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

// Keyset paging sorted on a string column: page 1, then page 2 from its end cursor.
using var connection = new SqliteConnection("Data Source=:memory:");
connection.Open();

var options = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(connection).Options;

await using (var db = new AppDbContext(options))
{
    await db.Database.EnsureCreatedAsync();
    db.Folders.AddRange(
        new Folder { Path = @"C:\data" },
        new Folder { Path = @"C:\temp\" },   // ends with a backslash
        new Folder { Path = @"D:\logs" },
        new Folder { Path = @"E:\backup" });
    await db.SaveChangesAsync();
}

await PageThrough("Sorted on Path, then Id", q => q.OrderBy(f => f.Path).ThenBy(f => f.Id));
await PageThrough("Sorted on Path only (unique)", q => q.OrderBy(f => f.Path));

async Task PageThrough(string title, Func<IQueryable<Folder>, IQueryable<Folder>> order)
{
    Console.WriteLine($@"== {title} (expected: C:\data, C:\temp\, D:\logs, E:\backup)");
    string? after = null;

    try
    {
        for (var i = 1; i <= 4; i++)
        {
            await using var db = new AppDbContext(options);
            var page = await order(db.Folders).ToPageAsync(new PagingArguments { First = 1, After = after });

            if (page.Items.Length == 0)
            {
                Console.WriteLine($"page {i}: <empty>");
                break;
            }

            Console.WriteLine($"page {i}: {page.Items[0].Path}");
            after = page.CreateEndCursor();
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine(ex);
    }

    Console.WriteLine();
}

public sealed class Folder
{
    public int Id { get; set; }

    public string Path { get; set; } = "";
}

public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Folder> Folders => Set<Folder>();
}
