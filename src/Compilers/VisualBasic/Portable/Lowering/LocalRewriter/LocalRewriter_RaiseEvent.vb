' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable
Imports System.Diagnostics
Imports Microsoft.CodeAnalysis.RuntimeMembers
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols

Namespace Microsoft.CodeAnalysis.VisualBasic
    Partial Friend NotInheritable Class LocalRewriter
        Public Overrides Function VisitRaiseEventStatement(node As BoundRaiseEventStatement) As BoundNode
            Dim syntax = node.Syntax

            Dim saveState As UnstructuredExceptionHandlingContext = LeaveUnstructuredExceptionHandlingContext(node)

            ' in the absence of errors invocation must be a call 
            '(could also be BadExpression, but that would have errors)
            Dim raiseCallExpression = DirectCast(node.EventInvocation, BoundCall)

            Dim result As BoundStatement
            Dim receiver = raiseCallExpression.ReceiverOpt

            If receiver Is Nothing OrElse receiver.IsMeReference Then
                result = New BoundExpressionStatement(
                                syntax,
                                VisitExpressionNode(raiseCallExpression))

            Else
                Debug.Assert(receiver.Kind = BoundKind.FieldAccess)

#If DEBUG Then
                ' NOTE: The receiver is always as lowered as it's going to get (generally, a MeReference), so there's no need to Visit it.
                Dim fieldAccess As BoundFieldAccess = DirectCast(receiver, BoundFieldAccess)
                Dim fieldAccessReceiver = fieldAccess.ReceiverOpt
                Debug.Assert(fieldAccessReceiver Is Nothing OrElse
                             fieldAccessReceiver.Kind = BoundKind.MeReference)
#End If


                If node.EventSymbol.IsWindowsRuntimeEvent Then
                    receiver = GetWindowsRuntimeEventReceiver(syntax, receiver)
                End If

                ' Need to null-check the receiver before invoking raise -
                '
                ' eventField.raiseCallExpression  === becomes ===>
                '
                ' Block
                '     Dim temp = eventField
                '     if temp is Nothing GoTo skipEventRaise
                '        Call temp.raiseCallExpression
                '     skipEventRaise:
                ' End Block
                '
                Dim temp As LocalSymbol = New SynthesizedLocal(Me._currentMethodOrLambda, receiver.Type, SynthesizedLocalKind.LoweringTemp)
                Dim tempAccess As BoundLocal = New BoundLocal(syntax, temp, temp.Type).MakeCompilerGenerated

                Dim tempInit = New BoundExpressionStatement(syntax,
                                                            New BoundAssignmentOperator(syntax, tempAccess, receiver, True, receiver.Type)).MakeCompilerGenerated

                ' replace receiver with temp.
                raiseCallExpression = raiseCallExpression.Update(raiseCallExpression.Method,
                                                        raiseCallExpression.MethodGroupOpt,
                                                        tempAccess,
                                                        raiseCallExpression.Arguments,
                                                        raiseCallExpression.DefaultArguments,
                                                        raiseCallExpression.ConstantValueOpt,
                                                        isLValue:=raiseCallExpression.IsLValue,
                                                        suppressObjectClone:=raiseCallExpression.SuppressObjectClone,
                                                        type:=raiseCallExpression.Type)

                Dim invokeStatement = New BoundExpressionStatement(
                                            syntax,
                                            VisitExpressionNode(raiseCallExpression))

                Dim condition = New BoundBinaryOperator(syntax,
                                                        BinaryOperatorKind.Is,
                                                        tempAccess.MakeRValue(),
                                                        New BoundLiteral(syntax, ConstantValue.Nothing,
                                                                         Me.Compilation.GetSpecialType(SpecialType.System_Object)),
                                                        False,
                                                        Me.Compilation.GetSpecialType(SpecialType.System_Boolean)).MakeCompilerGenerated

                Dim skipEventRaise As New GeneratedLabelSymbol("skipEventRaise")

                Dim ifNullSkip = New BoundConditionalGoto(syntax, condition, True, skipEventRaise).MakeCompilerGenerated

                result = New BoundBlock(syntax,
                                        Nothing,
                                        ImmutableArray.Create(temp),
                                        ImmutableArray.Create(Of BoundStatement)(
                                            tempInit,
                                            ifNullSkip,
                                            invokeStatement,
                                            New BoundLabelStatement(syntax, skipEventRaise)))
            End If

            RestoreUnstructuredExceptionHandlingContext(node, saveState)

            If ShouldGenerateUnstructuredExceptionHandlingResumeCode(node) Then
                result = RegisterUnstructuredExceptionHandlingResumeTarget(node.Syntax, result, canThrow:=True)
            End If

            If Instrument(node, result) Then
                result = _instrumenterOpt.InstrumentRaiseEventStatement(node, result)
            End If

            Return result
        End Function

        ' If the event is a WinRT event, then the backing field is actually an EventRegistrationTokenTable,
        ' rather than a delegate.  If this is the case, then we replace the receiver with 
        ' EventRegistrationTokenTable(Of Event).GetOrCreateEventRegistrationTokenTable(eventField).InvocationList.
        Private Function GetWindowsRuntimeEventReceiver(syntax As SyntaxNode, rewrittenReceiver As BoundExpression) As BoundExpression
            Dim fieldType As NamedTypeSymbol = DirectCast(rewrittenReceiver.Type, NamedTypeSymbol)
            Debug.Assert(fieldType.Name = "EventRegistrationTokenTable")

            Dim getOrCreateMethod As MethodSymbol = DirectCast(Compilation.GetWellKnownTypeMember(
                    WellKnownMember.System_Runtime_InteropServices_WindowsRuntime_EventRegistrationTokenTable_T__GetOrCreateEventRegistrationTokenTable), MethodSymbol)

            Debug.Assert(getOrCreateMethod IsNot Nothing, "Checked during initial binding")
            Debug.Assert(TypeSymbol.Equals(getOrCreateMethod.ReturnType, fieldType.OriginalDefinition, TypeCompareKind.ConsiderEverything), "Shape of well-known member")

            getOrCreateMethod = getOrCreateMethod.AsMember(fieldType)

            Dim invocationListProperty As PropertySymbol = Nothing
            If TryGetWellknownMember(invocationListProperty, WellKnownMember.System_Runtime_InteropServices_WindowsRuntime_EventRegistrationTokenTable_T__InvocationList, syntax) Then
                Dim invocationListAccessor As MethodSymbol = invocationListProperty.GetMethod

                If invocationListAccessor IsNot Nothing Then
                    invocationListAccessor = invocationListAccessor.AsMember(fieldType)

                    ' EventRegistrationTokenTable(Of Event).GetOrCreateEventRegistrationTokenTable(_tokenTable)
                    Dim getOrCreateCall = New BoundCall(syntax:=syntax,
                                                        method:=getOrCreateMethod,
                                                        methodGroupOpt:=Nothing,
                                                        receiverOpt:=Nothing,
                                                        arguments:=ImmutableArray.Create(Of BoundExpression)(rewrittenReceiver),
                                                        constantValueOpt:=Nothing,
                                                        isLValue:=False,
                                                        suppressObjectClone:=False,
                                                        type:=getOrCreateMethod.ReturnType).MakeCompilerGenerated()

                    ' EventRegistrationTokenTable(Of Event).GetOrCreateEventRegistrationTokenTable(_tokenTable).InvocationList
                    Dim invocationListAccessorCall = New BoundCall(syntax:=syntax,
                                                                   method:=invocationListAccessor,
                                                                   methodGroupOpt:=Nothing,
                                                                   receiverOpt:=getOrCreateCall,
                                                                   arguments:=ImmutableArray(Of BoundExpression).Empty,
                                                                   constantValueOpt:=Nothing,
                                                                   isLValue:=False,
                                                                   suppressObjectClone:=False,
                                                                   type:=invocationListAccessor.ReturnType).MakeCompilerGenerated()

                    Return invocationListAccessorCall
                End If

                Dim memberDescriptor As MemberDescriptor = WellKnownMembers.GetDescriptor(WellKnownMember.System_Runtime_InteropServices_WindowsRuntime_EventRegistrationTokenTable_T__InvocationList)
                ' isWinMd only matters for set accessors, we can safely say false here
                Dim accessorName As String = Binder.GetAccessorName(invocationListProperty.Name, MethodKind.PropertyGet, isWinMd:=False)
                Dim info = GetDiagnosticForMissingRuntimeHelper(memberDescriptor.DeclaringTypeMetadataName, accessorName, _compilationState.Compilation.Options.EmbedVbCoreRuntime)
                _diagnostics.Add(info, syntax.GetLocation())
            End If

            Return New BoundBadExpression(syntax, LookupResultKind.NotReferencable, ImmutableArray(Of Symbol).Empty, ImmutableArray.Create(rewrittenReceiver), ErrorTypeSymbol.UnknownResultType, hasErrors:=True)
        End Function
    End Class
End Namespace
