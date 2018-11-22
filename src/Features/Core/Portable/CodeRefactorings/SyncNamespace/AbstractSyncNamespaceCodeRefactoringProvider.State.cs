// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CodeRefactorings.SyncNamespace
{
    internal abstract partial class AbstractSyncNamespaceCodeRefactoringProvider<TNamespaceDeclarationSyntax, TCompilationUnitSyntax, TMemberDeclarationSyntax>
        : CodeRefactoringProvider
        where TNamespaceDeclarationSyntax : SyntaxNode
        where TCompilationUnitSyntax : SyntaxNode
        where TMemberDeclarationSyntax : SyntaxNode
    {
        internal sealed class State
        {                                                                        
            private static readonly SymbolDisplayFormat s_qualifiedNameOnlyFormat = 
                new SymbolDisplayFormat(globalNamespaceStyle: SymbolDisplayGlobalNamespaceStyle.Omitted,
                                        typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces);

            public Solution Solution { get; }

            /// <summary>
            /// The document in which the refactoring is triggered.
            /// </summary>
            public DocumentId OriginalDocumentId { get; }

            /// <summary>
            /// The refactoring is also enabled for document in a multi-targeting project, 
            /// which is the only form of linked document allowed. This property returns IDs
            /// of the original document that triggered the refactoring plus every such linked 
            /// documents.
            /// </summary>
            public ImmutableArray<DocumentId> DocumentIds { get;  }

            /// <summary>
            /// This is the default namespace defined in the project file.
            /// </summary>
            public string DefaultNamespace { get; }

            /// <summary>
            /// This is the name of the namespace declaration that trigger the refactoring.
            /// </summary>
            public string DeclaredNamespace { get; }
                                                                                                       
            /// <summary>
            /// This is the new name we want to change the namespace to.
            /// Empty string means global namespace, whereas null means change namespace action is not available.
            /// </summary>
            public string TargetNamespace { get; }

            /// <summary>
            /// This is the part of the declared namespace that is contained in default namespace.
            /// We will use this to construct target folder to move the file to.
            /// For example, if default namespace is `A` and declared namespace is `A.B.C`, 
            /// this would be `B.C`.
            /// </summary>
            public string RelativeDeclaredNamespace { get; }

            private State(
                Solution solution,
                DocumentId originalDocumentId,
                ImmutableArray<DocumentId> documentIds,
                string rootNamespce,
                string targetNamespace,
                string declaredNamespace,                           
                string relativeDeclaredNamespace)
            {
                Solution = solution;
                OriginalDocumentId = originalDocumentId;
                DocumentIds = documentIds;
                DefaultNamespace = rootNamespce;
                TargetNamespace = targetNamespace;
                DeclaredNamespace = declaredNamespace;
                RelativeDeclaredNamespace = relativeDeclaredNamespace;
            }

            /// <summary>
            /// This refactoring only supports non-linked document and linked document in the form of
            /// documents in multi-targeting project. Also for simplicity, we also don't support document
            /// what has different file path and logical path in project (i.e. [ProjectRoot] + `Document.Folders`). 
            /// If the requirements above is met, we will return IDs of all documents linked to the specified 
            /// document (inclusive), an array of single element will be returned for non-linked document.
            /// </summary>
            private static bool IsSupportedLinkedDocument(Document document, out ImmutableArray<DocumentId> allDocumentIds)
            {
                var solution = document.Project.Solution;
                var linkedDocumentids = document.GetLinkedDocumentIds();

                // TODO: figure out how to properly determine if and how a document is linked using project system.

                // If we found a linked document which is part of a project with differenct project file,
                // then it's an actual linked file (i.e. not a multi-targeting project). We don't support that, because 
                // we don't know which default namespace and folder path we should use to construct target
                // namespace.
                if (linkedDocumentids.Any(id => 
                        !PathUtilities.PathsEqual(solution.GetDocument(id).Project.FilePath, document.Project.FilePath)))
                {
                    allDocumentIds = default;
                    return false;
                }

                // Now determine if the actual file path matches its logical path in project 
                // which is constructed as <project root path>\Logical\Folders\. The refactoring 
                // is triggered only when the two match. The reason of doing this is we don't really know
                // the user's intention of keeping the file path out-of-sync with its logical path.
                var projectRoot = PathUtilities.GetDirectoryName(document.Project.FilePath);
                var folderPath = Path.Combine(document.Folders.ToArray());

                var absoluteDircetoryPath = PathUtilities.GetDirectoryName(document.FilePath);
                var logicalDirectoryPath = PathUtilities.CombineAbsoluteAndRelativePaths(projectRoot, folderPath);

                if (PathUtilities.PathsEqual(absoluteDircetoryPath, logicalDirectoryPath))
                {
                    allDocumentIds = linkedDocumentids.Add(document.Id);
                    return true;
                }
                else
                {
                    allDocumentIds = default;
                    return false;
                }
            }

            private static string GetDefaultNamespace(ImmutableArray<Document> documents, ISyntaxFactsService syntaxFacts)
            {
                // For all projects containing all the linked documents, bail if 
                // 1. Any of them doesn't have default namespace, or
                // 2. Multiple default namespace are found. (this might be possible by tweaking project file).
                // The refactoring depends on a single default namespace to operate.
                var defaultNamespaceFromProjects = new HashSet<string>(
                    documents.Select(d => d.Project.DefaultNamespace),
                    syntaxFacts.StringComparer);

                if (defaultNamespaceFromProjects.Count != 1
                    || defaultNamespaceFromProjects.First() == null)
                {
                    return default;
                }

                return defaultNamespaceFromProjects.Single();
            }

            private static async Task<(bool shouldTrigger, string declaredNamespace)> TryGetNamespaceDeclarationAsync(
                TextSpan textSpan,
                ImmutableArray<Document> documents,
                AbstractSyncNamespaceCodeRefactoringProvider<TNamespaceDeclarationSyntax, TCompilationUnitSyntax, TMemberDeclarationSyntax> provider,
                CancellationToken cancellationToken)
            {
                // If the cursor location doesn't meet the requirement to trigger the refactoring in any of the documents 
                // (See `ShouldPositionTriggerRefactoringAsync`), or we are getting different namespace declarations among 
                // those documents, then we know we can't make a proper code change. We will return false and the refactoring 
                // will then bail. We use span of namespace declaration found in each document to decide if they are identical.

                var spansForNamespaceDeclaration = PooledDictionary<TextSpan, TNamespaceDeclarationSyntax>.GetInstance();

                try
                {
                    foreach (var document in documents)
                    {
                        var compilationUnitOrNamespaceDeclOpt = await provider.TryGetApplicableInvocationNode(document, textSpan.Start, cancellationToken)
                            .ConfigureAwait(false);

                        if (compilationUnitOrNamespaceDeclOpt is TNamespaceDeclarationSyntax namespaceDeclaration)
                        {
                            spansForNamespaceDeclaration[namespaceDeclaration.Span] = namespaceDeclaration;
                        }
                        else if (compilationUnitOrNamespaceDeclOpt is TCompilationUnitSyntax)
                        {
                            // In case there's no namespace declaration in the document, we used an empty span as key, 
                            // since a valid namespace declaration node can't have zero length.
                            spansForNamespaceDeclaration[default] = null;
                        }
                        else
                        {
                            return default;
                        }
                    }

                    if (spansForNamespaceDeclaration.Count != 1)
                    {
                        return default;
                    }

                    var namespaceDecl = spansForNamespaceDeclaration.Values.Single();
                    var declaredNamespace = namespaceDecl == null
                        // namespaceDecl == null means the target namespace is global namespace.
                        ? string.Empty
                        // Since the node in each document has identical type and span, 
                        // they should have same name.
                        : SyntaxGenerator.GetGenerator(documents.First()).GetName(namespaceDecl);

                    return (true, declaredNamespace);
                }
                finally
                {
                    spansForNamespaceDeclaration.Free();
                }
            }

            public static async Task<State> CreateAsync(
                AbstractSyncNamespaceCodeRefactoringProvider<TNamespaceDeclarationSyntax, TCompilationUnitSyntax, TMemberDeclarationSyntax> provider, 
                Document document, 
                TextSpan textSpan, 
                CancellationToken cancellationToken)
            {
                if (document.Project.FilePath == null
                    || !textSpan.IsEmpty
                    || document.Project.Solution.Workspace.Kind == WorkspaceKind.MiscellaneousFiles
                    || document.IsGeneratedCode(cancellationToken))
                {
                    return null;
                }

                if (!IsSupportedLinkedDocument(document, out var documentIds))
                {
                    return null;
                }

                var syntaxFacts = document.GetLanguageService<ISyntaxFactsService>();
                var solution = document.Project.Solution;
                var documents = documentIds.SelectAsArray(id => solution.GetDocument(id));

                var defaultNamespace = GetDefaultNamespace(documents, syntaxFacts);
                if (defaultNamespace == null)
                {
                    return null;
                }

                var (shouldTrigger, declaredNamespace) = 
                    await TryGetNamespaceDeclarationAsync(textSpan, documents, provider, cancellationToken).ConfigureAwait(false);

                if (!shouldTrigger)
                {
                    return null;
                }

                // Namespace can't be changed if we can't construct a valid qualified identifier from folder names.
                // In this case, we might still be able to provide refactoring to move file to new location.
                var namespaceFromFolders = TryBuildNamespaceFromFolders(provider, document.Folders, syntaxFacts);
                var targetNamespace = namespaceFromFolders == null 
                    ? null 
                    : ConcatNamespace(defaultNamespace, namespaceFromFolders);      

                // No action required if namespace already matches folders.
                if (syntaxFacts.StringComparer.Equals(targetNamespace, declaredNamespace))
                {
                    return null;
                }

                // Only provide "move file" action if default namespace contains declared namespace.
                // For example, if the default namespace is `Microsoft.CodeAnalysis`, and declared
                // namespace is `System.Diagnostics`, it's very likely this document is an outlier  
                // in the project and user probably has some special rule for it.
                var relativeNamespace = GetRelativeNamespace(defaultNamespace, declaredNamespace, syntaxFacts);                                                                           
                                                                                                                          
                return new State(
                    solution, 
                    document.Id, 
                    documentIds, 
                    defaultNamespace, 
                    targetNamespace, 
                    declaredNamespace, 
                    relativeNamespace);
            }

            /// <summary>
            /// Create a qualified identifier as the suffix of namespace based on a list of folder names.
            /// </summary>
            private static string TryBuildNamespaceFromFolders(
                AbstractSyncNamespaceCodeRefactoringProvider<TNamespaceDeclarationSyntax, TCompilationUnitSyntax, TMemberDeclarationSyntax> service, 
                IEnumerable<string> folders, 
                ISyntaxFactsService syntaxFacts)
            {
                var parts = folders.SelectMany(folder => folder.Split(new[] { '.' }).SelectAsArray(service.EscapeIdentifier));
                return parts.All(syntaxFacts.IsValidIdentifier) ? string.Join(".", parts) : null;
            } 

            private static string ConcatNamespace(string rootNamespace, string namespaceSuffix)
            {
                Debug.Assert(rootNamespace != null && namespaceSuffix != null);
                if (namespaceSuffix.Length == 0)
                {
                    return rootNamespace;
                }
                else if (rootNamespace.Length == 0)
                {
                    return namespaceSuffix;
                }
                else
                {
                    return rootNamespace + "." + namespaceSuffix;
                }
            }
        }
    }
}
