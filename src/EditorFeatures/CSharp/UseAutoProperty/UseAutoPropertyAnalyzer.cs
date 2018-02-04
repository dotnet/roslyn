// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.UseAutoProperty;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UseAutoProperty
{
    [Export]
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    internal class CSharpUseAutoPropertyAnalyzer : AbstractUseAutoPropertyAnalyzer<PropertyDeclarationSyntax, FieldDeclarationSyntax, VariableDeclaratorSyntax, ExpressionSyntax>
    {
        protected override bool SupportsReadOnlyProperties(Compilation compilation)
            => ((CSharpCompilation)compilation).LanguageVersion >= LanguageVersion.CSharp6;

        protected override bool SupportsPropertyInitializer(Compilation compilation)
            => ((CSharpCompilation)compilation).LanguageVersion >= LanguageVersion.CSharp6;

        protected override bool CanExplicitInterfaceImplementationsBeFixed()
            => false;

        protected override void AnalyzeCompilationUnit(
            SemanticModelAnalysisContext context, SyntaxNode root, List<AnalysisResult> analysisResults)
            => AnalyzeMembers(context, ((CompilationUnitSyntax)root).Members, analysisResults);

        private void AnalyzeMembers(
            SemanticModelAnalysisContext context,
            SyntaxList<MemberDeclarationSyntax> members,
            List<AnalysisResult> analysisResults)
        {
            foreach (var memberDeclaration in members)
            {
                AnalyzeMemberDeclaration(context, memberDeclaration, analysisResults);
            }
        }

        private void AnalyzeMemberDeclaration(
            SemanticModelAnalysisContext context,
            MemberDeclarationSyntax member,
            List<AnalysisResult> analysisResults)
        {
            if (member.IsKind(SyntaxKind.NamespaceDeclaration, out NamespaceDeclarationSyntax namespaceDeclaration))
            {
                AnalyzeMembers(context, namespaceDeclaration.Members, analysisResults);
            }
            else if (member.IsKind(SyntaxKind.ClassDeclaration, out TypeDeclarationSyntax typeDeclaration) ||
                member.IsKind(SyntaxKind.StructDeclaration, out typeDeclaration))
            {
                // If we have a class or struct, recurse inwards.
                AnalyzeMembers(context, typeDeclaration.Members, analysisResults);
            }
            else if (member.IsKind(SyntaxKind.PropertyDeclaration, out PropertyDeclarationSyntax propertyDeclaration))
            {
                AnalyzeProperty(context, propertyDeclaration, analysisResults);
            }
        }

        protected override void RegisterIneligibleFieldsAction(
            List<AnalysisResult> analysisResults, HashSet<IFieldSymbol> ineligibleFields,
            Compilation compilation, CancellationToken cancellationToken)
        {
            var groups = analysisResults.Select(r => (TypeDeclarationSyntax)r.PropertyDeclaration.Parent)
                                        .Distinct()
                                        .GroupBy(n => n.SyntaxTree);

            foreach (var group in groups)
            {
                var tree = group.Key;
                var semanticModel = compilation.GetSemanticModel(tree);

                foreach (var typeDeclaration in group)
                {
                    foreach (var argument in typeDeclaration.DescendantNodesAndSelf().OfType<ArgumentSyntax>())
                    {
                        AnalyzeArgument(semanticModel, argument, ineligibleFields, cancellationToken);
                    }
                }
            }
        }

        protected override ExpressionSyntax GetFieldInitializer(VariableDeclaratorSyntax variable, CancellationToken cancellationToken)
        {
            return variable.Initializer?.Value;
        }

        private void AnalyzeArgument(
            SemanticModel semanticModel, ArgumentSyntax argument,
            HashSet<IFieldSymbol> ineligibleFields, CancellationToken cancellationToken)
        {
            // An argument will disqualify a field if that field is used in a ref/out position.  
            // We can't change such field references to be property references in C#.
            if (argument.RefOrOutKeyword.Kind() == SyntaxKind.None)
            {
                return;
            }

            var symbolInfo = semanticModel.GetSymbolInfo(argument.Expression, cancellationToken);
            AddIneligibleField(symbolInfo.Symbol, ineligibleFields);
            foreach (var symbol in symbolInfo.CandidateSymbols)
            {
                AddIneligibleField(symbol, ineligibleFields);
            }
        }

        private static void AddIneligibleField(ISymbol symbol, HashSet<IFieldSymbol> ineligibleFields)
        {
            if (symbol is IFieldSymbol field)
            {
                ineligibleFields.Add(field);
            }
        }

        private bool CheckExpressionSyntactically(ExpressionSyntax expression)
        {
            if (expression.IsKind(SyntaxKind.SimpleMemberAccessExpression))
            {
                var memberAccessExpression = (MemberAccessExpressionSyntax)expression;
                return memberAccessExpression.Expression.Kind() == SyntaxKind.ThisExpression &&
                    memberAccessExpression.Name.Kind() == SyntaxKind.IdentifierName;
            }
            else if (expression.IsKind(SyntaxKind.IdentifierName))
            {
                return true;
            }

            return false;
        }

        protected override ExpressionSyntax GetGetterExpression(IMethodSymbol getMethod, CancellationToken cancellationToken)
        {
            // Getter has to be of the form:
            //
            //     get { return field; } or
            //     get { return this.field; }
            var getAccessor = getMethod.DeclaringSyntaxReferences[0].GetSyntax(cancellationToken) as AccessorDeclarationSyntax;
            var statements = getAccessor?.Body?.Statements;
            if (statements?.Count == 1)
            {
                var statement = statements.Value[0];
                if (statement.Kind() == SyntaxKind.ReturnStatement)
                {
                    var expr = ((ReturnStatementSyntax)statement).Expression;
                    return CheckExpressionSyntactically(expr) ? expr : null;
                }
            }

            return null;
        }

        protected override ExpressionSyntax GetSetterExpression(IMethodSymbol setMethod, SemanticModel semanticModel, CancellationToken cancellationToken)
        {
            // Setter has to be of the form:
            //
            //     set { field = value; } or
            //     set { this.field = value; }
            var setAccessor = setMethod.DeclaringSyntaxReferences[0].GetSyntax(cancellationToken) as AccessorDeclarationSyntax;
            var statements = setAccessor?.Body?.Statements;
            if (statements?.Count == 1)
            {
                var statement = statements.Value[0];
                if (statement.IsKind(SyntaxKind.ExpressionStatement))
                {
                    var expressionStatement = (ExpressionStatementSyntax)statement;
                    if (expressionStatement.Expression.Kind() == SyntaxKind.SimpleAssignmentExpression)
                    {
                        var assignmentExpression = (AssignmentExpressionSyntax)expressionStatement.Expression;
                        if (assignmentExpression.Right.Kind() == SyntaxKind.IdentifierName &&
                            ((IdentifierNameSyntax)assignmentExpression.Right).Identifier.ValueText == "value")
                        {
                            return CheckExpressionSyntactically(assignmentExpression.Left) ? assignmentExpression.Left : null;
                        }
                    }
                }
            }

            return null;
        }

        protected override SyntaxNode GetNodeToFade(FieldDeclarationSyntax fieldDeclaration, VariableDeclaratorSyntax variableDeclarator)
        {
            return fieldDeclaration.Declaration.Variables.Count == 1
                ? fieldDeclaration
                : (SyntaxNode)variableDeclarator;
        }
    }
}
