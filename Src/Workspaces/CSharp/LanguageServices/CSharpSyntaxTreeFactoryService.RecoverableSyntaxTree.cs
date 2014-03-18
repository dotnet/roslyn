// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.LanguageServices;
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
            internal abstract class RecoverableSyntaxTree : CSharpSyntaxTree, IRecoverableSyntaxTree<CompilationUnitSyntax>
            {
                private readonly AbstractRecoverableSyntaxRoot<CompilationUnitSyntax> recoverableRoot;

                public RecoverableSyntaxTree(AbstractRecoverableSyntaxRoot<CompilationUnitSyntax> recoverableRoot)
                {
                    this.recoverableRoot = recoverableRoot;
                    this.recoverableRoot.SetContainingTree(this);
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
            }

            internal class SerializedSyntaxTree : RecoverableSyntaxTree
            {
                public SerializedSyntaxTree(
                    CSharpSyntaxTreeFactoryService service,
                    string fileName,
                    CSharpParseOptions options,
                    ValueSource<TextAndVersion> text,
                    CompilationUnitSyntax root) : base(new SerializedSyntaxRoot<CompilationUnitSyntax>(service, fileName, options, text, root))
                {
                }
            }

            internal class ReparsedSyntaxTree : RecoverableSyntaxTree
            {
                public ReparsedSyntaxTree(
                    CSharpSyntaxTreeFactoryService service,
                    string fileName,
                    CSharpParseOptions options,
                    ValueSource<TextAndVersion> text,
                    CompilationUnitSyntax root) : base(new ReparsedSyntaxRoot<CompilationUnitSyntax>(service, fileName, options, text, root))
                {
                }
            }
        }
    }
}