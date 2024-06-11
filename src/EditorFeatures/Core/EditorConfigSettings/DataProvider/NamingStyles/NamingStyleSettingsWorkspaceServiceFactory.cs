// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using Microsoft.CodeAnalysis.Editor.EditorConfigSettings.Data;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis.Editor.EditorConfigSettings.DataProvider.NamingStyles
{
    [ExportWorkspaceServiceFactory(typeof(IWorkspaceSettingsProviderFactory<NamingStyleSetting>)), Shared]
    [method: ImportingConstructor]
    [method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    internal sealed class NamingStyleSettingsWorkspaceServiceFactory(IGlobalOptionService globalOptions) : IWorkspaceServiceFactory
    {
        private readonly IGlobalOptionService _globalOptions = globalOptions;

        public IWorkspaceService CreateService(HostWorkspaceServices workspaceServices)
            => new NamingStyleSettingsProviderFactory(workspaceServices.Workspace, _globalOptions);
    }
}
