' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports System.Runtime.InteropServices
Imports Microsoft.CodeAnalysis.CodeGen
Imports Microsoft.CodeAnalysis.PooledObjects
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols

Namespace Microsoft.CodeAnalysis.VisualBasic

    ''' <summary>
    ''' A bound node rewriter that rewrites types properly (which in some cases the automatically-generated).
    ''' This is used in the lambda rewriter, the iterator rewriter, and the async rewriter.
    ''' </summary>
    Partial Friend MustInherit Class MethodToClassRewriter(Of TProxy)
        Inherits BoundTreeRewriterWithStackGuard

        ''' <summary>
        ''' For each captured variable, the corresponding field of its frame
        ''' </summary>
        Protected ReadOnly Proxies As Dictionary(Of Symbol, TProxy) = New Dictionary(Of Symbol, TProxy)()

        ''' <summary>
        ''' A mapping from every local variable to its replacement local variable. Local variables
        ''' are replaced when their types change due to being inside of a lambda within a generic method.
        ''' </summary>
        Protected ReadOnly LocalMap As Dictionary(Of LocalSymbol, LocalSymbol) = New Dictionary(Of LocalSymbol, LocalSymbol)(ReferenceEqualityComparer.Instance)

        ''' <summary>
        ''' A mapping from every parameter to its replacement parameter. Local variables
        ''' are replaced when their types change due to being inside of a lambda.
        ''' </summary>
        Protected ReadOnly ParameterMap As Dictionary(Of ParameterSymbol, ParameterSymbol) = New Dictionary(Of ParameterSymbol, ParameterSymbol)(ReferenceEqualityComparer.Instance)

        Protected ReadOnly PlaceholderReplacementMap As New Dictionary(Of BoundValuePlaceholderBase, BoundExpression)

        ''' <summary>
        ''' The mapping of type parameters for the current lambda body
        ''' </summary>
        Protected MustOverride ReadOnly Property TypeMap As TypeSubstitution

        Friend MustOverride Function FramePointer(syntax As SyntaxNode, frameClass As NamedTypeSymbol) As BoundExpression

        ''' <summary>
        ''' The method (e.g. lambda) which is currently being rewritten. If we are
        ''' rewriting a lambda, currentMethod is the new generated method.
        ''' </summary>
        Protected MustOverride ReadOnly Property CurrentMethod As MethodSymbol

        Protected MustOverride ReadOnly Property TopLevelMethod As MethodSymbol

        Protected MustOverride ReadOnly Property IsInExpressionLambda As Boolean

        ''' <summary>
        ''' A not-null collection of synthesized methods generated for the current source type.
        ''' </summary>
        Protected ReadOnly CompilationState As TypeCompilationState

        Protected ReadOnly Diagnostics As BindingDiagnosticBag
        Protected ReadOnly SlotAllocatorOpt As VariableSlotAllocator

        ''' <summary>
        ''' During rewriting, we ignore locals that have already been rewritten to a proxy (a field on a closure class).
        ''' However, in the EE, we need to preserve the original slots for all locals (slots for any new locals must be
        ''' appended after the originals).  The <see cref="PreserveOriginalLocals"/> field is intended to suppress any
        ''' rewriter logic that would result in original locals being omitted.
        ''' </summary>
        Protected ReadOnly PreserveOriginalLocals As Boolean

        Protected Sub New(slotAllocatorOpt As VariableSlotAllocator, compilationState As TypeCompilationState, diagnostics As BindingDiagnosticBag, preserveOriginalLocals As Boolean)
            Debug.Assert(compilationState IsNot Nothing)
            Debug.Assert(diagnostics.AccumulatesDiagnostics)
            Me.CompilationState = compilationState
            Me.Diagnostics = diagnostics
            Me.SlotAllocatorOpt = slotAllocatorOpt
            Me.PreserveOriginalLocals = preserveOriginalLocals
        End Sub

#Region "Visitors"

        Public Overrides Function VisitLocalDeclaration(node As BoundLocalDeclaration) As BoundNode
            Dim localSymbol = node.LocalSymbol

            Dim proxy As TProxy = Nothing

            If Proxies.TryGetValue(localSymbol, proxy) Then
                ' Constant locals are never captured.
                Debug.Assert(Not localSymbol.IsConst)
                Return Nothing
            End If

            Return node
        End Function

        Public NotOverridable Overrides Function VisitType(type As TypeSymbol) As TypeSymbol
            If type Is Nothing Then
                Return type
            End If

            Return type.InternalSubstituteTypeParameters(Me.TypeMap).Type
        End Function

        Public NotOverridable Overrides Function VisitMethodInfo(node As BoundMethodInfo) As BoundNode
            Return node.Update(VisitMethodSymbol(node.Method), VisitMethodSymbol(node.GetMethodFromHandle), VisitType(node.Type))
        End Function

        Public Overrides Function VisitPropertyAccess(node As BoundPropertyAccess) As BoundNode
            ' NOTE: this is only reachable from Lambda rewriter
            ' NOTE: in case property access is inside expression tree
            Dim rewrittenPropertySymbol = VisitPropertySymbol(node.PropertySymbol)
            Dim rewrittenReceiver = DirectCast(Visit(node.ReceiverOpt), BoundExpression)

            Dim arguments As ImmutableArray(Of BoundExpression) = node.Arguments
            Dim newArguments(arguments.Length - 1) As BoundExpression
            For i = 0 To arguments.Length - 1
                newArguments(i) = DirectCast(Visit(arguments(i)), BoundExpression)
            Next

            Return node.Update(rewrittenPropertySymbol,
                               Nothing,
                               node.AccessKind,
                               isWriteable:=node.IsWriteable,
                               isLValue:=node.IsLValue,
                               receiverOpt:=rewrittenReceiver,
                               arguments:=newArguments.AsImmutableOrNull,
                               defaultArguments:=node.DefaultArguments,
                               type:=VisitType(node.Type))
        End Function

        Public Overrides Function VisitCall(node As BoundCall) As BoundNode
            Dim receiverOpt As BoundExpression = node.ReceiverOpt
            Dim rewrittenReceiverOpt As BoundExpression = DirectCast(Visit(receiverOpt), BoundExpression)
            Dim newMethod As MethodSymbol = node.Method
            Dim arguments As ImmutableArray(Of BoundExpression) = VisitList(node.Arguments)
            Dim type As TypeSymbol = VisitType(node.Type)

            If ShouldRewriteMethodSymbol(receiverOpt, rewrittenReceiverOpt, newMethod) Then
                Dim methodBeingCalled As MethodSymbol = SubstituteMethodForMyBaseOrMyClassCall(receiverOpt, node.Method)
                newMethod = VisitMethodSymbol(methodBeingCalled)
            End If

            Return node.Update(newMethod,
                               Nothing,
                               rewrittenReceiverOpt,
                               arguments,
                               node.DefaultArguments,
                               node.ConstantValueOpt,
                               isLValue:=node.IsLValue,
                               suppressObjectClone:=node.SuppressObjectClone,
                               type:=type)
        End Function

        Private Function ShouldRewriteMethodSymbol(originalReceiver As BoundExpression, rewrittenReceiverOpt As BoundExpression, newMethod As MethodSymbol) As Boolean
            Return originalReceiver IsNot rewrittenReceiverOpt OrElse
                   Not newMethod.IsDefinition OrElse
                   (Me.TypeMap IsNot Nothing AndAlso Me.TypeMap.TargetGenericDefinition.Equals(newMethod)) OrElse
                   (Me.IsInExpressionLambda AndAlso rewrittenReceiverOpt IsNot Nothing AndAlso
                        (rewrittenReceiverOpt.IsMyClassReference OrElse rewrittenReceiverOpt.IsMyBaseReference))
        End Function

        Public NotOverridable Overrides Function VisitParameter(node As BoundParameter) As BoundNode
            Dim proxy As TProxy = Nothing
            If Proxies.TryGetValue(node.ParameterSymbol, proxy) Then
                Return Me.MaterializeProxy(node, proxy)
            End If

            Dim replacementParameter As ParameterSymbol = Nothing
            If Me.ParameterMap.TryGetValue(node.ParameterSymbol, replacementParameter) Then
                Return New BoundParameter(node.Syntax, replacementParameter, node.IsLValue, replacementParameter.Type, node.HasErrors)
            End If

            Return MyBase.VisitParameter(node)
        End Function

        Protected MustOverride Function MaterializeProxy(origExpression As BoundExpression, proxy As TProxy) As BoundNode

        Public NotOverridable Overrides Function VisitLocal(node As BoundLocal) As BoundNode
            Dim local As LocalSymbol = node.LocalSymbol

            If local.IsConst Then
                ' Local constants are never captured
                Return MyBase.VisitLocal(node)
            End If

            Dim proxy As TProxy = Nothing
            If Proxies.TryGetValue(local, proxy) Then
                Return Me.MaterializeProxy(node, proxy)
            End If

            Dim replacementLocal As LocalSymbol = Nothing
            If Me.LocalMap.TryGetValue(local, replacementLocal) Then
                Return New BoundLocal(node.Syntax, replacementLocal, node.IsLValue, replacementLocal.Type, node.HasErrors)
            End If

            Return MyBase.VisitLocal(node)
        End Function

        Public Overrides Function VisitFieldInfo(node As BoundFieldInfo) As BoundNode
            Return node.Update(VisitFieldSymbol(node.Field), VisitType(node.Type))
        End Function

        Public Overrides Function VisitFieldAccess(node As BoundFieldAccess) As BoundNode
            Return node.Update(DirectCast(Visit(node.ReceiverOpt), BoundExpression),
                               VisitFieldSymbol(node.FieldSymbol),
                               node.IsLValue,
                               node.SuppressVirtualCalls,
                               constantsInProgressOpt:=Nothing,
                               VisitType(node.Type))
        End Function

        Public Overrides Function VisitDelegateCreationExpression(node As BoundDelegateCreationExpression) As BoundNode
            Debug.Assert(node.RelaxationLambdaOpt Is Nothing AndAlso node.RelaxationReceiverPlaceholderOpt Is Nothing)

            Dim rewritten = DirectCast(MyBase.VisitDelegateCreationExpression(node), BoundDelegateCreationExpression)
            Dim newMethod As MethodSymbol = rewritten.Method
            Dim rewrittenReceiver As BoundExpression = rewritten.ReceiverOpt

            If ShouldRewriteMethodSymbol(node.ReceiverOpt, rewrittenReceiver, newMethod) Then
                Dim methodBeingCalled As MethodSymbol = SubstituteMethodForMyBaseOrMyClassCall(node.ReceiverOpt, node.Method)
                newMethod = VisitMethodSymbol(methodBeingCalled)
            End If

            Return node.Update(
                rewrittenReceiver,
                newMethod,
                rewritten.RelaxationLambdaOpt,
                rewritten.RelaxationReceiverPlaceholderOpt,
                methodGroupOpt:=Nothing,
                type:=rewritten.Type)
        End Function

        Public Overrides Function VisitObjectCreationExpression(node As BoundObjectCreationExpression) As BoundNode
            Dim rewritten = DirectCast(MyBase.VisitObjectCreationExpression(node), BoundObjectCreationExpression)

            Dim constructor = rewritten.ConstructorOpt
            If constructor IsNot Nothing Then
                If node.Type IsNot rewritten.Type OrElse Not constructor.IsDefinition Then
                    Dim newConstructor = VisitMethodSymbol(constructor)
                    rewritten = node.Update(
                        newConstructor,
                        rewritten.Arguments,
                        rewritten.DefaultArguments,
                        rewritten.InitializerOpt,
                        rewritten.Type)
                End If
            End If

            Return rewritten
        End Function

        ''' <summary>
        ''' Rewrites method.
        ''' </summary>
        Private Function VisitMethodSymbol(method As MethodSymbol) As MethodSymbol
            Dim substitution As TypeSubstitution = Me.TypeMap

            If substitution IsNot Nothing Then
                Dim newMethod As MethodSymbol = method.OriginalDefinition

                Dim newContainer As TypeSymbol = method.ContainingType.InternalSubstituteTypeParameters(substitution).AsTypeSymbolOnly()
                Dim substitutedContainer = TryCast(newContainer, SubstitutedNamedType)
                If substitutedContainer IsNot Nothing Then
                    newMethod = DirectCast(substitutedContainer.GetMemberForDefinition(newMethod), MethodSymbol)
                Else
                    Dim anonymousContainer = TryCast(newContainer, AnonymousTypeManager.AnonymousTypeOrDelegatePublicSymbol)
                    If anonymousContainer IsNot Nothing Then
                        newMethod = anonymousContainer.FindSubstitutedMethodSymbol(newMethod)
                    End If
                End If

                If newMethod.IsGenericMethod Then
                    Dim typeArgs = method.TypeArguments
                    Dim visitedTypeArgs(typeArgs.Length - 1) As TypeSymbol
                    For i = 0 To typeArgs.Length - 1
                        visitedTypeArgs(i) = VisitType(typeArgs(i))
                    Next
                    newMethod = newMethod.Construct(visitedTypeArgs)
                End If

                Return newMethod
            End If

            Return method
        End Function

        ''' <summary>
        ''' Rewrites property.
        ''' </summary>
        Private Function VisitPropertySymbol([property] As PropertySymbol) As PropertySymbol
            Dim substitution As TypeSubstitution = Me.TypeMap

            If substitution IsNot Nothing Then
                Dim newProperty As PropertySymbol = [property].OriginalDefinition

                Dim newContainer As TypeSymbol = [property].ContainingType.InternalSubstituteTypeParameters(substitution).AsTypeSymbolOnly()
                Dim substitutedContainer = TryCast(newContainer, SubstitutedNamedType)
                If substitutedContainer IsNot Nothing Then
                    newProperty = DirectCast(substitutedContainer.GetMemberForDefinition(newProperty), PropertySymbol)
                Else
                    Dim anonymousContainer = TryCast(newContainer, AnonymousTypeManager.AnonymousTypePublicSymbol)
                    If anonymousContainer IsNot Nothing Then
                        Dim anonProperty = TryCast(newProperty, AnonymousTypeManager.AnonymousTypePropertyPublicSymbol)
                        newProperty = anonymousContainer.Properties(anonProperty.PropertyIndex)
                    End If
                End If

                Return newProperty
            End If

            Return [property]
        End Function

        ''' <summary>
        ''' Rewrites field.
        ''' </summary>
        Friend Function VisitFieldSymbol(field As FieldSymbol) As FieldSymbol
            Dim substitution As TypeSubstitution = Me.TypeMap

            If substitution IsNot Nothing Then
                Dim newField As FieldSymbol = field.OriginalDefinition

                Dim newContainer As TypeSymbol = field.ContainingType.InternalSubstituteTypeParameters(substitution).AsTypeSymbolOnly()
                Dim substitutedContainer = TryCast(newContainer, SubstitutedNamedType)
                If substitutedContainer IsNot Nothing Then
                    newField = DirectCast(substitutedContainer.GetMemberForDefinition(newField), FieldSymbol)
                End If

                Return newField
            End If

            Return field
        End Function

        Public Overrides Function VisitBlock(node As BoundBlock) As BoundNode
            Return RewriteBlock(node)
        End Function

        Public Overrides Function VisitSequence(node As BoundSequence) As BoundNode
            Return RewriteSequence(node)
        End Function

        Public MustOverride Overrides Function VisitCatchBlock(node As BoundCatchBlock) As BoundNode

        Protected Function RewriteBlock(node As BoundBlock,
                                        prologue As ArrayBuilder(Of BoundExpression),
                                        newLocals As ArrayBuilder(Of LocalSymbol)) As BoundBlock

            For Each v In node.Locals
                If Me.PreserveOriginalLocals OrElse Not Me.Proxies.ContainsKey(v) Then
                    Dim vType = VisitType(v.Type)
                    If TypeSymbol.Equals(vType, v.Type, TypeCompareKind.ConsiderEverything) Then

                        Dim replacement As LocalSymbol = Nothing
                        Dim wasReplaced As Boolean = False
                        If Not LocalMap.TryGetValue(v, replacement) Then
                            replacement = CreateReplacementLocalOrReturnSelf(v, vType, onlyReplaceIfFunctionValue:=True, wasReplaced:=wasReplaced)
                        End If

                        If wasReplaced Then
                            LocalMap.Add(v, replacement)
                        End If

                        newLocals.Add(replacement)
                    Else
                        Dim replacement As LocalSymbol = CreateReplacementLocalOrReturnSelf(v, vType)
                        newLocals.Add(replacement)
                        LocalMap.Add(v, replacement)
                    End If
                End If
            Next

            Dim newStatements = ArrayBuilder(Of BoundStatement).GetInstance
            Dim start As Integer = 0
            Dim nodeStatements = node.Statements

            If prologue.Count > 0 Then
                ' Add hidden sequence point, prologue doesn't map to source, but if
                ' the first statement in the block is a non-hidden sequence point
                ' (for example, sequence point for a method block), keep it first.
                If nodeStatements.Length > 0 AndAlso nodeStatements(0).Syntax IsNot Nothing Then
                    Dim keepSequencePointFirst As Boolean = False

                    Select Case nodeStatements(0).Kind
                        Case BoundKind.SequencePoint
                            Dim sp = DirectCast(nodeStatements(0), BoundSequencePoint)
                            keepSequencePointFirst = sp.StatementOpt Is Nothing
                        Case BoundKind.SequencePointWithSpan
                            Dim sp = DirectCast(nodeStatements(0), BoundSequencePointWithSpan)
                            keepSequencePointFirst = sp.StatementOpt Is Nothing
                    End Select

                    If keepSequencePointFirst Then
                        Dim replacement = DirectCast(Me.Visit(nodeStatements(0)), BoundStatement)
                        If replacement IsNot Nothing Then
                            newStatements.Add(replacement)
                        End If

                        start = 1
                    End If
                End If

                newStatements.Add(New BoundSequencePoint(Nothing, Nothing).MakeCompilerGenerated)
            End If

            For Each expr In prologue
                newStatements.Add(New BoundExpressionStatement(expr.Syntax, expr))
            Next

            ' done with this
            prologue.Free()

            For i As Integer = start To nodeStatements.Length - 1
                Dim replacement = DirectCast(Me.Visit(nodeStatements(i)), BoundStatement)
                If replacement IsNot Nothing Then
                    newStatements.Add(replacement)
                End If
            Next

            'TODO: we may not need to update if there was nothing to rewrite.
            Return node.Update(node.StatementListSyntax, newLocals.ToImmutableAndFree(), newStatements.ToImmutableAndFree())
        End Function

        Protected Function RewriteBlock(node As BoundBlock) As BoundBlock
            Dim prologue = ArrayBuilder(Of BoundExpression).GetInstance
            Dim newLocals = ArrayBuilder(Of LocalSymbol).GetInstance

            Return RewriteBlock(node, prologue, newLocals)
        End Function

        Protected Shared Function CreateReplacementLocalOrReturnSelf(
            originalLocal As LocalSymbol,
            newType As TypeSymbol,
            Optional onlyReplaceIfFunctionValue As Boolean = False,
            <Out()> Optional ByRef wasReplaced As Boolean = False
        ) As LocalSymbol

            If Not onlyReplaceIfFunctionValue OrElse originalLocal.IsFunctionValue Then

                wasReplaced = True
                Return LocalSymbol.Create(originalLocal, newType)
            Else
                wasReplaced = False
                Return originalLocal
            End If
        End Function

        Protected Function RewriteSequence(node As BoundSequence) As BoundSequence
            Dim prologue = ArrayBuilder(Of BoundExpression).GetInstance
            Dim newLocals = ArrayBuilder(Of LocalSymbol).GetInstance

            Return RewriteSequence(node, prologue, newLocals)
        End Function

        Protected Function RewriteSequence(node As BoundSequence,
                                           prologue As ArrayBuilder(Of BoundExpression),
                                           newLocals As ArrayBuilder(Of LocalSymbol)) As BoundSequence

            Dim origLocals = node.Locals

            ' merge locals new and rewritten original
            For Each v In origLocals
                If Not Me.Proxies.ContainsKey(v) Then
                    Dim vType = VisitType(v.Type)
                    If TypeSymbol.Equals(vType, v.Type, TypeCompareKind.ConsiderEverything) Then
                        newLocals.Add(v)
                    Else
                        Dim replacement = CreateReplacementLocalOrReturnSelf(v, vType)
                        newLocals.Add(replacement)
                        LocalMap.Add(v, replacement)
                    End If
                End If
            Next

            ' merge side-effect - prologue followed by rewritten original side-effect
            For Each s In node.SideEffects
                Dim replacement = DirectCast(Me.Visit(s), BoundExpression)
                If replacement IsNot Nothing Then
                    prologue.Add(replacement)
                End If
            Next

            Debug.Assert(node.ValueOpt IsNot Nothing OrElse node.HasErrors OrElse node.Type.SpecialType = SpecialType.System_Void)
            Dim newValue = DirectCast(Me.Visit(node.ValueOpt), BoundExpression)

            Return node.Update(newLocals.ToImmutableAndFree(), prologue.ToImmutableAndFree(), newValue, If(newValue Is Nothing, node.Type, newValue.Type))
        End Function

        Public Overrides Function VisitRValuePlaceholder(node As BoundRValuePlaceholder) As BoundNode
            Return PlaceholderReplacementMap(node)
        End Function

        Public Overrides Function VisitLValuePlaceholder(node As BoundLValuePlaceholder) As BoundNode
            Return PlaceholderReplacementMap(node)
        End Function

        Public Overrides Function VisitAwaitOperator(node As BoundAwaitOperator) As BoundNode
            Dim awaitablePlaceholder As BoundRValuePlaceholder = node.AwaitableInstancePlaceholder
            PlaceholderReplacementMap.Add(awaitablePlaceholder,
                                          awaitablePlaceholder.Update(VisitType(awaitablePlaceholder.Type)))

            Dim awaiterPlaceholder As BoundLValuePlaceholder = node.AwaiterInstancePlaceholder
            PlaceholderReplacementMap.Add(awaiterPlaceholder,
                                          awaiterPlaceholder.Update(VisitType(awaiterPlaceholder.Type)))

            Dim result As BoundNode = MyBase.VisitAwaitOperator(node)

            PlaceholderReplacementMap.Remove(awaitablePlaceholder)
            PlaceholderReplacementMap.Remove(awaiterPlaceholder)

            Return result
        End Function

        Public Overrides Function VisitSelectStatement(node As BoundSelectStatement) As BoundNode
            Dim placeholder As BoundRValuePlaceholder = node.ExprPlaceholderOpt
            If placeholder IsNot Nothing Then
                PlaceholderReplacementMap.Add(placeholder,
                                              placeholder.Update(VisitType(placeholder.Type)))
            End If

            Dim result As BoundNode = MyBase.VisitSelectStatement(node)

            If placeholder IsNot Nothing Then
                PlaceholderReplacementMap.Remove(placeholder)
            End If
            Return result
        End Function

        Public Overrides Function VisitUserDefinedShortCircuitingOperator(node As BoundUserDefinedShortCircuitingOperator) As BoundNode
            Dim leftOperandPlaceholder As BoundRValuePlaceholder = node.LeftOperandPlaceholder
            PlaceholderReplacementMap.Add(leftOperandPlaceholder,
                                          leftOperandPlaceholder.Update(VisitType(leftOperandPlaceholder.Type)))

            Dim result As BoundNode = MyBase.VisitUserDefinedShortCircuitingOperator(node)

            PlaceholderReplacementMap.Remove(leftOperandPlaceholder)
            Return result
        End Function

#End Region

    End Class
End Namespace
