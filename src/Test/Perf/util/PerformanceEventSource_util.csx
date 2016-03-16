using System.Diagnostics.Tracing;

[EventSource(Name = "PerformanceEventSource")]
public class PerformanceEventSource : EventSource
{

    public static readonly PerformanceEventSource Log = new PerformanceEventSource();

    private PerformanceEventSource()
    {
    }

    [Event(1, Level = EventLevel.Verbose)]
    public void EventStart()
    {
        WriteEvent(1, nameof(EventStart));
    }

    [Event(2, Level = EventLevel.Verbose)]
    public void EventEnd()
    {
        WriteEvent(2, nameof(EventEnd));
    }
}