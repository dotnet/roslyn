' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols

Namespace Microsoft.CodeAnalysis.VisualBasic
    Friend Structure NamespaceOrTypeAndImportsClausePosition
        Public ReadOnly NamespaceOrType As NamespaceOrTypeSymbol
        Public ReadOnly ImportsClausePosition As Integer
        Public ReadOnly SyntaxReference As SyntaxReference
        Public ReadOnly Dependencies As ImmutableArray(Of AssemblySymbol)

        Public Sub New(namespaceOrType As NamespaceOrTypeSymbol, importsClausePosition As Integer, syntaxReference As SyntaxReference, dependencies As ImmutableArray(Of AssemblySymbol))
            Me.NamespaceOrType = namespaceOrType
            Me.ImportsClausePosition = importsClausePosition
            Me.SyntaxReference = syntaxReference
            Me.Dependencies = dependencies
        End Sub
    End Structure
End Namespace
