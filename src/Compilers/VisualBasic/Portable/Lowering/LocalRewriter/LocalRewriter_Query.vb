' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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

            Dim nodeRangeVariables As ImmutableArray(Of RangeVariableSymbol) = node.RangeVariables

            If nodeRangeVariables.Length > 0 Then
                If _rangeVariableMap Is Nothing Then
                    _rangeVariableMap = New Dictionary(Of RangeVariableSymbol, BoundExpression)()
                End If

                Dim firstUnmappedRangeVariable As Integer = 0

                For Each parameter As ParameterSymbol In node.LambdaSymbol.Parameters
                    Dim parameterName As String = parameter.Name
                    Dim isReservedName As Boolean = parameterName.StartsWith("$"c, StringComparison.Ordinal)

                    If isReservedName AndAlso String.Equals(parameterName, StringConstants.ItAnonymous, StringComparison.Ordinal) Then
                        ' This parameter represents "nameless" range variable, there are no references to it.
                        Continue For
                    End If

                    Dim paramRef As New BoundParameter(node.Syntax,
                                                       parameter,
                                                       False,
                                                       parameter.Type)

                    If isReservedName AndAlso Not String.Equals(parameterName, StringConstants.Group, StringComparison.Ordinal) Then
                        ' Compound variable.
                        ' Each range variable is an Anonymous Type property.
                        Debug.Assert(parameterName.Equals(StringConstants.It) OrElse parameterName.Equals(StringConstants.It1) OrElse parameterName.Equals(StringConstants.It2))
                        PopulateRangeVariableMapForAnonymousType(node.Syntax, paramRef, nodeRangeVariables, firstUnmappedRangeVariable)
                    Else
                        ' Simple case, range variable is a lambda parameter.
                        Debug.Assert(IdentifierComparison.Equals(parameterName, nodeRangeVariables(firstUnmappedRangeVariable).Name))
                        _rangeVariableMap.Add(nodeRangeVariables(firstUnmappedRangeVariable), paramRef)
                        firstUnmappedRangeVariable += 1
                    End If
                Next

                Debug.Assert(firstUnmappedRangeVariable = nodeRangeVariables.Length)
            End If

            Dim save_createSequencePointsForTopLevelNonCompilerGeneratedExpressions = _instrumentTopLevelNonCompilerGeneratedExpressionsInQuery
            Dim synthesizedKind As SynthesizedLambdaKind = node.LambdaSymbol.SynthesizedKind
            Dim instrumentQueryLambdaBody As Boolean = synthesizedKind = SynthesizedLambdaKind.AggregateQueryLambda OrElse
                                                       synthesizedKind = SynthesizedLambdaKind.LetVariableQueryLambda

            _instrumentTopLevelNonCompilerGeneratedExpressionsInQuery = Not instrumentQueryLambdaBody

            Dim returnstmt As BoundStatement = New BoundReturnStatement(node.Syntax,
                                                                        VisitExpressionNode(node.Expression),
                                                                        Nothing,
                                                                        Nothing)

            If instrumentQueryLambdaBody AndAlso Instrument Then
                returnstmt = _instrumenter.InstrumentQueryLambdaBody(node, returnstmt)
            End If

            _instrumentTopLevelNonCompilerGeneratedExpressionsInQuery = save_createSequencePointsForTopLevelNonCompilerGeneratedExpressions

            For Each rangeVar As RangeVariableSymbol In nodeRangeVariables
                _rangeVariableMap.Remove(rangeVar)
            Next

            Dim lambdaBody = New BoundBlock(node.Syntax,
                                            Nothing,
                                            ImmutableArray(Of LocalSymbol).Empty,
                                            ImmutableArray.Create(returnstmt))

            Me._hasLambdas = True

            Dim result As BoundLambda = New BoundLambda(node.Syntax,
                                   node.LambdaSymbol,
                                   lambdaBody,
                                   ImmutableArray(Of Diagnostic).Empty,
                                   Nothing,
                                   ConversionKind.DelegateRelaxationLevelNone,
                                   MethodConversionKind.Identity)

            result.MakeCompilerGenerated()

            ' Done with lambda body rewrite, restore current lambda.
            ' END LAMBDA REWRITE
            Me._currentMethodOrLambda = originalMethodOrLambda

            Return result
        End Function

        Private Sub PopulateRangeVariableMapForAnonymousType(
            syntax As VisualBasicSyntaxNode,
            anonymousTypeInstance As BoundExpression,
            rangeVariables As ImmutableArray(Of RangeVariableSymbol),
            ByRef firstUnmappedRangeVariable As Integer
        )
            Dim anonymousType = DirectCast(anonymousTypeInstance.Type, AnonymousTypeManager.AnonymousTypePublicSymbol)

            For Each propertyDef As PropertySymbol In anonymousType.Properties
                Dim getCallOrPropertyAccess As BoundExpression = Nothing
                If _inExpressionLambda Then
                    ' NOTE: If we are in context of a lambda to be converted to an expression tree we need to use PropertyAccess.
                    getCallOrPropertyAccess = New BoundPropertyAccess(syntax,
                                                                      propertyDef,
                                                                      Nothing,
                                                                      PropertyAccessKind.Get,
                                                                      False,
                                                                      anonymousTypeInstance,
                                                                      ImmutableArray(Of BoundExpression).Empty,
                                                                      propertyDef.Type)
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

                If propertyDefName.StartsWith("$"c, StringComparison.Ordinal) AndAlso Not String.Equals(propertyDefName, StringConstants.Group, StringComparison.Ordinal) Then
                    ' Nested compound variable.
                    Debug.Assert(propertyDefName.Equals(StringConstants.It) OrElse propertyDefName.Equals(StringConstants.It1) OrElse propertyDefName.Equals(StringConstants.It2))
                    PopulateRangeVariableMapForAnonymousType(syntax, getCallOrPropertyAccess, rangeVariables, firstUnmappedRangeVariable)

                Else
                    Debug.Assert(IdentifierComparison.Equals(propertyDefName, rangeVariables(firstUnmappedRangeVariable).Name))
                    _rangeVariableMap.Add(rangeVariables(firstUnmappedRangeVariable), getCallOrPropertyAccess)
                    firstUnmappedRangeVariable += 1
                End If
            Next
        End Sub

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
