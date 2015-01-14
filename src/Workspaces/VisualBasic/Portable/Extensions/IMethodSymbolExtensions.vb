' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Runtime.CompilerServices
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.Extensions
    Friend Module IMethodSymbolExtensions
        ''' <summary>
        ''' Determines whether the given IMethodSymbol can be used as an aggregate function
        ''' in a Group..By..Into or an Aggregate..Into clause.
        ''' </summary>
        <Extension()>
        Public Function IsAggregateFunction(symbol As IMethodSymbol) As Boolean
            If symbol.ReturnsVoid Then
                Return False
            End If

            If symbol.IsStatic AndAlso Not symbol.MethodKind = MethodKind.ReducedExtension Then
                Return False
            End If

            ' Function <name>() As <type>
            If symbol.Parameters.Length = 0 Then
                Return True
            End If

            ' Function <name>(selector as Func(Of T, R)) As R
            If symbol.Parameters.Length = 1 Then
                Dim parameter = symbol.Parameters(0)

                If parameter.Type.TypeKind = TypeKind.Delegate Then
                    Dim delegateInvokeMethod = DirectCast(parameter.Type, INamedTypeSymbol).DelegateInvokeMethod

                    If delegateInvokeMethod IsNot Nothing AndAlso
                       delegateInvokeMethod.Parameters.Length = 1 AndAlso
                       Not delegateInvokeMethod.ReturnsVoid Then

                        Return True
                    End If
                End If
            End If

            Return False
        End Function
    End Module
End Namespace
