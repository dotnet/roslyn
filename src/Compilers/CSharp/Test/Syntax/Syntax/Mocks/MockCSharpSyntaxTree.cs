// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests;

internal class MockCSharpSyntaxTree : CSharpSyntaxTree
{
    private readonly SourceText _sourceText;
    private readonly CSharpSyntaxNode? _root;

    public override CSharpParseOptions Options { get; }
    public override string FilePath { get; }

    public MockCSharpSyntaxTree(
        CSharpSyntaxNode? root = null,
        SourceText? source = null,
        CSharpParseOptions? options = null,
        string? filePath = null)
    {
        _root = (root != null) ? CloneNodeAsRoot(root) : null;
        _sourceText = source ?? SourceText.From("", Encoding.UTF8, SourceHashAlgorithm.Sha256);
        Options = options ?? TestOptions.Regular;
        FilePath = filePath ?? string.Empty;
    }

    public override SourceText GetText(CancellationToken cancellationToken)
        => _sourceText;

    public override bool TryGetText(out SourceText text)
    {
        text = _sourceText;
        return true;
    }

    public override Encoding? Encoding
        => _sourceText.Encoding;

    public override int Length
        => _sourceText.Length;

    public override CSharpSyntaxNode GetRoot(CancellationToken cancellationToken)
        => _root ?? throw new NotImplementedException();

    public override bool TryGetRoot([NotNullWhen(true)] out CSharpSyntaxNode? root)
    {
        root = _root;
        return root != null;
    }

    public override SyntaxTree WithRootAndOptions(SyntaxNode root, ParseOptions options)
        => new MockCSharpSyntaxTree((CSharpSyntaxNode)root, _sourceText, (CSharpParseOptions)options, FilePath);

    public override SyntaxTree WithFilePath(string path)
        => new MockCSharpSyntaxTree(_root, _sourceText, Options, FilePath);

    public override bool HasCompilationUnitRoot
        => _root != null;

    public override SyntaxReference GetReference(SyntaxNode node)
        => new SimpleSyntaxReference(node);
}
