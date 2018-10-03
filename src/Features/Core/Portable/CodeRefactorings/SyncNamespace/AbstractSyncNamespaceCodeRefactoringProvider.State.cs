// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.ProjectManagement;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CodeRefactorings.SyncNamespace
{
    internal abstract partial class AbstractSyncNamespaceCodeRefactoringProvider<TService, TNamespaceDeclarationSyntax, TCompilationUnitSyntax>
        where TService : AbstractSyncNamespaceCodeRefactoringProvider<TService, TNamespaceDeclarationSyntax, TCompilationUnitSyntax>
        where TNamespaceDeclarationSyntax : SyntaxNode
        where TCompilationUnitSyntax : SyntaxNode
    {
        private class State
        {                                                                        
            private static readonly SymbolDisplayFormat s_qualifiedNameOnlyFormat = 
                new SymbolDisplayFormat(globalNamespaceStyle: SymbolDisplayGlobalNamespaceStyle.Omitted,
                                        typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces);

            public Document Document { get;  }

            public string RootNamespace { get; }

            public string DeclaredNamespace { get; }
                                                                                                       
            /// <summary>
            /// This is the new name we want to change the namespace to.
            /// Empty string means global namespace, whereas null means renaming action is not available.
            /// </summary>
            public string TargetNamespace { get; }

            public string RelativeDeclaredNamespace { get; }

            public string RelativeTargetNamespace { get; }

            private State(
                Document document,
                string rootNamespce,
                string relativeTargetNamespace,
                string targetNamespace,
                string declaredNamespace,                           
                string relativeDeclaredNamespace)
            {                      
                Document = document;
                RootNamespace = rootNamespce;
                RelativeTargetNamespace = relativeTargetNamespace;
                TargetNamespace = targetNamespace;
                DeclaredNamespace = declaredNamespace;
                RelativeDeclaredNamespace = relativeDeclaredNamespace;
            }

            private static bool IsLinkedDocument(Document document)
            {
                var solution = document.Project.Solution;

                // TODO: figure out how to properly determine if a document is linked using project system.

                // If we found a linked document which is part of a project with differenct project file,
                // then it's an actual linked file (i.e. not a MTFM project).
                if (document.GetLinkedDocumentIds()
                    .Any(id => !PathUtilities.PathsEqual(solution.GetDocument(id).Project.FilePath, document.Project.FilePath)))
                {
                    return true;
                }
                
                // Now determine if the file path match its logical path in workspace,
                // only trigger the refactoring when they match.
                var projectRoot = PathUtilities.GetDirectoryName(document.Project.FilePath);
                var folderPath = Path.Combine(document.Folders.ToArray());

                var absoluteDircetoryPath = PathUtilities.GetDirectoryName(document.FilePath);
                var logicalDirectoryPath = PathUtilities.CombineAbsoluteAndRelativePaths(projectRoot, folderPath);

                return !PathUtilities.PathsEqual(absoluteDircetoryPath, logicalDirectoryPath);
            }

            public static async Task<State> CreateAsync(TService service, Document document, TextSpan textSpan, CancellationToken cancellationToken)
            {
                if (document.Project.FilePath == null)
                {
                    return null;
                }

                if (!textSpan.IsEmpty)
                {
                    return null;
                }

                var workspace = document.Project.Solution.Workspace;
                if (workspace.Kind == WorkspaceKind.MiscellaneousFiles)
                {
                    return null;
                }

                if (document.IsGeneratedCode(cancellationToken))
                {
                    return null;
                }

                // Bail if we can't get the root namespace for the project, the refactoring depends on it.
                var rootNamespace = document.Project.DefaultNamespace;
                if (rootNamespace == null)
                {
                    return null;
                }

                // Ignore linked documents
                // TODO: this only detect documents linked multiple times, need to figure out how to detect a document is linked, e.g. using <Link>, or shared project, etc.
                if (IsLinkedDocument(document))
                {
                    return null;
                }

                // Bail if cursor is not at the proper location.
                (var shouldTriggerRefactoring, var namespaceDeclaration) = 
                    await service.ShouldPositionTriggerRefactoringAsync(document, textSpan.Start, cancellationToken).ConfigureAwait(false);

                if (!shouldTriggerRefactoring)
                {
                    return null;
                }

                // Namespace can't be renamed if we can't construct a valid qualified identifier from folder names.
                // In this case, we might still be able to provide refactoring to move file to new location.
                var syntaxFacts = document.GetLanguageService<ISyntaxFactsService>();
                var namespaceFromFolders = TryBuildNamespaceFromFolders(service, document.Folders, syntaxFacts);
                var targetNamespace = namespaceFromFolders == null ? null : ConcatNamespace(rootNamespace, namespaceFromFolders);                

                // namespaceDeclaration == null means the target namespace is global namespace.
                var declaredNamespace = namespaceDeclaration == null
                    ? string.Empty
                    : SyntaxGenerator.GetGenerator(document).GetName(namespaceDeclaration);

                // No action required if namespace already matches folders.
                if (syntaxFacts.StringComparer.Equals(targetNamespace, declaredNamespace))
                {
                    return null;
                }

                // Only provide "move file" action if root namespace contains declared namespace.
                // It makes no sense to match folder hierarchy with namespace if it's not rooted at the root namespace of the project. 
                TryGetRelativeNamespace(rootNamespace, declaredNamespace, out var relativeNamespace);                                                                           
                                                                                                                          
                return new State(document, rootNamespace, namespaceFromFolders, targetNamespace, declaredNamespace, relativeNamespace);
            }

            private static async Task<string> GetRootNamespaceAsync(Project project, CancellationToken cancellationToken)
            {                                                                               
                var projectManagementService = project.Solution.Workspace.Services.GetService<IProjectManagementService>();
                if (projectManagementService == null)
                {
                    return null;
                }
                return await projectManagementService.GetDefaultNamespaceAsync(project, project.Solution.Workspace, cancellationToken).ConfigureAwait(false);
            }

            /// <summary>
            /// Create a qualified identifier as the suffix of namespace based on a list of folder names.
            /// </summary>
            private static string TryBuildNamespaceFromFolders(TService serivice, IEnumerable<string> folders, ISyntaxFactsService syntaxFacts)
            {
                var isFirst = true;
                var builder = PooledStringBuilder.GetInstance();
                foreach (var part in folders.SelectMany(folder => folder.Split(new[] { '.' })))
                {
                    if (isFirst)
                    {
                        isFirst = false;
                    }
                    else
                    {
                        builder.Builder.Append(".");
                    }
                    
                    var escapedPart = serivice.EscapeIdentifier(part);
                    if (!syntaxFacts.IsValidIdentifier(escapedPart))
                    {
                        builder.Free();                        
                        return null;
                    }
                    builder.Builder.Append(escapedPart);
                }
                return builder.ToStringAndFree();
            } 

            private static string ConcatNamespace(string rootnamespace, string namespaceSuffix)
            {
                Debug.Assert(rootnamespace != null && namespaceSuffix != null);
                if (namespaceSuffix.Length == 0)
                {
                    return rootnamespace;
                }
                else if (rootnamespace.Length == 0)
                {
                    return namespaceSuffix;
                }
                else
                {
                    return rootnamespace + "." + namespaceSuffix;
                }
            }
        }
    }
}
