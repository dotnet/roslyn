// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using System.Threading;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.LanguageServer;
using Microsoft.CommonLanguageServerProtocol.Framework;
using Roslyn.LanguageServer.Protocol;

namespace Roslyn.Test.Utilities;

[ExportLspServiceFactory(typeof(TestLspLogger), ProtocolConstants.RoslynLspLanguagesContract, WellKnownLspServerKinds.Any), Shared]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal class TestLspLoggerFactory() : ILspServiceFactory
{
    public ILspService CreateILspService(LspServices lspServices, WellKnownLspServerKinds serverKind)
        => new TestLspLogger(lspServices.GetRequiredService<IClientLanguageServerManager>());

    protected class TestLspLogger(IClientLanguageServerManager clientLanguageServerManager) : ILspLogger, ILspService
    {
        public IDisposable? CreateContext(string context) => null;
        public IDisposable? CreateLanguageContext(string? context) => null;

        public void LogDebug(string message, params object[] @params) => Log(MessageType.Debug, message, @params);

        public void LogError(string message, params object[] @params) => Log(MessageType.Error, message, @params);

        public void LogException(Exception exception, string? message = null, params object[] @params)
            => Log(MessageType.Error, $"{message}{Environment.NewLine}{exception}", @params);

        public void LogInformation(string message, params object[] @params) => Log(MessageType.Info, message, @params);

        public void LogWarning(string message, params object[] @params) => Log(MessageType.Warning, message, @params);

        private void Log(MessageType level, string message, params object[] @params)
        {
            var _ = clientLanguageServerManager.SendNotificationAsync(Methods.WindowLogMessageName, new LogMessageParams()
            {
                Message = string.Format(message, @params),
                MessageType = level,
            }, CancellationToken.None);
        }
    }

}
