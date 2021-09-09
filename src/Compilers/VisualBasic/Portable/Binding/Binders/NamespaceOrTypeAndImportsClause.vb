' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Concurrent
Imports System.Collections.Generic
Imports System.Collections.Immutable
Imports System.Runtime.InteropServices
Imports System.Threading
Imports Microsoft.CodeAnalysis.RuntimeMembers
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports TypeKind = Microsoft.CodeAnalysis.TypeKind

Namespace Microsoft.CodeAnalysis.VisualBasic

    Friend Structure NamespaceOrTypeAndImportsClausePosition
        Public ReadOnly NamespaceOrType As NamespaceOrTypeSymbol
        Public ReadOnly ImportsClausePosition As Integer
        Public ReadOnly Dependencies As ImmutableArray(Of AssemblySymbol)

        Public Sub New(namespaceOrType As NamespaceOrTypeSymbol, importsClausePosition As Integer, dependencies As ImmutableArray(Of AssemblySymbol))
            Me.NamespaceOrType = namespaceOrType
            Me.ImportsClausePosition = importsClausePosition
            Me.Dependencies = dependencies
        End Sub
    End Structure
End Namespace
