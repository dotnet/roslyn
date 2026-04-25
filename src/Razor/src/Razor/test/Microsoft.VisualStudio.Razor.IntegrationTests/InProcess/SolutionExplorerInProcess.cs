// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.Test.Common;
using Microsoft.VisualStudio.Razor.IntegrationTests;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.TextManager.Interop;
using Microsoft.VisualStudio.Threading;
using Xunit;
using Task = System.Threading.Tasks.Task;

namespace Microsoft.VisualStudio.Extensibility.Testing;

internal partial class SolutionExplorerInProcess
{
    public Task AddProjectAsync(string projectName, string projectTemplate, string languageName, CancellationToken cancellationToken)
        => AddProjectAsync(projectName, projectTemplate, groupId: null, templateId: null, languageName, cancellationToken);

    public async Task AddProjectAsync(string projectName, string projectTemplate, string? groupId, string? templateId, string languageName, CancellationToken cancellationToken)
    {
        await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

        var projectPath = Path.Combine(await GetDirectoryNameAsync(cancellationToken), projectName);
        var projectTemplatePath = await GetProjectTemplatePathAsync(projectTemplate, ConvertLanguageName(languageName), cancellationToken);
        var solution = await GetRequiredGlobalServiceAsync<SVsSolution, IVsSolution6>(cancellationToken);

        var args = new List<object>();
        if (groupId is not null)
            args.Add($"$groupid$={groupId}");
        if (groupId is not null)
            args.Add($"$templateid$={templateId}");

        ErrorHandler.ThrowOnFailure(solution.AddNewProjectFromTemplate(projectTemplatePath, args.Any() ? args.ToArray() : null, null, projectPath, projectName, null, out _));
    }

    public async Task CloseSolutionAndWaitAsync(CancellationToken cancellationToken)
    {
        await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

        await CloseSolutionAsync(cancellationToken);

        // Yes, this is annoying, but it seems to mitigate the dual-activate issue that the language client has
        // when closing and reopening solutions rapidly.
        await Task.Delay(1000, cancellationToken);
    }

    public async Task OpenSolutionAsync(string solutionFileName, CancellationToken cancellationToken)
    {
        await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

        var dte = await GetRequiredGlobalServiceAsync<SDTE, EnvDTE.DTE>(cancellationToken);

        dte.Solution.Open(solutionFileName);
    }

    public async Task OpenFileAsync(string projectName, string relativeFilePath, CancellationToken cancellationToken)
    {
        await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

        var filePath = await GetAbsolutePathForProjectRelativeFilePathAsync(projectName, relativeFilePath, cancellationToken);
        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException(filePath);
        }

        VsShellUtilities.OpenDocument(ServiceProvider.GlobalProvider, filePath, VSConstants.LOGVIEWID.Code_guid, out _, out _, out _, out var view);

        // Reliably set focus using NavigateToLineAndColumn
        var textManager = await GetRequiredGlobalServiceAsync<SVsTextManager, IVsTextManager>(cancellationToken);
        ErrorHandler.ThrowOnFailure(view.GetBuffer(out var textLines));
        ErrorHandler.ThrowOnFailure(view.GetCaretPos(out var line, out var column));
        ErrorHandler.ThrowOnFailure(textManager.NavigateToLineAndColumn(textLines, VSConstants.LOGVIEWID.Code_guid, line, column, line, column));

        var fileExtension = Path.GetExtension(filePath);
        if (fileExtension.Equals(".razor", StringComparison.OrdinalIgnoreCase) || fileExtension.Equals(".cshtml", StringComparison.OrdinalIgnoreCase))
        {
            await TestServices.RazorProjectSystem.WaitForHtmlVirtualDocumentAsync(filePath, cancellationToken);
        }
    }

    /// <summary>
    /// Add new file to project.
    /// </summary>
    /// <param name="projectName">The project that contains the file.</param>
    /// <param name="fileName">The name of the file to add.</param>
    /// <param name="contents">The contents of the file to overwrite. An empty file is create if null is passed.</param>
    /// <param name="open">Whether to open the file after it has been updated.</param>
    /// <param name="cancellationToken"></param>
    public async Task<int> AddFileAsync(string projectName, string fileName, TestCode? contents = null, bool open = false, CancellationToken cancellationToken = default)
    {
        await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

        var project = await GetProjectAsync(projectName, cancellationToken);
        var projectDirectory = Path.GetDirectoryName(project.FullName);
        var filePath = Path.Combine(projectDirectory, fileName);
        var directoryPath = Path.GetDirectoryName(filePath);
        Directory.CreateDirectory(directoryPath);

        if (contents is { Text: var text })
        {
            File.WriteAllText(filePath, text);
        }
        else if (!File.Exists(filePath))
        {
            File.Create(filePath).Dispose();
        }

        _ = project.ProjectItems.AddFromFile(filePath);

        if (open)
        {
            await OpenFileAsync(projectName, fileName, cancellationToken);
        }

        if (contents is { Positions: [var position] })
        {
            return position;
        }

        return 0;
    }

    /// <returns>
    /// The summary line for the build, which generally looks something like this:
    ///
    /// <code>
    /// ========== Build: 1 succeeded, 0 failed, 0 up-to-date, 0 skipped ==========
    /// </code>
    /// </returns>
    public async Task<string> BuildSolutionAndWaitAsync(CancellationToken cancellationToken)
    {
        await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

        var buildOutputWindowPane = await GetBuildOutputWindowPaneAsync(cancellationToken);
        buildOutputWindowPane.Clear();

        var buildManager = await GetRequiredGlobalServiceAsync<SVsSolutionBuildManager, IVsSolutionBuildManager2>(cancellationToken);
        using var solutionEvents = new UpdateSolutionEvents(buildManager);
        var buildCompleteTaskCompletionSource = new TaskCompletionSource<bool>();

        void HandleUpdateSolutionDone() => buildCompleteTaskCompletionSource.SetResult(true);
        solutionEvents.OnUpdateSolutionDone += HandleUpdateSolutionDone;
        try
        {
            await TestServices.Shell.ExecuteCommandAsync(VSConstants.VSStd97CmdID.BuildSln, cancellationToken);

            await buildCompleteTaskCompletionSource.Task;
        }
        finally
        {
            solutionEvents.OnUpdateSolutionDone -= HandleUpdateSolutionDone;
        }

        // Force the error list to update
        ErrorHandler.ThrowOnFailure(buildOutputWindowPane.FlushToTaskList());

        var textView = (IVsTextView)buildOutputWindowPane;
        var wpfTextViewHost = await textView.GetTextViewHostAsync(JoinableTaskFactory, cancellationToken);
        var lines = wpfTextViewHost.TextView.TextViewLines;
        if (lines.Count < 1)
        {
            return string.Empty;
        }

        // Find the build summary line
        for (var index = lines.Count - 1; index >= 0; index--)
        {
            var lineText = lines[index].Extent.GetText();
            if (lineText.StartsWith("========== Build:"))
            {
                return lineText;
            }
        }

        return string.Empty;
    }

    public async Task<IVsOutputWindowPane> GetBuildOutputWindowPaneAsync(CancellationToken cancellationToken)
    {
        await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

        var outputWindow = await GetRequiredGlobalServiceAsync<SVsOutputWindow, IVsOutputWindow>(cancellationToken);
        ErrorHandler.ThrowOnFailure(outputWindow.GetPane(VSConstants.OutputWindowPaneGuid.BuildOutputPane_guid, out var pane));
        return pane;
    }

    public async Task CreateSolutionAsync(string solutionPath, string solutionName, CancellationToken cancellationToken)
    {
        await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

        await CloseSolutionAndWaitAsync(cancellationToken);

        var solutionFileName = Path.ChangeExtension(solutionName, ".sln");
        Directory.CreateDirectory(solutionPath);

        // Make sure the shell debugger package is loaded so it doesn't try to load during the synchronous portion
        // of IVsSolution.CreateSolution.
        //
        // TODO: Identify the correct tracking bug
        _ = await GetRequiredGlobalServiceAsync<SVsShellDebugger, IVsDebugger>(cancellationToken);

        var solution = await GetRequiredGlobalServiceAsync<SVsSolution, IVsSolution>(cancellationToken);
        ErrorHandler.ThrowOnFailure(solution.CreateSolution(solutionPath, solutionFileName, (uint)__VSCREATESOLUTIONFLAGS.CSF_SILENT));
        ErrorHandler.ThrowOnFailure(solution.SaveSolutionElement((uint)__VSSLNSAVEOPTIONS.SLNSAVEOPT_ForceSave, null, 0));
    }

    private async Task<string> GetProjectTemplatePathAsync(string projectTemplate, string languageName, CancellationToken cancellationToken)
    {
        await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

        var dte = await GetRequiredGlobalServiceAsync<SDTE, EnvDTE.DTE>(cancellationToken);
        var solution = (EnvDTE80.Solution2)dte.Solution;

        if (string.Equals(languageName, "csharp", StringComparison.OrdinalIgnoreCase)
            && GetCSharpProjectTemplates().TryGetValue(projectTemplate, out var csharpProjectTemplate))
        {
            return solution.GetProjectTemplate(csharpProjectTemplate, languageName);
        }

        throw new NotImplementedException();

        static ImmutableDictionary<string, string> GetCSharpProjectTemplates()
        {
            var builder = ImmutableDictionary.CreateBuilder<string, string>();
            builder[WellKnownProjectTemplates.BlazorProject] = "BlazorTemplate";
            return builder.ToImmutable();
        }
    }

    private static string ConvertLanguageName(string languageName)
    {
        return languageName switch
        {
            LanguageNames.CSharp => "CSharp",
            LanguageNames.Razor => "CSharp",
            _ => throw new ArgumentException($"'{languageName}' is not supported.", nameof(languageName)),
        };
    }

    public async Task<string> GetAbsolutePathForProjectRelativeFilePathAsync(string projectName, string relativeFilePath, CancellationToken cancellationToken)
    {
        var projectFileName = await GetProjectFileNameAsync(projectName, cancellationToken);
        var projectPath = Path.GetDirectoryName(projectFileName);
        return Path.Combine(projectPath, relativeFilePath);
    }

    public async Task<string> GetProjectFileNameAsync(string projectName, CancellationToken cancellationToken)
    {
        await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

        var dte = await GetRequiredGlobalServiceAsync<SDTE, EnvDTE.DTE>(cancellationToken);
        var solution = dte.Solution;
        Assumes.Present(solution);

        var project = solution.Projects.Cast<EnvDTE.Project>().FirstOrDefault(x => x.Name == projectName);
        if (project is null)
        {
            Assert.Fail($"{projectName} doesn't exist, had {string.Join(",", solution.Projects.Cast<EnvDTE.Project>().Select(p => p.Name))}");
            throw new NotImplementedException("Prevent null fallthrough");
        }

        Assert.NotNull(project);
        return project.FullName;
    }

    public async Task<string> GetDirectoryNameAsync(CancellationToken cancellationToken)
    {
        await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

        var solution = await GetRequiredGlobalServiceAsync<SVsSolution, IVsSolution>(cancellationToken);
        ErrorHandler.ThrowOnFailure(solution.GetSolutionInfo(out _, out var solutionFileFullPath, out _));
        if (string.IsNullOrEmpty(solutionFileFullPath))
        {
            throw new InvalidOperationException();
        }

        return Path.GetDirectoryName(solutionFileFullPath);
    }

    private async Task<EnvDTE.Project> GetProjectAsync(string nameOrFileName, CancellationToken cancellationToken)
    {
        await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

        var dte = await GetRequiredGlobalServiceAsync<SDTE, EnvDTE.DTE>(cancellationToken);
        var solution = (EnvDTE80.Solution2)dte.Solution;
        return solution.Projects.OfType<EnvDTE.Project>().First(
            project =>
            {
                ThreadHelper.ThrowIfNotOnUIThread();
                return string.Equals(project.FileName, nameOrFileName, StringComparison.OrdinalIgnoreCase)
                    || string.Equals(project.Name, nameOrFileName, StringComparison.OrdinalIgnoreCase);
            });
    }

    internal sealed class UpdateSolutionEvents : IVsUpdateSolutionEvents, IDisposable
    {
        private uint _cookie;
        private readonly IVsSolutionBuildManager2 _solutionBuildManager;

        public event Action? OnUpdateSolutionDone;

        internal UpdateSolutionEvents(IVsSolutionBuildManager2 solutionBuildManager)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            _solutionBuildManager = solutionBuildManager;
            ErrorHandler.ThrowOnFailure(solutionBuildManager.AdviseUpdateSolutionEvents(this, out _cookie));
        }

        int IVsUpdateSolutionEvents.UpdateSolution_Begin(ref int pfCancelUpdate) => VSConstants.E_NOTIMPL;
        int IVsUpdateSolutionEvents.UpdateSolution_StartUpdate(ref int pfCancelUpdate) => VSConstants.E_NOTIMPL;
        int IVsUpdateSolutionEvents.UpdateSolution_Cancel() => VSConstants.E_NOTIMPL;
        int IVsUpdateSolutionEvents.OnActiveProjectCfgChange(IVsHierarchy pIVsHierarchy) => VSConstants.E_NOTIMPL;

        int IVsUpdateSolutionEvents.UpdateSolution_Done(int fSucceeded, int fModified, int fCancelCommand)
        {
            OnUpdateSolutionDone?.Invoke();
            return 0;
        }

        void IDisposable.Dispose()
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            OnUpdateSolutionDone = null;

            if (_cookie != 0)
            {
                var tempCookie = _cookie;
                _cookie = 0;
                ErrorHandler.ThrowOnFailure(_solutionBuildManager.UnadviseUpdateSolutionEvents(tempCookie));
            }
        }
    }
}
