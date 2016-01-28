// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.ComponentModel.Composition;
using Microsoft.VisualStudio.ProjectSystem.Utilities;

namespace Microsoft.VisualStudio.ProjectSystem
{
    /// <summary>
    ///     Provides the Visual Basic implementation of <see cref="IItemTypeGuidProvider"/> and <see cref="IAddItemTemplatesGuidProvider"/>.
    /// </summary>
    //[Export(typeof(IItemTypeGuidProvider))]
    //[Export(typeof(IAddItemTemplatesGuidProvider))]
    [AppliesTo(ProjectCapabilities.VB)]
    internal class VisualBasicProjectGuidProvider : IItemTypeGuidProvider, IAddItemTemplatesGuidProvider
    {
        private static readonly Guid s_visualBasicProjectType = new Guid("{F184B08F-C81C-45F6-A57F-5ABD9991F28F}");

        [ImportingConstructor]
        public VisualBasicProjectGuidProvider(UnconfiguredProject unconfiguredProject)
        {
            Requires.NotNull(unconfiguredProject, nameof(unconfiguredProject));
        }

        public Guid ProjectTypeGuid
        {
            get { return s_visualBasicProjectType; }
        }

        public Guid AddItemTemplatesGuid
        {
            get { return s_visualBasicProjectType; }
        }
    }
}
