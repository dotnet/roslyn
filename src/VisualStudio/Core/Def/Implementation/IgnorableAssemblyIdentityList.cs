// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
        {
            return _assemblyIdentities.Contains(assemblyIdentity);
        }
    }
}
