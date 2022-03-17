// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Threading;
using Microsoft.CodeAnalysis.Internal.Log;

namespace Microsoft.CodeAnalysis.LanguageServer.ExternalAccess.VSCode.API;

/// <summary>
/// Allows VSCode to implement a telemetry logger for <see cref="ILogger"/> events.
/// </summary>
internal abstract class VSCodeTelemetryLogger : ILogger
{
    bool ILogger.IsEnabled(FunctionId functionId)
    {
        return IsEnabled(functionId.Convert());
    }

    void ILogger.Log(FunctionId functionId, LogMessage logMessage)
    {
        Log(functionId.Convert(), GetProperties(logMessage), CancellationToken.None);
    }

    void ILogger.LogBlockStart(FunctionId functionId, LogMessage logMessage, int uniquePairId, CancellationToken cancellationToken)
    {
        LogBlockStart(functionId.Convert(), GetProperties(logMessage), cancellationToken);
    }

    void ILogger.LogBlockEnd(FunctionId functionId, LogMessage logMessage, int uniquePairId, int delta, CancellationToken cancellationToken)
    {
        LogBlockEnd(functionId.Convert(), GetProperties(logMessage), cancellationToken);
    }

    public void Register()
    {
        Logger.SetLogger(this);
    }

    public abstract bool IsEnabled(string functionId);

    public abstract void Log(string functionId, IEnumerable<KeyValuePair<string, object?>>? properties, CancellationToken cancellationToken);

    public abstract void LogBlockStart(string functionId, IEnumerable<KeyValuePair<string, object?>>? properties, CancellationToken cancellationToken);

    public abstract void LogBlockEnd(string functionId, IEnumerable<KeyValuePair<string, object?>>? properties, CancellationToken cancellationToken);

    private static IEnumerable<KeyValuePair<string, object?>>? GetProperties(LogMessage logMessage)
    {
        return logMessage is KeyValueLogMessage kvMessage ? kvMessage.Properties : null;
    }
}
