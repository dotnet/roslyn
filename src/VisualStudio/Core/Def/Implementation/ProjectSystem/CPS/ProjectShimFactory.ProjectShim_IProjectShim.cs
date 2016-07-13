// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.Text;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem
{
    internal partial class ProjectShimFactory
    {
        private sealed partial class ProjectShim : AbstractRoslynProject, IProjectShim
        {
            #region Options
            void IProjectShim.SetCommandLineArguments(CommandLineArguments commandLineArguments)
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

            void IProjectShim.RemoveMetadataReference(string referencePath)
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
            public void AddSourceFile(string filePath, uint itemId, SourceCodeKind sourceCodeKind = SourceCodeKind.Regular)
            {
                AddFile(filePath, sourceCodeKind, itemId, CanUseTextBuffer);
            }

            public void RemoveSourceFile(string filePath)
            {
                RemoveFile(filePath);
            }

            public void RenameSourceFile(string originalFilePath, string newFilePath)
            {
                var currentDocument = GetCurrentDocumentFromPath(originalFilePath);
                if (currentDocument != null)
                {
                    RemoveFile(currentDocument.FilePath);
                    AddFile(newFilePath, currentDocument.GetInitialState().SourceCodeKind,
                        currentDocument.GetItemId(), CanUseTextBuffer);
                }
            }

            private bool CanUseTextBuffer(ITextBuffer textBuffer)
            {
                return true;
            }

            #endregion
        }
    }
}
