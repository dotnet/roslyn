// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using System.Threading;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Microsoft.CodeAnalysis.CSharp.UsePatternMatching
{

    /// <summary>
    /// Looks for code of the forms:
    /// <code>
    ///     (x as Y)?.Prop == constant
    /// </code>
    /// and converts it to:
    /// <code>
    ///     x is Y { Prop: constant }
    /// </code>
    /// </summary>
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    internal partial class CSharpAsAndMemberAccessDiagnosticAnalyzer : AbstractBuiltInCodeStyleDiagnosticAnalyzer
    {
        public CSharpAsAndMemberAccessDiagnosticAnalyzer()
            : base(IDEDiagnosticIds.UsePatternMatchingAsAndMemberAccessId,
                   EnforceOnBuildValues.UsePatternMatchingAsAndMemberAccess,
                   CSharpCodeStyleOptions.PreferPatternMatchingOverAsWithNullCheck,
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
                // Recursive patterns (`is X { Prop: Y }`) is only available in C# 8.0 and above. Don't offer this
                // refactoring in projects targeting a lesser version.
                if (context.Compilation.LanguageVersion() < LanguageVersion.CSharp8)
                    return;

                context.RegisterSyntaxNodeAction(context => AnalyzeAsExpression(context), SyntaxKind.AsExpression);
            });
        }

        private void AnalyzeAsExpression(SyntaxNodeAnalysisContext context)
        {
            var styleOption = context.GetCSharpAnalyzerOptions().PreferPatternMatchingOverAsWithNullCheck;
            if (!styleOption.Value)
            {
                // Bail immediately if the user has disabled this feature.
                return;
            }

            var cancellationToken = context.CancellationToken;
            var semanticModel = context.SemanticModel;
            var asExpression = (BinaryExpressionSyntax)context.Node;

            if (!UsePatternMatchingHelpers.TryGetPartsOfAsAndMemberAccessCheck(
                    asExpression, out _, out var binaryExpression, out _))
            {
                return;
            }

            if (binaryExpression != null)
            {
                // `(expr as T)?... == other_expr
                //
                // in this case we can only convert if other_expr is a constant.
                var constantValue = semanticModel.GetConstantValue(binaryExpression.Right, cancellationToken);
                if (!constantValue.HasValue)
                    return;
            }

            // Looks good!
            //var additionalLocations = ImmutableArray.Create(
            //    ifStatement.GetLocation(),
            //    localDeclarationStatement.GetLocation());

            // Put a diagnostic with the appropriate severity on the declaration-statement itself.
            context.ReportDiagnostic(DiagnosticHelper.Create(
                Descriptor,
                asExpression.GetLocation(),
                styleOption.Notification.Severity,
                additionalLocations: null,
                properties: null));
        }
    }
}
