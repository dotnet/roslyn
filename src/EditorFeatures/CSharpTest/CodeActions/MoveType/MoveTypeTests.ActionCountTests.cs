// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.CodeActions.MoveType
{
    public partial class MoveTypeTests : CSharpMoveTypeTestsBase
    {
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsMoveType)]
        public async Task MoveType_ActionCounts_RenameOnly()
        {
            var code =
@"namespace N1
{
    class Class1[||]
    {
    }
}";
            // Fixes offered will be rename type to match file, rename file to match type.
            await TestActionCountAsync(code, count: 2);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsMoveType)]
        public async Task MoveType_MissingNotOnHeader1()
        {
            var code =
@"namespace N1
{
    [||]
    class Class1
    {
    }
}";
            await TestMissingInRegularAndScriptAsync(code);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsMoveType)]
        public async Task MoveType_MissingNotOnHeader2()
        {
            var code =
@"namespace N1
{
    [||][X]
    class Class1
    {
    }
}";
            await TestMissingInRegularAndScriptAsync(code);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsMoveType)]
        public async Task MoveType_MissingNotOnHeader3()
        {
            var code =
@"namespace N1
{
    class Class1
    [||]{
    }
}";
            await TestMissingInRegularAndScriptAsync(code);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsMoveType)]
        public async Task MoveType_ActionCounts_MoveOnly()
        {
            var code =
@"namespace N1
{
    class Class1[||]
    {
    }

    class test1 /* this matches file name assigned by TestWorkspace*/
    {
    }
}";
            // Fixes offered will be move type to new file.
            await TestActionCountAsync(code, count: 1);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsMoveType)]
        public async Task MoveType_ActionCounts_RenameAndMove()
        {
            var code =
@"namespace N1
{
    class Class1[||]
    {
    }

    class Class2
    {
    }
}";
            // Fixes offered will be move type, rename type to match file, rename file to match type.
            await TestActionCountAsync(code, count: 3);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsMoveType)]
        public async Task MoveType_ActionCounts_All()
        {
            var code =
@"namespace N1
{
    class OuterType
    {
        class InnerType[||]
        {
        }
    }

    class Class1
    {
    }
}";
            // Fixes offered will be
            // 1. move type to InnerType.cs
            // 2. move type to OuterType.InnerType.cs
            // 3. rename file to InnerType.cs
            // 4. rename file to OuterType.InnerType.cs
            // 5. rename type to test1 (which is the default document name given by TestWorkspace).
            await TestActionCountAsync(code, count: 5);
        }
    }
}
