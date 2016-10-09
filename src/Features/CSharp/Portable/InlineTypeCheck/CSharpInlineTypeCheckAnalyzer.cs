// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Microsoft.CodeAnalysis.CSharp.InlineTypeCheck
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    internal class CSharpInlineTypeCheckDiagnosticAnalyzer : AbstractCodeStyleDiagnosticAnalyzer, IBuiltInAnalyzer
    {
        public bool OpenFileOnly(Workspace workspace) => false;

        public CSharpInlineTypeCheckDiagnosticAnalyzer()
            : base(IDEDiagnosticIds.InlineTypeCheckId,
                   new LocalizableResourceString(nameof(FeaturesResources.Inline_type_check), FeaturesResources.ResourceManager, typeof(FeaturesResources)))
        {
        }

        public override void Initialize(AnalysisContext context)
        {
            context.RegisterSyntaxNodeAction(SyntaxNodeAction, SyntaxKind.IfStatement);
        }

        private void SyntaxNodeAction(SyntaxNodeAnalysisContext syntaxContext)
        {
            var options = syntaxContext.Options.GetOptionSet();
            var styleOption = options.GetOption(CSharpCodeStyleOptions.PreferInlinedTypeCheck);
            if (!styleOption.Value)
            {
                // Bail immediately if the user has disabled this feature.
                return;
            }

            var severity = styleOption.Notification.Value;

            // look for the form "if (a != null)" or "if (null != a)"
            var ifStatement = (IfStatementSyntax)syntaxContext.Node;

            // ?. is only available in C# 6.0 and above.  Don't offer this refactoring
            // in projects targetting a lesser version.
            if (((CSharpParseOptions)ifStatement.SyntaxTree.Options).LanguageVersion < LanguageVersion.CSharp6)
            {
                return;
            }

            if (!ifStatement.Condition.IsKind(SyntaxKind.NotEqualsExpression))
            {
                return;
            }

            var condition = (BinaryExpressionSyntax)ifStatement.Condition;
            CheckVariableAndIfStatementForm(
                syntaxContext, ifStatement, condition, severity);
        }

        private void CheckVariableAndIfStatementForm(
            SyntaxNodeAnalysisContext syntaxContext,
            IfStatementSyntax ifStatement,
            BinaryExpressionSyntax condition,
            DiagnosticSeverity severity)
        {
            var cancellationToken = syntaxContext.CancellationToken;
            cancellationToken.ThrowIfCancellationRequested();

            // look for the form "if (a != null)" or "if (null != a)"
            if (!ifStatement.Parent.IsKind(SyntaxKind.Block))
            {
                return;
            }

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

            cancellationToken.ThrowIfCancellationRequested();
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
                initializerValue.GetLocation());

            // Put a diagnostic with the appropriate severity on the declaration-statement itself.
            syntaxContext.ReportDiagnostic(Diagnostic.Create(
                CreateDescriptor(this.DescriptorId, severity),
                localDeclarationStatement.GetLocation(),
                additionalLocations));
        }

        private bool IsNullCheckExpression(ExpressionSyntax left, ExpressionSyntax right) =>
            left.IsKind(SyntaxKind.IdentifierName) && right.IsKind(SyntaxKind.NullLiteralExpression);

        public DiagnosticAnalyzerCategory GetAnalyzerCategory()
        {
            return DiagnosticAnalyzerCategory.SemanticDocumentAnalysis;
        }
    }
}