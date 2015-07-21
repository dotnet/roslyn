' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable
Imports System.Runtime.InteropServices
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports TypeKind = Microsoft.CodeAnalysis.TypeKind

Namespace Microsoft.CodeAnalysis.VisualBasic.CodeGen
    Partial Friend Class StackScheduler

        ''' <summary>
        ''' Analyses the tree trying to figure which locals may live on stack. It is 
        ''' a fairly delicate process and must be very familiar with how CodeGen works. 
        ''' It is essentially a part of CodeGen.
        ''' 
        ''' NOTE: It is always safe to mark a local as not eligible as a stack local 
        ''' so when situation gets complicated we just refuse to schedule and move on.
        ''' </summary>
        Private NotInheritable Class Analyzer
            Inherits BoundTreeRewriter

            ''' <summary>
            ''' context of expression evaluation. 
            ''' it will affect inference of stack behavior
            ''' it will also affect when expressions can be dup-reused
            '''     Example:
            '''         Foo(x, ref x)     x cannot be duped as it is used in different context  
            ''' </summary>
            Private Enum ExprContext
                None
                Sideeffects
                Value
                Address
                AssignmentTarget
                Box
            End Enum

            Private ReadOnly _container As Symbol

            Private _counter As Integer = 0
            Private _evalStack As Integer = 0
            Private _context As ExprContext = ExprContext.None
            Private _assignmentLocal As BoundLocal = Nothing

            Private _lastExpression As BoundExpression = Nothing
            Private _lastExprContext As ExprContext = ExprContext.None
            Private _lastExpressionCnt As Integer = 0

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

            Private Sub New(container As Symbol)
                Me._container = container
                Me._empty = New DummyLocal(container)

                ' this is the top of eval stack
                DeclareLocal(_empty, 0)
                RecordVarWrite(_empty)
            End Sub

            Public Shared Function Analyze(container As Symbol, node As BoundNode, <Out> ByRef locals As Dictionary(Of LocalSymbol, LocalDefUseInfo)) As BoundNode
                Dim analyzer = New Analyzer(container)

                Dim rewritten As BoundNode = analyzer.Visit(node)
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

            Private Shared Function CanDup(prevNode As BoundExpression, curNode As BoundExpression) As Boolean
                If prevNode Is Nothing Then
                    Return False
                End If

                If prevNode.Type <> curNode.Type Then
                    Return False
                End If

                ' TODO: constants
                '       Codegen sometimes folds constants (conditional ops, anything else?) 
                '       We cannot currently rely on constants being actually emitted so we cannot dup them.
                '       It could be attractive to do that though for big constants like doubles.
                Dim prevConst As ConstantValue = prevNode.ConstantValueOpt
                If prevConst IsNot Nothing Then
                    Return False
                End If

                ' locals
                Dim prevAsLocal = TryCast(prevNode, BoundLocal)
                Dim curAsLocal = TryCast(curNode, BoundLocal)
                If prevAsLocal IsNot Nothing AndAlso curAsLocal IsNot Nothing Then
                    Return prevAsLocal.LocalSymbol Is curAsLocal.LocalSymbol
                End If

                ' parameters
                Dim prevAsParam = TryCast(prevNode, BoundParameter)
                Dim curAsParam = TryCast(curNode, BoundParameter)
                If prevAsParam IsNot Nothing AndAlso curAsParam IsNot Nothing Then
                    ' TODO: it may be unnecessary to dup when it is ldloc[0-4]
                    '       just dup always for now for simplicity.
                    Return prevAsParam.ParameterSymbol Is curAsParam.ParameterSymbol
                End If

                ' TODO: fields, constants, sequence points ...

                Return False
            End Function

            ''' <summary>
            ''' Recursively rewrites the node or simply replaces it with a dup node
            ''' if we have just seen exactly same node.
            ''' </summary>
            Private Function ReuseOrVisit(node As BoundExpression, context As ExprContext) As BoundExpression
                ' TODO: can we dup expressions in assignment contexts? 
                If context = ExprContext.AssignmentTarget OrElse context = ExprContext.Sideeffects Then
                    Return DirectCast(MyBase.Visit(node), BoundExpression)
                End If

                ' it must be most recent expression
                ' it can be a ref when we want a value, but cannot be the other way
                ' TODO: we could reuse boxed values, but we would need to make the Dup to know it was boxed
                If Me._counter = Me._lastExpressionCnt + 1 AndAlso
                        Me._lastExprContext <> ExprContext.Box AndAlso
                        (Me._lastExprContext = context OrElse Me._lastExprContext <> ExprContext.Value) AndAlso CanDup(Me._lastExpression, node) Then

                    Me._lastExpressionCnt = Me._counter

                    ' when duping something not created in a Value context, we are actually duping a reference.
                    ' record that so that codegen could know if it is a value or a reference.
                    Dim dupRef As Boolean = Me._lastExprContext <> ExprContext.Value

                    ' change the context to the most recently used. 
                    ' Why? If we obtained a value from a duped reference, we now have a value on the stack.
                    Me._lastExprContext = context

                    Return New BoundDup(node.Syntax, dupRef, node.Type)
                Else

                    Dim result As BoundExpression = BaseVisitExpression(node)

                    Me._lastExpressionCnt = Me._counter
                    Me._lastExprContext = context
                    Me._lastExpression = result

                    Return result
                End If
            End Function

            Private Function BaseVisitExpression(node As BoundExpression) As BoundExpression
                ' Do Not recurse into constant expressions. Their children do Not push any locals.
                ' TODO: we may consider duping the constants though (see comments in CanDup)
                If node.ConstantValueOpt Is Nothing Then
                    node = DirectCast(MyBase.Visit(node), BoundExpression)
                End If

                Return node
            End Function

            Private Function VisitExpression(node As BoundExpression, context As ExprContext) As BoundExpression
                If node Is Nothing Then
                    Me._counter += 1
                    Return node
                End If

                Dim prevContext As ExprContext = Me._context
                Dim prevStack As Integer = Me._evalStack

                Me._context = context
                Dim result As BoundExpression = ReuseOrVisit(node, context)
                Me._counter += 1

                Select Case context

                    Case ExprContext.Sideeffects
                        Me._evalStack = prevStack

                    Case ExprContext.Value,
                        ExprContext.Address,
                        ExprContext.Box
                        Me._evalStack = prevStack + 1

                    Case ExprContext.AssignmentTarget
                        Me._evalStack = prevStack
                        If LhsUsesStackWhenAssignedTo(node, context) Then
                            Me._evalStack = prevStack + 1
                        End If

                    Case Else
                        Throw ExceptionUtilities.UnexpectedValue(context)
                End Select

                Me._context = prevContext
                Return result
            End Function

            Public Overrides Function VisitSpillSequence(node As BoundSpillSequence) As BoundNode
                Throw ExceptionUtilities.Unreachable
            End Function

            Private Function VisitStatement(node As BoundNode) As BoundNode

                Dim prevContext As ExprContext = Me._context
                Dim prevStack As Integer = Me._evalStack

                Dim result As BoundNode = MyBase.Visit(node)

                ClearLastExpression()

                Me._counter += 1
                Me._evalStack = prevStack
                Me._context = prevContext

                Return result
            End Function

            Private Sub ClearLastExpression()
                Me._lastExpressionCnt = 0
                Me._lastExpression = Nothing
                Me._lastExprContext = ExprContext.None
            End Sub

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
                Debug.Assert(Me._evalStack = 0, "entering blocks when evaluation stack is not empty?")

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
                Dim declarationStack As Integer = Me._evalStack

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

                            ' whatever is available as lastExpression is still available 
                            ' (adjust for visit of this node)
                            Me._lastExpressionCnt += 1

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
                Debug.Assert(node.ByRefLocal.Type.IsSameTypeIgnoringCustomModifiers(node.LValue.Type),
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

                ' Left on the right should be Nothing by this time
                Debug.Assert(node.LeftOnTheRightOpt Is Nothing)

                ' Do not visit "Left on the right"
                'Me.counter += 1

                Dim right As BoundExpression = node.Right

                ' if right is a struct ctor, it may be optimized into in-place call
                ' Such call will push the receiver ref before the arguments
                ' so we need to ensure that arguments cannot use stack temps
                Dim leftType As TypeSymbol = left.Type
                Dim cookie As Object = Nothing
                If right.Kind = BoundKind.ObjectCreationExpression Then
                    Dim ctor = DirectCast(right, BoundObjectCreationExpression).ConstructorOpt
                    If ctor IsNot Nothing AndAlso ctor.ParameterCount <> 0 Then
                        cookie = GetStackStateCookie()
                    End If
                End If

                Debug.Assert(Me._context <> ExprContext.AssignmentTarget, "assignment expression cannot be a target of another assignment")

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

                right = VisitExpression(node.Right, rhsContext)

                If cookie IsNot Nothing Then
                    ' There is still RHS on the stack, adjust for that
                    Me._evalStack -= 1
                    EnsureStackState(cookie)
                    Me._evalStack += 1
                End If

                ' if assigning to a local, now it is the time to record the Write
                If storedAssignmentLocal IsNot Nothing Then
                    ' this assert will fire if code relies on implicit CLR coercions 
                    ' - i.e assigns int value to a short local.
                    ' in that case we should force lhs to be a real local
                    Debug.Assert(node.Left.Type.IsSameTypeIgnoringCustomModifiers(node.Right.Type),
                                 "cannot use stack when assignment involves implicit coercion of the value")

                    Debug.Assert(Not isIndirect, "indirect assignment is a read, not a write")

                    RecordVarWrite(storedAssignmentLocal.LocalSymbol)
                End If

                Return node.Update(left, Nothing, right, node.SuppressObjectClone, node.Type)
            End Function

            ''' <summary>
            ''' indirect assignment is assignment to a value referenced indirectly
            ''' it may only happen if lhs is a reference (must be a parameter or a local)
            '''       1) lhs is a reference (must be a parameter or a local)
            '''       2) it is not a ref/out assignment where the reference itself would be assigned
            ''' </summary>
            Friend Shared Function IsIndirectAssignment(node As BoundAssignmentOperator) As Boolean
                If Not IsByRefLocalOrParameter(node.Left) Then
                    Return False
                End If
                Return Not IsByRefLocalOrParameter(node.Right)
            End Function

            Private Shared Function IsByRefLocalOrParameter(node As BoundExpression) As Boolean
                Select Case node.Kind
                    Case BoundKind.Parameter
                        Return DirectCast(node, BoundParameter).ParameterSymbol.IsByRef
                    Case BoundKind.Local
                        Return DirectCast(node, BoundLocal).LocalSymbol.IsByRef
                    Case Else
                        Return False
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
                Return node.Update(method, node.MethodGroupOpt, receiver, rewrittenArguments, node.ConstantValueOpt, node.SuppressObjectClone, node.Type)
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

                Return node.Update(constructor, rewrittenArguments, Nothing, node.Type)
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

                    ' whatever is available as lastExpression is still available 
                    ' (adjust for visit of this node)
                    Me._lastExpressionCnt += 1
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
                Me._evalStack -= 1 ' condition gets consumed
                RecordBranch(node.Label)
                Return result
            End Function

            Public Overrides Function VisitBinaryConditionalExpression(node As BoundBinaryConditionalExpression) As BoundNode
                Dim origStack = Me._evalStack
                Dim testExpression As BoundExpression = DirectCast(Me.Visit(node.TestExpression), BoundExpression)

                Debug.Assert(node.ConvertedTestExpression Is Nothing)
                ' Me.counter += 1 '' This child is not visited 

                Debug.Assert(node.TestExpressionPlaceholder Is Nothing)
                ' Me.counter += 1 '' This child is not visited 

                ' implicit branch here
                Dim cookie As Object = GetStackStateCookie()

                Me._evalStack = origStack  ' else expression is evaluated with original stack
                Dim elseExpression As BoundExpression = DirectCast(Me.Visit(node.ElseExpression), BoundExpression)

                ' implicit label here
                EnsureStackState(cookie)

                Return node.Update(testExpression, Nothing, Nothing, elseExpression, node.ConstantValueOpt, node.Type)
            End Function

            Public Overrides Function VisitTernaryConditionalExpression(node As BoundTernaryConditionalExpression) As BoundNode
                Dim origStack As Integer = Me._evalStack
                Dim condition = DirectCast(Me.Visit(node.Condition), BoundExpression)

                Dim cookie As Object = GetStackStateCookie() ' implicit goto here

                Me._evalStack = origStack ' consequence is evaluated with original stack
                Dim whenTrue = DirectCast(Me.Visit(node.WhenTrue), BoundExpression)

                EnsureStackState(cookie) ' implicit label here

                Me._evalStack = origStack ' alternative is evaluated with original stack
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

                Dim origStack = Me._evalStack

                Dim receiverOrCondition = DirectCast(Me.Visit(node.ReceiverOrCondition), BoundExpression)

                Dim cookie = GetStackStateCookie()     ' implicit branch here

                ' access Is evaluated with original stack 
                ' (this Is Not entirely true, codegen will keep receiver on the stack, but that Is irrelevant here)
                Me._evalStack = origStack

                Dim whenNotNull = DirectCast(Me.Visit(node.WhenNotNull), BoundExpression)

                EnsureStackState(cookie)   ' implicit label here

                Dim whenNull As BoundExpression = Nothing

                If node.WhenNullOpt IsNot Nothing Then
                    Me._evalStack = origStack
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

                Dim origStack As Integer = Me._evalStack

                Me._evalStack += 1

                Dim cookie As Object = GetStackStateCookie() ' implicit goto here

                Me._evalStack = origStack ' consequence is evaluated with original stack
                Dim valueTypeReceiver = DirectCast(Me.Visit(node.ValueTypeReceiver), BoundExpression)

                EnsureStackState(cookie) ' implicit label here

                Me._evalStack = origStack ' alternative is evaluated with original stack
                Dim referenceTypeReceiver = DirectCast(Me.Visit(node.ReferenceTypeReceiver), BoundExpression)

                EnsureStackState(cookie) ' implicit label here

                Return node.Update(valueTypeReceiver, referenceTypeReceiver, node.Type)
            End Function

            Public Overrides Function VisitBinaryOperator(node As BoundBinaryOperator) As BoundNode
                Select Case (node.OperatorKind And BinaryOperatorKind.OpMask)
                    Case BinaryOperatorKind.AndAlso, BinaryOperatorKind.OrElse
                        ' Short-circuit operators need to emulate implicit branch/label

                        Dim origStack = Me._evalStack
                        Dim left As BoundExpression = DirectCast(Me.Visit(node.Left), BoundExpression)

                        ' implicit branch here
                        Dim cookie As Object = GetStackStateCookie()

                        Me._evalStack = origStack  ' right is evaluated with original stack
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
                    Dim storedStack = Me._evalStack
                    Me._evalStack += 1
                    CannotReusePreviousExpression() ' Loaded 0 on the stack.
                    Dim operand = DirectCast(Me.Visit(node.Operand), BoundExpression)
                    Me._evalStack = storedStack
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
                    _locals(DirectCast(expressionStatement.Expression, BoundLocal).LocalSymbol).ShouldNotSchedule()
                End If

                Dim origStack = Me._evalStack

                EnsureOnlyEvalStack()
                Dim exprPlaceholderOpt As BoundRValuePlaceholder = DirectCast(Me.Visit(node.ExprPlaceholderOpt), BoundRValuePlaceholder)
                ' expression value is consumed by the switch
                Me._evalStack = origStack

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

                Me._evalStack -= 1 ' value gets consumed
                Return node.Update(value, Me.VisitList(node.Jumps))
            End Function

            Public Overrides Function VisitUnstructuredExceptionResumeSwitch(node As BoundUnstructuredExceptionResumeSwitch) As BoundNode
                Dim resumeLabel = DirectCast(Visit(node.ResumeLabel), BoundLabelStatement)
                Dim resumeNextLabel = DirectCast(Visit(node.ResumeNextLabel), BoundLabelStatement)
                Dim resumeTargetTemporary = DirectCast(Visit(node.ResumeTargetTemporary), BoundLocal)

                Me._evalStack -= 1 ' value gets consumed

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
                Dim origStack As Integer = Me._evalStack

                DeclareLocal(node.LocalOpt, origStack)

                EnsureOnlyEvalStack()
                Dim exceptionVariableOpt As BoundExpression = Me.VisitExpression(node.ExceptionSourceOpt, ExprContext.Value)
                Me._evalStack = origStack

                EnsureOnlyEvalStack()
                Dim errorLineNumberOpt As BoundExpression = Me.VisitExpression(node.ErrorLineNumberOpt, ExprContext.Value)
                Me._evalStack = origStack

                EnsureOnlyEvalStack()
                Dim exceptionFilterOpt As BoundExpression = Me.VisitExpression(node.ExceptionFilterOpt, ExprContext.Value)
                Me._evalStack = origStack

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
                CannotReusePreviousExpression()
                RecordVarRead(_empty)
            End Sub

            Private Function GetStackStateCookie() As Object
                CannotReusePreviousExpression()

                ' create a dummy and start tracing it
                Dim dummy As New DummyLocal(Me._container)
                Me._dummyVariables.Add(dummy, dummy)
                Me._locals.Add(dummy, New LocalDefUseInfo(Me._evalStack))
                RecordVarWrite(dummy)

                Return dummy
            End Function

            Private Sub EnsureStackState(cookie As Object)
                CannotReusePreviousExpression()
                RecordVarRead(Me._dummyVariables(cookie))
            End Sub

            ' called on branches and labels
            Private Sub RecordBranch(label As LabelSymbol)

                CannotReusePreviousExpression()

                Dim dummy As DummyLocal = Nothing
                If Me._dummyVariables.TryGetValue(label, dummy) Then
                    RecordVarRead(dummy)
                Else
                    ' create a dummy and start tracing it
                    dummy = New DummyLocal(Me._container)
                    Me._dummyVariables.Add(label, dummy)
                    Me._locals.Add(dummy, New LocalDefUseInfo(Me._evalStack))
                    RecordVarWrite(dummy)
                End If
            End Sub

            Private Sub RecordLabel(label As LabelSymbol)
                CannotReusePreviousExpression()

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

            Private Sub CannotReusePreviousExpression()
                ClearLastExpression()
            End Sub

            Private Sub RecordVarRef(local As LocalSymbol)
                Debug.Assert(Not local.IsByRef, "can't tke a ref of a ref")

                If Not CanScheduleToStack(local) Then
                    Return
                End If

                ' if we ever take a reference of a local, it must be a real local.
                _locals(local).ShouldNotSchedule()
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
                    If locInfo.StackAtDeclaration <> _evalStack Then
                        ' reading at different eval stack.
                        locInfo.ShouldNotSchedule()
                        Return
                    End If
                Else
                    ' dummy must be accessed on same stack.
                    Debug.Assert(local Is _empty OrElse locInfo.StackAtDeclaration = _evalStack)
                End If

                Dim definedAt As LocalDefUseSpan = locInfo.localDefs.Last()
                definedAt.SetEnd(Me._counter)

                Dim locDef As New LocalDefUseSpan(_counter)
                locInfo.localDefs.Add(locDef)
            End Sub

            Private Sub RecordVarWrite(local As LocalSymbol)
                If Not CanScheduleToStack(local) Then
                    Return
                End If

                Dim locInfo = _locals(local)
                If locInfo.CannotSchedule Then
                    Return
                End If

                ' if accessing real val, check stack
                If Not TypeOf local Is DummyLocal Then

                    ' -1 because real assignment "consumes" value.
                    Dim evalStack = Me._evalStack - 1

                    If locInfo.StackAtDeclaration <> evalStack Then
                        ' writing at different eval stack.
                        locInfo.ShouldNotSchedule()
                        Return
                    End If
                Else
                    ' dummy must be accessed on same stack.
                    Debug.Assert(local Is _empty OrElse locInfo.StackAtDeclaration = _evalStack)
                End If

                Dim locDef = New LocalDefUseSpan(Me._counter)
                locInfo.localDefs.Add(locDef)
            End Sub

            Private Shared Function CanScheduleToStack(local As LocalSymbol) As Boolean
                Return local.CanScheduleToStack
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

