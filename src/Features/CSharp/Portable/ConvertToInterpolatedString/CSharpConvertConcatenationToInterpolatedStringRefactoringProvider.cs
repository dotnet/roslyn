// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.ConvertToInterpolatedString;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Host.Mef;

namespace Microsoft.CodeAnalysis.CSharp.ConvertToInterpolatedString;

[ExportCodeRefactoringProvider(LanguageNames.CSharp, Name = PredefinedCodeRefactoringProviderNames.ConvertConcatenationToInterpolatedString), Shared]
internal sealed class CSharpConvertConcatenationToInterpolatedStringRefactoringProvider :
    AbstractConvertConcatenationToInterpolatedStringRefactoringProvider<ExpressionSyntax>
{
    [ImportingConstructor]
    [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    public CSharpConvertConcatenationToInterpolatedStringRefactoringProvider()
    {
    }

    protected override bool SupportsInterpolatedStringHandler(Compilation compilation)
        => compilation.GetTypeByMetadataName("System.Runtime.CompilerServices.DefaultInterpolatedStringHandler") != null;

    protected override string GetTextWithoutQuotes(string text, bool isVerbatim, bool isCharacterLiteral)
        => isVerbatim
            ? text.Substring("@'".Length, text.Length - "@''".Length)
            : text.Substring("'".Length, text.Length - "''".Length);
}
