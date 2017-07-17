﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
using Microsoft.CodeAnalysis.Serialization;
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

        // Checksums for this solution state
        private readonly ValueSource<ProjectStateChecksums> _lazyChecksums;

        // this will be initialized lazily.
        private AnalyzerOptions _analyzerOptionsDoNotAccessDirectly;

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
            _solutionServices = solutionServices;
            _languageServices = languageServices;
            _documentIds = documentIds.ToImmutableReadOnlyListOrEmpty();
            _additionalDocumentIds = additionalDocumentIds.ToImmutableReadOnlyListOrEmpty();
            _documentStates = documentStates;
            _additionalDocumentStates = additionalDocumentStates;
            _lazyLatestDocumentVersion = lazyLatestDocumentVersion;
            _lazyLatestDocumentTopLevelChangeVersion = lazyLatestDocumentTopLevelChangeVersion;

            // ownership of information on document has moved to project state. clear out documentInfo the state is
            // holding on. otherwise, these information will be held onto unnecesarily by projectInfo even after
            // the info has changed by DocumentState.
            _projectInfo = ClearAllDocumentsFromProjectInfo(projectInfo);

            _lazyChecksums = new AsyncLazy<ProjectStateChecksums>(ComputeChecksumsAsync, cacheResult: true);
        }

        public ProjectState(ProjectInfo projectInfo, HostLanguageServices languageServices, SolutionServices solutionServices)
        {
            Contract.ThrowIfNull(projectInfo);
            Contract.ThrowIfNull(languageServices);
            Contract.ThrowIfNull(solutionServices);

            _languageServices = languageServices;
            _solutionServices = solutionServices;

            var projectInfoFixed = FixProjectInfo(projectInfo);

            _documentIds = projectInfoFixed.Documents.Select(d => d.Id).ToImmutableArray();
            _additionalDocumentIds = projectInfoFixed.AdditionalDocuments.Select(d => d.Id).ToImmutableArray();

            var parseOptions = projectInfoFixed.ParseOptions;
            var docStates = ImmutableDictionary.CreateRange<DocumentId, DocumentState>(
                projectInfoFixed.Documents.Select(d =>
                    new KeyValuePair<DocumentId, DocumentState>(d.Id,
                        CreateDocument(d, parseOptions, languageServices, solutionServices))));

            _documentStates = docStates;

            var additionalDocStates = ImmutableDictionary.CreateRange<DocumentId, TextDocumentState>(
                    projectInfoFixed.AdditionalDocuments.Select(d =>
                        new KeyValuePair<DocumentId, TextDocumentState>(d.Id, TextDocumentState.Create(d, solutionServices))));

            _additionalDocumentStates = additionalDocStates;

            _lazyLatestDocumentVersion = new AsyncLazy<VersionStamp>(c => ComputeLatestDocumentVersionAsync(docStates, additionalDocStates, c), cacheResult: true);
            _lazyLatestDocumentTopLevelChangeVersion = new AsyncLazy<VersionStamp>(c => ComputeLatestDocumentTopLevelChangeVersionAsync(docStates, additionalDocStates, c), cacheResult: true);

            // ownership of information on document has moved to project state. clear out documentInfo the state is
            // holding on. otherwise, these information will be held onto unnecesarily by projectInfo even after
            // the info has changed by DocumentState.
            // we hold onto the info so that we don't need to duplicate all information info already has in the state
            _projectInfo = ClearAllDocumentsFromProjectInfo(projectInfoFixed);

            _lazyChecksums = new AsyncLazy<ProjectStateChecksums>(ComputeChecksumsAsync, cacheResult: true);
        }

        private static ProjectInfo ClearAllDocumentsFromProjectInfo(ProjectInfo projectInfo)
        {
            return projectInfo.WithDocuments(ImmutableArray<DocumentInfo>.Empty).WithAdditionalDocuments(ImmutableArray<DocumentInfo>.Empty);
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
            foreach (var (_, doc) in documentStates)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (!doc.IsGenerated)
                {
                    var version = await doc.GetTextVersionAsync(cancellationToken).ConfigureAwait(false);
                    latestVersion = version.GetNewerVersion(latestVersion);
                }
            }

            foreach (var (_, additionalDoc) in additionalDocumentStates)
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
            if (_lazyLatestDocumentTopLevelChangeVersion.TryGetValue(out var oldVersion))
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
            foreach (var (_, doc) in documentStates)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var version = await doc.GetTopLevelChangeTextVersionAsync(cancellationToken).ConfigureAwait(false);
                latestVersion = version.GetNewerVersion(latestVersion);
            }

            foreach (var (_, additionalDoc) in additionalDocumentStates)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var version = await additionalDoc.GetTopLevelChangeTextVersionAsync(cancellationToken).ConfigureAwait(false);
                latestVersion = version.GetNewerVersion(latestVersion);
            }

            return latestVersion;
        }

        private static DocumentState CreateDocument(DocumentInfo documentInfo, ParseOptions parseOptions, HostLanguageServices languageServices, SolutionServices solutionServices)
        {
            var doc = DocumentState.Create(documentInfo, parseOptions, languageServices, solutionServices);

            if (doc.SourceCodeKind != documentInfo.SourceCodeKind)
            {
                doc = doc.UpdateSourceCodeKind(documentInfo.SourceCodeKind);
            }

            return doc;
        }

        public AnalyzerOptions AnalyzerOptions
        {
            get
            {
                if (_analyzerOptionsDoNotAccessDirectly == null)
                {
                    _analyzerOptionsDoNotAccessDirectly = new AnalyzerOptions(_additionalDocumentStates.Values.Select(d => new AdditionalTextDocument(d)).ToImmutableArray<AdditionalText>());
                }

                return _analyzerOptionsDoNotAccessDirectly;
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

        public async Task<VersionStamp> GetSemanticVersionAsync(CancellationToken cancellationToken = default)
        {
            var docVersion = await this.GetLatestDocumentTopLevelChangeVersionAsync(cancellationToken).ConfigureAwait(false);
            return docVersion.GetNewerVersion(this.Version);
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Collapsed)]
        public ProjectId Id => this.ProjectInfo.Id;

        [DebuggerBrowsable(DebuggerBrowsableState.Collapsed)]
        public string FilePath => this.ProjectInfo.FilePath;

        [DebuggerBrowsable(DebuggerBrowsableState.Collapsed)]
        public string OutputFilePath => this.ProjectInfo.OutputFilePath;

        [DebuggerBrowsable(DebuggerBrowsableState.Collapsed)]
        public HostLanguageServices LanguageServices => _languageServices;

        [DebuggerBrowsable(DebuggerBrowsableState.Collapsed)]
        public string Language => LanguageServices.Language;

        [DebuggerBrowsable(DebuggerBrowsableState.Collapsed)]
        public string Name => this.ProjectInfo.Name;

        [DebuggerBrowsable(DebuggerBrowsableState.Collapsed)]
        public bool IsSubmission => this.ProjectInfo.IsSubmission;

        [DebuggerBrowsable(DebuggerBrowsableState.Collapsed)]
        public Type HostObjectType => this.ProjectInfo.HostObjectType;

        [DebuggerBrowsable(DebuggerBrowsableState.Collapsed)]
        public bool SupportsCompilation => this.LanguageServices.GetService<ICompilationFactoryService>() != null;

        [DebuggerBrowsable(DebuggerBrowsableState.Collapsed)]
        public VersionStamp Version => this.ProjectInfo.Version;

        [DebuggerBrowsable(DebuggerBrowsableState.Collapsed)]
        public ProjectInfo ProjectInfo => _projectInfo;

        [DebuggerBrowsable(DebuggerBrowsableState.Collapsed)]
        public string AssemblyName => this.ProjectInfo.AssemblyName;

        [DebuggerBrowsable(DebuggerBrowsableState.Collapsed)]
        public CompilationOptions CompilationOptions => this.ProjectInfo.CompilationOptions;

        [DebuggerBrowsable(DebuggerBrowsableState.Collapsed)]
        public ParseOptions ParseOptions => this.ProjectInfo.ParseOptions;

        [DebuggerBrowsable(DebuggerBrowsableState.Collapsed)]
        public IReadOnlyList<MetadataReference> MetadataReferences => this.ProjectInfo.MetadataReferences;

        [DebuggerBrowsable(DebuggerBrowsableState.Collapsed)]
        public IReadOnlyList<AnalyzerReference> AnalyzerReferences => this.ProjectInfo.AnalyzerReferences;

        [DebuggerBrowsable(DebuggerBrowsableState.Collapsed)]
        public IReadOnlyList<ProjectReference> ProjectReferences => this.ProjectInfo.ProjectReferences;

        [DebuggerBrowsable(DebuggerBrowsableState.Collapsed)]
        public bool HasAllInformation => this.ProjectInfo.HasAllInformation;

        [DebuggerBrowsable(DebuggerBrowsableState.Collapsed)]
        public bool HasDocuments => _documentIds.Count > 0;

        [DebuggerBrowsable(DebuggerBrowsableState.Collapsed)]
        public IEnumerable<DocumentState> OrderedDocumentStates => this.DocumentIds.Select(GetDocumentState);

        [DebuggerBrowsable(DebuggerBrowsableState.Collapsed)]
        public IReadOnlyList<DocumentId> DocumentIds => _documentIds;

        [DebuggerBrowsable(DebuggerBrowsableState.Collapsed)]
        public IReadOnlyList<DocumentId> AdditionalDocumentIds => _additionalDocumentIds;

        [DebuggerBrowsable(DebuggerBrowsableState.Collapsed)]
        public ImmutableDictionary<DocumentId, DocumentState> DocumentStates => _documentStates;

        [DebuggerBrowsable(DebuggerBrowsableState.Collapsed)]
        public ImmutableDictionary<DocumentId, TextDocumentState> AdditionalDocumentStates => _additionalDocumentStates;

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
            _documentStates.TryGetValue(documentId, out var state);
            return state;
        }

        public TextDocumentState GetAdditionalDocumentState(DocumentId documentId)
        {
            _additionalDocumentStates.TryGetValue(documentId, out var state);
            return state;
        }

        private ProjectState With(
            ProjectInfo projectInfo = null,
            ImmutableArray<DocumentId> documentIds = default,
            ImmutableArray<DocumentId> additionalDocumentIds = default,
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

            foreach (var (docId, _) in _documentStates)
            {
                var oldDocState = this.GetDocumentState(docId);
                var newDocState = oldDocState.UpdateParseOptions(options);
                docMap = docMap.SetItem(docId, newDocState);
            }

            return this.With(
                projectInfo: this.ProjectInfo.WithParseOptions(options).WithVersion(this.Version.GetNewerVersion()),
                documentStates: docMap);
        }

        public ProjectState UpdateHasAllInformation(bool hasAllInformation)
        {
            if (hasAllInformation == this.HasAllInformation)
            {
                return this;
            }

            return this.With(projectInfo: this.ProjectInfo.WithHasAllInformation(hasAllInformation).WithVersion(this.Version.GetNewerVersion()));
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
                projectInfo: this.ProjectInfo.WithVersion(this.Version.GetNewerVersion()),
                documentIds: this.DocumentIds.ToImmutableArray().Add(document.Id),
                documentStates: this.DocumentStates.Add(document.Id, document));
        }

        public ProjectState AddAdditionalDocument(TextDocumentState document)
        {
            Contract.Requires(!this.AdditionalDocumentStates.ContainsKey(document.Id));

            return this.With(
                projectInfo: this.ProjectInfo.WithVersion(this.Version.GetNewerVersion()),
                additionalDocumentIds: this.AdditionalDocumentIds.ToImmutableArray().Add(document.Id),
                additionalDocumentStates: this.AdditionalDocumentStates.Add(document.Id, document));
        }

        public ProjectState RemoveDocument(DocumentId documentId)
        {
            Contract.Requires(this.DocumentStates.ContainsKey(documentId));

            return this.With(
                projectInfo: this.ProjectInfo.WithVersion(this.Version.GetNewerVersion()),
                documentIds: this.DocumentIds.ToImmutableArray().Remove(documentId),
                documentStates: this.DocumentStates.Remove(documentId));
        }

        public ProjectState RemoveAdditionalDocument(DocumentId documentId)
        {
            Contract.Requires(this.AdditionalDocumentStates.ContainsKey(documentId));

            return this.With(
                projectInfo: this.ProjectInfo.WithVersion(this.Version.GetNewerVersion()),
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
            GetLatestDependentVersions(
                newDocumentStates, _additionalDocumentStates, oldDocument, newDocument, recalculateDependentVersions, textChanged,
                out var dependentDocumentVersion, out var dependentSemanticVersion);

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
            GetLatestDependentVersions(
                _documentStates, newDocumentStates, oldDocument, newDocument, recalculateDependentVersions, textChanged,
                out var dependentDocumentVersion, out var dependentSemanticVersion);

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
                if (oldDocument.TryGetTextVersion(out var oldVersion))
                {
                    if (!_lazyLatestDocumentVersion.TryGetValue(out var documentVersion) || documentVersion == oldVersion)
                    {
                        recalculateDocumentVersion = true;
                    }

                    if (!_lazyLatestDocumentTopLevelChangeVersion.TryGetValue(out var semanticVersion) || semanticVersion == oldVersion)
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
