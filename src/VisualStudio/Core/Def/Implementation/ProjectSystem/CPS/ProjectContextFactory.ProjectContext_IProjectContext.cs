// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem
{
    internal partial class ProjectContextFactory
    {
        private sealed partial class ProjectContext : AbstractProject, IProjectContext
        {
            #region Options
            void IProjectContext.SetCommandLineArguments(CommandLineArguments commandLineArguments)
            {
                base.SetCommandLineArguments(commandLineArguments);
            }
            #endregion

            #region References
            public void AddMetadataReference(string referencePath, MetadataReferenceProperties properties)
            {
                referencePath = FileUtilities.NormalizeAbsolutePath(referencePath);
                AddMetadataReferenceAndTryConvertingToProjectReferenceIfPossible(referencePath, properties);
            }

            void IProjectContext.RemoveMetadataReference(string referencePath)
            {
                referencePath = FileUtilities.NormalizeAbsolutePath(referencePath);
                base.RemoveMetadataReference(referencePath);
            }

            public void AddProjectReference(ProjectId projectId, MetadataReferenceProperties properties)
            {
                var referencedProject = this.ProjectTracker.GetProject(projectId);
                var binPathOpt = referencedProject.TryGetBinOutputPath();
                if (!string.IsNullOrEmpty(binPathOpt))
                {
                    AddMetadataReferenceAndTryConvertingToProjectReferenceIfPossible(binPathOpt, properties);
                }
            }

            public void RemoveProjectReference(ProjectId projectId)
            {
                foreach (var projectReference in GetCurrentProjectReferences())
                {
                    if (projectReference.ProjectId.Equals(projectId))
                    {
                        RemoveProjectReference(projectReference);
                        return;
                    }
                }
            }

            #endregion

            #region Source files
            public void AddSourceFile(string filePath, bool isInCurrentContext = true, IEnumerable<string> folderNames = null, SourceCodeKind sourceCodeKind = SourceCodeKind.Regular)
            {
                AddFile(filePath, sourceCodeKind, getIsCurrentContext: _ => isInCurrentContext, folderNames: folderNames.ToImmutableArrayOrEmpty());
            }

            public void RemoveSourceFile(string filePath)
            {
                RemoveFile(filePath);
            }

            #endregion

            #region Project property changes
            public void SetProjectGuid(Guid guid)
            {
                Guid = guid;
            }

            public void SetProjectTypeGuid(string projectTypeGuid)
            {
                ProjectType = projectTypeGuid;
            }

            public void SetProjectDisplayName(string projectDisplayName)
            {
                UpdateProjectDisplayName(projectDisplayName);
            }

            public void SetProjectFilePath(string projectFilePath)
            {
                UpdateProjectFilePath(projectFilePath);
            }

            #endregion
        }
    }
}
