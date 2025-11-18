' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Composition
Imports System.Diagnostics.CodeAnalysis
Imports System.Threading
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.CaseCorrection
Imports Microsoft.CodeAnalysis.CodeFixes.FullyQualify
Imports Microsoft.CodeAnalysis.Host.Mef
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.CodeFixes.FullyQualify
    <ExportLanguageService(GetType(IFullyQualifyService), LanguageNames.VisualBasic), [Shared]>
    Friend NotInheritable Class VisualBasicFullyQualifyService
        Inherits AbstractFullyQualifyService(Of SimpleNameSyntax)

        <ImportingConstructor>
        <Obsolete(MefConstruction.ImportingConstructorMessage, True)>
        Public Sub New()
        End Sub

        Protected Overrides Function CanFullyQualify(node As SyntaxNode, <NotNullWhen(True)> ByRef simpleName As SimpleNameSyntax) As Boolean
            Dim qn = TryCast(node, QualifiedNameSyntax)
            If qn IsNot Nothing Then
                node = GetLeftMostSimpleName(qn)
            End If

            simpleName = TryCast(node, SimpleNameSyntax)
            If simpleName Is Nothing Then
                Return False
            End If

            If Not simpleName.LooksLikeStandaloneTypeName() Then
                Return False
            End If

            If Not simpleName.CanBeReplacedWithAnyName() Then
                Return False
            End If

            Return True
        End Function

        Private Shared Function GetLeftMostSimpleName(qn As QualifiedNameSyntax) As SimpleNameSyntax
            While (qn IsNot Nothing)
                Dim left = qn.Left
                Dim simpleName = TryCast(left, SimpleNameSyntax)
                If simpleName IsNot Nothing Then
                    Return simpleName
                End If

                qn = TryCast(left, QualifiedNameSyntax)
            End While

            Return Nothing
        End Function

        Protected Overrides Async Function ReplaceNodeAsync(simpleName As SimpleNameSyntax, containerName As String, resultingSymbolIsType As Boolean, cancellationToken As CancellationToken) As Task(Of SyntaxNode)
            Dim leadingTrivia = simpleName.GetLeadingTrivia()
            Dim newName = simpleName.WithLeadingTrivia(CType(Nothing, SyntaxTriviaList))

            Dim qualifiedName = SyntaxFactory.QualifiedName(left:=SyntaxFactory.ParseName(containerName), right:=newName).
                WithLeadingTrivia(leadingTrivia).
                WithAdditionalAnnotations(CaseCorrector.Annotation)

            Dim tree = simpleName.SyntaxTree
            Dim root = Await tree.GetRootAsync(cancellationToken).ConfigureAwait(False)
            Return root.ReplaceNode(simpleName, qualifiedName)
        End Function
    End Class
End Namespace
