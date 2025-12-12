// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Structure;
using Microsoft.CodeAnalysis.Structure;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Structure;

[Trait(Traits.Feature, Traits.Features.Outlining)]
public sealed class MultilineCommentStructureTests : AbstractCSharpSyntaxTriviaStructureTests
{
    internal override AbstractSyntaxStructureProvider CreateProvider() => new MultilineCommentBlockStructureProvider();

    [Fact]
    public Task TestMultilineComment1()
        => VerifyBlockSpansAsync("""
            {|span:/* Hello
            $$C# */|}
            class C
            {
            }
            """,
            Region("span", "/* Hello ...", autoCollapse: true));

    [Fact]
    public Task TestMultilineCommentOnOneLine()
        => VerifyBlockSpansAsync("""
            {|span:/* Hello $$C# */|}
            class C
            {
            }
            """,
            Region("span", "/* Hello C# ...", autoCollapse: true));

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1108049")]
    [WorkItem("https://github.com/dotnet/roslyn/issues/791")]
    public Task TestIncompleteMultilineCommentZeroSpace()
        => VerifyBlockSpansAsync("""
            {|span:$$/*|}
            """,
            Region("span", "/*  ...", autoCollapse: true));

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1108049")]
    [WorkItem("https://github.com/dotnet/roslyn/issues/791")]
    public Task TestIncompleteMultilineCommentSingleSpace()
        => VerifyBlockSpansAsync("""
            {|span:$$/* |}
            """,
            Region("span", "/*  ...", autoCollapse: true));

    [Fact]
    public Task TestPropertyGetterWithMultiLineComments1()
        => VerifyBlockSpansAsync("""
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
            """,
            Region("span1", "/* My ...", autoCollapse: true));

    [Fact]
    public Task TestPropertyGetterWithMultiLineComments2()
        => VerifyBlockSpansAsync("""
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
            """,
            Region("span1", "/* My ...", autoCollapse: true));

    [Fact]
    public Task TestPropertyGetterWithMultiLineComments3()
        => VerifyBlockSpansAsync("""
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
            """,
            Region("span1", "/* My ...", autoCollapse: true));

    [Fact]
    public Task TestPropertySetterWithMultiLineComments1()
        => VerifyBlockSpansAsync("""
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
            }
            """,
            Region("span1", "/* My ...", autoCollapse: true));

    [Fact]
    public Task TestPropertySetterWithMultiLineComments2()
        => VerifyBlockSpansAsync("""
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
            }
            """,
            Region("span1", "/* My ...", autoCollapse: true));

    [Fact]
    public Task TestPropertySetterWithMultiLineComments3()
        => VerifyBlockSpansAsync("""
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
            }
            """,
            Region("span1", "/* My ...", autoCollapse: true));

    [Fact]
    public Task TestMultilineCommentInFile()
        => VerifyBlockSpansAsync("""
            $${|span1:/* Comment in file
             */|}
            namespace M
            {
            }
            """,
            Region("span1", "/* Comment in file ...", autoCollapse: true));

    [Fact]
    public Task TestMultilineCommentInNamespace()
        => VerifyBlockSpansAsync("""
            namespace M
            {
                $${|span1:/* Comment in namespace
                 */|}
            }
            """,
            Region("span1", "/* Comment in namespace ...", autoCollapse: true));

    [Fact]
    public Task TestMultilineCommentInClass()
        => VerifyBlockSpansAsync("""
            namespace M
            {
                class C
                {
                    $${|span1:/* Comment in class
                     */|}
                }

            }
            """,
            Region("span1", "/* Comment in class ...", autoCollapse: true));

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/64001")]
    public Task TestMultilineCommentInMethod()
        => VerifyBlockSpansAsync("""
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
            """,
            Region("span1", "/* Comment in method ...", autoCollapse: true));

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/64001")]
    public Task TestMultilineCommentInLocalFunction()
        => VerifyBlockSpansAsync("""
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
            """,
            Region("span1", "/* Comment in local function ...", autoCollapse: true));

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/64001")]
    public Task TestMultilineCommentInConstructor()
        => VerifyBlockSpansAsync("""
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
            """,
            Region("span1", "/* Comment in constructor ...", autoCollapse: true));

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/16186")]
    public Task TestInvalidComment()
        => VerifyBlockSpansAsync(@"$${|span:/*/|}",
            Region("span", "/* / ...", autoCollapse: true));
}
