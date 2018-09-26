// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.FindSymbols;
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
                if (document.GetLinkedDocumentIds().Any())
                {
                    return null;
                }

                var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);

                // Bail if multiple namespaces are declared in the document (including nested declarations)
                if (root.DescendantNodes().OfType<TNamespaceDeclarationSyntax>().Count() > 1)
                {
                    return null;
                }

                // Bail if cursor is not on namespace declaration.
                // TODO: Note that this also means we will ignore document with only global namespace.
                var syntaxFacts = document.GetLanguageService<ISyntaxFactsService>();
                if (!syntaxFacts.IsOnNamespaceDeclaration(root, textSpan.Start))
                {
                    return null;
                }

                var rootNamespace = await GetRootNamespaceAsync(document.Project, cancellationToken).ConfigureAwait(false);
                if (rootNamespace == null)
                {
                    return null;
                }

                var namespaceDeclaration = root.FindToken(textSpan.Start).GetAncestor<TNamespaceDeclarationSyntax>();
                Debug.Assert(namespaceDeclaration != null);

                var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false); 
                var namespaceSymbol = semanticModel.GetDeclaredSymbol(namespaceDeclaration, cancellationToken) as INamespaceSymbol;
                if (namespaceSymbol == null)
                {
                    return null;
                }

                var declaredNamespace = namespaceSymbol.ToDisplayString(QualifiedNameOnlyFormat);

                // Namespace can't be renamed if we can't construct a valid qualified identifier from folder names.
                // In this case, we might still be able to provide refactoring to move file to new location.
                var targetNamespace = TryBuildNamespaceFromFolders(document.Folders, syntaxFacts, out var qualifiedIdentifierFromFolders) 
                                        ? ConcatNamespace(rootNamespace, qualifiedIdentifierFromFolders)
                                        : null;

                // No action required if namespace already matches folders.
                if (string.Equals(targetNamespace, declaredNamespace, System.StringComparison.Ordinal))
                {
                    return null;
                }

                // Only provide "move file" action if root namespace is a proper prefix of declared namespace.
                IsProperPrefix(declaredNamespace, rootNamespace, out var qualifiedIdentifierFromDeclaration);
                                                                           
                // Only provide "change namespace" action if it wouldn't cause conflicts in target namespace.
                if (targetNamespace != null && 
                    (await WillCauseConflictInTargetNamespace(document.Project, targetNamespace, service.GetDeclaredSymbols(namespaceDeclaration, semanticModel, cancellationToken), cancellationToken).ConfigureAwait(false)))
                {
                    targetNamespace = null;
                }                                                                                                           
                return new State(document, rootNamespace, qualifiedIdentifierFromFolders, targetNamespace, declaredNamespace, qualifiedIdentifierFromDeclaration);
            }


            private static async Task<bool> WillCauseConflictInTargetNamespace(Project project, string targetNamespace, ImmutableArray<ISymbol> symbols, CancellationToken cancellationToken)
            {
                var compilation = await project.GetCompilationAsync(cancellationToken).ConfigureAwait(false);
                if (!TryGetQualifiedNamespaceWithOutConflict(compilation, targetNamespace, out var namespaceSymbol))
                {
                    return true;
                }

                // Can't cause conflict if target namespace doesn't exist yet.
                if ((object)namespaceSymbol == null)
                {
                    return false;
                }

                var nameArityMap = new Dictionary<string, List<int>>(StringComparer.Ordinal);
                foreach (var symbol in symbols)
                {
                    if (nameArityMap.TryGetValue(symbol.Name, out var arities))
                    {
                        arities.Add(symbol.GetArity());
                    }
                    else
                    {
                        nameArityMap[symbol.Name] = new List<int>() { symbol.GetArity() };
                    }
                }

                foreach (var name in nameArityMap.Keys)
                {
                    foreach (var memberWithName in namespaceSymbol.GetMembers(name))
                    {
                        if (nameArityMap.TryGetValue(memberWithName.Name, out var arities) && arities.Contains(memberWithName.GetArity()))
                        {
                            return true;
                        }
                    };
                }

                return false;
            }

            private static bool TryGetQualifiedNamespaceWithOutConflict(Compilation compilation, string qualifiedNamespaceName, out INamespaceSymbol namespaceSymbol)
            {
                namespaceSymbol = compilation.GlobalNamespace;
                if (qualifiedNamespaceName == string.Empty)
                {
                    return true;
                }

                foreach (var name in qualifiedNamespaceName.Split('.'))
                {
                    var membersWithName = namespaceSymbol.GetMembers(name);
                    if (membersWithName.Any())
                    {
                        foreach (var member in membersWithName)
                        {
                            switch (member)
                            {
                                case INamespaceSymbol namespaceMember:
                                    namespaceSymbol = namespaceMember;
                                    break;
                                case INamedTypeSymbol typeMember:
                                    if (typeMember.Arity == 0)
                                    {
                                        // The namespace confict with an existing type declaration.
                                        namespaceSymbol = null;
                                        return false;
                                    }
                                    break;
                                default:
                                    Debug.Assert(false, member.Kind.ToString());
                                    break;
                            }
                        }
                    }
                    else
                    {
                        namespaceSymbol = null;
                    }

                    // No type conflict but matcing namespace is not declared.
                    if (namespaceSymbol == null)
                    {
                        break;
                    }
                }
                return true;
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
            private static bool TryBuildNamespaceFromFolders(IEnumerable<string> folders, ISyntaxFactsService syntaxFacts, out string @namespace)
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
                    // prefix the part with '@' if it's a keyword
                    var escapedPart = syntaxFacts.IsKeyword(part) ? "@" + part : part; 
                    if (!syntaxFacts.IsValidIdentifier(escapedPart))
                    {
                        builder.Free();
                        @namespace = null;
                        return false;
                    }
                    builder.Builder.Append(escapedPart);
                }

                @namespace = builder.ToStringAndFree();
                return true;
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
