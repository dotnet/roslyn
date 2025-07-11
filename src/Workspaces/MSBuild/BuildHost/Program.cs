// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.CommandLine;
using System.Globalization;
using System.IO.Pipes;
using System.Threading.Tasks;

namespace Microsoft.CodeAnalysis.MSBuild;

internal static class Program
{
    internal static async Task Main(string[] args)
    {
        var pipeOption = new Option<string>("--pipe") { Required = true };
        var propertyOption = new Option<string[]>("--property") { Arity = ArgumentArity.ZeroOrMore };
        var binaryLogOption = new Option<string?>("--binlog") { Required = false };
        var localeOption = new Option<string>("--locale") { Required = true };
        var command = new RootCommand { pipeOption, binaryLogOption, propertyOption, localeOption };
        var parsedArguments = command.Parse(args);
        var pipeName = parsedArguments.GetValue(pipeOption)!;
        var properties = parsedArguments.GetValue(propertyOption)!;
        var binaryLogPath = parsedArguments.GetValue(binaryLogOption);
        var locale = parsedArguments.GetValue(localeOption)!;

        var propertiesBuilder = ImmutableDictionary.CreateBuilder<string, string>();

        foreach (var property in properties)
        {
            var propertyParts = property.Split(['='], count: 2);
            propertiesBuilder.Add(propertyParts[0], propertyParts[1]);
        }

        var logger = new BuildHostLogger(Console.Error);

        try
        {
            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo(locale);
        }
        catch (CultureNotFoundException)
        {
            // We couldn't find the culture, log a warning and fallback to the OS configured value.
            logger.LogWarning($"Culture {locale} was not found, falling back to OS culture");
        }

        logger.LogInformation($"BuildHost Runtime Version: {System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription}");

        var pipeServer = NamedPipeUtil.CreateServer(pipeName, PipeDirection.InOut);
        await pipeServer.WaitForConnectionAsync().ConfigureAwait(false);

        var server = new RpcServer(pipeServer);

        var targetObject = server.AddTarget(new BuildHost(logger, propertiesBuilder.ToImmutable(), binaryLogPath, server));
        Contract.ThrowIfFalse(targetObject == 0, "The first object registered should have target 0, which is assumed by the client.");

        await server.RunAsync().ConfigureAwait(false);

        logger.LogInformation("RPC channel closed; process exiting.");
    }
}
