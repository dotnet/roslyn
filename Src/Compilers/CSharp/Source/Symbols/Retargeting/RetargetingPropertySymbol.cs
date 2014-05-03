// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Globalization;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Emit;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Symbols.Retargeting
{
    internal sealed class RetargetingPropertySymbol : PropertySymbol
    {
        /// <summary>
        /// Owning RetargetingModuleSymbol.
        /// </summary>
        private readonly RetargetingModuleSymbol retargetingModule;

        /// <summary>
        /// The underlying PropertySymbol, cannot be another RetargetingPropertySymbol.
        /// </summary>
        private readonly PropertySymbol underlyingProperty;

        //we want to compute this lazily since it may be expensive for the underlying symbol
        private ImmutableArray<PropertySymbol> lazyExplicitInterfaceImplementations;
        private ImmutableArray<ParameterSymbol> lazyParameters;
        private ImmutableArray<CustomModifier> lazyTypeCustomModifiers;

        /// <summary>
        /// Retargeted custom attributes
        /// </summary>
        private ImmutableArray<CSharpAttributeData> lazyCustomAttributes;

        private DiagnosticInfo lazyUseSiteDiagnostic = CSDiagnosticInfo.EmptyErrorInfo; // Indicates unknown state. 

        private TypeSymbol lazyType;

        public RetargetingPropertySymbol(RetargetingModuleSymbol retargetingModule, PropertySymbol underlyingProperty)
        {
            Debug.Assert((object)retargetingModule != null);
            Debug.Assert((object)underlyingProperty != null);
            Debug.Assert(!(underlyingProperty is RetargetingPropertySymbol));

            this.retargetingModule = retargetingModule;
            this.underlyingProperty = underlyingProperty;
        }

        private RetargetingModuleSymbol.RetargetingSymbolTranslator RetargetingTranslator
        {
            get
            {
                return retargetingModule.RetargetingTranslator;
            }
        }

        public PropertySymbol UnderlyingProperty
        {
            get
            {
                return this.underlyingProperty;
            }
        }

        public override bool IsImplicitlyDeclared
        {
            get { return underlyingProperty.IsImplicitlyDeclared; }
        }

        public RetargetingModuleSymbol RetargetingModule
        {
            get
            {
                return this.retargetingModule;
            }
        }

        public override TypeSymbol Type
        {
            get
            {
                if ((object)this.lazyType == null)
                {
                    var type = this.RetargetingTranslator.Retarget(this.underlyingProperty.Type, RetargetOptions.RetargetPrimitiveTypesByTypeCode);
                    this.lazyType = type.AsDynamicIfNoPia(this.ContainingType);
                }
                return this.lazyType;
            }
        }

        public override ImmutableArray<CustomModifier> TypeCustomModifiers
        {
            get
            {
                return RetargetingTranslator.RetargetModifiers(
                    underlyingProperty.TypeCustomModifiers,
                    ref lazyTypeCustomModifiers);
            }
        }

        public override ImmutableArray<ParameterSymbol> Parameters
        {
            get
            {
                if (lazyParameters.IsDefault)
                {
                    ImmutableInterlocked.InterlockedCompareExchange(ref lazyParameters, this.RetargetParameters(), default(ImmutableArray<ParameterSymbol>));
                }

                return lazyParameters;
            }
        }

        private ImmutableArray<ParameterSymbol> RetargetParameters()
        {
            var list = this.underlyingProperty.Parameters;
            int count = list.Length;

            if (count == 0)
            {
                return ImmutableArray<ParameterSymbol>.Empty;
            }
            else
            {
                ParameterSymbol[] parameters = new ParameterSymbol[count];

                for (int i = 0; i < count; i++)
                {
                    parameters[i] = new RetargetingPropertyParameterSymbol(this, list[i]);
                }

                return parameters.AsImmutableOrNull();
            }
        }

        public override bool IsIndexer
        {
            get
            {
                return underlyingProperty.IsIndexer;
            }
        }

        public override MethodSymbol GetMethod
        {
            get
            {
                return (object)underlyingProperty.GetMethod == null
                    ? null
                    : this.RetargetingTranslator.Retarget(underlyingProperty.GetMethod);
            }
        }

        public override MethodSymbol SetMethod
        {
            get
            {
                return (object)underlyingProperty.SetMethod == null
                    ? null
                    : this.RetargetingTranslator.Retarget(underlyingProperty.SetMethod);
            }
        }

        internal override Microsoft.Cci.CallingConvention CallingConvention
        {
            get
            {
                return this.underlyingProperty.CallingConvention;
            }
        }

        internal override bool IsExplicitInterfaceImplementation
        {
            get
            {
                return this.underlyingProperty.IsExplicitInterfaceImplementation;
            }
        }

        public override ImmutableArray<PropertySymbol> ExplicitInterfaceImplementations
        {
            get
            {
                if (lazyExplicitInterfaceImplementations.IsDefault)
                {
                    ImmutableInterlocked.InterlockedCompareExchange(
                        ref lazyExplicitInterfaceImplementations,
                        this.RetargetExplicitInterfaceImplementations(),
                        default(ImmutableArray<PropertySymbol>));
                }
                return lazyExplicitInterfaceImplementations;
            }
        }

        private ImmutableArray<PropertySymbol> RetargetExplicitInterfaceImplementations()
        {
            var impls = this.underlyingProperty.ExplicitInterfaceImplementations;

            if (impls.IsEmpty)
            {
                Debug.Assert(!impls.IsDefault);
                return impls;
            }

            // CONSIDER: we could skip the builder until the first time we see a different method after retargeting

            var builder = ArrayBuilder<PropertySymbol>.GetInstance();

            for (int i = 0; i < impls.Length; i++)
            {
                var retargeted = this.RetargetingTranslator.Retarget(impls[i], MemberSignatureComparer.RetargetedExplicitImplementationComparer);
                if ((object)retargeted != null)
                {
                    builder.Add(retargeted);
                }
            }

            return builder.ToImmutableAndFree();
        }

        public override Symbol ContainingSymbol
        {
            get
            {
                return this.RetargetingTranslator.Retarget(this.underlyingProperty.ContainingSymbol);
            }
        }

        public override AssemblySymbol ContainingAssembly
        {
            get
            {
                return this.retargetingModule.ContainingAssembly;
            }
        }

        internal override ModuleSymbol ContainingModule
        {
            get
            {
                return this.retargetingModule;
            }
        }

        public override string Name
        {
            get
            {
                return this.underlyingProperty.Name;
            }
        }

        internal override bool HasSpecialName
        {
            get
            {
                return this.underlyingProperty.HasSpecialName;
            }
        }

        public override string GetDocumentationCommentXml(CultureInfo preferredCulture = null, bool expandIncludes = false, CancellationToken cancellationToken = default(CancellationToken))
        {
            return this.underlyingProperty.GetDocumentationCommentXml(preferredCulture, expandIncludes, cancellationToken);
        }

        public override ImmutableArray<Location> Locations
        {
            get
            {
                return this.underlyingProperty.Locations;
            }
        }

        public override ImmutableArray<SyntaxReference> DeclaringSyntaxReferences
        {
            get
            {
                return this.UnderlyingProperty.DeclaringSyntaxReferences;
            }
        }

        public override Accessibility DeclaredAccessibility
        {
            get
            {
                return this.underlyingProperty.DeclaredAccessibility;
            }
        }

        public override bool IsStatic
        {
            get
            {
                return underlyingProperty.IsStatic;
            }
        }

        public override bool IsVirtual
        {
            get
            {
                return underlyingProperty.IsVirtual;
            }
        }

        public override bool IsOverride
        {
            get
            {
                return underlyingProperty.IsOverride;
            }
        }

        public override bool IsAbstract
        {
            get
            {
                return underlyingProperty.IsAbstract;
            }
        }

        public override bool IsSealed
        {
            get
            {
                return underlyingProperty.IsSealed;
            }
        }

        public override bool IsExtern
        {
            get
            {
                return underlyingProperty.IsExtern;
            }
        }

        internal override ObsoleteAttributeData ObsoleteAttributeData
        {
            get
            {
                return underlyingProperty.ObsoleteAttributeData;
            }
        }

        public override ImmutableArray<CSharpAttributeData> GetAttributes()
        {
            return this.RetargetingTranslator.GetRetargetedAttributes(this.underlyingProperty.GetAttributes(), ref this.lazyCustomAttributes);
        }

        internal override IEnumerable<CSharpAttributeData> GetCustomAttributesToEmit(ModuleCompilationState compilationState)
        {
            return this.RetargetingTranslator.RetargetAttributes(this.underlyingProperty.GetCustomAttributesToEmit(compilationState));
        }

        internal override bool MustCallMethodsDirectly
        {
            get
            {
                return underlyingProperty.MustCallMethodsDirectly;
            }
        }

        internal override DiagnosticInfo GetUseSiteDiagnostic()
        {
            if (ReferenceEquals(lazyUseSiteDiagnostic, CSDiagnosticInfo.EmptyErrorInfo))
            {
                DiagnosticInfo result = null;
                CalculateUseSiteDiagnostic(ref result);
                lazyUseSiteDiagnostic = result;
            }

            return lazyUseSiteDiagnostic;
        }

        public override string MetadataName
        {
            // We'll never emit this symbol, so it doesn't really
            // make sense for it to have a metadata name.  However, all
            // symbols have an implementation of MetadataName (since it
            // is virtual on Symbol) so we might as well define it in a
            // consistent way.

            get
            {
                return underlyingProperty.MetadataName;
            }
        }
        internal override bool HasRuntimeSpecialName
        {
            get
            {
                return underlyingProperty.HasRuntimeSpecialName;
            }
        }

        internal sealed override CSharpCompilation DeclaringCompilation // perf, not correctness
        {
            get { return null; }
        }
    }
}