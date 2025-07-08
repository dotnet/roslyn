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
    public async Task TestInTokenDirectlyUnderNode()
    {
        await TestAsync<LocalFunctionStatementSyntax>("""
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
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/35525")]
    public async Task TestBeforeTokenDirectlyUnderNode()
    {
        await TestAsync<LocalFunctionStatementSyntax>("""
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
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/35525")]
    public async Task TestAfterTokenDirectlyUnderNode()
    {
        await TestAsync<LocalFunctionStatementSyntax>("""
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
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/35525")]
    public async Task TestMissingInTokenUnderDifferentNode()
    {
        await TestMissingAsync<LocalFunctionStatementSyntax>("""
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
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/35525")]
    public async Task TestClimbRightEdge()
    {
        await TestAsync<LocalFunctionStatementSyntax>("""
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
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/35525")]
    public async Task TestClimbLeftEdge()
    {
        await TestAsync<LocalFunctionStatementSyntax>("""
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
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/35525")]
    public async Task TestClimbLeftEdgeComments()
    {
        await TestAsync<LocalFunctionStatementSyntax>("""
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
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/35525")]
    public async Task TestMissingInAnotherChildNode()
    {
        await TestMissingAsync<LocalFunctionStatementSyntax>("""
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
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/35525")]
    public async Task TestMissingInTooFarBeforeInWhitespace()
    {
        await TestMissingAsync<LocalFunctionStatementSyntax>("""
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
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/35525")]
    public async Task TestMissingInWhiteSpaceOnLineWithDifferentStatement()
    {
        await TestMissingAsync<LocalFunctionStatementSyntax>("""
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
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/35525")]
    public async Task TestNotBeforePrecedingComment()
    {
        await TestMissingAsync<LocalFunctionStatementSyntax>("""
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
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/35525")]
    public async Task TestBeforeInWhitespace1_OnSameLine()
    {
        await TestAsync<LocalFunctionStatementSyntax>("""
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
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/35525")]
    public async Task TestBeforeInWhitespace1_OnPreviousLine()
    {
        await TestAsync<LocalFunctionStatementSyntax>("""
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
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/35525")]
    public async Task TestBeforeInWhitespace1_NotOnMultipleLinesPrior()
    {
        await TestMissingAsync<LocalFunctionStatementSyntax>("""
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
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/35525")]
    public async Task TestBeforeInWhitespace2()
    {
        await TestAsync<LocalFunctionStatementSyntax>("""
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
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/35525")]
    public async Task TestMissingInNextTokensLeadingTrivia()
    {
        await TestMissingAsync<LocalFunctionStatementSyntax>("""
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
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/35525")]
    public async Task TestInEmptySyntaxNode_AllowEmptyNodesTrue1()
    {
        await TestAsync<ArgumentSyntax>("""
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
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/35525")]
    public async Task TestInEmptySyntaxNode_AllowEmptyNodesTrue2()
    {
        await TestAsync<ArgumentSyntax>("""
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
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/35525")]
    public async Task TestInEmptySyntaxNode_AllowEmptyNodesFalse1()
    {
        await TestMissingAsync<ArgumentSyntax>("""
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
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/35525")]
    public async Task TestInEmptySyntaxNode_AllowEmptyNodesFalse2()
    {
        await TestAsync<ArgumentSyntax>("""
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
    }

    #endregion

    #region Selections
    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/35525")]
    public async Task TestSelectedTokenDirectlyUnderNode()
    {
        await TestAsync<LocalFunctionStatementSyntax>("""
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
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/35525")]
    public async Task TestPartiallySelectedTokenDirectlyUnderNode()
    {
        await TestAsync<LocalFunctionStatementSyntax>("""
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
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/35525")]
    public async Task TestSelectedMultipleTokensUnderNode()
    {
        await TestAsync<LocalFunctionStatementSyntax>("""
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
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/35525")]
    public async Task TestMissingSelectedMultipleTokensWithLowerCommonAncestor()
    {
        await TestMissingAsync<LocalFunctionStatementSyntax>("""
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
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/35525")]
    public async Task TestMissingSelectedLowerNode()
    {
        await TestMissingAsync<LocalFunctionStatementSyntax>("""
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
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/35525")]
    public async Task TestMissingSelectedWhitespace()
    {
        await TestMissingAsync<LocalFunctionStatementSyntax>("""
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
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/35525")]
    public async Task TestMissingSelectedWhitespace2()
    {
        await TestMissingAsync<LocalFunctionStatementSyntax>("""
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
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/35525")]
    public async Task TestCompleteSelection()
    {
        await TestAsync<LocalFunctionStatementSyntax>("""
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
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/35525")]
    public async Task TestOverSelection()
    {
        await TestAsync<LocalFunctionStatementSyntax>("""
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
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/35525")]
    public async Task TestOverSelectionComments()
    {
        await TestAsync<LocalFunctionStatementSyntax>("""
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
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/35525")]
    public async Task TestMissingOverSelection()
    {
        await TestMissingAsync<LocalFunctionStatementSyntax>("""
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
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/35525")]
    public async Task TestMissingSelectionBefore()
    {
        await TestMissingAsync<LocalFunctionStatementSyntax>("""
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
    }

    #endregion

    #region IsUnderselected
    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/38708")]
    public async Task TestUnderselectionOnSemicolon()
    {
        await TestNotUnderselectedAsync<ExpressionSyntax>("""
            class Program
            {
                static void Main()
                {
                    {|result:Main()|}[|;|]
                }
            }
            """);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/38708")]
    public async Task TestUnderselectionBug1()
    {
        await TestNotUnderselectedAsync<ExpressionSyntax>("""
            class Program
            {
                public static void Method()
                {
                    //[|>
                    var str = {|result:" <|] aaa"|};
                }
            }
            """);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/38708")]
    public async Task TestUnderselectionBug2()
    {
        await TestNotUnderselectedAsync<ExpressionSyntax>("""
            class C {
                public void M()
                {
                    Console.WriteLine("Hello world");[|
                    {|result:Console.WriteLine(new |]C())|};
                    }
                }
            """);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/38708")]
    public async Task TestUnderselection()
    {
        await TestNotUnderselectedAsync<BinaryExpressionSyntax>("""
            class C {
                public void M()
                {
                    bool a = {|result:[|true || false || true|]|};
                }
            """);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/38708")]
    public async Task TestUnderselection2()
    {
        await TestUnderselectedAsync<BinaryExpressionSyntax>("""
            class C {
                public void M()
                {
                    bool a = true || [|false || true|] || true;
                }
            """);
    }
    #endregion

    #region Attributes
    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/37584")]
    public async Task TestMissingEmptyMember()
    {
        await TestMissingAsync<MethodDeclarationSyntax>("""
            using System;
            public class Class1
            {
                [][||]
            }
            """);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/38502")]
    public async Task TestIncompleteAttribute()
    {
        await TestAsync<MethodDeclarationSyntax>("""
            using System;
            public class Class1
            {
                {|result:void foo([[||]bar) {}|}
            }
            """);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/38502")]
    public async Task TestIncompleteAttribute2()
    {
        await TestAsync<MethodDeclarationSyntax>("""
            using System;
            public class Class1
            {
                {|result:void foo([[||]Class1 arg1) {}|}
            }
            """);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/37837")]
    public async Task TestEmptyParameter()
    {
        await TestAsync<ParameterSyntax>("""
            using System;
            public class TestAttribute : Attribute { }
            public class Class1
            {
                static void foo({|result:[Test][||]
            |}    {

                }
            }
            """);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/37584")]
    public async Task TestMissingEmptyMember2()
    {
        await TestMissingAsync<MethodDeclarationSyntax>("""
            using System;
            public class Class1
            {
                [||]// Comment 
                []
            }
            """);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/37584")]
    public async Task TestEmptyAttributeList()
    {
        await TestAsync<MethodDeclarationSyntax>("""
            using System;
            public class Class1
            {
                {|result:[]
                [||]void a() {}|}
            }
            """);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/35525")]
    public async Task TestClimbLeftEdgeBeforeAttribute()
    {
        await TestAsync<MethodDeclarationSyntax>("""
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
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/35525")]
    public async Task TestClimbLeftEdgeAfterAttribute()
    {
        await TestAsync<MethodDeclarationSyntax>("""
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
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/35525")]
    public async Task TestClimbLeftEdgeAfterAttributeComments()
    {
        await TestAsync<MethodDeclarationSyntax>("""
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
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/35525")]
    public async Task TestClimbLeftEdgeAfterAttributes()
    {
        await TestAsync<MethodDeclarationSyntax>("""
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
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/35525")]
    public async Task TestMissingBetweenAttributes()
    {
        await TestMissingAsync<MethodDeclarationSyntax>("""
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
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/35525")]
    public async Task TestMissingBetweenInAttributes()
    {
        await TestMissingAsync<MethodDeclarationSyntax>("""
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
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/35525")]
    public async Task TestMissingSelectedAttributes()
    {
        await TestMissingAsync<MethodDeclarationSyntax>("""
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
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/35525")]
    public async Task TestMissingSelectedAttribute()
    {
        await TestMissingAsync<MethodDeclarationSyntax>("""
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
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/35525")]
    public async Task TestSelectedWholeNodeAndAttributes()
    {
        await TestAsync<MethodDeclarationSyntax>("""
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
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/35525")]
    public async Task TestSelectedWholeNodeWithoutAttributes()
    {
        await TestAsync<MethodDeclarationSyntax>("""
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
    }
    #endregion

    #region Extractions general
    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/35525")]
    public async Task TestExtractionsClimbing()
    {
        await TestAsync<ObjectCreationExpressionSyntax>("""
            using System;
            class C
            {
                void M()
                {
                    var a = {|result:new object()|};[||]
                }
            }
            """);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/35525")]
    public async Task TestMissingExtractHeaderForSelection()
    {
        await TestMissingAsync<PropertyDeclarationSyntax>("""
            using System;
            class C
            {
                class TestAttribute : Attribute { }
                [Test] public [|int|] a { get; set; }
            }
            """);
    }

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
    public async Task TestExtractFromDeclaration()
    {
        await TestAsync<ObjectCreationExpressionSyntax>("""
            using System;
            class C
            {
                void M()
                {
                    [|var a = {|result:new object()|};|]
                }
            }
            """);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/35525")]
    public async Task TestExtractFromDeclaration2()
    {
        await TestAsync<ObjectCreationExpressionSyntax>("""
            using System;
            class C
            {
                void M()
                {
                    var a = [|{|result:new object()|};|]
                }
            }
            """);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/35525")]
    public async Task TestExtractFromAssignment()
    {
        await TestAsync<ObjectCreationExpressionSyntax>("""
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
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/35525")]
    public async Task TestExtractFromDeclarator()
    {
        await TestAsync<ObjectCreationExpressionSyntax>("""
            using System;
            class C
            {
                void M()
                {
                    var [|a = {|result:new object()|}|];
                }
            }
            """);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/35525")]
    public async Task TestExtractFromDeclarator2()
    {
        await TestAsync<LocalDeclarationStatementSyntax>("""
            using System;
            class C
            {
                void M()
                {
                    {|result:var [|a = new object()|];|}
                }
            }
            """);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/35525")]
    public async Task TestExtractInHeaderOfProperty()
    {
        await TestAsync<PropertyDeclarationSyntax>("""
            using System;
            class C
            {
                class TestAttribute : Attribute { }
                {|result:[Test] public i[||]nt a { get; set; }|}
            }
            """);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/35525")]
    public async Task TestMissingExtractNotInHeaderOfProperty()
    {
        await TestMissingAsync<PropertyDeclarationSyntax>("""
            using System;
            class C
            {
                class TestAttribute : Attribute { }
                [Test] public int a { [||]get; set; }
            }
            """);
    }

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
    public async Task TestNextToHidden()
    {
        await TestAsync<LocalFunctionStatementSyntax>("""
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
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/35525")]
    public async Task TestNextToHidden2()
    {
        await TestAsync<LocalFunctionStatementSyntax>("""
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
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/35525")]
    public async Task TestMissingHidden()
    {
        await TestMissingAsync<LocalFunctionStatementSyntax>("""
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
    }
    #endregion

    #region Test predicate
    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/35525")]
    public async Task TestMissingPredicate()
    {
        await TestMissingAsync<ArgumentSyntax>("""
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
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/35525")]
    public async Task TestArgument()
    {
        await TestAsync<ArgumentSyntax>("""
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
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/35525")]
    public async Task TestPredicate()
    {
        await TestAsync<ArgumentSyntax>("""
            class C
            {
                void M()
                {
                    var a = ({|result:[||]2 + 3|}, 2 + 3);
                }
            }
            """, n => n.Parent is TupleExpressionSyntax);
    }
    #endregion

    #region Test arguments
    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/35525")]
    public async Task TestArgumentsExtractionsInInitializer()
    {
        await TestAsync<ParameterSyntax>("""
            using System;
            class C
            {
                class TestAttribute : Attribute { }
                public C({|result:[Test]int a = [||]42|}, int b = 41) {}
            }
            """);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/35525")]
    public async Task TestMissingArgumentsExtractionsSelectInitializer()
    {
        await TestMissingAsync<ParameterSyntax>("""
            using System;
            class C
            {
                class TestAttribute : Attribute { }
                public C([Test]int a = [|42|], int b = 41) {}
            }
            """);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/35525")]
    public async Task TestMissingArgumentsExtractionsSelectComma()
    {
        await TestMissingAsync<ParameterSyntax>("""
            using System;
            class C
            {
                class TestAttribute : Attribute { }
                public C([Test]int a = 42[|,|] int b = 41) {}
            }
            """);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/35525")]
    public async Task TestMissingArgumentsExtractionsInAttributes()
    {
        await TestMissingAsync<ParameterSyntax>("""
            using System;
            class C
            {
                class TestAttribute : Attribute { }
                public C([[||]Test]int a = 42, int b = 41) {}
            }
            """);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/35525")]
    public async Task TestMissingArgumentsExtractionsSelectType1()
    {
        await TestMissingAsync<ParameterSyntax>("""
            using System;
            class C
            {
                class TestAttribute : Attribute { }
                public C([Test][|int|] a = 42, int b = 41) {}
            }
            """);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/35525")]
    public async Task TestMissingArgumentsExtractionsSelectType2()
    {
        await TestMissingAsync<ParameterSyntax>("""
            using System;
            class C
            {
                class TestAttribute : Attribute { }
                public C([Test][|C|] a = null, int b = 41) {}
            }
            """);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/35525")]
    public async Task TestArgumentsExtractionsAtTheEnd()
    {
        await TestAsync<ParameterSyntax>("""
            using System;
            class C
            {
                class TestAttribute : Attribute { }
                public C({|result:[Test]int a = 42[||]|}, int b = 41) {}
            }
            """);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/35525")]
    public async Task TestArgumentsExtractionsBefore()
    {
        await TestAsync<ParameterSyntax>("""
            using System;
            class C
            {
                class TestAttribute : Attribute { }
                public C([||]{|result:[Test]int a = 42|}, int b = 41) {}
            }
            """);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/35525")]
    public async Task TestArgumentsExtractionsSelectParamName()
    {
        await TestAsync<ParameterSyntax>("""
            using System;
            class C
            {
                class TestAttribute : Attribute { }
                public C({|result:[Test]int [|a|] = 42|}, int b = 41) {}
            }
            """);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/35525")]
    public async Task TestArgumentsExtractionsSelectParam1()
    {
        await TestAsync<ParameterSyntax>("""
            using System;
            class C
            {
                class TestAttribute : Attribute { }
                public C({|result:[Test][|int a|] = 42|}, int b = 41) {}
            }
            """);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/35525")]
    public async Task TestArgumentsExtractionsSelectParam2()
    {
        await TestAsync<ParameterSyntax>("""
            using System;
            class C
            {
                class TestAttribute : Attribute { }
                public C([|{|result:[Test]int a = 42|}|], int b = 41) {}
            }
            """);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/35525")]
    public async Task TestArgumentsExtractionsSelectParam3()
    {
        await TestAsync<ParameterSyntax>("""
            using System;
            class C
            {
                class TestAttribute : Attribute { }
                public C({|result:[Test][|int a = 42|]|}, int b = 41) {}
            }
            """);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/35525")]
    public async Task TestArgumentsExtractionsInHeader()
    {
        await TestAsync<ParameterSyntax>("""
            using System;
            class CC
            {
                class TestAttribute : Attribute { }
                public CC({|result:[Test]C[||]C a = 42|}, int b = 41) {}
            }
            """);
    }

    #endregion

    #region Test methods
    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/35525")]
    public async Task TestMissingMethodExplicitInterfaceSelection()
    {
        await TestMissingAsync<MethodDeclarationSyntax>("""
            using System;
            class C
            {
                class TestAttribute : Attribute { }
                public void [|I|].A([Test]int a = 42, int b = 41) {}
            }
            """);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/35525")]
    public async Task TestMethodCaretBeforeInterfaceSelection()
    {
        await TestAsync<MethodDeclarationSyntax>("""
            using System;
            class C
            {
                class TestAttribute : Attribute { }
                {|result:public void [||]I.A([Test]int a = 42, int b = 41) {}|}
            }
            """);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/35525")]
    public async Task TestMethodNameAndExplicitInterfaceSelection()
    {
        await TestAsync<MethodDeclarationSyntax>("""
            using System;
            class C
            {
                class TestAttribute : Attribute { }
                {|result:public void [|I.A|]([Test]int a = 42, int b = 41) {}|}
            }
            """);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/35525")]
    public async Task TestMethodInHeader()
    {
        await TestAsync<MethodDeclarationSyntax>("""
            using System;
            class CC
            {
                class TestAttribute : Attribute { }
                {|result:public C[||]C I.A([Test]int a = 42, int b = 41) { return null; }|}
            }
            """);
    }

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
    public async Task TestDeepIn()
    {
        await TestAsync<ArgumentSyntax>("""
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
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/35525")]
    public async Task TestMissingDeepInSecondRow()
    {
        await TestMissingAsync<ArgumentSyntax>("""
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
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/35525")]
    public async Task TestDeepInExpression()
    {
        await TestAsync<ArgumentSyntax>("""
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
    }
    #endregion
}
