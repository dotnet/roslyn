' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Generic
Imports System.Collections.Immutable
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.PooledObjects
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.Symbols

    Partial Friend NotInheritable Class EmbeddedSymbolManager

        Friend NotInheritable Class EmbeddedNamedTypeSymbol
            Inherits SourceNamedTypeSymbol

            Private ReadOnly _kind As EmbeddedSymbolKind

            Public Sub New(decl As MergedTypeDeclaration, containingSymbol As NamespaceOrTypeSymbol, containingModule As SourceModuleSymbol, kind As EmbeddedSymbolKind)
                MyBase.New(decl, containingSymbol, containingModule)

#If DEBUG Then
                Dim references As ImmutableArray(Of SyntaxReference) = decl.SyntaxReferences
                Debug.Assert(references.Length() = 1)
                Debug.Assert(references.First.SyntaxTree.IsEmbeddedSyntaxTree())
#End If

                Debug.Assert(kind <> VisualBasic.Symbols.EmbeddedSymbolKind.None)
                _kind = kind
            End Sub

            Public Overrides ReadOnly Property IsImplicitlyDeclared As Boolean
                Get
                    Return True
                End Get
            End Property

            Friend Overrides ReadOnly Property AreMembersImplicitlyDeclared As Boolean
                Get
                    Return True
                End Get
            End Property

            Friend Overrides ReadOnly Property EmbeddedSymbolKind As EmbeddedSymbolKind
                Get
                    Return _kind
                End Get
            End Property

            Friend Overrides Function GetMembersForCci() As ImmutableArray(Of Symbol)
                Dim builder = ArrayBuilder(Of Symbol).GetInstance()
                Dim manager As EmbeddedSymbolManager = Me.DeclaringCompilation.EmbeddedSymbolManager
                For Each member In Me.GetMembers
                    If manager.IsSymbolReferenced(member) Then
                        builder.Add(member)
                    End If
                Next
                Return builder.ToImmutableAndFree()
            End Function

        End Class
    End Class
End Namespace
