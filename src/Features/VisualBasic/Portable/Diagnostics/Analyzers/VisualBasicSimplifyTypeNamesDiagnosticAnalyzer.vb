﻿' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports System.Threading
Imports Microsoft.CodeAnalysis.CodeStyle
Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.Options
Imports Microsoft.CodeAnalysis.Shared.Collections
Imports Microsoft.CodeAnalysis.SimplifyTypeNames
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Simplification.Simplifiers
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.CodeFixes.SimplifyTypeNames

    <DiagnosticAnalyzer(LanguageNames.VisualBasic)>
    Friend NotInheritable Class VisualBasicSimplifyTypeNamesDiagnosticAnalyzer
        Inherits SimplifyTypeNamesDiagnosticAnalyzerBase(Of SyntaxKind)

        Private Shared ReadOnly s_kindsOfInterest As ImmutableArray(Of SyntaxKind) = ImmutableArray.Create(
            SyntaxKind.QualifiedName,
            SyntaxKind.SimpleMemberAccessExpression,
            SyntaxKind.IdentifierName,
            SyntaxKind.GenericName)

        Protected Overrides Function IsIgnoredCodeBlock(codeBlock As SyntaxNode) As Boolean
            ' Avoid analysis of compilation units and types in AnalyzeCodeBlock. These nodes appear in code block
            ' callbacks when they include attributes, but analysis of the node at this level would block more efficient
            ' analysis of descendant members.
            Return codeBlock.IsKind(SyntaxKind.CompilationUnit, SyntaxKind.ClassBlock, SyntaxKind.StructureBlock) OrElse
                codeBlock.IsKind(SyntaxKind.InterfaceBlock, SyntaxKind.ModuleBlock, SyntaxKind.EnumBlock) OrElse
                codeBlock.IsKind(SyntaxKind.DelegateFunctionStatement)
        End Function

        Protected Overrides Sub AnalyzeCodeBlock(context As CodeBlockAnalysisContext)
            Dim semanticModel = context.SemanticModel
            Dim cancellationToken = context.CancellationToken

            Dim syntaxTree = semanticModel.SyntaxTree
            Dim options = context.Options
            Dim optionSet = options.GetDocumentOptionSetAsync(syntaxTree, cancellationToken).GetAwaiter().GetResult()

            Dim simplifier As New TypeSyntaxSimplifierWalker(Me, semanticModel, optionSet, ignoredSpans:=Nothing, cancellationToken)
            simplifier.Visit(context.CodeBlock)
            If Not simplifier.HasDiagnostics Then
                Return
            End If

            For Each diagnostic In simplifier.Diagnostics
                context.ReportDiagnostic(diagnostic)
            Next
        End Sub

        Protected Overrides Sub AnalyzeSemanticModel(context As SemanticModelAnalysisContext, codeBlockIntervalTree As SimpleIntervalTree(Of TextSpan, TextSpanIntervalIntrospector))
            Dim semanticModel = context.SemanticModel
            Dim cancellationToken = context.CancellationToken

            Dim syntaxTree = semanticModel.SyntaxTree
            Dim options = context.Options
            Dim optionSet = options.GetDocumentOptionSetAsync(syntaxTree, cancellationToken).GetAwaiter().GetResult()
            Dim root = syntaxTree.GetRoot(cancellationToken)

            Dim simplifier As New TypeSyntaxSimplifierWalker(Me, semanticModel, optionSet, ignoredSpans:=codeBlockIntervalTree, cancellationToken)
            simplifier.Visit(root)
            If Not simplifier.HasDiagnostics Then
                Return
            End If

            For Each diagnostic In simplifier.Diagnostics
                context.ReportDiagnostic(diagnostic)
            Next
        End Sub

        Private Shared Function IsNodeKindInteresting(node As SyntaxNode) As Boolean
            Return s_kindsOfInterest.Contains(node.Kind)
        End Function

        Friend Overrides Function IsCandidate(node As SyntaxNode) As Boolean
            Return node IsNot Nothing AndAlso IsNodeKindInteresting(node)
        End Function

        Friend Overrides Function CanSimplifyTypeNameExpression(
                model As SemanticModel, node As SyntaxNode, optionSet As OptionSet,
                ByRef issueSpan As TextSpan, ByRef diagnosticId As String, ByRef inDeclaration As Boolean,
                cancellationToken As CancellationToken) As Boolean
            issueSpan = Nothing
            diagnosticId = IDEDiagnosticIds.SimplifyNamesDiagnosticId

            Dim memberAccess = TryCast(node, MemberAccessExpressionSyntax)
            If memberAccess IsNot Nothing AndAlso memberAccess.Expression.IsKind(SyntaxKind.MeExpression) Then
                ' don't bother analyzing "me.Goo" expressions.  They will be analyzed by
                ' the VisualBasicSimplifyThisOrMeDiagnosticAnalyzer.
                Return False
            End If

            Dim expression = DirectCast(node, ExpressionSyntax)
            If expression.ContainsDiagnostics Then
                Return False
            End If

            Dim replacementSyntax As ExpressionSyntax = Nothing
            If Not ExpressionSimplifier.Instance.TrySimplify(expression, model, optionSet, replacementSyntax, issueSpan, cancellationToken) Then
                Return False
            End If

            ' set proper diagnostic ids.
            If replacementSyntax.HasAnnotations(NameOf(CodeStyleOptions.PreferIntrinsicPredefinedTypeKeywordInDeclaration)) Then
                inDeclaration = True
                diagnosticId = IDEDiagnosticIds.PreferBuiltInOrFrameworkTypeDiagnosticId
            ElseIf replacementSyntax.HasAnnotations(NameOf(CodeStyleOptions.PreferIntrinsicPredefinedTypeKeywordInMemberAccess)) Then
                inDeclaration = False
                diagnosticId = IDEDiagnosticIds.PreferBuiltInOrFrameworkTypeDiagnosticId
            ElseIf expression.Kind = SyntaxKind.SimpleMemberAccessExpression Then
                Dim method = model.GetMemberGroup(expression)
                If method.Length = 1 Then
                    Dim symbol = method.First()
                    If (symbol.IsOverrides Or symbol.IsOverridable) And memberAccess.Expression.Kind = SyntaxKind.MyClassExpression Then
                        Return False
                    End If
                End If

                diagnosticId = IDEDiagnosticIds.SimplifyMemberAccessDiagnosticId
            End If

            Return True
        End Function

        Protected Overrides Function GetLanguageName() As String
            Return LanguageNames.VisualBasic
        End Function
    End Class
End Namespace
