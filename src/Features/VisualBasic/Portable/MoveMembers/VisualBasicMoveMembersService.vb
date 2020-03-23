' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports System.Composition
Imports System.Threading
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.CodeRefactorings
Imports Microsoft.CodeAnalysis.Formatting
Imports Microsoft.CodeAnalysis.Host.Mef
Imports Microsoft.CodeAnalysis.MoveMembers
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.MoveMembers

    <ExportLanguageService(GetType(AbstractMoveMembersService), LanguageNames.VisualBasic), [Shared]>
    Class VisualBasicMoveMembersService
        Inherits AbstractMoveMembersService

        <ImportingConstructor>
        <System.Diagnostics.CodeAnalysis.SuppressMessage("RoslynDiagnosticsReliability", "RS0033:Importing constructor should be [Obsolete]", Justification:="Used in tests")>
        Public Sub New()
        End Sub

        Protected Overrides Async Function GetSelectedMemberNodeAsync(document As Document, selection As Text.TextSpan, cancellationToken As Threading.CancellationToken) As Task(Of SyntaxNode)
            Dim helpers = document.GetRequiredLanguageService(Of IRefactoringHelpersService)()
            Dim relaventFieldNodes = Await helpers.GetRelevantNodesAsync(Of FieldDeclarationSyntax)(document, selection, cancellationToken).ConfigureAwait(False)
            Dim selectedNode = relaventFieldNodes.FirstOrDefault()

            If selectedNode IsNot Nothing Then
                Return selectedNode
            End If

            Dim relaventClassNodes = Await helpers.GetRelevantNodesAsync(Of ClassStatementSyntax)(document, selection, cancellationToken).ConfigureAwait(False)
            Return relaventClassNodes.FirstOrDefault()
        End Function

        Protected Overrides Async Function UpdateMembersWithExplicitImplementationsAsync(
            unformattedSolution As Solution, documentIds As IReadOnlyList(Of DocumentId), extractedInterfaceSymbol As INamedTypeSymbol,
            includedMembers As IEnumerable(Of ISymbol),
            symbolToDeclarationAnnotationMap As Dictionary(Of ISymbol, SyntaxAnnotation), cancellationToken As CancellationToken) As Task(Of Solution)

            Dim docToRootMap = New Dictionary(Of DocumentId, CompilationUnitSyntax)

            Dim implementedInterfaceStatementSyntax = If(extractedInterfaceSymbol.TypeParameters.Any(),
                SyntaxFactory.GenericName(
                    SyntaxFactory.Identifier(extractedInterfaceSymbol.Name),
                    SyntaxFactory.TypeArgumentList(SyntaxFactory.SeparatedList(extractedInterfaceSymbol.TypeParameters.Select(Function(p) SyntaxFactory.ParseTypeName(p.Name))))),
                SyntaxFactory.ParseTypeName(extractedInterfaceSymbol.Name))

            For Each member In includedMembers
                Dim annotation = symbolToDeclarationAnnotationMap(member)

                Dim token As SyntaxNodeOrToken = Nothing
                Dim currentDocId As DocumentId = Nothing
                Dim currentRoot As CompilationUnitSyntax = Nothing

                For Each candidateDocId In documentIds
                    If docToRootMap.ContainsKey(candidateDocId) Then
                        currentRoot = docToRootMap(candidateDocId)
                    Else
                        Dim document = Await unformattedSolution.GetDocument(candidateDocId).GetSyntaxRootAsync(cancellationToken).ConfigureAwait(False)
                        currentRoot = CType(document, CompilationUnitSyntax)
                    End If

                    token = currentRoot.DescendantNodesAndTokensAndSelf().FirstOrDefault(Function(x) x.HasAnnotation(annotation))
                    If token <> Nothing Then
                        currentDocId = candidateDocId
                        Exit For
                    End If
                Next

                If token = Nothing Then
                    Continue For
                End If

                Dim qualifiedName As QualifiedNameSyntax = SyntaxFactory.QualifiedName(implementedInterfaceStatementSyntax.GetRightmostName(), SyntaxFactory.IdentifierName(member.Name))

                Dim method = TryCast(token.Parent, MethodStatementSyntax)
                If method IsNot Nothing Then
                    docToRootMap(currentDocId) = currentRoot.ReplaceNode(method, method.WithImplementsClause(GetUpdatedImplementsClause(method.ImplementsClause, qualifiedName)))
                    Continue For
                End If

                Dim [event] = TryCast(token.Parent, EventStatementSyntax)
                If [event] IsNot Nothing Then
                    docToRootMap(currentDocId) = currentRoot.ReplaceNode([event], [event].WithImplementsClause(GetUpdatedImplementsClause([event].ImplementsClause, qualifiedName)))
                    Continue For
                End If

                Dim prop = TryCast(token.Parent, PropertyStatementSyntax)
                If prop IsNot Nothing Then
                    docToRootMap(currentDocId) = currentRoot.ReplaceNode(prop, prop.WithImplementsClause(GetUpdatedImplementsClause(prop.ImplementsClause, qualifiedName)))
                    Continue For
                End If
            Next

            Return CreateFinalSolution(unformattedSolution, documentIds, docToRootMap)
        End Function

        Private Function CreateFinalSolution(solutionWithInterfaceDocument As Solution, documentIds As IEnumerable(Of DocumentId), docToRootMap As Dictionary(Of DocumentId, CompilationUnitSyntax)) As Solution
            Dim finalSolution = solutionWithInterfaceDocument

            For Each docId In docToRootMap.Keys
                finalSolution = finalSolution.WithDocumentSyntaxRoot(docId, docToRootMap(docId), PreservationMode.PreserveIdentity)
            Next

            Return finalSolution
        End Function

        Private Function GetUpdatedImplementsClause(implementsClause As ImplementsClauseSyntax, qualifiedName As QualifiedNameSyntax) As ImplementsClauseSyntax
            If implementsClause IsNot Nothing Then
                Return implementsClause.AddInterfaceMembers(qualifiedName).WithAdditionalAnnotations(Formatter.Annotation)
            Else
                Return SyntaxFactory.ImplementsClause(qualifiedName).WithAdditionalAnnotations(Formatter.Annotation)
            End If
        End Function
    End Class
End Namespace

