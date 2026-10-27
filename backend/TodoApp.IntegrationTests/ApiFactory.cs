using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.PostgreSql;

/// <summary>
/// Runs the real API in memory, connected to a real, throwaway PostgreSQL in Docker.
/// </summary>
/// <remarks>
/// Before the tests: start a Postgres container, point the API at it, and create the tables
/// with the real migrations. After the tests: the container is deleted. Needs Docker running.
/// </remarks>
public class ApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly PostgreSqlContainer _database = new PostgreSqlBuilder("postgres:17-alpine").Build();

    /// <summary>Starts the database and creates the tables, before any test runs.</summary>
    public async Task InitializeAsync()
    {
        await _database.StartAsync();

        // Same setting name the API reads everywhere else. Environment variables win over
        // User Secrets, so the API under test uses this container, never your real database.
        Environment.SetEnvironmentVariable("ConnectionStrings__TodoDb", _database.GetConnectionString());

        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<TodoDbContext>();
        await db.Database.MigrateAsync();
    }

    /// <summary>Stops and deletes the database container after the tests.</summary>
    public new async Task DisposeAsync()
    {
        await base.DisposeAsync();
        await _database.DisposeAsync();
    }
}
