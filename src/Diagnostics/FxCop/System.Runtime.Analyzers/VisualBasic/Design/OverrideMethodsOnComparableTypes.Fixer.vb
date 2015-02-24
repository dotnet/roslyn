' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Composition
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.CodeFixes
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace System.Runtime.Analyzers
    <ExportCodeFixProvider(LanguageNames.VisualBasic), [Shared]>
    Public Class BasicOverrideMethodsOnComparableTypesFixer
        Inherits OverrideMethodsOnComparableTypesFixer

        Protected Overrides Function GenerateOperatorDeclaration(returnType As SyntaxNode, operatorName As String, parameters As IEnumerable(Of SyntaxNode), notImplementedStatement As SyntaxNode) As SyntaxNode
            Debug.Assert(TypeOf returnType Is TypeSyntax)

            Dim operatorToken As SyntaxToken
            Select Case operatorName
                Case WellKnownMemberNames.EqualityOperatorName
                    operatorToken = SyntaxFactory.Token(SyntaxKind.EqualsToken)
                Case WellKnownMemberNames.InequalityOperatorName
                    operatorToken = SyntaxFactory.Token(SyntaxKind.LessThanGreaterThanToken)
                Case WellKnownMemberNames.LessThanOperatorName
                    operatorToken = SyntaxFactory.Token(SyntaxKind.LessThanToken)
                Case WellKnownMemberNames.GreaterThanOperatorName
                    operatorToken = SyntaxFactory.Token(SyntaxKind.GreaterThanToken)
                Case Else
                    Return Nothing
            End Select

            Dim operatorStatement = SyntaxFactory.OperatorStatement(Nothing,
                                                                    SyntaxFactory.TokenList(New SyntaxToken() {SyntaxFactory.Token(SyntaxKind.PublicKeyword), SyntaxFactory.Token(SyntaxKind.SharedKeyword)}),
                                                                    SyntaxFactory.Token(SyntaxKind.OperatorKeyword),
                                                                    operatorToken,
                                                                    SyntaxFactory.ParameterList(SyntaxFactory.SeparatedList(parameters.Cast(Of ParameterSyntax)())),
                                                                    SyntaxFactory.SimpleAsClause(DirectCast(returnType, TypeSyntax)))

            Return SyntaxFactory.OperatorBlock(operatorStatement,
                                               SyntaxFactory.SingletonList(DirectCast(notImplementedStatement, StatementSyntax)))
        End Function
    End Class
End Namespace
