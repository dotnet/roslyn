// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#nullable enable

using System;
using System.Collections.Concurrent;
using System.Composition;
using System.IO;
using System.Threading;
using EnvDTE;
using EnvDTE80;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Roslyn.Utilities;
using Task = System.Threading.Tasks.Task;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem
{
    [ExportWorkspaceService(typeof(IAddSolutionItemService)), Shared]
    internal partial class VisualStudioAddSolutionItemService : IAddSolutionItemService
    {
        private const string SolutionItemsFolderName = "Solution Items";

        private readonly object _gate = new object();
        private readonly IThreadingContext _threadingContext;
        private readonly DTE _dte;
        private readonly IVsFileChangeEx _fileChangeService;
        private readonly ConcurrentDictionary<string, FileChangeTracker> _fileChangeTrackers;

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public VisualStudioAddSolutionItemService(
            IThreadingContext threadingContext,
            SVsServiceProvider serviceProvider)
        {
            _threadingContext = threadingContext;
            _dte = (DTE)serviceProvider.GetService(typeof(DTE));
            _fileChangeService = (IVsFileChangeEx)serviceProvider.GetService(typeof(SVsFileChangeEx));
            _fileChangeTrackers = new ConcurrentDictionary<string, FileChangeTracker>(StringComparer.OrdinalIgnoreCase);
        }

        public Task TrackFilePathAndAddSolutionItemWhenFileCreatedAsync(string filePath, CancellationToken cancellationToken)
        {
            if (!PathUtilities.IsAbsolute(filePath))
            {
                return Task.CompletedTask;
            }

            if (File.Exists(filePath))
            {
                // File already created, so directly add the file as solution item.
                return AddSolutionItemAsync(filePath, cancellationToken);
            }

            // Otherwise, setup a new file change tracker to track file path and 
            // add newly created file as solution item.
            var tracker = _fileChangeTrackers.GetOrAdd(filePath, CreateTracker);
            return Task.CompletedTask;

            // Local functions
            FileChangeTracker CreateTracker(string filePath)
            {
                var tracker = new FileChangeTracker(_fileChangeService, filePath, _VSFILECHANGEFLAGS.VSFILECHG_Add);
                tracker.UpdatedOnDisk += OnFileAdded;
                tracker.StartFileChangeListeningAsync();
                return tracker;
            }
        }

        private void OnFileAdded(object sender, EventArgs e)
        {
            var tracker = (FileChangeTracker)sender;
            var filePath = tracker.FilePath;

            _fileChangeTrackers.TryRemove(filePath, out _);

            AddSolutionItemAsync(filePath, CancellationToken.None).Wait();

            tracker.UpdatedOnDisk -= OnFileAdded;
            tracker.Dispose();
        }

        public async Task AddSolutionItemAsync(string filePath, CancellationToken cancellationToken)
        {
            await _threadingContext.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

            var solution = (Solution2)_dte.Solution;

            lock (_gate)
            {
                if (!TryGetExistingSolutionItemsFolder(solution, filePath, out var solutionItemsFolder, out var hasExistingSolutionItem))
                {
                    solutionItemsFolder = solution.AddSolutionFolder("Solution Items");
                }

                if (!hasExistingSolutionItem &&
                    solutionItemsFolder != null &&
                    File.Exists(filePath))
                {
                    solutionItemsFolder.ProjectItems.AddFromFile(filePath);
                    solution.SaveAs(solution.FileName);
                }
            }
        }

        public static bool TryGetExistingSolutionItemsFolder(Solution2 solution, string filePath, out EnvDTE.Project? solutionItemsFolder, out bool hasExistingSolutionItem)
        {
            solutionItemsFolder = null;
            hasExistingSolutionItem = false;

            var fileName = PathUtilities.GetFileName(filePath);
            foreach (Project project in solution.Projects)
            {
                if (project.Kind == EnvDTE.Constants.vsProjectKindSolutionItems &&
                    project.Name == SolutionItemsFolderName)
                {
                    solutionItemsFolder = project;

                    foreach (ProjectItem projectItem in solutionItemsFolder.ProjectItems)
                    {
                        if (fileName == projectItem.Name)
                        {
                            hasExistingSolutionItem = true;
                            break;
                        }
                    }

                    return true;
                }
            }

            return false;
        }
    }
}
