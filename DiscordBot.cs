using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;

using CollabVMBot.utils;

namespace CollabVMBot;

public class DiscordBot
{
    private DiscordClient discord;
    private ulong reportChannelId;
    private DiscordChannel reportChannel;

    public DiscordBot(string token, ulong reportChannelId)
    {
        this.reportChannelId = reportChannelId;
        this.discord = new DiscordClient(new DiscordConfiguration
        {
            Token = token,
            TokenType = TokenType.Bot,
            Intents = DiscordIntents.AllUnprivileged,
        });
        var slash = discord.UseSlashCommands();
        slash.RegisterCommands<DiscordCommands>();
    }

    public async Task FlagChatMessageAsync(MessageIncident i)
    {
        string punishment = i.Punishment switch
        {
            Punishment.None => "None",
            Punishment.TempMute => "Temporary Mute",
            Punishment.PermMute => "Permanent Mute",
            Punishment.Kick => "Kick",
            Punishment.Ban => "Ban",
            _ => "None"
        };

        var embed = new DiscordEmbedBuilder()
            .WithTitle("Message Flagged")
            .WithAuthor(i.VM)
            .AddField("Username", i.Username)
            .AddField("IP", i.IP, true)
            .AddField("Message", i.Message)
            .AddField("Filter", i.FilterDescription)
            .AddField("Punishment", punishment)
            .Build();
        await reportChannel.SendMessageAsync($"<@&{Midnight.Config.Discord.ReportPingRole}>", embed: embed);
    }

        public async Task FlagUsernameAsync(UsernameIncident i)
    {
        var embed = new DiscordEmbedBuilder()
            .WithTitle("Username Flagged")
            .WithAuthor(i.VM)
            .AddField("Username", i.Username)
            .AddField("Filter", i.FilterDescription)
            .AddField("IP", i.IP, true)
            .AddField("Punishment", "Rename")
            .Build();
        await reportChannel.SendMessageAsync($"<@&{Midnight.Config.Discord.ReportPingRole}>", embed: embed);
    }

    public async Task Connect()
    {
        await discord.ConnectAsync();
        LogManager.Log(LogLevel.INFO, "Connected to Discord");
        reportChannel = await discord.GetChannelAsync(reportChannelId);
    }
    
    public async Task Disconnect()
    {
        await discord.DisconnectAsync();
        LogManager.Log(LogLevel.INFO, "Disconnected from Discord");
    }
}