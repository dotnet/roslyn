// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading.Tasks;
using Roslyn.Diagnostics.VisualBasic.Analyzers.BlankLines;
using Test.Utilities;
using Xunit;

namespace Roslyn.Diagnostics.Analyzers.UnitTests.BlankLines
{
    using Verify = VisualBasicCodeFixVerifier<
        VisualBasicBlankLinesDiagnosticAnalyzer,
        VisualBasicBlankLinesCodeFixProvider>;

    public class BlankLinesTests_VisualBasic
    {
        [Fact]
        public async Task TestOneBlankLineAtTopOfFile()
        {
            var code =
@"
' comment";

            await new Verify.Test()
            {
                TestCode = code,
                FixedCode = code,
            }.RunAsync();
        }

        [Fact]
        public async Task TestTwoBlankLineAtTopOfFile()
        {
            var code =
@"[||]

' comment";
            var fixedCode =
@"
' comment";

            await new Verify.Test()
            {
                TestCode = code,
                FixedCode = fixedCode,
            }.RunAsync();
        }

        [Fact]
        public async Task TestThreeBlankLineAtTopOfFile()
        {
            var code =
@"[||]


' comment";
            var fixedCode =
@"
' comment";

            await new Verify.Test()
            {
                TestCode = code,
                FixedCode = fixedCode,
            }.RunAsync();
        }

        [Fact]
        public async Task TestFourBlankLineAtTopOfFile()
        {
            var code =
@"[||]



' comment";
            var fixedCode =
@"
' comment";

            await new Verify.Test()
            {
                TestCode = code,
                FixedCode = fixedCode,
            }.RunAsync();
        }

        [Fact]
        public async Task TestOneBlankLineAtTopOfEmptyFile()
        {
            var code =
@"
";

            await new Verify.Test()
            {
                TestCode = code,
                FixedCode = code,
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

            await new Verify.Test()
            {
                TestCode = code,
                FixedCode = fixedCode,
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

            await new Verify.Test()
            {
                TestCode = code,
                FixedCode = fixedCode,
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

            await new Verify.Test()
            {
                TestCode = code,
                FixedCode = fixedCode,
            }.RunAsync();
        }

        [Fact]
        public async Task TestNoBlankLineAtEndOfFile_1()
        {
            var code =
@"Class C
End Class";

            await new Verify.Test()
            {
                TestCode = code,
                FixedCode = code,
            }.RunAsync();
        }

        [Fact]
        public async Task TestNoBlankLineAtEndOfFile_2()
        {
            var code =
@"Class C
End Class
";

            await new Verify.Test()
            {
                TestCode = code,
                FixedCode = code,
            }.RunAsync();
        }

        [Fact]
        public async Task TestOneBlankLineAtEndOfFile()
        {
            var code =
@"Class C
End Class

";

            await new Verify.Test()
            {
                TestCode = code,
                FixedCode = code,
            }.RunAsync();
        }

        [Fact]
        public async Task TestTwoBlankLineAtEndOfFile()
        {
            var code =
@"Class C
End Class
[||]

";
            var fixedCode =
@"Class C
End Class

";

            await new Verify.Test()
            {
                TestCode = code,
                FixedCode = fixedCode,
            }.RunAsync();
        }

        [Fact]
        public async Task TestThreeBlankLineAtEndOfFile()
        {
            var code =
@"Class C
End Class
[||]


";
            var fixedCode =
@"Class C
End Class

";

            await new Verify.Test()
            {
                TestCode = code,
                FixedCode = fixedCode,
            }.RunAsync();
        }

        [Fact]
        public async Task TestFourBlankLineAtEndOfFile()
        {
            var code =
@"Class C
End Class
[||]



";
            var fixedCode =
@"Class C
End Class

";

            await new Verify.Test()
            {
                TestCode = code,
                FixedCode = fixedCode,
            }.RunAsync();
        }

        [Fact]
        public async Task TestNoBlankLineBetweenTokens()
        {
            var code =
@"Class C
End Class";

            await new Verify.Test()
            {
                TestCode = code,
                FixedCode = code,
            }.RunAsync();
        }

        [Fact]
        public async Task TestOneBlankLineBetweenTokens()
        {
            var code =
@"Class C

End Class";

            await new Verify.Test()
            {
                TestCode = code,
                FixedCode = code,
            }.RunAsync();
        }

        [Fact]
        public async Task TestTwoBlankLineBetweenTokens()
        {
            var code =
@"Class C
[||]

End Class";
            var fixedCode =
@"Class C

End Class";

            await new Verify.Test()
            {
                TestCode = code,
                FixedCode = fixedCode,
            }.RunAsync();
        }

        [Fact]
        public async Task TestThreeBlankLineBetweenTokens()
        {
            var code =
@"Class C
[||]


End Class";
            var fixedCode =
@"Class C

End Class";

            await new Verify.Test()
            {
                TestCode = code,
                FixedCode = fixedCode,
            }.RunAsync();
        }

        [Fact]
        public async Task TestFourBlankLineBetweenTokens()
        {
            var code =
@"Class C
[||]



End Class";
            var fixedCode =
@"Class C

End Class";

            await new Verify.Test()
            {
                TestCode = code,
                FixedCode = fixedCode,
            }.RunAsync();
        }

        [Fact]
        public async Task TestNoBlankLineAfterComment()
        {
            var code =
@"Class C
    ' comment
End Class";

            await new Verify.Test()
            {
                TestCode = code,
                FixedCode = code,
            }.RunAsync();
        }

        [Fact]
        public async Task TestOneBlankLineAfterComment()
        {
            var code =
@"Class C
    ' comment

End Class";

            await new Verify.Test()
            {
                TestCode = code,
                FixedCode = code,
            }.RunAsync();
        }

        [Fact]
        public async Task TestTwoBlankLineAfterComment()
        {
            var code =
@"Class C
    ' comment
[||]

End Class";
            var fixedCode =
@"Class C
    ' comment

End Class";

            await new Verify.Test()
            {
                TestCode = code,
                FixedCode = fixedCode,
            }.RunAsync();
        }

        [Fact]
        public async Task TestThreeBlankLineAfterComment()
        {
            var code =
@"Class C
    ' comment
[||]


End Class";
            var fixedCode =
@"Class C
    ' comment

End Class";

            await new Verify.Test()
            {
                TestCode = code,
                FixedCode = fixedCode,
            }.RunAsync();
        }

        [Fact]
        public async Task TestFourBlankLineAfterComment()
        {
            var code =
@"Class C
    ' comment
[||]


End Class";
            var fixedCode =
@"Class C
    ' comment

End Class";

            await new Verify.Test()
            {
                TestCode = code,
                FixedCode = fixedCode,
            }.RunAsync();
        }

        [Fact]
        public async Task TestNoBlankLineAfterDirective()
        {
            var code =
@"Class C
    #Const X = 0
End Class";

            await new Verify.Test()
            {
                TestCode = code,
                FixedCode = code,
            }.RunAsync();
        }

        [Fact]
        public async Task TestOneBlankLineAfterDirective()
        {
            var code =
@"Class C
    #Const X = 0

End Class";

            await new Verify.Test()
            {
                TestCode = code,
                FixedCode = code,
            }.RunAsync();
        }

        [Fact]
        public async Task TestTwoBlankLineAfterDirective()
        {
            var code =
@"Class C
    #Const X = 0
[||]

End Class";
            var fixedCode =
@"Class C
    #Const X = 0

End Class";

            await new Verify.Test()
            {
                TestCode = code,
                FixedCode = fixedCode,
            }.RunAsync();
        }

        [Fact]
        public async Task TestThreeBlankLineAfterDirective()
        {
            var code =
@"Class C
    #Const X = 0
[||]


End Class";
            var fixedCode =
@"Class C
    #Const X = 0

End Class";

            await new Verify.Test()
            {
                TestCode = code,
                FixedCode = fixedCode,
            }.RunAsync();
        }

        [Fact]
        public async Task TestFourBlankLineAfterDirective()
        {
            var code =
@"Class C
    #Const X = 0
[||]


End Class";
            var fixedCode =
@"Class C
    #Const X = 0

End Class";

            await new Verify.Test()
            {
                TestCode = code,
                FixedCode = fixedCode,
            }.RunAsync();
        }

        [Fact]
        public async Task TestNoBlankLineAfterDocComment()
        {
            var code =
@"
''' <summary/>
Class C
End Class";

            await new Verify.Test()
            {
                TestCode = code,
                FixedCode = code,
            }.RunAsync();
        }

        [Fact]
        public async Task TestOneBlankLineAfterDocComment()
        {
            var code =
@"
''' <summary/>

Class C
End Class";

            await new Verify.Test()
            {
                TestCode = code,
                FixedCode = code,
            }.RunAsync();
        }

        [Fact]
        public async Task TestTwoBlankLineAfterDocComment()
        {
            var code =
@"
''' <summary/>
[||]

Class C
End Class";
            var fixedCode =
@"
''' <summary/>

Class C
End Class";

            await new Verify.Test()
            {
                TestCode = code,
                FixedCode = fixedCode,
            }.RunAsync();
        }

        [Fact]
        public async Task TestThreeBlankLineAfterDocComment()
        {
            var code =
@"
''' <summary/>
[||]


Class C
End Class";
            var fixedCode =
@"
''' <summary/>

Class C
End Class";

            await new Verify.Test()
            {
                TestCode = code,
                FixedCode = fixedCode,
            }.RunAsync();
        }

        [Fact]
        public async Task TestFourBlankLineAfterDocComment()
        {
            var code =
@"
''' <summary/>
[||]



Class C
End Class";
            var fixedCode =
@"
''' <summary/>

Class C
End Class";

            await new Verify.Test()
            {
                TestCode = code,
                FixedCode = fixedCode,
            }.RunAsync();
        }

        [Fact]
        public async Task TestNoBlankLineAllConstructs()
        {
            var code =
@"''' <summary/>
'
#Const X = 0
Class C
End Class";

            await new Verify.Test()
            {
                TestCode = code,
                FixedCode = code,
            }.RunAsync();
        }

        [Fact]
        public async Task TestOneBlankLineAllConstructs()
        {
            var code =
@"
''' <summary/>

'

#Const X = 0

Class C
End Class";

            await new Verify.Test()
            {
                TestCode = code,
                FixedCode = code,
            }.RunAsync();
        }

        [Fact]
        public async Task TestTwoBlankLineAllConstructs()
        {
            var code =
@"[||]

''' <summary/>


'


#Const X = 0


Class C
End Class";
            var fixedCode =
@"
''' <summary/>

'

#Const X = 0

Class C
End Class";

            await new Verify.Test()
            {
                TestCode = code,
                FixedCode = fixedCode,
            }.RunAsync();
        }

        [Fact]
        public async Task TestThreeBlankLineAllConstructs()
        {
            var code =
@"[||]


''' <summary/>



'



#Const X = 0



Class C
End Class";
            var fixedCode =
@"
''' <summary/>

'

#Const X = 0

Class C
End Class";

            await new Verify.Test()
            {
                TestCode = code,
                FixedCode = fixedCode,
            }.RunAsync();
        }

        [Fact]
        public async Task TestFourBlankLineAllConstructs()
        {
            var code =
@"[||]



''' <summary/>




'




#Const X = 0




Class C
End Class";
            var fixedCode =
@"
''' <summary/>

'

#Const X = 0

Class C
End Class";

            await new Verify.Test()
            {
                TestCode = code,
                FixedCode = fixedCode,
            }.RunAsync();
        }
    }
}
