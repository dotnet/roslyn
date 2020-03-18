﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Composition;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.ConvertNumericLiteral;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Microsoft.CodeAnalysis.CSharp.ConvertNumericLiteral
{
    [ExportCodeRefactoringProvider(LanguageNames.CSharp, Name = nameof(CSharpConvertNumericLiteralCodeRefactoringProvider)), Shared]
    internal sealed class CSharpConvertNumericLiteralCodeRefactoringProvider : AbstractConvertNumericLiteralCodeRefactoringProvider<LiteralExpressionSyntax>
    {
        [ImportingConstructor]
        public CSharpConvertNumericLiteralCodeRefactoringProvider()
        {
        }

        protected override (string hexPrefix, string binaryPrefix) GetNumericLiteralPrefixes() => (hexPrefix: "0x", binaryPrefix: "0b");
    }
}
