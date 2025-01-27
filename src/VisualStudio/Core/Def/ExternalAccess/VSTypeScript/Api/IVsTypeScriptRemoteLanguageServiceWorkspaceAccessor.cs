// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using Microsoft.CodeAnalysis;

namespace Microsoft.VisualStudio.LanguageServices.ExternalAccess.VSTypeScript.Api;

// Workaround for https://devdiv.visualstudio.com/DevDiv/_workitems/edit/1162267
internal interface IVsTypeScriptRemoteLanguageServiceWorkspaceAccessor
{
    Workspace RemoteLanguageServiceWorkspace { get; }
}
