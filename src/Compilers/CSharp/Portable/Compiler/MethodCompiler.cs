// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeGen;
using Microsoft.CodeAnalysis.CSharp.Emit;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Emit;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal sealed class MethodCompiler : CSharpSymbolVisitor<TypeCompilationState, object>
    {
        private readonly CSharpCompilation _compilation;
        private readonly bool _emittingPdb;
        private readonly CancellationToken _cancellationToken;
        private readonly DiagnosticBag _diagnostics;
        private readonly bool _hasDeclarationErrors;
        private readonly PEModuleBuilder _moduleBeingBuiltOpt; // Null if compiling for diagnostics
        private readonly Predicate<Symbol> _filterOpt;         // If not null, limit analysis to specific symbols
        private readonly DebugDocumentProvider _debugDocumentProvider;

        //
        // MethodCompiler employs concurrency by following flattened fork/join pattern.
        //
        // For every item that we want to compile in parallel a new task is forked.
        // compileTaskQueue is used to track and observe all the tasks. 
        // Once compileTaskQueue is empty, we know that there are no more tasks (and no more can be created)
        // and that means we are done compiling. WaitForWorkers ensures this condition.
        //
        // Note that while tasks may fork more tasks (nested types, lambdas, whatever else that may introduce more types),
        // we do not want any child/parent relationship between spawned tasks and their creators. 
        // Creator has no real dependencies on the completion of its children and should finish and release any resources
        // as soon as it can regardless of the tasks it may have spawned.
        //
        // Stack is used so that the wait would observe the most recently added task and have 
        // more chances to do inlined execution.
        private ConcurrentStack<Task> _compilerTasks;

        // This field tracks whether any bound method body had hasErrors set or whether any constant field had a bad value.
        // We track it so that we can abort emission in the event that an error occurs without a corresponding diagnostic
        // (e.g. if this module depends on a bad type or constant from another module).
        // CONSIDER: instead of storing a flag, we could track the first member symbol with an error (to improve the diagnostic).

        // NOTE: once the flag is set to true, it should never go back to false!!!
        // Do not use this as a short-circuiting for stages that might produce diagnostics.
        // That would make diagnostics to depend on the random order in which methods are compiled.
        private bool _globalHasErrors;

        private void SetGlobalErrorIfTrue(bool arg)
        {
            //NOTE: this is not a volatile write
            //      for correctness we need only single threaded consistency.
            //      Within a single task - if we have got an error it may not be safe to continue with some lowerings.
            //      It is ok if other tasks will see the change after some delay or does not observe at all.
            //      Such races are unavoidable and will just result in performing some work that is safe to do
            //      but may no longer be needed.
            //      The final Join of compiling tasks cannot happen without interlocked operations and that 
            //      will ensure that any write of the flag is globally visible.
            if (arg)
            {
                _globalHasErrors = true;
            }
        }

        // Internal for testing only.
        internal MethodCompiler(CSharpCompilation compilation, PEModuleBuilder moduleBeingBuiltOpt, bool emittingPdb, bool hasDeclarationErrors,
            DiagnosticBag diagnostics, Predicate<Symbol> filterOpt, CancellationToken cancellationToken)
        {
            Debug.Assert(compilation != null);
            Debug.Assert(diagnostics != null);

            _compilation = compilation;
            _moduleBeingBuiltOpt = moduleBeingBuiltOpt;
            _emittingPdb = emittingPdb;
            _cancellationToken = cancellationToken;
            _diagnostics = diagnostics;
            _filterOpt = filterOpt;

            _hasDeclarationErrors = hasDeclarationErrors;
            SetGlobalErrorIfTrue(hasDeclarationErrors);

            if (emittingPdb)
            {
                _debugDocumentProvider = (path, basePath) => moduleBeingBuiltOpt.GetOrAddDebugDocument(path, basePath, CreateDebugDocumentForFile);
            }
        }

        public static void CompileMethodBodies(
            CSharpCompilation compilation,
            PEModuleBuilder moduleBeingBuiltOpt,
            bool generateDebugInfo,
            bool hasDeclarationErrors,
            DiagnosticBag diagnostics,
            Predicate<Symbol> filterOpt,
            CancellationToken cancellationToken)
        {
            Debug.Assert(compilation != null);
            Debug.Assert(diagnostics != null);

            if (compilation.PreviousSubmission != null)
            {
                // In case there is a previous submission, we should ensure 
                // it has already created anonymous type/delegates templates

                // NOTE: if there are any errors, we will pick up what was created anyway
                compilation.PreviousSubmission.EnsureAnonymousTypeTemplates(cancellationToken);

                // TODO: revise to use a loop instead of a recursion
            }

            MethodCompiler methodCompiler = new MethodCompiler(
                compilation,
                moduleBeingBuiltOpt,
                generateDebugInfo,
                hasDeclarationErrors,
                diagnostics,
                filterOpt,
                cancellationToken);

            if (compilation.Options.ConcurrentBuild)
            {
                methodCompiler._compilerTasks = new ConcurrentStack<Task>();
            }

            // directly traverse global namespace (no point to defer this to async)
            methodCompiler.CompileNamespace(compilation.SourceModule.GlobalNamespace);
            methodCompiler.WaitForWorkers();

            // compile additional and anonymous types if any
            if (moduleBeingBuiltOpt != null)
            {
                var additionalTypes = moduleBeingBuiltOpt.GetAdditionalTopLevelTypes();
                if (!additionalTypes.IsEmpty)
                {
                    methodCompiler.CompileSynthesizedMethods(additionalTypes, diagnostics);
                }

                // By this time we have processed all types reachable from module's global namespace
                compilation.AnonymousTypeManager.AssignTemplatesNamesAndCompile(methodCompiler, moduleBeingBuiltOpt, diagnostics);
                methodCompiler.WaitForWorkers();

                var privateImplClass = moduleBeingBuiltOpt.PrivateImplClass;
                if (privateImplClass != null)
                {
                    // all threads that were adding methods must be finished now, we can freeze the class:
                    privateImplClass.Freeze();

                    methodCompiler.CompileSynthesizedMethods(privateImplClass, diagnostics);
                }
            }

            // If we are trying to emit and there's an error without a corresponding diagnostic (e.g. because
            // we depend on an invalid type or constant from another module), then explicitly add a diagnostic.
            // This diagnostic is not very helpful to the user, but it will prevent us from emitting an invalid
            // module or crashing.
            if (moduleBeingBuiltOpt != null && (methodCompiler._globalHasErrors || moduleBeingBuiltOpt.SourceModule.HasBadAttributes) && !diagnostics.HasAnyErrors() && !hasDeclarationErrors)
            {
                diagnostics.Add(ErrorCode.ERR_ModuleEmitFailure, NoLocation.Singleton, ((Cci.INamedEntity)moduleBeingBuiltOpt).Name);
            }

            diagnostics.AddRange(compilation.AdditionalCodegenWarnings);

            // we can get unused field warnings only if compiling whole compilation.
            if (filterOpt == null)
            {
                WarnUnusedFields(compilation, diagnostics, cancellationToken);
            }

            MethodSymbol entryPoint = GetEntryPoint(compilation, moduleBeingBuiltOpt, hasDeclarationErrors, diagnostics, cancellationToken);
            if (moduleBeingBuiltOpt != null && entryPoint != null && compilation.Options.OutputKind.IsApplication())
            {
                moduleBeingBuiltOpt.SetPEEntryPoint(entryPoint, diagnostics);
            }
        }

        private static MethodSymbol GetEntryPoint(CSharpCompilation compilation, PEModuleBuilder moduleBeingBuilt, bool hasDeclarationErrors, DiagnosticBag diagnostics, CancellationToken cancellationToken)
        {
            var entryPointAndDiagnostics = compilation.GetEntryPointAndDiagnostics(cancellationToken);
            if (entryPointAndDiagnostics == null)
            {
                return null;
            }

            Debug.Assert(!entryPointAndDiagnostics.Diagnostics.IsDefault);
            diagnostics.AddRange(entryPointAndDiagnostics.Diagnostics);

            var entryPoint = entryPointAndDiagnostics.MethodSymbol;
            var synthesizedEntryPoint = entryPoint as SynthesizedEntryPointSymbol;
            if (((object)synthesizedEntryPoint != null) &&
                (moduleBeingBuilt != null) &&
                !hasDeclarationErrors &&
                !diagnostics.HasAnyErrors())
            {
                var body = synthesizedEntryPoint.CreateBody();
                const int methodOrdinal = -1;
                var emittedBody = GenerateMethodBody(
                    moduleBeingBuilt,
                    synthesizedEntryPoint,
                    methodOrdinal,
                    body,
                    ImmutableArray<LambdaDebugInfo>.Empty,
                    ImmutableArray<ClosureDebugInfo>.Empty,
                    stateMachineTypeOpt: null,
                    variableSlotAllocatorOpt: null,
                    diagnostics: diagnostics,
                    debugDocumentProvider: null,
                    importChainOpt: null,
                    emittingPdb: false);
                moduleBeingBuilt.SetMethodBody(synthesizedEntryPoint, emittedBody);
            }

            Debug.Assert((object)entryPoint != null || entryPointAndDiagnostics.Diagnostics.HasAnyErrors() || !compilation.Options.Errors.IsDefaultOrEmpty);
            return entryPoint;
        }

        private void WaitForWorkers()
        {
            var tasks = _compilerTasks;
            if (tasks == null)
            {
                return;
            }

            Task curTask;
            while (tasks.TryPop(out curTask))
            {
                curTask.GetAwaiter().GetResult();
            }
        }

        private static void WarnUnusedFields(CSharpCompilation compilation, DiagnosticBag diagnostics, CancellationToken cancellationToken)
        {
            SourceAssemblySymbol assembly = (SourceAssemblySymbol)compilation.Assembly;
            diagnostics.AddRange(assembly.GetUnusedFieldWarnings(cancellationToken));
        }

        public override object VisitNamespace(NamespaceSymbol symbol, TypeCompilationState arg)
        {
            if (!PassesFilter(_filterOpt, symbol))
            {
                return null;
            }

            arg = null; // do not use compilation state of outer type.
            _cancellationToken.ThrowIfCancellationRequested();

            if (_compilation.Options.ConcurrentBuild)
            {
                Task worker = CompileNamespaceAsTask(symbol);
                _compilerTasks.Push(worker);
            }
            else
            {
                CompileNamespace(symbol);
            }

            return null;
        }

        private Task CompileNamespaceAsTask(NamespaceSymbol symbol)
        {
            return Task.Run(UICultureUtilities.WithCurrentUICulture(() =>
                {
                    try
                    {
                        CompileNamespace(symbol);
                    }
                    catch (Exception e) when (FatalError.ReportUnlessCanceled(e))
                    {
                        throw ExceptionUtilities.Unreachable;
                    }
                }), _cancellationToken);
        }

        private void CompileNamespace(NamespaceSymbol symbol)
        {
            foreach (var s in symbol.GetMembersUnordered())
            {
                s.Accept(this, null);
            }
        }

        public override object VisitNamedType(NamedTypeSymbol symbol, TypeCompilationState arg)
        {
            if (!PassesFilter(_filterOpt, symbol))
            {
                return null;
            }

            arg = null; // do not use compilation state of outer type.
            _cancellationToken.ThrowIfCancellationRequested();

            if (_compilation.Options.ConcurrentBuild)
            {
                Task worker = CompileNamedTypeAsTask(symbol);
                _compilerTasks.Push(worker);
            }
            else
            {
                CompileNamedType(symbol);
            }

            return null;
        }

        private Task CompileNamedTypeAsTask(NamedTypeSymbol symbol)
        {
            return Task.Run(UICultureUtilities.WithCurrentUICulture(() =>
                {
                    try
                    {
                        CompileNamedType(symbol);
                    }
                    catch (Exception e) when (FatalError.ReportUnlessCanceled(e))
                    {
                        throw ExceptionUtilities.Unreachable;
                    }
                }), _cancellationToken);
        }

        private void CompileNamedType(NamedTypeSymbol containingType)
        {
            var compilationState = new TypeCompilationState(containingType, _compilation, _moduleBeingBuiltOpt);

            _cancellationToken.ThrowIfCancellationRequested();

            // Find the constructor of a script class.
            SynthesizedInstanceConstructor scriptCtor = null;
            SynthesizedInteractiveInitializerMethod scriptInitializer = null;
            SynthesizedEntryPointSymbol scriptEntryPoint = null;
            int scriptCtorOrdinal = -1;
            if (containingType.IsScriptClass)
            {
                // The field initializers of a script class could be arbitrary statements,
                // including blocks.  Field initializers containing blocks need to
                // use a MethodBodySemanticModel to build up the appropriate tree of binders, and
                // MethodBodySemanticModel requires an "owning" method.  That's why we're digging out
                // the constructor - it will own the field initializers.
                scriptCtor = containingType.GetScriptConstructor();
                scriptInitializer = containingType.GetScriptInitializer();
                scriptEntryPoint = containingType.GetScriptEntryPoint();
                Debug.Assert((object)scriptCtor != null);
                Debug.Assert((object)scriptInitializer != null);
            }

            var synthesizedSubmissionFields = containingType.IsSubmissionClass ? new SynthesizedSubmissionFields(_compilation, containingType) : null;
            var processedStaticInitializers = new Binder.ProcessedFieldInitializers();
            var processedInstanceInitializers = new Binder.ProcessedFieldInitializers();

            var sourceTypeSymbol = containingType as SourceMemberContainerTypeSymbol;

            if ((object)sourceTypeSymbol != null)
            {
                _cancellationToken.ThrowIfCancellationRequested();
                Binder.BindFieldInitializers(_compilation, scriptInitializer, sourceTypeSymbol.StaticInitializers, _diagnostics, ref processedStaticInitializers);

                _cancellationToken.ThrowIfCancellationRequested();
                Binder.BindFieldInitializers(_compilation, scriptInitializer, sourceTypeSymbol.InstanceInitializers, _diagnostics, ref processedInstanceInitializers);

                if (compilationState.Emitting)
                {
                    CompileSynthesizedExplicitImplementations(sourceTypeSymbol, compilationState);
                }
            }

            // Indicates if a static constructor is in the member,
            // so we can decide to synthesize a static constructor.
            bool hasStaticConstructor = false;

            var members = containingType.GetMembers();
            for (int memberOrdinal = 0; memberOrdinal < members.Length; memberOrdinal++)
            {
                var member = members[memberOrdinal];

                //When a filter is supplied, limit the compilation of members passing the filter.
                if (!PassesFilter(_filterOpt, member))
                {
                    continue;
                }

                switch (member.Kind)
                {
                    case SymbolKind.NamedType:
                        member.Accept(this, compilationState);
                        break;

                    case SymbolKind.Method:
                        {
                            MethodSymbol method = (MethodSymbol)member;
                            if (method.IsScriptConstructor)
                            {
                                Debug.Assert(scriptCtorOrdinal == -1);
                                Debug.Assert((object)scriptCtor == method);
                                scriptCtorOrdinal = memberOrdinal;
                                continue;
                            }

                            if ((object)method == scriptEntryPoint)
                            {
                                continue;
                            }

                            if (IsFieldLikeEventAccessor(method))
                            {
                                continue;
                            }

                            if (method.IsPartialDefinition())
                            {
                                method = method.PartialImplementationPart;
                                if ((object)method == null)
                                {
                                    continue;
                                }
                            }

                            Binder.ProcessedFieldInitializers processedInitializers =
                                (method.MethodKind == MethodKind.Constructor || method.IsScriptInitializer) ? processedInstanceInitializers :
                                method.MethodKind == MethodKind.StaticConstructor ? processedStaticInitializers :
                                default(Binder.ProcessedFieldInitializers);

                            CompileMethod(method, memberOrdinal, ref processedInitializers, synthesizedSubmissionFields, compilationState);

                            // Set a flag to indicate that a static constructor is created.
                            if (method.MethodKind == MethodKind.StaticConstructor)
                            {
                                hasStaticConstructor = true;
                            }

                            break;
                        }

                    case SymbolKind.Property:
                        {
                            SourcePropertySymbol sourceProperty = member as SourcePropertySymbol;
                            if ((object)sourceProperty != null && sourceProperty.IsSealed && compilationState.Emitting)
                            {
                                CompileSynthesizedSealedAccessors(sourceProperty, compilationState);
                            }
                            break;
                        }

                    case SymbolKind.Event:
                        {
                            SourceEventSymbol eventSymbol = member as SourceEventSymbol;
                            if ((object)eventSymbol != null && eventSymbol.HasAssociatedField && !eventSymbol.IsAbstract && compilationState.Emitting)
                            {
                                CompileFieldLikeEventAccessor(eventSymbol, isAddMethod: true);
                                CompileFieldLikeEventAccessor(eventSymbol, isAddMethod: false);
                            }
                            break;
                        }

                    case SymbolKind.Field:
                        {
                            SourceMemberFieldSymbol fieldSymbol = member as SourceMemberFieldSymbol;
                            if ((object)fieldSymbol != null)
                            {
                                if (fieldSymbol.IsConst)
                                {
                                    // We check specifically for constant fields with bad values because they never result
                                    // in bound nodes being inserted into method bodies (in which case, they would be covered
                                    // by the method-level check).
                                    ConstantValue constantValue = fieldSymbol.GetConstantValue(ConstantFieldsInProgress.Empty, earlyDecodingWellKnownAttributes: false);
                                    SetGlobalErrorIfTrue(constantValue == null || constantValue.IsBad);
                                }

                                if (fieldSymbol.IsFixed && compilationState.Emitting)
                                {
                                    // force the generation of implementation types for fixed-size buffers
                                    TypeSymbol discarded = fieldSymbol.FixedImplementationType(compilationState.ModuleBuilderOpt);
                                }
                            }
                            break;
                        }
                }
            }

            Debug.Assert(containingType.IsScriptClass == (scriptCtorOrdinal >= 0));

            // process additional anonymous type members
            if (AnonymousTypeManager.IsAnonymousTypeTemplate(containingType))
            {
                var processedInitializers = default(Binder.ProcessedFieldInitializers);
                foreach (var method in AnonymousTypeManager.GetAnonymousTypeHiddenMethods(containingType))
                {
                    CompileMethod(method, -1, ref processedInitializers, synthesizedSubmissionFields, compilationState);
                }
            }

            // In the case there are field initializers but we haven't created an implicit static constructor (.cctor) for it,
            // (since we may not add .cctor implicitly created for decimals into the symbol table)
            // it is necessary for the compiler to generate the static constructor here if we are emitting.
            if (_moduleBeingBuiltOpt != null && !hasStaticConstructor && !processedStaticInitializers.BoundInitializers.IsDefaultOrEmpty)
            {
                Debug.Assert(processedStaticInitializers.BoundInitializers.All((init) =>
                    (init.Kind == BoundKind.FieldInitializer) && !((BoundFieldInitializer)init).Field.IsMetadataConstant));

                MethodSymbol method = new SynthesizedStaticConstructor(sourceTypeSymbol);
                if (PassesFilter(_filterOpt, method))
                {
                    CompileMethod(method, -1, ref processedStaticInitializers, synthesizedSubmissionFields, compilationState);

                    // If this method has been successfully built, we emit it.
                    if (_moduleBeingBuiltOpt.GetMethodBody(method) != null)
                    {
                        _moduleBeingBuiltOpt.AddSynthesizedDefinition(sourceTypeSymbol, method);
                    }
                }
            }

            // compile submission constructor last so that synthesized submission fields are collected from all script methods:
            if (scriptCtor != null && compilationState.Emitting)
            {
                Debug.Assert(scriptCtorOrdinal >= 0);
                var processedInitializers = new Binder.ProcessedFieldInitializers() { BoundInitializers = ImmutableArray<BoundInitializer>.Empty };
                CompileMethod(scriptCtor, scriptCtorOrdinal, ref processedInitializers, synthesizedSubmissionFields, compilationState);
                if (synthesizedSubmissionFields != null)
                {
                    synthesizedSubmissionFields.AddToType(containingType, compilationState.ModuleBuilderOpt);
                }
            }

            // Emit synthesized methods produced during lowering if any
            if (_moduleBeingBuiltOpt != null)
            {
                CompileSynthesizedMethods(compilationState);
            }

            compilationState.Free();
        }

        private void CompileSynthesizedMethods(PrivateImplementationDetails privateImplClass, DiagnosticBag diagnostics)
        {
            Debug.Assert(_moduleBeingBuiltOpt != null);

            var compilationState = new TypeCompilationState(null, _compilation, _moduleBeingBuiltOpt);
            foreach (MethodSymbol method in privateImplClass.GetMethods(new EmitContext(_moduleBeingBuiltOpt, null, diagnostics)))
            {
                Debug.Assert(method.SynthesizesLoweredBoundBody);
                method.GenerateMethodBody(compilationState, diagnostics);
            }

            CompileSynthesizedMethods(compilationState);
            compilationState.Free();
        }

        private void CompileSynthesizedMethods(ImmutableArray<NamedTypeSymbol> additionalTypes, DiagnosticBag diagnostics)
        {
            foreach (var additionalType in additionalTypes)
            {
                var compilationState = new TypeCompilationState(additionalType, _compilation, _moduleBeingBuiltOpt);
                foreach (var method in additionalType.GetMethodsToEmit())
                {
                    method.GenerateMethodBody(compilationState, diagnostics);
                }

                if (!diagnostics.HasAnyErrors())
                {
                    CompileSynthesizedMethods(compilationState);
                }

                compilationState.Free();
            }
        }

        private void CompileSynthesizedMethods(TypeCompilationState compilationState)
        {
            Debug.Assert(_moduleBeingBuiltOpt != null);
            Debug.Assert(compilationState.ModuleBuilderOpt == _moduleBeingBuiltOpt);

            var synthesizedMethods = compilationState.SynthesizedMethods;
            if (synthesizedMethods == null)
            {
                return;
            }

            var oldImportChain = compilationState.CurrentImportChain;
            try
            {
                foreach (var methodWithBody in synthesizedMethods)
                {
                    var importChain = methodWithBody.ImportChainOpt;
                    compilationState.CurrentImportChain = importChain;

                    var method = methodWithBody.Method;
                var lambda = method as SynthesizedLambdaMethod;
                var variableSlotAllocatorOpt = ((object)lambda != null) ?
                    _moduleBeingBuiltOpt.TryCreateVariableSlotAllocator(lambda, lambda.TopLevelMethod) :
                    _moduleBeingBuiltOpt.TryCreateVariableSlotAllocator(method, method);

                // We make sure that an asynchronous mutation to the diagnostic bag does not 
                // confuse the method body generator by making a fresh bag and then loading
                // any diagnostics emitted into it back into the main diagnostic bag.
                var diagnosticsThisMethod = DiagnosticBag.GetInstance();

                // Synthesized methods have no ordinal stored in custom debug information (only user-defined methods have ordinals).
                // In case of async lambdas, which synthesize a state machine type during the following rewrite, the containing method has already been uniquely named, 
                // so there is no need to produce a unique method ordinal for the corresponding state machine type, whose name includes the (unique) containing method name.
                const int methodOrdinal = -1;
                MethodBody emittedBody = null;

                try
                {
                    // Local functions can be iterators as well as be async (lambdas can only be async), so we need to lower both iterators and async
                    IteratorStateMachine iteratorStateMachine;
                    BoundStatement loweredBody = IteratorRewriter.Rewrite(methodWithBody.Body, method, methodOrdinal, variableSlotAllocatorOpt, compilationState, diagnosticsThisMethod, out iteratorStateMachine);
                    StateMachineTypeSymbol stateMachine = iteratorStateMachine;

                    if (!loweredBody.HasErrors)
                    {
                        AsyncStateMachine asyncStateMachine;
                        loweredBody = AsyncRewriter.Rewrite(loweredBody, method, methodOrdinal, variableSlotAllocatorOpt, compilationState, diagnosticsThisMethod, out asyncStateMachine);

                        Debug.Assert(iteratorStateMachine == null || asyncStateMachine == null);
                        stateMachine = stateMachine ?? asyncStateMachine;
                    }

                    if (!diagnosticsThisMethod.HasAnyErrors() && !_globalHasErrors)
                    {
                        emittedBody = GenerateMethodBody(
                            _moduleBeingBuiltOpt,
                            method,
                            methodOrdinal,
                            loweredBody,
                            ImmutableArray<LambdaDebugInfo>.Empty,
                            ImmutableArray<ClosureDebugInfo>.Empty,
                            stateMachine,
                            variableSlotAllocatorOpt,
                            diagnosticsThisMethod,
                            _debugDocumentProvider,
                                method.GenerateDebugInfo ? importChain : null,
                            emittingPdb: _emittingPdb);
                    }
                }
                catch (BoundTreeVisitor.CancelledByStackGuardException ex)
                {
                    ex.AddAnError(_diagnostics);
                }

                _diagnostics.AddRange(diagnosticsThisMethod);
                diagnosticsThisMethod.Free();

                // error while generating IL
                if (emittedBody == null)
                {
                    break;
                }

                _moduleBeingBuiltOpt.SetMethodBody(method, emittedBody);
            }
        }
            finally
            {
                compilationState.CurrentImportChain = oldImportChain;
            }
        }

        private static bool IsFieldLikeEventAccessor(MethodSymbol method)
        {
            Symbol associatedPropertyOrEvent = method.AssociatedSymbol;
            return (object)associatedPropertyOrEvent != null &&
                associatedPropertyOrEvent.Kind == SymbolKind.Event &&
                ((EventSymbol)associatedPropertyOrEvent).HasAssociatedField;
        }

        /// <summary>
        /// In some circumstances (e.g. implicit implementation of an interface method by a non-virtual method in a 
        /// base type from another assembly) it is necessary for the compiler to generate explicit implementations for
        /// some interface methods.  They don't go in the symbol table, but if we are emitting, then we should
        /// generate code for them.
        /// </summary>
        private void CompileSynthesizedExplicitImplementations(SourceMemberContainerTypeSymbol sourceTypeSymbol, TypeCompilationState compilationState)
        {
            // we are not generating any observable diagnostics here so it is ok to short-circuit on global errors.
            if (!_globalHasErrors)
            {
                foreach (var synthesizedExplicitImpl in sourceTypeSymbol.GetSynthesizedExplicitImplementations(_cancellationToken))
                {
                    Debug.Assert(synthesizedExplicitImpl.SynthesizesLoweredBoundBody);
                    var discardedDiagnostics = DiagnosticBag.GetInstance();
                    synthesizedExplicitImpl.GenerateMethodBody(compilationState, discardedDiagnostics);
                    Debug.Assert(!discardedDiagnostics.HasAnyErrors());
                    discardedDiagnostics.Free();
                    _moduleBeingBuiltOpt.AddSynthesizedDefinition(sourceTypeSymbol, synthesizedExplicitImpl);
                }
            }
        }

        private void CompileSynthesizedSealedAccessors(SourcePropertySymbol sourceProperty, TypeCompilationState compilationState)
        {
            SynthesizedSealedPropertyAccessor synthesizedAccessor = sourceProperty.SynthesizedSealedAccessorOpt;

            // we are not generating any observable diagnostics here so it is ok to short-circuit on global errors.
            if ((object)synthesizedAccessor != null && !_globalHasErrors)
            {
                Debug.Assert(synthesizedAccessor.SynthesizesLoweredBoundBody);
                var discardedDiagnostics = DiagnosticBag.GetInstance();
                synthesizedAccessor.GenerateMethodBody(compilationState, discardedDiagnostics);
                Debug.Assert(!discardedDiagnostics.HasAnyErrors());
                discardedDiagnostics.Free();

                _moduleBeingBuiltOpt.AddSynthesizedDefinition(sourceProperty.ContainingType, synthesizedAccessor);
            }
        }

        private void CompileFieldLikeEventAccessor(SourceEventSymbol eventSymbol, bool isAddMethod)
        {
            MethodSymbol accessor = isAddMethod ? eventSymbol.AddMethod : eventSymbol.RemoveMethod;

            var diagnosticsThisMethod = DiagnosticBag.GetInstance();
            try
            {
                BoundBlock boundBody = MethodBodySynthesizer.ConstructFieldLikeEventAccessorBody(eventSymbol, isAddMethod, _compilation, diagnosticsThisMethod);
                var hasErrors = diagnosticsThisMethod.HasAnyErrors();
                SetGlobalErrorIfTrue(hasErrors);

                // we cannot rely on GlobalHasErrors since that can be changed concurrently by other methods compiling
                // we however do not want to continue with generating method body if we have errors in this particular method - generating may crash
                // or if had declaration errors - we will fail anyways, but if some types are bad enough, generating may produce duplicate errors about that.
                if (!hasErrors && !_hasDeclarationErrors)
                {
                    const int accessorOrdinal = -1;

                    MethodBody emittedBody = GenerateMethodBody(
                        _moduleBeingBuiltOpt,
                        accessor,
                        accessorOrdinal,
                        boundBody,
                        ImmutableArray<LambdaDebugInfo>.Empty,
                        ImmutableArray<ClosureDebugInfo>.Empty,
                        stateMachineTypeOpt: null,
                        variableSlotAllocatorOpt: null,
                        diagnostics: diagnosticsThisMethod,
                        debugDocumentProvider: _debugDocumentProvider,
                        importChainOpt: null,
                        emittingPdb: false);

                    _moduleBeingBuiltOpt.SetMethodBody(accessor, emittedBody);
                    // Definition is already in the symbol table, so don't call moduleBeingBuilt.AddCompilerGeneratedDefinition
                }
            }
            finally
            {
                _diagnostics.AddRange(diagnosticsThisMethod);
                diagnosticsThisMethod.Free();
            }
        }

        public override object VisitMethod(MethodSymbol symbol, TypeCompilationState arg)
        {
            throw ExceptionUtilities.Unreachable;
        }

        public override object VisitProperty(PropertySymbol symbol, TypeCompilationState argument)
        {
            throw ExceptionUtilities.Unreachable;
        }

        public override object VisitEvent(EventSymbol symbol, TypeCompilationState argument)
        {
            throw ExceptionUtilities.Unreachable;
        }

        public override object VisitField(FieldSymbol symbol, TypeCompilationState argument)
        {
            throw ExceptionUtilities.Unreachable;
        }

        private void CompileMethod(
            MethodSymbol methodSymbol,
            int methodOrdinal,
            ref Binder.ProcessedFieldInitializers processedInitializers,
            SynthesizedSubmissionFields previousSubmissionFields,
            TypeCompilationState compilationState)
        {
            _cancellationToken.ThrowIfCancellationRequested();
            SourceMethodSymbol sourceMethod = methodSymbol as SourceMethodSymbol;

            if (methodSymbol.IsAbstract)
            {
                if ((object)sourceMethod != null)
                {
                    bool diagsWritten;
                    sourceMethod.SetDiagnostics(ImmutableArray<Diagnostic>.Empty, out diagsWritten);
                    if (diagsWritten && !methodSymbol.IsImplicitlyDeclared && _compilation.EventQueue != null)
                    {
                        _compilation.SymbolDeclaredEvent(methodSymbol);
                    }
                }

                return;
            }

            // get cached diagnostics if not building and we have 'em
            if (_moduleBeingBuiltOpt == null && (object)sourceMethod != null)
            {
                var cachedDiagnostics = sourceMethod.Diagnostics;

                if (!cachedDiagnostics.IsDefault)
                {
                    _diagnostics.AddRange(cachedDiagnostics);
                    return;
                }
            }

            ImportChain oldImportChain = compilationState.CurrentImportChain;

            // In order to avoid generating code for methods with errors, we create a diagnostic bag just for this method.
            DiagnosticBag diagsForCurrentMethod = DiagnosticBag.GetInstance();

            try
            {
                // if synthesized method returns its body in lowered form
                if (methodSymbol.SynthesizesLoweredBoundBody)
                {
                    if (_moduleBeingBuiltOpt != null)
                    {
                        methodSymbol.GenerateMethodBody(compilationState, diagsForCurrentMethod);
                        _diagnostics.AddRange(diagsForCurrentMethod);
                    }

                    return;
                }

                // no need to emit the default ctor, we are not emitting those
                if (methodSymbol.IsDefaultValueTypeConstructor())
                {
                    return;
                }

                bool includeInitializersInBody = false;
                BoundBlock body;
                bool originalBodyNested = false;

                // initializers that have been analyzed but not yet lowered.
                BoundStatementList analyzedInitializers = null;

                ImportChain importChain = null;
                var hasTrailingExpression = false;

                if (methodSymbol.IsScriptConstructor)
                {
                    body = new BoundBlock(methodSymbol.GetNonNullSyntaxNode(), ImmutableArray<LocalSymbol>.Empty, ImmutableArray<LocalFunctionSymbol>.Empty, ImmutableArray<BoundStatement>.Empty) { WasCompilerGenerated = true };
                }
                else if (methodSymbol.IsScriptInitializer)
                {
                    // rewrite top-level statements and script variable declarations to a list of statements and assignments, respectively:
                    var initializerStatements = InitializerRewriter.RewriteScriptInitializer(processedInitializers.BoundInitializers, (SynthesizedInteractiveInitializerMethod)methodSymbol, out hasTrailingExpression);

                    // the lowered script initializers should not be treated as initializers anymore but as a method body:
                    body = BoundBlock.SynthesizedNoLocals(initializerStatements.Syntax, initializerStatements.Statements);

                    var unusedDiagnostics = DiagnosticBag.GetInstance();
                    DataFlowPass.Analyze(_compilation, methodSymbol, initializerStatements, unusedDiagnostics, requireOutParamsAssigned: false);
                    DiagnosticsPass.IssueDiagnostics(_compilation, initializerStatements, unusedDiagnostics, methodSymbol);
                    unusedDiagnostics.Free();
                }
                else
                {
                    // Do not emit initializers if we are invoking another constructor of this class.
                    includeInitializersInBody = !processedInitializers.BoundInitializers.IsDefaultOrEmpty &&
                                                !HasThisConstructorInitializer(methodSymbol);

                    body = BindMethodBody(methodSymbol, compilationState, diagsForCurrentMethod, out importChain, out originalBodyNested);

                    // lower initializers just once. the lowered tree will be reused when emitting all constructors 
                    // with field initializers. Once lowered, these initializers will be stashed in processedInitializers.LoweredInitializers
                    // (see later in this method). Don't bother lowering _now_ if this particular ctor won't have the initializers 
                    // appended to its body.
                    if (includeInitializersInBody && processedInitializers.LoweredInitializers == null)
                    {
                        analyzedInitializers = InitializerRewriter.RewriteConstructor(processedInitializers.BoundInitializers, methodSymbol);
                        processedInitializers.HasErrors = processedInitializers.HasErrors || analyzedInitializers.HasAnyErrors;

                        if (body != null && methodSymbol.ContainingType.IsStructType() && !methodSymbol.IsImplicitConstructor)
                        {
                            // In order to get correct diagnostics, we need to analyze initializers and the body together.
                            body = body.Update(body.Locals, body.LocalFunctions, body.Statements.Insert(0, analyzedInitializers));
                            includeInitializersInBody = false;
                            analyzedInitializers = null;
                        }
                        else
                        {
                            // These analyses check for diagnostics in lambdas.
                            // Control flow analysis and implicit return insertion are unnecessary.
                            DataFlowPass.Analyze(_compilation, methodSymbol, analyzedInitializers, diagsForCurrentMethod, requireOutParamsAssigned: false);
                            DiagnosticsPass.IssueDiagnostics(_compilation, analyzedInitializers, diagsForCurrentMethod, methodSymbol);
                        }
                    }
                }

#if DEBUG
                // If the method is a synthesized static or instance constructor, then debugImports will be null and we will use the value
                // from the first field initializer.
                if ((methodSymbol.MethodKind == MethodKind.Constructor || methodSymbol.MethodKind == MethodKind.StaticConstructor) &&
                    methodSymbol.IsImplicitlyDeclared && body == null)
                {
                    // There was no body to bind, so we didn't get anything from BindMethodBody.
                    Debug.Assert(importChain == null);
                }

                // Either there were no field initializers or we grabbed debug imports from the first one.
                Debug.Assert(processedInitializers.BoundInitializers.IsDefaultOrEmpty || processedInitializers.FirstImportChain != null);
#endif

                importChain = importChain ?? processedInitializers.FirstImportChain;

                // Associate these debug imports with all methods generated from this one.
                compilationState.CurrentImportChain = importChain;

                if (body != null)
                {
                    DiagnosticsPass.IssueDiagnostics(_compilation, body, diagsForCurrentMethod, methodSymbol);
                }

                BoundBlock flowAnalyzedBody = null;
                if (body != null)
                {
                    flowAnalyzedBody = FlowAnalysisPass.Rewrite(methodSymbol, body, diagsForCurrentMethod, hasTrailingExpression: hasTrailingExpression, originalBodyNested: originalBodyNested);
                }

                bool hasErrors = _hasDeclarationErrors || diagsForCurrentMethod.HasAnyErrors() || processedInitializers.HasErrors;

                // Record whether or not the bound tree for the lowered method body (including any initializers) contained any
                // errors (note: errors, not diagnostics).
                SetGlobalErrorIfTrue(hasErrors);

                bool diagsWritten = false;
                var actualDiagnostics = diagsForCurrentMethod.ToReadOnly();
                if (sourceMethod != null)
                {
                    actualDiagnostics = sourceMethod.SetDiagnostics(actualDiagnostics, out diagsWritten);
                }

                if (diagsWritten && !methodSymbol.IsImplicitlyDeclared && _compilation.EventQueue != null)
                {
                    var lazySemanticModel = body == null ? null : new Lazy<SemanticModel>(() =>
                    {
                        var syntax = body.Syntax;
                        var semanticModel = (CSharpSemanticModel)_compilation.GetSemanticModel(syntax.SyntaxTree);
                        var memberModel = semanticModel.GetMemberModel(syntax);
                        if (memberModel != null)
                        {
                            memberModel.UnguardedAddBoundTreeForStandaloneSyntax(syntax, body);
                        }
                        return semanticModel;
                    });

                    _compilation.EventQueue.Enqueue(new SymbolDeclaredCompilationEvent(_compilation, methodSymbol, lazySemanticModel));
                }

                // Don't lower if we're not emitting or if there were errors. 
                // Methods that had binding errors are considered too broken to be lowered reliably.
                if (_moduleBeingBuiltOpt == null || hasErrors)
                {
                    _diagnostics.AddRange(actualDiagnostics);
                    return;
                }

                // ############################
                // LOWERING AND EMIT
                // Any errors generated below here are considered Emit diagnostics 
                // and will not be reported to callers Compilation.GetDiagnostics()

                bool hasBody = flowAnalyzedBody != null;
                VariableSlotAllocator lazyVariableSlotAllocator = null;
                StateMachineTypeSymbol stateMachineTypeOpt = null;
                var lambdaDebugInfoBuilder = ArrayBuilder<LambdaDebugInfo>.GetInstance();
                var closureDebugInfoBuilder = ArrayBuilder<ClosureDebugInfo>.GetInstance();
                BoundStatement loweredBodyOpt = null;

                try
                {
                    if (hasBody)
                    {
                        loweredBodyOpt = LowerBodyOrInitializer(
                            methodSymbol,
                            methodOrdinal,
                            flowAnalyzedBody,
                            previousSubmissionFields,
                            compilationState,
                            diagsForCurrentMethod,
                            ref lazyVariableSlotAllocator,
                            lambdaDebugInfoBuilder,
                            closureDebugInfoBuilder,
                            out stateMachineTypeOpt);

                        Debug.Assert(loweredBodyOpt != null);
                    }
                    else
                    {
                        loweredBodyOpt = null;
                    }

                    hasErrors = hasErrors || (hasBody && loweredBodyOpt.HasErrors) || diagsForCurrentMethod.HasAnyErrors();
                    SetGlobalErrorIfTrue(hasErrors);

                    // don't emit if the resulting method would contain initializers with errors
                    if (!hasErrors && (hasBody || includeInitializersInBody))
                    {
                        // Fields must be initialized before constructor initializer (which is the first statement of the analyzed body, if specified),
                        // so that the initialization occurs before any method overridden by the declaring class can be invoked from the base constructor
                        // and access the fields.

                        ImmutableArray<BoundStatement> boundStatements;

                        if (methodSymbol.IsScriptConstructor)
                        {
                            boundStatements = MethodBodySynthesizer.ConstructScriptConstructorBody(loweredBodyOpt, methodSymbol, previousSubmissionFields, _compilation);
                        }
                        else
                        {
                            boundStatements = ImmutableArray<BoundStatement>.Empty;

                            if (analyzedInitializers != null)
                            {
                                StateMachineTypeSymbol initializerStateMachineTypeOpt;

                                processedInitializers.LoweredInitializers = (BoundStatementList)LowerBodyOrInitializer(
                                    methodSymbol,
                                    methodOrdinal,
                                    analyzedInitializers,
                                    previousSubmissionFields,
                                    compilationState,
                                    diagsForCurrentMethod,
                                    ref lazyVariableSlotAllocator,
                                    lambdaDebugInfoBuilder,
                                    closureDebugInfoBuilder,
                                    out initializerStateMachineTypeOpt);

                                // initializers can't produce state machines
                                Debug.Assert((object)initializerStateMachineTypeOpt == null);

                                Debug.Assert(processedInitializers.LoweredInitializers.Kind == BoundKind.StatementList);
                                Debug.Assert(!hasErrors);
                                hasErrors = processedInitializers.LoweredInitializers.HasAnyErrors || diagsForCurrentMethod.HasAnyErrors();
                                SetGlobalErrorIfTrue(hasErrors);

                                if (hasErrors)
                                {
                                    _diagnostics.AddRange(diagsForCurrentMethod);
                                    return;
                                }
                            }

                            // initializers for global code have already been included in the body
                            if (includeInitializersInBody)
                            {
                                boundStatements = boundStatements.Concat(processedInitializers.LoweredInitializers.Statements);
                            }

                            if (hasBody)
                            {
                                boundStatements = boundStatements.Concat(ImmutableArray.Create(loweredBodyOpt));
                            }
                        }

                        // generated struct constructors should ensure that all fields are assigned (even those that do not have initializers)
                        var container = methodSymbol.ContainingType as SourceMemberContainerTypeSymbol;
                        if (container != null &&
                            container.IsStructType() &&
                            methodSymbol.IsImplicitInstanceConstructor)
                        {
                            StateMachineTypeSymbol ctorStateMachineTypeOpt;

                            var chain = ChainImplicitStructConstructor(methodSymbol, container);
                            chain = LowerBodyOrInitializer(
                                methodSymbol,
                                methodOrdinal,
                                chain,
                                previousSubmissionFields,
                                compilationState,
                                diagsForCurrentMethod,
                                ref lazyVariableSlotAllocator,
                                lambdaDebugInfoBuilder,
                                closureDebugInfoBuilder,
                                out ctorStateMachineTypeOpt);

                            // constructor can't produce state machine
                            Debug.Assert((object)ctorStateMachineTypeOpt == null);

                            boundStatements = boundStatements.Insert(0, chain);
                        }

                        CSharpSyntaxNode syntax = methodSymbol.GetNonNullSyntaxNode();

                        var boundBody = BoundStatementList.Synthesized(syntax, boundStatements);

                        var emittedBody = GenerateMethodBody(
                            _moduleBeingBuiltOpt,
                            methodSymbol,
                            methodOrdinal,
                            boundBody,
                            lambdaDebugInfoBuilder.ToImmutable(),
                            closureDebugInfoBuilder.ToImmutable(),
                            stateMachineTypeOpt,
                            lazyVariableSlotAllocator,
                            diagsForCurrentMethod,
                            _debugDocumentProvider,
                            importChain,
                            _emittingPdb);

                        _moduleBeingBuiltOpt.SetMethodBody(methodSymbol.PartialDefinitionPart ?? methodSymbol, emittedBody);
                    }

                    _diagnostics.AddRange(diagsForCurrentMethod);
                }
                finally
                {
                    lambdaDebugInfoBuilder.Free();
                    closureDebugInfoBuilder.Free();
                }
            }
            finally
            {
                diagsForCurrentMethod.Free();
                compilationState.CurrentImportChain = oldImportChain;
            }
        }

        /// <summary>
        /// Synthesized parameterless constructors in structs chain to the "default" constructor
        /// </summary>
        private BoundStatement ChainImplicitStructConstructor(MethodSymbol methodSymbol, SourceMemberContainerTypeSymbol containingType)
        {
            CSharpSyntaxNode syntax = methodSymbol.GetNonNullSyntaxNode();

            // TODO: can we skip this if we have as many initializers as instance fields?
            //       there could be an observable difference if initializer crashes 
            //       and constructor is invoked in-place and the partially initialized 
            //       instance escapes. (impossible in C#, I believe)
            //
            // add "this = default(T)" at the beginning of implicit struct ctor
            return new BoundExpressionStatement(syntax,
                    new BoundAssignmentOperator(
                    syntax,
                    new BoundThisReference(syntax, containingType),
                    new BoundDefaultOperator(syntax, containingType),
                    RefKind.None,
                    containingType));
        }

        // internal for testing
        internal static BoundStatement LowerBodyOrInitializer(
            MethodSymbol method,
            int methodOrdinal,
            BoundStatement body,
            SynthesizedSubmissionFields previousSubmissionFields,
            TypeCompilationState compilationState,
            DiagnosticBag diagnostics,
            ref VariableSlotAllocator lazyVariableSlotAllocator,
            ArrayBuilder<LambdaDebugInfo> lambdaDebugInfoBuilder,
            ArrayBuilder<ClosureDebugInfo> closureDebugInfoBuilder,
            out StateMachineTypeSymbol stateMachineTypeOpt)
        {
            Debug.Assert(compilationState.ModuleBuilderOpt != null);
            stateMachineTypeOpt = null;

            if (body.HasErrors)
            {
                return body;
            }

            try
            {
                bool sawLambdas;
            bool sawLocalFunctions;
                bool sawAwaitInExceptionHandler;
                var loweredBody = LocalRewriter.Rewrite(
                    method.DeclaringCompilation,
                    method,
                    methodOrdinal,
                    method.ContainingType,
                    body,
                    compilationState,
                    previousSubmissionFields: previousSubmissionFields,
                    allowOmissionOfConditionalCalls: true,
                    diagnostics: diagnostics,
                    sawLambdas: out sawLambdas,
                sawLocalFunctions: out sawLocalFunctions,
                    sawAwaitInExceptionHandler: out sawAwaitInExceptionHandler);

                if (loweredBody.HasErrors)
                {
                    return loweredBody;
                }

                if (sawAwaitInExceptionHandler)
                {
                    // If we have awaits in handlers, we need to 
                    // replace handlers with synthetic ones which can be consumed by async rewriter.
                    // The reason why this rewrite happens before the lambda rewrite 
                    // is that we may need access to exception locals and it would be fairly hard to do
                    // if these locals are captured into closures (possibly nested ones).
                    Debug.Assert(!method.IsIterator);
                    loweredBody = AsyncExceptionHandlerRewriter.Rewrite(
                        method,
                        method.ContainingType,
                        loweredBody,
                        compilationState,
                        diagnostics);
                }

                if (loweredBody.HasErrors)
                {
                    return loweredBody;
                }

                if (lazyVariableSlotAllocator == null)
                {
                    lazyVariableSlotAllocator = compilationState.ModuleBuilderOpt.TryCreateVariableSlotAllocator(method, method);
                }

                BoundStatement bodyWithoutLambdas = loweredBody;
            if (sawLambdas || sawLocalFunctions)
                {
                    bodyWithoutLambdas = LambdaRewriter.Rewrite(
                        loweredBody,
                        method.ContainingType,
                        method.ThisParameter,
                        method,
                        methodOrdinal,
                    null,
                        lambdaDebugInfoBuilder,
                        closureDebugInfoBuilder,
                        lazyVariableSlotAllocator,
                        compilationState,
                        diagnostics,
                        assignLocals: false);
                }

                if (bodyWithoutLambdas.HasErrors)
                {
                    return bodyWithoutLambdas;
                }

                IteratorStateMachine iteratorStateMachine;
                BoundStatement bodyWithoutIterators = IteratorRewriter.Rewrite(bodyWithoutLambdas, method, methodOrdinal, lazyVariableSlotAllocator, compilationState, diagnostics, out iteratorStateMachine);

                if (bodyWithoutIterators.HasErrors)
                {
                    return bodyWithoutIterators;
                }

                AsyncStateMachine asyncStateMachine;
                BoundStatement bodyWithoutAsync = AsyncRewriter.Rewrite(bodyWithoutIterators, method, methodOrdinal, lazyVariableSlotAllocator, compilationState, diagnostics, out asyncStateMachine);

                Debug.Assert(iteratorStateMachine == null || asyncStateMachine == null);
                stateMachineTypeOpt = (StateMachineTypeSymbol)iteratorStateMachine ?? asyncStateMachine;

                return bodyWithoutAsync;
            }
            catch (BoundTreeVisitor.CancelledByStackGuardException ex)
            {
                ex.AddAnError(diagnostics);
                return new BoundBadStatement(body.Syntax, ImmutableArray.Create<BoundNode>(body), hasErrors: true);
            }
        }

        private static MethodBody GenerateMethodBody(
            PEModuleBuilder moduleBuilder,
            MethodSymbol method,
            int methodOrdinal,
            BoundStatement block,
            ImmutableArray<LambdaDebugInfo> lambdaDebugInfo,
            ImmutableArray<ClosureDebugInfo> closureDebugInfo,
            StateMachineTypeSymbol stateMachineTypeOpt,
            VariableSlotAllocator variableSlotAllocatorOpt,
            DiagnosticBag diagnostics,
            DebugDocumentProvider debugDocumentProvider,
            ImportChain importChainOpt,
            bool emittingPdb)
        {
            // Note: don't call diagnostics.HasAnyErrors() in release; could be expensive if compilation has many warnings.
            Debug.Assert(!diagnostics.HasAnyErrors(), "Running code generator when errors exist might be dangerous; code generator not expecting errors");

            var compilation = moduleBuilder.Compilation;
            var localSlotManager = new LocalSlotManager(variableSlotAllocatorOpt);
            var optimizations = compilation.Options.OptimizationLevel;

            ILBuilder builder = new ILBuilder(moduleBuilder, localSlotManager, optimizations);
            DiagnosticBag diagnosticsForThisMethod = DiagnosticBag.GetInstance();
            try
            {
                Cci.AsyncMethodBodyDebugInfo asyncDebugInfo = null;

                var codeGen = new CodeGen.CodeGenerator(method, block, builder, moduleBuilder, diagnosticsForThisMethod, optimizations, emittingPdb);

                if (diagnosticsForThisMethod.HasAnyErrors())
                {
                    // we are done here. Since there were errors we should not emit anything.
                    return null;
                }

                // We need to save additional debugging information for MoveNext of an async state machine.
                var stateMachineMethod = method as SynthesizedStateMachineMethod;
                bool isStateMachineMoveNextMethod = stateMachineMethod != null && method.Name == WellKnownMemberNames.MoveNextMethodName;

                if (isStateMachineMoveNextMethod && stateMachineMethod.StateMachineType.KickoffMethod.IsAsync)
                {
                    int asyncCatchHandlerOffset;
                    ImmutableArray<int> asyncYieldPoints;
                    ImmutableArray<int> asyncResumePoints;
                    codeGen.Generate(out asyncCatchHandlerOffset, out asyncYieldPoints, out asyncResumePoints);

                    var kickoffMethod = stateMachineMethod.StateMachineType.KickoffMethod;

                    // The exception handler IL offset is used by the debugger to treat exceptions caught by the marked catch block as "user unhandled".
                    // This is important for async void because async void exceptions generally result in the process being terminated,
                    // but without anything useful on the call stack. Async Task methods on the other hand return exceptions as the result of the Task.
                    // So it is undesirable to consider these exceptions "user unhandled" since there may well be user code that is awaiting the task.
                    // This is a heuristic since it's possible that there is no user code awaiting the task.
                    asyncDebugInfo = new Cci.AsyncMethodBodyDebugInfo(kickoffMethod, kickoffMethod.ReturnsVoid ? asyncCatchHandlerOffset : -1, asyncYieldPoints, asyncResumePoints);
                }
                else
                {
                    codeGen.Generate();
                }

                // Translate the imports even if we are not writing PDBs. The translation has an impact on generated metadata 
                // and we don't want to emit different metadata depending on whether or we emit with PDB stream.
                // TODO (https://github.com/dotnet/roslyn/issues/2846): This will need to change for member initializers in partial class.
                var importScopeOpt = importChainOpt?.Translate(moduleBuilder, diagnosticsForThisMethod);

                var localVariables = builder.LocalSlotManager.LocalsInOrder();

                if (localVariables.Length > 0xFFFE)
                {
                    diagnosticsForThisMethod.Add(ErrorCode.ERR_TooManyLocals, method.Locations.First());
                }

                if (diagnosticsForThisMethod.HasAnyErrors())
                {
                    // we are done here. Since there were errors we should not emit anything.
                    return null;
                }

                // We will only save the IL builders when running tests.
                if (moduleBuilder.SaveTestData)
                {
                    moduleBuilder.SetMethodTestData(method, builder.GetSnapshot());
                }

                // Only compiler-generated MoveNext methods have iterator scopes.  See if this is one.
                var stateMachineHoistedLocalScopes = default(ImmutableArray<Cci.StateMachineHoistedLocalScope>);
                if (isStateMachineMoveNextMethod)
                {
                    stateMachineHoistedLocalScopes = builder.GetHoistedLocalScopes();
                }

                var stateMachineHoistedLocalSlots = default(ImmutableArray<EncHoistedLocalInfo>);
                var stateMachineAwaiterSlots = default(ImmutableArray<Cci.ITypeReference>);
                if (optimizations == OptimizationLevel.Debug && stateMachineTypeOpt != null)
                {
                    Debug.Assert(method.IsAsync || method.IsIterator);
                    GetStateMachineSlotDebugInfo(moduleBuilder, moduleBuilder.GetSynthesizedFields(stateMachineTypeOpt), variableSlotAllocatorOpt, diagnosticsForThisMethod, out stateMachineHoistedLocalSlots, out stateMachineAwaiterSlots);
                    Debug.Assert(!diagnostics.HasAnyErrors());
                }

                return new MethodBody(
                    builder.RealizedIL,
                    builder.MaxStack,
                    method.PartialDefinitionPart ?? method,
                    variableSlotAllocatorOpt?.MethodId ?? new DebugId(methodOrdinal, moduleBuilder.CurrentGenerationOrdinal),
                    localVariables,
                    builder.RealizedSequencePoints,
                    debugDocumentProvider,
                    builder.RealizedExceptionHandlers,
                    builder.GetAllScopes(),
                    builder.HasDynamicLocal,
                    importScopeOpt,
                    lambdaDebugInfo,
                    closureDebugInfo,
                    stateMachineTypeOpt?.Name,
                    stateMachineHoistedLocalScopes,
                    stateMachineHoistedLocalSlots,
                    stateMachineAwaiterSlots,
                    asyncDebugInfo);
            }
            finally
            {
                // Basic blocks contain poolable builders for IL and sequence points. Free those back
                // to their pools.
                builder.FreeBasicBlocks();

                // Remember diagnostics.
                diagnostics.AddRange(diagnosticsForThisMethod);
                diagnosticsForThisMethod.Free();
            }
        }

        private static void GetStateMachineSlotDebugInfo(
            PEModuleBuilder moduleBuilder,
            IEnumerable<Cci.IFieldDefinition> fieldDefs,
            VariableSlotAllocator variableSlotAllocatorOpt,
            DiagnosticBag diagnostics,
            out ImmutableArray<EncHoistedLocalInfo> hoistedVariableSlots,
            out ImmutableArray<Cci.ITypeReference> awaiterSlots)
        {
            var hoistedVariables = ArrayBuilder<EncHoistedLocalInfo>.GetInstance();
            var awaiters = ArrayBuilder<Cci.ITypeReference>.GetInstance();

            foreach (StateMachineFieldSymbol field in fieldDefs)
            {
                int index = field.SlotIndex;

                if (field.SlotDebugInfo.SynthesizedKind == SynthesizedLocalKind.AwaiterField)
                {
                    Debug.Assert(index >= 0);

                    while (index >= awaiters.Count)
                    {
                        awaiters.Add(null);
                    }

                    awaiters[index] = moduleBuilder.EncTranslateLocalVariableType(field.Type.TypeSymbol, diagnostics);
                }
                else if (!field.SlotDebugInfo.Id.IsNone)
                {
                    Debug.Assert(index >= 0 && field.SlotDebugInfo.SynthesizedKind.IsLongLived());

                    while (index >= hoistedVariables.Count)
                    {
                        // Empty slots may be present if variables were deleted during EnC.
                        hoistedVariables.Add(new EncHoistedLocalInfo(true));
                    }

                    hoistedVariables[index] = new EncHoistedLocalInfo(field.SlotDebugInfo, moduleBuilder.EncTranslateLocalVariableType(field.Type.TypeSymbol, diagnostics));
                }
            }

            // Fill in empty slots for variables deleted during EnC that are not followed by an existing variable:
            if (variableSlotAllocatorOpt != null)
            {
                int previousAwaiterCount = variableSlotAllocatorOpt.PreviousAwaiterSlotCount;
                while (awaiters.Count < previousAwaiterCount)
                {
                    awaiters.Add(null);
                }

                int previousAwaiterSlotCount = variableSlotAllocatorOpt.PreviousHoistedLocalSlotCount;
                while (hoistedVariables.Count < previousAwaiterSlotCount)
                {
                    hoistedVariables.Add(new EncHoistedLocalInfo(true));
                }
            }

            hoistedVariableSlots = hoistedVariables.ToImmutableAndFree();
            awaiterSlots = awaiters.ToImmutableAndFree();
        }

        // NOTE: can return null if the method has no body.
        internal static BoundBlock BindMethodBody(MethodSymbol method, TypeCompilationState compilationState, DiagnosticBag diagnostics)
        {
            ImportChain importChain;
            bool originalBodyNested;
            return BindMethodBody(method, compilationState, diagnostics, out importChain, out originalBodyNested);
        }

        // NOTE: can return null if the method has no body.
        private static BoundBlock BindMethodBody(MethodSymbol method, TypeCompilationState compilationState, DiagnosticBag diagnostics, out ImportChain importChain, out bool originalBodyNested)
        {
            originalBodyNested = false;
            importChain = null;

            BoundBlock body;

            var sourceMethod = method as SourceMethodSymbol;
            if ((object)sourceMethod != null)
            {
                if (sourceMethod.IsExtern)
                {
                    if (sourceMethod.BodySyntax == null && (sourceMethod.SyntaxNode as ConstructorDeclarationSyntax)?.Initializer == null)
                    {
                        // Generate warnings only if we are not generating ERR_ExternHasBody or ERR_ExternHasConstructorInitializer errors
                        GenerateExternalMethodWarnings(sourceMethod, diagnostics);
                    }

                    return null;
                }

                if (sourceMethod.IsDefaultValueTypeConstructor())
                {
                    // No body for default struct constructor.
                    return null;
                }

                var compilation = method.DeclaringCompilation;
                var factory = compilation.GetBinderFactory(sourceMethod.SyntaxTree);

                var blockSyntax = sourceMethod.BodySyntax as BlockSyntax;

                if (blockSyntax != null)
                {
                    var inMethodBinder = factory.GetBinder(blockSyntax);

                    var binder = new ExecutableCodeBinder(blockSyntax, sourceMethod, inMethodBinder);
                    body = binder.BindBlock(blockSyntax, diagnostics);

                    importChain = binder.ImportChain;

                    if (method.MethodKind == MethodKind.Destructor)
                    {
                        return MethodBodySynthesizer.ConstructDestructorBody(method, body);
                    }

                    foreach (var iterator in binder.MethodSymbolsWithYield)
                    {
                        foreach (var parameter in iterator.Parameters)
                        {
                            if (parameter.RefKind != RefKind.None)
                            {
                                diagnostics.Add(ErrorCode.ERR_BadIteratorArgType, parameter.Locations[0]);
                            }
                            else if (parameter.Type.IsUnsafe())
                            {
                                diagnostics.Add(ErrorCode.ERR_UnsafeIteratorArgType, parameter.Locations[0]);
                            }
                        }

                        if (iterator.IsVararg)
                        {
                            // error CS1636: __arglist is not allowed in the parameter list of iterators
                            diagnostics.Add(ErrorCode.ERR_VarargsIterator, iterator.Locations[0]);
                        }

                        if (((iterator as SourceMethodSymbol)?.IsUnsafe == true || (iterator as LocalFunctionSymbol)?.IsUnsafe == true) && compilation.Options.AllowUnsafe) // Don't cascade
                        {
                            diagnostics.Add(ErrorCode.ERR_IllegalInnerUnsafe, iterator.Locations[0]);
                        }
                    }
                }
                else if (sourceMethod.IsExpressionBodied)
                {
                    var methodSyntax = sourceMethod.SyntaxNode;
                    var arrowExpression = methodSyntax.GetExpressionBodySyntax();

                    Binder binder = factory.GetBinder(arrowExpression);
                    binder = new ExecutableCodeBinder(arrowExpression, sourceMethod, binder);
                    importChain = binder.ImportChain;
                    // Add locals
                    return binder.BindExpressionBodyAsBlock(arrowExpression, diagnostics);
                }
                else
                {
                    var property = sourceMethod.AssociatedSymbol as SourcePropertySymbol;
                    if ((object)property != null && property.IsAutoProperty)
                    {
                        return MethodBodySynthesizer.ConstructAutoPropertyAccessorBody(sourceMethod);
                    }

                    return null;
                }
            }
            else
            {
                // synthesized methods should return their bound bodies
                body = null;
            }

            var constructorInitializer = BindConstructorInitializerIfAny(method, compilationState, diagnostics);
            ImmutableArray<BoundStatement> statements;

            if (constructorInitializer == null)
            {
                if (body != null)
                {
                    return body;
                }
                statements = ImmutableArray<BoundStatement>.Empty;
            }
            else if (body == null)
            {
                statements = ImmutableArray.Create(constructorInitializer);
            }
            else
            {
                statements = ImmutableArray.Create(constructorInitializer, body);
                originalBodyNested = true;
            }

            return BoundBlock.SynthesizedNoLocals(method.GetNonNullSyntaxNode(), statements);
        }

        private static BoundStatement BindConstructorInitializerIfAny(MethodSymbol method, TypeCompilationState compilationState, DiagnosticBag diagnostics)
        {
            // delegates have constructors but not constructor initializers
            if (method.MethodKind == MethodKind.Constructor && !method.ContainingType.IsDelegateType() && !method.IsExtern)
            {
                var compilation = method.DeclaringCompilation;
                var initializerInvocation = BindConstructorInitializer(method, diagnostics, compilation);

                if (initializerInvocation != null)
                {
                    var ctorCall = initializerInvocation as BoundCall;
                    if (ctorCall != null && !ctorCall.HasAnyErrors && ctorCall.Method != method && ctorCall.Method.ContainingType == method.ContainingType)
                    {
                        // Detect and report indirect cycles in the ctor-initializer call graph.
                        compilationState.ReportCtorInitializerCycles(method, ctorCall.Method, ctorCall.Syntax, diagnostics);
                    }

                    var constructorInitializer = new BoundExpressionStatement(initializerInvocation.Syntax, initializerInvocation) { WasCompilerGenerated = true };
                    Debug.Assert(initializerInvocation.HasAnyErrors || constructorInitializer.IsConstructorInitializer(), "Please keep this bound node in sync with BoundNodeExtensions.IsConstructorInitializer.");
                    return constructorInitializer;
                }
            }

            return null;
        }

        /// <summary>
        /// Bind the (implicit or explicit) constructor initializer of a constructor symbol.
        /// </summary>
        /// <param name="constructor">Constructor method.</param>
        /// <param name="diagnostics">Accumulates errors (e.g. access "this" in constructor initializer).</param>
        /// <param name="compilation">Used to retrieve binder.</param>
        /// <returns>A bound expression for the constructor initializer call.</returns>
        internal static BoundExpression BindConstructorInitializer(MethodSymbol constructor, DiagnosticBag diagnostics, CSharpCompilation compilation)
        {
            // Note that the base type can be null if we're compiling System.Object in source.
            NamedTypeSymbol baseType = constructor.ContainingType.BaseTypeNoUseSiteDiagnostics;

            SourceMethodSymbol sourceConstructor = constructor as SourceMethodSymbol;
            ConstructorDeclarationSyntax constructorSyntax = null;
            ArgumentListSyntax initializerArgumentListOpt = null;
            if ((object)sourceConstructor != null)
            {
                constructorSyntax = (ConstructorDeclarationSyntax)sourceConstructor.SyntaxNode;
                if (constructorSyntax.Initializer != null)
                {
                    initializerArgumentListOpt = constructorSyntax.Initializer.ArgumentList;
                }
            }

            // The common case is that we have no constructor initializer and the type inherits directly from object.
            // Also, we might be trying to generate a constructor for an entirely compiler-generated class such
            // as a closure class; in that case it is vexing to try to find a suitable binder for the non-existing
            // constructor syntax so that we can do unnecessary overload resolution on the non-existing initializer!
            // Simply take the early out: bind directly to the parameterless object ctor rather than attempting
            // overload resolution.
            if (initializerArgumentListOpt == null && (object)baseType != null)
            {
                if (baseType.SpecialType == SpecialType.System_Object)
                {
                    return GenerateObjectConstructorInitializer(constructor, diagnostics);
                }
                else if (baseType.IsErrorType() || baseType.IsStatic)
                {
                    // If the base type is bad and there is no initializer then we can just bail.
                    // We have no expressions we need to analyze to report errors on.
                    return null;
                }
            }

            // Either our base type is not object, or we have an initializer syntax, or both. We're going to
            // need to do overload resolution on the set of constructors of the base type, either on
            // the provided initializer syntax, or on an implicit ": base()" syntax.

            // SPEC ERROR: The specification states that if you have the situation 
            // SPEC ERROR: class B { ... } class D1 : B {} then the default constructor
            // SPEC ERROR: generated for D1 must call an accessible *parameterless* constructor
            // SPEC ERROR: in B. However, it also states that if you have 
            // SPEC ERROR: class B { ... } class D2 : B { D2() {} }  or
            // SPEC ERROR: class B { ... } class D3 : B { D3() : base() {} }  then
            // SPEC ERROR: the compiler performs *overload resolution* to determine
            // SPEC ERROR: which accessible constructor of B is called. Since B might have
            // SPEC ERROR: a ctor with all optional parameters, overload resolution might
            // SPEC ERROR: succeed even if there is no parameterless constructor. This
            // SPEC ERROR: is unintentionally inconsistent, and the native compiler does not
            // SPEC ERROR: implement this behavior. Rather, we should say in the spec that
            // SPEC ERROR: if there is no ctor in D1, then a ctor is created for you exactly
            // SPEC ERROR: as though you'd said "D1() : base() {}". 
            // SPEC ERROR: This is what we now do in Roslyn.

            // Now, in order to do overload resolution, we're going to need a binder. There are
            // three possible situations:
            //
            // class D1 : B { }
            // class D2 : B { D2(int x) { } }
            // class D3 : B { D3(int x) : base(x) { } }
            //
            // In the first case the binder needs to be the binder associated with
            // the *body* of D1 because if the base class ctor is protected, we need
            // to be inside the body of a derived class in order for it to be in the
            // accessibility domain of the protected base class ctor.
            //
            // In the second case the binder could be the binder associated with 
            // the body of D2; since the implicit call to base() will have no arguments
            // there is no need to look up "x".
            // 
            // In the third case the binder must be the binder that knows about "x" 
            // because x is in scope.

            Binder outerBinder;

            if ((object)sourceConstructor == null)
            {
                // The constructor is implicit. We need to get the binder for the body
                // of the enclosing class. 
                CSharpSyntaxNode containerNode = constructor.GetNonNullSyntaxNode();
                SyntaxToken bodyToken = GetImplicitConstructorBodyToken(containerNode);
                outerBinder = compilation.GetBinderFactory(containerNode.SyntaxTree).GetBinder(containerNode, bodyToken.Position);
            }
            else if (initializerArgumentListOpt == null)
            {
                // We have a ctor in source but no explicit constructor initializer.  We can't just use the binder for the
                // type containing the ctor because the ctor might be marked unsafe.  Use the binder for the parameter list
                // as an approximation - the extra symbols won't matter because there are no identifiers to bind.

                outerBinder = compilation.GetBinderFactory(sourceConstructor.SyntaxTree).GetBinder(constructorSyntax.ParameterList);
            }
            else
            {
                outerBinder = compilation.GetBinderFactory(sourceConstructor.SyntaxTree).GetBinder(initializerArgumentListOpt);
            }

            //wrap in ConstructorInitializerBinder for appropriate errors
            Binder initializerBinder = outerBinder.WithAdditionalFlagsAndContainingMemberOrLambda(BinderFlags.ConstructorInitializer, constructor);

            return initializerBinder.BindConstructorInitializer(initializerArgumentListOpt, constructor, diagnostics);
        }

        private static SyntaxToken GetImplicitConstructorBodyToken(CSharpSyntaxNode containerNode)
        {
            var kind = containerNode.Kind();
            switch (kind)
            {
                case SyntaxKind.ClassDeclaration:
                    return ((ClassDeclarationSyntax)containerNode).OpenBraceToken;
                case SyntaxKind.StructDeclaration:
                    return ((StructDeclarationSyntax)containerNode).OpenBraceToken;
                case SyntaxKind.EnumDeclaration:
                    // We're not going to find any non-default ctors, but we'll look anyway.
                    return ((EnumDeclarationSyntax)containerNode).OpenBraceToken;
                default:
                    throw ExceptionUtilities.UnexpectedValue(kind);
            }
        }

        internal static BoundCall GenerateObjectConstructorInitializer(MethodSymbol constructor, DiagnosticBag diagnostics)
        {
            NamedTypeSymbol objectType = constructor.ContainingType.BaseTypeNoUseSiteDiagnostics;
            Debug.Assert(objectType.SpecialType == SpecialType.System_Object);
            MethodSymbol objectConstructor = null;
            LookupResultKind resultKind = LookupResultKind.Viable;

            foreach (MethodSymbol objectCtor in objectType.InstanceConstructors)
            {
                if (objectCtor.ParameterCount == 0)
                {
                    objectConstructor = objectCtor;
                    break;
                }
            }

            // UNDONE: If this happens then something is deeply wrong. Should we give a better error?
            if ((object)objectConstructor == null)
            {
                diagnostics.Add(ErrorCode.ERR_BadCtorArgCount, constructor.Locations[0], objectType, /*desired param count*/ 0);
                return null;
            }

            // UNDONE: If this happens then something is deeply wrong. Should we give a better error?
            bool hasErrors = false;
            HashSet<DiagnosticInfo> useSiteDiagnostics = null;
            if (!AccessCheck.IsSymbolAccessible(objectConstructor, constructor.ContainingType, ref useSiteDiagnostics))
            {
                diagnostics.Add(ErrorCode.ERR_BadAccess, constructor.Locations[0], objectConstructor);
                resultKind = LookupResultKind.Inaccessible;
                hasErrors = true;
            }

            if (!useSiteDiagnostics.IsNullOrEmpty())
            {
                diagnostics.Add(constructor.Locations.IsEmpty ? NoLocation.Singleton : constructor.Locations[0], useSiteDiagnostics);
            }

            CSharpSyntaxNode syntax = constructor.GetNonNullSyntaxNode();

            BoundExpression receiver = new BoundThisReference(syntax, constructor.ContainingType) { WasCompilerGenerated = true };
            return new BoundCall(
                syntax: syntax,
                receiverOpt: receiver,
                method: objectConstructor,
                arguments: ImmutableArray<BoundExpression>.Empty,
                argumentNamesOpt: ImmutableArray<string>.Empty,
                argumentRefKindsOpt: ImmutableArray<RefKind>.Empty,
                isDelegateCall: false,
                expanded: false,
                invokedAsExtensionMethod: false,
                argsToParamsOpt: ImmutableArray<int>.Empty,
                resultKind: resultKind,
                type: objectType,
                hasErrors: hasErrors)
            { WasCompilerGenerated = true };
        }

        private static void GenerateExternalMethodWarnings(SourceMethodSymbol methodSymbol, DiagnosticBag diagnostics)
        {
            if (methodSymbol.GetAttributes().IsEmpty && !methodSymbol.ContainingType.IsComImport)
            {
                // external method with no attributes
                var errorCode = (methodSymbol.MethodKind == MethodKind.Constructor || methodSymbol.MethodKind == MethodKind.StaticConstructor) ?
                    ErrorCode.WRN_ExternCtorNoImplementation :
                    ErrorCode.WRN_ExternMethodNoImplementation;
                diagnostics.Add(errorCode, methodSymbol.Locations[0], methodSymbol);
            }
        }

        /// <summary>
        /// Returns true if the method is a constructor and has a this() constructor initializer.
        /// </summary>
        private static bool HasThisConstructorInitializer(MethodSymbol method)
        {
            if ((object)method != null && method.MethodKind == MethodKind.Constructor)
            {
                SourceMethodSymbol sourceMethod = method as SourceMethodSymbol;
                if ((object)sourceMethod != null)
                {
                    ConstructorDeclarationSyntax constructorSyntax = sourceMethod.SyntaxNode as ConstructorDeclarationSyntax;
                    if (constructorSyntax != null)
                    {
                        ConstructorInitializerSyntax initializerSyntax = constructorSyntax.Initializer;
                        if (initializerSyntax != null)
                        {
                            return initializerSyntax.Kind() == SyntaxKind.ThisConstructorInitializer;
                        }
                    }
                }
            }

            return false;
        }

        private static Cci.DebugSourceDocument CreateDebugDocumentForFile(string normalizedPath)
        {
            return new Cci.DebugSourceDocument(normalizedPath, Cci.DebugSourceDocument.CorSymLanguageTypeCSharp);
        }

        private static bool PassesFilter(Predicate<Symbol> filterOpt, Symbol symbol)
        {
            return (filterOpt == null) || filterOpt(symbol);
        }
    }
}
