// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal partial class CSharpSyntaxTreeFactoryServiceFactory
    {
        // internal for testing
        internal partial class CSharpSyntaxTreeFactoryService
        {
            /// <summary>
            /// Represents a syntax tree that only has a weak reference to its 
            /// underlying data.  This way it can be passed around without forcing
            /// the underlying full tree to stay alive.  Think of it more as a 
            /// key that can be used to identify a tree rather than the tree itself.
            /// </summary>
            internal sealed class RecoverableSyntaxTree : CSharpSyntaxTree, IRecoverableSyntaxTree<CompilationUnitSyntax>, ICachedObjectOwner
            {
                private readonly RecoverableSyntaxRoot<CompilationUnitSyntax> recoverableRoot;
                private readonly SyntaxTreeInfo info;
                private readonly IProjectCacheHostService projectCacheService;
                private readonly ProjectId cacheKey;

                object ICachedObjectOwner.CachedObject { get; set; }

                private RecoverableSyntaxTree(AbstractSyntaxTreeFactoryService service, ProjectId cacheKey, CompilationUnitSyntax root, SyntaxTreeInfo info)
                {
                    this.recoverableRoot = new RecoverableSyntaxRoot<CompilationUnitSyntax>(service, root, this);
                    this.info = info;
                    this.projectCacheService = service.LanguageServices.WorkspaceServices.GetService<IProjectCacheHostService>();
                    this.cacheKey = cacheKey;
                }

                private RecoverableSyntaxTree(RecoverableSyntaxTree original, SyntaxTreeInfo info)
                {
                    this.recoverableRoot = original.recoverableRoot.WithSyntaxTree(this);
                    this.info = info;
                    this.projectCacheService = original.projectCacheService;
                    this.cacheKey = original.cacheKey;
                }

                internal static SyntaxTree CreateRecoverableTree(AbstractSyntaxTreeFactoryService service, ProjectId cacheKey, string filePath, ParseOptions options, ValueSource<TextAndVersion> text, CompilationUnitSyntax root)
                {
                    return root.AttributeLists.Any() || root.FullSpan.Length < service.MinimumLengthForRecoverableTree
                        ? Create(root, (CSharpParseOptions)options, filePath, root.SyntaxTree.GetText().Encoding)
                        : new RecoverableSyntaxTree(service, cacheKey, root, new SyntaxTreeInfo(filePath, options, text, root.FullSpan.Length));
                }

                public override string FilePath
                {
                    get { return this.info.FilePath; }
                }

                public override CSharpParseOptions Options
                {
                    get { return (CSharpParseOptions)this.info.Options; }
                }

                public override int Length
                {
                    get { return this.info.Length; }
                }

                public override bool TryGetText(out SourceText text)
                {
                    return this.info.TryGetText(out text);
                }

                public override SourceText GetText(CancellationToken cancellationToken)
                {
                    return this.info.TextSource.GetValue(cancellationToken).Text;
                }

                public override Task<SourceText> GetTextAsync(CancellationToken cancellationToken)
                {
                    return this.info.GetTextAsync(cancellationToken);
                }

                private CompilationUnitSyntax CacheRootNode(CompilationUnitSyntax node)
                {
                    return projectCacheService.CacheObjectIfCachingEnabledForKey(this.cacheKey, this, node);
                }

                public override bool TryGetRoot(out CSharpSyntaxNode root)
                {
                    CompilationUnitSyntax node;
                    bool status = recoverableRoot.TryGetValue(out node);
                    root = node;
                    CacheRootNode(node);
                    return status;
                }

                public override CSharpSyntaxNode GetRoot(CancellationToken cancellationToken = default(CancellationToken))
                {
                    return CacheRootNode(recoverableRoot.GetValue(cancellationToken));
                }

                public override async Task<CSharpSyntaxNode> GetRootAsync(CancellationToken cancellationToken)
                {
                    return CacheRootNode(await recoverableRoot.GetValueAsync(cancellationToken).ConfigureAwait(false));
                }

                public override bool HasCompilationUnitRoot
                {
                    get { return true; }
                }

                public override SyntaxReference GetReference(SyntaxNode node)
                {
                    if (node != null)
                    {
                        // many people will take references to nodes in this tree.  
                        // We don't actually want those references to keep the tree alive.
                        if (node.Span.Length == 0)
                        {
                            return new PathSyntaxReference(node);
                        }
                        else
                        {
                            return new PositionalSyntaxReference(node);
                        }
                    }
                    else
                    {
                        return new NullSyntaxReference(this);
                    }
                }

                CompilationUnitSyntax IRecoverableSyntaxTree<CompilationUnitSyntax>.CloneNodeAsRoot(CompilationUnitSyntax root)
                {
                    return CloneNodeAsRoot(root);
                }

                public override SyntaxTree WithRootAndOptions(SyntaxNode root, ParseOptions options)
                {
                    CSharpSyntaxNode oldRoot;
                    if (ReferenceEquals(this.info.Options, options) && this.TryGetRoot(out oldRoot) && ReferenceEquals(root, oldRoot))
                    {
                        return this;
                    }

                    return Create((CSharpSyntaxNode)root, this.Options, info.FilePath);
                }

                public override SyntaxTree WithFilePath(string path)
                {
                    if (path == this.FilePath)
                    {
                        return this;
                    }

                    return new RecoverableSyntaxTree(this, info.WithFilePath(path));
                }
            }
        }
    }
}