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
            string name,
            ImmutableArray<Location> locations,
            ParameterSyntax syntax,
            ConstantValue defaultSyntaxValue,
            bool isParams,
            bool isExtensionMethodThis,
            DiagnosticBag diagnostics
        ) : base(owner, ordinal, parameterType, refKind, ImmutableArray<CustomModifier>.Empty, false, name, locations, syntax.GetReference(), defaultSyntaxValue, isParams, isExtensionMethodThis)
        {
            bool modifierErrors;
            var modifiers = SourceMemberFieldSymbol.MakeModifiers(owner.ContainingType, syntax.Identifier, syntax.Modifiers, diagnostics, out modifierErrors, ignoreParameterModifiers: true);

            backingField = new BackingField(this, modifiers, modifierErrors, diagnostics);
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

            if ((object)this.ContainingSymbol != (object)((SourceMemberContainerTypeSymbol)this.ContainingType).PrimaryCtor)
            {
                this.backingField.ForceComplete(locationOpt, cancellationToken);
            }
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
            private readonly DeclarationModifiers modifiers;
            private ImmutableArray<CustomModifier> lazyCustomModifiers;

            internal BackingField(
                SourceParameterSymbol parameterSymbol,
                DeclarationModifiers modifiers,
                bool modifierErrors,
                DiagnosticBag diagnostics)
                : base((SourceMemberContainerTypeSymbol)parameterSymbol.ContainingType)
            {
                this.parameterSymbol = parameterSymbol;
                this.modifiers = modifiers;

                this.CheckAccessibility(diagnostics);

                if (!modifierErrors)
                {
                    this.ReportModifiersDiagnostics(diagnostics);
                }
            }

            public override string Name
            {
                get
                {
                    return parameterSymbol.Name;
                }
            }

            public override Symbol AssociatedSymbol
            {
                get
                {
                    return parameterSymbol;
                }
            }

            protected override DeclarationModifiers Modifiers
            {
                get
                {
                    return modifiers;
                }
            }

            internal override TypeSymbol GetFieldType(ConsList<FieldSymbol> fieldsBeingBound)
            {
                return parameterSymbol.Type;
            }

            public sealed override ImmutableArray<CustomModifier> CustomModifiers
            {
                get
                {
                    if (lazyCustomModifiers.IsDefault)
                    {
                        ImmutableInterlocked.InterlockedCompareExchange(ref lazyCustomModifiers, base.CustomModifiers, default(ImmutableArray<CustomModifier>));
                    }

                    return lazyCustomModifiers;
                }
            }

            internal override ConstantValue GetConstantValue(ConstantFieldsInProgress inProgress, bool earlyDecodingWellKnownAttributes)
            {
                return null;
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
                    return parameterSymbol.Locations;
                }
            }

            internal override LexicalSortKey GetLexicalSortKey()
            {
                return new LexicalSortKey(parameterSymbol.Locations[0], this.DeclaringCompilation);
            }

            internal override Location ErrorLocation
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