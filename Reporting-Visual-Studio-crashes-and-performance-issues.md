This document outlines the best practices for reporting Visual Studio crashes and performance issues (poor responsiveness, hangs, etc.). For a general overview of how to report problems in Visual Studio, see "[How to Report a Problem with Visual Studio 2017](https://docs.microsoft.com/en-us/visualstudio/ide/how-to-report-a-problem-with-visual-studio-2017)". This document adds additional guidance for gathering heap dumps / traces.

# Crashes

If you have a set of steps that reproduces the crash reliably, follow the steps in "[How to Report a Problem with Visual Studio 2017](https://docs.microsoft.com/en-us/visualstudio/ide/how-to-report-a-problem-with-visual-studio-2017)" and be sure to include:
- The language of the open projects (C#, C++, etc.)
- The kind of project (Console Application, ASP.NET, etc.)

If you're not sure what's causing your crashes or they seem random, then you can capture dumps locally each time Visual Studio crashes and attach those to separate feedback items. To save dumps locally when Visual Studio crashes, set the following registry entries:

```
[HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\Windows Error Reporting\LocalDumps\devenv.exe]
"DumpFolder"="C:\\Crashdumps"
"DumpCount"=dword:00000010
"DumpType"=dword:00000002
```

Then, use Visual Studio's "Report a Problem..." feature. It will allow you to attach the appropriate dump.

# Performance Issues

When diagnosing performance issues, the goal is to capture a performance trace while performing the slow/hanging action.

- If necessary, start a second Visual Studio instance (with no solution open) to capture the trace. You can target the data collection from the Visual Studio encountering the problem.
- Perform the action that is causing performance issues while the trace is recording. If typing is delayed, type during the trace. If opening a file is slow, open a bunch of files.
- Try to repeat the action and capture a trace for 30 seconds to 2 minutes.
- *File new feedback issues* instead of commenting on existing issues. This allows us to correctly deduplicate issues.