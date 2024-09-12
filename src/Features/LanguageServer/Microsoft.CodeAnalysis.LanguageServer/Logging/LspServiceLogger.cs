// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Logging;

namespace Microsoft.CodeAnalysis.LanguageServer.Logging;

/// <summary>
/// Implements <see cref="ILspServiceLogger"/> by sending LSP log messages back to the client.
/// </summary>
internal sealed class LspServiceLogger : ILspServiceLogger
{
    private readonly ILogger _hostLogger;

    public LspServiceLogger(ILogger hostLogger)
    {
        _hostLogger = hostLogger;
    }

    public void LogEndContext(string message, params object[] @params) => _hostLogger.LogDebug($"[{DateTime.UtcNow:hh:mm:ss.fff}][End]{message}", @params);

    public void LogError(string message, params object[] @params) => _hostLogger.LogError(message, @params);

    public void LogException(Exception exception, string? message = null, params object[] @params) => _hostLogger.LogError(exception, message, @params);

    /// <summary>
    /// TODO - This should call LogInformation, however we need to introduce a LogDebug call in clasp first.
    /// </summary>
    public void LogInformation(string message, params object[] @params) => _hostLogger.LogDebug(message, @params);

    public void LogStartContext(string message, params object[] @params) => _hostLogger.LogDebug($"[{DateTime.UtcNow:hh:mm:ss.fff}][Start]{message}", @params);

    public void LogWarning(string message, params object[] @params) => _hostLogger.LogWarning(message, @params);
}
