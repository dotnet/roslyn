' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable
Imports System.Composition
Imports System.Threading
Imports Microsoft.CodeAnalysis.CodeRefactorings.SyncNamespace
Imports Microsoft.CodeAnalysis.Host.Mef
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.CodeRefactorings.SyncNamespace
    <ExportLanguageService(GetType(ISyncNamespaceService), LanguageNames.VisualBasic), [Shared]>
    Friend Class VisualBasicSyncNamespaceService
        Inherits AbstractSyncNamespaceService(Of NamespaceStatementSyntax, CompilationUnitSyntax)

        Public Overrides Function TryGetReplacementReferenceSyntax(reference As SyntaxNode, newNamespaceParts As ImmutableArray(Of String), ByRef old As SyntaxNode, ByRef [new] As SyntaxNode) As Boolean
            Dim nameRef = TryCast(reference, SimpleNameSyntax)
            If nameRef IsNot Nothing Then
                Dim outerMostNode = GetQualifiedNameSyntax(nameRef)
                old = outerMostNode

                If outerMostNode Is nameRef Or newNamespaceParts.IsDefaultOrEmpty Then
                    [new] = outerMostNode
                Else
                    If newNamespaceParts.Length = 1 And newNamespaceParts(0).Length = 0 Then
                        [new] = nameRef
                    Else
                        Dim qualifiedNamespaceName = CreateNameSyntax(newNamespaceParts, newNamespaceParts.Length - 1)
                        [new] = SyntaxFactory.QualifiedName(qualifiedNamespaceName, nameRef)
                    End If
                    [new] = [new].WithTriviaFrom(outerMostNode)
                End If
                Return True
            Else
                old = Nothing
                [new] = Nothing
                Return False
            End If
        End Function

        Protected Overrides Function EscapeIdentifier(identifier As String) As String
            Return identifier?.EscapeIdentifier()
        End Function

        ' This is only reachable when called from a VB refacoring provider, which is not implemented yet.
        Protected Overrides Function ChangeNamespaceDeclaration(root As SyntaxNode, declaredNamespaceParts As ImmutableArray(Of String), targetNamespaceParts As ImmutableArray(Of String)) As SyntaxNode
            Throw ExceptionUtilities.Unreachable
        End Function

        ' This is only reachable when called from a VB refacoring provider, which is not implemented yet.
        Protected Overrides Function GetMemberDeclarationsInContainer(compilationUnitOrNamespaceDecl As SyntaxNode) As IReadOnlyList(Of SyntaxNode)
            Throw ExceptionUtilities.Unreachable
        End Function

        ' This is only reachable when called from a VB refacoring provider, which is not implemented yet.
        Protected Overrides Function ShouldPositionTriggerRefactoringAsync(document As Document, position As Integer, cancellationToken As CancellationToken) As Task(Of (Boolean, NamespaceStatementSyntax))
            Throw ExceptionUtilities.Unreachable
        End Function

        Private Function GetQualifiedNameSyntax(node As NameSyntax) As NameSyntax
            Dim parent = TryCast(node.Parent, QualifiedNameSyntax)
            If parent IsNot Nothing Then
                Return If(parent.Right Is node, parent, node)
            Else
                Return node
            End If
        End Function

        Private Function CreateNameSyntax(namespaceParts As ImmutableArray(Of String), index As Integer) As NameSyntax
            Dim part = namespaceParts(index).EscapeIdentifier()
            Dim namePiece = SyntaxFactory.IdentifierName(part)

            If index = 0 Then
                Return namePiece
            Else
                Return SyntaxFactory.QualifiedName(CreateNameSyntax(namespaceParts, index - 1), namePiece)
            End If
        End Function
    End Class
End Namespace
