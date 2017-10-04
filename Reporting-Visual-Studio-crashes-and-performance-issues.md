// This document is a work-in-progress. Do not use.

# Crashes

Log crash dumps and attach them to your feedback issue

```
[HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\Windows Error Reporting\LocalDumps\devenv.exe]
"DumpFolder"="C:\\Crashdumps"
"DumpCount"=dword:00000010
"DumpType"=dword:00000002
```

# Performance Issues

- If necessary, start a second Visual Studio instance (with no solution open) just to capture the trace. The trace will include information for the other Visual Studio session as well.
- Aim for 30 seconds to 2 minutes.
- Perform the action that is causing performance issues while the trace is recording. If typing is delayed, type during the trace. If opening a file is slow, open a bunch of files. Etc.
- File new feedback issues instead of commenting on existing issues.