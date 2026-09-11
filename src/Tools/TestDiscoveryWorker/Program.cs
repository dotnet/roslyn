// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Mono.Options;
using Xunit;
using Xunit.Runner.Common;
using Xunit.Sdk;
using Xunit.v3;

const int ExitFailure = 1;
const int ExitSuccess = 0;

string? assemblyFilePath = null;
string? outputFilePath = null;

var options = new OptionSet
{
    { "assembly=", "The assembly file to process.", v => assemblyFilePath = v },
    { "out=", "The output file name.", v => outputFilePath = v }
};

try
{
    List<string> extra = options.Parse(args);

    if (assemblyFilePath is null)
    {
        Console.WriteLine("Must pass an assembly file name.");
        return ExitFailure;
    }

    if (extra.Count > 0)
    {
        Console.WriteLine($"Unknown arguments: {string.Join(" ", extra)}");
        return ExitFailure;
    }

    // Resolve to an absolute path up front: xUnit changes the current directory to the test
    // assembly's own directory during discovery, so any relative path computed from
    // assemblyFilePath later on would otherwise silently break.
    assemblyFilePath = Path.GetFullPath(assemblyFilePath);

    // Same reasoning as above: normalize an explicit --out path too, since discovery changes
    // the current directory before the output file is written.
    outputFilePath = outputFilePath is null
        ? Path.Combine(Path.GetDirectoryName(assemblyFilePath)!, "testlist.json")
        : Path.GetFullPath(outputFilePath);

#if NET
    var resolver = new System.Runtime.Loader.AssemblyDependencyResolver(assemblyFilePath);
    System.Runtime.Loader.AssemblyLoadContext.Default.Resolving += (context, assemblyName) =>
    {
        var assemblyPath = resolver.ResolveAssemblyToPath(assemblyName);
        if (assemblyPath is not null)
        {
            return context.LoadFromAssemblyPath(assemblyPath);
        }

        return null;
    };
#else
    // .NET Framework only probes its own directory (and the GAC) for dependencies, but the test
    // assembly's dependencies live alongside it, not alongside this worker. Resolve them from
    // there instead.
    string testAssemblyDirectory = Path.GetDirectoryName(assemblyFilePath)!;
    AppDomain.CurrentDomain.AssemblyResolve += (sender, resolveArgs) =>
    {
        var candidatePath = Path.Combine(testAssemblyDirectory, new AssemblyName(resolveArgs.Name).Name + ".dll");
        return File.Exists(candidatePath) ? Assembly.LoadFrom(candidatePath) : null;
    };
#endif

    string assemblyFileName = Path.GetFileName(assemblyFilePath);
#if NET
    string tfm = "(.NET Core)";
#else
    string tfm = "(.NET Framework)";
#endif

    Console.Write($"Discovering tests in {tfm} {assemblyFileName} ... ");

    var testAssembly = Assembly.LoadFrom(assemblyFilePath);
    var assemblyMetadata = AssemblyUtility.GetAssemblyMetadata(assemblyFilePath)
        ?? throw new InvalidOperationException($"Could not determine the xUnit test framework used by '{assemblyFilePath}'.");
    var projectAssembly = new XunitProjectAssembly(new XunitProject(), assemblyFilePath, assemblyMetadata);

    // InProcessTestProcessLauncher requires xunit.v3.runner.inproc.console to already be loaded
    // into this process, but .NET only loads assemblies on first use. Since nothing else in this
    // worker references that assembly, force-load it from the test assembly's own output
    // directory before creating the front controller.
    string inProcConsolePath = Path.Combine(Path.GetDirectoryName(assemblyFilePath)!, "xunit.v3.runner.inproc.console.dll");
    if (File.Exists(inProcConsolePath))
    {
#if NET
        System.Runtime.Loader.AssemblyLoadContext.Default.LoadFromAssemblyPath(inProcConsolePath);
#else
        Assembly.LoadFrom(inProcConsolePath);
#endif
    }

    // Roslyn's xUnit v3 test projects override TargetExt to .dll (to satisfy the repo's
    // *.UnitTests.dll naming requirement), so they never produce the native apphost executable
    // that the default out-of-process launcher requires. Use the in-process launcher instead,
    // which loads the test assembly via reflection and requires no apphost.
    await using var controller = XunitFrontController.Create(projectAssembly, testProcessLauncher: InProcessTestProcessLauncher.Instance)
        ?? throw new InvalidOperationException($"Could not create a test framework front controller for '{assemblyFilePath}'.");
    var sink = new Sink(testAssembly);
    var discoveryOptions = TestFrameworkOptions.ForDiscovery(projectAssembly.Configuration);
    controller.Find(sink, new FrontControllerFindSettings(discoveryOptions, projectAssembly.Configuration?.Filters ?? new XunitFilters()));

    var testsToWrite = new Dictionary<string, bool>();
    await foreach (var (fullyQualifiedName, hasAsyncLifetime) in sink.GetTestCaseInfosAsync().ConfigureAwait(false))
    {
        if (!testsToWrite.ContainsKey(fullyQualifiedName))
            testsToWrite[fullyQualifiedName] = hasAsyncLifetime;
    }

    if (sink.AnyWriteFailures)
    {
        Console.WriteLine($"Channel failed to write for '{assemblyFileName}'");
        return ExitFailure;
    }

    Console.WriteLine($"{testsToWrite.Count} found");

    var testInfos = testsToWrite
        .OrderBy(x => x.Key)
        .Select(x => new TestInfo(x.Key, x.Value))
        .ToArray();
    using var fileStream = new FileStream(outputFilePath, FileMode.OpenOrCreate, FileAccess.Write, FileShare.None);
    await JsonSerializer.SerializeAsync(fileStream, testInfos).ConfigureAwait(false);
    return ExitSuccess;
}
catch (OptionException e)
{
    Console.WriteLine(e.Message);
    options.WriteOptionDescriptions(Console.Out);
    return ExitFailure;
}
catch (Exception ex)
{
    // Write the exception details to stderr so the host process can pick it up.
    Console.WriteLine(ex.ToString());
    return ExitFailure;
}

file class TestInfo
{
    public string MethodName { get; set; } = "";
    public bool HasAsyncLifetime { get; set; }

    public TestInfo() { }

    public TestInfo(string methodName, bool hasAsyncLifetime)
    {
        MethodName = methodName;
        HasAsyncLifetime = hasAsyncLifetime;
    }
}

file class Sink : IMessageSink
{
    private const string AsyncLifetimeInterfaceName = "Xunit.IAsyncLifetime";

    public bool AnyWriteFailures { get; private set; }

    public Sink(Assembly testAssembly)
    {
        _testAssembly = testAssembly;
        _channel = Channel.CreateUnbounded<(string FullName, bool HasAsyncLifetime)>();
    }

    private readonly Assembly _testAssembly;
    private readonly Channel<(string FullName, bool HasAsyncLifetime)> _channel;
    private readonly Dictionary<string, bool> _asyncLifetimeCache = new();
    private readonly Dictionary<string, Type?> _typeCache = new();

    public async IAsyncEnumerable<(string FullName, bool HasAsyncLifetime)> GetTestCaseInfosAsync()
    {
        while (await _channel.Reader.WaitToReadAsync(CancellationToken.None).ConfigureAwait(false))
        {
            while (_channel.Reader.TryRead(out var item))
            {
                yield return item;
            }
        }
    }

    public bool OnMessage(IMessageSinkMessage message)
    {
        if (message is ITestCaseDiscovered discoveryMessage)
        {
            OnTestDiscovered(discoveryMessage);
        }

        if (message is IDiscoveryComplete)
        {
            _channel.Writer.Complete();
        }

        return true;
    }

    private void OnTestDiscovered(ITestCaseDiscovered testCaseDiscovered)
    {
        var className = testCaseDiscovered.TestClassName;
        var methodName = testCaseDiscovered.TestMethodName;

        if (string.IsNullOrEmpty(className) || string.IsNullOrEmpty(methodName))
        {
            AnyWriteFailures = true;
            return;
        }

        var fullName = $"{className}.{methodName}";
        var hasAsyncLifetime = HasAsyncLifetime(className);

        // this shouldn't happen as our channel is unbounded but we are Paranoid Coding™️
        if (!_channel.Writer.TryWrite((fullName, hasAsyncLifetime)))
        {
            AnyWriteFailures = true;
        }
    }

    private bool HasAsyncLifetime(string typeName)
    {
        if (_asyncLifetimeCache.TryGetValue(typeName, out var cached))
            return cached;

        if (!_typeCache.TryGetValue(typeName, out var type))
        {
            type = _testAssembly.GetType(typeName, throwOnError: false);
            _typeCache[typeName] = type;
        }

        var result = type?.GetInterfaces().Any(@interface => @interface.FullName == AsyncLifetimeInterfaceName) == true;
        _asyncLifetimeCache[typeName] = result;
        return result;
    }
}
