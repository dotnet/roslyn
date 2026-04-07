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
using Xunit.Abstractions;

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

    if (outputFilePath is null)
    {
        outputFilePath = Path.Combine(Path.GetDirectoryName(assemblyFilePath)!, "testlist.json");
    }

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
#endif

    string assemblyFileName = Path.GetFileName(assemblyFilePath);
#if NET
    string tfm = "(.NET Core)";
#else
    string tfm = "(.NET Framework)";
#endif

    Console.Write($"Discovering tests in {tfm} {assemblyFileName} ... ");

    using var xunit = new XunitFrontController(AppDomainSupport.IfAvailable, assemblyFilePath, shadowCopy: false);
    var configuration = ConfigReader.Load(assemblyFileName, configFileName: null);
    var sink = new Sink();
    xunit.Find(includeSourceInformation: false,
                messageSink: sink,
                discoveryOptions: TestFrameworkOptions.ForDiscovery(configuration));

    var testsToWrite = new HashSet<string>();
    await foreach (var fullyQualifiedName in sink.GetTestCaseNamesAsync().ConfigureAwait(false))
    {
        testsToWrite.Add(fullyQualifiedName);
    }

    if (sink.AnyWriteFailures)
    {
        Console.WriteLine($"Channel failed to write for '{assemblyFileName}'");
        return ExitFailure;
    }

    Console.WriteLine($"{testsToWrite.Count} found");

    using var fileStream = new FileStream(outputFilePath, FileMode.OpenOrCreate, FileAccess.Write, FileShare.None);
    await JsonSerializer.SerializeAsync(fileStream, testsToWrite.OrderBy(x => x)).ConfigureAwait(false);
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

file class Sink : IMessageSink
{
    public bool AnyWriteFailures { get; private set; }

    public Sink()
    {
        _channel = Channel.CreateUnbounded<string>();
    }

    private readonly Channel<string> _channel;

    public async IAsyncEnumerable<string> GetTestCaseNamesAsync()
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
        if (message is ITestCaseDiscoveryMessage discoveryMessage)
        {
            OnTestDiscovered(discoveryMessage);
        }

        if (message is IDiscoveryCompleteMessage)
        {
            _channel.Writer.Complete();
        }

        return true;
    }

    private void OnTestDiscovered(ITestCaseDiscoveryMessage testCaseDiscovered)
    {
        var fullName = $"{testCaseDiscovered.TestCase.TestMethod.TestClass.Class.Name}.{testCaseDiscovered.TestCase.TestMethod.Method.Name}";
        // this shouldn't happen as our channel is unbounded but we are Paranoid Coding™️
        if (!_channel.Writer.TryWrite(fullName))
        {
            AnyWriteFailures = true;
        }
    }
}

