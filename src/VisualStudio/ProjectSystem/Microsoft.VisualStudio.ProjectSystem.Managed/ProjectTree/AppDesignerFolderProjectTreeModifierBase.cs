// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Linq;
using Microsoft.VisualStudio.ProjectSystem.Designers;
using Microsoft.VisualStudio.ProjectSystem.Utilities.Designers;
using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.Imaging.Interop;
using System.Collections.Immutable;

namespace Microsoft.VisualStudio.ProjectSystem.ProjectTree
{
    /// <summary>
    ///     Provides the base class for tree modifiers that handle the AppDesigner folder, called "Properties" in C# and "My Project" in Visual Basic.
    /// </summary>
    internal abstract class AppDesignerFolderProjectTreeModifierBase : SpecialItemProjectTreeModifierBase
    {
        /// <summary>
        /// A common set of project tree capabilities with the case-insensitive comparer.
        /// </summary>
        private static readonly ImmutableHashSet<string> DefaultAppDesignerFolderCapabilities =
                    ProjectTreeCapabilities.EmptyCapabilities.Add(ProjectTreeCapabilities.AppDesignerFolder)
                                                             .Add(ProjectTreeCapabilities.BubbleUp);

        protected AppDesignerFolderProjectTreeModifierBase()
        {
        }

        public override ImmutableHashSet<string> DefaultCapabilities
        {
            get { return DefaultAppDesignerFolderCapabilities; }
        }

        public override ImageMoniker Icon
        {
            get { return KnownMonikers.Property; }
        }

        protected override sealed IProjectTree FindCandidateSpecialItem(IProjectTree projectRoot)
        {
            string folderName = GetAppDesignerFolderName();

            IProjectTree folder = projectRoot.Children.FirstOrDefault(n => StringComparers.Paths.Equals(n.Caption, folderName) && n.IsFolder);
            if (folder == null || folder.HasCapability(ProjectTreeCapabilities.AppDesignerFolder))
                return null; // Couldn't find a candidate or already have a AppDesigner folder

            return folder;
        }

        protected virtual string GetAppDesignerFolderName()
        {
            // TODO: Read this from AppDesignerFolder MSBuild property 
            return null;
        }
    }
}
