// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis;
using System.Reflection;

namespace Microsoft.CodeAnalysis.Scripting
{
    /// <summary>
    /// Loads assemblies for Reflection based APIs.
    /// </summary>
    public abstract class AssemblyLoader
    {
        private sealed class _Default : AssemblyLoader
        {
            public override Assembly Load(AssemblyIdentity identity, string location = null)
            {
                return Assembly.Load(identity.ToAssemblyName());
            }
        }

        internal static readonly AssemblyLoader Default = new _Default();

        /// <summary>
        /// Loads an assembly given its full name.
        /// </summary>
        /// <param name="identity">The identity of the assembly to load.</param>
        /// <param name="location">Location of the assembly.</param>
        /// <returns>The loaded assembly.</returns>
        public abstract Assembly Load(AssemblyIdentity identity, string location = null);
    }
}
