// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Collections.ObjectModel;
using System.Windows.Documents;
using System.Windows.Media;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Classification;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Navigation;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.LanguageServices.Implementation.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.ValueTracking
{
    internal class ValueTrackingTreeItemViewModel : TreeViewItemBase
    {
        protected SourceText SourceText { get; }
        private readonly ISymbol _symbol;
        private readonly IGlyphService _glyphService;

        protected ValueTrackingTreeViewModel TreeViewModel { get; }
        protected TextSpan TextSpan { get; }
        protected LineSpan LineSpan { get; }
        protected Document Document { get; }
        protected IThreadingContext ThreadingContext { get; }

        public int LineNumber => LineSpan.Start;
        public ObservableCollection<TreeViewItemBase> ChildItems { get; } = new();

        public string FileDisplay => $"[{Document.Name}:{LineNumber}]";

        public ImageSource GlyphImage => _symbol.GetGlyph().GetImageSource(_glyphService);

        public ImmutableArray<ClassifiedSpan> ClassifiedSpans { get; }

        public IList<Inline> Inlines => GetInlines(20);

        public ValueTrackingTreeItemViewModel? Parent { get; set; }

        public ValueTrackingTreeItemViewModel(
            Document document,
            TextSpan textSpan,
            SourceText sourceText,
            ISymbol symbol,
            ImmutableArray<ClassifiedSpan> classifiedSpans,
            ValueTrackingTreeViewModel treeViewModel,
            IGlyphService glyphService,
            IThreadingContext threadingContext,
            ImmutableArray<ValueTrackingTreeItemViewModel> children = default)
        {
            Document = document;
            TextSpan = textSpan;

            ClassifiedSpans = classifiedSpans;
            TreeViewModel = treeViewModel;
            ThreadingContext = threadingContext;

            SourceText = sourceText;
            _symbol = symbol;
            _glyphService = glyphService;

            if (!children.IsDefaultOrEmpty)
            {
                foreach (var child in children)
                {
                    child.Parent = this;
                    ChildItems.Add(child);
                }
            }

            sourceText.GetLineAndOffset(textSpan.Start, out var lineStart, out var _);
            sourceText.GetLineAndOffset(textSpan.End, out var lineEnd, out var _);
            LineSpan = LineSpan.FromBounds(lineStart, lineEnd);
        }

        public virtual void Select()
        {
            var workspace = Document.Project.Solution.Workspace;
            var navigationService = workspace.Services.GetService<IDocumentNavigationService>();
            if (navigationService is null)
            {
                return;
            }

            // While navigating do not activate the tab, which will change focus from the tool window
            var options = workspace.Options
                .WithChangedOption(new OptionKey(NavigationOptions.PreferProvisionalTab), true)
                .WithChangedOption(new OptionKey(NavigationOptions.ActivateTab), false);

            navigationService.TryNavigateToLineAndOffset(workspace, Document.Id, LineSpan.Start, 0, options, ThreadingContext.DisposalToken);
        }

        protected virtual IList<Inline> GetInlines(int maxLength)
        {
            var classifiedTexts = ClassifiedSpans.SelectAsArray(
                   cs => new ClassifiedText(cs.ClassificationType, SourceText.ToString(cs.TextSpan)));

            return classifiedTexts.ToInlines(
                    TreeViewModel.ClassificationFormatMap,
                    TreeViewModel.ClassificationTypeMap,
                    (run, text, position) => BoldRunIfNeeded(run, position, TextSpan.Start, TextSpan.End));
        }

        protected static void BoldRunIfNeeded(Run run, int position, int start, int end)
        {
            if (position >= start && position <= end)
            {
                // Emphasize the span that's relavent for expansion using bold
                run.SetValue(
                    TextElement.FontWeightProperty,
                    System.Windows.FontWeights.ExtraBold);
            }
            else
            {
                // Everything that isn't being emphasized should not be bold
                run.SetValue(
                    TextElement.FontWeightProperty,
                    System.Windows.FontWeights.Normal);
            }
        }
    }
}
