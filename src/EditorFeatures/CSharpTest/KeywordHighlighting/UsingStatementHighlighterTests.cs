// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor.CSharp.KeywordHighlighting.KeywordHighlighters;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.KeywordHighlighting
{
    public class UsingStatementHighlighterTests : AbstractCSharpKeywordHighlighterTests
    {
        internal override IHighlighter CreateHighlighter()
        {
            return new UsingStatementHighlighter();
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordHighlighting)]
        public async Task TestExample1_1()
        {
            await TestAsync(
        @"class C {
    void M() {
        {|Cursor:[|using|]|} (Font f = new Font(“Arial”, 10.0f)) {
    // use f...
}
    }
}
");
        }
    }
}
