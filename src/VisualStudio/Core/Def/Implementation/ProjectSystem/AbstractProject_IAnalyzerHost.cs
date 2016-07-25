// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
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

            var fileChangeService = (IVsFileChangeEx)this.ServiceProvider.GetService(typeof(SVsFileChangeEx));
            if (Workspace == null)
            {
                // This can happen only in tests.
                var testAnalyzer = new VisualStudioAnalyzer(analyzerAssemblyFullPath, fileChangeService, this.HostDiagnosticUpdateSource, this.Id, this.Workspace, loader: null, language: this.Language);
                _analyzers[analyzerAssemblyFullPath] = testAnalyzer;
                return;
            }

            var analyzerLoader = Workspace.Services.GetRequiredService<IAnalyzerService>().GetLoader();
            analyzerLoader.AddDependencyLocation(analyzerAssemblyFullPath);
            var analyzer = new VisualStudioAnalyzer(analyzerAssemblyFullPath, fileChangeService, this.HostDiagnosticUpdateSource, this.Id, this.Workspace, analyzerLoader, this.Language);
            _analyzers[analyzerAssemblyFullPath] = analyzer;

            if (_pushingChangesToWorkspaceHosts)
            {
                var analyzerReference = analyzer.GetReference();
                this.ProjectTracker.NotifyWorkspaceHosts(host => host.OnAnalyzerReferenceAdded(Id, analyzerReference));

                List<VisualStudioAnalyzer> existingReferencesWithLoadErrors = _analyzers.Values.Where(a => a.HasLoadErrors).ToList();

                foreach (var existingReference in existingReferencesWithLoadErrors)
                {
                    this.ProjectTracker.NotifyWorkspaceHosts(host => host.OnAnalyzerReferenceRemoved(Id, existingReference.GetReference()));
                    existingReference.Reset();
                    this.ProjectTracker.NotifyWorkspaceHosts(host => host.OnAnalyzerReferenceAdded(Id, existingReference.GetReference()));
                }

                GetAnalyzerDependencyCheckingService().CheckForConflictsAsync();
            }

            if (File.Exists(analyzerAssemblyFullPath))
            {
                GetAnalyzerFileWatcherService().AddPath(analyzerAssemblyFullPath);
                GetAnalyzerFileWatcherService().ErrorIfAnalyzerAlreadyLoaded(Id, analyzerAssemblyFullPath);
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

            if (Workspace == null)
            {
                // This can happen only in tests.
                _analyzers.Remove(analyzerAssemblyFullPath);
                analyzer.Dispose();
                return;
            }

            GetAnalyzerFileWatcherService().RemoveAnalyzerAlreadyLoadedDiagnostics(Id, analyzerAssemblyFullPath);

            _analyzers.Remove(analyzerAssemblyFullPath);

            if (_pushingChangesToWorkspaceHosts)
            {
                var analyzerReference = analyzer.GetReference();
                this.ProjectTracker.NotifyWorkspaceHosts(host => host.OnAnalyzerReferenceRemoved(Id, analyzerReference));

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

            if (this.RuleSetFile != null &&
                this.RuleSetFile.FilePath.Equals(ruleSetFileFullPath, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            ResetAnalyzerRuleSet(ruleSetFileFullPath);
        }

        public void AddAdditionalFile(string additionalFilePath)
        {
            var document = this.DocumentProvider.TryGetDocumentForFile(
                this,
                ImmutableArray<string>.Empty,
                filePath: additionalFilePath,
                sourceCodeKind: SourceCodeKind.Regular,
                canUseTextBuffer: _ => true);

            if (document == null)
            {
                return;
            }

            AddAdditionalDocument(document,
                isCurrentContext: LinkedFileUtilities.IsCurrentContextHierarchy(document, RunningDocumentTable));
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
            UpdateOptions();
        }

        private void SetAnalyzerRuleSet(string ruleSetFileFullPath)
        {
            if (ruleSetFileFullPath.Length != 0)
            {
                this.RuleSetFile = this.ProjectTracker.RuleSetFileProvider.GetOrCreateRuleSet(ruleSetFileFullPath);
                this.RuleSetFile.UpdatedOnDisk += OnRuleSetFileUpdateOnDisk;
            }
        }

        private void ClearAnalyzerRuleSet()
        {
            if (this.RuleSetFile != null)
            {
                this.RuleSetFile.UpdatedOnDisk -= OnRuleSetFileUpdateOnDisk;
                this.RuleSetFile = null;
            }
        }

        private void OnRuleSetFileUpdateOnDisk(object sender, EventArgs e)
        {
            var filePath = this.RuleSetFile.FilePath;

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
