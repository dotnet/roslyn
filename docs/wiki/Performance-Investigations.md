⚠️ This is an early draft / work-in-progress.

:memo: This page is intended to help investigate performance issues submitted through [Developer Community](https://developercommunity.visualstudio.com), where performance data is only accessible to Microsoft employees working on the performance investigations. However, the practices apply to other applications and even to local investigations as long as developers are willing to record their own performance data using PerfView.

# Intended Outcomes

## User Complaint: Lag/Slow while typing or interacting with the UI

Cause: UI thread is not waiting for messages in the main message pump

1. Open the Thread Time view
1. Find the UI thread (`devenv!WinMain`)
1. Right Click &rarr; Include Item on the thread
1. In the By Name view, look at the Exc column to see if the delay is caused by BLOCKED_TIME or CPU_TIME

Intended outcome: Want to attribute the majority ("as much as reasonably possible") of the non-message pump time to an underlying cause, and for cases where "too much time is spent", file bugs to reduce the UI thread workload.

Common causes:

* Inappropriate use of the UI thread for computational work
* Waiting for GC
* Waiting for JIT
* Waiting in JoinableTaskFactory (JTF)
* Running a COM message pump

## User Complaint: Long-running operation takes too long

Long-running operations are typically asynchronous operations which produce a result over time, often providing a cancellable progress report as a natural part of the operation.

*TODO*

## User Complaint: High CPU usage

High CPU usage reports typically fall into one of two categories:

1. The CPU usage is itself a problem, e.g. a laptop fan keeps running, the battery is used too fast, the computer is too warm, or the use of Visual Studio interferes with the performance of unrelated applications running on the same computer.
1. The user is experiencing another problem, e.g. poor typing performance, and attributed the problem to CPU usage for the bug report.

Sometimes a user feedback report contains enough detail to classify the report into one of these categories, but most of the time a guess must be made. The intended outcome depends on what the user meant by the report.

Intended outcome:

* Clear indication that CPU usage is the problem: Investigate CPU usage across all processes running on the system, and attempt to account for "a reasonable majority" with bugs (Microsoft-cause) or feedback (non-Microsoft cause) sufficient to explain the CPU usage and lead to a resolution.
* Clear indication that CPU usage is *not* the true problem: Primarily investigate by following the steps for the true complaint type instead of investigating as a CPU usage complaint. However, as an added bonus, briefly look at the CPU usage information in the trace to see if majority offenders can be identified and possibly point to a low-hanging fruit bug.
* Unclear true problem: Investigate as a CPU usage report (assume the user meant what they said), but try to avoid spending too much time in an intense deep-dive to reduce CPU usage. Identify the largest single offender(s) in the trace, if they exist, and follow up with a description of the findings and a suggestion that the user take a look at the Performance Issues section of the "reporting a problem" page in case they want to provide more specific feedback for further investigation.

# Pitfalls

When investigating a performance scenario, be aware that many pitfalls exist. This section lists a few that are known and relatively frequently encountered.

## Measurement window accuracy

Sometimes a performance trace is taken during a period of time in which the stated complaint is not actively a problem. For example, in extreme instances users have submitted performance traces of a completely idle Visual Studio instance that did not have any solution open, while reporting a problem related to typing performance with a large solution. While this case was easy to identify (Visual Studio was completely inactive and not hung), other cases can be quite difficult.

:bulb: If the conclusions drawn from the performance trace do not seem obviously related to the stated complaint, it may indicate a measurement window mismatch.

## Performance trace size limitations

The performance measurement infrastructure (PerfView) uses a circular buffer which only keeps the most recent data up to some fixed size. Depending on the type and amount of activity on the system during measurement, it is possible for a performance trace to exceed the size limit, and only retain "more recent" information in the trace. The time supported for a trace *widely* varies by measurement conditions, so only loose guidance is provided in the recommended steps for measuring and reporting performance problems.

💡 If you open a view in PerfView and observe a lack of data for a substantial portion of the beginning of a measurement window (see the **When** column), the measurement may have hit the end of the buffer and discarded the oldest data. Note that the GC data and the Thread Time data are recorded separately, so the buffers will not discard the same duration of data.

# Investigating GC performance

Red flags that GC may be a problem:

* In the **GC Stats** window from PerfView
    1. Large "Total GC Pause"
    1. Large "% Time paused for Garbage Collection"
    1. Large "% CPU Time spent Garbage Collecting"
* In the **Thread Time Stacks** window in PerfView
    1. Large time spent in `clr!??WKS::gc_heap::gc1`
    1. Large time spent in `clr!WKS::GCHeap::WaitUntilGCComplete`
* In the **GC Heap Alloc Ignore Free** window in PerfView
    1. Large "Totals Metric" at the top of the window

GC problems are typically caused by one of the following:

1. **Allocation rate:** Code allocates large numbers of objects, requiring GC to run frequently to clean them up
2. **Memory pressure:** A large working set (which could be managed or unmanaged memory) is held by an application, leaving only a small amount of free memory space for the garbage collector to operate within. Increased memory pressure reduces the allocation rate required to cause observable performance problems.