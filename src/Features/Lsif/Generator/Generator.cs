// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host;
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
            var documentVertex = new LsifGraph.Document(new Uri(syntaxTree.FilePath), GetLanguageKind(semanticModel.Language));

            _lsifJsonWriter.Write(documentVertex);
            _lsifJsonWriter.Write(new Event(Event.EventKind.Begin, documentVertex.GetId()));
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
