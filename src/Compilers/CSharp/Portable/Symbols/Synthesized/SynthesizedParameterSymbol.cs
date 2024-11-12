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
    /// Represents a simple compiler generated parameter of a given type.
    /// </summary>
    internal abstract class SynthesizedParameterSymbolBase : ParameterSymbol
    {
        private readonly Symbol? _container;
        private readonly TypeWithAnnotations _type;
        private readonly int _ordinal;
        private readonly string _name;
        private readonly RefKind _refKind;
        private readonly ScopedKind _scope;

        public SynthesizedParameterSymbolBase(
            Symbol? container,
            TypeWithAnnotations type,
            int ordinal,
            RefKind refKind,
            ScopedKind scope,
            string name)
        {
            Debug.Assert(type.HasType);
            Debug.Assert(name != null);
            Debug.Assert(ordinal >= 0);

            _container = container;
            _type = type;
            _ordinal = ordinal;
            _refKind = refKind;
            _scope = scope;
            _name = name;
        }

        public override TypeWithAnnotations TypeWithAnnotations => _type;

        public override RefKind RefKind => _refKind;

        public sealed override bool IsDiscard => false;

        public override string Name
        {
            get { return _name; }
        }

        public abstract override ImmutableArray<CustomModifier> RefCustomModifiers { get; }

        public override int Ordinal
        {
            get { return _ordinal; }
        }

        public override bool IsParamsArray
        {
            get { return false; }
        }

        public override bool IsParamsCollection
        {
            get { return false; }
        }

        internal override bool IsMetadataOptional
        {
            get { return ExplicitDefaultConstantValue != null; }
        }

        public override bool IsImplicitlyDeclared
        {
            get { return true; }
        }

        internal override ConstantValue? ExplicitDefaultConstantValue
        {
            get { return null; }
        }

        internal virtual ConstantValue? DefaultValueFromAttributes => null;

        internal override bool IsIDispatchConstant
        {
            get { throw ExceptionUtilities.Unreachable(); }
        }

        internal override bool IsIUnknownConstant
        {
            get { throw ExceptionUtilities.Unreachable(); }
        }

        internal override bool IsCallerLineNumber
        {
            get { throw ExceptionUtilities.Unreachable(); }
        }

        internal override bool IsCallerFilePath
        {
            get { throw ExceptionUtilities.Unreachable(); }
        }

        internal override bool IsCallerMemberName
        {
            get { throw ExceptionUtilities.Unreachable(); }
        }

        internal override int CallerArgumentExpressionParameterIndex
        {
            get { return -1; }
        }

        internal override FlowAnalysisAnnotations FlowAnalysisAnnotations
        {
            get { return FlowAnalysisAnnotations.None; }
        }

        internal override ImmutableHashSet<string> NotNullIfParameterNotNull
        {
            get { return ImmutableHashSet<string>.Empty; }
        }

        public override Symbol? ContainingSymbol
        {
            get { return _container; }
        }

        public override ImmutableArray<Location> Locations
        {
            get { return ImmutableArray<Location>.Empty; }
        }

        public override ImmutableArray<SyntaxReference> DeclaringSyntaxReferences
        {
            get
            {
                return ImmutableArray<SyntaxReference>.Empty;
            }
        }

        internal override void AddSynthesizedAttributes(PEModuleBuilder moduleBuilder, ref ArrayBuilder<SynthesizedAttributeData> attributes)
        {
            // Emit [Dynamic] on synthesized parameter symbols when the original parameter was dynamic 
            // in order to facilitate debugging.  In the case the necessary attributes are missing 
            // this is a no-op.  Emitting an error here, or when the original parameter was bound, would
            // adversely effect the compilation or potentially change overload resolution.  
            var compilation = this.DeclaringCompilation;
            var type = this.TypeWithAnnotations;
            if (type.Type.ContainsDynamic() && compilation.HasDynamicEmitAttributes(BindingDiagnosticBag.Discarded, Location.None) && compilation.CanEmitBoolean())
            {
                AddSynthesizedAttribute(ref attributes, compilation.SynthesizeDynamicAttribute(type.Type, type.CustomModifiers.Length + this.RefCustomModifiers.Length, this.RefKind));
            }

            if (compilation.ShouldEmitNativeIntegerAttributes(type.Type))
            {
                AddSynthesizedAttribute(ref attributes, moduleBuilder.SynthesizeNativeIntegerAttribute(this, type.Type));
            }

            if (ParameterHelpers.RequiresScopedRefAttribute(this))
            {
                AddSynthesizedAttribute(ref attributes, moduleBuilder.SynthesizeScopedRefAttribute(this, EffectiveScope));
            }

            if (type.Type.ContainsTupleNames() &&
                compilation.HasTupleNamesAttributes(BindingDiagnosticBag.Discarded, Location.None) &&
                compilation.CanEmitSpecialType(SpecialType.System_String))
            {
                AddSynthesizedAttribute(ref attributes,
                    compilation.SynthesizeTupleNamesAttribute(type.Type));
            }

            if (compilation.ShouldEmitNullableAttributes(this))
            {
                AddSynthesizedAttribute(ref attributes, moduleBuilder.SynthesizeNullableAttributeIfNecessary(this, GetNullableContextValue(), type));
            }

            switch (this.RefKind)
            {
                case RefKind.In:
                    AddSynthesizedAttribute(ref attributes, moduleBuilder.SynthesizeIsReadOnlyAttribute(this));
                    break;
                case RefKind.RefReadOnlyParameter:
                    AddSynthesizedAttribute(ref attributes, moduleBuilder.SynthesizeRequiresLocationAttribute(this));
                    break;
            }

            if (this.HasUnscopedRefAttribute && this.ContainingSymbol is SynthesizedDelegateInvokeMethod)
            {
                AddSynthesizedAttribute(ref attributes, compilation.TrySynthesizeAttribute(WellKnownMember.System_Diagnostics_CodeAnalysis_UnscopedRefAttribute__ctor));
            }

            if (this.IsParamsArray && this.ContainingSymbol is SynthesizedDelegateInvokeMethod)
            {
                AddSynthesizedAttribute(ref attributes, compilation.TrySynthesizeAttribute(WellKnownMember.System_ParamArrayAttribute__ctor));
            }
            else if (this.IsParamsCollection && this.ContainingSymbol is SynthesizedDelegateInvokeMethod)
            {
                AddSynthesizedAttribute(ref attributes, moduleBuilder.SynthesizeParamCollectionAttribute(this));
            }

            var defaultValue = this.ExplicitDefaultConstantValue;
            if (defaultValue != ConstantValue.NotAvailable &&
                DefaultValueFromAttributes == ConstantValue.NotAvailable &&
                this.ContainingSymbol is SynthesizedDelegateInvokeMethod or SynthesizedClosureMethod)
            {
                var attrData = defaultValue.SpecialType switch
                {
                    SpecialType.System_Decimal => compilation.SynthesizeDecimalConstantAttribute(defaultValue.DecimalValue),
                    SpecialType.System_DateTime => compilation.SynthesizeDateTimeConstantAttribute(defaultValue.DateTimeValue),
                    _ => null
                };
                AddSynthesizedAttribute(ref attributes, attrData);
            }
        }

        internal override ImmutableArray<int> InterpolatedStringHandlerArgumentIndexes => ImmutableArray<int>.Empty;

        internal override bool HasInterpolatedStringHandlerArgumentError => false;

        internal sealed override ScopedKind EffectiveScope => _scope;

        internal sealed override bool UseUpdatedEscapeRules =>
            _container switch
            {
                MethodSymbol method => method.UseUpdatedEscapeRules,
                Symbol symbol => symbol.ContainingModule.UseUpdatedEscapeRules,
                _ => false,
            };
    }

    internal sealed class SynthesizedParameterSymbol : SynthesizedParameterSymbolBase
    {
        private SynthesizedParameterSymbol(
            Symbol? container,
            TypeWithAnnotations type,
            int ordinal,
            RefKind refKind,
            ScopedKind scope,
            string name)
            : base(container, type, ordinal, refKind, scope, name)
        {
        }

        internal sealed override bool IsMetadataIn => RefKind is RefKind.In or RefKind.RefReadOnlyParameter;

        internal sealed override bool IsMetadataOut => RefKind == RefKind.Out;

        public static ParameterSymbol Create(
            Symbol? container,
            TypeWithAnnotations type,
            int ordinal,
            RefKind refKind,
            string name = "",
            ScopedKind scope = ScopedKind.None,
            ConstantValue? defaultValue = null,
            ImmutableArray<CustomModifier> refCustomModifiers = default,
            SourceComplexParameterSymbolBase? baseParameterForAttributes = null,
            bool isParams = false,
            bool hasUnscopedRefAttribute = false)
        {
            if (!isParams
                && refCustomModifiers.IsDefaultOrEmpty
                && baseParameterForAttributes is null
                && defaultValue is null
                && !hasUnscopedRefAttribute)
            {
                return new SynthesizedParameterSymbol(container, type, ordinal, refKind, scope, name);
            }

            return new SynthesizedComplexParameterSymbol(
                container,
                type,
                ordinal,
                refKind,
                scope,
                defaultValue,
                name,
                refCustomModifiers.NullToEmpty(),
                baseParameterForAttributes,
                isParams: isParams,
                hasUnscopedRefAttribute: hasUnscopedRefAttribute);
        }

        /// <summary>
        /// For each parameter of a source method, construct a corresponding synthesized parameter
        /// for a destination method.
        /// </summary>
        /// <param name="sourceMethod">Has parameters.</param>
        /// <param name="destinationMethod">Needs parameters.</param>
        /// <returns>Synthesized parameters to add to destination method.</returns>
        internal static ImmutableArray<ParameterSymbol> DeriveParameters(MethodSymbol sourceMethod, MethodSymbol destinationMethod)
        {
            return sourceMethod.Parameters.SelectAsArray(
                static (oldParam, destinationMethod) => DeriveParameter(destinationMethod, oldParam),
                destinationMethod);
        }

        internal static ParameterSymbol DeriveParameter(Symbol destination, ParameterSymbol oldParam)
        {
            Debug.Assert(!(oldParam is SynthesizedComplexParameterSymbol));
            //same properties as the old one, just change the owner
            return Create(
                destination,
                oldParam.TypeWithAnnotations,
                oldParam.Ordinal,
                oldParam.RefKind,
                oldParam.Name,
                oldParam.EffectiveScope,
                oldParam.ExplicitDefaultConstantValue,
                oldParam.RefCustomModifiers,
                baseParameterForAttributes: null);
        }

        public override ImmutableArray<CustomModifier> RefCustomModifiers
        {
            get { return ImmutableArray<CustomModifier>.Empty; }
        }

        internal override MarshalPseudoCustomAttributeData? MarshallingInformation
        {
            get { return null; }
        }

        internal override bool HasUnscopedRefAttribute => false;
    }

    internal sealed class SynthesizedComplexParameterSymbol : SynthesizedParameterSymbolBase
    {
        private readonly ImmutableArray<CustomModifier> _refCustomModifiers;

        // The parameter containing attributes to inherit into this synthesized parameter, if any.
        private readonly SourceComplexParameterSymbolBase? _baseParameterForAttributes;
        private readonly ConstantValue? _defaultValue;
        private readonly bool _isParams;
        private readonly bool _hasUnscopedRefAttribute;

        public SynthesizedComplexParameterSymbol(
            Symbol? container,
            TypeWithAnnotations type,
            int ordinal,
            RefKind refKind,
            ScopedKind scope,
            ConstantValue? defaultValue,
            string name,
            ImmutableArray<CustomModifier> refCustomModifiers,
            SourceComplexParameterSymbolBase? baseParameterForAttributes,
            bool isParams,
            bool hasUnscopedRefAttribute)
            : base(container, type, ordinal, refKind, scope, name)
        {
            Debug.Assert(!refCustomModifiers.IsDefault);
            Debug.Assert(isParams || !refCustomModifiers.IsEmpty || baseParameterForAttributes is object || defaultValue is not null || hasUnscopedRefAttribute);
            Debug.Assert(baseParameterForAttributes is null || baseParameterForAttributes.ExplicitDefaultConstantValue == defaultValue);

            _refCustomModifiers = refCustomModifiers;
            _baseParameterForAttributes = baseParameterForAttributes;
            _defaultValue = defaultValue;
            _isParams = isParams;
            _hasUnscopedRefAttribute = hasUnscopedRefAttribute;
        }

        public override ImmutableArray<CustomModifier> RefCustomModifiers
        {
            get { return _refCustomModifiers; }
        }

        public override ImmutableArray<CSharpAttributeData> GetAttributes()
        {
            return _baseParameterForAttributes?.GetAttributes() ?? ImmutableArray<CSharpAttributeData>.Empty;
        }

        public bool HasEnumeratorCancellationAttribute => _baseParameterForAttributes?.HasEnumeratorCancellationAttribute ?? false;

        internal override MarshalPseudoCustomAttributeData? MarshallingInformation => _baseParameterForAttributes?.MarshallingInformation;

        public override bool IsParamsArray => _isParams && Type.IsSZArray();

        public override bool IsParamsCollection => _isParams && !Type.IsSZArray();

        internal override bool HasUnscopedRefAttribute => _hasUnscopedRefAttribute;

        internal override bool IsMetadataOptional => _baseParameterForAttributes?.IsMetadataOptional ?? base.IsMetadataOptional;

        internal override bool IsCallerLineNumber
        {
            get => _baseParameterForAttributes?.IsCallerLineNumber ?? false;
        }

        internal override bool IsCallerFilePath
        {
            get => _baseParameterForAttributes?.IsCallerFilePath ?? false;
        }

        internal override bool IsCallerMemberName
        {
            get => _baseParameterForAttributes?.IsCallerMemberName ?? false;
        }

        internal override bool IsMetadataIn => RefKind is RefKind.In or RefKind.RefReadOnlyParameter || _baseParameterForAttributes?.GetDecodedWellKnownAttributeData()?.HasInAttribute == true;

        internal override bool IsMetadataOut => RefKind == RefKind.Out || _baseParameterForAttributes?.GetDecodedWellKnownAttributeData()?.HasOutAttribute == true;

        internal override ConstantValue? ExplicitDefaultConstantValue => _baseParameterForAttributes?.ExplicitDefaultConstantValue ?? _defaultValue;

        internal override ConstantValue? DefaultValueFromAttributes => _baseParameterForAttributes?.DefaultValueFromAttributes;

        internal override FlowAnalysisAnnotations FlowAnalysisAnnotations
        {
            get
            {
                Debug.Assert(_baseParameterForAttributes is null);
                return base.FlowAnalysisAnnotations;
            }
        }

        internal override ImmutableHashSet<string> NotNullIfParameterNotNull
        {
            get
            {
                Debug.Assert(_baseParameterForAttributes is null);
                return base.NotNullIfParameterNotNull;
            }
        }
    }
}
