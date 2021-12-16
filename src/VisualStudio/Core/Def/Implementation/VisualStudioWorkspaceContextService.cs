// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.VisualStudio.Shell;

namespace Microsoft.VisualStudio.LanguageServices.Implementation
{
    [ExportWorkspaceService(typeof(IWorkspaceContextService), ServiceLayer.Host), Shared]
    internal class VisualStudioWorkspaceContextService : IWorkspaceContextService
    {
        // UI context defined by Live Share when connected as a guest in a Live Share session
        // https://devdiv.visualstudio.com/DevDiv/_git/Cascade?path=%2Fsrc%2FVS%2FContracts%2FGuidList.cs&version=GBmaster&line=32&lineEnd=33&lineStartColumn=1&lineEndColumn=1&lineStyle=plain&_a=contents
        private static readonly Guid LiveShareGuestUIContextGuid = Guid.Parse("fd93f3eb-60da-49cd-af15-acda729e357e");

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public VisualStudioWorkspaceContextService()
        {
        }

        public bool IsCloudEnvironmentClient()
        {
            var context = UIContext.FromUIContextGuid(VSConstants.UICONTEXT.CloudEnvironmentConnected_guid);
            return context.IsActive;
        }

        public bool IsInLspEditorContext()
        {
            return IsLiveShareGuest() || IsCloudEnvironmentClient();
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
