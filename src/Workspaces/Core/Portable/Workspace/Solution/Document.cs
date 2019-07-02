// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis
{
    /// <summary>
    /// Represents a source code document that is part of a project.
    /// It provides access to the source text, parsed syntax tree and the corresponding semantic model.
    /// </summary>
    [DebuggerDisplay("{GetDebuggerDisplay(),nq}")]
    public partial class Document : TextDocument
    {
        private WeakReference<SemanticModel> _model;
        private Task<SyntaxTree> _syntaxTreeResultTask;

        internal Document(Project project, DocumentState state)
            : base(project, state, TextDocumentKind.Document)
        {
        }

        private DocumentState DocumentState => (DocumentState)State;

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

        internal bool HasTextChanged(Document otherDocument)
        {
            return DocumentState.HasTextChanged(otherDocument.DocumentState);
        }

        /// <summary>
        /// Get the current syntax tree for the document if the text is already loaded and the tree is already parsed.
        /// In almost all cases, you should call <see cref="GetSyntaxTreeAsync"/> to fetch the tree, which will parse the tree
        /// if it's not already parsed.
        /// </summary>
        public bool TryGetSyntaxTree(out SyntaxTree syntaxTree)
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
        {
            return DocumentState.TryGetTopLevelChangeTextVersion(out version);
        }

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
        /// <see cref="GetSemanticModelAsync"/> method.
        /// 
        /// If <see langword="false"/> then this method will return <see langword="null"/> instead.
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
        public Task<SyntaxTree> GetSyntaxTreeAsync(CancellationToken cancellationToken = default)
        {
            // If the language doesn't support getting syntax trees for a document, then bail out immediately.
            if (!this.SupportsSyntaxTree)
            {
                return SpecializedTasks.Default<SyntaxTree>();
            }

            // if we have a cached result task use it
            if (_syntaxTreeResultTask != null)
            {
                return _syntaxTreeResultTask;
            }
            // check to see if we already have the tree before actually going async
            if (TryGetSyntaxTree(out var tree))
            {
                // stash a completed result task for this value for the next request (to reduce extraneous allocations of tasks)
                // don't use the actual async task because it depends on a specific cancellation token
                // its okay to cache the task and hold onto the SyntaxTree, because the DocumentState already keeps the SyntaxTree alive.
                Interlocked.CompareExchange(ref _syntaxTreeResultTask, Task.FromResult(tree), null);
                return _syntaxTreeResultTask;
            }

            // do it async for real.
            return DocumentState.GetSyntaxTreeAsync(cancellationToken).AsTask();
        }

        internal SyntaxTree GetSyntaxTreeSynchronously(CancellationToken cancellationToken)
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
        public bool TryGetSyntaxRoot(out SyntaxNode root)
        {
            root = null;
            return this.TryGetSyntaxTree(out var tree) && tree.TryGetRoot(out root) && root != null;
        }

        /// <summary>
        /// Gets the root node of the syntax tree asynchronously.
        /// </summary>
        public async Task<SyntaxNode> GetSyntaxRootAsync(CancellationToken cancellationToken = default)
        {
            if (!this.SupportsSyntaxTree)
            {
                return null;
            }

            var tree = await this.GetSyntaxTreeAsync(cancellationToken).ConfigureAwait(false);
            return await tree.GetRootAsync(cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Only for features that absolutely must run synchronously (probably because they're
        /// on the UI thread).  Right now, the only feature this is for is Outlining as VS will
        /// block on that feature from the UI thread when a document is opened.
        /// </summary>
        internal SyntaxNode GetSyntaxRootSynchronously(CancellationToken cancellationToken)
        {
            if (!this.SupportsSyntaxTree)
            {
                return null;
            }

            var tree = this.GetSyntaxTreeSynchronously(cancellationToken);
            return tree.GetRoot(cancellationToken);
        }

        /// <summary>
        /// Gets the current semantic model for this document if the model is already computed and still cached.
        /// In almost all cases, you should call <see cref="GetSemanticModelAsync"/>, which will compute the semantic model
        /// if necessary.
        /// </summary>
        public bool TryGetSemanticModel(out SemanticModel semanticModel)
        {
            semanticModel = null;
            return _model != null && _model.TryGetTarget(out semanticModel);
        }

        /// <summary>
        /// Gets the semantic model for this document asynchronously.
        /// </summary>
        public async Task<SemanticModel> GetSemanticModelAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                if (!this.SupportsSemanticModel)
                {
                    return null;
                }

                if (this.TryGetSemanticModel(out var semanticModel))
                {
                    return semanticModel;
                }

                var syntaxTree = await this.GetSyntaxTreeAsync(cancellationToken).ConfigureAwait(false);
                var compilation = await this.Project.GetCompilationAsync(cancellationToken).ConfigureAwait(false);

                var result = compilation.GetSemanticModel(syntaxTree);
                Contract.ThrowIfNull(result);

                // first try set the cache if it has not been set
                var original = Interlocked.CompareExchange(ref _model, new WeakReference<SemanticModel>(result), null);

                // okay, it is first time.
                if (original == null)
                {
                    return result;
                }

                // It looks like someone has set it. Try to reuse same semantic model, or assign the new model if that
                // fails. The lock is required since there is no compare-and-set primitive for WeakReference<T>.
                lock (original)
                {
                    if (original.TryGetTarget(out semanticModel))
                    {
                        return semanticModel;
                    }

                    original.SetTarget(result);
                    return result;
                }
            }
            catch (Exception e) when (FatalError.ReportUnlessCanceled(e))
            {
                throw ExceptionUtilities.Unreachable;
            }
        }

        /// <summary>
        /// Creates a new instance of this document updated to have the source code kind specified.
        /// </summary>
        public Document WithSourceCodeKind(SourceCodeKind kind)
        {
            return this.Project.Solution.WithDocumentSourceCodeKind(this.Id, kind).GetDocument(this.Id);
        }

        /// <summary>
        /// Creates a new instance of this document updated to have the text specified.
        /// </summary>
        public Document WithText(SourceText text)
        {
            return this.Project.Solution.WithDocumentText(this.Id, text, PreservationMode.PreserveIdentity).GetDocument(this.Id);
        }

        /// <summary>
        /// Creates a new instance of this document updated to have a syntax tree rooted by the specified syntax node.
        /// </summary>
        public Document WithSyntaxRoot(SyntaxNode root)
        {
            return this.Project.Solution.WithDocumentSyntaxRoot(this.Id, root, PreservationMode.PreserveIdentity).GetDocument(this.Id);
        }

        /// <summary>
        /// Creates a new instance of this document updated to have the specified name.
        /// </summary>
        public Document WithName(string name)
        {
            return this.Project.Solution.WithDocumentName(this.Id, name).GetDocument(this.Id);
        }

        /// <summary>
        /// Creates a new instance of this document updated to have the specified folders.
        /// </summary>
        public Document WithFolders(IEnumerable<string> folders)
        {
            return this.Project.Solution.WithDocumentFolders(this.Id, folders).GetDocument(this.Id);
        }

        /// <summary>
        /// Creates a new instance of this document updated to have the specified file path.
        /// </summary>
        /// <param name="filePath"></param>
        public Document WithFilePath(string filePath)
        {
            return this.Project.Solution.WithDocumentFilePath(this.Id, filePath).GetDocument(this.Id);
        }

        /// <summary>
        /// Get the text changes between this document and a prior version of the same document.
        /// The changes, when applied to the text of the old document, will produce the text of the current document.
        /// </summary>
        public async Task<IEnumerable<TextChange>> GetTextChangesAsync(Document oldDocument, CancellationToken cancellationToken = default)
        {
            try
            {
                using (Logger.LogBlock(FunctionId.Workspace_Document_GetTextChanges, this.Name, cancellationToken))
                {
                    if (oldDocument == this)
                    {
                        // no changes
                        return SpecializedCollections.EmptyEnumerable<TextChange>();
                    }

                    if (this.Id != oldDocument.Id)
                    {
                        throw new ArgumentException(WorkspacesResources.The_specified_document_is_not_a_version_of_this_document);
                    }

                    // first try to see if text already knows its changes
                    IList<TextChange> textChanges = null;
                    if (this.TryGetText(out var text) && oldDocument.TryGetText(out var oldText))
                    {
                        if (text == oldText)
                        {
                            return SpecializedCollections.EmptyEnumerable<TextChange>();
                        }

                        var container = text.Container;
                        if (container != null)
                        {
                            textChanges = text.GetTextChanges(oldText).ToList();

                            // if changes are significant (not the whole document being replaced) then use these changes
                            if (textChanges.Count > 1 || (textChanges.Count == 1 && textChanges[0].Span != new TextSpan(0, oldText.Length)))
                            {
                                return textChanges;
                            }
                        }
                    }

                    // get changes by diffing the trees
                    if (this.SupportsSyntaxTree)
                    {
                        var tree = await this.GetSyntaxTreeAsync(cancellationToken).ConfigureAwait(false);
                        var oldTree = await oldDocument.GetSyntaxTreeAsync(cancellationToken).ConfigureAwait(false);

                        return tree.GetChanges(oldTree);
                    }

                    text = await this.GetTextAsync(cancellationToken).ConfigureAwait(false);
                    oldText = await oldDocument.GetTextAsync(cancellationToken).ConfigureAwait(false);

                    return text.GetTextChanges(oldText).ToList();
                }
            }
            catch (Exception e) when (FatalError.ReportUnlessCanceled(e))
            {
                throw ExceptionUtilities.Unreachable;
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
            var documentIdsWithPath = this.Project.Solution.GetDocumentIdsWithFilePath(this.FilePath);
            var filteredDocumentIds = this.Project.Solution.FilterDocumentIdsByLanguage(documentIdsWithPath, this.Project.Language).ToImmutableArray();
            return filteredDocumentIds.Remove(this.Id);
        }

        /// <summary>
        /// Creates a branched version of this document that has its semantic model frozen in whatever state it is available at the time,
        /// assuming a background process is constructing the semantics asynchronously. Repeated calls to this method may return
        /// documents with increasingly more complete semantics.
        ///
        /// Use this method to gain access to potentially incomplete semantics quickly.
        /// </summary>
        internal Document WithFrozenPartialSemantics(CancellationToken cancellationToken)
        {
            var solution = this.Project.Solution;
            var workspace = solution.Workspace;

            // only produce doc with frozen semantics if this document is part of the workspace's
            // primary branch and there is actual background compilation going on, since w/o
            // background compilation the semantics won't be moving toward completeness.  Also,
            // ensure that the project that this document is part of actually supports compilations,
            // as partial semantics don't make sense otherwise.
            if (solution.BranchId == workspace.PrimaryBranchId &&
                workspace.PartialSemanticsEnabled &&
                this.Project.SupportsCompilation)
            {
                var newSolution = this.Project.Solution.WithFrozenPartialCompilationIncludingSpecificDocument(this.Id, cancellationToken);
                return newSolution.GetDocument(this.Id);
            }
            else
            {
                return this;
            }
        }

        private string GetDebuggerDisplay()
        {
            return this.Name;
        }

        private AsyncLazy<DocumentOptionSet> _cachedOptions;

        /// <summary>
        /// Returns the options that should be applied to this document. This consists of global options from <see cref="Solution.Options"/>,
        /// merged with any settings the user has specified at the document levels.
        /// </summary>
        /// <remarks>
        /// This method is async because this may require reading other files. In files that are already open, this is expected to be cheap and complete synchronously.
        /// </remarks>
        public Task<DocumentOptionSet> GetOptionsAsync(CancellationToken cancellationToken = default)
        {
            return GetOptionsAsync(Project.Solution.Options, cancellationToken);
        }

        [PerformanceSensitive("https://github.com/dotnet/roslyn/issues/23582", AllowCaptures = false)]
        internal Task<DocumentOptionSet> GetOptionsAsync(OptionSet solutionOptions, CancellationToken cancellationToken)
        {
            // TODO: we have this workaround since Solution.Options is not actually snapshot but just return Workspace.Options which violate snapshot model.
            //       this doesn't validate whether same optionset is given to invalidate the cache or not. this is not new since existing implementation
            //       also didn't check whether Workspace.Option is same as before or not. all wierd-ness come from the root cause of Solution.Options violating
            //       snapshot model. once that is fixed, we can remove this workaround - https://github.com/dotnet/roslyn/issues/19284
            if (_cachedOptions == null)
            {
                InitializeCachedOptions(solutionOptions);
            }

            return _cachedOptions.GetValueAsync(cancellationToken);
        }

        private void InitializeCachedOptions(OptionSet solutionOptions)
        {
            var newAsyncLazy = new AsyncLazy<DocumentOptionSet>(async c =>
            {
                var optionsService = Project.Solution.Workspace.Services.GetRequiredService<IOptionService>();
                var documentOptionSet = await optionsService.GetUpdatedOptionSetForDocumentAsync(this, solutionOptions, c).ConfigureAwait(false);
                return new DocumentOptionSet(documentOptionSet, Project.Language);
            }, cacheResult: true);

            Interlocked.CompareExchange(ref _cachedOptions, newAsyncLazy, comparand: null);
        }

        internal Task<ImmutableDictionary<string, string>> GetAnalyzerOptionsAsync(CancellationToken cancellationToken)
        {
            return DocumentState.GetAnalyzerOptionsAsync(Project.FilePath, cancellationToken);
        }
    }
}
