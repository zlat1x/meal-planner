using Npgsql;

var connectionString =
    "Host=localhost;Port=5434;Database=meal_planner;Username=meal_user;Password=meal_pass";

try
{
    await using var conn = new NpgsqlConnection(connectionString);
    await conn.OpenAsync();

    await using (var cmd = new NpgsqlCommand("SELECT COUNT(*) FROM users;", conn))
    {
        var count = (long)(await cmd.ExecuteScalarAsync() ?? 0);
        Console.WriteLine($"Connected users count = {count}");
    }
}
catch (Exception ex)
{
    Console.WriteLine("DB connection error");
    Console.WriteLine(ex.Message);
}
