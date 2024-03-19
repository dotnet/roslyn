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
        Public Overrides Function VisitCall(node As BoundCall) As BoundNode
            Debug.Assert(Not node.IsConstant, "Constant calls should become literals by now")

            Dim receiver As BoundExpression = node.ReceiverOpt
            Dim method As MethodSymbol = node.Method
            Dim arguments As ImmutableArray(Of BoundExpression) = node.Arguments

            ' Replace a call to AscW(<non constant char>) with a conversion, this makes sure we don't have a recursion inside AscW(Char).
            If method Is Compilation.GetWellKnownTypeMember(WellKnownMember.Microsoft_VisualBasic_Strings__AscWCharInt32) Then
                Return New BoundConversion(node.Syntax,
                                           VisitExpressionNode(arguments(0)),
                                           ConversionKind.WideningNumeric,
                                           checked:=False,
                                           explicitCastInCode:=True,
                                           type:=node.Type)
            End If

            ' Code below is for remapping of versioned functions.
            '
            ' Code compiled with VS7.1 or earlier must run on VS8.0 with no behavior changes.
            ' However, since VS8.0 introduces several new features which change the behavior of
            ' Public functions, we introduce the new behavior in a new function and map references
            ' to the old function onto the new function. In this way, old code continues to run and
            ' new code gets the new behavior, but the Public function doesn't change at all, from the
            ' user's point of view.
            ' Example:
            '
            '     Given:
            '     1. User code snippet: "If IsNumeric(something) Then ..."
            '     2. Public Function IsNumeric(o), has VS7.1 behavior.
            '     3. Public Function Versioned.IsNumeric(o), has VS8.0 behavior.
            '
            '     When compiled in VS7.1, the compiler binds to IsNumeric(o).
            '     When compiled in VS8.0, the compiler first binds to IsNumeric(o) and then remaps
            '     this binding to Versioned.IsNumeric(o) in the code generator so that the VS8.0
            '     behavior is selected.
            '
            Dim remappedMethodId As WellKnownMember = WellKnownMember.Count

            If method Is Compilation.GetWellKnownTypeMember(WellKnownMember.Microsoft_VisualBasic_Interaction__CallByName) Then
                remappedMethodId = WellKnownMember.Microsoft_VisualBasic_CompilerServices_Versioned__CallByName
            ElseIf method.ContainingSymbol Is Compilation.GetWellKnownType(WellKnownType.Microsoft_VisualBasic_Information) Then
                If method Is Compilation.GetWellKnownTypeMember(WellKnownMember.Microsoft_VisualBasic_Information__IsNumeric) Then
                    remappedMethodId = WellKnownMember.Microsoft_VisualBasic_CompilerServices_Versioned__IsNumeric
                ElseIf method Is Compilation.GetWellKnownTypeMember(WellKnownMember.Microsoft_VisualBasic_Information__SystemTypeName) Then
                    remappedMethodId = WellKnownMember.Microsoft_VisualBasic_CompilerServices_Versioned__SystemTypeName
                ElseIf method Is Compilation.GetWellKnownTypeMember(WellKnownMember.Microsoft_VisualBasic_Information__TypeName) Then
                    remappedMethodId = WellKnownMember.Microsoft_VisualBasic_CompilerServices_Versioned__TypeName
                ElseIf method Is Compilation.GetWellKnownTypeMember(WellKnownMember.Microsoft_VisualBasic_Information__VbTypeName) Then
                    remappedMethodId = WellKnownMember.Microsoft_VisualBasic_CompilerServices_Versioned__VbTypeName
                End If
            End If

            If remappedMethodId <> WellKnownMember.Count Then
                Dim remappedMethod = DirectCast(Compilation.GetWellKnownTypeMember(remappedMethodId), MethodSymbol)

                If remappedMethod IsNot Nothing AndAlso Not ReportMissingOrBadRuntimeHelper(node, remappedMethodId, remappedMethod) Then
                    method = remappedMethod
                End If
            End If

            UpdateMethodAndArgumentsIfReducedFromMethod(method, receiver, arguments)

            Dim temporaries As ImmutableArray(Of SynthesizedLocal) = Nothing
            Dim copyBack As ImmutableArray(Of BoundExpression) = Nothing
            Dim suppressObjectClone As Boolean = node.SuppressObjectClone OrElse
                                                 method Is Compilation.GetWellKnownTypeMember(
                                                     WellKnownMember.System_Runtime_CompilerServices_RuntimeHelpers__GetObjectValueObject)

            receiver = VisitExpressionNode(receiver)

            node = node.Update(method,
                               Nothing,
                               receiver,
                               RewriteCallArguments(arguments, method.Parameters, temporaries, copyBack, suppressObjectClone),
                               node.DefaultArguments,
                               Nothing,
                               isLValue:=node.IsLValue,
                               suppressObjectClone:=True,
                               type:=node.Type)

            If Not copyBack.IsDefault Then
                Return GenerateSequenceValueSideEffects(_currentMethodOrLambda, node, StaticCast(Of LocalSymbol).From(temporaries), copyBack)
            End If

            If Not temporaries.IsDefault Then
                If method.IsSub Then
                    Return New BoundSequence(node.Syntax,
                                             StaticCast(Of LocalSymbol).From(temporaries),
                                             ImmutableArray.Create(Of BoundExpression)(node),
                                             Nothing,
                                             node.Type)
                Else
                    Return New BoundSequence(node.Syntax,
                                             StaticCast(Of LocalSymbol).From(temporaries),
                                             ImmutableArray(Of BoundExpression).Empty,
                                             node,
                                             node.Type)
                End If
            End If

            Return node
        End Function

        Private Shared Sub UpdateMethodAndArgumentsIfReducedFromMethod(
            ByRef method As MethodSymbol,
            ByRef receiver As BoundExpression,
            ByRef arguments As ImmutableArray(Of BoundExpression))

            If receiver Is Nothing Then
                Return
            End If

            Dim reducedFrom As MethodSymbol = method.CallsiteReducedFromMethod
            If reducedFrom Is Nothing Then
                Return
            End If

            ' This is an extension method call
            If arguments.IsEmpty Then
                arguments = ImmutableArray.Create(Of BoundExpression)(receiver)
            Else
                Dim array(arguments.Length) As BoundExpression

                array(0) = receiver
                arguments.CopyTo(array, 1)
                arguments = array.AsImmutableOrNull()
            End If

            receiver = Nothing
            method = reducedFrom
        End Sub

        Public Overrides Function VisitByRefArgumentWithCopyBack(node As BoundByRefArgumentWithCopyBack) As BoundNode
            Throw ExceptionUtilities.Unreachable
        End Function

        Private Function RewriteCallArguments(
            arguments As ImmutableArray(Of BoundExpression),
            parameters As ImmutableArray(Of ParameterSymbol),
            <Out()> ByRef temporaries As ImmutableArray(Of SynthesizedLocal),
            <Out()> ByRef copyBack As ImmutableArray(Of BoundExpression),
            suppressObjectClone As Boolean
        ) As ImmutableArray(Of BoundExpression)
            temporaries = Nothing
            copyBack = Nothing

            If arguments.IsEmpty Then
                Return arguments
            End If

            Dim tempsArray As ArrayBuilder(Of SynthesizedLocal) = Nothing
            Dim copyBackArray As ArrayBuilder(Of BoundExpression) = Nothing
            Dim rewrittenArgs = ArrayBuilder(Of BoundExpression).GetInstance
            Dim changed As Boolean = False

            Dim paramIdx = 0

            For Each argument In arguments
                Dim rewritten As BoundExpression

                If argument.Kind = BoundKind.ByRefArgumentWithCopyBack Then
                    rewritten = RewriteByRefArgumentWithCopyBack(DirectCast(argument, BoundByRefArgumentWithCopyBack), tempsArray, copyBackArray)
                Else
                    rewritten = VisitExpressionNode(argument)

                    If parameters(paramIdx).IsByRef AndAlso Not argument.IsLValue AndAlso Not _inExpressionLambda Then
                        rewritten = PassArgAsTempClone(argument, rewritten, tempsArray)
                    End If

                End If

                If Not suppressObjectClone AndAlso (Not parameters(paramIdx).IsByRef OrElse Not rewritten.IsLValue) Then
                    rewritten = GenerateObjectCloneIfNeeded(argument, rewritten)
                End If

                If rewritten IsNot argument Then
                    changed = True
                End If

                rewrittenArgs.Add(rewritten)
                paramIdx += 1
            Next

            Debug.Assert(temporaries.IsDefault OrElse changed)

            If changed Then
                arguments = rewrittenArgs.ToImmutable()
            End If

            rewrittenArgs.Free()

            If tempsArray IsNot Nothing Then
                temporaries = tempsArray.ToImmutableAndFree()
            End If

            If copyBackArray IsNot Nothing Then
                ' It might feel strange, but Dev11 evaluates copy-back assignments in reverse order (from last argument to the first),
                ' which is observable. Doing the same thing.
                copyBackArray.ReverseContents()
                copyBack = copyBackArray.ToImmutableAndFree()
            End If

            Return arguments
        End Function

        Private Function PassArgAsTempClone(
            argument As BoundExpression,
            rewrittenArgument As BoundExpression,
            ByRef tempsArray As ArrayBuilder(Of SynthesizedLocal)
        ) As BoundExpression

            ' Need to allocate a temp of the target type,
            ' init it with argument's value,
            ' pass it ByRef

            If tempsArray Is Nothing Then
                tempsArray = ArrayBuilder(Of SynthesizedLocal).GetInstance()
            End If

            Dim temp = New SynthesizedLocal(Me._currentMethodOrLambda, rewrittenArgument.Type, SynthesizedLocalKind.LoweringTemp)
            tempsArray.Add(temp)

            Dim boundTemp = New BoundLocal(rewrittenArgument.Syntax, temp, temp.Type)
            Dim storeVal As BoundExpression = New BoundAssignmentOperator(rewrittenArgument.Syntax,
                                                                          boundTemp,
                                                                          GenerateObjectCloneIfNeeded(argument, rewrittenArgument),
                                                                          True,
                                                                          rewrittenArgument.Type)

            Return New BoundSequence(rewrittenArgument.Syntax, ImmutableArray(Of LocalSymbol).Empty, ImmutableArray.Create(storeVal), boundTemp, rewrittenArgument.Type)
        End Function

        Private Function RewriteByRefArgumentWithCopyBack(
            argument As BoundByRefArgumentWithCopyBack,
            ByRef tempsArray As ArrayBuilder(Of SynthesizedLocal),
            ByRef copyBackArray As ArrayBuilder(Of BoundExpression)
        ) As BoundExpression
            ' Need to allocate a temp of the target type,
            ' init it with argument's value,
            ' pass it ByRef,
            ' copy value back after the call.

            Dim originalArgument As BoundExpression = argument.OriginalArgument

            If originalArgument.IsPropertyOrXmlPropertyAccess Then
                Debug.Assert(originalArgument.GetAccessKind() = If(
                             originalArgument.IsPropertyReturnsByRef(),
                             PropertyAccessKind.Get,
                             PropertyAccessKind.Get Or PropertyAccessKind.Set))
                originalArgument = originalArgument.SetAccessKind(PropertyAccessKind.Unknown)
            ElseIf originalArgument.IsLateBound() Then
                Debug.Assert(originalArgument.GetLateBoundAccessKind() = (LateBoundAccessKind.Get Or LateBoundAccessKind.Set))
                originalArgument = originalArgument.SetLateBoundAccessKind(LateBoundAccessKind.Unknown)
            End If

            If _inExpressionLambda Then
                If originalArgument.IsPropertyOrXmlPropertyAccess Then
                    Debug.Assert(originalArgument.GetAccessKind() = PropertyAccessKind.Unknown)
                    originalArgument = originalArgument.SetAccessKind(PropertyAccessKind.Get)
                ElseIf originalArgument.IsLateBound() Then
                    Debug.Assert(originalArgument.GetLateBoundAccessKind() = LateBoundAccessKind.Unknown)
                    originalArgument = originalArgument.SetLateBoundAccessKind(LateBoundAccessKind.Get)
                End If

                If originalArgument.IsLValue Then
                    originalArgument = originalArgument.MakeRValue
                End If

                AddPlaceholderReplacement(argument.InPlaceholder, VisitExpressionNode(originalArgument))
                Dim rewrittenArgumentInConversion As BoundExpression = VisitExpression(argument.InConversion)
                RemovePlaceholderReplacement(argument.InPlaceholder)

                Return rewrittenArgumentInConversion
            End If

            If tempsArray Is Nothing Then
                tempsArray = ArrayBuilder(Of SynthesizedLocal).GetInstance()
            End If

            If copyBackArray Is Nothing Then
                copyBackArray = ArrayBuilder(Of BoundExpression).GetInstance()
            End If

            Dim firstUse As BoundExpression
            Dim secondUse As BoundExpression

            Dim useTwice As UseTwiceRewriter.Result = UseTwiceRewriter.UseTwice(Me._currentMethodOrLambda, originalArgument, isForRegularCompoundAssignment:=False, tempsArray)

            If originalArgument.IsPropertyOrXmlPropertyAccess Then
                firstUse = useTwice.First.SetAccessKind(PropertyAccessKind.Get).MakeRValue()
                secondUse = useTwice.Second.SetAccessKind(If(originalArgument.IsPropertyReturnsByRef(), PropertyAccessKind.Get, PropertyAccessKind.Set))

            ElseIf originalArgument.IsLateBound() Then
                firstUse = useTwice.First.SetLateBoundAccessKind(LateBoundAccessKind.Get)
                secondUse = useTwice.Second.SetLateBoundAccessKind(LateBoundAccessKind.Set)

            Else
                firstUse = useTwice.First.MakeRValue()
                secondUse = useTwice.Second
                Debug.Assert(secondUse.IsLValue)
            End If

            AddPlaceholderReplacement(argument.InPlaceholder, VisitExpressionNode(firstUse))
            Dim inputValue As BoundExpression = VisitAndGenerateObjectCloneIfNeeded(argument.InConversion)
            RemovePlaceholderReplacement(argument.InPlaceholder)

            Dim temp = New SynthesizedLocal(Me._currentMethodOrLambda, argument.Type, SynthesizedLocalKind.LoweringTemp)
            tempsArray.Add(temp)

            Dim boundTemp = New BoundLocal(argument.Syntax, temp, temp.Type)

            Dim storeVal As BoundExpression = New BoundAssignmentOperator(argument.Syntax, boundTemp, inputValue, True, argument.Type)

            AddPlaceholderReplacement(argument.OutPlaceholder, boundTemp.MakeRValue())

            Dim copyBack As BoundExpression

            If Not originalArgument.IsLateBound() Then
                copyBack = DirectCast(
                        VisitAssignmentOperator(New BoundAssignmentOperator(argument.Syntax, secondUse, argument.OutConversion, False)),
                        BoundExpression)
                RemovePlaceholderReplacement(argument.OutPlaceholder)
            Else
                Dim copyBackValue As BoundExpression = VisitExpressionNode(argument.OutConversion)
                RemovePlaceholderReplacement(argument.OutPlaceholder)

                If secondUse.Kind = BoundKind.LateMemberAccess Then
                    ' Method(ref objExpr.goo)

                    copyBack = LateSet(secondUse.Syntax,
                                       DirectCast(MyBase.VisitLateMemberAccess(DirectCast(secondUse, BoundLateMemberAccess)), BoundLateMemberAccess),
                                       copyBackValue,
                                       Nothing,
                                       Nothing,
                                       isCopyBack:=True)
                Else
                    Dim invocation = DirectCast(secondUse, BoundLateInvocation)

                    If invocation.Member.Kind = BoundKind.LateMemberAccess Then
                        ' Method(ref objExpr.goo(args))
                        copyBack = LateSet(invocation.Syntax,
                                           DirectCast(MyBase.VisitLateMemberAccess(DirectCast(invocation.Member, BoundLateMemberAccess)), BoundLateMemberAccess),
                                           copyBackValue,
                                           VisitList(invocation.ArgumentsOpt),
                                           invocation.ArgumentNamesOpt,
                                           isCopyBack:=True)
                    Else
                        ' Method(ref objExpr(args))
                        invocation = invocation.Update(VisitExpressionNode(invocation.Member),
                                                       VisitList(invocation.ArgumentsOpt),
                                                       invocation.ArgumentNamesOpt,
                                                       invocation.AccessKind,
                                                       invocation.MethodOrPropertyGroupOpt,
                                                       invocation.Type)

                        copyBack = LateIndexSet(invocation.Syntax,
                                                invocation,
                                                copyBackValue,
                                                isCopyBack:=True)
                    End If
                End If
            End If

            copyBackArray.Add(copyBack)

            Return New BoundSequence(argument.Syntax, ImmutableArray(Of LocalSymbol).Empty, ImmutableArray.Create(storeVal), boundTemp, argument.Type)
        End Function

    End Class

End Namespace
