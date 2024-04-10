// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CommonLanguageServerProtocol.Framework;
using Microsoft.Extensions.Logging;

namespace Microsoft.CodeAnalysis.LanguageServer.Logging;

/// <summary>
/// Implements <see cref="AbstractLspLogger"/> by sending LSP log messages back to the client.
/// </summary>
internal sealed class LspServiceLogger : AbstractLspLogger, ILspService
{
    private readonly ILogger _hostLogger;

    public LspServiceLogger(ILogger hostLogger)
    {
        _hostLogger = hostLogger;
    }

    public override void LogDebug(string message, params object[] @params) => _hostLogger.LogDebug(message, @params);

    public override void LogEndContext(string message, params object[] @params) => _hostLogger.LogDebug($"[{DateTime.UtcNow:hh:mm:ss.fff}][End]{message}", @params);

    public override void LogError(string message, params object[] @params) => _hostLogger.LogError(message, @params);

    public override void LogException(Exception exception, string? message = null, params object[] @params) => _hostLogger.LogError(exception, message, @params);

    /// <summary>
    /// TODO - Switch this to call LogInformation once appropriate callers have been changed to LogDebug.
    /// </summary>
    public override void LogInformation(string message, params object[] @params) => _hostLogger.LogDebug(message, @params);

    public override void LogStartContext(string message, params object[] @params) => _hostLogger.LogDebug($"[{DateTime.UtcNow:hh:mm:ss.fff}][Start]{message}", @params);

    public override void LogWarning(string message, params object[] @params) => _hostLogger.LogWarning(message, @params);
}
