using CollabVMBot.utils;

namespace CollabVMBot;

public class MessageIncident
{
    public string VM { get; set; }
    public string Username { get; set; }
    public string Message { get; set; }
    public string FilterDescription { get; set; }
    public Punishment Punishment { get; set; }
    public string IP { get; set; }
}

public class UsernameIncident
{
    public string VM { get; set; }
    public string Username { get; set; }
    public string FilterDescription { get; set; }
    public string IP { get; set; }
}