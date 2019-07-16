' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Composition
Imports System.Threading
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.CodeActions
Imports Microsoft.CodeAnalysis.Editing
Imports Microsoft.CodeAnalysis.Host.Mef
Imports Microsoft.CodeAnalysis.Simplification
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.Editing
    <ExportLanguageService(GetType(ImportAdderService), LanguageNames.VisualBasic), [Shared]>
    Partial Friend Class VisualBasicImportAdder
        Inherits ImportAdderService

        <ImportingConstructor>
        Public Sub New()
        End Sub

        Protected Overrides Function GetExplicitNamespaceSymbol(node As SyntaxNode, model As SemanticModel) As INamespaceSymbol
            Dim qname = TryCast(node, QualifiedNameSyntax)
            If qname IsNot Nothing Then
                Return GetExplicitNamespaceSymbol(qname, qname.Left, model)
            End If

            Dim maccess = TryCast(node, MemberAccessExpressionSyntax)
            If maccess IsNot Nothing Then
                Return GetExplicitNamespaceSymbol(maccess, maccess.Expression, model)
            End If

            Return Nothing
        End Function

        Protected Overrides Function MakeSafeToAddNamespaces(root As SyntaxNode, namespaceMembers As IEnumerable(Of INamespaceOrTypeSymbol), extensionMethods As IEnumerable(Of IMethodSymbol), model As SemanticModel, workspace As Workspace, cancellationToken As CancellationToken) As SyntaxNode
            Dim Rewriter = New Rewriter(namespaceMembers, extensionMethods, model, workspace, cancellationToken)

            Return Rewriter.Visit(root)
        End Function

        Private Overloads Function GetExplicitNamespaceSymbol(fullName As ExpressionSyntax, namespacePart As ExpressionSyntax, model As SemanticModel) As INamespaceSymbol
            ' name must refer to something that is not a namespace, but be qualified with a namespace.
            Dim Symbol = model.GetSymbolInfo(fullName).Symbol
            Dim nsSymbol = TryCast(model.GetSymbolInfo(namespacePart).Symbol, INamespaceSymbol)

            If Symbol IsNot Nothing AndAlso Symbol.Kind <> SymbolKind.Namespace AndAlso nsSymbol IsNot Nothing Then
                ' use the symbols containing namespace, and not the potentially less than fully qualified namespace in the full name expression.
                Dim ns = Symbol.ContainingNamespace
                If ns IsNot Nothing Then
                    Return model.Compilation.GetCompilationNamespace(ns)
                End If
            End If

            Return Nothing
        End Function

        Private Class Rewriter
            Inherits VisualBasicSyntaxRewriter

            Private ReadOnly _workspace As Workspace
            Private ReadOnly _cancellationToken As CancellationToken
            Private ReadOnly _model As SemanticModel
            Private ReadOnly _namespaceMembers As HashSet(Of String)
            Private ReadOnly _extensionMethods As HashSet(Of String)

            Public Sub New(namespaceMembers As IEnumerable(Of INamespaceOrTypeSymbol), extensionMethods As IEnumerable(Of IMethodSymbol), model As SemanticModel, workspace As Workspace, cancellationToken As CancellationToken)
                _model = model
                _workspace = workspace
                _cancellationToken = cancellationToken
                _namespaceMembers = New HashSet(Of String)(namespaceMembers.[Select](Function(x) x.Name), CaseInsensitiveComparison.Comparer)
                _extensionMethods = New HashSet(Of String)(extensionMethods.[Select](Function(x) x.Name), CaseInsensitiveComparison.Comparer)
            End Sub

            Public Overrides ReadOnly Property VisitIntoStructuredTrivia As Boolean
                Get
                    Return True
                End Get
            End Property

            Public Overrides Function VisitIdentifierName(node As IdentifierNameSyntax) As SyntaxNode
                If _namespaceMembers.Contains(node.Identifier.Text) Then
                    Return Simplifier.Expand(Of SyntaxNode)(node, _model, _workspace, cancellationToken:=_cancellationToken)
                End If

                Return node
            End Function

            Public Overrides Function VisitGenericName(node As GenericNameSyntax) As SyntaxNode
                If _namespaceMembers.Contains(node.Identifier.Text) Then
                    Return Simplifier.Expand(Of SyntaxNode)(node, _model, _workspace, cancellationToken:=_cancellationToken)
                End If

                Dim typeArgumentList = DirectCast(MyBase.Visit(node.TypeArgumentList), TypeArgumentListSyntax)
                Return node.Update(node.Identifier, typeArgumentList)
            End Function

            Public Overrides Function VisitQualifiedName(node As QualifiedNameSyntax) As SyntaxNode
                Dim left = CType(MyBase.Visit(node.Left), NameSyntax)
                ' We don't recurse on the right, as if B is a member of the imported namespace, A.B is still not ambiguous
                Dim right = node.Right

                If TypeOf right Is GenericNameSyntax Then
                    Dim genericName = DirectCast(right, GenericNameSyntax)
                    Dim typeArgumentList = DirectCast(MyBase.Visit(genericName.TypeArgumentList), TypeArgumentListSyntax)
                    right = genericName.Update(genericName.Identifier, typeArgumentList)
                End If

                Return node.Update(left, node.DotToken, right)
            End Function

            Public Overrides Function VisitInvocationExpression(node As InvocationExpressionSyntax) As SyntaxNode
                If TypeOf node.Expression Is MemberAccessExpressionSyntax Then
                    Dim memberAccess = DirectCast(node.Expression, MemberAccessExpressionSyntax)
                    If _extensionMethods.Contains(memberAccess.Name.Identifier.Text) Then
                        ' No need to visit this as simplifier will expand everything
                        Return Simplifier.Expand(Of SyntaxNode)(node, _model, _workspace, cancellationToken:=_cancellationToken)
                    End If
                End If

                Return MyBase.VisitInvocationExpression(node)
            End Function

            Public Overrides Function VisitMemberAccessExpression(ByVal node As MemberAccessExpressionSyntax) As SyntaxNode
                node = DirectCast(MyBase.VisitMemberAccessExpression(node), MemberAccessExpressionSyntax)

                If _extensionMethods.Contains(node.Name.Identifier.Text) Then
                    ' If an extension method is used as a delegate rather than invoked directly,
                    ' there is no semantically valid transformation that will fully qualify the extension method. 
                    ' For example `Dim f As Func<int> = x.M;` is not the same as `Dim f As Func<int> = Function(x) Extensions.M(x);`
                    ' since one captures x by value, and the other by reference.
                    '
                    ' We will not visit this node if the parent node was an InvocationExpression, 
                    ' since we would have expanded the parent node entirely, rather than visiting it.
                    ' Therefore it's possible that this is an extension method being used as a delegate so we warn.
                    node = node.WithAdditionalAnnotations(WarningAnnotation.Create(String.Format(
                        WorkspacesResources.Warning_adding_imports_will_bring_an_extension_method_into_scope_with_the_same_name_as_member_access,
                        node.Name.Identifier.Text)))
                End If

                Return node
            End Function
        End Class
    End Class
End Namespace
