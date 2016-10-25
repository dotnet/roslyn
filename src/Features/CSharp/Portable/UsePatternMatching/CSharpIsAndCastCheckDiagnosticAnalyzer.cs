// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Microsoft.CodeAnalysis.CSharp.UsePatternMatching
{
    /// <summary>
    /// Looks for code of the form:
    /// 
    ///     if (expr is Type)
    ///     {
    ///         var v = (Type)expr;
    ///     }
    ///     
    /// and converts it to:
    /// 
    ///     if (expr is Type v)
    ///     {
    ///     }
    /// </summary>
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    internal class CSharpIsAndCastCheckDiagnosticAnalyzer : AbstractCodeStyleDiagnosticAnalyzer, IBuiltInAnalyzer
    {
        public bool OpenFileOnly(Workspace workspace) => false;

        public CSharpIsAndCastCheckDiagnosticAnalyzer()
            : base(IDEDiagnosticIds.InlineIsTypeCheckId,
                   new LocalizableResourceString(nameof(FeaturesResources.Use_pattern_matching), FeaturesResources.ResourceManager, typeof(FeaturesResources)))
        {
        }

        public override void Initialize(AnalysisContext context)
        {
            context.RegisterSyntaxNodeAction(SyntaxNodeAction, SyntaxKind.IsExpression);
        }

        private void SyntaxNodeAction(SyntaxNodeAnalysisContext syntaxContext)
        {
            var options = syntaxContext.Options.GetOptionSet();
            var styleOption = options.GetOption(CSharpCodeStyleOptions.PreferPatternMatchingOverIsWithCastCheck);
            if (!styleOption.Value)
            {
                // Bail immediately if the user has disabled this feature.
                return;
            }

            var severity = styleOption.Notification.Value;

            var isExpression = (BinaryExpressionSyntax)syntaxContext.Node;

            // "x is Type y" is only available in C# 7.0 and above.  Don't offer this refactoring
            // in projects targetting a lesser version.
            if (((CSharpParseOptions)isExpression.SyntaxTree.Options).LanguageVersion < LanguageVersion.CSharp7)
            {
                return;
            }

            // The is check has to be in an if check: "if (x is Type)
            if (!isExpression.Parent.IsKind(SyntaxKind.IfStatement))
            {
                return;
            }

            var ifStatement = (IfStatementSyntax)isExpression.Parent;
            if (!ifStatement.Statement.IsKind(SyntaxKind.Block))
            {
                return;
            }

            var ifBlock = (BlockSyntax)ifStatement.Statement;
            if (ifBlock.Statements.Count == 0)
            {
                return;
            }

            var firstStatement = ifBlock.Statements[0];
            if (!firstStatement.IsKind(SyntaxKind.LocalDeclarationStatement))
            {
                return;
            }

            var localDeclarationStatement = (LocalDeclarationStatementSyntax)firstStatement;
            if (localDeclarationStatement.Declaration.Variables.Count != 1)
            {
                return;
            }

            var declarator = localDeclarationStatement.Declaration.Variables[0];
            if (declarator.Initializer == null)
            {
                return;
            }

            var declaratorValue = declarator.Initializer.Value.WalkDownParentheses();
            if (!declaratorValue.IsKind(SyntaxKind.CastExpression))
            {
                return;
            }

            var castExpression = (CastExpressionSyntax)declaratorValue;
            if (!SyntaxFactory.AreEquivalent(isExpression.Left.WalkDownParentheses(), castExpression.Expression.WalkDownParentheses(), topLevel: false) ||
                !SyntaxFactory.AreEquivalent(isExpression.Right.WalkDownParentheses(), castExpression.Type, topLevel: false))
            {
                return;
            }

            // It's of the form:
            //
            //     if (expr is Type)
            //     {
            //         var v = (Type)expr;
            //     }

            // Make sure that moving 'v' to the outer scope won't cause any conflicts.

            var ifStatementScope = ifStatement.Parent.IsKind(SyntaxKind.Block)
                ? ifStatement.Parent
                : ifStatement;

            if (ContainsVariableDeclaration(ifStatementScope, declarator))
            {
                // can't switch to using a pattern here as it would cause a scoping
                // problem.
                //
                // TODO(cyrusn): Consider allowing the user to do this, but giving 
                // them an error preview.
                return;
            }

            // Looks good!
            var additionalLocations = ImmutableArray.Create(
                ifStatement.GetLocation(),
                localDeclarationStatement.GetLocation());

            // Put a diagnostic with the appropriate severity on the declaration-statement itself.
            syntaxContext.ReportDiagnostic(Diagnostic.Create(
                CreateDescriptor(this.DescriptorId, severity),
                localDeclarationStatement.GetLocation(),
                additionalLocations));
        }

        private bool ContainsVariableDeclaration(
            SyntaxNode scope, VariableDeclaratorSyntax variable)
        {
            var variableName = variable.Identifier.ValueText;
            return scope.DescendantNodes()
                        .OfType<VariableDeclaratorSyntax>()
                        .Where(d => d != variable)
                        .Any(d => d.Identifier.ValueText.Equals(variableName));
        }

        public DiagnosticAnalyzerCategory GetAnalyzerCategory()
        {
            return DiagnosticAnalyzerCategory.SemanticDocumentAnalysis;
        }
    }
}