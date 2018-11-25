' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading
Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.LanguageServices
Imports Microsoft.CodeAnalysis.MakeFieldReadonly
Imports Microsoft.CodeAnalysis.PooledObjects
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.MakeFieldReadonly
    <DiagnosticAnalyzer(LanguageNames.VisualBasic)>
    Friend Class VisualBasicMakeFieldReadonlyDiagnosticAnalyzer
        Inherits AbstractMakeFieldReadonlyDiagnosticAnalyzer(Of IdentifierNameSyntax, ConstructorBlockSyntax)

        Protected Overrides Function GetSyntaxFactsService() As ISyntaxFactsService
            Return VisualBasicSyntaxFactsService.Instance
        End Function

        Protected Overrides Function IsWrittenTo(name As IdentifierNameSyntax, model As SemanticModel, cancellationToken As CancellationToken) As Boolean
            Return name.IsWrittenTo(model, cancellationToken)
        End Function

        Protected Overrides Function IsMemberOfThisInstance(node As SyntaxNode) As Boolean
            ' if it is a qualified name, make sure it is `Me.name`
            Dim memberAccess = TryCast(node.Parent, MemberAccessExpressionSyntax)
            If memberAccess IsNot Nothing Then
                Return TryCast(memberAccess.Expression, MeExpressionSyntax) IsNot Nothing
            End If

            ' make sure it isn't in an object initializer
            If TryCast(node.Parent.Parent, ObjectCreationInitializerSyntax) IsNot Nothing Then
                Return False
            End If

            Return True
        End Function

        Protected Overrides Sub AddCandidateTypesInCompilationUnit(semanticModel As SemanticModel, compilationUnit As SyntaxNode, candidateTypes As PooledHashSet(Of (ITypeSymbol, SyntaxNode)), cancellationToken As CancellationToken)
            For Each node In compilationUnit.DescendantNodes(descendIntoChildren:=Function(n) IsContainerOrAnalyzableType(n))
                Dim typeBlock As TypeBlockSyntax
                If node.IsKind(SyntaxKind.ModuleBlock, SyntaxKind.ClassBlock, SyntaxKind.StructureBlock) Then
                    typeBlock = DirectCast(node, TypeBlockSyntax)
                Else
                    Continue For
                End If

                Dim current = typeBlock
                While current IsNot Nothing
                    If Not current.GetModifiers.Any(SyntaxKind.PartialKeyword) Then
                        ' VB allows one block to omit the Partial keyword
                        Dim currentSymbol = semanticModel.GetDeclaredSymbol(current)
                        If currentSymbol.DeclaringSyntaxReferences.Length = 1 Then
                            Dim addedSymbol = If(current Is typeBlock, currentSymbol, semanticModel.GetDeclaredSymbol(typeBlock))
                            candidateTypes.Add((addedSymbol, typeBlock))
                        End If
                    End If

                    current = TryCast(current.Parent, TypeBlockSyntax)
                End While
            Next
        End Sub

        Private Shared Function IsContainerOrAnalyzableType(node As SyntaxNode) As Boolean
            Return node.IsKind(SyntaxKind.CompilationUnit, SyntaxKind.NamespaceBlock) OrElse
                node.IsKind(SyntaxKind.ModuleBlock, SyntaxKind.ClassBlock, SyntaxKind.StructureBlock)
        End Function
    End Class
End Namespace
