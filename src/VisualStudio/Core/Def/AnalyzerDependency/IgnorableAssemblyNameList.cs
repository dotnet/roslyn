// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

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
            => _assemblyNamesToIgnore.Contains(assemblyIdentity.Name);
    }
}
