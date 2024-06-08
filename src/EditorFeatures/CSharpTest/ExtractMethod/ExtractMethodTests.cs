// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.ExtractMethod;
using Microsoft.CodeAnalysis.Editor.UnitTests;
using Microsoft.CodeAnalysis.Editor.UnitTests.Extensions;
using Microsoft.CodeAnalysis.ExtractMethod;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Text.Editor.Commanding.Commands;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.ExtractMethod;

[Trait(Traits.Feature, Traits.Features.ExtractMethod)]
public partial class ExtractMethodTests : ExtractMethodBase
{
    [Fact]
    public async Task ExtractMethod1()
    {
        var code = """
            using System;
            using System.Collections.Generic;
            using System.Linq;

            class Program
            {
                void Test(string[] args)
                {
                    [|int i;
                    i = 10;|]
                }
            }
            """;
        var expected = """
            using System;
            using System.Collections.Generic;
            using System.Linq;

            class Program
            {
                void Test(string[] args)
                {
                    NewMethod();
                }

                private static void NewMethod()
                {
                    int i = 10;
                }
            }
            """;

        await TestExtractMethodAsync(code, expected);
    }

    [Fact]
    public async Task ExtractMethod2()
    {
        var code = """
            using System;
            using System.Collections.Generic;
            using System.Linq;

            class Program
            {
                void Test(string[] args)
                {
                    [|int i = 10;
                    int i2 = 10;|]
                }
            }
            """;
        var expected = """
            using System;
            using System.Collections.Generic;
            using System.Linq;

            class Program
            {
                void Test(string[] args)
                {
                    NewMethod();
                }

                private static void NewMethod()
                {
                    int i = 10;
                    int i2 = 10;
                }
            }
            """;

        await TestExtractMethodAsync(code, expected);
    }

    [Fact]
    public async Task ExtractMethod3()
    {
        var code = """
            using System;
            using System.Collections.Generic;
            using System.Linq;

            class Program
            {
                void Test(string[] args)
                {
                    int i = 10;
                    [|int i2 = i;|]
                }
            }
            """;
        var expected = """
            using System;
            using System.Collections.Generic;
            using System.Linq;

            class Program
            {
                void Test(string[] args)
                {
                    int i = 10;
                    NewMethod(i);
                }

                private static void NewMethod(int i)
                {
                    int i2 = i;
                }
            }
            """;

        await TestExtractMethodAsync(code, expected);
    }

    [Fact]
    public async Task ExtractMethod4()
    {
        var code = """
            using System;
            using System.Collections.Generic;
            using System.Linq;

            class Program
            {
                void Test(string[] args)
                {
                    int i = 10;
                    int i2 = i;

                    [|i2 += i;|]
                }
            }
            """;
        var expected = """
            using System;
            using System.Collections.Generic;
            using System.Linq;

            class Program
            {
                void Test(string[] args)
                {
                    int i = 10;
                    int i2 = i;
                    i2 = NewMethod(i);
                }

                private static int NewMethod(int i, int i2)
                {
                    i2 += i;
                    return i2;
                }
            }
            """;

        // compoundaction not supported yet.
        await TestExtractMethodAsync(code, expected, temporaryFailing: true);
    }

    [Fact]
    public async Task ExtractMethod5()
    {
        var code = """
            using System;
            using System.Collections.Generic;
            using System.Linq;

            class Program
            {
                void Test(string[] args)
                {
                    int i = 10;
                    int i2 = i;

                    [|i2 = i;|]
                }
            }
            """;
        var expected = """
            using System;
            using System.Collections.Generic;
            using System.Linq;

            class Program
            {
                void Test(string[] args)
                {
                    int i = 10;
                    int i2 = i;

                    i2 = NewMethod(i);
                }

                private static int NewMethod(int i)
                {
                    return i;
                }
            }
            """;

        await TestExtractMethodAsync(code, expected);
    }

    [Fact]
    public async Task ExtractMethod6()
    {
        var code = """
            using System;
            using System.Collections.Generic;
            using System.Linq;

            class Program
            {
                int field;

                void Test(string[] args)
                {
                    int i = 10;

                    [|field = i;|]
                }
            }
            """;
        var expected = """
            using System;
            using System.Collections.Generic;
            using System.Linq;

            class Program
            {
                int field;

                void Test(string[] args)
                {
                    int i = 10;

                    NewMethod(i);
                }

                private void NewMethod(int i)
                {
                    field = i;
                }
            }
            """;

        await TestExtractMethodAsync(code, expected);
    }

    [Fact]
    public async Task ExtractMethod7()
    {
        var code = """
            using System;
            using System.Collections.Generic;
            using System.Linq;

            class Program
            {
                void Test(string[] args)
                {
                    string [] a = null;

                    [|Test(a);|]
                }
            }
            """;
        var expected = """
            using System;
            using System.Collections.Generic;
            using System.Linq;

            class Program
            {
                void Test(string[] args)
                {
                    string[] a = null;

                    NewMethod(a);
                }

                private void NewMethod(string[] a)
                {
                    Test(a);
                }
            }
            """;

        await TestExtractMethodAsync(code, expected);
    }

    [Fact]
    public async Task ExtractMethod8()
    {
        var code = """
            using System;
            using System.Collections.Generic;
            using System.Linq;

            class Program
            {
                static void Test(string[] args)
                {
                    string [] a = null;

                    [|Test(a);|]
                }
            }
            """;
        var expected = """
            using System;
            using System.Collections.Generic;
            using System.Linq;

            class Program
            {
                static void Test(string[] args)
                {
                    string[] a = null;

                    NewMethod(a);
                }

                private static void NewMethod(string[] a)
                {
                    Test(a);
                }
            }
            """;

        await TestExtractMethodAsync(code, expected);
    }

    [Fact]
    public async Task ExtractMethod9()
    {
        var code = """
            using System;
            using System.Collections.Generic;
            using System.Linq;

            class Program
            {
                void Test(string[] args)
                {
                    int i;
                    string s;

                    [|i = 10;
                    s = args[0] + i.ToString();|]
                }
            }
            """;
        var expected = """
            using System;
            using System.Collections.Generic;
            using System.Linq;

            class Program
            {
                void Test(string[] args)
                {
                    int i;
                    string s;

                    NewMethod(args, out i, out s);
                }

                private static void NewMethod(string[] args, out int i, out string s)
                {
                    i = 10;
                    s = args[0] + i.ToString();
                }
            }
            """;

        await TestExtractMethodAsync(code, expected);
    }

    [Fact]
    public async Task ExtractMethod10()
    {
        var code = """
            using System;
            using System.Collections.Generic;
            using System.Linq;

            class Program
            {
                void Test(string[] args)
                {
                    [|int i;
                    i = 10;

                    string s;

                    s = args[0] + i.ToString();|]

                    Console.WriteLine(s);
                }
            }
            """;
        var expected = """
            using System;
            using System.Collections.Generic;
            using System.Linq;

            class Program
            {
                void Test(string[] args)
                {
                    string s = NewMethod(args);

                    Console.WriteLine(s);
                }

                private static string NewMethod(string[] args)
                {
                    int i = 10;

                    string s;

                    s = args[0] + i.ToString();
                    return s;
                }
            }
            """;

        await TestExtractMethodAsync(code, expected);
    }

    [Fact]
    public async Task ExtractMethod11()
    {
        var code = """
            using System;
            using System.Collections.Generic;
            using System.Linq;

            class Program
            {
                void Test(string[] args)
                {
                    [|int i;
                    int i2 = 10;|]
                    i = 10;
                }
            }
            """;
        var expected = """
            using System;
            using System.Collections.Generic;
            using System.Linq;

            class Program
            {
                void Test(string[] args)
                {
                    int i;
                    NewMethod();
                    i = 10;
                }

                private static void NewMethod()
                {
                    int i2 = 10;
                }
            }
            """;

        await TestExtractMethodAsync(code, expected);
    }

    [Fact]
    public async Task ExtractMethod11_1()
    {
        var code = """
            using System;
            using System.Collections.Generic;
            using System.Linq;

            class Program
            {
                void Test(string[] args)
                {
                    [|int i;
                    int i2 = 10;|]
                }
            }
            """;
        var expected = """
            using System;
            using System.Collections.Generic;
            using System.Linq;

            class Program
            {
                void Test(string[] args)
                {
                    NewMethod();
                }

                private static void NewMethod()
                {
                    int i;
                    int i2 = 10;
                }
            }
            """;

        await TestExtractMethodAsync(code, expected);
    }

    [Fact]
    public async Task ExtractMethod12()
    {
        var code = """
            using System;
            using System.Collections.Generic;
            using System.Linq;

            class Program
            {
                void Test(string[] args)
                {
                    int i = 10;

                    [|i = i + 1;|]

                    Console.WriteLine(i);
                }
            }
            """;
        var expected = """
            using System;
            using System.Collections.Generic;
            using System.Linq;

            class Program
            {
                void Test(string[] args)
                {
                    int i = 10;

                    i = NewMethod(i);

                    Console.WriteLine(i);
                }

                private static int NewMethod(int i)
                {
                    i = i + 1;
                    return i;
                }
            }
            """;

        await TestExtractMethodAsync(code, expected);
    }

    [Fact]
    public async Task ControlVariableInForeachStatement()
    {
        var code = """
            using System;
            using System.Collections.Generic;
            using System.Linq;

            class Program
            {
                void Test(string[] args)
                {
                    foreach (var s in args)
                    {
                        [|Console.WriteLine(s);|]
                    }
                }
            }
            """;
        var expected = """
            using System;
            using System.Collections.Generic;
            using System.Linq;

            class Program
            {
                void Test(string[] args)
                {
                    foreach (var s in args)
                    {
                        NewMethod(s);
                    }
                }

                private static void NewMethod(string s)
                {
                    Console.WriteLine(s);
                }
            }
            """;

        await TestExtractMethodAsync(code, expected);
    }

    [Fact]
    public async Task ExtractMethod14()
    {
        var code = """
            using System;
            using System.Collections.Generic;
            using System.Linq;

            class Program
            {
                void Test(string[] args)
                {
                    for(var i = 1; i < 10; i++)
                    {
                        [|Console.WriteLine(i);|]
                    }
                }
            }
            """;
        var expected = """
            using System;
            using System.Collections.Generic;
            using System.Linq;

            class Program
            {
                void Test(string[] args)
                {
                    for (var i = 1; i < 10; i++)
                    {
                        NewMethod(i);
                    }
                }

                private static void NewMethod(int i)
                {
                    Console.WriteLine(i);
                }
            }
            """;

        // var in for loop doesn't get bound yet
        await TestExtractMethodAsync(code, expected, temporaryFailing: true);
    }

    [Fact]
    public async Task ExtractMethod15()
    {
        var code = """
            using System;
            using System.Collections.Generic;
            using System.Linq;

            class Program
            {
                void Test(string[] args)
                {
                    [|int s = 10, i = 1;
                    int b = s + i;|]

                    System.Console.WriteLine(s);
                    System.Console.WriteLine(i);
                }
            }
            """;
        var expected = """
            using System;
            using System.Collections.Generic;
            using System.Linq;

            class Program
            {
                void Test(string[] args)
                {
                    int s, i;
                    NewMethod(out s, out i);

                    System.Console.WriteLine(s);
                    System.Console.WriteLine(i);
                }

                private static void NewMethod(out int s, out int i)
                {
                    s = 10;
                    i = 1;
                    int b = s + i;
                }
            }
            """;

        await TestExtractMethodAsync(code, expected);
    }

    [Fact]
    public async Task ExtractMethod16()
    {
        var code = """
            using System;
            using System.Collections.Generic;
            using System.Linq;

            class Program
            {
                void Test(string[] args)
                {
                    [|int i = 1;|]

                    System.Console.WriteLine(i);
                }
            }
            """;
        var expected = """
            using System;
            using System.Collections.Generic;
            using System.Linq;

            class Program
            {
                void Test(string[] args)
                {
                    int i = NewMethod();

                    System.Console.WriteLine(i);
                }

                private static int NewMethod()
                {
                    return 1;
                }
            }
            """;

        await TestExtractMethodAsync(code, expected);
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538932")]
    public async Task ExtractMethod17()
    {
        var code = """
            using System;
            using System.Collections.Generic;
            using System.Linq;

            class Program
            {
                void Test<T>(out T t) where T : class, new()
                {
                    [|T t1;
                    Test(out t1);
                    t = t1;|]

                    System.Console.WriteLine(t1.ToString());
                }
            }
            """;
        var expected = """
            using System;
            using System.Collections.Generic;
            using System.Linq;

            class Program
            {
                void Test<T>(out T t) where T : class, new()
                {
                    T t1;
                    NewMethod(out t, out t1);

                    System.Console.WriteLine(t1.ToString());
                }

                private void NewMethod<T>(out T t, out T t1) where T : class, new()
                {
                    Test(out t1);
                    t = t1;
                }
            }
            """;

        await TestExtractMethodAsync(code, expected);
    }

    [Fact]
    public async Task ExtractMethod18()
    {
        var code = """
            using System;
            using System.Collections.Generic;
            using System.Linq;

            class Program
            {
                void Test<T>(out T t) where T : class, new()
                {
                    [|T t1 = GetValue(out t);|]
                    System.Console.WriteLine(t1.ToString());
                }

                private T GetValue<T>(out T t) where T : class, new()
                {
                    return t = new T();
                }
            }
            """;
        var expected = """
            using System;
            using System.Collections.Generic;
            using System.Linq;

            class Program
            {
                void Test<T>(out T t) where T : class, new()
                {
                    T t1;
                    NewMethod(out t, out t1);
                    System.Console.WriteLine(t1.ToString());
                }

                private void NewMethod<T>(out T t, out T t1) where T : class, new()
                {
                    t1 = GetValue(out t);
                }

                private T GetValue<T>(out T t) where T : class, new()
                {
                    return t = new T();
                }
            }
            """;

        await TestExtractMethodAsync(code, expected);
    }

    [Fact]
    public async Task ExtractMethod19()
    {
        var code = """
            using System;
            using System.Collections.Generic;
            using System.Linq;

            unsafe class Program
            {
                void Test()
                {
                    [|int i = 1;|]
                }
            }
            """;
        var expected = """
            using System;
            using System.Collections.Generic;
            using System.Linq;

            unsafe class Program
            {
                void Test()
                {
                    NewMethod();
                }

                private static void NewMethod()
                {
                    int i = 1;
                }
            }
            """;

        await TestExtractMethodAsync(code, expected);
    }

    [Fact]
    public async Task ExtractMethod20()
    {
        var code = """
            using System;
            using System.Collections.Generic;
            using System.Linq;

            class Program
            {
                unsafe void Test()
                {
                    [|int i = 1;|]
                }
            }
            """;
        var expected = """
            using System;
            using System.Collections.Generic;
            using System.Linq;

            class Program
            {
                unsafe void Test()
                {
                    NewMethod();
                }

                private static unsafe void NewMethod()
                {
                    int i = 1;
                }
            }
            """;

        await TestExtractMethodAsync(code, expected);
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542677")]
    public async Task ExtractMethod21()
    {
        var code = """
            using System;
            using System.Collections.Generic;
            using System.Linq;

            class Program
            {
                void Test()
                {
                    unsafe
                    {
                        [|int i = 1;|]
                    }
                }
            }
            """;
        var expected = """
            using System;
            using System.Collections.Generic;
            using System.Linq;

            class Program
            {
                void Test()
                {
                    unsafe
                    {
                        NewMethod();
                    }
                }

                private static unsafe void NewMethod()
                {
                    int i = 1;
                }
            }
            """;

        await TestExtractMethodAsync(code, expected);
    }

    [Fact]
    public async Task ExtractMethod22()
    {
        var code = """
            using System;
            using System.Collections.Generic;
            using System.Linq;

            class Program
            {
                void Test()
                {
                    int i;

                    [|int b = 10;
                    if (b < 10)
                    {
                        i = 5;
                    }|]

                    i = 6;
                    Console.WriteLine(i);
                }
            }
            """;
        var expected = """
            using System;
            using System.Collections.Generic;
            using System.Linq;

            class Program
            {
                void Test()
                {
                    int i;

                    i = NewMethod(i);

                    i = 6;
                    Console.WriteLine(i);
                }

                private static int NewMethod(int i)
                {
                    int b = 10;
                    if (b < 10)
                    {
                        i = 5;
                    }

                    return i;
                }
            }
            """;

        await TestExtractMethodAsync(code, expected);
    }

    [Fact]
    public async Task ExtractMethod23()
    {
        var code = """
            using System;
            using System.Collections.Generic;
            using System.Linq;

            class Program
            {
                static void Main(string[] args)
                {
                    if (true)
                        [|Console.WriteLine(args[0].ToString());|]
                }
            }
            """;
        var expected = """
            using System;
            using System.Collections.Generic;
            using System.Linq;

            class Program
            {
                static void Main(string[] args)
                {
                    if (true)
                        NewMethod(args);
                }

                private static void NewMethod(string[] args)
                {
                    Console.WriteLine(args[0].ToString());
                }
            }
            """;

        await TestExtractMethodAsync(code, expected);
    }

    [Fact]
    public async Task ExtractMethod24()
    {
        var code = """
            using System;

            class Program
            {
                static void Main(string[] args)
                {
                    int y = [|int.Parse(args[0].ToString())|];
                }
            }
            """;
        var expected = """
            using System;

            class Program
            {
                static void Main(string[] args)
                {
                    int y = GetY(args);
                }

                private static int GetY(string[] args)
                {
                    return int.Parse(args[0].ToString());
                }
            }
            """;
        await TestExtractMethodAsync(code, expected);
    }

    [Fact]
    public async Task ExtractMethod25()
    {
        var code = """
            using System;
            using System.Collections.Generic;
            using System.Linq;

            class Program
            {
                static void Main(string[] args)
                {
                    if (([|new int[] { 1, 2, 3 }|]).Any())
                    {
                        return;
                    }
                }
            }
            """;
        var expected = """
            using System;
            using System.Collections.Generic;
            using System.Linq;

            class Program
            {
                static void Main(string[] args)
                {
                    if ((NewMethod()).Any())
                    {
                        return;
                    }
                }

                private static int[] NewMethod()
                {
                    return new int[] { 1, 2, 3 };
                }
            }
            """;

        await TestExtractMethodAsync(code, expected);
    }

    [Fact]
    public async Task ExtractMethod26()
    {
        var code = """
            using System;
            using System.Collections.Generic;
            using System.Linq;

            class Program
            {
                static void Main(string[] args)
                {
                    if ([|(new int[] { 1, 2, 3 })|].Any())
                    {
                        return;
                    }
                }
            }
            """;
        var expected = """
            using System;
            using System.Collections.Generic;
            using System.Linq;

            class Program
            {
                static void Main(string[] args)
                {
                    if (NewMethod().Any())
                    {
                        return;
                    }
                }

                private static int[] NewMethod()
                {
                    return (new int[] { 1, 2, 3 });
                }
            }
            """;

        await TestExtractMethodAsync(code, expected);
    }

    [Fact]
    public async Task ExtractMethod27()
    {
        var code = """
            using System;
            using System.Collections.Generic;
            using System.Linq;

            class Program
            {
                void Test()
                {
                    int i = 1;

                    [|int b = 10;
                    if (b < 10)
                    {
                        i = 5;
                    }|]

                    i = 6;
                    Console.WriteLine(i);
                }
            }
            """;
        var expected = """
            using System;
            using System.Collections.Generic;
            using System.Linq;

            class Program
            {
                void Test()
                {
                    int i = 1;

                    i = NewMethod(i);

                    i = 6;
                    Console.WriteLine(i);
                }

                private static int NewMethod(int i)
                {
                    int b = 10;
                    if (b < 10)
                    {
                        i = 5;
                    }

                    return i;
                }
            }
            """;

        await TestExtractMethodAsync(code, expected);
    }

    [Fact]
    public async Task ExtractMethod28()
    {
        var code = """
            using System;
            using System.Collections.Generic;
            using System.Linq;

            class Program
            {
                int Test()
                {
                    [|return 1;|]
                }
            }
            """;
        var expected = """
            using System;
            using System.Collections.Generic;
            using System.Linq;

            class Program
            {
                int Test()
                {
                    return NewMethod();
                }

                private static int NewMethod()
                {
                    return 1;
                }
            }
            """;

        await TestExtractMethodAsync(code, expected);
    }

    [Fact]
    public async Task ExtractMethod29()
    {
        var code = """
            using System;
            using System.Collections.Generic;
            using System.Linq;

            class Program
            {
                int Test()
                {
                    int i = 0;

                    [|if (i < 0)
                    {
                        return 1;
                    }
                    else
                    {
                        return 0;
                    }|]
                }
            }
            """;
        var expected = """
            using System;
            using System.Collections.Generic;
            using System.Linq;

            class Program
            {
                int Test()
                {
                    int i = 0;

                    return NewMethod(i);
                }

                private static int NewMethod(int i)
                {
                    if (i < 0)
                    {
                        return 1;
                    }
                    else
                    {
                        return 0;
                    }
                }
            }
            """;

        await TestExtractMethodAsync(code, expected);
    }

    [Fact]
    public async Task ExtractMethod30()
    {
        var code = """
            using System;
            using System.Collections.Generic;
            using System.Linq;

            class Program
            {
                void Test(out int i)
                {
                    [|i = 10;|]
                }
            }
            """;
        var expected = """
            using System;
            using System.Collections.Generic;
            using System.Linq;

            class Program
            {
                void Test(out int i)
                {
                    i = NewMethod();
                }

                private static int NewMethod()
                {
                    return 10;
                }
            }
            """;

        await TestExtractMethodAsync(code, expected);
    }

    [Fact]
    public async Task ExtractMethod31()
    {
        var code = """
            using System;
            using System.Collections.Generic;
            using System.Text;

            class Program
            {
                void Test()
                {
                    StringBuilder builder = new StringBuilder();
                    [|builder.Append("Hello");
                    builder.Append(" From ");
                    builder.Append(" Roslyn");|]
                    return builder.ToString();
                }
            }
            """;
        var expected = """
            using System;
            using System.Collections.Generic;
            using System.Text;

            class Program
            {
                void Test()
                {
                    StringBuilder builder = new StringBuilder();
                    NewMethod(builder);
                    return builder.ToString();
                }

                private static void NewMethod(StringBuilder builder)
                {
                    builder.Append("Hello");
                    builder.Append(" From ");
                    builder.Append(" Roslyn");
                }
            }
            """;

        await TestExtractMethodAsync(code, expected);
    }

    [Fact]
    public async Task ExtractMethod32()
    {
        var code = """
            using System;

            class Program
            {
                void Test()
                {
                    int v = 0;
                    Console.Write([|v|]);
                }
            }
            """;
        var expected = """
            using System;

            class Program
            {
                void Test()
                {
                    int v = 0;
                    Console.Write(GetV(v));
                }

                private static int GetV(int v)
                {
                    return v;
                }
            }
            """;

        await TestExtractMethodAsync(code, expected);
    }

    [Fact, WorkItem(3792, "DevDiv_Projects/Roslyn")]
    public async Task ExtractMethod33()
    {
        var code = """
            using System;

            class Program
            {
                void Test()
                {
                    int v = 0;
                    while (true)
                    {
                        Console.Write([|v++|]);
                    }
                }
            }
            """;
        var expected = """
            using System;

            class Program
            {
                void Test()
                {
                    int v = 0;
                    while (true)
                    {
                        Console.Write(NewMethod(ref v));
                    }
                }

                private static int NewMethod(ref int v)
                {
                    return v++;
                }
            }
            """;

        // this bug has two issues. one is "v" being not in the dataFlowIn and ReadInside collection (hence no method parameter)
        // and the other is binding not working for "v++" (hence object as return type)
        await TestExtractMethodAsync(code, expected);
    }

    [Fact]
    public async Task ExtractMethod34()
    {
        var code = """
            using System;

            class Program
            {
                static void Main(string[] args)
                {
                    int x = 1;
                    int y = 2;
                    int z = [|x + y|];
                }
            }
            """;
        var expected = """
            using System;

            class Program
            {
                static void Main(string[] args)
                {
                    int x = 1;
                    int y = 2;
                    int z = GetZ(x, y);
                }

                private static int GetZ(int x, int y)
                {
                    return x + y;
                }
            }
            """;

        await TestExtractMethodAsync(code, expected);
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538239")]
    public async Task ExtractMethod35()
    {
        var code = """
            using System;

            class Program
            {
                static void Main(string[] args)
                {
                    int[] r = [|new int[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20 }|];
                }
            }
            """;
        var expected = """
            using System;

            class Program
            {
                static void Main(string[] args)
                {
                    int[] r = GetR();
                }

                private static int[] GetR()
                {
                    return new int[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20 };
                }
            }
            """;

        await TestExtractMethodAsync(code, expected);
    }

    [Fact]
    public async Task ExtractMethod36()
    {
        var code = """
            using System;

            class Program
            {
                static void Main(ref int i)
                {
                    [|i = 1;|]
                }
            }
            """;
        var expected = """
            using System;

            class Program
            {
                static void Main(ref int i)
                {
                    i = NewMethod();
                }

                private static int NewMethod()
                {
                    return 1;
                }
            }
            """;

        await TestExtractMethodAsync(code, expected);
    }

    [Fact]
    public async Task ExtractMethod37()
    {
        var code = """
            using System;

            class Program
            {
                static void Main(out int i)
                {
                    [|i = 1;|]
                }
            }
            """;
        var expected = """
            using System;

            class Program
            {
                static void Main(out int i)
                {
                    i = NewMethod();
                }

                private static int NewMethod()
                {
                    return 1;
                }
            }
            """;

        await TestExtractMethodAsync(code, expected);
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538231")]
    public async Task ExtractMethod38()
    {
        var code = """
            using System;

            class Program
            {
                static void Main(string[] args)
                {
                     // int v = 0;
                     // while (true)
                     // {
                     // NewMethod(v++);
                     // NewMethod(ReturnVal(v++));
                     // }

                    int unassigned;
                    // extract
                    // unassigned = ReturnVal(0);
                    [|unassigned = unassigned + 10;|]

                    // read 
                    // int newVar = unassigned;

                    // write
                    // unassigned = 0;
                }
            }
            """;
        var expected = """
            using System;

            class Program
            {
                static void Main(string[] args)
                {
                    // int v = 0;
                    // while (true)
                    // {
                    // NewMethod(v++);
                    // NewMethod(ReturnVal(v++));
                    // }

                    int unassigned;
                    // extract
                    // unassigned = ReturnVal(0);
                    unassigned = NewMethod(unassigned);

                    // read 
                    // int newVar = unassigned;

                    // write
                    // unassigned = 0;
                }

                private static int NewMethod(int unassigned)
                {
                    unassigned = unassigned + 10;
                    return unassigned;
                }
            }
            """;

        await TestExtractMethodAsync(code, expected);
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538231")]
    public async Task ExtractMethod39()
    {
        var code = """
            using System;

            class Program
            {
                static void Main(string[] args)
                {
                     // int v = 0;
                     // while (true)
                     // {
                     // NewMethod(v++);
                     // NewMethod(ReturnVal(v++));
                     // }

                    int unassigned;
                    // extract
                    [|// unassigned = ReturnVal(0);
                    unassigned = unassigned + 10;

                    // read|] 
                    // int newVar = unassigned;

                    // write
                    // unassigned = 0;
                }
            }
            """;
        var expected = """
            using System;

            class Program
            {
                static void Main(string[] args)
                {
                    // int v = 0;
                    // while (true)
                    // {
                    // NewMethod(v++);
                    // NewMethod(ReturnVal(v++));
                    // }

                    int unassigned;
                    // extract
                    unassigned = NewMethod(unassigned);
                    // int newVar = unassigned;

                    // write
                    // unassigned = 0;
                }

                private static int NewMethod(int unassigned)
                {
                    // unassigned = ReturnVal(0);
                    unassigned = unassigned + 10;

                    // read 
                    return unassigned;
                }
            }
            """;

        // current bottom-up re-writer makes re-attaching trivia half belongs to previous token
        // and half belongs to next token very hard.
        // for now, it won't be able to re-associate trivia belongs to next token.
        await TestExtractMethodAsync(code, expected);
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538303")]
    public async Task ExtractMethod40()
    {
        var code = """
            class Program
            {
                static void Main(string[] args)
                {
                    [|int x;|]
                }
            }
            """;
        await ExpectExtractMethodToFailAsync(code);
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/868414")]
    public async Task ExtractMethodWithLeadingTrivia()
    {
        // ensure that the extraction doesn't result in trivia moving up a line:
        //        // a        //b
        //        NewMethod();
        await TestExtractMethodAsync(
            """
            class C
            {
                void M()
                {
                    // a
                    // b
                    [|System.Console.WriteLine();|]
                }
            }
            """,
            """
            class C
            {
                void M()
                {
                    // a
                    // b
                    NewMethod();
                }

                private static void NewMethod()
                {
                    System.Console.WriteLine();
                }
            }
            """);
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/632351")]
    public async Task ExtractMethodFailForTypeInFromClause()
    {
        var code = """
            class Program
            {
                static void Main(string[] args)
                {
                    var z = from [|T|] x in e;
                }
            }
            """;
        await ExpectExtractMethodToFailAsync(code);
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/632351")]
    public async Task ExtractMethodFailForTypeInFromClause_1()
    {
        var code = """
            class Program
            {
                static void Main(string[] args)
                {
                    var z = from [|W.T|] x in e;
                }
            }
            """;
        await ExpectExtractMethodToFailAsync(code);
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538314")]
    public async Task ExtractMethod41()
    {
        var code = """
            class Program
            {
                static void Main(string[] args)
                {
                    int x = 10;
                    [|int y;
                    if (x == 10)
                        y = 5;|]
                    Console.WriteLine(y);
                }
            }
            """;
        var expected = """
            class Program
            {
                static void Main(string[] args)
                {
                    int x = 10;
                    int y = NewMethod(x);
                    Console.WriteLine(y);
                }

                private static int NewMethod(int x)
                {
                    int y;
                    if (x == 10)
                        y = 5;
                    return y;
                }
            }
            """;
        await TestExtractMethodAsync(code, expected);
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538327")]
    public async Task ExtractMethod42()
    {
        var code = """
            using System;

            class Program
            {
                static void Main(string[] args)
                {
                    int a, b;
                    [|a = 5;
                    b = 7;|]
                    Console.Write(a + b);
                }
            }
            """;
        var expected = """
            using System;

            class Program
            {
                static void Main(string[] args)
                {
                    int a, b;
                    NewMethod(out a, out b);
                    Console.Write(a + b);
                }

                private static void NewMethod(out int a, out int b)
                {
                    a = 5;
                    b = 7;
                }
            }
            """;

        await TestExtractMethodAsync(code, expected);
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538327")]
    public async Task ExtractMethod43()
    {
        var code = """
            using System;

            class Program
            {
                static void Main(string[] args)
                {
                    int a, b;
                    [|a = 5;
                    b = 7;
                    int c;
                    int d;
                    int e, f;
                    c = 1;
                    d = 1;
                    e = 1;
                    f = 1;|]
                    Console.Write(a + b);
                }
            }
            """;
        var expected = """
            using System;

            class Program
            {
                static void Main(string[] args)
                {
                    int a, b;
                    NewMethod(out a, out b);
                    Console.Write(a + b);
                }

                private static void NewMethod(out int a, out int b)
                {
                    a = 5;
                    b = 7;
                    int c;
                    int d;
                    int e, f;
                    c = 1;
                    d = 1;
                    e = 1;
                    f = 1;
                }
            }
            """;

        await TestExtractMethodAsync(code, expected);
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538328")]
    public async Task ExtractMethod44()
    {
        var code = """
            using System;

            class Program
            {
                static void Main(string[] args)
                {
                    int a;
                    //modified in
                    [|a = 1;|]
                    /*data flow out*/
                    Console.Write(a);
                }
            }
            """;
        var expected = """
            using System;

            class Program
            {
                static void Main(string[] args)
                {
                    int a;
                    //modified in
                    a = NewMethod();
                    /*data flow out*/
                    Console.Write(a);
                }

                private static int NewMethod()
                {
                    return 1;
                }
            }
            """;

        await TestExtractMethodAsync(code, expected);
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538393")]
    public async Task ExtractMethod45()
    {
        var code = """
            using System;
            using System.Collections.Generic;
            using System.Linq;
            class Program
            {
                static void Main(string[] args)
                {
                    /**/[|;|]/**/
                }
            }
            """;
        var expected = """
            using System;
            using System.Collections.Generic;
            using System.Linq;
            class Program
            {
                static void Main(string[] args)
                {
                    /**/
                    NewMethod();/**/
                }

                private static void NewMethod()
                {
                    ;
                }
            }
            """;

        await TestExtractMethodAsync(code, expected);
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538393")]
    public async Task ExtractMethod46()
    {
        var code = """
            using System;
            using System.Collections.Generic;
            using System.Linq;

            class Program
            {
                static void Main()
                {
                    int x = 1;
                    [|Goo(ref x);|]
                    Console.WriteLine(x);
                }

                static void Goo(ref int x)
                {
                    x = x + 1;
                }
            }
            """;
        var expected = """
            using System;
            using System.Collections.Generic;
            using System.Linq;

            class Program
            {
                static void Main()
                {
                    int x = 1;
                    x = NewMethod(x);
                    Console.WriteLine(x);
                }

                private static int NewMethod(int x)
                {
                    Goo(ref x);
                    return x;
                }

                static void Goo(ref int x)
                {
                    x = x + 1;
                }
            }
            """;
        await TestExtractMethodAsync(code, expected);
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538399")]
    public async Task ExtractMethod47()
    {
        var code = """
            using System;
            using System.Collections.Generic;
            using System.Linq;

            class Program
            {
                static void Main()
                {
                    int x = 1;
                    [|while (true) Console.WriteLine(x);|]
                }
            }
            """;
        var expected = """
            using System;
            using System.Collections.Generic;
            using System.Linq;

            class Program
            {
                static void Main()
                {
                    int x = 1;
                    NewMethod(x);
                }

                private static void NewMethod(int x)
                {
                    while (true) Console.WriteLine(x);
                }
            }
            """;

        await TestExtractMethodAsync(code, expected);
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538401")]
    public async Task ExtractMethod48()
    {
        var code = """
            using System;
            using System.Collections.Generic;
            using System.Linq;

            class Program
            {
                static void Main()
                {
                    int[] x = [|{ 1, 2, 3 }|];
                }
            }
            """;
        var expected = """
            using System;
            using System.Collections.Generic;
            using System.Linq;

            class Program
            {
                static void Main()
                {
                    int[] x = GetX();
                }

                private static int[] GetX()
                {
                    return new int[] { 1, 2, 3 };
                }
            }
            """;

        await TestExtractMethodAsync(code, expected);
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538405")]
    public async Task ExtractMethod49()
    {
        var code = """
            using System;
            using System.Collections.Generic;
            using System.Linq;
            class Program
            {
                static void Goo(int GetX)
                {
                    int x = [|1|];
                }
            }
            """;
        var expected = """
            using System;
            using System.Collections.Generic;
            using System.Linq;
            class Program
            {
                static void Goo(int GetX)
                {
                    int x = GetX1();
                }

                private static int GetX1()
                {
                    return 1;
                }
            }
            """;

        await TestExtractMethodAsync(code, expected);
    }

    [Fact]
    public async Task ExtractMethodNormalProperty()
    {
        var code = """
            class Class
            {
                private static string name;
                public static string Names
                {
                    get { return "1"; }
                    set { name = value; }
                }
                static void Goo(int i)
                {
                    string str = [|Class.Names|];
                }
            }
            """;
        var expected = """
            class Class
            {
                private static string name;
                public static string Names
                {
                    get { return "1"; }
                    set { name = value; }
                }
                static void Goo(int i)
                {
                    string str = GetStr();
                }

                private static string GetStr()
                {
                    return Class.Names;
                }
            }
            """;

        await TestExtractMethodAsync(code, expected);
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538932")]
    public async Task ExtractMethodAutoProperty()
    {
        var code = """
            class Class
            {
                public string Name { get; set; }
                static void Main()
                {
                   string str = new Class().[|Name|];
                }
            }
            """;

        // given span is not an expression
        // selection validator should take care of this case

        var expected = """
            class Class
            {
                public string Name { get; set; }
                static void Main()
                {
                    string str = GetStr();
                }

                private static string GetStr()
                {
                    return new Class().Name;
                }
            }
            """;

        await TestExtractMethodAsync(code, expected);
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538402")]
    public async Task BugFix3994()
    {
        var code = """
            using System;
            using System.Collections.Generic;
            using System.Linq;

            class Program
            {
                static void Main()
                {
                    byte x = [|1|];
                }
            }
            """;
        var expected = """
            using System;
            using System.Collections.Generic;
            using System.Linq;

            class Program
            {
                static void Main()
                {
                    byte x = GetX();
                }

                private static byte GetX()
                {
                    return 1;
                }
            }
            """;

        await TestExtractMethodAsync(code, expected);
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538404")]
    public async Task BugFix3996()
    {
        var code = """
            class A<T>
            {
                class D : A<T> { }
                class B { }

                static D.B Goo()
                {
                    return null;
                }

                class C<T2>
                {
                    static void Bar()
                    {
                        D.B x = [|Goo()|];
                    }
                }
            }
            """;

        var expected = """
            class A<T>
            {
                class D : A<T> { }
                class B { }

                static D.B Goo()
                {
                    return null;
                }

                class C<T2>
                {
                    static void Bar()
                    {
                        D.B x = GetX();
                    }

                    private static B GetX()
                    {
                        return Goo();
                    }
                }
            }
            """;

        await TestExtractMethodAsync(code, expected);
    }

    [Fact]
    public async Task InsertionPoint()
    {
        var code = """
            class Test
            {
                void Method(string i)
                {
                    int y2 = [|1|];
                }

                void Method(int i)
                {
                }
            }
            """;

        var expected = """
            class Test
            {
                void Method(string i)
                {
                    int y2 = GetY2();
                }

                private static int GetY2()
                {
                    return 1;
                }

                void Method(int i)
                {
                }
            }
            """;

        await TestExtractMethodAsync(code, expected);
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538980")]
    public async Task BugFix4757()
    {
        var code = """
            class GenericMethod
            {
                void Method<T>(T t)
                {
                    T a;
                    [|a = t;|]
                }
            }
            """;

        var expected = """
            class GenericMethod
            {
                void Method<T>(T t)
                {
                    T a;
                    a = NewMethod(t);
                }

                private static T NewMethod<T>(T t)
                {
                    return t;
                }
            }
            """;

        await TestExtractMethodAsync(code, expected);
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538980")]
    public async Task BugFix4757_2()
    {
        var code = """
            class GenericMethod<T1>
            {
                void Method<T>(T t)
                {
                    T a;
                    T1 b;
                    [|a = t;
                    b = default(T1);|]
                }
            }
            """;

        var expected = """
            class GenericMethod<T1>
            {
                void Method<T>(T t)
                {
                    T a;
                    T1 b;
                    NewMethod(t, out a, out b);
                }

                private static void NewMethod<T>(T t, out T a, out T1 b)
                {
                    a = t;
                    b = default(T1);
                }
            }
            """;

        await TestExtractMethodAsync(code, expected);
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538980")]
    public async Task BugFix4757_3()
    {
        var code = """
            class GenericMethod
            {
                void Method<T, T1>(T t)
                {
                    T1 a1;
                    T a;
                    [|a = t;
                    a1 = default(T);|]
                }
            }
            """;

        var expected = """
            class GenericMethod
            {
                void Method<T, T1>(T t)
                {
                    T1 a1;
                    T a;
                    NewMethod(t, out a1, out a);
                }

                private static void NewMethod<T, T1>(T t, out T1 a1, out T a)
                {
                    a = t;
                    a1 = default(T);
                }
            }
            """;

        await TestExtractMethodAsync(code, expected);
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538422")]
    public async Task BugFix4758()
    {
        var code = """
            using System;
            class TestOutParameter
            {
                void Method(out int x)
                {
                    x = 5;
                    Console.Write([|x|]);
                }
            }
            """;

        var expected = """
            using System;
            class TestOutParameter
            {
                void Method(out int x)
                {
                    x = 5;
                    Console.Write(GetX(x));
                }

                private static int GetX(int x)
                {
                    return x;
                }
            }
            """;

        await TestExtractMethodAsync(code, expected);
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538422")]
    public async Task BugFix4758_2()
    {
        var code = """
            class TestOutParameter
            {
                void Method(out int x)
                {
                    x = 5;
                    Console.Write([|x|]);
                }
            }
            """;

        var expected = """
            class TestOutParameter
            {
                void Method(out int x)
                {
                    x = 5;
                    Console.Write(GetX(x));
                }

                private static int GetX(int x)
                {
                    return x;
                }
            }
            """;

        await TestExtractMethodAsync(code, expected);
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538984")]
    public async Task BugFix4761()
    {
        var code = """
            using System;

            class A
            {
                void Method()
                {
                    System.Func<int, int> a = x => [|x * x|];
                }
            }
            """;

        var expected = """
            using System;

            class A
            {
                void Method()
                {
                    System.Func<int, int> a = x => NewMethod(x);
                }

                private static int NewMethod(int x)
                {
                    return x * x;
                }
            }
            """;

        await TestExtractMethodAsync(code, expected);
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538997")]
    public async Task BugFix4779()
    {
        var code = """
            using System;

            class Program
            {
                static void Main()
                {
                    string s = ";
                    Func<string> f = [|s|].ToString;
                }
            }
            """;

        var expected = """
            using System;

            class Program
            {
                static void Main()
                {
                    string s = ";
                    Func<string> f = GetS(s).ToString;
                }

                private static string GetS(string s)
                {
                    return s;
                }
            }
            """;

        await TestExtractMethodAsync(code, expected);
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538997")]
    public async Task BugFix4779_2()
    {
        var code = """
            using System;

            class Program
            {
                static void Main()
                {
                    string s = ";
                    var f = [|s|].ToString();
                }
            }
            """;

        var expected = """
            using System;

            class Program
            {
                static void Main()
                {
                    string s = ";
                    var f = GetS(s).ToString();
                }

                private static string GetS(string s)
                {
                    return s;
                }
            }
            """;

        await TestExtractMethodAsync(code, expected);
    }

    [Fact, WorkItem(4780, "DevDiv_Projects/Roslyn")]
    public async Task BugFix4780()
    {
        var code = """
            using System;

            class Program
            {
                static void Main()
                {
                    string s = ";
                    object f = (Func<string>)[|s.ToString|];
                }
            }
            """;

        var expected = """
            using System;

            class Program
            {
                static void Main()
                {
                    string s = ";
                    object f = (Func<string>)GetToString(s);
                }

                private static Func<string> GetToString(string s)
                {
                    return s.ToString;
                }
            }
            """;

        await TestExtractMethodAsync(code, expected);
    }

    [Fact, WorkItem(4780, "DevDiv_Projects/Roslyn")]
    public async Task BugFix4780_2()
    {
        var code = """
            using System;

            class Program
            {
                static void Main()
                {
                    string s = ";
                    object f = (string)[|s.ToString()|];
                }
            }
            """;

        var expected = """
            using System;

            class Program
            {
                static void Main()
                {
                    string s = ";
                    object f = (string)NewMethod(s);
                }

                private static string NewMethod(string s)
                {
                    return s.ToString();
                }
            }
            """;

        await TestExtractMethodAsync(code, expected);
    }

    [Fact, WorkItem(4782, "DevDiv_Projects/Roslyn")]
    public async Task BugFix4782()
    {
        var code = """
            class A<T>
            {
                class D : A<T[]> { }
                class B { }

                class C<T>
                {
                    static void Goo<T>(T a)
                    {
                        T t = [|default(T)|];
                    }
                }
            }
            """;

        var expected = """
            class A<T>
            {
                class D : A<T[]> { }
                class B { }

                class C<T>
                {
                    static void Goo<T>(T a)
                    {
                        T t = GetT<T>();
                    }

                    private static T GetT<T>()
                    {
                        return default(T);
                    }
                }
            }
            """;
        await TestExtractMethodAsync(code, expected);
    }

    [Fact, WorkItem(4782, "DevDiv_Projects/Roslyn")]
    public async Task BugFix4782_2()
    {
        var code = """
            class A<T>
            {
                class D : A<T[]> { }
                class B { }

                class C<T>
                {
                    static void Goo()
                    {
                        D.B x = [|new D.B()|];
                    }
                }
            }
            """;

        await ExpectExtractMethodToFailAsync(code);
    }

    [Fact, WorkItem(4791, "DevDiv_Projects/Roslyn")]
    public async Task BugFix4791()
    {
        var code = """
            class Program
            {
                delegate int Func(int a);

                static void Main(string[] args)
                {
                    Func v = (int a) => [|a|];
                }
            }
            """;

        var expected = """
            class Program
            {
                delegate int Func(int a);

                static void Main(string[] args)
                {
                    Func v = (int a) => GetA(a);
                }

                private static int GetA(int a)
                {
                    return a;
                }
            }
            """;

        await TestExtractMethodAsync(code, expected);
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539019")]
    public async Task BugFix4809()
    {
        var code = """
            class Program
            {
                public Program()
                {
                    [|int x = 2;|]
                }
            }
            """;

        var expected = """
            class Program
            {
                public Program()
                {
                    NewMethod();
                }

                private static void NewMethod()
                {
                    int x = 2;
                }
            }
            """;

        await TestExtractMethodAsync(code, expected);
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539029")]
    public async Task BugFix4813()
    {
        var code = """
            using System;

            class Program
            {
                public Program()
                {
                    object o = [|new Program()|];
                }
            }
            """;

        var expected = """
            using System;

            class Program
            {
                public Program()
                {
                    object o = GetO();
                }

                private static Program GetO()
                {
                    return new Program();
                }
            }
            """;

        await TestExtractMethodAsync(code, expected);
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538425")]
    public async Task BugFix4031()
    {
        var code = """
            using System;
            using System.Collections.Generic;
            using System.Linq;
            class Program
            {
                static void Main()
                {
                    bool x = true, y = true, z = true;
                    if (x)
                        while (y) { }
                    else
                        [|while (z) { }|]
                }
            }
            """;

        var expected = """
            using System;
            using System.Collections.Generic;
            using System.Linq;
            class Program
            {
                static void Main()
                {
                    bool x = true, y = true, z = true;
                    if (x)
                        while (y) { }
                    else
                        NewMethod(z);
                }

                private static void NewMethod(bool z)
                {
                    while (z) { }
                }
            }
            """;

        await TestExtractMethodAsync(code, expected);
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/527499")]
    public async Task BugFix3992()
    {
        var code = """
            using System;
            using System.Collections.Generic;
            using System.Linq;
            class Program
            {
                static void Main()
                {
                    int x = 1;
                    [|while (false) Console.WriteLine(x);|]
                }
            }
            """;

        var expected = """
            using System;
            using System.Collections.Generic;
            using System.Linq;
            class Program
            {
                static void Main()
                {
                    int x = 1;
                    NewMethod(x);
                }

                private static void NewMethod(int x)
                {
                    while (false) Console.WriteLine(x);
                }
            }
            """;
        await TestExtractMethodAsync(code, expected);
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539029")]
    public async Task BugFix4823()
    {
        var code = """
            class Program
            {
                private double area = 1.0;
                public double Area
                {
                    get
                    {
                        return area;
                    }
                }
                public override string ToString()
                {
                    return string.Format("{0:F2}", [|Area|]);
                }
            }
            """;

        var expected = """
            class Program
            {
                private double area = 1.0;
                public double Area
                {
                    get
                    {
                        return area;
                    }
                }
                public override string ToString()
                {
                    return string.Format("{0:F2}", GetArea());
                }

                private double GetArea()
                {
                    return Area;
                }
            }
            """;

        await TestExtractMethodAsync(code, expected);
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538985")]
    public async Task BugFix4762()
    {
        var code = """
            class Program
            {
                static void Main(string[] args)
                {
                    //comments
                    [|int x = 2;|]
                }
            }
            """;

        var expected = """
            class Program
            {
                static void Main(string[] args)
                {
                    //comments
                    NewMethod();
                }

                private static void NewMethod()
                {
                    int x = 2;
                }
            }
            """;

        await TestExtractMethodAsync(code, expected);
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538966")]
    public async Task BugFix4744()
    {
        var code = """
            class Program
            {
                static void Main(string[] args)
                {
                    [|int x = 2;
                    //comments|]
                }
            }
            """;

        var expected = """
            class Program
            {
                static void Main(string[] args)
                {
                    NewMethod();
                }

                private static void NewMethod()
                {
                    int x = 2;
                    //comments
                }
            }
            """;

        await TestExtractMethodAsync(code, expected);
    }

    [Fact]
    public async Task MatrixCase_NoNoNoNoNoYesNoNo()
    {
        var code = """
            using System;

            class Program
            {
                void Test1()
                {
                    int i;

                    [|
                    if (int.Parse("1") > 0)
                    {
                        i = 10;
                    }
                    |]
                }
            }
            """;

        var expected = """
            using System;

            class Program
            {
                void Test1()
                {
                    int i;

                    i = NewMethod(i);
                }

                private static int NewMethod(int i)
                {
                    if (int.Parse("1") > 0)
                    {
                        i = 10;
                    }

                    return i;
                }
            }
            """;

        await TestExtractMethodAsync(code, expected);
    }

    [Fact]
    public async Task MatrixCase_NoNoNoNoNoYesNoYes()
    {
        var code = """
            using System;

            class Program
            {
                void Test2()
                {
                    int i = 0;

                    [|
                    if (int.Parse("1") > 0)
                    {
                        i = 10;
                    }
                    |]
                }
            }
            """;

        var expected = """
            using System;

            class Program
            {
                void Test2()
                {
                    int i = 0;

                    i = NewMethod(i);
                }

                private static int NewMethod(int i)
                {
                    if (int.Parse("1") > 0)
                    {
                        i = 10;
                    }

                    return i;
                }
            }
            """;

        await TestExtractMethodAsync(code, expected);
    }

    [Fact]
    public async Task MatrixCase_NoNoNoNoNoYesYesNo()
    {
        var code = """
            using System;

            class Program
            {
                void Test3()
                {
                    int i;

                    while (i > 10) ;

                    [|
                    if (int.Parse("1") > 0)
                    {
                        i = 10;
                    }
                    |]
                }
            }
            """;

        var expected = """
            using System;

            class Program
            {
                void Test3()
                {
                    int i;

                    while (i > 10) ;

                    i = NewMethod(i);
                }

                private static int NewMethod(int i)
                {
                    if (int.Parse("1") > 0)
                    {
                        i = 10;
                    }

                    return i;
                }
            }
            """;

        await TestExtractMethodAsync(code, expected);
    }

    [Fact]
    public async Task MatrixCase_NoNoNoNoNoYesYesYes()
    {
        var code = """
            using System;

            class Program
            {
                void Test4()
                {
                    int i = 10;

                    while (i > 10) ;

                    [|
                    if (int.Parse("1") > 0)
                    {
                        i = 10;
                    }
                    |]
                }
            }
            """;

        var expected = """
            using System;

            class Program
            {
                void Test4()
                {
                    int i = 10;

                    while (i > 10) ;

                    i = NewMethod(i);
                }

                private static int NewMethod(int i)
                {
                    if (int.Parse("1") > 0)
                    {
                        i = 10;
                    }

                    return i;
                }
            }
            """;

        await TestExtractMethodAsync(code, expected);
    }

    [Fact]
    public async Task MatrixCase_NoNoNoNoYesYesNoNo()
    {
        var code = """
            using System;

            class Program
            {
                void Test4_1()
                {
                    int i;

                    [|
                    if (int.Parse("1") > 0)
                    {
                        i = 10;
                        Console.WriteLine(i);
                    }
                    |]
                }
            }
            """;

        var expected = """
            using System;

            class Program
            {
                void Test4_1()
                {
                    int i;

                    i = NewMethod();
                }

                private static int NewMethod()
                {
                    int i;
                    if (int.Parse("1") > 0)
                    {
                        i = 10;
                        Console.WriteLine(i);
                    }

                    return i;
                }
            }
            """;

        await TestExtractMethodAsync(code, expected);
    }

    [Fact]
    public async Task MatrixCase_NoNoNoNoYesYesNoYes()
    {
        var code = """
            using System;

            class Program
            {
                void Test4_2()
                {
                    int i = 10;

                    [|
                    if (int.Parse("1") > 0)
                    {
                        i = 10;
                        Console.WriteLine(i);
                    }
                    |]
                }
            }
            """;

        var expected = """
            using System;

            class Program
            {
                void Test4_2()
                {
                    int i = 10;

                    i = NewMethod(i);
                }

                private static int NewMethod(int i)
                {
                    if (int.Parse("1") > 0)
                    {
                        i = 10;
                        Console.WriteLine(i);
                    }

                    return i;
                }
            }
            """;

        await TestExtractMethodAsync(code, expected);
    }

    [Fact]
    public async Task MatrixCase_NoNoNoNoYesYesYesNo()
    {
        var code = """
            using System;

            class Program
            {
                void Test4_3()
                {
                    int i;

                    Console.WriteLine(i);

                    [|
                    if (int.Parse("1") > 0)
                    {
                        i = 10;
                        Console.WriteLine(i);
                    }
                    |]
                }
            }
            """;

        var expected = """
            using System;

            class Program
            {
                void Test4_3()
                {
                    int i;

                    Console.WriteLine(i);

                    i = NewMethod();
                }

                private static int NewMethod()
                {
                    int i;
                    if (int.Parse("1") > 0)
                    {
                        i = 10;
                        Console.WriteLine(i);
                    }

                    return i;
                }
            }
            """;

        await TestExtractMethodAsync(code, expected);
    }

    [Fact]
    public async Task MatrixCase_NoNoNoNoYesYesYesYes()
    {
        var code = """
            using System;

            class Program
            {
                void Test4_4()
                {
                    int i = 10;

                    Console.WriteLine(i);

                    [|
                    if (int.Parse("1") > 0)
                    {
                        i = 10;
                        Console.WriteLine(i);
                    }
                    |]
                }
            }
            """;

        var expected = """
            using System;

            class Program
            {
                void Test4_4()
                {
                    int i = 10;

                    Console.WriteLine(i);

                    i = NewMethod(i);
                }

                private static int NewMethod(int i)
                {
                    if (int.Parse("1") > 0)
                    {
                        i = 10;
                        Console.WriteLine(i);
                    }

                    return i;
                }
            }
            """;

        await TestExtractMethodAsync(code, expected);
    }

    [Fact]
    public async Task MatrixCase_NoNoNoYesNoNoNoNo()
    {
        var code = """
            using System;

            class Program
            {
                void Test5()
                {
                    [|
                    int i;
                    |]
                }
            }
            """;

        await ExpectExtractMethodToFailAsync(code);
    }

    [Fact]
    public async Task MatrixCase_NoNoNoYesNoNoNoYes()
    {
        var code = """
            using System;

            class Program
            {
                void Test6()
                {
                    [|
                    int i;
                    |]

                    i = 1;
                }
            }
            """;

        await ExpectExtractMethodToFailAsync(code);
    }

    [Fact]
    public async Task MatrixCase_NoNoNoYesNoYesNoNo()
    {
        var code = """
            using System;

            class Program
            {
                void Test7()
                {
                    [|
                    int i;

                    if (int.Parse("1") > 0)
                    {
                        i = 10;
                    }
                    |]
                }
            }
            """;

        var expected = """
            using System;

            class Program
            {
                void Test7()
                {
                    NewMethod();
                }

                private static void NewMethod()
                {
                    int i;

                    if (int.Parse("1") > 0)
                    {
                        i = 10;
                    }
                }
            }
            """;

        await TestExtractMethodAsync(code, expected);
    }

    [Fact]
    public async Task MatrixCase_NoNoNoYesNoYesNoYes()
    {
        var code = """
            using System;

            class Program
            {
                void Test8()
                {
                    [|
                    int i;

                    if (int.Parse("1") > 0)
                    {
                        i = 10;
                    }
                    |]

                    i = 2;
                }
            }
            """;

        var expected = """
            using System;

            class Program
            {
                void Test8()
                {
                    int i = NewMethod();

                    i = 2;
                }

                private static int NewMethod()
                {
                    int i;

                    if (int.Parse("1") > 0)
                    {
                        i = 10;
                    }

                    return i;
                }
            }
            """;

        await TestExtractMethodAsync(code, expected);
    }

    [Fact]
    public async Task MatrixCase_NoNoNoYesYesNoNoNo()
    {
        var code = """
            using System;

            class Program
            {
                void Test9()
                {
                    [|
                    int i;

                    Console.WriteLine(i);
                    |]
                }
            }
            """;

        var expected = """
            using System;

            class Program
            {
                void Test9()
                {
                    NewMethod();
                }

                private static void NewMethod()
                {
                    int i;

                    Console.WriteLine(i);
                }
            }
            """;

        await TestExtractMethodAsync(code, expected);
    }

    [Fact]
    public async Task MatrixCase_NoNoNoYesYesNoNoYes()
    {
        var code = """
            using System;

            class Program
            {
                void Test10()
                {
                    [|
                    int i;

                    Console.WriteLine(i);
                    |]

                    i = 10;
                }
            }
            """;

        var expected = """
            using System;

            class Program
            {
                void Test10()
                {
                    int i;
                    NewMethod();

                    i = 10;
                }

                private static void NewMethod()
                {
                    int i;

                    Console.WriteLine(i);
                }
            }
            """;

        await TestExtractMethodAsync(code, expected);
    }

    [Fact]
    public async Task MatrixCase_NoNoNoYesYesYesNoNo()
    {
        var code = """
            using System;

            class Program
            {
                void Test11()
                {
                    [|
                    int i;

                    if (int.Parse("1") > 0)
                    {
                        i = 10;
                    }

                    Console.WriteLine(i);
                    |]
                }
            }
            """;

        var expected = """
            using System;

            class Program
            {
                void Test11()
                {
                    NewMethod();
                }

                private static void NewMethod()
                {
                    int i;

                    if (int.Parse("1") > 0)
                    {
                        i = 10;
                    }

                    Console.WriteLine(i);
                }
            }
            """;

        await TestExtractMethodAsync(code, expected);
    }

    [Fact]
    public async Task MatrixCase_NoNoNoYesYesYesNoYes()
    {
        var code = """
            using System;

            class Program
            {
                void Test12()
                {
                    [|
                    int i;

                    if (int.Parse("1") > 0)
                    {
                        i = 10;
                    }

                    Console.WriteLine(i);
                    |]

                    i = 10;
                }
            }
            """;

        var expected = """
            using System;

            class Program
            {
                void Test12()
                {
                    int i = NewMethod();

                    i = 10;
                }

                private static int NewMethod()
                {
                    int i;

                    if (int.Parse("1") > 0)
                    {
                        i = 10;
                    }

                    Console.WriteLine(i);
                    return i;
                }
            }
            """;

        await TestExtractMethodAsync(code, expected);
    }

    [Fact]
    public async Task MatrixCase_NoNoYesNoNoYesNoNo()
    {
        var code = """
            using System;

            class Program
            {
                void Test13()
                {
                    int i;

                    [|
                    i = 10;
                    |]
                }
            }
            """;

        var expected = """
            using System;

            class Program
            {
                void Test13()
                {
                    int i;

                    i = NewMethod();
                }

                private static int NewMethod()
                {
                    return 10;
                }
            }
            """;

        await TestExtractMethodAsync(code, expected);
    }

    [Fact]
    public async Task MatrixCase_NoNoYesNoNoYesNoYes()
    {
        var code = """
            using System;

            class Program
            {
                void Test14()
                {
                    int i;

                    [|
                    i = 10;
                    |]

                    i = 1;
                }
            }
            """;

        var expected = """
            using System;

            class Program
            {
                void Test14()
                {
                    int i;

                    i = NewMethod();

                    i = 1;
                }

                private static int NewMethod()
                {
                    return 10;
                }
            }
            """;

        await TestExtractMethodAsync(code, expected);
    }

    [Fact]
    public async Task MatrixCase_NoNoYesNoNoYesYesNo()
    {
        var code = """
            using System;

            class Program
            {
                void Test15()
                {
                    int i;

                    Console.WriteLine(i);

                    [|
                    i = 10;
                    |]
                }
            }
            """;

        var expected = """
            using System;

            class Program
            {
                void Test15()
                {
                    int i;

                    Console.WriteLine(i);

                    i = NewMethod();
                }

                private static int NewMethod()
                {
                    return 10;
                }
            }
            """;

        await TestExtractMethodAsync(code, expected);
    }

    [Fact]
    public async Task MatrixCase_NoNoYesNoNoYesYesYes()
    {
        var code = """
            using System;

            class Program
            {
                void Test16()
                {
                    int i;

                    [|
                    i = 10;
                    |]

                    i = 10;

                    Console.WriteLine(i);
                }
            }
            """;

        var expected = """
            using System;

            class Program
            {
                void Test16()
                {
                    int i;

                    i = NewMethod();

                    i = 10;

                    Console.WriteLine(i);
                }

                private static int NewMethod()
                {
                    return 10;
                }
            }
            """;

        await TestExtractMethodAsync(code, expected);
    }

    // dataflow in and out can be false for symbols in unreachable code
    // boolean indicates 
    // dataFlowIn: false, dataFlowOut: false, alwaysAssigned: true, variableDeclared: false, readInside: true, writtenInside: false, readOutside: false, writtenOutside: true
    [Fact]
    public async Task MatrixCase_NoNoYesNoYesNoNoYes()
    {
        var code = """
            using System.Collections.Generic;
            using System.Linq;

            namespace ConsoleApp1
            {
                class Test
                {
                    IEnumerable<object> Crash0(IEnumerable<object> enumerable)
                    {
                        [|while (true) ;
                        enumerable.Select(e => "");
                        return enumerable;|]
                    }
                }
            }
            """;

        var expected = """
            using System.Collections.Generic;
            using System.Linq;

            namespace ConsoleApp1
            {
                class Test
                {
                    IEnumerable<object> Crash0(IEnumerable<object> enumerable)
                    {
                        return NewMethod(enumerable);
                    }

                    private static IEnumerable<object> NewMethod(IEnumerable<object> enumerable)
                    {
                        while (true) ;
                        enumerable.Select(e => "");
                        return enumerable;
                    }
                }
            }
            """;

        await TestExtractMethodAsync(code, expected);
    }

    // dataflow in and out can be false for symbols in unreachable code
    // boolean indicates 
    // dataFlowIn: false, dataFlowOut: false, alwaysAssigned: true, variableDeclared: false, readInside: true, writtenInside: false, readOutside: true, writtenOutside: true
    [Fact]
    public async Task MatrixCase_NoNoYesNoYesNoYesYes()
    {
        var code = """
            using System.Collections.Generic;
            using System.Linq;

            namespace ConsoleApp1
            {
                class Test
                {
                    IEnumerable<object> Crash0(IEnumerable<object> enumerable)
                    {
                        [|while (true) ;
                        enumerable.Select(e => "");|]
                        return enumerable;
                    }
                }
            }
            """;

        var expected = """
            using System.Collections.Generic;
            using System.Linq;

            namespace ConsoleApp1
            {
                class Test
                {
                    IEnumerable<object> Crash0(IEnumerable<object> enumerable)
                    {
                        enumerable = NewMethod(enumerable);
                        return enumerable;
                    }

                    private static IEnumerable<object> NewMethod(IEnumerable<object> enumerable)
                    {
                        while (true) ;
                        enumerable.Select(e => "");
                        return enumerable;
                    }
                }
            }
            """;

        await TestExtractMethodAsync(code, expected);
    }

    [Fact]
    public async Task MatrixCase_NoNoYesNoYesYesNoNo()
    {
        var code = """
            using System;

            class Program
            {
                void Test16_1()
                {
                    int i;

                    [|
                    i = 10;
                    Console.WriteLine(i);
                    |]
                }
            }
            """;

        var expected = """
            using System;

            class Program
            {
                void Test16_1()
                {
                    int i;

                    i = NewMethod();
                }

                private static int NewMethod()
                {
                    int i = 10;
                    Console.WriteLine(i);
                    return i;
                }
            }
            """;

        await TestExtractMethodAsync(code, expected);
    }

    [Fact]
    public async Task MatrixCase_NoNoYesNoYesYesNoYes()
    {
        var code = """
            using System;

            class Program
            {
                void Test16_2()
                {
                    int i = 10;

                    [|
                    i = 10;
                    Console.WriteLine(i);
                    |]
                }
            }
            """;

        var expected = """
            using System;

            class Program
            {
                void Test16_2()
                {
                    int i = 10;

                    i = NewMethod();
                }

                private static int NewMethod()
                {
                    int i = 10;
                    Console.WriteLine(i);
                    return i;
                }
            }
            """;

        await TestExtractMethodAsync(code, expected);
    }

    [Fact]
    public async Task MatrixCase_NoNoYesNoYesYesYesNo()
    {
        var code = """
            using System;

            class Program
            {
                void Test16_3()
                {
                    int i;

                    Console.WriteLine(i);

                    [|
                    i = 10;
                    Console.WriteLine(i);
                    |]
                }
            }
            """;

        var expected = """
            using System;

            class Program
            {
                void Test16_3()
                {
                    int i;

                    Console.WriteLine(i);

                    i = NewMethod();
                }

                private static int NewMethod()
                {
                    int i = 10;
                    Console.WriteLine(i);
                    return i;
                }
            }
            """;

        await TestExtractMethodAsync(code, expected);
    }

    [Fact]
    public async Task MatrixCase_NoNoYesNoYesYesYesYes()
    {
        var code = """
            using System;

            class Program
            {
                void Test16_4()
                {
                    int i = 10;

                    Console.WriteLine(i);

                    [|
                    i = 10;
                    Console.WriteLine(i);
                    |]
                }
            }
            """;

        var expected = """
            using System;

            class Program
            {
                void Test16_4()
                {
                    int i = 10;

                    Console.WriteLine(i);

                    i = NewMethod();
                }

                private static int NewMethod()
                {
                    int i = 10;
                    Console.WriteLine(i);
                    return i;
                }
            }
            """;

        await TestExtractMethodAsync(code, expected);
    }

    [Fact]
    public async Task MatrixCase_NoNoYesYesNoYesNoNo()
    {
        var code = """
            using System;

            class Program
            {
                void Test17()
                {
                    [|
                    int i = 10;
                    |]
                }
            }
            """;

        var expected = """
            using System;

            class Program
            {
                void Test17()
                {
                    NewMethod();
                }

                private static void NewMethod()
                {
                    int i = 10;
                }
            }
            """;

        await TestExtractMethodAsync(code, expected);
    }

    [Fact]
    public async Task MatrixCase_NoNoYesYesNoYesNoYes()
    {
        var code = """
            using System;

            class Program
            {
                void Test18()
                {
                    [|
                    int i = 10;
                    |]

                    i = 10;
                }
            }
            """;

        var expected = """
            using System;

            class Program
            {
                void Test18()
                {
                    int i = NewMethod();

                    i = 10;
                }

                private static int NewMethod()
                {
                    return 10;
                }
            }
            """;

        await TestExtractMethodAsync(code, expected);
    }

    [Fact]
    public async Task MatrixCase_NoNoYesYesYesYesNoNo()
    {
        var code = """
            using System;

            class Program
            {
                void Test19()
                {
                    [|
                    int i = 10;
                    Console.WriteLine(i);
                    |]
                }
            }
            """;

        var expected = """
            using System;

            class Program
            {
                void Test19()
                {
                    NewMethod();
                }

                private static void NewMethod()
                {
                    int i = 10;
                    Console.WriteLine(i);
                }
            }
            """;

        await TestExtractMethodAsync(code, expected);
    }

    [Fact]
    public async Task MatrixCase_NoNoYesYesYesYesNoYes()
    {
        var code = """
            using System;

            class Program
            {
                void Test20()
                {
                    [|
                    int i = 10;
                    Console.WriteLine(i);
                    |]

                    i = 10;
                }
            }
            """;

        var expected = """
            using System;

            class Program
            {
                void Test20()
                {
                    int i;
                    NewMethod();

                    i = 10;
                }

                private static void NewMethod()
                {
                    int i = 10;
                    Console.WriteLine(i);
                }
            }
            """;

        await TestExtractMethodAsync(code, expected);
    }

    [Fact]
    public async Task MatrixCase_NoYesNoNoNoYesYesNo()
    {
        var code = """
            using System;

            class Program
            {
                void Test21()
                {
                    int i;

                    [|
                    if (int.Parse("1") > 10)
                    {
                        i = 1;
                    }
                    |]

                    Console.WriteLine(i);
                }
            }
            """;

        var expected = """
            using System;

            class Program
            {
                void Test21()
                {
                    int i;

                    i = NewMethod(i);

                    Console.WriteLine(i);
                }

                private static int NewMethod(int i)
                {
                    if (int.Parse("1") > 10)
                    {
                        i = 1;
                    }

                    return i;
                }
            }
            """;

        await TestExtractMethodAsync(code, expected);
    }

    [Fact]
    public async Task MatrixCase_NoYesNoNoNoYesYesYes()
    {
        var code = """
            using System;

            class Program
            {
                void Test22()
                {
                    int i = 10;

                    [|
                    if (int.Parse("1") > 10)
                    {
                        i = 1;
                    }
                    |]

                    Console.WriteLine(i);
                }
            }
            """;

        var expected = """
            using System;

            class Program
            {
                void Test22()
                {
                    int i = 10;

                    i = NewMethod(i);

                    Console.WriteLine(i);
                }

                private static int NewMethod(int i)
                {
                    if (int.Parse("1") > 10)
                    {
                        i = 1;
                    }

                    return i;
                }
            }
            """;

        await TestExtractMethodAsync(code, expected);
    }

    [Fact]
    public async Task MatrixCase_NoYesNoNoYesYesYesNo()
    {
        var code = """
            using System;

            class Program
            {
                void Test22_1()
                {
                    int i;

                    [|
                    if (int.Parse("1") > 10)
                    {
                        i = 1;
                        Console.WriteLine(i);
                    }
                    |]

                    Console.WriteLine(i);
                }
            }
            """;

        var expected = """
            using System;

            class Program
            {
                void Test22_1()
                {
                    int i;

                    i = NewMethod(i);

                    Console.WriteLine(i);
                }

                private static int NewMethod(int i)
                {
                    if (int.Parse("1") > 10)
                    {
                        i = 1;
                        Console.WriteLine(i);
                    }

                    return i;
                }
            }
            """;

        await TestExtractMethodAsync(code, expected);
    }

    [Fact]
    public async Task MatrixCase_NoYesNoNoYesYesYesYes()
    {
        var code = """
            using System;

            class Program
            {
                void Test22_2()
                {
                    int i = 10;

                    [|
                    if (int.Parse("1") > 10)
                    {
                        i = 1;
                        Console.WriteLine(i);
                    }
                    |]

                    Console.WriteLine(i);
                }
            }
            """;

        var expected = """
            using System;

            class Program
            {
                void Test22_2()
                {
                    int i = 10;

                    i = NewMethod(i);

                    Console.WriteLine(i);
                }

                private static int NewMethod(int i)
                {
                    if (int.Parse("1") > 10)
                    {
                        i = 1;
                        Console.WriteLine(i);
                    }

                    return i;
                }
            }
            """;

        await TestExtractMethodAsync(code, expected);
    }

    [Fact]
    public async Task MatrixCase_NoYesNoYesNoNoYesNo()
    {
        var code = """
            using System;

            class Program
            {
                void Test23()
                {
                    [|
                    int i;
                    |]

                    Console.WriteLine(i);
                }
            }
            """;

        await ExpectExtractMethodToFailAsync(code);
    }

    [Fact]
    public async Task MatrixCase_NoYesNoYesNoNoYesYes()
    {
        var code = """
            using System;

            class Program
            {
                void Test24()
                {
                    [|
                    int i;
                    |]

                    Console.WriteLine(i);

                    i = 10;
                }
            }
            """;

        await ExpectExtractMethodToFailAsync(code);
    }

    [Fact]
    public async Task MatrixCase_NoYesNoYesNoYesYesNo()
    {
        var code = """
            using System;

            class Program
            {
                void Test25()
                {
                    [|
                    int i;

                    if (int.Parse("1") > 9)
                    {
                        i = 10;
                    }
                    |]

                    Console.WriteLine(i);
                }
            }
            """;

        var expected = """
            using System;

            class Program
            {
                void Test25()
                {
                    int i = NewMethod();

                    Console.WriteLine(i);
                }

                private static int NewMethod()
                {
                    int i;

                    if (int.Parse("1") > 9)
                    {
                        i = 10;
                    }

                    return i;
                }
            }
            """;

        await TestExtractMethodAsync(code, expected);
    }

    [Fact]
    public async Task MatrixCase_NoYesNoYesNoYesYesYes()
    {
        var code = """
            using System;

            class Program
            {
                void Test26()
                {
                    [|
                    int i;

                    if (int.Parse("1") > 9)
                    {
                        i = 10;
                    }
                    |]

                    Console.WriteLine(i);

                    i = 10;
                }
            }
            """;

        var expected = """
            using System;

            class Program
            {
                void Test26()
                {
                    int i = NewMethod();

                    Console.WriteLine(i);

                    i = 10;
                }

                private static int NewMethod()
                {
                    int i;

                    if (int.Parse("1") > 9)
                    {
                        i = 10;
                    }

                    return i;
                }
            }
            """;

        await TestExtractMethodAsync(code, expected);
    }

    [Fact]
    public async Task MatrixCase_NoYesNoYesYesNoYesNo()
    {
        var code = """
            using System;

            class Program
            {
                void Test27()
                {
                    [|
                    int i;

                    Console.WriteLine(i);
                    |]

                    Console.WriteLine(i);
                }
            }
            """;

        var expected = """
            using System;

            class Program
            {
                void Test27()
                {
                    int i = NewMethod();

                    Console.WriteLine(i);
                }

                private static int NewMethod()
                {
                    int i;

                    Console.WriteLine(i);
                    return i;
                }
            }
            """;

        await TestExtractMethodAsync(code, expected);
    }

    [Fact]
    public async Task MatrixCase_NoYesNoYesYesNoYesYes()
    {
        var code = """
            using System;

            class Program
            {
                void Test28()
                {
                    [|
                    int i;

                    Console.WriteLine(i);
                    |]

                    Console.WriteLine(i);

                    i = 10;
                }
            }
            """;

        var expected = """
            using System;

            class Program
            {
                void Test28()
                {
                    int i = NewMethod();

                    Console.WriteLine(i);

                    i = 10;
                }

                private static int NewMethod()
                {
                    int i;

                    Console.WriteLine(i);
                    return i;
                }
            }
            """;

        await TestExtractMethodAsync(code, expected);
    }

    [Fact]
    public async Task MatrixCase_NoYesNoYesYesYesYesNo()
    {
        var code = """
            using System;

            class Program
            {
                void Test29()
                {
                    [|
                    int i;

                    if (int.Parse("1") > 0)
                    {
                        i = 10;
                    }

                    Console.WriteLine(i);
                    |]

                    Console.WriteLine(i);
                }
            }
            """;

        var expected = """
            using System;

            class Program
            {
                void Test29()
                {
                    int i = NewMethod();

                    Console.WriteLine(i);
                }

                private static int NewMethod()
                {
                    int i;

                    if (int.Parse("1") > 0)
                    {
                        i = 10;
                    }

                    Console.WriteLine(i);
                    return i;
                }
            }
            """;

        await TestExtractMethodAsync(code, expected);
    }

    [Fact]
    public async Task MatrixCase_NoYesNoYesYesYesYesYes()
    {
        var code = """
            using System;

            class Program
            {
                void Test30()
                {
                    [|
                    int i;

                    if (int.Parse("1") > 0)
                    {
                        i = 10;
                    }

                    Console.WriteLine(i);
                    |]

                    Console.WriteLine(i);

                    i = 10;
                }
            }
            """;

        var expected = """
            using System;

            class Program
            {
                void Test30()
                {
                    int i = NewMethod();

                    Console.WriteLine(i);

                    i = 10;
                }

                private static int NewMethod()
                {
                    int i;

                    if (int.Parse("1") > 0)
                    {
                        i = 10;
                    }

                    Console.WriteLine(i);
                    return i;
                }
            }
            """;

        await TestExtractMethodAsync(code, expected);
    }

    [Fact]
    public async Task MatrixCase_NoYesYesNoNoYesYesNo()
    {
        var code = """
            using System;

            class Program
            {
                void Test31()
                {
                    int i;

                    [|
                    i = 10;
                    |]

                    Console.WriteLine(i);
                }
            }
            """;

        var expected = """
            using System;

            class Program
            {
                void Test31()
                {
                    int i;

                    i = NewMethod();

                    Console.WriteLine(i);
                }

                private static int NewMethod()
                {
                    return 10;
                }
            }
            """;

        await TestExtractMethodAsync(code, expected);
    }

    [Fact]
    public async Task MatrixCase_NoYesYesNoNoYesYesYes()
    {
        var code = """
            using System;

            class Program
            {
                void Test32()
                {
                    int i;

                    [|
                    i = 10;
                    |]

                    Console.WriteLine(i);

                    i = 10;
                }
            }
            """;

        var expected = """
            using System;

            class Program
            {
                void Test32()
                {
                    int i;

                    i = NewMethod();

                    Console.WriteLine(i);

                    i = 10;
                }

                private static int NewMethod()
                {
                    return 10;
                }
            }
            """;

        await TestExtractMethodAsync(code, expected);
    }

    [Fact]
    public async Task MatrixCase_NoYesYesNoYesYesYesNo()
    {
        var code = """
            using System;

            class Program
            {
                void Test32_1()
                {
                    int i;

                    [|
                    i = 10;
                    Console.WriteLine(i);
                    |]

                    Console.WriteLine(i);
                }
            }
            """;

        var expected = """
            using System;

            class Program
            {
                void Test32_1()
                {
                    int i;

                    i = NewMethod();

                    Console.WriteLine(i);
                }

                private static int NewMethod()
                {
                    int i = 10;
                    Console.WriteLine(i);
                    return i;
                }
            }
            """;

        await TestExtractMethodAsync(code, expected);
    }

    [Fact]
    public async Task MatrixCase_NoYesYesNoYesYesYesYes()
    {
        var code = """
            using System;

            class Program
            {
                void Test32_2()
                {
                    int i = 10;

                    [|
                    i = 10;
                    Console.WriteLine(i);
                    |]

                    Console.WriteLine(i);
                }
            }
            """;

        var expected = """
            using System;

            class Program
            {
                void Test32_2()
                {
                    int i = 10;

                    i = NewMethod();

                    Console.WriteLine(i);
                }

                private static int NewMethod()
                {
                    int i = 10;
                    Console.WriteLine(i);
                    return i;
                }
            }
            """;

        await TestExtractMethodAsync(code, expected);
    }

    [Fact]
    public async Task MatrixCase_NoYesYesYesNoYesYesNo()
    {
        var code = """
            using System;

            class Program
            {
                void Test33()
                {
                    [|
                    int i = 10;
                    |]

                    Console.WriteLine(i);
                }
            }
            """;

        var expected = """
            using System;

            class Program
            {
                void Test33()
                {
                    int i = NewMethod();

                    Console.WriteLine(i);
                }

                private static int NewMethod()
                {
                    return 10;
                }
            }
            """;

        await TestExtractMethodAsync(code, expected);
    }

    [Fact]
    public async Task MatrixCase_NoYesYesYesNoYesYesYes()
    {
        var code = """
            using System;

            class Program
            {
                void Test34()
                {
                    [|
                    int i = 10;
                    |]

                    Console.WriteLine(i);

                    i = 10;
                }
            }
            """;

        var expected = """
            using System;

            class Program
            {
                void Test34()
                {
                    int i = NewMethod();

                    Console.WriteLine(i);

                    i = 10;
                }

                private static int NewMethod()
                {
                    return 10;
                }
            }
            """;

        await TestExtractMethodAsync(code, expected);
    }

    [Fact]
    public async Task MatrixCase_NoYesYesYesYesYesYesNo()
    {
        var code = """
            using System;

            class Program
            {
                void Test35()
                {
                    [|
                    int i = 10;

                    Console.WriteLine(i);
                    |]

                    Console.WriteLine(i);
                }
            }
            """;

        var expected = """
            using System;

            class Program
            {
                void Test35()
                {
                    int i = NewMethod();

                    Console.WriteLine(i);
                }

                private static int NewMethod()
                {
                    int i = 10;

                    Console.WriteLine(i);
                    return i;
                }
            }
            """;

        await TestExtractMethodAsync(code, expected);
    }

    [Fact]
    public async Task MatrixCase_NoYesYesYesYesYesYesYes()
    {
        var code = """
            using System;

            class Program
            {
                void Test36()
                {
                    [|
                    int i = 10;

                    Console.WriteLine(i);
                    |]

                    Console.WriteLine(i);

                    i = 10;
                }
            }
            """;

        var expected = """
            using System;

            class Program
            {
                void Test36()
                {
                    int i = NewMethod();

                    Console.WriteLine(i);

                    i = 10;
                }

                private static int NewMethod()
                {
                    int i = 10;

                    Console.WriteLine(i);
                    return i;
                }
            }
            """;

        await TestExtractMethodAsync(code, expected);
    }

    [Fact]
    public async Task MatrixCase_YesNoNoNoYesNoNoNo()
    {
        var code = """
            using System;

            class Program
            {
                void Test37()
                {
                    int i;

                    [|
                    Console.WriteLine(i);
                    |]
                }
            }
            """;

        var expected = """
            using System;

            class Program
            {
                void Test37()
                {
                    int i;

                    NewMethod(i);
                }

                private static void NewMethod(int i)
                {
                    Console.WriteLine(i);
                }
            }
            """;

        await TestExtractMethodAsync(code, expected);
    }

    [Fact]
    public async Task MatrixCase_YesNoNoNoYesNoNoYes()
    {
        var code = """
            using System;

            class Program
            {
                void Test38()
                {
                    int i = 10;

                    [|
                    Console.WriteLine(i);
                    |]
                }
            }
            """;

        var expected = """
            using System;

            class Program
            {
                void Test38()
                {
                    int i = 10;

                    NewMethod(i);
                }

                private static void NewMethod(int i)
                {
                    Console.WriteLine(i);
                }
            }
            """;

        await TestExtractMethodAsync(code, expected);
    }

    [Fact]
    public async Task MatrixCase_YesNoNoNoYesNoYesNo()
    {
        var code = """
            using System;

            class Program
            {
                void Test39()
                {
                    int i;

                    [|
                    Console.WriteLine(i);
                    |]

                    Console.WriteLine(i);
                }
            }
            """;

        var expected = """
            using System;

            class Program
            {
                void Test39()
                {
                    int i;

                    NewMethod(i);

                    Console.WriteLine(i);
                }

                private static void NewMethod(int i)
                {
                    Console.WriteLine(i);
                }
            }
            """;

        await TestExtractMethodAsync(code, expected);
    }

    [Fact]
    public async Task MatrixCase_YesNoNoNoYesNoYesYes()
    {
        var code = """
            using System;

            class Program
            {
                void Test40()
                {
                    int i = 10;

                    [|
                    Console.WriteLine(i);
                    |]

                    Console.WriteLine(i);
                }
            }
            """;

        var expected = """
            using System;

            class Program
            {
                void Test40()
                {
                    int i = 10;

                    NewMethod(i);

                    Console.WriteLine(i);
                }

                private static void NewMethod(int i)
                {
                    Console.WriteLine(i);
                }
            }
            """;

        await TestExtractMethodAsync(code, expected);
    }

    [Fact]
    public async Task MatrixCase_YesNoNoNoYesYesNoNo()
    {
        var code = """
            using System;

            class Program
            {
                void Test41()
                {
                    int i;

                    [|
                    Console.WriteLine(i);

                    if (int.Parse("1") > 0)
                    {
                        i = 10;
                    }
                    |]
                }
            }
            """;

        var expected = """
            using System;

            class Program
            {
                void Test41()
                {
                    int i;

                    i = NewMethod(i);
                }

                private static int NewMethod(int i)
                {
                    Console.WriteLine(i);

                    if (int.Parse("1") > 0)
                    {
                        i = 10;
                    }

                    return i;
                }
            }
            """;

        await TestExtractMethodAsync(code, expected);
    }

    [Fact]
    public async Task MatrixCase_YesNoNoNoYesYesNoYes()
    {
        var code = """
            using System;

            class Program
            {
                void Test42()
                {
                    int i = 10;

                    [|
                    Console.WriteLine(i);

                    if (int.Parse("1") > 0)
                    {
                        i = 10;
                    }
                    |]
                }
            }
            """;

        var expected = """
            using System;

            class Program
            {
                void Test42()
                {
                    int i = 10;

                    i = NewMethod(i);
                }

                private static int NewMethod(int i)
                {
                    Console.WriteLine(i);

                    if (int.Parse("1") > 0)
                    {
                        i = 10;
                    }

                    return i;
                }
            }
            """;

        await TestExtractMethodAsync(code, expected);
    }

    [Fact]
    public async Task MatrixCase_YesNoNoNoYesYesYesNo()
    {
        var code = """
            using System;

            class Program
            {
                void Test43()
                {
                    int i;

                    Console.WriteLine(i);

                    [|
                    Console.WriteLine(i);

                    if (int.Parse("1") > 0)
                    {
                        i = 10;
                    }
                    |]
                }
            }
            """;

        var expected = """
            using System;

            class Program
            {
                void Test43()
                {
                    int i;

                    Console.WriteLine(i);

                    i = NewMethod(i);
                }

                private static int NewMethod(int i)
                {
                    Console.WriteLine(i);

                    if (int.Parse("1") > 0)
                    {
                        i = 10;
                    }

                    return i;
                }
            }
            """;

        await TestExtractMethodAsync(code, expected);
    }

    [Fact]
    public async Task MatrixCase_YesNoNoNoYesYesYesYes()
    {
        var code = """
            using System;

            class Program
            {
                void Test44()
                {
                    int i = 10;

                    Console.WriteLine(i);

                    [|
                    Console.WriteLine(i);

                    if (int.Parse("1") > 0)
                    {
                        i = 10;
                    }
                    |]
                }
            }
            """;

        var expected = """
            using System;

            class Program
            {
                void Test44()
                {
                    int i = 10;

                    Console.WriteLine(i);

                    i = NewMethod(i);
                }

                private static int NewMethod(int i)
                {
                    Console.WriteLine(i);

                    if (int.Parse("1") > 0)
                    {
                        i = 10;
                    }

                    return i;
                }
            }
            """;

        await TestExtractMethodAsync(code, expected);
    }

    [Fact]
    public async Task MatrixCase_YesNoYesNoYesYesNoNo()
    {
        var code = """
            using System;

            class Program
            {
                void Test45()
                {
                    int i;

                    [|
                    Console.WriteLine(i);

                    i = 10;
                    |]
                }
            }
            """;

        var expected = """
            using System;

            class Program
            {
                void Test45()
                {
                    int i;

                    i = NewMethod(i);
                }

                private static int NewMethod(int i)
                {
                    Console.WriteLine(i);

                    i = 10;
                    return i;
                }
            }
            """;

        await TestExtractMethodAsync(code, expected);
    }

    [Fact]
    public async Task MatrixCase_YesNoYesNoYesYesNoYes()
    {
        var code = """
            using System;

            class Program
            {
                void Test46()
                {
                    int i = 10;

                    [|
                    Console.WriteLine(i);

                    i = 10;
                    |]
                }
            }
            """;

        var expected = """
            using System;

            class Program
            {
                void Test46()
                {
                    int i = 10;

                    i = NewMethod(i);
                }

                private static int NewMethod(int i)
                {
                    Console.WriteLine(i);

                    i = 10;
                    return i;
                }
            }
            """;

        await TestExtractMethodAsync(code, expected);
    }

    [Fact]
    public async Task MatrixCase_YesNoYesNoYesYesYesNo()
    {
        var code = """
            using System;

            class Program
            {
                void Test47()
                {
                    int i;

                    Console.WriteLine(i);

                    [|
                    Console.WriteLine(i);

                    i = 10;
                    |]
                }
            }
            """;

        var expected = """
            using System;

            class Program
            {
                void Test47()
                {
                    int i;

                    Console.WriteLine(i);

                    i = NewMethod(i);
                }

                private static int NewMethod(int i)
                {
                    Console.WriteLine(i);

                    i = 10;
                    return i;
                }
            }
            """;

        await TestExtractMethodAsync(code, expected);
    }

    [Fact]
    public async Task MatrixCase_YesNoYesNoYesYesYesYes()
    {
        var code = """
            using System;

            class Program
            {
                void Test48()
                {
                    int i = 10;

                    Console.WriteLine(i);

                    [|
                    Console.WriteLine(i);

                    i = 10;
                    |]
                }
            }
            """;

        var expected = """
            using System;

            class Program
            {
                void Test48()
                {
                    int i = 10;

                    Console.WriteLine(i);

                    i = NewMethod(i);
                }

                private static int NewMethod(int i)
                {
                    Console.WriteLine(i);

                    i = 10;
                    return i;
                }
            }
            """;

        await TestExtractMethodAsync(code, expected);
    }

    [Fact]
    public async Task MatrixCase_YesYesNoNoYesYesYesNo()
    {
        var code = """
            using System;

            class Program
            {
                void Test49()
                {
                    int i;

                    [|
                    Console.WriteLine(i);

                    if (int.Parse("1") > 0)
                    {
                        i = 10;
                    }
                    |]

                    Console.WriteLine(i);
                }
            }
            """;

        var expected = """
            using System;

            class Program
            {
                void Test49()
                {
                    int i;

                    i = NewMethod(i);

                    Console.WriteLine(i);
                }

                private static int NewMethod(int i)
                {
                    Console.WriteLine(i);

                    if (int.Parse("1") > 0)
                    {
                        i = 10;
                    }

                    return i;
                }
            }
            """;

        await TestExtractMethodAsync(code, expected);
    }

    [Fact]
    public async Task MatrixCase_YesYesNoNoYesYesYesYes()
    {
        var code = """
            using System;

            class Program
            {
                void Test50()
                {
                    int i = 10;

                    [|
                    Console.WriteLine(i);

                    if (int.Parse("1") > 0)
                    {
                        i = 10;
                    }
                    |]

                    Console.WriteLine(i);
                }
            }
            """;

        var expected = """
            using System;

            class Program
            {
                void Test50()
                {
                    int i = 10;

                    i = NewMethod(i);

                    Console.WriteLine(i);
                }

                private static int NewMethod(int i)
                {
                    Console.WriteLine(i);

                    if (int.Parse("1") > 0)
                    {
                        i = 10;
                    }

                    return i;
                }
            }
            """;

        await TestExtractMethodAsync(code, expected);
    }

    [Fact]
    public async Task MatrixCase_YesYesYesNoYesYesYesNo()
    {
        var code = """
            using System;

            class Program
            {
                void Test51()
                {
                    int i;

                    [|
                    Console.WriteLine(i);

                    i = 10;
                    |]

                    Console.WriteLine(i);
                }
            }
            """;

        var expected = """
            using System;

            class Program
            {
                void Test51()
                {
                    int i;

                    i = NewMethod(i);

                    Console.WriteLine(i);
                }

                private static int NewMethod(int i)
                {
                    Console.WriteLine(i);

                    i = 10;
                    return i;
                }
            }
            """;

        await TestExtractMethodAsync(code, expected);
    }

    [Fact]
    public async Task MatrixCase_YesYesYesNoYesYesYesYes()
    {
        var code = """
            using System;

            class Program
            {
                void Test52()
                {
                    int i = 10;

                    [|
                    Console.WriteLine(i);

                    i = 10;
                    |]

                    Console.WriteLine(i);
                }
            }
            """;

        var expected = """
            using System;

            class Program
            {
                void Test52()
                {
                    int i = 10;

                    i = NewMethod(i);

                    Console.WriteLine(i);
                }

                private static int NewMethod(int i)
                {
                    Console.WriteLine(i);

                    i = 10;
                    return i;
                }
            }
            """;

        await TestExtractMethodAsync(code, expected);
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539049")]
    public async Task ExtractMethodInProperty1()
    {
        var code = """
            class C2
            {
                static public int Area
                {
                    get { return 1; }
                }
            }

            class C3
            {
                public static int Area
                {
                    get
                    {
                        return [|C2.Area|];
                    }
                }
            }
            """;

        var expected = """
            class C2
            {
                static public int Area
                {
                    get { return 1; }
                }
            }

            class C3
            {
                public static int Area
                {
                    get
                    {
                        return GetArea();
                    }
                }

                private static int GetArea()
                {
                    return C2.Area;
                }
            }
            """;

        await TestExtractMethodAsync(code, expected);
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539049")]
    public async Task ExtractMethodInProperty2()
    {
        var code = """
            class C3
            {
                public static int Area
                {
                    get
                    {
                        [|int i = 10;
                        return i;|]
                    }
                }
            }
            """;

        var expected = """
            class C3
            {
                public static int Area
                {
                    get
                    {
                        return NewMethod();
                    }
                }

                private static int NewMethod()
                {
                    return 10;
                }
            }
            """;

        await TestExtractMethodAsync(code, expected);
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539049")]
    public async Task ExtractMethodInProperty3()
    {
        var code = """
            class C3
            {
                public static int Area
                {
                    set
                    {
                        [|int i = value;|]
                    }
                }
            }
            """;

        var expected = """
            class C3
            {
                public static int Area
                {
                    set
                    {
                        NewMethod(value);
                    }
                }

                private static void NewMethod(int value)
                {
                    int i = value;
                }
            }
            """;

        await TestExtractMethodAsync(code, expected);
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539029")]
    public async Task ExtractMethodProperty()
    {
        var code = """
            class Program
            {
                private double area = 1.0;
                public double Area
                {
                    get
                    {
                        return area;
                    }
                }

                public override string ToString()
                {
                    return string.Format("{0:F2}", [|Area|]);
                }
            }
            """;

        var expected = """
            class Program
            {
                private double area = 1.0;
                public double Area
                {
                    get
                    {
                        return area;
                    }
                }

                public override string ToString()
                {
                    return string.Format("{0:F2}", GetArea());
                }

                private double GetArea()
                {
                    return Area;
                }
            }
            """;
        await TestExtractMethodAsync(code, expected);
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539196")]
    public async Task ExtractMethodWithDeclareOneMoreVariablesInSameLineBeUsedAfter()
    {
        var code = """
            class C
            {
                void M()
                {
                    [|int x, y = 1;|]
                    x = 4;
                    Console.Write(x + y);
                }
            }
            """;
        var expected = """
            class C
            {
                void M()
                {
                    int x;
                    int y = NewMethod();
                    x = 4;
                    Console.Write(x + y);
                }

                private static int NewMethod()
                {
                    return 1;
                }
            }
            """;
        await TestExtractMethodAsync(code, expected);
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539196")]
    public async Task ExtractMethodWithDeclareOneMoreVariablesInSameLineNotBeUsedAfter()
    {
        var code = """
            class C
            {
                void M()
                {
                    [|int x, y = 1;|]
                }
            }
            """;
        var expected = """
            class C
            {
                void M()
                {
                    NewMethod();
                }

                private static void NewMethod()
                {
                    int x, y = 1;
                }
            }
            """;
        await TestExtractMethodAsync(code, expected);
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539214")]
    public async Task ExtractMethodForSplitOutStatementWithComments()
    {
        var code = """
            class C
            {
                void M()
                {
                    //start
                    [|int x, y;
                    x = 5;
                    y = 10;|]
                    //end
                    Console.Write(x + y);
                }
            }
            """;
        var expected = """
            class C
            {
                void M()
                {
                    //start
                    int x, y;
                    NewMethod(out x, out y);
                    //end
                    Console.Write(x + y);
                }

                private static void NewMethod(out int x, out int y)
                {
                    x = 5;
                    y = 10;
                }
            }
            """;
        await TestExtractMethodAsync(code, expected);
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539225")]
    public async Task Bug5098()
    {
        var code = """
            class Program
            {
                static void Main(string[] args)
                {
                    [|return;|]
                    Console.Write(4);
                }
            }
            """;

        await ExpectExtractMethodToFailAsync(code);
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539229")]
    public async Task Bug5107()
    {
        var code = """
            class Program
            {
                static void Main(string[] args)
                {
                    int i = 10;
                    [|int j = j + i;|]
                    Console.Write(i);
                    Console.Write(j); 
                }
            }
            """;

        var expected = """
            class Program
            {
                static void Main(string[] args)
                {
                    int i = 10;
                    int j = NewMethod(i);
                    Console.Write(i);
                    Console.Write(j);
                }

                private static int NewMethod(int i)
                {
                    return j + i;
                }
            }
            """;

        await TestExtractMethodAsync(code, expected);
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539500")]
    public async Task LambdaLiftedVariable1()
    {
        var code = """
            class Program
            {
                delegate void Func(ref int i, int r);
                static void Main(string[] args)
                {
                    int temp = 2;

                    Func fnc = (ref int arg, int arg2) => { [|temp = arg = arg2;|] };
                    temp = 4;
                    fnc(ref temp, 2);

                    System.Console.WriteLine(temp);
                }
            }
            """;

        var expected = """
            class Program
            {
                delegate void Func(ref int i, int r);
                static void Main(string[] args)
                {
                    int temp = 2;

                    Func fnc = (ref int arg, int arg2) => { NewMethod(out arg, arg2, out temp); };
                    temp = 4;
                    fnc(ref temp, 2);

                    System.Console.WriteLine(temp);
                }

                private static void NewMethod(out int arg, int arg2, out int temp)
                {
                    temp = arg = arg2;
                }
            }
            """;

        await TestExtractMethodAsync(code, expected);
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539488")]
    public async Task LambdaLiftedVariable2()
    {
        var code = """
            class Program
            {
                delegate void Action();
                static void Main(string[] args)
                {
                    int i = 0;
                    Action query = null;
                    if (i == 0)
                    {
                        query = () =>
                        {
                            System.Console.WriteLine(i);
                        };
                    }
                    [|i = 3;|]
                    query();
                }
            }
            """;

        var expected = """
            class Program
            {
                delegate void Action();
                static void Main(string[] args)
                {
                    int i = 0;
                    Action query = null;
                    if (i == 0)
                    {
                        query = () =>
                        {
                            System.Console.WriteLine(i);
                        };
                    }
                    i = NewMethod();
                    query();
                }

                private static int NewMethod()
                {
                    return 3;
                }
            }
            """;

        await TestExtractMethodAsync(code, expected);
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539531")]
    public async Task Bug5533()
    {
        var code = """
            using System;
            class Program
            {
                delegate void TestDelegate(ref int x);
                static void Main(string[] args)
                {
                    [|TestDelegate testDel = (ref int x) => { x = 10; };|]
                    int p = 2;
                    testDel(ref p);
                    Console.WriteLine(p);
                }
            }
            """;

        var expected = """
            using System;
            class Program
            {
                delegate void TestDelegate(ref int x);
                static void Main(string[] args)
                {
                    TestDelegate testDel = NewMethod();
                    int p = 2;
                    testDel(ref p);
                    Console.WriteLine(p);
                }

                private static TestDelegate NewMethod()
                {
                    return (ref int x) => { x = 10; };
                }
            }
            """;

        await TestExtractMethodAsync(code, expected);
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539531")]
    public async Task Bug5533_1()
    {
        var code = """
            using System;
            class Program
            {
                delegate void TestDelegate(ref int x);
                static void Main(string[] args)
                {
                    [|TestDelegate testDel = (ref int x) => { int y = x; x = 10; };|]
                    int p = 2;
                    testDel(ref p);
                    Console.WriteLine(p);
                }
            }
            """;

        var expected = """
            using System;
            class Program
            {
                delegate void TestDelegate(ref int x);
                static void Main(string[] args)
                {
                    TestDelegate testDel = NewMethod();
                    int p = 2;
                    testDel(ref p);
                    Console.WriteLine(p);
                }

                private static TestDelegate NewMethod()
                {
                    return (ref int x) => { int y = x; x = 10; };
                }
            }
            """;

        await TestExtractMethodAsync(code, expected);
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539531")]
    public async Task Bug5533_2()
    {
        var code = """
            using System;
            class Program
            {
                delegate void TestDelegate(ref int x);
                static void Main(string[] args)
                {
                    TestDelegate testDel = (ref int x) => { [|int y = x; x = 10;|] };
                    int p = 2;
                    testDel(ref p);
                    Console.WriteLine(p);
                }
            }
            """;

        var expected = """
            using System;
            class Program
            {
                delegate void TestDelegate(ref int x);
                static void Main(string[] args)
                {
                    TestDelegate testDel = (ref int x) => { x = NewMethod(x); };
                    int p = 2;
                    testDel(ref p);
                    Console.WriteLine(p);
                }

                private static int NewMethod(int x)
                {
                    int y = x; x = 10;
                    return x;
                }
            }
            """;

        await TestExtractMethodAsync(code, expected);
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539531")]
    public async Task Bug5533_3()
    {
        var code = """
            using System;
            class Program
            {
                delegate void TestDelegate(ref int x);
                static void Main(string[] args)
                {
                    [|TestDelegate testDel = delegate (ref int x) { x = 10; };|]
                    int p = 2;
                    testDel(ref p);
                    Console.WriteLine(p);
                }
            }
            """;

        var expected = """
            using System;
            class Program
            {
                delegate void TestDelegate(ref int x);
                static void Main(string[] args)
                {
                    TestDelegate testDel = NewMethod();
                    int p = 2;
                    testDel(ref p);
                    Console.WriteLine(p);
                }

                private static TestDelegate NewMethod()
                {
                    return delegate (ref int x) { x = 10; };
                }
            }
            """;

        await TestExtractMethodAsync(code, expected);
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539859")]
    public async Task LambdaLiftedVariable3()
    {
        var code = """
            using System;
            class Program
            {
                static void Main(string[] args)
                {
                    Action<int> F = x =>
                    {
                        Action<int> F2 = x2 =>
                        {
                            [|Console.WriteLine(args.Length + x2 + x);|]
                        };

                        F2(x);
                    };

                    F(args.Length);
                }
            }
            """;

        var expected = """
            using System;
            class Program
            {
                static void Main(string[] args)
                {
                    Action<int> F = x =>
                    {
                        Action<int> F2 = x2 =>
                        {
                            NewMethod(args, x, x2);
                        };

                        F2(x);
                    };

                    F(args.Length);
                }

                private static void NewMethod(string[] args, int x, int x2)
                {
                    Console.WriteLine(args.Length + x2 + x);
                }
            }
            """;

        await TestExtractMethodAsync(code, expected);
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539882")]
    public async Task Bug5982()
    {
        var code = """
            using System;
            using System.Collections.Generic;

            class Program
            {
                static void Main(string[] args)
                {
                    List<int> list = new List<int>();
                    Console.WriteLine([|list.Capacity|]);
                }
            }
            """;

        var expected = """
            using System;
            using System.Collections.Generic;

            class Program
            {
                static void Main(string[] args)
                {
                    List<int> list = new List<int>();
                    Console.WriteLine(GetCapacity(list));
                }

                private static int GetCapacity(List<int> list)
                {
                    return list.Capacity;
                }
            }
            """;

        await TestExtractMethodAsync(code, expected);
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539932")]
    public async Task Bug6041()
    {
        var code = """
            using System;
            class Program
            {
                delegate R Del<in T, out R>(T arg);
                public void Goo()
                {
                    Del<Exception, ArgumentException> d = (arg) => { return new ArgumentException(); };
                    [|d(new ArgumentException());|]
                }
            }
            """;

        var expected = """
            using System;
            class Program
            {
                delegate R Del<in T, out R>(T arg);
                public void Goo()
                {
                    Del<Exception, ArgumentException> d = (arg) => { return new ArgumentException(); };
                    NewMethod(d);
                }

                private static void NewMethod(Del<Exception, ArgumentException> d)
                {
                    d(new ArgumentException());
                }
            }
            """;

        await TestExtractMethodAsync(code, expected);
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540183")]
    public async Task ExtractMethod50()
    {
        var code = """
            class C
            {
                void Method()
                {
                    while (true)
                    {
                       [|int i = 1;
                       while (false)
                       {
                          int j = 1;|]
                       }
                    }
                }
            }
            """;

        var expected = """
            class C
            {
                void Method()
                {
                    while (true)
                    {
                        NewMethod();
                    }
                }

                private static void NewMethod()
                {
                    int i = 1;
                    while (false)
                    {
                        int j = 1;
                    }
                }
            }
            """;

        await TestExtractMethodAsync(code, expected);
    }

    [Fact]
    public async Task ExtractMethod51()
    {
        var code = """
            class C
            {
                void Method()
                {
                    while (true)
                    {
                        switch(1)
                        {
                            case 1:
                                [|int i = 10;
                                break;
                            case 2:
                                int i2 = 20;|]
                                break;
                        }
                    }
                }
            }
            """;

        var expected = """
            class C
            {
                void Method()
                {
                    while (true)
                    {
                        NewMethod();
                    }
                }

                private static void NewMethod()
                {
                    switch (1)
                    {
                        case 1:
                            int i = 10;
                            break;
                        case 2:
                            int i2 = 20;
                            break;
                    }
                }
            }
            """;

        await TestExtractMethodAsync(code, expected);
    }

    [Fact]
    public async Task ExtractMethod52()
    {
        var code = """
            class C
            {
                void Method()
                {
                    [|int i = 1;
                    while (false)
                    {
                       int j = 1;|]
                    }
                }
            }
            """;

        var expected = """
            class C
            {
                void Method()
                {
                    NewMethod();
                }

                private static void NewMethod()
                {
                    int i = 1;
                    while (false)
                    {
                        int j = 1;
                    }
                }
            }
            """;

        await TestExtractMethodAsync(code, expected);
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539963")]
    public async Task ExtractMethod53()
    {
        var code = """
            class Class
            {
                void Main()
                {
                    Enum e = Enum.[|Field|];
                }
            }
            enum Enum { }
            """;

        await ExpectExtractMethodToFailAsync(code);
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539964")]
    public async Task ExtractMethod54()
    {
        var code = """
            class Class
            {
                void Main([|string|][] args)
                {
                }
            }
            """;

        await ExpectExtractMethodToFailAsync(code);
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540072")]
    public async Task Bug6220()
    {
        var code = """
            class C
            {
                void Main()
                {
            [|        float f = 1.2f;
            |]        System.Console.WriteLine();
                }
            }
            """;

        var expected = """
            class C
            {
                void Main()
                {
                    NewMethod();
                    System.Console.WriteLine();
                }

                private static void NewMethod()
                {
                    float f = 1.2f;
                }
            }
            """;
        await TestExtractMethodAsync(code, expected);
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540072")]
    public async Task Bug6220_1()
    {
        var code = """
            class C
            {
                void Main()
                {
            [|        float f = 1.2f; // test
            |]        System.Console.WriteLine();
                }
            }
            """;

        var expected = """
            class C
            {
                void Main()
                {
                    NewMethod();
                    System.Console.WriteLine();
                }

                private static void NewMethod()
                {
                    float f = 1.2f; // test
                }
            }
            """;
        await TestExtractMethodAsync(code, expected);
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540071")]
    public async Task Bug6219()
    {
        var code = """
            class C
            {
                void Main()
                {
                    float @float = 1.2f;
                    [|@float = 1.44F;|]
                }
            }
            """;

        var expected = """
            class C
            {
                void Main()
                {
                    float @float = 1.2f;
                    @float = NewMethod();
                }

                private static float NewMethod()
                {
                    return 1.44F;
                }
            }
            """;
        await TestExtractMethodAsync(code, expected);
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540080")]
    public async Task Bug6230()
    {
        var code = """
            class C
            {
                void M()
                {
                    int v =[| /**/1 + 2|];
                    System.Console.WriteLine();
                }
            }
            """;

        var expected = """
            class C
            {
                void M()
                {
                    int v = GetV();
                    System.Console.WriteLine();
                }

                private static int GetV()
                {
                    return /**/1 + 2;
                }
            }
            """;
        await TestExtractMethodAsync(code, expected);
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540080")]
    public async Task Bug6230_1()
    {
        var code = """
            class C
            {
                void M()
                {
                    int v [|= /**/1 + 2|];
                    System.Console.WriteLine();
                }
            }
            """;

        var expected = """
            class C
            {
                void M()
                {
                    NewMethod();
                    System.Console.WriteLine();
                }

                private static void NewMethod()
                {
                    int v = /**/1 + 2;
                }
            }
            """;
        await TestExtractMethodAsync(code, expected);
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540052")]
    public async Task Bug6197()
    {
        var code = """
            using System;

            class Program
            {
                static void Main(string[] args)
                {
                    Func<int, int> d = [|NewMethod|];
                    d.Invoke(2);
                }

                private static int NewMethod(int x)
                {
                    return x * 2;
                }
            }
            """;

        var expected = """
            using System;

            class Program
            {
                static void Main(string[] args)
                {
                    Func<int, int> d = GetD();
                    d.Invoke(2);
                }

                private static Func<int, int> GetD()
                {
                    return NewMethod;
                }

                private static int NewMethod(int x)
                {
                    return x * 2;
                }
            }
            """;
        await TestExtractMethodAsync(code, expected);
    }

    [Fact, WorkItem(6277, "DevDiv_Projects/Roslyn")]
    public async Task Bug6277()
    {
        var code = """
            using System;

            class Program
            {
                static void Main(string[] args)
                {
                    [|int x;
                    x = 1;|]
                    return;
                    int y = x;
                }
            }
            """;

        var expected = """
            using System;

            class Program
            {
                static void Main(string[] args)
                {
                    int x = NewMethod();
                    return;
                    int y = x;
                }

                private static int NewMethod()
                {
                    return 1;
                }
            }
            """;
        await TestExtractMethodAsync(code, expected);
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540151")]
    public async Task ArgumentlessReturnWithConstIfExpression()
    {
        var code = """
            using System;

            class Program
            {
                void Test()
                {
                    [|if (true)
                        return;|]
                    Console.WriteLine();
                }
            }
            """;

        var expected = """
            using System;

            class Program
            {
                void Test()
                {
                    NewMethod();
                    return;
                    Console.WriteLine();
                }

                private static void NewMethod()
                {
                    if (true)
                        return;
                }
            }
            """;
        await TestExtractMethodAsync(code, expected);
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540151")]
    public async Task ArgumentlessReturnWithConstIfExpression_1()
    {
        var code = """
            using System;

            class Program
            {
                void Test()
                {
                    if (true)
                        [|if (true)
                            return;|]
                    Console.WriteLine();
                }
            }
            """;

        var expected = """
            using System;

            class Program
            {
                void Test()
                {
                    if (true)
                    {
                        NewMethod();
                        return;
                    }
                    Console.WriteLine();
                }

                private static void NewMethod()
                {
                    if (true)
                        return;
                }
            }
            """;
        await TestExtractMethodAsync(code, expected);
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540151")]
    public async Task ArgumentlessReturnWithConstIfExpression_2()
    {
        var code = """
            using System;

            class Program
            {
                void Test()
                {
                    [|if (true)
                        return;|]
                }
            }
            """;

        var expected = """
            using System;

            class Program
            {
                void Test()
                {
                    NewMethod();
                }

                private static void NewMethod()
                {
                    if (true)
                        return;
                }
            }
            """;
        await TestExtractMethodAsync(code, expected);
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540151")]
    public async Task ArgumentlessReturnWithConstIfExpression_3()
    {
        var code = """
            using System;

            class Program
            {
                void Test()
                {
                    if (true)
                        [|if (true)
                            return;|]
                }
            }
            """;

        var expected = """
            using System;

            class Program
            {
                void Test()
                {
                    if (true)
                    {
                        NewMethod();
                        return;
                    }
                }

                private static void NewMethod()
                {
                    if (true)
                        return;
                }
            }
            """;
        await TestExtractMethodAsync(code, expected);
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540154")]
    public async Task Bug6313()
    {
        var code = """
            using System;

            class Program
            {
                void Test(bool b)
                {
                    [|if (b)
                    {
                        return;
                    }
                    Console.WriteLine();|]
                }
            }
            """;

        var expected = """
            using System;

            class Program
            {
                void Test(bool b)
                {
                    NewMethod(b);
                }

                private static void NewMethod(bool b)
                {
                    if (b)
                    {
                        return;
                    }
                    Console.WriteLine();
                }
            }
            """;
        await TestExtractMethodAsync(code, expected);
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540154")]
    public async Task Bug6313_1()
    {
        var code = """
            using System;

            class Program
            {
                void Test(bool b)
                {
                    [|if (b)
                    {
                        return;
                    }|]
                    Console.WriteLine();
                }
            }
            """;

        await ExpectExtractMethodToFailAsync(code);
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540154")]
    public async Task Bug6313_2()
    {
        var code = """
            using System;

            class Program
            {
                int Test(bool b)
                {
                    [|if (b)
                    {
                        return 1;
                    }
                    Console.WriteLine();|]
                }
            }
            """;

        await ExpectExtractMethodToFailAsync(code);
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540154")]
    public async Task Bug6313_3()
    {
        var code = """
            using System;

            class Program
            {
                void Test()
                {
                    [|bool b = true;
                    if (b)
                    {
                        return;
                    }

                    Action d = () =>
                    {
                        if (b)
                        {
                            return;
                        }
                        Console.WriteLine(1);
                    };|]
                }
            }
            """;
        var expected = """
            using System;

            class Program
            {
                void Test()
                {
                    NewMethod();
                }

                private static void NewMethod()
                {
                    bool b = true;
                    if (b)
                    {
                        return;
                    }

                    Action d = () =>
                    {
                        if (b)
                        {
                            return;
                        }
                        Console.WriteLine(1);
                    };
                }
            }
            """;
        await TestExtractMethodAsync(code, expected);
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540154")]
    public async Task Bug6313_4()
    {
        var code = """
            using System;

            class Program
            {
                void Test()
                {
                    [|Action d = () =>
                    {
                        int i = 1;
                        if (i > 10)
                        {
                            return;
                        }
                        Console.WriteLine(1);
                    };

                    Action d2 = () =>
                    {
                        int i = 1;
                        if (i > 10)
                        {
                            return;
                        }
                        Console.WriteLine(1);
                    };|]

                    Console.WriteLine(1);
                }
            }
            """;
        var expected = """
            using System;

            class Program
            {
                void Test()
                {
                    NewMethod();

                    Console.WriteLine(1);
                }

                private static void NewMethod()
                {
                    Action d = () =>
                    {
                        int i = 1;
                        if (i > 10)
                        {
                            return;
                        }
                        Console.WriteLine(1);
                    };

                    Action d2 = () =>
                    {
                        int i = 1;
                        if (i > 10)
                        {
                            return;
                        }
                        Console.WriteLine(1);
                    };
                }
            }
            """;
        await TestExtractMethodAsync(code, expected);
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540154")]
    public async Task Bug6313_5()
    {
        var code = """
            using System;

            class Program
            {
                void Test()
                {
                    Action d = () =>
                    {
                        [|int i = 1;
                        if (i > 10)
                        {
                            return;
                        }
                        Console.WriteLine(1);|]
                    };
                }
            }
            """;
        var expected = """
            using System;

            class Program
            {
                void Test()
                {
                    Action d = () =>
                    {
                        NewMethod();
                    };
                }

                private static void NewMethod()
                {
                    int i = 1;
                    if (i > 10)
                    {
                        return;
                    }
                    Console.WriteLine(1);
                }
            }
            """;
        await TestExtractMethodAsync(code, expected);
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540154")]
    public async Task Bug6313_6()
    {
        var code = """
            using System;

            class Program
            {
                void Test()
                {
                    Action d = () =>
                    {
                        [|int i = 1;
                        if (i > 10)
                        {
                            return;
                        }|]
                        Console.WriteLine(1);
                    };
                }
            }
            """;
        await ExpectExtractMethodToFailAsync(code);
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540170")]
    public async Task Bug6333()
    {
        var code = """
            using System;

            class Program
            {
                void Test()
                {
                    Program p;
                    [|p = new Program()|];
                }
            }
            """;
        var expected = """
            using System;

            class Program
            {
                void Test()
                {
                    Program p;
                    p = NewMethod();
                }

                private static Program NewMethod()
                {
                    return new Program();
                }
            }
            """;

        await TestExtractMethodAsync(code, expected);
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540216")]
    public async Task Bug6393()
    {
        var code = """
            using System;

            class Program
            {
                object Test<T>()
                {
                    T abcd; [|abcd = new T()|];
                    return abcd;
                }
            }
            """;
        var expected = """
            using System;

            class Program
            {
                object Test<T>()
                {
                    T abcd; abcd = NewMethod<T>();
                    return abcd;
                }

                private static T NewMethod<T>()
                {
                    return new T();
                }
            }
            """;

        await TestExtractMethodAsync(code, expected);
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540184")]
    public async Task Bug6351()
    {
        var code = """
            class Test
            {
                void method()
                {
                    if (true)
                    {
                        for (int i = 0; i < 5; i++)
                        {
                            /*Begin*/
                            [|System.Console.WriteLine();
                        }
                        System.Console.WriteLine();|]
                        /*End*/
                    }
                }
            }
            """;
        var expected = """
            class Test
            {
                void method()
                {
                    if (true)
                    {
                        NewMethod();
                        /*End*/
                    }
                }

                private static void NewMethod()
                {
                    for (int i = 0; i < 5; i++)
                    {
                        /*Begin*/
                        System.Console.WriteLine();
                    }
                    System.Console.WriteLine();
                }
            }
            """;

        await TestExtractMethodAsync(code, expected);
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540184")]
    public async Task Bug6351_1()
    {
        var code = """
            class Test
            {
                void method()
                {
                    if (true)
                    {
                        for (int i = 0; i < 5; i++)
                        {
                            /*Begin*/
                            [|System.Console.WriteLine();
                        }
                        System.Console.WriteLine();
                        /*End*/|]
                    }
                }
            }
            """;
        var expected = """
            class Test
            {
                void method()
                {
                    if (true)
                    {
                        NewMethod();
                    }
                }

                private static void NewMethod()
                {
                    for (int i = 0; i < 5; i++)
                    {
                        /*Begin*/
                        System.Console.WriteLine();
                    }
                    System.Console.WriteLine();
                    /*End*/
                }
            }
            """;

        await TestExtractMethodAsync(code, expected);
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540184")]
    public async Task Bug6351_2()
    {
        var code = """
            class Test
            {
                void method()
                {
                    if (true)
                    [|{
                        for (int i = 0; i < 5; i++)
                        {
                            /*Begin*/
                            System.Console.WriteLine();
                        }
                        System.Console.WriteLine();
                        /*End*/
                    }|]
                }
            }
            """;
        var expected = """
            class Test
            {
                void method()
                {
                    if (true)
                        NewMethod();
                }

                private static void NewMethod()
                {
                    for (int i = 0; i < 5; i++)
                    {
                        /*Begin*/
                        System.Console.WriteLine();
                    }
                    System.Console.WriteLine();
                    /*End*/
                }
            }
            """;

        await TestExtractMethodAsync(code, expected);
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540333")]
    public async Task Bug6560()
    {
        var code = """
            using System;
            class Program
            {
                static void Main(string[] args)
                {
                    string S = [|null|];
                    int Y = S.Length;
                }
            }
            """;
        var expected = """
            using System;
            class Program
            {
                static void Main(string[] args)
                {
                    string S = GetS();
                    int Y = S.Length;
                }

                private static string GetS()
                {
                    return null;
                }
            }
            """;

        await TestExtractMethodAsync(code, expected);
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540335")]
    public async Task Bug6562()
    {
        var code = """
            using System;
            class Program
            {
                int y = [|10|];
            }
            """;
        var expected = """
            using System;
            class Program
            {
                int y = GetY();

                private static int GetY()
                {
                    return 10;
                }
            }
            """;

        await TestExtractMethodAsync(code, expected);
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540335")]
    public async Task Bug6562_1()
    {
        var code = """
            using System;
            class Program
            {
                const int i = [|10|];
            }
            """;
        await ExpectExtractMethodToFailAsync(code);
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540335")]
    public async Task Bug6562_2()
    {
        var code = """
            using System;
            class Program
            {
                Func<string> f = [|() => "test"|];
            }
            """;
        var expected = """
            using System;
            class Program
            {
                Func<string> f = GetF();

                private static Func<string> GetF()
                {
                    return () => "test";
                }
            }
            """;

        await TestExtractMethodAsync(code, expected);
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540335")]
    public async Task Bug6562_3()
    {
        var code = """
            using System;
            class Program
            {
                Func<string> f = () => [|"test"|];
            }
            """;
        var expected = """
            using System;
            class Program
            {
                Func<string> f = () => NewMethod();

                private static string NewMethod()
                {
                    return "test";
                }
            }
            """;

        await TestExtractMethodAsync(code, expected);
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540361")]
    public async Task Bug6598()
    {
        var code = """
            using System;
            using System.Collections.Generic;
            using System.Linq;

            class 
            {
                static void Main(string[] args)
                {
                    [|Program|]
                }
            }
            """;
        await ExpectExtractMethodToFailAsync(code);
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540372")]
    public async Task Bug6613()
    {
        var code = """
            #define A
            using System;

            class Program
            {
                static void Main(string[] args)
                {
                    #if A
                        [|Console.Write(5);|]
                    #endif 
                }
            }
            """;
        var expected = """
            #define A
            using System;

            class Program
            {
                static void Main(string[] args)
                {
            #if A
                    NewMethod();
            #endif
                }

                private static void NewMethod()
                {
                    Console.Write(5);
                }
            }
            """;

        await TestExtractMethodAsync(code, expected);
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540396")]
    public async Task InvalidSelection_MethodBody()
    {
        var code = """
            using System;

            class Program
            {
                static void Main(string[] args)
                {
                   void Method5(bool b1, bool b2)
                   [|{
                        if (b1)
                        {
                            if (b2)
                                return;
                            Console.WriteLine();
                        }
                        Console.WriteLine();
                    }|]
                }
            }
            """;
        await ExpectExtractMethodToFailAsync(code);
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541586")]
    public async Task StructThis()
    {
        var code = """
            struct S
            {
                void Goo()
                {
                    [|this = new S();|]
                }
            }
            """;
        var expected = """
            struct S
            {
                void Goo()
                {
                    NewMethod();
                }

                private void NewMethod()
                {
                    this = new S();
                }
            }
            """;
        await TestExtractMethodAsync(code, expected);
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541627")]
    public async Task DoNotUseConvertedTypeForImplicitNumericConversion()
    {
        var code = """
            class T
            {
                void Goo()
                {
                    int x1 = 5;
                    long x2 = [|x1|];
                }
            }
            """;
        var expected = """
            class T
            {
                void Goo()
                {
                    int x1 = 5;
                    long x2 = GetX2(x1);
                }

                private static int GetX2(int x1)
                {
                    return x1;
                }
            }
            """;
        await TestExtractMethodAsync(code, expected);
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541668")]
    public async Task BreakInSelection()
    {
        var code = """
            using System;
            using System.Collections.Generic;
            using System.Linq;

            class Program
            {
                static void Main(string[] args)
                {
                    string x1 = "Hello";
                    switch (x1)
                    {
                        case null:
                            int i1 = 10;
                            break;
                        default:
                            switch (x1)
                            {
                                default:
                                    switch (x1)
                                    {
                                        [|default:
                                            int j1 = 99;
                                            i1 = 45;
                                            x1 = "t";
                                            string j2 = i1.ToString() + j1.ToString() + x1;
                                            break;
                                    }
                                    break;
                            }
                            break;|]
                    }
                }
            }
            """;
        await ExpectExtractMethodToFailAsync(code);
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541671")]
    public async Task UnreachableCodeWithReturnStatement()
    {
        var code = """
            class Program
            {
                static void Main(string[] args)
                {
                    return;

                    [|int i1 = 45;
                    i1 = i1 + 10;|]

                    return;
                }
            }
            """;
        var expected = """
            class Program
            {
                static void Main(string[] args)
                {
                    return;

                    NewMethod();

                    return;
                }

                private static void NewMethod()
                {
                    int i1 = 45;
                    i1 = i1 + 10;
                }
            }
            """;
        await TestExtractMethodAsync(code, expected);
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539862")]
    public async Task DoNotBlindlyPutCapturedVariable1()
    {
        var code = """
            using System;
            class Program
            {
                private static readonly int v = 5;
                delegate int Del(int i);

                static void Main(string[] args)
                {
                    Ros.A a = new Ros.A();
                    Del d = (int x) => { [|a.F(x)|]; return x * v; };
                    d(3);
                }
            }

            namespace Ros
            {
                partial class A
                {
                    public void F(int s)
                    {
                    }
                }
            }
            """;
        var expected = """
            using System;
            class Program
            {
                private static readonly int v = 5;
                delegate int Del(int i);

                static void Main(string[] args)
                {
                    Ros.A a = new Ros.A();
                    Del d = (int x) => { NewMethod(x, a); return x * v; };
                    d(3);
                }

                private static void NewMethod(int x, Ros.A a)
                {
                    a.F(x);
                }
            }

            namespace Ros
            {
                partial class A
                {
                    public void F(int s)
                    {
                    }
                }
            }
            """;
        await TestExtractMethodAsync(code, expected);
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539862")]
    public async Task DoNotBlindlyPutCapturedVariable2()
    {
        var code = """
            using System;
            class Program
            {
                private static readonly int v = 5;
                delegate int Del(int i);

                static void Main(string[] args)
                {
                    Program p;
                    Del d = (int x) => { [|p = null;|]; return x * v; };
                    d(3);
                }
            }
            """;
        var expected = """
            using System;
            class Program
            {
                private static readonly int v = 5;
                delegate int Del(int i);

                static void Main(string[] args)
                {
                    Program p;
                    Del d = (int x) => { p = NewMethod(); ; return x * v; };
                    d(3);
                }

                private static Program NewMethod()
                {
                    return null;
                }
            }
            """;
        await TestExtractMethodAsync(code, expected);
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541889")]
    public async Task DoNotCrashOnRangeVariableSymbol()
    {
        var code = """
            class Test
            {
                public void Linq1()
                {
                    int[] numbers = { 5, 4, 1, 3, 9, 8, 6, 7, 2, 0 };
                    var lowNums = [|from|] n in numbers where n < 5 select n;
                }
            }
            """;
        await ExpectExtractMethodToFailAsync(code);
    }

    [Fact]
    public async Task ExtractRangeVariable()
    {
        var code = """
            using System.Linq;
            class Test
            {
                public void Linq1()
                {
                    string[] array = null;
                    var q = from string s in array select [|s|];
                }
            }
            """;

        var expected = """
            using System.Linq;
            class Test
            {
                public void Linq1()
                {
                    string[] array = null;
                    var q = from string s in array select GetS(s);
                }

                private static string GetS(string s)
                {
                    return s;
                }
            }
            """;
        await TestExtractMethodAsync(code, expected);
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542155")]
    public async Task GenericWithErrorType()
    {
        var code = """
            using Goo.Utilities;
            class Goo<T>
            {
            }

            class Bar
            {
                void gibberish()
                {
                    Goo<[|Integer|]> x = null;
                    x.IsEmpty();
                }
            }

            namespace Goo.Utilities
            {
                internal static class GooExtensions
                {
                    public static bool IsEmpty<T>(this Goo<T> source)
                    {
                        return false;
                    }
                }
            }
            """;
        var expected = """
            using Goo.Utilities;
            class Goo<T>
            {
            }

            class Bar
            {
                void gibberish()
                {
                    Goo<Integer> x = NewMethod();
                    x.IsEmpty();
                }

                private static Goo<Integer> NewMethod()
                {
                    return null;
                }
            }

            namespace Goo.Utilities
            {
                internal static class GooExtensions
                {
                    public static bool IsEmpty<T>(this Goo<T> source)
                    {
                        return false;
                    }
                }
            }
            """;

        await TestExtractMethodAsync(code, expected);
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542105")]
    public async Task NamedArgument()
    {
        var code = """
            using System;

            class C
            {
                int this[int x = 5, int y = 7] { get { return 0; } set { } }

                void Goo()
                {
                    var y = this[[|y|]: 1];
                }
            }
            """;
        var expected = """
            using System;

            class C
            {
                int this[int x = 5, int y = 7] { get { return 0; } set { } }

                void Goo()
                {
                    var y = GetY();
                }

                private int GetY()
                {
                    return this[y: 1];
                }
            }
            """;

        await TestExtractMethodAsync(code, expected);
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542213")]
    public async Task QueryExpressionVariable()
    {
        var code = """
            using System;
            using System.Linq;
            using System.Collections.Generic;

            class Program
            {
                static void Main(string[] args)
                {
                    var q2 =
                        from a in Enumerable.Range(1, 2)
                        from b in Enumerable.Range(1, 2)
                        where ([|a == b|])
                        select a;
                }
            }
            """;
        var expected = """
            using System;
            using System.Linq;
            using System.Collections.Generic;

            class Program
            {
                static void Main(string[] args)
                {
                    var q2 =
                        from a in Enumerable.Range(1, 2)
                        from b in Enumerable.Range(1, 2)
                        where (NewMethod(a, b))
                        select a;
                }

                private static bool NewMethod(int a, int b)
                {
                    return a == b;
                }
            }
            """;

        await TestExtractMethodAsync(code, expected);
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542465")]
    public async Task IsExpression()
    {
        var code = """
            using System;
            class Class1
            {
            }
            class IsTest
            {
                static void Test(Class1 o)
                {
                    var b = new Class1() is [|Class1|];
                }
                static void Main()
                {
                }
            }
            """;
        var expected = """
            using System;
            class Class1
            {
            }
            class IsTest
            {
                static void Test(Class1 o)
                {
                    var b = GetB();
                }

                private static bool GetB()
                {
                    return new Class1() is Class1;
                }

                static void Main()
                {
                }
            }
            """;

        await TestExtractMethodAsync(code, expected);
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542526")]
    public async Task TypeParametersInConstraint()
    {
        var code = """
            using System;
            using System.Collections.Generic;

            class A
            {
                static void Goo<T, S>(T x) where T : IList<S>
                {
                    var y = [|x.Count|];
                }
            }
            """;
        var expected = """
            using System;
            using System.Collections.Generic;

            class A
            {
                static void Goo<T, S>(T x) where T : IList<S>
                {
                    var y = GetY<T, S>(x);
                }

                private static int GetY<T, S>(T x) where T : IList<S>
                {
                    return x.Count;
                }
            }
            """;

        await TestExtractMethodAsync(code, expected);
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542619")]
    public async Task GlobalNamespaceInReturnType()
    {
        var code = """
            class Program
            {
                class System
                {
                    class Action { }
                }
                static global::System.Action a = () => { global::System.Console.WriteLine(); [|}|];
            }
            """;
        var expected = """
            class Program
            {
                class System
                {
                    class Action { }
                }
                static global::System.Action a = GetA();

                private static global::System.Action GetA()
                {
                    return () => { global::System.Console.WriteLine(); };
                }
            }
            """;

        await TestExtractMethodAsync(code, expected);
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542582")]
    public async Task ExtractMethodExpandSelectionOnFor()
    {
        var code = """
            using System;

            class Program
            {
                static void Main(string[] args)
                {
                    for (int i = 0; i < 10; i++)
                    [|{
                        Console.WriteLine(i);|]
                    }
                }
            }
            """;

        var expected = """
            using System;

            class Program
            {
                static void Main(string[] args)
                {
                    for (int i = 0; i < 10; i++)
                        NewMethod(i);
                }

                private static void NewMethod(int i)
                {
                    Console.WriteLine(i);
                }
            }
            """;

        await TestExtractMethodAsync(code, expected);
    }

    [Fact]
    public async Task ExtractMethodNotContainerOnFor()
    {
        var code = """
            using System;

            class Program
            {
                static void Main(string[] args)
                {
                    for (int i = 0; i < [|10|]; i++) ;
                }
            }
            """;

        var expected = """
            using System;

            class Program
            {
                static void Main(string[] args)
                {
                    for (int i = 0; i < NewMethod(); i++) ;
                }

                private static int NewMethod()
                {
                    return 10;
                }
            }
            """;

        await TestExtractMethodAsync(code, expected);
    }

    [Fact]
    public async Task ExtractMethodExpandSelectionOnForeach()
    {
        var code = """
            using System;

            class Program
            {
                static void Main(string[] args)
                {
                    foreach (var c in "123")
                    [|{
                        Console.Write(c);|]
                    }
                }
            }
            """;

        var expected = """
            using System;

            class Program
            {
                static void Main(string[] args)
                {
                    foreach (var c in "123")
                        NewMethod(c);
                }

                private static void NewMethod(char c)
                {
                    Console.Write(c);
                }
            }
            """;

        await TestExtractMethodAsync(code, expected);
    }

    [Fact]
    public async Task ExtractMethodNotContainerOnForeach()
    {
        var code = """
            using System;

            class Program
            {
                static void Main(string[] args)
                {
                    foreach (var c in [|"123"|])
                    {
                        Console.Write(c);
                    }
                }
            }
            """;

        var expected = """
            using System;

            class Program
            {
                static void Main(string[] args)
                {
                    foreach (var c in NewMethod())
                    {
                        Console.Write(c);
                    }
                }

                private static string NewMethod()
                {
                    return "123";
                }
            }
            """;

        await TestExtractMethodAsync(code, expected);
    }

    [Fact]
    public async Task ExtractMethodNotContainerOnElseClause()
    {
        var code = """
            using System;

            class Program
            {
                static void Main(string[] args)
                {
                    if ([|true|])
                    {
                    }
                    else
                    {
                    }
                }
            }
            """;

        var expected = """
            using System;

            class Program
            {
                static void Main(string[] args)
                {
                    if (NewMethod())
                    {
                    }
                    else
                    {
                    }
                }

                private static bool NewMethod()
                {
                    return true;
                }
            }
            """;

        await TestExtractMethodAsync(code, expected);
    }

    [Fact]
    public async Task ExtractMethodExpandSelectionOnLabel()
    {
        var code = """
            using System;

            class Program
            {
                static void Main(string[] args)
                {
                repeat:
                    Console.WriteLine("Roslyn")[|;|]
                    if (true)
                        goto repeat;
                }
            }
            """;

        var expected = """
            using System;

            class Program
            {
                static void Main(string[] args)
                {
                repeat:
                    NewMethod();
                    if (true)
                        goto repeat;
                }

                private static void NewMethod()
                {
                    Console.WriteLine("Roslyn");
                }
            }
            """;

        await TestExtractMethodAsync(code, expected);
    }

    [Fact]
    public async Task ExtractMethodNotContainerOnLabel()
    {
        var code = """
            using System;

            class Program
            {
                static void Main(string[] args)
                {
                repeat:
                    Console.WriteLine("Roslyn");
                    if ([|true|])
                        goto repeat;
                }
            }
            """;

        var expected = """
            using System;

            class Program
            {
                static void Main(string[] args)
                {
                repeat:
                    Console.WriteLine("Roslyn");
                    if (NewMethod())
                        goto repeat;
                }

                private static bool NewMethod()
                {
                    return true;
                }
            }
            """;

        await TestExtractMethodAsync(code, expected);
    }

    [Fact]
    public async Task ExtractMethodExpandSelectionOnSwitch()
    {
        var code = """
            using System;

            class Program
            {
                static void Main(string[] args)
                {
                    switch (args[0])
                    {
                        case "1": Console.WriteLine("one")[|;|] break;
                        default: Console.WriteLine("other"); break;
                    }
                }
            }
            """;

        var expected = """
            using System;

            class Program
            {
                static void Main(string[] args)
                {
                    switch (args[0])
                    {
                        case "1": NewMethod(); break;
                        default: Console.WriteLine("other"); break;
                    }
                }

                private static void NewMethod()
                {
                    Console.WriteLine("one");
                }
            }
            """;

        await TestExtractMethodAsync(code, expected);
    }

    [Fact]
    public async Task ExtractMethodNotContainerOnSwitch()
    {
        var code = """
            using System;

            class Program
            {
                static void Main(string[] args)
                {
                    switch ([|args[0]|])
                    {
                        case "1": Console.WriteLine("one"); break;
                        default: Console.WriteLine("other"); break;
                    }
                }
            }
            """;

        var expected = """
            using System;

            class Program
            {
                static void Main(string[] args)
                {
                    switch (NewMethod(args))
                    {
                        case "1": Console.WriteLine("one"); break;
                        default: Console.WriteLine("other"); break;
                    }
                }

                private static string NewMethod(string[] args)
                {
                    return args[0];
                }
            }
            """;

        await TestExtractMethodAsync(code, expected);
    }

    [Fact]
    public async Task ExtractMethodExpandSelectionOnDo()
    {
        var code = """
            using System;

            class Program
            {
                static void Main(string[] args)
                {
                    do
                    [|{
                        Console.WriteLine("I don't like");|]
                    } while (DateTime.Now.DayOfWeek == DayOfWeek.Monday);
                }
            }
            """;

        var expected = """
            using System;

            class Program
            {
                static void Main(string[] args)
                {
                    do
                        NewMethod(); while (DateTime.Now.DayOfWeek == DayOfWeek.Monday);
                }

                private static void NewMethod()
                {
                    Console.WriteLine("I don't like");
                }
            }
            """;

        await TestExtractMethodAsync(code, expected);
    }

    [Fact]
    public async Task ExtractMethodNotContainerOnDo()
    {
        var code = """
            using System;

            class Program
            {
                static void Main(string[] args)
                {
                    do
                    {
                        Console.WriteLine("I don't like");
                    } while ([|DateTime.Now.DayOfWeek == DayOfWeek.Monday|]);
                }
            }
            """;

        var expected = """
            using System;

            class Program
            {
                static void Main(string[] args)
                {
                    do
                    {
                        Console.WriteLine("I don't like");
                    } while (NewMethod());
                }

                private static bool NewMethod()
                {
                    return DateTime.Now.DayOfWeek == DayOfWeek.Monday;
                }
            }
            """;

        await TestExtractMethodAsync(code, expected);
    }

    [Fact]
    public async Task ExtractMethodExpandSelectionOnWhile()
    {
        var code = """
            using System;

            class Program
            {
                static void Main(string[] args)
                {
                    while (true)
                    [|{
                        ;|]
                    }
                }
            }
            """;

        var expected = """
            using System;

            class Program
            {
                static void Main(string[] args)
                {
                    while (true)
                        NewMethod();
                }

                private static void NewMethod()
                {
                    ;
                }
            }
            """;

        await TestExtractMethodAsync(code, expected);
    }

    [Fact]
    public async Task ExtractMethodExpandSelectionOnStruct()
    {
        var code = """
            using System;

            struct Goo
            {
                static Action a = () => { Console.WriteLine(); [|}|];
            }
            """;

        var expected = """
            using System;

            struct Goo
            {
                static Action a = GetA();

                private static Action GetA()
                {
                    return () => { Console.WriteLine(); };
                }
            }
            """;

        await TestExtractMethodAsync(code, expected);
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542619")]
    public async Task ExtractMethodIncludeGlobal()
    {
        var code = """
            class Program
            {
                class System
                {
                    class Action { }
                }
                static global::System.Action a = () => { global::System.Console.WriteLine(); [|}|];
                static void Main(string[] args)
                {
                }
            }
            """;

        var expected = """
            class Program
            {
                class System
                {
                    class Action { }
                }
                static global::System.Action a = GetA();

                private static global::System.Action GetA()
                {
                    return () => { global::System.Console.WriteLine(); };
                }

                static void Main(string[] args)
                {
                }
            }
            """;

        await TestExtractMethodAsync(code, expected);
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542582")]
    public async Task ExtractMethodExpandSelection()
    {
        var code = """
            class Program
            {
                static void Main(string[] args)
                {
                    for (int i = 0; i < 10; i++)
                    [|{
                        System.Console.WriteLine(i);|]
                    }
                }
            }
            """;

        var expected = """
            class Program
            {
                static void Main(string[] args)
                {
                    for (int i = 0; i < 10; i++)
                        NewMethod(i);
                }

                private static void NewMethod(int i)
                {
                    System.Console.WriteLine(i);
                }
            }
            """;

        await TestExtractMethodAsync(code, expected);
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542594")]
    public async Task ExtractMethodRename1()
    {
        var code = """
            class Program
            {
                static void Main()
                {
                    [|var i = 42;|]
                    var j = 42;
                }
                private static void NewMethod() { }
                private static void NewMethod2() { }
            }
            """;

        var expected = """
            class Program
            {
                static void Main()
                {
                    NewMethod1();
                    var j = 42;
                }

                private static void NewMethod1()
                {
                    var i = 42;
                }

                private static void NewMethod() { }
                private static void NewMethod2() { }
            }
            """;

        await TestExtractMethodAsync(code, expected);
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542594")]
    public async Task ExtractMethodRename2()
    {
        var code = """
            class Program
            {
                static void Main()
                {
                    NewMethod1();
                    [|var j = 42;|]
                }

                private static void NewMethod1()
                {
                    var i = 42;
                }

                private static void NewMethod() { }
                private static void NewMethod2() { }
            }
            """;

        var expected = """
            class Program
            {
                static void Main()
                {
                    NewMethod1();
                    NewMethod3();
                }

                private static void NewMethod3()
                {
                    var j = 42;
                }

                private static void NewMethod1()
                {
                    var i = 42;
                }

                private static void NewMethod() { }
                private static void NewMethod2() { }
            }
            """;

        await TestExtractMethodAsync(code, expected);
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542632")]
    public async Task ExtractMethodInInteractive1()
    {
        var code = @"int i; [|i = 2|]; i = 3;";
        var expected = """
            int i; i = NewMethod();

            static int NewMethod()
            {
                return 2;
            }

            i = 3;
            """;
        await TestExtractMethodAsync(code, expected, parseOptions: Options.Script);
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542670")]
    public async Task TypeParametersInConstraint1()
    {
        var code = """
            using System;
            using System.Collections.Generic;

            class A
            {
                static void Goo<T, S, U>(T x) where T : IList<S> where S : IList<U>
                {
                    var y = [|x.Count|];
                }
            }
            """;
        var expected = """
            using System;
            using System.Collections.Generic;

            class A
            {
                static void Goo<T, S, U>(T x) where T : IList<S> where S : IList<U>
                {
                    var y = GetY<T, S>(x);
                }

                private static int GetY<T, S>(T x) where T : IList<S>
                {
                    return x.Count;
                }
            }
            """;

        await TestExtractMethodAsync(code, expected);
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/706894")]
    [WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543012")]
    public async Task TypeParametersInConstraint2()
    {
        var code = """
            using System;

            interface I<T> where T : IComparable<T>
            {
                int Count { get; }
            }

            class A
            {
                static void Goo<T, S>(S x) where S : I<T> where T : IComparable<T>
                {
                    var y = [|x.Count|];
                }
            }
            """;
        var expected = """
            using System;

            interface I<T> where T : IComparable<T>
            {
                int Count { get; }
            }

            class A
            {
                static void Goo<T, S>(S x) where S : I<T> where T : IComparable<T>
                {
                    var y = GetY<T, S>(x);
                }

                private static int GetY<T, S>(S x)
                    where T : IComparable<T>
                    where S : I<T>
                {
                    return x.Count;
                }
            }
            """;

        await TestExtractMethodAsync(code, expected);
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/706894")]
    [WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543012")]
    public async Task TypeParametersInConstraint3()
    {
        var code = """
            using System;

            interface I<T> where T : class
            {
                int Count { get; }
            }

            class A
            {
                static void Goo<T, S>(S x) where S : I<T> where T : class
                {
                    var y = [|x.Count|];
                }
            }
            """;
        var expected = """
            using System;

            interface I<T> where T : class
            {
                int Count { get; }
            }

            class A
            {
                static void Goo<T, S>(S x) where S : I<T> where T : class
                {
                    var y = GetY<T, S>(x);
                }

                private static int GetY<T, S>(S x)
                    where T : class
                    where S : I<T>
                {
                    return x.Count;
                }
            }
            """;

        await TestExtractMethodAsync(code, expected);
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543012")]
    public async Task TypeParametersInConstraint4()
    {
        var code = """
            using System;
            using System.Collections.Generic;

            interface I<T> where T : class
            {
            }

            interface I2<T1, T2> : I<T1> where T1 : class, IEnumerable<T2> where T2 : struct
            {
                int Count { get; }
            }

            class A
            {
                public virtual void Goo<T, S>(S x) where S : IList<I2<IEnumerable<T>, T>> where T : struct
                {
                }
            }

            class B : A
            {
                public override void Goo<T, S>(S x)
                {
                    var y = [|x.Count|];
                }
            }
            """;
        var expected = """
            using System;
            using System.Collections.Generic;

            interface I<T> where T : class
            {
            }

            interface I2<T1, T2> : I<T1> where T1 : class, IEnumerable<T2> where T2 : struct
            {
                int Count { get; }
            }

            class A
            {
                public virtual void Goo<T, S>(S x) where S : IList<I2<IEnumerable<T>, T>> where T : struct
                {
                }
            }

            class B : A
            {
                public override void Goo<T, S>(S x)
                {
                    var y = GetY<T, S>(x);
                }

                private static int GetY<T, S>(S x)
                    where T : struct
                    where S : IList<I2<IEnumerable<T>, T>>
                {
                    return x.Count;
                }
            }
            """;

        await TestExtractMethodAsync(code, expected);
    }

    [Fact]
    public async Task NullabilityTypeParameters()
    {
        var code = """
            #nullable enable

            using System.Collections.Generic;

            public class Test
            {
                public int M(Dictionary<string, string?> v)
                {
                    [|return v.Count;|]
                }
            }
            """;
        var expected = """
            #nullable enable

            using System.Collections.Generic;

            public class Test
            {
                public int M(Dictionary<string, string?> v)
                {
                    return NewMethod(v);
                }

                private static int NewMethod(Dictionary<string, string?> v)
                {
                    return v.Count;
                }
            }
            """;

        await TestExtractMethodAsync(code, expected);
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543012")]
    public async Task TypeParametersInConstraintBestEffort()
    {
        var code = """
            using System;
            using System.Collections.Generic;
            using System.Linq;

            class A<T>
            {
                public virtual void Test<S>(S s) where S : T
                {
                }
            }

            class B : A<string>
            {
                public override void Test<S>(S s)
                {
                    var t = [|s.ToString()|];
                }
            }
            """;
        var expected = """
            using System;
            using System.Collections.Generic;
            using System.Linq;

            class A<T>
            {
                public virtual void Test<S>(S s) where S : T
                {
                }
            }

            class B : A<string>
            {
                public override void Test<S>(S s)
                {
                    var t = GetT(s);
                }

                private static string GetT<S>(S s) where S : string
                {
                    return s.ToString();
                }
            }
            """;

        await TestExtractMethodAsync(code, expected);
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542672")]
    public async Task ConstructedTypes()
    {
        var code = """
            using System;
            using System.Collections.Generic;

            class Program
            {
                static void Goo<T>()
                {
                    List<T> x = new List<T>();
                    Action a = () => Console.WriteLine([|x.Count|]);
                }
            }
            """;
        var expected = """
            using System;
            using System.Collections.Generic;

            class Program
            {
                static void Goo<T>()
                {
                    List<T> x = new List<T>();
                    Action a = () => Console.WriteLine(GetCount(x));
                }

                private static int GetCount<T>(List<T> x)
                {
                    return x.Count;
                }
            }
            """;

        await TestExtractMethodAsync(code, expected);
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542792")]
    public async Task TypeInDefault()
    {
        var code = """
            using System;
            using System.Collections.Generic;
            using System.Linq;

            class Program
            {
                static void Main(string[] args)
                {
                    Node<int, Exception> node = new Node<int, Exception>();
                }
            }

            class Node<K, T> where T : new()
            {
                public K Key;
                public T Item;
                public Node<K, T> NextNode;
                public Node()
                {
                    Key = default([|K|]);
                    Item = new T();
                    NextNode = null;
                    Console.WriteLine(Key);
                }
            }
            """;
        var expected = """
            using System;
            using System.Collections.Generic;
            using System.Linq;

            class Program
            {
                static void Main(string[] args)
                {
                    Node<int, Exception> node = new Node<int, Exception>();
                }
            }

            class Node<K, T> where T : new()
            {
                public K Key;
                public T Item;
                public Node<K, T> NextNode;
                public Node()
                {
                    Key = NewMethod();
                    Item = new T();
                    NextNode = null;
                    Console.WriteLine(Key);
                }

                private static K NewMethod()
                {
                    return default(K);
                }
            }
            """;

        await TestExtractMethodAsync(code, expected);
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542708")]
    public async Task Script_ArgumentException()
    {
        var code = """
            using System;
            public static void GetNonVirtualMethod<TDelegate>( Type type, string name)
            {
                Type delegateType = typeof(TDelegate);
                 var invoke = [|delegateType|].GetMethod("Invoke");
            }
            """;
        var expected = """
            using System;
            public static void GetNonVirtualMethod<TDelegate>( Type type, string name)
            {
                Type delegateType = typeof(TDelegate);
                var invoke = GetDelegateType(delegateType).GetMethod("Invoke");
            }

            static Type GetDelegateType(Type delegateType)
            {
                return delegateType;
            }
            """;

        await TestExtractMethodAsync(code, expected, parseOptions: Options.Script);
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/529008")]
    public async Task ReadOutSideIsUnReachable()
    {
        var code = """
            class Test
            {
                public static void Main()
                {
                    string str = string.Empty;
                    object obj;
                    [|lock (new string[][] { new string[] { str }, new string[] { str } })
                    {
                        obj = new object();
                        return;
                    }|]
                    System.Console.Write(obj);
                }
            }
            """;
        var expected = """
            class Test
            {
                public static void Main()
                {
                    string str = string.Empty;
                    object obj;
                    NewMethod(str, out obj);
                    return;
                    System.Console.Write(obj);
                }

                private static void NewMethod(string str, out object obj)
                {
                    lock (new string[][] { new string[] { str }, new string[] { str } })
                    {
                        obj = new object();
                        return;
                    }
                }
            }
            """;

        await TestExtractMethodAsync(code, expected);
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543186")]
    public async Task AnonymousTypePropertyName()
    {
        var code = """
            class C
            {
                void M()
                {
                    var x = new { [|String|] = true };
                }
            }
            """;
        var expected = """
            class C
            {
                void M()
                {
                    NewMethod();
                }

                private static void NewMethod()
                {
                    var x = new { String = true };
                }
            }
            """;
        await TestExtractMethodAsync(code, expected);
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543662")]
    public async Task ArgumentOfBaseConstrInit()
    {
        var code = """
            class O
            {
                public O(int t) : base([|t|])
                {
                }
            }
            """;
        var expected = """
            class O
            {
                public O(int t) : base(GetT(t))
                {
                }

                private static int GetT(int t)
                {
                    return t;
                }
            }
            """;

        await TestExtractMethodAsync(code, expected);
    }

    [Fact]
    public async Task UnsafeType()
    {
        var code = """
            unsafe class O
            {
                unsafe public O(int t)
                {
                    [|t = 1;|]
                }
            }
            """;
        var expected = """
            unsafe class O
            {
                unsafe public O(int t)
                {
                    t = NewMethod();
                }

                private static int NewMethod()
                {
                    return 1;
                }
            }
            """;

        await TestExtractMethodAsync(code, expected);
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544144")]
    public async Task CastExpressionWithImplicitUserDefinedConversion()
    {
        var code = """
            class C
            {
                static public implicit operator long(C i)
                {
                    return 5;
                }

                static void Main()
                {
                    C c = new C();
                    int y1 = [|(int)c|];
                }
            }
            """;
        var expected = """
            class C
            {
                static public implicit operator long(C i)
                {
                    return 5;
                }

                static void Main()
                {
                    C c = new C();
                    int y1 = GetY1(c);
                }

                private static int GetY1(C c)
                {
                    return (int)c;
                }
            }
            """;

        await TestExtractMethodAsync(code, expected);
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544387")]
    public async Task FixedPointerVariable()
    {
        var code = """
            class Test
            {
                static int x = 0;
                unsafe static void Main()
                {
                    fixed (int* p1 = &x)
                    {
                        int a1 = [|*p1|];
                    }
                }
            }
            """;
        var expected = """
            class Test
            {
                static int x = 0;
                unsafe static void Main()
                {
                    fixed (int* p1 = &x)
                    {
                        int a1 = GetA1(p1);
                    }
                }

                private static unsafe int GetA1(int* p1)
                {
                    return *p1;
                }
            }
            """;

        await TestExtractMethodAsync(code, expected);
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544444")]
    public async Task PointerDeclarationStatement()
    {
        var code = """
            class Program
            {
                unsafe static void Main()
                {
                    int* p1 = null;
                    [|int* p2 = p1;|]
                }
            }
            """;
        var expected = """
            class Program
            {
                unsafe static void Main()
                {
                    int* p1 = null;
                    NewMethod(p1);
                }

                private static unsafe void NewMethod(int* p1)
                {
                    int* p2 = p1;
                }
            }
            """;

        await TestExtractMethodAsync(code, expected);
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544446")]
    public async Task PrecededByCastExpr()
    {
        var code = """
            class Program
            {
                static void Main()
                {
                    int i1 = (int)[|5L|];
                }
            }
            """;
        var expected = """
            class Program
            {
                static void Main()
                {
                    int i1 = (int)NewMethod();
                }

                private static long NewMethod()
                {
                    return 5L;
                }
            }
            """;

        await TestExtractMethodAsync(code, expected);
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542944")]
    public async Task ExpressionWithLocalConst()
    {
        var code = """
            class Program
            {
                static void Main(string[] args)
                {
                    const string a = null;
                    [|a = null;|]
                }
            }
            """;
        var expected = """
            class Program
            {
                static void Main(string[] args)
                {
                    const string a = null;
                    a = NewMethod(a);
                }

                private static string NewMethod(string a)
                {
                    a = null;
                    return a;
                }
            }
            """;

        await TestExtractMethodAsync(code, expected);
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542944")]
    public async Task ExpressionWithLocalConst2()
    {
        var code = """
            class Program
            {
                static void Main(string[] args)
                {
                    const string a = null;
                    [|a = null;|]
                }
            }
            """;
        var expected = """
            class Program
            {
                static void Main(string[] args)
                {
                    const string a = null;
                    a = NewMethod(a);
                }

                private static string NewMethod(string a)
                {
                    a = null;
                    return a;
                }
            }
            """;

        await TestExtractMethodAsync(code, expected);
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544675")]
    public async Task HiddenPosition()
    {
        var code = """
            class Program
            {
                static void Main(string[] args)
                {
                    const string a = null;
                    [|a = null;|]
                }
            #line default
            #line hidden
            }
            """;

        await ExpectExtractMethodToFailAsync(code);
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530609")]
    public async Task NoCrashInteractive()
    {
        var code = """
            [|if (true)
            {
            }|]
            """;
        var expected = """
            NewMethod();

            static void NewMethod()
            {
                if (true)
                {
                }
            }
            """;

        await TestExtractMethodAsync(code, expected, parseOptions: new CSharpParseOptions(kind: SourceCodeKind.Script));
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530322")]
    public async Task ExtractMethodShouldNotBreakFormatting()
    {
        var code =
            """
            class C
            {
                void M(int i, int j, int k)
                {
                    M(0,
                      [|1|],
                      2);
                }
            }
            """;
        var expected = """
            class C
            {
                void M(int i, int j, int k)
                {
                    M(0,
                      NewMethod(),
                      2);
                }

                private static int NewMethod()
                {
                    return 1;
                }
            }
            """;

        await TestExtractMethodAsync(code, expected);
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/604389")]
    public async Task TestExtractLiteralExpression()
    {
        var code =
            """
            class Program
            {
                static void Main()
                {
                    var c = new C { X = { Y = { [|1|] } } };
                }
            }

            class C
            {
                public dynamic X;
            }
            """;
        var expected = """
            class Program
            {
                static void Main()
                {
                    var c = new C { X = { Y = { NewMethod() } } };
                }

                private static int NewMethod()
                {
                    return 1;
                }
            }

            class C
            {
                public dynamic X;
            }
            """;

        await TestExtractMethodAsync(code, expected);
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/604389")]
    public async Task TestExtractCollectionInitializer()
    {
        var code =
            """
            class Program
            {
                static void Main()
                {
                    var c = new C { X = { Y = [|{ 1 }|] } };
                }
            }

            class C
            {
                public dynamic X;
            }
            """;
        var expected = """
            class Program
            {
                static void Main()
                {
                    var c = GetC();
                }

                private static C GetC()
                {
                    return new C { X = { Y = { 1 } } };
                }
            }

            class C
            {
                public dynamic X;
            }
            """;

        await TestExtractMethodAsync(code, expected);
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/854662")]
    public async Task TestExtractCollectionInitializer2()
    {
        var code =
            """
            using System;
            using System.Collections.Generic;
            class Program
            {
                public Dictionary<int, int> A { get; private set; }
                static int Main(string[] args)
                {
                    int a = 0;
                    return new Program { A = { { [|a + 2|], 0 } } }.A.Count;
                }
            }
            """;
        var expected = """
            using System;
            using System.Collections.Generic;
            class Program
            {
                public Dictionary<int, int> A { get; private set; }
                static int Main(string[] args)
                {
                    int a = 0;
                    return new Program { A = { { NewMethod(a), 0 } } }.A.Count;
                }

                private static int NewMethod(int a)
                {
                    return a + 2;
                }
            }
            """;

        await TestExtractMethodAsync(code, expected);
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530267")]
    public async Task TestCoClassImplicitConversion()
    {
        var code =
            """
            using System;
            using System.Runtime.InteropServices;

            [CoClass(typeof(C))]
            [ComImport]
            [Guid("8D3A7A55-A8F5-4669-A5AD-996A3EB8F2ED")]
            interface I { }

            class C : I
            {
                static void Main()
                {
                    [|new I()|]; // Extract Method
                }
            }
            """;
        var expected = """
            using System;
            using System.Runtime.InteropServices;

            [CoClass(typeof(C))]
            [ComImport]
            [Guid("8D3A7A55-A8F5-4669-A5AD-996A3EB8F2ED")]
            interface I { }

            class C : I
            {
                static void Main()
                {
                    NewMethod(); // Extract Method
                }

                private static I NewMethod()
                {
                    return new I();
                }
            }
            """;

        await TestExtractMethodAsync(code, expected);
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530710")]
    public async Task TestOverloadResolution()
    {
        var code =
            """
            using System;

            static class C
            {
                static void Ex(this string x) { }

                static void Inner(Action<string> x, string y) { }
                static void Inner(Action<string> x, int y) { }
                static void Inner(Action<int> x, int y) { }

                static void Outer(object y, Action<string> x) { Console.WriteLine(1); }
                static void Outer(string y, Action<int> x) { Console.WriteLine(2); }

                static void Main()
                {
                    Outer(null, y => Inner(x => { [|x|].Ex(); }, y)); // Prints 1
                }
            }

            static class E
            {
                public static void Ex(this int x) { }
            }
            """;
        var expected = """
            using System;

            static class C
            {
                static void Ex(this string x) { }

                static void Inner(Action<string> x, string y) { }
                static void Inner(Action<string> x, int y) { }
                static void Inner(Action<int> x, int y) { }

                static void Outer(object y, Action<string> x) { Console.WriteLine(1); }
                static void Outer(string y, Action<int> x) { Console.WriteLine(2); }

                static void Main()
                {
                    Outer(null, (Action<string>)(y => Inner(x => { GetX(x).Ex(); }, y))); // Prints 1
                }

                private static string GetX(string x)
                {
                    return x;
                }
            }

            static class E
            {
                public static void Ex(this int x) { }
            }
            """;

        await TestExtractMethodAsync(code, expected);
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530710")]
    public async Task TestOverloadResolution1()
    {
        var code =
            """
            using System;

            static class C
            {
                static void Ex(this string x) { }

                static void Inner(Action<string> x, string y) { }
                static void Inner(Action<string> x, int y) { }
                static void Inner(Action<int> x, int y) { }

                static void Outer(object y, Action<string> x) { Console.WriteLine(1); }
                static void Outer(string y, Action<int> x) { Console.WriteLine(2); }

                static void Main()
                {
                    Outer(null, y => Inner(x => { [|x.Ex()|]; }, y)); // Prints 1
                }
            }

            static class E
            {
                public static void Ex(this int x) { }
            }
            """;
        var expected = """
            using System;

            static class C
            {
                static void Ex(this string x) { }

                static void Inner(Action<string> x, string y) { }
                static void Inner(Action<string> x, int y) { }
                static void Inner(Action<int> x, int y) { }

                static void Outer(object y, Action<string> x) { Console.WriteLine(1); }
                static void Outer(string y, Action<int> x) { Console.WriteLine(2); }

                static void Main()
                {
                    Outer(null, (Action<string>)(y => Inner(x => { NewMethod(x); }, y))); // Prints 1
                }

                private static void NewMethod(string x)
                {
                    x.Ex();
                }
            }

            static class E
            {
                public static void Ex(this int x) { }
            }
            """;

        await TestExtractMethodAsync(code, expected);
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530710")]
    public async Task TestOverloadResolution2()
    {
        var code =
            """
            using System;

            static class C
            {
                static void Ex(this string x) { }

                static void Inner(Action<string> x, string y) { }
                static void Inner(Action<string> x, int y) { }
                static void Inner(Action<int> x, int y) { }

                static void Outer(object y, Action<string> x) { Console.WriteLine(1); }
                static void Outer(string y, Action<int> x) { Console.WriteLine(2); }

                static void Main()
                {
                    Outer(null, y => Inner(x => { [|x.Ex();|] }, y)); // Prints 1
                }
            }

            static class E
            {
                public static void Ex(this int x) { }
            }
            """;
        var expected = """
            using System;

            static class C
            {
                static void Ex(this string x) { }

                static void Inner(Action<string> x, string y) { }
                static void Inner(Action<string> x, int y) { }
                static void Inner(Action<int> x, int y) { }

                static void Outer(object y, Action<string> x) { Console.WriteLine(1); }
                static void Outer(string y, Action<int> x) { Console.WriteLine(2); }

                static void Main()
                {
                    Outer(null, (Action<string>)(y => Inner(x => { NewMethod(x); }, y))); // Prints 1
                }

                private static void NewMethod(string x)
                {
                    x.Ex();
                }
            }

            static class E
            {
                public static void Ex(this int x) { }
            }
            """;

        await TestExtractMethodAsync(code, expected);
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/731924")]
    public async Task TestTreatEnumSpecial()
    {
        var code =
            """
            using System;

            class Program
            {
                public enum A
                {
                    A1,
                    A2
                }

                static void Main(string[] args)
                {
                    A a = A.A1;

                    [|Console.WriteLine(a);|]
                }
            }
            """;
        var expected = """
            using System;

            class Program
            {
                public enum A
                {
                    A1,
                    A2
                }

                static void Main(string[] args)
                {
                    A a = A.A1;

                    NewMethod(a);
                }

                private static void NewMethod(A a)
                {
                    Console.WriteLine(a);
                }
            }
            """;

        await TestExtractMethodAsync(code, expected);
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/756222")]
    public async Task TestReturnStatementInAsyncMethod()
    {
        var code =
            """
            using System.Threading.Tasks;

            class C
            {
                async Task<int> M()
                {
                    await Task.Yield();
                    [|return 3;|]
                }
            }
            """;
        var expected = """
            using System.Threading.Tasks;

            class C
            {
                async Task<int> M()
                {
                    await Task.Yield();
                    return NewMethod();
                }

                private static int NewMethod()
                {
                    return 3;
                }
            }
            """;

        await TestExtractMethodAsync(code, expected);
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/574576")]
    public async Task TestAsyncMethodWithRefOrOutParameters()
    {
        var code =
            """
            using System.Threading.Tasks;

            class C
            {
                public async void Goo()
                {
                    [|var q = 1;
                    var p = 2;
                    await Task.Yield();|]
                    var r = q;
                    var s = p;
                }
            }
            """;

        await ExpectExtractMethodToFailAsync(code);
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1025272")]
    public async Task TestAsyncMethodWithWellKnownValueType()
    {
        var code =
            """
            using System;
            using System.Threading;
            using System.Threading.Tasks;

            class Program
            {
                public async Task Hello()
                {
                    var cancellationToken = CancellationToken.None;

                    [|var i = await Task.Run(() =>
                    {
                        Console.WriteLine();
                        cancellationToken.ThrowIfCancellationRequested();

                        return 1;
                    }, cancellationToken);|]

                    cancellationToken.ThrowIfCancellationRequested();
                    Console.WriteLine(i);
                }
            }
            """;
        var expected = """
            using System;
            using System.Threading;
            using System.Threading.Tasks;

            class Program
            {
                public async Task Hello()
                {
                    var cancellationToken = CancellationToken.None;

                    int i = await NewMethod(ref cancellationToken);

                    cancellationToken.ThrowIfCancellationRequested();
                    Console.WriteLine(i);
                }

                private static async Task<int> NewMethod(ref CancellationToken cancellationToken)
                {
                    return await Task.Run(() =>
                    {
                        Console.WriteLine();
                        cancellationToken.ThrowIfCancellationRequested();

                        return 1;
                    }, cancellationToken);
                }
            }
            """;
        await ExpectExtractMethodToFailAsync(code, expected);
    }

    [Fact]
    public async Task TestAsyncMethodWithWellKnownValueType1()
    {
        var code =
            """
            using System;
            using System.Threading;
            using System.Threading.Tasks;

            class Program
            {
                public async Task Hello()
                {
                    var cancellationToken = CancellationToken.None;

                    [|var i = await Task.Run(() =>
                    {
                        Console.WriteLine();
                        cancellationToken = CancellationToken.None;

                        return 1;
                    }, cancellationToken);|]

                    cancellationToken.ThrowIfCancellationRequested();
                    Console.WriteLine(i);
                }
            }
            """;
        await ExpectExtractMethodToFailAsync(code);
    }

    [Fact]
    public async Task TestDoNotPutOutOrRefForStructOff()
    {
        var code =
            """
            using System.Threading.Tasks;

            namespace ClassLibrary9
            {
                public struct S
                {
                    public int I;
                }

                public class Class1
                {
                    private async Task<int> Test()
                    {
                        S s = new S();
                        s.I = 10;

                        [|int i = await Task.Run(() =>
                        {
                            var i2 = s.I;
                            return Test();
                        });|]

                        return i;
                    }
                }
            }
            """;
        await ExpectExtractMethodToFailAsync(code, dontPutOutOrRefOnStruct: false);
    }

    [Fact]
    public async Task TestDoNotPutOutOrRefForStructOn()
    {
        var code =
            """
            using System.Threading.Tasks;

            namespace ClassLibrary9
            {
                public struct S
                {
                    public int I;
                }

                public class Class1
                {
                    private async Task<int> Test()
                    {
                        S s = new S();
                        s.I = 10;

                        [|int i = await Task.Run(() =>
                        {
                            var i2 = s.I;
                            return Test();
                        });|]

                        return i;
                    }
                }
            }
            """;
        var expected =
            """
            using System.Threading.Tasks;

            namespace ClassLibrary9
            {
                public struct S
                {
                    public int I;
                }

                public class Class1
                {
                    private async Task<int> Test()
                    {
                        S s = new S();
                        s.I = 10;

                        int i = await NewMethod(s);

                        return i;
                    }

                    private async Task<int> NewMethod(S s)
                    {
                        return await Task.Run(() =>
                        {
                            var i2 = s.I;
                            return Test();
                        });
                    }
                }
            }
            """;

        await TestExtractMethodAsync(code, expected, dontPutOutOrRefOnStruct: true);
    }

    [Theory]
    [InlineData("add", "remove")]
    [InlineData("remove", "add")]
    [WorkItem("https://github.com/dotnet/roslyn/issues/17474")]
    public async Task TestExtractMethodEventAccessorUnresolvedName(string testedAccessor, string untestedAccessor)
    {
        // This code intentionally omits a 'using System;'
        var code =
$@"namespace ClassLibrary9
{{
    public class Class
    {{
        public event EventHandler Event
        {{
            {testedAccessor} {{ [|throw new NotImplementedException();|] }}
            {untestedAccessor} {{ throw new NotImplementedException(); }}
        }}
    }}
}}";
        var expected =
$@"namespace ClassLibrary9
{{
    public class Class
    {{
        public event EventHandler Event
        {{
            {testedAccessor} {{ NewMethod(); }}
            {untestedAccessor} {{ throw new NotImplementedException(); }}
        }}

        private static void NewMethod()
        {{
            throw new NotImplementedException();
        }}
    }}
}}";

        await TestExtractMethodAsync(code, expected);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/19958")]
    public async Task TestExtractMethodRefPassThrough()
    {
        var code =
            """
            using System;

            namespace ClassLibrary9
            {
                internal class ClassExtensions
                {
                    public static int OtherMethod(ref int x) => x;

                    public static void Method(ref int x)
                        => Console.WriteLine(OtherMethod(ref [|x|]));
                }
            }
            """;
        var expected =
            """
            using System;

            namespace ClassLibrary9
            {
                internal class ClassExtensions
                {
                    public static int OtherMethod(ref int x) => x;

                    public static void Method(ref int x)
                        => Console.WriteLine(OtherMethod(ref $x$));

                    public static ref int NewMethod(ref int x)
                    {
                        return ref x;
                    }
                }
            }
            """;

        await TestExtractMethodAsync(code, expected, temporaryFailing: true);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/19958")]
    public async Task TestExtractMethodRefPassThroughDuplicateVariable()
    {
        var code =
            """
            using System;

            namespace ClassLibrary9
            {
                internal interface IClass
                {
                    bool InterfaceMethod(ref Guid x, out IOtherClass y);
                }
                internal interface IOtherClass
                {
                    bool OtherInterfaceMethod();
                }

                internal static class ClassExtensions
                {
                    public static void Method(this IClass instance, Guid guid)
                    {
                        var r = instance.InterfaceMethod(ref [|guid|], out IOtherClass guid);
                        if (!r)
                            return;

                        r = guid.OtherInterfaceMethod();
                        if (r)
                            throw null;
                    }
                }
            }
            """;
        var expected =
            """
            using System;

            namespace ClassLibrary9
            {
                internal interface IClass
                {
                    bool InterfaceMethod(ref Guid x, out IOtherClass y);
                }
                internal interface IOtherClass
                {
                    bool OtherInterfaceMethod();
                }

                internal static class ClassExtensions
                {
                    public static void Method(this IClass instance, Guid guid)
                    {
                        var r = instance.InterfaceMethod(ref NewMethod(ref guid), out IOtherClass guid);
                        if (!r)
                            return;

                        r = guid.OtherInterfaceMethod();
                        if (r)
                            throw null;
                    }

                    public static ref Guid NewMethod(ref Guid guid)
                    {
                        return ref guid;
                    }
                }
            }
            """;

        await TestExtractMethodAsync(code, expected, temporaryFailing: true);
    }

    [Fact]
    public async Task ExtractMethod_Argument1()
    {
        var service = new CSharpExtractMethodService();
        Assert.NotNull(await Record.ExceptionAsync(async () =>
        {
            var tree = await service.ExtractMethodAsync(document: null, textSpan: default, localFunction: false, options: default, CancellationToken.None);
        }));
    }

    [Fact]
    public async Task ExtractMethod_Argument2()
    {
        var solution = new AdhocWorkspace().CurrentSolution;
        var projectId = ProjectId.CreateNewId();
        var project = solution.AddProject(projectId, "Project", "Project.dll", LanguageNames.CSharp).GetProject(projectId);

        var document = project.AddMetadataReference(TestMetadata.Net451.mscorlib)
                              .AddDocument("Document", SourceText.From(""));

        var service = new CSharpExtractMethodService() as IExtractMethodService;

        await service.ExtractMethodAsync(document, textSpan: default, localFunction: false, ExtractMethodGenerationOptions.GetDefault(project.Services), CancellationToken.None);
    }

    [WpfFact]
    [Trait(Traits.Feature, Traits.Features.Interactive)]
    public void ExtractMethodCommandDisabledInSubmission()
    {
        using var workspace = EditorTestWorkspace.Create(XElement.Parse("""
            <Workspace>
                <Submission Language="C#" CommonReferences="true">  
                    typeof(string).$$Name
                </Submission>
            </Workspace>
            """),
            workspaceKind: WorkspaceKind.Interactive,
            composition: EditorTestCompositions.EditorFeaturesWpf);
        // Force initialization.
        workspace.GetOpenDocumentIds().Select(id => workspace.GetTestDocument(id).GetTextView()).ToList();

        var textView = workspace.Documents.Single().GetTextView();

        var handler = workspace.ExportProvider.GetCommandHandler<ExtractMethodCommandHandler>(PredefinedCommandHandlerNames.ExtractMethod, ContentTypeNames.CSharpContentType);

        var state = handler.GetCommandState(new ExtractMethodCommandArgs(textView, textView.TextBuffer));
        Assert.True(state.IsUnspecified);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/18347")]
    public async Task ExtractMethodUnreferencedLocalFunction1()
    {
        var code = """
            namespace ExtractMethodCrashRepro
            {
                public static class SomeClass
                {
                    private static void Repro( int arg )
                    {
                        [|int localValue = arg;|]

                        int LocalCapture() => arg;
                    }
                }
            }
            """;
        var expected = """
            namespace ExtractMethodCrashRepro
            {
                public static class SomeClass
                {
                    private static void Repro( int arg )
                    {
                        NewMethod(arg);

                        int LocalCapture() => arg;
                    }

                    private static void NewMethod(int arg)
                    {
                        int localValue = arg;
                    }
                }
            }
            """;

        await TestExtractMethodAsync(code, expected);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/18347")]
    public async Task ExtractMethodUnreferencedLocalFunction2()
    {
        var code = """
            namespace ExtractMethodCrashRepro
            {
                public static class SomeClass
                {
                    private static void Repro( int arg )
                    {
                        int LocalCapture() => arg;

                        [|int localValue = arg;|]
                    }
                }
            }
            """;
        var expected = """
            namespace ExtractMethodCrashRepro
            {
                public static class SomeClass
                {
                    private static void Repro( int arg )
                    {
                        int LocalCapture() => arg;

                        NewMethod(arg);
                    }

                    private static void NewMethod(int arg)
                    {
                        int localValue = arg;
                    }
                }
            }
            """;

        await TestExtractMethodAsync(code, expected);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/18347")]
    public async Task ExtractMethodUnreferencedLocalFunction3()
    {
        var code = """
            namespace ExtractMethodCrashRepro
            {
                public static class SomeClass
                {
                    private static void Repro( int arg )
                    {
                        [|arg = arg + 3;|]

                        int LocalCapture() => arg;
                    }
                }
            }
            """;
        var expected = """
            namespace ExtractMethodCrashRepro
            {
                public static class SomeClass
                {
                    private static void Repro( int arg )
                    {
                        arg = NewMethod(arg);

                        int LocalCapture() => arg;
                    }

                    private static int NewMethod(int arg)
                    {
                        arg = arg + 3;
                        return arg;
                    }
                }
            }
            """;

        await TestExtractMethodAsync(code, expected);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/18347")]
    public async Task ExtractMethodUnreferencedLocalFunction4()
    {
        var code = """
            namespace ExtractMethodCrashRepro
            {
                public static class SomeClass
                {
                    private static void Repro( int arg )
                    {
                        int LocalCapture() => arg;

                        [|arg = arg + 3;|]
                    }
                }
            }
            """;
        var expected = """
            namespace ExtractMethodCrashRepro
            {
                public static class SomeClass
                {
                    private static void Repro( int arg )
                    {
                        int LocalCapture() => arg;

                        arg = NewMethod(arg);
                    }

                    private static int NewMethod(int arg)
                    {
                        arg = arg + 3;
                        return arg;
                    }
                }
            }
            """;

        await TestExtractMethodAsync(code, expected);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/18347")]
    public async Task ExtractMethodUnreferencedLocalFunction5()
    {
        var code = """
            namespace ExtractMethodCrashRepro
            {
                public static class SomeClass
                {
                    private static void Repro( int arg )
                    {
                        [|arg = arg + 3;|]

                        arg = 1;

                        int LocalCapture() => arg;
                    }
                }
            }
            """;
        var expected = """
            namespace ExtractMethodCrashRepro
            {
                public static class SomeClass
                {
                    private static void Repro( int arg )
                    {
                        arg = NewMethod(arg);

                        arg = 1;

                        int LocalCapture() => arg;
                    }

                    private static int NewMethod(int arg)
                    {
                        arg = arg + 3;
                        return arg;
                    }
                }
            }
            """;

        await TestExtractMethodAsync(code, expected);
    }

    [Theory]
    [InlineData("LocalCapture();")]
    [InlineData("System.Func<int> function = LocalCapture;")]
    [InlineData("System.Func<int> function = () => LocalCapture();")]
    [WorkItem("https://github.com/dotnet/roslyn/issues/18347")]
    public async Task ExtractMethodFlowsToLocalFunction1(string usageSyntax)
    {
        var code = $@"namespace ExtractMethodCrashRepro
{{
    public static class SomeClass
    {{
        private static void Repro( int arg )
        {{
            [|arg = arg + 3;|]

            {usageSyntax}

            int LocalCapture() => arg;
        }}
    }}
}}";
        var expected = $@"namespace ExtractMethodCrashRepro
{{
    public static class SomeClass
    {{
        private static void Repro( int arg )
        {{
            arg = NewMethod(arg);

            {usageSyntax}

            int LocalCapture() => arg;
        }}

        private static int NewMethod(int arg)
        {{
            arg = arg + 3;
            return arg;
        }}
    }}
}}";

        await TestExtractMethodAsync(code, expected);
    }

    [Theory]
    [InlineData("LocalCapture();")]
    [InlineData("System.Func<int> function = LocalCapture;")]
    [InlineData("System.Func<int> function = () => LocalCapture();")]
    [WorkItem("https://github.com/dotnet/roslyn/issues/18347")]
    public async Task ExtractMethodFlowsToLocalFunction2(string usageSyntax)
    {
        var code = $@"namespace ExtractMethodCrashRepro
{{
    public static class SomeClass
    {{
        private static void Repro( int arg )
        {{
            int LocalCapture() => arg;

            [|arg = arg + 3;|]

            {usageSyntax}
        }}
    }}
}}";
        var expected = $@"namespace ExtractMethodCrashRepro
{{
    public static class SomeClass
    {{
        private static void Repro( int arg )
        {{
            int LocalCapture() => arg;

            arg = NewMethod(arg);

            {usageSyntax}
        }}

        private static int NewMethod(int arg)
        {{
            arg = arg + 3;
            return arg;
        }}
    }}
}}";

        await TestExtractMethodAsync(code, expected);
    }

    /// <summary>
    /// This test verifies that Extract Method works properly when the region to extract references a local
    /// function, the local function uses an unassigned but wholly local variable.
    /// </summary>
    [Theory]
    [InlineData("LocalCapture();")]
    [InlineData("System.Func<int> function = LocalCapture;")]
    [InlineData("System.Func<int> function = () => LocalCapture();")]
    [WorkItem("https://github.com/dotnet/roslyn/issues/18347")]
    public async Task ExtractMethodFlowsToLocalFunctionWithUnassignedLocal(string usageSyntax)
    {
        var code = $@"namespace ExtractMethodCrashRepro
{{
    public static class SomeClass
    {{
        private static void Repro( int arg )
        {{
            int local;
            int LocalCapture() => arg + local;

            [|arg = arg + 3;|]

            {usageSyntax}
        }}
    }}
}}";
        var expected = $@"namespace ExtractMethodCrashRepro
{{
    public static class SomeClass
    {{
        private static void Repro( int arg )
        {{
            int local;
            int LocalCapture() => arg + local;

            arg = NewMethod(arg);

            {usageSyntax}
        }}

        private static int NewMethod(int arg)
        {{
            arg = arg + 3;
            return arg;
        }}
    }}
}}";

        await TestExtractMethodAsync(code, expected);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/18347")]
    public async Task ExtractMethodDoesNotFlowToLocalFunction1()
    {
        var code = """
            namespace ExtractMethodCrashRepro
            {
                public static class SomeClass
                {
                    private static void Repro( int arg )
                    {
                        [|arg = arg + 3;|]

                        arg = 1;

                        int LocalCapture() => arg;
                    }
                }
            }
            """;
        var expected = """
            namespace ExtractMethodCrashRepro
            {
                public static class SomeClass
                {
                    private static void Repro( int arg )
                    {
                        arg = NewMethod(arg);

                        arg = 1;

                        int LocalCapture() => arg;
                    }

                    private static int NewMethod(int arg)
                    {
                        arg = arg + 3;
                        return arg;
                    }
                }
            }
            """;

        await TestExtractMethodAsync(code, expected);
    }

    [Fact]
    public async Task TestUnreachableCodeModifiedInside()
    {
        var code = """
            using System.Collections.Generic;
            using System.Linq;

            namespace ConsoleApp1
            {
                class Test
                {
                    IEnumerable<object> Crash0(IEnumerable<object> enumerable)
                    {
                        [|while (true) ;
                        enumerable = null;
                        var i = enumerable.Any();
                        return enumerable;|]
                    }
                }
            }
            """;

        var expected = """
            using System.Collections.Generic;
            using System.Linq;

            namespace ConsoleApp1
            {
                class Test
                {
                    IEnumerable<object> Crash0(IEnumerable<object> enumerable)
                    {
                        return NewMethod(ref enumerable);
                    }

                    private static IEnumerable<object> NewMethod(ref IEnumerable<object> enumerable)
                    {
                        while (true) ;
                        enumerable = null;
                        var i = enumerable.Any();
                        return enumerable;
                    }
                }
            }
            """;

        // allowMovingDeclaration: false is default behavior on VS. 
        // it doesn't affect result mostly but it does affect for symbols in unreachable code since
        // data flow in and out for the symbol is always set to false
        await TestExtractMethodAsync(code, expected);
    }

    [Fact]
    public async Task TestUnreachableCodeModifiedOutside()
    {
        var code = """
            using System.Collections.Generic;
            using System.Linq;

            namespace ConsoleApp1
            {
                class Test
                {
                    IEnumerable<object> Crash0(IEnumerable<object> enumerable)
                    {
                        [|while (true) ;
                        var i = enumerable.Any();|]
                        enumerable = null;
                        return enumerable;
                    }
                }
            }
            """;

        var expected = """
            using System.Collections.Generic;
            using System.Linq;

            namespace ConsoleApp1
            {
                class Test
                {
                    IEnumerable<object> Crash0(IEnumerable<object> enumerable)
                    {
                        NewMethod(enumerable);
                        enumerable = null;
                        return enumerable;
                    }

                    private static void NewMethod(IEnumerable<object> enumerable)
                    {
                        while (true) ;
                        var i = enumerable.Any();
                    }
                }
            }
            """;

        await TestExtractMethodAsync(code, expected);
    }

    [Fact]
    public async Task TestUnreachableCodeModifiedBoth()
    {
        var code = """
            using System.Collections.Generic;
            using System.Linq;

            namespace ConsoleApp1
            {
                class Test
                {
                    IEnumerable<object> Crash0(IEnumerable<object> enumerable)
                    {
                        [|while (true) ;
                        enumerable = null;
                        var i = enumerable.Any();|]
                        enumerable = null;
                        return enumerable;
                    }
                }
            }
            """;

        var expected = """
            using System.Collections.Generic;
            using System.Linq;

            namespace ConsoleApp1
            {
                class Test
                {
                    IEnumerable<object> Crash0(IEnumerable<object> enumerable)
                    {
                        enumerable = NewMethod(enumerable);
                        enumerable = null;
                        return enumerable;
                    }

                    private static IEnumerable<object> NewMethod(IEnumerable<object> enumerable)
                    {
                        while (true) ;
                        enumerable = null;
                        var i = enumerable.Any();
                        return enumerable;
                    }
                }
            }
            """;

        await TestExtractMethodAsync(code, expected);
    }

    [Fact]
    public async Task TestLocalFunctionParameters()
    {
        var code = """
            using System.Collections.Generic;
            using System.Linq;

            namespace ConsoleApp1
            {
                class Test
                {
                    public void Bar(int value)
                    {
                        void Local(int value2)
                        {
                            [|Bar(value, value2);|]
                        }
                    }
                }
            }
            """;

        var expected = """
            using System.Collections.Generic;
            using System.Linq;

            namespace ConsoleApp1
            {
                class Test
                {
                    public void Bar(int value)
                    {
                        void Local(int value2)
                        {
                            NewMethod(value, value2);
                        }
                    }

                    private void NewMethod(int value, int value2)
                    {
                        Bar(value, value2);
                    }
                }
            }
            """;

        await TestExtractMethodAsync(code, expected);
    }

    [Fact]
    public async Task TestDataFlowInButNoReadInside()
    {
        var code = """
            using System;
            using System.Collections.Generic;
            using System.Linq;
            using System.Text;
            using System.Threading.Tasks;

            namespace ConsoleApp39
            {
                class Program
                {
                    void Method(out object test)
                    {
                        test = null;

                        var a = test != null;
                        [|if (a)
                        {
                            return;
                        }

                        if (A == a)
                        {
                            test = new object();
                        }|]
                    }
                }
            }
            """;

        var expected = """
            using System;
            using System.Collections.Generic;
            using System.Linq;
            using System.Text;
            using System.Threading.Tasks;

            namespace ConsoleApp39
            {
                class Program
                {
                    void Method(out object test)
                    {
                        test = null;

                        var a = test != null;
                        NewMethod(ref test, a);
                    }

                    private static void NewMethod(ref object test, bool a)
                    {
                        if (a)
                        {
                            return;
                        }

                        if (A == a)
                        {
                            test = new object();
                        }
                    }
                }
            }
            """;

        await TestExtractMethodAsync(code, expected);
    }

    [Fact]
    public async Task AllowBestEffortForUnknownVariableDataFlow()
    {
        var code = """
            class Program
            {
                void Method(out object test)
                {
                    test = null;
                    var a = test != null;
                    [|if (a)
                    {
                        return;
                    }
                    if (A == a)
                    {
                        test = new object();
                    }|]
                }
            }
            """;
        var expected = """
            class Program
            {
                void Method(out object test)
                {
                    test = null;
                    var a = test != null;
                    NewMethod(ref test, a);
                }

                private static void NewMethod(ref object test, bool a)
                {
                    if (a)
                    {
                        return;
                    }
                    if (A == a)
                    {
                        test = new object();
                    }
                }
            }
            """;
        await TestExtractMethodAsync(code, expected);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/30750")]
    public async Task ExtractMethodInInterface()
    {
        var code = """
            interface Program
            {
                void Goo();

                void Test()
                {
                    [|Goo();|]
                }
            }
            """;
        var expected = """
            interface Program
            {
                void Goo();

                void Test()
                {
                    NewMethod();
                }

                void NewMethod()
                {
                    Goo();
                }
            }
            """;
        await TestExtractMethodAsync(code, expected);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/33242")]
    public async Task ExtractMethodInExpressionBodiedConstructors()
    {
        var code = """
            class Goo
            {
                private readonly string _bar;

                private Goo(string bar) => _bar = [|bar|];
            }
            """;
        var expected = """
            class Goo
            {
                private readonly string _bar;

                private Goo(string bar) => _bar = GetBar(bar);

                private static string GetBar(string bar)
                {
                    return bar;
                }
            }
            """;
        await TestExtractMethodAsync(code, expected);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/33242")]
    public async Task ExtractMethodInExpressionBodiedFinalizers()
    {
        var code = """
            class Goo
            {
                bool finalized;

                ~Goo() => finalized = [|true|];
            }
            """;
        var expected = """
            class Goo
            {
                bool finalized;

                ~Goo() => finalized = NewMethod();

                private static bool NewMethod()
                {
                    return true;
                }
            }
            """;
        await TestExtractMethodAsync(code, expected);
    }

    [Fact]
    public async Task ExtractMethodInvolvingFunctionPointer()
    {
        var code = """
            class C
            {
                void M(delegate*<delegate*<ref string, ref readonly int>> ptr1)
                {
                    string s = null;
                    _ = [|ptr1()|](ref s);
                }
            }
            """;

        var expected = """
            class C
            {
                void M(delegate*<delegate*<ref string, ref readonly int>> ptr1)
                {
                    string s = null;
                    _ = NewMethod(ptr1)(ref s);
                }

                private static delegate*<ref string, ref readonly int> NewMethod(delegate*<delegate*<ref string, ref readonly int>> ptr1)
                {
                    return ptr1();
                }
            }
            """;

        await TestExtractMethodAsync(code, expected);
    }

    [Fact]
    public async Task ExtractMethodInvolvingFunctionPointerWithTypeParameter()
    {
        var code = """
            class C
            {
                void M<T1, T2>(delegate*<T1, T2> ptr1)
                {
                    _ = [|ptr1|]();
                }
            }
            """;

        var expected = """
            class C
            {
                void M<T1, T2>(delegate*<T1, T2> ptr1)
                {
                    _ = GetPtr1(ptr1)();
                }

                private static delegate*<T1, T2> GetPtr1<T1, T2>(delegate*<T1, T2> ptr1)
                {
                    return ptr1;
                }
            }
            """;
        await TestExtractMethodAsync(code, expected);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/44260")]
    public async Task TopLevelStatement_ValueInAssignment()
    {
        var code = """
            bool local;
            local = [|true|];
            """;
        var expected = """
            bool local;

            static bool NewMethod()
            {
                return true;
            }

            local = NewMethod();
            """;
        await TestExtractMethodAsync(code, expected);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/44260")]
    public async Task TopLevelStatement_ArgumentInInvocation()
    {
        // Note: the cast should be simplified 
        // https://github.com/dotnet/roslyn/issues/44260

        var code = """
            System.Console.WriteLine([|"string"|]);
            """;
        var expected = """
            System.Console.WriteLine((string)NewMethod());

            static string NewMethod()
            {
                return "string";
            }
            """;
        await TestExtractMethodAsync(code, expected);
    }

    [Theory]
    [InlineData("unsafe")]
    [InlineData("checked")]
    [InlineData("unchecked")]
    [WorkItem("https://github.com/dotnet/roslyn/issues/4950")]
    public async Task ExtractMethodInvolvingUnsafeBlock(string keyword)
    {
        var code = $@"
using System;

class Program {{
    static void Main(string[] args)
    {{
        object value = args;

        [|
        IntPtr p;
        {keyword}
        {{
            object t = value;
            p = IntPtr.Zero;
        }}
        |]

        Console.WriteLine(p);
    }}
}}
";
        var expected = $@"
using System;

class Program {{
    static void Main(string[] args)
    {{
        object value = args;

        IntPtr p = NewMethod(value);

        Console.WriteLine(p);
    }}

    private static IntPtr NewMethod(object value)
    {{
        IntPtr p;
        {keyword}
        {{
            object t = value;
            p = IntPtr.Zero;
        }}

        return p;
    }}
}}
";
        await TestExtractMethodAsync(code, expected);
    }

    [Fact]
    public async Task ExtractRawStringLiteral_SingleLine()
    {
        var code = """"
            class C
            {
                void M(int y)
                {
                    var s = [|"""Hello world"""|];
                }
            }
            """";
        var expected = """"
            class C
            {
                void M(int y)
                {
                    var s = GetS();
                }

                private static string GetS()
                {
                    return """Hello world""";
                }
            }
            """";

        await TestExtractMethodAsync(code, expected);
    }

    [Fact]
    public async Task ExtractRawStringLiteralInterpolation_SingleLine()
    {
        var code = """"
            class C
            {
                void M(int y)
                {
                    var s = [|$"""{y}"""|];
                }
            }
            """";
        var expected = """"
            class C
            {
                void M(int y)
                {
                    var s = GetS(y);
                }

                private static string GetS(int y)
                {
                    return $"""{y}""";
                }
            }
            """";

        await TestExtractMethodAsync(code, expected);
    }

    [Fact]
    public async Task ExtractRawStringLiteralInterpolationHole_SingleLine()
    {
        var code = """"
            class C
            {
                void M(int y)
                {
                    var s = $"""{[|y|]}""";
                }
            }
            """";
        var expected = """"
            class C
            {
                void M(int y)
                {
                    var s = $"""{GetY(y)}""";
                }

                private static int GetY(int y)
                {
                    return y;
                }
            }
            """";

        await TestExtractMethodAsync(code, expected);
    }

    [Fact]
    public async Task ExtractRawStringLiteral_MultiLine()
    {
        var code = """"
            class C
            {
                void M(int y)
                {
                    var s = [|"""
                        Hello world
                        """|];
                }
            }
            """";
        var expected = """"
            class C
            {
                void M(int y)
                {
                    var s = GetS();
                }

                private static string GetS()
                {
                    return """
                        Hello world
                        """;
                }
            }
            """";

        await TestExtractMethodAsync(code, expected);
    }

    [Fact]
    public async Task ExtractRawStringLiteralInterpolation_MultiLine()
    {
        var code = """"
            class C
            {
                void M(int y)
                {
                    var s = $[|"""
                        {y}
                        """|];
                }
            }
            """";
        var expected = """"
            class C
            {
                void M(int y)
                {
                    var s = GetS(y);
                }

                private static string GetS(int y)
                {
                    return $"""
                        {y}
                        """;
                }
            }
            """";

        await TestExtractMethodAsync(code, expected);
    }
}
