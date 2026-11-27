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
[ExportCSharpVisualBasicLspServiceFactory(typeof(LspServiceLogger)), Shared]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal sealed class LspServiceLoggerFactory() : ILspServiceFactory
{
    public ILspService CreateILspService(LspServices lspServices, WellKnownLspServerKinds serverKind)
    {
        var logger = lspServices.GetRequiredService<ILoggerFactory>().CreateLogger(string.Empty);
        return new LspServiceLogger(logger);
    }

    private sealed class LspServiceLogger : ILspLogger, ILspService
    {
        private readonly ILogger _hostLogger;

        public LspServiceLogger(ILogger hostLogger)
        {
            _hostLogger = hostLogger;
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
}