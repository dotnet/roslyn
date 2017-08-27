﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Composition;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.ConvertNumericLiteral;

namespace Microsoft.CodeAnalysis.CSharp.ConvertNumericLiteral
{
    [ExportCodeRefactoringProvider(LanguageNames.CSharp, Name = nameof(CSharpConvertNumericLiteralCodeRefactoringProvider)), Shared]
    internal sealed class CSharpConvertNumericLiteralCodeRefactoringProvider : AbstractConvertNumericLiteralCodeRefactoringProvider
    {
        protected override (string hexPrefix, string binaryPrefix) GetNumericLiteralPrefixes() => (hexPrefix: "0x", binaryPrefix: "0b");
    }
}
