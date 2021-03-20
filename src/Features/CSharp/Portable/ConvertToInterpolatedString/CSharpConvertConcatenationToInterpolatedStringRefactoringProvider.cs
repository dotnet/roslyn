﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.ConvertToInterpolatedString;
using Microsoft.CodeAnalysis.CSharp.Shared.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Host.Mef;

namespace Microsoft.CodeAnalysis.CSharp.ConvertToInterpolatedString
{
    [ExportCodeRefactoringProvider(LanguageNames.CSharp, Name = PredefinedCodeRefactoringProviderNames.ConvertToInterpolatedString), Shared]
    internal class CSharpConvertConcatenationToInterpolatedStringRefactoringProvider :
        AbstractConvertConcatenationToInterpolatedStringRefactoringProvider<ExpressionSyntax>
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public CSharpConvertConcatenationToInterpolatedStringRefactoringProvider()
        {
        }

        protected override bool SupportsConstantInterpolatedStrings(Document document)
            => ((CSharpParseOptions)document.Project.ParseOptions!).LanguageVersion.HasConstantInterpolatedStrings();

        protected override string GetTextWithoutQuotes(string text, bool isVerbatim, bool isCharacterLiteral)
            => isVerbatim
                ? text.Substring("@'".Length, text.Length - "@''".Length)
                : text.Substring("'".Length, text.Length - "''".Length);
    }
}
