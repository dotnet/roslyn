' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable
Imports System.Runtime.InteropServices
Imports Microsoft.CodeAnalysis.PooledObjects
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports TypeKind = Microsoft.CodeAnalysis.TypeKind

Namespace Microsoft.CodeAnalysis.VisualBasic.CodeGen
    Partial Friend Class StackScheduler

        ''' <summary>
        ''' context of expression evaluation. 
        ''' it will affect inference of stack behavior
        ''' it will also affect when expressions can be dup-reused
        '''     Example:
        '''         Goo(x, ref x)     x cannot be duped as it is used in different context  
        ''' </summary>
        Private Enum ExprContext
            None
            Sideeffects
            Value
            Address
            AssignmentTarget
            Box
        End Enum

        ''' <summary>
        ''' Analyzes the tree trying to figure which locals may live on stack. It is 
        ''' a fairly delicate process and must be very familiar with how CodeGen works. 
        ''' It is essentially a part of CodeGen.
        ''' 
        ''' NOTE: It is always safe to mark a local as not eligible as a stack local 
        ''' so when situation gets complicated we just refuse to schedule and move on.
        ''' </summary>
        Private NotInheritable Class Analyzer
            Inherits BoundTreeRewriter

            Private ReadOnly _container As Symbol

            Private _counter As Integer = 0
            Private ReadOnly _evalStack As ArrayBuilder(Of (expression As BoundExpression, context As ExprContext))
            Private ReadOnly _debugFriendly As Boolean

            Private _context As ExprContext = ExprContext.None
            Private _assignmentLocal As BoundLocal = Nothing

            Private ReadOnly _locals As New Dictionary(Of LocalSymbol, LocalDefUseInfo)

            ''' <summary>
            ''' fake local that represents the eval stack. when we need to ensure that eval
            ''' stack is not blocked by stack Locals, we record an access to empty.
            ''' </summary>
            Private ReadOnly _empty As DummyLocal

            ' we need to guarantee same stack patterns at branches and labels. we do that by placing 
            ' a fake dummy local at one end of a branch and force that it is accessible at another.
            ' if any stack local tries to intervene and misbalance the stack, it will clash with 
            ' the dummy and will be rejected.
            Private ReadOnly _dummyVariables As New Dictionary(Of Object, DummyLocal)

            Private _recursionDepth As Integer

            Private Sub New(container As Symbol,
                            evalStack As ArrayBuilder(Of ValueTuple(Of BoundExpression, ExprContext)),
                            debugFriendly As Boolean)

                Me._container = container
                Me._evalStack = evalStack
                Me._debugFriendly = debugFriendly
                Me._empty = New DummyLocal(container)

                ' this is the top of eval stack
                DeclareLocal(_empty, 0)
                RecordDummyWrite(_empty)
            End Sub

            Public Shared Function Analyze(
                             container As Symbol,
                             node As BoundNode,
                             debugFriendly As Boolean,
                             <Out> ByRef locals As Dictionary(Of LocalSymbol, LocalDefUseInfo)) As BoundNode

                Dim evalStack = ArrayBuilder(Of ValueTuple(Of BoundExpression, ExprContext)).GetInstance()
                Dim analyzer = New Analyzer(container, evalStack, debugFriendly)
                Dim rewritten As BoundNode = analyzer.Visit(node)
                evalStack.Free()

                locals = analyzer._locals

                Return rewritten
            End Function

            Public Overrides Function Visit(node As BoundNode) As BoundNode
                Dim result As BoundNode

                Dim expr = TryCast(node, BoundExpression)
                If expr IsNot Nothing Then
                    Debug.Assert(expr.Kind <> BoundKind.Label)
                    result = VisitExpression(expr, ExprContext.Value)
                Else
                    result = VisitStatement(node)
                End If

                Return result
            End Function

            Private Function VisitExpressionCore(node As BoundExpression, context As ExprContext) As BoundExpression
                If node Is Nothing Then
                    Me._counter += 1
                    Return node
                End If

                Dim prevContext As ExprContext = Me._context
                Dim prevStack As Integer = Me.StackDepth()
                Me._context = context

                ' Don not recurse into constant expressions. Their children do Not push any values.
                Dim result = If(node.ConstantValueOpt Is Nothing,
                                DirectCast(MyBase.Visit(node), BoundExpression),
                                node)

                _context = prevContext
                _counter += 1

                Select Case context

                    Case ExprContext.Sideeffects
                        SetStackDepth(prevStack)

                    Case ExprContext.AssignmentTarget
                        Exit Select

                    Case ExprContext.Value, ExprContext.Address, ExprContext.Box
                        SetStackDepth(prevStack)
                        PushEvalStack(node, context)

                    Case Else
                        Throw ExceptionUtilities.UnexpectedValue(context)
                End Select

                Return result
            End Function

            Private Sub PushEvalStack(result As BoundExpression, context As ExprContext)
                Debug.Assert(result IsNot Nothing OrElse context = ExprContext.None)
                _evalStack.Add((result, context))
            End Sub

            Private Function StackDepth() As Integer
                Return _evalStack.Count
            End Function

            Private Function EvalStackIsEmpty() As Boolean
                Return StackDepth() = 0
            End Function

            Private Sub SetStackDepth(depth As Integer)
                _evalStack.Clip(depth)
            End Sub

            Private Sub PopEvalStack()
                SetStackDepth(_evalStack.Count - 1)
            End Sub

            Private Sub ClearEvalStack()
                _evalStack.Clear()
            End Sub

            Private Function VisitExpression(node As BoundExpression, context As ExprContext) As BoundExpression
                Dim result As BoundExpression

                _recursionDepth += 1

                If _recursionDepth > 1 Then
                    StackGuard.EnsureSufficientExecutionStack(_recursionDepth)

                    result = VisitExpressionCore(node, context)
                Else
                    result = VisitExpressionCoreWithStackGuard(node, context)
                End If

                _recursionDepth -= 1

                Return result
            End Function

            Private Function VisitExpressionCoreWithStackGuard(node As BoundExpression, context As ExprContext) As BoundExpression
                Debug.Assert(_recursionDepth = 1)

                Try
                    Dim result = VisitExpressionCore(node, context)
                    Debug.Assert(_recursionDepth = 1)

                    Return result

                Catch ex As Exception When StackGuard.IsInsufficientExecutionStackException(ex)
                    Throw New CancelledByStackGuardException(ex, node)
                End Try
            End Function

            Protected Overrides Function VisitExpressionWithoutStackGuard(node As BoundExpression) As BoundExpression
                Throw ExceptionUtilities.Unreachable
            End Function

            Public Overrides Function VisitSpillSequence(node As BoundSpillSequence) As BoundNode
                Throw ExceptionUtilities.Unreachable
            End Function

            Private Function VisitStatement(node As BoundNode) As BoundNode
                Debug.Assert(node Is Nothing OrElse EvalStackIsEmpty())

                Dim origStack = StackDepth()
                Dim prevContext As ExprContext = Me._context

                Dim result As BoundNode = MyBase.Visit(node)

                ' prevent cross-statement local optimizations
                ' when emitting debug-friendly code.
                If _debugFriendly Then
                    EnsureOnlyEvalStack()
                End If

                Me._context = prevContext
                SetStackDepth(origStack)
                _counter += 1

                Return result
            End Function


            ''' <summary>
            ''' here we have a case of indirect assignment:  *t1 = expr;
            ''' normally we would need to push t1 and that will cause spilling of t2
            ''' 
            ''' TODO: an interesting case arises in unused x[i]++  and ++x[i] :
            '''       we have trees that look like:
            '''
            '''    t1 = &amp;(x[0])
            '''    t2 = *t1
            '''   *t1 = t2 + 1
            '''
            '''    t1 = &amp;(x[0])
            '''    t2 = *t1 + 1
            '''   *t1 = t2
            '''
            '''  in these cases, we could keep t2 on stack (dev10 does).
            '''  we are dealing with exactly 2 locals and access them in strict order 
            '''  t1, t2, t1, t2  and we are not using t2 after that.
            '''  We may consider detecting exactly these cases and pretend that we do not need 
            '''  to push either t1 or t2 in this case.
            ''' </summary>
            Private Function LhsUsesStackWhenAssignedTo(node As BoundNode, context As ExprContext) As Boolean
                Debug.Assert(context = ExprContext.AssignmentTarget)

                If node Is Nothing Then
                    Return Nothing
                End If

                Select Case node.Kind
                    Case BoundKind.Local, BoundKind.Parameter
                        Return False

                    Case BoundKind.FieldAccess
                        Return Not DirectCast(node, BoundFieldAccess).FieldSymbol.IsShared

                    Case BoundKind.Sequence
                        Return LhsUsesStackWhenAssignedTo(DirectCast(node, BoundSequence).ValueOpt, context)

                End Select

                Return True
            End Function

            Public Overrides Function VisitBlock(node As BoundBlock) As BoundNode
                Debug.Assert(EvalStackIsEmpty(), "entering blocks when evaluation stack is not empty?")

                ' normally we would not allow stack locals
                ' when evaluation stack is not empty.
                DeclareLocals(node.Locals, 0)

                Return MyBase.VisitBlock(node)
            End Function

            Public Overrides Function VisitSequence(node As BoundSequence) As BoundNode
                ' Normally we can only use stack for local scheduling if stack is not used for evaluation.
                ' In a context of a regular block that simply means that eval stack must be empty.
                ' Sequences, can be entered on a nonempty evaluation stack
                ' Ex:
                '      a.b = Seq{var y, y = 1, y}  // a is on the stack for the duration of the sequence.
                '
                ' However evaluation stack at the entry cannot be used inside the sequence, so such stack 
                ' works as effective "empty" for locals declared in sequence.
                ' Therefore sequence locals can be stack scheduled at same stack as at the entry to the sequence.

                ' it may seem attractive to relax the stack requirement to be: 
                ' "all uses must agree on stack depth".
                ' The following example illustrates a case where x is safely used at "declarationStack + 1" 
                ' Ex: 
                '      Seq{var x; y.a = Seq{x = 1; x}; y}  // x is used while y is on the eval stack
                '
                ' It is, however not safe assumption in general since eval stack may be accessed between usages.
                ' Ex:
                '      Seq{var x; y.a = Seq{x = 1; x}; y.z = x; y} // x blocks access to y
                ' 
                '
                ' ---------------------------------------------------
                ' NOTE: The following is not implemented in VB
                '
                ' There is one case where we want to tweak the "use at declaration stack" rule - in the case of 
                ' compound assignment that involves ByRef operand captures (like:   x[y]++ ) . 
                '
                ' Those cases produce specific sequences of the shapes:
                '
                '      prefix:  Seq{var temp, ref operand; operand initializers; *operand = Seq{temp = (T)(operand + 1);  temp;}          result: temp}
                '      postfix: Seq{var temp, ref operand; operand initializers; *operand = Seq{temp = operand;        ;  (T)(temp + 1);} result: temp}
                '
                '  1) temp is used as the result of the sequence (and that is the only reason why it is declared in the outer sequence).
                '  2) all side-effects except the last one do not use the temp.
                '  3) last side-effect is an indirect assignment of a sequence (and target does not involve the temp).
                '            
                '  Note that in a case of side-effects context, the result value will be ignored and therefore
                '  all usages of the nested temp will be confined to the nested sequence that is executed at +1 stack.
                '
                '  We will detect such case and indicate +1 as the desired stack depth at local accesses.
                ' ---------------------------------------------------
                '    
                Dim declarationStack As Integer = Me.StackDepth()

                Dim locals = node.Locals
                If Not locals.IsEmpty Then
                    If Me._context = ExprContext.Sideeffects Then
                        DeclareLocals(locals, declarationStack)

                        ' ---------------------------------------------------
                        ' NOTE: not implemented workaround from above
                        '            foreach (var local in locals)
                        '            {
                        '                if (IsNestedLocalOfCompoundOperator(local, node))
                        '                {
                        '                    // special case
                        '                    DeclareLocal(local, declarationStack + 1);
                        '                }
                        '                else
                        '                {
                        '                    DeclareLocal(local, declarationStack);
                        '                }
                        '            }
                        '        }
                        ' ---------------------------------------------------
                    Else
                        DeclareLocals(locals, declarationStack)
                    End If

                End If

                ' rewrite operands

                Dim origContext As ExprContext = Me._context

                Dim sideeffects As ImmutableArray(Of BoundExpression) = node.SideEffects
                Dim rewrittenSideeffects As ArrayBuilder(Of BoundExpression) = Nothing
                If Not sideeffects.IsDefault Then
                    For i = 0 To sideeffects.Length - 1
                        Dim sideeffect As BoundExpression = sideeffects(i)
                        Dim rewrittenSideeffect As BoundExpression = Me.VisitExpression(sideeffect, ExprContext.Sideeffects)

                        If rewrittenSideeffects Is Nothing AndAlso rewrittenSideeffect IsNot sideeffect Then
                            rewrittenSideeffects = ArrayBuilder(Of BoundExpression).GetInstance()
                            rewrittenSideeffects.AddRange(sideeffects, i)
                        End If

                        If rewrittenSideeffects IsNot Nothing Then
                            rewrittenSideeffects.Add(rewrittenSideeffect)
                        End If

                    Next
                End If

                Dim value As BoundExpression = Me.VisitExpression(node.ValueOpt, origContext)

                Return node.Update(node.Locals,
                                   If(rewrittenSideeffects IsNot Nothing, rewrittenSideeffects.ToImmutableAndFree(), sideeffects),
                                   value, node.Type)
            End Function

#If False Then

            '// detect a pattern used in compound operators
            '// where a temp is declared in the outer sequence
            '// only because it must be returned, otherwise all uses are 
            '// confined to the nested sequence that is indirectly assigned (and therefore has +1 stack)
            '// in such case the desired stack for this local is +1
            'private bool IsNestedLocalOfCompoundOperator(LocalSymbol local, BoundSequence node)
            '{
            '    var value = node.Value;

            '    // local must be used as the value of the sequence.
            '    if (value != null && value.Kind == BoundKind.Local && ((BoundLocal)value).LocalSymbol == local)
            '    {
            '        var sideeffects = node.SideEffects;
            '        var lastSideeffect = sideeffects.LastOrDefault();

            '        if (lastSideeffect != null)
            '        {
            '            // last side-effect must be an indirect assignment of a sequence.
            '            if (lastSideeffect.Kind == BoundKind.AssignmentOperator)
            '            {
            '                var assignment = (BoundAssignmentOperator)lastSideeffect;
            '                if (IsIndirectAssignment(assignment) &&
            '                    assignment.Right.Kind == BoundKind.Sequence)
            '                {
            '                    // and no other side-effects should use the variable
            '                    var localUsedWalker = new LocalUsedWalker(local);
            '                    for (int i = 0; i < sideeffects.Count - 1; i++)
            '                    {
            '                        if (localUsedWalker.IsLocalUsedIn(sideeffects[i]))
            '                        {
            '                            return false;
            '                        }
            '                    }

            '                    // and local is not used on the left of the assignment 
            '                    // (extra check, but better be safe)
            '                    if (localUsedWalker.IsLocalUsedIn(assignment.Left))
            '                    {
            '                        return false;
            '                    }

            '                    // it should be used somewhere
            '                    Debug.Assert(localUsedWalker.IsLocalUsedIn(assignment.Right), "who assigns the temp?");

            '                    return true;
            '                }
            '            }
            '        }
            '    }

            '    return false;
            '}

            'private class LocalUsedWalker : BoundTreeWalker
            '{
            '    private readonly LocalSymbol local;
            '    private bool found;

            '    internal LocalUsedWalker(LocalSymbol local)
            '    {
            '        this.local = local;
            '    }

            '    public bool IsLocalUsedIn(BoundExpression node)
            '    {
            '        this.found = false;
            '        this.Visit(node);

            '        return found;
            '    }

            '    public override BoundNode Visit(BoundNode node)
            '    {
            '        if (!found)
            '        {
            '            return base.Visit(node);
            '        }

            '        return null;
            '    }

            '    public override BoundNode VisitLocal(BoundLocal node)
            '    {
            '        if (node.LocalSymbol == local)
            '        {
            '            this.found = true;
            '        }

            '        return null;
            '    }
            '}

#End If

            Public Overrides Function VisitExpressionStatement(node As BoundExpressionStatement) As BoundNode
                Return node.Update(Me.VisitExpression(node.Expression, ExprContext.Sideeffects))
            End Function

            Public Overrides Function VisitLocal(node As BoundLocal) As BoundNode
                If node.ConstantValueOpt Is Nothing Then
                    Select Case Me._context
                        Case ExprContext.Address
                            If node.LocalSymbol.IsByRef Then
                                RecordVarRead(node.LocalSymbol)
                            Else
                                RecordVarRef(node.LocalSymbol)
                            End If

                        Case ExprContext.AssignmentTarget
                            Debug.Assert(Me._assignmentLocal Is Nothing)

                            ' actual assignment will happen later, after Right is evaluated
                            ' just remember what we are assigning to.
                            Me._assignmentLocal = node

                        Case ExprContext.Sideeffects
                            ' do nothing

                        Case ExprContext.Value,
                            ExprContext.Box
                            RecordVarRead(node.LocalSymbol)

                    End Select
                End If

                Return MyBase.VisitLocal(node)
            End Function

            Public Overrides Function VisitReferenceAssignment(node As BoundReferenceAssignment) As BoundNode
                ' Visit a local in context of regular assignment
                Dim left = DirectCast(VisitExpression(node.ByRefLocal, ExprContext.AssignmentTarget), BoundLocal)

                Dim storedAssignmentLocal = Me._assignmentLocal
                Me._assignmentLocal = Nothing

                ' Visit a l-value expression in context of 'address'
                Dim right As BoundExpression = VisitExpression(node.LValue, ExprContext.Address)

                ' record the Write to the local
                Debug.Assert(storedAssignmentLocal IsNot Nothing)

                ' this assert will fire if code relies on implicit CLR coercions 
                ' - i.e assigns int value to a short local.
                ' in that case we should force lhs to be a real local
                Debug.Assert(node.ByRefLocal.Type.IsSameTypeIgnoringAll(node.LValue.Type),
                             "cannot use stack when assignment involves implicit coercion of the value")

                RecordVarWrite(storedAssignmentLocal.LocalSymbol)

                Return node.Update(left, right, node.IsLValue, node.Type)
            End Function

            Public Overrides Function VisitAssignmentOperator(node As BoundAssignmentOperator) As BoundNode
                Dim isIndirect As Boolean = IsIndirectAssignment(node)

                Dim left As BoundExpression = VisitExpression(node.Left,
                                                              If(isIndirect,
                                                                 ExprContext.Address,
                                                                 ExprContext.AssignmentTarget))

                ' must delay recording a write until after RHS is evaluated
                Dim storedAssignmentLocal = Me._assignmentLocal
                Me._assignmentLocal = Nothing
                Debug.Assert(Me._context <> ExprContext.AssignmentTarget, "assignment expression cannot be a target of another assignment")

                ' Left on the right should be Nothing by this time
                Debug.Assert(node.LeftOnTheRightOpt Is Nothing)

                ' Do not visit "Left on the right"
                'Me.counter += 1

                Dim rhsContext As ExprContext
                If Me._context = ExprContext.Address Then
                    ' we need the address of rhs so we cannot have it on the stack.
                    rhsContext = ExprContext.Address
                Else

                    Debug.Assert(Me._context = ExprContext.Value OrElse
                                 Me._context = ExprContext.Box OrElse
                                 Me._context = ExprContext.Sideeffects, "assignment expression cannot be a target of another assignment")
                    ' we only need a value of rhs, so if otherwise possible it can be a stack value.
                    rhsContext = ExprContext.Value
                End If

                Dim right As BoundExpression = node.Right

                ' if right is a struct ctor, it may be optimized into in-place call
                ' Such call will push the receiver ref before the arguments
                ' so we need to ensure that arguments cannot use stack temps
                Dim leftType As TypeSymbol = left.Type
                Dim mayPushReceiver As Boolean = False
                If right.Kind = BoundKind.ObjectCreationExpression Then
                    Dim ctor = DirectCast(right, BoundObjectCreationExpression).ConstructorOpt
                    If ctor IsNot Nothing AndAlso ctor.ParameterCount <> 0 Then
                        mayPushReceiver = True
                    End If
                End If

                If mayPushReceiver Then
                    'push unknown value just to prevent access to stack locals.
                    PushEvalStack(Nothing, ExprContext.None)
                End If

                right = VisitExpression(node.Right, rhsContext)

                If mayPushReceiver Then
                    PopEvalStack()
                End If

                ' if assigning to a local, now it is the time to record the Write
                If storedAssignmentLocal IsNot Nothing Then
                    ' this assert will fire if code relies on implicit CLR coercions 
                    ' - i.e assigns int value to a short local.
                    ' in that case we should force lhs to be a real local
                    Debug.Assert(node.Left.Type.IsSameTypeIgnoringAll(node.Right.Type),
                                 "cannot use stack when assignment involves implicit coercion of the value")

                    Debug.Assert(Not isIndirect, "indirect assignment is a read, not a write")

                    RecordVarWrite(storedAssignmentLocal.LocalSymbol)
                End If

                Return node.Update(left, Nothing, right, node.SuppressObjectClone, node.Type)
            End Function

            ''' <summary>
            ''' VB uses a special node to assign references.
            ''' BoundAssignment is used only to assign values.
            ''' therefore an indirect assignment may only happen if lhs is a reference
            ''' </summary>
            Private Shared Function IsIndirectAssignment(node As BoundAssignmentOperator) As Boolean
                Return IsByRefVariable(node.Left)
            End Function

            Private Shared Function IsByRefVariable(node As BoundExpression) As Boolean
                Select Case node.Kind
                    Case BoundKind.Parameter
                        Return DirectCast(node, BoundParameter).ParameterSymbol.IsByRef
                    Case BoundKind.Local
                        Return DirectCast(node, BoundLocal).LocalSymbol.IsByRef
                    Case BoundKind.Call
                        Return DirectCast(node, BoundCall).Method.ReturnsByRef
                    Case BoundKind.Sequence
                        Debug.Assert(Not IsByRefVariable(DirectCast(node, BoundSequence).ValueOpt))
                        Return False

                    Case BoundKind.PseudoVariable
                        Return True
                    Case BoundKind.ReferenceAssignment
                        Return True
                    Case BoundKind.ValueTypeMeReference
                        Return True

                    Case BoundKind.ModuleVersionId,
                        BoundKind.InstrumentationPayloadRoot
                        ' same as static fields
                        Return False

                    Case BoundKind.FieldAccess,
                         BoundKind.ArrayAccess
                        ' fields are never byref
                        Return False

                    Case Else
                        Throw ExceptionUtilities.UnexpectedValue(node.Kind)
                End Select
            End Function

            Private Shared Function IsVerifierRef(type As TypeSymbol) As Boolean
                Return Not type.TypeKind = TypeKind.TypeParameter AndAlso type.IsReferenceType
            End Function

            Private Shared Function IsVerifierVal(type As TypeSymbol) As Boolean
                Return Not type.TypeKind = TypeKind.TypeParameter AndAlso type.IsValueType
            End Function

            Public Overrides Function VisitCall(node As BoundCall) As BoundNode
                Dim receiver = node.ReceiverOpt

                ' matches or a bit stronger than EmitReceiverRef
                ' if there are any doubts that receiver is a ref type, 
                ' assume we will need an address (that will prevent scheduling of receiver).
                If Not node.Method.IsShared Then
                    Dim receiverType = receiver.Type
                    Dim context As ExprContext

                    If receiverType.IsReferenceType Then
                        If (receiverType.IsTypeParameter()) Then
                            ' type param receiver that we statically know Is a reference will be boxed
                            context = ExprContext.Box
                        Else
                            ' reference receivers will be used as values
                            context = ExprContext.Value
                        End If
                    Else
                        ' everything else will get an address taken
                        context = ExprContext.Address
                    End If

                    receiver = VisitExpression(receiver, context)
                Else
                    Me._counter += 1
                    Debug.Assert(receiver Is Nothing OrElse receiver.Kind = BoundKind.TypeExpression)
                End If

                Dim method As MethodSymbol = node.Method
                Dim rewrittenArguments As ImmutableArray(Of BoundExpression) = VisitArguments(node.Arguments, method.Parameters)

                Debug.Assert(node.MethodGroupOpt Is Nothing)
                Return node.Update(
                    method,
                    node.MethodGroupOpt,
                    receiver,
                    rewrittenArguments,
                    node.DefaultArguments,
                    node.ConstantValueOpt,
                    isLValue:=node.IsLValue,
                    suppressObjectClone:=node.SuppressObjectClone,
                    type:=node.Type)
            End Function

            Private Function VisitArguments(arguments As ImmutableArray(Of BoundExpression), parameters As ImmutableArray(Of ParameterSymbol)) As ImmutableArray(Of BoundExpression)
                Debug.Assert(Not arguments.IsDefault)
                Debug.Assert(Not parameters.IsDefault)

                ' If this is a varargs method then there will be one additional argument for the __arglist().
                Debug.Assert(arguments.Length = parameters.Length OrElse arguments.Length = parameters.Length + 1)

                Dim rewrittenArguments As ArrayBuilder(Of BoundExpression) = Nothing
                For i = 0 To arguments.Length - 1

                    ' Treat the __arglist() as a value parameter.
                    Dim context As ExprContext = If(i = parameters.Length OrElse Not parameters(i).IsByRef, ExprContext.Value, ExprContext.Address)

                    Dim arg As BoundExpression = arguments(i)
                    Dim rewrittenArg As BoundExpression = VisitExpression(arg, context)
                    If rewrittenArguments Is Nothing AndAlso arg IsNot rewrittenArg Then
                        rewrittenArguments = ArrayBuilder(Of BoundExpression).GetInstance()
                        rewrittenArguments.AddRange(arguments, i)
                    End If

                    If rewrittenArguments IsNot Nothing Then
                        rewrittenArguments.Add(rewrittenArg)
                    End If
                Next

                Return If(rewrittenArguments IsNot Nothing, rewrittenArguments.ToImmutableAndFree, arguments)
            End Function

            Public Overrides Function VisitObjectCreationExpression(node As BoundObjectCreationExpression) As BoundNode
                Dim constructor As MethodSymbol = node.ConstructorOpt

                Debug.Assert(constructor IsNot Nothing OrElse node.Arguments.Length = 0)
                Dim rewrittenArguments As ImmutableArray(Of BoundExpression) = If(constructor Is Nothing, node.Arguments,
                                                                                 VisitArguments(node.Arguments, constructor.Parameters))
                Debug.Assert(node.InitializerOpt Is Nothing)
                Me._counter += 1

                Return node.Update(constructor, rewrittenArguments, node.DefaultArguments, Nothing, node.Type)
            End Function

            Public Overrides Function VisitArrayAccess(node As BoundArrayAccess) As BoundNode
                ' regardless of purpose, array access visits its children as values

                ' TODO: do we need to save/restore old context here?
                Dim oldContext = Me._context
                Me._context = ExprContext.Value
                Dim result As BoundNode = MyBase.VisitArrayAccess(node)
                Me._context = oldContext

                Return result
            End Function

            Public Overrides Function VisitFieldAccess(node As BoundFieldAccess) As BoundNode
                Dim field As FieldSymbol = node.FieldSymbol
                Dim receiver As BoundExpression = node.ReceiverOpt

                ' if there are any doubts that receiver is a ref type, assume we will 
                ' need an address. (that will prevent scheduling of receiver).
                If Not field.IsShared Then
                    If receiver.Type.IsTypeParameter Then
                        ' type parameters must be boxed to access fields.
                        receiver = VisitExpression(receiver, ExprContext.Box)
                    Else
                        ' need address when assigning to a field and receiver is not a reference
                        '              when accessing a field of a struct unless we only need Value and Value is preferred.
                        If receiver.Type.IsValueType AndAlso
                            (_context = ExprContext.AssignmentTarget OrElse
                             _context = ExprContext.Address OrElse
                             CodeGenerator.FieldLoadMustUseRef(receiver)) Then

                            receiver = VisitExpression(receiver, ExprContext.Address)
                        Else
                            receiver = VisitExpression(receiver, ExprContext.Value)
                        End If
                    End If
                Else
                    Me._counter += 1
                    receiver = Nothing
                End If

                Return node.Update(receiver, field, node.IsLValue, node.SuppressVirtualCalls, node.ConstantsInProgressOpt, node.Type)
            End Function

            Public Overrides Function VisitLabelStatement(node As BoundLabelStatement) As BoundNode
                RecordLabel(node.Label)
                Return MyBase.VisitLabelStatement(node)
            End Function

            Public Overrides Function VisitGotoStatement(node As BoundGotoStatement) As BoundNode
                Dim result As BoundNode = MyBase.VisitGotoStatement(node)
                RecordBranch(node.Label)
                Return result
            End Function

            Public Overrides Function VisitConditionalGoto(node As BoundConditionalGoto) As BoundNode
                Dim result As BoundNode = MyBase.VisitConditionalGoto(node)
                Me.PopEvalStack() ' condition gets consumed
                RecordBranch(node.Label)
                Return result
            End Function

            Public Overrides Function VisitBinaryConditionalExpression(node As BoundBinaryConditionalExpression) As BoundNode
                Dim origStack = Me.StackDepth
                Dim testExpression As BoundExpression = DirectCast(Me.Visit(node.TestExpression), BoundExpression)

                Debug.Assert(node.ConvertedTestExpression Is Nothing)
                ' Me.counter += 1 '' This child is not visited 

                Debug.Assert(node.TestExpressionPlaceholder Is Nothing)
                ' Me.counter += 1 '' This child is not visited 

                ' implicit branch here
                Dim cookie As Object = GetStackStateCookie()

                Me.SetStackDepth(origStack)  ' else expression is evaluated with original stack
                Dim elseExpression As BoundExpression = DirectCast(Me.Visit(node.ElseExpression), BoundExpression)

                ' implicit label here
                EnsureStackState(cookie)

                Return node.Update(testExpression, Nothing, Nothing, elseExpression, node.ConstantValueOpt, node.Type)
            End Function

            Public Overrides Function VisitTernaryConditionalExpression(node As BoundTernaryConditionalExpression) As BoundNode
                Dim origStack As Integer = Me.StackDepth
                Dim condition = DirectCast(Me.Visit(node.Condition), BoundExpression)

                Dim cookie As Object = GetStackStateCookie() ' implicit goto here

                Me.SetStackDepth(origStack) ' consequence is evaluated with original stack
                Dim whenTrue = DirectCast(Me.Visit(node.WhenTrue), BoundExpression)

                EnsureStackState(cookie) ' implicit label here

                Me.SetStackDepth(origStack) ' alternative is evaluated with original stack
                Dim whenFalse = DirectCast(Me.Visit(node.WhenFalse), BoundExpression)

                EnsureStackState(cookie) ' implicit label here

                Return node.Update(condition, whenTrue, whenFalse, node.ConstantValueOpt, node.Type)
            End Function

            Public Overrides Function VisitLoweredConditionalAccess(node As BoundLoweredConditionalAccess) As BoundNode
                If Not node.ReceiverOrCondition.Type.IsBooleanType() Then
                    ' We may need to load a reference to the receiver, or may need to  
                    ' reload it after the null check. This won't work well 
                    ' with a stack local.
                    EnsureOnlyEvalStack()
                End If

                Dim origStack = StackDepth()

                Dim receiverOrCondition = DirectCast(Me.Visit(node.ReceiverOrCondition), BoundExpression)

                Dim cookie = GetStackStateCookie()     ' implicit branch here

                ' access Is evaluated with original stack 
                ' (this Is Not entirely true, codegen will keep receiver on the stack, but that Is irrelevant here)
                Me.SetStackDepth(origStack)

                Dim whenNotNull = DirectCast(Me.Visit(node.WhenNotNull), BoundExpression)

                EnsureStackState(cookie)   ' implicit label here

                Dim whenNull As BoundExpression = Nothing

                If node.WhenNullOpt IsNot Nothing Then
                    Me.SetStackDepth(origStack)
                    whenNull = DirectCast(Me.Visit(node.WhenNullOpt), BoundExpression)
                    EnsureStackState(cookie) ' implicit label here
                End If

                Return node.Update(receiverOrCondition, node.CaptureReceiver, node.PlaceholderId, whenNotNull, whenNull, node.Type)
            End Function

            Public Overrides Function VisitConditionalAccessReceiverPlaceholder(node As BoundConditionalAccessReceiverPlaceholder) As BoundNode
                Return MyBase.VisitConditionalAccessReceiverPlaceholder(node)
            End Function

            Public Overrides Function VisitComplexConditionalAccessReceiver(node As BoundComplexConditionalAccessReceiver) As BoundNode
                EnsureOnlyEvalStack()

                Dim origStack As Integer = Me.StackDepth

                Me.PushEvalStack(Nothing, ExprContext.None)

                Dim cookie As Object = GetStackStateCookie() ' implicit goto here

                Me.SetStackDepth(origStack) ' consequence is evaluated with original stack
                Dim valueTypeReceiver = DirectCast(Me.Visit(node.ValueTypeReceiver), BoundExpression)

                EnsureStackState(cookie) ' implicit label here

                Me.SetStackDepth(origStack) ' alternative is evaluated with original stack
                Dim referenceTypeReceiver = DirectCast(Me.Visit(node.ReferenceTypeReceiver), BoundExpression)

                EnsureStackState(cookie) ' implicit label here

                Return node.Update(valueTypeReceiver, referenceTypeReceiver, node.Type)
            End Function

            Public Overrides Function VisitBinaryOperator(node As BoundBinaryOperator) As BoundNode
                ' Do not blow the stack due to a deep recursion on the left. 

                Dim child As BoundExpression = node.Left

                If child.Kind <> BoundKind.BinaryOperator OrElse child.ConstantValueOpt IsNot Nothing Then
                    Return VisitBinaryOperatorSimple(node)
                End If

                Dim stack = ArrayBuilder(Of BoundBinaryOperator).GetInstance()
                stack.Push(node)

                Dim binary As BoundBinaryOperator = DirectCast(child, BoundBinaryOperator)

                Do
                    stack.Push(binary)
                    child = binary.Left

                    If child.Kind <> BoundKind.BinaryOperator OrElse child.ConstantValueOpt IsNot Nothing Then
                        Exit Do
                    End If

                    binary = DirectCast(child, BoundBinaryOperator)
                Loop


                Dim prevStack As Integer = Me.StackDepth()

                Dim left = DirectCast(Me.Visit(child), BoundExpression)

                Do
                    binary = stack.Pop()

                    ' Short-circuit operators need to emulate implicit branch/label
                    Dim isLogical As Boolean
                    Dim cookie As Object = Nothing

                    Select Case (binary.OperatorKind And BinaryOperatorKind.OpMask)
                        Case BinaryOperatorKind.AndAlso, BinaryOperatorKind.OrElse
                            isLogical = True
                            ' implicit branch here
                            cookie = GetStackStateCookie()

                            Me.SetStackDepth(prevStack)  ' right is evaluated with original stack

                        Case Else
                            isLogical = False
                    End Select

                    Dim right = DirectCast(Me.Visit(binary.Right), BoundExpression)

                    If isLogical Then
                        ' implicit label here
                        EnsureStackState(cookie)
                    End If

                    Dim type As TypeSymbol = Me.VisitType(binary.Type)
                    left = binary.Update(binary.OperatorKind, left, right, binary.Checked, binary.ConstantValueOpt, type)

                    If stack.Count = 0 Then
                        Exit Do
                    End If

                    _counter += 1
                    SetStackDepth(prevStack)
                    PushEvalStack(node, ExprContext.Value)
                Loop

                Debug.Assert(binary Is node)
                stack.Free()

                Return left
            End Function

            Private Function VisitBinaryOperatorSimple(node As BoundBinaryOperator) As BoundNode
                Select Case (node.OperatorKind And BinaryOperatorKind.OpMask)
                    Case BinaryOperatorKind.AndAlso, BinaryOperatorKind.OrElse
                        ' Short-circuit operators need to emulate implicit branch/label

                        Dim origStack = Me.StackDepth
                        Dim left As BoundExpression = DirectCast(Me.Visit(node.Left), BoundExpression)

                        ' implicit branch here
                        Dim cookie As Object = GetStackStateCookie()

                        Me.SetStackDepth(origStack)  ' right is evaluated with original stack
                        Dim right As BoundExpression = DirectCast(Me.Visit(node.Right), BoundExpression)

                        ' implicit label here
                        EnsureStackState(cookie)

                        Return node.Update(node.OperatorKind, left, right, node.Checked, node.ConstantValueOpt, node.Type)

                    Case Else
                        Return MyBase.VisitBinaryOperator(node)
                End Select
            End Function

            Public Overrides Function VisitUnaryOperator(node As BoundUnaryOperator) As BoundNode
                ' checked(-x) is emitted as "0 - x"
                If node.Checked AndAlso (node.OperatorKind And UnaryOperatorKind.OpMask) = UnaryOperatorKind.Minus Then
                    Dim storedStack = Me.StackDepth
                    Me.PushEvalStack(Nothing, ExprContext.None)
                    Dim operand = DirectCast(Me.Visit(node.Operand), BoundExpression)
                    Me.SetStackDepth(storedStack)
                    Return node.Update(node.OperatorKind, operand, node.Checked, node.ConstantValueOpt, node.Type)

                Else
                    Return MyBase.VisitUnaryOperator(node)
                End If
            End Function

            Public Overrides Function VisitSelectStatement(node As BoundSelectStatement) As BoundNode
                ' switch requires that key local stays local
                EnsureOnlyEvalStack()

                Dim expressionStatement As BoundExpressionStatement = DirectCast(Me.Visit(node.ExpressionStatement), BoundExpressionStatement)
                If expressionStatement.Expression.Kind = BoundKind.Local Then
                    ' It is required by code gen that if node.ExpressionStatement 
                    ' is a local it should not be optimized out 
                    Dim local = DirectCast(expressionStatement.Expression, BoundLocal).LocalSymbol
                    ShouldNotSchedule(local)
                End If

                Dim origStack = Me.StackDepth

                EnsureOnlyEvalStack()
                Dim exprPlaceholderOpt As BoundRValuePlaceholder = DirectCast(Me.Visit(node.ExprPlaceholderOpt), BoundRValuePlaceholder)
                ' expression value is consumed by the switch
                Me.SetStackDepth(origStack)

                EnsureOnlyEvalStack()

                ' case blocks
                Dim caseBlocks As ImmutableArray(Of BoundCaseBlock) = Me.VisitList(node.CaseBlocks)

                ' exit label
                Dim exitLabel = node.ExitLabel
                If exitLabel IsNot Nothing Then
                    Me.RecordLabel(exitLabel)
                End If

                EnsureOnlyEvalStack()
                Return node.Update(expressionStatement, exprPlaceholderOpt, caseBlocks, node.RecommendSwitchTable, exitLabel)
            End Function

            Public Overrides Function VisitCaseBlock(node As BoundCaseBlock) As BoundNode
                EnsureOnlyEvalStack()
                Dim caseStatement As BoundCaseStatement = DirectCast(Me.Visit(node.CaseStatement), BoundCaseStatement)

                EnsureOnlyEvalStack()
                Dim body As BoundBlock = DirectCast(Me.Visit(node.Body), BoundBlock)

                EnsureOnlyEvalStack()
                Return node.Update(caseStatement, body)
            End Function

            Public Overrides Function VisitUnstructuredExceptionOnErrorSwitch(node As BoundUnstructuredExceptionOnErrorSwitch) As BoundNode
                Dim value = DirectCast(Visit(node.Value), BoundExpression)

                Me.PopEvalStack() ' value gets consumed
                Return node.Update(value, Me.VisitList(node.Jumps))
            End Function

            Public Overrides Function VisitUnstructuredExceptionResumeSwitch(node As BoundUnstructuredExceptionResumeSwitch) As BoundNode
                Dim resumeLabel = DirectCast(Visit(node.ResumeLabel), BoundLabelStatement)
                Dim resumeNextLabel = DirectCast(Visit(node.ResumeNextLabel), BoundLabelStatement)
                Dim resumeTargetTemporary = DirectCast(Visit(node.ResumeTargetTemporary), BoundLocal)

                Me.PopEvalStack() ' value gets consumed

                Return node.Update(resumeTargetTemporary, resumeLabel, resumeNextLabel, node.Jumps)
            End Function

            Public Overrides Function VisitTryStatement(node As BoundTryStatement) As BoundNode
                EnsureOnlyEvalStack()
                Dim tryBlock = DirectCast(Me.Visit(node.TryBlock), BoundBlock)

                EnsureOnlyEvalStack()

                Dim catchBlocks As ImmutableArray(Of BoundCatchBlock) = Me.VisitList(node.CatchBlocks)

                EnsureOnlyEvalStack()
                Dim finallyBlock = DirectCast(Me.Visit(node.FinallyBlockOpt), BoundBlock)

                EnsureOnlyEvalStack()

                If node.ExitLabelOpt IsNot Nothing Then
                    RecordLabel(node.ExitLabelOpt)
                End If

                EnsureOnlyEvalStack()
                Return node.Update(tryBlock, catchBlocks, finallyBlock, node.ExitLabelOpt)
            End Function

            Public Overrides Function VisitCatchBlock(node As BoundCatchBlock) As BoundNode
                Dim origStack As Integer = Me.StackDepth

                DeclareLocal(node.LocalOpt, origStack)

                EnsureOnlyEvalStack()
                Dim exceptionVariableOpt As BoundExpression = Me.VisitExpression(node.ExceptionSourceOpt, ExprContext.Value)
                Me.SetStackDepth(origStack)

                EnsureOnlyEvalStack()
                Dim errorLineNumberOpt As BoundExpression = Me.VisitExpression(node.ErrorLineNumberOpt, ExprContext.Value)
                Me.SetStackDepth(origStack)

                EnsureOnlyEvalStack()
                Dim exceptionFilterOpt As BoundExpression = Me.VisitExpression(node.ExceptionFilterOpt, ExprContext.Value)
                Me.SetStackDepth(origStack)

                EnsureOnlyEvalStack()
                Dim body As BoundBlock = DirectCast(Me.Visit(node.Body), BoundBlock)

                EnsureOnlyEvalStack()
                Return node.Update(node.LocalOpt, exceptionVariableOpt, errorLineNumberOpt, exceptionFilterOpt, body, node.IsSynthesizedAsyncCatchAll)
            End Function

            Public Overrides Function VisitArrayInitialization(node As BoundArrayInitialization) As BoundNode
                ' nontrivial construct - may use dups, metadata blob helpers etc..
                EnsureOnlyEvalStack()

                Dim initializers As ImmutableArray(Of BoundExpression) = node.Initializers
                Dim rewrittenInitializers As ArrayBuilder(Of BoundExpression) = Nothing
                If Not initializers.IsDefault Then
                    For i = 0 To initializers.Length - 1
                        ' array itself will be pushed on the stack here.
                        EnsureOnlyEvalStack()

                        Dim initializer As BoundExpression = initializers(i)
                        Dim rewrittenInitializer As BoundExpression = Me.VisitExpression(initializer, ExprContext.Value)

                        If rewrittenInitializers Is Nothing AndAlso rewrittenInitializer IsNot initializer Then
                            rewrittenInitializers = ArrayBuilder(Of BoundExpression).GetInstance()
                            rewrittenInitializers.AddRange(initializers, i)
                        End If

                        If rewrittenInitializers IsNot Nothing Then
                            rewrittenInitializers.Add(rewrittenInitializer)
                        End If
                    Next
                End If

                Return node.Update(If(rewrittenInitializers IsNot Nothing, rewrittenInitializers.ToImmutableAndFree(), initializers), node.Type)
            End Function

            Public Overrides Function VisitReturnStatement(node As BoundReturnStatement) As BoundNode
                Dim expressionOpt = TryCast(Me.Visit(node.ExpressionOpt), BoundExpression)

                ' must not have locals on stack when returning
                EnsureOnlyEvalStack()

                Return node.Update(expressionOpt, node.FunctionLocalOpt, node.ExitLabelOpt)
            End Function

            ''' <summary>
            ''' Ensures that there are no stack locals. It is done by accessing 
            ''' virtual "empty" local that is at the bottom of all stack locals.
            ''' </summary>
            ''' <remarks></remarks>
            Private Sub EnsureOnlyEvalStack()
                RecordVarRead(_empty)
            End Sub

            Private Function GetStackStateCookie() As Object
                ' create a dummy and start tracing it
                Dim dummy As New DummyLocal(Me._container)
                Me._dummyVariables.Add(dummy, dummy)
                Me._locals.Add(dummy, New LocalDefUseInfo(Me.StackDepth))
                RecordDummyWrite(dummy)

                Return dummy
            End Function

            Private Sub EnsureStackState(cookie As Object)
                RecordVarRead(Me._dummyVariables(cookie))
            End Sub

            ' called on branches and labels
            Private Sub RecordBranch(label As LabelSymbol)
                Dim dummy As DummyLocal = Nothing
                If Me._dummyVariables.TryGetValue(label, dummy) Then
                    RecordVarRead(dummy)
                Else
                    ' create a dummy and start tracing it
                    dummy = New DummyLocal(Me._container)
                    Me._dummyVariables.Add(label, dummy)
                    Me._locals.Add(dummy, New LocalDefUseInfo(Me.StackDepth))
                    RecordDummyWrite(dummy)
                End If
            End Sub

            Private Sub RecordLabel(label As LabelSymbol)
                Dim dummy As DummyLocal = Nothing
                If Me._dummyVariables.TryGetValue(label, dummy) Then
                    RecordVarRead(dummy)

                Else
                    ' this is a backwards jump with nontrivial stack requirements
                    ' just use empty.
                    dummy = _empty
                    Me._dummyVariables.Add(label, dummy)
                    RecordVarRead(dummy)
                End If
            End Sub

            Private Sub RecordVarRef(local As LocalSymbol)
                Debug.Assert(Not local.IsByRef, "can't tke a ref of a ref")

                If Not CanScheduleToStack(local) Then
                    Return
                End If

                ' if we ever take a reference of a local, it must be a real local.
                ShouldNotSchedule(local)
            End Sub

            Private Sub RecordVarRead(local As LocalSymbol)
                If Not CanScheduleToStack(local) Then
                    Return
                End If

                Dim locInfo As LocalDefUseInfo = _locals(local)

                If locInfo.CannotSchedule Then
                    Return
                End If

                If locInfo.localDefs.Count = 0 Then
                    ' reading before writing.
                    locInfo.ShouldNotSchedule()
                    Return
                End If

                ' if accessing real val, check stack
                If Not TypeOf local Is DummyLocal Then
                    If locInfo.StackAtDeclaration <> StackDepth() AndAlso
                        Not EvalStackHasLocal(local) Then
                        ' reading at different eval stack.
                        locInfo.ShouldNotSchedule()
                        Return
                    End If
                Else
                    ' dummy must be accessed on same stack.
                    Debug.Assert(local Is _empty OrElse locInfo.StackAtDeclaration = StackDepth())
                End If

                Dim definedAt As LocalDefUseSpan = locInfo.localDefs.Last()
                definedAt.SetEnd(Me._counter)

                Dim locDef As New LocalDefUseSpan(_counter)
                locInfo.localDefs.Add(locDef)
            End Sub

            Private Function EvalStackHasLocal(local As LocalSymbol) As Boolean
                Dim top = _evalStack.Last()

                Return top.context = If(Not local.IsByRef, ExprContext.Value, ExprContext.Address) AndAlso
                   top.expression.Kind = BoundKind.Local AndAlso
                   DirectCast(top.expression, BoundLocal).LocalSymbol = local
            End Function

            Private Sub RecordVarWrite(local As LocalSymbol)
                Debug.Assert(local.SynthesizedKind <> SynthesizedLocalKind.OptimizerTemp)

                If Not CanScheduleToStack(local) Then
                    Return
                End If

                Dim locInfo = _locals(local)
                If locInfo.CannotSchedule Then
                    Return
                End If

                ' check stack
                ' -1 because real assignment "consumes" value.
                Dim evalStack = Me.StackDepth - 1

                If locInfo.StackAtDeclaration <> evalStack Then
                    ' writing at different eval stack.
                    locInfo.ShouldNotSchedule()
                    Return
                End If

                Dim locDef = New LocalDefUseSpan(Me._counter)
                locInfo.localDefs.Add(locDef)
            End Sub

            Private Sub RecordDummyWrite(local As LocalSymbol)
                Debug.Assert(local.SynthesizedKind = SynthesizedLocalKind.OptimizerTemp)

                Dim locInfo = _locals(local)

                ' dummy must be accessed on same stack.
                Debug.Assert(local Is _empty OrElse locInfo.StackAtDeclaration = StackDepth())

                Dim locDef = New LocalDefUseSpan(Me._counter)
                locInfo.localDefs.Add(locDef)
            End Sub

            Private Sub ShouldNotSchedule(local As LocalSymbol)
                Dim localDefInfo As LocalDefUseInfo = Nothing
                If _locals.TryGetValue(local, localDefInfo) Then
                    localDefInfo.ShouldNotSchedule()
                End If
            End Sub

            Private Function CanScheduleToStack(local As LocalSymbol) As Boolean
                Return local.CanScheduleToStack AndAlso
                        (Not _debugFriendly OrElse Not local.SynthesizedKind.IsLongLived())
            End Function

            Private Sub DeclareLocals(locals As ImmutableArray(Of LocalSymbol), stack As Integer)
                For Each local In locals
                    DeclareLocal(local, stack)
                Next
            End Sub

            Private Sub DeclareLocal(local As LocalSymbol, stack As Integer)
                If local IsNot Nothing Then
                    If CanScheduleToStack(local) Then
                        Me._locals.Add(local, New LocalDefUseInfo(stack))
                    End If
                End If
            End Sub

        End Class

    End Class
End Namespace
