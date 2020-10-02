// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.Editor.UnitTests.SplitComment;
using Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.SplitComment
{
    [UseExportProvider]
    public class SplitCommentCommandHandlerTests : AbstractSplitCommentCommandHandlerTests
    {
        protected override TestWorkspace CreateWorkspace(string markup)
            => TestWorkspace.CreateCSharp(markup);

        [WorkItem(38516, "https://github.com/dotnet/roslyn/issues/38516")]
        [WpfFact, Trait(Traits.Feature, Traits.Features.SplitComment)]
        public void TestWithSelection()
        {
            TestHandled(
@"public class Program
{
    public static void Main(string[] args) 
    { 
        //[|Test|] Comment
    }
}",
@"public class Program
{
    public static void Main(string[] args) 
    { 
        //
        //Comment
    }
}");
        }

        [WorkItem(38516, "https://github.com/dotnet/roslyn/issues/38516")]
        [WpfFact, Trait(Traits.Feature, Traits.Features.SplitComment)]
        public void TestWithAllWhitespaceSelection()
        {
            TestHandled(
@"public class Program
{
    public static void Main(string[] args) 
    { 
        // [|  |] Test Comment
    }
}",
@"public class Program
{
    public static void Main(string[] args) 
    { 
        //
        // Test Comment
    }
}");
        }

        [WorkItem(38516, "https://github.com/dotnet/roslyn/issues/38516")]
        [WpfFact, Trait(Traits.Feature, Traits.Features.SplitComment)]
        public void TestMissingInSlashes()
        {
            TestNotHandled(
@"public class Program
{
    public static void Main(string[] args) 
    { 
        /[||]/Test Comment
    }
}");
        }

        [WorkItem(38516, "https://github.com/dotnet/roslyn/issues/38516")]
        [WpfFact, Trait(Traits.Feature, Traits.Features.SplitComment)]
        public void TestMissingAtEndOfFile()
        {
            TestNotHandled(
@"public class Program
{
    public static void Main(string[] args) 
    { 
        //Test Comment[||]");
        }

        [WorkItem(38516, "https://github.com/dotnet/roslyn/issues/38516")]
        [WpfFact, Trait(Traits.Feature, Traits.Features.SplitComment)]
        public void TestMissingBeforeSlashes()
        {
            TestNotHandled(
@"public class Program
{
    public static void Main(string[] args) 
    { 
        [||]//Test Comment
    }
}");
        }

        [WorkItem(38516, "https://github.com/dotnet/roslyn/issues/38516")]
        [WpfFact, Trait(Traits.Feature, Traits.Features.SplitComment)]
        public void TestMissingWithMultiSelection()
        {
            TestNotHandled(
@"public class Program
{
    public static void Main(string[] args) 
    { 
        //[||]Test[||] Comment
    }
}");
        }

        [WorkItem(38516, "https://github.com/dotnet/roslyn/issues/38516")]
        [WpfFact, Trait(Traits.Feature, Traits.Features.SplitComment)]
        public void TestSplitStartOfComment()
        {
            TestHandled(
@"public class Program
{
    public static void Main(string[] args) 
    { 
        //[||]Test Comment
    }
}",
@"public class Program
{
    public static void Main(string[] args) 
    { 
        //
        //Test Comment
    }
}");
        }

        [WorkItem(38516, "https://github.com/dotnet/roslyn/issues/38516")]
        [WpfFact, Trait(Traits.Feature, Traits.Features.SplitComment)]
        public void TestSplitStartOfQuadComment()
        {
            TestHandled(
@"public class Program
{
    public static void Main(string[] args) 
    { 
        ////[||]Test Comment
    }
}",
@"public class Program
{
    public static void Main(string[] args) 
    { 
        ////
        ////Test Comment
    }
}");
        }

        [WorkItem(38516, "https://github.com/dotnet/roslyn/issues/38516")]
        [WpfFact, Trait(Traits.Feature, Traits.Features.SplitComment)]
        public void TestSplitMiddleOfQuadComment()
        {
            TestHandled(
@"public class Program
{
    public static void Main(string[] args) 
    { 
        //[||]//Test Comment
    }
}",
@"public class Program
{
    public static void Main(string[] args) 
    { 
        //
        ////Test Comment
    }
}");
        }

        [WorkItem(38516, "https://github.com/dotnet/roslyn/issues/38516")]
        [WpfFact, Trait(Traits.Feature, Traits.Features.SplitComment)]
        public void TestSplitStartOfCommentWithLeadingSpace1()
        {
            TestHandled(
@"public class Program
{
    public static void Main(string[] args) 
    { 
        // [||]Test Comment
    }
}",
@"public class Program
{
    public static void Main(string[] args) 
    { 
        //
        // Test Comment
    }
}");
        }

        [WorkItem(38516, "https://github.com/dotnet/roslyn/issues/38516")]
        [WpfFact, Trait(Traits.Feature, Traits.Features.SplitComment)]
        public void TestSplitStartOfCommentWithLeadingSpace2()
        {
            TestHandled(
@"public class Program
{
    public static void Main(string[] args) 
    { 
        //[||] Test Comment
    }
}",
@"public class Program
{
    public static void Main(string[] args) 
    { 
        //
        //Test Comment
    }
}");
        }

        [WorkItem(38516, "https://github.com/dotnet/roslyn/issues/38516")]
        [WpfTheory, Trait(Traits.Feature, Traits.Features.SplitComment)]
        [InlineData("X[||]Test Comment")]
        [InlineData("X [||]Test Comment")]
        [InlineData("X[||] Test Comment")]
        [InlineData("X [||] Test Comment")]
        public void TestCommentWithMultipleLeadingSpaces(string commentValue)
        {
            TestHandled(
@$"public class Program
{{
    public static void Main(string[] args) 
    {{ 
        //    {commentValue}
    }}
}}",
@"public class Program
{
    public static void Main(string[] args) 
    { 
        //    X
        //    Test Comment
    }
}");
        }

        [WorkItem(38516, "https://github.com/dotnet/roslyn/issues/38516")]
        [WpfTheory, Trait(Traits.Feature, Traits.Features.SplitComment)]
        [InlineData("X[||]Test Comment")]
        [InlineData("X [||]Test Comment")]
        [InlineData("X[||] Test Comment")]
        [InlineData("X [||] Test Comment")]
        [InlineData("X[| |]Test Comment")]
        public void TestQuadCommentWithMultipleLeadingSpaces(string commentValue)
        {
            TestHandled(
@$"public class Program
{{
    public static void Main(string[] args) 
    {{ 
        ////    {commentValue}
    }}
}}",
@"public class Program
{
    public static void Main(string[] args) 
    { 
        ////    X
        ////    Test Comment
    }
}");
        }

        [WorkItem(38516, "https://github.com/dotnet/roslyn/issues/38516")]
        [WpfFact, Trait(Traits.Feature, Traits.Features.SplitComment)]
        public void TestSplitMiddleOfComment()
        {
            TestHandled(
@"public class Program
{
    public static void Main(string[] args) 
    { 
        // Test [||]Comment
    }
}",
@"public class Program
{
    public static void Main(string[] args) 
    { 
        // Test
        // Comment
    }
}");
        }

        [WorkItem(38516, "https://github.com/dotnet/roslyn/issues/38516")]
        [WpfFact, Trait(Traits.Feature, Traits.Features.SplitComment)]
        public void TestSplitEndOfComment()
        {
            TestNotHandled(
@"public class Program
{
    public static void Main(string[] args) 
    { 
        // Test Comment[||]
    }
}");
        }

        [WorkItem(38516, "https://github.com/dotnet/roslyn/issues/38516")]
        [WpfFact, Trait(Traits.Feature, Traits.Features.SplitComment)]
        public void TestSplitCommentEndOfLine1()
        {
            TestHandled(
@"public class Program
{
    public static void Main(string[] args) // Test [||]Comment
    {
    }
}",
@"public class Program
{
    public static void Main(string[] args) // Test
                                           // Comment
    {
    }
}");
        }

        [WorkItem(38516, "https://github.com/dotnet/roslyn/issues/38516")]
        [WpfFact, Trait(Traits.Feature, Traits.Features.SplitComment)]
        public void TestSplitCommentEndOfLine2()
        {
            TestHandled(
@"public class Program
{
    public static void Main(string[] args) // Test[||] Comment
    {
    }
}",
@"public class Program
{
    public static void Main(string[] args) // Test
                                           // Comment
    {
    }
}");
        }

        [WorkItem(38516, "https://github.com/dotnet/roslyn/issues/38516")]
        [WpfFact, Trait(Traits.Feature, Traits.Features.SplitComment)]
        public void TestUseTabs()
        {
            TestHandled(
@"public class Program
{
	public static void Main(string[] args) 
	{
		// X[||]Test Comment
	}
}",
@"public class Program
{
	public static void Main(string[] args) 
	{
		// X
		// Test Comment
	}
}", useTabs: true);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.SplitComment)]
        public void TestDoesNotHandleDocComments()
        {
            TestNotHandled(
@"namespace TestNamespace
{
    public class Program
    {
        /// <summary>Test [||]Comment</summary>
        public static void Main(string[] args)
        {
        }
    }
}");
        }
    }
}
