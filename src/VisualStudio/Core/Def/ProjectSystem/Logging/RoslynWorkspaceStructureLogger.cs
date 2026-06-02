// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Logging;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem.Extensions;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.FileDialog;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Threading;

#pragma warning disable CA2007 // We are OK awaiting tasks since we're following Visual Studio threading rules in this file

namespace Microsoft.VisualStudio.LanguageServices.ProjectSystem.Logging;

internal sealed class RoslynWorkspaceStructureLogger(IServiceProvider serviceProvider, IThreadingContext threadingContext) : WorkspaceStructureLogger
{
    public static void ShowSaveDialogAndLog(IServiceProvider serviceProvider)
    {
        ThreadHelper.ThrowIfNotOnUIThread();

        var uiShell = (IVsUIShell)serviceProvider.GetService(typeof(SVsUIShell));
        var uiShell2 = (IVsUIShell2)uiShell;

        Assumes.Present(uiShell2);

        ErrorHandler.ThrowOnFailure(uiShell.GetDialogOwnerHwnd(out var hwnd));

        var filters = new DialogFilters(
            new[] { new DialogFilter("Zip Files", "*.zip") },
            defaultFilterIndex: 0);

        var path = VsShellUtilities.SelectSaveAsFile(
            uiShell2,
            hwnd,
            title: string.Empty,
            initialDirectory: string.Empty,
            initialFileName: string.Empty,
            filters);

        if (string.IsNullOrEmpty(path))
            return;

        var threadingContext = serviceProvider.GetMefService<IThreadingContext>();

        threadingContext.JoinableTaskFactory.RunAsync(() => LogAsync(serviceProvider, threadingContext, path));
    }

    public static async Task LogAsync(IServiceProvider serviceProvider, IThreadingContext threadingContext, string path)
    {
        var componentModel = (IComponentModel)serviceProvider.GetService(typeof(SComponentModel));
        Assumes.Present(componentModel);

        var workspace = componentModel.GetService<VisualStudioWorkspace>();
        var solution = workspace.CurrentSolution;

        // Start a threaded wait dialog
        await threadingContext.JoinableTaskFactory.SwitchToMainThreadAsync();
        var dialogFactory = (IVsThreadedWaitDialogFactory)serviceProvider.GetService(typeof(SVsThreadedWaitDialogFactory));
        Assumes.Present(dialogFactory);
        using var session = dialogFactory.StartWaitDialog(
            ServicesVSResources.Visual_Studio,
            new ThreadedWaitDialogProgressData(
                ServicesVSResources.Logging_Roslyn_Workspace_structure,
                progressText: null,
                statusBarText: null,
                isCancelable: true,
                currentStep: 0,
                totalSteps: solution.ProjectIds.Count),
            delayToShowDialog: TimeSpan.Zero);
        var cancellationToken = session.UserCancellationToken;

        // Now switch to the background thread while we're working
        await TaskScheduler.Default;

        try
        {
            var progress = new Progress<(int current, int total)>(value =>
            {
                session.Progress.Report(new ThreadedWaitDialogProgressData(
                    ServicesVSResources.Logging_Roslyn_Workspace_structure,
                    progressText: null,
                    statusBarText: null,
                    isCancelable: true,
                    currentStep: value.current,
                    totalSteps: value.total));
            });

            var workspaceStructureLogger = new RoslynWorkspaceStructureLogger(serviceProvider, threadingContext);
            var document = await workspaceStructureLogger.BuildWorkspaceStructureAsync(
                solution,
                workspace.Kind,
                progress,
                cancellationToken);

            File.Delete(path);

            using (var zipFile = ZipFile.Open(path, ZipArchiveMode.Create))
            {
                var zipFileEntry = zipFile.CreateEntry("Workspace.xml", CompressionLevel.Fastest);
                using var stream = zipFileEntry.Open();
                document.Save(stream);
            }
        }
        catch (OperationCanceledException)
        {
            // They cancelled
        }
    }

    protected override async Task<IEnumerable<XElement>> CreateAdditionalProjectElementsAsync(Project project, CancellationToken cancellationToken)
    {
        await threadingContext.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

        var dte = (EnvDTE.DTE)serviceProvider.GetService(typeof(SDTE));

        VSLangProj.VSProject? langProjProject = null;
        foreach (EnvDTE.Project p in dte.Solution.Projects)
        {
            try
            {
                if (string.Equals(p.FullName, project.FilePath, StringComparison.OrdinalIgnoreCase))
                {
                    langProjProject = p.Object as VSLangProj.VSProject;
                    if (langProjProject is not null)
                        break;
                }
            }
            catch (NotImplementedException)
            {
                // Some EnvDTE.Projects will throw on p.FullName, so just bail in that case.
            }
        }

        if (langProjProject == null)
            return [];

        var elements = new List<XElement>();
        var dteReferences = new XElement("dteReferences");
        elements.Add(dteReferences);

        foreach (var reference in langProjProject.References.Cast<VSLangProj.Reference>())
        {
            if (reference.SourceProject != null)
            {
                dteReferences.Add(new XElement("projectReference", new XAttribute("projectName", reference.SourceProject.Name)));
            }
            else
            {
                dteReferences.Add(new XElement("metadataReference",
                    new XAttribute("path", SanitizePath(reference.Path)),
                    new XAttribute("name", reference.Name)));
            }
        }

        if (langProjProject is VSLangProj140.VSProject3 langProjProject3)
        {
            var dteAnalyzerReferences = new XElement("dteAnalyzerReferences");
            elements.Add(dteAnalyzerReferences);

            foreach (var analyzerPath in langProjProject3.AnalyzerReferences.Cast<string>())
            {
                dteAnalyzerReferences.Add(new XElement("analyzerReference",
                    new XAttribute("path", SanitizePath(analyzerPath))));
            }
        }

        return elements;
    }
}
