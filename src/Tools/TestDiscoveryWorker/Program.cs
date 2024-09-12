// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Threading;
using System.Threading.Channels;

using Xunit;
using Xunit.Abstractions;

int ExitFailure = 1;
int ExitSuccess = 0;

if (args.Length != 1)
{
    return ExitFailure;
}

try
{
    using var pipeClient = new AnonymousPipeClientStream(PipeDirection.In, args[0]);
    using var sr = new StreamReader(pipeClient);
    string? output;

    // Wait for 'sync message' from the server.
    do
    {
        output = await sr.ReadLineAsync().ConfigureAwait(false);
    }
    while (!(output?.StartsWith("ASSEMBLY", StringComparison.OrdinalIgnoreCase) == true));

    if ((output = await sr.ReadLineAsync().ConfigureAwait(false)) is not null)
    {
        var assemblyFileName = output;

#if NET6_0_OR_GREATER
        var resolver = new System.Runtime.Loader.AssemblyDependencyResolver(assemblyFileName);
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

        string testDescriptor = Path.GetFileName(assemblyFileName);
#if NET
        testDescriptor += " (.NET Core)";
#else
    testDescriptor += " (.NET Framework)";
#endif

        await Console.Out.WriteLineAsync($"Discovering tests in {testDescriptor}...").ConfigureAwait(false);

        using var xunit = new XunitFrontController(AppDomainSupport.IfAvailable, assemblyFileName, shadowCopy: false);
        var configuration = ConfigReader.Load(assemblyFileName, configFileName: null, warnings: null);
        var sink = new Sink();
        xunit.Find(includeSourceInformation: false,
                   messageSink: sink,
                   discoveryOptions: TestFrameworkOptions.ForDiscovery(configuration));

        var testsToWrite = new HashSet<string>();
        await foreach (var fullyQualifiedName in sink.GetTestCaseNamesAsync())
        {
            testsToWrite.Add(fullyQualifiedName);
        }

        if (sink.AnyWriteFailures)
        {
            await Console.Error.WriteLineAsync($"Channel failed to write for '{assemblyFileName}'").ConfigureAwait(false);
            return ExitFailure;
        }

#if NET6_0_OR_GREATER
        await Console.Out.WriteLineAsync($"Discovered {testsToWrite.Count} tests in {testDescriptor}").ConfigureAwait(false);
#else
        await Console.Out.WriteLineAsync($"Discovered {testsToWrite.Count} tests in {testDescriptor}").ConfigureAwait(false);
#endif

        var directory = Path.GetDirectoryName(assemblyFileName);
        using var fileStream = File.Create(Path.Combine(directory!, "testlist.json"));
        await JsonSerializer.SerializeAsync(fileStream, testsToWrite).ConfigureAwait(false);
        return ExitSuccess;
    }

    return ExitFailure;
}
catch (Exception ex)
{
    // Write the exception details to stderr so the host process can pick it up.
    await Console.Error.WriteLineAsync(ex.ToString()).ConfigureAwait(false);
    return 1;
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
