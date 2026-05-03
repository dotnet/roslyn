// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Composition;
using System.Text.Json.Serialization;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.LanguageServer.Handler;
using Microsoft.Extensions.Logging;

namespace Microsoft.CodeAnalysis.LanguageServer.LanguageServer.Handler.Logging;

[ExportCSharpVisualBasicStatelessLspService(typeof(UpdateLogLevelHandler)), Shared]
[Method(MethodName)]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal sealed class UpdateLogLevelHandler(ServerConfiguration serverConfiguration) : ILspServiceNotificationHandler<UpdateLogLevelParams>
{
    private const string MethodName = "roslyn/updateLogLevel";

    public bool MutatesSolutionState => false;

    public bool RequiresLSPSolution => false;

    public async Task HandleNotificationAsync(UpdateLogLevelParams request, RequestContext requestContext, CancellationToken cancellationToken)
    {
        var level = Enum.Parse<LogLevel>(request.LogLevelValue);
        serverConfiguration.LogConfiguration.UpdateLogLevel(level);
    }
}

/// <summary>
/// Request parameters for updating the log level in the server dynamically.
/// </summary>
/// <param name="LogLevelValue">the string value of the <see cref="LogLevel"/> enum</param>
internal sealed record class UpdateLogLevelParams([property: JsonPropertyName("logLevel")] string LogLevelValue);
