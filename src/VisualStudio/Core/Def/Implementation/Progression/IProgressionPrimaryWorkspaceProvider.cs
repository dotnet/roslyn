// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.ComponentModel.Composition;
using Microsoft.CodeAnalysis;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.Progression
{
    using Workspace = Microsoft.CodeAnalysis.Workspace;

    // Workaround since ETA doesn't export VisualStudioWorkspace
    internal interface IProgressionPrimaryWorkspaceProvider
    {
        Workspace PrimaryWorkspace { get; }
    }

    [Export(typeof(IProgressionPrimaryWorkspaceProvider))]
    internal class VisualStudioProvider : IProgressionPrimaryWorkspaceProvider
    {
        private readonly VisualStudioWorkspace _workspace;

        [ImportingConstructor]
        public VisualStudioProvider(VisualStudioWorkspace workspace)
        {
            _workspace = workspace;
        }

        public Workspace PrimaryWorkspace
        {
            get
            {
                return _workspace;
            }
        }
    }
}
