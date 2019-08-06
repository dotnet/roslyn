// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#nullable enable
using System;

namespace Microsoft.CodeAnalysis
{
    /// <summary>
    /// Represents the default state of nullable analysis in this compilation.
    /// </summary>
    [Flags]
    public enum NullableContextOptions
    {
        /// <summary>
        /// The nullable analysis feature is disabled.
        /// </summary>
        Disable = 0,

        /// <summary>
        /// Nullable warnings are enabled and will be reported by default.
        /// </summary>
        Warnings = 1,

        /// <summary>
        /// Nullable annotations are enabled and will be shown when APIs
        /// defined in this project are used in other contexts.
        /// </summary>
        Annotations = 1 << 1,

        /// <summary>
        /// The nullable analysis feature is fully enabled.
        /// </summary>
        Enable = Warnings | Annotations,
    }

    public static class NullableContextOptionsExtensions
    {
        private static bool IsFlagSet(NullableContextOptions context, NullableContextOptions flag) =>
            (context & flag) == flag;

        /// <summary>
        /// Returns whether nullable warnings are enabled.
        /// </summary>
        public static bool WarningsEnabled(this NullableContextOptions context) =>
            IsFlagSet(context, NullableContextOptions.Warnings);

        /// <summary>
        /// Returns whether nullable annotations are enabled.
        /// </summary>
        public static bool AnnotationsEnabled(this NullableContextOptions context) =>
            IsFlagSet(context, NullableContextOptions.Annotations);
    }
}
