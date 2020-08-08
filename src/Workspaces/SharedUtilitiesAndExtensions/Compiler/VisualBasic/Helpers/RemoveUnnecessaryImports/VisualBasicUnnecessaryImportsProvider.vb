' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports System.Threading
Imports Microsoft.CodeAnalysis.Formatting
Imports Microsoft.CodeAnalysis.RemoveUnnecessaryImports
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.RemoveUnnecessaryImports
    Friend NotInheritable Class VisualBasicUnnecessaryImportsProvider
        Inherits AbstractUnnecessaryImportsProvider(Of ImportsClauseSyntax)

        Public Shared Instance As New VisualBasicUnnecessaryImportsProvider

        Private Sub New()
        End Sub

        Protected Overrides Function GetUnnecessaryImports(
                model As SemanticModel, root As SyntaxNode,
                predicate As Func(Of SyntaxNode, Boolean),
                cancellationToken As CancellationToken) As ImmutableArray(Of SyntaxNode)
            predicate = If(predicate, Functions(Of SyntaxNode).True)
            Dim diagnostics = model.GetDiagnostics(cancellationToken:=cancellationToken)

            Dim unnecessaryImports = New HashSet(Of SyntaxNode)

            For Each diagnostic In diagnostics
                If diagnostic.Id = "BC50000" Then
                    Dim node = root.FindNode(diagnostic.Location.SourceSpan)
                    If node IsNot Nothing AndAlso predicate(node) Then
                        unnecessaryImports.Add(node)
                    End If
                End If

                If diagnostic.Id = "BC50001" Then
                    Dim node = TryCast(root.FindNode(diagnostic.Location.SourceSpan), ImportsStatementSyntax)
                    If node IsNot Nothing AndAlso predicate(node) Then
                        unnecessaryImports.AddRange(node.ImportsClauses)
                    End If
                End If
            Next

            Dim oldRoot = DirectCast(root, CompilationUnitSyntax)
            AddRedundantImports(oldRoot, model, unnecessaryImports, predicate, cancellationToken)

            Return unnecessaryImports.ToImmutableArray()
        End Function

        Private Shared Sub AddRedundantImports(
                compilationUnit As CompilationUnitSyntax,
                semanticModel As SemanticModel,
                unnecessaryImports As HashSet(Of SyntaxNode),
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
                            AddRedundantMemberImportsClause(simpleImportsClause, semanticModel, unnecessaryImports, predicate, cancellationToken)
                        Else
                            AddRedundantAliasImportsClause(simpleImportsClause, semanticModel, unnecessaryImports, predicate, cancellationToken)
                        End If
                    End If
                Next
            Next
        End Sub

        Private Shared Sub AddRedundantAliasImportsClause(
                clause As SimpleImportsClauseSyntax,
                semanticModel As SemanticModel,
                unnecessaryImports As HashSet(Of SyntaxNode),
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
                unnecessaryImports.Add(clause)
            End If
        End Sub

        Private Shared Sub AddRedundantMemberImportsClause(
                clause As SimpleImportsClauseSyntax,
                semanticModel As SemanticModel,
                unnecessaryImports As HashSet(Of SyntaxNode),
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
                unnecessaryImports.Add(clause)
            End If
        End Sub
    End Class
End Namespace
