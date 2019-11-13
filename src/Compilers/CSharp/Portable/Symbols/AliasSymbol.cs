// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Diagnostics;
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
    internal sealed class AliasSymbol : Symbol
    {
        private readonly SyntaxToken _aliasName;
        private readonly Binder _binder;

        private SymbolCompletionState _state;
        private NamespaceOrTypeSymbol _aliasTarget;
        private readonly ImmutableArray<Location> _locations;  // NOTE: can be empty for the "global" alias.

        // lazy binding
        private readonly NameSyntax _aliasTargetName;
        private readonly bool _isExtern;
        private DiagnosticBag _aliasTargetDiagnostics;

        private AliasSymbol(Binder binder, NamespaceOrTypeSymbol target, SyntaxToken aliasName, ImmutableArray<Location> locations)
        {
            _aliasName = aliasName;
            _locations = locations;
            _aliasTarget = target;
            _binder = binder;
            _state.NotePartComplete(CompletionPart.AliasTarget);
        }

        private AliasSymbol(Binder binder, SyntaxToken aliasName)
        {
            _aliasName = aliasName;
            _locations = ImmutableArray.Create(aliasName.GetLocation());
            _binder = binder;
        }

        internal AliasSymbol(Binder binder, UsingDirectiveSyntax syntax)
            : this(binder, syntax.Alias.Name.Identifier)
        {
            _aliasTargetName = syntax.Name;
        }

        internal AliasSymbol(Binder binder, ExternAliasDirectiveSyntax syntax)
            : this(binder, syntax.Identifier)
        {
            _isExtern = true;
        }

        // For the purposes of SemanticModel, it is convenient to have an AliasSymbol for the "global" namespace that "global::" binds
        // to. This alias symbol is returned only when binding "global::" (special case code).
        internal static AliasSymbol CreateGlobalNamespaceAlias(NamespaceSymbol globalNamespace, Binder globalNamespaceBinder)
        {
            SyntaxToken aliasName = SyntaxFactory.Identifier(SyntaxFactory.TriviaList(), SyntaxKind.GlobalKeyword, "global", "global", SyntaxFactory.TriviaList());
            return new AliasSymbol(globalNamespaceBinder, globalNamespace, aliasName, ImmutableArray<Location>.Empty);
        }

        internal static AliasSymbol CreateCustomDebugInfoAlias(NamespaceOrTypeSymbol targetSymbol, SyntaxToken aliasToken, Binder binder)
        {
            return new AliasSymbol(binder, targetSymbol, aliasToken, ImmutableArray.Create(aliasToken.GetLocation()));
        }

        internal AliasSymbol ToNewSubmission(CSharpCompilation compilation)
        {
            Debug.Assert(_binder.Compilation.IsSubmission);

            // We can pass basesBeingResolved: null because base type cycles can't cross
            // submission boundaries - there's no way to depend on a subsequent submission.
            var previousTarget = GetAliasTarget(basesBeingResolved: null);
            if (previousTarget.Kind != SymbolKind.Namespace)
            {
                return this;
            }

            var expandedGlobalNamespace = compilation.GlobalNamespace;
            var expandedNamespace = Imports.ExpandPreviousSubmissionNamespace((NamespaceSymbol)previousTarget, expandedGlobalNamespace);
            var binder = new InContainerBinder(expandedGlobalNamespace, new BuckStopsHereBinder(compilation));
            return new AliasSymbol(binder, expandedNamespace, _aliasName, _locations);
        }

        public override string Name
        {
            get
            {
                return _aliasName.ValueText;
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
        public NamespaceOrTypeSymbol Target
        {
            get
            {
                return GetAliasTarget(basesBeingResolved: null);
            }
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
                return GetDeclaringSyntaxReferenceHelper<UsingDirectiveSyntax>(_locations);
            }
        }

        public override bool IsExtern
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
        internal sealed override ObsoleteAttributeData ObsoleteAttributeData
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
        public override Symbol ContainingSymbol
        {
            get
            {
                return _binder.ContainingMemberOrLambda;
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
        internal NamespaceOrTypeSymbol GetAliasTarget(ConsList<TypeSymbol> basesBeingResolved)
        {
            if (!_state.HasComplete(CompletionPart.AliasTarget))
            {
                // the target is not yet bound. If it is an ordinary alias, bind the target
                // symbol. If it is an extern alias then find the target in the list of metadata references.
                var newDiagnostics = DiagnosticBag.GetInstance();

                NamespaceOrTypeSymbol symbol = this.IsExtern ?
                    ResolveExternAliasTarget(newDiagnostics) :
                    ResolveAliasTarget(_aliasTargetName, newDiagnostics, basesBeingResolved);

                if ((object)Interlocked.CompareExchange(ref _aliasTarget, symbol, null) == null)
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

            return _aliasTarget;
        }

        internal DiagnosticBag AliasTargetDiagnostics
        {
            get
            {
                GetAliasTarget(null);
                Debug.Assert(_aliasTargetDiagnostics != null);
                return _aliasTargetDiagnostics;
            }
        }

        internal void CheckConstraints(DiagnosticBag diagnostics)
        {
            var target = this.Target as TypeSymbol;
            if ((object)target != null && _locations.Length > 0)
            {
                var corLibrary = this.ContainingAssembly.CorLibrary;
                var conversions = new TypeConversions(corLibrary);
                target.CheckAllConstraints(DeclaringCompilation, conversions, _locations[0], diagnostics);
            }
        }

        private NamespaceSymbol ResolveExternAliasTarget(DiagnosticBag diagnostics)
        {
            NamespaceSymbol target;
            if (!_binder.Compilation.GetExternAliasTarget(_aliasName.ValueText, out target))
            {
                diagnostics.Add(ErrorCode.ERR_BadExternAlias, _aliasName.GetLocation(), _aliasName.ValueText);
            }
            else if (!_binder.IsSemanticModelBinder &&
                     !Compilation.ReportUnusedImportsInTree(_aliasName.SyntaxTree))
            {
                _binder.Compilation.AddAssembliesUsedByNamespaceReference(target);
            }

            Debug.Assert((object)target != null);
            Debug.Assert(target.IsGlobalNamespace);

            return target;
        }

        private NamespaceOrTypeSymbol ResolveAliasTarget(NameSyntax syntax, DiagnosticBag diagnostics, ConsList<TypeSymbol> basesBeingResolved)
        {
            var declarationBinder = _binder.WithAdditionalFlags(BinderFlags.SuppressConstraintChecks | BinderFlags.SuppressObsoleteChecks | BinderFlags.InUsing);
            NamespaceOrTypeSymbol result = declarationBinder.BindNamespaceOrTypeSymbol(syntax, diagnostics, basesBeingResolved).NamespaceOrTypeSymbol;

            if (!declarationBinder.IsSemanticModelBinder &&
                !Compilation.ReportUnusedImportsInTree(syntax.SyntaxTree) &&
                result is NamespaceSymbol ns)
            {
                declarationBinder.Compilation.AddAssembliesUsedByNamespaceReference(ns);
            }

            return result;
        }

        public override bool Equals(Symbol obj, TypeCompareKind compareKind)
        {
            if (ReferenceEquals(this, obj))
            {
                return true;
            }

            if (ReferenceEquals(obj, null))
            {
                return false;
            }

            AliasSymbol other = obj as AliasSymbol;

            return (object)other != null &&
                Equals(this.Locations.FirstOrDefault(), other.Locations.FirstOrDefault()) &&
                this.ContainingAssembly.Equals(other.ContainingAssembly, compareKind);
        }

        public override int GetHashCode()
        {
            if (this.Locations.Length > 0)
                return this.Locations.First().GetHashCode();
            else
                return Name.GetHashCode();
        }

        internal override bool RequiresCompletion
        {
            get { return true; }
        }

        protected override ISymbol CreateISymbol()
        {
            return new PublicModel.AliasSymbol(this);
        }
    }
}
