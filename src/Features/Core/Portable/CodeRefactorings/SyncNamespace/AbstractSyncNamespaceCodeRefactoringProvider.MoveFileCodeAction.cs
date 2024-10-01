// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CodeRefactorings.SyncNamespace;

internal abstract partial class AbstractSyncNamespaceCodeRefactoringProvider<TNamespaceDeclarationSyntax, TCompilationUnitSyntax, TMemberDeclarationSyntax>
    : CodeRefactoringProvider
    where TNamespaceDeclarationSyntax : SyntaxNode
    where TCompilationUnitSyntax : SyntaxNode
    where TMemberDeclarationSyntax : SyntaxNode
{
    private sealed class MoveFileCodeAction(State state, ImmutableArray<string> newFolders) : CodeAction
    {
        private readonly State _state = state;
        private readonly ImmutableArray<string> _newfolders = newFolders;

        public override string Title
            => _newfolders.Length > 0
            ? string.Format(FeaturesResources.Move_file_to_0, string.Join(PathUtilities.DirectorySeparatorStr, _newfolders))
            : FeaturesResources.Move_file_to_project_root_folder;

        protected override async Task<ImmutableArray<CodeActionOperation>> ComputeOperationsAsync(
            IProgress<CodeAnalysisProgress> progress, CancellationToken cancellationToken)
        {
            var document = _state.Document;
            var solution = _state.Document.Project.Solution;
            var newDocumentId = DocumentId.CreateNewId(document.Project.Id, document.Name);

            solution = solution.RemoveDocument(document.Id);

            var text = await document.GetValueTextAsync(cancellationToken).ConfigureAwait(false);
            solution = solution.AddDocument(newDocumentId, document.Name, text, folders: _newfolders);

            return [new ApplyChangesOperation(solution), new OpenDocumentOperation(newDocumentId, activateIfAlreadyOpen: true)];
        }

        public static ImmutableArray<MoveFileCodeAction> Create(State state)
        {
            Debug.Assert(state.RelativeDeclaredNamespace != null);

            // Since all documents have identical folder structure, we can do the computation on any of them.
            var document = state.Document;
            // In case the relative namespace is "", the file should be moved to project root,
            // set `parts` to empty to indicate that.
            var parts = state.RelativeDeclaredNamespace.Length == 0
                ? []
                : state.RelativeDeclaredNamespace.Split(['.']).ToImmutableArray();

            // Invalid char can only appear in namespace name when there's error,
            // which we have checked before creating any code actions.
            Debug.Assert(parts.IsEmpty || parts.Any(static s => s.IndexOfAny(Path.GetInvalidPathChars()) < 0));

            var projectRootFolder = FolderInfo.CreateFolderHierarchyForProject(document.Project);
            var candidateFolders = FindCandidateFolders(projectRootFolder, parts, []);
            return candidateFolders.SelectAsArray(folders => new MoveFileCodeAction(state, folders));
        }

        /// <summary>
        /// We try to provide additional "move file" options if we can find existing folders that matches target namespace.
        /// For example, if the target namespace is 'DefaultNamesapce.A.B.C', and there's a folder 'ProjectRoot\A.B\' already 
        /// exists, then will provide two actions, "move file to ProjectRoot\A.B\C\" and "move file to ProjectRoot\A\B\C\".
        /// </summary>
        private static ImmutableArray<ImmutableArray<string>> FindCandidateFolders(
            FolderInfo currentFolderInfo,
            ImmutableArray<string> parts,
            ImmutableArray<string> currentFolder)
        {
            if (parts.IsEmpty)
            {
                return [currentFolder];
            }

            // Try to figure out all possible folder names that can match the target namespace.
            // For example, if the target is "A.B.C", then the matching folder names include
            // "A", "A.B" and "A.B.C". The item "index" in the result tuple is the number 
            // of items in namespace parts used to construct iten "foldername".
            var candidates = Enumerable.Range(1, parts.Length)
                .Select(i => (foldername: string.Join(".", parts.Take(i)), index: i))
                .ToImmutableDictionary(t => t.foldername, t => t.index, PathUtilities.Comparer);

            var subFolders = currentFolderInfo.ChildFolders;

            var builder = ArrayBuilder<ImmutableArray<string>>.GetInstance();
            foreach (var (folderName, index) in candidates)
            {
                if (subFolders.TryGetValue(folderName, out var matchingFolderInfo))
                {
                    var newParts = index >= parts.Length
                        ? []
                        : ImmutableArray.Create(parts, index, parts.Length - index);
                    var newCurrentFolder = currentFolder.Add(matchingFolderInfo.Name);
                    builder.AddRange(FindCandidateFolders(matchingFolderInfo, newParts, newCurrentFolder));
                }
            }

            // Make sure we always have the default path as an available option to the user 
            // (which might have been found by the search above, therefore the check here)
            // For example, if the target namespace is "A.B.C.D", and there's folder <ROOT>\A.B\,
            // the search above would only return "<ROOT>\A.B\C\D". We'd want to provide 
            // "<ROOT>\A\B\C\D" as the default path.
            var defaultPathBasedOnCurrentFolder = currentFolder.AddRange(parts);
            if (builder.All(folders => !folders.SequenceEqual(defaultPathBasedOnCurrentFolder, PathUtilities.Comparer)))
            {
                builder.Add(defaultPathBasedOnCurrentFolder);
            }

            return builder.ToImmutableAndFree();
        }

        private sealed class FolderInfo
        {
            private readonly Dictionary<string, FolderInfo> _childFolders;

            public string Name { get; }

            public IReadOnlyDictionary<string, FolderInfo> ChildFolders => _childFolders;

            private FolderInfo(string name)
            {
                Name = name;
                _childFolders = new Dictionary<string, FolderInfo>(StringComparer.Ordinal);
            }

            private void AddFolder(IEnumerable<string> folder)
            {
                if (!folder.Any())
                {
                    return;
                }

                var firstFolder = folder.First();
                if (!_childFolders.TryGetValue(firstFolder, out var firstFolderInfo))
                {
                    firstFolderInfo = new FolderInfo(firstFolder);
                    _childFolders[firstFolder] = firstFolderInfo;
                }

                firstFolderInfo.AddFolder(folder.Skip(1));
            }

            // TODO: 
            // Since we are getting folder data from documents, only non-empty folders 
            // in the project are discovered. It's possible to get complete folder structure
            // from VS but it requires UI thread to do so. We might want to revisit this later.
            public static FolderInfo CreateFolderHierarchyForProject(Project project)
            {
                var handledFolders = new HashSet<string>(StringComparer.Ordinal);

                var rootFolderInfo = new FolderInfo("<ROOT>");
                foreach (var document in project.Documents)
                {
                    var folders = document.Folders;
                    if (handledFolders.Add(string.Join(PathUtilities.DirectorySeparatorStr, folders)))
                    {
                        rootFolderInfo.AddFolder(folders);
                    }
                }

                return rootFolderInfo;
            }
        }
    }
}
