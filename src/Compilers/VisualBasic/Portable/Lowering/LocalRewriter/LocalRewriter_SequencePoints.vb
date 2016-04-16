' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols

Namespace Microsoft.CodeAnalysis.VisualBasic
    Friend Partial Class LocalRewriter
        Friend Function AddConditionSequencePoint(condition As BoundExpression, containingCatchWithFilter As BoundCatchBlock) As BoundExpression
            Debug.Assert(containingCatchWithFilter.ExceptionFilterOpt.Syntax.Parent.IsKind(SyntaxKind.CatchFilterClause))
            Dim local As LocalSymbol = Nothing
            Return AddConditionSequencePoint(condition, containingCatchWithFilter.ExceptionFilterOpt.Syntax.Parent, local, wasGenerated:=containingCatchWithFilter.WasCompilerGenerated, shareLocal:=False)
        End Function

        Friend Function AddConditionSequencePoint(condition As BoundExpression, containingStatement As BoundStatement) As BoundExpression
            Dim local As LocalSymbol = Nothing
            Return AddConditionSequencePoint(condition, containingStatement.Syntax, local, wasGenerated:=containingStatement.WasCompilerGenerated, shareLocal:=False)
        End Function

        Friend Function AddConditionSequencePoint(condition As BoundExpression, containingStatement As BoundStatement, ByRef lazyConditionalBranchLocal As LocalSymbol) As BoundExpression
            Return AddConditionSequencePoint(condition, containingStatement.Syntax, lazyConditionalBranchLocal, wasGenerated:=containingStatement.WasCompilerGenerated, shareLocal:=True)
        End Function

        Private Function AddConditionSequencePoint(condition As BoundExpression,
                                                   synthesizedVariableSyntax As SyntaxNode,
                                                   ByRef lazyConditionalBranchLocal As LocalSymbol,
                                                   wasGenerated As Boolean,
                                                   shareLocal As Boolean) As BoundExpression
            If condition Is Nothing OrElse Not _compilationState.Compilation.Options.EnableEditAndContinue OrElse wasGenerated Then
                Return condition
            End If

            Dim conditionSyntax = condition.Syntax

            ' The local has to be associated with the syntax of the statement containing the condition since 
            ' EnC source mapping only operates on statements.
            If lazyConditionalBranchLocal Is Nothing Then
                lazyConditionalBranchLocal = New SynthesizedLocal(_currentMethodOrLambda, condition.Type, SynthesizedLocalKind.ConditionalBranchDiscriminator, synthesizedVariableSyntax)
            Else
                Debug.Assert(lazyConditionalBranchLocal.SynthesizedKind = SynthesizedLocalKind.ConditionalBranchDiscriminator)
                Debug.Assert(lazyConditionalBranchLocal.Type Is condition.Type)
            End If

            ' Add hidden sequence point unless the condition is a constant expression.
            ' Constant expression must stay a const to not invalidate results of control flow analysis.
            Dim valueExpression = If(condition.ConstantValueOpt Is Nothing,
                                     New BoundSequencePointExpression(Nothing, MakeLocalRead(conditionSyntax, lazyConditionalBranchLocal), condition.Type),
                                     condition)

            Return New BoundSequence(
                conditionSyntax,
                If(shareLocal, ImmutableArray(Of LocalSymbol).Empty, ImmutableArray.Create(lazyConditionalBranchLocal)),
                ImmutableArray.Create(MakeAssignmentExpression(conditionSyntax, MakeLocalWrite(conditionSyntax, lazyConditionalBranchLocal), condition)),
                valueExpression,
                condition.Type)
        End Function

        Friend Function AddConditionSequencePoint(condition As BoundExpression,
                                                  conditionalBranchLocal As LocalSymbol) As BoundExpression
            Debug.Assert(condition IsNot Nothing AndAlso
                         _compilationState.Compilation.Options.EnableEditAndContinue)

            Debug.Assert(conditionalBranchLocal.SynthesizedKind = SynthesizedLocalKind.ConditionalBranchDiscriminator)
            Dim conditionSyntax = condition.Syntax

            ' Add hidden sequence point unless the condition is a constant expression.
            ' Constant expression must stay a const to not invalidate results of control flow analysis.
            Dim valueExpression = If(condition.ConstantValueOpt Is Nothing,
                                     New BoundSequencePointExpression(Nothing, MakeLocalRead(conditionSyntax, conditionalBranchLocal), condition.Type),
                                     condition)

            Return New BoundSequence(
                conditionSyntax,
                ImmutableArray(Of LocalSymbol).Empty,
                ImmutableArray.Create(MakeAssignmentExpression(conditionSyntax, MakeLocalWrite(conditionSyntax, conditionalBranchLocal), condition)),
                valueExpression,
                condition.Type)
        End Function

        Private Shared Function MakeLocalRead(syntax As VisualBasicSyntaxNode, localSym As LocalSymbol) As BoundLocal
            Dim boundNode = New BoundLocal(syntax, localSym, isLValue:=False, type:=localSym.Type)
            boundNode.SetWasCompilerGenerated()
            Return boundNode
        End Function

        Private Shared Function MakeLocalWrite(syntax As VisualBasicSyntaxNode, localSym As LocalSymbol) As BoundLocal
            Dim boundNode = New BoundLocal(syntax, localSym, isLValue:=True, type:=localSym.Type)
            boundNode.SetWasCompilerGenerated()
            Return boundNode
        End Function

        Private Shared Function MakeAssignmentExpression(syntax As VisualBasicSyntaxNode, left As BoundExpression, right As BoundExpression) As BoundExpression
            Debug.Assert(left.Type = right.Type)
            Dim boundNode = New BoundAssignmentOperator(syntax, left, right, suppressObjectClone:=True)
            boundNode.SetWasCompilerGenerated()
            Return boundNode
        End Function
    End Class
End Namespace
