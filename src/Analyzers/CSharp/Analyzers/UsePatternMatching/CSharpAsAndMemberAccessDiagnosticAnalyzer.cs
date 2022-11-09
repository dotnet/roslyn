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
using Roslyn.Utilities;

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
                    asExpression, out _, out var binaryExpression, out var isPatternExpression, out var requiredLanguageVersion))
            {
                return;
            }

            if (context.Compilation.LanguageVersion() < requiredLanguageVersion)
                return;

            if (!IsSafeToConvert(semanticModel, binaryExpression, isPatternExpression, cancellationToken))
                return;

            // Looks good!

            // Put a diagnostic with the appropriate severity on the declaration-statement itself.
            context.ReportDiagnostic(DiagnosticHelper.Create(
                Descriptor,
                asExpression.GetLocation(),
                styleOption.Notification.Severity,
                additionalLocations: null,
                properties: null));
        }

        private bool IsSafeToConvert(
            SemanticModel semanticModel,
            BinaryExpressionSyntax? binaryExpression,
            IsPatternExpressionSyntax? isPatternExpression,
            CancellationToken cancellationToken)
        {
            if (binaryExpression != null)
            {
                // `(expr as T)?... == other_expr
                //
                // in this case we can only convert if other_expr is a constant.
                var constantValue = semanticModel.GetConstantValue(binaryExpression.Right, cancellationToken);
                if (!constantValue.HasValue)
                    return false;

                if (binaryExpression.Kind() is SyntaxKind.EqualsExpression)
                {
                    // (a as T)?.Prop == null
                    //
                    // The previous semantics are effectively:
                    //
                    //      a is not T || ((T)a).Prop == null
                    //
                    // The new semantics would be:
                    //
                    //      a is T && ((T)a).Prop == null
                    //
                    // Which is not the same.  So we cannot allow this.
                    //
                    // However: `(a as T)?.Prop != null` is safe to convert as it never will be true when the `as` cast
                    // fails.
                    if (constantValue.Value is null)
                        return false;
                }
                else if (binaryExpression.Kind() is SyntaxKind.NotEqualsExpression)
                {
                    // (a as T)?.Prop != constant
                    //
                    // The previous semantics are effectively:
                    //
                    //      a is not T || ((T)a).Prop != constant
                    //
                    // The new semantics would be:
                    //
                    //      a is T && ((T)a).Prop != constant
                    //
                    // Which is not the same.  So we cannot allow this.
                    //
                    // However: `(a as T)?.Prop == constant` is safe to convert as it never will be true when the `as` cast
                    // fails.
                    if (constantValue.Value is not null)
                        return false;
                }

                // don't need to check the other relational comparisons. These comparisons do a null check themselves,
                // so it's safe to add a null-check with the 'is'.
                return true;
            }
            else
            {
                Contract.ThrowIfNull(isPatternExpression);

                // similar to the binary cases above.

                if (isPatternExpression.Pattern is ConstantPatternSyntax { Expression: var expression1 })
                {
                    var constantValue = semanticModel.GetConstantValue(expression1, cancellationToken);
                    if (!constantValue.HasValue)
                        return false;

                    if (constantValue.Value is null)
                        return false;
                }
                else if (isPatternExpression.Pattern is UnaryPatternSyntax { Pattern: ConstantPatternSyntax { Expression: var expression2 } })
                {
                    var constantValue = semanticModel.GetConstantValue(expression2, cancellationToken);
                    if (!constantValue.HasValue)
                        return false;

                    if (constantValue.Value is not null)
                        return false;
                }

                return true;
            }
        }
    }
}
