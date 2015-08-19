// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.Imaging.Interop;
using Microsoft.VisualStudio.ProjectSystem.Designers;
using Microsoft.VisualStudio.ProjectSystem.ProjectTree;
using Microsoft.VisualStudio.ProjectSystem.Utilities;
using System.ComponentModel.Composition;

namespace Microsoft.VisualStudio.ProjectSystem.VisualBasic.ProjectTree
{
    /// <summary>
    ///     A tree modifier that sets the icon for the project root.
    /// </summary>
    [Export(typeof(IProjectTreeModifier))]
    [AppliesTo(ProjectCapabilities.VB)]
    internal class VisualBasicProjectRootProjectTreeModifier : ProjectRootProjectTreeModifierBase
    {
        [ImportingConstructor]
        public VisualBasicProjectRootProjectTreeModifier()
        {
        }

        public override ImageMoniker ProjectRootIcon
        {
            get { return KnownMonikers.VBProjectNode; }
        }
    }
}
