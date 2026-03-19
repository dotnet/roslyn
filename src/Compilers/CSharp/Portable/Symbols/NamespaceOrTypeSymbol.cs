// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using Microsoft.CodeAnalysis.Collections;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Symbols;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    /// <summary>
    /// Represents either a namespace or a type.
    /// </summary>
    internal abstract class NamespaceOrTypeSymbol : Symbol, INamespaceOrTypeSymbolInternal
    {
        protected static readonly ObjectPool<PooledDictionary<ReadOnlyMemory<char>, object>> s_nameToObjectPool =
            PooledDictionary<ReadOnlyMemory<char>, object>.CreatePool(ReadOnlyMemoryOfCharComparer.Instance);

        // !!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
        // Changes to the public interface of this class should remain synchronized with the VB version.
        // Do not make any changes to the public interface without making the corresponding change
        // to the VB version.
        // !!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!

        // Only the compiler can create new instances.
        internal NamespaceOrTypeSymbol()
        {
        }

        /// <summary>
        /// Returns true if this symbol is a namespace. If it is not a namespace, it must be a type.
        /// </summary>
        public bool IsNamespace
        {
            get
            {
                return Kind == SymbolKind.Namespace;
            }
        }

        /// <summary>
        /// Returns true if this symbols is a type. Equivalent to !IsNamespace.
        /// </summary>
        public bool IsType
        {
            get
            {
                return !IsNamespace;
            }
        }

        /// <summary>
        /// Returns true if this symbol is "virtual", has an implementation, and does not override a
        /// base class member; i.e., declared with the "virtual" modifier. Does not return true for
        /// members declared as abstract or override.
        /// </summary>
        /// <returns>
        /// Always returns false.
        /// </returns>
        public sealed override bool IsVirtual
        {
            get
            {
                return false;
            }
        }

        /// <summary>
        /// Returns true if this symbol was declared to override a base class member; i.e., declared
        /// with the "override" modifier. Still returns true if member was declared to override
        /// something, but (erroneously) no member to override exists.
        /// </summary>
        /// <returns>
        /// Always returns false.
        /// </returns>
        public sealed override bool IsOverride
        {
            get
            {
                return false;
            }
        }

        /// <summary>
        /// Returns true if this symbol has external implementation; i.e., declared with the 
        /// "extern" modifier. 
        /// </summary>
        /// <returns>
        /// Always returns false.
        /// </returns>
        public sealed override bool IsExtern
        {
            get
            {
                return false;
            }
        }

        /// <summary>
        /// Get all the members of this symbol.
        /// </summary>
        /// <returns>An ImmutableArray containing all the members of this symbol. If this symbol has no members,
        /// returns an empty ImmutableArray. Never returns null.</returns>
        public abstract ImmutableArray<Symbol> GetMembers();

        /// <summary>
        /// Get all the members of this symbol. The members may not be in a particular order, and the order
        /// may not be stable from call-to-call.
        /// </summary>
        /// <returns>An ImmutableArray containing all the members of this symbol. If this symbol has no members,
        /// returns an empty ImmutableArray. Never returns null.</returns>
        internal virtual ImmutableArray<Symbol> GetMembersUnordered()
        {
            // Default implementation is to use ordered version. When performance indicates, we specialize to have
            // separate implementation.

            return GetMembers().ConditionallyDeOrder();
        }

        /// <summary>
        /// Get all the members of this symbol that have a particular name.
        /// </summary>
        /// <returns>An ImmutableArray containing all the members of this symbol with the given name. If there are
        /// no members with this name, returns an empty ImmutableArray. Never returns null.</returns>
        public abstract ImmutableArray<Symbol> GetMembers(string name);

        /// <summary>
        /// Get all the members of this symbol that are types. The members may not be in a particular order, and the order
        /// may not be stable from call-to-call.
        /// </summary>
        /// <returns>An ImmutableArray containing all the types that are members of this symbol. If this symbol has no type members,
        /// returns an empty ImmutableArray. Never returns null.</returns>
        internal virtual ImmutableArray<NamedTypeSymbol> GetTypeMembersUnordered()
        {
            // Default implementation is to use ordered version. When performance indicates, we specialize to have
            // separate implementation.

            return GetTypeMembers().ConditionallyDeOrder();
        }

        /// <summary>
        /// Get all the members of this symbol that are types.
        /// </summary>
        /// <returns>An ImmutableArray containing all the types that are members of this symbol. If this symbol has no type members,
        /// returns an empty ImmutableArray. Never returns null.</returns>
        public abstract ImmutableArray<NamedTypeSymbol> GetTypeMembers();

        /// <summary>
        /// Get all the members of this symbol that are types that have a particular name, of any arity.
        /// </summary>
        /// <returns>An ImmutableArray containing all the types that are members of this symbol with the given name.
        /// If this symbol has no type members with this name,
        /// returns an empty ImmutableArray. Never returns null.</returns>
        public ImmutableArray<NamedTypeSymbol> GetTypeMembers(string name)
            => GetTypeMembers(name.AsMemory());

        /// <summary>
        /// Get all the members of this symbol that are types that have a particular name and arity
        /// </summary>
        /// <returns>An IEnumerable containing all the types that are members of this symbol with the given name and arity.
        /// If this symbol has no type members with this name and arity,
        /// returns an empty IEnumerable. Never returns null.</returns>
        public ImmutableArray<NamedTypeSymbol> GetTypeMembers(string name, int arity)
            => GetTypeMembers(name.AsMemory(), arity);

        /// <inheritdoc cref="GetTypeMembers(string)"/>
        public abstract ImmutableArray<NamedTypeSymbol> GetTypeMembers(ReadOnlyMemory<char> name);

        /// <inheritdoc cref="GetTypeMembers(string, int)"/>
        public virtual ImmutableArray<NamedTypeSymbol> GetTypeMembers(ReadOnlyMemory<char> name, int arity)
        {
            // default implementation does a post-filter. We can override this if its a performance burden, but 
            // experience is that it won't be.
            return GetTypeMembers(name).WhereAsArray(static (t, arity) => t.Arity == arity, arity);
        }

        /// <summary>
        /// Get a source type symbol for the given declaration syntax.
        /// </summary>
        /// <returns>Null if there is no matching declaration.</returns>
        internal SourceNamedTypeSymbol? GetSourceTypeMember(TypeDeclarationSyntax syntax)
        {
            return GetSourceTypeMember(syntax.Identifier.ValueText, syntax.Arity, syntax.Kind(), syntax);
        }

        /// <summary>
        /// Get a source type symbol for the given declaration syntax.
        /// </summary>
        /// <returns>Null if there is no matching declaration.</returns>
        internal SourceNamedTypeSymbol? GetSourceTypeMember(DelegateDeclarationSyntax syntax)
        {
            return GetSourceTypeMember(syntax.Identifier.ValueText, syntax.Arity, syntax.Kind(), syntax);
        }

        /// <summary>
        /// Get a source type symbol of given name, arity and kind.  If a tree and syntax are provided, restrict the results
        /// to those that are declared within the given syntax.
        /// </summary>
        /// <returns>Null if there is no matching declaration.</returns>
        internal SourceNamedTypeSymbol? GetSourceTypeMember(
            string name,
            int arity,
            SyntaxKind kind,
            CSharpSyntaxNode syntax)
        {
            TypeKind typeKind = kind.ToDeclarationKind().ToTypeKind();

            foreach (var member in GetTypeMembers(name, arity))
            {
                var memberT = member as SourceNamedTypeSymbol;
                if ((object?)memberT != null && memberT.TypeKind == typeKind)
                {
                    if (syntax != null)
                    {
                        // PERF: Avoid accessing Locations for performance, but assert that the alternative approach is
                        // equivalent.
                        Debug.Assert(memberT.MergedDeclaration.Declarations.SelectAsArray(decl => decl.NameLocation).SequenceEqual(memberT.Locations));
                        foreach (var declaration in memberT.MergedDeclaration.Declarations)
                        {
                            var loc = declaration.NameLocation;
                            if (loc.IsInSource && loc.SourceTree == syntax.SyntaxTree && syntax.Span.Contains(loc.SourceSpan))
                            {
                                return memberT;
                            }
                        }
                    }
                    else
                    {
                        return memberT;
                    }
                }
            }

            // None found.
            return null;
        }

        /// <summary>
        /// Lookup an immediately nested type referenced from metadata, names should be
        /// compared case-sensitively.
        /// </summary>
        /// <param name="emittedTypeName">
        /// Simple type name, possibly with generic name mangling.
        /// </param>
        /// <returns>
        /// Symbol for the type, or null if the type isn't found.
        /// </returns>
        internal virtual NamedTypeSymbol? LookupMetadataType(ref MetadataTypeName emittedTypeName)
        {
            Debug.Assert(!emittedTypeName.IsNull);

            NamespaceOrTypeSymbol scope = this;
            Debug.Assert(scope is not MergedNamespaceSymbol);
            Debug.Assert(scope is NamespaceSymbol or NamedTypeSymbol);

            if (scope.Kind == SymbolKind.ErrorType)
            {
                return null;
            }

            NamedTypeSymbol? namedType = null;

            ImmutableArray<NamedTypeSymbol> namespaceOrTypeMembers;
            bool isTopLevel = scope.IsNamespace;

            Debug.Assert(!isTopLevel || scope.ToDisplayString(SymbolDisplayFormat.QualifiedNameOnlyFormat) == emittedTypeName.NamespaceName);

            if (emittedTypeName.IsMangled)
            {
                Debug.Assert(!emittedTypeName.UnmangledTypeName.Equals(emittedTypeName.TypeName) && emittedTypeName.InferredArity > 0);

                if (emittedTypeName.ForcedArity == -1 || emittedTypeName.ForcedArity == emittedTypeName.InferredArity)
                {
                    // Let's handle mangling case first.
                    namespaceOrTypeMembers = scope.GetTypeMembers(emittedTypeName.UnmangledTypeNameMemory);

                    foreach (var named in namespaceOrTypeMembers)
                    {
                        if (emittedTypeName.InferredArity == named.Arity &&
                            named.MangleName &&
                            ReadOnlyMemoryOfCharComparer.Equals(named.MetadataName.AsSpan(), emittedTypeName.TypeNameMemory))
                        {
                            if (namedType is not null)
                            {
                                namedType = null;
                                break;
                            }

                            namedType = named;
                        }
                    }
                }
            }
            else
            {
                Debug.Assert(ReferenceEquals(emittedTypeName.UnmangledTypeName, emittedTypeName.TypeName) && emittedTypeName.InferredArity == 0);
            }

            // Now try lookup without removing generic arity mangling.
            int forcedArity = emittedTypeName.ForcedArity;

            if (emittedTypeName.UseCLSCompliantNameArityEncoding)
            {
                // Only types with arity 0 are acceptable, we already examined types with mangled names.
                if (emittedTypeName.InferredArity > 0)
                {
                    goto Done;
                }
                else if (forcedArity == -1)
                {
                    forcedArity = 0;
                }
                else if (forcedArity != 0)
                {
                    goto Done;
                }
                else
                {
                    Debug.Assert(forcedArity == emittedTypeName.InferredArity);
                }
            }

            namespaceOrTypeMembers = scope.GetTypeMembers(emittedTypeName.TypeNameMemory);

            foreach (var named in namespaceOrTypeMembers)
            {
                if (!named.MangleName &&
                    (forcedArity == -1 || forcedArity == named.Arity) &&
                    ReadOnlyMemoryOfCharComparer.Equals(named.MetadataName.AsSpan(), emittedTypeName.TypeNameMemory))
                {
                    if (namedType is not null)
                    {
                        namedType = null;
                        break;
                    }

                    namedType = named;
                }
            }

Done:
            if (isTopLevel
                && (emittedTypeName.ForcedArity == -1 || emittedTypeName.ForcedArity == emittedTypeName.InferredArity)
                // Quick check so we don't have to realize the expensive `UnmangledTypeName` in the common case.
                && emittedTypeName.TypeNameMemory.Span is [GeneratedNameParser.FileTypeNameStartChar, ..]
                && GeneratedNameParser.TryParseFileTypeName(
                    emittedTypeName.UnmangledTypeName,
                    out string? displayFileName,
                    out byte[]? checksum,
                    out string? sourceName))
            {
                // also do a lookup for file types from source.
                namespaceOrTypeMembers = scope.GetTypeMembers(sourceName);
                foreach (var named in namespaceOrTypeMembers)
                {
                    if (named.AssociatedFileIdentifier is FileIdentifier identifier
                        && identifier.DisplayFilePath == displayFileName
                        && !identifier.FilePathChecksumOpt.IsDefault
                        && identifier.FilePathChecksumOpt.SequenceEqual(checksum)
                        && named.Arity == emittedTypeName.InferredArity)
                    {
                        if ((object?)namedType != null)
                        {
                            namedType = null;
                            break;
                        }

                        namedType = named;
                    }
                }
            }

            return namedType;
        }

        /// <summary>
        /// Finds types or namespaces described by a qualified name.
        /// </summary>
        /// <param name="qualifiedName">Sequence of simple plain names.</param>
        /// <returns>
        /// A set of namespace or type symbols with given qualified name (might comprise of types with multiple generic arities), 
        /// or an empty set if the member can't be found (the qualified name is ambiguous or the symbol doesn't exist).
        /// </returns>
        /// <remarks>
        /// "C.D" matches C.D, C{T}.D, C{S,T}.D{U}, etc.
        /// </remarks>
        internal IEnumerable<NamespaceOrTypeSymbol>? GetNamespaceOrTypeByQualifiedName(IEnumerable<string> qualifiedName)
        {
            NamespaceOrTypeSymbol namespaceOrType = this;
            IEnumerable<NamespaceOrTypeSymbol>? symbols = null;
            foreach (string name in qualifiedName)
            {
                if (symbols != null)
                {
                    // there might be multiple types of different arity, prefer a non-generic type:
                    namespaceOrType = symbols.OfMinimalArity();
                    if ((object)namespaceOrType == null)
                    {
                        return SpecializedCollections.EmptyEnumerable<NamespaceOrTypeSymbol>();
                    }
                }

                symbols = namespaceOrType.GetMembers(name).OfType<NamespaceOrTypeSymbol>();
            }

            return symbols;
        }
    }
}
