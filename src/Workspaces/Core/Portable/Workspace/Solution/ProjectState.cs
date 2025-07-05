// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Collections;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Diagnostics.Analyzers.NamingStyles;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Serialization;
using Microsoft.CodeAnalysis.Shared.Collections;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis;

internal sealed partial class ProjectState : IComparable<ProjectState>
{
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

    /// <summary>
    /// Analyzer config options to be used for specific trees.
    /// </summary>
    private readonly AnalyzerConfigOptionsCache _analyzerConfigOptionsCache;

    private ProjectState(
        ProjectInfo projectInfo,
        LanguageServices languageServices,
        TextDocumentStates<DocumentState> documentStates,
        TextDocumentStates<AdditionalDocumentState> additionalDocumentStates,
        TextDocumentStates<AnalyzerConfigDocumentState> analyzerConfigDocumentStates,
        AsyncLazy<VersionStamp> lazyLatestDocumentVersion,
        AsyncLazy<VersionStamp> lazyLatestDocumentTopLevelChangeVersion,
        AnalyzerConfigOptionsCache analyzerConfigOptionsCache)
    {
        LanguageServices = languageServices;
        DocumentStates = documentStates;
        AdditionalDocumentStates = additionalDocumentStates;
        AnalyzerConfigDocumentStates = analyzerConfigDocumentStates;
        _lazyLatestDocumentVersion = lazyLatestDocumentVersion;
        _lazyLatestDocumentTopLevelChangeVersion = lazyLatestDocumentTopLevelChangeVersion;
        _analyzerConfigOptionsCache = analyzerConfigOptionsCache;

        // ownership of information on document has moved to project state. clear out documentInfo the state is
        // holding on. otherwise, these information will be held onto unnecessarily by projectInfo even after
        // the info has changed by DocumentState.
        ProjectInfo = ClearAllDocumentsFromProjectInfo(projectInfo);
    }

    public ProjectState(LanguageServices languageServices, ProjectInfo projectInfo, StructuredAnalyzerConfigOptions fallbackAnalyzerOptions)
    {
        Contract.ThrowIfNull(projectInfo);
        Contract.ThrowIfNull(languageServices);

        LanguageServices = languageServices;

        var projectInfoFixed = FixProjectInfo(projectInfo);
        var loadTextOptions = new LoadTextOptions(projectInfoFixed.Attributes.ChecksumAlgorithm);

        // We need to compute our AnalyerConfigDocumentStates first, since we use those to produce our DocumentStates
        AnalyzerConfigDocumentStates = new TextDocumentStates<AnalyzerConfigDocumentState>(projectInfoFixed.AnalyzerConfigDocuments, info => new AnalyzerConfigDocumentState(languageServices.SolutionServices, info, loadTextOptions));

        _analyzerConfigOptionsCache = new AnalyzerConfigOptionsCache(AnalyzerConfigDocumentStates, fallbackAnalyzerOptions);

        // Add analyzer config information to the compilation options
        if (projectInfoFixed.CompilationOptions != null)
        {
            projectInfoFixed = projectInfoFixed.WithCompilationOptions(
                projectInfoFixed.CompilationOptions.WithSyntaxTreeOptionsProvider(
                    new ProjectSyntaxTreeOptionsProvider(_analyzerConfigOptionsCache)));
        }

        var parseOptions = projectInfoFixed.ParseOptions;

        DocumentStates = new TextDocumentStates<DocumentState>(projectInfoFixed.Documents, info => CreateDocument(info, parseOptions, loadTextOptions));
        AdditionalDocumentStates = new TextDocumentStates<AdditionalDocumentState>(projectInfoFixed.AdditionalDocuments, info => new AdditionalDocumentState(languageServices.SolutionServices, info, loadTextOptions));

        _lazyLatestDocumentVersion = AsyncLazy.Create(static async (self, c) => await ComputeLatestDocumentVersionAsync(self.DocumentStates, self.AdditionalDocumentStates, c).ConfigureAwait(false), arg: this);
        _lazyLatestDocumentTopLevelChangeVersion = AsyncLazy.Create(static (self, c) => ComputeLatestDocumentTopLevelChangeVersionAsync(self.DocumentStates, self.AdditionalDocumentStates, c), arg: this);

        // ownership of information on document has moved to project state. clear out documentInfo the state is
        // holding on. otherwise, these information will be held onto unnecessarily by projectInfo even after
        // the info has changed by DocumentState.
        // we hold onto the info so that we don't need to duplicate all information info already has in the state
        ProjectInfo = ClearAllDocumentsFromProjectInfo(projectInfoFixed);
    }

    public TextDocumentStates<TDocumentState> GetDocumentStates<TDocumentState>()
        where TDocumentState : TextDocumentState
        => (TextDocumentStates<TDocumentState>)(object)(
            typeof(TDocumentState) == typeof(DocumentState) ? DocumentStates :
            typeof(TDocumentState) == typeof(AdditionalDocumentState) ? AdditionalDocumentStates :
            typeof(TDocumentState) == typeof(AnalyzerConfigDocumentState) ? AnalyzerConfigDocumentStates :
            throw ExceptionUtilities.UnexpectedValue(typeof(TDocumentState)));

    private async Task<Dictionary<ImmutableArray<byte>, DocumentId>> ComputeContentHashToDocumentIdAsync(CancellationToken cancellationToken)
    {
        var result = new Dictionary<ImmutableArray<byte>, DocumentId>(ImmutableArrayComparer<byte>.Instance);
        foreach (var (documentId, documentState) in this.DocumentStates.States)
        {
            var text = await documentState.GetTextAsync(cancellationToken).ConfigureAwait(false);
            var contentHash = text.GetContentHash();

            // Note: It is technically possible for multiple files to have the same content hash.  However, in that case
            // the compiler will produce an error if that content hash is referred to by an interceptor attribute.  So
            // it's fine for us to just pick one of the documents over the other here.
            result[contentHash] = documentId;
        }

        return result;
    }

    private AsyncLazy<ProjectStateChecksums> LazyChecksums
    {
        get
        {
            if (field is null)
            {
                Interlocked.CompareExchange(
                    ref field,
                    AsyncLazy.Create(static (self, cancellationToken) => self.ComputeChecksumsAsync(cancellationToken), arg: this),
                    null);
            }

            return field;
        }
    }

    // Mapping from content has to document id (access via LazyContentHashToDocumentId)
    private AsyncLazy<Dictionary<ImmutableArray<byte>, DocumentId>> LazyContentHashToDocumentId
    {
        get
        {
            if (field is null)
            {
                Interlocked.CompareExchange(
                    ref field,
                    AsyncLazy.Create(static (self, cancellationToken) => self.ComputeContentHashToDocumentIdAsync(cancellationToken), arg: this),
                    null);
            }

            return field;
        }
    }

    private static ProjectInfo ClearAllDocumentsFromProjectInfo(ProjectInfo projectInfo)
    {
        return projectInfo
            .WithDocuments([])
            .WithAdditionalDocuments([])
            .WithAnalyzerConfigDocuments([]);
    }

    public async ValueTask<DocumentId?> GetDocumentIdAsync(ImmutableArray<byte> contentHash, CancellationToken cancellationToken)
    {
        var map = await LazyContentHashToDocumentId.GetValueAsync(cancellationToken).ConfigureAwait(false);
        return map.TryGetValue(contentHash, out var documentId) ? documentId : null;
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

    private static async ValueTask<VersionStamp> ComputeLatestDocumentVersionAsync(TextDocumentStates<DocumentState> documentStates, TextDocumentStates<AdditionalDocumentState> additionalDocumentStates, CancellationToken cancellationToken)
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
        ImmutableArray<TextDocumentState> newDocuments,
        TextDocumentStates<DocumentState> newDocumentStates,
        TextDocumentStates<AdditionalDocumentState> newAdditionalDocumentStates)
    {
        if (_lazyLatestDocumentTopLevelChangeVersion.TryGetValue(out var oldVersion))
        {
            return AsyncLazy.Create(static (arg, cancellationToken) =>
                ComputeTopLevelChangeTextVersionAsync(arg.oldVersion, arg.newDocuments, cancellationToken),
                arg: (oldVersion, newDocuments));
        }
        else
        {
            return AsyncLazy.Create(static (arg, cancellationToken) =>
                ComputeLatestDocumentTopLevelChangeVersionAsync(arg.newDocumentStates, arg.newAdditionalDocumentStates, cancellationToken),
                arg: (newDocumentStates, newAdditionalDocumentStates));
        }
    }

    private static async Task<VersionStamp> ComputeTopLevelChangeTextVersionAsync(
        VersionStamp oldVersion, ImmutableArray<TextDocumentState> newDocuments, CancellationToken cancellationToken)
    {
        var finalVersion = oldVersion;
        foreach (var newDocument in newDocuments)
        {
            var newVersion = await newDocument.GetTopLevelChangeTextVersionAsync(cancellationToken).ConfigureAwait(false);
            finalVersion = newVersion.GetNewerVersion(finalVersion);
        }

        return finalVersion;
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
        var doc = DocumentState.Create(LanguageServices, documentInfo, parseOptions, loadTextOptions);

        if (doc.SourceCodeKind != documentInfo.SourceCodeKind)
        {
            doc = doc.UpdateSourceCodeKind(documentInfo.SourceCodeKind);
        }

        return doc;
    }

    internal TDocumentState CreateDocument<TDocumentState>(DocumentInfo documentInfo)
        where TDocumentState : TextDocumentState
        => (TDocumentState)(object)(
            typeof(TDocumentState) == typeof(DocumentState) ? CreateDocument(documentInfo, ParseOptions, new LoadTextOptions(ChecksumAlgorithm)) :
            typeof(TDocumentState) == typeof(AdditionalDocumentState) ? new AdditionalDocumentState(LanguageServices.SolutionServices, documentInfo, new LoadTextOptions(ChecksumAlgorithm)) :
            typeof(TDocumentState) == typeof(AnalyzerConfigDocumentState) ? new AnalyzerConfigDocumentState(LanguageServices.SolutionServices, documentInfo, new LoadTextOptions(ChecksumAlgorithm)) :
            throw ExceptionUtilities.UnexpectedValue(typeof(TDocumentState)));

    private ImmutableArray<AdditionalText> AdditionalFiles
    {
        get
        {
            return InterlockedOperations.Initialize(
                ref field,
                static self => self.AdditionalDocumentStates.SelectAsArray(static documentState => documentState.AdditionalText),
                this);
        }
    }

    public AnalyzerOptions ProjectAnalyzerOptions
        => InterlockedOperations.Initialize(
            ref field,
            static self => new AnalyzerOptions(
                additionalFiles: self.AdditionalFiles,
                optionsProvider: new ProjectAnalyzerConfigOptionsProvider(self)),
            this);

    public AnalyzerOptions HostAnalyzerOptions
    {
        get => InterlockedOperations.Initialize(
            ref field,
            static self => new AnalyzerOptions(
                additionalFiles: self.AdditionalFiles,
                optionsProvider: new ProjectHostAnalyzerConfigOptionsProvider(self)),
            this); private set;
    }

    public AnalyzerConfigData GetAnalyzerOptionsForPath(string path, CancellationToken cancellationToken)
        => _analyzerConfigOptionsCache.Lazy.GetValue(cancellationToken).GetOptionsForSourcePath(path);

    public AnalyzerConfigData? GetAnalyzerConfigOptions()
    {
        var extension = ProjectInfo.Language switch
        {
            LanguageNames.CSharp => ".cs",
            LanguageNames.VisualBasic => ".vb",
            _ => null
        };

        if (extension == null)
        {
            return null;
        }

        if (!PathUtilities.IsAbsolute(ProjectInfo.FilePath))
        {
            return null;
        }

        // We need to find the analyzer config options at the root of the project.
        // Currently, there is no compiler API to query analyzer config options for a directory in a language agnostic fashion.
        // So, we use a dummy language-specific file name appended to the project directory to query analyzer config options.
        // NIL character is invalid in paths so it will never match any pattern in editorconfig, but editorconfig parsing allows it.
        // TODO: https://github.com/dotnet/roslyn/issues/61217

        var projectDirectory = PathUtilities.GetDirectoryName(ProjectInfo.FilePath);
        Contract.ThrowIfNull(projectDirectory);

        var sourceFilePath = PathUtilities.CombinePathsUnchecked(projectDirectory, "\0" + extension);

        return GetAnalyzerOptionsForPath(sourceFilePath, CancellationToken.None);
    }

    internal sealed class ProjectAnalyzerConfigOptionsProvider(ProjectState projectState) : AnalyzerConfigOptionsProvider
    {
        private AnalyzerConfigOptionsCache.Value GetCache()
            => projectState._analyzerConfigOptionsCache.Lazy.GetValue(CancellationToken.None);

        public override AnalyzerConfigOptions GlobalOptions
            => GetCache().GlobalConfigOptions.ConfigOptionsWithoutFallback;

        public override AnalyzerConfigOptions GetOptions(SyntaxTree tree)
        {
            var documentId = DocumentState.GetDocumentIdForTree(tree);
            var cache = GetCache();
            if (documentId != null && projectState.DocumentStates.TryGetState(documentId, out var documentState))
            {
                return GetOptions(cache, documentState);
            }

            return GetOptionsForSourcePath(cache, tree.FilePath);
        }

        internal async ValueTask<StructuredAnalyzerConfigOptions> GetOptionsAsync(DocumentState documentState, CancellationToken cancellationToken)
        {
            var cache = await projectState._analyzerConfigOptionsCache.Lazy.GetValueAsync(cancellationToken).ConfigureAwait(false);
            return GetOptions(cache, documentState);
        }

        private StructuredAnalyzerConfigOptions GetOptions(in AnalyzerConfigOptionsCache.Value cache, DocumentState documentState)
        {
            var filePath = GetEffectiveFilePath(documentState);
            return filePath == null
                ? StructuredAnalyzerConfigOptions.Empty
                : GetOptionsForSourcePath(cache, filePath);
        }

        public override AnalyzerConfigOptions GetOptions(AdditionalText textFile)
        {
            // TODO: correctly find the file path, since it looks like we give this the document's .Name under the covers if we don't have one
            return GetOptionsForSourcePath(GetCache(), textFile.Path);
        }

        private static StructuredAnalyzerConfigOptions GetOptionsForSourcePath(in AnalyzerConfigOptionsCache.Value cache, string path)
            => cache.GetOptionsForSourcePath(path).ConfigOptionsWithoutFallback;

        private string? GetEffectiveFilePath(DocumentState documentState)
        {
            if (!string.IsNullOrEmpty(documentState.FilePath))
            {
                return documentState.FilePath;
            }

            // We need to work out path to this document. Documents may not have a "real" file path if they're something created
            // as a part of a code action, but haven't been written to disk yet.

            var projectFilePath = projectState.FilePath;

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

    internal sealed class ProjectHostAnalyzerConfigOptionsProvider(ProjectState projectState) : AnalyzerConfigOptionsProvider
    {
        private RazorDesignTimeAnalyzerConfigOptions? _lazyRazorDesignTimeOptions = null;

        private AnalyzerConfigOptionsCache.Value GetCache()
            => projectState._analyzerConfigOptionsCache.Lazy.GetValue(CancellationToken.None);

        public override AnalyzerConfigOptions GlobalOptions
            => GetCache().GlobalConfigOptions.ConfigOptionsWithoutFallback;

        public override AnalyzerConfigOptions GetOptions(SyntaxTree tree)
        {
            var documentId = DocumentState.GetDocumentIdForTree(tree);
            var cache = GetCache();
            if (documentId != null && projectState.DocumentStates.TryGetState(documentId, out var documentState))
            {
                return GetOptions(cache, documentState);
            }

            return GetOptionsForSourcePath(cache, tree.FilePath);
        }

        internal async ValueTask<StructuredAnalyzerConfigOptions> GetOptionsAsync(DocumentState documentState, CancellationToken cancellationToken)
        {
            var cache = await projectState._analyzerConfigOptionsCache.Lazy.GetValueAsync(cancellationToken).ConfigureAwait(false);
            return GetOptions(cache, documentState);
        }

        private StructuredAnalyzerConfigOptions GetOptions(in AnalyzerConfigOptionsCache.Value cache, DocumentState documentState)
        {
            var services = projectState.LanguageServices.SolutionServices;

            if (documentState.IsRazorDocument())
            {
                return _lazyRazorDesignTimeOptions ??= new RazorDesignTimeAnalyzerConfigOptions(services);
            }

            var filePath = GetEffectiveFilePath(documentState);
            return filePath == null
                ? StructuredAnalyzerConfigOptions.Empty
                : GetOptionsForSourcePath(cache, filePath);
        }

        public override AnalyzerConfigOptions GetOptions(AdditionalText textFile)
        {
            // TODO: correctly find the file path, since it looks like we give this the document's .Name under the covers if we don't have one
            return GetOptionsForSourcePath(GetCache(), textFile.Path);
        }

        private static StructuredAnalyzerConfigOptions GetOptionsForSourcePath(in AnalyzerConfigOptionsCache.Value cache, string path)
            => cache.GetOptionsForSourcePath(path).ConfigOptionsWithFallback;

        private string? GetEffectiveFilePath(DocumentState documentState)
        {
            if (!string.IsNullOrEmpty(documentState.FilePath))
            {
                return documentState.FilePath;
            }

            // We need to work out path to this document. Documents may not have a "real" file path if they're something created
            // as a part of a code action, but haven't been written to disk yet.

            var projectFilePath = projectState.FilePath;

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
    private sealed class RazorDesignTimeAnalyzerConfigOptions(SolutionServices services) : StructuredAnalyzerConfigOptions
    {
        private readonly ILegacyGlobalOptionsWorkspaceService? _globalOptions = services.GetService<ILegacyGlobalOptionsWorkspaceService>();

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

    private sealed class ProjectSyntaxTreeOptionsProvider(AnalyzerConfigOptionsCache lazyAnalyzerConfigSet) : SyntaxTreeOptionsProvider
    {
        private readonly AnalyzerConfigOptionsCache _lazyAnalyzerConfigSet = lazyAnalyzerConfigSet;

        public override GeneratedKind IsGenerated(SyntaxTree tree, CancellationToken cancellationToken)
        {
            var options = _lazyAnalyzerConfigSet.Lazy
                .GetValue(cancellationToken).GetOptionsForSourcePath(tree.FilePath);
            return GeneratedCodeUtilities.GetGeneratedCodeKindFromOptions(options.ConfigOptionsWithoutFallback);
        }

        public override bool TryGetDiagnosticValue(SyntaxTree tree, string diagnosticId, CancellationToken cancellationToken, out ReportDiagnostic severity)
        {
            var options = _lazyAnalyzerConfigSet.Lazy
                .GetValue(cancellationToken).GetOptionsForSourcePath(tree.FilePath);
            return options.TreeOptions.TryGetValue(diagnosticId, out severity);
        }

        public override bool TryGetGlobalDiagnosticValue(string diagnosticId, CancellationToken cancellationToken, out ReportDiagnostic severity)
        {
            var options = _lazyAnalyzerConfigSet.Lazy
                .GetValue(cancellationToken).GlobalConfigOptions;
            return options.TreeOptions.TryGetValue(diagnosticId, out severity);
        }

        public override bool Equals(object? obj)
        {
            return obj is ProjectSyntaxTreeOptionsProvider other
                && _lazyAnalyzerConfigSet.Lazy == other._lazyAnalyzerConfigSet.Lazy;
        }

        public override int GetHashCode() => _lazyAnalyzerConfigSet.GetHashCode();
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
    public ProjectInfo ProjectInfo { get; }

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
    internal bool HasSdkCodeStyleAnalyzers => this.ProjectInfo.HasSdkCodeStyleAnalyzers;

    private ProjectState With(
        ProjectInfo? projectInfo = null,
        TextDocumentStates<DocumentState>? documentStates = null,
        TextDocumentStates<AdditionalDocumentState>? additionalDocumentStates = null,
        TextDocumentStates<AnalyzerConfigDocumentState>? analyzerConfigDocumentStates = null,
        AsyncLazy<VersionStamp>? latestDocumentVersion = null,
        AsyncLazy<VersionStamp>? latestDocumentTopLevelChangeVersion = null,
        AnalyzerConfigOptionsCache? analyzerConfigOptionsCache = null)
    {
        return new ProjectState(
            projectInfo ?? ProjectInfo,
            LanguageServices,
            documentStates ?? DocumentStates,
            additionalDocumentStates ?? AdditionalDocumentStates,
            analyzerConfigDocumentStates ?? AnalyzerConfigDocumentStates,
            latestDocumentVersion ?? _lazyLatestDocumentVersion,
            latestDocumentTopLevelChangeVersion ?? _lazyLatestDocumentTopLevelChangeVersion,
            analyzerConfigOptionsCache ?? _analyzerConfigOptionsCache);
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

    internal ProjectState WithHasSdkCodeStyleAnalyzers(bool hasSdkCodeStyleAnalyzers)
    => (hasSdkCodeStyleAnalyzers == HasSdkCodeStyleAnalyzers) ? this : WithNewerAttributes(Attributes.With(hasSdkCodeStyleAnalyzers: hasSdkCodeStyleAnalyzers, version: Version.GetNewerVersion()));

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

    public ProjectState WithCompilationOptions(CompilationOptions? options)
    {
        if (options == CompilationOptions)
        {
            return this;
        }

        if (options == null)
        {
            throw new NotSupportedException(WorkspacesResources.Removing_compilation_options_is_not_supported);
        }

        var newProvider = new ProjectSyntaxTreeOptionsProvider(_analyzerConfigOptionsCache);

        return With(projectInfo: ProjectInfo.WithCompilationOptions(options.WithSyntaxTreeOptionsProvider(newProvider))
                   .WithVersion(Version.GetNewerVersion()));
    }

    public ProjectState WithParseOptions(ParseOptions? options)
    {
        if (options == ParseOptions)
        {
            return this;
        }

        if (options == null)
        {
            throw new NotSupportedException(WorkspacesResources.Removing_parse_options_is_not_supported);
        }

        var onlyPreprocessorDirectiveChange = ParseOptions != null &&
            LanguageServices.GetRequiredService<ISyntaxTreeFactoryService>().OptionsDifferOnlyByPreprocessorDirectives(options, ParseOptions);

        return With(
            projectInfo: ProjectInfo.WithParseOptions(options).WithVersion(Version.GetNewerVersion()),
            documentStates: DocumentStates.UpdateStates(static (state, args) =>
                state.UpdateParseOptionsAndSourceCodeKind(args.options, args.onlyPreprocessorDirectiveChange),
                arg: (options, onlyPreprocessorDirectiveChange)));
    }

    public ProjectState WithFallbackAnalyzerOptions(StructuredAnalyzerConfigOptions options)
    {
        if (options == _analyzerConfigOptionsCache.FallbackOptions)
        {
            return this;
        }

        return CreateNewStateForChangedAnalyzerConfig(AnalyzerConfigDocumentStates, options);
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

    public ProjectState WithAnalyzerReferences(IReadOnlyList<AnalyzerReference> analyzerReferences)
    {
        if (analyzerReferences == AnalyzerReferences)
        {
            return this;
        }

        return With(projectInfo: ProjectInfo.WithAnalyzerReferences(analyzerReferences).WithVersion(Version.GetNewerVersion()));
    }

    public ProjectState AddDocuments(ImmutableArray<DocumentState> documents)
    {
        if (documents.IsEmpty)
            return this;

        Debug.Assert(!documents.Any(d => DocumentStates.Contains(d.Id)));

        return With(
            projectInfo: ProjectInfo.WithVersion(Version.GetNewerVersion()),
            documentStates: DocumentStates.AddRange(documents));
    }

    public ProjectState AddAdditionalDocuments(ImmutableArray<AdditionalDocumentState> documents)
    {
        if (documents.IsEmpty)
            return this;

        Debug.Assert(!documents.Any(d => AdditionalDocumentStates.Contains(d.Id)));

        return With(
            projectInfo: ProjectInfo.WithVersion(Version.GetNewerVersion()),
            additionalDocumentStates: AdditionalDocumentStates.AddRange(documents));
    }

    public ProjectState AddAnalyzerConfigDocuments(ImmutableArray<AnalyzerConfigDocumentState> documents)
    {
        if (documents.IsEmpty)
            return this;

        Debug.Assert(!documents.Any(d => AnalyzerConfigDocumentStates.Contains(d.Id)));

        var newAnalyzerConfigDocumentStates = AnalyzerConfigDocumentStates.AddRange(documents);

        return CreateNewStateForChangedAnalyzerConfig(newAnalyzerConfigDocumentStates, _analyzerConfigOptionsCache.FallbackOptions);
    }

    private ProjectState CreateNewStateForChangedAnalyzerConfig(TextDocumentStates<AnalyzerConfigDocumentState> newAnalyzerConfigDocumentStates, StructuredAnalyzerConfigOptions fallbackOptions)
    {
        var newOptionsCache = new AnalyzerConfigOptionsCache(newAnalyzerConfigDocumentStates, fallbackOptions);
        var projectInfo = ProjectInfo.WithVersion(Version.GetNewerVersion());

        // Changing analyzer configs changes compilation options
        if (CompilationOptions != null)
        {
            var newProvider = new ProjectSyntaxTreeOptionsProvider(newOptionsCache);
            projectInfo = projectInfo
                .WithCompilationOptions(CompilationOptions.WithSyntaxTreeOptionsProvider(newProvider));
        }

        return With(
            projectInfo: projectInfo,
            analyzerConfigDocumentStates: newAnalyzerConfigDocumentStates,
            analyzerConfigOptionsCache: newOptionsCache);
    }

    public ProjectState RemoveDocuments(ImmutableArray<DocumentId> documentIds)
    {
        if (documentIds.IsEmpty)
            return this;

        // We create a new option cache for the new snapshot to avoid holding onto cached information
        // for removed documents.
        return With(
            projectInfo: ProjectInfo.WithVersion(Version.GetNewerVersion()),
            documentStates: DocumentStates.RemoveRange(documentIds),
            analyzerConfigOptionsCache: new AnalyzerConfigOptionsCache(AnalyzerConfigDocumentStates, _analyzerConfigOptionsCache.FallbackOptions));
    }

    public ProjectState RemoveAdditionalDocuments(ImmutableArray<DocumentId> documentIds)
    {
        if (documentIds.IsEmpty)
            return this;

        return With(
            projectInfo: ProjectInfo.WithVersion(Version.GetNewerVersion()),
            additionalDocumentStates: AdditionalDocumentStates.RemoveRange(documentIds));
    }

    public ProjectState RemoveAnalyzerConfigDocuments(ImmutableArray<DocumentId> documentIds)
    {
        if (documentIds.IsEmpty)
            return this;

        var newAnalyzerConfigDocumentStates = AnalyzerConfigDocumentStates.RemoveRange(documentIds);

        return CreateNewStateForChangedAnalyzerConfig(newAnalyzerConfigDocumentStates, _analyzerConfigOptionsCache.FallbackOptions);
    }

    public ProjectState RemoveAllNormalDocuments()
    {
        if (DocumentStates.IsEmpty)
            return this;

        // We create a new config options cache for the new snapshot to avoid holding onto cached information
        // for removed documents.
        return With(
            projectInfo: ProjectInfo.WithVersion(Version.GetNewerVersion()),
            documentStates: TextDocumentStates<DocumentState>.Empty,
            analyzerConfigOptionsCache: new AnalyzerConfigOptionsCache(AnalyzerConfigDocumentStates, _analyzerConfigOptionsCache.FallbackOptions));
    }

    public ProjectState UpdateDocument(DocumentState newDocument)
        => UpdateDocuments([DocumentStates.GetRequiredState(newDocument.Id)], [newDocument]);

    public ProjectState UpdateDocuments(ImmutableArray<DocumentState> oldDocuments, ImmutableArray<DocumentState> newDocuments)
    {
        Contract.ThrowIfTrue(oldDocuments.IsEmpty);
        Contract.ThrowIfFalse(oldDocuments.Length == newDocuments.Length);
        Debug.Assert(!oldDocuments.SequenceEqual(newDocuments));

        var newDocumentStates = DocumentStates.SetStates(newDocuments);

        // When computing the latest dependent version, we just need to know how
        GetLatestDependentVersions(
            newDocumentStates,
            AdditionalDocumentStates,
            oldDocuments.CastArray<TextDocumentState>(),
            newDocuments.CastArray<TextDocumentState>(),
            out var dependentDocumentVersion,
            out var dependentSemanticVersion);

        return With(
            documentStates: newDocumentStates,
            latestDocumentVersion: dependentDocumentVersion,
            latestDocumentTopLevelChangeVersion: dependentSemanticVersion);
    }

    public ProjectState UpdateAdditionalDocument(AdditionalDocumentState newDocument)
        => UpdateAdditionalDocuments([AdditionalDocumentStates.GetRequiredState(newDocument.Id)], [newDocument]);

    public ProjectState UpdateAdditionalDocuments(ImmutableArray<AdditionalDocumentState> oldDocuments, ImmutableArray<AdditionalDocumentState> newDocuments)
    {
        Contract.ThrowIfTrue(oldDocuments.IsEmpty);
        Contract.ThrowIfFalse(oldDocuments.Length == newDocuments.Length);
        Debug.Assert(!oldDocuments.SequenceEqual(newDocuments));

        var newDocumentStates = AdditionalDocumentStates.SetStates(newDocuments);

        GetLatestDependentVersions(
            DocumentStates,
            newDocumentStates,
            oldDocuments.CastArray<TextDocumentState>(),
            newDocuments.CastArray<TextDocumentState>(),
            out var dependentDocumentVersion,
            out var dependentSemanticVersion);

        return With(
            additionalDocumentStates: newDocumentStates,
            latestDocumentVersion: dependentDocumentVersion,
            latestDocumentTopLevelChangeVersion: dependentSemanticVersion);
    }

    public ProjectState UpdateAnalyzerConfigDocument(AnalyzerConfigDocumentState newDocument)
        => UpdateAnalyzerConfigDocuments([AnalyzerConfigDocumentStates.GetRequiredState(newDocument.Id)], [newDocument]);

    public ProjectState UpdateAnalyzerConfigDocuments(ImmutableArray<AnalyzerConfigDocumentState> oldDocuments, ImmutableArray<AnalyzerConfigDocumentState> newDocuments)
    {
        Debug.Assert(!oldDocuments.SequenceEqual(newDocuments));
        var newDocumentStates = AnalyzerConfigDocumentStates.SetStates(newDocuments);
        return CreateNewStateForChangedAnalyzerConfig(newDocumentStates, _analyzerConfigOptionsCache.FallbackOptions);
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
        ImmutableArray<TextDocumentState> oldDocuments,
        ImmutableArray<TextDocumentState> newDocuments,
        out AsyncLazy<VersionStamp> dependentDocumentVersion,
        out AsyncLazy<VersionStamp> dependentSemanticVersion)
    {
        var recalculateDocumentVersion = false;
        var recalculateSemanticVersion = false;

        var contentChanged = !oldDocuments.SequenceEqual(
            newDocuments,
            static (oldDocument, newDocument) => oldDocument.TextAndVersionSource == newDocument.TextAndVersionSource);

        if (contentChanged)
        {
            foreach (var oldDocument in oldDocuments)
            {
                if (oldDocument.TryGetTextVersion(out var oldVersion))
                {
                    if (!_lazyLatestDocumentVersion.TryGetValue(out var documentVersion) || documentVersion == oldVersion)
                        recalculateDocumentVersion = true;

                    if (!_lazyLatestDocumentTopLevelChangeVersion.TryGetValue(out var semanticVersion) || semanticVersion == oldVersion)
                        recalculateSemanticVersion = true;
                }

                if (recalculateDocumentVersion && recalculateSemanticVersion)
                    break;
            }
        }

        if (recalculateDocumentVersion)
        {
            dependentDocumentVersion = AsyncLazy.Create(static async (arg, cancellationToken) =>
                await ComputeLatestDocumentVersionAsync(arg.newDocumentStates, arg.newAdditionalDocumentStates, cancellationToken).ConfigureAwait(false),
                arg: (newDocumentStates, newAdditionalDocumentStates));
        }
        else if (contentChanged)
        {
            dependentDocumentVersion = AsyncLazy.Create(
                static async (newDocuments, cancellationToken) =>
                {
                    var finalVersion = await newDocuments[0].GetTextVersionAsync(cancellationToken).ConfigureAwait(false);
                    for (var i = 1; i < newDocuments.Length; i++)
                        finalVersion = finalVersion.GetNewerVersion(await newDocuments[i].GetTextVersionAsync(cancellationToken).ConfigureAwait(false));

                    return finalVersion;
                },
                arg: newDocuments);
        }
        else
        {
            dependentDocumentVersion = _lazyLatestDocumentVersion;
        }

        if (recalculateSemanticVersion)
        {
            dependentSemanticVersion = AsyncLazy.Create(static (arg, cancellationToken) =>
                ComputeLatestDocumentTopLevelChangeVersionAsync(arg.newDocumentStates, arg.newAdditionalDocumentStates, cancellationToken),
                arg: (newDocumentStates, newAdditionalDocumentStates));
        }
        else if (contentChanged)
        {
            dependentSemanticVersion = CreateLazyLatestDocumentTopLevelChangeVersion(newDocuments, newDocumentStates, newAdditionalDocumentStates);
        }
        else
        {
            dependentSemanticVersion = _lazyLatestDocumentTopLevelChangeVersion;
        }
    }

    public void AddDocumentIdsWithFilePath(ref TemporaryArray<DocumentId> temporaryArray, string filePath)
    {
        this.DocumentStates.AddDocumentIdsWithFilePath(ref temporaryArray, filePath);
        this.AdditionalDocumentStates.AddDocumentIdsWithFilePath(ref temporaryArray, filePath);
        this.AnalyzerConfigDocumentStates.AddDocumentIdsWithFilePath(ref temporaryArray, filePath);
    }

    public DocumentId? GetFirstDocumentIdWithFilePath(string filePath)
    {
        return this.DocumentStates.GetFirstDocumentIdWithFilePath(filePath) ??
            this.AdditionalDocumentStates.GetFirstDocumentIdWithFilePath(filePath) ??
            this.AnalyzerConfigDocumentStates.GetFirstDocumentIdWithFilePath(filePath);
    }

    public int CompareTo(ProjectState? other)
    {
        if (other is null)
            return 1;

        return this.Id.CompareTo(other.Id);
    }
}
