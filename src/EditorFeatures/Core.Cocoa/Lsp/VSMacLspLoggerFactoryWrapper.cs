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
/// as an <see cref="ILspServiceLoggerFactory"/> for inclusion in the vsmac composition.
/// </summary>
[Export(typeof(ILspServiceLoggerFactory)), Shared]
internal class VSMacLspLoggerFactoryWrapper : ILspServiceLoggerFactory
{
    private readonly IVSMacLspLoggerFactory _loggerFactory;

    [ImportingConstructor]
    [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    public VSMacLspLoggerFactoryWrapper(IVSMacLspLoggerFactory loggerFactory)
    {
        _loggerFactory = loggerFactory;
    }

    public async Task<ILspServiceLogger> CreateLoggerAsync(string serverTypeName, JsonRpc jsonRpc, CancellationToken cancellationToken)
    {
        var vsMacLogger = await _loggerFactory.CreateLoggerAsync(serverTypeName, jsonRpc, cancellationToken).ConfigureAwait(false);
        return new VSMacLspLoggerWrapper(vsMacLogger);
    }
}

internal class VSMacLspLoggerWrapper : ILspServiceLogger
{
    private readonly IVSMacLspLogger _logger;

    public VSMacLspLoggerWrapper(IVSMacLspLogger logger)
    {
        _logger = logger;
    }

    public void LogError(string message, params object[] @params)
    {
        _logger.TraceError(message);
    }

    public void LogException(Exception exception, string? message = null, params object[] @params)
    {
        _logger.TraceException(exception);
    }

    public void LogInformation(string message, params object[] @params)
    {
        _logger.TraceInformation(message);
    }

    public void LogStartContext(string message, params object[] @params)
    {
        _logger.TraceStart(message);
    }

    public void LogEndContext(string message, params object[] @params)
    {
        _logger.TraceStop(message);
    }

    public void LogWarning(string message, params object[] @params)
    {
        _logger.TraceWarning(message);
    }
}
