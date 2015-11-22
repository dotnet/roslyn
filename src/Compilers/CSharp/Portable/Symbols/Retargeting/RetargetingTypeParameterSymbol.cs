// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;
using System.Diagnostics;
using System.Globalization;

namespace Microsoft.CodeAnalysis.CSharp.Symbols.Retargeting
{
    /// <summary>
    /// Represents a type parameter in a RetargetingModuleSymbol. Essentially this is a wrapper around 
    /// another TypeParameterSymbol that is responsible for retargeting symbols from one assembly to another. 
    /// It can retarget symbols for multiple assemblies at the same time.
    /// </summary>
    internal sealed class RetargetingTypeParameterSymbol
        : TypeParameterSymbol
    {
        /// <summary>
        /// Owning RetargetingModuleSymbol.
        /// </summary>
        private readonly RetargetingModuleSymbol _retargetingModule;

        /// <summary>
        /// The underlying TypeParameterSymbol, cannot be another RetargetingTypeParameterSymbol.
        /// </summary>
        private readonly TypeParameterSymbol _underlyingTypeParameter;

        /// <summary>
        /// Retargeted custom attributes
        /// </summary>
        private ImmutableArray<CSharpAttributeData> _lazyCustomAttributes;

        public RetargetingTypeParameterSymbol(RetargetingModuleSymbol retargetingModule, TypeParameterSymbol underlyingTypeParameter)
        {
            Debug.Assert((object)retargetingModule != null);
            Debug.Assert((object)underlyingTypeParameter != null);
            Debug.Assert(!(underlyingTypeParameter is RetargetingTypeParameterSymbol));

            _retargetingModule = retargetingModule;
            _underlyingTypeParameter = underlyingTypeParameter;
        }

        private RetargetingModuleSymbol.RetargetingSymbolTranslator RetargetingTranslator
        {
            get
            {
                return _retargetingModule.RetargetingTranslator;
            }
        }

        public TypeParameterSymbol UnderlyingTypeParameter
        {
            get
            {
                return _underlyingTypeParameter;
            }
        }

        public override bool IsImplicitlyDeclared
        {
            get { return _underlyingTypeParameter.IsImplicitlyDeclared; }
        }

        public override TypeParameterKind TypeParameterKind
        {
            get
            {
                return _underlyingTypeParameter.TypeParameterKind;
            }
        }

        public override int Ordinal
        {
            get
            {
                return _underlyingTypeParameter.Ordinal;
            }
        }

        public override bool HasConstructorConstraint
        {
            get
            {
                return _underlyingTypeParameter.HasConstructorConstraint;
            }
        }

        public override bool HasReferenceTypeConstraint
        {
            get
            {
                return _underlyingTypeParameter.HasReferenceTypeConstraint;
            }
        }

        public override bool HasValueTypeConstraint
        {
            get
            {
                return _underlyingTypeParameter.HasValueTypeConstraint;
            }
        }

        public override VarianceKind Variance
        {
            get
            {
                return _underlyingTypeParameter.Variance;
            }
        }

        public override Symbol ContainingSymbol
        {
            get
            {
                return this.RetargetingTranslator.Retarget(_underlyingTypeParameter.ContainingSymbol);
            }
        }

        public override ImmutableArray<Location> Locations
        {
            get
            {
                return _underlyingTypeParameter.Locations;
            }
        }

        public override ImmutableArray<SyntaxReference> DeclaringSyntaxReferences
        {
            get
            {
                return _underlyingTypeParameter.DeclaringSyntaxReferences;
            }
        }

        public override ImmutableArray<CSharpAttributeData> GetAttributes()
        {
            return this.RetargetingTranslator.GetRetargetedAttributes(_underlyingTypeParameter.GetAttributes(), ref _lazyCustomAttributes);
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
                return _underlyingTypeParameter.Name;
            }
        }

        public override string GetDocumentationCommentXml(CultureInfo preferredCulture = null, bool expandIncludes = false, CancellationToken cancellationToken = default(CancellationToken))
        {
            return _underlyingTypeParameter.GetDocumentationCommentXml(preferredCulture, expandIncludes, cancellationToken);
        }

        internal override void EnsureAllConstraintsAreResolved()
        {
            _underlyingTypeParameter.EnsureAllConstraintsAreResolved();
        }

        internal override ImmutableArray<TypeSymbolWithAnnotations> GetConstraintTypes(ConsList<TypeParameterSymbol> inProgress)
        {
            return this.RetargetingTranslator.Retarget(_underlyingTypeParameter.GetConstraintTypes(inProgress));
        }

        internal override ImmutableArray<NamedTypeSymbol> GetInterfaces(ConsList<TypeParameterSymbol> inProgress)
        {
            return this.RetargetingTranslator.Retarget(_underlyingTypeParameter.GetInterfaces(inProgress));
        }

        internal override NamedTypeSymbol GetEffectiveBaseClass(ConsList<TypeParameterSymbol> inProgress)
        {
            return this.RetargetingTranslator.Retarget(_underlyingTypeParameter.GetEffectiveBaseClass(inProgress), RetargetOptions.RetargetPrimitiveTypesByTypeCode);
        }

        internal override TypeSymbol GetDeducedBaseType(ConsList<TypeParameterSymbol> inProgress)
        {
            return this.RetargetingTranslator.Retarget(_underlyingTypeParameter.GetDeducedBaseType(inProgress), RetargetOptions.RetargetPrimitiveTypesByTypeCode);
        }

        internal sealed override CSharpCompilation DeclaringCompilation // perf, not correctness
        {
            get { return null; }
        }
    }
}
