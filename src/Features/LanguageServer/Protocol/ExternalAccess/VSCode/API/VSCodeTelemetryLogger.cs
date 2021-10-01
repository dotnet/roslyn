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
    private readonly Dictionary<FunctionId, string> _functionIdToString = new Dictionary<FunctionId, string>();

    private string GetFunctionIdAsString(FunctionId functionId)
    {
        if (_functionIdToString.ContainsKey(functionId))
        {
            return _functionIdToString[functionId];
        }
        else
        {
            var functionIdAsString = functionId.ToString();
            _functionIdToString.Add(functionId, functionIdAsString);
            return functionIdAsString;
        }
    }

    bool ILogger.IsEnabled(FunctionId functionId)
    {
        return IsEnabled(GetFunctionIdAsString(functionId));
    }

    void ILogger.Log(FunctionId functionId, LogMessage logMessage)
    {
        Log(GetFunctionIdAsString(functionId), GetProperties(logMessage), CancellationToken.None);
    }

    void ILogger.LogBlockStart(FunctionId functionId, LogMessage logMessage, int uniquePairId, CancellationToken cancellationToken)
    {
        LogBlockStart(GetFunctionIdAsString(functionId), GetProperties(logMessage), cancellationToken);
    }

    void ILogger.LogBlockEnd(FunctionId functionId, LogMessage logMessage, int uniquePairId, int delta, CancellationToken cancellationToken)
    {
        LogBlockEnd(GetFunctionIdAsString(functionId), GetProperties(logMessage), cancellationToken);
    }

    public void Register()
    {
        Logger.SetLogger(this);
    }

    public abstract bool IsEnabled(string functionId);

    public abstract void Log(string functionId, IEnumerable<KeyValuePair<string, object>>? properties, CancellationToken cancellationToken);

    public abstract void LogBlockStart(string functionId, IEnumerable<KeyValuePair<string, object>>? properties, CancellationToken cancellationToken);

    public abstract void LogBlockEnd(string functionId, IEnumerable<KeyValuePair<string, object>>? properties, CancellationToken cancellationToken);

    private static IEnumerable<KeyValuePair<string, object>>? GetProperties(LogMessage logMessage)
    {
        return logMessage is KeyValueLogMessage kvMessage ? kvMessage.Properties : null;
    }
}
