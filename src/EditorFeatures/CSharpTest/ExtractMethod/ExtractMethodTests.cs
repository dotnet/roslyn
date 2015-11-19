// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.ExtractMethod;
using Microsoft.CodeAnalysis.Editor.CSharp.ExtractMethod;
using Microsoft.CodeAnalysis.Editor.Implementation.Interactive;
using Microsoft.CodeAnalysis.Editor.UnitTests;
using Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;
using Microsoft.CodeAnalysis.ExtractMethod;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Text.Operations;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.ExtractMethod
{
    public partial class ExtractMethodTests : ExtractMethodBase
    {
        [WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)]
        public void ExtractMethod1()
        {
            var code = @"using System;
using System.Collections.Generic;
using System.Linq;

class Program
{
    void Test(string[] args)
    {
        [|int i;
        i = 10;|]
    }
}";
            var expected = @"using System;
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
}";

            TestExtractMethod(code, expected);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)]
        public void ExtractMethod2()
        {
            var code = @"using System;
using System.Collections.Generic;
using System.Linq;

class Program
{
    void Test(string[] args)
    {
        [|int i = 10;
        int i2 = 10;|]
    }
}";
            var expected = @"using System;
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
}";

            TestExtractMethod(code, expected);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)]
        public void ExtractMethod3()
        {
            var code = @"using System;
using System.Collections.Generic;
using System.Linq;

class Program
{
    void Test(string[] args)
    {
        int i = 10;
        [|int i2 = i;|]
    }
}";
            var expected = @"using System;
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
}";

            TestExtractMethod(code, expected);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)]
        public void ExtractMethod4()
        {
            var code = @"using System;
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
}";
            var expected = @"using System;
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
}";

            // compoundaction not supported yet.
            TestExtractMethod(code, expected, temporaryFailing: true);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)]
        public void ExtractMethod5()
        {
            var code = @"using System;
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
}";
            var expected = @"using System;
using System.Collections.Generic;
using System.Linq;

class Program
{
    void Test(string[] args)
    {
        int i = 10;
        int i2 = i;

        NewMethod(i);
    }

    private static void NewMethod(int i)
    {
        int i2 = i;
    }
}";

            TestExtractMethod(code, expected);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)]
        public void ExtractMethod6()
        {
            var code = @"using System;
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
}";
            var expected = @"using System;
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
}";

            TestExtractMethod(code, expected);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)]
        public void ExtractMethod7()
        {
            var code = @"using System;
using System.Collections.Generic;
using System.Linq;

class Program
{
    void Test(string[] args)
    {
        string [] a = null;

        [|Test(a);|]
    }
}";
            var expected = @"using System;
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
}";

            TestExtractMethod(code, expected);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)]
        public void ExtractMethod8()
        {
            var code = @"using System;
using System.Collections.Generic;
using System.Linq;

class Program
{
    static void Test(string[] args)
    {
        string [] a = null;

        [|Test(a);|]
    }
}";
            var expected = @"using System;
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
}";

            TestExtractMethod(code, expected);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)]
        public void ExtractMethod9()
        {
            var code = @"using System;
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
}";
            var expected = @"using System;
using System.Collections.Generic;
using System.Linq;

class Program
{
    void Test(string[] args)
    {
        NewMethod(args);
    }

    private static void NewMethod(string[] args)
    {
        int i;
        string s;
        i = 10;
        s = args[0] + i.ToString();
    }
}";

            TestExtractMethod(code, expected);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)]
        public void ExtractMethod10()
        {
            var code = @"using System;
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
}";
            var expected = @"using System;
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
}";

            TestExtractMethod(code, expected);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)]
        public void ExtractMethod11()
        {
            var code = @"using System;
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
}";
            var expected = @"using System;
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
}";

            TestExtractMethod(code, expected);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)]
        public void ExtractMethod11_1()
        {
            var code = @"using System;
using System.Collections.Generic;
using System.Linq;

class Program
{
    void Test(string[] args)
    {
        [|int i;
        int i2 = 10;|]
    }
}";
            var expected = @"using System;
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
}";

            TestExtractMethod(code, expected);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)]
        public void ExtractMethod12()
        {
            var code = @"using System;
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
}";
            var expected = @"using System;
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
}";

            TestExtractMethod(code, expected);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)]
        public void ControlVariableInForeachStatement()
        {
            var code = @"using System;
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
}";
            var expected = @"using System;
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
}";

            TestExtractMethod(code, expected);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)]
        public void ExtractMethod14()
        {
            var code = @"using System;
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
}";
            var expected = @"using System;
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
}";

            // var in for loop doesn't get bound yet
            TestExtractMethod(code, expected, temporaryFailing: true);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)]
        public void ExtractMethod15()
        {
            var code = @"using System;
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
}";
            var expected = @"using System;
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
}";

            TestExtractMethod(code, expected);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)]
        public void ExtractMethod16()
        {
            var code = @"using System;
using System.Collections.Generic;
using System.Linq;

class Program
{
    void Test(string[] args)
    {
        [|int i = 1;|]

        System.Console.WriteLine(i);
    }
}";
            var expected = @"using System;
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
}";

            TestExtractMethod(code, expected);
        }

        [WorkItem(538932)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)]
        public void ExtractMethod17()
        {
            var code = @"using System;
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
}";
            var expected = @"using System;
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
}";

            TestExtractMethod(code, expected);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)]
        public void ExtractMethod18()
        {
            var code = @"using System;
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
}";
            var expected = @"using System;
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
}";

            TestExtractMethod(code, expected);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)]
        public void ExtractMethod19()
        {
            var code = @"using System;
using System.Collections.Generic;
using System.Linq;

unsafe class Program
{
    void Test()
    {
        [|int i = 1;|]
    }
}";
            var expected = @"using System;
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
}";

            TestExtractMethod(code, expected);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)]
        public void ExtractMethod20()
        {
            var code = @"using System;
using System.Collections.Generic;
using System.Linq;

class Program
{
    unsafe void Test()
    {
        [|int i = 1;|]
    }
}";
            var expected = @"using System;
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
}";

            TestExtractMethod(code, expected);
        }

        [WorkItem(542677)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)]
        public void ExtractMethod21()
        {
            var code = @"using System;
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
}";
            var expected = @"using System;
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
}";

            TestExtractMethod(code, expected);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)]
        public void ExtractMethod22()
        {
            var code = @"using System;
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
}";
            var expected = @"using System;
using System.Collections.Generic;
using System.Linq;

class Program
{
    void Test()
    {
        int i;

        NewMethod(i);

        i = 6;
        Console.WriteLine(i);
    }

    private static void NewMethod(int i)
    {
        int b = 10;
        if (b < 10)
        {
            i = 5;
        }
    }
}";

            TestExtractMethod(code, expected);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)]
        public void ExtractMethod23()
        {
            var code = @"using System;
using System.Collections.Generic;
using System.Linq;

class Program
{
    static void Main(string[] args)
    {
        if (true)
            [|Console.WriteLine(args[0].ToString());|]
    }
}";
            var expected = @"using System;
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
}";

            TestExtractMethod(code, expected);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)]
        public void ExtractMethod24()
        {
            var code = @"using System;

class Program
{
    static void Main(string[] args)
    {
        int y = [|int.Parse(args[0].ToString())|];
    }
}";
            var expected = @"using System;

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
}";
            TestExtractMethod(code, expected);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)]
        public void ExtractMethod25()
        {
            var code = @"using System;
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
}";
            var expected = @"using System;
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
}";

            TestExtractMethod(code, expected);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)]
        public void ExtractMethod26()
        {
            var code = @"using System;
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
}";
            var expected = @"using System;
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
}";

            TestExtractMethod(code, expected);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)]
        public void ExtractMethod27()
        {
            var code = @"using System;
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
}";
            var expected = @"using System;
using System.Collections.Generic;
using System.Linq;

class Program
{
    void Test()
    {
        int i = 1;

        NewMethod(i);

        i = 6;
        Console.WriteLine(i);
    }

    private static void NewMethod(int i)
    {
        int b = 10;
        if (b < 10)
        {
            i = 5;
        }
    }
}";

            TestExtractMethod(code, expected);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)]
        public void ExtractMethod28()
        {
            var code = @"using System;
using System.Collections.Generic;
using System.Linq;

class Program
{
    int Test()
    {
        [|return 1;|]
    }
}";
            var expected = @"using System;
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
}";

            TestExtractMethod(code, expected);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)]
        public void ExtractMethod29()
        {
            var code = @"using System;
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
}";
            var expected = @"using System;
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
}";

            TestExtractMethod(code, expected);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)]
        public void ExtractMethod30()
        {
            var code = @"using System;
using System.Collections.Generic;
using System.Linq;

class Program
{
    void Test(out int i)
    {
        [|i = 10;|]
    }
}";
            var expected = @"using System;
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
}";

            TestExtractMethod(code, expected);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)]
        public void ExtractMethod31()
        {
            var code = @"using System;
using System.Collections.Generic;
using System.Text;

class Program
{
    void Test()
    {
        StringBuilder builder = new StringBuilder();
        [|builder.Append(""Hello"");
        builder.Append("" From "");
        builder.Append("" Roslyn"");|]
        return builder.ToString();
    }
}";
            var expected = @"using System;
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
        builder.Append(""Hello"");
        builder.Append("" From "");
        builder.Append("" Roslyn"");
    }
}";

            TestExtractMethod(code, expected);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)]
        public void ExtractMethod32()
        {
            var code = @"using System;

class Program
{
    void Test()
    {
        int v = 0;
        Console.Write([|v|]);
    }
}";
            var expected = @"using System;

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
}";

            TestExtractMethod(code, expected);
        }

        [WorkItem(3792, "DevDiv_Projects/Roslyn")]
        [WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)]
        public void ExtractMethod33()
        {
            var code = @"using System;

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
}";
            var expected = @"using System;

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
}";

            // this bug has two issues. one is "v" being not in the dataFlowIn and ReadInside collection (hence no method parameter)
            // and the other is binding not working for "v++" (hence object as return type)
            TestExtractMethod(code, expected);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)]
        public void ExtractMethod34()
        {
            var code = @"using System;

class Program
{
    static void Main(string[] args)
    {
        int x = 1;
        int y = 2;
        int z = [|x + y|];
    }
}
";
            var expected = @"using System;

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
";

            TestExtractMethod(code, expected);
        }

        [WorkItem(538239)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)]
        public void ExtractMethod35()
        {
            var code = @"using System;

class Program
{
    static void Main(string[] args)
    {
        int[] r = [|new int[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20 }|];
    }
}";
            var expected = @"using System;

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
}";

            TestExtractMethod(code, expected);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)]
        public void ExtractMethod36()
        {
            var code = @"using System;

class Program
{
    static void Main(ref int i)
    {
        [|i = 1;|]
    }
}";
            var expected = @"using System;

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
}";

            TestExtractMethod(code, expected);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)]
        public void ExtractMethod37()
        {
            var code = @"using System;

class Program
{
    static void Main(out int i)
    {
        [|i = 1;|]
    }
}";
            var expected = @"using System;

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
}";

            TestExtractMethod(code, expected);
        }

        [WorkItem(538231)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)]
        public void ExtractMethod38()
        {
            var code = @"using System;

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
}";
            var expected = @"using System;

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
        NewMethod(unassigned);

        // read 
        // int newVar = unassigned;

        // write
        // unassigned = 0;
    }

    private static void NewMethod(int unassigned)
    {
        unassigned = unassigned + 10;
    }
}";

            TestExtractMethod(code, expected);
        }

        [WorkItem(538231)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)]
        public void ExtractMethod39()
        {
            var code = @"using System;

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
}";
            var expected = @"using System;

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
        NewMethod(unassigned);
        // int newVar = unassigned;

        // write
        // unassigned = 0;
    }

    private static void NewMethod(int unassigned)
    {
        // unassigned = ReturnVal(0);
        unassigned = unassigned + 10;

        // read 
    }
}";

            // current bottom-up re-writer makes re-attaching trivia half belongs to previous token
            // and half belongs to next token very hard.
            // for now, it won't be able to re-associate trivia belongs to next token.
            TestExtractMethod(code, expected);
        }

        [WorkItem(538303)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)]
        public async Task ExtractMethod40()
        {
            var code = @"class Program
{
    static void Main(string[] args)
    {
        [|int x;|]
    }
}";
            await ExpectExtractMethodToFailAsync(code);
        }

        [WorkItem(868414)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)]
        public void ExtractMethodWithLeadingTrivia()
        {
            // ensure that the extraction doesn't result in trivia moving up a line:
            //        // a        //b
            //        NewMethod();
            TestExtractMethod(@"
class C
{
    void M()
    {
        // a
        // b
        [|System.Console.WriteLine();|]
    }
}
", @"
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
");
        }

        [WorkItem(632351)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)]
        public async Task ExtractMethodFailForTypeInFromClause()
        {
            var code = @"class Program
{
    static void Main(string[] args)
    {
        var z = from [|T|] x in e;
    }
}";
            await ExpectExtractMethodToFailAsync(code);
        }

        [WorkItem(632351)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)]
        public async Task ExtractMethodFailForTypeInFromClause_1()
        {
            var code = @"class Program
{
    static void Main(string[] args)
    {
        var z = from [|W.T|] x in e;
    }
}";
            await ExpectExtractMethodToFailAsync(code);
        }

        [WorkItem(538314)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)]
        public void ExtractMethod41()
        {
            var code = @"class Program
{
    static void Main(string[] args)
    {
        int x = 10;
        [|int y;
        if (x == 10)
            y = 5;|]
        Console.WriteLine(y);
    }
}";
            var expected = @"class Program
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
}";
            TestExtractMethod(code, expected);
        }

        [WorkItem(538327)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)]
        public void ExtractMethod42()
        {
            var code = @"using System;

class Program
{
    static void Main(string[] args)
    {
        int a, b;
        [|a = 5;
        b = 7;|]
        Console.Write(a + b);
    }
}";
            var expected = @"using System;

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
}";

            TestExtractMethod(code, expected);
        }

        [WorkItem(538327)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)]
        public void ExtractMethod43()
        {
            var code = @"using System;

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
}";
            var expected = @"using System;

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
}";

            TestExtractMethod(code, expected);
        }

        [WorkItem(538328)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)]
        public void ExtractMethod44()
        {
            var code = @"using System;

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
}";
            var expected = @"using System;

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
}";

            TestExtractMethod(code, expected);
        }

        [WorkItem(538393)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)]
        public void ExtractMethod45()
        {
            var code = @"using System;
using System.Collections.Generic;
using System.Linq;
class Program
{
    static void Main(string[] args)
    {
        /**/[|;|]/**/
    }
}";
            var expected = @"using System;
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
}";

            TestExtractMethod(code, expected);
        }

        [WorkItem(538393)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)]
        public void ExtractMethod46()
        {
            var code = @"using System;
using System.Collections.Generic;
using System.Linq;

class Program
{
    static void Main()
    {
        int x = 1;
        [|Foo(ref x);|]
        Console.WriteLine(x);
    }

    static void Foo(ref int x)
    {
        x = x + 1;
    }
}";
            var expected = @"using System;
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
        Foo(ref x);
        return x;
    }

    static void Foo(ref int x)
    {
        x = x + 1;
    }
}";
            TestExtractMethod(code, expected);
        }

        [WorkItem(538399)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)]
        public void ExtractMethod47()
        {
            var code = @"using System;
using System.Collections.Generic;
using System.Linq;

class Program
{
    static void Main()
    {
        int x = 1;
        [|while (true) Console.WriteLine(x);|]
    }
}";
            var expected = @"using System;
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
}";

            TestExtractMethod(code, expected);
        }

        [WorkItem(538401)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)]
        public void ExtractMethod48()
        {
            var code = @"using System;
using System.Collections.Generic;
using System.Linq;

class Program
{
    static void Main()
    {
        int[] x = [|{ 1, 2, 3 }|];
    }
}";
            var expected = @"using System;
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
}";

            TestExtractMethod(code, expected);
        }

        [WorkItem(538405)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)]
        public void ExtractMethod49()
        {
            var code = @"using System;
using System.Collections.Generic;
using System.Linq;
class Program
{
    static void Foo(int GetX)
    {
        int x = [|1|];
    }
}";
            var expected = @"using System;
using System.Collections.Generic;
using System.Linq;
class Program
{
    static void Foo(int GetX)
    {
        int x = GetX1();
    }

    private static int GetX1()
    {
        return 1;
    }
}";

            TestExtractMethod(code, expected);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)]
        public void ExtractMethodNormalProperty()
        {
            var code = @"
class Class
{
    private static string name;
    public static string Names
    {
        get { return ""1""; }
        set { name = value; }
    }
    static void Foo(int i)
    {
        string str = [|Class.Names|];
    }
}";
            var expected = @"
class Class
{
    private static string name;
    public static string Names
    {
        get { return ""1""; }
        set { name = value; }
    }
    static void Foo(int i)
    {
        string str = GetStr();
    }

    private static string GetStr()
    {
        return Class.Names;
    }
}";

            TestExtractMethod(code, expected);
        }

        [WorkItem(538932)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)]
        public void ExtractMethodAutoProperty()
        {
            var code = @"
class Class
{
    public string Name { get; set; }
    static void Main()
    {
       string str = new Class().[|Name|];
    }
}";

            // given span is not an expression
            // selection validator should take care of this case

            var expected = @"
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
}";

            TestExtractMethod(code, expected);
        }

        [WorkItem(538402)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)]
        public void BugFix3994()
        {
            var code = @"using System;
using System.Collections.Generic;
using System.Linq;

class Program
{
    static void Main()
    {
        byte x = [|1|];
    }
}";
            var expected = @"using System;
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
}";

            TestExtractMethod(code, expected);
        }

        [WorkItem(538404)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)]
        public void BugFix3996()
        {
            var code = @"class A<T>
{
    class D : A<T> { }
    class B { }

    static D.B Foo()
    {
        return null;
    }

    class C<T2>
    {
        static void Bar()
        {
            D.B x = [|Foo()|];
        }
    }
}";

            var expected = @"class A<T>
{
    class D : A<T> { }
    class B { }

    static D.B Foo()
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
            return Foo();
        }
    }
}";

            TestExtractMethod(code, expected);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)]
        public void InsertionPoint()
        {
            var code = @"class Test
{
    void Method(string i)
    {
        int y2 = [|1|];
    }

    void Method(int i)
    {
    }
}";

            var expected = @"class Test
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
}";

            TestExtractMethod(code, expected);
        }

        [WorkItem(538980)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)]
        public void BugFix4757()
        {
            var code = @"class GenericMethod
{
    void Method<T>(T t)
    {
        T a;
        [|a = t;|]
    }
}";

            var expected = @"class GenericMethod
{
    void Method<T>(T t)
    {
        NewMethod(t);
    }

    private static void NewMethod<T>(T t)
    {
        T a = t;
    }
}";

            TestExtractMethod(code, expected);
        }

        [WorkItem(538980)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)]
        public void BugFix4757_2()
        {
            var code = @"class GenericMethod<T1>
{
    void Method<T>(T t)
    {
        T a;
        T1 b;
        [|a = t;
        b = default(T1);|]
    }
}";

            var expected = @"class GenericMethod<T1>
{
    void Method<T>(T t)
    {
        NewMethod(t);
    }

    private static void NewMethod<T>(T t)
    {
        T a;
        T1 b;
        a = t;
        b = default(T1);
    }
}";

            TestExtractMethod(code, expected);
        }

        [WorkItem(538980)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)]
        public void BugFix4757_3()
        {
            var code = @"class GenericMethod
{
    void Method<T, T1>(T t)
    {
        T1 a1;
        T a;
        [|a = t;
        a1 = default(T);|]
    }
}";

            var expected = @"class GenericMethod
{
    void Method<T, T1>(T t)
    {
        NewMethod<T, T1>(t);
    }

    private static void NewMethod<T, T1>(T t)
    {
        T1 a1;
        T a;
        a = t;
        a1 = default(T);
    }
}";

            TestExtractMethod(code, expected);
        }

        [WorkItem(538422)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)]
        public void BugFix4758()
        {
            var code = @"using System;
class TestOutParameter
{
    void Method(out int x)
    {
        x = 5;
        Console.Write([|x|]);
    }
}";

            var expected = @"using System;
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
}";

            TestExtractMethod(code, expected);
        }

        [WorkItem(538422)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)]
        public void BugFix4758_2()
        {
            var code = @"class TestOutParameter
{
    void Method(out int x)
    {
        x = 5;
        Console.Write([|x|]);
    }
}";

            var expected = @"class TestOutParameter
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
}";

            TestExtractMethod(code, expected);
        }

        [WorkItem(538984)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)]
        public void BugFix4761()
        {
            var code = @"using System;

class A
{
    void Method()
    {
        System.Func<int, int> a = x => [|x * x|];
    }
}";

            var expected = @"using System;

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
}";

            TestExtractMethod(code, expected);
        }

        [WorkItem(538997)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)]
        public void BugFix4779()
        {
            var code = @"using System;

class Program
{
    static void Main()
    {
        string s = "";
        Func<string> f = [|s|].ToString;
    }
}
";

            var expected = @"using System;

class Program
{
    static void Main()
    {
        string s = "";
        Func<string> f = GetS(s).ToString;
    }

    private static string GetS(string s)
    {
        return s;
    }
}
";

            TestExtractMethod(code, expected);
        }

        [WorkItem(538997)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)]
        public void BugFix4779_2()
        {
            var code = @"using System;

class Program
{
    static void Main()
    {
        string s = "";
        var f = [|s|].ToString();
    }
}
";

            var expected = @"using System;

class Program
{
    static void Main()
    {
        string s = "";
        var f = GetS(s).ToString();
    }

    private static string GetS(string s)
    {
        return s;
    }
}
";

            TestExtractMethod(code, expected);
        }

        [WorkItem(4780, "DevDiv_Projects/Roslyn")]
        [WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)]
        public void BugFix4780()
        {
            var code = @"using System;

class Program
{
    static void Main()
    {
        string s = "";
        object f = (Func<string>)[|s.ToString|];
    }
}";

            var expected = @"using System;

class Program
{
    static void Main()
    {
        string s = "";
        object f = (Func<string>)GetToString(s);
    }

    private static Func<string> GetToString(string s)
    {
        return s.ToString;
    }
}";

            TestExtractMethod(code, expected);
        }

        [WorkItem(4780, "DevDiv_Projects/Roslyn")]
        [WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)]
        public void BugFix4780_2()
        {
            var code = @"using System;

class Program
{
    static void Main()
    {
        string s = "";
        object f = (string)[|s.ToString()|];
    }
}";

            var expected = @"using System;

class Program
{
    static void Main()
    {
        string s = "";
        object f = (string)NewMethod(s);
    }

    private static string NewMethod(string s)
    {
        return s.ToString();
    }
}";

            TestExtractMethod(code, expected);
        }

        [WorkItem(4782, "DevDiv_Projects/Roslyn")]
        [WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)]
        public void BugFix4782()
        {
            var code = @"class A<T>
{
    class D : A<T[]> { }
    class B { }

    class C<T>
    {
        static void Foo<T>(T a)
        {
            T t = [|default(T)|];
        }
    }
}";

            var expected = @"class A<T>
{
    class D : A<T[]> { }
    class B { }

    class C<T>
    {
        static void Foo<T>(T a)
        {
            T t = GetT<T>();
        }

        private static T GetT<T>()
        {
            return default(T);
        }
    }
}";
            TestExtractMethod(code, expected);
        }

        [WorkItem(4782, "DevDiv_Projects/Roslyn")]
        [WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)]
        public async Task BugFix4782_2()
        {
            var code = @"class A<T>
{
    class D : A<T[]> { }
    class B { }

    class C<T>
    {
        static void Foo()
        {
            D.B x = [|new D.B()|];
        }
    }
}";

            await ExpectExtractMethodToFailAsync(code);
        }

        [WorkItem(4791, "DevDiv_Projects/Roslyn")]
        [WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)]
        public void BugFix4791()
        {
            var code = @"class Program
{
    delegate int Func(int a);

    static void Main(string[] args)
    {
        Func v = (int a) => [|a|];
    }
}";

            var expected = @"class Program
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
}";

            TestExtractMethod(code, expected);
        }

        [WorkItem(539019)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)]
        public void BugFix4809()
        {
            var code = @"class Program
{
    public Program()
    {
        [|int x = 2;|]
    }
}";

            var expected = @"class Program
{
    public Program()
    {
        NewMethod();
    }

    private static void NewMethod()
    {
        int x = 2;
    }
}";

            TestExtractMethod(code, expected);
        }

        [WorkItem(539029)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)]
        public void BugFix4813()
        {
            var code = @"using System;

class Program
{
    public Program()
    {
        object o = [|new Program()|];
    }
}";

            var expected = @"using System;

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
}";

            TestExtractMethod(code, expected);
        }

        [WorkItem(538425)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)]
        public void BugFix4031()
        {
            var code = @"using System;
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
}";

            var expected = @"using System;
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
}";

            TestExtractMethod(code, expected);
        }

        [WorkItem(527499)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)]
        public void BugFix3992()
        {
            var code = @"using System;
using System.Collections.Generic;
using System.Linq;
class Program
{
    static void Main()
    {
        int x = 1;
        [|while (false) Console.WriteLine(x);|]
    }
}";

            var expected = @"using System;
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
}";
            TestExtractMethod(code, expected);
        }

        [WorkItem(539029)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)]
        public void BugFix4823()
        {
            var code = @"class Program
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
        return string.Format(""{0:F2}"", [|Area|]);
    }
}";

            var expected = @"class Program
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
        return string.Format(""{0:F2}"", GetArea());
    }

    private double GetArea()
    {
        return Area;
    }
}";

            TestExtractMethod(code, expected);
        }

        [WorkItem(538985)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)]
        public void BugFix4762()
        {
            var code = @"class Program
{
    static void Main(string[] args)
    {
        //comments
        [|int x = 2;|]
    }
}
";

            var expected = @"class Program
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
";

            TestExtractMethod(code, expected);
        }

        [WorkItem(538966)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)]
        public void BugFix4744()
        {
            var code = @"class Program
{
    static void Main(string[] args)
    {
        [|int x = 2;
        //comments|]
    }
}
";

            var expected = @"class Program
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
";

            TestExtractMethod(code, expected);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)]
        public void MatrixCase_NoNoNoNoNoYesNoNo()
        {
            var code = @"using System;

class Program
{
    void Test1()
    {
        int i;

        [|
        if (int.Parse(""1"") > 0)
        {
            i = 10;
        }
        |]
    }
}";

            var expected = @"using System;

class Program
{
    void Test1()
    {
        int i;

        NewMethod(i);
    }

    private static void NewMethod(int i)
    {
        if (int.Parse(""1"") > 0)
        {
            i = 10;
        }
    }
}";

            TestExtractMethod(code, expected);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)]
        public void MatrixCase_NoNoNoNoNoYesNoYes()
        {
            var code = @"using System;

class Program
{
    void Test2()
    {
        int i = 0;

        [|
        if (int.Parse(""1"") > 0)
        {
            i = 10;
        }
        |]
    }
}";

            var expected = @"using System;

class Program
{
    void Test2()
    {
        int i = 0;

        NewMethod(i);
    }

    private static void NewMethod(int i)
    {
        if (int.Parse(""1"") > 0)
        {
            i = 10;
        }
    }
}";

            TestExtractMethod(code, expected);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)]
        public void MatrixCase_NoNoNoNoNoYesYesNo()
        {
            var code = @"using System;

class Program
{
    void Test3()
    {
        int i;

        while (i > 10) ;

        [|
        if (int.Parse(""1"") > 0)
        {
            i = 10;
        }
        |]
    }
}";

            var expected = @"using System;

class Program
{
    void Test3()
    {
        int i;

        while (i > 10) ;

        NewMethod(i);
    }

    private static void NewMethod(int i)
    {
        if (int.Parse(""1"") > 0)
        {
            i = 10;
        }
    }
}";

            TestExtractMethod(code, expected);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)]
        public void MatrixCase_NoNoNoNoNoYesYesYes()
        {
            var code = @"using System;

class Program
{
    void Test4()
    {
        int i = 10;

        while (i > 10) ;

        [|
        if (int.Parse(""1"") > 0)
        {
            i = 10;
        }
        |]
    }
}";

            var expected = @"using System;

class Program
{
    void Test4()
    {
        int i = 10;

        while (i > 10) ;

        NewMethod(i);
    }

    private static void NewMethod(int i)
    {
        if (int.Parse(""1"") > 0)
        {
            i = 10;
        }
    }
}";

            TestExtractMethod(code, expected);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)]
        public void MatrixCase_NoNoNoNoYesYesNoNo()
        {
            var code = @"using System;

class Program
{
    void Test4_1()
    {
        int i;

        [|
        if (int.Parse(""1"") > 0)
        {
            i = 10;
            Console.WriteLine(i);
        }
        |]
    }
}";

            var expected = @"using System;

class Program
{
    void Test4_1()
    {
        NewMethod();
    }

    private static void NewMethod()
    {
        int i;
        if (int.Parse(""1"") > 0)
        {
            i = 10;
            Console.WriteLine(i);
        }
    }
}";

            TestExtractMethod(code, expected);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)]
        public void MatrixCase_NoNoNoNoYesYesNoYes()
        {
            var code = @"using System;

class Program
{
    void Test4_2()
    {
        int i = 10;

        [|
        if (int.Parse(""1"") > 0)
        {
            i = 10;
            Console.WriteLine(i);
        }
        |]
    }
}";

            var expected = @"using System;

class Program
{
    void Test4_2()
    {
        int i = 10;

        NewMethod(i);
    }

    private static void NewMethod(int i)
    {
        if (int.Parse(""1"") > 0)
        {
            i = 10;
            Console.WriteLine(i);
        }
    }
}";

            TestExtractMethod(code, expected);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)]
        public void MatrixCase_NoNoNoNoYesYesYesNo()
        {
            var code = @"using System;

class Program
{
    void Test4_3()
    {
        int i;

        Console.WriteLine(i);

        [|
        if (int.Parse(""1"") > 0)
        {
            i = 10;
            Console.WriteLine(i);
        }
        |]
    }
}";

            var expected = @"using System;

class Program
{
    void Test4_3()
    {
        int i;

        Console.WriteLine(i);

        NewMethod();
    }

    private static void NewMethod()
    {
        int i;
        if (int.Parse(""1"") > 0)
        {
            i = 10;
            Console.WriteLine(i);
        }
    }
}";

            TestExtractMethod(code, expected);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)]
        public void MatrixCase_NoNoNoNoYesYesYesYes()
        {
            var code = @"using System;

class Program
{
    void Test4_4()
    {
        int i = 10;

        Console.WriteLine(i);

        [|
        if (int.Parse(""1"") > 0)
        {
            i = 10;
            Console.WriteLine(i);
        }
        |]
    }
}";

            var expected = @"using System;

class Program
{
    void Test4_4()
    {
        int i = 10;

        Console.WriteLine(i);

        NewMethod(i);
    }

    private static void NewMethod(int i)
    {
        if (int.Parse(""1"") > 0)
        {
            i = 10;
            Console.WriteLine(i);
        }
    }
}";

            TestExtractMethod(code, expected);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)]
        public async Task MatrixCase_NoNoNoYesNoNoNoNo()
        {
            var code = @"using System;

class Program
{
    void Test5()
    {
        [|
        int i;
        |]
    }
}";

            await ExpectExtractMethodToFailAsync(code);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)]
        public async Task MatrixCase_NoNoNoYesNoNoNoYes()
        {
            var code = @"using System;

class Program
{
    void Test6()
    {
        [|
        int i;
        |]

        i = 1;
    }
}";

            await ExpectExtractMethodToFailAsync(code);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)]
        public void MatrixCase_NoNoNoYesNoYesNoNo()
        {
            var code = @"using System;

class Program
{
    void Test7()
    {
        [|
        int i;

        if (int.Parse(""1"") > 0)
        {
            i = 10;
        }
        |]
    }
}";

            var expected = @"using System;

class Program
{
    void Test7()
    {
        NewMethod();
    }

    private static void NewMethod()
    {
        int i;

        if (int.Parse(""1"") > 0)
        {
            i = 10;
        }
    }
}";

            TestExtractMethod(code, expected);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)]
        public void MatrixCase_NoNoNoYesNoYesNoYes()
        {
            var code = @"using System;

class Program
{
    void Test8()
    {
        [|
        int i;

        if (int.Parse(""1"") > 0)
        {
            i = 10;
        }
        |]

        i = 2;
    }
}";

            var expected = @"using System;

class Program
{
    void Test8()
    {
        int i;
        NewMethod();

        i = 2;
    }

    private static void NewMethod()
    {
        int i;

        if (int.Parse(""1"") > 0)
        {
            i = 10;
        }
    }
}";

            TestExtractMethod(code, expected);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)]
        public void MatrixCase_NoNoNoYesYesNoNoNo()
        {
            var code = @"using System;

class Program
{
    void Test9()
    {
        [|
        int i;

        Console.WriteLine(i);
        |]
    }
}";

            var expected = @"using System;

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
}";

            TestExtractMethod(code, expected);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)]
        public void MatrixCase_NoNoNoYesYesNoNoYes()
        {
            var code = @"using System;

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
}";

            var expected = @"using System;

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
}";

            TestExtractMethod(code, expected);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)]
        public void MatrixCase_NoNoNoYesYesYesNoNo()
        {
            var code = @"using System;

class Program
{
    void Test11()
    {
        [|
        int i;

        if (int.Parse(""1"") > 0)
        {
            i = 10;
        }

        Console.WriteLine(i);
        |]
    }
}";

            var expected = @"using System;

class Program
{
    void Test11()
    {
        NewMethod();
    }

    private static void NewMethod()
    {
        int i;

        if (int.Parse(""1"") > 0)
        {
            i = 10;
        }

        Console.WriteLine(i);
    }
}";

            TestExtractMethod(code, expected);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)]
        public void MatrixCase_NoNoNoYesYesYesNoYes()
        {
            var code = @"using System;

class Program
{
    void Test12()
    {
        [|
        int i;

        if (int.Parse(""1"") > 0)
        {
            i = 10;
        }

        Console.WriteLine(i);
        |]

        i = 10;
    }
}";

            var expected = @"using System;

class Program
{
    void Test12()
    {
        int i;
        NewMethod();

        i = 10;
    }

    private static void NewMethod()
    {
        int i;

        if (int.Parse(""1"") > 0)
        {
            i = 10;
        }

        Console.WriteLine(i);
    }
}";

            TestExtractMethod(code, expected);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)]
        public void MatrixCase_NoNoYesNoNoYesNoNo()
        {
            var code = @"using System;

class Program
{
    void Test13()
    {
        int i;

        [|
        i = 10;
        |]
    }
}";

            var expected = @"using System;

class Program
{
    void Test13()
    {
        NewMethod();
    }

    private static void NewMethod()
    {
        int i = 10;
    }
}";

            TestExtractMethod(code, expected);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)]
        public void MatrixCase_NoNoYesNoNoYesNoYes()
        {
            var code = @"using System;

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
}";

            var expected = @"using System;

class Program
{
    void Test14()
    {
        int i;

        NewMethod();

        i = 1;
    }

    private static void NewMethod()
    {
        int i = 10;
    }
}";

            TestExtractMethod(code, expected);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)]
        public void MatrixCase_NoNoYesNoNoYesYesNo()
        {
            var code = @"using System;

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
}";

            var expected = @"using System;

class Program
{
    void Test15()
    {
        int i;

        Console.WriteLine(i);

        NewMethod();
    }

    private static void NewMethod()
    {
        int i = 10;
    }
}";

            TestExtractMethod(code, expected);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)]
        public void MatrixCase_NoNoYesNoNoYesYesYes()
        {
            var code = @"using System;

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
}";

            var expected = @"using System;

class Program
{
    void Test16()
    {
        int i;

        NewMethod();

        i = 10;

        Console.WriteLine(i);
    }

    private static void NewMethod()
    {
        int i = 10;
    }
}";

            TestExtractMethod(code, expected);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)]
        public void MatrixCase_NoNoYesNoYesYesNoNo()
        {
            var code = @"using System;

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
}";

            var expected = @"using System;

class Program
{
    void Test16_1()
    {
        NewMethod();
    }

    private static void NewMethod()
    {
        int i = 10;
        Console.WriteLine(i);
    }
}";

            TestExtractMethod(code, expected);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)]
        public void MatrixCase_NoNoYesNoYesYesNoYes()
        {
            var code = @"using System;

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
}";

            var expected = @"using System;

class Program
{
    void Test16_2()
    {
        int i = 10;

        NewMethod();
    }

    private static void NewMethod()
    {
        int i = 10;
        Console.WriteLine(i);
    }
}";

            TestExtractMethod(code, expected);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)]
        public void MatrixCase_NoNoYesNoYesYesYesNo()
        {
            var code = @"using System;

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
}";

            var expected = @"using System;

class Program
{
    void Test16_3()
    {
        int i;

        Console.WriteLine(i);

        NewMethod();
    }

    private static void NewMethod()
    {
        int i = 10;
        Console.WriteLine(i);
    }
}";

            TestExtractMethod(code, expected);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)]
        public void MatrixCase_NoNoYesNoYesYesYesYes()
        {
            var code = @"using System;

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
}";

            var expected = @"using System;

class Program
{
    void Test16_4()
    {
        int i = 10;

        Console.WriteLine(i);

        NewMethod();
    }

    private static void NewMethod()
    {
        int i = 10;
        Console.WriteLine(i);
    }
}";

            TestExtractMethod(code, expected);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)]
        public void MatrixCase_NoNoYesYesNoYesNoNo()
        {
            var code = @"using System;

class Program
{
    void Test17()
    {
        [|
        int i = 10;
        |]
    }
}";

            var expected = @"using System;

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
}";

            TestExtractMethod(code, expected);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)]
        public void MatrixCase_NoNoYesYesNoYesNoYes()
        {
            var code = @"using System;

class Program
{
    void Test18()
    {
        [|
        int i = 10;
        |]

        i = 10;
    }
}";

            var expected = @"using System;

class Program
{
    void Test18()
    {
        int i;
        NewMethod();

        i = 10;
    }

    private static void NewMethod()
    {
        int i = 10;
    }
}";

            TestExtractMethod(code, expected);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)]
        public void MatrixCase_NoNoYesYesYesYesNoNo()
        {
            var code = @"using System;

class Program
{
    void Test19()
    {
        [|
        int i = 10;
        Console.WriteLine(i);
        |]
    }
}";

            var expected = @"using System;

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
}";

            TestExtractMethod(code, expected);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)]
        public void MatrixCase_NoNoYesYesYesYesNoYes()
        {
            var code = @"using System;

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
}";

            var expected = @"using System;

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
}";

            TestExtractMethod(code, expected);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)]
        public void MatrixCase_NoYesNoNoNoYesYesNo()
        {
            var code = @"using System;

class Program
{
    void Test21()
    {
        int i;

        [|
        if (int.Parse(""1"") > 10)
        {
            i = 1;
        }
        |]

        Console.WriteLine(i);
    }
}";

            var expected = @"using System;

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
        if (int.Parse(""1"") > 10)
        {
            i = 1;
        }

        return i;
    }
}";

            TestExtractMethod(code, expected);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)]
        public void MatrixCase_NoYesNoNoNoYesYesYes()
        {
            var code = @"using System;

class Program
{
    void Test22()
    {
        int i = 10;

        [|
        if (int.Parse(""1"") > 10)
        {
            i = 1;
        }
        |]

        Console.WriteLine(i);
    }
}";

            var expected = @"using System;

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
        if (int.Parse(""1"") > 10)
        {
            i = 1;
        }

        return i;
    }
}";

            TestExtractMethod(code, expected);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)]
        public void MatrixCase_NoYesNoNoYesYesYesNo()
        {
            var code = @"using System;

class Program
{
    void Test22_1()
    {
        int i;

        [|
        if (int.Parse(""1"") > 10)
        {
            i = 1;
            Console.WriteLine(i);
        }
        |]

        Console.WriteLine(i);
    }
}";

            var expected = @"using System;

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
        if (int.Parse(""1"") > 10)
        {
            i = 1;
            Console.WriteLine(i);
        }

        return i;
    }
}";

            TestExtractMethod(code, expected);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)]
        public void MatrixCase_NoYesNoNoYesYesYesYes()
        {
            var code = @"using System;

class Program
{
    void Test22_2()
    {
        int i = 10;

        [|
        if (int.Parse(""1"") > 10)
        {
            i = 1;
            Console.WriteLine(i);
        }
        |]

        Console.WriteLine(i);
    }
}";

            var expected = @"using System;

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
        if (int.Parse(""1"") > 10)
        {
            i = 1;
            Console.WriteLine(i);
        }

        return i;
    }
}";

            TestExtractMethod(code, expected);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)]
        public async Task MatrixCase_NoYesNoYesNoNoYesNo()
        {
            var code = @"using System;

class Program
{
    void Test23()
    {
        [|
        int i;
        |]

        Console.WriteLine(i);
    }
}";

            await ExpectExtractMethodToFailAsync(code);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)]
        public async Task MatrixCase_NoYesNoYesNoNoYesYes()
        {
            var code = @"using System;

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
}";

            await ExpectExtractMethodToFailAsync(code);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)]
        public void MatrixCase_NoYesNoYesNoYesYesNo()
        {
            var code = @"using System;

class Program
{
    void Test25()
    {
        [|
        int i;

        if (int.Parse(""1"") > 9)
        {
            i = 10;
        }
        |]

        Console.WriteLine(i);
    }
}";

            var expected = @"using System;

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

        if (int.Parse(""1"") > 9)
        {
            i = 10;
        }

        return i;
    }
}";

            TestExtractMethod(code, expected);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)]
        public void MatrixCase_NoYesNoYesNoYesYesYes()
        {
            var code = @"using System;

class Program
{
    void Test26()
    {
        [|
        int i;

        if (int.Parse(""1"") > 9)
        {
            i = 10;
        }
        |]

        Console.WriteLine(i);

        i = 10;
    }
}";

            var expected = @"using System;

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

        if (int.Parse(""1"") > 9)
        {
            i = 10;
        }

        return i;
    }
}";

            TestExtractMethod(code, expected);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)]
        public void MatrixCase_NoYesNoYesYesNoYesNo()
        {
            var code = @"using System;

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
}";

            var expected = @"using System;

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
}";

            TestExtractMethod(code, expected);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)]
        public void MatrixCase_NoYesNoYesYesNoYesYes()
        {
            var code = @"using System;

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
}";

            var expected = @"using System;

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
}";

            TestExtractMethod(code, expected);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)]
        public void MatrixCase_NoYesNoYesYesYesYesNo()
        {
            var code = @"using System;

class Program
{
    void Test29()
    {
        [|
        int i;

        if (int.Parse(""1"") > 0)
        {
            i = 10;
        }

        Console.WriteLine(i);
        |]

        Console.WriteLine(i);
    }
}";

            var expected = @"using System;

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

        if (int.Parse(""1"") > 0)
        {
            i = 10;
        }

        Console.WriteLine(i);
        return i;
    }
}";

            TestExtractMethod(code, expected);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)]
        public void MatrixCase_NoYesNoYesYesYesYesYes()
        {
            var code = @"using System;

class Program
{
    void Test30()
    {
        [|
        int i;

        if (int.Parse(""1"") > 0)
        {
            i = 10;
        }

        Console.WriteLine(i);
        |]

        Console.WriteLine(i);

        i = 10;
    }
}";

            var expected = @"using System;

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

        if (int.Parse(""1"") > 0)
        {
            i = 10;
        }

        Console.WriteLine(i);
        return i;
    }
}";

            TestExtractMethod(code, expected);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)]
        public void MatrixCase_NoYesYesNoNoYesYesNo()
        {
            var code = @"using System;

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
}";

            var expected = @"using System;

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
}";

            TestExtractMethod(code, expected);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)]
        public void MatrixCase_NoYesYesNoNoYesYesYes()
        {
            var code = @"using System;

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
}";

            var expected = @"using System;

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
}";

            TestExtractMethod(code, expected);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)]
        public void MatrixCase_NoYesYesNoYesYesYesNo()
        {
            var code = @"using System;

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
}";

            var expected = @"using System;

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
}";

            TestExtractMethod(code, expected);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)]
        public void MatrixCase_NoYesYesNoYesYesYesYes()
        {
            var code = @"using System;

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
}";

            var expected = @"using System;

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
}";

            TestExtractMethod(code, expected);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)]
        public void MatrixCase_NoYesYesYesNoYesYesNo()
        {
            var code = @"using System;

class Program
{
    void Test33()
    {
        [|
        int i = 10;
        |]

        Console.WriteLine(i);
    }
}";

            var expected = @"using System;

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
}";

            TestExtractMethod(code, expected);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)]
        public void MatrixCase_NoYesYesYesNoYesYesYes()
        {
            var code = @"using System;

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
}";

            var expected = @"using System;

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
}";

            TestExtractMethod(code, expected);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)]
        public void MatrixCase_NoYesYesYesYesYesYesNo()
        {
            var code = @"using System;

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
}";

            var expected = @"using System;

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
}";

            TestExtractMethod(code, expected);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)]
        public void MatrixCase_NoYesYesYesYesYesYesYes()
        {
            var code = @"using System;

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
}";

            var expected = @"using System;

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
}";

            TestExtractMethod(code, expected);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)]
        public void MatrixCase_YesNoNoNoYesNoNoNo()
        {
            var code = @"using System;

class Program
{
    void Test37()
    {
        int i;

        [|
        Console.WriteLine(i);
        |]
    }
}";

            var expected = @"using System;

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
}";

            TestExtractMethod(code, expected);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)]
        public void MatrixCase_YesNoNoNoYesNoNoYes()
        {
            var code = @"using System;

class Program
{
    void Test38()
    {
        int i = 10;

        [|
        Console.WriteLine(i);
        |]
    }
}";

            var expected = @"using System;

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
}";

            TestExtractMethod(code, expected);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)]
        public void MatrixCase_YesNoNoNoYesNoYesNo()
        {
            var code = @"using System;

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
}";

            var expected = @"using System;

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
}";

            TestExtractMethod(code, expected);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)]
        public void MatrixCase_YesNoNoNoYesNoYesYes()
        {
            var code = @"using System;

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
}";

            var expected = @"using System;

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
}";

            TestExtractMethod(code, expected);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)]
        public void MatrixCase_YesNoNoNoYesYesNoNo()
        {
            var code = @"using System;

class Program
{
    void Test41()
    {
        int i;

        [|
        Console.WriteLine(i);

        if (int.Parse(""1"") > 0)
        {
            i = 10;
        }
        |]
    }
}";

            var expected = @"using System;

class Program
{
    void Test41()
    {
        int i;

        NewMethod(i);
    }

    private static void NewMethod(int i)
    {
        Console.WriteLine(i);

        if (int.Parse(""1"") > 0)
        {
            i = 10;
        }
    }
}";

            TestExtractMethod(code, expected);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)]
        public void MatrixCase_YesNoNoNoYesYesNoYes()
        {
            var code = @"using System;

class Program
{
    void Test42()
    {
        int i = 10;

        [|
        Console.WriteLine(i);

        if (int.Parse(""1"") > 0)
        {
            i = 10;
        }
        |]
    }
}";

            var expected = @"using System;

class Program
{
    void Test42()
    {
        int i = 10;

        NewMethod(i);
    }

    private static void NewMethod(int i)
    {
        Console.WriteLine(i);

        if (int.Parse(""1"") > 0)
        {
            i = 10;
        }
    }
}";

            TestExtractMethod(code, expected);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)]
        public void MatrixCase_YesNoNoNoYesYesYesNo()
        {
            var code = @"using System;

class Program
{
    void Test43()
    {
        int i;

        Console.WriteLine(i);

        [|
        Console.WriteLine(i);

        if (int.Parse(""1"") > 0)
        {
            i = 10;
        }
        |]
    }
}";

            var expected = @"using System;

class Program
{
    void Test43()
    {
        int i;

        Console.WriteLine(i);

        NewMethod(i);
    }

    private static void NewMethod(int i)
    {
        Console.WriteLine(i);

        if (int.Parse(""1"") > 0)
        {
            i = 10;
        }
    }
}";

            TestExtractMethod(code, expected);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)]
        public void MatrixCase_YesNoNoNoYesYesYesYes()
        {
            var code = @"using System;

class Program
{
    void Test44()
    {
        int i = 10;

        Console.WriteLine(i);

        [|
        Console.WriteLine(i);

        if (int.Parse(""1"") > 0)
        {
            i = 10;
        }
        |]
    }
}";

            var expected = @"using System;

class Program
{
    void Test44()
    {
        int i = 10;

        Console.WriteLine(i);

        NewMethod(i);
    }

    private static void NewMethod(int i)
    {
        Console.WriteLine(i);

        if (int.Parse(""1"") > 0)
        {
            i = 10;
        }
    }
}";

            TestExtractMethod(code, expected);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)]
        public void MatrixCase_YesNoYesNoYesYesNoNo()
        {
            var code = @"using System;

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
}";

            var expected = @"using System;

class Program
{
    void Test45()
    {
        int i;

        NewMethod(i);
    }

    private static void NewMethod(int i)
    {
        Console.WriteLine(i);

        i = 10;
    }
}";

            TestExtractMethod(code, expected);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)]
        public void MatrixCase_YesNoYesNoYesYesNoYes()
        {
            var code = @"using System;

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
}";

            var expected = @"using System;

class Program
{
    void Test46()
    {
        int i = 10;

        NewMethod(i);
    }

    private static void NewMethod(int i)
    {
        Console.WriteLine(i);

        i = 10;
    }
}";

            TestExtractMethod(code, expected);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)]
        public void MatrixCase_YesNoYesNoYesYesYesNo()
        {
            var code = @"using System;

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
}";

            var expected = @"using System;

class Program
{
    void Test47()
    {
        int i;

        Console.WriteLine(i);

        NewMethod(i);
    }

    private static void NewMethod(int i)
    {
        Console.WriteLine(i);

        i = 10;
    }
}";

            TestExtractMethod(code, expected);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)]
        public void MatrixCase_YesNoYesNoYesYesYesYes()
        {
            var code = @"using System;

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
}";

            var expected = @"using System;

class Program
{
    void Test48()
    {
        int i = 10;

        Console.WriteLine(i);

        NewMethod(i);
    }

    private static void NewMethod(int i)
    {
        Console.WriteLine(i);

        i = 10;
    }
}";

            TestExtractMethod(code, expected);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)]
        public void MatrixCase_YesYesNoNoYesYesYesNo()
        {
            var code = @"using System;

class Program
{
    void Test49()
    {
        int i;

        [|
        Console.WriteLine(i);

        if (int.Parse(""1"") > 0)
        {
            i = 10;
        }
        |]

        Console.WriteLine(i);
    }
}";

            var expected = @"using System;

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

        if (int.Parse(""1"") > 0)
        {
            i = 10;
        }

        return i;
    }
}";

            TestExtractMethod(code, expected);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)]
        public void MatrixCase_YesYesNoNoYesYesYesYes()
        {
            var code = @"using System;

class Program
{
    void Test50()
    {
        int i = 10;

        [|
        Console.WriteLine(i);

        if (int.Parse(""1"") > 0)
        {
            i = 10;
        }
        |]

        Console.WriteLine(i);
    }
}";

            var expected = @"using System;

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

        if (int.Parse(""1"") > 0)
        {
            i = 10;
        }

        return i;
    }
}";

            TestExtractMethod(code, expected);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)]
        public void MatrixCase_YesYesYesNoYesYesYesNo()
        {
            var code = @"using System;

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
}";

            var expected = @"using System;

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
}";

            TestExtractMethod(code, expected);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)]
        public void MatrixCase_YesYesYesNoYesYesYesYes()
        {
            var code = @"using System;

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
}";

            var expected = @"using System;

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
}";

            TestExtractMethod(code, expected);
        }

        [WorkItem(539049)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)]
        public void ExtractMethodInProperty1()
        {
            var code = @"class C2
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
";

            var expected = @"class C2
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
";

            TestExtractMethod(code, expected);
        }

        [WorkItem(539049)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)]
        public void ExtractMethodInProperty2()
        {
            var code = @"class C3
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
";

            var expected = @"class C3
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
";

            TestExtractMethod(code, expected);
        }

        [WorkItem(539049)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)]
        public void ExtractMethodInProperty3()
        {
            var code = @"class C3
{
    public static int Area
    {
        set
        {
            [|int i = value;|]
        }
    }
}
";

            var expected = @"class C3
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
";

            TestExtractMethod(code, expected);
        }

        [WorkItem(539029)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)]
        public void ExtractMethodProperty()
        {
            var code = @"class Program
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
        return string.Format(""{0:F2}"", [|Area|]);
    }
}
";

            var expected = @"class Program
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
        return string.Format(""{0:F2}"", GetArea());
    }

    private double GetArea()
    {
        return Area;
    }
}
";
            TestExtractMethod(code, expected);
        }

        [WorkItem(539196)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)]
        public void ExtractMethodWithDeclareOneMoreVariablesInSameLineBeUsedAfter()
        {
            var code = @"class C
{
    void M()
    {
        [|int x, y = 1;|]
        x = 4;
        Console.Write(x + y);
    }
}";
            var expected = @"class C
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
}";
            TestExtractMethod(code, expected);
        }

        [WorkItem(539196)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)]
        public void ExtractMethodWithDeclareOneMoreVariablesInSameLineNotBeUsedAfter()
        {
            var code = @"class C
{
    void M()
    {
        [|int x, y = 1;|]
    }
}";
            var expected = @"class C
{
    void M()
    {
        NewMethod();
    }

    private static void NewMethod()
    {
        int x, y = 1;
    }
}";
            TestExtractMethod(code, expected);
        }

        [WorkItem(539214)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)]
        public void ExtractMethodForSplitOutStatementWithComments()
        {
            var code = @"class C
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
}";
            var expected = @"class C
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
}";
            TestExtractMethod(code, expected);
        }

        [WorkItem(539225)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)]
        public async Task Bug5098()
        {
            var code = @"class Program
{
    static void Main(string[] args)
    {
        [|return;|]
        Console.Write(4);
    }
}";

            await ExpectExtractMethodToFailAsync(code);
        }

        [WorkItem(539229)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)]
        public void Bug5107()
        {
            var code = @"class Program
{
    static void Main(string[] args)
    {
        int i = 10;
        [|int j = j + i;|]
        Console.Write(i);
        Console.Write(j); 
    }
}";

            var expected = @"class Program
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
}";

            TestExtractMethod(code, expected);
        }

        [WorkItem(539500)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)]
        public void LambdaLiftedVariable1()
        {
            var code = @"class Program
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
}";

            var expected = @"class Program
{
    delegate void Func(ref int i, int r);
    static void Main(string[] args)
    {
        int temp = 2;

        Func fnc = (ref int arg, int arg2) => { arg = NewMethod(arg2); };
        temp = 4;
        fnc(ref temp, 2);

        System.Console.WriteLine(temp);
    }

    private static int NewMethod(int arg2)
    {
        int arg, temp;
        temp = arg = arg2;
        return arg;
    }
}";

            TestExtractMethod(code, expected);
        }

        [WorkItem(539488)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)]
        public void LambdaLiftedVariable2()
        {
            var code = @"class Program
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
}";

            var expected = @"class Program
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
        NewMethod();
        query();
    }

    private static void NewMethod()
    {
        int i = 3;
    }
}";

            TestExtractMethod(code, expected);
        }

        [WorkItem(539531)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)]
        public void Bug5533()
        {
            var code = @"using System;
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
}";

            var expected = @"using System;
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
}";

            TestExtractMethod(code, expected);
        }

        [WorkItem(539531)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)]
        public void Bug5533_1()
        {
            var code = @"using System;
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
}";

            var expected = @"using System;
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
}";

            TestExtractMethod(code, expected);
        }

        [WorkItem(539531)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)]
        public void Bug5533_2()
        {
            var code = @"using System;
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
}";

            var expected = @"using System;
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
}";

            TestExtractMethod(code, expected);
        }

        [WorkItem(539531)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)]
        public void Bug5533_3()
        {
            var code = @"using System;
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
}";

            var expected = @"using System;
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
}";

            TestExtractMethod(code, expected);
        }

        [WorkItem(539859)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)]
        public void LambdaLiftedVariable3()
        {
            var code = @"using System;
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
}";

            var expected = @"using System;
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
}";

            TestExtractMethod(code, expected);
        }

        [WorkItem(539882)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)]
        public void Bug5982()
        {
            var code = @"using System;
using System.Collections.Generic;

class Program
{
    static void Main(string[] args)
    {
        List<int> list = new List<int>();
        Console.WriteLine([|list.Capacity|]);
    }
}";

            var expected = @"using System;
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
}";

            TestExtractMethod(code, expected);
        }

        [WorkItem(539932)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)]
        public void Bug6041()
        {
            var code = @"using System;
class Program
{
    delegate R Del<in T, out R>(T arg);
    public void Foo()
    {
        Del<Exception, ArgumentException> d = (arg) => { return new ArgumentException(); };
        [|d(new ArgumentException());|]
    }
}";

            var expected = @"using System;
class Program
{
    delegate R Del<in T, out R>(T arg);
    public void Foo()
    {
        Del<Exception, ArgumentException> d = (arg) => { return new ArgumentException(); };
        NewMethod(d);
    }

    private static void NewMethod(Del<Exception, ArgumentException> d)
    {
        d(new ArgumentException());
    }
}";

            TestExtractMethod(code, expected, allowMovingDeclaration: false);
        }

        [WorkItem(540183)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)]
        public void ExtractMethod50()
        {
            var code = @"class C
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
}";

            var expected = @"class C
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
}";

            TestExtractMethod(code, expected);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)]
        public void ExtractMethod51()
        {
            var code = @"class C
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
}";

            var expected = @"class C
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
}";

            TestExtractMethod(code, expected);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)]
        public void ExtractMethod52()
        {
            var code = @"class C
{
    void Method()
    {
        [|int i = 1;
        while (false)
        {
           int j = 1;|]
        }
    }
}";

            var expected = @"class C
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
}";

            TestExtractMethod(code, expected);
        }

        [WorkItem(539963)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)]
        public async Task ExtractMethod53()
        {
            var code = @"class Class
{
    void Main()
    {
        Enum e = Enum.[|Field|];
    }
}
enum Enum { }";

            await ExpectExtractMethodToFailAsync(code);
        }

        [WorkItem(539964)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)]
        public async Task ExtractMethod54()
        {
            var code = @"class Class
{
    void Main([|string|][] args)
    {
    }
}";

            await ExpectExtractMethodToFailAsync(code);
        }

        [WorkItem(540072)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)]
        public void Bug6220()
        {
            var code = @"class C
{
    void Main()
    {
[|        float f = 1.2f;
|]        System.Console.WriteLine();
    }
}";

            var expected = @"class C
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
}";
            TestExtractMethod(code, expected);
        }

        [WorkItem(540072)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)]
        public void Bug6220_1()
        {
            var code = @"class C
{
    void Main()
    {
[|        float f = 1.2f; // test
|]        System.Console.WriteLine();
    }
}";

            var expected = @"class C
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
}";
            TestExtractMethod(code, expected);
        }

        [WorkItem(540071)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)]
        public void Bug6219()
        {
            var code = @"class C
{
    void Main()
    {
        float @float = 1.2f;
        [|@float = 1.44F;|]
    }
}";

            var expected = @"class C
{
    void Main()
    {
        float @float = 1.2f;
        NewMethod();
    }

    private static void NewMethod()
    {
        float @float = 1.44F;
    }
}";
            TestExtractMethod(code, expected);
        }

        [WorkItem(540080)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)]
        public void Bug6230()
        {
            var code = @"class C
{
    void M()
    {
        int v =[| /**/1 + 2|];
        System.Console.WriteLine();
    }
}";

            var expected = @"class C
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
}";
            TestExtractMethod(code, expected);
        }

        [WorkItem(540080)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)]
        public void Bug6230_1()
        {
            var code = @"class C
{
    void M()
    {
        int v [|= /**/1 + 2|];
        System.Console.WriteLine();
    }
}";

            var expected = @"class C
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
}";
            TestExtractMethod(code, expected);
        }

        [WorkItem(540052)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)]
        public void Bug6197()
        {
            var code = @"using System;

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
}";

            var expected = @"using System;

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
}";
            TestExtractMethod(code, expected);
        }

        [WorkItem(6277, "DevDiv_Projects/Roslyn")]
        [WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)]
        public void Bug6277()
        {
            var code = @"using System;

class Program
{
    static void Main(string[] args)
    {
        [|int x;
        x = 1;|]
        return;
        int y = x;
    }
}";

            var expected = @"using System;

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
}";
            TestExtractMethod(code, expected);
        }

        [WorkItem(540151)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)]
        public void ArgumentlessReturnWithConstIfExpression()
        {
            var code = @"using System;

class Program
{
    void Test()
    {
        [|if (true)
            return;|]
        Console.WriteLine();
    }
}";

            var expected = @"using System;

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
}";
            TestExtractMethod(code, expected);
        }

        [WorkItem(540151)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)]
        public void ArgumentlessReturnWithConstIfExpression_1()
        {
            var code = @"using System;

class Program
{
    void Test()
    {
        if (true)
            [|if (true)
                return;|]
        Console.WriteLine();
    }
}";

            var expected = @"using System;

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
}";
            TestExtractMethod(code, expected);
        }

        [WorkItem(540151)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)]
        public void ArgumentlessReturnWithConstIfExpression_2()
        {
            var code = @"using System;

class Program
{
    void Test()
    {
        [|if (true)
            return;|]
    }
}";

            var expected = @"using System;

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
}";
            TestExtractMethod(code, expected);
        }

        [WorkItem(540151)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)]
        public void ArgumentlessReturnWithConstIfExpression_3()
        {
            var code = @"using System;

class Program
{
    void Test()
    {
        if (true)
            [|if (true)
                return;|]
    }
}";

            var expected = @"using System;

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
}";
            TestExtractMethod(code, expected);
        }

        [WorkItem(540154)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)]
        public void Bug6313()
        {
            var code = @"using System;

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
}";

            var expected = @"using System;

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
}";
            TestExtractMethod(code, expected);
        }

        [WorkItem(540154)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)]
        public async Task Bug6313_1()
        {
            var code = @"using System;

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
}";

            await ExpectExtractMethodToFailAsync(code);
        }

        [WorkItem(540154)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)]
        public async Task Bug6313_2()
        {
            var code = @"using System;

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
}";

            await ExpectExtractMethodToFailAsync(code);
        }

        [WorkItem(540154)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)]
        public void Bug6313_3()
        {
            var code = @"using System;

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
}";
            var expected = @"using System;

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
}";
            TestExtractMethod(code, expected);
        }

        [WorkItem(540154)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)]
        public void Bug6313_4()
        {
            var code = @"using System;

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
}";
            var expected = @"using System;

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
}";
            TestExtractMethod(code, expected);
        }

        [WorkItem(540154)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)]
        public void Bug6313_5()
        {
            var code = @"using System;

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
}";
            var expected = @"using System;

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
}";
            TestExtractMethod(code, expected);
        }

        [WorkItem(540154)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)]
        public async Task Bug6313_6()
        {
            var code = @"using System;

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
}";
            await ExpectExtractMethodToFailAsync(code);
        }

        [WorkItem(540170)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)]
        public void Bug6333()
        {
            var code = @"using System;

class Program
{
    void Test()
    {
        Program p;
        [|p = new Program()|];
    }
}";
            var expected = @"using System;

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
}";

            TestExtractMethod(code, expected);
        }

        [WorkItem(540216)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)]
        public void Bug6393()
        {
            var code = @"using System;

class Program
{
    object Test<T>()
    {
        T abcd; [|abcd = new T()|];
        return abcd;
    }
}";
            var expected = @"using System;

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
}";

            TestExtractMethod(code, expected);
        }

        [WorkItem(540184)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)]
        public void Bug6351()
        {
            var code = @"class Test
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
}";
            var expected = @"class Test
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
}";

            TestExtractMethod(code, expected);
        }

        [WorkItem(540184)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)]
        public void Bug6351_1()
        {
            var code = @"class Test
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
}";
            var expected = @"class Test
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
}";

            TestExtractMethod(code, expected);
        }

        [WorkItem(540184)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)]
        public void Bug6351_2()
        {
            var code = @"class Test
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
}";
            var expected = @"class Test
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
}";

            TestExtractMethod(code, expected);
        }

        [WorkItem(538229)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)]
        public void Bug3790()
        {
            var code = @"class Test
{
    void method()
    {
        static void Main(string[] args)
        {
            int v = 0;
            for(int i=0 ; i<5; i++)
            {
                [|v = v + i;|]
            }
        }
    }
}";
            var expected = @"class Test
{
    void method()
    {
        static void Main(string[] args)
        {
            int v = 0;
            for(int i=0 ; i<5; i++)
            {
                v = NewMethod(v, i);
            }
        }
    }

    private static int NewMethod(int v, int i)
    {
        v = v + i;
        return v;
    }
}";

            TestExtractMethod(code, expected);
        }

        [WorkItem(538229)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)]
        public void Bug3790_1()
        {
            var code = @"class Test
{
    void method()
    {
        static void Main(string[] args)
        {
            int v = 0;
            for(int i=0 ; i<5; i++)
            {
                [|v = v + i|];
            }
        }
    }
}";
            var expected = @"class Test
{
    void method()
    {
        static void Main(string[] args)
        {
            int v = 0;
            for(int i=0 ; i<5; i++)
            {
                v = NewMethod(v, i);
            }
        }
    }

    private static int NewMethod(int v, int i)
    {
        return v + i;
    }
}";

            TestExtractMethod(code, expected);
        }

        [WorkItem(538229)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)]
        public void Bug3790_2()
        {
            var code = @"class Test
{
    void method()
    {
        static void Main(string[] args)
        {
            int v = 0;
            for(int i=0 ; i<5; i++)
            {
                [|i = v = v + i|];
            }
        }
    }
}";
            var expected = @"class Test
{
    void method()
    {
        static void Main(string[] args)
        {
            int v = 0;
            for(int i=0 ; i<5; i++)
            {
                i = NewMethod(ref v, i);
            }
        }
    }

    private static int NewMethod(ref int v, int i)
    {
        return v = v + i;
    }
}";

            TestExtractMethod(code, expected);
        }

        [WorkItem(540333)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)]
        public void Bug6560()
        {
            var code = @"using System;
class Program
{
    static void Main(string[] args)
    {
        string S = [|null|];
        int Y = S.Length;
    }
}";
            var expected = @"using System;
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
}";

            TestExtractMethod(code, expected);
        }

        [WorkItem(540335)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)]
        public void Bug6562()
        {
            var code = @"using System;
class Program
{
    int y = [|10|];
}";
            var expected = @"using System;
class Program
{
    int y = GetY();

    private static int GetY()
    {
        return 10;
    }
}";

            TestExtractMethod(code, expected);
        }

        [WorkItem(540335)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)]
        public async Task Bug6562_1()
        {
            var code = @"using System;
class Program
{
    const int i = [|10|];
}";
            await ExpectExtractMethodToFailAsync(code);
        }

        [WorkItem(540335)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)]
        public void Bug6562_2()
        {
            var code = @"using System;
class Program
{
    Func<string> f = [|() => ""test""|];
}";
            var expected = @"using System;
class Program
{
    Func<string> f = GetF();

    private static Func<string> GetF()
    {
        return () => ""test"";
    }
}";

            TestExtractMethod(code, expected);
        }

        [WorkItem(540335)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)]
        public void Bug6562_3()
        {
            var code = @"using System;
class Program
{
    Func<string> f = () => [|""test""|];
}";
            var expected = @"using System;
class Program
{
    Func<string> f = () => NewMethod();

    private static string NewMethod()
    {
        return ""test"";
    }
}";

            TestExtractMethod(code, expected);
        }

        [WorkItem(540361)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)]
        public async Task Bug6598()
        {
            var code = @"using System;
using System.Collections.Generic;
using System.Linq;

class 
{
    static void Main(string[] args)
    {
        [|Program|]
    }
}";
            await ExpectExtractMethodToFailAsync(code);
        }

        [WorkItem(540372)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)]
        public void Bug6613()
        {
            var code = @"#define A
using System;

class Program
{
    static void Main(string[] args)
    {
        #if A
            [|Console.Write(5);|]
        #endif 
    }
}";
            var expected = @"#define A
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
}";

            TestExtractMethod(code, expected);
        }

        [WorkItem(540396)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)]
        public async Task InvalidSelection_MethodBody()
        {
            var code = @"using System;

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
}    ";
            await ExpectExtractMethodToFailAsync(code);
        }

        [WorkItem(541586)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)]
        public void StructThis()
        {
            var code = @"struct S
{
    void Foo()
    {
        [|this = new S();|]
    }
}";
            var expected = @"struct S
{
    void Foo()
    {
        NewMethod();
    }

    private void NewMethod()
    {
        this = new S();
    }
}";
            TestExtractMethod(code, expected);
        }

        [WorkItem(541627)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)]
        public void DontUseConvertedTypeForImplicitNumericConversion()
        {
            var code = @"class T
{
    void Foo()
    {
        int x1 = 5;
        long x2 = [|x1|];
    }
}";
            var expected = @"class T
{
    void Foo()
    {
        int x1 = 5;
        long x2 = GetX2(x1);
    }

    private static int GetX2(int x1)
    {
        return x1;
    }
}";
            TestExtractMethod(code, expected);
        }

        [WorkItem(541668)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)]
        public async Task BreakInSelection()
        {
            var code = @"using System;
using System.Collections.Generic;
using System.Linq;
 
class Program
{
    static void Main(string[] args)
    {
        string x1 = ""Hello"";
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
                                x1 = ""t"";
                                string j2 = i1.ToString() + j1.ToString() + x1;
                                break;
                        }
                        break;
                }
                break;|]
        }
    }
}";
            await ExpectExtractMethodToFailAsync(code);
        }

        [WorkItem(541671)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)]
        public void UnreachableCodeWithReturnStatement()
        {
            var code = @"class Program
{
    static void Main(string[] args)
    {
        return;

        [|int i1 = 45;
        i1 = i1 + 10;|]

        return;
    }
}";
            var expected = @"class Program
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
}";
            TestExtractMethod(code, expected);
        }

        [WorkItem(539862)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)]
        public void DontBlindlyPutCapturedVariable1()
        {
            var code = @"using System;
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
}";
            var expected = @"using System;
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
}";
            TestExtractMethod(code, expected);
        }

        [WorkItem(539862)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)]
        public void DontBlindlyPutCapturedVariable2()
        {
            var code = @"using System;
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
}";
            var expected = @"using System;
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
}";
            TestExtractMethod(code, expected);
        }

        [WorkItem(541889)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)]
        public async Task DontCrashOnRangeVariableSymbol()
        {
            var code = @"class Test
{
    public void Linq1()
    {
        int[] numbers = { 5, 4, 1, 3, 9, 8, 6, 7, 2, 0 };
        var lowNums = [|from|] n in numbers where n < 5 select n;
    }
}";
            await ExpectExtractMethodToFailAsync(code);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)]
        public void ExtractRangeVariable()
        {
            var code = @"using System.Linq;
class Test
{
    public void Linq1()
    {
        string[] array = null;
        var q = from string s in array select [|s|];
    }
}";

            var expected = @"using System.Linq;
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
}";
            TestExtractMethod(code, expected);
        }

        [WorkItem(542155)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)]
        public void GenericWithErrorType()
        {
            var code = @"using Foo.Utilities;
class Foo<T>
{
}

class Bar
{
    void gibberish()
    {
        Foo<[|Integer|]> x = null;
        x.IsEmpty();
    }
}

namespace Foo.Utilities
{
    internal static class FooExtensions
    {
        public static bool IsEmpty<T>(this Foo<T> source)
        {
            return false;
        }
    }
}";
            var expected = @"using Foo.Utilities;
class Foo<T>
{
}

class Bar
{
    void gibberish()
    {
        Foo<Integer> x = NewMethod();
        x.IsEmpty();
    }

    private static Foo<Integer> NewMethod()
    {
        return null;
    }
}

namespace Foo.Utilities
{
    internal static class FooExtensions
    {
        public static bool IsEmpty<T>(this Foo<T> source)
        {
            return false;
        }
    }
}";

            TestExtractMethod(code, expected);
        }

        [WorkItem(542105)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)]
        public void NamedArgument()
        {
            var code = @"using System;

class C
{
    int this[int x = 5, int y = 7] { get { return 0; } set { } }

    void Foo()
    {
        var y = this[[|y|]: 1];
    }
}";
            var expected = @"using System;

class C
{
    int this[int x = 5, int y = 7] { get { return 0; } set { } }

    void Foo()
    {
        var y = GetY();
    }

    private int GetY()
    {
        return this[y: 1];
    }
}";

            TestExtractMethod(code, expected);
        }

        [WorkItem(542213)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)]
        public void QueryExpressionVariable()
        {
            var code = @"using System;
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
}";
            var expected = @"using System;
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
}";

            TestExtractMethod(code, expected);
        }

        [WorkItem(542465)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)]
        public void IsExpression()
        {
            var code = @"using System;
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
}";
            var expected = @"using System;
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
}";

            TestExtractMethod(code, expected);
        }

        [WorkItem(542526)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)]
        public void TypeParametersInConstraint()
        {
            var code = @"using System;
using System.Collections.Generic;
 
class A
{
    static void Foo<T, S>(T x) where T : IList<S>
    {
        var y = [|x.Count|];
    }
}";
            var expected = @"using System;
using System.Collections.Generic;
 
class A
{
    static void Foo<T, S>(T x) where T : IList<S>
    {
        var y = GetY<T, S>(x);
    }

    private static int GetY<T, S>(T x) where T : IList<S>
    {
        return x.Count;
    }
}";

            TestExtractMethod(code, expected);
        }

        [WorkItem(542619)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)]
        public void GlobalNamespaceInReturnType()
        {
            var code = @"class Program
{
    class System
    {
        class Action { }
    }
    static global::System.Action a = () => { global::System.Console.WriteLine(); [|}|];
}";
            var expected = @"class Program
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
}";

            TestExtractMethod(code, expected);
        }

        [WorkItem(542582)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)]
        public void ExtractMethodExpandSelectionOnFor()
        {
            var code = @"using System;

class Program
{
    static void Main(string[] args)
    {
        for (int i = 0; i < 10; i++)
        [|{
            Console.WriteLine(i);|]
        }
    }
}";

            var expected = @"using System;

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
}";

            TestExtractMethod(code, expected);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)]
        public void ExtractMethodNotContainerOnFor()
        {
            var code = @"using System;

class Program
{
    static void Main(string[] args)
    {
        for (int i = 0; i < [|10|]; i++) ;
    }
}";

            var expected = @"using System;

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
}";

            TestExtractMethod(code, expected);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)]
        public void ExtractMethodExpandSelectionOnForeach()
        {
            var code = @"using System;

class Program
{
    static void Main(string[] args)
    {
        foreach (var c in ""123"")
        [|{
            Console.Write(c);|]
        }
    }
}";

            var expected = @"using System;

class Program
{
    static void Main(string[] args)
    {
        foreach (var c in ""123"")
            NewMethod(c);
    }

    private static void NewMethod(char c)
    {
        Console.Write(c);
    }
}";

            TestExtractMethod(code, expected);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)]
        public void ExtractMethodNotContainerOnForeach()
        {
            var code = @"using System;

class Program
{
    static void Main(string[] args)
    {
        foreach (var c in [|""123""|])
        {
            Console.Write(c);
        }
    }
}";

            var expected = @"using System;

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
        return ""123"";
    }
}";

            TestExtractMethod(code, expected);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)]
        public void ExtractMethodNotContainerOnElseClause()
        {
            var code = @"using System;

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
}";

            var expected = @"using System;

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
}";

            TestExtractMethod(code, expected);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)]
        public void ExtractMethodExpandSelectionOnLabel()
        {
            var code = @"using System;

class Program
{
    static void Main(string[] args)
    {
    repeat:
        Console.WriteLine(""Roslyn"")[|;|]
        if (true)
            goto repeat;
    }
}";

            var expected = @"using System;

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
        Console.WriteLine(""Roslyn"");
    }
}";

            TestExtractMethod(code, expected);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)]
        public void ExtractMethodNotContainerOnLabel()
        {
            var code = @"using System;

class Program
{
    static void Main(string[] args)
    {
    repeat:
        Console.WriteLine(""Roslyn"");
        if ([|true|])
            goto repeat;
    }
}";

            var expected = @"using System;

class Program
{
    static void Main(string[] args)
    {
    repeat:
        Console.WriteLine(""Roslyn"");
        if (NewMethod())
            goto repeat;
    }

    private static bool NewMethod()
    {
        return true;
    }
}";

            TestExtractMethod(code, expected);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)]
        public void ExtractMethodExpandSelectionOnSwitch()
        {
            var code = @"using System;

class Program
{
    static void Main(string[] args)
    {
        switch (args[0])
        {
            case ""1"": Console.WriteLine(""one"")[|;|] break;
            default: Console.WriteLine(""other""); break;
        }
    }
}";

            var expected = @"using System;

class Program
{
    static void Main(string[] args)
    {
        switch (args[0])
        {
            case ""1"": NewMethod(); break;
            default: Console.WriteLine(""other""); break;
        }
    }

    private static void NewMethod()
    {
        Console.WriteLine(""one"");
    }
}";

            TestExtractMethod(code, expected);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)]
        public void ExtractMethodNotContainerOnSwitch()
        {
            var code = @"using System;

class Program
{
    static void Main(string[] args)
    {
        switch ([|args[0]|])
        {
            case ""1"": Console.WriteLine(""one""); break;
            default: Console.WriteLine(""other""); break;
        }
    }
}";

            var expected = @"using System;

class Program
{
    static void Main(string[] args)
    {
        switch (NewMethod(args))
        {
            case ""1"": Console.WriteLine(""one""); break;
            default: Console.WriteLine(""other""); break;
        }
    }

    private static string NewMethod(string[] args)
    {
        return args[0];
    }
}";

            TestExtractMethod(code, expected);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)]
        public void ExtractMethodExpandSelectionOnDo()
        {
            var code = @"using System;

class Program
{
    static void Main(string[] args)
    {
        do
        [|{
            Console.WriteLine(""I don't like"");|]
        } while (DateTime.Now.DayOfWeek == DayOfWeek.Monday);
    }
}";

            var expected = @"using System;

class Program
{
    static void Main(string[] args)
    {
        do
            NewMethod(); while (DateTime.Now.DayOfWeek == DayOfWeek.Monday);
    }

    private static void NewMethod()
    {
        Console.WriteLine(""I don't like"");
    }
}";

            TestExtractMethod(code, expected);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)]
        public void ExtractMethodNotContainerOnDo()
        {
            var code = @"using System;

class Program
{
    static void Main(string[] args)
    {
        do
        {
            Console.WriteLine(""I don't like"");
        } while ([|DateTime.Now.DayOfWeek == DayOfWeek.Monday|]);
    }
}";

            var expected = @"using System;

class Program
{
    static void Main(string[] args)
    {
        do
        {
            Console.WriteLine(""I don't like"");
        } while (NewMethod());
    }

    private static bool NewMethod()
    {
        return DateTime.Now.DayOfWeek == DayOfWeek.Monday;
    }
}";

            TestExtractMethod(code, expected);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)]
        public void ExtractMethodExpandSelectionOnWhile()
        {
            var code = @"using System;

class Program
{
    static void Main(string[] args)
    {
        while (true)
        [|{
            ;|]
        }
    }
}";

            var expected = @"using System;

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
}";

            TestExtractMethod(code, expected);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)]
        public void ExtractMethodExpandSelectionOnStruct()
        {
            var code = @"using System;

struct Foo
{
    static Action a = () => { Console.WriteLine(); [|}|];
}";

            var expected = @"using System;

struct Foo
{
    static Action a = GetA();

    private static Action GetA()
    {
        return () => { Console.WriteLine(); };
    }
}";

            TestExtractMethod(code, expected);
        }

        [WorkItem(542619)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)]
        public void ExtractMethodIncludeGlobal()
        {
            var code = @"class Program
{
    class System
    {
        class Action { }
    }
    static global::System.Action a = () => { global::System.Console.WriteLine(); [|}|];
    static void Main(string[] args)
    {
    }
}";

            var expected = @"class Program
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
}";

            TestExtractMethod(code, expected);
        }

        [WorkItem(542582)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)]
        public void ExtractMethodExpandSelection()
        {
            var code = @"class Program
{
    static void Main(string[] args)
    {
        for (int i = 0; i < 10; i++)
        [|{
            System.Console.WriteLine(i);|]
        }
    }
}";

            var expected = @"class Program
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
}";

            TestExtractMethod(code, expected);
        }

        [WorkItem(542594)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)]
        public void ExtractMethodRename1()
        {
            var code = @"class Program
{
    static void Main()
    {
        [|var i = 42;|]
        var j = 42;
    }
    private static void NewMethod() { }
    private static void NewMethod2() { }
}";

            var expected = @"class Program
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
}";

            TestExtractMethod(code, expected);
        }

        [WorkItem(542594)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)]
        public void ExtractMethodRename2()
        {
            var code = @"class Program
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
}";

            var expected = @"class Program
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
}";

            TestExtractMethod(code, expected);
        }

        [WorkItem(542632)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)]
        public void ExtractMethodInInteractive1()
        {
            var code = @"int i; [|i = 2|]; i = 3;";
            var expected = @"int i; i = NewMethod();

int NewMethod()
{
    return 2;
}

i = 3;";
            TestExtractMethod(code, expected, parseOptions: Options.Script);
        }

        [WorkItem(542670)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)]
        public void TypeParametersInConstraint1()
        {
            var code = @"using System;
using System.Collections.Generic;
 
class A
{
    static void Foo<T, S, U>(T x) where T : IList<S> where S : IList<U>
    {
        var y = [|x.Count|];
    }
}";
            var expected = @"using System;
using System.Collections.Generic;
 
class A
{
    static void Foo<T, S, U>(T x) where T : IList<S> where S : IList<U>
    {
        var y = GetY<T, S>(x);
    }

    private static int GetY<T, S>(T x) where T : IList<S>
    {
        return x.Count;
    }
}";

            TestExtractMethod(code, expected);
        }

        [WorkItem(706894)]
        [WorkItem(543012)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)]
        public void TypeParametersInConstraint2()
        {
            var code = @"
using System;

interface I<T> where T : IComparable<T>
{
    int Count { get; }
}

class A
{
    static void Foo<T, S>(S x) where S : I<T> where T : IComparable<T>
    {
        var y = [|x.Count|];
    }
}
";
            var expected = @"
using System;

interface I<T> where T : IComparable<T>
{
    int Count { get; }
}

class A
{
    static void Foo<T, S>(S x) where S : I<T> where T : IComparable<T>
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
";

            TestExtractMethod(code, expected);
        }

        [WorkItem(706894)]
        [WorkItem(543012)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)]
        public void TypeParametersInConstraint3()
        {
            var code = @"
using System;

interface I<T> where T : class
{
    int Count { get; }
}

class A
{
    static void Foo<T, S>(S x) where S : I<T> where T : class
    {
        var y = [|x.Count|];
    }
}
";
            var expected = @"
using System;

interface I<T> where T : class
{
    int Count { get; }
}

class A
{
    static void Foo<T, S>(S x) where S : I<T> where T : class
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
";

            TestExtractMethod(code, expected);
        }

        [WorkItem(543012)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)]
        public void TypeParametersInConstraint4()
        {
            var code = @"
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
    public virtual void Foo<T, S>(S x) where S : IList<I2<IEnumerable<T>, T>> where T : struct
    {
    }
}

class B : A
{
    public override void Foo<T, S>(S x)
    {
        var y = [|x.Count|];
    }
}
";
            var expected = @"
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
    public virtual void Foo<T, S>(S x) where S : IList<I2<IEnumerable<T>, T>> where T : struct
    {
    }
}

class B : A
{
    public override void Foo<T, S>(S x)
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
";

            TestExtractMethod(code, expected);
        }

        [WorkItem(543012)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)]
        public void TypeParametersInConstraintBestEffort()
        {
            var code = @"
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
";
            var expected = @"
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
";

            TestExtractMethod(code, expected);
        }

        [WorkItem(542672)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)]
        public void ConstructedTypes()
        {
            var code = @"using System;
using System.Collections.Generic;
 
class Program
{
    static void Foo<T>()
    {
        List<T> x = new List<T>();
        Action a = () => Console.WriteLine([|x.Count|]);
    }
}";
            var expected = @"using System;
using System.Collections.Generic;
 
class Program
{
    static void Foo<T>()
    {
        List<T> x = new List<T>();
        Action a = () => Console.WriteLine(GetCount(x));
    }

    private static int GetCount<T>(List<T> x)
    {
        return x.Count;
    }
}";

            TestExtractMethod(code, expected);
        }

        [WorkItem(542792)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)]
        public void TypeInDefault()
        {
            var code = @"using System;
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
}";
            var expected = @"using System;
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
}";

            TestExtractMethod(code, expected);
        }

        [WorkItem(542708)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)]
        public void Script_ArgumentException()
        {
            var code = @"using System;
public static void GetNonVirtualMethod<TDelegate>( Type type, string name)
{
    Type delegateType = typeof(TDelegate);
     var invoke = [|delegateType|].GetMethod(""Invoke"");
}";
            var expected = @"using System;
public static void GetNonVirtualMethod<TDelegate>( Type type, string name)
{
    Type delegateType = typeof(TDelegate);
    var invoke = GetDelegateType(delegateType).GetMethod(""Invoke"");
}

Type GetDelegateType(Type delegateType)
{
    return delegateType;
}";

            TestExtractMethod(code, expected, parseOptions: Options.Script);
        }

        [WorkItem(529008)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)]
        public void ReadOutSideIsUnReachable()
        {
            var code = @"class Test
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
}";
            var expected = @"class Test
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
}";

            TestExtractMethod(code, expected, allowMovingDeclaration: false);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)]
        [WorkItem(543186)]
        public void AnonymousTypePropertyName()
        {
            var code = @"class C
{
    void M()
    {
        var x = new { [|String|] = true };
    }
}";
            var expected = @"class C
{
    void M()
    {
        NewMethod();
    }

    private static void NewMethod()
    {
        var x = new { String = true };
    }
}";
            TestExtractMethod(code, expected);
        }

        [WorkItem(543662)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)]
        public void ArgumentOfBaseConstrInit()
        {
            var code = @"class O
{
    public O(int t) : base([|t|])
    {
    }
}";
            var expected = @"class O
{
    public O(int t) : base(GetT(t))
    {
    }

    private static int GetT(int t)
    {
        return t;
    }
}";

            TestExtractMethod(code, expected);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)]
        public void UnsafeType()
        {
            var code = @"
unsafe class O
{
    unsafe public O(int t)
    {
        [|t = 1;|]
    }
}";
            var expected = @"
unsafe class O
{
    unsafe public O(int t)
    {
        NewMethod();
    }

    private static void NewMethod()
    {
        int t = 1;
    }
}";

            TestExtractMethod(code, expected);
        }

        [WorkItem(544144)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)]
        public void CastExpressionWithImplicitUserDefinedConversion()
        {
            var code = @"
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
}";
            var expected = @"
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
}";

            TestExtractMethod(code, expected);
        }

        [WorkItem(544387)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)]
        public void FixedPointerVariable()
        {
            var code = @"
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
}";
            var expected = @"
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
}";

            TestExtractMethod(code, expected);
        }

        [WorkItem(544444)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)]
        public void PointerDeclarationStatement()
        {
            var code = @"
class Program
{
    unsafe static void Main()
    {
        int* p1 = null;
        [|int* p2 = p1;|]
    }
}";
            var expected = @"
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
}";

            TestExtractMethod(code, expected);
        }

        [WorkItem(544446)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)]
        public void PrecededByCastExpr()
        {
            var code = @"
class Program
{
    static void Main()
    {
        int i1 = (int)[|5L|];
    }
}";
            var expected = @"
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
}";

            TestExtractMethod(code, expected);
        }

        [WorkItem(542944)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)]
        public void ExpressionWithLocalConst()
        {
            var code = @"class Program
{
    static void Main(string[] args)
    {
        const string a = null;
        [|a = null;|]
    }
}";
            var expected = @"class Program
{
    static void Main(string[] args)
    {
        const string a = null;
        NewMethod(a);
    }

    private static void NewMethod(string a)
    {
        a = null;
    }
}";

            TestExtractMethod(code, expected, allowMovingDeclaration: true);
        }

        [WorkItem(542944)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)]
        public void ExpressionWithLocalConst2()
        {
            var code = @"class Program
{
    static void Main(string[] args)
    {
        const string a = null;
        [|a = null;|]
    }
}";
            var expected = @"class Program
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
}";

            TestExtractMethod(code, expected, allowMovingDeclaration: false);
        }

        [WorkItem(544675)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)]
        public async Task HiddenPosition()
        {
            var code = @"class Program
{
    static void Main(string[] args)
    {
        const string a = null;
        [|a = null;|]
    }
#line default
#line hidden
}";

            await ExpectExtractMethodToFailAsync(code);
        }

        [WorkItem(530609)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)]
        public void NoCrashInteractive()
        {
            var code = @"[|if (true)
{
}|]";
            var expected = @"NewMethod();

void NewMethod()
{
    if (true)
    {
    }
}";

            TestExtractMethod(code, expected, parseOptions: new CSharpParseOptions(kind: SourceCodeKind.Script));
        }

        [WorkItem(530322)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)]
        public void ExtractMethodShouldNotBreakFormatting()
        {
            var code =
@"class C
{
    void M(int i, int j, int k)
    {
        M(0,
          [|1|],
          2);
    }
}";
            var expected = @"class C
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
}";

            TestExtractMethod(code, expected);
        }

        [WorkItem(604389)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)]
        public void TestExtractLiteralExpression()
        {
            var code =
@"class Program
{
    static void Main()
    {
        var c = new C { X = { Y = { [|1|] } } };
    }
}
 
class C
{
    public dynamic X;
}";
            var expected = @"class Program
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
}";

            TestExtractMethod(code, expected);
        }

        [WorkItem(604389)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)]
        public void TestExtractCollectionInitializer()
        {
            var code =
@"class Program
{
    static void Main()
    {
        var c = new C { X = { Y = [|{ 1 }|] } };
    }
}
 
class C
{
    public dynamic X;
}";
            var expected = @"class Program
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
}";

            TestExtractMethod(code, expected);
        }

        [WorkItem(854662)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)]
        public void TestExtractCollectionInitializer2()
        {
            var code =
@"using System;
using System.Collections.Generic;
class Program
{
    public Dictionary<int, int> A { get; private set; }
    static int Main(string[] args)
    {
        int a = 0;
        return new Program { A = { { [|a + 2|], 0 } } }.A.Count;
    }
}";
            var expected = @"using System;
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
}";

            TestExtractMethod(code, expected);
        }

        [WorkItem(530267)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)]
        public void TestCoClassImplicitConversion()
        {
            var code =
@"using System;
using System.Runtime.InteropServices;
 
[CoClass(typeof(C))]
[ComImport]
[Guid(""8D3A7A55-A8F5-4669-A5AD-996A3EB8F2ED"")]
interface I { }
 
class C : I
{
    static void Main()
    {
        [|new I()|]; // Extract Method
    }
}";
            var expected = @"using System;
using System.Runtime.InteropServices;
 
[CoClass(typeof(C))]
[ComImport]
[Guid(""8D3A7A55-A8F5-4669-A5AD-996A3EB8F2ED"")]
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
}";

            TestExtractMethod(code, expected);
        }

        [WorkItem(530710)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)]
        public void TestOverloadResolution()
        {
            var code =
@"using System;
 
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
}";
            var expected = @"using System;
 
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
}";

            TestExtractMethod(code, expected);
        }

        [WorkItem(530710)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)]
        public void TestOverloadResolution1()
        {
            var code =
@"using System;
 
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
}";
            var expected = @"using System;
 
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
}";

            TestExtractMethod(code, expected);
        }

        [WorkItem(530710)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)]
        public void TestOverloadResolution2()
        {
            var code =
@"using System;
 
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
}";
            var expected = @"using System;
 
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
}";

            TestExtractMethod(code, expected);
        }

        [WorkItem(731924)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)]
        public void TestTreatEnumSpecial()
        {
            var code =
@"using System;

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
}";
            var expected = @"using System;

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
}";

            TestExtractMethod(code, expected);
        }

        [WorkItem(756222)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)]
        public void TestReturnStatementInAsyncMethod()
        {
            var code =
@"using System.Threading.Tasks;
 
class C
{
    async Task<int> M()
    {
        await Task.Yield();
        [|return 3;|]
    }
}";
            var expected = @"using System.Threading.Tasks;
 
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
}";

            TestExtractMethod(code, expected);
        }

        [WorkItem(574576)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)]
        public async Task TestAsyncMethodWithRefOrOutParameters()
        {
            var code =
@"using System.Threading.Tasks;
 
class C
{
    public async void Foo()
    {
        [|var q = 1;
        var p = 2;
        await Task.Yield();|]
        var r = q;
        var s = p;
    }
}";

            await ExpectExtractMethodToFailAsync(code);
        }

        [WorkItem(1025272)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)]
        public async Task TestAsyncMethodWithWellKnownValueType()
        {
            var code =
@"using System;
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
}";
            var expected = @"using System;
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
}";
            await ExpectExtractMethodToFailAsync(code, expected);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)]
        public async Task TestAsyncMethodWithWellKnownValueType1()
        {
            var code =
@"using System;
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
}";
            await ExpectExtractMethodToFailAsync(code, allowMovingDeclaration: false);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)]
        public async Task TestDontPutOutOrRefForStructOff()
        {
            var code =
@"using System.Threading.Tasks;

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
}";
            await ExpectExtractMethodToFailAsync(code, dontPutOutOrRefOnStruct: false);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)]
        public void TestDontPutOutOrRefForStructOn()
        {
            var code =
@"using System.Threading.Tasks;

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
}";
            var expected =
@"using System.Threading.Tasks;

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
}";

            TestExtractMethod(code, expected, dontPutOutOrRefOnStruct: true);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)]
        public void ExtractMethod_Argument1()
        {
            var service = new CSharpExtractMethodService();
            Assert.NotNull(Record.Exception(() =>
            {
                var tree = service.ExtractMethodAsync(null, default(TextSpan), null, CancellationToken.None).Result;
            }));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)]
        public void ExtractMethod_Argument2()
        {
            var solution = new AdhocWorkspace().CurrentSolution;
            var projectId = ProjectId.CreateNewId();
            var project = solution.AddProject(projectId, "Project", "Project.dll", LanguageNames.CSharp).GetProject(projectId);

            var document = project.AddMetadataReference(TestReferences.NetFx.v4_0_30319.mscorlib)
                                  .AddDocument("Document", SourceText.From(""));

            var service = new CSharpExtractMethodService() as IExtractMethodService;

            service.ExtractMethodAsync(document, default(TextSpan)).Wait();
        }

        [WpfFact]
        [Trait(Traits.Feature, Traits.Features.ExtractMethod)]
        [Trait(Traits.Feature, Traits.Features.Interactive)]
        public void ExtractMethodCommandDisabledInSubmission()
        {
            var exportProvider = MinimalTestExportProvider.CreateExportProvider(
                TestExportProvider.EntireAssemblyCatalogWithCSharpAndVisualBasic.WithParts(typeof(InteractiveDocumentSupportsFeatureService)));

            using (var workspace = TestWorkspaceFactory.CreateWorkspace(XElement.Parse(@"
                <Workspace>
                    <Submission Language=""C#"" CommonReferences=""true"">  
                        typeof(string).$$Name
                    </Submission>
                </Workspace> "),
                workspaceKind: WorkspaceKind.Interactive,
                exportProvider: exportProvider))
            {
                // Force initialization.
                workspace.GetOpenDocumentIds().Select(id => workspace.GetTestDocument(id).GetTextView()).ToList();

                var textView = workspace.Documents.Single().GetTextView();

                var handler = new ExtractMethodCommandHandler(
                    workspace.GetService<ITextBufferUndoManagerProvider>(),
                    workspace.GetService<IEditorOperationsFactoryService>(),
                    workspace.GetService<IInlineRenameService>(),
                    workspace.GetService<Host.IWaitIndicator>());
                var delegatedToNext = false;
                Func<CommandState> nextHandler = () =>
                {
                    delegatedToNext = true;
                    return CommandState.Unavailable;
                };

                var state = handler.GetCommandState(new Commands.ExtractMethodCommandArgs(textView, textView.TextBuffer), nextHandler);
                Assert.True(delegatedToNext);
                Assert.False(state.IsAvailable);
            }
        }
    }
}
