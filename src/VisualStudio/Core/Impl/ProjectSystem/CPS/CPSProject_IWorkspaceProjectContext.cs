// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#nullable enable

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Host;
using Microsoft.VisualStudio.LanguageServices.Implementation.CodeModel;
using Microsoft.VisualStudio.LanguageServices.Implementation.TaskList;
using Microsoft.VisualStudio.LanguageServices.ProjectSystem;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
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
        private readonly VisualStudioProjectOptionsProcessor? _visualStudioProjectOptionsProcessor;

        private readonly VisualStudioWorkspaceImpl _visualStudioWorkspace;
        private readonly IProjectCodeModel _projectCodeModel;
        private readonly Lazy<ProjectExternalErrorReporter?> _externalErrorReporter;

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

        public Guid Guid
        {
            get;
            set; // VisualStudioProject doesn't allow GUID to be changed after creation
        }

        public bool LastDesignTimeBuildSucceeded
        {
            get => _visualStudioProject.HasAllInformation;
            set => _visualStudioProject.HasAllInformation = value;
        }

        public CPSProject(VisualStudioProject visualStudioProject, VisualStudioWorkspaceImpl visualStudioWorkspace, IProjectCodeModelFactory projectCodeModelFactory, Guid projectGuid, string binOutputPath)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            _visualStudioProject = visualStudioProject;
            _visualStudioWorkspace = visualStudioWorkspace;

            _externalErrorReporter = new Lazy<ProjectExternalErrorReporter?>(() =>
            {
                var prefix = visualStudioProject.Language switch
                {
                    LanguageNames.CSharp => "CS",
                    LanguageNames.VisualBasic => "BC",
                    LanguageNames.FSharp => "FS",
                    _ => null
                };

                return (prefix != null) ? new ProjectExternalErrorReporter(visualStudioProject.Id, prefix, visualStudioWorkspace) : null;
            });

            visualStudioWorkspace.SubscribeExternalErrorDiagnosticUpdateSourceToSolutionBuildEvents();

            _projectCodeModel = projectCodeModelFactory.CreateProjectCodeModel(visualStudioProject.Id, new CPSCodeModelInstanceFactory(this));

            // If we have a command line parser service for this language, also set up our ability to process options if they come in
            if (visualStudioWorkspace.Services.GetLanguageServices(visualStudioProject.Language).GetService<ICommandLineParserService>() != null)
            {
                _visualStudioProjectOptionsProcessor = new VisualStudioProjectOptionsProcessor(_visualStudioProject, visualStudioWorkspace.Services);
                _visualStudioWorkspace.AddProjectRuleSetFileToInternalMaps(
                    visualStudioProject,
                    () => _visualStudioProjectOptionsProcessor.EffectiveRuleSetFilePath);
            }

            Guid = projectGuid;
            BinOutputPath = binOutputPath;
        }

        public string? BinOutputPath
        {
            get => _visualStudioProject.OutputFilePath;
            set
            {
                // If we don't have a path, always set it to null
                if (string.IsNullOrEmpty(value))
                {
                    _visualStudioProject.OutputFilePath = null;
                    _visualStudioProject.OutputRefFilePath = null;
                    return;
                }

                // If we only have a non-rooted path, make it full. This is apparently working around cases
                // where CPS pushes us a temporary path when they're loading. It's possible this hack
                // can be removed now, but we still have tests asserting it.
                if (!PathUtilities.IsAbsolute(value))
                {
                    var rootDirectory = _visualStudioProject.FilePath != null
                                        ? Path.GetDirectoryName(_visualStudioProject.FilePath)
                                        : Path.GetTempPath();

                    _visualStudioProject.OutputFilePath = Path.Combine(rootDirectory, value);
                }
                else
                {
                    _visualStudioProject.OutputFilePath = value;
                }

                // Compute the ref path based on the non-ref path. Ideally this should come from the
                // project system but we don't have a way to fetch that.
                _visualStudioProject.OutputRefFilePath =
                    Path.Combine(Path.GetDirectoryName(_visualStudioProject.OutputFilePath),
                    "ref",
                    Path.GetFileName(_visualStudioProject.OutputFilePath));
            }
        }

        internal string? GetIntermediateOutputFilePath()
        {
            return _visualStudioProject.IntermediateOutputFilePath;
        }

        public ProjectId Id => _visualStudioProject.Id;

        public void SetOptions(string commandLineForOptions)
        {
            if (_visualStudioProjectOptionsProcessor != null)
            {
                _visualStudioProjectOptionsProcessor.CommandLine = commandLineForOptions;
            }
        }

        public string? DefaultNamespace
        {
            get => _visualStudioProject.DefaultNamespace;
            private set => _visualStudioProject.DefaultNamespace = value;
        }

        public void SetProperty(string name, string? value)
        {
            if (name == AdditionalPropertyNames.RootNamespace)
            {
                // Right now VB doesn't have the concept of "default namespace". But we conjure one in workspace 
                // by assigning the value of the project's root namespace to it. So various feature can choose to 
                // use it for their own purpose.
                // In the future, we might consider officially exposing "default namespace" for VB project 
                // (e.g. through a <defaultnamespace> msbuild property)
                DefaultNamespace = value;
            }
            else if (name == AdditionalPropertyNames.MaxSupportedLangVersion)
            {
                _visualStudioProject.MaxLangVersion = value;
            }
            else if (name == AdditionalPropertyNames.RunAnalyzers)
            {
                bool? boolValue = bool.TryParse(value, out var parsedBoolValue) ? parsedBoolValue : (bool?)null;
                _visualStudioProject.RunAnalyzers = boolValue;
            }
            else if (name == AdditionalPropertyNames.RunAnalyzersDuringLiveAnalysis)
            {
                bool? boolValue = bool.TryParse(value, out var parsedBoolValue) ? parsedBoolValue : (bool?)null;
                _visualStudioProject.RunAnalyzersDuringLiveAnalysis = boolValue;
            }
        }

        public void AddMetadataReference(string referencePath, MetadataReferenceProperties properties)
        {
            referencePath = FileUtilities.NormalizeAbsolutePath(referencePath);
            _visualStudioProject.AddMetadataReference(referencePath, properties);
        }

        public void RemoveMetadataReference(string referencePath)
        {
            referencePath = FileUtilities.NormalizeAbsolutePath(referencePath);
            _visualStudioProject.RemoveMetadataReference(referencePath, _visualStudioProject.GetPropertiesForMetadataReference(referencePath).Single());
        }

        public void AddProjectReference(IWorkspaceProjectContext project, MetadataReferenceProperties properties)
        {
            var otherProjectId = ((CPSProject)project)._visualStudioProject.Id;
            _visualStudioProject.AddProjectReference(new ProjectReference(otherProjectId, properties.Aliases, properties.EmbedInteropTypes));
        }

        public void RemoveProjectReference(IWorkspaceProjectContext project)
        {
            var otherProjectId = ((CPSProject)project)._visualStudioProject.Id;
            var otherProjectReference = _visualStudioProject.GetProjectReferences().Single(pr => pr.ProjectId == otherProjectId);
            _visualStudioProject.RemoveProjectReference(otherProjectReference);
        }

        public void AddSourceFile(string filePath, bool isInCurrentContext = true, IEnumerable<string>? folderNames = null, SourceCodeKind sourceCodeKind = SourceCodeKind.Regular)
        {
            _visualStudioProject.AddSourceFile(filePath, sourceCodeKind, folderNames.AsImmutableOrNull());
        }

        public void RemoveSourceFile(string filePath)
        {
            _visualStudioProject.RemoveSourceFile(filePath);
            _projectCodeModel.OnSourceFileRemoved(filePath);
        }

        public void AddAdditionalFile(string filePath, bool isInCurrentContext = true)
        {
            _visualStudioProject.AddAdditionalFile(filePath);
        }

        public void Dispose()
        {
            _projectCodeModel?.OnProjectClosed();
            _visualStudioProjectOptionsProcessor?.Dispose();
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

        public void AddDynamicFile(string filePath, IEnumerable<string>? folderNames = null)
        {
            _visualStudioProject.AddDynamicSourceFile(filePath, folderNames.ToImmutableArrayOrEmpty());
        }

        public void RemoveDynamicFile(string filePath)
        {
            _visualStudioProject.RemoveDynamicSourceFile(filePath);
        }

        public void SetRuleSetFile(string filePath)
        {
            // This is now a no-op: we also receive the rule set file through SetOptions, and we'll just use that one
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

        public void ReorderSourceFiles(IEnumerable<string>? filePaths)
        {
            _visualStudioProject.ReorderSourceFiles(filePaths.ToImmutableArrayOrEmpty());
        }

        internal VisualStudioProject GetProject_TestOnly()
        {
            return _visualStudioProject;
        }

        public void AddAnalyzerConfigFile(string filePath)
        {
            _visualStudioProject.AddAnalyzerConfigFile(filePath);
        }

        public void RemoveAnalyzerConfigFile(string filePath)
        {
            _visualStudioProject.RemoveAnalyzerConfigFile(filePath);
        }
    }
}
