// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#nullable enable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Threading;

namespace Microsoft.CodeAnalysis
{
    /// <summary>
    /// Represents a symbol (namespace, class, method, parameter, etc.)
    /// exposed by the compiler.
    /// </summary>
    /// <remarks>
    /// This interface is reserved for implementation by its associated APIs. We reserve the right to
    /// change it in the future.
    /// </remarks>
    [InternalImplementationOnly]
    public interface ISymbol : IEquatable<ISymbol?>
    {
        /// <summary>
        /// Gets the <see cref="SymbolKind"/> indicating what kind of symbol it is.
        /// </summary>
        SymbolKind Kind { get; }

        /// <summary>
        /// Gets the source language ("C#" or "Visual Basic").
        /// </summary>
        string Language { get; }

        /// <summary>
        /// Gets the symbol name. Returns the empty string if unnamed.
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Gets the name of a symbol as it appears in metadata. Most of the time, this
        /// is the same as the Name property, with the following exceptions:
        /// <list type="number">
        /// <item>
        /// <description>The metadata name of generic types includes the "`1", "`2" etc. suffix that
        /// indicates the number of type parameters (it does not include, however, names of
        /// containing types or namespaces). </description>
        /// </item>
        /// <item>
        /// <description>The metadata name of explicit interface names have spaces removed, compared to
        /// the name property.</description>
        /// </item>
        /// <item>
        /// <description>The length of names is limited to not exceed metadata restrictions.</description>
        /// </item>
        /// </list>
        /// </summary>
        string MetadataName { get; }

#nullable disable
        /// <summary>
        /// Gets the <see cref="ISymbol"/> for the immediately containing symbol.
        /// </summary>
        ISymbol ContainingSymbol { get; }

        /// <summary>
        /// Gets the <see cref="IAssemblySymbol"/> for the containing assembly. Returns null if the
        /// symbol is shared across multiple assemblies.
        /// </summary>
        IAssemblySymbol ContainingAssembly { get; }

        /// <summary>
        /// Gets the <see cref="IModuleSymbol"/> for the containing module. Returns null if the
        /// symbol is shared across multiple modules.
        /// </summary>
        IModuleSymbol ContainingModule { get; }

        /// <summary>
        /// Gets the <see cref="INamedTypeSymbol"/> for the containing type. Returns null if the
        /// symbol is not contained within a type.
        /// </summary>
        INamedTypeSymbol ContainingType { get; }

        /// <summary>
        /// Gets the <see cref="INamespaceSymbol"/> for the nearest enclosing namespace. Returns null if the
        /// symbol isn't contained in a namespace.
        /// </summary>
        INamespaceSymbol ContainingNamespace { get; }
#nullable enable

        /// <summary>
        /// Gets a value indicating whether the symbol is the original definition. Returns false
        /// if the symbol is derived from another symbol, by type substitution for instance.
        /// </summary>
        bool IsDefinition { get; }

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
        /// Gets a value indicating whether the symbol is sealed.
        /// </summary>
        bool IsSealed { get; }

        /// <summary>
        /// Gets a value indicating whether the symbol is defined externally.
        /// </summary>
        bool IsExtern { get; }

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
        /// Returns true if this symbol can be referenced by its name in code.
        /// </summary>
        bool CanBeReferencedByName { get; }

        /// <summary>
        /// Gets the locations where the symbol was originally defined, either in source or
        /// metadata. Some symbols (for example, partial classes) may be defined in more than one
        /// location.
        /// </summary>
        ImmutableArray<Location> Locations { get; }

        /// <summary>
        /// Get the syntax node(s) where this symbol was declared in source. Some symbols (for example,
        /// partial classes) may be defined in more than one location. This property should return
        /// one or more syntax nodes only if the symbol was declared in source code and also was
        /// not implicitly declared (see the IsImplicitlyDeclared property). 
        /// 
        /// <para>
        /// Note that for namespace symbol, the declaring syntax might be declaring a nested namespace.
        /// For example, the declaring syntax node for N1 in "namespace N1.N2 {...}" is the entire
        /// NamespaceDeclarationSyntax for N1.N2. For the global namespace, the declaring syntax will
        /// be the CompilationUnitSyntax.
        /// </para>
        /// </summary>
        /// <returns>
        /// The syntax node(s) that declared the symbol. If the symbol was declared in metadata
        /// or was implicitly declared, returns an empty read-only array.
        /// </returns>
        ImmutableArray<SyntaxReference> DeclaringSyntaxReferences { get; }

        /// <summary>
        /// Gets the attributes for the symbol. Returns an empty <see cref="IEnumerable{ISymbolAttribute}"/>
        /// if there are no attributes.
        /// </summary>
        ImmutableArray<AttributeData> GetAttributes();

        /// <summary>
        /// Gets a <see cref="Accessibility"/> indicating the declared accessibility for the symbol.
        /// Returns NotApplicable if no accessibility is declared.
        /// </summary>
        Accessibility DeclaredAccessibility { get; }

        /// <summary>
        /// Gets the <see cref="ISymbol"/> for the original definition of the symbol.
        /// If this symbol is derived from another symbol, by type substitution for instance,
        /// this gets the original symbol, as it was defined in source or metadata.
        /// </summary>
        ISymbol OriginalDefinition { get; }

        void Accept(SymbolVisitor visitor);
        TResult Accept<TResult>(SymbolVisitor<TResult> visitor);

        /// <summary>
        /// Returns the Documentation Comment ID for the symbol, or null if the symbol doesn't
        /// support documentation comments.
        /// </summary>
        string? GetDocumentationCommentId();

        /// <summary>
        /// Gets the XML (as text) for the comment associated with the symbol.
        /// </summary>
        /// <param name="preferredCulture">Preferred culture or null for the default.</param>
        /// <param name="expandIncludes">Optionally, expand &lt;include&gt; elements.  No impact on non-source documentation comments.</param>
        /// <param name="cancellationToken">Token allowing cancellation of request.</param>
        /// <returns>The XML that would be written to the documentation file for the symbol.</returns>
        string? GetDocumentationCommentXml(CultureInfo? preferredCulture = null, bool expandIncludes = false, CancellationToken cancellationToken = default(CancellationToken));

        /// <summary>
        /// Converts the symbol to a string representation.
        /// </summary>
        /// <param name="format">Format or null for the default.</param>
        /// <returns>A formatted string representation of the symbol.</returns>
        string ToDisplayString(SymbolDisplayFormat? format = null);

        /// <summary>
        /// Convert a symbol to an array of string parts, each of which has a kind. Useful for
        /// colorizing the display string.
        /// </summary>
        /// <param name="format">Formatting rules - null implies
        /// SymbolDisplayFormat.ErrorMessageFormat.</param>
        /// <returns>A read-only array of string parts.</returns>
        ImmutableArray<SymbolDisplayPart> ToDisplayParts(SymbolDisplayFormat? format = null);

        /// <summary>
        /// Convert a symbol to a string that can be displayed to the user. May be tailored to a
        /// specific location in the source code.
        /// </summary>
        /// <param name="semanticModel">Binding information (for determining names appropriate to
        /// the context).</param>
        /// <param name="position">A position in the source code (context).</param>
        /// <param name="format">Formatting rules - null implies
        /// SymbolDisplayFormat.MinimallyQualifiedFormat.</param>
        /// <returns>A formatted string that can be displayed to the user.</returns>
        string ToMinimalDisplayString(
            SemanticModel semanticModel,
            int position,
            SymbolDisplayFormat? format = null);

        /// <summary>
        /// Convert a symbol to an array of string parts, each of which has a kind. May be tailored
        /// to a specific location in the source code. Useful for colorizing the display string.
        /// </summary>
        /// <param name="semanticModel">Binding information (for determining names appropriate to
        /// the context).</param>
        /// <param name="position">A position in the source code (context).</param>
        /// <param name="format">Formatting rules - null implies
        /// SymbolDisplayFormat.MinimallyQualifiedFormat.</param>
        /// <returns>A read-only array of string parts.</returns>
        ImmutableArray<SymbolDisplayPart> ToMinimalDisplayParts(
            SemanticModel semanticModel,
            int position,
            SymbolDisplayFormat? format = null);

        /// <summary>
        /// Indicates that this symbol uses metadata that cannot be supported by the language.
        /// 
        /// <para>
        /// Examples include:
        /// <list type="bullet">
        /// <item><description>Pointer types in VB</description></item>
        /// <item><description>ByRef return type</description></item>
        /// <item><description>Required custom modifiers</description></item>
        /// </list>
        /// </para>
        /// 
        /// <para>
        /// This is distinguished from, for example, references to metadata symbols defined in assemblies that weren't referenced.
        /// Symbols where this returns true can never be used successfully, and thus should never appear in any IDE feature.
        /// </para>
        /// 
        /// <para>
        /// This is set for metadata symbols, as follows:
        /// <list type="bullet">
        /// <item><description>Type - if a type is unsupported (for example, a pointer type)</description></item>
        /// <item><description>Method - parameter or return type is unsupported</description></item>
        /// <item><description>Field - type is unsupported</description></item>
        /// <item><description>Event - type is unsupported</description></item>
        /// <item><description>Property - type is unsupported</description></item>
        /// <item><description>Parameter - type is unsupported</description></item>
        /// </list>
        /// </para>
        /// </summary>
        bool HasUnsupportedMetadata { get; }

        /// <summary>
        /// Determines if this symbol is equal to another, according to the rules of the provided <see cref="SymbolEqualityComparer"/>
        /// </summary>
        /// <param name="other">The other symbol to compare against</param>
        /// <param name="equalityComparer">The <see cref="SymbolEqualityComparer"/> to use when comparing symbols</param>
        /// <returns>True if the symbols are equivalent.</returns>
        bool Equals([NotNullWhen(returnValue: true)] ISymbol? other, SymbolEqualityComparer equalityComparer);
    }
}
