// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.ExtractMethod;
using Microsoft.CodeAnalysis.Editor.UnitTests;
using Microsoft.CodeAnalysis.Editor.UnitTests.Extensions;
using Microsoft.CodeAnalysis.ExtractMethod;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Text.Editor.Commanding.Commands;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.ExtractMethod;

[Trait(Traits.Feature, Traits.Features.ExtractMethod)]
public sealed partial class ExtractMethodTests : ExtractMethodBase
{
    [Fact]
    public Task ExtractMethod1()
        => TestExtractMethodAsync("""
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
            """, """
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
            """);

    [Fact]
    public Task ExtractMethod_KeywordName()
        => TestExtractMethodAsync("""
            using System;

            class Program
            {
                void Test(string[] args)
                {
                    int @class = 0;
                    int @interface = 0;
                    [|@class++;
                    @interface++;|]
                    Console.WriteLine(@class + @interface);
                }
            }
            """, """
            using System;

            class Program
            {
                void Test(string[] args)
                {
                    int @class = 0;
                    int @interface = 0;
                    NewMethod(ref @class, ref @interface);
                    Console.WriteLine(@class + @interface);
                }

                private static void NewMethod(ref int @class, ref int @interface)
                {
                    @class++;
                    @interface++;
                }
            }
            """);

    [Fact]
    public Task ExtractMethod2()
        => TestExtractMethodAsync("""
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
            """, """
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
            """);

    [Fact]
    public Task ExtractMethod3()
        => TestExtractMethodAsync("""
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
            """, """
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
            """);

    [Fact]
    public Task ExtractMethod4()
        => TestExtractMethodAsync("""
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
            """, """
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
            """, temporaryFailing: true);

    [Fact]
    public Task ExtractMethod5()
        => TestExtractMethodAsync("""
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
            """, """
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
            """);

    [Fact]
    public Task ExtractMethod6()
        => TestExtractMethodAsync("""
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
            """, """
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
            """);

    [Fact]
    public Task ExtractMethod7()
        => TestExtractMethodAsync("""
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
            """, """
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
            """);

    [Fact]
    public Task ExtractMethod8()
        => TestExtractMethodAsync("""
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
            """, """
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
            """);

    [Fact]
    public Task ExtractMethod9()
        => TestExtractMethodAsync("""
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
            """, """
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
            """);

    [Fact]
    public Task ExtractMethod10()
        => TestExtractMethodAsync("""
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
            """, """
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
            """);

    [Fact]
    public Task ExtractMethod11()
        => TestExtractMethodAsync("""
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
            """, """
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
            """);

    [Fact]
    public Task ExtractMethod11_1()
        => TestExtractMethodAsync("""
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
            """, """
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
            """);

    [Fact]
    public Task ExtractMethod12()
        => TestExtractMethodAsync("""
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
            """, """
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
            """);

    [Fact]
    public Task ControlVariableInForeachStatement()
        => TestExtractMethodAsync("""
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
            """, """
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
            """);

    [Fact]
    public Task ExtractMethod14()
        => TestExtractMethodAsync("""
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
            """, """
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
            """, temporaryFailing: true);

    [Fact]
    public Task ExtractMethod15()
        => TestExtractMethodAsync("""
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
            """, """
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
            """);

    [Fact]
    public Task ExtractMethod16()
        => TestExtractMethodAsync("""
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
            """, """
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
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538932")]
    public Task ExtractMethod17()
        => TestExtractMethodAsync("""
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
            """, """
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
            """);

    [Fact]
    public Task ExtractMethod18()
        => TestExtractMethodAsync("""
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
            """, """
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
            """);

    [Fact]
    public Task ExtractMethod19()
        => TestExtractMethodAsync("""
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
            """, """
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
            """);

    [Fact]
    public Task ExtractMethod20()
        => TestExtractMethodAsync("""
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
            """, """
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
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542677")]
    public Task ExtractMethod21()
        => TestExtractMethodAsync("""
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
            """, """
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
            """);

    [Fact]
    public Task ExtractMethod22()
        => TestExtractMethodAsync("""
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
            """, """
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
            """);

    [Fact]
    public Task ExtractMethod23()
        => TestExtractMethodAsync("""
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
            """, """
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
            """);

    [Fact]
    public Task ExtractMethod24()
        => TestExtractMethodAsync("""
            using System;

            class Program
            {
                static void Main(string[] args)
                {
                    int y = [|int.Parse(args[0].ToString())|];
                }
            }
            """, """
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
            """);

    [Fact]
    public Task ExtractMethod25()
        => TestExtractMethodAsync("""
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
            """, """
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
            """);

    [Fact]
    public Task ExtractMethod26()
        => TestExtractMethodAsync("""
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
            """, """
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
            """);

    [Fact]
    public Task ExtractMethod27()
        => TestExtractMethodAsync("""
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
            """, """
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
            """);

    [Fact]
    public Task ExtractMethod28()
        => TestExtractMethodAsync("""
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
            """, """
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
            """);

    [Fact]
    public Task ExtractMethod29()
        => TestExtractMethodAsync("""
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
            """, """
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
            """);

    [Fact]
    public Task ExtractMethod30()
        => TestExtractMethodAsync("""
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
            """, """
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
            """);

    [Fact]
    public Task ExtractMethod31()
        => TestExtractMethodAsync("""
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
            """, """
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
            """);

    [Fact]
    public Task ExtractMethod32()
        => TestExtractMethodAsync("""
            using System;

            class Program
            {
                void Test()
                {
                    int v = 0;
                    Console.Write([|v|]);
                }
            }
            """, """
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
            """);

    [Fact, WorkItem(3792, "DevDiv_Projects/Roslyn")]
    public Task ExtractMethod33()
        => TestExtractMethodAsync("""
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
            """, """
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
            """);

    [Fact]
    public Task ExtractMethod34()
        => TestExtractMethodAsync("""
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
            """, """
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
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538239")]
    public Task ExtractMethod35()
        => TestExtractMethodAsync("""
            using System;

            class Program
            {
                static void Main(string[] args)
                {
                    int[] r = [|new int[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20 }|];
                }
            }
            """, """
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
            """);

    [Fact]
    public Task ExtractMethod36()
        => TestExtractMethodAsync("""
            using System;

            class Program
            {
                static void Main(ref int i)
                {
                    [|i = 1;|]
                }
            }
            """, """
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
            """);

    [Fact]
    public Task ExtractMethod37()
        => TestExtractMethodAsync("""
            using System;

            class Program
            {
                static void Main(out int i)
                {
                    [|i = 1;|]
                }
            }
            """, """
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
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538231")]
    public Task ExtractMethod38()
        => TestExtractMethodAsync("""
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
            """, """
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
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538231")]
    public Task ExtractMethod39()
        => TestExtractMethodAsync("""
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
            """, """
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
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538303")]
    public Task ExtractMethod40()
        => ExpectExtractMethodToFailAsync("""
            class Program
            {
                static void Main(string[] args)
                {
                    [|int x;|]
                }
            }
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/868414")]
    public Task ExtractMethodWithLeadingTrivia()
        => TestExtractMethodAsync(
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

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/632351")]
    public Task ExtractMethodFailForTypeInFromClause()
        => ExpectExtractMethodToFailAsync("""
            class Program
            {
                static void Main(string[] args)
                {
                    var z = from [|T|] x in e;
                }
            }
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/632351")]
    public Task ExtractMethodFailForTypeInFromClause_1()
        => ExpectExtractMethodToFailAsync("""
            class Program
            {
                static void Main(string[] args)
                {
                    var z = from [|W.T|] x in e;
                }
            }
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538314")]
    public Task ExtractMethod41()
        => TestExtractMethodAsync("""
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
            """, """
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
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538327")]
    public Task ExtractMethod42()
        => TestExtractMethodAsync("""
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
            """, """
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
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538327")]
    public Task ExtractMethod43()
        => TestExtractMethodAsync("""
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
            """, """
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
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538328")]
    public Task ExtractMethod44()
        => TestExtractMethodAsync("""
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
            """, """
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
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538393")]
    public Task ExtractMethod45()
        => TestExtractMethodAsync("""
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
            """, """
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
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538393")]
    public Task ExtractMethod46()
        => TestExtractMethodAsync("""
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
            """, """
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
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538399")]
    public Task ExtractMethod47()
        => TestExtractMethodAsync("""
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
            """, """
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
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538401")]
    public Task ExtractMethod48()
        => TestExtractMethodAsync("""
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
            """, """
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
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538405")]
    public Task ExtractMethod49()
        => TestExtractMethodAsync("""
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
            """, """
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
            """);

    [Fact]
    public Task ExtractMethodNormalProperty()
        => TestExtractMethodAsync("""
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
            """, """
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
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538932")]
    public Task ExtractMethodAutoProperty()
        => TestExtractMethodAsync("""
            class Class
            {
                public string Name { get; set; }
                static void Main()
                {
                   string str = new Class().[|Name|];
                }
            }
            """, """
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
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538402")]
    public Task BugFix3994()
        => TestExtractMethodAsync("""
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
            """, """
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
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538404")]
    public Task BugFix3996()
        => TestExtractMethodAsync("""
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
            """, """
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
            """);

    [Fact]
    public Task InsertionPoint()
        => TestExtractMethodAsync("""
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
            """, """
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
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538980")]
    public Task BugFix4757()
        => TestExtractMethodAsync("""
            class GenericMethod
            {
                void Method<T>(T t)
                {
                    T a;
                    [|a = t;|]
                }
            }
            """, """
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
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538980")]
    public Task BugFix4757_2()
        => TestExtractMethodAsync("""
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
            """, """
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
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538980")]
    public Task BugFix4757_3()
        => TestExtractMethodAsync("""
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
            """, """
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
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538422")]
    public Task BugFix4758()
        => TestExtractMethodAsync("""
            using System;
            class TestOutParameter
            {
                void Method(out int x)
                {
                    x = 5;
                    Console.Write([|x|]);
                }
            }
            """, """
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
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538422")]
    public Task BugFix4758_2()
        => TestExtractMethodAsync("""
            class TestOutParameter
            {
                void Method(out int x)
                {
                    x = 5;
                    Console.Write([|x|]);
                }
            }
            """, """
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
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538984")]
    public Task BugFix4761()
        => TestExtractMethodAsync("""
            using System;

            class A
            {
                void Method()
                {
                    System.Func<int, int> a = x => [|x * x|];
                }
            }
            """, """
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
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538997")]
    public Task BugFix4779()
        => TestExtractMethodAsync("""
            using System;

            class Program
            {
                static void Main()
                {
                    string s = ";
                    Func<string> f = [|s|].ToString;
                }
            }
            """, """
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
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538997")]
    public Task BugFix4779_2()
        => TestExtractMethodAsync("""
            using System;

            class Program
            {
                static void Main()
                {
                    string s = ";
                    var f = [|s|].ToString();
                }
            }
            """, """
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
            """);

    [Fact, WorkItem(4780, "DevDiv_Projects/Roslyn")]
    public Task BugFix4780()
        => TestExtractMethodAsync("""
            using System;

            class Program
            {
                static void Main()
                {
                    string s = ";
                    object f = (Func<string>)[|s.ToString|];
                }
            }
            """, """
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
            """);

    [Fact, WorkItem(4780, "DevDiv_Projects/Roslyn")]
    public Task BugFix4780_2()
        => TestExtractMethodAsync("""
            using System;

            class Program
            {
                static void Main()
                {
                    string s = ";
                    object f = (string)[|s.ToString()|];
                }
            }
            """, """
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
            """);

    [Fact, WorkItem(4782, "DevDiv_Projects/Roslyn")]
    public Task BugFix4782()
        => TestExtractMethodAsync("""
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
            """, """
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
            """);

    [Fact, WorkItem(4782, "DevDiv_Projects/Roslyn")]
    public Task BugFix4782_2()
        => ExpectExtractMethodToFailAsync("""
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
            """);

    [Fact, WorkItem(4791, "DevDiv_Projects/Roslyn")]
    public Task BugFix4791()
        => TestExtractMethodAsync("""
            class Program
            {
                delegate int Func(int a);

                static void Main(string[] args)
                {
                    Func v = (int a) => [|a|];
                }
            }
            """, """
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
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539019")]
    public Task BugFix4809()
        => TestExtractMethodAsync("""
            class Program
            {
                public Program()
                {
                    [|int x = 2;|]
                }
            }
            """, """
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
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539029")]
    public Task BugFix4813()
        => TestExtractMethodAsync("""
            using System;

            class Program
            {
                public Program()
                {
                    object o = [|new Program()|];
                }
            }
            """, """
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
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538425")]
    public Task BugFix4031()
        => TestExtractMethodAsync("""
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
            """, """
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
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/527499")]
    public Task BugFix3992()
        => TestExtractMethodAsync("""
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
            """, """
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
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539029")]
    public Task BugFix4823()
        => TestExtractMethodAsync("""
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
            """, """
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
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538985")]
    public Task BugFix4762()
        => TestExtractMethodAsync("""
            class Program
            {
                static void Main(string[] args)
                {
                    //comments
                    [|int x = 2;|]
                }
            }
            """, """
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
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538966")]
    public Task BugFix4744()
        => TestExtractMethodAsync("""
            class Program
            {
                static void Main(string[] args)
                {
                    [|int x = 2;
                    //comments|]
                }
            }
            """, """
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
            """);

    [Fact]
    public Task MatrixCase_NoNoNoNoNoYesNoNo()
        => TestExtractMethodAsync("""
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
            """, """
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
            """);

    [Fact]
    public Task MatrixCase_NoNoNoNoNoYesNoYes()
        => TestExtractMethodAsync("""
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
            """, """
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
            """);

    [Fact]
    public Task MatrixCase_NoNoNoNoNoYesYesNo()
        => TestExtractMethodAsync("""
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
            """, """
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
            """);

    [Fact]
    public Task MatrixCase_NoNoNoNoNoYesYesYes()
        => TestExtractMethodAsync("""
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
            """, """
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
            """);

    [Fact]
    public Task MatrixCase_NoNoNoNoYesYesNoNo()
        => TestExtractMethodAsync("""
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
            """, """
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
            """);

    [Fact]
    public Task MatrixCase_NoNoNoNoYesYesNoYes()
        => TestExtractMethodAsync("""
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
            """, """
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
            """);

    [Fact]
    public Task MatrixCase_NoNoNoNoYesYesYesNo()
        => TestExtractMethodAsync("""
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
            """, """
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
            """);

    [Fact]
    public Task MatrixCase_NoNoNoNoYesYesYesYes()
        => TestExtractMethodAsync("""
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
            """, """
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
            """);

    [Fact]
    public Task MatrixCase_NoNoNoYesNoNoNoNo()
        => ExpectExtractMethodToFailAsync("""
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
            """);

    [Fact]
    public Task MatrixCase_NoNoNoYesNoNoNoYes()
        => ExpectExtractMethodToFailAsync("""
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
            """);

    [Fact]
    public Task MatrixCase_NoNoNoYesNoYesNoNo()
        => TestExtractMethodAsync("""
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
            """, """
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
            """);

    [Fact]
    public Task MatrixCase_NoNoNoYesNoYesNoYes()
        => TestExtractMethodAsync("""
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
            """, """
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
            """);

    [Fact]
    public Task MatrixCase_NoNoNoYesYesNoNoNo()
        => TestExtractMethodAsync("""
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
            """, """
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
            """);

    [Fact]
    public Task MatrixCase_NoNoNoYesYesNoNoYes()
        => TestExtractMethodAsync("""
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
            """, """
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
            """);

    [Fact]
    public Task MatrixCase_NoNoNoYesYesYesNoNo()
        => TestExtractMethodAsync("""
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
            """, """
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
            """);

    [Fact]
    public Task MatrixCase_NoNoNoYesYesYesNoYes()
        => TestExtractMethodAsync("""
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
            """, """
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
            """);

    [Fact]
    public Task MatrixCase_NoNoYesNoNoYesNoNo()
        => TestExtractMethodAsync("""
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
            """, """
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
            """);

    [Fact]
    public Task MatrixCase_NoNoYesNoNoYesNoYes()
        => TestExtractMethodAsync("""
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
            """, """
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
            """);

    [Fact]
    public Task MatrixCase_NoNoYesNoNoYesYesNo()
        => TestExtractMethodAsync("""
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
            """, """
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
            """);

    [Fact]
    public Task MatrixCase_NoNoYesNoNoYesYesYes()
        => TestExtractMethodAsync("""
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
            """, """
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
            """);

    // dataflow in and out can be false for symbols in unreachable code
    // boolean indicates 
    // dataFlowIn: false, dataFlowOut: false, alwaysAssigned: true, variableDeclared: false, readInside: true, writtenInside: false, readOutside: false, writtenOutside: true
    [Fact]
    public Task MatrixCase_NoNoYesNoYesNoNoYes()
        => TestExtractMethodAsync("""
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
            """, """
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
            """);

    // dataflow in and out can be false for symbols in unreachable code
    // boolean indicates 
    // dataFlowIn: false, dataFlowOut: false, alwaysAssigned: true, variableDeclared: false, readInside: true, writtenInside: false, readOutside: true, writtenOutside: true
    [Fact]
    public Task MatrixCase_NoNoYesNoYesNoYesYes()
        => TestExtractMethodAsync("""
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
            """, """
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
            """);

    [Fact]
    public Task MatrixCase_NoNoYesNoYesYesNoNo()
        => TestExtractMethodAsync("""
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
            """, """
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
            """);

    [Fact]
    public Task MatrixCase_NoNoYesNoYesYesNoYes()
        => TestExtractMethodAsync("""
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
            """, """
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
            """);

    [Fact]
    public Task MatrixCase_NoNoYesNoYesYesYesNo()
        => TestExtractMethodAsync("""
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
            """, """
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
            """);

    [Fact]
    public Task MatrixCase_NoNoYesNoYesYesYesYes()
        => TestExtractMethodAsync("""
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
            """, """
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
            """);

    [Fact]
    public Task MatrixCase_NoNoYesYesNoYesNoNo()
        => TestExtractMethodAsync("""
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
            """, """
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
            """);

    [Fact]
    public Task MatrixCase_NoNoYesYesNoYesNoYes()
        => TestExtractMethodAsync("""
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
            """, """
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
            """);

    [Fact]
    public Task MatrixCase_NoNoYesYesYesYesNoNo()
        => TestExtractMethodAsync("""
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
            """, """
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
            """);

    [Fact]
    public Task MatrixCase_NoNoYesYesYesYesNoYes()
        => TestExtractMethodAsync("""
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
            """, """
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
            """);

    [Fact]
    public Task MatrixCase_NoYesNoNoNoYesYesNo()
        => TestExtractMethodAsync("""
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
            """, """
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
            """);

    [Fact]
    public Task MatrixCase_NoYesNoNoNoYesYesYes()
        => TestExtractMethodAsync("""
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
            """, """
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
            """);

    [Fact]
    public Task MatrixCase_NoYesNoNoYesYesYesNo()
        => TestExtractMethodAsync("""
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
            """, """
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
            """);

    [Fact]
    public Task MatrixCase_NoYesNoNoYesYesYesYes()
        => TestExtractMethodAsync("""
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
            """, """
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
            """);

    [Fact]
    public Task MatrixCase_NoYesNoYesNoNoYesNo()
        => ExpectExtractMethodToFailAsync("""
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
            """);

    [Fact]
    public Task MatrixCase_NoYesNoYesNoNoYesYes()
        => ExpectExtractMethodToFailAsync("""
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
            """);

    [Fact]
    public Task MatrixCase_NoYesNoYesNoYesYesNo()
        => TestExtractMethodAsync("""
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
            """, """
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
            """);

    [Fact]
    public Task MatrixCase_NoYesNoYesNoYesYesYes()
        => TestExtractMethodAsync("""
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
            """, """
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
            """);

    [Fact]
    public Task MatrixCase_NoYesNoYesYesNoYesNo()
        => TestExtractMethodAsync("""
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
            """, """
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
            """);

    [Fact]
    public Task MatrixCase_NoYesNoYesYesNoYesYes()
        => TestExtractMethodAsync("""
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
            """, """
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
            """);

    [Fact]
    public Task MatrixCase_NoYesNoYesYesYesYesNo()
        => TestExtractMethodAsync("""
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
            """, """
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
            """);

    [Fact]
    public Task MatrixCase_NoYesNoYesYesYesYesYes()
        => TestExtractMethodAsync("""
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
            """, """
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
            """);

    [Fact]
    public Task MatrixCase_NoYesYesNoNoYesYesNo()
        => TestExtractMethodAsync("""
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
            """, """
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
            """);

    [Fact]
    public Task MatrixCase_NoYesYesNoNoYesYesYes()
        => TestExtractMethodAsync("""
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
            """, """
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
            """);

    [Fact]
    public Task MatrixCase_NoYesYesNoYesYesYesNo()
        => TestExtractMethodAsync("""
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
            """, """
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
            """);

    [Fact]
    public Task MatrixCase_NoYesYesNoYesYesYesYes()
        => TestExtractMethodAsync("""
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
            """, """
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
            """);

    [Fact]
    public Task MatrixCase_NoYesYesYesNoYesYesNo()
        => TestExtractMethodAsync("""
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
            """, """
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
            """);

    [Fact]
    public Task MatrixCase_NoYesYesYesNoYesYesYes()
        => TestExtractMethodAsync("""
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
            """, """
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
            """);

    [Fact]
    public Task MatrixCase_NoYesYesYesYesYesYesNo()
        => TestExtractMethodAsync("""
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
            """, """
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
            """);

    [Fact]
    public Task MatrixCase_NoYesYesYesYesYesYesYes()
        => TestExtractMethodAsync("""
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
            """, """
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
            """);

    [Fact]
    public Task MatrixCase_YesNoNoNoYesNoNoNo()
        => TestExtractMethodAsync("""
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
            """, """
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
            """);

    [Fact]
    public Task MatrixCase_YesNoNoNoYesNoNoYes()
        => TestExtractMethodAsync("""
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
            """, """
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
            """);

    [Fact]
    public Task MatrixCase_YesNoNoNoYesNoYesNo()
        => TestExtractMethodAsync("""
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
            """, """
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
            """);

    [Fact]
    public Task MatrixCase_YesNoNoNoYesNoYesYes()
        => TestExtractMethodAsync("""
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
            """, """
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
            """);

    [Fact]
    public Task MatrixCase_YesNoNoNoYesYesNoNo()
        => TestExtractMethodAsync("""
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
            """, """
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
            """);

    [Fact]
    public Task MatrixCase_YesNoNoNoYesYesNoYes()
        => TestExtractMethodAsync("""
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
            """, """
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
            """);

    [Fact]
    public Task MatrixCase_YesNoNoNoYesYesYesNo()
        => TestExtractMethodAsync("""
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
            """, """
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
            """);

    [Fact]
    public Task MatrixCase_YesNoNoNoYesYesYesYes()
        => TestExtractMethodAsync("""
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
            """, """
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
            """);

    [Fact]
    public Task MatrixCase_YesNoYesNoYesYesNoNo()
        => TestExtractMethodAsync("""
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
            """, """
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
            """);

    [Fact]
    public Task MatrixCase_YesNoYesNoYesYesNoYes()
        => TestExtractMethodAsync("""
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
            """, """
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
            """);

    [Fact]
    public Task MatrixCase_YesNoYesNoYesYesYesNo()
        => TestExtractMethodAsync("""
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
            """, """
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
            """);

    [Fact]
    public Task MatrixCase_YesNoYesNoYesYesYesYes()
        => TestExtractMethodAsync("""
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
            """, """
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
            """);

    [Fact]
    public Task MatrixCase_YesYesNoNoYesYesYesNo()
        => TestExtractMethodAsync("""
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
            """, """
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
            """);

    [Fact]
    public Task MatrixCase_YesYesNoNoYesYesYesYes()
        => TestExtractMethodAsync("""
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
            """, """
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
            """);

    [Fact]
    public Task MatrixCase_YesYesYesNoYesYesYesNo()
        => TestExtractMethodAsync("""
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
            """, """
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
            """);

    [Fact]
    public Task MatrixCase_YesYesYesNoYesYesYesYes()
        => TestExtractMethodAsync("""
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
            """, """
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
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539049")]
    public Task ExtractMethodInProperty1()
        => TestExtractMethodAsync("""
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
            """, """
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
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539049")]
    public Task ExtractMethodInProperty2()
        => TestExtractMethodAsync("""
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
            """, """
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
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539049")]
    public Task ExtractMethodInProperty3()
        => TestExtractMethodAsync("""
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
            """, """
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
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539029")]
    public Task ExtractMethodProperty()
        => TestExtractMethodAsync("""
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
            """, """
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
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539196")]
    public Task ExtractMethodWithDeclareOneMoreVariablesInSameLineBeUsedAfter()
        => TestExtractMethodAsync("""
            class C
            {
                void M()
                {
                    [|int x, y = 1;|]
                    x = 4;
                    Console.Write(x + y);
                }
            }
            """, """
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
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539196")]
    public Task ExtractMethodWithDeclareOneMoreVariablesInSameLineNotBeUsedAfter()
        => TestExtractMethodAsync("""
            class C
            {
                void M()
                {
                    [|int x, y = 1;|]
                }
            }
            """, """
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
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539214")]
    public Task ExtractMethodForSplitOutStatementWithComments()
        => TestExtractMethodAsync("""
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
            """, """
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
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539225")]
    public Task Bug5098()
        => ExpectExtractMethodToFailAsync("""
            class Program
            {
                static void Main(string[] args)
                {
                    [|return;|]
                    Console.Write(4);
                }
            }
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539229")]
    public Task Bug5107()
        => TestExtractMethodAsync("""
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
            """, """
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
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539500")]
    public Task LambdaLiftedVariable1()
        => TestExtractMethodAsync("""
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
            """, """
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
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539488")]
    public Task LambdaLiftedVariable2()
        => TestExtractMethodAsync("""
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
            """, """
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
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539531")]
    public Task Bug5533()
        => TestExtractMethodAsync("""
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
            """, """
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
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539531")]
    public Task Bug5533_1()
        => TestExtractMethodAsync("""
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
            """, """
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
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539531")]
    public Task Bug5533_2()
        => TestExtractMethodAsync("""
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
            """, """
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
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539531")]
    public Task Bug5533_3()
        => TestExtractMethodAsync("""
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
            """, """
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
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539859")]
    public Task LambdaLiftedVariable3()
        => TestExtractMethodAsync("""
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
            """, """
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
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539882")]
    public Task Bug5982()
        => TestExtractMethodAsync("""
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
            """, """
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
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539932")]
    public Task Bug6041()
        => TestExtractMethodAsync("""
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
            """, """
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
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540183")]
    public Task ExtractMethod50()
        => TestExtractMethodAsync("""
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
            """, """
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
            """);

    [Fact]
    public Task ExtractMethod51()
        => TestExtractMethodAsync("""
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
            """, """
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
            """);

    [Fact]
    public Task ExtractMethod52()
        => TestExtractMethodAsync("""
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
            """, """
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
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539963")]
    public Task ExtractMethod53()
        => ExpectExtractMethodToFailAsync("""
            class Class
            {
                void Main()
                {
                    Enum e = Enum.[|Field|];
                }
            }
            enum Enum { }
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539964")]
    public Task ExtractMethod54()
        => ExpectExtractMethodToFailAsync("""
            class Class
            {
                void Main([|string|][] args)
                {
                }
            }
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540072")]
    public Task Bug6220()
        => TestExtractMethodAsync("""
            class C
            {
                void Main()
                {
            [|        float f = 1.2f;
            |]        System.Console.WriteLine();
                }
            }
            """, """
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
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540072")]
    public Task Bug6220_1()
        => TestExtractMethodAsync("""
            class C
            {
                void Main()
                {
            [|        float f = 1.2f; // test
            |]        System.Console.WriteLine();
                }
            }
            """, """
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
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540071")]
    public Task Bug6219()
        => TestExtractMethodAsync("""
            class C
            {
                void Main()
                {
                    float @float = 1.2f;
                    [|@float = 1.44F;|]
                }
            }
            """, """
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
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540080")]
    public Task Bug6230()
        => TestExtractMethodAsync("""
            class C
            {
                void M()
                {
                    int v =[| /**/1 + 2|];
                    System.Console.WriteLine();
                }
            }
            """, """
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
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540080")]
    public Task Bug6230_1()
        => TestExtractMethodAsync("""
            class C
            {
                void M()
                {
                    int v [|= /**/1 + 2|];
                    System.Console.WriteLine();
                }
            }
            """, """
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
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540052")]
    public Task Bug6197()
        => TestExtractMethodAsync("""
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
            """, """
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
            """);

    [Fact, WorkItem(6277, "DevDiv_Projects/Roslyn")]
    public Task Bug6277()
        => TestExtractMethodAsync("""
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
            """, """
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
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540151")]
    public Task ArgumentlessReturnWithConstIfExpression()
        => TestExtractMethodAsync("""
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
            """, """
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
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540151")]
    public Task ArgumentlessReturnWithConstIfExpression_1()
        => TestExtractMethodAsync("""
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
            """, """
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
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540151")]
    public Task ArgumentlessReturnWithConstIfExpression_2()
        => TestExtractMethodAsync("""
            using System;

            class Program
            {
                void Test()
                {
                    [|if (true)
                        return;|]
                }
            }
            """, """
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
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540151")]
    public Task ArgumentlessReturnWithConstIfExpression_3()
        => TestExtractMethodAsync("""
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
            """, """
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
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540154")]
    public Task Bug6313()
        => TestExtractMethodAsync("""
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
            """, """
            using System;

            class Program
            {
                void Test(bool b)
                {
                    bool flowControl = NewMethod(b);
                    if (!flowControl)
                    {
                        return;
                    }
                }

                private static bool NewMethod(bool b)
                {
                    if (b)
                    {
                        return false;
                    }
                    Console.WriteLine();
                    return true;
                }
            }
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540154")]
    public Task Bug6313_1()
        => TestExtractMethodAsync("""
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
            """,
            """
            using System;

            class Program
            {
                void Test(bool b)
                {
                    bool flowControl = NewMethod(b);
                    if (!flowControl)
                    {
                        return;
                    }
                    Console.WriteLine();
                }

                private static bool NewMethod(bool b)
                {
                    if (b)
                    {
                        return false;
                    }

                    return true;
                }
            }
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540154")]
    public Task Bug6313_2()
        => TestExtractMethodAsync("""
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
            """, """
            using System;

            class Program
            {
                int Test(bool b)
                {
                    (bool flowControl, int value) = NewMethod(b);
                    if (!flowControl)
                    {
                        return value;
                    }
                }

                private static (bool flowControl, int value) NewMethod(bool b)
                {
                    if (b)
                    {
                        return (flowControl: false, value: 1);
                    }
                    Console.WriteLine();
                    return (flowControl: true, value: default);
                }
            }
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540154")]
    public Task Bug6313_3()
        => TestExtractMethodAsync("""
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
            """, """
            using System;

            class Program
            {
                void Test()
                {
                    bool flowControl = NewMethod();
                    if (!flowControl)
                    {
                        return;
                    }
                }

                private static bool NewMethod()
                {
                    bool b = true;
                    if (b)
                    {
                        return false;
                    }

                    Action d = () =>
                    {
                        if (b)
                        {
                            return;
                        }
                        Console.WriteLine(1);
                    };
                    return true;
                }
            }
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540154")]
    public Task Bug6313_4()
        => TestExtractMethodAsync("""
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
            """, """
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
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540154")]
    public Task Bug6313_5()
        => TestExtractMethodAsync("""
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
            """, """
            using System;

            class Program
            {
                void Test()
                {
                    Action d = () =>
                    {
                        bool flowControl = NewMethod();
                        if (!flowControl)
                        {
                            return;
                        }
                    };
                }

                private static bool NewMethod()
                {
                    int i = 1;
                    if (i > 10)
                    {
                        return false;
                    }
                    Console.WriteLine(1);
                    return true;
                }
            }
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540154")]
    public Task Bug6313_6()
        => TestExtractMethodAsync("""
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
            """, """
            using System;

            class Program
            {
                void Test()
                {
                    Action d = () =>
                    {
                        bool flowControl = NewMethod();
                        if (!flowControl)
                        {
                            return;
                        }
                        Console.WriteLine(1);
                    };
                }

                private static bool NewMethod()
                {
                    int i = 1;
                    if (i > 10)
                    {
                        return false;
                    }

                    return true;
                }
            }
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540170")]
    public Task Bug6333()
        => TestExtractMethodAsync("""
            using System;

            class Program
            {
                void Test()
                {
                    Program p;
                    [|p = new Program()|];
                }
            }
            """, """
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
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540216")]
    public Task Bug6393()
        => TestExtractMethodAsync("""
            using System;

            class Program
            {
                object Test<T>()
                {
                    T abcd; [|abcd = new T()|];
                    return abcd;
                }
            }
            """, """
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
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540184")]
    public Task Bug6351()
        => TestExtractMethodAsync("""
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
            """, """
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
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540184")]
    public Task Bug6351_1()
        => TestExtractMethodAsync("""
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
            """, """
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
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540184")]
    public Task Bug6351_2()
        => TestExtractMethodAsync("""
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
            """, """
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
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540333")]
    public Task Bug6560()
        => TestExtractMethodAsync("""
            using System;
            class Program
            {
                static void Main(string[] args)
                {
                    string S = [|null|];
                    int Y = S.Length;
                }
            }
            """, """
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
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540335")]
    public Task Bug6562()
        => TestExtractMethodAsync("""
            using System;
            class Program
            {
                int y = [|10|];
            }
            """, """
            using System;
            class Program
            {
                int y = GetY();

                private static int GetY()
                {
                    return 10;
                }
            }
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540335")]
    public Task Bug6562_1()
        => ExpectExtractMethodToFailAsync("""
            using System;
            class Program
            {
                const int i = [|10|];
            }
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540335")]
    public Task Bug6562_2()
        => TestExtractMethodAsync("""
            using System;
            class Program
            {
                Func<string> f = [|() => "test"|];
            }
            """, """
            using System;
            class Program
            {
                Func<string> f = GetF();

                private static Func<string> GetF()
                {
                    return () => "test";
                }
            }
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540335")]
    public Task Bug6562_3()
        => TestExtractMethodAsync("""
            using System;
            class Program
            {
                Func<string> f = () => [|"test"|];
            }
            """, """
            using System;
            class Program
            {
                Func<string> f = () => NewMethod();

                private static string NewMethod()
                {
                    return "test";
                }
            }
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540361")]
    public Task Bug6598()
        => ExpectExtractMethodToFailAsync("""
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
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540372")]
    public Task Bug6613()
        => TestExtractMethodAsync("""
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
            """, """
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
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540396")]
    public Task InvalidSelection_MethodBody()
        => ExpectExtractMethodToFailAsync("""
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
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541586")]
    public Task StructThis()
        => TestExtractMethodAsync("""
            struct S
            {
                void Goo()
                {
                    [|this = new S();|]
                }
            }
            """, """
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
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541627")]
    public Task DoNotUseConvertedTypeForImplicitNumericConversion()
        => TestExtractMethodAsync("""
            class T
            {
                void Goo()
                {
                    int x1 = 5;
                    long x2 = [|x1|];
                }
            }
            """, """
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
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541668")]
    public Task BreakInSelection()
        => TestExtractMethodAsync(
            """
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
            """,
            """
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
                            i1 = NewMethod(ref x1);
                            break;
                    }
                }

                private static int NewMethod(ref string x1)
                {
                    int i1;
                    switch (x1)
                    {
                        default:
                            switch (x1)
                            {
                                default:
                                    int j1 = 99;
                                    i1 = 45;
                                    x1 = "t";
                                    string j2 = i1.ToString() + j1.ToString() + x1;
                                    break;
                            }
                            break;
                    }
                    return i1;
                }
            }
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541671")]
    public Task UnreachableCodeWithReturnStatement()
        => TestExtractMethodAsync("""
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
            """, """
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
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539862")]
    public Task DoNotBlindlyPutCapturedVariable1()
        => TestExtractMethodAsync("""
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
            """, """
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
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539862")]
    public Task DoNotBlindlyPutCapturedVariable2()
        => TestExtractMethodAsync("""
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
            """, """
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
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541889")]
    public Task DoNotCrashOnRangeVariableSymbol()
        => ExpectExtractMethodToFailAsync("""
            class Test
            {
                public void Linq1()
                {
                    int[] numbers = { 5, 4, 1, 3, 9, 8, 6, 7, 2, 0 };
                    var lowNums = [|from|] n in numbers where n < 5 select n;
                }
            }
            """);

    [Fact]
    public Task ExtractRangeVariable()
        => TestExtractMethodAsync("""
            using System.Linq;
            class Test
            {
                public void Linq1()
                {
                    string[] array = null;
                    var q = from string s in array select [|s|];
                }
            }
            """, """
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
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542155")]
    public Task GenericWithErrorType()
        => TestExtractMethodAsync("""
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
            """, """
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
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542105")]
    public Task NamedArgument()
        => TestExtractMethodAsync("""
            using System;

            class C
            {
                int this[int x = 5, int y = 7] { get { return 0; } set { } }

                void Goo()
                {
                    var y = this[[|y|]: 1];
                }
            }
            """, """
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
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542213")]
    public Task QueryExpressionVariable()
        => TestExtractMethodAsync("""
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
            """, """
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
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542465")]
    public Task IsExpression()
        => TestExtractMethodAsync("""
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
            """, """
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
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542526")]
    public Task TypeParametersInConstraint()
        => TestExtractMethodAsync("""
            using System;
            using System.Collections.Generic;

            class A
            {
                static void Goo<T, S>(T x) where T : IList<S>
                {
                    var y = [|x.Count|];
                }
            }
            """, """
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
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542619")]
    public Task GlobalNamespaceInReturnType()
        => TestExtractMethodAsync("""
            class Program
            {
                class System
                {
                    class Action { }
                }
                static global::System.Action a = () => { global::System.Console.WriteLine(); [|}|];
            }
            """, """
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
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542582")]
    public Task ExtractMethodExpandSelectionOnFor()
        => TestExtractMethodAsync("""
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
            """, """
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
            """);

    [Fact]
    public Task ExtractMethodNotContainerOnFor()
        => TestExtractMethodAsync("""
            using System;

            class Program
            {
                static void Main(string[] args)
                {
                    for (int i = 0; i < [|10|]; i++) ;
                }
            }
            """, """
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
            """);

    [Fact]
    public Task ExtractMethodExpandSelectionOnForeach()
        => TestExtractMethodAsync("""
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
            """, """
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
            """);

    [Fact]
    public Task ExtractMethodNotContainerOnForeach()
        => TestExtractMethodAsync("""
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
            """, """
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
            """);

    [Fact]
    public Task ExtractMethodNotContainerOnElseClause()
        => TestExtractMethodAsync("""
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
            """, """
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
            """);

    [Fact]
    public Task ExtractMethodExpandSelectionOnLabel()
        => TestExtractMethodAsync("""
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
            """, """
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
            """);

    [Fact]
    public Task ExtractMethodNotContainerOnLabel()
        => TestExtractMethodAsync("""
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
            """, """
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
            """);

    [Fact]
    public Task ExtractMethodExpandSelectionOnSwitch()
        => TestExtractMethodAsync("""
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
            """, """
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
            """);

    [Fact]
    public Task ExtractMethodNotContainerOnSwitch()
        => TestExtractMethodAsync("""
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
            """, """
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
            """);

    [Fact]
    public Task ExtractMethodExpandSelectionOnDo()
        => TestExtractMethodAsync("""
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
            """, """
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
            """);

    [Fact]
    public Task ExtractMethodNotContainerOnDo()
        => TestExtractMethodAsync("""
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
            """, """
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
            """);

    [Fact]
    public Task ExtractMethodExpandSelectionOnWhile()
        => TestExtractMethodAsync("""
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
            """, """
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
            """);

    [Fact]
    public Task ExtractMethodExpandSelectionOnStruct()
        => TestExtractMethodAsync("""
            using System;

            struct Goo
            {
                static Action a = () => { Console.WriteLine(); [|}|];
            }
            """, """
            using System;

            struct Goo
            {
                static Action a = GetA();

                private static Action GetA()
                {
                    return () => { Console.WriteLine(); };
                }
            }
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542619")]
    public Task ExtractMethodIncludeGlobal()
        => TestExtractMethodAsync("""
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
            """, """
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
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542582")]
    public Task ExtractMethodExpandSelection()
        => TestExtractMethodAsync("""
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
            """, """
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
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542594")]
    public Task ExtractMethodRename1()
        => TestExtractMethodAsync("""
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
            """, """
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
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542594")]
    public Task ExtractMethodRename2()
        => TestExtractMethodAsync("""
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
            """, """
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
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542632")]
    public Task ExtractMethodInInteractive1()
        => TestExtractMethodAsync(@"int i; [|i = 2|]; i = 3;", """
            int i; i = NewMethod();

            static int NewMethod()
            {
                return 2;
            }

            i = 3;
            """, parseOptions: Options.Script);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542670")]
    public Task TypeParametersInConstraint1()
        => TestExtractMethodAsync("""
            using System;
            using System.Collections.Generic;

            class A
            {
                static void Goo<T, S, U>(T x) where T : IList<S> where S : IList<U>
                {
                    var y = [|x.Count|];
                }
            }
            """, """
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
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/706894")]
    [WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543012")]
    public Task TypeParametersInConstraint2()
        => TestExtractMethodAsync("""
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
            """, """
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
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/706894")]
    [WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543012")]
    public Task TypeParametersInConstraint3()
        => TestExtractMethodAsync("""
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
            """, """
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
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543012")]
    public Task TypeParametersInConstraint4()
        => TestExtractMethodAsync("""
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
            """, """
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
            """);

    [Fact]
    public Task NullabilityTypeParameters()
        => TestExtractMethodAsync("""
            #nullable enable

            using System.Collections.Generic;

            public class Test
            {
                public int M(Dictionary<string, string?> v)
                {
                    [|return v.Count;|]
                }
            }
            """, """
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
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543012")]
    public Task TypeParametersInConstraintBestEffort()
        => TestExtractMethodAsync("""
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
            """, """
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
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542672")]
    public Task ConstructedTypes()
        => TestExtractMethodAsync("""
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
            """, """
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
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542792")]
    public Task TypeInDefault()
        => TestExtractMethodAsync("""
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
            """, """
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
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542708")]
    public Task Script_ArgumentException()
        => TestExtractMethodAsync("""
            using System;
            public static void GetNonVirtualMethod<TDelegate>( Type type, string name)
            {
                Type delegateType = typeof(TDelegate);
                 var invoke = [|delegateType|].GetMethod("Invoke");
            }
            """, """
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
            """, parseOptions: Options.Script);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/529008")]
    public Task ReadOutSideIsUnReachable()
        => TestExtractMethodAsync("""
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
            """, """
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
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543186")]
    public Task AnonymousTypePropertyName()
        => TestExtractMethodAsync("""
            class C
            {
                void M()
                {
                    var x = new { [|String|] = true };
                }
            }
            """, """
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
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543662")]
    public Task ArgumentOfBaseConstrInit()
        => TestExtractMethodAsync("""
            class O
            {
                public O(int t) : base([|t|])
                {
                }
            }
            """, """
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
            """);

    [Fact]
    public Task UnsafeType()
        => TestExtractMethodAsync("""
            unsafe class O
            {
                unsafe public O(int t)
                {
                    [|t = 1;|]
                }
            }
            """, """
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
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544144")]
    public Task CastExpressionWithImplicitUserDefinedConversion()
        => TestExtractMethodAsync("""
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
            """, """
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
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544387")]
    public Task FixedPointerVariable()
        => TestExtractMethodAsync("""
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
            """, """
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
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544444")]
    public Task PointerDeclarationStatement()
        => TestExtractMethodAsync("""
            class Program
            {
                unsafe static void Main()
                {
                    int* p1 = null;
                    [|int* p2 = p1;|]
                }
            }
            """, """
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
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544446")]
    public Task PrecededByCastExpr()
        => TestExtractMethodAsync("""
            class Program
            {
                static void Main()
                {
                    int i1 = (int)[|5L|];
                }
            }
            """, """
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
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542944")]
    public Task ExpressionWithLocalConst()
        => TestExtractMethodAsync("""
            class Program
            {
                static void Main(string[] args)
                {
                    const string a = null;
                    [|a = null;|]
                }
            }
            """, """
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
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542944")]
    public Task ExpressionWithLocalConst2()
        => TestExtractMethodAsync("""
            class Program
            {
                static void Main(string[] args)
                {
                    const string a = null;
                    [|a = null;|]
                }
            }
            """, """
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
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544675")]
    public Task HiddenPosition()
        => ExpectExtractMethodToFailAsync("""
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
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530609")]
    public Task NoCrashInteractive()
        => TestExtractMethodAsync("""
            [|if (true)
            {
            }|]
            """, """
            NewMethod();

            static void NewMethod()
            {
                if (true)
                {
                }
            }
            """, parseOptions: new CSharpParseOptions(kind: SourceCodeKind.Script));

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530322")]
    public Task ExtractMethodShouldNotBreakFormatting()
        => TestExtractMethodAsync("""
            class C
            {
                void M(int i, int j, int k)
                {
                    M(0,
                      [|1|],
                      2);
                }
            }
            """, """
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
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/604389")]
    public Task TestExtractLiteralExpression()
        => TestExtractMethodAsync("""
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
            """, """
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
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/604389")]
    public Task TestExtractCollectionInitializer()
        => TestExtractMethodAsync("""
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
            """, """
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
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/854662")]
    public Task TestExtractCollectionInitializer2()
        => TestExtractMethodAsync("""
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
            """, """
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
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530267")]
    public Task TestCoClassImplicitConversion()
        => TestExtractMethodAsync("""
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
            """, """
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
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530710")]
    public Task TestOverloadResolution()
        => TestExtractMethodAsync("""
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
            """, """
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
                    Outer(null, y => Inner(x => { GetX(x).Ex(); }, y)); // Prints 1
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
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530710")]
    public Task TestOverloadResolution1()
        => TestExtractMethodAsync("""
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
            """, """
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
                    Outer(null, y => Inner(x => { NewMethod(x); }, y)); // Prints 1
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
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530710")]
    public Task TestOverloadResolution2()
        => TestExtractMethodAsync("""
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
            """, """
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
                    Outer(null, y => Inner(x => { NewMethod(x); }, y)); // Prints 1
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
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/731924")]
    public Task TestTreatEnumSpecial()
        => TestExtractMethodAsync("""
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
            """, """
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
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/756222")]
    public Task TestReturnStatementInAsyncMethod()
        => TestExtractMethodAsync("""
            using System.Threading.Tasks;

            class C
            {
                async Task<int> M()
                {
                    await Task.Yield();
                    [|return 3;|]
                }
            }
            """, """
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
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/574576")]
    public Task TestAsyncMethodWithRefOrOutParameters()
        => TestExtractMethodAsync(
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
            """,

            """
            using System.Threading.Tasks;

            class C
            {
                public async void Goo()
                {
                    (int q, int p) = await NewMethod();
                    var r = q;
                    var s = p;
                }

                private static async Task<(int q, int p)> NewMethod()
                {
                    var q = 1;
                    var p = 2;
                    await Task.Yield();
                    return (q, p);
                }
            }
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/574576")]
    public Task TestAsyncLocalFunctionWithRefOrOutParameters()
        => TestExtractMethodAsync(
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
            """,

            """
            using System.Threading.Tasks;

            class C
            {
                public async void Goo()
                {
                    (int q, int p) = await NewMethod();
                    var r = q;
                    var s = p;

                    static async Task<(int q, int p)> NewMethod()
                    {
                        var q = 1;
                        var p = 2;
                        await Task.Yield();
                        return (q, p);
                    }
                }
            }
            """, localFunction: true);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1025272")]
    public Task TestAsyncMethodWithWellKnownValueType1()
        => TestExtractMethodAsync(
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
            """, """
            using System;
            using System.Threading;
            using System.Threading.Tasks;

            class Program
            {
                public async Task Hello()
                {
                    var cancellationToken = CancellationToken.None;

                    (int i, cancellationToken) = await NewMethod(cancellationToken);

                    cancellationToken.ThrowIfCancellationRequested();
                    Console.WriteLine(i);
                }

                private static async Task<(int i, CancellationToken cancellationToken)> NewMethod(CancellationToken cancellationToken)
                {
                    var i = await Task.Run(() =>
                    {
                        Console.WriteLine();
                        cancellationToken.ThrowIfCancellationRequested();

                        return 1;
                    }, cancellationToken);
                    return (i, cancellationToken);
                }
            }
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1025272")]
    public Task TestAsyncMethodWithWellKnownValueType2()
        => TestExtractMethodAsync(
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
                        cancellationToken = default;

                        return 1;
                    }, cancellationToken);|]

                    cancellationToken.ThrowIfCancellationRequested();
                    Console.WriteLine(i);
                }
            }
            """, """
            using System;
            using System.Threading;
            using System.Threading.Tasks;

            class Program
            {
                public async Task Hello()
                {
                    var cancellationToken = CancellationToken.None;

                    (int i, cancellationToken) = await NewMethod(cancellationToken);

                    cancellationToken.ThrowIfCancellationRequested();
                    Console.WriteLine(i);
                }

                private static async Task<(int i, CancellationToken cancellationToken)> NewMethod(CancellationToken cancellationToken)
                {
                    var i = await Task.Run(() =>
                    {
                        Console.WriteLine();
                        cancellationToken.ThrowIfCancellationRequested();
                        cancellationToken = default;

                        return 1;
                    }, cancellationToken);
                    return (i, cancellationToken);
                }
            }
            """);

    [Fact]
    public Task TestDoNotPutOutOrRefForStructOn()
        => TestExtractMethodAsync("""
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
            """, """
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
            """);

    [Theory]
    [InlineData("add", "remove")]
    [InlineData("remove", "add")]
    [WorkItem("https://github.com/dotnet/roslyn/issues/17474")]
    public Task TestExtractMethodEventAccessorUnresolvedName(string testedAccessor, string untestedAccessor)
        => TestExtractMethodAsync($$"""
            namespace ClassLibrary9
            {
                public class Class
                {
                    public event EventHandler Event
                    {
                        {{testedAccessor}} { [|throw new NotImplementedException();|] }
                        {{untestedAccessor}} { throw new NotImplementedException(); }
                    }
                }
            }
            """, $$"""
            namespace ClassLibrary9
            {
                public class Class
                {
                    public event EventHandler Event
                    {
                        {{testedAccessor}} { NewMethod(); }
                        {{untestedAccessor}} { throw new NotImplementedException(); }
                    }

                    private static void NewMethod()
                    {
                        throw new NotImplementedException();
                    }
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/19958")]
    public Task TestExtractMethodRefPassThrough()
        => TestExtractMethodAsync("""
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
            """, """
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
            """, temporaryFailing: true);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/19958")]
    public Task TestExtractMethodRefPassThroughDuplicateVariable()
        => TestExtractMethodAsync("""
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
            """, """
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
            """, temporaryFailing: true);

    [Fact]
    public async Task ExtractMethod_Argument1()
    {
        var service = new CSharpExtractMethodService();
        Assert.NotNull(await Record.ExceptionAsync(async () =>
        {
            var tree = await service.ExtractMethodAsync(document: null!, textSpan: default, localFunction: false, options: default, CancellationToken.None);
        }));
    }

    [Fact]
    public async Task ExtractMethod_Argument2()
    {
        var solution = new AdhocWorkspace().CurrentSolution;
        var projectId = ProjectId.CreateNewId();
        var project = solution
            .AddProject(projectId, "Project", "Project.dll", LanguageNames.CSharp)
            .GetRequiredProject(projectId);

        var document = project.AddMetadataReference(NetFramework.mscorlib)
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
            composition: EditorTestCompositions.EditorFeatures);
        // Force initialization.
        workspace.GetOpenDocumentIds().Select(id => workspace.GetTestDocument(id)!.GetTextView()).ToList();

        var textView = workspace.Documents.Single().GetTextView();

        var handler = workspace.ExportProvider.GetCommandHandler<ExtractMethodCommandHandler>(PredefinedCommandHandlerNames.ExtractMethod, ContentTypeNames.CSharpContentType);

        var state = handler.GetCommandState(new ExtractMethodCommandArgs(textView, textView.TextBuffer));
        Assert.True(state.IsUnspecified);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/18347")]
    public Task ExtractMethodUnreferencedLocalFunction1()
        => TestExtractMethodAsync("""
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
            """, """
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
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/18347")]
    public Task ExtractMethodUnreferencedLocalFunction2()
        => TestExtractMethodAsync("""
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
            """, """
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
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/18347")]
    public Task ExtractMethodUnreferencedLocalFunction3()
        => TestExtractMethodAsync("""
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
            """, """
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
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/18347")]
    public Task ExtractMethodUnreferencedLocalFunction4()
        => TestExtractMethodAsync("""
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
            """, """
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
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/18347")]
    public Task ExtractMethodUnreferencedLocalFunction5()
        => TestExtractMethodAsync("""
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
            """, """
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
            """);

    [Theory]
    [InlineData("LocalCapture();")]
    [InlineData("System.Func<int> function = LocalCapture;")]
    [InlineData("System.Func<int> function = () => LocalCapture();")]
    [WorkItem("https://github.com/dotnet/roslyn/issues/18347")]
    public Task ExtractMethodFlowsToLocalFunction1(string usageSyntax)
        => TestExtractMethodAsync($$"""
            namespace ExtractMethodCrashRepro
            {
                public static class SomeClass
                {
                    private static void Repro( int arg )
                    {
                        [|arg = arg + 3;|]

                        {{usageSyntax}}

                        int LocalCapture() => arg;
                    }
                }
            }
            """, $$"""
            namespace ExtractMethodCrashRepro
            {
                public static class SomeClass
                {
                    private static void Repro( int arg )
                    {
                        arg = NewMethod(arg);

                        {{usageSyntax}}

                        int LocalCapture() => arg;
                    }

                    private static int NewMethod(int arg)
                    {
                        arg = arg + 3;
                        return arg;
                    }
                }
            }
            """);

    [Theory]
    [InlineData("LocalCapture();")]
    [InlineData("System.Func<int> function = LocalCapture;")]
    [InlineData("System.Func<int> function = () => LocalCapture();")]
    [WorkItem("https://github.com/dotnet/roslyn/issues/18347")]
    public Task ExtractMethodFlowsToLocalFunction2(string usageSyntax)
        => TestExtractMethodAsync($$"""
            namespace ExtractMethodCrashRepro
            {
                public static class SomeClass
                {
                    private static void Repro( int arg )
                    {
                        int LocalCapture() => arg;

                        [|arg = arg + 3;|]

                        {{usageSyntax}}
                    }
                }
            }
            """, $$"""
            namespace ExtractMethodCrashRepro
            {
                public static class SomeClass
                {
                    private static void Repro( int arg )
                    {
                        int LocalCapture() => arg;

                        arg = NewMethod(arg);

                        {{usageSyntax}}
                    }

                    private static int NewMethod(int arg)
                    {
                        arg = arg + 3;
                        return arg;
                    }
                }
            }
            """);

    /// <summary>
    /// This test verifies that Extract Method works properly when the region to extract references a local
    /// function, the local function uses an unassigned but wholly local variable.
    /// </summary>
    [Theory]
    [InlineData("LocalCapture();")]
    [InlineData("System.Func<int> function = LocalCapture;")]
    [InlineData("System.Func<int> function = () => LocalCapture();")]
    [WorkItem("https://github.com/dotnet/roslyn/issues/18347")]
    public Task ExtractMethodFlowsToLocalFunctionWithUnassignedLocal(string usageSyntax)
        => TestExtractMethodAsync($$"""
            namespace ExtractMethodCrashRepro
            {
                public static class SomeClass
                {
                    private static void Repro( int arg )
                    {
                        int local;
                        int LocalCapture() => arg + local;

                        [|arg = arg + 3;|]

                        {{usageSyntax}}
                    }
                }
            }
            """, $$"""
            namespace ExtractMethodCrashRepro
            {
                public static class SomeClass
                {
                    private static void Repro( int arg )
                    {
                        int local;
                        int LocalCapture() => arg + local;

                        arg = NewMethod(arg);

                        {{usageSyntax}}
                    }

                    private static int NewMethod(int arg)
                    {
                        arg = arg + 3;
                        return arg;
                    }
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/18347")]
    public Task ExtractMethodDoesNotFlowToLocalFunction1()
        => TestExtractMethodAsync("""
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
            """, """
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
            """);

    [Fact]
    public Task TestUnreachableCodeModifiedInside()
        => TestExtractMethodAsync("""
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
            """, """
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
            """);

    [Fact]
    public Task TestUnreachableCodeModifiedOutside()
        => TestExtractMethodAsync("""
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
            """, """
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
            """);

    [Fact]
    public Task TestUnreachableCodeModifiedBoth()
        => TestExtractMethodAsync("""
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
            """, """
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
            """);

    [Fact]
    public Task TestLocalFunctionParameters()
        => TestExtractMethodAsync("""
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
            """, """
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
            """);

    [Fact]
    public Task TestDataFlowInButNoReadInside()
        => TestExtractMethodAsync("""
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
            """, """
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
                        bool flowControl = NewMethod(ref test, a);
                        if (!flowControl)
                        {
                            return;
                        }
                    }

                    private static bool NewMethod(ref object test, bool a)
                    {
                        if (a)
                        {
                            return false;
                        }

                        if (A == a)
                        {
                            test = new object();
                        }

                        return true;
                    }
                }
            }
            """);

    [Fact]
    public Task AllowBestEffortForUnknownVariableDataFlow()
        => TestExtractMethodAsync("""
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
            """, """
            class Program
            {
                void Method(out object test)
                {
                    test = null;
                    var a = test != null;
                    bool flowControl = NewMethod(ref test, a);
                    if (!flowControl)
                    {
                        return;
                    }
                }

                private static bool NewMethod(ref object test, bool a)
                {
                    if (a)
                    {
                        return false;
                    }
                    if (A == a)
                    {
                        test = new object();
                    }

                    return true;
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/30750")]
    public Task ExtractMethodInInterface()
        => TestExtractMethodAsync("""
            interface Program
            {
                void Goo();

                void Test()
                {
                    [|Goo();|]
                }
            }
            """, """
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
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/33242")]
    public Task ExtractMethodInExpressionBodiedConstructors()
        => TestExtractMethodAsync("""
            class Goo
            {
                private readonly string _bar;

                private Goo(string bar) => _bar = [|bar|];
            }
            """, """
            class Goo
            {
                private readonly string _bar;

                private Goo(string bar) => _bar = GetBar(bar);

                private static string GetBar(string bar)
                {
                    return bar;
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/33242")]
    public Task ExtractMethodInExpressionBodiedFinalizers()
        => TestExtractMethodAsync("""
            class Goo
            {
                bool finalized;

                ~Goo() => finalized = [|true|];
            }
            """, """
            class Goo
            {
                bool finalized;

                ~Goo() => finalized = NewMethod();

                private static bool NewMethod()
                {
                    return true;
                }
            }
            """);

    [Fact]
    public Task ExtractMethodInvolvingFunctionPointer()
        => TestExtractMethodAsync("""
            class C
            {
                void M(delegate*<delegate*<ref string, ref readonly int>> ptr1)
                {
                    string s = null;
                    _ = [|ptr1()|](ref s);
                }
            }
            """, """
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
            """);

    [Fact]
    public Task ExtractMethodInvolvingFunctionPointerWithTypeParameter()
        => TestExtractMethodAsync("""
            class C
            {
                void M<T1, T2>(delegate*<T1, T2> ptr1)
                {
                    _ = [|ptr1|]();
                }
            }
            """, """
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
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/44260")]
    public Task TopLevelStatement_ValueInAssignment()
        => TestExtractMethodAsync("""
            bool local;
            local = [|true|];
            """, """
            bool local;
            local = NewMethod();

            static bool NewMethod()
            {
                return true;
            }
            """, localFunction: true);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/44260")]
    public Task TopLevelStatement_ArgumentInInvocation()
        => TestExtractMethodAsync("""
            System.Console.WriteLine([|"string"|]);
            """, """
            System.Console.WriteLine(NewMethod());

            static string NewMethod()
            {
                return "string";
            }
            """, localFunction: true);

    [Theory]
    [InlineData("unsafe")]
    [InlineData("checked")]
    [InlineData("unchecked")]
    [WorkItem("https://github.com/dotnet/roslyn/issues/4950")]
    public Task ExtractMethodInvolvingUnsafeBlock(string keyword)
        => TestExtractMethodAsync($$"""
            using System;

            class Program {
                static void Main(string[] args)
                {
                    object value = args;

                    [|
                    IntPtr p;
                    {{keyword}}
                    {
                        object t = value;
                        p = IntPtr.Zero;
                    }
                    |]

                    Console.WriteLine(p);
                }
            }
            """, $$"""
            using System;

            class Program {
                static void Main(string[] args)
                {
                    object value = args;

                    IntPtr p = NewMethod(value);

                    Console.WriteLine(p);
                }

                private static IntPtr NewMethod(object value)
                {
                    IntPtr p;
                    {{keyword}}
                    {
                        object t = value;
                        p = IntPtr.Zero;
                    }

                    return p;
                }
            }
            """);

    [Fact]
    public Task ExtractRawStringLiteral_SingleLine()
        => TestExtractMethodAsync(""""
            class C
            {
                void M(int y)
                {
                    var s = [|"""Hello world"""|];
                }
            }
            """", """"
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
            """");

    [Fact]
    public Task ExtractRawStringLiteralInterpolation_SingleLine()
        => TestExtractMethodAsync(""""
            class C
            {
                void M(int y)
                {
                    var s = [|$"""{y}"""|];
                }
            }
            """", """"
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
            """");

    [Fact]
    public Task ExtractRawStringLiteralInterpolationHole_SingleLine()
        => TestExtractMethodAsync(""""
            class C
            {
                void M(int y)
                {
                    var s = $"""{[|y|]}""";
                }
            }
            """", """"
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
            """");

    [Fact]
    public Task ExtractRawStringLiteral_MultiLine()
        => TestExtractMethodAsync(""""
            class C
            {
                void M(int y)
                {
                    var s = [|"""
                        Hello world
                        """|];
                }
            }
            """", """"
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
            """");

    [Fact]
    public Task ExtractRawStringLiteralInterpolation_MultiLine()
        => TestExtractMethodAsync(""""
            class C
            {
                void M(int y)
                {
                    var s = $[|"""
                        {y}
                        """|];
                }
            }
            """", """"
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
            """");

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/73044")]
    public Task CapturedPrimaryConstructorParameter()
        => TestExtractMethodAsync(""""
            public class Test(int value)
            {
                public int M()
                {
                    return [|value + 1|];
                }
            }
            """", """"
            public class Test(int value)
            {
                public int M()
                {
                    return NewMethod();
                }

                private int NewMethod()
                {
                    return value + 1;
                }
            }
            """");

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/39329")]
    public Task ExtractUsingLocalDeclaration1()
        => TestExtractMethodAsync(""""
            using System;

            public class Goo : IDisposable
            {
                void M2() { }

                void M()
                {
                    [|using var g = new Goo();
                    g.M2();|]
                }

                public void Dispose()
                {
                }
            }
            """", """"
            using System;
            
            public class Goo : IDisposable
            {
                void M2() { }
            
                void M()
                {
                    NewMethod();
                }

                private static void NewMethod()
                {
                    using var g = new Goo();
                    g.M2();
                }
            
                public void Dispose()
                {
                }
            }
            """");

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/39329")]
    public Task ExtractUsingLocalDeclaration2()
        => TestExtractMethodAsync(""""
            using System;

            public class Goo : IDisposable
            {
                void M2() { }

                void M()
                {
                    [|using var g = new Goo();
                    g.M2();|]
                    g.M2();
                }

                public void Dispose()
                {
                }
            }
            """", """"
            using System;
            
            public class Goo : IDisposable
            {
                void M2() { }
            
                void M()
                {
                    using Goo g = NewMethod();
                    g.M2();
                }

                private static Goo NewMethod()
                {
                    var g = new Goo();
                    g.M2();
                    return g;
                }
            
                public void Dispose()
                {
                }
            }
            """");

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/39329")]
    public Task ExtractUsingLocalDeclaration3()
        => TestExtractMethodAsync(""""
            using System;

            public class Goo
            {
                void M()
                {
                    [|using var x1 = new System.IO.MemoryStream();
                    using var x2 = new System.IO.MemoryStream();
                    using var x3 = new System.IO.MemoryStream();|]
                }
            }
            """", """"
            using System;
            
            public class Goo
            {
                void M()
                {
                    NewMethod();
                }

                private static void NewMethod()
                {
                    using var x1 = new System.IO.MemoryStream();
                    using var x2 = new System.IO.MemoryStream();
                    using var x3 = new System.IO.MemoryStream();
                }
            }
            """");

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/39329")]
    public Task ExtractUsingLocalDeclaration4()
        => TestExtractMethodAsync(""""
            using System.Collections.Generic;

            class C
            {
                bool M(IEnumerable<int> p)
                {
                    [|using var x = p.GetEnumerator();
                    return x.MoveNext();|]
                }
            }
            """", """"
            using System.Collections.Generic;

            class C
            {
                bool M(IEnumerable<int> p)
                {
                    return NewMethod(p);
                }

                private static bool NewMethod(IEnumerable<int> p)
                {
                    using var x = p.GetEnumerator();
                    return x.MoveNext();
                }
            }
            """");

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/18656")]
    public Task TestSelectionMidwayThroughTwoStatements()
        => TestExtractMethodAsync(""""
            class C
            {
                static void Main(string[] args)
                {
                    var isAdditive = true;
                    var isSoftSelection = true;
                    var indexOfLastSelected = 0;
                    var index = 0;
                    var item = new Item();

                    if (isAddi[|tive)
                    {
                        if (!isSoftSelection)
                        {
                            item.IsSelected = !item.IsSelected;
                        }
                    }
                    else
                    {
                        item.IsSelected = true;
                    }
                    indexOfLast|]Selected = index;
                }
            }

            class Item
            {
                public bool IsSelected { get; set; }
            }
            """", """"
            class C
            {
                static void Main(string[] args)
                {
                    var isAdditive = true;
                    var isSoftSelection = true;
                    var indexOfLastSelected = 0;
                    var index = 0;
                    var item = new Item();
                    indexOfLastSelected = NewMethod(isAdditive, isSoftSelection, index, item);
                }

                private static int NewMethod(bool isAdditive, bool isSoftSelection, int index, Item item)
                {
                    int indexOfLastSelected;
                    if (isAdditive)
                    {
                        if (!isSoftSelection)
                        {
                            item.IsSelected = !item.IsSelected;
                        }
                    }
                    else
                    {
                        item.IsSelected = true;
                    }
                    indexOfLastSelected = index;
                    return indexOfLastSelected;
                }
            }

            class Item
            {
                public bool IsSelected { get; set; }
            }
            """");

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/70024")]
    public Task TestAliasedType()
        => TestExtractMethodAsync(""""
            using System;
            using Spec = System.Collections.Specialized;

            namespace ClassLibrary3
            {
                public class T
                {
                    public void Method()
                    {
                        var value = new Spec.ListDictionary();

                        [|Console.WriteLine(value);|]
                    }
                }
            }
            """", """"
            using System;
            using Spec = System.Collections.Specialized;
            
            namespace ClassLibrary3
            {
                public class T
                {
                    public void Method()
                    {
                        var value = new Spec.ListDictionary();
            
                        NewMethod(value);
                    }

                    private static void NewMethod(Spec.ListDictionary value)
                    {
                        Console.WriteLine(value);
                    }
                }
            }
            """");
}
