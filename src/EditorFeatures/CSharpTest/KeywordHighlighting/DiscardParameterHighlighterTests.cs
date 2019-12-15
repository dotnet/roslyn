// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor.CSharp.Highlighting.KeywordHighlighters;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.KeywordHighlighting
{
    public class DiscardParameterHighlighterTests : AbstractCSharpKeywordHighlighterTests
    {
        internal override IHighlighter CreateHighlighter()
        {
            return new DiscardParameterHighlighter();
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordHighlighting)]
        public async Task TestDiscardWithoutType()
        {
            await TestAsync(
@"using System;

class DiscardExample
{
    void Method()
    {
        int i = int.TryParse("""", out {|Cursor:[|_|]|});
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordHighlighting)]
        public async Task TestDiscardWithVarType()
        {
            await TestAsync(
@"using System;

class DiscardExample
{
    void Method()
    {
        int i = int.TryParse("""", out var {|Cursor:[|_|]|});
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordHighlighting)]
        public async Task TestDiscardWithExplicitType()
        {
            await TestAsync(
@"using System;

class DiscardExample
{
    void Method()
    {
        int i = int.TryParse("""", out int {|Cursor:[|_|]|});
    }
}");
        }
    }
}
