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
                => $"Move file to \"{string.Join(".", _newfolders)}\" folder";

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

                var builder = ArrayBuilder<ImmutableArray<string>>.GetInstance();
                var parts = state.RelativeDeclaredNamespace.Split(new[] { '.' }).ToImmutableArray();

                if (parts.Any(s => s.IndexOfAny(Path.GetInvalidPathChars()) >= 0))
                {
                    return ImmutableArray<MoveFileCodeAction>.Empty;
                }

                var projectRootPath = PathUtilities.GetDirectoryName(document.Project.FilePath);
                FindCandidateFolders(new DirectoryInfo(projectRootPath), parts, ImmutableArray<string>.Empty, builder);

                var candidateFolders = builder.ToImmutableAndFree();
                if (candidateFolders.All(folders => !folders.SequenceEqual(parts, PathUtilities.Comparer)))
                {
                    candidateFolders = candidateFolders.Add(parts);
                }

                return candidateFolders.SelectAsArray(folders => new MoveFileCodeAction(document, folders));
            }

            private static void FindCandidateFolders(DirectoryInfo currentDirInfo, ImmutableArray<string> parts, ImmutableArray<string> currentFolder, ArrayBuilder<ImmutableArray<string>> builder)
            {
                if (parts.Length == 0)
                {
                    builder.Add(currentFolder);
                    return;
                }

                var partsArray = parts.ToArray();
                var candidates = Enumerable.Range(1, parts.Length)
                    .Select(i => (foldername: string.Join(".", partsArray, 0, i), index: i))     // index is the number of items in parts used to construct key
                    .ToImmutableDictionary(t => t.foldername, t => t.index, PathUtilities.Comparer);

                ImmutableArray<DirectoryInfo> subDirectoryInfos = default;
                try
                {
                    subDirectoryInfos = currentDirInfo.EnumerateDirectories().ToImmutableArray();
                }
                catch (Exception)
                {
                    // Ignore all expcetions might be thrown from examining file system.
                }

                if (subDirectoryInfos.IsDefault)
                {
                    return;
                }

                foreach (var subDirInfo in subDirectoryInfos)
                {
                    var dirName = subDirInfo.Name;
                    if (candidates.TryGetValue(dirName, out var index))
                    {
                        var newParts = index >= parts.Length 
                            ? ImmutableArray<string>.Empty
                            : ImmutableArray.Create(parts, index, parts.Length - index);
                        var newCurrentFolder = currentFolder.Add(dirName);
                        FindCandidateFolders(subDirInfo, newParts, newCurrentFolder, builder);
                    }
                }
                
            }
        }
    }
}
