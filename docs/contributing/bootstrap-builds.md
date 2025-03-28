# Bootstrap Builds

The build correctness leg ensures that the latest code in dotnet/roslyn can be used to build dotnet/roslyn. Essentially our compiler can bootstrap itself.

## Why? 

The reason we go through so much effort is failures of this nature are incredibly hard to track down after the fact. The compiler is a complex tool and dotnet/roslyn is a large code base. It can take significant time to work out which of the ~300 commits since the last successful bootstrap is causing the current one to fail.

In the past we used to run a bootstrap exercise roughly once a month. Every time there was a failure to build dotnet/roslyn with the latest compiler and it often took weeks for us to understand what change caused the failure. That resulted in a **lot** of wasted time and almost caused us to slip several releases.

The build correctness jobs was introduced to solve this problem. The compiler verifies it can bootstrap on every change which means we find the failures immediately.

## Process

The build correctness job works by first building the Microsoft.Net.Compilers.Toolset package. This gives us a functioning compiler with the latest changes. This build occurs using the `/define:BOOTSTRAP` which allows the compiler to make failures more actionable. This is primarily leveraged in the following ways:

- Inserts a [ExitingTraceListener](https://github.com/dotnet/roslyn/blob/main/src/Compilers/Shared/ExitingTraceListener.cs) into the process trace listeners. This means any `Debug.Assert` failure will result in the compilation failing with an actionable stack trace.
- Defines a [ValidateBootstrap](https://github.com/dotnet/roslyn/blob/main/src/Compilers/Core/MSBuildTask/ValidateBootstrap.cs). This lets us validate that the compiler used in the bootstrap build is actually the one we built vs. the default. This helps protect against build authoring changes which could inadvertently cause the default compiler to be used in a bootstrap build.

The job then cleans out all of the artifacts from the build and starts a normal build of Roslyn.sln but specifies `/p:BootstrapBuildPath=...`. This causes two files to be loaded:

- [Bootstrap.props](https://github.com/dotnet/roslyn/blob/main/eng/targets/Bootstrap.props): loads the bootstrap compiler over the default one
- [Bootstrap.targets](https://github.com/dotnet/roslyn/blob/main/eng/targets/Bootstrap.targets): verifies the bootstrap compiler was actually used

This leg also ensures that the binary log and the build server log are captured in the set of published artifacts to allow for easy investigations.

## Investigating 

The first step for investigating a correctness build failure is downloading the log files. These are available in the published artifacts

![Published Artifacts](images/bootstrap-logs.png)

The two most interesting files are:

1. Build.Server.log: this is the text log of the compilation process. All stack traces and server error messages will be present in this file
1. Build.binlog: this is the binary log that results from building dotnet/roslyn with a bootstrap compiler.

The build server log file will contain the reason why the particular request to the compiler failed. In most cases searching for one of two terms will take you straight to the failure. 

The first term to search for is `"Debug.Assert"` (no quotes). The most common cause of a bootstrap failure is a `Debug.Assert` call failing during compilation. This will result in an exception being added to the log with the full stack trace. For example: 

```txt
ID=VBCSCompiler TID=33: Debug.Assert failed with message: Fail: 
Stack Trace
   at Microsoft.CodeAnalysis.CommandLine.ExitingTraceListener.Exit(String originalMessage)
   at Microsoft.CodeAnalysis.CommandLine.ExitingTraceListener.WriteLine(String message)
   at System.Diagnostics.TraceInternal.Fail(String message)
   at System.Diagnostics.Debug.Assert(Boolean condition)
   at Microsoft.CodeAnalysis.GeneratorDriver..ctor(GeneratorDriverState state)
   at Microsoft.CodeAnalysis.CSharp.CSharpGeneratorDriver..ctor(GeneratorDriverState state)
   at Microsoft.CodeAnalysis.CSharp.CSharpGeneratorDriver.FromState(GeneratorDriverState state)
   at Microsoft.CodeAnalysis.GeneratorDriver.RunGeneratorsAndUpdateCompilation(Compilation compilation, Compilation& outputCompilation, ImmutableArray`1& diagnostics, CancellationToken cancellationToken)
   at Microsoft.CodeAnalysis.CommonCompiler.RunGenerators(Compilation input, ParseOptions parseOptions, ImmutableArray`1 generators, AnalyzerConfigOptionsProvider analyzerConfigOptionsProvider, ImmutableArray`1 additionalTexts, DiagnosticBag generatorDiagnostics)
   at Microsoft.CodeAnalysis.CommonCompiler.CompileAndEmit(TouchedFileLogger touchedFilesLogger, Compilation& compilation, ImmutableArray`1 analyzers, ImmutableArray`1 generators, ImmutableArray`1 additionalTextFiles, AnalyzerConfigSet analyzerConfigSet, ImmutableArray`1 sourceFileAnalyzerConfigOptions, ImmutableArray`1 embeddedTexts, DiagnosticBag diagnostics, CancellationToken cancellationToken, CancellationTokenSource& analyzerCts, AnalyzerDriver& analyzerDriver, Nullable`1& generatorTimingInfo)
   at Microsoft.CodeAnalysis.CommonCompiler.RunCore(TextWriter consoleOutput, ErrorLogger errorLogger, CancellationToken cancellationToken)
   at Microsoft.CodeAnalysis.CommonCompiler.Run(TextWriter consoleOutput, CancellationToken cancellationToken)
   at Microsoft.CodeAnalysis.CompilerServer.CompilerServerHost.RunCompilation(RunRequest& request, CancellationToken cancellationToken)
   at Microsoft.CodeAnalysis.CompilerServer.CompilerServerHost.Microsoft.CodeAnalysis.CompilerServer.ICompilerServerHost.RunCompilation(RunRequest& request, CancellationToken cancellationToken)
   at Microsoft.CodeAnalysis.CompilerServer.ClientConnectionHandler.<>c__DisplayClass8_0.<ProcessCompilationRequestAsync>b__1()
   at System.Threading.Tasks.Task`1.InnerInvoke()
   at System.Threading.Tasks.Task.Execute()
   at System.Threading.ExecutionContext.RunInternal(ExecutionContext executionContext, ContextCallback callback, Object state, Boolean preserveSyncCtx)
   at System.Threading.ExecutionContext.Run(ExecutionContext executionContext, ContextCallback callback, Object state, Boolean preserveSyncCtx)
   at System.Threading.Tasks.Task.ExecuteWithThreadLocal(Task& currentTaskSlot)
   at System.Threading.Tasks.Task.ExecuteEntry(Boolean bPreventDoubleExecution)
   at System.Threading.ExecutionContext.RunInternal(ExecutionContext executionContext, ContextCallback callback, Object state, Boolean preserveSyncCtx)
   at System.Threading.ExecutionContext.Run(ExecutionContext executionContext, ContextCallback callback, Object state, Boolean preserveSyncCtx)
   at System.Threading.ExecutionContext.Run(ExecutionContext executionContext, ContextCallback callback, Object state)
   at System.Threading.ThreadHelper.ThreadStart(Object obj)
   ```

These failures are almost always due to changes being tested. Essentially the change updated compiler logic in such a way that it caused an `Assert` to fail. On occasion this will also fail because an IDE change introduces a coding pattern that sets of a latent bug in the compiler or analyzer but this is certainly the rare case.

The next term to search for is `"Error "` (no quotes but keep the space). This will be added to the log every the server hits an error and needs to shut down.

```txt
ID=MSBuild 60300 TID=3: Error Error: 'EndOfStreamException' 'Reached end of stream before end of read.' occurred during 'Reading response for d2c3aeac-bd8a-4251-bde0-2e11bbc57d13'
Stack trace:
   at Microsoft.CodeAnalysis.CommandLine.BuildProtocolConstants.<ReadAllAsync>d__4.MoveNext() in C:\Users\jaredpar\code\wt\ros2\src\Compilers\Core\CommandLine\BuildProtocol.cs:line 641
--- End of stack trace from previous location where exception was thrown ---
   at System.Runtime.CompilerServices.TaskAwaiter.ThrowForNonSuccess(Task task)
   at System.Runtime.CompilerServices.TaskAwaiter.HandleNonSuccessAndDebuggerNotification(Task task)
   at System.Runtime.CompilerServices.ConfiguredTaskAwaitable.ConfiguredTaskAwaiter.GetResult()
   at Microsoft.CodeAnalysis.CommandLine.BuildResponse.<ReadAsync>d__5.MoveNext() in C:\Users\jaredpar\code\wt\ros2\src\Compilers\Core\CommandLine\BuildProtocol.cs:line 342
--- End of stack trace from previous location where exception was thrown ---
   at System.Runtime.CompilerServices.TaskAwaiter.ThrowForNonSuccess(Task task)
   at System.Runtime.CompilerServices.TaskAwaiter.HandleNonSuccessAndDebuggerNotification(Task task)
   at System.Runtime.CompilerServices.ConfiguredTaskAwaitable`1.ConfiguredTaskAwaiter.GetResult()
   at Microsoft.CodeAnalysis.CommandLine.BuildServerConnection.<<RunServerBuildRequestAsync>g__tryRunRequestAsync|7_1>d.MoveNext() in C:\Users\jaredpar\code\wt\ros2\src\Compilers\Shared\BuildServerConnection.cs:line 288
```

These type of errors, when not paired with a `Debug.Assert` failure, are almost always bugs in the compiler server. Please contact the compiler team to help track down such failures.

Note: when you encounter a case where the log file does not have an actionable description of why a build failed, strongly consider sending a PR that fixes this. This approach is why the log is such a valuable tool for tracking down bootstrap failures. 

### Reproducing locally

Locally reproducing and attaching a debugger to a bootstrap build failure is fairly simple with use of `src/Tools/Replay`, which can replay a binlog with the checked-out Roslyn compiler source. To do this, run a build locally with the `-bl` flag to get a binary log, then use replay to replay the binary log:

**Windows**

```powershell
.\build.ps1 -bl
dotnet run --framework net9.0 src\Tools\Replay\ artifacts\log\Debug\Build.binlog -w
```

**Linux**

```sh
./build.sh -bl
dotnet run --framework net9.0 src/Tools/Replay/ artifacts/log/Debug/Build.binlog -w
```

The `-w` flag will cause replay to print the process ID of the compiler and wait until you hit a key, so that you can attach a debugger to the process and set breakpoints for exceptions you are interested in.

## Debugging

To debug a bootstrap build failure locally do the following. 

The first step is disabling the `ExitingTraceListener`. This is important for CI where the compiler needs to crash on a `Debug.Assert` failure vs. popping up a dialog that would hang CI. When debugging locally though developers want the `Debug.Assert` pops up a dialog behavior. To disable the `ExitingTraceListener` comment out the following line: 

https://github.com/dotnet/roslyn/blob/d73d31cbccb9aa850f3582afb464b709fef88fd7/src/Compilers/Server/VBCSCompiler/VBCSCompiler.cs#L22

Next just run the bootstrap build locally, wait for the `Debug.Assert` to trigger which pops up a dialog. From there you can attach to the VBCSCompiler process and debug through the problem

```cmd
> Build.cmd -bootstrap
```
