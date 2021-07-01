' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Composition
Imports Microsoft.CodeAnalysis.Host.Mef
Imports Microsoft.CodeAnalysis.Simplification
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.CodeRefactorings.PullMemberUp
    <ExportLanguageService(GetType(IMembersPullerService), LanguageNames.VisualBasic), [Shared]>
    Friend Class ViualBasicMembersPullerService
        Inherits AbstractMembersPullerService(Of
            ImportsStatementSyntax)

        <ImportingConstructor>
        <Obsolete(MefConstruction.ImportingConstructorMessage, True)>
        Public Sub New()
        End Sub

        Protected Overrides Async Function GetImportsAsync(document As Document, cancellationToken As Threading.CancellationToken) As Task(Of Immutable.ImmutableArray(Of ImportsStatementSyntax))
            Dim root = Await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(False)
            Return root.DescendantNodesAndSelf().
                Where(Function(node) TypeOf node Is CompilationUnitSyntax).
                SelectMany(Function(node) DirectCast(node, CompilationUnitSyntax).Imports).
                Distinct().
                ToImmutableArrayOrEmpty()
        End Function

        Protected Overrides Function EnsureLeadingBlankLineBeforeFirstMember(node As SyntaxNode) As SyntaxNode
            If TypeOf node IsNot CompilationUnitSyntax Then
                Return node
            End If
            Dim members = DirectCast(node, CompilationUnitSyntax).Members
            If members.Count = 0 Then
                Return node
            End If

            Dim firstMember = members.First()
            Dim firstMemberTrivia = firstMember.GetLeadingTrivia()

            If firstMemberTrivia.Count > 0 And firstMemberTrivia.First().IsKind(SyntaxKind.EndOfLineTrivia) Then
                Return node
            End If

            Dim newFirstMember = firstMember.WithLeadingTrivia(firstMemberTrivia.Insert(0, SyntaxFactory.CarriageReturnLineFeed))
            Return node.ReplaceNode(firstMember, newFirstMember)
        End Function
    End Class
End Namespace

