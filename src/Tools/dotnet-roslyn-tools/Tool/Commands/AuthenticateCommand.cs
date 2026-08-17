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

    internal static readonly Option<bool> ClearOption = new("--clear", "-c")
    {
        Description = "Clear any settings to defaults."
    };

    public static Command GetCommand()
    {
        var command = new Command("authenticate", "Stores the AzDO and GitHub tokens required for remote operations.")
        {
            ClearOption,
            VerbosityOption
        };
        command.Action = s_authenticateCommandHandler;
        return command;
    }

    private class AuthenticateCommandDefaultHandler : AsynchronousCommandLineAction
    {
        public override async Task<int> InvokeAsync(ParseResult parseResult, CancellationToken cancellationToken)
        {
            var logger = parseResult.SetupLogging();
            var clearSettings = parseResult.GetValue(ClearOption);

            return await Authenticator.UpdateAsync(clearSettings, logger);
        }
    }
}

