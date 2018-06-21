// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis.Diagnostics
{
    /// <summary>
    /// Provide options keyed on <see cref="SyntaxTree"/>.
    /// </summary>
    public abstract class PerTreeOptionsProvider
    {
        /// <summary>
        /// Get options for a given <paramref name="tree"/>.
        /// </summary>
        public abstract OptionSet GetOptions(SyntaxTree tree);
    }
}
