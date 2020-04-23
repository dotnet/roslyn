' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports System.Diagnostics
Imports System.Runtime.InteropServices
Imports Microsoft.CodeAnalysis.PooledObjects
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports TypeKind = Microsoft.CodeAnalysis.TypeKind

Namespace Microsoft.CodeAnalysis.VisualBasic
    Partial Friend NotInheritable Class LocalRewriter

        ''' <summary>
        ''' Rewrites a for each statement.
        ''' </summary>
        ''' <param name="node">The node.</param><returns></returns>
        Public Overrides Function VisitForEachStatement(node As BoundForEachStatement) As BoundNode
            Dim locals = ArrayBuilder(Of LocalSymbol).GetInstance()
            Dim statements = ArrayBuilder(Of BoundStatement).GetInstance()

            ' there can be a conversion on top of an one dimensional array to cause the ConvertedType of the semantic model
            ' to show the IEnumerable type.
            ' we need to ignore this conversion for the rewriting.
            ' Strings and multidimensional arrays match the pattern and will not have this artificial conversion.
            Dim collectionType As TypeSymbol
            Dim originalCollection As BoundExpression
            If node.Collection.Kind = BoundKind.Conversion Then
                Dim conversion = DirectCast(node.Collection, BoundConversion)
                Dim operand = conversion.Operand
                If Not conversion.ExplicitCastInCode AndAlso Not operand.IsNothingLiteral AndAlso
                    (operand.Type.IsArrayType OrElse operand.Type.IsStringType) Then
                    originalCollection = operand
                Else
                    originalCollection = node.Collection
                End If
            Else
                originalCollection = node.Collection
            End If
            collectionType = originalCollection.Type

            Dim replacedCollection As BoundExpression = originalCollection

            ' LIFTING OF FOR-EACH CONTROL VARIABLES
            '
            ' For Each <ControlVariable> In <CollectionExpression>
            '	<Body>
            ' Next
            '
            ' VB has two forms of "For Each". The first is where the <ControlVariable>
            ' binds to an already-existing variable. The second is where it introduces a new
            ' control variable.
            '
            ' Execution of For Each (the form that introduces a new control variable) is as follows:
            ' [1] Allocate a new temporary local symbol for the control variable
            ' [2] Evaluate <CollectionExpression>, where any reference to "<ControlVariable>" binds to the location in [1]
            ' [3] Allocate a new temporary and initialize it to the result of calling GetEnumerator() on [2]
            ' [4] We will call MoveNext/Current to execute the <Body> a number of times...
            ' For each iteration of the body,
            ' [5] Allocate a new storage location for the control variable
            ' [6] Initialize it to [3].Current
            ' [7] Execute <Body>, where any reference to "<ControlVariable>" binds to the location in [5] for this iteration
            '
            ' Of course, none of this allocation is observable unless there are lambdas.
            ' If there are no lambdas then we can make do with only a single allocation for the whole thing.
            ' But there may be lambdas in either <EnumeratorExpression> or <Body>...
            '
            '
            ' IMPLEMENTATION:
            '
            ' The LambdaRewriter code will, if there were a lambda inside <Body> that captures "<ControlVariable>", 
            ' lift "<ControlVariable>" into a closure which is allocated once for each iteration of the body and initialized with the
            ' (This allocation is initialized by copying the previously allocated closure if there was one).
            '
            ' However, if <EnumeratorExpression> referred to "<ControlVariable>", then we'll need to allocate another one for it.
            ' This rewrite is how we do it:
            '	 Dim tmp ' implicitly initialized with default(CollectionVariableType)
            '	 For Each <ControlVariable> In <CollectionExpression> {tmp/<ControlVariable>}
            '	   <Body>
            '	 Next
            '

            ' if the for each loop declares a variable we need to check if the collection expression references
            ' it. If it does, we allocate a new temporary variable of the control variable's type. All references to
            ' the control variable in the collection expression will be replaced with a reference to the temporary local.
            ' This way capturing this local will be correct.
            ' The variable will not be initialized, because a declared control variable is also not initialized when 
            ' executing the collection expression.
            If node.DeclaredOrInferredLocalOpt IsNot Nothing Then
                Dim tempLocal = New SynthesizedLocal(Me._currentMethodOrLambda, node.ControlVariable.Type, SynthesizedLocalKind.LoweringTemp)
                Dim tempForControlVariable = New BoundLocal(node.Syntax, tempLocal, node.ControlVariable.Type)

                Dim replacedControlVariable As Boolean = False
                replacedCollection = DirectCast(LocalVariableSubstituter.Replace(originalCollection,
                                                                                 node.DeclaredOrInferredLocalOpt,
                                                                                 tempLocal,
                                                                                 RecursionDepth,
                                                                                 replacedControlVariable), BoundExpression)

                ' if a reference to the control variable was found we add the temporary local and we need to make sure
                ' that this variable does not get captured by using a copy constructor.
                ' Example:
                ' for i = 0 to 3 do
                '     for each x in (function() {x+1})()
                '     next x
                ' next i
                '
                ' Should always capture x in the collection expression uninitialized.
                If replacedControlVariable Then
                    locals.Add(tempLocal)

                    If Me._symbolsCapturedWithoutCopyCtor Is Nothing Then
                        Me._symbolsCapturedWithoutCopyCtor = New HashSet(Of Symbol)()
                    End If

                    Me._symbolsCapturedWithoutCopyCtor.Add(tempLocal)
                End If
            End If

            ' Either replace the placeholder with the original collection or the one that the referenced replaced.
            If node.EnumeratorInfo.CollectionPlaceholder IsNot Nothing Then
                AddPlaceholderReplacement(node.EnumeratorInfo.CollectionPlaceholder,
                                          VisitExpressionNode(replacedCollection).MakeRValue)
            End If

            If collectionType.IsArrayType AndAlso DirectCast(collectionType, ArrayTypeSymbol).IsSZArray Then
                ' Optimized rewrite for one dimensional arrays (iterate over index is faster than IEnumerable)
                RewriteForEachArrayOrString(node, statements, locals, isArray:=True, collectionExpression:=replacedCollection)
            ElseIf collectionType.IsStringType Then
                ' Optimized rewrite for strings (iterate over index is faster than IEnumerable)
                RewriteForEachArrayOrString(node, statements, locals, isArray:=False, collectionExpression:=replacedCollection)
            ElseIf Not node.Collection.HasErrors Then
                RewriteForEachIEnumerable(node, statements, locals)
            End If

            If node.EnumeratorInfo.CollectionPlaceholder IsNot Nothing Then
                RemovePlaceholderReplacement(node.EnumeratorInfo.CollectionPlaceholder)
            End If

            Return New BoundBlock(node.Syntax,
                                  Nothing,
                                  locals.ToImmutableAndFree(),
                                  statements.ToImmutableAndFree)
        End Function

        ''' <summary>
        ''' Rewrites a for each over an one dimensional array or a string.
        ''' 
        ''' As an optimization, if c is an array type of rank 1, the form becomes:
        '''
        '''     Dim collectionCopy As C = c
        '''     Dim collectionIndex As Integer = 0
        '''     Do While collectionIndex &lt; len(collectionCopy)    ' len(a) represents the LDLEN opcode
        '''         dim controlVariable = DirectCast(collectionCopy(collectionIndex), typeOfControlVariable)
        '''         &lt;loop body&gt;
        '''     continue:
        '''         collectionIndex += 1
        '''     postIncrement:
        '''     Loop
        '''
        ''' An iteration over a string becomes
        '''     Dim collectionCopy As String = c
        '''     Dim collectionIndex As Integer = 0
        '''     Dim limit as Integer = s.Length
        '''     Do While collectionIndex &lt; limit
        '''         dim controlVariable = DirectCast(collectionCopy.Chars(collectionIndex), typeOfControlVariable)
        '''         &lt;loop body&gt;
        '''     continue:
        '''         collectionIndex += 1
        '''     postIncrement:
        '''     Loop
        ''' </summary>
        ''' <param name="node">The node.</param>
        ''' <param name="statements">The statements.</param>
        ''' <param name="locals">The locals.</param>
        ''' <param name="isArray">if set to <c>true</c> [is array].</param>
        Private Sub RewriteForEachArrayOrString(
            node As BoundForEachStatement,
            statements As ArrayBuilder(Of BoundStatement),
            locals As ArrayBuilder(Of LocalSymbol),
            isArray As Boolean,
            collectionExpression As BoundExpression
        )

            Dim syntaxNode = DirectCast(node.Syntax, ForEachBlockSyntax)

            Dim generateUnstructuredExceptionHandlingResumeCode As Boolean = ShouldGenerateUnstructuredExceptionHandlingResumeCode(node)

            Dim loopResumeTarget As ImmutableArray(Of BoundStatement) = Nothing

            If generateUnstructuredExceptionHandlingResumeCode Then
                loopResumeTarget = RegisterUnstructuredExceptionHandlingResumeTarget(syntaxNode, canThrow:=True)
            End If

            Dim controlVariableType = node.ControlVariable.Type
            Dim enumeratorInfo = node.EnumeratorInfo

            If collectionExpression.Kind = BoundKind.Conversion Then
                Dim conversion = DirectCast(collectionExpression, BoundConversion)
                If Not conversion.ExplicitCastInCode AndAlso conversion.Operand.Type.IsArrayType Then
                    collectionExpression = conversion.Operand
                End If
            End If
            Dim collectionType = collectionExpression.Type

            Debug.Assert(collectionExpression.Type.SpecialType = SpecialType.System_String OrElse
             (collectionType.IsArrayType AndAlso DirectCast(collectionType, ArrayTypeSymbol).IsSZArray))

            ' Where do the bound expressions of the bound for each node get rewritten?
            ' The collection will be assigned to a local copy. This assignment is rewritten by using "CreateLocalAndAssignment"
            ' The current value will be assigned to the control variable once per iteration. This is done in this method
            ' The casts and conversions get rewritten in "RewriteWhileStatement"

            '
            ' Loop initialization: collection copy + index & limit 
            '

            ' Dim collectionCopy As C = c
            Dim boundCollectionLocal As BoundLocal = Nothing
            Dim boundCollectionAssignment = CreateLocalAndAssignment(syntaxNode.ForEachStatement,
                                                                     collectionExpression.MakeRValue(),
                                                                     boundCollectionLocal,
                                                                     locals,
                                                                     SynthesizedLocalKind.ForEachArray)

            If Not loopResumeTarget.IsDefaultOrEmpty Then
                boundCollectionAssignment = New BoundStatementList(boundCollectionAssignment.Syntax, loopResumeTarget.Add(boundCollectionAssignment))
            End If

            If Instrument(node) Then
                ' first sequence point to highlight the for each statement
                boundCollectionAssignment = _instrumenterOpt.InstrumentForEachLoopInitialization(node, boundCollectionAssignment)
            End If

            statements.Add(boundCollectionAssignment)

            ' Dim collectionIndex As Integer = 0
            Dim boundIndex As BoundLocal = Nothing
            Dim integerType = GetSpecialTypeWithUseSiteDiagnostics(SpecialType.System_Int32, syntaxNode)
            Dim boundIndexInitialization = CreateLocalAndAssignment(syntaxNode.ForEachStatement,
                                                                    New BoundLiteral(syntaxNode,
                                                                                     ConstantValue.Default(SpecialType.System_Int32),
                                                                                     integerType),
                                                                    boundIndex,
                                                                    locals,
                                                                    SynthesizedLocalKind.ForEachArrayIndex)
            statements.Add(boundIndexInitialization)

            ' build either
            ' Array.Length(collectionCopy)
            ' or
            ' collectionCopy.Length
            Dim boundLimit As BoundExpression = Nothing
            If isArray Then
                ' in case of an array, the upper limit is Array.Length. This will be the ldlen opcode later on
                boundLimit = New BoundArrayLength(syntaxNode, boundCollectionLocal, integerType)
            Else
                ' for strings we use String.Length
                Dim lengthPropertyGet = GetSpecialTypeMember(SpecialMember.System_String__Length)
                boundLimit = New BoundCall(syntaxNode,
                                                      DirectCast(lengthPropertyGet, MethodSymbol),
                                                      Nothing,
                                                      boundCollectionLocal,
                                                      ImmutableArray(Of BoundExpression).Empty,
                                                      Nothing,
                                                      integerType)
            End If

            '
            ' new statements for the loop body (getting current value + index increment)
            '

            Dim boundCurrent As BoundExpression
            Dim elementType As TypeSymbol
            If isArray Then
                elementType = DirectCast(collectionType, ArrayTypeSymbol).ElementType
                boundCurrent = New BoundArrayAccess(syntaxNode,
                                                    boundCollectionLocal.MakeRValue(),
                                                    ImmutableArray.Create(Of BoundExpression)(boundIndex.MakeRValue()),
                                                    isLValue:=False,
                                                    type:=elementType)
            Else
                elementType = GetSpecialType(SpecialType.System_Char)

                ' controlVariable = StringCopy.Chars(arrayIndex)
                ' because the controlVariable can be any LValue, this might have side effects (e.g. ObjArray(SomeFunc()).field)
                ' we will evaluate the expression once per iteration, the side effects are intended.
                Dim charsPropertyGet As MethodSymbol = Nothing
                If TryGetSpecialMember(charsPropertyGet, SpecialMember.System_String__Chars, syntaxNode) Then
                    boundCurrent = New BoundCall(syntaxNode,
                                             DirectCast(charsPropertyGet, MethodSymbol),
                                             Nothing,
                                             boundCollectionLocal,
                                             ImmutableArray.Create(Of BoundExpression)(boundIndex.MakeRValue()),
                                             constantValueOpt:=Nothing,
                                             type:=elementType)
                Else
                    boundCurrent = New BoundBadExpression(syntaxNode, LookupResultKind.NotReferencable, ImmutableArray(Of Symbol).Empty,
                                                          ImmutableArray.Create(Of BoundExpression)(boundIndex.MakeRValue()), elementType, hasErrors:=True)
                End If
            End If
            ' now we know the bound node for the current value; add it to the replacement map to get inserted into the
            ' conversion from current to the type of the control variable
            If enumeratorInfo.CurrentPlaceholder IsNot Nothing Then
                AddPlaceholderReplacement(enumeratorInfo.CurrentPlaceholder, boundCurrent)
            End If

            ' controlVariable = arrayCopy(arrayIndex)
            ' because the controlVariable can be any LValue, this might have side effects (e.g. ObjArray(SomeFunc()).field)
            ' we will evaluate the expression once per iteration, the side effects are intended.
            Dim boundCurrentAssignment As BoundStatement = New BoundAssignmentOperator(syntaxNode,
                                                                                       node.ControlVariable,
                                                                                       enumeratorInfo.CurrentConversion,
                                                                                       suppressObjectClone:=False,
                                                                                       type:=node.ControlVariable.Type).ToStatement
            boundCurrentAssignment.SetWasCompilerGenerated() ' used to not create sequence points
            boundCurrentAssignment = DirectCast(Visit(boundCurrentAssignment), BoundStatement)

            Dim boundIncrementAssignment = CreateIndexIncrement(syntaxNode, boundIndex)

            '
            ' build loop statement
            '

            ' now build while loop
            Dim boundWhileStatement = CreateLoweredWhileStatements(node,
                                                                   boundLimit,
                                                                   boundIndex,
                                                                   boundCurrentAssignment,
                                                                   boundIncrementAssignment,
                                                                   generateUnstructuredExceptionHandlingResumeCode)

            statements.AddRange(DirectCast(boundWhileStatement, BoundStatementList).Statements)

            If enumeratorInfo.CurrentPlaceholder IsNot Nothing Then
                RemovePlaceholderReplacement(enumeratorInfo.CurrentPlaceholder)
            End If
        End Sub

        ''' <summary>
        ''' Creates a local and assigns it the given bound expression.
        ''' </summary>
        ''' <param name="syntaxNode">The syntax node.</param>
        ''' <param name="initExpression">The initialization expression.</param>
        ''' <param name="boundLocal">The bound local.</param>
        ''' <param name="locals">The locals.</param>
        Private Function CreateLocalAndAssignment(
            syntaxNode As StatementSyntax,
            initExpression As BoundExpression,
            <Out()> ByRef boundLocal As BoundLocal,
            locals As ArrayBuilder(Of LocalSymbol),
            kind As SynthesizedLocalKind
        ) As BoundStatement
            ' Dim collectionCopy As C = c
            Dim expressionType = initExpression.Type
            Debug.Assert(kind.IsLongLived())
            Dim collectionCopy = New SynthesizedLocal(Me._currentMethodOrLambda, expressionType, kind, syntaxNode)
            locals.Add(collectionCopy)
            boundLocal = New BoundLocal(syntaxNode, collectionCopy, expressionType)

            ' if this ever fails, you found a test case to see how the suppressObjectClone flag should be set to match
            ' Dev10 behavior. 
            Debug.Assert(expressionType.SpecialType <> SpecialType.System_Object)
            Dim boundCollectionAssignment = New BoundAssignmentOperator(syntaxNode,
                                                                        boundLocal,
                                                                        VisitAndGenerateObjectCloneIfNeeded(initExpression),
                                                                        suppressObjectClone:=True,
                                                                        type:=expressionType).ToStatement
            boundCollectionAssignment.SetWasCompilerGenerated() ' used to not create sequence points

            Return boundCollectionAssignment
        End Function

        ''' <summary>
        ''' Creates the index increment statement.
        ''' </summary>
        ''' <param name="syntaxNode">The syntax node.</param>
        ''' <param name="boundIndex">The bound index expression (bound local).</param>
        Private Function CreateIndexIncrement(
            syntaxNode As VisualBasicSyntaxNode,
            boundIndex As BoundLocal
        ) As BoundStatement
            ' collectionIndex += 1
            Dim expressionType = boundIndex.Type
            Dim boundAddition = New BoundBinaryOperator(syntaxNode,
                                                        BinaryOperatorKind.Add,
                                                        boundIndex.MakeRValue(),
                                                        New BoundLiteral(syntaxNode, ConstantValue.Create(1), expressionType),
                                                        checked:=True,
                                                        type:=expressionType)

            ' if this ever fails, you found a test case to see how the suppressObjectClone flag should be set to match
            ' Dev10 behavior. 
            Debug.Assert(expressionType.SpecialType <> SpecialType.System_Object)
            Dim boundIncrementAssignment As BoundStatement = New BoundAssignmentOperator(syntaxNode,
                                                                                         boundIndex,
                                                                                         boundAddition,
                                                                                         suppressObjectClone:=False,
                                                                                         type:=expressionType).ToStatement.MakeCompilerGenerated

            boundIncrementAssignment = DirectCast(Visit(boundIncrementAssignment), BoundStatement)

            If Instrument Then
                ' create a hidden sequence point for the index increment to not stop on it while debugging
                boundIncrementAssignment = SyntheticBoundNodeFactory.HiddenSequencePoint(boundIncrementAssignment)
            End If

            Return boundIncrementAssignment
        End Function

        ''' <summary>
        ''' Creates the while statement for the for each rewrite
        ''' </summary>
        ''' <param name="limit">The limit to check the index against.</param>
        ''' <param name="index">The index.</param>
        ''' <param name="currentAssignment">The assignment statement of the current value.</param>
        ''' <param name="incrementAssignment">The increment statement.</param>
        ''' <param name="forEachStatement">The bound for each node.</param>
        ''' <returns>The lowered statement list for the while statement.</returns>
        Private Function CreateLoweredWhileStatements(
            forEachStatement As BoundForEachStatement,
            limit As BoundExpression,
            index As BoundLocal,
            currentAssignment As BoundStatement,
            incrementAssignment As BoundStatement,
            generateUnstructuredExceptionHandlingResumeCode As Boolean
        ) As BoundStatementList

            Dim body = DirectCast(Visit(forEachStatement.Body), BoundStatement)
            Dim statementSyntax = forEachStatement.Syntax
            Dim epilogue As BoundStatement = Nothing

            If generateUnstructuredExceptionHandlingResumeCode Then
                epilogue = New BoundStatementList(statementSyntax, RegisterUnstructuredExceptionHandlingResumeTarget(statementSyntax, canThrow:=True))
            End If

            If Instrument(forEachStatement) Then
                epilogue = _instrumenterOpt.InstrumentForEachLoopEpilogue(forEachStatement, epilogue)
            End If

            If epilogue IsNot Nothing Then
                incrementAssignment = New BoundStatementList(statementSyntax, ImmutableArray.Create(epilogue, incrementAssignment))
            End If

            ' Note: we're moving the continue label before the increment of the array index to not create an infinite loop
            ' if somebody uses "Continue For". This will then increase the index and start a new iteration. 
            ' Also: see while node creation below
            Dim rewrittenBodyStatements = ImmutableArray.Create(Of BoundStatement)(currentAssignment,
                                                                                      body,
                                                                                      New BoundLabelStatement(statementSyntax, forEachStatement.ContinueLabel),
                                                                                      incrementAssignment)

            ' declare the control variable inside of the while loop to capture it for each
            ' iteration of this loop with a copy constructor
            Dim rewrittenBodyBlock As BoundBlock = New BoundBlock(statementSyntax,
                                                                  Nothing,
                                                                  If(forEachStatement.DeclaredOrInferredLocalOpt IsNot Nothing,
                                                                     ImmutableArray.Create(Of LocalSymbol)(forEachStatement.DeclaredOrInferredLocalOpt),
                                                                     ImmutableArray(Of LocalSymbol).Empty),
                                                                  rewrittenBodyStatements)

            Dim booleanType = GetSpecialTypeWithUseSiteDiagnostics(SpecialType.System_Boolean, statementSyntax)
            Dim boundCondition = TransformRewrittenBinaryOperator(
                                    New BoundBinaryOperator(statementSyntax,
                                                            BinaryOperatorKind.LessThan,
                                                            index.MakeRValue(),
                                                            limit,
                                                            checked:=False,
                                                            type:=booleanType))

            ' now build while loop
            ' Note: we're creating a new label for the while loop that get's used for the initial jump from the 
            ' beginning of the loop to the condition to check it for the first time.
            ' Also: see while body creation above
            Dim boundWhileStatement = RewriteWhileStatement(forEachStatement,
                                                            VisitExpressionNode(boundCondition),
                                                            rewrittenBodyBlock,
                                                            New GeneratedLabelSymbol("postIncrement"),
                                                            forEachStatement.ExitLabel)
            Return DirectCast(boundWhileStatement, BoundStatementList)
        End Function

        ''' <summary>
        ''' Rewrite a for each that uses IEnumerable. It's basic form is:
        '''
        '''     Dim e As E = c.GetEnumerator()
        '''     Do While e.MoveNext()
        '''        controlVariable = e.Current
        '''        &lt;loop body&gt;
        '''     Loop
        '''
        ''' To support disposable enumerators, the compiler will generate code to dispose the
        ''' enumerator after loop termination.  Only when E implements IDisposable can this be done.
        ''' The one exception to this rule is when E is specifically IEnumerator, in which case
        ''' the compiler will generate code to dynamically query the enumerator to determine
        ''' if it implements IDisposable.
        '''
        ''' If E is IEnumerator the loop becomes:
        '''
        '''     Dim e As IEnumerator = c.GetEnumerator()
        '''     Try
        '''         Do While e.MoveNext()
        '''            dim controlVariable = e.Current
        '''            &lt;loop body&gt;
        '''         Loop
        '''     Finally
        '''         If TryCast(e, IDisposable) IsNot Nothing then
        '''             CType(e, IDisposable).Dispose()
        '''         End If
        '''     End Try
        '''
        ''' If E is known at compile time to implement IDisposable the loop becomes:
        '''
        '''     Dim e As E = c.GetEnumerator()
        '''     Try
        '''         Do While e.MoveNext()
        '''            dim controlVariable = e.Current
        '''            &lt;loop body&gt;
        '''         Loop
        '''     Finally
        '''         If Not e Is Nothing Then
        '''             CType(e, IDisposable).Dispose()
        '''         End If
        '''     End Try
        '''
        ''' The exception to these forms is the existence of On Error in which case the Try/Finally
        ''' block will be eliminated (instead the body of the Finally will immediately follow
        ''' the end of the loop).
        ''' </summary>
        ''' <param name="node"></param>
        ''' <param name="statements"></param>
        ''' <param name="locals"></param>
        Private Sub RewriteForEachIEnumerable(
            node As BoundForEachStatement,
            statements As ArrayBuilder(Of BoundStatement),
            locals As ArrayBuilder(Of LocalSymbol)
        )
            Dim syntaxNode = DirectCast(node.Syntax, ForEachBlockSyntax)
            Dim enumeratorInfo = node.EnumeratorInfo

            ' We don't wrap the loop with a Try block if On Error is present.
            Dim needTryFinally As Boolean = enumeratorInfo.NeedToDispose AndAlso Not InsideValidUnstructuredExceptionHandlingOnErrorContext()

            Dim saveState As UnstructuredExceptionHandlingContext = Nothing

            If needTryFinally Then
                ' Unstructured Exception Handling should be disabled inside compiler generated Try/Catch/Finally.
                saveState = LeaveUnstructuredExceptionHandlingContext(node)
            End If

            Dim generateUnstructuredExceptionHandlingResumeCode As Boolean = ShouldGenerateUnstructuredExceptionHandlingResumeCode(node)

            Dim loopResumeTarget As ImmutableArray(Of BoundStatement) = Nothing

            If generateUnstructuredExceptionHandlingResumeCode Then
                loopResumeTarget = RegisterUnstructuredExceptionHandlingResumeTarget(syntaxNode, canThrow:=True)
            End If

            ' Where do the bound expressions of the bound for each node get rewritten?
            ' The collection will be used in a call to GetEnumerator() and assigned to a local. This assignment is rewritten 
            ' by using "CreateLocalAndAssignment"
            ' The current value will be assigned to the control variable once per iteration. This is done in this method
            ' The casts and conversions get rewritten in "RewriteWhileStatement".

            ' Get Enumerator and store it in a temporary
            ' FYI: The GetEnumerator call accesses the collection and does not contain a placeholder.
            Dim boundEnumeratorLocal As BoundLocal = Nothing
            Dim boundEnumeratorAssignment = CreateLocalAndAssignment(syntaxNode.ForEachStatement,
                                                                     enumeratorInfo.GetEnumerator,
                                                                     boundEnumeratorLocal,
                                                                     locals,
                                                                     SynthesizedLocalKind.ForEachEnumerator)

            If Not loopResumeTarget.IsDefaultOrEmpty Then
                boundEnumeratorAssignment = New BoundStatementList(boundEnumeratorAssignment.Syntax, loopResumeTarget.Add(boundEnumeratorAssignment))
            End If

            If Instrument(node) Then
                ' first sequence point; highlight for each statement
                boundEnumeratorAssignment = _instrumenterOpt.InstrumentForEachLoopInitialization(node, boundEnumeratorAssignment)
            End If

            Debug.Assert(enumeratorInfo.EnumeratorPlaceholder IsNot Nothing)
            AddPlaceholderReplacement(enumeratorInfo.EnumeratorPlaceholder, boundEnumeratorLocal)

            ' Dev10 adds a conversion here from the result of MoveNext to Boolean. However we know for sure that the return
            ' type must be boolean because we check specifically for the return type in case of the design pattern, or we look
            ' up the method by using GetSpecialTypeMember which has the return type encoded.
            Debug.Assert(enumeratorInfo.MoveNext.Type.SpecialType = SpecialType.System_Boolean)

            If enumeratorInfo.CurrentPlaceholder IsNot Nothing Then
                AddPlaceholderReplacement(enumeratorInfo.CurrentPlaceholder, VisitExpressionNode(enumeratorInfo.Current))
            End If

            ' assign the returned value from current to the control variable
            Dim boundCurrentAssignment = New BoundAssignmentOperator(syntaxNode,
                                                                     node.ControlVariable,
                                                                     enumeratorInfo.CurrentConversion,
                                                                     suppressObjectClone:=False,
                                                                     type:=node.ControlVariable.Type).ToStatement
            boundCurrentAssignment.SetWasCompilerGenerated() ' used to not create sequence points

            Dim rewrittenBodyStatements = ImmutableArray.Create(Of BoundStatement)(DirectCast(Visit(boundCurrentAssignment), BoundStatement),
                                                                                  DirectCast(Visit(node.Body), BoundStatement))

            ' declare the control variable inside of the while loop to capture it for each
            ' iteration of this loop with a copy constructor
            Dim rewrittenBodyBlock As BoundBlock = New BoundBlock(syntaxNode, Nothing, If(node.DeclaredOrInferredLocalOpt IsNot Nothing, ImmutableArray.Create(Of LocalSymbol)(node.DeclaredOrInferredLocalOpt), ImmutableArray(Of LocalSymbol).Empty), rewrittenBodyStatements)

            Dim bodyEpilogue As BoundStatement = New BoundLabelStatement(syntaxNode, node.ContinueLabel)

            If generateUnstructuredExceptionHandlingResumeCode Then
                bodyEpilogue = Concat(bodyEpilogue, New BoundStatementList(syntaxNode, RegisterUnstructuredExceptionHandlingResumeTarget(syntaxNode, canThrow:=True)))
            End If

            If Instrument(node) Then
                bodyEpilogue = _instrumenterOpt.InstrumentForEachLoopEpilogue(node, bodyEpilogue)
            End If

            rewrittenBodyBlock = AppendToBlock(rewrittenBodyBlock, bodyEpilogue)

            ' now build while loop
            Dim boundWhileStatement = RewriteWhileStatement(node,
                                                            VisitExpressionNode(enumeratorInfo.MoveNext),
                                                            rewrittenBodyBlock,
                                                            New GeneratedLabelSymbol("MoveNextLabel"),
                                                            node.ExitLabel)

            Dim visitedWhile = DirectCast(boundWhileStatement, BoundStatementList)

            If enumeratorInfo.CurrentPlaceholder IsNot Nothing Then
                RemovePlaceholderReplacement(enumeratorInfo.CurrentPlaceholder)
            End If

            If enumeratorInfo.NeedToDispose Then
                Dim disposalStatement = GenerateDisposeCallForForeachAndUsing(node.Syntax,
                                                                              boundEnumeratorLocal,
                                                                              VisitExpressionNode(enumeratorInfo.DisposeCondition),
                                                                              enumeratorInfo.IsOrInheritsFromOrImplementsIDisposable,
                                                                              VisitExpressionNode(enumeratorInfo.DisposeCast))

                If Not needTryFinally Then
                    statements.Add(boundEnumeratorAssignment)
                    statements.Add(visitedWhile)

                    If generateUnstructuredExceptionHandlingResumeCode Then
                        ' Do not reenter the loop if Dispose throws.
                        RegisterUnstructuredExceptionHandlingResumeTarget(syntaxNode, canThrow:=True, statements:=statements)
                    End If

                    statements.Add(disposalStatement)
                Else
                    Dim boundTryFinally = New BoundTryStatement(syntaxNode,
                                                                New BoundBlock(syntaxNode,
                                                                               Nothing, ImmutableArray(Of LocalSymbol).Empty,
                                                                               ImmutableArray.Create(Of BoundStatement)(boundEnumeratorAssignment,
                                                                                                                       visitedWhile)),
                                                                ImmutableArray(Of BoundCatchBlock).Empty,
                                                                New BoundBlock(syntaxNode,
                                                                               Nothing, ImmutableArray(Of LocalSymbol).Empty,
                                                                               ImmutableArray.Create(Of BoundStatement)(disposalStatement)),
                                                                           exitLabelOpt:=Nothing)
                    boundTryFinally.SetWasCompilerGenerated() ' used to not create sequence points
                    statements.Add(boundTryFinally)
                End If
            Else
                Debug.Assert(Not needTryFinally)
                statements.Add(boundEnumeratorAssignment)
                statements.AddRange(visitedWhile)
            End If

            If needTryFinally Then
                RestoreUnstructuredExceptionHandlingContext(node, saveState)
            End If

            RemovePlaceholderReplacement(enumeratorInfo.EnumeratorPlaceholder)
        End Sub

        ''' <summary>
        ''' Depending on whether the bound local's type is, implements or inherits IDisposable for sure, or might implement it,
        ''' this function returns the statements to call Dispose on the bound local.
        ''' 
        ''' If it's known to implement IDisposable, the generated code looks like this for reference types:
        '''     If e IsNot Nothing Then
        '''       CType(e, IDisposable).Dispose()
        '''     End If
        ''' or
        ''' e.Dispose()
        ''' for value types (including type parameters with a value constraint).
        ''' Otherwise it looks like the following
        '''     If TryCast(e, IDisposable) IsNot Nothing then
        '''       CType(e, IDisposable).Dispose()
        '''     End If
        ''' </summary>
        ''' <remarks>This method is used by the for each rewriter and the using rewriter. The latter should only call 
        ''' this method with both IsOrInheritsFromOrImplementsIDisposable and needToDispose set to true, as using is not
        ''' pattern based and must implement IDisposable.
        ''' </remarks>
        ''' <param name="syntaxNode">The syntax node.</param>
        ''' <param name="rewrittenBoundLocal">The bound local.</param>
        ''' <param name="rewrittenCondition">The condition used in the if statement around the dispose call</param>
        ''' <param name="IsOrInheritsFromOrImplementsIDisposable">A flag indicating whether the bound local's type is,
        ''' inherits or implements IDisposable or not.</param>
        ''' <param name="rewrittenDisposeConversion">Conversion from the local type to IDisposable</param>
        Public Function GenerateDisposeCallForForeachAndUsing(
            syntaxNode As SyntaxNode,
            rewrittenBoundLocal As BoundLocal,
            rewrittenCondition As BoundExpression,
            IsOrInheritsFromOrImplementsIDisposable As Boolean,
            rewrittenDisposeConversion As BoundExpression
        ) As BoundStatement
            Dim disposeMethod As MethodSymbol = Nothing
            If Not TryGetSpecialMember(disposeMethod, SpecialMember.System_IDisposable__Dispose, syntaxNode) Then
                Return New BoundBadExpression(syntaxNode, LookupResultKind.NotReferencable, ImmutableArray(Of Symbol).Empty,
                                              If(rewrittenCondition IsNot Nothing,
                                                 ImmutableArray.Create(rewrittenBoundLocal, rewrittenCondition),
                                                 ImmutableArray.Create(Of BoundExpression)(rewrittenBoundLocal)),
                                              ErrorTypeSymbol.UnknownResultType, hasErrors:=True).ToStatement()
            End If

            Dim voidType = GetSpecialTypeWithUseSiteDiagnostics(SpecialType.System_Void, syntaxNode)

            Dim localType = rewrittenBoundLocal.Type

            ' the bound nodes for the condition and the cast both come from the initial binding and are contained in
            ' the bound for each node.
            ' This way the construction of the "reference type implements IDisposable" case and the case of 
            ' "may implement IDisposable" can be handled by the same code fragment. Only the value types know to implement
            ' IDisposable need to be handled differently

            Dim boundCall As BoundStatement
            If IsOrInheritsFromOrImplementsIDisposable AndAlso
                (localType.IsValueType OrElse localType.IsTypeParameter) Then

                ' there's no need to cast the enumerator if it's known to be a value type, because they are implicitly
                ' sealed the methods cannot ever be overridden.

                ' this will be a constrained call, because the receiver is a value type and the method is an
                ' interface method: 
                ' e.Dispose()    ' constrained call
                boundCall = New BoundCall(syntaxNode,
                                          disposeMethod,
                                          Nothing,
                                          rewrittenBoundLocal,
                                          ImmutableArray(Of BoundExpression).Empty,
                                          constantValueOpt:=Nothing,
                                          type:=voidType).ToStatement
                boundCall.SetWasCompilerGenerated() ' used to not create sequence points

                If localType.IsValueType Then
                    Return boundCall
                End If
            Else
                Debug.Assert(rewrittenDisposeConversion IsNot Nothing)

                ' call to Dispose
                boundCall = New BoundCall(syntaxNode,
                                          disposeMethod,
                                          Nothing,
                                          rewrittenDisposeConversion,
                                          ImmutableArray(Of BoundExpression).Empty, Nothing, voidType).ToStatement
                boundCall.SetWasCompilerGenerated() ' used to not create sequence points
            End If

            ' if statement (either the condition is "e IsNot nothing" or "TryCast(e, IDisposable) IsNot Nothing", see comment above)
            Return RewriteIfStatement(syntaxNode, rewrittenCondition, boundCall, Nothing, instrumentationTargetOpt:=Nothing)
        End Function

        ''' <summary>
        ''' Internal helper class to replace local symbols in bound locals of a given bound tree.
        ''' </summary>
        Private NotInheritable Class LocalVariableSubstituter
            Inherits BoundTreeRewriterWithStackGuardWithoutRecursionOnTheLeftOfBinaryOperator

            Private ReadOnly _original As LocalSymbol
            Private ReadOnly _replacement As LocalSymbol
            Private _replacedNode As Boolean = False

            Public Shared Function Replace(
                node As BoundNode,
                original As LocalSymbol,
                replacement As LocalSymbol,
                recursionDepth As Integer,
                ByRef replacedNode As Boolean
            ) As BoundNode
                Dim rewriter As New LocalVariableSubstituter(original, replacement, recursionDepth)

                Dim result = rewriter.Visit(node)
                replacedNode = rewriter.ReplacedNode

                Return result
            End Function

            Private ReadOnly Property ReplacedNode As Boolean
                Get
                    Return _replacedNode
                End Get
            End Property

            Private Sub New(original As LocalSymbol, replacement As LocalSymbol, recursionDepth As Integer)
                MyBase.New(recursionDepth)
                _original = original
                _replacement = replacement
            End Sub

            Public Overrides Function VisitLocal(node As BoundLocal) As BoundNode

                If node.LocalSymbol Is _original Then
                    _replacedNode = True

                    Return node.Update(_replacement, node.IsLValue, node.Type)
                End If

                Return node
            End Function
        End Class

    End Class
End Namespace
