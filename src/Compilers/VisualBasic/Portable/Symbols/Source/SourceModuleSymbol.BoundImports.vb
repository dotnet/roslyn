' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Generic
Imports System.Collections.Immutable
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.Symbols
    Friend Partial Class SourceModuleSymbol
        ' A class to hold the bound project-level imports, and associated binding diagnostics.
        Private NotInheritable Class BoundImports
            ' can be Nothing if no member imports
            Public ReadOnly MemberImports As ImmutableArray(Of NamespaceOrTypeAndImportsClausePosition)
            Public ReadOnly MemberImportsInfo As ImmutableArray(Of GlobalImportInfo)

            ' can be Nothing if no alias imports
            Public ReadOnly AliasImportsMap As Dictionary(Of String, AliasAndImportsClausePosition)
            Public ReadOnly AliasImports As ImmutableArray(Of AliasAndImportsClausePosition)
            Public ReadOnly AliasImportsInfo As ImmutableArray(Of GlobalImportInfo)

            ' can be Nothing if no xmlns imports
            Public ReadOnly XmlNamespaces As Dictionary(Of String, XmlNamespaceAndImportsClausePosition)

            Public ReadOnly Diagnostics As DiagnosticBag

            Public Sub New(memberImports As ImmutableArray(Of NamespaceOrTypeAndImportsClausePosition),
                           memberImportsInfo As ImmutableArray(Of GlobalImportInfo),
                           aliasImportsMap As Dictionary(Of String, AliasAndImportsClausePosition),
                           aliasImports As ImmutableArray(Of AliasAndImportsClausePosition),
                           aliasImportsInfo As ImmutableArray(Of GlobalImportInfo),
                           xmlNamespaces As Dictionary(Of String, XmlNamespaceAndImportsClausePosition),
                           diags As DiagnosticBag)
                Me.MemberImports = memberImports
                Me.MemberImportsInfo = memberImportsInfo
                Me.AliasImportsMap = aliasImportsMap
                Me.AliasImports = aliasImports
                Me.AliasImportsInfo = aliasImportsInfo
                Me.XmlNamespaces = xmlNamespaces
                Me.Diagnostics = diags
            End Sub
        End Class

        Private Structure GlobalImportInfo
            Public ReadOnly Import As GlobalImport
            Public ReadOnly SyntaxReference As SyntaxReference

            Public Sub New(import As GlobalImport, syntaxReference As SyntaxReference)
                Me.Import = import
                Me.SyntaxReference = syntaxReference
            End Sub
        End Structure
    End Class
End Namespace
