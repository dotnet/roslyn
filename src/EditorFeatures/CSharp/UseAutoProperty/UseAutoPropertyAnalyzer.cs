// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Concurrent;
using System.ComponentModel.Composition;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.UseAutoProperty;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UseAutoProperty
{
    // https://github.com/dotnet/roslyn/issues/5408
    //[Export]
    //[DiagnosticAnalyzer(LanguageNames.CSharp)]
    internal class UseAutoPropertyAnalyzer : AbstractUseAutoPropertyAnalyzer<PropertyDeclarationSyntax, FieldDeclarationSyntax, VariableDeclaratorSyntax, ExpressionSyntax>
    {
        protected override bool SupportsReadOnlyProperties(Compilation compilation)
        {
            return ((CSharpCompilation)compilation).LanguageVersion >= LanguageVersion.CSharp6;
        }

        protected override bool SupportsPropertyInitializer(Compilation compilation)
        {
            return ((CSharpCompilation)compilation).LanguageVersion >= LanguageVersion.CSharp6;
        }

        protected override void RegisterIneligibleFieldsAction(CompilationStartAnalysisContext context, ConcurrentBag<IFieldSymbol> ineligibleFields)
        {
            context.RegisterSyntaxNodeAction(snac => AnalyzeArgument(ineligibleFields, snac), SyntaxKind.Argument);
        }

        protected override ExpressionSyntax GetFieldInitializer(VariableDeclaratorSyntax variable, CancellationToken cancellationToken)
        {
            return variable.Initializer?.Value;
        }

        private void AnalyzeArgument(ConcurrentBag<IFieldSymbol> ineligibleFields, SyntaxNodeAnalysisContext context)
        {
            // An argument will disqualify a field if that field is used in a ref/out position.  
            // We can't change such field references to be property references in C#.
            var argument = (ArgumentSyntax)context.Node;
            if (argument.RefOrOutKeyword.Kind() == SyntaxKind.None)
            {
                return;
            }

            var cancellationToken = context.CancellationToken;
            var symbolInfo = context.SemanticModel.GetSymbolInfo(argument.Expression, cancellationToken);
            AddIneligibleField(symbolInfo.Symbol, ineligibleFields);
            foreach (var symbol in symbolInfo.CandidateSymbols)
            {
                AddIneligibleField(symbol, ineligibleFields);
            }
        }

        private static void AddIneligibleField(ISymbol symbol, ConcurrentBag<IFieldSymbol> ineligibleFields)
        {
            var field = symbol as IFieldSymbol;
            if (field != null)
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