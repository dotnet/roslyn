// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeGen;
using Microsoft.CodeAnalysis.CSharp.Emit;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Roslyn.Utilities;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal class MethodBodyCompiler : CSharpSymbolVisitor<TypeCompilationState, object>
    {
        // The moduleBeingBuilt can be null. This indicates that we are compiling the method bodies solely
        // for the purpose of getting diagnostics.
        private readonly CSharpCompilation compilation;
        private readonly bool generateDebugInfo;
        private readonly bool optimize;
        private readonly CancellationToken cancellationToken;
        private readonly DiagnosticBag diagnostics;
        private readonly bool hasDeclarationErrors;
        private readonly NamespaceScopeBuilder namespaceScopeBuilder;
        private readonly PEModuleBuilder moduleBeingBuilt;
        private readonly Predicate<Symbol> filter; // if not null, limit analysis to specific symbols
        private readonly DebugDocumentProvider debugDocumentProvider;

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
        private ConcurrentStack<Task> compilerTasks;

        // This field tracks whether any bound method body had hasErrors set or whether any constant field had a bad value.
        // We track it so that we can abort emission in the event that an error occurs without a corresponding diagnostic
        // (e.g. if this module depends on a bad type or constant from another module).
        // CONSIDER: instead of storing a flag, we could track the first member symbol with an error (to improve the diagnostic).


        // NOTE: once the flag is set to true, it should never go back to false!!!
        // Do not use this as a shortcircuiting for stages that might produce diagnostics.
        // That would make diagnostics to depend on the random order in which methods are compiled.
        private bool globalHasErrors;
        private bool GlobalHasErrors
        {
            get
            {
                return globalHasErrors;
            }
        }

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
                globalHasErrors = true;
            }
        }

        public static void CompileMethodBodies(
            CSharpCompilation compilation,
            PEModuleBuilder moduleBeingBuilt,
            bool generateDebugInfo,
            bool hasDeclarationErrors,
            DiagnosticBag diagnostics,
            Predicate<Symbol> filter,
            CancellationToken cancellationToken)
        {
            MethodBodyCompiler methodBodyCompiler = new MethodBodyCompiler(
                                                            compilation,
                                                            moduleBeingBuilt,
                                                            generateDebugInfo,
                                                            hasDeclarationErrors,
                                                            diagnostics,
                                                            filter,
                                                            cancellationToken);

            if (compilation.Options.ConcurrentBuild)
            {
                methodBodyCompiler.compilerTasks = new ConcurrentStack<Task>();
            }

            // directly traverse global namespace (no point to defer this to async)
            methodBodyCompiler.CompileNamespace(compilation.SourceModule.GlobalNamespace);
            methodBodyCompiler.WaitForWorkers();

            // compile additional and anonymous types if any
            if (moduleBeingBuilt != null)
            {
                var additionalTypes = moduleBeingBuilt.GetAdditionalTopLevelTypes();
                if (!additionalTypes.IsEmpty)
                {
                    methodBodyCompiler.CompileGeneratedMethods(additionalTypes, diagnostics);
                }

                // By this time we have processed all types reachable from module's global namespace
                compilation.AnonymousTypeManager.AssignTemplatesNamesAndCompile(methodBodyCompiler, moduleBeingBuilt, diagnostics);
                methodBodyCompiler.WaitForWorkers();

                var privateImplClass = moduleBeingBuilt.PrivateImplClass;
                if (privateImplClass != null)
                {
                    // all threads that were adding methods must be finished now, we can freeze the class:
                    privateImplClass.Freeze();

                    methodBodyCompiler.CompileGeneratedMethods(privateImplClass, diagnostics);
                }
            }

            // If we are trying to emit and there's an error without a corresponding diagnostic (e.g. because
            // we depend on an invalid type or constant from another module), then explicitly add a diagnostic.
            // This diagnostic is not very helpful to the user, but it will prevent us from emitting an invalid
            // module or crashing.
            if (moduleBeingBuilt != null && methodBodyCompiler.GlobalHasErrors && !diagnostics.HasAnyErrors() && !hasDeclarationErrors)
            {
                diagnostics.Add(ErrorCode.ERR_ModuleEmitFailure, NoLocation.Singleton, ((Microsoft.Cci.INamedEntity)moduleBeingBuilt).Name);
            }

            diagnostics.AddRange(compilation.AdditionalCodegenWarnings);

            // we can get unused field warnings only if compiling whole compilation.
            if (filter == null)
            {
                WarnUnusedFields(compilation, diagnostics, cancellationToken);
            }
        }

        private void WaitForWorkers()
        {
            var tasks = this.compilerTasks;
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

        // Internal for testing only.
        internal MethodBodyCompiler(CSharpCompilation compilation, PEModuleBuilder moduleBeingBuilt, bool generateDebugInfo, bool hasDeclarationErrors,
            DiagnosticBag diagnostics, Predicate<Symbol> filter, CancellationToken cancellationToken)
        {
            this.compilation = compilation;
            this.moduleBeingBuilt = moduleBeingBuilt;
            this.generateDebugInfo = generateDebugInfo;
            this.cancellationToken = cancellationToken;
            this.diagnostics = diagnostics;
            this.optimize = compilation.Options.Optimize;
            this.filter = filter;
            
            this.hasDeclarationErrors = hasDeclarationErrors;
            SetGlobalErrorIfTrue(hasDeclarationErrors);

            if (generateDebugInfo)
            {
                this.debugDocumentProvider = (path, basePath) => moduleBeingBuilt.GetOrAddDebugDocument(path, basePath, CreateDebugDocumentForFile);
                this.namespaceScopeBuilder = new NamespaceScopeBuilder(compilation);
            }
        }

        public override object VisitNamespace(NamespaceSymbol symbol, TypeCompilationState arg)
        {
            if ((this.filter != null) && !this.filter(symbol))
            {
                return null;
            }

            arg = null; // do not use compilation state of outer type.
            cancellationToken.ThrowIfCancellationRequested();

            if (compilation.Options.ConcurrentBuild)
            {
                Task worker = CompileNamespaceAsTask(symbol);
                compilerTasks.Push(worker);
            }
            else
            {
                CompileNamespace(symbol);
            }

            return null;
        }

        private Task CompileNamespaceAsTask(NamespaceSymbol symbol)
        {
            return Task.Run(() =>
                {
                    try
                    {
                        CompileNamespace(symbol);
                        return (object)null;
                    }
                    catch (Exception e) if (CompilerFatalError.ReportUnlessCanceled(e))
                    {
                        throw ExceptionUtilities.Unreachable;
                    }
                }, this.cancellationToken);
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
            if ((this.filter != null) && !this.filter(symbol))
            {
                return null;
            }

            arg = null; // do not use compilation state of outer type.
            cancellationToken.ThrowIfCancellationRequested();

            if (compilation.Options.ConcurrentBuild)
            {
                Task worker = CompileNamedTypeAsTask(symbol);
                compilerTasks.Push(worker);
            }
            else
            {
                CompileNamedType(symbol);
            }

            return null;
        }

        private Task CompileNamedTypeAsTask(NamedTypeSymbol symbol)
        {
            return Task.Run(() =>
                {
                    try
                    {
                        CompileNamedType(symbol);
                        return (object)null;
                    }
                    catch (Exception e) if (CompilerFatalError.Report(e))
                    {
                        throw ExceptionUtilities.Unreachable;
                    }
                }, this.cancellationToken);
        }

        private void CompileNamedType(NamedTypeSymbol symbol)
        {
            TypeCompilationState compilationState = new TypeCompilationState(symbol, moduleBeingBuilt);

            cancellationToken.ThrowIfCancellationRequested();

            // Find the constructor of a script class.
            MethodSymbol scriptCtor = null;
            if (symbol.IsScriptClass)
            {
                // The field initializers of a script class could be arbitrary statements,
                // including blocks.  Field initializers containing blocks need to
                // use a MethodBodySemanticModel to build up the appropriate tree of binders, and
                // MethodBodySemanticModel requires an "owning" method.  That's why we're digging out
                // the constructor - it will own the field initializers.
                scriptCtor = symbol.InstanceConstructors[0];
                Debug.Assert((object)scriptCtor != null);
            }

            var synthesizedSubmissionFields = symbol.IsSubmissionClass ? new SynthesizedSubmissionFields(compilation, symbol) : null;
            var processedStaticInitializers = new ProcessedFieldInitializers();
            var processedInstanceInitializers = new ProcessedFieldInitializers();

            var sourceTypeSymbol = symbol as SourceMemberContainerTypeSymbol;
            if ((object)sourceTypeSymbol != null)
            {
                BindFieldInitializers(sourceTypeSymbol, scriptCtor, sourceTypeSymbol.StaticInitializers, this.generateDebugInfo, ref processedStaticInitializers);
                BindFieldInitializers(sourceTypeSymbol, scriptCtor, sourceTypeSymbol.InstanceInitializers, this.generateDebugInfo, ref processedInstanceInitializers);

                if (compilationState.Emitting)
                {
                    CompileSynthesizedExplicitImplementations(sourceTypeSymbol, compilationState);
                }
            }

            // Indicates if a static constructor is in the member,
            // so we can decide to synthesize a static constructor.
            bool hasStaticConstructor = false;

            foreach (var member in symbol.GetMembers())
            {
                //When a filter is supplied, limit the compilation of members passing the filter.
                if ((this.filter != null) && !this.filter(member))
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
                            if (method.IsSubmissionConstructor || IsFieldLikeEventAccessor(method))
                            {
                                continue;
                            }

                            if (method.IsPartial())
                            {
                                if (method.IsPartialDefinition())
                                {
                                    method = method.PartialImplementation();
                                }
                                if ((object)method == null)
                                {
                                    continue;
                                }
                            }

                            ProcessedFieldInitializers processedInitializers =
                                method.MethodKind == MethodKind.Constructor ? processedInstanceInitializers :
                                method.MethodKind == MethodKind.StaticConstructor ? processedStaticInitializers :
                                default(ProcessedFieldInitializers);

                            CompileMethod(method, ref processedInitializers, synthesizedSubmissionFields, compilationState);

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
                                CompileFieldLikeEventAccessor(eventSymbol, isAddMethod: true, compilationState: compilationState);
                                CompileFieldLikeEventAccessor(eventSymbol, isAddMethod: false, compilationState: compilationState);
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
                                    TypeSymbol discarded = fieldSymbol.FixedImplementationType(compilationState.ModuleBuilder);
                                }
                            }
                            break;
                        }
                }
            }

            Debug.Assert(symbol.TypeKind != TypeKind.Submission || ((object)scriptCtor != null && scriptCtor.IsSubmissionConstructor));

            //  process additional anonymous type members
            if (AnonymousTypeManager.IsAnonymousTypeTemplate(symbol))
            {
                ProcessedFieldInitializers processedInitializers = default(ProcessedFieldInitializers);
                foreach (var method in AnonymousTypeManager.GetAnonymousTypeHiddenMethods(symbol))
                {
                    CompileMethod(method, ref processedInitializers, synthesizedSubmissionFields, compilationState);
                }
            }

            // In the case there are field initializers but we haven't created an implicit static constructor (.cctor) for it,
            // (since we may not add .cctor implicitly created for decimals into the symbol table)
            // it is necessary for the compiler to generate the static constructor here if we are emitting.
            if (moduleBeingBuilt != null && !hasStaticConstructor && !processedStaticInitializers.BoundInitializers.IsDefaultOrEmpty)
            {
                Debug.Assert(processedStaticInitializers.BoundInitializers.All((init) =>
                    (init.Kind == BoundKind.FieldInitializer) && !((BoundFieldInitializer)init).Field.IsMetadataConstant));

                MethodSymbol method = new SynthesizedStaticConstructor(sourceTypeSymbol);
                if ((this.filter == null) || this.filter(method))
                {
                    CompileMethod(method, ref processedStaticInitializers, synthesizedSubmissionFields, compilationState);
                    // If this method has been successfully built, we emit it.
                    if (moduleBeingBuilt.GetMethodBody(method) != null)
                        moduleBeingBuilt.AddCompilerGeneratedDefinition(sourceTypeSymbol, method);
                }
            }

            // compile submission constructor last so that synthesized submission fields are collected from all script methods:
            if (synthesizedSubmissionFields != null && compilationState.Emitting)
            {
                Debug.Assert(scriptCtor.IsSubmissionConstructor);
                CompileMethod(scriptCtor, ref processedInstanceInitializers, synthesizedSubmissionFields, compilationState);
                synthesizedSubmissionFields.AddToType(scriptCtor.ContainingType, compilationState.ModuleBuilder);
            }

            //  Emit synthesized methods produced during lowering if any
            CompileGeneratedMethods(compilationState);
            compilationState.Free();
        }

        private void CompileGeneratedMethods(PrivateImplementationDetails privateImplClass, DiagnosticBag diagnostics)
        {
            TypeCompilationState compilationState = new TypeCompilationState(null, moduleBeingBuilt);
            foreach (MethodSymbol method in privateImplClass.GetMethods(new Microsoft.CodeAnalysis.Emit.Context(moduleBeingBuilt, null, diagnostics)))
            {
                Debug.Assert(method.SynthesizesLoweredBoundBody);
                method.GenerateMethodBody(compilationState, diagnostics);
            }

            CompileGeneratedMethods(compilationState);
            compilationState.Free();
        }

        private void CompileGeneratedMethods(ImmutableArray<NamedTypeSymbol> additionalTypes, DiagnosticBag diagnostics)
        {
            TypeCompilationState compilationState = new TypeCompilationState(null, moduleBeingBuilt);
            foreach (var type in additionalTypes)
            {
                foreach (var method in type.GetMethodsToEmit())
                {
                    method.GenerateMethodBody(compilationState, diagnostics);
                }
            }

            if (!diagnostics.HasAnyErrors())
            {
                CompileGeneratedMethods(compilationState);
            }
            compilationState.Free();
        }

        private void CompileGeneratedMethods(TypeCompilationState compilationState)
        {
            if (compilationState.AnyGeneratedMethods)
            {
                foreach (var methodWithBody in compilationState.GeneratedMethods)
                {
                    // We make sure that an asynchronous mutation to the diagnostic bag does not 
                    // confuse the method body generator by making a fresh bag and then loading
                    // any diagnostics emitted into it back into the main diagnostic bag.
                    var diagnosticsThisMethod = DiagnosticBag.GetInstance();

                    var method = methodWithBody.Method;
                    BoundStatement bodyWithoutAsync = AsyncRewriter2.Rewrite(methodWithBody.Body, method, compilationState, diagnosticsThisMethod, generateDebugInfo);

                    MethodBody emittedBody = null;

                    if (!diagnosticsThisMethod.HasAnyErrors() && !globalHasErrors)
                    {
                        emittedBody = Compiler.GenerateMethodBody(
                            compilationState,
                            method,
                            bodyWithoutAsync,
                            diagnosticsThisMethod,
                            optimize,
                            debugDocumentProvider,
                            GetNamespaceScopes(method, methodWithBody.DebugImports));
                    }

                    this.diagnostics.AddRange(diagnosticsThisMethod);
                    diagnosticsThisMethod.Free();

                    // error while generating IL
                    if (emittedBody == null)
                    {
                        break;
                    }

                    moduleBeingBuilt.SetMethodBody(method, emittedBody);
                }
            }
        }

        private ImmutableArray<NamespaceScope> GetNamespaceScopes(MethodSymbol method, ConsList<Imports> debugImports)
        {
            Debug.Assert(generateDebugInfo == (namespaceScopeBuilder != null));

            return (generateDebugInfo && method.GenerateDebugInfo) ?
                namespaceScopeBuilder.GetNamespaceScopes(debugImports) : default(ImmutableArray<NamespaceScope>);
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
            // we are not generating any observable diagnostics here so it is ok to shortcircuit on global errors.
            if (!GlobalHasErrors)
            {
                foreach (var synthesizedExplicitImpl in sourceTypeSymbol.GetSynthesizedExplicitImplementations(cancellationToken))
                {
                    Debug.Assert(synthesizedExplicitImpl.SynthesizesLoweredBoundBody);
                    var discardedDiagnostics = DiagnosticBag.GetInstance();
                    synthesizedExplicitImpl.GenerateMethodBody(compilationState, discardedDiagnostics);
                    Debug.Assert(!discardedDiagnostics.HasAnyErrors());
                    discardedDiagnostics.Free();
                    moduleBeingBuilt.AddCompilerGeneratedDefinition(sourceTypeSymbol, synthesizedExplicitImpl);
                }
            }
        }

        private void CompileSynthesizedSealedAccessors(SourcePropertySymbol sourceProperty, TypeCompilationState compilationState)
        {
            SynthesizedSealedPropertyAccessor synthesizedAccessor = sourceProperty.SynthesizedSealedAccessorOpt;

            // we are not generating any observable diagnostics here so it is ok to shortcircuit on global errors.
            if ((object)synthesizedAccessor != null && !GlobalHasErrors)
            {
                Debug.Assert(synthesizedAccessor.SynthesizesLoweredBoundBody);
                var discardedDiagnostics = DiagnosticBag.GetInstance();
                synthesizedAccessor.GenerateMethodBody(compilationState, discardedDiagnostics);
                Debug.Assert(!discardedDiagnostics.HasAnyErrors());
                discardedDiagnostics.Free();

                moduleBeingBuilt.AddCompilerGeneratedDefinition(sourceProperty.ContainingType, synthesizedAccessor);
            }
        }

        private void CompileFieldLikeEventAccessor(SourceEventSymbol eventSymbol, bool isAddMethod, TypeCompilationState compilationState)
        {
            MethodSymbol accessor = isAddMethod ? eventSymbol.AddMethod : eventSymbol.RemoveMethod;

            var diagnosticsThisMethod = DiagnosticBag.GetInstance();
            try
            {
                BoundBlock boundBody = MethodBodySynthesizer.ConstructFieldLikeEventAccessorBody(eventSymbol, isAddMethod, compilation, diagnosticsThisMethod);
                var hasErrors = diagnosticsThisMethod.HasAnyErrors();
                SetGlobalErrorIfTrue(hasErrors);

                // we cannot rely on GlobalHasErrors since that can be changed concurrently by other methods compiling
                // we however do not want to continue with generating method body if we have errors in this particular method - generating may crash
                // or if had declaration errors - we will fail anyways, but if some types are bad enough, generating may produce duplicate errors about that.
                if (!hasErrors && !hasDeclarationErrors)
                {
                    MethodBody emittedBody = Compiler.GenerateMethodBody(
                        compilationState,
                        accessor,
                        boundBody,
                        diagnosticsThisMethod,
                        optimize,
                        debugDocumentProvider,
                        default(ImmutableArray<NamespaceScope>));

                    moduleBeingBuilt.SetMethodBody(accessor, emittedBody);
                    // Definition is already in the symbol table, so don't call moduleBeingBuilt.AddCompilerGeneratedDefinition
                }
            }
            finally
            {
                diagnostics.AddRange(diagnosticsThisMethod);
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

        private void BindFieldInitializers(
            SourceMemberContainerTypeSymbol typeSymbol,
            MethodSymbol scriptCtor,
            ImmutableArray<ImmutableArray<FieldInitializer>> fieldInitializers,
            bool generateDebugInfo,
            ref ProcessedFieldInitializers processedInitializers) //by ref so that we can store the results of lowering
        {
            DiagnosticBag diagsForInstanceInitializers = DiagnosticBag.GetInstance();
            try
            {
                cancellationToken.ThrowIfCancellationRequested();

                ConsList<Imports> firstDebugImports;
                processedInitializers.BoundInitializers = Compiler.BindFieldInitializers(typeSymbol, scriptCtor, fieldInitializers,
                    diagsForInstanceInitializers, generateDebugInfo, out firstDebugImports);
                processedInitializers.HasErrors = diagsForInstanceInitializers.HasAnyErrors();
                processedInitializers.FirstDebugImports = firstDebugImports;
            }
            finally
            {
                this.diagnostics.AddRange(diagsForInstanceInitializers);
                diagsForInstanceInitializers.Free();
            }
        }

        //TODO: it might be nice to make this a static method on Compiler
        private void CompileMethod(
            MethodSymbol methodSymbol,
            ref ProcessedFieldInitializers processedInitializers,
            SynthesizedSubmissionFields previousSubmissionFields,
            TypeCompilationState compilationState)
        {
            cancellationToken.ThrowIfCancellationRequested();
            SourceMethodSymbol sourceMethod = methodSymbol as SourceMethodSymbol;

            if (methodSymbol.IsAbstract)
            {
                if ((object)sourceMethod != null)
                {
                    bool diagsWritten;
                    sourceMethod.SetDiagnostics(ImmutableArray<Diagnostic>.Empty, out diagsWritten);
                    if (diagsWritten && !methodSymbol.IsImplicitlyDeclared && compilation.EventQueue != null)
                    {
                        compilation.SymbolDeclaredEvent(methodSymbol);
                    }
                }

                return;
            }

            // get cached diagnostics if not building and we have 'em
            bool calculateDiagnosticsOnly = moduleBeingBuilt == null;
            if (calculateDiagnosticsOnly && ((object)sourceMethod != null))
            {
                var cachedDiagnostics = sourceMethod.Diagnostics;

                if (!cachedDiagnostics.IsDefault)
                {
                    this.diagnostics.AddRange(cachedDiagnostics);
                    return;
                }
            }

            ConsList<Imports> oldDebugImports = compilationState.CurrentDebugImports;

            // In order to avoid generating code for methods with errors, we create a diagnostic bag just for this method.
            DiagnosticBag diagsForCurrentMethod = DiagnosticBag.GetInstance();

            try
            {
                bool includeInitializersInBody;
                BoundBlock body;

                // if synthesized method returns its body in lowered form
                if (methodSymbol.SynthesizesLoweredBoundBody)
                {
                    if (moduleBeingBuilt != null)
                    {
                        methodSymbol.GenerateMethodBody(compilationState, diagsForCurrentMethod);
                        this.diagnostics.AddRange(diagsForCurrentMethod);
                    }

                    return;
                }

                //EDMAURER initializers that have been analyzed but not yet lowered.
                BoundStatementList analyzedInitializers = null;

                ConsList<Imports> debugImports;

                if (methodSymbol.IsScriptConstructor)
                {
                    // rewrite top-level statements and script variable declarations to a list of statements and assignments, respectively:
                    BoundStatementList initializerStatements = InitializerRewriter.Rewrite(processedInitializers.BoundInitializers, methodSymbol);

                    // the lowered script initializers should not be treated as initializers anymore but as a method body:
                    body = new BoundBlock(initializerStatements.Syntax, ImmutableArray<LocalSymbol>.Empty, initializerStatements.Statements) { WasCompilerGenerated = true };
                    includeInitializersInBody = false;

                    debugImports = null;
                }
                else
                {
                    // do not emit initializers if we are invoking another constructor of this class:
                    includeInitializersInBody = !processedInitializers.BoundInitializers.IsDefaultOrEmpty && !HasThisConstructorInitializer(methodSymbol);

                    // lower initializers just once. the lowered tree will be reused when emitting all constructors 
                    // with field initializers. Once lowered, these initializers will be stashed in processedInitializers.LoweredInitializers
                    // (see later in this method). Don't bother lowering _now_ if this particular ctor won't have the initializers 
                    // appended to its body.
                    if (includeInitializersInBody && processedInitializers.LoweredInitializers == null)
                    {
                        analyzedInitializers = InitializerRewriter.Rewrite(processedInitializers.BoundInitializers, methodSymbol);
                        processedInitializers.HasErrors = processedInitializers.HasErrors || analyzedInitializers.HasAnyErrors;

                        // These analyses check for diagnostics in lambdas.
                        // Control flow analysis and implicit return insertion are unnecessary.
                        DataFlowPass.Analyze(compilation, methodSymbol, analyzedInitializers, diagsForCurrentMethod, requireOutParamsAssigned: false);
                        DiagnosticsPass.IssueDiagnostics(compilation, analyzedInitializers, diagsForCurrentMethod, methodSymbol);
                    }

                    body = Compiler.BindMethodBody(methodSymbol, diagsForCurrentMethod, this.generateDebugInfo, out debugImports);
                }

#if DEBUG
                // If the method is a synthesized static or instance constructor, then debugImports will be null and we will use the value
                // from the first field initializer.
                if (this.generateDebugInfo)
                {
                    if ((methodSymbol.MethodKind == MethodKind.Constructor || methodSymbol.MethodKind == MethodKind.StaticConstructor) && methodSymbol.IsImplicitlyDeclared)
                    {
                        // There was no body to bind, so we didn't get anything from Compiler.BindMethodBody.
                        Debug.Assert(debugImports == null);
                        // Either there were no field initializers or we grabbed debug imports from the first one.
                        Debug.Assert(processedInitializers.BoundInitializers.IsDefaultOrEmpty || processedInitializers.FirstDebugImports != null);
                    }
                }
#endif

                debugImports = debugImports ?? processedInitializers.FirstDebugImports;

                // Associate these debug imports with all methods generated from this one.
                compilationState.CurrentDebugImports = debugImports;

                if (body != null && methodSymbol is SourceMethodSymbol)
                {
                    // TODO: Do we need to issue warnings for non-SourceMethodSymbol methods, like synthesized ctors?
                    DiagnosticsPass.IssueDiagnostics(compilation, body, diagsForCurrentMethod, methodSymbol);
                }

                BoundBlock flowAnalyzedBody = null;
                if (body != null)
                {
                    flowAnalyzedBody = FlowAnalysisPass.Rewrite(methodSymbol, body, diagsForCurrentMethod);
                }

                bool hasErrors = hasDeclarationErrors || diagsForCurrentMethod.HasAnyErrors() || processedInitializers.HasErrors;

                // Record whether or not the bound tree for the lowered method body (including any initializers) contained any
                // errors (note: errors, not diagnostics).
                SetGlobalErrorIfTrue(hasErrors);

                bool diagsWritten = false;
                var actualDiagnostics = diagsForCurrentMethod.ToReadOnly();
                if (sourceMethod != null) actualDiagnostics = sourceMethod.SetDiagnostics(actualDiagnostics, out diagsWritten);
                if (diagsWritten && !methodSymbol.IsImplicitlyDeclared && compilation.EventQueue != null)
                {
                    var lazySemanticModel = body == null ? null : new Lazy<SemanticModel>(() =>
                    {
                        var syntax = body.Syntax;
                        var semanticModel = (CSharpSemanticModel)compilation.GetSemanticModel(syntax.SyntaxTree);
                        var memberModel = semanticModel.GetMemberModel(syntax);
                        if (memberModel != null)
                        {
                            memberModel.AddBoundTreeForStandaloneSyntax(syntax, body);
                        }
                        return semanticModel;
                    });
                    compilation.EventQueue.Enqueue(new CompilationEvent.SymbolDeclared(compilation, methodSymbol, lazySemanticModel));
                }

                // Don't lower if we're not emitting or if there were errors. 
                // Methods that had binding errors are considered too broken to be lowered reliably.
                if (calculateDiagnosticsOnly || hasErrors)
                {
                    this.diagnostics.AddRange(actualDiagnostics);
                    return;
                }

                // ############################
                // LOWERING AND EMIT
                // Any errors generated below here are considered Emit diagnostics 
                // and will not be reported to callers Compilation.GetDiagnostics()

                BoundStatement loweredBody = (flowAnalyzedBody == null) ? null :
                    Compiler.LowerStatement(this.generateDebugInfo, methodSymbol, flowAnalyzedBody, previousSubmissionFields, compilationState, diagsForCurrentMethod);

                bool hasBody = loweredBody != null;

                hasErrors = hasErrors || (hasBody && loweredBody.HasErrors) || diagsForCurrentMethod.HasAnyErrors();
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
                        boundStatements = MethodBodySynthesizer.ConstructScriptConstructorBody(loweredBody, methodSymbol, previousSubmissionFields, compilation);
                    }
                    else
                    {
                        boundStatements = ImmutableArray<BoundStatement>.Empty;

                        if (analyzedInitializers != null)
                        {
                            processedInitializers.LoweredInitializers = (BoundStatementList)Compiler.LowerStatement(
                                this.generateDebugInfo,
                                methodSymbol,
                                analyzedInitializers,
                                previousSubmissionFields,
                                compilationState,
                                diagsForCurrentMethod);

                            Debug.Assert(!hasErrors);
                            hasErrors = processedInitializers.LoweredInitializers.HasAnyErrors || diagsForCurrentMethod.HasAnyErrors();
                            SetGlobalErrorIfTrue(hasErrors);

                            if (hasErrors)
                            {
                                this.diagnostics.AddRange(diagsForCurrentMethod);
                                return;
                            }
                        }

                        // initializers for global code have already been included in the body
                        if (includeInitializersInBody)
                        {
                            //TODO: rewrite any BoundThis and BoundBase nodes in the initializers to have the correct ThisParameter symbol
                            if (compilation.Options.Optimize)
                            {
                                // TODO: this part may conflict with InitializerRewriter.Rewrite in how it handles 
                                //       the first field initializer (see 'if (i == 0)'...) which seems suspicious
                                ArrayBuilder<BoundStatement> statements = ArrayBuilder<BoundStatement>.GetInstance();
                                statements.AddRange(boundStatements);
                                bool anyNonDefault = false;

                                foreach (var initializer in processedInitializers.LoweredInitializers.Statements)
                                {
                                    if (ShouldOptimizeOutInitializer(initializer))
                                    {
                                        if (methodSymbol.IsStatic)
                                        {
                                            // NOTE: Dev11 removes static initializers if ONLY all of them are optimized out
                                            statements.Add(initializer);
                                        }
                                    }
                                    else
                                    {
                                        statements.Add(initializer);
                                        anyNonDefault = true;
                                    }
                                }

                                if (anyNonDefault)
                                {
                                    boundStatements = statements.ToImmutableAndFree();
                                }
                                else
                                {
                                    statements.Free();
                                }
                            }
                            else
                            {
                                boundStatements = boundStatements.Concat(processedInitializers.LoweredInitializers.Statements);
                            }
                        }

                        if (hasBody)
                        {
                            boundStatements = boundStatements.Concat(ImmutableArray.Create(loweredBody));
                        }
                    }

                    CSharpSyntaxNode syntax = methodSymbol.GetNonNullSyntaxNode();

                    var boundBody = BoundStatementList.Synthesized(syntax, boundStatements);

                    var emittedBody = Compiler.GenerateMethodBody(
                        compilationState,
                        methodSymbol,
                        boundBody,
                        diagsForCurrentMethod,
                        optimize,
                        debugDocumentProvider,
                        GetNamespaceScopes(methodSymbol, debugImports));

                    moduleBeingBuilt.SetMethodBody(methodSymbol, emittedBody);
                }

                this.diagnostics.AddRange(diagsForCurrentMethod);
            }
            finally
            {
                diagsForCurrentMethod.Free();
                compilationState.CurrentDebugImports = oldDebugImports;
            }
        }

        /// <summary>
        /// Returns true if the initializer is a field initializer which should be optimized out
        /// </summary>
        private static bool ShouldOptimizeOutInitializer(BoundStatement initializer)
        {
            BoundStatement statement = initializer;

            if (initializer.Kind == BoundKind.SequencePointWithSpan)
            {
                statement = ((BoundSequencePointWithSpan)initializer).StatementOpt;
            }
            else if (initializer.Kind == BoundKind.SequencePoint)
            {
                statement = ((BoundSequencePoint)initializer).StatementOpt;
            }

            if (statement == null || statement.Kind != BoundKind.ExpressionStatement)
            {
                Debug.Assert(false, "initializer does not initialize a field?");
                return false;
            }

            BoundAssignmentOperator assignment = ((BoundExpressionStatement)statement).Expression as BoundAssignmentOperator;
            if (assignment == null)
            {
                Debug.Assert(false, "initializer does not initialize a field?");
                return false;
            }

            Debug.Assert(assignment.Left.Kind == BoundKind.FieldAccess);

            BoundExpression rhs = assignment.Right;
            return rhs.IsDefaultValue();
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
                            return initializerSyntax.Kind == SyntaxKind.ThisConstructorInitializer;
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

        private struct ProcessedFieldInitializers
        {
            internal ImmutableArray<BoundInitializer> BoundInitializers { get; set; }
            internal BoundStatementList LoweredInitializers { get; set; }
            internal bool HasErrors { get; set; }
            internal ConsList<Imports> FirstDebugImports { get; set; }
        }
    }
}
