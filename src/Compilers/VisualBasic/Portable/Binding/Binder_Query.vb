' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable
Imports System.Runtime.InteropServices
Imports Microsoft.CodeAnalysis.Collections
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic

    Partial Friend Class Binder

        Private Function CreateQueryLambdaSymbol(syntaxNode As VisualBasicSyntaxNode,
                                                 kind As SynthesizedLambdaKind,
                                                 parameters As ImmutableArray(Of BoundLambdaParameterSymbol)) As SynthesizedLambdaSymbol

            Debug.Assert(kind.IsQueryLambda)

            Return New SynthesizedLambdaSymbol(kind,
                                               syntaxNode,
                                               parameters,
                                               LambdaSymbol.ReturnTypePendingDelegate,
                                               Me)
        End Function

        Private Shared Function CreateBoundQueryLambda(queryLambdaSymbol As SynthesizedLambdaSymbol,
                                                       rangeVariables As ImmutableArray(Of RangeVariableSymbol),
                                                       expression As BoundExpression,
                                                       exprIsOperandOfConditionalBranch As Boolean) As BoundQueryLambda
            Return New BoundQueryLambda(queryLambdaSymbol.Syntax, queryLambdaSymbol, rangeVariables, expression, exprIsOperandOfConditionalBranch)
        End Function

        Friend Overridable Function BindGroupAggregationExpression(group As GroupAggregationSyntax, diagnostics As DiagnosticBag) As BoundExpression
            ' Only special query binders that have enough context can bind GroupAggregationSyntax.
            ' TODO: Do we need to report any diagnostic?
            Debug.Assert(False, "Binding out of context is unsupported!")
            Return BadExpression(group, ErrorTypeSymbol.UnknownResultType)
        End Function

        Friend Overridable Function BindFunctionAggregationExpression([function] As FunctionAggregationSyntax, diagnostics As DiagnosticBag) As BoundExpression
            ' Only special query binders that have enough context can bind FunctionAggregationSyntax.
            ' TODO: Do we need to report any diagnostic?
            Debug.Assert(False, "Binding out of context is unsupported!")
            Return BadExpression([function], ErrorTypeSymbol.UnknownResultType)
        End Function

        ''' <summary>
        ''' Bind a Query Expression.
        ''' This is the entry point.
        ''' </summary>
        Private Function BindQueryExpression(
            query As QueryExpressionSyntax,
            diagnostics As DiagnosticBag
        ) As BoundExpression

            If query.Clauses.Count < 1 Then
                ' Syntax error must have been reported
                Return BadExpression(query, ErrorTypeSymbol.UnknownResultType)
            End If

            Dim operators As SyntaxList(Of QueryClauseSyntax).Enumerator = query.Clauses.GetEnumerator()
            Dim moveResult = operators.MoveNext()
            Debug.Assert(moveResult)

            Dim current As QueryClauseSyntax = operators.Current

            Select Case current.Kind
                Case SyntaxKind.FromClause
                    Return BindFromQueryExpression(query, operators, diagnostics)

                Case SyntaxKind.AggregateClause
                    Return BindAggregateQueryExpression(query, operators, diagnostics)

                Case Else
                    ' Syntax error must have been reported
                    Return BadExpression(query, ErrorTypeSymbol.UnknownResultType)
            End Select
        End Function

        ''' <summary>
        ''' Given a result of binding of initial set of collection range variables, the source,
        ''' bind the rest of the operators in the enumerator.
        ''' 
        ''' There is a special method to bind an operator of each kind, the common thing among them is that
        ''' all of them take the result we have so far, the source, and return result of an application 
        ''' of one or two following operators. 
        ''' Some of the methods also take operators enumerator in order to be able to do a necessary look-ahead
        ''' and in some cases even to advance the enumerator themselves.
        ''' Join and From operators absorb following Select or Let, that is when the process of binding of 
        ''' a single operator actually handles two and advances the enumerator. 
        ''' </summary>
        Private Function BindSubsequentQueryOperators(
            source As BoundQueryClauseBase,
            operators As SyntaxList(Of QueryClauseSyntax).Enumerator,
            diagnostics As DiagnosticBag
        ) As BoundQueryClauseBase
            Debug.Assert(source IsNot Nothing)

            While operators.MoveNext()
                Dim current As QueryClauseSyntax = operators.Current

                Select Case current.Kind
                    Case SyntaxKind.FromClause
                        ' Note, this call can advance [operators] enumerator if it absorbs the following Let or Select.
                        source = BindFromClause(source, DirectCast(current, FromClauseSyntax), operators, diagnostics)

                    Case SyntaxKind.SelectClause
                        source = BindSelectClause(source, DirectCast(current, SelectClauseSyntax), operators, diagnostics)

                    Case SyntaxKind.LetClause
                        source = BindLetClause(source, DirectCast(current, LetClauseSyntax), operators, diagnostics)

                    Case SyntaxKind.WhereClause
                        source = BindWhereClause(source, DirectCast(current, WhereClauseSyntax), diagnostics)

                    Case SyntaxKind.SkipWhileClause
                        source = BindSkipWhileClause(source, DirectCast(current, PartitionWhileClauseSyntax), diagnostics)

                    Case SyntaxKind.TakeWhileClause
                        source = BindTakeWhileClause(source, DirectCast(current, PartitionWhileClauseSyntax), diagnostics)

                    Case SyntaxKind.DistinctClause
                        source = BindDistinctClause(source, DirectCast(current, DistinctClauseSyntax), diagnostics)

                    Case SyntaxKind.SkipClause
                        source = BindSkipClause(source, DirectCast(current, PartitionClauseSyntax), diagnostics)

                    Case SyntaxKind.TakeClause
                        source = BindTakeClause(source, DirectCast(current, PartitionClauseSyntax), diagnostics)

                    Case SyntaxKind.OrderByClause
                        source = BindOrderByClause(source, DirectCast(current, OrderByClauseSyntax), diagnostics)

                    Case SyntaxKind.SimpleJoinClause
                        ' Note, this call can advance [operators] enumerator if it absorbs the following Let or Select.
                        source = BindInnerJoinClause(source, DirectCast(current, SimpleJoinClauseSyntax), Nothing, operators, diagnostics)

                    Case SyntaxKind.GroupJoinClause
                        source = BindGroupJoinClause(source, DirectCast(current, GroupJoinClauseSyntax), Nothing, operators, diagnostics)

                    Case SyntaxKind.GroupByClause
                        source = BindGroupByClause(source, DirectCast(current, GroupByClauseSyntax), diagnostics)

                    Case SyntaxKind.AggregateClause
                        source = BindAggregateClause(source, DirectCast(current, AggregateClauseSyntax), operators, diagnostics)

                    Case Else
                        Throw ExceptionUtilities.UnexpectedValue(current.Kind)
                End Select
            End While

            Return source
        End Function

        ''' <summary>
        ''' Bind query expression that starts with From keyword, as opposed to the one that starts with Aggregate.
        ''' 
        '''     From {collection range variables} [{other operators}]
        ''' </summary>
        Private Function BindFromQueryExpression(
            query As QueryExpressionSyntax,
            operators As SyntaxList(Of QueryClauseSyntax).Enumerator,
            diagnostics As DiagnosticBag
        ) As BoundQueryExpression
            ' Note, this call can advance [operators] enumerator if it absorbs the following Let or Select.
            Dim source As BoundQueryClauseBase = BindFromClause(Nothing, DirectCast(operators.Current, FromClauseSyntax), operators, diagnostics)

            source = BindSubsequentQueryOperators(source, operators, diagnostics)

            If Not source.Type.IsErrorType() AndAlso source.Kind = BoundKind.QueryableSource AndAlso
               DirectCast(source, BoundQueryableSource).Source.Kind = BoundKind.QuerySource Then
                ' Need to apply implicit Select.
                source = BindFinalImplicitSelectClause(source, diagnostics)
            End If

            Return New BoundQueryExpression(query, source, source.Type)

        End Function

        ''' <summary>
        ''' Bind query expression that starts with Aggregate keyword, as opposed to the one that starts with From.
        ''' 
        '''     Aggregate {collection range variables} [{other operators}] Into {aggregation range variables}
        ''' 
        ''' If Into clause has one item, a single value is produced. If it has multiple items, values are
        ''' combined into an instance of an Anonymous Type.
        ''' </summary>
        Private Function BindAggregateQueryExpression(
            query As QueryExpressionSyntax,
            operators As SyntaxList(Of QueryClauseSyntax).Enumerator,
            diagnostics As DiagnosticBag
        ) As BoundQueryExpression
            Dim aggregate = DirectCast(operators.Current, AggregateClauseSyntax)

            Dim malformedSyntax As Boolean = operators.MoveNext()

            Debug.Assert(Not malformedSyntax, "Malformed syntax tree. Parser shouldn't produce a tree like that.")

            operators = aggregate.AdditionalQueryOperators.GetEnumerator()

            ' Note, this call can advance [operators] enumerator if it absorbs the following Let or Select.
            Dim source As BoundQueryClauseBase = BindCollectionRangeVariables(aggregate, Nothing, aggregate.Variables, operators, diagnostics)
            source = BindSubsequentQueryOperators(source, operators, diagnostics)

            Dim aggregationVariables As SeparatedSyntaxList(Of AggregationRangeVariableSyntax) = aggregate.AggregationVariables
            Dim aggregationVariablesCount As Integer = aggregationVariables.Count

            Select Case aggregationVariablesCount
                Case 0
                    Debug.Assert(aggregationVariables.Count > 0, "Malformed syntax tree.")
                    Dim intoBinder As New IntoClauseDisallowGroupReferenceBinder(Me, source, source.RangeVariables, source.CompoundVariableType, source.RangeVariables)

                    source = New BoundAggregateClause(aggregate, Nothing, Nothing,
                                                      BadExpression(aggregate, source, ErrorTypeSymbol.UnknownResultType).MakeCompilerGenerated(),
                                                      ImmutableArray(Of RangeVariableSymbol).Empty,
                                                      ErrorTypeSymbol.UnknownResultType,
                                                      ImmutableArray.Create(Of Binder)(intoBinder),
                                                      ErrorTypeSymbol.UnknownResultType)

                Case 1
                    ' Simple case, one item in the [Into] clause, source is our group.
                    Dim intoBinder As New IntoClauseDisallowGroupReferenceBinder(Me, source, source.RangeVariables, source.CompoundVariableType, source.RangeVariables)

                    Dim aggregationSelector As BoundExpression = Nothing
                    intoBinder.BindAggregationRangeVariable(aggregationVariables(0),
                                                            Nothing,
                                                            aggregationSelector,
                                                            diagnostics)

                    source = New BoundAggregateClause(aggregate, Nothing, Nothing,
                                                      aggregationSelector,
                                                      ImmutableArray(Of RangeVariableSymbol).Empty,
                                                      aggregationSelector.Type,
                                                      ImmutableArray.Create(Of Binder)(intoBinder),
                                                      aggregationSelector.Type)

                Case Else
                    ' Complex case, need to build an instance of an Anonymous Type. 
                    Dim declaredNames As HashSet(Of String) = CreateSetOfDeclaredNames()

                    Dim selectors = New BoundExpression(aggregationVariablesCount - 1) {}
                    Dim fields = New AnonymousTypeField(selectors.Length - 1) {}

                    Dim groupReference = New BoundRValuePlaceholder(aggregate, source.Type).MakeCompilerGenerated()
                    Dim intoBinder As New IntoClauseDisallowGroupReferenceBinder(Me, groupReference, source.RangeVariables, source.CompoundVariableType, source.RangeVariables)

                    For i As Integer = 0 To aggregationVariablesCount - 1
                        Dim rangeVar As RangeVariableSymbol = intoBinder.BindAggregationRangeVariable(aggregationVariables(i),
                                                                                                      declaredNames, selectors(i),
                                                                                                      diagnostics)

                        Debug.Assert(rangeVar IsNot Nothing)
                        fields(i) = New AnonymousTypeField(rangeVar.Name, rangeVar.Type, rangeVar.Syntax.GetLocation(), isKeyOrByRef:=True)
                    Next

                    Dim result As BoundExpression = BindAnonymousObjectCreationExpression(aggregate,
                                                             New AnonymousTypeDescriptor(fields.AsImmutableOrNull(),
                                                                                         aggregate.IntoKeyword.GetLocation(),
                                                                                         True),
                                                             selectors.AsImmutableOrNull(),
                                                             diagnostics).MakeCompilerGenerated()

                    source = New BoundAggregateClause(aggregate, source, groupReference,
                                                      result,
                                                      ImmutableArray(Of RangeVariableSymbol).Empty,
                                                      result.Type,
                                                      ImmutableArray.Create(Of Binder)(intoBinder),
                                                      result.Type)
            End Select

            Debug.Assert(Not source.Binders.IsDefault AndAlso source.Binders.Length = 1 AndAlso source.Binders(0) IsNot Nothing)

            Return New BoundQueryExpression(query, source, If(malformedSyntax, ErrorTypeSymbol.UnknownResultType, source.Type), hasErrors:=malformedSyntax)
        End Function

        ''' <summary>
        ''' Given result of binding preceding query operators, the source, bind the following Aggregate operator.
        ''' 
        '''     {Preceding query operators} Aggregate {collection range variables} [{other operators}] Into {aggregation range variables}
        ''' 
        ''' Depending on how many items we have in the INTO clause,
        ''' we will interpret Aggregate operator as follows:
        '''
        ''' FROM a in AA              FROM a in AA
        ''' AGGREGATE b in a.BB  =>   LET count = (FROM b IN a.BB).Count()
        ''' INTO Count()
        '''
        ''' FROM a in AA              FROM a in AA
        ''' AGGREGATE b in a.BB  =>   LET Group = (FROM b IN a.BB)
        ''' INTO Count(),             Select a, Count=Group.Count(), Sum=Group.Sum(b=>b)
        '''      Sum(b)
        '''
        ''' </summary>
        Private Function BindAggregateClause(
            source As BoundQueryClauseBase,
            aggregate As AggregateClauseSyntax,
            operatorsEnumerator As SyntaxList(Of QueryClauseSyntax).Enumerator,
            diagnostics As DiagnosticBag
        ) As BoundAggregateClause
            Debug.Assert(operatorsEnumerator.Current Is aggregate)

            ' Let's interpret our group.
            ' Create LambdaSymbol for the shape of the Let-selector lambda.
            Dim letSelectorParam As BoundLambdaParameterSymbol = CreateQueryLambdaParameterSymbol(GetQueryLambdaParameterName(source.RangeVariables), 0,
                                                                                                  source.CompoundVariableType,
                                                                                                  aggregate, source.RangeVariables)

            ' The lambda that will contain the nested query.
            Dim letSelectorLambdaSymbol = Me.CreateQueryLambdaSymbol(LambdaUtilities.GetAggregateLambdaBody(aggregate),
                                                                     SynthesizedLambdaKind.AggregateQueryLambda,
                                                                     ImmutableArray.Create(letSelectorParam))

            ' Create binder for the [Let] selector.
            Dim letSelectorBinder As New QueryLambdaBinder(letSelectorLambdaSymbol, source.RangeVariables)

            Dim declaredRangeVariables As ImmutableArray(Of RangeVariableSymbol) = Nothing
            Dim group As BoundQueryClauseBase = Nothing
            Dim intoBinder As IntoClauseDisallowGroupReferenceBinder = Nothing
            Dim letSelector As BoundExpression = letSelectorBinder.BindAggregateClauseFirstSelector(aggregate, operatorsEnumerator,
                                                                                                    source.RangeVariables,
                                                                                                    ImmutableArray(Of RangeVariableSymbol).Empty,
                                                                                                    declaredRangeVariables,
                                                                                                    group,
                                                                                                    intoBinder,
                                                                                                    diagnostics)

            Dim letSelectorLambda As BoundQueryLambda

            letSelectorLambda = CreateBoundQueryLambda(letSelectorLambdaSymbol,
                                                       source.RangeVariables,
                                                       letSelector,
                                                       exprIsOperandOfConditionalBranch:=False)

            letSelectorLambdaSymbol.SetQueryLambdaReturnType(letSelector.Type)
            letSelectorLambda.SetWasCompilerGenerated()


            ' Now bind the [Let] operator call.
            Dim suppressDiagnostics As DiagnosticBag = Nothing
            Dim underlyingExpression As BoundExpression

            If source.Type.IsErrorType() Then
                underlyingExpression = BadExpression(aggregate, ImmutableArray.Create(Of BoundNode)(source, letSelectorLambda),
                                                         ErrorTypeSymbol.UnknownResultType).MakeCompilerGenerated()
            Else
                Dim callDiagnostics As DiagnosticBag = diagnostics

                If ShouldSuppressDiagnostics(letSelectorLambda) Then
                    ' Operator BindQueryClauseCall will fail, let's suppress any additional errors it will report.
                    Debug.Assert(suppressDiagnostics Is Nothing)
                    suppressDiagnostics = DiagnosticBag.GetInstance()
                    callDiagnostics = suppressDiagnostics
                End If

                underlyingExpression = BindQueryOperatorCall(aggregate, source,
                                                             StringConstants.SelectMethod,
                                                             ImmutableArray.Create(Of BoundExpression)(letSelectorLambda),
                                                             aggregate.AggregateKeyword.Span,
                                                             callDiagnostics)
            End If

            If suppressDiagnostics IsNot Nothing Then
                suppressDiagnostics.Free()
            End If

            Return CompleteAggregateClauseBinding(aggregate,
                                                  operatorsEnumerator,
                                                  source.RangeVariables,
                                                  ImmutableArray(Of RangeVariableSymbol).Empty,
                                                  underlyingExpression,
                                                  letSelectorBinder,
                                                  declaredRangeVariables,
                                                  letSelectorLambda.Expression.Type,
                                                  group,
                                                  intoBinder,
                                                  diagnostics)
        End Function

        Private Function CompleteAggregateClauseBinding(
            aggregate As AggregateClauseSyntax,
            operatorsEnumerator As SyntaxList(Of QueryClauseSyntax).Enumerator,
            sourceRangeVariablesPart1 As ImmutableArray(Of RangeVariableSymbol),
            sourceRangeVariablesPart2 As ImmutableArray(Of RangeVariableSymbol),
            firstSelectExpression As BoundExpression,
            firstSelectSelectorBinder As QueryLambdaBinder,
            firstSelectDeclaredRangeVariables As ImmutableArray(Of RangeVariableSymbol),
            firstSelectCompoundVariableType As TypeSymbol,
            group As BoundQueryClauseBase,
            intoBinder As IntoClauseDisallowGroupReferenceBinder,
            diagnostics As DiagnosticBag
        ) As BoundAggregateClause
            Debug.Assert((sourceRangeVariablesPart1.Length = 0) = (sourceRangeVariablesPart2 = firstSelectSelectorBinder.RangeVariables))
            Debug.Assert((sourceRangeVariablesPart2.Length = 0) = (sourceRangeVariablesPart1 = firstSelectSelectorBinder.RangeVariables))
            Debug.Assert(firstSelectSelectorBinder.RangeVariables.Length = sourceRangeVariablesPart1.Length + sourceRangeVariablesPart2.Length)

            Dim result As BoundAggregateClause
            Dim aggregationVariables As SeparatedSyntaxList(Of AggregationRangeVariableSyntax) = aggregate.AggregationVariables

            If aggregationVariables.Count <= 1 Then
                ' Simple case
                Debug.Assert(intoBinder IsNot Nothing)
                Debug.Assert(firstSelectDeclaredRangeVariables.Length <= 1)
                result = New BoundAggregateClause(aggregate, Nothing, Nothing,
                                                  firstSelectExpression,
                                                  firstSelectSelectorBinder.RangeVariables.Concat(firstSelectDeclaredRangeVariables),
                                                  firstSelectCompoundVariableType,
                                                  ImmutableArray.Create(Of Binder)(firstSelectSelectorBinder, intoBinder),
                                                  firstSelectExpression.Type)
            Else

                ' Complex case, apply the [Select].
                Debug.Assert(intoBinder Is Nothing)
                Debug.Assert(firstSelectDeclaredRangeVariables.Length = 1)

                Dim suppressCallDiagnostics As Boolean = (firstSelectExpression.Kind = BoundKind.BadExpression)

                If Not suppressCallDiagnostics AndAlso firstSelectExpression.HasErrors AndAlso firstSelectExpression.Kind = BoundKind.QueryClause Then
                    Dim query = DirectCast(firstSelectExpression, BoundQueryClause)
                    suppressCallDiagnostics = query.UnderlyingExpression.Kind = BoundKind.BadExpression
                End If

                Dim letOperator = New BoundQueryClause(aggregate,
                                                       firstSelectExpression,
                                                       firstSelectSelectorBinder.RangeVariables.Concat(firstSelectDeclaredRangeVariables),
                                                       firstSelectCompoundVariableType,
                                                       ImmutableArray(Of Binder).Empty,
                                                       firstSelectExpression.Type).MakeCompilerGenerated()

                Dim selectSelectorParam As BoundLambdaParameterSymbol = CreateQueryLambdaParameterSymbol(GetQueryLambdaParameterName(letOperator.RangeVariables), 0,
                                                                                                         letOperator.CompoundVariableType,
                                                                                                         aggregate, letOperator.RangeVariables)

                ' This lambda only contains non-user code. We associate it with the aggregate clause itself.
                Debug.Assert(LambdaUtilities.IsNonUserCodeQueryLambda(aggregate))
                Dim selectSelectorLambdaSymbol = Me.CreateQueryLambdaSymbol(aggregate,
                                                                            SynthesizedLambdaKind.AggregateNonUserCodeQueryLambda,
                                                                            ImmutableArray.Create(selectSelectorParam))

                ' Create new binder for the [Select] selector.
                Dim groupRangeVar As RangeVariableSymbol = firstSelectDeclaredRangeVariables(0)
                Dim selectSelectorBinder As New QueryLambdaBinder(selectSelectorLambdaSymbol, ImmutableArray(Of RangeVariableSymbol).Empty)
                Dim groupReference = New BoundRangeVariable(groupRangeVar.Syntax, groupRangeVar, groupRangeVar.Type).MakeCompilerGenerated()
                intoBinder = New IntoClauseDisallowGroupReferenceBinder(selectSelectorBinder,
                                                                        groupReference, group.RangeVariables, group.CompoundVariableType,
                                                                        firstSelectSelectorBinder.RangeVariables.Concat(group.RangeVariables))


                ' Compound range variable after the first [Let] has shape { [<compound key part1>, ][<compound key part2>, ]<group> }.
                Dim compoundKeyReferencePart1 As BoundExpression
                Dim keysRangeVariablesPart1 As ImmutableArray(Of RangeVariableSymbol)
                Dim compoundKeyReferencePart2 As BoundExpression
                Dim keysRangeVariablesPart2 As ImmutableArray(Of RangeVariableSymbol)

                If sourceRangeVariablesPart1.Length > 0 Then
                    ' So we need to get a reference to the first property of selector's parameter.
                    Dim anonymousType = DirectCast(selectSelectorParam.Type, AnonymousTypeManager.AnonymousTypePublicSymbol)
                    Dim keyProperty = anonymousType.Properties(0)

                    Debug.Assert(keyProperty.Type Is firstSelectSelectorBinder.LambdaSymbol.Parameters(0).Type)

                    compoundKeyReferencePart1 = New BoundPropertyAccess(aggregate,
                                                                        keyProperty,
                                                                        Nothing,
                                                                        PropertyAccessKind.Get,
                                                                        False,
                                                                        New BoundParameter(selectSelectorParam.Syntax,
                                                                                           selectSelectorParam, False,
                                                                                           selectSelectorParam.Type).MakeCompilerGenerated(),
                                                                        ImmutableArray(Of BoundExpression).Empty).MakeCompilerGenerated()

                    keysRangeVariablesPart1 = sourceRangeVariablesPart1

                    If sourceRangeVariablesPart2.Length > 0 Then
                        keyProperty = anonymousType.Properties(1)

                        Debug.Assert(keyProperty.Type Is firstSelectSelectorBinder.LambdaSymbol.Parameters(1).Type)

                        ' We need to get a reference to the second property of selector's parameter.
                        compoundKeyReferencePart2 = New BoundPropertyAccess(aggregate,
                                                                            keyProperty,
                                                                            Nothing,
                                                                            PropertyAccessKind.Get,
                                                                            False,
                                                                            New BoundParameter(selectSelectorParam.Syntax,
                                                                                               selectSelectorParam, False,
                                                                                               selectSelectorParam.Type).MakeCompilerGenerated(),
                                                                            ImmutableArray(Of BoundExpression).Empty).MakeCompilerGenerated()

                        keysRangeVariablesPart2 = sourceRangeVariablesPart2
                    Else
                        compoundKeyReferencePart2 = Nothing
                        keysRangeVariablesPart2 = ImmutableArray(Of RangeVariableSymbol).Empty
                    End If
                ElseIf sourceRangeVariablesPart2.Length > 0 Then
                    ' So we need to get a reference to the first property of selector's parameter.
                    Dim anonymousType = DirectCast(selectSelectorParam.Type, AnonymousTypeManager.AnonymousTypePublicSymbol)
                    Dim keyProperty = anonymousType.Properties(0)

                    Debug.Assert(keyProperty.Type Is firstSelectSelectorBinder.LambdaSymbol.Parameters(1).Type)

                    compoundKeyReferencePart1 = New BoundPropertyAccess(aggregate,
                                                                        keyProperty,
                                                                        Nothing,
                                                                        PropertyAccessKind.Get,
                                                                        False,
                                                                        New BoundParameter(selectSelectorParam.Syntax,
                                                                                           selectSelectorParam, False,
                                                                                           selectSelectorParam.Type).MakeCompilerGenerated(),
                                                                        ImmutableArray(Of BoundExpression).Empty).MakeCompilerGenerated()

                    keysRangeVariablesPart1 = sourceRangeVariablesPart2
                    compoundKeyReferencePart2 = Nothing
                    keysRangeVariablesPart2 = ImmutableArray(Of RangeVariableSymbol).Empty
                Else
                    compoundKeyReferencePart1 = Nothing
                    keysRangeVariablesPart1 = ImmutableArray(Of RangeVariableSymbol).Empty
                    compoundKeyReferencePart2 = Nothing
                    keysRangeVariablesPart2 = ImmutableArray(Of RangeVariableSymbol).Empty
                End If

                Dim declaredRangeVariables As ImmutableArray(Of RangeVariableSymbol) = Nothing
                Dim selectSelector As BoundExpression = intoBinder.BindIntoSelector(aggregate,
                                                                                    firstSelectSelectorBinder.RangeVariables,
                                                                                    compoundKeyReferencePart1,
                                                                                    keysRangeVariablesPart1,
                                                                                    compoundKeyReferencePart2,
                                                                                    keysRangeVariablesPart2,
                                                                                    Nothing,
                                                                                    aggregationVariables,
                                                                                    MustProduceFlatCompoundVariable(operatorsEnumerator),
                                                                                    declaredRangeVariables,
                                                                                    diagnostics)

                Dim selectSelectorLambda = CreateBoundQueryLambda(selectSelectorLambdaSymbol,
                                                                  letOperator.RangeVariables,
                                                                  selectSelector,
                                                                  exprIsOperandOfConditionalBranch:=False)

                selectSelectorLambdaSymbol.SetQueryLambdaReturnType(selectSelector.Type)
                selectSelectorLambda.SetWasCompilerGenerated()

                Dim underlyingExpression As BoundExpression
                Dim suppressDiagnostics As DiagnosticBag = Nothing

                If letOperator.Type.IsErrorType() Then
                    underlyingExpression = BadExpression(aggregate, ImmutableArray.Create(Of BoundNode)(letOperator, selectSelectorLambda),
                                                             ErrorTypeSymbol.UnknownResultType).MakeCompilerGenerated()
                Else
                    Dim callDiagnostics As DiagnosticBag = diagnostics

                    If suppressCallDiagnostics OrElse ShouldSuppressDiagnostics(selectSelectorLambda) Then
                        ' Operator BindQueryClauseCall will fail, let's suppress any additional errors it will report.
                        suppressDiagnostics = DiagnosticBag.GetInstance()
                        callDiagnostics = suppressDiagnostics
                    End If

                    underlyingExpression = BindQueryOperatorCall(aggregate, letOperator,
                                                                     StringConstants.SelectMethod,
                                                                     ImmutableArray.Create(Of BoundExpression)(selectSelectorLambda),
                                                                     aggregate.AggregateKeyword.Span,
                                                                     callDiagnostics)
                End If

                If suppressDiagnostics IsNot Nothing Then
                    suppressDiagnostics.Free()
                End If

                result = New BoundAggregateClause(aggregate, Nothing, Nothing,
                                                  underlyingExpression,
                                                  firstSelectSelectorBinder.RangeVariables.Concat(declaredRangeVariables),
                                                  selectSelectorLambda.Expression.Type,
                                                  ImmutableArray.Create(Of Binder)(firstSelectSelectorBinder, intoBinder),
                                                  underlyingExpression.Type)
            End If

            Debug.Assert(Not result.Binders.IsDefault AndAlso result.Binders.Length = 2 AndAlso
                         result.Binders(0) IsNot Nothing AndAlso result.Binders(1) IsNot Nothing)

            Return result
        End Function

        ''' <summary>
        ''' Apply implicit Select operator at the end of the query to 
        ''' ensure that at least one query operator is called.
        ''' 
        ''' Basically makes query like: 
        '''     From a In AA
        ''' into:
        '''     From a In AA Select a
        ''' </summary>
        Private Function BindFinalImplicitSelectClause(
            source As BoundQueryClauseBase,
            diagnostics As DiagnosticBag
        ) As BoundQueryClause
            Debug.Assert(Not source.Type.IsErrorType())

            Dim fromClauseSyntax = DirectCast(source.Syntax.Parent, FromClauseSyntax)

            ' Create LambdaSymbol for the shape of the selector.
            Dim param As BoundLambdaParameterSymbol = CreateQueryLambdaParameterSymbol(GetQueryLambdaParameterName(source.RangeVariables), 0,
                                                                                       source.CompoundVariableType,
                                                                                       fromClauseSyntax, source.RangeVariables)

            ' An implicit selector is an identity (x => x) function that doesn't contain any user code.
            LambdaUtilities.IsNonUserCodeQueryLambda(fromClauseSyntax)
            Dim lambdaSymbol = Me.CreateQueryLambdaSymbol(fromClauseSyntax,
                                                          SynthesizedLambdaKind.FromNonUserCodeQueryLambda,
                                                          ImmutableArray.Create(param))

            lambdaSymbol.SetQueryLambdaReturnType(source.CompoundVariableType)

            Dim selector As BoundExpression = New BoundParameter(param.Syntax,
                                                                 param,
                                                                 isLValue:=False,
                                                                 type:=param.Type).MakeCompilerGenerated()

            Debug.Assert(Not selector.HasErrors)

            Dim selectorLambda = CreateBoundQueryLambda(lambdaSymbol,
                                                        ImmutableArray(Of RangeVariableSymbol).Empty,
                                                        selector,
                                                        exprIsOperandOfConditionalBranch:=False)
            selectorLambda.SetWasCompilerGenerated()

            Debug.Assert(Not selectorLambda.HasErrors)

            Dim suppressDiagnostics As DiagnosticBag = Nothing

            If param.Type.IsErrorType() Then
                suppressDiagnostics = DiagnosticBag.GetInstance()
                diagnostics = suppressDiagnostics
            End If

            Dim boundCallOrBadExpression As BoundExpression
            boundCallOrBadExpression = BindQueryOperatorCall(source.Syntax.Parent, source,
                                                             StringConstants.SelectMethod,
                                                             ImmutableArray.Create(Of BoundExpression)(selectorLambda),
                                                             source.Syntax.Span,
                                                             diagnostics)

            Debug.Assert(boundCallOrBadExpression.WasCompilerGenerated)

            If suppressDiagnostics IsNot Nothing Then
                suppressDiagnostics.Free()
            End If

            Return New BoundQueryClause(source.Syntax.Parent,
                                        boundCallOrBadExpression,
                                        ImmutableArray(Of RangeVariableSymbol).Empty,
                                        source.CompoundVariableType,
                                        ImmutableArray(Of Binder).Empty,
                                        boundCallOrBadExpression.Type).MakeCompilerGenerated()
        End Function

        ''' <summary>
        ''' Given result of binding preceding query operators, the source, bind the following Select operator.
        ''' 
        '''     {Preceding query operators} Select {expression range variables}
        ''' 
        ''' From a In AA Select b  ==> AA.Select(Function(a) b)
        ''' 
        ''' From a In AA Select b, c  ==> AA.Select(Function(a) New With {b, c})
        ''' </summary>
        Private Function BindSelectClause(
            source As BoundQueryClauseBase,
            clauseSyntax As SelectClauseSyntax,
            operatorsEnumerator As SyntaxList(Of QueryClauseSyntax).Enumerator,
            diagnostics As DiagnosticBag
        ) As BoundQueryClause
            Debug.Assert(clauseSyntax Is operatorsEnumerator.Current)

            ' Create LambdaSymbol for the shape of the selector.
            Dim param As BoundLambdaParameterSymbol = CreateQueryLambdaParameterSymbol(GetQueryLambdaParameterName(source.RangeVariables), 0,
                                                                                       source.CompoundVariableType,
                                                                                       clauseSyntax, source.RangeVariables)

            Dim lambdaSymbol = Me.CreateQueryLambdaSymbol(LambdaUtilities.GetSelectLambdaBody(clauseSyntax),
                                                          SynthesizedLambdaKind.SelectQueryLambda,
                                                          ImmutableArray.Create(param))

            ' Create binder for the selector.
            Dim selectorBinder As New QueryLambdaBinder(lambdaSymbol, source.RangeVariables)

            Dim declaredRangeVariables As ImmutableArray(Of RangeVariableSymbol) = Nothing
            Dim selector As BoundExpression = selectorBinder.BindSelectClauseSelector(clauseSyntax,
                                                                                      operatorsEnumerator,
                                                                                      declaredRangeVariables,
                                                                                      diagnostics)

            Dim selectorLambda = CreateBoundQueryLambda(lambdaSymbol,
                                                        source.RangeVariables,
                                                        selector,
                                                        exprIsOperandOfConditionalBranch:=False)

            lambdaSymbol.SetQueryLambdaReturnType(selector.Type)
            selectorLambda.SetWasCompilerGenerated()

            ' Now bind the call.
            Dim boundCallOrBadExpression As BoundExpression

            If source.Type.IsErrorType() Then
                boundCallOrBadExpression = BadExpression(clauseSyntax, ImmutableArray.Create(Of BoundNode)(source, selectorLambda),
                                                         ErrorTypeSymbol.UnknownResultType).MakeCompilerGenerated()
            Else
                Dim suppressDiagnostics As DiagnosticBag = Nothing

                If ShouldSuppressDiagnostics(selectorLambda) Then
                    ' Operator BindQueryClauseCall will fail, let's suppress any additional errors it will report.
                    suppressDiagnostics = DiagnosticBag.GetInstance()
                    diagnostics = suppressDiagnostics
                End If

                boundCallOrBadExpression = BindQueryOperatorCall(clauseSyntax, source,
                                                                 StringConstants.SelectMethod,
                                                                 ImmutableArray.Create(Of BoundExpression)(selectorLambda),
                                                                 clauseSyntax.SelectKeyword.Span,
                                                                 diagnostics)

                If suppressDiagnostics IsNot Nothing Then
                    suppressDiagnostics.Free()
                End If
            End If

            Return New BoundQueryClause(clauseSyntax,
                                        boundCallOrBadExpression,
                                        declaredRangeVariables,
                                        selectorLambda.Expression.Type,
                                        ImmutableArray.Create(Of Binder)(selectorBinder),
                                        boundCallOrBadExpression.Type)
        End Function

        Private Shared Function ShouldSuppressDiagnostics(lambda As BoundQueryLambda) As Boolean
            If lambda.HasErrors Then
                Return True
            End If

            For Each param As ParameterSymbol In lambda.LambdaSymbol.Parameters
                If param.Type.IsErrorType() Then
                    Return True
                End If
            Next

            Dim bodyType As TypeSymbol = lambda.Expression.Type
            Return bodyType IsNot Nothing AndAlso bodyType.IsErrorType()
        End Function


        Private Shared Function ShadowsRangeVariableInTheChildScope(
            childScopeBinder As Binder,
            rangeVar As RangeVariableSymbol
        ) As Boolean
            Dim lookup = LookupResult.GetInstance()

            childScopeBinder.LookupInSingleBinder(lookup, rangeVar.Name, 0, Nothing, childScopeBinder, useSiteDiagnostics:=Nothing)

            Dim result As Boolean = (lookup.IsGood AndAlso lookup.Symbols(0).Kind = SymbolKind.RangeVariable)

            lookup.Free()

            Return result
        End Function

        ''' <summary>
        ''' Given result of binding preceding query operators, the source, bind the following Let operator.
        ''' 
        '''     {Preceding query operators} Let {expression range variables}
        ''' 
        ''' Ex: From a In AA Let b  ==> AA.Select(Function(a) New With {a, b})
        ''' 
        ''' Ex: From a In AA Let b, c  ==> AA.Select(Function(a) New With {a, b}).Select(Function({a, b}) New With {a, b, c})
        ''' 
        ''' Note, that preceding Select operator can introduce unnamed range variable, which is dropped by the Let
        ''' 
        ''' Ex: From a In AA Select a + 1 Let b ==> AA.Select(Function(a) a + 1).Select(Function(unnamed) b)  
        ''' 
        ''' Also, depending on the amount of expression range variables declared by the Let, and the following query operators,
        ''' translation can produce a nested, as opposed to flat, compound variable.
        ''' 
        ''' Ex: From a In AA Let b, c, d ==> AA.Select(Function(a) New With {a, b}).
        '''                                     Select(Function({a, b}) New With {{a, b}, c}).
        '''                                     Select(Function({{a, b}, c}) New With {a, b, c, d})   
        ''' </summary>
        Private Function BindLetClause(
            source As BoundQueryClauseBase,
            clauseSyntax As LetClauseSyntax,
            operatorsEnumerator As SyntaxList(Of QueryClauseSyntax).Enumerator,
            diagnostics As DiagnosticBag,
            Optional skipFirstVariable As Boolean = False
        ) As BoundQueryClause
            Debug.Assert(clauseSyntax Is operatorsEnumerator.Current)

            Dim suppressDiagnostics As DiagnosticBag = Nothing
            Dim callDiagnostics As DiagnosticBag = diagnostics

            Dim variables As SeparatedSyntaxList(Of ExpressionRangeVariableSyntax) = clauseSyntax.Variables
            Debug.Assert(variables.Count > 0, "Malformed syntax tree.")

            If variables.Count = 0 Then
                ' Handle malformed tree gracefully.
                Debug.Assert(Not skipFirstVariable)
                Return New BoundQueryClause(clauseSyntax,
                                            BadExpression(clauseSyntax, source, ErrorTypeSymbol.UnknownResultType).MakeCompilerGenerated(),
                                            source.RangeVariables.Add(RangeVariableSymbol.CreateForErrorRecovery(Me,
                                                                                                                 clauseSyntax,
                                                                                                                 ErrorTypeSymbol.UnknownResultType)),
                                            ErrorTypeSymbol.UnknownResultType,
                                            ImmutableArray.Create(Me),
                                            ErrorTypeSymbol.UnknownResultType,
                                            hasErrors:=True)
            End If

            Debug.Assert(Not skipFirstVariable OrElse source.Syntax Is variables.First)

            For i = If(skipFirstVariable, 1, 0) To variables.Count - 1

                Dim variable As ExpressionRangeVariableSyntax = variables(i)

                ' Create LambdaSymbol for the shape of the selector lambda.
                Dim param As BoundLambdaParameterSymbol = CreateQueryLambdaParameterSymbol(GetQueryLambdaParameterName(source.RangeVariables), 0,
                                                                                           source.CompoundVariableType,
                                                                                           variable, source.RangeVariables)

                Dim lambdaSymbol = Me.CreateQueryLambdaSymbol(LambdaUtilities.GetLetVariableLambdaBody(variable),
                                                              SynthesizedLambdaKind.LetVariableQueryLambda,
                                                              ImmutableArray.Create(param))

                ' Create binder for a variable expression.
                Dim selectorBinder As New QueryLambdaBinder(lambdaSymbol, source.RangeVariables)

                Dim declaredRangeVariables As ImmutableArray(Of RangeVariableSymbol) = Nothing
                Dim selector As BoundExpression = selectorBinder.BindLetClauseVariableSelector(variable,
                                                                                               operatorsEnumerator,
                                                                                               declaredRangeVariables,
                                                                                               diagnostics)

                Dim selectorLambda = CreateBoundQueryLambda(lambdaSymbol,
                                                            source.RangeVariables,
                                                            selector,
                                                            exprIsOperandOfConditionalBranch:=False)

                lambdaSymbol.SetQueryLambdaReturnType(selector.Type)
                selectorLambda.SetWasCompilerGenerated()

                ' Now bind the call.
                Dim boundCallOrBadExpression As BoundExpression

                If source.Type.IsErrorType() Then
                    boundCallOrBadExpression = BadExpression(variable, ImmutableArray.Create(Of BoundNode)(source, selectorLambda),
                                                             ErrorTypeSymbol.UnknownResultType).MakeCompilerGenerated()
                Else
                    If suppressDiagnostics Is Nothing AndAlso ShouldSuppressDiagnostics(selectorLambda) Then
                        ' Operator BindQueryClauseCall will fail, let's suppress any additional errors it will report.
                        suppressDiagnostics = DiagnosticBag.GetInstance()
                        callDiagnostics = suppressDiagnostics
                    End If

                    Dim operatorNameLocation As TextSpan

                    If i = 0 Then
                        ' This is the first variable.
                        operatorNameLocation = clauseSyntax.LetKeyword.Span
                    Else
                        operatorNameLocation = variables.GetSeparator(i - 1).Span
                    End If

                    boundCallOrBadExpression = BindQueryOperatorCall(variable, source,
                                                                     StringConstants.SelectMethod,
                                                                     ImmutableArray.Create(Of BoundExpression)(selectorLambda),
                                                                     operatorNameLocation,
                                                                     callDiagnostics)
                End If

                source = New BoundQueryClause(variable,
                                              boundCallOrBadExpression,
                                              source.RangeVariables.Concat(declaredRangeVariables),
                                              selectorLambda.Expression.Type,
                                              ImmutableArray.Create(Of Binder)(selectorBinder),
                                              boundCallOrBadExpression.Type)
            Next

            If suppressDiagnostics IsNot Nothing Then
                suppressDiagnostics.Free()
            End If

            Return DirectCast(source, BoundQueryClause)
        End Function

        ''' <summary>
        ''' In some scenarios, it is safe to leave compound variable in nested form when there is an
        ''' operator down the road that does its own projection (Select, Group By, ...). 
        ''' All following operators have to take an Anonymous Type in both cases and, since there is no way to
        ''' restrict the shape of the Anonymous Type in method's declaration, the operators should be
        ''' insensitive to the shape of the Anonymous Type.
        ''' </summary>
        Private Shared Function MustProduceFlatCompoundVariable(
            operatorsEnumerator As SyntaxList(Of QueryClauseSyntax).Enumerator
        ) As Boolean

            While operatorsEnumerator.MoveNext()
                Select Case operatorsEnumerator.Current.Kind
                    Case SyntaxKind.SimpleJoinClause,
                         SyntaxKind.GroupJoinClause,
                         SyntaxKind.SelectClause,
                         SyntaxKind.LetClause,
                         SyntaxKind.FromClause,
                         SyntaxKind.AggregateClause
                        Return False

                    Case SyntaxKind.GroupByClause
                        ' If [Group By] doesn't have selector for a group's element, we must produce flat result. 
                        ' Element of the group can be observed through result of the query.
                        Dim groupBy = DirectCast(operatorsEnumerator.Current, GroupByClauseSyntax)
                        Return groupBy.Items.Count = 0
                End Select
            End While

            Return True
        End Function

        ''' <summary>
        ''' In some scenarios, it is safe to leave compound variable in nested form when there is an
        ''' operator down the road that does its own projection (Select, Group By, ...). 
        ''' All following operators have to take an Anonymous Type in both cases and, since there is no way to
        ''' restrict the shape of the Anonymous Type in method's declaration, the operators should be
        ''' insensitive to the shape of the Anonymous Type.
        ''' </summary>
        Private Shared Function MustProduceFlatCompoundVariable(
            groupOrInnerJoin As JoinClauseSyntax,
            operatorsEnumerator As SyntaxList(Of QueryClauseSyntax).Enumerator
        ) As Boolean
            Select Case groupOrInnerJoin.Parent.Kind
                Case SyntaxKind.SimpleJoinClause
                    ' If we are nested into an Inner Join, it is safe to not flatten.
                    ' Parent join will take care of flattening.
                    Return False

                Case SyntaxKind.GroupJoinClause
                    Dim groupJoin = DirectCast(groupOrInnerJoin.Parent, GroupJoinClauseSyntax)

                    ' If we are nested into a Group Join, we are building the group for it.
                    ' It is safe to not flatten, if there is another nested join after this one,
                    ' the last nested join will take care of flattening.
                    Return groupOrInnerJoin Is groupJoin.AdditionalJoins.LastOrDefault

                Case Else
                    Return MustProduceFlatCompoundVariable(operatorsEnumerator)
            End Select
        End Function


        ''' <summary>
        ''' Given result of binding preceding query operators, if any, bind the following From operator.
        ''' 
        '''     [{Preceding query operators}] From {collection range variables}
        ''' 
        ''' Ex: From a In AA  ==> AA
        ''' 
        ''' Ex: From a In AA, b in BB  ==> AA.SelectMany(Function(a) BB, Function(a, b) New With {a, b})
        ''' 
        ''' Ex: {source with range variable 'd'} From a In AA, b in BB  ==> source.SelectMany(Function(d) AA, Function(d, a) New With {d, a}).
        '''                                                                        SelectMany(Function({d, a}) BB, 
        '''                                                                                   Function({d, a}, b) New With {d, a, b})
        ''' 
        ''' Note, that preceding Select operator can introduce unnamed range variable, which is dropped by the From
        ''' 
        ''' Ex: From a In AA Select a + 1 From b in BB ==> AA.Select(Function(a) a + 1).
        '''                                                   SelectMany(Function(unnamed) BB,
        '''                                                              Function(unnamed, b) b)  
        ''' 
        ''' Also, depending on the amount of collection range variables declared by the From, and the following query operators,
        ''' translation can produce a nested, as opposed to flat, compound variable.
        ''' 
        ''' Ex: From a In AA From b In BB, c In CC, d In DD ==> AA.SelectMany(Function(a) BB, Function(a, b) New With {a, b}).
        '''                                                        SelectMany(Function({a, b}) CC, Function({a, b}, c) New With {{a, b}, c}).
        '''                                                        SelectMany(Function({{a, b}, c}) DD, 
        '''                                                                   Function({{a, b}, c}, d) New With {a, b, c, d})   
        ''' 
        ''' If From operator translation results in a SelectMany call and the From is immediately followed by a Select or a Let operator, 
        ''' they are absorbed by the From translation. When this happens, operatorsEnumerator is advanced appropriately.
        ''' 
        ''' Ex: From a In AA From b In BB Select a + b ==> AA.SelectMany(Function(a) BB, Function(a, b) a + b)
        ''' 
        ''' Ex: From a In AA From b In BB Let c ==> AA.SelectMany(Function(a) BB, Function(a, b) new With {a, b, c})
        ''' 
        ''' </summary>
        Private Function BindFromClause(
            sourceOpt As BoundQueryClauseBase,
            from As FromClauseSyntax,
            ByRef operatorsEnumerator As SyntaxList(Of QueryClauseSyntax).Enumerator,
            diagnostics As DiagnosticBag
        ) As BoundQueryClauseBase
            Debug.Assert(from Is operatorsEnumerator.Current)
            Return BindCollectionRangeVariables(from, sourceOpt, from.Variables, operatorsEnumerator, diagnostics)
        End Function

        ''' <summary>
        ''' See comments for BindFromClause method, this method actually does all the work.
        ''' </summary>
        Private Function BindCollectionRangeVariables(
            clauseSyntax As QueryClauseSyntax,
            sourceOpt As BoundQueryClauseBase,
            variables As SeparatedSyntaxList(Of CollectionRangeVariableSyntax),
            ByRef operatorsEnumerator As SyntaxList(Of QueryClauseSyntax).Enumerator,
            diagnostics As DiagnosticBag
        ) As BoundQueryClauseBase

            Debug.Assert(clauseSyntax.IsKind(SyntaxKind.AggregateClause) OrElse clauseSyntax.IsKind(SyntaxKind.FromClause))
            Debug.Assert(variables.Count > 0, "Malformed syntax tree.")

            ' Handle malformed tree gracefully.
            If variables.Count = 0 Then
                Dim rangeVar = RangeVariableSymbol.CreateForErrorRecovery(Me, clauseSyntax, ErrorTypeSymbol.UnknownResultType)

                If sourceOpt Is Nothing Then
                    Return New BoundQueryableSource(clauseSyntax,
                                                    New BoundQuerySource(BadExpression(clauseSyntax, ErrorTypeSymbol.UnknownResultType).MakeCompilerGenerated()).MakeCompilerGenerated(),
                                                    Nothing,
                                                    ImmutableArray.Create(rangeVar),
                                                    ErrorTypeSymbol.UnknownResultType,
                                                    ImmutableArray.Create(Me),
                                                    ErrorTypeSymbol.UnknownResultType,
                                                    hasErrors:=True)
                Else
                    Return New BoundQueryClause(clauseSyntax,
                                                BadExpression(clauseSyntax, sourceOpt, ErrorTypeSymbol.UnknownResultType).MakeCompilerGenerated(),
                                                sourceOpt.RangeVariables.Add(rangeVar),
                                                ErrorTypeSymbol.UnknownResultType,
                                                ImmutableArray.Create(Me),
                                                ErrorTypeSymbol.UnknownResultType,
                                                hasErrors:=True)
                End If
            End If

            Dim source As BoundQueryClauseBase = sourceOpt

            If source Is Nothing Then
                ' We are at the beginning of the query.
                ' Let's go ahead and process the first collection range variable then.
                source = BindCollectionRangeVariable(variables(0), True, Nothing, diagnostics)
                Debug.Assert(source.RangeVariables.Length = 1)
            End If

            Dim suppressDiagnostics As DiagnosticBag = Nothing
            Dim callDiagnostics As DiagnosticBag = diagnostics

            For i = If(source Is sourceOpt, 0, 1) To variables.Count - 1

                Dim variable As CollectionRangeVariableSyntax = variables(i)

                ' Create LambdaSymbol for the shape of the many-selector lambda.
                Dim manySelectorParam As BoundLambdaParameterSymbol = CreateQueryLambdaParameterSymbol(GetQueryLambdaParameterName(source.RangeVariables), 0,
                                                                                                       source.CompoundVariableType,
                                                                                                       variable, source.RangeVariables)

                Dim manySelectorLambdaSymbol = Me.CreateQueryLambdaSymbol(LambdaUtilities.GetFromOrAggregateVariableLambdaBody(variable),
                                                                          SynthesizedLambdaKind.FromOrAggregateVariableQueryLambda,
                                                                          ImmutableArray.Create(manySelectorParam))

                ' Create binder for the many selector.
                Dim manySelectorBinder As New QueryLambdaBinder(manySelectorLambdaSymbol, source.RangeVariables)

                Dim manySelector As BoundQueryableSource = manySelectorBinder.BindCollectionRangeVariable(variable, False, Nothing, diagnostics)
                Debug.Assert(manySelector.RangeVariables.Length = 1)

                Dim manySelectorLambda = CreateBoundQueryLambda(manySelectorLambdaSymbol,
                                                                source.RangeVariables,
                                                                manySelector,
                                                                exprIsOperandOfConditionalBranch:=False)

                ' Note, we are not setting return type for the manySelectorLambdaSymbol because
                ' we want it to be taken from the target delegate type. We don't care what it is going to be
                ' because it doesn't affect types of range variables after this operator.
                manySelectorLambda.SetWasCompilerGenerated()

                ' Create LambdaSymbol for the shape of the join-selector lambda.
                Dim joinSelectorParamLeft As BoundLambdaParameterSymbol = CreateQueryLambdaParameterSymbol(GetQueryLambdaParameterNameLeft(source.RangeVariables), 0,
                                                                                                           source.CompoundVariableType,
                                                                                                           variable, source.RangeVariables)

                Dim joinSelectorParamRight As BoundLambdaParameterSymbol = CreateQueryLambdaParameterSymbol(GetQueryLambdaParameterNameRight(manySelector.RangeVariables), 1,
                                                                                                            manySelector.CompoundVariableType,
                                                                                                            variable, manySelector.RangeVariables)
                Dim lambdaBinders As ImmutableArray(Of Binder)

                ' If this is the last collection range variable, see if the next operator is
                ' a Select or a Let. If it is, we should absorb it by putting its selector
                ' in the join lambda.
                Dim absorbNextOperator As QueryClauseSyntax = Nothing

                If i = variables.Count - 1 Then
                    absorbNextOperator = JoinShouldAbsorbNextOperator(operatorsEnumerator)
                End If

                Dim sourceRangeVariables = source.RangeVariables
                Dim joinSelectorRangeVariables As ImmutableArray(Of RangeVariableSymbol) = sourceRangeVariables.Concat(manySelector.RangeVariables)
                Dim joinSelectorDeclaredRangeVariables As ImmutableArray(Of RangeVariableSymbol)
                Dim joinSelector As BoundExpression
                Dim group As BoundQueryClauseBase = Nothing
                Dim intoBinder As IntoClauseDisallowGroupReferenceBinder = Nothing
                Dim joinSelectorBinder As QueryLambdaBinder = Nothing

                Dim joinSelectorLambdaKind As SynthesizedLambdaKind = Nothing
                Dim joinSelectorSyntax As VisualBasicSyntaxNode = Nothing
                GetAbsorbingJoinSelectorLambdaKindAndSyntax(clauseSyntax, absorbNextOperator, joinSelectorLambdaKind, joinSelectorSyntax)

                Dim joinSelectorLambdaSymbol = Me.CreateQueryLambdaSymbol(joinSelectorSyntax,
                                                                          joinSelectorLambdaKind,
                                                                          ImmutableArray.Create(joinSelectorParamLeft, joinSelectorParamRight))

                If absorbNextOperator IsNot Nothing Then

                    ' Absorb selector of the next operator.
                    joinSelectorBinder = New QueryLambdaBinder(joinSelectorLambdaSymbol, joinSelectorRangeVariables)

                    joinSelectorDeclaredRangeVariables = Nothing
                    joinSelector = joinSelectorBinder.BindAbsorbingJoinSelector(absorbNextOperator,
                                                                                operatorsEnumerator,
                                                                                sourceRangeVariables,
                                                                                manySelector.RangeVariables,
                                                                                joinSelectorDeclaredRangeVariables,
                                                                                group,
                                                                                intoBinder,
                                                                                diagnostics)

                    lambdaBinders = ImmutableArray.Create(Of Binder)(manySelectorBinder, joinSelectorBinder)
                Else
                    joinSelectorDeclaredRangeVariables = ImmutableArray(Of RangeVariableSymbol).Empty

                    If sourceRangeVariables.Length > 0 Then
                        ' Need to build an Anonymous Type.
                        joinSelectorBinder = New QueryLambdaBinder(joinSelectorLambdaSymbol, joinSelectorRangeVariables)

                        ' If it is not the last variable in the list, we simply combine source's
                        ' compound variable (an instance of its Anonymous Type) with our new variable, 
                        ' creating new compound variable of nested Anonymous Type.

                        ' In some scenarios, it is safe to leave compound variable in nested form when there is an
                        ' operator down the road that does its own projection (Select, Group By, ...). 
                        ' All following operators have to take an Anonymous Type in both cases and, since there is no way to
                        ' restrict the shape of the Anonymous Type in method's declaration, the operators should be
                        ' insensitive to the shape of the Anonymous Type.
                        joinSelector = joinSelectorBinder.BuildJoinSelector(variable,
                                                                            (i = variables.Count - 1 AndAlso
                                                                                MustProduceFlatCompoundVariable(operatorsEnumerator)),
                                                                            diagnostics)
                    Else
                        ' Easy case, no need to build an Anonymous Type.
                        Debug.Assert(sourceRangeVariables.Length = 0)
                        joinSelector = New BoundParameter(joinSelectorParamRight.Syntax, joinSelectorParamRight,
                                                           False, joinSelectorParamRight.Type).MakeCompilerGenerated()
                    End If

                    lambdaBinders = ImmutableArray.Create(Of Binder)(manySelectorBinder)
                End If

                ' Join selector is either associated with absorbed select/let/aggregate clause
                ' or it doesn't contain user code (just pairs outer with inner into an anonymous type or is an identity).
                Dim joinSelectorLambda = CreateBoundQueryLambda(joinSelectorLambdaSymbol,
                                                                joinSelectorRangeVariables,
                                                                joinSelector,
                                                                exprIsOperandOfConditionalBranch:=False)

                joinSelectorLambdaSymbol.SetQueryLambdaReturnType(joinSelector.Type)
                joinSelectorLambda.SetWasCompilerGenerated()

                ' Now bind the call.
                Dim boundCallOrBadExpression As BoundExpression

                If source.Type.IsErrorType() Then
                    boundCallOrBadExpression = BadExpression(variable, ImmutableArray.Create(Of BoundNode)(source, manySelectorLambda, joinSelectorLambda),
                                                             ErrorTypeSymbol.UnknownResultType).MakeCompilerGenerated()
                Else
                    If suppressDiagnostics Is Nothing AndAlso
                       (ShouldSuppressDiagnostics(manySelectorLambda) OrElse ShouldSuppressDiagnostics(joinSelectorLambda)) Then
                        ' Operator BindQueryClauseCall will fail, let's suppress any additional errors it will report.
                        suppressDiagnostics = DiagnosticBag.GetInstance()
                        callDiagnostics = suppressDiagnostics
                    End If

                    Dim operatorNameLocation As TextSpan

                    If i = 0 Then
                        ' This is the first variable.
                        operatorNameLocation = clauseSyntax.GetFirstToken().Span
                    Else
                        operatorNameLocation = variables.GetSeparator(i - 1).Span
                    End If

                    boundCallOrBadExpression = BindQueryOperatorCall(variable, source,
                                                                     StringConstants.SelectManyMethod,
                                                                     ImmutableArray.Create(Of BoundExpression)(manySelectorLambda, joinSelectorLambda),
                                                                     operatorNameLocation,
                                                                     callDiagnostics)
                End If

                source = New BoundQueryClause(variable,
                                              boundCallOrBadExpression,
                                              joinSelectorRangeVariables,
                                              joinSelectorLambda.Expression.Type,
                                              lambdaBinders,
                                              boundCallOrBadExpression.Type)

                If absorbNextOperator IsNot Nothing Then
                    Debug.Assert(i = variables.Count - 1)
                    source = AbsorbOperatorFollowingJoin(DirectCast(source, BoundQueryClause),
                                                         absorbNextOperator, operatorsEnumerator,
                                                         joinSelectorDeclaredRangeVariables,
                                                         joinSelectorBinder,
                                                         sourceRangeVariables,
                                                         manySelector.RangeVariables,
                                                         group,
                                                         intoBinder,
                                                         diagnostics)
                    Exit For
                End If
            Next

            If suppressDiagnostics IsNot Nothing Then
                suppressDiagnostics.Free()
            End If

            Return source
        End Function

        Private Shared Sub GetAbsorbingJoinSelectorLambdaKindAndSyntax(
            clauseSyntax As QueryClauseSyntax,
            absorbNextOperator As QueryClauseSyntax,
            <Out> ByRef lambdaKind As SynthesizedLambdaKind,
            <Out> ByRef lambdaSyntax As VisualBasicSyntaxNode)

            ' Join selector is either associated with absorbed select/let/aggregate clause
            ' or it doesn't contain user code (just pairs outer with inner into an anonymous type or is an identity).

            If absorbNextOperator Is Nothing Then
                Select Case clauseSyntax.Kind
                    Case SyntaxKind.SimpleJoinClause
                        lambdaKind = SynthesizedLambdaKind.JoinNonUserCodeQueryLambda

                    Case SyntaxKind.FromClause
                        lambdaKind = SynthesizedLambdaKind.FromNonUserCodeQueryLambda

                    Case SyntaxKind.AggregateClause
                        lambdaKind = SynthesizedLambdaKind.FromNonUserCodeQueryLambda

                    Case Else
                        Throw ExceptionUtilities.UnexpectedValue(clauseSyntax.Kind)
                End Select

                lambdaSyntax = clauseSyntax
                Debug.Assert(LambdaUtilities.IsNonUserCodeQueryLambda(lambdaSyntax))
            Else
                Select Case absorbNextOperator.Kind
                    Case SyntaxKind.AggregateClause
                        Dim firstVariable = DirectCast(absorbNextOperator, AggregateClauseSyntax).Variables.First
                        lambdaSyntax = LambdaUtilities.GetFromOrAggregateVariableLambdaBody(firstVariable)
                        lambdaKind = SynthesizedLambdaKind.AggregateQueryLambda

                    Case SyntaxKind.LetClause
                        Dim firstVariable = DirectCast(absorbNextOperator, LetClauseSyntax).Variables.First
                        lambdaSyntax = LambdaUtilities.GetLetVariableLambdaBody(firstVariable)
                        lambdaKind = SynthesizedLambdaKind.LetVariableQueryLambda

                    Case SyntaxKind.SelectClause
                        Dim selectClause = DirectCast(absorbNextOperator, SelectClauseSyntax)
                        lambdaSyntax = LambdaUtilities.GetSelectLambdaBody(selectClause)
                        lambdaKind = SynthesizedLambdaKind.SelectQueryLambda

                    Case Else
                        Throw ExceptionUtilities.UnexpectedValue(absorbNextOperator.Kind)
                End Select

                Debug.Assert(LambdaUtilities.IsLambdaBody(lambdaSyntax))
            End If
        End Sub

        Private Shared Function JoinShouldAbsorbNextOperator(
            ByRef operatorsEnumerator As SyntaxList(Of QueryClauseSyntax).Enumerator
        ) As QueryClauseSyntax
            Dim copyOfOperatorsEnumerator As SyntaxList(Of QueryClauseSyntax).Enumerator = operatorsEnumerator

            If copyOfOperatorsEnumerator.MoveNext() Then
                Dim nextOperator As QueryClauseSyntax = copyOfOperatorsEnumerator.Current

                Select Case nextOperator.Kind
                    Case SyntaxKind.LetClause
                        If DirectCast(nextOperator, LetClauseSyntax).Variables.Count > 0 Then
                            ' Absorb Let
                            operatorsEnumerator = copyOfOperatorsEnumerator
                            Return nextOperator
                        Else
                            ' Malformed tree.
                            Debug.Assert(DirectCast(nextOperator, LetClauseSyntax).Variables.Count > 0, "Malformed syntax tree.")
                        End If

                    Case SyntaxKind.SelectClause
                        ' Absorb Select
                        operatorsEnumerator = copyOfOperatorsEnumerator
                        Return nextOperator

                    Case SyntaxKind.AggregateClause
                        ' Absorb Aggregate
                        operatorsEnumerator = copyOfOperatorsEnumerator
                        Return nextOperator

                End Select
            End If

            Return Nothing
        End Function

        Private Function AbsorbOperatorFollowingJoin(
            absorbingJoin As BoundQueryClause,
            absorbNextOperator As QueryClauseSyntax,
            operatorsEnumerator As SyntaxList(Of QueryClauseSyntax).Enumerator,
            joinSelectorDeclaredRangeVariables As ImmutableArray(Of RangeVariableSymbol),
            joinSelectorBinder As QueryLambdaBinder,
            leftRangeVariables As ImmutableArray(Of RangeVariableSymbol),
            rightRangeVariables As ImmutableArray(Of RangeVariableSymbol),
            group As BoundQueryClauseBase,
            intoBinder As IntoClauseDisallowGroupReferenceBinder,
            diagnostics As DiagnosticBag
        ) As BoundQueryClauseBase
            Debug.Assert(absorbNextOperator Is operatorsEnumerator.Current)
            Debug.Assert(absorbingJoin.Binders.Length > 1)

            Select Case absorbNextOperator.Kind
                Case SyntaxKind.SelectClause
                    ' Absorb Select.
                    Return New BoundQueryClause(absorbNextOperator,
                                                absorbingJoin, joinSelectorDeclaredRangeVariables,
                                                absorbingJoin.CompoundVariableType,
                                                ImmutableArray.Create(absorbingJoin.Binders.Last),
                                                absorbingJoin.Type)

                Case SyntaxKind.LetClause
                    ' Absorb Let.
                    ' First expression range variable was handled by the join selector,
                    ' create node for it.
                    Dim [let] = DirectCast(absorbNextOperator, LetClauseSyntax)
                    Debug.Assert([let].Variables.Count > 0)
                    Dim firstVariable As ExpressionRangeVariableSyntax = [let].Variables.First
                    Dim absorbedLet As New BoundQueryClause(firstVariable,
                                                            absorbingJoin,
                                                            absorbingJoin.RangeVariables.Concat(joinSelectorDeclaredRangeVariables),
                                                            absorbingJoin.CompoundVariableType,
                                                            ImmutableArray.Create(absorbingJoin.Binders.Last),
                                                            absorbingJoin.Type)

                    ' Handle the rest of the variables.
                    Return BindLetClause(absorbedLet, [let], operatorsEnumerator, diagnostics, skipFirstVariable:=True)

                Case SyntaxKind.AggregateClause
                    ' Absorb Aggregate.

                    Return CompleteAggregateClauseBinding(DirectCast(absorbNextOperator, AggregateClauseSyntax),
                                                          operatorsEnumerator,
                                                          leftRangeVariables,
                                                          rightRangeVariables,
                                                          absorbingJoin,
                                                          joinSelectorBinder,
                                                          joinSelectorDeclaredRangeVariables,
                                                          absorbingJoin.CompoundVariableType,
                                                          group,
                                                          intoBinder,
                                                          diagnostics)
                Case Else
                    Throw ExceptionUtilities.UnexpectedValue(absorbNextOperator.Kind)

            End Select

        End Function

        ''' <summary>
        ''' Given result of binding preceding query operators, the outer, bind the following Join operator.
        ''' 
        '''     [{Preceding query operators}] Join {collection range variable} 
        '''                                        [{additional joins}] 
        '''                                   On {condition}
        ''' 
        ''' Ex: From a In AA Join b in BB On Key(a) Equals Key(b)  ==> AA.Join(BB, Function(a) Key(a), Function(b) Key(b), 
        '''                                                                    Function(a, b) New With {a, b})
        ''' 
        ''' Ex: From a In AA                       AA.Join(
        '''     Join b in BB                               BB.Join(CC, Function(b) Key(b), Function(c) Key(c),
        '''          Join c in CC             ==>                  Function(b, c) New With {b, c}),
        '''          On Key(c) Equals Key(b)               Function(a) Key(a), Function({b, c}) Key(b),
        '''     On Key(a) Equals Key(b)                    Function(a, {b, c}) New With {a, b, c})
        '''                                                                    
        ''' 
        ''' Also, depending on the amount of collection range variables in scope, and the following query operators,
        ''' translation can produce a nested, as opposed to flat, compound variable.
        ''' 
        ''' Ex: From a In AA                       AA.Join(BB, Function(a) Key(a), Function(b) Key(b),
        '''     Join b in BB                               Function(a, b) New With {a, b}).
        '''     On Key(a) Equals Key(b)               Join(CC, Function({a, b}) Key(a, b), Function(c) Key(c),
        '''     Join c in CC             ==>               Function({a, b}, c) New With {{a, b}, c}).
        '''     On Key(c) Equals Key(a, b)            Join(DD, Function({{a, b}, c}) Key(a, b, c), Function(d) Key(d),
        '''     Join d in DD                               Function({{a, b}, c}, d) New With {a, b, c, d})
        '''     On Key(a, b, c) Equals Key(d)
        ''' 
        ''' If Join is immediately followed by a Select or a Let operator, they are absorbed by the translation. 
        ''' When this happens, operatorsEnumerator is advanced appropriately.
        ''' 
        ''' Ex: From a In AA Join b in BB On Key(a) Equals Key(b)  ==> AA.Join(BB, Function(a) Key(a), Function(b) Key(b), 
        '''     Select a + b                                                   Function(a, b) a + b)
        ''' 
        ''' Ex: From a In AA Join b in BB On Key(a) Equals Key(b)  ==> AA.Join(BB, Function(a) Key(a), Function(b) Key(b), 
        '''     Let c                                                   Function(a, b) New With {a, b, c})
        ''' 
        ''' </summary>
        Private Function BindInnerJoinClause(
            outer As BoundQueryClauseBase,
            join As SimpleJoinClauseSyntax,
            declaredNames As HashSet(Of String),
            ByRef operatorsEnumerator As SyntaxList(Of QueryClauseSyntax).Enumerator,
            diagnostics As DiagnosticBag
        ) As BoundQueryClauseBase
            Debug.Assert(join.Kind = SyntaxKind.SimpleJoinClause)
            Debug.Assert((declaredNames IsNot Nothing) = (join.Parent.Kind = SyntaxKind.SimpleJoinClause OrElse join.Parent.Kind = SyntaxKind.GroupJoinClause))

            Dim isNested As Boolean

            If declaredNames Is Nothing Then
                Debug.Assert(join Is operatorsEnumerator.Current)
                isNested = False
                declaredNames = CreateSetOfDeclaredNames(outer.RangeVariables)
            Else
                isNested = True
                AssertDeclaredNames(declaredNames, outer.RangeVariables)
            End If

            Debug.Assert(join.JoinedVariables.Count = 1, "Malformed syntax tree.")

            Dim inner As BoundQueryClauseBase

            If join.JoinedVariables.Count = 0 Then
                ' Malformed tree.
                Dim rangeVar = RangeVariableSymbol.CreateForErrorRecovery(Me, join, ErrorTypeSymbol.UnknownResultType)

                inner = New BoundQueryableSource(join,
                                                 New BoundQuerySource(BadExpression(join, ErrorTypeSymbol.UnknownResultType).MakeCompilerGenerated()).MakeCompilerGenerated(),
                                                 Nothing,
                                                 ImmutableArray.Create(rangeVar),
                                                 ErrorTypeSymbol.UnknownResultType,
                                                 ImmutableArray(Of Binder).Empty,
                                                 ErrorTypeSymbol.UnknownResultType,
                                                 hasErrors:=True).MakeCompilerGenerated()
            Else
                inner = BindCollectionRangeVariable(join.JoinedVariables(0), False, declaredNames, diagnostics)
            End If


            For Each additionalJoin As JoinClauseSyntax In join.AdditionalJoins
                Select Case additionalJoin.Kind
                    Case SyntaxKind.SimpleJoinClause
                        inner = BindInnerJoinClause(inner, DirectCast(additionalJoin, SimpleJoinClauseSyntax), declaredNames, Nothing, diagnostics)
                    Case SyntaxKind.GroupJoinClause
                        inner = BindGroupJoinClause(inner, DirectCast(additionalJoin, GroupJoinClauseSyntax), declaredNames, Nothing, diagnostics)
                End Select
            Next

            AssertDeclaredNames(declaredNames, inner.RangeVariables)

            ' Bind keys.
            Dim outerKeyLambda As BoundQueryLambda = Nothing
            Dim innerKeyLambda As BoundQueryLambda = Nothing
            Dim outerKeyBinder As QueryLambdaBinder = Nothing
            Dim innerKeyBinder As QueryLambdaBinder = Nothing
            Dim joinSelectorRangeVariables As ImmutableArray(Of RangeVariableSymbol) = outer.RangeVariables.Concat(inner.RangeVariables)

            QueryLambdaBinder.BindJoinKeys(Me, join, outer, inner,
                                           joinSelectorRangeVariables,
                                           outerKeyLambda, outerKeyBinder,
                                           innerKeyLambda, innerKeyBinder,
                                           diagnostics)

            ' Create LambdaSymbol for the shape of the join-selector lambda.
            Dim joinSelectorParamLeft As BoundLambdaParameterSymbol = CreateQueryLambdaParameterSymbol(GetQueryLambdaParameterNameLeft(outer.RangeVariables), 0,
                                                                                                       outer.CompoundVariableType,
                                                                                                       join, outer.RangeVariables)

            Dim joinSelectorParamRight As BoundLambdaParameterSymbol = CreateQueryLambdaParameterSymbol(GetQueryLambdaParameterNameRight(inner.RangeVariables), 1,
                                                                                                        inner.CompoundVariableType,
                                                                                                        join, inner.RangeVariables)

            Dim lambdaBinders As ImmutableArray(Of Binder)

            ' If the next operator is a Select or a Let, we should absorb it by putting its selector
            ' in the join lambda.
            Dim absorbNextOperator As QueryClauseSyntax = Nothing

            If Not isNested Then
                absorbNextOperator = JoinShouldAbsorbNextOperator(operatorsEnumerator)
            End If

            Dim joinSelectorDeclaredRangeVariables As ImmutableArray(Of RangeVariableSymbol)
            Dim joinSelector As BoundExpression
            Dim group As BoundQueryClauseBase = Nothing
            Dim intoBinder As IntoClauseDisallowGroupReferenceBinder = Nothing

            Dim joinSelectorLambdaKind As SynthesizedLambdaKind = Nothing
            Dim joinSelectorSyntax As VisualBasicSyntaxNode = Nothing
            GetAbsorbingJoinSelectorLambdaKindAndSyntax(join, absorbNextOperator, joinSelectorLambdaKind, joinSelectorSyntax)

            Dim joinSelectorLambdaSymbol = Me.CreateQueryLambdaSymbol(joinSelectorSyntax,
                                                                      joinSelectorLambdaKind,
                                                                      ImmutableArray.Create(joinSelectorParamLeft, joinSelectorParamRight))

            Dim joinSelectorBinder As New QueryLambdaBinder(joinSelectorLambdaSymbol, joinSelectorRangeVariables)

            If absorbNextOperator IsNot Nothing Then

                ' Absorb selector of the next operator.
                joinSelectorDeclaredRangeVariables = Nothing
                joinSelectorSyntax = Nothing
                joinSelector = joinSelectorBinder.BindAbsorbingJoinSelector(absorbNextOperator,
                                                                            operatorsEnumerator,
                                                                            outer.RangeVariables,
                                                                            inner.RangeVariables,
                                                                            joinSelectorDeclaredRangeVariables,
                                                                            group,
                                                                            intoBinder,
                                                                            diagnostics)

                lambdaBinders = ImmutableArray.Create(Of Binder)(outerKeyBinder, innerKeyBinder, joinSelectorBinder)
            Else
                Debug.Assert(outer.RangeVariables.Length > 0 AndAlso inner.RangeVariables.Length > 0)
                joinSelectorDeclaredRangeVariables = ImmutableArray(Of RangeVariableSymbol).Empty

                ' Need to build an Anonymous Type.

                ' In some scenarios, it is safe to leave compound variable in nested form when there is an
                ' operator down the road that does its own projection (Select, Group By, ...). 
                ' All following operators have to take an Anonymous Type in both cases and, since there is no way to
                ' restrict the shape of the Anonymous Type in method's declaration, the operators should be
                ' insensitive to the shape of the Anonymous Type.
                joinSelector = joinSelectorBinder.BuildJoinSelector(join,
                                                                    MustProduceFlatCompoundVariable(join, operatorsEnumerator),
                                                                    diagnostics)

                ' Not including joinSelectorBinder because there is no syntax behind this joinSelector,
                ' it is purely synthetic.
                lambdaBinders = ImmutableArray.Create(Of Binder)(outerKeyBinder, innerKeyBinder)
            End If

            Dim joinSelectorLambda = CreateBoundQueryLambda(joinSelectorLambdaSymbol,
                                                            joinSelectorRangeVariables,
                                                            joinSelector,
                                                            exprIsOperandOfConditionalBranch:=False)

            joinSelectorLambdaSymbol.SetQueryLambdaReturnType(joinSelector.Type)
            joinSelectorLambda.SetWasCompilerGenerated()

            ' Now bind the call.
            Dim boundCallOrBadExpression As BoundExpression

            If outer.Type.IsErrorType() Then
                boundCallOrBadExpression = BadExpression(join, ImmutableArray.Create(Of BoundNode)(outer, inner, outerKeyLambda, innerKeyLambda, joinSelectorLambda),
                                                         ErrorTypeSymbol.UnknownResultType).MakeCompilerGenerated()
            Else
                Dim callDiagnostics As DiagnosticBag = diagnostics
                Dim suppressDiagnostics As DiagnosticBag = Nothing

                If inner.HasErrors OrElse inner.Type.IsErrorType() OrElse
                   ShouldSuppressDiagnostics(outerKeyLambda) OrElse
                   ShouldSuppressDiagnostics(innerKeyLambda) OrElse
                   ShouldSuppressDiagnostics(joinSelectorLambda) Then
                    ' Operator BindQueryClauseCall will fail, let's suppress any additional errors it will report.
                    suppressDiagnostics = DiagnosticBag.GetInstance()
                    callDiagnostics = suppressDiagnostics
                End If

                boundCallOrBadExpression = BindQueryOperatorCall(join, outer,
                                                                 StringConstants.JoinMethod,
                                                                 ImmutableArray.Create(Of BoundExpression)(inner, outerKeyLambda, innerKeyLambda, joinSelectorLambda),
                                                                 join.JoinKeyword.Span,
                                                                 callDiagnostics)

                If suppressDiagnostics IsNot Nothing Then
                    suppressDiagnostics.Free()
                End If
            End If

            Dim result As BoundQueryClauseBase = New BoundQueryClause(join,
                                                                      boundCallOrBadExpression,
                                                                      joinSelectorRangeVariables,
                                                                      joinSelectorLambda.Expression.Type,
                                                                      lambdaBinders,
                                                                      boundCallOrBadExpression.Type)

            If absorbNextOperator IsNot Nothing Then
                Debug.Assert(Not isNested)
                result = AbsorbOperatorFollowingJoin(DirectCast(result, BoundQueryClause),
                                                     absorbNextOperator, operatorsEnumerator,
                                                     joinSelectorDeclaredRangeVariables,
                                                     joinSelectorBinder,
                                                     outer.RangeVariables,
                                                     inner.RangeVariables,
                                                     group,
                                                     intoBinder,
                                                     diagnostics)
            End If

            Return result
        End Function

        Private Shared Function CreateSetOfDeclaredNames() As HashSet(Of String)
            Return New HashSet(Of String)(CaseInsensitiveComparison.Comparer)
        End Function

        Private Shared Function CreateSetOfDeclaredNames(rangeVariables As ImmutableArray(Of RangeVariableSymbol)) As HashSet(Of String)
            Dim declaredNames As New HashSet(Of String)(CaseInsensitiveComparison.Comparer)

            For Each rangeVar As RangeVariableSymbol In rangeVariables
                declaredNames.Add(rangeVar.Name)
            Next

            Return declaredNames
        End Function

        <Conditional("DEBUG")>
        Private Shared Sub AssertDeclaredNames(declaredNames As HashSet(Of String), rangeVariables As ImmutableArray(Of RangeVariableSymbol))
#If DEBUG Then
            For Each rangeVar As RangeVariableSymbol In rangeVariables
                If Not rangeVar.Name.StartsWith("$"c, StringComparison.Ordinal) Then
                    Debug.Assert(declaredNames.Contains(rangeVar.Name))
                End If
            Next
#End If
        End Sub

        ''' <summary>
        ''' Given result of binding preceding query operators, the outer, bind the following Group Join operator.
        ''' 
        '''     [{Preceding query operators}] Group Join {collection range variable} 
        '''                                              [{additional joins}] 
        '''                                   On {condition}
        '''                                   Into {aggregation range variables}
        ''' 
        ''' Ex: From a In AA Group Join b in BB          AA.GroupJoin(BB, Function(a) Key(a), Function(b) Key(b), 
        '''                  On Key(a) Equals Key(b) ==>              Function(a, group_b) New With {a, group_b.Count()})
        '''                  Into Count()
        ''' 
        ''' Also, depending on the amount of collection range variables in scope, and the following query operators,
        ''' translation can produce a nested, as opposed to flat, compound variable (see BindInnerJoinClause for an example).
        ''' 
        ''' Note, that type of the group must be inferred from the set of available GroupJoin operators in order to be able to 
        ''' interpret the aggregation range variables. 
        ''' </summary>
        Private Function BindGroupJoinClause(
            outer As BoundQueryClauseBase,
            groupJoin As GroupJoinClauseSyntax,
            declaredNames As HashSet(Of String),
            operatorsEnumerator As SyntaxList(Of QueryClauseSyntax).Enumerator,
            diagnostics As DiagnosticBag
        ) As BoundQueryClause
            Debug.Assert((declaredNames IsNot Nothing) = (groupJoin.Parent.Kind = SyntaxKind.SimpleJoinClause OrElse groupJoin.Parent.Kind = SyntaxKind.GroupJoinClause))

            ' Shadowing rules for range variables declared by [Group Join] are a little
            ' bit tricky:
            '  1) Range variables declared within the inner source (JoinedVariables + AdditionalJoins)
            '     can survive only up until the [On] clause, after that, even those in scope, move out of scope
            '     into the Group. Other range variables in scope in the [On] are the outer's range variables.
            '     Range variables from outer's outer are not in scope and, therefore, are never shadowed by the
            '     same-named inner source range variables. Range variables declared within the inner source that
            '     go out of scope before interpretation reaches the [On] clause do not shadow even same-named
            '     outer's range variables. 
            '
            '  2) Range variables declared in the [Into] clause must not shadow outer's range variables simply 
            '     because they are merged into the same Anonymous Type by the [Into] selector. They also must
            '     not shadow outer's outer range variables (possibly throughout the whole hierarchy),
            '     with which they will later get into the same scope within an [On] clause. Note, that declaredNames
            '     parameter, when passed, includes the names of all such range variables and this function will add
            '     to this set.

            Dim namesInScopeInOnClause As HashSet(Of String) = CreateSetOfDeclaredNames(outer.RangeVariables)

            If declaredNames Is Nothing Then
                Debug.Assert(groupJoin Is operatorsEnumerator.Current)
                declaredNames = CreateSetOfDeclaredNames(outer.RangeVariables)
            Else
                AssertDeclaredNames(declaredNames, outer.RangeVariables)
            End If

            Debug.Assert(groupJoin.JoinedVariables.Count = 1, "Malformed syntax tree.")

            Dim inner As BoundQueryClauseBase

            If groupJoin.JoinedVariables.Count = 0 Then
                ' Malformed tree.
                Dim rangeVar = RangeVariableSymbol.CreateForErrorRecovery(Me, groupJoin, ErrorTypeSymbol.UnknownResultType)

                inner = New BoundQueryableSource(groupJoin,
                                                 New BoundQuerySource(BadExpression(groupJoin, ErrorTypeSymbol.UnknownResultType).MakeCompilerGenerated()).MakeCompilerGenerated(),
                                                 Nothing,
                                                 ImmutableArray.Create(rangeVar),
                                                 ErrorTypeSymbol.UnknownResultType,
                                                 ImmutableArray(Of Binder).Empty,
                                                 ErrorTypeSymbol.UnknownResultType,
                                                 hasErrors:=True).MakeCompilerGenerated()
            Else
                inner = BindCollectionRangeVariable(groupJoin.JoinedVariables(0), False, namesInScopeInOnClause, diagnostics)
            End If

            For Each additionalJoin As JoinClauseSyntax In groupJoin.AdditionalJoins
                Select Case additionalJoin.Kind
                    Case SyntaxKind.SimpleJoinClause
                        inner = BindInnerJoinClause(inner, DirectCast(additionalJoin, SimpleJoinClauseSyntax), namesInScopeInOnClause, Nothing, diagnostics)
                    Case SyntaxKind.GroupJoinClause
                        inner = BindGroupJoinClause(inner, DirectCast(additionalJoin, GroupJoinClauseSyntax), namesInScopeInOnClause, Nothing, diagnostics)
                End Select
            Next

            Debug.Assert(outer.RangeVariables.Length > 0 AndAlso inner.RangeVariables.Length > 0)
            AssertDeclaredNames(namesInScopeInOnClause, inner.RangeVariables)

            ' Bind keys.
            Dim outerKeyLambda As BoundQueryLambda = Nothing
            Dim innerKeyLambda As BoundQueryLambda = Nothing
            Dim outerKeyBinder As QueryLambdaBinder = Nothing
            Dim innerKeyBinder As QueryLambdaBinder = Nothing

            QueryLambdaBinder.BindJoinKeys(Me, groupJoin, outer, inner,
                                           outer.RangeVariables.Concat(inner.RangeVariables),
                                           outerKeyLambda, outerKeyBinder,
                                           innerKeyLambda, innerKeyBinder,
                                           diagnostics)

            ' Infer type of the resulting group.
            Dim methodGroup As BoundMethodGroup = Nothing
            Dim groupType As TypeSymbol = InferGroupType(outer, inner, groupJoin, outerKeyLambda, innerKeyLambda, methodGroup, diagnostics)

            ' Bind the INTO selector.
            Dim intoBinder As IntoClauseBinder = Nothing
            Dim intoRangeVariables As ImmutableArray(Of RangeVariableSymbol) = Nothing
            Dim intoLambda As BoundQueryLambda = BindIntoSelectorLambda(groupJoin, outer.RangeVariables, outer.CompoundVariableType,
                                                                        True, declaredNames,
                                                                        groupType, inner.RangeVariables, inner.CompoundVariableType,
                                                                        groupJoin.AggregationVariables,
                                                                        MustProduceFlatCompoundVariable(groupJoin, operatorsEnumerator),
                                                                        diagnostics, intoBinder, intoRangeVariables)

            ' Now bind the call.
            Dim boundCallOrBadExpression As BoundExpression

            If outer.Type.IsErrorType() OrElse methodGroup Is Nothing Then
                boundCallOrBadExpression = BadExpression(groupJoin, ImmutableArray.Create(Of BoundNode)(outer, inner, outerKeyLambda, innerKeyLambda, intoLambda),
                                                         ErrorTypeSymbol.UnknownResultType).MakeCompilerGenerated()
            Else
                Dim callDiagnostics As DiagnosticBag = diagnostics

                If inner.HasErrors OrElse inner.Type.IsErrorType() OrElse
                   ShouldSuppressDiagnostics(outerKeyLambda) OrElse
                   ShouldSuppressDiagnostics(innerKeyLambda) OrElse
                   ShouldSuppressDiagnostics(intoLambda) Then

                    ' Operator BindQueryClauseCall will fail, let's suppress any additional errors it will report.
                    callDiagnostics = DiagnosticBag.GetInstance()
                End If

                ' Reusing method group that we got while inferring group type, this way we can avoid doing name lookup again. 
                boundCallOrBadExpression = BindQueryOperatorCall(groupJoin, outer,
                                                               StringConstants.GroupJoinMethod,
                                                               methodGroup,
                                                               ImmutableArray.Create(Of BoundExpression)(inner, outerKeyLambda, innerKeyLambda, intoLambda),
                                                               groupJoin.JoinKeyword.Span,
                                                               callDiagnostics)

                If callDiagnostics IsNot diagnostics Then
                    callDiagnostics.Free()
                End If
            End If

            Return New BoundQueryClause(groupJoin,
                                        boundCallOrBadExpression,
                                        outer.RangeVariables.Concat(intoRangeVariables),
                                        intoLambda.Expression.Type,
                                        ImmutableArray.Create(Of Binder)(outerKeyBinder, innerKeyBinder, intoBinder),
                                        boundCallOrBadExpression.Type)

        End Function

        ''' <summary>
        ''' Given result of binding preceding query operators, the source, bind the following Group By operator.
        ''' 
        '''     [{Preceding query operators}] Group [{items expression range variables}] 
        '''                                   By {keys expression range variables}
        '''                                   Into {aggregation range variables}
        ''' 
        ''' Ex: From a In AA Group By Key(a)          AA.GroupBy(Function(a) Key(a), 
        '''                  Into Count()     ==>                Function(key, group_a) New With {key, group_a.Count()})
        '''                  
        ''' Ex: From a In AA Group Item(a)            AA.GroupBy(Function(a) Key(a), 
        '''                  By Key(a)        ==>                Function(a) Item(a), 
        '''                  Into Count()                        Function(key, group_a) New With {key, group_a.Count()})
        ''' 
        ''' Note, that type of the group must be inferred from the set of available GroupBy operators in order to be able to 
        ''' interpret the aggregation range variables. 
        ''' </summary>
        Private Function BindGroupByClause(
            source As BoundQueryClauseBase,
            groupBy As GroupByClauseSyntax,
            diagnostics As DiagnosticBag
        ) As BoundQueryClause

            ' Handle group items.
            Dim itemsLambdaBinder As QueryLambdaBinder = Nothing
            Dim itemsRangeVariables As ImmutableArray(Of RangeVariableSymbol) = Nothing
            Dim itemsLambda As BoundQueryLambda = BindGroupByItems(source, groupBy, itemsLambdaBinder, itemsRangeVariables, diagnostics)

            ' Handle grouping keys.
            Dim keysLambdaBinder As QueryLambdaBinder = Nothing
            Dim keysRangeVariables As ImmutableArray(Of RangeVariableSymbol) = Nothing
            Dim keysLambda As BoundQueryLambda = BindGroupByKeys(source, groupBy, keysLambdaBinder, keysRangeVariables, diagnostics)
            Debug.Assert(keysLambda IsNot Nothing)

            ' Infer type of the resulting group.
            Dim methodGroup As BoundMethodGroup = Nothing
            Dim groupType As TypeSymbol = InferGroupType(source, groupBy, itemsLambda, keysLambda, keysRangeVariables, methodGroup, diagnostics)

            ' Bind the INTO selector.
            Dim groupRangeVariables As ImmutableArray(Of RangeVariableSymbol)
            Dim groupCompoundVariableType As TypeSymbol

            If itemsLambda Is Nothing Then
                groupRangeVariables = source.RangeVariables
                groupCompoundVariableType = source.CompoundVariableType
            Else
                groupRangeVariables = itemsRangeVariables
                groupCompoundVariableType = itemsLambda.Expression.Type
            End If

            Dim intoBinder As IntoClauseBinder = Nothing
            Dim intoRangeVariables As ImmutableArray(Of RangeVariableSymbol) = Nothing
            Dim intoLambda As BoundQueryLambda = BindIntoSelectorLambda(groupBy, keysRangeVariables, keysLambda.Expression.Type, False, Nothing,
                                                                        groupType, groupRangeVariables, groupCompoundVariableType,
                                                                        groupBy.AggregationVariables, True,
                                                                        diagnostics, intoBinder, intoRangeVariables)

            ' Now bind the call.
            Dim groupByArguments() As BoundExpression
            Dim lambdaBinders As ImmutableArray(Of Binder)

            Debug.Assert((itemsLambda Is Nothing) = (itemsLambdaBinder Is Nothing))

            If itemsLambda Is Nothing Then
                groupByArguments = {keysLambda, intoLambda}
                lambdaBinders = ImmutableArray.Create(Of Binder)(keysLambdaBinder, intoBinder)
            Else
                groupByArguments = {keysLambda, itemsLambda, intoLambda}
                lambdaBinders = ImmutableArray.Create(Of Binder)(keysLambdaBinder, itemsLambdaBinder, intoBinder)
            End If

            Dim boundCallOrBadExpression As BoundExpression

            If source.Type.IsErrorType() OrElse methodGroup Is Nothing Then
                boundCallOrBadExpression = BadExpression(groupBy,
                                                         ImmutableArray.Create(Of BoundNode)(source).AddRange(groupByArguments),
                                                         ErrorTypeSymbol.UnknownResultType).MakeCompilerGenerated()
            Else
                Dim callDiagnostics As DiagnosticBag = diagnostics

                If ShouldSuppressDiagnostics(keysLambda) OrElse ShouldSuppressDiagnostics(intoLambda) OrElse
                   (itemsLambda IsNot Nothing AndAlso ShouldSuppressDiagnostics(itemsLambda)) Then

                    ' Operator BindQueryClauseCall will fail, let's suppress any additional errors it will report.
                    callDiagnostics = DiagnosticBag.GetInstance()
                End If

                ' Reusing method group that we got while inferring group type, this way we can avoid doing name lookup again. 
                boundCallOrBadExpression = BindQueryOperatorCall(groupBy, source,
                                                               StringConstants.GroupByMethod,
                                                               methodGroup,
                                                               groupByArguments.AsImmutableOrNull(),
                                                               GetGroupByOperatorNameSpan(groupBy),
                                                               callDiagnostics)

                If callDiagnostics IsNot diagnostics Then
                    callDiagnostics.Free()
                End If
            End If

            Return New BoundQueryClause(groupBy,
                                        boundCallOrBadExpression,
                                        keysRangeVariables.Concat(intoRangeVariables),
                                        intoLambda.Expression.Type,
                                        lambdaBinders,
                                        boundCallOrBadExpression.Type)
        End Function

        Private Shared Function GetGroupByOperatorNameSpan(groupBy As GroupByClauseSyntax) As TextSpan
            If groupBy.Items.Count = 0 Then
                Return GetQueryOperatorNameSpan(groupBy.GroupKeyword, groupBy.ByKeyword)
            Else
                Return groupBy.GroupKeyword.Span
            End If
        End Function


        ''' <summary>
        ''' Returns Nothing if items were omitted.
        ''' </summary>
        Private Function BindGroupByItems(
            source As BoundQueryClauseBase,
            groupBy As GroupByClauseSyntax,
            <Out()> ByRef itemsLambdaBinder As QueryLambdaBinder,
            <Out()> ByRef itemsRangeVariables As ImmutableArray(Of RangeVariableSymbol),
            diagnostics As DiagnosticBag
        ) As BoundQueryLambda
            Debug.Assert(itemsLambdaBinder Is Nothing)
            Debug.Assert(itemsRangeVariables.IsDefault)

            ' Handle group items.
            Dim itemsLambda As BoundQueryLambda = Nothing
            Dim items As SeparatedSyntaxList(Of ExpressionRangeVariableSyntax) = groupBy.Items

            If items.Count > 0 Then
                Dim itemsParam As BoundLambdaParameterSymbol = CreateQueryLambdaParameterSymbol(GetQueryLambdaParameterName(source.RangeVariables), 0,
                                                                                                source.CompoundVariableType,
                                                                                                groupBy, source.RangeVariables)

                Dim itemsLambdaSymbol = Me.CreateQueryLambdaSymbol(LambdaUtilities.GetGroupByItemsLambdaBody(groupBy),
                                                                   SynthesizedLambdaKind.GroupByItemsQueryLambda,
                                                                   ImmutableArray.Create(itemsParam))

                ' Create binder for the selector.
                itemsLambdaBinder = New QueryLambdaBinder(itemsLambdaSymbol, source.RangeVariables)

                Dim itemsSelector = itemsLambdaBinder.BindExpressionRangeVariables(items, False, groupBy,
                                                                                   itemsRangeVariables, diagnostics)

                itemsLambda = CreateBoundQueryLambda(itemsLambdaSymbol,
                                                     source.RangeVariables,
                                                     itemsSelector,
                                                     exprIsOperandOfConditionalBranch:=False)

                itemsLambdaSymbol.SetQueryLambdaReturnType(itemsSelector.Type)
                itemsLambda.SetWasCompilerGenerated()
            Else
                itemsLambdaBinder = Nothing
                itemsRangeVariables = ImmutableArray(Of RangeVariableSymbol).Empty
            End If

            Debug.Assert((itemsLambda Is Nothing) = (itemsLambdaBinder Is Nothing))
            Debug.Assert(Not itemsRangeVariables.IsDefault)
            Debug.Assert(itemsLambda IsNot Nothing OrElse itemsRangeVariables.Length = 0)
            Return itemsLambda
        End Function

        Private Function BindGroupByKeys(
            source As BoundQueryClauseBase,
            groupBy As GroupByClauseSyntax,
            <Out()> ByRef keysLambdaBinder As QueryLambdaBinder,
            <Out()> ByRef keysRangeVariables As ImmutableArray(Of RangeVariableSymbol),
            diagnostics As DiagnosticBag
        ) As BoundQueryLambda
            Debug.Assert(keysLambdaBinder Is Nothing)
            Debug.Assert(keysRangeVariables.IsDefault)

            Dim keys As SeparatedSyntaxList(Of ExpressionRangeVariableSyntax) = groupBy.Keys

            Dim keysParam As BoundLambdaParameterSymbol = CreateQueryLambdaParameterSymbol(GetQueryLambdaParameterName(source.RangeVariables), 0,
                                                                                           source.CompoundVariableType,
                                                                                           groupBy, source.RangeVariables)

            Dim keysLambdaSymbol = Me.CreateQueryLambdaSymbol(LambdaUtilities.GetGroupByKeysLambdaBody(groupBy),
                                                              SynthesizedLambdaKind.GroupByKeysQueryLambda,
                                                              ImmutableArray.Create(keysParam))

            ' Create binder for the selector.
            keysLambdaBinder = New QueryLambdaBinder(keysLambdaSymbol, source.RangeVariables)

            Dim keysSelector = keysLambdaBinder.BindExpressionRangeVariables(keys, True, groupBy,
                                                                             keysRangeVariables, diagnostics)

            Dim keysLambda = CreateBoundQueryLambda(keysLambdaSymbol,
                                                    source.RangeVariables,
                                                    keysSelector,
                                                    exprIsOperandOfConditionalBranch:=False)

            keysLambdaSymbol.SetQueryLambdaReturnType(keysSelector.Type)
            keysLambda.SetWasCompilerGenerated()

            Return keysLambda
        End Function

        ''' <summary>
        ''' Infer type of the group for a Group By operator from the set of available GroupBy methods.
        ''' 
        ''' In short, given already bound itemsLambda and keysLambda, this method performs overload
        ''' resolution over the set of available GroupBy operator methods using fake Into lambda:
        '''     Function(key, group As typeToBeInferred) New With {group}
        ''' 
        ''' If resolution succeeds, the type inferred for the best candidate is our result.  
        ''' </summary>
        Private Function InferGroupType(
            source As BoundQueryClauseBase,
            groupBy As GroupByClauseSyntax,
            itemsLambda As BoundQueryLambda,
            keysLambda As BoundQueryLambda,
            keysRangeVariables As ImmutableArray(Of RangeVariableSymbol),
            <Out()> ByRef methodGroup As BoundMethodGroup,
            diagnostics As DiagnosticBag
        ) As TypeSymbol
            Debug.Assert(methodGroup Is Nothing)

            Dim groupType As TypeSymbol = ErrorTypeSymbol.UnknownResultType

            If Not source.Type.IsErrorType() Then

                methodGroup = LookupQueryOperator(groupBy, source, StringConstants.GroupByMethod, Nothing, diagnostics)
                Debug.Assert(methodGroup Is Nothing OrElse methodGroup.ResultKind = LookupResultKind.Good OrElse methodGroup.ResultKind = LookupResultKind.Inaccessible)

                If methodGroup Is Nothing Then
                    ReportDiagnostic(diagnostics, Location.Create(groupBy.SyntaxTree, GetGroupByOperatorNameSpan(groupBy)), ERRID.ERR_QueryOperatorNotFound, StringConstants.GroupByMethod)

                ElseIf Not (ShouldSuppressDiagnostics(keysLambda) OrElse
                         (itemsLambda IsNot Nothing AndAlso ShouldSuppressDiagnostics(itemsLambda))) Then

                    Dim inferenceLambda As New GroupTypeInferenceLambda(groupBy, Me,
                                                                        (New ParameterSymbol() {
                                    CreateQueryLambdaParameterSymbol(StringConstants.It1,
                                                                   0,
                                                                   keysLambda.Expression.Type,
                                                                   groupBy, keysRangeVariables),
                                    CreateQueryLambdaParameterSymbol(StringConstants.It2,
                                                                   1,
                                                                   Nothing,
                                                                   groupBy)}).AsImmutableOrNull(),
                                                                        Compilation)

                    Dim groupByArguments() As BoundExpression

                    If itemsLambda Is Nothing Then
                        groupByArguments = {keysLambda, inferenceLambda}
                    Else
                        groupByArguments = {keysLambda, itemsLambda, inferenceLambda}
                    End If

                    Dim useSiteDiagnostics As HashSet(Of DiagnosticInfo) = Nothing
                    Dim results As OverloadResolution.OverloadResolutionResult = OverloadResolution.QueryOperatorInvocationOverloadResolution(methodGroup,
                                                                                                                                              groupByArguments.AsImmutableOrNull(), Me,
                                                                                                                                              useSiteDiagnostics)

                    diagnostics.Add(groupBy, useSiteDiagnostics)

                    If results.BestResult.HasValue Then
                        Dim method = DirectCast(results.BestResult.Value.Candidate.UnderlyingSymbol, MethodSymbol)
                        Dim resultSelector As TypeSymbol = method.Parameters(groupByArguments.Length - 1).Type

                        groupType = resultSelector.DelegateOrExpressionDelegate(Me).DelegateInvokeMethod.Parameters(1).Type

                    ElseIf Not source.HasErrors Then
                        ReportDiagnostic(diagnostics, Location.Create(groupBy.SyntaxTree, GetGroupByOperatorNameSpan(groupBy)), ERRID.ERR_QueryOperatorNotFound, StringConstants.GroupByMethod)
                    End If
                End If
            End If

            Debug.Assert(groupType IsNot Nothing)
            Return groupType
        End Function

        ''' <summary>
        ''' Infer type of the group for a Group Join operator from the set of available GroupJoin methods.
        ''' 
        ''' In short, given already bound inner source and the join key lambdas, this method performs overload
        ''' resolution over the set of available GroupJoin operator methods using fake Into lambda:
        '''     Function(outerVar, group As typeToBeInferred) New With {group}
        ''' 
        ''' If resolution succeeds, the type inferred for the best candidate is our result.  
        ''' </summary>
        Private Function InferGroupType(
            outer As BoundQueryClauseBase,
            inner As BoundQueryClauseBase,
            groupJoin As GroupJoinClauseSyntax,
            outerKeyLambda As BoundQueryLambda,
            innerKeyLambda As BoundQueryLambda,
            <Out()> ByRef methodGroup As BoundMethodGroup,
            diagnostics As DiagnosticBag
        ) As TypeSymbol
            Debug.Assert(methodGroup Is Nothing)

            Dim groupType As TypeSymbol = ErrorTypeSymbol.UnknownResultType

            If Not outer.Type.IsErrorType() Then

                ' If outer's type is "good", we should always do a lookup, even if we won't attempt the inference
                ' because BindGroupJoinClause still expects to have a group, unless the lookup fails.
                methodGroup = LookupQueryOperator(groupJoin, outer, StringConstants.GroupJoinMethod, Nothing, diagnostics)
                Debug.Assert(methodGroup Is Nothing OrElse methodGroup.ResultKind = LookupResultKind.Good OrElse methodGroup.ResultKind = LookupResultKind.Inaccessible)

                If methodGroup Is Nothing Then
                    ReportDiagnostic(diagnostics,
                                     Location.Create(groupJoin.SyntaxTree, GetQueryOperatorNameSpan(groupJoin.GroupKeyword, groupJoin.JoinKeyword)),
                                     ERRID.ERR_QueryOperatorNotFound, StringConstants.GroupJoinMethod)

                ElseIf Not ShouldSuppressDiagnostics(innerKeyLambda) AndAlso Not ShouldSuppressDiagnostics(outerKeyLambda) AndAlso
                       Not inner.HasErrors AndAlso Not inner.Type.IsErrorType() Then

                    Dim inferenceLambda As New GroupTypeInferenceLambda(groupJoin, Me,
                                                                        (New ParameterSymbol() {
                                    CreateQueryLambdaParameterSymbol(StringConstants.It1,
                                                                   0,
                                                                   outer.CompoundVariableType,
                                                                   groupJoin, outer.RangeVariables),
                                    CreateQueryLambdaParameterSymbol(StringConstants.It2,
                                                                   1,
                                                                   Nothing,
                                                                   groupJoin)}).AsImmutableOrNull(),
                                                                        Compilation)

                    Dim groupJoinArguments() As BoundExpression = {inner, outerKeyLambda, innerKeyLambda, inferenceLambda}

                    Dim useSiteDiagnostics As HashSet(Of DiagnosticInfo) = Nothing
                    Dim results As OverloadResolution.OverloadResolutionResult = OverloadResolution.QueryOperatorInvocationOverloadResolution(methodGroup,
                                                                                                                                              groupJoinArguments.AsImmutableOrNull(), Me,
                                                                                                                                              useSiteDiagnostics)

                    diagnostics.Add(groupJoin, useSiteDiagnostics)

                    If results.BestResult.HasValue Then
                        Dim method = DirectCast(results.BestResult.Value.Candidate.UnderlyingSymbol, MethodSymbol)
                        Dim resultSelector As TypeSymbol = method.Parameters(groupJoinArguments.Length - 1).Type

                        Dim resultSelectorDelegate = resultSelector.DelegateOrExpressionDelegate(Me)
                        groupType = resultSelectorDelegate.DelegateInvokeMethod.Parameters(1).Type

                    ElseIf Not outer.HasErrors Then
                        ReportDiagnostic(diagnostics,
                                         Location.Create(groupJoin.SyntaxTree, GetQueryOperatorNameSpan(groupJoin.GroupKeyword, groupJoin.JoinKeyword)),
                                         ERRID.ERR_QueryOperatorNotFound, StringConstants.GroupJoinMethod)
                    End If
                End If
            End If

            Debug.Assert(groupType IsNot Nothing)
            Return groupType
        End Function

        ''' <summary>
        ''' This is a helper method to create a BoundQueryLambda for an Into clause 
        ''' of a Group By or a Group Join operator. 
        ''' </summary>
        Private Function BindIntoSelectorLambda(
            clauseSyntax As QueryClauseSyntax,
            keysRangeVariables As ImmutableArray(Of RangeVariableSymbol),
            keysCompoundVariableType As TypeSymbol,
            addKeysInScope As Boolean,
            declaredNames As HashSet(Of String),
            groupType As TypeSymbol,
            groupRangeVariables As ImmutableArray(Of RangeVariableSymbol),
            groupCompoundVariableType As TypeSymbol,
            aggregationVariables As SeparatedSyntaxList(Of AggregationRangeVariableSyntax),
            mustProduceFlatCompoundVariable As Boolean,
            diagnostics As DiagnosticBag,
            <Out()> ByRef intoBinder As IntoClauseBinder,
            <Out()> ByRef intoRangeVariables As ImmutableArray(Of RangeVariableSymbol)
        ) As BoundQueryLambda
            Debug.Assert(clauseSyntax.Kind = SyntaxKind.GroupByClause OrElse clauseSyntax.Kind = SyntaxKind.GroupJoinClause)
            Debug.Assert(mustProduceFlatCompoundVariable OrElse clauseSyntax.Kind = SyntaxKind.GroupJoinClause)
            Debug.Assert((declaredNames IsNot Nothing) = (clauseSyntax.Kind = SyntaxKind.GroupJoinClause))
            Debug.Assert(keysRangeVariables.Length > 0)
            Debug.Assert(intoBinder Is Nothing)
            Debug.Assert(intoRangeVariables.IsDefault)

            Dim keyParam As BoundLambdaParameterSymbol = CreateQueryLambdaParameterSymbol(GetQueryLambdaParameterName(keysRangeVariables), 0,
                                                                                          keysCompoundVariableType,
                                                                                          clauseSyntax, keysRangeVariables)

            Dim groupParam As BoundLambdaParameterSymbol = CreateQueryLambdaParameterSymbol(StringConstants.ItAnonymous, 1,
                                                                                            groupType, clauseSyntax)

            Debug.Assert(LambdaUtilities.IsNonUserCodeQueryLambda(clauseSyntax))
            Dim intoLambdaSymbol = Me.CreateQueryLambdaSymbol(clauseSyntax,
                                                              SynthesizedLambdaKind.GroupNonUserCodeQueryLambda,
                                                              ImmutableArray.Create(keyParam, groupParam))

            ' Create binder for the INTO lambda.
            Dim intoLambdaBinder As New QueryLambdaBinder(intoLambdaSymbol, ImmutableArray(Of RangeVariableSymbol).Empty)
            Dim groupReference = New BoundParameter(groupParam.Syntax, groupParam, False, groupParam.Type).MakeCompilerGenerated()

            intoBinder = New IntoClauseBinder(intoLambdaBinder,
                                              groupReference, groupRangeVariables, groupCompoundVariableType,
                                              If(addKeysInScope, keysRangeVariables.Concat(groupRangeVariables), groupRangeVariables))

            Dim intoSelector As BoundExpression = intoBinder.BindIntoSelector(clauseSyntax,
                                                                              keysRangeVariables,
                                                                              New BoundParameter(keyParam.Syntax, keyParam, False, keyParam.Type).MakeCompilerGenerated(),
                                                                              keysRangeVariables,
                                                                              Nothing,
                                                                              ImmutableArray(Of RangeVariableSymbol).Empty,
                                                                              declaredNames,
                                                                              aggregationVariables,
                                                                              mustProduceFlatCompoundVariable,
                                                                              intoRangeVariables,
                                                                              diagnostics)

            Dim intoLambda = CreateBoundQueryLambda(intoLambdaSymbol,
                                                    keysRangeVariables,
                                                    intoSelector,
                                                    exprIsOperandOfConditionalBranch:=False)

            intoLambdaSymbol.SetQueryLambdaReturnType(intoSelector.Type)
            intoLambda.SetWasCompilerGenerated()

            Return intoLambda
        End Function


        Private Sub VerifyRangeVariableName(rangeVar As RangeVariableSymbol, identifier As SyntaxToken, diagnostics As DiagnosticBag)
            Debug.Assert(identifier.Parent Is rangeVar.Syntax)

            If identifier.GetTypeCharacter() <> TypeCharacter.None Then
                ReportDiagnostic(diagnostics, identifier, ERRID.ERR_QueryAnonymousTypeDisallowsTypeChar)
            End If

            If Compilation.ObjectType.GetMembers(rangeVar.Name).Length > 0 Then
                ReportDiagnostic(diagnostics, identifier, ERRID.ERR_QueryInvalidControlVariableName1)
            Else
                VerifyNameShadowingInMethodBody(rangeVar, identifier, identifier, diagnostics)
            End If
        End Sub

        Private Function GetQueryLambdaParameterSyntax(syntaxNode As VisualBasicSyntaxNode, rangeVariables As ImmutableArray(Of RangeVariableSymbol)) As VisualBasicSyntaxNode
            If rangeVariables.Length = 1 Then
                Return rangeVariables(0).Syntax
            End If

            Return syntaxNode
        End Function

        Private Function CreateQueryLambdaParameterSymbol(
            name As String,
            ordinal As Integer,
            type As TypeSymbol,
            syntaxNode As VisualBasicSyntaxNode,
            rangeVariables As ImmutableArray(Of RangeVariableSymbol)
        ) As BoundLambdaParameterSymbol
            syntaxNode = Me.GetQueryLambdaParameterSyntax(syntaxNode, rangeVariables)
            Dim param = New BoundLambdaParameterSymbol(name, ordinal, type, isByRef:=False, syntaxNode:=syntaxNode, location:=syntaxNode.GetLocation())
            Return param
        End Function

        Private Function CreateQueryLambdaParameterSymbol(
            name As String,
            ordinal As Integer,
            type As TypeSymbol,
            syntaxNode As VisualBasicSyntaxNode
        ) As BoundLambdaParameterSymbol
            Dim param = New BoundLambdaParameterSymbol(name, ordinal, type, isByRef:=False, syntaxNode:=syntaxNode, location:=syntaxNode.GetLocation())
            Return param
        End Function

        Private Shared Function GetQueryLambdaParameterName(rangeVariables As ImmutableArray(Of RangeVariableSymbol)) As String
            Select Case rangeVariables.Length
                Case 0
                    Return StringConstants.ItAnonymous
                Case 1
                    Return rangeVariables(0).Name
                Case Else
                    Return StringConstants.It
            End Select
        End Function

        Private Shared Function GetQueryLambdaParameterNameLeft(rangeVariables As ImmutableArray(Of RangeVariableSymbol)) As String
            Select Case rangeVariables.Length
                Case 0
                    Return StringConstants.ItAnonymous
                Case 1
                    Return rangeVariables(0).Name
                Case Else
                    Return StringConstants.It1
            End Select
        End Function

        Private Shared Function GetQueryLambdaParameterNameRight(rangeVariables As ImmutableArray(Of RangeVariableSymbol)) As String
            Select Case rangeVariables.Length
                Case 0
                    Throw ExceptionUtilities.UnexpectedValue(rangeVariables.Length)
                Case 1
                    Return rangeVariables(0).Name
                Case Else
                    Return StringConstants.It2
            End Select
        End Function

        ''' <summary>
        ''' Given result of binding preceding query operators, the source, bind the following Where operator.
        ''' 
        '''     {Preceding query operators} Where {expression}
        ''' 
        ''' Ex: From a In AA Where a > 0 ==> AA.Where(Function(a) a > b)
        ''' 
        ''' </summary>
        Private Function BindWhereClause(
            source As BoundQueryClauseBase,
            where As WhereClauseSyntax,
            diagnostics As DiagnosticBag
        ) As BoundQueryClause
            Return BindFilterQueryOperator(source, where,
                                              StringConstants.WhereMethod, where.WhereKeyword.Span,
                                              where.Condition, diagnostics)
        End Function

        ''' <summary>
        ''' Given result of binding preceding query operators, the source, bind the following Skip While operator.
        ''' 
        '''     {Preceding query operators} Skip While {expression}
        ''' 
        ''' Ex: From a In AA Skip While a > 0 ==> AA.SkipWhile(Function(a) a > b)
        ''' 
        ''' </summary>
        Private Function BindSkipWhileClause(
            source As BoundQueryClauseBase,
            skipWhile As PartitionWhileClauseSyntax,
            diagnostics As DiagnosticBag
        ) As BoundQueryClause
            Return BindFilterQueryOperator(source, skipWhile,
                                              StringConstants.SkipWhileMethod,
                                              GetQueryOperatorNameSpan(skipWhile.SkipOrTakeKeyword, skipWhile.WhileKeyword),
                                              skipWhile.Condition, diagnostics)
        End Function

        Private Shared Function GetQueryOperatorNameSpan(ByRef left As SyntaxToken, ByRef right As SyntaxToken) As TextSpan
            Dim operatorNameSpan As TextSpan = left.Span

            If right.ValueText.Length > 0 Then
                operatorNameSpan = TextSpan.FromBounds(operatorNameSpan.Start, right.Span.End)
            End If

            Return operatorNameSpan
        End Function

        ''' <summary>
        ''' Given result of binding preceding query operators, the source, bind the following Take While operator.
        ''' 
        '''     {Preceding query operators} Take While {expression}
        ''' 
        ''' Ex: From a In AA Skip While a > 0 ==> AA.TakeWhile(Function(a) a > b)
        ''' 
        ''' </summary>
        Private Function BindTakeWhileClause(
            source As BoundQueryClauseBase,
            takeWhile As PartitionWhileClauseSyntax,
            diagnostics As DiagnosticBag
        ) As BoundQueryClause
            Return BindFilterQueryOperator(source, takeWhile,
                                              StringConstants.TakeWhileMethod,
                                              GetQueryOperatorNameSpan(takeWhile.SkipOrTakeKeyword, takeWhile.WhileKeyword),
                                              takeWhile.Condition, diagnostics)
        End Function

        ''' <summary>
        ''' This helper method does all the work to bind Where, Take While and Skip While query operators.
        ''' </summary>
        Private Function BindFilterQueryOperator(
            source As BoundQueryClauseBase,
            operatorSyntax As QueryClauseSyntax,
            operatorName As String,
            operatorNameLocation As TextSpan,
            condition As ExpressionSyntax,
            diagnostics As DiagnosticBag
        ) As BoundQueryClause
            Dim suppressDiagnostics As DiagnosticBag = Nothing

            ' Create LambdaSymbol for the shape of the filter lambda.

            Dim param As BoundLambdaParameterSymbol = CreateQueryLambdaParameterSymbol(GetQueryLambdaParameterName(source.RangeVariables), 0,
                                                                                       source.CompoundVariableType,
                                                                                       condition, source.RangeVariables)

            Debug.Assert(LambdaUtilities.IsLambdaBody(condition))
            Dim lambdaSymbol = Me.CreateQueryLambdaSymbol(condition,
                                                          SynthesizedLambdaKind.FilterConditionQueryLambda,
                                                          ImmutableArray.Create(param))

            ' Create binder for a filter condition.
            Dim filterBinder As New QueryLambdaBinder(lambdaSymbol, source.RangeVariables)

            ' Bind condition as a value, conversion should take care of the rest (making it an RValue, etc.). 
            Dim predicate As BoundExpression = filterBinder.BindValue(condition, diagnostics)

            ' Need to verify result type of the condition and enforce ExprIsOperandOfConditionalBranch for possible future conversions. 
            ' In order to do verification, we simply attempt conversion to boolean in the same manner as BindBooleanExpression.
            Dim conversionDiagnostic = DiagnosticBag.GetInstance()

            Dim boolSymbol As NamedTypeSymbol = GetSpecialType(SpecialType.System_Boolean, condition, diagnostics)

            ' If predicate has type Object we will keep result of conversion, otherwise we drop it.
            Dim predicateType As TypeSymbol = predicate.Type
            Dim keepConvertedPredicate As Boolean = False

            If predicateType Is Nothing Then
                If predicate.IsNothingLiteral() Then
                    keepConvertedPredicate = True
                End If
            ElseIf predicateType.IsObjectType() Then
                keepConvertedPredicate = True
            End If

            Dim convertedToBoolean As BoundExpression = filterBinder.ApplyImplicitConversion(condition,
                                                                                             boolSymbol, predicate,
                                                                                             conversionDiagnostic, isOperandOfConditionalBranch:=True)

            ' If we don't keep result of the conversion, keep diagnostic if conversion failed.
            If keepConvertedPredicate Then
                predicate = convertedToBoolean
                diagnostics.AddRange(conversionDiagnostic)

            ElseIf convertedToBoolean.HasErrors AndAlso conversionDiagnostic.HasAnyErrors() Then
                diagnostics.AddRange(conversionDiagnostic)
                ' Suppress any additional diagnostic, otherwise we might end up with duplicate errors.
                If suppressDiagnostics Is Nothing Then
                    suppressDiagnostics = DiagnosticBag.GetInstance()
                    diagnostics = suppressDiagnostics
                End If
            End If

            conversionDiagnostic.Free()

            ' Bind the Filter
            Dim filterLambda = CreateBoundQueryLambda(lambdaSymbol,
                                                      source.RangeVariables,
                                                      predicate,
                                                      exprIsOperandOfConditionalBranch:=True)

            filterLambda.SetWasCompilerGenerated()

            ' Now bind the call.
            Dim boundCallOrBadExpression As BoundExpression

            If source.Type.IsErrorType() Then
                boundCallOrBadExpression = BadExpression(operatorSyntax, ImmutableArray.Create(Of BoundNode)(source, filterLambda),
                                                         ErrorTypeSymbol.UnknownResultType).MakeCompilerGenerated()
            Else
                If suppressDiagnostics Is Nothing AndAlso ShouldSuppressDiagnostics(filterLambda) Then
                    ' Operator BindQueryClauseCall will fail, let's suppress any additional errors it will report.
                    suppressDiagnostics = DiagnosticBag.GetInstance()
                    diagnostics = suppressDiagnostics
                End If

                boundCallOrBadExpression = BindQueryOperatorCall(operatorSyntax, source,
                                                                 operatorName,
                                                                 ImmutableArray.Create(Of BoundExpression)(filterLambda),
                                                                 operatorNameLocation,
                                                                 diagnostics)
            End If

            If suppressDiagnostics IsNot Nothing Then
                suppressDiagnostics.Free()
            End If

            Return New BoundQueryClause(operatorSyntax,
                                        boundCallOrBadExpression,
                                        source.RangeVariables,
                                        source.CompoundVariableType,
                                        ImmutableArray.Create(Of Binder)(filterBinder),
                                        boundCallOrBadExpression.Type)
        End Function


        ''' <summary>
        ''' Given result of binding preceding query operators, the source, bind the following Distinct operator.
        ''' 
        '''     {Preceding query operators} Distinct
        ''' 
        ''' Ex: From a In AA Distinct ==> AA.Distinct()
        ''' 
        ''' </summary>
        Private Function BindDistinctClause(
            source As BoundQueryClauseBase,
            distinct As DistinctClauseSyntax,
            diagnostics As DiagnosticBag
        ) As BoundQueryClause

            Dim boundCallOrBadExpression As BoundExpression

            If source.Type.IsErrorType() Then
                ' Operator BindQueryClauseCall will fail, let's not bother.
                boundCallOrBadExpression = BadExpression(distinct, ImmutableArray.Create(Of BoundNode)(source),
                                                         ErrorTypeSymbol.UnknownResultType).MakeCompilerGenerated()
            Else
                boundCallOrBadExpression = BindQueryOperatorCall(distinct, source,
                                                                 StringConstants.DistinctMethod,
                                                                 ImmutableArray(Of BoundExpression).Empty,
                                                                 distinct.DistinctKeyword.Span,
                                                                 diagnostics)
            End If

            Return New BoundQueryClause(distinct,
                                        boundCallOrBadExpression,
                                        source.RangeVariables,
                                        source.CompoundVariableType,
                                        ImmutableArray(Of Binder).Empty,
                                        boundCallOrBadExpression.Type)
        End Function

        ''' <summary>
        ''' Given result of binding preceding query operators, the source, bind the following Skip operator.
        ''' 
        '''     {Preceding query operators} Skip {expression}
        ''' 
        ''' Ex: From a In AA Skip 10 ==> AA.Skip(10)
        ''' 
        ''' </summary>
        Private Function BindSkipClause(
            source As BoundQueryClauseBase,
            skip As PartitionClauseSyntax,
            diagnostics As DiagnosticBag
        ) As BoundQueryClause
            Return BindPartitionClause(source, skip, StringConstants.SkipMethod, diagnostics)
        End Function

        ''' <summary>
        ''' Given result of binding preceding query operators, the source, bind the following Take operator.
        ''' 
        '''     {Preceding query operators} Take {expression}
        ''' 
        ''' Ex: From a In AA Take 10 ==> AA.Take(10)
        ''' 
        ''' </summary>
        Private Function BindTakeClause(
            source As BoundQueryClauseBase,
            take As PartitionClauseSyntax,
            diagnostics As DiagnosticBag
        ) As BoundQueryClause
            Return BindPartitionClause(source, take, StringConstants.TakeMethod, diagnostics)
        End Function

        ''' <summary>
        ''' This helper method does all the work to bind Take and Skip query operators.
        ''' </summary>
        Private Function BindPartitionClause(
            source As BoundQueryClauseBase,
            partition As PartitionClauseSyntax,
            operatorName As String,
            diagnostics As DiagnosticBag
        ) As BoundQueryClause

            ' Bind the Count expression as a value, conversion should take care of the rest (making it an RValue, etc.). 
            Dim boundCount As BoundExpression = Me.BindValue(partition.Count, diagnostics)

            ' Now bind the call.
            Dim boundCallOrBadExpression As BoundExpression

            If source.Type.IsErrorType() Then
                boundCallOrBadExpression = BadExpression(partition, ImmutableArray.Create(Of BoundNode)(source, boundCount),
                                                         ErrorTypeSymbol.UnknownResultType).MakeCompilerGenerated()
            Else
                Dim suppressDiagnostics As DiagnosticBag = Nothing

                If boundCount.HasErrors OrElse (boundCount.Type IsNot Nothing AndAlso boundCount.Type.IsErrorType()) Then
                    ' Operator BindQueryClauseCall will fail, let's suppress any additional errors it will report.
                    suppressDiagnostics = DiagnosticBag.GetInstance()
                    diagnostics = suppressDiagnostics
                End If

                boundCallOrBadExpression = BindQueryOperatorCall(partition, source,
                                                               operatorName,
                                                               ImmutableArray.Create(Of BoundExpression)(boundCount),
                                                               partition.SkipOrTakeKeyword.Span,
                                                               diagnostics)

                If suppressDiagnostics IsNot Nothing Then
                    suppressDiagnostics.Free()
                End If
            End If

            Return New BoundQueryClause(partition,
                                        boundCallOrBadExpression,
                                        source.RangeVariables,
                                        source.CompoundVariableType,
                                        ImmutableArray(Of Binder).Empty,
                                        boundCallOrBadExpression.Type)
        End Function


        ''' <summary>
        ''' Given result of binding preceding query operators, the source, bind the following Order By operator.
        ''' 
        '''     {Preceding query operators} Order By {orderings}
        ''' 
        ''' Ex: From a In AA Order By a ==> AA.OrderBy(Function(a) a)
        ''' 
        ''' Ex: From a In AA Order By a.Key1, a.Key2 Descending ==> AA.OrderBy(Function(a) a.Key1).ThenByDescending(Function(a) a.Key2)
        ''' 
        ''' </summary>
        Private Function BindOrderByClause(
            source As BoundQueryClauseBase,
            orderBy As OrderByClauseSyntax,
            diagnostics As DiagnosticBag
        ) As BoundQueryClause
            Dim suppressDiagnostics As DiagnosticBag = Nothing
            Dim callDiagnostics As DiagnosticBag = diagnostics

            Dim lambdaParameterName As String = GetQueryLambdaParameterName(source.RangeVariables)

            Dim sourceOrPreviousOrdering As BoundQueryPart = source
            Dim orderByOrderings As SeparatedSyntaxList(Of OrderingSyntax) = orderBy.Orderings

            If orderByOrderings.Count = 0 Then
                ' Malformed tree.
                Debug.Assert(orderByOrderings.Count > 0, "Malformed syntax tree.")
                Return New BoundQueryClause(orderBy,
                                            BadExpression(orderBy, source, ErrorTypeSymbol.UnknownResultType).MakeCompilerGenerated(),
                                            source.RangeVariables,
                                            source.CompoundVariableType,
                                            ImmutableArray.Create(Me),
                                            ErrorTypeSymbol.UnknownResultType,
                                            hasErrors:=True)
            End If

            Dim keyBinder As QueryLambdaBinder = Nothing

            For i = 0 To orderByOrderings.Count - 1
                Dim ordering As OrderingSyntax = orderByOrderings(i)

                ' Create LambdaSymbol for the shape of the key lambda.
                Dim param As BoundLambdaParameterSymbol = CreateQueryLambdaParameterSymbol(lambdaParameterName, 0,
                                                                                           source.CompoundVariableType,
                                                                                           ordering.Expression, source.RangeVariables)

                Dim lambdaSymbol = Me.CreateQueryLambdaSymbol(LambdaUtilities.GetOrderingLambdaBody(ordering),
                                                              SynthesizedLambdaKind.OrderingQueryLambda,
                                                              ImmutableArray.Create(param))

                ' Create binder for a key expression.
                keyBinder = New QueryLambdaBinder(lambdaSymbol, source.RangeVariables)

                ' Bind expression as a value, conversion during overload resolution should take care of the rest (making it an RValue, etc.). 
                Dim key As BoundExpression = keyBinder.BindValue(ordering.Expression, diagnostics)

                Dim keyLambda = CreateBoundQueryLambda(lambdaSymbol,
                                                       source.RangeVariables,
                                                       key,
                                                       exprIsOperandOfConditionalBranch:=False)

                keyLambda.SetWasCompilerGenerated()

                ' Now bind the call.
                Dim boundCallOrBadExpression As BoundExpression

                If sourceOrPreviousOrdering.Type.IsErrorType() Then
                    boundCallOrBadExpression = BadExpression(ordering, ImmutableArray.Create(Of BoundNode)(sourceOrPreviousOrdering, keyLambda),
                                                             ErrorTypeSymbol.UnknownResultType).MakeCompilerGenerated()
                Else
                    If suppressDiagnostics Is Nothing AndAlso ShouldSuppressDiagnostics(keyLambda) Then
                        ' Operator BindQueryClauseCall will fail, let's suppress any additional errors it will report.
                        suppressDiagnostics = DiagnosticBag.GetInstance()
                        callDiagnostics = suppressDiagnostics
                    End If

                    Dim operatorName As String
                    Dim operatorNameLocation As TextSpan

                    If i = 0 Then
                        ' This is the first ordering.
                        If ordering.Kind = SyntaxKind.AscendingOrdering Then
                            operatorName = StringConstants.OrderByMethod
                        Else
                            Debug.Assert(ordering.Kind = SyntaxKind.DescendingOrdering)
                            operatorName = StringConstants.OrderByDescendingMethod
                        End If

                        operatorNameLocation = GetQueryOperatorNameSpan(orderBy.OrderKeyword, orderBy.ByKeyword)
                    Else
                        If ordering.Kind = SyntaxKind.AscendingOrdering Then
                            operatorName = StringConstants.ThenByMethod
                        Else
                            Debug.Assert(ordering.Kind = SyntaxKind.DescendingOrdering)
                            operatorName = StringConstants.ThenByDescendingMethod
                        End If

                        operatorNameLocation = orderByOrderings.GetSeparator(i - 1).Span
                    End If

                    boundCallOrBadExpression = BindQueryOperatorCall(ordering, sourceOrPreviousOrdering,
                                                                   operatorName,
                                                                   ImmutableArray.Create(Of BoundExpression)(keyLambda),
                                                                   operatorNameLocation,
                                                                   callDiagnostics)
                End If

                sourceOrPreviousOrdering = New BoundOrdering(ordering, boundCallOrBadExpression, boundCallOrBadExpression.Type)
            Next

            If suppressDiagnostics IsNot Nothing Then
                suppressDiagnostics.Free()
            End If

            Debug.Assert(sourceOrPreviousOrdering IsNot source)
            Debug.Assert(keyBinder IsNot Nothing)

            Return New BoundQueryClause(orderBy,
                                        sourceOrPreviousOrdering,
                                        source.RangeVariables,
                                        source.CompoundVariableType,
                                        ImmutableArray.Create(Of Binder)(keyBinder),
                                        sourceOrPreviousOrdering.Type)
        End Function


        ''' <summary>
        ''' This is a top level binder used to bind bodies of query lambdas.
        ''' It also contains a bunch of helper methods to bind bodies of a particular kind.
        ''' </summary>
        Private NotInheritable Class QueryLambdaBinder
            Inherits BlockBaseBinder(Of RangeVariableSymbol) ' BlockBaseBinder implements various lookup methods as we need.

            Private ReadOnly _lambdaSymbol As LambdaSymbol
            Private ReadOnly _rangeVariables As ImmutableArray(Of RangeVariableSymbol)

            Public Sub New(lambdaSymbol As LambdaSymbol, rangeVariables As ImmutableArray(Of RangeVariableSymbol))
                MyBase.New(lambdaSymbol.ContainingBinder)
                _lambdaSymbol = lambdaSymbol
                _rangeVariables = rangeVariables
            End Sub

            Friend Overrides ReadOnly Property Locals As ImmutableArray(Of RangeVariableSymbol)
                Get
                    Return _rangeVariables
                End Get
            End Property

            Public ReadOnly Property RangeVariables As ImmutableArray(Of RangeVariableSymbol)
                Get
                    Return _rangeVariables
                End Get
            End Property

            Public Overrides ReadOnly Property ContainingMember As Symbol
                Get
                    Return _lambdaSymbol
                End Get
            End Property

            Public ReadOnly Property LambdaSymbol As LambdaSymbol
                Get
                    Return _lambdaSymbol
                End Get
            End Property

            Public Overrides ReadOnly Property IsInQuery As Boolean
                Get
                    Return True
                End Get
            End Property

            ''' <summary>
            ''' Bind body of a lambda representing Select operator selector in context of this binder.
            ''' </summary>
            Public Function BindSelectClauseSelector(
                [select] As SelectClauseSyntax,
                operatorsEnumerator As SyntaxList(Of QueryClauseSyntax).Enumerator,
                <Out()> ByRef declaredRangeVariables As ImmutableArray(Of RangeVariableSymbol),
                diagnostics As DiagnosticBag
            ) As BoundExpression
                Debug.Assert(declaredRangeVariables.IsDefault)

                Dim selectVariables As SeparatedSyntaxList(Of ExpressionRangeVariableSyntax) = [select].Variables
                Dim requireRangeVariable As Boolean

                If selectVariables.Count = 1 Then
                    requireRangeVariable = False

                    ' If there is a Join or a GroupJoin ahead, this operator must
                    ' add a range variable into the scope.
                    While operatorsEnumerator.MoveNext()
                        Select Case operatorsEnumerator.Current.Kind
                            Case SyntaxKind.SimpleJoinClause, SyntaxKind.GroupJoinClause
                                requireRangeVariable = True
                                Exit While

                            Case SyntaxKind.SelectClause, SyntaxKind.LetClause, SyntaxKind.FromClause
                                Exit While

                            Case SyntaxKind.AggregateClause
                                Exit While

                            Case SyntaxKind.GroupByClause
                                Exit While
                        End Select
                    End While
                Else
                    requireRangeVariable = True
                End If

                Return BindExpressionRangeVariables(selectVariables,
                                                    requireRangeVariable,
                                                    [select],
                                                    declaredRangeVariables,
                                                    diagnostics)
            End Function

            ''' <summary>
            ''' Bind Select like selector based on the set of expression range variables in context of this binder.
            ''' </summary>
            Public Function BindExpressionRangeVariables(
                selectVariables As SeparatedSyntaxList(Of ExpressionRangeVariableSyntax),
                requireRangeVariable As Boolean,
                selectorSyntax As QueryClauseSyntax,
                <Out()> ByRef declaredRangeVariables As ImmutableArray(Of RangeVariableSymbol),
                diagnostics As DiagnosticBag
            ) As BoundExpression
                Debug.Assert(selectorSyntax IsNot Nothing AndAlso declaredRangeVariables.IsDefault)

                Dim selector As BoundExpression

                If selectVariables.Count = 1 Then
                    ' Simple case, one item in the projection.
                    Dim item As ExpressionRangeVariableSyntax = selectVariables(0)
                    selector = Nothing ' Suppress definite assignment error.

                    Debug.Assert(item.NameEquals Is Nothing OrElse item.NameEquals.AsClause Is Nothing)

                    ' Using enclosing binder for shadowing check, because range variables brought in scope by the current binder
                    ' are no longer in scope after the Select. So, it is fine to shadow them. 
                    Dim rangeVar As RangeVariableSymbol = Me.BindExpressionRangeVariable(item, requireRangeVariable, Me.ContainingBinder, Nothing, selector, diagnostics)

                    If rangeVar IsNot Nothing Then
                        declaredRangeVariables = ImmutableArray.Create(Of RangeVariableSymbol)(rangeVar)
                    Else
                        Debug.Assert(Not requireRangeVariable)
                        declaredRangeVariables = ImmutableArray(Of RangeVariableSymbol).Empty
                    End If

                ElseIf selectVariables.Count = 0 Then
                    ' Malformed tree
                    Debug.Assert(selectVariables.Count > 0, "Malformed syntax tree.")
                    selector = BadExpression(selectorSyntax, ErrorTypeSymbol.UnknownResultType).MakeCompilerGenerated()
                    declaredRangeVariables = ImmutableArray.Create(RangeVariableSymbol.CreateForErrorRecovery(Me, selectorSyntax, selector.Type))
                Else
                    ' Need to build an Anonymous Type
                    Dim selectors = New BoundExpression(selectVariables.Count - 1) {}
                    Dim declaredNames As HashSet(Of String) = CreateSetOfDeclaredNames()
                    Dim rangeVariables(selectVariables.Count - 1) As RangeVariableSymbol

                    For i As Integer = 0 To selectVariables.Count - 1
                        Debug.Assert(selectVariables(i).NameEquals Is Nothing OrElse selectVariables(i).NameEquals.AsClause Is Nothing)

                        ' Using enclosing binder for shadowing check, because range variables brought in scope by the current binder
                        ' are no longer in scope after the Select. So, it is fine to shadow them. 
                        Dim rangeVar As RangeVariableSymbol = Me.BindExpressionRangeVariable(selectVariables(i), True, Me.ContainingBinder, declaredNames, selectors(i), diagnostics)
                        Debug.Assert(rangeVar IsNot Nothing)
                        rangeVariables(i) = rangeVar
                    Next

                    AssertDeclaredNames(declaredNames, rangeVariables.AsImmutableOrNull())

                    Dim fields = New AnonymousTypeField(selectVariables.Count - 1) {}

                    For i As Integer = 0 To rangeVariables.Length - 1
                        Dim rangeVar As RangeVariableSymbol = rangeVariables(i)
                        fields(i) = New AnonymousTypeField(
                            rangeVar.Name, rangeVar.Type, rangeVar.Syntax.GetLocation(), isKeyOrByRef:=True)
                    Next

                    selector = BindAnonymousObjectCreationExpression(selectorSyntax,
                                                                     New AnonymousTypeDescriptor(fields.AsImmutableOrNull(),
                                                                                                 selectorSyntax.QueryClauseKeywordOrRangeVariableIdentifier.GetLocation(),
                                                                                                 True),
                                                                     selectors.AsImmutableOrNull(),
                                                                     diagnostics).MakeCompilerGenerated()

                    declaredRangeVariables = rangeVariables.AsImmutableOrNull()
                End If

                Debug.Assert(Not declaredRangeVariables.IsDefault AndAlso selectorSyntax IsNot Nothing)
                Return selector
            End Function

            ''' <summary>
            ''' Bind ExpressionRangeVariableSyntax in context of this binder.
            ''' </summary>
            Private Function BindExpressionRangeVariable(
                item As ExpressionRangeVariableSyntax,
                requireRangeVariable As Boolean,
                shadowingCheckBinder As Binder,
                declaredNames As HashSet(Of String),
                <Out()> ByRef selector As BoundExpression,
                diagnostics As DiagnosticBag
            ) As RangeVariableSymbol
                Debug.Assert(selector Is Nothing)

                Dim variableName As VariableNameEqualsSyntax = item.NameEquals

                ' Figure out the name of the new range variable
                Dim rangeVarName As String = Nothing
                Dim rangeVarNameSyntax As SyntaxToken = Nothing
                Dim targetVariableType As TypeSymbol = Nothing

                If variableName IsNot Nothing Then
                    rangeVarNameSyntax = variableName.Identifier.Identifier
                    rangeVarName = rangeVarNameSyntax.ValueText

                    ' Deal with AsClauseOpt and various modifiers.
                    If variableName.AsClause IsNot Nothing Then
                        targetVariableType = DecodeModifiedIdentifierType(variableName.Identifier,
                                                                          variableName.AsClause,
                                                                          Nothing,
                                                                          Nothing,
                                                                          diagnostics,
                                                                          ModifiedIdentifierTypeDecoderContext.LocalType Or
                                                                          ModifiedIdentifierTypeDecoderContext.QueryRangeVariableType)

                    ElseIf variableName.Identifier.Nullable.Node IsNot Nothing Then
                        ReportDiagnostic(diagnostics, variableName.Identifier.Nullable, ERRID.ERR_NullableTypeInferenceNotSupported)
                    End If
                Else
                    ' Infer the name from expression
                    Dim failedToInferFromXmlName As XmlNameSyntax = Nothing
                    Dim nameToken As SyntaxToken = item.Expression.ExtractAnonymousTypeMemberName(failedToInferFromXmlName)

                    If nameToken.Kind <> SyntaxKind.None Then
                        rangeVarNameSyntax = nameToken
                        rangeVarName = rangeVarNameSyntax.ValueText
                    ElseIf requireRangeVariable Then
                        If failedToInferFromXmlName IsNot Nothing Then
                            ReportDiagnostic(diagnostics, failedToInferFromXmlName.LocalName, ERRID.ERR_QueryAnonTypeFieldXMLNameInference)
                        Else
                            ReportDiagnostic(diagnostics, item.Expression, ERRID.ERR_QueryAnonymousTypeFieldNameInference)
                        End If
                    End If
                End If

                If rangeVarName IsNot Nothing AndAlso rangeVarName.Length = 0 Then
                    ' Empty string must have been a syntax error. 
                    rangeVarName = Nothing
                End If

                selector = BindValue(item.Expression, diagnostics)

                If targetVariableType Is Nothing Then
                    selector = MakeRValue(selector, diagnostics)
                Else
                    selector = ApplyImplicitConversion(item.Expression, targetVariableType, selector, diagnostics)
                End If

                Dim rangeVar As RangeVariableSymbol = Nothing

                If rangeVarName IsNot Nothing Then

                    rangeVar = RangeVariableSymbol.Create(Me, rangeVarNameSyntax, selector.Type)

                    ' Note what we are doing here:
                    ' We are creating BoundRangeVariableAssignment before doing any shadowing checks
                    ' so that SemanticModel can find the declared symbol, but, if the variable will conflict with another
                    ' variable in the same child scope, we will not add it to the scope. Instead, we create special
                    ' error recovery range variable symbol and add it to the scope at the same place, making sure 
                    ' that the earlier declared range variable wins during name lookup.
                    ' As an alternative, we could still add the original range variable symbol to the scope and then,
                    ' while we are binding the rest of the query in error recovery mode, references to the name would 
                    ' cause ambiguity. However, this could negatively affect IDE experience. Also, as we build an
                    ' Anonymous Type for the compound range variables, we would end up with a type with duplicate members,
                    ' which could cause problems elsewhere.

                    selector = New BoundRangeVariableAssignment(item, rangeVar, selector, selector.Type)

                    Dim doErrorRecovery As Boolean = False

                    If declaredNames IsNot Nothing AndAlso Not declaredNames.Add(rangeVarName) Then
                        Debug.Assert(item.Parent.Kind = SyntaxKind.SelectClause OrElse item.Parent.Kind = SyntaxKind.GroupByClause)
                        ReportDiagnostic(diagnostics, rangeVarNameSyntax, ERRID.ERR_QueryDuplicateAnonTypeMemberName1, rangeVarName)
                        doErrorRecovery = True  ' Shouldn't add to the scope.
                    Else
                        shadowingCheckBinder.VerifyRangeVariableName(rangeVar, rangeVarNameSyntax, diagnostics)

                        If item.Parent.Kind = SyntaxKind.LetClause AndAlso ShadowsRangeVariableInTheChildScope(shadowingCheckBinder, rangeVar) Then
                            ' We are about to add this range variable to the current child scope.
                            ' Shadowing error was reported earlier.
                            doErrorRecovery = True  ' Shouldn't add to the scope.
                        End If
                    End If

                    If doErrorRecovery Then
                        If requireRangeVariable Then
                            rangeVar = RangeVariableSymbol.CreateForErrorRecovery(Me, rangeVar.Syntax, selector.Type)
                        Else
                            rangeVar = Nothing
                        End If
                    End If

                ElseIf requireRangeVariable Then
                    Debug.Assert(rangeVar Is Nothing)
                    rangeVar = RangeVariableSymbol.CreateForErrorRecovery(Me, item, selector.Type)
                End If

                Debug.Assert(selector IsNot Nothing)
                Debug.Assert(Not requireRangeVariable OrElse rangeVar IsNot Nothing)
                Return rangeVar
            End Function

            ''' <summary>
            ''' Bind Let operator selector for a particular ExpressionRangeVariableSyntax.
            ''' Takes care of "carrying over" of previously declared range variables as well as introduction of the new one.
            ''' </summary>
            Public Function BindLetClauseVariableSelector(
                variable As ExpressionRangeVariableSyntax,
                operatorsEnumerator As SyntaxList(Of QueryClauseSyntax).Enumerator,
                <Out()> ByRef declaredRangeVariables As ImmutableArray(Of RangeVariableSymbol),
                diagnostics As DiagnosticBag
            ) As BoundExpression
                Debug.Assert(declaredRangeVariables.IsDefault)

                Dim selector As BoundExpression = Nothing

                ' Using selector binder for shadowing checks because all its range variables stay in scope
                ' after the operator.
                Dim rangeVar As RangeVariableSymbol = Me.BindExpressionRangeVariable(variable, True, Me, Nothing, selector, diagnostics)
                Debug.Assert(rangeVar IsNot Nothing)

                declaredRangeVariables = ImmutableArray.Create(rangeVar)

                If _rangeVariables.Length > 0 Then
                    ' Need to build an Anonymous Type.

                    ' If it is not the last variable in the list, we simply combine source's
                    ' compound variable (an instance of its Anonymous Type) with our new variable, 
                    ' creating new compound variable of nested Anonymous Type.

                    ' In some scenarios, it is safe to leave compound variable in nested form when there is an
                    ' operator down the road that does its own projection (Select, Group By, ...). 
                    ' All following operators have to take an Anonymous Type in both cases and, since there is no way to
                    ' restrict the shape of the Anonymous Type in method's declaration, the operators should be
                    ' insensitive to the shape of the Anonymous Type.
                    selector = BuildJoinSelector(variable,
                                                 (variable Is DirectCast(operatorsEnumerator.Current, LetClauseSyntax).Variables.Last() AndAlso
                                                      MustProduceFlatCompoundVariable(operatorsEnumerator)),
                                                 diagnostics,
                                                 rangeVar, selector)

                Else
                    ' Easy case, no need to build an Anonymous Type.
                    Debug.Assert(_rangeVariables.Length = 0)
                End If

                Debug.Assert(Not declaredRangeVariables.IsDefault)
                Return selector
            End Function


            ''' <summary>
            ''' Bind body of a lambda representing first Select operator selector for an aggregate clause in context of this binder.
            ''' </summary>
            Public Function BindAggregateClauseFirstSelector(
                aggregate As AggregateClauseSyntax,
                operatorsEnumerator As SyntaxList(Of QueryClauseSyntax).Enumerator,
                rangeVariablesPart1 As ImmutableArray(Of RangeVariableSymbol),
                rangeVariablesPart2 As ImmutableArray(Of RangeVariableSymbol),
                <Out()> ByRef declaredRangeVariables As ImmutableArray(Of RangeVariableSymbol),
                <Out()> ByRef group As BoundQueryClauseBase,
                <Out()> ByRef intoBinder As IntoClauseDisallowGroupReferenceBinder,
                diagnostics As DiagnosticBag
            ) As BoundExpression
                Debug.Assert(operatorsEnumerator.Current Is aggregate)
                Debug.Assert(declaredRangeVariables.IsDefault)
                Debug.Assert((rangeVariablesPart1.Length = 0) = (rangeVariablesPart2 = _rangeVariables))
                Debug.Assert((rangeVariablesPart2.Length = 0) = (rangeVariablesPart1 = _rangeVariables))
                Debug.Assert(_lambdaSymbol.ParameterCount = If(rangeVariablesPart2.Length = 0, 1, 2))
                Debug.Assert(_rangeVariables.Length = rangeVariablesPart1.Length + rangeVariablesPart2.Length)

                ' Let's interpret our group.
                Dim groupAdditionalOperators As SyntaxList(Of QueryClauseSyntax).Enumerator = aggregate.AdditionalQueryOperators.GetEnumerator()

                ' Note, this call can advance [groupAdditionalOperators] enumerator if it absorbs the following Let or Select.
                group = BindCollectionRangeVariables(aggregate, Nothing, aggregate.Variables, groupAdditionalOperators, diagnostics)
                group = BindSubsequentQueryOperators(group, groupAdditionalOperators, diagnostics)

                ' Now, deal with aggregate functions.
                Dim aggregationVariables As SeparatedSyntaxList(Of AggregationRangeVariableSyntax) = aggregate.AggregationVariables
                Dim letSelector As BoundExpression
                Dim groupRangeVar As RangeVariableSymbol = Nothing

                Select Case aggregationVariables.Count
                    Case 0
                        ' Malformed syntax tree.
                        Debug.Assert(aggregationVariables.Count > 0, "Malformed syntax tree.")
                        declaredRangeVariables = ImmutableArray.Create(Of RangeVariableSymbol)(
                                       RangeVariableSymbol.CreateForErrorRecovery(Me,
                                                                                  aggregate,
                                                                                  ErrorTypeSymbol.UnknownResultType))

                        letSelector = BadExpression(aggregate, group, ErrorTypeSymbol.UnknownResultType).MakeCompilerGenerated()

                        intoBinder = New IntoClauseDisallowGroupReferenceBinder(New QueryLambdaBinder(_lambdaSymbol,
                                                                                                      ImmutableArray(Of RangeVariableSymbol).Empty),
                                                                                group, group.RangeVariables, group.CompoundVariableType,
                                                                                _rangeVariables.Concat(group.RangeVariables))

                    Case 1
                        ' Simple case - one aggregate function.
                        '
                        ' FROM a in AA              FROM a in AA
                        ' AGGREGATE b in a.BB  =>   LET count = (FROM b IN a.BB).Count()
                        ' INTO Count()
                        '
                        intoBinder = New IntoClauseDisallowGroupReferenceBinder(New QueryLambdaBinder(_lambdaSymbol,
                                                                                                      ImmutableArray(Of RangeVariableSymbol).Empty),
                                                                                group, group.RangeVariables, group.CompoundVariableType,
                                                                                _rangeVariables.Concat(group.RangeVariables))

                        Dim compoundKeyReferencePart1 As BoundExpression
                        Dim keysRangeVariablesPart1 As ImmutableArray(Of RangeVariableSymbol)
                        Dim compoundKeyReferencePart2 As BoundExpression
                        Dim keysRangeVariablesPart2 As ImmutableArray(Of RangeVariableSymbol)
                        Dim letSelectorParam As BoundLambdaParameterSymbol

                        If rangeVariablesPart1.Length > 0 Then
                            letSelectorParam = DirectCast(_lambdaSymbol.Parameters(0), BoundLambdaParameterSymbol)
                            compoundKeyReferencePart1 = New BoundParameter(letSelectorParam.Syntax, letSelectorParam, False, letSelectorParam.Type).MakeCompilerGenerated()
                            keysRangeVariablesPart1 = rangeVariablesPart1

                            If rangeVariablesPart2.Length > 0 Then
                                letSelectorParam = DirectCast(_lambdaSymbol.Parameters(1), BoundLambdaParameterSymbol)
                                compoundKeyReferencePart2 = New BoundParameter(letSelectorParam.Syntax, letSelectorParam, False, letSelectorParam.Type).MakeCompilerGenerated()
                                keysRangeVariablesPart2 = rangeVariablesPart2
                            Else
                                compoundKeyReferencePart2 = Nothing
                                keysRangeVariablesPart2 = ImmutableArray(Of RangeVariableSymbol).Empty
                            End If
                        ElseIf rangeVariablesPart2.Length > 0 Then
                            letSelectorParam = DirectCast(_lambdaSymbol.Parameters(1), BoundLambdaParameterSymbol)
                            compoundKeyReferencePart1 = New BoundParameter(letSelectorParam.Syntax, letSelectorParam, False, letSelectorParam.Type).MakeCompilerGenerated()
                            keysRangeVariablesPart1 = rangeVariablesPart2
                            compoundKeyReferencePart2 = Nothing
                            keysRangeVariablesPart2 = ImmutableArray(Of RangeVariableSymbol).Empty
                        Else
                            compoundKeyReferencePart1 = Nothing
                            keysRangeVariablesPart1 = ImmutableArray(Of RangeVariableSymbol).Empty
                            compoundKeyReferencePart2 = Nothing
                            keysRangeVariablesPart2 = ImmutableArray(Of RangeVariableSymbol).Empty
                        End If

                        letSelector = intoBinder.BindIntoSelector(aggregate,
                                                                  _rangeVariables,
                                                                  compoundKeyReferencePart1,
                                                                  keysRangeVariablesPart1,
                                                                  compoundKeyReferencePart2,
                                                                  keysRangeVariablesPart2,
                                                                  Nothing,
                                                                  aggregationVariables,
                                                                  MustProduceFlatCompoundVariable(operatorsEnumerator),
                                                                  declaredRangeVariables,
                                                                  diagnostics)

                    Case Else

                        ' Complex case:
                        '
                        ' FROM a in AA              FROM a in AA
                        ' AGGREGATE b in a.BB  =>   LET Group = (FROM b IN a.BB)
                        ' INTO Count(),             Select a, Count=Group.Count(), Sum=Group.Sum(b=>b)
                        '      Sum(b)
                        '

                        ' Handle selector for the Let.
                        groupRangeVar = RangeVariableSymbol.CreateCompilerGenerated(Me, aggregate, StringConstants.Group, group.Type)

                        If _rangeVariables.Length = 0 Then
                            letSelector = group
                        Else
                            letSelector = BuildJoinSelector(aggregate,
                                                            mustProduceFlatCompoundVariable:=False, diagnostics:=diagnostics,
                                                            rangeVarOpt:=groupRangeVar, rangeVarValueOpt:=group)
                        End If

                        declaredRangeVariables = ImmutableArray.Create(Of RangeVariableSymbol)(groupRangeVar)
                        intoBinder = Nothing
                End Select

                Return letSelector
            End Function

            ''' <summary>
            ''' Bind Join/From selector that absorbs following Select/Let in context of this binder.
            ''' </summary>
            Public Function BindAbsorbingJoinSelector(
                absorbNextOperator As QueryClauseSyntax,
                operatorsEnumerator As SyntaxList(Of QueryClauseSyntax).Enumerator,
                leftRangeVariables As ImmutableArray(Of RangeVariableSymbol),
                rightRangeVariables As ImmutableArray(Of RangeVariableSymbol),
                <Out> ByRef joinSelectorDeclaredRangeVariables As ImmutableArray(Of RangeVariableSymbol),
                <Out> ByRef group As BoundQueryClauseBase,
                <Out> ByRef intoBinder As IntoClauseDisallowGroupReferenceBinder,
                diagnostics As DiagnosticBag
            ) As BoundExpression
                Debug.Assert(joinSelectorDeclaredRangeVariables.IsDefault)
                Debug.Assert(absorbNextOperator Is operatorsEnumerator.Current)

                group = Nothing
                intoBinder = Nothing

                Dim joinSelector As BoundExpression

                Select Case absorbNextOperator.Kind
                    Case SyntaxKind.SelectClause
                        ' Absorb Select
                        Dim [select] = DirectCast(absorbNextOperator, SelectClauseSyntax)
                        joinSelector = BindSelectClauseSelector([select],
                                                                operatorsEnumerator,
                                                                joinSelectorDeclaredRangeVariables,
                                                                diagnostics)

                    Case SyntaxKind.LetClause
                        ' Absorb Let
                        Dim [let] = DirectCast(absorbNextOperator, LetClauseSyntax)
                        Debug.Assert([let].Variables.Count > 0)

                        joinSelector = BindLetClauseVariableSelector([let].Variables.First,
                                                                     operatorsEnumerator,
                                                                     joinSelectorDeclaredRangeVariables,
                                                                     diagnostics)

                    Case SyntaxKind.AggregateClause
                        ' Absorb Aggregate
                        Dim aggregate = DirectCast(absorbNextOperator, AggregateClauseSyntax)
                        joinSelector = BindAggregateClauseFirstSelector(aggregate,
                                                                        operatorsEnumerator,
                                                                        leftRangeVariables,
                                                                        rightRangeVariables,
                                                                        joinSelectorDeclaredRangeVariables,
                                                                        group,
                                                                        intoBinder,
                                                                        diagnostics)
                    Case Else
                        Throw ExceptionUtilities.UnexpectedValue(absorbNextOperator.Kind)

                End Select

                Return joinSelector
            End Function

            ''' <summary>
            ''' Bind Join/Let like and mixed selector in context of this binder.
            ''' 
            ''' Join like selector: Function(a, b) New With {a, b}
            ''' 
            ''' Let like selector: Function(a) New With {a, letExpressionRangeVariable}
            ''' 
            ''' Mixed selector: Function(a, b) New With {a, b, letExpressionRangeVariable}
            ''' </summary>
            Public Function BuildJoinSelector(
                syntax As VisualBasicSyntaxNode,
                mustProduceFlatCompoundVariable As Boolean,
                diagnostics As DiagnosticBag,
                Optional rangeVarOpt As RangeVariableSymbol = Nothing,
                Optional rangeVarValueOpt As BoundExpression = Nothing
            ) As BoundExpression
                Debug.Assert(_rangeVariables.Length > 0)
                Debug.Assert((rangeVarOpt Is Nothing) = (rangeVarValueOpt Is Nothing))
                Debug.Assert(rangeVarOpt IsNot Nothing OrElse _lambdaSymbol.ParameterCount > 1)

                Dim selectors As BoundExpression()
                Dim fields As AnonymousTypeField()

                ' In some scenarios, it is safe to leave compound variable in nested form when there is an
                ' operator down the road that does its own projection (Select, Group By, ...). 
                ' All following operators have to take an Anonymous Type in both cases and, since there is no way to
                ' restrict the shape of the Anonymous Type in method's declaration, the operators should be
                ' insensitive to the shape of the Anonymous Type.
                Dim lastIndex As Integer
                Dim sizeIncrease As Integer = If(rangeVarOpt Is Nothing, 0, 1)

                Debug.Assert(sizeIncrease + _rangeVariables.Length > 1)

                ' Note, the special case for [sourceRangeVariables.Count = 1] helps in the
                ' following scenario:
                '     From x In Xs Select x+1 From y In Ys Let z= Zz, ...
                ' Selector lambda should be:
                '     Function(unnamed, y) New With {y, .z=Zz}
                ' The lambda has two parameters, but we have only one range variable that should be carried over.
                ' If we were simply copying lambda's parameters to the Anonymous Type instance, we would 
                ' copy data that aren't needed (value of the first parameter should be dropped).
                If _rangeVariables.Length = 1 OrElse mustProduceFlatCompoundVariable Then
                    ' Need to flatten
                    lastIndex = _rangeVariables.Length + sizeIncrease - 1
                    selectors = New BoundExpression(lastIndex) {}
                    fields = New AnonymousTypeField(lastIndex) {}

                    For j As Integer = 0 To _rangeVariables.Length - 1
                        Dim leftVar As RangeVariableSymbol = _rangeVariables(j)
                        selectors(j) = New BoundRangeVariable(leftVar.Syntax, leftVar, leftVar.Type).MakeCompilerGenerated()
                        fields(j) = New AnonymousTypeField(leftVar.Name, leftVar.Type, leftVar.Syntax.GetLocation(), isKeyOrByRef:=True)
                    Next
                Else
                    ' Nesting ...
                    Debug.Assert(_rangeVariables.Length > 1)

                    Dim parameters As ImmutableArray(Of BoundLambdaParameterSymbol) = _lambdaSymbol.Parameters.As(Of BoundLambdaParameterSymbol)

                    lastIndex = parameters.Length + sizeIncrease - 1
                    selectors = New BoundExpression(lastIndex) {}
                    fields = New AnonymousTypeField(lastIndex) {}

                    For j As Integer = 0 To parameters.Length - 1
                        Dim param As BoundLambdaParameterSymbol = parameters(j)
                        selectors(j) = New BoundParameter(param.Syntax, param, False, param.Type).MakeCompilerGenerated()
                        fields(j) = New AnonymousTypeField(param.Name, param.Type, param.Syntax.GetLocation(), isKeyOrByRef:=True)
                    Next
                End If

                If rangeVarOpt IsNot Nothing Then
                    selectors(lastIndex) = rangeVarValueOpt
                    fields(lastIndex) = New AnonymousTypeField(
                        rangeVarOpt.Name, rangeVarOpt.Type, rangeVarOpt.Syntax.GetLocation(), isKeyOrByRef:=True)
                End If

                Return BindAnonymousObjectCreationExpression(syntax,
                                                             New AnonymousTypeDescriptor(fields.AsImmutableOrNull(),
                                                                                         syntax.QueryClauseKeywordOrRangeVariableIdentifier.GetLocation(),
                                                                                         True),
                                                             selectors.AsImmutableOrNull(),
                                                             diagnostics).MakeCompilerGenerated()
            End Function

            ''' <summary>
            ''' Bind key selectors for a Join/Group Join operator.
            ''' </summary>
            Public Shared Sub BindJoinKeys(
                parentBinder As Binder,
                join As JoinClauseSyntax,
                outer As BoundQueryClauseBase,
                inner As BoundQueryClauseBase,
                joinSelectorRangeVariables As ImmutableArray(Of RangeVariableSymbol),
                <Out()> ByRef outerKeyLambda As BoundQueryLambda,
                <Out()> ByRef outerKeyBinder As QueryLambdaBinder,
                <Out()> ByRef innerKeyLambda As BoundQueryLambda,
                <Out()> ByRef innerKeyBinder As QueryLambdaBinder,
                diagnostics As DiagnosticBag
            )
                Debug.Assert(outerKeyLambda Is Nothing)
                Debug.Assert(outerKeyBinder Is Nothing)
                Debug.Assert(innerKeyLambda Is Nothing)
                Debug.Assert(innerKeyBinder Is Nothing)
                Debug.Assert(joinSelectorRangeVariables.SequenceEqual(outer.RangeVariables.Concat(inner.RangeVariables)))

                Dim outerKeyParam As BoundLambdaParameterSymbol = parentBinder.CreateQueryLambdaParameterSymbol(
                                                                                                   GetQueryLambdaParameterName(outer.RangeVariables), 0,
                                                                                                   outer.CompoundVariableType,
                                                                                                   join, outer.RangeVariables)

                Dim outerKeyLambdaSymbol = parentBinder.CreateQueryLambdaSymbol(LambdaUtilities.GetJoinLeftLambdaBody(join),
                                                                                SynthesizedLambdaKind.JoinLeftQueryLambda,
                                                                                ImmutableArray.Create(outerKeyParam))
                outerKeyBinder = New QueryLambdaBinder(outerKeyLambdaSymbol, joinSelectorRangeVariables)

                Dim innerKeyParam As BoundLambdaParameterSymbol = parentBinder.CreateQueryLambdaParameterSymbol(
                                                                                                   GetQueryLambdaParameterName(inner.RangeVariables), 0,
                                                                                                   inner.CompoundVariableType,
                                                                                                   join, inner.RangeVariables)

                Dim innerKeyLambdaSymbol = parentBinder.CreateQueryLambdaSymbol(LambdaUtilities.GetJoinRightLambdaBody(join),
                                                                                SynthesizedLambdaKind.JoinRightQueryLambda,
                                                                                ImmutableArray.Create(innerKeyParam))

                innerKeyBinder = New QueryLambdaBinder(innerKeyLambdaSymbol, joinSelectorRangeVariables)

                Dim suppressDiagnostics As DiagnosticBag = Nothing

                Dim sideDeterminator As New JoinConditionSideDeterminationVisitor(outer.RangeVariables, inner.RangeVariables)

                Dim joinConditions As SeparatedSyntaxList(Of JoinConditionSyntax) = join.JoinConditions
                Dim outerKey As BoundExpression
                Dim innerKey As BoundExpression
                Dim keysAreGood As Boolean

                If joinConditions.Count = 0 Then
                    ' Malformed tree.
                    Debug.Assert(joinConditions.Count > 0, "Malformed syntax tree.")
                    outerKey = BadExpression(join, ErrorTypeSymbol.UnknownResultType).MakeCompilerGenerated()
                    innerKey = BadExpression(join, ErrorTypeSymbol.UnknownResultType).MakeCompilerGenerated()
                    keysAreGood = False

                ElseIf joinConditions.Count = 1 Then
                    ' Simple case, no need to build an Anonymous Type.
                    outerKey = Nothing
                    innerKey = Nothing
                    keysAreGood = BindJoinCondition(joinConditions(0),
                                                    outerKeyBinder,
                                                    outer.RangeVariables,
                                                    outerKey,
                                                    innerKeyBinder,
                                                    inner.RangeVariables,
                                                    innerKey,
                                                    sideDeterminator,
                                                    diagnostics,
                                                    suppressDiagnostics)

                    If Not keysAreGood Then
                        If outerKey.Type IsNot ErrorTypeSymbol.UnknownResultType Then
                            outerKey = BadExpression(outerKey.Syntax, outerKey, ErrorTypeSymbol.UnknownResultType).MakeCompilerGenerated()
                        End If
                        If innerKey.Type IsNot ErrorTypeSymbol.UnknownResultType Then
                            innerKey = BadExpression(innerKey.Syntax, innerKey, ErrorTypeSymbol.UnknownResultType).MakeCompilerGenerated()
                        End If
                    End If
                Else
                    ' Need to build a compound key as an instance of an Anonymous Type.
                    Dim outerKeys(joinConditions.Count - 1) As BoundExpression
                    Dim innerKeys(joinConditions.Count - 1) As BoundExpression
                    keysAreGood = True

                    For i As Integer = 0 To joinConditions.Count - 1
                        If Not BindJoinCondition(joinConditions(i),
                                                 outerKeyBinder,
                                                 outer.RangeVariables,
                                                 outerKeys(i),
                                                 innerKeyBinder,
                                                 inner.RangeVariables,
                                                 innerKeys(i),
                                                 sideDeterminator,
                                                 diagnostics,
                                                 suppressDiagnostics) Then
                            keysAreGood = False
                        End If
                    Next

                    If keysAreGood Then
                        Dim fields(joinConditions.Count - 1) As AnonymousTypeField

                        For i As Integer = 0 To joinConditions.Count - 1
                            fields(i) = New AnonymousTypeField(
                                "Key" & i.ToString(), outerKeys(i).Type, joinConditions(i).GetLocation(), isKeyOrByRef:=True)
                        Next

                        Dim descriptor As New AnonymousTypeDescriptor(fields.AsImmutableOrNull(),
                                                                      join.OnKeyword.GetLocation(),
                                                                      True)

                        outerKey = outerKeyBinder.BindAnonymousObjectCreationExpression(join, descriptor, outerKeys.AsImmutableOrNull(),
                                                                                        diagnostics).MakeCompilerGenerated()
                        innerKey = innerKeyBinder.BindAnonymousObjectCreationExpression(join, descriptor, innerKeys.AsImmutableOrNull(),
                                                                                        diagnostics).MakeCompilerGenerated()
                    Else
                        outerKey = BadExpression(join, DirectCast(outerKeys, BoundNode()).AsImmutableOrNull(), ErrorTypeSymbol.UnknownResultType)
                        innerKey = BadExpression(join, DirectCast(innerKeys, BoundNode()).AsImmutableOrNull(), ErrorTypeSymbol.UnknownResultType)
                    End If
                End If

                If suppressDiagnostics IsNot Nothing Then
                    suppressDiagnostics.Free()
                End If

                outerKeyLambda = CreateBoundQueryLambda(outerKeyLambdaSymbol,
                                                        outer.RangeVariables,
                                                        outerKey,
                                                        exprIsOperandOfConditionalBranch:=False)

                outerKeyLambda.SetWasCompilerGenerated()

                innerKeyLambda = CreateBoundQueryLambda(innerKeyLambdaSymbol,
                                                        inner.RangeVariables,
                                                        innerKey,
                                                        exprIsOperandOfConditionalBranch:=False)

                innerKeyLambda.SetWasCompilerGenerated()

                Debug.Assert(outerKeyLambda IsNot Nothing)
                Debug.Assert(innerKeyLambda IsNot Nothing)
            End Sub

            Private Shared Function BindJoinCondition(
                joinCondition As JoinConditionSyntax,
                outerKeyBinder As QueryLambdaBinder,
                outerRangeVariables As ImmutableArray(Of RangeVariableSymbol),
                <Out()> ByRef outerKey As BoundExpression,
                innerKeyBinder As QueryLambdaBinder,
                innerRangeVariables As ImmutableArray(Of RangeVariableSymbol),
                <Out()> ByRef innerKey As BoundExpression,
                sideDeterminator As JoinConditionSideDeterminationVisitor,
                diagnostics As DiagnosticBag,
                ByRef suppressDiagnostics As DiagnosticBag
            ) As Boolean
                Debug.Assert(outerKey Is Nothing AndAlso innerKey Is Nothing)

                ' For a JoinConditionSyntax (<left expr> Equals <right expr>), we are going to bind
                ' left side with outerKeyBinder, which has all range variables from outer and from inner
                ' in scope. Then, we try to figure out to which join side the left key actually belongs.
                ' If it turns out that it belongs to the inner side, we will bind the left again,
                ' but now using the innerKeyBinder. The reason why we need to rebind expressions is that
                ' innerKeyBinder, outerKeyBinder are each associated with different lambda symbols and
                ' the binding process might capture the lambda in the tree or in symbols referenced by
                ' the tree, so we can't safely move bound nodes between lambdas. Usually expressions are
                ' small and very simple, so rebinding them shouldn't create a noticeable performance impact.
                ' If that will not turn out to be the case, we might try to optimize by tracking if someone
                ' requested ContainingMember from outerKeyBinder while an expression is being bound and
                ' rebind only in that case. 
                ' Similarly, in some scenarios we bind right key with innerKeyBinder first and then rebind 
                ' it with outerKeyBinder.

                Dim keysAreGood As Boolean

                Dim left As BoundExpression = outerKeyBinder.BindRValue(joinCondition.Left, diagnostics)

                Dim leftSide As JoinConditionSideDeterminationVisitor.Result
                leftSide = sideDeterminator.DetermineTheSide(left, diagnostics)

                If leftSide = JoinConditionSideDeterminationVisitor.Result.Outer Then
                    outerKey = left
                    innerKey = innerKeyBinder.BindRValue(joinCondition.Right, diagnostics)
                    keysAreGood = innerKeyBinder.VerifyJoinKeys(outerKey, outerRangeVariables, leftSide,
                                                                innerKey, innerRangeVariables, sideDeterminator.DetermineTheSide(innerKey, diagnostics),
                                                                diagnostics)
                ElseIf leftSide = JoinConditionSideDeterminationVisitor.Result.Inner Then
                    ' Rebind with the inner binder.
                    If suppressDiagnostics Is Nothing Then
                        suppressDiagnostics = DiagnosticBag.GetInstance()
                    End If

                    innerKey = innerKeyBinder.BindRValue(joinCondition.Left, suppressDiagnostics)
                    Debug.Assert(leftSide = sideDeterminator.DetermineTheSide(innerKey, diagnostics))
                    outerKey = outerKeyBinder.BindRValue(joinCondition.Right, diagnostics)
                    keysAreGood = innerKeyBinder.VerifyJoinKeys(outerKey, outerRangeVariables, sideDeterminator.DetermineTheSide(outerKey, diagnostics),
                                                                innerKey, innerRangeVariables, leftSide,
                                                                diagnostics)
                Else
                    Dim right As BoundExpression = innerKeyBinder.BindRValue(joinCondition.Right, diagnostics)

                    Dim rightSide As JoinConditionSideDeterminationVisitor.Result
                    rightSide = sideDeterminator.DetermineTheSide(right, diagnostics)

                    If rightSide = JoinConditionSideDeterminationVisitor.Result.Outer Then
                        ' Rebind with the outer binder.
                        If suppressDiagnostics Is Nothing Then
                            suppressDiagnostics = DiagnosticBag.GetInstance()
                        End If

                        outerKey = outerKeyBinder.BindRValue(joinCondition.Right, suppressDiagnostics)
                        innerKey = innerKeyBinder.BindRValue(joinCondition.Left, suppressDiagnostics)
                        keysAreGood = innerKeyBinder.VerifyJoinKeys(outerKey, outerRangeVariables, rightSide,
                                                                    innerKey, innerRangeVariables, leftSide,
                                                                    diagnostics)
                    Else
                        outerKey = left
                        innerKey = right

                        keysAreGood = innerKeyBinder.VerifyJoinKeys(outerKey, outerRangeVariables, leftSide,
                                                                    innerKey, innerRangeVariables, rightSide,
                                                                    diagnostics)
                    End If

                    Debug.Assert(Not keysAreGood)
                End If

                If keysAreGood Then
                    keysAreGood = Not (outerKey.Type.IsErrorType() OrElse innerKey.Type.IsErrorType())
                End If

                If keysAreGood AndAlso Not outerKey.Type.IsSameTypeIgnoringCustomModifiers(innerKey.Type) Then
                    ' Apply conversion if available.
                    Dim targetType As TypeSymbol = Nothing
                    Dim intrinsicOperatorType As SpecialType = SpecialType.None
                    Dim useSiteDiagnostics As HashSet(Of DiagnosticInfo) = Nothing
                    Dim operatorKind As BinaryOperatorKind = OverloadResolution.ResolveBinaryOperator(BinaryOperatorKind.Equals,
                                                                                                      outerKey, innerKey,
                                                                                                      innerKeyBinder,
                                                                                                      considerUserDefinedOrLateBound:=False,
                                                                                                      intrinsicOperatorType:=intrinsicOperatorType,
                                                                                                      userDefinedOperator:=Nothing,
                                                                                                      useSiteDiagnostics:=useSiteDiagnostics)

                    If (operatorKind And BinaryOperatorKind.Equals) <> 0 AndAlso
                       (operatorKind And Not (BinaryOperatorKind.Equals Or BinaryOperatorKind.Lifted)) = 0 AndAlso
                       intrinsicOperatorType <> SpecialType.None Then
                        ' There is an intrinsic (=) operator, use its argument type. 
                        Debug.Assert(useSiteDiagnostics.IsNullOrEmpty)
                        targetType = innerKeyBinder.GetSpecialTypeForBinaryOperator(joinCondition, outerKey.Type, innerKey.Type, intrinsicOperatorType,
                                                                                    (operatorKind And BinaryOperatorKind.Lifted) <> 0, diagnostics)
                    Else
                        ' Use dominant type.
                        Dim inferenceCollection = New TypeInferenceCollection()
                        inferenceCollection.AddType(outerKey.Type, RequiredConversion.Any, outerKey)
                        inferenceCollection.AddType(innerKey.Type, RequiredConversion.Any, innerKey)

                        Dim resultList = ArrayBuilder(Of DominantTypeData).GetInstance()
                        inferenceCollection.FindDominantType(resultList, Nothing, useSiteDiagnostics)

                        If diagnostics.Add(joinCondition, useSiteDiagnostics) Then
                            ' Suppress additional diagnostics
                            diagnostics = New DiagnosticBag()
                        End If

                        If resultList.Count = 1 Then
                            targetType = resultList(0).ResultType
                        End If

                        resultList.Free()
                    End If

                    If targetType Is Nothing Then
                        ReportDiagnostic(diagnostics, joinCondition, ERRID.ERR_EqualsTypeMismatch, outerKey.Type, innerKey.Type)
                        keysAreGood = False
                    Else
                        outerKey = outerKeyBinder.ApplyImplicitConversion(outerKey.Syntax, targetType, outerKey, diagnostics, False)
                        innerKey = innerKeyBinder.ApplyImplicitConversion(innerKey.Syntax, targetType, innerKey, diagnostics, False)
                    End If
                End If

                Debug.Assert(outerKey IsNot Nothing AndAlso innerKey IsNot Nothing)
                Return keysAreGood
            End Function


            Private Function VerifyJoinKeys(
                outerKey As BoundExpression,
                outerRangeVariables As ImmutableArray(Of RangeVariableSymbol),
                outerSide As JoinConditionSideDeterminationVisitor.Result,
                innerKey As BoundExpression,
                innerRangeVariables As ImmutableArray(Of RangeVariableSymbol),
                innerSide As JoinConditionSideDeterminationVisitor.Result,
                diagnostics As DiagnosticBag
            ) As Boolean
                If outerSide = JoinConditionSideDeterminationVisitor.Result.Outer AndAlso
                   innerSide = JoinConditionSideDeterminationVisitor.Result.Inner Then
                    Return True
                End If

                If outerKey.HasErrors OrElse innerKey.HasErrors Then
                    Return False
                End If

                Dim builder = PooledStringBuilder.GetInstance()

                Dim outerNames As String = BuildEqualsOperandIsBadErrorArgument(builder.Builder, outerRangeVariables)
                Dim innerNames As String = BuildEqualsOperandIsBadErrorArgument(builder.Builder, innerRangeVariables)

                builder.Free()

                If outerNames Is Nothing OrElse innerNames Is Nothing Then
                    ' Syntax errors were earlier reported.
                    Return False
                End If

                Dim errorInfo = ErrorFactory.ErrorInfo(ERRID.ERR_EqualsOperandIsBad, outerNames, innerNames)

                Select Case outerSide
                    Case JoinConditionSideDeterminationVisitor.Result.Both,
                         JoinConditionSideDeterminationVisitor.Result.Inner
                        ' Report errors on references to inner.
                        EqualsOperandIsBadErrorVisitor.Report(Me, errorInfo, innerRangeVariables, outerKey, diagnostics)

                    Case JoinConditionSideDeterminationVisitor.Result.None
                        ReportDiagnostic(diagnostics, outerKey.Syntax, errorInfo)

                    Case JoinConditionSideDeterminationVisitor.Result.Outer
                        ' This side is good.
                    Case Else
                        Throw ExceptionUtilities.UnexpectedValue(outerSide)
                End Select

                Select Case innerSide
                    Case JoinConditionSideDeterminationVisitor.Result.Both,
                         JoinConditionSideDeterminationVisitor.Result.Outer
                        ' Report errors on references to outer.
                        EqualsOperandIsBadErrorVisitor.Report(Me, errorInfo, outerRangeVariables, innerKey, diagnostics)

                    Case JoinConditionSideDeterminationVisitor.Result.None
                        ReportDiagnostic(diagnostics, innerKey.Syntax, errorInfo)

                    Case JoinConditionSideDeterminationVisitor.Result.Inner
                        ' This side is good.
                    Case Else
                        Throw ExceptionUtilities.UnexpectedValue(innerSide)
                End Select

                Return False
            End Function

            Private Shared Function BuildEqualsOperandIsBadErrorArgument(
                builder As System.Text.StringBuilder,
                rangeVariables As ImmutableArray(Of RangeVariableSymbol)
            ) As String
                builder.Clear()

                Dim i As Integer

                For i = 0 To rangeVariables.Length - 1
                    If Not rangeVariables(i).Name.StartsWith("$"c, StringComparison.Ordinal) Then
                        builder.Append("'"c)
                        builder.Append(rangeVariables(i).Name)
                        builder.Append("'"c)
                        i += 1
                        Exit For
                    End If
                Next

                For i = i To rangeVariables.Length - 1
                    If Not rangeVariables(i).Name.StartsWith("$"c, StringComparison.Ordinal) Then
                        builder.Append(","c)
                        builder.Append(" "c)
                        builder.Append("'"c)
                        builder.Append(rangeVariables(i).Name)
                        builder.Append("'"c)
                    End If
                Next

                If builder.Length = 0 Then
                    Return Nothing
                End If

                Return builder.ToString()
            End Function

            ''' <summary>
            ''' Helper visitor to determine what join sides are referenced by an expression.
            ''' </summary>
            Private Class JoinConditionSideDeterminationVisitor
                Inherits BoundTreeWalkerWithStackGuardWithoutRecursionOnTheLeftOfBinaryOperator

                <Flags()>
                Public Enum Result
                    None = 0
                    Outer = 1
                    Inner = 2
                    Both = Outer Or Inner
                End Enum

                Private ReadOnly _outerRangeVariables As ImmutableArray(Of Object) 'ImmutableArray(Of RangeVariableSymbol)
                Private ReadOnly _innerRangeVariables As ImmutableArray(Of Object) 'ImmutableArray(Of RangeVariableSymbol)
                Private _side As Result

                Public Sub New(
                    outerRangeVariables As ImmutableArray(Of RangeVariableSymbol),
                    innerRangeVariables As ImmutableArray(Of RangeVariableSymbol)
                )
                    _outerRangeVariables = StaticCast(Of Object).From(outerRangeVariables)
                    _innerRangeVariables = StaticCast(Of Object).From(innerRangeVariables)
                End Sub

                Public Function DetermineTheSide(node As BoundExpression, diagnostics As DiagnosticBag) As Result
                    _side = Result.None
                    Try
                        Visit(node)
                    Catch ex As CancelledByStackGuardException
                        ex.AddAnError(diagnostics)
                    End Try

                    Return _side
                End Function

                Public Overrides Function VisitRangeVariable(node As BoundRangeVariable) As BoundNode
                    Dim rangeVariable As RangeVariableSymbol = node.RangeVariable

                    If _outerRangeVariables.IndexOf(rangeVariable, ReferenceEqualityComparer.Instance) >= 0 Then
                        _side = _side Or Result.Outer
                    ElseIf _innerRangeVariables.IndexOf(rangeVariable, ReferenceEqualityComparer.Instance) >= 0 Then
                        _side = _side Or Result.Inner
                    End If

                    Return node
                End Function
            End Class


            ''' <summary>
            ''' Helper visitor to report query specific errors for an operand of an Equals expression.
            ''' </summary>
            Private Class EqualsOperandIsBadErrorVisitor
                Inherits BoundTreeWalkerWithStackGuardWithoutRecursionOnTheLeftOfBinaryOperator

                Private ReadOnly _binder As Binder
                Private ReadOnly _errorInfo As DiagnosticInfo
                Private ReadOnly _diagnostics As DiagnosticBag
                Private ReadOnly _badRangeVariables As ImmutableArray(Of Object) 'ImmutableArray(Of RangeVariableSymbol)

                Private Sub New(
                    binder As Binder,
                    errorInfo As DiagnosticInfo,
                    badRangeVariables As ImmutableArray(Of RangeVariableSymbol),
                    diagnostics As DiagnosticBag
                )
                    _badRangeVariables = StaticCast(Of Object).From(badRangeVariables)
                    _binder = binder
                    _diagnostics = diagnostics
                    _errorInfo = errorInfo
                End Sub

                Public Shared Sub Report(
                    binder As Binder,
                    errorInfo As DiagnosticInfo,
                    badRangeVariables As ImmutableArray(Of RangeVariableSymbol),
                    node As BoundExpression,
                    diagnostics As DiagnosticBag
                )
                    Dim v As New EqualsOperandIsBadErrorVisitor(binder, errorInfo, badRangeVariables, diagnostics)
                    Try
                        v.Visit(node)
                    Catch ex As CancelledByStackGuardException
                        ex.AddAnError(diagnostics)
                    End Try
                End Sub

                Public Overrides Function VisitRangeVariable(node As BoundRangeVariable) As BoundNode
                    Dim rangeVariable As RangeVariableSymbol = node.RangeVariable

                    If _badRangeVariables.IndexOf(rangeVariable, ReferenceEqualityComparer.Instance) >= 0 Then
                        ReportDiagnostic(_diagnostics, node.Syntax, _errorInfo)
                    End If

                    Return node
                End Function

            End Class
        End Class

        ''' <summary>
        ''' Knows how to bind FunctionAggregationSyntax and GroupAggregationSyntax
        ''' within particular [Into] clause. 
        ''' 
        ''' Also implements Lookup/LookupNames methods to make sure that lookup without 
        ''' container type, uses type of the group as the container type.
        ''' </summary>
        Private Class IntoClauseBinder
            Inherits Binder

            Protected ReadOnly m_GroupReference As BoundExpression
            Private ReadOnly _groupRangeVariables As ImmutableArray(Of RangeVariableSymbol)
            Private ReadOnly _groupCompoundVariableType As TypeSymbol
            Private ReadOnly _aggregationArgumentRangeVariables As ImmutableArray(Of RangeVariableSymbol)

            Public Sub New(
                parent As Binder,
                groupReference As BoundExpression,
                groupRangeVariables As ImmutableArray(Of RangeVariableSymbol),
                groupCompoundVariableType As TypeSymbol,
                aggregationArgumentRangeVariables As ImmutableArray(Of RangeVariableSymbol)
            )
                MyBase.New(parent)
                m_GroupReference = groupReference
                _groupRangeVariables = groupRangeVariables
                _groupCompoundVariableType = groupCompoundVariableType
                _aggregationArgumentRangeVariables = aggregationArgumentRangeVariables
            End Sub

            Friend Overrides Function BindGroupAggregationExpression(
                group As GroupAggregationSyntax,
                diagnostics As DiagnosticBag
            ) As BoundExpression
                Return New BoundGroupAggregation(group, m_GroupReference, m_GroupReference.Type)
            End Function

            ''' <summary>
            ''' Given aggregationVariables, bind Into selector in context of this binder.
            ''' </summary>
            Public Function BindIntoSelector(
                syntaxNode As QueryClauseSyntax,
                keysRangeVariables As ImmutableArray(Of RangeVariableSymbol),
                compoundKeyReferencePart1 As BoundExpression,
                keysRangeVariablesPart1 As ImmutableArray(Of RangeVariableSymbol),
                compoundKeyReferencePart2 As BoundExpression,
                keysRangeVariablesPart2 As ImmutableArray(Of RangeVariableSymbol),
                declaredNames As HashSet(Of String),
                aggregationVariables As SeparatedSyntaxList(Of AggregationRangeVariableSyntax),
                mustProduceFlatCompoundVariable As Boolean,
                <Out()> ByRef declaredRangeVariables As ImmutableArray(Of RangeVariableSymbol),
                diagnostics As DiagnosticBag
            ) As BoundExpression
                Debug.Assert(declaredRangeVariables.IsDefault)
                Debug.Assert(compoundKeyReferencePart2 Is Nothing OrElse compoundKeyReferencePart1 IsNot Nothing)
                Debug.Assert((compoundKeyReferencePart1 Is Nothing) = (keysRangeVariablesPart1.Length = 0))
                Debug.Assert((compoundKeyReferencePart2 Is Nothing) = (keysRangeVariablesPart2.Length = 0))
                Debug.Assert((compoundKeyReferencePart2 Is Nothing) = (keysRangeVariables = keysRangeVariablesPart1))
                Debug.Assert(compoundKeyReferencePart1 Is Nothing OrElse keysRangeVariables.Length = keysRangeVariablesPart1.Length + keysRangeVariablesPart2.Length)

                Dim keys As Integer = keysRangeVariables.Length
                Dim selectors As BoundExpression()
                Dim fields As AnonymousTypeField()

                If declaredNames Is Nothing Then
                    declaredNames = CreateSetOfDeclaredNames(keysRangeVariables)
                Else
                    AssertDeclaredNames(declaredNames, keysRangeVariables)
                End If

                Dim intoSelector As BoundExpression
                Dim rangeVariables() As RangeVariableSymbol

                Dim fieldsToReserveForAggregationVariables As Integer = Math.Max(aggregationVariables.Count, 1)

                If keys + fieldsToReserveForAggregationVariables > 1 Then
                    ' Need to build an Anonymous Type
                    ' Add keys first.
                    If keys > 1 AndAlso Not mustProduceFlatCompoundVariable Then
                        If compoundKeyReferencePart2 Is Nothing Then
                            selectors = New BoundExpression(fieldsToReserveForAggregationVariables + 1 - 1) {}
                            fields = New AnonymousTypeField(selectors.Length - 1) {}

                            ' Using syntax of the first range variable in the source, this shouldn't create any problems.
                            fields(0) = New AnonymousTypeField(GetQueryLambdaParameterName(keysRangeVariablesPart1),
                                                               compoundKeyReferencePart1.Type,
                                                               keysRangeVariables(0).Syntax.GetLocation(), isKeyOrByRef:=True)
                            selectors(0) = compoundKeyReferencePart1
                            keys = 1
                        Else
                            selectors = New BoundExpression(fieldsToReserveForAggregationVariables + 2 - 1) {}
                            fields = New AnonymousTypeField(selectors.Length - 1) {}

                            ' Using syntax of the first range variable in the source, this shouldn't create any problems.
                            fields(0) = New AnonymousTypeField(GetQueryLambdaParameterNameLeft(keysRangeVariablesPart1),
                                                               compoundKeyReferencePart1.Type,
                                                               keysRangeVariablesPart1(0).Syntax.GetLocation(),
                                                               isKeyOrByRef:=True)
                            selectors(0) = compoundKeyReferencePart1

                            fields(1) = New AnonymousTypeField(GetQueryLambdaParameterNameRight(keysRangeVariablesPart2),
                                                               compoundKeyReferencePart2.Type,
                                                               keysRangeVariablesPart2(0).Syntax.GetLocation(),
                                                               isKeyOrByRef:=True)
                            selectors(1) = compoundKeyReferencePart2

                            keys = 2
                        End If
                    Else
                        selectors = New BoundExpression(keys + fieldsToReserveForAggregationVariables - 1) {}
                        fields = New AnonymousTypeField(selectors.Length - 1) {}

                        For i As Integer = 0 To keys - 1
                            Dim rangeVar As RangeVariableSymbol = keysRangeVariables(i)
                            fields(i) = New AnonymousTypeField(rangeVar.Name, rangeVar.Type, rangeVar.Syntax.GetLocation(), isKeyOrByRef:=True)
                            selectors(i) = New BoundRangeVariable(rangeVar.Syntax, rangeVar, rangeVar.Type).MakeCompilerGenerated()
                        Next
                    End If

                    ' Now add aggregation variables.
                    rangeVariables = New RangeVariableSymbol(fieldsToReserveForAggregationVariables - 1) {}

                    If aggregationVariables.Count = 0 Then
                        ' Malformed syntax tree.
                        Debug.Assert(aggregationVariables.Count > 0, "Malformed syntax tree.")
                        Dim rangeVar = RangeVariableSymbol.CreateForErrorRecovery(Me, syntaxNode, ErrorTypeSymbol.UnknownResultType)

                        rangeVariables(0) = rangeVar
                        selectors(keys) = BadExpression(syntaxNode, m_GroupReference, ErrorTypeSymbol.UnknownResultType).MakeCompilerGenerated()
                        fields(keys) = New AnonymousTypeField(rangeVar.Name, rangeVar.Type, rangeVar.Syntax.GetLocation(), isKeyOrByRef:=True)
                    Else
                        For i As Integer = 0 To aggregationVariables.Count - 1
                            Dim selector As BoundExpression = Nothing
                            Dim rangeVar As RangeVariableSymbol = BindAggregationRangeVariable(aggregationVariables(i),
                                                                                               declaredNames, selector,
                                                                                               diagnostics)

                            Debug.Assert(rangeVar IsNot Nothing)
                            rangeVariables(i) = rangeVar

                            selectors(keys + i) = selector
                            fields(keys + i) = New AnonymousTypeField(
                                rangeVar.Name, rangeVar.Type, rangeVar.Syntax.GetLocation(), isKeyOrByRef:=True)
                        Next
                    End If

                    Debug.Assert(selectors.Length > 1)
                    intoSelector = BindAnonymousObjectCreationExpression(syntaxNode,
                                                                     New AnonymousTypeDescriptor(fields.AsImmutableOrNull(),
                                                                                                 syntaxNode.QueryClauseKeywordOrRangeVariableIdentifier.GetLocation(),
                                                                                                 True),
                                                                     selectors.AsImmutableOrNull(),
                                                                     diagnostics).MakeCompilerGenerated()
                Else
                    Debug.Assert(keys = 0)

                    Dim rangeVar As RangeVariableSymbol

                    If aggregationVariables.Count = 0 Then
                        ' Malformed syntax tree.
                        Debug.Assert(aggregationVariables.Count > 0, "Malformed syntax tree.")
                        rangeVar = RangeVariableSymbol.CreateForErrorRecovery(Me, syntaxNode, ErrorTypeSymbol.UnknownResultType)
                        intoSelector = BadExpression(syntaxNode, m_GroupReference, ErrorTypeSymbol.UnknownResultType).MakeCompilerGenerated()
                    Else
                        Debug.Assert(aggregationVariables.Count = 1)
                        intoSelector = Nothing
                        rangeVar = BindAggregationRangeVariable(aggregationVariables(0),
                                                                declaredNames, intoSelector,
                                                                diagnostics)
                        Debug.Assert(rangeVar IsNot Nothing)
                    End If

                    rangeVariables = {rangeVar}
                End If


                AssertDeclaredNames(declaredNames, rangeVariables.AsImmutableOrNull())

                declaredRangeVariables = rangeVariables.AsImmutableOrNull()
                Return intoSelector
            End Function

            Friend Overrides Function BindFunctionAggregationExpression(
                functionAggregationSyntax As FunctionAggregationSyntax,
                diagnostics As DiagnosticBag
            ) As BoundExpression
                If functionAggregationSyntax.FunctionName.GetTypeCharacter() <> TypeCharacter.None Then
                    ReportDiagnostic(diagnostics, functionAggregationSyntax.FunctionName, ERRID.ERR_TypeCharOnAggregation)
                End If

                Dim aggregationParam As BoundLambdaParameterSymbol = CreateQueryLambdaParameterSymbol(GetQueryLambdaParameterName(_groupRangeVariables), 0,
                                                                                                      _groupCompoundVariableType,
                                                                                                      If(functionAggregationSyntax.Argument, functionAggregationSyntax),
                                                                                                      _groupRangeVariables)

                ' Note [Binder:=Me.ContainingBinder] below. We are excluding this binder from the chain of
                ' binders for an argument of the function. Do not want it to interfere in any way given
                ' its special binding and Lookup/LookupNames behavior.

                Dim aggregationLambdaSymbol = Me.ContainingBinder.CreateQueryLambdaSymbol(
                    If(LambdaUtilities.GetAggregationLambdaBody(functionAggregationSyntax), functionAggregationSyntax),
                    SynthesizedLambdaKind.AggregationQueryLambda,
                    ImmutableArray.Create(aggregationParam))

                ' Create binder for the aggregation.
                Dim aggregationBinder As New QueryLambdaBinder(aggregationLambdaSymbol, _aggregationArgumentRangeVariables)

                Dim arguments As ImmutableArray(Of BoundExpression)
                Dim aggregationLambda As BoundQueryLambda = Nothing

                If functionAggregationSyntax.Argument Is Nothing Then
                    arguments = ImmutableArray(Of BoundExpression).Empty
                Else
                    ' Bind argument as a value, conversion during overload resolution should take care of the rest (making it an RValue, etc.). 
                    Dim aggregationSelector = aggregationBinder.BindValue(functionAggregationSyntax.Argument, diagnostics)

                    aggregationLambda = CreateBoundQueryLambda(aggregationLambdaSymbol,
                                                               _groupRangeVariables,
                                                               aggregationSelector,
                                                               exprIsOperandOfConditionalBranch:=False)

                    ' Note, we are not setting ReturnType for aggregationLambdaSymbol to allow
                    ' additional conversions. This type doesn't affect type of any range variable
                    ' in the query.
                    aggregationLambda.SetWasCompilerGenerated()

                    arguments = ImmutableArray.Create(Of BoundExpression)(aggregationLambda)
                End If

                ' Now bind the aggregation call.
                Dim boundCallOrBadExpression As BoundExpression

                If m_GroupReference.Type.IsErrorType() OrElse String.IsNullOrEmpty(functionAggregationSyntax.FunctionName.ValueText) Then
                    boundCallOrBadExpression = BadExpression(functionAggregationSyntax,
                                                             ImmutableArray.Create(Of BoundNode)(m_GroupReference).AddRange(arguments),
                                                             ErrorTypeSymbol.UnknownResultType).MakeCompilerGenerated()
                Else
                    Dim callDiagnostics As DiagnosticBag = diagnostics

                    If aggregationLambda IsNot Nothing AndAlso ShouldSuppressDiagnostics(aggregationLambda) Then
                        ' Operator BindQueryClauseCall will fail, let's suppress any additional errors it will report.
                        callDiagnostics = DiagnosticBag.GetInstance()
                    End If

                    boundCallOrBadExpression = BindQueryOperatorCall(functionAggregationSyntax, m_GroupReference,
                                                                   functionAggregationSyntax.FunctionName.ValueText,
                                                                   arguments,
                                                                   functionAggregationSyntax.FunctionName.Span,
                                                                   callDiagnostics)

                    If callDiagnostics IsNot diagnostics Then
                        callDiagnostics.Free()
                    End If
                End If

                Return New BoundQueryClause(functionAggregationSyntax, boundCallOrBadExpression,
                                            ImmutableArray(Of RangeVariableSymbol).Empty,
                                            boundCallOrBadExpression.Type,
                                            ImmutableArray.Create(Of Binder)(aggregationBinder),
                                            boundCallOrBadExpression.Type)
            End Function


            Public Overrides Sub AddLookupSymbolsInfo(nameSet As LookupSymbolsInfo, options As LookupOptions)
                If (options And (LookupOptionExtensions.ConsiderationMask Or LookupOptions.MustNotBeInstance)) <> 0 Then
                    Return
                End If

                ' Should look for group's methods only. 
                AddMemberLookupSymbolsInfo(nameSet,
                                  m_GroupReference.Type,
                                  options Or CType(LookupOptions.MethodsOnly Or LookupOptions.MustBeInstance, LookupOptions))
            End Sub

            Public Overrides Sub Lookup(lookupResult As LookupResult, name As String, arity As Integer, options As LookupOptions, <[In], Out> ByRef useSiteDiagnostics As HashSet(Of DiagnosticInfo))
                If (options And (LookupOptionExtensions.ConsiderationMask Or LookupOptions.MustNotBeInstance)) <> 0 Then
                    Return
                End If

                ' Should look for group's methods only. 
                LookupMember(lookupResult,
                             m_GroupReference.Type,
                             name,
                             arity,
                             options Or CType(LookupOptions.MethodsOnly Or LookupOptions.MustBeInstance, LookupOptions),
                             useSiteDiagnostics)
            End Sub

            ''' <summary>
            ''' Bind AggregationRangeVariableSyntax in context of this binder.
            ''' </summary>
            Public Function BindAggregationRangeVariable(
                item As AggregationRangeVariableSyntax,
                declaredNames As HashSet(Of String),
                <Out()> ByRef selector As BoundExpression,
                diagnostics As DiagnosticBag
            ) As RangeVariableSymbol
                Debug.Assert(selector Is Nothing)

                Dim variableName As VariableNameEqualsSyntax = item.NameEquals

                ' Figure out the name of the new range variable
                Dim rangeVarName As String = Nothing
                Dim rangeVarNameSyntax As SyntaxToken = Nothing

                If variableName IsNot Nothing Then
                    rangeVarNameSyntax = variableName.Identifier.Identifier
                    rangeVarName = rangeVarNameSyntax.ValueText
                    Debug.Assert(variableName.AsClause Is Nothing)

                Else
                    ' Infer the name from expression
                    Select Case item.Aggregation.Kind
                        Case SyntaxKind.GroupAggregation
                            ' AggregateClause doesn't support GroupAggregation.
                            If item.Parent.Kind <> SyntaxKind.AggregateClause Then
                                rangeVarNameSyntax = DirectCast(item.Aggregation, GroupAggregationSyntax).GroupKeyword
                                rangeVarName = rangeVarNameSyntax.ValueText
                            End If
                        Case SyntaxKind.FunctionAggregation
                            rangeVarNameSyntax = DirectCast(item.Aggregation, FunctionAggregationSyntax).FunctionName
                            rangeVarName = rangeVarNameSyntax.ValueText
                        Case Else
                            Throw ExceptionUtilities.UnexpectedValue(item.Aggregation.Kind)
                    End Select
                End If

                ' Bind the value.
                selector = BindRValue(item.Aggregation, diagnostics)

                If rangeVarName IsNot Nothing AndAlso rangeVarName.Length = 0 Then
                    ' Empty string must have been a syntax error. 
                    rangeVarName = Nothing
                End If

                Dim rangeVar As RangeVariableSymbol = Nothing

                If rangeVarName IsNot Nothing Then

                    rangeVar = RangeVariableSymbol.Create(Me, rangeVarNameSyntax, selector.Type)

                    ' Note what we are doing here:
                    ' We are creating BoundRangeVariableAssignment before doing any shadowing checks
                    ' so that SemanticModel can find the declared symbol, but, if the variable will conflict with another
                    ' variable in the same child scope, we will not add it to the scope. Instead, we create special
                    ' error recovery range variable symbol and add it to the scope at the same place, making sure 
                    ' that the earlier declared range variable wins during name lookup.
                    ' As an alternative, we could still add the original range variable symbol to the scope and then,
                    ' while we are binding the rest of the query in error recovery mode, references to the name would 
                    ' cause ambiguity. However, this could negatively affect IDE experience. Also, as we build an
                    ' Anonymous Type for the compound range variables, we would end up with a type with duplicate members,
                    ' which could cause problems elsewhere.
                    selector = New BoundRangeVariableAssignment(item, rangeVar, selector, selector.Type)
                    Dim doErrorRecovery As Boolean = False

                    If declaredNames IsNot Nothing AndAlso Not declaredNames.Add(rangeVarName) Then
                        ReportDiagnostic(diagnostics, rangeVarNameSyntax, ERRID.ERR_QueryDuplicateAnonTypeMemberName1, rangeVarName)
                        doErrorRecovery = True  ' Shouldn't add to the scope.
                    Else
                        Me.VerifyRangeVariableName(rangeVar, rangeVarNameSyntax, diagnostics)
                    End If

                    If doErrorRecovery Then
                        rangeVar = RangeVariableSymbol.CreateForErrorRecovery(Me, rangeVar.Syntax, selector.Type)
                    End If

                Else
                    Debug.Assert(rangeVar Is Nothing)
                    rangeVar = RangeVariableSymbol.CreateForErrorRecovery(Me, item, selector.Type)
                End If

                Debug.Assert(selector IsNot Nothing)

                Return rangeVar
            End Function

        End Class

        ''' <summary>
        ''' Same as IntoClauseBinder, but disallows references to GroupAggregationSyntax.
        ''' </summary>
        Private Class IntoClauseDisallowGroupReferenceBinder
            Inherits IntoClauseBinder

            Public Sub New(
                parent As Binder,
                groupReference As BoundExpression,
                groupRangeVariables As ImmutableArray(Of RangeVariableSymbol),
                groupCompoundVariableType As TypeSymbol,
                aggregationArgumentRangeVariables As ImmutableArray(Of RangeVariableSymbol)
            )
                MyBase.New(parent, groupReference, groupRangeVariables, groupCompoundVariableType, aggregationArgumentRangeVariables)
            End Sub

            Friend Overrides Function BindGroupAggregationExpression(group As GroupAggregationSyntax, diagnostics As DiagnosticBag) As BoundExpression
                ' Parser should have reported an error.
                Return BadExpression(group, m_GroupReference, ErrorTypeSymbol.UnknownResultType)
            End Function
        End Class

        ''' <summary>
        ''' Bind CollectionRangeVariableSyntax, applying AsQueryable/AsEnumerable/Cast(Of Object) calls and 
        ''' Select with implicit type conversion as appropriate.
        ''' </summary>
        Private Function BindCollectionRangeVariable(
            syntax As CollectionRangeVariableSyntax,
            beginsTheQuery As Boolean,
            declaredNames As HashSet(Of String),
            diagnostics As DiagnosticBag
        ) As BoundQueryableSource
            Debug.Assert(declaredNames Is Nothing OrElse syntax.Parent.Kind = SyntaxKind.SimpleJoinClause OrElse syntax.Parent.Kind = SyntaxKind.GroupJoinClause)

            Dim source As BoundQueryPart = New BoundQuerySource(BindRValue(syntax.Expression, diagnostics))

            Dim variableType As TypeSymbol = Nothing
            Dim queryable As BoundExpression = ConvertToQueryableType(source, diagnostics, variableType)

            Dim sourceIsNotQueryable As Boolean = False

            If variableType Is Nothing Then
                If Not source.HasErrors Then
                    ReportDiagnostic(diagnostics, syntax.Expression, ERRID.ERR_ExpectedQueryableSource, source.Type)
                End If

                sourceIsNotQueryable = True

            ElseIf source IsNot queryable Then
                source = New BoundToQueryableCollectionConversion(DirectCast(queryable, BoundCall)).MakeCompilerGenerated()
            End If

            ' Deal with AsClauseOpt and various modifiers.
            Dim targetVariableType As TypeSymbol = Nothing

            If syntax.AsClause IsNot Nothing Then
                targetVariableType = DecodeModifiedIdentifierType(syntax.Identifier,
                                                                  syntax.AsClause,
                                                                  Nothing,
                                                                  Nothing,
                                                                  diagnostics,
                                                                  ModifiedIdentifierTypeDecoderContext.LocalType Or
                                                                  ModifiedIdentifierTypeDecoderContext.QueryRangeVariableType)
            ElseIf syntax.Identifier.Nullable.Node IsNot Nothing Then
                ReportDiagnostic(diagnostics, syntax.Identifier.Nullable, ERRID.ERR_NullableTypeInferenceNotSupported)
            End If

            If variableType Is Nothing Then
                Debug.Assert(sourceIsNotQueryable)

                If targetVariableType Is Nothing Then
                    variableType = ErrorTypeSymbol.UnknownResultType
                Else
                    variableType = targetVariableType
                End If

            ElseIf targetVariableType IsNot Nothing AndAlso
                   Not targetVariableType.IsSameTypeIgnoringCustomModifiers(variableType) Then
                Debug.Assert(Not sourceIsNotQueryable AndAlso syntax.AsClause IsNot Nothing)
                ' Need to apply implicit Select that converts variableType to targetVariableType.
                source = ApplyImplicitCollectionConversion(syntax, source, variableType, targetVariableType, diagnostics)
                variableType = targetVariableType
            End If

            Dim variable As RangeVariableSymbol = Nothing
            Dim rangeVarName As String = syntax.Identifier.Identifier.ValueText
            Dim rangeVariableOpt As RangeVariableSymbol = Nothing

            If rangeVarName IsNot Nothing AndAlso rangeVarName.Length = 0 Then
                ' Empty string must have been a syntax error. 
                rangeVarName = Nothing
            End If

            If rangeVarName IsNot Nothing Then
                variable = RangeVariableSymbol.Create(Me, syntax.Identifier.Identifier, variableType)

                ' Note what we are doing here:
                ' We are capturing rangeVariableOpt before doing any shadowing checks
                ' so that SemanticModel can find the declared symbol, but, if the variable will conflict with another
                ' variable in the same child scope, we will not add it to the scope. Instead, we create special
                ' error recovery range variable symbol and add it to the scope at the same place, making sure 
                ' that the earlier declared range variable wins during name lookup.
                ' As an alternative, we could still add the original range variable symbol to the scope and then,
                ' while we are binding the rest of the query in error recovery mode, references to the name would 
                ' cause ambiguity. However, this could negatively affect IDE experience. Also, as we build an
                ' Anonymous Type for the compound range variables, we would end up with a type with duplicate members,
                ' which could cause problems elsewhere.
                rangeVariableOpt = variable

                Dim doErrorRecovery As Boolean = False

                If declaredNames IsNot Nothing AndAlso Not declaredNames.Add(rangeVarName) Then
                    ReportDiagnostic(diagnostics, syntax.Identifier.Identifier, ERRID.ERR_QueryDuplicateAnonTypeMemberName1, rangeVarName)
                    doErrorRecovery = True  ' Shouldn't add to the scope.
                Else

                    ' Check shadowing etc. 
                    VerifyRangeVariableName(variable, syntax.Identifier.Identifier, diagnostics)

                    If Not beginsTheQuery AndAlso declaredNames Is Nothing Then
                        Debug.Assert(syntax.Parent.Kind = SyntaxKind.FromClause OrElse syntax.Parent.Kind = SyntaxKind.AggregateClause)
                        ' We are about to add this range variable to the current child scope.
                        If ShadowsRangeVariableInTheChildScope(Me, variable) Then
                            ' Shadowing error was reported earlier.
                            doErrorRecovery = True  ' Shouldn't add to the scope.
                        End If
                    End If
                End If

                If doErrorRecovery Then
                    variable = RangeVariableSymbol.CreateForErrorRecovery(Me, variable.Syntax, variableType)
                End If

            Else
                Debug.Assert(variable Is Nothing)
                variable = RangeVariableSymbol.CreateForErrorRecovery(Me, syntax, variableType)
            End If

            Dim result As New BoundQueryableSource(syntax, source, rangeVariableOpt,
                                                   ImmutableArray.Create(variable), variableType,
                                                   ImmutableArray(Of Binder).Empty,
                                                   If(sourceIsNotQueryable, ErrorTypeSymbol.UnknownResultType, source.Type),
                                                   hasErrors:=sourceIsNotQueryable)

            Return result
        End Function

        ''' <summary>
        ''' Apply "conversion" to the source based on the target AsClause Type of the CollectionRangeVariableSyntax.
        ''' Returns implicit BoundQueryClause or the source, in case of an early failure.
        ''' </summary>
        Private Function ApplyImplicitCollectionConversion(
            syntax As CollectionRangeVariableSyntax,
            source As BoundQueryPart,
            variableType As TypeSymbol,
            targetVariableType As TypeSymbol,
            diagnostics As DiagnosticBag
        ) As BoundQueryPart
            If source.Type.IsErrorType() Then
                ' If the source is already a "bad" type, we know that we will not be able to bind to the Select.
                ' Let's just report errors for the conversion between types, if any.
                Dim sourceValue As New BoundRValuePlaceholder(syntax.AsClause, variableType)
                ApplyImplicitConversion(syntax.AsClause, targetVariableType, sourceValue, diagnostics)
            Else
                ' Create LambdaSymbol for the shape of the selector.
                Dim param As BoundLambdaParameterSymbol = CreateQueryLambdaParameterSymbol(syntax.Identifier.Identifier.ValueText, 0,
                                                                                           variableType,
                                                                                           syntax.AsClause)

                Debug.Assert(LambdaUtilities.IsNonUserCodeQueryLambda(syntax.AsClause))
                Dim lambdaSymbol = Me.CreateQueryLambdaSymbol(syntax.AsClause,
                                                              SynthesizedLambdaKind.ConversionNonUserCodeQueryLambda,
                                                              ImmutableArray.Create(param))

                lambdaSymbol.SetQueryLambdaReturnType(targetVariableType)

                Dim selectorBinder As New QueryLambdaBinder(lambdaSymbol, ImmutableArray(Of RangeVariableSymbol).Empty)

                Dim selector As BoundExpression = selectorBinder.ApplyImplicitConversion(syntax.AsClause, targetVariableType,
                                                                                         New BoundParameter(param.Syntax,
                                                                                                            param,
                                                                                                            isLValue:=False,
                                                                                                            type:=variableType).MakeCompilerGenerated(),
                                                                                         diagnostics)

                Dim selectorLambda = CreateBoundQueryLambda(lambdaSymbol,
                                                            ImmutableArray(Of RangeVariableSymbol).Empty,
                                                            selector,
                                                            exprIsOperandOfConditionalBranch:=False)
                selectorLambda.SetWasCompilerGenerated()

                Dim suppressDiagnostics As DiagnosticBag = Nothing

                If ShouldSuppressDiagnostics(selectorLambda) Then
                    ' If the selector is already "bad", we know that we will not be able to bind to the Select.
                    ' Let's suppress additional errors.
                    suppressDiagnostics = DiagnosticBag.GetInstance()
                    diagnostics = suppressDiagnostics
                End If

                Dim boundCallOrBadExpression As BoundExpression
                boundCallOrBadExpression = BindQueryOperatorCall(syntax.AsClause, source,
                                                                 StringConstants.SelectMethod,
                                                                 ImmutableArray.Create(Of BoundExpression)(selectorLambda),
                                                                 syntax.AsClause.Span,
                                                                 diagnostics)

                Debug.Assert(boundCallOrBadExpression.WasCompilerGenerated)

                If suppressDiagnostics IsNot Nothing Then
                    suppressDiagnostics.Free()
                End If

                Return New BoundQueryClause(source.Syntax,
                                            boundCallOrBadExpression,
                                            ImmutableArray(Of RangeVariableSymbol).Empty,
                                            targetVariableType,
                                            ImmutableArray.Create(Of Binder)(selectorBinder),
                                            boundCallOrBadExpression.Type).MakeCompilerGenerated()
            End If

            Return source
        End Function


        ''' <summary>
        ''' Convert source expression to queryable type by inferring control variable type 
        ''' and applying AsQueryable/AsEnumerable or Cast(Of Object) calls.   
        ''' 
        ''' In case of success, returns possibly "converted" source and non-Nothing controlVariableType.
        ''' In case of failure, returns passed in source and Nothing as controlVariableType.
        ''' </summary>
        Private Function ConvertToQueryableType(
            source As BoundExpression,
            diagnostics As DiagnosticBag,
            <Out()> ByRef controlVariableType As TypeSymbol
        ) As BoundExpression
            controlVariableType = Nothing

            If Not source.IsValue OrElse source.Type.IsErrorType Then
                Return source
            End If

            ' 11.21.2 Queryable Types
            ' A queryable collection type must satisfy one of the following conditions, in order of preference:
            ' -	It must define a conforming Select method.
            ' -	It must have one of the following methods
            ' Function AsEnumerable() As CT
            ' Function AsQueryable() As CT
            ' which can be called to obtain a queryable collection. If both methods are provided, AsQueryable is preferred over AsEnumerable.
            ' -	It must have a method
            ' Function Cast(Of T)() As CT
            ' which can be called with the type of the range variable to produce a queryable collection.

            ' Does it define a conforming Select method?
            Dim inferredType As TypeSymbol = InferControlVariableType(source, diagnostics)

            If inferredType IsNot Nothing Then
                controlVariableType = inferredType
                Return source
            End If

            Dim result As BoundExpression = Nothing
            Dim additionalDiagnostics = DiagnosticBag.GetInstance()

            ' Does it have Function AsQueryable() As CT returning queryable collection?
            Dim asQueryable As BoundExpression = BindQueryOperatorCall(source.Syntax, source, StringConstants.AsQueryableMethod,
                                                                       ImmutableArray(Of BoundExpression).Empty,
                                                                       source.Syntax.Span, additionalDiagnostics)

            If Not asQueryable.HasErrors AndAlso asQueryable.Kind = BoundKind.Call Then
                inferredType = InferControlVariableType(asQueryable, diagnostics)

                If inferredType IsNot Nothing Then
                    controlVariableType = inferredType
                    result = asQueryable
                    diagnostics.AddRange(additionalDiagnostics)
                End If
            End If

            If result Is Nothing Then
                additionalDiagnostics.Clear()

                ' Does it have Function AsEnumerable() As CT returning queryable collection?
                Dim asEnumerable As BoundExpression = BindQueryOperatorCall(source.Syntax, source, StringConstants.AsEnumerableMethod,
                                                                           ImmutableArray(Of BoundExpression).Empty,
                                                                           source.Syntax.Span, additionalDiagnostics)

                If Not asEnumerable.HasErrors AndAlso asEnumerable.Kind = BoundKind.Call Then
                    inferredType = InferControlVariableType(asEnumerable, diagnostics)

                    If inferredType IsNot Nothing Then
                        controlVariableType = inferredType
                        result = asEnumerable
                        diagnostics.AddRange(additionalDiagnostics)
                    End If
                End If
            End If

            If result Is Nothing Then
                additionalDiagnostics.Clear()

                ' If it has Function Cast(Of T)() As CT, call it with T == Object and assume Object is control variable type.
                inferredType = GetSpecialType(SpecialType.System_Object, source.Syntax, additionalDiagnostics)

                Dim cast As BoundExpression = BindQueryOperatorCall(source.Syntax, source, StringConstants.CastMethod,
                                                                    New BoundTypeArguments(source.Syntax,
                                                                                           ImmutableArray.Create(Of TypeSymbol)(inferredType)),
                                                                    ImmutableArray(Of BoundExpression).Empty,
                                                                    source.Syntax.Span, additionalDiagnostics)

                If Not cast.HasErrors AndAlso cast.Kind = BoundKind.Call Then
                    controlVariableType = inferredType
                    result = cast
                    diagnostics.AddRange(additionalDiagnostics)
                End If
            End If

            additionalDiagnostics.Free()

            Debug.Assert((result Is Nothing) = (controlVariableType Is Nothing))
            Return If(result Is Nothing, source, result)
        End Function


        ''' <summary>
        ''' Given query operator source, infer control variable type from available
        ''' 'Select' methods. 
        ''' 
        ''' Returns inferred type or Nothing.
        ''' </summary>
        Private Function InferControlVariableType(source As BoundExpression, diagnostics As DiagnosticBag) As TypeSymbol
            Debug.Assert(source.IsValue)

            Dim result As TypeSymbol = Nothing

            ' Look for Select methods available for the source.
            Dim lookupResult As LookupResult = LookupResult.GetInstance()
            Dim useSiteDiagnostics As HashSet(Of DiagnosticInfo) = Nothing
            LookupMember(lookupResult, source.Type, StringConstants.SelectMethod, 0, QueryOperatorLookupOptions, useSiteDiagnostics)

            If lookupResult.IsGood Then

                Dim failedDueToAnAmbiguity As Boolean = False

                ' Name lookup does not look for extension methods if it found a suitable
                ' instance method, which is a good thing because according to language spec:
                '
                ' 11.21.2 Queryable Types
                ' ... , when determining the element type of a collection if there
                ' are instance methods that match well-known methods, then any extension methods
                ' that match well-known methods are ignored.
                Debug.Assert((QueryOperatorLookupOptions And LookupOptions.EagerlyLookupExtensionMethods) = 0)

                result = InferControlVariableType(lookupResult.Symbols, failedDueToAnAmbiguity)

                If result Is Nothing AndAlso Not failedDueToAnAmbiguity AndAlso Not lookupResult.Symbols(0).IsReducedExtensionMethod() Then
                    ' We tried to infer from instance methods and there were no suitable 'Select' method, 
                    ' let's try to infer from extension methods.
                    lookupResult.Clear()
                    Me.LookupExtensionMethods(lookupResult, source.Type, StringConstants.SelectMethod, 0, QueryOperatorLookupOptions, useSiteDiagnostics)

                    If lookupResult.IsGood Then
                        result = InferControlVariableType(lookupResult.Symbols, failedDueToAnAmbiguity)
                    End If
                End If
            End If

            diagnostics.Add(source, useSiteDiagnostics)
            lookupResult.Free()

            Return result
        End Function

        ''' <summary>
        ''' Given a set of 'Select' methods, infer control variable type. 
        ''' 
        ''' Returns inferred type or Nothing.
        ''' </summary>
        Private Function InferControlVariableType(
            methods As ArrayBuilder(Of Symbol),
            <Out()> ByRef failedDueToAnAmbiguity As Boolean
        ) As TypeSymbol
            Dim result As TypeSymbol = Nothing
            failedDueToAnAmbiguity = False

            For Each method As MethodSymbol In methods
                Dim inferredType As TypeSymbol = InferControlVariableType(method)

                If inferredType IsNot Nothing Then
                    If inferredType.ReferencesMethodsTypeParameter(method) Then
                        failedDueToAnAmbiguity = True
                        Return Nothing
                    End If

                    If result Is Nothing Then
                        result = inferredType
                    ElseIf Not result.IsSameTypeIgnoringCustomModifiers(inferredType) Then
                        failedDueToAnAmbiguity = True
                        Return Nothing
                    End If
                End If
            Next

            Return result
        End Function

        ''' <summary>
        ''' Given a method, infer control variable type. 
        ''' 
        ''' Returns inferred type or Nothing.
        ''' </summary>
        Private Function InferControlVariableType(method As MethodSymbol) As TypeSymbol
            ' Ignore Subs
            If method.IsSub Then
                Return Nothing
            End If

            ' Only methods taking exactly one parameter are acceptable.
            If method.ParameterCount <> 1 Then
                Return Nothing
            End If

            Dim selectParameter As ParameterSymbol = method.Parameters(0)

            If selectParameter.IsByRef Then
                Return Nothing
            End If

            Dim parameterType As TypeSymbol = selectParameter.Type

            ' We are expecting a delegate type with the following shape:
            '     Function Selector (element as ControlVariableType) As AType

            ' The delegate type, directly converted to or argument of Expression(Of T)
            Dim delegateType As NamedTypeSymbol = parameterType.DelegateOrExpressionDelegate(Me)

            If delegateType Is Nothing Then
                Return Nothing
            End If

            Dim invoke As MethodSymbol = delegateType.DelegateInvokeMethod

            If invoke Is Nothing OrElse invoke.IsSub OrElse invoke.ParameterCount <> 1 Then
                Return Nothing
            End If

            Dim invokeParameter As ParameterSymbol = invoke.Parameters(0)

            ' Do not allow Optional, ParamArray and ByRef.
            If invokeParameter.IsOptional OrElse invokeParameter.IsByRef OrElse invokeParameter.IsParamArray Then
                Return Nothing
            End If

            Dim controlVariableType As TypeSymbol = invokeParameter.Type

            Return If(controlVariableType.IsErrorType(), Nothing, controlVariableType)
        End Function


        ''' <summary>
        ''' Return method group or Nothing in case nothing was found.
        ''' Note, returned group might have ResultKind = "Inaccessible".
        ''' </summary>
        Private Function LookupQueryOperator(
            node As VisualBasicSyntaxNode,
            source As BoundExpression,
            operatorName As String,
            typeArgumentsOpt As BoundTypeArguments,
            diagnostics As DiagnosticBag
        ) As BoundMethodGroup
            Dim lookupResult As LookupResult = LookupResult.GetInstance()
            Dim useSiteDiagnostics As HashSet(Of DiagnosticInfo) = Nothing
            LookupMember(lookupResult, source.Type, operatorName, 0, QueryOperatorLookupOptions, useSiteDiagnostics)

            Dim methodGroup As BoundMethodGroup = Nothing

            ' NOTE: Lookup may return Kind = LookupResultKind.Inaccessible or LookupResultKind.MustBeInstance;
            '
            '       It looks we intentionally pass LookupResultKind.Inaccessible to CreateBoundMethodGroup(...) 
            '       causing BC30390 to be generated instead of BC36594 reported by Dev11 (more accurate message?)
            '
            '       As CreateBoundMethodGroup(...) only expects Kind = LookupResultKind.Good or 
            '       LookupResultKind.Inaccessible in all other cases we just skip calling this method
            '       so that BC36594 is generated which what seems to what Dev11 does.
            If Not lookupResult.IsClear AndAlso (lookupResult.Kind = LookupResultKind.Good OrElse lookupResult.Kind = LookupResultKind.Inaccessible) Then
                methodGroup = CreateBoundMethodGroup(
                            node,
                            lookupResult,
                            QueryOperatorLookupOptions,
                            source,
                            typeArgumentsOpt,
                            QualificationKind.QualifiedViaValue).MakeCompilerGenerated()
            End If

            diagnostics.Add(node, useSiteDiagnostics)
            lookupResult.Free()

            Return methodGroup
        End Function


        Private Function BindQueryOperatorCall(
            node As VisualBasicSyntaxNode,
            source As BoundExpression,
            operatorName As String,
            arguments As ImmutableArray(Of BoundExpression),
            operatorNameLocation As TextSpan,
            diagnostics As DiagnosticBag
        ) As BoundExpression
            Return BindQueryOperatorCall(node,
                                         source,
                                         operatorName,
                                         LookupQueryOperator(node, source, operatorName, Nothing, diagnostics),
                                         arguments,
                                         operatorNameLocation,
                                         diagnostics)
        End Function

        Private Function BindQueryOperatorCall(
            node As VisualBasicSyntaxNode,
            source As BoundExpression,
            operatorName As String,
            typeArgumentsOpt As BoundTypeArguments,
            arguments As ImmutableArray(Of BoundExpression),
            operatorNameLocation As TextSpan,
            diagnostics As DiagnosticBag
        ) As BoundExpression
            Return BindQueryOperatorCall(node,
                                         source,
                                         operatorName,
                                         LookupQueryOperator(node, source, operatorName, typeArgumentsOpt, diagnostics),
                                         arguments,
                                         operatorNameLocation,
                                         diagnostics)
        End Function

        ''' <summary>
        ''' [methodGroup] can be Nothing if lookup didn't find anything.
        ''' </summary>
        Private Function BindQueryOperatorCall(
            node As VisualBasicSyntaxNode,
            source As BoundExpression,
            operatorName As String,
            methodGroup As BoundMethodGroup,
            arguments As ImmutableArray(Of BoundExpression),
            operatorNameLocation As TextSpan,
            diagnostics As DiagnosticBag
        ) As BoundExpression
            Debug.Assert(source.IsValue)
            Debug.Assert(methodGroup Is Nothing OrElse
                         (methodGroup.ReceiverOpt Is source AndAlso
                            (methodGroup.ResultKind = LookupResultKind.Good OrElse methodGroup.ResultKind = LookupResultKind.Inaccessible)))

            Dim boundCall As BoundExpression = Nothing

            If methodGroup IsNot Nothing Then
                Dim useSiteDiagnostics As HashSet(Of DiagnosticInfo) = Nothing
                Dim results As OverloadResolution.OverloadResolutionResult = OverloadResolution.QueryOperatorInvocationOverloadResolution(methodGroup,
                                                                                                                                          arguments, Me,
                                                                                                                                          useSiteDiagnostics)

                If diagnostics.Add(node, useSiteDiagnostics) Then
                    If methodGroup.ResultKind <> LookupResultKind.Inaccessible Then
                        ' Suppress additional diagnostics
                        diagnostics = New DiagnosticBag()
                    End If
                End If

                If Not results.BestResult.HasValue Then
                    ' Create and report the diagnostic.
                    If results.Candidates.Length = 0 Then
                        results = OverloadResolution.QueryOperatorInvocationOverloadResolution(methodGroup, arguments, Me, includeEliminatedCandidates:=True,
                                                                                               useSiteDiagnostics:=useSiteDiagnostics)
                    End If

                    If results.Candidates.Length > 0 Then
                        boundCall = ReportOverloadResolutionFailureAndProduceBoundNode(node, methodGroup, arguments, Nothing, results,
                                                                                       diagnostics, callerInfoOpt:=Nothing, queryMode:=True,
                                                                                       diagnosticLocationOpt:=Location.Create(node.SyntaxTree, operatorNameLocation))
                    End If
                Else
                    boundCall = CreateBoundCallOrPropertyAccess(node, node, TypeCharacter.None, methodGroup,
                                                                arguments, results.BestResult.Value,
                                                                results.AsyncLambdaSubToFunctionMismatch,
                                                                diagnostics)

                    ' May need to update return type for LambdaSymbols associated with query lambdas.
                    For i As Integer = 0 To arguments.Length - 1
                        Dim arg As BoundExpression = arguments(i)

                        If arg.Kind = BoundKind.QueryLambda Then
                            Dim queryLambda = DirectCast(arg, BoundQueryLambda)

                            If queryLambda.LambdaSymbol.ReturnType Is LambdaSymbol.ReturnTypePendingDelegate Then
                                Dim delegateReturnType As TypeSymbol = DirectCast(boundCall, BoundCall).Method.Parameters(i).Type.DelegateOrExpressionDelegate(Me).DelegateInvokeMethod.ReturnType
                                queryLambda.LambdaSymbol.SetQueryLambdaReturnType(delegateReturnType)
                            End If
                        End If
                    Next
                End If
            End If

            If boundCall Is Nothing Then
                Dim childBoundNodes As ImmutableArray(Of BoundNode)

                If arguments.IsEmpty Then
                    childBoundNodes = ImmutableArray.Create(Of BoundNode)(If(methodGroup, source))
                Else
                    Dim builder = ArrayBuilder(Of BoundNode).GetInstance()
                    builder.Add(If(methodGroup, source))
                    builder.AddRange(arguments)
                    childBoundNodes = builder.ToImmutableAndFree()
                End If

                If methodGroup Is Nothing Then
                    boundCall = BadExpression(node, childBoundNodes, ErrorTypeSymbol.UnknownResultType)
                Else
                    Dim symbols = ArrayBuilder(Of Symbol).GetInstance()
                    methodGroup.GetExpressionSymbols(symbols)

                    Dim resultKind = LookupResultKind.OverloadResolutionFailure
                    If methodGroup.ResultKind < resultKind Then
                        resultKind = methodGroup.ResultKind
                    End If

                    boundCall = New BoundBadExpression(node, resultKind, symbols.ToImmutableAndFree(), childBoundNodes, ErrorTypeSymbol.UnknownResultType, hasErrors:=True)
                End If
            End If

            If boundCall.HasErrors AndAlso Not source.HasErrors Then
                ReportDiagnostic(diagnostics, Location.Create(node.SyntaxTree, operatorNameLocation), ERRID.ERR_QueryOperatorNotFound, operatorName)
            End If

            boundCall.SetWasCompilerGenerated()

            Return boundCall
        End Function

    End Class

End Namespace
