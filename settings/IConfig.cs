namespace CollabVMBot.settings;

public class IConfig
{
    public string Username { get; set; }
    public string[] KnownBots { get; set; }
    public string IPInfoToken { get; set; }
    public int ConnectRetryMaxSeconds { get; set; }
    public ConfigDiscord Discord { get; set; }
    public ConfigAPI API { get; set; }
    public ConfigDatabase Database { get; set; }
    public ConfigVM[] VMs { get; set; }
    public ConfigFilter[] Filters { get; set; }
}

public class ConfigDiscord
{
    public string Token { get; set; }
    public ulong ReportChannel { get; set; }
    public ulong ReportPingRole { get; set; }
    public ulong[] ModRoles { get; set; }
}

public class ConfigAPI
{
    public int HttpPort { get; set; }
    public string ModAPIPassword { get; set; }
}
public class ConfigVM
{
    public string Name { get; set; }
    public string URL { get; set; }
    public string Node { get; set; }
    public string? Password { get; set; }
    public string? Token { get; set; }
    public ulong[]? DiscordMods { get; set; }
}

public class ConfigDatabase
{
    public string Host { get; set; }
    public string Username { get; set; }
    public string Password { get; set; }
    public string Database { get; set; }
}

public class ConfigFilter
{
    public string Description { get; set; }
    public bool CheckUsername { get; set; }
    public bool CheckMessage { get; set; }
    public string Regex { get; set; }
    public string Punishment { get; set; }
}