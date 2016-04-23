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
        private readonly DocumentState _state;

        private WeakReference<SemanticModel> _model;
        private Task<SyntaxTree> _syntaxTreeResultTask;

        internal Document(Project project, DocumentState state)
        {
            Contract.ThrowIfNull(project);
            Contract.ThrowIfNull(state);

            this.Project = project;
            _state = state;
        }

        internal DocumentState State
        {
            get
            {
                return _state;
            }
        }

        internal override TextDocumentState GetDocumentState()
        {
            return _state;
        }

        /// <summary>
        /// The kind of source code this document contains.
        /// </summary>
        public SourceCodeKind SourceCodeKind
        {
            get
            {
                return _state.SourceCodeKind;
            }
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
            }

            if (!_state.TryGetSyntaxTree(out syntaxTree))
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
            version = default(VersionStamp);

            VersionStamp textVersion;
            if (!this.TryGetTextVersion(out textVersion))
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
            return _state.TryGetTopLevelChangeTextVersion(out version);
        }

        /// <summary>
        /// Gets the version of the syntax tree. This is generally the newer of the text version and the project's version.
        /// </summary>
        public async Task<VersionStamp> GetSyntaxVersionAsync(CancellationToken cancellationToken = default(CancellationToken))
        {
            var textVersion = await this.GetTextVersionAsync(cancellationToken).ConfigureAwait(false);
            var projectVersion = this.Project.Version;
            return textVersion.GetNewerVersion(projectVersion);
        }


        /// <summary>
        /// <code>true</code> if this Document supports providing data through the
        /// <see cref="GetSyntaxTreeAsync"/> and <see cref="GetSyntaxRootAsync"/> methods.
        /// 
        /// If <code>false</code> then these methods will return <code>null</code> instead.
        /// </summary>
        public bool SupportsSyntaxTree
        {
            get
            {
                return this.State.SupportsSyntaxTree;
            }
        }

        /// <summary>
        /// <code>true</code> if this Document supports providing data through the
        /// <see cref="GetSemanticModelAsync"/> method.
        /// 
        /// If <code>false</code> then this method will return <code>null</code> instead.
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
        public Task<SyntaxTree> GetSyntaxTreeAsync(CancellationToken cancellationToken = default(CancellationToken))
        {
            // If the language doesn't support getting syntax trees for a document, then bail out
            // immediately.
            if (!this.SupportsSyntaxTree)
            {
                return SpecializedTasks.Default<SyntaxTree>();
            }

            if (_syntaxTreeResultTask != null)
            {
                return _syntaxTreeResultTask;
            }

            // First see if we already have a semantic model computed.  If so, we can just return
            // that syntax tree.
            SemanticModel semanticModel;
            if (TryGetSemanticModel(out semanticModel))
            {
                // PERF: This is a hot code path, so cache the result to reduce allocations
                var result = Task.FromResult(semanticModel.SyntaxTree);
                Interlocked.CompareExchange(ref _syntaxTreeResultTask, result, null);
                return _syntaxTreeResultTask;
            }

            // second, see whether we already computed the tree, if we already did, return the cache
            SyntaxTree tree;
            if (TryGetSyntaxTree(out tree))
            {
                if (_syntaxTreeResultTask == null)
                {
                    var result = Task.FromResult(tree);
                    Interlocked.CompareExchange(ref _syntaxTreeResultTask, result, null);
                }

                return _syntaxTreeResultTask;
            }

            // we can't cache this result, since internally it uses AsyncLazy which
            // care about cancellation token
            return _state.GetSyntaxTreeAsync(cancellationToken);
        }

        /// <summary>
        /// Gets the root node of the current syntax tree if the syntax tree has already been parsed and the tree is still cached.
        /// In almost all cases, you should call <see cref="GetSyntaxRootAsync"/> to fetch the root node, which will parse
        /// the document if necessary.
        /// </summary>
        public bool TryGetSyntaxRoot(out SyntaxNode root)
        {
            root = null;
            SyntaxTree tree;
            return this.TryGetSyntaxTree(out tree) && tree.TryGetRoot(out root) && root != null;
        }

        /// <summary>
        /// Gets the root node of the syntax tree asynchronously.
        /// </summary>
        public async Task<SyntaxNode> GetSyntaxRootAsync(CancellationToken cancellationToken = default(CancellationToken))
        {
            if (!this.SupportsSyntaxTree)
            {
                return null;
            }

            var tree = await this.GetSyntaxTreeAsync(cancellationToken).ConfigureAwait(false);
            return await tree.GetRootAsync(cancellationToken).ConfigureAwait(false);
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
        public async Task<SemanticModel> GetSemanticModelAsync(CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                if (!this.SupportsSemanticModel)
                {
                    return null;
                }

                SemanticModel semanticModel;
                if (this.TryGetSemanticModel(out semanticModel))
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

                // it looks like someone has set it. try to reuse same semantic model
                if (original.TryGetTarget(out semanticModel))
                {
                    return semanticModel;
                }

                // it looks like cache is gone. reset the cache.
                original.SetTarget(result);
                return result;
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
        /// Get the text changes between this document and a prior version of the same document.
        /// The changes, when applied to the text of the old document, will produce the text of the current document.
        /// </summary>
        public async Task<IEnumerable<TextChange>> GetTextChangesAsync(Document oldDocument, CancellationToken cancellationToken = default(CancellationToken))
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
                        throw new ArgumentException(WorkspacesResources.DocumentVersionIsDifferent);
                    }

                    // first try to see if text already knows its changes
                    IList<TextChange> textChanges = null;

                    SourceText text;
                    SourceText oldText;
                    if (this.TryGetText(out text) && oldDocument.TryGetText(out oldText))
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
        internal async Task<Document> WithFrozenPartialSemanticsAsync(CancellationToken cancellationToken)
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
                var newSolution = await this.Project.Solution.WithFrozenPartialCompilationIncludingSpecificDocumentAsync(this.Id, cancellationToken).ConfigureAwait(false);
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
    }
}
