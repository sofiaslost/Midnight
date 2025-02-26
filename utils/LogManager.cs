using System.Text;

namespace CollabVMBot.utils;

public enum LogLevel
{
    DEBUG,
    INFO,
    WARN,
    ERROR,
    FATAL
}

public static class LogManager
{
    public static void Log(LogLevel level, string msg)
    {
#if !DEBUG
        if (level == LogLevel.DEBUG)
            return;
#endif
        StringBuilder logstr = new StringBuilder();
        logstr.Append("[");
        logstr.Append(DateTime.Now.ToString("G"));
        logstr.Append("] [");
        switch (level)
        {
            case LogLevel.DEBUG:
                logstr.Append("DEBUG");
                break;
            case LogLevel.INFO:
                logstr.Append("INFO");
                break;
            case LogLevel.WARN:
                logstr.Append("WARN");
                break;
            case LogLevel.ERROR:
                logstr.Append("ERROR");
                break;
            case LogLevel.FATAL:
                logstr.Append("FATAL");
                break;
            default:
                throw new ArgumentException("Invalid log level, Dumbo");
        }
        logstr.Append("] ");
        logstr.Append(msg);
        switch (level)
        {
            case LogLevel.DEBUG:
            case LogLevel.INFO:
                Console.WriteLine(logstr.ToString());
                break;
            case LogLevel.WARN:
            case LogLevel.ERROR:
            case LogLevel.FATAL:
                Console.Error.Write(logstr.ToString());
                break;
        }
    }
}