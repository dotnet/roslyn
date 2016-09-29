// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.LanguageServices.ProjectSystem;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem.CPS
{
    internal sealed partial class CPSProject : AbstractProject, IWorkspaceProjectContext
    {
        #region Project properties
        string IWorkspaceProjectContext.DisplayName
        {
            get
            {
                return base.DisplayName;
            }
            set
            {
                UpdateProjectDisplayName(value);
            }
        }

        string IWorkspaceProjectContext.ProjectFilePath
        {
            get
            {
                return base.ProjectFilePath;
            }
            set
            {
                UpdateProjectFilePath(value);
            }
        }

        Guid IWorkspaceProjectContext.Guid
        {
            get
            {
                return base.Guid;
            }

            set
            {
                base.Guid = value;
            }
        }

        bool IWorkspaceProjectContext.LastDesignTimeBuildSucceeded
        {
            get
            {
                return _lastDesignTimeBuildSucceeded;
            }
            set
            {
                _lastDesignTimeBuildSucceeded = value;
            }
        }

        string IWorkspaceProjectContext.BinOutputPath
        {
            get
            {
                return base.BinOutputPath;
            }
            set
            {
                SetBinOutputPathAndRelatedData(value);
            }
        }

        #endregion

        #region Options
        public void SetOptions(string commandLineForOptions)
        {
            var commandLineArguments = SetArgumentsAndUpdateOptions(commandLineForOptions);
            PostSetOptions(commandLineArguments);
        }

        private void PostSetOptions(CommandLineArguments commandLineArguments)
        {
            // Invoke SetOutputPathAndRelatedData to update the project obj output path.
            if (commandLineArguments.OutputFileName != null && commandLineArguments.OutputDirectory != null)
            {
                var objOutputPath = PathUtilities.CombinePathsUnchecked(commandLineArguments.OutputDirectory, commandLineArguments.OutputFileName);
                SetObjOutputPathAndRelatedData(objOutputPath);
            }
        }
        #endregion

        #region References
        public void AddMetadataReference(string referencePath, MetadataReferenceProperties properties)
        {
            referencePath = FileUtilities.NormalizeAbsolutePath(referencePath);
            AddMetadataReferenceAndTryConvertingToProjectReferenceIfPossible(referencePath, properties);
        }

        public new void RemoveMetadataReference(string referencePath)
        {
            referencePath = FileUtilities.NormalizeAbsolutePath(referencePath);
            base.RemoveMetadataReference(referencePath);
        }

        public void AddProjectReference(IWorkspaceProjectContext project, MetadataReferenceProperties properties)
        {
            var abstractProject = GetAbstractProject(project);
            AddProjectReference(new ProjectReference(abstractProject.Id, properties.Aliases, properties.EmbedInteropTypes));
        }

        public void RemoveProjectReference(IWorkspaceProjectContext project)
        {
            var referencedProject = GetAbstractProject(project);
            var projectReference = GetCurrentProjectReferences().Single(p => p.ProjectId == referencedProject.Id);
            RemoveProjectReference(projectReference);
        }

        private AbstractProject GetAbstractProject(IWorkspaceProjectContext project)
        {
            var abstractProject = project as AbstractProject;
            if (abstractProject == null)
            {
                throw new ArgumentException("Unsupported project kind", nameof(project));
            }

            return abstractProject;
        }
        #endregion

        #region Files
        public void AddSourceFile(string filePath, bool isInCurrentContext = true, IEnumerable<string> folderNames = null, SourceCodeKind sourceCodeKind = SourceCodeKind.Regular)
        {
            AddFile(filePath, sourceCodeKind, _ => isInCurrentContext, _ => folderNames.ToImmutableArrayOrEmpty());
        }

        public void RemoveSourceFile(string filePath)
        {
            RemoveFile(filePath);
        }

        public void AddAdditionalFile(string filePath, bool isInCurrentContext = true)
        {
            AddAdditionalFile(filePath, getIsInCurrentContext: _ => isInCurrentContext);
        }

        #endregion

        #region IDisposable
        public void Dispose()
        {
            Disconnect();
        }
        #endregion
    }
}
