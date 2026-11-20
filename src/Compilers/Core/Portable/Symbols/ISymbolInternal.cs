// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Reflection.Metadata;
using System.Threading;
using Microsoft.CodeAnalysis.Text;

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

        /// <summary>
        /// Gets the name of a symbol as it appears in metadata.
        /// </summary>
        string MetadataName { get; }

        /// <summary>
        /// Gets the metadata token associated with this symbol, or 0 if the symbol is not loaded from metadata.
        /// </summary>
        int MetadataToken { get; }

        /// <summary>
        /// Visibility of the member as emitted to the metadata.
        /// </summary>
        Cci.TypeMemberVisibility MetadataVisibility { get; }

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
        /// Returns <see langword="null"/> for top-level symbols like namespaces or certain error types.
        /// </summary>
        ISymbolInternal? ContainingSymbol { get; }

        /// <summary>
        /// Gets the <see cref="IAssemblySymbolInternal"/> for the containing assembly. Returns <see langword="null"/> if the
        /// symbol is shared across multiple assemblies.
        /// </summary>
        IAssemblySymbolInternal? ContainingAssembly { get; }

        /// <summary>
        /// Gets the <see cref="IModuleSymbolInternal"/> for the containing module. Returns <see langword="null"/> if the
        /// symbol is shared across multiple modules.
        /// </summary>
        IModuleSymbolInternal? ContainingModule { get; }

        /// <summary>
        /// Gets the <see cref="INamedTypeSymbolInternal"/> for the containing type. Returns <see langword="null"/> if the
        /// symbol is not contained within a type.
        /// </summary>
        INamedTypeSymbolInternal? ContainingType { get; }

        /// <summary>
        /// Gets the <see cref="INamespaceSymbolInternal"/> for the nearest enclosing namespace. Returns <see langword="null"/> if the
        /// symbol isn't contained in a namespace.
        /// </summary>
        INamespaceSymbolInternal? ContainingNamespace { get; }

        /// <summary>
        /// Gets a value indicating whether the symbol is the original definition. Returns false
        /// if the symbol is derived from another symbol, by type substitution for instance.
        /// </summary>
        bool IsDefinition { get; }

        /// <summary>
        /// Similar to getting the first location from <see cref="Locations"/>.  However, this can be more efficient as
        /// an intermediary array may not need to be created.  This can often be advantageous for perf as most symbols
        /// only have a single location, and most clients only need the first location for some purpose (like error
        /// reporting).
        /// </summary>
        /// <exception cref="InvalidOperationException">If the symbol has no locations.</exception>
        Location GetFirstLocation();

        /// <summary>
        /// Equivalent to calling <see cref="GetFirstLocation"/>,  except that if <see cref="Locations"/> is empty this
        /// will return <see cref="Location.None"/>.
        /// </summary>
        Location GetFirstLocationOrNone();

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
        /// Gets a value indicating whether the symbol is defined externally.
        /// </summary>
        bool IsExtern { get; }

        /// <summary>
        /// Returns an <see cref="ISymbol"/> instance associated with this symbol.
        /// </summary>
        ISymbol GetISymbol();

        /// <summary>
        /// Returns an <see cref="Cci.IReference"/> instance associated with this symbol.
        /// In general, this API is not safe to use. Transition from symbols to Cci interfaces
        /// should be handled by PEModuleBuilder translation layer. One relatively safe scenario
        /// is to use it on a symbol that is a definition.
        /// </summary>
        Cci.IReference GetCciAdapter();

        /// <summary>
        /// <see langword="true"/> if this symbol has any location that is within <paramref name="tree"/>. <see
        /// langword="false"/> otherwise. Can be more efficient than iteration over all the <see
        /// cref="ISymbol.Locations"/> as it will avoid an unnecessary array allocation.
        /// </summary>
        /// <param name="definedWithinSpan">Optional span.  If present, the location of this symbol must be both inside
        /// this tree and within the span passed in.</param>
        bool IsDefinedInSourceTree(SyntaxTree tree, TextSpan? definedWithinSpan, CancellationToken cancellationToken = default);
    }
}
