// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Composition;
using Roslyn.Utilities;
using static AnalyzerRunner.Program;

namespace AnalyzerRunner
{
    public sealed class CodeRefactoringRunner
    {
        private readonly Workspace _workspace;
        private readonly Options _options;
        private readonly ImmutableDictionary<string, ImmutableArray<CodeRefactoringProvider>> _refactorings;
        private readonly ImmutableDictionary<string, ImmutableHashSet<int>> _syntaxKinds;

        public CodeRefactoringRunner(Workspace workspace, Options options)
        {
            _workspace = workspace;
            _options = options;

            var refactorings = GetCodeRefactoringProviders(options.AnalyzerPath);
            _refactorings = FilterRefactorings(refactorings, options);

            _syntaxKinds = GetSyntaxKinds(options.RefactoringNodes);
        }

        public bool HasRefactorings => _refactorings.Any(pair => pair.Value.Any());

        public async Task RunAsync(CancellationToken cancellationToken)
        {
            if (!HasRefactorings)
            {
                return;
            }

            var solution = _workspace.CurrentSolution;
            var updatedSolution = solution;

            foreach (var project in solution.Projects)
            {
                foreach (var document in project.Documents)
                {
                    var newDocument = await RefactorDocumentAsync(document, cancellationToken).ConfigureAwait(false);
                    if (newDocument is null)
                    {
                        continue;
                    }

                    updatedSolution = updatedSolution.WithDocumentSyntaxRoot(document.Id, await newDocument.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false));
                }
            }

            if (_options.ApplyChanges)
            {
                _workspace.TryApplyChanges(updatedSolution);
            }
        }

        private async Task<Document> RefactorDocumentAsync(Document document, CancellationToken cancellationToken)
        {
            var syntaxKinds = _syntaxKinds[document.Project.Language];
            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            foreach (var node in root.DescendantNodesAndTokens(descendIntoTrivia: true))
            {
                if (!syntaxKinds.Contains(node.RawKind))
                {
                    continue;
                }

                foreach (var refactoringProvider in _refactorings[document.Project.Language])
                {
                    var codeActions = new List<CodeAction>();
                    var context = new CodeRefactoringContext(document, new TextSpan(node.SpanStart, 0), codeActions.Add, cancellationToken);
                    await refactoringProvider.ComputeRefactoringsAsync(context).ConfigureAwait(false);

                    foreach (var codeAction in codeActions)
                    {
                        var operations = await codeAction.GetOperationsAsync(
                            document.Project.Solution, CodeAnalysisProgress.None, cancellationToken).ConfigureAwait(false);
                        foreach (var operation in operations)
                        {
                            if (operation is not ApplyChangesOperation applyChangesOperation)
                            {
                                continue;
                            }

                            var changes = applyChangesOperation.ChangedSolution.GetChanges(document.Project.Solution);
                            var projectChanges = changes.GetProjectChanges().ToArray();
                            if (projectChanges.Length != 1 || projectChanges[0].ProjectId != document.Project.Id)
                            {
                                continue;
                            }

                            var documentChanges = projectChanges[0].GetChangedDocuments().ToArray();
                            if (documentChanges.Length != 1 || documentChanges[0] != document.Id)
                            {
                                continue;
                            }

                            return projectChanges[0].NewProject.GetDocument(document.Id);
                        }
                    }
                }
            }

            return null;
        }

        private static ImmutableDictionary<string, ImmutableHashSet<int>> GetSyntaxKinds(ImmutableHashSet<string> refactoringNodes)
        {
            var knownLanguages = new[]
            {
                (LanguageNames.CSharp, typeof(Microsoft.CodeAnalysis.CSharp.SyntaxKind)),
                (LanguageNames.VisualBasic, typeof(Microsoft.CodeAnalysis.VisualBasic.SyntaxKind)),
            };

            var builder = ImmutableDictionary.CreateBuilder<string, ImmutableHashSet<int>>();
            foreach (var (language, enumType) in knownLanguages)
            {
                var kindBuilder = ImmutableHashSet.CreateBuilder<int>();
                foreach (var name in refactoringNodes)
                {
                    if (!Enum.IsDefined(enumType, name))
                    {
                        continue;
                    }

                    kindBuilder.Add(Convert.ToInt32(Enum.Parse(enumType, name)));
                }

                builder.Add(language, kindBuilder.ToImmutable());
            }

            return builder.ToImmutable();
        }

        private static ImmutableDictionary<string, ImmutableArray<CodeRefactoringProvider>> FilterRefactorings(ImmutableDictionary<string, ImmutableArray<Lazy<CodeRefactoringProvider, CodeRefactoringProviderMetadata>>> refactorings, Options options)
        {
            return refactorings.ToImmutableDictionary(
                pair => pair.Key,
                pair => FilterRefactorings(pair.Value, options).ToImmutableArray());
        }

        private static IEnumerable<CodeRefactoringProvider> FilterRefactorings(IEnumerable<Lazy<CodeRefactoringProvider, CodeRefactoringProviderMetadata>> refactorings, Options options)
        {
            if (options.IncrementalAnalyzerNames.Any())
            {
                // AnalyzerRunner is running for IIncrementalAnalyzer testing. DiagnosticAnalyzer testing is disabled
                // unless /all or /a was used.
                if (!options.UseAll && options.AnalyzerNames.IsEmpty)
                {
                    yield break;
                }
            }

            if (options.RefactoringNodes.IsEmpty)
            {
                // AnalyzerRunner isn't configured to run refactorings on any nodes.
                yield break;
            }

            var refactoringTypes = new HashSet<Type>();

            foreach (var refactoring in refactorings.Select(refactoring => refactoring.Value))
            {
                if (!refactoringTypes.Add(refactoring.GetType()))
                {
                    // Avoid running the same analyzer multiple times
                    continue;
                }

                if (options.AnalyzerNames.Count == 0)
                {
                    yield return refactoring;
                }
                else if (options.AnalyzerNames.Contains(refactoring.GetType().Name))
                {
                    yield return refactoring;
                }
            }
        }

        private static ImmutableDictionary<string, ImmutableArray<Lazy<CodeRefactoringProvider, CodeRefactoringProviderMetadata>>> GetCodeRefactoringProviders(string path)
        {
            var assemblies = new List<Assembly>(MefHostServices.DefaultAssemblies);
            if (File.Exists(path))
            {
                assemblies.Add(Assembly.LoadFrom(path));
            }
            else if (Directory.Exists(path))
            {
                foreach (var file in Directory.GetFiles(path, "*.dll", SearchOption.AllDirectories))
                {
                    try
                    {
                        assemblies.Add(Assembly.LoadFrom(file));
                    }
                    catch
                    {
                        WriteLine($"Skipped assembly '{Path.GetFileNameWithoutExtension(file)}' during code refactoring discovery.", ConsoleColor.Yellow);
                    }
                }
            }

            var discovery = new AttributedPartDiscovery(Resolver.DefaultInstance, isNonPublicSupported: true);
            var parts = Task.Run(() => discovery.CreatePartsAsync(assemblies)).GetAwaiter().GetResult();
            var catalog = ComposableCatalog.Create(Resolver.DefaultInstance).AddParts(parts);

            var configuration = CompositionConfiguration.Create(catalog);
            var runtimeConfiguration = RuntimeComposition.CreateRuntimeComposition(configuration);
            var exportProviderFactory = runtimeConfiguration.CreateExportProviderFactory();

            var exportProvider = exportProviderFactory.CreateExportProvider();
            var refactorings = exportProvider.GetExports<CodeRefactoringProvider, CodeRefactoringProviderMetadata>();
            var languages = refactorings.SelectMany(refactoring => refactoring.Metadata.Languages).Distinct();
            return languages.ToImmutableDictionary(
                language => language,
                language => refactorings.WhereAsArray(refactoring => refactoring.Metadata.Languages.Contains(language)));
        }

        private class CodeRefactoringProviderMetadata
        {
            public IEnumerable<string> Languages { get; }

            public CodeRefactoringProviderMetadata(IDictionary<string, object> data)
            {
                data.TryGetValue(nameof(Languages), out var languages);

                Languages = languages switch
                {
                    IEnumerable<string> values => values,
                    string value => [value],
                    _ => Array.Empty<string>(),
                };
            }
        }
    }
}
