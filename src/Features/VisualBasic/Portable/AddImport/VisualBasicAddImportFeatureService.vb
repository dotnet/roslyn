' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Composition
Imports System.Threading
Imports Microsoft.CodeAnalysis.AddImport
Imports Microsoft.CodeAnalysis.CaseCorrection
Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.Editing
Imports Microsoft.CodeAnalysis.Formatting
Imports Microsoft.CodeAnalysis.Host.Mef
Imports Microsoft.CodeAnalysis.LanguageService
Imports Microsoft.CodeAnalysis.Simplification
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.AddImport
    <ExportLanguageService(GetType(IAddImportFeatureService), LanguageNames.VisualBasic), [Shared]>
    Friend NotInheritable Class VisualBasicAddImportFeatureService
        Inherits AbstractAddImportFeatureService(Of SimpleNameSyntax)

        <ImportingConstructor>
        <Obsolete(MefConstruction.ImportingConstructorMessage, True)>
        Public Sub New()
        End Sub

        Protected Overrides Function IsWithinImport(node As SyntaxNode) As Boolean
            Return node.GetAncestor(Of ImportsStatementSyntax)() IsNot Nothing
        End Function

        Protected Overrides Function CanAddImport(node As SyntaxNode, allowInHiddenRegions As Boolean, cancellationToken As CancellationToken) As Boolean
            Return node.CanAddImportsStatements(allowInHiddenRegions, cancellationToken)
        End Function

        Protected Overrides Function CanAddImportForMember(
                diagnosticId As String,
                syntaxFacts As ISyntaxFacts,
                node As SyntaxNode,
                ByRef nameNode As SimpleNameSyntax) As Boolean
            Select Case diagnosticId
                Case AddImportDiagnosticIds.BC30456,
                     AddImportDiagnosticIds.BC30390,
                     AddImportDiagnosticIds.BC42309,
                     AddImportDiagnosticIds.BC30451
                    Exit Select
                Case AddImportDiagnosticIds.BC30512
                    ' look up its corresponding method name
                    Dim parent = node.GetAncestor(Of InvocationExpressionSyntax)()
                    If parent Is Nothing Then
                        Return False
                    End If

                    Dim method = TryCast(parent.Expression, MemberAccessExpressionSyntax)
                    If method IsNot Nothing Then
                        node = method.Name
                    Else
                        node = parent.Expression
                    End If

                    Exit Select
                Case AddImportDiagnosticIds.BC36719
                    If node.IsKind(SyntaxKind.ObjectCollectionInitializer) Then
                        Return True
                    End If

                    Return False
                Case AddImportDiagnosticIds.BC32016
                    Dim memberAccessName = TryCast(node, MemberAccessExpressionSyntax)?.Name
                    Dim conditionalAccessName = TryCast(TryCast(TryCast(node, ConditionalAccessExpressionSyntax)?.WhenNotNull, InvocationExpressionSyntax)?.Expression, MemberAccessExpressionSyntax)?.Name

                    If memberAccessName Is Nothing AndAlso conditionalAccessName Is Nothing Then
                        Return False
                    End If

                    node = If(memberAccessName Is Nothing, conditionalAccessName, memberAccessName)
                    Exit Select
                Case Else
                    Return False
            End Select

            Dim memberAccess = TryCast(node, MemberAccessExpressionSyntax)
            If memberAccess IsNot Nothing Then
                node = memberAccess.Name
            End If

            If memberAccess.IsParentKind(SyntaxKind.SimpleMemberAccessExpression) Then
                Return False
            End If

            nameNode = TryCast(node, SimpleNameSyntax)
            If nameNode Is Nothing Then
                Return False
            End If

            Return True
        End Function

        Protected Overrides Function CanAddImportForNamespace(diagnosticId As String, node As SyntaxNode, ByRef nameNode As SimpleNameSyntax) As Boolean
            Select Case diagnosticId
                Case AddImportDiagnosticIds.BC30002,
                     IDEDiagnosticIds.UnboundIdentifierId,
                     AddImportDiagnosticIds.BC30451
                    Exit Select
                Case Else
                    Return False
            End Select

            Return CanAddImportForTypeOrNamespaceCore(node, nameNode)
        End Function

        Protected Overrides Function CanAddImportForDeconstruct(diagnosticId As String, node As SyntaxNode) As Boolean
            ' Not supported yet.
            Return False
        End Function

        Protected Overrides Function CanAddImportForGetAwaiter(diagnosticId As String, syntaxFactsService As ISyntaxFacts, node As SyntaxNode) As Boolean
            Return diagnosticId = AddImportDiagnosticIds.BC36930 AndAlso
                AncestorOrSelfIsAwaitExpression(syntaxFactsService, node)
        End Function

        Protected Overrides Function CanAddImportForGetEnumerator(diagnosticId As String, syntaxFactsService As ISyntaxFacts, node As SyntaxNode) As Boolean
            Return False
        End Function

        Protected Overrides Function CanAddImportForGetAsyncEnumerator(diagnosticId As String, syntaxFactsService As ISyntaxFacts, node As SyntaxNode) As Boolean
            Return False
        End Function

        Protected Overrides Function CanAddImportForQuery(diagnosticId As String, node As SyntaxNode) As Boolean
            Return diagnosticId = AddImportDiagnosticIds.BC36593 AndAlso
                node.GetAncestor(Of QueryExpressionSyntax)() IsNot Nothing
        End Function

        Protected Overrides Function CanAddImportForTypeOrNamespace(
                diagnosticId As String, node As SyntaxNode, ByRef nameNode As SimpleNameSyntax) As Boolean
            Select Case diagnosticId
                Case AddImportDiagnosticIds.BC30002,
                     IDEDiagnosticIds.UnboundIdentifierId,
                     AddImportDiagnosticIds.BC30451,
                     AddImportDiagnosticIds.BC32042,
                     AddImportDiagnosticIds.BC32045,
                     AddImportDiagnosticIds.BC30389,
                     AddImportDiagnosticIds.BC31504,
                     AddImportDiagnosticIds.BC36610,
                     AddImportDiagnosticIds.BC30182
                    Exit Select
                Case AddImportDiagnosticIds.BC42309
                    Select Case node.Kind
                        Case SyntaxKind.XmlCrefAttribute
                            node = CType(node, XmlCrefAttributeSyntax).Reference.DescendantNodes().OfType(Of IdentifierNameSyntax).FirstOrDefault()
                        Case SyntaxKind.CrefReference
                            node = CType(node, CrefReferenceSyntax).DescendantNodes().OfType(Of IdentifierNameSyntax).FirstOrDefault()
                    End Select
                Case Else
                    Return False
            End Select

            Return CanAddImportForTypeOrNamespaceCore(node, nameNode)
        End Function

        Private Shared Function CanAddImportForTypeOrNamespaceCore(node As SyntaxNode, ByRef nameNode As SimpleNameSyntax) As Boolean
            Dim qn = TryCast(node, QualifiedNameSyntax)
            If qn IsNot Nothing Then
                node = GetLeftMostSimpleName(qn)
            End If

            nameNode = TryCast(node, SimpleNameSyntax)
            Return nameNode.LooksLikeStandaloneTypeName()
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

        Protected Overrides Function GetDescription(nameParts As IReadOnlyList(Of String)) As String
            Return $"Imports { String.Join(".", nameParts) }"
        End Function

        Protected Overrides Function GetDescription(
            document As Document,
            options As AddImportPlacementOptions,
            symbol As INamespaceOrTypeSymbol,
            semanticModel As SemanticModel,
            root As SyntaxNode,
            cancellationToken As CancellationToken) As (description As String, hasExistingImport As Boolean)

            Dim importsStatement = GetImportsStatement(symbol)
            Dim addImportService = document.GetLanguageService(Of IAddImportsService)
            Dim generator = SyntaxGenerator.GetGenerator(document)
            Return ($"Imports {symbol.ToDisplayString()}",
                    addImportService.HasExistingImport(semanticModel, root, root, importsStatement, generator, cancellationToken))
        End Function

        Private Shared Function GetImportsStatement(symbol As INamespaceOrTypeSymbol) As ImportsStatementSyntax
            Dim nameSyntax = DirectCast(symbol.GenerateTypeSyntax(addGlobal:=False), NameSyntax)
            Return GetImportsStatement(nameSyntax)
        End Function

        Private Shared Function GetImportsStatement(nameSyntax As NameSyntax) As ImportsStatementSyntax
            nameSyntax = nameSyntax.WithAdditionalAnnotations(Simplifier.Annotation)

            Dim memberImportsClause = SyntaxFactory.SimpleImportsClause(nameSyntax)
            Dim newImport = SyntaxFactory.ImportsStatement(
                importsClauses:=SyntaxFactory.SingletonSeparatedList(Of ImportsClauseSyntax)(memberImportsClause))

            Return newImport
        End Function

        Protected Overrides Function GetImportNamespacesInScope(semanticModel As SemanticModel, node As SyntaxNode, cancellationToken As CancellationToken) As ISet(Of INamespaceSymbol)
            Return semanticModel.GetImportNamespacesInScope(node)
        End Function

        Protected Overrides Function GetDeconstructInfo(semanticModel As SemanticModel, node As SyntaxNode, cancellationToken As CancellationToken) As ITypeSymbol
            Return Nothing
        End Function

        Protected Overrides Function GetQueryClauseInfo(
                semanticModel As SemanticModel,
                node As SyntaxNode,
                cancellationToken As CancellationToken) As ITypeSymbol

            Dim query = TryCast(node, QueryExpressionSyntax)

            If query Is Nothing Then
                query = node.GetAncestor(Of QueryExpressionSyntax)()
            End If

            For Each clause In query.Clauses
                If TypeOf clause Is AggregateClauseSyntax Then
                    Dim aggregateClause = DirectCast(clause, AggregateClauseSyntax)
                    Dim aggregateInfo = semanticModel.GetAggregateClauseSymbolInfo(aggregateClause, cancellationToken)
                    If IsValid(aggregateInfo.Select1) OrElse IsValid(aggregateInfo.Select2) Then
                        Return Nothing
                    End If

                    For Each variable In aggregateClause.AggregationVariables
                        Dim info = semanticModel.GetSymbolInfo(variable.Aggregation, cancellationToken)
                        If IsValid(info) Then
                            Return Nothing
                        End If
                    Next
                Else
                    Dim symbolInfo = semanticModel.GetSymbolInfo(clause, cancellationToken)
                    If IsValid(symbolInfo) Then
                        Return Nothing
                    End If
                End If
            Next

            Dim type As ITypeSymbol
            Dim fromOrAggregateClause = query.Clauses.First()
            If TypeOf fromOrAggregateClause Is FromClauseSyntax Then
                Dim fromClause = DirectCast(fromOrAggregateClause, FromClauseSyntax)
                type = semanticModel.GetTypeInfo(fromClause.Variables.First().Expression, cancellationToken).Type
            Else
                Dim aggregateClause = DirectCast(fromOrAggregateClause, AggregateClauseSyntax)
                type = semanticModel.GetTypeInfo(aggregateClause.Variables.First().Expression, cancellationToken).Type
            End If

            Return type
        End Function

        Private Shared Function IsValid(info As SymbolInfo) As Boolean
            Dim symbol = info.Symbol.GetOriginalUnreducedDefinition()
            Return symbol IsNot Nothing AndAlso symbol.Locations.Length > 0
        End Function

        Protected Overloads Overrides Async Function AddImportAsync(
                contextNode As SyntaxNode,
                symbol As INamespaceOrTypeSymbol,
                document As Document,
                options As AddImportPlacementOptions,
                cancellationToken As CancellationToken) As Task(Of Document)

            Dim importsStatement = GetImportsStatement(symbol)

            Return Await AddImportAsync(contextNode, document, importsStatement, options, cancellationToken).ConfigureAwait(False)
        End Function

        Private Overloads Shared Async Function AddImportAsync(
                contextNode As SyntaxNode,
                document As Document,
                importsStatement As ImportsStatementSyntax,
                options As AddImportPlacementOptions,
                cancellationToken As CancellationToken) As Task(Of Document)

            Dim semanticModel = Await document.GetRequiredSemanticModelAsync(cancellationToken).ConfigureAwait(False)
            Dim importService = document.GetLanguageService(Of IAddImportsService)
            Dim generator = SyntaxGenerator.GetGenerator(document)

            Dim root = Await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(False)
            Dim newRoot = importService.AddImport(semanticModel, root, contextNode, importsStatement, generator, options, cancellationToken)
            newRoot = newRoot.WithAdditionalAnnotations(CaseCorrector.Annotation, Formatter.Annotation)
            Dim newDocument = document.WithSyntaxRoot(newRoot)

            Return newDocument
        End Function

        Protected Overrides Function AddImportAsync(
                contextNode As SyntaxNode,
                nameSpaceParts As IReadOnlyList(Of String),
                document As Document,
                options As AddImportPlacementOptions,
                cancellationToken As CancellationToken) As Task(Of Document)
            Dim nameSyntax = CreateNameSyntax(nameSpaceParts, nameSpaceParts.Count - 1)
            Dim importsStatement = GetImportsStatement(nameSyntax)

            Return AddImportAsync(contextNode, document, importsStatement, options, cancellationToken)
        End Function

        Private Shared Function CreateNameSyntax(nameSpaceParts As IReadOnlyList(Of String), index As Integer) As NameSyntax
            Dim namePiece = SyntaxFactory.IdentifierName(nameSpaceParts(index))
            Return If(index = 0,
                DirectCast(namePiece, NameSyntax),
                SyntaxFactory.QualifiedName(CreateNameSyntax(nameSpaceParts, index - 1), namePiece))
        End Function

        Protected Overrides Function IsAddMethodContext(
                node As SyntaxNode,
                semanticModel As SemanticModel,
                ByRef objectCreateExpression As SyntaxNode) As Boolean
            If node.IsKind(SyntaxKind.ObjectCollectionInitializer) Then
                objectCreateExpression = node.GetAncestor(Of ObjectCreationExpressionSyntax)
                Return objectCreateExpression IsNot Nothing
            End If

            objectCreateExpression = Nothing
            Return False
        End Function
    End Class
End Namespace
