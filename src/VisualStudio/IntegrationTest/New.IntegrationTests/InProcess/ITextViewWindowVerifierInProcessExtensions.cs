// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.VisualStudio.IntegrationTest.Utilities;
using Microsoft.VisualStudio.LanguageServices;
using Roslyn.Test.Utilities;
using Roslyn.Utilities;
using Xunit;

namespace Roslyn.VisualStudio.NewIntegrationTests.InProcess;

internal static class ITextViewWindowVerifierInProcessExtensions
{
    public static async Task CodeActionAsync(
        this ITextViewWindowVerifierInProcess textViewWindowVerifier,
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

            applied = await textViewWindowVerifier.CodeActionsAsync(expectedItems, applyFix ? expectedItem : null, verifyNotShowing,
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
    public static async Task<bool?> CodeActionsAsync(
        this ITextViewWindowVerifierInProcess textViewWindowVerifier,
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

        var workspace = await textViewWindowVerifier.TestServices.Shell.GetComponentModelServiceAsync<VisualStudioWorkspace>(cancellationToken);
        using var workspaceEventRestorer = WithWorkspaceChangedHandler(workspace, WorkspaceChangedHandler);

        await textViewWindowVerifier.TestServices.Editor.ShowLightBulbAsync(cancellationToken);

        if (verifyNotShowing)
        {
            await textViewWindowVerifier.CodeActionsNotShowingAsync(cancellationToken);
            return null;
        }

        var actions = await textViewWindowVerifier.TestServices.Editor.GetLightBulbActionsAsync(cancellationToken);

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

            var result = await textViewWindowVerifier.TestServices.Editor.ApplyLightBulbActionAsync(applyFix, fixAllScope, blockUntilComplete, cancellationToken);

            if (blockUntilComplete)
            {
                // wait for action to complete
                await textViewWindowVerifier.TestServices.Workspace.WaitForAllAsyncOperationsAsync(
                    [
                        FeatureAttribute.Workspace,
                        FeatureAttribute.LightBulb,
                        FeatureAttribute.Rename,
                    ],
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

    public static async Task CodeActionsNotShowingAsync(this ITextViewWindowVerifierInProcess textViewWindowVerifier, CancellationToken cancellationToken)
    {
        if (await textViewWindowVerifier.TextViewWindow.IsLightBulbSessionExpandedAsync(cancellationToken))
        {
            throw new InvalidOperationException("Expected no light bulb session, but one was found.");
        }
    }

    public static async Task CurrentTokenTypeAsync(this ITextViewWindowVerifierInProcess textViewWindowVerifier, string tokenType, CancellationToken cancellationToken)
    {
        await textViewWindowVerifier.TestServices.Workspace.WaitForAllAsyncOperationsAsync(
            [FeatureAttribute.SolutionCrawlerLegacy, FeatureAttribute.DiagnosticService, FeatureAttribute.Classification],
            cancellationToken);

        var actualTokenTypes = await textViewWindowVerifier.TestServices.Editor.GetCurrentClassificationsAsync(cancellationToken);
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
        public List<string> Messages { get; } = [];

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
