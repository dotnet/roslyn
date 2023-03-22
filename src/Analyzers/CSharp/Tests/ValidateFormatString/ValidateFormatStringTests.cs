// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.ValidateFormatString;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Diagnostics;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.ValidateFormatString
{
    [Trait(Traits.Feature, Traits.Features.ValidateFormatString)]
    public class ValidateFormatStringTests : AbstractCSharpDiagnosticProviderBasedUserDiagnosticTest
    {
        public ValidateFormatStringTests(ITestOutputHelper logger)
           : base(logger)
        {
        }

        internal override (DiagnosticAnalyzer, CodeFixProvider?) CreateDiagnosticProviderAndFixer(Workspace workspace)
            => (new CSharpValidateFormatStringDiagnosticAnalyzer(), null);

        [Fact]
        public async Task OnePlaceholder()
        {
            await TestDiagnosticMissingAsync("""
                class Program
                {
                    static void Main(string[] args)
                    {
                        string.Format("This {0[||]} works", "test"); 
                    }     
                }
                """);
        }

        [Fact]
        public async Task TwoPlaceholders()
        {
            await TestDiagnosticMissingAsync("""
                class Program
                {
                    static void Main(string[] args)
                    {
                        string.Format("This {0[||]} {1} works", "test", "also"); 
                    }     
                }
                """);
        }

        [Fact]
        public async Task ThreePlaceholders()
        {
            await TestDiagnosticMissingAsync("""
                class Program
                {
                    static void Main(string[] args)
                    {
                        string.Format("This {0} {1[||]} works {2} ", "test", "also", "well"); 
                    }     
                }
                """);
        }

        [Fact]
        public async Task FourPlaceholders()
        {
            await TestDiagnosticMissingAsync("""
                class Program
                {
                    static void Main(string[] args)
                    {
                        string.Format("This {1} is {2} my {6[||]} test ", "teststring1", "teststring2",
                            "teststring3", "teststring4", "teststring5", "teststring6", "teststring7");
                    }     
                }
                """);
        }

        [Fact]
        public async Task ObjectArray()
        {
            await TestDiagnosticMissingAsync("""
                class Program
                {
                    static void Main(string[] args)
                    {
                        object[] objectArray = { 1.25, "2", "teststring"};
                        string.Format("This {0} {1} {2[||]} works", objectArray); 
                    }     
                }
                """);
        }

        [Fact]
        public async Task MultipleObjectArrays()
        {
            await TestDiagnosticMissingAsync("""
                class Program
                {
                    static void Main(string[] args)
                    {
                        object[] objectArray = { 1.25, "2", "teststring"};
                        string.Format("This {0} {1} {2[||]} works", objectArray, objectArray, objectArray, objectArray); 
                    }     
                }
                """);
        }

        [Fact]
        public async Task IntArray()
        {
            await TestDiagnosticMissingAsync("""
                class Program
                {
                    static void Main(string[] args)
                    {
                        int[] intArray = {1, 2, 3};
                        string.Format("This {0[||]} works", intArray); 
                    }     
                }
                """);
        }

        [Fact]
        public async Task StringArray()
        {
            await TestDiagnosticMissingAsync("""
                class Program
                {
                    static void Main(string[] args)
                    {
                        string[] stringArray = {"test1", "test2", "test3"};
                        string.Format("This {0} {1} {2[||]} works", stringArray); 
                    }     
                }
                """);
        }

        [Fact]
        public async Task LiteralArray()
        {
            await TestDiagnosticMissingAsync("""
                class Program
                {
                    static void Main(string[] args)
                    {
                        string.Format("This {0[||]} {1} {2} {3} works", new [] {"test1", "test2", "test3", "test4"}); 
                    }     
                }
                """);
        }

        [Fact]
        public async Task StringArrayOutOfBounds_NoDiagnostic()
        {
            await TestDiagnosticMissingAsync("""
                class Program
                {
                    static void Main(string[] args)
                    {
                        string[] stringArray = {"test1", "test2"};
                        string.Format("This {0} {1} {2[||]} works", stringArray); 
                    }     
                }
                """);
        }

        [Fact]
        public async Task IFormatProviderAndOnePlaceholder()
        {
            await TestDiagnosticMissingAsync("""
                using System.Globalization; 
                class Program
                {
                    static void Main(string[] args)
                    {
                        testStr = string.Format(new CultureInfo("pt-BR", useUserOverride: false), "The current price is {0[||]:C2} per ounce", 2.45);
                    }     
                }
                """);
        }

        [Fact]
        public async Task IFormatProviderAndTwoPlaceholders()
        {
            await TestDiagnosticMissingAsync("""
                using System.Globalization; 
                class Program
                {
                    static void Main(string[] args)
                    {
                        testStr = string.Format(new CultureInfo("pt-BR", useUserOverride: false), "The current price is {0[||]:C2} per {1} ", 2.45, "ounce");
                    }     
                }
                """);
        }

        [Fact]
        public async Task IFormatProviderAndThreePlaceholders()
        {
            await TestDiagnosticMissingAsync("""
                using System.Globalization; 
                class Program
                {
                    static void Main(string[] args)
                    {
                        testStr = string.Format(new CultureInfo("pt-BR", useUserOverride: false), "The current price is {0} {[||]1} {2} ", 
                            2.45, "per", "ounce");
                    }     
                }
                """);
        }

        [Fact]
        public async Task IFormatProviderAndFourPlaceholders()
        {
            await TestDiagnosticMissingAsync("""
                using System.Globalization; 
                class Program
                {
                    static void Main(string[] args)
                    {
                        testStr = string.Format(new CultureInfo("pt-BR", useUserOverride: false), "The current price is {0} {1[||]} {2} {3} ", 
                            2.45, "per", "ounce", "today only");
                    }     
                }
                """);
        }

        [Fact]
        public async Task IFormatProviderAndObjectArray()
        {
            await TestDiagnosticMissingAsync("""
                using System.Globalization; 
                class Program
                {
                    static void Main(string[] args)
                    {
                        object[] objectArray = { 1.25, "2", "teststring"};
                        string.Format(new CultureInfo("pt-BR", useUserOverride: false), "This {0} {1} {[||]2} works", objectArray); 
                    }     
                }
                """);
        }

        [Fact]
        public async Task WithComma()
        {
            await TestDiagnosticMissingAsync("""
                class Program
                {
                    static void Main(string[] args)
                    {
                        string.Format("{0[||],6}", 34);
                    }     
                }
                """);
        }

        [Fact]
        public async Task WithColon()
        {
            await TestDiagnosticMissingAsync("""
                class Program
                {
                    static void Main(string[] args)
                    {
                        string.Format("{[||]0:N0}", 34);
                    }     
                }
                """);
        }

        [Fact]
        public async Task WithCommaAndColon()
        {
            await TestDiagnosticMissingAsync("""
                class Program
                {
                    static void Main(string[] args)
                    {
                        string.Format("Test {0,[||]15:N0} output", 34);
                    }     
                }
                """);
        }

        [Fact]
        public async Task WithPlaceholderAtBeginning()
        {
            await TestDiagnosticMissingAsync("""
                class Program
                {
                    static void Main(string[] args)
                    {
                        string.Format("{0[||]} is my test case", "This");
                    }     
                }
                """);
        }

        [Fact]
        public async Task WithPlaceholderAtEnd()
        {
            await TestDiagnosticMissingAsync("""
                class Program
                {
                    static void Main(string[] args)
                    {
                        string.Format("This is my {0[||]}", "test");
                    }     
                }
                """);
        }

        [Fact]
        public async Task WithDoubleBraces()
        {
            await TestDiagnosticMissingAsync("""
                class Program
                {
                    static void Main(string[] args)
                    {
                        string.Format(" {{ 2}} This {1[||]} is {2} {{ my {0} test }} ", "teststring1", "teststring2", "teststring3");
                    }     
                }
                """);
        }

        [Fact]
        public async Task WithDoubleBracesAtBeginning()
        {
            await TestDiagnosticMissingAsync("""
                class Program
                {
                    static void Main(string[] args)
                    {
                        string.Format("{{ 2}} This {1[||]} is {2} {{ my {0} test }} ", "teststring1", "teststring2", "teststring3");
                    }     
                }
                """);
        }

        [Fact]
        public async Task WithDoubleBracesAtEnd()
        {
            await TestDiagnosticMissingAsync("""
                class Program
                {
                    static void Main(string[] args)
                    {
                        string.Format(" {{ 2}} This {1[||]} is {2} {{ my {0} test }}", "teststring1", "teststring2", "teststring3");
                    }     
                }
                """);
        }

        [Fact]
        public async Task WithTripleBraces()
        {
            await TestDiagnosticMissingAsync("""
                class Program
                {
                    static void Main(string[] args)
                    {
                        string.Format(" {{{2}} This {1[||]} is {2} {{ my {0} test }}", "teststring1", "teststring2", "teststring3");
                    }     
                }
                """);
        }

        [Fact]
        public async Task NamedParameters()
        {
            await TestDiagnosticMissingAsync("""
                class Program
                {
                    static void Main(string[] args)
                    {
                        string.Format(arg0: "test", arg1: "also", format: "This {0} {[||]1} works"); 
                    }     
                }
                """);
        }

        [Fact]
        public async Task NamedParametersWithIFormatProvider()
        {
            await TestDiagnosticMissingAsync("""
                using System.Globalization;
                class Program
                {
                    static void Main(string[] args)
                    {
                        string.Format(arg0: "test", provider: new CultureInfo("pt-BR", useUserOverride: false), format: "This {0[||]} works"); 
                    }     
                }
                """);
        }

        [Fact]
        public async Task NamespaceAliasForStringClass()
        {
            await TestDiagnosticMissingAsync("""
                using stringAlias = System.String;
                class Program
                {
                    static void Main(string[] args)
                    {
                        stringAlias.Format("This {0[||]} works", "test"); 
                    }     
                }
                """);
        }

        [Fact]
        public async Task MethodCallAsAnArgumentToAnotherMethod()
        {
            await TestDiagnosticMissingAsync("""
                using System.IO;
                class Program
                {
                    static void Main(string[] args)
                    {
                        Console.WriteLine(string.Format(format: "This {0[||]} works", arg0:"test")); 
                    }
                }
                """);
        }

        [Fact]
        public async Task VerbatimMultipleLines()
        {
            await TestDiagnosticMissingAsync("""
                class Program
                {
                    static void Main(string[] args)
                    {
                        string.Format(@"This {0} 
                {1} {2[||]} works", "multiple", "line", "test")); 
                    }
                }
                """);
        }

        [Fact]
        public async Task Interpolated()
        {
            await TestDiagnosticMissingAsync("""
                class Program
                {
                    static void Main(string[] args)
                    {
                        var Name = "Peter";
                        var Age = 30;

                        string.Format($"{Name,[||] 20} is {Age:D3} "); 
                    }
                }
                """);
        }

        [Fact]
        public async Task Empty()
        {
            await TestDiagnosticMissingAsync("""
                class Program
                {
                    static void Main(string[] args)
                    {
                        string.Format("[||]"); 
                    }
                }
                """);
        }

        [Fact]
        public async Task LeftParenOnly()
        {
            await TestDiagnosticMissingAsync("""
                class Program
                {
                    static void Main(string[] args)
                    {
                        string.Format([||]; 
                    }
                }
                """);
        }

        [Fact]
        public async Task ParenthesesOnly()
        {
            await TestDiagnosticMissingAsync("""
                class Program
                {
                    static void Main(string[] args)
                    {
                        string.Format([||]); 
                    }
                }
                """);
        }

        [Fact]
        public async Task EmptyString()
        {
            await TestDiagnosticMissingAsync("""
                class Program
                {
                    static void Main(string[] args)
                    {
                        string.Format("[||]"); 
                    }
                }
                """);
        }

        [Fact]
        public async Task FormatOnly_NoStringDot()
        {
            await TestDiagnosticMissingAsync("""
                using static System.String
                class Program
                {
                    static void Main(string[] args)
                    {
                        Format("[||]"); 
                    }
                }
                """);
        }

        [Fact]
        public async Task NamedParameters_BlankName()
        {
            await TestDiagnosticMissingAsync("""
                class Program
                {
                    static void Main(string[] args)
                    {
                        string.Format( : "value"[||])); 
                    }     
                }
                """);
        }

        [Fact]
        public async Task DuplicateNamedArgs()
        {
            await TestDiagnosticMissingAsync("""
                class Program
                {
                    static void Main(string[] args)
                    {
                        string.Format(format:"This [||] ", format:" test "); 
                    }     
                }
                """);
        }

        [Fact]
        public async Task GenericIdentifier()
        {
            await TestDiagnosticMissingAsync("""
                using System;
                using System.Collections;
                using System.Collections.Generic;
                using System.Text;

                namespace Generics_CSharp
                {
                    public class String<T> 
                    {
                        public void Format<T>(string teststr)
                        {
                            Console.WriteLine(teststr);
                        }
                    }

                    class Generics
                    {
                        static void Main(string[] args)
                        {
                            String<int> testList = new String<int>();
                            testList.Format<int>("Test[||]String");
                        }
                    }
                }
                """);
        }

        [Fact]
        public async Task ClassNamedString()
        {
            await TestDiagnosticMissingAsync("""
                using System;

                namespace System
                {
                    public class String
                    {
                        public static String Format(string format, object arg0) { return new String(); }
                    }
                }

                class C
                {
                    static void Main(string[] args)
                    {
                        Console.WriteLine(String.Format("test {[||]5} ", 1));
                    }
                }
                """);
        }

#if CODE_STYLE
        // Option has no effect on CodeStyle layer CI execution as it is not an editorconfig option.
        [Fact]
        public async Task TestOption_Ignored()
        {
            var source = @"class Program
{
    static void Main(string[] args)
    {
        string.Format(""This [|{1}|] is my test"", ""teststring1"");
    }     
}";
            await TestDiagnosticInfoAsync(
                source,
                options: null,
                diagnosticId: IDEDiagnosticIds.ValidateFormatStringDiagnosticID,
                diagnosticSeverity: DiagnosticSeverity.Info,
                diagnosticMessage: AnalyzersResources.Format_string_contains_invalid_placeholder);
        }
#else
        [Fact]
        public async Task TestOption_Enabled()
        {
            var source = """
                class Program
                {
                    static void Main(string[] args)
                    {
                        string.Format("This [|{1}|] is my test", "teststring1");
                    }     
                }
                """;
            var options = Option(IdeAnalyzerOptionsStorage.ReportInvalidPlaceholdersInStringDotFormatCalls, true);

            await TestDiagnosticInfoAsync(
                source,
                options: null,
                globalOptions: options,
                diagnosticId: IDEDiagnosticIds.ValidateFormatStringDiagnosticID,
                diagnosticSeverity: DiagnosticSeverity.Info,
                diagnosticMessage: AnalyzersResources.Format_string_contains_invalid_placeholder);
        }

        [Fact]
        public async Task TestOption_Disabled()
        {
            var source = """
                class Program
                {
                    static void Main(string[] args)
                    {
                        string.Format("This [|{1}|] is my test", "teststring1");
                    }     
                }
                """;
            var options = Option(IdeAnalyzerOptionsStorage.ReportInvalidPlaceholdersInStringDotFormatCalls, false);

            await TestDiagnosticMissingAsync(source, new TestParameters(globalOptions: options));
        }
#endif
        [Fact]
        public async Task OnePlaceholderOutOfBounds()
        {
            await TestDiagnosticInfoAsync("""
                class Program
                {
                    static void Main(string[] args)
                    {
                        string.Format("This [|{1}|] is my test", "teststring1");
                    }     
                }
                """,
                options: null,
                diagnosticId: IDEDiagnosticIds.ValidateFormatStringDiagnosticID,
                diagnosticSeverity: DiagnosticSeverity.Info,
                diagnosticMessage: AnalyzersResources.Format_string_contains_invalid_placeholder);
        }

        [Fact]
        public async Task TwoPlaceholdersWithOnePlaceholderOutOfBounds()
        {
            await TestDiagnosticInfoAsync("""
                class Program
                {
                    static void Main(string[] args)
                    {
                        string.Format("This [|{2}|] is my test", "teststring1", "teststring2");
                    }     
                }
                """,
                options: null,
                diagnosticId: IDEDiagnosticIds.ValidateFormatStringDiagnosticID,
                diagnosticSeverity: DiagnosticSeverity.Info,
                diagnosticMessage: AnalyzersResources.Format_string_contains_invalid_placeholder);
        }

        [Fact]
        public async Task ThreePlaceholdersWithOnePlaceholderOutOfBounds()
        {
            await TestDiagnosticInfoAsync("""
                class Program
                {
                    static void Main(string[] args)
                    {
                        string.Format("This{0}{1}{2}[|{3}|] is my test", "teststring1", "teststring2", "teststring3");
                    }     
                }
                """,
                options: null,
                diagnosticId: IDEDiagnosticIds.ValidateFormatStringDiagnosticID,
                diagnosticSeverity: DiagnosticSeverity.Info,
                diagnosticMessage: AnalyzersResources.Format_string_contains_invalid_placeholder);
        }

        [Fact]
        public async Task FourPlaceholdersWithOnePlaceholderOutOfBounds()
        {
            await TestDiagnosticInfoAsync("""
                class Program
                {
                    static void Main(string[] args)
                    {
                        string.Format("This{0}{1}{2}{3}[|{4}|] is my test", "teststring1", "teststring2", 
                            "teststring3", "teststring4");
                    }     
                }
                """,
                options: null,
                diagnosticId: IDEDiagnosticIds.ValidateFormatStringDiagnosticID,
                diagnosticSeverity: DiagnosticSeverity.Info,
                diagnosticMessage: AnalyzersResources.Format_string_contains_invalid_placeholder);
        }

        [Fact]
        public async Task iFormatProviderAndOnePlaceholderOutOfBounds()
        {
            await TestDiagnosticInfoAsync("""
                using System.Globalization; 
                class Program
                {
                    static void Main(string[] args)
                    {
                        string.Format(new CultureInfo("pt-BR", useUserOverride: false), "This [|{1}|] is my test", "teststring1");
                    }     
                }
                """,
                options: null,
                diagnosticId: IDEDiagnosticIds.ValidateFormatStringDiagnosticID,
                diagnosticSeverity: DiagnosticSeverity.Info,
                diagnosticMessage: AnalyzersResources.Format_string_contains_invalid_placeholder);
        }

        [Fact]
        public async Task iFormatProviderAndTwoPlaceholdersWithOnePlaceholderOutOfBounds()
        {
            await TestDiagnosticInfoAsync("""
                using System.Globalization; 
                class Program
                {
                    static void Main(string[] args)
                    {
                        string.Format(new CultureInfo("pt-BR", useUserOverride: false), "This [|{2}|] is my test", "teststring1", "teststring2");
                    }     
                }
                """,
                options: null,
                diagnosticId: IDEDiagnosticIds.ValidateFormatStringDiagnosticID,
                diagnosticSeverity: DiagnosticSeverity.Info,
                diagnosticMessage: AnalyzersResources.Format_string_contains_invalid_placeholder);
        }

        [Fact]
        public async Task IFormatProviderAndThreePlaceholdersWithOnePlaceholderOutOfBounds()
        {
            await TestDiagnosticInfoAsync("""
                using System.Globalization; 
                class Program
                {
                    static void Main(string[] args)
                    {
                        string.Format(new CultureInfo("pt-BR", useUserOverride: false), "This{0}{1}{2}[|{3}|] is my test", "teststring1", 
                            "teststring2", "teststring3");
                    }     
                }
                """,
                options: null,
                diagnosticId: IDEDiagnosticIds.ValidateFormatStringDiagnosticID,
                diagnosticSeverity: DiagnosticSeverity.Info,
                diagnosticMessage: AnalyzersResources.Format_string_contains_invalid_placeholder);
        }

        [Fact]
        public async Task IFormatProviderAndFourPlaceholdersWithOnePlaceholderOutOfBounds()
        {
            await TestDiagnosticInfoAsync("""
                using System.Globalization; 
                class Program
                {
                    static void Main(string[] args)
                    {
                        string.Format(new CultureInfo("pt-BR", useUserOverride: false), "This{0}{1}{2}{3}[|{4}|] is my test", "teststring1", 
                            "teststring2", "teststring3", "teststring4");
                    }     
                }
                """,
                options: null,
                diagnosticId: IDEDiagnosticIds.ValidateFormatStringDiagnosticID,
                diagnosticSeverity: DiagnosticSeverity.Info,
                diagnosticMessage: AnalyzersResources.Format_string_contains_invalid_placeholder);
        }

        [Fact]
        public async Task PlaceholderAtBeginningWithOnePlaceholderOutOfBounds()
        {
            await TestDiagnosticInfoAsync("""
                class Program
                {
                    static void Main(string[] args)
                    {
                        string.Format( "[|{1}|]is my test", "teststring1");
                    }     
                }
                """,
                options: null,
                diagnosticId: IDEDiagnosticIds.ValidateFormatStringDiagnosticID,
                diagnosticSeverity: DiagnosticSeverity.Info,
                diagnosticMessage: AnalyzersResources.Format_string_contains_invalid_placeholder);
        }

        [Fact]
        public async Task PlaceholderAtEndWithOnePlaceholderOutOfBounds()
        {
            await TestDiagnosticInfoAsync("""
                class Program
                {
                    static void Main(string[] args)
                    {
                        string.Format( "is my test [|{2}|]", "teststring1", "teststring2");
                    }     
                }
                """,
                options: null,
                diagnosticId: IDEDiagnosticIds.ValidateFormatStringDiagnosticID,
                diagnosticSeverity: DiagnosticSeverity.Info,
                diagnosticMessage: AnalyzersResources.Format_string_contains_invalid_placeholder);
        }

        [Fact]
        public async Task DoubleBracesAtBeginningWithOnePlaceholderOutOfBounds()
        {
            await TestDiagnosticInfoAsync("""
                class Program
                {
                    static void Main(string[] args)
                    {
                        string.Format( "}}is my test [|{2}|]", "teststring1", "teststring2");
                    }     
                }
                """,
                options: null,
                diagnosticId: IDEDiagnosticIds.ValidateFormatStringDiagnosticID,
                diagnosticSeverity: DiagnosticSeverity.Info,
                diagnosticMessage: AnalyzersResources.Format_string_contains_invalid_placeholder);
        }

        [Fact]
        public async Task DoubleBracesAtEndWithOnePlaceholderOutOfBounds()
        {
            await TestDiagnosticInfoAsync("""
                class Program
                {
                    static void Main(string[] args)
                    {
                        string.Format( "is my test [|{2}|]{{", "teststring1", "teststring2");
                    }     
                }
                """,
                options: null,
                diagnosticId: IDEDiagnosticIds.ValidateFormatStringDiagnosticID,
                diagnosticSeverity: DiagnosticSeverity.Info,
                diagnosticMessage: AnalyzersResources.Format_string_contains_invalid_placeholder);
        }

        [Fact]
        public async Task NamedParametersOneOutOfBounds()
        {
            await TestDiagnosticInfoAsync("""
                using System.Globalization; 
                class Program
                {
                    static void Main(string[] args)
                    {
                        string.Format(arg0: "test", arg1: "also", format: "This {0} [|{2}|] works");
                    }     
                }
                """,
                options: null,
                diagnosticId: IDEDiagnosticIds.ValidateFormatStringDiagnosticID,
                diagnosticSeverity: DiagnosticSeverity.Info,
                diagnosticMessage: AnalyzersResources.Format_string_contains_invalid_placeholder);
        }

        [Fact]
        public async Task NamedParametersWithIFormatProviderOneOutOfBounds()
        {
            await TestDiagnosticInfoAsync("""
                using System.Globalization; 
                class Program
                {
                    static void Main(string[] args)
                    {
                        string.Format(arg0: "test", arg1: "also", format: "This {0} [|{2}|] works", provider: new CultureInfo("pt-BR", useUserOverride: false))
                    }     
                }
                """,
                options: null,
                diagnosticId: IDEDiagnosticIds.ValidateFormatStringDiagnosticID,
                diagnosticSeverity: DiagnosticSeverity.Info,
                diagnosticMessage: AnalyzersResources.Format_string_contains_invalid_placeholder);
        }

        [Fact]
        public async Task FormatOnly_NoStringDot_OneOutOfBounds()
        {
            await TestDiagnosticInfoAsync("""
                using static System.String
                class Program
                {
                    static void Main(string[] args)
                    {
                        Format("This {0} [|{2}|] squiggles", "test", "gets");
                    }     
                }
                """,
                options: null,
                diagnosticId: IDEDiagnosticIds.ValidateFormatStringDiagnosticID,
                diagnosticSeverity: DiagnosticSeverity.Info,
                diagnosticMessage: AnalyzersResources.Format_string_contains_invalid_placeholder);
        }

        [Fact]
        public async Task Net45TestOutOfBounds()
        {
            var input = """
                            < Workspace >
                                < Project Language = "C#" AssemblyName="Assembly1" CommonReferencesNet45="true"> 
                 <Document FilePath="CurrentDocument.cs"><![CDATA[
                using System.Globalization; 
                class Program
                {
                    static void Main(string[] args)
                    {
                        string.Format("This [|{1}|] is my test", "teststring1");
                    }     
                }
                ]]>
                        </Document>
                                </Project>
                            </Workspace>
                """;
            await TestDiagnosticInfoAsync(input,
                options: null,
                diagnosticId: IDEDiagnosticIds.ValidateFormatStringDiagnosticID,
                diagnosticSeverity: DiagnosticSeverity.Info,
                diagnosticMessage: AnalyzersResources.Format_string_contains_invalid_placeholder);
        }

        [Fact]
        public async Task VerbatimMultipleLinesPlaceholderOutOfBounds()
        {
            await TestDiagnosticInfoAsync("""
                class Program
                {
                    static void Main(string[] args)
                    {
                        string.Format(@"This {0} 
                {1} [|{3}|] works", "multiple", "line", "test")); 
                    }
                }
                """,
                options: null,
                diagnosticId: IDEDiagnosticIds.ValidateFormatStringDiagnosticID,
                diagnosticSeverity: DiagnosticSeverity.Info,
                diagnosticMessage: AnalyzersResources.Format_string_contains_invalid_placeholder);
        }

        [Fact]
        public async Task IntArrayOutOfBounds()
        {
            await TestDiagnosticInfoAsync("""
                class Program
                {
                    static void Main(string[] args)
                    {
                        int[] intArray = {1, 2};
                        string.Format("This {0} [|{1}|] {2} works", intArray); 
                    }     
                }
                """,
                options: null,
                diagnosticId: IDEDiagnosticIds.ValidateFormatStringDiagnosticID,
                diagnosticSeverity: DiagnosticSeverity.Info,
                diagnosticMessage: AnalyzersResources.Format_string_contains_invalid_placeholder);
        }

        [Fact]
        public async Task FirstPlaceholderOutOfBounds()
        {
            await TestDiagnosticInfoAsync("""
                class Program
                {
                    static void Main(string[] args)
                    {
                        int[] intArray = {1, 2};
                        string.Format("This {0} [|{1}|] {2} works", "TestString"); 
                    }     
                }
                """,
                options: null,
                diagnosticId: IDEDiagnosticIds.ValidateFormatStringDiagnosticID,
                diagnosticSeverity: DiagnosticSeverity.Info,
                diagnosticMessage: AnalyzersResources.Format_string_contains_invalid_placeholder);
        }

        [Fact]
        public async Task SecondPlaceholderOutOfBounds()
        {
            await TestDiagnosticInfoAsync("""
                class Program
                {
                    static void Main(string[] args)
                    {
                        int[] intArray = {1, 2};
                        string.Format("This {0} {1} [|{2}|] works", "TestString"); 
                    }     
                }
                """,
                options: null,
                diagnosticId: IDEDiagnosticIds.ValidateFormatStringDiagnosticID,
                diagnosticSeverity: DiagnosticSeverity.Info,
                diagnosticMessage: AnalyzersResources.Format_string_contains_invalid_placeholder);
        }

        [Fact]
        public async Task FirstOfMultipleSameNamedPlaceholderOutOfBounds()
        {
            await TestDiagnosticInfoAsync("""
                class Program
                {
                    static void Main(string[] args)
                    {
                        int[] intArray = {1, 2};
                        string.Format("This {0} [|{2}|] {2} works", "TestString"); 
                    }     
                }
                """,
                options: null,
                diagnosticId: IDEDiagnosticIds.ValidateFormatStringDiagnosticID,
                diagnosticSeverity: DiagnosticSeverity.Info,
                diagnosticMessage: AnalyzersResources.Format_string_contains_invalid_placeholder);
        }

        [Fact]
        public async Task SecondOfMultipleSameNamedPlaceholderOutOfBounds()
        {
            await TestDiagnosticInfoAsync("""
                class Program
                {
                    static void Main(string[] args)
                    {
                        int[] intArray = {1, 2};
                        string.Format("This {0} {2} [|{2}|] works", "TestString"); 
                    }     
                }
                """,
                options: null,
                diagnosticId: IDEDiagnosticIds.ValidateFormatStringDiagnosticID,
                diagnosticSeverity: DiagnosticSeverity.Info,
                diagnosticMessage: AnalyzersResources.Format_string_contains_invalid_placeholder);
        }

        [Fact]
        public async Task EmptyPlaceholder()
        {
            await TestDiagnosticInfoAsync("""
                class Program
                {
                    static void Main(string[] args)
                    {
                        int[] intArray = {1, 2};
                        string.Format("This [|{}|] ", "TestString"); 
                    }     
                }
                """,
                options: null,
                diagnosticId: IDEDiagnosticIds.ValidateFormatStringDiagnosticID,
                diagnosticSeverity: DiagnosticSeverity.Info,
                diagnosticMessage: AnalyzersResources.Format_string_contains_invalid_placeholder);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/29398")]
        public async Task LocalFunctionNamedFormat()
        {
            await TestDiagnosticMissingAsync("""
                public class C
                {
                    public void M()
                    {
                        Forma[||]t();
                        void Format() { }
                    }
                }
                """);
        }
    }
}
