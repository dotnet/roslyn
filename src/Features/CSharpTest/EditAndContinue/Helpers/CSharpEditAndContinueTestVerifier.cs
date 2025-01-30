// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.Differencing;
using Microsoft.CodeAnalysis.EditAndContinue;
using Microsoft.CodeAnalysis.EditAndContinue.UnitTests;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.EditAndContinue.UnitTests;

internal sealed class CSharpEditAndContinueTestVerifier(Action<SyntaxNode>? faultInjector = null) : EditAndContinueTestVerifier
{
    private readonly CSharpEditAndContinueAnalyzer _analyzer = new(faultInjector);

    public override AbstractEditAndContinueAnalyzer Analyzer => _analyzer;
    public override string LanguageName => LanguageNames.CSharp;
    public override string ProjectFileExtension => ".csproj";
    public override TreeComparer<SyntaxNode> TopSyntaxComparer => SyntaxComparer.TopLevel;
    public override string? TryGetResource(string keyword) => EditingTestBase.TryGetResource(keyword);

    public override ImmutableArray<SyntaxNode> GetDeclarators(ISymbol method)
    {
        Assert.True(method is IMethodSymbol, "Only methods should have a syntax map.");
        return LocalVariableDeclaratorsCollector.GetDeclarators((SourceMemberMethodSymbol)method);
    }
}
