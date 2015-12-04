// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Host;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem.Interop;
using Microsoft.VisualStudio.Shell.Interop;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem
{
    internal partial class AbstractProject : IAnalyzerHost
    {
        private AnalyzerFileWatcherService _analyzerFileWatcherService = null;
        private AnalyzerDependencyCheckingService _dependencyCheckingService = null;

        public void AddAnalyzerAssembly(string analyzerAssemblyFullPath)
        {
            if (_analyzers.ContainsKey(analyzerAssemblyFullPath))
            {
                return;
            }

            var analyzerLoader = _visualStudioWorkspaceOpt.Services.GetRequiredService<IAnalyzerService>().GetLoader();
            analyzerLoader.AddDependencyLocation(analyzerAssemblyFullPath);

            var fileChangeService = (IVsFileChangeEx)this.ServiceProvider.GetService(typeof(SVsFileChangeEx));
            var analyzer = new VisualStudioAnalyzer(analyzerAssemblyFullPath, fileChangeService, this.HostDiagnosticUpdateSource, this.Id, this.Workspace, analyzerLoader, this.Language);
            _analyzers[analyzerAssemblyFullPath] = analyzer;

            if (_pushingChangesToWorkspaceHosts)
            {
                var analyzerReference = analyzer.GetReference();
                this.ProjectTracker.NotifyWorkspaceHosts(host => host.OnAnalyzerReferenceAdded(_id, analyzerReference));

                List<VisualStudioAnalyzer> existingReferencesWithLoadErrors = _analyzers.Values.Where(a => a.HasLoadErrors).ToList();

                foreach (var existingReference in existingReferencesWithLoadErrors)
                {
                    this.ProjectTracker.NotifyWorkspaceHosts(host => host.OnAnalyzerReferenceRemoved(_id, existingReference.GetReference()));
                    existingReference.Reset();
                    this.ProjectTracker.NotifyWorkspaceHosts(host => host.OnAnalyzerReferenceAdded(_id, existingReference.GetReference()));
                }

                GetAnalyzerDependencyCheckingService().CheckForConflictsAsync();
            }

            if (File.Exists(analyzerAssemblyFullPath))
            {
                GetAnalyzerFileWatcherService().AddPath(analyzerAssemblyFullPath);
                GetAnalyzerFileWatcherService().ErrorIfAnalyzerAlreadyLoaded(_id, analyzerAssemblyFullPath);
            }
            else
            {
                analyzer.UpdatedOnDisk += OnAnalyzerChanged;
            }
        }

        public void RemoveAnalyzerAssembly(string analyzerAssemblyFullPath)
        {
            VisualStudioAnalyzer analyzer;
            if (!_analyzers.TryGetValue(analyzerAssemblyFullPath, out analyzer))
            {
                return;
            }

            GetAnalyzerFileWatcherService().RemoveAnalyzerAlreadyLoadedDiagnostics(_id, analyzerAssemblyFullPath);

            _analyzers.Remove(analyzerAssemblyFullPath);

            if (_pushingChangesToWorkspaceHosts)
            {
                var analyzerReference = analyzer.GetReference();
                this.ProjectTracker.NotifyWorkspaceHosts(host => host.OnAnalyzerReferenceRemoved(_id, analyzerReference));

                GetAnalyzerDependencyCheckingService().CheckForConflictsAsync();
            }

            analyzer.Dispose();
        }

        public void SetRuleSetFile(string ruleSetFileFullPath)
        {
            if (ruleSetFileFullPath == null)
            {
                ruleSetFileFullPath = string.Empty;
            }

            if (!ruleSetFileFullPath.Equals(string.Empty))
            {
                // This is already a full path, but run it through GetFullPath to clean it (e.g., remove
                // extra backslashes).
                ruleSetFileFullPath = Path.GetFullPath(ruleSetFileFullPath);
            }

            if (this.ruleSet != null &&
                this.ruleSet.FilePath.Equals(ruleSetFileFullPath, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            ResetAnalyzerRuleSet(ruleSetFileFullPath);
        }

        public void AddAdditionalFile(string additionalFilePath)
        {
            var document = this.DocumentProvider.TryGetDocumentForFile(this, (uint)VSConstants.VSITEMID.Nil, filePath: additionalFilePath, sourceCodeKind: SourceCodeKind.Regular, canUseTextBuffer: (b) => true);

            if (document == null)
            {
                return;
            }

            AddAdditionalDocument(document,
                isCurrentContext: document.Project.Hierarchy == LinkedFileUtilities.GetContextHierarchy(document, RunningDocumentTable));
        }

        public void RemoveAdditionalFile(string additionalFilePath)
        {
            IVisualStudioHostDocument document = this.GetCurrentDocumentFromPath(additionalFilePath);
            if (document == null)
            {
                throw new InvalidOperationException("The document is not a part of the finalProject.");
            }

            RemoveAdditionalDocument(document);
        }

        private void ResetAnalyzerRuleSet(string ruleSetFileFullPath)
        {
            ClearAnalyzerRuleSet();
            SetAnalyzerRuleSet(ruleSetFileFullPath);
            UpdateAnalyzerRules();
        }

        private void SetAnalyzerRuleSet(string ruleSetFileFullPath)
        {
            if (ruleSetFileFullPath.Length != 0)
            {
                this.ruleSet = this.ProjectTracker.RuleSetFileProvider.GetOrCreateRuleSet(ruleSetFileFullPath);
                this.ruleSet.UpdatedOnDisk += OnRuleSetFileUpdateOnDisk;
            }
        }

        private void ClearAnalyzerRuleSet()
        {
            if (this.ruleSet != null)
            {
                this.ruleSet.UpdatedOnDisk -= OnRuleSetFileUpdateOnDisk;
                this.ruleSet = null;
            }
        }

        private void OnRuleSetFileUpdateOnDisk(object sender, EventArgs e)
        {
            var filePath = this.ruleSet.FilePath;

            ResetAnalyzerRuleSet(filePath);
        }

        private AnalyzerFileWatcherService GetAnalyzerFileWatcherService()
        {
            if (_analyzerFileWatcherService == null)
            {
                var componentModel = (IComponentModel)this.ServiceProvider.GetService(typeof(SComponentModel));

                _analyzerFileWatcherService = componentModel.GetService<AnalyzerFileWatcherService>();
            }

            return _analyzerFileWatcherService;
        }

        private AnalyzerDependencyCheckingService GetAnalyzerDependencyCheckingService()
        {
            if (_dependencyCheckingService == null)
            {
                var componentModel = (IComponentModel)this.ServiceProvider.GetService(typeof(SComponentModel));

                _dependencyCheckingService = componentModel.GetService<AnalyzerDependencyCheckingService>();
            }

            return _dependencyCheckingService;
        }
    }
}
