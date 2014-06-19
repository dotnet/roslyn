// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#if DEBUG
//#define CHECK_LOCALS // define CHECK_LOCALS to help debug some rewriting problems that would otherwise cause code-gen failures
#endif

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.CodeAnalysis.CSharp.Symbols;
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
    /// have captured variables.  The result of this analysis is left in <see cref="analysis"/>.
    /// 
    /// Then we make a frame, or compiler-generated class, represented by an instance of
    /// <see cref="LambdaFrame"/> for each scope with captured variables.  The generated frames are kept
    /// in <see cref="frames"/>.  Each frame is given a single field for each captured
    /// variable in the corresponding scope.  These are are maintained in <see cref="MethodToClassRewriter.proxies"/>.
    /// 
    /// Finally, we walk and rewrite the input bound tree, keeping track of the following:
    /// (1) The current set of active frame pointers, in <see cref="framePointers"/>
    /// (2) The current method being processed (this changes within a lambda's body), in <see cref="currentMethod"/>
    /// (3) The "this" symbol for the current method in <see cref="currentFrameThis"/>, and
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
    partial class LambdaRewriter : MethodToClassRewriter
    {
        private readonly Analysis analysis;
        private readonly MethodSymbol topLevelMethod;

        // for each block with lifted (captured) variables, the corresponding frame type
        private readonly Dictionary<BoundNode, LambdaFrame> frames = new Dictionary<BoundNode, LambdaFrame>();

        // the current set of frame pointers in scope.  Each is either a local variable (where introduced),
        // or the "this" parameter when at the top level.  Keys in this map are never constructed types.
        private readonly Dictionary<NamedTypeSymbol, Symbol> framePointers = new Dictionary<NamedTypeSymbol, Symbol>();

        // True if the rewritten tree should include assignments of the
        // original locals to the lifted proxies. This is only useful for the
        // expression evaluator where the original locals are left as is.
        private readonly bool assignLocals;

        // The current method or lambda being processed.
        private MethodSymbol currentMethod;

        // The "this" symbol for the current method.
        private ParameterSymbol currentFrameThis;

        // The symbol (field or local) holding the innermost frame
        private Symbol innermostFramePointer;

        // The mapping of type parameters for the current lambda body
        private TypeMap currentLambdaBodyTypeMap;

        // The current set of type parameters (mapped from the enclosing method's type parameters)
        private ImmutableArray<TypeParameterSymbol> currentTypeParameters;

        // Initialization for the proxy of the upper frame if it needs to be deferred.
        // Such situation happens when lifting this in a ctor.
        private BoundExpression thisProxyInitDeferred;

        // Set to true once we've seen the base (or self) constructor invocation in a constructor
        private bool seenBaseCall;

        // Set to true while translating code inside of an expression lambda.
        private bool inExpressionLambda;

        // When a lambda captures only 'this' of the enclosing method, we cache it in a local
        // variable.  This is the set of such local variables that must be added to the enclosing
        // method's top-level block.
        private ArrayBuilder<LocalSymbol> addedLocals;

        // Similarly, this is the set of statements that must be added to the enclosing method's
        // top-level block initializing those variables to null.
        private ArrayBuilder<BoundStatement> addedStatements;

        private LambdaRewriter(
            Analysis analysis,
            NamedTypeSymbol thisType,
            ParameterSymbol thisParameterOpt,
            MethodSymbol method,
            TypeCompilationState compilationState,
            DiagnosticBag diagnostics,
            bool generateDebugInfo,
            bool assignLocals)
            : base(compilationState, diagnostics, generateDebugInfo)
        {
            Debug.Assert(analysis != null);
            Debug.Assert(thisType != null);
            Debug.Assert(method != null);
            Debug.Assert(compilationState != null);
            Debug.Assert(diagnostics != null);

            this.topLevelMethod = method;
            this.currentMethod = method;
            this.analysis = analysis;
            this.assignLocals = assignLocals;
            this.currentTypeParameters = method.TypeParameters;
            this.currentLambdaBodyTypeMap = TypeMap.Empty;
            this.innermostFramePointer = currentFrameThis = thisParameterOpt;
            this.framePointers[thisType] = thisParameterOpt;
            this.seenBaseCall = method.MethodKind != MethodKind.Constructor; // only used for ctors
        }

        /// <summary>
        /// Rewrite the given node to eliminate lambda expressions.  Also returned are the method symbols and their
        /// bound bodies for the extracted lambda bodies. These would typically be emitted by the caller such as
        /// MethodBodyCompiler.  See this class' documentation
        /// for a more thorough explanation of the algorithm and its use by clients.
        /// </summary>
        /// <param name="node">The bound node to be rewritten</param>
        /// <param name="thisType">The type of the top-most frame</param>
        /// <param name="thisParameter">The "this" parameter in the top-most frame, or null if static method</param>
        /// <param name="method">The containing method of the node to be rewritten</param>
        /// <param name="compilationState">The caller's buffer into which we produce additional methods to be emitted by the caller</param>
        /// <param name="diagnostics">Diagnostic bag for diagnostics</param>
        /// <param name="analysis">A caller-provided analysis of the node's lambdas</param>
        /// <param name="generateDebugInfo"></param>
        /// <param name="assignLocals">The rewritten tree should include assignments of the original locals to the lifted proxies</param>
        public static BoundStatement Rewrite(
            BoundStatement node,
            NamedTypeSymbol thisType,
            ParameterSymbol thisParameter,
            MethodSymbol method,
            TypeCompilationState compilationState,
            DiagnosticBag diagnostics,
            Analysis analysis,
            bool generateDebugInfo,
            bool assignLocals = false)
        {
            Debug.Assert((object)thisType != null);
            Debug.Assert(((object)thisParameter == null) || (thisParameter.Type == thisType));

            CheckLocalsDefined(node);
            var rewriter = new LambdaRewriter(
                analysis,
                thisType,
                thisParameter,
                method,
                compilationState,
                diagnostics,
                generateDebugInfo,
                assignLocals);
            analysis.ComputeLambdaScopesAndFrameCaptures();
            rewriter.MakeFrames();
            var body = rewriter.AddStatementsIfNeeded((BoundStatement)rewriter.Visit(node));
            CheckLocalsDefined(body);
            return body;
        }

        BoundStatement AddStatementsIfNeeded(BoundStatement body)
        {
            if (addedLocals != null)
            {
                addedStatements.Add(body);
                body = new BoundBlock(body.Syntax, addedLocals.ToImmutableAndFree(), addedStatements.ToImmutableAndFree()) { WasCompilerGenerated = true };
                addedLocals = null;
                addedStatements = null;
            }
            else
            {
                Debug.Assert(addedStatements == null);
            }

            return body;
        }

        protected override HashSet<Symbol> VariablesCaptured
        {
            get { return analysis.variablesCaptured; }
        }

        protected override TypeMap TypeMap
        {
            get { return currentLambdaBodyTypeMap; }
        }

        protected override MethodSymbol CurrentMethod
        {
            get { return currentMethod; }
        }

        protected override NamedTypeSymbol ContainingType
        {
            get { return this.topLevelMethod.ContainingType; }
        }

        /// <summary>
        /// Check that the top-level node is well-defined, in the sense that all
        /// locals that are used are defined in some enclosing scope.
        /// </summary>
        /// <param name="node"></param>
        static partial void CheckLocalsDefined(BoundNode node);

        /// <summary>
        /// Create the frame types.
        /// </summary>
        private void MakeFrames()
        {
            NamedTypeSymbol containingType = this.ContainingType;

            foreach (Symbol captured in analysis.variablesCaptured)
            {
                BoundNode node;
                if (!analysis.variableBlock.TryGetValue(captured, out node) ||
                    analysis.declaredInsideExpressionLambda.Contains(captured))
                {
                    continue;
                }

                LambdaFrame frame;
                if (!frames.TryGetValue(node, out frame))
                {
                    frame = new LambdaFrame(topLevelMethod, CompilationState);
                    frames.Add(node, frame);

                    if (CompilationState.Emitting)
                    {
                        CompilationState.ModuleBuilderOpt.AddSynthesizedDefinition(containingType, frame);
                    }
                }

                SynthesizedFieldSymbolBase proxy = new LambdaCapturedVariable(frame, captured);
                proxies.Add(captured, new CapturedToFrameSymbolReplacement(proxy));
                if (CompilationState.Emitting)
                {
                    CompilationState.ModuleBuilderOpt.AddSynthesizedDefinition(frame, proxy);
                }

                if (proxy.Type.IsRestrictedType())
                {
                    foreach (CSharpSyntaxNode syntax in analysis.capturedSyntax[captured])
                    {
                        // CS4013: Instance of type '{0}' cannot be used inside an anonymous function, query expression, iterator block or async method
                        this.Diagnostics.Add(ErrorCode.ERR_SpecialByRefInLambda, syntax.Location, proxy.Type);
                    }
                }
            }
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
            if ((object)currentFrameThis != null && currentFrameThis.Type == frameClass)
            {
                return new BoundThisReference(syntax, frameClass);
            }

            // Otherwise we need to return the value from a frame pointer local variable...
            Symbol framePointer = framePointers[frameClass];
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
            NamedTypeSymbol frameType = frame.ConstructIfGeneric(StaticCast<TypeSymbol>.From(currentTypeParameters));
            LocalSymbol framePointer = new LambdaFrameLocalSymbol(this.topLevelMethod, frameType, CompilationState);

            CSharpSyntaxNode syntax = node.Syntax;

            // assign new frame to the frame variable
            CompilationState.AddSynthesizedMethod(frame.Constructor, FlowAnalysisPass.AppendImplicitReturn(MethodCompiler.BindMethodBody(frame.Constructor, CompilationState, null), frame.Constructor));

            var prologue = ArrayBuilder<BoundExpression>.GetInstance();

            MethodSymbol constructor = frame.Constructor.AsMember(frameType);
            Debug.Assert(frameType == constructor.ContainingType);
            var newFrame = new BoundObjectCreationExpression(
                syntax: syntax,
                constructor: constructor);

            prologue.Add(new BoundAssignmentOperator(syntax,
                new BoundLocal(syntax, framePointer, null, frameType),
                newFrame,
                frameType));

            CapturedSymbolReplacement oldInnermostFrameProxy = null;
            if ((object)innermostFramePointer != null)
            {
                proxies.TryGetValue(innermostFramePointer, out oldInnermostFrameProxy);
                if (analysis.needsParentFrame.Contains(node))
                {
                    var capturedFrame = new LambdaCapturedVariable(frame, innermostFramePointer);
                    FieldSymbol frameParent = capturedFrame.AsMember(frameType);
                    BoundExpression left = new BoundFieldAccess(syntax, new BoundLocal(syntax, framePointer, null, frameType), frameParent, null);
                    BoundExpression right = FrameOfType(syntax, frameParent.Type as NamedTypeSymbol);
                    BoundExpression assignment = new BoundAssignmentOperator(syntax, left, right, left.Type);

                    if (this.currentMethod.MethodKind == MethodKind.Constructor && capturedFrame.Type == this.currentMethod.ContainingType && !this.seenBaseCall)
                    {
                        // Containing method is a constructor 
                        // Initialization statement for the "this" proxy must be inserted
                        // after the constructor initializer statement block
                        // This insertion will be done by the delegate F
                        Debug.Assert(thisProxyInitDeferred == null);
                        thisProxyInitDeferred = assignment;
                    }
                    else
                    {
                        prologue.Add(assignment);
                    }

                    if (CompilationState.Emitting)
                    {
                        CompilationState.ModuleBuilderOpt.AddSynthesizedDefinition(frame, capturedFrame);
                    }

                    proxies[innermostFramePointer] = new CapturedToFrameSymbolReplacement(capturedFrame);
                }
            }

            // Capture any parameters of this block.  This would typically occur
            // at the top level of a method or lambda with captured parameters.
            // TODO: speed up the following by computing it in analysis.
            foreach (var v in analysis.variablesCaptured)
            {
                BoundNode varNode;
                if (!analysis.variableBlock.TryGetValue(v, out varNode) ||
                    varNode != node ||
                    analysis.declaredInsideExpressionLambda.Contains(v))
                {
                    continue;
                }

                InitVariableProxy(syntax, v, framePointer, prologue);
            }

            Symbol oldInnermostFramePointer = innermostFramePointer;

            innermostFramePointer = framePointer;
            var addedLocals = ArrayBuilder<LocalSymbol>.GetInstance();
            addedLocals.Add(framePointer);
            framePointers.Add(frame, framePointer);

            var result = F(prologue, addedLocals);

            framePointers.Remove(frame);
            innermostFramePointer = oldInnermostFramePointer;

            if ((object)innermostFramePointer != null)
            {
                if (oldInnermostFrameProxy != null)
                {
                    proxies[innermostFramePointer] = oldInnermostFrameProxy;
                }
                else
                {
                    proxies.Remove(innermostFramePointer);
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
                {
                            var parameter = (ParameterSymbol)symbol;
                    ParameterSymbol parameterToUse;
                            if (!parameterMap.TryGetValue(parameter, out parameterToUse))
                            {
                                parameterToUse = parameter;
                            }
                            value = new BoundParameter(syntax, parameterToUse);
                        }
                        break;
                    case SymbolKind.Local:
                        if (!this.assignLocals)
                        {
                            return;
                }
                else
                {
                            var local = (LocalSymbol)symbol;
                    LocalSymbol localToUse;
                            if (!localMap.TryGetValue(local, out localToUse))
                            {
                                localToUse = local;
                            }
                            value = new BoundLocal(syntax, localToUse, null, localToUse.Type);
                        }
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

            return (currentMethod == topLevelMethod || topLevelMethod.ThisParameter == null ?
                node :
                FramePointer(node.Syntax, (NamedTypeSymbol)node.Type));
        }

        public override BoundNode VisitBaseReference(BoundBaseReference node)
        {
            return (currentMethod.ContainingType == topLevelMethod.ContainingType)
                ? node
                : FramePointer(node.Syntax, topLevelMethod.ContainingType); // technically, not the correct static type
        }

        public override BoundNode VisitCall(BoundCall node)
        {
            var visited = base.VisitCall(node);
            if (visited.Kind != BoundKind.Call)
            {
                return visited;
            }

            var rewritten = (BoundCall)visited;

            // Check if we need to init the 'this' proxy in a ctor call
            if (!seenBaseCall)
            {
                seenBaseCall = currentMethod == topLevelMethod && node.IsConstructorInitializer();
                if (seenBaseCall && this.thisProxyInitDeferred != null)
                {
                    // Insert the this proxy assignment after the ctor call.
                    // Create bound sequence: { ctor call, thisProxyInitDeferred }
                    return new BoundSequence(
                        syntax: node.Syntax,
                        locals: ImmutableArray<LocalSymbol>.Empty,
                        sideEffects: ImmutableArray.Create<BoundExpression>(rewritten),
                        value: this.thisProxyInitDeferred,
                        type: rewritten.Type);
                }
            }

            return rewritten;
        }

        private BoundSequence RewriteSequence(BoundSequence node, ArrayBuilder<BoundExpression> prologue, ArrayBuilder<LocalSymbol> newLocals)
        {
            AddLocals(node.Locals, newLocals);

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
            if (frames.TryGetValue(node, out frame))
            {
                return IntroduceFrame(node, frame, (ArrayBuilder<BoundExpression> prologue, ArrayBuilder<LocalSymbol> newLocals) =>
                    RewriteBlock(node, prologue, newLocals));
            }
            else
            {
                return RewriteBlock(node, ArrayBuilder<BoundExpression>.GetInstance(), ArrayBuilder<LocalSymbol>.GetInstance());
            }
        }

        protected BoundBlock RewriteBlock(BoundBlock node, ArrayBuilder<BoundExpression> prologue, ArrayBuilder<LocalSymbol> newLocals)
        {
            AddLocals(node.LocalsOpt, newLocals);

            var newStatements = ArrayBuilder<BoundStatement>.GetInstance();

            if (GenerateDebugInfo && prologue.Count > 0)
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
            return node.Update(newLocals.ToImmutableAndFree(), newStatements.ToImmutableAndFree());
        }

        public override BoundNode VisitCatchBlock(BoundCatchBlock node)
        {
            // Test if this frame has captured variables and requires the introduction of a closure class.
            LambdaFrame frame;
            if (frames.TryGetValue(node, out frame))
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
            AddLocals(node.Locals, newLocals);
            var rewrittenCatchLocals = newLocals.ToImmutableAndFree();

            // If exception variable got lifted, IntroduceFrame will give us frame init prologue.
            // It needs to run before the exception variable is accessed.
            // To ensure that, we will make exception variable a sequence that performs prologue as its its sideeffecs.
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
            prologue.Free();

            // rewrite filter and body
            // NOTE: this will proxy all accesses to exception local if that got lifted.
            var exceptionTypeOpt = this.VisitType(node.ExceptionTypeOpt);
            var rewrittenBlock = (BoundBlock)this.Visit(node.Body);

            return node.Update(
                rewrittenCatchLocals, 
                rewrittenExceptionSource,
                exceptionTypeOpt,
                rewrittenFilter,
                rewrittenBlock);
        }

        public override BoundNode VisitSequence(BoundSequence node)
        {
            LambdaFrame frame;
            // Test if this frame has captured variables and requires the introduction of a closure class.
            if (frames.TryGetValue(node, out frame))
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
            if (frames.TryGetValue(node, out frame))
            {
                return IntroduceFrame(node, frame, (ArrayBuilder<BoundExpression> prologue, ArrayBuilder<LocalSymbol> newLocals) =>
                {
                    var newStatements = ArrayBuilder<BoundStatement>.GetInstance();
                    InsertAndFreePrologue(newStatements, prologue);

                    foreach (var s in node.Statements)
                    {
                        newStatements.Add((BoundStatement)this.Visit(s));
                    }

                    return new BoundBlock(node.Syntax, newLocals.ToImmutableAndFree(), newStatements.ToImmutableAndFree(), node.HasErrors);
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
            if (frames.TryGetValue(node, out frame))
            {
                return IntroduceFrame(node, frame, (ArrayBuilder<BoundExpression> prologue, ArrayBuilder<LocalSymbol> newLocals) =>
                {
                    var newStatements = ArrayBuilder<BoundStatement>.GetInstance();
                    InsertAndFreePrologue(newStatements, prologue);
                    newStatements.Add((BoundStatement)base.VisitSwitchStatement(node));

                    return new BoundBlock(node.Syntax, newLocals.ToImmutableAndFree(), newStatements.ToImmutableAndFree(), node.HasErrors);
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
                return base.VisitDelegateCreationExpression(node);
            }
        }

        public override BoundNode VisitConversion(BoundConversion conversion)
        {
            if (conversion.ConversionKind == ConversionKind.AnonymousFunction)
            {
                var result = (BoundExpression)RewriteLambdaConversion((BoundLambda)conversion.Operand);
                return inExpressionLambda && conversion.ExplicitCastInCode
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
                return base.VisitConversion(conversion);
            }
        }

        private BoundNode RewriteLambdaConversion(BoundLambda node)
        {
            var wasInExpressionLambda = inExpressionLambda;
            inExpressionLambda = inExpressionLambda || node.Type.IsExpressionTree();

            if (inExpressionLambda)
            {
                var newType = VisitType(node.Type);
                var newBody = (BoundBlock)Visit(node.Body);
                node = node.Update(node.Symbol, newBody, node.Diagnostics, node.Binder, newType);
                var result0 = wasInExpressionLambda ? node : ExpressionLambdaRewriter.RewriteLambda(node, CompilationState, Diagnostics);
                inExpressionLambda = wasInExpressionLambda;
                return result0;
            }

            NamedTypeSymbol translatedLambdaContainer;
            BoundNode lambdaScope = null;
            if (analysis.lambdaScopes.TryGetValue(node.Symbol, out lambdaScope))
            {
                translatedLambdaContainer = frames[lambdaScope];
            }
            else
            {
                translatedLambdaContainer = topLevelMethod.ContainingType;
            }

            // Move the body of the lambda to a freshly generated synthetic method on its frame.
            bool lambdaIsStatic = analysis.captures[node.Symbol].IsEmpty();
            var synthesizedMethod = new SynthesizedLambdaMethod(translatedLambdaContainer, topLevelMethod, node, lambdaIsStatic, CompilationState);
            if (CompilationState.Emitting)
            {
                CompilationState.ModuleBuilderOpt.AddSynthesizedDefinition(translatedLambdaContainer, synthesizedMethod);
            }

            for (int i = 0; i < node.Symbol.ParameterCount; i++)
            {
                parameterMap.Add(node.Symbol.Parameters[i], synthesizedMethod.Parameters[i]);
            }

            // rewrite the lambda body as the generated method's body
            var oldMethod = currentMethod;
            var oldFrameThis = currentFrameThis;
            var oldTypeParameters = currentTypeParameters;
            var oldInnermostFramePointer = innermostFramePointer;
            var oldTypeMap = currentLambdaBodyTypeMap;
            var oldAddedStatements = addedStatements;
            var oldAddedLocals = addedLocals;
            addedStatements = null;
            addedLocals = null;

            // switch to the generated method

            currentMethod = synthesizedMethod;
            if (lambdaIsStatic)
            {
                // no link from a static lambda to its container
                innermostFramePointer = currentFrameThis = null;
            }
            else
            {
                currentFrameThis = synthesizedMethod.ThisParameter;
                innermostFramePointer = null;
                framePointers.TryGetValue(translatedLambdaContainer, out innermostFramePointer);
            }

            if (translatedLambdaContainer.OriginalDefinition is LambdaFrame)
            {
                currentTypeParameters = translatedLambdaContainer.TypeParameters;
                currentLambdaBodyTypeMap = ((LambdaFrame)translatedLambdaContainer).TypeMap;
            }
            else
            {
                currentTypeParameters = synthesizedMethod.TypeParameters;
                currentLambdaBodyTypeMap = new TypeMap(topLevelMethod.TypeParameters, currentTypeParameters);
            }

            var body = AddStatementsIfNeeded((BoundStatement)VisitBlock(node.Body));
            CheckLocalsDefined(body);
            CompilationState.AddSynthesizedMethod(synthesizedMethod, body);

            // return to the old method

            currentMethod = oldMethod;
            currentFrameThis = oldFrameThis;
            currentTypeParameters = oldTypeParameters;
            innermostFramePointer = oldInnermostFramePointer;
            currentLambdaBodyTypeMap = oldTypeMap;
            addedLocals = oldAddedLocals;
            addedStatements = oldAddedStatements;

            // Rewrite the lambda expression (and the enclosing anonymous method conversion) as a delegate creation expression
            NamedTypeSymbol constructedFrame = (translatedLambdaContainer is LambdaFrame) ? translatedLambdaContainer.ConstructIfGeneric(StaticCast<TypeSymbol>.From(currentTypeParameters)) : translatedLambdaContainer;
            BoundExpression receiver = lambdaIsStatic ? new BoundTypeExpression(node.Syntax, null, constructedFrame) : FrameOfType(node.Syntax, constructedFrame);
            MethodSymbol referencedMethod = synthesizedMethod.AsMember(constructedFrame);
            if (referencedMethod.IsGenericMethod) referencedMethod = referencedMethod.Construct(StaticCast<TypeSymbol>.From(currentTypeParameters));
            TypeSymbol type = this.VisitType(node.Type);
            BoundExpression result = new BoundDelegateCreationExpression(
                node.Syntax,
                receiver,
                referencedMethod,
                isExtensionMethod: false,
                type: type);

            // if the block containing the lambda is not the innermost block,
            // or the lambda is static, then the lambda object should be cached in its frame.
            // NOTE: we are not caching static lambdas in static ctors - cannot reuse such cache.
            var shouldCacheForStaticMethod = lambdaIsStatic &&
                currentMethod.MethodKind != MethodKind.StaticConstructor &&
                !referencedMethod.IsGenericMethod;

            // NOTE: We require "lambdaScope != null". 
            //       We do not want to introduce a field into an actual user's class (not a synthetic frame).
            var shouldCacheInLoop = lambdaScope != null &&
                lambdaScope != analysis.blockParent[node.Body] &&
                InLoopOrLambda(node.Syntax, lambdaScope.Syntax);

            if (shouldCacheForStaticMethod || shouldCacheInLoop)
            {

                // replace the expression "new Delegate(frame.M)" with "(frame.cache == null) ? (frame.cache = new Delegate(frame.M)) : frame.cache"
                var F = new SyntheticBoundNodeFactory(currentMethod, node.Syntax, CompilationState, Diagnostics);
                try
                {
                    var cacheVariableName = GeneratedNames.MakeLambdaCacheName(CompilationState.GenerateTempNumber());
                    BoundExpression cacheVariable;
                    if (shouldCacheForStaticMethod || shouldCacheInLoop && translatedLambdaContainer is LambdaFrame)
                    {
                        var cacheVariableType = lambdaIsStatic ? type : (translatedLambdaContainer as LambdaFrame).TypeMap.SubstituteType(type);
                        var cacheField = new SynthesizedFieldSymbol(translatedLambdaContainer, cacheVariableType, cacheVariableName, isPublic: !lambdaIsStatic, isStatic: lambdaIsStatic);
                        CompilationState.ModuleBuilderOpt.AddSynthesizedDefinition(translatedLambdaContainer, cacheField);
                        cacheVariable = F.Field(receiver, cacheField.AsMember(constructedFrame)); //NOTE: the field was added to the unconstructed frame type.
                    }
                    else
                    {
                        // the lambda captures at most the "this" of the enclosing method.  We cache its delegate in a local variable.
                        var cacheLocal = F.SynthesizedLocal(type, cacheVariableName);
                        if (addedLocals == null) addedLocals = ArrayBuilder<LocalSymbol>.GetInstance();
                        addedLocals.Add(cacheLocal);
                        if (addedStatements == null) addedStatements = ArrayBuilder<BoundStatement>.GetInstance();
                        cacheVariable = F.Local(cacheLocal);
                        addedStatements.Add(F.Assignment(cacheVariable, F.Null(type)));
                    }

                    result = F.Coalesce(cacheVariable, F.AssignmentExpression(cacheVariable, result));
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
                switch (curSyntax.CSharpKind())
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
