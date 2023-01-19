// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.LanguageServer;

namespace Microsoft.CodeAnalysis.ExternalAccess.Razor
{
    [Export(typeof(RazorTestWorkspaceRegistrationService))]
    [Export(typeof(LspWorkspaceRegistrationService))]
    [Shared, PartNotDiscoverable]
    internal class RazorTestWorkspaceRegistrationService : LspWorkspaceRegistrationService
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public RazorTestWorkspaceRegistrationService()
        {
        }

        public override string GetHostWorkspaceKind()
        {
            return WorkspaceKind.Host;
        }

        // Method purposely doesn't override the base so any changes to the method
        // signature in the base class won't automatically break Razor.
        public new void Register(Workspace workspace)
        {
            base.Register(workspace);
        }
    }
}
