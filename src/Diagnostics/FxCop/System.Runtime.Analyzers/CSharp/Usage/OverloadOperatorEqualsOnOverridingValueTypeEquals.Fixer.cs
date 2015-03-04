// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Composition;
using System.Diagnostics;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace System.Runtime.Analyzers
{
    /// <summary>
    /// CA2231: Overload operator equals on overriding ValueType.Equals
    /// </summary>
    [ExportCodeFixProvider(LanguageNames.CSharp), Shared]
    public class CSharpOverloadOperatorEqualsOnOverridingValueTypeEqualsFixer : OverloadOperatorEqualsOnOverridingValueTypeEqualsFixer
    {
        protected override SyntaxNode GenerateOperatorDeclaration(SyntaxNode returnType, string operatorName, IEnumerable<SyntaxNode> parameters, SyntaxNode notImplementedStatement)
        {
            Debug.Assert(returnType is TypeSyntax);

            SyntaxToken operatorToken;
            switch (operatorName)
            {
                case WellKnownMemberNames.EqualityOperatorName:
                    operatorToken = SyntaxFactory.Token(SyntaxKind.EqualsEqualsToken);
                    break;
                case WellKnownMemberNames.InequalityOperatorName:
                    operatorToken = SyntaxFactory.Token(SyntaxKind.ExclamationEqualsToken);
                    break;
                case WellKnownMemberNames.LessThanOperatorName:
                    operatorToken = SyntaxFactory.Token(SyntaxKind.LessThanToken);
                    break;
                case WellKnownMemberNames.GreaterThanOperatorName:
                    operatorToken = SyntaxFactory.Token(SyntaxKind.GreaterThanToken);
                    break;
                default:
                    return null;
            }

            return SyntaxFactory.OperatorDeclaration(
                default(SyntaxList<AttributeListSyntax>),
                SyntaxFactory.TokenList(new[] { SyntaxFactory.Token(SyntaxKind.PublicKeyword), SyntaxFactory.Token(SyntaxKind.StaticKeyword) }),
                (TypeSyntax)returnType,
                SyntaxFactory.Token(SyntaxKind.OperatorKeyword),
                operatorToken,
                SyntaxFactory.ParameterList(SyntaxFactory.SeparatedList(parameters.Cast<ParameterSyntax>())),
                SyntaxFactory.Block((StatementSyntax)notImplementedStatement),
                default(SyntaxToken));
        }
    }
}
