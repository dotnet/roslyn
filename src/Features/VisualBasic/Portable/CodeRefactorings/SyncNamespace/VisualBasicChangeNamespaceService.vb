' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports System.Composition
Imports System.Threading
Imports Microsoft.CodeAnalysis.ChangeNamespace
Imports Microsoft.CodeAnalysis.Host.Mef
Imports Microsoft.CodeAnalysis.LanguageService
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.ChangeNamespace
    <ExportLanguageService(GetType(IChangeNamespaceService), LanguageNames.VisualBasic), [Shared]>
    Friend Class VisualBasicChangeNamespaceService
        Inherits AbstractChangeNamespaceService(Of NamespaceStatementSyntax, CompilationUnitSyntax, StatementSyntax)

        <ImportingConstructor>
        <Obsolete(MefConstruction.ImportingConstructorMessage, True)>
        Public Sub New()
        End Sub

        Public Overrides Function TryGetReplacementReferenceSyntax(reference As SyntaxNode, newNamespaceParts As ImmutableArray(Of String), syntaxFacts As ISyntaxFactsService, ByRef old As SyntaxNode, ByRef [new] As SyntaxNode) As Boolean
            Dim nameRef = TryCast(reference, SimpleNameSyntax)
            old = nameRef
            [new] = nameRef

            If nameRef Is Nothing Or newNamespaceParts.IsDefaultOrEmpty Then
                Return False
            End If

            If syntaxFacts.IsRightOfQualifiedName(nameRef) Then
                old = nameRef.Parent
                If IsGlobalNamespace(newNamespaceParts) Then
                    [new] = SyntaxFactory.QualifiedName(SyntaxFactory.GlobalName(), nameRef.WithoutTrivia())
                Else
                    Dim qualifiedNamespaceName = CreateNamespaceAsQualifiedName(newNamespaceParts, newNamespaceParts.Length - 1)
                    [new] = SyntaxFactory.QualifiedName(qualifiedNamespaceName, nameRef.WithoutTrivia())
                End If

                [new] = [new].WithTriviaFrom(old)

            ElseIf syntaxFacts.IsNameOfsimpleMemberAccessExpression(nameRef) Then
                old = nameRef.Parent
                If IsGlobalNamespace(newNamespaceParts) Then
                    [new] = SyntaxFactory.SimpleMemberAccessExpression(SyntaxFactory.GlobalName(), nameRef.WithoutTrivia())
                Else
                    Dim memberAccessNamespaceName = CreateNamespaceAsMemberAccess(newNamespaceParts, newNamespaceParts.Length - 1)
                    [new] = SyntaxFactory.SimpleMemberAccessExpression(memberAccessNamespaceName, nameRef.WithoutTrivia())
                End If

                [new] = [new].WithTriviaFrom(old)
            End If

            Return True
        End Function

        ' TODO: Implement the service for VB
        Protected Overrides Function GetValidContainersFromAllLinkedDocumentsAsync(document As Document, container As SyntaxNode, cancellationToken As CancellationToken) As Task(Of ImmutableArray(Of (DocumentId, SyntaxNode)))
            Return SpecializedTasks.Default(Of ImmutableArray(Of (DocumentId, SyntaxNode)))()
        End Function

        ' This is only reachable when called from a VB service, which is not implemented yet.
        Protected Overrides Function ChangeNamespaceDeclaration(root As CompilationUnitSyntax, declaredNamespaceParts As ImmutableArray(Of String), targetNamespaceParts As ImmutableArray(Of String)) As CompilationUnitSyntax
            Throw ExceptionUtilities.Unreachable
        End Function

        ' This is only reachable when called from a VB service, which is not implemented yet.
        Protected Overrides Function GetMemberDeclarationsInContainer(container As SyntaxNode) As SyntaxList(Of StatementSyntax)
            Throw ExceptionUtilities.Unreachable
        End Function

        ' This is only reachable when called from a VB service, which is not implemented yet.
        Protected Overrides Function TryGetApplicableContainerFromSpanAsync(document As Document, span As TextSpan, cancellationToken As CancellationToken) As Task(Of SyntaxNode)
            Throw ExceptionUtilities.Unreachable
        End Function

        ' This is only reachable when called from a VB service, which is not implemented yet.
        Protected Overrides Function GetDeclaredNamespace(container As SyntaxNode) As String
            Throw ExceptionUtilities.Unreachable
        End Function

        Private Shared Function CreateNamespaceAsQualifiedName(namespaceParts As ImmutableArray(Of String), index As Integer) As NameSyntax
            Dim part = namespaceParts(index).EscapeIdentifier()
            Dim namePiece = SyntaxFactory.IdentifierName(part)

            If index = 0 Then
                Return namePiece
            Else
                Return SyntaxFactory.QualifiedName(CreateNamespaceAsQualifiedName(namespaceParts, index - 1), namePiece)
            End If
        End Function

        Private Shared Function CreateNamespaceAsMemberAccess(namespaceParts As ImmutableArray(Of String), index As Integer) As ExpressionSyntax
            Dim part = namespaceParts(index).EscapeIdentifier()
            Dim namePiece = SyntaxFactory.IdentifierName(part)

            If index = 0 Then
                Return namePiece
            Else
                Return SyntaxFactory.SimpleMemberAccessExpression(CreateNamespaceAsMemberAccess(namespaceParts, index - 1), namePiece)
            End If
        End Function
    End Class
End Namespace
