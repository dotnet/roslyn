' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Concurrent
Imports System.Collections.Generic
Imports System.Runtime.InteropServices
Imports System.Threading
Imports Microsoft.CodeAnalysis.RuntimeMembers
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Roslyn.Utilities
Imports TypeKind = Microsoft.CodeAnalysis.TypeKind

Namespace Microsoft.CodeAnalysis.VisualBasic

    ''' <summary>
    ''' A ImportAliasesBinder provides lookup for looking up import aliases (A = Foo.Bar),
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

        Friend Overrides Sub LookupInSingleBinder(lookupResult As LookupResult,
                                                     name As String,
                                                     arity As Integer,
                                                     options As LookupOptions,
                                                     originalBinder As Binder,
                                                     <[In], Out> ByRef useSiteDiagnostics As HashSet(Of DiagnosticInfo))
            Debug.Assert(lookupResult.IsClear)

            Dim [alias] As AliasAndImportsClausePosition = Nothing
            If _importedAliases.TryGetValue(name, [alias]) Then
                ' Got an alias. Return it without checking arity.

                Dim res = CheckViability([alias].Alias, arity, options, Nothing, useSiteDiagnostics)
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
                If originalBinder.CheckViability([alias].Alias.Target, -1, options, Nothing, useSiteDiagnostics:=Nothing).IsGoodOrAmbiguous Then
                    nameSet.AddSymbol([alias].Alias, [alias].Alias.Name, 0)
                End If
            Next
        End Sub

        Public Overrides ReadOnly Property ContainingMember As Symbol
            Get
                Return Me.Compilation.SourceModule
            End Get
        End Property
    End Class

End Namespace
