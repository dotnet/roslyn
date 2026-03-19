// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Editor.UnitTests.Formatting;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit.Abstractions;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Formatting;

[UseExportProvider]
public class CSharpFormattingEngineTestBase : CoreFormatterTestsBase
{
    protected CSharpFormattingEngineTestBase(ITestOutputHelper output) : base(output) { }

    protected override string GetLanguageName()
        => LanguageNames.CSharp;

    protected override SyntaxNode ParseCompilationUnit(string expected)
        => SyntaxFactory.ParseCompilationUnit(expected);
}
