// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Microsoft.CodeAnalysis.CSharp
{
    public partial class SyntaxFactory
    {
        public static LocalFunctionStatementSyntax LocalFunctionStatement(
            SyntaxTokenList modifiers,
            TypeSyntax returnType,
            SyntaxToken identifier,
            TypeParameterListSyntax typeParameterList,
            ParameterListSyntax parameterList,
            SyntaxList<TypeParameterConstraintClauseSyntax> constraintClauses,
            BlockSyntax body,
            ArrowExpressionClauseSyntax expressionBody)
        {
            return LocalFunctionStatement(attributeLists: default, modifiers, returnType, identifier, typeParameterList, parameterList, constraintClauses, body, expressionBody, semicolonToken: default);
        }

        public static LocalFunctionStatementSyntax LocalFunctionStatement(SyntaxTokenList modifiers, TypeSyntax returnType, SyntaxToken identifier, TypeParameterListSyntax typeParameterList, ParameterListSyntax parameterList, SyntaxList<TypeParameterConstraintClauseSyntax> constraintClauses, BlockSyntax body, ArrowExpressionClauseSyntax expressionBody, SyntaxToken semicolonToken)
        {
            return LocalFunctionStatement(default, modifiers, returnType, identifier, typeParameterList, parameterList, constraintClauses, body, expressionBody, semicolonToken);
        }
    }
}

namespace Microsoft.CodeAnalysis.CSharp.Syntax
{
    public partial class LocalFunctionStatementSyntax
    {
        public LocalFunctionStatementSyntax Update(SyntaxTokenList modifiers, TypeSyntax returnType, SyntaxToken identifier, TypeParameterListSyntax typeParameterList, ParameterListSyntax parameterList, SyntaxList<TypeParameterConstraintClauseSyntax> constraintClauses, BlockSyntax body, ArrowExpressionClauseSyntax expressionBody, SyntaxToken semicolonToken)
        {
            if (modifiers != this.Modifiers || returnType != this.ReturnType || identifier != this.Identifier || typeParameterList != this.TypeParameterList || parameterList != this.ParameterList || constraintClauses != this.ConstraintClauses || body != this.Body || expressionBody != this.ExpressionBody || semicolonToken != this.SemicolonToken)
            {
                var newNode = SyntaxFactory.LocalFunctionStatement(this.AttributeLists, modifiers, returnType, identifier, typeParameterList, parameterList, constraintClauses, body, expressionBody, semicolonToken);
                var annotations = this.GetAnnotations();
                if (annotations != null && annotations.Length > 0)
                    return newNode.WithAnnotations(annotations);
                return newNode;
            }

            return this;
        }
    }
}
