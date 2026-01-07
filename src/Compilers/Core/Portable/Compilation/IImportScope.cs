// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Diagnostics;

namespace Microsoft.CodeAnalysis;

/// <summary>
/// Represents the set of symbols that are imported to a particular position in a source file. Each import has a
/// reference to the location the import directive was declared at.  For the <see cref="IAliasSymbol"/> import, the
/// location can be found using either <see cref="ISymbol.Locations"/> or <see
/// cref="ISymbol.DeclaringSyntaxReferences"/> on the <see cref="IAliasSymbol"/> itself.  For <see cref="Imports"/>
/// or <see cref="XmlNamespaces"/> the location is found through <see
/// cref="ImportedNamespaceOrType.DeclaringSyntaxReference"/> or <see
/// cref="ImportedXmlNamespace.DeclaringSyntaxReference"/> respectively.
/// </summary>
/// <remarks>
/// <list type="bullet">
/// <item><description>Scopes returned will always have at least one non-empty property value in them.</description></item>
/// <item><description>Symbols may be imported, but may not necessarily be available at that location (for example, an alias
/// symbol hidden by another symbol).</description></item>
/// <item>
/// <description>In C# there will be an <see cref="IImportScope"/> for every containing namespace-declarations that include any
/// import directives.  There will also be an <see cref="IImportScope"/> for the containing compilation-unit if it
/// includes any import directives or if there are global import directives pulled in from other files.</description>
/// </item>
/// <item>
/// <description>In Visual Basic there will commonly be one or two <see cref="IImportScope"/>s returned for any position.  This will
/// commonly be a scope for the containing compilation unit if it includes any import directives.  As well as a scope
/// representing any imports specified at the project level.</description>
/// </item>
/// <item>
/// <description>Elements of any property have no defined order.  Even if they represent items from a single document, they are
/// not guaranteed to be returned in any specific file-oriented order.</description>
/// </item>
/// <item><description>There is no guarantee that the same scope instances will be returned from successive calls to <see
/// cref="SemanticModel.GetImportScopes"/>.</description></item>
/// </list>
/// </remarks>
public interface IImportScope
{
    /// <summary>
    /// Aliases defined at this level of the chain.  This corresponds to <c>using X = TypeOrNamespace;</c> in C# or
    /// <c>Imports X = TypeOrNamespace</c> in Visual Basic.  This will include global aliases if present for both
    /// languages.
    /// </summary>
    /// <remarks>May be <see cref="ImmutableArray{T}.Empty"/>, will never be <see cref="ImmutableArray{T}.IsDefault"/>.</remarks>
    ImmutableArray<IAliasSymbol> Aliases { get; }

    /// <summary>
    /// Extern aliases defined at this level of the chain.  This corresponds to <c>extern alias X;</c> in C#.  It
    /// will be empty in Visual Basic.
    /// </summary>
    /// <remarks>May be <see cref="ImmutableArray{T}.Empty"/>, will never be <see cref="ImmutableArray{T}.IsDefault"/>.</remarks>
    ImmutableArray<IAliasSymbol> ExternAliases { get; }

    /// <summary>
    /// Types or namespaces imported at this level of the chain.  This corresponds to <c>using Namespace;</c> or
    /// <c>using static Type;</c> in C#, or <c>Imports TypeOrNamespace</c> in Visual Basic.  This will include
    /// global namespace or type imports for both languages.
    /// </summary>
    /// <remarks>May be <see cref="ImmutableArray{T}.Empty"/>, will never be <see cref="ImmutableArray{T}.IsDefault"/>.</remarks>
    ImmutableArray<ImportedNamespaceOrType> Imports { get; }

    /// <summary>
    /// Xml namespaces imported at this level of the chain.  This corresponds to <c>Imports &lt;xmlns:prefix =
    /// "name"&gt;</c> in Visual Basic.  It will be empty in C#.
    /// </summary>
    /// <remarks>May be <see cref="ImmutableArray{T}.Empty"/>, will never be <see cref="ImmutableArray{T}.IsDefault"/>.</remarks>
    ImmutableArray<ImportedXmlNamespace> XmlNamespaces { get; }
}

/// <summary>
/// Represents an <see cref="INamespaceOrTypeSymbol"/> that has been imported, and the location the import was
/// declared at.  This corresponds to <c>using Namespace;</c> or <c>using static Type;</c> in C#, or <c>Imports
/// TypeOrNamespace</c> in Visual Basic.
/// </summary>
public readonly struct ImportedNamespaceOrType
{
    public INamespaceOrTypeSymbol NamespaceOrType { get; }

#pragma warning disable CA1200 // Avoid using cref tags with a prefix
    /// <summary>
    /// Location in source where the <c>using</c> directive or <c>Imports</c> clause was declared. May be null for
    /// Visual Basic for a project-level import directive, or for a C# global using provided directly through <see
    /// cref="P:Microsoft.CodeAnalysis.CSharp.CSharpCompilationOptions.Usings"/>.
    /// </summary>
    public SyntaxReference? DeclaringSyntaxReference { get; }
#pragma warning restore CA1200 // Avoid using cref tags with a prefix

    internal ImportedNamespaceOrType(INamespaceOrTypeSymbol namespaceOrType, SyntaxReference? declaringSyntaxReference)
    {
        NamespaceOrType = namespaceOrType;
        DeclaringSyntaxReference = declaringSyntaxReference;
    }
}

/// <summary>
/// Represents an imported xml namespace name. This corresponds to <c>Imports &lt;xmlns:prefix = "name"&gt;</c> in
/// Visual Basic.  It does not exist for C#.
/// </summary>
public readonly struct ImportedXmlNamespace
{
    public string XmlNamespace { get; }

    /// <summary>
    /// Location in source where the <c>Imports</c> clause was declared. May be null for a project-level import
    /// directive.
    /// </summary>
    public SyntaxReference? DeclaringSyntaxReference { get; }

    internal ImportedXmlNamespace(string xmlNamespace, SyntaxReference? declaringSyntaxReference)
    {
        XmlNamespace = xmlNamespace;
        DeclaringSyntaxReference = declaringSyntaxReference;
    }
}

/// <summary>
/// Simple POCO implementation of the import scope, usable by both C# and VB.
/// </summary>
internal sealed class SimpleImportScope : IImportScope
{
    public SimpleImportScope(
        ImmutableArray<IAliasSymbol> aliases,
        ImmutableArray<IAliasSymbol> externAliases,
        ImmutableArray<ImportedNamespaceOrType> imports,
        ImmutableArray<ImportedXmlNamespace> xmlNamespaces)
    {
        Debug.Assert(!aliases.IsDefault);
        Debug.Assert(!externAliases.IsDefault);
        Debug.Assert(!imports.IsDefault);
        Debug.Assert(!xmlNamespaces.IsDefault);
        Debug.Assert(aliases.Length + externAliases.Length + imports.Length + xmlNamespaces.Length > 0);

        // We make no guarantees about order of these arrays.  So intentionally reorder them in debug to help find any
        // cases where code may be depending on a particular order.
        Aliases = aliases.ConditionallyDeOrder();
        ExternAliases = externAliases.ConditionallyDeOrder();
        Imports = imports.ConditionallyDeOrder();
        XmlNamespaces = xmlNamespaces.ConditionallyDeOrder();
    }

    public ImmutableArray<IAliasSymbol> Aliases { get; }
    public ImmutableArray<IAliasSymbol> ExternAliases { get; }
    public ImmutableArray<ImportedNamespaceOrType> Imports { get; }
    public ImmutableArray<ImportedXmlNamespace> XmlNamespaces { get; }
}
