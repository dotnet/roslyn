// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.ComponentModel.Composition;
using Microsoft.VisualStudio.ProjectSystem.Designers.Imaging;
using Microsoft.VisualStudio.ProjectSystem.Utilities;

namespace Microsoft.VisualStudio.ProjectSystem.Designers
{
    /// <summary>
    ///     A tree modifier that turns "Properties" folder into a special folder.
    /// </summary>
    [Export(typeof(IProjectTreeModifier))]
    [AppliesTo(ProjectCapabilities.CSharp)]
    internal class PropertiesFolderProjectTreeModifier : AbstractAppDesignerFolderProjectTreeModifier
    {
        [ImportingConstructor]
        public PropertiesFolderProjectTreeModifier([Import(typeof(ProjectImageProviderAggregator))]IProjectImageProvider imageProvider, IUnconfiguredProjectCommonServices projectServices, IProjectDesignerService designerService)
            : base(imageProvider, projectServices, designerService)
        {
        }
        
        public override bool IsExpandableByDefault
        {
            get { return true; }
        }

        protected override string GetAppDesignerFolderName()
        {
            string folderName = base.GetAppDesignerFolderName();
            if (!string.IsNullOrEmpty(folderName))
                return folderName;

            return "Properties";        // Not localized
        }
    }
}
