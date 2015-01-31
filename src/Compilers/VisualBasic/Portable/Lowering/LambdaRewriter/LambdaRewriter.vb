' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable
Imports Microsoft.CodeAnalysis.CodeGen
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols

Namespace Microsoft.CodeAnalysis.VisualBasic

    ''' <summary>
    ''' The rewriter for removing lambda expressions from method bodies and introducing closure classes
    ''' as containers for captured variables along the lines of the example in section 6.5.3 of the
    ''' C# language specification.
    ''' 
    ''' The entry point is the public method Rewrite.  It operates as follows:
    ''' 
    ''' First, an analysis of the whole method body is performed that determines which variables are
    ''' captured, what their scopes are, and what the nesting relationship is between scopes that
    ''' have captured variables.  The result of this analysis is left in LambdaRewriter.analysis.
    ''' 
    ''' Then we make frame, or compiler-generated class, represented by an instance of
    ''' LambdaRewriter.Frame for each scope with captured variables.  The generated frames are kept
    ''' in LambdaRewriter.frames.  Each frame is given a single field for each captured
    ''' variable in the corresponding scope.  These are are maintained in LambdaRewriter.proxies.
    ''' 
    ''' Finally, we walk and rewrite the input bound tree, keeping track of the following:
    ''' (1) The current set of active frame pointers, in LambdaRewriter.framePointers
    ''' (2) The current method being processed (this changes within a lambda's body), in LambdaRewriter.currentMethod
    ''' (3) The "this" symbol for the current method in LambdaRewriter.currentFrameThis, and
    ''' (4) The symbol that is used to access the innermost frame pointer (it could be a local variable or "this" parameter)
    ''' 
    ''' There are a few key transformations done in the rewriting.
    ''' (1) Lambda expressions are turned into delegate creation expressions, and the body of the lambda is
    '''     moved into a new, compiler-generated method of a selected frame class.
    ''' (2) On entry to a scope with captured variables, we create a frame object and store it in a local variable.
    ''' (3) References to captured variables are transformed into references to fields of a frame class.
    ''' 
    ''' In addition, the rewriting deposits into the field LambdaRewriter.generatedMethods a (MethodSymbol, BoundStatement)
    ''' pair for each generated method.
    ''' 
    ''' LambdaRewriter.Rewrite produces its output in two forms.  First, it returns a new bound statement
    ''' for the caller to use for the body of the original method.  Second, it returns a collection of
    ''' (MethodSymbol, BoundStatement) pairs for additional method that the lambda rewriter produced.
    ''' These additional methods contain the bodies of the lambdas moved into ordinary methods of their
    ''' respective frame classes, and the caller is responsible for processing them just as it does with
    ''' the returned bound node.  For example, the caller will typically perform iterator method and
    ''' asynchronous method transformations, and emit IL instructions into an assembly.
    ''' </summary>
    Partial Class LambdaRewriter
        Inherits MethodToClassRewriter(Of FieldSymbol)

        Private ReadOnly _analysis As Analysis
        Private ReadOnly _topLevelMethod As MethodSymbol
        Private ReadOnly _topLevelMethodOrdinal As Integer

        ' lambda frame for static lambdas. 
        ' initialized lazily and could be Nothing if there are no static lambdas
        Private lazyStaticLambdaFrame As LambdaFrame

        ' for each block with lifted (captured) variables, the corresponding frame type
        Private ReadOnly frames As Dictionary(Of BoundNode, LambdaFrame) = New Dictionary(Of BoundNode, LambdaFrame)()

        ' the current set of frame pointers in scope.  Each is either a local variable (where introduced),
        ' or the "this" parameter when at the top level. Keys in this map are never constructed types.
        Private ReadOnly framePointers As Dictionary(Of NamedTypeSymbol, Symbol) = New Dictionary(Of NamedTypeSymbol, Symbol)()

        ' The method/lambda which is currently being rewritten.
        ' if we are rewriting a lambda, currentMethod is the new generated method.
        Private _currentMethod As MethodSymbol

        ' "This" in the context of current method.
        Private currentFrameThis As ParameterSymbol

        Private _lambdaOrdinalDispenser As Integer
        Private _delegateRelaxationIdDispenser As Integer

        ' ID dispenser for field names of frame references.
        Private synthesizedFieldNameIdDispenser As Integer

        ' The symbol (field or local) holding the innermost frame. 
        ' Needed in case inner frame needs to reference outer frame.
        Private innermostFramePointer As Symbol

        ' The mapping of type parameters for the current lambda body
        Private currentLambdaBodyTypeSubstitution As TypeSubstitution

        ' The current set of type parameters (mapped from the enclosing method's type parameters)
        Private currentTypeParameters As ImmutableArray(Of TypeParameterSymbol)

        'initialization for the proxy of the upper frame if it needs to be deferred 
        'such situation happens when lifting Me in a ctor.
        'CLR requires that the first use of "Me" must be a constructor call for which "Me" is a receiver
        'only after that we can proceed with lifting "Me"
        Private thisProxyInitDeferred As BoundExpression

        ' Are we code that will be rewritten into an expression tree?
        Private inExpressionLambda As Boolean

        Private reported_ERR_CannotUseOnErrorGotoWithClosure As Boolean

        ''' <summary> WARNING: used ONLY in DEBUG </summary>
        Private rewrittenNodes As HashSet(Of BoundNode) = Nothing

        Private Sub New(analysis As Analysis,
                        method As MethodSymbol,
                        methodOrdinal As Integer,
                        lambdaOrdinalDispenser As Integer,
                        delegateRelaxationIdDispenser As Integer,
                        slotAllocatorOpt As VariableSlotAllocator,
                        compilationState As TypeCompilationState,
                        diagnostics As DiagnosticBag)
            MyBase.New(slotAllocatorOpt, compilationState, diagnostics)

            Me._topLevelMethod = method
            Me._topLevelMethodOrdinal = methodOrdinal
            Me._lambdaOrdinalDispenser = lambdaOrdinalDispenser
            Me._delegateRelaxationIdDispenser = delegateRelaxationIdDispenser
            Me._currentMethod = method
            Me._analysis = analysis
            Me.currentTypeParameters = Me._topLevelMethod.TypeParameters
            Me.inExpressionLambda = False

            If Not method.IsShared Then
                Me.innermostFramePointer = method.MeParameter
                framePointers(method.ContainingType) = method.MeParameter
            End If

            Me.currentFrameThis = method.MeParameter
            Me.synthesizedFieldNameIdDispenser = 1
        End Sub

        ''' <summary>
        ''' Rewrite the given node to eliminate lambda expressions.  Also returned are the method symbols and their
        ''' bound bodies for the extracted lambda bodies. These would typically be emitted by the caller such as
        ''' MethodBodyCompiler.  See this class' documentation
        ''' for a more thorough explanation of the algorithm and its use by clients.
        ''' </summary>
        ''' <param name="node">The bound node to be rewritten</param>
        ''' <param name="method">The containing method of the node to be rewritten</param>
        ''' <param name="methodOrdinal">Index of the method symbol in its containing type member list.</param>
        ''' <param name="compilationState">The caller's buffer into which we produce additional methods to be emitted by the caller</param>
        ''' <param name="symbolsCapturedWithoutCopyCtor">Set of symbols that should not be captured using a copy constructor</param>
        ''' <param name="diagnostics">The caller's buffer into which we place any diagnostics for problems encountered</param>
        Public Shared Function Rewrite(node As BoundBlock,
                                       method As MethodSymbol,
                                       methodOrdinal As Integer,
                                       ByRef lambdaOrdinalDispenser As Integer,
                                       ByRef scopeOrdinalDispenser As Integer,
                                       ByRef delegateRelaxationIdDispenser As Integer,
                                       slotAllocatorOpt As VariableSlotAllocator,
                                       CompilationState As TypeCompilationState,
                                       symbolsCapturedWithoutCopyCtor As ISet(Of Symbol),
                                       diagnostics As DiagnosticBag,
                                       rewrittenNodes As HashSet(Of BoundNode)) As BoundBlock

            Dim analysis = LambdaRewriter.Analysis.AnalyzeMethodBody(node, method, symbolsCapturedWithoutCopyCtor, diagnostics)
            If Not analysis.seenLambda Then
                Return node
            End If

            Dim rewriter = New LambdaRewriter(analysis,
                                              method,
                                              methodOrdinal,
                                              lambdaOrdinalDispenser,
                                              delegateRelaxationIdDispenser,
                                              slotAllocatorOpt,
                                              CompilationState,
                                              diagnostics)
#If DEBUG Then
            Debug.Assert(rewrittenNodes IsNot Nothing)
            rewriter.rewrittenNodes = rewrittenNodes
#End If

            analysis.ComputeLambdaScopesAndFrameCaptures()
            rewriter.MakeFrames(scopeOrdinalDispenser)

            Dim body = DirectCast(rewriter.Visit(node), BoundBlock)

            ' Lambdas created during the rewriter are assigned indices And the dispenser field Is updated.
            lambdaOrdinalDispenser = rewriter._lambdaOrdinalDispenser
            delegateRelaxationIdDispenser = rewriter._delegateRelaxationIdDispenser

            Return body
        End Function

        Protected Overrides ReadOnly Property CurrentMethod As MethodSymbol
            Get
                Return Me._currentMethod
            End Get
        End Property

        Protected Overrides ReadOnly Property TopLevelMethod As MethodSymbol
            Get
                Return Me._topLevelMethod
            End Get
        End Property

        Protected Overrides ReadOnly Property TypeMap As TypeSubstitution
            Get
                Return Me.currentLambdaBodyTypeSubstitution
            End Get
        End Property

        Protected Overrides ReadOnly Property IsInExpressionLambda As Boolean
            Get
                Return Me.inExpressionLambda
            End Get
        End Property

        ''' <summary>
        ''' Create the frame types.
        ''' </summary>
        Private Sub MakeFrames(ByRef scopeOrdinalDispenser As Integer)
            ' There is a simple test to determine whether to do copy-construction:
            ' If method contains a backward branch, then all closures should attempt copy-construction.
            ' In the worst case, we will redundantly check if a previous version exists which is cheap
            ' compared to other costs associated with closure.
            '
            ' In some cases the closure should never be copy constructed, where the symbols are contained in 
            '  _analysis.symbolsCapturedWithoutCopyCtor.
            Dim copyConstructor = _analysis.seenBackBranches

            For Each captured In _analysis.capturedVariables
                Dim node As BoundNode = Nothing
                If Not _analysis.variableScope.TryGetValue(captured, node) OrElse _analysis.declaredInsideExpressionLambda.Contains(captured) Then
                    Continue For
                End If

                Dim frame As LambdaFrame = GetFrameForScope(copyConstructor, captured, node, scopeOrdinalDispenser, _delegateRelaxationIdDispenser)

                Dim proxy = LambdaCapturedVariable.Create(frame, captured, synthesizedFieldNameIdDispenser)
                Proxies.Add(captured, proxy)
                If CompilationState.ModuleBuilderOpt IsNot Nothing Then
                    frame.m_captured_locals.Add(proxy)
                End If
            Next

            If CompilationState.ModuleBuilderOpt IsNot Nothing Then
                For Each frame In frames.Values
                    CompilationState.AddSynthesizedMethod(frame.Constructor, MakeFrameCtor(frame, Diagnostics))
                Next
            End If
        End Sub

        Private Function GetFrameForScope(copyConstructor As Boolean,
                                          captured As Symbol,
                                          node As BoundNode,
                                          ByRef scopeOrdinalDispenser As Integer,
                                          ByRef delegateRelaxationIdDispenser As Integer) As LambdaFrame
            Dim frame As LambdaFrame = Nothing

            If Not frames.TryGetValue(node, frame) Then
                ' if the control variable of a for each is lifted, make sure it's using the copy constructor
                Debug.Assert(captured.Kind <> SymbolKind.Local OrElse
                             Not DirectCast(captured, LocalSymbol).IsForEach OrElse
                             copyConstructor)

                ' Frames created for delegate relaxations are just immutable wrappers of the delegate target object.
                ' They are not reused during EnC update and thus don't have a closure scope.
                Dim isDelegateRelaxationFrame = If(TryCast(captured, SynthesizedLocal)?.SynthesizedKind = SynthesizedLocalKind.DelegateRelaxationReceiver, False)
                Dim scopeOrdinal As Integer
                If isDelegateRelaxationFrame Then
                    scopeOrdinal = delegateRelaxationIdDispenser
                    delegateRelaxationIdDispenser += 1
                Else
                    scopeOrdinal = scopeOrdinalDispenser
                    scopeOrdinalDispenser += 1
                End If

                frame = New LambdaFrame(SlotAllocatorOpt,
                                        CompilationState,
                                        _topLevelMethod,
                                        _topLevelMethodOrdinal,
                                        node.Syntax,
                                        scopeOrdinal,
                                        copyConstructor AndAlso Not _analysis.symbolsCapturedWithoutCopyCtor.Contains(captured),
                                        isStatic:=False,
                                        isDelegateRelaxationFrame:=isDelegateRelaxationFrame)

                frames(node) = frame

                If CompilationState.ModuleBuilderOpt IsNot Nothing Then
                    CompilationState.ModuleBuilderOpt.AddSynthesizedDefinition(_topLevelMethod.ContainingType, frame)
                    ' NOTE: we will add this ctor to compilation state after we know all captured locals
                    '       we need them to generate copy constructor, if needed
                End If
            End If

            Return frame
        End Function

        Private Function GetStaticFrame(lambda As BoundNode, diagnostics As DiagnosticBag) As LambdaFrame
            If Me.lazyStaticLambdaFrame Is Nothing Then
                Dim isNonGeneric = Not TopLevelMethod.IsGenericMethod
                If isNonGeneric Then
                    Me.lazyStaticLambdaFrame = CompilationState.staticLambdaFrame
                End If

                If Me.lazyStaticLambdaFrame Is Nothing Then
                    ' associate the frame with the the first lambda that caused it to exist. 
                    ' we need to associate this with somme syntax.
                    ' unfortunately either containing method or containing class could be synthetic
                    ' therefore could have no syntax.
                    Me.lazyStaticLambdaFrame = New LambdaFrame(SlotAllocatorOpt, CompilationState, _topLevelMethod, If(isNonGeneric, -1, _topLevelMethodOrdinal), lambda.Syntax, scopeOrdinal:=-1, copyConstructor:=False, isStatic:=True, isDelegateRelaxationFrame:=False)

                    ' non-generic static lambdas can share the frame
                    If isNonGeneric Then
                        CompilationState.staticLambdaFrame = Me.lazyStaticLambdaFrame
                    End If

                    If CompilationState.ModuleBuilderOpt IsNot Nothing Then
                        Dim frame = Me.lazyStaticLambdaFrame

                        ' add frame type
                        CompilationState.ModuleBuilderOpt.AddSynthesizedDefinition(_topLevelMethod.ContainingType, frame)

                        ' add its ctor
                        Dim syntax = lambda.Syntax
                        CompilationState.AddSynthesizedMethod(frame.Constructor, MakeFrameCtor(frame, diagnostics))

                        ' add cctor
                        ' Frame.inst = New Frame()
                        Dim F = New SyntheticBoundNodeFactory(frame.SharedConstructor, frame.SharedConstructor, syntax, CompilationState, diagnostics)
                        Dim body = F.Block(
                                F.Assignment(
                                    F.Field(Nothing, frame.SingletonCache, isLValue:=True),
                                    F.[New](frame.Constructor)),
                                F.Return())

                        CompilationState.AddSynthesizedMethod(frame.SharedConstructor, body)
                    End If
                End If
            End If

            Return Me.lazyStaticLambdaFrame
        End Function


        ''' <summary>
        ''' Produces a bound expression representing a pointer to a frame of a particular frame type.
        ''' </summary>
        ''' <param name="syntax">The syntax to attach to the bound nodes produced</param>
        ''' <param name="frameType">The type of frame to be returned</param>
        ''' <returns>A bound node that computes the pointer to the required frame</returns>
        Private Function FrameOfType(syntax As VisualBasicSyntaxNode, frameType As NamedTypeSymbol) As BoundExpression
            Dim result As BoundExpression = FramePointer(syntax, frameType.OriginalDefinition)
            Debug.Assert(result.Type = frameType)
            Return result
        End Function

        ''' <summary>
        ''' Produce a bound expression representing a pointer to a frame of a particular frame class.
        ''' Note that for generic frames, the frameClass parameter is the generic definition, but
        ''' the resulting expression will be constructed with the current type parameters.
        ''' </summary>
        ''' <param name="syntax">The syntax to attach to the bound nodes produced</param>
        ''' <param name="frameClass">The class type of frame to be returned</param>
        ''' <returns>A bound node that computes the pointer to the required frame</returns>
        Friend Overrides Function FramePointer(syntax As VisualBasicSyntaxNode, frameClass As NamedTypeSymbol) As BoundExpression
            Debug.Assert(frameClass.IsDefinition)
            If currentFrameThis IsNot Nothing AndAlso currentFrameThis.Type = frameClass Then
                Return New BoundMeReference(syntax, frameClass)
            End If

            ' Otherwise we need to return the value from a frame pointer local variable...
            Dim result As Symbol = framePointers(frameClass)
            Dim proxyField As FieldSymbol = Nothing
            If Proxies.TryGetValue(result, proxyField) Then
                ' However, frame pointer local variables themselves can be "captured".  In that case
                ' the inner frames contain pointers to the enclosing frames.  That is, nested
                ' frame pointers are organized in a linked list.
                Dim innerFrame As BoundExpression = FramePointer(syntax, proxyField.ContainingType)
                Dim proxyFieldParented = proxyField.AsMember(DirectCast(innerFrame.Type, NamedTypeSymbol))
                Return New BoundFieldAccess(syntax, innerFrame, proxyFieldParented, False, proxyFieldParented.Type)
            End If

            Dim localFrame = DirectCast(result, LocalSymbol)
            Return New BoundLocal(syntax, localFrame, isLValue:=False, type:=localFrame.Type)
        End Function

        Protected Overrides Function MaterializeProxy(origExpression As BoundExpression, proxy As FieldSymbol) As BoundNode
            Dim frame As BoundExpression = FramePointer(origExpression.Syntax, proxy.ContainingType)
            Dim constructedProxyField = proxy.AsMember(DirectCast(frame.Type, NamedTypeSymbol))
            Return New BoundFieldAccess(origExpression.Syntax,
                                        frame,
                                        constructedProxyField,
                                        origExpression.IsLValue,
                                        constructedProxyField.Type)
        End Function

        Private Function MakeFrameCtor(frame As LambdaFrame, diagnostics As DiagnosticBag) As BoundBlock
            Dim constructor = frame.Constructor
            Dim syntaxNode As VisualBasicSyntaxNode = constructor.Syntax

            Dim builder = ArrayBuilder(Of BoundStatement).GetInstance
            builder.Add(MethodCompiler.BindDefaultConstructorInitializer(constructor, diagnostics))

            ' add copy logic if ctor has parameters - 
            '
            ' Sub  New(arg as Frame)
            '   if arg is nothing goto Done
            '       Me.field0 = arg.field0                
            '       Me.field1 = arg.field1                
            '       . . .
            '       Me.fieldN = arg.fieldN                
            '   Done:
            '   Return
            ' End Sub

            If Not constructor.Parameters.IsEmpty Then
                Dim arg = constructor.Parameters(0)
                Debug.Assert(arg.Type Is frame)

                Dim parameterExpr = New BoundParameter(syntaxNode, arg, frame)

                Dim bool = frame.ContainingAssembly.GetSpecialType(SpecialType.System_Boolean)
                Dim useSiteError = bool.GetUseSiteErrorInfo()
                If useSiteError IsNot Nothing Then
                    diagnostics.Add(useSiteError, syntaxNode.GetLocation())
                End If

                Dim obj = frame.ContainingAssembly.GetSpecialType(SpecialType.System_Object)
                ' WARN: We assume that if System_Object was not found we would never reach 
                '       this point because the error should have been/processed generated earlier
                Debug.Assert(obj.GetUseSiteErrorInfo() Is Nothing)

                Dim condition = New BoundBinaryOperator(syntaxNode, BinaryOperatorKind.Is,
                                                        New BoundDirectCast(syntaxNode, parameterExpr.MakeRValue(), ConversionKind.WideningReference, obj, Nothing),
                                                        New BoundLiteral(syntaxNode, ConstantValue.Nothing, obj),
                                                        False,
                                                        bool)

                Dim doneLabel = New GeneratedLabelSymbol("Done")
                Dim condGoto = New BoundConditionalGoto(syntaxNode, condition, True, doneLabel)

                ' If arg is Nothing Then GoTo Done
                builder.Add(condGoto)

                Dim thisParam = constructor.MeParameter
                Debug.Assert(thisParam.Type Is frame)

                Dim this = New BoundParameter(syntaxNode, thisParam, frame)

                For Each field In frame.m_captured_locals
                    Dim type = field.Type
                    Dim left = New BoundFieldAccess(syntaxNode, this, field, True, field.Type)
                    Dim right = New BoundFieldAccess(syntaxNode, parameterExpr, field, False, field.Type)
                    Dim fieldInit = New BoundAssignmentOperator(syntaxNode, left, right, True, type)

                    ' me.FieldX = arg.FieldX
                    builder.Add(New BoundExpressionStatement(syntaxNode, fieldInit))
                Next

                ' Done:
                builder.Add(New BoundLabelStatement(syntaxNode, doneLabel))
            End If

            ' Return
            builder.Add(New BoundReturnStatement(syntaxNode, Nothing, Nothing, Nothing))

            Return New BoundBlock(syntaxNode,
                                Nothing,
                                ImmutableArray(Of LocalSymbol).Empty,
                                builder.ToImmutableAndFree())
        End Function

        ''' <summary>
        ''' Constructs a concrete frame type if needed.
        ''' </summary>
        Friend Shared Function ConstructFrameType(Of T As TypeSymbol)(type As LambdaFrame, typeArguments As ImmutableArray(Of T)) As NamedTypeSymbol
            If type.CanConstruct Then
                Return type.Construct(StaticCast(Of TypeSymbol).From(typeArguments))
            Else
                Debug.Assert(typeArguments.IsEmpty)
                Return type
            End If
        End Function

        ''' <summary>
        ''' Introduce a frame around the translation of the given node.
        ''' </summary>
        ''' <param name="node">The node whose translation should be translated to contain a frame</param>
        ''' <param name="frame">The frame for the translated node</param>
        ''' <param name="F">A function that computes the translation of the node.  It receives lists of added statements and added symbols</param>
        ''' <returns>The translated statement, as returned from F</returns>
        Private Function IntroduceFrame(node As BoundNode,
                                        frame As LambdaFrame,
                                        F As Func(Of ArrayBuilder(Of BoundExpression), ArrayBuilder(Of LocalSymbol), BoundNode),
                                        Optional origLambda As LambdaSymbol = Nothing) As BoundNode

            Dim frameType As NamedTypeSymbol = ConstructFrameType(frame, currentTypeParameters)
            Dim framePointer = New SynthesizedLocal(Me._topLevelMethod, frameType, SynthesizedLocalKind.LambdaDisplayClass, frame.ScopeSyntax)
            Dim prologue = ArrayBuilder(Of BoundExpression).GetInstance()
            Dim constructor As MethodSymbol = frame.Constructor.AsMember(frameType)
            Debug.Assert(frameType = constructor.ContainingType)

            Dim syntaxNode As VisualBasicSyntaxNode = node.Syntax

            Dim frameAccess = New BoundLocal(syntaxNode, framePointer, frameType)

            ' if this is a copy-ctor, it should get previous frame value
            Dim args = If(constructor.Parameters.IsEmpty,
                          ImmutableArray(Of BoundExpression).Empty,
                          ImmutableArray.Create(Of BoundExpression)(frameAccess))

            prologue.Add(New BoundAssignmentOperator(
                syntaxNode,
                frameAccess,
                New BoundObjectCreationExpression(syntaxNode, constructor, args, Nothing, frameType),
                True,
                frameType))

            Dim oldInnermostFrameProxy As FieldSymbol = Nothing

            If innermostFramePointer IsNot Nothing Then
                Proxies.TryGetValue(innermostFramePointer, oldInnermostFrameProxy)

                If _analysis.needsParentFrame.Contains(node) Then
                    Dim capturedFrame = LambdaCapturedVariable.Create(frame, innermostFramePointer, synthesizedFieldNameIdDispenser)
                    Dim frameParent = capturedFrame.AsMember(frameType)
                    Dim left As BoundExpression = New BoundFieldAccess(syntaxNode,
                                                                       New BoundLocal(syntaxNode, framePointer, frameType),
                                                                       frameParent,
                                                                       True,
                                                                       frameParent.Type)

                    Dim right As BoundExpression = FrameOfType(syntaxNode, TryCast(frameParent.Type, NamedTypeSymbol))
                    Dim assignment = New BoundAssignmentOperator(syntaxNode, left, right, True, left.Type)

                    ' if we are capturing "Me" in a ctor, we should do it after "Me" is initialized
                    If innermostFramePointer.Kind = SymbolKind.Parameter AndAlso _topLevelMethod.MethodKind = MethodKind.Constructor AndAlso _topLevelMethod Is _currentMethod Then
                        Debug.Assert(thisProxyInitDeferred Is Nothing, "we should be capturing 'Me' only once")
                        thisProxyInitDeferred = assignment
                    Else
                        prologue.Add(assignment)
                    End If

                    If CompilationState.ModuleBuilderOpt IsNot Nothing Then
                        CompilationState.ModuleBuilderOpt.AddSynthesizedDefinition(frame, capturedFrame)
                    End If

                    Proxies(innermostFramePointer) = capturedFrame
                End If
            End If

            If origLambda IsNot Nothing Then
                ' init proxies for lambda parameters
                For Each p In origLambda.Parameters
                    InitParameterProxy(syntaxNode, p, framePointer, frameType, prologue)
                Next
            Else
                ' init proxies for method parameters when seeing top block
                If Not _analysis.blockParent.ContainsKey(node) Then
                    Debug.Assert(_topLevelMethod = _currentMethod)

                    For Each p In _topLevelMethod.Parameters
                        InitParameterProxy(syntaxNode, p, framePointer, frameType, prologue)
                    Next
                End If
            End If

            Dim oldInnermostFramePointer As Symbol = innermostFramePointer
            innermostFramePointer = framePointer
            Dim addedLocals = ArrayBuilder(Of LocalSymbol).GetInstance()
            addedLocals.Add(framePointer)
            framePointers.Add(frame, framePointer)

            Dim result = F(prologue, addedLocals)

            framePointers.Remove(frame)
            innermostFramePointer = oldInnermostFramePointer

            If innermostFramePointer IsNot Nothing Then
                If oldInnermostFrameProxy IsNot Nothing Then
                    Proxies(innermostFramePointer) = oldInnermostFrameProxy
                Else
                    Proxies.Remove(innermostFramePointer)
                End If
            End If

            Return result
        End Function

        ''' <summary>
        ''' If parameter is lifted, initializes its proxy
        ''' </summary>
        Private Sub InitParameterProxy(syntaxNode As VisualBasicSyntaxNode,
                                       origParameter As ParameterSymbol,
                                       framePointer As LocalSymbol,
                                       frameType As NamedTypeSymbol,
                                       prologue As ArrayBuilder(Of BoundExpression))

            If origParameter IsNot Nothing Then
                Dim proxy As FieldSymbol = Nothing
                If Proxies.TryGetValue(origParameter, proxy) AndAlso Not _analysis.declaredInsideExpressionLambda.Contains(origParameter) Then
                    ' parameter needs to be accessed in the context of current method
                    Dim actualParameter As ParameterSymbol = Nothing
                    If Not ParameterMap.TryGetValue(origParameter, actualParameter) Then
                        actualParameter = origParameter
                    End If

                    Dim parameterAccess = New BoundParameter(syntaxNode,
                                                           actualParameter,
                                                           isLValue:=False,
                                                           type:=actualParameter.Type)

                    Dim constructedProxy = proxy.AsMember(frameType)
                    Dim assignParameterProxy As BoundExpression = New BoundAssignmentOperator(
                                                                    syntaxNode,
                                                                    New BoundFieldAccess(
                                                                        syntaxNode,
                                                                        New BoundLocal(syntaxNode,
                                                                                       framePointer,
                                                                                       frameType),
                                                                        constructedProxy,
                                                                        True,
                                                                        constructedProxy.Type),
                                                                    parameterAccess,
                                                                    True,
                                                                    constructedProxy.Type)

                    prologue.Add(assignParameterProxy)
                End If
            End If
        End Sub

        Public Overrides Function VisitMeReference(node As BoundMeReference) As BoundNode
            If _currentMethod = _topLevelMethod Then
                Return node
            End If

            ' this can happen only in a case of errors
            ' we will not find a frame for "Me" in a shared method
            If _topLevelMethod.IsShared Then
                Return node
            End If

            Return FramePointer(node.Syntax, TryCast(node.Type, NamedTypeSymbol))
        End Function

        Public Overrides Function VisitMyClassReference(node As BoundMyClassReference) As BoundNode
            Return If(_currentMethod Is _topLevelMethod, node,
                      If(_currentMethod.ContainingType Is _topLevelMethod.ContainingType,
                         New BoundMyClassReference(node.Syntax, node.Type),
                         FramePointer(node.Syntax, TryCast(_topLevelMethod.ContainingType, NamedTypeSymbol))))
        End Function

        Public Overrides Function VisitMyBaseReference(node As BoundMyBaseReference) As BoundNode
            Return If(_currentMethod Is _topLevelMethod, node,
                      If(_currentMethod.ContainingType Is _topLevelMethod.ContainingType,
                         New BoundMyBaseReference(node.Syntax, node.Type),
                         FramePointer(node.Syntax, TryCast(_topLevelMethod.ContainingType, NamedTypeSymbol))))
        End Function

        Public Overrides Function VisitRangeVariable(node As BoundRangeVariable) As BoundNode
            Throw ExceptionUtilities.Unreachable
        End Function

        Private Function RewriteStatementList(node As BoundStatementList,
                               prologue As ArrayBuilder(Of BoundExpression),
                               newLocals As ArrayBuilder(Of LocalSymbol)) As BoundStatement

            Dim newStatements = ArrayBuilder(Of BoundStatement).GetInstance

            For Each expr In prologue
                newStatements.Add(New BoundExpressionStatement(expr.Syntax, expr))
            Next

            ' done with this
            prologue.Free()

            For Each s In node.Statements
                Dim replacement = DirectCast(Me.Visit(s), BoundStatement)
                If replacement IsNot Nothing Then
                    newStatements.Add(replacement)
                End If
            Next

            If newLocals.Count = 0 Then
                newLocals.Free()
                Return node.Update(newStatements.ToImmutableAndFree())
            Else
                Return New BoundBlock(node.Syntax,
                                      Nothing,
                                      newLocals.ToImmutableAndFree(),
                                      newStatements.ToImmutableAndFree())
            End If
        End Function

        Public Overrides Function VisitBlock(node As BoundBlock) As BoundNode
            Dim frame As LambdaFrame = Nothing

            ' Test if this frame has captured variables and requires the introduction of a closure class.
            If frames.TryGetValue(node, frame) Then
                Return IntroduceFrame(node, frame,
                                      Function(prologue As ArrayBuilder(Of BoundExpression), newLocals As ArrayBuilder(Of LocalSymbol))
                                          Return RewriteBlock(node, prologue, newLocals)
                                      End Function)
            Else
                Return RewriteBlock(node)
            End If
        End Function

        Public Overrides Function VisitSequence(node As BoundSequence) As BoundNode
            Dim frame As LambdaFrame = Nothing

            ' Test if this frame has captured variables and requires the introduction of a closure class.
            If frames.TryGetValue(node, frame) Then
                Return IntroduceFrame(node, frame,
                                      Function(prologue As ArrayBuilder(Of BoundExpression), newLocals As ArrayBuilder(Of LocalSymbol))
                                          Return RewriteSequence(node, prologue, newLocals)
                                      End Function)
            Else
                Return RewriteSequence(node)
            End If
        End Function

        Public Overrides Function VisitCatchBlock(node As BoundCatchBlock) As BoundNode
            Dim frame As LambdaFrame = Nothing

            ' Test if this frame has captured variables and requires the introduction of a closure class.
            If frames.TryGetValue(node, frame) Then
                Return IntroduceFrame(node, frame,
                                      Function(prologue As ArrayBuilder(Of BoundExpression), newLocals As ArrayBuilder(Of LocalSymbol))
                                          Return RewriteCatch(node, prologue, newLocals)
                                      End Function)
            Else
                Return RewriteCatch(node, ArrayBuilder(Of BoundExpression).GetInstance, ArrayBuilder(Of LocalSymbol).GetInstance)
            End If
        End Function

        Private Function RewriteCatch(node As BoundCatchBlock,
                                      prologue As ArrayBuilder(Of BoundExpression),
                                      newLocals As ArrayBuilder(Of LocalSymbol)) As BoundCatchBlock

            ' Catch node contains 3 important pieces
            ' 1) LocalOpt - like BoundBlock, catch may own variables, but it happens so that it never needs more than one.
            ' 2) ExceptionVariable - presense of this variable indicates that caught exception needs to be stored. 
            '                        in such case ExceptionVariable is used as a target of a one-time assignment
            '                        when Catch is entered.
            ' 3) Code (Filter and Body)
            '
            ' It is important to note that all these 3 parts do not have any any dependencies on each other 
            ' except that assignment must happen before any other Catch code is executed.
            '
            ' When LocalOpt is present, ExceptionVariable typically holds a reference to it, but it is not a requirement.
            ' There is nothing wrong with LocalOpt holding a reference to a closure frame and 
            ' ExceptionVariable pointing to a lifted variable.
            ' And we will do exactly that in a case if LocalOpt gets lifted.

            Dim rewrittenCatchLocal As LocalSymbol = Nothing

            If newLocals.Count <> 0 Then
                Debug.Assert(newLocals.Count = 1, "must be only one local that is the frame reference")
                Debug.Assert(Me.Proxies.ContainsKey(node.LocalOpt), "original local should be proxied")

                ' getting new locals means that our original local was lifted into a closure
                ' and instead of an actual local Catch will own frame reference.
                rewrittenCatchLocal = newLocals(0)

            ElseIf node.LocalOpt IsNot Nothing Then
                ' local was not lifted, but its type may need to be rewritten
                ' this happens when it has a generic type which needs to be rewritten
                ' when lambda body was moved to a separate method.
                Dim origLocal = node.LocalOpt
                Debug.Assert(Not Me.Proxies.ContainsKey(origLocal), "captured local should not need rewriting")

                Dim newType = VisitType(origLocal.Type)
                If newType = origLocal.Type Then
                    ' keeping same local
                    rewrittenCatchLocal = origLocal

                Else
                    ' need a local of a different type
                    rewrittenCatchLocal = CreateReplacementLocalOrReturnSelf(origLocal, newType)
                    LocalMap.Add(origLocal, rewrittenCatchLocal)
                End If
            End If

            Dim rewrittenExceptionSource = DirectCast(Me.Visit(node.ExceptionSourceOpt), BoundExpression)

            ' If exception variable got lifted, IntroduceFrame will give us frame init prologue.
            ' It needs to run before the exception variable is accessed.
            ' To ensure that, we will make exception variable a sequence that performs prologue as its its sideeffecs.
            If prologue.Count <> 0 Then
                rewrittenExceptionSource = New BoundSequence(
                    rewrittenExceptionSource.Syntax,
                    ImmutableArray(Of LocalSymbol).Empty,
                    prologue.ToImmutable,
                    rewrittenExceptionSource,
                    rewrittenExceptionSource.Type)
            End If

            ' done with these.
            newLocals.Free()
            prologue.Free()

            ' rewrite filter and body
            ' NOTE: this will proxy all accesses to exception local if that got lifted.
            Dim rewrittenErrorLineNumberOpt = DirectCast(Me.Visit(node.ErrorLineNumberOpt), BoundExpression)
            Dim rewrittenFilter = DirectCast(Me.Visit(node.ExceptionFilterOpt), BoundExpression)
            Dim rewrittenBody = DirectCast(Me.Visit(node.Body), BoundBlock)

            ' rebuild the node.
            Return node.Update(rewrittenCatchLocal,
                               rewrittenExceptionSource,
                               rewrittenErrorLineNumberOpt,
                               rewrittenFilter,
                               rewrittenBody,
                               node.IsSynthesizedAsyncCatchAll)
        End Function

        Public Overrides Function VisitStatementList(node As BoundStatementList) As BoundNode
            Dim frame As LambdaFrame = Nothing

            ' Test if this frame has captured variables and requires the introduction of a closure class.
            ' That can occur for a BoundStatementList if it is the body of a method with captured parameters.
            If frames.TryGetValue(node, frame) Then
                Return IntroduceFrame(node, frame,
                                      Function(prologue As ArrayBuilder(Of BoundExpression), newLocals As ArrayBuilder(Of LocalSymbol))
                                          Return RewriteStatementList(node, prologue, newLocals)
                                      End Function)
            Else
                Return MyBase.VisitStatementList(node)
            End If

        End Function

        ''' <summary>
        ''' Rewrites lambda body into a body of a method.
        ''' </summary>
        ''' <param name="method">Method symbol for the rewritten lambda body.</param>
        ''' <param name="lambda">Original lambda node.</param>
        ''' <returns>Lambda body rewritten as a body of the given method symbol.</returns>
        Public Function RewriteLambdaAsMethod(method As MethodSymbol, lambda As BoundLambda) As BoundBlock

            ' report use site errors for attributes that are needed later on in the rewriter
            Dim lambdaSyntax = lambda.Syntax

            Dim node As BoundBlock = lambda.Body
            Dim frame As LambdaFrame = Nothing
            Dim loweredBody As BoundBlock = Nothing

            If frames.TryGetValue(node, frame) Then
                loweredBody = DirectCast(
                                    IntroduceFrame(node, frame,
                                      Function(prologue As ArrayBuilder(Of BoundExpression), newLocals As ArrayBuilder(Of LocalSymbol))
                                          Return RewriteBlock(node, prologue, newLocals)
                                      End Function,
                                      lambda.LambdaSymbol),
                                    BoundBlock)
            Else
                loweredBody = RewriteBlock(node)
            End If

            ' TODO: create slot allocator
            Dim slotAllocatorOpt As VariableSlotAllocator = Nothing
            Dim stateMachineTypeOpt As StateMachineTypeSymbol = Nothing

            ' In case of async/iterator lambdas, the method has already been uniquely named, so there is no need to
            ' produce a unique method ordinal for the corresponding state machine type, whose name includes the (unique) method name.
            Const methodOrdinal As Integer = -1

            Return Rewriter.RewriteIteratorAndAsync(loweredBody, method, methodOrdinal, CompilationState, Diagnostics, slotAllocatorOpt, stateMachineTypeOpt)
        End Function

        Public Overrides Function VisitTryCast(node As BoundTryCast) As BoundNode
            Debug.Assert(node.RelaxationLambdaOpt Is Nothing)

            Dim lambda As BoundLambda = TryCast(node.Operand, BoundLambda)
            If lambda Is Nothing Then
                Return MyBase.VisitTryCast(node)
            End If

            Dim result As BoundExpression = RewriteLambda(lambda, VisitType(node.Type), (node.ConversionKind And ConversionKind.ConvertedToExpressionTree) <> 0)
            If inExpressionLambda Then
                result = node.Update(result, node.ConversionKind, node.ConstantValueOpt, node.RelaxationLambdaOpt, node.Type)
            End If
            Return result
        End Function

        Public Overrides Function VisitDirectCast(node As BoundDirectCast) As BoundNode
            Debug.Assert(node.RelaxationLambdaOpt Is Nothing)

            Dim lambda As BoundLambda = TryCast(node.Operand, BoundLambda)
            If lambda Is Nothing Then
                Return MyBase.VisitDirectCast(node)
            End If

            Dim result As BoundExpression = RewriteLambda(lambda, VisitType(node.Type), (node.ConversionKind And ConversionKind.ConvertedToExpressionTree) <> 0)
            If inExpressionLambda Then
                result = node.Update(result, node.ConversionKind, node.SuppressVirtualCalls, node.ConstantValueOpt, node.RelaxationLambdaOpt, node.Type)
            End If
            Return result
        End Function

        Public Overrides Function VisitConversion(conversion As BoundConversion) As BoundNode
            Debug.Assert(conversion.RelaxationLambdaOpt Is Nothing AndAlso conversion.RelaxationReceiverPlaceholderOpt Is Nothing)

            Dim lambda As BoundLambda = TryCast(conversion.Operand, BoundLambda)
            If lambda Is Nothing Then
                Return MyBase.VisitConversion(conversion)
            End If

            Dim result As BoundExpression = RewriteLambda(lambda, VisitType(conversion.Type), (conversion.ConversionKind And ConversionKind.ConvertedToExpressionTree) <> 0)
            If inExpressionLambda Then
                result = conversion.Update(result,
                                           conversion.ConversionKind,
                                           conversion.Checked,
                                           conversion.ExplicitCastInCode,
                                           conversion.ConstantValueOpt,
                                           conversion.ConstructorOpt,
                                           conversion.RelaxationLambdaOpt,
                                           conversion.RelaxationReceiverPlaceholderOpt,
                                           conversion.Type)
            End If
            Return result
        End Function

        Private Function RewriteLambda(node As BoundLambda, type As TypeSymbol, convertToExpressionTree As Boolean) As BoundExpression
            If convertToExpressionTree Or inExpressionLambda Then
                ' This lambda is being converted to an expression tree.
                Dim wasInExpressionLambda = inExpressionLambda
                inExpressionLambda = True

                Dim newBody = DirectCast(Visit(node.Body), BoundBlock)
                node = node.Update(node.LambdaSymbol, newBody, node.Diagnostics, node.LambdaBinderOpt, node.DelegateRelaxation, node.MethodConversionKind)

                Dim rewrittenNode As BoundExpression = node
                If Not wasInExpressionLambda Then
                    ' Rewritten outermost lambda as expression tree
                    Dim delegateType = type.ExpressionTargetDelegate(CompilationState.Compilation)
                    rewrittenNode = ExpressionLambdaRewriter.RewriteLambda(node, Me._currentMethod, delegateType, Me.CompilationState, Me.TypeMap, Me.Diagnostics, Me.rewrittenNodes)
                End If

                inExpressionLambda = wasInExpressionLambda
                Return rewrittenNode
            End If

            Dim translatedLambdaContainer As InstanceTypeSymbol
            Dim lambdaScope As BoundNode = Nothing
            Dim closureKind As ClosureKind

            If _analysis.lambdaScopes.TryGetValue(node.LambdaSymbol, lambdaScope) Then
                translatedLambdaContainer = frames(lambdaScope)
                closureKind = ClosureKind.General
            ElseIf _analysis.capturedVariablesByLambda(node.LambdaSymbol).Count = 0
                translatedLambdaContainer = GetStaticFrame(node, Diagnostics)
                closureKind = ClosureKind.Static
            Else
                translatedLambdaContainer = DirectCast(_topLevelMethod.ContainingType, InstanceTypeSymbol)
                closureKind = ClosureKind.ThisOnly
            End If

            Dim lambdaOrdinal As Integer
            If node.LambdaSymbol.SynthesizedKind = SynthesizedLambdaKind.DelegateRelaxationStub Then
                _delegateRelaxationIdDispenser += 1
                lambdaOrdinal = _delegateRelaxationIdDispenser
            Else
                _lambdaOrdinalDispenser += 1
                lambdaOrdinal = _lambdaOrdinalDispenser
            End If

            ' Move the body of the lambda to a freshly generated synthetic method on its container.
            Dim synthesizedMethod = New SynthesizedLambdaMethod(SlotAllocatorOpt, CompilationState, translatedLambdaContainer, closureKind, _topLevelMethod, _topLevelMethodOrdinal, node, lambdaOrdinal, Me.Diagnostics)

            If CompilationState.ModuleBuilderOpt IsNot Nothing Then
                CompilationState.ModuleBuilderOpt.AddSynthesizedDefinition(translatedLambdaContainer, synthesizedMethod)
            End If

            For Each parameter In node.LambdaSymbol.Parameters
                ParameterMap.Add(parameter, synthesizedMethod.Parameters(parameter.Ordinal))
            Next

            Dim oldMethod = _currentMethod
            Dim oldFrameThis = currentFrameThis
            Dim oldTypeParameters = currentTypeParameters
            Dim oldInnermostFramePointer = innermostFramePointer
            Dim oldTypeSubstitution = currentLambdaBodyTypeSubstitution

            Dim containerAsFrame = TryCast(translatedLambdaContainer, LambdaFrame)

            Me._currentMethod = synthesizedMethod

            If closureKind = ClosureKind.Static Then
                ' no link from a static lambda to its container
                innermostFramePointer = Nothing
                currentFrameThis = Nothing
            Else
                currentFrameThis = synthesizedMethod.MeParameter
                innermostFramePointer = Nothing
                framePointers.TryGetValue(translatedLambdaContainer, innermostFramePointer)
            End If

            If containerAsFrame IsNot Nothing Then
                currentTypeParameters = translatedLambdaContainer.TypeParameters
                currentLambdaBodyTypeSubstitution = containerAsFrame.TypeMap
            Else
                currentTypeParameters = synthesizedMethod.TypeParameters
                currentLambdaBodyTypeSubstitution = TypeSubstitution.Create(_topLevelMethod, _topLevelMethod.TypeParameters, _currentMethod.TypeArguments)
            End If

            Dim body = DirectCast(RewriteLambdaAsMethod(synthesizedMethod, node), BoundStatement)
            CompilationState.AddSynthesizedMethod(synthesizedMethod, body)

            ' return to old method

            Me._currentMethod = oldMethod
            currentFrameThis = oldFrameThis
            currentTypeParameters = oldTypeParameters
            innermostFramePointer = oldInnermostFramePointer
            currentLambdaBodyTypeSubstitution = oldTypeSubstitution

            ' Rewrite the lambda expression as a delegate creation expression
            Dim constructedFrame As NamedTypeSymbol = translatedLambdaContainer

            ' If container is a frame, create a concrete type
            If containerAsFrame IsNot Nothing Then
                constructedFrame = ConstructFrameType(containerAsFrame, currentTypeParameters)
            End If

            ' for instance lambdas, receiver is the frame
            ' for static lambdas, get the singleton receiver 
            Dim receiver As BoundExpression
            If closureKind <> ClosureKind.Static Then
                receiver = FrameOfType(node.Syntax, constructedFrame)
            Else
                Dim field = containerAsFrame.SingletonCache.AsMember(constructedFrame)
                receiver = New BoundFieldAccess(node.Syntax, Nothing, field, isLValue:=False, type:=field.Type)
            End If

            Dim referencedMethod As MethodSymbol = synthesizedMethod.AsMember(constructedFrame)

            If referencedMethod.IsGenericMethod Then
                referencedMethod = referencedMethod.Construct(StaticCast(Of TypeSymbol).From(currentTypeParameters))
            End If

            ' static lambdas are emitted as instance methods on a singleton receiver
            ' delegates invoke dispatch is optimized for instance delegates so 
            ' it is preferrable to emit lambdas as instance methods enven when lambdas 
            ' do Not capture anything
            Dim result As BoundExpression = New BoundDelegateCreationExpression(
                         node.Syntax,
                         receiver,
                         referencedMethod,
                         relaxationLambdaOpt:=Nothing,
                         relaxationReceiverPlaceholderOpt:=Nothing,
                         methodGroupOpt:=Nothing,
                         type:=type)

            ' If the block containing the lambda is not the innermost block,
            ' or the lambda is static, then the lambda object should be cached in its frame.
            ' NOTE: we are not caching static lambdas in static ctors - cannot reuse such cache
            ' NOTE: we require lambdaScope IsNot Nothing. We do not want to introduce a field into a user's class (not a synthetic frame)
            Dim shouldCacheStaticlambda As Boolean =
                (closureKind = ClosureKind.Static AndAlso CurrentMethod.MethodKind <> MethodKind.SharedConstructor AndAlso Not referencedMethod.IsGenericMethod)

            Dim shouldCacheInLoop As Boolean =
                (lambdaScope IsNot Nothing AndAlso lambdaScope IsNot _analysis.blockParent(node.Body)) AndAlso
                InLoopOrLambda(node.Syntax, lambdaScope.Syntax)

            If shouldCacheStaticlambda OrElse shouldCacheInLoop Then
                ' replace the expression "new Delegate(frame.M)" with "(frame.cache == null) ? (frame.cache = new Delegate(frame.M)) : frame.cache"

                Dim cachedFieldType As TypeSymbol = If(containerAsFrame Is Nothing,
                                                       type,
                                                       type.InternalSubstituteTypeParameters(containerAsFrame.TypeMap))

                ' If we are generating the field into a display class created exclusively for the lambda the lambdaOrdinal itself Is unique already, 
                ' no need to include the top-level method ordinal in the field name.
                Dim cacheFieldName As String = GeneratedNames.MakeLambdaCacheFieldName(
                    If(closureKind = ClosureKind.General, -1, _topLevelMethodOrdinal),
                    If(CompilationState.ModuleBuilderOpt?.CurrentGenerationOrdinal, 0), ' Note: module builder is not available only when testing emit diagnostics
                    lambdaOrdinal,
                    node.LambdaSymbol.SynthesizedKind)

                Dim cacheField As FieldSymbol = New SynthesizedFieldSymbol(translatedLambdaContainer,
                                                                           implicitlyDefinedBy:=node.LambdaSymbol,
                                                                           type:=cachedFieldType,
                                                                           name:=cacheFieldName,
                                                                           accessibility:=Accessibility.Public,
                                                                           isShared:=(closureKind = ClosureKind.Static))

                If CompilationState.ModuleBuilderOpt IsNot Nothing Then
                    CompilationState.ModuleBuilderOpt.AddSynthesizedDefinition(translatedLambdaContainer, cacheField)
                End If

                Dim F = New SyntheticBoundNodeFactory(Me._topLevelMethod, Me._currentMethod, node.Syntax, CompilationState, Diagnostics)
                Dim fieldToAccess As FieldSymbol = cacheField.AsMember(constructedFrame)
                Dim cacheVariableLeft = F.Field(receiver, fieldToAccess, isLValue:=True)
                Dim cacheVariableRight = F.Field(receiver, fieldToAccess, isLValue:=False)
                result = F.Conditional(
                    F.ObjectReferenceEqual(cacheVariableRight, F.Null(cacheVariableRight.Type)),
                    F.AssignmentExpression(cacheVariableLeft, result),
                    cacheVariableRight,
                    cacheVariableRight.Type)
            End If

            Return result
        End Function


        ' This helper checks syntactically whether there is a loop or lambda expression
        ' between given lambda syntax and the syntax that corresponds to its closure.
        ' we use this heuristic as a hint that the lambda delegate may be created 
        ' multiple times with same closure.
        ' In such cases it makes sense to cache the delegate.
        '
        ' Examples:
        '            dim x = 123
        '            for i as integer = 1 to 10 
        '                if (i < 2)
        '                    arr[i].Execute(Function(arg) arg + x)  // delegate should be cached
        '                end if
        '            Next

        '            for i as integer = 1 to 10 
        '                Dim val = i
        '                if (i< 2)
        '                    Dim y = i + i
        '                    System.Console.WriteLine(y)
        '                    arr[i].Execute(Function(arg) arg + val);  // delegate should Not be cached (closure created inside the loop)
        '                end if
        '            Next
        '
        Private Function InLoopOrLambda(lambdaSyntax As SyntaxNode, scopeSyntax As SyntaxNode) As Boolean
            Dim curSyntax = lambdaSyntax.Parent
            While (curSyntax IsNot Nothing AndAlso curSyntax IsNot scopeSyntax)
                Select Case curSyntax.Kind
                    Case SyntaxKind.ForBlock,
                         SyntaxKind.ForEachBlock,
                         SyntaxKind.WhileBlock,
                         SyntaxKind.SimpleDoLoopBlock,
                         SyntaxKind.DoWhileLoopBlock,
                         SyntaxKind.DoUntilLoopBlock,
                         SyntaxKind.DoLoopWhileBlock,
                         SyntaxKind.DoLoopUntilBlock,
                         SyntaxKind.MultiLineFunctionLambdaExpression,
                         SyntaxKind.MultiLineSubLambdaExpression,
                         SyntaxKind.SingleLineFunctionLambdaExpression,
                         SyntaxKind.SingleLineSubLambdaExpression
                        Return True
                End Select

                curSyntax = curSyntax.Parent
            End While

            Return False
        End Function

        Public Overrides Function VisitLambda(node As BoundLambda) As BoundNode
            Throw ExceptionUtilities.Unreachable
        End Function

        Private Function LowestCommonAncestor(gotoBlock As BoundNode, labelBlock As BoundNode) As BoundNode
            Dim gotoPath As New HashSet(Of BoundNode)

            gotoPath.Add(gotoBlock)
            While _analysis.blockParent.TryGetValue(gotoBlock, gotoBlock)
                gotoPath.Add(gotoBlock)
            End While

            Dim lca = labelBlock

            ' in a worst case we will find the top scope
            While Not gotoPath.Contains(lca)
                lca = _analysis.blockParent(lca)
            End While

            Return lca
        End Function

        ''' <summary>
        ''' It is illegal to jump into blocks that reference lifted variable
        ''' as that could leave closure frames of the target block uninitialized.
        ''' 
        ''' The fact that closure could be created as high as the declaration level of the variable
        ''' and well above goto block (thus making the jump safe) is considered an optional optimization 
        ''' and ignored. 
        ''' For the purpose of this analysis just having lifting lambdas already means 
        ''' that block may require initialization and cannot be jumped into.
        ''' 
        ''' Note that when you are jumping into a block you are essentially jumping into ALL blocks
        ''' on the path from LowestCommonAncestor(goto, label) to the actual label block.
        ''' </summary>
        Private Function IsLegalBranch(gotoBlock As BoundNode, labelBlock As BoundNode) As Boolean
            ' if goto block or any of its parents are same as label block we can jump.
            ' jumps OUT or WITHIN goto block are ok

            Dim curBlock = gotoBlock
            Do
                If labelBlock Is curBlock Then
                    Return True
                End If
            Loop While curBlock IsNot Nothing AndAlso _analysis.blockParent.TryGetValue(curBlock, curBlock)

            ' so, we are not jumping OUT or WITHIN a block.
            ' it may still be a valid jump if there are not lifting lambdas between 
            ' LeastCommonAncestor(goto, label) and label
            Dim commonAncestor = LowestCommonAncestor(gotoBlock, labelBlock)
            curBlock = labelBlock
            Do
                If curBlock Is commonAncestor Then
                    ' reached common ancestor and found no blocks that might initialize closures 
                    Return True
                End If

                If _analysis.containsLiftingLambda.Contains(curBlock) Then
                    ' this block contains a lambda that lifts. 
                    ' we cannot guarantee closure initialization so this is not a valid jump
                    Return False
                End If

                _analysis.blockParent.TryGetValue(curBlock, curBlock)
            Loop
        End Function

        Public Overrides Function VisitGotoStatement(node As BoundGotoStatement) As BoundNode
            Dim label = node.Label
            Dim labelBlock As BoundNode = Nothing

            ' label may not exist or not recorded if there were errors
            If label IsNot Nothing AndAlso _analysis.labelBlock.TryGetValue(node.Label, labelBlock) Then

                Dim gotoBlock As BoundNode = Nothing
                If _analysis.gotoBlock.TryGetValue(node, gotoBlock) Then
                    Dim isLegal = IsLegalBranch(gotoBlock, labelBlock)

                    If Not isLegal Then
                        Dim resumeLabel = TryCast(label, GeneratedUnstructuredExceptionHandlingResumeLabel)

                        If resumeLabel IsNot Nothing Then
                            If Not reported_ERR_CannotUseOnErrorGotoWithClosure Then
                                reported_ERR_CannotUseOnErrorGotoWithClosure = True
                                Me.Diagnostics.Add(ERRID.ERR_CannotUseOnErrorGotoWithClosure, resumeLabel.ResumeStatement.GetLocation(),
                                                   resumeLabel.ResumeStatement.ToString())
                            End If
                        Else
                            Select Case node.Syntax.Kind
                                Case SyntaxKind.ResumeLabelStatement, SyntaxKind.OnErrorGoToLabelStatement
                                    Me.Diagnostics.Add(ERRID.ERR_CannotGotoNonScopeBlocksWithClosure, node.Syntax.GetLocation(),
                                                       node.Syntax.ToString(), String.Empty, label.Name)
                                Case Else
                                    Me.Diagnostics.Add(ERRID.ERR_CannotGotoNonScopeBlocksWithClosure, node.Syntax.GetLocation(), "Goto ", label.Name, label.Name)
                            End Select
                        End If

                        node = New BoundGotoStatement(node.Syntax, node.Label, node.LabelExpressionOpt, True)
                    End If
                End If
            End If

            Return MyBase.VisitGotoStatement(node)
        End Function

        Public Overrides Function VisitCall(node As BoundCall) As BoundNode
            Dim rewritten As BoundNode = MyBase.VisitCall(node)

            If rewritten.Kind = BoundKind.Call Then
                Dim rewrittenCall As BoundCall = DirectCast(rewritten, BoundCall)
                Dim rewrittenMethod As MethodSymbol = rewrittenCall.Method
                Dim rewrittenReceiverOpt As BoundExpression = rewrittenCall.ReceiverOpt
                Dim rewrittenArguments As ImmutableArray(Of BoundExpression) = rewrittenCall.Arguments

                rewrittenCall = OptimizeMethodCallForDelegateInvoke(rewrittenCall, rewrittenMethod, rewrittenReceiverOpt, rewrittenArguments)

                ' Check if we need to init Me proxy and this is a ctor call
                If thisProxyInitDeferred IsNot Nothing AndAlso _currentMethod Is _topLevelMethod Then
                    Dim receiver As BoundExpression = node.ReceiverOpt
                    ' are we calling a ctor on Me or MyBase?
                    If node.Method.MethodKind = MethodKind.Constructor AndAlso receiver IsNot Nothing AndAlso receiver.IsInstanceReference Then
                        Return LocalRewriter.GenerateSequenceValueSideEffects(Me._currentMethod,
                                                                              rewrittenCall,
                                                                              ImmutableArray(Of LocalSymbol).Empty,
                                                                              ImmutableArray.Create(Of BoundExpression)(thisProxyInitDeferred))
                    End If
                End If

                Return rewrittenCall
            End If

            Return rewritten
        End Function

        Public Overrides Function VisitLoweredConditionalAccess(node As BoundLoweredConditionalAccess) As BoundNode
            Dim result = DirectCast(MyBase.VisitLoweredConditionalAccess(node), BoundLoweredConditionalAccess)

            If Not result.CaptureReceiver AndAlso Not node.ReceiverOrCondition.Type.IsBooleanType() AndAlso
               node.ReceiverOrCondition.Kind <> result.ReceiverOrCondition.Kind Then
                ' It looks like the receiver got lifted into a closure, we cannot assume that it will not change between null check and the following access.
                Return result.Update(result.ReceiverOrCondition, True, result.PlaceholderId, result.WhenNotNull, result.WhenNullOpt, result.Type)
            Else
                Return result
            End If
        End Function

        ''' <summary>
        ''' Optimize the case where we create an instance of a delegate and invoke it right away.
        ''' Skip the delegate creation and invoke the method directly. Specifically, we are targeting 
        ''' lambda relaxation scenario that requires a stub, which invokes original lambda by instantiating
        ''' an Anonymous Delegate and calling its Invoke method. That is why this optimization should be done
        ''' after lambdas are rewritten.
        ''' CONSIDER: Should we expand this optimization to all delegate types and all explicitly written code?
        '''           If we decide to do this, we should be careful with extension methods because they have
        '''           special treatment of 'this' parameter. 
        ''' </summary>
        Private Function OptimizeMethodCallForDelegateInvoke(node As BoundCall, method As MethodSymbol, receiver As BoundExpression, arguments As ImmutableArray(Of BoundExpression)) As BoundCall

            Dim useSiteDiagnostics As HashSet(Of DiagnosticInfo) = Nothing

            If method.MethodKind = MethodKind.DelegateInvoke AndAlso
                    method.ContainingType.IsAnonymousType AndAlso
                    receiver.Kind = BoundKind.DelegateCreationExpression AndAlso
                    Conversions.ClassifyMethodConversionForLambdaOrAnonymousDelegate(
                        method, DirectCast(receiver, BoundDelegateCreationExpression).Method, useSiteDiagnostics) = MethodConversionKind.Identity Then
                Diagnostics.Add(node, useSiteDiagnostics)

                Dim delegateCreation = DirectCast(receiver, BoundDelegateCreationExpression)
                Debug.Assert(delegateCreation.RelaxationLambdaOpt Is Nothing AndAlso delegateCreation.RelaxationReceiverPlaceholderOpt Is Nothing)

                If Not delegateCreation.Method.IsReducedExtensionMethod Then
                    method = delegateCreation.Method
                    receiver = delegateCreation.ReceiverOpt
                    node = node.Update(method, Nothing, receiver, arguments, Nothing, node.SuppressObjectClone, node.Type)
                End If
            End If

            Return node
        End Function

    End Class
End Namespace

