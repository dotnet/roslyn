// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Host
{
    internal abstract partial class AbstractSyntaxTreeFactoryService
    {
        /// <summary>
        /// Represents all the state used by a language-specific recoverable syntax tree
        /// </summary>
        internal abstract class AbstractRecoverableSyntaxRoot<TRoot> where TRoot : SyntaxNode
        {
            // information needed to either recreate tree or feed into wrapping tree
            private readonly AbstractSyntaxTreeFactoryService service;
            private readonly string filePath;
            private readonly ParseOptions options;
            private readonly ValueSource<TextAndVersion> textSource;
            private readonly int length;

            // root cache
            private readonly ISyntaxTreeCacheService syntaxTreeCache;
            private readonly IWeakAction<SyntaxNode> evictAction;

            // Use the "Gate" property to ensure lazy creation
            private AsyncSemaphore gateDontAccessDirectly;
            private IRecoverableSyntaxTree<TRoot> containingTree;

            // lazyRoot guard rootSource to be initialized only once when there are multiple requestor
            private AsyncLazy<TRoot> lazyRoot;

            // isSaved make sure it gets serialized only once
            private bool isSaved;

            // actual holder of the root
            private ValueSource<TRoot> rootSource;

            public AbstractRecoverableSyntaxRoot(
                AbstractSyntaxTreeFactoryService service,
                string filePath,
                ParseOptions options,
                ValueSource<TextAndVersion> text,
                TRoot root)
            {
                System.Diagnostics.Debug.Assert(service != null);
                System.Diagnostics.Debug.Assert(filePath != null);
                System.Diagnostics.Debug.Assert(options != null);
                System.Diagnostics.Debug.Assert(text != null);
                System.Diagnostics.Debug.Assert(root != null);

                this.service = service;
                this.filePath = filePath;
                this.options = options;
                this.textSource = text;
                this.length = root.FullSpan.Length;

                this.syntaxTreeCache = WorkspaceServices.GetService<ISyntaxTreeCacheService>();
                System.Diagnostics.Debug.Assert(this.syntaxTreeCache != null);

                this.evictAction = new WeakAction<AbstractRecoverableSyntaxRoot<TRoot>, SyntaxNode>(this, (r, d) => r.OnEvicted(d));

                this.rootSource = new ConstantValueSource<TRoot>(root);
            }

            /// <summary>
            /// Called by the constructor the parent tree.
            /// </summary>
            /// <remarks>
            /// This is to address the catch-22 dependency -- a tree needs it's recoverable root
            /// and vice versa. The dangerous bit is when we call TickleCache because it's the
            /// first place when this tree gets handed out to another system. We need to make sure
            /// both this object and the parent tree are fully constructed by that point.</remarks>
            public void SetContainingTree(IRecoverableSyntaxTree<TRoot> containingTree)
            {
                this.containingTree = containingTree;
                this.rootSource = new ConstantValueSource<TRoot>(containingTree.CloneNodeAsRoot(this.rootSource.GetValue()));
                this.TickleCache(this.rootSource.GetValue());
            }

            protected abstract Task<TRoot> RecoverRootAsync(CancellationToken cancellationToken);
            protected abstract TRoot RecoverRoot(CancellationToken cancellationToken);
            protected abstract Task SaveRootAsync(SyntaxTree tree, TRoot root, CancellationToken cancellationToken);

            private TRoot TickleCache(TRoot root)
            {
                // put the tree in the cache and manipulate root node retention when evicted.
                this.syntaxTreeCache.AddOrAccess(root, this.evictAction);

                return root;
            }

            protected HostWorkspaceServices WorkspaceServices
            {
                get { return this.service.languageServices.WorkspaceServices; }
            }

            protected ISyntaxTreeFactoryService Service
            {
                get { return this.service; }
            }

            protected ISyntaxTreeStorageService SyntaxTreeStorageService
            {
                get { return WorkspaceServices.GetService<ISyntaxTreeStorageService>(); }
            }

            protected ITemporaryStorageService TemporaryStorageService
            {
                get { return WorkspaceServices.GetService<ITemporaryStorageService>(); }
            }

            public string FilePath
            {
                get
                {
                    return this.filePath;
                }
            }

            public ParseOptions Options
            {
                get
                {
                    return this.options;
                }
            }

            public int Length
            {
                get { return this.length; }
            }

            public bool TryGetText(out SourceText text)
            {
                TextAndVersion textAndVersion;
                if (this.textSource.TryGetValue(out textAndVersion))
                {
                    text = textAndVersion.Text;
                    return true;
                }
                else
                {
                    text = null;
                    return false;
                }
            }

            public SourceText GetText(CancellationToken cancellationToken)
            {
                return textSource.GetValue(cancellationToken).Text;
            }

            public async Task<SourceText> GetTextAsync(CancellationToken cancellationToken)
            {
                var textAndVersion = await this.textSource.GetValueAsync(cancellationToken).ConfigureAwait(false);
                return textAndVersion.Text;
            }

            public bool TryGetRoot(out TRoot root)
            {
                if (this.rootSource.TryGetValue(out root))
                {
                    this.TickleCache(root);
                    return true;
                }

                return false;
            }

            public TRoot GetRoot(CancellationToken cancellationToken)
            {
                TRoot node;
                if (!this.rootSource.TryGetValue(out node))
                {
                    node = GetAsyncLazyRoot().GetValue(cancellationToken);
                }

                return this.TickleCache(node);
            }

            public async Task<TRoot> GetRootAsync(CancellationToken cancellationToken)
            {
                TRoot node;
                if (!this.rootSource.TryGetValue(out node))
                {
                    node = await GetAsyncLazyRoot().GetValueAsync(cancellationToken).ConfigureAwait(false);
                }

                return this.TickleCache(node);
            }

            private static readonly Func<AbstractRecoverableSyntaxRoot<TRoot>, AsyncLazy<TRoot>> rootFactory =
                a => new AsyncLazy<TRoot>(c => a.RestoreRootAsync(c), c => a.RestoreRoot(c), cacheResult: false);

            private AsyncLazy<TRoot> GetAsyncLazyRoot()
            {
                return LazyInitialization.EnsureInitialized(ref lazyRoot, rootFactory, this);
            }

            private AsyncSemaphore Gate
            {
                get
                {
                    return LazyInitialization.EnsureInitialized(ref gateDontAccessDirectly, AsyncSemaphore.Factory);
                }
            }

            private async void OnEvicted(SyntaxNode root)
            {
                var cu = (TRoot)root;

                // save tree if this is the first time evicted.
                if (!this.isSaved)
                {
                    using (await this.Gate.DisposableWaitAsync(CancellationToken.None).ConfigureAwait(false))
                    {
                        if (!this.isSaved)
                        {
                            await SaveRootAsync(this.containingTree as SyntaxTree, cu, CancellationToken.None).ConfigureAwait(false);
                            this.isSaved = true;
                        }
                    }
                }

                // replace root with weak value source to mimic no longer being cached
                this.rootSource = new WeakConstantValueSource<TRoot>(cu);
            }

            private async Task<TRoot> RestoreRootAsync(CancellationToken cancellationToken)
            {
                using (await this.Gate.DisposableWaitAsync(cancellationToken).ConfigureAwait(false))
                {
                    TRoot root;
                    if (!this.rootSource.TryGetValue(out root))
                    {
                        root = containingTree.CloneNodeAsRoot(await this.RecoverFromStorageIfPossibleAsync(cancellationToken).ConfigureAwait(false));
                        Contract.ThrowIfFalse(root.SyntaxTree == containingTree);

                        // now keep it around until we get evicted again
                        this.rootSource = new ConstantValueSource<TRoot>(root);
                    }

                    return root;
                }
            }

            private async Task<TRoot> RecoverFromStorageIfPossibleAsync(CancellationToken cancellationToken)
            {
                var storageService = this.SyntaxTreeStorageService;
                var tree = this.containingTree as SyntaxTree;

                // if we have this tree already in the storage, get one from it.
                if (storageService.CanRetrieve(tree))
                {
                    var node = await storageService.RetrieveAsync(tree, this.Service, cancellationToken).ConfigureAwait(false) as TRoot;
                    if (node != null)
                    {
                        return node;
                    }
                }

                return await this.RecoverRootAsync(cancellationToken).ConfigureAwait(false);
            }

            private TRoot RestoreRoot(CancellationToken cancellationToken)
            {
                using (this.Gate.DisposableWait(cancellationToken))
                {
                    TRoot root;
                    if (!this.rootSource.TryGetValue(out root))
                    {
                        root = containingTree.CloneNodeAsRoot(this.RecoverFromStorageIfPossible(cancellationToken));
                        Contract.ThrowIfFalse(root.SyntaxTree == containingTree);

                        // now keep it around until we get evicted again
                        this.rootSource = new ConstantValueSource<TRoot>(root);
                    }

                    return root;
                }
            }

            private TRoot RecoverFromStorageIfPossible(CancellationToken cancellationToken)
            {
                var storageService = this.SyntaxTreeStorageService;
                var tree = this.containingTree as SyntaxTree;

                // if we have this tree already in the storage, get one from it.
                if (storageService.CanRetrieve(tree))
                {
                    var node = storageService.Retrieve(tree, this.Service, cancellationToken) as TRoot;
                    if (node != null)
                    {
                        return node;
                    }
                }

                return this.RecoverRoot(cancellationToken);
            }
        }

        // a recoverable syntax tree that recovers its nodes from a serialized temporary storage
        internal class SerializedSyntaxRoot<TRoot> : AbstractRecoverableSyntaxRoot<TRoot> where TRoot : SyntaxNode
        {
            public SerializedSyntaxRoot(
                AbstractSyntaxTreeFactoryService service,
                string fileName,
                ParseOptions options,
                ValueSource<TextAndVersion> text,
                TRoot root)
                : base(service, fileName, options, text, root)
            {
            }

            protected override Task SaveRootAsync(SyntaxTree tree, TRoot root, CancellationToken cancellationToken)
            {
                // rather than we store it right away, we enqueue request to do so, until it happens, root will stay alive
                // in memory by store service
                this.SyntaxTreeStorageService.EnqueueStore(tree, root, this.TemporaryStorageService, cancellationToken);

                return SpecializedTasks.EmptyTask;
            }

            protected override Task<TRoot> RecoverRootAsync(CancellationToken cancellationToken)
            {
                Contract.Fail("we shouldn't reach here");
                return SpecializedTasks.Default<TRoot>();
            }

            protected override TRoot RecoverRoot(CancellationToken cancellationToken)
            {
                Contract.Fail("we shouldn't reach here");
                return default(TRoot);
            }
        }

        // a recoverable syntax tree that recovers its nodes by reparsing the text
        internal class ReparsedSyntaxRoot<TRoot> : AbstractRecoverableSyntaxRoot<TRoot> where TRoot : SyntaxNode
        {
            public ReparsedSyntaxRoot(
                AbstractSyntaxTreeFactoryService service,
                string fileName,
                ParseOptions options,
                ValueSource<TextAndVersion> text,
                TRoot root)
                : base(service, fileName, options, text, root)
            {
            }

            protected override Task SaveRootAsync(SyntaxTree tree, TRoot root, CancellationToken cancellationToken)
            {
                // before kicking out the tree, touch the text if it is still in the cache
                SourceText dummy;
                this.TryGetText(out dummy);

                // do nothing
                return SpecializedTasks.EmptyTask;
            }

            protected override async Task<TRoot> RecoverRootAsync(CancellationToken cancellationToken)
            {
                // get the text and parse it again
                var text = await this.GetTextAsync(cancellationToken).ConfigureAwait(false);
                return RecoverRootFromText(text, cancellationToken);
            }

            protected override TRoot RecoverRoot(CancellationToken cancellationToken)
            {
                return RecoverRootFromText(this.GetText(cancellationToken), cancellationToken);
            }

            private TRoot RecoverRootFromText(SourceText text, CancellationToken cancellationToken)
            {
                return (TRoot)this.Service.ParseSyntaxTree(this.FilePath, this.Options, text, cancellationToken).GetRoot(cancellationToken);
            }
        }
    }

    internal interface IRecoverableSyntaxTree<TRoot> where TRoot : SyntaxNode
    {
        TRoot CloneNodeAsRoot(TRoot root);
    }
}
