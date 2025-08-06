using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Threading.Tasks;
using Windows.Storage;

namespace POSApp
{
    public class DatabaseService
    {
        private string _databasePath;

        public DatabaseService()
        {
            InitializeDatabase();
        }

        private async void InitializeDatabase()
        {
            var localFolder = ApplicationData.Current.LocalFolder;
            _databasePath = Path.Combine(localFolder.Path, "pos_database.db");

            using var connection = new SqliteConnection($"Data Source={_databasePath}");
            await connection.OpenAsync();

            var createTableCommand = connection.CreateCommand();

            createTableCommand.CommandText = @"
                CREATE TABLE IF NOT EXISTS Products (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    Name TEXT NOT NULL,
                    Price DECIMAL(10,2) NOT NULL,
                    Category TEXT NOT NULL,
                    ImagePath TEXT
                );
                
              CREATE TABLE IF NOT EXISTS Orders (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    Name TEXT,
                    OrderDate TEXT NOT NULL,
                    IsPaid INTEGER NOT NULL,
                    TotalAmount DECIMAL(10,2) NOT NULL
               );
            ";
            await createTableCommand.ExecuteNonQueryAsync();
        }

        public async Task<List<Product>> GetProductsAsync()
        {
            var products = new List<Product>();

            using var connection = new SqliteConnection($"Data Source={_databasePath}");
            await connection.OpenAsync();

            var command = connection.CreateCommand();
            command.CommandText = "SELECT Id, Name, Price, Category, ImagePath FROM Products ORDER BY Name";

            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                products.Add(new Product
                {
                    Id = reader.GetInt32("Id"),
                    Name = reader.GetString("Name"),
                    Price = reader.GetDecimal("Price"),
                    Category = reader.GetString("Category"),
                    ImagePath = reader.IsDBNull("ImagePath") ? "" : reader.GetString("ImagePath")
                });
            }

            return products;
        }

        public async Task<int> InsertProductAsync(Product product)
        {
            using var connection = new SqliteConnection($"Data Source={_databasePath}");
            await connection.OpenAsync();

            var command = connection.CreateCommand();
            command.CommandText = @"
                INSERT INTO Products (Name, Price, Category, ImagePath) 
                VALUES (@name, @price, @category, @imagePath);
                SELECT last_insert_rowid();";

            command.Parameters.AddWithValue("@name", product.Name);
            command.Parameters.AddWithValue("@price", product.Price);
            command.Parameters.AddWithValue("@category", product.Category ?? "");
            command.Parameters.AddWithValue("@imagePath", product.ImagePath ?? "");

            var id = Convert.ToInt32(await command.ExecuteScalarAsync());
            product.Id = id;
            return id;
        }

        public async Task UpdateProductAsync(Product product)
        {
            using var connection = new SqliteConnection($"Data Source={_databasePath}");
            await connection.OpenAsync();

            var command = connection.CreateCommand();
            command.CommandText = @"
                UPDATE Products 
                SET Name = @name, Price = @price, Category=@category, ImagePath = @imagePath 
                WHERE Id = @id";

            command.Parameters.AddWithValue("@id", product.Id);
            command.Parameters.AddWithValue("@name", product.Name);
            command.Parameters.AddWithValue("@price", product.Price);
            command.Parameters.AddWithValue("@category", product.Category ?? "");
            command.Parameters.AddWithValue("@imagePath", product.ImagePath ?? "");

            await command.ExecuteNonQueryAsync();
        }

        public async Task DeleteProductAsync(int productId)
        {
            using var connection = new SqliteConnection($"Data Source={_databasePath}");
            await connection.OpenAsync();

            var command = connection.CreateCommand();
            command.CommandText = "DELETE FROM Products WHERE Id = @id";
            command.Parameters.AddWithValue("@id", productId);

            await command.ExecuteNonQueryAsync();
        }

        public async Task<Product> GetProductByIdAsync(int id)
        {
            using var connection = new SqliteConnection($"Data Source={_databasePath}");
            await connection.OpenAsync();

            var command = connection.CreateCommand();
            command.CommandText = "SELECT Id, Name, Price, Category, ImagePath FROM Products WHERE Id = @id";
            command.Parameters.AddWithValue("@id", id);

            using var reader = await command.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                return new Product
                {
                    Id = reader.GetInt32("Id"),
                    Name = reader.GetString("Name"),
                    Price = reader.GetDecimal("Price"),
                    Category = reader.GetString("Category"),
                    ImagePath = reader.IsDBNull("ImagePath") ? "" : reader.GetString("ImagePath")
                };
            }

            return null;
        }

        // Insert Order in the Order table
        public async Task<int> InsertOrderAsync(Order order)
        {
            using var connection = new SqliteConnection($"Data Source={_databasePath}");
            await connection.OpenAsync();
            var command = connection.CreateCommand();
            command.CommandText = @"
                INSERT INTO Orders (OrderDate, IsPaid, TotalAmount, Name) 
                VALUES (@orderDate, @isPaid, @totalAmount, @Name);
                SELECT last_insert_rowid();";

            command.Parameters.AddWithValue("@orderDate", order.OrderDate.ToString("s"));
            command.Parameters.AddWithValue("@isPaid", order.IsPaid ? 1 : 0);
            command.Parameters.AddWithValue("@totalAmount", order.TotalAmount);

            command.Parameters.AddWithValue("@Name", string.IsNullOrEmpty(order.Name) ? DBNull.Value : order.Name);


            var id = Convert.ToInt32(await command.ExecuteScalarAsync());
            order.Id = id;
            return id;
        }

        public async Task<List<Order>> LoadOrdersByDateAsync(DateTime date)
        {
            using var connection = new SqliteConnection($"Data Source={_databasePath}");
            await connection.OpenAsync();

            var command = connection.CreateCommand();
            command.CommandText = @"
                        SELECT Id, OrderDate, IsPaid, TotalAmount,Name 
                        FROM Orders
                        WHERE date(OrderDate) = date(@selectedDate)
                        ORDER BY OrderDate DESC;";

            command.Parameters.AddWithValue("@selectedDate", date.Date);

            var orders = new List<Order>();

            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                var order = new Order
                {
                    Id = reader.GetInt32(0),
                    OrderDate = reader.GetDateTime(1),
                    IsPaid = reader.GetInt32(2) == 1,
                    TotalAmount = reader.GetDecimal(3),
                    Name = reader.IsDBNull(4) ? null : reader.GetString(4)
                };
                orders.Add(order);
            }

            return orders;
        }


    }
}