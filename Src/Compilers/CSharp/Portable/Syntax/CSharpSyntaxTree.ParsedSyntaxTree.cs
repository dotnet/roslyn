// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.Text;
using System.Threading;
using Microsoft.CodeAnalysis.Instrumentation;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CSharp
{
    public partial class CSharpSyntaxTree
    {
        private partial class ParsedSyntaxTree : CSharpSyntaxTree
        {
            private readonly CSharpParseOptions options;
            private readonly string path;
            private readonly CSharpSyntaxNode root;
            private readonly bool hasCompilationUnitRoot;
            private readonly Encoding encodingOpt;
            private SourceText lazyText;

            internal ParsedSyntaxTree(SourceText textOpt, Encoding encodingOpt, string path, CSharpParseOptions options, CSharpSyntaxNode root, Syntax.InternalSyntax.DirectiveStack directives, bool cloneRoot = true)
            {
                Debug.Assert(root != null);
                Debug.Assert(options != null);
                Debug.Assert(path != null);
                Debug.Assert(textOpt == null || textOpt.Encoding == encodingOpt);

                this.lazyText = textOpt;
                this.encodingOpt = encodingOpt;
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
                if (this.lazyText == null)
                {
                    using (Logger.LogBlock(FunctionId.CSharp_SyntaxTree_GetText, message: this.FilePath, cancellationToken: cancellationToken))
                    {
                        Interlocked.CompareExchange(ref this.lazyText, this.GetRoot(cancellationToken).GetText(encodingOpt), null);
                    }
                }

                return this.lazyText;
            }

            public override bool TryGetText(out SourceText text)
            {
                text = this.lazyText;
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

            public override SyntaxTree WithRootAndOptions(SyntaxNode root, ParseOptions options)
            {
                if (ReferenceEquals(this.root, root) && ReferenceEquals(this.options, options))
                {
                    return this;
                }

                return new ParsedSyntaxTree(
                    this.lazyText,
                    this.encodingOpt,
                    this.path,
                    (CSharpParseOptions)options,
                    (CSharpSyntaxNode)root,
                    this.directives);
            }

            public override SyntaxTree WithFilePath(string path)
            {
                if (this.path == path)
                {
                    return this;
                }

                return new ParsedSyntaxTree(
                    this.lazyText,
                    this.encodingOpt,
                    path,
                    this.options,
                    this.root,
                    this.directives);
            }
        }
    }
}