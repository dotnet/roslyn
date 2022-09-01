// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
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

        /// <summary>
        /// The documents in this project. They are sorted by <see cref="DocumentId.Id"/> to provide a stable sort for
        /// <see cref="GetChecksumAsync(CancellationToken)"/>.
        /// </summary>
        public readonly TextDocumentStates<DocumentState> DocumentStates;

        /// <summary>
        /// The additional documents in this project. They are sorted by <see cref="DocumentId.Id"/> to provide a stable sort for
        /// <see cref="GetChecksumAsync(CancellationToken)"/>.
        /// </summary>
        public readonly TextDocumentStates<AdditionalDocumentState> AdditionalDocumentStates;

        /// <summary>
        /// The analyzer config documents in this project.  They are sorted by <see cref="DocumentId.Id"/> to provide a stable sort for
        /// <see cref="GetChecksumAsync(CancellationToken)"/>.
        /// </summary>
        public readonly TextDocumentStates<AnalyzerConfigDocumentState> AnalyzerConfigDocumentStates;

        private readonly AsyncLazy<VersionStamp> _lazyLatestDocumentVersion;
        private readonly AsyncLazy<VersionStamp> _lazyLatestDocumentTopLevelChangeVersion;

        // Checksums for this solution state
        private readonly ValueSource<ProjectStateChecksums> _lazyChecksums;

        /// <summary>
        /// Analyzer config options to be used for specific trees.
        /// </summary>
        private readonly ValueSource<AnalyzerConfigOptionsCache> _lazyAnalyzerConfigOptions;

        private AnalyzerOptions? _lazyAnalyzerOptions;

        /// <summary>
        /// Backing field for <see cref="SourceGenerators"/>; this is a default ImmutableArray if it hasn't been computed yet.
        /// </summary>
        private ImmutableArray<ISourceGenerator> _lazySourceGenerators;

        private ProjectState(
            ProjectInfo projectInfo,
            HostLanguageServices languageServices,
            SolutionServices solutionServices,
            TextDocumentStates<DocumentState> documentStates,
            TextDocumentStates<AdditionalDocumentState> additionalDocumentStates,
            TextDocumentStates<AnalyzerConfigDocumentState> analyzerConfigDocumentStates,
            AsyncLazy<VersionStamp> lazyLatestDocumentVersion,
            AsyncLazy<VersionStamp> lazyLatestDocumentTopLevelChangeVersion,
            ValueSource<AnalyzerConfigOptionsCache> lazyAnalyzerConfigSet)
        {
            _solutionServices = solutionServices;
            _languageServices = languageServices;
            DocumentStates = documentStates;
            AdditionalDocumentStates = additionalDocumentStates;
            AnalyzerConfigDocumentStates = analyzerConfigDocumentStates;
            _lazyLatestDocumentVersion = lazyLatestDocumentVersion;
            _lazyLatestDocumentTopLevelChangeVersion = lazyLatestDocumentTopLevelChangeVersion;
            _lazyAnalyzerConfigOptions = lazyAnalyzerConfigSet;

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
            AnalyzerConfigDocumentStates = new TextDocumentStates<AnalyzerConfigDocumentState>(projectInfoFixed.AnalyzerConfigDocuments, info => new AnalyzerConfigDocumentState(info, solutionServices));

            _lazyAnalyzerConfigOptions = ComputeAnalyzerConfigOptionsValueSource(AnalyzerConfigDocumentStates);

            // Add analyzer config information to the compilation options
            if (projectInfoFixed.CompilationOptions != null)
            {
                projectInfoFixed = projectInfoFixed.WithCompilationOptions(
                    projectInfoFixed.CompilationOptions.WithSyntaxTreeOptionsProvider(
                        new ProjectSyntaxTreeOptionsProvider(_lazyAnalyzerConfigOptions)));
            }

            var parseOptions = projectInfoFixed.ParseOptions;

            DocumentStates = new TextDocumentStates<DocumentState>(projectInfoFixed.Documents, info => CreateDocument(info, parseOptions));
            AdditionalDocumentStates = new TextDocumentStates<AdditionalDocumentState>(projectInfoFixed.AdditionalDocuments, info => new AdditionalDocumentState(info, solutionServices));

            _lazyLatestDocumentVersion = new AsyncLazy<VersionStamp>(c => ComputeLatestDocumentVersionAsync(DocumentStates, AdditionalDocumentStates, c), cacheResult: true);
            _lazyLatestDocumentTopLevelChangeVersion = new AsyncLazy<VersionStamp>(c => ComputeLatestDocumentTopLevelChangeVersionAsync(DocumentStates, AdditionalDocumentStates, c), cacheResult: true);

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

        private static async Task<VersionStamp> ComputeLatestDocumentVersionAsync(TextDocumentStates<DocumentState> documentStates, TextDocumentStates<AdditionalDocumentState> additionalDocumentStates, CancellationToken cancellationToken)
        {
            // this may produce a version that is out of sync with the actual Document versions.
            var latestVersion = VersionStamp.Default;
            foreach (var (_, state) in documentStates.States)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (!state.IsGenerated)
                {
                    var version = await state.GetTextVersionAsync(cancellationToken).ConfigureAwait(false);
                    latestVersion = version.GetNewerVersion(latestVersion);
                }
            }

            foreach (var (_, state) in additionalDocumentStates.States)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var version = await state.GetTextVersionAsync(cancellationToken).ConfigureAwait(false);
                latestVersion = version.GetNewerVersion(latestVersion);
            }

            return latestVersion;
        }

        private AsyncLazy<VersionStamp> CreateLazyLatestDocumentTopLevelChangeVersion(
            TextDocumentState newDocument,
            TextDocumentStates<DocumentState> newDocumentStates,
            TextDocumentStates<AdditionalDocumentState> newAdditionalDocumentStates)
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

        private static async Task<VersionStamp> ComputeLatestDocumentTopLevelChangeVersionAsync(TextDocumentStates<DocumentState> documentStates, TextDocumentStates<AdditionalDocumentState> additionalDocumentStates, CancellationToken cancellationToken)
        {
            // this may produce a version that is out of sync with the actual Document versions.
            var latestVersion = VersionStamp.Default;
            foreach (var (_, state) in documentStates.States)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var version = await state.GetTopLevelChangeTextVersionAsync(cancellationToken).ConfigureAwait(false);
                latestVersion = version.GetNewerVersion(latestVersion);
            }

            foreach (var (_, state) in additionalDocumentStates.States)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var version = await state.GetTopLevelChangeTextVersionAsync(cancellationToken).ConfigureAwait(false);
                latestVersion = version.GetNewerVersion(latestVersion);
            }

            return latestVersion;
        }

        internal DocumentState CreateDocument(DocumentInfo documentInfo, ParseOptions? parseOptions)
        {
            var doc = new DocumentState(documentInfo, parseOptions, _languageServices, _solutionServices);

            if (doc.SourceCodeKind != documentInfo.SourceCodeKind)
            {
                doc = doc.UpdateSourceCodeKind(documentInfo.SourceCodeKind);
            }

            return doc;
        }

        public AnalyzerOptions AnalyzerOptions
            => _lazyAnalyzerOptions ??= new AnalyzerOptions(
                additionalFiles: AdditionalDocumentStates.SelectAsArray(static documentState => documentState.AdditionalText),
                optionsProvider: new ProjectAnalyzerConfigOptionsProvider(this));

        public async Task<ImmutableDictionary<string, string>> GetAnalyzerOptionsForPathAsync(
            string path,
            CancellationToken cancellationToken)
        {
            var configSet = await _lazyAnalyzerConfigOptions.GetValueAsync(cancellationToken).ConfigureAwait(false);
            return configSet.GetOptionsForSourcePath(path).AnalyzerOptions;
        }

        public AnalyzerConfigOptionsResult? GetAnalyzerConfigOptions()
        {
            // We need to find the analyzer config options at the root of the project.
            // Currently, there is no compiler API to query analyzer config options for a directory in a language agnostic fashion.
            // So, we use a dummy language-specific file name appended to the project directory to query analyzer config options.

            var projectDirectory = PathUtilities.GetDirectoryName(_projectInfo.FilePath);
            if (!PathUtilities.IsAbsolute(projectDirectory))
            {
                return null;
            }

            var fileName = Guid.NewGuid().ToString();
            string sourceFilePath;
            switch (_projectInfo.Language)
            {
                case LanguageNames.CSharp:
                    // Suppression should be removed or addressed https://github.com/dotnet/roslyn/issues/41636
                    sourceFilePath = PathUtilities.CombineAbsoluteAndRelativePaths(projectDirectory, $"{fileName}.cs")!;
                    break;

                case LanguageNames.VisualBasic:
                    // Suppression should be removed or addressed https://github.com/dotnet/roslyn/issues/41636
                    sourceFilePath = PathUtilities.CombineAbsoluteAndRelativePaths(projectDirectory, $"{fileName}.vb")!;
                    break;

                default:
                    return null;
            }

            return _lazyAnalyzerConfigOptions.GetValue(CancellationToken.None).GetOptionsForSourcePath(sourceFilePath);
        }

        internal sealed class ProjectAnalyzerConfigOptionsProvider : AnalyzerConfigOptionsProvider
        {
            private readonly ProjectState _projectState;

            public ProjectAnalyzerConfigOptionsProvider(ProjectState projectState)
                => _projectState = projectState;

            public override AnalyzerConfigOptions GlobalOptions
                => GetOptionsForSourcePath(string.Empty);

            public override AnalyzerConfigOptions GetOptions(SyntaxTree tree)
                => GetOptionsForSourcePath(tree.FilePath);

            public override AnalyzerConfigOptions GetOptions(AdditionalText textFile)
            {
                // TODO: correctly find the file path, since it looks like we give this the document's .Name under the covers if we don't have one
                return GetOptionsForSourcePath(textFile.Path);
            }

            public AnalyzerConfigOptions GetOptionsForSourcePath(string path)
                => new DictionaryAnalyzerConfigOptions(_projectState._lazyAnalyzerConfigOptions.GetValue(CancellationToken.None).GetOptionsForSourcePath(path).AnalyzerOptions);
        }

        private sealed class ProjectSyntaxTreeOptionsProvider : SyntaxTreeOptionsProvider
        {
            private readonly ValueSource<AnalyzerConfigOptionsCache> _lazyAnalyzerConfigSet;

            public ProjectSyntaxTreeOptionsProvider(ValueSource<AnalyzerConfigOptionsCache> lazyAnalyzerConfigSet)
                => _lazyAnalyzerConfigSet = lazyAnalyzerConfigSet;

            public override GeneratedKind IsGenerated(SyntaxTree tree, CancellationToken cancellationToken)
            {
                var options = _lazyAnalyzerConfigSet
                    .GetValue(cancellationToken).GetOptionsForSourcePath(tree.FilePath);
                return GeneratedCodeUtilities.GetIsGeneratedCodeFromOptions(options.AnalyzerOptions);
            }

            public override bool TryGetDiagnosticValue(SyntaxTree tree, string diagnosticId, CancellationToken cancellationToken, out ReportDiagnostic severity)
            {
                var options = _lazyAnalyzerConfigSet
                    .GetValue(cancellationToken).GetOptionsForSourcePath(tree.FilePath);
                return options.TreeOptions.TryGetValue(diagnosticId, out severity);
            }

            public override bool TryGetGlobalDiagnosticValue(string diagnosticId, CancellationToken cancellationToken, out ReportDiagnostic severity)
            {
                var options = _lazyAnalyzerConfigSet
                    .GetValue(cancellationToken).GlobalConfigOptions;
                return options.TreeOptions.TryGetValue(diagnosticId, out severity);
            }

            public override bool Equals(object? obj)
            {
                return obj is ProjectSyntaxTreeOptionsProvider other
                    && _lazyAnalyzerConfigSet == other._lazyAnalyzerConfigSet;
            }

            public override int GetHashCode() => _lazyAnalyzerConfigSet.GetHashCode();
        }

        private static ValueSource<AnalyzerConfigOptionsCache> ComputeAnalyzerConfigOptionsValueSource(TextDocumentStates<AnalyzerConfigDocumentState> analyzerConfigDocumentStates)
        {
            return new AsyncLazy<AnalyzerConfigOptionsCache>(
                asynchronousComputeFunction: async cancellationToken =>
                {
                    var tasks = analyzerConfigDocumentStates.States.Values.Select(a => a.GetAnalyzerConfigAsync(cancellationToken));
                    var analyzerConfigs = await Task.WhenAll(tasks).ConfigureAwait(false);

                    cancellationToken.ThrowIfCancellationRequested();

                    return new AnalyzerConfigOptionsCache(AnalyzerConfigSet.Create(analyzerConfigs));
                },
                synchronousComputeFunction: cancellationToken =>
                {
                    var analyzerConfigs = analyzerConfigDocumentStates.SelectAsArray(a => a.GetAnalyzerConfig(cancellationToken));
                    return new AnalyzerConfigOptionsCache(AnalyzerConfigSet.Create(analyzerConfigs));
                },
                cacheResult: true);
        }

        private readonly struct AnalyzerConfigOptionsCache
        {
            private readonly ConcurrentDictionary<string, AnalyzerConfigOptionsResult> _sourcePathToResult = new();
            private readonly Func<string, AnalyzerConfigOptionsResult> _computeFunction;
            private readonly AnalyzerConfigSet _configSet;

            public AnalyzerConfigOptionsCache(AnalyzerConfigSet configSet)
            {
                _configSet = configSet;
                _computeFunction = _configSet.GetOptionsForSourcePath;
            }

            public AnalyzerConfigOptionsResult GlobalConfigOptions
                => _configSet.GlobalConfigOptions;

            public AnalyzerConfigOptionsResult GetOptionsForSourcePath(string sourcePath)
                => _sourcePathToResult.GetOrAdd(sourcePath, _computeFunction);
        }

        public Task<VersionStamp> GetLatestDocumentVersionAsync(CancellationToken cancellationToken)
            => _lazyLatestDocumentVersion.GetValueAsync(cancellationToken);

        public async Task<VersionStamp> GetSemanticVersionAsync(CancellationToken cancellationToken = default)
        {
            var docVersion = await _lazyLatestDocumentTopLevelChangeVersion.GetValueAsync(cancellationToken).ConfigureAwait(false);

            // This is unfortunate, however the impact of this is that *any* change to our project-state version will 
            // cause us to think the semantic version of the project has changed.  Thus, any change to a project property
            // that does *not* flow into the compiler still makes us think the semantic version has changed.  This is 
            // likely to not be too much of an issue as these changes should be rare, and it's better to be conservative
            // and assume there was a change than to wrongly presume there was not.
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
        public CompilationOutputInfo CompilationOutputInfo => this.ProjectInfo.CompilationOutputInfo;

        [DebuggerBrowsable(DebuggerBrowsableState.Collapsed)]
        public string? DefaultNamespace => this.ProjectInfo.DefaultNamespace;

        [DebuggerBrowsable(DebuggerBrowsableState.Collapsed)]
        public HostLanguageServices LanguageServices => _languageServices;

        [DebuggerBrowsable(DebuggerBrowsableState.Collapsed)]
        public string Language => LanguageServices.Language;

        [DebuggerBrowsable(DebuggerBrowsableState.Collapsed)]
        public string Name => this.ProjectInfo.Name;

        /// <inheritdoc cref="ProjectInfo.NameAndFlavor"/>
        [DebuggerBrowsable(DebuggerBrowsableState.Collapsed)]
        public (string? name, string? flavor) NameAndFlavor => this.ProjectInfo.NameAndFlavor;

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

        private ProjectState With(
            ProjectInfo? projectInfo = null,
            TextDocumentStates<DocumentState>? documentStates = null,
            TextDocumentStates<AdditionalDocumentState>? additionalDocumentStates = null,
            TextDocumentStates<AnalyzerConfigDocumentState>? analyzerConfigDocumentStates = null,
            AsyncLazy<VersionStamp>? latestDocumentVersion = null,
            AsyncLazy<VersionStamp>? latestDocumentTopLevelChangeVersion = null,
            ValueSource<AnalyzerConfigOptionsCache>? analyzerConfigSet = null)
        {
            return new ProjectState(
                projectInfo ?? _projectInfo,
                _languageServices,
                _solutionServices,
                documentStates ?? DocumentStates,
                additionalDocumentStates ?? AdditionalDocumentStates,
                analyzerConfigDocumentStates ?? AnalyzerConfigDocumentStates,
                latestDocumentVersion ?? _lazyLatestDocumentVersion,
                latestDocumentTopLevelChangeVersion ?? _lazyLatestDocumentTopLevelChangeVersion,
                analyzerConfigSet ?? _lazyAnalyzerConfigOptions);
        }

        private ProjectInfo.ProjectAttributes Attributes
            => ProjectInfo.Attributes;

        private ProjectState WithAttributes(ProjectInfo.ProjectAttributes attributes)
            => With(projectInfo: ProjectInfo.With(attributes: attributes));

        public ProjectState WithName(string name)
            => (name == Name) ? this : WithAttributes(Attributes.With(name: name, version: Version.GetNewerVersion()));

        public ProjectState WithFilePath(string? filePath)
            => (filePath == FilePath) ? this : WithAttributes(Attributes.With(filePath: filePath, version: Version.GetNewerVersion()));

        public ProjectState WithAssemblyName(string assemblyName)
            => (assemblyName == AssemblyName) ? this : WithAttributes(Attributes.With(assemblyName: assemblyName, version: Version.GetNewerVersion()));

        public ProjectState WithOutputFilePath(string? outputFilePath)
            => (outputFilePath == OutputFilePath) ? this : WithAttributes(Attributes.With(outputPath: outputFilePath, version: Version.GetNewerVersion()));

        public ProjectState WithOutputRefFilePath(string? outputRefFilePath)
            => (outputRefFilePath == OutputRefFilePath) ? this : WithAttributes(Attributes.With(outputRefPath: outputRefFilePath, version: Version.GetNewerVersion()));

        public ProjectState WithCompilationOutputInfo(in CompilationOutputInfo info)
            => (info == CompilationOutputInfo) ? this : WithAttributes(Attributes.With(compilationOutputInfo: info, version: Version.GetNewerVersion()));

        public ProjectState WithDefaultNamespace(string? defaultNamespace)
            => (defaultNamespace == DefaultNamespace) ? this : WithAttributes(Attributes.With(defaultNamespace: defaultNamespace, version: Version.GetNewerVersion()));

        public ProjectState WithHasAllInformation(bool hasAllInformation)
            => (hasAllInformation == HasAllInformation) ? this : WithAttributes(Attributes.With(hasAllInformation: hasAllInformation, version: Version.GetNewerVersion()));

        public ProjectState WithRunAnalyzers(bool runAnalyzers)
            => (runAnalyzers == RunAnalyzers) ? this : WithAttributes(Attributes.With(runAnalyzers: runAnalyzers, version: Version.GetNewerVersion()));

        public ProjectState WithCompilationOptions(CompilationOptions options)
        {
            if (options == CompilationOptions)
            {
                return this;
            }

            var newProvider = new ProjectSyntaxTreeOptionsProvider(_lazyAnalyzerConfigOptions);

            return With(projectInfo: ProjectInfo.WithCompilationOptions(options.WithSyntaxTreeOptionsProvider(newProvider))
                       .WithVersion(Version.GetNewerVersion()));
        }

        public ProjectState WithParseOptions(ParseOptions options)
        {
            if (options == ParseOptions)
            {
                return this;
            }

            var onlyPreprocessorDirectiveChange = ParseOptions != null &&
                _languageServices.SyntaxTreeFactory!.OptionsDifferOnlyByPreprocessorDirectives(options, ParseOptions);

            return With(
                projectInfo: ProjectInfo.WithParseOptions(options).WithVersion(Version.GetNewerVersion()),
                documentStates: DocumentStates.UpdateStates((state, options) => state.UpdateParseOptions(options, onlyPreprocessorDirectiveChange), options));
        }

        public static bool IsSameLanguage(ProjectState project1, ProjectState project2)
            => project1.LanguageServices == project2.LanguageServices;

        /// <summary>
        /// Determines whether <see cref="ProjectReferences"/> contains a reference to a specified project.
        /// </summary>
        /// <param name="projectId">The target project of the reference.</param>
        /// <returns><see langword="true"/> if this project references <paramref name="projectId"/>; otherwise, <see langword="false"/>.</returns>
        public bool ContainsReferenceToProject(ProjectId projectId)
        {
            foreach (var projectReference in ProjectReferences)
            {
                if (projectReference.ProjectId == projectId)
                    return true;
            }

            return false;
        }

        public ProjectState WithProjectReferences(IReadOnlyList<ProjectReference> projectReferences)
        {
            if (projectReferences == ProjectReferences)
            {
                return this;
            }

            return With(projectInfo: ProjectInfo.With(projectReferences: projectReferences).WithVersion(Version.GetNewerVersion()));
        }

        public ProjectState WithMetadataReferences(IReadOnlyList<MetadataReference> metadataReferences)
        {
            if (metadataReferences == MetadataReferences)
            {
                return this;
            }

            return With(projectInfo: ProjectInfo.With(metadataReferences: metadataReferences).WithVersion(Version.GetNewerVersion()));
        }

        public ProjectState WithAnalyzerReferences(IEnumerable<AnalyzerReference> analyzerReferences)
        {
            if (analyzerReferences == AnalyzerReferences)
            {
                return this;
            }

            return With(projectInfo: ProjectInfo.WithAnalyzerReferences(analyzerReferences).WithVersion(Version.GetNewerVersion()));
        }

        public ImmutableArray<ISourceGenerator> SourceGenerators
        {
            get
            {
                if (_lazySourceGenerators.IsDefault)
                {
                    var generators = AnalyzerReferences.SelectMany(a => a.GetGenerators(this.Language)).ToImmutableArray();
                    ImmutableInterlocked.InterlockedInitialize(ref _lazySourceGenerators, generators);
                }

                return _lazySourceGenerators;
            }
        }

        public ProjectState AddDocuments(ImmutableArray<DocumentState> documents)
        {
            Debug.Assert(!documents.Any(d => DocumentStates.Contains(d.Id)));

            return With(
                projectInfo: ProjectInfo.WithVersion(Version.GetNewerVersion()),
                documentStates: DocumentStates.AddRange(documents));
        }

        public ProjectState AddAdditionalDocuments(ImmutableArray<AdditionalDocumentState> documents)
        {
            Debug.Assert(!documents.Any(d => AdditionalDocumentStates.Contains(d.Id)));

            return With(
                projectInfo: ProjectInfo.WithVersion(Version.GetNewerVersion()),
                additionalDocumentStates: AdditionalDocumentStates.AddRange(documents));
        }

        public ProjectState AddAnalyzerConfigDocuments(ImmutableArray<AnalyzerConfigDocumentState> documents)
        {
            Debug.Assert(!documents.Any(d => AnalyzerConfigDocumentStates.Contains(d.Id)));

            var newAnalyzerConfigDocumentStates = AnalyzerConfigDocumentStates.AddRange(documents);

            return CreateNewStateForChangedAnalyzerConfigDocuments(newAnalyzerConfigDocumentStates);
        }

        private ProjectState CreateNewStateForChangedAnalyzerConfigDocuments(TextDocumentStates<AnalyzerConfigDocumentState> newAnalyzerConfigDocumentStates)
        {
            var newAnalyzerConfigSet = ComputeAnalyzerConfigOptionsValueSource(newAnalyzerConfigDocumentStates);
            var projectInfo = ProjectInfo.WithVersion(Version.GetNewerVersion());

            // Changing analyzer configs changes compilation options
            if (CompilationOptions != null)
            {
                var newProvider = new ProjectSyntaxTreeOptionsProvider(newAnalyzerConfigSet);
                projectInfo = projectInfo
                    .WithCompilationOptions(CompilationOptions.WithSyntaxTreeOptionsProvider(newProvider));
            }

            return With(
                projectInfo: projectInfo,
                analyzerConfigDocumentStates: newAnalyzerConfigDocumentStates,
                analyzerConfigSet: newAnalyzerConfigSet);
        }

        public ProjectState RemoveDocuments(ImmutableArray<DocumentId> documentIds)
        {
            // We create a new CachingAnalyzerConfigSet for the new snapshot to avoid holding onto cached information
            // for removed documents.
            return With(
                projectInfo: ProjectInfo.WithVersion(Version.GetNewerVersion()),
                documentStates: DocumentStates.RemoveRange(documentIds),
                analyzerConfigSet: ComputeAnalyzerConfigOptionsValueSource(AnalyzerConfigDocumentStates));
        }

        public ProjectState RemoveAdditionalDocuments(ImmutableArray<DocumentId> documentIds)
        {
            return With(
                projectInfo: ProjectInfo.WithVersion(Version.GetNewerVersion()),
                additionalDocumentStates: AdditionalDocumentStates.RemoveRange(documentIds));
        }

        public ProjectState RemoveAnalyzerConfigDocuments(ImmutableArray<DocumentId> documentIds)
        {
            var newAnalyzerConfigDocumentStates = AnalyzerConfigDocumentStates.RemoveRange(documentIds);

            return CreateNewStateForChangedAnalyzerConfigDocuments(newAnalyzerConfigDocumentStates);
        }

        public ProjectState RemoveAllDocuments()
        {
            // We create a new CachingAnalyzerConfigSet for the new snapshot to avoid holding onto cached information
            // for removed documents.
            return With(
                projectInfo: ProjectInfo.WithVersion(Version.GetNewerVersion()),
                documentStates: TextDocumentStates<DocumentState>.Empty,
                analyzerConfigSet: ComputeAnalyzerConfigOptionsValueSource(AnalyzerConfigDocumentStates));
        }

        public ProjectState UpdateDocument(DocumentState newDocument, bool textChanged, bool recalculateDependentVersions)
        {
            var oldDocument = DocumentStates.GetRequiredState(newDocument.Id);
            if (oldDocument == newDocument)
            {
                return this;
            }

            var newDocumentStates = DocumentStates.SetState(newDocument.Id, newDocument);
            GetLatestDependentVersions(
                newDocumentStates, AdditionalDocumentStates, oldDocument, newDocument, recalculateDependentVersions, textChanged,
                out var dependentDocumentVersion, out var dependentSemanticVersion);

            return With(
                documentStates: newDocumentStates,
                latestDocumentVersion: dependentDocumentVersion,
                latestDocumentTopLevelChangeVersion: dependentSemanticVersion);
        }

        public ProjectState UpdateAdditionalDocument(AdditionalDocumentState newDocument, bool textChanged, bool recalculateDependentVersions)
        {
            var oldDocument = AdditionalDocumentStates.GetRequiredState(newDocument.Id);
            if (oldDocument == newDocument)
            {
                return this;
            }

            var newDocumentStates = AdditionalDocumentStates.SetState(newDocument.Id, newDocument);
            GetLatestDependentVersions(
                DocumentStates, newDocumentStates, oldDocument, newDocument, recalculateDependentVersions, textChanged,
                out var dependentDocumentVersion, out var dependentSemanticVersion);

            return this.With(
                additionalDocumentStates: newDocumentStates,
                latestDocumentVersion: dependentDocumentVersion,
                latestDocumentTopLevelChangeVersion: dependentSemanticVersion);
        }

        public ProjectState UpdateAnalyzerConfigDocument(AnalyzerConfigDocumentState newDocument)
        {
            var oldDocument = AnalyzerConfigDocumentStates.GetRequiredState(newDocument.Id);
            if (oldDocument == newDocument)
            {
                return this;
            }

            var newDocumentStates = AnalyzerConfigDocumentStates.SetState(newDocument.Id, newDocument);

            return CreateNewStateForChangedAnalyzerConfigDocuments(newDocumentStates);
        }

        public ProjectState UpdateDocumentsOrder(ImmutableList<DocumentId> documentIds)
        {
            if (documentIds.SequenceEqual(DocumentStates.Ids))
            {
                return this;
            }

            return With(
                projectInfo: ProjectInfo.WithVersion(Version.GetNewerVersion()),
                documentStates: DocumentStates.WithCompilationOrder(documentIds));
        }

        private void GetLatestDependentVersions(
            TextDocumentStates<DocumentState> newDocumentStates,
            TextDocumentStates<AdditionalDocumentState> newAdditionalDocumentStates,
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
