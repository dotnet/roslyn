// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.NewLines.MultipleBlankLines;
using Microsoft.CodeAnalysis.Editor.UnitTests.CodeActions;
using Microsoft.CodeAnalysis.NewLines.MultipleBlankLines;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.NewLines.MultipleBlankLines
{
    using Verify = CSharpCodeFixVerifier<
        CSharpMultipleBlankLinesDiagnosticAnalyzer,
        MultipleBlankLinesCodeFixProvider>;

    public class MultipleBlankLinesTests
    {
        [Fact]
        public async Task TestOneBlankLineAtTopOfFile()
        {
            var code =
@"
// comment";

            await new Verify.Test
            {
                TestCode = code,
                FixedCode = code,
                Options = { { CodeStyleOptions2.AllowMultipleBlankLines, CodeStyleOption2.FalseWithSuggestionEnforcement } }
            }.RunAsync();
        }

        [Fact]
        public async Task TestTwoBlankLineAtTopOfFile()
        {
            var code =
@"[||]

// comment";
            var fixedCode =
@"
// comment";

            await new Verify.Test
            {
                TestCode = code,
                FixedCode = fixedCode,
                Options = { { CodeStyleOptions2.AllowMultipleBlankLines, CodeStyleOption2.FalseWithSuggestionEnforcement } }
            }.RunAsync();
        }

        [Fact]
        public async Task TestTwoBlankLineAtTopOfFile_NotWithOptionOff()
        {
            var code =
@"

// comment";

            await new Verify.Test
            {
                TestCode = code,
                FixedCode = code,
                Options = { { CodeStyleOptions2.AllowMultipleBlankLines, CodeStyleOption2.TrueWithSuggestionEnforcement } }
            }.RunAsync();
        }

        [Fact]
        public async Task TestThreeBlankLineAtTopOfFile()
        {
            var code =
@"[||]


// comment";
            var fixedCode =
@"
// comment";

            await new Verify.Test
            {
                TestCode = code,
                FixedCode = fixedCode,
                Options = { { CodeStyleOptions2.AllowMultipleBlankLines, CodeStyleOption2.FalseWithSuggestionEnforcement } }
            }.RunAsync();
        }

        [Fact]
        public async Task TestFourBlankLineAtTopOfFile()
        {
            var code =
@"[||]



// comment";
            var fixedCode =
@"
// comment";

            await new Verify.Test
            {
                TestCode = code,
                FixedCode = fixedCode,
                Options = { { CodeStyleOptions2.AllowMultipleBlankLines, CodeStyleOption2.FalseWithSuggestionEnforcement } }
            }.RunAsync();
        }

        [Fact]
        public async Task TestOneBlankLineAtTopOfEmptyFile()
        {
            var code =
@"
";

            await new Verify.Test
            {
                TestCode = code,
                FixedCode = code,
                Options = { { CodeStyleOptions2.AllowMultipleBlankLines, CodeStyleOption2.FalseWithSuggestionEnforcement } }
            }.RunAsync();
        }

        [Fact]
        public async Task TestTwoBlankLinesAtTopOfEmptyFile()
        {
            var code =
@"[||]

";
            var fixedCode =
@"
";

            await new Verify.Test
            {
                TestCode = code,
                FixedCode = fixedCode,
                Options = { { CodeStyleOptions2.AllowMultipleBlankLines, CodeStyleOption2.FalseWithSuggestionEnforcement } }
            }.RunAsync();
        }

        [Fact]
        public async Task TestThreeBlankLinesAtTopOfEmptyFile()
        {
            var code =
@"[||]


";
            var fixedCode =
@"
";

            await new Verify.Test
            {
                TestCode = code,
                FixedCode = fixedCode,
                Options = { { CodeStyleOptions2.AllowMultipleBlankLines, CodeStyleOption2.FalseWithSuggestionEnforcement } }
            }.RunAsync();
        }

        [Fact]
        public async Task TestFourBlankLinesAtTopOfEmptyFile()
        {
            var code =
@"[||]



";
            var fixedCode =
@"
";

            await new Verify.Test
            {
                TestCode = code,
                FixedCode = fixedCode,
                Options = { { CodeStyleOptions2.AllowMultipleBlankLines, CodeStyleOption2.FalseWithSuggestionEnforcement } }
            }.RunAsync();
        }

        [Fact]
        public async Task TestNoBlankLineAtEndOfFile_1()
        {
            var code =
@"class C
{
}";

            await new Verify.Test
            {
                TestCode = code,
                FixedCode = code,
                Options = { { CodeStyleOptions2.AllowMultipleBlankLines, CodeStyleOption2.FalseWithSuggestionEnforcement } }
            }.RunAsync();
        }

        [Fact]
        public async Task TestNoBlankLineAtEndOfFile_2()
        {
            var code =
@"class C
{
}
";

            await new Verify.Test
            {
                TestCode = code,
                FixedCode = code,
                Options = { { CodeStyleOptions2.AllowMultipleBlankLines, CodeStyleOption2.FalseWithSuggestionEnforcement } }
            }.RunAsync();
        }

        [Fact]
        public async Task TestOneBlankLineAtEndOfFile()
        {
            var code =
@"class C
{
}

";

            await new Verify.Test
            {
                TestCode = code,
                FixedCode = code,
                Options = { { CodeStyleOptions2.AllowMultipleBlankLines, CodeStyleOption2.FalseWithSuggestionEnforcement } }
            }.RunAsync();
        }

        [Fact]
        public async Task TestTwoBlankLineAtEndOfFile()
        {
            var code =
@"class C
{
}
[||]

";
            var fixedCode =
@"class C
{
}

";

            await new Verify.Test
            {
                TestCode = code,
                FixedCode = fixedCode,
                Options = { { CodeStyleOptions2.AllowMultipleBlankLines, CodeStyleOption2.FalseWithSuggestionEnforcement } }
            }.RunAsync();
        }

        [Fact]
        public async Task TestThreeBlankLineAtEndOfFile()
        {
            var code =
@"class C
{
}
[||]


";
            var fixedCode =
@"class C
{
}

";

            await new Verify.Test
            {
                TestCode = code,
                FixedCode = fixedCode,
                Options = { { CodeStyleOptions2.AllowMultipleBlankLines, CodeStyleOption2.FalseWithSuggestionEnforcement } }
            }.RunAsync();
        }

        [Fact]
        public async Task TestFourBlankLineAtEndOfFile()
        {
            var code =
@"class C
{
}
[||]



";
            var fixedCode =
@"class C
{
}

";

            await new Verify.Test
            {
                TestCode = code,
                FixedCode = fixedCode,
                Options = { { CodeStyleOptions2.AllowMultipleBlankLines, CodeStyleOption2.FalseWithSuggestionEnforcement } }
            }.RunAsync();
        }

        [Fact]
        public async Task TestNoBlankLineBetweenTokens()
        {
            var code =
@"class C
{
}";

            await new Verify.Test
            {
                TestCode = code,
                FixedCode = code,
                Options = { { CodeStyleOptions2.AllowMultipleBlankLines, CodeStyleOption2.FalseWithSuggestionEnforcement } }
            }.RunAsync();
        }

        [Fact]
        public async Task TestOneBlankLineBetweenTokens()
        {
            var code =
@"class C
{

}";

            await new Verify.Test
            {
                TestCode = code,
                FixedCode = code,
                Options = { { CodeStyleOptions2.AllowMultipleBlankLines, CodeStyleOption2.FalseWithSuggestionEnforcement } }
            }.RunAsync();
        }

        [Fact]
        public async Task TestTwoBlankLineBetweenTokens()
        {
            var code =
@"class C
{
[||]

}";
            var fixedCode =
@"class C
{

}";

            await new Verify.Test
            {
                TestCode = code,
                FixedCode = fixedCode,
                Options = { { CodeStyleOptions2.AllowMultipleBlankLines, CodeStyleOption2.FalseWithSuggestionEnforcement } }
            }.RunAsync();
        }

        [Fact]
        public async Task TestThreeBlankLineBetweenTokens()
        {
            var code =
@"class C
{
[||]


}";
            var fixedCode =
@"class C
{

}";

            await new Verify.Test
            {
                TestCode = code,
                FixedCode = fixedCode,
                Options = { { CodeStyleOptions2.AllowMultipleBlankLines, CodeStyleOption2.FalseWithSuggestionEnforcement } }
            }.RunAsync();
        }

        [Fact]
        public async Task TestFourBlankLineBetweenTokens()
        {
            var code =
@"class C
{
[||]



}";
            var fixedCode =
@"class C
{

}";

            await new Verify.Test
            {
                TestCode = code,
                FixedCode = fixedCode,
                Options = { { CodeStyleOptions2.AllowMultipleBlankLines, CodeStyleOption2.FalseWithSuggestionEnforcement } }
            }.RunAsync();
        }

        [Fact]
        public async Task TestNoBlankLineAfterComment()
        {
            var code =
@"class C
{
    // comment
}";

            await new Verify.Test
            {
                TestCode = code,
                FixedCode = code,
                Options = { { CodeStyleOptions2.AllowMultipleBlankLines, CodeStyleOption2.FalseWithSuggestionEnforcement } }
            }.RunAsync();
        }

        [Fact]
        public async Task TestOneBlankLineAfterComment()
        {
            var code =
@"class C
{
    // comment

}";

            await new Verify.Test
            {
                TestCode = code,
                FixedCode = code,
                Options = { { CodeStyleOptions2.AllowMultipleBlankLines, CodeStyleOption2.FalseWithSuggestionEnforcement } }
            }.RunAsync();
        }

        [Fact]
        public async Task TestTwoBlankLineAfterComment()
        {
            var code =
@"class C
{
    // comment
[||]

}";
            var fixedCode =
@"class C
{
    // comment

}";

            await new Verify.Test
            {
                TestCode = code,
                FixedCode = fixedCode,
                Options = { { CodeStyleOptions2.AllowMultipleBlankLines, CodeStyleOption2.FalseWithSuggestionEnforcement } }
            }.RunAsync();
        }

        [Fact]
        public async Task TestThreeBlankLineAfterComment()
        {
            var code =
@"class C
{
    // comment
[||]


}";
            var fixedCode =
@"class C
{
    // comment

}";

            await new Verify.Test
            {
                TestCode = code,
                FixedCode = fixedCode,
                Options = { { CodeStyleOptions2.AllowMultipleBlankLines, CodeStyleOption2.FalseWithSuggestionEnforcement } }
            }.RunAsync();
        }

        [Fact]
        public async Task TestFourBlankLineAfterComment()
        {
            var code =
@"class C
{
    // comment
[||]


}";
            var fixedCode =
@"class C
{
    // comment

}";

            await new Verify.Test
            {
                TestCode = code,
                FixedCode = fixedCode,
                Options = { { CodeStyleOptions2.AllowMultipleBlankLines, CodeStyleOption2.FalseWithSuggestionEnforcement } }
            }.RunAsync();
        }

        [Fact]
        public async Task TestNoBlankLineAfterDirective()
        {
            var code =
@"class C
{
    #nullable enable
}";

            await new Verify.Test
            {
                TestCode = code,
                FixedCode = code,
                LanguageVersion = Microsoft.CodeAnalysis.CSharp.LanguageVersion.CSharp8,
                Options = { { CodeStyleOptions2.AllowMultipleBlankLines, CodeStyleOption2.FalseWithSuggestionEnforcement } }
            }.RunAsync();
        }

        [Fact]
        public async Task TestOneBlankLineAfterDirective()
        {
            var code =
@"class C
{
    #nullable enable

}";

            await new Verify.Test
            {
                TestCode = code,
                FixedCode = code,
                LanguageVersion = Microsoft.CodeAnalysis.CSharp.LanguageVersion.CSharp8,
                Options = { { CodeStyleOptions2.AllowMultipleBlankLines, CodeStyleOption2.FalseWithSuggestionEnforcement } }
            }.RunAsync();
        }

        [Fact]
        public async Task TestTwoBlankLineAfterDirective()
        {
            var code =
@"class C
{
    #nullable enable
[||]

}";
            var fixedCode =
@"class C
{
    #nullable enable

}";

            await new Verify.Test
            {
                TestCode = code,
                FixedCode = fixedCode,
                LanguageVersion = Microsoft.CodeAnalysis.CSharp.LanguageVersion.CSharp8,
                Options = { { CodeStyleOptions2.AllowMultipleBlankLines, CodeStyleOption2.FalseWithSuggestionEnforcement } }
            }.RunAsync();
        }

        [Fact]
        public async Task TestThreeBlankLineAfterDirective()
        {
            var code =
@"class C
{
    #nullable enable
[||]


}";
            var fixedCode =
@"class C
{
    #nullable enable

}";

            await new Verify.Test
            {
                TestCode = code,
                FixedCode = fixedCode,
                LanguageVersion = Microsoft.CodeAnalysis.CSharp.LanguageVersion.CSharp8,
                Options = { { CodeStyleOptions2.AllowMultipleBlankLines, CodeStyleOption2.FalseWithSuggestionEnforcement } }
            }.RunAsync();
        }

        [Fact]
        public async Task TestFourBlankLineAfterDirective()
        {
            var code =
@"class C
{
    #nullable enable
[||]


}";
            var fixedCode =
@"class C
{
    #nullable enable

}";

            await new Verify.Test
            {
                TestCode = code,
                FixedCode = fixedCode,
                LanguageVersion = Microsoft.CodeAnalysis.CSharp.LanguageVersion.CSharp8,
                Options = { { CodeStyleOptions2.AllowMultipleBlankLines, CodeStyleOption2.FalseWithSuggestionEnforcement } }
            }.RunAsync();
        }

        [Fact]
        public async Task TestNoBlankLineAfterDocComment()
        {
            var code =
@"
/// <summary/>
class C
{
}";

            await new Verify.Test
            {
                TestCode = code,
                FixedCode = code,
                Options = { { CodeStyleOptions2.AllowMultipleBlankLines, CodeStyleOption2.FalseWithSuggestionEnforcement } }
            }.RunAsync();
        }

        [Fact]
        public async Task TestOneBlankLineAfterDocComment()
        {
            var code =
@"
/// <summary/>

class C
{
}";

            await new Verify.Test
            {
                TestCode = code,
                FixedCode = code,
                Options = { { CodeStyleOptions2.AllowMultipleBlankLines, CodeStyleOption2.FalseWithSuggestionEnforcement } }
            }.RunAsync();
        }

        [Fact]
        public async Task TestTwoBlankLineAfterDocComment()
        {
            var code =
@"
/// <summary/>
[||]

class C
{
}";
            var fixedCode =
@"
/// <summary/>

class C
{
}";

            await new Verify.Test
            {
                TestCode = code,
                FixedCode = fixedCode,
                Options = { { CodeStyleOptions2.AllowMultipleBlankLines, CodeStyleOption2.FalseWithSuggestionEnforcement } }
            }.RunAsync();
        }

        [Fact]
        public async Task TestThreeBlankLineAfterDocComment()
        {
            var code =
@"
/// <summary/>
[||]


class C
{
}";
            var fixedCode =
@"
/// <summary/>

class C
{
}";

            await new Verify.Test
            {
                TestCode = code,
                FixedCode = fixedCode,
                Options = { { CodeStyleOptions2.AllowMultipleBlankLines, CodeStyleOption2.FalseWithSuggestionEnforcement } }
            }.RunAsync();
        }

        [Fact]
        public async Task TestFourBlankLineAfterDocComment()
        {
            var code =
@"
/// <summary/>
[||]



class C
{
}";
            var fixedCode =
@"
/// <summary/>

class C
{
}";

            await new Verify.Test
            {
                TestCode = code,
                FixedCode = fixedCode,
                Options = { { CodeStyleOptions2.AllowMultipleBlankLines, CodeStyleOption2.FalseWithSuggestionEnforcement } }
            }.RunAsync();
        }

        [Fact]
        public async Task TestNoBlankLineAllConstructs()
        {
            var code =
@"/// <summary/>
//
#nullable enable
class C
{
}";

            await new Verify.Test
            {
                TestCode = code,
                FixedCode = code,
                LanguageVersion = Microsoft.CodeAnalysis.CSharp.LanguageVersion.CSharp8,
                Options = { { CodeStyleOptions2.AllowMultipleBlankLines, CodeStyleOption2.FalseWithSuggestionEnforcement } }
            }.RunAsync();
        }

        [Fact]
        public async Task TestOneBlankLineAllConstructs()
        {
            var code =
@"
/// <summary/>

//

#nullable enable

class C
{
}";

            await new Verify.Test
            {
                TestCode = code,
                FixedCode = code,
                LanguageVersion = Microsoft.CodeAnalysis.CSharp.LanguageVersion.CSharp8,
                Options = { { CodeStyleOptions2.AllowMultipleBlankLines, CodeStyleOption2.FalseWithSuggestionEnforcement } }
            }.RunAsync();
        }

        [Fact]
        public async Task TestTwoBlankLineAllConstructs()
        {
            var code =
@"[||]

/// <summary/>


//


#nullable enable


class C
{
}";
            var fixedCode =
@"
/// <summary/>

//

#nullable enable

class C
{
}";

            await new Verify.Test
            {
                TestCode = code,
                FixedCode = fixedCode,
                LanguageVersion = Microsoft.CodeAnalysis.CSharp.LanguageVersion.CSharp8,
                Options = { { CodeStyleOptions2.AllowMultipleBlankLines, CodeStyleOption2.FalseWithSuggestionEnforcement } }
            }.RunAsync();
        }

        [Fact]
        public async Task TestThreeBlankLineAllConstructs()
        {
            var code =
@"[||]


/// <summary/>



//



#nullable enable



class C
{
}";
            var fixedCode =
@"
/// <summary/>

//

#nullable enable

class C
{
}";

            await new Verify.Test
            {
                TestCode = code,
                FixedCode = fixedCode,
                LanguageVersion = Microsoft.CodeAnalysis.CSharp.LanguageVersion.CSharp8,
                Options = { { CodeStyleOptions2.AllowMultipleBlankLines, CodeStyleOption2.FalseWithSuggestionEnforcement } }
            }.RunAsync();
        }

        [Fact]
        public async Task TestFourBlankLineAllConstructs()
        {
            var code =
@"[||]



/// <summary/>




//




#nullable enable




class C
{
}";
            var fixedCode =
@"
/// <summary/>

//

#nullable enable

class C
{
}";

            await new Verify.Test
            {
                TestCode = code,
                FixedCode = fixedCode,
                LanguageVersion = Microsoft.CodeAnalysis.CSharp.LanguageVersion.CSharp8,
                Options = { { CodeStyleOptions2.AllowMultipleBlankLines, CodeStyleOption2.FalseWithSuggestionEnforcement } }
            }.RunAsync();
        }
    }
}
