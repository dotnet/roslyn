// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Diagnostics;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;

namespace Microsoft.VisualStudio.LanguageServices.Implementation
{
    internal sealed class IgnorableAssemblyNameList : IIgnorableAssemblyList
    {
        private readonly ImmutableHashSet<string> _assemblyNamesToIgnore;

        public IgnorableAssemblyNameList(ImmutableHashSet<string> assemblyNamesToIgnore)
        {
            Debug.Assert(assemblyNamesToIgnore != null);

            _assemblyNamesToIgnore = assemblyNamesToIgnore;
        }

        public bool Includes(AssemblyIdentity assemblyIdentity)
        {
            return _assemblyNamesToIgnore.Contains(assemblyIdentity.Name);
        }
    }
}
