// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.VisualStudio.Text.Editor;

namespace Microsoft.CodeAnalysis.Editor.Implementation.Classification
{
    /// <summary>
    /// Determines whether a set of <see cref="ITextView"/>s that are not backed by a 
    /// workspace should be classified.
    /// </summary>
    internal interface IViewSupportsClassificationService
    {
        /// <returns>The return value must be consistent.</returns>
        bool CanClassifyViews(IEnumerable<ITextView> views);
    }
}
