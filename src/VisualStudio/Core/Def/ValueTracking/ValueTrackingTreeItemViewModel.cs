// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.ObjectModel;
using Microsoft.CodeAnalysis;
using System.Windows.Documents;
using System.Windows.Media;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.VisualStudio.Language.Intellisense;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Classification;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.VisualStudio.LanguageServices.Implementation.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.ValueTracking
{
    internal class ValueTrackingTreeItemViewModel : TreeViewItemBase
    {
        private readonly SourceText _sourceText;
        private readonly ISymbol _symbol;
        private readonly IClassificationFormatMap _classificationFormatMap;
        private readonly ClassificationTypeMap _classificationTypeMap;
        private readonly IGlyphService _glyphService;

        protected Document Document { get; }
        protected int LineNumber { get; }

        public ObservableCollection<TreeViewItemBase> ChildItems { get; } = new();

        public string FileDisplay => $"[{Document.Name}:{LineNumber}]";

        public ImageSource GlyphImage => _symbol.GetGlyph().GetImageSource(_glyphService);

        public ImmutableArray<ClassifiedSpan> ClassifiedSpans { get; }

        public IList<Inline> Inlines
        {
            get
            {
                var classifiedTexts = ClassifiedSpans.SelectAsArray(
                   cs => new ClassifiedText(cs.ClassificationType, _sourceText.ToString(cs.TextSpan)));

                return classifiedTexts.ToInlines(
                    _classificationFormatMap,
                    _classificationTypeMap);
            }
        }

        public ValueTrackingTreeItemViewModel(
            Document document,
            int lineNumber,
            SourceText sourceText,
            ISymbol symbol,
            ImmutableArray<ClassifiedSpan> classifiedSpans,
            IClassificationFormatMap classificationFormatMap,
            ClassificationTypeMap classificationTypeMap,
            IGlyphService glyphService,
            ImmutableArray<ValueTrackingTreeItemViewModel> children = default)
        {
            Document = document;
            LineNumber = lineNumber;
            ClassifiedSpans = classifiedSpans;

            _sourceText = sourceText;
            _symbol = symbol;
            _classificationFormatMap = classificationFormatMap;
            _classificationTypeMap = classificationTypeMap;
            _glyphService = glyphService;

            if (!children.IsDefaultOrEmpty)
            {
                foreach (var child in children)
                {
                    ChildItems.Add(child);
                }
            }
        }
    }
}
