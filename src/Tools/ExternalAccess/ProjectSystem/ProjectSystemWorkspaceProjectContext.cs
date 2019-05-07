// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.LanguageServices.ProjectSystem;

namespace Microsoft.CodeAnalysis.ExternalAccess.ProjectSystem
{
    internal readonly struct ProjectSystemWorkspaceProjectContextWrapper
    {
        internal ProjectSystemWorkspaceProjectContextWrapper(IWorkspaceProjectContext workspaceProjectContext)
        {
            WorkspaceProjectContext = workspaceProjectContext;
        }

        internal IWorkspaceProjectContext WorkspaceProjectContext { get; }

        public string DisplayName
        {
            get => WorkspaceProjectContext.DisplayName;
            set => WorkspaceProjectContext.DisplayName = value;
        }

        public string ProjectFilePath
        {
            get => WorkspaceProjectContext.ProjectFilePath;
            set => WorkspaceProjectContext.ProjectFilePath = value;
        }

        public Guid Guid
        {
            get => WorkspaceProjectContext.Guid;
            set => WorkspaceProjectContext.Guid = value;
        }

        public bool LastDesignTimeBuildSucceeded
        {
            get => WorkspaceProjectContext.LastDesignTimeBuildSucceeded;
            set => WorkspaceProjectContext.LastDesignTimeBuildSucceeded = value;
        }

        public string BinOutputPath
        {
            get => WorkspaceProjectContext.BinOutputPath;
            set => WorkspaceProjectContext.BinOutputPath = value;
        }

        public ProjectId Id => WorkspaceProjectContext.Id;

        public void AddAdditionalFile(string filePath, bool isInCurrentContext = true)
            => WorkspaceProjectContext.AddAdditionalFile(filePath, isInCurrentContext);

        public void AddAnalyzerConfigFile(string filePath)
            => WorkspaceProjectContext.AddAnalyzerConfigFile(filePath);

        public void AddAnalyzerReference(string referencePath)
            => WorkspaceProjectContext.AddAnalyzerReference(referencePath);

        public void AddDynamicFile(string filePath, IEnumerable<string> folderNames = null)
            => WorkspaceProjectContext.AddDynamicFile(filePath, folderNames);

        public void AddMetadataReference(string referencePath, MetadataReferenceProperties properties)
            => WorkspaceProjectContext.AddMetadataReference(referencePath, properties);

        public void AddProjectReference(ProjectSystemWorkspaceProjectContextWrapper project, MetadataReferenceProperties properties)
            => WorkspaceProjectContext.AddProjectReference(project.WorkspaceProjectContext, properties);

        public void AddSourceFile(string filePath, bool isInCurrentContext = true, IEnumerable<string> folderNames = null, SourceCodeKind sourceCodeKind = SourceCodeKind.Regular)
            => WorkspaceProjectContext.AddSourceFile(filePath, isInCurrentContext, folderNames, sourceCodeKind);

        public void Dispose()
            => WorkspaceProjectContext.Dispose();

        public void EndBatch()
            => WorkspaceProjectContext.EndBatch();

        public void RemoveAdditionalFile(string filePath)
            => WorkspaceProjectContext.RemoveAdditionalFile(filePath);

        public void RemoveAnalyzerConfigFile(string filePath)
            => WorkspaceProjectContext.RemoveAnalyzerConfigFile(filePath);

        public void RemoveAnalyzerReference(string referencePath)
            => WorkspaceProjectContext.RemoveAnalyzerReference(referencePath);

        public void RemoveDynamicFile(string filePath)
            => WorkspaceProjectContext.RemoveDynamicFile(filePath);

        public void RemoveMetadataReference(string referencePath)
            => WorkspaceProjectContext.RemoveMetadataReference(referencePath);

        public void RemoveProjectReference(ProjectSystemWorkspaceProjectContextWrapper project)
            => WorkspaceProjectContext.RemoveProjectReference(project.WorkspaceProjectContext);

        public void RemoveSourceFile(string filePath)
            => WorkspaceProjectContext.RemoveSourceFile(filePath);

        public void ReorderSourceFiles(IEnumerable<string> filePaths)
            => WorkspaceProjectContext.ReorderSourceFiles(filePaths);

        public void SetOptions(string commandLineForOptions)
            => WorkspaceProjectContext.SetOptions(commandLineForOptions);

        public void SetProperty(string name, string value)
            => WorkspaceProjectContext.SetProperty(name, value);

        public void SetRuleSetFile(string filePath)
            => WorkspaceProjectContext.SetRuleSetFile(filePath);

        public void StartBatch()
            => WorkspaceProjectContext.StartBatch();
    }
}
