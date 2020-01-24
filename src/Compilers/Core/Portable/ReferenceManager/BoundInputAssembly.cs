﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;

namespace Microsoft.CodeAnalysis
{
    internal partial class CommonReferenceManager<TCompilation, TAssemblySymbol>
    {
        /// <summary>
        /// Result of binding an input assembly and its references. 
        /// </summary>
        [DebuggerDisplay("{GetDebuggerDisplay(), nq}")]
        internal struct BoundInputAssembly
        {
            /// <summary>
            /// Suitable AssemblySymbol instance for the corresponding assembly, 
            /// null reference if none is available/found.
            /// </summary>
            internal TAssemblySymbol AssemblySymbol;

            /// <summary>
            /// For each AssemblyRef of this AssemblyDef specifies which AssemblyDef matches the reference.
            /// </summary>
            /// <remarks>
            /// Result of resolving assembly references of the corresponding assembly 
            /// against provided set of assemblies. Essentially, this is an array returned by
            /// AssemblyData.BindAssemblyReferences method. 
            /// 
            /// Each element describes the assembly the corresponding reference of the input assembly 
            /// is bound to.
            /// </remarks>
            internal AssemblyReferenceBinding[] ReferenceBinding;

            private string GetDebuggerDisplay()
            {
                return AssemblySymbol == null ? "?" : AssemblySymbol.ToString();
            }
        }
    }
}
