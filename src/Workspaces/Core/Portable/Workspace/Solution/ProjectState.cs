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
using Microsoft.CodeAnalysis.Diagnostics.Analyzers.NamingStyles;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Serialization;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis
{
    internal partial class ProjectState
    {
        private readonly ProjectInfo _projectInfo;
        public readonly LanguageServices LanguageServices;

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
        private readonly AsyncLazy<ProjectStateChecksums> _lazyChecksums;

        /// <summary>
        /// Analyzer config options to be used for specific trees.
        /// </summary>
        private readonly AsyncLazy<AnalyzerConfigOptionsCache> _lazyAnalyzerConfigOptions;

        private AnalyzerOptions? _lazyAnalyzerOptions;

        /// <summary>
        /// The list of source generators and the analyzer reference they came from.
        /// </summary>
        private ImmutableDictionary<ISourceGenerator, AnalyzerReference>? _lazySourceGenerators;

        private ProjectState(
            ProjectInfo projectInfo,
            LanguageServices languageServices,
            TextDocumentStates<DocumentState> documentStates,
            TextDocumentStates<AdditionalDocumentState> additionalDocumentStates,
            TextDocumentStates<AnalyzerConfigDocumentState> analyzerConfigDocumentStates,
            AsyncLazy<VersionStamp> lazyLatestDocumentVersion,
            AsyncLazy<VersionStamp> lazyLatestDocumentTopLevelChangeVersion,
            AsyncLazy<AnalyzerConfigOptionsCache> lazyAnalyzerConfigSet)
        {
            LanguageServices = languageServices;
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

            _lazyChecksums = AsyncLazy.Create(ComputeChecksumsAsync);
        }

        public ProjectState(LanguageServices languageServices, ProjectInfo projectInfo)
        {
            Contract.ThrowIfNull(projectInfo);
            Contract.ThrowIfNull(languageServices);

            LanguageServices = languageServices;

            var projectInfoFixed = FixProjectInfo(projectInfo);
            var loadTextOptions = new LoadTextOptions(projectInfoFixed.Attributes.ChecksumAlgorithm);

            // We need to compute our AnalyerConfigDocumentStates first, since we use those to produce our DocumentStates
            AnalyzerConfigDocumentStates = new TextDocumentStates<AnalyzerConfigDocumentState>(projectInfoFixed.AnalyzerConfigDocuments, info => new AnalyzerConfigDocumentState(languageServices.SolutionServices, info, loadTextOptions));

            _lazyAnalyzerConfigOptions = ComputeAnalyzerConfigOptionsValueSource(AnalyzerConfigDocumentStates);

            // Add analyzer config information to the compilation options
            if (projectInfoFixed.CompilationOptions != null)
            {
                projectInfoFixed = projectInfoFixed.WithCompilationOptions(
                    projectInfoFixed.CompilationOptions.WithSyntaxTreeOptionsProvider(
                        new ProjectSyntaxTreeOptionsProvider(_lazyAnalyzerConfigOptions)));
            }

            var parseOptions = projectInfoFixed.ParseOptions;

            DocumentStates = new TextDocumentStates<DocumentState>(projectInfoFixed.Documents, info => CreateDocument(info, parseOptions, loadTextOptions));
            AdditionalDocumentStates = new TextDocumentStates<AdditionalDocumentState>(projectInfoFixed.AdditionalDocuments, info => new AdditionalDocumentState(languageServices.SolutionServices, info, loadTextOptions));

            _lazyLatestDocumentVersion = AsyncLazy.Create(c => ComputeLatestDocumentVersionAsync(DocumentStates, AdditionalDocumentStates, c));
            _lazyLatestDocumentTopLevelChangeVersion = AsyncLazy.Create(c => ComputeLatestDocumentTopLevelChangeVersionAsync(DocumentStates, AdditionalDocumentStates, c));

            // ownership of information on document has moved to project state. clear out documentInfo the state is
            // holding on. otherwise, these information will be held onto unnecessarily by projectInfo even after
            // the info has changed by DocumentState.
            // we hold onto the info so that we don't need to duplicate all information info already has in the state
            _projectInfo = ClearAllDocumentsFromProjectInfo(projectInfoFixed);

            _lazyChecksums = AsyncLazy.Create(ComputeChecksumsAsync);
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
                var compilationFactory = LanguageServices.GetService<ICompilationFactoryService>();
                if (compilationFactory != null)
                {
                    projectInfo = projectInfo.WithCompilationOptions(compilationFactory.GetDefaultCompilationOptions());
                }
            }

            if (projectInfo.ParseOptions == null)
            {
                var syntaxTreeFactory = LanguageServices.GetService<ISyntaxTreeFactoryService>();
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
                return AsyncLazy.Create(c => ComputeTopLevelChangeTextVersionAsync(oldVersion, newDocument, c));
            }
            else
            {
                return AsyncLazy.Create(c => ComputeLatestDocumentTopLevelChangeVersionAsync(newDocumentStates, newAdditionalDocumentStates, c));
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

        internal DocumentState CreateDocument(DocumentInfo documentInfo, ParseOptions? parseOptions, LoadTextOptions loadTextOptions)
        {
            var doc = new DocumentState(LanguageServices, documentInfo, parseOptions, loadTextOptions);

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

        public async Task<AnalyzerConfigData> GetAnalyzerOptionsForPathAsync(string path, CancellationToken cancellationToken)
        {
            var cache = await _lazyAnalyzerConfigOptions.GetValueAsync(cancellationToken).ConfigureAwait(false);
            return cache.GetOptionsForSourcePath(path);
        }

        public AnalyzerConfigData GetAnalyzerOptionsForPath(string path, CancellationToken cancellationToken)
            => _lazyAnalyzerConfigOptions.GetValue(cancellationToken).GetOptionsForSourcePath(path);

        public AnalyzerConfigData? GetAnalyzerConfigOptions()
        {
            var extension = _projectInfo.Language switch
            {
                LanguageNames.CSharp => ".cs",
                LanguageNames.VisualBasic => ".vb",
                _ => null
            };

            if (extension == null)
            {
                return null;
            }

            if (!PathUtilities.IsAbsolute(_projectInfo.FilePath))
            {
                return null;
            }

            // We need to find the analyzer config options at the root of the project.
            // Currently, there is no compiler API to query analyzer config options for a directory in a language agnostic fashion.
            // So, we use a dummy language-specific file name appended to the project directory to query analyzer config options.
            // NIL character is invalid in paths so it will never match any pattern in editorconfig, but editorconfig parsing allows it.
            // TODO: https://github.com/dotnet/roslyn/issues/61217

            var projectDirectory = PathUtilities.GetDirectoryName(_projectInfo.FilePath);
            Contract.ThrowIfNull(projectDirectory);

            var sourceFilePath = PathUtilities.CombinePathsUnchecked(projectDirectory, "\0" + extension);

            return GetAnalyzerOptionsForPath(sourceFilePath, CancellationToken.None);
        }

        internal sealed class ProjectAnalyzerConfigOptionsProvider : AnalyzerConfigOptionsProvider
        {
            private readonly ProjectState _projectState;
            private RazorDesignTimeAnalyzerConfigOptions? _lazyRazorDesignTimeOptions = null;

            public ProjectAnalyzerConfigOptionsProvider(ProjectState projectState)
                => _projectState = projectState;

            private AnalyzerConfigOptionsCache GetCache()
                => _projectState._lazyAnalyzerConfigOptions.GetValue(CancellationToken.None);

            public override AnalyzerConfigOptions GlobalOptions
                => GetCache().GlobalConfigOptions.ConfigOptions;

            public override AnalyzerConfigOptions GetOptions(SyntaxTree tree)
            {
                var documentId = DocumentState.GetDocumentIdForTree(tree);
                var cache = GetCache();
                if (documentId != null && _projectState.DocumentStates.TryGetState(documentId, out var documentState))
                {
                    return GetOptions(cache, documentState);
                }

                return GetOptionsForSourcePath(cache, tree.FilePath);
            }

            internal async ValueTask<StructuredAnalyzerConfigOptions> GetOptionsAsync(DocumentState documentState, CancellationToken cancellationToken)
            {
                var cache = await _projectState._lazyAnalyzerConfigOptions.GetValueAsync(cancellationToken).ConfigureAwait(false);
                return GetOptions(cache, documentState);
            }

            private StructuredAnalyzerConfigOptions GetOptions(in AnalyzerConfigOptionsCache cache, DocumentState documentState)
            {
                var services = _projectState.LanguageServices.SolutionServices;

                if (documentState.IsRazorDocument())
                {
                    return _lazyRazorDesignTimeOptions ??= new RazorDesignTimeAnalyzerConfigOptions(services);
                }

                var filePath = GetEffectiveFilePath(documentState);
                if (filePath == null)
                {
                    return StructuredAnalyzerConfigOptions.Empty;
                }

                var legacyDocumentOptionsProvider = services.GetService<ILegacyDocumentOptionsProvider>();
                if (legacyDocumentOptionsProvider != null)
                {
                    return StructuredAnalyzerConfigOptions.Create(legacyDocumentOptionsProvider.GetOptions(_projectState.Id, filePath));
                }

                return GetOptionsForSourcePath(cache, filePath);
            }

            public override AnalyzerConfigOptions GetOptions(AdditionalText textFile)
            {
                // TODO: correctly find the file path, since it looks like we give this the document's .Name under the covers if we don't have one
                return GetOptionsForSourcePath(GetCache(), textFile.Path);
            }

            private static StructuredAnalyzerConfigOptions GetOptionsForSourcePath(in AnalyzerConfigOptionsCache cache, string path)
                => cache.GetOptionsForSourcePath(path).ConfigOptions;

            private string? GetEffectiveFilePath(DocumentState documentState)
            {
                if (!string.IsNullOrEmpty(documentState.FilePath))
                {
                    return documentState.FilePath;
                }

                // We need to work out path to this document. Documents may not have a "real" file path if they're something created
                // as a part of a code action, but haven't been written to disk yet.

                var projectFilePath = _projectState.FilePath;

                if (documentState.Name != null && projectFilePath != null)
                {
                    var projectPath = PathUtilities.GetDirectoryName(projectFilePath);

                    if (!RoslynString.IsNullOrEmpty(projectPath) &&
                        PathUtilities.GetDirectoryName(projectFilePath) is string directory)
                    {
                        return PathUtilities.CombinePathsUnchecked(directory, documentState.Name);
                    }
                }

                return null;
            }
        }

        /// <summary>
        /// Provides editorconfig options for Razor design-time documents.
        /// Razor does not support editorconfig options but has custom settings for a few formatting options whose values
        /// are only available in-proc and the same for all Razor design-time documents.
        /// This type emulates these options as analyzer config options.
        /// </summary>
        private sealed class RazorDesignTimeAnalyzerConfigOptions : StructuredAnalyzerConfigOptions
        {
            private readonly ILegacyGlobalOptionsWorkspaceService? _globalOptions;

            public RazorDesignTimeAnalyzerConfigOptions(SolutionServices services)
            {
                // not available OOP:
                _globalOptions = services.GetService<ILegacyGlobalOptionsWorkspaceService>();
            }

            public override bool TryGetValue(string key, [NotNullWhen(true)] out string? value)
            {
                if (_globalOptions != null)
                {
                    if (key == "indent_style")
                    {
                        value = _globalOptions.RazorUseTabs ? "tab" : "space";
                        return true;
                    }

                    if (key == "tab_width" || key == "indent_size")
                    {
                        value = _globalOptions.RazorTabSize.ToString();
                        return true;
                    }
                }

                value = null;
                return false;
            }

            public override IEnumerable<string> Keys
            {
                get
                {
                    if (_globalOptions != null)
                    {
                        yield return "indent_style";
                        yield return "tab_width";
                        yield return "indent_size";
                    }
                }
            }

            public override NamingStylePreferences GetNamingStylePreferences()
                => NamingStylePreferences.Empty;
        }

        private sealed class ProjectSyntaxTreeOptionsProvider : SyntaxTreeOptionsProvider
        {
            private readonly AsyncLazy<AnalyzerConfigOptionsCache> _lazyAnalyzerConfigSet;

            public ProjectSyntaxTreeOptionsProvider(AsyncLazy<AnalyzerConfigOptionsCache> lazyAnalyzerConfigSet)
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

        private static AsyncLazy<AnalyzerConfigOptionsCache> ComputeAnalyzerConfigOptionsValueSource(TextDocumentStates<AnalyzerConfigDocumentState> analyzerConfigDocumentStates)
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
                });
        }

        private readonly struct AnalyzerConfigOptionsCache
        {
            private readonly ConcurrentDictionary<string, AnalyzerConfigData> _sourcePathToResult = new();
            private readonly Func<string, AnalyzerConfigData> _computeFunction;
            private readonly Lazy<AnalyzerConfigData> _global;

            public AnalyzerConfigOptionsCache(AnalyzerConfigSet configSet)
            {
                _global = new Lazy<AnalyzerConfigData>(() => new AnalyzerConfigData(configSet.GlobalConfigOptions));
                _computeFunction = path => new AnalyzerConfigData(configSet.GetOptionsForSourcePath(path));
            }

            public AnalyzerConfigData GlobalConfigOptions
                => _global.Value;

            public AnalyzerConfigData GetOptionsForSourcePath(string sourcePath)
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
        public SourceHashAlgorithm ChecksumAlgorithm => this.ProjectInfo.ChecksumAlgorithm;

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
            AsyncLazy<AnalyzerConfigOptionsCache>? analyzerConfigSet = null)
        {
            return new ProjectState(
                projectInfo ?? _projectInfo,
                LanguageServices,
                documentStates ?? DocumentStates,
                additionalDocumentStates ?? AdditionalDocumentStates,
                analyzerConfigDocumentStates ?? AnalyzerConfigDocumentStates,
                latestDocumentVersion ?? _lazyLatestDocumentVersion,
                latestDocumentTopLevelChangeVersion ?? _lazyLatestDocumentTopLevelChangeVersion,
                analyzerConfigSet ?? _lazyAnalyzerConfigOptions);
        }

        internal ProjectInfo.ProjectAttributes Attributes
            => ProjectInfo.Attributes;

        /// <summary>
        /// Updates <see cref="ProjectInfo"/> to a newer version of attributes.
        /// </summary>
        private ProjectState WithNewerAttributes(ProjectInfo.ProjectAttributes attributes)
        {
            // version must have already been updated:
            Debug.Assert(attributes.Version != Attributes.Version);

            return With(projectInfo: ProjectInfo.With(attributes: attributes));
        }

        public ProjectState WithName(string name)
            => (name == Name) ? this : WithNewerAttributes(Attributes.With(name: name, version: Version.GetNewerVersion()));

        public ProjectState WithFilePath(string? filePath)
            => (filePath == FilePath) ? this : WithNewerAttributes(Attributes.With(filePath: filePath, version: Version.GetNewerVersion()));

        public ProjectState WithAssemblyName(string assemblyName)
            => (assemblyName == AssemblyName) ? this : WithNewerAttributes(Attributes.With(assemblyName: assemblyName, version: Version.GetNewerVersion()));

        public ProjectState WithOutputFilePath(string? outputFilePath)
            => (outputFilePath == OutputFilePath) ? this : WithNewerAttributes(Attributes.With(outputPath: outputFilePath, version: Version.GetNewerVersion()));

        public ProjectState WithOutputRefFilePath(string? outputRefFilePath)
            => (outputRefFilePath == OutputRefFilePath) ? this : WithNewerAttributes(Attributes.With(outputRefPath: outputRefFilePath, version: Version.GetNewerVersion()));

        public ProjectState WithCompilationOutputInfo(in CompilationOutputInfo info)
            => (info == CompilationOutputInfo) ? this : WithNewerAttributes(Attributes.With(compilationOutputInfo: info, version: Version.GetNewerVersion()));

        public ProjectState WithDefaultNamespace(string? defaultNamespace)
            => (defaultNamespace == DefaultNamespace) ? this : WithNewerAttributes(Attributes.With(defaultNamespace: defaultNamespace, version: Version.GetNewerVersion()));

        public ProjectState WithHasAllInformation(bool hasAllInformation)
            => (hasAllInformation == HasAllInformation) ? this : WithNewerAttributes(Attributes.With(hasAllInformation: hasAllInformation, version: Version.GetNewerVersion()));

        public ProjectState WithRunAnalyzers(bool runAnalyzers)
            => (runAnalyzers == RunAnalyzers) ? this : WithNewerAttributes(Attributes.With(runAnalyzers: runAnalyzers, version: Version.GetNewerVersion()));

        public ProjectState WithChecksumAlgorithm(SourceHashAlgorithm checksumAlgorithm)
        {
            if (checksumAlgorithm == ChecksumAlgorithm)
            {
                return this;
            }

            return With(
                projectInfo: ProjectInfo.With(attributes: Attributes.With(checksumAlgorithm: checksumAlgorithm, version: Version.GetNewerVersion())),
                documentStates: UpdateDocumentsChecksumAlgorithm(checksumAlgorithm));
        }

        private TextDocumentStates<DocumentState> UpdateDocumentsChecksumAlgorithm(SourceHashAlgorithm checksumAlgorithm)
            => DocumentStates.UpdateStates(static (state, checksumAlgorithm) => state.UpdateChecksumAlgorithm(checksumAlgorithm), checksumAlgorithm);

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
                LanguageServices.GetRequiredService<ISyntaxTreeFactoryService>().OptionsDifferOnlyByPreprocessorDirectives(options, ParseOptions);

            return With(
                projectInfo: ProjectInfo.WithParseOptions(options).WithVersion(Version.GetNewerVersion()),
                documentStates: DocumentStates.UpdateStates(static (state, args) => state.UpdateParseOptions(args.options, args.onlyPreprocessorDirectiveChange), (options, onlyPreprocessorDirectiveChange)));
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

        [MemberNotNull(nameof(_lazySourceGenerators))]
        private void EnsureSourceGeneratorsInitialized()
        {
            if (_lazySourceGenerators == null)
            {
                var builder = ImmutableDictionary.CreateBuilder<ISourceGenerator, AnalyzerReference>();

                foreach (var analyzerReference in AnalyzerReferences)
                {
                    foreach (var generator in analyzerReference.GetGenerators(Language))
                        builder.Add(generator, analyzerReference);
                }

                Interlocked.CompareExchange(ref _lazySourceGenerators, builder.ToImmutable(), comparand: null);
            }
        }

        public IEnumerable<ISourceGenerator> SourceGenerators
        {
            get
            {
                EnsureSourceGeneratorsInitialized();
                return _lazySourceGenerators.Keys;
            }
        }

        public AnalyzerReference GetAnalyzerReferenceForGenerator(ISourceGenerator generator)
        {
            EnsureSourceGeneratorsInitialized();
            return _lazySourceGenerators[generator];
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

        public ProjectState UpdateDocument(DocumentState newDocument, bool contentChanged)
        {
            var oldDocument = DocumentStates.GetRequiredState(newDocument.Id);
            if (oldDocument == newDocument)
            {
                return this;
            }

            var newDocumentStates = DocumentStates.SetState(newDocument.Id, newDocument);
            GetLatestDependentVersions(
                newDocumentStates, AdditionalDocumentStates, oldDocument, newDocument, contentChanged,
                out var dependentDocumentVersion, out var dependentSemanticVersion);

            return With(
                documentStates: newDocumentStates,
                latestDocumentVersion: dependentDocumentVersion,
                latestDocumentTopLevelChangeVersion: dependentSemanticVersion);
        }

        public ProjectState UpdateAdditionalDocument(AdditionalDocumentState newDocument, bool contentChanged)
        {
            var oldDocument = AdditionalDocumentStates.GetRequiredState(newDocument.Id);
            if (oldDocument == newDocument)
            {
                return this;
            }

            var newDocumentStates = AdditionalDocumentStates.SetState(newDocument.Id, newDocument);
            GetLatestDependentVersions(
                DocumentStates, newDocumentStates, oldDocument, newDocument, contentChanged,
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
            bool contentChanged,
            out AsyncLazy<VersionStamp> dependentDocumentVersion, out AsyncLazy<VersionStamp> dependentSemanticVersion)
        {
            var recalculateDocumentVersion = false;
            var recalculateSemanticVersion = false;

            if (contentChanged)
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

            dependentDocumentVersion = recalculateDocumentVersion
                ? AsyncLazy.Create(c => ComputeLatestDocumentVersionAsync(newDocumentStates, newAdditionalDocumentStates, c))
                : contentChanged
                    ? AsyncLazy.Create(newDocument.GetTextVersionAsync)
                    : _lazyLatestDocumentVersion;

            dependentSemanticVersion = recalculateSemanticVersion
                ? AsyncLazy.Create(c => ComputeLatestDocumentTopLevelChangeVersionAsync(newDocumentStates, newAdditionalDocumentStates, c))
                : contentChanged
                    ? CreateLazyLatestDocumentTopLevelChangeVersion(newDocument, newDocumentStates, newAdditionalDocumentStates)
                    : _lazyLatestDocumentTopLevelChangeVersion;
        }
    }
}
