// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#if DEBUG
//#define CHECK_LOCALS // define CHECK_LOCALS to help debug some rewriting problems that would otherwise cause code-gen failures

#endif
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.CodeAnalysis.CodeGen;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp
{
    /// <summary>
    /// The rewriter for removing lambda expressions from method bodies and introducing closure classes
    /// as containers for captured variables along the lines of the example in section 6.5.3 of the
    /// C# language specification.
    /// 
    /// The entry point is the public method <see cref="Rewrite"/>.  It operates as follows:
    /// 
    /// First, an analysis of the whole method body is performed that determines which variables are
    /// captured, what their scopes are, and what the nesting relationship is between scopes that
    /// have captured variables.  The result of this analysis is left in <see cref="_analysis"/>.
    /// 
    /// Then we make a frame, or compiler-generated class, represented by an instance of
    /// <see cref="LambdaFrame"/> for each scope with captured variables.  The generated frames are kept
    /// in <see cref="_frames"/>.  Each frame is given a single field for each captured
    /// variable in the corresponding scope.  These are maintained in <see cref="MethodToClassRewriter.proxies"/>.
    /// 
    /// Finally, we walk and rewrite the input bound tree, keeping track of the following:
    /// (1) The current set of active frame pointers, in <see cref="_framePointers"/>
    /// (2) The current method being processed (this changes within a lambda's body), in <see cref="_currentMethod"/>
    /// (3) The "this" symbol for the current method in <see cref="_currentFrameThis"/>, and
    /// (4) The symbol that is used to access the innermost frame pointer (it could be a local variable or "this" parameter)
    /// 
    /// There are a few key transformations done in the rewriting.
    /// (1) Lambda expressions are turned into delegate creation expressions, and the body of the lambda is
    ///     moved into a new, compiler-generated method of a selected frame class.
    /// (2) On entry to a scope with captured variables, we create a frame object and store it in a local variable.
    /// (3) References to captured variables are transformed into references to fields of a frame class.
    /// 
    /// In addition, the rewriting deposits into <see cref="TypeCompilationState.SynthesizedMethods"/> a (<see cref="MethodSymbol"/>, <see cref="BoundStatement"/>)
    /// pair for each generated method.
    /// 
    /// <see cref="Rewrite"/> produces its output in two forms.  First, it returns a new bound statement
    /// for the caller to use for the body of the original method.  Second, it returns a collection of
    /// (<see cref="MethodSymbol"/>, <see cref="BoundStatement"/>) pairs for additional methods that the lambda rewriter produced.
    /// These additional methods contain the bodies of the lambdas moved into ordinary methods of their
    /// respective frame classes, and the caller is responsible for processing them just as it does with
    /// the returned bound node.  For example, the caller will typically perform iterator method and
    /// asynchronous method transformations, and emit IL instructions into an assembly.
    /// </summary>
    internal sealed partial class LambdaRewriter : MethodToClassRewriter
    {
        private readonly Analysis _analysis;
        private readonly MethodSymbol _topLevelMethod;
        private readonly MethodSymbol _substitutedSourceMethod;
        private readonly int _topLevelMethodOrdinal;

        // lambda frame for static lambdas. 
        // initialized lazily and could be null if there are no static lambdas
        private LambdaFrame _lazyStaticLambdaFrame;

        // A mapping from every lambda parameter to its corresponding method's parameter.
        private readonly Dictionary<ParameterSymbol, ParameterSymbol> _parameterMap = new Dictionary<ParameterSymbol, ParameterSymbol>();

        // A mapping from every local function to its lowered method
        private struct MappedLocalFunction
        {
            public readonly SynthesizedLambdaMethod Symbol;
            public readonly ClosureKind ClosureKind;
            public MappedLocalFunction(SynthesizedLambdaMethod symbol, ClosureKind closureKind)
            {
                Symbol = symbol;
                ClosureKind = closureKind;
            }
        }
        private readonly Dictionary<LocalFunctionSymbol, MappedLocalFunction> _localFunctionMap = new Dictionary<LocalFunctionSymbol, MappedLocalFunction>();

        // for each block with lifted (captured) variables, the corresponding frame type
        private readonly Dictionary<BoundNode, LambdaFrame> _frames = new Dictionary<BoundNode, LambdaFrame>();

        // the current set of frame pointers in scope.  Each is either a local variable (where introduced),
        // or the "this" parameter when at the top level.  Keys in this map are never constructed types.
        private readonly Dictionary<NamedTypeSymbol, Symbol> _framePointers = new Dictionary<NamedTypeSymbol, Symbol>();

        // True if the rewritten tree should include assignments of the
        // original locals to the lifted proxies. This is only useful for the
        // expression evaluator where the original locals are left as is.
        private readonly bool _assignLocals;

        // The current method or lambda being processed.
        private MethodSymbol _currentMethod;

        // The "this" symbol for the current method.
        private ParameterSymbol _currentFrameThis;

        private readonly ArrayBuilder<LambdaDebugInfo> _lambdaDebugInfoBuilder;

        // ID dispenser for field names of frame references
        private int _synthesizedFieldNameIdDispenser;

        // The symbol (field or local) holding the innermost frame
        private Symbol _innermostFramePointer;

        // The mapping of type parameters for the current lambda body
        private TypeMap _currentLambdaBodyTypeMap;

        // The current set of type parameters (mapped from the enclosing method's type parameters)
        private ImmutableArray<TypeParameterSymbol> _currentTypeParameters;

        // Initialization for the proxy of the upper frame if it needs to be deferred.
        // Such situation happens when lifting this in a ctor.
        private BoundExpression _thisProxyInitDeferred;

        // Set to true once we've seen the base (or self) constructor invocation in a constructor
        private bool _seenBaseCall;

        // Set to true while translating code inside of an expression lambda.
        private bool _inExpressionLambda;

        // When a lambda captures only 'this' of the enclosing method, we cache it in a local
        // variable.  This is the set of such local variables that must be added to the enclosing
        // method's top-level block.
        private ArrayBuilder<LocalSymbol> _addedLocals;

        // Similarly, this is the set of statements that must be added to the enclosing method's
        // top-level block initializing those variables to null.
        private ArrayBuilder<BoundStatement> _addedStatements;

        private LambdaRewriter(
            Analysis analysis,
            NamedTypeSymbol thisType,
            ParameterSymbol thisParameterOpt,
            MethodSymbol method,
            int methodOrdinal,
            MethodSymbol substitutedSourceMethod,
            ArrayBuilder<LambdaDebugInfo> lambdaDebugInfoBuilder,
            VariableSlotAllocator slotAllocatorOpt,
            TypeCompilationState compilationState,
            DiagnosticBag diagnostics,
            bool assignLocals)
            : base(slotAllocatorOpt, compilationState, diagnostics)
        {
            Debug.Assert(analysis != null);
            Debug.Assert(thisType != null);
            Debug.Assert(method != null);
            Debug.Assert(compilationState != null);
            Debug.Assert(diagnostics != null);

            _topLevelMethod = method;
            _substitutedSourceMethod = substitutedSourceMethod;
            _topLevelMethodOrdinal = methodOrdinal;
            _lambdaDebugInfoBuilder = lambdaDebugInfoBuilder;
            _currentMethod = method;
            _analysis = analysis;
            _assignLocals = assignLocals;
            _currentTypeParameters = method.TypeParameters;
            _currentLambdaBodyTypeMap = TypeMap.Empty;
            _innermostFramePointer = _currentFrameThis = thisParameterOpt;
            _framePointers[thisType] = thisParameterOpt;
            _seenBaseCall = method.MethodKind != MethodKind.Constructor; // only used for ctors
            _synthesizedFieldNameIdDispenser = 1;
        }

        protected override bool NeedsProxy(Symbol localOrParameter)
        {
            Debug.Assert(localOrParameter is LocalSymbol || localOrParameter is ParameterSymbol ||
                (localOrParameter as MethodSymbol)?.MethodKind == MethodKind.LocalFunction);
            return _analysis.CapturedVariables.ContainsKey(localOrParameter);
        }

        /// <summary>
        /// Rewrite the given node to eliminate lambda expressions.  Also returned are the method symbols and their
        /// bound bodies for the extracted lambda bodies. These would typically be emitted by the caller such as
        /// MethodBodyCompiler.  See this class' documentation
        /// for a more thorough explanation of the algorithm and its use by clients.
        /// </summary>
        /// <param name="loweredBody">The bound node to be rewritten</param>
        /// <param name="thisType">The type of the top-most frame</param>
        /// <param name="thisParameter">The "this" parameter in the top-most frame, or null if static method</param>
        /// <param name="method">The containing method of the node to be rewritten</param>
        /// <param name="methodOrdinal">Index of the method symbol in its containing type member list.</param>
        /// <param name="substitutedSourceMethod">If this is non-null, then <paramref name="method"/> will be treated as this for uses of parent symbols. For use in EE.</param>
        /// <param name="lambdaDebugInfoBuilder">Information on lambdas defined in <paramref name="method"/> needed for debugging.</param>
        /// <param name="closureDebugInfoBuilder">Information on closures defined in <paramref name="method"/> needed for debugging.</param>
        /// <param name="slotAllocatorOpt">Slot allocator.</param>
        /// <param name="compilationState">The caller's buffer into which we produce additional methods to be emitted by the caller</param>
        /// <param name="diagnostics">Diagnostic bag for diagnostics</param>
        /// <param name="assignLocals">The rewritten tree should include assignments of the original locals to the lifted proxies</param>
        public static BoundStatement Rewrite(
            BoundStatement loweredBody,
            NamedTypeSymbol thisType,
            ParameterSymbol thisParameter,
            MethodSymbol method,
            int methodOrdinal,
            MethodSymbol substitutedSourceMethod,
            ArrayBuilder<LambdaDebugInfo> lambdaDebugInfoBuilder,
            ArrayBuilder<ClosureDebugInfo> closureDebugInfoBuilder,
            VariableSlotAllocator slotAllocatorOpt,
            TypeCompilationState compilationState,
            DiagnosticBag diagnostics,
            bool assignLocals)
        {
            Debug.Assert((object)thisType != null);
            Debug.Assert(((object)thisParameter == null) || (thisParameter.Type == thisType));
            Debug.Assert(compilationState.ModuleBuilderOpt != null);

            var analysis = Analysis.Analyze(loweredBody, method);
            if (!analysis.SeenLambda)
            {
                // Unreachable anonymous functions are ignored by the analyzer.
                // No closures or lambda methods are generated.
                // E.g. 
                //   int y = 0;
                //   var b = false && (from z in new X(y) select f(z + y))
                return loweredBody;
            }

            CheckLocalsDefined(loweredBody);
            var rewriter = new LambdaRewriter(
                analysis,
                thisType,
                thisParameter,
                method,
                methodOrdinal,
                substitutedSourceMethod,
                lambdaDebugInfoBuilder,
                slotAllocatorOpt,
                compilationState,
                diagnostics,
                assignLocals);

            analysis.ComputeLambdaScopesAndFrameCaptures();
            rewriter.MakeFrames(closureDebugInfoBuilder);
            var body = rewriter.AddStatementsIfNeeded((BoundStatement)rewriter.Visit(loweredBody));
            CheckLocalsDefined(body);

            return body;
        }

        private BoundStatement AddStatementsIfNeeded(BoundStatement body)
        {
            if (_addedLocals != null)
            {
                _addedStatements.Add(body);
                body = new BoundBlock(body.Syntax, _addedLocals.ToImmutableAndFree(), ImmutableArray<LocalFunctionSymbol>.Empty, _addedStatements.ToImmutableAndFree()) { WasCompilerGenerated = true };
                _addedLocals = null;
                _addedStatements = null;
            }
            else
            {
                Debug.Assert(_addedStatements == null);
            }

            return body;
        }

        protected override TypeMap TypeMap
        {
            get { return _currentLambdaBodyTypeMap; }
        }

        protected override MethodSymbol CurrentMethod
        {
            get { return _currentMethod; }
        }

        protected override NamedTypeSymbol ContainingType
        {
            get { return _topLevelMethod.ContainingType; }
        }

        /// <summary>
        /// Check that the top-level node is well-defined, in the sense that all
        /// locals that are used are defined in some enclosing scope.
        /// </summary>
        static partial void CheckLocalsDefined(BoundNode node);

        /// <summary>
        /// Create the frame types.
        /// </summary>
        private void MakeFrames(ArrayBuilder<ClosureDebugInfo> closureDebugInfo)
        {
            NamedTypeSymbol containingType = this.ContainingType;

            foreach (var kvp in _analysis.CapturedVariables)
            {
                var captured = kvp.Key;

                BoundNode scope;
                if (!_analysis.VariableScope.TryGetValue(captured, out scope))
                {
                    continue;
                }

                LambdaFrame frame = GetFrameForScope(scope, closureDebugInfo);

                if (captured.Kind != SymbolKind.Method)
                {
                var hoistedField = LambdaCapturedVariable.Create(frame, captured, ref _synthesizedFieldNameIdDispenser);
                proxies.Add(captured, new CapturedToFrameSymbolReplacement(hoistedField, isReusable: false));
                CompilationState.ModuleBuilderOpt.AddSynthesizedDefinition(frame, hoistedField);

                if (hoistedField.Type.IsRestrictedType())
                {
                    foreach (CSharpSyntaxNode syntax in kvp.Value)
                    {
                        // CS4013: Instance of type '{0}' cannot be used inside an anonymous function, query expression, iterator block or async method
                        this.Diagnostics.Add(ErrorCode.ERR_SpecialByRefInLambda, syntax.Location, hoistedField.Type);
                    }
                }
            }
        }
        }

        private LambdaFrame GetFrameForScope(BoundNode scope, ArrayBuilder<ClosureDebugInfo> closureDebugInfo)
        {
            LambdaFrame frame;
            if (!_frames.TryGetValue(scope, out frame))
            {
                var syntax = scope.Syntax;
                Debug.Assert(syntax != null);

                DebugId methodId = GetTopLevelMethodId();
                DebugId closureId = GetClosureId(syntax, closureDebugInfo);

                var canBeStruct = !_analysis.ScopesThatCantBeStructs.Contains(scope);

                var containingMethod = _analysis.ScopeOwner[scope];
                if (_substitutedSourceMethod != null && containingMethod == _topLevelMethod)
                {
                    containingMethod = _substitutedSourceMethod;
                }
                frame = new LambdaFrame(_topLevelMethod, containingMethod, canBeStruct, syntax, methodId, closureId);
                _frames.Add(scope, frame);

                CompilationState.ModuleBuilderOpt.AddSynthesizedDefinition(this.ContainingType, frame);
                if (frame.Constructor != null)
                {
                CompilationState.AddSynthesizedMethod(
                    frame.Constructor,
                    FlowAnalysisPass.AppendImplicitReturn(
                        MethodCompiler.BindMethodBody(frame.Constructor, CompilationState, null),
                        frame.Constructor));
            }
            }

            return frame;
        }

        private LambdaFrame GetStaticFrame(DiagnosticBag diagnostics, IBoundLambdaOrFunction lambda)
        {
            if (_lazyStaticLambdaFrame == null)
            {
                var isNonGeneric = !_topLevelMethod.IsGenericMethod;
                if (isNonGeneric)
                {
                    _lazyStaticLambdaFrame = CompilationState.StaticLambdaFrame;
                }

                if (_lazyStaticLambdaFrame == null)
                {
                    DebugId methodId;
                    if (isNonGeneric)
                    {
                        methodId = new DebugId(DebugId.UndefinedOrdinal, CompilationState.ModuleBuilderOpt.CurrentGenerationOrdinal);
                    }
                    else
                    {
                        methodId = GetTopLevelMethodId();
                    }

                    DebugId closureId = default(DebugId);
                    // using _topLevelMethod as containing member because the static frame does not have generic parameters, except for the top level method's
                    var containingMethod = isNonGeneric ? null : (_substitutedSourceMethod ?? _topLevelMethod);
                    _lazyStaticLambdaFrame = new LambdaFrame(_topLevelMethod, containingMethod, isStruct: false, scopeSyntaxOpt: null, methodId: methodId, closureId: closureId);

                    // non-generic static lambdas can share the frame
                    if (isNonGeneric)
                    {
                        CompilationState.StaticLambdaFrame = _lazyStaticLambdaFrame;
                    }

                    var frame = _lazyStaticLambdaFrame;

                    // add frame type
                    CompilationState.ModuleBuilderOpt.AddSynthesizedDefinition(this.ContainingType, frame);

                    // add its ctor (note Constructor can be null if TypeKind.Struct is passed in to LambdaFrame.ctor, but Class is passed in above)
                    CompilationState.AddSynthesizedMethod(
                        frame.Constructor,
                        FlowAnalysisPass.AppendImplicitReturn(
                            MethodCompiler.BindMethodBody(frame.Constructor, CompilationState, null),
                            frame.Constructor));

                    // associate the frame with the first lambda that caused it to exist. 
                    // we need to associate this with some syntax.
                    // unfortunately either containing method or containing class could be synthetic
                    // therefore could have no syntax.
                    CSharpSyntaxNode syntax = lambda.Syntax;

                    // add cctor
                    // Frame.inst = new Frame()
                    var F = new SyntheticBoundNodeFactory(frame.StaticConstructor, syntax, CompilationState, diagnostics);
                    var body = F.Block(
                            F.Assignment(
                                F.Field(null, frame.SingletonCache),
                                F.New(frame.Constructor)),
                            new BoundReturnStatement(syntax, RefKind.None, null));

                    CompilationState.AddSynthesizedMethod(frame.StaticConstructor, body);
                }
            }

            return _lazyStaticLambdaFrame;
        }

        /// <summary>
        /// Produce a bound expression representing a pointer to a frame of a particular frame type.
        /// </summary>
        /// <param name="syntax">The syntax to attach to the bound nodes produced</param>
        /// <param name="frameType">The type of frame to be returned</param>
        /// <returns>A bound node that computes the pointer to the required frame</returns>
        private BoundExpression FrameOfType(CSharpSyntaxNode syntax, NamedTypeSymbol frameType)
        {
            BoundExpression result = FramePointer(syntax, frameType.OriginalDefinition);
            Debug.Assert(result.Type == frameType);
            return result;
        }

        /// <summary>
        /// Produce a bound expression representing a pointer to a frame of a particular frame class.
        /// Note that for generic frames, the frameClass parameter is the generic definition, but
        /// the resulting expression will be constructed with the current type parameters.
        /// </summary>
        /// <param name="syntax">The syntax to attach to the bound nodes produced</param>
        /// <param name="frameClass">The class type of frame to be returned</param>
        /// <returns>A bound node that computes the pointer to the required frame</returns>
        protected override BoundExpression FramePointer(CSharpSyntaxNode syntax, NamedTypeSymbol frameClass)
        {
            Debug.Assert(frameClass.IsDefinition);

            // If in an instance method of the right type, we can just return the "this" pointer.
            if ((object)_currentFrameThis != null && _currentFrameThis.Type == frameClass)
            {
                return new BoundThisReference(syntax, frameClass);
            }

            // If the current method has by-ref struct closure parameters, and one of them is correct, use it.
            var lambda = _currentMethod as SynthesizedLambdaMethod;
            if (lambda != null)
            {
                var start = lambda.ParameterCount - lambda.ExtraSynthesizedParameterCount;
                for (var i = start; i < lambda.ParameterCount; i++)
                {
                    var potentialParameter = lambda.Parameters[i];
                    if (potentialParameter.Type.OriginalDefinition == frameClass)
                    {
                        return new BoundParameter(syntax, potentialParameter);
                    }
                }
            }

            // Otherwise we need to return the value from a frame pointer local variable...
            Symbol framePointer = _framePointers[frameClass];
            CapturedSymbolReplacement proxyField;
            if (proxies.TryGetValue(framePointer, out proxyField))
            {
                // However, frame pointer local variables themselves can be "captured".  In that case
                // the inner frames contain pointers to the enclosing frames.  That is, nested
                // frame pointers are organized in a linked list.
                return proxyField.Replacement(syntax, frameType => FramePointer(syntax, frameType));
            }

            var localFrame = framePointer as LocalSymbol;
            return new BoundLocal(syntax, localFrame, null, localFrame.Type);
        }

        private static void InsertAndFreePrologue(ArrayBuilder<BoundStatement> result, ArrayBuilder<BoundExpression> prologue)
        {
            foreach (var expr in prologue)
            {
                result.Add(new BoundExpressionStatement(expr.Syntax, expr));
            }

            prologue.Free();
        }

        /// <summary>
        /// Introduce a frame around the translation of the given node.
        /// </summary>
        /// <param name="node">The node whose translation should be translated to contain a frame</param>
        /// <param name="frame">The frame for the translated node</param>
        /// <param name="F">A function that computes the translation of the node.  It receives lists of added statements and added symbols</param>
        /// <returns>The translated statement, as returned from F</returns>
        private T IntroduceFrame<T>(BoundNode node, LambdaFrame frame, Func<ArrayBuilder<BoundExpression>, ArrayBuilder<LocalSymbol>, T> F)
        {
            var frameTypeParameters = ImmutableArray.Create(StaticCast<TypeSymbol>.From(_currentTypeParameters).SelectAsArray(TypeMap.TypeSymbolAsTypeWithModifiers), 0, frame.Arity);
            NamedTypeSymbol frameType = frame.ConstructIfGeneric(frameTypeParameters);

            Debug.Assert(frame.ScopeSyntaxOpt != null);
            LocalSymbol framePointer = new SynthesizedLocal(_topLevelMethod, frameType, SynthesizedLocalKind.LambdaDisplayClass, frame.ScopeSyntaxOpt);

            CSharpSyntaxNode syntax = node.Syntax;

            // assign new frame to the frame variable

            var prologue = ArrayBuilder<BoundExpression>.GetInstance();

            BoundExpression newFrame;
            if (frame.Constructor == null)
            {
                Debug.Assert(frame.TypeKind == TypeKind.Struct);
                newFrame = new BoundDefaultOperator(syntax: syntax, type: frameType);
            }
            else
            {
            MethodSymbol constructor = frame.Constructor.AsMember(frameType);
            Debug.Assert(frameType == constructor.ContainingType);
                newFrame = new BoundObjectCreationExpression(
                syntax: syntax,
                constructor: constructor);
            }

            prologue.Add(new BoundAssignmentOperator(syntax,
                new BoundLocal(syntax, framePointer, null, frameType),
                newFrame,
                frameType));

            CapturedSymbolReplacement oldInnermostFrameProxy = null;
            if ((object)_innermostFramePointer != null)
            {
                proxies.TryGetValue(_innermostFramePointer, out oldInnermostFrameProxy);
                if (_analysis.NeedsParentFrame.Contains(node))
                {
                    var capturedFrame = LambdaCapturedVariable.Create(frame, _innermostFramePointer, ref _synthesizedFieldNameIdDispenser);
                    FieldSymbol frameParent = capturedFrame.AsMember(frameType);
                    BoundExpression left = new BoundFieldAccess(syntax, new BoundLocal(syntax, framePointer, null, frameType), frameParent, null);
                    BoundExpression right = FrameOfType(syntax, frameParent.Type as NamedTypeSymbol);
                    BoundExpression assignment = new BoundAssignmentOperator(syntax, left, right, left.Type);

                    if (_currentMethod.MethodKind == MethodKind.Constructor && capturedFrame.Type == _currentMethod.ContainingType && !_seenBaseCall)
                    {
                        // Containing method is a constructor 
                        // Initialization statement for the "this" proxy must be inserted
                        // after the constructor initializer statement block
                        // This insertion will be done by the delegate F
                        Debug.Assert(_thisProxyInitDeferred == null);
                        _thisProxyInitDeferred = assignment;
                    }
                    else
                    {
                        prologue.Add(assignment);
                    }

                    if (CompilationState.Emitting)
                    {
                        Debug.Assert(capturedFrame.Type.IsReferenceType); // Make sure we're not accidentally capturing a struct by value
                        CompilationState.ModuleBuilderOpt.AddSynthesizedDefinition(frame, capturedFrame);
                    }

                    proxies[_innermostFramePointer] = new CapturedToFrameSymbolReplacement(capturedFrame, isReusable: false);
                }
            }

            // Capture any parameters of this block.  This would typically occur
            // at the top level of a method or lambda with captured parameters.
            // TODO: speed up the following by computing it in analysis.
            foreach (var variable in _analysis.CapturedVariables.Keys)
            {
                BoundNode varNode;
                if (!_analysis.VariableScope.TryGetValue(variable, out varNode) || varNode != node)
                {
                    continue;
                }

                InitVariableProxy(syntax, variable, framePointer, prologue);
            }

            Symbol oldInnermostFramePointer = _innermostFramePointer;
            _innermostFramePointer = framePointer;
            var addedLocals = ArrayBuilder<LocalSymbol>.GetInstance();
            addedLocals.Add(framePointer);
            _framePointers.Add(frame, framePointer);

            var result = F(prologue, addedLocals);

            _framePointers.Remove(frame);
            _innermostFramePointer = oldInnermostFramePointer;

            if ((object)_innermostFramePointer != null)
            {
                if (oldInnermostFrameProxy != null)
                {
                    proxies[_innermostFramePointer] = oldInnermostFrameProxy;
                }
                else
                {
                    proxies.Remove(_innermostFramePointer);
                }
            }

            return result;
        }

        private void InitVariableProxy(CSharpSyntaxNode syntax, Symbol symbol, LocalSymbol framePointer, ArrayBuilder<BoundExpression> prologue)
        {
            CapturedSymbolReplacement proxy;
            if (proxies.TryGetValue(symbol, out proxy))
            {
                BoundExpression value;
                switch (symbol.Kind)
                {
                    case SymbolKind.Parameter:
                        var parameter = (ParameterSymbol)symbol;
                        ParameterSymbol parameterToUse;
                        if (!_parameterMap.TryGetValue(parameter, out parameterToUse))
                        {
                            parameterToUse = parameter;
                        }

                        value = new BoundParameter(syntax, parameterToUse);
                        break;

                    case SymbolKind.Local:
                        if (!_assignLocals)
                        {
                            return;
                        }

                        var local = (LocalSymbol)symbol;
                        LocalSymbol localToUse;
                        if (!localMap.TryGetValue(local, out localToUse))
                        {
                            localToUse = local;
                        }

                        value = new BoundLocal(syntax, localToUse, null, localToUse.Type);
                        break;

                    default:
                        throw ExceptionUtilities.UnexpectedValue(symbol.Kind);
                }

                var left = proxy.Replacement(syntax, frameType1 => new BoundLocal(syntax, framePointer, null, framePointer.Type));
                var assignToProxy = new BoundAssignmentOperator(syntax, left, value, value.Type);
                prologue.Add(assignToProxy);
            }
        }

        #region Visit Methods

        protected override BoundNode VisitUnhoistedParameter(BoundParameter node)
        {
            ParameterSymbol replacementParameter;
            if (_parameterMap.TryGetValue(node.ParameterSymbol, out replacementParameter))
            {
                return new BoundParameter(node.Syntax, replacementParameter, replacementParameter.Type, node.HasErrors);
            }

            return base.VisitUnhoistedParameter(node);
        }

        public override BoundNode VisitThisReference(BoundThisReference node)
        {
            // "topLevelMethod.ThisParameter == null" can occur in a delegate creation expression because the method group
            // in the argument can have a "this" receiver even when "this"
            // is not captured because a static method is selected.  But we do preserve
            // the method group and its receiver in the bound tree.
            // No need to capture "this" in such case.

            // TODO: Why don't we drop "this" while lowering if method is static? 
            //       Actually, considering that method group expression does not evaluate to a particular value 
            //       why do we have it in the lowered tree at all?

            return (_currentMethod == _topLevelMethod || _topLevelMethod.ThisParameter == null ?
                node :
                FramePointer(node.Syntax, (NamedTypeSymbol)node.Type));
        }

        public override BoundNode VisitBaseReference(BoundBaseReference node)
        {
            return (!_currentMethod.IsStatic && _currentMethod.ContainingType == _topLevelMethod.ContainingType)
                ? node
                : FramePointer(node.Syntax, _topLevelMethod.ContainingType); // technically, not the correct static type
        }

        private void RemapLambdaOrLocalFunction(
            CSharpSyntaxNode syntax,
            MethodSymbol originalMethod,
            ImmutableArray<TypeSymbol> typeArgumentsOpt,
            ClosureKind closureKind,
            ref MethodSymbol synthesizedMethod,
            out BoundExpression receiver,
            out NamedTypeSymbol constructedFrame)
        {
            var translatedLambdaContainer = synthesizedMethod.ContainingType;
            var containerAsFrame = translatedLambdaContainer as LambdaFrame;

            // All of _currentTypeParameters might not be preserved here due to recursively calling upwards in the chain of local functions/lambdas
            Debug.Assert((typeArgumentsOpt.IsDefault && !originalMethod.IsGenericMethod) || (typeArgumentsOpt.Length == originalMethod.Arity));
            var totalTypeArgumentCount = (containerAsFrame?.Arity ?? 0) + synthesizedMethod.Arity;
            var realTypeArguments = ImmutableArray.Create(StaticCast<TypeSymbol>.From(_currentTypeParameters), 0, totalTypeArgumentCount - originalMethod.Arity);
            if (!typeArgumentsOpt.IsDefault)
            {
                realTypeArguments = realTypeArguments.Concat(typeArgumentsOpt);
            }

            if (containerAsFrame != null && containerAsFrame.Arity != 0)
            {
                var containerTypeArguments = ImmutableArray.Create(realTypeArguments, 0, containerAsFrame.Arity);
                realTypeArguments = ImmutableArray.Create(realTypeArguments, containerAsFrame.Arity, realTypeArguments.Length - containerAsFrame.Arity);
                constructedFrame = containerAsFrame.Construct(containerTypeArguments);
            }
            else
            {
                constructedFrame = translatedLambdaContainer;
            }

            // for instance lambdas, receiver is the frame
            // for static lambdas, get the singleton receiver
            if (closureKind == ClosureKind.Singleton)
            {
                var field = containerAsFrame.SingletonCache.AsMember(constructedFrame);
                receiver = new BoundFieldAccess(syntax, null, field, constantValueOpt: null);
            }
            else if (closureKind == ClosureKind.Static)
            {
                receiver = null;
            }
            else // ThisOnly and General
            {
                receiver = FrameOfType(syntax, constructedFrame);
            }

            synthesizedMethod = synthesizedMethod.AsMember(constructedFrame);
            if (synthesizedMethod.IsGenericMethod)
            {
                synthesizedMethod = synthesizedMethod.Construct(StaticCast<TypeSymbol>.From(realTypeArguments));
            }
            else
            {
                Debug.Assert(realTypeArguments.Length == 0);
            }
        }

        private void RemapLocalFunction(
            CSharpSyntaxNode syntax, MethodSymbol symbol,
            out BoundExpression receiver, out MethodSymbol method,
            ref ImmutableArray<BoundExpression> parameters,
            ImmutableArray<TypeSymbol> typeArguments = default(ImmutableArray<TypeSymbol>))
        {
            Debug.Assert(symbol.MethodKind == MethodKind.LocalFunction);

            var constructed = symbol as ConstructedMethodSymbol;
            if (constructed != null)
            {
                RemapLocalFunction(syntax, constructed.ConstructedFrom, out receiver, out method, ref parameters, this.TypeMap.SubstituteTypes(constructed.TypeArguments).SelectAsArray(t => t.Type));
                return;
            }

            var mappedLocalFunction = _localFunctionMap[(LocalFunctionSymbol)symbol];

            var lambda = mappedLocalFunction.Symbol;
            var frameCount = lambda.ExtraSynthesizedParameterCount;
            if (frameCount != 0)
            {
                Debug.Assert(!parameters.IsDefault);
                var builder = ArrayBuilder<BoundExpression>.GetInstance();
                builder.AddRange(parameters);
                var start = lambda.ParameterCount - frameCount;
                for (int i = start; i < lambda.ParameterCount; i++)
                {
                    // will always be a LambdaFrame, it's always a closure class
                    var frameType = (NamedTypeSymbol)lambda.Parameters[i].Type.OriginalDefinition;
                    if (frameType.IsGenericType)
                    {
                        var typeParameters = ((LambdaFrame)frameType).ConstructedFromTypeParameters;
                        var subst = this.TypeMap.SubstituteTypeParameters(typeParameters);
                        frameType = frameType.Construct(subst);
                    }
                    var frame = FrameOfType(syntax, frameType);
                    builder.Add(frame);
                }
                parameters = builder.ToImmutableAndFree();
            }

            method = lambda;
            NamedTypeSymbol constructedFrame;
            RemapLambdaOrLocalFunction(syntax, symbol, typeArguments, mappedLocalFunction.ClosureKind, ref method, out receiver, out constructedFrame);
        }

        public override BoundNode VisitCall(BoundCall node)
        {
            if (node.Method.MethodKind == MethodKind.LocalFunction)
            {
                BoundExpression receiver;
                MethodSymbol method;
                var arguments = node.Arguments;
                RemapLocalFunction(node.Syntax, node.Method, out receiver, out method, ref arguments);
                node = node.Update(receiver, method, arguments);
            }
            var visited = base.VisitCall(node);
            if (visited.Kind != BoundKind.Call)
            {
                return visited;
            }

            var rewritten = (BoundCall)visited;

            // Check if we need to init the 'this' proxy in a ctor call
            if (!_seenBaseCall)
            {
                _seenBaseCall = _currentMethod == _topLevelMethod && node.IsConstructorInitializer();
                if (_seenBaseCall && _thisProxyInitDeferred != null)
                {
                    // Insert the this proxy assignment after the ctor call.
                    // Create bound sequence: { ctor call, thisProxyInitDeferred }
                    return new BoundSequence(
                        syntax: node.Syntax,
                        locals: ImmutableArray<LocalSymbol>.Empty,
                        sideEffects: ImmutableArray.Create<BoundExpression>(rewritten),
                        value: _thisProxyInitDeferred,
                        type: rewritten.Type);
                }
            }

            return rewritten;
        }

        private BoundSequence RewriteSequence(BoundSequence node, ArrayBuilder<BoundExpression> prologue, ArrayBuilder<LocalSymbol> newLocals)
        {
            RewriteLocals(node.Locals, newLocals);

            foreach (var expr in node.SideEffects)
            {
                var replacement = (BoundExpression)this.Visit(expr);
                if (replacement != null) prologue.Add(replacement);
            }

            var newValue = (BoundExpression)this.Visit(node.Value);
            var newType = this.VisitType(node.Type);

            return node.Update(newLocals.ToImmutableAndFree(), prologue.ToImmutableAndFree(), newValue, newType);
        }

        public override BoundNode VisitBlock(BoundBlock node)
        {
            LambdaFrame frame;
            // Test if this frame has captured variables and requires the introduction of a closure class.
            if (_frames.TryGetValue(node, out frame))
            {
                return IntroduceFrame(node, frame, (ArrayBuilder<BoundExpression> prologue, ArrayBuilder<LocalSymbol> newLocals) =>
                    RewriteBlock(node, prologue, newLocals));
            }
            else
            {
                return RewriteBlock(node, ArrayBuilder<BoundExpression>.GetInstance(), ArrayBuilder<LocalSymbol>.GetInstance());
            }
        }

        private BoundBlock RewriteBlock(BoundBlock node, ArrayBuilder<BoundExpression> prologue, ArrayBuilder<LocalSymbol> newLocals)
        {
            RewriteLocals(node.Locals, newLocals);

            var newStatements = ArrayBuilder<BoundStatement>.GetInstance();

            if (prologue.Count > 0)
            {
                newStatements.Add(new BoundSequencePoint(null, null) { WasCompilerGenerated = true });
            }

            InsertAndFreePrologue(newStatements, prologue);

            foreach (var statement in node.Statements)
            {
                var replacement = (BoundStatement)this.Visit(statement);
                if (replacement != null)
                {
                    newStatements.Add(replacement);
                }
            }

            // TODO: we may not need to update if there was nothing to rewrite.
            return node.Update(newLocals.ToImmutableAndFree(), node.LocalFunctions, newStatements.ToImmutableAndFree());
        }

        public override BoundNode VisitCatchBlock(BoundCatchBlock node)
        {
            // Test if this frame has captured variables and requires the introduction of a closure class.
            LambdaFrame frame;
            if (_frames.TryGetValue(node, out frame))
            {
                return IntroduceFrame(node, frame, (ArrayBuilder<BoundExpression> prologue, ArrayBuilder<LocalSymbol> newLocals) =>
                {
                    return RewriteCatch(node, prologue, newLocals);
                });
            }
            else
            {
                return RewriteCatch(node, ArrayBuilder<BoundExpression>.GetInstance(), ArrayBuilder<LocalSymbol>.GetInstance());
            }
        }

        private BoundNode RewriteCatch(BoundCatchBlock node, ArrayBuilder<BoundExpression> prologue, ArrayBuilder<LocalSymbol> newLocals)
        {
            LocalSymbol newLocal;
            if ((object)node.LocalOpt != null && TryRewriteLocal(node.LocalOpt, out newLocal))
            {
                newLocals.Add(newLocal);
            }

            LocalSymbol rewrittenCatchLocal;

            if (newLocals.Count > 0)
            {
                // If the original LocalOpt was lifted into a closure,
                // the newLocals will contain a frame reference. In this case, 
                // instead of an actual local, catch will own the frame reference.

                Debug.Assert((object)node.LocalOpt != null && newLocals.Count == 1);
                rewrittenCatchLocal = newLocals[0];
            }
            else
            {
                Debug.Assert((object)node.LocalOpt == null);
                rewrittenCatchLocal = null;
            }

            // If exception variable got lifted, IntroduceFrame will give us frame init prologue.
            // It needs to run before the exception variable is accessed.
            // To ensure that, we will make exception variable a sequence that performs prologue as its side-effects.
            BoundExpression rewrittenExceptionSource = null;
            var rewrittenFilter = (BoundExpression)this.Visit(node.ExceptionFilterOpt);
            if (node.ExceptionSourceOpt != null)
            {
                rewrittenExceptionSource = (BoundExpression)Visit(node.ExceptionSourceOpt);
                if (prologue.Count > 0)
                {
                    rewrittenExceptionSource = new BoundSequence(
                        rewrittenExceptionSource.Syntax,
                        ImmutableArray.Create<LocalSymbol>(),
                        prologue.ToImmutable(),
                        rewrittenExceptionSource,
                        rewrittenExceptionSource.Type);
                }
            }
            else if (prologue.Count > 0)
            {
                Debug.Assert(rewrittenFilter != null);
                rewrittenFilter = new BoundSequence(
                    rewrittenFilter.Syntax,
                    ImmutableArray.Create<LocalSymbol>(),
                    prologue.ToImmutable(),
                    rewrittenFilter,
                    rewrittenFilter.Type);
            }

            // done with this.
            newLocals.Free();
            prologue.Free();

            // rewrite filter and body
            // NOTE: this will proxy all accesses to exception local if that got lifted.
            var exceptionTypeOpt = this.VisitType(node.ExceptionTypeOpt);
            var rewrittenBlock = (BoundBlock)this.Visit(node.Body);

            return node.Update(
                rewrittenCatchLocal,
                rewrittenExceptionSource,
                exceptionTypeOpt,
                rewrittenFilter,
                rewrittenBlock,
                node.IsSynthesizedAsyncCatchAll);
        }

        public override BoundNode VisitSequence(BoundSequence node)
        {
            LambdaFrame frame;
            // Test if this frame has captured variables and requires the introduction of a closure class.
            if (_frames.TryGetValue(node, out frame))
            {
                return IntroduceFrame(node, frame, (ArrayBuilder<BoundExpression> prologue, ArrayBuilder<LocalSymbol> newLocals) =>
                {
                    return RewriteSequence(node, prologue, newLocals);
                });
            }
            else
            {
                return RewriteSequence(node, ArrayBuilder<BoundExpression>.GetInstance(), ArrayBuilder<LocalSymbol>.GetInstance());
            }
        }

        public override BoundNode VisitStatementList(BoundStatementList node)
        {
            LambdaFrame frame;
            // Test if this frame has captured variables and requires the introduction of a closure class.
            // That can occur for a BoundStatementList if it is the body of a method with captured parameters.
            if (_frames.TryGetValue(node, out frame))
            {
                return IntroduceFrame(node, frame, (ArrayBuilder<BoundExpression> prologue, ArrayBuilder<LocalSymbol> newLocals) =>
                {
                    var newStatements = ArrayBuilder<BoundStatement>.GetInstance();
                    InsertAndFreePrologue(newStatements, prologue);

                    foreach (var s in node.Statements)
                    {
                        newStatements.Add((BoundStatement)this.Visit(s));
                    }

                    return new BoundBlock(node.Syntax, newLocals.ToImmutableAndFree(), ImmutableArray<LocalFunctionSymbol>.Empty, newStatements.ToImmutableAndFree(), node.HasErrors);
                });
            }
            else
            {
                return base.VisitStatementList(node);
            }
        }

        public override BoundNode VisitSwitchStatement(BoundSwitchStatement node)
        {
            LambdaFrame frame;
            // Test if this frame has captured variables and requires the introduction of a closure class.
            if (_frames.TryGetValue(node, out frame))
            {
                return IntroduceFrame(node, frame, (ArrayBuilder<BoundExpression> prologue, ArrayBuilder<LocalSymbol> newLocals) =>
                {
                    var newStatements = ArrayBuilder<BoundStatement>.GetInstance();
                    InsertAndFreePrologue(newStatements, prologue);
                    newStatements.Add((BoundStatement)base.VisitSwitchStatement(node));

                    return new BoundBlock(node.Syntax, newLocals.ToImmutableAndFree(), ImmutableArray<LocalFunctionSymbol>.Empty, newStatements.ToImmutableAndFree(), node.HasErrors);
                });
            }
            else
            {
                return base.VisitSwitchStatement(node);
            }
        }

        public override BoundNode VisitDelegateCreationExpression(BoundDelegateCreationExpression node)
        {
            // A delegate creation expression of the form "new Action( ()=>{} )" is treated exactly like
            // (Action)(()=>{})
            if (node.Argument.Kind == BoundKind.Lambda)
            {
                return RewriteLambdaConversion((BoundLambda)node.Argument);
            }
            else
            {
                if (node.MethodOpt?.MethodKind == MethodKind.LocalFunction)
                {
                    BoundExpression receiver;
                    MethodSymbol method;
                    var arguments = default(ImmutableArray<BoundExpression>);
                    RemapLocalFunction(node.Syntax, node.MethodOpt, out receiver, out method, ref arguments);
                    var result = new BoundDelegateCreationExpression(node.Syntax, receiver, method, isExtensionMethod: false, type: node.Type);
                    return result;
                }
                return base.VisitDelegateCreationExpression(node);
            }
        }

        public override BoundNode VisitConversion(BoundConversion conversion)
        {
            if (conversion.ConversionKind == ConversionKind.AnonymousFunction)
            {
                var result = (BoundExpression)RewriteLambdaConversion((BoundLambda)conversion.Operand);
                return _inExpressionLambda && conversion.ExplicitCastInCode
                    ? new BoundConversion(
                        syntax: conversion.Syntax,
                        operand: result,
                        conversionKind: conversion.ConversionKind,
                        resultKind: conversion.ResultKind,
                        isBaseConversion: false,
                        symbolOpt: null,
                        @checked: false,
                        explicitCastInCode: true,
                        isExtensionMethod: false,
                        isArrayIndex: false,
                        constantValueOpt: conversion.ConstantValueOpt,
                        type: conversion.Type)
                    : result;
            }
            else
            {
                if (conversion.ConversionKind == ConversionKind.MethodGroup && conversion.SymbolOpt?.MethodKind == MethodKind.LocalFunction)
                {
                    BoundExpression receiver;
                    MethodSymbol method;
                    var arguments = default(ImmutableArray<BoundExpression>);
                    RemapLocalFunction(conversion.Syntax, conversion.SymbolOpt, out receiver, out method, ref arguments);
                    var result = new BoundDelegateCreationExpression(conversion.Syntax, receiver, method, isExtensionMethod: false, type: conversion.Type);
                    return result;
                }
                return base.VisitConversion(conversion);
            }
        }

        public override BoundNode VisitLocalFunctionStatement(BoundLocalFunctionStatement node)
        {
            ClosureKind closureKind;
            NamedTypeSymbol translatedLambdaContainer;
            LambdaFrame containerAsFrame;
            BoundNode lambdaScope;
            DebugId topLevelMethodId;
            DebugId lambdaId;
            RewriteLambdaOrLocalFunction(
                node,
                out closureKind,
                out translatedLambdaContainer,
                out containerAsFrame,
                out lambdaScope,
                out topLevelMethodId,
                out lambdaId);

            return new BoundNoOpStatement(node.Syntax, NoOpStatementFlavor.Default);
        }

        private DebugId GetTopLevelMethodId()
        {
            return slotAllocatorOpt?.MethodId ?? new DebugId(_topLevelMethodOrdinal, CompilationState.ModuleBuilderOpt.CurrentGenerationOrdinal);
        }

        private DebugId GetClosureId(SyntaxNode syntax, ArrayBuilder<ClosureDebugInfo> closureDebugInfo)
        {
            Debug.Assert(syntax != null);

            DebugId closureId;
            DebugId previousClosureId;
            if (slotAllocatorOpt != null && slotAllocatorOpt.TryGetPreviousClosure(syntax, out previousClosureId))
            {
                closureId = previousClosureId;
            }
            else
            {
                closureId = new DebugId(closureDebugInfo.Count, CompilationState.ModuleBuilderOpt.CurrentGenerationOrdinal);
            }

            int syntaxOffset = _topLevelMethod.CalculateLocalSyntaxOffset(syntax.SpanStart, syntax.SyntaxTree);
            closureDebugInfo.Add(new ClosureDebugInfo(syntaxOffset, closureId));

            return closureId;
        }

        private DebugId GetLambdaId(SyntaxNode syntax, ClosureKind closureKind, int closureOrdinal)
        {
            Debug.Assert(syntax != null);

            SyntaxNode lambdaOrLambdaBodySyntax;
            var anonymousFunction = syntax as AnonymousFunctionExpressionSyntax;
            var localFunction = syntax as LocalFunctionStatementSyntax;
            bool isLambdaBody;

            if (anonymousFunction != null)
            {
                lambdaOrLambdaBodySyntax = anonymousFunction.Body;
                isLambdaBody = true;
            }
            else if (localFunction != null)
            {
                lambdaOrLambdaBodySyntax = (SyntaxNode)localFunction.Body ?? localFunction.ExpressionBody;
                isLambdaBody = true;
            }
            else if (LambdaUtilities.IsQueryPairLambda(syntax))
            {
                // "pair" query lambdas
                lambdaOrLambdaBodySyntax = syntax;
                isLambdaBody = false;
                Debug.Assert(closureKind == ClosureKind.Singleton);
            }
            else
            {
                // query lambdas
                lambdaOrLambdaBodySyntax = syntax;
                isLambdaBody = true;
            }

            Debug.Assert(!isLambdaBody || LambdaUtilities.IsLambdaBody(lambdaOrLambdaBodySyntax));

            // determine lambda ordinal and calculate syntax offset

            DebugId lambdaId;
            DebugId previousLambdaId;
            if (slotAllocatorOpt != null && slotAllocatorOpt.TryGetPreviousLambda(lambdaOrLambdaBodySyntax, isLambdaBody, out previousLambdaId))
            {
                lambdaId = previousLambdaId;
            }
            else
            {
                lambdaId = new DebugId(_lambdaDebugInfoBuilder.Count, CompilationState.ModuleBuilderOpt.CurrentGenerationOrdinal);
            }

            int syntaxOffset = _topLevelMethod.CalculateLocalSyntaxOffset(lambdaOrLambdaBodySyntax.SpanStart, lambdaOrLambdaBodySyntax.SyntaxTree);
            _lambdaDebugInfoBuilder.Add(new LambdaDebugInfo(syntaxOffset, lambdaId, closureOrdinal));
            return lambdaId;
        }

        private SynthesizedLambdaMethod RewriteLambdaOrLocalFunction(
            IBoundLambdaOrFunction node,
            out ClosureKind closureKind,
            out NamedTypeSymbol translatedLambdaContainer,
            out LambdaFrame containerAsFrame,
            out BoundNode lambdaScope,
            out DebugId topLevelMethodId,
            out DebugId lambdaId)
        {
            ImmutableArray<TypeSymbol> structClosures;
            int closureOrdinal;
            if (_analysis.LambdaScopes.TryGetValue(node.Symbol, out lambdaScope))
            {
                containerAsFrame = _frames[lambdaScope];
                var structClosureParamBuilder = ArrayBuilder<TypeSymbol>.GetInstance();
                while (containerAsFrame != null && containerAsFrame.IsValueType)
                {
                    structClosureParamBuilder.Add(containerAsFrame);
                    if (this._analysis.NeedsParentFrame.Contains(lambdaScope))
                    {
                        var found = false;
                        while (this._analysis.ScopeParent.TryGetValue(lambdaScope, out lambdaScope))
                        {
                            if (_frames.TryGetValue(lambdaScope, out containerAsFrame))
                            {
                                found = true;
                                break;
            }
                        }
                        if (found)
                        {
                            continue;
                        }
                    }
                    // can happen when scope no longer needs parent frame, or we're at the outermost level and the "parent frame" is top level "this".
                    lambdaScope = null;
                    containerAsFrame = null;
                }
                // Reverse it because we're going from inner to outer, and parameters are in order of outer to inner
                structClosureParamBuilder.ReverseContents();
                structClosures = structClosureParamBuilder.ToImmutableAndFree();
                if (containerAsFrame == null)
                {
                    closureKind = ClosureKind.Static; // not exactly... but we've rewritten the receiver to be a by-ref parameter
                    translatedLambdaContainer = _topLevelMethod.ContainingType;
                    closureOrdinal = LambdaDebugInfo.StaticClosureOrdinal;
                }
                else
            {
                closureKind = ClosureKind.General;
                    translatedLambdaContainer = containerAsFrame;
                closureOrdinal = containerAsFrame.ClosureOrdinal;
            }
            }
            else if (_analysis.CapturedVariablesByLambda[node.Symbol].Count == 0)
            {
                if (_analysis.MethodsConvertedToDelegates.Contains(node.Symbol))
            {
                translatedLambdaContainer = containerAsFrame = GetStaticFrame(Diagnostics, node);
                    closureKind = ClosureKind.Singleton;
                    closureOrdinal = LambdaDebugInfo.StaticClosureOrdinal;
                }
                else
                {
                    containerAsFrame = null;
                    translatedLambdaContainer = _topLevelMethod.ContainingType;
                closureKind = ClosureKind.Static;
                closureOrdinal = LambdaDebugInfo.StaticClosureOrdinal;
            }
                structClosures = default(ImmutableArray<TypeSymbol>);
            }
            else
            {
                containerAsFrame = null;
                translatedLambdaContainer = _topLevelMethod.ContainingType;
                closureKind = ClosureKind.ThisOnly;
                closureOrdinal = LambdaDebugInfo.ThisOnlyClosureOrdinal;
                structClosures = default(ImmutableArray<TypeSymbol>);
            }

            // Move the body of the lambda to a freshly generated synthetic method on its frame.
            topLevelMethodId = GetTopLevelMethodId();
            lambdaId = GetLambdaId(node.Syntax, closureKind, closureOrdinal);

            var synthesizedMethod = new SynthesizedLambdaMethod(translatedLambdaContainer, structClosures, closureKind, _topLevelMethod, topLevelMethodId, node, lambdaId);
            CompilationState.ModuleBuilderOpt.AddSynthesizedDefinition(translatedLambdaContainer, synthesizedMethod);

            foreach (var parameter in node.Symbol.Parameters)
            {
                _parameterMap.Add(parameter, synthesizedMethod.Parameters[parameter.Ordinal]);
            }

            if (node is BoundLocalFunctionStatement)
            {
                _localFunctionMap[((BoundLocalFunctionStatement)node).Symbol] = new MappedLocalFunction(synthesizedMethod, closureKind);
            }

            // rewrite the lambda body as the generated method's body
            var oldMethod = _currentMethod;
            var oldFrameThis = _currentFrameThis;
            var oldTypeParameters = _currentTypeParameters;
            var oldInnermostFramePointer = _innermostFramePointer;
            var oldTypeMap = _currentLambdaBodyTypeMap;
            var oldAddedStatements = _addedStatements;
            var oldAddedLocals = _addedLocals;
            _addedStatements = null;
            _addedLocals = null;

            // switch to the generated method

            _currentMethod = synthesizedMethod;
            if (closureKind == ClosureKind.Static || closureKind == ClosureKind.Singleton)
            {
                // no link from a static lambda to its container
                _innermostFramePointer = _currentFrameThis = null;
            }
            else
            {
                _currentFrameThis = synthesizedMethod.ThisParameter;
                _innermostFramePointer = null;
                _framePointers.TryGetValue(translatedLambdaContainer, out _innermostFramePointer);
            }

            _currentTypeParameters = containerAsFrame?.TypeParameters.Concat(synthesizedMethod.TypeParameters) ?? synthesizedMethod.TypeParameters;
            _currentLambdaBodyTypeMap = synthesizedMethod.TypeMap;

            var body = AddStatementsIfNeeded((BoundStatement)VisitBlock(node.Body));
            CheckLocalsDefined(body);
            CompilationState.AddSynthesizedMethod(synthesizedMethod, body);

            // return to the old method

            _currentMethod = oldMethod;
            _currentFrameThis = oldFrameThis;
            _currentTypeParameters = oldTypeParameters;
            _innermostFramePointer = oldInnermostFramePointer;
            _currentLambdaBodyTypeMap = oldTypeMap;
            _addedLocals = oldAddedLocals;
            _addedStatements = oldAddedStatements;

            return synthesizedMethod;
            }

        private BoundNode RewriteLambdaConversion(BoundLambda node)
            {
            var wasInExpressionLambda = _inExpressionLambda;
            _inExpressionLambda = _inExpressionLambda || node.Type.IsExpressionTree();

            if (_inExpressionLambda)
            {
                var newType = VisitType(node.Type);
                var newBody = (BoundBlock)Visit(node.Body);
                node = node.Update(node.Symbol, newBody, node.Diagnostics, node.Binder, newType);
                var result0 = wasInExpressionLambda ? node : ExpressionLambdaRewriter.RewriteLambda(node, CompilationState, TypeMap, RecursionDepth, Diagnostics);
                _inExpressionLambda = wasInExpressionLambda;
                return result0;
            }

            ClosureKind closureKind;
            NamedTypeSymbol translatedLambdaContainer;
            LambdaFrame containerAsFrame;
            BoundNode lambdaScope;
            DebugId topLevelMethodId;
            DebugId lambdaId;
            SynthesizedLambdaMethod synthesizedMethod = RewriteLambdaOrLocalFunction(
                node,
                out closureKind,
                out translatedLambdaContainer,
                out containerAsFrame,
                out lambdaScope,
                out topLevelMethodId,
                out lambdaId);

            MethodSymbol referencedMethod = synthesizedMethod;
            BoundExpression receiver;
            NamedTypeSymbol constructedFrame;
            RemapLambdaOrLocalFunction(node.Syntax, node.Symbol, default(ImmutableArray<TypeSymbol>), closureKind, ref referencedMethod, out receiver, out constructedFrame);

            // Rewrite the lambda expression (and the enclosing anonymous method conversion) as a delegate creation expression

            TypeSymbol type = this.VisitType(node.Type);

            // static lambdas are emitted as instance methods on a singleton receiver
            // delegates invoke dispatch is optimized for instance delegates so 
            // it is preferable to emit lambdas as instance methods even when lambdas 
            // do not capture anything
            BoundExpression result = new BoundDelegateCreationExpression(
                node.Syntax,
                receiver,
                referencedMethod,
                isExtensionMethod: false,
                type: type);

            // if the block containing the lambda is not the innermost block,
            // or the lambda is static, then the lambda object should be cached in its frame.
            // NOTE: we are not caching static lambdas in static ctors - cannot reuse such cache.
            var shouldCacheForStaticMethod = closureKind == ClosureKind.Singleton &&
                _currentMethod.MethodKind != MethodKind.StaticConstructor &&
                !referencedMethod.IsGenericMethod;

            // NOTE: We require "lambdaScope != null". 
            //       We do not want to introduce a field into an actual user's class (not a synthetic frame).
            var shouldCacheInLoop = lambdaScope != null &&
                lambdaScope != _analysis.ScopeParent[node.Body] &&
                InLoopOrLambda(node.Syntax, lambdaScope.Syntax);

            if (shouldCacheForStaticMethod || shouldCacheInLoop)
            {
                // replace the expression "new Delegate(frame.M)" with "frame.cache ?? (frame.cache = new Delegate(frame.M));
                var F = new SyntheticBoundNodeFactory(_currentMethod, node.Syntax, CompilationState, Diagnostics);
                try
                {
                    BoundExpression cache;
                    if (shouldCacheForStaticMethod || shouldCacheInLoop && (object)containerAsFrame != null)
                    {
                        // Since the cache variable will be in a container with possibly alpha-rewritten generic parameters, we need to
                        // substitute the original type according to the type map for that container. That substituted type may be
                        // different from the local variable `type`, which has the node's type substituted for the current container.
                        var cacheVariableType = containerAsFrame.TypeMap.SubstituteType(node.Type).Type;

                        var cacheVariableName = GeneratedNames.MakeLambdaCacheFieldName(
                            // If we are generating the field into a display class created exclusively for the lambda the lambdaOrdinal itself is unique already, 
                            // no need to include the top-level method ordinal in the field name.
                            (closureKind == ClosureKind.General) ? -1 : topLevelMethodId.Ordinal,
                            topLevelMethodId.Generation,
                            lambdaId.Ordinal,
                            lambdaId.Generation);

                        var cacheField = new SynthesizedLambdaCacheFieldSymbol(translatedLambdaContainer, cacheVariableType, cacheVariableName, _topLevelMethod, isReadOnly: false, isStatic: closureKind == ClosureKind.Singleton);
                        CompilationState.ModuleBuilderOpt.AddSynthesizedDefinition(translatedLambdaContainer, cacheField);
                        cache = F.Field(receiver, cacheField.AsMember(constructedFrame)); //NOTE: the field was added to the unconstructed frame type.
                    }
                    else
                    {
                        // the lambda captures at most the "this" of the enclosing method.  We cache its delegate in a local variable.
                        var cacheLocal = F.SynthesizedLocal(type, kind: SynthesizedLocalKind.CachedAnonymousMethodDelegate);
                        if (_addedLocals == null) _addedLocals = ArrayBuilder<LocalSymbol>.GetInstance();
                        _addedLocals.Add(cacheLocal);
                        if (_addedStatements == null) _addedStatements = ArrayBuilder<BoundStatement>.GetInstance();
                        cache = F.Local(cacheLocal);
                        _addedStatements.Add(F.Assignment(cache, F.Null(type)));
                    }

                    result = F.Coalesce(cache, F.AssignmentExpression(cache, result));
                }
                catch (SyntheticBoundNodeFactory.MissingPredefinedMember ex)
                {
                    Diagnostics.Add(ex.Diagnostic);
                    return new BoundBadExpression(F.Syntax, LookupResultKind.Empty, ImmutableArray<Symbol>.Empty, ImmutableArray.Create<BoundNode>(node), node.Type);
                }
            }

            return result;
        }

        // This helper checks syntactically whether there is a loop or lambda expression
        // between given lambda syntax and the syntax that corresponds to its closure.
        // we use this heuristic as a hint that the lambda delegate may be created 
        // multiple times with same closure.
        // In such cases it makes sense to cache the delegate.
        //
        // Examples:
        //            int x = 123;
        //            for (int i = 1; i< 10; i++)
        //            {
        //                if (i< 2)
        //                {
        //                    arr[i].Execute(arg => arg + x);  // delegate should be cached
        //                }
        //            }

        //            for (int i = 1; i< 10; i++)
        //            {
        //                var val = i;
        //                if (i< 2)
        //                {
        //                    int y = i + i;
        //                    System.Console.WriteLine(y);
        //                    arr[i].Execute(arg => arg + val);  // delegate should NOT be cached (closure created inside the loop)
        //                }
        //            }
        //
        private static bool InLoopOrLambda(SyntaxNode lambdaSyntax, SyntaxNode scopeSyntax)
        {
            var curSyntax = lambdaSyntax.Parent;
            while (curSyntax != null && curSyntax != scopeSyntax)
            {
                switch (curSyntax.Kind())
                {
                    case SyntaxKind.ForStatement:
                    case SyntaxKind.ForEachStatement:
                    case SyntaxKind.WhileStatement:
                    case SyntaxKind.DoStatement:
                    case SyntaxKind.SimpleLambdaExpression:
                    case SyntaxKind.ParenthesizedLambdaExpression:
                        return true;
                }

                curSyntax = curSyntax.Parent;
            }

            return false;
        }

        public override BoundNode VisitLambda(BoundLambda node)
        {
            // these nodes have been handled in the context of the enclosing anonymous method conversion.
            throw ExceptionUtilities.Unreachable;
        }

        #endregion

#if CHECK_LOCALS
        /// <summary>
        /// Ensure that local variables are always in scope where used in bound trees
        /// </summary>
        /// <param name="node"></param>
        static partial void CheckLocalsDefined(BoundNode node)
        {
            LocalsDefinedScanner.INSTANCE.Visit(node);
        }

        class LocalsDefinedScanner : BoundTreeWalker
        {
            internal static LocalsDefinedScanner INSTANCE = new LocalsDefinedScanner();

            HashSet<Symbol> localsDefined = new HashSet<Symbol>();

            public override BoundNode VisitLocal(BoundLocal node)
            {
                Debug.Assert(node.LocalSymbol.IsConst || localsDefined.Contains(node.LocalSymbol));
                return base.VisitLocal(node);
            }

            public override BoundNode VisitSequence(BoundSequence node)
            {
                try
                {
                    if (!node.Locals.IsNullOrEmpty)
                        foreach (var l in node.Locals)
                            localsDefined.Add(l);
                    return base.VisitSequence(node);
                }
                finally
                {
                    if (!node.Locals.IsNullOrEmpty)
                        foreach (var l in node.Locals)
                            localsDefined.Remove(l);
                }
            }

            public override BoundNode VisitCatchBlock(BoundCatchBlock node)
            {
                try
                {
                    if ((object)node.LocalOpt != null) localsDefined.Add(node.LocalOpt);
                    return base.VisitCatchBlock(node);
                }
                finally
                {
                    if ((object)node.LocalOpt != null) localsDefined.Remove(node.LocalOpt);
                }
            }

            public override BoundNode VisitSwitchStatement(BoundSwitchStatement node)
            {
                try
                {
                    if (!node.LocalsOpt.IsNullOrEmpty)
                        foreach (var l in node.LocalsOpt)
                            localsDefined.Add(l);
                    return base.VisitSwitchStatement(node);
                }
                finally
                {
                    if (!node.LocalsOpt.IsNullOrEmpty)
                        foreach (var l in node.LocalsOpt)
                            localsDefined.Remove(l);
                }
            }

            public override BoundNode VisitBlock(BoundBlock node)
            {
                try
                {
                    if (!node.LocalsOpt.IsNullOrEmpty)
                        foreach (var l in node.LocalsOpt)
                            localsDefined.Add(l);
                    return base.VisitBlock(node);
                }
                finally
                {
                    if (!node.LocalsOpt.IsNullOrEmpty)
                        foreach (var l in node.LocalsOpt)
                            localsDefined.Remove(l);
                }
            }
        }
#endif
    }
}
