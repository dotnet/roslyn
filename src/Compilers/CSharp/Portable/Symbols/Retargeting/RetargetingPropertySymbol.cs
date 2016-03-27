// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
        private readonly RetargetingModuleSymbol _retargetingModule;

        /// <summary>
        /// The underlying PropertySymbol, cannot be another RetargetingPropertySymbol.
        /// </summary>
        private readonly PropertySymbol _underlyingProperty;

        //we want to compute this lazily since it may be expensive for the underlying symbol
        private ImmutableArray<PropertySymbol> _lazyExplicitInterfaceImplementations;
        private ImmutableArray<ParameterSymbol> _lazyParameters;

        /// <summary>
        /// Retargeted custom attributes
        /// </summary>
        private ImmutableArray<CSharpAttributeData> _lazyCustomAttributes;

        private DiagnosticInfo _lazyUseSiteDiagnostic = CSDiagnosticInfo.EmptyErrorInfo; // Indicates unknown state. 

        private TypeSymbolWithAnnotations _lazyType;

        public RetargetingPropertySymbol(RetargetingModuleSymbol retargetingModule, PropertySymbol underlyingProperty)
        {
            Debug.Assert((object)retargetingModule != null);
            Debug.Assert((object)underlyingProperty != null);
            Debug.Assert(!(underlyingProperty is RetargetingPropertySymbol));

            _retargetingModule = retargetingModule;
            _underlyingProperty = underlyingProperty;
        }

        private RetargetingModuleSymbol.RetargetingSymbolTranslator RetargetingTranslator
        {
            get
            {
                return _retargetingModule.RetargetingTranslator;
            }
        }

        public PropertySymbol UnderlyingProperty
        {
            get
            {
                return _underlyingProperty;
            }
        }

        public override bool IsImplicitlyDeclared
        {
            get { return _underlyingProperty.IsImplicitlyDeclared; }
        }

        public RetargetingModuleSymbol RetargetingModule
        {
            get
            {
                return _retargetingModule;
            }
        }

        internal override RefKind RefKind
        {
            get
            {
                return _underlyingProperty.RefKind;
            }
        }

        public override TypeSymbolWithAnnotations Type
        {
            get
            {
                if ((object)_lazyType == null)
                {
                    _lazyType = this.RetargetingTranslator.Retarget(_underlyingProperty.Type, RetargetOptions.RetargetPrimitiveTypesByTypeCode, this.ContainingType);
                }

                return _lazyType;
            }
        }

        public override ImmutableArray<ParameterSymbol> Parameters
        {
            get
            {
                if (_lazyParameters.IsDefault)
                {
                    ImmutableInterlocked.InterlockedCompareExchange(ref _lazyParameters, this.RetargetParameters(), default(ImmutableArray<ParameterSymbol>));
                }

                return _lazyParameters;
            }
        }

        private ImmutableArray<ParameterSymbol> RetargetParameters()
        {
            var list = _underlyingProperty.Parameters;
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
                return _underlyingProperty.IsIndexer;
            }
        }

        public override MethodSymbol GetMethod
        {
            get
            {
                return (object)_underlyingProperty.GetMethod == null
                    ? null
                    : this.RetargetingTranslator.Retarget(_underlyingProperty.GetMethod);
            }
        }

        public override MethodSymbol SetMethod
        {
            get
            {
                return (object)_underlyingProperty.SetMethod == null
                    ? null
                    : this.RetargetingTranslator.Retarget(_underlyingProperty.SetMethod);
            }
        }

        internal override Microsoft.Cci.CallingConvention CallingConvention
        {
            get
            {
                return _underlyingProperty.CallingConvention;
            }
        }

        internal override bool IsExplicitInterfaceImplementation
        {
            get
            {
                return _underlyingProperty.IsExplicitInterfaceImplementation;
            }
        }

        public override ImmutableArray<PropertySymbol> ExplicitInterfaceImplementations
        {
            get
            {
                if (_lazyExplicitInterfaceImplementations.IsDefault)
                {
                    ImmutableInterlocked.InterlockedCompareExchange(
                        ref _lazyExplicitInterfaceImplementations,
                        this.RetargetExplicitInterfaceImplementations(),
                        default(ImmutableArray<PropertySymbol>));
                }
                return _lazyExplicitInterfaceImplementations;
            }
        }

        private ImmutableArray<PropertySymbol> RetargetExplicitInterfaceImplementations()
        {
            var impls = _underlyingProperty.ExplicitInterfaceImplementations;

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
                return this.RetargetingTranslator.Retarget(_underlyingProperty.ContainingSymbol);
            }
        }

        public override AssemblySymbol ContainingAssembly
        {
            get
            {
                return _retargetingModule.ContainingAssembly;
            }
        }

        internal override ModuleSymbol ContainingModule
        {
            get
            {
                return _retargetingModule;
            }
        }

        public override string Name
        {
            get
            {
                return _underlyingProperty.Name;
            }
        }

        internal override bool HasSpecialName
        {
            get
            {
                return _underlyingProperty.HasSpecialName;
            }
        }

        public override string GetDocumentationCommentXml(CultureInfo preferredCulture = null, bool expandIncludes = false, CancellationToken cancellationToken = default(CancellationToken))
        {
            return _underlyingProperty.GetDocumentationCommentXml(preferredCulture, expandIncludes, cancellationToken);
        }

        public override ImmutableArray<Location> Locations
        {
            get
            {
                return _underlyingProperty.Locations;
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
                return _underlyingProperty.DeclaredAccessibility;
            }
        }

        public override bool IsStatic
        {
            get
            {
                return _underlyingProperty.IsStatic;
            }
        }

        public override bool IsVirtual
        {
            get
            {
                return _underlyingProperty.IsVirtual;
            }
        }

        public override bool IsOverride
        {
            get
            {
                return _underlyingProperty.IsOverride;
            }
        }

        public override bool IsAbstract
        {
            get
            {
                return _underlyingProperty.IsAbstract;
            }
        }

        public override bool IsSealed
        {
            get
            {
                return _underlyingProperty.IsSealed;
            }
        }

        public override bool IsExtern
        {
            get
            {
                return _underlyingProperty.IsExtern;
            }
        }

        internal override ObsoleteAttributeData ObsoleteAttributeData
        {
            get
            {
                return _underlyingProperty.ObsoleteAttributeData;
            }
        }

        public override ImmutableArray<CSharpAttributeData> GetAttributes()
        {
            return this.RetargetingTranslator.GetRetargetedAttributes(_underlyingProperty.GetAttributes(), ref _lazyCustomAttributes);
        }

        internal override IEnumerable<CSharpAttributeData> GetCustomAttributesToEmit(ModuleCompilationState compilationState)
        {
            return this.RetargetingTranslator.RetargetAttributes(_underlyingProperty.GetCustomAttributesToEmit(compilationState));
        }

        internal override bool MustCallMethodsDirectly
        {
            get
            {
                return _underlyingProperty.MustCallMethodsDirectly;
            }
        }

        internal override DiagnosticInfo GetUseSiteDiagnostic()
        {
            if (ReferenceEquals(_lazyUseSiteDiagnostic, CSDiagnosticInfo.EmptyErrorInfo))
            {
                DiagnosticInfo result = null;
                CalculateUseSiteDiagnostic(ref result);
                _lazyUseSiteDiagnostic = result;
            }

            return _lazyUseSiteDiagnostic;
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
                return _underlyingProperty.MetadataName;
            }
        }
        internal override bool HasRuntimeSpecialName
        {
            get
            {
                return _underlyingProperty.HasRuntimeSpecialName;
            }
        }

        internal sealed override CSharpCompilation DeclaringCompilation // perf, not correctness
        {
            get { return null; }
        }
    }
}
