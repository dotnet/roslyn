// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Composition;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.ConvertToInterpolatedString;

namespace Microsoft.CodeAnalysis.CSharp.ConvertToInterpolatedString
{
    [ExportCodeRefactoringProvider(LanguageNames.CSharp, Name = PredefinedCodeRefactoringProviderNames.ConvertToInterpolatedString), Shared]
    internal class CSharpConvertConcatenationToInterpolatedStringRefactoringProvider :
        AbstractConvertConcatenationToInterpolatedStringRefactoringProvider
    {
        private const string InterpolatedVerbatimText = "$@\"";

        [ImportingConstructor]
        public CSharpConvertConcatenationToInterpolatedStringRefactoringProvider()
        {
        }

        protected override SyntaxToken CreateInterpolatedStringStartToken(bool isVerbatim)
        {
            return isVerbatim
                ? SyntaxFactory.Token(default, SyntaxKind.InterpolatedVerbatimStringStartToken, InterpolatedVerbatimText, InterpolatedVerbatimText, default)
                : SyntaxFactory.Token(SyntaxKind.InterpolatedStringStartToken);
        }

        protected override SyntaxToken CreateInterpolatedStringEndToken()
            => SyntaxFactory.Token(SyntaxKind.InterpolatedStringEndToken);

        protected override string GetTextWithoutQuotes(string text, bool isVerbatim, bool isCharacterLiteral)
            => isVerbatim
                ? text.Substring("@'".Length, text.Length - "@''".Length)
                : text.Substring("'".Length, text.Length - "''".Length);
    }
}
