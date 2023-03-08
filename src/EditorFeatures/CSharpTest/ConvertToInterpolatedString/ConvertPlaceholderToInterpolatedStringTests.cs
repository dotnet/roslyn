// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.CSharp.ConvertToInterpolatedString;
using Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.CodeRefactorings;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.ConvertToInterpolatedString
{
    [Trait(Traits.Feature, Traits.Features.CodeActionsConvertToInterpolatedString)]
    public class ConvertPlaceholderToInterpolatedStringTests : AbstractCSharpCodeActionTest
    {
        protected override CodeRefactoringProvider CreateCodeRefactoringProvider(Workspace workspace, TestParameters parameters)
            => new CSharpConvertPlaceholderToInterpolatedStringRefactoringProvider();

        private static readonly string[] CompositeFormattedMethods = new[]
        {
            "Console.Write",
            "Console.WriteLine",
            "Debug.WriteLine",
            "Debug.Print",
            "Trace.TraceError",
            "Trace.TraceWarning",
            "Trace.TraceInformation",
        };

        public static IEnumerable<object[]> InvocationData
        {
            get
            {
                // Every API so far starts to use a params object after 4 paramters following the formatted 
                const int ParametersToCheck = 4;

                // string.Format gets replaced with just the interpolated string
                for (var i = 1; i <= ParametersToCheck; i++)
                {
                    var invocation = $"string.Format({MakeFormattedParameters(i)})";
                    var result = $"${MakeInterpolatedString(i)}";
                    yield return new[] { invocation, result };
                }

                // Composite Formatted methods do not get replaced, but instead
                // take the string as the only parameter
                for (var i = 1; i <= ParametersToCheck; i++)
                {
                    foreach (var function in CompositeFormattedMethods)
                    {
                        var invocation = $"{function}({MakeFormattedParameters(i)})";
                        var result = $"{function}(${MakeInterpolatedString(i)})";
                        yield return new[] { invocation, result };
                    }
                }

                // Makes a string of form "{0} {1}..."
                static string MakeInterpolatedString(int numberOfParameters)
                {
                    var interpolatedString = "\"";

                    for (var i = 0; i < numberOfParameters; i++)
                    {
                        interpolatedString += $"{{{i}}}";
                    }

                    return interpolatedString + "\"";
                }

                // Makes a string of form "{0} {1}..., 0, 1, ..."
                static string MakeFormattedParameters(int numberOfParameters)
                {
                    var formatString = MakeInterpolatedString(numberOfParameters);
                    return formatString + "," + string.Join(",", Enumerable.Range(0, numberOfParameters));

                }
            }
        }

        [Theory]
        [MemberData(nameof(InvocationData))]
        public async Task TestInvocationSubstitution(string before, string after)
        {
            await TestInRegularAndScriptAsync(
@$"using System;
using System.Diagnostics;

class T
{{
    void M()
    {{
        [|{before}|];
    }}
}}",
@$"using System;
using System.Diagnostics;

class T
{{
    void M()
    {{
        {after};
    }}
}}");
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/55053")]
        public async Task TestMissing_ConsoleWriteLine()
        {
            await TestMissingAsync(
@"using System;

class T
{
    void M()
    {
        var i = 25;
        [|Console.WriteLine(GetString(), i)|];
    }

    string GetString() => """";
}");
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/55053")]
        public async Task TestMissing_ConsoleWrite()
        {
            await TestMissingAsync(
@"using System;

class T
{
    void M()
    {
        var i = 25;
        [|Console.Write(GetString(), i)|];
    }

    string GetString() => """";
}");
        }

        [Fact]
        public async Task TestItemOrdering()
        {
            await TestInRegularAndScriptAsync(
@"using System;

class T
{
    void M()
    {
        var a = [|string.Format(""{0}{1}{2}"", 1, 2, 3)|];
    }
}",
@"using System;

class T
{
    void M()
    {
        var a = $""{1}{2}{3}"";
    }
}");
        }

        [Fact]
        public async Task TestItemOrdering2()
        {
            await TestInRegularAndScriptAsync(
@"using System;

class T
{
    void M()
    {
        var a = [|string.Format(""{0}{2}{1}"", 1, 2, 3)|];
    }
}",
@"using System;

class T
{
    void M()
    {
        var a = $""{1}{3}{2}"";
    }
}");
        }

        [Fact]
        public async Task TestItemOrdering3()
        {
            // Missing as we have arguments we don't know what to do with here.  Likely a bug in user code that needs
            // fixing first.
            await TestMissingAsync(
@"using System;

class T
{
    void M()
    {
        var a = [|string.Format(""{0}{0}{0}"", 1, 2, 3)|];
    }
}");
        }

        [Fact]
        public async Task TestItemOrdering4()
        {
            await TestInRegularAndScriptAsync(
@"using System;

class T
{
    void M()
    {
        var a = [|string.Format(""{0}{1}{2}{0}{1}{2}"", 1, 2, 3)|];
    }
}",
@"using System;

class T
{
    void M()
    {
        var a = $""{1}{2}{3}{1}{2}{3}"";
    }
}");
        }

        [Fact]
        public async Task TestNotWithMissingCurly1()
        {
            await TestMissingAsync(
@"using System;

class T
{
    void M()
    {
        var a = [|string.Format(""0}{"", 1)|];
    }
}");
        }

        [Fact]
        public async Task TestNotWithMissingCurly2()
        {
            await TestMissingAsync(
@"using System;

class T
{
    void M()
    {
        var a = [|string.Format(""0{"", 1)|];
    }
}");
        }

        [Fact]
        public async Task TestNotWithIncorrectSyntax1()
        {
            await TestMissingAsync(
@"using System;

class T
{
    void M()
    {
        var a = [|string.Format(""{:0}"", 1)|];
    }
}");
        }

        [Fact]
        public async Task TestNotWithMatchWithFollowingNumber()
        {
            // want to make sure that `{1` is not matched as `{1}`
            await TestMissingAsync(
@"using System;

class T
{
    void M()
    {
        var a = [|string.Format(""{0}{12}"", 1, 2)|];
    }
}");
        }

        [Fact]
        public async Task TestNotWithStringThatParsesWrongAsInterpolation()
        {
            await TestMissingAsync(
@"using System;

class T
{
    void M()
    {
        var a = [|string.Format(""{0}{a +}"", 1)|];
    }
}");
        }

        [Fact]
        public async Task TestItemOutsideRange()
        {
            // Missing as the format string refers to parameters that aren't provided.
            await TestMissingAsync(
@"using System;

class T
{
    void M()
    {
        var a = [|string.Format(""{4}{5}{6}"", 1, 2, 3)|];
    }
}");
        }

        [Fact]
        public async Task TestItemDoNotHaveCast()
        {
            await TestInRegularAndScriptAsync(
@"using System;

class T
{
    void M()
    {
        var a = [|string.Format(""{0}{1}{2}"", 0.5, ""Hello"", 3)|];
    }
}",
@"using System;

class T
{
    void M()
    {
        var a = $""{0.5}{""Hello""}{3}"";
    }
}");
        }

        [Fact]
        public async Task TestItemWithSyntaxErrorDoesHaveCast()
        {
            await TestInRegularAndScriptAsync(
@"using System;

class T
{
    void M()
    {
        var a = [|string.Format(""{0}"", new object)|];
    }
}",
@"using System;

class T
{
    void M()
    {
        var a = $""{(object)new object}"";
    }
}");
        }

        [Fact]
        public async Task TestItemWithoutSyntaxErrorDoesNotHaveCast()
        {
            await TestInRegularAndScriptAsync(
@"using System;

class T
{
    void M()
    {
        var a = [|string.Format(""{0}"", new object())|];
    }
}",
@"using System;

class T
{
    void M()
    {
        var a = $""{new object()}"";
    }
}");
        }

        [Fact]
        public async Task TestParenthesisAddedForTernaryExpression()
        {
            await TestInRegularAndScriptAsync(
@"using System;

class T
{
    void M()
    {
        var a = [|string.Format(""{0}"", true ? ""Yes"" : ""No"")|];
    }
}",
@"using System;

class T
{
    void M()
    {
        var a = $""{(true ? ""Yes"" : ""No"")}"";
    }
}");
        }

        [Fact]
        public async Task TestDoesNotAddDoubleParenthesisForTernaryExpression()
        {
            await TestInRegularAndScriptAsync(
@"using System;

class T
{
    void M()
    {
        var a = [|string.Format(""{0}"", (true ? ""Yes"" : ""No""))|];
    }
}",
@"using System;

class T
{
    void M()
    {
        var a = $""{(true ? ""Yes"" : ""No"")}"";
    }
}");
        }

        [Fact]
        public async Task TestMultiLineExpression()
        {
            await TestInRegularAndScriptAsync(
@"using System;

class T
{
    void M()
    {
        var a = [|string.Format(
            ""{0}"",
            true ? ""Yes"" : false as object)|];
    }
}",
@"using System;

class T
{
    void M()
    {
        var a = $""{(true ? ""Yes"" : false as object)}"";
    }
}");
        }

        [Fact]
        public async Task TestFormatSpecifiers()
        {
            await TestInRegularAndScriptAsync(
@"using System;

class T
{
    void M()
    {
        Decimal pricePerOunce = 17.36m;
        String s = [|String.Format(""The current price is { 0:C2} per ounce."",
                                 pricePerOunce)|];
    }
}",
@"using System;

class T
{
    void M()
    {
        Decimal pricePerOunce = 17.36m;
        String s = $""The current price is {pricePerOunce:C2} per ounce."";
    }
}");
        }

        [Fact]
        public async Task TestFormatSpecifiers2()
        {
            await TestInRegularAndScriptAsync(
@"using System;

class T
{
    void M()
    {
        string s = [|String.Format(""It is now {0:d} at {0:t}"", DateTime.Now)|];
    }
}",
@"using System;

class T
{
    void M()
    {
        string s = $""It is now {DateTime.Now:d} at {DateTime.Now:t}"";
    }
}");
        }

        [Fact]
        public async Task TestFormatSpecifiers3()
        {
            await TestInRegularAndScriptAsync(
@"using System;
class T
{
    void M()
    {
        int[] years = { 2013, 2014, 2015 };
        int[] population = { 1025632, 1105967, 1148203 };
        String s = String.Format(""{0,6} {1,15}\n\n"", ""Year"", ""Population"");
        for (int index = 0; index < years.Length; index++)
            s += [|String.Format(""{0,6} {1,15:N0}\n"",
                               years[index], population[index])|];
    }
}",
@"using System;
class T
{
    void M()
    {
        int[] years = { 2013, 2014, 2015 };
        int[] population = { 1025632, 1105967, 1148203 };
        String s = String.Format(""{0,6} {1,15}\n\n"", ""Year"", ""Population"");
        for (int index = 0; index < years.Length; index++)
            s += $""{years[index],6} {population[index],15:N0}\n"";
    }
}");
        }

        [Fact]
        public async Task TestFormatSpecifiers4()
        {
            await TestInRegularAndScriptAsync(
@"using System;

class T
{
    void M()
    {
        var a = [|String.Format(""{ 0,-10:C}"", 126347.89m)|];
    }
}",
@"using System;

class T
{
    void M()
    {
        var a = $""{126347.89m,-10:C}"";
    }
}");
        }

        [Fact]
        public async Task TestFormatSpecifiers5()
        {
            await TestInRegularAndScriptAsync(
@"using System;

public class T
{
    public static void M()
    {
        Tuple<string, DateTime, int, DateTime, int>[] cities = {
            Tuple.Create(""Los Angeles"", new DateTime(1940, 1, 1), 1504277,
                         new DateTime(1950, 1, 1), 1970358),
            Tuple.Create(""New York"", new DateTime(1940, 1, 1), 7454995,
                         new DateTime(1950, 1, 1), 7891957),
            Tuple.Create(""Chicago"", new DateTime(1940, 1, 1), 3396808,
                         new DateTime(1950, 1, 1), 3620962),
            Tuple.Create(""Detroit"", new DateTime(1940, 1, 1), 1623452,
                         new DateTime(1950, 1, 1), 1849568)
        };
        string output;
        foreach (var city in cities)
        {
            output = [|String.Format(""{0,-12}{1,8:yyyy}{2,12:N0}{3,8:yyyy}{4,12:N0}{5,14:P1}"",
                                   city.Item1, city.Item2, city.Item3, city.Item4, city.Item5,
                                   (city.Item5 - city.Item3) / (double)city.Item3)|];
        }
    }
}",
@"using System;

public class T
{
    public static void M()
    {
        Tuple<string, DateTime, int, DateTime, int>[] cities = {
            Tuple.Create(""Los Angeles"", new DateTime(1940, 1, 1), 1504277,
                         new DateTime(1950, 1, 1), 1970358),
            Tuple.Create(""New York"", new DateTime(1940, 1, 1), 7454995,
                         new DateTime(1950, 1, 1), 7891957),
            Tuple.Create(""Chicago"", new DateTime(1940, 1, 1), 3396808,
                         new DateTime(1950, 1, 1), 3620962),
            Tuple.Create(""Detroit"", new DateTime(1940, 1, 1), 1623452,
                         new DateTime(1950, 1, 1), 1849568)
        };
        string output;
        foreach (var city in cities)
        {
            output = $""{city.Item1,-12}{city.Item2,8:yyyy}{city.Item3,12:N0}{city.Item4,8:yyyy}{city.Item5,12:N0}{(city.Item5 - city.Item3) / (double)city.Item3,14:P1}"";
        }
    }
}");
        }

        [Fact]
        public async Task TestFormatSpecifiers6()
        {
            await TestInRegularAndScriptAsync(
@"using System;

public class T
{
    public static void M()
    {
        short[] values = {
            Int16.MinValue,
            -27,
            0,
            1042,
            Int16.MaxValue
        };
        foreach (short value in values)
        {
            string formatString = [|String.Format(""{0,10:G}: {0,10:X}"", value)|];
        }
    }
}",
@"using System;

public class T
{
    public static void M()
    {
        short[] values = {
            Int16.MinValue,
            -27,
            0,
            1042,
            Int16.MaxValue
        };
        foreach (short value in values)
        {
            string formatString = $""{value,10:G}: {value,10:X}"";
        }
    }
}");
        }

        [Fact]
        public async Task TestVerbatimStringLiteral()
        {
            await TestInRegularAndScriptAsync(
@"using System;

public class T
{
    public static void M()
    {
        int value1 = 16932;
        int value2 = 15421;
        string result = [|string.Format(@""
    {0,10} ({0,8:X8})
And {1,10} ({1,8:X8})
  = {2,10} ({2,8:X8})"",
                                      value1, value2, value1 & value2)|];
    }
}",
@"using System;

public class T
{
    public static void M()
    {
        int value1 = 16932;
        int value2 = 15421;
        string result = $@""
    {value1,10} ({value1,8:X8})
And {value2,10} ({value2,8:X8})
  = {value1 & value2,10} ({value1 & value2,8:X8})"";
    }
}");
        }

        [Fact]
        public async Task TestFormatWithParams()
        {
            await TestMissingInRegularAndScriptAsync(
@"using System;

public class T
{
    public static void M()
    {
        DateTime date1 = new DateTime(2009, 7, 1);
        TimeSpan hiTime = new TimeSpan(14, 17, 32);
        decimal hiTemp = 62.1m;
        TimeSpan loTime = new TimeSpan(3, 16, 10);
        decimal loTemp = 54.8m;
        string result = [|String.Format(@""Temperature on {0:d}:
                                        {1,11}: {2} degrees (hi)
                                        {3,11}: {4} degrees (lo)"",
                                      new object[] { date1, hiTime, hiTemp, loTime, loTemp })|];
    }
}");
        }

        [Fact]
        public async Task TestInvalidInteger()
        {
            await TestInRegularAndScriptAsync(
@"using System;

public class T
{
    public static void M()
    {
        string result = [|String.Format(""{0L}"", 5)|];
    }
}",
@"using System;

public class T
{
    public static void M()
    {
        string result = $""{5}"";
    }
}");
        }

        [Fact]
        public async Task TestOutVariableDeclaration_01()
        {
            await TestMissingInRegularAndScriptAsync(
@"using System;

class T
{
    void M()
    {
        var a = [|string.Format(""{0}"", out int x)|];
    }
}");
        }

        [Fact]
        public async Task TestOutVariableDeclaration_02()
        {
            await TestMissingInRegularAndScriptAsync(
@"using System;

class T
{
    void M()
    {
        var a = [|string.Format(out string x, 1)|];
    }
}");
        }

        [Fact]
        public async Task TestFormatWithNamedArguments1()
        {
            // Missing as this scenario is too esoteric.  I was not able to find any examples of code that reorders and
            // names thigns like this with format strings.
            await TestMissingAsync(
@"using System;

class T
{
    void M()
    {
        var a = [|string.Format(arg0: ""test"", arg1: ""also"", format: ""This {0} {1} works"")|];
    }
}");
        }

        [Fact]
        public async Task TestFormatWithNamedArguments2()
        {
            await TestInRegularAndScriptAsync(
@"using System;

class T
{
    void M()
    {
        var a = [|string.Format(""This {0} {1} works"", arg1: ""also"", arg0: ""test"")|];
    }
}",
@"using System;

class T
{
    void M()
    {
        var a = $""This {""test""} {""also""} works"";
    }
}");
        }

        [Fact]
        public async Task TestFormatWithNamedArguments3()
        {
            await TestInRegularAndScriptAsync(
@"using System;

class T
{
    void M()
    {
        var a = [|string.Format(""{0} {1} {2}"", ""10"", arg1: ""11"", arg2: ""12"" )|];
    }
}",
@"using System;

class T
{
    void M()
    {
        var a = $""{""10""} {""11""} {""12""}"";
    }
}");
        }

        [Fact]
        public async Task TestFormatWithNamedArguments4()
        {
            await TestInRegularAndScriptAsync(
@"using System;

class T
{
    void M()
    {
        var a = [|string.Format(""{0} {1} {2}"", ""10"", arg2: ""12"", arg1: ""11"")|];
    }
}",
@"using System;

class T
{
    void M()
    {
        var a = $""{""10""} {""11""} {""12""}"";
    }
}");
        }

        [Fact]
        public async Task TestFormatWithNamedArguments5()
        {
            await TestInRegularAndScriptAsync(
@"using System;

class T
{
    void M()
    {
        var a = [|string.Format(""{0} {1} {2} {3}"", ""10"", arg1: ""11"", arg2: ""12"")|];
    }
}",
@"using System;

class T
{
    void M()
    {
        var a = $""{""10""} {""11""} {""12""} {3}"";
    }
}");
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/35525")]
        public async Task TestOnlyArgumentSelection1()
        {
            await TestInRegularAndScriptAsync(
@"using System;

class T
{
    void M()
    {
        var a = string.Format([|""{0}""|], 1);
    }
}",
@"using System;

class T
{
    void M()
    {
        var a = $""{1}"";
    }
}");
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/35525")]
        public async Task TestOnlyArgumentSelection2()
        {
            await TestInRegularAndScriptAsync(
@"using System;

class T
{
    void M()
    {
        var a = string.Format(""{0}"", [|1|]);
    }
}",
@"using System;

class T
{
    void M()
    {
        var a = $""{1}"";
    }
}");
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/35525")]
        public async Task TestArgumentsSelection2()
        {
            await TestInRegularAndScriptAsync(
@"using System;

class T
{
    void M()
    {
        var a = string.Format([|""{0}"", 1|]);
    }
}",
@"using System;

class T
{
    void M()
    {
        var a = $""{1}"";
    }
}");
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/61346")]
        public async Task TestNoCastToObjectWhenNullableEnabled()
        {
            await TestInRegularAndScriptAsync(
@"using System;

#nullable enable

class T
{
    void M()
    {
        var a = string.Format([|""{0}"", 1|]);
    }
}",
@"using System;

#nullable enable

class T
{
    void M()
    {
        var a = $""{1}"";
    }
}");
        }

        [Fact, WorkItem(1756068, "https://devdiv.visualstudio.com/DevDiv/_workitems/edit/1756068")]
        public async Task TestArbitraryAPI()
        {
            await TestInRegularAndScriptAsync(
@"using System;

interface ILogger
{
    public void Log(string s) { }
    public void Log(string s, object arg1) { }
}

class T
{
    void M(ILogger logger)
    {
        [|logger.Log(""{0}"", 5)|];
    }
}",
@"using System;

interface ILogger
{
    public void Log(string s) { }
    public void Log(string s, object arg1) { }
}

class T
{
    void M(ILogger logger)
    {
        logger.Log($""{5}"");
    }
}");
        }

        [Fact, WorkItem(61346, "https://github.com/dotnet/roslyn/issues/61346")]
        public async Task TestNotWithExplicitCultureInfo()
        {
            await TestMissingInRegularAndScriptAsync(
@"using System;
using System.Globalization;

class T
{
    void M()
    {
        var a = string.Format(CultureInfo.InvariantCulture, [|""{0}"", 1|]);
    }
}");
        }
    }
}
