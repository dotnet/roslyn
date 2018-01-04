// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.RegularExpressions;
using Microsoft.CodeAnalysis.Editor.BraceMatching;

namespace Microsoft.CodeAnalysis.Editor.CSharp.BraceMatching
{
    [ExportBraceMatcher(LanguageNames.CSharp)]
    internal class RegexBraceMatcher : AbstractRegexBraceMatcher
    {
        public RegexBraceMatcher()
            : base((int)SyntaxKind.StringLiteralToken,
                   CSharpSyntaxFactsService.Instance,
                   CSharpSemanticFactsService.Instance,
                   CSharpVirtualCharService.Instance)
        {
        }
    }
}
