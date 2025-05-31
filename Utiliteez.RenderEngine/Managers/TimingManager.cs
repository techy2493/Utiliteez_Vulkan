using System.Diagnostics;

namespace Utiliteez.RenderEngine;

public class TimingManager : ITimingManager
{
    Stopwatch _stopwatch = Stopwatch.StartNew();
    public double Now => GetNow();

    private double GetNow()
    {
        return _stopwatch.ElapsedMilliseconds / 1000d; // Convert milliseconds to microseconds
    }
}