// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Host;
using Microsoft.VisualStudio.LanguageServices.Implementation.CodeModel;
using Microsoft.VisualStudio.LanguageServices.Implementation.TaskList;
using Microsoft.VisualStudio.LanguageServices.ProjectSystem;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem.CPS
{
    internal sealed partial class CPSProject : IWorkspaceProjectContext
    {
        private readonly VisualStudioProject _visualStudioProject;

        /// <summary>
        /// The <see cref="VisualStudioProjectOptionsProcessor"/> we're using to parse command line options. Null if we don't
        /// have the ability to parse command line options.
        /// </summary>
        private readonly VisualStudioProjectOptionsProcessor _visualStudioProjectOptionsProcessor;

        private readonly VisualStudioWorkspaceImpl _visualStudioWorkspace;
        private readonly IProjectCodeModel _projectCodeModel;
        private readonly ProjectExternalErrorReporter _externalErrorReporterOpt;

        public string DisplayName
        {
            get => _visualStudioProject.DisplayName;
            set => _visualStudioProject.DisplayName = value;
        }

        public string ProjectFilePath
        {
            get => _visualStudioProject.FilePath;
            set => _visualStudioProject.FilePath = value;
        }

        public Guid Guid { get; set; }
        public bool LastDesignTimeBuildSucceeded { get; set; }

        public CPSProject(VisualStudioProject visualStudioProject, VisualStudioWorkspaceImpl visualStudioWorkspace, IProjectCodeModelFactory projectCodeModelFactory, ProjectExternalErrorReporter errorReporterOpt)
        {
            _visualStudioProject = visualStudioProject;
            _visualStudioWorkspace = visualStudioWorkspace;
            _externalErrorReporterOpt = errorReporterOpt;

            _projectCodeModel = projectCodeModelFactory.CreateProjectCodeModel(visualStudioProject.Id, new CPSCodeModelInstanceFactory(this));

            // If we have a command line parser service for this language, also set up our ability to process options if they come in
            if (visualStudioWorkspace.Services.GetLanguageServices(visualStudioProject.Language).GetService<ICommandLineParserService>() != null)
            {
                _visualStudioProjectOptionsProcessor = new VisualStudioProjectOptionsProcessor(_visualStudioProject, visualStudioWorkspace.Services);
            }
        }

        public string BinOutputPath
        {
            get => _visualStudioProject.OutputFilePath;
            set => _visualStudioProject.OutputFilePath = value;
        }

        public ProjectId Id => _visualStudioProject.Id;

        public void SetOptions(string commandLineForOptions)
        {
            if (_visualStudioProjectOptionsProcessor != null)
            {
                _visualStudioProjectOptionsProcessor.CommandLine = commandLineForOptions;
            }
        }

        public void AddMetadataReference(string referencePath, MetadataReferenceProperties properties)
        {
            referencePath = FileUtilities.NormalizeAbsolutePath(referencePath);
            _visualStudioProject.AddMetadataReference(referencePath, properties);
        }

        public void RemoveMetadataReference(string referencePath)
        {
            // TODO: this won't work with non-standard properties
            _visualStudioProject.RemoveMetadataReference(referencePath, MetadataReferenceProperties.Assembly);
        }

        public void AddProjectReference(IWorkspaceProjectContext project, MetadataReferenceProperties properties)
        {
            throw new NotImplementedException();
        }

        public void RemoveProjectReference(IWorkspaceProjectContext project)
        {
            throw new NotImplementedException();
        }

        public void AddSourceFile(string filePath, bool isInCurrentContext = true, IEnumerable<string> folderNames = null, SourceCodeKind sourceCodeKind = SourceCodeKind.Regular)
        {
            _visualStudioProject.AddSourceFile(filePath, sourceCodeKind, folderNames.AsImmutableOrNull());
        }

        public void RemoveSourceFile(string filePath)
        {
            _visualStudioProject.RemoveSourceFile(filePath);
        }

        public void AddAdditionalFile(string filePath, bool isInCurrentContext = true)
        {
            _visualStudioProject.AddAdditionalFile(filePath);
        }

        public void Dispose()
        {
            _visualStudioProject.RemoveFromWorkspace();
        }

        public void AddAnalyzerReference(string referencePath)
        {
            _visualStudioProject.AddAnalyzerReference(referencePath);
        }

        public void RemoveAnalyzerReference(string referencePath)
        {
            _visualStudioProject.RemoveAnalyzerReference(referencePath);
        }

        public void RemoveAdditionalFile(string filePath)
        {
            _visualStudioProject.RemoveAdditionalFile(filePath);
        }

        public void SetRuleSetFile(string filePath)
        {
            // This is now a no-op: we also recieve the rule set file through SetOptions, and we'll just use that one
        }

        private readonly ConcurrentQueue<VisualStudioProject.BatchScope> _batchScopes = new ConcurrentQueue<VisualStudioProject.BatchScope>();

        public void StartBatch()
        {
            _batchScopes.Enqueue(_visualStudioProject.CreateBatchScope());
        }

        public void EndBatch()
        {
            Contract.ThrowIfFalse(_batchScopes.TryDequeue(out var scope));
            scope.Dispose();
        }
        
        internal VisualStudioProject GetProject_TestOnly()
        {
            return _visualStudioProject;
        }
    }
}
