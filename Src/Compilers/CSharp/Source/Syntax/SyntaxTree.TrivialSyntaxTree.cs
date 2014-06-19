using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using Roslyn.Compilers;
using Roslyn.Compilers.CSharp;

namespace Roslyn.Services.CSharp.LanguageServices
{
#if false
    internal partial class SyntaxTree
    {
        [ExcludeFromCodeCoverage]
        private class TrivialSyntaxTree : SyntaxTree
        {
            private readonly string fileName;
            private readonly SyntaxNode rootNode;
            private readonly IText text;
            private readonly ParseOptions options;

            public TrivialSyntaxTree(string fileName, SyntaxNode rootNode, ParseOptions options)
            {
                this.fileName = fileName;
                this.rootNode = rootNode;
                this.options = options;

                // TODO: HACK HACK HACK HACK HACK HACK: look away, for this is terribly inefficient
                this.text = new StringText(rootNode.GetFullText());
            }

            protected override SyntaxNode GetRoot(CancellationToken cancellationToken)
            {
                return rootNode;
            }

            protected override IText GetText(CancellationToken cancellationToken)
            {
                return text;
            }

            public override ParseOptions Options
            {
                get
                {
                    return options;
                }
            }

            public override string FileName
            {
                get
                {
                    return this.fileName;
                }
            }

            public override SyntaxTree WithChange(IText newText, params TextChangeRange[] changes)
            {
                throw new NotSupportedException();
            }

            public override SyntaxReference GetReference(SyntaxNode node)
            {
                return new TrivialReference(this, node);
            }

            internal class TrivialReference : SyntaxReference
            {
                private SyntaxTree syntaxTree;
                private SyntaxNode node;

                internal TrivialReference(SyntaxTree syntaxTree, SyntaxNode node)
                {
                    this.syntaxTree = syntaxTree;
                    this.node = node;
                }

                public override SyntaxTree SyntaxTree
                {
                    get
                    {
                        return this.syntaxTree;
                    }
                }

                public override TextSpan Span
                {
                    get
                    {
                        return this.node.Span;
                    }
                }

                public override SyntaxNode GetSyntax()
                {
                    return this.node;
                }
            }
        }
    }
#endif
}