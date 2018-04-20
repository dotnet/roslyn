// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
