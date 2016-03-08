// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editor.CSharp.Outlining;
using Microsoft.CodeAnalysis.Editor.Implementation.Outlining;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Outlining
{
    public class AccessorDeclarationTests : AbstractCSharpSyntaxNodeOutlinerTests<AccessorDeclarationSyntax>
    {
        internal override AbstractSyntaxOutliner CreateOutliner() => new AccessorDeclarationOutliner();

        [Fact, Trait(Traits.Feature, Traits.Features.Outlining)]
        public async Task TestPropertyGetter1()
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

            await VerifyRegionsAsync(code,
                Region("collapse", "hint", CSharpOutliningHelpers.Ellipsis, autoCollapse: true));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Outlining)]
        public async Task TestPropertyGetterWithSingleLineComments1()
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

            await VerifyRegionsAsync(code,
                Region("span1", "// My ...", autoCollapse: true),
                Region("collapse2", "hint2", CSharpOutliningHelpers.Ellipsis, autoCollapse: true));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Outlining)]
        public async Task TestPropertyGetterWithMultiLineComments1()
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

            await VerifyRegionsAsync(code,
                Region("span1", "/* My ...", autoCollapse: true),
                Region("collapse2", "hint2", CSharpOutliningHelpers.Ellipsis, autoCollapse: true));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Outlining)]
        public async Task TestPropertyGetter2()
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

            await VerifyRegionsAsync(code,
                Region("collapse", "hint", CSharpOutliningHelpers.Ellipsis, autoCollapse: true));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Outlining)]
        public async Task TestPropertyGetterWithSingleLineComments2()
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

            await VerifyRegionsAsync(code,
                Region("span1", "// My ...", autoCollapse: true),
                Region("collapse2", "hint2", CSharpOutliningHelpers.Ellipsis, autoCollapse: true));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Outlining)]
        public async Task TestPropertyGetterWithMultiLineComments2()
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

            await VerifyRegionsAsync(code,
                Region("span1", "/* My ...", autoCollapse: true),
                Region("collapse2", "hint2", CSharpOutliningHelpers.Ellipsis, autoCollapse: true));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Outlining)]
        public async Task TestPropertySetter1()
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

            await VerifyRegionsAsync(code,
                Region("collapse", "hint", CSharpOutliningHelpers.Ellipsis, autoCollapse: true));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Outlining)]
        public async Task TestPropertySetterWithSingleLineComments1()
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

            await VerifyRegionsAsync(code,
                Region("span1", "// My ...", autoCollapse: true),
                Region("collapse2", "hint2", CSharpOutliningHelpers.Ellipsis, autoCollapse: true));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Outlining)]
        public async Task TestPropertySetterWithMultiLineComments1()
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

            await VerifyRegionsAsync(code,
                Region("span1", "/* My ...", autoCollapse: true),
                Region("collapse2", "hint2", CSharpOutliningHelpers.Ellipsis, autoCollapse: true));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Outlining)]
        public async Task TestPropertySetter2()
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

            await VerifyRegionsAsync(code,
                Region("collapse", "hint", CSharpOutliningHelpers.Ellipsis, autoCollapse: true));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Outlining)]
        public async Task TestPropertySetterWithSingleLineComments2()
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

            await VerifyRegionsAsync(code,
                Region("span1", "// My ...", autoCollapse: true),
                Region("collapse2", "hint2", CSharpOutliningHelpers.Ellipsis, autoCollapse: true));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Outlining)]
        public async Task TestPropertySetterWithMultiLineComments2()
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

            await VerifyRegionsAsync(code,
                Region("span1", "/* My ...", autoCollapse: true),
                Region("collapse2", "hint2", CSharpOutliningHelpers.Ellipsis, autoCollapse: true));
        }
    }
}
