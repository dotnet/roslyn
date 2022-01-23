' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Concurrent
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic

    Partial Friend Class BoundAddressOfOperator

        Private ReadOnly _delegateResolutionResultCache As New ConcurrentDictionary(Of TypeSymbol, Binder.DelegateResolutionResult)()

        ''' <summary>
        ''' Gets the <see>Binder.DelegateResolutionResult</see> for the given targetType. 
        ''' </summary>
        ''' <remarks>
        ''' One needs to call <see>GetConversionClassification</see> before in order to fill the cache.
        ''' </remarks>
        ''' <param name="targetType">Type of the target.</param>
        ''' <returns>The <see cref="Binder.DelegateResolutionResult">Binder.DelegateResolutionResult</see> for the conversion 
        ''' of the AddressOf operand to the target type
        ''' </returns>
        Friend Function GetDelegateResolutionResult(targetType As TypeSymbol, ByRef delegateResolutionResult As Binder.DelegateResolutionResult) As Boolean
            Return _delegateResolutionResultCache.TryGetValue(targetType, delegateResolutionResult)
        End Function

        ''' <summary>
        ''' Gets the conversion classification.
        ''' </summary>
        ''' <param name="targetType">The destination type to convert to.</param>
        Friend Function GetConversionClassification(targetType As TypeSymbol) As ConversionKind
            Dim delegateResolutionResult As Binder.DelegateResolutionResult = Nothing

            If Not _delegateResolutionResultCache.TryGetValue(targetType, delegateResolutionResult) Then
                delegateResolutionResult = Binder.InterpretDelegateBinding(Me, targetType, isForHandles:=False)
                _delegateResolutionResultCache.TryAdd(targetType, delegateResolutionResult)
            End If

            Return delegateResolutionResult.DelegateConversions
        End Function

    End Class
End Namespace
