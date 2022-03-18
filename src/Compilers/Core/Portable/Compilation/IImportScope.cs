// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Diagnostics;

namespace Microsoft.CodeAnalysis
{
    /// <summary>
    /// Represents a chain of symbols that are imported to a particular position in a source file.  Symbols may be
    /// imported, but may not necessarily be available at that location (for example, an alias symbol hidden by another
    /// symbol).  There is no guarantee that the same chain will be returned from successive calls to <see
    /// cref="SemanticModel.GetImportScopes"/>.  Each symbol or xml-namespace import has an optional associated location
    /// the import was declared at.  For all imports declared at a project level, this syntax reference will be null.
    /// </summary>
    public interface IImportScope
    {
        /// <summary>
        /// Aliases defined at this level of the chain.  This corresponds to <c>using X = TypeOrNamespace;</c> in C# or
        /// <c>Imports X = TypeOrNamespace</c> in Visual Basic.
        /// </summary>
        ImmutableArray<(IAliasSymbol alias, SyntaxReference? declaratingSyntaxReference)> Aliases { get; }

        /// <summary>
        /// Aliases defined at this level of the chain.  This corresponds to <c>extern alias X;</c> in C#.  It will be
        /// empty in Visual Basic.
        /// </summary>
        ImmutableArray<(IAliasSymbol alias, SyntaxReference? declaratingSyntaxReference)> ExternAliases { get; }

        /// <summary>
        /// Types or namespaces imported at this level of the chain.  This corresponds to <c>using Namespace;</c> or
        /// <c>using static Type;</c> in C#, or <c>Imports TypeOrNamespace</c> in Visual Basic.
        /// </summary>
        ImmutableArray<(INamespaceOrTypeSymbol namespaceOrType, SyntaxReference? declaratingSyntaxReference)> Imports { get; }

        /// <summary>
        /// Xml namespaces imported at this level of the chain.  This corresponds to <c>Imports &lt;xmlns:prefix =
        /// "name"&gt;</c> in Visual Basic.  It will be empty in C#.
        /// </summary>
        ImmutableArray<(string xmlNamespace, SyntaxReference? declaratingSyntaxReference)> XmlNamespaces { get; }
    }

    /// <summary>
    /// Simple POCO implementation of the import scope, usable by both C# and VB.
    /// </summary>
    internal sealed record SimpleImportScope(
        ImmutableArray<(IAliasSymbol alias, SyntaxReference? declaratingSyntaxReference)> Aliases,
        ImmutableArray<(IAliasSymbol alias, SyntaxReference? declaratingSyntaxReference)> ExternAliases,
        ImmutableArray<(INamespaceOrTypeSymbol namespaceOrType, SyntaxReference? declaratingSyntaxReference)> Imports,
        ImmutableArray<(string xmlNamespace, SyntaxReference? declaratingSyntaxReference)> XmlNamespaces) : IImportScope;
}
