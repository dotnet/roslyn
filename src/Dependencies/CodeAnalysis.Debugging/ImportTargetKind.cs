// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CodeAnalysis.Debugging
{
    internal enum ImportTargetKind
    {
        /// <summary>
        /// C# or VB namespace import.
        /// </summary>
        Namespace,

        /// <summary>
        /// C# or VB type import.
        /// </summary>
        Type,

        /// <summary>
        /// VB namespace or type alias target (not specified).
        /// </summary>
        NamespaceOrType,

        /// <summary>
        /// C# extern alias.
        /// </summary>
        Assembly,

        /// <summary>
        /// VB XML import.
        /// </summary>
        XmlNamespace,

        /// <summary>
        /// VB forwarding information (i.e. another method has the imports for this one).
        /// </summary>
        MethodToken,

        /// <summary>
        /// VB containing namespace (not an import).
        /// </summary>
        CurrentNamespace,

        /// <summary>
        /// VB root namespace (not an import).
        /// </summary>
        DefaultNamespace,

        /// <summary>
        /// A kind that is no longer used.
        /// </summary>
        Defunct,
    }
}
