' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading
Imports System.Threading.Tasks
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.CaseCorrection
Imports Microsoft.CodeAnalysis.CodeGeneration
Imports Microsoft.CodeAnalysis.Formatting
Imports Microsoft.CodeAnalysis.Simplification
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.CodeGeneration
    Friend Class ImportsStatementsAdder
        Inherits AbstractImportsAdder

        Public Sub New(document As Document)
            MyBase.New(document)
        End Sub

        Protected Overrides Function GetInnermostNamespaceScope(nodeOrToken As SyntaxNodeOrToken) As SyntaxNode
            Dim node = If(nodeOrToken.IsNode, nodeOrToken.AsNode, nodeOrToken.Parent)
            Return If(node.GetAncestorOrThis(Of NamespaceBlockSyntax),
                      DirectCast(node.GetAncestorOrThis(Of CompilationUnitSyntax)(), SyntaxNode))
        End Function

        Protected Overrides Function GetImportsContainer(node As SyntaxNode) As SyntaxNode
            Return node.GetAncestorOrThis(Of CompilationUnitSyntax)()
        End Function

        Protected Overloads Overrides Function GetExistingNamespaces(semanticModel As SemanticModel, namespaceScope As SyntaxNode, cancellationToken As CancellationToken) As IList(Of INamespaceSymbol)
            If TypeOf namespaceScope Is NamespaceBlockSyntax Then
                Return GetExistingNamespaces(semanticModel, DirectCast(namespaceScope, NamespaceBlockSyntax), cancellationToken)
            Else
                Return GetExistingNamespaces(semanticModel, DirectCast(namespaceScope, CompilationUnitSyntax), cancellationToken)
            End If
        End Function

        Private Overloads Function GetExistingNamespaces(semanticModel As SemanticModel, namespaceDeclaration As NamespaceBlockSyntax, cancellationToken As CancellationToken) As IList(Of INamespaceSymbol)

            Dim namespaceSymbol = TryCast(semanticModel.GetDeclaredSymbol(namespaceDeclaration, cancellationToken), INamespaceSymbol)
            Dim namespaceImports = GetContainingNamespacesAndThis(namespaceSymbol).ToList()

            Dim outerNamespaces = Me.GetExistingNamespaces(semanticModel, namespaceDeclaration.Parent, cancellationToken)

            Return outerNamespaces.Concat(namespaceImports).
                                   Distinct().
                                   OrderBy(INamespaceSymbolExtensions.CompareNamespaces).
                                   ToList()
        End Function

        Private Overloads Function GetExistingNamespaces(semanticModel As SemanticModel, compilationUnit As CompilationUnitSyntax, cancellationToken As CancellationToken) As IList(Of INamespaceSymbol)
            Dim memberImports =
                From i In compilationUnit.Imports
                From c In i.ImportsClauses.OfType(Of SimpleImportsClauseSyntax)()
                Where c.Alias Is Nothing
                Let symbol = TryCast(semanticModel.GetSymbolInfo(c.Name, cancellationToken).Symbol, INamespaceSymbol)
                Where symbol IsNot Nothing AndAlso Not symbol.IsGlobalNamespace
                Select symbol

            Dim compilationImports =
                DirectCast(semanticModel.Compilation, VisualBasicCompilation).MemberImports.OfType(Of INamespaceSymbol)()

            Return memberImports.Concat(compilationImports).
                                 Distinct().
                                 OrderBy(INamespaceSymbolExtensions.CompareNamespaces).
                                 ToList()
        End Function

        Public Overrides Async Function AddAsync(members As IEnumerable(Of ISymbol),
                                      placeSystemNamespaceFirst As Boolean,
                                      options As CodeGenerationOptions,
                                      cancellationToken As CancellationToken) As Task(Of Document)

            Dim importsContainerToMissingNamespaces = Await DetermineNamespaceToImportAsync(members, options, cancellationToken).ConfigureAwait(False)
            If importsContainerToMissingNamespaces.Count = 0 Then
                Return Me.Document
            End If

            Dim commonRoot = Await Document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(False)
            Dim root = DirectCast(commonRoot, CompilationUnitSyntax)
            If Not root.CanAddImportsStatements(cancellationToken) Then
                Return Me.Document
            End If

            Dim usingDirectives =
                From n In importsContainerToMissingNamespaces.Values.Flatten
                Let name = DirectCast(n.GenerateTypeSyntax(addGlobal:=False), NameSyntax).WithAdditionalAnnotations(Simplifier.Annotation)
                Select SyntaxFactory.ImportsStatement(
                    importsClauses:=SyntaxFactory.SingletonSeparatedList(Of ImportsClauseSyntax)(SyntaxFactory.SimpleImportsClause(name)))

            Dim newRoot = root.AddImportsStatements(
                usingDirectives.ToList(), placeSystemNamespaceFirst,
                CaseCorrector.Annotation, Formatter.Annotation)

            Return Document.WithSyntaxRoot(newRoot)
        End Function
    End Class
End Namespace
