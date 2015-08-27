// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.Imaging.Interop;
using Microsoft.VisualStudio.ProjectSystem.Designers;
using Microsoft.VisualStudio.ProjectSystem.Utilities;

namespace Microsoft.VisualStudio.ProjectSystem.CSharp.ProjectTree
{
    /// <summary>
    ///     A tree modifier that sets the icon for the project root.
    /// </summary>
    [Export(typeof(IProjectTreeModifier))]
    [AppliesTo(ProjectCapabilities.CSharp)]
    internal class CSharpProjectRootProjectTreeModifier : ProjectRootProjectTreeModifierBase
    {
        [ImportingConstructor]
        public CSharpProjectRootProjectTreeModifier()
        {
        }

        public override ImageMoniker ProjectRootIcon
        {
            get { return KnownMonikers.CSProjectNode; }
        }
    }
}
