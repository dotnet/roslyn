// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CodeAnalysis.Options
{
    /// <summary>
    /// Group/sub-feature associated with an <see cref="IOption"/>.
    /// </summary>
    internal interface IOptionWithGroup : IOption
    {
        /// <summary>
        /// Group/sub-feature for this option.
        /// </summary>
        OptionGroup Group { get; }
    }
}
