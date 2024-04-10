// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.ConvertIfToSwitch;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Shared.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.LanguageService;

namespace Microsoft.CodeAnalysis.CSharp.ConvertIfToSwitch;

[ExportCodeRefactoringProvider(LanguageNames.CSharp, Name = PredefinedCodeRefactoringProviderNames.ConvertIfToSwitch), Shared]
internal sealed partial class CSharpConvertIfToSwitchCodeRefactoringProvider
    : AbstractConvertIfToSwitchCodeRefactoringProvider<IfStatementSyntax, ExpressionSyntax, BinaryExpressionSyntax, PatternSyntax>
{
    [ImportingConstructor]
    [SuppressMessage("RoslynDiagnosticsReliability", "RS0033:Importing constructor should be [Obsolete]", Justification = "Used in test code: https://github.com/dotnet/roslyn/issues/42814")]
    public CSharpConvertIfToSwitchCodeRefactoringProvider()
    {
    }

    public override string GetTitle(bool forSwitchExpression)
        => forSwitchExpression
            ? CSharpFeaturesResources.Convert_to_switch_expression
            : CSharpFeaturesResources.Convert_to_switch_statement;

    public override Analyzer CreateAnalyzer(ISyntaxFacts syntaxFacts, ParseOptions options)
    {
        var version = options.LanguageVersion();
        var features =
            (version >= LanguageVersion.CSharp7 ? Feature.SourcePattern | Feature.IsTypePattern | Feature.CaseGuard : 0) |
            (version >= LanguageVersion.CSharp8 ? Feature.SwitchExpression : 0) |
            (version >= LanguageVersion.CSharp9 ? Feature.RelationalPattern | Feature.OrPattern | Feature.AndPattern | Feature.TypePattern : 0);
        return new CSharpAnalyzer(syntaxFacts, features);
    }

    protected override SyntaxTriviaList GetLeadingTriviaToTransfer(SyntaxNode syntaxToRemove)
    {
        if (syntaxToRemove is (IfStatementSyntax or BlockSyntax) and { Parent: ElseClauseSyntax elseClause } &&
            elseClause.ElseKeyword.LeadingTrivia.Any(t => t.IsSingleOrMultiLineComment()))
        {
            // users sometimes write:
            //
            //  // Comment
            //  else if (x == b)
            //
            // Attempt to move 'comment' over to the switch section.
            return elseClause.ElseKeyword.LeadingTrivia;
        }

        return default;
    }
}
