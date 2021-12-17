// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Windows;
using Microsoft.CodeAnalysis.Editor.Host;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Formatting;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.InheritanceMargin
{
    internal sealed class InheritanceGlyphFactory : IGlyphFactory
    {
        private readonly IThreadingContext _threadingContext;
        private readonly IStreamingFindUsagesPresenter _streamingFindUsagesPresenter;
        private readonly ClassificationTypeMap _classificationTypeMap;
        private readonly IClassificationFormatMap _classificationFormatMap;
        private readonly IWaitIndicator _waitIndicator;
        private readonly IWpfTextView _textView;

        public InheritanceGlyphFactory(
            IThreadingContext threadingContext,
            IStreamingFindUsagesPresenter streamingFindUsagesPresenter,
            ClassificationTypeMap classificationTypeMap,
            IClassificationFormatMap classificationFormatMap,
            IWaitIndicator waitIndicator,
            IWpfTextView textView)
        {
            _threadingContext = threadingContext;
            _streamingFindUsagesPresenter = streamingFindUsagesPresenter;
            _classificationTypeMap = classificationTypeMap;
            _classificationFormatMap = classificationFormatMap;
            _waitIndicator = waitIndicator;
            _textView = textView;
        }

        public UIElement? GenerateGlyph(IWpfTextViewLine line, IGlyphTag tag)
        {
            if (tag is InheritanceMarginTag inheritanceMarginTag)
            {
                var membersOnLine = inheritanceMarginTag.MembersOnLine;
                Contract.ThrowIfTrue(membersOnLine.IsEmpty);

                return new MarginGlyph.InheritanceMargin(
                    _threadingContext,
                    _streamingFindUsagesPresenter,
                    _classificationTypeMap,
                    _classificationFormatMap,
                    _waitIndicator,
                    inheritanceMarginTag,
                    _textView);
            }

            return null;
        }
    }
}
