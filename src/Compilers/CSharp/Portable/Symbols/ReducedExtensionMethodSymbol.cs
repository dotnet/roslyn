// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    /// <summary>
    /// An extension method with the "this" parameter removed.
    /// Used for the public binding API only, not for compilation.
    /// </summary>
    internal sealed class ReducedExtensionMethodSymbol : MethodSymbol
    {
        private readonly MethodSymbol _reducedFrom;
        private readonly TypeMap _typeMap;
        private readonly ImmutableArray<TypeParameterSymbol> _typeParameters;
        private readonly ImmutableArray<TypeWithAnnotations> _typeArguments;
        private ImmutableArray<ParameterSymbol> _lazyParameters;

        /// <summary>
        /// Return the extension method in reduced form if the extension method
        /// is applicable, and satisfies type parameter constraints, based on the
        /// "this" argument type. Otherwise, returns null.
        /// </summary>
        /// <param name="compilation">Compilation used to check constraints.
        /// The latest language version is assumed if this is null.</param>
        public static MethodSymbol Create(MethodSymbol method, TypeSymbol receiverType, CSharpCompilation compilation)
        {
            Debug.Assert(method.IsExtensionMethod && method.MethodKind != MethodKind.ReducedExtension);
            Debug.Assert(method.ParameterCount > 0);
            Debug.Assert((object)receiverType != null);

            HashSet<DiagnosticInfo> useSiteDiagnostics = null;

            method = InferExtensionMethodTypeArguments(method, receiverType, compilation, ref useSiteDiagnostics);
            if ((object)method == null)
            {
                return null;
            }

            var conversions = new TypeConversions(method.ContainingAssembly.CorLibrary);
            var conversion = conversions.ConvertExtensionMethodThisArg(method.Parameters[0].Type, receiverType, ref useSiteDiagnostics);
            if (!conversion.Exists)
            {
                return null;
            }

            if (useSiteDiagnostics != null)
            {
                foreach (var diag in useSiteDiagnostics)
                {
                    if (diag.Severity == DiagnosticSeverity.Error)
                    {
                        return null;
                    }
                }
            }

            return Create(method);
        }

        public static MethodSymbol Create(MethodSymbol method)
        {
            Debug.Assert(method.IsExtensionMethod && method.MethodKind != MethodKind.ReducedExtension);

            // The reduced form is always created from the unconstructed method symbol.
            var constructedFrom = method.ConstructedFrom;
            var reducedMethod = new ReducedExtensionMethodSymbol(constructedFrom);

            if (constructedFrom == method)
            {
                return reducedMethod;
            }

            // If the given method is a constructed method, the same type arguments
            // are applied to construct the result from the reduced form.
            Debug.Assert(!method.TypeArgumentsWithAnnotations.IsEmpty);
            return reducedMethod.Construct(method.TypeArgumentsWithAnnotations);
        }

        private ReducedExtensionMethodSymbol(MethodSymbol reducedFrom)
        {
            Debug.Assert((object)reducedFrom != null);
            Debug.Assert(reducedFrom.IsExtensionMethod);
            Debug.Assert((object)reducedFrom.ReducedFrom == null);
            Debug.Assert(reducedFrom.ConstructedFrom == reducedFrom);
            Debug.Assert(reducedFrom.ParameterCount > 0);

            _reducedFrom = reducedFrom;
            _typeMap = TypeMap.Empty.WithAlphaRename(reducedFrom, this, out _typeParameters);
            _typeArguments = _typeMap.SubstituteTypes(reducedFrom.TypeArgumentsWithAnnotations);
        }

        /// <summary>
        /// If the extension method is applicable based on the "this" argument type, return
        /// the method constructed with the inferred type arguments. If the method is not an
        /// unconstructed generic method, type inference is skipped. If the method is not
        /// applicable, or if constraints when inferring type parameters from the "this" type
        /// are not satisfied, the return value is null.
        /// </summary>
        /// <param name="compilation">Compilation used to check constraints.  The latest language version is assumed if this is null.</param>
        private static MethodSymbol InferExtensionMethodTypeArguments(MethodSymbol method, TypeSymbol thisType, CSharpCompilation compilation, ref HashSet<DiagnosticInfo> useSiteDiagnostics)
        {
            Debug.Assert(method.IsExtensionMethod);
            Debug.Assert((object)thisType != null);

            if (!method.IsGenericMethod || method != method.ConstructedFrom)
            {
                return method;
            }

            // We never resolve extension methods on a dynamic receiver.
            if (thisType.IsDynamic())
            {
                return null;
            }

            var containingAssembly = method.ContainingAssembly;
            var errorNamespace = containingAssembly.GlobalNamespace;
            var conversions = new TypeConversions(containingAssembly.CorLibrary);

            // There is absolutely no plausible syntax/tree that we could use for these
            // synthesized literals.  We could be speculatively binding a call to a PE method.
            var syntaxTree = CSharpSyntaxTree.Dummy;
            var syntax = (CSharpSyntaxNode)syntaxTree.GetRoot();

            // Create an argument value for the "this" argument of specific type,
            // and pass the same bad argument value for all other arguments.
            var thisArgumentValue = new BoundLiteral(syntax, ConstantValue.Bad, thisType) { WasCompilerGenerated = true };
            var otherArgumentType = new ExtendedErrorTypeSymbol(errorNamespace, name: string.Empty, arity: 0, errorInfo: null, unreported: false);
            var otherArgumentValue = new BoundLiteral(syntax, ConstantValue.Bad, otherArgumentType) { WasCompilerGenerated = true };

            var paramCount = method.ParameterCount;
            var arguments = new BoundExpression[paramCount];

            for (int i = 0; i < paramCount; i++)
            {
                var argument = (i == 0) ? thisArgumentValue : otherArgumentValue;
                arguments[i] = argument;
            }

            var typeArgs = MethodTypeInferrer.InferTypeArgumentsFromFirstArgument(
                conversions,
                method,
                arguments.AsImmutable(),
                useSiteDiagnostics: ref useSiteDiagnostics);

            if (typeArgs.IsDefault)
            {
                return null;
            }

            // For the purpose of constraint checks we use error type symbol in place of type arguments that we couldn't infer from the first argument.
            // This prevents constraint checking from failing for corresponding type parameters.
            int firstNullInTypeArgs = -1;
            var notInferredTypeParameters = PooledHashSet<TypeParameterSymbol>.GetInstance();
            var typeParams = method.TypeParameters;
            var typeArgsForConstraintsCheck = typeArgs;
            for (int i = 0; i < typeArgsForConstraintsCheck.Length; i++)
            {
                if (!typeArgsForConstraintsCheck[i].HasType)
                {
                    firstNullInTypeArgs = i;
                    var builder = ArrayBuilder<TypeWithAnnotations>.GetInstance();
                    builder.AddRange(typeArgsForConstraintsCheck, firstNullInTypeArgs);

                    for (; i < typeArgsForConstraintsCheck.Length; i++)
                    {
                        var typeArg = typeArgsForConstraintsCheck[i];
                        if (!typeArg.HasType)
                        {
                            notInferredTypeParameters.Add(typeParams[i]);
                            builder.Add(TypeWithAnnotations.Create(ErrorTypeSymbol.UnknownResultType));
                        }
                        else
                        {
                            builder.Add(typeArg);
                        }
                    }

                    typeArgsForConstraintsCheck = builder.ToImmutableAndFree();
                    break;
                }
            }

            // Check constraints.
            var diagnosticsBuilder = ArrayBuilder<TypeParameterDiagnosticInfo>.GetInstance();
            var substitution = new TypeMap(typeParams, typeArgsForConstraintsCheck);
            ArrayBuilder<TypeParameterDiagnosticInfo> useSiteDiagnosticsBuilder = null;
            var success = method.CheckConstraints(conversions, includeNullability: false, substitution, typeParams, typeArgsForConstraintsCheck, compilation, diagnosticsBuilder, nullabilityDiagnosticsBuilderOpt: null, ref useSiteDiagnosticsBuilder,
                                                  ignoreTypeConstraintsDependentOnTypeParametersOpt: notInferredTypeParameters.Count > 0 ? notInferredTypeParameters : null);
            diagnosticsBuilder.Free();
            notInferredTypeParameters.Free();

            if (useSiteDiagnosticsBuilder != null && useSiteDiagnosticsBuilder.Count > 0)
            {
                if (useSiteDiagnostics == null)
                {
                    useSiteDiagnostics = new HashSet<DiagnosticInfo>();
                }

                foreach (var diag in useSiteDiagnosticsBuilder)
                {
                    useSiteDiagnostics.Add(diag.DiagnosticInfo);
                }
            }

            if (!success)
            {
                return null;
            }

            // For the purpose of construction we use original type parameters in place of type arguments that we couldn't infer from the first argument.
            ImmutableArray<TypeWithAnnotations> typeArgsForConstruct = typeArgs;
            if (typeArgs.Any(t => !t.HasType))
            {
                typeArgsForConstruct = typeArgs.ZipAsArray(
                    method.TypeParameters,
                    (t, tp) => t.HasType ? t : TypeWithAnnotations.Create(tp));
            }

            return method.Construct(typeArgsForConstruct);
        }


        internal override MethodSymbol CallsiteReducedFromMethod
        {
            get { return _reducedFrom.ConstructIfGeneric(_typeArguments); }
        }

        public override TypeSymbol ReceiverType
        {
            get
            {
                return _reducedFrom.Parameters[0].Type;
            }
        }

        protected override CodeAnalysis.NullableAnnotation ReceiverNullableAnnotation =>
            _reducedFrom.Parameters[0].TypeWithAnnotations.ToPublicAnnotation();

        public override TypeSymbol GetTypeInferredDuringReduction(TypeParameterSymbol reducedFromTypeParameter)
        {
            if ((object)reducedFromTypeParameter == null)
            {
                throw new System.ArgumentNullException();
            }

            if (reducedFromTypeParameter.ContainingSymbol != _reducedFrom)
            {
                throw new System.ArgumentException();
            }

            return null;
        }

        public override MethodSymbol ReducedFrom
        {
            get { return _reducedFrom; }
        }

        public override MethodSymbol ConstructedFrom
        {
            get
            {
                Debug.Assert(_reducedFrom.ConstructedFrom == _reducedFrom);
                return this;
            }
        }

        public override ImmutableArray<TypeParameterSymbol> TypeParameters
        {
            get { return _typeParameters; }
        }

        public override ImmutableArray<TypeWithAnnotations> TypeArgumentsWithAnnotations
        {
            get { return _typeArguments; }
        }

        internal override Microsoft.Cci.CallingConvention CallingConvention
        {
            get { return _reducedFrom.CallingConvention; }
        }

        public override int Arity
        {
            get { return _reducedFrom.Arity; }
        }

        public override string Name
        {
            get { return _reducedFrom.Name; }
        }

        internal override bool HasSpecialName
        {
            get { return _reducedFrom.HasSpecialName; }
        }

        internal override System.Reflection.MethodImplAttributes ImplementationAttributes
        {
            get { return _reducedFrom.ImplementationAttributes; }
        }

        internal override bool RequiresSecurityObject
        {
            get { return _reducedFrom.RequiresSecurityObject; }
        }

        public override DllImportData GetDllImportData()
        {
            return _reducedFrom.GetDllImportData();
        }

        internal override MarshalPseudoCustomAttributeData ReturnValueMarshallingInformation
        {
            get { return _reducedFrom.ReturnValueMarshallingInformation; }
        }

        internal override bool HasDeclarativeSecurity
        {
            get { return _reducedFrom.HasDeclarativeSecurity; }
        }

        internal override IEnumerable<Microsoft.Cci.SecurityAttribute> GetSecurityInformation()
        {
            return _reducedFrom.GetSecurityInformation();
        }

        internal override ImmutableArray<string> GetAppliedConditionalSymbols()
        {
            return _reducedFrom.GetAppliedConditionalSymbols();
        }

        public override AssemblySymbol ContainingAssembly
        {
            get { return _reducedFrom.ContainingAssembly; }
        }

        public override ImmutableArray<Location> Locations
        {
            get { return _reducedFrom.Locations; }
        }

        public override ImmutableArray<SyntaxReference> DeclaringSyntaxReferences
        {
            get { return _reducedFrom.DeclaringSyntaxReferences; }
        }

        public override string GetDocumentationCommentXml(CultureInfo preferredCulture = null, bool expandIncludes = false, CancellationToken cancellationToken = default(CancellationToken))
        {
            return _reducedFrom.GetDocumentationCommentXml(preferredCulture, expandIncludes, cancellationToken);
        }

        public override MethodSymbol OriginalDefinition
        {
            get { return this; }
        }

        public override bool IsExtern
        {
            get { return _reducedFrom.IsExtern; }
        }

        public override bool IsSealed
        {
            get { return _reducedFrom.IsSealed; }
        }

        public override bool IsVirtual
        {
            get { return _reducedFrom.IsVirtual; }
        }

        public override bool IsAbstract
        {
            get { return _reducedFrom.IsAbstract; }
        }

        public override bool IsOverride
        {
            get { return _reducedFrom.IsOverride; }
        }

        public override bool IsStatic
        {
            get { return false; }
        }

        public override bool IsAsync
        {
            get { return _reducedFrom.IsAsync; }
        }

        public override bool IsExtensionMethod
        {
            get { return true; }
        }

        internal sealed override bool IsMetadataNewSlot(bool ignoreInterfaceImplementationChanges = false)
        {
            return false;
        }

        internal sealed override bool IsMetadataVirtual(bool ignoreInterfaceImplementationChanges = false)
        {
            return false;
        }

        internal override bool IsMetadataFinal
        {
            get
            {
                return false;
            }
        }

        internal sealed override ObsoleteAttributeData ObsoleteAttributeData
        {
            get { return _reducedFrom.ObsoleteAttributeData; }
        }

        public override Accessibility DeclaredAccessibility
        {
            get { return _reducedFrom.DeclaredAccessibility; }
        }

        public override Symbol ContainingSymbol
        {
            get { return _reducedFrom.ContainingSymbol; }
        }

        public override ImmutableArray<CSharpAttributeData> GetAttributes()
        {
            return _reducedFrom.GetAttributes();
        }

        public override Symbol AssociatedSymbol
        {
            get { return null; }
        }

        public override MethodKind MethodKind
        {
            get { return MethodKind.ReducedExtension; }
        }

        public override bool ReturnsVoid
        {
            get { return _reducedFrom.ReturnsVoid; }
        }

        public override bool IsGenericMethod
        {
            get { return _reducedFrom.IsGenericMethod; }
        }

        public override bool IsVararg
        {
            get { return _reducedFrom.IsVararg; }
        }

        public override RefKind RefKind
        {
            get { return _reducedFrom.RefKind; }
        }

        public override TypeWithAnnotations ReturnTypeWithAnnotations
        {
            get { return _typeMap.SubstituteType(_reducedFrom.ReturnTypeWithAnnotations); }
        }

        public override FlowAnalysisAnnotations ReturnTypeFlowAnalysisAnnotations => _reducedFrom.ReturnTypeFlowAnalysisAnnotations;

        public override ImmutableHashSet<string> ReturnNotNullIfParameterNotNull => _reducedFrom.ReturnNotNullIfParameterNotNull;

        public override FlowAnalysisAnnotations FlowAnalysisAnnotations => _reducedFrom.FlowAnalysisAnnotations;

        public override ImmutableArray<CustomModifier> RefCustomModifiers
        {
            get { return _typeMap.SubstituteCustomModifiers(_reducedFrom.RefCustomModifiers); }
        }

        internal override int ParameterCount
        {
            get { return _reducedFrom.ParameterCount - 1; }
        }

        internal override bool GenerateDebugInfo
        {
            get { return _reducedFrom.GenerateDebugInfo; }
        }

        public override ImmutableArray<ParameterSymbol> Parameters
        {
            get
            {
                if (_lazyParameters.IsDefault)
                {
                    ImmutableInterlocked.InterlockedCompareExchange(ref _lazyParameters, this.MakeParameters(), default(ImmutableArray<ParameterSymbol>));
                }
                return _lazyParameters;
            }
        }

        internal override bool IsExplicitInterfaceImplementation
        {
            get { return false; }
        }

        internal override bool IsDeclaredReadOnly => false;

        internal override bool IsEffectivelyReadOnly => _reducedFrom.Parameters[0].RefKind == RefKind.In;

        public override ImmutableArray<MethodSymbol> ExplicitInterfaceImplementations
        {
            get { return ImmutableArray<MethodSymbol>.Empty; }
        }

        public override bool HidesBaseMethodsByName
        {
            get { return false; }
        }

        internal override bool CallsAreOmitted(SyntaxTree syntaxTree)
        {
            return _reducedFrom.CallsAreOmitted(syntaxTree);
        }

        private ImmutableArray<ParameterSymbol> MakeParameters()
        {
            var reducedFromParameters = _reducedFrom.Parameters;
            int count = reducedFromParameters.Length;

            if (count <= 1)
            {
                Debug.Assert(count == 1);
                return ImmutableArray<ParameterSymbol>.Empty;
            }
            else
            {
                var parameters = new ParameterSymbol[count - 1];
                for (int i = 0; i < count - 1; i++)
                {
                    parameters[i] = new ReducedExtensionMethodParameterSymbol(this, reducedFromParameters[i + 1]);
                }

                return parameters.AsImmutableOrNull();
            }
        }

        internal override int CalculateLocalSyntaxOffset(int localPosition, SyntaxTree localTree)
        {
            throw ExceptionUtilities.Unreachable;
        }

        public override bool Equals(Symbol obj, TypeCompareKind compareKind)
        {
            if ((object)this == obj) return true;

            ReducedExtensionMethodSymbol other = obj as ReducedExtensionMethodSymbol;
            return (object)other != null && _reducedFrom.Equals(other._reducedFrom, compareKind);
        }

        public override int GetHashCode()
        {
            return _reducedFrom.GetHashCode();
        }

        private sealed class ReducedExtensionMethodParameterSymbol : WrappedParameterSymbol
        {
            private readonly ReducedExtensionMethodSymbol _containingMethod;

            public ReducedExtensionMethodParameterSymbol(ReducedExtensionMethodSymbol containingMethod, ParameterSymbol underlyingParameter) :
                base(underlyingParameter)
            {
                Debug.Assert(underlyingParameter.Ordinal > 0);
                _containingMethod = containingMethod;
            }

            public override Symbol ContainingSymbol
            {
                get { return _containingMethod; }
            }

            public override int Ordinal
            {
                get { return this._underlyingParameter.Ordinal - 1; }
            }

            public override TypeWithAnnotations TypeWithAnnotations
            {
                get { return _containingMethod._typeMap.SubstituteType(this._underlyingParameter.TypeWithAnnotations); }
            }

            public override ImmutableArray<CustomModifier> RefCustomModifiers
            {
                get
                {
                    return _containingMethod._typeMap.SubstituteCustomModifiers(this._underlyingParameter.RefCustomModifiers);
                }
            }

            public sealed override bool Equals(Symbol obj, TypeCompareKind compareKind)
            {
                if ((object)this == obj)
                {
                    return true;
                }

                // Equality of ordinal and containing symbol is a correct
                // implementation for all ParameterSymbols, but we don't 
                // define it on the base type because most can simply use
                // ReferenceEquals.

                var other = obj as ReducedExtensionMethodParameterSymbol;
                return (object)other != null &&
                    this.Ordinal == other.Ordinal &&
                    this.ContainingSymbol.Equals(other.ContainingSymbol, compareKind);
            }

            public sealed override int GetHashCode()
            {
                return Hash.Combine(ContainingSymbol, _underlyingParameter.Ordinal);
            }
        }
    }
}
