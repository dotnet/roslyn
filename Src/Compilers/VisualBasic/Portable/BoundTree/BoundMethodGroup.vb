' Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports System.Runtime.InteropServices
Imports System.Threading


Namespace Microsoft.CodeAnalysis.VisualBasic

    Partial Class BoundMethodGroup

        Public Sub New(
            syntax As VBSyntaxNode,
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
        Private ReadOnly m_LookupBinder As Binder
        Private ReadOnly m_LookupOptions As LookupOptions
        Private m_lazyMethods As ImmutableArray(Of MethodSymbol)
        Private m_lasyUseSiteDiagnostics As HashSet(Of DiagnosticInfo)

        Public Sub New(lookupBinder As Binder, lookupOptions As LookupOptions)
            Debug.Assert(lookupBinder IsNot Nothing)
            m_LookupBinder = lookupBinder
            m_LookupOptions = lookupOptions
        End Sub

        Public Function LazyLookupAdditionalExtensionMethods(group As BoundMethodGroup, <[In], Out> ByRef useSiteDiagnostics As HashSet(Of DiagnosticInfo)) As ImmutableArray(Of MethodSymbol)
            Debug.Assert(group.PendingExtensionMethodsOpt Is Me)

            If m_lazyMethods.IsDefault Then
                Dim receiverOpt As BoundExpression = group.ReceiverOpt
                Dim methods As ImmutableArray(Of MethodSymbol) = ImmutableArray(Of MethodSymbol).Empty
                Dim localUseSiteDiagnostics As HashSet(Of DiagnosticInfo) = Nothing

                If receiverOpt IsNot Nothing AndAlso receiverOpt.Type IsNot Nothing Then
                    Dim lookup = LookupResult.GetInstance()

                    m_LookupBinder.LookupExtensionMethods(lookup,
                                                    receiverOpt.Type,
                                                    group.Methods(0).Name,
                                                    If(group.TypeArgumentsOpt Is Nothing, 0, group.TypeArgumentsOpt.Arguments.Length),
                                                    m_LookupOptions,
                                                    localUseSiteDiagnostics)

                    If lookup.IsGood Then
                        methods = lookup.Symbols.ToDowncastedImmutable(Of MethodSymbol)()
                    End If

                    lookup.Free()
                End If

                Interlocked.CompareExchange(m_lasyUseSiteDiagnostics, localUseSiteDiagnostics, Nothing)
                ImmutableInterlocked.InterlockedCompareExchange(m_lazyMethods, methods, Nothing)
            End If

            If Not m_lasyUseSiteDiagnostics.IsNullOrEmpty Then
                If useSiteDiagnostics Is Nothing Then
                    useSiteDiagnostics = New HashSet(Of DiagnosticInfo)()
                End If

                For Each info In m_lasyUseSiteDiagnostics
                    useSiteDiagnostics.Add(info)
                Next
            End If

            Return m_lazyMethods
        End Function

    End Class

End Namespace
