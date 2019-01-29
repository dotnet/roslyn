// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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

        private TypeSymbolWithAnnotations.Builder _lazyType;

        internal SubstitutedFieldSymbol(SubstitutedNamedTypeSymbol containingType, FieldSymbol substitutedFrom)
            : base((FieldSymbol)substitutedFrom.OriginalDefinition)
        {
            _containingType = containingType;
        }

        internal override TypeSymbolWithAnnotations GetFieldType(ConsList<FieldSymbol> fieldsBeingBound)
        {
            if (_lazyType.IsNull)
            {
                _lazyType.InterlockedInitialize(_containingType.TypeSubstitution.SubstituteTypeWithTupleUnification(OriginalDefinition.GetFieldType(fieldsBeingBound)));
            }

            return _lazyType.ToType();
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

        internal override NamedTypeSymbol FixedImplementationType(PEModuleBuilder emitModule)
        {
            // This occurs rarely, if ever.  The scenario would be a generic struct
            // containing a fixed-size buffer.  Given the rarity there would be little
            // benefit to "optimizing" the performance of this by caching the
            // translated implementation type.
            return (NamedTypeSymbol)_containingType.TypeSubstitution.SubstituteType(OriginalDefinition.FixedImplementationType(emitModule)).TypeSymbol;
        }

        public override bool Equals(object obj)
        {
            if ((object)this == obj)
            {
                return true;
            }

            var other = obj as SubstitutedFieldSymbol;
            return (object)other != null && TypeSymbol.Equals(_containingType, other._containingType, TypeCompareKind.ConsiderEverything2) && OriginalDefinition == other.OriginalDefinition;
        }

        public override int GetHashCode()
        {
            return Hash.Combine(_containingType, OriginalDefinition.GetHashCode());
        }
    }
}
