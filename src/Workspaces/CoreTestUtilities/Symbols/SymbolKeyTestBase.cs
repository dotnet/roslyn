// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Symbols;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.UnitTests.Symbols
{
    public abstract class SymbolKeyTestBase
    {
        private static readonly MetadataReference s_mscorlib = MetadataReference.CreateFromFile(typeof(string).Assembly.Location);
        private static readonly MetadataReference s_systemCore = MetadataReference.CreateFromFile(typeof(Enumerable).Assembly.Location);

        protected abstract string LanguageName { get; }
        protected abstract ParseOptions SetLanguageVersion(ParseOptions options);

        private async Task<(Document document, TSyntax syntax)> GetSyntaxAsync<TSyntax>(string code, string expected)
            where TSyntax : SyntaxNode
        {
            MarkupTestFile.GetPosition(code, out var output, out int position);

            var workspace = new AdhocWorkspace();

            var project = workspace.AddProject("TestProject", LanguageNames.CSharp)
                .AddMetadataReference(s_mscorlib)
                .AddMetadataReference(s_systemCore);

            project = project.WithParseOptions(
                SetLanguageVersion(project.ParseOptions));

            var document = project.AddDocument("TestFile", SourceText.From(output));

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
    }
}
