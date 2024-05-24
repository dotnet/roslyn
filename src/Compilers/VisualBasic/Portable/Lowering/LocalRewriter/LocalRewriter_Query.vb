' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports System.Diagnostics
Imports System.Runtime.InteropServices
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports TypeKind = Microsoft.CodeAnalysis.TypeKind

Namespace Microsoft.CodeAnalysis.VisualBasic
    Partial Friend Class LocalRewriter

        Public Overrides Function VisitQueryExpression(node As BoundQueryExpression) As BoundNode
            Return Visit(node.LastOperator)
        End Function

        Public Overrides Function VisitQueryClause(node As BoundQueryClause) As BoundNode
            Return Visit(node.UnderlyingExpression)
        End Function

        Public Overrides Function VisitOrdering(node As BoundOrdering) As BoundNode
            Return Visit(node.UnderlyingExpression)
        End Function

        Public Overrides Function VisitRangeVariableAssignment(node As BoundRangeVariableAssignment) As BoundNode
            Return Visit(node.Value)
        End Function

        Public Overrides Function VisitGroupAggregation(node As BoundGroupAggregation) As BoundNode
            Return Visit(node.Group)
        End Function

        Public Overrides Function VisitQueryLambda(node As BoundQueryLambda) As BoundNode
            ' query expression should be rewritten in the context of corresponding lambda.
            ' since everything in the expression will end up in the body of that lambda.
            ' Conveniently, we already know the lambda's symbol.

            ' BEGIN LAMBDA REWRITE
            Dim originalMethodOrLambda = Me._currentMethodOrLambda
            Me._currentMethodOrLambda = node.LambdaSymbol

            PopulateRangeVariableMapForQueryLambdaRewrite(node, _rangeVariableMap, _inExpressionLambda)

            Dim save_createSequencePointsForTopLevelNonCompilerGeneratedExpressions = _instrumentTopLevelNonCompilerGeneratedExpressionsInQuery
            Dim synthesizedKind As SynthesizedLambdaKind = node.LambdaSymbol.SynthesizedKind
            Dim instrumentQueryLambdaBody As Boolean = synthesizedKind = SynthesizedLambdaKind.AggregateQueryLambda OrElse
                                                       synthesizedKind = SynthesizedLambdaKind.LetVariableQueryLambda

            _instrumentTopLevelNonCompilerGeneratedExpressionsInQuery = Not instrumentQueryLambdaBody

            Dim rewrittenBody As BoundExpression = VisitExpressionNode(node.Expression)
            Dim returnstmt = CreateReturnStatementForQueryLambdaBody(rewrittenBody, node)

            If instrumentQueryLambdaBody AndAlso Instrument Then
                returnstmt = _instrumenterOpt.InstrumentQueryLambdaBody(node, returnstmt)
            End If

            RemoveRangeVariables(node, _rangeVariableMap)

            _instrumentTopLevelNonCompilerGeneratedExpressionsInQuery = save_createSequencePointsForTopLevelNonCompilerGeneratedExpressions

            Me._hasLambdas = True

            Dim result As BoundLambda = RewriteQueryLambda(returnstmt, node)

            ' Done with lambda body rewrite, restore current lambda.
            ' END LAMBDA REWRITE
            Me._currentMethodOrLambda = originalMethodOrLambda

            Return result
        End Function

        Friend Shared Sub PopulateRangeVariableMapForQueryLambdaRewrite(
            node As BoundQueryLambda,
            ByRef rangeVariableMap As Dictionary(Of RangeVariableSymbol, BoundExpression),
            inExpressionLambda As Boolean)

            Dim nodeRangeVariables As ImmutableArray(Of RangeVariableSymbol) = node.RangeVariables

            If nodeRangeVariables.Length > 0 Then
                If rangeVariableMap Is Nothing Then
                    rangeVariableMap = New Dictionary(Of RangeVariableSymbol, BoundExpression)()
                End If

                Dim firstUnmappedRangeVariable As Integer = 0

                For Each parameter As ParameterSymbol In node.LambdaSymbol.Parameters
                    Dim parameterName As String = parameter.Name
                    Dim isReservedName As Boolean = parameterName.StartsWith("$"c, StringComparison.Ordinal)

                    If isReservedName AndAlso String.Equals(parameterName, GeneratedNameConstants.ItAnonymous, StringComparison.Ordinal) Then
                        ' This parameter represents "nameless" range variable, there are no references to it.
                        Continue For
                    End If

                    Dim paramRef As New BoundParameter(node.Syntax,
                                                       parameter,
                                                       False,
                                                       parameter.Type)

                    If isReservedName AndAlso IsCompoundVariableName(parameterName) Then
                        If parameter.Type.IsErrorType() Then
                            ' Skip adding variables to the range variable map and bail out for error case.
                            Return
                        Else
                            ' Compound variable.
                            ' Each range variable is an Anonymous Type property.
                            PopulateRangeVariableMapForAnonymousType(node.Syntax, paramRef.MakeCompilerGenerated(), nodeRangeVariables, firstUnmappedRangeVariable, rangeVariableMap, inExpressionLambda)
                        End If
                    Else
                        ' Simple case, range variable is a lambda parameter.
                        Debug.Assert(IdentifierComparison.Equals(parameterName, nodeRangeVariables(firstUnmappedRangeVariable).Name))
                        rangeVariableMap.Add(nodeRangeVariables(firstUnmappedRangeVariable), paramRef)
                        firstUnmappedRangeVariable += 1
                    End If
                Next

                Debug.Assert(firstUnmappedRangeVariable = nodeRangeVariables.Length)
            End If
        End Sub

        Private Shared Sub PopulateRangeVariableMapForAnonymousType(
            syntax As SyntaxNode,
            anonymousTypeInstance As BoundExpression,
            rangeVariables As ImmutableArray(Of RangeVariableSymbol),
            ByRef firstUnmappedRangeVariable As Integer,
            rangeVariableMap As Dictionary(Of RangeVariableSymbol, BoundExpression),
            inExpressionLambda As Boolean)

            Dim anonymousType = DirectCast(anonymousTypeInstance.Type, AnonymousTypeManager.AnonymousTypePublicSymbol)

            For Each propertyDef As PropertySymbol In anonymousType.Properties
                Dim getCallOrPropertyAccess As BoundExpression = Nothing
                If inExpressionLambda Then
                    ' NOTE: If we are in context of a lambda to be converted to an expression tree we need to use PropertyAccess.
                    getCallOrPropertyAccess = New BoundPropertyAccess(syntax,
                                                                      propertyDef,
                                                                      propertyGroupOpt:=Nothing,
                                                                      PropertyAccessKind.Get,
                                                                      isWriteable:=False,
                                                                      isLValue:=False,
                                                                      receiverOpt:=anonymousTypeInstance,
                                                                      arguments:=ImmutableArray(Of BoundExpression).Empty,
                                                                      defaultArguments:=BitVector.Null,
                                                                      type:=propertyDef.Type)
                Else
                    Dim getter = propertyDef.GetMethod
                    getCallOrPropertyAccess = New BoundCall(syntax,
                                                            getter,
                                                            Nothing,
                                                            anonymousTypeInstance,
                                                            ImmutableArray(Of BoundExpression).Empty,
                                                            Nothing,
                                                            getter.ReturnType)
                End If

                Dim propertyDefName As String = propertyDef.Name

                If propertyDefName.StartsWith("$"c, StringComparison.Ordinal) AndAlso
                   IsCompoundVariableName(propertyDefName) Then
                    ' Nested compound variable.
                    PopulateRangeVariableMapForAnonymousType(syntax, getCallOrPropertyAccess.MakeCompilerGenerated(), rangeVariables, firstUnmappedRangeVariable, rangeVariableMap, inExpressionLambda)
                Else
                    Debug.Assert(IdentifierComparison.Equals(propertyDefName, rangeVariables(firstUnmappedRangeVariable).Name))
                    rangeVariableMap.Add(rangeVariables(firstUnmappedRangeVariable), getCallOrPropertyAccess)
                    firstUnmappedRangeVariable += 1
                End If
            Next
        End Sub

        Private Shared Function IsCompoundVariableName(name As String) As Boolean
            Return name.Equals(GeneratedNameConstants.It, StringComparison.Ordinal) OrElse
                   name.Equals(GeneratedNameConstants.It1, StringComparison.Ordinal) OrElse
                   name.Equals(GeneratedNameConstants.It2, StringComparison.Ordinal)
        End Function

        Friend Shared Function CreateReturnStatementForQueryLambdaBody(
            rewrittenBody As BoundExpression,
            originalNode As BoundQueryLambda,
            Optional hasErrors As Boolean = False) As BoundStatement

            Return New BoundReturnStatement(originalNode.Syntax,
                                            rewrittenBody,
                                            Nothing,
                                            Nothing,
                                            hasErrors).MakeCompilerGenerated()
        End Function

        Friend Shared Sub RemoveRangeVariables(originalNode As BoundQueryLambda, rangeVariableMap As Dictionary(Of RangeVariableSymbol, BoundExpression))
            For Each rangeVar As RangeVariableSymbol In originalNode.RangeVariables
                rangeVariableMap.Remove(rangeVar)
            Next
        End Sub

        Friend Shared Function RewriteQueryLambda(rewrittenBody As BoundStatement, originalNode As BoundQueryLambda) As BoundLambda
            Dim lambdaBody = New BoundBlock(originalNode.Syntax,
                                            Nothing,
                                            ImmutableArray(Of LocalSymbol).Empty,
                                            ImmutableArray.Create(rewrittenBody)).MakeCompilerGenerated()

            Dim result As BoundLambda = New BoundLambda(originalNode.Syntax,
                                   originalNode.LambdaSymbol,
                                   lambdaBody,
                                   ReadOnlyBindingDiagnostic(Of AssemblySymbol).Empty,
                                   Nothing,
                                   ConversionKind.DelegateRelaxationLevelNone,
                                   MethodConversionKind.Identity)

            result.MakeCompilerGenerated()

            Return result
        End Function

        Public Overrides Function VisitRangeVariable(node As BoundRangeVariable) As BoundNode
            Return _rangeVariableMap(node.RangeVariable)
        End Function

        Public Overrides Function VisitQueryableSource(node As BoundQueryableSource) As BoundNode
            Return Visit(node.Source)
        End Function

        Public Overrides Function VisitQuerySource(node As BoundQuerySource) As BoundNode
            Return Visit(node.Expression)
        End Function

        Public Overrides Function VisitToQueryableCollectionConversion(node As BoundToQueryableCollectionConversion) As BoundNode
            Return Visit(node.ConversionCall)
        End Function

        Public Overrides Function VisitAggregateClause(node As BoundAggregateClause) As BoundNode
            If node.CapturedGroupOpt IsNot Nothing Then
                Debug.Assert(node.GroupPlaceholderOpt IsNot Nothing)
                Dim groupLocal = New SynthesizedLocal(Me._currentMethodOrLambda, node.CapturedGroupOpt.Type, SynthesizedLocalKind.LoweringTemp)

                AddPlaceholderReplacement(node.GroupPlaceholderOpt,
                                              New BoundLocal(node.Syntax, groupLocal, False, groupLocal.Type))

                Dim result = New BoundSequence(node.Syntax,
                                                               ImmutableArray.Create(Of LocalSymbol)(groupLocal),
                                                               ImmutableArray.Create(Of BoundExpression)(
                                                                   New BoundAssignmentOperator(node.Syntax,
                                                                                               New BoundLocal(node.Syntax, groupLocal, True, groupLocal.Type),
                                                                                               VisitExpressionNode(node.CapturedGroupOpt),
                                                                                               True,
                                                                                               groupLocal.Type)),
                                                                VisitExpressionNode(node.UnderlyingExpression),
                                                                node.Type)

                RemovePlaceholderReplacement(node.GroupPlaceholderOpt)

                Return result
            End If

            Return Visit(node.UnderlyingExpression)
        End Function
    End Class

End Namespace
