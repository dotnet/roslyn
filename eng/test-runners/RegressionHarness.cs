// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.Loader;
using System.Text.Json;

// This standalone, package-free harness is compiled only by test-test-runners.ps1.
internal static class RegressionHarness
{
    private const BindingFlags All = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance;
    private static int s_checks;
    private static string s_root = "";
    private static string s_dotnet = "";

    private static async Task Main(string[] args)
    {
        var (repo, root, dotnet, localPath, helixPath) = (args[0], args[1], args[2], args[3], args[4]);
        s_root = root;
        s_dotnet = dotnet;
        var local = Load(localPath);
        var helix = Load(helixPath);
        foreach (var assembly in new[] { local, helix })
        {
            CheckOptions(assembly);
            CheckDiscovery(assembly);
            Check(Process(s_dotnet, root, succeeds: false, assembly.Location, "--unknown-option").Contains("Unrecognized arguments"), "Unknown CLI option must exit unsuccessfully");
            Check(Process(s_dotnet, root, succeeds: false, assembly.Location, "--helix").Contains("Unrecognized arguments"), "Retired mode switch must exit unsuccessfully");
        }
        CheckLocalResponse(local);
        CheckFailureLogging(local);
        CheckScheduling(helix);
        await CheckHelixArtifacts(helix, repo);
        CheckPreparedPayload(repo, localPath, helixPath, includeHelix: true);
        CheckPreparedPayload(repo, localPath, helixPath, includeHelix: false);
        Console.WriteLine($"PASS: {s_checks} offline runner regression assertions.");
        Console.WriteLine("LIMITATION: no real xUnit pass/fail, timeout/dump execution, single-partition marker, live Helix submission, or external monitoring is exercised.");
    }

    private static Assembly Load(string path)
    {
        var context = new AssemblyLoadContext(Path.GetFileNameWithoutExtension(path), isCollectible: true);
        var resolver = new AssemblyDependencyResolver(path);
        context.Resolving += (_, name) => resolver.ResolveAssemblyToPath(name) is { } resolved
            ? context.LoadFromAssemblyPath(resolved) : null;
        return context.LoadFromAssemblyPath(path);
    }

    private static string Prefix(Assembly assembly) => assembly.GetName().Name == "RunTests" ? "TestRunner.RunTests." : "TestRunner.Helix.";
    private static Type Type(Assembly assembly, string name) => assembly.GetType(name, throwOnError: true)!;
    private static object? Call(Type type, string name, params object?[] args)
        => type.GetMethod(name, All)!.Invoke(null, args);
    private static object Property(object value, string name) => value.GetType().GetProperty(name, All)!.GetValue(value)!;
    private static object[] Items(object value) => ((IEnumerable)value).Cast<object>().ToArray();

    private static void Check(bool condition, string message)
    {
        if (!condition)
            throw new InvalidOperationException(message);
        s_checks++;
    }

    private static (object? Options, bool Help, string Output) Parse(Assembly assembly, params string[] arguments)
    {
        var saved = Console.Out;
        using var output = new StringWriter();
        try
        {
            Console.SetOut(output);
            object?[] parameters = [arguments, false];
            var optionsTypeName = assembly.GetName().Name == "RunTests" ? "RunTestOptions" : "RunHelixOptions";
            var result = Call(Type(assembly, Prefix(assembly) + optionsTypeName), "Parse", parameters);
            return (result, (bool)parameters[1]!, output.ToString());
        }
        finally
        {
            Console.SetOut(saved);
        }
    }

    private static string[] Arguments(Assembly assembly, string artifacts, params string[] extra)
        => ["--artifactspath=" + artifacts, "--dotnet=" + s_dotnet, "--testPlatform=x64",
            .. (Prefix(assembly).Contains("Helix") ? new[] { "--helixQueueName=offline" } : []), .. extra];

    private static object Options(Assembly assembly, string artifacts, params string[] extra)
    {
        var result = Parse(assembly, Arguments(assembly, artifacts, extra));
        Check(result.Options is not null && !result.Help, $"Parse failed: {result.Output}");
        return result.Options!;
    }

    private static void CheckOptions(Assembly assembly)
    {
        var isLocal = Prefix(assembly).Contains("RunTests");
        string[] localFlags = ["--out", "--logs", "--timeout", "--testfilter", "--html", "--sequential", "--collectdumps"];
        string[] helixFlags = ["--helixQueueName", "--helixApiAccessToken", "--accessToken", "--projectUri", "--pipelineDefinitionId", "--targetBranchName"];
        var help = Parse(assembly, "--help");
        Check(help.Options is null && help.Help, "Help must succeed without path/queue validation");
        foreach (var flag in isLocal ? localFlags : helixFlags)
            Check(help.Output.Contains(flag, StringComparison.Ordinal), $"Missing help option {flag}");
        foreach (var flag in isLocal ? helixFlags : localFlags)
            Check(!help.Output.Contains(flag, StringComparison.Ordinal), $"Leaked help option {flag}");
        foreach (var flag in (isLocal ? helixFlags : localFlags).Concat(["--helix", "--unknown-option", "unexpected.dll"]))
        {
            var bad = Parse(assembly, Arguments(assembly, s_root, flag));
            Check(bad.Options is null && !bad.Help && bad.Output.Contains("Unrecognized arguments"), $"Accepted {flag}");
        }
        Check(Parse(assembly, "--help", "--unknown-option").Help is false, "Invalid arguments must not become successful help");
        Check((Parse(assembly).Options is not null) == isLocal, "Local defaults must parse; Helix defaults must require a queue");
        if (!isLocal)
            Check(Parse(assembly, "--artifactspath=" + s_root, "--dotnet=" + s_dotnet).Options is null, "Helix must require a queue");
        var defaultPlatform = Parse(assembly, Arguments(assembly, s_root).Where(x => !x.StartsWith("--testPlatform=", StringComparison.Ordinal)).ToArray()).Options!;
        Check((string)Property(defaultPlatform, "Architecture") == System.Runtime.InteropServices.RuntimeInformation.ProcessArchitecture.ToString().ToLowerInvariant(), "Default architecture");

        foreach (var invalid in new[] { "--testSet=wrong", "--testKind=wrong", "--testFramework=wrong", "--testKind=runtimeasync", "--env=DOTNET_RuntimeAsync=1" })
            Check(Parse(assembly, Arguments(assembly, s_root, invalid)).Options is null, $"Invalid common option accepted: {invalid}");
        var defaults = Options(assembly, s_root);
        Check(Convert.ToInt32(Property(defaults, "TestRuntime")) == 3, "Default runtime must include core and desktop");
        Check(Items(Property(defaults, "IncludeFilter")).SequenceEqual(new[] { ".*UnitTests.*" }), "Default include");
        Check(Items(Property(defaults, "ExcludeFilter")).Contains(@"\.InteractiveHost"), "Desktop x64 exclusion");
        var x86 = Options(assembly, s_root, "--testPlatform=x86");
        Check(!Items(Property(x86, "ExcludeFilter")).Contains(@"\.InteractiveHost"), "Desktop x86 must keep InteractiveHost");
        foreach (var (kind, key, value) in new[] { ("IOperation", "ROSLYN_TEST_IOPERATION", "true"), ("runtimeasync", "DOTNET_RuntimeAsync", "1"), ("usedassemblies", "ROSLYN_TEST_USEDASSEMBLIES", "true") })
        {
            var options = Options(assembly, s_root, "--testFramework=CORE", "--testKind=" + kind, "--ci", "--env=FLAG", "--env=VALUE=a=b", "--env=EMPTY=", "--env=FLAG=override");
            var env = (IDictionary)Property(options, "EnvironmentVariables");
            Check((string?)env[key] == value && (string?)env["ROSLYN_TEST_CI"] == "true", "Test kind/CI environment");
            Check((string?)env["FLAG"] == "override" && (string?)env["VALUE"] == "a=b" && (string?)env["EMPTY"] == "", "Environment syntax/last-value semantics");
            Check(!Items(Property(options, "ExcludeFilter")).Contains(@"\.InteractiveHost"), "Core must keep InteractiveHost");
        }
        var caseOptions = Options(assembly, s_root, "--env=CASE_KEY=first", "--env=case_key=second");
        var caseEnv = (IDictionary)Property(caseOptions, "EnvironmentVariables");
        Check((string?)caseEnv["CASE_KEY"] == (OperatingSystem.IsWindows() ? "second" : "first"), "Environment key platform comparer");
    }

    private static string Fixture(string root, string name, string tfm, string? fileName = null)
    {
        var directory = Path.Combine(root, "bin", name, "Debug", tfm);
        Directory.CreateDirectory(directory);
        var file = Path.Combine(directory, fileName ?? name + ".dll");
        File.Copy(Assembly.GetExecutingAssembly().Location, file);
        return file;
    }

    private static object Discover(Assembly assembly, object options)
        => Call(Type(assembly, "TestRunner.AssemblyDiscovery"), "GetAssemblyFilePaths", options)!;

    private static void Throws(Action action, string text)
    {
        try { action(); }
        catch (TargetInvocationException e) when (e.InnerException?.Message.Contains(text, StringComparison.Ordinal) == true)
        {
            s_checks++;
            return;
        }
        throw new InvalidOperationException($"Expected exception containing '{text}'");
    }

    private static void CheckDiscovery(Assembly assembly)
    {
        var root = Path.Combine(s_root, assembly.GetName().Name + "-discovery");
        var core = Fixture(root, "Extra.UnitTests", "net10.0");
        var desktop = Fixture(root, "Extra.UnitTests", "net472");
        var windows = Fixture(root, "Extra.UnitTests", "net10.0-windows");
        var mac = Fixture(root, "Extra.UnitTests", "net10.0-macos");
        Fixture(root, "Extra.UnitTests", "netstandard2.0");
        var alternate = Fixture(root, "AlternateProject", "net10.0", "Alternate.UnitTests.dll");
        var compiler = Fixture(root, "Microsoft.CodeAnalysis.CSharp.Syntax.UnitTests", "net10.0");
        Fixture(root, "Microsoft.CodeAnalysis.VisualBasic.Syntax.UnitTests", "net10.0");
        var options = Options(assembly, root, "--testSet=COMPILER", "--include='^Extra'", "--include=^AlternateProject$", "--exclude=VisualBasic");
        string[] expected = [core, desktop, alternate, compiler, .. (OperatingSystem.IsWindows() ? new[] { windows } : OperatingSystem.IsMacOS() ? new[] { mac } : [])];
        var actual = Items(Discover(assembly, options)).Select(x => (string)Property(x, "AssemblyPath")).ToArray();
        Check(actual.Order().SequenceEqual(expected.Order()), "Compiler union, quoted include, exclusion, alternate name or platform discovery changed");
        foreach (var framework in new[] { "core", "desktop" })
        {
            var paths = Items(Discover(assembly, Options(assembly, root, "--include=^Extra", "--testFramework=" + framework)))
                .Select(x => (string)Property(x, "AssemblyPath")).ToArray();
            Check(framework == "desktop" ? paths.SequenceEqual(new[] { desktop }) : !paths.Contains(desktop) && paths.Contains(core), "Runtime discovery selection");
        }
        var both = Options(assembly, root, "--testFramework=desktop", "--testFramework=core");
        Check(Convert.ToInt32(Property(both, "TestRuntime")) == 3, "Repeated runtime options must union");
        Throws(() => Discover(assembly, Options(assembly, root, "--include=^Missing$")), "Did not find any test assemblies");
        Fixture(root, "AmbiguousProject", "net10.0", "First.UnitTests.dll");
        Fixture(root, "AmbiguousProject", "net10.0", "Second.UnitTests.dll");
        Throws(() => Discover(assembly, Options(assembly, root, "--include=^AmbiguousProject$")), "Multiple unit test assemblies");
    }

    private static void CheckLocalResponse(Assembly assembly)
    {
        var root = Path.Combine(s_root, "local-rsp");
        var file = Fixture(root, "Local.UnitTests", "net10.0");
        var options = Options(assembly, root, "--testFramework=core", "--testfilter=FullyQualifiedName~Example", "--html", "--sequential", "--timeout=2", "--collectdumps");
        var assemblies = Discover(assembly, options);
        var workItems = Items(Call(Type(assembly, Prefix(assembly) + "TestRunner"), "CreateWorkItemsForFullAssemblies", assemblies)!);
        Check(workItems.Length == 1 && !File.Exists(Path.Combine(Path.GetDirectoryName(file)!, "testlist.json")), "Local scheduling must not need testlist.json");
        var rsp = (string)Call(Type(assembly, Prefix(assembly) + "ProcessTestExecutor"), "BuildRspFileContents", workItems[0], options, "results.xml", "results.html")!;
        foreach (var text in new[] { $"\"{file}\"", "TestTimeout=25minutes", "/Logger:html;LogFileName=results.html", "/TestCaseFilter:\"FullyQualifiedName~Example\"", "/ResultsDirectory:" + Property(options, "TestResultsDirectory") })
            Check(rsp.Contains(text, StringComparison.Ordinal), "Missing local response content: " + text);
        Check((bool)Property(options, "Sequential") && (bool)Property(options, "CollectDumps") && (TimeSpan)Property(options, "Timeout") == TimeSpan.FromMinutes(2), "Local execution flags");
    }

    private static void CheckFailureLogging(Assembly assembly)
    {
        var root = Path.Combine(s_root, "failure-logging");
        Fixture(root, "Failure.UnitTests", "net10.0");
        var logs = Path.Combine(root, "missing", "logs");
        var options = Options(assembly, root, "--testFramework=core", "--logs=" + logs);
        var runnerType = Type(assembly, Prefix(assembly) + "TestRunner");
        var workItem = Items(Call(runnerType, "CreateWorkItemsForFullAssemblies", Discover(assembly, options))!)[0];
        var resultInfo = Activator.CreateInstance(Type(assembly, Prefix(assembly) + "TestResultInfo"), All, null,
            [1, null, null, TimeSpan.Zero, "failure output", "failure error"], null)!;
        var resultType = Type(assembly, Prefix(assembly) + "TestResult");
        var constructor = resultType.GetConstructors(All).Single();
        var processes = Activator.CreateInstance(constructor.GetParameters()[3].ParameterType);
        var result = constructor.Invoke([workItem, resultInfo, "test command", processes, null]);
        var executor = Activator.CreateInstance(Type(assembly, Prefix(assembly) + "ProcessTestExecutor"), nonPublic: true);
        var runner = Activator.CreateInstance(runnerType, All, null, [options, executor], null);

        Check(!Directory.Exists(logs), "Failure logging fixture must start without a log directory");
        runnerType.GetMethod("PrintFailedTestResult", All)!.Invoke(runner, [result]);
        var log = Path.Combine(logs, $"xUnitFailure-{Property(result, "DisplayName")}.log");
        Check(File.ReadAllText(log) == "failure output", "Failure output must be written to a newly created log directory");
    }

    private static string[] SchedulerFixtures(string root)
    {
        return Enumerable.Range(0, 3).Select(i =>
        {
            var file = Fixture(root, $"Schedule{i}.UnitTests", "net10.0");
            File.WriteAllText(Path.Combine(Path.GetDirectoryName(file)!, "testlist.json"), JsonSerializer.Serialize(new[]
            {
                new { MethodName = $"Tests.Type{i}.First", HasAsyncLifetime = true },
                new { MethodName = $"Tests.Type{i}.Second", HasAsyncLifetime = false },
            }));
            return file;
        }).ToArray();
    }

    private static void CheckScheduling(Assembly assembly)
    {
        var files = SchedulerFixtures(Path.Combine(s_root, "schedule"));
        var scheduler = Type(assembly, Prefix(assembly) + "AssemblyScheduler");
        object[] Schedule(string architecture, object? history) => Items(Call(scheduler, "Schedule", files, architecture, history)!);
        var count = Schedule("x64", null);
        Check(count.Length == 6 && count.All(x => Items(Property(x, "TestMethodNames")).Length == 1), "Count fallback partitions");
        Check(count.Select(x => (string)Property(x, "DisplayName")).SequenceEqual(Enumerable.Range(0, 6).Select(i => $"workitem_{i}")), "Work item names");
        var history = new Dictionary<string, (TimeSpan Duration, int TestTheoryInstances)> { ["Tests.Type0.First"] = (TimeSpan.FromSeconds(1), 2) };
        var timed = Schedule("x64", history);
        Check(timed.Length == 1 && Items(Property(timed[0], "AssemblyFilePaths")).Length == 3, "History must combine small assemblies");
        Check((TimeSpan)Property(timed[0], "EstimatedExecutionTime") == TimeSpan.FromSeconds(8.8), "History fallback/async lifetime overhead");
        var x86 = Schedule("X86", history);
        Check(x86.Length == 2 && x86.All(x => Items(Property(x, "AssemblyFilePaths")).Length <= 2), "x86 maximum two assemblies");
        var rsp = (string)Call(Type(assembly, Prefix(assembly) + "HelixTestRunner"), "GetRspFileContent", new List<string> { "relative.dll" }, new[] { "Tests.Type0.First" }, "x86")!;
        foreach (var text in new[] { "\"relative.dll\"", "TestTimeout=15minutes", "/ResultsDirectory:.", "/Logger:xunit;LogFilePath=test-results.xml", "FullyQualifiedName=Tests.Type0.First" })
            Check(rsp.Contains(text, StringComparison.Ordinal), "Missing Helix response content: " + text);
    }

    private static async Task CheckHelixArtifacts(Assembly assembly, string repo)
    {
        var root = Path.Combine(s_root, "helix-artifacts");
        var artifacts = Path.Combine(root, "artifacts");
        SchedulerFixtures(artifacts);
        Directory.CreateDirectory(Path.Combine(root, "eng"));
        File.Copy(Path.Combine(repo, "global.json"), Path.Combine(root, "global.json"));
        File.Copy(Path.Combine(repo, "NuGet.config"), Path.Combine(root, "NuGet.config"));
        // A child process owns all environment changes; explicitly empty the token
        // option as well, so ambient CI credentials can never enable history calls.
        var options = Options(assembly, artifacts, "--testFramework=core", "--accessToken=", "--env=OFFLINE_FLAG=enabled");
        var probe = Path.Combine(root, "link-probe");
        try
        {
            Directory.CreateSymbolicLink(probe, Path.Combine(root, "eng"));
            Directory.Delete(probe);
        }
        catch (UnauthorizedAccessException)
        {
            Console.WriteLine("SKIP: artifact generation requires symbolic-link permission (Windows Developer Mode).");
            return;
        }
        catch (IOException e) when ((e.HResult & 0xffff) == 1314)
        {
            Console.WriteLine("SKIP: artifact generation requires symbolic-link permission (Windows Developer Mode).");
            return;
        }
        var task = (Task<string>)Call(Type(assembly, Prefix(assembly) + "HelixTestRunner"), "CreateHelixArtifactsAsync", options, Discover(assembly, options), CancellationToken.None)!;
        var project = await task;
        var xml = File.ReadAllText(project);
        Check(xml.Contains("<EnableHelixJobMonitor>true</EnableHelixJobMonitor>") && xml.Contains("<Timeout>01:00:00</Timeout>"), "Helix monitor/count fallback timeout");
        Check(xml.Contains("<HelixTargetQueues>offline</HelixTargetQueues>"), "Helix queue metadata");
        var payloads = Directory.GetDirectories(Path.Combine(artifacts, "payloads"));
        Check(payloads.Length == 6, "Generated payload count");
        foreach (var payload in payloads)
        {
            Check(File.ReadAllText(Path.Combine(payload, "vstest.rsp")).Contains("TestTimeout=15minutes"), "Payload response timeout");
            var commands = Directory.GetFiles(payload, OperatingSystem.IsWindows() ? "*.cmd" : "*.sh");
            Check(commands.Any(p => File.ReadAllText(p).Contains("OFFLINE_FLAG")), "Payload environment propagation");
        }
        Check(File.Exists(Path.Combine(artifacts, "log", "Debug", "helix.proj")), "Submission diagnostics copy");
    }

    private static string Process(string executable, string workingDirectory, bool succeeds, params string[] arguments)
    {
        var info = new ProcessStartInfo(executable) { WorkingDirectory = workingDirectory, RedirectStandardOutput = true, RedirectStandardError = true };
        foreach (var argument in arguments)
            info.ArgumentList.Add(argument);
        using var process = System.Diagnostics.Process.Start(info)!;
        var output = process.StandardOutput.ReadToEndAsync();
        var error = process.StandardError.ReadToEndAsync();
        if (!process.WaitForExit(30_000))
        {
            process.Kill(entireProcessTree: true);
            throw new TimeoutException(executable);
        }
        var text = output.GetAwaiter().GetResult() + error.GetAwaiter().GetResult();
        Check((process.ExitCode == 0) == succeeds, $"{executable} exited {process.ExitCode}: {text}");
        return text;
    }

    private static void CheckPreparedPayload(string repo, string localPath, string helixPath, bool includeHelix)
    {
        var root = Path.Combine(s_root, includeHelix ? "prepared-both" : "prepared-local");
        var source = Path.Combine(root, "source");
        var destination = Path.Combine(root, "destination");
        Directory.CreateDirectory(Path.Combine(source, "eng"));
        foreach (var file in new[] { "global.json", "NuGet.config" })
            File.Copy(Path.Combine(repo, file), Path.Combine(source, file));
        foreach (var runner in includeHelix ? new[] { localPath, helixPath } : new[] { localPath })
        {
            var sourceDirectory = Path.GetDirectoryName(runner)!;
            var target = Path.Combine(source, "artifacts", "bin", Path.GetFileNameWithoutExtension(runner), "Debug", Path.GetFileName(sourceDirectory));
            foreach (var file in Directory.GetFiles(sourceDirectory, "*", SearchOption.AllDirectories))
            {
                var output = Path.Combine(target, Path.GetRelativePath(sourceDirectory, file));
                Directory.CreateDirectory(Path.GetDirectoryName(output)!);
                File.Copy(file, output);
            }
        }
        MinimizeUtil.Run(source, destination, isUnix: !OperatingSystem.IsWindows());
        var rehydrate = Path.Combine(destination, OperatingSystem.IsWindows() ? "rehydrate-all.cmd" : "rehydrate-all.sh");
        Process(OperatingSystem.IsWindows() ? Environment.GetEnvironmentVariable("ComSpec")! : "bash",
            destination, succeeds: true, OperatingSystem.IsWindows() ? new[] { "/d", "/c", rehydrate } : new[] { rehydrate });
        foreach (var name in includeHelix ? new[] { "RunTests", "RunHelix" } : new[] { "RunTests" })
        {
            var binary = Directory.GetFiles(Path.Combine(destination, "artifacts", "bin", name), name + ".dll", SearchOption.AllDirectories).Single();
            Check(File.Exists(Path.Combine(Path.GetDirectoryName(binary)!, OperatingSystem.IsWindows() ? "rehydrate.cmd" : "rehydrate.sh")), "Runner rehydration script");
            Check(Process(s_dotnet, destination, succeeds: true, binary, "--help").Contains("Usage: " + name), "Prepared runner startup");
            var noArgs = Process(s_dotnet, destination, succeeds: false, binary);
            Check(noArgs.Contains(name == "RunTests" ? "Did not find any test assemblies" : "--helixQueueName is required"), "No-argument diagnostics");
        }
        Check(includeHelix || !Directory.Exists(Path.Combine(destination, "artifacts", "bin", "RunHelix")), "Local-only payload must tolerate absent RunHelix");
    }
}
