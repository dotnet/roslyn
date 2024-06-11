// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Extensibility.Testing;
using Microsoft.VisualStudio.IntegrationTest.Utilities;
using Microsoft.VisualStudio.LanguageServices;
using Microsoft.VisualStudio.Text.Adornments;
using Roslyn.Test.Utilities;
using Roslyn.Utilities;
using Xunit;

namespace Roslyn.VisualStudio.IntegrationTests.InProcess
{
    [TestService]
    internal partial class EditorVerifierInProcess
    {
        public async Task CurrentLineTextAsync(
            string expectedText,
            bool assertCaretPosition = false,
            CancellationToken cancellationToken = default)
        {
            if (assertCaretPosition)
            {
                await CurrentLineTextAndAssertCaretPositionAsync(expectedText, cancellationToken);
            }
            else
            {
                var lineText = await TestServices.Editor.GetCurrentLineTextAsync(cancellationToken);
                Assert.Equal(expectedText, lineText);
            }
        }

        private async Task CurrentLineTextAndAssertCaretPositionAsync(
            string expectedText,
            CancellationToken cancellationToken)
        {
            var expectedCaretIndex = expectedText.IndexOf("$$");
            if (expectedCaretIndex < 0)
            {
                throw new ArgumentException("Expected caret position to be specified with $$", nameof(expectedText));
            }

            var expectedCaretMarkupEndIndex = expectedCaretIndex + "$$".Length;

            var expectedTextBeforeCaret = expectedText[..expectedCaretIndex];
            var expectedTextAfterCaret = expectedText[expectedCaretMarkupEndIndex..];

            var lineText = await TestServices.Editor.GetCurrentLineTextAsync(cancellationToken);
            var lineTextBeforeCaret = await TestServices.Editor.GetLineTextBeforeCaretAsync(cancellationToken);
            var lineTextAfterCaret = await TestServices.Editor.GetLineTextAfterCaretAsync(cancellationToken);

            Assert.Equal(expectedTextBeforeCaret, lineTextBeforeCaret);
            Assert.Equal(expectedTextAfterCaret, lineTextAfterCaret);
            Assert.Equal(expectedTextBeforeCaret.Length + expectedTextAfterCaret.Length, lineText.Length);
        }

        public async Task TextEqualsAsync(
            string expectedText,
            CancellationToken cancellationToken)
        {
            var view = await TestServices.Editor.GetActiveTextViewAsync(cancellationToken);
            var editorText = view.TextSnapshot.GetText();
            var caretPosition = await TestServices.Editor.GetCaretPositionAsync(cancellationToken);
            editorText = editorText.Insert(caretPosition, "$$");
            AssertEx.EqualOrDiff(expectedText, editorText);
        }

        public async Task TextContainsAsync(
            string expectedText,
            bool assertCaretPosition = false,
            CancellationToken cancellationToken = default)
        {
            if (assertCaretPosition)
            {
                await TextContainsAndAssertCaretPositionAsync(expectedText, cancellationToken);
            }
            else
            {
                var view = await TestServices.Editor.GetActiveTextViewAsync(cancellationToken);
                var editorText = view.TextSnapshot.GetText();
                Assert.Contains(expectedText, editorText);
            }
        }

        private async Task TextContainsAndAssertCaretPositionAsync(
            string expectedText,
            CancellationToken cancellationToken)
        {
            var caretStartIndex = expectedText.IndexOf("$$");
            if (caretStartIndex < 0)
            {
                throw new ArgumentException("Expected caret position to be specified with $$", nameof(expectedText));
            }

            var caretEndIndex = caretStartIndex + "$$".Length;

            var expectedTextBeforeCaret = expectedText[..caretStartIndex];
            var expectedTextAfterCaret = expectedText[caretEndIndex..];

            var expectedTextWithoutCaret = expectedTextBeforeCaret + expectedTextAfterCaret;

            var view = await TestServices.Editor.GetActiveTextViewAsync(cancellationToken);
            var editorText = view.TextSnapshot.GetText();
            Assert.Contains(expectedTextWithoutCaret, editorText);

            var index = editorText.IndexOf(expectedTextWithoutCaret);

            var caretPosition = await TestServices.Editor.GetCaretPositionAsync(cancellationToken);
            Assert.Equal(caretStartIndex + index, caretPosition);
        }

        public async Task CodeActionAsync(
            string expectedItem,
            bool applyFix = false,
            bool verifyNotShowing = false,
            bool ensureExpectedItemsAreOrdered = false,
            FixAllScope? fixAllScope = null,
            bool blockUntilComplete = true,
            CancellationToken cancellationToken = default)
        {
            var expectedItems = new[] { expectedItem };

            bool? applied;
            do
            {
                cancellationToken.ThrowIfCancellationRequested();

                applied = await CodeActionsAsync(expectedItems, applyFix ? expectedItem : null, verifyNotShowing,
                    ensureExpectedItemsAreOrdered, fixAllScope, blockUntilComplete, cancellationToken);
            } while (applied is false);
        }

        /// <returns>
        /// <list type="bullet">
        /// <item><description><see langword="true"/> if <paramref name="applyFix"/> is specified and the fix is successfully applied</description></item>
        /// <item><description><see langword="false"/> if <paramref name="applyFix"/> is specified but the fix is not successfully applied</description></item>
        /// <item><description><see langword="null"/> if <paramref name="applyFix"/> is false, so there is no fix to apply</description></item>
        /// </list>
        /// </returns>
        public async Task<bool?> CodeActionsAsync(
            IEnumerable<string> expectedItems,
            string? applyFix = null,
            bool verifyNotShowing = false,
            bool ensureExpectedItemsAreOrdered = false,
            FixAllScope? fixAllScope = null,
            bool blockUntilComplete = true,
            CancellationToken cancellationToken = default)
        {
            var events = new List<WorkspaceChangeEventArgs>();
            void WorkspaceChangedHandler(object sender, WorkspaceChangeEventArgs e) => events.Add(e);

            var workspace = await TestServices.Shell.GetComponentModelServiceAsync<VisualStudioWorkspace>(cancellationToken);
            using var workspaceEventRestorer = WithWorkspaceChangedHandler(workspace, WorkspaceChangedHandler);

            await TestServices.Editor.ShowLightBulbAsync(cancellationToken);

            if (verifyNotShowing)
            {
                await CodeActionsNotShowingAsync(cancellationToken);
                return null;
            }

            var actions = await TestServices.Editor.GetLightBulbActionsAsync(cancellationToken);

            if (expectedItems != null && expectedItems.Any())
            {
                if (ensureExpectedItemsAreOrdered)
                {
                    TestUtilities.ThrowIfExpectedItemNotFoundInOrder(
                        actions,
                        expectedItems);
                }
                else
                {
                    TestUtilities.ThrowIfExpectedItemNotFound(
                        actions,
                        expectedItems);
                }
            }

            if (fixAllScope.HasValue)
            {
                Assumes.Present(applyFix);
            }

            if (!RoslynString.IsNullOrEmpty(applyFix))
            {
                var codeActionLogger = new CodeActionLogger();
                using var loggerRestorer = WithLogger(AggregateLogger.AddOrReplace(codeActionLogger, Logger.GetLogger(), logger => logger is CodeActionLogger));

                var result = await TestServices.Editor.ApplyLightBulbActionAsync(applyFix, fixAllScope, blockUntilComplete, cancellationToken);

                if (blockUntilComplete)
                {
                    // wait for action to complete
                    await TestServices.Workspace.WaitForAllAsyncOperationsAsync(
                        new[]
                        {
                            FeatureAttribute.Workspace,
                            FeatureAttribute.LightBulb,
                        },
                        cancellationToken);

                    if (codeActionLogger.Messages.Any())
                    {
                        foreach (var e in events)
                        {
                            codeActionLogger.Messages.Add($"{e.OldSolution.WorkspaceVersion} to {e.NewSolution.WorkspaceVersion}: {e.Kind} {e.DocumentId}");
                        }
                    }

                    AssertEx.EqualOrDiff(
                        "",
                        string.Join(Environment.NewLine, codeActionLogger.Messages));
                }

                return result;
            }

            return null;
        }

        public async Task CodeActionsNotShowingAsync(CancellationToken cancellationToken)
        {
            if (await TestServices.Editor.IsLightBulbSessionExpandedAsync(cancellationToken))
            {
                throw new InvalidOperationException("Expected no light bulb session, but one was found.");
            }
        }

        public async Task CaretPositionAsync(int expectedCaretPosition, CancellationToken cancellationToken)
        {
            Assert.Equal(expectedCaretPosition, await TestServices.Editor.GetCaretPositionAsync(cancellationToken));
        }

        public async Task ErrorTagsAsync(
            (string errorType, TextSpan textSpan, string taggedText, string tooltipText)[] expectedTags, CancellationToken cancellationToken)
        {
            await TestServices.Workspace.WaitForAllAsyncOperationsAsync(
                new[] { FeatureAttribute.Workspace, FeatureAttribute.SolutionCrawlerLegacy, FeatureAttribute.DiagnosticService, FeatureAttribute.ErrorSquiggles },
                cancellationToken);

            var actualTags = await TestServices.Editor.GetErrorTagsAsync(cancellationToken);
            Assert.Equal(expectedTags.Length, actualTags.Length);
            for (var i = 0; i < expectedTags.Length; i++)
            {
                var expectedTag = expectedTags[i];
                var actualTaggedSpan = actualTags[i];
                Assert.Equal(expectedTag.errorType, actualTaggedSpan.Tag.ErrorType);
                Assert.Equal(expectedTag.textSpan.Start, actualTaggedSpan.Span.Start.Position);
                Assert.Equal(expectedTag.textSpan.Length, actualTaggedSpan.Span.Length);

                var actualTaggedText = actualTaggedSpan.Span.GetText();
                Assert.Equal(expectedTag.taggedText, actualTaggedText);

                AssertEx.NotNull(actualTaggedSpan.Tag.ToolTipContent);
                var containerElement = (ContainerElement)actualTaggedSpan.Tag.ToolTipContent;
                var actualTooltipText = CollectTextInRun(containerElement);
                Assert.Equal(expectedTag.tooltipText, actualTooltipText);
            }

            static string CollectTextInRun(ContainerElement? containerElement)
            {
                var builder = new StringBuilder();

                if (containerElement is not null)
                {
                    foreach (var element in containerElement.Elements)
                    {
                        if (element is ClassifiedTextElement classifiedTextElement)
                        {
                            foreach (var run in classifiedTextElement.Runs)
                            {
                                builder.Append(run.Text);
                            }
                        }
                    }
                }

                return builder.ToString();
            }
        }

        public async Task CurrentTokenTypeAsync(string tokenType, CancellationToken cancellationToken)
        {
            await TestServices.Workspace.WaitForAllAsyncOperationsAsync(
                new[] { FeatureAttribute.SolutionCrawlerLegacy, FeatureAttribute.DiagnosticService, FeatureAttribute.Classification },
                cancellationToken);

            var actualTokenTypes = await TestServices.Editor.GetCurrentClassificationsAsync(cancellationToken);
            Assert.Equal(1, actualTokenTypes.Length);
            Assert.Contains(tokenType, actualTokenTypes[0]);
            Assert.NotEqual("text", tokenType);
        }

        private static WorkspaceEventRestorer WithWorkspaceChangedHandler(Workspace workspace, EventHandler<WorkspaceChangeEventArgs> eventHandler)
        {
            workspace.WorkspaceChanged += eventHandler;
            return new WorkspaceEventRestorer(workspace, eventHandler);
        }

        private static LoggerRestorer WithLogger(ILogger logger)
        {
            return new LoggerRestorer(Logger.SetLogger(logger));
        }

        private sealed class CodeActionLogger : ILogger
        {
            public List<string> Messages { get; } = new();

            public bool IsEnabled(FunctionId functionId)
            {
                return functionId == FunctionId.Workspace_ApplyChanges;
            }

            public void Log(FunctionId functionId, LogMessage logMessage)
            {
                if (functionId != FunctionId.Workspace_ApplyChanges)
                    return;

                lock (Messages)
                {
                    Messages.Add(logMessage.GetMessage());
                }
            }

            public void LogBlockEnd(FunctionId functionId, LogMessage logMessage, int uniquePairId, int delta, CancellationToken cancellationToken)
            {
            }

            public void LogBlockStart(FunctionId functionId, LogMessage logMessage, int uniquePairId, CancellationToken cancellationToken)
            {
            }
        }

        private readonly struct WorkspaceEventRestorer : IDisposable
        {
            private readonly Workspace _workspace;
            private readonly EventHandler<WorkspaceChangeEventArgs> _eventHandler;

            public WorkspaceEventRestorer(Workspace workspace, EventHandler<WorkspaceChangeEventArgs> eventHandler)
            {
                _workspace = workspace;
                _eventHandler = eventHandler;
            }

            public void Dispose()
            {
                _workspace.WorkspaceChanged -= _eventHandler;
            }
        }

        private readonly struct LoggerRestorer : IDisposable
        {
            private readonly ILogger? _logger;

            public LoggerRestorer(ILogger? logger)
            {
                _logger = logger;
            }

            public void Dispose()
            {
                Logger.SetLogger(_logger);
            }
        }
    }
}
