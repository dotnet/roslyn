// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.ChangeNamespace;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.LanguageServices;
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
            /// <summary>
            /// The document in which the refactoring is triggered.
            /// </summary>
            public Document Document { get; }

            /// <summary>
            /// The applicable container node based on cursor location,
            /// which will be used to change namespace.
            /// </summary>
            public SyntaxNode Container { get; }

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
                Document document,
                SyntaxNode container,
                string targetNamespace,
                string relativeDeclaredNamespace)
            {
                Document = document;
                Container = container;
                TargetNamespace = targetNamespace;
                RelativeDeclaredNamespace = relativeDeclaredNamespace;
            }

            public static async Task<State> CreateAsync(
                AbstractSyncNamespaceCodeRefactoringProvider<TNamespaceDeclarationSyntax, TCompilationUnitSyntax, TMemberDeclarationSyntax> provider,
                Document document,
                TextSpan textSpan,
                CancellationToken cancellationToken)
            {
                // User must put cursor on one of the nodes described below to trigger the refactoring. 
                // For each scenario, all requirements must be met. Some of them are checked by `TryGetApplicableInvocationNodeAsync`, 
                // rest by `IChangeNamespaceService.CanChangeNamespaceAsync`.
                // 
                // - A namespace declaration node that is the only namespace declaration in the document and all types are declared in it:
                //    1. No nested namespace declarations (even it's empty).
                //    2. The cursor is on the name of the namespace declaration.
                //    3. The name of the namespace is valid (i.e. no errors).
                //    4. No partial type declared in the namespace. Otherwise its multiple declaration will
                //       end up in different namespace.
                //
                // - A compilation unit node that contains no namespace declaration:
                //    1. The cursor is on the name of first declared type.
                //    2. No partial type declared in the document. Otherwise its multiple declaration will
                //       end up in different namespace.

                var applicableNode = await provider.TryGetApplicableInvocationNodeAsync(document, textSpan, cancellationToken).ConfigureAwait(false);
                if (applicableNode == null)
                {
                    return null;
                }

                var changenameSpaceService = document.GetLanguageService<IChangeNamespaceService>();
                var canChange = await changenameSpaceService.CanChangeNamespaceAsync(document, applicableNode, cancellationToken).ConfigureAwait(false);

                if (!canChange || !IsDocumentPathRootedInProjectFolder(document))
                {
                    return null;
                }

                var syntaxFacts = document.GetLanguageService<ISyntaxFactsService>();
                var solution = document.Project.Solution;

                // We can't determine what the expected namespace would be without knowing the default namespace.
                var defaultNamespace = GetDefaultNamespace(document, syntaxFacts);
                if (defaultNamespace == null)
                {
                    return null;
                }

                string declaredNamespace;
                if (applicableNode is TCompilationUnitSyntax)
                {
                    declaredNamespace = string.Empty;
                }
                else if (applicableNode is TNamespaceDeclarationSyntax)
                {
                    var syntaxGenerator = SyntaxGenerator.GetGenerator(document);
                    declaredNamespace = syntaxGenerator.GetName(applicableNode);
                }
                else
                {
                    throw ExceptionUtilities.Unreachable;
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

                return new State(document, applicableNode, targetNamespace, relativeNamespace);
            }

            /// <summary>
            /// Determines if the actual file path matches its logical path in project 
            /// which is constructed as [project_root_path]\Logical\Folders\. The refactoring 
            /// is triggered only when the two match. The reason of doing this is we don't really know
            /// the user's intention of keeping the file path out-of-sync with its logical path.
            /// </summary>
            private static bool IsDocumentPathRootedInProjectFolder(Document document)
            {
                var projectRoot = PathUtilities.GetDirectoryName(document.Project.FilePath);
                var folderPath = Path.Combine(document.Folders.ToArray());

                var absoluteDircetoryPath = PathUtilities.GetDirectoryName(document.FilePath);
                var logicalDirectoryPath = PathUtilities.CombineAbsoluteAndRelativePaths(projectRoot, folderPath);

                return PathUtilities.PathsEqual(absoluteDircetoryPath, logicalDirectoryPath);
            }

            private static string GetDefaultNamespace(Document document, ISyntaxFactsService syntaxFacts)
            {
                var solution = document.Project.Solution;
                var linkedIds = document.GetLinkedDocumentIds();
                var documents = linkedIds.SelectAsArray(id => solution.GetDocument(id)).Add(document);

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

            /// <summary>
            /// Try get the relative namespace for <paramref name="namespace"/> based on <paramref name="relativeTo"/>,
            /// if <paramref name="relativeTo"/> is the containing namespace of <paramref name="namespace"/>. Otherwise,
            /// Returns null.
            /// For example:
            /// - If <paramref name="relativeTo"/> is "A.B" and <paramref name="namespace"/> is "A.B.C.D", then
            /// the relative namespace is "C.D".
            /// - If <paramref name="relativeTo"/> is "A.B" and <paramref name="namespace"/> is also "A.B", then
            /// the relative namespace is "".
            /// - If <paramref name="relativeTo"/> is "" then the relative namespace us <paramref name="namespace"/>.
            /// </summary>
            private static string GetRelativeNamespace(string relativeTo, string @namespace, ISyntaxFactsService syntaxFacts)
            {
                Debug.Assert(relativeTo != null && @namespace != null);

                if (syntaxFacts.StringComparer.Equals(@namespace, relativeTo))
                {
                    return string.Empty;
                }
                else if (relativeTo.Length == 0)
                {
                    return @namespace;
                }
                else if (relativeTo.Length >= @namespace.Length)
                {
                    return null;
                }

                var containingText = relativeTo + ".";
                var namespacePrefix = @namespace.Substring(0, containingText.Length);

                return syntaxFacts.StringComparer.Equals(containingText, namespacePrefix)
                    ? @namespace.Substring(relativeTo.Length + 1)
                    : null;
            }
        }
    }
}
