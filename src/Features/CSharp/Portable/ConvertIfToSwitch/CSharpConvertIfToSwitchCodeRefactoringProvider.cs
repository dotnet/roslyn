// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#nullable enable

using System.Composition;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.ConvertIfToSwitch;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.LanguageServices;

namespace Microsoft.CodeAnalysis.CSharp.ConvertIfToSwitch
{
    [ExportCodeRefactoringProvider(LanguageNames.CSharp, Name = nameof(CSharpConvertIfToSwitchCodeRefactoringProvider)), Shared]
    internal sealed partial class CSharpConvertIfToSwitchCodeRefactoringProvider
        : AbstractConvertIfToSwitchCodeRefactoringProvider<IfStatementSyntax, ExpressionSyntax, BinaryExpressionSyntax, PatternSyntax>
    {
        [ImportingConstructor]
        public CSharpConvertIfToSwitchCodeRefactoringProvider()
        {
        }

        public override string GetTitle(bool forSwitchExpression)
            => forSwitchExpression
                ? "TODO"
                : CSharpFeaturesResources.Convert_to_switch;

        public override Analyzer CreateAnalyzer(ISyntaxFactsService syntaxFacts)
            => new CSharpAnalyzer(syntaxFacts);
    }
}
