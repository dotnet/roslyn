// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

// Disable nullable in legacy test framework
#nullable disable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Analyzer.Utilities.Extensions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;
using Xunit;

namespace Test.Utilities
{
    public abstract class DiagnosticAnalyzerTestBase
    {
        protected static readonly CompilationOptions s_CSharpDefaultOptions = new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary);

        internal const string DefaultFilePathPrefix = "Test";
        internal const string CSharpDefaultFileExt = "cs";
        protected static readonly string CSharpDefaultFilePath = DefaultFilePathPrefix + 0 + "." + CSharpDefaultFileExt;

        private const string TestProjectName = "TestProject";

        protected DiagnosticAnalyzerTestBase()
        {
        }

        protected static Project CreateProject(string[] sources)
        {
            return CreateProject(sources.ToFileAndSource());
        }

        private static Project CreateProject(FileAndSource[] sources)
        {
            string fileNamePrefix = DefaultFilePathPrefix;
            string fileExt = CSharpDefaultFileExt;
            CompilationOptions options = s_CSharpDefaultOptions;

            ProjectId projectId = ProjectId.CreateNewId(debugName: TestProjectName);

            var defaultReferences = ReferenceAssemblies.NetFramework.Net48.Default;
            var references = Task.Run(() => defaultReferences.ResolveAsync(LanguageNames.CSharp, CancellationToken.None)).GetAwaiter().GetResult();

#pragma warning disable CA2000 // Dispose objects before losing scope - Current solution/project takes the dispose ownership of the created AdhocWorkspace
            Project project = new AdhocWorkspace().CurrentSolution
#pragma warning restore CA2000 // Dispose objects before losing scope
                .AddProject(projectId, TestProjectName, TestProjectName, LanguageNames.CSharp)
                .AddMetadataReferences(projectId, references)
                .AddMetadataReference(projectId, AdditionalMetadataReferences.CodeAnalysisReference)
                .AddMetadataReference(projectId, AdditionalMetadataReferences.WorkspacesReference)
                .AddMetadataReference(projectId, AdditionalMetadataReferences.SystemWebReference)
                .AddMetadataReference(projectId, AdditionalMetadataReferences.SystemRuntimeSerialization)
                .AddMetadataReference(projectId, AdditionalMetadataReferences.SystemDirectoryServices)
                .AddMetadataReference(projectId, AdditionalMetadataReferences.SystemXaml)
                .AddMetadataReference(projectId, AdditionalMetadataReferences.PresentationFramework)
                .AddMetadataReference(projectId, AdditionalMetadataReferences.SystemWebExtensions)
                .WithProjectCompilationOptions(projectId, options)
                .WithProjectParseOptions(projectId, null)
                .GetProject(projectId);

            // Enable Flow-Analysis feature on the project
            var parseOptions = project.ParseOptions.WithFeatures(
                project.ParseOptions.Features.Concat(
                    new[] { new KeyValuePair<string, string>("flow-analysis", "true") }));
            project = project.WithParseOptions(parseOptions);

            MetadataReference symbolsReference = AdditionalMetadataReferences.CSharpSymbolsReference;
            project = project.AddMetadataReference(symbolsReference);

            project = project.AddMetadataReference(AdditionalMetadataReferences.SystemCollectionsImmutableReference);
            project = project.AddMetadataReference(AdditionalMetadataReferences.SystemXmlDataReference);

            int count = 0;
            foreach (FileAndSource source in sources)
            {
                string newFileName = source.FilePath ?? fileNamePrefix + count++ + "." + fileExt;
                DocumentId documentId = DocumentId.CreateNewId(projectId, debugName: newFileName);
                project = project.AddDocument(newFileName, SourceText.From(source.Source)).Project;
            }

            return project;
        }

        protected static List<SyntaxNode> GetSyntaxNodeList(SyntaxTree syntaxTree)
        {
            return GetSyntaxNodeList(syntaxTree.GetRoot(), null);
        }

        protected static List<SyntaxNode> GetSyntaxNodeList(SyntaxNode node, List<SyntaxNode> synList)
        {
            if (synList == null)
                synList = new List<SyntaxNode>();

            synList.Add(node);

            foreach (var child in node.ChildNodesAndTokens())
            {
                if (child.IsNode)
                    synList = GetSyntaxNodeList(child.AsNode(), synList);
            }

            return synList;
        }

        protected static (IOperation operation, SemanticModel model, SyntaxNode node) GetOperationAndSyntaxForTest<TSyntaxNode>(CSharpCompilation compilation)
    where TSyntaxNode : SyntaxNode
        {
            var tree = compilation.SyntaxTrees[0];
            var model = compilation.GetSemanticModel(tree);
            SyntaxNode syntaxNode = GetSyntaxNodeOfTypeForBinding<TSyntaxNode>(GetSyntaxNodeList(tree));
            if (syntaxNode == null)
            {
                return (null, null, null);
            }

            var operation = model.GetOperation(syntaxNode);
            if (operation != null)
            {
                Assert.Same(model, operation.SemanticModel);
            }
            return (operation, model, syntaxNode);
        }

        protected const string StartString = "/*<bind>*/";
        protected const string EndString = "/*</bind>*/";

        protected static TNode GetSyntaxNodeOfTypeForBinding<TNode>(List<SyntaxNode> synList) where TNode : SyntaxNode
        {
            foreach (var node in synList.OfType<TNode>())
            {
                string exprFullText = node.ToFullString();
                exprFullText = exprFullText.Trim();

                if (exprFullText.StartsWith(StartString, StringComparison.Ordinal))
                {
                    if (exprFullText.Contains(EndString))
                    {
                        if (exprFullText.EndsWith(EndString, StringComparison.Ordinal))
                        {
                            return node;
                        }
                        else
                        {
                            continue;
                        }
                    }
                    else
                    {
                        return node;
                    }
                }

                if (exprFullText.EndsWith(EndString, StringComparison.Ordinal))
                {
                    if (exprFullText.Contains(StartString))
                    {
                        if (exprFullText.StartsWith(StartString, StringComparison.Ordinal))
                        {
                            return node;
                        }
                        else
                        {
                            continue;
                        }
                    }
                    else
                    {
                        return node;
                    }
                }
            }

            return null;
        }

        public static FileAndSource GetEditorConfigAdditionalFile(string source)
            => new FileAndSource() { Source = source, FilePath = ".editorconfig" };
    }

    // Justification for suppression: We are not going to compare FileAndSource objects for equality.
#pragma warning disable CA1815 // Override equals and operator equals on value types
    public struct FileAndSource
#pragma warning restore CA1815 // Override equals and operator equals on value types
    {
        public string FilePath { get; set; }
        public string Source { get; set; }
    }
}
