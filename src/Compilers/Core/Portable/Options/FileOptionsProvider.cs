// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.CodeAnalysis.Options
{
    /// <summary>
    /// A class that provides <see cref="OptionSet" /> instances for a given file path. Implementations of this should be
    /// immutable; that is two implementations that are Equal according to <see cref="Object.Equals(object)"/> should return
    /// the same options for the same path.
    /// </summary>
    public abstract class FileOptionsProvider
    {
        /// <summary>
        /// Provides an <see cref="OptionSet"/> for options that apply to the given tree.
        /// </summary>
        /// <param name="path">The path of the syntax tree to provide options for.</param>
        /// <returns>An <see cref="OptionSet"/>. Will never return null.</returns>
        // PROTOTYPE: determine if this path is post-pathmap'ed or if this function still needs to do path mapping itself
        public abstract OptionSet GetOptionsForSyntaxTreePath(string path);
    }
}
