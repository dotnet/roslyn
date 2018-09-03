// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Diagnostics;

namespace Microsoft.CodeAnalysis.CSharp.Features.EmbeddedLanguages
{
    internal static class EmbeddedLanguageUtilities
    {
        public static string EscapeText(string text, SyntaxToken token)
        {
            Debug.Assert(token.Kind() == SyntaxKind.StringLiteralToken);
            return token.IsVerbatimStringLiteral()
                ? text
                : text.Replace("\\", "\\\\");
        }
    }
}
