using CollabVMBot.settings;
using CollabVMBot.utils;
using CollabVMBot.logs;

using CollabVMSharp;

using Timer = System.Timers.Timer;
using System.Net;
using SixLabors.ImageSharp;

namespace CollabVMBot.VMStuff;

public class VM
{
    public event EventHandler<MessageIncident> MessageIncident;
    public event EventHandler<UsernameIncident> UsernameIncident;

    public CollabVMClient cvm;
    private string username;
    private Database database;
    private TaskCompletionSource<object?> GotAuth = new();

    private int errorlevel = 0;
    private bool wantsconnect = false;

    private Timer retryTimer = new();
    public readonly ConfigVM Config;

    public VM(string username, ConfigVM config, Database db)
    {
        this.Config = config;

        if (this.Config.Password == null && this.Config.Token == null)
        {
            LogManager.Log(LogLevel.FATAL, $"VM {this.Config.Name} has no password or token set.");
            Environment.Exit(1);
        }

        if (this.Config.Password != null && this.Config.Token != null)
        {
            LogManager.Log(LogLevel.FATAL, $"VM {this.Config.Name} has both a password and token set.");
            Environment.Exit(1);
        }

        this.username = username;
        this.database = db;

        retryTimer.Interval = 1000;
        retryTimer.AutoReset = false;
        retryTimer.Elapsed += (_, _) => OpenAsync();

        this.cvm = new CollabVMClient(Config.URL, username, Config.Node);
        this.cvm.Chat += CvmOnChat;
        this.cvm.ConnectedToNode += CvmOnConnectedToNode;
        this.cvm.ConnectionClosed += CvmOnConnectionClosed;
        this.cvm.UserRenamed += (_, e) => CheckUsername(e.User);
        this.cvm.UserJoined += (_, e) => CheckUsername(e);

        // Bot Commands
        this.cvm.RegisterCommand("!quote", QuoteCommand);
        this.cvm.RegisterCommand("!help", HelpCommand);
    }

    private async void QuoteCommand(string username, string[] args)
    {
        if (args.Length != 1)
        {
            await cvm.SendChat($"@{username} Usage: !quote <username>");
            return;
        }

        var chat = await database.GetChatlogsAsync(new ChatlogQuery
        {
            Username = args[0],
            Count = 1,
            Random = true
        });

        if (chat.Length == 0)
        {
            await cvm.SendChat($"@{username} No message found for {args[0]}");
            return;
        }
        await cvm.SendXSSChat($"\"{WebUtility.HtmlEncode(chat[0].Message)}\" - {chat[0].Username}");
    }

    private async void HelpCommand(string username, string msg)
    {
        string helpEncode = WebUtility.HtmlEncode("<h4>Midnight Help Menu:</h4>");
        string helpDecode = WebUtility.HtmlDecode(helpEncode);

        await cvm.SendXSSChat($"\"{WebUtility.HtmlDecode(helpDecode)}");
        Console.WriteLine($"Printed html code: {helpDecode}");
    }

    private void CvmOnConnectionClosed(object? sender, EventArgs e)
    {
        if (!this.wantsconnect) return;
        this.GotAuth = new();

        errorlevel++;

        LogManager.Log(LogLevel.INFO, $"Disconnected from {Config.Name}");
        LogManager.Log(LogLevel.INFO, $"Retrying in {errorlevel} seconds...");

        retryTimer.Interval = errorlevel * 1000;
        retryTimer.Start();
    }

    private void CvmOnConnectedToNode(object? sender, EventArgs e)
    {
        LogManager.Log(LogLevel.INFO, $"Connected to {Config.Name}");
        errorlevel = 0;
        retryTimer.Stop();
    }

    public async Task OpenAsync()
    {
        this.wantsconnect = true;
        if (cvm.Connected) return;

        try
        {
            await cvm.Connect();
        }
        catch (Exception e)
        {
            LogManager.Log(LogLevel.ERROR, $"Failed to connect to {Config.Name}: {e.Message}");
            
            if (errorlevel < Midnight.Config.ConnectRetryMaxSeconds) errorlevel++;
            
            LogManager.Log(LogLevel.INFO, $"Retrying in {errorlevel} seconds...");

            retryTimer.Interval = errorlevel * 1000;
            retryTimer.Start();
            return;
        }

        if (this.Config.Password != null)
            await cvm.Login(Config.Password);
        else
            await cvm.LoginAccount(Config.Token!);
        GotAuth.TrySetResult(null);
    }

    public async Task CloseAsync()
    {
        this.wantsconnect = false;
        if (!cvm.Connected) return;
        await cvm.Disconnect();
    }

    public async Task<byte[]> GetScreenshotPngAsync()
    {
        using MemoryStream ms = new();

        Image img = cvm.GetFramebuffer();
        await img.SaveAsPngAsync(ms);

        return ms.ToArray();
    }

    private async void CvmOnChat(object? sender, ChatMessage e)
    {
        if (string.IsNullOrEmpty(e.Username)) return;
        database.LogChatMessageAsync(Config.Name, e.Username, e.Message);

        if (cvm.Users.All(u => u.Username != e.Username)) return;
        if (cvm.Users.First(u => u.Username == e.Username).Rank != Rank.Unregistered) return;

        foreach (var filter in Midnight.Filters.Where(f => f.CheckMessage))
        {
            if (filter.Check(e.Message))
            {
                switch (filter.Punishment)
                {
                    case Punishment.TempMute:
                        await cvm.MuteUser(e.Username, false);
                        break;
                    case Punishment.PermMute:
                        await cvm.MuteUser(e.Username, true);
                        break;
                    case Punishment.Kick:
                        await cvm.Kick(e.Username);
                        break;
                    case Punishment.Ban:
                        await cvm.Ban(e.Username);
                        break;
                }
                string ip;

                try
                {
                    ip = await cvm.GetIP(e.Username);
                }
                catch (TimeoutException ex)
                {
                    ip = "unknown";
                }
                MessageIncident.Invoke(this, new MessageIncident
                {
                    FilterDescription = filter.Description,
                    IP = ip,
                    Message = e.Message,
                    Punishment = filter.Punishment,
                    Username = e.Username,
                    VM = Config.Name
                });
            }
        }
    }

    private async Task CheckUsername(User user)
    {
        string ip;

        try
        {
            ip = await cvm.GetIP(user.Username);
            await database.LogIPAsync(Config.Name, user.Username, IPAddress.Parse(ip));
        }
        catch (TimeoutException ex)
        {
            ip = "unknown";
        }

        if (user.Rank != Rank.Unregistered) return;
        foreach (var filter in Midnight.Filters.Where(f => f.CheckUsername))
        {
            if (filter.Check(user.Username))
            {
                await cvm.RenameUser(user.Username, "Naught" + new Random().Next(1, 100000));
                UsernameIncident.Invoke(this, new UsernameIncident
                {
                    FilterDescription = filter.Description,
                    IP = ip,
                    Username = user.Username,
                    VM = Config.Name
                });
            }
        }
    }
}