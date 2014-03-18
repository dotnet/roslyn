// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.CodeAnalysis.Text;
using Cci = Microsoft.Cci;

namespace Microsoft.CodeAnalysis.CodeGen
{
    /// <summary>
    /// This is a list of the using directives (including aliases) in
    /// a namespace.
    /// </summary>
    internal class NamespaceScope : Cci.INamespaceScope
    {
        public static readonly NamespaceScope Empty = new NamespaceScope(ImmutableArray<UsedNamespaceOrType>.Empty);

        private readonly ImmutableArray<UsedNamespaceOrType> usedNamespaces;

        internal NamespaceScope(ImmutableArray<UsedNamespaceOrType> usedNamespaces)
        {
            Debug.Assert(!usedNamespaces.IsDefault);
            this.usedNamespaces = usedNamespaces;
        }

        ImmutableArray<Cci.IUsedNamespaceOrType> Cci.INamespaceScope.UsedNamespaces
        {
            get
            {
                return this.usedNamespaces.Cast<UsedNamespaceOrType, Cci.IUsedNamespaceOrType>();
            }
        }
    }
}