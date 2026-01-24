// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.CodeAnalysis.CSharp.Emit;
using Microsoft.CodeAnalysis.PooledObjects;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    /// <summary>
    /// Base class for all parameters that are emitted.
    /// </summary>
    internal abstract class SourceParameterSymbolBase : ParameterSymbol
    {
        private readonly Symbol _containingSymbol;
        private readonly ushort _ordinal;

        public SourceParameterSymbolBase(Symbol containingSymbol, int ordinal)
        {
            Debug.Assert((object)containingSymbol != null);
            Debug.Assert(containingSymbol.ContainingAssembly != null);
            _ordinal = (ushort)ordinal;
            _containingSymbol = containingSymbol;
        }

        public sealed override bool Equals(Symbol obj, TypeCompareKind compareKind)
        {
            if (obj == (object)this)
            {
                return true;
            }

            if (obj is NativeIntegerParameterSymbol nps)
            {
                return nps.Equals(this, compareKind);
            }

            var symbol = obj as SourceParameterSymbolBase;
            return symbol is not null
                && symbol.Ordinal == this.Ordinal
                && symbol._containingSymbol.Equals(_containingSymbol, compareKind);
        }

        public sealed override int GetHashCode()
        {
            return Hash.Combine(_containingSymbol.GetHashCode(), this.Ordinal);
        }

        public sealed override int Ordinal
        {
            get { return _ordinal; }
        }

        public sealed override Symbol ContainingSymbol
        {
            get { return _containingSymbol; }
        }

        public sealed override AssemblySymbol ContainingAssembly
        {
            get { return _containingSymbol.ContainingAssembly; }
        }

        internal override void AddSynthesizedAttributes(PEModuleBuilder moduleBuilder, ref ArrayBuilder<CSharpAttributeData> attributes)
        {
            base.AddSynthesizedAttributes(moduleBuilder, ref attributes);
            AddSynthesizedAttributes(this, moduleBuilder, ref attributes);
        }

        internal static void AddSynthesizedAttributes(ParameterSymbol parameter, PEModuleBuilder moduleBuilder, ref ArrayBuilder<CSharpAttributeData> attributes)
        {
            var compilation = parameter.DeclaringCompilation;

            if (parameter.IsParamsArray)
            {
                AddSynthesizedAttribute(ref attributes, compilation.TrySynthesizeAttribute(WellKnownMember.System_ParamArrayAttribute__ctor));
            }
            else if (parameter.IsParamsCollection)
            {
                AddSynthesizedAttribute(ref attributes, moduleBuilder.SynthesizeParamCollectionAttribute(parameter));
            }

            // Synthesize DecimalConstantAttribute if we don't have an explicit custom attribute already:
            var defaultValue = parameter.ExplicitDefaultConstantValue;
            if (defaultValue != ConstantValue.NotAvailable &&
                defaultValue.SpecialType == SpecialType.System_Decimal &&
                parameter is SourceParameterSymbolBase sourceParameter &&
                sourceParameter.DefaultValueFromAttributes == ConstantValue.NotAvailable)
            {
                AddSynthesizedAttribute(ref attributes, compilation.SynthesizeDecimalConstantAttribute(defaultValue.DecimalValue));
            }

            var type = parameter.TypeWithAnnotations;

            if (type.Type.ContainsDynamic())
            {
                AddSynthesizedAttribute(ref attributes, compilation.SynthesizeDynamicAttribute(type.Type, type.CustomModifiers.Length + parameter.RefCustomModifiers.Length, parameter.RefKind));
            }

            if (compilation.ShouldEmitNativeIntegerAttributes(type.Type))
            {
                AddSynthesizedAttribute(ref attributes, moduleBuilder.SynthesizeNativeIntegerAttribute(parameter, type.Type));
            }

            if (ParameterHelpers.RequiresScopedRefAttribute(parameter))
            {
                AddSynthesizedAttribute(ref attributes, moduleBuilder.SynthesizeScopedRefAttribute(parameter, parameter.EffectiveScope));
            }

            if (type.Type.ContainsTupleNames())
            {
                AddSynthesizedAttribute(ref attributes,
                    compilation.SynthesizeTupleNamesAttribute(type.Type));
            }

            switch (parameter.RefKind)
            {
                case RefKind.In:
                    AddSynthesizedAttribute(ref attributes, moduleBuilder.SynthesizeIsReadOnlyAttribute(parameter));
                    break;
                case RefKind.RefReadOnlyParameter:
                    AddSynthesizedAttribute(ref attributes, moduleBuilder.SynthesizeRequiresLocationAttribute(parameter));
                    break;
            }

            if (compilation.ShouldEmitNullableAttributes(parameter))
            {
                AddSynthesizedAttribute(ref attributes, moduleBuilder.SynthesizeNullableAttributeIfNecessary(parameter, parameter.GetNullableContextValue(), type));
            }
        }

        internal abstract ParameterSymbol WithCustomModifiersAndParams(TypeSymbol newType, ImmutableArray<CustomModifier> newCustomModifiers, ImmutableArray<CustomModifier> newRefCustomModifiers, bool newIsParams);
    }
}
