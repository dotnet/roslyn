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
using System.Threading.Channels;

using Xunit;
using Xunit.Abstractions;

if (args.Length <= 0)
{
    return;
}

using var pipeClient = new AnonymousPipeClientStream(PipeDirection.In, args[0]);
using var sr = new StreamReader(pipeClient);
// Display the read text to the console
string? output;

// Wait for 'sync message' from the server.
do
{
    output = sr.ReadLine();
}
while (!(output?.StartsWith("ASSEMBLY") == true));

if ((output = sr.ReadLine()) is not null)
{
    var assemblyFileName = output;

#if NET6_0_OR_GREATER   
    var resolver = new System.Runtime.Loader.AssemblyDependencyResolver(assemblyFileName);
    System.Runtime.Loader.AssemblyLoadContext.Default.Resolving += (context, assemblyName) =>
    {
        var assemblyPath = resolver.ResolveAssemblyToPath(assemblyName);
        if (assemblyPath != null)
        {
            return Assembly.LoadFrom(assemblyPath);
        }

        return null;
    };
#endif


    using var xunit = new XunitFrontController(AppDomainSupport.IfAvailable, assemblyFileName, shadowCopy: false);
    var configuration = ConfigReader.Load(assemblyFileName);
    var sink = new Sink();
    xunit.Find(includeSourceInformation: false, messageSink: sink,
                discoveryOptions: TestFrameworkOptions.ForDiscovery(configuration));

    var builder = ImmutableArray.CreateBuilder<string>();
    await foreach (var fullyQualifiedName in sink.GetTestCaseNamesAsync())
    {
        builder.Add(fullyQualifiedName);
    }

    var testsToWrite = builder.Distinct().ToArray();
#if NET6_0_OR_GREATER
    Console.WriteLine($"Discovered {testsToWrite.Length} tests in {Path.GetFileName(assemblyFileName)} (.NET Core)");
#else
    Console.WriteLine($"Discovered {testsToWrite.Length} tests in {Path.GetFileName(assemblyFileName)} (.NET Framework)");
#endif

    var directory = Path.GetDirectoryName(assemblyFileName);
    using var fileStream = File.Create(Path.Combine(directory!, "testlist.json"));
    JsonSerializer.Serialize(fileStream, testsToWrite);
}

internal class Sink : IMessageSink
{
    public Sink()
    {
        _channel = Channel.CreateUnbounded<string>();
    }

    private readonly Channel<string> _channel;

    public async IAsyncEnumerable<string> GetTestCaseNamesAsync()
    {
        while (await _channel.Reader.WaitToReadAsync(default).ConfigureAwait(false))
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
        _ = _channel.Writer.TryWrite(fullName);
    }
}
