// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Composition;
using Microsoft.CodeAnalysis.Extensions;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.Extensions.Logging;

namespace Microsoft.CodeAnalysis.LanguageServer.HostWorkspace;

[ExportWorkspaceService(typeof(IExtensionManager), ServiceLayer.Host), Shared]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal sealed class LanguageServerExtensionManager(ILoggerFactory loggerFactory) : AbstractExtensionManager
{
    private readonly ILogger _logger = loggerFactory.CreateLogger(nameof(LanguageServerExtensionManager));

    protected override void HandleNonCancellationException(object provider, Exception exception)
        => _logger.Log(LogLevel.Error, exception, $"{provider.GetType()} threw an exception.");
}
