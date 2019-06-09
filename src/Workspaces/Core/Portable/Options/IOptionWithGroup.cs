// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
