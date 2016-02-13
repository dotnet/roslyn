// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editor.CSharp.Outlining;
using Microsoft.CodeAnalysis.Editor.Implementation.Outlining;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Outlining
{
    public class NamespaceDeclarationOutlinerTests : AbstractCSharpSyntaxNodeOutlinerTests<NamespaceDeclarationSyntax>
    {
        internal override AbstractSyntaxOutliner CreateOutliner() => new NamespaceDeclarationOutliner();

        [Fact, Trait(Traits.Feature, Traits.Features.Outlining)]
        public async Task TestNamespace()
        {
            const string code = @"
class C
{
    {|hint:$$namespace N{|collapse:
    {
    }|}|}
}";

            await VerifyRegionsAsync(code,
                Region("collapse", "hint", CSharpOutliningHelpers.Ellipsis, autoCollapse: false));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Outlining)]
        public async Task TestNamespaceWithLeadingComments()
        {
            const string code = @"
class C
{
    {|span1:// Foo
    // Bar|}
    {|hint2:$$namespace N{|collapse2:
    {
    }|}|}
}";

            await VerifyRegionsAsync(code,
                Region("span1", "// Foo ...", autoCollapse: true),
                Region("collapse2", "hint2", CSharpOutliningHelpers.Ellipsis, autoCollapse: false));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Outlining)]
        public async Task TestNamespaceWithNestedUsings()
        {
            const string code = @"
class C
{
    {|hint1:$$namespace N{|collapse1:
    {
        {|hint2:using {|collapse2:System;
        using System.Linq;|}|}
    }|}|}
}";

            await VerifyRegionsAsync(code,
                Region("collapse1", "hint1", CSharpOutliningHelpers.Ellipsis, autoCollapse: false),
                Region("collapse2", "hint2", CSharpOutliningHelpers.Ellipsis, autoCollapse: true));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Outlining)]
        public async Task TestNamespaceWithNestedUsingsWithLeadingComments()
        {
            const string code = @"
class C
{
    {|hint1:$$namespace N{|collapse1:
    {
        {|span2:// Foo
        // Bar|}
        {|hint3:using {|collapse3:System;
        using System.Linq;|}|}
    }|}|}
}";

            await VerifyRegionsAsync(code,
                Region("collapse1", "hint1", CSharpOutliningHelpers.Ellipsis, autoCollapse: false),
                Region("span2", "// Foo ...", autoCollapse: true),
                Region("collapse3", "hint3", CSharpOutliningHelpers.Ellipsis, autoCollapse: true));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Outlining)]
        public async Task TestNamespaceWithNestedComments()
        {
            const string code = @"
class C
{
    {|hint1:$$namespace N{|collapse1:
    {
        {|span2:// Foo
        // Bar|}
    }|}|}
}";

            await VerifyRegionsAsync(code,
                Region("collapse1", "hint1", CSharpOutliningHelpers.Ellipsis, autoCollapse: false),
                Region("span2", "// Foo ...", autoCollapse: true));
        }
    }
}
