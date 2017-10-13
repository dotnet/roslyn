For a general overview of how to report problems in Visual Studio, see "[How to Report a Problem with Visual Studio 2017](https://docs.microsoft.com/en-us/visualstudio/ide/how-to-report-a-problem-with-visual-studio-2017)". This document adds additional guidance and best practices for reporting crashes and performance issues by gathering heap dumps / performance traces.

:arrow_right: **The product teams review all feedback submitted through the Report a Problem tool.** Users are not required to follow the steps in this document to provide feedback.

# Audience

This document describes the best known ways to report actionable product feedback, i.e. feedback for which the product team is most likely to resolve quickly. It focused on two categories of severe problems which are historically challenging to resolve.

:bulb: After identifying the case which best describes your issue, follow the feedback steps specific to that case.

* [**Crashes:**](#crashes) A crash occurs when when the process (Visual Studio) terminates unexpectedly.
* [**Performance issues:**](#performance-issues) Performance problems fall into several categories, but are typically handled the same way.
    * Solution load
    * Typing
    * Open/closing documents
    * Searching
    * Analyzers and code fixes
    * Any other specific action which is slower than desired

# Crashes

## Directly reproducible crashes

Directly reproducible crashes are cases which have all of the following characteristics:

1. Can be observed by following a known set of steps
2. Can be observed on multiple computers (if available)
3. If the steps involve opening a project or document, can be reproduced in sample code or a project which can be linked to or provided as part of the feedback

For these issues, follow the steps in "[How to Report a Problem with Visual Studio 2017](https://docs.microsoft.com/en-us/visualstudio/ide/how-to-report-a-problem-with-visual-studio-2017)" and be sure to include:
- The steps to reproduce the problem
- The language of the open projects (C#, C++, etc.)
- The kind of project (Console Application, ASP.NET, etc.)

:bulb: **Most valuable feedback:** For this case, the most valuable feedback is the set of steps to reproduce the issue along with sample source code.

## Unknown crashes

If you're not sure what's causing your crashes or they seem random, then you can capture dumps locally each time Visual Studio crashes and attach those to separate feedback items. To save dumps locally when Visual Studio crashes, set the following registry entries:

```
[HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\Windows Error Reporting\LocalDumps\devenv.exe]
"DumpFolder"="C:\\Crashdumps"
"DumpCount"=dword:00000010
"DumpType"=dword:00000002
```

⚠️ Each dump file produced by this method will be up to 4GiB in size. Make sure to set `DumpFolder` to a location with adequate drive space.

Each time Visual Studio crashes, it will create a dump file **devenv.exe.[number].dmp** file in the configured location.

Then, use Visual Studio's "Report a Problem..." feature. It will allow you to attach the appropriate dump.

1. Locate the dump file for the crash you are reporting (look for a file with the correct Creation time
2. If possible, zip the file (*.zip) to reduce its size before submitting feedback
3. Follow the steps in "[How to Report a Problem with Visual Studio 2017](https://docs.microsoft.com/en-us/visualstudio/ide/how-to-report-a-problem-with-visual-studio-2017)", and attach the heap dump to a new feedback item.

⚠️ Do not attach heap dumps to existing feedback items. Please create a new feedback item for each heap dump you would like to submit. If you were requested to provide a heap dump in order to resolve a previous feedback item, simply reply to the request with a link to the new feedback item where the heap dump is attached.

:bulb: **Most valuable feedback:** For this case, the most valuable feedback is the heap dump captured at the time of the crash.

# Performance Issues

When diagnosing performance issues, the goal is to capture a performance trace while performing the slow/hanging action.

- If necessary, start a second Visual Studio instance (with no solution open) to capture the trace. You can target the data collection from the Visual Studio encountering the problem.
- Perform the action that is causing performance issues while the trace is recording. If typing is delayed, type during the trace. If opening a file is slow, open a bunch of files.
- Try to repeat the action and capture a trace for 30 seconds to 2 minutes.
- *File new feedback issues* instead of commenting on existing issues. This allows us to correctly deduplicate issues.