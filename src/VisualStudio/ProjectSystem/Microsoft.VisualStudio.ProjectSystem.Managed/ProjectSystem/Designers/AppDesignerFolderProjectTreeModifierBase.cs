// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Linq;
using Microsoft.VisualStudio.ProjectSystem.Designers.Imaging;
using Microsoft.VisualStudio.ProjectSystem.Utilities.Designers;

namespace Microsoft.VisualStudio.ProjectSystem.Designers
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

        private readonly IProjectFeatures _features;

        protected AppDesignerFolderProjectTreeModifierBase(IProjectImageProvider imageProvider, IProjectFeatures features)
            : base(imageProvider)
        {
            Requires.NotNull(features, nameof(features));

            _features = features;
        }

        public override bool IsSupported
        {
            get { return _features.SupportsProjectDesigner; }
        }

        public override ImmutableHashSet<string> DefaultCapabilities
        {
            get { return DefaultAppDesignerFolderCapabilities; }
        }

        public override string ImageKey
        {
            get {  return ProjectImageKey.AppDesignerFolder; }
        }

        protected override sealed IProjectTree FindCandidateSpecialItem(IProjectTree projectRoot)
        {
            string folderName = GetAppDesignerFolderName();

            IProjectTree candidate = projectRoot.Children.FirstOrDefault(n => StringComparers.Paths.Equals(n.Caption, folderName));
            if (candidate == null || !candidate.IsFolder || candidate.HasCapability(ProjectTreeCapabilities.AppDesignerFolder) || !candidate.IsIncludedInProject())
                return null; // Couldn't find a candidate or already have a AppDesigner folder

            return candidate;
        }

        protected virtual string GetAppDesignerFolderName()
        {
            // TODO: Read this from AppDesignerFolder MSBuild property 
            return null;
        }
    }
}
