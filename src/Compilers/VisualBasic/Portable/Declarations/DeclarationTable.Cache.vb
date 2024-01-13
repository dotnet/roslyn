' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable

Namespace Microsoft.CodeAnalysis.VisualBasic.Symbols
    Partial Friend Class DeclarationTable
        ' The structure of the DeclarationTable provides us with a set of 'old' declarations that
        ' stay relatively unchanged and a 'new' declaration that is repeatedly added and removed.
        ' This mimics the expected usage pattern of a user repeatedly typing in a single file.
        ' Because of this usage pattern, we can cache information about these 'old' declarations and
        ' keep that around as long as they do not change. For example, we keep a single 'merged
        ' declaration' for all those root declarations as well as sets of interesting information
        ' (like the type names in those decls).
        Private Class Cache
            ' The merged root declaration for all the 'old' declarations.
            Friend ReadOnly MergedRoot As Lazy(Of MergedNamespaceDeclaration)

            ' All the simple type names for all the types in the 'old' declarations.
            Friend ReadOnly TypeNames As Lazy(Of ICollection(Of String))
            Friend ReadOnly NamespaceNames As Lazy(Of ICollection(Of String))
            Friend ReadOnly ReferenceDirectives As Lazy(Of ImmutableArray(Of ReferenceDirective))

            Public Sub New(table As DeclarationTable)
                Me.MergedRoot = New Lazy(Of MergedNamespaceDeclaration)(AddressOf table.MergeOlderNamespaces)

                Me.TypeNames = New Lazy(Of ICollection(Of String))(Function() GetTypeNames(Me.MergedRoot.Value))

                Me.NamespaceNames = New Lazy(Of ICollection(Of String))(Function() GetNamespaceNames(Me.MergedRoot.Value))

                Me.ReferenceDirectives = New Lazy(Of ImmutableArray(Of ReferenceDirective))(
                    Function() table.SelectManyFromOlderDeclarationsNoEmbedded(Function(r) r.ReferenceDirectives))
            End Sub
        End Class
    End Class
End Namespace
