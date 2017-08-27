// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Roslyn.Utilities;
using MSB = Microsoft.Build;

namespace Microsoft.CodeAnalysis.MSBuild
{
    internal abstract class ProjectFile : IProjectFile
    {
        private readonly ProjectFileLoader _loader;
        private readonly MSB.Evaluation.Project _loadedProject;
        private readonly string _errorMessage;

        public ProjectFile(ProjectFileLoader loader, MSB.Evaluation.Project loadedProject, string errorMessage)
        {
            _loader = loader;
            _loadedProject = loadedProject;
            _errorMessage = errorMessage;
        }

        ~ProjectFile()
        {
            try
            {
                // unload project so collection will release global strings
                _loadedProject.ProjectCollection.UnloadAllProjects();
            }
            catch
            {
            }
        }

        public virtual string FilePath
        {
            get { return _loadedProject.FullPath; }
        }

        public string ErrorMessage
        {
            get { return _errorMessage; }
        }

        public string GetPropertyValue(string name)
        {
            return _loadedProject.GetPropertyValue(name);
        }

        public abstract SourceCodeKind GetSourceCodeKind(string documentFileName);
        public abstract string GetDocumentExtension(SourceCodeKind kind);
        public abstract Task<ProjectFileInfo> GetProjectFileInfoAsync(CancellationToken cancellationToken);

        public struct BuildInfo
        {
            public readonly MSB.Execution.ProjectInstance Project;
            public readonly string ErrorMessage;

            public BuildInfo(MSB.Execution.ProjectInstance project, string errorMessage)
            {
                this.Project = project;
                this.ErrorMessage = errorMessage;
            }
        }

        protected async Task<BuildInfo> BuildAsync(string taskName, MSB.Framework.ITaskHost taskHost, CancellationToken cancellationToken)
        {
            // create a project instance to be executed by build engine.
            // The executed project will hold the final model of the project after execution via msbuild.
            var executedProject = _loadedProject.CreateProjectInstance();

            if (!executedProject.Targets.ContainsKey("Compile"))
            {
                return new BuildInfo(executedProject, null);
            }

            var hostServices = new MSB.Execution.HostServices();

            // connect the host "callback" object with the host services, so we get called back with the exact inputs to the compiler task.
            hostServices.RegisterHostObject(_loadedProject.FullPath, "CoreCompile", taskName, taskHost);

            var buildParameters = new MSB.Execution.BuildParameters(_loadedProject.ProjectCollection);

            // capture errors that are output in the build log
            var errorBuilder = new StringWriter();
            var errorLogger = new ErrorLogger(errorBuilder) { Verbosity = MSB.Framework.LoggerVerbosity.Normal };
            buildParameters.Loggers = new MSB.Framework.ILogger[] { errorLogger };

            var buildRequestData = new MSB.Execution.BuildRequestData(executedProject, new string[] { "Compile" }, hostServices);

            var result = await this.BuildAsync(buildParameters, buildRequestData, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);

            if (result.OverallResult == MSB.Execution.BuildResultCode.Failure)
            {
                if (result.Exception != null)
                {
                    return new BuildInfo(executedProject, result.Exception.Message);
                }
                else
                {
                    return new BuildInfo(executedProject, errorBuilder.ToString());
                }
            }
            else
            {
                return new BuildInfo(executedProject, null);
            }
        }

        private class ErrorLogger : MSB.Framework.ILogger
        {
            private readonly TextWriter _writer;
            private bool _hasError;
            private MSB.Framework.IEventSource _eventSource;

            public string Parameters { get; set; }
            public MSB.Framework.LoggerVerbosity Verbosity { get; set; }

            public ErrorLogger(TextWriter writer)
            {
                _writer = writer;
            }

            public void Initialize(MSB.Framework.IEventSource eventSource)
            {
                _eventSource = eventSource;
                _eventSource.ErrorRaised += OnErrorRaised;
            }

            private void OnErrorRaised(object sender, MSB.Framework.BuildErrorEventArgs e)
            {
                if (_hasError)
                {
                    _writer.WriteLine();
                }

                _writer.Write($"{e.File}: ({e.LineNumber}, {e.ColumnNumber}): {e.Message}");
                _hasError = true;
            }

            public void Shutdown()
            {
                if (_eventSource != null)
                {
                    _eventSource.ErrorRaised -= OnErrorRaised;
                }
            }
        }

        // this lock is static because we are using the default build manager, and there is only one per process
        private static readonly SemaphoreSlim s_buildManagerLock = new SemaphoreSlim(initialCount: 1);

        private async Task<MSB.Execution.BuildResult> BuildAsync(MSB.Execution.BuildParameters parameters, MSB.Execution.BuildRequestData requestData, CancellationToken cancellationToken)
        {
            // only allow one build to use the default build manager at a time
            using (await s_buildManagerLock.DisposableWaitAsync(cancellationToken).ConfigureAwait(continueOnCapturedContext: false))
            {
                return await BuildAsync(MSB.Execution.BuildManager.DefaultBuildManager, parameters, requestData, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
            }
        }

        private static Task<MSB.Execution.BuildResult> BuildAsync(MSB.Execution.BuildManager buildManager, MSB.Execution.BuildParameters parameters, MSB.Execution.BuildRequestData requestData, CancellationToken cancellationToken)
        {
            var taskSource = new TaskCompletionSource<MSB.Execution.BuildResult>();

            buildManager.BeginBuild(parameters);

            // enable cancellation of build
            CancellationTokenRegistration registration = default;
            if (cancellationToken.CanBeCanceled)
            {
                registration = cancellationToken.Register(() =>
                {
                    try
                    {
                        buildManager.CancelAllSubmissions();
                        buildManager.EndBuild();
                        registration.Dispose();
                    }
                    finally
                    {
                        taskSource.TrySetCanceled();
                    }
                });
            }

            // execute build async
            try
            {
                buildManager.PendBuildRequest(requestData).ExecuteAsync(sub =>
                {
                    // when finished
                    try
                    {
                        var result = sub.BuildResult;
                        buildManager.EndBuild();
                        registration.Dispose();
                        taskSource.TrySetResult(result);
                    }
                    catch (Exception e)
                    {
                        taskSource.TrySetException(e);
                    }
                }, null);
            }
            catch (Exception e)
            {
                taskSource.SetException(e);
            }

            return taskSource.Task;
        }

        protected virtual string GetOutputDirectory()
        {
            var targetPath = _loadedProject.GetPropertyValue("TargetPath");

            if (string.IsNullOrEmpty(targetPath))
            {
                targetPath = _loadedProject.DirectoryPath;
            }

            return Path.GetDirectoryName(this.GetAbsolutePath(targetPath));
        }

        protected virtual string GetAssemblyName()
        {
            var assemblyName = _loadedProject.GetPropertyValue("AssemblyName");

            if (string.IsNullOrEmpty(assemblyName))
            {
                assemblyName = Path.GetFileNameWithoutExtension(_loadedProject.FullPath);
            }

            return PathUtilities.GetFileName(assemblyName);
        }

        protected bool IsProjectReferenceOutputAssembly(MSB.Framework.ITaskItem item)
        {
            return item.GetMetadata("ReferenceOutputAssembly") == "true";
        }

        protected IEnumerable<ProjectFileReference> GetProjectReferences(MSB.Execution.ProjectInstance executedProject)
        {
            return executedProject
                .GetItems("ProjectReference")
                .Where(i => !string.Equals(
                    i.GetMetadataValue("ReferenceOutputAssembly"),
                    bool.FalseString,
                    StringComparison.OrdinalIgnoreCase))
                .Select(CreateProjectFileReference);
        }

        /// <summary>
        /// Create a <see cref="ProjectFileReference"/> from a ProjectReference node in the MSBuild file.
        /// </summary>
        protected virtual ProjectFileReference CreateProjectFileReference(MSB.Execution.ProjectItemInstance reference)
        {
            return new ProjectFileReference(
                path: reference.EvaluatedInclude,
                aliases: ImmutableArray<string>.Empty);
        }

        protected virtual IEnumerable<MSB.Framework.ITaskItem> GetDocumentsFromModel(MSB.Execution.ProjectInstance executedProject)
        {
            return executedProject.GetItems("Compile");
        }

        protected virtual IEnumerable<MSB.Framework.ITaskItem> GetMetadataReferencesFromModel(MSB.Execution.ProjectInstance executedProject)
        {
            return executedProject.GetItems("ReferencePath");
        }

        protected virtual IEnumerable<MSB.Framework.ITaskItem> GetAnalyzerReferencesFromModel(MSB.Execution.ProjectInstance executedProject)
        {
            return executedProject.GetItems("Analyzer");
        }

        protected virtual IEnumerable<MSB.Framework.ITaskItem> GetAdditionalFilesFromModel(MSB.Execution.ProjectInstance executedProject)
        {
            return executedProject.GetItems("AdditionalFiles");
        }

        public MSB.Evaluation.ProjectProperty GetProperty(string name)
        {
            return _loadedProject.GetProperty(name);
        }

        protected IEnumerable<MSB.Framework.ITaskItem> GetTaskItems(MSB.Execution.ProjectInstance executedProject, string itemType)
        {
            return executedProject.GetItems(itemType);
        }

        protected string GetItemString(MSB.Execution.ProjectInstance executedProject, string itemType)
        {
            string text = "";
            foreach (var item in executedProject.GetItems(itemType))
            {
                if (text.Length > 0)
                {
                    text = text + " ";
                }

                text = text + item.EvaluatedInclude;
            }

            return text;
        }

        protected string ReadPropertyString(MSB.Execution.ProjectInstance executedProject, string propertyName)
        {
            return this.ReadPropertyString(executedProject, propertyName, propertyName);
        }

        protected string ReadPropertyString(MSB.Execution.ProjectInstance executedProject, string executedPropertyName, string evaluatedPropertyName)
        {
            var executedProperty = executedProject.GetProperty(executedPropertyName);
            if (executedProperty != null)
            {
                return executedProperty.EvaluatedValue;
            }

            var evaluatedProperty = _loadedProject.GetProperty(evaluatedPropertyName);
            if (evaluatedProperty != null)
            {
                return evaluatedProperty.EvaluatedValue;
            }

            return null;
        }

        protected bool ReadPropertyBool(MSB.Execution.ProjectInstance executedProject, string propertyName)
        {
            return ConvertToBool(ReadPropertyString(executedProject, propertyName));
        }

        protected bool ReadPropertyBool(MSB.Execution.ProjectInstance executedProject, string executedPropertyName, string evaluatedPropertyName)
        {
            return ConvertToBool(ReadPropertyString(executedProject, executedPropertyName, evaluatedPropertyName));
        }

        private static bool ConvertToBool(string value)
        {
            return value != null && (string.Equals("true", value, StringComparison.OrdinalIgnoreCase) ||
                string.Equals("On", value, StringComparison.OrdinalIgnoreCase));
        }

        protected int ReadPropertyInt(MSB.Execution.ProjectInstance executedProject, string propertyName)
        {
            return ConvertToInt(ReadPropertyString(executedProject, propertyName));
        }

        protected int ReadPropertyInt(MSB.Execution.ProjectInstance executedProject, string executedPropertyName, string evaluatedPropertyName)
        {
            return ConvertToInt(ReadPropertyString(executedProject, executedPropertyName, evaluatedPropertyName));
        }

        private static int ConvertToInt(string value)
        {
            if (value == null)
            {
                return 0;
            }
            else
            {
                int.TryParse(value, out var result);
                return result;
            }
        }

        protected ulong ReadPropertyULong(MSB.Execution.ProjectInstance executedProject, string propertyName)
        {
            return ConvertToULong(ReadPropertyString(executedProject, propertyName));
        }

        protected ulong ReadPropertyULong(MSB.Execution.ProjectInstance executedProject, string executedPropertyName, string evaluatedPropertyName)
        {
            return ConvertToULong(this.ReadPropertyString(executedProject, executedPropertyName, evaluatedPropertyName));
        }

        private static ulong ConvertToULong(string value)
        {
            if (value == null)
            {
                return 0;
            }
            else
            {
                ulong.TryParse(value, out var result);
                return result;
            }
        }

        protected TEnum? ReadPropertyEnum<TEnum>(MSB.Execution.ProjectInstance executedProject, string propertyName)
            where TEnum : struct
        {
            return ConvertToEnum<TEnum>(ReadPropertyString(executedProject, propertyName));
        }

        protected TEnum? ReadPropertyEnum<TEnum>(MSB.Execution.ProjectInstance executedProject, string executedPropertyName, string evaluatedPropertyName)
            where TEnum : struct
        {
            return ConvertToEnum<TEnum>(ReadPropertyString(executedProject, executedPropertyName, evaluatedPropertyName));
        }

        private static TEnum? ConvertToEnum<TEnum>(string value)
            where TEnum : struct
        {
            if (value == null)
            {
                return null;
            }
            else
            {
                if (Enum.TryParse<TEnum>(value, out var result))
                {
                    return result;
                }
                else
                {
                    return null;
                }
            }
        }

        /// <summary>
        /// Resolves the given path that is possibly relative to the project directory.
        /// </summary>
        /// <remarks>
        /// The resulting path is absolute but might not be normalized.
        /// </remarks>
        protected string GetAbsolutePath(string path)
        {
            // TODO (tomat): should we report an error when drive-relative path (e.g. "C:goo.cs") is encountered?
            return Path.GetFullPath(FileUtilities.ResolveRelativePath(path, _loadedProject.DirectoryPath) ?? path);
        }

        protected string GetDocumentFilePath(MSB.Framework.ITaskItem documentItem)
        {
            return GetAbsolutePath(documentItem.ItemSpec);
        }

        protected static bool IsDocumentLinked(MSB.Framework.ITaskItem documentItem)
        {
            return !string.IsNullOrEmpty(documentItem.GetMetadata("Link"));
        }

        private IDictionary<string, MSB.Evaluation.ProjectItem> _documents;

        protected bool IsDocumentGenerated(MSB.Framework.ITaskItem documentItem)
        {
            if (_documents == null)
            {
                _documents = new Dictionary<string, MSB.Evaluation.ProjectItem>();
                foreach (var item in _loadedProject.GetItems("compile"))
                {
                    _documents[GetAbsolutePath(item.EvaluatedInclude)] = item;
                }
            }

            return !_documents.ContainsKey(GetAbsolutePath(documentItem.ItemSpec));
        }

        protected static string GetDocumentLogicalPath(MSB.Framework.ITaskItem documentItem, string projectDirectory)
        {
            var link = documentItem.GetMetadata("Link");
            if (!string.IsNullOrEmpty(link))
            {
                // if a specific link is specified in the project file then use it to form the logical path.
                return link;
            }
            else
            {
                var result = documentItem.ItemSpec;
                if (Path.IsPathRooted(result))
                {
                    // If we have an absolute path, there are two possibilities:
                    result = Path.GetFullPath(result);

                    // If the document is within the current project directory (or subdirectory), then the logical path is the relative path 
                    // from the project's directory.
                    if (result.StartsWith(projectDirectory, StringComparison.OrdinalIgnoreCase))
                    {
                        result = result.Substring(projectDirectory.Length);
                    }
                    else
                    {
                        // if the document lies outside the project's directory (or subdirectory) then place it logically at the root of the project.
                        // if more than one document ends up with the same logical name then so be it (the workspace will survive.)
                        return Path.GetFileName(result);
                    }
                }

                return result;
            }
        }

        protected string GetReferenceFilePath(MSB.Execution.ProjectItemInstance projectItem)
        {
            return GetAbsolutePath(projectItem.EvaluatedInclude);
        }

        public void AddDocument(string filePath, string logicalPath = null)
        {
            var relativePath = PathUtilities.GetRelativePath(_loadedProject.DirectoryPath, filePath);

            Dictionary<string, string> metadata = null;
            if (logicalPath != null && relativePath != logicalPath)
            {
                metadata = new Dictionary<string, string>();
                metadata.Add("link", logicalPath);
                relativePath = filePath; // link to full path
            }

            _loadedProject.AddItem("Compile", relativePath, metadata);
        }

        public void RemoveDocument(string filePath)
        {
            var relativePath = PathUtilities.GetRelativePath(_loadedProject.DirectoryPath, filePath);

            var items = _loadedProject.GetItems("Compile");
            var item = items.FirstOrDefault(it => PathUtilities.PathsEqual(it.EvaluatedInclude, relativePath)
                                               || PathUtilities.PathsEqual(it.EvaluatedInclude, filePath));
            if (item != null)
            {
                _loadedProject.RemoveItem(item);
            }
        }

        public void AddMetadataReference(MetadataReference reference, AssemblyIdentity identity)
        {
            if (reference is PortableExecutableReference peRef && peRef.FilePath != null)
            {
                var metadata = new Dictionary<string, string>();
                if (!peRef.Properties.Aliases.IsEmpty)
                {
                    metadata.Add("Aliases", string.Join(",", peRef.Properties.Aliases));
                }

                if (IsInGAC(peRef.FilePath) && identity != null)
                {
                    // Since the location of the reference is in GAC, need to use full identity name to find it again.
                    // This typically happens when you base the reference off of a reflection assembly location.
                    _loadedProject.AddItem("Reference", identity.GetDisplayName(), metadata);
                }
                else if (IsFrameworkReferenceAssembly(peRef.FilePath))
                {
                    // just use short name since this will be resolved by msbuild relative to the known framework reference assemblies.
                    var fileName = identity != null ? identity.Name : Path.GetFileNameWithoutExtension(peRef.FilePath);
                    _loadedProject.AddItem("Reference", fileName, metadata);
                }
                else // other location -- need hint to find correct assembly
                {
                    string relativePath = PathUtilities.GetRelativePath(_loadedProject.DirectoryPath, peRef.FilePath);
                    var fileName = Path.GetFileNameWithoutExtension(peRef.FilePath);
                    metadata.Add("HintPath", relativePath);
                    _loadedProject.AddItem("Reference", fileName, metadata);
                }
            }
        }

        private bool IsInGAC(string filePath)
        {
            return GlobalAssemblyCacheLocation.RootLocations.Any(gloc => PathUtilities.IsChildPath(gloc, filePath));
        }

        private static string s_frameworkRoot;
        private static string FrameworkRoot
        {
            get
            {
                if (string.IsNullOrEmpty(s_frameworkRoot))
                {
                    var runtimeDir = System.Runtime.InteropServices.RuntimeEnvironment.GetRuntimeDirectory();
                    s_frameworkRoot = Path.GetDirectoryName(runtimeDir); // back out one directory level to be root path of all framework versions
                }

                return s_frameworkRoot;
            }
        }

        private bool IsFrameworkReferenceAssembly(string filePath)
        {
            return PathUtilities.IsChildPath(FrameworkRoot, filePath);
        }

        public void RemoveMetadataReference(MetadataReference reference, AssemblyIdentity identity)
        {
            if (reference is PortableExecutableReference peRef && peRef.FilePath != null)
            {
                var item = FindReferenceItem(identity, peRef.FilePath);
                if (item != null)
                {
                    _loadedProject.RemoveItem(item);
                }
            }
        }

        private MSB.Evaluation.ProjectItem FindReferenceItem(AssemblyIdentity identity, string filePath)
        {
            var references = _loadedProject.GetItems("Reference");
            MSB.Evaluation.ProjectItem item = null;

            var fileName = Path.GetFileNameWithoutExtension(filePath);

            if (identity != null)
            {
                var shortAssemblyName = identity.Name;
                var fullAssemblyName = identity.GetDisplayName();

                // check for short name match
                item = references.FirstOrDefault(it => string.Compare(it.EvaluatedInclude, shortAssemblyName, StringComparison.OrdinalIgnoreCase) == 0);

                // check for full name match
                if (item == null)
                {
                    item = references.FirstOrDefault(it => string.Compare(it.EvaluatedInclude, fullAssemblyName, StringComparison.OrdinalIgnoreCase) == 0);
                }
            }

            // check for file path match
            if (item == null)
            {
                string relativePath = PathUtilities.GetRelativePath(_loadedProject.DirectoryPath, filePath);

                item = references.FirstOrDefault(it => PathUtilities.PathsEqual(it.EvaluatedInclude, filePath)
                                                    || PathUtilities.PathsEqual(it.EvaluatedInclude, relativePath)
                                                    || PathUtilities.PathsEqual(GetHintPath(it), filePath)
                                                    || PathUtilities.PathsEqual(GetHintPath(it), relativePath));
            }

            // check for partial name match
            if (item == null && identity != null)
            {
                var partialName = identity.Name + ",";
                var items = references.Where(it => it.EvaluatedInclude.StartsWith(partialName, StringComparison.OrdinalIgnoreCase)).ToList();
                if (items.Count == 1)
                {
                    item = items[0];
                }
            }

            return item;
        }

        private string GetHintPath(MSB.Evaluation.ProjectItem item)
        {
            return item.Metadata.FirstOrDefault(m => m.Name == "HintPath")?.EvaluatedValue ?? "";
        }

        public void AddProjectReference(string projectName, ProjectFileReference reference)
        {
            var metadata = new Dictionary<string, string>();
            metadata.Add("Name", projectName);

            if (!reference.Aliases.IsEmpty)
            {
                metadata.Add("Aliases", string.Join(",", reference.Aliases));
            }

            string relativePath = PathUtilities.GetRelativePath(_loadedProject.DirectoryPath, reference.Path);
            _loadedProject.AddItem("ProjectReference", relativePath, metadata);
        }

        public void RemoveProjectReference(string projectName, string projectFilePath)
        {
            string relativePath = PathUtilities.GetRelativePath(_loadedProject.DirectoryPath, projectFilePath);
            var item = FindProjectReferenceItem(projectName, projectFilePath);
            if (item != null)
            {
                _loadedProject.RemoveItem(item);
            }
        }

        private MSB.Evaluation.ProjectItem FindProjectReferenceItem(string projectName, string projectFilePath)
        {
            var references = _loadedProject.GetItems("ProjectReference");
            string relativePath = PathUtilities.GetRelativePath(_loadedProject.DirectoryPath, projectFilePath);

            MSB.Evaluation.ProjectItem item = null;

            // find by project file path
            item = references.First(it => PathUtilities.PathsEqual(it.EvaluatedInclude, relativePath)
                                       || PathUtilities.PathsEqual(it.EvaluatedInclude, projectFilePath));

            // try to find by project name
            if (item == null)
            {
                item = references.First(it => string.Compare(projectName, it.GetMetadataValue("Name"), StringComparison.OrdinalIgnoreCase) == 0);
            }

            return item;
        }

        public void AddAnalyzerReference(AnalyzerReference reference)
        {
            if (reference is AnalyzerFileReference fileRef)
            {
                string relativePath = PathUtilities.GetRelativePath(_loadedProject.DirectoryPath, fileRef.FullPath);
                _loadedProject.AddItem("Analyzer", relativePath);
            }
        }

        public void RemoveAnalyzerReference(AnalyzerReference reference)
        {
            if (reference is AnalyzerFileReference fileRef)
            {
                string relativePath = PathUtilities.GetRelativePath(_loadedProject.DirectoryPath, fileRef.FullPath);

                var analyzers = _loadedProject.GetItems("Analyzer");
                var item = analyzers.FirstOrDefault(it => PathUtilities.PathsEqual(it.EvaluatedInclude, relativePath)
                                                       || PathUtilities.PathsEqual(it.EvaluatedInclude, fileRef.FullPath));
                if (item != null)
                {
                    _loadedProject.RemoveItem(item);
                }
            }
        }

        public void Save()
        {
            _loadedProject.Save();
        }

        internal static bool TryGetOutputKind(string outputKind, out OutputKind kind)
        {
            if (string.Equals(outputKind, "Library", StringComparison.OrdinalIgnoreCase))
            {
                kind = OutputKind.DynamicallyLinkedLibrary;
                return true;
            }
            else if (string.Equals(outputKind, "Exe", StringComparison.OrdinalIgnoreCase))
            {
                kind = OutputKind.ConsoleApplication;
                return true;
            }
            else if (string.Equals(outputKind, "WinExe", StringComparison.OrdinalIgnoreCase))
            {
                kind = OutputKind.WindowsApplication;
                return true;
            }
            else if (string.Equals(outputKind, "Module", StringComparison.OrdinalIgnoreCase))
            {
                kind = OutputKind.NetModule;
                return true;
            }
            else if (string.Equals(outputKind, "WinMDObj", StringComparison.OrdinalIgnoreCase))
            {
                kind = OutputKind.WindowsRuntimeMetadata;
                return true;
            }
            else
            {
                kind = OutputKind.DynamicallyLinkedLibrary;
                return false;
            }
        }
    }
}
