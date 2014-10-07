using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using Roslyn.Compilers;
using Roslyn.Compilers.CSharp;
using Roslyn.Utilities;

namespace Roslyn.Services.Editor.CSharp.Utilities
{
    // TODO: we really should have this in the CaaS layer. The code-coverage exclusion is
    // otherwise we get penalized for having methods we don't care about
    [ExcludeFromCodeCoverage]
    internal class TrivialSyntaxTree : SyntaxTree
    {
        private readonly string fileName;
        private readonly SyntaxNode rootNode;
        private readonly CancellableFuture<IText> text;
        private readonly ParseOptions options;

        public TrivialSyntaxTree(SyntaxTree syntaxTree, SyntaxNode rootNode)
            : this(syntaxTree.FilePath, rootNode, syntaxTree.Options)
        {
        }

        public TrivialSyntaxTree(string fileName, SyntaxNode rootNode, ParseOptions options)
        {
            this.fileName = fileName;
            this.rootNode = this.CloneNodeAsRoot(rootNode);
            this.options = options;

            this.text = new CancellableFuture<IText>(c => new StringText(this.rootNode.GetFullText()));
        }

        public override SyntaxNode GetRoot(CancellationToken cancellationToken)
        {
            return rootNode;
        }

        public override bool TryGetRoot(out SyntaxNode root)
        {
            root = this.rootNode;
            return true;
        }

        public override IText GetText(CancellationToken cancellationToken)
        {
            return text.GetValue(cancellationToken);
        }

        public override ParseOptions Options
        {
            get
            {
                return options;
            }
        }

        public override string FilePath
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
            private readonly SyntaxTree syntaxTree;
            private readonly SyntaxNode node;

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