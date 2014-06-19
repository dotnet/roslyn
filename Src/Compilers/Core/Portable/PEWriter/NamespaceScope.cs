// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Diagnostics;

namespace Microsoft.Cci
{
    /// <summary>
    /// This is a list of the using directives (including aliases) in a lexial scope.
    /// </summary>
    /// <remarks>
    /// This scope is tied to a particular method body, so that partial types can be accommodated.
    /// </remarks>
    internal class NamespaceScope
    {
        public static readonly NamespaceScope Empty = new NamespaceScope(ImmutableArray<UsedNamespaceOrType>.Empty);

        private readonly ImmutableArray<UsedNamespaceOrType> usedNamespaces;

        internal NamespaceScope(ImmutableArray<UsedNamespaceOrType> usedNamespaces)
        {
            Debug.Assert(!usedNamespaces.IsDefault);
            this.usedNamespaces = usedNamespaces;
        }

        /// <summary>
        /// Zero or more used namespaces. These correspond to using clauses in C#.
        /// </summary>
        public ImmutableArray<UsedNamespaceOrType> UsedNamespaces
        {
            get
            {
                return this.usedNamespaces;
            }
        }
    }
}