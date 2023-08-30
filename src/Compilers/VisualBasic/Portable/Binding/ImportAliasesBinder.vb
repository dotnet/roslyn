' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Concurrent
Imports System.Collections.Generic
Imports System.Collections.Immutable
Imports System.Runtime.InteropServices
Imports System.Threading
Imports Microsoft.CodeAnalysis.PooledObjects
Imports Microsoft.CodeAnalysis.RuntimeMembers
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Roslyn.Utilities
Imports TypeKind = Microsoft.CodeAnalysis.TypeKind

Namespace Microsoft.CodeAnalysis.VisualBasic

    ''' <summary>
    ''' A ImportAliasesBinder provides lookup for looking up import aliases (A = Goo.Bar),
    ''' either at file level or project level.
    ''' </summary>
    Friend Class ImportAliasesBinder
        Inherits Binder

        Private ReadOnly _importedAliases As IReadOnlyDictionary(Of String, AliasAndImportsClausePosition)

        Public Sub New(containingBinder As Binder, importedAliases As IReadOnlyDictionary(Of String, AliasAndImportsClausePosition))
            MyBase.New(containingBinder)

            Debug.Assert(importedAliases IsNot Nothing)

            _importedAliases = importedAliases

            ' Binder.Lookup relies on the following invariant.
            Debug.Assert(TypeOf containingBinder Is SourceFileBinder OrElse TypeOf containingBinder Is SourceModuleBinder OrElse
                         (TypeOf containingBinder Is ImportedTypesAndNamespacesMembersBinder AndAlso
                          TypeOf containingBinder.ContainingBinder Is TypesOfImportedNamespacesMembersBinder AndAlso
                          (TypeOf containingBinder.ContainingBinder.ContainingBinder Is SourceFileBinder OrElse
                           TypeOf containingBinder.ContainingBinder.ContainingBinder Is SourceModuleBinder)))
        End Sub

        Public Function GetImportChainData() As ImmutableArray(Of IAliasSymbol)
            Return ImmutableArray(Of IAliasSymbol).CastUp(_importedAliases.SelectAsArray(Function(kvp) kvp.Value.Alias))
        End Function

        Friend Overrides Sub LookupInSingleBinder(lookupResult As LookupResult,
                                                     name As String,
                                                     arity As Integer,
                                                     options As LookupOptions,
                                                     originalBinder As Binder,
                                                     <[In], Out> ByRef useSiteInfo As CompoundUseSiteInfo(Of AssemblySymbol))
            Debug.Assert(lookupResult.IsClear)

            Dim [alias] As AliasAndImportsClausePosition = Nothing
            If _importedAliases.TryGetValue(name, [alias]) Then
                ' Got an alias. Return it without checking arity.

                Dim res = CheckViability([alias].Alias, arity, options, Nothing, useSiteInfo)
                If res.IsGoodOrAmbiguous AndAlso Not originalBinder.IsSemanticModelBinder Then
                    Me.Compilation.MarkImportDirectiveAsUsed(Me.SyntaxTree, [alias].ImportsClausePosition)
                End If

                lookupResult.SetFrom(res) ' -1 for arity: don't check arity.
            Else
                Return
            End If
        End Sub

        Friend Overrides Sub AddLookupSymbolsInfoInSingleBinder(nameSet As LookupSymbolsInfo,
                                                                    options As LookupOptions,
                                                                    originalBinder As Binder)
            For Each [alias] In _importedAliases.Values
                If originalBinder.CheckViability([alias].Alias.Target, -1, options, Nothing, useSiteInfo:=CompoundUseSiteInfo(Of AssemblySymbol).Discarded).IsGoodOrAmbiguous Then
                    nameSet.AddSymbol([alias].Alias, [alias].Alias.Name, 0)
                End If
            Next
        End Sub

        Public Overrides ReadOnly Property ContainingMember As Symbol
            Get
                Return Me.Compilation.SourceModule
            End Get
        End Property

        Public Overrides ReadOnly Property AdditionalContainingMembers As ImmutableArray(Of Symbol)
            Get
                Return ImmutableArray(Of Symbol).Empty
            End Get
        End Property
    End Class

End Namespace
