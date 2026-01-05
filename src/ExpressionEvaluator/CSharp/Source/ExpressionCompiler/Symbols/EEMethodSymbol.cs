// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using Microsoft.CodeAnalysis.CodeGen;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.CodeAnalysis.ExpressionEvaluator;
using Microsoft.CodeAnalysis.PooledObjects;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.ExpressionEvaluator
{
    internal delegate BoundExpression GenerateThisReference(SyntaxNode syntax);

    internal delegate BoundStatement GenerateMethodBody(
        EEMethodSymbol method,
        DiagnosticBag diagnostics,
        out ImmutableArray<LocalSymbol> declaredLocals,
        out ResultProperties properties);

    /// <summary>
    /// Synthesized expression evaluation method.
    /// </summary>
    internal sealed class EEMethodSymbol : MethodSymbol
    {
        // We only create a single EE method (per EE type) that represents an arbitrary expression,
        // whose lowering may produce synthesized members (lambdas, dynamic sites, etc).
        // We may thus assume that the method ordinal is always 0.
        //
        // Consider making the implementation more flexible in order to avoid this assumption.
        // In future we might need to compile multiple expression and then we'll need to assign 
        // a unique method ordinal to each of them to avoid duplicate synthesized member names.
        private const int _methodOrdinal = 0;

        internal readonly TypeMap TypeMap;
        internal readonly MethodSymbol SubstitutedSourceMethod;
        internal readonly ImmutableArray<LocalSymbol> Locals;

        /// <summary>
        /// Display class variables declared outside of the current source method.
        /// They are shadowed by source method parameters and locals declared within the method.
        /// </summary>
        internal readonly ImmutableArray<LocalSymbol> LocalsForBindingOutside;

        /// <summary>
        /// Locals and display class variables declared within the current source method.
        /// They shadow the source method parameters. In other words, display class variables
        /// created for method parameters shadow the parameters.
        /// </summary>
        internal readonly ImmutableArray<LocalSymbol> LocalsForBindingInside;

        private readonly EENamedTypeSymbol _container;
        private readonly string _name;
        private readonly ImmutableArray<Location> _locations;
        private readonly ImmutableArray<TypeParameterSymbol> _typeParameters;
        private readonly ImmutableArray<ParameterSymbol> _parameters;
        private readonly ParameterSymbol _thisParameter;
        private readonly ImmutableDictionary<string, DisplayClassVariable> _displayClassVariables;

        /// <summary>
        /// Invoked at most once to generate the method body.
        /// (If the compilation has no errors, it will be invoked
        /// exactly once, otherwise it may be skipped.)
        /// </summary>
        private readonly GenerateMethodBody _generateMethodBody;
        private TypeWithAnnotations _lazyReturnType;
        private ResultProperties _lazyResultProperties;

        // NOTE: This is only used for asserts, so it could be conditional on DEBUG.
        private readonly ImmutableArray<TypeParameterSymbol> _allTypeParameters;

        internal EEMethodSymbol(
            EENamedTypeSymbol container,
            string name,
            Location location,
            MethodSymbol sourceMethod,
            ImmutableArray<LocalSymbol> sourceLocals,
            ImmutableArray<LocalSymbol> sourceLocalsForBindingOutside,
            ImmutableArray<LocalSymbol> sourceLocalsForBindingInside,
            ImmutableDictionary<string, DisplayClassVariable> sourceDisplayClassVariables,
            GenerateMethodBody generateMethodBody)
        {
            Debug.Assert(sourceMethod.IsDefinition);
            Debug.Assert(TypeSymbol.Equals((TypeSymbol)sourceMethod.ContainingSymbol, container.SubstitutedSourceType.OriginalDefinition, TypeCompareKind.ConsiderEverything2));
            Debug.Assert(sourceLocals.All(l => l.ContainingSymbol == sourceMethod));

            _container = container;
            _name = name;
            _locations = ImmutableArray.Create(location);

            // What we want is to map all original type parameters to the corresponding new type parameters
            // (since the old ones have the wrong owners).  Unfortunately, we have a circular dependency:
            //   1) Each new type parameter requires the entire map in order to be able to construct its constraint list.
            //   2) The map cannot be constructed until all new type parameters exist.
            // Our solution is to pass each new type parameter a lazy reference to the type map.  We then 
            // initialize the map as soon as the new type parameters are available - and before they are 
            // handed out - so that there is never a period where they can require the type map and find
            // it uninitialized.

            var sourceMethodTypeParameters = sourceMethod.TypeParameters;
            var allSourceTypeParameters = container.SourceTypeParameters.Concat(sourceMethodTypeParameters);

            sourceMethod = new EECompilationContextMethod(DeclaringCompilation, sourceMethod);

            sourceMethodTypeParameters = sourceMethod.TypeParameters;
            allSourceTypeParameters = allSourceTypeParameters.Concat(sourceMethodTypeParameters);

            var getTypeMap = new Func<TypeMap>(() => this.TypeMap);
            _typeParameters = sourceMethodTypeParameters.SelectAsArray(
                (tp, i, arg) => (TypeParameterSymbol)new EETypeParameterSymbol(this, tp, i, getTypeMap),
                (object)null);
            _allTypeParameters = container.TypeParameters.Concat(_typeParameters).Concat(_typeParameters);
            this.TypeMap = new TypeMap(allSourceTypeParameters, _allTypeParameters, allowAlpha: true);

            EENamedTypeSymbol.VerifyTypeParameters(this, _typeParameters);

            var substitutedSourceType = container.SubstitutedSourceType;
            this.SubstitutedSourceMethod = sourceMethod.AsMember(substitutedSourceType);
            if (sourceMethod.Arity > 0)
            {
                this.SubstitutedSourceMethod = this.SubstitutedSourceMethod.Construct(_typeParameters.As<TypeSymbol>());
            }
            TypeParameterChecker.Check(this.SubstitutedSourceMethod, _allTypeParameters);

            // Create a map from original parameter to target parameter.
            var parameterBuilder = ArrayBuilder<ParameterSymbol>.GetInstance();

            var substitutedSourceThisParameter = this.SubstitutedSourceMethod.ThisParameter;
            var substitutedSourceHasThisParameter = (object)substitutedSourceThisParameter != null;
            if (substitutedSourceHasThisParameter)
            {
                _thisParameter = MakeParameterSymbol(0, GeneratedNames.ThisProxyFieldName(), substitutedSourceThisParameter);
                Debug.Assert(TypeSymbol.Equals(_thisParameter.Type, this.SubstitutedSourceMethod.ContainingType, TypeCompareKind.ConsiderEverything2));
                parameterBuilder.Add(_thisParameter);
            }

            var ordinalOffset = (substitutedSourceHasThisParameter ? 1 : 0);
            foreach (var substitutedSourceParameter in this.SubstitutedSourceMethod.Parameters)
            {
                var ordinal = substitutedSourceParameter.Ordinal + ordinalOffset;
                Debug.Assert(ordinal == parameterBuilder.Count);
                var parameter = MakeParameterSymbol(ordinal, substitutedSourceParameter.Name, substitutedSourceParameter);
                parameterBuilder.Add(parameter);
            }

            _parameters = parameterBuilder.ToImmutableAndFree();

            var localsBuilder = ArrayBuilder<LocalSymbol>.GetInstance();
            var localsMap = PooledDictionary<LocalSymbol, LocalSymbol>.GetInstance();
            foreach (var sourceLocal in sourceLocals)
            {
                var local = sourceLocal.ToOtherMethod(this, this.TypeMap);
                localsMap.Add(sourceLocal, local);
                localsBuilder.Add(local);
            }
            this.Locals = localsBuilder.ToImmutableAndFree();

            this.LocalsForBindingInside = remapLocalsForBinding(sourceLocalsForBindingInside, localsMap);
            this.LocalsForBindingOutside = remapLocalsForBinding(sourceLocalsForBindingOutside, localsMap);

            // Create a map from variable name to display class field.
            var displayClassVariables = PooledDictionary<string, DisplayClassVariable>.GetInstance();
            foreach (var pair in sourceDisplayClassVariables)
            {
                var variable = pair.Value;
                var oldDisplayClassInstance = variable.DisplayClassInstance;

                // Note: we don't call ToOtherMethod in the local case because doing so would produce
                // a new LocalSymbol that would not be ReferenceEquals to the one in this.LocalsForBinding.
                var oldDisplayClassInstanceFromLocal = oldDisplayClassInstance as DisplayClassInstanceFromLocal;
                var newDisplayClassInstance = (oldDisplayClassInstanceFromLocal == null)
                    ? oldDisplayClassInstance.ToOtherMethod(this, this.TypeMap)
                    : new DisplayClassInstanceFromLocal((EELocalSymbol)localsMap[oldDisplayClassInstanceFromLocal.Local]);

                variable = variable.SubstituteFields(newDisplayClassInstance, this.TypeMap);
                displayClassVariables.Add(pair.Key, variable);
            }

            _displayClassVariables = displayClassVariables.ToImmutableDictionary();
            displayClassVariables.Free();
            localsMap.Free();

            _generateMethodBody = generateMethodBody;

            ImmutableArray<LocalSymbol> remapLocalsForBinding(
                ImmutableArray<LocalSymbol> sourceLocalsForBinding,
                Dictionary<LocalSymbol, LocalSymbol> localsMap)
            {
                var localsBuilder = ArrayBuilder<LocalSymbol>.GetInstance(sourceLocalsForBinding.Length);
                foreach (var sourceLocal in sourceLocalsForBinding)
                {
                    LocalSymbol local;
                    if (!localsMap.TryGetValue(sourceLocal, out local))
                    {
                        local = sourceLocal.ToOtherMethod(this, this.TypeMap);
                        localsMap.Add(sourceLocal, local);
                    }
                    localsBuilder.Add(local);
                }

                return localsBuilder.ToImmutableAndFree();
            }
        }

        private ParameterSymbol MakeParameterSymbol(int ordinal, string name, ParameterSymbol sourceParameter)
        {
            return SynthesizedParameterSymbol.Create(this, sourceParameter.TypeWithAnnotations, ordinal, sourceParameter.RefKind, name, ScopedKind.None, refCustomModifiers: sourceParameter.RefCustomModifiers);
        }

        internal override bool IsMetadataNewSlot(bool ignoreInterfaceImplementationChanges = false)
        {
            return false;
        }

        internal override bool IsMetadataVirtual(IsMetadataVirtualOption option = IsMetadataVirtualOption.None)
        {
            return false;
        }

        internal override bool IsMetadataFinal
        {
            get { return false; }
        }

        public override MethodKind MethodKind
        {
            get { return MethodKind.Ordinary; }
        }

        public override string Name
        {
            get { return _name; }
        }

        public override int Arity
        {
            get { return _typeParameters.Length; }
        }

        public override bool IsExtensionMethod
        {
            get { return false; }
        }

        internal override bool HasSpecialName
        {
            get { return true; }
        }

        internal override System.Reflection.MethodImplAttributes ImplementationAttributes
        {
            get { return default(System.Reflection.MethodImplAttributes); }
        }

        internal override bool HasDeclarativeSecurity
        {
            get { return false; }
        }

        public override DllImportData GetDllImportData()
        {
            return null;
        }

        public override bool AreLocalsZeroed
        {
            get { return true; }
        }

        internal override IEnumerable<Cci.SecurityAttribute> GetSecurityInformation()
        {
            throw ExceptionUtilities.Unreachable();
        }

        internal override MarshalPseudoCustomAttributeData ReturnValueMarshallingInformation
        {
            get { return null; }
        }

        internal override bool RequiresSecurityObject
        {
            get { return false; }
        }

#nullable enable

        internal override bool TryGetThisParameter(out ParameterSymbol? thisParameter)
        {
            thisParameter = null;
            return true;
        }

#nullable disable

        public override bool HidesBaseMethodsByName
        {
            get { return false; }
        }

        public override bool IsVararg
        {
            get { return this.SubstitutedSourceMethod.IsVararg; }
        }

        public override RefKind RefKind
        {
            get { return this.SubstitutedSourceMethod.RefKind; }
        }

        public override bool ReturnsVoid
        {
            get { return this.ReturnType.SpecialType == SpecialType.System_Void; }
        }

        public override bool IsAsync
        {
            get { return false; }
        }

        public override TypeWithAnnotations ReturnTypeWithAnnotations
        {
            get
            {
                if ((object)_lazyReturnType == null)
                {
                    throw new InvalidOperationException();
                }
                return _lazyReturnType;
            }
        }

        public override FlowAnalysisAnnotations ReturnTypeFlowAnalysisAnnotations => FlowAnalysisAnnotations.None;

        public override ImmutableHashSet<string> ReturnNotNullIfParameterNotNull => ImmutableHashSet<string>.Empty;

        public override FlowAnalysisAnnotations FlowAnalysisAnnotations => FlowAnalysisAnnotations.None;

        public override ImmutableArray<TypeWithAnnotations> TypeArgumentsWithAnnotations
        {
            get { return GetTypeParametersAsTypeArguments(); }
        }

        public override ImmutableArray<TypeParameterSymbol> TypeParameters
        {
            get { return _typeParameters; }
        }

        public override ImmutableArray<ParameterSymbol> Parameters
        {
            get { return _parameters; }
        }

        public override ImmutableArray<MethodSymbol> ExplicitInterfaceImplementations
        {
            get { return ImmutableArray<MethodSymbol>.Empty; }
        }

        public override ImmutableArray<CustomModifier> RefCustomModifiers
        {
            get { return ImmutableArray<CustomModifier>.Empty; }
        }

        public override Symbol AssociatedSymbol
        {
            get { return null; }
        }

        internal override ImmutableArray<string> GetAppliedConditionalSymbols()
        {
            throw ExceptionUtilities.Unreachable();
        }

        internal override Cci.CallingConvention CallingConvention
        {
            get
            {
                Debug.Assert(this.IsStatic);
                var cc = Cci.CallingConvention.Default;
                if (this.IsVararg)
                {
                    cc |= Cci.CallingConvention.ExtraArguments;
                }
                if (this.IsGenericMethod)
                {
                    cc |= Cci.CallingConvention.Generic;
                }
                return cc;
            }
        }

        internal override bool GenerateDebugInfo
        {
            get { return false; }
        }

        public override Symbol ContainingSymbol
        {
            get { return _container; }
        }

        public override ImmutableArray<Location> Locations
        {
            get { return _locations; }
        }

        public override ImmutableArray<SyntaxReference> DeclaringSyntaxReferences
        {
            get { return GetDeclaringSyntaxReferenceHelper<CSharpSyntaxNode>(_locations); }
        }

        public override Accessibility DeclaredAccessibility
        {
            get { return Accessibility.Internal; }
        }

        public override bool IsStatic
        {
            get { return true; }
        }

        public override bool IsVirtual
        {
            get { return false; }
        }

        public override bool IsOverride
        {
            get { return false; }
        }

        public override bool IsAbstract
        {
            get { return false; }
        }

        public override bool IsSealed
        {
            get { return false; }
        }

        public override bool IsExtern
        {
            get { return false; }
        }

        internal override bool IsDeclaredReadOnly => false;

        internal override bool IsInitOnly => false;

        internal override ObsoleteAttributeData ObsoleteAttributeData
        {
            get { throw ExceptionUtilities.Unreachable(); }
        }

        internal sealed override UnmanagedCallersOnlyAttributeData GetUnmanagedCallersOnlyAttributeData(bool forceComplete) => throw ExceptionUtilities.Unreachable();

        internal sealed override bool HasSpecialNameAttribute => throw ExceptionUtilities.Unreachable();

        internal override bool HasUnscopedRefAttribute => false;

        internal override bool UseUpdatedEscapeRules => false;

        internal ResultProperties ResultProperties
        {
            get { return _lazyResultProperties; }
        }

        internal override void GenerateMethodBody(TypeCompilationState compilationState, BindingDiagnosticBag diagnostics)
        {
            ImmutableArray<LocalSymbol> declaredLocalsArray;
            var body = _generateMethodBody(this, diagnostics.DiagnosticBag, out declaredLocalsArray, out _lazyResultProperties);
            var compilation = compilationState.Compilation;

            _lazyReturnType = TypeWithAnnotations.Create(CalculateReturnType(compilation, body));

            // Can't do this until the return type has been computed.
            TypeParameterChecker.Check(this, _allTypeParameters);

            if (diagnostics.HasAnyErrors())
            {
                return;
            }

            DiagnosticsPass.IssueDiagnostics(compilation, body, diagnostics, this);
            if (diagnostics.HasAnyErrors())
            {
                return;
            }

            // Check for use-site diagnostics (e.g. missing types in the signature).
            UseSiteInfo<AssemblySymbol> useSiteInfo = default;
            this.CalculateUseSiteDiagnostic(ref useSiteInfo);
            if (useSiteInfo.DiagnosticInfo != null && useSiteInfo.DiagnosticInfo.Severity == DiagnosticSeverity.Error)
            {
                diagnostics.Add(useSiteInfo.DiagnosticInfo, this.GetFirstLocation());
                return;
            }

            try
            {
                var declaredLocals = PooledHashSet<LocalSymbol>.GetInstance();
                try
                {
                    // Rewrite local declaration statement.
                    body = (BoundStatement)LocalDeclarationRewriter.Rewrite(
                        compilation,
                        declaredLocals,
                        body,
                        declaredLocalsArray,
                        diagnostics.DiagnosticBag);

                    // Verify local declaration names.
                    foreach (var local in declaredLocals)
                    {
                        Debug.Assert(local.Locations.Length > 0);
                        var name = local.Name;
                        if (name.StartsWith("$", StringComparison.Ordinal))
                        {
                            diagnostics.Add(ErrorCode.ERR_UnexpectedCharacter, local.GetFirstLocation(), name[0]);
                            return;
                        }
                    }

                    // Rewrite references to placeholder "locals".
                    body = (BoundStatement)PlaceholderLocalRewriter.Rewrite(compilation, declaredLocals, body, diagnostics.DiagnosticBag);

                    if (diagnostics.HasAnyErrors())
                    {
                        return;
                    }
                }
                finally
                {
                    declaredLocals.Free();
                }

                var syntax = body.Syntax;
                var statementsBuilder = ArrayBuilder<BoundStatement>.GetInstance();
                statementsBuilder.Add(body);
                // Insert an implicit return statement if necessary.
                if (body.Kind != BoundKind.ReturnStatement)
                {
                    statementsBuilder.Add(new BoundReturnStatement(syntax, RefKind.None, expressionOpt: null, @checked: false));
                }

                var localsSet = PooledHashSet<LocalSymbol>.GetInstance();
                try
                {
                    var localsBuilder = ArrayBuilder<LocalSymbol>.GetInstance();
                    foreach (var local in this.LocalsForBindingOutside)
                    {
                        Debug.Assert(!localsSet.Contains(local));
                        localsBuilder.Add(local);
                        localsSet.Add(local);
                    }

                    foreach (var local in this.LocalsForBindingInside)
                    {
                        Debug.Assert(!localsSet.Contains(local));
                        localsBuilder.Add(local);
                        localsSet.Add(local);
                    }

                    foreach (var local in this.Locals)
                    {
                        if (localsSet.Add(local))
                        {
                            localsBuilder.Add(local);
                        }
                    }

                    body = new BoundBlock(syntax, localsBuilder.ToImmutableAndFree(), statementsBuilder.ToImmutableAndFree()) { WasCompilerGenerated = true };

                    Debug.Assert(!diagnostics.HasAnyErrors());
                    Debug.Assert(!body.HasErrors);
                    PipelinePhaseValidator.AssertAfterInitialBinding(body);

                    body = LocalRewriter.Rewrite(
                        compilation: this.DeclaringCompilation,
                        method: this,
                        methodOrdinal: _methodOrdinal,
                        containingType: _container,
                        statement: body,
                        compilationState: compilationState,
                        previousSubmissionFields: null,
                        allowOmissionOfConditionalCalls: false,
                        instrumentation: MethodInstrumentation.Empty,
                        debugDocumentProvider: null,
                        diagnostics: diagnostics,
                        codeCoverageSpans: out ImmutableArray<SourceSpan> codeCoverageSpans,
                        sawLambdas: out bool sawLambdas,
                        sawLocalFunctions: out bool sawLocalFunctions,
                        sawAwaitInExceptionHandler: out bool sawAwaitInExceptionHandler);

                    Debug.Assert(!sawAwaitInExceptionHandler);
                    Debug.Assert(codeCoverageSpans.IsEmpty);

                    if (body.HasErrors)
                    {
                        return;
                    }

                    body = ExtensionMethodReferenceRewriter.Rewrite(body);

                    if (body.HasErrors)
                    {
                        return;
                    }

                    // Variables may have been captured by lambdas in the original method
                    // or in the expression, and we need to preserve the existing values of
                    // those variables in the expression. This requires rewriting the variables
                    // in the expression based on the closure classes from both the original
                    // method and the expression, and generating a preamble that copies
                    // values into the expression closure classes.
                    //
                    // Consider the original method:
                    // static void M()
                    // {
                    //     int x, y, z;
                    //     ...
                    //     F(() => x + y);
                    // }
                    // and the expression in the EE: "F(() => x + z)".
                    //
                    // The expression is first rewritten using the closure class and local <1>
                    // from the original method: F(() => <1>.x + z)
                    // Then lambda rewriting introduces a new closure class that includes
                    // the locals <1> and z, and a corresponding local <2>: F(() => <2>.<1>.x + <2>.z)
                    // And a preamble is added to initialize the fields of <2>:
                    //     <2> = new <>c__DisplayClass0();
                    //     <2>.<1> = <1>;
                    //     <2>.z = z;

                    // Rewrite "this" and "base" references to parameter in this method.
                    // Rewrite variables within body to reference existing display classes.
                    body = (BoundStatement)CapturedVariableRewriter.Rewrite(
                        this.GenerateThisReference,
                        compilation.Conversions,
                        _displayClassVariables,
                        body,
                        diagnostics.DiagnosticBag);

                    if (body.HasErrors)
                    {
                        Debug.Assert(false, "Please add a test case capturing whatever caused this assert.");
                        return;
                    }

                    if (diagnostics.HasAnyErrors())
                    {
                        return;
                    }

                    if (sawLambdas || sawLocalFunctions)
                    {
                        var closureDebugInfoBuilder = ArrayBuilder<EncClosureInfo>.GetInstance();
                        var lambdaDebugInfoBuilder = ArrayBuilder<EncLambdaInfo>.GetInstance();
                        var lambdaRuntimeRudeEditsBuilder = ArrayBuilder<LambdaRuntimeRudeEditInfo>.GetInstance();

                        body = ClosureConversion.Rewrite(
                            loweredBody: body,
                            thisType: this.SubstitutedSourceMethod.ContainingType,
                            thisParameter: _thisParameter,
                            method: this,
                            methodOrdinal: _methodOrdinal,
                            substitutedSourceMethod: this.SubstitutedSourceMethod.OriginalDefinition,
                            lambdaDebugInfoBuilder,
                            lambdaRuntimeRudeEditsBuilder,
                            closureDebugInfoBuilder,
                            slotAllocator: null,
                            compilationState: compilationState,
                            diagnostics: diagnostics,
                            assignLocals: localsSet);

                        // we don't need this information:
                        closureDebugInfoBuilder.Free();
                        lambdaDebugInfoBuilder.Free();
                        lambdaRuntimeRudeEditsBuilder.Free();
                    }
                }
                finally
                {
                    localsSet.Free();
                }

                PipelinePhaseValidator.AssertAfterClosureConversion(body);

                // Insert locals from the original method,
                // followed by any new locals.
                var block = (BoundBlock)body;
                var localBuilder = ArrayBuilder<LocalSymbol>.GetInstance();
                foreach (var local in this.Locals)
                {
                    Debug.Assert(!(local is EELocalSymbol) || (((EELocalSymbol)local).Ordinal == localBuilder.Count));
                    localBuilder.Add(local);
                }
                foreach (var local in block.Locals)
                {
                    if (local is EELocalSymbol oldLocal)
                    {
                        Debug.Assert(localBuilder[oldLocal.Ordinal] == oldLocal);
                        continue;
                    }
                    localBuilder.Add(local);
                }

                body = block.Update(localBuilder.ToImmutableAndFree(), block.LocalFunctions, block.HasUnsafeModifier, instrumentation: null, block.Statements);
                TypeParameterChecker.Check(body, _allTypeParameters);
                compilationState.AddSynthesizedMethod(this, body);
            }
            catch (BoundTreeVisitor.CancelledByStackGuardException ex)
            {
                ex.AddAnError(diagnostics);
            }
        }

        private BoundExpression GenerateThisReference(SyntaxNode syntax)
        {
            var thisProxy = CompilationContext.GetThisProxy(_displayClassVariables);
            if (thisProxy != null)
            {
                return thisProxy.ToBoundExpression(syntax);
            }
            if ((object)_thisParameter != null)
            {
                var typeNameKind = GeneratedNameParser.GetKind(_thisParameter.TypeWithAnnotations.Type.Name);
                if (typeNameKind != GeneratedNameKind.None && typeNameKind != GeneratedNameKind.AnonymousType)
                {
                    Debug.Assert(typeNameKind == GeneratedNameKind.LambdaDisplayClass ||
                        typeNameKind == GeneratedNameKind.StateMachineType,
                        $"Unexpected typeNameKind '{typeNameKind}'");
                    return null;
                }
                return new BoundParameter(syntax, _thisParameter);
            }
            return null;
        }

        private static TypeSymbol CalculateReturnType(CSharpCompilation compilation, BoundStatement bodyOpt)
        {
            if (bodyOpt == null)
            {
                // If the method doesn't do anything, then it doesn't return anything.
                return compilation.GetSpecialType(SpecialType.System_Void);
            }

            switch (bodyOpt.Kind)
            {
                case BoundKind.ReturnStatement:
                    return ((BoundReturnStatement)bodyOpt).ExpressionOpt.Type;
                case BoundKind.ExpressionStatement:
                case BoundKind.LocalDeclaration:
                case BoundKind.MultipleLocalDeclarations:
                    return compilation.GetSpecialType(SpecialType.System_Void);
                default:
                    throw ExceptionUtilities.UnexpectedValue(bodyOpt.Kind);
            }
        }

        internal override int CalculateLocalSyntaxOffset(int localPosition, SyntaxTree localTree)
        {
            return localPosition;
        }

        internal override bool IsNullableAnalysisEnabled() => false;

        protected override bool HasSetsRequiredMembersImpl => throw ExceptionUtilities.Unreachable();

        internal sealed override bool HasAsyncMethodBuilderAttribute(out TypeSymbol builderArgument)
        {
            builderArgument = null;
            return false;
        }

        internal override int TryGetOverloadResolutionPriority() => 0;
    }
}
