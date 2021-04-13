// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Experiments;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.VisualStudio.Shell;

namespace Microsoft.VisualStudio.LanguageServices.Implementation
{
    [ExportWorkspaceService(typeof(IWorkspaceContextService), ServiceLayer.Host), Shared]
    internal class VisualStudioWorkspaceContextService : IWorkspaceContextService
    {
        /// <summary>
        /// Roslyn LSP feature flag name, as defined in the PackageRegistraion.pkgdef
        /// by everything following '$RootKey$\FeatureFlags\' and '\' replaced by '.'
        /// </summary>
        public const string LspEditorFeatureFlagName = "Roslyn.LSP.Editor";

        // UI context defined by Live Share when connected as a guest in a Live Share session
        // https://devdiv.visualstudio.com/DevDiv/_git/Cascade?path=%2Fsrc%2FVS%2FContracts%2FGuidList.cs&version=GBmain&line=32&lineEnd=33&lineStartColumn=1&lineEndColumn=1&lineStyle=plain&_a=contents
        private static readonly Guid LiveShareGuestUIContextGuid = Guid.Parse("fd93f3eb-60da-49cd-af15-acda729e357e");

        private readonly Workspace _workspace;

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public VisualStudioWorkspaceContextService(VisualStudioWorkspace vsWorkspace)
        {
            _workspace = vsWorkspace;
        }

        public bool IsCloudEnvironmentClient()
        {
            var context = UIContext.FromUIContextGuid(VSConstants.UICONTEXT.CloudEnvironmentConnected_guid);
            return context.IsActive;
        }

        public bool IsInLspEditorContext()
        {
            var featureFlagService = _workspace.Services.GetRequiredService<IExperimentationService>();
            var isInLspContext = IsLiveShareGuest() || IsCloudEnvironmentClient() || featureFlagService.IsExperimentEnabled(LspEditorFeatureFlagName);
            return isInLspContext;
        }

        /// <summary>
        /// Checks if the VS instance is running as a Live Share guest session.
        /// </summary>
        private static bool IsLiveShareGuest()
        {
            var context = UIContext.FromUIContextGuid(LiveShareGuestUIContextGuid);
            return context.IsActive;
        }
    }
}
