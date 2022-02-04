// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.Shared.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Microsoft.CodeAnalysis.CSharp.UsePatternMatching
{
    /// <summary>
    /// Looks for code of the forms:
    /// 
    ///     var x = o as Type;
    ///     if (!(x is Y y)) ...
    /// 
    /// and converts it to:
    /// 
    ///     if (x is not Y y) ...
    ///     
    /// </summary>
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    internal partial class CSharpUseNotPatternDiagnosticAnalyzer : AbstractBuiltInCodeStyleDiagnosticAnalyzer
    {
        public CSharpUseNotPatternDiagnosticAnalyzer()
            : base(IDEDiagnosticIds.UseNotPatternDiagnosticId,
                   EnforceOnBuildValues.UseNotPattern,
                   CSharpCodeStyleOptions.PreferNotPattern,
                   LanguageNames.CSharp,
                   new LocalizableResourceString(
                        nameof(CSharpAnalyzersResources.Use_pattern_matching), CSharpAnalyzersResources.ResourceManager, typeof(CSharpAnalyzersResources)))
        {
        }

        public override DiagnosticAnalyzerCategory GetAnalyzerCategory()
            => DiagnosticAnalyzerCategory.SemanticSpanAnalysis;

        protected override void InitializeWorker(AnalysisContext context)
        {
            context.RegisterCompilationStartAction(context =>
            {
                // "x is not Type y" is only available in C# 9.0 and above. Don't offer this refactoring
                // in projects targeting a lesser version.
                if (context.Compilation.LanguageVersion() < LanguageVersion.CSharp9)
                    return;

                context.RegisterSyntaxNodeAction(n => SyntaxNodeAction(n), SyntaxKind.LogicalNotExpression);
            });
        }

        private void SyntaxNodeAction(SyntaxNodeAnalysisContext syntaxContext)
        {
            var node = syntaxContext.Node;
            var syntaxTree = node.SyntaxTree;

            var options = syntaxContext.Options;
            var cancellationToken = syntaxContext.CancellationToken;

            // Bail immediately if the user has disabled this feature.
            var styleOption = options.GetOption(CSharpCodeStyleOptions.PreferNotPattern, syntaxTree, cancellationToken);
            if (!styleOption.Value)
                return;

            // Look for the form: !(...)
            if (node is not PrefixUnaryExpressionSyntax(SyntaxKind.LogicalNotExpression)
                {
                    Operand: ParenthesizedExpressionSyntax parenthesizedExpression
                })
            {
                return;
            }

            var isKeywordLocation = parenthesizedExpression.Expression switch
            {
                // Look for the form: !(x is Y y) and !(x is const)
                IsPatternExpressionSyntax { Pattern: DeclarationPatternSyntax or ConstantPatternSyntax } isPattern => isPattern.IsKeyword.GetLocation(),

                // Look for the form: !(x is Y)
                BinaryExpressionSyntax(SyntaxKind.IsExpression) { Right: TypeSyntax } isExpression => isExpression.OperatorToken.GetLocation(),

                _ => null
            };

            if (isKeywordLocation is null)
                return;

            syntaxContext.ReportDiagnostic(DiagnosticHelper.Create(
                Descriptor,
                isKeywordLocation,
                styleOption.Notification.Severity,
                ImmutableArray.Create(node.GetLocation()),
                properties: null));
        }
    }
}
