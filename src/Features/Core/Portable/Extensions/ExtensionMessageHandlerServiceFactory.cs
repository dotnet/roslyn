// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#if NET

using System;
using System.Collections.Immutable;
using System.Composition;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;

namespace Microsoft.CodeAnalysis.Extensions;

[ExportWorkspaceServiceFactory(typeof(IExtensionMessageHandlerService)), Shared]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal sealed partial class ExtensionMessageHandlerServiceFactory(IExtensionMessageHandlerFactory customMessageHandlerFactory)
    : IWorkspaceServiceFactory
{
    public IWorkspaceService CreateService(HostWorkspaceServices workspaceServices)
        => new ExtensionMessageHandlerService(
            workspaceServices.SolutionServices,
            customMessageHandlerFactory);
}
#endif
