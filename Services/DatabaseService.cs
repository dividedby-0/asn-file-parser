namespace AsnFileParser.Services;

using System.Threading.Tasks;
using Dapper;
using System;
using Microsoft.Data.SqlClient;
using AsnFileParser.Models;

public class DatabaseService(string connectionString)
{
    private readonly string _connectionString = connectionString;

    public async Task<int> InsertBoxAsync(Box box)
    {
        await using var connection = new SqlConnection(_connectionString);

        try
        {
            await connection.OpenAsync();
        }
        catch (SqlException ex)
        {
            Console.WriteLine($"Error connecting to database: {ex.Message}");
        }

        try
        {
            var boxId = await connection.ExecuteScalarAsync<int>(
                "INSERT INTO Boxes (SupplierIdentifier, CartonBoxIdentifier) OUTPUT INSERTED.Id VALUES (@SupplierIdentifier, @CartonBoxIdentifier)",
                new { box.SupplierIdentifier, box.CartonBoxIdentifier }
            );

            foreach (var content in box.Contents)
            {
                await connection.ExecuteAsync(
                    "INSERT INTO BoxContents (BoxId, PoNumber, ISBN, Quantity) VALUES (@BoxId, @PoNumber, @Isbn, @Quantity)",
                    new { BoxId = boxId, content.PoNumber, content.Isbn, content.Quantity }
                );
            }

            return boxId;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error writing to database: {ex.Message}");
            throw;
        }
    }

    public async Task LogFileProcessedAsync(string fileName)
    {
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();

        await connection.ExecuteAsync(
            "INSERT INTO FileProcessingLog (FileName) VALUES (@FileName)",
            new { FileName = fileName });
    }
}