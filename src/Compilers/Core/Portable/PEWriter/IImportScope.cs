// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;

namespace Microsoft.Cci
{
    /// <summary>
    /// Represents a lexical scope that declares imports.
    /// </summary>
    internal interface IImportScope
    {
        /// <summary>
        /// Zero or more used namespaces. These correspond to using directives in C# or Imports syntax in VB.
        /// Multiple invocations return the same array instance.
        /// </summary>
        ImmutableArray<UsedNamespaceOrType> GetUsedNamespaces();

        /// <summary>
        /// Parent import scope, or null.
        /// </summary>
        IImportScope Parent { get; }
    }
}
