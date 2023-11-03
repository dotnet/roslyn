// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Microsoft.CodeAnalysis.Editor.Shared.Options;
using Microsoft.CodeAnalysis.InheritanceMargin;
using Microsoft.CodeAnalysis.Options;
using Microsoft.VisualStudio.Extensibility.Testing;
using Microsoft.VisualStudio.LanguageServices.Implementation.InheritanceMargin;
using Microsoft.VisualStudio.LanguageServices.Implementation.InheritanceMargin.MarginGlyph;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.TextManager.Interop;
using Xunit;

namespace Roslyn.VisualStudio.NewIntegrationTests.InProcess
{
    [TestService]
    internal partial class InheritanceMarginInProcess
    {
        private const string MarginName = nameof(InheritanceMarginViewMargin);

        public async Task EnableOptionsAsync(string languageName, CancellationToken cancellationToken)
        {
            var optionService = await GetComponentModelServiceAsync<IGlobalOptionService>(cancellationToken);
            var showInheritanceMargin = optionService.GetOption(InheritanceMarginOptionsStorage.ShowInheritanceMargin, languageName);
            var combinedWithIndicatorMargin = optionService.GetOption(InheritanceMarginOptionsStorage.InheritanceMarginCombinedWithIndicatorMargin);
            var showGlobalUsings = optionService.GetOption(InheritanceMarginOptionsStorage.InheritanceMarginIncludeGlobalImports, languageName);

            if (showInheritanceMargin != true)
            {
                optionService.SetGlobalOption(InheritanceMarginOptionsStorage.ShowInheritanceMargin, languageName, true);
            }

            if (!showGlobalUsings)
            {
                optionService.SetGlobalOption(InheritanceMarginOptionsStorage.InheritanceMarginIncludeGlobalImports, languageName, true);
            }

            if (combinedWithIndicatorMargin)
            {
                // Glyphs in Indicator margin are owned by editor, and we don't know when the glyphs would be added/removed.
                optionService.SetGlobalOption(InheritanceMarginOptionsStorage.InheritanceMarginCombinedWithIndicatorMargin, false);
            }
        }

        public async Task DisableOptionsAsync(string languageName, CancellationToken cancellationToken)
        {
            var optionService = await GetComponentModelServiceAsync<IGlobalOptionService>(cancellationToken);
            var showInheritanceMargin = optionService.GetOption(InheritanceMarginOptionsStorage.ShowInheritanceMargin, languageName);
            var showGlobalUsings = optionService.GetOption(InheritanceMarginOptionsStorage.InheritanceMarginIncludeGlobalImports, languageName);

            if (showInheritanceMargin != false)
            {
                optionService.SetGlobalOption(InheritanceMarginOptionsStorage.ShowInheritanceMargin, languageName, false);
            }

            if (showGlobalUsings)
            {
                optionService.SetGlobalOption(InheritanceMarginOptionsStorage.InheritanceMarginIncludeGlobalImports, languageName, false);
            }
        }

        private async Task EnsureGlyphsAppearAsync(Func<CancellationToken, Task> makeChangeFunc, int expectedGlyphsNumberInMargin, CancellationToken cancellationToken)
        {
            var margin = await GetTextViewMarginAsync(cancellationToken);
            var marginCanvas = (Canvas)margin.VisualElement;
            var taskCompletionSource = new TaskCompletionSource<bool>();
            using var _ = cancellationToken.Register(() => taskCompletionSource.TrySetCanceled());

            try
            {
                marginCanvas.LayoutUpdated += OnGlyphChanged;
                await makeChangeFunc(cancellationToken);
                await taskCompletionSource.Task;
            }
            finally
            {
                marginCanvas.LayoutUpdated -= OnGlyphChanged;
            }

            void OnGlyphChanged(object sender, EventArgs e)
            {
                if (marginCanvas.Children.Count == expectedGlyphsNumberInMargin)
                {
                    taskCompletionSource.TrySetResult(true);
                }
            }
        }

        public Task EnableOptionsAndEnsureGlyphsAppearAsync(string languageName, int expectedGlyphsNumberInMargin, CancellationToken cancellationToken)
            => EnsureGlyphsAppearAsync(cts => EnableOptionsAsync(languageName, cts), expectedGlyphsNumberInMargin, cancellationToken);

        public Task SetTextAndEnsureGlyphsAppearAsync(string text, int expectedGlyphsNumberInMargin, CancellationToken cancellationToken)
            => EnsureGlyphsAppearAsync(cts => TestServices.Editor.SetTextAsync(text, cts), expectedGlyphsNumberInMargin, cancellationToken);

        public async Task ClickTheGlyphOnLine(int lineNumber, CancellationToken cancellationToken)
        {
            await WaitForApplicationIdleAsync(cancellationToken);
            var glyph = await GetTheGlyphOnLineAsync(lineNumber, cancellationToken);

            var point = await GetCenterOfGlyphOnScreenAsync(glyph, cancellationToken);
            await TestServices.Input.MoveMouseAsync(point, cancellationToken);
            await TestServices.Input.SendWithoutActivateAsync(
                simulator => simulator.Mouse.LeftButtonClick(), cancellationToken);
        }

        public async Task<InheritanceMarginGlyph> GetTheGlyphOnLineAsync(int lineNumber, CancellationToken cancellationToken)
        {
            await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);
            var activeView = await TestServices.Editor.GetActiveTextViewAsync(cancellationToken);
            var wpfTextViewLine = activeView.TextViewLines[lineNumber - 1];
            var midOfTheLine = wpfTextViewLine.TextTop + wpfTextViewLine.Height / 2;
            var margin = await GetTextViewMarginAsync(cancellationToken);
            var containingCanvas = (Canvas)margin.VisualElement;

            var glyphsOnLine = new List<InheritanceMarginGlyph>();
            foreach (var glyph in containingCanvas.Children)
            {
                if (glyph is InheritanceMarginGlyph inheritanceMarginGlyph)
                {
                    var glyphTop = Canvas.GetTop(inheritanceMarginGlyph);
                    var glyphBottom = glyphTop + inheritanceMarginGlyph.ActualHeight;
                    if (midOfTheLine > glyphTop && midOfTheLine < glyphBottom)
                    {
                        glyphsOnLine.Add(inheritanceMarginGlyph);
                    }
                }
            }

            if (glyphsOnLine.Count != 1)
            {
                Assert.False(true, $"{glyphsOnLine.Count} glyphs are found at line: {lineNumber}.");
            }

            return glyphsOnLine[0];
        }

        private async Task<IWpfTextViewMargin> GetTextViewMarginAsync(CancellationToken cancellationToken)
        {
            await WaitForApplicationIdleAsync(cancellationToken);
            var vsTextManager = await GetRequiredGlobalServiceAsync<SVsTextManager, IVsTextManager>(cancellationToken);
            var vsTextView = await vsTextManager.GetActiveViewAsync(JoinableTaskFactory, cancellationToken);
            var testViewHost = await vsTextView.GetTextViewHostAsync(JoinableTaskFactory, cancellationToken);
            return testViewHost.GetTextViewMargin(MarginName);
        }

        private async Task<Point> GetCenterOfGlyphOnScreenAsync(FrameworkElement glyph, CancellationToken cancellationToken)
        {
            await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);
            var center = new Point(glyph.ActualWidth / 2, glyph.ActualHeight / 2);
            return glyph.PointToScreen(center);
        }
    }
}
