// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Razor.Logging;
using Microsoft.CodeAnalysis.Razor.Workspaces.Settings;
using Microsoft.VisualStudio.Razor.IntegrationTests.Extensions;
using Microsoft.VisualStudio.Razor.IntegrationTests.Logging;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.TextManager.Interop;
using Xunit.Abstractions;

namespace Microsoft.VisualStudio.Extensibility.Testing;

[TestService]
internal partial class OutputInProcess
{
    private const string RazorPaneName = "Razor Logger Output";

    private IntegrationTestOutputLoggerProvider? _testLoggerProvider;

    public async Task<ILogger> SetupIntegrationTestLoggerAsync(ITestOutputHelper testOutputHelper, CancellationToken cancellationToken)
    {
        // Make sure we log as much as possible for integration tests
        var clientSettingsManager = await TestServices.Shell.GetComponentModelServiceAsync<IClientSettingsManager>(cancellationToken);
        clientSettingsManager.Update(clientSettingsManager.GetClientSettings().AdvancedSettings with { LogLevel = LogLevel.Trace });

        var loggerFactory = await TestServices.Shell.GetComponentModelServiceAsync<ILoggerFactory>(cancellationToken);

        // We can't remove logging providers, so we just keep track of ours so we can make sure it points to the right test output helper
        if (_testLoggerProvider is null)
        {
            _testLoggerProvider = new IntegrationTestOutputLoggerProvider(testOutputHelper);
            loggerFactory.AddLoggerProvider(_testLoggerProvider);
        }
        else
        {
            _testLoggerProvider.SetOutput(testOutputHelper);
        }

        return loggerFactory.GetOrCreateLogger(GetType().Name);
    }

    public void ClearIntegrationTestLogger()
    {
        _testLoggerProvider?.SetOutput(null);
    }

    public async Task<bool> HasErrorsAsync(CancellationToken cancellationToken)
    {
        var content = await GetRazorOutputPaneContentAsync(cancellationToken);

        return content is null || content.Contains("Error");
    }

    /// <summary>
    /// This method returns the current content of the "Razor Language Server Client" output pane.
    /// </summary>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The contents of the RLSC output pane.</returns>
    public async Task<string?> GetRazorOutputPaneContentAsync(CancellationToken cancellationToken)
    {
        var outputPaneTextView = await GetOutputPaneTextViewAsync(RazorPaneName, cancellationToken);

        if (outputPaneTextView is null)
        {
            return null;
        }

        return await outputPaneTextView.GetContentAsync(JoinableTaskFactory, cancellationToken);
    }

    private async Task<IVsTextView?> GetOutputPaneTextViewAsync(string paneName, CancellationToken cancellationToken)
    {
        var sVSOutputWindow = await TestServices.Shell.GetRequiredGlobalServiceAsync<SVsOutputWindow, IVsOutputWindow>(cancellationToken);
        var extensibleObject = await TestServices.Shell.GetRequiredGlobalServiceAsync<SVsOutputWindow, IVsExtensibleObject>(cancellationToken);

        // The null propName gives use the OutputWindow object
        ErrorHandler.ThrowOnFailure(extensibleObject.GetAutomationObject(pszPropName: null, out var outputWindowObj));
        var outputWindow = (EnvDTE.OutputWindow)outputWindowObj;

        // This is a public entry point to COutputWindow::GetPaneByName
        EnvDTE.OutputWindowPane? pane = null;
        try
        {
            pane = outputWindow.OutputWindowPanes.Item(paneName);
        }
        catch (ArgumentException)
        {
            return null;
        }

        var textView = OutputWindowPaneToIVsTextView(pane, sVSOutputWindow);

        return textView;

        static IVsTextView OutputWindowPaneToIVsTextView(EnvDTE.OutputWindowPane outputWindowPane, IVsOutputWindow sVsOutputWindow)
        {
            var guid = Guid.Parse(outputWindowPane.Guid);
            ErrorHandler.ThrowOnFailure(sVsOutputWindow.GetPane(guid, out var result));

            if (result is not IVsTextView textView)
            {
                throw new InvalidOperationException($"{nameof(IVsOutputWindowPane)} should implement {nameof(IVsTextView)}");
            }

            return textView;
        }
    }
}
