// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.ValidateFormatString;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Diagnostics;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.ValidateFormatString
{
    public class ValidateFormatStringTests : AbstractCSharpDiagnosticProviderBasedUserDiagnosticTest
    {
        internal override (DiagnosticAnalyzer, CodeFixProvider) CreateDiagnosticProviderAndFixer(Workspace workspace)
            => (new ValidateFormatStringDiagnosticAnalyzer(), new EmptyCodeFixProvider());
        
        [Fact, Trait(Traits.Feature, Traits.Features.ValidateFormatString)]
        public async Task OnePlaceholder()
        {
            await TestMissingAsync(@" class Program
{
    static void Main(string[] args)
    {
        string.Format(""This {0} works[||]"", ""test""); 
    }     
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.ValidateFormatString)]
        public async Task TwoPlaceholders()
        {
            await TestMissingAsync(@" class Program
{
    static void Main(string[] args)
    {
        string.Format(""This {0} {1} works[||]"", ""test"", ""also""); 
    }     
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.ValidateFormatString)]
        public async Task ThreePlaceholders()
        {
            await TestMissingAsync(@" class Program
{
    static void Main(string[] args)
    {
        string.Format(""This {0} {1} works {2} [||]"", ""test"", ""also"", ""well""); 
    }     
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.ValidateFormatString)]
        public async Task FourPlaceholders()
        {
            await TestMissingAsync(@" class Program
{
    static void Main(string[] args)
    {
        string.Format(""This {1} is {2} [||]my {6} test "", ""teststring1"", ""teststring2"", ""teststring3"", ""teststring4"", ""teststring5"", ""teststring6"", ""teststring7"");
    }     
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.ValidateFormatString)]
        public async Task ParamsObjectArray()
        {
            await TestMissingAsync(@" class Program
{
    static void Main(string[] args)
    {
        object[] testParamsArray = { 1.25, ""2"", ""teststring""};
        string.Format(""This {0} {1} {2} works[||]"", testParamsArray); 
    }     
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.ValidateFormatString)]
        public async Task IFormatProviderandOnePlaceholder()
        {
            await TestMissingAsync(@" using System.Globalization; 
class Program
{
    static void Main(string[] args)
    {
        CultureInfo culture = new System.Globalization.CultureInfo(""da - da"");
        testStr = string.Format(culture, ""The current price [||]is {0:C2} per ounce"", 2.45);
    }     
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.ValidateFormatString)]
        public async Task IFormatProviderandTwoPlaceholders()
        {
            await TestMissingAsync(@" using System.Globalization; 
class Program
{
    static void Main(string[] args)
    {
        CultureInfo culture = new System.Globalization.CultureInfo(""da - da"");
        testStr = string.Format(culture, ""The current price [||]is {0:C2} per {1} "", 2.45, ""ounce"");
    }     
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.ValidateFormatString)]
        public async Task IFormatProviderandThreePlaceholders()
        {
            await TestMissingAsync(@" using System.Globalization; 
class Program
{
    static void Main(string[] args)
    {
        CultureInfo culture = new System.Globalization.CultureInfo(""da - da"");
        testStr = string.Format(culture, ""The current price [||]is {0} {1} {2} "", 2.45, ""per"", ""ounce"");
    }     
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.ValidateFormatString)]
        public async Task IFormatProviderandFourPlaceholders()
        {
            await TestMissingAsync(@" using System.Globalization; 
class Program
{
    static void Main(string[] args)
    {
        CultureInfo culture = new System.Globalization.CultureInfo(""da - da"");
        testStr = string.Format(culture, ""The current price [||]is {0} {1} {2} {3} "", 2.45, ""per"", ""ounce"", ""today only"");
    }     
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.ValidateFormatString)]
        public async Task IFormatProviderandParamsObjectArray()
        {
            await TestMissingAsync(@" using System.Globalization; 
class Program
{
    static void Main(string[] args)
    {
        CultureInfo culture = new System.Globalization.CultureInfo(""da - da"");
        object[] testParamsArray = { 1.25, ""2"", ""teststring""};
        string.Format(culture, ""This {0} {1} {2} works[||]"", testParamsArray); 
    }     
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.ValidateFormatString)]
        public async Task FormatStringWithComma()
        {
            await TestMissingAsync(@" class Program
{
    static void Main(string[] args)
    {
        string.Format(""{0,6}[||]"",34);
    }     
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.ValidateFormatString)]
        public async Task FormatStringWithColon()
        {
            await TestMissingAsync(@" class Program
{
    static void Main(string[] args)
    {
        string.Format(""{0:[||]N0}"",34);
    }     
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.ValidateFormatString)]
        public async Task FormatStringWithCommaAndColon()
        {
            await TestMissingAsync(@" class Program
{
    static void Main(string[] args)
    {
        string.Format(""Test {0,15:[||]N0} output"",34);
    }     
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.ValidateFormatString)]
        public async Task FormatStringWithPlaceholderAtBeginning()
        {
            await TestMissingAsync(@" class Program
{
    static void Main(string[] args)
    {
        string.Format(""{0} is my [||]test case"",""This"");
    }     
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.ValidateFormatString)]
        public async Task FormatStringWithPlaceholderAtEnd()
        {
            await TestMissingAsync(@" class Program
{
    static void Main(string[] args)
    {
        string.Format(""This is [||]my {0}"",""test"");
    }     
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.ValidateFormatString)]
        public async Task FormatStringWithDoubleBraces()
        {
            await TestMissingAsync(@" class Program
{
    static void Main(string[] args)
    {
        string.Format("" {{ 2}} This {1} is[||] {2} {{ my {0} test }} "", ""teststring1"", ""teststring2"", ""teststring3"");
    }     
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.ValidateFormatString)]
        public async Task FormatStringWithDoubleBracesAtBeginning()
        {
            await TestMissingAsync(@" class Program
{
    static void Main(string[] args)
    {
        string.Format(""{{ 2}} This {1} is[||] {2} {{ my {0} test }} "", ""teststring1"", ""teststring2"", ""teststring3"");
    }     
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.ValidateFormatString)]
        public async Task FormatStringWithDoubleBracesAtEnd()
        {
            await TestMissingAsync(@" class Program
{
    static void Main(string[] args)
    {
        string.Format("" {{ 2}} This {1} is[||] {2} {{ my {0} test }}"", ""teststring1"", ""teststring2"", ""teststring3"");
    }     
}");
        }
        [Fact, Trait(Traits.Feature, Traits.Features.ValidateFormatString)]
        public async Task PassingOnePlaceholderOutOfBounds()
        {
            await TestSpansAsync(@" class Program
{
    static void Main(string[] args)
    {
        string.Format([|""This {1} is my test""|], ""teststring1"");
    }     
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.ValidateFormatString)]
        public async Task PassingTwoPlaceholdersWithOnePlaceholderOutOfBounds()
        {
            await TestSpansAsync(@" class Program
{
    static void Main(string[] args)
    {
        string.Format([|""This {2} is my test""|], ""teststring1"", ""teststring2"");
    }     
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.ValidateFormatString)]
        public async Task PassingThreePlaceholdersWithOnePlaceholderOutOfBounds()
        {
            await TestSpansAsync(@" class Program
{
    static void Main(string[] args)
    {
        string.Format([|""This{0}{1}{2}{3} is my test""|], ""teststring1"", ""teststring2"", ""teststring3"");
    }     
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.ValidateFormatString)]
        public async Task FormatStringPassingFourPlaceholdersWithOnePlaceholderOutOfBounds()
        {
            await TestSpansAsync(@" class Program
{
    static void Main(string[] args)
    {
        string.Format([|""This{0}{1}{2}{3}{4} is my test""|], ""teststring1"", ""teststring2"", ""teststring3"", ""teststring4"");
    }     
}");
        }
        [Fact, Trait(Traits.Feature, Traits.Features.ValidateFormatString)]
        public async Task PassingiFormatProviderandOnePlaceholderOutOfBounds()
        {
            await TestSpansAsync(@" using System.Globalization; 
class Program
{
    static void Main(string[] args)
    {
        CultureInfo culture = new System.Globalization.CultureInfo(""da - da"");
        string.Format(culture, [|""This {1} is my test""|], ""teststring1"");
    }     
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.ValidateFormatString)]
        public async Task PassingiFormatProviderandTwoPlaceholdersWithOnePlaceholderOutOfBounds()
        {
            await TestSpansAsync(@" using System.Globalization; 
class Program
{
    static void Main(string[] args)
    {
        CultureInfo culture = new System.Globalization.CultureInfo(""da - da"");
        string.Format(culture, [|""This {2} is my test""|], ""teststring1"", ""teststring2"");
    }     
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.ValidateFormatString)]
        public async Task PassingIFormatProviderandThreePlaceholdersWithOnePlaceholderOutOfBounds()
        {
            await TestSpansAsync(@" using System.Globalization; 
class Program
{
    static void Main(string[] args)
    {
        CultureInfo culture = new System.Globalization.CultureInfo(""da - da"");
        string.Format(culture, [|""This{0}{1}{2}{3} is my test""|], ""teststring1"", ""teststring2"", ""teststring3"");
    }     
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.ValidateFormatString)]
        public async Task PassingIFormatProviderandFourPlaceholdersWithOnePlaceholderOutOfBounds()
        {
            await TestSpansAsync(@" using System.Globalization; 
class Program
{
    static void Main(string[] args)
    {
        CultureInfo culture = new System.Globalization.CultureInfo(""da - da"");
        string.Format(culture, [|""This{0}{1}{2}{3}{4} is my test""|], ""teststring1"", ""teststring2"", ""teststring3"", ""teststring4"");
    }     
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.ValidateFormatString)]
        public async Task PlaceholderAtBeginningWithOnePlaceholderOutOfBounds()
        {
            await TestSpansAsync(@" class Program
{
    static void Main(string[] args)
    {
        string.Format( [|""{1}is my test""|], ""teststring1"");
    }     
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.ValidateFormatString)]
        public async Task PlaceholderAtEndWithOnePlaceholderOutOfBounds()
        {
            await TestSpansAsync(@" class Program
{
    static void Main(string[] args)
    {
        string.Format( [|""is my test {2}""|], ""teststring1"", ""teststring2"");
    }     
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.ValidateFormatString)]
        public async Task FormatStringDoubleBracesAtBeginningWithOnePlaceholderOutOfBounds()
        {
            await TestSpansAsync(@" class Program
{
    static void Main(string[] args)
    {
        string.Format( [|""}}is my test {2}""|], ""teststring1"", ""teststring2"");
    }     
}");
        }
        [Fact, Trait(Traits.Feature, Traits.Features.ValidateFormatString)]
        public async Task DoubleBracesAtEndWithOnePlaceholderOutOfBounds()
        {
            await TestSpansAsync(@" class Program
{
    static void Main(string[] args)
    {
        string.Format( [|""is my test {2}{{""|], ""teststring1"", ""teststring2"");
    }     
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.ValidateFormatString)]
        public async Task NamedParameters()
        {
            await TestMissingAsync(@" class Program
{
    static void Main(string[] args)
    {
        string.Format(arg0: ""test"", arg1: ""also"", format: ""This {0} {1} works[||]""); 
    }     
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.ValidateFormatString)]
        public async Task NamedParametersWithIFormatProvider()
        {
            await TestMissingAsync(@" using System.Globalization;
class Program
{
    static void Main(string[] args)
    {
        CultureInfo culture = new System.Globalization.CultureInfo(""da - da"");
        string.Format(arg0: ""test"", provider: culture, format: ""This {0} works[||]""); 
    }     
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.ValidateFormatString)]
        public async Task NamedParametersWithIFormatProviderAndParamsObjectArray()
        {
            await TestMissingAsync(@" using System.Globalization;
class Program
{
    static void Main(string[] args)
    {
        object[] testParamsArray = { 1.25, ""2"", ""teststring""};
        CultureInfo culture = new System.Globalization.CultureInfo(""da - da"");
        string.Format(params object: testParamsArray, provider: culture, format: ""This {0} works[||]""); 
    }     
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.ValidateFormatString)]
        public async Task NamespaceAliasForStringClass()
        {
            await TestMissingAsync(@" using stringAlias = System.String;
class Program
{
    static void Main(string[] args)
    {
        stringAlias.Format(""This {0} works[||]"", ""test""); 
    }     
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.ValidateFormatString)]
        public async Task FormatStringMethodCallAsAnArgumentToAnotherMethod()
        {
            await TestMissingAsync(@" using System.IO;
class Program
{
    static void Main(string[] args)
    {
        Console.WriteLine(string.Format(format: ""This {0} works[||]"", arg0:""test"")); 
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.ValidateFormatString)]
        public async Task FormatStringVerbatimMultipleLines()
        {
            await TestMissingAsync(@" class Program
{
    static void Main(string[] args)
    {
        string.Format(@""This {0} 
{1} {2} works[||]"", ""multiple"", ""line"", ""test"")); 
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.ValidateFormatString)]
        public async Task FormatStringVerbatimMultipleLinesPlaceholderOutOfBounds()
        {
            await TestSpansAsync(@" class Program
{
    static void Main(string[] args)
    {
        string.Format([|@""This {0} 
{1} {3} works""|], ""multiple"", ""line"", ""test"")); 
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.ValidateFormatString)]
        public async Task FormatStringInterpolated()
        {
            await TestMissingAsync(@" class Program
{
    static void Main(string[] args)
    {
        var Name = ""Peter"";
        var Age = 30;
       
        string.Format($""{Name,[||] 20} is {Age:D3} ""); 
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.ValidateFormatString)]
        public async Task FormatStringEmpty()
        {
            await TestMissingAsync(@" class Program
{
    static void Main(string[] args)
    {
        string.Format(""[||]""); 
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.ValidateFormatString)]
        public async Task FormatStringLeftParenOnly()
        {
            await TestMissingAsync(@" class Program
{
    static void Main(string[] args)
    {
        string.Format([||]; 
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.ValidateFormatString)]
        public async Task FormatStringParenthesesOnly()
        {
            await TestMissingAsync(@" class Program
{
    static void Main(string[] args)
    {
        string.Format([||]); 
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.ValidateFormatString)]
        public async Task FormatStringEmptyString()
        {
            await TestMissingAsync(@" class Program
{
    static void Main(string[] args)
    {
        string.Format(""[||]""); 
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.ValidateFormatString)]
        public async Task FormatOnly_NoStringDot()
        {
            await TestMissingAsync(@" using static System.String
class Program
{
    static void Main(string[] args)
    {
        Format(""[||]""); 
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.ValidateFormatString)]
        public async Task NamedParametersOneOutOfBounds()
        {
            await TestSpansAsync(@" using System.Globalization; 
class Program
{
    static void Main(string[] args)
    {
        string.Format(arg0: ""test"", arg1: ""also"", [|format: ""This {0} {2} works""|]);
    }     
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.ValidateFormatString)]
        public async Task NamedParametersWithIFormatProviderOneOutOfBounds()
        {
            await TestSpansAsync(@" using System.Globalization; 
class Program
{
    static void Main(string[] args)
    {
        CultureInfo culture = new System.Globalization.CultureInfo(""da - da"");
        string.Format(arg0: ""test"", arg1: ""also"", [|format: ""This {0} {2} works""|], provider: culture)
    }     
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.ValidateFormatString)]
        public async Task FormatOnly_NoStringDot_OneOutOfBounds()
        {
            await TestSpansAsync(@" using static System.String
class Program
{
    static void Main(string[] args)
    {
        Format([|""This {0} {2} squiggles""|], ""test"", ""gets"");
    }     
}");
        }
    }

    // Currently the test infrastructure doesn't accomodate diagnostics without code fixes,
    // so this empty code fix provider is a temporary solution to get tests running.
    // I plan to add an appropriate test helper to test diagnostics without code fixes
    public class EmptyCodeFixProvider : CodeFixProvider
    {
        public override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(IDEDiagnosticIds.ValidateFormatStringDiagnosticID);

        public override Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            context.RegisterCodeFix(new MyCodeAction(), context.Diagnostics[0]);
            return Task.CompletedTask;
        }

        private class MyCodeAction : CodeAction
        {
            public override string Title => "";
        }
    }
}
