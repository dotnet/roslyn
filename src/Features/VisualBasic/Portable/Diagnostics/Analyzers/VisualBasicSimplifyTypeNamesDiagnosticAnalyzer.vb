' Licensed to the .NET Foundation under one or more agreements.
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
Imports Microsoft.CodeAnalysis.VisualBasic.Simplification
Imports Microsoft.CodeAnalysis.VisualBasic.Simplification.Simplifiers
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.CodeFixes.SimplifyTypeNames

    <DiagnosticAnalyzer(LanguageNames.VisualBasic)>
    Friend NotInheritable Class VisualBasicSimplifyTypeNamesDiagnosticAnalyzer
        Inherits SimplifyTypeNamesDiagnosticAnalyzerBase(Of SyntaxKind, VisualBasicSimplifierOptions)

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

        Protected Overrides Function AnalyzeCodeBlock(context As CodeBlockAnalysisContext, root As SyntaxNode) As ImmutableArray(Of Diagnostic)
            Debug.Assert(context.CodeBlock.DescendantNodesAndSelf().Contains(root))

            Dim semanticModel = context.SemanticModel
            Dim cancellationToken = context.CancellationToken

            Dim simplifierOptions = context.GetVisualBasicAnalyzerOptions().GetSimplifierOptions()

            Dim simplifier As New TypeSyntaxSimplifierWalker(Me, semanticModel, simplifierOptions, ignoredSpans:=Nothing, cancellationToken)
            simplifier.Visit(root)
            Return simplifier.Diagnostics
        End Function

        Protected Overrides Function AnalyzeSemanticModel(context As SemanticModelAnalysisContext, root As SyntaxNode, codeBlockIntervalTree As SimpleIntervalTree(Of TextSpan, TextSpanIntervalIntrospector)) As ImmutableArray(Of Diagnostic)
            Dim configOptions = context.Options.AnalyzerConfigOptionsProvider.GetOptions(context.SemanticModel.SyntaxTree)
            Dim simplifierOptions = context.GetVisualBasicAnalyzerOptions().GetSimplifierOptions()
            Dim simplifier As New TypeSyntaxSimplifierWalker(Me, context.SemanticModel, simplifierOptions, ignoredSpans:=codeBlockIntervalTree, context.CancellationToken)
            simplifier.Visit(root)
            Return simplifier.Diagnostics
        End Function

        Private Shared Function IsNodeKindInteresting(node As SyntaxNode) As Boolean
            Return s_kindsOfInterest.Contains(node.Kind)
        End Function

        Friend Overrides Function IsCandidate(node As SyntaxNode) As Boolean
            Return node IsNot Nothing AndAlso IsNodeKindInteresting(node)
        End Function

        Friend Overrides Function CanSimplifyTypeNameExpression(
                model As SemanticModel, node As SyntaxNode, options As VisualBasicSimplifierOptions,
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
            If Not ExpressionSimplifier.Instance.TrySimplify(expression, model, options, replacementSyntax, issueSpan, cancellationToken) Then
                Return False
            End If

            ' set proper diagnostic ids.
            If replacementSyntax.HasAnnotations(NameOf(CodeStyleOptions2.PreferIntrinsicPredefinedTypeKeywordInDeclaration)) Then
                inDeclaration = True
                diagnosticId = IDEDiagnosticIds.PreferBuiltInOrFrameworkTypeDiagnosticId
            ElseIf replacementSyntax.HasAnnotations(NameOf(CodeStyleOptions2.PreferIntrinsicPredefinedTypeKeywordInMemberAccess)) Then
                inDeclaration = False
                diagnosticId = IDEDiagnosticIds.PreferBuiltInOrFrameworkTypeDiagnosticId
            ElseIf expression.Kind = SyntaxKind.SimpleMemberAccessExpression Then
                Dim method = model.GetMemberGroup(expression, cancellationToken)
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
    End Class
End Namespace
