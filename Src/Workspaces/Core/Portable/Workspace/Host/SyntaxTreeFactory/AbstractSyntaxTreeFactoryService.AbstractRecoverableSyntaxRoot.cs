// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Host
{
    internal abstract partial class AbstractSyntaxTreeFactoryService
    {
        internal abstract class RecoverableSyntaxRoot<TRoot>
            where TRoot : SyntaxNode
        {
            // information needed to either recreate tree or feed into wrapping tree
            internal readonly AbstractSyntaxTreeFactoryService Service;
            internal readonly string FilePath;
            internal readonly ParseOptions Options;

            protected readonly ValueSource<TextAndVersion> TextSource;

            protected RecoverableSyntaxRoot(
                AbstractSyntaxTreeFactoryService service,
                string filePath,
                ParseOptions options,
                ValueSource<TextAndVersion> textSource)
            {
                Debug.Assert(service != null);
                Debug.Assert(filePath != null);
                Debug.Assert(options != null);
                Debug.Assert(textSource != null);

                this.Service = service;
                this.FilePath = filePath;
                this.Options = options;
                this.TextSource = textSource;
            }

            internal RecoverableSyntaxRoot<TRoot> WithRootAndOptions(TRoot root, ParseOptions options)
            {
                return new ReplacedSyntaxRoot<TRoot>(
                    this.Service,
                    this.FilePath,
                    options,
                    this.TextSource,
                    root);
            }

            internal abstract RecoverableSyntaxRoot<TRoot> WithFilePath(string path);

            public bool TryGetText(out SourceText text)
            {
                TextAndVersion textAndVersion;
                if (this.TextSource.TryGetValue(out textAndVersion))
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
                return TextSource.GetValue(cancellationToken).Text;
            }

            public async Task<SourceText> GetTextAsync(CancellationToken cancellationToken)
            {
                var textAndVersion = await this.TextSource.GetValueAsync(cancellationToken).ConfigureAwait(false);
                return textAndVersion.Text;
            }

            public abstract bool IsReparsed { get; }
            public abstract int Length { get; }
            public abstract bool TryGetRoot(out TRoot root);
            public abstract TRoot GetRoot(CancellationToken cancellationToken);
            public abstract Task<TRoot> GetRootAsync(CancellationToken cancellationToken);
        }

        private sealed class ReplacedSyntaxRoot<TRoot> : RecoverableSyntaxRoot<TRoot>
            where TRoot : SyntaxNode
        {
            private readonly TRoot root;

            internal ReplacedSyntaxRoot(
                AbstractSyntaxTreeFactoryService service,
                string filePath,
                ParseOptions options,
                ValueSource<TextAndVersion> textSource,
                TRoot root) 
                : base(service, filePath, options, textSource)
            {
                Debug.Assert(root != null);
                this.root = root;
            }

            public override bool IsReparsed
            {
                get { return false; }
            }

            public override int Length
            {
                get { return root.FullSpan.Length; }
            }

            internal override RecoverableSyntaxRoot<TRoot> WithFilePath(string path)
            {
                return new ReplacedSyntaxRoot<TRoot>(
                    this.Service,
                    path,
                    this.Options,
                    this.TextSource,
                    this.root);
            }

            public override bool TryGetRoot(out TRoot root)
            {
                root = this.root;
                return true;
            }

            public override TRoot GetRoot(CancellationToken cancellationToken)
            {
                return this.root;
            }

            public override Task<TRoot> GetRootAsync(CancellationToken cancellationToken)
            {
                return Task.FromResult(this.root);
            }
        }

        /// <summary>
        /// Represents all the state used by a language-specific recoverable syntax tree
        /// </summary>
        internal abstract class CachedRecoverableSyntaxRoot<TRoot> : RecoverableSyntaxRoot<TRoot>
            where TRoot : SyntaxNode
        {
            // actual holder of the root
            protected ValueSource<TRoot> rootSource;
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

            protected CachedRecoverableSyntaxRoot(
                AbstractSyntaxTreeFactoryService service,
                string filePath,
                ParseOptions options,
                ValueSource<TextAndVersion> textSource,
                ValueSource<TRoot> rootSource,
                int length)
                : base(service, filePath, options, textSource)
            {
                this.rootSource = rootSource;
                this.length = length;
                this.syntaxTreeCache = service.languageServices.WorkspaceServices.GetService<ISyntaxTreeCacheService>();
                this.evictAction = new WeakAction<CachedRecoverableSyntaxRoot<TRoot>, SyntaxNode>(this, (r, d) => r.OnEvicted(d));
            }

            public static CachedRecoverableSyntaxRoot<TRoot> Create(
                AbstractSyntaxTreeFactoryService service, 
                string filePath, 
                ParseOptions options,
                ValueSource<TextAndVersion> text,
                TRoot root, 
                bool reparse)
            {
                var rootSource = new ConstantValueSource<TRoot>(root);
                int length = root.FullSpan.Length;

                if (reparse)
                {
                    return new ReparsedSyntaxRoot<TRoot>(service, filePath, options, text, rootSource, length);
                }
                else
                {
                    return new SerializedSyntaxRoot<TRoot>(service, filePath, options, text, rootSource, length);
                }
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
                this.OnRootAccessed(this.rootSource.GetValue());
            }

            protected abstract Task<TRoot> RecoverRootAsync(CancellationToken cancellationToken);
            protected abstract TRoot RecoverRoot(CancellationToken cancellationToken);
            protected abstract Task SaveRootAsync(SyntaxTree tree, TRoot root, CancellationToken cancellationToken);

            private void OnRootAccessed(TRoot root)
            {
                // put the tree in the cache and manipulate root node retention when evicted.
                this.syntaxTreeCache.AddOrAccess(root, this.evictAction);
            }

            protected HostWorkspaceServices WorkspaceServices
            {
                get { return this.Service.languageServices.WorkspaceServices; }
            }

            protected ISyntaxTreeStorageService SyntaxTreeStorageService
            {
                get { return WorkspaceServices.GetService<ISyntaxTreeStorageService>(); }
            }

            protected ITemporaryStorageService TemporaryStorageService
            {
                get { return WorkspaceServices.GetService<ITemporaryStorageService>(); }
            }

            public sealed override int Length
            {
                get { return length; }
            }

            public sealed override bool TryGetRoot(out TRoot root)
            {
                if (this.rootSource.TryGetValue(out root))
                {
                    this.OnRootAccessed(root);
                    return true;
                }

                return false;
            }

            public sealed override TRoot GetRoot(CancellationToken cancellationToken)
            {
                TRoot root;
                if (!this.rootSource.TryGetValue(out root))
                {
                    root = GetAsyncLazyRoot().GetValue(cancellationToken);
                }

                this.OnRootAccessed(root);
                return root;
            }

            public sealed override async Task<TRoot> GetRootAsync(CancellationToken cancellationToken)
            {
                TRoot root;
                if (!this.rootSource.TryGetValue(out root))
                {
                    root = await GetAsyncLazyRoot().GetValueAsync(cancellationToken).ConfigureAwait(false);
                }

                this.OnRootAccessed(root);
                return root;
            }

            private static readonly Func<CachedRecoverableSyntaxRoot<TRoot>, AsyncLazy<TRoot>> rootFactory =
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

        // a recoverable syntax root that recovers its nodes from a serialized temporary storage
        private sealed class SerializedSyntaxRoot<TRoot> : CachedRecoverableSyntaxRoot<TRoot> 
            where TRoot : SyntaxNode
        {
            internal SerializedSyntaxRoot(
                AbstractSyntaxTreeFactoryService service,
                string filePath,
                ParseOptions options,
                ValueSource<TextAndVersion> textSource,
                ValueSource<TRoot> rootSource,
                int length)
                : base(service, filePath, options, textSource, rootSource, length)
            {
            }

            public override bool IsReparsed
            {
                get { return false; }
            }

            internal override RecoverableSyntaxRoot<TRoot> WithFilePath(string path)
            {
                return new SerializedSyntaxRoot<TRoot>(
                    this.Service,
                    path,
                    this.Options,
                    this.TextSource,
                    this.rootSource,
                    this.Length);
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
                throw ExceptionUtilities.Unreachable;
            }

            protected override TRoot RecoverRoot(CancellationToken cancellationToken)
            {
                throw ExceptionUtilities.Unreachable;
            }
        }

        // a recoverable syntax root that recovers its nodes by reparsing the text
        private sealed class ReparsedSyntaxRoot<TRoot> : CachedRecoverableSyntaxRoot<TRoot>
            where TRoot : SyntaxNode
        {
            internal ReparsedSyntaxRoot(
                AbstractSyntaxTreeFactoryService service,
                string filePath,
                ParseOptions options,
                ValueSource<TextAndVersion> textSource,
                ValueSource<TRoot> rootSource,
                int length)
                : base(service, filePath, options, textSource, rootSource, length)
            {
            }

            public override bool IsReparsed
            {
                get { return true; }
            }

            internal override RecoverableSyntaxRoot<TRoot> WithFilePath(string path)
            {
                return new ReparsedSyntaxRoot<TRoot>(
                    this.Service,
                    path,
                    this.Options,
                    this.TextSource,
                    this.rootSource,
                    this.Length);
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
