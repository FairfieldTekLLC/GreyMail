using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;

namespace GreyMail
{
    public static class SqlLiteProvider
    {
        const string connectionString = "Data Source=hello.db";

        public static void SaveMessageId(string messageId)
        {
            using var connection = new SqliteConnection(connectionString);
            connection.Open();
            var insertCommand = connection.CreateCommand();
            insertCommand.CommandText = "INSERT INTO emails (messageId) VALUES (@messageId)";
            insertCommand.Parameters.AddWithValue("@messageId", messageId);
            insertCommand.ExecuteNonQuery();
            connection.Close();
        }

        public static bool CheckIfMessageProcessed(string messageId)
        {
            using var connection = new SqliteConnection(connectionString);
            try
            {
                connection.Open();
                var selectCommand = connection.CreateCommand();
                selectCommand.CommandText = "SELECT messageId FROM emails where messageId = @messageId";
                selectCommand.Parameters.AddWithValue("@messageId", messageId);
                using var reader = selectCommand.ExecuteReader();
                return reader.Read();
            }
            catch (Exception e)
            {
                return false;
            }
            finally
            {
                connection.Close();
            }
            
            
        }




        public static void Initialize()
        {
            using var connection = new SqliteConnection(connectionString);
            connection.Open();
            var createTableCommand = connection.CreateCommand();
            createTableCommand.CommandText =
                """
                    CREATE TABLE IF NOT EXISTS emails (
                        id INTEGER PRIMARY KEY,
                        messageId TEXT
                    )
                """;
            createTableCommand.ExecuteNonQuery();
            connection.Close();
        }

    }
}
