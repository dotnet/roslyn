// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.ValidateFormatString;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Diagnostics;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.ValidateFormatString;
using Roslyn.Test.Utilities;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.ValidateFormatString;

[Trait(Traits.Feature, Traits.Features.ValidateFormatString)]
public sealed class ValidateFormatStringTests : AbstractCSharpDiagnosticProviderBasedUserDiagnosticTest_NoEditor
{
    public ValidateFormatStringTests(ITestOutputHelper logger)
       : base(logger)
    {
    }

    internal override (DiagnosticAnalyzer, CodeFixProvider?) CreateDiagnosticProviderAndFixer(Workspace workspace)
        => (new CSharpValidateFormatStringDiagnosticAnalyzer(), null);

    [Fact]
    public Task OnePlaceholder()
        => TestDiagnosticMissingAsync("""
            class Program
            {
                static void Main(string[] args)
                {
                    string.Format("This {0[||]} works", "test"); 
                }     
            }
            """);

    [Fact]
    public Task TwoPlaceholders()
        => TestDiagnosticMissingAsync("""
            class Program
            {
                static void Main(string[] args)
                {
                    string.Format("This {0[||]} {1} works", "test", "also"); 
                }     
            }
            """);

    [Fact]
    public Task ThreePlaceholders()
        => TestDiagnosticMissingAsync("""
            class Program
            {
                static void Main(string[] args)
                {
                    string.Format("This {0} {1[||]} works {2} ", "test", "also", "well"); 
                }     
            }
            """);

    [Fact]
    public Task FourPlaceholders()
        => TestDiagnosticMissingAsync("""
            class Program
            {
                static void Main(string[] args)
                {
                    string.Format("This {1} is {2} my {6[||]} test ", "teststring1", "teststring2",
                        "teststring3", "teststring4", "teststring5", "teststring6", "teststring7");
                }     
            }
            """);

    [Fact]
    public Task ObjectArray()
        => TestDiagnosticMissingAsync("""
            class Program
            {
                static void Main(string[] args)
                {
                    object[] objectArray = { 1.25, "2", "teststring"};
                    string.Format("This {0} {1} {2[||]} works", objectArray); 
                }     
            }
            """);

    [Fact]
    public Task MultipleObjectArrays()
        => TestDiagnosticMissingAsync("""
            class Program
            {
                static void Main(string[] args)
                {
                    object[] objectArray = { 1.25, "2", "teststring"};
                    string.Format("This {0} {1} {2[||]} works", objectArray, objectArray, objectArray, objectArray); 
                }     
            }
            """);

    [Fact]
    public Task IntArray()
        => TestDiagnosticMissingAsync("""
            class Program
            {
                static void Main(string[] args)
                {
                    int[] intArray = {1, 2, 3};
                    string.Format("This {0[||]} works", intArray); 
                }     
            }
            """);

    [Fact]
    public Task StringArray()
        => TestDiagnosticMissingAsync("""
            class Program
            {
                static void Main(string[] args)
                {
                    string[] stringArray = {"test1", "test2", "test3"};
                    string.Format("This {0} {1} {2[||]} works", stringArray); 
                }     
            }
            """);

    [Fact]
    public Task LiteralArray()
        => TestDiagnosticMissingAsync("""
            class Program
            {
                static void Main(string[] args)
                {
                    string.Format("This {0[||]} {1} {2} {3} works", new [] {"test1", "test2", "test3", "test4"}); 
                }     
            }
            """);

    [Fact]
    public Task StringArrayOutOfBounds_NoDiagnostic()
        => TestDiagnosticMissingAsync("""
            class Program
            {
                static void Main(string[] args)
                {
                    string[] stringArray = {"test1", "test2"};
                    string.Format("This {0} {1} {2[||]} works", stringArray); 
                }     
            }
            """);

    [Fact]
    public Task IFormatProviderAndOnePlaceholder()
        => TestDiagnosticMissingAsync("""
            using System.Globalization; 
            class Program
            {
                static void Main(string[] args)
                {
                    testStr = string.Format(new CultureInfo("pt-BR", useUserOverride: false), "The current price is {0[||]:C2} per ounce", 2.45);
                }     
            }
            """);

    [Fact]
    public Task IFormatProviderAndTwoPlaceholders()
        => TestDiagnosticMissingAsync("""
            using System.Globalization; 
            class Program
            {
                static void Main(string[] args)
                {
                    testStr = string.Format(new CultureInfo("pt-BR", useUserOverride: false), "The current price is {0[||]:C2} per {1} ", 2.45, "ounce");
                }     
            }
            """);

    [Fact]
    public Task IFormatProviderAndThreePlaceholders()
        => TestDiagnosticMissingAsync("""
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

    [Fact]
    public Task IFormatProviderAndFourPlaceholders()
        => TestDiagnosticMissingAsync("""
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

    [Fact]
    public Task IFormatProviderAndObjectArray()
        => TestDiagnosticMissingAsync("""
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

    [Fact]
    public Task WithComma()
        => TestDiagnosticMissingAsync("""
            class Program
            {
                static void Main(string[] args)
                {
                    string.Format("{0[||],6}", 34);
                }     
            }
            """);

    [Fact]
    public Task WithColon()
        => TestDiagnosticMissingAsync("""
            class Program
            {
                static void Main(string[] args)
                {
                    string.Format("{[||]0:N0}", 34);
                }     
            }
            """);

    [Fact]
    public Task WithCommaAndColon()
        => TestDiagnosticMissingAsync("""
            class Program
            {
                static void Main(string[] args)
                {
                    string.Format("Test {0,[||]15:N0} output", 34);
                }     
            }
            """);

    [Fact]
    public Task WithPlaceholderAtBeginning()
        => TestDiagnosticMissingAsync("""
            class Program
            {
                static void Main(string[] args)
                {
                    string.Format("{0[||]} is my test case", "This");
                }     
            }
            """);

    [Fact]
    public Task WithPlaceholderAtEnd()
        => TestDiagnosticMissingAsync("""
            class Program
            {
                static void Main(string[] args)
                {
                    string.Format("This is my {0[||]}", "test");
                }     
            }
            """);

    [Fact]
    public Task WithDoubleBraces()
        => TestDiagnosticMissingAsync("""
            class Program
            {
                static void Main(string[] args)
                {
                    string.Format(" {{ 2}} This {1[||]} is {2} {{ my {0} test }} ", "teststring1", "teststring2", "teststring3");
                }     
            }
            """);

    [Fact]
    public Task WithDoubleBracesAtBeginning()
        => TestDiagnosticMissingAsync("""
            class Program
            {
                static void Main(string[] args)
                {
                    string.Format("{{ 2}} This {1[||]} is {2} {{ my {0} test }} ", "teststring1", "teststring2", "teststring3");
                }     
            }
            """);

    [Fact]
    public Task WithDoubleBracesAtEnd()
        => TestDiagnosticMissingAsync("""
            class Program
            {
                static void Main(string[] args)
                {
                    string.Format(" {{ 2}} This {1[||]} is {2} {{ my {0} test }}", "teststring1", "teststring2", "teststring3");
                }     
            }
            """);

    [Fact]
    public Task WithTripleBraces()
        => TestDiagnosticMissingAsync("""
            class Program
            {
                static void Main(string[] args)
                {
                    string.Format(" {{{2}} This {1[||]} is {2} {{ my {0} test }}", "teststring1", "teststring2", "teststring3");
                }     
            }
            """);

    [Fact]
    public Task NamedParameters()
        => TestDiagnosticMissingAsync("""
            class Program
            {
                static void Main(string[] args)
                {
                    string.Format(arg0: "test", arg1: "also", format: "This {0} {[||]1} works"); 
                }     
            }
            """);

    [Fact]
    public Task NamedParametersWithIFormatProvider()
        => TestDiagnosticMissingAsync("""
            using System.Globalization;
            class Program
            {
                static void Main(string[] args)
                {
                    string.Format(arg0: "test", provider: new CultureInfo("pt-BR", useUserOverride: false), format: "This {0[||]} works"); 
                }     
            }
            """);

    [Fact]
    public Task NamespaceAliasForStringClass()
        => TestDiagnosticMissingAsync("""
            using stringAlias = System.String;
            class Program
            {
                static void Main(string[] args)
                {
                    stringAlias.Format("This {0[||]} works", "test"); 
                }     
            }
            """);

    [Fact]
    public Task MethodCallAsAnArgumentToAnotherMethod()
        => TestDiagnosticMissingAsync("""
            using System.IO;
            class Program
            {
                static void Main(string[] args)
                {
                    Console.WriteLine(string.Format(format: "This {0[||]} works", arg0:"test")); 
                }
            }
            """);

    [Fact]
    public Task VerbatimMultipleLines()
        => TestDiagnosticMissingAsync("""
            class Program
            {
                static void Main(string[] args)
                {
                    string.Format(@"This {0} 
            {1} {2[||]} works", "multiple", "line", "test")); 
                }
            }
            """);

    [Fact]
    public Task Interpolated()
        => TestDiagnosticMissingAsync("""
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

    [Fact]
    public Task Empty()
        => TestDiagnosticMissingAsync("""
            class Program
            {
                static void Main(string[] args)
                {
                    string.Format("[||]"); 
                }
            }
            """);

    [Fact]
    public Task LeftParenOnly()
        => TestDiagnosticMissingAsync("""
            class Program
            {
                static void Main(string[] args)
                {
                    string.Format([||]; 
                }
            }
            """);

    [Fact]
    public Task ParenthesesOnly()
        => TestDiagnosticMissingAsync("""
            class Program
            {
                static void Main(string[] args)
                {
                    string.Format([||]); 
                }
            }
            """);

    [Fact]
    public Task EmptyString()
        => TestDiagnosticMissingAsync("""
            class Program
            {
                static void Main(string[] args)
                {
                    string.Format("[||]"); 
                }
            }
            """);

    [Fact]
    public Task FormatOnly_NoStringDot()
        => TestDiagnosticMissingAsync("""
            using static System.String
            class Program
            {
                static void Main(string[] args)
                {
                    Format("[||]"); 
                }
            }
            """);

    [Fact]
    public Task NamedParameters_BlankName()
        => TestDiagnosticMissingAsync("""
            class Program
            {
                static void Main(string[] args)
                {
                    string.Format( : "value"[||])); 
                }     
            }
            """);

    [Fact]
    public Task DuplicateNamedArgs()
        => TestDiagnosticMissingAsync("""
            class Program
            {
                static void Main(string[] args)
                {
                    string.Format(format:"This [||] ", format:" test "); 
                }     
            }
            """);

    [Fact]
    public Task GenericIdentifier()
        => TestDiagnosticMissingAsync("""
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

    [Fact]
    public Task ClassNamedString()
        => TestDiagnosticMissingAsync("""
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

    [Fact]
    public async Task TestOption_Enabled()
    {
        var options = Option(FormatStringValidationOptionStorage.ReportInvalidPlaceholdersInStringDotFormatCalls, true);

        await TestDiagnosticInfoAsync(
            """
            class Program
            {
                static void Main(string[] args)
                {
                    string.Format("This [|{1}|] is my test", "teststring1");
                }     
            }
            """,
            options: options,
            diagnosticId: IDEDiagnosticIds.ValidateFormatStringDiagnosticID,
            diagnosticSeverity: DiagnosticSeverity.Info,
            diagnosticMessage: AnalyzersResources.Format_string_contains_invalid_placeholder);
    }

    [Fact]
    public async Task TestOption_Disabled()
    {
        var options = Option(FormatStringValidationOptionStorage.ReportInvalidPlaceholdersInStringDotFormatCalls, false);

        await TestDiagnosticMissingAsync("""
            class Program
            {
                static void Main(string[] args)
                {
                    string.Format("This [|{1}|] is my test", "teststring1");
                }     
            }
            """, new TestParameters(options: options));
    }

    [Fact]
    public Task OnePlaceholderOutOfBounds()
        => TestDiagnosticInfoAsync("""
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

    [Fact]
    public Task TwoPlaceholdersWithOnePlaceholderOutOfBounds()
        => TestDiagnosticInfoAsync("""
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

    [Fact]
    public Task ThreePlaceholdersWithOnePlaceholderOutOfBounds()
        => TestDiagnosticInfoAsync("""
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

    [Fact]
    public Task FourPlaceholdersWithOnePlaceholderOutOfBounds()
        => TestDiagnosticInfoAsync("""
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

    [Fact]
    public Task iFormatProviderAndOnePlaceholderOutOfBounds()
        => TestDiagnosticInfoAsync("""
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

    [Fact]
    public Task iFormatProviderAndTwoPlaceholdersWithOnePlaceholderOutOfBounds()
        => TestDiagnosticInfoAsync("""
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

    [Fact]
    public Task IFormatProviderAndThreePlaceholdersWithOnePlaceholderOutOfBounds()
        => TestDiagnosticInfoAsync("""
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

    [Fact]
    public Task IFormatProviderAndFourPlaceholdersWithOnePlaceholderOutOfBounds()
        => TestDiagnosticInfoAsync("""
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

    [Fact]
    public Task PlaceholderAtBeginningWithOnePlaceholderOutOfBounds()
        => TestDiagnosticInfoAsync("""
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

    [Fact]
    public Task PlaceholderAtEndWithOnePlaceholderOutOfBounds()
        => TestDiagnosticInfoAsync("""
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

    [Fact]
    public Task DoubleBracesAtBeginningWithOnePlaceholderOutOfBounds()
        => TestDiagnosticInfoAsync("""
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

    [Fact]
    public Task DoubleBracesAtEndWithOnePlaceholderOutOfBounds()
        => TestDiagnosticInfoAsync("""
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

    [Fact]
    public Task NamedParametersOneOutOfBounds()
        => TestDiagnosticInfoAsync("""
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

    [Fact]
    public Task NamedParametersWithIFormatProviderOneOutOfBounds()
        => TestDiagnosticInfoAsync("""
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

    [Fact]
    public Task FormatOnly_NoStringDot_OneOutOfBounds()
        => TestDiagnosticInfoAsync("""
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

    [Fact]
    public Task Net45TestOutOfBounds()
        => TestDiagnosticInfoAsync("""
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
            """,
            options: null,
            diagnosticId: IDEDiagnosticIds.ValidateFormatStringDiagnosticID,
            diagnosticSeverity: DiagnosticSeverity.Info,
            diagnosticMessage: AnalyzersResources.Format_string_contains_invalid_placeholder);

    [Fact]
    public Task VerbatimMultipleLinesPlaceholderOutOfBounds()
        => TestDiagnosticInfoAsync("""
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

    [Fact]
    public Task IntArrayOutOfBounds()
        => TestDiagnosticInfoAsync("""
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

    [Fact]
    public Task FirstPlaceholderOutOfBounds()
        => TestDiagnosticInfoAsync("""
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

    [Fact]
    public Task SecondPlaceholderOutOfBounds()
        => TestDiagnosticInfoAsync("""
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

    [Fact]
    public Task FirstOfMultipleSameNamedPlaceholderOutOfBounds()
        => TestDiagnosticInfoAsync("""
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

    [Fact]
    public Task SecondOfMultipleSameNamedPlaceholderOutOfBounds()
        => TestDiagnosticInfoAsync("""
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

    [Fact]
    public Task EmptyPlaceholder()
        => TestDiagnosticInfoAsync("""
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

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/29398")]
    public Task LocalFunctionNamedFormat()
        => TestDiagnosticMissingAsync("""
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
