// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis
{
    /// <summary>
    /// Specifies the kind of debug information to be emitted.
    /// </summary>
    public enum DebugInformationKind
    {
        /// <summary>
        /// Emit no debug information.
        /// </summary>
        /// <remarks>
        /// Not specifying "/debug" command line switch or specifying "/debug-" command line switch enforces this setting.
        /// </remarks>
        None,

        /// <summary>
        /// Emit PDB file only.
        /// </summary>
        /// <remarks>
        /// Specifying "/debug:pdbonly" command line switch enforces this setting.
        /// </remarks>
        PdbOnly,

        /// <summary>
        /// Emit full debugging information.
        /// </summary>
        /// <remarks>
        /// Specifying "/debug" or "/debug:full" or "/debug+" command line switch enforces this setting.
        /// </remarks>
        Full
    };

    internal static partial class EnumBounds
    {
        internal static bool IsValid(this DebugInformationKind value)
        {
            return value >= DebugInformationKind.None && value <= DebugInformationKind.Full;
        }
    }
}