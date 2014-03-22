using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    /// <summary>
    /// A Primary Constructor parameter that will have backing field fo sure.
    /// </summary>
    internal class SourcePrimaryConstructorParameterSymbolWithBackingField : SourceComplexParameterSymbol, IAttributeTargetSymbol
    {
        private readonly BackingField backingField;

        internal SourcePrimaryConstructorParameterSymbolWithBackingField(
            Symbol owner,
            int ordinal,
            TypeSymbol parameterType,
            RefKind refKind,
            ImmutableArray<CustomModifier> customModifiers,
            bool hasByRefBeforeCustomModifiers,
            string name,
            ImmutableArray<Location> locations,
            SyntaxReference syntaxRef,
            ConstantValue defaultSyntaxValue,
            bool isParams,
            bool isExtensionMethodThis
        ) : base(owner, ordinal, parameterType, refKind, customModifiers, hasByRefBeforeCustomModifiers, name, locations, syntaxRef, defaultSyntaxValue, isParams, isExtensionMethodThis)
        {
            backingField = new BackingField(this);
        }

        internal override ParameterSymbol WithCustomModifiersAndParams(TypeSymbol newType, ImmutableArray<CustomModifier> newCustomModifiers, bool hasByRefBeforeCustomModifiers, bool newIsParams)
        {
            throw ExceptionUtilities.Unreachable;
        }

        AttributeLocation IAttributeTargetSymbol.AllowedAttributeLocations
        {
            get { return AttributeLocation.Parameter | AttributeLocation.Field; }
        }

        internal override void ForceComplete(SourceLocation locationOpt, CancellationToken cancellationToken)
        {
            base.ForceComplete(locationOpt, cancellationToken);

            this.backingField.ForceComplete(locationOpt, cancellationToken);
        }

        internal override FieldSymbol PrimaryConstructorParameterBackingField
        {
            get
            {
                return backingField;
            }
        }

        internal sealed class BackingField : SourceFieldSymbol
        {
            private readonly SourceParameterSymbol parameterSymbol;

            internal BackingField(SourceParameterSymbol parameterSymbol)
                : base((SourceMemberContainerTypeSymbol)parameterSymbol.ContainingType)
            {
                this.parameterSymbol = parameterSymbol;
            }

            public override string Name
            {
                get
                {
                    return parameterSymbol.Name;
                }
            }

            public override Accessibility DeclaredAccessibility
            {
                get
                {
                    return Accessibility.Private;
                }
            }

            public override bool IsReadOnly
            {
                get
                {
                    return false;
                }
            }

            public override bool IsStatic
            {
                get
                {
                    return false;
                }
            }

            public override bool IsConst
            {
                get
                {
                    return false;
                }
            }

            public override bool IsVolatile
            {
                get
                {
                    return false;
                }
            }

            internal override TypeSymbol GetFieldType(ConsList<FieldSymbol> fieldsBeingBound)
            {
                return parameterSymbol.Type;
            }

            public override ImmutableArray<CustomModifier> CustomModifiers
            {
                get
                {
                    return parameterSymbol.CustomModifiers;
                }
            }

            protected override ConstantValue MakeConstantValue(HashSet<SourceFieldSymbol> dependencies, bool earlyDecodingWellKnownAttributes, DiagnosticBag diagnostics)
            {
                return null;
            }

            public override Symbol AssociatedPropertyOrEvent
            {
                get
                {
                    return null;
                }
            }

            public override ImmutableArray<SyntaxReference> DeclaringSyntaxReferences
            {
                get
                {
                    return ImmutableArray<SyntaxReference>.Empty;
                }
            }

            public override bool IsImplicitlyDeclared
            {
                get
                {
                    return true;
                }
            }

            public override ImmutableArray<Location> Locations
            {
                get
                {
                    return ImmutableArray<Location>.Empty;
                }
            }

            internal override Location Location
            {
                get
                {
                    return parameterSymbol.Locations[0];
                }
            }

            protected override IAttributeTargetSymbol AttributeOwner
            {
                get
                {
                    return ((IAttributeTargetSymbol)(parameterSymbol as SourcePrimaryConstructorParameterSymbolWithBackingField)) ?? this;
                }
            }

            protected override SyntaxList<AttributeListSyntax> AttributeDeclarationSyntaxList
            {
                get
                {
                    // TODO: What to do about ERR_MissingStructOffset when there is no attributes for a field?
                    //       Should it be reported even if field is not created?
                    return (object)AttributeOwner == (object)this ?
                        default(SyntaxList<AttributeListSyntax>) :
                        parameterSymbol.AttributeDeclarationList;
                }
            }
        }
    }
}