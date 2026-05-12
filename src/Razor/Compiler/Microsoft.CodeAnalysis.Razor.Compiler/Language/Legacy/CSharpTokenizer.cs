// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Razor.Language.Syntax.InternalSyntax;
using CSharpSyntaxKind = Microsoft.CodeAnalysis.CSharp.SyntaxKind;

namespace Microsoft.AspNetCore.Razor.Language.Legacy;

internal abstract class CSharpTokenizer : Tokenizer
{
    protected CSharpTokenizer(SeekableTextReader source) : base(source)
    {
    }

    internal abstract CSharpSyntaxKind? GetTokenKeyword(SyntaxToken token);
}
