// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Microsoft.CodeAnalysis
{
#pragma warning disable RS0010
    /// <summary>
    /// Represents structured trivia that contains skipped tokens. This is implemented by
    /// <see cref="T:Microsoft.CodeAnalysis.CSharp.Syntax.SkippedTokensTriviaSyntax"/> and
    /// <see cref="T:Microsoft.CodeAnalysis.VisualBasic.Syntax.SkippedTokensTriviaSyntax"/>.
    /// </summary>
#pragma warning restore RS0010
    public interface ISkippedTokensTriviaSyntax
    {
        SyntaxTokenList Tokens { get; }
    }
}
