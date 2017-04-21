// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using System.Threading;
using Microsoft.CodeAnalysis.Shared.Extensions;

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
    internal class CSharpAsAndNullCheckDiagnosticAnalyzer : AbstractCodeStyleDiagnosticAnalyzer
    {
        public override bool OpenFileOnly(Workspace workspace) => false;

        public CSharpAsAndNullCheckDiagnosticAnalyzer()
            : base(IDEDiagnosticIds.InlineAsTypeCheckId,
                   new LocalizableResourceString(
                       nameof(FeaturesResources.Use_pattern_matching), FeaturesResources.ResourceManager, typeof(FeaturesResources)))
        {
        }

        protected override void InitializeWorker(AnalysisContext context)
            => context.RegisterSyntaxNodeAction(SyntaxNodeAction, SyntaxKind.IfStatement);

        private void SyntaxNodeAction(SyntaxNodeAnalysisContext syntaxContext)
        {
            var options = syntaxContext.Options;
            var syntaxTree = syntaxContext.Node.SyntaxTree;
            var cancellationToken = syntaxContext.CancellationToken;
            var optionSet = options.GetDocumentOptionSetAsync(syntaxTree, cancellationToken).GetAwaiter().GetResult();
            if (optionSet == null)
            {
                return;
            }

            var styleOption = optionSet.GetOption(CSharpCodeStyleOptions.PreferPatternMatchingOverAsWithNullCheck);
            if (!styleOption.Value)
            {
                // Bail immediately if the user has disabled this feature.
                return;
            }

            var severity = styleOption.Notification.Value;

            // look for the form "if (a != null)" or "if (null != a)"
            var ifStatement = (IfStatementSyntax)syntaxContext.Node;

            // "x is Type y" is only available in C# 7.0 and above.  Don't offer this refactoring
            // in projects targeting a lesser version.
            if (((CSharpParseOptions)ifStatement.SyntaxTree.Options).LanguageVersion < LanguageVersion.CSharp7)
            {
                return;
            }

            // If has to be in a block so we can at least look for a preceding local variable declaration.
            if (!ifStatement.Parent.IsKind(SyntaxKind.Block))
            {
                return;
            }

            // We need to find the leftmost expression in the if-condition.  If this is a
            // "x != null" expression, then we can replace it with "o is Type x".  
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

            var semanticModel = syntaxContext.SemanticModel;
            var asExpression = (BinaryExpressionSyntax)initializerValue;
            var typeNode = (TypeSyntax)asExpression.Right;
            var asType = semanticModel.GetTypeInfo(typeNode, cancellationToken).Type;
            if (asType.IsNullable())
            {
                // not legal to write "if (x is int? y)"
                return;
            }

            var localSymbol = (ILocalSymbol)semanticModel.GetDeclaredSymbol(variableDeclaration.Variables[0]);
            if (!localSymbol.Type.Equals(asType))
            {
                // we have something like:
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

            // If we convert this to 'if (o is Type x)' then 'x' will not be definitely assigned 
            // in the Else branch of the IfStatement, or after the IfStatement.  Make sure 
            // that doesn't cause definite assignment issues.
            if (IsAccessedBeforeAssignment(syntaxContext, declarator, ifStatement, cancellationToken))
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
                GetDescriptorWithSeverity(severity),
                localDeclarationStatement.GetLocation(),
                additionalLocations));
        }

        private bool IsAccessedBeforeAssignment(
            SyntaxNodeAnalysisContext syntaxContext,
            VariableDeclaratorSyntax declarator,
            IfStatementSyntax ifStatement,
            CancellationToken cancellationToken)
        {
            var semanticModel = syntaxContext.SemanticModel;
            var localVariable = semanticModel.GetDeclaredSymbol(declarator);

            var isAssigned = false;
            var isAccessedBeforeAssignment = false;

            CheckDefiniteAssignment(
                semanticModel, localVariable, ifStatement.Else,
                out isAssigned, out isAccessedBeforeAssignment,
                cancellationToken);

            if (isAccessedBeforeAssignment)
            {
                return true;
            }

            var parentBlock = (BlockSyntax)ifStatement.Parent;
            var ifIndex = parentBlock.Statements.IndexOf(ifStatement);
            for (int i = ifIndex + 1, n = parentBlock.Statements.Count; i < n; i++)
            {
                if (!isAssigned)
                {
                    CheckDefiniteAssignment(
                        semanticModel, localVariable, parentBlock.Statements[i],
                        out isAssigned, out isAccessedBeforeAssignment,
                        cancellationToken);

                    if (isAccessedBeforeAssignment)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private void CheckDefiniteAssignment(
            SemanticModel semanticModel, ISymbol localVariable, SyntaxNode node,
            out bool isAssigned, out bool isAccessedBeforeAssignment,
            CancellationToken cancellationToken)
        {
            if (node != null)
            {
                foreach (var id in node.DescendantNodes().OfType<IdentifierNameSyntax>())
                {
                    var symbol = semanticModel.GetSymbolInfo(id, cancellationToken).GetAnySymbol();
                    if (localVariable.Equals(symbol))
                    {
                        isAssigned = id.IsOnlyWrittenTo();
                        isAccessedBeforeAssignment = !isAssigned;
                        return;
                    }
                }
            }

            isAssigned = false;
            isAccessedBeforeAssignment = false;
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

        public override DiagnosticAnalyzerCategory GetAnalyzerCategory()
            => DiagnosticAnalyzerCategory.SemanticDocumentAnalysis;
    }
}