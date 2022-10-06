﻿// Licensed to the .NET Foundation under one or more agreements.
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
        private readonly MethodSymbol? _container;
        private readonly TypeWithAnnotations _type;
        private readonly int _ordinal;
        private readonly string _name;
        private readonly RefKind _refKind;
        private readonly DeclarationScope _scope;

        public SynthesizedParameterSymbolBase(
            MethodSymbol? container,
            TypeWithAnnotations type,
            int ordinal,
            RefKind refKind,
            DeclarationScope scope,
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

        public override bool IsParams
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

            if (this.RefKind == RefKind.RefReadOnly)
            {
                AddSynthesizedAttribute(ref attributes, moduleBuilder.SynthesizeIsReadOnlyAttribute(this));
            }
        }

        internal override ImmutableArray<int> InterpolatedStringHandlerArgumentIndexes => ImmutableArray<int>.Empty;

        internal override bool HasInterpolatedStringHandlerArgumentError => false;

        internal sealed override DeclarationScope EffectiveScope => _scope;

        internal sealed override bool UseUpdatedEscapeRules => _container?.UseUpdatedEscapeRules ?? false;
    }

    internal sealed class SynthesizedParameterSymbol : SynthesizedParameterSymbolBase
    {
        private SynthesizedParameterSymbol(
            MethodSymbol? container,
            TypeWithAnnotations type,
            int ordinal,
            RefKind refKind,
            DeclarationScope scope,
            string name)
            : base(container, type, ordinal, refKind, scope, name)
        {
        }

        internal sealed override bool IsMetadataIn => RefKind == RefKind.In;

        internal sealed override bool IsMetadataOut => RefKind == RefKind.Out;

        public static ParameterSymbol Create(
            MethodSymbol? container,
            TypeWithAnnotations type,
            int ordinal,
            RefKind refKind,
            string name = "",
            DeclarationScope scope = DeclarationScope.Unscoped,
            ConstantValue? defaultValue = null,
            ImmutableArray<CustomModifier> refCustomModifiers = default,
            SourceComplexParameterSymbolBase? baseParameterForAttributes = null)
        {
            if (refCustomModifiers.IsDefaultOrEmpty && baseParameterForAttributes is null && defaultValue is null)
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
                baseParameterForAttributes);
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
            var builder = ArrayBuilder<ParameterSymbol>.GetInstance();

            foreach (var oldParam in sourceMethod.Parameters)
            {
                Debug.Assert(!(oldParam is SynthesizedComplexParameterSymbol));
                //same properties as the old one, just change the owner
                builder.Add(Create(
                    destinationMethod,
                    oldParam.TypeWithAnnotations,
                    oldParam.Ordinal,
                    oldParam.RefKind,
                    oldParam.Name,
                    oldParam.EffectiveScope,
                    oldParam.ExplicitDefaultConstantValue,
                    oldParam.RefCustomModifiers,
                    baseParameterForAttributes: null));
            }

            return builder.ToImmutableAndFree();
        }

        public override ImmutableArray<CustomModifier> RefCustomModifiers
        {
            get { return ImmutableArray<CustomModifier>.Empty; }
        }

        internal override MarshalPseudoCustomAttributeData? MarshallingInformation
        {
            get { return null; }
        }
    }

    internal sealed class SynthesizedComplexParameterSymbol : SynthesizedParameterSymbolBase
    {
        private readonly ImmutableArray<CustomModifier> _refCustomModifiers;

        // The parameter containing attributes to inherit into this synthesized parameter, if any.
        private readonly SourceComplexParameterSymbolBase? _baseParameterForAttributes;
        private readonly ConstantValue? _defaultValue;

        public SynthesizedComplexParameterSymbol(
            MethodSymbol? container,
            TypeWithAnnotations type,
            int ordinal,
            RefKind refKind,
            DeclarationScope scope,
            ConstantValue? defaultValue,
            string name,
            ImmutableArray<CustomModifier> refCustomModifiers,
            SourceComplexParameterSymbolBase? baseParameterForAttributes)
            : base(container, type, ordinal, refKind, scope, name)
        {
            Debug.Assert(!refCustomModifiers.IsDefault);
            Debug.Assert(!refCustomModifiers.IsEmpty || baseParameterForAttributes is object || defaultValue is not null);
            Debug.Assert(baseParameterForAttributes is null || baseParameterForAttributes.ExplicitDefaultConstantValue == defaultValue);

            _refCustomModifiers = refCustomModifiers;
            _baseParameterForAttributes = baseParameterForAttributes;
            _defaultValue = defaultValue;
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

        internal override bool IsMetadataIn => RefKind == RefKind.In || _baseParameterForAttributes?.GetDecodedWellKnownAttributeData()?.HasInAttribute == true;

        internal override bool IsMetadataOut => RefKind == RefKind.Out || _baseParameterForAttributes?.GetDecodedWellKnownAttributeData()?.HasOutAttribute == true;

        internal override ConstantValue? ExplicitDefaultConstantValue => _baseParameterForAttributes?.ExplicitDefaultConstantValue ?? _defaultValue;

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
