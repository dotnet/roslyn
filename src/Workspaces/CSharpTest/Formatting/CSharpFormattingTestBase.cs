// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor.UnitTests.CodeActions;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.UnitTests.Formatting;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.Formatting;

public class CSharpFormattingTestBase : FormattingTestBase
{
    protected Workspace DefaultWorkspace { get => field ??= new AdhocWorkspace(); private set; }

    protected override SyntaxNode ParseCompilation(string text, ParseOptions? parseOptions)
        => SyntaxFactory.ParseCompilationUnit(text, options: (CSharpParseOptions?)parseOptions);

    private protected Task AssertNoFormattingChangesAsync(
        [StringSyntax(PredefinedEmbeddedLanguageNames.CSharpTest)] string code,
        OptionsCollection? changedOptionSet = null,
        bool testWithTransformation = true,
        ParseOptions? parseOptions = null)
    {
        return AssertFormatAsync(code, code, [new TextSpan(0, code.Length)], changedOptionSet, testWithTransformation, parseOptions);
    }

    private protected Task AssertFormatAsync(
        [StringSyntax(PredefinedEmbeddedLanguageNames.CSharpTest)] string expected,
        [StringSyntax(PredefinedEmbeddedLanguageNames.CSharpTest)] string code,
        OptionsCollection? changedOptionSet = null,
        bool testWithTransformation = true,
        ParseOptions? parseOptions = null)
    {
        return AssertFormatAsync(expected, code, [new TextSpan(0, code.Length)], changedOptionSet, testWithTransformation, parseOptions);
    }

    private protected Task AssertFormatAsync(
        [StringSyntax(PredefinedEmbeddedLanguageNames.CSharpTest)] string expected,
        [StringSyntax(PredefinedEmbeddedLanguageNames.CSharpTest)] string code,
        IEnumerable<TextSpan> spans,
        OptionsCollection? changedOptionSet = null,
        bool testWithTransformation = true,
        ParseOptions? parseOptions = null)
    {
        return AssertFormatAsync(expected, code, spans, LanguageNames.CSharp, changedOptionSet, testWithTransformation, parseOptions);
    }
}
