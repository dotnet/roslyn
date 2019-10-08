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
using Microsoft.CodeAnalysis.Debugging;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.CodeAnalysis.PooledObjects;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal sealed class MethodCompiler : CSharpSymbolVisitor<TypeCompilationState, object>
    {
        private readonly CSharpCompilation _compilation;
        private readonly bool _emittingPdb;
        private readonly bool _emitTestCoverageData;
        private readonly CancellationToken _cancellationToken;
        private readonly DiagnosticBag _diagnostics;
        private readonly bool _hasDeclarationErrors;
        private readonly PEModuleBuilder _moduleBeingBuiltOpt; // Null if compiling for diagnostics
        private readonly Predicate<Symbol> _filterOpt;         // If not null, limit analysis to specific symbols
        private readonly DebugDocumentProvider _debugDocumentProvider;
        private readonly SynthesizedEntryPointSymbol.AsyncForwardEntryPoint _entryPointOpt;

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
        internal MethodCompiler(CSharpCompilation compilation, PEModuleBuilder moduleBeingBuiltOpt, bool emittingPdb, bool emitTestCoverageData, bool hasDeclarationErrors,
            DiagnosticBag diagnostics, Predicate<Symbol> filterOpt, SynthesizedEntryPointSymbol.AsyncForwardEntryPoint entryPointOpt, CancellationToken cancellationToken)
        {
            Debug.Assert(compilation != null);
            Debug.Assert(diagnostics != null);

            _compilation = compilation;
            _moduleBeingBuiltOpt = moduleBeingBuiltOpt;
            _emittingPdb = emittingPdb;
            _cancellationToken = cancellationToken;
            _diagnostics = diagnostics;
            _filterOpt = filterOpt;
            _entryPointOpt = entryPointOpt;

            _hasDeclarationErrors = hasDeclarationErrors;
            SetGlobalErrorIfTrue(hasDeclarationErrors);

            if (emittingPdb || emitTestCoverageData)
            {
                _debugDocumentProvider = (path, basePath) => moduleBeingBuiltOpt.DebugDocumentsBuilder.GetOrAddDebugDocument(path, basePath, CreateDebugDocumentForFile);
            }

            _emitTestCoverageData = emitTestCoverageData;
        }

        public static void CompileMethodBodies(
            CSharpCompilation compilation,
            PEModuleBuilder moduleBeingBuiltOpt,
            bool emittingPdb,
            bool emitTestCoverageData,
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

            MethodSymbol entryPoint = null;
            if (filterOpt is null)
            {
                entryPoint = GetEntryPoint(compilation, moduleBeingBuiltOpt, hasDeclarationErrors, diagnostics, cancellationToken);
            }

            var methodCompiler = new MethodCompiler(
                compilation,
                moduleBeingBuiltOpt,
                emittingPdb,
                emitTestCoverageData,
                hasDeclarationErrors,
                diagnostics,
                filterOpt,
                entryPoint as SynthesizedEntryPointSymbol.AsyncForwardEntryPoint,
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
                var additionalTypes = moduleBeingBuiltOpt.GetAdditionalTopLevelTypes(diagnostics);
                methodCompiler.CompileSynthesizedMethods(additionalTypes, diagnostics);

                var embeddedTypes = moduleBeingBuiltOpt.GetEmbeddedTypes(diagnostics);
                methodCompiler.CompileSynthesizedMethods(embeddedTypes, diagnostics);

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

                if (moduleBeingBuiltOpt != null && entryPoint != null && compilation.Options.OutputKind.IsApplication())
                {
                    moduleBeingBuiltOpt.SetPEEntryPoint(entryPoint, diagnostics);
                }
            }
        }

        // Returns the MethodSymbol for the assembly entrypoint.  If the user has a Task returning main,
        // this function returns the synthesized Main MethodSymbol.
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

            if ((object)entryPoint == null)
            {
                Debug.Assert(entryPointAndDiagnostics.Diagnostics.HasAnyErrors() || !compilation.Options.Errors.IsDefaultOrEmpty);
                return null;
            }

            // entryPoint can be a SynthesizedEntryPointSymbol if a script is being compiled.
            SynthesizedEntryPointSymbol synthesizedEntryPoint = entryPoint as SynthesizedEntryPointSymbol;
            if ((object)synthesizedEntryPoint == null)
            {
                var returnType = entryPoint.ReturnType;
                if (returnType.IsGenericTaskType(compilation) || returnType.IsNonGenericTaskType(compilation))
                {
                    synthesizedEntryPoint = new SynthesizedEntryPointSymbol.AsyncForwardEntryPoint(compilation, entryPoint.ContainingType, entryPoint);
                    entryPoint = synthesizedEntryPoint;
                    if ((object)moduleBeingBuilt != null)
                    {
                        moduleBeingBuilt.AddSynthesizedDefinition(entryPoint.ContainingType, synthesizedEntryPoint);
                    }
                }
            }

            if (((object)synthesizedEntryPoint != null) &&
                (moduleBeingBuilt != null) &&
                !hasDeclarationErrors &&
                !diagnostics.HasAnyErrors())
            {
                BoundStatement body = synthesizedEntryPoint.CreateBody(diagnostics);
                if (body.HasErrors || diagnostics.HasAnyErrors())
                {
                    return entryPoint;
                }

                var dynamicAnalysisSpans = ImmutableArray<SourceSpan>.Empty;
                VariableSlotAllocator lazyVariableSlotAllocator = null;
                var lambdaDebugInfoBuilder = ArrayBuilder<LambdaDebugInfo>.GetInstance();
                var closureDebugInfoBuilder = ArrayBuilder<ClosureDebugInfo>.GetInstance();
                StateMachineTypeSymbol stateMachineTypeOpt = null;
                const int methodOrdinal = -1;

                var loweredBody = LowerBodyOrInitializer(
                    synthesizedEntryPoint,
                    methodOrdinal,
                    body,
                    null,
                    new TypeCompilationState(synthesizedEntryPoint.ContainingType, compilation, moduleBeingBuilt),
                    false,
                    null,
                    ref dynamicAnalysisSpans,
                    diagnostics,
                    ref lazyVariableSlotAllocator,
                    lambdaDebugInfoBuilder,
                    closureDebugInfoBuilder,
                    out stateMachineTypeOpt);

                Debug.Assert((object)lazyVariableSlotAllocator == null);
                Debug.Assert((object)stateMachineTypeOpt == null);
                Debug.Assert(dynamicAnalysisSpans.IsEmpty);
                Debug.Assert(lambdaDebugInfoBuilder.IsEmpty());
                Debug.Assert(closureDebugInfoBuilder.IsEmpty());

                lambdaDebugInfoBuilder.Free();
                closureDebugInfoBuilder.Free();

                var emittedBody = GenerateMethodBody(
                    moduleBeingBuilt,
                    synthesizedEntryPoint,
                    methodOrdinal,
                    loweredBody,
                    ImmutableArray<LambdaDebugInfo>.Empty,
                    ImmutableArray<ClosureDebugInfo>.Empty,
                    stateMachineTypeOpt: null,
                    variableSlotAllocatorOpt: null,
                    diagnostics: diagnostics,
                    debugDocumentProvider: null,
                    importChainOpt: null,
                    emittingPdb: false,
                    emitTestCoverageData: false,
                    dynamicAnalysisSpans: ImmutableArray<SourceSpan>.Empty,
                    entryPointOpt: null);
                moduleBeingBuilt.SetMethodBody(synthesizedEntryPoint, emittedBody);
            }

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

                                if (fieldSymbol.IsFixedSizeBuffer && compilationState.Emitting)
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
                    (init.Kind == BoundKind.FieldEqualsValue) && !((BoundFieldEqualsValue)init).Field.IsMetadataConstant));

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

            // If there is no explicit or implicit .cctor and no static initializers, then report
            // warnings for any static non-nullable fields. (If there is no .cctor, there
            // shouldn't be any initializers but for robustness, we check both.)
            if (!hasStaticConstructor &&
                processedStaticInitializers.BoundInitializers.IsDefaultOrEmpty &&
                _compilation.LanguageVersion >= MessageID.IDS_FeatureNullableReferenceTypes.RequiredVersion())
            {
                switch (containingType.TypeKind)
                {
                    case TypeKind.Class:
                    case TypeKind.Struct:
                        UnassignedFieldsWalker.ReportUninitializedNonNullableReferenceTypeFields(
                            walkerOpt: null,
                            thisSlot: -1,
                            isStatic: true,
                            members,
                            getIsAssigned: (walker, slot, member) => false,
                            getSymbolForLocation: (walker, member) => member,
                            _diagnostics);
                        break;
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
            foreach (MethodSymbol method in privateImplClass.GetMethods(new EmitContext(_moduleBeingBuiltOpt, null, diagnostics, metadataOnly: false, includePrivateMembers: true)))
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

                    // We make sure that an asynchronous mutation to the diagnostic bag does not 
                    // confuse the method body generator by making a fresh bag and then loading
                    // any diagnostics emitted into it back into the main diagnostic bag.
                    var diagnosticsThisMethod = DiagnosticBag.GetInstance();

                    var method = methodWithBody.Method;
                    var lambda = method as SynthesizedClosureMethod;
                    var variableSlotAllocatorOpt = ((object)lambda != null) ?
                        _moduleBeingBuiltOpt.TryCreateVariableSlotAllocator(lambda, lambda.TopLevelMethod, diagnosticsThisMethod) :
                        _moduleBeingBuiltOpt.TryCreateVariableSlotAllocator(method, method, diagnosticsThisMethod);

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

                            Debug.Assert((object)iteratorStateMachine == null || (object)asyncStateMachine == null);
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
                                emittingPdb: _emittingPdb,
                                emitTestCoverageData: _emitTestCoverageData,
                                dynamicAnalysisSpans: ImmutableArray<SourceSpan>.Empty,
                                _entryPointOpt);
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
                        emittingPdb: false,
                        emitTestCoverageData: _emitTestCoverageData,
                        dynamicAnalysisSpans: ImmutableArray<SourceSpan>.Empty,
                        entryPointOpt: null);

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
            SourceMemberMethodSymbol sourceMethod = methodSymbol as SourceMemberMethodSymbol;

            if (methodSymbol.IsAbstract || methodSymbol.ContainingType?.IsDelegateType() == true)
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
                MethodBodySemanticModel.InitialState forSemanticModel = default;
                ImportChain importChain = null;
                var hasTrailingExpression = false;

                if (methodSymbol.IsScriptConstructor)
                {
                    Debug.Assert(methodSymbol.IsImplicitlyDeclared);
                    body = new BoundBlock(methodSymbol.GetNonNullSyntaxNode(), ImmutableArray<LocalSymbol>.Empty, ImmutableArray<BoundStatement>.Empty) { WasCompilerGenerated = true };
                }
                else if (methodSymbol.IsScriptInitializer)
                {
                    Debug.Assert(methodSymbol.IsImplicitlyDeclared);

                    // rewrite top-level statements and script variable declarations to a list of statements and assignments, respectively:
                    var initializerStatements = InitializerRewriter.RewriteScriptInitializer(processedInitializers.BoundInitializers, (SynthesizedInteractiveInitializerMethod)methodSymbol, out hasTrailingExpression);

                    // the lowered script initializers should not be treated as initializers anymore but as a method body:
                    body = BoundBlock.SynthesizedNoLocals(initializerStatements.Syntax, initializerStatements.Statements);

                    var unusedDiagnostics = DiagnosticBag.GetInstance();
                    DefiniteAssignmentPass.Analyze(_compilation, methodSymbol, initializerStatements, unusedDiagnostics, requireOutParamsAssigned: false);
                    DiagnosticsPass.IssueDiagnostics(_compilation, initializerStatements, unusedDiagnostics, methodSymbol);
                    unusedDiagnostics.Free();
                }
                else
                {
                    // Do not emit initializers if we are invoking another constructor of this class.
                    includeInitializersInBody = !processedInitializers.BoundInitializers.IsDefaultOrEmpty &&
                                                !HasThisConstructorInitializer(methodSymbol);

                    body = BindMethodBody(methodSymbol, compilationState, diagsForCurrentMethod, out importChain, out originalBodyNested, out forSemanticModel);
                    if (diagsForCurrentMethod.HasAnyErrors() && body != null)
                    {
                        body = (BoundBlock)body.WithHasErrors();
                    }

                    if (body != null && methodSymbol.IsConstructor())
                    {
                        UnassignedFieldsWalker.Analyze(_compilation, methodSymbol, body, diagsForCurrentMethod);
                    }

                    // lower initializers just once. the lowered tree will be reused when emitting all constructors 
                    // with field initializers. Once lowered, these initializers will be stashed in processedInitializers.LoweredInitializers
                    // (see later in this method). Don't bother lowering _now_ if this particular ctor won't have the initializers 
                    // appended to its body.
                    if (includeInitializersInBody && processedInitializers.LoweredInitializers == null)
                    {
                        analyzedInitializers = InitializerRewriter.RewriteConstructor(processedInitializers.BoundInitializers, methodSymbol);
                        processedInitializers.HasErrors = processedInitializers.HasErrors || analyzedInitializers.HasAnyErrors;

                        if (body != null && ((methodSymbol.ContainingType.IsStructType() && !methodSymbol.IsImplicitConstructor) || _emitTestCoverageData))
                        {
                            if (_emitTestCoverageData && methodSymbol.IsImplicitConstructor)
                            {
                                // Flow analysis over the initializers is necessary in order to find assignments to fields.
                                // Bodies of implicit constructors do not get flow analysis later, so the initializers
                                // are analyzed here.
                                DefiniteAssignmentPass.Analyze(_compilation, methodSymbol, analyzedInitializers, diagsForCurrentMethod, requireOutParamsAssigned: false);
                            }

                            // In order to get correct diagnostics, we need to analyze initializers and the body together.
                            body = body.Update(body.Locals, body.LocalFunctions, body.Statements.Insert(0, analyzedInitializers));
                            includeInitializersInBody = false;
                            analyzedInitializers = null;
                        }
                        else
                        {
                            // These analyses check for diagnostics in lambdas.
                            // Control flow analysis and implicit return insertion are unnecessary.
                            DefiniteAssignmentPass.Analyze(_compilation, methodSymbol, analyzedInitializers, diagsForCurrentMethod, requireOutParamsAssigned: false);
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
                    Lazy<SemanticModel> lazySemanticModel = null;

                    if (body != null)
                    {
                        lazySemanticModel = new Lazy<SemanticModel>(() =>
                        {
                            var syntax = body.Syntax;
                            var semanticModel = (SyntaxTreeSemanticModel)_compilation.GetSemanticModel(syntax.SyntaxTree);

                            if (forSemanticModel.Syntax is { } semanticModelSyntax)
                            {
                                semanticModel.GetOrAddModel(semanticModelSyntax,
                                                            (rootSyntax) =>
                                                            {
                                                                Debug.Assert(rootSyntax == forSemanticModel.Syntax);
                                                                return MethodBodySemanticModel.Create(semanticModel,
                                                                                                      methodSymbol,
                                                                                                      forSemanticModel);
                                                            });
                            }

                            return semanticModel;
                        });
                    }

                    _compilation.EventQueue.TryEnqueue(new SymbolDeclaredCompilationEvent(_compilation, methodSymbol, lazySemanticModel));
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

                ImmutableArray<SourceSpan> dynamicAnalysisSpans = ImmutableArray<SourceSpan>.Empty;
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
                            _emitTestCoverageData,
                            _debugDocumentProvider,
                            ref dynamicAnalysisSpans,
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
                        Debug.Assert(!methodSymbol.IsImplicitInstanceConstructor || !methodSymbol.ContainingType.IsStructType());

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
                                // For dynamic analysis, field initializers are instrumented as part of constructors,
                                // and so are never instrumented here.
                                Debug.Assert(!_emitTestCoverageData);
                                StateMachineTypeSymbol initializerStateMachineTypeOpt;

                                BoundStatement lowered = LowerBodyOrInitializer(
                                    methodSymbol,
                                    methodOrdinal,
                                    analyzedInitializers,
                                    previousSubmissionFields,
                                    compilationState,
                                    _emitTestCoverageData,
                                    _debugDocumentProvider,
                                    ref dynamicAnalysisSpans,
                                    diagsForCurrentMethod,
                                    ref lazyVariableSlotAllocator,
                                    lambdaDebugInfoBuilder,
                                    closureDebugInfoBuilder,
                                    out initializerStateMachineTypeOpt);

                                processedInitializers.LoweredInitializers = lowered;

                                // initializers can't produce state machines
                                Debug.Assert((object)initializerStateMachineTypeOpt == null);
                                Debug.Assert(!hasErrors);
                                hasErrors = lowered.HasAnyErrors || diagsForCurrentMethod.HasAnyErrors();
                                SetGlobalErrorIfTrue(hasErrors);
                                if (hasErrors)
                                {
                                    _diagnostics.AddRange(diagsForCurrentMethod);
                                    return;
                                }

                                // Only do the cast if we haven't returned with some error diagnostics.
                                // Otherwise, `lowered` might have been a BoundBadStatement.
                                processedInitializers.LoweredInitializers = (BoundStatementList)lowered;
                            }

                            // initializers for global code have already been included in the body
                            if (includeInitializersInBody)
                            {
                                if (processedInitializers.LoweredInitializers.Kind == BoundKind.StatementList)
                                {
                                    BoundStatementList lowered = (BoundStatementList)processedInitializers.LoweredInitializers;
                                    boundStatements = boundStatements.Concat(lowered.Statements);
                                }
                                else
                                {
                                    boundStatements = boundStatements.Add(processedInitializers.LoweredInitializers);
                                }
                            }

                            if (hasBody)
                            {
                                boundStatements = boundStatements.Concat(ImmutableArray.Create(loweredBodyOpt));
                            }
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
                            _emittingPdb,
                            _emitTestCoverageData,
                            dynamicAnalysisSpans,
                            entryPointOpt: null);

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

        // internal for testing
        internal static BoundStatement LowerBodyOrInitializer(
            MethodSymbol method,
            int methodOrdinal,
            BoundStatement body,
            SynthesizedSubmissionFields previousSubmissionFields,
            TypeCompilationState compilationState,
            bool instrumentForDynamicAnalysis,
            DebugDocumentProvider debugDocumentProvider,
            ref ImmutableArray<SourceSpan> dynamicAnalysisSpans,
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
                var loweredBody = LocalRewriter.Rewrite(
                    method.DeclaringCompilation,
                    method,
                    methodOrdinal,
                    method.ContainingType,
                    body,
                    compilationState,
                    previousSubmissionFields: previousSubmissionFields,
                    allowOmissionOfConditionalCalls: true,
                    instrumentForDynamicAnalysis: instrumentForDynamicAnalysis,
                    debugDocumentProvider: debugDocumentProvider,
                    dynamicAnalysisSpans: ref dynamicAnalysisSpans,
                    diagnostics: diagnostics,
                    sawLambdas: out bool sawLambdas,
                    sawLocalFunctions: out bool sawLocalFunctions,
                    sawAwaitInExceptionHandler: out bool sawAwaitInExceptionHandler);

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
                    lazyVariableSlotAllocator = compilationState.ModuleBuilderOpt.TryCreateVariableSlotAllocator(method, method, diagnostics);
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
                        assignLocals: null);
                }

                if (bodyWithoutLambdas.HasErrors)
                {
                    return bodyWithoutLambdas;
                }

                BoundStatement bodyWithoutIterators = IteratorRewriter.Rewrite(bodyWithoutLambdas, method, methodOrdinal, lazyVariableSlotAllocator, compilationState, diagnostics,
                    out IteratorStateMachine iteratorStateMachine);

                if (bodyWithoutIterators.HasErrors)
                {
                    return bodyWithoutIterators;
                }

                BoundStatement bodyWithoutAsync = AsyncRewriter.Rewrite(bodyWithoutIterators, method, methodOrdinal, lazyVariableSlotAllocator, compilationState, diagnostics,
                    out AsyncStateMachine asyncStateMachine);

                Debug.Assert((object)iteratorStateMachine == null || (object)asyncStateMachine == null);
                stateMachineTypeOpt = (StateMachineTypeSymbol)iteratorStateMachine ?? asyncStateMachine;

                return bodyWithoutAsync;
            }
            catch (BoundTreeVisitor.CancelledByStackGuardException ex)
            {
                ex.AddAnError(diagnostics);
                return new BoundBadStatement(body.Syntax, ImmutableArray.Create<BoundNode>(body), hasErrors: true);
            }
        }

        /// <summary>
        /// entryPointOpt is only considered for synthesized methods (to recognize the synthesized MoveNext method for async Main)
        /// </summary>
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
            bool emittingPdb,
            bool emitTestCoverageData,
            ImmutableArray<SourceSpan> dynamicAnalysisSpans,
            SynthesizedEntryPointSymbol.AsyncForwardEntryPoint entryPointOpt)
        {
            // Note: don't call diagnostics.HasAnyErrors() in release; could be expensive if compilation has many warnings.
            Debug.Assert(!diagnostics.HasAnyErrors(), "Running code generator when errors exist might be dangerous; code generator not expecting errors");

            var compilation = moduleBuilder.Compilation;
            var localSlotManager = new LocalSlotManager(variableSlotAllocatorOpt);
            var optimizations = compilation.Options.OptimizationLevel;

            ILBuilder builder = new ILBuilder(moduleBuilder, localSlotManager, optimizations, method.AreLocalsZeroed);
            bool hasStackalloc = false;
            DiagnosticBag diagnosticsForThisMethod = DiagnosticBag.GetInstance();
            try
            {
                StateMachineMoveNextBodyDebugInfo moveNextBodyDebugInfoOpt = null;

                var codeGen = new CodeGen.CodeGenerator(method, block, builder, moduleBuilder, diagnosticsForThisMethod, optimizations, emittingPdb);

                if (diagnosticsForThisMethod.HasAnyErrors())
                {
                    // we are done here. Since there were errors we should not emit anything.
                    return null;
                }

                bool isAsyncStateMachine;
                MethodSymbol kickoffMethod;

                if (method is SynthesizedStateMachineMethod stateMachineMethod &&
                    method.Name == WellKnownMemberNames.MoveNextMethodName)
                {
                    kickoffMethod = stateMachineMethod.StateMachineType.KickoffMethod;
                    Debug.Assert(kickoffMethod != null);

                    isAsyncStateMachine = kickoffMethod.IsAsync;

                    // Async void method may be partial. Debug info needs to be associated with the emitted definition, 
                    // but the kickoff method is the method implementation (the part with body).
                    kickoffMethod = kickoffMethod.PartialDefinitionPart ?? kickoffMethod;
                }
                else
                {
                    kickoffMethod = null;
                    isAsyncStateMachine = false;
                }

                if (isAsyncStateMachine)
                {
                    codeGen.Generate(out int asyncCatchHandlerOffset, out var asyncYieldPoints, out var asyncResumePoints);

                    // The exception handler IL offset is used by the debugger to treat exceptions caught by the marked catch block as "user unhandled".
                    // This is important for async void because async void exceptions generally result in the process being terminated,
                    // but without anything useful on the call stack. Async Task methods on the other hand return exceptions as the result of the Task.
                    // So it is undesirable to consider these exceptions "user unhandled" since there may well be user code that is awaiting the task.
                    // This is a heuristic since it's possible that there is no user code awaiting the task.

                    // We do the same for async Main methods, since it is unlikely that user code will be awaiting the Task:
                    // AsyncForwardEntryPoint <Main> -> kick-off method Main -> MoveNext.

                    bool isAsyncMainMoveNext = entryPointOpt?.UserMain.Equals(kickoffMethod) == true;

                    moveNextBodyDebugInfoOpt = new AsyncMoveNextBodyDebugInfo(
                        kickoffMethod,
                        catchHandlerOffset: (kickoffMethod.ReturnsVoid || isAsyncMainMoveNext) ? asyncCatchHandlerOffset : -1,
                        asyncYieldPoints,
                        asyncResumePoints);
                }
                else
                {
                    codeGen.Generate(out hasStackalloc);

                    if ((object)kickoffMethod != null)
                    {
                        moveNextBodyDebugInfoOpt = new IteratorMoveNextBodyDebugInfo(kickoffMethod);
                    }
                }

                // Compiler-generated MoveNext methods have hoisted local scopes.
                // These are built by call to CodeGen.Generate.
                var stateMachineHoistedLocalScopes = ((object)kickoffMethod != null) ?
                    builder.GetHoistedLocalScopes() : default(ImmutableArray<StateMachineHoistedLocalScope>);

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

                var stateMachineHoistedLocalSlots = default(ImmutableArray<EncHoistedLocalInfo>);
                var stateMachineAwaiterSlots = default(ImmutableArray<Cci.ITypeReference>);
                if (optimizations == OptimizationLevel.Debug && (object)stateMachineTypeOpt != null)
                {
                    Debug.Assert(method.IsAsync || method.IsIterator);
                    GetStateMachineSlotDebugInfo(moduleBuilder, moduleBuilder.GetSynthesizedFields(stateMachineTypeOpt), variableSlotAllocatorOpt, diagnosticsForThisMethod, out stateMachineHoistedLocalSlots, out stateMachineAwaiterSlots);
                    Debug.Assert(!diagnostics.HasAnyErrors());
                }

                DynamicAnalysisMethodBodyData dynamicAnalysisDataOpt = null;
                if (emitTestCoverageData)
                {
                    Debug.Assert(debugDocumentProvider != null);
                    dynamicAnalysisDataOpt = new DynamicAnalysisMethodBodyData(dynamicAnalysisSpans);
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
                    builder.AreLocalsZeroed,
                    hasStackalloc,
                    builder.GetAllScopes(),
                    builder.HasDynamicLocal,
                    importScopeOpt,
                    lambdaDebugInfo,
                    closureDebugInfo,
                    stateMachineTypeOpt?.Name,
                    stateMachineHoistedLocalScopes,
                    stateMachineHoistedLocalSlots,
                    stateMachineAwaiterSlots,
                    moveNextBodyDebugInfoOpt,
                    dynamicAnalysisDataOpt);
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

                    awaiters[index] = moduleBuilder.EncTranslateLocalVariableType(field.Type, diagnostics);
                }
                else if (!field.SlotDebugInfo.Id.IsNone)
                {
                    Debug.Assert(index >= 0 && field.SlotDebugInfo.SynthesizedKind.IsLongLived());

                    while (index >= hoistedVariables.Count)
                    {
                        // Empty slots may be present if variables were deleted during EnC.
                        hoistedVariables.Add(new EncHoistedLocalInfo(true));
                    }

                    hoistedVariables[index] = new EncHoistedLocalInfo(field.SlotDebugInfo, moduleBuilder.EncTranslateLocalVariableType(field.Type, diagnostics));
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
            return BindMethodBody(method, compilationState, diagnostics, out _, out _, out _);
        }

        // NOTE: can return null if the method has no body.
        private static BoundBlock BindMethodBody(MethodSymbol method, TypeCompilationState compilationState, DiagnosticBag diagnostics,
                                                 out ImportChain importChain, out bool originalBodyNested,
                                                 out MethodBodySemanticModel.InitialState forSemanticModel)
        {
            originalBodyNested = false;
            importChain = null;
            forSemanticModel = default;

            BoundBlock body;

            var sourceMethod = method as SourceMemberMethodSymbol;
            if ((object)sourceMethod != null)
            {
                CSharpSyntaxNode syntaxNode = sourceMethod.SyntaxNode;
                var constructorSyntax = syntaxNode as ConstructorDeclarationSyntax;

                // Static constructor can't have any this/base call
                if (method.MethodKind == MethodKind.StaticConstructor &&
                    constructorSyntax?.Initializer != null)
                {
                    diagnostics.Add(
                        ErrorCode.ERR_StaticConstructorWithExplicitConstructorCall,
                        constructorSyntax.Initializer.ThisOrBaseKeyword.GetLocation(),
                        constructorSyntax.Identifier.ValueText);
                }

                ExecutableCodeBinder bodyBinder = sourceMethod.TryGetBodyBinder();

                if (sourceMethod.IsExtern)
                {
                    if (bodyBinder == null)
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

                if (bodyBinder != null)
                {
                    importChain = bodyBinder.ImportChain;
                    bodyBinder.ValidateIteratorMethods(diagnostics);

                    BoundNode methodBody = bodyBinder.BindMethodBody(syntaxNode, diagnostics);
                    BoundNode methodBodyForSemanticModel = methodBody;
                    NullableWalker.SnapshotManager snapshotManager = null;
                    ImmutableDictionary<Symbol, Symbol> remappedSymbols = null;
                    if (bodyBinder.Compilation.NullableSemanticAnalysisEnabled)
                    {
                        // Currently, we're passing an empty DiagnosticBag here because the flow analysis pass later will
                        // also run the nullable walker, and issue duplicate warnings. We should try to only run the pass
                        // once.
                        // https://github.com/dotnet/roslyn/issues/35041
                        methodBodyForSemanticModel = NullableWalker.AnalyzeAndRewrite(bodyBinder.Compilation, method, methodBody, bodyBinder, new DiagnosticBag(), createSnapshots: true, out snapshotManager, ref remappedSymbols);
                    }
                    forSemanticModel = new MethodBodySemanticModel.InitialState(syntaxNode, methodBodyForSemanticModel, bodyBinder, snapshotManager, remappedSymbols);

                    switch (methodBody.Kind)
                    {
                        case BoundKind.ConstructorMethodBody:
                            var constructor = (BoundConstructorMethodBody)methodBody;
                            body = constructor.BlockBody ?? constructor.ExpressionBody;

                            if (constructor.Initializer != null)
                            {
                                ReportCtorInitializerCycles(method, constructor.Initializer.Expression, compilationState, diagnostics);

                                if (body == null)
                                {
                                    body = new BoundBlock(constructor.Syntax, constructor.Locals, ImmutableArray.Create<BoundStatement>(constructor.Initializer));
                                }
                                else
                                {
                                    body = new BoundBlock(constructor.Syntax, constructor.Locals, ImmutableArray.Create<BoundStatement>(constructor.Initializer, body));
                                    originalBodyNested = true;
                                }

                                return body;
                            }
                            else
                            {
                                Debug.Assert(constructor.Locals.IsEmpty);
                            }
                            break;

                        case BoundKind.NonConstructorMethodBody:
                            var nonConstructor = (BoundNonConstructorMethodBody)methodBody;
                            body = nonConstructor.BlockBody ?? nonConstructor.ExpressionBody;
                            break;

                        case BoundKind.Block:
                            body = (BoundBlock)methodBody;
                            break;
                        default:
                            throw ExceptionUtilities.UnexpectedValue(methodBody.Kind);
                    }
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

            if (method.MethodKind == MethodKind.Destructor && body != null)
            {
                return MethodBodySynthesizer.ConstructDestructorBody(method, body);
            }

            var constructorInitializer = BindImplicitConstructorInitializerIfAny(method, compilationState, diagnostics);
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

        private static BoundStatement BindImplicitConstructorInitializerIfAny(MethodSymbol method, TypeCompilationState compilationState, DiagnosticBag diagnostics)
        {
            Debug.Assert(!method.ContainingType.IsDelegateType());

            // delegates have constructors but not constructor initializers
            if (method.MethodKind == MethodKind.Constructor && !method.IsExtern)
            {
                var compilation = method.DeclaringCompilation;
                var initializerInvocation = BindImplicitConstructorInitializer(method, diagnostics, compilation);

                if (initializerInvocation != null)
                {
                    ReportCtorInitializerCycles(method, initializerInvocation, compilationState, diagnostics);

                    //  Base WasCompilerGenerated state off of whether constructor is implicitly declared, this will ensure proper instrumentation.
                    var constructorInitializer = new BoundExpressionStatement(initializerInvocation.Syntax, initializerInvocation) { WasCompilerGenerated = method.IsImplicitlyDeclared };
                    Debug.Assert(initializerInvocation.HasAnyErrors || constructorInitializer.IsConstructorInitializer(), "Please keep this bound node in sync with BoundNodeExtensions.IsConstructorInitializer.");
                    return constructorInitializer;
                }
            }

            return null;
        }

        private static void ReportCtorInitializerCycles(MethodSymbol method, BoundExpression initializerInvocation, TypeCompilationState compilationState, DiagnosticBag diagnostics)
        {
            var ctorCall = initializerInvocation as BoundCall;
            if (ctorCall != null && !ctorCall.HasAnyErrors && ctorCall.Method != method && TypeSymbol.Equals(ctorCall.Method.ContainingType, method.ContainingType, TypeCompareKind.ConsiderEverything2))
            {
                // Detect and report indirect cycles in the ctor-initializer call graph.
                compilationState.ReportCtorInitializerCycles(method, ctorCall.Method, ctorCall.Syntax, diagnostics);
            }
        }

        /// <summary>
        /// Bind the implicit constructor initializer of a constructor symbol.
        /// </summary>
        /// <param name="constructor">Constructor method.</param>
        /// <param name="diagnostics">Accumulates errors (e.g. access "this" in constructor initializer).</param>
        /// <param name="compilation">Used to retrieve binder.</param>
        /// <returns>A bound expression for the constructor initializer call.</returns>
        internal static BoundExpression BindImplicitConstructorInitializer(
            MethodSymbol constructor, DiagnosticBag diagnostics, CSharpCompilation compilation)
        {
            // Note that the base type can be null if we're compiling System.Object in source.
            NamedTypeSymbol baseType = constructor.ContainingType.BaseTypeNoUseSiteDiagnostics;

            SourceMemberMethodSymbol sourceConstructor = constructor as SourceMemberMethodSymbol;
            Debug.Assert(((ConstructorDeclarationSyntax)sourceConstructor?.SyntaxNode)?.Initializer == null);

            // The common case is that the type inherits directly from object.
            // Also, we might be trying to generate a constructor for an entirely compiler-generated class such
            // as a closure class; in that case it is vexing to try to find a suitable binder for the non-existing
            // constructor syntax so that we can do unnecessary overload resolution on the non-existing initializer!
            // Simply take the early out: bind directly to the parameterless object ctor rather than attempting
            // overload resolution.
            if ((object)baseType != null)
            {
                if (baseType.SpecialType == SpecialType.System_Object)
                {
                    return GenerateBaseParameterlessConstructorInitializer(constructor, diagnostics);
                }
                else if (baseType.IsErrorType() || baseType.IsStatic)
                {
                    // If the base type is bad and there is no initializer then we can just bail.
                    // We have no expressions we need to analyze to report errors on.
                    return null;
                }
            }

            // Now, in order to do overload resolution, we're going to need a binder. There are
            // two possible situations:
            //
            // class D1 : B { }
            // class D2 : B { D2(int x) { } }
            //
            // In the first case the binder needs to be the binder associated with
            // the *body* of D1 because if the base class ctor is protected, we need
            // to be inside the body of a derived class in order for it to be in the
            // accessibility domain of the protected base class ctor.
            //
            // In the second case the binder could be the binder associated with 
            // the body of D2; since the implicit call to base() will have no arguments
            // there is no need to look up "x".
            Binder outerBinder;

            if ((object)sourceConstructor == null)
            {
                // The constructor is implicit. We need to get the binder for the body
                // of the enclosing class. 
                CSharpSyntaxNode containerNode = constructor.GetNonNullSyntaxNode();
                SyntaxToken bodyToken = GetImplicitConstructorBodyToken(containerNode);
                outerBinder = compilation.GetBinderFactory(containerNode.SyntaxTree).GetBinder(containerNode, bodyToken.Position);
            }
            else
            {
                // We have a ctor in source but no explicit constructor initializer.  We can't just use the binder for the
                // type containing the ctor because the ctor might be marked unsafe.  Use the binder for the parameter list
                // as an approximation - the extra symbols won't matter because there are no identifiers to bind.

                outerBinder = compilation.GetBinderFactory(sourceConstructor.SyntaxTree).GetBinder(((ConstructorDeclarationSyntax)sourceConstructor.SyntaxNode).ParameterList);
            }

            // wrap in ConstructorInitializerBinder for appropriate errors
            // Handle scoping for possible pattern variables declared in the initializer
            Binder initializerBinder = outerBinder.WithAdditionalFlagsAndContainingMemberOrLambda(BinderFlags.ConstructorInitializer, constructor);

            return initializerBinder.BindConstructorInitializer(null, constructor, diagnostics);
        }

        private static SyntaxToken GetImplicitConstructorBodyToken(CSharpSyntaxNode containerNode)
        {
            return ((BaseTypeDeclarationSyntax)containerNode).OpenBraceToken;
        }

        internal static BoundCall GenerateBaseParameterlessConstructorInitializer(MethodSymbol constructor, DiagnosticBag diagnostics)
        {
            NamedTypeSymbol baseType = constructor.ContainingType.BaseTypeNoUseSiteDiagnostics;
            MethodSymbol baseConstructor = null;
            LookupResultKind resultKind = LookupResultKind.Viable;
            Location diagnosticsLocation = constructor.Locations.IsEmpty ? NoLocation.Singleton : constructor.Locations[0];

            foreach (MethodSymbol ctor in baseType.InstanceConstructors)
            {
                if (ctor.ParameterCount == 0)
                {
                    baseConstructor = ctor;
                    break;
                }
            }

            // UNDONE: If this happens then something is deeply wrong. Should we give a better error?
            if ((object)baseConstructor == null)
            {
                diagnostics.Add(ErrorCode.ERR_BadCtorArgCount, diagnosticsLocation, baseType, /*desired param count*/ 0);
                return null;
            }

            if (Binder.ReportUseSiteDiagnostics(baseConstructor, diagnostics, diagnosticsLocation))
            {
                return null;
            }

            // UNDONE: If this happens then something is deeply wrong. Should we give a better error?
            bool hasErrors = false;
            HashSet<DiagnosticInfo> useSiteDiagnostics = null;
            if (!AccessCheck.IsSymbolAccessible(baseConstructor, constructor.ContainingType, ref useSiteDiagnostics))
            {
                diagnostics.Add(ErrorCode.ERR_BadAccess, diagnosticsLocation, baseConstructor);
                resultKind = LookupResultKind.Inaccessible;
                hasErrors = true;
            }

            if (!useSiteDiagnostics.IsNullOrEmpty())
            {
                diagnostics.Add(diagnosticsLocation, useSiteDiagnostics);
            }

            CSharpSyntaxNode syntax = constructor.GetNonNullSyntaxNode();

            BoundExpression receiver = new BoundThisReference(syntax, constructor.ContainingType) { WasCompilerGenerated = true };
            return new BoundCall(
                syntax: syntax,
                receiverOpt: receiver,
                method: baseConstructor,
                arguments: ImmutableArray<BoundExpression>.Empty,
                argumentNamesOpt: ImmutableArray<string>.Empty,
                argumentRefKindsOpt: ImmutableArray<RefKind>.Empty,
                isDelegateCall: false,
                expanded: false,
                invokedAsExtensionMethod: false,
                argsToParamsOpt: ImmutableArray<int>.Empty,
                resultKind: resultKind,
                binderOpt: null,
                type: baseConstructor.ReturnType,
                hasErrors: hasErrors)
            { WasCompilerGenerated = true };
        }

        private static void GenerateExternalMethodWarnings(SourceMemberMethodSymbol methodSymbol, DiagnosticBag diagnostics)
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
                SourceMemberMethodSymbol sourceMethod = method as SourceMemberMethodSymbol;
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
