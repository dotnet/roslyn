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
    /// cref="SemanticModel.GetImportScope"/>
    /// </summary>
    public interface IImportScope
    {
        /// <summary>
        /// Next item in the chain.  This generally represents the next scope in a file, or compilation that pulls in
        /// imported symbols.
        /// </summary>
        IImportScope? Parent { get; }

        /// <summary>
        /// Aliases defined at this level of the chain.  This corresponds to <c>using X = TypeOrNamespace;</c> in C# or
        /// <c>Imports X = TypeOrNamespace</c> in Visual Basic.
        /// </summary>
        ImmutableArray<IAliasSymbol> Aliases { get; }

        /// <summary>
        /// Aliases defined at this level of the chain.  This corresponds to <c>extern alias X;</c> in C#.  It will be
        /// empty in Visual Basic.
        /// </summary>
        ImmutableArray<IAliasSymbol> ExternAliases { get; }

        /// <summary>
        /// Types or namespaces imported at this level of the chain.  This corresponds to <c>using Namespace;</c> or
        /// <c>using static Type;</c> in C#, or <c>Imports TypeOrNamespace</c> in Visual Basic.
        /// </summary>
        ImmutableArray<INamespaceOrTypeSymbol> Imports { get; }

        /// <summary>
        /// Xml namespaces imported at this level of the chain.  This corresponds to <c>Imports &lt;xmlns:prefix =
        /// "name"&gt;</c> in Visual Basic.  It will be empty in C#.
        /// </summary>
        ImmutableArray<string> XmlNamespaces { get; }
    }

    /// <summary>
    /// Simple POCO implementation of the import scope, usable by both C# and VB.
    /// </summary>
    internal sealed class ImportScope : IImportScope
    {
        public IImportScope? Parent { get; }

        public ImmutableArray<IAliasSymbol> Aliases { get; }
        public ImmutableArray<IAliasSymbol> ExternAliases { get; }
        public ImmutableArray<INamespaceOrTypeSymbol> Imports { get; }
        public ImmutableArray<string> XmlNamespaces { get; }

        public ImportScope(
            IImportScope? parent,
            ImmutableArray<IAliasSymbol> aliases,
            ImmutableArray<IAliasSymbol> externAliases,
            ImmutableArray<INamespaceOrTypeSymbol> imports,
            ImmutableArray<string> xmlNamespaces)
        {
            Debug.Assert(aliases.Length > 0 || externAliases.Length > 0 || imports.Length > 0 || xmlNamespaces.Length > 0);
            Parent = parent;
            Aliases = aliases;
            ExternAliases = externAliases;
            Imports = imports;
            XmlNamespaces = xmlNamespaces;
        }
    }
}
