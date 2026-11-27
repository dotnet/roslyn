// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Composition;
using System.Text.Json.Serialization;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.LanguageServer.Handler;
using Microsoft.CodeAnalysis.LanguageServer.Logging;
using Microsoft.Extensions.Logging;

namespace Microsoft.CodeAnalysis.LanguageServer.LanguageServer.Handler.Logging;

[ExportCSharpVisualBasicStatelessLspService(typeof(UpdateLogLevelHandler)), Shared]
[Method(MethodName)]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal sealed class UpdateLogLevelHandler() : ILspServiceNotificationHandler<UpdateLogLevelParams>
{
    internal const string MethodName = "roslyn/updateLogLevel";

    public bool MutatesSolutionState => false;

    public bool RequiresLSPSolution => false;

    public Task HandleNotificationAsync(UpdateLogLevelParams request, RequestContext requestContext, CancellationToken cancellationToken)
    {
        var level = Enum.Parse<LogLevel>(request.LogLevelValue);
        var loggerFactory = requestContext.GetRequiredService<LspLoggerFactory>();
        loggerFactory.LogConfiguration.UpdateLogLevel(level);
        return Task.CompletedTask;
    }
}

/// <summary>
/// Request parameters for updating the log level in the server dynamically.
/// </summary>
/// <param name="LogLevelValue">the string value of the <see cref="LogLevel"/> enum</param>
internal sealed record class UpdateLogLevelParams([property: JsonPropertyName("logLevel")] string LogLevelValue);
