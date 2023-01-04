// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.FindSymbols
{
    internal class FindReferencesDocumentState
    {
        private static readonly HashSet<string> s_empty = new();

        public readonly Document Document;
        public readonly SemanticModel SemanticModel;
        public readonly SyntaxNode Root;
        public readonly FindReferenceCache Cache;
        public readonly HashSet<string> GlobalAliases;

        public readonly Solution Solution;
        public readonly SyntaxTree SyntaxTree;
        public readonly ISyntaxFactsService SyntaxFacts;
        public readonly ISemanticFactsService SemanticFacts;

        public FindReferencesDocumentState(
            Document document,
            SemanticModel semanticModel,
            SyntaxNode root,
            FindReferenceCache cache,
            HashSet<string>? globalAliases)
        {
            Document = document;
            SemanticModel = semanticModel;
            Root = root;
            Cache = cache;
            GlobalAliases = globalAliases ?? s_empty;

            Solution = document.Project.Solution;
            SyntaxTree = semanticModel.SyntaxTree;
            SyntaxFacts = document.GetRequiredLanguageService<ISyntaxFactsService>();
            SemanticFacts = document.GetRequiredLanguageService<ISemanticFactsService>();
        }
    }
}
