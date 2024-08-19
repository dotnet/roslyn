// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the License.txt file in the project root for more information.

using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.RoslynTools.DartTest;

namespace Microsoft.RoslynTools.Commands;

internal static class DartTestCommand
{
    private static readonly DartTestCommandDefaultHandler s_dartTestCommandHandler = new();
    private static readonly Option<int> prNumber = new(["--prNumber", "-pr"], "PR number") { IsRequired = true };
    private static readonly Option<string> sha = new(["--sha", "-s"], "Relevant SHA") { IsRequired = false };

    public static Symbol GetCommand()
    {
        var command = new Command("dart-test",
            @"Runs the dartlab pipeline for a given PR number and SHA.
It works by cloning the PR from the dotnet/roslyn repo into the internal mirror and then running the dartlab pipeline from it.")
        {
            prNumber,
            sha,
            CommonOptions.GitHubTokenOption,
            CommonOptions.DevDivAzDOTokenOption,
            CommonOptions.DncEngAzDOTokenOption,
        };

        command.Handler = s_dartTestCommandHandler;
        return command;
    }

    private class DartTestCommandDefaultHandler : ICommandHandler
    {
        public async Task<int> InvokeAsync(InvocationContext context)
        {
            var logger = context.SetupLogging();
            var settings = context.ParseResult.LoadSettings(logger);

            if (string.IsNullOrEmpty(settings.GitHubToken) ||
                string.IsNullOrEmpty(settings.DevDivAzureDevOpsToken) ||
                string.IsNullOrEmpty(settings.DncEngAzureDevOpsToken))
            {
                logger.LogError("Missing authentication token.");
                return -1;
            }

            var pr = context.ParseResult.GetValueForOption(prNumber);
            var shaFromPR = context.ParseResult.GetValueForOption(sha);

            if (shaFromPR is null)
            {
                logger.LogInformation("Using most recent SHA");
            }

            using var remoteConnections = new RemoteConnections(settings);

            return await DartTest.DartTest.RunDartPipeline(
                remoteConnections,
                logger,
                pr,
                shaFromPR).ConfigureAwait(false);
        }
    }
}
