This document provides specific guidance and best practices for reporting crashes and performance issues in Visual Studio by gathering heap dumps / performance traces as part of Visual Studio's built-in Report a Problem workflow. For a general overview of how to report problems in Visual Studio, see "[How to Report a Problem with Visual Studio 2017](https://docs.microsoft.com/en-us/visualstudio/ide/how-to-report-a-problem-with-visual-studio-2017)". 

:arrow_right: **The product teams review all feedback submitted through the [Report a Problem](https://docs.microsoft.com/en-us/visualstudio/ide/how-to-report-a-problem-with-visual-studio-2017) tool.** Users are not required to follow the steps in this document to provide feedback.

# Audience

This document describes the best known ways to report actionable product feedback, i.e. feedback for which the product team is most likely to diagnose and resolve quickly. It focuses on two categories of severe problems which are historically challenging to resolve.

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

For these issues, follow the steps in "[How to Report a Problem](https://docs.microsoft.com/en-us/visualstudio/ide/how-to-report-a-problem-with-visual-studio-2017)" and be sure to include:
- The steps to reproduce the problem
- A standalone repro project as described above. If this is not possible, then please include:
    - The language of the open projects (C#, C++, etc.)
    - The kind of project (Console Application, ASP.NET, etc.)
    - Any extensions that are installed

:bulb: **Most valuable feedback:** For this case, the most valuable feedback is the set of steps to reproduce the issue along with sample source code.

## Unknown crashes

If you're not sure what's causing your crashes or they seem random, then you can capture dumps locally each time Visual Studio crashes and attach those to separate feedback items. To save dumps locally when Visual Studio crashes, set the following registry entries:

```
[HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\Windows Error Reporting\LocalDumps\devenv.exe]
"DumpFolder"="C:\\Crashdumps"
"DumpCount"=dword:00000005
"DumpType"=dword:00000002

[HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\Windows Error Reporting\LocalDumps\ServiceHub.RoslynCodeAnalysisService32.exe]
"DumpFolder"="C:\\Crashdumps"
"DumpCount"=dword:00000005
"DumpType"=dword:00000002

[HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\Windows Error Reporting\LocalDumps\ServiceHub.RoslynCodeAnalysisService.exe]
"DumpFolder"="C:\\Crashdumps"
"DumpCount"=dword:00000005
"DumpType"=dword:00000002
```

⚠️ Each dump file produced by this method will be up to 4GiB in size. Make sure to set `DumpFolder` to a location with adequate drive space or adjust the `DumpCount` appropriately.

Each time Visual Studio crashes, it will create a dump file **devenv.exe.[number].dmp** file in the configured location. If one of the helper processes crashes, it will create a dump file **ServiceHub.RoslynCodeAnalysisService32.exe.[number].dmp** or **ServiceHub.RoslynCodeAnalysisService.exe.[number].dmp** file in the configured location.

Then, use Visual Studio's "Report a Problem..." feature. It will allow you to attach the appropriate dump.

1. Locate the dump file for the crash you are reporting (look for a file with the correct Creation time)
2. If possible, zip the file (*.zip) to reduce its size before submitting feedback
3. Follow the steps in "[How to Report a Problem](https://docs.microsoft.com/en-us/visualstudio/ide/how-to-report-a-problem-with-visual-studio-2017)", and attach the heap dump to a new feedback item.

⚠️ Do not attach heap dumps to existing feedback items. Please create a new feedback item for each heap dump you would like to submit. If you were requested to provide a heap dump in order to resolve a previous feedback item, simply reply to the request with a link to the new feedback item where the heap dump is attached.

:bulb: **Most valuable feedback:** For this case, the most valuable feedback is the heap dump captured at the time of the crash.

# Performance Issues

When diagnosing performance issues, the goal is to capture a performance trace while performing the slow/hanging action. 

:bulb: When possible, isolate each scenario in a separate, specific feedback report. For example, if typing and navigation are both slow, follow the steps below once per issue. This helps the product team isolate the cause of specific issues.

For best results in capturing the performance, follow these steps:

1. If not already running, have a copy of Visual Studio open where you will reproduce the problem
    * Have everything set up to reproduce the problem. For example, if you need a particular project to be loaded with a specific file opened, then be sure both of those steps are complete before proceeding.
    * If you are *not* reporting a problem specific to loading a solution, try to wait 5-10 minutes (or more, depending on solution size) after opening the solution before recording the performance trace. The solution load process produces a large amount of data, so waiting for a few minutes helps us focus on the specific problem you are reporting.
2. Start a second copy of Visual Studio *with no solution open*
3. In the new copy of Visual Studio, open the **Report a Problem** tool
4. Follow the steps in "[How to Report a Problem](https://docs.microsoft.com/en-us/visualstudio/ide/how-to-report-a-problem-with-visual-studio-2017)" until you reach the "Provide a trace and heap dump (optional)" step.
5. Choose to record the first copy of Visual Studio (the one encountering performance problems), and start recording.
    * The Steps Recorder application will appear begin recording. 
    * **During the recording,** perform the problematic action in the separate copy of Visual Studio. It is very difficult for us to correct specific performance problems if they do not appear within the recorded time.
    * If the action is shorter than 30 seconds and can be easily repeated, repeat the action to further demonstrate the problem.
    * For most cases, a trace of 60 seconds is sufficient to demonstrate the problems, especially if the problematic action lasted (or was repeated) for more than 30 seconds. The duration can be adjusted as necessary to capture the behavior you would like fixed.
6. Click "Stop Record" in Steps Recorder. It may take a few minutes to process the performance trace. 
7. Once complete, there will be several attachments to your feedback. Attach any additional files that may help reproduce the problem (a sample project, screenshots, videos, etc.).
8. Submit the feedback.

⚠️ Do not directly attach performance traces to existing feedback items on Developer Community website. Requesting/providing additional information is a supported workflow in Visual Studio's built-in Report a Problem tool. If a performance trace is required in order to resolve a previous feedback item, we will set the state of the feedback item to "Need More Info", which can be responded to in the same way as reporting a new problem. For detailed instruction, please refer to ["Need More Info" section](https://docs.microsoft.com/en-us/visualstudio/ide/how-to-report-a-problem-with-visual-studio-2017?view=vs-2017#when-further-information-is-needed-need-more-info) in Report a Problem tool's document.



:bulb: **Most valuable feedback:** For almost all performance issues, the most valuable feedback is a high-level description of what you were trying to do, along with the performance trace (*.etl.zip) which captures the behavior during that time.

## Advanced Performance Traces

Steps for manually recording performance traces using the PerfView tool can be found on the [Recording performance traces with PerfView](https://github.com/dotnet/roslyn/blob/main/docs/wiki/Recording-performance-traces-with-PerfView.md) page.
