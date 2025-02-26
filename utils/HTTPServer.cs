using System.Net;
using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;

using CollabVMBot.VMStuff;
using CollabVMBot.logs;

namespace CollabVMBot.utils;

public class HTTPServer
{
    private WebApplication app;

    public HTTPServer()
    {
        var builder = WebApplication.CreateBuilder();

        builder.WebHost.UseKestrel(k => k.ListenLocalhost(Midnight.Config.API.HttpPort));
        this.app = builder.Build();
        this.app.MapGet("/api/v1/list", (Delegate) VMListHandler);
        this.app.MapGet("/api/v1/vminfo/{vm:required}", VmInfoHandler);
        this.app.MapGet("/api/v1/screenshot/{vm:required}", VMScreenshotHandler);
        this.app.MapGet("/api/v1/chatlogs", (Delegate)VMChatlogHandler).WithRequestTimeout(TimeSpan.FromMinutes(10));
        this.app.MapGet("/api/v1/mod/iptousername/{ip:required}", IPToUsernameHandler);
        this.app.MapGet("/api/v1/mod/usernametoip/{username:required}", UsernameToIPHandler);
        this.app.Lifetime.ApplicationStarted.Register(this.onServerStarted);
        this.app.Lifetime.ApplicationStopping.Register(onServerStopping);
    }

    private void onServerStopping()
    {
        LogManager.Log(LogLevel.INFO, "HTTP server is shutting down...");
    }

    private async Task<IResult> VMListHandler(HttpContext context)
    {
        var j = new JsonObject();
        foreach (var v in Midnight.VMs.Where(v => v.cvm.ConnectedToVM))
        {
            j[v.Config.Name] = v.Config.Node;
        }
        return Results.Json(j);
    }

    private async Task<IResult> UsernameToIPHandler(HttpContext context, string username)
    {
        if (!context.Request.Query.ContainsKey("token") ||
            context.Request.Query["token"] != Midnight.Config.API.ModAPIPassword)
        {
            context.Response.StatusCode = 401;
            return Results.Text("401: Unauthorized.");
        }
        var ips = await Midnight.Database.GetIPFromUsernameAsync(username);
        return Results.Json(ips);
    }

    private async Task<IResult> IPToUsernameHandler(HttpContext context, string ip)
    {
        if (!context.Request.Query.ContainsKey("token") ||
            context.Request.Query["token"] != Midnight.Config.API.ModAPIPassword)
        {
            context.Response.StatusCode = 401;
            return Results.Text("401: Unauthorized.");
        }
        if (!IPAddress.TryParse(ip, out var ipaddr))
        {
            context.Response.StatusCode = 400;
            return Results.Text("400: Invalid IP address.");
        }
        var usernames = await Midnight.Database.GetUsernameFromIPAsync(ipaddr);
        return Results.Json(usernames);
    }

    private async Task<IResult> VMChatlogHandler(HttpContext context)
    {
        var q = new ChatlogQuery();
        if (context.Request.Query.ContainsKey("vm"))
        {
            string vm = context.Request.Query["vm"];
            q.VM = vm;
        }
        if (context.Request.Query.ContainsKey("username"))
            q.Username = context.Request.Query["username"];
        if (context.Request.Query.ContainsKey("from"))
        {
            if (!DateTime.TryParse(context.Request.Query["from"].ToString(), out var from))
            {
                context.Response.StatusCode = 400;
                return Results.Text("400: Invalid from timestamp.");
            }
            q.FromTimestamp = from;
        }
        if (context.Request.Query.ContainsKey("to"))
        {
            if (!DateTime.TryParse(context.Request.Query["to"].ToString(), out var to))
            {
                context.Response.StatusCode = 400;
                return Results.Text("400: Invalid to timestamp.");
            }
            q.ToTimeStamp = to;
        }
        if (context.Request.Query.ContainsKey("count"))
        {
            if (!int.TryParse(context.Request.Query["count"], out var count) || count < 1)
            {
                context.Response.StatusCode = 400;
                return Results.Text("400: Count must be a valid integer higher than 0.");
            }
            q.Count = count;
        }
        if (context.Request.Query.ContainsKey("regex"))
            q.Regex = context.Request.Query["regex"];
        if (context.Request.Query.ContainsKey("random") && context.Request.Query["random"] == "1")
            q.Random = true;
        if (q.Username == null && q.FromTimestamp == null && q.ToTimeStamp == null && q.Count == null &&
            q.Regex == null)
            q.Count = 10;
        var logs = await Midnight.Database.GetChatlogsAsync(q);
        return Results.Json(logs);
    }

    private void onServerStarted() {
        LogManager.Log(LogLevel.INFO, $"HTTP server listing on port {Midnight.Config.API.HttpPort}");
    }

    private async Task<IResult> VMScreenshotHandler(HttpContext context, string vm) {
        if (vm.Length > 4 && vm[^4..] == ".png")
            vm = vm[..^4];
        if (Midnight.VMs.All(v => v.cvm.Node != vm)) {
            context.Response.StatusCode = 404;
            return Results.Text($"404: {vm} not found.", "text/plain");
        }
        var VM = Midnight.VMs.First(v => v.cvm.Node == vm);
        return Results.Bytes(await VM.GetScreenshotPngAsync(), "image/png");
    }

    private async Task<IResult> VmInfoHandler(HttpContext context, string vm) {
        if (Midnight.VMs.All(v => v.cvm.Node != vm)) {
            context.Response.StatusCode = 404;
            return Results.Text($"404: {vm} not found.");
        }
        var VM = Midnight.VMs.First(v => v.cvm.Node == vm);
        return Results.Json(new VMInfo(VM.cvm));
    }

    public Task RunAsync()
    {
        return this.app.RunAsync();
    }
    
    public Task StopAsync()
    {
        return this.app.StopAsync();
    }
}