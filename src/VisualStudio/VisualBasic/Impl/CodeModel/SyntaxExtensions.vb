' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Runtime.CompilerServices
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.VisualStudio.LanguageServices.VisualBasic.CodeModel
    Friend Module SyntaxExtensions

        <Extension>
        Public Function GetExpression(argument As ArgumentSyntax) As ExpressionSyntax
            Debug.Assert(TypeOf argument Is SimpleArgumentSyntax OrElse
                         TypeOf argument Is OmittedArgumentSyntax)

            Return argument.GetExpression()
        End Function

        <Extension>
        Public Function GetName(importsClause As ImportsClauseSyntax) As NameSyntax
            Debug.Assert(TypeOf importsClause Is SimpleImportsClauseSyntax)

            Return DirectCast(importsClause, SimpleImportsClauseSyntax).Name
        End Function

        <Extension>
        Public Function GetNameText(method As MethodBaseSyntax) As String
            Select Case method.Kind
                Case SyntaxKind.SubStatement,
                     SyntaxKind.FunctionStatement
                    Return DirectCast(method, MethodStatementSyntax).Identifier.ToString()
                Case SyntaxKind.SubNewStatement
                    Return DirectCast(method, SubNewStatementSyntax).NewKeyword.ToString()
                Case SyntaxKind.OperatorStatement
                    Return DirectCast(method, OperatorStatementSyntax).OperatorToken.ToString()
                Case SyntaxKind.DeclareSubStatement,
                     SyntaxKind.DeclareFunctionStatement
                    Return DirectCast(method, DeclareStatementSyntax).Identifier.ToString()
                Case SyntaxKind.DelegateSubStatement,
                     SyntaxKind.DelegateFunctionStatement
                    Return DirectCast(method, DelegateStatementSyntax).Identifier.ToString()
                Case SyntaxKind.EventStatement
                    Return DirectCast(method, EventStatementSyntax).Identifier.ToString()
                Case SyntaxKind.PropertyStatement
                    Return DirectCast(method, PropertyStatementSyntax).Identifier.ToString()
                Case Else
                    Debug.Fail(String.Format("Unexpected node kind: {0}", method.Kind))
                    Return String.Empty
            End Select
        End Function

        <Extension>
        Public Function Type(method As MethodBaseSyntax) As TypeSyntax
            Dim asClause As AsClauseSyntax

            Select Case method.Kind
                Case SyntaxKind.SubStatement, SyntaxKind.FunctionStatement
                    asClause = DirectCast(method, MethodStatementSyntax).AsClause

                Case SyntaxKind.SubLambdaHeader, SyntaxKind.FunctionLambdaHeader
                    asClause = DirectCast(method, LambdaHeaderSyntax).AsClause

                Case SyntaxKind.DeclareSubStatement, SyntaxKind.DeclareFunctionStatement
                    asClause = DirectCast(method, DeclareStatementSyntax).AsClause

                Case SyntaxKind.DelegateSubStatement, SyntaxKind.DelegateFunctionStatement
                    asClause = DirectCast(method, DelegateStatementSyntax).AsClause

                Case SyntaxKind.EventStatement
                    asClause = DirectCast(method, EventStatementSyntax).AsClause

                Case SyntaxKind.OperatorStatement
                    asClause = DirectCast(method, OperatorStatementSyntax).AsClause

                Case SyntaxKind.PropertyStatement
                    asClause = DirectCast(method, PropertyStatementSyntax).AsClause

                Case Else
                    Return Nothing
            End Select

            Return If(asClause IsNot Nothing,
                      asClause.Type(),
                      Nothing)
        End Function

        <Extension>
        Public Function Type(parameter As ParameterSyntax) As TypeSyntax
            Return If(parameter.AsClause IsNot Nothing,
                      parameter.AsClause.Type,
                      Nothing)
        End Function

        <Extension>
        Public Function Type(variableDeclarator As VariableDeclaratorSyntax) As TypeSyntax
            Return If(variableDeclarator.AsClause IsNot Nothing,
                      variableDeclarator.AsClause.Type(),
                      Nothing)
        End Function

    End Module
End Namespace
