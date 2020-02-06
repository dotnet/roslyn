// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Composition;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.ConvertToInterpolatedString;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.LanguageServices;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.ConvertToInterpolatedString
{
    [ExportCodeRefactoringProvider(LanguageNames.CSharp, Name = PredefinedCodeRefactoringProviderNames.ConvertToInterpolatedString), Shared]
    internal class CSharpConvertConcatenationToInterpolatedStringRefactoringProvider :
        AbstractConvertConcatenationToInterpolatedStringRefactoringProvider<ExpressionSyntax>
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
