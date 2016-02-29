// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
        private readonly ProjectInfo _projectInfo;
        private readonly HostLanguageServices _languageServices;
        private readonly SolutionServices _solutionServices;
        private readonly ImmutableDictionary<DocumentId, DocumentState> _documentStates;
        private readonly ImmutableDictionary<DocumentId, TextDocumentState> _additionalDocumentStates;
        private readonly IReadOnlyList<DocumentId> _documentIds;
        private readonly IReadOnlyList<DocumentId> _additionalDocumentIds;
        private readonly AsyncLazy<VersionStamp> _lazyLatestDocumentVersion;
        private readonly AsyncLazy<VersionStamp> _lazyLatestDocumentTopLevelChangeVersion;
        private AnalyzerOptions _analyzerOptions;

        private ProjectState(
            ProjectInfo projectInfo,
            HostLanguageServices languageServices,
            SolutionServices solutionServices,
            IEnumerable<DocumentId> documentIds,
            IEnumerable<DocumentId> additionalDocumentIds,
            ImmutableDictionary<DocumentId, DocumentState> documentStates,
            ImmutableDictionary<DocumentId, TextDocumentState> additionalDocumentStates,
            AsyncLazy<VersionStamp> lazyLatestDocumentVersion,
            AsyncLazy<VersionStamp> lazyLatestDocumentTopLevelChangeVersion)
        {
            _projectInfo = projectInfo;
            _solutionServices = solutionServices;
            _languageServices = languageServices;
            _documentIds = documentIds.ToImmutableReadOnlyListOrEmpty();
            _additionalDocumentIds = additionalDocumentIds.ToImmutableReadOnlyListOrEmpty();
            _documentStates = documentStates;
            _additionalDocumentStates = additionalDocumentStates;
            _lazyLatestDocumentVersion = lazyLatestDocumentVersion;
            _lazyLatestDocumentTopLevelChangeVersion = lazyLatestDocumentTopLevelChangeVersion;
        }

        internal ProjectState(ProjectInfo projectInfo, HostLanguageServices languageServices, SolutionServices solutionServices)
        {
            Contract.ThrowIfNull(projectInfo);
            Contract.ThrowIfNull(languageServices);
            Contract.ThrowIfNull(solutionServices);

            _languageServices = languageServices;
            _solutionServices = solutionServices;

            _projectInfo = FixProjectInfo(projectInfo);

            _documentIds = _projectInfo.Documents.Select(d => d.Id).ToImmutableArray();
            _additionalDocumentIds = this.ProjectInfo.AdditionalDocuments.Select(d => d.Id).ToImmutableArray();

            var docStates = ImmutableDictionary.CreateRange<DocumentId, DocumentState>(
                _projectInfo.Documents.Select(d =>
                    new KeyValuePair<DocumentId, DocumentState>(d.Id,
                        CreateDocument(this.ProjectInfo, d, languageServices, solutionServices))));

            _documentStates = docStates;

            var additionalDocStates = ImmutableDictionary.CreateRange<DocumentId, TextDocumentState>(
                    _projectInfo.AdditionalDocuments.Select(d =>
                        new KeyValuePair<DocumentId, TextDocumentState>(d.Id, TextDocumentState.Create(d, solutionServices))));

            _additionalDocumentStates = additionalDocStates;

            _lazyLatestDocumentVersion = new AsyncLazy<VersionStamp>(c => ComputeLatestDocumentVersionAsync(docStates, additionalDocStates, c), cacheResult: true);
            _lazyLatestDocumentTopLevelChangeVersion = new AsyncLazy<VersionStamp>(c => ComputeLatestDocumentTopLevelChangeVersionAsync(docStates, additionalDocStates, c), cacheResult: true);
        }

        private ProjectInfo FixProjectInfo(ProjectInfo projectInfo)
        {
            if (projectInfo.CompilationOptions == null)
            {
                var compilationFactory = _languageServices.GetService<ICompilationFactoryService>();
                if (compilationFactory != null)
                {
                    projectInfo = projectInfo.WithCompilationOptions(compilationFactory.GetDefaultCompilationOptions());
                }
            }

            if (projectInfo.ParseOptions == null)
            {
                var syntaxTreeFactory = _languageServices.GetService<ISyntaxTreeFactoryService>();
                if (syntaxTreeFactory != null)
                {
                    projectInfo = projectInfo.WithParseOptions(syntaxTreeFactory.GetDefaultParseOptions());
                }
            }

            return projectInfo;
        }

        private static async Task<VersionStamp> ComputeLatestDocumentVersionAsync(ImmutableDictionary<DocumentId, DocumentState> documentStates, ImmutableDictionary<DocumentId, TextDocumentState> additionalDocumentStates, CancellationToken cancellationToken)
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

            foreach (var additionalDoc in additionalDocumentStates.Values)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var version = await additionalDoc.GetTextVersionAsync(cancellationToken).ConfigureAwait(false);
                latestVersion = version.GetNewerVersion(latestVersion);
            }

            return latestVersion;
        }

        private AsyncLazy<VersionStamp> CreateLazyLatestDocumentTopLevelChangeVersion(
            TextDocumentState newDocument,
            ImmutableDictionary<DocumentId, DocumentState> newDocumentStates,
            ImmutableDictionary<DocumentId, TextDocumentState> newAdditionalDocumentStates)
        {
            VersionStamp oldVersion;
            if (_lazyLatestDocumentTopLevelChangeVersion.TryGetValue(out oldVersion))
            {
                return new AsyncLazy<VersionStamp>(c => ComputeTopLevelChangeTextVersionAsync(oldVersion, newDocument, c), cacheResult: true);
            }
            else
            {
                return new AsyncLazy<VersionStamp>(c => ComputeLatestDocumentTopLevelChangeVersionAsync(newDocumentStates, newAdditionalDocumentStates, c), cacheResult: true);
            }
        }

        private static async Task<VersionStamp> ComputeTopLevelChangeTextVersionAsync(VersionStamp oldVersion, TextDocumentState newDocument, CancellationToken cancellationToken)
        {
            var newVersion = await newDocument.GetTopLevelChangeTextVersionAsync(cancellationToken).ConfigureAwait(false);
            return newVersion.GetNewerVersion(oldVersion);
        }

        private static async Task<VersionStamp> ComputeLatestDocumentTopLevelChangeVersionAsync(ImmutableDictionary<DocumentId, DocumentState> documentStates, ImmutableDictionary<DocumentId, TextDocumentState> additionalDocumentStates, CancellationToken cancellationToken)
        {
            // this may produce a version that is out of sync with the actual Document versions.
            var latestVersion = VersionStamp.Default;
            foreach (var doc in documentStates.Values)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var version = await doc.GetTopLevelChangeTextVersionAsync(cancellationToken).ConfigureAwait(false);
                latestVersion = version.GetNewerVersion(latestVersion);
            }

            foreach (var additionalDoc in additionalDocumentStates.Values)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var version = await additionalDoc.GetTopLevelChangeTextVersionAsync(cancellationToken).ConfigureAwait(false);
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
            get { return _languageServices; }
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

        public AnalyzerOptions AnalyzerOptions
        {
            get
            {
                if (_analyzerOptions == null)
                {
                    _analyzerOptions = new AnalyzerOptions(_additionalDocumentStates.Values.Select(d => new AdditionalTextDocument(d)).ToImmutableArray<AdditionalText>());
                }

                return _analyzerOptions;
            }
        }

        private static AnalyzerOptions CreateAnalyzerOptions(ImmutableDictionary<DocumentId, TextDocumentState> additionalDocStates)
        {
            return new AnalyzerOptions(additionalDocStates.Values.Select(d => new AdditionalTextDocument(d)).ToImmutableArray<AdditionalText>());
        }

        public Task<VersionStamp> GetLatestDocumentVersionAsync(CancellationToken cancellationToken)
        {
            return _lazyLatestDocumentVersion.GetValueAsync(cancellationToken);
        }

        public Task<VersionStamp> GetLatestDocumentTopLevelChangeVersionAsync(CancellationToken cancellationToken)
        {
            return _lazyLatestDocumentTopLevelChangeVersion.GetValueAsync(cancellationToken);
        }

        public async Task<VersionStamp> GetSemanticVersionAsync(CancellationToken cancellationToken = default(CancellationToken))
        {
            var docVersion = await this.GetLatestDocumentTopLevelChangeVersionAsync(cancellationToken).ConfigureAwait(false);
            return docVersion.GetNewerVersion(this.Version);
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Collapsed)]
        public ProjectInfo ProjectInfo
        {
            get { return _projectInfo; }
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
        public IReadOnlyList<MetadataReference> MetadataReferences
        {
            get { return this.ProjectInfo.MetadataReferences; }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Collapsed)]
        public IReadOnlyList<AnalyzerReference> AnalyzerReferences
        {
            get { return this.ProjectInfo.AnalyzerReferences; }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Collapsed)]
        public IReadOnlyList<ProjectReference> ProjectReferences
        {
            get { return this.ProjectInfo.ProjectReferences; }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Collapsed)]
        public bool HasDocuments
        {
            get { return _documentIds.Count > 0; }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Collapsed)]
        public IEnumerable<DocumentState> OrderedDocumentStates
        {
            get { return this.DocumentIds.Select(GetDocumentState); }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Collapsed)]
        public IReadOnlyList<DocumentId> DocumentIds
        {
            get { return _documentIds; }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Collapsed)]
        public IReadOnlyList<DocumentId> AdditionalDocumentIds
        {
            get { return _additionalDocumentIds; }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Collapsed)]
        private ImmutableDictionary<DocumentId, DocumentState> DocumentStates
        {
            get { return _documentStates; }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Collapsed)]
        private ImmutableDictionary<DocumentId, TextDocumentState> AdditionalDocumentStates
        {
            get { return _additionalDocumentStates; }
        }

        public bool ContainsDocument(DocumentId documentId)
        {
            return _documentStates.ContainsKey(documentId);
        }

        public bool ContainsAdditionalDocument(DocumentId documentId)
        {
            return _additionalDocumentStates.ContainsKey(documentId);
        }

        public DocumentState GetDocumentState(DocumentId documentId)
        {
            DocumentState state;
            _documentStates.TryGetValue(documentId, out state);
            return state;
        }

        public TextDocumentState GetAdditionalDocumentState(DocumentId documentId)
        {
            TextDocumentState state;
            _additionalDocumentStates.TryGetValue(documentId, out state);
            return state;
        }

        private ProjectState With(
            ProjectInfo projectInfo = null,
            ImmutableArray<DocumentId> documentIds = default(ImmutableArray<DocumentId>),
            ImmutableArray<DocumentId> additionalDocumentIds = default(ImmutableArray<DocumentId>),
            ImmutableDictionary<DocumentId, DocumentState> documentStates = null,
            ImmutableDictionary<DocumentId, TextDocumentState> additionalDocumentStates = null,
            AsyncLazy<VersionStamp> latestDocumentVersion = null,
            AsyncLazy<VersionStamp> latestDocumentTopLevelChangeVersion = null)
        {
            return new ProjectState(
                projectInfo ?? _projectInfo,
                _languageServices,
                _solutionServices,
                documentIds.IsDefault ? _documentIds : documentIds,
                additionalDocumentIds.IsDefault ? _additionalDocumentIds : additionalDocumentIds,
                documentStates ?? _documentStates,
                additionalDocumentStates ?? _additionalDocumentStates,
                latestDocumentVersion ?? _lazyLatestDocumentVersion,
                latestDocumentTopLevelChangeVersion ?? _lazyLatestDocumentTopLevelChangeVersion);
        }

        public ProjectState UpdateName(string name)
        {
            if (name == this.Name)
            {
                return this;
            }

            return this.With(projectInfo: this.ProjectInfo.WithName(name).WithVersion(this.Version.GetNewerVersion()));
        }

        public ProjectState UpdateFilePath(string filePath)
        {
            if (filePath == this.FilePath)
            {
                return this;
            }

            return this.With(projectInfo: this.ProjectInfo.WithFilePath(filePath).WithVersion(this.Version.GetNewerVersion()));
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
            var docMap = _documentStates;

            foreach (var docId in _documentStates.Keys)
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
                projectInfo: this.ProjectInfo.WithProjectReferences(this.ProjectReferences.ToImmutableArray().Add(projectReference)).WithVersion(this.Version.GetNewerVersion()));
        }

        public ProjectState RemoveProjectReference(ProjectReference projectReference)
        {
            Contract.Requires(this.ProjectReferences.Contains(projectReference));

            return this.With(
                projectInfo: this.ProjectInfo.WithProjectReferences(this.ProjectReferences.ToImmutableArray().Remove(projectReference)).WithVersion(this.Version.GetNewerVersion()));
        }

        public ProjectState AddProjectReferences(IEnumerable<ProjectReference> projectReferences)
        {
            var newProjectRefs = this.ProjectReferences;
            foreach (var projectReference in projectReferences)
            {
                Contract.Requires(!newProjectRefs.Contains(projectReference));
                newProjectRefs = newProjectRefs.ToImmutableArray().Add(projectReference);
            }

            return this.With(
                projectInfo: this.ProjectInfo.WithProjectReferences(newProjectRefs).WithVersion(this.Version.GetNewerVersion()));
        }

        public ProjectState WithProjectReferences(IEnumerable<ProjectReference> projectReferences)
        {
            return this.With(
                projectInfo: this.ProjectInfo.WithProjectReferences(projectReferences).WithVersion(this.Version.GetNewerVersion()));
        }

        public ProjectState AddMetadataReference(MetadataReference toMetadata)
        {
            Contract.Requires(!this.MetadataReferences.Contains(toMetadata));

            return this.With(
                projectInfo: this.ProjectInfo.WithMetadataReferences(this.MetadataReferences.ToImmutableArray().Add(toMetadata)).WithVersion(this.Version.GetNewerVersion()));
        }

        public ProjectState RemoveMetadataReference(MetadataReference toMetadata)
        {
            Contract.Requires(this.MetadataReferences.Contains(toMetadata));

            return this.With(
                projectInfo: this.ProjectInfo.WithMetadataReferences(this.MetadataReferences.ToImmutableArray().Remove(toMetadata)).WithVersion(this.Version.GetNewerVersion()));
        }

        public ProjectState AddMetadataReferences(IEnumerable<MetadataReference> metadataReferences)
        {
            var newMetaRefs = this.MetadataReferences;
            foreach (var metadataReference in metadataReferences)
            {
                Contract.Requires(!newMetaRefs.Contains(metadataReference));
                newMetaRefs = newMetaRefs.ToImmutableArray().Add(metadataReference);
            }

            return this.With(
                projectInfo: this.ProjectInfo.WithMetadataReferences(newMetaRefs).WithVersion(this.Version.GetNewerVersion()));
        }

        public ProjectState WithMetadataReferences(IEnumerable<MetadataReference> metadataReferences)
        {
            return this.With(
                projectInfo: this.ProjectInfo.WithMetadataReferences(metadataReferences).WithVersion(this.Version.GetNewerVersion()));
        }

        public ProjectState AddAnalyzerReference(AnalyzerReference analyzerReference)
        {
            Contract.Requires(!this.AnalyzerReferences.Contains(analyzerReference));

            return this.With(
                projectInfo: this.ProjectInfo.WithAnalyzerReferences(this.AnalyzerReferences.ToImmutableArray().Add(analyzerReference)).WithVersion(this.Version.GetNewerVersion()));
        }

        public ProjectState RemoveAnalyzerReference(AnalyzerReference analyzerReference)
        {
            Contract.Requires(this.AnalyzerReferences.Contains(analyzerReference));

            return this.With(
                projectInfo: this.ProjectInfo.WithAnalyzerReferences(this.AnalyzerReferences.ToImmutableArray().Remove(analyzerReference)).WithVersion(this.Version.GetNewerVersion()));
        }

        public ProjectState AddAnalyzerReferences(IEnumerable<AnalyzerReference> analyzerReferences)
        {
            var newAnalyzerReferences = this.AnalyzerReferences;
            foreach (var analyzerReference in analyzerReferences)
            {
                Contract.Requires(!newAnalyzerReferences.Contains(analyzerReference));
                newAnalyzerReferences = newAnalyzerReferences.ToImmutableArray().Add(analyzerReference);
            }

            return this.With(
                projectInfo: this.ProjectInfo.WithAnalyzerReferences(newAnalyzerReferences).WithVersion(this.Version.GetNewerVersion()));
        }

        public ProjectState WithAnalyzerReferences(IEnumerable<AnalyzerReference> analyzerReferences)
        {
            return this.With(
                projectInfo: this.ProjectInfo.WithAnalyzerReferences(analyzerReferences).WithVersion(this.Version.GetNewerVersion()));
        }

        public ProjectState AddDocument(DocumentState document)
        {
            Contract.Requires(!this.DocumentStates.ContainsKey(document.Id));

            return this.With(
                projectInfo: this.ProjectInfo.WithVersion(this.Version.GetNewerVersion()).WithDocuments(this.ProjectInfo.Documents.Concat(document.Info)),
                documentIds: this.DocumentIds.ToImmutableArray().Add(document.Id),
                documentStates: this.DocumentStates.Add(document.Id, document));
        }

        public ProjectState AddAdditionalDocument(TextDocumentState document)
        {
            Contract.Requires(!this.AdditionalDocumentStates.ContainsKey(document.Id));

            return this.With(
                projectInfo: this.ProjectInfo.WithVersion(this.Version.GetNewerVersion()).WithAdditionalDocuments(this.ProjectInfo.AdditionalDocuments.Concat(document.Info)),
                additionalDocumentIds: this.AdditionalDocumentIds.ToImmutableArray().Add(document.Id),
                additionalDocumentStates: this.AdditionalDocumentStates.Add(document.Id, document));
        }

        public ProjectState RemoveDocument(DocumentId documentId)
        {
            Contract.Requires(this.DocumentStates.ContainsKey(documentId));

            return this.With(
                projectInfo: this.ProjectInfo.WithVersion(this.Version.GetNewerVersion()).WithDocuments(this.ProjectInfo.Documents.Where(info => info.Id != documentId)),
                documentIds: this.DocumentIds.ToImmutableArray().Remove(documentId),
                documentStates: this.DocumentStates.Remove(documentId));
        }

        public ProjectState RemoveAdditionalDocument(DocumentId documentId)
        {
            Contract.Requires(this.AdditionalDocumentStates.ContainsKey(documentId));

            return this.With(
                projectInfo: this.ProjectInfo.WithVersion(this.Version.GetNewerVersion()).WithDocuments(this.ProjectInfo.AdditionalDocuments.Where(info => info.Id != documentId)),
                additionalDocumentIds: this.AdditionalDocumentIds.ToImmutableArray().Remove(documentId),
                additionalDocumentStates: this.AdditionalDocumentStates.Remove(documentId));
        }

        public ProjectState RemoveAllDocuments()
        {
            return this.With(
                projectInfo: this.ProjectInfo.WithVersion(this.Version.GetNewerVersion()).WithDocuments(SpecializedCollections.EmptyEnumerable<DocumentInfo>()),
                documentIds: ImmutableArray.Create<DocumentId>(),
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
                newDocumentStates, _additionalDocumentStates, oldDocument, newDocument, recalculateDependentVersions, textChanged,
                out dependentDocumentVersion, out dependentSemanticVersion);

            return this.With(
                documentStates: newDocumentStates,
                latestDocumentVersion: dependentDocumentVersion,
                latestDocumentTopLevelChangeVersion: dependentSemanticVersion);
        }

        public ProjectState UpdateAdditionalDocument(TextDocumentState newDocument, bool textChanged, bool recalculateDependentVersions)
        {
            Contract.Requires(this.ContainsAdditionalDocument(newDocument.Id));

            var oldDocument = this.GetAdditionalDocumentState(newDocument.Id);
            if (oldDocument == newDocument)
            {
                return this;
            }

            var newDocumentStates = this.AdditionalDocumentStates.SetItem(newDocument.Id, newDocument);

            AsyncLazy<VersionStamp> dependentDocumentVersion;
            AsyncLazy<VersionStamp> dependentSemanticVersion;
            GetLatestDependentVersions(
                _documentStates, newDocumentStates, oldDocument, newDocument, recalculateDependentVersions, textChanged,
                out dependentDocumentVersion, out dependentSemanticVersion);

            return this.With(
                additionalDocumentStates: newDocumentStates,
                latestDocumentVersion: dependentDocumentVersion,
                latestDocumentTopLevelChangeVersion: dependentSemanticVersion);
        }

        private void GetLatestDependentVersions(
            ImmutableDictionary<DocumentId, DocumentState> newDocumentStates,
            ImmutableDictionary<DocumentId, TextDocumentState> newAdditionalDocumentStates,
            TextDocumentState oldDocument, TextDocumentState newDocument,
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
                    if (!_lazyLatestDocumentVersion.TryGetValue(out documentVersion) || documentVersion == oldVersion)
                    {
                        recalculateDocumentVersion = true;
                    }

                    VersionStamp semanticVersion;
                    if (!_lazyLatestDocumentTopLevelChangeVersion.TryGetValue(out semanticVersion) || semanticVersion == oldVersion)
                    {
                        recalculateSemanticVersion = true;
                    }
                }
            }

            dependentDocumentVersion = recalculateDocumentVersion ?
                new AsyncLazy<VersionStamp>(c => ComputeLatestDocumentVersionAsync(newDocumentStates, newAdditionalDocumentStates, c), cacheResult: true) :
                textChanged ?
                    new AsyncLazy<VersionStamp>(newDocument.GetTextVersionAsync, cacheResult: true) :
                    _lazyLatestDocumentVersion;

            dependentSemanticVersion = recalculateSemanticVersion ?
                new AsyncLazy<VersionStamp>(c => ComputeLatestDocumentTopLevelChangeVersionAsync(newDocumentStates, newAdditionalDocumentStates, c), cacheResult: true) :
                textChanged ?
                    CreateLazyLatestDocumentTopLevelChangeVersion(newDocument, newDocumentStates, newAdditionalDocumentStates) :
                    _lazyLatestDocumentTopLevelChangeVersion;
        }
    }
}
