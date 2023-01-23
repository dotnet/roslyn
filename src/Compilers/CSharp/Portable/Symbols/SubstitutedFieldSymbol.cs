// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Immutable;
using System.Threading;
using Roslyn.Utilities;
using Microsoft.CodeAnalysis.CSharp.Emit;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    internal sealed class SubstitutedFieldSymbol : WrappedFieldSymbol
    {
        private readonly SubstitutedNamedTypeSymbol _containingType;

        private TypeWithAnnotations.Boxed _lazyType;

        internal SubstitutedFieldSymbol(SubstitutedNamedTypeSymbol containingType, FieldSymbol substitutedFrom)
            : base((FieldSymbol)substitutedFrom.OriginalDefinition)
        {
            _containingType = containingType;
        }

        internal override TypeWithAnnotations GetFieldType(ConsList<FieldSymbol> fieldsBeingBound)
        {
            if (_lazyType == null)
            {
                var type = _containingType.TypeSubstitution.SubstituteType(OriginalDefinition.GetFieldType(fieldsBeingBound));
                Interlocked.CompareExchange(ref _lazyType, new TypeWithAnnotations.Boxed(type), null);
            }

            return _lazyType.Value;
        }

        public override Symbol ContainingSymbol
        {
            get
            {
                return _containingType;
            }
        }

        public override NamedTypeSymbol ContainingType
        {
            get
            {
                return _containingType;
            }
        }

        public override FieldSymbol OriginalDefinition
        {
            get
            {
                return _underlyingField;
            }
        }

        public override bool IsImplicitlyDeclared
        {
            get
            {
                if (this.ContainingType.IsTupleType && this.IsDefaultTupleElement)
                {
                    // To improve backwards compatibility with earlier implementation of tuples,
                    // we pretend that default tuple element fields are implicitly declared, despite having locations
                    return true;
                }

                return base.IsImplicitlyDeclared;
            }
        }

        public override ImmutableArray<CSharpAttributeData> GetAttributes()
        {
            return OriginalDefinition.GetAttributes();
        }

        public override Symbol AssociatedSymbol
        {
            get
            {
                Symbol underlying = OriginalDefinition.AssociatedSymbol;

                if ((object)underlying == null)
                {
                    return null;
                }

                return underlying.SymbolAsMember(ContainingType);
            }
        }

        internal override NamedTypeSymbol FixedImplementationType(PEModuleBuilder emitModule)
        {
            // This occurs rarely, if ever.  The scenario would be a generic struct
            // containing a fixed-size buffer.  Given the rarity there would be little
            // benefit to "optimizing" the performance of this by caching the
            // translated implementation type.
            return (NamedTypeSymbol)_containingType.TypeSubstitution.SubstituteType(OriginalDefinition.FixedImplementationType(emitModule)).Type;
        }

        public override RefKind RefKind => _underlyingField.RefKind;

        public override ImmutableArray<CustomModifier> RefCustomModifiers =>
            _containingType.TypeSubstitution.SubstituteCustomModifiers(_underlyingField.RefCustomModifiers);

        public override bool Equals(Symbol obj, TypeCompareKind compareKind)
        {
            if ((object)this == obj)
            {
                return true;
            }

            var other = obj as FieldSymbol;
            return (object)other != null && TypeSymbol.Equals(_containingType, other.ContainingType, compareKind) && OriginalDefinition == other.OriginalDefinition;
        }

        public override int GetHashCode()
        {
            var code = this.OriginalDefinition.GetHashCode();

            // If the containing type of the original definition is the same as our containing type
            // it's possible that we will compare equal to the original definition under certain conditions 
            // (e.g, ignoring nullability) and want to retain the same hashcode. As such only make
            // the containing type part of the hashcode when we know equality isn't possible
            var containingHashCode = _containingType.GetHashCode();
            if (containingHashCode != this.OriginalDefinition.ContainingType.GetHashCode())
            {
                code = Hash.Combine(containingHashCode, code);
            }

            return code;
        }
    }
}
