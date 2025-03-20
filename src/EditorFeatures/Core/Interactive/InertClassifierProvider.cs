// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.ComponentModel.Composition;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Text.Shared.Extensions;
using Microsoft.VisualStudio.InteractiveWindow;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Text.Editor;

namespace Microsoft.CodeAnalysis.Interactive
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

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public InertClassifierProvider()
        {
        }

        public IClassifier GetClassifier(ITextBuffer textBuffer)
            => new InertClassifier(textBuffer);

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
                if (classifier is IDisposable disposable)
                {
                    disposable.Dispose();
                }
            }
        }
    }
}
