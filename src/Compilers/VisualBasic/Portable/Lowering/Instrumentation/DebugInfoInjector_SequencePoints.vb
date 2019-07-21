' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic
    Partial Friend Class DebugInfoInjector
        Friend Shared Function AddConditionSequencePoint(condition As BoundExpression, containingCatchWithFilter As BoundCatchBlock, currentMethodOrLambda As MethodSymbol) As BoundExpression
            Debug.Assert(containingCatchWithFilter.ExceptionFilterOpt.Syntax.Parent.IsKind(SyntaxKind.CatchFilterClause))
            Dim local As LocalSymbol = Nothing
            Return AddConditionSequencePoint(condition, containingCatchWithFilter.ExceptionFilterOpt.Syntax.Parent, currentMethodOrLambda, local, shareLocal:=False)
        End Function

        Friend Shared Function AddConditionSequencePoint(condition As BoundExpression, containingStatement As BoundStatement, currentMethodOrLambda As MethodSymbol) As BoundExpression
            Dim local As LocalSymbol = Nothing
            Return AddConditionSequencePoint(condition, containingStatement.Syntax, currentMethodOrLambda, local, shareLocal:=False)
        End Function

        Friend Shared Function AddConditionSequencePoint(
            condition As BoundExpression,
            containingStatement As BoundStatement,
            currentMethodOrLambda As MethodSymbol,
            ByRef lazyConditionalBranchLocal As LocalSymbol
        ) As BoundExpression
            Return AddConditionSequencePoint(condition, containingStatement.Syntax, currentMethodOrLambda, lazyConditionalBranchLocal, shareLocal:=True)
        End Function

        Private Shared Function AddConditionSequencePoint(condition As BoundExpression,
                                                   synthesizedVariableSyntax As SyntaxNode,
                                                   currentMethodOrLambda As MethodSymbol,
                                                   ByRef lazyConditionalBranchLocal As LocalSymbol,
                                                   shareLocal As Boolean) As BoundExpression
            If Not currentMethodOrLambda.DeclaringCompilation.Options.EnableEditAndContinue Then
                Return condition
            End If

            Dim conditionSyntax = condition.Syntax

            ' The local has to be associated with the syntax of the statement containing the condition since 
            ' EnC source mapping only operates on statements.
            If lazyConditionalBranchLocal Is Nothing Then
                lazyConditionalBranchLocal = New SynthesizedLocal(currentMethodOrLambda, condition.Type, SynthesizedLocalKind.ConditionalBranchDiscriminator, synthesizedVariableSyntax)
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

        Private Shared Function MakeLocalRead(syntax As SyntaxNode, localSym As LocalSymbol) As BoundLocal
            Dim boundNode = New BoundLocal(syntax, localSym, isLValue:=False, type:=localSym.Type)
            boundNode.SetWasCompilerGenerated()
            Return boundNode
        End Function

        Private Shared Function MakeLocalWrite(syntax As SyntaxNode, localSym As LocalSymbol) As BoundLocal
            Dim boundNode = New BoundLocal(syntax, localSym, isLValue:=True, type:=localSym.Type)
            boundNode.SetWasCompilerGenerated()
            Return boundNode
        End Function

        Private Shared Function MakeAssignmentExpression(syntax As SyntaxNode, left As BoundExpression, right As BoundExpression) As BoundExpression
            Debug.Assert(TypeSymbol.Equals(left.Type, right.Type, TypeCompareKind.ConsiderEverything))
            Dim boundNode = New BoundAssignmentOperator(syntax, left, right, suppressObjectClone:=True)
            boundNode.SetWasCompilerGenerated()
            Return boundNode
        End Function

        Public Overloads Shared Function CreateBlockPrologue(node As BoundBlock, previousPrologue As BoundStatement) As BoundStatement
            ' method block needs to get a sequence point inside the method scope, 
            ' but before starting any statements
            Dim asMethod = TryCast(node.Syntax, MethodBlockBaseSyntax)
            If asMethod IsNot Nothing Then
                Dim methodStatement As MethodBaseSyntax = asMethod.BlockStatement

                ' For methods we want the span of the statement without any leading attributes.
                ' The span begins at the first modifier or the 'Sub'/'Function' keyword.
                ' There is no need to do this adjustment for lambdas because they cannot
                ' have attributes.

                Dim firstModifierOrKeyword As SyntaxToken

                If methodStatement.Modifiers.Count > 0 Then
                    firstModifierOrKeyword = methodStatement.Modifiers(0)
                Else
                    firstModifierOrKeyword = methodStatement.DeclarationKeyword
                End If

                Dim statementSpanWithoutAttributes = TextSpan.FromBounds(firstModifierOrKeyword.SpanStart, methodStatement.Span.End)

                previousPrologue = New BoundSequencePointWithSpan(methodStatement, previousPrologue, statementSpanWithoutAttributes)
            Else
                Dim asLambda = TryCast(node.Syntax, LambdaExpressionSyntax)
                If asLambda IsNot Nothing Then
                    previousPrologue = New BoundSequencePoint(asLambda.SubOrFunctionHeader, previousPrologue)
                End If
            End If

            Return previousPrologue
        End Function

        Private Shared Function MarkInitializerSequencePoint(rewrittenStatement As BoundStatement, syntax As SyntaxNode, nameIndex As Integer) As BoundStatement
            If syntax.Parent.IsKind(SyntaxKind.PropertyStatement) Then
                ' Property [|P As Integer = 1|] Implements I.P
                ' Property [|P As New Integer|] Implements I.P
                Dim propertyStatement = DirectCast(syntax.Parent, PropertyStatementSyntax)

                Dim span = TextSpan.FromBounds(propertyStatement.Identifier.SpanStart,
                                               If(propertyStatement.Initializer Is Nothing, propertyStatement.AsClause.Span.End, propertyStatement.Initializer.Span.End))

                Return New BoundSequencePointWithSpan(syntax, rewrittenStatement, span)
            End If

            If syntax.IsKind(SyntaxKind.AsNewClause) Then
                Dim declarator = DirectCast(syntax.Parent, VariableDeclaratorSyntax)
                If declarator.Names.Count > 1 Then
                    ' Dim [|a|], b As New C()
                    Return New BoundSequencePoint(declarator.Names(nameIndex), rewrittenStatement)
                Else
                    ' Dim [|a As New C()|]
                    Return New BoundSequencePoint(syntax.Parent, rewrittenStatement)
                End If
            End If

            If syntax.IsKind(SyntaxKind.ModifiedIdentifier) Then
                Debug.Assert(DirectCast(syntax, ModifiedIdentifierSyntax).ArrayBounds IsNot Nothing)
                ' Dim [|a(1)|] As Integer
                Return New BoundSequencePoint(syntax, rewrittenStatement)
            End If

            ' Dim [|a = 1|]
            Debug.Assert(syntax.IsKind(SyntaxKind.EqualsValue))
            Return New BoundSequencePoint(syntax.Parent, rewrittenStatement)
        End Function

        Private Shared Function MarkInitializerSequencePoint(rewrittenStatement As BoundStatement, syntax As SyntaxNode) As BoundStatement
            Debug.Assert(syntax.IsKind(SyntaxKind.ModifiedIdentifier))
            Debug.Assert(syntax.Parent.Kind = SyntaxKind.VariableDeclarator)

            Dim modifiedIdentifier = DirectCast(syntax, ModifiedIdentifierSyntax)
            If modifiedIdentifier.ArrayBounds IsNot Nothing Then
                ' Dim [|a(1)|], b(1) As Integer
                Return New BoundSequencePoint(syntax, rewrittenStatement)
            End If

            Dim declarator = DirectCast(syntax.Parent, VariableDeclaratorSyntax)
            If declarator.Names.Count > 1 Then
                Debug.Assert(declarator.AsClause.IsKind(SyntaxKind.AsNewClause))

                ' Dim [|a|], b As New C()
                Return New BoundSequencePoint(syntax, rewrittenStatement)
            End If

            ' Dim [|a = 1|]
            ' Dim [|a As New C()|]
            Return New BoundSequencePoint(declarator, rewrittenStatement)
        End Function
    End Class
End Namespace
