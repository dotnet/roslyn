// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Cci;
using Microsoft.CodeAnalysis.CodeGen;
using Microsoft.CodeAnalysis.CSharp.Emit;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Instrumentation;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal static class Compiler
    {
        internal static readonly ParallelOptions defaultParallelOptions = new ParallelOptions();

        // Do the steps in compilation to get the method body diagnostics, but don't actually generate
        // IL or emit an assembly.
        public static ImmutableArray<Diagnostic> GetAllMethodBodyDiagnostics(CSharpCompilation compilation, CancellationToken cancellationToken)
        {
            DiagnosticBag diagnostics = DiagnosticBag.GetInstance();
            CompileMethodBodies(
                compilation: compilation, 
                moduleBeingBuilt: null, 
                generateDebugInfo: false, 
                hasDeclarationErrors: false, 
                filter: null, 
                filterTree: null, 
                filterSpanWithinTree: null,
                diagnostics: diagnostics, 
                cancellationToken: cancellationToken);
            DocumentationCommentCompiler.WriteDocumentationCommentXml(compilation, null, null, diagnostics, cancellationToken);
            compilation.ReportUnusedImports(diagnostics, cancellationToken);
            return diagnostics.ToReadOnlyAndFree();
        }

        internal static ImmutableArray<Diagnostic> GetMethodBodyDiagnosticsForTree(CSharpCompilation compilation, SyntaxTree tree, TextSpan? span, CancellationToken cancellationToken)
        {
            DiagnosticBag diagnostics = DiagnosticBag.GetInstance();
            CompileMethodBodies(
                compilation: compilation,
                moduleBeingBuilt: null,
                generateDebugInfo: false,
                hasDeclarationErrors: false,
                filter: null,
                filterTree: tree,
                filterSpanWithinTree: span,
                diagnostics: diagnostics,
                cancellationToken: cancellationToken);
            DocumentationCommentCompiler.WriteDocumentationCommentXml(compilation, null, null, diagnostics, cancellationToken, tree, span);

            // Report unused directives only if computing diagnostics for the entire tree.
            // Otherwise we cannot determine if a particular directive is used outside of the given sub-span within the tree.
            if (!span.HasValue || span.Value == tree.GetRoot(cancellationToken).FullSpan)
            {
                compilation.ReportUnusedImports(diagnostics, cancellationToken, tree);
            }

            return diagnostics.ToReadOnlyAndFree();
        }

        public static void CompileMethodBodies(
            CSharpCompilation compilation,
            PEModuleBuilder moduleBeingBuilt,
            bool generateDebugInfo,
            bool hasDeclarationErrors,
            Predicate<Symbol> filter,
            SyntaxTree filterTree,
            TextSpan? filterSpanWithinTree,
            DiagnosticBag diagnostics,
            CancellationToken cancellationToken)
        {
            using (Logger.LogBlock(FunctionId.CSharp_Compiler_CompileMethodBodies, message: compilation.AssemblyName, cancellationToken: cancellationToken))
            {
                Debug.Assert(filter == null || filterTree == null, "Cannot provide both a filter predicate and a filter tree.");

                if (filter == null && filterTree != null)
                {
                    filter = s => s.IsDefinedInSourceTree(filterTree, filterSpanWithinTree);
                }

                if (compilation.PreviousSubmission != null)
                {
                    // In case there is a previous submission, we should ensure 
                    // it has already created anonymous type/delegates templates

                    // NOTE: if there are any errors, we will pick up what was created anyway
                    compilation.PreviousSubmission.EnsureAnonymousTypeTemplates(cancellationToken);

                    // TODO: revise to use a loop instead of a recursion
                }

                MethodBodyCompiler.CompileMethodBodies(compilation, moduleBeingBuilt, generateDebugInfo, hasDeclarationErrors, diagnostics, filter, cancellationToken);

                MethodSymbol entryPoint = GetEntryPoint(compilation, moduleBeingBuilt, hasDeclarationErrors, diagnostics, cancellationToken);
                if (moduleBeingBuilt != null)
                {
                    moduleBeingBuilt.SetEntryPoint(entryPoint);
                }
            }
        }

        /// <summary>
        /// Traverse the symbol table and call Module.AddCompilerGeneratedDefinition for each
        /// synthesized explicit implementation stub that has been generated (e.g. when the real
        /// implementation doesn't have the appropriate custom modifiers).
        /// </summary>
        public static void CompileSynthesizedMethodMetadata(
            CSharpCompilation compilation,
            PEModuleBuilder moduleBeingBuilt,
            CancellationToken cancellationToken)
        {
            using (Logger.LogBlock(FunctionId.CSharp_Compiler_CompileSynthesizedMethodMetadata, message: compilation.AssemblyName, cancellationToken: cancellationToken))
            {
                var synthesizedMethodCompiler = new SynthesizedMethodMetadataCompiler(moduleBeingBuilt, cancellationToken);
                synthesizedMethodCompiler.Visit(compilation.SourceModule.GlobalNamespace);
            }
        }

        internal static MethodSymbol GetEntryPoint(CSharpCompilation compilation, PEModuleBuilder moduleBeingBuilt, bool hasDeclarationErrors, DiagnosticBag diagnostics, CancellationToken cancellationToken)
        {
            CSharpCompilationOptions options = compilation.Options;
            if (!options.OutputKind.IsApplication())
            {
                Debug.Assert(compilation.GetEntryPointAndDiagnostics(cancellationToken) == null);
                return compilation.IsSubmission
                    ? DefineScriptEntryPoint(compilation, moduleBeingBuilt, compilation.GetSubmissionReturnType(), hasDeclarationErrors, diagnostics)
                    : null;
            }

            Debug.Assert(!compilation.IsSubmission);
            Debug.Assert(options.OutputKind.IsApplication());

            CSharpCompilation.EntryPoint entryPoint = compilation.GetEntryPointAndDiagnostics(cancellationToken);
            Debug.Assert(entryPoint != null);
            Debug.Assert(!entryPoint.Diagnostics.IsDefault);

            diagnostics.AddRange(entryPoint.Diagnostics);

            if ((object)compilation.ScriptClass != null)
            {
                Debug.Assert((object)entryPoint.MethodSymbol == null);
                return DefineScriptEntryPoint(compilation, moduleBeingBuilt, compilation.GetSpecialType(SpecialType.System_Void), hasDeclarationErrors, diagnostics);
            }

            Debug.Assert((object)entryPoint.MethodSymbol != null || entryPoint.Diagnostics.HasAnyErrors() || !compilation.Options.Errors.IsDefaultOrEmpty);
            return entryPoint.MethodSymbol;
        }

        internal static MethodBody GenerateMethodBody(TypeCompilationState compilationState, MethodSymbol method, BoundStatement block, DiagnosticBag diagnostics,
            bool optimize, DebugDocumentProvider debugDocumentProvider, ImmutableArray<NamespaceScope> namespaceScopes)
        {
            // Note: don't call diagnostics.HasAnyErrors() in release; could be expensive if compilation has many warnings.
            Debug.Assert(!diagnostics.HasAnyErrors(), "Running code generator when errors exist might be dangerous; code generator not well hardened");

            bool emitSequencePoints = !namespaceScopes.IsDefault && !method.IsAsync;
            var module = compilationState.ModuleBuilder;
            var compilation = module.Compilation;
            var localSlotManager = module.CreateLocalSlotManager(method);

            ILBuilder builder = new ILBuilder(module, localSlotManager, optimize);
            DiagnosticBag diagnosticsForThisMethod = DiagnosticBag.GetInstance();
            try
            {
                AsyncMethodBodyDebugInfo asyncDebugInfo = null;
                if ((object)method.AsyncKickoffMethod == null) // is this the MoveNext of an async method?
                {
                    CodeGen.CodeGenerator.Run(
                        method, block, builder, module, diagnosticsForThisMethod, optimize, emitSequencePoints);
                }
                else
                {
                    int asyncCatchHandlerOffset;
                    ImmutableArray<int> asyncYieldPoints;
                    ImmutableArray<int> asyncResumePoints;
                    CodeGen.CodeGenerator.Run(
                        method, block, builder, module, diagnosticsForThisMethod, optimize, emitSequencePoints,
                        out asyncCatchHandlerOffset, out asyncYieldPoints, out asyncResumePoints);
                    asyncDebugInfo = new AsyncMethodBodyDebugInfo(method.AsyncKickoffMethod, asyncCatchHandlerOffset, asyncYieldPoints, asyncResumePoints);
                }

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
                if (module.SaveTestData)
                {
                    module.SetMethodTestData(method, builder.GetSnapshot());
                }

                // Only compiler-generated MoveNext methods have iterator scopes.  See if this is one.
                bool hasIteratorScopes =
                    method.Locations.IsEmpty && method.Name == "MoveNext" &&
                    (method.ExplicitInterfaceImplementations.Contains(compilation.GetSpecialTypeMember(SpecialMember.System_Collections_IEnumerator__MoveNext) as MethodSymbol) ||
                     method.ExplicitInterfaceImplementations.Contains(compilation.GetWellKnownTypeMember(WellKnownMember.System_Runtime_CompilerServices_IAsyncStateMachine_MoveNext) as MethodSymbol));

                var iteratorScopes = hasIteratorScopes ? builder.GetIteratorScopes() : ImmutableArray<LocalScope>.Empty;

                var iteratorOrAsyncImplementation = compilationState.GetIteratorOrAsyncImplementationClass(method);
                return new MethodBody(
                    builder.RealizedIL,
                    builder.MaxStack,
                    method,
                    localVariables,
                    builder.RealizedSequencePoints,
                    debugDocumentProvider,
                    builder.RealizedExceptionHandlers,
                    builder.GetAllScopes(),
                    Microsoft.Cci.CustomDebugInfoKind.CSharpStyle,
                    builder.HasDynamicLocal,
                    namespaceScopes,
                    (object)iteratorOrAsyncImplementation == null ? null : iteratorOrAsyncImplementation.MetadataName,
                    iteratorScopes,
                    asyncMethodDebugInfo: asyncDebugInfo
                );
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

        #region Lowering

        internal static BoundStatement LowerStatement(
            bool generateDebugInfo,
            MethodSymbol method,
            BoundStatement body,
            SynthesizedSubmissionFields previousSubmissionFields,
            TypeCompilationState compilationState,
            DiagnosticBag diagnostics)
        {
            if (body.HasErrors)
            {
                return body;
            }

            bool sawLambdas;
            bool sawDynamicOperations;
            bool sawAwaitInExceptionHandler;
            var loweredBody = LocalRewriter.Rewrite(
                method.DeclaringCompilation,
                generateDebugInfo,
                method,
                method.ContainingType,
                body,
                compilationState,
                diagnostics,
                previousSubmissionFields,
                out sawLambdas,
                out sawDynamicOperations,
                out sawAwaitInExceptionHandler);

            if (sawDynamicOperations && compilationState.ModuleBuilder.IsENCDelta)
            {
                // Dynamic operations are not supported in ENC.
                var location = method.Locations[0];
                diagnostics.Add(new CSDiagnosticInfo(ErrorCode.ERR_EnCNoDynamicOperation), location);
            }

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
                Debug.Assert(method.IteratorElementType == null);
                loweredBody = AsyncHandlerRewriter.Rewrite(
                    generateDebugInfo,
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

            BoundStatement bodyWithoutLambdas = loweredBody;
            if (sawLambdas)
            {
                LambdaRewriter.Analysis lambdaAnalysis = LambdaRewriter.Analysis.Analyze(loweredBody, method, out sawLambdas);
                if (sawLambdas)
                {
                    bodyWithoutLambdas = LambdaRewriter.Rewrite(loweredBody, method.ContainingType, method.ThisParameter, method, compilationState, diagnostics, lambdaAnalysis, generateDebugInfo);
                }
            }

            if (bodyWithoutLambdas.HasErrors)
            {
                return bodyWithoutLambdas;
            }

            BoundStatement bodyWithoutIterators = IteratorRewriter.Rewrite(bodyWithoutLambdas, method, compilationState, diagnostics, generateDebugInfo);

            if (bodyWithoutIterators.HasErrors)
            {
                return bodyWithoutIterators;
            }

            BoundStatement bodyWithoutAsync = AsyncRewriter2.Rewrite(bodyWithoutIterators, method, compilationState, diagnostics, generateDebugInfo);

            return bodyWithoutAsync;
        }

        #endregion Lowering

        #region Field Initializers

        internal static ImmutableArray<BoundInitializer> BindFieldInitializers(
            SourceMemberContainerTypeSymbol containingType,
            MethodSymbol scriptCtor,
            ImmutableArray<ImmutableArray<FieldInitializer>> initializers,
            DiagnosticBag diagnostics,
            bool generateDebugInfo,
            out ConsList<Imports> firstDebugImports)
        {
            if (initializers.IsEmpty)
            {
                firstDebugImports = null;
                return ImmutableArray<BoundInitializer>.Empty;
            }

            CSharpCompilation compilation = containingType.DeclaringCompilation;
            ArrayBuilder<BoundInitializer> boundInitializers = ArrayBuilder<BoundInitializer>.GetInstance();

            if ((object)scriptCtor == null)
            {
                BindRegularCSharpFieldInitializers(compilation, initializers, boundInitializers, diagnostics, generateDebugInfo, out firstDebugImports);
            }
            else
            {
                BindScriptFieldInitializers(compilation, scriptCtor, initializers, boundInitializers, diagnostics, generateDebugInfo, out firstDebugImports);
            }

            return boundInitializers.ToImmutableAndFree();
        }

        /// <summary>
        /// In regular C#, all field initializers are assignments to fields and the assigned expressions
        /// may not reference instance members.
        /// </summary>
        private static void BindRegularCSharpFieldInitializers(
            CSharpCompilation compilation,
            ImmutableArray<ImmutableArray<FieldInitializer>> initializers,
            ArrayBuilder<BoundInitializer> boundInitializers,
            DiagnosticBag diagnostics,
            bool generateDebugInfo,
            out ConsList<Imports> firstDebugImports)
        {
            firstDebugImports = null;

            foreach (ImmutableArray<FieldInitializer> siblingInitializers in initializers)
            {
                // All sibling initializers share the same parent node and tree so we can reuse the binder 
                // factory across siblings.  Unfortunately, we cannot reuse the binder itself, because
                // individual fields might have their own binders (e.g. because of being declared unsafe).
                BinderFactory binderFactory = null;

                foreach (FieldInitializer initializer in siblingInitializers)
                {
                    FieldSymbol fieldSymbol = initializer.Field;
                    Debug.Assert((object)fieldSymbol != null);

                    // A constant field of type decimal needs a field initializer, so
                    // check if it is a metadata constant, not just a constant to exclude
                    // decimals. Other constants do not need field initializers.
                    if (!fieldSymbol.IsMetadataConstant)
                    {
                        //Can't assert that this is a regular C# compilation, because we could be in a nested type of a script class.
                        SyntaxReference syntaxRef = initializer.Syntax;
                        var initializerNode = (CSharpSyntaxNode)syntaxRef.GetSyntax();

                        if (binderFactory == null)
                        {
                            binderFactory = compilation.GetBinderFactory(syntaxRef.SyntaxTree);
                        }

                        Binder parentBinder = binderFactory.GetBinder(initializerNode);
                        Debug.Assert(parentBinder.ContainingMemberOrLambda == fieldSymbol.ContainingType || //should be the binder for the type
                            fieldSymbol.ContainingType.IsImplicitClass); //however, we also allow fields in namespaces to help support script scenarios

                        if (generateDebugInfo && firstDebugImports == null)
                        {
                            firstDebugImports = parentBinder.ImportsList;
                        }

                        BoundInitializer boundInitializer = BindFieldInitializer(
                            new LocalScopeBinder(parentBinder.WithPrimaryConstructorParametersIfNecessary(fieldSymbol.ContainingType, shadowBackingFields: true)).
                                            WithAdditionalFlagsAndContainingMemberOrLambda(parentBinder.Flags | BinderFlags.FieldInitializer, fieldSymbol),
                            fieldSymbol,
                            (EqualsValueClauseSyntax)initializerNode,
                            diagnostics);

                        boundInitializers.Add(boundInitializer);
                    }
                }
            }
        }

        /// <summary>
        /// In script C#, some field initializers are assignments to fields and others are global
        /// statements.  There are no restrictions on accessing instance members.
        /// </summary>
        private static void BindScriptFieldInitializers(CSharpCompilation compilation, MethodSymbol scriptCtor,
            ImmutableArray<ImmutableArray<FieldInitializer>> initializers, ArrayBuilder<BoundInitializer> boundInitializers, DiagnosticBag diagnostics,
            bool generateDebugInfo, out ConsList<Imports> firstDebugImports)
        {
            Debug.Assert((object)scriptCtor != null);

            firstDebugImports = null;

            for (int i = 0; i < initializers.Length; i++)
            {
                ImmutableArray<FieldInitializer> siblingInitializers = initializers[i];

                // All sibling initializers share the same parent node and tree so we can reuse the binder 
                // factory across siblings.  Unfortunately, we cannot reuse the binder itself, because
                // individual fields might have their own binders (e.g. because of being declared unsafe).
                BinderFactory binderFactory = null;

                for (int j = 0; j < siblingInitializers.Length; j++)
                {
                    var initializer = siblingInitializers[j];
                    var fieldSymbol = initializer.Field;

                    if ((object)fieldSymbol != null && fieldSymbol.IsConst)
                    {
                        // Constants do not need field initializers.
                        continue;
                    }

                    var syntaxRef = initializer.Syntax;
                    Debug.Assert(syntaxRef.SyntaxTree.Options.Kind != SourceCodeKind.Regular);

                    var initializerNode = (CSharpSyntaxNode)syntaxRef.GetSyntax();

                    if (binderFactory == null)
                    {
                        binderFactory = compilation.GetBinderFactory(syntaxRef.SyntaxTree);
                    }

                    Binder scriptClassBinder = binderFactory.GetBinder(initializerNode);
                    Debug.Assert(((ImplicitNamedTypeSymbol)scriptClassBinder.ContainingMemberOrLambda).IsScriptClass);

                    if (generateDebugInfo && firstDebugImports == null)
                    {
                        firstDebugImports = scriptClassBinder.ImportsList;
                    }

                    Binder parentBinder = new ExecutableCodeBinder((CSharpSyntaxNode)syntaxRef.SyntaxTree.GetRoot(), scriptCtor, scriptClassBinder);

                    BoundInitializer boundInitializer;
                    if ((object)fieldSymbol != null)
                    {
                        boundInitializer = BindFieldInitializer(
                            new LocalScopeBinder(parentBinder).WithAdditionalFlagsAndContainingMemberOrLambda(parentBinder.Flags | BinderFlags.FieldInitializer, fieldSymbol),
                            fieldSymbol,
                            (EqualsValueClauseSyntax)initializerNode,
                            diagnostics);
                    }
                    else if (initializerNode.Kind == SyntaxKind.LabeledStatement)
                    {
                        // TODO: labels in interactive
                        var boundStatement = new BoundBadStatement(initializerNode, ImmutableArray<BoundNode>.Empty, true);
                        boundInitializer = new BoundGlobalStatementInitializer(initializerNode, boundStatement);
                    }
                    else
                    {
                        var collisionDetector = new LocalScopeBinder(parentBinder);
                        boundInitializer = BindGlobalStatement(collisionDetector, (StatementSyntax)initializerNode, diagnostics,
                            isLast: i == initializers.Length - 1 && j == siblingInitializers.Length - 1);
                    }

                    boundInitializers.Add(boundInitializer);
                }
            }
        }

        private static BoundInitializer BindGlobalStatement(Binder binder, StatementSyntax statementNode, DiagnosticBag diagnostics, bool isLast)
        {
            BoundStatement boundStatement = binder.BindStatement(statementNode, diagnostics);

            // the result of the last global expression is assigned to the result storage for submission result:
            if (binder.Compilation.IsSubmission && isLast && boundStatement.Kind == BoundKind.ExpressionStatement && !boundStatement.HasAnyErrors)
            {
                var submissionReturnType = binder.Compilation.GetSubmissionReturnType();

                // insert an implicit conversion for the submission return type (if needed):
                var expression = ((BoundExpressionStatement)boundStatement).Expression;
                if ((object)expression.Type == null || expression.Type.SpecialType != SpecialType.System_Void)
                {
                    expression = binder.GenerateConversionForAssignment(submissionReturnType, expression, diagnostics);
                    boundStatement = new BoundExpressionStatement(boundStatement.Syntax, expression, expression.HasErrors);
                }
            }

            return new BoundGlobalStatementInitializer(statementNode, boundStatement);
        }

        private static BoundFieldInitializer BindFieldInitializer(Binder binder, FieldSymbol fieldSymbol, EqualsValueClauseSyntax equalsValueClauseNode,
            DiagnosticBag diagnostics)
        {
            Debug.Assert(!fieldSymbol.IsMetadataConstant);

            var fieldsBeingBound = binder.FieldsBeingBound;

            var sourceField = fieldSymbol as SourceMemberFieldSymbol;
            bool isImplicitlyTypedField = (object)sourceField != null && sourceField.FieldTypeInferred(fieldsBeingBound);

            // If the type is implicitly typed, the initializer diagnostics have already been reported, so ignore them here:
            // CONSIDER (tomat): reusing the bound field initializers for implicitly typed fields.
            DiagnosticBag initializerDiagnostics;
            if (isImplicitlyTypedField)
            {
                initializerDiagnostics = DiagnosticBag.GetInstance();
            }
            else
            {
                initializerDiagnostics = diagnostics;
            }

            var collisionDetector = new LocalScopeBinder(binder);
            var boundInitValue = collisionDetector.BindVariableOrAutoPropInitializer(equalsValueClauseNode, fieldSymbol.GetFieldType(fieldsBeingBound), initializerDiagnostics);

            if (isImplicitlyTypedField)
            {
                initializerDiagnostics.Free();
            }

            return new BoundFieldInitializer(
                equalsValueClauseNode.Value, //we want the attached sequence point to indicate the value node
                fieldSymbol,
                boundInitValue);
        }

        #endregion

        #region Method Body Binding

        // NOTE: can return null if the method has no body.
        internal static BoundBlock BindMethodBody(MethodSymbol method, TypeCompilationState compilationState, DiagnosticBag diagnostics)
        {
            ConsList<Imports> unused;
            return BindMethodBody(method, compilationState, diagnostics, false, out unused);
        }

        // NOTE: can return null if the method has no body.
        internal static BoundBlock BindMethodBody(MethodSymbol method, TypeCompilationState compilationState, DiagnosticBag diagnostics, bool generateDebugInfo, out ConsList<Imports> debugImports)
        {
            debugImports = null;

            BoundStatement constructorInitializer = null;
            BoundBlock body;

            var compilation = method.DeclaringCompilation;

            var sourceMethod = method as SourceMethodSymbol;
            if ((object)sourceMethod != null)
            {
                if (sourceMethod.IsExtern)
                {
                    if (sourceMethod.BlockSyntax == null)
                    {
                        // Generate warnings only if we are not generating ERR_ExternHasBody error
                        GenerateExternalMethodWarnings(sourceMethod, diagnostics);
                    }
                    return null;
                }
                else if (sourceMethod.IsParameterlessValueTypeConstructor(requireSynthesized: true))
                {
                    // No body for default struct constructor.
                    return null;
                }

                var blockSyntax = sourceMethod.BlockSyntax;
                if (blockSyntax != null)
                {
                    var factory = compilation.GetBinderFactory(sourceMethod.SyntaxTree);
                    var inMethodBinder = factory.GetBinder(blockSyntax);
                    var binder = new ExecutableCodeBinder(blockSyntax, sourceMethod, inMethodBinder);
                    body = binder.BindBlock(blockSyntax, diagnostics);
                    if (generateDebugInfo)
                    {
                        debugImports = binder.ImportsList;
                    }
                    if (inMethodBinder.IsDirectlyInIterator)
                    {
                        foreach (var parameter in method.Parameters)
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

                        if (sourceMethod.IsUnsafe && compilation.Options.AllowUnsafe) // Don't cascade
                        {
                            diagnostics.Add(ErrorCode.ERR_IllegalInnerUnsafe, sourceMethod.Locations[0]);
                        }

                        if (sourceMethod.IsVararg)
                        {
                            // error CS1636: __arglist is not allowed in the parameter list of iterators
                            diagnostics.Add(ErrorCode.ERR_VarargsIterator, sourceMethod.Locations[0]);
                        }
                    }
                }
                else // for [if (blockSyntax != null)]
                {
                    var property = sourceMethod.AssociatedSymbol as SourcePropertySymbol;
                    if ((object)property != null && property.IsAutoProperty)
                    {
                        return MethodBodySynthesizer.ConstructAutoPropertyAccessorBody(sourceMethod);
                    }

                    if (sourceMethod.IsPrimaryCtor)
                    {
                        body = null;
                    }
                    else
                    {
                        return null;
                    }
                }
            }
            else
            {
                //  synthesized methods should return their bound bodies 
                body = null;
            }

            // delegates have constructors but not constructor initializers
            if (method.MethodKind == MethodKind.Constructor && !method.ContainingType.IsDelegateType())
            {
                var initializerInvocation = BindConstructorInitializer(method, diagnostics, compilation);

                if (initializerInvocation != null)
                {
                    constructorInitializer = new BoundExpressionStatement(initializerInvocation.Syntax, initializerInvocation) { WasCompilerGenerated = true };
                    Debug.Assert(initializerInvocation.HasAnyErrors || constructorInitializer.IsConstructorInitializer(), "Please keep this bound node in sync with BoundNodeExtensions.IsConstructorInitializer.");
                }
            }

            var statements = ArrayBuilder<BoundStatement>.GetInstance();

            if (constructorInitializer != null)
            {
                statements.Add(constructorInitializer);
            }

            if ((object)sourceMethod != null && sourceMethod.IsPrimaryCtor && (object)((SourceMemberContainerTypeSymbol)sourceMethod.ContainingType).PrimaryCtor == (object)sourceMethod)
            {
                Debug.Assert(method.MethodKind == MethodKind.Constructor && !method.ContainingType.IsDelegateType());
                Debug.Assert(body == null);

                if (sourceMethod.ParameterCount > 0)
                {
                    var factory = new SyntheticBoundNodeFactory(sourceMethod, sourceMethod.SyntaxNode, compilationState, diagnostics);
                    factory.CurrentMethod = sourceMethod; 

                    foreach (var parameter in sourceMethod.Parameters)
                    {
                        FieldSymbol field = parameter.PrimaryConstructorParameterBackingField;

                        if ((object)field != null)
                        {
                            statements.Add(factory.Assignment(factory.Field(factory.This(), field),
                                                                   factory.Parameter(parameter)));
                        }
                    }
                }
            }

            if (body != null)
            {
                statements.Add(body);
            }

            CSharpSyntaxNode syntax = body != null ? body.Syntax : method.GetNonNullSyntaxNode();

            BoundBlock block;
            if (statements.Count == 1 && statements[0].Kind == ((body == null) ? BoundKind.Block : body.Kind))
            {
                // most common case - we just have a single block for the body.
                block = (BoundBlock)statements[0];
                statements.Free();
            }
            else
            {
                block = new BoundBlock(syntax, default(ImmutableArray<LocalSymbol>), statements.ToImmutableAndFree()) { WasCompilerGenerated = true };
            }

            return method.MethodKind == MethodKind.Destructor ? MethodBodySynthesizer.ConstructDestructorBody(syntax, method, block) : block;
        }

        /// <summary>
        /// Bind the (implicit or explicit) constructor initializer of a constructor symbol.
        /// </summary>
        /// <param name="constructor">Constructor method.</param>
        /// <param name="diagnostics">Accumulates errors (e.g. access "this" in constructor initializer).</param>
        /// <param name="compilation">Used to retrieve binder.</param>
        /// <returns>A bound expression for the constructor initializer call.</returns>
        private static BoundExpression BindConstructorInitializer(MethodSymbol constructor, DiagnosticBag diagnostics, CSharpCompilation compilation)
        {
            // Note that the base type can be null if we're compiling System.Object in source.
            NamedTypeSymbol baseType = constructor.ContainingType.BaseTypeNoUseSiteDiagnostics;

            SourceMethodSymbol sourceConstructor = constructor as SourceMethodSymbol;
            CSharpSyntaxNode syntax = null;
            ArgumentListSyntax initializerArgumentListOpt = null;
            if ((object)sourceConstructor != null)
            {
                syntax = sourceConstructor.SyntaxNode;

                if (syntax.Kind == SyntaxKind.ConstructorDeclaration)
                {
                    var constructorSyntax = (ConstructorDeclarationSyntax)syntax;
                    if (constructorSyntax.Initializer != null)
                    {
                        initializerArgumentListOpt = constructorSyntax.Initializer.ArgumentList;
                    }

                    ErrorCode reportIfHavePrimaryCtor = ErrorCode.Void;

                    if (initializerArgumentListOpt == null || initializerArgumentListOpt.Parent.Kind != SyntaxKind.ThisConstructorInitializer)
                    {
                        reportIfHavePrimaryCtor = ErrorCode.ERR_InstanceCtorMustHaveThisInitializer;
                    }
                    else if (initializerArgumentListOpt.Arguments.Count == 0 && constructor.ContainingType.TypeKind == TypeKind.Struct)
                    {
                        // Based on C# Design Notes for Oct 21, 2013:
                        reportIfHavePrimaryCtor = ErrorCode.ERR_InstanceCtorCannotHaveDefaultThisInitializer;
                    }

                    if (reportIfHavePrimaryCtor != ErrorCode.Void)
                    {
                        var container = constructor.ContainingType as SourceMemberContainerTypeSymbol;

                        if ((object)container != null && (object)container.PrimaryCtor != null)
                        {
                            diagnostics.Add(reportIfHavePrimaryCtor, constructor.Locations[0]);
                        }
                    }
                }
                else 
                {
                    // Primary constuctor case.
                    Debug.Assert(syntax.Kind == SyntaxKind.ParameterList);
                    if (syntax.Parent.Kind == SyntaxKind.ClassDeclaration)
                    {
                        var classDecl = (ClassDeclarationSyntax)syntax.Parent;

                        if (classDecl.BaseList != null && classDecl.BaseList.Types.Count > 0)
                        {
                            TypeSyntax baseTypeSyntax = classDecl.BaseList.Types[0];
                            if (baseTypeSyntax.Kind == SyntaxKind.BaseClassWithArguments)
                            {
                                initializerArgumentListOpt = ((BaseClassWithArgumentsSyntax)baseTypeSyntax).ArgumentList;
                            }
                        }
                    }
                    else
            {
                        Debug.Assert(syntax.Parent.Kind == SyntaxKind.StructDeclaration);
                    }
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
                SyntaxToken bodyToken;
                if (containerNode.Kind == SyntaxKind.ClassDeclaration)
                {
                    bodyToken = ((ClassDeclarationSyntax)containerNode).OpenBraceToken;
                }
                else if (containerNode.Kind == SyntaxKind.StructDeclaration)
                {
                    bodyToken = ((StructDeclarationSyntax)containerNode).OpenBraceToken;
                }
                else if (containerNode.Kind == SyntaxKind.EnumDeclaration)
                {
                    // We're not going to find any non-default ctors, but we'll look anyway.
                    bodyToken = ((EnumDeclarationSyntax)containerNode).OpenBraceToken;
                }
                else
                {
                    Debug.Assert(false, "How did we get an implicit constructor added to something that is neither a class nor a struct?");
                    bodyToken = containerNode.GetFirstToken();
                }

                outerBinder = compilation.GetBinderFactory(containerNode.SyntaxTree).GetBinder(containerNode, bodyToken.Position);
            }
            else if (initializerArgumentListOpt == null)
            {
                // We have a ctor in source but no explicit constructor initializer.  We can't just use the binder for the
                // type containing the ctor because the ctor might be marked unsafe.  Use the binder for the parameter list
                // as an approximation - the extra symbols won't matter because there are no identifiers to bind.

                outerBinder = compilation.GetBinderFactory(sourceConstructor.SyntaxTree).GetBinder(syntax.Kind == SyntaxKind.ParameterList ? 
                                                                                                        syntax :
                                                                                                        ((ConstructorDeclarationSyntax)syntax).ParameterList);
            }
            else
            {
                outerBinder = compilation.GetBinderFactory(sourceConstructor.SyntaxTree).GetBinder(initializerArgumentListOpt);
            }

            //wrap in ConstructorInitializerBinder for appropriate errors
            Binder initializerBinder = outerBinder.WithAdditionalFlagsAndContainingMemberOrLambda(BinderFlags.ConstructorInitializer, constructor);
            return initializerBinder.BindConstructorInitializer(initializerArgumentListOpt, constructor, diagnostics);
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

        #endregion Method Body Binding

        #region Script

        internal static MethodSymbol DefineScriptEntryPoint(CSharpCompilation compilation, PEModuleBuilder moduleBeingBuilt, TypeSymbol returnType, bool hasDeclarationErrors, DiagnosticBag diagnostics)
        {
            var scriptEntryPoint = new SynthesizedEntryPointSymbol(compilation.ScriptClass, returnType, diagnostics);
            if (moduleBeingBuilt != null && !hasDeclarationErrors && !diagnostics.HasAnyErrors())
            {
                var compilationState = new TypeCompilationState(compilation.ScriptClass, moduleBeingBuilt);
                var body = scriptEntryPoint.CreateBody();

                var emittedBody = GenerateMethodBody(
                    compilationState,
                    scriptEntryPoint,
                    body,
                    diagnostics,
                    compilation.Options.Optimize,
                    debugDocumentProvider: null,
                    namespaceScopes: default(ImmutableArray<NamespaceScope>));

                moduleBeingBuilt.SetMethodBody(scriptEntryPoint, emittedBody);
                moduleBeingBuilt.AddCompilerGeneratedDefinition(compilation.ScriptClass, scriptEntryPoint);
            }

            return scriptEntryPoint;
        }

        #endregion Script
    }
}
