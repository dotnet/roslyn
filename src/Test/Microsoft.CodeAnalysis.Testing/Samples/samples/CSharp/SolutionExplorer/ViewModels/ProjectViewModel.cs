using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace MSBuildWorkspaceTester.ViewModels
{
    internal class ProjectViewModel : HierarchyItemViewModel
    {
        private readonly ProjectId _projectId;
        private readonly Dictionary<string, FolderViewModel> _folders;

        public ProjectViewModel(Workspace workspace, ProjectId projectId)
            : base(workspace, isExpanded: false)
        {
            _projectId = projectId;
            _folders = new Dictionary<string, FolderViewModel>(StringComparer.OrdinalIgnoreCase);

            Solution solution = workspace.CurrentSolution;
            Project project = solution.GetProject(projectId);

            foreach (DocumentId documentId in project.DocumentIds)
            {
                DocumentViewModel documentViewModel = new DocumentViewModel(workspace, documentId);

                IReadOnlyList<string> folders = solution.GetDocument(documentId).Folders;
                if (folders.Count > 0)
                {
                    FolderViewModel folderViewModel = GetOrCreateFolder(workspace, folders);
                    folderViewModel.AddChild(documentViewModel);
                }
                else
                {
                    AddChild(documentViewModel);
                }
            }

            // Add root folders
            foreach (KeyValuePair<string, FolderViewModel> folder in _folders)
            {
                if (!folder.Key.Contains('\\'))
                {
                    AddChild(folder.Value);
                }
            }

            ReferencesFolderViewModel referencesFolderViewModel = null;

            // Add references
            if (project.MetadataReferences.Count > 0)
            {
                referencesFolderViewModel = new ReferencesFolderViewModel(workspace);

                foreach (MetadataReference metadataReference in project.MetadataReferences)
                {
                    MetadataReferenceViewModel metadataReferenceViewModel = new MetadataReferenceViewModel(workspace, metadataReference);
                    referencesFolderViewModel.AddChild(metadataReferenceViewModel);
                }
            }

            if (project.ProjectReferences.Any())
            {
                if (referencesFolderViewModel == null)
                {
                    referencesFolderViewModel = new ReferencesFolderViewModel(workspace);
                }

                foreach (ProjectReference projectReference in project.ProjectReferences)
                {
                    ProjectReferenceViewModel projectReferenceViewModel = new ProjectReferenceViewModel(workspace, projectReference);
                    referencesFolderViewModel.AddChild(projectReferenceViewModel);
                }
            }

            if (referencesFolderViewModel != null)
            {
                AddChild(referencesFolderViewModel);
            }
        }

        private FolderViewModel GetOrCreateFolder(Workspace workspace, IReadOnlyList<string> folders)
        {
            string folderPath = string.Join(@"\", folders);

            if (_folders.TryGetValue(folderPath, out FolderViewModel folderViewModel))
            {
                return folderViewModel;
            }

            string currentFolderPath = folderPath;

            foreach (string folder in folders.Reverse())
            {
                FolderViewModel newFolderViewModel = new FolderViewModel(workspace, folder);

                if (folderViewModel != null)
                {
                    newFolderViewModel.AddChild(folderViewModel);
                }

                folderViewModel = newFolderViewModel;
                _folders.Add(currentFolderPath, folderViewModel);

                int lastSlashIndex = currentFolderPath.LastIndexOf('\\');
                if (lastSlashIndex < 0)
                {
                    break;
                }

                string parentFolderPath = currentFolderPath.Substring(0, lastSlashIndex);

                if (_folders.TryGetValue(parentFolderPath, out FolderViewModel parentFolderViewModel))
                {
                    parentFolderViewModel.AddChild(folderViewModel);
                    return folderViewModel;
                }

                currentFolderPath = parentFolderPath;
            }

            return _folders[folderPath];
        }

        protected override string GetDisplayName()
            => Workspace.CurrentSolution.GetProject(_projectId).Name;

        public string Language
            => Workspace.CurrentSolution.GetProject(_projectId).Language;
    }
}
