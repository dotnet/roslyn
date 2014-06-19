// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Diagnostics;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis
{
    partial class CommonReferenceManager<TCompilation, TAssemblySymbol>
    {
        /// <summary>
        /// Result of binding an AssemblyRef to an AssemblyDef. 
        /// </summary>
        [DebuggerDisplay("{GetDebuggerDisplay(), nq}")]
        internal struct AssemblyReferenceBinding
        {
            private readonly AssemblyIdentity referenceIdentity;
            private readonly int definitionIndex;
            private readonly int versionDifference;

            /// <summary>
            /// Failed binding.
            /// </summary>
            public AssemblyReferenceBinding(AssemblyIdentity referenceIdentity)
            {
                Debug.Assert(referenceIdentity != null);

                this.referenceIdentity = referenceIdentity;
                this.definitionIndex = -1;
                this.versionDifference = 0;
            }

            /// <summary>
            /// Successful binding.
            /// </summary>
            public AssemblyReferenceBinding(AssemblyIdentity referenceIdentity, int definitionIndex, int versionDifference = 0)
            {
                Debug.Assert(referenceIdentity != null);
                Debug.Assert(definitionIndex >= 0);
                Debug.Assert(versionDifference >= -1 && versionDifference <= +1);

                this.referenceIdentity = referenceIdentity;
                this.definitionIndex = definitionIndex;
                this.versionDifference = versionDifference;
            }

            /// <summary>
            /// Returns true if the reference was matched with the identity of the assembly being built.
            /// </summary>
            internal bool BoundToAssemblyBeingBuilt
            {
                get { return definitionIndex == 0; }
            }

            /// <summary>
            /// True if the definition index is available (reference was successfully matched with the definition).
            /// </summary>
            internal bool IsBound
            {
                get
                {
                    return definitionIndex >= 0;
                }
            }

            /// <summary>
            ///  0 if the reference is equivalent to the definition.
            /// -1 if version of the matched definition is lower than version of the reference, but the reference otherwise matches the definition.
            /// +1 if version of the matched definition is higher than version of the reference, but the reference otherwise matches the definition.
            ///   
            /// Undefined unless <see cref="P:IsBound"/> is true.
            /// </summary>
            internal int VersionDifference
            {
                get
                {
                    Debug.Assert(IsBound);
                    return versionDifference;
                }
            }

            /// <summary>
            /// Index into assembly definition list.
            /// Undefined unless <see cref="P:IsBound"/> is true.
            /// </summary>
            internal int DefinitionIndex
            {
                get
                {
                    Debug.Assert(IsBound);
                    return definitionIndex;
                }
            }

            internal AssemblyIdentity ReferenceIdentity
            {
                get
                {
                    Debug.Assert(IsBound);
                    return referenceIdentity;
                }
            }

            private string GetDebuggerDisplay()
            {
                return IsBound ? ReferenceIdentity.GetDisplayName() + " -> #" + DefinitionIndex + (VersionDifference != 0 ? " VersionDiff=" + VersionDifference : "") : "unbound";
            }
        }
    }
}
