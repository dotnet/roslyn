// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Diagnostics;
using Microsoft.CodeAnalysis;

namespace Microsoft.VisualStudio.LanguageServices.Implementation
{
    internal sealed class AssemblyNamePrefixWhiteList : IAssemblyWhiteList
    {
        private readonly string _prefix;

        public AssemblyNamePrefixWhiteList(string prefix)
        {
            Debug.Assert(prefix != null);

            _prefix = prefix;
        }

        public bool Includes(AssemblyIdentity assemblyIdentity)
        {
            return assemblyIdentity.Name.StartsWith(_prefix);
        }
    }
}
