// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Roslyn.Utilities;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Threading;
namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    /// <summary>
    /// A plain TupleElementFieldSymbol (as opposed to a TupleVirtualElementFieldSymbol) represents
    /// an element field of a tuple type (such as (int, byte).Item1) that is backed by a real field
    /// with the same name within the tuple underlying type.
    ///
    /// Note that original tuple fields (like 'System.ValueTuple`2.Item1') do not get wrapped.
    /// </summary>
    internal class TupleElementFieldSymbol : WrappedFieldSymbol
    {
        /// <summary>
        /// If this field represents a tuple element with index X
        ///  2X      if this field represents Default-named element
        ///  2X + 1  if this field represents Friendly-named element
        /// </summary>
        private readonly int _tupleElementIndex;

        protected readonly NamedTypeSymbol _containingTuple;

        private readonly ImmutableArray<Location> _locations;
        protected readonly FieldSymbol _correspondingDefaultField;

        // default tuple elements like Item1 or Item20 could be provided by the user or
        // otherwise implicitly declared by compiler
        private readonly bool _isImplicitlyDeclared;

        public TupleElementFieldSymbol(
            NamedTypeSymbol container,
            FieldSymbol underlyingField,
            int tupleElementIndex,
            ImmutableArray<Location> locations,
            bool isImplicitlyDeclared,
            FieldSymbol? correspondingDefaultFieldOpt = null)
            : base(underlyingField)
        {
            Debug.Assert(tupleElementIndex >= 0);
            Debug.Assert(container.Equals(underlyingField.ContainingType, TypeCompareKind.IgnoreDynamicAndTupleNames) || this is TupleVirtualElementFieldSymbol,
                                            "virtual fields should be represented by " + nameof(TupleVirtualElementFieldSymbol));
            Debug.Assert(!(underlyingField is TupleElementFieldSymbol));

            // The fields on definition of ValueTuple<...> types don't need to be wrapped
            Debug.Assert(!container.IsDefinition);

            Debug.Assert(container.IsTupleType);
            _containingTuple = container;
            _tupleElementIndex = correspondingDefaultFieldOpt is null ? tupleElementIndex << 1 : (tupleElementIndex << 1) + 1;
            Debug.Assert(!locations.IsDefault);
            _locations = locations;
            _isImplicitlyDeclared = isImplicitlyDeclared;
            _correspondingDefaultField = correspondingDefaultFieldOpt ?? this;
        }

        /// <summary>
        /// If this is a field representing a tuple element,
        /// returns the index of the element (zero-based).
        /// Otherwise returns -1
        /// </summary>
        public sealed override int TupleElementIndex
            => _tupleElementIndex >> 1;

        public sealed override bool IsDefaultTupleElement
        {
            get
            {
                // even
                return (_tupleElementIndex & 1) == 0;
            }
        }

        public sealed override bool IsExplicitlyNamedTupleElement
        {
            get
            {
                return !_isImplicitlyDeclared;
            }
        }

        public sealed override FieldSymbol TupleUnderlyingField
        {
            get
            {
                return _underlyingField;
            }
        }

        public sealed override Symbol? AssociatedSymbol
        {
            get
            {
                return null;
            }
        }

        public override FieldSymbol OriginalDefinition
        {
            get
            {
                NamedTypeSymbol originalContainer = ContainingType.OriginalDefinition;
                if (!originalContainer.IsTupleType)
                {
                    return this;
                }
                return originalContainer.GetTupleMemberSymbolForUnderlyingMember(_underlyingField.OriginalDefinition)!;
            }
        }

        public sealed override Symbol ContainingSymbol
        {
            get
            {
                return _containingTuple;
            }
        }

        public sealed override RefKind RefKind => _underlyingField.RefKind;

        public sealed override ImmutableArray<CustomModifier> RefCustomModifiers => _underlyingField.RefCustomModifiers;

        internal override TypeWithAnnotations GetFieldType(ConsList<FieldSymbol> fieldsBeingBound)
        {
            return _underlyingField.GetFieldType(fieldsBeingBound);
        }

        public override ImmutableArray<CSharpAttributeData> GetAttributes()
        {
            return _underlyingField.GetAttributes();
        }

        internal override UseSiteInfo<AssemblySymbol> GetUseSiteInfo()
        {
            return _underlyingField.GetUseSiteInfo();
        }

        internal override bool RequiresCompletion => _underlyingField.RequiresCompletion;

        internal override bool HasComplete(CompletionPart part) => _underlyingField.HasComplete(part);

        internal override void ForceComplete(SourceLocation locationOpt, CancellationToken cancellationToken)
        {
            _underlyingField.ForceComplete(locationOpt, cancellationToken);
        }

        public sealed override int GetHashCode()
        {
            return Hash.Combine(_containingTuple.GetHashCode(), _tupleElementIndex.GetHashCode());
        }

        public sealed override bool Equals(Symbol obj, TypeCompareKind compareKind)
        {
            var other = obj as TupleElementFieldSymbol;

            if ((object?)other == this)
            {
                return true;
            }

            // note we have to compare both index and name because
            // in nameless tuple there could be fields that differ only by index
            // and in named tuples there could be fields that differ only by name
            return (object?)other != null &&
                _tupleElementIndex == other._tupleElementIndex &&
                TypeSymbol.Equals(_containingTuple, other._containingTuple, compareKind);
        }

        public sealed override ImmutableArray<Location> Locations
        {
            get
            {
                return _locations;
            }
        }

        public sealed override ImmutableArray<SyntaxReference> DeclaringSyntaxReferences
        {
            get
            {
                return _isImplicitlyDeclared ?
                    ImmutableArray<SyntaxReference>.Empty :
                    GetDeclaringSyntaxReferenceHelper<CSharpSyntaxNode>(_locations);
            }
        }

        public sealed override bool IsImplicitlyDeclared
        {
            get
            {
                return _isImplicitlyDeclared;
            }
        }

        public sealed override FieldSymbol CorrespondingTupleField
        {
            get
            {
                return _correspondingDefaultField;
            }
        }

        internal override FieldSymbol AsMember(NamedTypeSymbol newOwner)
        {
            Debug.Assert(newOwner.IsTupleType);

            NamedTypeSymbol newUnderlyingOwner = GetNewUnderlyingOwner(newOwner);
            return new TupleElementFieldSymbol(newOwner, _underlyingField.OriginalDefinition.AsMember(newUnderlyingOwner), TupleElementIndex, Locations, IsImplicitlyDeclared);
        }

        protected NamedTypeSymbol GetNewUnderlyingOwner(NamedTypeSymbol newOwner)
        {
            int currentIndex = TupleElementIndex;
            NamedTypeSymbol newUnderlyingOwner = newOwner;
            while (currentIndex >= NamedTypeSymbol.ValueTupleRestIndex)
            {
                newUnderlyingOwner = (NamedTypeSymbol)newUnderlyingOwner.TypeArgumentsWithAnnotationsNoUseSiteDiagnostics[NamedTypeSymbol.ValueTupleRestIndex].Type;
                currentIndex -= NamedTypeSymbol.ValueTupleRestIndex;
            }

            return newUnderlyingOwner;
        }
    }

    /// <summary>
    /// Represents an element field of a tuple type that is not backed by a real field
    /// with the same name within the tuple type.
    ///
    /// Examples
    ///     // alias to Item1 with a different name
    ///     (int a, byte b).a
    ///
    ///     // not backed directly by the type
    ///     (int i1, int i2, int i3, int i4, int i5, int i6, int i7, int i8).i8
    ///
    ///     // Item8, which is also not backed directly by the type
    ///     (int, int, int, int, int, int, int, int).Item8
    ///
    /// NOTE: For any virtual element, there is a nonvirtual way to access the same underlying field.
    ///       In scenarios where we need to enumerate actual fields of a struct,
    ///       virtual fields should be ignored.
    /// </summary>
    internal sealed class TupleVirtualElementFieldSymbol : TupleElementFieldSymbol
    {
        private readonly string _name;
        private readonly bool _cannotUse; // With LanguageVersion 7, we will produce named elements that should not be used

        public TupleVirtualElementFieldSymbol(
            NamedTypeSymbol container,
            FieldSymbol underlyingField,
            string name,
            int tupleElementIndex,
            ImmutableArray<Location> locations,
            bool cannotUse,
            bool isImplicitlyDeclared,
            FieldSymbol? correspondingDefaultFieldOpt)
            : base(container, underlyingField, tupleElementIndex, locations, isImplicitlyDeclared, correspondingDefaultFieldOpt)
        {
            // The underlying field for 'Hanna' (an 8-th named element) in a long tuple is Item1. The corresponding field is Item8.

            Debug.Assert(container.IsTupleType);
            Debug.Assert(underlyingField.ContainingType.IsTupleType);
            Debug.Assert(name != null);
            Debug.Assert(name != underlyingField.Name || !container.Equals(underlyingField.ContainingType, TypeCompareKind.IgnoreDynamicAndTupleNames),
                                "fields that map directly to underlying should not be represented by " + nameof(TupleVirtualElementFieldSymbol));
            Debug.Assert((correspondingDefaultFieldOpt is null) == (NamedTypeSymbol.TupleMemberName(tupleElementIndex + 1) == name));
            Debug.Assert(!(correspondingDefaultFieldOpt is TupleErrorFieldSymbol));

            _name = name;
            _cannotUse = cannotUse;
        }

        internal override UseSiteInfo<AssemblySymbol> GetUseSiteInfo()
        {
            if (_cannotUse)
            {
                return new UseSiteInfo<AssemblySymbol>(new CSDiagnosticInfo(ErrorCode.ERR_TupleInferredNamesNotAvailable, _name,
                    new CSharpRequiredLanguageVersion(MessageID.IDS_FeatureInferredTupleNames.RequiredVersion())));
            }

            return base.GetUseSiteInfo();
        }

        public override string Name
        {
            get
            {
                return _name;
            }
        }

        internal override int? TypeLayoutOffset
        {
            get
            {
                return null;
            }
        }

        public override FieldSymbol OriginalDefinition
        {
            get
            {
                return this;
            }
        }

        public override bool IsVirtualTupleField
        {
            get
            {
                return true;
            }
        }

        internal override TypeWithAnnotations GetFieldType(ConsList<FieldSymbol> fieldsBeingBound)
            => _underlyingField.GetFieldType(fieldsBeingBound);

        public override ImmutableArray<CSharpAttributeData> GetAttributes()
        {
            return _underlyingField.GetAttributes();
        }

        internal override FieldSymbol AsMember(NamedTypeSymbol newOwner)
        {
            Debug.Assert(newOwner.IsTupleType);
            Debug.Assert(newOwner.TupleElements.Length == this._containingTuple.TupleElements.Length);

            NamedTypeSymbol newUnderlyingOwner = GetNewUnderlyingOwner(newOwner);

            FieldSymbol? newCorrespondingDefaultFieldOpt = null;
            if ((object)_correspondingDefaultField != this)
            {
                newCorrespondingDefaultFieldOpt = _correspondingDefaultField.OriginalDefinition.AsMember(newOwner);
            }

            return new TupleVirtualElementFieldSymbol(newOwner, _underlyingField.OriginalDefinition.AsMember(newUnderlyingOwner), _name, TupleElementIndex, Locations, _cannotUse, IsImplicitlyDeclared, newCorrespondingDefaultFieldOpt);
        }
    }
}
