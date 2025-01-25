// Licensed to the.NET Foundation under one or more agreements.
// The.NET Foundation licenses this file to you under the MIT license.
// See the License.txt file in the project root for more information.

using System.CommandLine;
using System.CommandLine.Invocation;
using Microsoft.RoslynTools.Authentication;

namespace Microsoft.RoslynTools.Commands;

using static CommonOptions;

internal class AuthenticateCommand
{
    private static readonly AuthenticateCommandDefaultHandler s_authenticateCommandHandler = new();

    internal static readonly Option ClearOption = new(["--clear", "-c"], "Clear any settings to defaults.");

    public static Symbol GetCommand()
    {
        var command = new Command("authenticate", "Stores the AzDO and GitHub tokens required for remote operations.")
        {
            ClearOption,
            VerbosityOption
        };
        command.Handler = s_authenticateCommandHandler;
        return command;
    }

    private class AuthenticateCommandDefaultHandler : ICommandHandler
    {
        public Task<int> InvokeAsync(InvocationContext context)
        {
            var logger = context.SetupLogging();

            var clearSettings = context.ParseResult.WasOptionUsed([.. ClearOption.Aliases]);

            return Authenticator.UpdateAsync(clearSettings, logger);
        }
    }
}

