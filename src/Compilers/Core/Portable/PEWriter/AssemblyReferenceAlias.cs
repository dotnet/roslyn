// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#nullable enable

using System.Diagnostics;
using Roslyn.Utilities;

namespace Microsoft.Cci
{
    /// <summary>
    /// Represents an assembly reference with an alias (C# only, /r:Name=Reference on command line).
    /// </summary>
    internal struct AssemblyReferenceAlias
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
