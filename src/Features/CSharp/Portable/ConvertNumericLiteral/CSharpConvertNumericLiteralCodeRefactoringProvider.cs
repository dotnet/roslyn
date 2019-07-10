// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.ConvertNumericLiteral;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CSharp.ConvertNumericLiteral
{
    [ExportCodeRefactoringProvider(LanguageNames.CSharp, Name = nameof(CSharpConvertNumericLiteralCodeRefactoringProvider)), Shared]
    internal sealed class CSharpConvertNumericLiteralCodeRefactoringProvider : AbstractConvertNumericLiteralCodeRefactoringProvider
    {
        [ImportingConstructor]
        public CSharpConvertNumericLiteralCodeRefactoringProvider()
        {
        }

        protected override (string hexPrefix, string binaryPrefix) GetNumericLiteralPrefixes() => (hexPrefix: "0x", binaryPrefix: "0b");

        internal override async Task<SyntaxToken> GetNumericTokenAsync(Document document, TextSpan span, CancellationToken cancellationToken)
        {
            var expressionNode = await GetNumericLiteralExpression<LiteralExpressionSyntax>(document, span, cancellationToken).ConfigureAwait(false);
            return expressionNode != null
                ? expressionNode.Token
                : default;
        }
    }
}
