// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.EditAndContinue.UnitTests;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Public = Microsoft.CodeAnalysis.CSharp.Symbols.PublicModel;

namespace Microsoft.CodeAnalysis.CSharp.EditAndContinue.UnitTests;

internal sealed class EditAndContinueTest(
    CSharpCompilationOptions? options = null,
    CSharpParseOptions? parseOptions = null,
    TargetFramework targetFramework = TargetFramework.Standard,
    Verification? verification = null)
    : EditAndContinueTest<EditAndContinueTest>(verification)
{
    private readonly CSharpCompilationOptions _compilationOptions = options ?? EditAndContinueTestBase.ComSafeDebugDll;
    private readonly CSharpParseOptions _parseOptions = parseOptions ?? TestOptions.Regular.WithNoRefSafetyRulesAttribute();

    protected override Compilation CreateCompilation(SyntaxTree tree)
        => CSharpTestBase.CreateCompilation(tree, options: _compilationOptions, targetFramework: targetFramework);

    protected override SourceWithMarkedNodes CreateSourceWithMarkedNodes(string source)
        => EditAndContinueTestBase.MarkedSource(source, options: _parseOptions);

    protected override Func<SyntaxNode, SyntaxNode> GetEquivalentNodesMap(ISymbol left, ISymbol right)
        => EditAndContinueTestBase.GetEquivalentNodesMap(
            ((Public.MethodSymbol)left).GetSymbol<MethodSymbol>(), ((Public.MethodSymbol)right).GetSymbol<MethodSymbol>());
}
