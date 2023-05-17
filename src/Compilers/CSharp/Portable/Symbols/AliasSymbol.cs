// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    /// <summary>
    /// Symbol representing a using alias appearing in a compilation unit or within a namespace
    /// declaration. Generally speaking, these symbols do not appear in the set of symbols reachable
    /// from the unnamed namespace declaration.  In other words, when a using alias is used in a
    /// program, it acts as a transparent alias, and the symbol to which it is an alias is used in
    /// the symbol table.  For example, in the source code
    /// <pre>
    /// namespace NS
    /// {
    ///     using o = System.Object;
    ///     partial class C : o {}
    ///     partial class C : object {}
    ///     partial class C : System.Object {}
    /// }
    /// </pre>
    /// all three declarations for class C are equivalent and result in the same symbol table object
    /// for C. However, these using alias symbols do appear in the results of certain SemanticModel
    /// APIs. Specifically, for the base clause of the first of C's class declarations, the
    /// following APIs may produce a result that contains an AliasSymbol:
    /// <pre>
    ///     SemanticInfo SemanticModel.GetSemanticInfo(ExpressionSyntax expression);
    ///     SemanticInfo SemanticModel.BindExpression(CSharpSyntaxNode location, ExpressionSyntax expression);
    ///     SemanticInfo SemanticModel.BindType(CSharpSyntaxNode location, ExpressionSyntax type);
    ///     SemanticInfo SemanticModel.BindNamespaceOrType(CSharpSyntaxNode location, ExpressionSyntax type);
    /// </pre>
    /// Also, the following are affected if container==null (and, for the latter, when arity==null
    /// or arity==0):
    /// <pre>
    ///     IList&lt;string&gt; SemanticModel.LookupNames(CSharpSyntaxNode location, NamespaceOrTypeSymbol container = null, LookupOptions options = LookupOptions.Default, List&lt;string> result = null);
    ///     IList&lt;Symbol&gt; SemanticModel.LookupSymbols(CSharpSyntaxNode location, NamespaceOrTypeSymbol container = null, string name = null, int? arity = null, LookupOptions options = LookupOptions.Default, List&lt;Symbol> results = null);
    /// </pre>
    /// </summary>
    internal abstract class AliasSymbol : Symbol
    {
        private readonly ImmutableArray<Location> _locations;  // NOTE: can be empty for the "global" alias.
        private readonly string _aliasName;
        private readonly bool _isExtern;
        private readonly Symbol _containingSymbol;

        protected AliasSymbol(string aliasName, Symbol containingSymbol, ImmutableArray<Location> locations, bool isExtern)
        {
            Debug.Assert(locations.Length == 1 || (locations.IsEmpty && aliasName == "global")); // It looks like equality implementation depends on this condition.
            Debug.Assert(containingSymbol is not null);

            _locations = locations;
            _aliasName = aliasName;
            _isExtern = isExtern;
            _containingSymbol = containingSymbol;
        }

        // For the purposes of SemanticModel, it is convenient to have an AliasSymbol for the "global" namespace that "global::" binds
        // to. This alias symbol is returned only when binding "global::" (special case code).
        internal static AliasSymbol CreateGlobalNamespaceAlias(NamespaceSymbol globalNamespace)
        {
            return new AliasSymbolFromResolvedTarget(globalNamespace, "global", globalNamespace, ImmutableArray<Location>.Empty, isExtern: false);
        }

        internal static AliasSymbol CreateCustomDebugInfoAlias(NamespaceOrTypeSymbol targetSymbol, SyntaxToken aliasToken, Symbol containingSymbol, bool isExtern)
        {
            return new AliasSymbolFromResolvedTarget(targetSymbol, aliasToken.ValueText, containingSymbol, ImmutableArray.Create(aliasToken.GetLocation()), isExtern);
        }

        internal AliasSymbol ToNewSubmission(CSharpCompilation compilation)
        {
            // We can pass basesBeingResolved: null because base type cycles can't cross
            // submission boundaries - there's no way to depend on a subsequent submission.
            var previousTarget = Target;
            if (previousTarget.Kind != SymbolKind.Namespace)
            {
                return this;
            }

            var expandedGlobalNamespace = compilation.GlobalNamespace;
            var expandedNamespace = Imports.ExpandPreviousSubmissionNamespace((NamespaceSymbol)previousTarget, expandedGlobalNamespace);
            return new AliasSymbolFromResolvedTarget(expandedNamespace, Name, ContainingSymbol, _locations, _isExtern);
        }

        public sealed override string Name
        {
            get
            {
                return _aliasName;
            }
        }

        public override SymbolKind Kind
        {
            get
            {
                return SymbolKind.Alias;
            }
        }

        /// <summary>
        /// Gets the <see cref="NamespaceOrTypeSymbol"/> for the
        /// namespace or type referenced by the alias.
        /// </summary>
        public abstract NamespaceOrTypeSymbol Target
        {
            get;
        }

        public override ImmutableArray<Location> Locations
        {
            get
            {
                return _locations;
            }
        }

        public override ImmutableArray<SyntaxReference> DeclaringSyntaxReferences
        {
            get
            {
                return _isExtern
                    ? GetDeclaringSyntaxReferenceHelper<ExternAliasDirectiveSyntax>(_locations)
                    : GetDeclaringSyntaxReferenceHelper<UsingDirectiveSyntax>(_locations);
            }
        }

        public sealed override bool IsExtern
        {
            get
            {
                return _isExtern;
            }
        }

        public override bool IsSealed
        {
            get
            {
                return false;
            }
        }

        public override bool IsAbstract
        {
            get
            {
                return false;
            }
        }
        public override bool IsOverride
        {
            get
            {
                return false;
            }
        }

        public override bool IsVirtual
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

        /// <summary>
        /// Returns data decoded from Obsolete attribute or null if there is no Obsolete attribute.
        /// This property returns ObsoleteAttributeData.Uninitialized if attribute arguments haven't been decoded yet.
        /// </summary>
        internal sealed override ObsoleteAttributeData? ObsoleteAttributeData
        {
            get { return null; }
        }

        public override Accessibility DeclaredAccessibility
        {
            get
            {
                return Accessibility.NotApplicable;
            }
        }

        /// <summary>
        /// Using aliases in C# are always contained within a namespace declaration, or at the top
        /// level within a compilation unit, within the implicit unnamed namespace declaration.  We
        /// return that as the "containing" symbol, even though the alias isn't a member of the
        /// namespace as such.
        /// </summary>
        public sealed override Symbol ContainingSymbol
        {
            get
            {
                return _containingSymbol;
            }
        }

        internal override TResult Accept<TArg, TResult>(CSharpSymbolVisitor<TArg, TResult> visitor, TArg a)
        {
            return visitor.VisitAlias(this, a);
        }

        public override void Accept(CSharpSymbolVisitor visitor)
        {
            visitor.VisitAlias(this);
        }

        public override TResult Accept<TResult>(CSharpSymbolVisitor<TResult> visitor)
        {
            return visitor.VisitAlias(this);
        }

        // basesBeingResolved is only used to break circular references.
        internal abstract NamespaceOrTypeSymbol GetAliasTarget(ConsList<TypeSymbol>? basesBeingResolved);

        internal void CheckConstraints(BindingDiagnosticBag diagnostics)
        {
            var target = this.Target as TypeSymbol;
            if ((object?)target != null && Locations.Length > 0)
            {
                var corLibrary = this.ContainingAssembly.CorLibrary;
                var conversions = new TypeConversions(corLibrary);
                target.CheckAllConstraints(DeclaringCompilation, conversions, GetFirstLocation(), diagnostics);
            }
        }

        public override bool Equals(Symbol? obj, TypeCompareKind compareKind)
        {
            if (ReferenceEquals(this, obj))
            {
                return true;
            }

            if (ReferenceEquals(obj, null))
            {
                return false;
            }

            AliasSymbol? other = obj as AliasSymbol;

            return (object?)other != null &&
                Equals(this.TryGetFirstLocation(), other.TryGetFirstLocation()) &&
                Equals(this.ContainingSymbol, other.ContainingSymbol, compareKind);
        }

        public override int GetHashCode()
            => this.TryGetFirstLocation()?.GetHashCode() ?? Name.GetHashCode();

        internal abstract override bool RequiresCompletion
        {
            get;
        }

        protected override ISymbol CreateISymbol()
        {
            return new PublicModel.AliasSymbol(this);
        }
    }

    internal sealed class AliasSymbolFromSyntax : AliasSymbol
    {
        private readonly SyntaxReference _directive;
        private SymbolCompletionState _state;
        private NamespaceOrTypeSymbol? _aliasTarget;

        // lazy binding
        private BindingDiagnosticBag? _aliasTargetDiagnostics;

        internal AliasSymbolFromSyntax(SourceNamespaceSymbol containingSymbol, UsingDirectiveSyntax syntax)
            : base(syntax.Alias!.Name.Identifier.ValueText, containingSymbol, ImmutableArray.Create(syntax.Alias!.Name.Identifier.GetLocation()), isExtern: false)
        {
            Debug.Assert(syntax.Alias is object);

            _directive = syntax.GetReference();
        }

        internal AliasSymbolFromSyntax(SourceNamespaceSymbol containingSymbol, ExternAliasDirectiveSyntax syntax)
            : base(syntax.Identifier.ValueText, containingSymbol, ImmutableArray.Create(syntax.Identifier.GetLocation()), isExtern: true)
        {
            _directive = syntax.GetReference();
        }

        /// <summary>
        /// Gets the <see cref="NamespaceOrTypeSymbol"/> for the
        /// namespace or type referenced by the alias.
        /// </summary>
        public override NamespaceOrTypeSymbol Target
        {
            get
            {
                return GetAliasTarget(basesBeingResolved: null);
            }
        }

        // basesBeingResolved is only used to break circular references.
        internal override NamespaceOrTypeSymbol GetAliasTarget(ConsList<TypeSymbol>? basesBeingResolved)
        {
            if (!_state.HasComplete(CompletionPart.AliasTarget))
            {
                // the target is not yet bound. If it is an ordinary alias, bind the target
                // symbol. If it is an extern alias then find the target in the list of metadata references.
                var newDiagnostics = BindingDiagnosticBag.GetInstance();

                NamespaceOrTypeSymbol symbol = this.IsExtern
                    ? ResolveExternAliasTarget(newDiagnostics)
                    : ResolveAliasTarget((UsingDirectiveSyntax)_directive.GetSyntax(), newDiagnostics, basesBeingResolved);

                if (Interlocked.CompareExchange(ref _aliasTarget, symbol, null) is null)
                {
                    // Note: It's important that we don't call newDiagnosticsToReadOnlyAndFree here. That call
                    // can force the prompt evaluation of lazy initialized diagnostics.  That in turn can 
                    // call back into GetAliasTarget on the same thread resulting in a dead lock scenario.
                    bool won = Interlocked.Exchange(ref _aliasTargetDiagnostics, newDiagnostics) == null;
                    Debug.Assert(won, "Only one thread can win the alias target CompareExchange");

                    _state.NotePartComplete(CompletionPart.AliasTarget);
                    // we do not clear this.aliasTargetName, as another thread might be about to use it for ResolveAliasTarget(...)
                }
                else
                {
                    newDiagnostics.Free();
                    // Wait for diagnostics to have been reported if another thread resolves the alias
                    _state.SpinWaitComplete(CompletionPart.AliasTarget, default(CancellationToken));
                }
            }

            return _aliasTarget!;
        }

        internal BindingDiagnosticBag AliasTargetDiagnostics
        {
            get
            {
                GetAliasTarget(null);
                RoslynDebug.Assert(_aliasTargetDiagnostics != null);
                return _aliasTargetDiagnostics;
            }
        }

        private NamespaceSymbol ResolveExternAliasTarget(BindingDiagnosticBag diagnostics)
        {
            NamespaceSymbol? target;
            if (!ContainingSymbol.DeclaringCompilation.GetExternAliasTarget(Name, out target))
            {
                diagnostics.Add(ErrorCode.ERR_BadExternAlias, GetFirstLocation(), Name);
            }

            RoslynDebug.Assert(target is object);
            RoslynDebug.Assert(target.IsGlobalNamespace);

            return target;
        }

        private NamespaceOrTypeSymbol ResolveAliasTarget(
            UsingDirectiveSyntax usingDirective,
            BindingDiagnosticBag diagnostics,
            ConsList<TypeSymbol>? basesBeingResolved)
        {
            if (usingDirective.UnsafeKeyword != default)
            {
                MessageID.IDS_FeatureUsingTypeAlias.CheckFeatureAvailability(diagnostics, usingDirective.UnsafeKeyword);
            }
            else if (usingDirective.NamespaceOrType is not NameSyntax)
            {
                MessageID.IDS_FeatureUsingTypeAlias.CheckFeatureAvailability(diagnostics, usingDirective.NamespaceOrType);
            }

            var syntax = usingDirective.NamespaceOrType;
            var flags = BinderFlags.SuppressConstraintChecks | BinderFlags.SuppressObsoleteChecks;
            if (usingDirective.UnsafeKeyword != default)
            {
                this.CheckUnsafeModifier(DeclarationModifiers.Unsafe, usingDirective.UnsafeKeyword.GetLocation(), diagnostics);
                flags |= BinderFlags.UnsafeRegion;
            }
            else
            {
                // Prior to C#12, allow the alias to be an unsafe region.  This allows us to maintain compat with prior
                // versions of the compiler that allowed `using X = List<int*[]>` to be written.  In 12.0 and onwards
                // though, we require the code to explicitly contain the `unsafe` keyword.
                if (!DeclaringCompilation.IsFeatureEnabled(MessageID.IDS_FeatureUsingTypeAlias))
                    flags |= BinderFlags.UnsafeRegion;
            }

            var declarationBinder = ContainingSymbol.DeclaringCompilation
                .GetBinderFactory(syntax.SyntaxTree)
                .GetBinder(syntax)
                .WithAdditionalFlags(flags);

            var annotatedNamespaceOrType = declarationBinder.BindNamespaceOrTypeSymbol(syntax, diagnostics, basesBeingResolved);

            // `using X = RefType?;` is not legal.
            if (usingDirective.NamespaceOrType is NullableTypeSyntax nullableType &&
                annotatedNamespaceOrType.TypeWithAnnotations.NullableAnnotation == NullableAnnotation.Annotated &&
                annotatedNamespaceOrType.TypeWithAnnotations.Type?.IsReferenceType is true)
            {
                diagnostics.Add(ErrorCode.ERR_BadNullableReferenceTypeInUsingAlias, nullableType.QuestionToken.GetLocation());
            }

            var namespaceOrType = annotatedNamespaceOrType.NamespaceOrTypeSymbol;
            if (namespaceOrType is TypeSymbol { IsNativeIntegerWrapperType: true } &&
                (usingDirective.NamespaceOrType.IsNint || usingDirective.NamespaceOrType.IsNuint))
            {
                // using X = nint;
                MessageID.IDS_FeatureUsingTypeAlias.CheckFeatureAvailability(diagnostics, usingDirective.NamespaceOrType);
            }

            return namespaceOrType;
        }

        internal override bool RequiresCompletion
        {
            get { return true; }
        }
    }

    internal sealed class AliasSymbolFromResolvedTarget : AliasSymbol
    {
        private readonly NamespaceOrTypeSymbol _aliasTarget;

        internal AliasSymbolFromResolvedTarget(NamespaceOrTypeSymbol target, string aliasName, Symbol containingSymbol, ImmutableArray<Location> locations, bool isExtern)
            : base(aliasName, containingSymbol, locations, isExtern)
        {
            _aliasTarget = target;
        }

        /// <summary>
        /// Gets the <see cref="NamespaceOrTypeSymbol"/> for the
        /// namespace or type referenced by the alias.
        /// </summary>
        public override NamespaceOrTypeSymbol Target
        {
            get
            {
                return _aliasTarget;
            }
        }

        internal override NamespaceOrTypeSymbol GetAliasTarget(ConsList<TypeSymbol>? basesBeingResolved)
        {
            return _aliasTarget;
        }

        internal override bool RequiresCompletion
        {
            get { return false; }
        }
    }
}
