// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.LanguageServices.ProjectSystem;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem.CPS
{
    internal sealed partial class CPSProject : AbstractProject, IProjectContext
    {
        #region Project properties
        string IProjectContext.DisplayName
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

        string IProjectContext.ProjectFilePath
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

        Guid IProjectContext.Guid
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

        string IProjectContext.ProjectType
        {
            get
            {
                return base.ProjectType;
            }
            set
            {
                base.ProjectType = value;
            }
        }

        bool IProjectContext.DesignTimeBuildStatus
        {
            get
            {
                return _designTimeBuildStatus;
            }
            set
            {
                _designTimeBuildStatus = value;
            }
        }
        #endregion

        #region Options
        public void SetCommandLineArguments(CommandLineArguments commandLineArguments)
        {
            SetArgumentsAndUpdateOptions(commandLineArguments);
            PostSetCommandLineArguments(commandLineArguments);            
        }

        public void SetCommandLineArguments(string commandLineForOptions)
        {
            var commandLineArguments = SetArgumentsAndUpdateOptions(commandLineForOptions);
            PostSetCommandLineArguments(commandLineArguments);
        }

        private void PostSetCommandLineArguments(CommandLineArguments commandLineArguments)
        {
            // Invoke SetOutputPathAndRelatedData to update the project tracker bin path for this project, if required.
            if (commandLineArguments.OutputFileName != null && commandLineArguments.OutputDirectory != null)
            {
                var newOutputPath = PathUtilities.CombinePathsUnchecked(commandLineArguments.OutputDirectory, commandLineArguments.OutputFileName);
                SetOutputPathAndRelatedData(newOutputPath, hasSameBinAndObjOutputPaths: true);
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

        public void AddProjectReference(IProjectContext project, MetadataReferenceProperties properties)
        {
            var abstractProject = GetAbstractProject(project);

            // AbstractProject and ProjectTracker track project references using the project bin output path.
            // Setting the command line arguments should have already set the output file name and folder.
            // We fetch this output path to add the reference.
            var referencedProject = this.ProjectTracker.GetProject(abstractProject.Id);
            var binPathOpt = referencedProject.TryGetBinOutputPath();
            if (!string.IsNullOrEmpty(binPathOpt))
            {
                AddMetadataReferenceAndTryConvertingToProjectReferenceIfPossible(binPathOpt, properties);
            }
        }

        public void RemoveProjectReference(IProjectContext project)
        {
            var referencedProject = GetAbstractProject(project);

            // AbstractProject and ProjectTracker track project references using the project bin output path.
            // We fetch this output path to remove the reference.
            var binPathOpt = referencedProject.TryGetBinOutputPath();
            if (!string.IsNullOrEmpty(binPathOpt))
            {
                base.RemoveMetadataReference(binPathOpt);
            }
        }

        private AbstractProject GetAbstractProject(IProjectContext project)
        {
            var abstractProject = project as AbstractProject;
            if (abstractProject == null)
            {
                throw new ArgumentException("Unsupported project kind", nameof(project));
            }

            return abstractProject;
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

        #region IDisposable
        public void Dispose()
        {
            Disconnect();
        }
        #endregion
    }
}
