using CollabVMSharp;

namespace CollabVMBot.VMStuff;

public class VMInfo
{
    public VMInfo(CollabVMClient cvm)
    {
        this.ID = cvm.Node;
        this.Users = UserInfo.UserArrToInfo(cvm.Users);

        if (cvm.CurrentTurn.Queue.Length > 0)
            this.TurnQueue = UserInfo.UserArrToInfo(cvm.CurrentTurn.Queue);

        if (cvm.CurrentVote.Status != VoteStatus.None)
            this.VoteInfo = new VoteInfo
            {
                Yes = cvm.CurrentVote.Yes,
                No = cvm.CurrentVote.No,
                Time = cvm.CurrentVote.TimeToVoteEnd,
            };
    }
    public string ID { get; set; }
    public UserInfo[] Users { get; set; }
    public UserInfo[] TurnQueue { get; set; }
    public VoteInfo? VoteInfo { get; set; }
}

public class TurnInfo
{
    public User[] Queue { get; set; }
}

public class VoteInfo
{
    public int Yes { get; set; }
    public int No { get; set; }
    public int Time { get; set; }
}

public class UserInfo
{
    public UserInfo(User user)
    {
        this.Username = user.Username;
        this.Rank = (int)user.Rank;
    }

    public static UserInfo[] UserArrToInfo(IEnumerable<User> users)
    {
        List<UserInfo> list = new();

        foreach (var user in users)
        {
            list.Add(new UserInfo(user));
        }
        return list.ToArray();
    }
    public string Username { get; set; }
    public int Rank { get; set; }
}