// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Windows.Documents;
using System.Windows.Media;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Classification;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.LanguageServices.Implementation.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.ValueTracking
{
    internal abstract class TreeItemViewModel : TreeViewItemBase
    {
        private readonly SourceText _sourceText;
        private readonly Glyph _glyph;
        private readonly IGlyphService _glyphService;

        protected ValueTrackingTreeViewModel TreeViewModel { get; }
        protected TextSpan TextSpan { get; }
        protected LineSpan LineSpan { get; }
        protected IThreadingContext ThreadingContext { get; }
        protected DocumentId DocumentId { get; }
        protected Workspace Workspace { get; }

        public int LineNumber => LineSpan.Start + 1; // LineSpan is 0 indexed, editors are not

        public string FileName { get; }

        public ImageSource GlyphImage => _glyph.GetImageSource(_glyphService);
        public bool ShowGlyph => !IsLoading;

        public ImmutableArray<ClassifiedSpan> ClassifiedSpans { get; }

        public ImmutableArray<Inline> Inlines => CalculateInlines();
        public override string AutomationName => _sourceText.ToString(TextSpan);

        protected TreeItemViewModel(
            TextSpan textSpan,
            SourceText sourceText,
            DocumentId documentId,
            string fileName,
            Glyph glyph,
            ImmutableArray<ClassifiedSpan> classifiedSpans,
            ValueTrackingTreeViewModel treeViewModel,
            IGlyphService glyphService,
            IThreadingContext threadingContext,
            Workspace workspace,
            ImmutableArray<TreeItemViewModel> children)
        {
            FileName = fileName;
            TextSpan = textSpan;
            _sourceText = sourceText;
            ClassifiedSpans = classifiedSpans;
            TreeViewModel = treeViewModel;
            ThreadingContext = threadingContext;

            _glyph = glyph;
            _glyphService = glyphService;
            Workspace = workspace;
            DocumentId = documentId;

            if (!children.IsDefaultOrEmpty)
            {
                foreach (var child in children)
                {
                    ChildItems.Add(child);
                }
            }

            sourceText.GetLineAndOffset(textSpan.Start, out var lineStart, out var _);
            sourceText.GetLineAndOffset(textSpan.End, out var lineEnd, out var _);
            LineSpan = LineSpan.FromBounds(lineStart, lineEnd);

            PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(IsLoading))
                {
                    NotifyPropertyChanged(nameof(ShowGlyph));
                }
            };

            TreeViewModel.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(TreeViewModel.HighlightBrush))
                {
                    // If the highlight changes we need to recalculate the inlines so the 
                    // highlighting is correct
                    NotifyPropertyChanged(nameof(Inlines));
                }
            };
        }

        public abstract void NavigateTo();

        private ImmutableArray<Inline> CalculateInlines()
        {
            if (ClassifiedSpans.IsDefaultOrEmpty)
            {
                return ImmutableArray<Inline>.Empty;
            }

            var classifiedTexts = ClassifiedSpans.SelectAsArray(
               cs =>
               {
                   return new ClassifiedText(cs.ClassificationType, _sourceText.ToString(cs.TextSpan));
               });

            var spanStartPosition = TextSpan.Start - ClassifiedSpans[0].TextSpan.Start;
            var highlightSpan = new TextSpan(spanStartPosition, TextSpan.Length);

            return classifiedTexts.ToInlines(
                TreeViewModel.ClassificationFormatMap,
                TreeViewModel.ClassificationTypeMap,
                (run, classifiedText, position) =>
                {
                    if (TreeViewModel.HighlightBrush is not null)
                    {
                        // Check the span start first because we always want to highlight a run that 
                        // is at the start, even if the TextSpan length is 0. If it's not the start,
                        // highlighting should still happen if the run position is contained within
                        // the span.
                        if (position == highlightSpan.Start || highlightSpan.Contains(position))
                        {
                            run.SetValue(
                                TextElement.BackgroundProperty,
                                TreeViewModel.HighlightBrush);
                        }
                    }
                }).ToImmutableArray();
        }
    }
}
