using MySqlConnector;
using CollabVMBot.logs;
using CollabVMBot.settings;
using System.Net;

namespace CollabVMBot;

public class Database
{
    private readonly string connstr;

    public Database(ConfigDatabase config)
    {
        connstr = new MySqlConnectionStringBuilder
        {
            Server = config.Host,
            UserID = config.Username,
            Password = config.Password,
            Database = config.Database,
        }.ToString();
    }

    public async Task initAsync()
    {
        await using var db = new MySqlConnection(connstr);
        await db.OpenAsync();

        using var cmd = db.CreateCommand();

        cmd.CommandText = """
            CREATE TABLE IF NOT EXISTS chatlogs (
            vm TEXT NOT NULL,
            username TEXT NOT NULL,
            message TEXT NOT NULL,
            date TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP
            )
        """;
        await cmd.ExecuteNonQueryAsync();
    }

    public async Task LogChatMessageAsync(string vm, string username, string message)
    {
        await using var db = new MySqlConnection(connstr);
        await db.OpenAsync();

        await using var cmd = db.CreateCommand();

        cmd.CommandText = "INSERT INTO chatlogs (vm, username, message) VALUES (@vm, @username, @message)";
        cmd.Parameters.AddWithValue("@vm", vm);
        cmd.Parameters.AddWithValue("@username", username);
        cmd.Parameters.AddWithValue("@message", message);

        await cmd.ExecuteNonQueryAsync();
    }

    public async Task LogIPAsync(string vm, string username, IPAddress ip)
    {
        await using var db = new MySqlConnection(connstr);
        await db.OpenAsync();
        await using var cmd = db.CreateCommand();
        cmd.CommandText = "SELECT COUNT(ip) FROM iplog WHERE ip = @ip AND date >= DATE_SUB(NOW(), INTERVAL 1 HOUR) AND vm = @vm AND username = @username";
        cmd.Parameters.AddWithValue("@ip", ip.GetAddressBytes());
        cmd.Parameters.AddWithValue("@vm", vm);
        cmd.Parameters.AddWithValue("@username", username);
        var count = (long)await cmd.ExecuteScalarAsync();
        if (count > 0)
            return;
        cmd.CommandText = "INSERT INTO iplog (vm, username, ip) VALUES (@vm, @username, @ip)";
        await cmd.ExecuteNonQueryAsync();
    }


    public async Task<LoggedChatMessage[]> GetChatlogsAsync(ChatlogQuery q)
    {
        await using var db = new MySqlConnection(connstr);
        await db.OpenAsync();
        await using var cmd = db.CreateCommand();
        cmd.CommandText = "SELECT * FROM chatlogs";
        List<string> where = new();
        if (q.VM != null)
        {
            where.Add("vm = @vm");
            cmd.Parameters.AddWithValue("@vm", q.VM);
        }
        if (q.Username != null)
        {
            where.Add("username = @username");
            cmd.Parameters.AddWithValue("@username", q.Username);
        }
        if (q.FromTimestamp != null)
        {
            where.Add("date >= @from");
            cmd.Parameters.AddWithValue("@from", q.FromTimestamp);
        }
        if (q.ToTimeStamp != null)
        {
            where.Add("date <= @to");
            cmd.Parameters.AddWithValue("@to", q.ToTimeStamp);
        }
        if (q.Regex != null)
        {
            where.Add("message REGEXP @regex");
            cmd.Parameters.AddWithValue("@regex", q.Regex);
        }
        if (q.CustomWhere != null)
            where.AddRange(q.CustomWhere);
        if (where.Count > 0)
        {
            cmd.CommandText += " WHERE " + string.Join(" AND ", where);
        }
        if (q.Count != null)
        {
            cmd.CommandText += $" ORDER BY {(q.Random ? "RAND()" : "date DESC")} LIMIT @count";
            cmd.Parameters.AddWithValue("@count", q.Count);
        }
        await using var reader = await cmd.ExecuteReaderAsync();
        List<LoggedChatMessage> logs = new();
        while (await reader.ReadAsync())
        {
            logs.Add(new LoggedChatMessage
            {
                VM = reader.GetString(0),
                Username = reader.GetString(1),
                Message = reader.GetString(2),
                Timestamp = reader.GetDateTime(3).ToString("yyyy-MM-dd HH:mm:ss")
            });
        }
        if (q.Count != null)
            logs.Reverse();
        return logs.ToArray();
    }

    public async Task<LoggedIP[]> GetIPFromUsernameAsync(string username)
    {
        await using var db = new MySqlConnection(connstr);
        await db.OpenAsync();
        await using var cmd = db.CreateCommand();
        cmd.CommandText = "SELECT * FROM iplog WHERE username = @username";
        cmd.Parameters.AddWithValue("@username", username);
        await using var reader = await cmd.ExecuteReaderAsync();
        List<LoggedIP> logs = new();
        while (await reader.ReadAsync())
        {
            logs.Add(new LoggedIP
            {
                VM = reader.GetString(0),
                Username = reader.GetString(1),
                IP = new IPAddress(reader.GetFieldValue<byte[]>(2)).ToString(),
                Timestamp = reader.GetDateTime(3).ToString("yyyy-MM-dd HH:mm:ss")
            });
        }
        return logs.ToArray();
    }
    
    public async Task<LoggedIP[]> GetUsernameFromIPAsync(IPAddress ip)
    {
        await using var db = new MySqlConnection(connstr);
        await db.OpenAsync();
        await using var cmd = db.CreateCommand();
        cmd.CommandText = "SELECT * FROM iplog WHERE ip = @ip";
        cmd.Parameters.AddWithValue("@ip", ip.GetAddressBytes());
        await using var reader = await cmd.ExecuteReaderAsync();
        List<LoggedIP> logs = new();
        while (await reader.ReadAsync())
        {  
            logs.Add(new LoggedIP
            {
                VM = reader.GetString(0),
                Username = reader.GetString(1),
                IP = new IPAddress(reader.GetFieldValue<byte[]>(2)).ToString(),
                Timestamp = reader.GetDateTime(3).ToString("yyyy-MM-dd HH:mm:ss")
            });
        }
        return logs.ToArray();
    }

}