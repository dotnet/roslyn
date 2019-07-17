// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#nullable enable
using System;

namespace Microsoft.CodeAnalysis
{
    /// <summary>
    /// Represents the state of the nullable analysis at a specific point in a file. Bits one and
    /// two correspond to whether the nullable feature is enabled. Bits three and four correspond
    /// to whether the context was inherited from the global context.
    /// </summary>
    [Flags]
    public enum NullableContext
    {
        /// <summary>
        /// Nullable warnings and annotations are explicitly turned off at this location.
        /// </summary>
        Disabled = 0,

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
        /// files have nullable off by default, regardless of of the project-level
        /// default setting.
        /// </remarks>
        WarningsContextInherited = 1 << 2,

        /// <summary>
        /// The nullable annotation state is inherited from the project default.
        /// </summary>
        /// <remarks>
        /// The project default can change depending on the file type. Generated
        /// files have nullable off by default, regardless of of the project-level
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
        /// files have nullable off by default, regardless of of the project-level
        /// default setting.
        /// </remarks>
        ContextInherited = WarningsContextInherited | AnnotationsContextInherited
    }

    public static class NullableContextExtensions
    {
        private static bool IsFlagSet(NullableContext context, NullableContext flag) =>
            (context & flag) == flag;

        /// <summary>
        /// Returns whether nullable warnings are enabled for this context.
        /// </summary>
        public static bool WarningsEnabled(this NullableContext context) =>
            IsFlagSet(context, NullableContext.WarningsEnabled);

        /// <summary>
        /// Returns whether nullable annotations are enabled for this context.
        /// </summary>
        public static bool AnnotationsEnabled(this NullableContext context) =>
            IsFlagSet(context, NullableContext.AnnotationsEnabled);

        /// <summary>
        /// Returns whether the nullable warning state was inherited from the project default for this file type.
        /// </summary>
        public static bool WarningsInherited(this NullableContext context) =>
            IsFlagSet(context, NullableContext.WarningsContextInherited);

        /// <summary>
        /// Returns whether the nullable annotation state was inherited from the project default for this file type.
        /// </summary>
        public static bool AnnotationsInherited(this NullableContext context) =>
            IsFlagSet(context, NullableContext.AnnotationsContextInherited);
    }
}
