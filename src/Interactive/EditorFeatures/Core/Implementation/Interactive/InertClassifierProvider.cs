﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.ComponentModel.Composition;
using Microsoft.CodeAnalysis.Text.Shared.Extensions;
using Microsoft.VisualStudio.InteractiveWindow;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.CodeAnalysis.Editor.Implementation.Interactive
{
    /// <summary>
    /// A classifier provider that caches the classification results from actual classifiers and
    /// stores it on the text buffer so they can be used from that point on.  Used for interactive
    /// buffers that have been reset but which we still want to look good.
    /// </summary>
    [Export(typeof(IClassifierProvider))]
    [TextViewRole(PredefinedInteractiveTextViewRoles.InteractiveTextViewRole)]
    internal partial class InertClassifierProvider : IClassifierProvider
    {
        private static readonly object s_classificationsKey = new object();
        private readonly IViewClassifierAggregatorService _classifierAggregator;

        [ImportingConstructor]
        public InertClassifierProvider(IViewClassifierAggregatorService classifierAggregator)
        {
            _classifierAggregator = classifierAggregator;
        }

        public IClassifier GetClassifier(ITextBuffer textBuffer)
        {
            return new InertClassifier(textBuffer);
        }

        internal static void CaptureExistingClassificationSpans(
            IViewClassifierAggregatorService classifierAggregator, ITextView textView, ITextBuffer textBuffer)
        {
            // No need to do this more than once.
            if (textBuffer.Properties.ContainsProperty(s_classificationsKey))
            {
                return;
            }

            // Capture the existing set of classifications and attach them to the buffer as a
            // property.
            var classifier = classifierAggregator.GetClassifier(textView);
            try
            {
                var classifications = classifier.GetClassificationSpans(textBuffer.CurrentSnapshot.GetFullSpan());
                textBuffer.Properties.AddProperty(s_classificationsKey, classifications);
            }
            finally
            {
                var disposable = classifier as IDisposable;
                if (disposable != null)
                {
                    disposable.Dispose();
                }
            }
        }
    }
}
