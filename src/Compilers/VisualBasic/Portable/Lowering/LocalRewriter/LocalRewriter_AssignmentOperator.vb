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
        Public Overrides Function VisitAssignmentOperator(node As BoundAssignmentOperator) As BoundNode
            Dim nodeLeft = node.Left

            If nodeLeft.IsLateBound Then
                Return RewriteLateBoundAssignment(node)
            End If

            If node.Right.Kind = BoundKind.MidResult Then
                ' This is a case of Mid assignment where the target was a String, no additional conversion to/from string is needed.
#If DEBUG Then
                Dim midResult = DirectCast(node.Right, BoundMidResult)
                Debug.Assert(midResult.Original Is node.LeftOnTheRightOpt OrElse
                             (midResult.Original.Kind = BoundKind.Parenthesized AndAlso DirectCast(midResult.Original, BoundParenthesized).Expression Is node.LeftOnTheRightOpt))
#End If

                If nodeLeft.IsLValue Then
                    ' Trivial case - a simple call
                    Return RewriteTrivialMidAssignment(node)
                End If
            End If

            Dim setNode = If(IsPropertyAssignment(node), nodeLeft, Nothing)

#If DEBUG Then
            If setNode IsNot Nothing Then
                Dim accessKind = setNode.GetAccessKind()
                Debug.Assert((accessKind And PropertyAccessKind.Set) <> 0)
                Debug.Assert(((accessKind And PropertyAccessKind.Get) = 0) = (node.LeftOnTheRightOpt Is Nothing))
            End If
#End If

            If setNode Is Nothing AndAlso node.LeftOnTheRightOpt Is Nothing Then
                Return Me.VisitAssignmentOperatorSimple(node)
            End If

            Debug.Assert(nodeLeft.Kind <> BoundKind.FieldAccess OrElse
                         Not nodeLeft.IsConstant OrElse
                         Not DirectCast(nodeLeft, BoundFieldAccess).FieldSymbol.IsConstButNotMetadataConstant)

            Dim temps = ImmutableArray(Of SynthesizedLocal).Empty
            Dim assignmentTarget As BoundExpression

            If node.LeftOnTheRightOpt IsNot Nothing Then
                ' Make sure side effects are evaluated only once.
                If setNode IsNot Nothing Then
                    assignmentTarget = setNode.SetAccessKind(PropertyAccessKind.Unknown)
                Else
                    assignmentTarget = nodeLeft
                End If

                Dim temporaries = ArrayBuilder(Of SynthesizedLocal).GetInstance()
                Dim useTwice As UseTwiceRewriter.Result = UseTwiceRewriter.UseTwice(Me._currentMethodOrLambda, assignmentTarget, isForRegularCompoundAssignment:=True, temporaries)
                temps = temporaries.ToImmutableAndFree()

                Dim leftOnTheRight As BoundExpression

                If setNode IsNot Nothing Then
                    setNode = useTwice.First.SetAccessKind(PropertyAccessKind.Set)
                    assignmentTarget = setNode
                    leftOnTheRight = useTwice.Second.SetAccessKind(PropertyAccessKind.Get)
                Else
                    assignmentTarget = useTwice.First
                    Debug.Assert(assignmentTarget.IsLValue)
                    leftOnTheRight = useTwice.Second.MakeRValue()
                End If

                AddPlaceholderReplacement(node.LeftOnTheRightOpt, VisitExpressionNode(leftOnTheRight))
            Else
                assignmentTarget = nodeLeft
            End If

            Dim result As BoundExpression

            If setNode IsNot Nothing Then
                ' Rewrite property assignment into call to setter.
                Debug.Assert(assignmentTarget Is setNode)
                result = RewritePropertyAssignmentAsSetCall(node, setNode)
            Else
                result = node.Update(VisitExpressionNode(assignmentTarget),
                                     Nothing,
                                     VisitAndGenerateObjectCloneIfNeeded(node.Right, node.SuppressObjectClone),
                                     True,
                                     node.Type)
            End If

            If temps.Length > 0 Then
                If result.Type.IsVoidType() Then
                    result = New BoundSequence(node.Syntax,
                                               StaticCast(Of LocalSymbol).From(temps),
                                               ImmutableArray.Create(Of BoundExpression)(result),
                                               Nothing,
                                               result.Type)
                Else
                    result = New BoundSequence(node.Syntax,
                                               StaticCast(Of LocalSymbol).From(temps),
                                               ImmutableArray(Of BoundExpression).Empty,
                                               result,
                                               result.Type)
                End If
            End If

            If node.LeftOnTheRightOpt IsNot Nothing Then
                RemovePlaceholderReplacement(node.LeftOnTheRightOpt)
            End If

            Return result
        End Function

        Private Shared Function IsPropertyAssignment(node As BoundAssignmentOperator) As Boolean
            Select Case node.Left.Kind
                Case BoundKind.PropertyAccess
                    Dim propertyAccess = DirectCast(node.Left, BoundPropertyAccess)
                    Return Not propertyAccess.PropertySymbol.ReturnsByRef
                Case BoundKind.XmlMemberAccess
                    Return True
                Case Else
                    Return False
            End Select
        End Function

        ''' <summary>
        ''' Make sure GetObjectValue calls are injected.
        ''' </summary>
        Private Function VisitAssignmentOperatorSimple(node As BoundAssignmentOperator) As BoundExpression
            Debug.Assert(node.LeftOnTheRightOpt Is Nothing)

            Return node.Update(VisitAssignmentLeftExpression(node), Nothing, VisitAndGenerateObjectCloneIfNeeded(node.Right, node.SuppressObjectClone), True, node.Type)
        End Function

        Private Function VisitAssignmentLeftExpression(node As BoundAssignmentOperator) As BoundExpression
            Dim leftNode = node.Left

            ' If the lhs of this assignment operator is a field access, it should not be 
            ' rewritten even if it's const. If you do that, it will create ObjectCreationExpressions 
            ' for Dates and Decimals which are not allowed there.
            If leftNode.Kind = BoundKind.FieldAccess Then
                Dim leftFieldAccess = DirectCast(leftNode, BoundFieldAccess)
                If leftFieldAccess.IsConstant Then
#If DEBUG Then
                    Debug.Assert(Not _rewrittenNodes.Contains(node), "LocalRewriter: Rewriting the same node several times.")
                    Dim originalNode = node
#End If

                    Dim result = DirectCast(MyBase.VisitFieldAccess(leftFieldAccess), BoundExpression)

#If DEBUG Then
                    If result Is originalNode Then
                        result = result.MemberwiseClone(Of BoundExpression)()
                    End If
                    _rewrittenNodes.Add(result)
#End If
                    Return result
                End If
            End If

            Return Me.VisitExpressionNode(leftNode)
        End Function

        Private Function RewritePropertyAssignmentAsSetCall(node As BoundAssignmentOperator, setNode As BoundExpression) As BoundExpression
            Select Case setNode.Kind
                Case BoundKind.XmlMemberAccess
                    Return RewritePropertyAssignmentAsSetCall(node, DirectCast(setNode, BoundXmlMemberAccess).MemberAccess)

                Case BoundKind.PropertyAccess
                    Return RewritePropertyAssignmentAsSetCall(node, DirectCast(setNode, BoundPropertyAccess))

                Case Else
                    Throw ExceptionUtilities.UnexpectedValue(setNode.Kind)

            End Select
        End Function

        <Conditional("DEBUG")>
        Private Shared Sub AssertIsWriteableFromMember(node As BoundPropertyAccess, fromMember As Symbol)
            Dim receiver = node.ReceiverOpt

            Dim sourceProperty As SourcePropertySymbol = DirectCast(node.PropertySymbol, SourcePropertySymbol)
            Dim propertyIsStatic As Boolean = node.PropertySymbol.IsShared

            Debug.Assert(
                sourceProperty IsNot Nothing AndAlso
                sourceProperty.IsAutoProperty AndAlso
                TypeSymbol.Equals(sourceProperty.ContainingType, fromMember.ContainingType, TypeCompareKind.ConsiderEverything) AndAlso
                propertyIsStatic = fromMember.IsShared AndAlso
                (propertyIsStatic OrElse receiver.Kind = BoundKind.MeReference) AndAlso
                (fromMember.Kind = SymbolKind.Field OrElse (fromMember.Kind = SymbolKind.Method AndAlso
                                                            DirectCast(fromMember, MethodSymbol).IsAnyConstructor)))

        End Sub

        Private Function RewritePropertyAssignmentAsSetCall(node As BoundAssignmentOperator, setNode As BoundPropertyAccess) As BoundExpression
            Debug.Assert(setNode.AccessKind = PropertyAccessKind.Set)

            Dim [property] = setNode.PropertySymbol
            Dim setMethod = [property].GetMostDerivedSetMethod()

            If setMethod Is Nothing Then
                AssertIsWriteableFromMember(setNode, Me._currentMethodOrLambda)

                Dim backingField = [property].AssociatedField
                Debug.Assert(backingField IsNot Nothing, "autoproperty must have a backing field")

                Dim rewrittenReceiver = VisitExpressionNode(setNode.ReceiverOpt)
                Dim field = New BoundFieldAccess(setNode.Syntax,
                                                 rewrittenReceiver,
                                                 backingField,
                                                 isLValue:=True,
                                                 type:=backingField.Type)

                Dim rewrittenValue = VisitExpression(node.Right)

                Return New BoundAssignmentOperator(node.Syntax,
                                                   field,
                                                   rewrittenValue,
                                                   node.SuppressObjectClone,
                                                   node.Type)

            Else
                ' GenerateAccessorCall rewrites the arguments
                Return RewriteReceiverArgumentsAndGenerateAccessorCall(node.Syntax,
                                  setMethod,
                                  setNode.ReceiverOpt,
                                  setNode.Arguments.Concat(node.Right),
                                  node.ConstantValueOpt,
                                  isLValue:=False,
                                  suppressObjectClone:=False,
                                  type:=setMethod.ReturnType)
            End If

        End Function

        Private Function RewriteLateBoundAssignment(node As BoundAssignmentOperator) As BoundNode

            Dim assignmentTarget As BoundExpression = node.Left

#If DEBUG Then
            Dim accessKind As LateBoundAccessKind = node.Left.GetLateBoundAccessKind()
            Debug.Assert((accessKind And LateBoundAccessKind.Set) <> 0)
            Debug.Assert(((accessKind And LateBoundAccessKind.Get) = 0) = (node.LeftOnTheRightOpt Is Nothing))
#End If

            Dim temps = ImmutableArray(Of SynthesizedLocal).Empty

            If node.LeftOnTheRightOpt IsNot Nothing Then
                ' Make sure side effects are evaluated only once.
                assignmentTarget = assignmentTarget.SetLateBoundAccessKind(LateBoundAccessKind.Unknown)

                Dim temporaries = ArrayBuilder(Of SynthesizedLocal).GetInstance()
                Dim useTwice As UseTwiceRewriter.Result = UseTwiceRewriter.UseTwice(Me._currentMethodOrLambda, assignmentTarget, isForRegularCompoundAssignment:=False, temporaries)
                temps = temporaries.ToImmutableAndFree()

                Dim leftOnTheRight As BoundExpression

                assignmentTarget = useTwice.First.SetLateBoundAccessKind(LateBoundAccessKind.Set)
                leftOnTheRight = useTwice.Second.SetLateBoundAccessKind(LateBoundAccessKind.Get)

                AddPlaceholderReplacement(node.LeftOnTheRightOpt, VisitExpressionNode(leftOnTheRight))
            End If

            Dim value As BoundExpression = VisitExpressionNode(node.Right)

            If node.LeftOnTheRightOpt IsNot Nothing Then
                RemovePlaceholderReplacement(node.LeftOnTheRightOpt)
            End If

            Dim result As BoundExpression

            If assignmentTarget.Kind = BoundKind.LateMemberAccess Then
                ' objExpr.goo = bar
                result = LateSet(node.Syntax,
                                 DirectCast(MyBase.VisitLateMemberAccess(DirectCast(assignmentTarget, BoundLateMemberAccess)), BoundLateMemberAccess),
                                 value,
                                 Nothing,
                                 Nothing,
                                 isCopyBack:=False)
            Else
                Dim invocation = DirectCast(assignmentTarget, BoundLateInvocation)

                If invocation.Member.Kind = BoundKind.LateMemberAccess Then
                    ' objExpr.goo(args) = bar
                    result = LateSet(node.Syntax,
                                     DirectCast(MyBase.VisitLateMemberAccess(DirectCast(invocation.Member, BoundLateMemberAccess)), BoundLateMemberAccess),
                                     value,
                                     VisitList(invocation.ArgumentsOpt),
                                     invocation.ArgumentNamesOpt,
                                     isCopyBack:=False)
                Else
                    ' objExpr(args) = bar
                    invocation = invocation.Update(VisitExpressionNode(invocation.Member),
                                                   VisitList(invocation.ArgumentsOpt),
                                                   invocation.ArgumentNamesOpt,
                                                   invocation.AccessKind,
                                                   invocation.MethodOrPropertyGroupOpt,
                                                   invocation.Type)

                    result = LateIndexSet(node.Syntax,
                                          invocation,
                                          value,
                                          isCopyBack:=False)
                End If
            End If

            If temps.Length > 0 Then
                result = New BoundSequence(node.Syntax,
                                           StaticCast(Of LocalSymbol).From(temps),
                                           ImmutableArray.Create(Of BoundExpression)(result),
                                           Nothing,
                                           result.Type)
            End If

            Return result
        End Function

        Private Function VisitAndGenerateObjectCloneIfNeeded(right As BoundExpression, Optional suppressObjectClone As Boolean = False) As BoundExpression
            Return If(suppressObjectClone OrElse right.HasErrors(), VisitExpression(right), GenerateObjectCloneIfNeeded(right, VisitExpression(right)))
        End Function

        ''' <summary>
        ''' Apply GetObjectValue call if needed.
        ''' </summary>
        Private Function GenerateObjectCloneIfNeeded(generatedExpression As BoundExpression) As BoundExpression
            Return GenerateObjectCloneIfNeeded(generatedExpression, generatedExpression)
        End Function

        ''' <summary>
        ''' Apply GetObjectValue call if needed.
        ''' </summary>
        Private Function GenerateObjectCloneIfNeeded(expression As BoundExpression, rewrittenExpression As BoundExpression) As BoundExpression
            If expression.HasErrors OrElse rewrittenExpression.HasErrors OrElse Me._inExpressionLambda Then
                Return rewrittenExpression
            End If

            Dim result As BoundExpression = rewrittenExpression

            If Not result.HasErrors AndAlso result.Type.IsObjectType() AndAlso Not Me.ContainingAssembly.IsVbRuntime Then

                ' There are a series of object operations which we know don't require a call to GetObjectValue.
                ' These operations are math and logic operators.
                Dim nodeToCheck As BoundExpression = expression

                Do
                    If nodeToCheck.IsConstant Then
                        Debug.Assert(nodeToCheck.ConstantValueOpt.IsNothing)
                        Return result
                    End If

                    Select Case nodeToCheck.Kind
                        Case BoundKind.BinaryOperator

                            Dim binaryOperator = DirectCast(nodeToCheck, BoundBinaryOperator)

                            If (binaryOperator.OperatorKind And BinaryOperatorKind.UserDefined) = 0 Then
                                Select Case (binaryOperator.OperatorKind And BinaryOperatorKind.OpMask)
                                    Case BinaryOperatorKind.Power,
                                         BinaryOperatorKind.Divide,
                                         BinaryOperatorKind.Modulo,
                                         BinaryOperatorKind.IntegerDivide,
                                         BinaryOperatorKind.Concatenate,
                                         BinaryOperatorKind.And,
                                         BinaryOperatorKind.AndAlso,
                                         BinaryOperatorKind.Or,
                                         BinaryOperatorKind.OrElse,
                                         BinaryOperatorKind.Xor,
                                         BinaryOperatorKind.Multiply,
                                         BinaryOperatorKind.Add,
                                         BinaryOperatorKind.Subtract,
                                         BinaryOperatorKind.LeftShift,
                                         BinaryOperatorKind.RightShift

                                        Return result
                                End Select
                            End If

                            Exit Do

                        Case BoundKind.UnaryOperator

                            Dim unaryOperator = DirectCast(nodeToCheck, BoundUnaryOperator)

                            If (unaryOperator.OperatorKind And UnaryOperatorKind.UserDefined) = 0 Then
                                Select Case (unaryOperator.OperatorKind And UnaryOperatorKind.IntrinsicOpMask)
                                    Case UnaryOperatorKind.Minus,
                                         UnaryOperatorKind.Plus,
                                         UnaryOperatorKind.Not

                                        Return result
                                End Select
                            End If

                            Exit Do

                        Case BoundKind.DirectCast,
                             BoundKind.TryCast,
                             BoundKind.Conversion

                            Dim conversionKind As ConversionKind

                            If nodeToCheck.Kind = BoundKind.DirectCast Then
                                Dim conversion = DirectCast(nodeToCheck, BoundDirectCast)
                                conversionKind = conversion.ConversionKind
                                nodeToCheck = conversion.Operand
                            ElseIf nodeToCheck.Kind = BoundKind.TryCast Then
                                Dim conversion = DirectCast(nodeToCheck, BoundTryCast)
                                conversionKind = conversion.ConversionKind
                                nodeToCheck = conversion.Operand
                            Else
                                Dim conversion = DirectCast(nodeToCheck, BoundConversion)
                                conversionKind = conversion.ConversionKind
                                nodeToCheck = conversion.Operand
                            End If

                            Debug.Assert((conversionKind And ConversionKind.UserDefined) = 0)

                            ' there are cases where there's an explicit cast in code, that may be an identity conversion and 
                            ' it should still get ignored in order to create a call to the GetObjectValue helper.
                            ' e.g. happens in the conversion of the get method of the current property in a for each loop.
                            If Not Conversions.IsIdentityConversion(conversionKind) Then
                                Return result
                            End If

                        Case BoundKind.Parenthesized
                            nodeToCheck = DirectCast(nodeToCheck, BoundParenthesized).Expression

                        Case BoundKind.XmlEmbeddedExpression
                            nodeToCheck = DirectCast(nodeToCheck, BoundXmlEmbeddedExpression).Expression

                        Case Else
                            Exit Do
                    End Select
                Loop

                Const getObjectValue As WellKnownMember = WellKnownMember.System_Runtime_CompilerServices_RuntimeHelpers__GetObjectValueObject
                Dim getObjectValueMethod = DirectCast(Compilation.GetWellKnownTypeMember(getObjectValue), MethodSymbol)

                If Not ReportMissingOrBadRuntimeHelper(nodeToCheck, getObjectValue, getObjectValueMethod) Then
                    result = New BoundCall(expression.Syntax, getObjectValueMethod, Nothing, Nothing,
                                                      ImmutableArray.Create(result), Nothing, getObjectValueMethod.ReturnType)
                End If
            End If

            Return result
        End Function

        Private Function RewriteTrivialMidAssignment(node As BoundAssignmentOperator) As BoundExpression
            ' This is a case when target is an LValue (not a property, etc.) of type String.
            Dim midResult = DirectCast(node.Right, BoundMidResult)
            Debug.Assert(node.Left.IsLValue AndAlso node.LeftOnTheRightOpt IsNot Nothing AndAlso
                         (node.LeftOnTheRightOpt Is midResult.Original OrElse
                          (midResult.Original.Kind = BoundKind.Parenthesized AndAlso node.LeftOnTheRightOpt Is DirectCast(midResult.Original, BoundParenthesized).Expression)))
            Debug.Assert(midResult.Type.IsStringType())

            Dim memberSymbol As MethodSymbol = Nothing
            Const memberId As WellKnownMember = WellKnownMember.Microsoft_VisualBasic_CompilerServices_StringType__MidStmtStr
            memberSymbol = DirectCast(Compilation.GetWellKnownTypeMember(memberId), MethodSymbol)

            If ReportMissingOrBadRuntimeHelper(node, memberId, memberSymbol) Then
                Return midResult.Update(VisitExpressionNode(node.Left),
                                        VisitExpressionNode(midResult.Start),
                                        VisitExpressionNode(midResult.LengthOpt),
                                        VisitExpressionNode(midResult.Source),
                                        node.Type)
            End If

            Dim temporaries As ImmutableArray(Of SynthesizedLocal) = Nothing
            Dim copyBack As ImmutableArray(Of BoundExpression) = Nothing

            ' If the length is omitted, it is implicitly the full length of the string.
            ' This is encoded as the largest positive long value, which is greater than the
            ' maximum length of any string on a 32-bit platform.
            Dim midCall = New BoundCall(node.Syntax,
                                        memberSymbol,
                                        Nothing,
                                        Nothing,
                                        RewriteCallArguments(ImmutableArray.Create(node.Left, midResult.Start,
                                                              If(midResult.LengthOpt, New BoundLiteral(node.Syntax, ConstantValue.Create(&H7FFFFFFF), midResult.Start.Type)),
                                                              midResult.Source),
                                                             memberSymbol.Parameters,
                                                             temporaries,
                                                             copyBack,
                                                             suppressObjectClone:=False),
                                        Nothing,
                                        memberSymbol.ReturnType)

            Debug.Assert(temporaries.IsDefault)
            Debug.Assert(copyBack.IsDefault)

            Return midCall
        End Function

        Public Overrides Function VisitMidResult(node As BoundMidResult) As BoundNode
            ' This is a non-trivial case, either a conversion is involved or the target is a property, etc.
            ' Need to allocate a temp, store original string in it, pass it by ref to MidStmtStr and return its value after the call.
            ' We will achieve this by synthesizing and rewriting trivial Mid assignment with the temp as the target.

            Dim temp = New SynthesizedLocal(Me._currentMethodOrLambda, node.Type, SynthesizedLocalKind.LoweringTemp)
            Dim localRef = New BoundLocal(node.Syntax, temp, node.Type)
            Dim placeholder = New BoundCompoundAssignmentTargetPlaceholder(node.Syntax, node.Type)

            Return New BoundSequence(node.Syntax,
                                     ImmutableArray.Create(Of LocalSymbol)(temp),
                                     ImmutableArray.Create(Of BoundExpression)(New BoundAssignmentOperator(node.Syntax,
                                                                                                        localRef,
                                                                                                        VisitExpressionNode(node.Original),
                                                                                                        suppressObjectClone:=True),
                                                                            RewriteTrivialMidAssignment(New BoundAssignmentOperator(node.Syntax,
                                                                                                                                    localRef,
                                                                                                                                    placeholder,
                                                                                                                                    node.Update(placeholder,
                                                                                                                                                node.Start,
                                                                                                                                                node.LengthOpt,
                                                                                                                                                node.Source,
                                                                                                                                                node.Type),
                                                                                                                                    suppressObjectClone:=False))),
                                     localRef.MakeRValue(),
                                     node.Type)
        End Function
    End Class
End Namespace
