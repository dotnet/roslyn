// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using Microsoft.CodeAnalysis.Editor.EditorConfigSettings.Data;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;

namespace Microsoft.CodeAnalysis.Editor.EditorConfigSettings.DataProvider.Whitespace
{
    [ExportWorkspaceServiceFactory(typeof(IWorkspaceSettingsProviderFactory<WhitespaceSetting>)), Shared]
    internal class CommonWhitespaceSettingsWorkspaceServiceFactory : IWorkspaceServiceFactory
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public CommonWhitespaceSettingsWorkspaceServiceFactory() { }

        public IWorkspaceService CreateService(HostWorkspaceServices workspaceServices)
            => new CommonWhitespaceSettingsProviderFactory(workspaceServices.Workspace);
    }
}
