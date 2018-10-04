// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.PooledObjects;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CodeRefactorings.SyncNamespace
{
    internal abstract partial class AbstractSyncNamespaceCodeRefactoringProvider<TService, TNamespaceDeclarationSyntax, TCompilationUnitSyntax>
        where TService : AbstractSyncNamespaceCodeRefactoringProvider<TService, TNamespaceDeclarationSyntax, TCompilationUnitSyntax>
        where TNamespaceDeclarationSyntax : SyntaxNode
        where TCompilationUnitSyntax : SyntaxNode 
    {
        private class MoveFileCodeAction : CodeAction
        {
            private readonly Document _document;
            private readonly ImmutableArray<string> _newfolders;

            public override string Title
                => _newfolders.Length > 0 
                ? string.Format(FeaturesResources.Move_file_to_0_folder, string.Join(".", _newfolders)) 
                : FeaturesResources.Move_file_to_project_root_folder;


            public MoveFileCodeAction(Document document, ImmutableArray<string> newFolders)
            {
                _document = document;
                _newfolders = newFolders;
            }

            protected override async Task<IEnumerable<CodeActionOperation>> ComputeOperationsAsync(CancellationToken cancellationToken)
                => await MoveFileToMatchNamespaceAsync(cancellationToken).ConfigureAwait(false);

            private async Task<ImmutableArray<CodeActionOperation>> MoveFileToMatchNamespaceAsync(CancellationToken cancellationToken)
            {
                var newSolution = _document.Project.Solution.RemoveDocument(_document.Id);
                var text = await _document.GetTextAsync(cancellationToken).ConfigureAwait(false);

                var newDocumentId = DocumentId.CreateNewId(_document.Project.Id, _document.Name);
                newSolution = newSolution.AddDocument(newDocumentId, _document.Name, text: string.Empty, folders: _newfolders);
                newSolution = newSolution.WithDocumentText(newDocumentId, text, PreservationMode.PreserveIdentity);

                return ImmutableArray.Create<CodeActionOperation>(
                    new ApplyChangesOperation(newSolution),
                    new OpenDocumentOperation(newDocumentId, activateIfAlreadyOpen: true));
            }
            
            public static ImmutableArray<MoveFileCodeAction> Create(State state)
            {
                var document = state.Document;

                var parts = state.RelativeDeclaredNamespace.Split(new[] { '.' }).ToImmutableArray();
                if (parts.Any(s => s.IndexOfAny(Path.GetInvalidPathChars()) >= 0))
                {
                    return ImmutableArray<MoveFileCodeAction>.Empty;
                }

                var projectRootFolder = FolderInfo.CreateFolderHierarchyForProject(document.Project);
                var candidateFolders = FindCandidateFolders(projectRootFolder, parts, ImmutableArray<string>.Empty);
                return candidateFolders.SelectAsArray(folders => new MoveFileCodeAction(document, folders));
            }

            /// <summary>
            /// We try to provide additional "move file" options if we can find existing folders that matches target namespace.
            /// For example, if the target naemspace is 'DefaultNamesapce.A.B.C', and there's a folder 'ProjectRoot\A.B\' already exist, then will provide
            /// two actions, "move file to ProjectRoot\A.B\C\" and "move file to ProjectRoot\A\B\C\".
            /// </summary>
            private static ImmutableArray<ImmutableArray<string>> FindCandidateFolders(FolderInfo currentFolderInfo, ImmutableArray<string> parts, ImmutableArray<string> currentFolder)
            {
                if (parts.Length == 0)
                {
                    return ImmutableArray.Create(currentFolder);
                }

                var partsArray = parts.ToArray();
                var candidates = Enumerable.Range(1, parts.Length)
                    .Select(i => (foldername: string.Join(".", partsArray, 0, i), index: i))     // index is the number of items in parts used to construct key
                    .ToImmutableDictionary(t => t.foldername, t => t.index, PathUtilities.Comparer);

                var subFolders = currentFolderInfo.Folders;

                var builder = ArrayBuilder<ImmutableArray<string>>.GetInstance();
                foreach ((var folderName, var index) in candidates)
                {
                    if (subFolders.TryGetValue(folderName, out var matchingFolderInfo))
                    {
                        var newParts = index >= parts.Length
                            ? ImmutableArray<string>.Empty
                            : ImmutableArray.Create(parts, index, parts.Length - index);
                        var newCurrentFolder = currentFolder.Add(matchingFolderInfo.Name);
                        builder.AddRange(FindCandidateFolders(matchingFolderInfo, newParts, newCurrentFolder));
                    }
                }

                var defaultPathBasedOnCurrentFolder = currentFolder.AddRange(parts);
                if (builder.All(folders => !folders.SequenceEqual(defaultPathBasedOnCurrentFolder, PathUtilities.Comparer)))
                {
                    builder.Add(defaultPathBasedOnCurrentFolder);
                }

                return builder.ToImmutableAndFree();
            }

            private class FolderInfo
            {
                Dictionary<string, FolderInfo> _folders;

                public string Name { get; }

                public FolderInfo Parent { get; }

                public IReadOnlyDictionary<string, FolderInfo> Folders => _folders;

                private FolderInfo(string name, FolderInfo parent)
                {
                    Name = name;
                    Parent = parent;
                    _folders = new Dictionary<string, FolderInfo>(StringComparer.Ordinal);
                }

                public void AddFolder(ImmutableArray<string> folder)
                {
                    if (folder.IsDefaultOrEmpty)
                    {
                        return;
                    }

                    var firstFolder = folder[0];
                    if (!_folders.TryGetValue(firstFolder, out var firstFolderInfo))
                    {
                        firstFolderInfo = new FolderInfo(firstFolder, this);
                        _folders[firstFolder] = firstFolderInfo;
                    }

                    firstFolderInfo.AddFolder(ImmutableArray.CreateRange(folder.Skip(1)));
                }

                // TODO: 
                // Since we are getting folder data from documents, only non-empty folders 
                // in the project are discovered. It's possible get complete folder structure
                // from VS but it requires UI thread to do so. We might want to revisit this
                // later.
                public static FolderInfo CreateFolderHierarchyForProject(Project project)
                {
                    var rootFolderInfo = new FolderInfo("<ROOT>", parent: null);
                    foreach (var document in project.Documents)
                    {
                        rootFolderInfo.AddFolder(document.Folders.ToImmutableArray());
                    }
                    return rootFolderInfo;
                }
            }
        }
    }
}
