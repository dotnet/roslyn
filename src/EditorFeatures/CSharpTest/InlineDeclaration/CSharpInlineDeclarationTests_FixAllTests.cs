// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.InlineDeclaration
{
    public partial class CSharpInlineDeclarationTests
    {
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInlineDeclaration)]
        public async Task FixAllInDocument1()
        {
            await TestInRegularAndScriptAsync(
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

            await TestInRegularAndScriptAsync(
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

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInlineDeclaration)]
        public async Task FixAllInDocument3()
        {

            await TestInRegularAndScriptAsync(
@"class C
{
    void M()
    {
        // Now get final exe and args. CTtrl-F5 wraps exe in cmd prompt
        string {|FixAllInDocument:finalExecutable|}, finalArguments;
        GetExeAndArguments(useCmdShell, executable, arguments, out finalExecutable, out finalArguments);
    }
}",
@"class C
{
    void M()
    {
        GetExeAndArguments(useCmdShell, executable, arguments, out string finalExecutable, out string finalArguments);
    }
}");
        }
    }
}
