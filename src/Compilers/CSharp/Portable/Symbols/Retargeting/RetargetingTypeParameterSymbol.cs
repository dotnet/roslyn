// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#nullable enable

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
        : WrappedTypeParameterSymbol
    {
        /// <summary>
        /// Owning RetargetingModuleSymbol.
        /// </summary>
        private readonly RetargetingModuleSymbol _retargetingModule;

        /// <summary>
        /// Retargeted custom attributes
        /// </summary>
        private ImmutableArray<CSharpAttributeData> _lazyCustomAttributes;

        public RetargetingTypeParameterSymbol(RetargetingModuleSymbol retargetingModule, TypeParameterSymbol underlyingTypeParameter)
            : base(underlyingTypeParameter)
        {
            RoslynDebug.Assert((object)retargetingModule != null);
            Debug.Assert(!(underlyingTypeParameter is RetargetingTypeParameterSymbol));

            _retargetingModule = retargetingModule;
        }

        private RetargetingModuleSymbol.RetargetingSymbolTranslator RetargetingTranslator
        {
            get
            {
                return _retargetingModule.RetargetingTranslator;
            }
        }

        public override Symbol? ContainingSymbol
        {
            get
            {
                return this.RetargetingTranslator.Retarget(_underlyingTypeParameter.ContainingSymbol);
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

        internal override ImmutableArray<TypeWithAnnotations> GetConstraintTypes(ConsList<TypeParameterSymbol> inProgress)
        {
            return this.RetargetingTranslator.Retarget(_underlyingTypeParameter.GetConstraintTypes(inProgress));
        }

        internal override bool? IsNotNullable
        {
            get
            {
                return _underlyingTypeParameter.IsNotNullable;
            }
        }

        internal override ImmutableArray<NamedTypeSymbol> GetInterfaces(ConsList<TypeParameterSymbol> inProgress)
        {
            return this.RetargetingTranslator.Retarget(_underlyingTypeParameter.GetInterfaces(inProgress));
        }

        internal override NamedTypeSymbol GetEffectiveBaseClass(ConsList<TypeParameterSymbol> inProgress)
        {
#nullable disable // Can '_underlyingTypeParameter.GetEffectiveBaseClass(inProgress)' be null? https://github.com/dotnet/roslyn/issues/39166
            return this.RetargetingTranslator.Retarget(_underlyingTypeParameter.GetEffectiveBaseClass(inProgress), RetargetOptions.RetargetPrimitiveTypesByTypeCode);
#nullable enable
        }

        internal override TypeSymbol GetDeducedBaseType(ConsList<TypeParameterSymbol> inProgress)
        {
#nullable disable // Can '_underlyingTypeParameter.GetDeducedBaseType(inProgress)' be null? https://github.com/dotnet/roslyn/issues/39166
            return this.RetargetingTranslator.Retarget(_underlyingTypeParameter.GetDeducedBaseType(inProgress), RetargetOptions.RetargetPrimitiveTypesByTypeCode);
#nullable enable
        }

        internal sealed override CSharpCompilation? DeclaringCompilation // perf, not correctness
        {
            get { return null; }
        }
    }
}
