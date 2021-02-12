﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.ConvertSwitchStatementToExpression;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editor.UnitTests.CodeActions;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Testing;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.ConvertSwitchStatementToExpression
{
    using VerifyCS = CSharpCodeFixVerifier<
        ConvertSwitchStatementToExpressionDiagnosticAnalyzer,
        ConvertSwitchStatementToExpressionCodeFixProvider>;

    public class ConvertSwitchStatementToExpressionTests
    {
        private static readonly LanguageVersion CSharp9 = LanguageVersion.CSharp9;

        [Theory, CombinatorialData, Trait(Traits.Feature, Traits.Features.CodeActionsConvertSwitchStatementToExpression)]
        public void TestStandardProperty(AnalyzerProperty property)
            => VerifyCS.VerifyStandardProperty(property);

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertSwitchStatementToExpression)]
        public async Task TestReturn()
        {
            await VerifyCS.VerifyCodeFixAsync(
@"class Program
{
    int M(int i)
    {
        [|switch|] (i)
        {
            case 1:
                return 4;
            case 2:
                return 5;
            case 3:
                return 6;
            default:
                return 7;
        }
    }
}",
@"class Program
{
    int M(int i)
    {
        return i switch
        {
            1 => 4,
            2 => 5,
            3 => 6,
            _ => 7,
        };
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertSwitchStatementToExpression)]
        public async Task TestReturnAndThrow()
        {
            await VerifyCS.VerifyCodeFixAsync(
@"class Program
{
    int M(int i)
    {
        [|switch|] (i)
        {
            case 1:
                return 4;
            default: 
                throw null;
            case 2:
                return 5;
            case 3:
                return 6;
        }
    }
}",
@"class Program
{
    int M(int i)
    {
        return i switch
        {
            1 => 4,
            2 => 5,
            3 => 6,
            _ => throw null,
        };
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertSwitchStatementToExpression)]
        public async Task TestAssignment_Array()
        {
            await VerifyCS.VerifyCodeFixAsync(
@"class Program
{
    int[] array = new int[1];

    void M(int i)
    {
        [|switch|] (i)
        {
            case 1:
                array[0] = 4;
                break;
            case 2:
                array[0] = 5;
                break;
            case 3:
                array[0] = 6;
                break;
            default:
                array[0] = 7;
                break;
        }
    }
}",
@"class Program
{
    int[] array = new int[1];

    void M(int i)
    {
        array[0] = i switch
        {
            1 => 4,
            2 => 5,
            3 => 6,
            _ => 7,
        };
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertSwitchStatementToExpression)]
        public async Task TestMissingOnDifferentIndexerArgs()
        {
            var code = @"class Program
{
    int[] array = new int[1];

    void M(int i)
    {
        switch (i)
        {
            case 1:
                array[1] = 4;
                break;
            case 2:
                array[2] = 5;
                break;
            case 3:
                array[2] = 6;
                break;
            default:
                array[2] = 7;
                break;
        }
    }
}";

            await VerifyCS.VerifyCodeFixAsync(code, code);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertSwitchStatementToExpression)]
        public async Task TestMissingOnQualifiedName()
        {
            var code = @"class Program
{
    int[] array = new int[1];

    void M(int i)
    {
        switch (i)
        {
            case 1:
                this.array[2] = 4;
                break;
            case 2:
                array[2] = 5;
                break;
            case 3:
                array[2] = 6;
                break;
            default:
                array[2] = 7;
                break;
        }
    }
}";

            await VerifyCS.VerifyCodeFixAsync(code, code);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertSwitchStatementToExpression)]
        public async Task TestMissingOnDefaultBreak_01()
        {
            var code = @"class Program
{
    void M(int i)
    {
        switch (i)
        {
            default:
                break;
        }
    }
}";

            await VerifyCS.VerifyCodeFixAsync(code, code);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertSwitchStatementToExpression)]
        public async Task TestMissingOnDefaultBreak_02()
        {
            var code = @"class Program
{
    void M(int i)
    {
        switch (i)
        {
            case { }:
                break;
        }
    }
}";

            await VerifyCS.VerifyCodeFixAsync(code, code);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertSwitchStatementToExpression)]
        public async Task TestMissingOnDefaultBreak_03()
        {
            var code = @"class Program
{
    void M(int i)
    {
        switch (i)
        {
            case var _:
                break;
        }
    }
}";

            await VerifyCS.VerifyCodeFixAsync(code, code);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertSwitchStatementToExpression)]
        public async Task TestMissingOnDefaultBreak_04()
        {
            var code = @"class Program
{
    void M(int i)
    {
        switch (i)
        {
            case var x:
                break;
        }
    }
}";

            await VerifyCS.VerifyCodeFixAsync(code, code);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertSwitchStatementToExpression)]
        public async Task TestMissingOnAllBreak()
        {
            var code = @"class Program
{
    void M(int i)
    {
        switch (i)
        {
            case 1:
                break;
            case 2:
                break;
            case 3:
                break;
        }
    }
}";

            await VerifyCS.VerifyCodeFixAsync(code, code);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertSwitchStatementToExpression)]
        public async Task TestAllThrow()
        {
            await VerifyCS.VerifyCodeFixAsync(
@"using System;
class Program
{
    void M(int i)
    {
        [|switch|] (i)
        {
            case 1:
                throw null;
            default:
                throw new Exception();
        }
    }
}",
@"using System;
class Program
{
    void M(int i)
    {
        throw i switch
        {
            1 => null,
            _ => new Exception(),
        };
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertSwitchStatementToExpression)]
        public async Task TestAssignment()
        {
            await VerifyCS.VerifyCodeFixAsync(
@"class Program
{
    void M(int i)
    {
        int j;
        [|switch|] (i)
        {
            case 1:
                j = 4;
                break;
            case 2:
                j = 5;
                break;
            case 3:
                j = 6;
                break;
        }
        throw null;
    }
}",
@"class Program
{
    void M(int i)
    {
        var j = i switch
        {
            1 => 4,
            2 => 5,
            3 => 6,
            _ => throw null,
        };
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertSwitchStatementToExpression)]
        public async Task TestMissingOnNextStatementMismatch()
        {
            var code = @"class Program
{
    int M(int i)
    {
        int j = 0;
        switch (i)
        {
            case 1:
                j = 4;
                break;
            case 2:
                j = 5;
                break;
            case 3:
                j = 6;
                break;
        }
        return j;
    }
}";

            await VerifyCS.VerifyCodeFixAsync(code, code);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertSwitchStatementToExpression)]
        public async Task TestMissingOnAssignmentMismatch()
        {
            var code = @"class Program
{
    int M(int i)
    {
        int j = 0;
        switch (i)
        {
            case 1:
                j = 4;
                break;
            case 2:
                j += 5;
                break;
        }
        return j;
    }
}";

            await VerifyCS.VerifyCodeFixAsync(code, code);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertSwitchStatementToExpression)]
        public async Task TestAssignment_Compound()
        {
            await VerifyCS.VerifyCodeFixAsync(
@"class Program
{
    void M(int i)
    {
        int j = 0;
        [|switch|] (i)
        {
            case 1:
                j += 4;
                break;
            case 2:
                j += 5;
                break;
            case 3:
                j += 6;
                break;
        }
        throw null;
    }
}",
@"class Program
{
    void M(int i)
    {
        int j = 0;
        j += i switch
        {
            1 => 4,
            2 => 5,
            3 => 6,
            _ => throw null,
        };
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertSwitchStatementToExpression)]
        public async Task TestAssignment_UseBeforeAssignment()
        {
            await VerifyCS.VerifyCodeFixAsync(
@"class Program
{
    void M(int i)
    {
        int j = 123;
        M(i);
        [|switch|] (i)
        {
            case 1:
                j = 4;
                break;
            case 2:
                j = 5;
                break;
            case 3:
                j = 6;
                break;
        }
        throw null;
    }
}",
@"class Program
{
    void M(int i)
    {
        int j = 123;
        M(i);
        j = i switch
        {
            1 => 4,
            2 => 5,
            3 => 6,
            _ => throw null,
        };
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertSwitchStatementToExpression)]
        public async Task TestMissingOnMultiAssignment()
        {
            var code = @"class Program
{
    void M(int i)
    {
        int j, k;
        switch (i)
        {
            case 1:
                j = 4;
                k = 5;
                break;
            case 2:
                j = 6;
                k = 7;
                break;
            case 3:
                j = 8;
                k = 9;
                break;
        }
        throw null;
    }
}";

            await VerifyCS.VerifyCodeFixAsync(code, code);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertSwitchStatementToExpression)]
        public async Task TestMissingOnMultiCaseSection()
        {
            var code = @"class Program
{
    void M(int i)
    {
        int j;
        switch (i)
        {
            case 1:
            case 2:
                j = 4;
                break;
        }
        throw null;
    }
}";

            await VerifyCS.VerifyCodeFixAsync(code, code);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertSwitchStatementToExpression)]
        public async Task TestMissingOnMultiCaseSectionWithWhenClause_CSharp9()
        {
            var code = @"class Program
{
    void M(int i)
    {
        int j;
        switch (i)
        {
            case 1:
            case 2 when true:
                j = 4;
                break;
        }
        throw null;
    }
}";

            await new VerifyCS.Test
            {
                TestCode = code,
                FixedCode = code,
                LanguageVersion = CSharp9,
            }.RunAsync();
        }

        [WorkItem(42368, "https://github.com/dotnet/roslyn/issues/42368")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertSwitchStatementToExpression)]
        public async Task TestOnMultiCaseSection_CSharp9()
        {
            var testCode = @"class Program
{
    void M(int i)
    {
        int j;
        [|switch|] (i)
        {
            case 1:
            case 2:
                j = 4;
                break;
        }
        throw null;
    }
}";
            var fixedCode = @"class Program
{
    void M(int i)
    {
        var j = i switch
        {
            1 or 2 => 4,
            _ => throw null,
        };
    }
}";

            await new VerifyCS.Test
            {
                TestCode = testCode,
                FixedCode = fixedCode,
                LanguageVersion = CSharp9,
            }.RunAsync();
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertSwitchStatementToExpression)]
        public async Task TestMissingOnMultiCompoundAssignment()
        {
            var code = @"class Program
{
    void M(int i)
    {
        int j = 0, k = 0;
        switch (i)
        {
            case 1:
                j += 4;
                k += 5;
                break;
            case 2:
                j += 6;
                k += 7;
                break;
            case 3:
                j += 8;
                k += 9;
                break;
        }
        throw null;
    }
}";

            await VerifyCS.VerifyCodeFixAsync(code, code);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertSwitchStatementToExpression)]
        public async Task TestMissingOnGoto()
        {
            var code = @"class Program
{
    int M(int i)
    {
        switch (i)
        {
            case 1:
                return 0;
            case 2:
                goto default;
            default:
                return 2;
        }
    }
}";

            await VerifyCS.VerifyCodeFixAsync(code, code);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertSwitchStatementToExpression)]
        public async Task TestTrivia_01()
        {
            await VerifyCS.VerifyCodeFixAsync(
@"class Program
{
    int M(int i)
    {
        // leading switch
        [|switch|] (i) // trailing switch
        {
            // leading label
            case 1: // trailing label
                // leading body
                return 4; // trailing body
            case 2:
                return 5;
            case 3:
                return 6;
        }
        
        // leading next statement
        throw null; // leading next statement
    }
}",
@"class Program
{
    int M(int i)
    {
        // leading switch
        return i switch // trailing switch
        {
            // leading label
            // trailing label
            1 => 4,// leading body
                   // trailing body
            2 => 5,
            3 => 6,
            // leading next statement
            _ => throw null,// leading next statement
        };
    }
}");
        }

        [WorkItem(37873, "https://github.com/dotnet/roslyn/issues/37873")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertSwitchStatementToExpression)]
        public async Task TestTrivia_02()
        {
            await VerifyCS.VerifyCodeFixAsync(
@"class Program
{
    static int GetValue(int input)
    {
        [|switch|] (input)
        {
            case 1:
                // this little piggy went to market
                return 42;
            case 2:
                // this little piggy stayed home
                return 50;
            case 3:
                // this little piggy had roast beef
                return 79;
            default:
                // this little piggy had none
                return 80;
        }
    }
}",
@"class Program
{
    static int GetValue(int input)
    {
        return input switch
        {
            1 => 42,// this little piggy went to market
            2 => 50,// this little piggy stayed home
            3 => 79,// this little piggy had roast beef
            _ => 80,// this little piggy had none
        };
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertSwitchStatementToExpression)]
        [WorkItem(36086, "https://github.com/dotnet/roslyn/issues/36086")]
        public async Task TestSeverity()
        {
            var source =
@"class Program
{
    int M(int i)
    {
        switch (i)
        {
            case 1:
                return 4;
            case 2:
                return 5;
            case 3:
                return 6;
            default:
                return 7;
        }
    }
}";

            var analyzer = new ConvertSwitchStatementToExpressionDiagnosticAnalyzer();
            var descriptor = analyzer.SupportedDiagnostics.First(descriptor => descriptor.Id == IDEDiagnosticIds.ConvertSwitchStatementToExpressionDiagnosticId);
            await new VerifyCS.Test
            {
                TestCode = source,
                ExpectedDiagnostics =
                {
                    // Test0.cs(5,9): warning IDE0066: Use 'switch' expression
                    new DiagnosticResult(descriptor).WithSeverity(DiagnosticSeverity.Warning).WithSpan(5, 9, 5, 15).WithSpan(5, 9, 15, 10),
                },
                Options =
                {
                    { CSharpCodeStyleOptions.PreferSwitchExpression, true, NotificationOption2.Warning },
                },
            }.RunAsync();
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertSwitchStatementToExpression)]
        [WorkItem(36995, "https://github.com/dotnet/roslyn/issues/36995")]
        public async Task TestAddParenthesesAroundBinaryExpression()
        {
            await VerifyCS.VerifyCodeFixAsync(
@"class Program
{
    void M(int i)
    {
        int j = 123;
        [|switch|] (i % 10)
        {
            case 1:
                j = 4;
                break;
            case 2:
                j = 5;
                break;
        }
        throw null;
    }
}",
@"class Program
{
    void M(int i)
    {
        int j = 123;
        j = (i % 10) switch
        {
            1 => 4,
            2 => 5,
            _ => throw null,
        };
    }
}");
        }

        [WorkItem(37947, "https://github.com/dotnet/roslyn/issues/37947")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertSwitchStatementToExpression)]
        public async Task TestMultiLabelWithDefault()
        {
            await VerifyCS.VerifyCodeFixAsync(
@"using System;

class Program
{
    public static string FromDay(DayOfWeek dayOfWeek)
    {
        [|switch|] (dayOfWeek)
        {
            case DayOfWeek.Monday:
                return ""Monday"";
            case DayOfWeek.Friday:
            default:
                return ""Other"";
        }
    }
}",
@"using System;

class Program
{
    public static string FromDay(DayOfWeek dayOfWeek)
    {
        return dayOfWeek switch
        {
            DayOfWeek.Monday => ""Monday"",
            _ => ""Other"",
        };
    }
}");
        }

        [WorkItem(37949, "https://github.com/dotnet/roslyn/issues/37949")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertSwitchStatementToExpression)]
        public async Task TestMissingOnUseInNextStatement()
        {
            var code = @"using System;

class Program
{
    public static void Throw(int index)
    {
        string name = """";
        switch (index)
        {
            case 0: name = ""1""; break;
            case 1: name = ""2""; break;
        }
        throw new Exception(name);
    }
}";

            await VerifyCS.VerifyCodeFixAsync(code, code);
        }

        [WorkItem(36876, "https://github.com/dotnet/roslyn/issues/36876")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertSwitchStatementToExpression)]
        public async Task TestDeclarationInOuterScope()
        {
            await VerifyCS.VerifyCodeFixAsync(
@"using System;
using System.IO;

class Program
{
    static SeekOrigin origin;
    static long offset;
    static long position;
    static long length;
    public static void Test()
    {
        long target;
        try
        {
            [|switch|] (origin)
            {
                case SeekOrigin.Begin:
                    target = offset;
                    break;

                case SeekOrigin.Current:
                    target = checked(offset + position);
                    break;

                case SeekOrigin.End:
                    target = checked(offset + length);
                    break;

                default:
                    throw new ArgumentOutOfRangeException(nameof(origin));
            }
        }
        catch (OverflowException)
        {
            throw new ArgumentOutOfRangeException(nameof(offset));
        }

        if (target < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(offset));
        }
    }
}",
@"using System;
using System.IO;

class Program
{
    static SeekOrigin origin;
    static long offset;
    static long position;
    static long length;
    public static void Test()
    {
        long target;
        try
        {
            target = origin switch
            {
                SeekOrigin.Begin => offset,
                SeekOrigin.Current => checked(offset + position),
                SeekOrigin.End => checked(offset + length),
                _ => throw new ArgumentOutOfRangeException(nameof(origin)),
            };
        }
        catch (OverflowException)
        {
            throw new ArgumentOutOfRangeException(nameof(offset));
        }

        if (target < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(offset));
        }
    }
}");
        }

        [WorkItem(37872, "https://github.com/dotnet/roslyn/issues/37872")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertSwitchStatementToExpression)]
        public async Task TestMissingOnDirectives()
        {
            var code = @"class Program
{
    static void Main() { }

    static int GetValue(int input)
    {
        switch (input)
        {
            case 1:
                return 42;
            case 2:
#if PLATFORM_UNIX
                return 50;
#else
                return 51;
#endif
            case 3:
                return 79;
            default:
                return 80;
        }
    }
}";

            await VerifyCS.VerifyCodeFixAsync(code, code);
        }

        [WorkItem(37950, "https://github.com/dotnet/roslyn/issues/37950")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertSwitchStatementToExpression)]
        public async Task TestShouldNotCastNullOnNullableValueType_ReturnStatement()
        {
            await VerifyCS.VerifyCodeFixAsync(
@"class Program
{
    public static bool? GetBool(string name)
    {
        [|switch|] (name)
        {
            case ""a"": return true;
            case ""b"": return false;
            default: return null;
        }
    }
}",
@"class Program
{
    public static bool? GetBool(string name)
    {
        return name switch
        {
            ""a"" => true,
            ""b"" => false,
            _ => null,
        };
    }
}");
        }

        [WorkItem(37950, "https://github.com/dotnet/roslyn/issues/37950")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertSwitchStatementToExpression)]
        public async Task TestShouldNotCastNullOnNullableValueType_Assignment()
        {
            await VerifyCS.VerifyCodeFixAsync(
@"class Program
{
    public static void Test(string name)
    {
        bool? result;
        [|switch|] (name)
        {
            case ""a"": result = true; break;
            case ""b"": result = false; break;
            default: result = null; break;
        }
    }
}",
@"class Program
{
    public static void Test(string name)
    {
        bool? result = name switch
        {
            ""a"" => true,
            ""b"" => false,
            _ => null,
        };
    }
}");
        }

        [WorkItem(38771, "https://github.com/dotnet/roslyn/issues/38771")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertSwitchStatementToExpression)]
        public async Task TestExplicitDeclaration_Interfaces()
        {
            var input =
@"using System;

class Program
{
    interface IFruit { }

    interface IFruit2 { }

    class Apple : IFruit, IFruit2 { }

    class Banana : IFruit, IFruit2 { }

    public static void Test(string name)
    {
        IFruit2 fruit;
        [|switch|] (name)
        {
            case ""apple"":
                fruit = new Apple();
            break;
            case ""banana"":
                fruit = new Banana();
            break;
            default:
                throw new InvalidOperationException(""Unknown fruit."");
        }
    }
}";
            var expected =
@"using System;

class Program
{
    interface IFruit { }

    interface IFruit2 { }

    class Apple : IFruit, IFruit2 { }

    class Banana : IFruit, IFruit2 { }

    public static void Test(string name)
    {
        IFruit2 fruit = name switch
        {
            ""apple"" => new Apple(),
            ""banana"" => new Banana(),
            _ => throw new InvalidOperationException(""Unknown fruit.""),
        };
    }
}";

            await new VerifyCS.Test
            {
                TestCode = input,
                FixedCode = expected,
                Options =
                {
                    { CSharpCodeStyleOptions.VarElsewhere, true, NotificationOption2.Silent },
                },
            }.RunAsync();
        }

        [WorkItem(38771, "https://github.com/dotnet/roslyn/issues/38771")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertSwitchStatementToExpression)]
        public async Task TestExplicitDeclaration_Interfaces2()
        {
            var input =
@"using System;

class Program
{
    interface IFruit { }

    interface IFruit2 { }

    class Apple : IFruit, IFruit2 { }

    class Banana : IFruit, IFruit2 { }

    public static void Test(string name)
    {
        IFruit2 fruit;
        [|switch|] (name)
        {
            case ""banana"":
                fruit = new Banana();
            break;
            case ""banana2"":
                fruit = new Banana();
            break;
            default:
                throw new InvalidOperationException(""Unknown fruit."");
        }
    }
}";
            var expected =
@"using System;

class Program
{
    interface IFruit { }

    interface IFruit2 { }

    class Apple : IFruit, IFruit2 { }

    class Banana : IFruit, IFruit2 { }

    public static void Test(string name)
    {
        IFruit2 fruit = name switch
        {
            ""banana"" => new Banana(),
            ""banana2"" => new Banana(),
            _ => throw new InvalidOperationException(""Unknown fruit.""),
        };
    }
}";

            await new VerifyCS.Test
            {
                TestCode = input,
                FixedCode = expected,
                Options =
                {
                    { CSharpCodeStyleOptions.VarElsewhere, true, NotificationOption2.Silent },
                },
            }.RunAsync();
        }

        [WorkItem(38771, "https://github.com/dotnet/roslyn/issues/38771")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertSwitchStatementToExpression)]
        public async Task TestExplicitDeclaration_Interfaces3()
        {
            var input =
@"using System;

class Program
{
    interface IFruit { }

    interface IFruit2 { }

    class Apple : IFruit2 { }

    class Banana : IFruit, IFruit2 { }

    public static void Test(string name)
    {
        IFruit2 fruit;
        [|switch|] (name)
        {
            case ""apple"":
                fruit = new Apple();
            break;
            case ""banana"":
                fruit = new Banana();
            break;
            default:
                throw new InvalidOperationException(""Unknown fruit."");
        }
    }
}";
            var expected =
@"using System;

class Program
{
    interface IFruit { }

    interface IFruit2 { }

    class Apple : IFruit2 { }

    class Banana : IFruit, IFruit2 { }

    public static void Test(string name)
    {
        IFruit2 fruit = name switch
        {
            ""apple"" => new Apple(),
            ""banana"" => new Banana(),
            _ => throw new InvalidOperationException(""Unknown fruit.""),
        };
    }
}";

            await new VerifyCS.Test
            {
                TestCode = input,
                FixedCode = expected,
                Options =
                {
                    { CSharpCodeStyleOptions.VarElsewhere, true, NotificationOption2.Silent },
                },
            }.RunAsync();
        }

        [WorkItem(38771, "https://github.com/dotnet/roslyn/issues/38771")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertSwitchStatementToExpression)]
        public async Task TestExplicitDeclaration_ClassInheritance()
        {
            var input =
@"using System;

class Program
{
    interface IFruit { }

    interface IFruit2 { }

    class Apple : IFruit2 { }

    class Banana : IFruit, IFruit2 { }

    class OrganicApple : Apple { }

    class OrganicBanana : Banana { }

    public static void Test(string name)
    {
        IFruit2 fruit;
        [|switch|] (name)
        {
            case ""apple"":
                fruit = new OrganicApple();
            break;
            case ""banana"":
                fruit = new OrganicBanana();
            break;
            default:
                throw new InvalidOperationException(""Unknown fruit."");
        }
    }
}";
            var expected =
@"using System;

class Program
{
    interface IFruit { }

    interface IFruit2 { }

    class Apple : IFruit2 { }

    class Banana : IFruit, IFruit2 { }

    class OrganicApple : Apple { }

    class OrganicBanana : Banana { }

    public static void Test(string name)
    {
        IFruit2 fruit = name switch
        {
            ""apple"" => new OrganicApple(),
            ""banana"" => new OrganicBanana(),
            _ => throw new InvalidOperationException(""Unknown fruit.""),
        };
    }
}";

            await new VerifyCS.Test
            {
                TestCode = input,
                FixedCode = expected,
                Options =
                {
                    { CSharpCodeStyleOptions.VarElsewhere, true, NotificationOption2.Silent },
                },
            }.RunAsync();
        }

        [WorkItem(38771, "https://github.com/dotnet/roslyn/issues/38771")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertSwitchStatementToExpression)]
        public async Task TestExplicitDeclaration_ClassInheritance2()
        {
            var input =
@"using System;

class Program
{
    interface IFruit { }

    interface IFruit2 { }

    class Banana : IFruit, IFruit2 { }

    class OrganicBanana : Banana, IFruit { }

    public static void Test(string name)
    {
        IFruit2 fruit;
        [|switch|] (name)
        {
            case ""banana"":
                fruit = new Banana();
            break;
            case ""organic banana"":
                fruit = new OrganicBanana();
            break;
            default:
                throw new InvalidOperationException(""Unknown fruit."");
        }
    }
}";
            var expected =
@"using System;

class Program
{
    interface IFruit { }

    interface IFruit2 { }

    class Banana : IFruit, IFruit2 { }

    class OrganicBanana : Banana, IFruit { }

    public static void Test(string name)
    {
        IFruit2 fruit = name switch
        {
            ""banana"" => new Banana(),
            ""organic banana"" => new OrganicBanana(),
            _ => throw new InvalidOperationException(""Unknown fruit.""),
        };
    }
}";

            await new VerifyCS.Test
            {
                TestCode = input,
                FixedCode = expected,
                Options =
                {
                    { CSharpCodeStyleOptions.VarElsewhere, true, NotificationOption2.Silent },
                },
            }.RunAsync();
        }

        [WorkItem(38771, "https://github.com/dotnet/roslyn/issues/38771")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertSwitchStatementToExpression)]
        public async Task TestImplicitDeclaration_ClassInheritance()
        {
            var input =
@"using System;

class Program
{
    interface IFruit { }

    interface IFruit2 { }

    class Banana : IFruit, IFruit2 { }

    class OrganicBanana : Banana { }

    public static void Test(string name)
    {
        Banana fruit;
        [|switch|] (name)
        {
            case ""banana"":
                fruit = new Banana();
            break;
            case ""organic banana"":
                fruit = new OrganicBanana();
            break;
            default:
                throw new InvalidOperationException(""Unknown fruit."");
        }
    }
}";
            var expected =
@"using System;

class Program
{
    interface IFruit { }

    interface IFruit2 { }

    class Banana : IFruit, IFruit2 { }

    class OrganicBanana : Banana { }

    public static void Test(string name)
    {
        var fruit = name switch
        {
            ""banana"" => new Banana(),
            ""organic banana"" => new OrganicBanana(),
            _ => throw new InvalidOperationException(""Unknown fruit.""),
        };
    }
}";

            await new VerifyCS.Test
            {
                TestCode = input,
                FixedCode = expected,
                Options =
                {
                    { CSharpCodeStyleOptions.VarElsewhere, true, NotificationOption2.Silent },
                },
            }.RunAsync();
        }

        [WorkItem(38771, "https://github.com/dotnet/roslyn/issues/38771")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertSwitchStatementToExpression)]
        public async Task TestImplicitDeclaration_ClassInheritance2()
        {
            var input =
@"using System;

class Program
{
    interface IFruit { }

    interface IFruit2 { }

    class Banana : IFruit, IFruit2 { }

    class OrganicBanana : Banana, IFruit { }

    public static void Test(string name)
    {
        Banana fruit;
        [|switch|] (name)
        {
            case ""banana"":
                fruit = new Banana();
            break;
            case ""organic banana"":
                fruit = new OrganicBanana();
            break;
            default:
                throw new InvalidOperationException(""Unknown fruit."");
        }
    }
}";
            var expected =
@"using System;

class Program
{
    interface IFruit { }

    interface IFruit2 { }

    class Banana : IFruit, IFruit2 { }

    class OrganicBanana : Banana, IFruit { }

    public static void Test(string name)
    {
        var fruit = name switch
        {
            ""banana"" => new Banana(),
            ""organic banana"" => new OrganicBanana(),
            _ => throw new InvalidOperationException(""Unknown fruit.""),
        };
    }
}";

            await new VerifyCS.Test
            {
                TestCode = input,
                FixedCode = expected,
                Options =
                {
                    { CSharpCodeStyleOptions.VarElsewhere, true, NotificationOption2.Silent },
                },
            }.RunAsync();
        }

        [WorkItem(38771, "https://github.com/dotnet/roslyn/issues/38771")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertSwitchStatementToExpression)]
        public async Task TestExplicitDeclaration_AllCasesDefaultLiteral()
        {
            var input =
@"class Program
{
    public static void Test()
    {
        var a = 0;
        object o;
        [|switch|] (a)
        {
            case 0:
                o = default;
                break;
            default:
                o = default;
                break;
        }
    }
}";
            var expected =
@"class Program
{
    public static void Test()
    {
        var a = 0;
        object o = a switch
        {
            0 => default,
            _ => default,
        };
    }
}";

            await new VerifyCS.Test
            {
                TestCode = input,
                FixedCode = expected,
                Options =
                {
                    { CSharpCodeStyleOptions.VarForBuiltInTypes, true, NotificationOption2.Silent },
                },
            }.RunAsync();
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertSwitchStatementToExpression)]
        public async Task TestExplicitDeclaration_MixedDefaultLiteralDefaultParameter()
        {
            var input =
@"class Program
{
    public static void Test()
    {
        var a = 0;
        object o;
        [|switch|] (a)
        {
            case 0:
                o = default(string);
                break;
            default:
                o = default;
                break;
        }
    }
}";
            var expected = @"class Program
{
    public static void Test()
    {
        var a = 0;
        object o = a switch
        {
            0 => default(string),
            _ => default,
        };
    }
}";

            await new VerifyCS.Test
            {
                TestCode = input,
                FixedCode = expected,
                Options =
                {
                    { CSharpCodeStyleOptions.VarForBuiltInTypes, true, NotificationOption2.Silent },
                },
            }.RunAsync();
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertSwitchStatementToExpression)]
        public async Task TestImplicitDeclaration_AllCasesDefaultParameter()
        {
            var input =
@"class Program
{
    public static void Test()
    {
        var a = 0;
        object o;
        [|switch|] (a)
        {
            case 0:
                o = default(object);
                break;
            default:
                o = default(object);
                break;
        }
    }
}";
            var expected =
@"class Program
{
    public static void Test()
    {
        var a = 0;
        var o = a switch
        {
            0 => default(object),
            _ => default(object),
        };
    }
}";

            await new VerifyCS.Test
            {
                TestCode = input,
                FixedCode = expected,
                Options =
                {
                    { CSharpCodeStyleOptions.VarForBuiltInTypes, true, NotificationOption2.Silent },
                },
            }.RunAsync();
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertSwitchStatementToExpression)]
        public async Task TestExplicitDeclaration_AllCasesDefaultParameter()
        {
            var input =
@"class Program
{
    public static void Test()
    {
        var a = 0;
        object o;
        [|switch|] (a)
        {
            case 0:
                o = default(object);
                break;
            default:
                o = default(object);
                break;
        }
    }
}";
            var expected =
@"class Program
{
    public static void Test()
    {
        var a = 0;
        object o = a switch
        {
            0 => default(object),
            _ => default(object),
        };
    }
}";

            await new VerifyCS.Test
            {
                TestCode = input,
                FixedCode = expected,
                Options =
                {
                    { CSharpCodeStyleOptions.VarForBuiltInTypes, false, NotificationOption2.Silent },
                },
            }.RunAsync();
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertSwitchStatementToExpression)]
        public async Task TestExplicitDeclaration_DeclarationTypeDifferentFromAllCaseTypes()
        {
            var input =
@"class Program
{
    public static void Test()
    {
        var a = 0;
        object o;
        [|switch|] (a)
        {
            case 0:
                o = """";
                break;
            default:
                o = """";
                break;
        }
    }
}";
            var expected =
@"class Program
{
    public static void Test()
    {
        var a = 0;
        object o = a switch
        {
            0 => """",
            _ => """",
        };
    }
}";

            await new VerifyCS.Test
            {
                TestCode = input,
                FixedCode = expected,
                Options =
                {
                    { CSharpCodeStyleOptions.VarForBuiltInTypes, true, NotificationOption2.Silent },
                },
            }.RunAsync();
        }

        [WorkItem(40198, "https://github.com/dotnet/roslyn/issues/40198")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertSwitchStatementToExpression)]
        public async Task TestNotWithRefReturns()
        {
            var code = @"using System;
class Program
{
    static ref int GetRef(int[] mem, int addr, int mode)
    {
        switch (mode)
        {
            case 0: return ref mem[mem[addr]];
            case 1: return ref mem[addr];
            default: throw new Exception();
        }
    }
}";

            await VerifyCS.VerifyCodeFixAsync(code, code);
        }

        [WorkItem(40198, "https://github.com/dotnet/roslyn/issues/40198")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertSwitchStatementToExpression)]
        public async Task TestNotWithRefAssignment()
        {
            var code = @"using System;
class Program
{
    static ref int GetRef(int[] mem, int addr, int mode)
    {
        ref int i = ref addr;
        switch (mode)
        {
            case 0: i = ref mem[mem[addr]]; break;
            default: throw new Exception();
        }

        return ref mem[addr];
    }
}";

            await VerifyCS.VerifyCodeFixAsync(code, code);
        }

        [WorkItem(40198, "https://github.com/dotnet/roslyn/issues/40198")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertSwitchStatementToExpression)]
        public async Task TestNotWithRefConditionalAssignment()
        {
            var code = @"using System;
class Program
{
    static ref int GetRef(int[] mem, int addr, int mode)
    {
        ref int i = ref addr;
        switch (mode)
        {
            case 0: i = ref true ? ref mem[mem[addr]] : ref mem[mem[addr]]; break;
            default: throw new Exception();
        }

        return ref mem[addr];
    }
}";

            await VerifyCS.VerifyCodeFixAsync(code, code);
        }

        [WorkItem(40198, "https://github.com/dotnet/roslyn/issues/40198")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertSwitchStatementToExpression)]
        public async Task TestWithRefInsideConditionalAssignment()
        {
            await VerifyCS.VerifyCodeFixAsync(
@"using System;
class Program
{
    static void GetRef(int[] mem, int addr, int mode)
    {
        ref int i = ref addr;
        [|switch|] (mode)
        {
            case 0: i = true ? ref mem[mem[addr]] : ref mem[mem[addr]]; break;
            default: throw new Exception();
        }
    }
}",
@"using System;
class Program
{
    static void GetRef(int[] mem, int addr, int mode)
    {
        ref int i = ref addr;
        i = mode switch
        {
            0 => true ? ref mem[mem[addr]] : ref mem[mem[addr]],
            _ => throw new Exception(),
        };
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertSwitchStatementToExpression)]
        public async Task TopLevelStatement()
        {
            var source = @"
int i = 0;
[|switch|] (i)
{
    case 1:
        return 4;
    default:
        return 7;
}";

            var fixedSource = @"
int i = 0;
return i switch
{
    1 => 4,
    _ => 7,
};
";

            var test = new VerifyCS.Test
            {
                TestCode = source,
                FixedCode = fixedSource,
                LanguageVersion = LanguageVersion.CSharp9,
            };

            test.ExpectedDiagnostics.Add(
                // /0/Test0.cs(2,1): error CS8805: Program using top-level statements must be an executable.
                DiagnosticResult.CompilerError("CS8805").WithSpan(2, 1, 2, 11));

            await test.RunAsync();
        }

        [WorkItem(44449, "https://github.com/dotnet/roslyn/issues/44449")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertSwitchStatementToExpression)]
        public async Task TopLevelStatement_FollowedWithThrow()
        {
            // We should be rewriting the declaration for 'j' to get 'var j = i switch ...'
            var source = @"
int i = 0;
int j;
[|switch|] (i)
{
    case 1:
        j = 4;
        break;
    case 2:
        j = 5;
        break;
}
throw null;
";

            var fixedSource = @"
int i = 0;
int j;
j = i switch
{
    1 => 4,
    2 => 5,
    _ => throw null,
};
";

            var test = new VerifyCS.Test
            {
                TestCode = source,
                FixedCode = fixedSource,
                LanguageVersion = LanguageVersion.CSharp9,
            };

            test.ExpectedDiagnostics.Add(
                // /0/Test0.cs(2,1): error CS8805: Program using top-level statements must be an executable.
                DiagnosticResult.CompilerError("CS8805").WithSpan(2, 1, 2, 11));

            await test.RunAsync();
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertSwitchStatementToExpression)]
        [WorkItem(48006, "https://github.com/dotnet/roslyn/issues/48006")]
        public async Task TestOnMultiCaseSection_String_CSharp9()
        {
            var testCode = @"
class Program
{
    bool M(string s)
    {
        [|switch|] (s)
        {
	        case ""Last"":
            case ""First"":
            case ""Count"":
                return true;
            default:
                return false;
        }
    }
}";
            var fixedCode = @"
class Program
{
    bool M(string s)
    {
        return s switch
        {
            ""Last"" or ""First"" or ""Count"" => true,
            _ => false,
        };
    }
}";

            await new VerifyCS.Test
            {
                TestCode = testCode,
                FixedCode = fixedCode,
                LanguageVersion = CSharp9,
            }.RunAsync();
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertSwitchStatementToExpression)]
        [WorkItem(49788, "https://github.com/dotnet/roslyn/issues/49788")]
        public async Task TestParenthesizedExpression1()
        {
            await VerifyCS.VerifyCodeFixAsync(
@"class Program
{
    int M(object i)
    {
        [|switch|] (i.GetType())
        {
            default: return 0;
        }
    }
}",
@"class Program
{
    int M(object i)
    {
        return i.GetType() switch
        {
            _ => 0,
        };
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertSwitchStatementToExpression)]
        [WorkItem(49788, "https://github.com/dotnet/roslyn/issues/49788")]
        public async Task TestParenthesizedExpression2()
        {
            await VerifyCS.VerifyCodeFixAsync(
@"class Program
{
    int M()
    {
        [|switch|] (1 + 1)
        {
            default: return 0;
        }
    }
}",
@"class Program
{
    int M()
    {
        return (1 + 1) switch
        {
            _ => 0,
        };
    }
}");
        }
    }
}
