using Microsoft.EntityFrameworkCore;
using testAPI;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var configuration = new ConfigurationBuilder()
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json")
    .Build();

var connectionString = configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(connectionString));

var app = builder.Build();


if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();



app.MapGet("/api/users", async (AppDbContext context) =>
{
    try
    {
        var users = await context.Users.ToListAsync();
        return Results.Ok(users);
    }
    catch (Exception ex)
    {
        return Results.BadRequest($"Error getting users: {ex.Message}");
    }
});


app.MapPost("/api/user/AddColumns", async (List<string> columnNames, AppDbContext context) =>
{
    try
    {
        // Get the existing columns of the "Users" table
        var existingColumns = await GetTableColumns("Users", context);

        foreach (var columnName in columnNames)
        {
            // Check if the column does not exist, then add it to the table
            if (!existingColumns.Contains(columnName, StringComparer.OrdinalIgnoreCase))
            {
                await context.Database.ExecuteSqlRawAsync($"ALTER TABLE Users ADD {columnName} NVARCHAR(MAX) NULL");
            }
            else
            {
                
            }
        }

        await context.SaveChangesAsync();
        return Results.Ok("Columns added successfully");
    }
    catch (Exception ex)
    {
        return Results.BadRequest($"Error adding columns: {ex.Message}");
    }
});

app.MapGet("/api/table/GetColumns", async (AppDbContext context) =>
{
    try
    {
        var tableName = "Users";
        // Retrieve the columns of the specified table from the database
        var columns = await GetTableColumns(tableName, context);
        return Results.Ok(columns);
    }
    catch (Exception ex)
    {
        return Results.BadRequest($"Error getting columns: {ex.Message}");
    }
});

app.MapDelete("/api/table/RemoveColumn", async (string columnName, AppDbContext context) =>
{
    try
    {
        var tableName = "Users";
        var existingColumns = await GetTableColumns(tableName, context);

        if (existingColumns.Contains(columnName, StringComparer.OrdinalIgnoreCase))
        {
            await context.Database.ExecuteSqlRawAsync($"ALTER TABLE {tableName} DROP COLUMN {columnName}");
            await context.SaveChangesAsync();
            return Results.Ok($"Column '{columnName}' removed successfully");
        }
        else
        {
            return Results.BadRequest($"Column '{columnName}' does not exist");
        }
    }
    catch (Exception ex)
    {
        return Results.BadRequest($"Error removing column: {ex.Message}");
    }
});


app.MapPut("/api/table/RenameColumn", async (string oldColumnName, string newColumnName, AppDbContext context) =>
{
    try
    {
        var tableName = "Users";
        var existingColumns = await GetTableColumns(tableName, context);

        if (existingColumns.Contains(oldColumnName, StringComparer.OrdinalIgnoreCase))
        {
            
            await context.Database.ExecuteSqlRawAsync($"EXEC sp_rename '{tableName}.{oldColumnName}', '{newColumnName}', 'COLUMN'");
            await context.SaveChangesAsync();
            return Results.Ok($"Column '{oldColumnName}' renamed to '{newColumnName}' successfully");
        }
        else
        {
            return Results.BadRequest($"Column '{oldColumnName}' does not exist");
        }
    }
    catch (Exception ex)
    {
        return Results.BadRequest($"Error renaming column: {ex.Message}");
    }
});

app.Run();

async Task<List<string>> GetTableColumns(string tableName, AppDbContext context)
{
    var columns = new List<string>();

    var connection = context.Database.GetDbConnection();
    await connection.OpenAsync();

    using (var command = connection.CreateCommand())
    {
        command.CommandText = $"SELECT COLUMN_NAME FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = '{tableName}'";
        using (var reader = await command.ExecuteReaderAsync())
        {
            while (await reader.ReadAsync())
            {
               
                var columnName = reader.GetString(0)?.ToString(System.Globalization.CultureInfo.InvariantCulture);
                columns.Add(columnName);
            }
        }
    }

    return columns;
}
