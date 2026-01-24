// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Symbols;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    /// <summary>
    /// Represents a namespace.
    /// </summary>
    internal abstract partial class NamespaceSymbol : NamespaceOrTypeSymbol, INamespaceSymbolInternal
    {
        // PERF: initialization of the following fields will allocate, so we make them lazy
        private ImmutableArray<NamedTypeSymbol> _lazyTypesMightContainExtensionMethods;
        private string _lazyQualifiedName;

        // !!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
        // Changes to the public interface of this class should remain synchronized with the VB version.
        // Do not make any changes to the public interface without making the corresponding change
        // to the VB version.
        // !!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!

        /// <summary>
        /// Get all the members of this symbol that are namespaces.
        /// </summary>
        /// <returns>An IEnumerable containing all the namespaces that are members of this symbol.
        /// If this symbol has no namespace members, returns an empty IEnumerable. Never returns
        /// null.</returns>
        public IEnumerable<NamespaceSymbol> GetNamespaceMembers()
        {
            return this.GetMembers().OfType<NamespaceSymbol>();
        }

        /// <summary>
        /// Returns whether this namespace is the unnamed, global namespace that is 
        /// at the root of all namespaces.
        /// </summary>
        public virtual bool IsGlobalNamespace
        {
            get
            {
                return (object)ContainingNamespace == null;
            }
        }

        internal abstract NamespaceExtent Extent { get; }

        /// <summary>
        /// The kind of namespace: Module, Assembly or Compilation.
        /// Module namespaces contain only members from the containing module that share the same namespace name.
        /// Assembly namespaces contain members for all modules in the containing assembly that share the same namespace name.
        /// Compilation namespaces contain all members, from source or referenced metadata (assemblies and modules) that share the same namespace name.
        /// </summary>
        public NamespaceKind NamespaceKind
        {
            get { return this.Extent.Kind; }
        }

        /// <summary>
        /// The containing compilation for compilation namespaces.
        /// </summary>
        public CSharpCompilation ContainingCompilation
        {
            get { return this.NamespaceKind == NamespaceKind.Compilation ? this.Extent.Compilation : null; }
        }

        /// <summary>
        /// If a namespace has Assembly or Compilation extent, it may be composed of multiple
        /// namespaces that are merged together. If so, ConstituentNamespaces returns
        /// all the namespaces that were merged. If this namespace was not merged, returns
        /// an array containing only this namespace.
        /// </summary>
        public virtual ImmutableArray<NamespaceSymbol> ConstituentNamespaces
        {
            get
            {
                return ImmutableArray.Create(this);
            }
        }

        public sealed override NamedTypeSymbol ContainingType
        {
            get
            {
                return null;
            }
        }

        /// <summary>
        /// Containing assembly.
        /// </summary>
        public abstract override AssemblySymbol ContainingAssembly { get; }

        internal override ModuleSymbol ContainingModule
        {
            get
            {
                var extent = this.Extent;
                if (extent.Kind == NamespaceKind.Module)
                {
                    return extent.Module;
                }

                return null;
            }
        }

        /// <summary>
        /// Gets the kind of this symbol.
        /// </summary>
        public sealed override SymbolKind Kind
        {
            get
            {
                return SymbolKind.Namespace;
            }
        }

        public sealed override bool IsImplicitlyDeclared
        {
            get
            {
                return this.IsGlobalNamespace;
            }
        }

        /// <summary>
        /// Implements visitor pattern.
        /// </summary>
        internal override TResult Accept<TArgument, TResult>(CSharpSymbolVisitor<TArgument, TResult> visitor, TArgument argument)
        {
            return visitor.VisitNamespace(this, argument);
        }

        public override void Accept(CSharpSymbolVisitor visitor)
        {
            visitor.VisitNamespace(this);
        }

        public override TResult Accept<TResult>(CSharpSymbolVisitor<TResult> visitor)
        {
            return visitor.VisitNamespace(this);
        }

        // Only the compiler can create namespace symbols.
        internal NamespaceSymbol()
        {
        }

        /// <summary>
        /// Get this accessibility that was declared on this symbol. For symbols that do not have
        /// accessibility declared on them, returns NotApplicable.
        /// </summary>
        public sealed override Accessibility DeclaredAccessibility
        {
            // C# spec 3.5.1: Namespaces implicitly have public declared accessibility.
            get
            {
                return Accessibility.Public;
            }
        }

        /// <summary>
        /// Returns true if this symbol is "static"; i.e., declared with the "static" modifier or
        /// implicitly static.
        /// </summary>
        public sealed override bool IsStatic
        {
            get
            {
                return true;
            }
        }

        /// <summary>
        /// Returns true if this symbol was declared as requiring an override; i.e., declared with
        /// the "abstract" modifier. Also returns true on a type declared as "abstract", all
        /// interface types, and members of interface types.
        /// </summary>
        public sealed override bool IsAbstract
        {
            get
            {
                return false;
            }
        }

        /// <summary>
        /// Returns true if this symbol was declared to override a base class member and was also
        /// sealed from further overriding; i.e., declared with the "sealed" modifier.  Also set for
        /// types that do not allow a derived class (declared with "sealed" or "static" or "struct"
        /// or "enum" or "delegate").
        /// </summary>
        public sealed override bool IsSealed
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

        /// <summary>
        /// Returns an implicit type symbol for this namespace or null if there is none. This type
        /// wraps misplaced global code.
        /// </summary>
        internal NamedTypeSymbol ImplicitType
        {
            get
            {
                var types = this.GetTypeMembers(TypeSymbol.ImplicitTypeName);
                if (types.Length == 0)
                {
                    return null;
                }

                Debug.Assert(types.Length == 1);
                return types[0];
            }
        }

#nullable enable

        /// <summary>
        /// Lookup a nested namespace.
        /// </summary>
        /// <param name="names">
        /// Sequence of names for nested child namespaces.
        /// </param>
        /// <returns>
        /// Symbol for the most nested namespace, if found. Nothing 
        /// if namespace or any part of it can not be found.
        /// </returns>
        internal NamespaceSymbol? LookupNestedNamespace(ImmutableArray<ReadOnlyMemory<char>> names)
        {
            NamespaceSymbol? scope = this;
            foreach (ReadOnlyMemory<char> name in names)
            {
                scope = scope.GetNestedNamespace(name);
                if (scope is null)
                    return null;
            }

            return scope;
        }

        internal NamespaceSymbol? GetNestedNamespace(string name)
            => GetNestedNamespace(name.AsMemory());

        internal virtual NamespaceSymbol? GetNestedNamespace(ReadOnlyMemory<char> name)
        {
            foreach (var sym in this.GetMembers(name))
            {
                if (sym.Kind == SymbolKind.Namespace)
                {
                    return (NamespaceSymbol)sym;
                }
            }

            return null;
        }

#nullable disable

        public abstract ImmutableArray<Symbol> GetMembers(ReadOnlyMemory<char> name);

        public sealed override ImmutableArray<Symbol> GetMembers(string name)
            => GetMembers(name.AsMemory());

        internal NamespaceSymbol GetNestedNamespace(NameSyntax name)
        {
            switch (name.Kind())
            {
                case SyntaxKind.GenericName: // DeclarationTreeBuilder.VisitNamespace uses the PlainName, even for generic names
                case SyntaxKind.IdentifierName:
                    return this.GetNestedNamespace(((SimpleNameSyntax)name).Identifier.ValueText);

                case SyntaxKind.QualifiedName:
                    var qn = (QualifiedNameSyntax)name;
                    var leftNs = this.GetNestedNamespace(qn.Left);
                    if ((object)leftNs != null)
                    {
                        return leftNs.GetNestedNamespace(qn.Right);
                    }

                    break;

                case SyntaxKind.AliasQualifiedName:
                    // This is an error scenario, but we should still handle it.
                    // We recover in the same way as DeclarationTreeBuilder.VisitNamespaceDeclaration.
                    return this.GetNestedNamespace(name.GetUnqualifiedName().Identifier.ValueText);
            }

            return null;
        }

        private ImmutableArray<NamedTypeSymbol> TypesMightContainExtensionMethods
        {
            get
            {
                var typesWithExtensionMethods = this._lazyTypesMightContainExtensionMethods;
                if (typesWithExtensionMethods.IsDefault)
                {
                    this._lazyTypesMightContainExtensionMethods = this.GetTypeMembersUnordered().WhereAsArray(t => t.MightContainExtensionMethods);
                    typesWithExtensionMethods = this._lazyTypesMightContainExtensionMethods;
                }

                return typesWithExtensionMethods;
            }
        }

        /// <summary>
        /// Add all extension methods in this namespace to the given list. If name or arity
        /// or both are provided, only those extension methods that match are included.
        /// </summary>
        /// <param name="methods">Methods list</param>
        /// <param name="nameOpt">Optional method name</param>
        /// <param name="arity">Method arity</param>
        /// <param name="options">Lookup options</param>
        /// <remarks>Does not perform a full viability check</remarks>
        internal virtual void GetExtensionMethods(ArrayBuilder<MethodSymbol> methods, string nameOpt, int arity, LookupOptions options)
        {
            var assembly = this.ContainingAssembly;

            // Only MergedNamespaceSymbol should have a null ContainingAssembly
            // and MergedNamespaceSymbol overrides GetExtensionMethods.
            Debug.Assert((object)assembly != null);

            if (!assembly.MightContainExtensionMethods)
            {
                return;
            }

            var typesWithExtensionMethods = this.TypesMightContainExtensionMethods;

            foreach (var type in typesWithExtensionMethods)
            {
                type.DoGetExtensionMethods(methods, nameOpt, arity, options);
            }
        }

#nullable enable
        /// <remarks>Does not perform a full viability check</remarks>
        internal virtual void GetExtensionMembers(ArrayBuilder<Symbol> members, string? name, string? alternativeName, int arity, LookupOptions options, ConsList<FieldSymbol> fieldsBeingBound)
        {
            var assembly = this.ContainingAssembly;

            // Only MergedNamespaceSymbol should have a null ContainingAssembly
            // and MergedNamespaceSymbol overrides GetExtensionMembers.
            Debug.Assert((object)assembly != null);

            if (!assembly.MightContainExtensionMethods)
            {
                return;
            }

            var typesWithExtensionMethods = this.TypesMightContainExtensionMethods;

            foreach (var type in typesWithExtensionMethods)
            {
                type.GetExtensionMembers(members, name, alternativeName, arity, options, fieldsBeingBound);
            }
        }
#nullable disable

        internal string QualifiedName
        {
            get
            {
                return _lazyQualifiedName ??
                    (_lazyQualifiedName = this.ToDisplayString(SymbolDisplayFormat.QualifiedNameOnlyFormat));
            }
        }

        protected sealed override ISymbol CreateISymbol()
        {
            return new PublicModel.NamespaceSymbol(this);
        }

        bool INamespaceSymbolInternal.IsGlobalNamespace => this.IsGlobalNamespace;
    }
}
