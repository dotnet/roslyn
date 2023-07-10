// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Emit;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Symbols.Metadata.PE;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Symbols;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp
{
    /// <summary>
    /// The base class for all symbols (namespaces, classes, method, parameters, etc.) that are 
    /// exposed by the compiler.
    /// </summary>
    [DebuggerDisplay("{GetDebuggerDisplay(), nq}")]
    internal abstract partial class Symbol : ISymbolInternal, IFormattable
    {
        private ISymbol _lazyISymbol;

        // !!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
        // Changes to the public interface of this class should remain synchronized with the VB version of Symbol.
        // Do not make any changes to the public interface without making the corresponding change
        // to the VB version.
        // !!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!

        /// <summary>
        /// True if this Symbol should be completed by calling ForceComplete.
        /// Intuitively, true for source entities (from any compilation).
        /// </summary>
        internal virtual bool RequiresCompletion
        {
            get { return false; }
        }

        internal virtual void ForceComplete(SourceLocation locationOpt, CancellationToken cancellationToken)
        {
            // must be overridden by source symbols, no-op for other symbols
            Debug.Assert(!this.RequiresCompletion);
        }

        internal virtual bool HasComplete(CompletionPart part)
        {
            // must be overridden by source symbols, no-op for other symbols
            Debug.Assert(!this.RequiresCompletion);
            return true;
        }

        /// <summary>
        /// Gets the name of this symbol. Symbols without a name return the empty string; null is
        /// never returned.
        /// </summary>
        public virtual string Name
        {
            get
            {
                return string.Empty;
            }
        }

        /// <summary>
        /// Gets the name of a symbol as it appears in metadata. Most of the time, this
        /// is the same as the Name property, with the following exceptions:
        /// 1) The metadata name of generic types includes the "`1", "`2" etc. suffix that
        /// indicates the number of type parameters (it does not include, however, names of
        /// containing types or namespaces).
        /// 2) The metadata name of explicit interface names have spaces removed, compared to
        /// the name property.
        /// </summary>
        public virtual string MetadataName
        {
            get
            {
                return this.Name;
            }
        }

        /// <summary>
        /// Gets the token for this symbol as it appears in metadata. Most of the time this is 0,
        /// as it is when the symbol is not loaded from metadata.
        /// </summary>
        public virtual int MetadataToken => 0;

        /// <summary>
        /// Gets the kind of this symbol.
        /// </summary>
        public abstract SymbolKind Kind { get; }

        /// <summary>
        /// Get the symbol that logically contains this symbol. 
        /// </summary>
        public abstract Symbol ContainingSymbol { get; }

        /// <summary>
        /// Returns the nearest lexically enclosing type, or null if there is none.
        /// </summary>
        public virtual NamedTypeSymbol ContainingType
        {
            get
            {
                Symbol container = this.ContainingSymbol;

                NamedTypeSymbol containerAsType = container as NamedTypeSymbol;

                // NOTE: container could be null, so we do not check 
                //       whether containerAsType is not null, but 
                //       instead check if it did not change after 
                //       the cast.
                if ((object)containerAsType == (object)container)
                {
                    // this should be relatively uncommon
                    // most symbols that may be contained in a type
                    // know their containing type and can override ContainingType
                    // with a more precise implementation
                    return containerAsType;
                }

                // this is recursive, but recursion should be very short 
                // before we reach symbol that definitely knows its containing type.
                return container.ContainingType;
            }
        }

        /// <summary>
        /// Gets the nearest enclosing namespace for this namespace or type. For a nested type,
        /// returns the namespace that contains its container.
        /// </summary>
        public virtual NamespaceSymbol ContainingNamespace
        {
            get
            {
                for (var container = this.ContainingSymbol; (object)container != null; container = container.ContainingSymbol)
                {
                    var ns = container as NamespaceSymbol;
                    if ((object)ns != null)
                    {
                        return ns;
                    }
                }

                return null;
            }
        }

        /// <summary>
        /// Returns the assembly containing this symbol. If this symbol is shared across multiple
        /// assemblies, or doesn't belong to an assembly, returns null.
        /// </summary>
        public virtual AssemblySymbol ContainingAssembly
        {
            get
            {
                // Default implementation gets the containers assembly.

                var container = this.ContainingSymbol;
                return (object)container != null ? container.ContainingAssembly : null;
            }
        }

        /// <summary>
        /// For a source assembly, the associated compilation.
        /// For any other assembly, null.
        /// For a source module, the DeclaringCompilation of the associated source assembly.
        /// For any other module, null.
        /// For any other symbol, the DeclaringCompilation of the associated module.
        /// </summary>
        /// <remarks>
        /// We're going through the containing module, rather than the containing assembly,
        /// because of /addmodule (symbols in such modules should return null).
        /// 
        /// Remarks, not "ContainingCompilation" because it isn't transitive.
        /// </remarks>
        internal virtual CSharpCompilation DeclaringCompilation
        {
            get
            {
                if (!this.IsDefinition)
                {
                    return OriginalDefinition.DeclaringCompilation;
                }

                switch (this.Kind)
                {
                    case SymbolKind.ErrorType:
                        return null;
                    case SymbolKind.Assembly:
                        Debug.Assert(!(this is SourceAssemblySymbol), "SourceAssemblySymbol must override DeclaringCompilation");
                        return null;
                    case SymbolKind.NetModule:
                        Debug.Assert(!(this is SourceModuleSymbol), "SourceModuleSymbol must override DeclaringCompilation");
                        return null;
                }

                switch (this.ContainingModule)
                {
                    case SourceModuleSymbol sourceModuleSymbol:
                        return sourceModuleSymbol.DeclaringCompilation;

                    case PEModuleSymbol:
                        // A special handling for EE.
                        return ContainingSymbol?.DeclaringCompilation;
                }

                return null;
            }
        }

        Compilation ISymbolInternal.DeclaringCompilation
            => DeclaringCompilation;

        string ISymbolInternal.Name => this.Name;

        string ISymbolInternal.MetadataName => this.MetadataName;

        ISymbolInternal ISymbolInternal.ContainingSymbol => this.ContainingSymbol;

        IModuleSymbolInternal ISymbolInternal.ContainingModule => this.ContainingModule;

        IAssemblySymbolInternal ISymbolInternal.ContainingAssembly => this.ContainingAssembly;

        ImmutableArray<Location> ISymbolInternal.Locations => this.Locations;

        INamespaceSymbolInternal ISymbolInternal.ContainingNamespace => this.ContainingNamespace;

        bool ISymbolInternal.IsImplicitlyDeclared => this.IsImplicitlyDeclared;

        INamedTypeSymbolInternal ISymbolInternal.ContainingType
        {
            get
            {
                return this.ContainingType;
            }
        }

        ISymbol ISymbolInternal.GetISymbol() => this.ISymbol;

        /// <summary>
        /// Returns the module containing this symbol. If this symbol is shared across multiple
        /// modules, or doesn't belong to a module, returns null.
        /// </summary>
        internal virtual ModuleSymbol ContainingModule
        {
            get
            {
                // Default implementation gets the containers module.

                var container = this.ContainingSymbol;
                return (object)container != null ? container.ContainingModule : null;
            }
        }

        /// <summary>
        /// The index of this member in the containing symbol. This is an optional
        /// property, implemented by anonymous type properties only, for comparing
        /// symbols in flow analysis.
        /// </summary>
        /// <remarks>
        /// Should this be used for tuple fields as well?
        /// </remarks>
        internal virtual int? MemberIndexOpt => null;

        /// <summary>
        /// The original definition of this symbol. If this symbol is constructed from another
        /// symbol by type substitution then OriginalDefinition gets the original symbol as it was defined in
        /// source or metadata.
        /// </summary>
        public Symbol OriginalDefinition
        {
            get
            {
                return OriginalSymbolDefinition;
            }
        }

        protected virtual Symbol OriginalSymbolDefinition
        {
            get
            {
                return this;
            }
        }

        /// <summary>
        /// Returns true if this is the original definition of this symbol.
        /// </summary>
        public bool IsDefinition
        {
            get
            {
                return (object)this == (object)OriginalDefinition;
            }
        }

        /// <summary>
        /// <para>
        /// Get a source location key for sorting. For performance, it's important that this
        /// be able to be returned from a symbol without doing any additional allocations (even
        /// if nothing is cached yet.)
        /// </para>
        /// <para>
        /// Only (original) source symbols and namespaces that can be merged
        /// need implement this function if they want to do so for efficiency.
        /// </para>
        /// </summary>
        internal virtual LexicalSortKey GetLexicalSortKey()
        {
            var firstLocation = this.TryGetFirstLocation();
            if (firstLocation is null)
                return LexicalSortKey.NotInSource;

            var declaringCompilation = this.DeclaringCompilation;
            Debug.Assert(declaringCompilation != null); // require that it is a source symbol
            return new LexicalSortKey(firstLocation, declaringCompilation);
        }

        /// <summary>
        /// Gets the locations where this symbol was originally defined, either in source or
        /// metadata. Some symbols (for example, partial classes) may be defined in more than one
        /// location.
        /// </summary>
        public abstract ImmutableArray<Location> Locations { get; }

#nullable enable

        public virtual Location? TryGetFirstLocation()
        {
            // Simple (but allocating) impl that can be overridden in subtypes if they show up in traces.
            var locations = this.Locations;
            return locations.IsEmpty ? null : locations[0];
        }

        public Location GetFirstLocation()
            => TryGetFirstLocation() ?? throw new InvalidOperationException("Symbol has no locations");

        public Location GetFirstLocationOrNone()
            => TryGetFirstLocation() ?? Location.None;

        /// <summary>
        /// Determines if there is a location (see <see cref="Locations"/>) for this symbol whose span is in <paramref
        /// name="tree"/> and is contained within <paramref name="declarationSpan"/>.  Subclasses can override this to
        /// be more efficient if desired (especially if avoiding allocations of the <see cref="Locations"/> array is
        /// desired).
        /// </summary>
        public virtual bool HasLocationContainedWithin(SyntaxTree tree, TextSpan declarationSpan, out bool wasZeroWidthMatch)
        {
            foreach (var loc in this.Locations)
            {
                if (IsLocationContainedWithin(loc, tree, declarationSpan, out wasZeroWidthMatch))
                    return true;
            }

            wasZeroWidthMatch = false;
            return false;
        }

        protected static bool IsLocationContainedWithin(Location loc, SyntaxTree tree, TextSpan declarationSpan, out bool wasZeroWidthMatch)
        {
            if (loc.IsInSource && loc.SourceTree == tree && declarationSpan.Contains(loc.SourceSpan))
            {
                wasZeroWidthMatch = loc.SourceSpan.IsEmpty && loc.SourceSpan.End == declarationSpan.Start;
                return true;
            }

            wasZeroWidthMatch = false;
            return false;
        }

#nullable disable

        /// <summary>
        /// <para>
        /// Get the syntax node(s) where this symbol was declared in source. Some symbols (for
        /// example, partial classes) may be defined in more than one location. This property should
        /// return one or more syntax nodes only if the symbol was declared in source code and also
        /// was not implicitly declared (see the <see cref="IsImplicitlyDeclared"/> property). 
        /// </para>
        /// <para>
        /// Note that for namespace symbol, the declaring syntax might be declaring a nested
        /// namespace. For example, the declaring syntax node for N1 in "namespace N1.N2 {...}" is
        /// the entire <see cref="BaseNamespaceDeclarationSyntax"/> for N1.N2. For the global namespace, the declaring
        /// syntax will be the <see cref="CompilationUnitSyntax"/>.
        /// </para>
        /// </summary>
        /// <returns>
        /// The syntax node(s) that declared the symbol. If the symbol was declared in metadata or
        /// was implicitly declared, returns an empty read-only array.
        /// </returns>
        /// <remarks>
        /// To go the opposite direction (from syntax node to symbol), see <see
        /// cref="CSharpSemanticModel.GetDeclaredSymbol(MemberDeclarationSyntax, CancellationToken)"/>.
        /// </remarks>
        public abstract ImmutableArray<SyntaxReference> DeclaringSyntaxReferences { get; }

        /// <summary>
        /// Helper for implementing <see cref="DeclaringSyntaxReferences"/> for derived classes that store a location but not a 
        /// <see cref="CSharpSyntaxNode"/> or <see cref="SyntaxReference"/>.
        /// </summary>
        internal static ImmutableArray<SyntaxReference> GetDeclaringSyntaxReferenceHelper<TNode>(ImmutableArray<Location> locations)
            where TNode : CSharpSyntaxNode
        {
            if (locations.IsEmpty)
            {
                return ImmutableArray<SyntaxReference>.Empty;
            }

            ArrayBuilder<SyntaxReference> builder = ArrayBuilder<SyntaxReference>.GetInstance();
            foreach (Location location in locations)
            {
                // Location may be null. See https://github.com/dotnet/roslyn/issues/28862.
                if (location == null || !location.IsInSource)
                {
                    continue;
                }

                if (location.SourceSpan.Length != 0)
                {
                    SyntaxToken token = location.SourceTree.GetRoot().FindToken(location.SourceSpan.Start);
                    if (token.Kind() != SyntaxKind.None)
                    {
                        CSharpSyntaxNode node = token.Parent.FirstAncestorOrSelf<TNode>();
                        if (node != null)
                        {
                            builder.Add(node.GetReference());
                        }
                    }
                }
                else
                {
                    // Since the location we're interested in can't contain a token, we'll inspect the whole tree,
                    // pruning away branches that don't contain that location. We'll pick the narrowest node of the type
                    // we're looking for.
                    // eg: finding the ParameterSyntax from the empty location of a blank identifier
                    SyntaxNode parent = location.SourceTree.GetRoot();
                    SyntaxNode found = null;
                    foreach (var descendant in parent.DescendantNodesAndSelf(c => c.Location.SourceSpan.Contains(location.SourceSpan)))
                    {
                        if (descendant is TNode && descendant.Location.SourceSpan.Contains(location.SourceSpan))
                        {
                            found = descendant;
                        }
                    }

                    if (found is object)
                    {
                        builder.Add(found.GetReference());
                    }
                }
            }

            return builder.ToImmutableAndFree();
        }

        /// <summary>
        /// Get this accessibility that was declared on this symbol. For symbols that do not have
        /// accessibility declared on them, returns <see cref="Accessibility.NotApplicable"/>.
        /// </summary>
        public abstract Accessibility DeclaredAccessibility { get; }

        /// <summary>
        /// Returns true if this symbol is "static"; i.e., declared with the <c>static</c> modifier or
        /// implicitly static.
        /// </summary>
        public abstract bool IsStatic { get; }

        /// <summary>
        /// Returns true if this symbol is "virtual", has an implementation, and does not override a
        /// base class member; i.e., declared with the <c>virtual</c> modifier. Does not return true for
        /// members declared as abstract or override.
        /// </summary>
        public abstract bool IsVirtual { get; }

        /// <summary>
        /// Returns true if this symbol was declared to override a base class member; i.e., declared
        /// with the <c>override</c> modifier. Still returns true if member was declared to override
        /// something, but (erroneously) no member to override exists.
        /// </summary>
        /// <remarks>
        /// Even for metadata symbols, <see cref="IsOverride"/> = true does not imply that <see cref="IMethodSymbol.OverriddenMethod"/> will
        /// be non-null.
        /// </remarks>
        public abstract bool IsOverride { get; }

        /// <summary>
        /// Returns true if this symbol was declared as requiring an override; i.e., declared with
        /// the <c>abstract</c> modifier. Also returns true on a type declared as "abstract", all
        /// interface types, and members of interface types.
        /// </summary>
        public abstract bool IsAbstract { get; }

        /// <summary>
        /// Returns true if this symbol was declared to override a base class member and was also
        /// sealed from further overriding; i.e., declared with the <c>sealed</c> modifier. Also set for
        /// types that do not allow a derived class (declared with <c>sealed</c> or <c>static</c> or <c>struct</c>
        /// or <c>enum</c> or <c>delegate</c>).
        /// </summary>
        public abstract bool IsSealed { get; }

        /// <summary>
        /// Returns true if this symbol has external implementation; i.e., declared with the 
        /// <c>extern</c> modifier. 
        /// </summary>
        public abstract bool IsExtern { get; }

        /// <summary>
        /// Returns true if this symbol was automatically created by the compiler, and does not
        /// have an explicit corresponding source code declaration.  
        /// 
        /// This is intended for symbols that are ordinary symbols in the language sense,
        /// and may be used by code, but that are simply declared implicitly rather than
        /// with explicit language syntax.
        /// 
        /// Examples include (this list is not exhaustive):
        ///   the default constructor for a class or struct that is created if one is not provided,
        ///   the BeginInvoke/Invoke/EndInvoke methods for a delegate,
        ///   the generated backing field for an auto property or a field-like event,
        ///   the "this" parameter for non-static methods,
        ///   the "value" parameter for a property setter,
        ///   the parameters on indexer accessor methods (not on the indexer itself),
        ///   methods in anonymous types,
        ///   anonymous functions
        /// </summary>
        public virtual bool IsImplicitlyDeclared
        {
            get { return false; }
        }

        /// <summary>
        /// Returns true if this symbol can be referenced by its name in code. Examples of symbols
        /// that cannot be referenced by name are:
        ///    constructors, destructors, operators, explicit interface implementations,
        ///    accessor methods for properties and events, array types.
        /// </summary>
        public bool CanBeReferencedByName
        {
            get
            {
                switch (this.Kind)
                {
                    case SymbolKind.Local:
                    case SymbolKind.Label:
                    case SymbolKind.Alias:
                    case SymbolKind.RangeVariable:
                        // never imported, and always references by name.
                        return true;

                    case SymbolKind.Namespace:
                    case SymbolKind.Field:
                    case SymbolKind.ErrorType:
                    case SymbolKind.Parameter:
                    case SymbolKind.TypeParameter:
                    case SymbolKind.Event:
                        break;

                    case SymbolKind.NamedType:
                        if (((NamedTypeSymbol)this).IsSubmissionClass)
                        {
                            return false;
                        }
                        break;

                    case SymbolKind.Property:
                        var property = (PropertySymbol)this;
                        if (property.IsIndexer || property.MustCallMethodsDirectly)
                        {
                            return false;
                        }
                        break;

                    case SymbolKind.Method:
                        var method = (MethodSymbol)this;
                        switch (method.MethodKind)
                        {
                            case MethodKind.Ordinary:
                            case MethodKind.LocalFunction:
                            case MethodKind.ReducedExtension:
                                break;
                            case MethodKind.Destructor:
                                // You wouldn't think that destructors would be referenceable by name, but
                                // dev11 only prevents them from being invoked - they can still be assigned
                                // to delegates.
                                return true;
                            case MethodKind.DelegateInvoke:
                                return true;
                            case MethodKind.PropertyGet:
                            case MethodKind.PropertySet:
                                if (!((PropertySymbol)method.AssociatedSymbol).CanCallMethodsDirectly())
                                {
                                    return false;
                                }
                                break;
                            default:
                                return false;
                        }
                        break;

                    case SymbolKind.ArrayType:
                    case SymbolKind.PointerType:
                    case SymbolKind.FunctionPointerType:
                    case SymbolKind.Assembly:
                    case SymbolKind.DynamicType:
                    case SymbolKind.NetModule:
                    case SymbolKind.Discard:
                        return false;

                    default:
                        throw ExceptionUtilities.UnexpectedValue(this.Kind);
                }

                // This will eliminate backing fields for auto-props, explicit interface implementations,
                // indexers, etc.
                // See the comment on ContainsDroppedIdentifierCharacters for an explanation of why
                // such names are not referenceable (or see DevDiv #14432).
                return SyntaxFacts.IsValidIdentifier(this.Name) &&
                    !SyntaxFacts.ContainsDroppedIdentifierCharacters(this.Name);
            }
        }

        /// <summary>
        /// As an optimization, viability checking in the lookup code should use this property instead
        /// of <see cref="CanBeReferencedByName"/>. The full name check will then be performed in the <see cref="CSharpSemanticModel"/>.
        /// </summary>
        /// <remarks>
        /// This property exists purely for performance reasons.
        /// </remarks>
        internal bool CanBeReferencedByNameIgnoringIllegalCharacters
        {
            get
            {
                if (this.Kind == SymbolKind.Method)
                {
                    var method = (MethodSymbol)this;
                    switch (method.MethodKind)
                    {
                        case MethodKind.Ordinary:
                        case MethodKind.LocalFunction:
                        case MethodKind.DelegateInvoke:
                        case MethodKind.Destructor: // See comment in CanBeReferencedByName.
                            return true;
                        case MethodKind.PropertyGet:
                        case MethodKind.PropertySet:
                            return ((PropertySymbol)method.AssociatedSymbol).CanCallMethodsDirectly();
                        default:
                            return false;
                    }
                }
                return true;
            }
        }

        /// <summary>
        /// Perform additional checks after the member has been
        /// added to the member list of the containing type.
        /// </summary>
        internal virtual void AfterAddingTypeMembersChecks(ConversionsBase conversions, BindingDiagnosticBag diagnostics)
        {
        }

        // Note: This is no public "IsNew". This is intentional, because new has no syntactic meaning.
        // It serves only to remove a warning. Furthermore, it can not be inferred from 
        // metadata. For symbols defined in source, the modifiers in the syntax tree
        // can be examined.

        /// <summary>
        /// Compare two symbol objects to see if they refer to the same symbol. You should always
        /// use <see cref="operator =="/> and <see cref="operator !="/>, or the <see cref="Equals(object)"/> method, to compare two symbols for equality.
        /// </summary>
        public static bool operator ==(Symbol left, Symbol right)
        {
            //PERF: this function is often called with
            //      1) left referencing same object as the right 
            //      2) right being null
            //      The code attempts to check for these conditions before 
            //      resorting to .Equals

            // the condition is expected to be folded when inlining "someSymbol == null"
            if (right is null)
            {
                return left is null;
            }

            // this part is expected to disappear when inlining "someSymbol == null"
            return (object)left == (object)right || right.Equals(left);
        }

        /// <summary>
        /// Compare two symbol objects to see if they refer to the same symbol. You should always
        /// use == and !=, or the Equals method, to compare two symbols for equality.
        /// </summary>
        public static bool operator !=(Symbol left, Symbol right)
        {
            //PERF: this function is often called with
            //      1) left referencing same object as the right 
            //      2) right being null
            //      The code attempts to check for these conditions before 
            //      resorting to .Equals
            //
            //NOTE: we do not implement this as !(left == right) 
            //      since that sometimes results in a worse code

            // the condition is expected to be folded when inlining "someSymbol != null"
            if (right is null)
            {
                return left is object;
            }

            // this part is expected to disappear when inlining "someSymbol != null"
            return (object)left != (object)right && !right.Equals(left);
        }

        public sealed override bool Equals(object obj)
        {
            return this.Equals(obj as Symbol, SymbolEqualityComparer.Default.CompareKind);
        }

        public bool Equals(Symbol other)
        {
            return this.Equals(other, SymbolEqualityComparer.Default.CompareKind);
        }

        bool ISymbolInternal.Equals(ISymbolInternal other, TypeCompareKind compareKind)
        {
            return this.Equals(other as Symbol, compareKind);
        }

        // By default we don't consider the compareKind, and do reference equality. This can be overridden.
        public virtual bool Equals(Symbol other, TypeCompareKind compareKind)
        {
            return (object)this == other;
        }

        // By default, we do reference equality. This can be overridden.
        public override int GetHashCode()
        {
            return System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(this);
        }

        public static bool Equals(Symbol first, Symbol second, TypeCompareKind compareKind)
        {
            if (first is null)
            {
                return second is null;
            }

            return first.Equals(second, compareKind);
        }

        /// <summary>
        /// Returns a string representation of this symbol, suitable for debugging purposes, or
        /// for placing in an error message.
        /// </summary>
        /// <remarks>
        /// This will provide a useful representation, but it would be clearer to call <see cref="ToDisplayString"/>
        /// directly and provide an explicit format.
        /// Sealed so that <see cref="ToString"/> and <see cref="ToDisplayString"/> can't get out of sync.
        /// </remarks>
        public sealed override string ToString()
        {
            return this.ToDisplayString();
        }

        // ---- End of Public Definition ---
        // Below here can be various useful virtual methods that are useful to the compiler, but we don't
        // want to expose publicly.
        // ---- End of Public Definition ---

        // Must override this in derived classes for visitor pattern.
        internal abstract TResult Accept<TArgument, TResult>(CSharpSymbolVisitor<TArgument, TResult> visitor, TArgument a);

        // Prevent anyone else from deriving from this class.
        internal Symbol()
        {
        }

        /// <summary>
        /// Build and add synthesized attributes for this symbol.
        /// </summary>
        internal virtual void AddSynthesizedAttributes(PEModuleBuilder moduleBuilder, ref ArrayBuilder<SynthesizedAttributeData> attributes)
        {
        }

        /// <summary>
        /// Convenience helper called by subclasses to add a synthesized attribute to a collection of attributes.
        /// </summary>
        internal static void AddSynthesizedAttribute(ref ArrayBuilder<SynthesizedAttributeData> attributes, SynthesizedAttributeData attribute)
        {
            if (attribute != null)
            {
                if (attributes == null)
                {
                    attributes = new ArrayBuilder<SynthesizedAttributeData>(1);
                }

                attributes.Add(attribute);
            }
        }

        /// <summary>
        /// <see cref="CharSet"/> effective for this symbol (type or DllImport method).
        /// Nothing if <see cref="DefaultCharSetAttribute"/> isn't applied on the containing module or it doesn't apply on this symbol.
        /// </summary>
        /// <remarks>
        /// Determined based upon value specified via <see cref="DefaultCharSetAttribute"/> applied on the containing module.
        /// </remarks>
        internal CharSet? GetEffectiveDefaultMarshallingCharSet()
        {
            Debug.Assert(this.Kind == SymbolKind.NamedType || this.Kind == SymbolKind.Method);
            return this.ContainingModule.DefaultMarshallingCharSet;
        }

        internal bool IsFromCompilation(CSharpCompilation compilation)
        {
            Debug.Assert(compilation != null);
            return compilation == this.DeclaringCompilation;
        }

        /// <summary>
        /// Always prefer <see cref="IsFromCompilation"/>.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Unfortunately, when determining overriding/hiding/implementation relationships, we don't 
        /// have the "current" compilation available.  We could, but that would clutter up the API 
        /// without providing much benefit.  As a compromise, we consider all compilations "current".
        /// </para>
        /// <para>
        /// Unlike in VB, we are not allowing retargeting symbols.  This method is used as an approximation
        /// for <see cref="IsFromCompilation"/> when a compilation is not available and that method will never return
        /// true for retargeting symbols.
        /// </para>
        /// </remarks>
        internal bool Dangerous_IsFromSomeCompilation
        {
            get { return this.DeclaringCompilation != null; }
        }

        public virtual bool IsDefinedInSourceTree(SyntaxTree tree, TextSpan? definedWithinSpan, CancellationToken cancellationToken = default(CancellationToken))
        {
            var declaringReferences = this.DeclaringSyntaxReferences;
            if (this.IsImplicitlyDeclared && declaringReferences.Length == 0)
            {
                return this.ContainingSymbol.IsDefinedInSourceTree(tree, definedWithinSpan, cancellationToken);
            }

            foreach (var syntaxRef in declaringReferences)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (IsDefinedInSourceTree(syntaxRef, tree, definedWithinSpan))
                {
                    return true;
                }
            }

            return false;
        }

        protected static bool IsDefinedInSourceTree(SyntaxReference syntaxRef, SyntaxTree tree, TextSpan? definedWithinSpan)
            => syntaxRef.SyntaxTree == tree &&
                (!definedWithinSpan.HasValue || syntaxRef.Span.IntersectsWith(definedWithinSpan.Value));

        internal static void ForceCompleteMemberByLocation(SourceLocation locationOpt, Symbol member, CancellationToken cancellationToken)
        {
            if (locationOpt == null || member.IsDefinedInSourceTree(locationOpt.SourceTree, locationOpt.SourceSpan, cancellationToken))
            {
                cancellationToken.ThrowIfCancellationRequested();
                member.ForceComplete(locationOpt, cancellationToken);
            }
        }

        /// <summary>
        /// Returns the Documentation Comment ID for the symbol, or null if the symbol doesn't
        /// support documentation comments.
        /// </summary>
        public virtual string GetDocumentationCommentId()
        {
            // NOTE: we're using a try-finally here because there's a test that specifically
            // triggers an exception here to confirm that some symbols don't have documentation
            // comment IDs.  We don't care about "leaks" in such cases, but we don't want spew
            // in the test output.
            var pool = PooledStringBuilder.GetInstance();
            try
            {
                StringBuilder builder = pool.Builder;
                DocumentationCommentIDVisitor.Instance.Visit(this, builder);
                return builder.Length == 0 ? null : builder.ToString();
            }
            finally
            {
                pool.Free();
            }
        }

#nullable enable 
        /// <summary>
        /// Fetches the documentation comment for this element with a cancellation token.
        /// </summary>
        /// <param name="preferredCulture">Optionally, retrieve the comments formatted for a particular culture. No impact on source documentation comments.</param>
        /// <param name="expandIncludes">Optionally, expand <![CDATA[<include>]]> elements. No impact on non-source documentation comments.</param>
        /// <param name="cancellationToken">Optionally, allow cancellation of documentation comment retrieval.</param>
        /// <returns>The XML that would be written to the documentation file for the symbol.</returns>
        public virtual string GetDocumentationCommentXml(
            CultureInfo? preferredCulture = null,
            bool expandIncludes = false,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            return "";
        }
#nullable disable

        private static readonly SymbolDisplayFormat s_debuggerDisplayFormat =
            SymbolDisplayFormat.TestFormat
                .AddMiscellaneousOptions(SymbolDisplayMiscellaneousOptions.IncludeNullableReferenceTypeModifier
                    | SymbolDisplayMiscellaneousOptions.IncludeNotNullableReferenceTypeModifier)
                .WithCompilerInternalOptions(SymbolDisplayCompilerInternalOptions.IncludeContainingFileForFileTypes);

        internal virtual string GetDebuggerDisplay()
        {
            return $"{this.Kind} {this.ToDisplayString(s_debuggerDisplayFormat)}";
        }

        internal virtual void AddDeclarationDiagnostics(BindingDiagnosticBag diagnostics)
        {
#if DEBUG
            if (ContainingSymbol is SourceMemberContainerTypeSymbol container)
            {
                container.AssertMemberExposure(this, forDiagnostics: true);
            }
#endif
            if (diagnostics.DiagnosticBag?.IsEmptyWithoutResolution == false || diagnostics.DependenciesBag?.Count > 0)
            {
                CSharpCompilation compilation = this.DeclaringCompilation;
                Debug.Assert(compilation != null);

                compilation.AddUsedAssemblies(diagnostics.DependenciesBag);

                if (diagnostics.DiagnosticBag?.IsEmptyWithoutResolution == false)
                {
                    compilation.DeclarationDiagnostics.AddRange(diagnostics.DiagnosticBag);
                }
            }
        }

        #region Use-Site Diagnostics

        /// <summary>
        /// True if the symbol has a use-site diagnostic with error severity.
        /// </summary>
        internal bool HasUseSiteError
        {
            get
            {
                var info = GetUseSiteInfo();
                return info.DiagnosticInfo?.Severity == DiagnosticSeverity.Error;
            }
        }

        /// <summary>
        /// Returns diagnostic info that should be reported at the use site of the symbol, or default if there is none.
        /// </summary>
        internal virtual UseSiteInfo<AssemblySymbol> GetUseSiteInfo()
        {
            return default;
        }

        protected AssemblySymbol PrimaryDependency
        {
            get
            {
                AssemblySymbol dependency = this.ContainingAssembly;
                if (dependency is object && dependency.CorLibrary == dependency)
                {
                    return null;
                }

                return dependency;
            }
        }

        /// <summary>
        /// Returns true if the error code is the highest priority while calculating use site error for this symbol. 
        /// Supposed to be ErrorCode, but it causes inconsistent accessibility error.
        /// </summary>
        protected virtual bool IsHighestPriorityUseSiteErrorCode(int code) => true;

        /// <summary>
        /// Indicates that this symbol uses metadata that cannot be supported by the language.
        /// 
        /// Examples include:
        ///    - Pointer types in VB
        ///    - ByRef return type
        ///    - Required custom modifiers
        ///    
        /// This is distinguished from, for example, references to metadata symbols defined in assemblies that weren't referenced.
        /// Symbols where this returns true can never be used successfully, and thus should never appear in any IDE feature.
        /// 
        /// This is set for metadata symbols, as follows:
        /// Type - if a type is unsupported (e.g., a pointer type, etc.)
        /// Method - parameter or return type is unsupported
        /// Field - type is unsupported
        /// Event - type is unsupported
        /// Property - type is unsupported
        /// Parameter - type is unsupported
        /// </summary>
        public virtual bool HasUnsupportedMetadata
        {
            get
            {
                return false;
            }
        }

        /// <summary>
        /// Merges given diagnostic to the existing result diagnostic.
        /// </summary>
        internal bool MergeUseSiteDiagnostics(ref DiagnosticInfo result, DiagnosticInfo info)
        {
            if (info == null)
            {
                return false;
            }

            if (info.Severity == DiagnosticSeverity.Error && IsHighestPriorityUseSiteErrorCode(info.Code))
            {
                // this error is final, no other error can override it:
                result = info;
                return true;
            }

            if (result == null || result.Severity == DiagnosticSeverity.Warning && info.Severity == DiagnosticSeverity.Error)
            {
                // there could be an error of higher-priority
                result = info;
                return false;
            }

            // we have a second low-pri error, continue looking for a higher priority one
            return false;
        }

        /// <summary>
        /// Merges given diagnostic and dependencies to the existing result.
        /// </summary>
        internal bool MergeUseSiteInfo(ref UseSiteInfo<AssemblySymbol> result, UseSiteInfo<AssemblySymbol> info)
        {
            DiagnosticInfo diagnosticInfo = result.DiagnosticInfo;

            bool retVal = MergeUseSiteDiagnostics(ref diagnosticInfo, info.DiagnosticInfo);

            if (diagnosticInfo?.Severity == DiagnosticSeverity.Error)
            {
                result = new UseSiteInfo<AssemblySymbol>(diagnosticInfo);
                return retVal;
            }

            var secondaryDependencies = result.SecondaryDependencies;
            var primaryDependency = result.PrimaryDependency;

            info.MergeDependencies(ref primaryDependency, ref secondaryDependencies);

            result = new UseSiteInfo<AssemblySymbol>(diagnosticInfo, primaryDependency, secondaryDependencies);
            Debug.Assert(!retVal);
            return retVal;
        }

        /// <summary>
        /// Reports specified use-site diagnostic to given diagnostic bag. 
        /// </summary>
        /// <remarks>
        /// This method should be the only method adding use-site diagnostics to a diagnostic bag. 
        /// It performs additional adjustments of the location for unification related diagnostics and 
        /// may be the place where to add more use-site location post-processing.
        /// </remarks>
        /// <returns>True if the diagnostic has error severity.</returns>
        internal static bool ReportUseSiteDiagnostic(DiagnosticInfo info, DiagnosticBag diagnostics, Location location)
        {
            // Unlike VB the C# Dev11 compiler reports only a single unification error/warning.
            // By dropping the location we effectively merge all unification use-site errors that have the same error code into a single error.
            // The error message clearly explains how to fix the problem and reporting the error for each location wouldn't add much value. 
            if (info.Code == (int)ErrorCode.WRN_UnifyReferenceBldRev ||
                info.Code == (int)ErrorCode.WRN_UnifyReferenceMajMin ||
                info.Code == (int)ErrorCode.ERR_AssemblyMatchBadVersion)
            {
                location = NoLocation.Singleton;
            }

            diagnostics.Add(info, location);
            return info.Severity == DiagnosticSeverity.Error;
        }

        internal static bool ReportUseSiteDiagnostic(DiagnosticInfo info, BindingDiagnosticBag diagnostics, Location location)
        {
            return diagnostics.ReportUseSiteDiagnostic(info, location);
        }

        /// <summary>
        /// Derive use-site info from a type symbol.
        /// </summary>
        internal bool DeriveUseSiteInfoFromType(ref UseSiteInfo<AssemblySymbol> result, TypeSymbol type)
        {
            UseSiteInfo<AssemblySymbol> info = type.GetUseSiteInfo();
            if (info.DiagnosticInfo?.Code == (int)ErrorCode.ERR_BogusType)
            {
                GetSymbolSpecificUnsupportedMetadataUseSiteErrorInfo(ref info);
            }

            return MergeUseSiteInfo(ref result, info);
        }

        private void GetSymbolSpecificUnsupportedMetadataUseSiteErrorInfo(ref UseSiteInfo<AssemblySymbol> info)
        {
            switch (this.Kind)
            {
                case SymbolKind.Field:
                case SymbolKind.Method:
                case SymbolKind.Property:
                case SymbolKind.Event:
                    info = info.AdjustDiagnosticInfo(new CSDiagnosticInfo(ErrorCode.ERR_BindToBogus, this));
                    break;
            }
        }

        private UseSiteInfo<AssemblySymbol> GetSymbolSpecificUnsupportedMetadataUseSiteErrorInfo()
        {
            var useSiteInfo = new UseSiteInfo<AssemblySymbol>(new CSDiagnosticInfo(ErrorCode.ERR_BogusType, string.Empty));
            GetSymbolSpecificUnsupportedMetadataUseSiteErrorInfo(ref useSiteInfo);
            return useSiteInfo;
        }

        internal bool DeriveUseSiteInfoFromType(ref UseSiteInfo<AssemblySymbol> result, TypeWithAnnotations type, AllowedRequiredModifierType allowedRequiredModifierType)
        {
            return DeriveUseSiteInfoFromType(ref result, type.Type) ||
                   DeriveUseSiteInfoFromCustomModifiers(ref result, type.CustomModifiers, allowedRequiredModifierType);
        }

        internal bool DeriveUseSiteInfoFromParameter(ref UseSiteInfo<AssemblySymbol> result, ParameterSymbol param)
        {
            return DeriveUseSiteInfoFromType(ref result, param.TypeWithAnnotations, AllowedRequiredModifierType.None) ||
                   DeriveUseSiteInfoFromCustomModifiers(ref result, param.RefCustomModifiers,
                                                              this is MethodSymbol method && method.MethodKind == MethodKind.FunctionPointerSignature ?
                                                                  AllowedRequiredModifierType.System_Runtime_InteropServices_InAttribute | AllowedRequiredModifierType.System_Runtime_CompilerServices_OutAttribute :
                                                                  AllowedRequiredModifierType.System_Runtime_InteropServices_InAttribute);
        }

        internal bool DeriveUseSiteInfoFromParameters(ref UseSiteInfo<AssemblySymbol> result, ImmutableArray<ParameterSymbol> parameters)
        {
            foreach (ParameterSymbol param in parameters)
            {
                if (DeriveUseSiteInfoFromParameter(ref result, param))
                {
                    return true;
                }
            }

            return false;
        }

        [Flags]
        internal enum AllowedRequiredModifierType
        {
            None = 0,
            System_Runtime_CompilerServices_Volatile = 1,
            System_Runtime_InteropServices_InAttribute = 1 << 1,
            System_Runtime_CompilerServices_IsExternalInit = 1 << 2,
            System_Runtime_CompilerServices_OutAttribute = 1 << 3,
        }

        internal bool DeriveUseSiteInfoFromCustomModifiers(ref UseSiteInfo<AssemblySymbol> result, ImmutableArray<CustomModifier> customModifiers, AllowedRequiredModifierType allowedRequiredModifierType)
        {
            AllowedRequiredModifierType requiredModifiersFound = AllowedRequiredModifierType.None;
            bool checkRequiredModifiers = true;

            foreach (CustomModifier modifier in customModifiers)
            {
                NamedTypeSymbol modifierType = ((CSharpCustomModifier)modifier).ModifierSymbol;

                if (checkRequiredModifiers && !modifier.IsOptional)
                {
                    AllowedRequiredModifierType current = AllowedRequiredModifierType.None;

                    if ((allowedRequiredModifierType & AllowedRequiredModifierType.System_Runtime_InteropServices_InAttribute) != 0 &&
                        modifierType.IsWellKnownTypeInAttribute())
                    {
                        current = AllowedRequiredModifierType.System_Runtime_InteropServices_InAttribute;
                    }
                    else if ((allowedRequiredModifierType & AllowedRequiredModifierType.System_Runtime_CompilerServices_Volatile) != 0 &&
                        modifierType.SpecialType == SpecialType.System_Runtime_CompilerServices_IsVolatile)
                    {
                        current = AllowedRequiredModifierType.System_Runtime_CompilerServices_Volatile;
                    }
                    else if ((allowedRequiredModifierType & AllowedRequiredModifierType.System_Runtime_CompilerServices_IsExternalInit) != 0 &&
                        modifierType.IsWellKnownTypeIsExternalInit())
                    {
                        current = AllowedRequiredModifierType.System_Runtime_CompilerServices_IsExternalInit;
                    }
                    else if ((allowedRequiredModifierType & AllowedRequiredModifierType.System_Runtime_CompilerServices_OutAttribute) != 0 &&
                        modifierType.IsWellKnownTypeOutAttribute())
                    {
                        current = AllowedRequiredModifierType.System_Runtime_CompilerServices_OutAttribute;
                    }

                    if (current == AllowedRequiredModifierType.None ||
                        (current != requiredModifiersFound && requiredModifiersFound != AllowedRequiredModifierType.None)) // At the moment we don't support applying different allowed modreqs to the same target.
                    {
                        if (MergeUseSiteInfo(ref result, GetSymbolSpecificUnsupportedMetadataUseSiteErrorInfo()))
                        {
                            return true;
                        }

                        checkRequiredModifiers = false;
                    }

                    requiredModifiersFound |= current;
                }

                // Unbound generic type is valid as a modifier, let's not report any use site diagnostics because of that.
                if (modifierType.IsUnboundGenericType)
                {
                    modifierType = modifierType.OriginalDefinition;
                }

                if (DeriveUseSiteInfoFromType(ref result, modifierType))
                {
                    return true;
                }
            }

            return false;
        }

        internal static bool GetUnificationUseSiteDiagnosticRecursive<T>(ref DiagnosticInfo result, ImmutableArray<T> types, Symbol owner, ref HashSet<TypeSymbol> checkedTypes) where T : TypeSymbol
        {
            foreach (var t in types)
            {
                if (t.GetUnificationUseSiteDiagnosticRecursive(ref result, owner, ref checkedTypes))
                {
                    return true;
                }
            }

            return false;
        }

        internal static bool GetUnificationUseSiteDiagnosticRecursive(ref DiagnosticInfo result, ImmutableArray<TypeWithAnnotations> types, Symbol owner, ref HashSet<TypeSymbol> checkedTypes)
        {
            foreach (var t in types)
            {
                if (t.GetUnificationUseSiteDiagnosticRecursive(ref result, owner, ref checkedTypes))
                {
                    return true;
                }
            }

            return false;
        }

        internal static bool GetUnificationUseSiteDiagnosticRecursive(ref DiagnosticInfo result, ImmutableArray<CustomModifier> modifiers, Symbol owner, ref HashSet<TypeSymbol> checkedTypes)
        {
            foreach (var modifier in modifiers)
            {
                if (((CSharpCustomModifier)modifier).ModifierSymbol.GetUnificationUseSiteDiagnosticRecursive(ref result, owner, ref checkedTypes))
                {
                    return true;
                }
            }

            return false;
        }

        internal static bool GetUnificationUseSiteDiagnosticRecursive(ref DiagnosticInfo result, ImmutableArray<ParameterSymbol> parameters, Symbol owner, ref HashSet<TypeSymbol> checkedTypes)
        {
            foreach (var parameter in parameters)
            {
                if (parameter.TypeWithAnnotations.GetUnificationUseSiteDiagnosticRecursive(ref result, owner, ref checkedTypes) ||
                    GetUnificationUseSiteDiagnosticRecursive(ref result, parameter.RefCustomModifiers, owner, ref checkedTypes))
                {
                    return true;
                }
            }

            return false;
        }

        internal static bool GetUnificationUseSiteDiagnosticRecursive(ref DiagnosticInfo result, ImmutableArray<TypeParameterSymbol> typeParameters, Symbol owner, ref HashSet<TypeSymbol> checkedTypes)
        {
            foreach (var typeParameter in typeParameters)
            {
                if (GetUnificationUseSiteDiagnosticRecursive(ref result, typeParameter.ConstraintTypesNoUseSiteDiagnostics, owner, ref checkedTypes))
                {
                    return true;
                }
            }

            return false;
        }

        #endregion

        /// <summary>
        /// True if this symbol has been marked with the <see cref="ObsoleteAttribute"/> attribute. 
        /// This property returns <see cref="ThreeState.Unknown"/> if the <see cref="ObsoleteAttribute"/> attribute hasn't been cracked yet.
        /// </summary>
        internal ThreeState ObsoleteState
        {
            get
            {
                switch (ObsoleteKind)
                {
                    case ObsoleteAttributeKind.None:
                    case ObsoleteAttributeKind.WindowsExperimental:
                    case ObsoleteAttributeKind.Experimental:
                        return ThreeState.False;
                    case ObsoleteAttributeKind.Uninitialized:
                        return ThreeState.Unknown;
                    default:
                        return ThreeState.True;
                }
            }
        }

        internal ObsoleteAttributeKind ObsoleteKind
        {
            get
            {
                var data = this.ObsoleteAttributeData;
                return (data == null) ? ObsoleteAttributeKind.None : data.Kind;
            }
        }

        /// <summary>
        /// Returns data decoded from <see cref="ObsoleteAttribute"/> attribute or null if there is no <see cref="ObsoleteAttribute"/> attribute.
        /// This property returns <see cref="Microsoft.CodeAnalysis.ObsoleteAttributeData.Uninitialized"/> if attribute arguments haven't been decoded yet.
        /// </summary>
        internal abstract ObsoleteAttributeData ObsoleteAttributeData { get; }

        /// <summary>
        /// Returns true and a <see cref="string"/> from the first <see cref="GuidAttribute"/> on the symbol, 
        /// the string might be null or an invalid guid representation. False, 
        /// if there is no <see cref="GuidAttribute"/> with string argument.
        /// </summary>
        internal bool GetGuidStringDefaultImplementation(out string guidString)
        {
            foreach (var attrData in this.GetAttributes())
            {
                if (attrData.IsTargetAttribute(this, AttributeDescription.GuidAttribute))
                {
                    if (attrData.TryGetGuidAttributeValue(out guidString))
                    {
                        return true;
                    }
                }
            }

            guidString = null;
            return false;
        }

        public string ToDisplayString(SymbolDisplayFormat format = null)
        {
            return SymbolDisplay.ToDisplayString(ISymbol, format);
        }

        public ImmutableArray<SymbolDisplayPart> ToDisplayParts(SymbolDisplayFormat format = null)
        {
            return SymbolDisplay.ToDisplayParts(ISymbol, format);
        }

        public string ToMinimalDisplayString(
            SemanticModel semanticModel,
            int position,
            SymbolDisplayFormat format = null)
        {
            return SymbolDisplay.ToMinimalDisplayString(ISymbol, semanticModel, position, format);
        }

        public ImmutableArray<SymbolDisplayPart> ToMinimalDisplayParts(
            SemanticModel semanticModel,
            int position,
            SymbolDisplayFormat format = null)
        {
            return SymbolDisplay.ToMinimalDisplayParts(ISymbol, semanticModel, position, format);
        }

        internal static void ReportErrorIfHasConstraints(
            SyntaxList<TypeParameterConstraintClauseSyntax> constraintClauses, DiagnosticBag diagnostics)
        {
            if (constraintClauses.Count > 0)
            {
                diagnostics.Add(
                    ErrorCode.ERR_ConstraintOnlyAllowedOnGenericDecl,
                    constraintClauses[0].WhereKeyword.GetLocation());
            }
        }

        internal static void CheckForBlockAndExpressionBody(
            CSharpSyntaxNode block,
            CSharpSyntaxNode expression,
            CSharpSyntaxNode syntax,
            BindingDiagnosticBag diagnostics)
        {
            if (block != null && expression != null)
            {
                diagnostics.Add(ErrorCode.ERR_BlockBodyAndExpressionBody, syntax.GetLocation());
            }
        }

        [Flags]
        internal enum ReservedAttributes
        {
            DynamicAttribute = 1 << 1,
            IsReadOnlyAttribute = 1 << 2,
            IsUnmanagedAttribute = 1 << 3,
            IsByRefLikeAttribute = 1 << 4,
            TupleElementNamesAttribute = 1 << 5,
            NullableAttribute = 1 << 6,
            NullableContextAttribute = 1 << 7,
            NullablePublicOnlyAttribute = 1 << 8,
            NativeIntegerAttribute = 1 << 9,
            CaseSensitiveExtensionAttribute = 1 << 10,
            RequiredMemberAttribute = 1 << 11,
            ScopedRefAttribute = 1 << 12,
            RefSafetyRulesAttribute = 1 << 13,
            RequiresLocationAttribute = 1 << 14,
        }

        internal bool ReportExplicitUseOfReservedAttributes(in DecodeWellKnownAttributeArguments<AttributeSyntax, CSharpAttributeData, AttributeLocation> arguments, ReservedAttributes reserved)
        {
            var attribute = arguments.Attribute;
            var diagnostics = (BindingDiagnosticBag)arguments.Diagnostics;

            if ((reserved & ReservedAttributes.DynamicAttribute) != 0 &&
                attribute.IsTargetAttribute(this, AttributeDescription.DynamicAttribute))
            {
                // DynamicAttribute should not be set explicitly.
                diagnostics.Add(ErrorCode.ERR_ExplicitDynamicAttr, arguments.AttributeSyntaxOpt.Location);
            }
            else if ((reserved & ReservedAttributes.IsReadOnlyAttribute) != 0 &&
                reportExplicitUseOfReservedAttribute(attribute, arguments, AttributeDescription.IsReadOnlyAttribute))
            {
            }
            else if ((reserved & ReservedAttributes.RequiresLocationAttribute) != 0 &&
                reportExplicitUseOfReservedAttribute(attribute, arguments, AttributeDescription.RequiresLocationAttribute))
            {
            }
            else if ((reserved & ReservedAttributes.IsUnmanagedAttribute) != 0 &&
                reportExplicitUseOfReservedAttribute(attribute, arguments, AttributeDescription.IsUnmanagedAttribute))
            {
            }
            else if ((reserved & ReservedAttributes.IsByRefLikeAttribute) != 0 &&
                reportExplicitUseOfReservedAttribute(attribute, arguments, AttributeDescription.IsByRefLikeAttribute))
            {
            }
            else if ((reserved & ReservedAttributes.TupleElementNamesAttribute) != 0 &&
                attribute.IsTargetAttribute(this, AttributeDescription.TupleElementNamesAttribute))
            {
                diagnostics.Add(ErrorCode.ERR_ExplicitTupleElementNamesAttribute, arguments.AttributeSyntaxOpt.Location);
            }
            else if ((reserved & ReservedAttributes.NullableAttribute) != 0 &&
                attribute.IsTargetAttribute(this, AttributeDescription.NullableAttribute))
            {
                // NullableAttribute should not be set explicitly.
                diagnostics.Add(ErrorCode.ERR_ExplicitNullableAttribute, arguments.AttributeSyntaxOpt.Location);
            }
            else if ((reserved & ReservedAttributes.NullableContextAttribute) != 0 &&
                reportExplicitUseOfReservedAttribute(attribute, arguments, AttributeDescription.NullableContextAttribute))
            {
            }
            else if ((reserved & ReservedAttributes.NullablePublicOnlyAttribute) != 0 &&
                reportExplicitUseOfReservedAttribute(attribute, arguments, AttributeDescription.NullablePublicOnlyAttribute))
            {
            }
            else if ((reserved & ReservedAttributes.NativeIntegerAttribute) != 0 &&
                reportExplicitUseOfReservedAttribute(attribute, arguments, AttributeDescription.NativeIntegerAttribute))
            {
            }
            else if ((reserved & ReservedAttributes.CaseSensitiveExtensionAttribute) != 0 &&
                attribute.IsTargetAttribute(this, AttributeDescription.CaseSensitiveExtensionAttribute))
            {
                // ExtensionAttribute should not be set explicitly.
                diagnostics.Add(ErrorCode.ERR_ExplicitExtension, arguments.AttributeSyntaxOpt.Location);
            }
            else if ((reserved & ReservedAttributes.RequiredMemberAttribute) != 0 &&
                attribute.IsTargetAttribute(this, AttributeDescription.RequiredMemberAttribute))
            {
                // Do not use 'System.Runtime.CompilerServices.RequiredMemberAttribute'. Use the 'required' keyword on required fields and properties instead.
                diagnostics.Add(ErrorCode.ERR_ExplicitRequiredMember, arguments.AttributeSyntaxOpt.Location);
            }
            else if ((reserved & ReservedAttributes.ScopedRefAttribute) != 0 &&
                attribute.IsTargetAttribute(this, AttributeDescription.ScopedRefAttribute))
            {
                // Do not use 'System.Runtime.CompilerServices.ScopedRefAttribute'. Use the 'scoped' keyword instead.
                diagnostics.Add(ErrorCode.ERR_ExplicitScopedRef, arguments.AttributeSyntaxOpt.Location);
            }
            else if ((reserved & ReservedAttributes.RefSafetyRulesAttribute) != 0 &&
                reportExplicitUseOfReservedAttribute(attribute, arguments, AttributeDescription.RefSafetyRulesAttribute))
            {
            }
            else
            {
                return false;
            }
            return true;

            bool reportExplicitUseOfReservedAttribute(CSharpAttributeData attribute, in DecodeWellKnownAttributeArguments<AttributeSyntax, CSharpAttributeData, AttributeLocation> arguments, in AttributeDescription attributeDescription)
            {
                if (attribute.IsTargetAttribute(this, attributeDescription))
                {
                    // Do not use '{FullName}'. This is reserved for compiler usage.
                    diagnostics.Add(ErrorCode.ERR_ExplicitReservedAttr, arguments.AttributeSyntaxOpt.Location, attributeDescription.FullName);
                    return true;
                }
                return false;
            }
        }

        internal virtual byte? GetNullableContextValue()
        {
            return GetLocalNullableContextValue() ?? ContainingSymbol?.GetNullableContextValue();
        }

        internal virtual byte? GetLocalNullableContextValue()
        {
            return null;
        }

        internal void GetCommonNullableValues(CSharpCompilation compilation, ref MostCommonNullableValueBuilder builder)
        {
            switch (this.Kind)
            {
                case SymbolKind.NamedType:
                    if (compilation.ShouldEmitNullableAttributes(this))
                    {
                        builder.AddValue(this.GetLocalNullableContextValue());
                    }
                    break;
                case SymbolKind.Event:
                    if (compilation.ShouldEmitNullableAttributes(this))
                    {
                        builder.AddValue(((EventSymbol)this).TypeWithAnnotations);
                    }
                    break;
                case SymbolKind.Field:
                    var field = (FieldSymbol)this;
                    if (field is TupleElementFieldSymbol tupleElement)
                    {
                        field = tupleElement.TupleUnderlyingField;
                    }

                    if (compilation.ShouldEmitNullableAttributes(field))
                    {
                        builder.AddValue(field.TypeWithAnnotations);
                    }
                    break;
                case SymbolKind.Method:
                    if (compilation.ShouldEmitNullableAttributes(this))
                    {
                        builder.AddValue(this.GetLocalNullableContextValue());
                    }
                    break;
                case SymbolKind.Property:
                    if (compilation.ShouldEmitNullableAttributes(this))
                    {
                        builder.AddValue(((PropertySymbol)this).TypeWithAnnotations);
                        // Attributes are not emitted for property parameters.
                    }
                    break;
                case SymbolKind.Parameter:
                    builder.AddValue(((ParameterSymbol)this).TypeWithAnnotations);
                    break;
                case SymbolKind.TypeParameter:
                    if (this is SourceTypeParameterSymbolBase typeParameter)
                    {
                        builder.AddValue(typeParameter.GetSynthesizedNullableAttributeValue());
                        foreach (var constraintType in typeParameter.ConstraintTypesNoUseSiteDiagnostics)
                        {
                            builder.AddValue(constraintType);
                        }
                    }
                    break;
            }
        }

        internal bool ShouldEmitNullableContextValue(out byte value)
        {
            byte? localValue = GetLocalNullableContextValue();
            if (localValue == null)
            {
                value = 0;
                return false;
            }

            value = localValue.GetValueOrDefault();
            byte containingValue = ContainingSymbol?.GetNullableContextValue() ?? 0;
            return value != containingValue;
        }

#nullable enable
        /// <summary>
        /// True if the symbol is declared outside of the scope of the containing
        /// symbol
        /// </summary>
        internal static bool IsCaptured(Symbol variable, SourceMethodSymbol containingSymbol)
        {
            switch (variable.Kind)
            {
                case SymbolKind.Field:
                case SymbolKind.Property:
                case SymbolKind.Event:
                // Range variables are not captured, but their underlying parameters
                // may be. If this is a range underlying parameter it will be a
                // ParameterSymbol, not a RangeVariableSymbol.
                case SymbolKind.RangeVariable:
                    return false;

                case SymbolKind.Local:
                    if (((LocalSymbol)variable).IsConst)
                    {
                        return false;
                    }
                    break;

                case SymbolKind.Parameter:
                    break;

                case SymbolKind.Method:
                    if (variable is LocalFunctionSymbol localFunction)
                    {
                        // calling a static local function doesn't require capturing state
                        if (localFunction.IsStatic)
                        {
                            return false;
                        }

                        break;
                    }

                    throw ExceptionUtilities.UnexpectedValue(variable);

                default:
                    throw ExceptionUtilities.UnexpectedValue(variable.Kind);
            }

            // Walk up the containing symbols until we find the target function, in which
            // case the variable is not captured by the target function, or null, in which
            // case it is.
            for (var currentFunction = variable.ContainingSymbol;
                 (object)currentFunction != null;
                 currentFunction = currentFunction.ContainingSymbol)
            {
                if (ReferenceEquals(currentFunction, containingSymbol))
                {
                    return false;
                }
            }

            return true;
        }
#nullable disable

        bool ISymbolInternal.IsStatic
        {
            get { return this.IsStatic; }
        }

        bool ISymbolInternal.IsVirtual
        {
            get { return this.IsVirtual; }
        }

        bool ISymbolInternal.IsOverride
        {
            get { return this.IsOverride; }
        }

        bool ISymbolInternal.IsAbstract
        {
            get
            {
                return this.IsAbstract;
            }
        }

        Accessibility ISymbolInternal.DeclaredAccessibility
        {
            get
            {
                return this.DeclaredAccessibility;
            }
        }

        public abstract void Accept(CSharpSymbolVisitor visitor);

        public abstract TResult Accept<TResult>(CSharpSymbolVisitor<TResult> visitor);

        string IFormattable.ToString(string format, IFormatProvider formatProvider)
        {
            return ToString();
        }

        protected abstract ISymbol CreateISymbol();

        internal ISymbol ISymbol
        {
            get
            {
                if (_lazyISymbol is null)
                {
                    Interlocked.CompareExchange(ref _lazyISymbol, CreateISymbol(), null);
                }

                return _lazyISymbol;
            }
        }
    }
}
