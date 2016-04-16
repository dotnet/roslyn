' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Runtime.CompilerServices
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.Symbols

    Friend Module PropertySymbolExtensions

        ''' <summary>
        ''' Determines if the property can be accessed with empty parameter list.
        ''' </summary>
        ''' <param name="prop">The property.</param><returns></returns>
        <Extension()>
        Friend Function GetCanBeCalledWithNoParameters(prop As PropertySymbol) As Boolean
            Dim parameterCount = prop.ParameterCount
            If parameterCount = 0 Then
                Return True
            End If

            Dim parameters = prop.Parameters
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
        Public Function GetTypeFromGetMethod([property] As PropertySymbol) As TypeSymbol
            Dim accessor = [property].GetMethod
            Return If(accessor Is Nothing, [property].Type, accessor.ReturnType)
        End Function

        <Extension()>
        Public Function GetTypeFromSetMethod([property] As PropertySymbol) As TypeSymbol
            Dim accessor = [property].SetMethod
            If accessor Is Nothing Then
                Return [property].Type
            End If
            Dim parameters = accessor.Parameters
            Return parameters(parameters.Length - 1).Type
        End Function

    End Module

End Namespace
