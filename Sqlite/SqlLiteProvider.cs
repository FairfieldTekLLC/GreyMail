using Microsoft.Data.Sqlite;

namespace GreyMail.Sqlite;

public static class SqlLiteProvider
{
    private const string ConnectionString = "Data Source=hello.db";

    public static void SaveMessageId(string messageId)
    {
        using SqliteConnection connection = new SqliteConnection(ConnectionString);
        connection.Open();
        SqliteCommand insertCommand = connection.CreateCommand();
        insertCommand.CommandText = "INSERT INTO emails (messageId) VALUES (@messageId)";
        insertCommand.Parameters.AddWithValue("@messageId", messageId);
        insertCommand.ExecuteNonQuery();
        connection.Close();
    }

    public static void SaveWhiteList(string domain)
    {
        if (IsWhiteListed(domain.ToLower()))
            return;

        using SqliteConnection connection = new SqliteConnection(ConnectionString);
        connection.Open();
        SqliteCommand insertCommand = connection.CreateCommand();
        insertCommand.CommandText = "INSERT INTO whitelist (domain) VALUES (@domain)";
        insertCommand.Parameters.AddWithValue("@domain", domain.ToLower());
        insertCommand.ExecuteNonQuery();
        connection.Close();
    }

    public static bool IsWhiteListed(string domain)
    {
        using SqliteConnection connection = new SqliteConnection(ConnectionString);
        try
        {
            connection.Open();
            SqliteCommand selectCommand = connection.CreateCommand();
            selectCommand.CommandText = "SELECT domain FROM whitelist where domain = @domain";
            selectCommand.Parameters.AddWithValue("@domain", domain.ToLower());
            using SqliteDataReader reader = selectCommand.ExecuteReader();
            return reader.Read();
        }
        catch (Exception )
        {
            return false;
        }
        finally
        {
            connection.Close();
        }
    }

    public static bool CheckIfMessageProcessed(string messageId)
    {
        using SqliteConnection connection = new SqliteConnection(ConnectionString);
        try
        {
            connection.Open();
            SqliteCommand selectCommand = connection.CreateCommand();
            selectCommand.CommandText = "SELECT messageId FROM emails where messageId = @messageId";
            selectCommand.Parameters.AddWithValue("@messageId", messageId);
            using SqliteDataReader reader = selectCommand.ExecuteReader();
            return reader.Read();
        }
        catch (Exception )
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
        using SqliteConnection connection = new SqliteConnection(ConnectionString);
        connection.Open();
        SqliteCommand createTableCommand = connection.CreateCommand();
        createTableCommand.CommandText =
            """
                CREATE TABLE IF NOT EXISTS emails (
                    id INTEGER PRIMARY KEY,
                    messageId TEXT
                )
            """;
        createTableCommand.ExecuteNonQuery();
        createTableCommand = connection.CreateCommand();
        createTableCommand.CommandText =
            """
                CREATE TABLE IF NOT EXISTS whitelist (
                    id INTEGER PRIMARY KEY,
                    domain TEXT
                )
            """;
        createTableCommand.ExecuteNonQuery();
        connection.Close();
    }
}