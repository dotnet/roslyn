' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable

Namespace Microsoft.CodeAnalysis.VisualBasic
    Partial Friend MustInherit Class VBSemanticModel
        Private NotInheritable Class ImportChainWrapper
            Implements IImportChain

            Public ReadOnly Property Parent As IImportChain Implements IImportChain.Parent
            Public ReadOnly Property Aliases As ImmutableArray(Of IAliasSymbol) Implements IImportChain.Aliases
            Public ReadOnly Property [Imports] As ImmutableArray(Of INamespaceOrTypeSymbol) Implements IImportChain.Imports
            Public ReadOnly Property XmlNamespaces As ImmutableArray(Of String) Implements IImportChain.XmlNamespaces

            Public ReadOnly Property ExternAliases As ImmutableArray(Of IAliasSymbol) Implements IImportChain.ExternAliases
                Get
                    Return ImmutableArray(Of IAliasSymbol).Empty
                End Get
            End Property

            Public Sub New(
                    parent As IImportChain,
                    aliases As ImmutableArray(Of IAliasSymbol),
                    [imports] As ImmutableArray(Of INamespaceOrTypeSymbol),
                    xmlNamespaces As ImmutableArray(Of String))
                Me.Parent = parent
                Me.Aliases = aliases
                Me.Imports = [imports]
                Me.XmlNamespaces = xmlNamespaces
            End Sub

            Public Shared Function Convert(binder As Binder) As IImportChain
                ' The binder chain has the following in it (walking from the innermost level outwards)
                '
                ' 1. Optional binders for the compilation unit of the present source file.
                ' 2. SourceFileBinder.  Required.
                ' 3. Optional binders for the imports brought in by the compilation options.
                '
                ' Both '1' and '3' are the same binders.  Specifically:
                '
                ' a. XmlNamespaceImportsBinder. Optional.  Present if source file has xml imports present.
                ' b. ImportAliasesBinder. Optional.  Present if source file has import aliases present.
                ' c. TypesOfImportedNamespacesMembersBinder.  Optional.  Present if source file has member imports present.
                '
                ' As such, we can walk upwards looking for any of these binders if present until we hit the end of the
                ' binder chain.  We know which set we're in depending on if we've seen the SourceFileBinder or not.
                '
                ' This also means that in VB the max length of the import chain is two, while in C# it can be unbounded
                ' in length.

                Dim typesOfImportedNamespacesMembers As TypesOfImportedNamespacesMembersBinder = Nothing
                Dim importAliases As ImportAliasesBinder = Nothing
                Dim xmlNamespaceImports As XmlNamespaceImportsBinder = Nothing

                While binder IsNot Nothing
                    If TypeOf binder Is SourceFileBinder Then
                        ' We hit the source file binder.  That means anything we found up till now were the imports for
                        ' this file.
                        Return Create(Convert(binder.ContainingBinder), typesOfImportedNamespacesMembers, importAliases, xmlNamespaceImports)
                    End If

                    typesOfImportedNamespacesMembers = If(typesOfImportedNamespacesMembers, TryCast(binder, TypesOfImportedNamespacesMembersBinder))
                    importAliases = If(importAliases, TryCast(binder, ImportAliasesBinder))
                    xmlNamespaceImports = If(xmlNamespaceImports, TryCast(binder, XmlNamespaceImportsBinder))

                    binder = binder.ContainingBinder
                End While

                ' We hit the end of the binder chain.  Anything we found up till now are the compilation option imports
                Return Create(parent:=Nothing, typesOfImportedNamespacesMembers, importAliases, xmlNamespaceImports)
            End Function

            Private Shared Function Create(
                parent As IImportChain,
                typesOfImportedNamespacesMembers As TypesOfImportedNamespacesMembersBinder,
                importAliases As ImportAliasesBinder,
                xmlNamespaceImports As XmlNamespaceImportsBinder) As IImportChain

                If typesOfImportedNamespacesMembers Is Nothing AndAlso importAliases Is Nothing AndAlso xmlNamespaceImports Is Nothing Then
                    Return parent
                End If

                Dim aliases = If(importAliases IsNot Nothing,
                    importAliases.GetImportChainData(),
                    ImmutableArray(Of IAliasSymbol).Empty)

                Dim [imports] = If(typesOfImportedNamespacesMembers IsNot Nothing,
                    typesOfImportedNamespacesMembers.GetImportChainData(),
                    ImmutableArray(Of INamespaceOrTypeSymbol).Empty)

                Dim xmlNamespaces = If(xmlNamespaceImports IsNot Nothing,
                    xmlNamespaceImports.GetImportChainData(),
                    ImmutableArray(Of String).Empty)

                Return New ImportChainWrapper(parent, aliases, [imports], xmlNamespaces)
            End Function
        End Class
    End Class
End Namespace
