// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Logging;

namespace Microsoft.CodeAnalysis.LanguageServer;

internal sealed class HostLspLogger : ILspServiceLogger
{
    private readonly ILogger _logger;

    public HostLspLogger(ILogger logger)
    {
        _logger = logger;
    }

    public void LogEndContext(string message, params object[] @params) => _logger.LogTrace($"[{DateTime.UtcNow:hh:mm:ss.fff}][End]{message}", @params);

    public void LogError(string message, params object[] @params) => _logger.LogError(message, @params);

    public void LogException(Exception exception, string? message = null, params object[] @params) => _logger.LogError(exception, message, @params);

    public void LogInformation(string message, params object[] @params) => _logger.LogInformation(message, @params);

    public void LogStartContext(string message, params object[] @params) => _logger.LogTrace($"[{DateTime.UtcNow:hh:mm:ss.fff}][Start]{message}", @params);

    public void LogWarning(string message, params object[] @params) => _logger.LogWarning(message, @params);
}
