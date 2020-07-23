// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Formatting;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.UseAutoProperty;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.UseAutoProperty
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    internal class CSharpUseAutoPropertyAnalyzer : AbstractUseAutoPropertyAnalyzer<
        PropertyDeclarationSyntax, FieldDeclarationSyntax, VariableDeclaratorSyntax, ExpressionSyntax>
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
            if (member.IsKind(SyntaxKind.NamespaceDeclaration, out NamespaceDeclarationSyntax? namespaceDeclaration))
            {
                AnalyzeMembers(context, namespaceDeclaration.Members, analysisResults);
            }
            else if (member.IsKind(SyntaxKind.ClassDeclaration, out TypeDeclarationSyntax? typeDeclaration) ||
                member.IsKind(SyntaxKind.StructDeclaration, out typeDeclaration) ||
                member.IsKind(SyntaxKindEx.RecordDeclaration, out typeDeclaration))
            {
                // If we have a class or struct, recurse inwards.
                AnalyzeMembers(context, typeDeclaration.Members, analysisResults);
            }
            else if (member.IsKind(SyntaxKind.PropertyDeclaration, out PropertyDeclarationSyntax? propertyDeclaration))
            {
                AnalyzeProperty(context, propertyDeclaration, analysisResults);
            }
        }

        protected override void RegisterIneligibleFieldsAction(
            List<AnalysisResult> analysisResults, HashSet<IFieldSymbol> ineligibleFields,
            Compilation compilation, CancellationToken cancellationToken)
        {
            var groups = analysisResults.Select(r => (typeDeclaration: (TypeDeclarationSyntax)r.PropertyDeclaration.Parent!, r.SemanticModel))
                                        .Distinct()
                                        .GroupBy(n => n.typeDeclaration.SyntaxTree);

            foreach (var (tree, typeDeclarations) in groups)
            {
                foreach (var (typeDeclaration, semanticModel) in typeDeclarations)
                {
                    foreach (var argument in typeDeclaration.DescendantNodesAndSelf().OfType<ArgumentSyntax>())
                    {
                        // An argument will disqualify a field if that field is used in a ref/out position.  
                        // We can't change such field references to be property references in C#.
                        if (argument.RefKindKeyword.Kind() != SyntaxKind.None)
                        {
                            AddIneligibleFields(semanticModel, argument.Expression, ineligibleFields, cancellationToken);
                        }
                    }

                    foreach (var refExpression in typeDeclaration.DescendantNodesAndSelf().OfType<RefExpressionSyntax>())
                    {
                        AddIneligibleFields(semanticModel, refExpression.Expression, ineligibleFields, cancellationToken);
                    }
                }
            }
        }

        protected override ExpressionSyntax? GetFieldInitializer(
            VariableDeclaratorSyntax variable, CancellationToken cancellationToken)
        {
            return variable.Initializer?.Value;
        }

        private static void AddIneligibleFields(
            SemanticModel semanticModel, ExpressionSyntax expression,
            HashSet<IFieldSymbol> ineligibleFields, CancellationToken cancellationToken)
        {
            var symbolInfo = semanticModel.GetSymbolInfo(expression, cancellationToken);
            AddIneligibleField(symbolInfo.Symbol);
            foreach (var symbol in symbolInfo.CandidateSymbols)
            {
                AddIneligibleField(symbol);
            }

            void AddIneligibleField(ISymbol? symbol)
            {
                if (symbol is IFieldSymbol field)
                {
                    ineligibleFields.Add(field);
                }
            }
        }

        private static bool CheckExpressionSyntactically(ExpressionSyntax expression)
        {
            if (expression.IsKind(SyntaxKind.SimpleMemberAccessExpression, out MemberAccessExpressionSyntax? memberAccessExpression))
            {
                return memberAccessExpression.Expression.Kind() == SyntaxKind.ThisExpression &&
                    memberAccessExpression.Name.Kind() == SyntaxKind.IdentifierName;
            }
            else if (expression.IsKind(SyntaxKind.IdentifierName))
            {
                return true;
            }

            return false;
        }

        protected override ExpressionSyntax? GetGetterExpression(IMethodSymbol getMethod, CancellationToken cancellationToken)
        {
            // Getter has to be of the form:
            // 1. Getter can be defined as accessor or expression bodied lambda
            //     get { return field; }
            //     get => field;
            //     int Property => field;
            // 2. Underlying field can be accessed with this qualifier or not
            //     get { return field; }
            //     get { return this.field; }
            var expr = GetGetterExpressionFromSymbol(getMethod, cancellationToken);
            if (expr == null)
            {
                return null;
            }

            return CheckExpressionSyntactically(expr) ? expr : null;
        }

        private static ExpressionSyntax? GetGetterExpressionFromSymbol(IMethodSymbol getMethod, CancellationToken cancellationToken)
        {
            var declaration = getMethod.DeclaringSyntaxReferences[0].GetSyntax(cancellationToken);
            switch (declaration)
            {
                case AccessorDeclarationSyntax accessorDeclaration:
                    return accessorDeclaration.ExpressionBody?.Expression ??
                           GetSingleStatementFromAccessor<ReturnStatementSyntax>(accessorDeclaration)?.Expression;
                case ArrowExpressionClauseSyntax arrowExpression:
                    return arrowExpression.Expression;
                case null: return null;
                default: throw ExceptionUtilities.Unreachable;
            }
        }

        private static T? GetSingleStatementFromAccessor<T>(AccessorDeclarationSyntax? accessorDeclaration) where T : StatementSyntax
        {
            var statements = accessorDeclaration?.Body?.Statements;
            if (statements?.Count == 1)
            {
                var statement = statements.Value[0];
                return statement as T;
            }

            return null;
        }

        protected override ExpressionSyntax? GetSetterExpression(
            IMethodSymbol setMethod, SemanticModel semanticModel, CancellationToken cancellationToken)
        {
            // Setter has to be of the form:
            //
            //     set { field = value; }
            //     set { this.field = value; }
            //     set => field = value; 
            //     set => this.field = value; 
            var setAccessor = setMethod.DeclaringSyntaxReferences[0].GetSyntax(cancellationToken) as AccessorDeclarationSyntax;
            var setExpression = GetExpressionFromSetter(setAccessor);
            if (setExpression?.Kind() == SyntaxKind.SimpleAssignmentExpression)
            {
                var assignmentExpression = (AssignmentExpressionSyntax)setExpression;
                if (assignmentExpression.Right.Kind() == SyntaxKind.IdentifierName &&
                    ((IdentifierNameSyntax)assignmentExpression.Right).Identifier.ValueText == "value")
                {
                    return CheckExpressionSyntactically(assignmentExpression.Left) ? assignmentExpression.Left : null;
                }
            }

            return null;
        }

        private static ExpressionSyntax? GetExpressionFromSetter(AccessorDeclarationSyntax? setAccessor)
            => setAccessor?.ExpressionBody?.Expression ??
               GetSingleStatementFromAccessor<ExpressionStatementSyntax>(setAccessor)?.Expression;

        protected override SyntaxNode GetNodeToFade(
            FieldDeclarationSyntax fieldDeclaration, VariableDeclaratorSyntax variableDeclarator)
        {
            return fieldDeclaration.Declaration.Variables.Count == 1
                ? fieldDeclaration
                : (SyntaxNode)variableDeclarator;
        }
    }
}
