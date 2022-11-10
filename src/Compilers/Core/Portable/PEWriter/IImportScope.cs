// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Emit;

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
        ImmutableArray<UsedNamespaceOrType> GetUsedNamespaces(EmitContext context);

        /// <summary>
        /// Parent import scope, or null.
        /// </summary>
        IImportScope Parent { get; }
    }
}
