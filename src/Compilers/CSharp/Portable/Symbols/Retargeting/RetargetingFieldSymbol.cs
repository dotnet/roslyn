// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using Roslyn.Utilities;
using Microsoft.CodeAnalysis.CSharp.Emit;

namespace Microsoft.CodeAnalysis.CSharp.Symbols.Retargeting
{
    /// <summary>
    /// Represents a field in a RetargetingModuleSymbol. Essentially this is a wrapper around 
    /// another FieldSymbol that is responsible for retargeting symbols from one assembly to another. 
    /// It can retarget symbols for multiple assemblies at the same time.
    /// </summary>
    internal sealed class RetargetingFieldSymbol : WrappedFieldSymbol
    {
        /// <summary>
        /// Owning RetargetingModuleSymbol.
        /// </summary>
        private readonly RetargetingModuleSymbol _retargetingModule;

        /// <summary>
        /// Retargeted custom attributes
        /// </summary>
        private ImmutableArray<CSharpAttributeData> _lazyCustomAttributes;

        private CachedUseSiteInfo<AssemblySymbol> _lazyCachedUseSiteInfo = CachedUseSiteInfo<AssemblySymbol>.Uninitialized;

        public RetargetingFieldSymbol(RetargetingModuleSymbol retargetingModule, FieldSymbol underlyingField)
            : base(underlyingField)
        {
            Debug.Assert((object)retargetingModule != null);
            Debug.Assert(!(underlyingField is RetargetingFieldSymbol));

            _retargetingModule = retargetingModule;
        }

        private RetargetingModuleSymbol.RetargetingSymbolTranslator RetargetingTranslator
        {
            get
            {
                return _retargetingModule.RetargetingTranslator;
            }
        }

        public RetargetingModuleSymbol RetargetingModule
        {
            get
            {
                return _retargetingModule;
            }
        }

        internal override TypeWithAnnotations GetFieldType(ConsList<FieldSymbol> fieldsBeingBound)
        {
            return this.RetargetingTranslator.Retarget(_underlyingField.GetFieldType(fieldsBeingBound), RetargetOptions.RetargetPrimitiveTypesByTypeCode);
        }

        public override Symbol ContainingSymbol
        {
            get
            {
                return this.RetargetingTranslator.Retarget(_underlyingField.ContainingSymbol);
            }
        }

        public override RefKind RefKind => _underlyingField.RefKind;

        public override ImmutableArray<CustomModifier> RefCustomModifiers =>
            this.RetargetingTranslator.RetargetModifiers(_underlyingField.RefCustomModifiers, out _);

        public override ImmutableArray<CSharpAttributeData> GetAttributes()
        {
            return this.RetargetingTranslator.GetRetargetedAttributes(_underlyingField.GetAttributes(), ref _lazyCustomAttributes);
        }

        internal override IEnumerable<CSharpAttributeData> GetCustomAttributesToEmit(PEModuleBuilder moduleBuilder)
        {
            return this.RetargetingTranslator.RetargetAttributes(_underlyingField.GetCustomAttributesToEmit(moduleBuilder));
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

        internal override MarshalPseudoCustomAttributeData MarshallingInformation
        {
            get
            {
                return this.RetargetingTranslator.Retarget(_underlyingField.MarshallingInformation);
            }
        }

        public override Symbol AssociatedSymbol
        {
            get
            {
                var associated = _underlyingField.AssociatedSymbol;
                return (object)associated == null ? null : this.RetargetingTranslator.Retarget(associated);
            }
        }

        public override int TupleElementIndex => _underlyingField.TupleElementIndex;

        internal override UseSiteInfo<AssemblySymbol> GetUseSiteInfo()
        {
            if (!_lazyCachedUseSiteInfo.IsInitialized)
            {
                AssemblySymbol primaryDependency = PrimaryDependency;
                var result = new UseSiteInfo<AssemblySymbol>(primaryDependency);
                CalculateUseSiteDiagnostic(ref result);
                _lazyCachedUseSiteInfo.Initialize(primaryDependency, result);
            }

            return _lazyCachedUseSiteInfo.ToUseSiteInfo(PrimaryDependency);
        }

        internal sealed override CSharpCompilation DeclaringCompilation // perf, not correctness
        {
            get { return null; }
        }
    }
}
