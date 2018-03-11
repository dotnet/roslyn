' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Runtime.CompilerServices
Imports System.Threading
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.Extensions

    Friend Module MethodBaseSyntaxExtensions
        <Extension>
        Public Function WithParameterList(method As MethodBaseSyntax, parameterList As ParameterListSyntax) As MethodBaseSyntax
            Select Case method.Kind
                Case SyntaxKind.SubNewStatement
                    Return DirectCast(method, SubNewStatementSyntax).WithParameterList(parameterList)
                Case SyntaxKind.OperatorStatement
                    Return DirectCast(method, OperatorStatementSyntax).WithParameterList(parameterList)
                Case SyntaxKind.PropertyStatement
                    Return DirectCast(method, PropertyStatementSyntax).WithParameterList(parameterList)
            End Select

            If TypeOf method Is MethodStatementSyntax Then
                Return DirectCast(method, MethodStatementSyntax).WithParameterList(parameterList)
            ElseIf TypeOf method Is AccessorStatementSyntax Then
                Return DirectCast(method, AccessorStatementSyntax).WithParameterList(parameterList)
            ElseIf TypeOf method Is DeclareStatementSyntax Then
                Return DirectCast(method, DeclareStatementSyntax).WithParameterList(parameterList)
            ElseIf TypeOf method Is DelegateStatementSyntax Then
                Return DirectCast(method, DelegateStatementSyntax).WithParameterList(parameterList)
            ElseIf TypeOf method Is EventStatementSyntax Then
                Return DirectCast(method, EventStatementSyntax).WithParameterList(parameterList)
            ElseIf TypeOf method Is LambdaHeaderSyntax Then
                Return DirectCast(method, LambdaHeaderSyntax).WithParameterList(parameterList)
            End If

            Throw ExceptionUtilities.Unreachable
        End Function
    End Module
End Namespace
