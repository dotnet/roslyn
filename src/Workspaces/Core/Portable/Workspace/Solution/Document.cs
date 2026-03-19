// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis;

/// <summary>
/// Represents a source code document that is part of a project.
/// It provides access to the source text, parsed syntax tree and the corresponding semantic model.
/// </summary>
[DebuggerDisplay("{GetDebuggerDisplay(),nq}")]
public class Document : TextDocument
{
    /// <summary>
    /// A cached reference to the <see cref="SemanticModel"/>.
    /// </summary>
    private WeakReference<SemanticModel>? _model;

    /// <summary>
    /// A cached reference to the <see cref="SemanticModel"/>.
    /// </summary>
    private WeakReference<SemanticModel>? _nullableDisabledModel;

    /// <summary>
    /// A cached task that can be returned once the tree has already been created. This is only set if <see cref="SupportsSyntaxTree"/> returns true,
    /// so the inner value can be non-null.
    /// </summary>
    private Task<SyntaxTree>? _syntaxTreeResultTask;

    internal Document(Project project, DocumentState state)
        : base(project, state, TextDocumentKind.Document)
    {
    }

    internal DocumentState DocumentState => (DocumentState)State;

    /// <summary>
    /// The kind of source code this document contains.
    /// </summary>
    public SourceCodeKind SourceCodeKind => DocumentState.SourceCodeKind;

    /// <summary>
    /// True if the info of the document change (name, folders, file path; not the content)
    /// </summary>
    internal override bool HasInfoChanged(TextDocument otherTextDocument)
    {
        var otherDocument = otherTextDocument as Document ??
            throw new ArgumentException($"{nameof(otherTextDocument)} isn't a regular document.", nameof(otherTextDocument));

        return base.HasInfoChanged(otherDocument) ||
               DocumentState.SourceCodeKind != otherDocument.SourceCodeKind;
    }

    [Obsolete("Use TextDocument.HasTextChanged")]
    internal bool HasTextChanged(Document otherDocument)
        => HasTextChanged(otherDocument, ignoreUnchangeableDocument: false);

    /// <summary>
    /// Get the current syntax tree for the document if the text is already loaded and the tree is already parsed.
    /// In almost all cases, you should call <see cref="GetSyntaxTreeAsync"/> to fetch the tree, which will parse the tree
    /// if it's not already parsed.
    /// </summary>
    public bool TryGetSyntaxTree([NotNullWhen(returnValue: true)] out SyntaxTree? syntaxTree)
    {
        // if we already have cache, use it
        if (_syntaxTreeResultTask != null)
        {
            syntaxTree = _syntaxTreeResultTask.Result;
            return true;
        }

        if (!DocumentState.TryGetSyntaxTree(out syntaxTree))
        {
            return false;
        }

        // cache the result if it is not already cached
        if (_syntaxTreeResultTask == null)
        {
            var result = Task.FromResult(syntaxTree);
            Interlocked.CompareExchange(ref _syntaxTreeResultTask, result, null);
        }

        return true;
    }

    /// <summary>
    /// Get the current syntax tree version for the document if the text is already loaded and the tree is already parsed.
    /// In almost all cases, you should call <see cref="GetSyntaxVersionAsync"/> to fetch the version, which will load the tree
    /// if it's not already available.
    /// </summary>
    public bool TryGetSyntaxVersion(out VersionStamp version)
    {
        version = default;
        if (!this.TryGetTextVersion(out var textVersion))
        {
            return false;
        }

        var projectVersion = this.Project.Version;
        version = textVersion.GetNewerVersion(projectVersion);
        return true;
    }

    /// <summary>
    /// Gets the version of the document's top level signature if it is already loaded and available.
    /// </summary>
    internal bool TryGetTopLevelChangeTextVersion(out VersionStamp version)
        => DocumentState.TryGetTopLevelChangeTextVersion(out version);

    /// <summary>
    /// Gets the version of the syntax tree. This is generally the newer of the text version and the project's version.
    /// </summary>
    public async Task<VersionStamp> GetSyntaxVersionAsync(CancellationToken cancellationToken = default)
    {
        var textVersion = await this.GetTextVersionAsync(cancellationToken).ConfigureAwait(false);
        var projectVersion = this.Project.Version;
        return textVersion.GetNewerVersion(projectVersion);
    }

    /// <summary>
    /// <see langword="true"/> if this Document supports providing data through the
    /// <see cref="GetSyntaxTreeAsync"/> and <see cref="GetSyntaxRootAsync"/> methods.
    ///
    /// If <see langword="false"/> then these methods will return <see langword="null"/> instead.
    /// </summary>
    public bool SupportsSyntaxTree => DocumentState.SupportsSyntaxTree;

    /// <summary>
    /// <see langword="true"/> if this Document supports providing data through the
    /// <see cref="GetSemanticModelAsync(CancellationToken)"/> method.
    ///
    /// If <see langword="false"/> then that method will return <see langword="null"/> instead.
    /// </summary>
    public bool SupportsSemanticModel
    {
        get
        {
            return this.SupportsSyntaxTree && this.Project.SupportsCompilation;
        }
    }

    /// <summary>
    /// Gets the <see cref="SyntaxTree" /> for this document asynchronously.
    /// </summary>
    /// <returns>
    /// The returned syntax tree can be <see langword="null"/> if the <see cref="SupportsSyntaxTree"/> returns <see
    /// langword="false"/>. This function may cause computation to occur the first time it is called, but will return
    /// a cached result every subsequent time.  <see cref="SyntaxTree"/>'s can hold onto their roots lazily. So calls 
    /// to <see cref="SyntaxTree.GetRoot"/> or <see cref="SyntaxTree.GetRootAsync"/> may end up causing computation
    /// to occur at that point.
    /// </returns>
    public async Task<SyntaxTree?> GetSyntaxTreeAsync(CancellationToken cancellationToken = default)
    {
        // If the language doesn't support getting syntax trees for a document, then bail out immediately.
        if (!this.SupportsSyntaxTree)
        {
            return null;
        }

        // if we have a cached result task use it
        if (_syntaxTreeResultTask != null)
        {
            return await _syntaxTreeResultTask.ConfigureAwait(false);
        }

        // check to see if we already have the tree before actually going async
        if (TryGetSyntaxTree(out var tree))
        {
            // stash a completed result task for this value for the next request (to reduce extraneous allocations of tasks)
            // don't use the actual async task because it depends on a specific cancellation token
            // its okay to cache the task and hold onto the SyntaxTree, because the DocumentState already keeps the SyntaxTree alive.
            _ = Interlocked.CompareExchange(ref _syntaxTreeResultTask, Task.FromResult(tree), null);

            return await _syntaxTreeResultTask.ConfigureAwait(false);
        }

        // do it async for real.
        return await DocumentState.GetSyntaxTreeAsync(cancellationToken).ConfigureAwait(false);
    }

    internal SyntaxTree? GetSyntaxTreeSynchronously(CancellationToken cancellationToken)
    {
        if (!this.SupportsSyntaxTree)
        {
            return null;
        }

        return DocumentState.GetSyntaxTree(cancellationToken);
    }

    /// <summary>
    /// Gets the root node of the current syntax tree if the syntax tree has already been parsed and the tree is still cached.
    /// In almost all cases, you should call <see cref="GetSyntaxRootAsync"/> to fetch the root node, which will parse
    /// the document if necessary.
    /// </summary>
    public bool TryGetSyntaxRoot([NotNullWhen(returnValue: true)] out SyntaxNode? root)
    {
        root = null;
        return this.TryGetSyntaxTree(out var tree) && tree.TryGetRoot(out root) && root != null;
    }

    /// <summary>
    /// Gets the root node of the syntax tree asynchronously.
    /// </summary>
    /// <returns>
    /// The returned <see cref="SyntaxNode"/> will be <see langword="null"/> if <see
    /// cref="SupportsSyntaxTree"/> returns <see langword="false"/>.  This function will return
    /// the same value if called multiple times.
    /// </returns>
    public async Task<SyntaxNode?> GetSyntaxRootAsync(CancellationToken cancellationToken = default)
    {
        if (!this.SupportsSyntaxTree)
        {
            return null;
        }

        var tree = (await this.GetSyntaxTreeAsync(cancellationToken).ConfigureAwait(false))!;
        return await tree.GetRootAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Only for features that absolutely must run synchronously (probably because they're
    /// on the UI thread).  Right now, the only feature this is for is Outlining as VS will
    /// block on that feature from the UI thread when a document is opened.
    /// </summary>
    internal SyntaxNode? GetSyntaxRootSynchronously(CancellationToken cancellationToken)
    {
        if (!this.SupportsSyntaxTree)
        {
            return null;
        }

        var tree = this.GetSyntaxTreeSynchronously(cancellationToken)!;
        return tree.GetRoot(cancellationToken);
    }

    /// <summary>
    /// Gets the current semantic model for this document if the model is already computed and still cached.
    /// In almost all cases, you should call <see cref="GetSemanticModelAsync(CancellationToken)"/>, which will compute the semantic model
    /// if necessary.
    /// </summary>
    public bool TryGetSemanticModel([NotNullWhen(returnValue: true)] out SemanticModel? semanticModel)
    {
        semanticModel = null;
        return _model != null && _model.TryGetTarget(out semanticModel);
    }

    /// <summary>
    /// Gets the current nullable disabled semantic model for this document if the model is already computed and still cached.
    /// In almost all cases, you should call <see cref="GetSemanticModelAsync(CancellationToken)"/>, which will compute the semantic model
    /// if necessary.
    /// </summary>
    internal bool TryGetNullableDisabledSemanticModel([NotNullWhen(returnValue: true)] out SemanticModel? semanticModel)
    {
        semanticModel = null;
        return _nullableDisabledModel != null && _nullableDisabledModel.TryGetTarget(out semanticModel);
    }

    /// <summary>
    /// Gets the nullable disabled semantic model for this document asynchronously.
    /// </summary>
    /// <returns>
    /// The returned <see cref="SemanticModel"/> may be <see langword="null"/> if <see
    /// cref="SupportsSemanticModel"/> returns <see langword="false"/>. This function will
    /// return the same value if called multiple times.
    /// </returns>
#pragma warning disable RSEXPERIMENTAL001 // sym-shipped usage of experimental API
    internal async Task<SemanticModel?> GetSemanticModelAsync(SemanticModelOptions options, CancellationToken cancellationToken = default)
    {
        return await GetSemanticModelHelperAsync(
            disableNullableAnalysis: (options & SemanticModelOptions.DisableNullableAnalysis) == SemanticModelOptions.DisableNullableAnalysis,
            cancellationToken
        ).ConfigureAwait(false);
    }
#pragma warning restore RSEXPERIMENTAL001

    // If the API used in this method is made public, we can consider moving this method to DocumentExtensions.
    internal async ValueTask<SemanticModel> GetRequiredNullableDisabledSemanticModelAsync(CancellationToken cancellationToken)
    {
        if (this.TryGetNullableDisabledSemanticModel(out var semanticModel))
            return semanticModel;

#pragma warning disable RSEXPERIMENTAL001 // Sym-shipped usage of experimental API
        semanticModel = await this.GetSemanticModelAsync(SemanticModelOptions.DisableNullableAnalysis, cancellationToken).ConfigureAwait(false);
#pragma warning restore RSEXPERIMENTAL001
        return semanticModel ?? throw new InvalidOperationException(string.Format(WorkspaceExtensionsResources.SyntaxTree_is_required_to_accomplish_the_task_but_is_not_supported_by_document_0, this.Name));
    }

    /// <summary>
    /// Gets the semantic model for this document asynchronously.
    /// </summary>
    /// <returns>
    /// The returned <see cref="SemanticModel"/> may be <see langword="null"/> if <see
    /// cref="SupportsSemanticModel"/> returns <see langword="false"/>. This function will
    /// return the same value if called multiple times.
    /// </returns>
    public Task<SemanticModel?> GetSemanticModelAsync(CancellationToken cancellationToken = default)
    {
        return GetSemanticModelHelperAsync(disableNullableAnalysis: false, cancellationToken);
    }

    /// <summary>
    /// Gets the semantic model for this document asynchronously.
    /// </summary>
    /// <returns>
    /// The returned <see cref="SemanticModel"/> may be <see langword="null"/> if <see
    /// cref="SupportsSemanticModel"/> returns <see langword="false"/>. This function will
    /// return the same value if called multiple times.
    /// </returns>
    private async Task<SemanticModel?> GetSemanticModelHelperAsync(bool disableNullableAnalysis, CancellationToken cancellationToken)
    {
        if (!this.SupportsSemanticModel)
            return null;

        var semanticModel = await GetSemanticModelWorkerAsync().ConfigureAwait(false);
        this.Project.Solution.OnSemanticModelObtained(this.Id, semanticModel);
        return semanticModel;

        async Task<SemanticModel> GetSemanticModelWorkerAsync()
        {
            try
            {
                if (disableNullableAnalysis)
                {
                    if (this.TryGetNullableDisabledSemanticModel(out var semanticModel))
                        return semanticModel;
                }
                else
                {
                    if (this.TryGetSemanticModel(out var semanticModel))
                        return semanticModel;
                }

                var syntaxTree = await this.GetRequiredSyntaxTreeAsync(cancellationToken).ConfigureAwait(false);
                var compilation = await this.Project.GetRequiredCompilationAsync(cancellationToken).ConfigureAwait(false);

#pragma warning disable RSEXPERIMENTAL001 // sym-shipped usage of experimental API
                var result = compilation.GetSemanticModel(syntaxTree, disableNullableAnalysis ? SemanticModelOptions.DisableNullableAnalysis : SemanticModelOptions.None);
#pragma warning restore RSEXPERIMENTAL001
                Contract.ThrowIfNull(result);
                var original = Interlocked.CompareExchange(ref disableNullableAnalysis ? ref _nullableDisabledModel : ref _model, new WeakReference<SemanticModel>(result), null);

                // okay, it is first time.
                if (original == null)
                    return result;

                // It looks like someone has set it. Try to reuse same semantic model, or assign the new model if that
                // fails. The lock is required since there is no compare-and-set primitive for WeakReference<T>.
                lock (original)
                {
                    if (original.TryGetTarget(out var semanticModel))
                        return semanticModel;

                    original.SetTarget(result);
                    return result;
                }
            }
            catch (Exception e) when (FatalError.ReportAndPropagateUnlessCanceled(e, cancellationToken, ErrorSeverity.Critical))
            {
                throw ExceptionUtilities.Unreachable();
            }
        }
    }

    /// <summary>
    /// Creates a new instance of this document updated to have the source code kind specified.
    /// </summary>
    public Document WithSourceCodeKind(SourceCodeKind kind)
        => this.Project.Solution.WithDocumentSourceCodeKind(this.Id, kind).GetRequiredDocument(Id);

    /// <summary>
    /// Creates a new instance of this document updated to have the text specified.
    /// </summary>
    public Document WithText(SourceText text)
    {
        var solution = this.Project.Solution.WithDocumentText(this.Id, text, PreservationMode.PreserveIdentity);

        if (Id.IsSourceGenerated)
        {
            // We just modified the text of the generated document, so it should be available synchronously, and throwing is appropriate if it isn't.
            return solution.GetRequiredSourceGeneratedDocumentForAlreadyGeneratedId(Id);
        }

        return solution.GetRequiredDocument(Id);
    }

    /// <summary>
    /// Creates a new instance of this document updated to have a syntax tree rooted by the specified syntax node.
    /// </summary>
    public Document WithSyntaxRoot(SyntaxNode root)
    {
        var solution = this.Project.Solution.WithDocumentSyntaxRoot(this.Id, root, PreservationMode.PreserveIdentity);

        if (Id.IsSourceGenerated)
        {
            // We just modified the text of the generated document, so it should be available synchronously, and throwing is appropriate if it isn't.
            return solution.GetRequiredSourceGeneratedDocumentForAlreadyGeneratedId(Id);
        }

        return solution.GetRequiredDocument(Id);
    }

    /// <summary>
    /// Creates a new instance of this document updated to have the specified name.
    /// </summary>
    public Document WithName(string name)
        => this.Project.Solution.WithDocumentName(this.Id, name).GetRequiredDocument(Id);

    /// <summary>
    /// Creates a new instance of this document updated to have the specified folders.
    /// </summary>
    public Document WithFolders(IEnumerable<string> folders)
        => this.Project.Solution.WithDocumentFolders(this.Id, folders).GetRequiredDocument(Id);

    /// <summary>
    /// Creates a new instance of this document updated to have the specified file path.
    /// </summary>
    public Document WithFilePath(string? filePath)
        => this.Project.Solution.WithDocumentFilePath(this.Id, filePath).GetRequiredDocument(Id);

    /// <summary>
    /// Get the text changes between this document and a prior version of the same document. The changes, when applied
    /// to the text of the old document, will produce the text of the current document.
    /// </summary>
    public async Task<IEnumerable<TextChange>> GetTextChangesAsync(Document oldDocument, CancellationToken cancellationToken = default)
    {
        return await GetTextChangesAsync(useAsync: true, oldDocument, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Similar to <see cref="GetTextChangesAsync(Document, CancellationToken)"/>, but should be used when in a forced
    /// synchronous context.
    /// </summary>
    internal ImmutableArray<TextChange> GetTextChangesSynchronously(
        Document oldDocument, CancellationToken cancellationToken)
    {
        // Should always complete synchronously since we passed in 'useAsync: false'
        var result = GetTextChangesAsync(useAsync: false, oldDocument, cancellationToken);
        return result.VerifyCompleted();
    }

    private async Task<ImmutableArray<TextChange>> GetTextChangesAsync(
        bool useAsync, Document oldDocument, CancellationToken cancellationToken)
    {
        try
        {
            using (Logger.LogBlock(FunctionId.Workspace_Document_GetTextChanges, this.Name, cancellationToken))
            {
                // no changes
                if (oldDocument == this)
                    return [];

                if (this.Id != oldDocument.Id)
                {
                    throw new ArgumentException(WorkspacesResources.The_specified_document_is_not_a_version_of_this_document);
                }

                // first try to see if text already knows its changes
                if (this.TryGetText(out var text) && oldDocument.TryGetText(out var oldText))
                {
                    if (text == oldText)
                        return [];

                    var container = text.Container;
                    if (container != null)
                    {
                        var textChanges = text.GetTextChanges(oldText).ToImmutableArray();

                        // if changes are significant (not the whole document being replaced) then use these changes
                        if (textChanges.Length > 1 || (textChanges.Length == 1 && textChanges[0].Span != new TextSpan(0, oldText.Length)))
                        {
                            return textChanges;
                        }
                    }
                }

                // get changes by diffing the trees
                if (this.SupportsSyntaxTree)
                {
                    var tree = useAsync ? await GetSyntaxTreeAsync(cancellationToken).ConfigureAwait(false) : this.GetSyntaxTreeSynchronously(cancellationToken);
                    var oldTree = useAsync ? await oldDocument.GetSyntaxTreeAsync(cancellationToken).ConfigureAwait(false) : oldDocument.GetSyntaxTreeSynchronously(cancellationToken);

                    RoslynDebug.Assert(tree is object);
                    RoslynDebug.Assert(oldTree is object);
                    return [.. tree.GetChanges(oldTree)];
                }

                text = useAsync ? await this.GetTextAsync(cancellationToken).ConfigureAwait(false) : this.GetTextSynchronously(cancellationToken);
                oldText = useAsync ? await oldDocument.GetTextAsync(cancellationToken).ConfigureAwait(false) : oldDocument.GetTextSynchronously(cancellationToken);

                return [.. text.GetTextChanges(oldText)];
            }
        }
        catch (Exception e) when (FatalError.ReportAndPropagateUnlessCanceled(e, cancellationToken))
        {
            throw ExceptionUtilities.Unreachable();
        }
    }

    /// <summary>
    /// Gets the list of <see cref="DocumentId"/>s that are linked to this
    /// <see cref="Document" />. <see cref="Document"/>s are considered to be linked if they
    /// share the same <see cref="TextDocument.FilePath" />. This <see cref="DocumentId"/> is excluded from the
    /// result.
    /// </summary>
    public ImmutableArray<DocumentId> GetLinkedDocumentIds()
    {
        var filteredDocumentIds = this.Project.Solution.GetRelatedDocumentIds(this.Id);
        return filteredDocumentIds.Remove(this.Id);
    }

    /// <inheritdoc cref="WithFrozenPartialSemantics(bool, CancellationToken)"/>
    internal Document WithFrozenPartialSemantics(CancellationToken cancellationToken)
        => WithFrozenPartialSemantics(forceFreeze: false, cancellationToken);

    /// <summary>
    /// Creates a branched version of this document that has its semantic model frozen in whatever state it is available
    /// at the time, assuming a background process is constructing the semantics asynchronously. Repeated calls to this
    /// method may return documents with increasingly more complete semantics.
    /// <para/>
    /// Use this method to gain access to potentially incomplete semantics quickly.
    /// <para/> Note: this will give back a solution where this <see cref="Document"/>'s project will not run generators
    /// when getting its compilation.  However, all other projects will still run generators when their compilations are
    /// requested.
    /// </summary>
    /// <param name="forceFreeze">If <see langword="true"/> then a forked document will be returned no matter what. This
    /// should be used when the caller wants to ensure that further forks of that document will remain frozen and will
    /// not run generators/skeletons. For example, if it is about to transform the document many times, and is fine with
    /// the original semantic information they started with.  If <see langword="false"/> then this same document may be
    /// returned if the compilation for its <see cref="Project"/> was already produced.  In this case, generators and
    /// skeletons will already have been run, so returning the same instance will be fast when getting semantics.
    /// However, this does mean that future forks of this instance will continue running generators/skeletons.  This
    /// should be used for most clients that intend to just query for semantic information and do not intend to make any
    /// further changes.
    /// </param>
    internal virtual Document WithFrozenPartialSemantics(bool forceFreeze, CancellationToken cancellationToken)
    {
        if (!forceFreeze && this.Project.TryGetCompilation(out _))
            return this;

        var solution = this.Project.Solution;

        // only produce doc with frozen semantics if this workspace has support for that, as without
        // background compilation the semantics won't be moving toward completeness.  Also,
        // ensure that the project that this document is part of actually supports compilations,
        // as partial semantics don't make sense otherwise.
        if (solution.PartialSemanticsEnabled &&
            this.Project.SupportsCompilation)
        {
            var newSolution = this.Project.Solution.WithFrozenPartialCompilationIncludingSpecificDocument(this.Id, cancellationToken);
            return newSolution.GetRequiredDocument(this.Id);
        }
        else
        {
            return this;
        }
    }

    private string GetDebuggerDisplay()
        => this.Name;

#pragma warning disable RS0030 // Do not used banned APIs (backwards compat)
    private AsyncLazy<DocumentOptionSet>? _cachedOptions;

    /// <summary>
    /// Returns the options that should be applied to this document. This consists of global options from <see cref="Solution.Options"/>,
    /// merged with any settings the user has specified at the document levels.
    /// </summary>
    /// <remarks>
    /// This method is async because this may require reading other files. In files that are already open, this is expected to be cheap and complete synchronously.
    /// </remarks>
    [PerformanceSensitive("https://github.com/dotnet/roslyn/issues/23582", AllowCaptures = false)]
    public Task<DocumentOptionSet> GetOptionsAsync(CancellationToken cancellationToken = default)
    {
        if (_cachedOptions == null)
        {
            InitializeCachedOptions(Project.Solution.Options);
        }

        Contract.ThrowIfNull(_cachedOptions);
        return _cachedOptions.GetValueAsync(cancellationToken);
    }

    private void InitializeCachedOptions(OptionSet solutionOptions)
    {
        var newAsyncLazy = AsyncLazy.Create(static async (arg, cancellationToken) =>
        {
            var options = await arg.self.GetHostAnalyzerConfigOptionsAsync(cancellationToken).ConfigureAwait(false);
            return new DocumentOptionSet(options, arg.solutionOptions, arg.self.Project.Language);
        },
        arg: (self: this, solutionOptions));

        Interlocked.CompareExchange(ref _cachedOptions, newAsyncLazy, comparand: null);
    }
#pragma warning restore

    internal async ValueTask<StructuredAnalyzerConfigOptions> GetHostAnalyzerConfigOptionsAsync(CancellationToken cancellationToken)
    {
        var provider = (ProjectState.ProjectHostAnalyzerConfigOptionsProvider)Project.State.HostAnalyzerOptions.AnalyzerConfigOptionsProvider;
        return await provider.GetOptionsAsync(DocumentState, cancellationToken).ConfigureAwait(false);
    }

    internal ValueTask<ImmutableArray<byte>> GetContentHashAsync(CancellationToken cancellationToken)
        => this.DocumentState.GetContentHashAsync(cancellationToken);
}
