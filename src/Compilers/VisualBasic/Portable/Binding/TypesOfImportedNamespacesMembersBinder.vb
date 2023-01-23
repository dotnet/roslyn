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
    ''' Provides lookup in types of imported namespaces, either at file level or project level.
    ''' </summary>
    Friend Class TypesOfImportedNamespacesMembersBinder
        Inherits Binder

        Private ReadOnly _importedSymbols As ImmutableArray(Of NamespaceOrTypeAndImportsClausePosition)

        Public Sub New(containingBinder As Binder,
                       importedSymbols As ImmutableArray(Of NamespaceOrTypeAndImportsClausePosition))
            MyBase.New(containingBinder)
            _importedSymbols = importedSymbols
        End Sub

        Friend Overrides Sub LookupInSingleBinder(lookupResult As LookupResult,
                                                     name As String,
                                                     arity As Integer,
                                                     options As LookupOptions,
                                                     originalBinder As Binder,
                                                     <[In], Out> ByRef useSiteInfo As CompoundUseSiteInfo(Of AssemblySymbol))
            Debug.Assert(lookupResult.IsClear)

            ' Lookup in modules of imported namespaces. 
            For Each importedSym In _importedSymbols
                If importedSym.NamespaceOrType.IsNamespace Then
                    Dim currentResult = LookupResult.GetInstance()
                    originalBinder.LookupMemberInModules(currentResult, DirectCast(importedSym.NamespaceOrType, NamespaceSymbol), name, arity, options, useSiteInfo)
                    If currentResult.IsGood AndAlso Not originalBinder.IsSemanticModelBinder Then
                        Me.Compilation.MarkImportDirectiveAsUsed(Me.SyntaxTree, importedSym.ImportsClausePosition)
                    End If

                    lookupResult.MergeAmbiguous(currentResult, ImportedTypesAndNamespacesMembersBinder.GenerateAmbiguityError)
                    currentResult.Free()
                End If
            Next
        End Sub

        Public Function GetImportChainData() As ImmutableArray(Of ImportedNamespaceOrType)
            Return _importedSymbols.SelectAsArray(Function(n) New ImportedNamespaceOrType(n.NamespaceOrType, n.SyntaxReference))
        End Function

        ''' <summary>
        ''' Collect extension methods with the given name that are in scope in this binder.
        ''' The passed in ArrayBuilder must be empty. Extension methods from the same containing type
        ''' must be grouped together. 
        ''' </summary>
        Protected Overrides Sub CollectProbableExtensionMethodsInSingleBinder(name As String,
                                                                      methods As ArrayBuilder(Of MethodSymbol),
                                                                      originalBinder As Binder)
            Debug.Assert(methods.Count = 0)

            For Each importedSym In _importedSymbols
                If importedSym.NamespaceOrType.IsNamespace Then
                    Dim count = methods.Count
                    DirectCast(importedSym.NamespaceOrType, NamespaceSymbol).AppendProbableExtensionMethods(name, methods)
                    If methods.Count <> count AndAlso Not originalBinder.IsSemanticModelBinder Then
                        Me.Compilation.MarkImportDirectiveAsUsed(Me.SyntaxTree, importedSym.ImportsClausePosition)
                    End If
                End If
            Next
        End Sub

        Protected Overrides Sub AddExtensionMethodLookupSymbolsInfoInSingleBinder(nameSet As LookupSymbolsInfo,
                                                                                   options As LookupOptions,
                                                                                   originalBinder As Binder)
            For Each importedSym In _importedSymbols
                If importedSym.NamespaceOrType.IsNamespace Then
                    DirectCast(importedSym.NamespaceOrType, NamespaceSymbol).AddExtensionMethodLookupSymbolsInfo(nameSet, options, originalBinder)
                End If
            Next
        End Sub

        Friend Overrides Sub AddLookupSymbolsInfoInSingleBinder(nameSet As LookupSymbolsInfo,
                                                                    options As LookupOptions,
                                                                    originalBinder As Binder)
            options = options Or LookupOptions.IgnoreExtensionMethods

            For Each importedSym In _importedSymbols
                If importedSym.NamespaceOrType.IsNamespace Then
                    For Each containedModule As NamedTypeSymbol In DirectCast(importedSym.NamespaceOrType, NamespaceSymbol).GetModuleMembers()
                        originalBinder.AddMemberLookupSymbolsInfo(nameSet, containedModule, options)
                    Next
                End If
            Next
        End Sub
    End Class

End Namespace
