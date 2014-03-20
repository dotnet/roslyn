// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Globalization;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    /// <summary>
    /// An extension method with the "this" parameter removed.
    /// Used for the public binding API only, not for compilation.
    /// </summary>
    internal sealed class ReducedExtensionMethodSymbol : MethodSymbol
    {
        private readonly MethodSymbol reducedFrom;
        private readonly TypeMap typeMap;
        private readonly ImmutableArray<TypeParameterSymbol> typeParameters;
        private readonly ImmutableArray<TypeSymbol> typeArguments;
        private ImmutableArray<ParameterSymbol> lazyParameters;

        /// <summary>
        /// Return the extension method in reduced form if the extension method
        /// is applicable, and satisfies type parameter constraints, based on the
        /// "this" argument type. Otherwise, returns null.
        /// </summary>
        public static MethodSymbol Create(MethodSymbol method, TypeSymbol receiverType, Compilation compilation)
        {
            Debug.Assert(method.IsExtensionMethod && method.MethodKind != MethodKind.ReducedExtension);
            Debug.Assert(method.ParameterCount > 0);
            Debug.Assert((object)receiverType != null);

            HashSet<DiagnosticInfo> useSiteDiagnostics = null;

            method = method.InferExtensionMethodTypeArguments(receiverType, compilation, ref useSiteDiagnostics);
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
            Debug.Assert(!method.TypeArguments.IsEmpty);
            return reducedMethod.Construct(method.TypeArguments);
        }

        private ReducedExtensionMethodSymbol(MethodSymbol reducedFrom)
        {
            Debug.Assert((object)reducedFrom != null);
            Debug.Assert(reducedFrom.IsExtensionMethod);
            Debug.Assert((object)reducedFrom.ReducedFrom == null);
            Debug.Assert(reducedFrom.ConstructedFrom == reducedFrom);
            Debug.Assert(reducedFrom.ParameterCount > 0);

            this.reducedFrom = reducedFrom;
            this.typeMap = TypeMap.Empty.WithAlphaRename(reducedFrom, this, out this.typeParameters);
            this.typeArguments = this.typeMap.SubstituteTypes(reducedFrom.TypeArguments);
        }

        internal override MethodSymbol CallsiteReducedFromMethod
        {
            get { return this.reducedFrom.ConstructIfGeneric(this.typeArguments); }
        }

        public override TypeSymbol ReceiverType
        {
            get
            {
                return this.reducedFrom.Parameters[0].Type;
            }
        }

        public override TypeSymbol GetTypeInferredDuringReduction(TypeParameterSymbol reducedFromTypeParameter)
        {
            if ((object)reducedFromTypeParameter == null)
            {
                throw new System.ArgumentNullException();
            }

            if (reducedFromTypeParameter.ContainingSymbol != this.reducedFrom)
            {
                throw new System.ArgumentException();
            }

            return null;
        }

        public override MethodSymbol ReducedFrom
        {
            get { return this.reducedFrom; }
        }

        public override MethodSymbol ConstructedFrom
        {
            get
            {
                Debug.Assert(this.reducedFrom.ConstructedFrom == this.reducedFrom);
                return this;
            }
        }

        public override ImmutableArray<TypeParameterSymbol> TypeParameters
        {
            get { return this.typeParameters; }
        }

        public override ImmutableArray<TypeSymbol> TypeArguments
        {
            get { return this.typeArguments; }
        }

        internal override Microsoft.Cci.CallingConvention CallingConvention
        {
            get { return this.reducedFrom.CallingConvention; }
        }

        public override int Arity
        {
            get { return this.reducedFrom.Arity; }
        }

        public override string Name
        {
            get { return this.reducedFrom.Name; }
        }

        internal override bool HasSpecialName
        {
            get { return this.reducedFrom.HasSpecialName; }
        }

        internal override System.Reflection.MethodImplAttributes ImplementationAttributes
        {
            get { return this.reducedFrom.ImplementationAttributes; }
        }

        internal override bool RequiresSecurityObject
        {
            get { return this.reducedFrom.RequiresSecurityObject; }
        }

        public override DllImportData GetDllImportData()
        {
            return this.reducedFrom.GetDllImportData();
        }

        internal override MarshalPseudoCustomAttributeData ReturnValueMarshallingInformation
        {
            get { return this.reducedFrom.ReturnValueMarshallingInformation; }
        }

        internal override bool HasDeclarativeSecurity
        {
            get { return this.reducedFrom.HasDeclarativeSecurity; }
        }

        internal override IEnumerable<Microsoft.Cci.SecurityAttribute> GetSecurityInformation()
        {
            return this.reducedFrom.GetSecurityInformation();
        }

        internal override ImmutableArray<string> GetAppliedConditionalSymbols()
        {
            return this.reducedFrom.GetAppliedConditionalSymbols();
        }

        public override AssemblySymbol ContainingAssembly
        {
            get { return this.reducedFrom.ContainingAssembly; }
        }

        public override ImmutableArray<Location> Locations
        {
            get { return this.reducedFrom.Locations; }
        }

        public override ImmutableArray<SyntaxReference> DeclaringSyntaxReferences
        {
            get { return this.reducedFrom.DeclaringSyntaxReferences; }
        }

        public override string GetDocumentationCommentXml(CultureInfo preferredCulture = null, bool expandIncludes = false, CancellationToken cancellationToken = default(CancellationToken))
        {
            return this.reducedFrom.GetDocumentationCommentXml(preferredCulture, expandIncludes, cancellationToken);
        }

        public override MethodSymbol OriginalDefinition
        {
            get { return this; }
        }

        public override bool IsExtern
        {
            get { return this.reducedFrom.IsExtern; }
        }

        public override bool IsSealed
        {
            get { return this.reducedFrom.IsSealed; }
        }

        public override bool IsVirtual
        {
            get { return this.reducedFrom.IsVirtual; }
        }

        public override bool IsAbstract
        {
            get { return this.reducedFrom.IsAbstract; }
        }

        public override bool IsOverride
        {
            get { return this.reducedFrom.IsOverride; }
        }

        public override bool IsStatic
        {
            get { return false; }
        }

        public override bool IsAsync
        {
            get { return this.reducedFrom.IsAsync; }
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

        internal override bool IsMetadataFinal()
        {
            return false;
        }

        internal sealed override ObsoleteAttributeData ObsoleteAttributeData
        {
            get { return reducedFrom.ObsoleteAttributeData; }
        }

        public override Accessibility DeclaredAccessibility
        {
            get { return this.reducedFrom.DeclaredAccessibility; }
        }

        public override Symbol ContainingSymbol
        {
            get { return this.reducedFrom.ContainingSymbol; }
        }

        public override ImmutableArray<CSharpAttributeData> GetAttributes()
        {
            return this.reducedFrom.GetAttributes();
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
            get { return this.reducedFrom.ReturnsVoid; }
        }

        public override bool IsGenericMethod
        {
            get { return this.reducedFrom.IsGenericMethod; }
        }

        public override bool IsVararg
        {
            get { return this.reducedFrom.IsVararg; }
        }

        public override TypeSymbol ReturnType
        {
            get { return this.typeMap.SubstituteType(this.reducedFrom.ReturnType); }
        }

        public override ImmutableArray<CustomModifier> ReturnTypeCustomModifiers
        {
            get { return this.reducedFrom.ReturnTypeCustomModifiers; }
        }

        internal override int ParameterCount
        {
            get { return this.reducedFrom.ParameterCount - 1; }
        }

        internal override bool GenerateDebugInfo
        {
            get { return this.reducedFrom.GenerateDebugInfo; }
        }

        public override ImmutableArray<ParameterSymbol> Parameters
        {
            get
            {
                if (this.lazyParameters.IsDefault)
                {
                    ImmutableInterlocked.InterlockedCompareExchange(ref this.lazyParameters, this.MakeParameters(), default(ImmutableArray<ParameterSymbol>));
                }
                return this.lazyParameters;
            }
        }

        internal override bool IsExplicitInterfaceImplementation
        {
            get { return false; }
        }

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
            return this.reducedFrom.CallsAreOmitted(syntaxTree);
        }

        private ImmutableArray<ParameterSymbol> MakeParameters()
        {
            var reducedFromParameters = this.reducedFrom.Parameters;
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

        public override bool Equals(object obj)
        {
            if ((object)this == obj) return true;

            ReducedExtensionMethodSymbol other = obj as ReducedExtensionMethodSymbol;
            return (object)other != null && reducedFrom.Equals(other.reducedFrom);
        }

        public override int GetHashCode()
        {
            return reducedFrom.GetHashCode();
        }

        private sealed class ReducedExtensionMethodParameterSymbol : WrappedParameterSymbol
        {
            private readonly ReducedExtensionMethodSymbol containingMethod;

            public ReducedExtensionMethodParameterSymbol(ReducedExtensionMethodSymbol containingMethod, ParameterSymbol underlyingParameter) :
                base(underlyingParameter)
            {
                Debug.Assert(underlyingParameter.Ordinal > 0);
                this.containingMethod = containingMethod;
            }

            public override Symbol ContainingSymbol
            {
                get { return this.containingMethod; }
            }

            public override int Ordinal
            {
                get { return this.underlyingParameter.Ordinal - 1; }
            }

            public override TypeSymbol Type
            {
                get { return this.containingMethod.typeMap.SubstituteType(this.underlyingParameter.Type); }
            }
        }
    }
}
