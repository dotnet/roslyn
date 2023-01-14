// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Diagnostics;
using Microsoft.CodeAnalysis;

namespace Microsoft.VisualStudio.LanguageServices.Implementation
{
    internal sealed class IgnorableAssemblyNamePrefixList : IIgnorableAssemblyList
    {
        private readonly string _prefix;

        public IgnorableAssemblyNamePrefixList(string prefix)
        {
            Debug.Assert(prefix != null);

            _prefix = prefix;
        }

        public bool Includes(AssemblyIdentity assemblyIdentity)
            => assemblyIdentity.Name.StartsWith(_prefix);
    }
}
