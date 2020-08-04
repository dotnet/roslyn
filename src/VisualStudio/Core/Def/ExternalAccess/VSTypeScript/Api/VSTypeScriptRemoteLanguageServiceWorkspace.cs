// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Host;

namespace Microsoft.VisualStudio.LanguageServices.ExternalAccess.VSTypeScript.Api
{
    /// <summary>
    /// Used by TypeScript to acquire the RemoteLanguageServiceWorkspace.
    /// </summary>
    internal abstract class VSTypeScriptRemoteLanguageServiceWorkspace : Workspace
    {
        protected VSTypeScriptRemoteLanguageServiceWorkspace(HostServices host, string workspaceKind) : base(host, workspaceKind)
        {
        }
    }
}
