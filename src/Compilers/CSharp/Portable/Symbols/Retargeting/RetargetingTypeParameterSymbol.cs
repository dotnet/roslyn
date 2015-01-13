// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
        private readonly RetargetingModuleSymbol retargetingModule;

        /// <summary>
        /// The underlying TypeParameterSymbol, cannot be another RetargetingTypeParameterSymbol.
        /// </summary>
        private readonly TypeParameterSymbol underlyingTypeParameter;

        /// <summary>
        /// Retargeted custom attributes
        /// </summary>
        private ImmutableArray<CSharpAttributeData> lazyCustomAttributes;

        public RetargetingTypeParameterSymbol(RetargetingModuleSymbol retargetingModule, TypeParameterSymbol underlyingTypeParameter)
        {
            Debug.Assert((object)retargetingModule != null);
            Debug.Assert((object)underlyingTypeParameter != null);
            Debug.Assert(!(underlyingTypeParameter is RetargetingTypeParameterSymbol));

            this.retargetingModule = retargetingModule;
            this.underlyingTypeParameter = underlyingTypeParameter;
        }

        private RetargetingModuleSymbol.RetargetingSymbolTranslator RetargetingTranslator
        {
            get
            {
                return retargetingModule.RetargetingTranslator;
            }
        }

        public TypeParameterSymbol UnderlyingTypeParameter
        {
            get
            {
                return this.underlyingTypeParameter;
            }
        }

        public override bool IsImplicitlyDeclared
        {
            get { return underlyingTypeParameter.IsImplicitlyDeclared; }
        }

        public override TypeParameterKind TypeParameterKind
        {
            get
            {
                return underlyingTypeParameter.TypeParameterKind;
            }
        }

        public override int Ordinal
        {
            get
            {
                return this.underlyingTypeParameter.Ordinal;
            }
        }

        public override bool HasConstructorConstraint
        {
            get
            {
                return this.underlyingTypeParameter.HasConstructorConstraint;
            }
        }

        public override bool HasReferenceTypeConstraint
        {
            get
            {
                return this.underlyingTypeParameter.HasReferenceTypeConstraint;
            }
        }

        public override bool HasValueTypeConstraint
        {
            get
            {
                return this.underlyingTypeParameter.HasValueTypeConstraint;
            }
        }

        public override VarianceKind Variance
        {
            get
            {
                return this.underlyingTypeParameter.Variance;
            }
        }

        public override Symbol ContainingSymbol
        {
            get
            {
                return this.RetargetingTranslator.Retarget(this.underlyingTypeParameter.ContainingSymbol);
            }
        }

        public override ImmutableArray<Location> Locations
        {
            get
            {
                return this.underlyingTypeParameter.Locations;
            }
        }

        public override ImmutableArray<SyntaxReference> DeclaringSyntaxReferences
        {
            get
            {
                return this.underlyingTypeParameter.DeclaringSyntaxReferences;
            }
        }

        public override ImmutableArray<CSharpAttributeData> GetAttributes()
        {
            return this.RetargetingTranslator.GetRetargetedAttributes(this.underlyingTypeParameter.GetAttributes(), ref this.lazyCustomAttributes);
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
                return this.underlyingTypeParameter.Name;
            }
        }

        public override string GetDocumentationCommentXml(CultureInfo preferredCulture = null, bool expandIncludes = false, CancellationToken cancellationToken = default(CancellationToken))
        {
            return this.underlyingTypeParameter.GetDocumentationCommentXml(preferredCulture, expandIncludes, cancellationToken);
        }

        internal override void EnsureAllConstraintsAreResolved()
        {
            this.underlyingTypeParameter.EnsureAllConstraintsAreResolved();
        }

        internal override ImmutableArray<TypeSymbol> GetConstraintTypes(ConsList<TypeParameterSymbol> inProgress)
        {
            return this.RetargetingTranslator.Retarget(this.underlyingTypeParameter.GetConstraintTypes(inProgress));
        }

        internal override ImmutableArray<NamedTypeSymbol> GetInterfaces(ConsList<TypeParameterSymbol> inProgress)
        {
            return this.RetargetingTranslator.Retarget(this.underlyingTypeParameter.GetInterfaces(inProgress));
        }

        internal override NamedTypeSymbol GetEffectiveBaseClass(ConsList<TypeParameterSymbol> inProgress)
        {
            return this.RetargetingTranslator.Retarget(this.underlyingTypeParameter.GetEffectiveBaseClass(inProgress), RetargetOptions.RetargetPrimitiveTypesByTypeCode);
        }

        internal override TypeSymbol GetDeducedBaseType(ConsList<TypeParameterSymbol> inProgress)
        {
            return this.RetargetingTranslator.Retarget(this.underlyingTypeParameter.GetDeducedBaseType(inProgress), RetargetOptions.RetargetPrimitiveTypesByTypeCode);
        }

        internal sealed override CSharpCompilation DeclaringCompilation // perf, not correctness
        {
            get { return null; }
        }
    }
}