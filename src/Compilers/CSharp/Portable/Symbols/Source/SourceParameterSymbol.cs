// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Diagnostics;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Symbols.Metadata.PE;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    /// <summary>
    /// Base class for parameters can be referred to from source code.
    /// </summary>
    /// <remarks>
    /// These parameters can potentially be targetted by an attribute specified in source code. 
    /// As an optimization we distinguish simple parameters (no attributes, no modifiers, etc.) and complex parameters.
    /// </remarks>
    internal abstract class SourceParameterSymbol : SourceParameterSymbolBase
    {
        protected SymbolCompletionState state;
        protected readonly TypeSymbolWithAnnotations parameterType;
        private readonly string _name;
        private readonly ImmutableArray<Location> _locations;
        private readonly RefKind _refKind;

        public static SourceParameterSymbol Create(
            Binder context,
            Symbol owner,
            TypeSymbolWithAnnotations parameterType,
            ParameterSyntax syntax,
            RefKind refKind,
            SyntaxToken identifier,
            int ordinal,
            bool isParams,
            bool isExtensionMethodThis,
            DiagnosticBag diagnostics,
            bool beStrict)
        {
            var name = identifier.ValueText;
            var locations = ImmutableArray.Create<Location>(new SourceLocation(identifier));

            if (isParams)
            {
                // touch the constructor in order to generate proper use-site diagnostics
                Binder.ReportUseSiteDiagnosticForSynthesizedAttribute(context.Compilation,
                    WellKnownMember.System_ParamArrayAttribute__ctor,
                    diagnostics,
                    identifier.Parent.GetLocation());
            }

            if (!isParams &&
                !isExtensionMethodThis &&
                (syntax.Default == null) &&
                (syntax.AttributeLists.Count == 0) &&
                !owner.IsPartialMethod())
            {
                return new SourceSimpleParameterSymbol(owner, parameterType, ordinal, refKind, name, locations);
            }

            if (beStrict)
            {
                return new SourceStrictComplexParameterSymbol(
                    diagnostics,
                    context,
                    owner,
                    ordinal,
                    parameterType,
                    refKind,
                    false,
                    name,
                    locations,
                    syntax.GetReference(),
                    ConstantValue.Unset,
                    isParams,
                    isExtensionMethodThis);
            }

            return new SourceComplexParameterSymbol(
                owner,
                ordinal,
                parameterType,
                refKind,
                name,
                locations,
                syntax.GetReference(),
                ConstantValue.Unset,
                isParams,
                isExtensionMethodThis);
        }

        protected SourceParameterSymbol(
            Symbol owner,
            TypeSymbolWithAnnotations parameterType,
            int ordinal,
            RefKind refKind,
            string name,
            ImmutableArray<Location> locations)
            : base(owner, ordinal)
        {
            Debug.Assert((owner.Kind == SymbolKind.Method) || (owner.Kind == SymbolKind.Property));
            this.parameterType = parameterType;
            _refKind = refKind;
            _name = name;
            _locations = locations;
        }

        internal override ParameterSymbol WithCustomModifiersAndParams(TypeSymbol newType, ImmutableArray<CustomModifier> newCustomModifiers, ushort countOfCustomModifiersPrecedingByRef, bool newIsParams)
        {
            return WithCustomModifiersAndParamsCore(newType, newCustomModifiers, countOfCustomModifiersPrecedingByRef, newIsParams);
        }

        internal SourceParameterSymbol WithCustomModifiersAndParamsCore(TypeSymbol newType, ImmutableArray<CustomModifier> newCustomModifiers, ushort countOfCustomModifiersPrecedingByRef, bool newIsParams)
        {
            newType = CustomModifierUtils.CopyTypeCustomModifiers(newType, this.Type.TypeSymbol, _refKind, this.ContainingAssembly);

            TypeSymbolWithAnnotations newTypeWithModifiers = this.Type.Update(newType, newCustomModifiers);

            if (countOfCustomModifiersPrecedingByRef == 0)
            {
                return new SourceComplexParameterSymbol(
                    this.ContainingSymbol,
                    this.Ordinal,
                    newTypeWithModifiers,
                    _refKind,
                    _name,
                    _locations,
                    this.SyntaxReference,
                    this.ExplicitDefaultConstantValue,
                    newIsParams,
                    this.IsExtensionMethodThis);
            }

            return new SourceComplexParameterSymbolWithCustomModifiersPrecedingByRef(
                this.ContainingSymbol,
                this.Ordinal,
                newTypeWithModifiers,
                _refKind,
                countOfCustomModifiersPrecedingByRef,
                _name,
                _locations,
                this.SyntaxReference,
                this.ExplicitDefaultConstantValue,
                newIsParams,
                this.IsExtensionMethodThis);
        }

        internal sealed override bool RequiresCompletion
        {
            get { return true; }
        }

        internal sealed override bool HasComplete(CompletionPart part)
        {
            return state.HasComplete(part);
        }

        internal override void ForceComplete(SourceLocation locationOpt, CancellationToken cancellationToken)
        {
            state.DefaultForceComplete(this);
        }

        /// <summary>
        /// True if the parameter is marked by <see cref="System.Runtime.InteropServices.OptionalAttribute"/>.
        /// </summary>
        internal abstract bool HasOptionalAttribute { get; }

        /// <summary>
        /// True if the parameter has default argument syntax.
        /// </summary>
        internal abstract bool HasDefaultArgumentSyntax { get; }

        internal abstract SyntaxList<AttributeListSyntax> AttributeDeclarationList { get; }

        internal abstract CustomAttributesBag<CSharpAttributeData> GetAttributesBag(DiagnosticBag diagnosticsOpt);

        /// <summary>
        /// Gets the attributes applied on this symbol.
        /// Returns an empty array if there are no attributes.
        /// </summary>
        /// <remarks>
        /// NOTE: This method should always be kept as a sealed override.
        /// If you want to override attribute binding logic for a sub-class, then override <see cref="GetAttributesBag"/> method.
        /// </remarks>
        public sealed override ImmutableArray<CSharpAttributeData> GetAttributes()
        {
            return this.GetAttributesBag(null).Attributes;
        }

        internal abstract SyntaxReference SyntaxReference { get; }

        internal abstract bool IsExtensionMethodThis { get; }

        public sealed override RefKind RefKind
        {
            get
            {
                return _refKind;
            }
        }

        public sealed override string Name
        {
            get
            {
                return _name;
            }
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
                return IsImplicitlyDeclared ?
                    ImmutableArray<SyntaxReference>.Empty :
                    GetDeclaringSyntaxReferenceHelper<ParameterSyntax>(_locations);
            }
        }

        public sealed override TypeSymbolWithAnnotations Type
        {
            get
            {
                return this.parameterType;
            }
        }

        public override bool IsImplicitlyDeclared
        {
            get
            {
                // Parameters of accessors are always synthesized. (e.g., parameter of indexer accessors).
                // The non-synthesized accessors are on the property/event itself.
                MethodSymbol owningMethod = ContainingSymbol as MethodSymbol;
                return (object)owningMethod != null && owningMethod.IsAccessor();
            }
        }
    }
}
