// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editor.CSharp.Outlining;
using Microsoft.CodeAnalysis.Editor.Implementation.Outlining;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Outlining
{
    public class ConstructorDeclarationOutlinerTests : AbstractOutlinerTests<ConstructorDeclarationSyntax>
    {
        internal override AbstractSyntaxNodeOutliner<ConstructorDeclarationSyntax> CreateOutliner()
        {
            return new ConstructorDeclarationOutliner();
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Outlining)]
        public void TestConstructor1()
        {
            const string code = @"
class C
{
    {|hint:$$public C(){|collapse:
    {
    }|}|}
}";

            Regions(code,
                Region("collapse", "hint", CSharpOutliningHelpers.Ellipsis, autoCollapse: true));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Outlining)]
        public void TestConstructor2()
        {
            const string code = @"
class C
{
    {|hint:$$public C(){|collapse:
    {
    }                 |}|}
}";

            Regions(code,
                Region("collapse", "hint", CSharpOutliningHelpers.Ellipsis, autoCollapse: true));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Outlining)]
        public void TestConstructor3()
        {
            const string code = @"
class C
{
    {|hint:$$public C(){|collapse:
    {
    }|}|} // .ctor
}";

            Regions(code,
                Region("collapse", "hint", CSharpOutliningHelpers.Ellipsis, autoCollapse: true));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Outlining)]
        public void TestConstructor4()
        {
            const string code = @"
class C
{
    {|hint:$$public C(){|collapse:
    {
    }|}|} /* .ctor */
}";

            Regions(code,
                Region("collapse", "hint", CSharpOutliningHelpers.Ellipsis, autoCollapse: true));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Outlining)]
        public void TestConstructor5()
        {
            const string code = @"
class C
{
    {|hint:$$public C() // .ctor{|collapse:
    {
    }|}|} // .ctor
}";

            Regions(code,
                Region("collapse", "hint", CSharpOutliningHelpers.Ellipsis, autoCollapse: true));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Outlining)]
        public void TestConstructor6()
        {
            const string code = @"
class C
{
    {|hint:$$public C() /* .ctor */{|collapse:
    {
    }|}|} // .ctor
}";

            Regions(code,
                Region("collapse", "hint", CSharpOutliningHelpers.Ellipsis, autoCollapse: true));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Outlining)]
        public void TestConstructor7()
        {
            const string code = @"
class C
{
    {|hint:$$public C()
    // .ctor{|collapse:
    {
    }|}|} // .ctor
}";

            Regions(code,
                Region("collapse", "hint", CSharpOutliningHelpers.Ellipsis, autoCollapse: true));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Outlining)]
        public void TestConstructor8()
        {
            const string code = @"
class C
{
    {|hint:$$public C()
    /* .ctor */{|collapse:
    {
    }|}|} // .ctor
}";

            Regions(code,
                Region("collapse", "hint", CSharpOutliningHelpers.Ellipsis, autoCollapse: true));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Outlining)]
        public void TestConstructorWithComments()
        {
            const string code = @"
class C
{
    {|span1:// Foo
    // Bar|}
    {|hint2:$$public C(){|collapse2:
    {
    }|}|} // .ctor
}";

            Regions(code,
                Region("span1", "// Foo ...", autoCollapse: true),
                Region("collapse2", "hint2", CSharpOutliningHelpers.Ellipsis, autoCollapse: true));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Outlining)]
        public void TestConstructorMissingCloseParenAndBody()
        {
            // Expected behavior is that the class should be outlined, but the constructor should not.

            const string code = @"
class C
{
    $$C(
}";

            NoRegions(code);
        }
    }
}
