// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;
using Microsoft.CodeAnalysis.CSharp.Emit;
using System.Globalization;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    internal sealed class SubstitutedFieldSymbol : WrappedFieldSymbol
    {
        private readonly SubstitutedNamedTypeSymbol _containingType;
        private TypeSymbol _lazyType;

        internal SubstitutedFieldSymbol(SubstitutedNamedTypeSymbol containingType, FieldSymbol substitutedFrom)
            : base((FieldSymbol)substitutedFrom.OriginalDefinition)
        {
            _containingType = containingType;
        }

        internal override TypeSymbol GetFieldType(ConsList<FieldSymbol> fieldsBeingBound)
        {
            if ((object)_lazyType == null)
            {
                Interlocked.CompareExchange(ref _lazyType, _containingType.TypeSubstitution.SubstituteTypeWithTupleUnification(OriginalDefinition.GetFieldType(fieldsBeingBound)).Type, null);
            }

            return _lazyType;
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

        public override ImmutableArray<CustomModifier> CustomModifiers
        {
            get
            {
                return _containingType.TypeSubstitution.SubstituteCustomModifiers(OriginalDefinition.Type, OriginalDefinition.CustomModifiers);
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

        public override bool Equals(object obj)
        {
            if ((object)this == obj)
            {
                return true;
            }

            var other = obj as SubstitutedFieldSymbol;
            return (object)other != null && _containingType == other._containingType && OriginalDefinition == other.OriginalDefinition;
        }

        public override int GetHashCode()
        {
            return Hash.Combine(_containingType, OriginalDefinition.GetHashCode());
        }
    }
}
