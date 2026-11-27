// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

using Microsoft.AspNetCore.Razor.Language.Syntax.InternalSyntax;

using CSharpParseOptions = Microsoft.CodeAnalysis.CSharp.CSharpParseOptions;

namespace Microsoft.AspNetCore.Razor.Language.Legacy;

public abstract class CSharpTokenizerTestBase : TokenizerTestBase<CSharpParseOptions>
{
    private static readonly SyntaxToken _ignoreRemaining = SyntaxFactory.Token(SyntaxKind.Marker, string.Empty);

    internal override object IgnoreRemaining
    {
        get { return _ignoreRemaining; }
    }

    internal override object CreateTokenizer(SeekableTextReader source, CSharpParseOptions parseOptions)
    {
        return new RoslynCSharpTokenizer(source, parseOptions);
    }

    internal override CSharpParseOptions DefaultTokenizerArg => CSharpParseOptions.Default;

    internal void TestSingleToken(string text, SyntaxKind expectedTokenKind)
    {
        TestTokenizer(text, SyntaxFactory.Token(expectedTokenKind, text));
    }
}
