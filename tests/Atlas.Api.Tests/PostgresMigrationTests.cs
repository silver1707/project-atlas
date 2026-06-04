using FluentAssertions;
using Npgsql;
using Testcontainers.PostgreSql;

namespace Atlas.Api.Tests;

public sealed class PostgresMigrationTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder()
        .WithImage("postgres:17")
        .WithDatabase("atlas_tests")
        .WithUsername("atlas")
        .WithPassword("atlas_test_password")
        .Build();

    public async Task InitializeAsync() => await _postgres.StartAsync();

    public async Task DisposeAsync() => await _postgres.DisposeAsync();

    [Fact]
    public async Task Initial_migration_creates_rls_partitioned_tables_and_seed_data()
    {
        await using var connection = new NpgsqlConnection(_postgres.GetConnectionString());
        await connection.OpenAsync();

        var root = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));
        var migration = await File.ReadAllTextAsync(Path.Combine(root, "db", "migrations", "001_initial.sql"));
        var seed = await File.ReadAllTextAsync(Path.Combine(root, "db", "migrations", "002_seed.sql"));

        await using (var command = new NpgsqlCommand(migration, connection))
        {
            await command.ExecuteNonQueryAsync();
        }

        await using (var command = new NpgsqlCommand(seed, connection))
        {
            await command.ExecuteNonQueryAsync();
        }

        await using var check = new NpgsqlCommand("""
            select
              to_regclass('catalog.products') is not null as has_products,
              to_regclass('inventory.stock_movements_2026') is not null as has_stock_partition,
              to_regclass('audit.audit_records_2026') is not null as has_audit_partition,
              (select count(*) from catalog.products) as seeded_products
            """, connection);

        await using var reader = await check.ExecuteReaderAsync();
        await reader.ReadAsync();
        reader.GetBoolean(0).Should().BeTrue();
        reader.GetBoolean(1).Should().BeTrue();
        reader.GetBoolean(2).Should().BeTrue();
        reader.GetInt64(3).Should().BeGreaterThanOrEqualTo(3);
    }
}
