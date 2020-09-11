' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports System.Runtime.InteropServices
Imports System.Threading


Namespace Microsoft.CodeAnalysis.VisualBasic

    Partial Friend Class BoundMethodGroup

        Public Sub New(
            syntax As SyntaxNode,
            typeArgumentsOpt As BoundTypeArguments,
            methods As ImmutableArray(Of MethodSymbol),
            resultKind As LookupResultKind,
            receiverOpt As BoundExpression,
            qualificationKind As QualificationKind,
            Optional hasErrors As Boolean = False
        )
            Me.New(syntax, typeArgumentsOpt, methods, Nothing, resultKind, receiverOpt, qualificationKind, hasErrors)
        End Sub

        ' Lazily filled once the value is requested.
        Public Function AdditionalExtensionMethods(<[In], Out> ByRef useSiteDiagnostics As HashSet(Of DiagnosticInfo)) As ImmutableArray(Of MethodSymbol)
            If _PendingExtensionMethodsOpt Is Nothing Then
                Return ImmutableArray(Of MethodSymbol).Empty
            End If

            Return _PendingExtensionMethodsOpt.LazyLookupAdditionalExtensionMethods(Me, useSiteDiagnostics)
        End Function

    End Class

    Friend Class ExtensionMethodGroup
        Private ReadOnly _lookupBinder As Binder
        Private ReadOnly _lookupOptions As LookupOptions
        Private _lazyMethods As ImmutableArray(Of MethodSymbol)
        Private _lazyUseSiteDiagnostics As HashSet(Of DiagnosticInfo)

        Public Sub New(lookupBinder As Binder, lookupOptions As LookupOptions)
            Debug.Assert(lookupBinder IsNot Nothing)
            _lookupBinder = lookupBinder
            _lookupOptions = lookupOptions
        End Sub

        Public Function LazyLookupAdditionalExtensionMethods(group As BoundMethodGroup, <[In], Out> ByRef useSiteDiagnostics As HashSet(Of DiagnosticInfo)) As ImmutableArray(Of MethodSymbol)
            Debug.Assert(group.PendingExtensionMethodsOpt Is Me)

            If _lazyMethods.IsDefault Then
                Dim receiverOpt As BoundExpression = group.ReceiverOpt
                Dim methods As ImmutableArray(Of MethodSymbol) = ImmutableArray(Of MethodSymbol).Empty
                Dim localUseSiteDiagnostics As HashSet(Of DiagnosticInfo) = Nothing

                If receiverOpt IsNot Nothing AndAlso receiverOpt.Type IsNot Nothing Then
                    Dim lookup = LookupResult.GetInstance()

                    _lookupBinder.LookupExtensionMethods(lookup,
                                                    receiverOpt.Type,
                                                    group.Methods(0).Name,
                                                    If(group.TypeArgumentsOpt Is Nothing, 0, group.TypeArgumentsOpt.Arguments.Length),
                                                    _lookupOptions,
                                                    localUseSiteDiagnostics)

                    If lookup.IsGood Then
                        methods = lookup.Symbols.ToDowncastedImmutable(Of MethodSymbol)()
                    End If

                    lookup.Free()
                End If

                Interlocked.CompareExchange(_lazyUseSiteDiagnostics, localUseSiteDiagnostics, Nothing)
                ImmutableInterlocked.InterlockedCompareExchange(_lazyMethods, methods, Nothing)
            End If

            If Not _lazyUseSiteDiagnostics.IsNullOrEmpty Then
                If useSiteDiagnostics Is Nothing Then
                    useSiteDiagnostics = New HashSet(Of DiagnosticInfo)()
                End If

                For Each info In _lazyUseSiteDiagnostics
                    useSiteDiagnostics.Add(info)
                Next
            End If

            Return _lazyMethods
        End Function

    End Class

End Namespace
