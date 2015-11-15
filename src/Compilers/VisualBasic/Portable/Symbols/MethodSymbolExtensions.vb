' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System
Imports System.Collections.Immutable
Imports System.Runtime.CompilerServices
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.Symbols

    Friend Module MethodSymbolExtensions

        ''' <summary>
        ''' Determines if the method can be called with empty parameter list.
        ''' </summary>
        ''' <param name="method">The method.</param><returns></returns>
        <Extension()>
        Friend Function CanBeCalledWithNoParameters(method As MethodSymbol) As Boolean
            Dim parameterCount = method.ParameterCount
            If parameterCount = 0 Then
                Return True
            End If

            Dim parameters = method.Parameters
            For parameterIndex = 0 To parameterCount - 1
                Dim param As ParameterSymbol = parameters(parameterIndex)
                If param.IsParamArray AndAlso parameterIndex = parameterCount - 1 Then
                    ' ParamArray may be ignored only if the type is an array of rank = 1
                    Dim type = param.Type
                    If Not type.IsArrayType OrElse Not DirectCast(type, ArrayTypeSymbol).IsSZArray Then
                        Return False
                    End If

                ElseIf Not param.IsOptional Then
                    ' We got non-optional parameter 
                    Return False
                End If
            Next

            Return True
        End Function

        <Extension()>
        Friend Function GetParameterSymbol(parameters As ImmutableArray(Of ParameterSymbol), parameter As ParameterSyntax) As ParameterSymbol
            Dim syntaxTree = parameter.SyntaxTree
            For Each symbol In parameters
                For Each location In symbol.Locations
                    If location.IsInSource AndAlso location.SourceTree Is syntaxTree AndAlso parameter.Span.Contains(location.SourceSpan) Then
                        Return symbol
                    End If
                Next
            Next

            Return Nothing
        End Function

        ''' <summary> 
        ''' Determines if the method is partial 
        ''' </summary>
        ''' <param name="method">The method</param>
        <Extension()>
        Friend Function IsPartial(method As MethodSymbol) As Boolean
            Dim sourceMethod = TryCast(method, SourceMemberMethodSymbol)
            Return sourceMethod IsNot Nothing AndAlso sourceMethod.IsPartial
        End Function

        ''' <summary> 
        ''' Determines if the method is partial and does NOT have implementation provided 
        ''' </summary>
        ''' <param name="method">The method</param>
        <Extension()>
        Friend Function IsPartialWithoutImplementation(method As MethodSymbol) As Boolean
            Dim sourceMethod = TryCast(method, SourceMemberMethodSymbol)
            Return sourceMethod IsNot Nothing AndAlso sourceMethod.IsPartial AndAlso sourceMethod.OtherPartOfPartial Is Nothing
        End Function

        ''' <summary>
        ''' Is method a user-defined operator.
        ''' </summary>
        <Extension()>
        Friend Function IsUserDefinedOperator(method As MethodSymbol) As Boolean
            Select Case method.MethodKind
                Case MethodKind.UserDefinedOperator, MethodKind.Conversion
                    Return True
            End Select

            Return False
        End Function

    End Module
End Namespace
