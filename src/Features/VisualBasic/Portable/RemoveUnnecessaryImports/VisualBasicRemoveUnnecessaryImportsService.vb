' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Composition
Imports System.Threading
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Formatting
Imports Microsoft.CodeAnalysis.Host.Mef
Imports Microsoft.CodeAnalysis.Internal.Log
Imports Microsoft.CodeAnalysis.RemoveUnnecessaryImports
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.RemoveUnnecessaryImports
    <ExportLanguageService(GetType(IRemoveUnnecessaryImportsService), LanguageNames.VisualBasic), [Shared]>
    Partial Friend Class VisualBasicRemoveUnnecessaryImportsService
        Inherits AbstractRemoveUnnecessaryImportsService(Of ImportsClauseSyntax)
        Implements IRemoveUnnecessaryImportsService

        Public Shared Function GetUnnecessaryImports(model As SemanticModel, root As SyntaxNode, cancellationToken As CancellationToken) As IEnumerable(Of SyntaxNode)
            Dim unnecessaryImports = DirectCast(GetIndividualUnnecessaryImports(model, root, cancellationToken), ISet(Of ImportsClauseSyntax))
            If unnecessaryImports Is Nothing Then
                Return Nothing
            End If

            Return unnecessaryImports.Select(
                    Function(i) As SyntaxNode
                        Dim statement = DirectCast(i.Parent, ImportsStatementSyntax)
                        If statement.ImportsClauses.All(AddressOf unnecessaryImports.Contains) Then
                            Return statement
                        Else
                            Return i
                        End If
                    End Function).ToSet()
        End Function

        Public Async Function RemoveUnnecessaryImportsAsync(document As Document, cancellationToken As CancellationToken) As Task(Of Document) Implements IRemoveUnnecessaryImportsService.RemoveUnnecessaryImportsAsync
            Using Logger.LogBlock(FunctionId.Refactoring_RemoveUnnecessaryImports_VisualBasic, cancellationToken)

                Dim unnecessaryImports = Await GetCommonUnnecessaryImportsOfAllContextAsync(document, cancellationToken).ConfigureAwait(False)
                If unnecessaryImports Is Nothing OrElse unnecessaryImports.Any(Function(import) import.OverlapsHiddenPosition(cancellationToken)) Then
                    Return document
                End If

                Dim root = Await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(False)

                Dim oldRoot = DirectCast(root, CompilationUnitSyntax)
                Dim newRoot = New Rewriter(unnecessaryImports, cancellationToken).Visit(oldRoot)
                newRoot = newRoot.WithAdditionalAnnotations(Formatter.Annotation)

                If cancellationToken.IsCancellationRequested Then
                    Return Nothing
                End If

                Return document.WithSyntaxRoot(newRoot)
            End Using
        End Function

        Protected Overrides Function GetUnusedUsings(model As SemanticModel, root As SyntaxNode, cancellationToken As CancellationToken) As IEnumerable(Of ImportsClauseSyntax)
            Return DirectCast(GetIndividualUnnecessaryImports(model, root, cancellationToken), IEnumerable(Of ImportsClauseSyntax))
        End Function

        Private Shared Function GetIndividualUnnecessaryImports(semanticModel As SemanticModel, root As SyntaxNode, cancellationToken As CancellationToken) As IEnumerable(Of SyntaxNode)
            Dim diagnostics = semanticModel.GetDiagnostics(cancellationToken:=cancellationToken)

            Dim unnecessaryImports = New HashSet(Of ImportsClauseSyntax)

            For Each diagnostic In diagnostics
                If diagnostic.Id = "BC50000" Then
                    Dim node = root.FindNode(diagnostic.Location.SourceSpan)
                    If node IsNot Nothing Then
                        unnecessaryImports.Add(DirectCast(node, ImportsClauseSyntax))
                    End If
                End If

                If diagnostic.Id = "BC50001" Then
                    Dim node = TryCast(root.FindNode(diagnostic.Location.SourceSpan), ImportsStatementSyntax)
                    If node IsNot Nothing Then
                        unnecessaryImports.AddRange(node.ImportsClauses)
                    End If
                End If
            Next

            If cancellationToken.IsCancellationRequested Then
                Return Nothing
            End If

            Dim oldRoot = DirectCast(root, CompilationUnitSyntax)
            AddRedundantImports(oldRoot, semanticModel, unnecessaryImports, cancellationToken)

            If unnecessaryImports.Count = 0 Then
                Return Nothing
            End If

            Return unnecessaryImports
        End Function

        Private Shared Sub AddRedundantImports(compilationUnit As CompilationUnitSyntax,
                                semanticModel As SemanticModel,
                                unnecessaryImports As HashSet(Of ImportsClauseSyntax),
                                cancellationToken As CancellationToken)
            ' Now that we've visited the tree, add any imports that bound to project level
            ' imports.  We definitely can remove them.
            For Each statement In compilationUnit.Imports
                For Each clause In statement.ImportsClauses
                    cancellationToken.ThrowIfCancellationRequested()

                    Dim simpleImportsClause = TryCast(clause, SimpleImportsClauseSyntax)
                    If simpleImportsClause IsNot Nothing Then
                        If simpleImportsClause.Alias Is Nothing Then
                            AddRedundantMemberImportsClause(simpleImportsClause, semanticModel, unnecessaryImports, cancellationToken)
                        Else
                            AddRedundantAliasImportsClause(simpleImportsClause, semanticModel, unnecessaryImports, cancellationToken)
                        End If
                    End If
                Next
            Next
        End Sub

        Private Shared Sub AddRedundantAliasImportsClause(clause As SimpleImportsClauseSyntax,
                                                   semanticModel As SemanticModel,
                                                   unnecessaryImports As HashSet(Of ImportsClauseSyntax),
                                                   cancellationToken As CancellationToken)

            Dim semanticInfo = semanticModel.GetSymbolInfo(clause.Name, cancellationToken)

            Dim namespaceOrType = TryCast(semanticInfo.Symbol, INamespaceOrTypeSymbol)
            If namespaceOrType Is Nothing Then
                Return
            End If

            Dim compilation = semanticModel.Compilation
            Dim aliasSymbol = compilation.AliasImports.FirstOrDefault(Function(a) a.Name = clause.Alias.Identifier.ValueText)
            If aliasSymbol IsNot Nothing AndAlso aliasSymbol.Target.Equals(semanticInfo.Symbol) Then
                unnecessaryImports.Add(clause)
            End If
        End Sub

        Private Shared Sub AddRedundantMemberImportsClause(clause As SimpleImportsClauseSyntax,
                                                    semanticModel As SemanticModel,
                                                    unnecessaryImports As HashSet(Of ImportsClauseSyntax),
                                                    cancellationToken As CancellationToken)

            Dim semanticInfo = semanticModel.GetSymbolInfo(clause.Name, cancellationToken)

            Dim namespaceOrType = TryCast(semanticInfo.Symbol, INamespaceOrTypeSymbol)
            If namespaceOrType Is Nothing Then
                Return
            End If

            Dim compilation = semanticModel.Compilation
            If compilation.MemberImports.Contains(namespaceOrType) Then
                unnecessaryImports.Add(clause)
            End If
        End Sub
    End Class
End Namespace
