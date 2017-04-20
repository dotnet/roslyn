using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;

public class WaitForIdleCPUAction
{
    private const int LogEntries = 10;
    private readonly List<string> loads = new List<string>(LogEntries);

    /// <summary>
    /// This command waits until total CPU load has been below Threshold (default is 15%) for a number of seconds.
    /// Use MaxWaitMinutes to set a timeout limit for waiting. Default is 10 minutes. 
    /// </summary>
    public static void Execute(int threshold = 15, int maxWaitMinutes = 60)
    {
        var startTime = DateTime.UtcNow;
        var waiter = new WaitForIdleCPUAction();
        waiter.Do(startTime, threshold, maxWaitMinutes);
    }

    public void Do(DateTime startTime, int threshold, int maxwaitminutes)
    {
        if (!TryWaitForAllCoresIdle(startTime, "CPU", 10, threshold, maxwaitminutes) || !TryWaitForIdle(startTime, "PhysicalDisk", "% Disk Time", "_Total", "Disk", 5, maxwaitminutes, threshold))
        {
            return;
        }
    }

    private bool TryWaitForAllCoresIdle(DateTime startTime, string counterName, int tryCount, int threshold, int maxwaitminutes)
    {
        var counters = new List<PerformanceCounter>();

        // initialize all counters
        for (var i = 0; i < Environment.ProcessorCount; i++)
        {
            var counter = new PerformanceCounter("Processor", "% Processor Time", i.ToString());
            InitializeCounter(counter);

            counters.Add(counter);
        }

        if (!TryWaitForIdle(startTime, counters, counterName, tryCount, threshold, maxwaitminutes))
        {
            return false;
        }

        // uninitialize all counters
        for (var i = 0; i < counters.Count; i++)
        {
            counters[i].Dispose();
            counters[i] = null;
        }

        return true;
    }

    private  bool TryWaitForIdle(
        DateTime startTime, string categoryName, string counterName, string instanceName, string reportingCounterName, int tryCount,
        int maxWaitMinutes, int threshold)
    {
        using (var counter = new PerformanceCounter(categoryName, counterName, instanceName))
        {
            InitializeCounter(counter);

            var idleCount = 0;

            while (idleCount < tryCount)
            {
                Thread.Sleep(1000);

                var load = counter.NextValue();
                ReportLoad(counterName, load);
                idleCount = load > threshold ? 0 : idleCount + 1;

                if (DateTime.UtcNow.Subtract(startTime).TotalMinutes >= maxWaitMinutes)
                {
                    throw new InvalidOperationException(string.Format("\t  Failed to reach {0} idle state within {1} minutes!", counterName, maxWaitMinutes));
                }
            }

            Debug.WriteLine(string.Format("\t  Waited {0:F1} seconds to reach {1} idle state", DateTime.UtcNow.Subtract(startTime).TotalSeconds, counterName));
            return true;
        }
    }

    private bool TryWaitForIdle(DateTime startTime, List<PerformanceCounter> counters, string counterName, int tryCount, int threshold, int maxWaitMinutes)
    {
        var idleCount = 0;

        while (idleCount < tryCount)
        {
            Thread.Sleep(1000);

            for (var i = 0; i < counters.Count; i++)
            {
                var counter = counters[i];

                var load = counter.NextValue();
                ReportLoad(counterName, load);

                if (load > threshold)
                {
                    idleCount = 0;
                    break;
                }

                idleCount++;
            }

            if (DateTime.UtcNow.Subtract(startTime).TotalMinutes >= maxWaitMinutes)
            {
                throw new InvalidOperationException(string.Format("\t  Failed to reach {0} idle state within {1} minutes!", counterName, maxWaitMinutes));
            }
        }

        Debug.WriteLine(string.Format("\t  Waited {0:F1} seconds to reach {1} idle state", DateTime.UtcNow.Subtract(startTime).TotalSeconds, counterName));
        return true;
    }

    private void ReportLoad(string counterName, float load)
    {
        if (loads.Count >= LogEntries)
        {
            Debug.WriteLine("\t  {0} load %: {1}", counterName, string.Join(", ", loads));
            loads.Clear();
        }
        else
        {
            loads.Add(load.ToString("F1"));
        }
    }

    private static void InitializeCounter(PerformanceCounter counter)
    {
        counter.NextValue();
        Thread.Sleep(1000);
        counter.NextValue();
        Thread.Sleep(1000);
    }
}
