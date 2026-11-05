using Azure;
using BuildingApp.Models;
using Microsoft.EntityFrameworkCore;
using System.Windows.Media.Media3D;

namespace BuildingApp.Data;

public class AppDbContext : DbContext
{
    // ⚠️ ВАЖНО: на другом ПК проверь строку подключения!
    // Варианты сервера:
    //   "Server=localhost;..."                  → обычный SQL Server
    //   "Server=.\SQLEXPRESS;..."               → SQL Server Express
    //   "Server=(localdb)\MSSQLLocalDB;..."     → LocalDB
    private const string ConnectionString =
        @"Server=NEKETON;Database=BuildingMaterialsDB;Trusted_Connection=True;TrustServerCertificate=True;";

    public DbSet<Material> Materials => Set<Material>();
    public DbSet<Operation> Operations => Set<Operation>();

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseSqlServer(ConnectionString);
    }
}