// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.ProjectManagement;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CodeRefactorings.SyncNamespace
{
    internal abstract partial class AbstractSyncNamespaceCodeRefactoringProvider<TService, TNamespaceDeclarationSyntax, TCompilationUnitSyntax>
        where TService : AbstractSyncNamespaceCodeRefactoringProvider<TService, TNamespaceDeclarationSyntax, TCompilationUnitSyntax>
        where TNamespaceDeclarationSyntax : SyntaxNode
        where TCompilationUnitSyntax : SyntaxNode
    {
        private class State
        {                                                                        
            private static SymbolDisplayFormat QualifiedNameOnlyFormat => 
                new SymbolDisplayFormat(globalNamespaceStyle: SymbolDisplayGlobalNamespaceStyle.Omitted,
                                        typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces);

            public Document Document { get;  }

            public string RootNamespace { get; }

            public string DeclaredNamespace { get; }
                                                                                                       
            /// <summary>
            /// This is the new name we want to change the namespace to use.
            /// Empty string means global namespace, whereas null means renaming action is not availalbe.
            /// </summary>
            public string TargetNamespace { get; }

            public string QualifiedIdentifierFromDeclaration { get; }

            public string QualifiedIdentifierFromFolders { get; }

            private State(
                Document document,
                string rootNamespce,
                string qualifiedIdentifierFromFolders,
                string targetNamespace,
                string declaredNamespace,                           
                string qualifiedIdentifierFromDeclaration)
            {                      
                Document = document;
                RootNamespace = rootNamespce;
                QualifiedIdentifierFromFolders = qualifiedIdentifierFromFolders;
                TargetNamespace = targetNamespace;
                DeclaredNamespace = declaredNamespace;
                QualifiedIdentifierFromDeclaration = qualifiedIdentifierFromDeclaration;
            }

            private static bool IsLinkedDocument(Document document)
            {
                // TODO: figure out how to properly determine if a document is linked.
                if (document.GetLinkedDocumentIds().Any())
                {
                    return true;
                }

                var fileName = Path.GetFileName(document.FilePath);
                var projectRoot = Path.GetDirectoryName(document.Project.FilePath);
                var folderPath = Path.Combine(document.Folders.ToArray());

                var expectFilePath = Path.Combine(projectRoot, folderPath, fileName);
                var IdForDocumentsWithExpectedPath = document.Project.Solution.GetDocumentIdsWithFilePath(expectFilePath);

                return IdForDocumentsWithExpectedPath.Length != 1 || document.Id != IdForDocumentsWithExpectedPath[0];
            }

            public static async Task<State> CreateAsync(TService service, Document document, TextSpan textSpan, CancellationToken cancellationToken)
            {
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

                // Ignore linked documents
                // TODO: this only detect documents linked multiple times, need to figure out how to detect a document is linked, e.g. using <Link>, or shared project, etc.
                if (IsLinkedDocument(document))
                {
                    return null;
                }

                var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);

                // Bail if cursor is not at the proper location.
                if (!service.ShouldPositionTriggerRefactoring(root, textSpan.Start, out var namespaceDeclaration))
                {
                    return null;
                }

                // Bail if we can't get the root namespace for the project, the refactoring depends on it.
                var rootNamespace = await GetRootNamespaceAsync(document.Project, cancellationToken).ConfigureAwait(false);
                if (rootNamespace == null)
                {
                    return null;
                }

                // Namespace can't be renamed if we can't construct a valid qualified identifier from folder names.
                // In this case, we might still be able to provide refactoring to move file to new location.
                var syntaxFacts = document.GetLanguageService<ISyntaxFactsService>();
                var namespaceFromFolders = TryBuildNamespaceFromFolders(service, document.Folders, syntaxFacts);
                var targetNamespace = namespaceFromFolders == null ? null : ConcatNamespace(rootNamespace, namespaceFromFolders);

                var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false); 
                var namespaceSymbol = semanticModel.GetDeclaredSymbol(namespaceDeclaration, cancellationToken) as INamespaceSymbol;
                if (namespaceSymbol == null)
                {
                    return null;
                }
                var declaredNamespace = namespaceSymbol.ToDisplayString(QualifiedNameOnlyFormat);

                //TODO: Handle partial definitions.

                // No action required if namespace already matches folders.
                if (string.Equals(targetNamespace, declaredNamespace, System.StringComparison.Ordinal))
                {
                    return null;
                }

                // Only provide "move file" action if root namespace contains declared namespace.
                // It makes no sense to match folder hierarchy with namespace if it's not rooted at the root namespace of the project. 
                TryGetRelativeNamespace(rootNamespace, declaredNamespace, out var qualifiedIdentifierFromDeclaration);                                                                           
                                                                                                                          
                return new State(document, rootNamespace, namespaceFromFolders, targetNamespace, declaredNamespace, qualifiedIdentifierFromDeclaration);
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
