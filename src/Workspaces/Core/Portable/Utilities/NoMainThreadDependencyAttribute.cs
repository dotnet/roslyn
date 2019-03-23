// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;

namespace Microsoft.CodeAnalysis.Utilities
{
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Method | AttributeTargets.Parameter | AttributeTargets.Property | AttributeTargets.ReturnValue, AllowMultiple = false, Inherited = true)]
    internal sealed class NoMainThreadDependencyAttribute : Attribute
    {
        /// <summary>
        /// Gets or sets a value indicating whether the task is always completed.
        /// </summary>
        public bool AlwaysCompleted { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the dependency claim has been verified against the signatures and
        /// contracts of referenced code.
        /// </summary>
        public bool Verified { get; set; } = true;
    }
}
