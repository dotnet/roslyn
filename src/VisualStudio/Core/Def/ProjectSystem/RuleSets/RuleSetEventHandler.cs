// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Roslyn.Utilities;
using VSLangProj;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem.RuleSets;

[Export(typeof(RuleSetEventHandler))]
internal sealed class RuleSetEventHandler : IVsTrackProjectDocumentsEvents2, IVsTrackProjectDocumentsEvents3, IVsTrackProjectDocumentsEvents4
{
    private readonly IThreadingContext _threadingContext;
    private readonly IServiceProvider _serviceProvider;
    private bool _eventsHookedUp = false;
    private uint _cookie = 0;

    [ImportingConstructor]
    [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    public RuleSetEventHandler(
        IThreadingContext threadingContext,
        [Import(typeof(SVsServiceProvider))] IServiceProvider serviceProvider)
    {
        _threadingContext = threadingContext;
        _serviceProvider = serviceProvider;
    }

    public async Task RegisterAsync(IAsyncServiceProvider serviceProvider, CancellationToken cancellationToken)
    {
        if (!_eventsHookedUp)
        {
            var trackProjectDocuments = await serviceProvider.GetServiceAsync<SVsTrackProjectDocuments, IVsTrackProjectDocuments2>(_threadingContext.JoinableTaskFactory).ConfigureAwait(false);
            await _threadingContext.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

            if (!_eventsHookedUp)
            {
                if (ErrorHandler.Succeeded(trackProjectDocuments.AdviseTrackProjectDocumentsEvents(this, out _cookie)))
                    _eventsHookedUp = true;
            }
        }
    }

    public void Unregister()
    {
        if (_eventsHookedUp)
        {
            var trackProjectDocuments = (IVsTrackProjectDocuments2)_serviceProvider.GetService(typeof(SVsTrackProjectDocuments));

            // Null check, because sometimes during shutdown the IVsTrackProjectDocuments2 is cleaned up before we get told to unregister
            if (trackProjectDocuments is null || ErrorHandler.Succeeded(trackProjectDocuments.UnadviseTrackProjectDocumentsEvents(_cookie)))
            {
                _eventsHookedUp = false;
                _cookie = 0;
            }
        }
    }

    int IVsTrackProjectDocumentsEvents2.OnAfterAddDirectoriesEx(int cProjects, int cDirectories, IVsProject[] rgpProjects, int[] rgFirstIndices, string[] rgpszMkDocuments, VSADDDIRECTORYFLAGS[] rgFlags)
        => VSConstants.S_OK;

    int IVsTrackProjectDocumentsEvents2.OnAfterAddFilesEx(int cProjects, int cFiles, IVsProject[] rgpProjects, int[] rgFirstIndices, string[] rgpszMkDocuments, VSADDFILEFLAGS[] rgFlags)
        => VSConstants.S_OK;

    int IVsTrackProjectDocumentsEvents2.OnAfterRemoveDirectories(int cProjects, int cDirectories, IVsProject[] rgpProjects, int[] rgFirstIndices, string[] rgpszMkDocuments, VSREMOVEDIRECTORYFLAGS[] rgFlags)
        => VSConstants.S_OK;

    int IVsTrackProjectDocumentsEvents2.OnAfterRemoveFiles(int cProjects, int cFiles, IVsProject[] rgpProjects, int[] rgFirstIndices, string[] rgpszMkDocuments, VSREMOVEFILEFLAGS[] rgFlags)
        => VSConstants.S_OK;

    int IVsTrackProjectDocumentsEvents2.OnAfterRenameDirectories(int cProjects, int cDirs, IVsProject[] rgpProjects, int[] rgFirstIndices, string[] rgszMkOldNames, string[] rgszMkNewNames, VSRENAMEDIRECTORYFLAGS[] rgFlags)
        => VSConstants.S_OK;

    int IVsTrackProjectDocumentsEvents2.OnAfterRenameFiles(int cProjects, int cFiles, IVsProject[] rgpProjects, int[] rgFirstIndices, string[] rgszMkOldNames, string[] rgszMkNewNames, VSRENAMEFILEFLAGS[] rgFlags)
    {
        var ruleSetRenames = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        for (var i = 0; i < rgszMkOldNames.Length; i++)
        {
            var oldFileFullPath = rgszMkOldNames[i];
            if (Path.GetExtension(oldFileFullPath).Equals(".ruleset", StringComparison.OrdinalIgnoreCase))
            {
                var newFileFullPath = rgszMkNewNames[i];
                ruleSetRenames[oldFileFullPath] = newFileFullPath;
            }
        }

        foreach (var path in ruleSetRenames.Values)
        {
            UpdateCodeAnalysisRuleSetPropertiesInAllProjects(path);
        }

        return VSConstants.S_OK;
    }

    int IVsTrackProjectDocumentsEvents2.OnAfterSccStatusChanged(int cProjects, int cFiles, IVsProject[] rgpProjects, int[] rgFirstIndices, string[] rgpszMkDocuments, uint[] rgdwSccStatus)
        => VSConstants.S_OK;

    int IVsTrackProjectDocumentsEvents2.OnQueryAddDirectories(IVsProject pProject, int cDirectories, string[] rgpszMkDocuments, VSQUERYADDDIRECTORYFLAGS[] rgFlags, VSQUERYADDDIRECTORYRESULTS[] pSummaryResult, VSQUERYADDDIRECTORYRESULTS[] rgResults)
        => VSConstants.S_OK;

    int IVsTrackProjectDocumentsEvents2.OnQueryAddFiles(IVsProject pProject, int cFiles, string[] rgpszMkDocuments, VSQUERYADDFILEFLAGS[] rgFlags, VSQUERYADDFILERESULTS[] pSummaryResult, VSQUERYADDFILERESULTS[] rgResults)
        => VSConstants.S_OK;

    int IVsTrackProjectDocumentsEvents2.OnQueryRemoveDirectories(IVsProject pProject, int cDirectories, string[] rgpszMkDocuments, VSQUERYREMOVEDIRECTORYFLAGS[] rgFlags, VSQUERYREMOVEDIRECTORYRESULTS[] pSummaryResult, VSQUERYREMOVEDIRECTORYRESULTS[] rgResults)
        => VSConstants.S_OK;

    int IVsTrackProjectDocumentsEvents2.OnQueryRemoveFiles(IVsProject pProject, int cFiles, string[] rgpszMkDocuments, VSQUERYREMOVEFILEFLAGS[] rgFlags, VSQUERYREMOVEFILERESULTS[] pSummaryResult, VSQUERYREMOVEFILERESULTS[] rgResults)
        => VSConstants.S_OK;

    int IVsTrackProjectDocumentsEvents2.OnQueryRenameDirectories(IVsProject pProject, int cDirs, string[] rgszMkOldNames, string[] rgszMkNewNames, VSQUERYRENAMEDIRECTORYFLAGS[] rgFlags, VSQUERYRENAMEDIRECTORYRESULTS[] pSummaryResult, VSQUERYRENAMEDIRECTORYRESULTS[] rgResults)
        => VSConstants.S_OK;

    int IVsTrackProjectDocumentsEvents2.OnQueryRenameFiles(IVsProject pProject, int cFiles, string[] rgszMkOldNames, string[] rgszMkNewNames, VSQUERYRENAMEFILEFLAGS[] rgFlags, VSQUERYRENAMEFILERESULTS[] pSummaryResult, VSQUERYRENAMEFILERESULTS[] rgResults)
        => VSConstants.S_OK;

    int IVsTrackProjectDocumentsEvents3.OnBeginQueryBatch()
        => VSConstants.S_OK;

    int IVsTrackProjectDocumentsEvents3.OnEndQueryBatch(out int pfActionOK)
    {
        pfActionOK = 1;
        return VSConstants.S_OK;
    }

    int IVsTrackProjectDocumentsEvents3.OnCancelQueryBatch()
        => VSConstants.S_OK;

    int IVsTrackProjectDocumentsEvents3.OnQueryAddFilesEx(IVsProject pProject, int cFiles, string[] rgpszNewMkDocuments, string[] rgpszSrcMkDocuments, VSQUERYADDFILEFLAGS[] rgFlags, VSQUERYADDFILERESULTS[] pSummaryResult, VSQUERYADDFILERESULTS[] rgResults)
        => VSConstants.S_OK;

    int IVsTrackProjectDocumentsEvents3.HandsOffFiles(uint grfRequiredAccess, int cFiles, string[] rgpszMkDocuments)
        => VSConstants.S_OK;

    int IVsTrackProjectDocumentsEvents3.HandsOnFiles(int cFiles, string[] rgpszMkDocuments)
        => VSConstants.S_OK;

    void IVsTrackProjectDocumentsEvents4.OnQueryRemoveFilesEx(IVsProject pProject, int cFiles, string[] rgpszMkDocuments, uint[] rgFlags, VSQUERYREMOVEFILERESULTS[] pSummaryResult, VSQUERYREMOVEFILERESULTS[] rgResults)
    {
    }

    void IVsTrackProjectDocumentsEvents4.OnQueryRemoveDirectoriesEx(IVsProject pProject, int cDirectories, string[] rgpszMkDocuments, uint[] rgFlags, VSQUERYREMOVEDIRECTORYRESULTS[] pSummaryResult, VSQUERYREMOVEDIRECTORYRESULTS[] rgResults)
    {
    }

    void IVsTrackProjectDocumentsEvents4.OnAfterRemoveFilesEx(int cProjects, int cFiles, IVsProject[] rgpProjects, int[] rgFirstIndices, string[] rgpszMkDocuments, uint[] rgFlags)
    {
        // First, handle the files that have been removed from projects (rather than deleted).
        // Here we only want to update the projects from which the file was removed.

        for (var i = 0; i < rgpProjects.Length; i++)
        {
            var indexOfFirstDocumentInProject = IndexOfFirstDocumentInProject(i, rgFirstIndices);
            var indexOfFirstDocumentInNextProject = IndexOfFirstDocumentInProject(i + 1, rgFirstIndices);
            for (var j = indexOfFirstDocumentInProject; j < indexOfFirstDocumentInNextProject; j++)
            {
                var fileFullPath = rgpszMkDocuments[j];
                var removed = (rgFlags[j] & (uint)__VSREMOVEFILEFLAGS2.VSREMOVEFILEFLAGS_IsRemovedFromProjectOnly) != 0;
                if (removed &&
                    Path.GetExtension(fileFullPath).Equals(".ruleset", StringComparison.OrdinalIgnoreCase))
                {
                    if (rgpProjects[i] is IVsHierarchy hierarchy &&
                        hierarchy.TryGetProject(out var project))
                    {
                        UpdateCodeAnalysisRuleSetPropertiesInProject(project, string.Empty);
                    }
                }
            }
        }

        // Second, handle the files that have been deleted. In this case we need to update
        // every project that was using this file in some way.
        var ruleSetDeletions = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        for (var i = 0; i < rgpszMkDocuments.Length; i++)
        {
            var fileFullPath = rgpszMkDocuments[i];
            var deleted = (rgFlags[i] & (uint)__VSREMOVEFILEFLAGS2.VSREMOVEFILEFLAGS_IsRemovedFromProjectOnly) == 0;
            if (deleted &&
                Path.GetExtension(fileFullPath).Equals(".ruleset", StringComparison.OrdinalIgnoreCase))
            {
                ruleSetDeletions.Add(fileFullPath);
            }
        }

#pragma warning disable IDE0059 // Unnecessary assignment of a value - https://github.com/dotnet/roslyn/issues/46168
        foreach (var fileFullPath in ruleSetDeletions)
#pragma warning restore IDE0059 // Unnecessary assignment of a value
        {
            UpdateCodeAnalysisRuleSetPropertiesInAllProjects(string.Empty);
        }
    }

    void IVsTrackProjectDocumentsEvents4.OnAfterRemoveDirectoriesEx(int cProjects, int cDirectories, IVsProject[] rgpProjects, int[] rgFirstIndices, string[] rgpszMkDocuments, uint[] rgFlags)
    {
    }

    private void UpdateCodeAnalysisRuleSetPropertiesInAllProjects(string newFileFullPath)
    {
        var dte = (EnvDTE.DTE)_serviceProvider.GetService(typeof(SDTE));
        foreach (EnvDTE.Project project in dte.Solution.Projects)
        {
            UpdateCodeAnalysisRuleSetPropertiesInProject(project, newFileFullPath);
        }
    }

    private static void UpdateCodeAnalysisRuleSetPropertiesInProject(EnvDTE.Project project, string newRuleSetFilePath)
    {
        if (project.Kind is PrjKind.prjKindCSharpProject or
            PrjKind.prjKindVBProject)
        {
            var projectFullName = project.FullName;
            if (!string.IsNullOrWhiteSpace(projectFullName))
            {
                var projectDirectoryFullPath = Path.GetDirectoryName(project.FullName);
                foreach (EnvDTE.Configuration config in project.ConfigurationManager)
                {
                    UpdateCodeAnalysisRuleSetPropertyInConfiguration(config, newRuleSetFilePath, projectDirectoryFullPath);
                }
            }
        }
    }

    private static void UpdateCodeAnalysisRuleSetPropertyInConfiguration(EnvDTE.Configuration config, string newRuleSetFilePath, string projectDirectoryFullPath)
    {
        var properties = config.Properties;
        try
        {
            var codeAnalysisRuleSetFileProperty = properties?.Item("CodeAnalysisRuleSet");

            if (codeAnalysisRuleSetFileProperty != null)
            {
                var codeAnalysisRuleSetFileName = codeAnalysisRuleSetFileProperty.Value as string;
                if (!string.IsNullOrWhiteSpace(codeAnalysisRuleSetFileName))
                {
                    var codeAnalysisRuleSetFullPath = FileUtilities.ResolveRelativePath(codeAnalysisRuleSetFileName, projectDirectoryFullPath);
                    codeAnalysisRuleSetFullPath = FileUtilities.NormalizeAbsolutePath(codeAnalysisRuleSetFullPath);
                    var oldRuleSetFilePath = FileUtilities.NormalizeAbsolutePath(codeAnalysisRuleSetFullPath);

                    if (codeAnalysisRuleSetFullPath.Equals(oldRuleSetFilePath, StringComparison.OrdinalIgnoreCase))
                    {
                        var newRuleSetRelativePath = PathUtilities.GetRelativePath(projectDirectoryFullPath, newRuleSetFilePath);
                        codeAnalysisRuleSetFileProperty.Value = newRuleSetRelativePath;
                    }
                }
            }
        }
        catch (ArgumentException)
        {
            // Unfortunately the properties collection sometimes throws an ArgumentException
            // instead of returning null if the current configuration doesn't support CodeAnalysisRuleSet.
            // Ignore it and move on.
        }
    }

    private static int IndexOfFirstDocumentInProject(int projectIndex, int[] firstDocumentIndices)
    {
        if (projectIndex >= firstDocumentIndices.Length)
        {
            return firstDocumentIndices.Length;
        }
        else
        {
            return firstDocumentIndices[projectIndex];
        }
    }
}
