﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.ConvertSwitchStatementToExpression;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Diagnostics;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.ConvertSwitchStatementToExpression
{
    public partial class ConvertSwitchStatementToExpressionTests : AbstractCSharpDiagnosticProviderBasedUserDiagnosticTest
    {
        internal override (DiagnosticAnalyzer, CodeFixProvider) CreateDiagnosticProviderAndFixer(Workspace workspace)
            => (new ConvertSwitchStatementToExpressionDiagnosticAnalyzer(), new ConvertSwitchStatementToExpressionCodeFixProvider());

        private Task TestInCSharp8(string actual, string expected)
            => TestInRegularAndScriptAsync(actual, expected, parseOptions: TestOptions.Regular8);

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertSwitchStatementToExpression)]
        public async Task TestReturn()
        {
            await TestInCSharp8(
@"class Program
{
    int M(int i)
    {
        [||]switch (i)
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
            await TestInCSharp8(
@"class Program
{
    int M(int i)
    {
        [||]switch (i)
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
            await TestInCSharp8(
@"class Program
{
    int[] array = new int[1];

    int M(int i)
    {
        [||]switch (i)
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

    int M(int i)
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
            await TestMissingAsync(
@"class Program
{
    int[] array = new int[1];

    int M(int i)
    {
        [||]switch (i)
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
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertSwitchStatementToExpression)]
        public async Task TestMissingOnQualifiedName()
        {
            await TestMissingAsync(
@"class Program
{
    int[] array = new int[1];

    int M(int i)
    {
        [||]switch (i)
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
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertSwitchStatementToExpression)]
        public async Task TestMissingOnDefaultBreak_01()
        {
            await TestMissingAsync(
@"class Program
{
    void M(int i)
    {
        [||]switch (i)
        {
            default:
                break;
        }
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertSwitchStatementToExpression)]
        public async Task TestMissingOnDefaultBreak_02()
        {
            await TestMissingAsync(
@"class Program
{
    void M(int i)
    {
        [||]switch (i)
        {
            case _:
                break;
        }
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertSwitchStatementToExpression)]
        public async Task TestMissingOnDefaultBreak_03()
        {
            await TestMissingAsync(
@"class Program
{
    void M(int i)
    {
        [||]switch (i)
        {
            case var _:
                break;
        }
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertSwitchStatementToExpression)]
        public async Task TestMissingOnDefaultBreak_04()
        {
            await TestMissingAsync(
@"class Program
{
    void M(int i)
    {
        [||]switch (i)
        {
            case var x:
                break;
        }
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertSwitchStatementToExpression)]
        public async Task TestMissingOnAllBreak()
        {
            await TestMissingAsync(
@"class Program
{
    void M(int i)
    {
        [||]switch (i)
        {
            case 1:
                break;
            case 2:
                break;
            case 3:
                break;
        }
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertSwitchStatementToExpression)]
        public async Task TestAllThrow()
        {
            await TestInCSharp8(
@"class Program
{
    void M(int i)
    {
        [||]switch (i)
        {
            case 1:
                throw null;
            default:
                throw new Exception();
        }
    }
}",
@"class Program
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
            await TestInCSharp8(
@"class Program
{
    void M(int i)
    {
        int j;
        [||]switch (i)
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
            await TestMissingAsync(
@"class Program
{
    int M(int i)
    {
        int j = 0;
        [||]switch (i)
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
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertSwitchStatementToExpression)]
        public async Task TestMissingOnAssignmentMismatch()
        {
            await TestMissingAsync(
@"class Program
{
    int M(int i)
    {
        int j = 0;
        [||]switch (i)
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
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertSwitchStatementToExpression)]
        public async Task TestAssignment_Compound()
        {
            await TestInCSharp8(
@"class Program
{
    void M(int i)
    {
        int j = 0;
        [||]switch (i)
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
            await TestInCSharp8(
@"class Program
{
    void M(int i)
    {
        int j = 123;
        M(i);
        [||]switch (i)
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
            await TestMissingAsync(
@"class Program
{
    void M(int i)
    {
        int j, k;
        [||]switch (i)
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
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertSwitchStatementToExpression)]
        public async Task TestMissingONMultiCaseSection()
        {
            await TestMissingAsync(
@"class Program
{
    void M(int i)
    {
        int j;
        [||]switch (i)
        {
            case 1:
            case 2:
                j = 4;
                break;
        }
        throw null;
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertSwitchStatementToExpression)]
        public async Task TestMissingOnMultiCompoundAssignment()
        {
            await TestMissingAsync(
@"class Program
{
    void M(int i)
    {
        int j = 0, k = 0;
        [||]switch (i)
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
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertSwitchStatementToExpression)]
        public async Task TestMissingOnGoto()
        {
            await TestMissingAsync(
@"class Program
{
    int M(int i)
    {
        [||]switch (i)
        {
            case 1:
                return 0;
            case 2:
                goto default;
            default:
                return 2;
        }
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertSwitchStatementToExpression)]
        public async Task TestTrivia_01()
        {
            await TestInCSharp8(
@"class Program
{
    int M(int i)
    {
        // leading switch
        [||]switch (i) // trailing switch
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
            await TestInCSharp8(
@"class Program
{
    static int GetValue(int input)
    {
        [||]switch (input)
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
        [||]switch (i)
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
            var warningOption = new CodeStyleOption<bool>(true, NotificationOption.Warning);
            var options = Option(CSharpCodeStyleOptions.PreferSwitchExpression, warningOption);
            var testParameters = new TestParameters(options: options, parseOptions: TestOptions.Regular8);

            using var workspace = CreateWorkspaceFromOptions(source, testParameters);
            var diag = (await GetDiagnosticsAsync(workspace, testParameters)).Single();
            Assert.Equal(DiagnosticSeverity.Warning, diag.Severity);
            Assert.Equal(IDEDiagnosticIds.ConvertSwitchStatementToExpressionDiagnosticId, diag.Id);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertSwitchStatementToExpression)]
        [WorkItem(36995, "https://github.com/dotnet/roslyn/issues/36995")]
        public async Task TestAddParenthesesAroundBinaryExpression()
        {
            await TestInCSharp8(
@"class Program
{
    void M(int i)
    {
        int j = 123;
        [||]switch (i % 10)
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
            await TestInCSharp8(
@"using System;

class Program
{
    public static string FromDay(DayOfWeek dayOfWeek)
    {
        [||]switch (dayOfWeek)
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
            await TestMissingAsync(
@"using System;

class Program
{
    public static void Throw(int index)
    {
        string name = """";
        [||]switch (index)
        {
            case 0: name = ""1""; break;
            case 1: name = ""2""; break;
        }
        throw new Exception(name);
    }
}");
        }

        [WorkItem(36876, "https://github.com/dotnet/roslyn/issues/36876")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertSwitchStatementToExpression)]
        public async Task TestDeclarationInOuterScope()
        {
            await TestInCSharp8(
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
            [||]switch (origin)
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
            await TestMissingAsync(
@"class Program
{
    static void Main() { }

    static int GetValue(int input)
    {
        [||]switch (input)
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
}");
        }

        [WorkItem(37950, "https://github.com/dotnet/roslyn/issues/37950")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertSwitchStatementToExpression)]
        public async Task TestShouldNotCastNullOnNullableValueType_ReturnStatement()
        {
            await TestInCSharp8(
@"class Program
{
    public static bool? GetBool(string name)
    {
        [||]switch (name)
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
            await TestInCSharp8(
@"class Program
{
    public static void Test(string name)
    {
        bool? result;
        [||]switch (name)
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
        [||]switch (name)
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
            await TestInRegularAndScriptAsync(input, expected, options: Option(CSharpCodeStyleOptions.VarElsewhere, CodeStyleOptions.TrueWithSilentEnforcement), parseOptions: TestOptions.Regular8);
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
        [||]switch (name)
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
            await TestInRegularAndScriptAsync(input, expected, options: Option(CSharpCodeStyleOptions.VarElsewhere, CodeStyleOptions.TrueWithSilentEnforcement), parseOptions: TestOptions.Regular8);
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
        [||]switch (name)
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
            await TestInRegularAndScriptAsync(input, expected, options: Option(CSharpCodeStyleOptions.VarElsewhere, CodeStyleOptions.TrueWithSilentEnforcement), parseOptions: TestOptions.Regular8);
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
        [||]switch (name)
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
            await TestInRegularAndScriptAsync(input, expected, options: Option(CSharpCodeStyleOptions.VarElsewhere, CodeStyleOptions.TrueWithSilentEnforcement), parseOptions: TestOptions.Regular8);
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
        [||]switch (name)
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
            await TestInRegularAndScriptAsync(input, expected, options: Option(CSharpCodeStyleOptions.VarElsewhere, CodeStyleOptions.TrueWithSilentEnforcement), parseOptions: TestOptions.Regular8);
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
        [||]switch (name)
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
            await TestInRegularAndScriptAsync(input, expected, options: Option(CSharpCodeStyleOptions.VarElsewhere, CodeStyleOptions.TrueWithSilentEnforcement), parseOptions: TestOptions.Regular8);
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
        [||]switch (name)
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
            await TestInRegularAndScriptAsync(input, expected, options: Option(CSharpCodeStyleOptions.VarElsewhere, CodeStyleOptions.TrueWithSilentEnforcement), parseOptions: TestOptions.Regular8);
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
        [||]switch (a)
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
            await TestInRegularAndScriptAsync(input, expected, options: Option(CSharpCodeStyleOptions.VarForBuiltInTypes, CodeStyleOptions.TrueWithSilentEnforcement), parseOptions: TestOptions.Regular8);
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
        [||]switch (a)
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
            await TestInRegularAndScriptAsync(input, expected, options: Option(CSharpCodeStyleOptions.VarForBuiltInTypes, CodeStyleOptions.TrueWithSilentEnforcement), parseOptions: TestOptions.Regular8);
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
        [||]switch (a)
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
            await TestInRegularAndScriptAsync(input, expected, options: Option(CSharpCodeStyleOptions.VarForBuiltInTypes, CodeStyleOptions.TrueWithSilentEnforcement), parseOptions: TestOptions.Regular8);
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
        [||]switch (a)
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
            await TestInRegularAndScriptAsync(input, expected, options: Option(CSharpCodeStyleOptions.VarForBuiltInTypes, CodeStyleOptions.FalseWithSilentEnforcement), parseOptions: TestOptions.Regular8);
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
        [||]switch (a)
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
            await TestInRegularAndScriptAsync(input, expected, options: Option(CSharpCodeStyleOptions.VarForBuiltInTypes, CodeStyleOptions.TrueWithSilentEnforcement), parseOptions: TestOptions.Regular8);
        }

        [WorkItem(40198, "https://github.com/dotnet/roslyn/issues/40198")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertSwitchStatementToExpression)]
        public async Task TestNotWithRefReturns()
        {
            await TestMissingInRegularAndScriptAsync(
@"class Program
{
    static ref int GetRef(int[] mem, int addr, int mode)
    {
        [||]switch (mode)
        {
            case 0: return ref mem[mem[addr]];
            case 1: return ref mem[addr];
            default: throw new Exception();
        }
    }
}");
        }

        [WorkItem(40198, "https://github.com/dotnet/roslyn/issues/40198")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertSwitchStatementToExpression)]
        public async Task TestNotWithRefAssignment()
        {
            await TestMissingInRegularAndScriptAsync(
@"class Program
{
    static ref int GetRef(int[] mem, int addr, int mode)
    {
        ref int i = ref addr;
        [||]switch (mode)
        {
            case 0: i = ref mem[mem[addr]]; break;
            default: throw new Exception();
        }

        return ref mem[addr];
    }
}");
        }

        [WorkItem(40198, "https://github.com/dotnet/roslyn/issues/40198")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertSwitchStatementToExpression)]
        public async Task TestNotWithRefConditionalAssignment()
        {
            await TestMissingInRegularAndScriptAsync(
@"class Program
{
    static ref int GetRef(int[] mem, int addr, int mode)
    {
        ref int i = ref addr;
        [||]switch (mode)
        {
            case 0: i = ref true ? ref mem[mem[addr]] : ref mem[mem[addr]]; break;
            default: throw new Exception();
        }

        return ref mem[addr];
    }
}");
        }

        [WorkItem(40198, "https://github.com/dotnet/roslyn/issues/40198")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertSwitchStatementToExpression)]
        public async Task TestWithRefInsideConditionalAssignment()
        {
            await TestInRegularAndScript1Async(
@"class Program
{
    static void GetRef(int[] mem, int addr, int mode)
    {
        ref int i = ref addr;
        [||]switch (mode)
        {
            case 0: i = true ? ref mem[mem[addr]] : ref mem[mem[addr]]; break;
            default: throw new Exception();
        }
    }
}",
@"class Program
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
    }
}
