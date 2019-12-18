// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.CommandLine;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Lsif.Generator.LsifGraph;
using Microsoft.CodeAnalysis.Lsif.Generator.Writing;

namespace Microsoft.CodeAnalysis.Lsif.Generator
{
    internal sealed class Generator
    {
        private readonly ILsifJsonWriter _lsifJsonWriter;

        public Generator(ILsifJsonWriter lsifJsonWriter)
        {
            _lsifJsonWriter = lsifJsonWriter;
        }

        public async Task GenerateForCompilation(Compilation compilation, string projectPath, HostLanguageServices languageServices)
        {
            var projectVertex = new LsifGraph.Project(kind: GetLanguageKind(compilation.Language), new Uri(projectPath));
            _lsifJsonWriter.Write(projectVertex);
            _lsifJsonWriter.Write(new Event(Event.EventKind.Begin, projectVertex.GetId()));

            var documentIds = new List<Id<LsifGraph.Document>>();

            foreach (var syntaxTree in compilation.SyntaxTrees)
            {
                var semanticModel = compilation.GetSemanticModel(syntaxTree);

                documentIds.Add(await GenerateForDocument(semanticModel, languageServices));
            }

            _lsifJsonWriter.Write(Edge.Create("contains", projectVertex.GetId(), documentIds));

            _lsifJsonWriter.Write(new Event(Event.EventKind.End, projectVertex.GetId()));
        }

        private Task<Id<LsifGraph.Document>> GenerateForDocument(SemanticModel semanticModel, HostLanguageServices languageServices)
        {
            var syntaxTree = semanticModel.SyntaxTree;
            var sourceText = semanticModel.SyntaxTree.GetText();
            var syntaxFactsService = languageServices.GetRequiredService<ISyntaxFactsService>();

            var documentVertex = new LsifGraph.Document(new Uri(syntaxTree.FilePath), GetLanguageKind(semanticModel.Language));

            _lsifJsonWriter.Write(documentVertex);
            _lsifJsonWriter.Write(new Event(Event.EventKind.Begin, documentVertex.GetId()));

            // We will walk the file token-by-token, making a range for each one and then attaching information for it
            var rangeVertices = new List<Id<LsifGraph.Range>>();

            foreach (var syntaxToken in syntaxTree.GetRoot().DescendantTokens(descendIntoTrivia: true))
            {
                if (syntaxFactsService.IsBindableToken(syntaxToken))
                {
                    var bindableParent = syntaxFactsService.GetBindableParent(syntaxToken);

                    if (bindableParent != null)
                    {
                        var symbolInfo = semanticModel.GetSymbolInfo(bindableParent);

                        if (symbolInfo.Symbol != null)
                        {
                            var rangeVertex = LsifGraph.Range.FromTextSpan(syntaxToken.Span, sourceText);

                            _lsifJsonWriter.Write(rangeVertex);
                            rangeVertices.Add(rangeVertex.GetId());
                        }
                    }
                }
            }

            _lsifJsonWriter.Write(Edge.Create("contains", documentVertex.GetId(), rangeVertices));
            _lsifJsonWriter.Write(new Event(Event.EventKind.End, documentVertex.GetId()));

            return Task.FromResult(documentVertex.GetId());
        }

        private static string GetLanguageKind(string languageName)
        {
            return languageName switch
            {
                LanguageNames.CSharp => "csharp",
                LanguageNames.VisualBasic => "vb",
                _ => throw new NotSupportedException(languageName),
            };
        }
    }
}
