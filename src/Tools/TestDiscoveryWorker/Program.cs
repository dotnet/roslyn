// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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

    assemblyFilePath = Path.GetFullPath(assemblyFilePath);

    outputFilePath = outputFilePath is null
        ? Path.Combine(Path.GetDirectoryName(assemblyFilePath)!, "testlist.json")
        : Path.GetFullPath(outputFilePath);

    string assemblyFileName = Path.GetFileName(assemblyFilePath);
#if NET
    string tfm = "(.NET Core)";
#else
    string tfm = "(.NET Framework)";
#endif

    Console.Write($"Discovering tests in {tfm} {assemblyFileName} ... ");

    var assemblyMetadata = AssemblyUtility.GetAssemblyMetadata(assemblyFilePath)
        ?? throw new InvalidOperationException($"Could not determine the xUnit test framework used by '{assemblyFilePath}'.");
    var projectAssembly = new XunitProjectAssembly(new XunitProject(), assemblyFilePath, assemblyMetadata);

    var controller = XunitFrontController.Create(projectAssembly)
        ?? throw new InvalidOperationException($"Could not create a test framework front controller for '{assemblyFilePath}'.");
    await using var controllerDisposer = controller.ConfigureAwait(false);
    var sink = new Sink();
    var discoveryOptions = TestFrameworkOptions.ForDiscovery(projectAssembly.Configuration);
    controller.Find(sink, new FrontControllerFindSettings(discoveryOptions, projectAssembly.Configuration?.Filters ?? new XunitFilters()));

    var testsToWrite = new HashSet<string>();
    await foreach (var fullyQualifiedName in sink.GetTestCaseInfosAsync().ConfigureAwait(false))
        testsToWrite.Add(fullyQualifiedName);

    if (sink.AnyWriteFailures)
    {
        Console.WriteLine($"Channel failed to write for '{assemblyFileName}'");
        return ExitFailure;
    }

    Console.WriteLine($"{testsToWrite.Count} found");

    var testInfos = testsToWrite
        .OrderBy(x => x)
        .Select(x => new TestInfo(x))
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

    public TestInfo() { }

    public TestInfo(string methodName)
        => MethodName = methodName;
}

file class Sink : IMessageSink
{
    public bool AnyWriteFailures { get; private set; }

    public Sink()
    {
        _channel = Channel.CreateUnbounded<string>();
    }

    private readonly Channel<string> _channel;

    public async IAsyncEnumerable<string> GetTestCaseInfosAsync()
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

        // this shouldn't happen as our channel is unbounded but we are Paranoid Coding™️
        if (!_channel.Writer.TryWrite(fullName))
        {
            AnyWriteFailures = true;
        }
    }
}
