' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Composition
Imports System.Threading
Imports Microsoft.CodeAnalysis.ExtractInterface
Imports Microsoft.CodeAnalysis.Formatting
Imports Microsoft.CodeAnalysis.Host.Mef
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.ExtractInterface
    <ExportLanguageService(GetType(AbstractExtractInterfaceService), LanguageNames.VisualBasic), [Shared]>
    Friend Class VisualBasicExtractInterfaceService
        Inherits AbstractExtractInterfaceService

        <ImportingConstructor>
        Public Sub New()
        End Sub

        Protected Overrides Async Function GetTypeDeclarationAsync(
            document As Document, position As Integer,
            typeDiscoveryRule As TypeDiscoveryRule,
            cancellationToken As CancellationToken) As Task(Of SyntaxNode)

            Dim tree = Await document.GetSyntaxTreeAsync(cancellationToken).ConfigureAwait(False)
            Dim root = Await tree.GetRootAsync(cancellationToken).ConfigureAwait(False)
            Dim token = root.FindToken(If(position <> tree.Length, position, Math.Max(0, position - 1)))
            Dim typeDeclaration = token.GetAncestor(Of TypeBlockSyntax)()

            If typeDeclaration Is Nothing OrElse
               typeDeclaration.Kind = SyntaxKind.ModuleStatement Then
                Return Nothing
            ElseIf typeDiscoveryRule = TypeDiscoveryRule.TypeDeclaration Then
                Return typeDeclaration
            End If

            Dim spanStart = typeDeclaration.BlockStatement.Identifier.SpanStart
            Dim spanEnd = If(typeDeclaration.BlockStatement.TypeParameterList IsNot Nothing, typeDeclaration.BlockStatement.TypeParameterList.Span.End, typeDeclaration.BlockStatement.Identifier.Span.End)
            Dim span = New TextSpan(spanStart, spanEnd - spanStart)

            Return If(span.IntersectsWith(position), typeDeclaration, Nothing)
        End Function

        Friend Overrides Function GetGeneratedNameTypeParameterSuffix(typeParameters As IList(Of ITypeParameterSymbol), workspace As Workspace) As String
            If typeParameters.IsEmpty() Then
                Return String.Empty
            End If

            Dim typeParameterList = SyntaxFactory.TypeParameterList(SyntaxFactory.SeparatedList(typeParameters.Select(Function(p) SyntaxFactory.TypeParameter(p.Name))))
            Return Formatter.Format(typeParameterList, workspace).ToString()
        End Function

        Friend Overrides Function GetContainingNamespaceDisplay(typeSymbol As INamedTypeSymbol, compilationOptions As CompilationOptions) As String
            Dim namespaceSymbol = typeSymbol.ContainingNamespace
            If namespaceSymbol.IsGlobalNamespace Then
                Return String.Empty
            End If

            Dim fullDisplayName = namespaceSymbol.ToDisplayString()

            Dim rootNamespace = DirectCast(compilationOptions, VisualBasicCompilationOptions).RootNamespace
            If rootNamespace Is Nothing OrElse rootNamespace = String.Empty Then
                Return fullDisplayName
            End If

            If rootNamespace.Equals(fullDisplayName, StringComparison.Ordinal) Then
                Return String.Empty
            End If

            If fullDisplayName.StartsWith(rootNamespace + ".", StringComparison.Ordinal) Then
                Return fullDisplayName.Substring(rootNamespace.Length + 1)
            End If

            Return fullDisplayName
        End Function

        Private Function GetUpdatedImplementsClause(implementsClause As ImplementsClauseSyntax, qualifiedName As QualifiedNameSyntax) As ImplementsClauseSyntax
            If implementsClause IsNot Nothing Then
                Return implementsClause.AddInterfaceMembers(qualifiedName).WithAdditionalAnnotations(Formatter.Annotation)
            Else
                Return SyntaxFactory.ImplementsClause(qualifiedName).WithAdditionalAnnotations(Formatter.Annotation)
            End If
        End Function

        Private Function CreateFinalSolution(solutionWithInterfaceDocument As Solution, documentIds As IEnumerable(Of DocumentId), docToRootMap As Dictionary(Of DocumentId, CompilationUnitSyntax)) As Solution
            Dim finalSolution = solutionWithInterfaceDocument

            For Each docId In documentIds
                finalSolution = finalSolution.WithDocumentSyntaxRoot(docId, docToRootMap(docId), PreservationMode.PreserveIdentity)
            Next

            Return finalSolution
        End Function

        Friend Overrides Function ShouldIncludeAccessibilityModifier(typeNode As SyntaxNode) As Boolean
            Dim typeDeclaration = DirectCast(typeNode, TypeBlockSyntax)
            Return typeDeclaration.GetModifiers().Any(Function(m) SyntaxFacts.IsAccessibilityModifier(m.Kind()))
        End Function

        Protected Overrides Async Function UpdateMembersWithExplicitImplementationsAsync(
            unformattedSolution As Solution, documentIds As IReadOnlyList(Of DocumentId), extractedInterfaceSymbol As INamedTypeSymbol,
            typeToExtractFrom As INamedTypeSymbol, includedMembers As IEnumerable(Of ISymbol),
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
    End Class
End Namespace
