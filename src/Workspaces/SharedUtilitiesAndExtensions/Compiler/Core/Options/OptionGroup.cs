// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace Microsoft.CodeAnalysis.Options
{
    /// <summary>
    /// Group/sub-feature associated with an option.
    /// </summary>
    internal sealed class OptionGroup(string name, string description, int priority = int.MaxValue, OptionGroup? parent = null)
    {
        public static readonly OptionGroup Default = new(string.Empty, string.Empty, int.MaxValue);

        /// <summary>
        /// Optional parent group.
        /// </summary>
        public OptionGroup? Parent { get; } = parent;

        /// <summary>
        /// A localizable resource description string for the option group.
        /// </summary>
        public string Description { get; } = description;

        /// <summary>
        /// Name of the option group
        /// </summary>
        public string Name { get; } = name;

        /// <summary>
        /// Relative priority of the option group with respect to other option groups within the same feature.
        /// </summary>
        public int Priority { get; } = priority;
    }
}
