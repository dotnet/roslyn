// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editor.CSharp.Outlining;
using Microsoft.CodeAnalysis.Editor.Implementation.Outlining;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Outlining
{
    public class NamespaceDeclarationOutlinerTests : AbstractOutlinerTests<NamespaceDeclarationSyntax>
    {
        internal override AbstractSyntaxNodeOutliner<NamespaceDeclarationSyntax> CreateOutliner()
        {
            return new NamespaceDeclarationOutliner();
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Outlining)]
        public void TestNamespace()
        {
            const string code = @"
class C
{
    {|hint:$$namespace N{|collapse:
    {
    }|}|}
}";

            Regions(code,
                Region("collapse", "hint", CSharpOutliningHelpers.Ellipsis, autoCollapse: false));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Outlining)]
        public void TestNamespaceWithLeadingComments()
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

            Regions(code,
                Region("span1", "// Foo ...", autoCollapse: true),
                Region("collapse2", "hint2", CSharpOutliningHelpers.Ellipsis, autoCollapse: false));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Outlining)]
        public void TestNamespaceWithNestedUsings()
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

            Regions(code,
                Region("collapse1", "hint1", CSharpOutliningHelpers.Ellipsis, autoCollapse: false),
                Region("collapse2", "hint2", CSharpOutliningHelpers.Ellipsis, autoCollapse: true));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Outlining)]
        public void TestNamespaceWithNestedUsingsWithLeadingComments()
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

            Regions(code,
                Region("collapse1", "hint1", CSharpOutliningHelpers.Ellipsis, autoCollapse: false),
                Region("span2", "// Foo ...", autoCollapse: true),
                Region("collapse3", "hint3", CSharpOutliningHelpers.Ellipsis, autoCollapse: true));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Outlining)]
        public void TestNamespaceWithNestedComments()
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

            Regions(code,
                Region("collapse1", "hint1", CSharpOutliningHelpers.Ellipsis, autoCollapse: false),
                Region("span2", "// Foo ...", autoCollapse: true));
        }
    }
}
