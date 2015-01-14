' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Concurrent
Imports System.Collections.Generic
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

        Public Sub New(namespaceOrType As NamespaceOrTypeSymbol, importsClausePosition As Integer)
            Me.NamespaceOrType = namespaceOrType
            Me.ImportsClausePosition = importsClausePosition
        End Sub
    End Structure
End Namespace
