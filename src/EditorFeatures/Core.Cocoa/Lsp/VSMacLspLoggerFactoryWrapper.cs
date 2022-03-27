// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.LanguageServer;
using Microsoft.CodeAnalysis.LanguageServer.ExternalAccess.VSMac.API;
using StreamJsonRpc;

namespace Microsoft.CodeAnalysis.EditorFeatures.Cocoa.Lsp;

/// <summary>
/// Wraps the external access <see cref="IVSMacLspLoggerFactory"/> and exports it
/// as an <see cref="ILspLoggerFactory"/> for inclusion in the vsmac composition.
/// </summary>
[Export(typeof(ILspLoggerFactory)), Shared]
internal class VSMacLspLoggerFactoryWrapper : ILspLoggerFactory
{
    private readonly IVSMacLspLoggerFactory _loggerFactory;

    [ImportingConstructor]
    [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    public VSMacLspLoggerFactoryWrapper(IVSMacLspLoggerFactory loggerFactory)
    {
        _loggerFactory = loggerFactory;
    }

    public async Task<ILspLogger> CreateLoggerAsync(string serverTypeName, string? clientName, JsonRpc jsonRpc, CancellationToken cancellationToken)
    {
        var vsMacLogger = await _loggerFactory.CreateLoggerAsync(serverTypeName, clientName, jsonRpc, cancellationToken).ConfigureAwait(false);
        return new VSMacLspLoggerWrapper(vsMacLogger);
    }
}

internal class VSMacLspLoggerWrapper : ILspLogger
{
    private readonly IVSMacLspLogger _logger;

    public VSMacLspLoggerWrapper(IVSMacLspLogger logger)
    {
        _logger = logger;
    }

    public void TraceError(string message) => _logger.TraceError(message);

    public void TraceException(Exception exception) => _logger.TraceException(exception);

    public void TraceInformation(string message) => _logger.TraceInformation(message);

    public void TraceStart(string message) => _logger.TraceStart(message);

    public void TraceStop(string message) => _logger.TraceStop(message);

    public void TraceWarning(string message) => _logger.TraceWarning(message);
}
