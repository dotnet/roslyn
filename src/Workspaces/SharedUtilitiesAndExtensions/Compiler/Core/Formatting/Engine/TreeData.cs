// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Formatting;

/// <summary>
/// this provides information about the syntax tree formatting service is formatting.
/// this provides necessary abstraction between different kinds of syntax trees so that ones that contain
/// actual text or cache can answer queries more efficiently.
/// </summary>
internal abstract partial class TreeData
{
    public static TreeData Create(SyntaxNode root)
    {
        // either there is no tree or a tree that is not generated from a text.
        if (root.SyntaxTree == null || !root.SyntaxTree.TryGetText(out var text))
        {
            return new Node(root);
        }

#if DEBUG
        return new Debug(root, text);
#else
        return new NodeAndText(root, text);
#endif
    }

    public static TreeData Create(SyntaxTrivia trivia, int initialColumn)
        => new StructuredTrivia(trivia, initialColumn);

    private readonly SyntaxToken _firstToken;
    private readonly SyntaxToken _lastToken;

    public TreeData(SyntaxNode root)
    {
        Contract.ThrowIfNull(root);
        Root = root;

        _firstToken = Root.GetFirstToken(includeZeroWidth: true);
        _lastToken = Root.GetLastToken(includeZeroWidth: true);
    }

    public abstract string GetTextBetween(SyntaxToken token1, SyntaxToken token2);
    public abstract int GetOriginalColumn(int tabSize, SyntaxToken token);

    public SyntaxNode Root { get; }

    public bool IsFirstToken(SyntaxToken token)
        => _firstToken == token;

    public bool IsLastToken(SyntaxToken token)
        => _lastToken == token;

    public int StartPosition
    {
        get { return this.Root.FullSpan.Start; }
    }

    public int EndPosition
    {
        get { return this.Root.FullSpan.End; }
    }

    public IEnumerable<SyntaxToken> GetApplicableTokens(TextSpan textSpan)
        => this.Root.DescendantTokens(textSpan);
}
