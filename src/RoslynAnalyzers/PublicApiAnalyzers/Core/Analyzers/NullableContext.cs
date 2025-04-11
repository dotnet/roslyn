// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics.CodeAnalysis;

namespace Analyzer.Utilities.Lightup
{
    /// <summary>
    /// Represents the state of the nullable analysis at a specific point in a file. Bits one and
    /// two correspond to whether the nullable feature is enabled. Bits three and four correspond
    /// to whether the context was inherited from the global context.
    /// </summary>
    [Flags]
    [SuppressMessage("Naming", "CA1714:Flags enums should have plural names", Justification = "Intentionally uses the name from Roslyn.")]
    internal enum NullableContext
    {
        /// <summary>
        /// Nullable warnings and annotations are explicitly turned off at this location.
        /// </summary>
#pragma warning disable CA1008 // Enums should have zero value - Rename zero valued field to 'None'
        Disabled = 0,
#pragma warning restore CA1008 // Enums should have zero value

        /// <summary>
        /// Nullable warnings are enabled and will be reported at this file location.
        /// </summary>
        WarningsEnabled = 1,

        /// <summary>
        /// Nullable annotations are enabled and will be shown when APIs defined at
        /// this location are used in other contexts.
        /// </summary>
        AnnotationsEnabled = 1 << 1,

        /// <summary>
        /// The nullable feature is fully enabled.
        /// </summary>
        Enabled = WarningsEnabled | AnnotationsEnabled,

        /// <summary>
        /// The nullable warning state is inherited from the project default.
        /// </summary>
        /// <remarks>
        /// The project default can change depending on the file type. Generated
        /// files have nullable off by default, regardless of the project-level
        /// default setting.
        /// </remarks>
        WarningsContextInherited = 1 << 2,

        /// <summary>
        /// The nullable annotation state is inherited from the project default.
        /// </summary>
        /// <remarks>
        /// The project default can change depending on the file type. Generated
        /// files have nullable off by default, regardless of the project-level
        /// default setting.
        /// </remarks>
        AnnotationsContextInherited = 1 << 3,

        /// <summary>
        /// The current state of both warnings and annotations are inherited from
        /// the project default.
        /// </summary>
        /// <remarks>
        /// This flag is set by default at the start of all files.
        ///
        /// The project default can change depending on the file type. Generated
        /// files have nullable off by default, regardless of the project-level
        /// default setting.
        /// </remarks>
        ContextInherited = WarningsContextInherited | AnnotationsContextInherited
    }
}
