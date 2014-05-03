// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
        private readonly SubstitutedNamedTypeSymbol containingType;
        private readonly FieldSymbol originalDefinition;

        private TypeSymbol lazyType;

        internal SubstitutedFieldSymbol(SubstitutedNamedTypeSymbol containingType, FieldSymbol substitutedFrom)
        {
            this.containingType = containingType;
            this.originalDefinition = substitutedFrom.OriginalDefinition as FieldSymbol;
        }

        internal override TypeSymbol GetFieldType(ConsList<FieldSymbol> fieldsBeingBound)
        {
            if ((object)this.lazyType == null)
            {
                Interlocked.CompareExchange(ref this.lazyType, containingType.TypeSubstitution.SubstituteType(originalDefinition.GetFieldType(fieldsBeingBound)), null);
            }

            return this.lazyType;
        }

        public override string Name
        {
            get
            {
                return originalDefinition.Name;
            }
        }

        public override string GetDocumentationCommentXml(CultureInfo preferredCulture = null, bool expandIncludes = false, CancellationToken cancellationToken = default(CancellationToken))
        {
            return originalDefinition.GetDocumentationCommentXml(preferredCulture, expandIncludes, cancellationToken);
        }

        internal override bool HasSpecialName
        {
            get
            {
                return originalDefinition.HasSpecialName;
            }
        }

        internal override bool HasRuntimeSpecialName
        {
            get
            {
                return originalDefinition.HasRuntimeSpecialName;
            }
        }

        internal override bool IsNotSerialized
        {
            get
            {
                return originalDefinition.IsNotSerialized;
            }
        }

        internal override MarshalPseudoCustomAttributeData MarshallingInformation
        {
            get
            {
                return originalDefinition.MarshallingInformation;
            }
        }

        internal override int? TypeLayoutOffset
        {
            get
            {
                return originalDefinition.TypeLayoutOffset;
            }
        }

        public override Symbol ContainingSymbol
        {
            get
            {
                return containingType;
            }
        }

        public override NamedTypeSymbol ContainingType
        {
            get
            {
                return containingType;
            }
        }

        public override FieldSymbol OriginalDefinition
        {
            get
            {
                return originalDefinition.OriginalDefinition;
            }
        }

        public override ImmutableArray<Location> Locations
        {
            get
            {
                return originalDefinition.Locations;
            }
        }

        public override ImmutableArray<SyntaxReference> DeclaringSyntaxReferences
        {
            get
            {
                return originalDefinition.DeclaringSyntaxReferences;
            }
        }

        public override ImmutableArray<CSharpAttributeData> GetAttributes()
        {
            return this.originalDefinition.GetAttributes();
        }

        public override Symbol AssociatedSymbol
        {
            get
            {
                Symbol underlying = originalDefinition.AssociatedSymbol;

                if ((object) underlying == null)
                {
                    return null;
                }

                if (underlying.Kind == SymbolKind.Parameter)
                {
                    // This can only be a parameter of a Primary Constructor
                    var parameter = (ParameterSymbol)underlying;
                    return ((MethodSymbol)parameter.ContainingSymbol.SymbolAsMember(ContainingType)).Parameters[parameter.Ordinal];
                }

                return underlying.SymbolAsMember(ContainingType);
            }
        }

        public override bool IsStatic
        {
            get
            {
                return originalDefinition.IsStatic;
            }
        }

        public override bool IsReadOnly
        {
            get
            {
                return originalDefinition.IsReadOnly;
            }
        }

        public override bool IsConst
        {
            get
            {
                return originalDefinition.IsConst;
            }
        }

        internal override ObsoleteAttributeData ObsoleteAttributeData
        {
            get
            {
                return originalDefinition.ObsoleteAttributeData;
            }
        }

        public override object ConstantValue
        {
            get
            {
                return originalDefinition.ConstantValue;
            }
        }

        internal override ConstantValue GetConstantValue(ConstantFieldsInProgress inProgress, bool earlyDecodingWellKnownAttributes)
        {
            return originalDefinition.GetConstantValue(inProgress, earlyDecodingWellKnownAttributes);
        }

        public override bool IsVolatile
        {
            get
            {
                return originalDefinition.IsVolatile;
            }
        }

        public override bool IsImplicitlyDeclared
        {
            get
            {
                return this.originalDefinition.IsImplicitlyDeclared;
            }
        }

        public override Accessibility DeclaredAccessibility
        {
            get
            {
                return originalDefinition.DeclaredAccessibility;
            }
        }

        public override ImmutableArray<CustomModifier> CustomModifiers
        {
            get
            {
                return originalDefinition.CustomModifiers;
            }
        }

        internal override NamedTypeSymbol FixedImplementationType(PEModuleBuilder emitModule)
        {
            // This occurs rarely, if ever.  The scenario would be a generic struct
            // containing a fixed-size buffer.  Given the rarity there would be little
            // benefit to "optimizing" the performance of this by cacheing the
            // translated implementation type.
            return (NamedTypeSymbol)containingType.TypeSubstitution.SubstituteType(originalDefinition.FixedImplementationType(emitModule));
        }

        public override bool Equals(object obj)
        {
            if ((object)this == obj)
            {
                return true;
            }

            var other = obj as SubstitutedFieldSymbol;
            return (object)other != null && this.containingType == other.containingType && this.originalDefinition == other.originalDefinition;
        }

        public override int GetHashCode()
        {
            return Hash.Combine(containingType, originalDefinition.GetHashCode());
        }
    }
}