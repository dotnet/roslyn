// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#nullable enable

using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis.Symbols
{
    /// <summary>
    /// Interface implemented by the compiler's internal representation of a symbol.
    /// An object implementing this interface might also implement <see cref="ISymbol"/> (as is done in VB),
    /// or the compiler's symbols might be wrapped to implement ISymbol (as is done in C#).
    /// </summary>
    internal interface ISymbolInternal
    {
        /// <summary>
        /// Gets the <see cref="SymbolKind"/> indicating what kind of symbol it is.
        /// </summary>
        SymbolKind Kind { get; }

        /// <summary>
        /// Gets the symbol name. Returns the empty string if unnamed.
        /// </summary>
        string Name { get; }

        string MetadataName { get; }

#nullable disable // Skipped for now https://github.com/dotnet/roslyn/issues/39166
        Compilation DeclaringCompilation { get; }
#nullable enable

        /// <summary>
        /// Allows a symbol to support comparisons that involve child type symbols
        /// </summary>
        /// <remarks>
        /// Because TypeSymbol equality can differ based on e.g. nullability, any symbols that contain TypeSymbols can also differ in the same way
        /// This call allows the symbol to accept a comparison kind that should be used when comparing its contained types
        /// </remarks>
        bool Equals(ISymbolInternal? other, TypeCompareKind compareKind);

        /// <summary>
        /// Gets the <see cref="ISymbolInternal"/> for the immediately containing symbol.
        /// </summary>
        ISymbolInternal ContainingSymbol { get; }

        /// <summary>
        /// Gets the <see cref="IAssemblySymbolInternal"/> for the containing assembly. Returns null if the
        /// symbol is shared across multiple assemblies.
        /// </summary>
        IAssemblySymbolInternal ContainingAssembly { get; }

        /// <summary>
        /// Gets the <see cref="IModuleSymbolInternal"/> for the containing module. Returns null if the
        /// symbol is shared across multiple modules.
        /// </summary>
        IModuleSymbolInternal ContainingModule { get; }

        /// <summary>
        /// Gets the <see cref="INamedTypeSymbolInternal"/> for the containing type. Returns null if the
        /// symbol is not contained within a type.
        /// </summary>
        INamedTypeSymbolInternal ContainingType { get; }

        /// <summary>
        /// Gets the <see cref="INamespaceSymbolInternal"/> for the nearest enclosing namespace. Returns null if the
        /// symbol isn't contained in a namespace.
        /// </summary>
        INamespaceSymbolInternal ContainingNamespace { get; }

        /// <summary>
        /// Gets a value indicating whether the symbol is the original definition. Returns false
        /// if the symbol is derived from another symbol, by type substitution for instance.
        /// </summary>
        bool IsDefinition { get; }

        /// <summary>
        /// Gets the locations where the symbol was originally defined, either in source or
        /// metadata. Some symbols (for example, partial classes) may be defined in more than one
        /// location.
        /// </summary>
        ImmutableArray<Location> Locations { get; }

        /// <summary>
        /// Returns true if this symbol was automatically created by the compiler, and does not have
        /// an explicit corresponding source code declaration. 
        /// </summary> 
        /// <remarks>
        /// This is intended for symbols that are ordinary symbols in the language sense, and may be
        /// used by code, but that are simply declared implicitly rather than with explicit language
        /// syntax.
        /// 
        /// <para>
        /// Examples include (this list is not exhaustive):
        /// <list type="bullet">
        /// <item><description>The default constructor for a class or struct that is created if one is not provided.</description></item>
        /// <item><description>The BeginInvoke/Invoke/EndInvoke methods for a delegate.</description></item>
        /// <item><description>The generated backing field for an auto property or a field-like event.</description></item>
        /// <item><description>The "this" parameter for non-static methods.</description></item>
        /// <item><description>The "value" parameter for a property setter.</description></item>
        /// <item><description>The parameters on indexer accessor methods (not on the indexer itself).</description></item>
        /// <item><description>Methods in anonymous types.</description></item>
        /// </list>
        /// </para>
        /// </remarks>
        bool IsImplicitlyDeclared { get; }

        /// <summary>
        /// Gets a <see cref="Accessibility"/> indicating the declared accessibility for the symbol.
        /// Returns NotApplicable if no accessibility is declared.
        /// </summary>
        Accessibility DeclaredAccessibility { get; }

        /// <summary>
        /// Gets a value indicating whether the symbol is static.
        /// </summary>
        bool IsStatic { get; }

        /// <summary>
        /// Gets a value indicating whether the symbol is virtual.
        /// </summary>
        bool IsVirtual { get; }

        /// <summary>
        /// Gets a value indicating whether the symbol is an override of a base class symbol.
        /// </summary>
        bool IsOverride { get; }

        /// <summary>
        /// Gets a value indicating whether the symbol is abstract.
        /// </summary>
        bool IsAbstract { get; }

        /// <summary>
        /// Returns an <see cref="ISymbol"/> instance associated with this symbol.
        /// </summary>
        ISymbol GetISymbol();
    }
}
