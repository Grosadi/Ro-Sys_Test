using Npgsql;
using Ro_Sys_Test.Classes;

namespace Ro_Sys_Test
{
    public static class DatabaseHelper
    {                    
        public async static Task<List<Cell>> LoadData(string connectionString, string query)
        {
            var list = new List<Cell>();

            try
            {
                Console.WriteLine("Connecting to database...");

                await using var datasource = NpgsqlDataSource.Create(connectionString);

                await using var cmd = datasource.CreateCommand(query);

                using var reader = await cmd.ExecuteReaderAsync();

                Console.WriteLine("Executing query...");

                while (await reader.ReadAsync())
                {
                    var cell = new Cell()
                    {
                        Value = ((int)reader.GetDouble(0)),
                        Col = reader.GetInt32(1),
                        Row = reader.GetInt32(2),
                        Longtitude = reader.GetDouble(3),
                        Latitude = reader.GetDouble(4)
                    };

                    list.Add(cell);
                }

                Console.WriteLine($"Query completed. Processed rows: {list.Count}");
            }
            catch(Exception ex)
            {
                Console.WriteLine($"Error during database operation: {ex}");
            }          
            
            return list;
        }
    }
}
