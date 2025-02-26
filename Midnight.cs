using System.Runtime.InteropServices;
using CollabVMBot.settings;
using CollabVMBot.utils;
using CollabVMBot.VMStuff;

using IPinfo;
using Tomlet;

namespace CollabVMBot;

class Midnight
{
    public static IConfig Config { get; private set; }
    public static Database Database { get; private set; }
    public static IPinfoClient IPinfo { get; private set; }
    public static DiscordBot Discord { get; private set; }
    public static Filter[] Filters { get; private set; }
    public static VM[] VMs { get; private set; }

    private static CancellationTokenSource cts = new();
    private static HTTPServer HTTP;

    static async Task Main(string[] args)
    {
        LogManager.Log(LogLevel.INFO, "Midnight is starting up...");

        string configraw;

        try
        {
            configraw = File.ReadAllText("config.toml");
        }
        catch (Exception e)
        {
            LogManager.Log(LogLevel.FATAL, $"Failed to read config.toml: {e.Message}");
            Environment.Exit(1);
            return;
        }

        try
        {
            Config = TomletMain.To<IConfig>(configraw);
        }
        catch (Exception e)
        {
            LogManager.Log(LogLevel.FATAL, $"Failed to parse config.toml: {e.Message}");
            Environment.Exit(1);
            return;
        }

        Console.CancelKeyPress += (_, _) => Exit();
        PosixSignalRegistration.Create(PosixSignal.SIGTERM, _ => Exit());

        IPinfo = new IPinfoClient.Builder().AccessToken(Config.IPInfoToken).Build();

        Filters = (Config.Filters?.Length > 0) ? Config.Filters.Select(f => new Filter(f)).ToArray() : [];
        
        Database = new Database(Config.Database);
        await Database.initAsync();
        LogManager.Log(LogLevel.INFO, "Connected to MySQL Database");

        HTTP = new HTTPServer();
        var t = HTTP.RunAsync();

        Discord = new DiscordBot(Config.Discord.Token, Config.Discord.ReportChannel);
        await Discord.Connect();

        VMs = Config.VMs.Select(vm => new VM(Config.Username, vm, Database)).ToArray();
        var _t = new List<Task>();

        foreach(var vm in VMs)
        {
            vm.MessageIncident += (_, i) => Discord.FlagChatMessageAsync(i);
            vm.UsernameIncident += (_, i) => Discord.FlagUsernameAsync(i);
            _t.Add(vm.OpenAsync());
        }
        await Task.WhenAll(_t);
        await t;
    }

    public static async Task Exit()
    {
        var t = new List<Task>();
        t.Add(HTTP.StopAsync());
        t.Add(Discord.Disconnect());
        t.AddRange(VMs.Select(vm => vm.CloseAsync()).ToArray());

        await Task.WhenAll(t);
        await cts.CancelAsync();

        Environment.Exit(0);
    }
}