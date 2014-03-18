// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Instrumentation;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;
using System.Diagnostics;

namespace Microsoft.CodeAnalysis.CSharp
{
    public partial class CSharpSyntaxTree
    {
        private partial class ParsedSyntaxTree : CSharpSyntaxTree
        {
            private readonly CSharpParseOptions options;
            private readonly string path;
            private readonly CSharpSyntaxNode root;
            private SourceText text;
            private readonly bool hasCompilationUnitRoot;

            internal ParsedSyntaxTree(SourceText source, string path, CSharpParseOptions options, CSharpSyntaxNode root, Syntax.InternalSyntax.DirectiveStack directives, bool cloneRoot = true)
            {
                Debug.Assert(root != null);
                Debug.Assert(options != null);
                Debug.Assert(path != null);

                this.text = source;
                this.options = options;
                this.path = path;
                this.root = cloneRoot ? this.CloneNodeAsRoot(root) : root;
                this.hasCompilationUnitRoot = root.Kind == SyntaxKind.CompilationUnit;
                this.SetDirectiveStack(directives);
            }

            public override string FilePath
            {
                get { return path; }
            }

            public override SourceText GetText(CancellationToken cancellationToken)
            {
                if (this.text == null)
                {
                    using (Logger.LogBlock(FunctionId.CSharp_SyntaxTree_GetText, message: this.FilePath, cancellationToken: cancellationToken))
                    {
                        Interlocked.CompareExchange(ref this.text, this.GetRoot(cancellationToken).GetText(), null);
                    }
                }

                return this.text;
            }

            public override bool TryGetText(out SourceText text)
            {
                text = this.text;
                return text != null;
            }

            public override int Length
            {
                get { return this.root.FullSpan.Length; }
            }

            public override CSharpSyntaxNode GetRoot(CancellationToken cancellationToken)
            {
                return root;
            }

            public override bool TryGetRoot(out CSharpSyntaxNode root)
            {
                root = this.root;
                return true;
            }

            public override bool HasCompilationUnitRoot
            {
                get
                {
                    return this.hasCompilationUnitRoot;
                }
            }

            public override CSharpParseOptions Options
            {
                get
                {
                    return this.options;
                }
            }

            public override SyntaxReference GetReference(SyntaxNode node)
            {
                return new SimpleSyntaxReference(node);
            }

            public override string ToString()
            {
                return this.GetText(CancellationToken.None).ToString();
            }
        }
    }
}