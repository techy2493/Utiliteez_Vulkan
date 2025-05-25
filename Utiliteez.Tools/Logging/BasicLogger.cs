namespace Utiliteez.Tools.Logging;

public class BasicLogger: ILogger
{
    public void Log(string message, string level, ConsoleColor color)
    {
        var timeStamp = DateTime.UtcNow;
        Console.ForegroundColor = color;
        Console.WriteLine($"[{timeStamp}] {level} | {message}");
    }

    public void Debug(string message)
    {
        Log(message, "DEBUG", ConsoleColor.DarkGray);
    }

    public void Debug(Exception ex, string message)
    {
        Log(message, "DEBUG", ConsoleColor.DarkGray);
        Log(ex.Message, "DEBUG", ConsoleColor.DarkGray);
    }

    public void Info(string message)
    {
        Log(message, "INFO", ConsoleColor.Gray);
    }

    public void Warning(string message)
    {
        Log(message, "WARN", ConsoleColor.White);
    }

    public void Error(string message)
    {
        Log(message, "WARN", ConsoleColor.Yellow);
    }

    public void Error(Exception exception)
    {
        Log(exception.Message, "WARN", ConsoleColor.Yellow);
    }

    public void Error(Exception exception, string message)
    {
        Log(message, "WARN", ConsoleColor.Yellow);
        Log(exception.Message, "WARN", ConsoleColor.Yellow);
    }

    public void Fatal(Exception exception)
    {
        Log(exception.Message, "WARN", ConsoleColor.Red);
    }

    public void Fatal(Exception exception, string message)
    {
        Log(message, "WARN", ConsoleColor.Red);
        Log(exception.Message, "WARN", ConsoleColor.Red);
    }
}