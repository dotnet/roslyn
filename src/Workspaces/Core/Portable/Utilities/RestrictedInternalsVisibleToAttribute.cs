// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;

namespace System.Runtime.CompilerServices
{
    [AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true)]
    internal sealed class RestrictedInternalsVisibleToAttribute : Attribute
    {
        public RestrictedInternalsVisibleToAttribute(string assemblyName, params string[] allowedNamespaces)
        {
            AssemblyName = assemblyName;
            AllowedNamespaces = allowedNamespaces.ToImmutableArray();
        }

        public string AssemblyName { get; }
        public ImmutableArray<string> AllowedNamespaces { get; }
    }
}
