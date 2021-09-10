// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
    [Export]
    [ExportWorkspaceService(typeof(IAddSolutionItemService)), Shared]
    internal partial class VisualStudioAddSolutionItemService : IAddSolutionItemService
    {
        private const string SolutionItemsFolderName = "Solution Items";

        private readonly object _gate = new();
        private readonly IThreadingContext _threadingContext;
        private readonly ConcurrentDictionary<string, FileChangeTracker> _fileChangeTrackers;

        private DTE? _dte;
        private IVsFileChangeEx? _fileChangeService;

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public VisualStudioAddSolutionItemService(
            IThreadingContext threadingContext)
        {
            _threadingContext = threadingContext;
            _fileChangeTrackers = new ConcurrentDictionary<string, FileChangeTracker>(StringComparer.OrdinalIgnoreCase);
        }

        public void Initialize(IServiceProvider serviceProvider)
        {
            _dte = (DTE)serviceProvider.GetService(typeof(DTE));
            _fileChangeService = (IVsFileChangeEx)serviceProvider.GetService(typeof(SVsFileChangeEx));
        }

        public void TrackFilePathAndAddSolutionItemWhenFileCreated(string filePath)
        {
            if (_fileChangeService != null &&
                PathUtilities.IsAbsolute(filePath) &&
                FileExistsWithGuard(filePath) == false)
            {
                // Setup a new file change tracker to track file path and 
                // add newly created file as solution item.
                _fileChangeTrackers.GetOrAdd(filePath, CreateTracker);
            }

            return;

            // Local functions
            FileChangeTracker CreateTracker(string filePath)
            {
                var tracker = new FileChangeTracker(_fileChangeService, filePath, _VSFILECHANGEFLAGS.VSFILECHG_Add);
                tracker.UpdatedOnDisk += OnFileAdded;
                _ = tracker.StartFileChangeListeningAsync();
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
            if (_dte == null)
            {
                return;
            }

            await _threadingContext.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

            lock (_gate)
            {
                var solution = (Solution2)_dte.Solution;
                if (!TryGetExistingSolutionItemsFolder(solution, filePath, out var solutionItemsFolder, out var hasExistingSolutionItem))
                {
                    solutionItemsFolder = solution.AddSolutionFolder("Solution Items");
                }

                if (!hasExistingSolutionItem &&
                    solutionItemsFolder != null &&
                    FileExistsWithGuard(filePath) == true)
                {
                    solutionItemsFolder.ProjectItems.AddFromFile(filePath);
                    solution.SaveAs(solution.FileName);
                }
            }
        }

        private static bool? FileExistsWithGuard(string filePath)
        {
            try
            {
                return File.Exists(filePath);
            }
            catch (IOException)
            {
                return null;
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
