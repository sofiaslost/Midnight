using System.Text.RegularExpressions;
using CollabVMBot.settings;

namespace CollabVMBot.utils;

public class Filter
{
    public string Description { get; set; }
    public bool CheckUsername { get; set; }
    public bool CheckMessage { get; set; }
    public Regex Regex { get; set; }
    public Punishment Punishment { get; set; }

    public Filter(ConfigFilter config)
    {
        Description = config.Description;
        CheckMessage = config.CheckMessage;
        CheckUsername = config.CheckUsername;
        Regex = new Regex(config.Regex, RegexOptions.IgnoreCase);
        Punishment = config.Punishment switch
        {
            "None" => Punishment.None,
            "TempMute" => Punishment.TempMute,
            "PermMute" => Punishment.PermMute,
            "Kick" => Punishment.Kick,
            "Ban" => Punishment.Ban,
            _ => throw new ArgumentException("Invalid punishment type")
        };
    }

    public bool Check(string str)
    {
        return Regex.IsMatch(str);
    }
}

public enum Punishment
{
    None, TempMute, PermMute, Kick, Ban
}