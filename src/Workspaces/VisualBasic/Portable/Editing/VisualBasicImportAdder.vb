' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable
Imports System.Composition
Imports System.Threading
Imports System.Threading.Tasks
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Editing
Imports Microsoft.CodeAnalysis.Host.Mef
Imports Microsoft.CodeAnalysis.Options
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Microsoft.CodeAnalysis.VisualBasic.Utilities

Namespace Microsoft.CodeAnalysis.VisualBasic.Editing
    <ExportLanguageService(GetType(ImportAdderService), LanguageNames.VisualBasic), [Shared]>
    Partial Friend Class VisualBasicImportAdder
        Inherits ImportAdderService

        Protected Overrides Async Function GetExistingImportedNamespacesAsync(
                document As Document, model As SemanticModel, namespaces As HashSet(Of INamespaceSymbol), cancellationToken As CancellationToken) As Task
            namespaces.AddRange(model.Compilation.MemberImports.OfType(Of INamespaceSymbol))

            ' consider all imports clauses
            Dim root = DirectCast(Await model.SyntaxTree.GetRootAsync(cancellationToken).ConfigureAwait(False), CompilationUnitSyntax)
            For Each import As ImportsStatementSyntax In root.Imports
                For Each clause In import.ImportsClauses
                    Dim symbol = GetImportedNamespaceSymbol(clause, model)
                    If symbol IsNot Nothing Then
                        namespaces.Add(symbol)
                    End If
                Next
            Next
        End Function

        Protected Overrides Function GetImportedNamespaceSymbol(namespaceImport As SyntaxNode, model As SemanticModel) As INamespaceSymbol
            Select Case namespaceImport.Kind
                Case SyntaxKind.ImportsStatement
                    Dim import = DirectCast(namespaceImport, ImportsStatementSyntax)
                    Return GetImportedNamespaceSymbol(import.ImportsClauses(0), model)
                Case SyntaxKind.SimpleImportsClause
                    Dim clause = DirectCast(namespaceImport, SimpleImportsClauseSyntax)
                    Return TryCast(model.GetSymbolInfo(clause.Name).Symbol, INamespaceSymbol)
                Case Else
                    Return Nothing
            End Select
        End Function

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

        Protected Overrides Function InsertNamespaceImport(root As SyntaxNode, gen As SyntaxGenerator, import As SyntaxNode, options As OptionSet) As SyntaxNode
            Dim comparer = If(options.GetOption(GenerationOptions.PlaceSystemNamespaceFirst, LanguageNames.VisualBasic),
                              ImportsStatementComparer.SystemFirstInstance,
                              ImportsStatementComparer.NormalInstance)

            ' find insertion point
            For Each existingImport As SyntaxNode In gen.GetNamespaceImports(root)
                If comparer.Compare(DirectCast(import, ImportsStatementSyntax), DirectCast(existingImport, ImportsStatementSyntax)) < 0 Then
                    Return gen.InsertNodesBefore(root, existingImport, {import})
                End If
            Next

            Return gen.AddNamespaceImports(root, import)
        End Function

    End Class
End Namespace
