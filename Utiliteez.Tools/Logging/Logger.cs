namespace Utiliteez.Tools.Logging;

public static class Logger
{
    private static ILogger? _log;

    public static ILogger Log
    {
        get => _log ??= new BasicLogger();
        set => _log = value;
    }
}