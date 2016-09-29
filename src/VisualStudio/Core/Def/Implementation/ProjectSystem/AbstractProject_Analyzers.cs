// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Host;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.Shell.Interop;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem
{
    internal partial class AbstractProject
    {
        private AnalyzerFileWatcherService _analyzerFileWatcherService = null;
        private AnalyzerDependencyCheckingService _dependencyCheckingService = null;

        public void AddAnalyzerReference(string analyzerAssemblyFullPath)
        {
            if (CurrentProjectAnalyzersContains(analyzerAssemblyFullPath))
            {
                return;
            }

            var fileChangeService = (IVsFileChangeEx)this.ServiceProvider.GetService(typeof(SVsFileChangeEx));
            if (Workspace == null)
            {
                // This can happen only in tests.
                var testAnalyzer = new VisualStudioAnalyzer(analyzerAssemblyFullPath, fileChangeService, this.HostDiagnosticUpdateSource, this.Id, this.Workspace, loader: null, language: this.Language);
                this.AddOrUpdateAnalyzer(analyzerAssemblyFullPath, testAnalyzer);
                return;
            }

            var analyzerLoader = Workspace.Services.GetRequiredService<IAnalyzerService>().GetLoader();
            analyzerLoader.AddDependencyLocation(analyzerAssemblyFullPath);
            var analyzer = new VisualStudioAnalyzer(analyzerAssemblyFullPath, fileChangeService, this.HostDiagnosticUpdateSource, this.Id, this.Workspace, analyzerLoader, this.Language);
            this.AddOrUpdateAnalyzer(analyzerAssemblyFullPath, analyzer);

            if (_pushingChangesToWorkspaceHosts)
            {
                var analyzerReference = analyzer.GetReference();
                this.ProjectTracker.NotifyWorkspaceHosts(host => host.OnAnalyzerReferenceAdded(Id, analyzerReference));

                List<VisualStudioAnalyzer> existingReferencesWithLoadErrors = GetCurrentAnalyzers().Where(a => a.HasLoadErrors).ToList();

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

        public void RemoveAnalyzerReference(string analyzerAssemblyFullPath)
        {
            VisualStudioAnalyzer analyzer;
            if (!TryGetAnalyzer(analyzerAssemblyFullPath, out analyzer))
            {
                return;
            }

            if (Workspace == null)
            {
                // This can happen only in tests.
                RemoveAnalyzer(analyzerAssemblyFullPath);
                analyzer.Dispose();
                return;
            }

            GetAnalyzerFileWatcherService().RemoveAnalyzerAlreadyLoadedDiagnostics(Id, analyzerAssemblyFullPath);

            RemoveAnalyzer(analyzerAssemblyFullPath);

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

        public void AddAdditionalFile(string additionalFilePath, Func<IVisualStudioHostDocument, bool> getIsInCurrentContext)
        {
            var document = this.DocumentProvider.TryGetDocumentForFile(
                this,
                filePath: additionalFilePath,
                sourceCodeKind: SourceCodeKind.Regular,
                getFolderNames: _ => SpecializedCollections.EmptyReadOnlyList<string>(),
                canUseTextBuffer: _ => true,
                updatedOnDiskHandler: s_additionalDocumentUpdatedOnDiskEventHandler,
                openedHandler: s_additionalDocumentOpenedEventHandler,
                closingHandler: s_additionalDocumentClosingEventHandler);

            if (document == null)
            {
                return;
            }

            AddAdditionalDocument(document, isCurrentContext: getIsInCurrentContext(document));
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
            ResetArgumentsAndUpdateOptions();
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

        // internal for testing purpose.
        internal void OnRuleSetFileUpdateOnDisk(object sender, EventArgs e)
        {
            var filePath = this.RuleSetFile.FilePath;

            ResetAnalyzerRuleSet(filePath);
        }

        private AnalyzerFileWatcherService GetAnalyzerFileWatcherService()
        {
            if (_analyzerFileWatcherService == null)
            {
                var componentModel = (IComponentModel)this.ServiceProvider.GetService(typeof(SComponentModel));
                Interlocked.CompareExchange(ref _analyzerFileWatcherService, componentModel.GetService<AnalyzerFileWatcherService>(), null);
            }

            return _analyzerFileWatcherService;
        }

        private AnalyzerDependencyCheckingService GetAnalyzerDependencyCheckingService()
        {
            if (_dependencyCheckingService == null)
            {
                var componentModel = (IComponentModel)this.ServiceProvider.GetService(typeof(SComponentModel));
                Interlocked.CompareExchange(ref _dependencyCheckingService, componentModel.GetService<AnalyzerDependencyCheckingService>(), null);
            }

            return _dependencyCheckingService;
        }
    }
}
