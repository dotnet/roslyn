// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.CSharp.UsePatternCombinators
{
    using static AnalyzedPattern;

    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    internal sealed class CSharpUsePatternCombinatorsDiagnosticAnalyzer :
        AbstractBuiltInCodeStyleDiagnosticAnalyzer
    {
        private const string SafeKey = "safe";

        private static readonly LocalizableResourceString s_safePatternTitle = new(nameof(CSharpAnalyzersResources.Use_pattern_matching), CSharpAnalyzersResources.ResourceManager, typeof(CSharpAnalyzersResources));
        private static readonly LocalizableResourceString s_unsafePatternTitle = new(nameof(CSharpAnalyzersResources.Use_pattern_matching_may_change_code_meaning), CSharpAnalyzersResources.ResourceManager, typeof(CSharpAnalyzersResources));

        private static readonly ImmutableDictionary<string, string?> s_safeProperties = ImmutableDictionary<string, string?>.Empty.Add(SafeKey, "");
        private static readonly DiagnosticDescriptor s_unsafeDescriptor = CreateDescriptorWithId(
            IDEDiagnosticIds.UsePatternCombinatorsDiagnosticId,
            EnforceOnBuildValues.UsePatternCombinators,
            s_unsafePatternTitle);

        public CSharpUsePatternCombinatorsDiagnosticAnalyzer()
            : base(IDEDiagnosticIds.UsePatternCombinatorsDiagnosticId,
                EnforceOnBuildValues.UsePatternCombinators,
                CSharpCodeStyleOptions.PreferPatternMatching,
                LanguageNames.CSharp,
                s_safePatternTitle,
                s_safePatternTitle)
        {
        }

        public static bool IsSafe(Diagnostic diagnostic)
            => diagnostic.Properties.ContainsKey(SafeKey);

        protected override void InitializeWorker(AnalysisContext context)
            => context.RegisterSyntaxNodeAction(AnalyzeNode,
                SyntaxKind.LogicalAndExpression,
                SyntaxKind.LogicalOrExpression,
                SyntaxKind.LogicalNotExpression);

        private void AnalyzeNode(SyntaxNodeAnalysisContext context)
        {
            var expression = (ExpressionSyntax)context.Node;
            if (expression.GetDiagnostics().Any(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error))
                return;

            // Bail if this is not a topmost expression
            // to avoid overlapping diagnostics.
            if (!IsTopmostExpression(expression))
                return;

            var syntaxTree = expression.SyntaxTree;
            if (syntaxTree.Options.LanguageVersion() < LanguageVersion.CSharp9)
                return;

            var cancellationToken = context.CancellationToken;
            var styleOption = context.Options.GetOption(CSharpCodeStyleOptions.PreferPatternMatching, syntaxTree, cancellationToken);
            if (!styleOption.Value)
                return;

            var semanticModel = context.SemanticModel;
            var expressionType = semanticModel.Compilation.ExpressionOfTType();
            if (expression.IsInExpressionTree(semanticModel, expressionType, cancellationToken))
                return;

            var operation = semanticModel.GetOperation(expression, cancellationToken);
            if (operation is null)
                return;

            var pattern = CSharpUsePatternCombinatorsAnalyzer.Analyze(operation);
            if (pattern is null)
                return;

            // Avoid rewriting trivial patterns, such as a single relational or a constant pattern.
            if (IsTrivial(pattern))
                return;

            // C# 9.0 does not support pattern variables under `not` and `or` combinators,
            // except for top-level `not` patterns.
            if (HasIllegalPatternVariables(pattern, isTopLevel: true))
                return;

            // if the target (the common expression in the pattern) is a method call,
            // then we can't guarantee that the rewritting won't have side-effects,
            // so we should warn the user
            var isSafe = UnwrapImplicitConversion(pattern.Target) is not Operations.IInvocationOperation;

            context.ReportDiagnostic(DiagnosticHelper.Create(
                descriptor: isSafe ? this.Descriptor : s_unsafeDescriptor,
                expression.GetLocation(),
                styleOption.Notification.Severity,
                additionalLocations: null,
                properties: isSafe ? s_safeProperties : null));
        }

        private static IOperation UnwrapImplicitConversion(IOperation operation)
            => operation is IConversionOperation conversion && conversion.IsImplicit
                ? conversion.Operand
                : operation;

        private static bool HasIllegalPatternVariables(AnalyzedPattern pattern, bool permitDesignations = true, bool isTopLevel = false)
        {
            switch (pattern)
            {
                case Not p:
                    return HasIllegalPatternVariables(p.Pattern, permitDesignations: isTopLevel);
                case Binary p:
                    if (p.IsDisjunctive)
                        permitDesignations = false;
                    return HasIllegalPatternVariables(p.Left, permitDesignations) ||
                           HasIllegalPatternVariables(p.Right, permitDesignations);
                case Source p when !permitDesignations:
                    return p.PatternSyntax.DescendantNodes()
                        .OfType<SingleVariableDesignationSyntax>()
                        .Any(variable => !variable.Identifier.IsMissing);
                default:
                    return false;
            }
        }

        private static bool IsTopmostExpression(ExpressionSyntax node)
        {
            return node.WalkUpParentheses().Parent switch
            {
                LambdaExpressionSyntax _ => true,
                AssignmentExpressionSyntax _ => true,
                ConditionalExpressionSyntax _ => true,
                ExpressionSyntax _ => false,
                _ => true
            };
        }

        private static bool IsTrivial(AnalyzedPattern pattern)
        {
            return pattern switch
            {
                Not { Pattern: Constant _ } => true,
                Not { Pattern: Source { PatternSyntax: ConstantPatternSyntax _ } } => true,
                Not _ => false,
                Binary _ => false,
                _ => true
            };
        }

        public override DiagnosticAnalyzerCategory GetAnalyzerCategory()
            => DiagnosticAnalyzerCategory.SemanticSpanAnalysis;
    }
}
