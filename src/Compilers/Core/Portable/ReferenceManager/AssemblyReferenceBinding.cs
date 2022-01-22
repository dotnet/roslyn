// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis
{
    internal partial class CommonReferenceManager<TCompilation, TAssemblySymbol>
    {
        /// <summary>
        /// Result of binding an AssemblyRef to an AssemblyDef. 
        /// </summary>
        [DebuggerDisplay("{GetDebuggerDisplay(), nq}")]
        internal readonly struct AssemblyReferenceBinding
        {
            private readonly AssemblyIdentity? _referenceIdentity;
            private readonly int _definitionIndex;
            private readonly int _versionDifference;

            /// <summary>
            /// Failed binding.
            /// </summary>
            public AssemblyReferenceBinding(AssemblyIdentity referenceIdentity)
            {
                Debug.Assert(referenceIdentity != null);

                _referenceIdentity = referenceIdentity;
                _definitionIndex = -1;
                _versionDifference = 0;
            }

            /// <summary>
            /// Successful binding.
            /// </summary>
            public AssemblyReferenceBinding(AssemblyIdentity referenceIdentity, int definitionIndex, int versionDifference = 0)
            {
                Debug.Assert(referenceIdentity != null);
                Debug.Assert(definitionIndex >= 0);
                Debug.Assert(versionDifference >= -1 && versionDifference <= +1);

                _referenceIdentity = referenceIdentity;
                _definitionIndex = definitionIndex;
                _versionDifference = versionDifference;
            }

            /// <summary>
            /// Returns true if the reference was matched with the identity of the assembly being built.
            /// </summary>
            internal bool BoundToAssemblyBeingBuilt
            {
                get { return _definitionIndex == 0; }
            }

            /// <summary>
            /// True if the definition index is available (reference was successfully matched with the definition).
            /// </summary>
            internal bool IsBound
            {
                get
                {
                    return _definitionIndex >= 0;
                }
            }

            /// <summary>
            ///  0 if the reference is equivalent to the definition.
            /// -1 if version of the matched definition is lower than version of the reference, but the reference otherwise matches the definition.
            /// +1 if version of the matched definition is higher than version of the reference, but the reference otherwise matches the definition.
            ///   
            /// Undefined unless <see cref="IsBound"/> is true.
            /// </summary>
            internal int VersionDifference
            {
                get
                {
                    Debug.Assert(IsBound);
                    return _versionDifference;
                }
            }

            /// <summary>
            /// Index into assembly definition list.
            /// Undefined unless <see cref="IsBound"/> is true.
            /// </summary>
            internal int DefinitionIndex
            {
                get
                {
                    Debug.Assert(IsBound);
                    return _definitionIndex;
                }
            }

            internal AssemblyIdentity? ReferenceIdentity
            {
                get
                {
                    return _referenceIdentity;
                }
            }

            private string GetDebuggerDisplay()
            {
                var displayName = ReferenceIdentity?.GetDisplayName() ?? "";
                return IsBound ? displayName + " -> #" + DefinitionIndex + (VersionDifference != 0 ? " VersionDiff=" + VersionDifference : "") : "unbound";
            }
        }
    }
}
