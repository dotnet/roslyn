// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading.Tasks;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.InlineDeclaration
{
    public partial class CSharpInlineDeclarationTests
    {
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInlineDeclaration)]
        public async Task FixAllInDocument1()
        {
            await TestAsync(
@"class C
{
    void M()
    {
        int {|FixAllInDocument:i|}, j;
        if (int.TryParse(v, out i, out j))
        {
        } 
    }
}",
@"class C
{
    void M()
    {
        if (int.TryParse(v, out int i, out int j))
        {
        } 
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInlineDeclaration)]
        public async Task FixAllInDocument2()
        {

            await TestAsync(
@"class C
{
    void M()
    {
        {|FixAllInDocument:int|} i;
        if (int.TryParse(v, out i))
        {
        } 
    }

    void M1()
    {
        int i;
        if (int.TryParse(v, out i))
        {
        } 
    }
}",
@"class C
{
    void M()
    {
        if (int.TryParse(v, out int i))
        {
        } 
    }

    void M1()
    {
        if (int.TryParse(v, out int i))
        {
        } 
    }
}");
        }
    }
}