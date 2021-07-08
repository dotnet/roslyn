﻿// Licensed to the .NET Foundation under one or more agreements.
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
        protected readonly IThreadingContext ThreadingContext;
        protected readonly IViewTagAggregatorFactoryService TagAggregatorFactoryService;
        protected readonly IAsynchronousOperationListener AsyncListener;

        protected AbstractAdornmentManagerProvider(
            IThreadingContext threadingContext,
            IViewTagAggregatorFactoryService tagAggregatorFactoryService,
            IAsynchronousOperationListenerProvider listenerProvider)
        {
            ThreadingContext = threadingContext;
            TagAggregatorFactoryService = tagAggregatorFactoryService;
            AsyncListener = listenerProvider.GetListener(this.FeatureAttributeName);
        }

        protected abstract string FeatureAttributeName { get; }
        protected abstract string AdornmentLayerName { get; }

        protected abstract void CreateAdornmentManager(IWpfTextView textView);

        public void TextViewCreated(IWpfTextView textView)
        {
            if (textView == null)
            {
                throw new ArgumentNullException(nameof(textView));
            }

            if (!textView.TextBuffer.GetFeatureOnOffOption(EditorComponentOnOffOptions.Adornment))
            {
                return;
            }

            CreateAdornmentManager(textView);
        }
    }
}
