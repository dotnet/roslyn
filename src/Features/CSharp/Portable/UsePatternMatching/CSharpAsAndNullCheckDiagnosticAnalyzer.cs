// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.ExtractMethod;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.UsePatternMatching
{
    /// <summary>
    /// Looks for code of the forms:
    /// 
    ///     var x = o as Type;
    ///     if (x != null) ...
    /// 
    /// and converts it to:
    /// 
    ///     if (o is Type x) ...
    ///     
    /// </summary>
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    internal partial class CSharpAsAndNullCheckDiagnosticAnalyzer : AbstractBuiltInCodeStyleDiagnosticAnalyzer
    {
        public CSharpAsAndNullCheckDiagnosticAnalyzer()
            : base(IDEDiagnosticIds.InlineAsTypeCheckId,
                   CSharpCodeStyleOptions.PreferPatternMatchingOverAsWithNullCheck,
                   LanguageNames.CSharp,
                   new LocalizableResourceString(
                        nameof(FeaturesResources.Use_pattern_matching), FeaturesResources.ResourceManager, typeof(FeaturesResources)))
        {
        }

        protected override void InitializeWorker(AnalysisContext context)
            => context.RegisterSyntaxNodeAction(SyntaxNodeAction,
                SyntaxKind.EqualsExpression,
                SyntaxKind.NotEqualsExpression,
                SyntaxKind.IsExpression,
                SyntaxKind.IsPatternExpression);

        private void SyntaxNodeAction(SyntaxNodeAnalysisContext syntaxContext)
        {
            var node = syntaxContext.Node;
            var syntaxTree = node.SyntaxTree;

            // "x is Type y" is only available in C# 7.0 and above. Don't offer this refactoring
            // in projects targeting a lesser version.
            if (((CSharpParseOptions)syntaxTree.Options).LanguageVersion < LanguageVersion.CSharp7)
            {
                return;
            }

            var options = syntaxContext.Options;
            var cancellationToken = syntaxContext.CancellationToken;

            var styleOption = options.GetOption(CSharpCodeStyleOptions.PreferPatternMatchingOverAsWithNullCheck, syntaxTree, cancellationToken);
            if (!styleOption.Value)
            {
                // Bail immediately if the user has disabled this feature.
                return;
            }

            var comparison = (ExpressionSyntax)node;
            var (comparisonLeft, comparisonRight) = comparison switch
            {
                BinaryExpressionSyntax binaryExpression => (binaryExpression.Left, (SyntaxNode)binaryExpression.Right),
                IsPatternExpressionSyntax isPattern => (isPattern.Expression, isPattern.Pattern),
                _ => throw ExceptionUtilities.Unreachable,
            };
            var operand = GetNullCheckOperand(comparisonLeft, comparison.Kind(), comparisonRight)?.WalkDownParentheses();
            if (operand == null)
            {
                return;
            }

            var semanticModel = syntaxContext.SemanticModel;
            if (operand.IsKind(SyntaxKind.CastExpression, out CastExpressionSyntax castExpression))
            {
                // Unwrap object cast
                var castType = semanticModel.GetTypeInfo(castExpression.Type).Type;
                if (castType.IsObjectType())
                {
                    operand = castExpression.Expression;
                }
            }

            if (semanticModel.GetSymbolInfo(comparison).GetAnySymbol().IsUserDefinedOperator())
            {
                return;
            }

            if (!TryGetTypeCheckParts(semanticModel, operand,
                    out var declarator,
                    out var asExpression,
                    out var localSymbol))
            {
                return;
            }

            var localStatement = declarator.Parent?.Parent;
            var enclosingBlock = localStatement?.Parent;
            if (localStatement == null ||
                enclosingBlock == null)
            {
                return;
            }

            var typeNode = asExpression.Right;
            var asType = semanticModel.GetTypeInfo(typeNode, cancellationToken).Type;
            if (asType.IsNullable())
            {
                // Not legal to write "x is int? y"
                return;
            }

            if (asType?.TypeKind == TypeKind.Dynamic)
            {
                // Not legal to use dynamic in a pattern.
                return;
            }

            if (!localSymbol.Type.Equals(asType))
            {
                // We have something like:
                //
                //      BaseType b = x as DerivedType;
                //      if (b != null) { ... }
                //
                // It's not necessarily safe to convert this to:
                //
                //      if (x is DerivedType b) { ... }
                //
                // That's because there may be later code that wants to do something like assign a
                // 'BaseType' into 'b'.  As we've now claimed that it must be DerivedType, that
                // won't work.  This might also cause unintended changes like changing overload
                // resolution.  So, we conservatively do not offer the change in a situation like this.
                return;
            }

            // Check if the as operand is ever written up to the point of null check.
            //
            //      var s = field as string;
            //      field = null;
            //      if (s != null) { ... }
            //
            // It's no longer safe to use pattern-matching because 'field is string s' would never be true.
            // 
            // Additionally, also bail out if the assigned local is referenced (i.e. read/write/nameof) up to the point of null check.
            //      var s = field as string;
            //      MethodCall(flag: s == null);
            //      if (s != null) { ... }
            //
            var asOperand = semanticModel.GetSymbolInfo(asExpression.Left, cancellationToken).Symbol;
            var localStatementStart = localStatement.SpanStart;
            var comparisonSpanStart = comparison.SpanStart;

            foreach (var descendentNode in enclosingBlock.DescendantNodes())
            {
                var descendentNodeSpanStart = descendentNode.SpanStart;
                if (descendentNodeSpanStart <= localStatementStart)
                {
                    continue;
                }

                if (descendentNodeSpanStart >= comparisonSpanStart)
                {
                    break;
                }

                if (descendentNode.IsKind(SyntaxKind.IdentifierName, out IdentifierNameSyntax identifierName))
                {
                    // Check if this is a 'write' to the asOperand.
                    if (identifierName.Identifier.ValueText == asOperand?.Name &&
                        asOperand.Equals(semanticModel.GetSymbolInfo(identifierName, cancellationToken).Symbol) &&
                        identifierName.IsWrittenTo())
                    {
                        return;
                    }

                    // Check is a reference of any sort (i.e. read/write/nameof) to the local.
                    if (identifierName.Identifier.ValueText == localSymbol.Name)
                    {
                        return;
                    }
                }
            }

            if (!Analyzer.CanSafelyConvertToPatternMatching(
                semanticModel, localSymbol, comparison, operand,
                localStatement, enclosingBlock, cancellationToken))
            {
                return;
            }

            // Looks good!
            var additionalLocations = ImmutableArray.Create(
                declarator.GetLocation(),
                comparison.GetLocation(),
                asExpression.GetLocation());

            // Put a diagnostic with the appropriate severity on the declaration-statement itself.
            syntaxContext.ReportDiagnostic(DiagnosticHelper.Create(
                Descriptor,
                localStatement.GetLocation(),
                styleOption.Notification.Severity,
                additionalLocations,
                properties: null));
        }

        private static bool TryGetTypeCheckParts(
            SemanticModel semanticModel,
            SyntaxNode operand,
            out VariableDeclaratorSyntax declarator,
            out BinaryExpressionSyntax asExpression,
            out ILocalSymbol localSymbol)
        {
            switch (operand.Kind())
            {
                case SyntaxKind.IdentifierName:
                    {
                        // var x = e as T;
                        // if (x != null) F(x);
                        var identifier = (IdentifierNameSyntax)operand;
                        if (!TryFindVariableDeclarator(semanticModel, identifier, out localSymbol, out declarator))
                        {
                            break;
                        }

                        var initializerValue = declarator.Initializer?.Value;
                        if (!initializerValue.IsKind(SyntaxKind.AsExpression, out asExpression))
                        {
                            break;
                        }

                        return true;
                    }

                case SyntaxKind.SimpleAssignmentExpression:
                    {
                        // T x;
                        // if ((x = e as T) != null) F(x);
                        var assignment = (AssignmentExpressionSyntax)operand;
                        if (!assignment.Right.IsKind(SyntaxKind.AsExpression, out asExpression) ||
                            !assignment.Left.IsKind(SyntaxKind.IdentifierName, out IdentifierNameSyntax identifier))
                        {
                            break;
                        }

                        if (!TryFindVariableDeclarator(semanticModel, identifier, out localSymbol, out declarator))
                        {
                            break;
                        }

                        return true;
                    }
            }

            declarator = null;
            asExpression = null;
            localSymbol = null;
            return false;
        }

        private static bool TryFindVariableDeclarator(
            SemanticModel semanticModel,
            IdentifierNameSyntax identifier,
            out ILocalSymbol localSymbol,
            out VariableDeclaratorSyntax declarator)
        {
            localSymbol = semanticModel.GetSymbolInfo(identifier).Symbol as ILocalSymbol;
            declarator = localSymbol?.DeclaringSyntaxReferences.FirstOrDefault()?.GetSyntax() as VariableDeclaratorSyntax;
            return declarator != null;
        }

        private static ExpressionSyntax GetNullCheckOperand(ExpressionSyntax left, SyntaxKind comparisonKind, SyntaxNode right)
        {
            if (left.IsKind(SyntaxKind.NullLiteralExpression))
            {
                // null == x
                // null != x
                return (ExpressionSyntax)right;
            }

            if (right.IsKind(SyntaxKind.NullLiteralExpression))
            {
                // x == null
                // x != null
                return left;
            }

            if (right.IsKind(SyntaxKind.PredefinedType, out PredefinedTypeSyntax predefinedType)
                && predefinedType.Keyword.IsKind(SyntaxKind.ObjectKeyword)
                && comparisonKind == SyntaxKind.IsExpression)
            {
                // x is object
                return left;
            }

            if (right.IsKind(SyntaxKind.ConstantPattern, out ConstantPatternSyntax constantPattern)
                && constantPattern.Expression.IsKind(SyntaxKind.NullLiteralExpression)
                && comparisonKind == SyntaxKind.IsPatternExpression)
            {
                // x is null
                return left;
            }

            return null;
        }

        public override DiagnosticAnalyzerCategory GetAnalyzerCategory()
            => DiagnosticAnalyzerCategory.SemanticSpanAnalysis;
    }
}
