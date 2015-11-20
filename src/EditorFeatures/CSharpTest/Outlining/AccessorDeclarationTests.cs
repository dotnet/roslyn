// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editor.CSharp.Outlining;
using Microsoft.CodeAnalysis.Editor.Implementation.Outlining;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Outlining
{
    public class AccessorDeclarationTests : AbstractOutlinerTests<AccessorDeclarationSyntax>
    {
        internal override AbstractSyntaxNodeOutliner<AccessorDeclarationSyntax> CreateOutliner()
        {
            return new AccessorDeclarationOutliner();
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Outlining)]
        public void TestPropertyGetter1()
        {
            const string code = @"
class C
{
    public string Text
    {
        $${|hint:get{|collapse:
        {
        }|}|}
    }
}";

            Regions(code,
                Region("collapse", "hint", CSharpOutliningHelpers.Ellipsis, autoCollapse: true));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Outlining)]
        public void TestPropertyGetterWithSingleLineComments1()
        {
            const string code = @"
class C
{
    public string Text
    {
        {|span1:// My
        // Getter|}
        $${|hint2:get{|collapse2:
        {
        }|}|}
    }
}
";

            Regions(code,
                Region("span1", "// My ...", autoCollapse: true),
                Region("collapse2", "hint2", CSharpOutliningHelpers.Ellipsis, autoCollapse: true));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Outlining)]
        public void TestPropertyGetterWithMultiLineComments1()
        {
            const string code = @"
class C
{
    public string Text
    {
        {|span1:/* My
           Getter */|}
        $${|hint2:get{|collapse2:
        {
        }|}|}
    }
}
";

            Regions(code,
                Region("span1", "/* My ...", autoCollapse: true),
                Region("collapse2", "hint2", CSharpOutliningHelpers.Ellipsis, autoCollapse: true));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Outlining)]
        public void TestPropertyGetter2()
        {
            const string code = @"
class C
{
    public string Text
    {
        $${|hint:get{|collapse:
        {
        }|}|}
        set
        {
        }
    }
}";

            Regions(code,
                Region("collapse", "hint", CSharpOutliningHelpers.Ellipsis, autoCollapse: true));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Outlining)]
        public void TestPropertyGetterWithSingleLineComments2()
        {
            const string code = @"
class C
{
    public string Text
    {
        {|span1:// My
        // Getter|}
        $${|hint2:get{|collapse2:
        {
        }|}|}
        set
        {
        }
    }
}
";

            Regions(code,
                Region("span1", "// My ...", autoCollapse: true),
                Region("collapse2", "hint2", CSharpOutliningHelpers.Ellipsis, autoCollapse: true));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Outlining)]
        public void TestPropertyGetterWithMultiLineComments2()
        {
            const string code = @"
class C
{
    public string Text
    {
        {|span1:/* My
           Getter */|}
        $${|hint2:get{|collapse2:
        {
        }|}|}
        set
        {
        }
    }
}
";

            Regions(code,
                Region("span1", "/* My ...", autoCollapse: true),
                Region("collapse2", "hint2", CSharpOutliningHelpers.Ellipsis, autoCollapse: true));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Outlining)]
        public void TestPropertySetter1()
        {
            const string code = @"
class C
{
    public string Text
    {
        $${|hint:set{|collapse:
        {
        }|}|}
    }
}";

            Regions(code,
                Region("collapse", "hint", CSharpOutliningHelpers.Ellipsis, autoCollapse: true));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Outlining)]
        public void TestPropertySetterWithSingleLineComments1()
        {
            const string code = @"
class C
{
    public string Text
    {
        {|span1:// My
        // Setter|}
        $${|hint2:set{|collapse2:
        {
        }|}|}
    }
}";

            Regions(code,
                Region("span1", "// My ...", autoCollapse: true),
                Region("collapse2", "hint2", CSharpOutliningHelpers.Ellipsis, autoCollapse: true));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Outlining)]
        public void TestPropertySetterWithMultiLineComments1()
        {
            const string code = @"
class C
{
    public string Text
    {
        {|span1:/* My
           Setter */|}
        $${|hint2:set{|collapse2:
        {
        }|}|}
    }
}";

            Regions(code,
                Region("span1", "/* My ...", autoCollapse: true),
                Region("collapse2", "hint2", CSharpOutliningHelpers.Ellipsis, autoCollapse: true));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Outlining)]
        public void TestPropertySetter2()
        {
            const string code = @"
class C
{
    public string Text
    {
        get
        {
        }
        $${|hint:set{|collapse:
        {
        }|}|}
    }
}";

            Regions(code,
                Region("collapse", "hint", CSharpOutliningHelpers.Ellipsis, autoCollapse: true));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Outlining)]
        public void TestPropertySetterWithSingleLineComments2()
        {
            const string code = @"
class C
{
    public string Text
    {
        get
        {
        }
        {|span1:// My
        // Setter|}
        $${|hint2:set{|collapse2:
        {
        }|}|}
    }
}";

            Regions(code,
                Region("span1", "// My ...", autoCollapse: true),
                Region("collapse2", "hint2", CSharpOutliningHelpers.Ellipsis, autoCollapse: true));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Outlining)]
        public void TestPropertySetterWithMultiLineComments2()
        {
            const string code = @"
class C
{
    public string Text
    {
        get
        {
        }
        {|span1:/* My
           Setter */|}
        $${|hint2:set{|collapse2:
        {
        }|}|}
    }
}";

            Regions(code,
                Region("span1", "/* My ...", autoCollapse: true),
                Region("collapse2", "hint2", CSharpOutliningHelpers.Ellipsis, autoCollapse: true));
        }
    }
}
