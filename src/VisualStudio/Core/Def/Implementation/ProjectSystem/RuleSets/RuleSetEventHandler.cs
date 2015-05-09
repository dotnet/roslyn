// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem.RuleSets
{
    [Export(typeof(RuleSetEventHandler))]
    internal sealed class RuleSetEventHandler : IVsTrackProjectDocumentsEvents2
    {
        private readonly IServiceProvider _serviceProvider;
        private bool _eventsHookedUp = false;
        private uint _cookie = 0;

        [ImportingConstructor]
        public RuleSetEventHandler(
            [Import(typeof(SVsServiceProvider))]IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public void Register()
        {
            if (!_eventsHookedUp)
            {
                var trackProjectDocuments = (IVsTrackProjectDocuments2)_serviceProvider.GetService(typeof(SVsTrackProjectDocuments));

                if (ErrorHandler.Succeeded(trackProjectDocuments.AdviseTrackProjectDocumentsEvents(this, out _cookie)))
                {
                    _eventsHookedUp = true;
                }
            }
        }

        public void Unregister()
        {
            if (_eventsHookedUp)
            {
                var trackProjectDocuments = (IVsTrackProjectDocuments2)_serviceProvider.GetService(typeof(SVsTrackProjectDocuments));

                if (ErrorHandler.Succeeded(trackProjectDocuments.UnadviseTrackProjectDocumentsEvents(_cookie)))
                {
                    _eventsHookedUp = false;
                    _cookie = 0;
                }
            }
        }

        int IVsTrackProjectDocumentsEvents2.OnAfterAddDirectoriesEx(int cProjects, int cDirectories, IVsProject[] rgpProjects, int[] rgFirstIndices, string[] rgpszMkDocuments, VSADDDIRECTORYFLAGS[] rgFlags)
        {
            return VSConstants.S_OK;
        }

        int IVsTrackProjectDocumentsEvents2.OnAfterAddFilesEx(int cProjects, int cFiles, IVsProject[] rgpProjects, int[] rgFirstIndices, string[] rgpszMkDocuments, VSADDFILEFLAGS[] rgFlags)
        {
            return VSConstants.S_OK;
        }

        int IVsTrackProjectDocumentsEvents2.OnAfterRemoveDirectories(int cProjects, int cDirectories, IVsProject[] rgpProjects, int[] rgFirstIndices, string[] rgpszMkDocuments, VSREMOVEDIRECTORYFLAGS[] rgFlags)
        {
            return VSConstants.S_OK;
        }

        int IVsTrackProjectDocumentsEvents2.OnAfterRemoveFiles(int cProjects, int cFiles, IVsProject[] rgpProjects, int[] rgFirstIndices, string[] rgpszMkDocuments, VSREMOVEFILEFLAGS[] rgFlags)
        {
            for (int i = 0; i < rgpProjects.Length; i++)
            {
                int indexOfFirstDocumentInProject = IndexOfFirstDocumentInProject(i, rgFirstIndices);
                int indexOfFirstDocumentInNextProject = IndexOfFirstDocumentInProject(i + 1, rgFirstIndices);
                for (int j = indexOfFirstDocumentInProject; j < indexOfFirstDocumentInNextProject; j++)
                {
                    string fileFullPath = rgpszMkDocuments[j];
                    if (Path.GetExtension(fileFullPath).Equals(".ruleset", StringComparison.OrdinalIgnoreCase))
                    {
                        EnvDTE.Project project;
                        IVsHierarchy hierarchy = rgpProjects[i] as IVsHierarchy;
                        if (hierarchy != null &&
                            hierarchy.TryGetProject(out project))
                        {
                            UpdateCodeAnalysisRuleSetPropertiesInProject(project, fileFullPath, string.Empty);
                        }
                    }
                }
            }

            return VSConstants.S_OK;
        }

        int IVsTrackProjectDocumentsEvents2.OnAfterRenameDirectories(int cProjects, int cDirs, IVsProject[] rgpProjects, int[] rgFirstIndices, string[] rgszMkOldNames, string[] rgszMkNewNames, VSRENAMEDIRECTORYFLAGS[] rgFlags)
        {
            return VSConstants.S_OK;
        }

        int IVsTrackProjectDocumentsEvents2.OnAfterRenameFiles(int cProjects, int cFiles, IVsProject[] rgpProjects, int[] rgFirstIndices, string[] rgszMkOldNames, string[] rgszMkNewNames, VSRENAMEFILEFLAGS[] rgFlags)
        {
            for (int i = 0; i < rgpProjects.Length; i++)
            {
                int indexOfFirstDocumentInProject = IndexOfFirstDocumentInProject(i, rgFirstIndices);
                int indexOfFirstDocumentInNextProject = IndexOfFirstDocumentInProject(i + 1, rgFirstIndices);
                for (int j = indexOfFirstDocumentInProject; j < indexOfFirstDocumentInNextProject; j++)
                {
                    string oldFileFullPath = rgszMkOldNames[j];
                    string newFileFullPath = rgszMkNewNames[j];
                    if (Path.GetExtension(oldFileFullPath).Equals(".ruleset", StringComparison.OrdinalIgnoreCase))
                    {
                        EnvDTE.Project project;
                        IVsHierarchy hierarchy = rgpProjects[i] as IVsHierarchy;
                        if (hierarchy != null &&
                            hierarchy.TryGetProject(out project))
                        {
                            UpdateCodeAnalysisRuleSetPropertiesInProject(project, oldFileFullPath, newFileFullPath);
                        }
                    }
                }
            }

            return VSConstants.S_OK;
        }

        int IVsTrackProjectDocumentsEvents2.OnAfterSccStatusChanged(int cProjects, int cFiles, IVsProject[] rgpProjects, int[] rgFirstIndices, string[] rgpszMkDocuments, uint[] rgdwSccStatus)
        {
            return VSConstants.S_OK;
        }

        int IVsTrackProjectDocumentsEvents2.OnQueryAddDirectories(IVsProject pProject, int cDirectories, string[] rgpszMkDocuments, VSQUERYADDDIRECTORYFLAGS[] rgFlags, VSQUERYADDDIRECTORYRESULTS[] pSummaryResult, VSQUERYADDDIRECTORYRESULTS[] rgResults)
        {
            return VSConstants.S_OK;
        }

        int IVsTrackProjectDocumentsEvents2.OnQueryAddFiles(IVsProject pProject, int cFiles, string[] rgpszMkDocuments, VSQUERYADDFILEFLAGS[] rgFlags, VSQUERYADDFILERESULTS[] pSummaryResult, VSQUERYADDFILERESULTS[] rgResults)
        {
            return VSConstants.S_OK;
        }

        int IVsTrackProjectDocumentsEvents2.OnQueryRemoveDirectories(IVsProject pProject, int cDirectories, string[] rgpszMkDocuments, VSQUERYREMOVEDIRECTORYFLAGS[] rgFlags, VSQUERYREMOVEDIRECTORYRESULTS[] pSummaryResult, VSQUERYREMOVEDIRECTORYRESULTS[] rgResults)
        {
            return VSConstants.S_OK;
        }

        int IVsTrackProjectDocumentsEvents2.OnQueryRemoveFiles(IVsProject pProject, int cFiles, string[] rgpszMkDocuments, VSQUERYREMOVEFILEFLAGS[] rgFlags, VSQUERYREMOVEFILERESULTS[] pSummaryResult, VSQUERYREMOVEFILERESULTS[] rgResults)
        {
            return VSConstants.S_OK;
        }

        int IVsTrackProjectDocumentsEvents2.OnQueryRenameDirectories(IVsProject pProject, int cDirs, string[] rgszMkOldNames, string[] rgszMkNewNames, VSQUERYRENAMEDIRECTORYFLAGS[] rgFlags, VSQUERYRENAMEDIRECTORYRESULTS[] pSummaryResult, VSQUERYRENAMEDIRECTORYRESULTS[] rgResults)
        {
            return VSConstants.S_OK;
        }

        int IVsTrackProjectDocumentsEvents2.OnQueryRenameFiles(IVsProject pProject, int cFiles, string[] rgszMkOldNames, string[] rgszMkNewNames, VSQUERYRENAMEFILEFLAGS[] rgFlags, VSQUERYRENAMEFILERESULTS[] pSummaryResult, VSQUERYRENAMEFILERESULTS[] rgResults)
        {
            return VSConstants.S_OK;
        }

        private static void UpdateCodeAnalysisRuleSetPropertiesInProject(EnvDTE.Project project, string oldRuleSetFilePath, string newRuleSetFilePath)
        {
            string projectDirectoryFullPath = Path.GetDirectoryName(project.FullName);
            foreach (EnvDTE.Configuration config in project.ConfigurationManager)
            {
                UpdateCodeAnalysisRuleSetPropertyInConfiguration(config, oldRuleSetFilePath, newRuleSetFilePath, projectDirectoryFullPath);
            }
        }

        private static void UpdateCodeAnalysisRuleSetPropertyInConfiguration(EnvDTE.Configuration config, string oldRuleSetFilePath, string newRuleSetFilePath, string projectDirectoryFullPath)
        {
            EnvDTE.Properties properties = config.Properties;
            try
            {
                EnvDTE.Property codeAnalysisRuleSetFileProperty = properties.Item("CodeAnalysisRuleSet");

                if (codeAnalysisRuleSetFileProperty != null)
                {
                    string codeAnalysisRuleSetFileName = codeAnalysisRuleSetFileProperty.Value as string;
                    if (!string.IsNullOrWhiteSpace(codeAnalysisRuleSetFileName))
                    {
                        string codeAnalysisRuleSetFullPath = FileUtilities.ResolveRelativePath(codeAnalysisRuleSetFileName, projectDirectoryFullPath);
                        if (codeAnalysisRuleSetFullPath.Equals(oldRuleSetFilePath, StringComparison.OrdinalIgnoreCase))
                        {
                            string newRuleSetRelativePath = FilePathUtilities.GetRelativePath(projectDirectoryFullPath, newRuleSetFilePath);
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
}
