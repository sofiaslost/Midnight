using System.Text;
using CollabVMSharp;
using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;
using Microsoft.VisualBasic;

using CollabVMBot.logs;
using CollabVMBot.settings;
using CollabVMBot.VMStuff;

namespace CollabVMBot;

public class DiscordCommands : ApplicationCommandModule
{
    [SlashCommand("vm", "Get info from a VM")]
    public async Task VM(InteractionContext ctx,  [Autocomplete(typeof(VMAutocompleteProvider))] [Option("vm", "VM to get info from")] string VM) {
        if (Midnight.VMs.All(v => v.Config.Name != VM)) {
            await ctx.CreateResponseAsync("No VM by that name found.");
            return;
        }
        var vm = Midnight.VMs.First(v => v.Config.Name == VM);
        if (!vm.cvm.ConnectedToVM) {
            await ctx.CreateResponseAsync($"Not currently connected to {vm.Config.Name}");
        }
        await ctx.DeferAsync();
        StringBuilder userlist = new();
        StringBuilder botlist = new();
        foreach (User user in vm.cvm.Users) {
            var b = (Midnight.Config.KnownBots.Contains(user.Username)) ? botlist : userlist;
            switch (user.Rank) {
                case Rank.Admin:
                    b.Append(":red_circle: ");
                    break;
                case Rank.Moderator:
                    b.Append(":green_circle: ");
                    break;
            }
            b.Append(user.Username + "\n");
        }

        if (String.IsNullOrEmpty(userlist.ToString()))
            userlist.Append("(none)");
        if (String.IsNullOrEmpty(botlist.ToString()))
            botlist.Append("(none)");
        var embed = new DiscordEmbedBuilder()
            .WithTitle(VM)
            .WithImageUrl("attachment://vmscreen.png")
            .AddField("Users", userlist.ToString(), true)
            .AddField("Bots", botlist.ToString(), true)
            .Build();
        using var ms = new MemoryStream(await vm.GetScreenshotPngAsync());
        await ctx.EditResponseAsync(new DiscordWebhookBuilder()
            .AddEmbed(embed)
            .AddFile("vmscreen.png", ms));
    }
    
        [SlashCommand("finduser", "Find a user on all VMs")]
    public async Task FindUser(InteractionContext ctx, [Option("Username", "Username to find")] string username) {
        if (Midnight.VMs.All(v => v.cvm.Users.All(u => u.Username != username))) {
            await ctx.CreateResponseAsync($"**{username}** not found on any VM");
            return;
        }
        List<string> foundvms = new();
        foreach (var vm in Midnight.VMs) {
            if (!vm.cvm.ConnectedToVM) continue;
            if (vm.cvm.Users.Any(u => u.Username == username))
                foundvms.Add(vm.Config.Name);
        }
        await ctx.CreateResponseAsync($"User **{username}** found on **{String.Join(", ", foundvms)}**");
    }

    [SlashCommand("ban", "Ban a user")]
    public async Task Ban(InteractionContext ctx, [Option("username", "User to ban")] string username,
        [Option("vm", "VM to ban from")] [Autocomplete(typeof(VMAutocompleteProvider))] string VM) {
        if (Midnight.VMs.All(v => v.Config.Name != VM)) {
            await ctx.CreateResponseAsync("No VM by that name found.");
            return;
        }
        var vm = Midnight.VMs.First(v => v.Config.Name == VM);
        if (!vm.cvm.ConnectedToVM) {
            await ctx.CreateResponseAsync($"Not currently connected to {vm.Config.Name}");
        }
        if (vm.Config.DiscordMods?.Contains(ctx.Member.Id) != true && ctx.Member.Roles.All(r => !Midnight.Config.Discord.ModRoles.Contains(r.Id))) {
            await ctx.CreateResponseAsync("You do not have permission to use that command.");
            return;
        }
        if (vm.cvm.Users.All(u => u.Username != username)) {
            await ctx.CreateResponseAsync($"User **{username}** not found on **{VM}**");
            return;
        }
        await vm.cvm.Ban(username);
        await ctx.CreateResponseAsync($"Successfully banned **{username}** from **{vm.Config.Name}**");
    }
    
    [SlashCommand("kick", "Kick a user")]
    public async Task Kick(InteractionContext ctx, [Option("username", "User to kick")] string username,
        [Option("vm", "VM to kick from"), Autocomplete(typeof(VMAutocompleteProvider))] string VM) {
        if (Midnight.VMs.All(v => v.Config.Name != VM)) {
            await ctx.CreateResponseAsync("No VM by that name found.");
            return;
        }
        var vm = Midnight.VMs.First(v => v.Config.Name == VM);
        if (!vm.cvm.ConnectedToVM) {
            await ctx.CreateResponseAsync($"Not currently connected to {vm.Config.Name}");
        }
        if (vm.Config.DiscordMods?.Contains(ctx.Member.Id) != true && ctx.Member.Roles.All(r => !Midnight.Config.Discord.ModRoles.Contains(r.Id))) {
            await ctx.CreateResponseAsync("You do not have permission to use that command.");
            return;
        }
        if (vm.cvm.Users.All(u => u.Username != username)) {
            await ctx.CreateResponseAsync($"User **{username}** not found on **{VM}**");
            return;
        }
        await vm.cvm.Kick(username);
        await ctx.CreateResponseAsync($"Successfully kicked **{username}** from **{vm.Config.Name}**");
    }

    [SlashCommand("reboot", "Reboot a VM")]
    public async Task Reboot(InteractionContext ctx,
        [Option("vm", "VM to reboot"), Autocomplete(typeof(VMAutocompleteProvider))] string VM)
    {
        if (Midnight.VMs.All(v => v.Config.Name != VM)) {
            await ctx.CreateResponseAsync("No VM by that name found.");
            return;
        }
        var vm = Midnight.VMs.First(v => v.Config.Name == VM);
        if (!vm.cvm.ConnectedToVM) {
            await ctx.CreateResponseAsync($"Not currently connected to {vm.Config.Name}");
        }
        if (vm.Config.DiscordMods?.Contains(ctx.Member.Id) != true && ctx.Member.Roles.All(r => !Midnight.Config.Discord.ModRoles.Contains(r.Id))) {
            await ctx.CreateResponseAsync("You do not have permission to use that command.");
            return;
        }
        await vm.cvm.Reboot();
        await ctx.CreateResponseAsync($"Successfully rebooted **{vm.Config.Name}**");
    }
    
    [SlashCommand("restore", "Restore a VM")]
    public async Task Restore(InteractionContext ctx,
        [Option("vm", "VM to restore"), Autocomplete(typeof(VMAutocompleteProvider))] string VM)
    {
        if (Midnight.VMs.All(v => v.Config.Name != VM)) {
            await ctx.CreateResponseAsync("No VM by that name found.");
            return;
        }
        var vm = Midnight.VMs.First(v => v.Config.Name == VM);
        if (!vm.cvm.ConnectedToVM) {
            await ctx.CreateResponseAsync($"Not currently connected to {vm.Config.Name}");
        }
        if (vm.Config.DiscordMods?.Contains(ctx.Member.Id) != true && ctx.Member.Roles.All(r => !Midnight.Config.Discord.ModRoles.Contains(r.Id))) {
            await ctx.CreateResponseAsync("You do not have permission to use that command.");
            return;
        }
        await vm.cvm.Restore();
        await ctx.CreateResponseAsync($"Successfully restored **{vm.Config.Name}**");
    }

    [SlashCommand("getip", "Get the IP address of a user")]
    public async Task GetIP(InteractionContext ctx, [Option("username", "Username to grab IP from")] string username)
    {
        VM[] vms;
        if (ctx.Member.Roles.Any(r => Midnight.Config.Discord.ModRoles.Contains(r.Id)))
            vms = Midnight.VMs;
        else
            vms = Midnight.VMs.Where(v => v.Config.DiscordMods?.Contains(ctx.Member.Id) == true).ToArray();
        if (vms.Length == 0) {
            await ctx.CreateResponseAsync("You do not have permission to use that command.");
            return;
        }
        if (vms.All(v => v.cvm.Users.All(u => u.Username != username))) {
            await ctx.CreateResponseAsync($"**{username}** not found on any VM");
            return;
        }
        await ctx.DeferAsync();
        List<DiscordEmbed> IPEmbeds = new();
        foreach (var vm in vms)
        {
            if (!vm.cvm.ConnectedToVM || vm.cvm.Users.All(u => u.Username != username)) continue;
            string ip;
            try
            {
                ip = await vm.cvm.GetIP(username);
            }
            catch (TimeoutException ex)
            {
                continue;
            }
            var ipinfo = await Midnight.IPinfo.IPApi.GetDetailsAsync(ip);
            IPEmbeds.Add(new DiscordEmbedBuilder()
                .WithAuthor(username)
                .WithTitle(vm.Config.Name)
                .AddField("IP", ip)
                .AddField("Location", $"{ipinfo.City}, {ipinfo.Region}, {ipinfo.Country} {ipinfo.Postal} ({ipinfo.Loc})")
                .AddField("ASN", ipinfo.Org)
                .Build());
        }
        await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbeds(IPEmbeds));
    }

    [SlashCommand("quote", "Quote a user")]
    public async Task Quote(InteractionContext ctx, [Option("username", "User to quote")] string username = "", [Option("guest", "Should the random quote be from a guest only? Only works if username is not specified")] bool guest = false)
    {
        if (username != "" && guest)
        {
            await ctx.CreateResponseAsync("You cannot specify both a username and guest.");
            return;
        }
        await ctx.DeferAsync();
        var q = new ChatlogQuery
        {
            Count = 1,
            Random = true
        };
        if (username != "")
            q.Username = username;
        if (guest)
            q.CustomWhere = ["username REGEXP '^guest'"];
        var chat = await Midnight.Database.GetChatlogsAsync(q);
        if (chat.Length == 0)
        {
            await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent($"No messages found for {username}"));
            return;
        }
        await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent($"> {chat[0].Message}\n\\- {chat[0].Username}, {chat[0].Timestamp} {chat[0].VM}"));
    }
}

public class VMAutocompleteProvider : IAutocompleteProvider {

    public Task<IEnumerable<DiscordAutoCompleteChoice>> Provider(AutocompleteContext ctx)
    {
        var list = new List<DiscordAutoCompleteChoice>();
        foreach (ConfigVM vm in Midnight.Config.VMs) {
            list.Add(new DiscordAutoCompleteChoice(vm.Name, vm.Name));
        }
        if (list.Count > 25) list.RemoveRange(24, list.Count - 25);
        return Task.FromResult(list.AsEnumerable());
    }
}