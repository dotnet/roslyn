// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Structure;
using Microsoft.CodeAnalysis.Editor.UnitTests.Structure;
using Microsoft.CodeAnalysis.Structure;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Structure;

[Trait(Traits.Feature, Traits.Features.Outlining)]
public class MultilineCommentStructureTests : AbstractCSharpSyntaxTriviaStructureTests
{
    internal override AbstractSyntaxStructureProvider CreateProvider() => new MultilineCommentBlockStructureProvider();

    [Fact]
    public async Task TestMultilineComment1()
    {
        const string code = @"
{|span:/* Hello
$$C# */|}
class C
{
}
";

        await VerifyBlockSpansAsync(code,
            Region("span", "/* Hello ...", autoCollapse: true));
    }

    [Fact]
    public async Task TestMultilineCommentOnOneLine()
    {
        const string code = @"
{|span:/* Hello $$C# */|}
class C
{
}
";

        await VerifyBlockSpansAsync(code,
            Region("span", "/* Hello C# ...", autoCollapse: true));
    }

    [Fact, WorkItem(1108049, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1108049")]
    [WorkItem(791, "https://github.com/dotnet/roslyn/issues/791")]
    public async Task TestIncompleteMultilineCommentZeroSpace()
    {
        const string code = @"
{|span:$$/*|}";

        await VerifyBlockSpansAsync(code,
            Region("span", "/*  ...", autoCollapse: true));
    }

    [Fact, WorkItem(1108049, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1108049")]
    [WorkItem(791, "https://github.com/dotnet/roslyn/issues/791")]
    public async Task TestIncompleteMultilineCommentSingleSpace()
    {
        const string code = @"
{|span:$$/* |}";

        await VerifyBlockSpansAsync(code,
            Region("span", "/*  ...", autoCollapse: true));
    }

    [Fact]
    public async Task TestPropertyGetterWithMultiLineComments1()
    {
        const string code = @"
class C
{
    public string Text
    {
        $${|span1:/* My
           Getter */|}
        get
        {
        }
    }
}
";

        await VerifyBlockSpansAsync(code,
            Region("span1", "/* My ...", autoCollapse: true));
    }

    [Fact]
    public async Task TestPropertyGetterWithMultiLineComments2()
    {
        const string code = @"
class C
{
    public string Text
    {
        $${|span1:/* My
           Getter */|}
        get
        {
        }
        set
        {
        }
    }
}
";

        await VerifyBlockSpansAsync(code,
            Region("span1", "/* My ...", autoCollapse: true));
    }

    [Fact]
    public async Task TestPropertyGetterWithMultiLineComments3()
    {
        const string code = @"
class C
{
    public string Text
    {
        $${|span1:/* My
           Getter */|}
        get
        {
        }

        set
        {
        }
    }
}
";

        await VerifyBlockSpansAsync(code,
            Region("span1", "/* My ...", autoCollapse: true));
    }

    [Fact]
    public async Task TestPropertySetterWithMultiLineComments1()
    {
        const string code = @"
class C
{
    public string Text
    {
        $${|span1:/* My
           Setter */|}
        set
        {
        }
    }
}";

        await VerifyBlockSpansAsync(code,
            Region("span1", "/* My ...", autoCollapse: true));
    }

    [Fact]
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
        $${|span1:/* My
           Setter */|}
        set
        {
        }
    }
}";

        await VerifyBlockSpansAsync(code,
            Region("span1", "/* My ...", autoCollapse: true));
    }

    [Fact]
    public async Task TestPropertySetterWithMultiLineComments3()
    {
        const string code = @"
class C
{
    public string Text
    {
        get
        {
        }

        $${|span1:/* My
           Setter */|}
        set
        {
        }
    }
}";

        await VerifyBlockSpansAsync(code,
            Region("span1", "/* My ...", autoCollapse: true));
    }

    [Fact]
    public async Task TestMultilineCommentInFile()
    {
        const string code = @"
$${|span1:/* Comment in file
 */|}
namespace M
{
}
";

        await VerifyBlockSpansAsync(code,
            Region("span1", "/* Comment in file ...", autoCollapse: true));
    }

    [Fact]
    public async Task TestMultilineCommentInNamespace()
    {
        const string code = @"
namespace M
{
    $${|span1:/* Comment in namespace
     */|}
}
";

        await VerifyBlockSpansAsync(code,
            Region("span1", "/* Comment in namespace ...", autoCollapse: true));
    }

    [Fact]
    public async Task TestMultilineCommentInClass()
    {
        const string code = @"
namespace M
{
    class C
    {
        $${|span1:/* Comment in class
         */|}
    }
    
}
";

        await VerifyBlockSpansAsync(code,
            Region("span1", "/* Comment in class ...", autoCollapse: true));
    }

    [Fact, WorkItem(64001, "https://github.com/dotnet/roslyn/issues/64001")]
    public async Task TestMultilineCommentInMethod()
    {
        const string code = @"
namespace M
{
    class C
    {
        void M()
        {
            $${|span1:/* Comment in method
             */|}
        }
    }
    
}
";

        await VerifyBlockSpansAsync(code,
            Region("span1", "/* Comment in method ...", autoCollapse: true));
    }

    [Fact, WorkItem(64001, "https://github.com/dotnet/roslyn/issues/64001")]
    public async Task TestMultilineCommentInLocalFunction()
    {
        const string code = @"
namespace M
{
    class C
    {
        void M()
        {
            void LocalFunc()
            {
                $${|span1:/* Comment in local function
                 */|}
            }
        }
    }
    
}
";

        await VerifyBlockSpansAsync(code,
            Region("span1", "/* Comment in local function ...", autoCollapse: true));
    }

    [Fact, WorkItem(64001, "https://github.com/dotnet/roslyn/issues/64001")]
    public async Task TestMultilineCommentInConstructor()
    {
        const string code = @"
namespace M
{
    class C
    {
        C()
        {
            $${|span1:/* Comment in constructor
             */|}
        }
    }
    
}
";

        await VerifyBlockSpansAsync(code,
            Region("span1", "/* Comment in constructor ...", autoCollapse: true));
    }

    [Fact, WorkItem(16186, "https://github.com/dotnet/roslyn/issues/16186")]
    public async Task TestInvalidComment()
    {
        const string code = @"$${|span:/*/|}";

        await VerifyBlockSpansAsync(code,
            Region("span", "/* / ...", autoCollapse: true));
    }
}
