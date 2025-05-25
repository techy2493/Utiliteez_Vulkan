namespace Utiliteez.Tools.Logging;

public interface ILogger
{
    public void Debug(string message);
    public void Debug(Exception ex, string message);
    public void Info(string message);
    public void Warning(string message);
    public void Error(string message);
    public void Error(Exception exception);
    public void Error(Exception exception, string message);
    public void Fatal(Exception exception);
    public void Fatal(Exception exception, string message);
}