// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.Host;
using Microsoft.CommonLanguageServerProtocol.Framework;

namespace Microsoft.CodeAnalysis.LanguageServer;

internal interface ILspMiscellaneousFilesWorkspaceProviderFactory : ILspService
{
    ILspMiscellaneousFilesWorkspaceProvider CreateLspMiscellaneousFilesWorkspaceProvider(ILspServices lspServices, HostServices hostServices);
}
