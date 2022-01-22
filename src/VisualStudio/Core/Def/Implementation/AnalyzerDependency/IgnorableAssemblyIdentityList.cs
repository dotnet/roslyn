// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.CodeAnalysis;

namespace Microsoft.VisualStudio.LanguageServices.Implementation
{
    internal sealed class IgnorableAssemblyIdentityList : IIgnorableAssemblyList
    {
        private readonly HashSet<AssemblyIdentity> _assemblyIdentities;

        public IgnorableAssemblyIdentityList(IEnumerable<AssemblyIdentity> assemblyIdentities)
        {
            Debug.Assert(assemblyIdentities != null);

            _assemblyIdentities = new HashSet<AssemblyIdentity>(assemblyIdentities);
        }

        public bool Includes(AssemblyIdentity assemblyIdentity)
            => _assemblyIdentities.Contains(assemblyIdentity);
    }
}
