// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Roslyn.Utilities;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Threading;
#nullable enable

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    /// <summary>
    /// Represents a non-element field of a tuple type (such as (int, byte).Rest)
    /// that is backed by a real field within the tuple underlying type.
    /// </summary>
    internal class TupleFieldSymbol : WrappedFieldSymbol
    {
        protected readonly NamedTypeSymbol _containingTuple;

        /// <summary>
        /// If this field represents a tuple element with index X
        ///  2X      if this field represents Default-named element
        ///  2X + 1  if this field represents Friendly-named element
        /// Otherwise, (-1 - [index in members array]);
        /// </summary>
        private readonly int _tupleElementIndex;

        public TupleFieldSymbol(NamedTypeSymbol container, FieldSymbol underlyingField, int tupleElementIndex)
            : base(underlyingField)
        {
            Debug.Assert(container.IsTupleType);
            Debug.Assert(container.Equals(underlyingField.ContainingType, TypeCompareKind.IgnoreDynamicAndTupleNames) || this is TupleVirtualElementFieldSymbol,
                                            "virtual fields should be represented by " + nameof(TupleVirtualElementFieldSymbol));

            _containingTuple = container;
            _tupleElementIndex = tupleElementIndex;
        }

        /// <summary>
        /// If this is a field representing a tuple element,
        /// returns the index of the element (zero-based).
        /// Otherwise returns -1
        /// </summary>
        public override int TupleElementIndex
        {
            get
            {
                if (_tupleElementIndex < 0)
                {
                    return -1;
                }

                return _tupleElementIndex >> 1;
            }
        }

        public override bool IsDefaultTupleElement
        {
            get
            {
                // not negative and even
                return (_tupleElementIndex & ((1 << 31) | 1)) == 0;
            }
        }

        public sealed override FieldSymbol TupleUnderlyingField
        {
            get
            {
                return _underlyingField;
            }
        }

        public override Symbol? AssociatedSymbol
        {
            get
            {
                return _containingTuple.GetTupleMemberSymbolForUnderlyingMember(_underlyingField.AssociatedSymbol);
            }
        }

        public override FieldSymbol OriginalDefinition
        {
            get
            {
                NamedTypeSymbol originalContainer = ContainingType.OriginalDefinition;
                if (!originalContainer.IsTupleType || ContainingType.IsDefinition)
                {
                    return this;
                }
                return originalContainer.GetTupleMemberSymbolForUnderlyingMember(_underlyingField.OriginalDefinition)!;
            }
        }

        public override Symbol ContainingSymbol
        {
            get
            {
                return _containingTuple;
            }
        }

        internal override TypeWithAnnotations GetFieldType(ConsList<FieldSymbol> fieldsBeingBound)
        {
            return _underlyingField.GetFieldType(fieldsBeingBound);
        }

        public override ImmutableArray<CSharpAttributeData> GetAttributes()
        {
            return _underlyingField.GetAttributes();
        }

        internal override DiagnosticInfo GetUseSiteDiagnostic()
        {
            return _underlyingField.GetUseSiteDiagnostic();
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
            var other = obj as TupleFieldSymbol;

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
    }

    /// <summary>
    /// Represents an element field of a tuple type (such as (int, byte).Item1)
    /// that is backed by a real field with the same name within the tuple underlying type.
    /// </summary>
    internal class TupleElementFieldSymbol : TupleFieldSymbol
    {
        private readonly ImmutableArray<Location> _locations;
        protected readonly TupleElementFieldSymbol _correspondingDefaultField;

        // default tuple elements like Item1 or Item20 could be provided by the user or
        // otherwise implicitly declared by compiler
        private readonly bool _isImplicitlyDeclared;

        public TupleElementFieldSymbol(
            NamedTypeSymbol container,
            FieldSymbol underlyingField,
            int tupleElementIndex,
            ImmutableArray<Location> locations,
            bool isImplicitlyDeclared,
            TupleElementFieldSymbol? correspondingDefaultFieldOpt)
            : base(container, underlyingField, correspondingDefaultFieldOpt is null ? tupleElementIndex << 1 : (tupleElementIndex << 1) + 1)
        {
            Debug.Assert(container.IsTupleType);
            Debug.Assert(!locations.IsDefault);
            _locations = locations;
            _isImplicitlyDeclared = isImplicitlyDeclared;
            _correspondingDefaultField = correspondingDefaultFieldOpt ?? this;
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

        public override Symbol? AssociatedSymbol
        {
            get
            {
                if (!TypeSymbol.Equals(_underlyingField.ContainingType, _containingTuple.TupleUnderlyingType, TypeCompareKind.ConsiderEverything))
                {
                    return null;
                }

                return base.AssociatedSymbol;
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
            return new TupleElementFieldSymbol(newOwner, _underlyingField.OriginalDefinition.AsMember(newUnderlyingOwner), TupleElementIndex, Locations, IsImplicitlyDeclared, correspondingDefaultFieldOpt: null);
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
            TupleElementFieldSymbol? correspondingDefaultFieldOpt)
            : base(container, underlyingField, tupleElementIndex, locations, isImplicitlyDeclared, correspondingDefaultFieldOpt)
        {
            // The underlying field for 'Hanna' (an 8-th named element) in a long tuple is Item1. The corresponding field is Item8.

            Debug.Assert(container.IsTupleType);
            Debug.Assert(underlyingField.ContainingType.IsTupleType);
            RoslynDebug.Assert(name != null);
            Debug.Assert(name != underlyingField.Name || !container.Equals(underlyingField.ContainingType, TypeCompareKind.IgnoreDynamicAndTupleNames),
                                "fields that map directly to underlying should not be represented by " + nameof(TupleVirtualElementFieldSymbol));

            _name = name;
            _cannotUse = cannotUse;
        }

        internal override DiagnosticInfo GetUseSiteDiagnostic()
        {
            if (_cannotUse)
            {
                return new CSDiagnosticInfo(ErrorCode.ERR_TupleInferredNamesNotAvailable, _name,
                    new CSharpRequiredLanguageVersion(MessageID.IDS_FeatureInferredTupleNames.RequiredVersion()));
            }

            return base.GetUseSiteDiagnostic();
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

        public override Symbol? AssociatedSymbol
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

            TupleElementFieldSymbol? newCorrespondingDefaultFieldOpt = null;
            if ((object)_correspondingDefaultField != this)
            {
                newCorrespondingDefaultFieldOpt = (TupleElementFieldSymbol)_correspondingDefaultField.AsMember(newOwner);
            }

            return new TupleVirtualElementFieldSymbol(newOwner, _underlyingField.OriginalDefinition.AsMember(newUnderlyingOwner), _name, TupleElementIndex, Locations, _cannotUse, IsImplicitlyDeclared, newCorrespondingDefaultFieldOpt);
        }
    }
}
