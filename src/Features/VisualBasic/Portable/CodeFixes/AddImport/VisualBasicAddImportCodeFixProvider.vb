' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable
Imports System.Composition
Imports System.Threading
Imports Microsoft.CodeAnalysis.CaseCorrection
Imports Microsoft.CodeAnalysis.CodeFixes
Imports Microsoft.CodeAnalysis.CodeFixes.AddImport
Imports Microsoft.CodeAnalysis.Formatting
Imports Microsoft.CodeAnalysis.LanguageServices
Imports Microsoft.CodeAnalysis.Simplification
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.CodeFixes.AddImport
    <ExportCodeFixProvider(LanguageNames.VisualBasic, Name:=PredefinedCodeFixProviderNames.AddUsingOrImport), [Shared]>
    Friend Class VisualBasicAddImportCodeFixProvider
        Inherits AbstractAddImportCodeFixProvider(Of SimpleNameSyntax)

        ''' <summary>
        ''' Type xxx is not defined
        ''' </summary>
        Friend Const BC30002 = "BC30002"

        ''' <summary>
        ''' Error 'x' is not declared
        ''' </summary>
        Friend Const BC30451 = "BC30451"

        ''' <summary>
        ''' xxx is not a member of yyy
        ''' </summary>
        Friend Const BC30456 = "BC30456"

        ''' <summary>
        ''' 'X' has no parameters and its return type cannot be indexed
        ''' </summary>
        Friend Const BC32016 = "BC32016"

        ''' <summary>
        ''' Too few type arguments
        ''' </summary>
        Friend Const BC32042 = "BC32042"

        ''' <summary>
        ''' Expression of type xxx is not queryable
        ''' </summary>
        Friend Const BC36593 = "BC36593"

        ''' <summary>
        ''' 'A' has no type parameters and so cannot have type arguments.
        ''' </summary>
        Friend Const BC32045 = "BC32045"

        ''' <summary>
        ''' 'A' is not accessible in this context because it is 'Friend'.
        ''' </summary>
        Friend Const BC30389 = "BC30389"

        ''' <summary>
        ''' 'A' cannot be used as an attribute because it does not inherit from 'System.Attribute'.
        ''' </summary>
        Friend Const BC31504 = "BC31504"

        ''' <summary>
        ''' Name 'A' is either not declared or not in the current scope.
        ''' </summary>
        Friend Const BC36610 = "BC36610"

        ''' <summary>
        ''' Cannot initialize the type 'A' with a collection initializer because it does not have an accessible 'Add' method
        ''' </summary>
        Friend Const BC36719 = "BC36719"

        ''' <summary>
        ''' Option Strict On disallows implicit conversions from 'Integer' to 'String'.
        ''' </summary>
        Friend Const BC30512 = "BC30512"

        ''' <summary>
        ''' 'A' is not accessible in this context because it is 'Private'.
        ''' </summary>
        Friend Const BC30390 = "BC30390"

        ''' <summary>
        ''' XML comment has a tag With a 'cref' attribute that could not be resolved. XML comment will be ignored.
        ''' </summary>
        Friend Const BC42309 = "BC42309"

        ''' <summary>
        ''' Type expected.
        ''' </summary>
        Friend Const BC30182 = "BC30182"

        Public Overrides ReadOnly Property FixableDiagnosticIds As ImmutableArray(Of String)
            Get
                Return ImmutableArray.Create(BC30002, BC30451, BC30456, BC32042, BC36593, BC32045, BC30389, BC31504, BC32016, BC36610, BC36719, BC30512, BC30390, BC42309, BC30182)
            End Get
        End Property

        Protected Overrides Function CanAddImport(node As SyntaxNode, cancellationToken As CancellationToken) As Boolean
            If node.GetAncestor(Of ImportsStatementSyntax)() IsNot Nothing Then
                Return False
            End If

            Return node.CanAddImportsStatements(cancellationToken)
        End Function

        Protected Overrides Function CanAddImportForMethod(
                diagnostic As Diagnostic,
                syntaxFacts As ISyntaxFactsService,
                node As SyntaxNode,
                ByRef nameNode As SimpleNameSyntax) As Boolean
            Select Case diagnostic.Id
                Case BC30456, BC30390, BC42309, BC30451
                    Exit Select
                Case BC30512
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
                Case BC36719
                    If node.IsKind(SyntaxKind.ObjectCollectionInitializer) Then
                        Return True
                    End If

                    Return False
                Case BC32016
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

        Protected Overrides Function CanAddImportForNamespace(diagnostic As Diagnostic, node As SyntaxNode, ByRef nameNode As SimpleNameSyntax) As Boolean
            Select Case diagnostic.Id
                Case BC30002, BC30451
                    Exit Select
                Case Else
                    Return False
            End Select

            Return CanAddImportForTypeOrNamespaceCore(node, nameNode)
        End Function

        Protected Overrides Function CanAddImportForQuery(diagnostic As Diagnostic, node As SyntaxNode) As Boolean
            If diagnostic.Id <> BC36593 Then
                Return False
            End If

            Dim queryClause = node.GetAncestor(Of QueryExpressionSyntax)()
            Return queryClause IsNot Nothing
        End Function

        Private Function IsOutermostQueryExpression(node As SyntaxNode) As Boolean
            ' TODO(cyrusn): Figure out how to implement this.
            Return True
        End Function

        Protected Overrides Function CanAddImportForType(
                diagnostic As Diagnostic, node As SyntaxNode, ByRef nameNode As SimpleNameSyntax) As Boolean
            Select Case diagnostic.Id
                Case BC30002, BC30451, BC32042, BC32045, BC30389, BC31504, BC36610, BC30182
                    Exit Select
                Case BC42309
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

        Protected Overrides Function GetDescription(namespaceSymbol As INamespaceOrTypeSymbol, semanticModel As SemanticModel, root As SyntaxNode) As String
            Return $"Imports {namespaceSymbol.ToDisplayString()}"
        End Function

        Protected Overrides Function GetNamespacesInScope(semanticModel As SemanticModel, node As SyntaxNode, cancellationToken As CancellationToken) As ISet(Of INamespaceSymbol)
            Return semanticModel.GetImportNamespacesInScope(node)
        End Function

        Protected Overrides Function GetQueryClauseInfo(
                model As SemanticModel,
                node As SyntaxNode,
                cancellationToken As CancellationToken) As ITypeSymbol

            Dim query = TryCast(node, QueryExpressionSyntax)

            If query Is Nothing Then
                query = node.GetAncestor(Of QueryExpressionSyntax)()
            End If

            Dim semanticModel = DirectCast(model, SemanticModel)

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

        Private Function IsValid(info As SymbolInfo) As Boolean
            Dim symbol = info.Symbol.GetOriginalUnreducedDefinition()
            Return symbol IsNot Nothing AndAlso symbol.Locations.Length > 0
        End Function

        Protected Overloads Overrides Function AddImportAsync(
                contextNode As SyntaxNode,
                symbol As INamespaceOrTypeSymbol,
                document As Document,
                placeSystemNamespaceFirst As Boolean,
                cancellationToken As CancellationToken) As Task(Of Document)

            Dim root = DirectCast(contextNode.SyntaxTree.GetRoot(cancellationToken), CompilationUnitSyntax)

            Dim memberImportsClause =
                SyntaxFactory.SimpleImportsClause(name:=DirectCast(symbol.GenerateTypeSyntax(addGlobal:=False), NameSyntax).WithAdditionalAnnotations(Simplifier.Annotation))
            Dim newImport = SyntaxFactory.ImportsStatement(
                importsClauses:=SyntaxFactory.SingletonSeparatedList(Of ImportsClauseSyntax)(memberImportsClause))

            Dim syntaxTree = contextNode.SyntaxTree
            Return Task.FromResult(
                document.WithSyntaxRoot(
                root.AddImportsStatement(newImport, placeSystemNamespaceFirst,
                                         CaseCorrector.Annotation, Formatter.Annotation)))
        End Function

        Protected Overrides Function IsViableExtensionMethod(method As IMethodSymbol,
                                                             expression As SyntaxNode,
                                                             semanticModel As SemanticModel,
                                                             syntaxFacts As ISyntaxFactsService,
                                                             cancellationToken As CancellationToken) As Boolean
            Dim leftExpressionType As ITypeSymbol = Nothing
            If syntaxFacts.IsInvocationExpression(expression) Then
                leftExpressionType = semanticModel.GetEnclosingNamedType(expression.SpanStart, cancellationToken)
            Else
                Dim leftExpression As SyntaxNode
                If TypeOf expression Is ObjectCreationExpressionSyntax Then
                    leftExpression = expression
                Else
                    leftExpression = syntaxFacts.GetExpressionOfMemberAccessExpression(expression)
                    If leftExpression Is Nothing Then
                        Return False
                    End If
                End If

                Dim semanticInfo = semanticModel.GetTypeInfo(leftExpression, cancellationToken)
                leftExpressionType = semanticInfo.Type
            End If

            Return leftExpressionType IsNot Nothing AndAlso method.ReduceExtensionMethod(leftExpressionType) IsNot Nothing
        End Function

        Friend Overrides Function IsViableField(field As IFieldSymbol, expression As SyntaxNode, semanticModel As SemanticModel, syntaxFacts As ISyntaxFactsService, cancellationToken As CancellationToken) As Boolean
            Return False
        End Function

        Friend Overrides Function IsViableProperty([property] As IPropertySymbol, expression As SyntaxNode, semanticModel As SemanticModel, syntaxFacts As ISyntaxFactsService, cancellationToken As CancellationToken) As Boolean
            Return False
        End Function

        Friend Overrides Function IsAddMethodContext(node As SyntaxNode, semanticModel As SemanticModel) As Boolean
            If node.IsKind(SyntaxKind.ObjectCollectionInitializer) Then
                Dim objectCreateExpression = node.GetAncestor(Of ObjectCreationExpressionSyntax)
                If objectCreateExpression Is Nothing Then
                    Return False
                End If

                Return True
            End If

            Return False
        End Function
    End Class
End Namespace
