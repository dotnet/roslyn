// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Extensions;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.Extensions.Logging;

namespace Microsoft.CodeAnalysis.LanguageServer.HostWorkspace;

[ExportWorkspaceService(typeof(IExtensionManager), ServiceLayer.Host), Shared]
internal class ExtensionManager : IExtensionManager
{
    private readonly ILogger _logger;

    [ImportingConstructor]
    [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    public ExtensionManager(ILoggerFactory loggerFactory)
    {
        _logger = loggerFactory.CreateLogger(nameof(ExtensionManager));
    }

    public bool CanHandleException(object provider, Exception exception) => true;

    public void HandleException(object provider, Exception exception)
    {
        _logger.Log(LogLevel.Error, exception, $"{provider.GetType().ToString()} threw an exception.");
    }

    public bool IsDisabled(object provider)
    {
        // We don't have an UI to allow disabling yet
        return false;
    }
}
