' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable

Namespace Microsoft.CodeAnalysis.VisualBasic.Symbols
    Friend Class RootSingleNamespaceDeclaration
        Inherits GlobalNamespaceDeclaration

        Private ReadOnly _referenceDirectives As ImmutableArray(Of ReferenceDirective)
        Private ReadOnly _hasAssemblyAttributes As Boolean

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
                       hasAssemblyAttributes As Boolean)
            MyBase.New(hasImports, treeNode, treeNode.GetLocation(), children)

            Debug.Assert(Not referenceDirectives.IsDefault)

            Me._referenceDirectives = referenceDirectives
            Me._hasAssemblyAttributes = hasAssemblyAttributes
        End Sub
    End Class
End Namespace
