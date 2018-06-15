// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Threading;
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

        private async Task<(Document document, TSyntax syntax)> GetSyntaxAsync<TSyntax>(string code, string expected)
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

        protected async Task AssertDeclaredSymbol<TSyntax>(string code, string expected, bool useNew = true)
            where TSyntax : SyntaxNode
        {
            var (document, syntax) = await GetSyntaxAsync<TSyntax>(code, expected);

            var semanticModel = await document.GetSemanticModelAsync();
            var symbol = semanticModel.GetDeclaredSymbol(syntax);

            var encodedSymbolData = useNew
                ? SymbolKeyWriter.Write(symbol, CancellationToken.None)
                : SymbolKey.GetEncodedSymbolData(symbol);

            Assert.Equal(expected, encodedSymbolData);
        }

        protected async Task AssertSymbol<TSyntax>(string code, string expected, bool useNew = true)
            where TSyntax : SyntaxNode
        {
            var (document, syntax) = await GetSyntaxAsync<TSyntax>(code, expected);

            var semanticModel = await document.GetSemanticModelAsync();
            var symbol = semanticModel.GetSymbolInfo(syntax).Symbol;

            var encodedSymbolData = useNew
                ? SymbolKeyWriter.Write(symbol, CancellationToken.None)
                : SymbolKey.GetEncodedSymbolData(symbol);

            Assert.Equal(expected, encodedSymbolData);
        }

        protected async Task AssertSymbol<TSyntax>(string code, string expected, Func<ISymbol, ISymbol> symbolFinder, bool useNew = true)
            where TSyntax : SyntaxNode
        {
            var (document, syntax) = await GetSyntaxAsync<TSyntax>(code, expected);

            var semanticModel = await document.GetSemanticModelAsync();
            var symbol = symbolFinder(semanticModel.GetSymbolInfo(syntax).Symbol);

            var encodedSymbolData = useNew
                ? SymbolKeyWriter.Write(symbol, CancellationToken.None)
                : SymbolKey.GetEncodedSymbolData(symbol);

            Assert.Equal(expected, encodedSymbolData);
        }

        protected async Task AssertType<TSyntax>(string code, string expected, bool useNew = true)
            where TSyntax : SyntaxNode
        {
            var (document, syntax) = await GetSyntaxAsync<TSyntax>(code, expected);

            var semanticModel = await document.GetSemanticModelAsync();
            var symbol = semanticModel.GetTypeInfo(syntax).Type;

            var encodedSymbolData = useNew
                ? SymbolKeyWriter.Write(symbol, CancellationToken.None)
                : SymbolKey.GetEncodedSymbolData(symbol);

            Assert.Equal(expected, encodedSymbolData);
        }
    }
}
