' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading
Imports Microsoft.CodeAnalysis.PooledObjects
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Roslyn.Utilities

Namespace Microsoft.CodeAnalysis.VisualBasic

    Partial Public Class VisualBasicCompilation
        Friend Class EntryPointCandidateFinder
            Inherits VisualBasicSymbolVisitor(Of Predicate(Of Symbol), Boolean)

            Private ReadOnly _entryPointCandidates As ArrayBuilder(Of MethodSymbol)
            Private ReadOnly _visitNestedTypes As Boolean
            Private ReadOnly _cancellationToken As CancellationToken

            Friend Shared Sub FindCandidatesInNamespace(root As NamespaceSymbol, entryPointCandidates As ArrayBuilder(Of MethodSymbol), cancellationToken As CancellationToken)
                Dim finder As New EntryPointCandidateFinder(entryPointCandidates, visitNestedTypes:=True, cancellationToken:=cancellationToken)
                finder.Visit(root)
            End Sub

            Private Sub New(entryPointCandidates As ArrayBuilder(Of MethodSymbol),
                           visitNestedTypes As Boolean,
                           cancellationToken As CancellationToken)

                Me._entryPointCandidates = entryPointCandidates
                Me._visitNestedTypes = visitNestedTypes
                Me._cancellationToken = cancellationToken
            End Sub

            Public Overrides Function VisitNamespace(symbol As NamespaceSymbol, filter As Predicate(Of Symbol)) As Boolean
                _cancellationToken.ThrowIfCancellationRequested()

                If filter Is Nothing OrElse filter(symbol) Then
                    For Each member In symbol.GetMembersUnordered()
                        member.Accept(Me, filter)
                    Next
                End If

                Return True
            End Function

            Public Overrides Function VisitNamedType(symbol As NamedTypeSymbol, filter As Predicate(Of Symbol)) As Boolean
                _cancellationToken.ThrowIfCancellationRequested()

                If filter IsNot Nothing AndAlso Not filter(symbol) Then
                    Return True
                End If

                If symbol.IsEmbedded Then
                    ' Don't process embedded types
                    Return True
                End If

                For Each member In symbol.GetMembersUnordered()
                    ' process all members that are not methods as usual
                    If member.Kind = SymbolKind.NamedType Then
                        If Me._visitNestedTypes Then
                            member.Accept(Me, filter)
                        End If
                    ElseIf member.Kind = SymbolKind.Method Then

                        Dim method = DirectCast(member, MethodSymbol)
                        ' check if this is an entry point
                        If Not method.IsSubmissionConstructor AndAlso _entryPointCandidates IsNot Nothing AndAlso Not method.IsImplicitlyDeclared AndAlso method.IsEntryPointCandidate Then
                            _entryPointCandidates.Add(method)
                        End If
                    End If
                Next

                Return True
            End Function

        End Class
    End Class
End Namespace
