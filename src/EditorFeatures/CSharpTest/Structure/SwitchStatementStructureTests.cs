// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Structure;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Structure;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Structure
{
    public class SwitchStatementStructureTests : AbstractCSharpSyntaxNodeStructureTests<SwitchStatementSyntax>
    {
        internal override AbstractSyntaxStructureProvider CreateProvider() => new SwitchStatementStructureProvider();

        [Fact, Trait(Traits.Feature, Traits.Features.Outlining)]
        public async Task TestSwitchStatement1()
        {
            const string code = @"
class C
{
    void M()
    {
        {|hint:$$switch (expr){|textspan:
        {
        }|}|}
    }
}";

            await VerifyBlockSpansAsync(code,
                Region("textspan", "hint", CSharpStructureHelpers.Ellipsis, autoCollapse: false));
        }


        [Fact, Trait(Traits.Feature, Traits.Features.Outlining), Trait(Traits.Feature, Traits.Features.AdditionalInternalStructureOutlinings)]
        public async Task TestSwitchStatement2()
        {
            const string code = @"
class C
{
    void M()
    {
        {|hint:$$switch (expr){|textspan:
        {
            {|case0:{|casetext:case 0:
                Console.WriteLine();
                break;|}|}
            {|default:{|defaulttext:default:
                break;|}|}
        }|}|}
    }
}";
            await VerifyBlockSpansAsync(
                code,
                Region("textspan", "hint", CSharpStructureHelpers.Ellipsis, autoCollapse: false ),
                Region("case0", "casetext", "case 0:", autoCollapse: false),
                Region("default", "defaulttext", "default:", false));

        }
    }
}
