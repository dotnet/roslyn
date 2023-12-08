// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Workspaces.ProjectSystem;
using Microsoft.VisualStudio.LanguageServices.Implementation.CodeModel;
using Microsoft.VisualStudio.LanguageServices.Implementation.TaskList;
using Microsoft.VisualStudio.LanguageServices.ProjectSystem;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem.CPS
{
    internal sealed partial class CPSProject : IWorkspaceProjectContext
    {
        private readonly ProjectSystemProject _projectSystemProject;

        /// <summary>
        /// The <see cref="ProjectSystemProjectOptionsProcessor"/> we're using to parse command line options. Null if we don't
        /// have the ability to parse command line options.
        /// </summary>
        private readonly ProjectSystemProjectOptionsProcessor? _projectSystemProjectOptionsProcessor;

        private readonly VisualStudioWorkspaceImpl _visualStudioWorkspace;
        private readonly IProjectCodeModel _projectCodeModel;
        private readonly Lazy<ProjectExternalErrorReporter?> _externalErrorReporter;

        private readonly ConcurrentQueue<ProjectSystemProject.BatchScope> _batchScopes = new();

        public string DisplayName
        {
            get => _projectSystemProject.DisplayName;
            set => _projectSystemProject.DisplayName = value;
        }

        public string? ProjectFilePath
        {
            get => _projectSystemProject.FilePath;
            set => _projectSystemProject.FilePath = value;
        }

        public bool IsPrimary
        {
            get => _projectSystemProject.IsPrimary;
            set => _projectSystemProject.IsPrimary = value;
        }

        public Guid Guid
        {
            get;
            set; // VisualStudioProject doesn't allow GUID to be changed after creation
        }

        public bool LastDesignTimeBuildSucceeded
        {
            get => _projectSystemProject.HasAllInformation;
            set => _projectSystemProject.HasAllInformation = value;
        }

        public CPSProject(ProjectSystemProject projectSystemProject, VisualStudioWorkspaceImpl visualStudioWorkspace, IProjectCodeModelFactory projectCodeModelFactory, Guid projectGuid)
        {
            _projectSystemProject = projectSystemProject;
            _visualStudioWorkspace = visualStudioWorkspace;

            _externalErrorReporter = new Lazy<ProjectExternalErrorReporter?>(() =>
            {
                var prefix = projectSystemProject.Language switch
                {
                    LanguageNames.CSharp => "CS",
                    LanguageNames.VisualBasic => "BC",
                    LanguageNames.FSharp => "FS",
                    _ => null
                };

                return (prefix != null) ? new ProjectExternalErrorReporter(projectSystemProject.Id, prefix, projectSystemProject.Language, visualStudioWorkspace) : null;
            });

            _projectCodeModel = projectCodeModelFactory.CreateProjectCodeModel(projectSystemProject.Id, new CPSCodeModelInstanceFactory(this));

            // If we have a command line parser service for this language, also set up our ability to process options if they come in
            if (visualStudioWorkspace.Services.GetLanguageServices(projectSystemProject.Language).GetService<ICommandLineParserService>() != null)
            {
                _projectSystemProjectOptionsProcessor = new ProjectSystemProjectOptionsProcessor(_projectSystemProject, visualStudioWorkspace.Services.SolutionServices);
                _visualStudioWorkspace.AddProjectRuleSetFileToInternalMaps(
                    projectSystemProject,
                    () => _projectSystemProjectOptionsProcessor.EffectiveRuleSetFilePath);
            }

            Guid = projectGuid;
        }

        public string? BinOutputPath
        {
            get => _projectSystemProject.OutputFilePath;
            set
            {
                // If we don't have a path, always set it to null
                if (string.IsNullOrEmpty(value))
                {
                    _projectSystemProject.OutputFilePath = null;
                    return;
                }

                // If we only have a non-rooted path, make it full. This is apparently working around cases
                // where CPS pushes us a temporary path when they're loading. It's possible this hack
                // can be removed now, but we still have tests asserting it.
                if (!PathUtilities.IsAbsolute(value))
                {
                    var rootDirectory = _projectSystemProject.FilePath != null
                                        ? Path.GetDirectoryName(_projectSystemProject.FilePath)
                                        : Path.GetTempPath();

                    _projectSystemProject.OutputFilePath = Path.Combine(rootDirectory, value);
                }
                else
                {
                    _projectSystemProject.OutputFilePath = value;
                }
            }
        }

        internal string? CompilationOutputAssemblyFilePath
        {
            get => _projectSystemProject.CompilationOutputAssemblyFilePath;
            set => _projectSystemProject.CompilationOutputAssemblyFilePath = value;
        }

        public ProjectId Id => _projectSystemProject.Id;

        public void SetOptions(string commandLineForOptions)
            => _projectSystemProjectOptionsProcessor?.SetCommandLine(commandLineForOptions);

        public void SetOptions(ImmutableArray<string> arguments)
            => _projectSystemProjectOptionsProcessor?.SetCommandLine(arguments);

        public void SetProperty(string name, string? value)
        {
            if (name == BuildPropertyNames.RootNamespace)
            {
                // Right now VB doesn't have the concept of "default namespace". But we conjure one in workspace 
                // by assigning the value of the project's root namespace to it. So various feature can choose to 
                // use it for their own purpose.
                // In the future, we might consider officially exposing "default namespace" for VB project 
                // (e.g. through a <defaultnamespace> msbuild property)
                _projectSystemProject.DefaultNamespace = value;
            }
            else if (name == BuildPropertyNames.MaxSupportedLangVersion)
            {
                _projectSystemProject.MaxLangVersion = value;
            }
            else if (name == BuildPropertyNames.RunAnalyzers)
            {
                var boolValue = bool.TryParse(value, out var parsedBoolValue) ? parsedBoolValue : (bool?)null;
                _projectSystemProject.RunAnalyzers = boolValue;
            }
            else if (name == BuildPropertyNames.RunAnalyzersDuringLiveAnalysis)
            {
                var boolValue = bool.TryParse(value, out var parsedBoolValue) ? parsedBoolValue : (bool?)null;
                _projectSystemProject.RunAnalyzersDuringLiveAnalysis = boolValue;
            }
            else if (name == BuildPropertyNames.TemporaryDependencyNodeTargetIdentifier && !RoslynString.IsNullOrEmpty(value))
            {
                _projectSystemProject.DependencyNodeTargetIdentifier = value;
            }
            else if (name == BuildPropertyNames.TargetRefPath)
            {
                // If we don't have a path, always set it to null
                if (string.IsNullOrEmpty(value))
                {
                    _projectSystemProject.OutputRefFilePath = null;
                }
                else
                {
                    // If we only have a non-rooted path, make it full. This is apparently working around cases
                    // where CPS pushes us a temporary path when they're loading. It's possible this hack
                    // can be removed now, but we still have tests asserting it.
                    if (!PathUtilities.IsAbsolute(value))
                    {
                        var rootDirectory = _projectSystemProject.FilePath != null
                                            ? Path.GetDirectoryName(_projectSystemProject.FilePath)
                                            : Path.GetTempPath();

                        _projectSystemProject.OutputRefFilePath = Path.Combine(rootDirectory, value);
                    }
                    else
                    {
                        _projectSystemProject.OutputRefFilePath = value;
                    }
                }
            }
        }

        public void AddMetadataReference(string referencePath, MetadataReferenceProperties properties)
        {
            referencePath = FileUtilities.NormalizeAbsolutePath(referencePath);
            _projectSystemProject.AddMetadataReference(referencePath, properties);
        }

        public void RemoveMetadataReference(string referencePath)
        {
            referencePath = FileUtilities.NormalizeAbsolutePath(referencePath);
            _projectSystemProject.RemoveMetadataReference(referencePath, _projectSystemProject.GetPropertiesForMetadataReference(referencePath).Single());
        }

        public void AddProjectReference(IWorkspaceProjectContext project, MetadataReferenceProperties properties)
        {
            var otherProjectId = ((CPSProject)project)._projectSystemProject.Id;
            _projectSystemProject.AddProjectReference(new ProjectReference(otherProjectId, properties.Aliases, properties.EmbedInteropTypes));
        }

        public void RemoveProjectReference(IWorkspaceProjectContext project)
        {
            var otherProjectId = ((CPSProject)project)._projectSystemProject.Id;
            var otherProjectReference = _projectSystemProject.GetProjectReferences().Single(pr => pr.ProjectId == otherProjectId);
            _projectSystemProject.RemoveProjectReference(otherProjectReference);
        }

        public void AddSourceFile(string filePath, bool isInCurrentContext = true, IEnumerable<string>? folderNames = null, SourceCodeKind sourceCodeKind = SourceCodeKind.Regular)
            => _projectSystemProject.AddSourceFile(filePath, sourceCodeKind, folderNames.AsImmutableOrNull());

        public void RemoveSourceFile(string filePath)
        {
            _projectSystemProject.RemoveSourceFile(filePath);
            _projectCodeModel.OnSourceFileRemoved(filePath);
        }

        public void AddAdditionalFile(string filePath, bool isInCurrentContext = true)
            => _projectSystemProject.AddAdditionalFile(filePath);

        public void AddAdditionalFile(string filePath, IEnumerable<string> folderNames, bool isInCurrentContext = true)
            => _projectSystemProject.AddAdditionalFile(filePath, folders: folderNames.ToImmutableArray());

        public void Dispose()
        {
            _projectCodeModel?.OnProjectClosed();
            _projectSystemProjectOptionsProcessor?.Dispose();
            _projectSystemProject.RemoveFromWorkspace();
        }

        public void AddAnalyzerReference(string referencePath)
            => _projectSystemProject.AddAnalyzerReference(referencePath);

        public void RemoveAnalyzerReference(string referencePath)
            => _projectSystemProject.RemoveAnalyzerReference(referencePath);

        public void RemoveAdditionalFile(string filePath)
            => _projectSystemProject.RemoveAdditionalFile(filePath);

        public void AddDynamicFile(string filePath, IEnumerable<string>? folderNames = null)
            => _projectSystemProject.AddDynamicSourceFile(filePath, folderNames.ToImmutableArrayOrEmpty());

        public void RemoveDynamicFile(string filePath)
            => _projectSystemProject.RemoveDynamicSourceFile(filePath);

        public void StartBatch()
            => _batchScopes.Enqueue(_projectSystemProject.CreateBatchScope());

        public ValueTask EndBatchAsync()
        {
            Contract.ThrowIfFalse(_batchScopes.TryDequeue(out var scope));
            return scope.DisposeAsync();
        }

        public void ReorderSourceFiles(IEnumerable<string>? filePaths)
            => _projectSystemProject.ReorderSourceFiles(filePaths.ToImmutableArrayOrEmpty());

        internal ProjectSystemProject GetProject_TestOnly()
            => _projectSystemProject;

        public void AddAnalyzerConfigFile(string filePath)
            => _projectSystemProject.AddAnalyzerConfigFile(filePath);

        public void RemoveAnalyzerConfigFile(string filePath)
            => _projectSystemProject.RemoveAnalyzerConfigFile(filePath);

        public IAsyncDisposable CreateBatchScope() => _projectSystemProject.CreateBatchScope();
    }
}
