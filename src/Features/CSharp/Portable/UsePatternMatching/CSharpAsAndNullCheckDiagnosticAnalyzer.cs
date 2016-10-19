// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Microsoft.CodeAnalysis.CSharp.UsePatternMatching
{
    /// <summary>
    /// Looks for code of the form:
    /// 
    ///     var x = o as Type;
    ///     if (x != null) ...
    ///     
    /// and converts it to:
    /// 
    ///     if (o is Type x) ...
    /// </summary>
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    internal class CSharpAsAndNullCheckDiagnosticAnalyzer : AbstractCodeStyleDiagnosticAnalyzer, IBuiltInAnalyzer
    {
        public bool OpenFileOnly(Workspace workspace) => false;

        public CSharpAsAndNullCheckDiagnosticAnalyzer()
            : base(IDEDiagnosticIds.InlineAsTypeCheckId,
                   new LocalizableResourceString(nameof(FeaturesResources.Use_pattern_matching), FeaturesResources.ResourceManager, typeof(FeaturesResources)))
        {
        }

        public override void Initialize(AnalysisContext context)
        {
            context.RegisterSyntaxNodeAction(SyntaxNodeAction, SyntaxKind.IfStatement);
        }

        private void SyntaxNodeAction(SyntaxNodeAnalysisContext syntaxContext)
        {
            var options = syntaxContext.Options.GetOptionSet();
            var styleOption = options.GetOption(CSharpCodeStyleOptions.PreferPatternMatchingOverAsWithNullCheck);
            if (!styleOption.Value)
            {
                // Bail immediately if the user has disabled this feature.
                return;
            }

            var severity = styleOption.Notification.Value;

            // look for the form "if (a != null)" or "if (null != a)"
            var ifStatement = (IfStatementSyntax)syntaxContext.Node;

            // "x is Type y" is only available in C# 7.0 and above.  Don't offer this refactoring
            // in projects targetting a lesser version.
            if (((CSharpParseOptions)ifStatement.SyntaxTree.Options).LanguageVersion < LanguageVersion.CSharp7)
            {
                return;
            }

            // If has to be in a block so we can at least look for a preceding local variable declaration.
            if (!ifStatement.Parent.IsKind(SyntaxKind.Block))
            {
                return;
            }

            // We need to find the leftmost expression in teh if-condition.  If this is a
            // "x != null" expression the we can replace it with "o as Type x".  
            var condition = GetLeftmostCondition(ifStatement.Condition);
            if (!condition.IsKind(SyntaxKind.NotEqualsExpression))
            {
                return;
            }

            // look for the form "x != null" or "null != x".
            if (!IsNullCheckExpression(condition.Left, condition.Right) &&
                !IsNullCheckExpression(condition.Right, condition.Left))
            {
                return;
            }

            var conditionName = condition.Left is IdentifierNameSyntax
                ? (IdentifierNameSyntax)condition.Left
                : (IdentifierNameSyntax)condition.Right;

            // Now make sure the previous statement is "var a = ..."
            var parentBlock = (BlockSyntax)ifStatement.Parent;
            var ifIndex = parentBlock.Statements.IndexOf(ifStatement);
            if (ifIndex == 0)
            {
                return;
            }

            var previousStatement = parentBlock.Statements[ifIndex - 1];
            if (!previousStatement.IsKind(SyntaxKind.LocalDeclarationStatement))
            {
                return;
            }

            var localDeclarationStatement = (LocalDeclarationStatementSyntax)previousStatement;
            var variableDeclaration = localDeclarationStatement.Declaration;

            if (variableDeclaration.Variables.Count != 1)
            {
                return;
            }

            var declarator = variableDeclaration.Variables[0];
            if (declarator.Initializer == null)
            {
                return;
            }

            if (!Equals(declarator.Identifier.ValueText, conditionName.Identifier.ValueText))
            {
                return;
            }

            // Make sure the initializer has the form "... = expr as Type;
            var initializerValue = declarator.Initializer.Value;
            if (!initializerValue.IsKind(SyntaxKind.AsExpression))
            {
                return;
            }

            // Looks good!
            var additionalLocations = ImmutableArray.Create(
                localDeclarationStatement.GetLocation(),
                ifStatement.GetLocation(),
                condition.GetLocation(),
                initializerValue.GetLocation());

            // Put a diagnostic with the appropriate severity on the declaration-statement itself.
            syntaxContext.ReportDiagnostic(Diagnostic.Create(
                CreateDescriptor(this.DescriptorId, severity),
                localDeclarationStatement.GetLocation(),
                additionalLocations));
        }

        private BinaryExpressionSyntax GetLeftmostCondition(ExpressionSyntax condition)
        {
            switch (condition.Kind())
            {
                case SyntaxKind.ParenthesizedExpression:
                    return GetLeftmostCondition(((ParenthesizedExpressionSyntax)condition).Expression);
                case SyntaxKind.ConditionalExpression:
                    return GetLeftmostCondition(((ConditionalExpressionSyntax)condition).Condition);
                case SyntaxKind.LogicalAndExpression:
                case SyntaxKind.LogicalOrExpression:
                    return GetLeftmostCondition(((BinaryExpressionSyntax)condition).Left);
            }

            return condition as BinaryExpressionSyntax;
        }

        private bool IsNullCheckExpression(ExpressionSyntax left, ExpressionSyntax right) =>
            left.IsKind(SyntaxKind.IdentifierName) && right.IsKind(SyntaxKind.NullLiteralExpression);

        public DiagnosticAnalyzerCategory GetAnalyzerCategory()
        {
            return DiagnosticAnalyzerCategory.SemanticDocumentAnalysis;
        }
    }
}