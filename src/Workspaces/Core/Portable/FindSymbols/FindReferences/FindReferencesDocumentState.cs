// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.FindSymbols;

/// <summary>
/// Ephemeral information that find-references needs for a particular document when searching for a <em>specific</em>
/// symbol.  Importantly, it contains the global aliases to that symbol within the current project.
/// </summary>
internal sealed class FindReferencesDocumentState(
    FindReferenceCache cache,
    HashSet<string>? globalAliases)
{
    private static readonly HashSet<string> s_empty = [];

    public readonly FindReferenceCache Cache = cache;
    public readonly HashSet<string> GlobalAliases = globalAliases ?? s_empty;

    public Document Document => this.Cache.Document;
    public SyntaxNode Root => this.Cache.Root;
    public SemanticModel SemanticModel => this.Cache.SemanticModel;
    public SyntaxTree SyntaxTree => this.SemanticModel.SyntaxTree;

    public Solution Solution => this.Document.Project.Solution;

    // These are expensive enough (in the GetRequiredLanguageService call) that we cache this up front in stead of
    // computing on demand.

    public ISyntaxFactsService SyntaxFacts { get; } = cache.Document.GetRequiredLanguageService<ISyntaxFactsService>();
    public ISemanticFactsService SemanticFacts { get; } = cache.Document.GetRequiredLanguageService<ISemanticFactsService>();
}
