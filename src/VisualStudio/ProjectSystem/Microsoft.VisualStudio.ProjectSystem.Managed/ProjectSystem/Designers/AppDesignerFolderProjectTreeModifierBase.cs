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

        private readonly IUnconfiguredProjectCommonServices _projectServices;
        private readonly IProjectDesignerService _designerService;

        protected AppDesignerFolderProjectTreeModifierBase(IProjectImageProvider imageProvider, IUnconfiguredProjectCommonServices projectServices, IProjectDesignerService designerService)
            : base(imageProvider)
        {
            Requires.NotNull(projectServices, nameof(projectServices));
            Requires.NotNull(designerService, nameof(designerService));

            _projectServices = projectServices;
            _designerService = designerService;
        }

        public override bool IsSupported
        {
            get { return _designerService.SupportsProjectDesigner; }
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
            // Returns the <AppDesignerFolder> from the project file
            return _projectServices.ThreadingPolicy.ExecuteSynchronously(async () => {

                var generalProperties = await _projectServices.ActiveConfiguredProjectProperties.GetConfigurationGeneralPropertiesAsync()
                                                                                                .ConfigureAwait(false);

                return (string)await generalProperties.AppDesignerFolder.GetValueAsync()
                                                                        .ConfigureAwait(false);
            });
        }
    }
}
