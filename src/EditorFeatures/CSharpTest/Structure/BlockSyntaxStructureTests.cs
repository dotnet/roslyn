// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Structure;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Structure;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Structure
{
    public class BlockSyntaxStructureTests : AbstractCSharpSyntaxNodeStructureTests<BlockSyntax>
    {
        internal override AbstractSyntaxStructureProvider CreateProvider() => new BlockSyntaxStructureProvider();

        [Fact, Trait(Traits.Feature, Traits.Features.Outlining)]
        public async Task TestTryBlock1()
        {
            const string code = @"
class C
{
    void M()
    {
        {|hint:try{|textspan:
        {$$
        }
        catch 
        {
        }
        finally
        {
        }|}|}
    }
}";

            await VerifyBlockSpansAsync(code,
                Region("textspan", "hint", CSharpStructureHelpers.Ellipsis, autoCollapse: false));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Outlining)]
        public async Task TestUnsafe1()
        {
            const string code = @"
class C
{
    void M()
    {
        {|hint:unsafe{|textspan:
        {$$
        }|}|}
    }
}";

            await VerifyBlockSpansAsync(code,
                Region("textspan", "hint", CSharpStructureHelpers.Ellipsis, autoCollapse: false));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Outlining)]
        public async Task TestFixed1()
        {
            const string code = @"
class C
{
    void M()
    {
        {|hint:fixed(int* i = &j){|textspan:
        {$$
        }|}|}
    }
}";

            await VerifyBlockSpansAsync(code,
                Region("textspan", "hint", CSharpStructureHelpers.Ellipsis, autoCollapse: false));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Outlining)]
        public async Task TestUsing1()
        {
            const string code = @"
class C
{
    void M()
    {
        {|hint:using (goo){|textspan:
        {$$
        }|}|}
    }
}";

            await VerifyBlockSpansAsync(code,
                Region("textspan", "hint", CSharpStructureHelpers.Ellipsis, autoCollapse: false));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Outlining)]
        public async Task TestLock1()
        {
            const string code = @"
class C
{
    void M()
    {
        {|hint:lock (goo){|textspan:
        {$$
        }|}|}
    }
}";

            await VerifyBlockSpansAsync(code,
                Region("textspan", "hint", CSharpStructureHelpers.Ellipsis, autoCollapse: false));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Outlining)]
        public async Task TestForStatement1()
        {
            const string code = @"
class C
{
    void M()
    {
        {|hint:for (;;){|textspan:
        {$$
        }|}|}
    }
}";

            await VerifyBlockSpansAsync(code,
                Region("textspan", "hint", CSharpStructureHelpers.Ellipsis, autoCollapse: false));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Outlining)]
        public async Task TestForEachStatement1()
        {
            const string code = @"
class C
{
    void M()
    {
        {|hint:foreach (var v in e){|textspan:
        {$$
        }|}|}
    }
}";

            await VerifyBlockSpansAsync(code,
                Region("textspan", "hint", CSharpStructureHelpers.Ellipsis, autoCollapse: false));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Outlining)]
        public async Task TestCompoundForEachStatement1()
        {
            const string code = @"
class C
{
    void M()
    {
        {|hint:foreach ((var v, var x) in e){|textspan:
        {$$
        }|}|}
    }
}";

            await VerifyBlockSpansAsync(code,
                Region("textspan", "hint", CSharpStructureHelpers.Ellipsis, autoCollapse: false));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Outlining)]
        public async Task TestWhileStatement1()
        {
            const string code = @"
class C
{
    void M()
    {
        {|hint:while (true){|textspan:
        {$$
        }|}|}
    }
}";

            await VerifyBlockSpansAsync(code,
                Region("textspan", "hint", CSharpStructureHelpers.Ellipsis, autoCollapse: false));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Outlining)]
        public async Task TestDoStatement1()
        {
            const string code = @"
class C
{
    void M()
    {
        {|hint:do{|textspan:
        {$$
        }
        while (true);|}|}
    }
}";

            await VerifyBlockSpansAsync(code,
                Region("textspan", "hint", CSharpStructureHelpers.Ellipsis, autoCollapse: false));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Outlining)]
        public async Task TestIfStatement1()
        {
            const string code = @"
class C
{
    void M()
    {
        {|hint:if (true){|textspan:
        {$$
        }|}|}
    }
}";

            await VerifyBlockSpansAsync(code,
                Region("textspan", "hint", CSharpStructureHelpers.Ellipsis, autoCollapse: false));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Outlining)]
        public async Task TestIfStatement2()
        {
            const string code = @"
class C
{
    void M()
    {
        {|hint:if (true){|textspan:
        {$$
        }|}|}
        else
        {
        }
    }
}";

            await VerifyBlockSpansAsync(code,
                Region("textspan", "hint", CSharpStructureHelpers.Ellipsis, autoCollapse: false));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Outlining)]
        public async Task TestIfStatement3()
        {
            const string code = @"
class C
{
    void M()
    {
        {|hint:if (true){|textspan:
        {$$
        }|}|}
        else
            return;
    }
}";

            await VerifyBlockSpansAsync(code,
                Region("textspan", "hint", CSharpStructureHelpers.Ellipsis, autoCollapse: false));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Outlining)]
        public async Task TestElseClause1()
        {
            const string code = @"
class C
{
    void M()
    {
        if (true)
        {
        }
        {|hint:else{|textspan:
        {$$
        }|}|}
    }
}";

            await VerifyBlockSpansAsync(code,
                Region("textspan", "hint", CSharpStructureHelpers.Ellipsis, autoCollapse: false));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Outlining)]
        public async Task TestIfElse1()
        {
            const string code = @"
class C
{
    void M()
    {
        if (true)
        {
        }
        else {|hint:if (false){|textspan:
        {$$
        }|}|}
    }
}";

            await VerifyBlockSpansAsync(code,
                Region("textspan", "hint", CSharpStructureHelpers.Ellipsis, autoCollapse: false));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Outlining)]
        public async Task TestNestedBlock()
        {
            const string code = @"
class C
{
    void M()
    {
        {|hint:{|textspan:{$$

        }|}|}
    }
}";

            await VerifyBlockSpansAsync(code,
                Region("textspan", "hint", CSharpStructureHelpers.Ellipsis, autoCollapse: false));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Outlining)]
        public async Task TestNestedBlockInSwitchSection1()
        {
            const string code = @"
class C
{
    void M()
    {
        switch (e)
        {
            case 0:
                {|hint:{|textspan:{$$

                }|}|}
        }
    }
}";

            await VerifyBlockSpansAsync(code,
                Region("textspan", "hint", CSharpStructureHelpers.Ellipsis, autoCollapse: false));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Outlining)]
        public async Task TestNestedBlockInSwitchSection2()
        {
            const string code = @"
class C
{
    void M()
    {
        switch (e)
        {
        case 0:
            int i = 0;
            {|hint:{|textspan:{$$

            }|}|}
        }
    }
}";

            await VerifyBlockSpansAsync(code,
                Region("textspan", "hint", CSharpStructureHelpers.Ellipsis, autoCollapse: false));
        }
    }
}
