' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports Microsoft.CodeAnalysis.PooledObjects

Namespace Microsoft.CodeAnalysis.VisualBasic.Symbols

    Friend Module SynthesizedPropertyAccessorHelper

        Friend Function GetBoundMethodBody(accessor As MethodSymbol,
                                                            backingField As FieldSymbol,
                                                            Optional ByRef methodBodyBinder As Binder = Nothing) As BoundBlock
            methodBodyBinder = Nothing

            ' NOTE: Current implementation of this method does generate the code for both getter and setter,
            '       Ideally it could have been split into two different implementations, but the code gen is
            '       quite similar in these two cases and current types hierarchy makes this solution preferable

            Dim propertySymbol = DirectCast(accessor.AssociatedSymbol, PropertySymbol)

            Dim syntax = DirectCast(VisualBasic.VisualBasicSyntaxTree.Dummy.GetRoot(), VisualBasicSyntaxNode)

            If propertySymbol.Type.IsVoidType Then
                ' An error is reported elsewhere
                Return (New BoundBlock(syntax, Nothing, ImmutableArray(Of LocalSymbol).Empty, ImmutableArray(Of BoundStatement).Empty, hasErrors:=True)).MakeCompilerGenerated()
            End If

            Dim meSymbol As ParameterSymbol = Nothing
            Dim meReference As BoundExpression = Nothing

            If Not accessor.IsShared Then
                meSymbol = accessor.MeParameter
                meReference = New BoundMeReference(syntax, meSymbol.Type)
            End If

            Dim isOverride As Boolean = propertySymbol.IsWithEvents AndAlso propertySymbol.IsOverrides

            Dim field As FieldSymbol = Nothing
            Dim fieldAccess As BoundFieldAccess = Nothing

            Dim myBaseReference As BoundExpression = Nothing
            Dim baseGet As BoundExpression = Nothing

            If isOverride Then
                ' overriding property gets its value via a base call
                myBaseReference = New BoundMyBaseReference(syntax, meSymbol.Type)
                Dim baseGetSym = propertySymbol.GetMethod.OverriddenMethod

                baseGet = New BoundCall(
                    syntax,
                    baseGetSym,
                    Nothing,
                    myBaseReference,
                    ImmutableArray(Of BoundExpression).Empty,
                    Nothing,
                    type:=baseGetSym.ReturnType,
                    suppressObjectClone:=True)
            Else
                ' not overriding property operates with field
                field = backingField
                fieldAccess = New BoundFieldAccess(syntax, meReference, field, True, field.Type)
            End If

            Dim exitLabel = New GeneratedLabelSymbol("exit")

            Dim statements As ArrayBuilder(Of BoundStatement) = ArrayBuilder(Of BoundStatement).GetInstance
            Dim locals As ImmutableArray(Of LocalSymbol)
            Dim returnLocal As BoundLocal

            If accessor.MethodKind = MethodKind.PropertyGet Then
                ' Declare local variable for function return.
                Dim local = New SynthesizedLocal(accessor, accessor.ReturnType, SynthesizedLocalKind.LoweringTemp)

                Dim returnValue As BoundExpression
                If isOverride Then
                    returnValue = baseGet
                Else
                    returnValue = fieldAccess.MakeRValue()
                End If

                statements.Add(New BoundReturnStatement(syntax, returnValue, local, exitLabel).MakeCompilerGenerated())

                locals = ImmutableArray.Create(Of LocalSymbol)(local)
                returnLocal = New BoundLocal(syntax, local, isLValue:=False, type:=local.Type)
            Else
                Debug.Assert(accessor.MethodKind = MethodKind.PropertySet)

                ' NOTE: at this point number of parameters in a VALID property must be 1.
                '       In the case when an auto-property has some parameters we assume that 
                '       ERR_AutoPropertyCantHaveParams(36759) is already generated,
                '       in this case we just ignore all the parameters and assume that the 
                '       last parameter is what we need to use below
                Debug.Assert(accessor.ParameterCount >= 1)
                Dim parameter = accessor.Parameters(accessor.ParameterCount - 1)
                Dim parameterAccess = New BoundParameter(syntax, parameter, isLValue:=False, type:=parameter.Type)

                Dim eventsToHookup As ArrayBuilder(Of ValueTuple(Of EventSymbol, PropertySymbol)) = Nothing

                ' contains temps for handler delegates followed by other stuff that is needed.
                ' so it will have at least eventsToHookup.Count temps
                Dim temps As ArrayBuilder(Of LocalSymbol) = Nothing
                ' accesses to the handler delegates
                ' we use them once to unhook from old source and then again to hook to the new source
                Dim handlerlocalAccesses As ArrayBuilder(Of BoundLocal) = Nothing

                ' //process Handles that need to be hooked up in this method
                ' //if there are events to hook up, the body will look like this:
                '
                ' Dim tempHandlerLocal = AddressOf handlerMethod   ' addressOf is already bound and may contain conversion
                ' . . .
                ' Dim tempHandlerLocalN = AddressOf handlerMethodN   
                '
                ' Dim valueTemp = [ _backingField | BaseGet ] 
                ' If valueTemp isnot nothing
                '
                '       // unhook handlers from the old value. 
                '       // Note that we can use the handler temps we have just created. 
                '       // Delegate identity is {target, method} so that will work
                '
                '       valueTemp.E1.Remove(tempLocalHandler1)
                '       valueTemp.E2.Remove(tempLocalHandler2)
                '
                ' End If
                '
                ' //Now store the new value
                '
                ' [ _backingField = value | BaseSet(value) ]
                ' 
                ' // re-read the value (we use same assignment here as before)
                ' valueTemp = [ _backingField | BaseGet ] 
                '
                ' If valueTemp isnot nothing
                '
                '       // re-hook handlers to the new value. 
                '
                '       valueTemp.E1.Add(tempLocalHandler1)
                '       valueTemp.E2.Add(tempLocalHandler2)
                '
                ' End If
                '
                If propertySymbol.IsWithEvents Then
                    For Each member In accessor.ContainingType.GetMembers()
                        If member.Kind = SymbolKind.Method Then
                            Dim methodMember = DirectCast(member, MethodSymbol)

                            Dim handledEvents = methodMember.HandledEvents

                            ' if method has definition and implementation parts
                            ' their "Handles" should be merged.
                            If methodMember.IsPartial Then
                                Dim implementationPart = methodMember.PartialImplementationPart
                                If implementationPart IsNot Nothing Then
                                    handledEvents = handledEvents.Concat(implementationPart.HandledEvents)
                                Else
                                    ' partial methods with no implementation do not handle anything
                                    Continue For
                                End If
                            End If

                            If Not handledEvents.IsEmpty Then
                                For Each handledEvent In handledEvents
                                    If handledEvent.hookupMethod = accessor Then
                                        If eventsToHookup Is Nothing Then
                                            eventsToHookup = ArrayBuilder(Of ValueTuple(Of EventSymbol, PropertySymbol)).GetInstance
                                            temps = ArrayBuilder(Of LocalSymbol).GetInstance
                                            handlerlocalAccesses = ArrayBuilder(Of BoundLocal).GetInstance
                                        End If

                                        eventsToHookup.Add(New ValueTuple(Of EventSymbol, PropertySymbol)(
                                                           DirectCast(handledEvent.EventSymbol, EventSymbol),
                                                           DirectCast(handledEvent.WithEventsSourceProperty, PropertySymbol)))
                                        Dim handlerLocal = New SynthesizedLocal(accessor, handledEvent.delegateCreation.Type, SynthesizedLocalKind.LoweringTemp)
                                        temps.Add(handlerLocal)

                                        Dim localAccess = New BoundLocal(syntax, handlerLocal, handlerLocal.Type)
                                        handlerlocalAccesses.Add(localAccess.MakeRValue())

                                        Dim handlerLocalinit = New BoundExpressionStatement(
                                                               syntax,
                                                               New BoundAssignmentOperator(
                                                                   syntax,
                                                                   localAccess,
                                                                   handledEvent.delegateCreation,
                                                                   False,
                                                                   localAccess.Type))

                                        statements.Add(handlerLocalinit)

                                    End If
                                Next
                            End If
                        End If
                    Next
                End If

                Dim withEventsLocalAccess As BoundLocal = Nothing
                Dim withEventsLocalStore As BoundExpressionStatement = Nothing

                ' need to unhook old handlers before setting a new event source
                If eventsToHookup IsNot Nothing Then
                    Dim withEventsValue As BoundExpression
                    If isOverride Then
                        withEventsValue = baseGet
                    Else
                        withEventsValue = fieldAccess.MakeRValue()
                    End If

                    Dim withEventsLocal = New SynthesizedLocal(accessor, withEventsValue.Type, SynthesizedLocalKind.LoweringTemp)
                    temps.Add(withEventsLocal)

                    withEventsLocalAccess = New BoundLocal(syntax, withEventsLocal, withEventsLocal.Type)
                    withEventsLocalStore = New BoundExpressionStatement(
                        syntax,
                        New BoundAssignmentOperator(
                            syntax,
                            withEventsLocalAccess,
                            withEventsValue,
                            True,
                            withEventsLocal.Type))

                    statements.Add(withEventsLocalStore)

                    ' if witheventsLocalStore isnot nothing
                    '           ...
                    '           withEventsLocalAccess.eventN_remove(handlerLocalN)
                    '           ...
                    Dim eventRemovals = ArrayBuilder(Of BoundStatement).GetInstance
                    For i As Integer = 0 To eventsToHookup.Count - 1
                        Dim eventSymbol As EventSymbol = eventsToHookup(i).Item1
                        ' Normally, we would synthesize lowered bound nodes, but we know that these nodes will
                        ' be run through the LocalRewriter.  Let the LocalRewriter handle the special code for
                        ' WinRT events.
                        Dim withEventsProviderAccess As BoundExpression = withEventsLocalAccess

                        Dim providerProperty = eventsToHookup(i).Item2
                        If providerProperty IsNot Nothing Then
                            withEventsProviderAccess = New BoundPropertyAccess(syntax,
                                                                               providerProperty,
                                                                               Nothing,
                                                                               PropertyAccessKind.Get,
                                                                               False,
                                                                               If(providerProperty.IsShared, Nothing, withEventsLocalAccess),
                                                                               ImmutableArray(Of BoundExpression).Empty)
                        End If

                        eventRemovals.Add(
                            New BoundRemoveHandlerStatement(
                                syntax:=syntax,
                                eventAccess:=New BoundEventAccess(syntax, withEventsProviderAccess, eventSymbol, eventSymbol.Type),
                                handler:=handlerlocalAccesses(i)))
                    Next

                    Dim removalStatement = New BoundStatementList(syntax, eventRemovals.ToImmutableAndFree)

                    Dim conditionalRemoval = New BoundIfStatement(
                                             syntax,
                                             (New BoundBinaryOperator(
                                                 syntax,
                                                 BinaryOperatorKind.IsNot,
                                                 withEventsLocalAccess.MakeRValue(),
                                                 New BoundLiteral(syntax, ConstantValue.Nothing,
                                                                  accessor.ContainingAssembly.GetSpecialType(SpecialType.System_Object)),
                                                 False,
                                                 accessor.ContainingAssembly.GetSpecialType(SpecialType.System_Boolean))).MakeCompilerGenerated,
                                             removalStatement,
                                             Nothing)

                    statements.Add(conditionalRemoval.MakeCompilerGenerated)
                End If

                ' set the value of the property
                ' if it is overriding, call the base
                ' otherwise assign to associated field.
                Dim valueSettingExpression As BoundExpression
                If isOverride Then
                    Dim baseSet = accessor.OverriddenMethod
                    valueSettingExpression = New BoundCall(
                        syntax,
                        baseSet,
                        Nothing,
                        myBaseReference,
                        ImmutableArray.Create(Of BoundExpression)(parameterAccess),
                        Nothing,
                        suppressObjectClone:=True,
                        type:=baseSet.ReturnType)
                Else
                    valueSettingExpression = New BoundAssignmentOperator(
                        syntax,
                        fieldAccess,
                        parameterAccess,
                        suppressObjectClone:=False,
                        type:=propertySymbol.Type)
                End If

                statements.Add(
                    (New BoundExpressionStatement(
                        syntax,
                        valueSettingExpression).MakeCompilerGenerated()))

                ' after setting new event source, hookup handlers
                If eventsToHookup IsNot Nothing Then
                    statements.Add(withEventsLocalStore)

                    ' if witheventsLocalStore isnot nothing
                    '           ...
                    '           withEventsLocalAccess.eventN_add(handlerLocalN)
                    '           ...
                    Dim eventAdds = ArrayBuilder(Of BoundStatement).GetInstance
                    For i As Integer = 0 To eventsToHookup.Count - 1
                        Dim eventSymbol As EventSymbol = eventsToHookup(i).Item1
                        ' Normally, we would synthesize lowered bound nodes, but we know that these nodes will
                        ' be run through the LocalRewriter.  Let the LocalRewriter handle the special code for
                        ' WinRT events.
                        Dim withEventsProviderAccess As BoundExpression = withEventsLocalAccess
                        Dim providerProperty = eventsToHookup(i).Item2
                        If providerProperty IsNot Nothing Then
                            withEventsProviderAccess = New BoundPropertyAccess(syntax,
                                                                               providerProperty,
                                                                               Nothing,
                                                                               PropertyAccessKind.Get,
                                                                               False,
                                                                               If(providerProperty.IsShared, Nothing, withEventsLocalAccess),
                                                                               ImmutableArray(Of BoundExpression).Empty)
                        End If

                        eventAdds.Add(
                            New BoundAddHandlerStatement(
                                syntax:=syntax,
                                eventAccess:=New BoundEventAccess(syntax, withEventsProviderAccess, eventSymbol, eventSymbol.Type),
                                handler:=handlerlocalAccesses(i)))
                    Next

                    Dim addStatement = New BoundStatementList(syntax, eventAdds.ToImmutableAndFree())

                    Dim conditionalAdd = New BoundIfStatement(
                                             syntax,
                                             (New BoundBinaryOperator(
                                                 syntax,
                                                 BinaryOperatorKind.IsNot,
                                                 withEventsLocalAccess.MakeRValue(),
                                                 New BoundLiteral(syntax, ConstantValue.Nothing,
                                                                  accessor.ContainingAssembly.GetSpecialType(SpecialType.System_Object)),
                                                 False,
                                                 accessor.ContainingAssembly.GetSpecialType(SpecialType.System_Boolean))).MakeCompilerGenerated,
                                             addStatement,
                                             Nothing)

                    statements.Add(conditionalAdd.MakeCompilerGenerated)
                End If

                locals = If(temps Is Nothing, ImmutableArray(Of LocalSymbol).Empty, temps.ToImmutableAndFree)
                returnLocal = Nothing

                If eventsToHookup IsNot Nothing Then
                    eventsToHookup.Free()
                    handlerlocalAccesses.Free()
                End If
            End If

            statements.Add((New BoundLabelStatement(syntax, exitLabel)).MakeCompilerGenerated())
            statements.Add((New BoundReturnStatement(syntax, returnLocal, Nothing, Nothing)).MakeCompilerGenerated())

            Return (New BoundBlock(syntax, Nothing, locals, statements.ToImmutableAndFree())).MakeCompilerGenerated()
        End Function

    End Module

End Namespace
