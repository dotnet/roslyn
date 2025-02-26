' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

Imports System.Runtime.CompilerServices
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Global.Analyzer.Utilities.Extensions

    Public Module StatementSyntaxExtensions

        <Extension()>
        Public Function GetAsClause(member As StatementSyntax) As AsClauseSyntax
            If member IsNot Nothing Then
                Select Case member.Kind
                    Case SyntaxKind.FunctionBlock
                        Return DirectCast(member, MethodBlockSyntax).SubOrFunctionStatement.AsClause
                    Case SyntaxKind.OperatorBlock
                        Return DirectCast(member, OperatorBlockSyntax).OperatorStatement.AsClause
                    Case SyntaxKind.FunctionStatement
                        Return DirectCast(member, MethodStatementSyntax).AsClause
                    Case SyntaxKind.OperatorStatement
                        Return DirectCast(member, OperatorStatementSyntax).AsClause
                    Case SyntaxKind.DeclareFunctionStatement
                        Return DirectCast(member, DeclareStatementSyntax).AsClause
                    Case SyntaxKind.DelegateFunctionStatement
                        Return DirectCast(member, DelegateStatementSyntax).AsClause
                    Case SyntaxKind.PropertyBlock
                        Return DirectCast(member, PropertyBlockSyntax).PropertyStatement.AsClause
                    Case SyntaxKind.PropertyStatement
                        Return DirectCast(member, PropertyStatementSyntax).AsClause
                    Case SyntaxKind.EventBlock
                        Return DirectCast(member, EventBlockSyntax).EventStatement.AsClause
                    Case SyntaxKind.EventStatement
                        Return DirectCast(member, EventStatementSyntax).AsClause
                End Select
            End If

            Return Nothing
        End Function

        <Extension()>
        Public Function GetReturnType(member As StatementSyntax) As TypeSyntax
            Dim asClause = member.GetAsClause()
            Return asClause?.Type
        End Function

        <Extension()>
        Public Function HasReturnType(member As StatementSyntax) As Boolean
            Return member.GetReturnType() IsNot Nothing
        End Function

    End Module

End Namespace
