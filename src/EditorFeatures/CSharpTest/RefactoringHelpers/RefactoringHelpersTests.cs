// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editor.UnitTests.RefactoringHelpers;
using Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.RefactoringHelpers
{
    public partial class RefactoringHelpersTests : RefactoringHelpersTestBase<CSharpTestWorkspaceFixture>
    {
        public RefactoringHelpersTests(CSharpTestWorkspaceFixture workspaceFixture) : base(workspaceFixture)
        {
        }

        #region Locations
        [Fact]
        [WorkItem(35525, "https://github.com/dotnet/roslyn/issues/35525")]
        public async Task TestInTokenDirectlyUnderNode()
        {
            var testText = @"
class C
{
    void M()
    {
        {|result:C Local[||]Function(C c)
        {
            return null;
        }|}
    }
}";
            await TestAsync<LocalFunctionStatementSyntax>(testText);
        }

        [Fact]
        [WorkItem(35525, "https://github.com/dotnet/roslyn/issues/35525")]
        public async Task TestBeforeTokenDirectlyUnderNode()
        {
            var testText = @"
class C
{
    void M()
    {
        {|result:C [||]LocalFunction(C c)
        {
            return null;
        }|}
    }
}";
            await TestAsync<LocalFunctionStatementSyntax>(testText);
        }

        [Fact]
        [WorkItem(35525, "https://github.com/dotnet/roslyn/issues/35525")]
        public async Task TestAfterTokenDirectlyUnderNode()
        {
            var testText = @"
class C
{
    void M()
    {
        {|result:C [||]LocalFunction(C c)
        {
            return null;
        }|}
    }
}";
            await TestAsync<LocalFunctionStatementSyntax>(testText);
        }

        [Fact]
        [WorkItem(35525, "https://github.com/dotnet/roslyn/issues/35525")]
        public async Task TestMissingInTokenUnderDifferentNode()
        {
            var testText = @"
class C
{
    void M()
    {
        C[||] LocalFunction(C c)
        {
            return null;
        }
        
    }
}";
            await TestMissingAsync<LocalFunctionStatementSyntax>(testText);
        }

        [Fact]
        [WorkItem(35525, "https://github.com/dotnet/roslyn/issues/35525")]
        public async Task TestClimbRightEdge()
        {
            var testText = @"
class C
{
    void M()
    {
        {|result:C LocalFunction(C c)
        {
            return null;
        }[||]|}
    }
}";
            await TestAsync<LocalFunctionStatementSyntax>(testText);
        }

        [Fact]
        [WorkItem(35525, "https://github.com/dotnet/roslyn/issues/35525")]
        public async Task TestClimbLeftEdge()
        {
            var testText = @"
class C
{
    void M()
    {
        {|result:[||]C LocalFunction(C c)
        {
            return null;
        }|}
    }
}";
            await TestAsync<LocalFunctionStatementSyntax>(testText);
        }

        [Fact]
        [WorkItem(35525, "https://github.com/dotnet/roslyn/issues/35525")]
        public async Task TestClimbLeftEdgeComments()
        {
            var testText = @"
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
}";
            await TestAsync<LocalFunctionStatementSyntax>(testText);
        }

        [Fact]
        [WorkItem(35525, "https://github.com/dotnet/roslyn/issues/35525")]
        public async Task TestMissingInAnotherChildNode()
        {
            var testText = @"
class C
{
    void M()
    {
        C LocalFunction(C c)
        {
            [||]return null;
        }
    }
}";
            await TestMissingAsync<LocalFunctionStatementSyntax>(testText);
        }

        [Fact]
        [WorkItem(35525, "https://github.com/dotnet/roslyn/issues/35525")]
        public async Task TestMissingLocationAfter()
        {
            var testText = @"
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
}";
            await TestMissingAsync<LocalFunctionStatementSyntax>(testText);
        }

        [Fact]
        [WorkItem(35525, "https://github.com/dotnet/roslyn/issues/35525")]
        public async Task TestMissingLocationBefore()
        {
            var testText = @"
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
}";
            await TestMissingAsync<LocalFunctionStatementSyntax>(testText);
        }

        [Fact]
        [WorkItem(35525, "https://github.com/dotnet/roslyn/issues/35525")]
        public async Task TestMissingLocationBefore2()
        {
            var testText = @"
class C
{
    void M()
    {
        var a = new object();[||]
        C LocalFunction(C c)
        {
            return null;
        }
    }
}";
            await TestMissingAsync<LocalFunctionStatementSyntax>(testText);
        }

        #endregion

        #region Selections
        [Fact]
        [WorkItem(35525, "https://github.com/dotnet/roslyn/issues/35525")]
        public async Task TestSelectedTokenDirectlyUnderNode()
        {
            var testText = @"
class C
{
    void M()
    {
        {|result:C [|LocalFunction|](C c)
        {
            return null;
        }|}
    }
}";
            await TestAsync<LocalFunctionStatementSyntax>(testText);
        }

        [Fact]
        [WorkItem(35525, "https://github.com/dotnet/roslyn/issues/35525")]
        public async Task TestPartiallySelectedTokenDirectlyUnderNode()
        {
            var testText = @"
class C
{
    void M()
    {
        {|result:C Lo[|calFunct|]ion(C c)
        {
            return null;
        }|}
    }
}";
            await TestAsync<LocalFunctionStatementSyntax>(testText);
        }

        [Fact]
        [WorkItem(35525, "https://github.com/dotnet/roslyn/issues/35525")]
        public async Task TestSelectedMultipleTokensUnderNode()
        {
            var testText = @"
class C
{
    void M()
    {
        {|result:[|C LocalFunction(C c)|]
        {
            return null;
        }|}
    }
}";
            await TestAsync<LocalFunctionStatementSyntax>(testText);
        }

        [Fact]
        [WorkItem(35525, "https://github.com/dotnet/roslyn/issues/35525")]
        public async Task TestMissingSelectedMultipleTokensWithLowerCommonAncestor()
        {
            var testText = @"
class C
{
    void M()
    {
        C LocalFunction(C c)
        [|{
            return null;
        }|]
    }
}";
            await TestMissingAsync<LocalFunctionStatementSyntax>(testText);
        }

        [Fact]
        [WorkItem(35525, "https://github.com/dotnet/roslyn/issues/35525")]
        public async Task TestMissingSelectedLowerNode()
        {
            var testText = @"
class C
{
    void M()
    {
        [|C|] LocalFunction(C c)
        {
            return null;
        }
    }
}";
            await TestMissingAsync<LocalFunctionStatementSyntax>(testText);
        }

        [Fact]
        [WorkItem(35525, "https://github.com/dotnet/roslyn/issues/35525")]
        public async Task TestMissingSelectedWhitespace()
        {
            var testText = @"
class C
{
    void M()
    {
        C[| |]LocalFunction(C c)
        {
            return null;
        }
    }
}";
            await TestMissingAsync<LocalFunctionStatementSyntax>(testText);
        }

        [Fact]
        [WorkItem(35525, "https://github.com/dotnet/roslyn/issues/35525")]
        public async Task TestMissingSelectedWhitespace2()
        {
            var testText = @"
class C
{
    void M()
    {
       [| |]C LocalFunction(C c)
        {
            return null;
        }
    }
}";
            await TestMissingAsync<LocalFunctionStatementSyntax>(testText);
        }

        [Fact]
        [WorkItem(35525, "https://github.com/dotnet/roslyn/issues/35525")]
        public async Task TestCompleteSelection()
        {
            var testText = @"
class C
{
    void M()
    {
        {|result:[|C LocalFunction(C c)
        {
            return null;
        }|]|}
    }
}";
            await TestAsync<LocalFunctionStatementSyntax>(testText);
        }

        [Fact]
        [WorkItem(35525, "https://github.com/dotnet/roslyn/issues/35525")]
        public async Task TestOverSelection()
        {
            var testText = @"
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
}";
            await TestAsync<LocalFunctionStatementSyntax>(testText);
        }

        [Fact]
        [WorkItem(35525, "https://github.com/dotnet/roslyn/issues/35525")]
        public async Task TestOverSelectionComments()
        {
            var testText = @"
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
}";
            await TestAsync<LocalFunctionStatementSyntax>(testText);
        }

        [Fact]
        [WorkItem(35525, "https://github.com/dotnet/roslyn/issues/35525")]
        public async Task TestMissingOverSelection()
        {
            var testText = @"
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
}";
            await TestMissingAsync<LocalFunctionStatementSyntax>(testText);
        }

        #endregion

        #region Attributes
        [Fact]
        [WorkItem(35525, "https://github.com/dotnet/roslyn/issues/35525")]
        public async Task TestClimbLeftEdgeBeforeAttribute()
        {
            var testText = @"
using System;
class C
{
    class TestAttribute : Attribute { }

    // Comment1
    [||]{|result:[Test]
    void M()
    {
    }|}
}";
            await TestAsync<MethodDeclarationSyntax>(testText);
        }

        [Fact]
        [WorkItem(35525, "https://github.com/dotnet/roslyn/issues/35525")]
        public async Task TestClimbLeftEdgeAfterAttribute()
        {
            var testText = @"
using System;
class C
{
    class TestAttribute : Attribute { }

    {|result:[Test]
    [||]void M()
    {
    }|}
}";
            await TestAsync<MethodDeclarationSyntax>(testText);
        }

        [Fact]
        [WorkItem(35525, "https://github.com/dotnet/roslyn/issues/35525")]
        public async Task TestClimbLeftEdgeAfterAttributeComments()
        {
            var testText = @"
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
}";
            await TestAsync<MethodDeclarationSyntax>(testText);
        }

        [Fact]
        [WorkItem(35525, "https://github.com/dotnet/roslyn/issues/35525")]
        public async Task TestClimbLeftEdgeAfterAttributes()
        {
            var testText = @"
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
}";
            await TestAsync<MethodDeclarationSyntax>(testText);
        }

        [Fact]
        [WorkItem(35525, "https://github.com/dotnet/roslyn/issues/35525")]
        public async Task TestMissingBetweenAttributes()
        {
            var testText = @"
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
}";
            await TestMissingAsync<MethodDeclarationSyntax>(testText);
        }

        [Fact]
        [WorkItem(35525, "https://github.com/dotnet/roslyn/issues/35525")]
        public async Task TestMissingBetweenInAttributes()
        {
            var testText = @"
using System;
class C
{
    class TestAttribute : Attribute { }

    [[||]Test]
    void M()
    {
    }
}";
            await TestMissingAsync<MethodDeclarationSyntax>(testText);
        }

        [Fact]
        [WorkItem(35525, "https://github.com/dotnet/roslyn/issues/35525")]
        public async Task TestMissingSelectedAttributes()
        {
            var testText = @"
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
}";
            await TestMissingAsync<MethodDeclarationSyntax>(testText);
        }

        [Fact]
        [WorkItem(35525, "https://github.com/dotnet/roslyn/issues/35525")]
        public async Task TestMissingSelectedAttribute()
        {
            var testText = @"
using System;
class C
{
    class TestAttribute : Attribute { }

    [|[Test]|]
    void M()
    {
    }
}";
            await TestMissingAsync<MethodDeclarationSyntax>(testText);
        }

        [Fact]
        [WorkItem(35525, "https://github.com/dotnet/roslyn/issues/35525")]
        public async Task TestSelectedWholeNodeAndAttributes()
        {
            var testText = @"
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
}";
            await TestAsync<MethodDeclarationSyntax>(testText);
        }

        [Fact]
        [WorkItem(35525, "https://github.com/dotnet/roslyn/issues/35525")]
        public async Task TestSelectedWholeNodeWithoutAttributes()
        {
            var testText = @"
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
}";
            await TestAsync<MethodDeclarationSyntax>(testText);
        }

        #endregion

        #region Extractions general
        [Fact]
        [WorkItem(35525, "https://github.com/dotnet/roslyn/issues/35525")]
        public async Task TestExtractionsClimbing()
        {
            var testText = @"
using System;
class C
{
    void M()
    {
        var a = {|result:new object()|};[||]
    }
}";
            await TestAsync<ObjectCreationExpressionSyntax>(testText);
        }

        [Fact]
        [WorkItem(35525, "https://github.com/dotnet/roslyn/issues/35525")]
        public async Task TestMissingExtractHeaderForSelection()
        {
            var testText = @"
using System;
class C
{
    class TestAttribute : Attribute { }
    [Test] public [|int|] a { get; set; }
}";
            await TestMissingAsync<PropertyDeclarationSyntax>(testText);
        }
        #endregion

        #region Extractions
        [Fact]
        [WorkItem(35525, "https://github.com/dotnet/roslyn/issues/35525")]
        public async Task TestExtractFromDeclaration()
        {
            var testText = @"
using System;
class C
{
    void M()
    {
        [|var a = {|result:new object()|};|]
    }
}";
            await TestAsync<ObjectCreationExpressionSyntax>(testText);
        }

        [Fact]
        [WorkItem(35525, "https://github.com/dotnet/roslyn/issues/35525")]
        public async Task TestExtractFromDeclaration2()
        {
            var testText = @"
using System;
class C
{
    void M()
    {
        var a = [|{|result:new object()|};|]
    }
}";
            await TestAsync<ObjectCreationExpressionSyntax>(testText);
        }

        [Fact]
        [WorkItem(35525, "https://github.com/dotnet/roslyn/issues/35525")]
        public async Task TestExtractFromAssignment()
        {
            var testText = @"
using System;
class C
{
    void M()
    {
        object a;
        a = [|{|result:new object()|};|]
    }
}";
            await TestAsync<ObjectCreationExpressionSyntax>(testText);
        }

        [Fact]
        [WorkItem(35525, "https://github.com/dotnet/roslyn/issues/35525")]
        public async Task TestExtractInHeaderOfProperty()
        {
            var testText = @"
using System;
class C
{
    class TestAttribute : Attribute { }
    {|result:[Test] public i[||]nt a { get; set; }|}
}";
            await TestAsync<PropertyDeclarationSyntax>(testText);
        }

        [Fact]
        [WorkItem(35525, "https://github.com/dotnet/roslyn/issues/35525")]
        public async Task TestMissingExtractNotInHeaderOfProperty()
        {
            var testText = @"
using System;
class C
{
    class TestAttribute : Attribute { }
    [Test] public int a { [||]get; set; }
}";
            await TestMissingAsync<PropertyDeclarationSyntax>(testText);
        }

        #endregion

        #region Test arguments
        [Fact]
        [WorkItem(35525, "https://github.com/dotnet/roslyn/issues/35525")]
        public async Task TestMissingArgumentsExtractionsInInitializer()
        {
            var testText = @"
using System;
class C
{
    class TestAttribute : Attribute { }
    public C([Test]int a = [||]42, int b = 41) {}
}";
            await TestMissingAsync<ParameterSyntax>(testText);
        }

        [Fact]
        [WorkItem(35525, "https://github.com/dotnet/roslyn/issues/35525")]
        public async Task TestMissingArgumentsExtractionsSelectInitializer()
        {
            var testText = @"
using System;
class C
{
    class TestAttribute : Attribute { }
    public C([Test]int a = [|42|], int b = 41) {}
}";
            await TestMissingAsync<ParameterSyntax>(testText);
        }

        [Fact]
        [WorkItem(35525, "https://github.com/dotnet/roslyn/issues/35525")]
        public async Task TestMissingArgumentsExtractionsSelectComma()
        {
            var testText = @"
using System;
class C
{
    class TestAttribute : Attribute { }
    public C([Test]int a = 42[|,|] int b = 41) {}
}";
            await TestMissingAsync<ParameterSyntax>(testText);
        }

        [Fact]
        [WorkItem(35525, "https://github.com/dotnet/roslyn/issues/35525")]
        public async Task TestMissingArgumentsExtractionsInAttributes()
        {
            var testText = @"
using System;
class C
{
    class TestAttribute : Attribute { }
    public C([[||]Test]int a = 42, int b = 41) {}
}";
            await TestMissingAsync<ParameterSyntax>(testText);
        }

        [Fact]
        [WorkItem(35525, "https://github.com/dotnet/roslyn/issues/35525")]
        public async Task TestMissingArgumentsExtractionsSelectType1()
        {
            var testText = @"
using System;
class C
{
    class TestAttribute : Attribute { }
    public C([Test][|int|] a = 42, int b = 41) {}
}";
            await TestMissingAsync<ParameterSyntax>(testText);
        }

        [Fact]
        [WorkItem(35525, "https://github.com/dotnet/roslyn/issues/35525")]
        public async Task TestMissingArgumentsExtractionsSelectType2()
        {
            var testText = @"
using System;
class C
{
    class TestAttribute : Attribute { }
    public C([Test][|C|] a = null, int b = 41) {}
}";
            await TestMissingAsync<ParameterSyntax>(testText);
        }

        [Fact]
        [WorkItem(35525, "https://github.com/dotnet/roslyn/issues/35525")]
        public async Task TestArgumentsExtractionsAtTheEnd()
        {
            var testText = @"
using System;
class C
{
    class TestAttribute : Attribute { }
    public C({|result:[Test]int a = 42[||]|}, int b = 41) {}
}";
            await TestAsync<ParameterSyntax>(testText);
        }

        [Fact]
        [WorkItem(35525, "https://github.com/dotnet/roslyn/issues/35525")]
        public async Task TestArgumentsExtractionsBefore()
        {
            var testText = @"
using System;
class C
{
    class TestAttribute : Attribute { }
    public C([||]{|result:[Test]int a = 42|}, int b = 41) {}
}";
            await TestAsync<ParameterSyntax>(testText);
        }

        [Fact]
        [WorkItem(35525, "https://github.com/dotnet/roslyn/issues/35525")]
        public async Task TestArgumentsExtractionsSelectParamName()
        {
            var testText = @"
using System;
class C
{
    class TestAttribute : Attribute { }
    public C({|result:[Test]int [|a|] = 42|}, int b = 41) {}
}";
            await TestAsync<ParameterSyntax>(testText);
        }

        [Fact]
        [WorkItem(35525, "https://github.com/dotnet/roslyn/issues/35525")]
        public async Task TestArgumentsExtractionsSelectParam1()
        {
            var testText = @"
using System;
class C
{
    class TestAttribute : Attribute { }
    public C({|result:[Test][|int a|] = 42|}, int b = 41) {}
}";
            await TestAsync<ParameterSyntax>(testText);
        }

        [Fact]
        [WorkItem(35525, "https://github.com/dotnet/roslyn/issues/35525")]
        public async Task TestArgumentsExtractionsSelectParam2()
        {
            var testText = @"
using System;
class C
{
    class TestAttribute : Attribute { }
    public C([|{|result:[Test]int a = 42|}|], int b = 41) {}
}";
            await TestAsync<ParameterSyntax>(testText);
        }

        [Fact]
        [WorkItem(35525, "https://github.com/dotnet/roslyn/issues/35525")]
        public async Task TestArgumentsExtractionsSelectParam3()
        {
            var testText = @"
using System;
class C
{
    class TestAttribute : Attribute { }
    public C({|result:[Test][|int a = 42|]|}, int b = 41) {}
}";
            await TestAsync<ParameterSyntax>(testText);
        }

        [Fact]
        [WorkItem(35525, "https://github.com/dotnet/roslyn/issues/35525")]
        public async Task TestArgumentsExtractionsInHeader()
        {
            var testText = @"
using System;
class CC
{
    class TestAttribute : Attribute { }
    public CC({|result:[Test]C[||]C a = 42|}, int b = 41) {}
}";
            await TestAsync<ParameterSyntax>(testText);
        }

        #endregion

        #region Test methods
        [Fact]
        [WorkItem(35525, "https://github.com/dotnet/roslyn/issues/35525")]
        public async Task TestMissingMethodExplicitInterfaceSelection()
        {
            var testText = @"
using System;
class C
{
    class TestAttribute : Attribute { }
    public void [|I|].A([Test]int a = 42, int b = 41) {}
}";
            await TestMissingAsync<MethodDeclarationSyntax>(testText);
        }

        [Fact]
        [WorkItem(35525, "https://github.com/dotnet/roslyn/issues/35525")]
        public async Task TestMethodCaretBeforeInterfaceSelection()
        {
            var testText = @"
using System;
class C
{
    class TestAttribute : Attribute { }
    {|result:public void [||]I.A([Test]int a = 42, int b = 41) {}|}
}";
            await TestAsync<MethodDeclarationSyntax>(testText);
        }

        [Fact]
        [WorkItem(35525, "https://github.com/dotnet/roslyn/issues/35525")]
        public async Task TestMethodNameAndExplicitInterfaceSelection()
        {
            var testText = @"
using System;
class C
{
    class TestAttribute : Attribute { }
    {|result:public void [|I.A|]([Test]int a = 42, int b = 41) {}|}
}";
            await TestAsync<MethodDeclarationSyntax>(testText);
        }

        [Fact]
        [WorkItem(35525, "https://github.com/dotnet/roslyn/issues/35525")]
        public async Task TestMethodInHeader()
        {
            var testText = @"
using System;
class CC
{
    class TestAttribute : Attribute { }
    {|result:public C[||]C I.A([Test]int a = 42, int b = 41) { return null; }|}
}";
            await TestAsync<MethodDeclarationSyntax>(testText);
        }

        #endregion
    }
}
