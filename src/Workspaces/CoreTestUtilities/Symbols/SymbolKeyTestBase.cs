// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Symbols;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Test.Utilities;
using Roslyn.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.UnitTests.Symbols
{
    [UseExportProvider]
    public abstract class SymbolKeyTestBase
    {
        private static readonly MetadataReference[] s_metadataReferences = new[]
        {
            MetadataReference.CreateFromFile(typeof(string).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(Enumerable).Assembly.Location)
        };

        protected abstract string LanguageName { get; }
        protected abstract ParseOptions CreateParseOptions();

        private async Task<(Document document, TSyntax syntax)> GetSyntaxAsync<TSyntax>(string code)
            where TSyntax : SyntaxNode
        {
            MarkupTestFile.GetPosition(code, out var output, out int position);

            var workspace = new AdhocWorkspace();

            var projectId = ProjectId.CreateNewId(debugName: "TestProject");
            var documentId = DocumentId.CreateNewId(projectId, debugName: "TestFile");

            var text = SourceText.From(output);
            var textAndVersion = TextAndVersion.Create(text, VersionStamp.Default);
            var loader = TextLoader.From(textAndVersion);

            var documentInfo = DocumentInfo.Create(
                documentId,
                "TestFile",
                loader: loader);

            var projectInfo = ProjectInfo.Create(
                projectId,
                version: VersionStamp.Default,
                name: "TestProject",
                assemblyName: "TestProject",
                language: LanguageName,
                parseOptions: CreateParseOptions(),
                documents: SpecializedCollections.SingletonEnumerable(documentInfo),
                metadataReferences: s_metadataReferences);

            var project = workspace.AddProject(projectInfo);
            var document = project.GetDocument(documentId);

            var syntaxRoot = await document.GetSyntaxRootAsync();
            var token = syntaxRoot.FindToken(position);

            return (document, token.Parent.FirstAncestorOrSelf<TSyntax>());
        }

        private static string GetSymbolKey(ISymbol symbol, bool useNew)
            => useNew
                ? SymbolKeyBuilder.Create(symbol)
                : SymbolKey.GetEncodedSymbolData(symbol);

        protected async Task AssertSymbolKeyCreatedFromDeclaredSymbol<TSyntax>(string code, string expectedSymbolKey, bool useNew = true)
            where TSyntax : SyntaxNode
        {
            var (document, syntax) = await GetSyntaxAsync<TSyntax>(code);

            var semanticModel = await document.GetSemanticModelAsync();
            var symbol = semanticModel.GetDeclaredSymbol(syntax);
            var symbolKey = GetSymbolKey(symbol, useNew);

            Assert.Equal(expectedSymbolKey, symbolKey);
        }

        protected async Task AssertSymbolKeyCreatedFromSymbolInfo<TSyntax>(string code, string expectedSymbolKey, bool useNew = true)
            where TSyntax : SyntaxNode
        {
            var (document, syntax) = await GetSyntaxAsync<TSyntax>(code);

            var semanticModel = await document.GetSemanticModelAsync();
            var symbol = semanticModel.GetSymbolInfo(syntax).Symbol;
            var symbolKey = GetSymbolKey(symbol, useNew);

            Assert.Equal(expectedSymbolKey, symbolKey);
        }

        protected async Task AssertSymbolKeyCreatedFromSymbolInfo<TSyntax>(string code, string expectedSymbolKey, Func<ISymbol, ISymbol> symbolFinder, bool useNew = true)
            where TSyntax : SyntaxNode
        {
            var (document, syntax) = await GetSyntaxAsync<TSyntax>(code);

            var semanticModel = await document.GetSemanticModelAsync();
            var symbol = symbolFinder(semanticModel.GetSymbolInfo(syntax).Symbol);
            var symbolKey = GetSymbolKey(symbol, useNew);

            Assert.Equal(expectedSymbolKey, symbolKey);
        }

        protected async Task AssertSymbolKeyCreatedFromTypeInfo<TSyntax>(string code, string expectedSymbolKey, bool useNew = true)
            where TSyntax : SyntaxNode
        {
            var (document, syntax) = await GetSyntaxAsync<TSyntax>(code);

            var semanticModel = await document.GetSemanticModelAsync();
            var symbol = semanticModel.GetTypeInfo(syntax).Type;
            var symbolKey = GetSymbolKey(symbol, useNew);

            Assert.Equal(expectedSymbolKey, symbolKey);
        }

        protected async Task AssertSymbolKeyResolvesToDeclaredSymbol<TSyntax>(string code, string symbolKey)
            where TSyntax : SyntaxNode
        {
            var (document, syntax) = await GetSyntaxAsync<TSyntax>(code);

            var compilation = await document.Project.GetCompilationAsync();
            var semanticModel = await document.GetSemanticModelAsync();
            var symbol = semanticModel.GetDeclaredSymbol(syntax);

            var resolvedSymbol = SymbolKeyResolver.Resolve(symbolKey, compilation);

            Assert.Same(symbol, resolvedSymbol.Symbol);
        }

        protected async Task AssertSymbolKeyWithDeclaredSymbol<TSyntax>(string code, string symbolKey, bool useNew = true)
            where TSyntax : SyntaxNode
        {
            await AssertSymbolKeyCreatedFromDeclaredSymbol<TSyntax>(code, symbolKey, useNew);
            await AssertSymbolKeyResolvesToDeclaredSymbol<TSyntax>(code, symbolKey);
        }
    }
}
