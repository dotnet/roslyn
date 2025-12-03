// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.VisualStudio.Editor;
using Microsoft.VisualStudio.IntegrationTest.Utilities;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.TextManager.Interop;
using Microsoft.VisualStudio.Threading;
using NuGet.SolutionRestoreManager;
using Roslyn.Utilities;
using Roslyn.VisualStudio.IntegrationTests.InProcess;
using Reference = VSLangProj.Reference;
using VSProject = VSLangProj.VSProject;
using VSProject3 = VSLangProj140.VSProject3;

namespace Microsoft.VisualStudio.Extensibility.Testing;

internal sealed partial class SolutionExplorerInProcess
{
    public async Task CreateSolutionAsync(string solutionName, CancellationToken cancellationToken)
    {
        await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

        Contract.ThrowIfTrue(await IsSolutionOpenAsync(cancellationToken));

        var solutionPath = CreateTemporaryPath();
        await CreateSolutionAsync(solutionPath, solutionName, cancellationToken);
    }

    public async Task CreateSolutionAsync(string solutionName, XElement solutionElement, CancellationToken cancellationToken)
    {
        if (solutionElement.Name != "Solution")
        {
            throw new ArgumentException(nameof(solutionElement));
        }

        await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

        await CreateSolutionAsync(solutionName, cancellationToken);

        foreach (var projectElement in solutionElement.Elements("Project"))
        {
            await CreateProjectAsync(projectElement, cancellationToken);
        }

        foreach (var projectElement in solutionElement.Elements("Project"))
        {
            var projectReferences = projectElement.Attribute("ProjectReferences")?.Value;
            if (projectReferences != null)
            {
                var projectName = projectElement.Attribute("ProjectName").Value;
                foreach (var projectReference in projectReferences.Split(';'))
                {
                    await AddProjectReferenceAsync(projectName, projectReference, cancellationToken);
                }
            }
        }
    }

    private async Task CreateProjectAsync(XElement projectElement, CancellationToken cancellationToken)
    {
        const string language = "Language";
        const string name = "ProjectName";
        const string template = "ProjectTemplate";
        var languageName = projectElement.Attribute(language)?.Value
            ?? throw new ArgumentException($"You must specify an attribute called '{language}' on a project element.");
        var projectName = projectElement.Attribute(name)?.Value
            ?? throw new ArgumentException($"You must specify an attribute called '{name}' on a project element.");
        var projectTemplate = projectElement.Attribute(template)?.Value
            ?? throw new ArgumentException($"You must specify an attribute called '{template}' on a project element.");

        var projectPath = Path.Combine(await GetDirectoryNameAsync(cancellationToken), projectName);
        var projectTemplatePath = await GetProjectTemplatePathAsync(projectTemplate, ConvertLanguageName(languageName), cancellationToken);

        var solution = (await GetRequiredGlobalServiceAsync<SDTE, EnvDTE.DTE>(cancellationToken)).Solution;
        Assumes.Present(solution);

        solution.AddFromTemplate(projectTemplatePath, projectPath, projectName, Exclusive: false);
        foreach (var documentElement in projectElement.Elements("Document"))
        {
            var fileName = documentElement.Attribute("FileName").Value;
            await UpdateOrAddFileAsync(projectName, fileName, contents: documentElement.Value, cancellationToken: cancellationToken);
        }
    }

    public async Task SetProjectInferAsync(string projectName, bool value, CancellationToken cancellationToken)
    {
        var convertedValue = value ? 1 : 0;
        var project = await GetProjectAsync(projectName, cancellationToken);
        project.Properties.Item("OptionInfer").Value = convertedValue;
        await TestServices.Workspace.WaitForAllAsyncOperationsAsync([FeatureAttribute.Workspace], cancellationToken);
    }

    public async Task AddProjectReferenceAsync(string projectName, string projectToReferenceName, CancellationToken cancellationToken)
    {
        await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

        var project = await GetProjectAsync(projectName, cancellationToken);
        var projectToReference = await GetProjectAsync(projectToReferenceName, cancellationToken);
        ((VSProject)project.Object).References.AddProject(projectToReference);
    }

    public Task RemoveProjectReferenceAsync(string projectName, string projectReferenceName, CancellationToken cancellationToken)
    {
        return RemoveReference(projectName, projectReferenceName, cancellationToken);
    }

    public async Task AddAnalyzerReferenceAsync(string projectName, string filePath, CancellationToken cancellationToken)
    {
        await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

        var project = await GetProjectAsync(projectName, cancellationToken);
        ((VSProject3)project.Object).AnalyzerReferences.Add(filePath);
    }

    public async Task AddDllReferenceAsync(string projectName, string filePath, CancellationToken cancellationToken)
    {
        await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

        var project = await GetProjectAsync(projectName, cancellationToken);
        ((VSProject)project.Object).References.Add(filePath);
    }

    public Task RemoveDllReferenceAsync(string projectName, string assemblyName, CancellationToken cancellationToken)
    {
        return RemoveReference(projectName, assemblyName, cancellationToken);
    }

    public async Task AddMetadataReferenceAsync(string assemblyName, string projectName, CancellationToken cancellationToken)
    {
        await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

        var project = await GetProjectAsync(projectName, cancellationToken);
        var vsproject = ((VSProject)project.Object);
        vsproject.References.Add(assemblyName);
    }

    private async Task RemoveReference(string projectName, string referenceName, CancellationToken cancellationToken)
    {
        await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

        var project = await GetProjectAsync(projectName, cancellationToken);
        var vsProject = (VSProject)project.Object;
        var referenceCount = vsProject.References.Count;
        // The index for references starts at 1
        for (var i = 1; i <= referenceCount; i++)
        {
            var reference = vsProject.References.Item(i);
            var name = reference?.Name;
            if (reference != null && name == referenceName)
            {
                reference.Remove();
                return;
            }
        }

        throw new InvalidOperationException($"Could not find reference {referenceName} to remove");
    }

    private async Task CreateSolutionAsync(string solutionPath, string solutionName, CancellationToken cancellationToken)
    {
        await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

        await CloseSolutionAsync(cancellationToken);

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

    public async Task<ImmutableArray<(string name, string version, string publicKeyToken)>> GetAssemblyReferencesAsync(string projectName, CancellationToken cancellationToken)
    {
        await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

        var project = await GetProjectAsync(projectName, cancellationToken);
        var references = ((VSProject)project.Object).References.Cast<Reference>().SelectAsArray(
            predicate: x => x.SourceProject == null,
            selector: x => (x.Name, x.Version, x.PublicKeyToken));
        return references;
    }

    public async Task<ImmutableArray<string>> GetProjectReferencesAsync(string projectName, CancellationToken cancellationToken)
    {
        await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

        var project = await GetProjectAsync(projectName, cancellationToken);
        var references = ((VSProject)project.Object).References.Cast<Reference>().SelectAsArray(x => x.SourceProject != null, x => x.Name);
        return references;
    }

    public Task AddProjectAsync(string projectName, string projectTemplate, string languageName, CancellationToken cancellationToken)
        => AddProjectAsync(projectName, projectTemplate, templateGroupId: null, languageName, cancellationToken);

    public async Task AddProjectAsync(string projectName, string projectTemplate, string? templateGroupId, string languageName, CancellationToken cancellationToken)
    {
        await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

        var projectPath = Path.Combine(await GetDirectoryNameAsync(cancellationToken), projectName);
        var projectTemplatePath = await GetProjectTemplatePathAsync(projectTemplate, ConvertLanguageName(languageName), cancellationToken);
        var solution = await GetRequiredGlobalServiceAsync<SVsSolution, IVsSolution6>(cancellationToken);

        var args = new List<object>();
        if (templateGroupId is not null)
            args.Add($"$groupid$={templateGroupId}");

        ErrorHandler.ThrowOnFailure(solution.AddNewProjectFromTemplate(projectTemplatePath, args.Any() ? args.ToArray() : null, null, projectPath, projectName, null, out _));
    }

    public async Task AddCustomProjectAsync(string projectName, string projectFileExtension, string projectFileContent, CancellationToken cancellationToken)
    {
        await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

        var projectPath = Path.Combine(await GetDirectoryNameAsync(cancellationToken), projectName);
        Directory.CreateDirectory(projectPath);

        var projectFilePath = Path.Combine(projectPath, projectName + projectFileExtension);
        File.WriteAllText(projectFilePath, projectFileContent);

        var solution = await GetRequiredGlobalServiceAsync<SVsSolution, IVsSolution6>(cancellationToken);
        ErrorHandler.ThrowOnFailure(solution.AddExistingProject(projectFilePath, pParent: null, out _));
    }

    public async Task SaveFileAsync(string projectName, string relativeFilePath, CancellationToken cancellationToken)
    {
        await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

        var dte = await GetRequiredGlobalServiceAsync<SDTE, EnvDTE.DTE>(cancellationToken);
        var filePath = await GetAbsolutePathForProjectRelativeFilePathAsync(projectName, relativeFilePath, cancellationToken);

        SaveFileWithExtraValidation(await GetOpenDocumentAsync(JoinableTaskFactory, dte, filePath, cancellationToken));

        static async Task<EnvDTE.Document> GetOpenDocumentAsync(JoinableTaskFactory joinableTaskFactory, EnvDTE.DTE dte, string filePath, CancellationToken cancellationToken)
        {
            await joinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

            var documents = dte.Documents.Cast<EnvDTE.Document>();
            var document = documents.SingleOrDefault(d => d.FullName == filePath);
            return document ?? throw new InvalidOperationException($"Open document '{filePath} could not be found. Available documents: {string.Join(", ", documents.Select(x => x.FullName))}.");
        }

        static void SaveFileWithExtraValidation(EnvDTE.Document document)
        {
            var textDocument = (EnvDTE.TextDocument)document.Object(nameof(EnvDTE.TextDocument));
            var currentTextInDocument = textDocument.StartPoint.CreateEditPoint().GetText(textDocument.EndPoint);
            var fullPath = document.FullName;
            document.Save();
            if (File.ReadAllText(fullPath) != currentTextInDocument)
            {
                throw new InvalidOperationException("The text that we thought we were saving isn't what we saved!");
            }
        }
    }

    public async Task RestoreNuGetPackagesAsync(CancellationToken cancellationToken)
    {
        await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

        var dte = await GetRequiredGlobalServiceAsync<SDTE, EnvDTE.DTE>(cancellationToken);
        var solution = (EnvDTE80.Solution2)dte.Solution;
        foreach (var project in solution.Projects.OfType<EnvDTE.Project>())
        {
            await RestoreNuGetPackagesAsync(project.FullName, cancellationToken);
        }
    }

    public async Task RestoreNuGetPackagesAsync(string projectName, CancellationToken cancellationToken)
    {
        await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

        await TestServices.Workspace.WaitForProjectSystemAsync(cancellationToken);

        var solutionRestoreService = await GetComponentModelServiceAsync<IVsSolutionRestoreService>(cancellationToken);
        await solutionRestoreService.CurrentRestoreOperation;

        var projectFullPath = (await GetProjectAsync(projectName, cancellationToken)).FullName;
        var solutionRestoreStatusProvider = await GetComponentModelServiceAsync<IVsSolutionRestoreStatusProvider>(cancellationToken);
        if (await solutionRestoreStatusProvider.IsRestoreCompleteAsync(cancellationToken))
        {
            return;
        }

        // Check IsRestoreCompleteAsync until it returns true (this stops the retry because true != default(bool))
        await Helper.RetryAsync(
            async cancellationToken =>
            {
                try
                {
                    return await solutionRestoreStatusProvider.IsRestoreCompleteAsync(cancellationToken);
                }
                catch (NullReferenceException)
                {
                    // 🤮 Workaround for NuGet package restore throwing exceptions
                    return false;
                }
            },
            TimeSpan.FromMilliseconds(50),
            cancellationToken);
    }

    public async Task SelectItemAsync(string itemName, CancellationToken cancellationToken)
    {
        await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

        var dte = await GetRequiredGlobalServiceAsync<SDTE, EnvDTE80.DTE2>(cancellationToken);
        var solutionExplorer = dte.ToolWindows.SolutionExplorer;

        var item = FindFirstItemRecursively(solutionExplorer.UIHierarchyItems, itemName);
        Contract.ThrowIfNull(item);

        item.Select(EnvDTE.vsUISelectionType.vsUISelectionTypeSelect);
        solutionExplorer.Parent.Activate();
    }

    private static EnvDTE.UIHierarchyItem? FindFirstItemRecursively(
        EnvDTE.UIHierarchyItems currentItems,
        string itemName)
    {
        if (currentItems == null)
        {
            return null;
        }

        foreach (var item in currentItems.Cast<EnvDTE.UIHierarchyItem>())
        {
            if (item.Name == itemName)
            {
                return item;
            }

            var result = FindFirstItemRecursively(item.UIHierarchyItems, itemName);
            if (result != null)
            {
                return result;
            }
        }

        return null;
    }

    public async Task SaveAllAsync(CancellationToken cancellationToken)
    {
        await TestServices.Shell.ExecuteCommandAsync(VSConstants.VSStd97CmdID.SaveSolution, cancellationToken);

        // Wait for async save operations to complete before proceeding
        await TestServices.Workspace.WaitForAllAsyncOperationsAsync([FeatureAttribute.Workspace], cancellationToken);

        // Verify documents are truly saved after a Save Solution operation
        await TestServices.SolutionExplorerVerifier.AllDocumentsAreSavedAsync(cancellationToken);
    }

    public async Task OpenFileWithDesignerAsync(string projectName, string relativeFilePath, CancellationToken cancellationToken)
    {
        await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

        var filePath = await GetAbsolutePathForProjectRelativeFilePathAsync(projectName, relativeFilePath, cancellationToken);
        VsShellUtilities.OpenDocument(ServiceProvider.GlobalProvider, filePath, VSConstants.LOGVIEWID.Designer_guid, out _, out _, out var windowFrame, out _);
        ErrorHandler.ThrowOnFailure(windowFrame.Show());
    }

    public async Task OpenFileAsync(string projectName, string relativeFilePath, CancellationToken cancellationToken)
    {
        await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

        var filePath = await GetAbsolutePathForProjectRelativeFilePathAsync(projectName, relativeFilePath, cancellationToken);
        VsShellUtilities.OpenDocument(ServiceProvider.GlobalProvider, filePath, VSConstants.LOGVIEWID.Code_guid, out _, out _, out _, out var view);

        // Reliably set focus using NavigateToLineAndColumn
        var textManager = await GetRequiredGlobalServiceAsync<SVsTextManager, IVsTextManager>(cancellationToken);
        ErrorHandler.ThrowOnFailure(view.GetBuffer(out var textLines));
        ErrorHandler.ThrowOnFailure(view.GetCaretPos(out var line, out var column));
        ErrorHandler.ThrowOnFailure(textManager.NavigateToLineAndColumn(textLines, VSConstants.LOGVIEWID.Code_guid, line, column, line, column));
    }

    public async Task CloseActiveWindow(CancellationToken cancellationToken)
    {
        await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

        var monitorSelection = await GetRequiredGlobalServiceAsync<SVsShellMonitorSelection, IVsMonitorSelection>(cancellationToken);
        ErrorHandler.ThrowOnFailure(monitorSelection.GetCurrentElementValue((uint)VSConstants.VSSELELEMID.SEID_WindowFrame, out var windowFrameObj));
        var windowFrame = (IVsWindowFrame)windowFrameObj;

        ErrorHandler.ThrowOnFailure(windowFrame.CloseFrame((uint)__FRAMECLOSE.FRAMECLOSE_NoSave));
    }

    public Task CloseDesignerFileAsync(string projectName, string relativeFilePath, bool saveFile, CancellationToken cancellationToken)
        => CloseFileAsync(projectName, relativeFilePath, VSConstants.LOGVIEWID.Designer_guid, saveFile, cancellationToken);

    public Task CloseCodeFileAsync(string projectName, string relativeFilePath, bool saveFile, CancellationToken cancellationToken)
        => CloseFileAsync(projectName, relativeFilePath, VSConstants.LOGVIEWID.Code_guid, saveFile, cancellationToken);

    private async Task CloseFileAsync(string projectName, string relativeFilePath, Guid logicalView, bool saveFile, CancellationToken cancellationToken)
    {
        await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

        var filePath = await GetAbsolutePathForProjectRelativeFilePathAsync(projectName, relativeFilePath, cancellationToken);
        if (!VsShellUtilities.IsDocumentOpen(ServiceProvider.GlobalProvider, filePath, logicalView, out _, out _, out var windowFrame))
        {
            throw new InvalidOperationException($"File '{filePath}' is not open in logical view '{logicalView}'");
        }

        var frameClose = saveFile ? __FRAMECLOSE.FRAMECLOSE_SaveIfDirty : __FRAMECLOSE.FRAMECLOSE_NoSave;
        ErrorHandler.ThrowOnFailure(windowFrame.CloseFrame((uint)frameClose));
    }

    /// <summary>
    /// Update the given file if it already exists in the project, otherwise add a new file to the project.
    /// </summary>
    /// <param name="projectName">The project that contains the file.</param>
    /// <param name="fileName">The name of the file to update or add.</param>
    /// <param name="contents">The contents of the file to overwrite if the file already exists or set if the file it created. Empty string is used if null is passed.</param>
    /// <param name="open">Whether to open the file after it has been updated/created.</param>
    public async Task UpdateOrAddFileAsync(string projectName, string fileName, string? contents = null, bool open = false, CancellationToken cancellationToken = default)
    {
        await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

        var project = await GetProjectAsync(projectName, cancellationToken);
        if (project.ProjectItems.Cast<EnvDTE.ProjectItem>().Any(x => x.Name == fileName))
        {
            await UpdateFileAsync(projectName, fileName, contents, open, cancellationToken);
        }
        else
        {
            await AddFileAsync(projectName, fileName, contents, open, cancellationToken);
        }
    }

    /// <summary>
    /// Update the given file to have the contents given.
    /// </summary>
    /// <param name="projectName">The project that contains the file.</param>
    /// <param name="fileName">The name of the file to update or add.</param>
    /// <param name="contents">The contents of the file to overwrite. Empty string is used if null is passed.</param>
    /// <param name="open">Whether to open the file after it has been updated.</param>
    public async Task UpdateFileAsync(string projectName, string fileName, string? contents = null, bool open = false, CancellationToken cancellationToken = default)
    {
        async Task SetTextAsync(string text, CancellationToken cancellationToken)
        {
            await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

            // The active text view might not have finished composing yet, waiting for the application to 'idle'
            // means that it is done pumping messages (including WM_PAINT) and the window should return the correct text view
            await WaitForApplicationIdleAsync(cancellationToken);

            var vsTextManager = await GetRequiredGlobalServiceAsync<SVsTextManager, IVsTextManager>(cancellationToken);
            var hresult = vsTextManager.GetActiveView(fMustHaveFocus: 1, pBuffer: null, ppView: out var vsTextView);
            Marshal.ThrowExceptionForHR(hresult);
            var activeVsTextView = (IVsUserData)vsTextView;

            hresult = activeVsTextView.GetData(DefGuidList.guidIWpfTextViewHost, out var wpfTextViewHost);
            Marshal.ThrowExceptionForHR(hresult);

            var view = ((IWpfTextViewHost)wpfTextViewHost).TextView;
            var textSnapshot = view.TextSnapshot;
            var replacementSpan = new SnapshotSpan(textSnapshot, 0, textSnapshot.Length);
            view.TextBuffer.Replace(replacementSpan, text);
        }

        await OpenFileAsync(projectName, fileName, cancellationToken);
        await SetTextAsync(contents ?? string.Empty, cancellationToken);
        await CloseCodeFileAsync(projectName, fileName, saveFile: true, cancellationToken);
        if (open)
        {
            await OpenFileAsync(projectName, fileName, cancellationToken);
        }
    }

    /// <summary>
    /// Add new file to project.
    /// </summary>
    /// <param name="projectName">The project that contains the file.</param>
    /// <param name="fileName">The name of the file to add.</param>
    /// <param name="contents">The contents of the file to overwrite. An empty file is create if null is passed.</param>
    /// <param name="open">Whether to open the file after it has been updated.</param>
    public async Task AddFileAsync(string projectName, string fileName, string? contents = null, bool open = false, CancellationToken cancellationToken = default)
    {
        await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

        var project = await GetProjectAsync(projectName, cancellationToken);
        var projectDirectory = Path.GetDirectoryName(project.FullName);
        var filePath = Path.Combine(projectDirectory, fileName);
        var directoryPath = Path.GetDirectoryName(filePath);
        Directory.CreateDirectory(directoryPath);

        if (contents != null)
        {
            File.WriteAllText(filePath, contents);
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
    }

    /// <summary>
    /// Adds a new standalone file to the Miscellaneous Files workspace.
    /// </summary>
    /// <param name="fileName">The name of the file to add.</param>
    public async Task AddStandaloneFileAsync(string fileName, CancellationToken cancellationToken)
    {
        await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

        string itemTemplate;

        var extension = Path.GetExtension(fileName).ToLowerInvariant();
        switch (extension)
        {
            case ".cs":
                itemTemplate = @"General\C# Class";
                break;
            case ".csx":
                itemTemplate = @"Script\Visual C# Script";
                break;
            case ".vb":
                itemTemplate = @"General\Visual Basic Class";
                break;
            case ".txt":
                itemTemplate = @"General\Text File";
                break;
            default:
                throw new NotSupportedException($"File type '{extension}' is not yet supported.");
        }

        var dte = await GetRequiredGlobalServiceAsync<SDTE, EnvDTE.DTE>(cancellationToken);
        dte.ItemOperations.NewFile(itemTemplate, fileName);
    }

    public async Task RenameFileAsync(string projectName, string oldFileName, string newFileName, CancellationToken cancellationToken)
    {
        await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);
        var projectItem = await GetProjectItemAsync(projectName, oldFileName, cancellationToken);

        projectItem.Name = newFileName;
    }

    public async Task<EnvDTE.ProjectItem> GetProjectItemAsync(string projectName, string relativeFilePath, CancellationToken cancellationToken)
    {
        await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

        var solution = (await GetRequiredGlobalServiceAsync<SDTE, EnvDTE.DTE>(cancellationToken)).Solution;
        var projects = solution.Projects.Cast<EnvDTE.Project>();
        var project = projects.FirstOrDefault(x => x.Name == projectName);

        if (project == null)
        {
            throw new InvalidOperationException($"Project '{projectName} could not be found. Available projects: {string.Join(", ", projects.Select(x => x.Name))}.");
        }

        var projectPath = Path.GetDirectoryName(project.FullName);
        var fullFilePath = Path.Combine(projectPath, relativeFilePath);

        var projectItems = project.ProjectItems.Cast<EnvDTE.ProjectItem>();
        var document = projectItems.FirstOrDefault(d => d.get_FileNames(1).Equals(fullFilePath));

        if (document == null)
        {
            throw new InvalidOperationException($"File '{fullFilePath}' could not be found.  Available files: {string.Join(", ", projectItems.Select(x => x.get_FileNames(1)))}.");
        }

        return document;
    }

    public async Task SetFileContentsAsync(string projectName, string relativeFilePath, string content, CancellationToken cancellationToken)
    {
        await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

        var project = await GetProjectAsync(projectName, cancellationToken);
        var projectPath = Path.GetDirectoryName(project.FullName);
        var filePath = Path.Combine(projectPath, relativeFilePath);

        File.WriteAllText(filePath, content);
    }

    public async Task<string> GetFileContentsAsync(string projectName, string relativeFilePath, CancellationToken cancellationToken)
    {
        await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

        var project = await GetProjectAsync(projectName, cancellationToken);
        var projectPath = Path.GetDirectoryName(project.FullName);
        var filePath = Path.Combine(projectPath, relativeFilePath);

        return File.ReadAllText(filePath);
    }

    public async Task<(string solutionDirectory, string solutionFileFullPath, string userOptionsFile)> GetSolutionInfoAsync(CancellationToken cancellationToken)
    {
        await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

        if (!await IsSolutionOpenAsync(cancellationToken))
            throw new InvalidOperationException("No solution is open.");

        var solution = await GetRequiredGlobalServiceAsync<SVsSolution, IVsSolution>(cancellationToken);
        ErrorHandler.ThrowOnFailure(solution.GetSolutionInfo(out var solutionDirectory, out var solutionFileFullPath, out var userOptionsFile));

        return (solutionDirectory, solutionFileFullPath, userOptionsFile);
    }

    /// <returns>
    /// true if build succeeds, otherwise false.
    /// </returns>
    public async Task<bool> BuildSolutionAndWaitAsync(CancellationToken cancellationToken)
    {
        await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

        var buildManager = await GetRequiredGlobalServiceAsync<SVsSolutionBuildManager, IVsSolutionBuildManager2>(cancellationToken);
        using var solutionEvents = new UpdateSolutionEvents(buildManager);
        var buildCompleteTaskCompletionSource = new TaskCompletionSource<bool>();

        void HandleUpdateSolutionDone(bool buildSucceed) => buildCompleteTaskCompletionSource.SetResult(buildSucceed);
        solutionEvents.OnUpdateSolutionDone += HandleUpdateSolutionDone;
        try
        {
            ErrorHandler.ThrowOnFailure(buildManager.StartSimpleUpdateSolutionConfiguration((uint)VSSOLNBUILDUPDATEFLAGS.SBF_OPERATION_BUILD, 0, 0));
            return await buildCompleteTaskCompletionSource.Task;
        }
        finally
        {
            solutionEvents.OnUpdateSolutionDone -= HandleUpdateSolutionDone;
        }
    }

    private static string ConvertLanguageName(string languageName)
    {
        return languageName switch
        {
            LanguageNames.CSharp => "CSharp",
            LanguageNames.VisualBasic => "VisualBasic",
            _ => throw new ArgumentException($"'{languageName}' is not supported.", nameof(languageName)),
        };
    }

    public async Task<string> GetAbsolutePathForProjectRelativeFilePathAsync(string projectName, string relativeFilePath, CancellationToken cancellationToken)
    {
        await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

        var dte = await GetRequiredGlobalServiceAsync<SDTE, EnvDTE.DTE>(cancellationToken);
        var solution = dte.Solution;
        Assumes.Present(solution);

        var project = solution.Projects.Cast<EnvDTE.Project>().First(x => x.Name == projectName);
        var projectPath = Path.GetDirectoryName(project.FullName);
        return Path.Combine(projectPath, relativeFilePath);
    }

    private async Task<string> GetDirectoryNameAsync(CancellationToken cancellationToken)
    {
        var (solutionDirectory, solutionFileFullPath, _) = await GetSolutionInfoAsync(cancellationToken);
        if (string.IsNullOrEmpty(solutionFileFullPath))
        {
            throw new InvalidOperationException();
        }

        return solutionDirectory;
    }

    private async Task<string> GetProjectTemplatePathAsync(string projectTemplate, string languageName, CancellationToken cancellationToken)
    {
        await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

        var dte = await GetRequiredGlobalServiceAsync<SDTE, EnvDTE.DTE>(cancellationToken);
        var solution = (EnvDTE80.Solution2)dte.Solution;

        var hostLocale = await GetRequiredGlobalServiceAsync<SUIHostLocale, IUIHostLocale>(cancellationToken);
        ErrorHandler.ThrowOnFailure(hostLocale.GetUILocale(out var localeID));

        if (string.Equals(languageName, "csharp", StringComparison.OrdinalIgnoreCase)
            && GetCSharpProjectTemplates(localeID).TryGetValue(projectTemplate, out var csharpProjectTemplate))
        {
            return solution.GetProjectTemplate(csharpProjectTemplate, languageName);
        }

        if (string.Equals(languageName, "visualbasic", StringComparison.OrdinalIgnoreCase)
            && GetVisualBasicProjectTemplates(localeID).TryGetValue(projectTemplate, out var visualBasicProjectTemplate))
        {
            return solution.GetProjectTemplate(visualBasicProjectTemplate, languageName);
        }

        return solution.GetProjectTemplate(projectTemplate, languageName);

        static ImmutableDictionary<string, string> GetCSharpProjectTemplates(uint localeID)
        {
            var builder = ImmutableDictionary.CreateBuilder<string, string>();
            builder[WellKnownProjectTemplates.ClassLibrary] = $@"Windows\{localeID}\ClassLibrary.zip";
            builder[WellKnownProjectTemplates.ConsoleApplication] = "Microsoft.CSharp.ConsoleApplication";
            builder[WellKnownProjectTemplates.Website] = "EmptyWeb.zip";
            builder[WellKnownProjectTemplates.WinFormsApplication] = "WindowsApplication.zip";
            builder[WellKnownProjectTemplates.WpfApplication] = "WpfApplication.zip";
            builder[WellKnownProjectTemplates.WebApplication] = "WebApplicationProject40";
            return builder.ToImmutable();
        }

        static ImmutableDictionary<string, string> GetVisualBasicProjectTemplates(uint localeID)
        {
            var builder = ImmutableDictionary.CreateBuilder<string, string>();
            builder[WellKnownProjectTemplates.ClassLibrary] = $@"Windows\{localeID}\ClassLibrary.zip";
            builder[WellKnownProjectTemplates.ConsoleApplication] = "Microsoft.VisualBasic.Windows.ConsoleApplication";
            builder[WellKnownProjectTemplates.Website] = "EmptyWeb.zip";
            builder[WellKnownProjectTemplates.WinFormsApplication] = "WindowsApplication.zip";
            builder[WellKnownProjectTemplates.WpfApplication] = "WpfApplication.zip";
            builder[WellKnownProjectTemplates.WebApplication] = "WebApplicationProject40";
            return builder.ToImmutable();
        }
    }

    private static string CreateTemporaryPath()
    {
        return Path.Combine(Path.GetTempPath(), "roslyn-test", Path.GetRandomFileName());
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
}

internal sealed class UpdateSolutionEvents : IVsUpdateSolutionEvents, IDisposable
{
    private uint _cookie;
    private readonly IVsSolutionBuildManager2 _solutionBuildManager;

    public event Action<bool>? OnUpdateSolutionDone;

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
        var buildSucceeded = fSucceeded == 1;
        OnUpdateSolutionDone?.Invoke(buildSucceeded);
        return VSConstants.S_OK;
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
