// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CodeAnalysis.UnusedReferences
{
    internal class Reference
    {
        /// <summary>
        /// Indicates the type of reference.
        /// </summary>
        public ReferenceType Type { get; }

        /// <summary>
        /// Uniquely identifies the reference.
        /// </summary>
        /// <remarks>
        /// Should match the Include or Name attribute used in the project file.
        /// </remarks>
        public string ItemSpec { get; }

        /// <summary>
        /// Indicates that this reference should be treated as if it were used.
        /// </summary>
        public bool TreatAsUsed { get; }
    }
}
