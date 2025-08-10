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

                CREATE TABLE IF NOT EXISTS OrderItems (
                        Id INTEGER PRIMARY KEY AUTOINCREMENT,
                        OrderId INTEGER NOT NULL,
                        ProductId INTEGER NOT NULL,
                        Quantity INTEGER NOT NULL,
                        UnitPrice DECIMAL(10,2) NOT NULL,
                        TotalPrice DECIMAL(10,2) NOT NULL,
                        FOREIGN KEY (OrderId) REFERENCES Orders(Id) ON DELETE CASCADE,
                        FOREIGN KEY (ProductId) REFERENCES Products(Id) ON DELETE CASCADE
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

            using var transaction = connection.BeginTransaction();

            try
            {
                // Insert into Orders table
                var orderCommand = connection.CreateCommand();
                orderCommand.Transaction = transaction;
                orderCommand.CommandText = @"
                        INSERT INTO Orders (OrderDate, IsPaid, TotalAmount, Name) 
                        VALUES (@orderDate, @isPaid, @totalAmount, @Name);
                        SELECT last_insert_rowid();";

                orderCommand.Parameters.AddWithValue("@orderDate", order.OrderDate.ToString("s"));
                orderCommand.Parameters.AddWithValue("@isPaid", order.IsPaid ? 1 : 0);
                orderCommand.Parameters.AddWithValue("@totalAmount", order.TotalAmount);
                orderCommand.Parameters.AddWithValue("@Name", string.IsNullOrEmpty(order.Name) ? DBNull.Value : order.Name);

                var orderId = Convert.ToInt32(await orderCommand.ExecuteScalarAsync());
                order.Id = orderId;

                // Insert corresponding OrderItems
                foreach (var item in order.Items)
                {
                    var itemCommand = connection.CreateCommand();
                    itemCommand.Transaction = transaction;
                    itemCommand.CommandText = @"
                                    INSERT INTO OrderItems (OrderId, ProductId, Quantity, UnitPrice, TotalPrice)
                                    VALUES (@orderId, @productId, @quantity, @unitPrice, @totalPrice);";

                    itemCommand.Parameters.AddWithValue("@orderId", orderId);
                    itemCommand.Parameters.AddWithValue("@productId", item.ProductId);
                    itemCommand.Parameters.AddWithValue("@quantity", item.Quantity);
                    itemCommand.Parameters.AddWithValue("@unitPrice", item.UnitPrice);
                    itemCommand.Parameters.AddWithValue("@totalPrice", item.Total);

                    await itemCommand.ExecuteNonQueryAsync();
                }

                // Commit the transaction
                await transaction.CommitAsync();

                return orderId;
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }


        public async Task<List<Order>> LoadOrdersByDateAsync(DateTime date)
        {
            using var connection = new SqliteConnection($"Data Source={_databasePath}");
            await connection.OpenAsync();

            var dateString = date.Date.ToString("yyyy-MM-dd");

            var orderCmd = connection.CreateCommand();
            orderCmd.CommandText = @"
                            SELECT Id, OrderDate, IsPaid, TotalAmount, Name
                            FROM Orders
                            WHERE date(OrderDate) = date(@selectedDate)
                            ORDER BY OrderDate DESC;";
            orderCmd.Parameters.AddWithValue("@selectedDate", dateString);

            var orders = new List<Order>();

            using var reader = await orderCmd.ExecuteReaderAsync();
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

        public async Task<List<OrderItem>> GetOrderItemsAsync(int orderId)
        {
            using var connection = new SqliteConnection($"Data Source={_databasePath}");
            await connection.OpenAsync();

            var cmd = connection.CreateCommand();
            cmd.CommandText = @"
                            SELECT oi.Id, oi.ProductId, oi.Quantity, oi.UnitPrice, oi.TotalPrice,
                                   p.Name, p.Price, p.Category, p.ImagePath
                            FROM OrderItems oi
                            LEFT JOIN Products p ON p.Id = oi.ProductId
                            WHERE oi.OrderId = @orderId;";

            cmd.Parameters.AddWithValue("@orderId", orderId);

            var items = new List<OrderItem>();

            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                var item = new OrderItem
                {
                    Id = reader.GetInt32(0),
                    OrderId = orderId,
                    ProductId = reader.GetInt32(1),
                    Quantity = reader.GetInt32(2),
                    UnitPrice = reader.GetDecimal(3)
                };

                if (!reader.IsDBNull(5))
                {
                    item.Product = new Product
                    {
                        Id = item.ProductId,
                        Name = reader.IsDBNull(5) ? null : reader.GetString(5),
                        Price = reader.IsDBNull(6) ? 0 : reader.GetDecimal(6),
                        Category = reader.IsDBNull(7) ? null : reader.GetString(7),
                        ImagePath = reader.IsDBNull(8) ? null : reader.GetString(8)
                    };
                }

                items.Add(item);
            }

            return items;
        }


    }
}