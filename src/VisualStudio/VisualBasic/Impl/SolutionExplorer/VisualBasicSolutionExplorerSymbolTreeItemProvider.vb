' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports System.Composition
Imports System.Threading
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Host.Mef
Imports Microsoft.CodeAnalysis.PooledObjects
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Microsoft.VisualStudio.LanguageServices.Implementation.SolutionExplorer

Namespace Microsoft.VisualStudio.LanguageServices.VisualBasic.SolutionExplorer
    <ExportLanguageService(GetType(ISolutionExplorerSymbolTreeItemProvider), LanguageNames.VisualBasic), [Shared]>
    Friend Class VisualBasicSolutionExplorerSymbolTreeItemProvider
        Inherits AbstractSolutionExplorerSymbolTreeItemProvider

        <ImportingConstructor>
        <Obsolete(MefConstruction.ImportingConstructorMessage, True)>
        Public Sub New()
        End Sub

        Public Overrides Function GetItems(declarationNode As SyntaxNode, cancellationToken As CancellationToken) As ImmutableArray(Of SymbolTreeItemData)
            Dim items = ArrayBuilder(Of SymbolTreeItemData).GetInstance()
            Dim nameBuilder = PooledStringBuilder.GetInstance()

            Dim compilationUnit = TryCast(declarationNode, CompilationUnitSyntax)
            If compilationUnit IsNot Nothing Then
                AddTopLevelTypes(compilationUnit, items, nameBuilder, cancellationToken)
            End If

            Dim typeBlock = TryCast(declarationNode, TypeBlockSyntax)
            If typeBlock IsNot Nothing Then
                AddTypeBlockMembers(typeBlock, items, nameBuilder, cancellationToken)
            End If

            nameBuilder.Free()
            Return items.ToImmutableAndFree()
        End Function

        Private Sub AddTopLevelTypes(compilationUnit As CompilationUnitSyntax, items As ArrayBuilder(Of SymbolTreeItemData), nameBuilder As PooledStringBuilder, cancellationToken As CancellationToken)
            For Each member In compilationUnit.Members
                cancellationToken.ThrowIfCancellationRequested()

                Dim namespaceBlock = TryCast(member, NamespaceBlockSyntax)
                If namespaceBlock IsNot Nothing Then
                    AddTopLevelTypes(namespaceBlock, items, nameBuilder, cancellationToken)
                Else
                    TryAddType(member, items, nameBuilder, cancellationToken)
                End If
            Next
        End Sub

        Private Sub AddTopLevelTypes(namespaceBlock As NamespaceBlockSyntax, items As ArrayBuilder(Of SymbolTreeItemData), nameBuilder As PooledStringBuilder, cancellationToken As CancellationToken)
            For Each member In namespaceBlock.Members
                cancellationToken.ThrowIfCancellationRequested()

                Dim childNamespaceBlock = TryCast(member, NamespaceBlockSyntax)
                If childNamespaceBlock IsNot Nothing Then
                    AddTopLevelTypes(childNamespaceBlock, items, nameBuilder, cancellationToken)
                Else
                    TryAddType(member, items, nameBuilder, cancellationToken)
                End If
            Next
        End Sub

        Private Sub TryAddType(member As DeclarationStatementSyntax, items As ArrayBuilder(Of SymbolTreeItemData), nameBuilder As PooledStringBuilder, cancellationToken As CancellationToken)
            Throw New NotImplementedException()
        End Sub
    End Class
End Namespace
