using System.Diagnostics;

namespace Utiliteez.RenderEngine;

public class TimingManager : ITimingManager
{
    Stopwatch _stopwatch = Stopwatch.StartNew();
    public long Now => GetNow();

    private long GetNow()
    {
        return _stopwatch.ElapsedTicks;
    }
}