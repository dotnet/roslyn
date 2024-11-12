// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Threading;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CSharp;

internal partial class CSharpSyntaxTreeFactoryService
{
    /// <summary>
    /// Parsed <see cref="CSharpSyntaxTree"/> that creates <see cref="SourceText"/> with given encoding and checksum algorithm.
    /// </summary>
    private sealed class ParsedSyntaxTree : CSharpSyntaxTree
    {
        private readonly CSharpSyntaxNode _root;
        private readonly SourceHashAlgorithm _checksumAlgorithm;

        public override Encoding? Encoding { get; }
        public override CSharpParseOptions Options { get; }
        public override string FilePath { get; }

        private SourceText? _lazyText;

        public ParsedSyntaxTree(
            SourceText? lazyText,
            CSharpSyntaxNode root,
            CSharpParseOptions options,
            string filePath,
            Encoding? encoding,
            SourceHashAlgorithm checksumAlgorithm)
        {
            _lazyText = lazyText;
            _root = CloneNodeAsRoot(root);
            _checksumAlgorithm = checksumAlgorithm;

            Encoding = encoding;
            Options = options;
            FilePath = filePath;
        }

        public override SourceText GetText(CancellationToken cancellationToken)
        {
            if (_lazyText == null)
            {
                Interlocked.CompareExchange(ref _lazyText, GetRoot(cancellationToken).GetText(Encoding, _checksumAlgorithm), null);
            }

            return _lazyText;
        }

        public override bool TryGetText([NotNullWhen(true)] out SourceText? text)
        {
            text = _lazyText;
            return text != null;
        }

        public override int Length
            => _root.FullSpan.Length;

        public override bool HasCompilationUnitRoot
            => true;

        public override CSharpSyntaxNode GetRoot(CancellationToken cancellationToken)
            => _root;

        public override bool TryGetRoot([NotNullWhen(true)] out CSharpSyntaxNode? root)
        {
            root = _root;
            return true;
        }

        public override SyntaxTree WithRootAndOptions(SyntaxNode root, ParseOptions options)
            => root == _root && options == Options
                ? this
                : new ParsedSyntaxTree(
                    root == _root ? _lazyText : null,
                    (CSharpSyntaxNode)root,
                    (CSharpParseOptions)options,
                    FilePath,
                    Encoding,
                    _checksumAlgorithm);

        public override SyntaxTree WithFilePath(string path)
            => path == FilePath
                ? this
                : new ParsedSyntaxTree(_lazyText, _root, Options, path, Encoding, _checksumAlgorithm);

        public override SyntaxReference GetReference(SyntaxNode node)
            => new NodeSyntaxReference(node);
    }
}
