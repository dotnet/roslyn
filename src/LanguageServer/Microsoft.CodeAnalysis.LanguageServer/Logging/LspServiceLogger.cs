// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Composition;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.LanguageServer.Handler;
using Microsoft.CommonLanguageServerProtocol.Framework;
using Microsoft.Extensions.Logging;

namespace Microsoft.CodeAnalysis.LanguageServer.Logging;

/// <summary>
/// Implements <see cref="ILspLogger"/> by sending LSP log messages back to the client.
/// </summary>
[ExportCSharpVisualBasicLspService(typeof(LspServiceLogger)), Shared(LspServiceComposition.SharingBoundary)]
internal sealed class LspServiceLogger : ILspLogger, ILspService
{
    private readonly ILogger _hostLogger;

    [ImportingConstructor]
    [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    public LspServiceLogger(LspService<ILoggerFactory> loggerFactory)
    {
        _hostLogger = loggerFactory.Value.CreateLogger(string.Empty);
    }

    public IDisposable? CreateContext(string context) => _hostLogger.BeginScope(new LspLoggingScope(context, null));

    public IDisposable? CreateLanguageContext(string? language)
        => language is null or LanguageServerConstants.DefaultLanguageName or LanguageNames.CSharp
            ? null
            : _hostLogger.BeginScope(new LspLoggingScope(null, language));

    public void LogDebug(string message, params object[] @params) => _hostLogger.LogDebug(message, @params);

    public void LogError(string message, params object[] @params) => _hostLogger.LogError(message, @params);

    public void LogException(Exception exception, string? message = null, params object[] @params) => _hostLogger.LogError(exception, message, @params);

    public void LogInformation(string message, params object[] @params) => _hostLogger.LogInformation(message, @params);

    public void LogWarning(string message, params object[] @params) => _hostLogger.LogWarning(message, @params);
}
