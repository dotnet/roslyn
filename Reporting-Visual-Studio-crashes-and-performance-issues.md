This document outlines the best practices for reporting Visual Studio crashes and performance issues (poor responsiveness, hangs, etc.).

# Crashes

If you have a set of steps that reproduces the crash reliably, then first check to see if someone else has filed an issue for that scenario. You can search for existing issues on the [Developer Community site](https://developercommunity.visualstudio.com/).

If you can't find an existing issue but have reliable repro steps, then you can file a new issue describing those repro steps. Be sure to include:
- The language of the open projects (C#, C++, etc.)
- The kind of project (Console Application, ASP.NET, etc.)

If you're not sure what's causing your crashes or they seem random, then you can capture dumps locally each time Visual Studio crashes and attach those to feedback items. To save dumps locally when Visual Studio crashes, set the following registry entries:

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