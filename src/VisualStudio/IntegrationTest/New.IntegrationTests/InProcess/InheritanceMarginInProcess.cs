// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using Microsoft.CodeAnalysis.Editor.Shared.Options;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.UnitTests;
using Microsoft.VisualStudio.CorDebugInterop;
using Microsoft.VisualStudio.Extensibility.Testing;
using Microsoft.VisualStudio.LanguageServices.Implementation.InheritanceMargin;
using Microsoft.VisualStudio.LanguageServices.Implementation.InheritanceMargin.MarginGlyph;
using Microsoft.VisualStudio.LanguageServices.InheritanceMargin;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.TextManager.Interop;
using Roslyn.Test.Utilities;
using Roslyn.Utilities;
using Xunit;

namespace Roslyn.VisualStudio.NewIntegrationTests.InProcess
{
    [TestService]
    internal partial class InheritanceMarginInProcess
    {
        private const double HeightAndWidthOfTheGlyph = InheritanceMarginViewMargin.HeightAndWidthOfMargin;
        private const string MarginName = nameof(InheritanceMarginViewMargin);

        public async Task EnableOptionsAsync(string languageName, CancellationToken cancellationToken)
        {
            var optionService = await GetComponentModelServiceAsync<IGlobalOptionService>(cancellationToken).ConfigureAwait(false);
            var showInheritanceMargin = optionService.GetOption(FeatureOnOffOptions.ShowInheritanceMargin, languageName);
            var combinedWithIndicatorMargin = optionService.GetOption(FeatureOnOffOptions.InheritanceMarginCombinedWithIndicatorMargin);

            if (showInheritanceMargin != true)
            {
                optionService.SetGlobalOption(new OptionKey(FeatureOnOffOptions.ShowInheritanceMargin, languageName), true);
            }

            if (combinedWithIndicatorMargin)
            {
                optionService.SetGlobalOption(new OptionKey(FeatureOnOffOptions.InheritanceMarginCombinedWithIndicatorMargin), false);
            }
        }

        public async Task SetTextAndEnsureGlyphsAppearAsync(string text, int expectedGlyphsNumberInMargin, CancellationToken cancellationToken)
        {
            var margin = await GetTextViewMarginAsync(cancellationToken);
            var marginCanvas = (InheritanceMarginCanvas)((Grid)margin.VisualElement).Children[0];
            var currentGlyphsNumber = marginCanvas.Children.Count;
            var taskCompletionSource = new TaskCompletionSource<bool>();
            using var _ = cancellationToken.Register(() => taskCompletionSource.SetCanceled());

            try
            {
                marginCanvas.OnGlyphsChanged += OnGlyphChanged;

                await TestServices.Editor.SetTextAsync(text, cancellationToken);
                await taskCompletionSource.Task;
            }
            finally
            {
                marginCanvas.OnGlyphsChanged -= OnGlyphChanged;
            }

            void OnGlyphChanged(object sender, (InheritanceMarginGlyph? glyphAdded, InheritanceMarginGlyph? glyphRemoved) changedGlyphs)
            {
                var (glyphAdded, glyphRemoved) = changedGlyphs;
                if (glyphAdded is not null)
                {
                    currentGlyphsNumber++;
                }

                if (glyphRemoved is not null)
                {
                    currentGlyphsNumber--;
                }

                if (currentGlyphsNumber == expectedGlyphsNumberInMargin)
                {
                    taskCompletionSource.SetResult(true);
                }
            }
        }

        public async Task ClickTheGlyphOnLine(int lineNumber, CancellationToken cancellationToken)
        {
            await WaitForApplicationIdleAsync(cancellationToken);
            var glyph = await GetTheGlyphOnLineAsync(lineNumber, cancellationToken);
            // TODO: Ideally, we should not rely on creating WPF event, and using real mouse to click.
            glyph.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent));
        }

        public async Task<InheritanceMarginGlyph> GetTheGlyphOnLineAsync(int lineNumber, CancellationToken cancellationToken)
        {
            await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);
            var activeView = await TestServices.Editor.GetActiveTextViewAsync(cancellationToken);
            var wpfTextViewLine = activeView.TextViewLines[lineNumber - 1];
            var viewTop = activeView.ViewportTop;
            var midOfTheLine = wpfTextViewLine.TextTop + wpfTextViewLine.Height / 2;
            var margin = await GetTextViewMarginAsync(cancellationToken);
            var containingCanvas = (Canvas)((Grid)margin.VisualElement).Children[0];

            foreach (var glyph in containingCanvas.Children)
            {
                if (glyph is InheritanceMarginGlyph inheritanceMarginGlyph)
                {
                    var glyphTop = Canvas.GetTop(inheritanceMarginGlyph);
                    var glyphBottom = glyphTop + HeightAndWidthOfTheGlyph;
                    if (midOfTheLine > glyphTop && midOfTheLine < glyphBottom)
                    {
                        return inheritanceMarginGlyph;
                    }
                }
            }

            Assert.False(true, $"No {nameof(InheritanceMarginGlyph)} is found at line: {lineNumber}.");
            throw ExceptionUtilities.Unreachable;
        }

        private async Task<IWpfTextViewMargin> GetTextViewMarginAsync(CancellationToken cancellationToken)
        {
            await WaitForApplicationIdleAsync(cancellationToken);
            var vsTextManager = await GetRequiredGlobalServiceAsync<SVsTextManager, IVsTextManager>(cancellationToken);
            var vsTextView = await vsTextManager.GetActiveViewAsync(JoinableTaskFactory, cancellationToken);
            var testViewHost = await vsTextView.GetTextViewHostAsync(JoinableTaskFactory, cancellationToken);
            return testViewHost.GetTextViewMargin(MarginName);
        }
    }
}
