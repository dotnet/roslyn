// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.SourceGeneration;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFacts;

namespace Microsoft.CodeAnalysis.CSharp.SourceGeneration
{
    internal partial class CSharpCodeGenerator
    {
        private static IdentifierNameSyntax IdentifierName(string text)
            => SyntaxFactory.IdentifierName(Identifier(text));

        private static SyntaxToken Identifier(string text)
        {
            return GetKeywordKind(text) != SyntaxKind.None || GetContextualKeywordKind(text) != SyntaxKind.None
                ? SyntaxFactory.Identifier("@" + text)
                : SyntaxFactory.Identifier(text);
        }
    }
}
