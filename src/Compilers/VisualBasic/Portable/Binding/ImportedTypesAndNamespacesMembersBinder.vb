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
    ''' Provides lookup in imported namespaces and types (not the alias kind),
    ''' either at file level or project level.
    ''' </summary>
    Friend Class ImportedTypesAndNamespacesMembersBinder
        Inherits Binder

        Private ReadOnly _importedSymbols As ImmutableArray(Of NamespaceOrTypeAndImportsClausePosition)

        Public Sub New(containingBinder As Binder, importedSymbols As ImmutableArray(Of NamespaceOrTypeAndImportsClausePosition))
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

            ' Look up the name in all imported symbols, merging and generating ambiguity errors if needed.

            Dim currentResult As LookupResult

            options = options Or LookupOptions.IgnoreExtensionMethods

            ' First, lookup immediate members of imported types and namespaces 
            For Each importedSym In _importedSymbols
                currentResult = LookupResult.GetInstance()

                If importedSym.NamespaceOrType.IsNamespace Then
                    originalBinder.LookupMemberImmediate(currentResult, DirectCast(importedSym.NamespaceOrType, NamespaceSymbol), name, arity, options, useSiteInfo)
                Else
                    originalBinder.LookupMember(currentResult, importedSym.NamespaceOrType, name, arity, options, useSiteInfo)
                End If

                If currentResult.IsGoodOrAmbiguous AndAlso Not originalBinder.IsSemanticModelBinder Then
                    Me.Compilation.MarkImportDirectiveAsUsed(Me.SyntaxTree, importedSym.ImportsClausePosition)
                End If

                ' If lookup in an import produces an ambiguous result, return that ambiguity.
                Dim cancelLookup As Boolean = currentResult.IsAmbiguous

                If cancelLookup Then
                    lookupResult.SetFrom(currentResult)
                Else
                    ' If currentResult is a namespace that doesn't contain accessible types 
                    ' (including types in child namespaces), ignore the namespace.
                    If Not (currentResult.IsGood AndAlso currentResult.HasSingleSymbol AndAlso
                            currentResult.SingleSymbol.Kind = SymbolKind.Namespace AndAlso
                            Not DirectCast(currentResult.SingleSymbol, NamespaceSymbol).ContainsTypesAccessibleFrom(Compilation.Assembly)) Then

                        If lookupResult.StopFurtherLookup AndAlso currentResult.StopFurtherLookup Then
                            Debug.Assert(lookupResult.Symbols.Count > 0)  ' How can it stop lookup otherwise?
                            Debug.Assert(currentResult.Symbols.Count > 0) ' How can it stop lookup  otherwise?

                            Dim lookupResultIsNamespace As Boolean = (lookupResult.Symbols(0).Kind = SymbolKind.Namespace)
                            Dim currentResultIsNamespace As Boolean = (currentResult.Symbols(0).Kind = SymbolKind.Namespace)

                            ' Non-namespace wins over a namespace
                            If lookupResultIsNamespace AndAlso (Not currentResultIsNamespace) Then
                                lookupResult.SetFrom(currentResult)

                            ElseIf (Not currentResultIsNamespace) OrElse lookupResultIsNamespace Then
                                Debug.Assert(currentResultIsNamespace = lookupResultIsNamespace)

                                ' Ignore the same result. Assuming that in case of overloaded members both equal results
                                ' should list members in the same order, it is sufficient to compare first
                                ' symbols from each result.
                                If Not (lookupResult.Symbols.Count = currentResult.Symbols.Count AndAlso
                                        lookupResult.Symbols(0).Equals(currentResult.Symbols(0))) Then
                                    If lookupResultIsNamespace AndAlso currentResult.IsGood AndAlso lookupResult.IsGood Then
                                        ' Collect all ambiguous namespaces so that we can create a namespace group at the end.
                                        lookupResult.Symbols.AddRange(currentResult.Symbols)
                                    Else
                                        lookupResult.MergeAmbiguous(currentResult, GenerateAmbiguityError)
                                    End If
                                End If

                            Else
                                Debug.Assert(currentResultIsNamespace AndAlso (Not lookupResultIsNamespace))
                                ' ignore the current result
                            End If
                        Else
                            lookupResult.MergeAmbiguous(currentResult, GenerateAmbiguityError)
                        End If
                    End If
                End If

                currentResult.Free()

                If cancelLookup Then
                    Debug.Assert(lookupResult.StopFurtherLookup)
                    Exit For
                End If
            Next

            If lookupResult.IsGood AndAlso lookupResult.Symbols.Count > 1 AndAlso lookupResult.Symbols(0).Kind = SymbolKind.Namespace Then
                ' Create and return namespace group symbol
                lookupResult.SetFrom(MergedNamespaceSymbol.CreateNamespaceGroup(lookupResult.Symbols.Cast(Of NamespaceSymbol)))
            End If
        End Sub

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
                If importedSym.NamespaceOrType.Kind = SymbolKind.NamedType Then
                    DirectCast(importedSym.NamespaceOrType, NamedTypeSymbol).AppendProbableExtensionMethods(name, methods)

                    If methods.Count <> 0 AndAlso Not originalBinder.IsSemanticModelBinder Then
                        Me.Compilation.MarkImportDirectiveAsUsed(Me.SyntaxTree, importedSym.ImportsClausePosition)
                    End If
                End If
            Next
        End Sub

        Protected Overrides Sub AddExtensionMethodLookupSymbolsInfoInSingleBinder(nameSet As LookupSymbolsInfo,
                                                                                   options As LookupOptions,
                                                                                   originalBinder As Binder)
            For Each importedSym In _importedSymbols
                If importedSym.NamespaceOrType.Kind = SymbolKind.NamedType Then
                    DirectCast(importedSym.NamespaceOrType, NamedTypeSymbol).AddExtensionMethodLookupSymbolsInfo(
                        nameSet, options, originalBinder)
                End If
            Next
        End Sub

        Friend Overrides Sub AddLookupSymbolsInfoInSingleBinder(nameSet As LookupSymbolsInfo,
                                                                    options As LookupOptions,
                                                                    originalBinder As Binder)
            For Each importedSym In _importedSymbols
                originalBinder.AddMemberLookupSymbolsInfo(nameSet, importedSym.NamespaceOrType, options Or LookupOptions.IgnoreExtensionMethods)
            Next
        End Sub

        Friend Shared GenerateAmbiguityError As Func(Of ImmutableArray(Of Symbol), AmbiguousSymbolDiagnostic) =
            Function(ambiguousSymbols As ImmutableArray(Of Symbol)) As AmbiguousSymbolDiagnostic
                Return New AmbiguousSymbolDiagnostic(ERRID.ERR_AmbiguousInImports2,
                                                     ambiguousSymbols,
                                                     ambiguousSymbols(0).Name,
                                                     New FormattedSymbolList(ambiguousSymbols.Select(Function(sym) sym.ContainingSymbol)))
            End Function
    End Class

End Namespace
