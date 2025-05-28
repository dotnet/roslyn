' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports System.Composition
Imports System.Text
Imports System.Threading
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Host.Mef
Imports Microsoft.CodeAnalysis.PooledObjects
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Microsoft.VisualStudio.LanguageServices.Implementation.SolutionExplorer

Namespace Microsoft.VisualStudio.LanguageServices.VisualBasic.SolutionExplorer
    <ExportLanguageService(GetType(ISolutionExplorerSymbolTreeItemProvider), LanguageNames.VisualBasic), [Shared]>
    Friend Class VisualBasicSolutionExplorerSymbolTreeItemProvider
        Inherits AbstractSolutionExplorerSymbolTreeItemProvider(Of
            CompilationUnitSyntax,
            StatementSyntax,
            NamespaceBlockSyntax,
            EnumBlockSyntax,
            TypeBlockSyntax)

        <ImportingConstructor>
        <Obsolete(MefConstruction.ImportingConstructorMessage, True)>
        Public Sub New()
        End Sub

        Protected Overrides Function GetMembers(root As CompilationUnitSyntax) As SyntaxList(Of StatementSyntax)
            Return root.Members
        End Function

        Protected Overrides Function GetMembers(baseNamespace As NamespaceBlockSyntax) As SyntaxList(Of StatementSyntax)
            Return baseNamespace.Members
        End Function

        Protected Overrides Function GetMembers(typeDeclaration As TypeBlockSyntax) As SyntaxList(Of StatementSyntax)
            Return typeDeclaration.Members
        End Function

        Protected Overrides Function TryAddType(member As StatementSyntax, items As ArrayBuilder(Of SymbolTreeItemData), nameBuilder As StringBuilder, cancellationToken As CancellationToken) As Boolean
            Throw New NotImplementedException()
        End Function

        Protected Overrides Sub AddEnumDeclarationMembers(enumDeclaration As EnumBlockSyntax, items As ArrayBuilder(Of SymbolTreeItemData), cancellationToken As CancellationToken)
            For Each member In enumDeclaration.Members
                Dim enumMember = TryCast(member, EnumMemberDeclarationSyntax)
                items.Add(New SymbolTreeItemData(
                    enumMember.Identifier.ValueText,
                    Glyph.EnumMemberPublic,
                    hasItems:=False,
                    enumMember,
                    enumMember.Identifier))
            Next
        End Sub

        Protected Overrides Sub AddMemberDeclaration(member As StatementSyntax, items As ArrayBuilder(Of SymbolTreeItemData), nameBuilder As StringBuilder)
            Throw New NotImplementedException()
        End Sub
    End Class
End Namespace
