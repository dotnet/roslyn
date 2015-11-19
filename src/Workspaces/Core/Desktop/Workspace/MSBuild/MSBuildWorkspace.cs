// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

#if !MSBUILD12
using Microsoft.Build.Construction;
#endif

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;
using System.Runtime.InteropServices;

namespace Microsoft.CodeAnalysis.MSBuild
{
    /// <summary>
    /// A workspace that can be populated by opening MSBuild solution and project files.
    /// </summary>
    public sealed class MSBuildWorkspace : Workspace
    {
        // used to serialize access to public methods
        private readonly NonReentrantLock _serializationLock = new NonReentrantLock();

        private MSBuildProjectLoader _loader;

        private MSBuildWorkspace(
            HostServices hostServices,
            ImmutableDictionary<string, string> properties)
            : base(hostServices, "MSBuildWorkspace")
        {
            _loader = new MSBuildProjectLoader(this, properties);
        }

        /// <summary>
        /// Create a new instance of a workspace that can be populated by opening solution and project files.
        /// </summary>
        public static MSBuildWorkspace Create()
        {
            return Create(ImmutableDictionary<string, string>.Empty);
        }

        /// <summary>
        /// Create a new instance of a workspace that can be populated by opening solution and project files.
        /// </summary>
        /// <param name="properties">An optional set of MSBuild properties used when interpreting project files.
        /// These are the same properties that are passed to msbuild via the /property:&lt;n&gt;=&lt;v&gt; command line argument.</param>
        public static MSBuildWorkspace Create(IDictionary<string, string> properties)
        {
            return Create(properties, DesktopMefHostServices.DefaultServices);
        }

        /// <summary>
        /// Create a new instance of a workspace that can be populated by opening solution and project files.
        /// </summary>
        /// <param name="hostServices">The <see cref="HostServices"/> used to configure this workspace.</param>
        public static MSBuildWorkspace Create(HostServices hostServices)
        {
            return Create(ImmutableDictionary<string, string>.Empty, hostServices);
        }

        /// <summary>
        /// Create a new instance of a workspace that can be populated by opening solution and project files.
        /// </summary>
        /// <param name="properties">The MSBuild properties used when interpreting project files.
        /// These are the same properties that are passed to msbuild via the /property:&lt;n&gt;=&lt;v&gt; command line argument.</param>
        /// <param name="hostServices">The <see cref="HostServices"/> used to configure this workspace.</param>
        public static MSBuildWorkspace Create(IDictionary<string, string> properties, HostServices hostServices)
        {
            if (properties == null)
            {
                throw new ArgumentNullException(nameof(properties));
            }

            if (hostServices == null)
            {
                throw new ArgumentNullException(nameof(hostServices));
            }

            return new MSBuildWorkspace(hostServices, properties.ToImmutableDictionary());
        }

        /// <summary>
        /// The MSBuild properties used when interpreting project files.
        /// These are the same properties that are passed to msbuild via the /property:&lt;n&gt;=&lt;v&gt; command line argument.
        /// </summary>
        public ImmutableDictionary<string, string> Properties
        {
            get { return _loader.Properties; }
        }

        /// <summary>
        /// Determines if metadata from existing output assemblies is loaded instead of opening referenced projects.
        /// If the referenced project is already opened, the metadata will not be loaded.
        /// If the metadata assembly cannot be found the referenced project will be opened instead.
        /// </summary>
        public bool LoadMetadataForReferencedProjects
        {
            get { return _loader.LoadMetadataForReferencedProjects; }
            set { _loader.LoadMetadataForReferencedProjects = value; }
        }

        /// <summary>
        /// Determines if unrecognized projects are skipped when solutions or projects are opened.
        /// 
        /// An project is unrecognized if it either has 
        ///   a) an invalid file path, 
        ///   b) a non-existent project file,
        ///   c) has an unrecognized file extension or 
        ///   d) a file extension associated with an unsupported language.
        /// 
        /// If unrecognized projects cannot be skipped a corresponding exception is thrown.
        /// </summary>
        public bool SkipUnrecognizedProjects
        {
            get { return _loader.SkipUnrecognizedProjects; }
            set { _loader.SkipUnrecognizedProjects = value; }
        }

        /// <summary>
        /// Associates a project file extension with a language name.
        /// </summary>
        public void AssociateFileExtensionWithLanguage(string projectFileExtension, string language)
        {
            _loader.AssociateFileExtensionWithLanguage(projectFileExtension, language);
        }

        /// <summary>
        /// Close the open solution, and reset the workspace to a new empty solution.
        /// </summary>
        public void CloseSolution()
        {
            using (_serializationLock.DisposableWait())
            {
                this.ClearSolution();
            }
        }

        private string GetAbsolutePath(string path, string baseDirectoryPath)
        {
            return Path.GetFullPath(FileUtilities.ResolveRelativePath(path, baseDirectoryPath) ?? path);
        }

        #region Open Solution & Project
        /// <summary>
        /// Open a solution file and all referenced projects.
        /// </summary>
        public async Task<Solution> OpenSolutionAsync(string solutionFilePath, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (solutionFilePath == null)
            {
                throw new ArgumentNullException(nameof(solutionFilePath));
            }

            this.ClearSolution();

            var solutionInfo = await _loader.LoadSolutionInfoAsync(solutionFilePath, cancellationToken: cancellationToken).ConfigureAwait(false);

            // construct workspace from loaded project infos
            this.OnSolutionAdded(solutionInfo);

            this.UpdateReferencesAfterAdd();

            return this.CurrentSolution;
        }

        /// <summary>
        /// Open a project file and all referenced projects.
        /// </summary>
        public async Task<Project> OpenProjectAsync(string projectFilePath, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (projectFilePath == null)
            {
                throw new ArgumentNullException(nameof(projectFilePath));
            }

            var projects = await _loader.LoadProjectInfoAsync(projectFilePath, GetCurrentProjectMap(), cancellationToken).ConfigureAwait(false);

            // add projects to solution
            foreach (var project in projects)
            {
                this.OnProjectAdded(project);
            }

            this.UpdateReferencesAfterAdd();

            return this.CurrentSolution.GetProject(projects[0].Id);
        }

        private ImmutableDictionary<string, ProjectId> GetCurrentProjectMap()
        {
            return this.CurrentSolution.Projects
                .Where(p => !string.IsNullOrEmpty(p.FilePath))
                .ToImmutableDictionary(p => p.FilePath, p => p.Id);
        }

        #endregion

        #region Apply Changes
        public override bool CanApplyChange(ApplyChangesKind feature)
        {
            switch (feature)
            {
                case ApplyChangesKind.ChangeDocument:
                case ApplyChangesKind.AddDocument:
                case ApplyChangesKind.RemoveDocument:
                case ApplyChangesKind.AddMetadataReference:
                case ApplyChangesKind.RemoveMetadataReference:
                case ApplyChangesKind.AddProjectReference:
                case ApplyChangesKind.RemoveProjectReference:
                case ApplyChangesKind.AddAnalyzerReference:
                case ApplyChangesKind.RemoveAnalyzerReference:
                    return true;
                default:
                    return false;
            }
        }

        private bool HasProjectFileChanges(ProjectChanges changes)
        {
            return changes.GetAddedDocuments().Any() ||
                   changes.GetRemovedDocuments().Any() ||
                   changes.GetAddedMetadataReferences().Any() ||
                   changes.GetRemovedMetadataReferences().Any() ||
                   changes.GetAddedProjectReferences().Any() ||
                   changes.GetRemovedProjectReferences().Any() ||
                   changes.GetAddedAnalyzerReferences().Any() ||
                   changes.GetRemovedAnalyzerReferences().Any();
        }

        private IProjectFile _applyChangesProjectFile;

        public override bool TryApplyChanges(Solution newSolution)
        {
            using (_serializationLock.DisposableWait())
            {
                return base.TryApplyChanges(newSolution);
            }
        }

        protected override void ApplyProjectChanges(ProjectChanges projectChanges)
        {
            System.Diagnostics.Debug.Assert(_applyChangesProjectFile == null);

            var project = projectChanges.OldProject ?? projectChanges.NewProject;

            try
            {
                // if we need to modify the project file, load it first.
                if (this.HasProjectFileChanges(projectChanges))
                {
                    var projectPath = project.FilePath;
                    IProjectFileLoader loader;
                    if (_loader.TryGetLoaderFromProjectPath(projectPath, out loader))
                    {
                        try
                        {
                            _applyChangesProjectFile = loader.LoadProjectFileAsync(projectPath, _loader.Properties, CancellationToken.None).Result;
                        }
                        catch (System.IO.IOException exception)
                        {
                            this.OnWorkspaceFailed(new ProjectDiagnostic(WorkspaceDiagnosticKind.Failure, exception.Message, projectChanges.ProjectId));
                        }
                    }
                }

                // do normal apply operations
                base.ApplyProjectChanges(projectChanges);

                // save project file
                if (_applyChangesProjectFile != null)
                {
                    try
                    {
                        _applyChangesProjectFile.Save();
                    }
                    catch (System.IO.IOException exception)
                    {
                        this.OnWorkspaceFailed(new ProjectDiagnostic(WorkspaceDiagnosticKind.Failure, exception.Message, projectChanges.ProjectId));
                    }
                }
            }
            finally
            {
                _applyChangesProjectFile = null;
            }
        }

        protected override void ApplyDocumentTextChanged(DocumentId documentId, SourceText text)
        {
            var document = this.CurrentSolution.GetDocument(documentId);
            if (document != null)
            {
                Encoding encoding = DetermineEncoding(text, document);

                this.SaveDocumentText(documentId, document.FilePath, text, encoding ?? new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
                this.OnDocumentTextChanged(documentId, text, PreservationMode.PreserveValue);
            }
        }

        private static Encoding DetermineEncoding(SourceText text, Document document)
        {
            if (text.Encoding != null)
            {
                return text.Encoding;
            }

            try
            {
                using (ExceptionHelpers.SuppressFailFast())
                {
                    using (var stream = new FileStream(document.FilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                    {
                        var onDiskText = EncodedStringText.Create(stream);
                        return onDiskText.Encoding;
                    }
                }
            }
            catch (IOException)
            {
            }
            catch (InvalidDataException)
            {
            }

            return null;
        }

        protected override void ApplyDocumentAdded(DocumentInfo info, SourceText text)
        {
            System.Diagnostics.Debug.Assert(_applyChangesProjectFile != null);

            var project = this.CurrentSolution.GetProject(info.Id.ProjectId);

            IProjectFileLoader loader;
            if (_loader.TryGetLoaderFromProjectPath(project.FilePath, out loader))
            {
                var extension = _applyChangesProjectFile.GetDocumentExtension(info.SourceCodeKind);
                var fileName = Path.ChangeExtension(info.Name, extension);

                var relativePath = (info.Folders != null && info.Folders.Count > 0)
                    ? Path.Combine(Path.Combine(info.Folders.ToArray()), fileName)
                    : fileName;

                var fullPath = GetAbsolutePath(relativePath, Path.GetDirectoryName(project.FilePath));

                var newDocumentInfo = info.WithName(fileName)
                    .WithFilePath(fullPath)
                    .WithTextLoader(new FileTextLoader(fullPath, text.Encoding));

                // add document to project file
                _applyChangesProjectFile.AddDocument(relativePath);

                // add to solution
                this.OnDocumentAdded(newDocumentInfo);

                // save text to disk
                if (text != null)
                {
                    this.SaveDocumentText(info.Id, fullPath, text, text.Encoding ?? Encoding.UTF8);
                }
            }
        }

        private void SaveDocumentText(DocumentId id, string fullPath, SourceText newText, Encoding encoding)
        {
            try
            {
                using (ExceptionHelpers.SuppressFailFast())
                {
                    var dir = Path.GetDirectoryName(fullPath);
                    if (!Directory.Exists(dir))
                    {
                        Directory.CreateDirectory(dir);
                    }

                    Debug.Assert(encoding != null);
                    using (var writer = new StreamWriter(fullPath, append: false, encoding: encoding))
                    {
                        newText.Write(writer);
                    }
                }
            }
            catch (IOException exception)
            {
                this.OnWorkspaceFailed(new DocumentDiagnostic(WorkspaceDiagnosticKind.Failure, exception.Message, id));
            }
        }

        protected override void ApplyDocumentRemoved(DocumentId documentId)
        {
            Debug.Assert(_applyChangesProjectFile != null);

            var document = this.CurrentSolution.GetDocument(documentId);
            if (document != null)
            {
                _applyChangesProjectFile.RemoveDocument(document.FilePath);
                this.DeleteDocumentFile(document.Id, document.FilePath);
                this.OnDocumentRemoved(documentId);
            }
        }

        private void DeleteDocumentFile(DocumentId documentId, string fullPath)
        {
            try
            {
                if (File.Exists(fullPath))
                {
                    File.Delete(fullPath);
                }
            }
            catch (IOException exception)
            {
                this.OnWorkspaceFailed(new DocumentDiagnostic(WorkspaceDiagnosticKind.Failure, exception.Message, documentId));
            }
            catch (NotSupportedException exception)
            {
                this.OnWorkspaceFailed(new DocumentDiagnostic(WorkspaceDiagnosticKind.Failure, exception.Message, documentId));
            }
            catch (UnauthorizedAccessException exception)
            {
                this.OnWorkspaceFailed(new DocumentDiagnostic(WorkspaceDiagnosticKind.Failure, exception.Message, documentId));
            }
        }

        protected override void ApplyMetadataReferenceAdded(ProjectId projectId, MetadataReference metadataReference)
        {
            Debug.Assert(_applyChangesProjectFile != null);
            var identity = GetAssemblyIdentity(projectId, metadataReference);
            _applyChangesProjectFile.AddMetadataReference(metadataReference, identity);
            this.OnMetadataReferenceAdded(projectId, metadataReference);
        }

        protected override void ApplyMetadataReferenceRemoved(ProjectId projectId, MetadataReference metadataReference)
        {
            Debug.Assert(_applyChangesProjectFile != null);
            var identity = GetAssemblyIdentity(projectId, metadataReference);
            _applyChangesProjectFile.RemoveMetadataReference(metadataReference, identity);
            this.OnMetadataReferenceRemoved(projectId, metadataReference);
        }

        private AssemblyIdentity GetAssemblyIdentity(ProjectId projectId, MetadataReference metadataReference)
        {
            var project = this.CurrentSolution.GetProject(projectId);
            if (!project.MetadataReferences.Contains(metadataReference))
            {
                project = project.AddMetadataReference(metadataReference);
            }

            var compilation = project.GetCompilationAsync(CancellationToken.None).WaitAndGetResult(CancellationToken.None);
            var symbol = compilation.GetAssemblyOrModuleSymbol(metadataReference) as IAssemblySymbol;
            return symbol != null ? symbol.Identity : null;
        }

        protected override void ApplyProjectReferenceAdded(ProjectId projectId, ProjectReference projectReference)
        {
            Debug.Assert(_applyChangesProjectFile != null);

            var project = this.CurrentSolution.GetProject(projectReference.ProjectId);
            if (project != null)
            {
                _applyChangesProjectFile.AddProjectReference(project.Name, new ProjectFileReference(project.FilePath, projectReference.Aliases));
            }

            this.OnProjectReferenceAdded(projectId, projectReference);
        }

        protected override void ApplyProjectReferenceRemoved(ProjectId projectId, ProjectReference projectReference)
        {
            Debug.Assert(_applyChangesProjectFile != null);

            var project = this.CurrentSolution.GetProject(projectReference.ProjectId);
            if (project != null)
            {
                _applyChangesProjectFile.RemoveProjectReference(project.Name, project.FilePath);
            }

            this.OnProjectReferenceRemoved(projectId, projectReference);
        }

        protected override void ApplyAnalyzerReferenceAdded(ProjectId projectId, AnalyzerReference analyzerReference)
        {
            Debug.Assert(_applyChangesProjectFile != null);
            _applyChangesProjectFile.AddAnalyzerReference(analyzerReference);
            this.OnAnalyzerReferenceAdded(projectId, analyzerReference);
        }

        protected override void ApplyAnalyzerReferenceRemoved(ProjectId projectId, AnalyzerReference analyzerReference)
        {
            Debug.Assert(_applyChangesProjectFile != null);
            _applyChangesProjectFile.RemoveAnalyzerReference(analyzerReference);
            this.OnAnalyzerReferenceRemoved(projectId, analyzerReference);
        }
    }
#endregion
}
