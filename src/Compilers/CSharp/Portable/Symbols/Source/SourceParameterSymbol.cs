// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Diagnostics;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    /// <summary>
    /// Base class for parameters can be referred to from source code.
    /// </summary>
    /// <remarks>
    /// These parameters can potentially be targeted by an attribute specified in source code. 
    /// As an optimization we distinguish simple parameters (no attributes, no modifiers, etc.) and complex parameters.
    /// </remarks>
    internal abstract class SourceParameterSymbol : SourceParameterSymbolBase
    {
        protected SymbolCompletionState state;
        protected readonly TypeWithAnnotations parameterType;
        private readonly string _name;
        private readonly Location? _location;
        private readonly RefKind _refKind;
        private readonly ScopedKind _scope;
#nullable disable

        public static SourceParameterSymbol Create(
            Binder context,
            Symbol owner,
            TypeWithAnnotations parameterType,
            ParameterSyntax syntax,
            RefKind refKind,
            SyntaxToken identifier,
            int ordinal,
            bool isParams,
            bool isExtensionMethodThis,
            bool addRefReadOnlyModifier,
            ScopedKind scope,
            BindingDiagnosticBag declarationDiagnostics)
        {
            Debug.Assert(owner is not LambdaSymbol); // therefore we don't need to deal with discard parameters

            var name = identifier.ValueText;
            var location = new SourceLocation(identifier);

            if (isParams)
            {
                // touch the constructor in order to generate proper use-site diagnostics
                Binder.ReportUseSiteDiagnosticForSynthesizedAttribute(context.Compilation,
                    WellKnownMember.System_ParamArrayAttribute__ctor,
                    declarationDiagnostics,
                    identifier.Parent.GetLocation());
            }

            ImmutableArray<CustomModifier> inModifiers = ParameterHelpers.ConditionallyCreateInModifiers(refKind, addRefReadOnlyModifier, context, declarationDiagnostics, syntax);
            Debug.Assert(!inModifiers.IsDefault);

            if (!inModifiers.IsDefaultOrEmpty)
            {
                return new SourceComplexParameterSymbolWithCustomModifiersPrecedingRef(
                    owner,
                    ordinal,
                    parameterType,
                    refKind,
                    inModifiers,
                    name,
                    location,
                    syntax.GetReference(),
                    isParams,
                    isExtensionMethodThis,
                    scope);
            }

            if (!isParams &&
                !isExtensionMethodThis &&
                (syntax.Default == null) &&
                (syntax.AttributeLists.Count == 0) &&
                !owner.IsPartialMethod())
            {
                return new SourceSimpleParameterSymbol(owner, parameterType, ordinal, refKind, scope, name, location);
            }

            return new SourceComplexParameterSymbol(
                owner,
                ordinal,
                parameterType,
                refKind,
                name,
                location,
                syntax.GetReference(),
                isParams,
                isExtensionMethodThis,
                scope);
        }

        protected SourceParameterSymbol(
            Symbol owner,
            TypeWithAnnotations parameterType,
            int ordinal,
            RefKind refKind,
            ScopedKind scope,
            string name,
            Location location)
            : base(owner, ordinal)
        {
            Debug.Assert((owner.Kind == SymbolKind.Method) || (owner.Kind == SymbolKind.Property));
            this.parameterType = parameterType;
            _refKind = refKind;
            _scope = scope;
            _name = name;
            _location = location;
        }

        internal override ParameterSymbol WithCustomModifiersAndParams(TypeSymbol newType, ImmutableArray<CustomModifier> newCustomModifiers, ImmutableArray<CustomModifier> newRefCustomModifiers, bool newIsParams)
        {
            return WithCustomModifiersAndParamsCore(newType, newCustomModifiers, newRefCustomModifiers, newIsParams);
        }

        internal SourceParameterSymbol WithCustomModifiersAndParamsCore(TypeSymbol newType, ImmutableArray<CustomModifier> newCustomModifiers, ImmutableArray<CustomModifier> newRefCustomModifiers, bool newIsParams)
        {
            newType = CustomModifierUtils.CopyTypeCustomModifiers(newType, this.Type, this.ContainingAssembly);

            TypeWithAnnotations newTypeWithModifiers = this.TypeWithAnnotations.WithTypeAndModifiers(newType, newCustomModifiers);

            if (newRefCustomModifiers.IsEmpty)
            {
                return new SourceComplexParameterSymbol(
                    this.ContainingSymbol,
                    this.Ordinal,
                    newTypeWithModifiers,
                    _refKind,
                    _name,
                    _location,
                    this.SyntaxReference,
                    newIsParams,
                    this.IsExtensionMethodThis,
                    this.DeclaredScope);
            }

            // Local functions should never have custom modifiers
            Debug.Assert(!(ContainingSymbol is LocalFunctionSymbol));

            return new SourceComplexParameterSymbolWithCustomModifiersPrecedingRef(
                this.ContainingSymbol,
                this.Ordinal,
                newTypeWithModifiers,
                _refKind,
                newRefCustomModifiers,
                _name,
                _location,
                this.SyntaxReference,
                newIsParams,
                this.IsExtensionMethodThis,
                this.DeclaredScope);
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
            state.DefaultForceComplete(this, cancellationToken);
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

        internal abstract CustomAttributesBag<CSharpAttributeData> GetAttributesBag();

        /// <summary>
        /// Gets the attributes applied on this symbol.
        /// Returns an empty array if there are no attributes.
        /// </summary>
        public sealed override ImmutableArray<CSharpAttributeData> GetAttributes()
        {
            return this.GetAttributesBag().Attributes;
        }

        /// <summary>
        /// The declaration diagnostics for a parameter depend on the containing symbol.
        /// For instance, if the containing symbol is a method the declaration diagnostics
        /// go on the compilation, but if it is a local function it is part of the local
        /// function's declaration diagnostics.
        /// </summary>
        internal override void AddDeclarationDiagnostics(BindingDiagnosticBag diagnostics)
            => ContainingSymbol.AddDeclarationDiagnostics(diagnostics);

        internal abstract SyntaxReference SyntaxReference { get; }

        internal abstract bool IsExtensionMethodThis { get; }

        public sealed override RefKind RefKind
        {
            get
            {
                return _refKind;
            }
        }

        /// <summary>
        /// The declared scope. From source, this is from the <c>scope</c> keyword only.
        /// </summary>
        internal ScopedKind DeclaredScope => _scope;

        internal abstract override ScopedKind EffectiveScope { get; }

        protected ScopedKind CalculateEffectiveScopeIgnoringAttributes()
        {
            var declaredScope = this.DeclaredScope;
            return declaredScope == ScopedKind.None && ParameterHelpers.IsRefScopedByDefault(this) ?
                ScopedKind.ScopedRef :
                declaredScope;
        }

        internal sealed override bool UseUpdatedEscapeRules => ContainingModule.UseUpdatedEscapeRules;

        public sealed override string Name
        {
            get
            {
                return _name;
            }
        }

        public sealed override ImmutableArray<Location> Locations
            => _location is null ? ImmutableArray<Location>.Empty : ImmutableArray.Create(_location);

#nullable enable

        public override Location? TryGetFirstLocation()
            => _location;

#nullable disable

        public sealed override ImmutableArray<SyntaxReference> DeclaringSyntaxReferences
        {
            get
            {
                return IsImplicitlyDeclared ?
                    ImmutableArray<SyntaxReference>.Empty :
                    GetDeclaringSyntaxReferenceHelper<ParameterSyntax>(this.Locations);
            }
        }

        public sealed override TypeWithAnnotations TypeWithAnnotations
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

        internal override bool IsMetadataIn => RefKind is RefKind.In or RefKind.RefReadOnlyParameter;

        internal override bool IsMetadataOut => RefKind == RefKind.Out;
    }
}
