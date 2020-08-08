// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Microsoft.CodeAnalysis.Editor.UnitTests.KeywordHighlighting;
using Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.KeywordHighlighting
{
    public abstract class AbstractCSharpKeywordHighlighterTests
        : AbstractKeywordHighlighterTests
    {
        protected override TestWorkspace CreateWorkspaceFromFile(string code, ParseOptions options)
            => TestWorkspace.CreateCSharp(code, options, composition: Composition);

        protected override IEnumerable<ParseOptions> GetOptions()
        {
            yield return Options.Regular;
            yield return Options.Script;
        }
    }
}
