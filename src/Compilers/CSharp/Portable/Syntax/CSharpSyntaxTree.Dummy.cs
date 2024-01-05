// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Text;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp
{
    public partial class CSharpSyntaxTree
    {
        internal sealed class DummySyntaxTree : CSharpSyntaxTree
        {
            private const SourceHashAlgorithm ChecksumAlgorithm = SourceHashAlgorithm.Sha1;

            private readonly CompilationUnitSyntax _node;

            public DummySyntaxTree()
            {
                _node = this.CloneNodeAsRoot(SyntaxFactory.ParseCompilationUnit(string.Empty));
            }

            public override string ToString()
            {
                return string.Empty;
            }

            public override SourceText GetText(CancellationToken cancellationToken)
            {
                return SourceText.From(string.Empty, Encoding, ChecksumAlgorithm);
            }

            public override bool TryGetText(out SourceText text)
            {
                text = SourceText.From(string.Empty, Encoding, ChecksumAlgorithm);
                return true;
            }

            public override Encoding Encoding
            {
                get { return Encoding.UTF8; }
            }

            public override int Length
            {
                get { return 0; }
            }

            public override CSharpParseOptions Options
            {
                get { return CSharpParseOptions.Default; }
            }

            [Obsolete("Obsolete due to performance problems, use CompilationOptions.SyntaxTreeOptionsProvider instead", error: false)]
            public override ImmutableDictionary<string, ReportDiagnostic> DiagnosticOptions
                => throw ExceptionUtilities.Unreachable();

            public override string FilePath
            {
                get { return string.Empty; }
            }

            public override SyntaxReference GetReference(SyntaxNode node)
            {
                return new SimpleSyntaxReference(node);
            }

            public override CSharpSyntaxNode GetRoot(CancellationToken cancellationToken)
            {
                return _node;
            }

            public override bool TryGetRoot(out CSharpSyntaxNode root)
            {
                root = _node;
                return true;
            }

            public override bool HasCompilationUnitRoot
            {
                get { return true; }
            }

            public override FileLinePositionSpan GetLineSpan(TextSpan span, CancellationToken cancellationToken = default(CancellationToken))
            {
                return default(FileLinePositionSpan);
            }

            public override SyntaxTree WithRootAndOptions(SyntaxNode root, ParseOptions options)
                => Create((CSharpSyntaxNode)root, (CSharpParseOptions)options, FilePath, Encoding, ChecksumAlgorithm);

            public override SyntaxTree WithFilePath(string path)
                => Create(_node, Options, path, Encoding, ChecksumAlgorithm);
        }
    }
}
