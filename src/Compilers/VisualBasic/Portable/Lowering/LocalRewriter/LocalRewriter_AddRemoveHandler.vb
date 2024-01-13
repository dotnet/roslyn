' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports System.Diagnostics
Imports System.Runtime.InteropServices
Imports Microsoft.CodeAnalysis.CodeGen
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic
    Partial Friend NotInheritable Class LocalRewriter
        Public Overrides Function VisitAddHandlerStatement(node As BoundAddHandlerStatement) As BoundNode
            Dim rewritten = RewriteAddRemoveHandler(node)

            If Instrument(node, rewritten) Then
                rewritten = _instrumenterOpt.InstrumentAddHandlerStatement(node, rewritten)
            End If

            Return rewritten
        End Function

        Public Overrides Function VisitRemoveHandlerStatement(node As BoundRemoveHandlerStatement) As BoundNode
            Dim rewritten = RewriteAddRemoveHandler(node)

            If Instrument(node, rewritten) Then
                rewritten = _instrumenterOpt.InstrumentRemoveHandlerStatement(node, rewritten)
            End If

            Return rewritten
        End Function

        Private Function RewriteAddRemoveHandler(node As BoundAddRemoveHandlerStatement) As BoundStatement
            Dim unwrappedEventAccess As BoundEventAccess = UnwrapEventAccess(node.EventAccess)
            Dim [event] = unwrappedEventAccess.EventSymbol

            Dim saveState As UnstructuredExceptionHandlingContext = LeaveUnstructuredExceptionHandlingContext(node)

            Dim result As BoundStatement

            If [event].IsWindowsRuntimeEvent Then
                result = RewriteWinRtEvent(node, unwrappedEventAccess, isAddition:=(node.Kind = BoundKind.AddHandlerStatement))
            Else
                result = MakeEventAccessorCall(node, unwrappedEventAccess, If(node.Kind = BoundKind.AddHandlerStatement, [event].AddMethod, [event].RemoveMethod))
            End If

            RestoreUnstructuredExceptionHandlingContext(node, saveState)

            If ShouldGenerateUnstructuredExceptionHandlingResumeCode(node) Then
                result = RegisterUnstructuredExceptionHandlingResumeTarget(node.Syntax, result, canThrow:=True)
            End If

            Return result
        End Function

        ''' <summary>
        ''' If we have a WinRT type event, we need to encapsulate the adder call
        ''' (which returns an EventRegistrationToken) with a call to 
        ''' WindowsRuntimeMarshal.AddEventHandler or RemoveEventHandler, but these
        ''' require us to create a new Func representing the adder and another
        ''' Action representing the remover.
        ''' 
        ''' The rewritten call looks something like:
        ''' 
        ''' WindowsRuntimeMarshal.AddEventHandler(Of TEventHandler)(
        '''            New Func(Of TEventHandler, EventRegistrationToken)([object].add_T), 
        '''            New Action(Of EventRegistrationToken)([object].remove_T), 
        '''            New TEventHandler(Me.OnSuspending))
        ''' 
        ''' 
        ''' where [object] is a compiler-generated local temp.
        ''' 
        ''' For a remover, the call looks like:
        ''' 
        ''' WindowsRuntimeMarshal.RemoveEventHandler(Of TEventHandler)(
        '''            New Action(Of EventRegistrationToken)([object].remove_T), 
        '''            New TEventHandler(Me.OnSuspending))
        ''' </summary>
        Private Function RewriteWinRtEvent(node As BoundAddRemoveHandlerStatement, unwrappedEventAccess As BoundEventAccess,
                                               isAddition As Boolean) As BoundStatement

            Dim syntax As SyntaxNode = node.Syntax
            Dim eventSymbol As EventSymbol = unwrappedEventAccess.EventSymbol

            Dim rewrittenReceiverOpt As BoundExpression = GetEventAccessReceiver(unwrappedEventAccess)
            Dim rewrittenHandler As BoundExpression = VisitExpressionNode(node.Handler)

            Dim tempAssignment As BoundAssignmentOperator = Nothing
            Dim boundTemp As BoundLocal = Nothing
            If Not eventSymbol.IsShared AndAlso EventReceiverNeedsTemp(rewrittenReceiverOpt) Then
                Dim receiverType As TypeSymbol = rewrittenReceiverOpt.Type
                boundTemp = New BoundLocal(syntax, New SynthesizedLocal(Me._currentMethodOrLambda, receiverType, SynthesizedLocalKind.LoweringTemp), receiverType)
                tempAssignment = New BoundAssignmentOperator(syntax, boundTemp, GenerateObjectCloneIfNeeded(unwrappedEventAccess.ReceiverOpt, rewrittenReceiverOpt.MakeRValue), True)
            End If

            Dim tokenType As NamedTypeSymbol = Me.Compilation.GetWellKnownType(WellKnownType.System_Runtime_InteropServices_WindowsRuntime_EventRegistrationToken)
            Dim marshalType As NamedTypeSymbol = Me.Compilation.GetWellKnownType(WellKnownType.System_Runtime_InteropServices_WindowsRuntime_WindowsRuntimeMarshal)

            Dim actionType As NamedTypeSymbol = Me.Compilation.GetWellKnownType(WellKnownType.System_Action_T)

            Dim eventType As TypeSymbol = eventSymbol.Type
            actionType = actionType.Construct(tokenType)

            Dim delegateCreationArgument = If(boundTemp, If(rewrittenReceiverOpt, New BoundTypeExpression(syntax, eventType).MakeCompilerGenerated)).MakeRValue

            Dim removeDelegate As BoundDelegateCreationExpression =
                New BoundDelegateCreationExpression(
                    syntax:=syntax,
                    receiverOpt:=delegateCreationArgument,
                    method:=eventSymbol.RemoveMethod,
                    relaxationLambdaOpt:=Nothing,
                    relaxationReceiverPlaceholderOpt:=Nothing,
                    methodGroupOpt:=Nothing,
                    type:=actionType)

            Dim marshalMethodId As WellKnownMember
            Dim marshalArguments As ImmutableArray(Of BoundExpression)
            If isAddition Then
                Dim func2Type As NamedTypeSymbol = Me.Compilation.GetWellKnownType(WellKnownType.System_Func_T2)
                func2Type = func2Type.Construct(eventType, tokenType)

                Dim addDelegate As BoundDelegateCreationExpression =
                    New BoundDelegateCreationExpression(
                        syntax:=syntax,
                        receiverOpt:=delegateCreationArgument,
                        method:=eventSymbol.AddMethod,
                        relaxationLambdaOpt:=Nothing,
                        relaxationReceiverPlaceholderOpt:=Nothing,
                        methodGroupOpt:=Nothing,
                        type:=func2Type)

                marshalMethodId = WellKnownMember.System_Runtime_InteropServices_WindowsRuntime_WindowsRuntimeMarshal__AddEventHandler_T
                marshalArguments = ImmutableArray.Create(Of BoundExpression)(addDelegate, removeDelegate, rewrittenHandler)
            Else
                marshalMethodId = WellKnownMember.System_Runtime_InteropServices_WindowsRuntime_WindowsRuntimeMarshal__RemoveEventHandler_T
                marshalArguments = ImmutableArray.Create(Of BoundExpression)(removeDelegate, rewrittenHandler)
            End If

            Dim marshalMethod As MethodSymbol = Nothing
            If Not TryGetWellknownMember(marshalMethod, marshalMethodId, syntax) Then
                Return New BoundExpressionStatement(
                    syntax,
                    New BoundBadExpression(
                        syntax,
                        LookupResultKind.Empty,
                        ImmutableArray.Create(Of Symbol)(eventSymbol),
                        ImmutableArray(Of BoundExpression).Empty,
                        ErrorTypeSymbol.UnknownResultType,
                        hasErrors:=True))
            End If

            marshalMethod = marshalMethod.Construct(eventType)

            Dim marshalCall As BoundExpression =
                New BoundCall(
                    syntax:=syntax,
                    method:=DirectCast(marshalMethod, MethodSymbol),
                    methodGroupOpt:=Nothing,
                    receiverOpt:=Nothing,
                    arguments:=marshalArguments,
                    constantValueOpt:=Nothing,
                    suppressObjectClone:=True,
                    type:=marshalMethod.ReturnType)

            If boundTemp Is Nothing Then
                Return New BoundExpressionStatement(syntax, marshalCall)
            End If

            Return New BoundBlock(
                syntax:=syntax,
                statementListSyntax:=Nothing,
                locals:=ImmutableArray.Create(Of LocalSymbol)(boundTemp.LocalSymbol),
                statements:=ImmutableArray.Create(Of BoundStatement)(
                    New BoundExpressionStatement(syntax, tempAssignment),
                    New BoundExpressionStatement(syntax, marshalCall)))
        End Function

        ' See LocalRewriter.NeedsTemp in C# if we want to use this more generally.
        Private Shared Function EventReceiverNeedsTemp(expression As BoundExpression) As Boolean
            Select Case expression.Kind
                Case BoundKind.MeReference, BoundKind.MyClassReference, BoundKind.MyBaseReference
                    Return False

                Case BoundKind.Literal
                    ' don't allocate a temp for simple integral primitive types
                    Return expression.Type IsNot Nothing AndAlso Not expression.Type.SpecialType.IsClrInteger()

                Case BoundKind.Local, BoundKind.Parameter
                    Return False

                Case Else
                    Return True
            End Select
        End Function

        Public Overrides Function VisitEventAccess(node As BoundEventAccess) As BoundNode
            Debug.Assert(False, "All event accesses should be handled by AddHandler/RemoveHandler/RaiseEvent.")
            Return MyBase.VisitEventAccess(node)
        End Function

        Private Function MakeEventAccessorCall(node As BoundAddRemoveHandlerStatement,
                                               unwrappedEventAccess As BoundEventAccess,
                                               accessorSymbol As MethodSymbol) As BoundStatement

            Dim receiver As BoundExpression = GetEventAccessReceiver(unwrappedEventAccess)
            Dim handler As BoundExpression = VisitExpressionNode(node.Handler)
            Dim [event] = unwrappedEventAccess.EventSymbol
            Dim expr As BoundExpression = Nothing

            If receiver IsNot Nothing AndAlso
                [event].ContainingAssembly.IsLinked AndAlso
                [event].ContainingType.IsInterfaceType() Then

                Dim [interface] = [event].ContainingType

                For Each attrData In [interface].GetAttributes()
                    Dim signatureIndex As Integer = attrData.GetTargetAttributeSignatureIndex(AttributeDescription.ComEventInterfaceAttribute)

                    If signatureIndex = 0 Then
                        Dim errorInfo As DiagnosticInfo = attrData.ErrorInfo
                        If errorInfo IsNot Nothing Then
                            _diagnostics.Add(errorInfo, node.EventAccess.Syntax.Location)
                        End If

                        If Not attrData.HasErrors Then
                            expr = RewriteNoPiaAddRemoveHandler(node, receiver, [event], handler)
                            Exit For
                        End If
                    End If
                Next
            End If

            If expr Is Nothing Then
                expr = New BoundCall(node.Syntax,
                                     accessorSymbol,
                                     Nothing,
                                     receiver,
                                     ImmutableArray.Create(handler),
                                     Nothing,
                                     accessorSymbol.ReturnType)
            End If

            Return New BoundExpressionStatement(node.Syntax, expr)
        End Function

        Private Function UnwrapEventAccess(node As BoundExpression) As BoundEventAccess
            If node.Kind = BoundKind.EventAccess Then
                Return DirectCast(node, BoundEventAccess)
            End If

            Debug.Assert(node.Kind = BoundKind.Parenthesized, "node can only be EventAccess or Parenthesized")
            Return UnwrapEventAccess(DirectCast(node, BoundParenthesized).Expression)
        End Function

        Private Function GetEventAccessReceiver(unwrappedEventAccess As BoundEventAccess) As BoundExpression
            If unwrappedEventAccess.ReceiverOpt Is Nothing Then
                Return Nothing
            End If

            ' Visit (for diagnostics) regardless of whether or not it will be returned.
            Dim rewrittenReceiver As BoundExpression = VisitExpressionNode(unwrappedEventAccess.ReceiverOpt)
            Return If(unwrappedEventAccess.EventSymbol.IsShared, Nothing, rewrittenReceiver)
        End Function

        Private Function RewriteNoPiaAddRemoveHandler(
            node As BoundAddRemoveHandlerStatement,
            receiver As BoundExpression,
            [event] As EventSymbol,
            handler As BoundExpression
        ) As BoundExpression
            ' Translate: AddHandler myPIA.Event, Handler
            ' to: New ComAwareEventInfo(GetType(myPIA), "Event").AddEventHandler(myPIA, Handler)

            Dim factory As New SyntheticBoundNodeFactory(_topMethod, _currentMethodOrLambda, node.Syntax, _compilationState, _diagnostics)
            Dim result As BoundExpression = Nothing

            Dim ctor = factory.WellKnownMember(Of MethodSymbol)(WellKnownMember.System_Runtime_InteropServices_ComAwareEventInfo__ctor)
            If ctor IsNot Nothing Then
                Dim addRemove = factory.WellKnownMember(Of MethodSymbol)(If(node.Kind = BoundKind.AddHandlerStatement,
                                                                            WellKnownMember.System_Runtime_InteropServices_ComAwareEventInfo__AddEventHandler,
                                                                            WellKnownMember.System_Runtime_InteropServices_ComAwareEventInfo__RemoveEventHandler))
                If addRemove IsNot Nothing Then
                    Dim eventInfo = factory.[New](ctor, factory.Typeof([event].ContainingType), factory.Literal([event].MetadataName))
                    result = factory.Call(eventInfo,
                                          addRemove,
                                          Convert(factory, addRemove.Parameters(0).Type, receiver.MakeRValue()),
                                          Convert(factory, addRemove.Parameters(1).Type, handler))
                End If
            End If

            ' The code we just generated doesn't contain any direct references to the event itself,
            ' but the com event binder needs the event to exist on the local type. We'll poke the pia reference
            ' cache directly so that the event is embedded.
            If _emitModule IsNot Nothing Then
                _emitModule.EmbeddedTypesManagerOpt.EmbedEventIfNeedTo([event].GetCciAdapter(), node.Syntax, _diagnostics.DiagnosticBag, isUsedForComAwareEventBinding:=True)
            End If

            If result IsNot Nothing Then
                Return result
            End If

            Return New BoundBadExpression(node.Syntax,
                                          LookupResultKind.NotCreatable,
                                          ImmutableArray.Create(Of Symbol)([event]),
                                          ImmutableArray.Create(receiver, handler),
                                          ErrorTypeSymbol.UnknownResultType,
                                          hasErrors:=True)
        End Function

        Private Function Convert(factory As SyntheticBoundNodeFactory, type As TypeSymbol, expr As BoundExpression) As BoundExpression
            Return TransformRewrittenConversion(factory.Convert(type, expr))
        End Function

    End Class
End Namespace
