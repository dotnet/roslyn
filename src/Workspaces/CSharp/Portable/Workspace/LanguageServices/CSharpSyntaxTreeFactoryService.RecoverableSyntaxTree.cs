// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Text;
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
        private partial class CSharpSyntaxTreeFactoryService
        {
            /// <summary>
            /// Represents a syntax tree that only has a weak reference to its 
            /// underlying data.  This way it can be passed around without forcing
            /// the underlying full tree to stay alive.  Think of it more as a 
            /// key that can be used to identify a tree rather than the tree itself.
            /// </summary>
            internal sealed class RecoverableSyntaxTree : CSharpSyntaxTree, IRecoverableSyntaxTree<CSharpSyntaxNode>, ICachedObjectOwner
            {
                private readonly RecoverableSyntaxRoot<CSharpSyntaxNode> _recoverableRoot;
                private readonly SyntaxTreeInfo _info;
                private readonly AbstractSyntaxTreeFactoryService _service;
                private readonly ProjectId _cacheKey;

                public override bool HasCompilationUnitRoot { get; }

                object ICachedObjectOwner.CachedObject { get; set; }

                private RecoverableSyntaxTree(AbstractSyntaxTreeFactoryService service, ProjectId cacheKey, CSharpSyntaxNode root, SyntaxTreeInfo info)
                {
                    _recoverableRoot = new RecoverableSyntaxRoot<CSharpSyntaxNode>(service, root, this);
                    _info = info;
                    _service = service;
                    _cacheKey = cacheKey;
                    HasCompilationUnitRoot = root.IsKind(SyntaxKind.CompilationUnit);
                }

                private RecoverableSyntaxTree(RecoverableSyntaxTree original, SyntaxTreeInfo info)
                {
                    _recoverableRoot = original._recoverableRoot.WithSyntaxTree(this);
                    _info = info;
                    _service = original._service;
                    _cacheKey = original._cacheKey;
                    HasCompilationUnitRoot = original.HasCompilationUnitRoot;
                }

                internal static SyntaxTree CreateRecoverableTree(
                    AbstractSyntaxTreeFactoryService service,
                    ProjectId cacheKey,
                    string filePath,
                    ParseOptions options,
                    ITextAndVersionSource text,
                    Encoding encoding,
                    CompilationUnitSyntax root)
                {
                    return new RecoverableSyntaxTree(
                        service,
                        cacheKey,
                        root,
                        new SyntaxTreeInfo(
                            filePath,
                            options,
                            text,
                            encoding,
                            root.FullSpan.Length,
                            root.ContainsDirectives));
                }

                public override string FilePath
                    => _info.FilePath;

                public override CSharpParseOptions Options
                    => (CSharpParseOptions)_info.Options;

                public override int Length
                    => _info.Length;

                public override bool TryGetText(out SourceText text)
                    => _info.TryGetText(out text);

                public override SourceText GetText(CancellationToken cancellationToken)
                    => _info.TextSource.GetValue(cancellationToken).Text;

                public override Task<SourceText> GetTextAsync(CancellationToken cancellationToken)
                    => _info.GetTextAsync(cancellationToken);

                public override Encoding Encoding
                    => _info.Encoding;

                private CSharpSyntaxNode CacheRootNode(CSharpSyntaxNode node)
                    => _service.SolutionServices.GetRequiredService<IProjectCacheHostService>().CacheObjectIfCachingEnabledForKey(_cacheKey, this, node);

                public override bool TryGetRoot(out CSharpSyntaxNode root)
                {
                    var status = _recoverableRoot.TryGetValue(out var node);
                    root = node;
                    CacheRootNode(node);
                    return status;
                }

                public override CSharpSyntaxNode GetRoot(CancellationToken cancellationToken = default)
                    => CacheRootNode(_recoverableRoot.GetValue(cancellationToken));

                public override async Task<CSharpSyntaxNode> GetRootAsync(CancellationToken cancellationToken)
                    => CacheRootNode(await _recoverableRoot.GetValueAsync(cancellationToken).ConfigureAwait(false));

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

                CSharpSyntaxNode IRecoverableSyntaxTree<CSharpSyntaxNode>.CloneNodeAsRoot(CSharpSyntaxNode root)
                    => CloneNodeAsRoot(root);

                public bool ContainsDirectives => _info.ContainsDirectives;

                public override SyntaxTree WithRootAndOptions(SyntaxNode root, ParseOptions options)
                {
                    if (ReferenceEquals(_info.Options, options) && TryGetRoot(out var oldRoot) && ReferenceEquals(root, oldRoot))
                    {
                        return this;
                    }

                    return new RecoverableSyntaxTree(_service, _cacheKey, (CSharpSyntaxNode)root,
                        _info.WithOptionsAndLengthAndContainsDirectives(options, root.FullSpan.Length, root.ContainsDirectives));
                }

                public override SyntaxTree WithFilePath(string path)
                {
                    if (path == this.FilePath)
                    {
                        return this;
                    }

                    return new RecoverableSyntaxTree(this, _info.WithFilePath(path));
                }

                public SyntaxTree WithOptions(ParseOptions parseOptions)
                {
                    if (ReferenceEquals(_info.Options, parseOptions))
                    {
                        return this;
                    }

                    return new RecoverableSyntaxTree(this, _info.WithOptions(parseOptions));
                }
            }
        }
    }
}
