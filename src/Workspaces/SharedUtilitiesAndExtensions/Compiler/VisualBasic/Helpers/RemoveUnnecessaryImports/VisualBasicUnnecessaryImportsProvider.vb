' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports System.Threading
Imports Microsoft.CodeAnalysis.Formatting
Imports Microsoft.CodeAnalysis.PooledObjects
Imports Microsoft.CodeAnalysis.RemoveUnnecessaryImports
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.RemoveUnnecessaryImports
    Friend NotInheritable Class VisualBasicUnnecessaryImportsProvider
        Inherits AbstractUnnecessaryImportsProvider(Of ImportsClauseSyntax)

        Private Const BC30561 As String = NameOf(BC30561) 'X' is ambiguous, imported from the namespaces or types...
        Private Const BC50000 As String = NameOf(BC50000) ' HDN_UnusedImportClause
        Private Const BC50001 As String = NameOf(BC50001) ' HDN_UnusedImportStatement

        Public Shared Instance As New VisualBasicUnnecessaryImportsProvider

        Private Sub New()
        End Sub

        Public Overrides Function GetUnnecessaryImports(
                model As SemanticModel,
                predicate As Func(Of SyntaxNode, Boolean),
                cancellationToken As CancellationToken) As ImmutableArray(Of ImportsClauseSyntax)

            Dim root = model.SyntaxTree.GetRoot(cancellationToken)
            predicate = If(predicate, Functions(Of SyntaxNode).True)
            Dim diagnostics = model.GetDiagnostics(cancellationToken:=cancellationToken)

            Dim unnecessaryImports = ArrayBuilder(Of ImportsClauseSyntax).GetInstance()

            For Each diagnostic In diagnostics
                If diagnostic.Id = BC50000 Then
                    Dim node = TryCast(root.FindNode(diagnostic.Location.SourceSpan), ImportsClauseSyntax)
                    If node IsNot Nothing AndAlso predicate(node) Then
                        unnecessaryImports.Add(node)
                    End If
                End If

                If diagnostic.Id = BC50001 Then
                    Dim node = TryCast(root.FindNode(diagnostic.Location.SourceSpan), ImportsStatementSyntax)
                    If node IsNot Nothing AndAlso predicate(node) Then
                        unnecessaryImports.AddRange(node.ImportsClauses)
                    End If
                End If
            Next

            ' Now, look for imports in the file that seem redundant because there is also the same project-level import.
            ' However, it may not be viable to remove these as its possible that these imports are necessary to prevent
            ' ambiguity warnings.  Specifically, the local imports are examined first prior to looking up in the project
            ' imports.
            Dim redundantImports = ArrayBuilder(Of ImportsClauseSyntax).GetInstance()
            AddRedundantImports(DirectCast(root, CompilationUnitSyntax), model, redundantImports, predicate, cancellationToken)

            For Each redundantImport In redundantImports
                If unnecessaryImports.Contains(redundantImport) OrElse
                   RemovalCausesAmbiguity(model, redundantImport, cancellationToken) Then
                    Continue For
                End If

                unnecessaryImports.Add(redundantImport)
            Next

            unnecessaryImports.RemoveDuplicates()
            Return unnecessaryImports.ToImmutableArray()
        End Function

        Private Shared Function RemovalCausesAmbiguity(model As SemanticModel, redundantImport As ImportsClauseSyntax, cancellationToken As CancellationToken) As Boolean
            Dim root = DirectCast(model.SyntaxTree.GetRoot(cancellationToken), CompilationUnitSyntax)

            Dim updatedRoot = VisualBasicRemoveUnnecessaryImportsRewriter.RemoveUnnecessaryImports(root, redundantImport, cancellationToken)
            Dim updatedSyntaxTree = model.SyntaxTree.WithRootAndOptions(updatedRoot, model.SyntaxTree.Options)
            Dim updatedCompilation = model.Compilation.ReplaceSyntaxTree(model.SyntaxTree, updatedSyntaxTree)

            Dim updatedModel = updatedCompilation.GetSemanticModel(updatedSyntaxTree)
            Dim diagnostics = updatedModel.GetDiagnostics(cancellationToken:=cancellationToken)
            Return diagnostics.Any(Function(d) d.Id = BC30561)
        End Function

        Private Shared Sub AddRedundantImports(
                compilationUnit As CompilationUnitSyntax,
                semanticModel As SemanticModel,
                redundantImports As ArrayBuilder(Of ImportsClauseSyntax),
                predicate As Func(Of SyntaxNode, Boolean),
                cancellationToken As CancellationToken)
            ' Now that we've visited the tree, add any imports that bound to project level
            ' imports.  We definitely can remove them.
            For Each statement In compilationUnit.Imports
                For Each clause In statement.ImportsClauses
                    cancellationToken.ThrowIfCancellationRequested()

                    Dim simpleImportsClause = TryCast(clause, SimpleImportsClauseSyntax)
                    If simpleImportsClause IsNot Nothing Then
                        If simpleImportsClause.Alias Is Nothing Then
                            AddRedundantMemberImportsClause(simpleImportsClause, semanticModel, redundantImports, predicate, cancellationToken)
                        Else
                            AddRedundantAliasImportsClause(simpleImportsClause, semanticModel, redundantImports, predicate, cancellationToken)
                        End If
                    End If
                Next
            Next
        End Sub

        Private Shared Sub AddRedundantAliasImportsClause(
                clause As SimpleImportsClauseSyntax,
                semanticModel As SemanticModel,
                redundantImports As ArrayBuilder(Of ImportsClauseSyntax),
                predicate As Func(Of SyntaxNode, Boolean),
                cancellationToken As CancellationToken)

            Dim semanticInfo = semanticModel.GetSymbolInfo(clause.Name, cancellationToken)

            Dim namespaceOrType = TryCast(semanticInfo.Symbol, INamespaceOrTypeSymbol)
            If namespaceOrType Is Nothing Then
                Return
            End If

            Dim compilation = semanticModel.Compilation
            Dim aliasSymbol = compilation.AliasImports.FirstOrDefault(Function(a) a.Name = clause.Alias.Identifier.ValueText)
            If aliasSymbol IsNot Nothing AndAlso
               aliasSymbol.Target.Equals(semanticInfo.Symbol) AndAlso
               predicate(clause) Then
                redundantImports.Add(clause)
            End If
        End Sub

        Private Shared Sub AddRedundantMemberImportsClause(
                clause As SimpleImportsClauseSyntax,
                semanticModel As SemanticModel,
                redundantImports As ArrayBuilder(Of ImportsClauseSyntax),
                predicate As Func(Of SyntaxNode, Boolean),
                cancellationToken As CancellationToken)

            Dim semanticInfo = semanticModel.GetSymbolInfo(clause.Name, cancellationToken)

            Dim namespaceOrType = TryCast(semanticInfo.Symbol, INamespaceOrTypeSymbol)
            If namespaceOrType Is Nothing Then
                Return
            End If

            Dim compilation = semanticModel.Compilation
            If compilation.MemberImports.Contains(namespaceOrType) AndAlso
               predicate(clause) Then
                redundantImports.Add(clause)
            End If
        End Sub
    End Class
End Namespace
