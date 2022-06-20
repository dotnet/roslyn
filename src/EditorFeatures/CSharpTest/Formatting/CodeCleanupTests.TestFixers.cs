﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Formatting
{
    public partial class CodeCleanupTests
    {
        private abstract class TestThirdPartyCodeFix : CodeFixProvider
        {
            public override ImmutableArray<string> FixableDiagnosticIds { get; } = ImmutableArray.Create("HasDefaultCase");

            public override Task RegisterCodeFixesAsync(CodeFixContext context)
            {
                foreach (var diagnostic in context.Diagnostics)
                {
                    context.RegisterCodeFix(
                        CodeAction.Create(
                            "Remove default case",
                            async cancellationToken =>
                            {
                                var root = await context.Document.GetSyntaxRootAsync(cancellationToken);
                                Assumes.NotNull(root);
                                var sourceTree = diagnostic.Location.SourceTree;
                                Assumes.NotNull(sourceTree);
                                var node = (await sourceTree.GetRootAsync(cancellationToken)).FindNode(diagnostic.Location.SourceSpan);
                                Assumes.NotNull(node?.Parent);
                                var newRoot = root.RemoveNode(node.Parent, SyntaxRemoveOptions.KeepNoTrivia);
                                Assumes.NotNull(newRoot);
                                return context.Document.WithSyntaxRoot(newRoot);
                            },
                            nameof(TestThirdPartyCodeFix)),
                        diagnostic);
                }

                return Task.CompletedTask;
            }
        }

        [PartNotDiscoverable, Shared, ExportCodeFixProvider(LanguageNames.CSharp)]
        private class TestThirdPartyCodeFixWithFixAll : TestThirdPartyCodeFix
        {
            [ImportingConstructor]
            [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
            public TestThirdPartyCodeFixWithFixAll()
            {
            }

            public override FixAllProvider GetFixAllProvider() => BatchFixAllProvider.Instance;
        }

        [PartNotDiscoverable, Shared, ExportCodeFixProvider(LanguageNames.CSharp)]
        private class TestThirdPartyCodeFixWithOutFixAll : TestThirdPartyCodeFix
        {
            [ImportingConstructor]
            [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
            public TestThirdPartyCodeFixWithOutFixAll()
            {
            }
        }

        [PartNotDiscoverable, Shared, ExportCodeFixProvider(LanguageNames.CSharp)]
        private class TestThirdPartyCodeFixModifiesSolution : TestThirdPartyCodeFix
        {
            [ImportingConstructor]
            [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
            public TestThirdPartyCodeFixModifiesSolution()
            {
            }

            public override FixAllProvider GetFixAllProvider() => new ModifySolutionFixAll();

            private class ModifySolutionFixAll : FixAllProvider
            {
                public override Task<CodeAction?> GetFixAsync(FixAllContext fixAllContext)
                {
                    var solution = fixAllContext.Solution;
                    return Task.FromResult<CodeAction?>(CodeAction.Create(
                            "Remove default case",
                            async cancellationToken =>
                            {
                                var toFix = await fixAllContext.GetDocumentDiagnosticsToFixAsync();
                                Project? project = null;
                                foreach (var kvp in toFix)
                                {
                                    var document = kvp.Key;
                                    project ??= document.Project;
                                    var diagnostics = kvp.Value;
                                    var root = await document.GetSyntaxRootAsync(cancellationToken);
                                    Assumes.NotNull(root);
                                    foreach (var diagnostic in diagnostics)
                                    {
                                        var sourceTree = diagnostic.Location.SourceTree;
                                        Assumes.NotNull(sourceTree);
                                        var node = (await sourceTree.GetRootAsync(cancellationToken)).FindNode(diagnostic.Location.SourceSpan);
                                        Assumes.NotNull(node?.Parent);
                                        var newRoot = root.RemoveNode(node.Parent, SyntaxRemoveOptions.KeepNoTrivia);
                                        Assumes.NotNull(newRoot);
                                        document = document.WithSyntaxRoot(newRoot);
                                    }

                                    solution = solution.WithDocumentText(document.Id, await document.GetTextAsync());
                                }

                                Assumes.NotNull(project);
                                return solution.AddDocument(DocumentId.CreateNewId(project.Id), "new.cs", SourceText.From(""));
                            },
                            nameof(TestThirdPartyCodeFix)));
                }
            }
        }

        [PartNotDiscoverable, Shared, ExportCodeFixProvider(LanguageNames.CSharp)]
        private class TestThirdPartyCodeFixDoesNotSupportDocumentScope : TestThirdPartyCodeFix
        {
            [ImportingConstructor]
            [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
            public TestThirdPartyCodeFixDoesNotSupportDocumentScope()
            {
            }

            public override FixAllProvider GetFixAllProvider() => new ModifySolutionFixAll();

            private class ModifySolutionFixAll : FixAllProvider
            {
                public override IEnumerable<FixAllScope> GetSupportedFixAllScopes()
                {
                    return new[] { FixAllScope.Project, FixAllScope.Solution, FixAllScope.Custom };
                }

                public override Task<CodeAction?> GetFixAsync(FixAllContext fixAllContext)
                {
                    var solution = fixAllContext.Solution;
                    return Task.FromResult<CodeAction?>(CodeAction.Create(
                            "Remove default case",
                            async cancellationToken =>
                            {
                                var toFix = await fixAllContext.GetDocumentDiagnosticsToFixAsync();
                                Project? project = null;
                                foreach (var kvp in toFix)
                                {
                                    var document = kvp.Key;
                                    project ??= document.Project;
                                    var diagnostics = kvp.Value;
                                    var root = await document.GetSyntaxRootAsync(cancellationToken);
                                    Assumes.NotNull(root);
                                    foreach (var diagnostic in diagnostics)
                                    {
                                        var sourceTree = diagnostic.Location.SourceTree;
                                        Assumes.NotNull(sourceTree);
                                        var node = (await sourceTree.GetRootAsync(cancellationToken)).FindNode(diagnostic.Location.SourceSpan);
                                        Assumes.NotNull(node?.Parent);
                                        var newRoot = root.RemoveNode(node.Parent, SyntaxRemoveOptions.KeepNoTrivia);
                                        Assumes.NotNull(newRoot);
                                        document = document.WithSyntaxRoot(newRoot);
                                    }

                                    solution = solution.WithDocumentText(document.Id, await document.GetTextAsync());
                                }

                                Assumes.NotNull(project);
                                return solution.AddDocument(DocumentId.CreateNewId(project.Id), "new.cs", SourceText.From(""));
                            },
                            nameof(TestThirdPartyCodeFix)));
                }
            }
        }
    }
}
