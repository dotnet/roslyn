// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.FindSymbols;

internal class FindReferencesDocumentState(
    Document document,
    SyntaxNode root,
    FindReferenceCache cache,
    HashSet<string>? globalAliases)
{
    private static readonly HashSet<string> s_empty = [];

    public readonly Document Document = document;
    public readonly SyntaxNode Root = root;
    public readonly FindReferenceCache Cache = cache;
    public readonly HashSet<string> GlobalAliases = globalAliases ?? s_empty;

    public readonly Solution Solution = document.Project.Solution;
    public readonly ISyntaxFactsService SyntaxFacts = document.GetRequiredLanguageService<ISyntaxFactsService>();
    public readonly ISemanticFactsService SemanticFacts = document.GetRequiredLanguageService<ISemanticFactsService>();

    public SemanticModel SemanticModel => Cache.SemanticModel;
    public SyntaxTree SyntaxTree => SemanticModel.SyntaxTree;
}
