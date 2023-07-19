' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Generic
Imports System.Collections.Immutable
Imports System.Diagnostics
Imports System.Linq
Imports System.Runtime.InteropServices
Imports System.Threading
Imports Microsoft.CodeAnalysis.Collections
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic

    Friend MustInherit Class BlockBaseBinder
        Inherits BlockBaseBinder(Of LocalSymbol)

        Public Sub New(enclosing As Binder)
            MyBase.New(enclosing)
        End Sub
    End Class

    Friend MustInherit Class BlockBaseBinder(Of T As Symbol)
        Inherits Binder

        Public Sub New(enclosing As Binder)
            MyBase.New(enclosing)
        End Sub

        Friend MustOverride ReadOnly Property Locals As ImmutableArray(Of T)
        Private _lazyLocalsMap As Dictionary(Of String, T)

        Private ReadOnly Property LocalsMap As Dictionary(Of String, T)
            Get
                If Me._lazyLocalsMap Is Nothing AndAlso Not Me.Locals.IsEmpty Then
                    Interlocked.CompareExchange(Me._lazyLocalsMap, BuildMap(Me.Locals), Nothing)
                End If
                Return Me._lazyLocalsMap
            End Get
        End Property

        Private Function BuildMap(locals As ImmutableArray(Of T)) As Dictionary(Of String, T)
            Debug.Assert(Not locals.IsEmpty)

            Dim map = New Dictionary(Of String, T)(locals.Length, IdentifierComparison.Comparer)
            For Each local In locals
                If Not map.ContainsKey(local.Name) Then
                    map(local.Name) = local
                End If
            Next
            Return map
        End Function

        Friend Overrides Sub LookupInSingleBinder(lookupResult As LookupResult,
                                                      name As String,
                                                      arity As Integer,
                                                      options As LookupOptions,
                                                      originalBinder As Binder,
                                                      <[In], Out> ByRef useSiteInfo As CompoundUseSiteInfo(Of AssemblySymbol))
            ' locals are always arity 0, and never types and namespaces.
            Dim locals = Me.Locals
            Dim localSymbol As T = Nothing
            If Not locals.IsEmpty AndAlso (options And (LookupOptions.NamespacesOrTypesOnly Or LookupOptions.LabelsOnly Or LookupOptions.MustNotBeLocalOrParameter)) = 0 Then

                'with small lists linear search may be cheaper than dictionary.
                'TODO: 6 is sufficiently small, but may need tuning.
                If locals.Length < 6 Then
                    For Each localSymbol In locals
                        Dim symName = localSymbol.Name
                        If symName Is name OrElse (symName.Length = name.Length And IdentifierComparison.Equals(symName, name)) Then
                            lookupResult.SetFrom(CheckViability(localSymbol, arity, options, Nothing, useSiteInfo))
                            Exit For
                        End If
                    Next
                Else
                    If Me.LocalsMap.TryGetValue(name, localSymbol) Then
                        lookupResult.SetFrom(CheckViability(localSymbol, arity, options, Nothing, useSiteInfo))
                    End If
                End If
            End If

            Return
        End Sub

        Friend Overrides Sub AddLookupSymbolsInfoInSingleBinder(nameSet As LookupSymbolsInfo,
                                                                    options As LookupOptions,
                                                                    originalBinder As Binder)
            Dim locals = Me.Locals
            If Not locals.IsEmpty AndAlso (options And (LookupOptions.NamespacesOrTypesOnly Or LookupOptions.LabelsOnly)) = 0 Then
                For Each localSymbol In locals
                    If originalBinder.CanAddLookupSymbolInfo(localSymbol, options, nameSet, Nothing) Then
                        nameSet.AddSymbol(localSymbol, localSymbol.Name, 0)
                    End If
                Next
            End If
        End Sub
    End Class
End Namespace
