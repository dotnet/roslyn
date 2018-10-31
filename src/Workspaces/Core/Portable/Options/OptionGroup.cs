// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;

namespace Microsoft.CodeAnalysis.Options
{
    /// <summary>
    /// Group/sub-feature associated with an <see cref="IOption"/>.
    /// </summary>
    internal sealed class OptionGroup
    {
        public static readonly OptionGroup Default = new OptionGroup(string.Empty, int.MaxValue);

        public OptionGroup(string description, int priority)
        {
            Description = description ?? throw new ArgumentNullException(nameof(description));
            Priority = priority;
        }

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
