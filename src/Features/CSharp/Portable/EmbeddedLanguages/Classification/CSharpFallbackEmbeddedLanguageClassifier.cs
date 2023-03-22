// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis.Classification;
using Microsoft.CodeAnalysis.CSharp.EmbeddedLanguages.LanguageServices;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CSharp.Classification
{
    internal class CSharpFallbackEmbeddedLanguageClassifier : AbstractFallbackEmbeddedLanguageClassifier
    {
        public static readonly CSharpFallbackEmbeddedLanguageClassifier Instance = new();

        // https://github.com/dotnet/csharpstandard/blob/standard-v6/standard/lexical-structure.md#6456-string-literals
        // Regular String Literal Character can have these escape character
        // 1. Simple_Escape_Sequence ('\\\'' | '\\"' | '\\\\' | '\\0' | '\\a' | '\\b' | '\\f' | '\\n' | '\\r' | '\\t' | '\\v')
        // 2. Hexadecimal_Escape_Sequence ('\\x' hex_digit)
        // 3. Unicode_Escape_Sequence ('\\u' or '\\U' )
        //  Verbatim string can only escape double quote
        private static readonly ImmutableArray<string> s_regularStringLiteralEscapeStrings = ImmutableArray.Create(
            "\\x", "\\u", "\\U", "\\\'", "\\\"", "\\\\", "\\0", "\\a", "\\b", "\\f", "\\n", "\\r", "\\t", "\\v",
            "\"\"");

        private CSharpFallbackEmbeddedLanguageClassifier()
            : base(CSharpEmbeddedLanguagesProvider.Info)
        {
        }

        protected override bool TextStartWithEscapeCharacter(string text)
            => s_regularStringLiteralEscapeStrings.Any(s => text.StartsWith(s, StringComparison.InvariantCulture));
    }
}
