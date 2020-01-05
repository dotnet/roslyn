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
                ? CSharpFeaturesResources.Convert_to_switch_expression
                : CSharpFeaturesResources.Convert_to_switch_statement;

        public override Analyzer CreateAnalyzer(ISyntaxFactsService syntaxFacts, ParseOptions options)
        {
            var version = ((CSharpParseOptions)options).LanguageVersion;
            var features =
                (version >= LanguageVersion.CSharp7 ? Feature.SourcePattern | Feature.TypePattern | Feature.CaseGuard : 0) |
                (version >= LanguageVersion.CSharp8 ? Feature.SwitchExpression : 0);
            return new CSharpAnalyzer(syntaxFacts, features);
        }
    }
}
