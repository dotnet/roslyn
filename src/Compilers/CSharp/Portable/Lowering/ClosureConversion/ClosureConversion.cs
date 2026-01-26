// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

#if DEBUG
//#define CHECK_LOCALS // define CHECK_LOCALS to help debug some rewriting problems that would otherwise cause code-gen failures

#endif

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using Microsoft.CodeAnalysis.CodeGen;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.CodeAnalysis.PooledObjects;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp
{
    /// <summary>
    /// The rewriter for removing lambda expressions from method bodies and introducing closure classes
    /// as containers for captured variables along the lines of the example in section 6.5.3 of the
    /// C# language specification. A closure is the lowered form of a nested function, consisting of a
    /// synthesized method and a set of environments containing the captured variables.
    /// 
    /// The entry point is the public method <see cref="Rewrite"/>.  It operates as follows:
    /// 
    /// First, an analysis of the whole method body is performed that determines which variables are
    /// captured, what their scopes are, and what the nesting relationship is between scopes that
    /// have captured variables.  The result of this analysis is left in <see cref="_analysis"/>.
    /// 
    /// Then we make a frame, or compiler-generated class, represented by an instance of
    /// <see cref="SynthesizedClosureEnvironment"/> for each scope with captured variables. The generated frames are kept
    /// in <see cref="_frames"/>.  Each frame is given a single field for each captured
    /// variable in the corresponding scope.  These are maintained in <see cref="MethodToClassRewriter.proxies"/>.
    /// 
    /// Next, we walk and rewrite the input bound tree, keeping track of the following:
    /// (1) The current set of active frame pointers, in <see cref="_framePointers"/>
    /// (2) The current method being processed (this changes within a lambda's body), in <see cref="_currentMethod"/>
    /// (3) The "this" symbol for the current method in <see cref="_currentFrameThis"/>, and
    /// (4) The symbol that is used to access the innermost frame pointer (it could be a local variable or "this" parameter)
    ///
    /// Lastly, we visit the top-level method and each of the lowered methods
    /// to rewrite references (e.g., calls and delegate conversions) to local
    /// functions. We visit references to local functions separately from
    /// lambdas because we may see the reference before we lower the target
    /// local function. Lambdas, on the other hand, are always convertible as
    /// they are being lowered.
    /// 
    /// There are a few key transformations done in the rewriting.
    /// (1) Lambda expressions are turned into delegate creation expressions, and the body of the lambda is
    ///     moved into a new, compiler-generated method of a selected frame class.
    /// (2) On entry to a scope with captured variables, we create a frame object and store it in a local variable.
    /// (3) References to captured variables are transformed into references to fields of a frame class.
    /// 
    /// In addition, the rewriting deposits into <see cref="TypeCompilationState.SynthesizedMethods"/>
    /// a (<see cref="MethodSymbol"/>, <see cref="BoundStatement"/>) pair for each generated method.
    /// 
    /// <see cref="Rewrite"/> produces its output in two forms.  First, it returns a new bound statement
    /// for the caller to use for the body of the original method.  Second, it returns a collection of
    /// (<see cref="MethodSymbol"/>, <see cref="BoundStatement"/>) pairs for additional methods that the lambda rewriter produced.
    /// These additional methods contain the bodies of the lambdas moved into ordinary methods of their
    /// respective frame classes, and the caller is responsible for processing them just as it does with
    /// the returned bound node.  For example, the caller will typically perform iterator method and
    /// asynchronous method transformations, and emit IL instructions into an assembly.
    /// </summary>
    internal sealed partial class ClosureConversion : MethodToClassRewriter
    {
        private readonly Analysis _analysis;
        private readonly MethodSymbol _topLevelMethod;
        private readonly MethodSymbol _substitutedSourceMethod;
        private readonly int _topLevelMethodOrdinal;

        // lambda frame for static lambdas. 
        // initialized lazily and could be null if there are no static lambdas
        private SynthesizedClosureEnvironment _lazyStaticLambdaFrame;

        // A mapping from every lambda parameter to its corresponding method's parameter.
        private readonly Dictionary<ParameterSymbol, ParameterSymbol> _parameterMap = new Dictionary<ParameterSymbol, ParameterSymbol>();

        // for each block with lifted (captured) variables, the corresponding frame type
        private readonly Dictionary<BoundNode, Analysis.ClosureEnvironment> _frames = new Dictionary<BoundNode, Analysis.ClosureEnvironment>();

        // the current set of frame pointers in scope.  Each is either a local variable (where introduced),
        // or the "this" parameter when at the top level.  Keys in this map are never constructed types.
        private readonly Dictionary<NamedTypeSymbol, Symbol> _framePointers = new Dictionary<NamedTypeSymbol, Symbol>();

        // The set of original locals that should be assigned to proxies
        // if lifted. This is useful for the expression evaluator where
        // the original locals are left as is.
        private readonly HashSet<LocalSymbol> _assignLocals;

        // The current method or lambda being processed.
        private MethodSymbol _currentMethod;

        // The "this" symbol for the current method.
        private ParameterSymbol _currentFrameThis;

        private readonly ArrayBuilder<EncLambdaInfo> _lambdaDebugInfoBuilder;
        private readonly ArrayBuilder<LambdaRuntimeRudeEditInfo> _lambdaRuntimeRudeEditsBuilder;

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

        /// <summary>
        /// Temporary bag for methods synthesized by the rewriting. Added to
        /// <see cref="TypeCompilationState.SynthesizedMethods"/> at the end of rewriting.
        /// </summary>
        private ArrayBuilder<TypeCompilationState.MethodWithBody> _synthesizedMethods;

        /// <summary>
        /// TODO(https://github.com/dotnet/roslyn/projects/26): Delete this.
        /// This should only be used by <see cref="NeedsProxy(Symbol)"/> which
        /// hasn't had logic to move the proxy analysis into <see cref="Analysis"/>,
        /// where the <see cref="Analysis.ScopeTree"/> could be walked to build
        /// the proxy list.
        /// </summary>
        private readonly ImmutableHashSet<Symbol> _allCapturedVariables;

        /// <summary>
        /// Containing Symbols are not checked after this step - for performance reasons we can allow inaccurate locals
        /// </summary>
        protected override bool EnforceAccurateContainerForLocals => false;

#nullable enable

        private ClosureConversion(
            Analysis analysis,
            NamedTypeSymbol thisType,
            ParameterSymbol? thisParameter,
            MethodSymbol method,
            int methodOrdinal,
            MethodSymbol substitutedSourceMethod,
            ArrayBuilder<EncLambdaInfo> lambdaDebugInfoBuilder,
            ArrayBuilder<LambdaRuntimeRudeEditInfo> lambdaRuntimeRudeEditsBuilder,
            VariableSlotAllocator? slotAllocator,
            TypeCompilationState compilationState,
            BindingDiagnosticBag diagnostics,
            HashSet<LocalSymbol> assignLocals)
            : base(slotAllocator, compilationState, diagnostics)
        {
            RoslynDebug.Assert(analysis != null);
            RoslynDebug.Assert((object)thisType != null);
            RoslynDebug.Assert(method != null);
            RoslynDebug.Assert(compilationState != null);
            RoslynDebug.Assert(diagnostics != null);

            _topLevelMethod = method;
            _substitutedSourceMethod = substitutedSourceMethod;
            _topLevelMethodOrdinal = methodOrdinal;
            _lambdaDebugInfoBuilder = lambdaDebugInfoBuilder;
            _lambdaRuntimeRudeEditsBuilder = lambdaRuntimeRudeEditsBuilder;
            _currentMethod = method;
            _analysis = analysis;
            _assignLocals = assignLocals;
            _currentTypeParameters = method.TypeParameters;
            _currentLambdaBodyTypeMap = TypeMap.Empty;
            _innermostFramePointer = _currentFrameThis = thisParameter;
            _framePointers[thisType] = thisParameter;
            _seenBaseCall = method.MethodKind != MethodKind.Constructor; // only used for ctors
            _synthesizedFieldNameIdDispenser = 1;

            var allCapturedVars = ImmutableHashSet.CreateBuilder<Symbol>();
            Analysis.VisitNestedFunctions(analysis.ScopeTree, (scope, function) =>
            {
                allCapturedVars.UnionWith(function.CapturedVariables);
            });
            _allCapturedVariables = allCapturedVars.ToImmutable();
        }

        protected override bool NeedsProxy(Symbol localOrParameter)
        {
            Debug.Assert(localOrParameter is LocalSymbol || localOrParameter is ParameterSymbol ||
                (localOrParameter as MethodSymbol)?.MethodKind == MethodKind.LocalFunction);
            return _allCapturedVariables.Contains(localOrParameter);
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
        /// <param name="lambdaRuntimeRudeEditsBuilder">EnC rude edit information on lambdas defined in <paramref name="method"/>.</param>
        /// <param name="closureDebugInfoBuilder">Information on closures defined in <paramref name="method"/> needed for debugging.</param>
        /// <param name="slotAllocator">Slot allocator.</param>
        /// <param name="compilationState">The caller's buffer into which we produce additional methods to be emitted by the caller</param>
        /// <param name="diagnostics">Diagnostic bag for diagnostics</param>
        /// <param name="assignLocals">The set of original locals that should be assigned to proxies if lifted</param>
        public static BoundStatement Rewrite(
            BoundStatement loweredBody,
            NamedTypeSymbol thisType,
            ParameterSymbol? thisParameter,
            MethodSymbol method,
            int methodOrdinal,
            MethodSymbol substitutedSourceMethod,
            ArrayBuilder<EncLambdaInfo> lambdaDebugInfoBuilder,
            ArrayBuilder<LambdaRuntimeRudeEditInfo> lambdaRuntimeRudeEditsBuilder,
            ArrayBuilder<EncClosureInfo> closureDebugInfoBuilder,
            VariableSlotAllocator? slotAllocator,
            TypeCompilationState compilationState,
            BindingDiagnosticBag diagnostics,
            HashSet<LocalSymbol> assignLocals)
        {
            Debug.Assert(thisType is not null);
            Debug.Assert(thisParameter is null || TypeSymbol.Equals(thisParameter.Type, thisType, TypeCompareKind.ConsiderEverything2));
            Debug.Assert(compilationState.ModuleBuilderOpt != null);
            Debug.Assert(diagnostics.DiagnosticBag != null);

            var analysis = Analysis.Analyze(
                loweredBody,
                method,
                methodOrdinal,
                slotAllocator,
                compilationState,
                diagnostics.DiagnosticBag);

            CheckLocalsDefined(loweredBody);
            var rewriter = new ClosureConversion(
                analysis,
                thisType,
                thisParameter,
                method,
                methodOrdinal,
                substitutedSourceMethod,
                lambdaDebugInfoBuilder,
                lambdaRuntimeRudeEditsBuilder,
                slotAllocator,
                compilationState,
                diagnostics,
                assignLocals);

            rewriter.SynthesizeClosureEnvironments(closureDebugInfoBuilder);
            rewriter.SynthesizeClosureMethods();

            var body = rewriter.AddStatementsIfNeeded(
                (BoundStatement)rewriter.Visit(loweredBody));

            // Add the completed methods to the compilation state
            if (rewriter._synthesizedMethods != null)
            {
                if (compilationState.SynthesizedMethods == null)
                {
                    compilationState.SynthesizedMethods = rewriter._synthesizedMethods;
                }
                else
                {
                    compilationState.SynthesizedMethods.AddRange(rewriter._synthesizedMethods);
                    rewriter._synthesizedMethods.Free();
                }
            }

            CheckLocalsDefined(body);

            analysis.Free();

            return body;
        }
#nullable disable
        private BoundStatement AddStatementsIfNeeded(BoundStatement body)
        {
            if (_addedLocals != null)
            {
                _addedStatements.Add(body);
                body = new BoundBlock(body.Syntax, _addedLocals.ToImmutableAndFree(), _addedStatements.ToImmutableAndFree()) { WasCompilerGenerated = true };
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
        /// Adds <see cref="SynthesizedClosureEnvironment"/> synthesized types to the compilation state
        /// and creates hoisted fields for all locals captured by the environments.
        /// </summary>
        private void SynthesizeClosureEnvironments(ArrayBuilder<EncClosureInfo> closureDebugInfo)
        {
            Analysis.VisitScopeTree(_analysis.ScopeTree, scope =>
            {
                if (scope.DeclaredEnvironment is { } env)
                {
                    Debug.Assert(!_frames.ContainsKey(scope.BoundNode));

                    var frame = MakeFrame(scope, env);
                    env.SynthesizedEnvironment = frame;

                    CompilationState.ModuleBuilderOpt.AddSynthesizedDefinition(ContainingType, frame.GetCciAdapter());
                    if (frame.Constructor != null)
                    {
                        AddSynthesizedMethod(
                            frame.Constructor,
                            FlowAnalysisPass.AppendImplicitReturn(
                                MethodCompiler.BindSynthesizedMethodBody(frame.Constructor, CompilationState, Diagnostics),
                                frame.Constructor));
                    }

                    _frames.Add(scope.BoundNode, env);
                }
            });

            SynthesizedClosureEnvironment MakeFrame(Analysis.Scope scope, Analysis.ClosureEnvironment env)
            {
                var scopeBoundNode = scope.BoundNode;

                var syntax = scopeBoundNode.Syntax;
                Debug.Assert(syntax != null);

                DebugId methodId = _analysis.GetTopLevelMethodId();
                DebugId closureId = _analysis.GetClosureId(env, syntax, closureDebugInfo, out var rudeEdit);

                var containingMethod = scope.ContainingFunctionOpt?.OriginalMethodSymbol ?? _topLevelMethod;
                if ((object)_substitutedSourceMethod != null && containingMethod == _topLevelMethod)
                {
                    containingMethod = _substitutedSourceMethod;
                }

                var synthesizedEnv = new SynthesizedClosureEnvironment(
                    _topLevelMethod,
                    containingMethod,
                    env.IsStruct,
                    syntax,
                    methodId,
                    closureId,
                    rudeEdit);

                foreach (var captured in env.CapturedVariables)
                {
                    Debug.Assert(!proxies.ContainsKey(captured));

                    var hoistedField = LambdaCapturedVariable.Create(synthesizedEnv, captured, ref _synthesizedFieldNameIdDispenser);
                    proxies.Add(captured, new CapturedToFrameSymbolReplacement(hoistedField, isReusable: false));
                    synthesizedEnv.AddHoistedField(hoistedField);
                    CompilationState.ModuleBuilderOpt.AddSynthesizedDefinition(synthesizedEnv, hoistedField.GetCciAdapter());
                }

                return synthesizedEnv;
            }
        }

        /// <summary>
        /// Synthesize closure methods for all nested functions.
        /// </summary>
        private void SynthesizeClosureMethods()
        {
            Analysis.VisitNestedFunctions(_analysis.ScopeTree, (scope, nestedFunction) =>
            {
                var originalMethod = nestedFunction.OriginalMethodSymbol;
                var syntax = originalMethod.DeclaringSyntaxReferences[0].GetSyntax();

                int closureOrdinal;
                ClosureKind closureKind;
                NamedTypeSymbol translatedLambdaContainer;
                SynthesizedClosureEnvironment containerAsFrame;
                DebugId topLevelMethodId;
                DebugId lambdaId;

                if (nestedFunction.ContainingEnvironmentOpt != null)
                {
                    containerAsFrame = nestedFunction.ContainingEnvironmentOpt.SynthesizedEnvironment;
                    translatedLambdaContainer = containerAsFrame;

                    closureKind = ClosureKind.General;
                    closureOrdinal = containerAsFrame.ClosureId.Ordinal;
                }
                else if (nestedFunction.CapturesThis)
                {
                    containerAsFrame = null;
                    translatedLambdaContainer = _topLevelMethod.ContainingType;
                    closureKind = ClosureKind.ThisOnly;
                    closureOrdinal = LambdaDebugInfo.ThisOnlyClosureOrdinal;
                }
                else if ((nestedFunction.CapturedEnvironments.Count == 0 &&
                          originalMethod.MethodKind == MethodKind.LambdaMethod &&
                          _analysis.MethodsConvertedToDelegates.Contains(originalMethod)) ||
                         // If we are in a variant interface, runtime might not consider the 
                         // method synthesized directly within the interface as variant safe.
                         // For simplicity we do not perform precise analysis whether this would
                         // definitely be the case. If we are in a variant interface, we always force
                         // creation of a display class.
                         VarianceSafety.GetEnclosingVariantInterface(_topLevelMethod) is object)
                {
                    translatedLambdaContainer = containerAsFrame = GetStaticFrame(Diagnostics, syntax);
                    closureKind = ClosureKind.Singleton;
                    closureOrdinal = LambdaDebugInfo.StaticClosureOrdinal;
                }
                else
                {
                    // Lower directly onto the containing type
                    containerAsFrame = null;
                    translatedLambdaContainer = _topLevelMethod.ContainingType;
                    closureKind = ClosureKind.Static;
                    closureOrdinal = LambdaDebugInfo.StaticClosureOrdinal;
                }

                Debug.Assert((object)translatedLambdaContainer != _topLevelMethod.ContainingType ||
                             VarianceSafety.GetEnclosingVariantInterface(_topLevelMethod) is null);

                var structEnvironments = getStructEnvironments(nestedFunction);

                // Move the body of the lambda to a freshly generated synthetic method on its frame.
                topLevelMethodId = _analysis.GetTopLevelMethodId();
                lambdaId = GetLambdaId(syntax, closureKind, closureOrdinal, structEnvironments.SelectAsArray(e => e.ClosureId), containerAsFrame?.RudeEdit);

                var synthesizedMethod = new SynthesizedClosureMethod(
                    translatedLambdaContainer,
                    structEnvironments,
                    closureKind,
                    _topLevelMethod,
                    topLevelMethodId,
                    originalMethod,
                    nestedFunction.BlockSyntax,
                    lambdaId,
                    CompilationState);
                nestedFunction.SynthesizedLoweredMethod = synthesizedMethod;
            });

            static ImmutableArray<SynthesizedClosureEnvironment> getStructEnvironments(Analysis.NestedFunction function)
            {
                var environments = ArrayBuilder<SynthesizedClosureEnvironment>.GetInstance();

                foreach (var env in function.CapturedEnvironments)
                {
                    if (env.IsStruct)
                    {
                        environments.Add(env.SynthesizedEnvironment);
                    }
                }

                return environments.ToImmutableAndFree();
            }
        }

        /// <summary>
        /// Get the static container for closures or create one if one doesn't already exist.
        /// </summary>
        /// <param name="syntax">
        /// associate the frame with the first lambda that caused it to exist. 
        /// we need to associate this with some syntax.
        /// unfortunately either containing method or containing class could be synthetic
        /// therefore could have no syntax.
        /// </param>
        private SynthesizedClosureEnvironment GetStaticFrame(BindingDiagnosticBag diagnostics, SyntaxNode syntax)
        {
            if ((object)_lazyStaticLambdaFrame == null)
            {
                var isNonGeneric = !_topLevelMethod.IsGenericMethod;
                if (isNonGeneric)
                {
                    _lazyStaticLambdaFrame = CompilationState.StaticLambdaFrame;
                }

                if ((object)_lazyStaticLambdaFrame == null)
                {
                    DebugId methodId;
                    if (isNonGeneric)
                    {
                        methodId = new DebugId(DebugId.UndefinedOrdinal, CompilationState.ModuleBuilderOpt.CurrentGenerationOrdinal);
                    }
                    else
                    {
                        methodId = _analysis.GetTopLevelMethodId();
                    }

                    // using _topLevelMethod as containing member because the static frame does not have generic parameters, except for the top level method's
                    var containingMethod = isNonGeneric ? null : (_substitutedSourceMethod ?? _topLevelMethod);
                    _lazyStaticLambdaFrame = new SynthesizedClosureEnvironment(
                        _topLevelMethod,
                        containingMethod,
                        isStruct: false,
                        scopeSyntaxOpt: null,
                        methodId: methodId,
                        closureId: default,
                        rudeEdit: null);

                    // non-generic static lambdas can share the frame
                    if (isNonGeneric)
                    {
                        CompilationState.StaticLambdaFrame = _lazyStaticLambdaFrame;
                    }

                    var frame = _lazyStaticLambdaFrame;

                    // add frame type and cache field
                    CompilationState.ModuleBuilderOpt.AddSynthesizedDefinition(this.ContainingType, frame.GetCciAdapter());

                    // add its ctor (note Constructor can be null if TypeKind.Struct is passed in to LambdaFrame.ctor, but Class is passed in above)
                    AddSynthesizedMethod(
                        frame.Constructor,
                        FlowAnalysisPass.AppendImplicitReturn(
                            MethodCompiler.BindSynthesizedMethodBody(frame.Constructor, CompilationState, diagnostics),
                            frame.Constructor));

                    // add cctor
                    // Frame.inst = new Frame()
                    var F = new SyntheticBoundNodeFactory(frame.StaticConstructor, syntax, CompilationState, diagnostics);
                    var body = F.Block(
                            F.Assignment(
                                F.Field(null, frame.SingletonCache),
                                F.New(frame.Constructor)),
                            new BoundReturnStatement(syntax, RefKind.None, null, @checked: false));

                    AddSynthesizedMethod(frame.StaticConstructor, body);
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
        private BoundExpression FrameOfType(SyntaxNode syntax, NamedTypeSymbol frameType)
        {
            BoundExpression result = FramePointer(syntax, frameType.OriginalDefinition);
            Debug.Assert(TypeSymbol.Equals(result.Type, frameType, TypeCompareKind.ConsiderEverything2));
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
        protected override BoundExpression FramePointer(SyntaxNode syntax, NamedTypeSymbol frameClass)
        {
            Debug.Assert(frameClass.IsDefinition);

            // If in an instance method of the right type, we can just return the "this" pointer.
            if ((object)_currentFrameThis != null && TypeSymbol.Equals(_currentFrameThis.Type, frameClass, TypeCompareKind.ConsiderEverything2))
            {
                return new BoundThisReference(syntax, frameClass);
            }

            // If the current method has by-ref struct closure parameters, and one of them is correct, use it.
            var lambda = _currentMethod as SynthesizedClosureMethod;
            if (lambda != null)
            {
                var start = lambda.ParameterCount - lambda.ExtraSynthesizedParameterCount;
                for (var i = start; i < lambda.ParameterCount; i++)
                {
                    var potentialParameter = lambda.Parameters[i];
                    if (TypeSymbol.Equals(potentialParameter.Type.OriginalDefinition, frameClass, TypeCompareKind.ConsiderEverything2))
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
                return proxyField.Replacement(
                    syntax,
                    static (frameType, arg) => arg.self.FramePointer(arg.syntax, frameType),
                    (syntax, self: this));
            }

            var localFrame = (LocalSymbol)framePointer;
            return new BoundLocal(syntax, localFrame, null, localFrame.Type);
        }

        private static void InsertAndFreePrologue<T>(ArrayBuilder<BoundStatement> result, ArrayBuilder<T> prologue) where T : BoundNode
        {
            foreach (var node in prologue)
            {
                if (node is BoundStatement stmt)
                {
                    result.Add(stmt);
                }
                else
                {
                    result.Add(new BoundExpressionStatement(node.Syntax, (BoundExpression)(BoundNode)node));
                }
            }

            prologue.Free();
        }

        /// <summary>
        /// Introduce a frame around the translation of the given node.
        /// </summary>
        /// <param name="node">The node whose translation should be translated to contain a frame</param>
        /// <param name="env">The environment for the translated node</param>
        /// <param name="F">A function that computes the translation of the node.  It receives lists of added statements and added symbols</param>
        /// <returns>The translated statement, as returned from F</returns>
        private BoundNode IntroduceFrame(BoundNode node, Analysis.ClosureEnvironment env, Func<ArrayBuilder<BoundExpression>, ArrayBuilder<LocalSymbol>, BoundNode> F)
        {
            var frame = env.SynthesizedEnvironment;
            var frameTypeParameters = ImmutableArray.Create(_currentTypeParameters.SelectAsArray(t => TypeWithAnnotations.Create(t)), 0, frame.Arity);
            NamedTypeSymbol frameType = frame.ConstructIfGeneric(frameTypeParameters);

            Debug.Assert(frame.ScopeSyntaxOpt != null);
            LocalSymbol framePointer = new SynthesizedLocal(_topLevelMethod, TypeWithAnnotations.Create(frameType), SynthesizedLocalKind.LambdaDisplayClass, frame.ScopeSyntaxOpt);

            SyntaxNode syntax = node.Syntax;

            // assign new frame to the frame variable

            var prologue = ArrayBuilder<BoundExpression>.GetInstance();

            if ((object)frame.Constructor != null)
            {
                MethodSymbol constructor = frame.Constructor.AsMember(frameType);
                Debug.Assert(TypeSymbol.Equals(frameType, constructor.ContainingType, TypeCompareKind.ConsiderEverything2));

                prologue.Add(new BoundAssignmentOperator(syntax,
                    new BoundLocal(syntax, framePointer, null, frameType),
                    new BoundObjectCreationExpression(syntax: syntax, constructor: constructor),
                    frameType));
            }

            CapturedSymbolReplacement oldInnermostFrameProxy = null;
            if ((object)_innermostFramePointer != null)
            {
                proxies.TryGetValue(_innermostFramePointer, out oldInnermostFrameProxy);
                if (env.CapturesParent)
                {
                    var capturedFrame = LambdaCapturedVariable.Create(frame, _innermostFramePointer, ref _synthesizedFieldNameIdDispenser);
                    FieldSymbol frameParent = capturedFrame.AsMember(frameType);
                    BoundExpression left = new BoundFieldAccess(syntax, new BoundLocal(syntax, framePointer, null, frameType), frameParent, null);
                    BoundExpression right = FrameOfType(syntax, frameParent.Type as NamedTypeSymbol);
                    BoundExpression assignment = new BoundAssignmentOperator(syntax, left, right, left.Type);
                    prologue.Add(assignment);

                    if (CompilationState.Emitting)
                    {
                        Debug.Assert(capturedFrame.Type.IsReferenceType); // Make sure we're not accidentally capturing a struct by value
                        frame.AddHoistedField(capturedFrame);
                        CompilationState.ModuleBuilderOpt.AddSynthesizedDefinition(frame, capturedFrame.GetCciAdapter());
                    }

                    proxies[_innermostFramePointer] = new CapturedToFrameSymbolReplacement(capturedFrame, isReusable: false);
                }
            }

            // Capture any parameters of this block.  This would typically occur
            // at the top level of a method or lambda with captured parameters.
            foreach (var variable in env.CapturedVariables)
            {
                InitVariableProxy(syntax, variable, framePointer, prologue);
            }

            Symbol oldInnermostFramePointer = _innermostFramePointer;
            if (!framePointer.Type.IsValueType)
            {
                _innermostFramePointer = framePointer;
            }
            var addedLocals = ArrayBuilder<LocalSymbol>.GetInstance();
            addedLocals.Add(framePointer);
            _framePointers.Add(frame, framePointer);

            var result = F(prologue, addedLocals);

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

        private void InitVariableProxy(SyntaxNode syntax, Symbol symbol, LocalSymbol framePointer, ArrayBuilder<BoundExpression> prologue)
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
                        var local = (LocalSymbol)symbol;
                        if (_assignLocals == null || !_assignLocals.Contains(local))
                        {
                            return;
                        }

                        LocalSymbol localToUse;
                        if (!TryGetRewrittenLocal(local, out localToUse))
                        {
                            localToUse = local;
                        }
                        else
                        {
                            Debug.Assert(false);
                        }

                        value = new BoundLocal(syntax, localToUse, null, localToUse.Type);
                        break;

                    default:
                        throw ExceptionUtilities.UnexpectedValue(symbol.Kind);
                }

                var left = proxy.Replacement(
                    syntax,
                    static (frameType1, arg) => new BoundLocal(arg.syntax, arg.framePointer, null, arg.framePointer.Type),
                    (syntax, framePointer));

                var assignToProxy = new BoundAssignmentOperator(syntax, left, value, value.Type);
                if (_currentMethod.MethodKind == MethodKind.Constructor &&
                    symbol == _currentMethod.ThisParameter &&
                    !_seenBaseCall &&
                    // Primary constructor doesn't have any user code after base constructor initializer.
                    // Therefore, if we detected a proxy for 'this', it must be used to refer in a lambda
                    // to a constructor parameter captured into the containing type state.
                    // That lambda could be executed before the base constructor initializer, or by
                    // the base constructor initializer. That is why we cannot defer the proxy
                    // initialization until after the base constructor initializer is executed.
                    // Even though that is going to be an unverifiable IL.
                    _currentMethod is not SynthesizedPrimaryConstructor)
                {
                    // Containing method is a constructor 
                    // Initialization statement for the "this" proxy must be inserted
                    // after the constructor initializer statement block
                    Debug.Assert(_thisProxyInitDeferred == null);
                    _thisProxyInitDeferred = assignToProxy;
                }
                else
                {
                    Debug.Assert(_currentMethod is not SynthesizedPrimaryConstructor primaryConstructor ||
                                 symbol != _currentMethod.ThisParameter ||
                                 primaryConstructor.GetCapturedParameters().Any());
                    prologue.Add(assignToProxy);
                }
            }
        }

        #region Visit Methods

        protected override BoundNode VisitUnhoistedParameter(BoundParameter node)
        {
            ParameterSymbol replacementParameter;
            if (_parameterMap.TryGetValue(node.ParameterSymbol, out replacementParameter))
            {
                return new BoundParameter(node.Syntax, replacementParameter, node.HasErrors);
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
            return (!_currentMethod.IsStatic && TypeSymbol.Equals(_currentMethod.ContainingType, _topLevelMethod.ContainingType, TypeCompareKind.ConsiderEverything2))
                ? node
                : FramePointer(node.Syntax, _topLevelMethod.ContainingType); // technically, not the correct static type
        }

        public override BoundNode VisitMethodDefIndex(BoundMethodDefIndex node)
        {
            TypeSymbol type = VisitType(node.Type);

            var loweredSymbol = (node.Method.MethodKind is MethodKind.LambdaMethod or MethodKind.LocalFunction) ?
                Analysis.GetNestedFunctionInTree(_analysis.ScopeTree, node.Method.OriginalDefinition).SynthesizedLoweredMethod : node.Method;

            return node.Update(loweredSymbol, type);
        }

        /// <summary>
        /// Rewrites a reference to an unlowered local function to the newly
        /// lowered local function.
        /// </summary>
        private void RemapLocalFunction(
            SyntaxNode syntax,
            MethodSymbol localFunc,
            out BoundExpression receiver,
            out MethodSymbol method,
            ref ImmutableArray<BoundExpression> arguments,
            ref ImmutableArray<RefKind> argRefKinds)
        {
            Debug.Assert(localFunc.MethodKind == MethodKind.LocalFunction);

            var function = Analysis.GetNestedFunctionInTree(_analysis.ScopeTree, localFunc.OriginalDefinition);
            var loweredSymbol = function.SynthesizedLoweredMethod;

            // If the local function captured variables then they will be stored
            // in frames and the frames need to be passed as extra parameters.
            var frameCount = loweredSymbol.ExtraSynthesizedParameterCount;
            if (frameCount != 0)
            {
                Debug.Assert(!arguments.IsDefault);

                // Build a new list of arguments to pass to the local function
                // call that includes any necessary capture frames
                var argumentsBuilder = ArrayBuilder<BoundExpression>.GetInstance(loweredSymbol.ParameterCount);
                argumentsBuilder.AddRange(arguments);

                var start = loweredSymbol.ParameterCount - frameCount;
                for (int i = start; i < loweredSymbol.ParameterCount; i++)
                {
                    // will always be a LambdaFrame, it's always a capture frame
                    var frameType = (NamedTypeSymbol)loweredSymbol.Parameters[i].Type.OriginalDefinition;

                    Debug.Assert(frameType is SynthesizedClosureEnvironment);

                    if (frameType.Arity > 0)
                    {
                        var typeParameters = ((SynthesizedClosureEnvironment)frameType).ConstructedFromTypeParameters;
                        Debug.Assert(typeParameters.Length == frameType.Arity);
                        var subst = this.TypeMap.SubstituteTypeParameters(typeParameters);
                        frameType = frameType.Construct(subst);
                    }

                    var frame = FrameOfType(syntax, frameType);
                    argumentsBuilder.Add(frame);
                }

                // frame arguments are passed by ref
                // add corresponding refkinds
                var refkindsBuilder = ArrayBuilder<RefKind>.GetInstance(argumentsBuilder.Count);
                if (!argRefKinds.IsDefault)
                {
                    refkindsBuilder.AddRange(argRefKinds);
                }
                else
                {
                    refkindsBuilder.AddMany(RefKind.None, arguments.Length);
                }

                refkindsBuilder.AddMany(RefKind.Ref, frameCount);

                arguments = argumentsBuilder.ToImmutableAndFree();
                argRefKinds = refkindsBuilder.ToImmutableAndFree();
            }

            method = loweredSymbol;
            NamedTypeSymbol constructedFrame;

            RemapLambdaOrLocalFunction(syntax,
                                       localFunc,
                                       SubstituteTypeArguments(localFunc.TypeArgumentsWithAnnotations),
                                       loweredSymbol.ClosureKind,
                                       ref method,
                                       out receiver,
                                       out constructedFrame);
        }

        /// <summary>
        /// Substitutes references from old type arguments to new type arguments
        /// in the lowered methods.
        /// </summary>
        /// <example>
        /// Consider the following method:
        ///     void M() {
        ///         void L&lt;T&gt;(T t) => Console.Write(t);
        ///         L("A");
        ///     }
        ///     
        /// In this example, L&lt;T&gt; is a local function that will be
        /// lowered into its own method and the type parameter T will be
        /// alpha renamed to something else (let's call it T'). In this case,
        /// all references to the original type parameter T in L must be
        /// rewritten to the renamed parameter, T'.
        /// </example>
        private ImmutableArray<TypeWithAnnotations> SubstituteTypeArguments(ImmutableArray<TypeWithAnnotations> typeArguments)
        {
            Debug.Assert(!typeArguments.IsDefault);

            if (typeArguments.IsEmpty)
            {
                return typeArguments;
            }

            // We must perform this process repeatedly as local
            // functions may nest inside one another and capture type
            // parameters from the enclosing local functions. Each
            // iteration of nesting will cause alpha-renaming of the captured
            // parameters, meaning that we must replace until there are no
            // more alpha-rename mappings.
            //
            // The method symbol references are different from all other
            // substituted types in this context because the method symbol in
            // local function references is not rewritten until all local
            // functions have already been lowered. Everything else is rewritten
            // by the visitors as the definition is lowered. This means that
            // only one substitution happens per lowering, but we need to do
            // N substitutions all at once, where N is the number of lowerings.

            var builder = ArrayBuilder<TypeWithAnnotations>.GetInstance(typeArguments.Length);
            foreach (var typeArg in typeArguments)
            {
                TypeWithAnnotations oldTypeArg;
                TypeWithAnnotations newTypeArg = typeArg;
                do
                {
                    oldTypeArg = newTypeArg;
                    newTypeArg = this.TypeMap.SubstituteType(oldTypeArg);
                }
                while (!TypeSymbol.Equals(oldTypeArg.Type, newTypeArg.Type, TypeCompareKind.ConsiderEverything));

                // When type substitution does not change the type, it is expected to return the very same object.
                // Therefore the loop is terminated when that type (as an object) does not change.
                Debug.Assert((object)oldTypeArg.Type == newTypeArg.Type);

                // The types are the same, so the last pass performed no substitutions.
                // Therefore the annotations ought to be the same too.
                Debug.Assert(oldTypeArg.NullableAnnotation == newTypeArg.NullableAnnotation);

                builder.Add(newTypeArg);
            }

            return builder.ToImmutableAndFree();
        }

        private void RemapLambdaOrLocalFunction(
            SyntaxNode syntax,
            MethodSymbol originalMethod,
            ImmutableArray<TypeWithAnnotations> typeArgumentsOpt,
            ClosureKind closureKind,
            ref MethodSymbol synthesizedMethod,
            out BoundExpression receiver,
            out NamedTypeSymbol constructedFrame)
        {
            var translatedLambdaContainer = synthesizedMethod.ContainingType;
            var containerAsFrame = translatedLambdaContainer as SynthesizedClosureEnvironment;

            // All of _currentTypeParameters might not be preserved here due to recursively calling upwards in the chain of local functions/lambdas
            Debug.Assert((typeArgumentsOpt.IsDefault && !originalMethod.IsGenericMethod) || (typeArgumentsOpt.Length == originalMethod.Arity));
            var totalTypeArgumentCount = (containerAsFrame?.Arity ?? 0) + synthesizedMethod.Arity;
            var realTypeArguments = ImmutableArray.Create(_currentTypeParameters.SelectAsArray(t => TypeWithAnnotations.Create(t)), 0, totalTypeArgumentCount - originalMethod.Arity);
            if (!typeArgumentsOpt.IsDefault)
            {
                realTypeArguments = realTypeArguments.Concat(typeArgumentsOpt);
            }

            if ((object)containerAsFrame != null && containerAsFrame.Arity != 0)
            {
                var containerTypeArguments = ImmutableArray.Create(realTypeArguments, 0, containerAsFrame.Arity);
                realTypeArguments = ImmutableArray.Create(realTypeArguments, containerAsFrame.Arity, realTypeArguments.Length - containerAsFrame.Arity);
                constructedFrame = containerAsFrame.Construct(containerTypeArguments);
            }
            else
            {
                constructedFrame = translatedLambdaContainer;
            }

            synthesizedMethod = synthesizedMethod.AsMember(constructedFrame);
            if (synthesizedMethod.IsGenericMethod)
            {
                synthesizedMethod = synthesizedMethod.Construct(realTypeArguments);
            }
            else
            {
                Debug.Assert(realTypeArguments.Length == 0);
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
                receiver = new BoundTypeExpression(syntax, null, synthesizedMethod.ContainingType);
            }
            else // ThisOnly and General
            {
                receiver = FrameOfType(syntax, constructedFrame);
            }
        }

        public override BoundNode VisitCall(BoundCall node)
        {
            if (node.Method.MethodKind == MethodKind.LocalFunction)
            {
                var args = VisitList(node.Arguments);
                var argRefKinds = node.ArgumentRefKindsOpt;
                var type = VisitType(node.Type);

                Debug.Assert(node.ArgsToParamsOpt.IsDefault, "should be done with argument reordering by now");

                RemapLocalFunction(
                    node.Syntax,
                    node.Method,
                    out var receiver,
                    out var method,
                    ref args,
                    ref argRefKinds);

                return node.Update(
                    receiver,
                    node.InitialBindingReceiverIsSubjectToCloning,
                    method,
                    args,
                    node.ArgumentNamesOpt,
                    argRefKinds,
                    node.IsDelegateCall,
                    node.Expanded,
                    node.InvokedAsExtensionMethod,
                    node.ArgsToParamsOpt,
                    node.DefaultArguments,
                    node.ResultKind,
                    type);
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
                if (_currentMethod == _topLevelMethod && node.IsConstructorInitializer())
                {
                    _seenBaseCall = true;
                    if (_thisProxyInitDeferred != null)
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
            }

            return rewritten;
        }

        private BoundSequence RewriteSequence(BoundSequence node, ArrayBuilder<BoundExpression> prologue, ArrayBuilder<LocalSymbol> newLocals)
        {
            RewriteLocals(node.Locals, newLocals);

            foreach (var effect in node.SideEffects)
            {
                var replacement = (BoundExpression)this.Visit(effect);
                if (replacement != null) prologue.Add(replacement);
            }

            var newValue = (BoundExpression)this.Visit(node.Value);
            var newType = this.VisitType(node.Type);

            return node.Update(newLocals.ToImmutableAndFree(), prologue.ToImmutableAndFree(), newValue, newType);
        }

        public override BoundNode VisitBlock(BoundBlock node)
        {
            // Test if this frame has captured variables and requires the introduction of a closure class.
            if (_frames.TryGetValue(node, out var frame))
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
                newStatements.Add(BoundSequencePoint.CreateHidden());
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

            var newInstrumentation = node.Instrumentation;
            if (newInstrumentation != null)
            {
                var newPrologue = (BoundStatement)Visit(newInstrumentation.Prologue);
                var newEpilogue = (BoundStatement)Visit(newInstrumentation.Epilogue);
                newInstrumentation = newInstrumentation.Update(newInstrumentation.Locals, newPrologue, newEpilogue);
            }

            // TODO: we may not need to update if there was nothing to rewrite.
            return node.Update(newLocals.ToImmutableAndFree(), node.LocalFunctions, node.HasUnsafeModifier, newInstrumentation, newStatements.ToImmutableAndFree());
        }

        public override BoundNode VisitScope(BoundScope node)
        {
            Debug.Assert(!node.Locals.IsEmpty);
            var newLocals = VisitLocals(node.Locals);

            var statements = VisitList(node.Statements);
            if (newLocals.Length == 0)
            {
                return new BoundStatementList(node.Syntax, statements);
            }

            return node.Update(newLocals, statements);
        }

        public override BoundNode VisitCatchBlock(BoundCatchBlock node)
        {
            // Test if this frame has captured variables and requires the introduction of a closure class.
            if (_frames.TryGetValue(node, out var frame))
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
            RewriteLocals(node.Locals, newLocals);
            var rewrittenCatchLocals = newLocals.ToImmutableAndFree();

            // If exception variable got lifted, IntroduceFrame will give us frame init prologue.
            // It needs to run before the exception variable is accessed.
            // To ensure that, we will make exception variable a sequence that performs prologue as its side-effects.
            BoundExpression rewrittenExceptionSource = null;
            var rewrittenFilterPrologue = (BoundStatementList)this.Visit(node.ExceptionFilterPrologueOpt);
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
                var prologueBuilder = ArrayBuilder<BoundStatement>.GetInstance(prologue.Count);
                foreach (var p in prologue)
                {
                    prologueBuilder.Add(new BoundExpressionStatement(p.Syntax, p) { WasCompilerGenerated = true });
                }
                if (rewrittenFilterPrologue != null)
                {
                    prologueBuilder.AddRange(rewrittenFilterPrologue.Statements);
                }

                rewrittenFilterPrologue = new BoundStatementList(rewrittenFilter.Syntax, prologueBuilder.ToImmutableAndFree());
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
                rewrittenFilterPrologue,
                rewrittenFilter,
                rewrittenBlock,
                node.IsSynthesizedAsyncCatchAll);
        }

        public override BoundNode VisitSequence(BoundSequence node)
        {
            // Test if this frame has captured variables and requires the introduction of a closure class.
            if (_frames.TryGetValue(node, out var frame))
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
            // Test if this frame has captured variables and requires the introduction of a closure class.
            // That can occur for a BoundStatementList if it is the body of a method with captured parameters.
            if (_frames.TryGetValue(node, out var frame))
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

        public override BoundNode VisitDelegateCreationExpression(BoundDelegateCreationExpression node)
        {
            // A delegate creation expression of the form "new Action( ()=>{} )" is treated exactly like
            // (Action)(()=>{})
            if (node.Argument.Kind == BoundKind.Lambda)
            {
                return RewriteLambdaConversion((BoundLambda)node.Argument);
            }

            if (node.MethodOpt?.MethodKind == MethodKind.LocalFunction)
            {
                var arguments = default(ImmutableArray<BoundExpression>);
                var argRefKinds = default(ImmutableArray<RefKind>);

                RemapLocalFunction(
                    node.Syntax,
                    node.MethodOpt,
                    out var receiver,
                    out var method,
                    ref arguments,
                    ref argRefKinds);

                return new BoundDelegateCreationExpression(
                    node.Syntax,
                    receiver,
                    method,
                    node.IsExtensionMethod,
                    node.WasTargetTyped,
                    VisitType(node.Type));
            }
            return base.VisitDelegateCreationExpression(node);
        }

        public override BoundNode VisitFunctionPointerLoad(BoundFunctionPointerLoad node)
        {
            if (node.TargetMethod.MethodKind == MethodKind.LocalFunction)
            {
                Debug.Assert(node.TargetMethod is { RequiresInstanceReceiver: false, IsStatic: true });
                ImmutableArray<BoundExpression> arguments = default;
                ImmutableArray<RefKind> argRefKinds = default;

                RemapLocalFunction(
                    node.Syntax,
                    node.TargetMethod,
                    out BoundExpression receiver,
                    out MethodSymbol remappedMethod,
                    ref arguments,
                    ref argRefKinds);

                Debug.Assert(arguments.IsDefault &&
                             argRefKinds.IsDefault &&
                             receiver.Kind == BoundKind.TypeExpression &&
                             remappedMethod is { RequiresInstanceReceiver: false, IsStatic: true });

                return node.Update(remappedMethod, constrainedToTypeOpt: node.ConstrainedToTypeOpt, node.Type);
            }

            return base.VisitFunctionPointerLoad(node);
        }

        public override BoundNode VisitConversion(BoundConversion conversion)
        {
            // a conversion with a method should have been rewritten, e.g. to an invocation
            Debug.Assert(_inExpressionLambda || conversion.Conversion.MethodSymbol is null);

            Debug.Assert(conversion.ConversionKind != ConversionKind.MethodGroup);
            if (conversion.ConversionKind == ConversionKind.AnonymousFunction)
            {
                var result = (BoundExpression)RewriteLambdaConversion((BoundLambda)conversion.Operand);

                if (_inExpressionLambda && conversion.ExplicitCastInCode)
                {
                    result = new BoundConversion(
                        syntax: conversion.Syntax,
                        operand: result,
                        conversion: conversion.Conversion,
                        isBaseConversion: false,
                        @checked: false,
                        explicitCastInCode: true,
                        conversionGroupOpt: conversion.ConversionGroupOpt,
                        inConversionGroupFlags: conversion.InConversionGroupFlags,
                        constantValueOpt: conversion.ConstantValueOpt,
                        type: conversion.Type);
                }

                return result;
            }

            return base.VisitConversion(conversion);
        }

        public override BoundNode VisitLocalFunctionStatement(BoundLocalFunctionStatement node)
        {
            ClosureKind closureKind;
            NamedTypeSymbol translatedLambdaContainer;
            SynthesizedClosureEnvironment containerAsFrame;
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
#nullable enable
        private DebugId GetLambdaId(SyntaxNode syntax, ClosureKind closureKind, int closureOrdinal, ImmutableArray<DebugId> structClosureIds, RuntimeRudeEdit? closureRudeEdit)
        {
            Debug.Assert(syntax != null);
            Debug.Assert(CompilationState.ModuleBuilderOpt != null);
            Debug.Assert(closureOrdinal >= LambdaDebugInfo.MinClosureOrdinal);

            SyntaxNode? lambdaOrLambdaBodySyntax;
            bool isLambdaBody;

            if (syntax is AnonymousFunctionExpressionSyntax anonymousFunction)
            {
                lambdaOrLambdaBodySyntax = anonymousFunction.Body;
                isLambdaBody = true;
            }
            else if (syntax is LocalFunctionStatementSyntax localFunction)
            {
                lambdaOrLambdaBodySyntax = (SyntaxNode?)localFunction.Body ?? localFunction.ExpressionBody?.Expression;

                if (lambdaOrLambdaBodySyntax is null)
                {
                    lambdaOrLambdaBodySyntax = localFunction;
                    isLambdaBody = false;
                }
                else
                {
                    isLambdaBody = true;
                }
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
            DebugId previousLambdaId = default;
            RuntimeRudeEdit? lambdaRudeEdit = null;

            if (closureRudeEdit == null &&
                slotAllocator?.TryGetPreviousLambda(lambdaOrLambdaBodySyntax, isLambdaBody, closureOrdinal, structClosureIds, out previousLambdaId, out lambdaRudeEdit) == true &&
                lambdaRudeEdit == null)
            {
                lambdaId = previousLambdaId;
            }
            else
            {
                lambdaId = new DebugId(_lambdaDebugInfoBuilder.Count, CompilationState.ModuleBuilderOpt.CurrentGenerationOrdinal);

                var rudeEdit = closureRudeEdit ?? lambdaRudeEdit;
                if (rudeEdit != null)
                {
                    _lambdaRuntimeRudeEditsBuilder.Add(new LambdaRuntimeRudeEditInfo(previousLambdaId, rudeEdit.Value));
                }
            }

            int syntaxOffset = _topLevelMethod.CalculateLocalSyntaxOffset(LambdaUtilities.GetDeclaratorPosition(lambdaOrLambdaBodySyntax), lambdaOrLambdaBodySyntax.SyntaxTree);
            _lambdaDebugInfoBuilder.Add(new EncLambdaInfo(new LambdaDebugInfo(syntaxOffset, lambdaId, closureOrdinal), structClosureIds));
            return lambdaId;
        }
#nullable disable
        private SynthesizedClosureMethod RewriteLambdaOrLocalFunction(
            IBoundLambdaOrFunction node,
            out ClosureKind closureKind,
            out NamedTypeSymbol translatedLambdaContainer,
            out SynthesizedClosureEnvironment containerAsFrame,
            out BoundNode lambdaScope,
            out DebugId topLevelMethodId,
            out DebugId lambdaId)
        {
            Analysis.NestedFunction function = Analysis.GetNestedFunctionInTree(_analysis.ScopeTree, node.Symbol);
            var synthesizedMethod = function.SynthesizedLoweredMethod;
            Debug.Assert(synthesizedMethod != null);

            closureKind = synthesizedMethod.ClosureKind;
            translatedLambdaContainer = synthesizedMethod.ContainingType;
            containerAsFrame = translatedLambdaContainer as SynthesizedClosureEnvironment;
            topLevelMethodId = _analysis.GetTopLevelMethodId();
            lambdaId = synthesizedMethod.LambdaId;

            if (function.ContainingEnvironmentOpt != null)
            {
                // Find the scope of the containing environment
                BoundNode tmpScope = null;
                Analysis.VisitScopeTree(_analysis.ScopeTree, scope =>
                {
                    if (scope.DeclaredEnvironment == function.ContainingEnvironmentOpt)
                    {
                        tmpScope = scope.BoundNode;
                    }
                });
                Debug.Assert(tmpScope != null);
                lambdaScope = tmpScope;
            }
            else
            {
                lambdaScope = null;
            }

            CompilationState.ModuleBuilderOpt.AddSynthesizedDefinition(translatedLambdaContainer, synthesizedMethod.GetCciAdapter());

            foreach (var parameter in node.Symbol.Parameters)
            {
                _parameterMap.Add(parameter, synthesizedMethod.Parameters[parameter.Ordinal]);
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
                _framePointers.TryGetValue(translatedLambdaContainer, out _innermostFramePointer);
            }

            _currentTypeParameters = containerAsFrame?.TypeParameters.Concat(synthesizedMethod.TypeParameters) ?? synthesizedMethod.TypeParameters;
            _currentLambdaBodyTypeMap = synthesizedMethod.TypeMap;

            if (node.Body is BoundBlock block)
            {
                var body = AddStatementsIfNeeded((BoundStatement)VisitBlock(block));
                CheckLocalsDefined(body);
                AddSynthesizedMethod(synthesizedMethod, body);
            }

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

        private void AddSynthesizedMethod(MethodSymbol method, BoundStatement body)
        {
            if (_synthesizedMethods == null)
            {
                _synthesizedMethods = ArrayBuilder<TypeCompilationState.MethodWithBody>.GetInstance();
            }

            _synthesizedMethods.Add(
                new TypeCompilationState.MethodWithBody(
                    method,
                    body,
                    CompilationState.CurrentImportChain));
        }

        private BoundNode RewriteLambdaConversion(BoundLambda node)
        {
            var wasInExpressionLambda = _inExpressionLambda;
            _inExpressionLambda = _inExpressionLambda || node.Type.IsExpressionTree();

            if (_inExpressionLambda)
            {
                var newType = VisitType(node.Type);
                var newBody = (BoundBlock)Visit(node.Body);
                node = node.Update(node.UnboundLambda, node.Symbol, newBody, node.Diagnostics, node.Binder, newType);
                var result0 = wasInExpressionLambda ? node : ExpressionLambdaRewriter.RewriteLambda(node, CompilationState, TypeMap, RecursionDepth, Diagnostics);
                _inExpressionLambda = wasInExpressionLambda;
                return result0;
            }

            ClosureKind closureKind;
            NamedTypeSymbol translatedLambdaContainer;
            SynthesizedClosureEnvironment containerAsFrame;
            BoundNode lambdaScope;
            DebugId topLevelMethodId;
            DebugId lambdaId;
            SynthesizedClosureMethod synthesizedMethod = RewriteLambdaOrLocalFunction(
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
            RemapLambdaOrLocalFunction(node.Syntax, node.Symbol, default(ImmutableArray<TypeWithAnnotations>), closureKind, ref referencedMethod, out receiver, out constructedFrame);

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
                wasTargetTyped: false,
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
                lambdaScope != Analysis.GetScopeParent(_analysis.ScopeTree, node.Body).BoundNode &&
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

                        var hasTypeParametersFromAnyMethod = cacheVariableType.ContainsMethodTypeParameter();

                        // If we want to cache a variable by moving its value into a field,
                        // the variable cannot use any type parameter from the method it is currently declared within.
                        if (!hasTypeParametersFromAnyMethod)
                        {
                            var cacheVariableName = GeneratedNames.MakeLambdaCacheFieldName(
                                // If we are generating the field into a display class created exclusively for the lambda the lambdaOrdinal itself is unique already,
                                // no need to include the top-level method ordinal in the field name.
                                (closureKind == ClosureKind.General) ? -1 : topLevelMethodId.Ordinal,
                                topLevelMethodId.Generation,
                                lambdaId.Ordinal,
                                lambdaId.Generation);

                            var cacheField = new SynthesizedLambdaCacheFieldSymbol(translatedLambdaContainer, cacheVariableType, cacheVariableName, _topLevelMethod, isReadOnly: false, isStatic: closureKind == ClosureKind.Singleton);
                            CompilationState.ModuleBuilderOpt.AddSynthesizedDefinition(translatedLambdaContainer, cacheField.GetCciAdapter());
                            cache = F.Field(receiver, cacheField.AsMember(constructedFrame)); //NOTE: the field was added to the unconstructed frame type.
                            result = F.Coalesce(cache, F.AssignmentExpression(cache, result));
                        }
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
                        result = F.Coalesce(cache, F.AssignmentExpression(cache, result));
                    }
                }
                catch (SyntheticBoundNodeFactory.MissingPredefinedMember ex)
                {
                    Diagnostics.Add(ex.Diagnostic);
                    return new BoundBadExpression(F.Syntax, LookupResultKind.Empty, ImmutableArray<Symbol>.Empty, ImmutableArray.Create<BoundExpression>(node), node.Type);
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
                    case SyntaxKind.ForEachVariableStatement:
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
            throw ExceptionUtilities.Unreachable();
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
