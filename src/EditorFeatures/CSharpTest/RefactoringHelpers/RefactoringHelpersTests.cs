// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editor.UnitTests.RefactoringHelpers;
using Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.RefactoringHelpers;

public sealed partial class RefactoringHelpersTests : RefactoringHelpersTestBase<CSharpTestWorkspaceFixture>
{
    #region Locations
    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/35525")]
    public Task TestInTokenDirectlyUnderNode()
        => TestAsync<LocalFunctionStatementSyntax>("""
            class C
            {
                void M()
                {
                    {|result:C Local[||]Function(C c)
                    {
                        return null;
                    }|}
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/35525")]
    public Task TestBeforeTokenDirectlyUnderNode()
        => TestAsync<LocalFunctionStatementSyntax>("""
            class C
            {
                void M()
                {
                    {|result:C [||]LocalFunction(C c)
                    {
                        return null;
                    }|}
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/35525")]
    public Task TestAfterTokenDirectlyUnderNode()
        => TestAsync<LocalFunctionStatementSyntax>("""
            class C
            {
                void M()
                {
                    {|result:C [||]LocalFunction(C c)
                    {
                        return null;
                    }|}
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/35525")]
    public Task TestMissingInTokenUnderDifferentNode()
        => TestMissingAsync<LocalFunctionStatementSyntax>("""
            class C
            {
                void M()
                {
                    C LocalFunction(C c)
                    {
                        [||]return null;
                    }

                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/35525")]
    public Task TestClimbRightEdge()
        => TestAsync<LocalFunctionStatementSyntax>("""
            class C
            {
                void M()
                {
                    {|result:C LocalFunction(C c)
                    {
                        return null;
                    }[||]|}
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/35525")]
    public Task TestClimbLeftEdge()
        => TestAsync<LocalFunctionStatementSyntax>("""
            class C
            {
                void M()
                {
                    {|result:[||]C LocalFunction(C c)
                    {
                        return null;
                    }|}
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/35525")]
    public Task TestClimbLeftEdgeComments()
        => TestAsync<LocalFunctionStatementSyntax>("""
            class C
            {
                void M()
                {
                    /// <summary>
                    /// Comment1
                    /// </summary>
                    {|result:[||]C LocalFunction(C c)
                    {
                        return null;
                    }|}
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/35525")]
    public Task TestMissingInAnotherChildNode()
        => TestMissingAsync<LocalFunctionStatementSyntax>("""
            class C
            {
                void M()
                {
                    C LocalFunction(C c)
                    {
                        [||]return null;
                    }
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/35525")]
    public Task TestMissingInTooFarBeforeInWhitespace()
        => TestMissingAsync<LocalFunctionStatementSyntax>("""
            class C
            {
                void M()
                {
                    [||]

                    C LocalFunction(C c)
                    {
                        return null;
                    }

                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/35525")]
    public Task TestMissingInWhiteSpaceOnLineWithDifferentStatement()
        => TestMissingAsync<LocalFunctionStatementSyntax>("""
            class C
            {
                void M()
                {

                    var a = null; [||]
                    C LocalFunction(C c)
                    {
                        return null;
                    }

                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/35525")]
    public Task TestNotBeforePrecedingComment()
        => TestMissingAsync<LocalFunctionStatementSyntax>("""
            class C
            {
                void M()
                {
                    [||]//Test comment
                    C LocalFunction(C c)
                    {
                        return null;
                    }
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/35525")]
    public Task TestBeforeInWhitespace1_OnSameLine()
        => TestAsync<LocalFunctionStatementSyntax>("""
            class C
            {
                void M()
                {
            [||]    {|result:C LocalFunction(C c)
                    {
                        return null;
                    }|}
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/35525")]
    public Task TestBeforeInWhitespace1_OnPreviousLine()
        => TestAsync<LocalFunctionStatementSyntax>("""
            class C
            {
                void M()
                {
                    [||]
                    {|result:C LocalFunction(C c)
                    {
                        return null;
                    }|}
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/35525")]
    public Task TestBeforeInWhitespace1_NotOnMultipleLinesPrior()
        => TestMissingAsync<LocalFunctionStatementSyntax>("""
            class C
            {
                void M()
                {
                    [||]

                    C LocalFunction(C c)
                    {
                        return null;
                    }
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/35525")]
    public Task TestBeforeInWhitespace2()
        => TestAsync<LocalFunctionStatementSyntax>("""
            class C
            {
                void M()
                {
                        var a = null;
            [||]        {|result:C LocalFunction(C c)
                    {
                        return null;
                    }|}
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/35525")]
    public Task TestMissingInNextTokensLeadingTrivia()
        => TestMissingAsync<LocalFunctionStatementSyntax>("""
            class C
            {
                void M()
                {

                    C LocalFunction(C c)
                    {
                        return null;
                    }
                    [||]
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/35525")]
    public Task TestInEmptySyntaxNode_AllowEmptyNodesTrue1()
        => TestAsync<ArgumentSyntax>("""
            class C
            {
                void M()
                {
                    N(0, [||]{|result:|}); 
                }

                int N(int a, int b, int c)
                {
                }
            }
            """, allowEmptyNodes: true);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/35525")]
    public Task TestInEmptySyntaxNode_AllowEmptyNodesTrue2()
        => TestAsync<ArgumentSyntax>("""
            class C
            {
                void M()
                {
                    N(0, N(0, [||]{|result:|}, 0)); 
                }

                int N(int a, int b, int c)
                {
                }
            }
            """, allowEmptyNodes: true);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/35525")]
    public Task TestInEmptySyntaxNode_AllowEmptyNodesFalse1()
        => TestMissingAsync<ArgumentSyntax>("""
            class C
            {
                void M()
                {
                    N(0, [||], 0)); 
                }

                int N(int a, int b, int c)
                {
                }
            }
            """, allowEmptyNodes: false);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/35525")]
    public Task TestInEmptySyntaxNode_AllowEmptyNodesFalse2()
        => TestAsync<ArgumentSyntax>("""
            class C
            {
                void M()
                {
                    N(0, {|result:N(0, [||], 0)|}); 
                }

                int N(int a, int b, int c)
                {
                }
            }
            """, allowEmptyNodes: false);

    #endregion

    #region Selections
    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/35525")]
    public Task TestSelectedTokenDirectlyUnderNode()
        => TestAsync<LocalFunctionStatementSyntax>("""
            class C
            {
                void M()
                {
                    {|result:C [|LocalFunction|](C c)
                    {
                        return null;
                    }|}
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/35525")]
    public Task TestPartiallySelectedTokenDirectlyUnderNode()
        => TestAsync<LocalFunctionStatementSyntax>("""
            class C
            {
                void M()
                {
                    {|result:C Lo[|calFunct|]ion(C c)
                    {
                        return null;
                    }|}
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/35525")]
    public Task TestSelectedMultipleTokensUnderNode()
        => TestAsync<LocalFunctionStatementSyntax>("""
            class C
            {
                void M()
                {
                    {|result:[|C LocalFunction(C c)|]
                    {
                        return null;
                    }|}
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/35525")]
    public Task TestMissingSelectedMultipleTokensWithLowerCommonAncestor()
        => TestMissingAsync<LocalFunctionStatementSyntax>("""
            class C
            {
                void M()
                {
                    C LocalFunction(C c)
                    [|{
                        return null;
                    }|]
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/35525")]
    public Task TestMissingSelectedLowerNode()
        => TestMissingAsync<LocalFunctionStatementSyntax>("""
            class C
            {
                void M()
                {
                    [|C|] LocalFunction(C c)
                    {
                        return null;
                    }
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/35525")]
    public Task TestMissingSelectedWhitespace()
        => TestMissingAsync<LocalFunctionStatementSyntax>("""
            class C
            {
                void M()
                {
                    C[| |]LocalFunction(C c)
                    {
                        return null;
                    }
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/35525")]
    public Task TestMissingSelectedWhitespace2()
        => TestMissingAsync<LocalFunctionStatementSyntax>("""
            class C
            {
                void M()
                {
                   [| |]C LocalFunction(C c)
                    {
                        return null;
                    }
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/35525")]
    public Task TestCompleteSelection()
        => TestAsync<LocalFunctionStatementSyntax>("""
            class C
            {
                void M()
                {
                    {|result:[|C LocalFunction(C c)
                    {
                        return null;
                    }|]|}
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/35525")]
    public Task TestOverSelection()
        => TestAsync<LocalFunctionStatementSyntax>("""
            class C
            {
                void M()
                {
                    [|

                    {|result:C LocalFunction(C c)
                    {
                        return null;
                    }|}

                    |]var a = new object();
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/35525")]
    public Task TestOverSelectionComments()
        => TestAsync<LocalFunctionStatementSyntax>("""
            class C
            {
                void M()
                {

                    // Co[|mment1
                    {|result:C LocalFunction(C c)
                    {
                        return null;
                    }|}|]
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/35525")]
    public Task TestMissingOverSelection()
        => TestMissingAsync<LocalFunctionStatementSyntax>("""
            class C
            {
                void M()
                {
                    [|
                    C LocalFunction(C c)
                    {
                        return null;
                    }
                    v|]ar a = new object();
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/35525")]
    public Task TestMissingSelectionBefore()
        => TestMissingAsync<LocalFunctionStatementSyntax>("""
            class C
            {
                void M()
                {

             [|       |]C LocalFunction(C c)
                    {
                        return null;
                    }
                    var a = new object();
                }
            }
            """);

    #endregion

    #region IsUnderselected
    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/38708")]
    public Task TestUnderselectionOnSemicolon()
        => TestNotUnderselectedAsync<ExpressionSyntax>("""
            class Program
            {
                static void Main()
                {
                    {|result:Main()|}[|;|]
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/38708")]
    public Task TestUnderselectionBug1()
        => TestNotUnderselectedAsync<ExpressionSyntax>("""
            class Program
            {
                public static void Method()
                {
                    //[|>
                    var str = {|result:" <|] aaa"|};
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/38708")]
    public Task TestUnderselectionBug2()
        => TestNotUnderselectedAsync<ExpressionSyntax>("""
            class C {
                public void M()
                {
                    Console.WriteLine("Hello world");[|
                    {|result:Console.WriteLine(new |]C())|};
                    }
                }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/38708")]
    public Task TestUnderselection()
        => TestNotUnderselectedAsync<BinaryExpressionSyntax>("""
            class C {
                public void M()
                {
                    bool a = {|result:[|true || false || true|]|};
                }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/38708")]
    public Task TestUnderselection2()
        => TestUnderselectedAsync<BinaryExpressionSyntax>("""
            class C {
                public void M()
                {
                    bool a = true || [|false || true|] || true;
                }
            """);
    #endregion

    #region Attributes
    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/37584")]
    public Task TestMissingEmptyMember()
        => TestMissingAsync<MethodDeclarationSyntax>("""
            using System;
            public class Class1
            {
                [][||]
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/38502")]
    public Task TestIncompleteAttribute()
        => TestAsync<MethodDeclarationSyntax>("""
            using System;
            public class Class1
            {
                {|result:void foo([[||]bar) {}|}
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/38502")]
    public Task TestIncompleteAttribute2()
        => TestAsync<MethodDeclarationSyntax>("""
            using System;
            public class Class1
            {
                {|result:void foo([[||]Class1 arg1) {}|}
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/37837")]
    public Task TestEmptyParameter()
        => TestAsync<ParameterSyntax>("""
            using System;
            public class TestAttribute : Attribute { }
            public class Class1
            {
                static void foo({|result:[Test][||]
            |}    {

                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/37584")]
    public Task TestMissingEmptyMember2()
        => TestMissingAsync<MethodDeclarationSyntax>("""
            using System;
            public class Class1
            {
                [||]// Comment 
                []
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/37584")]
    public Task TestEmptyAttributeList()
        => TestAsync<MethodDeclarationSyntax>("""
            using System;
            public class Class1
            {
                {|result:[]
                [||]void a() {}|}
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/35525")]
    public Task TestClimbLeftEdgeBeforeAttribute()
        => TestAsync<MethodDeclarationSyntax>("""
            using System;
            class C
            {
                class TestAttribute : Attribute { }

                // Comment1
                [||]{|result:[Test]
                void M()
                {
                }|}
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/35525")]
    public Task TestClimbLeftEdgeAfterAttribute()
        => TestAsync<MethodDeclarationSyntax>("""
            using System;
            class C
            {
                class TestAttribute : Attribute { }

                {|result:[Test]
                [||]void M()
                {
                }|}
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/35525")]
    public Task TestClimbLeftEdgeAfterAttributeComments()
        => TestAsync<MethodDeclarationSyntax>("""
            using System;
            class C
            {
                class TestAttribute : Attribute { }

                // Comment1
                {|result:[Test]
                // Comment2
                [||]void M()
                {
                }|}
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/35525")]
    public Task TestClimbLeftEdgeAfterAttributes()
        => TestAsync<MethodDeclarationSyntax>("""
            using System;
            class C
            {
                class TestAttribute : Attribute { }
                class Test2Attribute : Attribute { }

                // Comment1
                {|result:[Test]
                [Test2]
                // Comment2
                [||]void M()
                {
                }|}
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/35525")]
    public Task TestMissingBetweenAttributes()
        => TestMissingAsync<MethodDeclarationSyntax>("""
            using System;
            class C
            {
                class TestAttribute : Attribute { }
                class Test2Attribute : Attribute { }

                [Test]
                [||][Test2]
                void M()
                {
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/35525")]
    public Task TestMissingBetweenInAttributes()
        => TestMissingAsync<MethodDeclarationSyntax>("""
            using System;
            class C
            {
                class TestAttribute : Attribute { }

                [[||]Test]
                void M()
                {
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/35525")]
    public Task TestMissingSelectedAttributes()
        => TestMissingAsync<MethodDeclarationSyntax>("""
            using System;
            class C
            {
                class TestAttribute : Attribute { }
                class Test2Attribute : Attribute { }

                [|[Test]
                [Test2]|]
                void M()
                {
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/35525")]
    public Task TestMissingSelectedAttribute()
        => TestMissingAsync<MethodDeclarationSyntax>("""
            using System;
            class C
            {
                class TestAttribute : Attribute { }

                [|[Test]|]
                void M()
                {
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/35525")]
    public Task TestSelectedWholeNodeAndAttributes()
        => TestAsync<MethodDeclarationSyntax>("""
            using System;
            class C
            {
                class TestAttribute : Attribute { }
                class Test2Attribute : Attribute { }

                // Comment1
                [|{|result:[Test]
                [Test2]
                // Comment2
                void M()
                {
                }|}|]
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/35525")]
    public Task TestSelectedWholeNodeWithoutAttributes()
        => TestAsync<MethodDeclarationSyntax>("""
            using System;
            class C
            {
                class TestAttribute : Attribute { }
                class Test2Attribute : Attribute { }

                // Comment1
                {|result:[Test]
                [Test2]
                // Comment2
                [|void M()
                {
                }|]|}
            }
            """);
    #endregion

    #region Extractions general
    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/35525")]
    public Task TestExtractionsClimbing()
        => TestAsync<ObjectCreationExpressionSyntax>("""
            using System;
            class C
            {
                void M()
                {
                    var a = {|result:new object()|};[||]
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/35525")]
    public Task TestMissingExtractHeaderForSelection()
        => TestMissingAsync<PropertyDeclarationSyntax>("""
            using System;
            class C
            {
                class TestAttribute : Attribute { }
                [Test] public [|int|] a { get; set; }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/35525")]
    public async Task TestMultipleExtractions()
    {
        await TestAsync<LocalDeclarationStatementSyntax>(GetTestText("{|result:string name = \"\", b[||]b = null;|}"));
        await TestAsync<VariableDeclaratorSyntax>(GetTestText("string name = \"\", {|result:bb[||] = null|};"));

        static string GetTestText(string data)
        {
            return """
                class C
                {
                    void M()
                    {
                        C LocalFunction(C c)
                        {
                """ + data + """
                return null;
                        }
                        var a = new object();
                    }
                }
                """;

        }
    }
    #endregion

    #region Extractions
    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/35525")]
    public Task TestExtractFromDeclaration()
        => TestAsync<ObjectCreationExpressionSyntax>("""
            using System;
            class C
            {
                void M()
                {
                    [|var a = {|result:new object()|};|]
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/35525")]
    public Task TestExtractFromDeclaration2()
        => TestAsync<ObjectCreationExpressionSyntax>("""
            using System;
            class C
            {
                void M()
                {
                    var a = [|{|result:new object()|};|]
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/35525")]
    public Task TestExtractFromAssignment()
        => TestAsync<ObjectCreationExpressionSyntax>("""
            using System;
            class C
            {
                void M()
                {
                    object a;
                    a = [|{|result:new object()|};|]
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/35525")]
    public Task TestExtractFromDeclarator()
        => TestAsync<ObjectCreationExpressionSyntax>("""
            using System;
            class C
            {
                void M()
                {
                    var [|a = {|result:new object()|}|];
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/35525")]
    public Task TestExtractFromDeclarator2()
        => TestAsync<LocalDeclarationStatementSyntax>("""
            using System;
            class C
            {
                void M()
                {
                    {|result:var [|a = new object()|];|}
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/35525")]
    public Task TestExtractInHeaderOfProperty()
        => TestAsync<PropertyDeclarationSyntax>("""
            using System;
            class C
            {
                class TestAttribute : Attribute { }
                {|result:[Test] public i[||]nt a { get; set; }|}
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/35525")]
    public Task TestMissingExtractNotInHeaderOfProperty()
        => TestMissingAsync<PropertyDeclarationSyntax>("""
            using System;
            class C
            {
                class TestAttribute : Attribute { }
                [Test] public int a { [||]get; set; }
            }
            """);

    #endregion

    #region Headers & holes
    [Theory]
    [InlineData("var aa = nul[||]l;")]
    [InlineData("var aa = n[||]ull;")]
    [InlineData("string aa = null, bb = n[||]ull;")]
    [WorkItem("https://github.com/dotnet/roslyn/issues/35525")]
    public async Task TestMissingInHeaderHole(string data)
    {
        var testText = """
            class C
            {
                void M()
                {
                    C LocalFunction(C c)
                    {
            """ + data + """
            return null;
                    }
                    var a = new object();
                }
            }
            """;
        await TestMissingAsync<LocalDeclarationStatementSyntax>(testText);
    }

    [Theory]
    [InlineData("{|result:[||]var aa = null;|}")]
    [InlineData("{|result:var aa = [||]null;|}")]
    [InlineData("{|result:var aa = null[||];|}")]
    [InlineData("{|result:string aa = null, b[||]b = null;|}")]
    [InlineData("{|result:string aa = null, bb = [||]null;|}")]
    [InlineData("{|result:string aa = null, bb = null[||];|}")]
    [InlineData("{|result:string aa = null, bb = null;[||]|}")]
    [WorkItem("https://github.com/dotnet/roslyn/issues/35525")]
    public async Task TestInHeader(string data)
    {
        var testText = """
            class C
            {
                void M()
                {
                    C LocalFunction(C c)
                    {
            """ + data + """
            return null;
                    }
                    var a = new object();
                }
            }
            """;
        await TestAsync<LocalDeclarationStatementSyntax>(testText);
    }
    #endregion

    #region TestHidden
    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/35525")]
    public Task TestNextToHidden()
        => TestAsync<LocalFunctionStatementSyntax>("""
            #line default
            class C
            {
                void M()
                {
            #line hidden
                    var a = b;
            #line default
                    {|result:C [||]LocalFunction(C c)
                    {
                        return null;
                    }|}
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/35525")]
    public Task TestNextToHidden2()
        => TestAsync<LocalFunctionStatementSyntax>("""
            #line default
            class C
            {
                void M()
                {
            #line hidden
                    var a = b;
            #line default
                    {|result:C [||]LocalFunction(C c)
                    {
                        return null;
                    }|}
            #line hidden
                    var a = b;
            #line default
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/35525")]
    public Task TestMissingHidden()
        => TestMissingAsync<LocalFunctionStatementSyntax>("""
            #line default
            class C
            {
                void M()
                {
            #line hidden
                    C LocalFunction(C c)
            #line default
                    {
                        return null;
                    }[||]
                }
            }
            """);
    #endregion

    #region Test predicate
    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/35525")]
    public Task TestMissingPredicate()
        => TestMissingAsync<ArgumentSyntax>("""
            class C
            {
                void M()
                {
                    N([||]2+3);
                }

                void N(int a)
                {
                }
            }
            """, n => n.Parent is TupleExpressionSyntax);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/35525")]
    public Task TestArgument()
        => TestAsync<ArgumentSyntax>("""
            class C
            {
                void M()
                {
                    N({|result:[||]2+3|});
                }

                void N(int a)
                {
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/35525")]
    public Task TestPredicate()
        => TestAsync<ArgumentSyntax>("""
            class C
            {
                void M()
                {
                    var a = ({|result:[||]2 + 3|}, 2 + 3);
                }
            }
            """, n => n.Parent is TupleExpressionSyntax);
    #endregion

    #region Test arguments
    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/35525")]
    public Task TestArgumentsExtractionsInInitializer()
        => TestAsync<ParameterSyntax>("""
            using System;
            class C
            {
                class TestAttribute : Attribute { }
                public C({|result:[Test]int a = [||]42|}, int b = 41) {}
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/35525")]
    public Task TestMissingArgumentsExtractionsSelectInitializer()
        => TestMissingAsync<ParameterSyntax>("""
            using System;
            class C
            {
                class TestAttribute : Attribute { }
                public C([Test]int a = [|42|], int b = 41) {}
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/35525")]
    public Task TestMissingArgumentsExtractionsSelectComma()
        => TestMissingAsync<ParameterSyntax>("""
            using System;
            class C
            {
                class TestAttribute : Attribute { }
                public C([Test]int a = 42[|,|] int b = 41) {}
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/35525")]
    public Task TestMissingArgumentsExtractionsInAttributes()
        => TestMissingAsync<ParameterSyntax>("""
            using System;
            class C
            {
                class TestAttribute : Attribute { }
                public C([[||]Test]int a = 42, int b = 41) {}
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/35525")]
    public Task TestMissingArgumentsExtractionsSelectType1()
        => TestMissingAsync<ParameterSyntax>("""
            using System;
            class C
            {
                class TestAttribute : Attribute { }
                public C([Test][|int|] a = 42, int b = 41) {}
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/35525")]
    public Task TestMissingArgumentsExtractionsSelectType2()
        => TestMissingAsync<ParameterSyntax>("""
            using System;
            class C
            {
                class TestAttribute : Attribute { }
                public C([Test][|C|] a = null, int b = 41) {}
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/35525")]
    public Task TestArgumentsExtractionsAtTheEnd()
        => TestAsync<ParameterSyntax>("""
            using System;
            class C
            {
                class TestAttribute : Attribute { }
                public C({|result:[Test]int a = 42[||]|}, int b = 41) {}
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/35525")]
    public Task TestArgumentsExtractionsBefore()
        => TestAsync<ParameterSyntax>("""
            using System;
            class C
            {
                class TestAttribute : Attribute { }
                public C([||]{|result:[Test]int a = 42|}, int b = 41) {}
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/35525")]
    public Task TestArgumentsExtractionsSelectParamName()
        => TestAsync<ParameterSyntax>("""
            using System;
            class C
            {
                class TestAttribute : Attribute { }
                public C({|result:[Test]int [|a|] = 42|}, int b = 41) {}
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/35525")]
    public Task TestArgumentsExtractionsSelectParam1()
        => TestAsync<ParameterSyntax>("""
            using System;
            class C
            {
                class TestAttribute : Attribute { }
                public C({|result:[Test][|int a|] = 42|}, int b = 41) {}
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/35525")]
    public Task TestArgumentsExtractionsSelectParam2()
        => TestAsync<ParameterSyntax>("""
            using System;
            class C
            {
                class TestAttribute : Attribute { }
                public C([|{|result:[Test]int a = 42|}|], int b = 41) {}
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/35525")]
    public Task TestArgumentsExtractionsSelectParam3()
        => TestAsync<ParameterSyntax>("""
            using System;
            class C
            {
                class TestAttribute : Attribute { }
                public C({|result:[Test][|int a = 42|]|}, int b = 41) {}
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/35525")]
    public Task TestArgumentsExtractionsInHeader()
        => TestAsync<ParameterSyntax>("""
            using System;
            class CC
            {
                class TestAttribute : Attribute { }
                public CC({|result:[Test]C[||]C a = 42|}, int b = 41) {}
            }
            """);

    #endregion

    #region Test methods
    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/35525")]
    public Task TestMissingMethodExplicitInterfaceSelection()
        => TestMissingAsync<MethodDeclarationSyntax>("""
            using System;
            class C
            {
                class TestAttribute : Attribute { }
                public void [|I|].A([Test]int a = 42, int b = 41) {}
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/35525")]
    public Task TestMethodCaretBeforeInterfaceSelection()
        => TestAsync<MethodDeclarationSyntax>("""
            using System;
            class C
            {
                class TestAttribute : Attribute { }
                {|result:public void [||]I.A([Test]int a = 42, int b = 41) {}|}
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/35525")]
    public Task TestMethodNameAndExplicitInterfaceSelection()
        => TestAsync<MethodDeclarationSyntax>("""
            using System;
            class C
            {
                class TestAttribute : Attribute { }
                {|result:public void [|I.A|]([Test]int a = 42, int b = 41) {}|}
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/35525")]
    public Task TestMethodInHeader()
        => TestAsync<MethodDeclarationSyntax>("""
            using System;
            class CC
            {
                class TestAttribute : Attribute { }
                {|result:public C[||]C I.A([Test]int a = 42, int b = 41) { return null; }|}
            }
            """);

    #endregion

    #region TestLocalDeclaration
    [Theory]
    [InlineData("{|result:v[||]ar name = \"\";|}")]
    [InlineData("{|result:string name = \"\", b[||]b = null;|}")]
    [InlineData("{|result:var[||] name = \"\";|}")]
    [InlineData("{|result:var [||]name = \"\";|}")]
    [InlineData("{|result:var na[||]me = \"\";|}")]
    [InlineData("{|result:var name[||] = \"\";|}")]
    [InlineData("{|result:var name [||]= \"\";|}")]
    [InlineData("{|result:var name =[||] \"\";|}")]
    [InlineData("{|result:var name = [||]\"\";|}")]
    [InlineData("{|result:[|var name = \"\";|]|}")]
    [InlineData("{|result:var name = \"\"[||];|}")]
    [InlineData("{|result:var name = \"\";[||]|}")]
    [InlineData("{|result:var name = \"\"[||]|}")]
    [WorkItem("https://github.com/dotnet/roslyn/issues/35525")]
    public async Task TestLocalDeclarationInHeader(string data)
    {
        var testText = """
            class C
            {
                void M()
                {

                    C LocalFunction(C c)
                    {
            """ + data + """
            return null;
                    }
                    var a = new object();
                }
            }
            """;
        await TestAsync<LocalDeclarationStatementSyntax>(testText);
    }

    [Theory]
    [InlineData("var name = \"[||]\";")]
    [InlineData("var name=[|\"\"|];")]
    [InlineData("string name = \"\", bb = n[||]ull;")]
    [WorkItem("https://github.com/dotnet/roslyn/issues/35525")]
    public async Task TestMissingLocalDeclarationCaretInHeader(string data)
    {
        var testText = """
            class C
            {
                void M()
                {

                    C LocalFunction(C c)
                    {
            """ + data + """
            return null;
                    }
                    var a = new object();
                }
            }
            """;
        await TestMissingAsync<LocalDeclarationStatementSyntax>(testText);
    }
    #endregion

    #region Test Ifs
    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/35525")]
    public Task TestMultiline_IfElseIfElseSelection1()
        => TestAsync<IfStatementSyntax>(
            """
            class A
            {
                void Goo()
                {
                    {|result:[|if (a)
                    {
                        a();
                    }|]
                    else if (b)
                    {
                        b();
                    }
                    else
                    {
                        c();
                    }|}
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/35525")]
    public Task TestMultiline_IfElseIfElseSelection2()
        => TestAsync<IfStatementSyntax>(
            """
            class A
            {
                void Goo()
                {
                    {|result:[|if (a)
                    {
                        a();
                    }
                    else if (b)
                    {
                        b();
                    }
                    else
                    {
                        c();
                    }|]|}
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/35525")]
    public Task TestMissingMultiline_IfElseIfElseSelection()
        => TestMissingAsync<IfStatementSyntax>(
            """
            class A
            {
                void Goo()
                {
                    if (a)
                    {
                        a();
                    }
                    [|else if (b)
                    {
                        b();
                    }
                    else
                    {
                        c();
                    }|]
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/78749")]
    public Task TestMalformedIfBlock1()
        => TestAsync<IfStatementSyntax>(
            """
            {
                {|result:[||]if (devsBad)
                    [crash]
                else return;|}
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/78749")]
    public Task TestMalformedIfBlock2()
        => TestAsync<IfStatementSyntax>(
            """
            {
                if (devsBad)
                    {|result:[|[crash]
                else return;|]|}
            }
            """);

    #endregion

    #region Test Deep in expression
    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/35525")]
    public Task TestDeepIn()
        => TestAsync<ArgumentSyntax>("""
            class C
            {
                void M()
                {
                    N({|result:2+[||]3+4|});
                }

                void N(int a)
                {
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/35525")]
    public Task TestMissingDeepInSecondRow()
        => TestMissingAsync<ArgumentSyntax>("""
            class C
            {
                void M()
                {
                    N(2
                        +[||]3+4);
                }

                void N(int a)
                {
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/35525")]
    public Task TestDeepInExpression()
        => TestAsync<ArgumentSyntax>("""
            class C
            {
                void M()
                {
                    var b = ({|result:N(2[||])|}, 0);
                }

                int N(int a)
                {
                    return a;
                }
            }
            """, predicate: n => n.Parent is TupleExpressionSyntax);
    #endregion
}
