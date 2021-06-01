// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.Editor.Implementation.Adornments;
using Microsoft.CodeAnalysis.Editor.Implementation.LineSeparators;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Tagging;

namespace Microsoft.CodeAnalysis.Editor.LineSeparators
{
    internal class LineSeparatorAdornmentManager : AdornmentManager<LineSeparatorTag>
    {
        public LineSeparatorAdornmentManager(IThreadingContext threadingContext, IWpfTextView textView, IViewTagAggregatorFactoryService tagAggregatorFactoryService, IAsynchronousOperationListener asyncListener, string adornmentLayerName)
            : base(threadingContext, textView, tagAggregatorFactoryService, asyncListener, adornmentLayerName)
        {
        }
    }
}
