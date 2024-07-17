// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Generic;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Editor.UnitTests.KeywordHighlighting;
using Microsoft.CodeAnalysis.Test.Utilities;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.KeywordHighlighting;

public abstract class AbstractCSharpKeywordHighlighterTests
    : AbstractKeywordHighlighterTests
{
    protected override EditorTestWorkspace CreateWorkspaceFromFile(string code, ParseOptions options)
        => EditorTestWorkspace.CreateCSharp(code, options, composition: Composition);

    protected override IEnumerable<ParseOptions> GetOptions()
    {
        yield return TestOptions.Regular;
        yield return TestOptions.Script;
    }
}
