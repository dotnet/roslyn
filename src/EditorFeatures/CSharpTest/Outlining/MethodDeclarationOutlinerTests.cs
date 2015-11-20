// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editor.CSharp.Outlining;
using Microsoft.CodeAnalysis.Editor.Implementation.Outlining;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Outlining
{
    public class MethodDeclarationOutlinerTests : AbstractOutlinerTests<MethodDeclarationSyntax>
    {
        internal override AbstractSyntaxNodeOutliner<MethodDeclarationSyntax> CreateOutliner()
        {
            return new MethodDeclarationOutliner();
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Outlining)]
        public void TestMethod()
        {
            const string code = @"
class C
{
    {|hint:$$public string Foo(){|collapse:
    {
    }|}|}
}";

            Regions(code,
                Region("collapse", "hint", CSharpOutliningHelpers.Ellipsis, autoCollapse: true));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Outlining)]
        public void TestMethodWithTrailingSpaces()
        {
            const string code = @"
class C
{
    {|hint:$$public string Foo()    {|collapse:
    {
    }|}|}
}";

            Regions(code,
                Region("collapse", "hint", CSharpOutliningHelpers.Ellipsis, autoCollapse: true));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Outlining)]
        public void TestMethodWithLeadingComments()
        {
            const string code = @"
class C
{
    {|span1:// Foo
    // Bar|}
    {|hint2:$$public string Foo(){|collapse2:
    {
    }|}|}
}";

            Regions(code,
                Region("span1", "// Foo ...", autoCollapse: true),
                Region("collapse2", "hint2", CSharpOutliningHelpers.Ellipsis, autoCollapse: true));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Outlining)]
        public void TestMethodWithWithExpressionBodyAndComments()
        {
            const string code = @"
class C
{
    {|span:// Foo
    // Bar|}
    $$public string Foo() => ""Foo"";
}";

            Regions(code,
                Region("span", "// Foo ...", autoCollapse: true));
        }
    }
}
