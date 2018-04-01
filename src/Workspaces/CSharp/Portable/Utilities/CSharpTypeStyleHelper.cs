// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Diagnostics;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis.CSharp.Utilities
{
    internal abstract partial class CSharpTypeStyleHelper
    {
        public abstract bool IsStylePreferred(
            SemanticModel semanticModel, OptionSet optionSet, CSharpTypeStyleContext state, CancellationToken cancellationToken);

        public abstract bool TryAnalyzeVariableDeclaration(TypeSyntax typeName, SemanticModel semanticModel, OptionSet optionSet, CancellationToken cancellationToken);
        protected abstract bool AssignmentSupportsStylePreference(SyntaxToken identifier, TypeSyntax typeName, ExpressionSyntax initializer, SemanticModel semanticModel, OptionSet optionSet, CancellationToken cancellationToken);

        internal TypeSyntax FindAnalyzableType(SyntaxNode node, SemanticModel semanticModel, CancellationToken cancellationToken)
        {
            Debug.Assert(node.IsKind(SyntaxKind.VariableDeclaration, SyntaxKind.ForEachStatement, SyntaxKind.DeclarationExpression));

            switch (node)
            {
                case VariableDeclarationSyntax variableDeclaration:
                    return ShouldAnalyzeVariableDeclaration(variableDeclaration, semanticModel, cancellationToken)
                        ? variableDeclaration.Type
                        : null;
                case ForEachStatementSyntax forEachStatement:
                    return ShouldAnalyzeForEachStatement(forEachStatement, semanticModel, cancellationToken)
                        ? forEachStatement.Type
                        : null;
                case DeclarationExpressionSyntax declarationExpression:
                    return ShouldAnalyzeDeclarationExpression(declarationExpression, semanticModel, cancellationToken)
                        ? declarationExpression.Type
                        : null;
            }

            return null;
        }

        protected virtual bool ShouldAnalyzeVariableDeclaration(VariableDeclarationSyntax variableDeclaration, SemanticModel semanticModel, CancellationToken cancellationToken)
        {
            // implict type is applicable only for local variables and
            // such declarations cannot have multiple declarators and
            // must have an initializer.
            var isSupportedParentKind = variableDeclaration.IsParentKind(
                SyntaxKind.LocalDeclarationStatement,
                SyntaxKind.ForStatement,
                SyntaxKind.UsingStatement);

            return isSupportedParentKind &&
                variableDeclaration.Variables.Count == 1 &&
                variableDeclaration.Variables.Single().Initializer.IsKind(SyntaxKind.EqualsValueClause);
        }

        protected virtual bool ShouldAnalyzeForEachStatement(ForEachStatementSyntax forEachStatement, SemanticModel semanticModel, CancellationToken cancellationToken)
            => true;

        protected virtual bool ShouldAnalyzeDeclarationExpression(DeclarationExpressionSyntax declaration, SemanticModel semanticModel, CancellationToken cancellationToken)
            => true;
    }
}
