This document describes practices for gathering performance traces for scenarios not covered by the user-friendly Report a Problem tool.

:bulb: This page is for advanced usage scenarios. For almost all users, the best way to report performance problems is following the instructions on [Reporting Visual Studio crashes and performance issues](https://github.com/dotnet/roslyn/blob/main/docs/wiki/Reporting-Visual-Studio-crashes-and-performance-issues.md).

# Audience

In some scenarios, users may wish to collect performance traces using the PerfView command line tool rather than using the Report a Problem tool. The most common reason for this is it increases the amount of data collected by a single performance trace, which in turn supports measurements for a larger period of time. It also uses larger buffers, reducing the chances of lost data during tracing. Other users may require these steps as a result of company policies preventing users from accessing the Report a Problem tool.

# Common Scenarios

Instructions for manually recording with PerfView:

1. Download [PerfView](https://github.com/Microsoft/perfview/blob/main/documentation/Downloading.md) and save it to a temporary directory
1. If using one of the predefined command lines below, review the section to identify the best time to start and stop measuring for the problem at hand and type of recording
1. Create the directory **C:\temp** if it doesn't exist (or change the following instructions to use a different directory)
1. From an administrative command prompt, start a PerfView collection using one of the command line sequences from defined below
1. At the desired time, press **Stop Collection** in the PerfView UI to stop the recording at that time. It will take a while to process the trace, but at the end there should be a zipped file at **C:\temp\ReproTrace.etl.zip**.

## General Purpose

This performance trace performs well for gathering general information during an interval of 100 seconds or more, depending on resource usage during the interval.

    perfview.exe collect C:\temp\ReproTrace.etl -CircularMB:4096 -BufferSizeMB:256 -Merge:true -Zip:true -Providers:641d7f6c-481c-42e8-ab7e-d18dc5e5cb9e,*Microsoft-VisualStudio-Common,*RoslynEventSource,*StreamJsonRpc -ThreadTime -NoV2Rundown -NoNGenRundown

## CPU Only

This performance trace gathers CPU usage information only. It supports longer trace durations than the general purpose trace, but is only useful in resolving a subset of performance problems users encounter. Information about delays caused by non-CPU situations (e.g. network operations, disk operations, various waits, etc.) are not captured in the trace.

⚠️ This performance trace is unable to gather information about hangs or UI delays. It should only be used in cases where the General Purpose command failed to produce the desired result.

    perfview.exe collect C:\temp\ReproTrace.etl -CircularMB:4096 -BufferSizeMB:256 -Merge:true -Zip:true -Providers:641d7f6c-481c-42e8-ab7e-d18dc5e5cb9e,*Microsoft-VisualStudio-Common,*RoslynEventSource,*StreamJsonRpc -NoV2Rundown -NoNGenRundown

## GC Only

This performance trace gathers information related to garbage collection (GC) operations. It can be used to gather information over long periods of time as an aid to reducing allocation rates in applications.

⚠️ This performance trace is unable to gather information about CPU usage, hangs, or UI delays. It should only be used in cases where the General Purpose command indicated a GC-related problem is occurring.

    perfview.exe collect C:\temp\ReproTrace.etl -CircularMB:8192 -BufferSizeMB:256 -Merge:true -Zip:true -Providers:641d7f6c-481c-42e8-ab7e-d18dc5e5cb9e,*Microsoft-VisualStudio-Common,*RoslynEventSource,*StreamJsonRpc -GCOnly -NoV2Rundown -NoNGenRundown
