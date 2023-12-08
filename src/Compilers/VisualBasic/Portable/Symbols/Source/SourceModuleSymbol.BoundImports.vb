' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Generic
Imports System.Collections.Immutable
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.Symbols
    Partial Friend Class SourceModuleSymbol
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

            Public ReadOnly Diagnostics As ImmutableBindingDiagnostic(Of AssemblySymbol)

            Public Sub New(memberImports As ImmutableArray(Of NamespaceOrTypeAndImportsClausePosition),
                           memberImportsInfo As ImmutableArray(Of GlobalImportInfo),
                           aliasImportsMap As Dictionary(Of String, AliasAndImportsClausePosition),
                           aliasImports As ImmutableArray(Of AliasAndImportsClausePosition),
                           aliasImportsInfo As ImmutableArray(Of GlobalImportInfo),
                           xmlNamespaces As Dictionary(Of String, XmlNamespaceAndImportsClausePosition),
                           diags As ImmutableBindingDiagnostic(Of AssemblySymbol))
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
