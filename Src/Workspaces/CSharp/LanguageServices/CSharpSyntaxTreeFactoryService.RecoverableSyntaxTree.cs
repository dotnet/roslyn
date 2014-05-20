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
            internal sealed class RecoverableSyntaxTree : CSharpSyntaxTree, IRecoverableSyntaxTree<CompilationUnitSyntax>
            {
                private readonly RecoverableSyntaxRoot<CompilationUnitSyntax> recoverableRoot;

                private RecoverableSyntaxTree(RecoverableSyntaxRoot<CompilationUnitSyntax> recoverableRoot)
                {
                    Debug.Assert(recoverableRoot != null);
                    this.recoverableRoot = recoverableRoot;
                }

                internal static SyntaxTree CreateRecoverableTree(AbstractSyntaxTreeFactoryService service, string filePath, ParseOptions options, ValueSource<TextAndVersion> text, CompilationUnitSyntax root, bool reparse)
                {
                    var recoverableRoot = CachedRecoverableSyntaxRoot<CompilationUnitSyntax>.Create(service, filePath, options, text, root, reparse);
                    var recoverableTree = new RecoverableSyntaxTree(recoverableRoot);
                    recoverableRoot.SetContainingTree(recoverableTree);
                    return recoverableTree;
                }

                public override string FilePath
                {
                    get
                    {
                        return recoverableRoot.FilePath;
                    }
                }

                public override CSharpParseOptions Options
                {
                    get
                    {
                        return (CSharpParseOptions)recoverableRoot.Options;
                    }
                }

                public override int Length
                {
                    get { return recoverableRoot.Length; }
                }

                public override bool TryGetText(out SourceText text)
                {
                    return recoverableRoot.TryGetText(out text);
                }

                public override SourceText GetText(CancellationToken cancellationToken)
                {
                    return recoverableRoot.GetText(cancellationToken);
                }

                public override Task<SourceText> GetTextAsync(CancellationToken cancellationToken)
                {
                    return recoverableRoot.GetTextAsync(cancellationToken);
                }

                public override bool TryGetRoot(out CSharpSyntaxNode root)
                {
                    CompilationUnitSyntax node;
                    bool status = this.TryGetRoot(out node);
                    root = node;
                    return status;
                }

                public bool TryGetRoot(out CompilationUnitSyntax root)
                {
                    return recoverableRoot.TryGetRoot(out root);
                }

                public override CSharpSyntaxNode GetRoot(CancellationToken cancellationToken = default(CancellationToken))
                {
                    return recoverableRoot.GetRoot(cancellationToken);
                }

                public override async Task<CSharpSyntaxNode> GetRootAsync(CancellationToken cancellationToken)
                {
                    return await recoverableRoot.GetRootAsync(cancellationToken).ConfigureAwait(false);
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
                    CompilationUnitSyntax oldRoot;
                    if (ReferenceEquals(recoverableRoot.Options, options) && this.TryGetRoot(out oldRoot) && ReferenceEquals(root, oldRoot))
                    {
                        return this;
                    }

                    return new RecoverableSyntaxTree(recoverableRoot.WithRootAndOptions((CompilationUnitSyntax)root, options));
                }

                public override SyntaxTree WithFilePath(string path)
                {
                    if (path == recoverableRoot.FilePath)
                    {
                        return this;
                    }

                    return new RecoverableSyntaxTree(recoverableRoot.WithFilePath(path));
                }

                internal bool IsReparsed
                {
                    get { return this.recoverableRoot.IsReparsed; }
                }
            }
        }
    }
}