' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable
Imports System.Diagnostics
Imports Microsoft.CodeAnalysis.PooledObjects
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic
    Partial Friend NotInheritable Class LocalRewriter
        Public Overrides Function VisitObjectCreationExpression(node As BoundObjectCreationExpression) As BoundNode

            ' save the object initializer away to rewrite them later on and set the initializers to nothing to not rewrite them
            ' two times.
            Dim objectInitializer = node.InitializerOpt
            node = node.Update(node.ConstructorOpt, node.Arguments, node.DefaultArguments, Nothing, node.Type)

            Dim ctor = node.ConstructorOpt
            Dim result As BoundExpression = node

            If ctor IsNot Nothing Then
                Dim temporaries As ImmutableArray(Of SynthesizedLocal) = Nothing
                Dim copyBack As ImmutableArray(Of BoundExpression) = Nothing

                result = node.Update(ctor,
                                     RewriteCallArguments(node.Arguments, ctor.Parameters, temporaries, copyBack, False),
                                     node.DefaultArguments,
                                     Nothing,
                                     ctor.ContainingType)

                If Not temporaries.IsDefault Then
                    result = GenerateSequenceValueSideEffects(_currentMethodOrLambda, result, StaticCast(Of LocalSymbol).From(temporaries), copyBack)
                End If

                ' If a coclass was instantiated, convert the class to the interface type.
                If node.Type.IsInterfaceType() Then
                    Debug.Assert(result.Type.Equals(DirectCast(node.Type, NamedTypeSymbol).CoClassType))

                    Dim useSiteDiagnostics As HashSet(Of DiagnosticInfo) = Nothing
                    Dim conv As ConversionKind = Conversions.ClassifyDirectCastConversion(result.Type, node.Type, useSiteDiagnostics)
                    Debug.Assert(Conversions.ConversionExists(conv))
                    _diagnostics.Add(result, useSiteDiagnostics)
                    result = New BoundDirectCast(node.Syntax, result, conv, node.Type, Nothing)
                Else
                    Debug.Assert(node.Type.IsSameTypeIgnoringAll(result.Type))
                End If
            End If

            If objectInitializer IsNot Nothing Then
                Return VisitObjectCreationInitializer(objectInitializer, node, result)
            End If

            Return result
        End Function

        Public Overrides Function VisitNoPiaObjectCreationExpression(node As BoundNoPiaObjectCreationExpression) As BoundNode
            ' For the NoPIA feature, we need to gather the GUID from the coclass, and 
            ' generate the following:
            ' DirectCast(System.Activator.CreateInstance(System.Runtime.InteropServices.Marshal.GetTypeFromCLSID(New Guid(GUID))), IPiaType)
            '
            ' If System.Runtime.InteropServices.Marshal.GetTypeFromCLSID is not available (older framework),
            ' System.Type.GetTypeFromCLSID() is used to get the type for the CLSID.

            Dim factory As New SyntheticBoundNodeFactory(_topMethod, _currentMethodOrLambda, node.Syntax, _compilationState, _diagnostics)

            Dim ctor = factory.WellKnownMember(Of MethodSymbol)(WellKnownMember.System_Guid__ctor)
            Dim newGuid As BoundExpression
            If ctor IsNot Nothing Then
                newGuid = factory.[New](ctor, factory.Literal(node.GuidString))
            Else
                newGuid = New BoundBadExpression(node.Syntax, LookupResultKind.NotCreatable, ImmutableArray(Of Symbol).Empty, ImmutableArray(Of BoundExpression).Empty, ErrorTypeSymbol.UnknownResultType, hasErrors:=True)
            End If

            Dim getTypeFromCLSID = If(factory.WellKnownMember(Of MethodSymbol)(WellKnownMember.System_Runtime_InteropServices_Marshal__GetTypeFromCLSID, isOptional:=True),
                   factory.WellKnownMember(Of MethodSymbol)(WellKnownMember.System_Type__GetTypeFromCLSID))
            Dim callGetTypeFromCLSID As BoundExpression
            If getTypeFromCLSID IsNot Nothing Then
                callGetTypeFromCLSID = factory.Call(Nothing, getTypeFromCLSID, newGuid)
            Else
                callGetTypeFromCLSID = New BoundBadExpression(node.Syntax, LookupResultKind.OverloadResolutionFailure, ImmutableArray(Of Symbol).Empty, ImmutableArray(Of BoundExpression).Empty, ErrorTypeSymbol.UnknownResultType, hasErrors:=True)
            End If

            Dim createInstance = factory.WellKnownMember(Of MethodSymbol)(WellKnownMember.System_Activator__CreateInstance)
            Dim rewrittenObjectCreation As BoundExpression
            If createInstance IsNot Nothing AndAlso Not createInstance.ReturnType.IsErrorType() Then
                Dim useSiteDiagnostics As HashSet(Of DiagnosticInfo) = Nothing
                Dim conversion = Conversions.ClassifyDirectCastConversion(createInstance.ReturnType, node.Type, useSiteDiagnostics)
                _diagnostics.Add(node, useSiteDiagnostics)
                rewrittenObjectCreation = New BoundDirectCast(node.Syntax, factory.Call(Nothing, createInstance, callGetTypeFromCLSID), conversion, node.Type)
            Else
                rewrittenObjectCreation = New BoundBadExpression(node.Syntax, LookupResultKind.OverloadResolutionFailure, ImmutableArray(Of Symbol).Empty, ImmutableArray(Of BoundExpression).Empty, node.Type, hasErrors:=True)
            End If

            If node.InitializerOpt Is Nothing OrElse node.InitializerOpt.HasErrors Then
                Return rewrittenObjectCreation
            End If

            Return VisitObjectCreationInitializer(node.InitializerOpt, node, rewrittenObjectCreation)
        End Function

        Private Function VisitObjectCreationInitializer(
            objectInitializer As BoundObjectInitializerExpressionBase,
            objectCreationExpression As BoundExpression,
            rewrittenObjectCreationExpression As BoundExpression
        ) As BoundNode
            If objectInitializer.Kind = BoundKind.CollectionInitializerExpression Then
                Return RewriteCollectionInitializerExpression(DirectCast(objectInitializer, BoundCollectionInitializerExpression),
                                                              objectCreationExpression, rewrittenObjectCreationExpression)
            Else
                Return RewriteObjectInitializerExpression(DirectCast(objectInitializer, BoundObjectInitializerExpression),
                                                          objectCreationExpression, rewrittenObjectCreationExpression)
            End If
        End Function

        Public Overrides Function VisitNewT(node As BoundNewT) As BoundNode
            ' Unlike C#, "New T()" is always rewritten as "Activator.CreateInstance<T>()",
            ' even if T is known to be a value type or reference type. This matches Dev10 VB.

            If _inExpressionLambda Then
                ' NOTE: If we are in expression lambda, we want to keep BoundNewT 
                ' NOTE: node, but we need to rewrite initializers if any.

                If node.InitializerOpt IsNot Nothing Then
                    Return VisitObjectCreationInitializer(node.InitializerOpt, node, node)
                Else
                    Return node
                End If
            End If

            Dim syntax = node.Syntax
            Dim typeParameter = DirectCast(node.Type, TypeParameterSymbol)

            Dim result As BoundExpression

            Dim method As MethodSymbol = Nothing
            If TryGetWellknownMember(method, WellKnownMember.System_Activator__CreateInstance_T, syntax) Then
                Debug.Assert(method IsNot Nothing)
                method = method.Construct(ImmutableArray.Create(Of TypeSymbol)(typeParameter))

                result = New BoundCall(syntax,
                                       method,
                                       methodGroupOpt:=Nothing,
                                       receiverOpt:=Nothing,
                                       arguments:=ImmutableArray(Of BoundExpression).Empty,
                                       constantValueOpt:=Nothing,
                                       isLValue:=False,
                                       suppressObjectClone:=False,
                                       type:=typeParameter)
            Else
                result = New BoundBadExpression(syntax, LookupResultKind.NotReferencable, ImmutableArray(Of Symbol).Empty, ImmutableArray(Of BoundExpression).Empty, typeParameter, hasErrors:=True)
            End If

            If node.InitializerOpt IsNot Nothing Then
                Return VisitObjectCreationInitializer(node.InitializerOpt, result, result)
            End If

            Return result
        End Function

        ''' <summary>
        ''' Rewrites a CollectionInitializerExpression to a list of Add calls and returns the temporary.
        ''' E.g. the following code:
        '''     Dim x As New CollectionType(param1) From {1, {2, 3}, {4, {5, 6, 7}}}
        ''' gets rewritten to 
        '''     Dim temp as CollectionType 
        '''     temp = new CollectionType(param1)
        '''     temp.Add(1)
        '''     temp.Add(2, 3)
        '''     temp.Add(4, {5, 6, 7})
        '''     x = temp
        ''' where the last assignment is not part of this rewriting, because the BoundCollectionInitializerExpression
        ''' only represents the object creation expression with the initialization.
        ''' </summary>
        ''' <param name="node">The BoundCollectionInitializerExpression that should be rewritten.</param>
        ''' <returns>A bound sequence for the object creation expression containing the invocation expressions.</returns>
        Public Function RewriteCollectionInitializerExpression(
            node As BoundCollectionInitializerExpression,
            objectCreationExpression As BoundExpression,
            rewrittenObjectCreationExpression As BoundExpression
        ) As BoundNode
            Debug.Assert(node.PlaceholderOpt IsNot Nothing)

            Dim expressionType = node.Type

            Dim syntaxNode = node.Syntax
            Dim tempLocalSymbol As LocalSymbol
            Dim tempLocal As BoundLocal
            Dim expressions = ArrayBuilder(Of BoundExpression).GetInstance()
            Dim newPlaceholder As BoundWithLValueExpressionPlaceholder

            If _inExpressionLambda Then
                ' A temp is not needed for this case 
                tempLocalSymbol = Nothing
                tempLocal = Nothing

                ' Simply replace placeholder with a copy, it will be dropped by Expression Tree rewriter. The copy is needed to 
                ' keep the double rewrite tracking happy.
                newPlaceholder = New BoundWithLValueExpressionPlaceholder(node.PlaceholderOpt.Syntax, node.PlaceholderOpt.Type)
                AddPlaceholderReplacement(node.PlaceholderOpt, newPlaceholder)
            Else
                ' Create a temp symbol 
                '    Dim temp as CollectionType

                ' Create assignment for the rewritten object 
                ' creation expression to the temp
                '    temp = new CollectionType(param1)
                tempLocalSymbol = New SynthesizedLocal(Me._currentMethodOrLambda, expressionType, SynthesizedLocalKind.LoweringTemp)
                tempLocal = New BoundLocal(syntaxNode, tempLocalSymbol, expressionType)
                Dim temporaryAssignment = New BoundAssignmentOperator(syntaxNode,
                                                                  tempLocal,
                                                                  GenerateObjectCloneIfNeeded(objectCreationExpression, rewrittenObjectCreationExpression),
                                                                  suppressObjectClone:=True,
                                                                  type:=expressionType)
                expressions.Add(temporaryAssignment)

                newPlaceholder = Nothing
                AddPlaceholderReplacement(node.PlaceholderOpt, tempLocal)
            End If

            Dim initializerCount = node.Initializers.Length

            ' rewrite the invocation expressions and add them to the expression of the sequence
            '    temp.Add(...)
            For initializerIndex = 0 To initializerCount - 1
                ' NOTE: if the method Add(...) is omitted we build a local which
                '       seems to be redundant, this will optimized out later 
                '       by stack scheduler
                Dim initializer As BoundExpression = node.Initializers(initializerIndex)
                If Not IsOmittedBoundCall(initializer) Then
                    expressions.Add(VisitExpressionNode(initializer))
                End If
            Next

            RemovePlaceholderReplacement(node.PlaceholderOpt)

            If _inExpressionLambda Then
                Debug.Assert(tempLocalSymbol Is Nothing)
                Debug.Assert(tempLocal Is Nothing)

                ' NOTE: if inside expression lambda we rewrite the collection initializer 
                ' NOTE: node and attach it back to object creation expression, it will be 
                ' NOTE: rewritten later in ExpressionLambdaRewriter

                ' Rewrite object creation
                Return ReplaceObjectOrCollectionInitializer(
                            rewrittenObjectCreationExpression,
                            node.Update(newPlaceholder,
                                        expressions.ToImmutableAndFree(),
                                        node.Type))
            Else
                Debug.Assert(tempLocalSymbol IsNot Nothing)
                Debug.Assert(tempLocal IsNot Nothing)

                Return New BoundSequence(syntaxNode,
                                     ImmutableArray.Create(Of LocalSymbol)(tempLocalSymbol),
                                     expressions.ToImmutableAndFree(),
                                     tempLocal.MakeRValue(),
                                     expressionType)
            End If
        End Function

        ''' <summary>
        ''' Rewrites a ObjectInitializerExpression to either a statement list (in case the there is no temporary used) or a bound
        ''' sequence expression (in case there is a temporary used). The information whether to use a temporary or not is 
        ''' stored in the bound object member initializer node itself.
        ''' 
        ''' E.g. the following code:
        '''     Dim x = New RefTypeName(param1) With {.FieldName1 = 23, .FieldName2 = .FieldName3, .FieldName4 = x.FieldName1}
        ''' gets rewritten to 
        '''     Dim temp as RefTypeName 
        '''     temp = new RefTypeName(param1)
        '''     temp.FieldName1 = 23
        '''     temp.FieldName2 = temp.FieldName3
        '''     temp.FieldName4 = x.FieldName1
        '''     x = temp
        ''' where the last assignment is not part of this rewriting, because the BoundObjectInitializerExpression
        ''' only represents the object creation expression with the initialization.
        ''' 
        ''' In a case where no temporary is used the following code:
        '''     Dim x As New ValueTypeName(param1) With {.FieldName1 = 23, .FieldName2 = .FieldName3, .FieldName4 = x.FieldName1}
        ''' gets rewritten to 
        '''     x = new ValueTypeName(param1)
        '''     x.FieldName1 = 23
        '''     x.FieldName2 = x.FieldName3
        '''     x.FieldName4 = x.FieldName1
        ''' </summary>
        ''' <param name="node">The BoundObjectInitializerExpression that should be rewritten.</param>
        ''' <returns>A bound sequence for the object creation expression containing the invocation expressions, or a 
        ''' bound statement list if no temporary should be used.</returns>
        Public Function RewriteObjectInitializerExpression(
            node As BoundObjectInitializerExpression,
            objectCreationExpression As BoundExpression,
            rewrittenObjectCreationExpression As BoundExpression
        ) As BoundNode
            Dim targetObjectReference As BoundExpression
            Dim expressionType = node.Type
            Dim initializerCount = node.Initializers.Length
            Dim syntaxNode = node.Syntax
            Dim sequenceType As TypeSymbol
            Dim sequenceTemporaries As ImmutableArray(Of LocalSymbol)
            Dim sequenceValueExpression As BoundExpression

            Debug.Assert(node.PlaceholderOpt IsNot Nothing)

            ' NOTE: If we are in an expression lambda not all object initializers are allowed, essentially
            ' NOTE: everything requiring temp local creation is disabled; this rule is not applicable to 
            ' NOTE: locals that are created and ONLY used on left-hand-side of initializer assignments,
            ' NOTE: ExpressionLambdaRewriter will get rid of them
            ' NOTE: In order ExpressionLambdaRewriter to be able to detect such locals being used on the 
            ' NOTE: *right* side of initializer assignments we rewrite node.PlaceholderOpt into itself

            If node.CreateTemporaryLocalForInitialization Then
                ' create temporary
                '    Dim temp as RefTypeName 
                Dim tempLocalSymbol As LocalSymbol = New SynthesizedLocal(Me._currentMethodOrLambda, expressionType, SynthesizedLocalKind.LoweringTemp)
                sequenceType = expressionType

                sequenceTemporaries = ImmutableArray.Create(Of LocalSymbol)(tempLocalSymbol)

                targetObjectReference = If(_inExpressionLambda,
                                           DirectCast(node.PlaceholderOpt, BoundExpression),
                                           New BoundLocal(syntaxNode, tempLocalSymbol, expressionType))
                sequenceValueExpression = targetObjectReference.MakeRValue()
                AddPlaceholderReplacement(node.PlaceholderOpt, targetObjectReference)
            Else
                ' Get the receiver for the current initialized variable in case of an "AsNew" declaration
                ' this is the only case where there might be no temporary needed.
                ' The replacement for this placeholder was added in VisitAsNewLocalDeclarations.
                targetObjectReference = PlaceholderReplacement(node.PlaceholderOpt)
                sequenceType = GetSpecialType(SpecialType.System_Void)

                sequenceTemporaries = ImmutableArray(Of LocalSymbol).Empty
                sequenceValueExpression = Nothing
            End If

            Dim sequenceExpressions(initializerCount) As BoundExpression

            ' create assignment for object creation expression to temporary or variable declaration
            '    x = new TypeName(...)
            '       or
            '    temp = new TypeName(...)
            sequenceExpressions(0) = New BoundAssignmentOperator(syntaxNode,
                                                                 targetObjectReference,
                                                                 GenerateObjectCloneIfNeeded(objectCreationExpression, rewrittenObjectCreationExpression),
                                                                 suppressObjectClone:=True,
                                                                 type:=expressionType)

            ' rewrite the assignment expressions and add them to the statement list
            '    x.FieldName = value expression
            '        or
            '    temp.FieldName = value expression
            For initializerIndex = 0 To initializerCount - 1
                If _inExpressionLambda Then
                    ' NOTE: Inside expression lambda we rewrite only right-hand-side of the assignments, left part 
                    ' NOTE: will be kept unchanged to make sure we got proper symbol out of it 
                    Dim assignment = DirectCast(node.Initializers(initializerIndex), BoundAssignmentOperator)
                    Debug.Assert(assignment.LeftOnTheRightOpt Is Nothing)
                    sequenceExpressions(initializerIndex + 1) = assignment.Update(assignment.Left,
                                                                                  assignment.LeftOnTheRightOpt,
                                                                                  VisitExpressionNode(assignment.Right),
                                                                                  True,
                                                                                  assignment.Type)
                Else
                    sequenceExpressions(initializerIndex + 1) = VisitExpressionNode(node.Initializers(initializerIndex))
                End If
            Next

            If node.CreateTemporaryLocalForInitialization Then
                RemovePlaceholderReplacement(node.PlaceholderOpt)
            End If

            If _inExpressionLambda Then
                ' when converting object initializer inside expression lambdas we want to keep 
                ' object initializer in object creation expression; we just store visited initializers 
                ' back to the original object initializer and update the original object creation expression

                ' create new initializers
                Dim newInitializers(initializerCount - 1) As BoundExpression
                Dim errors As Boolean = False
                For index = 0 To initializerCount - 1
                    newInitializers(index) = sequenceExpressions(index + 1)
                Next

                ' Rewrite object creation
                Return ReplaceObjectOrCollectionInitializer(
                            rewrittenObjectCreationExpression,
                            node.Update(node.CreateTemporaryLocalForInitialization,
                                        node.Binder,
                                        node.PlaceholderOpt,
                                        newInitializers.AsImmutableOrNull(),
                                        node.Type))
            End If

            Return New BoundSequence(syntaxNode,
                                     sequenceTemporaries,
                                     sequenceExpressions.AsImmutableOrNull,
                                     sequenceValueExpression,
                                     sequenceType)
        End Function

        Private Function ReplaceObjectOrCollectionInitializer(rewrittenObjectCreationExpression As BoundExpression, rewrittenInitializer As BoundObjectInitializerExpressionBase) As BoundExpression
            Select Case rewrittenObjectCreationExpression.Kind
                Case BoundKind.ObjectCreationExpression
                    Dim objCreation = DirectCast(rewrittenObjectCreationExpression, BoundObjectCreationExpression)
                    Return objCreation.Update(objCreation.ConstructorOpt, objCreation.Arguments, objCreation.DefaultArguments, rewrittenInitializer, objCreation.Type)

                Case BoundKind.NewT
                    Dim newT = DirectCast(rewrittenObjectCreationExpression, BoundNewT)
                    Return newT.Update(rewrittenInitializer, newT.Type)

                Case BoundKind.Sequence
                    ' NOTE: is rewrittenObjectCreationExpression is not an object creation expression, it 
                    ' NOTE: was probably wrapped with sequence which means that this case is not supported 
                    ' NOTE: inside expression lambdas.
                    Dim sequence = DirectCast(rewrittenObjectCreationExpression, BoundSequence)
                    Debug.Assert(sequence.ValueOpt IsNot Nothing AndAlso sequence.ValueOpt.Kind = BoundKind.ObjectCreationExpression)
                    Return sequence.Update(sequence.Locals,
                                           sequence.SideEffects,
                                           ReplaceObjectOrCollectionInitializer(sequence.ValueOpt, rewrittenInitializer),
                                           sequence.Type)

                Case Else
                    Throw ExceptionUtilities.UnexpectedValue(rewrittenObjectCreationExpression.Kind)
            End Select
        End Function

    End Class
End Namespace
