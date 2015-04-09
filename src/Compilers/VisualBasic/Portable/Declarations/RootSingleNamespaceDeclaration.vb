' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System
Imports System.Collections.Generic
Imports System.Collections.Immutable
Imports System.Diagnostics
Imports System.Linq
Imports System.Text
Imports System.Threading
Imports Microsoft.CodeAnalysis.Collections
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.Symbols
    Friend Class RootSingleNamespaceDeclaration
        Inherits GlobalNamespaceDeclaration

        Private _referenceDirectiveDiagnostics As ImmutableArray(Of Diagnostic)
        Private _referenceDirectives As ImmutableArray(Of ReferenceDirective)
        Private _hasAssemblyAttributes As Boolean

        Public ReadOnly Property ReferenceDirectiveDiagnostics As ImmutableArray(Of Diagnostic)
            Get
                Return _referenceDirectiveDiagnostics
            End Get
        End Property

        Public ReadOnly Property ReferenceDirectives As ImmutableArray(Of ReferenceDirective)
            Get
                Return _referenceDirectives
            End Get
        End Property

        Public ReadOnly Property HasAssemblyAttributes As Boolean
            Get
                Return _hasAssemblyAttributes
            End Get
        End Property

        Public Sub New(hasImports As Boolean,
                       treeNode As SyntaxReference,
                       children As ImmutableArray(Of SingleNamespaceOrTypeDeclaration),
                       referenceDirectives As ImmutableArray(Of ReferenceDirective),
                       diagnostics As ImmutableArray(Of Diagnostic),
                       hasAssemblyAttributes As Boolean)
            MyBase.New(hasImports, treeNode, treeNode.GetLocation(), children)

            Debug.Assert(Not referenceDirectives.IsDefault)
            Debug.Assert(Not diagnostics.IsDefault)

            Me._referenceDirectives = referenceDirectives
            Me._referenceDirectiveDiagnostics = diagnostics
            Me._hasAssemblyAttributes = hasAssemblyAttributes
        End Sub
    End Class
End Namespace
