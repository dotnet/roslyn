// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using Roslyn.Utilities;

namespace Microsoft.Cci
{
    /// <summary>
    /// Represents an assembly reference with an alias (C# only, /r:Name=Reference on command line).
    /// </summary>
    internal readonly struct AssemblyReferenceAlias
    {
        /// <summary>
        /// An alias for the global namespace of the assembly.
        /// </summary>
        public readonly string Name;

        /// <summary>
        /// The assembly reference.
        /// </summary>
        public readonly IAssemblyReference Assembly;

        internal AssemblyReferenceAlias(string name, IAssemblyReference assembly)
        {
            RoslynDebug.Assert(name != null);
            RoslynDebug.Assert(assembly != null);

            Name = name;
            Assembly = assembly;
        }
    }
}
