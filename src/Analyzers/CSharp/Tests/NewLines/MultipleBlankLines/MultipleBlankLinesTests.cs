// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.NewLines.MultipleBlankLines;
using Microsoft.CodeAnalysis.Editor.UnitTests.CodeActions;
using Microsoft.CodeAnalysis.NewLines.MultipleBlankLines;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.NewLines.MultipleBlankLines;

using Verify = CSharpCodeFixVerifier<
    CSharpMultipleBlankLinesDiagnosticAnalyzer,
    MultipleBlankLinesCodeFixProvider>;

public sealed class MultipleBlankLinesTests
{
    [Fact]
    public async Task TestOneBlankLineAtTopOfFile()
    {
        await new Verify.Test
        {
            TestCode = @"
// comment",
            Options = { { CodeStyleOptions2.AllowMultipleBlankLines, CodeStyleOption2.FalseWithSuggestionEnforcement } }
        }.RunAsync();
    }

    [Fact]
    public async Task TestTwoBlankLineAtTopOfFile()
    {
        await new Verify.Test
        {
            TestCode = @"[||]

// comment",
            FixedCode = @"
// comment",
            Options = { { CodeStyleOptions2.AllowMultipleBlankLines, CodeStyleOption2.FalseWithSuggestionEnforcement } }
        }.RunAsync();
    }

    [Fact]
    public async Task TestTwoBlankLineAtTopOfFile_NotWithOptionOff()
    {
        await new Verify.Test
        {
            TestCode = @"

// comment",
            Options = { { CodeStyleOptions2.AllowMultipleBlankLines, CodeStyleOption2.TrueWithSuggestionEnforcement } }
        }.RunAsync();
    }

    [Fact]
    public async Task TestThreeBlankLineAtTopOfFile()
    {
        await new Verify.Test
        {
            TestCode = @"[||]


// comment",
            FixedCode = @"
// comment",
            Options = { { CodeStyleOptions2.AllowMultipleBlankLines, CodeStyleOption2.FalseWithSuggestionEnforcement } }
        }.RunAsync();
    }

    [Fact]
    public async Task TestFourBlankLineAtTopOfFile()
    {
        await new Verify.Test
        {
            TestCode = @"[||]



// comment",
            FixedCode = @"
// comment",
            Options = { { CodeStyleOptions2.AllowMultipleBlankLines, CodeStyleOption2.FalseWithSuggestionEnforcement } }
        }.RunAsync();
    }

    [Fact]
    public async Task TestOneBlankLineAtTopOfEmptyFile()
    {
        await new Verify.Test
        {
            TestCode = @"
",
            Options = { { CodeStyleOptions2.AllowMultipleBlankLines, CodeStyleOption2.FalseWithSuggestionEnforcement } }
        }.RunAsync();
    }

    [Fact]
    public async Task TestTwoBlankLinesAtTopOfEmptyFile()
    {
        await new Verify.Test
        {
            TestCode = @"[||]

",
            FixedCode = @"
",
            Options = { { CodeStyleOptions2.AllowMultipleBlankLines, CodeStyleOption2.FalseWithSuggestionEnforcement } }
        }.RunAsync();
    }

    [Fact]
    public async Task TestThreeBlankLinesAtTopOfEmptyFile()
    {
        await new Verify.Test
        {
            TestCode = @"[||]


",
            FixedCode = @"
",
            Options = { { CodeStyleOptions2.AllowMultipleBlankLines, CodeStyleOption2.FalseWithSuggestionEnforcement } }
        }.RunAsync();
    }

    [Fact]
    public async Task TestFourBlankLinesAtTopOfEmptyFile()
    {
        await new Verify.Test
        {
            TestCode = @"[||]



",
            FixedCode = @"
",
            Options = { { CodeStyleOptions2.AllowMultipleBlankLines, CodeStyleOption2.FalseWithSuggestionEnforcement } }
        }.RunAsync();
    }

    [Fact]
    public async Task TestNoBlankLineAtEndOfFile_1()
    {
        await new Verify.Test
        {
            TestCode = @"class C
{
}",
            Options = { { CodeStyleOptions2.AllowMultipleBlankLines, CodeStyleOption2.FalseWithSuggestionEnforcement } }
        }.RunAsync();
    }

    [Fact]
    public async Task TestNoBlankLineAtEndOfFile_2()
    {
        await new Verify.Test
        {
            TestCode = @"class C
{
}
",
            Options = { { CodeStyleOptions2.AllowMultipleBlankLines, CodeStyleOption2.FalseWithSuggestionEnforcement } }
        }.RunAsync();
    }

    [Fact]
    public async Task TestOneBlankLineAtEndOfFile()
    {
        await new Verify.Test
        {
            TestCode = @"class C
{
}

",
            Options = { { CodeStyleOptions2.AllowMultipleBlankLines, CodeStyleOption2.FalseWithSuggestionEnforcement } }
        }.RunAsync();
    }

    [Fact]
    public async Task TestTwoBlankLineAtEndOfFile()
    {
        await new Verify.Test
        {
            TestCode = @"class C
{
}
[||]

",
            FixedCode = @"class C
{
}

",
            Options = { { CodeStyleOptions2.AllowMultipleBlankLines, CodeStyleOption2.FalseWithSuggestionEnforcement } }
        }.RunAsync();
    }

    [Fact]
    public async Task TestThreeBlankLineAtEndOfFile()
    {
        await new Verify.Test
        {
            TestCode = @"class C
{
}
[||]


",
            FixedCode = @"class C
{
}

",
            Options = { { CodeStyleOptions2.AllowMultipleBlankLines, CodeStyleOption2.FalseWithSuggestionEnforcement } }
        }.RunAsync();
    }

    [Fact]
    public async Task TestFourBlankLineAtEndOfFile()
    {
        await new Verify.Test
        {
            TestCode = @"class C
{
}
[||]



",
            FixedCode = @"class C
{
}

",
            Options = { { CodeStyleOptions2.AllowMultipleBlankLines, CodeStyleOption2.FalseWithSuggestionEnforcement } }
        }.RunAsync();
    }

    [Fact]
    public async Task TestNoBlankLineBetweenTokens()
    {
        await new Verify.Test
        {
            TestCode = @"class C
{
}",
            Options = { { CodeStyleOptions2.AllowMultipleBlankLines, CodeStyleOption2.FalseWithSuggestionEnforcement } }
        }.RunAsync();
    }

    [Fact]
    public async Task TestOneBlankLineBetweenTokens()
    {
        await new Verify.Test
        {
            TestCode = @"class C
{

}",
            Options = { { CodeStyleOptions2.AllowMultipleBlankLines, CodeStyleOption2.FalseWithSuggestionEnforcement } }
        }.RunAsync();
    }

    [Fact]
    public async Task TestTwoBlankLineBetweenTokens()
    {
        await new Verify.Test
        {
            TestCode = @"class C
{
[||]

}",
            FixedCode = @"class C
{

}",
            Options = { { CodeStyleOptions2.AllowMultipleBlankLines, CodeStyleOption2.FalseWithSuggestionEnforcement } }
        }.RunAsync();
    }

    [Fact]
    public async Task TestThreeBlankLineBetweenTokens()
    {
        await new Verify.Test
        {
            TestCode = @"class C
{
[||]


}",
            FixedCode = @"class C
{

}",
            Options = { { CodeStyleOptions2.AllowMultipleBlankLines, CodeStyleOption2.FalseWithSuggestionEnforcement } }
        }.RunAsync();
    }

    [Fact]
    public async Task TestFourBlankLineBetweenTokens()
    {
        await new Verify.Test
        {
            TestCode = @"class C
{
[||]



}",
            FixedCode = @"class C
{

}",
            Options = { { CodeStyleOptions2.AllowMultipleBlankLines, CodeStyleOption2.FalseWithSuggestionEnforcement } }
        }.RunAsync();
    }

    [Fact]
    public async Task TestNoBlankLineAfterComment()
    {
        await new Verify.Test
        {
            TestCode = @"class C
{
    // comment
}",
            Options = { { CodeStyleOptions2.AllowMultipleBlankLines, CodeStyleOption2.FalseWithSuggestionEnforcement } }
        }.RunAsync();
    }

    [Fact]
    public async Task TestOneBlankLineAfterComment()
    {
        await new Verify.Test
        {
            TestCode = @"class C
{
    // comment

}",
            Options = { { CodeStyleOptions2.AllowMultipleBlankLines, CodeStyleOption2.FalseWithSuggestionEnforcement } }
        }.RunAsync();
    }

    [Fact]
    public async Task TestTwoBlankLineAfterComment()
    {
        await new Verify.Test
        {
            TestCode = @"class C
{
    // comment
[||]

}",
            FixedCode = @"class C
{
    // comment

}",
            Options = { { CodeStyleOptions2.AllowMultipleBlankLines, CodeStyleOption2.FalseWithSuggestionEnforcement } }
        }.RunAsync();
    }

    [Fact]
    public async Task TestThreeBlankLineAfterComment()
    {
        await new Verify.Test
        {
            TestCode = @"class C
{
    // comment
[||]


}",
            FixedCode = @"class C
{
    // comment

}",
            Options = { { CodeStyleOptions2.AllowMultipleBlankLines, CodeStyleOption2.FalseWithSuggestionEnforcement } }
        }.RunAsync();
    }

    [Fact]
    public async Task TestFourBlankLineAfterComment()
    {
        await new Verify.Test
        {
            TestCode = @"class C
{
    // comment
[||]


}",
            FixedCode = @"class C
{
    // comment

}",
            Options = { { CodeStyleOptions2.AllowMultipleBlankLines, CodeStyleOption2.FalseWithSuggestionEnforcement } }
        }.RunAsync();
    }

    [Fact]
    public async Task TestNoBlankLineAfterDirective()
    {
        await new Verify.Test
        {
            TestCode = @"class C
{
    #nullable enable
}",
            LanguageVersion = Microsoft.CodeAnalysis.CSharp.LanguageVersion.CSharp8,
            Options = { { CodeStyleOptions2.AllowMultipleBlankLines, CodeStyleOption2.FalseWithSuggestionEnforcement } }
        }.RunAsync();
    }

    [Fact]
    public async Task TestOneBlankLineAfterDirective()
    {
        await new Verify.Test
        {
            TestCode = @"class C
{
    #nullable enable

}",
            LanguageVersion = Microsoft.CodeAnalysis.CSharp.LanguageVersion.CSharp8,
            Options = { { CodeStyleOptions2.AllowMultipleBlankLines, CodeStyleOption2.FalseWithSuggestionEnforcement } }
        }.RunAsync();
    }

    [Fact]
    public async Task TestTwoBlankLineAfterDirective()
    {
        await new Verify.Test
        {
            TestCode = @"class C
{
    #nullable enable
[||]

}",
            FixedCode = @"class C
{
    #nullable enable

}",
            LanguageVersion = Microsoft.CodeAnalysis.CSharp.LanguageVersion.CSharp8,
            Options = { { CodeStyleOptions2.AllowMultipleBlankLines, CodeStyleOption2.FalseWithSuggestionEnforcement } }
        }.RunAsync();
    }

    [Fact]
    public async Task TestThreeBlankLineAfterDirective()
    {
        await new Verify.Test
        {
            TestCode = @"class C
{
    #nullable enable
[||]


}",
            FixedCode = @"class C
{
    #nullable enable

}",
            LanguageVersion = Microsoft.CodeAnalysis.CSharp.LanguageVersion.CSharp8,
            Options = { { CodeStyleOptions2.AllowMultipleBlankLines, CodeStyleOption2.FalseWithSuggestionEnforcement } }
        }.RunAsync();
    }

    [Fact]
    public async Task TestFourBlankLineAfterDirective()
    {
        await new Verify.Test
        {
            TestCode = @"class C
{
    #nullable enable
[||]


}",
            FixedCode = @"class C
{
    #nullable enable

}",
            LanguageVersion = Microsoft.CodeAnalysis.CSharp.LanguageVersion.CSharp8,
            Options = { { CodeStyleOptions2.AllowMultipleBlankLines, CodeStyleOption2.FalseWithSuggestionEnforcement } }
        }.RunAsync();
    }

    [Fact]
    public async Task TestNoBlankLineAfterDocComment()
    {
        await new Verify.Test
        {
            TestCode = @"
/// <summary/>
class C
{
}",
            Options = { { CodeStyleOptions2.AllowMultipleBlankLines, CodeStyleOption2.FalseWithSuggestionEnforcement } }
        }.RunAsync();
    }

    [Fact]
    public async Task TestOneBlankLineAfterDocComment()
    {
        await new Verify.Test
        {
            TestCode = @"
/// <summary/>

class C
{
}",
            Options = { { CodeStyleOptions2.AllowMultipleBlankLines, CodeStyleOption2.FalseWithSuggestionEnforcement } }
        }.RunAsync();
    }

    [Fact]
    public async Task TestTwoBlankLineAfterDocComment()
    {
        await new Verify.Test
        {
            TestCode = @"
/// <summary/>
[||]

class C
{
}",
            FixedCode = @"
/// <summary/>

class C
{
}",
            Options = { { CodeStyleOptions2.AllowMultipleBlankLines, CodeStyleOption2.FalseWithSuggestionEnforcement } }
        }.RunAsync();
    }

    [Fact]
    public async Task TestThreeBlankLineAfterDocComment()
    {
        await new Verify.Test
        {
            TestCode = @"
/// <summary/>
[||]


class C
{
}",
            FixedCode = @"
/// <summary/>

class C
{
}",
            Options = { { CodeStyleOptions2.AllowMultipleBlankLines, CodeStyleOption2.FalseWithSuggestionEnforcement } }
        }.RunAsync();
    }

    [Fact]
    public async Task TestFourBlankLineAfterDocComment()
    {
        await new Verify.Test
        {
            TestCode = @"
/// <summary/>
[||]



class C
{
}",
            FixedCode = @"
/// <summary/>

class C
{
}",
            Options = { { CodeStyleOptions2.AllowMultipleBlankLines, CodeStyleOption2.FalseWithSuggestionEnforcement } }
        }.RunAsync();
    }

    [Fact]
    public async Task TestNoBlankLineAllConstructs()
    {
        await new Verify.Test
        {
            TestCode = @"/// <summary/>
//
#nullable enable
class C
{
}",
            LanguageVersion = Microsoft.CodeAnalysis.CSharp.LanguageVersion.CSharp8,
            Options = { { CodeStyleOptions2.AllowMultipleBlankLines, CodeStyleOption2.FalseWithSuggestionEnforcement } }
        }.RunAsync();
    }

    [Fact]
    public async Task TestOneBlankLineAllConstructs()
    {
        await new Verify.Test
        {
            TestCode = @"
/// <summary/>

//

#nullable enable

class C
{
}",
            LanguageVersion = Microsoft.CodeAnalysis.CSharp.LanguageVersion.CSharp8,
            Options = { { CodeStyleOptions2.AllowMultipleBlankLines, CodeStyleOption2.FalseWithSuggestionEnforcement } }
        }.RunAsync();
    }

    [Fact]
    public async Task TestTwoBlankLineAllConstructs()
    {
        await new Verify.Test
        {
            TestCode = @"[||]

/// <summary/>


//


#nullable enable


class C
{
}",
            FixedCode = @"
/// <summary/>

//

#nullable enable

class C
{
}",
            LanguageVersion = Microsoft.CodeAnalysis.CSharp.LanguageVersion.CSharp8,
            Options = { { CodeStyleOptions2.AllowMultipleBlankLines, CodeStyleOption2.FalseWithSuggestionEnforcement } }
        }.RunAsync();
    }

    [Fact]
    public async Task TestThreeBlankLineAllConstructs()
    {
        await new Verify.Test
        {
            TestCode = @"[||]


/// <summary/>



//



#nullable enable



class C
{
}",
            FixedCode = @"
/// <summary/>

//

#nullable enable

class C
{
}",
            LanguageVersion = Microsoft.CodeAnalysis.CSharp.LanguageVersion.CSharp8,
            Options = { { CodeStyleOptions2.AllowMultipleBlankLines, CodeStyleOption2.FalseWithSuggestionEnforcement } }
        }.RunAsync();
    }

    [Fact]
    public async Task TestFourBlankLineAllConstructs()
    {
        await new Verify.Test
        {
            TestCode = @"[||]



/// <summary/>




//




#nullable enable




class C
{
}",
            FixedCode = @"
/// <summary/>

//

#nullable enable

class C
{
}",
            LanguageVersion = Microsoft.CodeAnalysis.CSharp.LanguageVersion.CSharp8,
            Options = { { CodeStyleOptions2.AllowMultipleBlankLines, CodeStyleOption2.FalseWithSuggestionEnforcement } }
        }.RunAsync();
    }
}
