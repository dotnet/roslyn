// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Roslyn.Diagnostics.CSharp.Analyzers.BracePlacement;
using Test.Utilities;
using Xunit;

namespace Roslyn.Diagnostics.Analyzers.UnitTests.BracePlacement
{
    using VerifyCS = CSharpCodeFixVerifier<
        CSharpBracePlacementDiagnosticAnalyzer,
        CSharpBracePlacementCodeFixProvider>;

    public class BracePlacementTests
    {
        [Fact]
        public async Task NotForBracesOnSameLineDirectlyTouching()
        {
            var code =
@"class C { void M() { }}";

            await new VerifyCS.Test
            {
                TestCode = code,
                FixedCode = code
            }.RunAsync();
        }

        [Fact]
        public async Task NotForBracesOnSameLineWithSpace()
        {
            var code =
@"class C { void M() { } }";

            await new VerifyCS.Test
            {
                TestCode = code,
                FixedCode = code
            }.RunAsync();
        }

        [Fact]
        public async Task NotForBracesOnSameLineWithComment()
        {
            var code =
@"class C { void M() { }/*goo*/}";

            await new VerifyCS.Test
            {
                TestCode = code,
                FixedCode = code
            }.RunAsync();
        }

        [Fact]
        public async Task NotForBracesOnSameLineWithCommentAndSpaces()
        {
            var code =
@"class C { void M() { } /*goo*/ }";

            await new VerifyCS.Test
            {
                TestCode = code,
                FixedCode = code
            }.RunAsync();
        }

        [Fact]
        public async Task NotForBracesOnSubsequentLines_TopLevel()
        {
            var code =
@"class C
{
    void M()
    {
    }
}";

            await new VerifyCS.Test
            {
                TestCode = code,
                FixedCode = code
            }.RunAsync();
        }

        [Fact]
        public async Task NotForBracesOnSubsequentLinesWithComment1_TopLevel()
        {
            var code =
@"class C
{
    void M()
    {
    } // comment
}";

            await new VerifyCS.Test
            {
                TestCode = code,
                FixedCode = code
            }.RunAsync();
        }

        [Fact]
        public async Task NotForBracesOnSubsequentLinesWithComment2_TopLevel()
        {
            var code =
@"class C
{
    void M()
    {
    } /* comment */
}";

            await new VerifyCS.Test
            {
                TestCode = code,
                FixedCode = code
            }.RunAsync();
        }

        [Fact]
        public async Task NotForBracesOnSubsequentLinesIndented()
        {
            var code =
@"namespace N
{
    class C
    {
        void M()
        {
        }
    }
}";

            await new VerifyCS.Test
            {
                TestCode = code,
                FixedCode = code
            }.RunAsync();
        }

        [Fact]
        public async Task NotForBracesOnSubsequentLinesIndentedWithComment1()
        {
            var code =
@"namespace N
{
    class C
    {
        void M()
        {
        } // comment
    }
}";

            await new VerifyCS.Test
            {
                TestCode = code,
                FixedCode = code
            }.RunAsync();
        }

        [Fact]
        public async Task NotForBracesOnSubsequentLinesIndentedWithComment2()
        {
            var code =
@"namespace N
{
    class C
    {
        void M()
        {
        } /* comment */
    }
}";

            await new VerifyCS.Test
            {
                TestCode = code,
                FixedCode = code
            }.RunAsync();
        }

        [Fact]
        public async Task NotForBracesWithBlankLinesIfCommentBetween1_TopLevel()
        {
            var code =
@"class C
{
    void M()
    {
    }

    // comment

}";

            await new VerifyCS.Test
            {
                TestCode = code,
                FixedCode = code
            }.RunAsync();
        }

        [Fact]
        public async Task NotForBracesWithBlankLinesIfCommentBetween2_TopLevel()
        {
            var code =
@"class C
{
    void M()
    {
    }

    /* comment */

}";

            await new VerifyCS.Test
            {
                TestCode = code,
                FixedCode = code
            }.RunAsync();
        }

        [Fact]
        public async Task NotForBracesWithBlankLinesIfDirectiveBetween1_TopLeve()
        {
            var code =
@"class C
{
    void M()
    {
    }

    #nullable enable

}";

            await new VerifyCS.Test
            {
                TestCode = code,
                FixedCode = code,
                LanguageVersion = Microsoft.CodeAnalysis.CSharp.LanguageVersion.CSharp8,
            }.RunAsync();
        }

        [Fact]
        public async Task NotForBracesWithBlankLinesIfCommentBetween1_Nested()
        {
            var code =
@"namespace N
{
    class C
    {
        void M()
        {
        }

        // comment

    }
}";

            await new VerifyCS.Test
            {
                TestCode = code,
                FixedCode = code
            }.RunAsync();
        }

        [Fact]
        public async Task NotForBracesWithBlankLinesIfCommentBetween2_Nested()
        {
            var code =
@"namespace N
{
    class C
    {
        void M()
        {
        }

        /* comment */

    }
}";

            await new VerifyCS.Test
            {
                TestCode = code,
                FixedCode = code
            }.RunAsync();
        }

        [Fact]
        public async Task NotForBracesWithBlankLinesIfDirectiveBetween_Nested()
        {
            var code =
@"namespace N
{
    class C
    {
        void M()
        {
        }

        #nullable enable

    }
}";

            await new VerifyCS.Test
            {
                TestCode = code,
                FixedCode = code,
                LanguageVersion = Microsoft.CodeAnalysis.CSharp.LanguageVersion.CSharp8,
            }.RunAsync();
        }

        [Fact]
        public async Task OneBlankLineBetweenBraces_TopLevel()
        {
            var code =
@"class C
{
    void M()
    {
    }

[|}|]";
            var fixedCode =
@"class C
{
    void M()
    {
    }
}";

            await new VerifyCS.Test
            {
                TestCode = code,
                FixedCode = fixedCode
            }.RunAsync();
        }

        [Fact]
        public async Task TwoBlankLinesBetweenBraces_TopLevel()
        {
            var code =
@"class C
{
    void M()
    {
    }


[|}|]";
            var fixedCode =
@"class C
{
    void M()
    {
    }
}";

            await new VerifyCS.Test
            {
                TestCode = code,
                FixedCode = fixedCode
            }.RunAsync();
        }

        [Fact]
        public async Task ThreeBlankLinesBetweenBraces_TopLevel()
        {
            var code =
@"class C
{
    void M()
    {
    }



[|}|]";
            var fixedCode =
@"class C
{
    void M()
    {
    }
}";

            await new VerifyCS.Test
            {
                TestCode = code,
                FixedCode = fixedCode
            }.RunAsync();
        }

        [Fact]
        public async Task BlankLinesBetweenBraces_LeadingComment_TopLevel()
        {
            var code =
@"class C
{
    void M()
    {
    }



/*comment*/[|}|]";
            var fixedCode =
@"class C
{
    void M()
    {
    }
/*comment*/}";

            await new VerifyCS.Test
            {
                TestCode = code,
                FixedCode = fixedCode
            }.RunAsync();
        }

        [Fact]
        public async Task BlankLinesBetweenBraces_TrailingComment_TopLevel()
        {
            var code =
@"class C
{
    void M()
    {
    } /*comment*/



[|}|]";
            var fixedCode =
@"class C
{
    void M()
    {
    } /*comment*/
}";

            await new VerifyCS.Test
            {
                TestCode = code,
                FixedCode = fixedCode
            }.RunAsync();
        }

        [Fact]
        public async Task OneBlankLineBetweenBraces_Nested()
        {
            var code =
@"namespace N
{
    class C
    {
        void M()
        {
        }

    [|}|]
}";
            var fixedCode =
@"namespace N
{
    class C
    {
        void M()
        {
        }
    }
}";

            await new VerifyCS.Test
            {
                TestCode = code,
                FixedCode = fixedCode
            }.RunAsync();
        }

        [Fact]
        public async Task TwoBlankLinesBetweenBraces_Nested()
        {
            var code =
@"namespace N
{
    class C
    {
        void M()
        {
        }


    [|}|]
}";
            var fixedCode =
@"namespace N
{
    class C
    {
        void M()
        {
        }
    }
}";

            await new VerifyCS.Test
            {
                TestCode = code,
                FixedCode = fixedCode
            }.RunAsync();
        }

        [Fact]
        public async Task ThreeBlankLinesBetweenBraces_Nested()
        {
            var code =
@"namespace N
{
    class C
    {
        void M()
        {
        }



    [|}|]
}";
            var fixedCode =
@"namespace N
{
    class C
    {
        void M()
        {
        }
    }
}";

            await new VerifyCS.Test
            {
                TestCode = code,
                FixedCode = fixedCode
            }.RunAsync();
        }

        [Fact]
        public async Task BlankLinesBetweenBraces_LeadingComment_Nested()
        {
            var code =
@"namespace N
{
    class C
    {
        void M()
        {
        }



    /*comment*/[|}|]
}";
            var fixedCode =
@"namespace N
{
    class C
    {
        void M()
        {
        }
    /*comment*/}
}";

            await new VerifyCS.Test
            {
                TestCode = code,
                FixedCode = fixedCode
            }.RunAsync();
        }

        [Fact]
        public async Task BlankLinesBetweenBraces_TrailingComment_Nested()
        {
            var code =
@"namespace N
{
    class C
    {
        void M()
        {
        } /*comment*/



    [|}|]
}";
            var fixedCode =
@"namespace N
{
    class C
    {
        void M()
        {
        } /*comment*/
    }
}";

            await new VerifyCS.Test
            {
                TestCode = code,
                FixedCode = fixedCode
            }.RunAsync();
        }

        [Fact]
        public async Task FixAll1()
        {
            var code =
@"namespace N
{
    class C
    {
        void M()
        {
        }

    [|}|]

[|}|]";
            var fixedCode =
@"namespace N
{
    class C
    {
        void M()
        {
        }
    }
}";

            await new VerifyCS.Test
            {
                TestCode = code,
                FixedCode = fixedCode
            }.RunAsync();
        }

        [Fact]
        public async Task RealCode1()
        {
            var code =
@"
#nullable enable

using System;

#if CODE_STYLE
using System.Collections.Generic;
#endif

namespace Microsoft.CodeAnalysis.Options
{
    internal interface IOption { }

    internal interface IOption2
#if !CODE_STYLE
    : IOption
#endif
    {
        string OptionDefinition { get; }

#if CODE_STYLE
        string Feature { get; }
        string Name { get; }
        Type Type { get; }
        object? DefaultValue { get; }
        bool IsPerLanguage { get; }

        List<string> StorageLocations { get; }
#endif
    }
}
";

            await new VerifyCS.Test
            {
                TestCode = code,
                FixedCode = code,
                LanguageVersion = Microsoft.CodeAnalysis.CSharp.LanguageVersion.CSharp8,
            }.RunAsync();
        }

        [Fact]
        public async Task RealCode2()
        {
            var code =
@"
#define CODE_STYLE
#nullable enable

using System;

#if CODE_STYLE
using System.Collections.Generic;
#endif

namespace Microsoft.CodeAnalysis.Options
{
    internal interface IOption { }

    internal interface IOption2
#if !CODE_STYLE
    : IOption
#endif
    {
        string OptionDefinition { get; }

#if CODE_STYLE
        string Feature { get; }
        string Name { get; }
        Type Type { get; }
        object? DefaultValue { get; }
        bool IsPerLanguage { get; }

        List<string> StorageLocations { get; }
#endif
    }
}
";

            await new VerifyCS.Test
            {
                TestCode = code,
                FixedCode = code,
                LanguageVersion = Microsoft.CodeAnalysis.CSharp.LanguageVersion.CSharp8,
            }.RunAsync();
        }
    }
}
