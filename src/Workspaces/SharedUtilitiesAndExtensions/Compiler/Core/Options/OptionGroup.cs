// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace Microsoft.CodeAnalysis.Options
{
    /// <summary>
    /// Group/sub-feature associated with an option.
    /// </summary>
    internal sealed class OptionGroup
    {
        public static readonly OptionGroup Default = new(string.Empty, int.MaxValue);

        public OptionGroup(string description, int priority, OptionGroup? parent = null)
        {
            Description = description;
            Priority = priority;
            Parent = parent;
        }

        /// <summary>
        /// Optional parent group.
        /// </summary>
        public OptionGroup? Parent { get; }

        /// <summary>
        /// A localizable resource description string for the option group.
        /// </summary>
        public string Description { get; }

        /// <summary>
        /// Relative priority of the option group with respect to other option groups within the same feature.
        /// </summary>
        public int Priority { get; }
    }
}
