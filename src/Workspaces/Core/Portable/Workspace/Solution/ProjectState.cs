// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#nullable enable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.CompilerServices;
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

        /// <summary>
        /// The documents in this project. They are sorted by <see cref="DocumentId.Id"/> to provide a stable sort for
        /// <see cref="GetChecksumAsync(CancellationToken)"/>.
        /// </summary>
        private readonly ImmutableSortedDictionary<DocumentId, DocumentState> _documentStates;

        /// <summary>
        /// The additional documents in this project. They are sorted by <see cref="DocumentId.Id"/> to provide a stable sort for
        /// <see cref="GetChecksumAsync(CancellationToken)"/>.
        /// </summary>
        private readonly ImmutableSortedDictionary<DocumentId, TextDocumentState> _additionalDocumentStates;

        /// <summary>
        /// The analyzer config documents in this project.  They are sorted by <see cref="DocumentId.Id"/> to provide a stable sort for
        /// <see cref="GetChecksumAsync(CancellationToken)"/>.
        /// </summary>
        private readonly ImmutableSortedDictionary<DocumentId, AnalyzerConfigDocumentState> _analyzerConfigDocumentStates;

        private readonly ImmutableList<DocumentId> _documentIds;
        private readonly ImmutableList<DocumentId> _additionalDocumentIds;
        private readonly AsyncLazy<VersionStamp> _lazyLatestDocumentVersion;
        private readonly AsyncLazy<VersionStamp> _lazyLatestDocumentTopLevelChangeVersion;

        // Checksums for this solution state
        private readonly ValueSource<ProjectStateChecksums> _lazyChecksums;

        /// <summary>
        /// The <see cref="AnalyzerConfigSet"/> to be used for analyzer options for specific trees.
        /// </summary>
        private readonly ValueSource<AnalyzerConfigSet> _lazyAnalyzerConfigSet;

        // this will be initialized lazily.
        private AnalyzerOptions? _analyzerOptionsDoNotAccessDirectly;

        private ProjectState(
            ProjectInfo projectInfo,
            HostLanguageServices languageServices,
            SolutionServices solutionServices,
            ImmutableList<DocumentId> documentIds,
            ImmutableList<DocumentId> additionalDocumentIds,
            ImmutableSortedDictionary<DocumentId, DocumentState> documentStates,
            ImmutableSortedDictionary<DocumentId, TextDocumentState> additionalDocumentStates,
            ImmutableSortedDictionary<DocumentId, AnalyzerConfigDocumentState> analyzerConfigDocumentStates,
            AsyncLazy<VersionStamp> lazyLatestDocumentVersion,
            AsyncLazy<VersionStamp> lazyLatestDocumentTopLevelChangeVersion,
            ValueSource<AnalyzerConfigSet> lazyAnalyzerConfigSet)
        {
            _solutionServices = solutionServices;
            _languageServices = languageServices;
            _documentIds = documentIds;
            _additionalDocumentIds = additionalDocumentIds;
            _documentStates = documentStates;
            _additionalDocumentStates = additionalDocumentStates;
            _analyzerConfigDocumentStates = analyzerConfigDocumentStates;
            _lazyLatestDocumentVersion = lazyLatestDocumentVersion;
            _lazyLatestDocumentTopLevelChangeVersion = lazyLatestDocumentTopLevelChangeVersion;
            _lazyAnalyzerConfigSet = lazyAnalyzerConfigSet;

            // ownership of information on document has moved to project state. clear out documentInfo the state is
            // holding on. otherwise, these information will be held onto unnecessarily by projectInfo even after
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

            // We need to compute our AnalyerConfigDocumentStates first, since we use those to produce our DocumentStates
            _analyzerConfigDocumentStates = ImmutableSortedDictionary.CreateRange(DocumentIdComparer.Instance,
                projectInfoFixed.AnalyzerConfigDocuments.Select(d =>
                    KeyValuePairUtil.Create(d.Id, new AnalyzerConfigDocumentState(d, solutionServices))));
            _lazyAnalyzerConfigSet = ComputeAnalyzerConfigSetValueSource(_analyzerConfigDocumentStates.Values);

            _documentIds = projectInfoFixed.Documents.Select(d => d.Id).ToImmutableList();
            _additionalDocumentIds = projectInfoFixed.AdditionalDocuments.Select(d => d.Id).ToImmutableList();

            var parseOptions = projectInfoFixed.ParseOptions;
            var docStates = ImmutableSortedDictionary.CreateRange(DocumentIdComparer.Instance,
                projectInfoFixed.Documents.Select(d =>
                    new KeyValuePair<DocumentId, DocumentState>(d.Id,
                        CreateDocument(d, parseOptions))));

            _documentStates = docStates;

            var additionalDocStates = ImmutableSortedDictionary.CreateRange(DocumentIdComparer.Instance,
                    projectInfoFixed.AdditionalDocuments.Select(d =>
                        new KeyValuePair<DocumentId, TextDocumentState>(d.Id, new TextDocumentState(d, solutionServices))));

            _additionalDocumentStates = additionalDocStates;
            _lazyLatestDocumentVersion = new AsyncLazy<VersionStamp>(c => ComputeLatestDocumentVersionAsync(docStates, additionalDocStates, c), cacheResult: true);
            _lazyLatestDocumentTopLevelChangeVersion = new AsyncLazy<VersionStamp>(c => ComputeLatestDocumentTopLevelChangeVersionAsync(docStates, additionalDocStates, c), cacheResult: true);

            // ownership of information on document has moved to project state. clear out documentInfo the state is
            // holding on. otherwise, these information will be held onto unnecessarily by projectInfo even after
            // the info has changed by DocumentState.
            // we hold onto the info so that we don't need to duplicate all information info already has in the state
            _projectInfo = ClearAllDocumentsFromProjectInfo(projectInfoFixed);

            _lazyChecksums = new AsyncLazy<ProjectStateChecksums>(ComputeChecksumsAsync, cacheResult: true);
        }

        private static ProjectInfo ClearAllDocumentsFromProjectInfo(ProjectInfo projectInfo)
        {
            return projectInfo
                .WithDocuments(ImmutableArray<DocumentInfo>.Empty)
                .WithAdditionalDocuments(ImmutableArray<DocumentInfo>.Empty)
                .WithAnalyzerConfigDocuments(ImmutableArray<DocumentInfo>.Empty);
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

        private static async Task<VersionStamp> ComputeLatestDocumentVersionAsync(IImmutableDictionary<DocumentId, DocumentState> documentStates, IImmutableDictionary<DocumentId, TextDocumentState> additionalDocumentStates, CancellationToken cancellationToken)
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
            IImmutableDictionary<DocumentId, DocumentState> newDocumentStates,
            IImmutableDictionary<DocumentId, TextDocumentState> newAdditionalDocumentStates)
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

        private static async Task<VersionStamp> ComputeLatestDocumentTopLevelChangeVersionAsync(IImmutableDictionary<DocumentId, DocumentState> documentStates, IImmutableDictionary<DocumentId, TextDocumentState> additionalDocumentStates, CancellationToken cancellationToken)
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

        internal DocumentState CreateDocument(DocumentInfo documentInfo, ParseOptions? parseOptions)
        {
            var doc = new DocumentState(documentInfo, parseOptions, _lazyAnalyzerConfigSet, _languageServices, _solutionServices);

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
                    _analyzerOptionsDoNotAccessDirectly = new AnalyzerOptions(
                        _additionalDocumentStates.Values.Select(d => new AdditionalTextWithState(d)).ToImmutableArray<AdditionalText>(),
                        new WorkspaceAnalyzerConfigOptionsProvider(this));
                }

                return _analyzerOptionsDoNotAccessDirectly;
            }
        }

        public ImmutableDictionary<string, ReportDiagnostic> GetAnalyzerConfigSpecialDiagnosticOptions()
        {
            // We need to find the analyzer config options at the root of the project.
            // Currently, there is no compiler API to query analyzer config options for a directory in a language agnostic fashion.
            // So, we use a dummy language-specific file name appended to the project directory to query analyzer config options.

            var projectDirectory = PathUtilities.GetDirectoryName(_projectInfo.FilePath);
            if (!PathUtilities.IsAbsolute(projectDirectory))
            {
                return ImmutableDictionary<string, ReportDiagnostic>.Empty;
            }

            var fileName = Guid.NewGuid().ToString();
            string sourceFilePath;
            switch (_projectInfo.Language)
            {
                case LanguageNames.CSharp:
                    sourceFilePath = PathUtilities.CombineAbsoluteAndRelativePaths(projectDirectory, $"{fileName}.cs");
                    break;

                case LanguageNames.VisualBasic:
                    sourceFilePath = PathUtilities.CombineAbsoluteAndRelativePaths(projectDirectory, $"{fileName}.vb");
                    break;

                default:
                    return ImmutableDictionary<string, ReportDiagnostic>.Empty;
            }

            return _lazyAnalyzerConfigSet.GetValue(CancellationToken.None).GetOptionsForSourcePath(sourceFilePath).TreeOptions;
        }

        private sealed class WorkspaceAnalyzerConfigOptionsProvider : AnalyzerConfigOptionsProvider
        {
            private readonly ProjectState _projectState;

            public WorkspaceAnalyzerConfigOptionsProvider(ProjectState projectState)
            {
                _projectState = projectState;
            }

            public override AnalyzerConfigOptions GetOptions(SyntaxTree tree)
            {
                return new WorkspaceAnalyzerConfigOptions(_projectState._lazyAnalyzerConfigSet.GetValue(CancellationToken.None).GetOptionsForSourcePath(tree.FilePath));
            }

            public override AnalyzerConfigOptions GetOptions(AdditionalText textFile)
            {
                // TODO: correctly find the file path, since it looks like we give this the document's .Name under the covers if we don't have one
                return new WorkspaceAnalyzerConfigOptions(_projectState._lazyAnalyzerConfigSet.GetValue(CancellationToken.None).GetOptionsForSourcePath(textFile.Path));
            }

            // PROTOTYPE: why isn't this just a provided implementation?
            private sealed class WorkspaceAnalyzerConfigOptions : AnalyzerConfigOptions
            {
                private readonly ImmutableDictionary<string, string> _analyzerOptions;

                public WorkspaceAnalyzerConfigOptions(AnalyzerConfigOptionsResult analyzerConfigOptions)
                {
                    _analyzerOptions = analyzerConfigOptions.AnalyzerOptions;
                }

                public override bool TryGetValue(string key, [NotNullWhen(returnValue: true)] out string? value) => _analyzerOptions.TryGetValue(key, out value);
            }
        }

        private static ValueSource<AnalyzerConfigSet> ComputeAnalyzerConfigSetValueSource(IEnumerable<AnalyzerConfigDocumentState> analyzerConfigDocumentStates)
        {
            return new AsyncLazy<AnalyzerConfigSet>(
                asynchronousComputeFunction: async cancellationToken =>
                {
                    var tasks = analyzerConfigDocumentStates.Select(a => a.GetAnalyzerConfigAsync(cancellationToken));
                    var analyzerConfigs = await Task.WhenAll(tasks).ConfigureAwait(false);

                    cancellationToken.ThrowIfCancellationRequested();

                    return AnalyzerConfigSet.Create(analyzerConfigs);
                },
                synchronousComputeFunction: cancellationToken =>
                {
                    var analyzerConfigs = analyzerConfigDocumentStates.SelectAsArray(a => a.GetAnalyzerConfig(cancellationToken));
                    return AnalyzerConfigSet.Create(analyzerConfigs);
                },
                cacheResult: true);
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
        public string? FilePath => this.ProjectInfo.FilePath;

        [DebuggerBrowsable(DebuggerBrowsableState.Collapsed)]
        public string? OutputFilePath => this.ProjectInfo.OutputFilePath;

        [DebuggerBrowsable(DebuggerBrowsableState.Collapsed)]
        public string? OutputRefFilePath => this.ProjectInfo.OutputRefFilePath;

        [DebuggerBrowsable(DebuggerBrowsableState.Collapsed)]
        public string? DefaultNamespace => this.ProjectInfo.DefaultNamespace;

        [DebuggerBrowsable(DebuggerBrowsableState.Collapsed)]
        public HostLanguageServices LanguageServices => _languageServices;

        [DebuggerBrowsable(DebuggerBrowsableState.Collapsed)]
        public string Language => LanguageServices.Language;

        [DebuggerBrowsable(DebuggerBrowsableState.Collapsed)]
        public string Name => this.ProjectInfo.Name;

        [DebuggerBrowsable(DebuggerBrowsableState.Collapsed)]
        public bool IsSubmission => this.ProjectInfo.IsSubmission;

        [DebuggerBrowsable(DebuggerBrowsableState.Collapsed)]
        public Type? HostObjectType => this.ProjectInfo.HostObjectType;

        [DebuggerBrowsable(DebuggerBrowsableState.Collapsed)]
        public bool SupportsCompilation => this.LanguageServices.GetService<ICompilationFactoryService>() != null;

        [DebuggerBrowsable(DebuggerBrowsableState.Collapsed)]
        public VersionStamp Version => this.ProjectInfo.Version;

        [DebuggerBrowsable(DebuggerBrowsableState.Collapsed)]
        public ProjectInfo ProjectInfo => _projectInfo;

        [DebuggerBrowsable(DebuggerBrowsableState.Collapsed)]
        public string AssemblyName => this.ProjectInfo.AssemblyName;

        [DebuggerBrowsable(DebuggerBrowsableState.Collapsed)]
        public CompilationOptions? CompilationOptions => this.ProjectInfo.CompilationOptions;

        [DebuggerBrowsable(DebuggerBrowsableState.Collapsed)]
        public ParseOptions? ParseOptions => this.ProjectInfo.ParseOptions;

        [DebuggerBrowsable(DebuggerBrowsableState.Collapsed)]
        public IReadOnlyList<MetadataReference> MetadataReferences => this.ProjectInfo.MetadataReferences;

        [DebuggerBrowsable(DebuggerBrowsableState.Collapsed)]
        public IReadOnlyList<AnalyzerReference> AnalyzerReferences => this.ProjectInfo.AnalyzerReferences;

        [DebuggerBrowsable(DebuggerBrowsableState.Collapsed)]
        public IReadOnlyList<ProjectReference> ProjectReferences => this.ProjectInfo.ProjectReferences;

        [DebuggerBrowsable(DebuggerBrowsableState.Collapsed)]
        public bool HasAllInformation => this.ProjectInfo.HasAllInformation;

        [DebuggerBrowsable(DebuggerBrowsableState.Collapsed)]
        public bool RunAnalyzers => this.ProjectInfo.RunAnalyzers;

        [DebuggerBrowsable(DebuggerBrowsableState.Collapsed)]
        public bool HasDocuments => _documentIds.Count > 0;

        [DebuggerBrowsable(DebuggerBrowsableState.Collapsed)]
        public IEnumerable<DocumentState> OrderedDocumentStates => this.DocumentIds.Select(GetDocumentState)!;

        [DebuggerBrowsable(DebuggerBrowsableState.Collapsed)]
        public IReadOnlyList<DocumentId> DocumentIds => _documentIds;

        [DebuggerBrowsable(DebuggerBrowsableState.Collapsed)]
        public IReadOnlyList<DocumentId> AdditionalDocumentIds => _additionalDocumentIds;

        [DebuggerBrowsable(DebuggerBrowsableState.Collapsed)]
        // Regular documents and additionald documents have an ordering, and so we maintain lists of the IDs in order; in the case of analyzerconfig documents,
        // we don't define a workspace ordering because they are ordered via fancier algorithms in the compiler based on directory depth.
        public IEnumerable<DocumentId> AnalyzerConfigDocumentIds => _analyzerConfigDocumentStates.Keys;

        [DebuggerBrowsable(DebuggerBrowsableState.Collapsed)]
        public IImmutableDictionary<DocumentId, DocumentState> DocumentStates => _documentStates;

        [DebuggerBrowsable(DebuggerBrowsableState.Collapsed)]
        public IImmutableDictionary<DocumentId, TextDocumentState> AdditionalDocumentStates => _additionalDocumentStates;

        [DebuggerBrowsable(DebuggerBrowsableState.Collapsed)]
        public IImmutableDictionary<DocumentId, AnalyzerConfigDocumentState> AnalyzerConfigDocumentStates => _analyzerConfigDocumentStates;

        public bool ContainsDocument(DocumentId documentId)
        {
            return _documentStates.ContainsKey(documentId);
        }

        public bool ContainsAdditionalDocument(DocumentId documentId)
        {
            return _additionalDocumentStates.ContainsKey(documentId);
        }

        public bool ContainsAnalyzerConfigDocument(DocumentId documentId)
        {
            return _analyzerConfigDocumentStates.ContainsKey(documentId);
        }

        public DocumentState? GetDocumentState(DocumentId documentId)
        {
            _documentStates.TryGetValue(documentId, out var state);
            return state;
        }

        public TextDocumentState? GetAdditionalDocumentState(DocumentId documentId)
        {
            _additionalDocumentStates.TryGetValue(documentId, out var state);
            return state;
        }

        public AnalyzerConfigDocumentState? GetAnalyzerConfigDocumentState(DocumentId documentId)
        {
            _analyzerConfigDocumentStates.TryGetValue(documentId, out var state);
            return state;
        }

        private ProjectState With(
            ProjectInfo? projectInfo = null,
            ImmutableList<DocumentId>? documentIds = null,
            ImmutableList<DocumentId>? additionalDocumentIds = null,
            ImmutableSortedDictionary<DocumentId, DocumentState>? documentStates = null,
            ImmutableSortedDictionary<DocumentId, TextDocumentState>? additionalDocumentStates = null,
            ImmutableSortedDictionary<DocumentId, AnalyzerConfigDocumentState>? analyzerConfigDocumentStates = null,
            AsyncLazy<VersionStamp>? latestDocumentVersion = null,
            AsyncLazy<VersionStamp>? latestDocumentTopLevelChangeVersion = null,
            ValueSource<AnalyzerConfigSet>? analyzerConfigSet = null)
        {
            return new ProjectState(
                projectInfo ?? _projectInfo,
                _languageServices,
                _solutionServices,
                documentIds ?? _documentIds,
                additionalDocumentIds ?? _additionalDocumentIds,
                documentStates ?? _documentStates,
                additionalDocumentStates ?? _additionalDocumentStates,
                analyzerConfigDocumentStates ?? _analyzerConfigDocumentStates,
                latestDocumentVersion ?? _lazyLatestDocumentVersion,
                latestDocumentTopLevelChangeVersion ?? _lazyLatestDocumentTopLevelChangeVersion,
                analyzerConfigSet ?? _lazyAnalyzerConfigSet);
        }

        public ProjectState UpdateName(string name)
        {
            if (name == this.Name)
            {
                return this;
            }

            return this.With(projectInfo: this.ProjectInfo.WithName(name).WithVersion(this.Version.GetNewerVersion()));
        }

        public ProjectState UpdateFilePath(string? filePath)
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

        public ProjectState UpdateOutputFilePath(string? outputFilePath)
        {
            if (outputFilePath == this.OutputFilePath)
            {
                return this;
            }

            return this.With(projectInfo: this.ProjectInfo.WithOutputFilePath(outputFilePath).WithVersion(this.Version.GetNewerVersion()));
        }

        public ProjectState UpdateOutputRefFilePath(string? outputRefFilePath)
        {
            if (outputRefFilePath == this.OutputRefFilePath)
            {
                return this;
            }

            return this.With(projectInfo: this.ProjectInfo.WithOutputRefFilePath(outputRefFilePath).WithVersion(this.Version.GetNewerVersion()));
        }

        public ProjectState UpdateDefaultNamespace(string? defaultNamespace)
        {
            if (defaultNamespace == this.DefaultNamespace)
            {
                return this;
            }

            return this.With(projectInfo: this.ProjectInfo.WithDefaultNamespace(defaultNamespace).WithVersion(this.Version.GetNewerVersion()));
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

            foreach (var (docId, oldDocState) in _documentStates)
            {
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

        public ProjectState UpdateRunAnalyzers(bool runAnalyzers)
        {
            if (runAnalyzers == this.RunAnalyzers)
            {
                return this;
            }

            return this.With(projectInfo: this.ProjectInfo.WithRunAnalyzers(runAnalyzers).WithVersion(this.Version.GetNewerVersion()));
        }

        public static bool IsSameLanguage(ProjectState project1, ProjectState project2)
        {
            return project1.LanguageServices == project2.LanguageServices;
        }

        public ProjectState RemoveProjectReference(ProjectReference projectReference)
        {
            Debug.Assert(this.ProjectReferences.Contains(projectReference));

            return this.With(
                projectInfo: this.ProjectInfo.WithProjectReferences(this.ProjectReferences.ToImmutableArray().Remove(projectReference)).WithVersion(this.Version.GetNewerVersion()));
        }

        public ProjectState AddProjectReferences(IEnumerable<ProjectReference> projectReferences)
        {
            var newProjectRefs = this.ProjectReferences;
            foreach (var projectReference in projectReferences)
            {
                Debug.Assert(!newProjectRefs.Contains(projectReference));
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
            Debug.Assert(!this.MetadataReferences.Contains(toMetadata));

            return this.With(
                projectInfo: this.ProjectInfo.WithMetadataReferences(this.MetadataReferences.ToImmutableArray().Add(toMetadata)).WithVersion(this.Version.GetNewerVersion()));
        }

        public ProjectState RemoveMetadataReference(MetadataReference toMetadata)
        {
            Debug.Assert(this.MetadataReferences.Contains(toMetadata));

            return this.With(
                projectInfo: this.ProjectInfo.WithMetadataReferences(this.MetadataReferences.ToImmutableArray().Remove(toMetadata)).WithVersion(this.Version.GetNewerVersion()));
        }

        public ProjectState AddMetadataReferences(IEnumerable<MetadataReference> metadataReferences)
        {
            var newMetaRefs = this.MetadataReferences;
            foreach (var metadataReference in metadataReferences)
            {
                Debug.Assert(!newMetaRefs.Contains(metadataReference));
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
            Debug.Assert(!this.AnalyzerReferences.Contains(analyzerReference));

            return this.With(
                projectInfo: this.ProjectInfo.WithAnalyzerReferences(this.AnalyzerReferences.ToImmutableArray().Add(analyzerReference)).WithVersion(this.Version.GetNewerVersion()));
        }

        public ProjectState RemoveAnalyzerReference(AnalyzerReference analyzerReference)
        {
            Debug.Assert(this.AnalyzerReferences.Contains(analyzerReference));

            return this.With(
                projectInfo: this.ProjectInfo.WithAnalyzerReferences(this.AnalyzerReferences.ToImmutableArray().Remove(analyzerReference)).WithVersion(this.Version.GetNewerVersion()));
        }

        public ProjectState AddAnalyzerReferences(IEnumerable<AnalyzerReference> analyzerReferences)
        {
            var newAnalyzerReferences = this.AnalyzerReferences;
            foreach (var analyzerReference in analyzerReferences)
            {
                Debug.Assert(!newAnalyzerReferences.Contains(analyzerReference));
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

        public ProjectState AddDocuments(ImmutableArray<DocumentState> documents)
        {
            Debug.Assert(!documents.Any(d => this.DocumentStates.ContainsKey(d.Id)));

            return this.With(
                projectInfo: this.ProjectInfo.WithVersion(this.Version.GetNewerVersion()),
                documentIds: _documentIds.AddRange(documents.Select(d => d.Id)),
                documentStates: _documentStates.AddRange(documents.Select(d => KeyValuePairUtil.Create(d.Id, d))));
        }

        public ProjectState AddAdditionalDocuments(ImmutableArray<TextDocumentState> documents)
        {
            Debug.Assert(!documents.Any(d => this.AdditionalDocumentStates.ContainsKey(d.Id)));

            return this.With(
                projectInfo: this.ProjectInfo.WithVersion(this.Version.GetNewerVersion()),
                additionalDocumentIds: _additionalDocumentIds.AddRange(documents.Select(d => d.Id)),
                additionalDocumentStates: _additionalDocumentStates.AddRange(documents.Select(d => KeyValuePairUtil.Create(d.Id, d))));
        }

        public ProjectState AddAnalyzerConfigDocuments(ImmutableArray<AnalyzerConfigDocumentState> documents)
        {
            Debug.Assert(!documents.Any(d => this._analyzerConfigDocumentStates.ContainsKey(d.Id)));

            var newAnalyzerConfigDocumentStates = _analyzerConfigDocumentStates.AddRange(documents.Select(d => KeyValuePairUtil.Create(d.Id, d)));

            return CreateNewStateForChangedAnalyzerConfigDocuments(newAnalyzerConfigDocumentStates);
        }

        private ProjectState CreateNewStateForChangedAnalyzerConfigDocuments(ImmutableSortedDictionary<DocumentId, AnalyzerConfigDocumentState> newAnalyzerConfigDocumentStates)
        {
            var newAnalyzerConfigSet = ComputeAnalyzerConfigSetValueSource(newAnalyzerConfigDocumentStates.Values);

            // The addition of any .editorconfig can modify the diagnostic reporting options that are on
            // a specific syntax tree; therefore we must update all our syntax trees.
            var docMap = _documentStates;

            foreach (var (docId, oldDocState) in _documentStates)
            {
                var newDocState = oldDocState.UpdateAnalyzerConfigSet(newAnalyzerConfigSet);
                docMap = docMap.SetItem(docId, newDocState);
            }

            return this.With(
                projectInfo: this.ProjectInfo.WithVersion(this.Version.GetNewerVersion()),
                analyzerConfigDocumentStates: newAnalyzerConfigDocumentStates,
                documentStates: docMap,
                analyzerConfigSet: newAnalyzerConfigSet);
        }

        public ProjectState RemoveDocument(DocumentId documentId)
        {
            Debug.Assert(this.DocumentStates.ContainsKey(documentId));

            return this.With(
                projectInfo: this.ProjectInfo.WithVersion(this.Version.GetNewerVersion()),
                documentIds: _documentIds.Remove(documentId),
                documentStates: _documentStates.Remove(documentId));
        }

        public ProjectState RemoveAdditionalDocument(DocumentId documentId)
        {
            Debug.Assert(this.AdditionalDocumentStates.ContainsKey(documentId));

            return this.With(
                projectInfo: this.ProjectInfo.WithVersion(this.Version.GetNewerVersion()),
                additionalDocumentIds: _additionalDocumentIds.Remove(documentId),
                additionalDocumentStates: _additionalDocumentStates.Remove(documentId));
        }

        public ProjectState RemoveAnalyzerConfigDocument(DocumentId documentId)
        {
            Debug.Assert(_analyzerConfigDocumentStates.ContainsKey(documentId));

            var newAnalyzerConfigDocumentStates = _analyzerConfigDocumentStates.Remove(documentId);

            return CreateNewStateForChangedAnalyzerConfigDocuments(newAnalyzerConfigDocumentStates);
        }

        public ProjectState RemoveAllDocuments()
        {
            return this.With(
                projectInfo: this.ProjectInfo.WithVersion(this.Version.GetNewerVersion()).WithDocuments(SpecializedCollections.EmptyEnumerable<DocumentInfo>()),
                documentIds: ImmutableList<DocumentId>.Empty,
                documentStates: ImmutableSortedDictionary.Create<DocumentId, DocumentState>(DocumentIdComparer.Instance));
        }

        public ProjectState UpdateDocument(DocumentState newDocument, bool textChanged, bool recalculateDependentVersions)
        {
            Debug.Assert(this.ContainsDocument(newDocument.Id));

            var oldDocument = this.GetDocumentState(newDocument.Id)!;
            if (oldDocument == newDocument)
            {
                return this;
            }

            var newDocumentStates = _documentStates.SetItem(newDocument.Id, newDocument);
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
            Debug.Assert(this.ContainsAdditionalDocument(newDocument.Id));

            var oldDocument = this.GetAdditionalDocumentState(newDocument.Id)!;
            if (oldDocument == newDocument)
            {
                return this;
            }

            var newDocumentStates = _additionalDocumentStates.SetItem(newDocument.Id, newDocument);
            GetLatestDependentVersions(
                _documentStates, newDocumentStates, oldDocument, newDocument, recalculateDependentVersions, textChanged,
                out var dependentDocumentVersion, out var dependentSemanticVersion);

            return this.With(
                additionalDocumentStates: newDocumentStates,
                latestDocumentVersion: dependentDocumentVersion,
                latestDocumentTopLevelChangeVersion: dependentSemanticVersion);
        }

        public ProjectState UpdateAnalyzerConfigDocument(AnalyzerConfigDocumentState newDocument, bool textChanged, bool recalculateDependentVersions)
        {
            Debug.Assert(this.ContainsAnalyzerConfigDocument(newDocument.Id));

            var oldDocument = this.GetAnalyzerConfigDocumentState(newDocument.Id);
            if (oldDocument == newDocument)
            {
                return this;
            }

            var newDocumentStates = _analyzerConfigDocumentStates.SetItem(newDocument.Id, newDocument);

            return CreateNewStateForChangedAnalyzerConfigDocuments(newDocumentStates);
        }

        public ProjectState UpdateDocumentsOrder(ImmutableList<DocumentId> documentIds)
        {
            if (documentIds.IsEmpty)
            {
                throw new ArgumentOutOfRangeException("The specified documents are empty.", nameof(documentIds));
            }

            if (documentIds.Count != _documentIds.Count)
            {
                throw new ArgumentException($"The specified documents do not equal the project document count.", nameof(documentIds));
            }

            var hasOrderChanged = false;

            for (var i = 0; i < documentIds.Count; ++i)
            {
                var documentId = documentIds[i];

                if (!ContainsDocument(documentId))
                {
                    throw new InvalidOperationException($"The document '{documentId}' does not exist in the project.");
                }

                if (DocumentIds[i] != documentId)
                {
                    hasOrderChanged = true;
                }
            }

            if (!hasOrderChanged)
            {
                return this;
            }

            return this.With(
                projectInfo: this.ProjectInfo.WithVersion(this.Version.GetNewerVersion()),
                documentIds: documentIds);
        }

        private void GetLatestDependentVersions(
            IImmutableDictionary<DocumentId, DocumentState> newDocumentStates,
            IImmutableDictionary<DocumentId, TextDocumentState> newAdditionalDocumentStates,
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

        private sealed class DocumentIdComparer : IComparer<DocumentId>
        {
            public static IComparer<DocumentId> Instance = new DocumentIdComparer();

            private DocumentIdComparer()
            {
            }

            public int Compare(DocumentId x, DocumentId y)
            {
                return x.Id.CompareTo(y.Id);
            }
        }
    }
}
