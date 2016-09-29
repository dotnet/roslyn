// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Editor.UnitTests.KeywordHighlighting;
using Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.KeywordHighlighting
{
    public abstract class AbstractCSharpKeywordHighlighterTests
        : AbstractKeywordHighlighterTests
    {
        protected override Task<TestWorkspace> CreateWorkspaceFromFileAsync(string code, ParseOptions options)
        {
            return TestWorkspace.CreateCSharpAsync(code, (CSharpParseOptions)options);
        }

        protected override IEnumerable<ParseOptions> GetOptions()
        {
            yield return Options.Regular;
            yield return Options.Script;
        }
    }
}
