' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Runtime.CompilerServices

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
