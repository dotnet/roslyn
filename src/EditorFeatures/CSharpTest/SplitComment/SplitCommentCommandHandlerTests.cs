// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.Editor.UnitTests.SplitComment;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.SplitComment
{
    [UseExportProvider]
    public class SplitCommentCommandHandlerTests : AbstractSplitCommentCommandHandlerTests
    {
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
        // Test Comment
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
        public void TestSplitCommentOutsideOfMethod()
        {
            TestHandled(
@"public class Program
{
    public static void Main(string[] args) 
    { 
        
    }
    // Test [||]Comment
}",
@"public class Program
{
    public static void Main(string[] args) 
    { 
        
    }
    // Test
    // Comment
}");
        }

        [WorkItem(38516, "https://github.com/dotnet/roslyn/issues/38516")]
        [WpfFact, Trait(Traits.Feature, Traits.Features.SplitComment)]
        public void TestSplitCommentOutsideOfClass()
        {
            TestHandled(
@"public class Program
{
    public static void Main(string[] args) 
    { 
        
    }
}
// Test [||]Comment
",
@"public class Program
{
    public static void Main(string[] args) 
    { 
        
    }
}
// Test
// Comment
");
        }

        [WorkItem(38516, "https://github.com/dotnet/roslyn/issues/38516")]
        [WpfFact, Trait(Traits.Feature, Traits.Features.SplitComment)]
        public void TestSplitCommentOutsideOfNamespace()
        {
            TestHandled(
@"namespace TestNamespace
{
    public class Program
    {
        public static void Main(string[] args)
        {
            
        }
    }
}
// Test [||]Comment
",
@"namespace TestNamespace
{
    public class Program
    {
        public static void Main(string[] args)
        {
            
        }
    }
}
// Test
// Comment
");
        }
    }
}
