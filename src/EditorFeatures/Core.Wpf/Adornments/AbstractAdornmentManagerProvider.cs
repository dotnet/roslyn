// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Editor.Shared.Options;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Tagging;

namespace Microsoft.CodeAnalysis.Editor.Implementation.Adornments
{
    internal abstract class AbstractAdornmentManagerProvider<TTag> :
        IWpfTextViewCreationListener
        where TTag : GraphicsTag
    {
        protected readonly IThreadingContext _threadingContext;
        protected readonly IViewTagAggregatorFactoryService _tagAggregatorFactoryService;
        protected readonly IAsynchronousOperationListener _asyncListener;

        protected AbstractAdornmentManagerProvider(
            IThreadingContext threadingContext,
            IViewTagAggregatorFactoryService tagAggregatorFactoryService,
            IAsynchronousOperationListenerProvider listenerProvider)
        {
            _threadingContext = threadingContext;
            _tagAggregatorFactoryService = tagAggregatorFactoryService;
            _asyncListener = listenerProvider.GetListener(this.FeatureAttributeName);
        }

        protected abstract string FeatureAttributeName { get; }
        protected abstract string AdornmentLayerName { get; }

        public abstract void TextViewCreated(IWpfTextView textView);
    }
}
