// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Host;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis
{
    internal partial class ProjectState
    {
        private readonly ProjectInfo projectInfo;
        private readonly HostLanguageServices languageServices;
        private readonly SolutionServices solutionServices;
        private readonly ImmutableDictionary<DocumentId, DocumentState> documentStates;
        private readonly ImmutableList<DocumentId> documentIds;
        private readonly AsyncLazy<VersionStamp> lazyLatestDocumentVersion;
        private readonly AsyncLazy<VersionStamp> lazyLatestDocumentTopLevelChangeVersion;

        private ProjectState(
            ProjectInfo projectInfo,
            HostLanguageServices languageServices,
            SolutionServices solutionServices,
            ImmutableList<DocumentId> documentIds,
            ImmutableDictionary<DocumentId, DocumentState> documentStates,
            AsyncLazy<VersionStamp> lazyLatestDocumentVersion,
            AsyncLazy<VersionStamp> lazyLatestDocumentTopLevelChangeVersion)
        {
            this.projectInfo = projectInfo;
            this.solutionServices = solutionServices;
            this.languageServices = languageServices;
            this.documentIds = documentIds;
            this.documentStates = documentStates;
            this.lazyLatestDocumentVersion = lazyLatestDocumentVersion;
            this.lazyLatestDocumentTopLevelChangeVersion = lazyLatestDocumentTopLevelChangeVersion;
        }

        internal ProjectState(ProjectInfo projectInfo, HostLanguageServices languageServices, SolutionServices solutionServices)
        {
            Contract.ThrowIfNull(projectInfo);
            Contract.ThrowIfNull(languageServices);
            Contract.ThrowIfNull(solutionServices);

            this.languageServices = languageServices;
            this.solutionServices = solutionServices;

            this.projectInfo = FixProjectInfo(projectInfo);

            this.documentIds = this.projectInfo.Documents.Select(d => d.Id).ToImmutableList();

            var docStates = ImmutableDictionary.CreateRange<DocumentId, DocumentState>(
                this.projectInfo.Documents.Select(d =>
                    new KeyValuePair<DocumentId, DocumentState>(d.Id,
                        CreateDocument(this.ProjectInfo, d, languageServices, solutionServices))));

            this.documentStates = docStates;

            this.lazyLatestDocumentVersion = new AsyncLazy<VersionStamp>(c => ComputeLatestDocumentVersionAsync(docStates, c), cacheResult: true);
            this.lazyLatestDocumentTopLevelChangeVersion = new AsyncLazy<VersionStamp>(c => ComputeLatestDocumentTopLevelChangeVersionAsync(docStates, c), cacheResult: true);
        }

        private ProjectInfo FixProjectInfo(ProjectInfo projectInfo)
        {
            if (projectInfo.CompilationOptions == null)
            {
                var compilationFactory = this.languageServices.GetService<ICompilationFactoryService>();
                if (compilationFactory != null)
                {
                    projectInfo = projectInfo.WithCompilationOptions(compilationFactory.GetDefaultCompilationOptions());
                }
            }

            if (projectInfo.ParseOptions == null)
            {
                var syntaxTreeFactory = this.languageServices.GetService<ISyntaxTreeFactoryService>();
                if (syntaxTreeFactory != null)
                {
                    projectInfo = projectInfo.WithParseOptions(syntaxTreeFactory.GetDefaultParseOptions());
                }
            }

            return projectInfo;
        }

        private static async Task<VersionStamp> ComputeLatestDocumentVersionAsync(ImmutableDictionary<DocumentId, DocumentState> documentStates, CancellationToken cancellationToken)
        {
            // this may produce a version that is out of sync with the actual Document versions.
            var latestVersion = VersionStamp.Default;
            foreach (var doc in documentStates.Values)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (!doc.IsGenerated)
                {
                    var version = await doc.GetTextVersionAsync(cancellationToken).ConfigureAwait(false);
                    latestVersion = version.GetNewerVersion(latestVersion);
                }
            }

            return latestVersion;
        }

        private AsyncLazy<VersionStamp> CreateLazyLatestDocumentTopLevelChangeVersion(DocumentState newDocument, ImmutableDictionary<DocumentId, DocumentState> newDocumentStates)
        {
            VersionStamp oldVersion;
            if (this.lazyLatestDocumentTopLevelChangeVersion.TryGetValue(out oldVersion))
            {
                return new AsyncLazy<VersionStamp>(c => ComputeTopLevelChangeTextVersionAsync(oldVersion, newDocument, c), cacheResult: true);
            }
            else
            {
                return new AsyncLazy<VersionStamp>(c => ComputeLatestDocumentTopLevelChangeVersionAsync(newDocumentStates, c), cacheResult: true);
            }
        }

        private static async Task<VersionStamp> ComputeTopLevelChangeTextVersionAsync(VersionStamp oldVersion, DocumentState newDocument, CancellationToken cancellationToken)
        {
            var newVersion = await newDocument.GetTopLevelChangeTextVersionAsync(cancellationToken).ConfigureAwait(false);
            return newVersion.GetNewerVersion(oldVersion);
        }

        private static async Task<VersionStamp> ComputeLatestDocumentTopLevelChangeVersionAsync(ImmutableDictionary<DocumentId, DocumentState> documentStates, CancellationToken cancellationToken)
        {
            // this may produce a version that is out of sync with the actual Document versions.
            var latestVersion = VersionStamp.Default;
            foreach (var doc in documentStates.Values)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var version = await doc.GetTopLevelChangeTextVersionAsync(cancellationToken).ConfigureAwait(false);
                latestVersion = version.GetNewerVersion(latestVersion);
            }

            return latestVersion;
        }

        private static DocumentState CreateDocument(ProjectInfo projectInfo, DocumentInfo documentInfo, HostLanguageServices languageServices, SolutionServices solutionServices)
        {
            var doc = DocumentState.Create(documentInfo, projectInfo.ParseOptions, languageServices, solutionServices);

            if (doc.SourceCodeKind != documentInfo.SourceCodeKind)
            {
                doc = doc.UpdateSourceCodeKind(documentInfo.SourceCodeKind);
            }

            return doc;
        }

        public ProjectId Id
        {
            get { return this.ProjectInfo.Id; }
        }

        public string FilePath
        {
            get { return this.ProjectInfo.FilePath; }
        }

        public string OutputFilePath
        {
            get { return this.ProjectInfo.OutputFilePath; }
        }

        public HostLanguageServices LanguageServices
        {
            get { return this.languageServices; }
        }

        public string Name
        {
            get { return this.ProjectInfo.Name; }
        }

        public bool IsSubmission
        {
            get { return this.ProjectInfo.IsSubmission; }
        }

        public Type HostObjectType
        {
            get { return this.ProjectInfo.HostObjectType; }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Collapsed)]
        public VersionStamp Version
        {
            get { return this.ProjectInfo.Version; }
        }

        public Task<VersionStamp> GetLatestDocumentVersionAsync(CancellationToken cancellationToken)
        {
            return this.lazyLatestDocumentVersion.GetValueAsync(cancellationToken);
        }

        public Task<VersionStamp> GetLatestDocumentTopLevelChangeVersionAsync(CancellationToken cancellationToken)
        {
            return this.lazyLatestDocumentTopLevelChangeVersion.GetValueAsync(cancellationToken);
        }

        public async Task<VersionStamp> GetSemanticVersionAsync(CancellationToken cancellationToken = default(CancellationToken))
        {
            var docVersion = await this.GetLatestDocumentTopLevelChangeVersionAsync(cancellationToken).ConfigureAwait(false);
            return docVersion.GetNewerVersion(this.Version);
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Collapsed)]
        public ProjectInfo ProjectInfo
        {
            get { return this.projectInfo; }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Collapsed)]
        public string AssemblyName
        {
            get { return this.ProjectInfo.AssemblyName; }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Collapsed)]
        public CompilationOptions CompilationOptions
        {
            get { return this.ProjectInfo.CompilationOptions; }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Collapsed)]
        public ParseOptions ParseOptions
        {
            get { return this.ProjectInfo.ParseOptions; }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Collapsed)]
        public ImmutableList<MetadataReference> MetadataReferences
        {
            get { return (ImmutableList<MetadataReference>)this.ProjectInfo.MetadataReferences; }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Collapsed)]
        public ImmutableList<AnalyzerReference> AnalyzerReferences
        {
            get { return (ImmutableList<AnalyzerReference>)this.ProjectInfo.AnalyzerReferences; }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Collapsed)]
        public ImmutableList<ProjectReference> ProjectReferences
        {
            get { return (ImmutableList<ProjectReference>)this.ProjectInfo.ProjectReferences; }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Collapsed)]
        public bool HasDocuments
        {
            get { return this.documentIds.Count > 0; }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Collapsed)]
        public IEnumerable<DocumentState> OrderedDocumentStates
        {
            get { return this.DocumentIds.Select(GetDocumentState); }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Collapsed)]
        public ImmutableList<DocumentId> DocumentIds
        {
            get { return this.documentIds; }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Collapsed)]
        private ImmutableDictionary<DocumentId, DocumentState> DocumentStates
        {
            get { return this.documentStates; }
        }

        public bool ContainsDocument(DocumentId documentId)
        {
            return this.documentStates.ContainsKey(documentId);
        }

        public DocumentState GetDocumentState(DocumentId documentId)
        {
            DocumentState state;
            this.documentStates.TryGetValue(documentId, out state);
            return state;
        }

        private ProjectState With(
            ProjectInfo projectInfo = null,
            ImmutableList<DocumentId> documentIds = null,
            ImmutableDictionary<DocumentId, DocumentState> documentStates = null,
            AsyncLazy<VersionStamp> latestDocumentVersion = null,
            AsyncLazy<VersionStamp> latestDocumentTopLevelChangeVersion = null)
        {
            return new ProjectState(
                projectInfo ?? this.projectInfo,
                this.languageServices,
                this.solutionServices,
                documentIds ?? this.documentIds,
                documentStates ?? this.documentStates,
                latestDocumentVersion ?? this.lazyLatestDocumentVersion,
                latestDocumentTopLevelChangeVersion ?? this.lazyLatestDocumentTopLevelChangeVersion);
        }

        public ProjectState UpdateAssemblyName(string assemblyName)
        {
            if (assemblyName == this.AssemblyName)
            {
                return this;
            }

            return this.With(projectInfo: this.ProjectInfo.WithAssemblyName(assemblyName).WithVersion(this.Version.GetNewerVersion()));
        }

        public ProjectState UpdateOutputPath(string outputFilePath)
        {
            if (outputFilePath == this.OutputFilePath)
            {
                return this;
            }

            return this.With(projectInfo: this.ProjectInfo.WithOutputFilePath(outputFilePath).WithVersion(this.Version.GetNewerVersion()));
        }

        public ProjectState UpdateCompilationOptions(CompilationOptions options)
        {
            if (options == this.CompilationOptions)
            {
                return this;
            }

            return this.With(projectInfo: this.ProjectInfo.WithCompilationOptions(options).WithVersion(this.Version.GetNewerVersion()));
        }

        public ProjectState UpdateParseOptions(ParseOptions options)
        {
            if (options == this.ParseOptions)
            {
                return this;
            }

            // update parse options for all documents too
            var docMap = this.documentStates;

            foreach (var docId in this.documentStates.Keys)
            {
                var oldDocState = this.GetDocumentState(docId);
                var newDocState = oldDocState.UpdateParseOptions(options);
                docMap = docMap.SetItem(docId, newDocState);
            }

            return this.With(
                projectInfo: this.ProjectInfo.WithParseOptions(options).WithVersion(this.Version.GetNewerVersion()),
                documentStates: docMap);
        }

        public static bool IsSameLanguage(ProjectState project1, ProjectState project2)
        {
            return project1.LanguageServices == project2.LanguageServices;
        }

        public ProjectState AddProjectReference(ProjectReference projectReference)
        {
            Contract.Requires(!this.ProjectReferences.Contains(projectReference));

            return this.With(
                projectInfo: this.ProjectInfo.WithProjectReferences(this.ProjectReferences.Add(projectReference)).WithVersion(this.Version.GetNewerVersion()));
        }

        public ProjectState RemoveProjectReference(ProjectReference projectReference)
        {
            Contract.Requires(this.ProjectReferences.Contains(projectReference));

            return this.With(
                projectInfo: this.ProjectInfo.WithProjectReferences(this.ProjectReferences.Remove(projectReference)).WithVersion(this.Version.GetNewerVersion()));
        }

        public ProjectState AddProjectReferences(IEnumerable<ProjectReference> projectReferences)
        {
            var newProjectRefs = this.ProjectReferences;
            foreach (var projectReference in projectReferences)
            {
                Contract.Requires(!newProjectRefs.Contains(projectReference));
                newProjectRefs = newProjectRefs.Add(projectReference);
            }

            return this.With(
                projectInfo: this.ProjectInfo.WithProjectReferences(newProjectRefs).WithVersion(this.Version.GetNewerVersion()));
        }

        public ProjectState WithProjectReferences(IEnumerable<ProjectReference> projectReferences)
        {
            return this.With(
                projectInfo: this.ProjectInfo.WithProjectReferences(projectReferences.ToImmutableListOrEmpty()).WithVersion(this.Version.GetNewerVersion()));
        }

        public ProjectState AddMetadataReference(MetadataReference toMetadata)
        {
            Contract.Requires(!this.MetadataReferences.Contains(toMetadata));

            return this.With(
                projectInfo: this.ProjectInfo.WithMetadataReferences(this.MetadataReferences.Add(toMetadata)).WithVersion(this.Version.GetNewerVersion()));
        }

        public ProjectState RemoveMetadataReference(MetadataReference toMetadata)
        {
            Contract.Requires(this.MetadataReferences.Contains(toMetadata));

            return this.With(
                projectInfo: this.ProjectInfo.WithMetadataReferences(this.MetadataReferences.Remove(toMetadata)).WithVersion(this.Version.GetNewerVersion()));
        }

        public ProjectState AddMetadataReferences(IEnumerable<MetadataReference> metadataReferences)
        {
            var newMetaRefs = this.MetadataReferences;
            foreach (var metadataReference in metadataReferences)
            {
                Contract.Requires(!newMetaRefs.Contains(metadataReference));
                newMetaRefs = newMetaRefs.Add(metadataReference);
            }

            return this.With(
                projectInfo: this.ProjectInfo.WithMetadataReferences(newMetaRefs).WithVersion(this.Version.GetNewerVersion()));
        }

        public ProjectState WithMetadataReferences(IEnumerable<MetadataReference> metadataReferences)
        {
            return this.With(
                projectInfo: this.ProjectInfo.WithMetadataReferences(metadataReferences.ToImmutableListOrEmpty()).WithVersion(this.Version.GetNewerVersion()));
        }

        public ProjectState AddAnalyzerReference(AnalyzerReference analyzerReference)
        {
            Contract.Requires(!this.AnalyzerReferences.Contains(analyzerReference));

            return this.With(
                projectInfo: this.ProjectInfo.WithAnalyzerReferences(this.AnalyzerReferences.Add(analyzerReference)).WithVersion(this.Version.GetNewerVersion()));
        }

        public ProjectState RemoveAnalyzerReference(AnalyzerReference analyzerReference)
        {
            Contract.Requires(this.AnalyzerReferences.Contains(analyzerReference));

            return this.With(
                projectInfo: this.ProjectInfo.WithAnalyzerReferences(this.AnalyzerReferences.Remove(analyzerReference)).WithVersion(this.Version.GetNewerVersion()));
        }

        public ProjectState AddAnalyzerReferences(IEnumerable<AnalyzerReference> analyzerReferences)
        {
            var newAnalyzerReferences = this.AnalyzerReferences;
            foreach (var analyzerReference in analyzerReferences)
            {
                Contract.Requires(!newAnalyzerReferences.Contains(analyzerReference));
                newAnalyzerReferences = newAnalyzerReferences.Add(analyzerReference);
            }

            return this.With(
                projectInfo: this.ProjectInfo.WithAnalyzerReferences(newAnalyzerReferences).WithVersion(this.Version.GetNewerVersion()));
        }

        public ProjectState WithAnalyzerReferences(IEnumerable<AnalyzerReference> analyzerReferences)
        {
            return this.With(
                projectInfo: this.ProjectInfo.WithAnalyzerReferences(analyzerReferences.ToImmutableListOrEmpty()).WithVersion(this.Version.GetNewerVersion()));
        }

        public ProjectState AddDocument(DocumentState document)
        {
            Contract.Requires(!this.DocumentStates.ContainsKey(document.Id));

            return this.With(
                projectInfo: this.ProjectInfo.WithVersion(this.Version.GetNewerVersion()).WithDocuments(this.ProjectInfo.Documents.Concat(document.Info)),
                documentIds: this.DocumentIds.Add(document.Id),
                documentStates: this.DocumentStates.Add(document.Id, document));
        }

        public ProjectState RemoveDocument(DocumentId documentId)
        {
            Contract.Requires(this.DocumentStates.ContainsKey(documentId));

            return this.With(
                projectInfo: this.ProjectInfo.WithVersion(this.Version.GetNewerVersion()).WithDocuments(this.ProjectInfo.Documents.Where(info => info.Id != documentId)),
                documentIds: this.DocumentIds.Remove(documentId),
                documentStates: this.DocumentStates.Remove(documentId));
        }

        public ProjectState RemoveAllDocuments()
        {
            return this.With(
                projectInfo: this.ProjectInfo.WithVersion(this.Version.GetNewerVersion()).WithDocuments(SpecializedCollections.EmptyEnumerable<DocumentInfo>()),
                documentIds: ImmutableList<DocumentId>.Empty,
                documentStates: ImmutableDictionary<DocumentId, DocumentState>.Empty);
        }

        public ProjectState UpdateDocument(DocumentState newDocument, bool textChanged, bool recalculateDependentVersions)
        {
            Contract.Requires(this.ContainsDocument(newDocument.Id));

            var oldDocument = this.GetDocumentState(newDocument.Id);
            if (oldDocument == newDocument)
            {
                return this;
            }

            var newDocumentStates = this.DocumentStates.SetItem(newDocument.Id, newDocument);

            AsyncLazy<VersionStamp> dependentDocumentVersion;
            AsyncLazy<VersionStamp> dependentSemanticVersion;
            GetLatestDependentVersions(
                newDocumentStates, oldDocument, newDocument, recalculateDependentVersions, textChanged,
                out dependentDocumentVersion, out dependentSemanticVersion);

            return this.With(
                documentStates: newDocumentStates,
                latestDocumentVersion: dependentDocumentVersion,
                latestDocumentTopLevelChangeVersion: dependentSemanticVersion);
        }

        private void GetLatestDependentVersions(
            ImmutableDictionary<DocumentId, DocumentState> newDocumentStates,
            DocumentState oldDocument, DocumentState newDocument,
            bool recalculateDependentVersions, bool textChanged,
            out AsyncLazy<VersionStamp> dependentDocumentVersion, out AsyncLazy<VersionStamp> dependentSemanticVersion)
        {
            var recalculateDocumentVersion = false;
            var recalculateSemanticVersion = false;

            if (recalculateDependentVersions)
            {
                VersionStamp oldVersion;
                if (oldDocument.TryGetTextVersion(out oldVersion))
                {
                    VersionStamp documentVersion;
                    if (!this.lazyLatestDocumentVersion.TryGetValue(out documentVersion) || documentVersion == oldVersion)
                    {
                        recalculateDocumentVersion = true;
                    }

                    VersionStamp semanticVersion;
                    if (!this.lazyLatestDocumentTopLevelChangeVersion.TryGetValue(out semanticVersion) || semanticVersion == oldVersion)
                    {
                        recalculateSemanticVersion = true;
                    }
                }
            }

            dependentDocumentVersion = recalculateDocumentVersion ?
                new AsyncLazy<VersionStamp>(c => ComputeLatestDocumentVersionAsync(newDocumentStates, c), cacheResult: true) :
                textChanged ?
                    new AsyncLazy<VersionStamp>(newDocument.GetTextVersionAsync, cacheResult: true) :
                    this.lazyLatestDocumentVersion;

            dependentSemanticVersion = recalculateSemanticVersion ?
                new AsyncLazy<VersionStamp>(c => ComputeLatestDocumentTopLevelChangeVersionAsync(newDocumentStates, c), cacheResult: true) :
                textChanged ?
                    CreateLazyLatestDocumentTopLevelChangeVersion(newDocument, newDocumentStates) :
                    this.lazyLatestDocumentTopLevelChangeVersion;
        }
    }
}
