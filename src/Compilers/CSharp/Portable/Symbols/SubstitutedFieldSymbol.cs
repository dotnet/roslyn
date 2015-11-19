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
    internal sealed class SubstitutedFieldSymbol : FieldSymbol
    {
        private readonly SubstitutedNamedTypeSymbol _containingType;
        private readonly FieldSymbol _originalDefinition;

        private TypeSymbol _lazyType;

        internal SubstitutedFieldSymbol(SubstitutedNamedTypeSymbol containingType, FieldSymbol substitutedFrom)
        {
            _containingType = containingType;
            _originalDefinition = substitutedFrom.OriginalDefinition as FieldSymbol;
        }

        internal override TypeSymbol GetFieldType(ConsList<FieldSymbol> fieldsBeingBound)
        {
            if ((object)_lazyType == null)
            {
                Interlocked.CompareExchange(ref _lazyType, _containingType.TypeSubstitution.SubstituteType(_originalDefinition.GetFieldType(fieldsBeingBound)).Type, null);
            }

            return _lazyType;
        }

        public override string Name
        {
            get
            {
                return _originalDefinition.Name;
            }
        }

        public override string GetDocumentationCommentXml(CultureInfo preferredCulture = null, bool expandIncludes = false, CancellationToken cancellationToken = default(CancellationToken))
        {
            return _originalDefinition.GetDocumentationCommentXml(preferredCulture, expandIncludes, cancellationToken);
        }

        internal override bool HasSpecialName
        {
            get
            {
                return _originalDefinition.HasSpecialName;
            }
        }

        internal override bool HasRuntimeSpecialName
        {
            get
            {
                return _originalDefinition.HasRuntimeSpecialName;
            }
        }

        internal override bool IsNotSerialized
        {
            get
            {
                return _originalDefinition.IsNotSerialized;
            }
        }

        internal override MarshalPseudoCustomAttributeData MarshallingInformation
        {
            get
            {
                return _originalDefinition.MarshallingInformation;
            }
        }

        internal override int? TypeLayoutOffset
        {
            get
            {
                return _originalDefinition.TypeLayoutOffset;
            }
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
                return _originalDefinition.OriginalDefinition;
            }
        }

        public override ImmutableArray<Location> Locations
        {
            get
            {
                return _originalDefinition.Locations;
            }
        }

        public override ImmutableArray<SyntaxReference> DeclaringSyntaxReferences
        {
            get
            {
                return _originalDefinition.DeclaringSyntaxReferences;
            }
        }

        public override ImmutableArray<CSharpAttributeData> GetAttributes()
        {
            return _originalDefinition.GetAttributes();
        }

        public override Symbol AssociatedSymbol
        {
            get
            {
                Symbol underlying = _originalDefinition.AssociatedSymbol;

                if ((object)underlying == null)
                {
                    return null;
                }

                return underlying.SymbolAsMember(ContainingType);
            }
        }

        public override bool IsStatic
        {
            get
            {
                return _originalDefinition.IsStatic;
            }
        }

        public override bool IsReadOnly
        {
            get
            {
                return _originalDefinition.IsReadOnly;
            }
        }

        public override bool IsConst
        {
            get
            {
                return _originalDefinition.IsConst;
            }
        }

        internal override ObsoleteAttributeData ObsoleteAttributeData
        {
            get
            {
                return _originalDefinition.ObsoleteAttributeData;
            }
        }

        public override object ConstantValue
        {
            get
            {
                return _originalDefinition.ConstantValue;
            }
        }

        internal override ConstantValue GetConstantValue(ConstantFieldsInProgress inProgress, bool earlyDecodingWellKnownAttributes)
        {
            return _originalDefinition.GetConstantValue(inProgress, earlyDecodingWellKnownAttributes);
        }

        public override bool IsVolatile
        {
            get
            {
                return _originalDefinition.IsVolatile;
            }
        }

        public override bool IsImplicitlyDeclared
        {
            get
            {
                return _originalDefinition.IsImplicitlyDeclared;
            }
        }

        public override Accessibility DeclaredAccessibility
        {
            get
            {
                return _originalDefinition.DeclaredAccessibility;
            }
        }

        public override ImmutableArray<CustomModifier> CustomModifiers
        {
            get
            {
                return _containingType.TypeSubstitution.SubstituteCustomModifiers(_originalDefinition.Type,_originalDefinition.CustomModifiers);
            }
        }

        internal override NamedTypeSymbol FixedImplementationType(PEModuleBuilder emitModule)
        {
            // This occurs rarely, if ever.  The scenario would be a generic struct
            // containing a fixed-size buffer.  Given the rarity there would be little
            // benefit to "optimizing" the performance of this by caching the
            // translated implementation type.
            return (NamedTypeSymbol)_containingType.TypeSubstitution.SubstituteType(_originalDefinition.FixedImplementationType(emitModule)).Type;
        }

        public override bool Equals(object obj)
        {
            if ((object)this == obj)
            {
                return true;
            }

            var other = obj as SubstitutedFieldSymbol;
            return (object)other != null && _containingType == other._containingType && _originalDefinition == other._originalDefinition;
        }

        public override int GetHashCode()
        {
            return Hash.Combine(_containingType, _originalDefinition.GetHashCode());
        }
    }
}
