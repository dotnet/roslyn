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
    [Export]
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    internal class CSharpUseAutoPropertyAnalyzer : AbstractUseAutoPropertyAnalyzer<PropertyDeclarationSyntax, FieldDeclarationSyntax, VariableDeclaratorSyntax, ExpressionSyntax>
    {
        protected override bool SupportsReadOnlyProperties(Compilation compilation)
        {
            return ((CSharpCompilation)compilation).LanguageVersion >= LanguageVersion.CSharp6;
        }

        protected override bool SupportsPropertyInitializer(Compilation compilation)
        {
            return ((CSharpCompilation)compilation).LanguageVersion >= LanguageVersion.CSharp6;
        }

        protected override ExpressionSyntax GetFieldInitializer(VariableDeclaratorSyntax variable, CancellationToken cancellationToken)
        {
            return variable.Initializer?.Value;
        }

        private string GetFieldName(ExpressionSyntax expression)
        {
            if (expression.IsKind(SyntaxKind.SimpleMemberAccessExpression))
            {
                var memberAccessExpression = (MemberAccessExpressionSyntax)expression;
                if (memberAccessExpression.Expression.Kind() == SyntaxKind.ThisExpression && 
                    memberAccessExpression.Name.Kind() == SyntaxKind.IdentifierName)
                {
                    return ((IdentifierNameSyntax)memberAccessExpression.Name).Identifier.ValueText;
                }
            }
            else if (expression.IsKind(SyntaxKind.IdentifierName))
            {
                return ((IdentifierNameSyntax)expression).Identifier.ValueText;
            }

            return null;
        }

        protected override string GetGetterFieldName(IMethodSymbol getMethod, CancellationToken cancellationToken)
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
                    return GetFieldName(expr);
                }
            }

            return null;
        }

        protected override string GetSetterFieldName(IMethodSymbol setMethod, CancellationToken cancellationToken)
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
                            return GetFieldName(assignmentExpression.Left);
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