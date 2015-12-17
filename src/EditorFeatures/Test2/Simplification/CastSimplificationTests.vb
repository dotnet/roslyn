' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading.Tasks

Namespace Microsoft.CodeAnalysis.Editor.UnitTests.Simplification
    Public Class CastSimplificationTests
        Inherits AbstractSimplificationTests

#Region "CSharp tests"

        <WpfFact, Trait(Traits.Feature, Traits.Features.Simplification)>
        Public Async Function TestCSharp_Remove_IntToInt() As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
class C
{
    void M()
    {
        int x = {|Simplify:(int)0|};
    }
}
        </Document>
    </Project>
</Workspace>

            Dim expected =
<code>
class C
{
    void M()
    {
        int x = 0;
    }
}
</code>

            Await TestAsync(input, expected)

        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Simplification)>
        Public Async Function TestCSharp_Remove_ByteToInt() As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
class C
{
    void M()
    {
        int x = {|Simplify:(byte)0|};
    }
}
        </Document>
    </Project>
</Workspace>

            Dim expected =
<code>
class C
{
    void M()
    {
        int x = 0;
    }
}
</code>

            Await TestAsync(input, expected)

        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Simplification)>
        Public Async Function TestCSharp_Remove_ByteToVar() As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
class C
{
    void M()
    {
        var x = {|Simplify:(byte)0|};
    }
}
        </Document>
    </Project>
</Workspace>

            Dim expected =
<code>
class C
{
    void M()
    {
        var x = (byte)0;
    }
}
</code>

            Await TestAsync(input, expected)

        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Simplification)>
        Public Async Function TestCSharp_DoNotRemove_UncheckedByteToInt() As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
class C
{
    void M()
    {
        int x = unchecked({|Simplify:(byte)257|});
    }
}
        </Document>
    </Project>
</Workspace>

            Dim expected =
<code>
class C
{
    void M()
    {
        int x = unchecked((byte)257);
    }
}
</code>

            Await TestAsync(input, expected)

        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Simplification)>
        Public Async Function TestCSharp_DoNotRemove_UncheckedByteToVar() As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
class C
{
    void M()
    {
        var x = unchecked({|Simplify:(byte)257|});
    }
}
        </Document>
    </Project>
</Workspace>

            Dim expected =
<code>
class C
{
    void M()
    {
        var x = unchecked((byte)257);
    }
}
</code>

            Await TestAsync(input, expected)

        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Simplification)>
        Public Async Function TestCSharp_DoNotRemove_UncheckedByteToIntToVar() As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
class C
{
    void M()
    {
        var x = int(unchecked({|Simplify:(byte)257|}));
    }
}
        </Document>
    </Project>
</Workspace>

            Dim expected =
<code>
class C
{
    void M()
    {
        var x = int(unchecked((byte)257));
    }
}
</code>

            Await TestAsync(input, expected)

        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Simplification)>
        Public Async Function TestCSharp_Remove_IntToObjectInInvocation() As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
class C
{
    void Foo(object o) { }

    void M()
    {
        int x = Foo({|Simplify:(object)1|});
    }
}
        </Document>
    </Project>
</Workspace>

            Dim expected =
<code>
class C
{
    void Foo(object o) { }

    void M()
    {
        int x = Foo(1);
    }
}
</code>

            Await TestAsync(input, expected)

        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Simplification)>
        Public Async Function TestCSharp_DoNotRemove_IntToObject_Overloads1() As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
class C
{
    void Foo(object o) { }
    void Foo(int i) { }

    void M()
    {
        int x = Foo({|Simplify:(object)1|});
    }
}
        </Document>
    </Project>
</Workspace>

            Dim expected =
<code>
class C
{
    void Foo(object o) { }
    void Foo(int i) { }

    void M()
    {
        int x = Foo((object)1);
    }
}
</code>

            Await TestAsync(input, expected)

        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Simplification)>
        Public Async Function TestCSharp_DoNotRemove_IntToObject_Overloads2() As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
class C
{
    void Foo(object o) { }
    void Foo(int i) { }

    void M()
    {
        int x = Foo({|Simplify:(object)(1 + 2)|});
    }
}
        </Document>
    </Project>
</Workspace>

            Dim expected =
<code>
class C
{
    void Foo(object o) { }
    void Foo(int i) { }

    void M()
    {
        int x = Foo((object)(1 + 2));
    }
}
</code>

            Await TestAsync(input, expected)

        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Simplification)>
        Public Async Function TestCSharp_DoNotRemove_IntToDouble1() As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
class C
{
    void M()
    {
        string s = ({|Simplify:(double)3|}).ToString();
    }
}
        </Document>
    </Project>
</Workspace>

            Dim expected =
<code>
class C
{
    void M()
    {
        string s = ((double)3).ToString();
    }
}
</code>

            Await TestAsync(input, expected)

        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Simplification)>
        Public Async Function TestCSharp_Remove_LambdaToDelegateType1() As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
class C
{
    void M()
    {
        System.Action a = {|Simplify:(System.Action)(() => { })|};
    }
}
        </Document>
    </Project>
</Workspace>

            Dim expected =
<code>
class C
{
    void M()
    {
        System.Action a = (() => { });
    }
}
</code>

            Await TestAsync(input, expected)

        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Simplification)>
        Public Async Function TestCSharp_Remove_LambdaToDelegateType2() As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
class C
{
    void M()
    {
        System.Action a = null;
        a = {|Simplify:(System.Action)(() => { })|};
    }
}
        </Document>
    </Project>
</Workspace>

            Dim expected =
<code>
class C
{
    void M()
    {
        System.Action a = null;
        a = (() => { });
    }
}
</code>

            Await TestAsync(input, expected)

        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Simplification)>
        Public Async Function TestCSharp_Remove_LambdaToDelegateTypeInInvocation1() As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
class C
{
    void M()
    {
        Foo({|Simplify:(System.Func&lt;string&gt;)(() => "Foo")|});
    }

    void Foo&lt;T&gt;(System.Func&lt;T&gt; f) { }
}
        </Document>
    </Project>
</Workspace>

            Dim expected =
<code>
class C
{
    void M()
    {
        Foo((() => "Foo"));
    }

    void Foo&lt;T&gt;(System.Func&lt;T&gt; f) { }
}
</code>

            Await TestAsync(input, expected)

        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Simplification)>
        Public Async Function TestCSharp_Remove_LambdaToDelegateTypeInInvocation2() As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
class C
{
    void M()
    {
        Foo(f: {|Simplify:(System.Func&lt;string&gt;)(() => "Foo")|});
    }

    void Foo&lt;T&gt;(System.Func&lt;T&gt; f) { }
}
        </Document>
    </Project>
</Workspace>

            Dim expected =
<code>
class C
{
    void M()
    {
        Foo(f: (() => "Foo"));
    }

    void Foo&lt;T&gt;(System.Func&lt;T&gt; f) { }
}
</code>

            Await TestAsync(input, expected)

        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Simplification)>
        Public Async Function TestCSharp_DoNotRemove_LambdaToDelegateTypeWithVar() As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
class C
{
    void M()
    {
        var a = {|Simplify:(System.Action)(() => { })|};
    }
}
        </Document>
    </Project>
</Workspace>

            Dim expected =
<code>
class C
{
    void M()
    {
        var a = (System.Action)(() => { });
    }
}
</code>

            Await TestAsync(input, expected)

        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Simplification)>
        Public Async Function TestCSharp_DoNotRemove_LambdaToDelegateTypeWhenInvoked() As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
class C
{
    void M()
    {
        var x = ({|Simplify:(System.Func&lt;int&gt;)(() => 1)|})();
    }
}
        </Document>
    </Project>
</Workspace>

            Dim expected =
<code>
class C
{
    void M()
    {
        var x = ((System.Func&lt;int&gt;)(() => 1))();
    }
}
</code>

            Await TestAsync(input, expected)

        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Simplification)>
        Public Async Function TestCSharp_DoNotRemove_MethodGroupToDelegateTypeWhenInvoked() As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
class C
{
    void M(object o)
    {
        ({|Simplify:(Action&lt;object&gt;)Main|})(null);
    }
}
        </Document>
    </Project>
</Workspace>

            Dim expected =
<code>
class C
{
    void M(object o)
    {
        ((Action&lt;object&gt;)Main)(null);
    }
}
</code>

            Await TestAsync(input, expected)

        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Simplification)>
        Public Async Function TestCSharp_DoNotRemove_AnonymousFunctionToDelegateTypeInNullCoalescingExpression() As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
class C
{
    void M()
    {
        System.Action y = {|Simplify:(System.Action)delegate { }|} ?? null;
    }
}
        </Document>
    </Project>
</Workspace>

            Dim expected =
<code>
class C
{
    void M()
    {
        System.Action y = (System.Action)delegate { } ?? null;
    }
}
</code>

            Await TestAsync(input, expected)

        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Simplification)>
        Public Async Function TestCSharp_Remove_MethodGroupToDelegateTypeInDelegateCombineExpression1() As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
class C
{
    void M()
    {
        System.Action&lt;string&gt; g = null;
        var h = {|Simplify:(System.Action&lt;string&gt;)(Foo&lt;string&gt;)|} + g;
    }

    static void Foo&lt;T&gt;(T y) { }
}
        </Document>
    </Project>
</Workspace>

            Dim expected =
<code>
class C
{
    void M()
    {
        System.Action&lt;string&gt; g = null;
        var h = (Foo&lt;string&gt;) + g;
    }

    static void Foo&lt;T&gt;(T y) { }
}
</code>

            Await TestAsync(input, expected)

        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Simplification)>
        Public Async Function TestCSharp_Remove_MethodGroupToDelegateTypeInDelegateCombineExpression2() As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
class C
{
    void M()
    {
        System.Action&lt;string&gt; g = null;
        var h = ({|Simplify:(System.Action&lt;string&gt;)Foo&lt;string&gt;|}) + g;
    }

    static void Foo&lt;T&gt;(T y) { }
}
        </Document>
    </Project>
</Workspace>

            Dim expected =
<code>
class C
{
    void M()
    {
        System.Action&lt;string&gt; g = null;
        var h = (Foo&lt;string&gt;) + g;
    }

    static void Foo&lt;T&gt;(T y) { }
}
</code>

            Await TestAsync(input, expected)

        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Simplification)>
        Public Async Function TestCSharp_DoNotRemove_NullLiteralToStringInInvocation() As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
using System;
class C
{
    void M()
    {
        Console.WriteLine({|Simplify:(string)null|});
    }
}
        </Document>
    </Project>
</Workspace>

            Dim expected =
<code>
using System;
class C
{
    void M()
    {
        Console.WriteLine((string)null);
    }
}
</code>

            Await TestAsync(input, expected)

        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Simplification)>
        <WorkItem(529816)>
        Public Async Function TestCSharp_DoNotRemove_QuerySelectMethodChanges() As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document><![CDATA[
using System;

class A
{
    int Select(Func<int, long> x) { return 1; }
    int Select(Func<int, int> x) { return 2; }

    static void Main()
    {
        Console.WriteLine(from y in new A() select {|Simplify:(long)0|});
    }
}]]>
        </Document>
    </Project>
</Workspace>

            Dim expected =
<code><![CDATA[
using System;

class A
{
    int Select(Func<int, long> x) { return 1; }
    int Select(Func<int, int> x) { return 2; }

    static void Main()
    {
        Console.WriteLine(from y in new A() select (long)0);
    }
}]]>
</code>

            Await TestAsync(input, expected)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Simplification)>
        <WorkItem(529816)>
        Public Async Function TestCSharp_DoNotRemove_QueryOrderingMethodChanges() As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document><![CDATA[
using System;
using System.Linq;

public class A
{
    IOrderedEnumerable<int> OrderByDescending(Func<int, long> keySelector) { Console.WriteLine("long"); return null; }
    IOrderedEnumerable<int> OrderByDescending(Func<int, int> keySelector) { Console.WriteLine("int"); return null; }
    IOrderedEnumerable<int> OrderByDescending(Func<int, object> keySelector) { Console.WriteLine("object"); return null; }

    public static void Main()
    {
        var q1 =
            from x in new A()
            orderby
                {|Simplify:(long)x descending|},
                {|Simplify:(string)x.ToString() ascending|},
                {|Simplify:(int)x descending|}
            select x;
    }
}]]>
        </Document>
    </Project>
</Workspace>

            Dim expected =
<code><![CDATA[
using System;
using System.Linq;

public class A
{
    IOrderedEnumerable<int> OrderByDescending(Func<int, long> keySelector) { Console.WriteLine("long"); return null; }
    IOrderedEnumerable<int> OrderByDescending(Func<int, int> keySelector) { Console.WriteLine("int"); return null; }
    IOrderedEnumerable<int> OrderByDescending(Func<int, object> keySelector) { Console.WriteLine("object"); return null; }

    public static void Main()
    {
        var q1 =
            from x in new A()
            orderby
                (long)x descending,
                x.ToString() ascending,
                x descending
            select x;
    }
}]]>
</code>

            Await TestAsync(input, expected)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Simplification)>
        <WorkItem(529816)>
        Public Async Function TestCSharp_DoNotRemove_QueryClauseChanges() As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document><![CDATA[
using System;
using System.Linq;
using System.Collections.Generic;

class A
{
    A Select(Func<int, long> x) { return this; }
    A Select(Func<int, int> x) { return this; }

    static void Main()
    {
        var qie = from x3 in new int[] { 0 }
                  join x7 in (new int[] { 1 }) on 5 equals {|Simplify:(long)5|} into x8
                  select x8;
    }
}]]>
        </Document>
    </Project>
</Workspace>

            Dim expected =
<code><![CDATA[
using System;
using System.Linq;
using System.Collections.Generic;

class A
{
    A Select(Func<int, long> x) { return this; }
    A Select(Func<int, int> x) { return this; }

    static void Main()
    {
        var qie = from x3 in new int[] { 0 }
                  join x7 in (new int[] { 1 }) on 5 equals (long)5 into x8
                  select x8;
    }
}]]>
</code>

            Await TestAsync(input, expected)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Simplification)>
        <WorkItem(529842)>
        Public Async Function TestCSharp_DoNotRemove_CastInTernary() As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document><![CDATA[
using System;
 
class X
{
    public static implicit operator string (X x)
    {
        return x.ToString();
    }
 
    static void Main()
    {
        bool b = true;
        X x = new X();
        Console.WriteLine(b ? {|Simplify:(string)null|} : x);
    }
}
]]>
        </Document>
    </Project>
</Workspace>

            Dim expected =
<code><![CDATA[
using System;
 
class X
{
    public static implicit operator string (X x)
    {
        return x.ToString();
    }
 
    static void Main()
    {
        bool b = true;
        X x = new X();
        Console.WriteLine(b ? (string)null : x);
    }
}
]]>
</code>

            Await TestAsync(input, expected)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Simplification)>
        <WorkItem(529855)>
        Public Async Function TestCSharp_Remove_CastInIsExpression() As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document><![CDATA[
using System.Collections;

static class A
{
    static void Foo(IEnumerable x)
    {
        if ({|Simplify:(object)x|} is string)
        {
        }
    }
}
]]>
        </Document>
    </Project>
</Workspace>

            Dim expected =
<code><![CDATA[
using System.Collections;

static class A
{
    static void Foo(IEnumerable x)
    {
        if (x is string)
        {
        }
    }
}
]]>
</code>

            Await TestAsync(input, expected)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Simplification)>
        <WorkItem(529843)>
        Public Async Function TestCSharp_Remove_CastToObjectTypeInReferenceComparison() As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document><![CDATA[
class Program
{
    static void Foo<T, S>(T x, S y)
         where T : class
         where S : class
    {
        if (x == {|Simplify:(object)y|}) { }
    }
}
]]>
        </Document>
    </Project>
</Workspace>

            Dim expected =
<code><![CDATA[
class Program
{
    static void Foo<T, S>(T x, S y)
         where T : class
         where S : class
    {
        if (x == y) { }
    }
}
]]>
</code>

            Await TestAsync(input, expected)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Simplification)>
        <WorkItem(529914)>
        Public Async Function TestCSharp_Remove_TypeParameterToEffectiveBaseType() As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document><![CDATA[
using System;
class Program
{
    static void Foo<T, S>(T x, S y)
         where T : Exception
         where S : Exception
    {
        if (x == {|Simplify:(Exception)y|}) { }
    }
}
]]>
        </Document>
    </Project>
</Workspace>

            Dim expected =
<code><![CDATA[
using System;
class Program
{
    static void Foo<T, S>(T x, S y)
         where T : Exception
         where S : Exception
    {
        if (x == y) { }
    }
}
]]>
</code>

            Await TestAsync(input, expected)
        End Function

        <WpfFact(), Trait(Traits.Feature, Traits.Features.Simplification)>
        <WorkItem(529917)>
        Public Async Function TestCSharp_Remove_NullableTypeToInterfaceTypeInNullComparison() As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document><![CDATA[
using System;

class Program
{
    static void Main()
    {
        int? x = null;
        var y = {|Simplify:(IComparable)x|} == null;
    }
}
]]>
        </Document>
    </Project>
</Workspace>

            Dim expected =
<code><![CDATA[
using System;

class Program
{
    static void Main()
    {
        int? x = null;
        var y = x == null;
    }
}
]]>
</code>

            Await TestAsync(input, expected)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Simplification)>
        <WorkItem(530745)>
        Public Async Function TestCSharp_DoNotRemove_RequiredExplicitNullableCast1() As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document><![CDATA[
using System;
 
class Program
{
    static void Main()
    {
        var x = {|Simplify:(long?){|Simplify:(int?)long.MaxValue|}|};
        Console.WriteLine(x);
    }
}
]]>
        </Document>
    </Project>
</Workspace>

            Dim expected =
<code><![CDATA[
using System;
 
class Program
{
    static void Main()
    {
        var x = (long?)(int?)long.MaxValue;
        Console.WriteLine(x);
    }
}
]]>
</code>

            Await TestAsync(input, expected)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Simplification)>
        <WorkItem(531431)>
        Public Async Function TestCSharp_DoNotRemove_RequiredExplicitNullableCast2() As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document><![CDATA[
using System;

class Program
{
    static void Main()
    {
        Console.WriteLine((int){|Simplify:(float?)(int?)2147483647|}); // Prints -2147483648
    }
}
]]>
        </Document>
    </Project>
</Workspace>

            Dim expected =
<code><![CDATA[
using System;

class Program
{
    static void Main()
    {
        Console.WriteLine((int)(float?)(int?)2147483647); // Prints -2147483648
    }
}
]]>
</code>

            Await TestAsync(input, expected)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Simplification)>
        <WorkItem(531431)>
        Public Async Function TestCSharp_Remove_UnnecessaryExplicitNullableCast() As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document><![CDATA[
using System;

class Program
{
    static void Main()
    {
        Console.WriteLine((int)(float?){|Simplify:(int?)2147483647|}); // Prints -2147483648
    }
}
]]>
        </Document>
    </Project>
</Workspace>

            Dim expected =
<code><![CDATA[
using System;

class Program
{
    static void Main()
    {
        Console.WriteLine((int)(float?)2147483647); // Prints -2147483648
    }
}
]]>
</code>

            Await TestAsync(input, expected)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Simplification)>
        <WorkItem(531431)>
        Public Async Function TestCSharp_DoNotRemove_RequiredExplicitNullableCast_And_Remove_UnnecessaryExplicitNullableCast() As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document><![CDATA[
using System;

class Program
{
    static void Main()
    {
        Console.WriteLine({|Simplify:(int){|Simplify:(float?){|Simplify:(int?)2147483647|}|}|}); // Prints -2147483648
    }
}
]]>
        </Document>
    </Project>
</Workspace>

            Dim expected =
<code><![CDATA[
using System;

class Program
{
    static void Main()
    {
        Console.WriteLine((int)(float?)2147483647); // Prints -2147483648
    }
}
]]>
</code>

            Await TestAsync(input, expected)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Simplification)>
        <WorkItem(530248)>
        Public Async Function TestCSharp_DoNotRemove_NecessaryCastInTernaryExpression() As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document><![CDATA[
class Base { }
class Derived1 : Base { }
class Derived2 : Base { }

class Test
{
    public Base F(bool flag, Derived1 d1, Derived2 d2)
    {
        return flag ? d1 : {|Simplify:(Base)d2|};
    }
}
]]>
        </Document>
    </Project>
</Workspace>

            Dim expected =
<code><![CDATA[
class Base { }
class Derived1 : Base { }
class Derived2 : Base { }

class Test
{
    public Base F(bool flag, Derived1 d1, Derived2 d2)
    {
        return flag ? d1 : (Base)d2;
    }
}
]]>
</code>

            Await TestAsync(input, expected)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Simplification)>
        <WorkItem(530248)>
        Public Async Function TestCSharp_DoNotRemove_NecessaryCastInTernaryExpression2() As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document><![CDATA[
class Base { }
class Derived1 : Base { }
class Derived2 : Base { }

class Test
{
    public Base F(bool flag, Derived1 d1, Derived2 d2)
    {
        return flag ? {|Simplify:(Base)d1|} : d2;
    }
}
]]>
        </Document>
    </Project>
</Workspace>

            Dim expected =
<code><![CDATA[
class Base { }
class Derived1 : Base { }
class Derived2 : Base { }

class Test
{
    public Base F(bool flag, Derived1 d1, Derived2 d2)
    {
        return flag ? (Base)d1 : d2;
    }
}
]]>
</code>

            Await TestAsync(input, expected)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Simplification)>
        <WorkItem(530085)>
        Public Async Function TestCSharp_DoNotRemove_NecessaryCastInTernaryExpression3() As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document><![CDATA[
class Program
{
    static void Main(string[] args)
    {
        bool b = true;
        long value = 0;
        long? a = b ? {|Simplify:(long?)value|} : null;
    }
}
]]>
        </Document>
    </Project>
</Workspace>

            Dim expected =
<code><![CDATA[
class Program
{
    static void Main(string[] args)
    {
        bool b = true;
        long value = 0;
        long? a = b ? (long?)value : null;
    }
}
]]>
</code>

            Await TestAsync(input, expected)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Simplification)>
        <WorkItem(529985)>
        Public Async Function TestCSharp_DoNotRemove_NecessaryCastInMemberAccessExpression() As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document><![CDATA[
using System;

class C
{
    static void Main()
    {
        C c = null;
        Console.WriteLine(({|Simplify:(Attribute)c|}).GetType());
    }

    public static implicit operator Attribute(C x)
    {
        return new ObsoleteAttribute();
    }
}
]]>
        </Document>
    </Project>
</Workspace>

            Dim expected =
<code><![CDATA[
using System;

class C
{
    static void Main()
    {
        C c = null;
        Console.WriteLine(((Attribute)c).GetType());
    }

    public static implicit operator Attribute(C x)
    {
        return new ObsoleteAttribute();
    }
}
]]>
</code>

            Await TestAsync(input, expected)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Simplification)>
        <WorkItem(529956)>
        Public Async Function TestCSharp_DoNotRemove_NecessaryCastInForEachExpression() As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document><![CDATA[
using System;
using System.Collections;
 
class C
{
    static void Main()
    {
        foreach (C x in {|Simplify:(IEnumerable) new string[] { null }|})
        {
            Console.WriteLine(x == null);
        }
    }
 
    public static implicit operator C(string s)
    {
        return new C();
    }
}
]]>
        </Document>
    </Project>
</Workspace>

            Dim expected =
<code><![CDATA[
using System;
using System.Collections;
 
class C
{
    static void Main()
    {
        foreach (C x in (IEnumerable) new string[] { null })
        {
            Console.WriteLine(x == null);
        }
    }
 
    public static implicit operator C(string s)
    {
        return new C();
    }
}
]]>
</code>

            Await TestAsync(input, expected)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Simplification)>
        <WorkItem(529956)>
        Public Async Function TestCSharp_DoNotRemove_NecessaryCastInForEachExpressionInsideLambda() As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document><![CDATA[
using System;
using System.Collections;

class C
{
    static void Main()
    {
        Action a = () =>
        {
            foreach (C x in {|Simplify:(IEnumerable)new string[] { null }|})
            {
                Console.WriteLine(x == null);
            }
        };
    }

    public static implicit operator C(string s)
    {
        return new C();
    }
}
]]>
        </Document>
    </Project>
</Workspace>

            Dim expected =
<code><![CDATA[
using System;
using System.Collections;

class C
{
    static void Main()
    {
        Action a = () =>
        {
            foreach (C x in (IEnumerable)new string[] { null })
            {
                Console.WriteLine(x == null);
            }
        };
    }

    public static implicit operator C(string s)
    {
        return new C();
    }
}
]]>
</code>

            Await TestAsync(input, expected)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Simplification)>
        <WorkItem(529844)>
        Public Async Function TestCSharp_DoNotRemove_NecessaryCastInNumericConversion() As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document><![CDATA[
using System;

class Program
{
    static void Main()
    {
        int x = int.MaxValue;
        double y = x;
        double z = {|Simplify:(float)x|};
        Console.WriteLine(x);
        Console.WriteLine(y);
        Console.WriteLine(z);
        Console.WriteLine(y == z);
    }
}
]]>
        </Document>
    </Project>
</Workspace>

            Dim expected =
<code><![CDATA[
using System;

class Program
{
    static void Main()
    {
        int x = int.MaxValue;
        double y = x;
        double z = (float)x;
        Console.WriteLine(x);
        Console.WriteLine(y);
        Console.WriteLine(z);
        Console.WriteLine(y == z);
    }
}
]]>
</code>

            Await TestAsync(input, expected)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Simplification)>
        <WorkItem(662196)>
        Public Async Function TestCSharp_DoNotRemove_NecessaryCastInDynamicInvocation() As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document><![CDATA[
class C
{
    void Foo(string x) { }
    void Foo(string[] x) { }
    static void Main()
    {
        dynamic c = new C();
        c.Foo({|Simplify:(string)null|});
    }
}
]]>
        </Document>
    </Project>
</Workspace>

            Dim expected =
<code><![CDATA[
class C
{
    void Foo(string x) { }
    void Foo(string[] x) { }
    static void Main()
    {
        dynamic c = new C();
        c.Foo((string)null);
    }
}
]]>
</code>

            Await TestAsync(input, expected)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Simplification)>
        <WorkItem(529962)>
        Public Async Function TestCSharp_Remove_UnnecessaryCastInIsExpression() As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document><![CDATA[
using System;
using System.Collections;
 
class C
{
    static void Main()
    {
        string[] x = { };
        Console.WriteLine({|Simplify:(IEnumerable)x|} is object[]);
    }
}
]]>
        </Document>
    </Project>
</Workspace>

            Dim expected =
<code><![CDATA[
using System;
using System.Collections;
 
class C
{
    static void Main()
    {
        string[] x = { };
        Console.WriteLine(x is object[]);
    }
}
]]>
</code>

            Await TestAsync(input, expected)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Simplification)>
        <WorkItem(662196)>
        Public Async Function TestCSharp_Remove_UnnecessaryCastInAsExpression() As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document><![CDATA[
using System;
using System.Collections;
 
class C
{
    static void Main()
    {
        string[] x = { };
        Console.WriteLine({|Simplify:(IEnumerable)x|} as object[]);
    }
}
]]>
        </Document>
    </Project>
</Workspace>

            Dim expected =
<code><![CDATA[
using System;
using System.Collections;
 
class C
{
    static void Main()
    {
        string[] x = { };
        Console.WriteLine(x as object[]);
    }
}
]]>
</code>

            Await TestAsync(input, expected)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Simplification)>
        <WorkItem(529973)>
        Public Async Function TestCSharp_DoNotRemove_NecessaryCastToDelegateInIsExpression() As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document><![CDATA[
using System;
 
class Program
{
    static void Main()
    {
        var x = checked({|Simplify:(Action)delegate { }|}) is object;
    }
}
]]>
        </Document>
    </Project>
</Workspace>

            Dim expected =
<code><![CDATA[
using System;
 
class Program
{
    static void Main()
    {
        var x = checked((Action)delegate { }) is object;
    }
}
]]>
</code>

            Await TestAsync(input, expected)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Simplification)>
        <WorkItem(529973)>
        Public Async Function TestCSharp_DoNotRemove_NecessaryCastToDelegateInAsExpression() As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document><![CDATA[
using System;
 
class Program
{
    static void Main()
    {
        var x = checked({|Simplify:(Action)delegate { }|}) as object;
    }
}
]]>
        </Document>
    </Project>
</Workspace>

            Dim expected =
<code><![CDATA[
using System;
 
class Program
{
    static void Main()
    {
        var x = checked((Action)delegate { }) as object;
    }
}
]]>
</code>

            Await TestAsync(input, expected)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Simplification)>
        <WorkItem(529968)>
        Public Async Function TestCSharp_DoNotRemove_NecessaryCastForParamsArgument() As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document><![CDATA[
using System;
 
class A
{
    static void Main()
    {
        Foo({|Simplify:(object) new A()|});
    }
 
    static void Foo(params object[] x)
    {
        Console.WriteLine(x == null);
    }
 
    public static implicit operator object[](A a)
    {
        return null;
    }
}
]]>
        </Document>
    </Project>
</Workspace>

            Dim expected =
<code><![CDATA[
using System;
 
class A
{
    static void Main()
    {
        Foo((object) new A());
    }
 
    static void Foo(params object[] x)
    {
        Console.WriteLine(x == null);
    }
 
    public static implicit operator object[](A a)
    {
        return null;
    }
}
]]>
</code>

            Await TestAsync(input, expected)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Simplification)>
        <WorkItem(529968)>
        Public Async Function TestCSharp_Remove_UnnecessaryCastsForParamsArguments() As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document><![CDATA[
using System;
 
class A
{
    static void Main()
    {
        Foo({|Simplify:(object)new A()|}, {|Simplify:(object)new A()|});
    }
 
    static void Foo(params object[] x)
    {
        Console.WriteLine(x == null);
    }
 
    public static implicit operator object[](A a)
    {
        return null;
    }
}
]]>
        </Document>
    </Project>
</Workspace>

            Dim expected =
<code><![CDATA[
using System;
 
class A
{
    static void Main()
    {
        Foo(new A(), new A());
    }
 
    static void Foo(params object[] x)
    {
        Console.WriteLine(x == null);
    }
 
    public static implicit operator object[](A a)
    {
        return null;
    }
}
]]>
</code>

            Await TestAsync(input, expected)
        End Function

        <WorkItem(530083)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Simplification)>
        Public Async Function TestCSharp_DoNotRemove_InsideThrowStatement() As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
using System;

class C
{
	public static void Main()
	{
		object ex = new Exception();
		throw {|Simplify:(Exception)ex|};
	}
}
        </Document>
    </Project>
</Workspace>

            Dim expected =
<code>
using System;

class C
{
	public static void Main()
	{
		object ex = new Exception();
		throw (Exception)ex;
	}
}
</code>

            Await TestAsync(input, expected)

        End Function

        <WorkItem(530083)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Simplification)>
        Public Async Function TestCSharp_Remove_InsideThrowStatement() As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
using System;

class C
{
	public static void Main()
	{
		var ex = new ArgumentException();
		throw {|Simplify:(Exception)ex|};
	}
}
       </Document>
    </Project>
</Workspace>

            Dim expected =
<code>
using System;

class C
{
	public static void Main()
	{
		var ex = new ArgumentException();
		throw ex;
	}
}
</code>

            Await TestAsync(input, expected)

        End Function

        <WorkItem(530083)>
        <WorkItem(2761, "https://github.com/dotnet/roslyn/issues/2761")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Simplification)>
        Public Async Function TestCSharp_Remove_InsideThrowStatement2() As Task
            ' We can't remove cast from base to derived, as we cannot be sure that the cast will succeed at runtime.
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
using System;

class C
{
	public static void Main()
	{
		Exception ex = new ArgumentException();
		throw {|Simplify:(ArgumentException)ex|};
	}
}
        </Document>
    </Project>
</Workspace>

            Dim expected =
<code>
using System;

class C
{
	public static void Main()
	{
		Exception ex = new ArgumentException();
		throw (ArgumentException)ex;
	}
}
</code>

            Await TestAsync(input, expected)

        End Function

        <WorkItem(529919)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Simplification)>
        Public Async Function TestCsharp_Remove_DelegateVarianceConversions() As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document><![CDATA[
using System;

class Program
{
    static void Main()
    {
        Action<object> a = Console.WriteLine;
        Action<string> b = {|Simplify:(Action<string>)a|};
        ({|Simplify:(Action<string>)a|})("A");
        ({|Simplify:(Action<string>)a|}).Invoke("A");
    }
}]]>
        </Document>
    </Project>
</Workspace>

            Dim expected =
<code><![CDATA[
using System;

class Program
{
    static void Main()
    {
        Action<object> a = Console.WriteLine;
        Action<string> b = a;
        (a)("A");
        (a).Invoke("A");
    }
}]]>
</code>

            Await TestAsync(input, expected)

        End Function

        <WorkItem(529884)>
        <WorkItem(1043494, "DevDiv")>
        <WpfFact(Skip:="1043494"), Trait(Traits.Feature, Traits.Features.Simplification)>
        Public Async Function TestCsharp_DoNotRemove_ParamDefaultValueNegativeZero() As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document><![CDATA[
using System;

interface I
{
    void Foo(double x = +0.0);
}

sealed class C : I
{
    public void Foo(double x = -0.0)
    {
        Console.WriteLine(1 / x > 0);
    }
 
    static void Main()
    {
        ({|Simplify:(I)new C()|}).Foo();
    }
}]]>
        </Document>
    </Project>
</Workspace>

            Dim expected =
<code><![CDATA[
using System;

interface I
{
    void Foo(double x = +0.0);
}

sealed class C : I
{
    public void Foo(double x = -0.0)
    {
        Console.WriteLine(1 / x > 0);
    }
 
    static void Main()
    {
        ((I)new C()).Foo();
    }
}]]>
</code>

            Await TestAsync(input, expected)

        End Function

        <WorkItem(529884)>
        <WorkItem(1043494, "DevDiv")>
        <WpfFact(Skip:="1043494"), Trait(Traits.Feature, Traits.Features.Simplification)>
        Public Async Function TestCsharp_DoNotRemove_ParamDefaultValueNegativeZero2() As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document><![CDATA[
using System;

interface I
{
    void Foo(double x = -(-0.0));
}

sealed class C : I
{
    public void Foo(double x = -0.0)
    {
        Console.WriteLine(1 / x > 0);
    }
 
    static void Main()
    {
        ({|Simplify:(I)new C()|}).Foo();
    }
}]]>
        </Document>
    </Project>
</Workspace>

            Dim expected =
<code><![CDATA[
using System;

interface I
{
    void Foo(double x = -(-0.0));
}

sealed class C : I
{
    public void Foo(double x = -0.0)
    {
        Console.WriteLine(1 / x > 0);
    }
 
    static void Main()
    {
        ((I)new C()).Foo();
    }
}]]>
</code>

            Await TestAsync(input, expected)

        End Function

        <WorkItem(529884)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Simplification)>
        Public Async Function TestCsharp_Remove_ParamDefaultValueZero() As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document><![CDATA[
using System;

interface I
{
    void Foo(double x = +0.0);
}

sealed class C : I
{
    public void Foo(double x = -(-0.0))
    {
        Console.WriteLine(1 / x > 0);
    }
 
    static void Main()
    {
        ({|Simplify:(I)new C()|}).Foo();
    }
}]]>
        </Document>
    </Project>
</Workspace>

            Dim expected =
<code><![CDATA[
using System;

interface I
{
    void Foo(double x = +0.0);
}

sealed class C : I
{
    public void Foo(double x = -(-0.0))
    {
        Console.WriteLine(1 / x > 0);
    }
 
    static void Main()
    {
        (new C()).Foo();
    }
}]]>
</code>

            Await TestAsync(input, expected)

        End Function

        <WorkItem(529791)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Simplification)>
        Public Async Function TestCsharp_Remove_UnnecessaryImplicitNullableCast() As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document><![CDATA[
class X
{
    static void Foo()
    {
        object x = {|Simplify:(string)null|};
        object y = {|Simplify:(int?)null|};
    }
}
]]>
        </Document>
    </Project>
</Workspace>

            Dim expected =
<code><![CDATA[
class X
{
    static void Foo()
    {
        object x = null;
        object y = null;
    }
}
]]>
</code>

            Await TestAsync(input, expected)

        End Function

        <WorkItem(530744)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Simplification)>
        Public Async Function TestCsharp_Remove_UnnecessaryImplicitEnumerationCast() As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document><![CDATA[
using System;
 
class Program
{
    static void Main()
    {
        DayOfWeek? x = {|Simplify:(DayOfWeek)0|};
    }
}
]]>
        </Document>
    </Project>
</Workspace>

            Dim expected =
<code><![CDATA[
using System;
 
class Program
{
    static void Main()
    {
        DayOfWeek? x = 0;
    }
}
]]>
</code>

            Await TestAsync(input, expected)

        End Function

        <WorkItem(529831)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Simplification)>
        Public Async Function TestCsharp_Remove_UnnecessaryInterfaceCast() As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document><![CDATA[
using System;
 
interface IIncrementable
{
    int Value { get; }
    void Increment();
}
 
struct S : IIncrementable
{
    public int Value { get; private set; }
    public void Increment() { Value++; }
}
 
class C : IIncrementable
{
    public int Value { get; private set; }
    public void Increment() { Value++; }
}
 
static class Program
{
    static void Main()
    {
        Foo(new S(), new C(), new C(), new C());
    }
 
    static void Foo<TAny, TClass, TClass2, TClass3>(TAny x, TClass y, TClass2 z, TClass3 t)
        where TAny : IIncrementable // Can be a value type
        where TClass : class, IIncrementable, new() // Always a reference type because of explicit 'class' constraint
        where TClass2 : IIncrementable // Always a reference type because used as a constraint for TClass3
        where TClass3 : TClass, TClass2 // Always a reference type because its constraint TClass cannot be an interface (because of the new() constraint), object, System.ValueType or System.Enum (because of the IIncrementable constraint)
    {
        ({|Simplify:(IIncrementable)x|}).Increment(); // Necessary cast
        ({|Simplify:(IIncrementable)y|}).Increment(); // Unnecessary Cast - OK
        ({|Simplify:(IIncrementable)z|}).Increment(); // Necessary cast
        ({|Simplify:(IIncrementable)t|}).Increment(); // Necessary cast

        Console.WriteLine(x.Value);
        Console.WriteLine(y.Value);
        Console.WriteLine(z.Value);
        Console.WriteLine(t.Value);
    }
}
]]>
        </Document>
    </Project>
</Workspace>

            Dim expected =
<code><![CDATA[
using System;
 
interface IIncrementable
{
    int Value { get; }
    void Increment();
}
 
struct S : IIncrementable
{
    public int Value { get; private set; }
    public void Increment() { Value++; }
}
 
class C : IIncrementable
{
    public int Value { get; private set; }
    public void Increment() { Value++; }
}
 
static class Program
{
    static void Main()
    {
        Foo(new S(), new C(), new C(), new C());
    }
 
    static void Foo<TAny, TClass, TClass2, TClass3>(TAny x, TClass y, TClass2 z, TClass3 t)
        where TAny : IIncrementable // Can be a value type
        where TClass : class, IIncrementable, new() // Always a reference type because of explicit 'class' constraint
        where TClass2 : IIncrementable // Always a reference type because used as a constraint for TClass3
        where TClass3 : TClass, TClass2 // Always a reference type because its constraint TClass cannot be an interface (because of the new() constraint), object, System.ValueType or System.Enum (because of the IIncrementable constraint)
    {
        ((IIncrementable)x).Increment(); // Necessary cast
        (y).Increment(); // Unnecessary Cast - OK
        ((IIncrementable)z).Increment(); // Necessary cast
        ((IIncrementable)t).Increment(); // Necessary cast

        Console.WriteLine(x.Value);
        Console.WriteLine(y.Value);
        Console.WriteLine(z.Value);
        Console.WriteLine(t.Value);
    }
}
]]>
</code>

            Await TestAsync(input, expected)

        End Function

        <WorkItem(529877)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Simplification)>
        Public Async Function TestCsharp_Remove_UnnecessarySealedClassToInterfaceCast() As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document><![CDATA[
using System;
 
class C : IDisposable
{
    public void Dispose() { }
}
 
sealed class D : C
{
    static void Main()
    {
        D s = new D();
        ({|Simplify:(IDisposable)s|}).Dispose();
    }
}
]]>
        </Document>
    </Project>
</Workspace>

            Dim expected =
<code><![CDATA[
using System;
 
class C : IDisposable
{
    public void Dispose() { }
}
 
sealed class D : C
{
    static void Main()
    {
        D s = new D();
        (s).Dispose();
    }
}
]]>
</code>

            Await TestAsync(input, expected)

        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Simplification)>
        <WorkItem(529887)>
        Public Async Function TestCsharp_Remove_UnnecessaryReadOnlyValueTypeToInterfaceCast() As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document><![CDATA[
using System;
 
interface IIncrementable
{
    int Value { get; }
    void Increment();
}
 
struct S : IIncrementable
{
    public int Value { get; private set; }
    public void Increment() { Value++; }
 
    static readonly S s = new S();
 
    static void Main()
    {
        // Note: readonly modifier guarantees that a copy of a value type is always made before modification, so a boxing is not observable.

        ({|Simplify:(IIncrementable)s|}).Increment();
        Console.WriteLine(s.Value);
    }
}
]]>
        </Document>
    </Project>
</Workspace>

            Dim expected =
<code><![CDATA[
using System;
 
interface IIncrementable
{
    int Value { get; }
    void Increment();
}
 
struct S : IIncrementable
{
    public int Value { get; private set; }
    public void Increment() { Value++; }
 
    static readonly S s = new S();
 
    static void Main()
    {
        // Note: readonly modifier guarantees that a copy of a value type is always made before modification, so a boxing is not observable.

        (s).Increment();
        Console.WriteLine(s.Value);
    }
}
]]>
</code>

            Await TestAsync(input, expected)

        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Simplification)>
        <WorkItem(529888)>
        Public Async Function TestCsharp_Remove_UnnecessaryObjectCreationToInterfaceCast() As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document><![CDATA[
using System;

struct Y : IDisposable
{
    public void Dispose() { }
}
 
class X : IDisposable
{
    static void Main()
    {
        ({|Simplify:(IDisposable)new X()|}).Dispose();
        ({|Simplify:(IDisposable)new Y()|}).Dispose();
    }
 
    public void Dispose() { }
}
]]>
        </Document>
    </Project>
</Workspace>

            Dim expected =
<code><![CDATA[
using System;

struct Y : IDisposable
{
    public void Dispose() { }
}
 
class X : IDisposable
{
    static void Main()
    {
        (new X()).Dispose();
        (new Y()).Dispose();
    }
 
    public void Dispose() { }
}
]]>
</code>

            Await TestAsync(input, expected)

        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Simplification)>
        <WorkItem(529912)>
        Public Async Function TestCsharp_Remove_UnnecessaryEffectivelySealedClassToInterfaceCast() As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document><![CDATA[
using System;
 
class C : IDisposable
{
    private C() { }
 
    public void Dispose() { }
 
    static void Main()
    {
        var x = new C();
        ({|Simplify:(IDisposable)x|}).Dispose();
    }
}
]]>
        </Document>
    </Project>
</Workspace>

            Dim expected =
<code><![CDATA[
using System;
 
class C : IDisposable
{
    private C() { }
 
    public void Dispose() { }
 
    static void Main()
    {
        var x = new C();
        (x).Dispose();
    }
}
]]>
</code>

            Await TestAsync(input, expected)

        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Simplification)>
        <WorkItem(529912)>
        Public Async Function TestCsharp_Remove_UnnecessaryEffectivelySealedClassToInterfaceCast2() As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document><![CDATA[
using System;

class C : IDisposable
{
    private C() { }
 
    public void Dispose() { }
 
    static void Main()
    {
        var x = new C();
        ({|Simplify:(IDisposable)x|}).Dispose();
    }
}

struct D : IDisposable
{
    public void Dispose() { }
}

class E: C, IDisposable
{
    public void Dispose() { }
}
]]>
        </Document>
    </Project>
</Workspace>

            Dim expected =
<code><![CDATA[
using System;

class C : IDisposable
{
    private C() { }
 
    public void Dispose() { }
 
    static void Main()
    {
        var x = new C();
        (x).Dispose();
    }
}

struct D : IDisposable
{
    public void Dispose() { }
}

class E: C, IDisposable
{
    public void Dispose() { }
}
]]>
</code>

            Await TestAsync(input, expected)

        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Simplification)>
        <WorkItem(529912)>
        Public Async Function TestCsharp_Remove_UnnecessaryEffectivelySealedClassToInterfaceCast3() As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document><![CDATA[
using System;

class C : IDisposable
{
    private C() { }
 
    public void Dispose() { }
 
    static void Main()
    {
        var x = new C();
        ({|Simplify:(IDisposable)x|}).Dispose();
    }

    interface I { }
}

struct D : IDisposable
{
    public void Dispose() { }
}

class E: C, IDisposable
{
    public void Dispose() { }
}
]]>
        </Document>
    </Project>
</Workspace>

            Dim expected =
<code><![CDATA[
using System;

class C : IDisposable
{
    private C() { }
 
    public void Dispose() { }
 
    static void Main()
    {
        var x = new C();
        (x).Dispose();
    }

    interface I { }
}

struct D : IDisposable
{
    public void Dispose() { }
}

class E: C, IDisposable
{
    public void Dispose() { }
}
]]>
</code>

            Await TestAsync(input, expected)

        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Simplification)>
        <WorkItem(529913)>
        Public Async Function TestCsharp_Remove_UnnecessaryEffectivelySealedClassToInterface4() As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document><![CDATA[
using System;
 
class A
{
    class C : IDisposable
    {
        public void Dispose() { }
 
        static void Main()
        {
            var x = new C();
            ({|Simplify:(IDisposable)x|}).Dispose();
        }
    }
}
]]>
        </Document>
    </Project>
</Workspace>

            Dim expected =
<code><![CDATA[
using System;
 
class A
{
    class C : IDisposable
    {
        public void Dispose() { }
 
        static void Main()
        {
            var x = new C();
            (x).Dispose();
        }
    }
}
]]>
</code>

            Await TestAsync(input, expected)

        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Simplification)>
        <WorkItem(529913)>
        Public Async Function TestCsharp_Remove_UnnecessaryEffectivelySealedClassToInterfaceCast5() As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document><![CDATA[
using System;
 
class A
{
    class C : IDisposable
    {
        public void Dispose() { }
 
        static void Main()
        {
            var x = new C();
            ({|Simplify:(IDisposable)x|}).Dispose();
        }
    }
}

struct D : IDisposable
{
    public void Dispose() { }
}

class E: C, IDisposable
{
    public void Dispose() { }
}
]]>
        </Document>
    </Project>
</Workspace>

            Dim expected =
<code><![CDATA[
using System;
 
class A
{
    class C : IDisposable
    {
        public void Dispose() { }
 
        static void Main()
        {
            var x = new C();
            (x).Dispose();
        }
    }
}

struct D : IDisposable
{
    public void Dispose() { }
}

class E: C, IDisposable
{
    public void Dispose() { }
}
]]>
</code>

            Await TestAsync(input, expected)

        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Simplification)>
        <WorkItem(529912)>
        Public Async Function TestCsharp_DoNotRemove_NecessaryClassToInterfaceCast() As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document><![CDATA[
using System;

class C : IDisposable
{
    private C() { }
 
    public void Dispose() { }
 
    static void Main()
    {
        var x = new C();
        ({|Simplify:(IDisposable)x|}).Dispose();
    }

    class E: C, IDisposable
    {
        public void Dispose() { }
    }
}

struct D : IDisposable
{
    public void Dispose() { }
}
]]>
        </Document>
    </Project>
</Workspace>

            Dim expected =
<code><![CDATA[
using System;

class C : IDisposable
{
    private C() { }
 
    public void Dispose() { }
 
    static void Main()
    {
        var x = new C();
        ((IDisposable)x).Dispose();
    }

    class E: C, IDisposable
    {
        public void Dispose() { }
    }
}

struct D : IDisposable
{
    public void Dispose() { }
}
]]>
</code>

            Await TestAsync(input, expected)

        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Simplification)>
        <WorkItem(529913)>
        Public Async Function TestCsharp_DoNotRemove_NecessaryClassToInterfaceCast2() As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document><![CDATA[
using System;
 
class A
{
    class C : IDisposable
    {
        public void Dispose() { }
 
        static void Main()
        {
            var x = new C();
            ({|Simplify:(IDisposable)x|}).Dispose();
        }

        class E: C, IDisposable
        {
            public void Dispose() { }
        }
    }
}

struct D : IDisposable
{
    public void Dispose() { }
}
]]>
        </Document>
    </Project>
</Workspace>

            Dim expected =
<code><![CDATA[
using System;
 
class A
{
    class C : IDisposable
    {
        public void Dispose() { }
 
        static void Main()
        {
            var x = new C();
            ((IDisposable)x).Dispose();
        }

        class E: C, IDisposable
        {
            public void Dispose() { }
        }
    }
}

struct D : IDisposable
{
    public void Dispose() { }
}
]]>
</code>

            Await TestAsync(input, expected)

        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Simplification)>
        <WorkItem(529889)>
        Public Async Function TestCsharp_Remove_UnnecessaryCastFromImmutableValueTypeToInterface() As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document><![CDATA[
using System;
 
class Program
{
    static void Main()
    {
        int x = 1;
        var y = ({|Simplify:(IComparable<int>)x|}).CompareTo(0);
    }
}
]]>
        </Document>
    </Project>
</Workspace>

            Dim expected =
<code><![CDATA[
using System;
 
class Program
{
    static void Main()
    {
        int x = 1;
        var y = (x).CompareTo(0);
    }
}
]]>
</code>

            Await TestAsync(input, expected)

        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Simplification)>
        <WorkItem(529988)>
        Public Async Function TestCsharp_DoNotRemove_NecessaryCastInDelegateCreationExpression() As Task
            ' Note: Removing the cast changes the lambda parameter type and invocation method symbol.

            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document><![CDATA[
using System;
 
static class Program
{
    static void Main()
    {
        new Action<string>({|Simplify:(Action<object>)(y => y.Foo())|})(null);
    }
 
    static void Foo(this object x) { Console.WriteLine(1); }
    static void Foo(this string x) { Console.WriteLine(2); }
}
]]>
        </Document>
    </Project>
</Workspace>

            Dim expected =
<code><![CDATA[
using System;
 
static class Program
{
    static void Main()
    {
        new Action<string>((Action<object>)(y => y.Foo()))(null);
    }
 
    static void Foo(this object x) { Console.WriteLine(1); }
    static void Foo(this string x) { Console.WriteLine(2); }
}
]]>
</code>

            Await TestAsync(input, expected)

        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Simplification)>
        <WorkItem(529988)>
        Public Async Function TestCsharp_DoNotRemove_NecessaryCastInDelegateCreationExpression2() As Task
            ' Note: Removing the cast changes the lambda parameter type and invocation method symbol.

            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document><![CDATA[
using System;
 
static class Program
{
    static void Main()
    {
        new Action<string>({|Simplify:(Action<object>)((y) => { y.Foo(); })|})(null);
    }
 
    static void Foo(this object x) { Console.WriteLine(1); }
    static void Foo(this string x) { Console.WriteLine(2); }
}
]]>
        </Document>
    </Project>
</Workspace>

            Dim expected =
<code><![CDATA[
using System;
 
static class Program
{
    static void Main()
    {
        new Action<string>((Action<object>)((y) => { y.Foo(); }))(null);
    }
 
    static void Foo(this object x) { Console.WriteLine(1); }
    static void Foo(this string x) { Console.WriteLine(2); }
}
]]>
</code>

            Await TestAsync(input, expected)

        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Simplification)>
        <WorkItem(529988)>
        Public Async Function TestCsharp_Remove_UnnecessaryCastInDelegateCreationExpression3() As Task
            ' Note: Removing the cast changes the lambda parameter type, but doesn't change the semantics of the lambda body.

            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document><![CDATA[
using System;
 
static class Program
{
    static void Main()
    {
        new Action<string>({|Simplify:(Action<object>)(y => Foo(1))|})(null);
    }
 
    static void Foo(this object x) { Console.WriteLine(1); }
    static void Foo(this string x) { Console.WriteLine(2); }
}
]]>
        </Document>
    </Project>
</Workspace>

            Dim expected =
<code><![CDATA[
using System;
 
static class Program
{
    static void Main()
    {
        new Action<string>((y => Foo(1)))(null);
    }
 
    static void Foo(this object x) { Console.WriteLine(1); }
    static void Foo(this string x) { Console.WriteLine(2); }
}
]]>
</code>

            Await TestAsync(input, expected)

        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Simplification)>
        <WorkItem(529988)>
        Public Async Function TestCsharp_Remove_UnnecessaryCastInDelegateCreationExpression4() As Task
            ' Note: Removing the cast changes the lambda parameter type, but doesn't change the semantics of the lambda body.

            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document><![CDATA[
using System;
 
static class Program
{
    static void Main()
    {
        new Action<string>({|Simplify:(Action<object>)((y) => { string x = y; x.Foo(); })|})(null);
    }
 
    static void Foo(this object x) { Console.WriteLine(1); }
    static void Foo(this string x) { Console.WriteLine(2); }
}
]]>
        </Document>
    </Project>
</Workspace>

            Dim expected =
<code><![CDATA[
using System;
 
static class Program
{
    static void Main()
    {
        new Action<string>(((y) => { string x = y; x.Foo(); }))(null);
    }
 
    static void Foo(this object x) { Console.WriteLine(1); }
    static void Foo(this string x) { Console.WriteLine(2); }
}
]]>
</code>

            Await TestAsync(input, expected)

        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Simplification)>
        <WorkItem(529988)>
        Public Async Function TestCsharp_DoNotRemove_NecessaryCastInDelegateCreationExpression5() As Task
            ' Note: Removing the cast changes the lambda parameter type and hence changes the inferred type of lambda local "x".

            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document><![CDATA[
using System;
 
static class Program
{
    static void Main()
    {
        new Action<string>({|Simplify:(Action<object>)((y) => { var x = y; x.Foo(); })|})(null);
    }
 
    static void Foo(this object x) { Console.WriteLine(1); }
    static void Foo(this string x) { Console.WriteLine(2); }
}
]]>
        </Document>
    </Project>
</Workspace>

            Dim expected =
<code><![CDATA[
using System;
 
static class Program
{
    static void Main()
    {
        new Action<string>((Action<object>)((y) => { var x = y; x.Foo(); }))(null);
    }
 
    static void Foo(this object x) { Console.WriteLine(1); }
    static void Foo(this string x) { Console.WriteLine(2); }
}
]]>
</code>

            Await TestAsync(input, expected)

        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Simplification)>
        <WorkItem(529988)>
        Public Async Function TestCsharp_DoNotRemove_NecessaryCastInDelegateCreationExpression6() As Task
            ' Note: Removing the cast changes the parameter type of lambda parameter "z"
            ' and changes the method symbol Foo invoked in the lambda body.

            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document><![CDATA[
using System;

static class Program
{
    static void Main()
    {
        new Action<object, string>({|Simplify:(Action<object, object>)((y, z) => { z.Foo(); })|})(null, null);
    }

    static void Foo(this object x) { Console.WriteLine(1); }
    static void Foo(this string x) { Console.WriteLine(2); }
}
]]>
        </Document>
    </Project>
</Workspace>

            Dim expected =
<code><![CDATA[
using System;

static class Program
{
    static void Main()
    {
        new Action<object, string>((Action<object, object>)((y, z) => { z.Foo(); }))(null, null);
    }

    static void Foo(this object x) { Console.WriteLine(1); }
    static void Foo(this string x) { Console.WriteLine(2); }
}
]]>
</code>

            Await TestAsync(input, expected)

        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Simplification)>
        <WorkItem(529988)>
        Public Async Function TestCsharp_Remove_UnnecessaryCastInDelegateCreationExpression7() As Task
            ' Note: Removing the cast changes the parameter type of lambda parameter "z"
            ' but not that of parameter "y" and hence the semantics of the lambda body aren't changed.

            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document><![CDATA[
using System;

static class Program
{
    static void Main()
    {
        new Action<string, object>({|Simplify:(Action<object, object>)((y, z) => { object x = y; z.Foo(); })|})(null, null);
    }

    static void Foo(this object x) { Console.WriteLine(1); }
    static void Foo(this string x) { Console.WriteLine(2); }
}
]]>
        </Document>
    </Project>
</Workspace>

            Dim expected =
<code><![CDATA[
using System;

static class Program
{
    static void Main()
    {
        new Action<string, object>(((y, z) => { object x = y; z.Foo(); }))(null, null);
    }

    static void Foo(this object x) { Console.WriteLine(1); }
    static void Foo(this string x) { Console.WriteLine(2); }
}
]]>
</code>

            Await TestAsync(input, expected)

        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Simplification)>
        <WorkItem(529988)>
        Public Async Function TestCsharp_DoNotRemove_NecessaryCastInDelegateCreationExpression8() As Task
            ' Note: Removing the cast changes the parameter type of lambda parameter "y"
            ' and changes the built in operator invoked for "y + z".

            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document><![CDATA[
using System;

static class Program
{
    static void Main()
    {
        new Action<string, string>({|Simplify:(Action<object, string>)((y, z) => { Console.WriteLine(y + z); })|})("Hi", "Hello");
    }
}
]]>
        </Document>
    </Project>
</Workspace>

            Dim expected =
<code><![CDATA[
using System;

static class Program
{
    static void Main()
    {
        new Action<string, string>((Action<object, string>)((y, z) => { Console.WriteLine(y + z); }))("Hi", "Hello");
    }
}
]]>
</code>

            Await TestAsync(input, expected)

        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Simplification)>
        <WorkItem(529988)>
        Public Async Function TestCsharp_DoNotRemove_NecessaryCastInDelegateCreationExpression9() As Task
            ' Note: Removing the cast changes the parameter type of lambda parameter "y"
            ' and changes the semantics of nested lambda body.

            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document><![CDATA[
using System;

static class Program
{
    static void Main()
    {
        new Action<string, string>({|Simplify:(Action<object, string>)((y, z) =>
                { Action<string> a = (w) => { y.Foo(); }; }
            )|})("Hi", "Hello");
    }

    static void Foo(this object x) { Console.WriteLine(1); }
    static void Foo(this string x) { Console.WriteLine(2); }
}
]]>
        </Document>
    </Project>
</Workspace>

            Dim expected =
<code><![CDATA[
using System;

static class Program
{
    static void Main()
    {
        new Action<string, string>((Action<object, string>)((y, z) =>
                { Action<string> a = (w) => { y.Foo(); }; }
            ))("Hi", "Hello");
    }

    static void Foo(this object x) { Console.WriteLine(1); }
    static void Foo(this string x) { Console.WriteLine(2); }
}
]]>
</code>

            Await TestAsync(input, expected)

        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Simplification)>
        <WorkItem(529982)>
        Public Async Function TestCsharp_Remove_UnnecessaryExplicitCastForLambdaExpression() As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document><![CDATA[
using System;

class Program
{
    static void Main(string[] args)
    {
        Func<Exception> f = {|Simplify:(Func<Exception>)(() => new ArgumentException())|};
    }
}
]]>
        </Document>
    </Project>
</Workspace>

            Dim expected =
<code><![CDATA[
using System;

class Program
{
    static void Main(string[] args)
    {
        Func<Exception> f = (() => new ArgumentException());
    }
}
]]>
</code>

            Await TestAsync(input, expected)

        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Simplification)>
        <WorkItem(835671)>
        Public Async Function TestCsharp_DoNotRemove_NecessaryCastInUnaryExpression() As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document><![CDATA[
class C
{
    void Method(double d)
    {
        Method({|Simplify:(int)d|}); // not flagged because the cast changes the semantics
        Method(-{|Simplify:(int)d|}); // should not be flagged
    }
}
]]>
        </Document>
    </Project>
</Workspace>

            Dim expected =
<code><![CDATA[
class C
{
    void Method(double d)
    {
        Method((int)d); // not flagged because the cast changes the semantics
        Method(-(int)d); // should not be flagged
    }
}
]]>
</code>

            Await TestAsync(input, expected)

        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Simplification)>
        <WorkItem(838107)>
        Public Async Function TestCsharp_DoNotRemove_NecessaryCastInPointerExpression() As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document><![CDATA[
unsafe class C
{
    internal static uint F(byte* ptr)
    {
        return *{|Simplify:(ushort*)ptr|};
    }
}
]]>
        </Document>
    </Project>
</Workspace>

            Dim expected =
<code><![CDATA[
unsafe class C
{
    internal static uint F(byte* ptr)
    {
        return *(ushort*)ptr;
    }
}
]]>
</code>

            Await TestAsync(input, expected)

        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Simplification)>
        <WorkItem(835537)>
        Public Async Function TestCsharp_DoNotRemove_NecessaryExplicitCastInReferenceComparison() As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document><![CDATA[
class Program
{
    void F()
    {
        object x = string.Intern("Hi!");
        bool wasInterned = {|Simplify:(object)x|} == "Hi!";
    }
}
]]>
        </Document>
    </Project>
</Workspace>

            Dim expected =
<code><![CDATA[
class Program
{
    void F()
    {
        object x = string.Intern("Hi!");
        bool wasInterned = (object)x == "Hi!";
    }
}
]]>
</code>

            Await TestAsync(input, expected)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Simplification)>
        <WorkItem(835537)>
        Public Async Function TestCsharp_DoNotRemove_NecessaryExplicitCastInReferenceComparison2() As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document><![CDATA[
class Program
{
    void F()
    {
        object x = string.Intern("Hi!");
        bool wasInterned = x == {|Simplify:(object)"Hi!"|};
    }
}
]]>
        </Document>
    </Project>
</Workspace>

            Dim expected =
<code><![CDATA[
class Program
{
    void F()
    {
        object x = string.Intern("Hi!");
        bool wasInterned = x == (object)"Hi!";
    }
}
]]>
</code>

            Await TestAsync(input, expected)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Simplification)>
        <WorkItem(835537)>
        Public Async Function TestCsharp_Remove_UnnecessaryExplicitCastInReferenceComparison() As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document><![CDATA[
class Program
{
    void F()
    {
        object x = string.Intern("Hi!");
        bool wasInterned = {|Simplify:(object)x|} == (object)"Hi!";
    }
}
]]>
        </Document>
    </Project>
</Workspace>

            Dim expected =
<code><![CDATA[
class Program
{
    void F()
    {
        object x = string.Intern("Hi!");
        bool wasInterned = x == (object)"Hi!";
    }
}
]]>
</code>

            Await TestAsync(input, expected)

        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Simplification)>
        <WorkItem(835537), WorkItem(902508)>
        Public Async Function TestCsharp_Remove_UnnecessaryExplicitCastInReferenceComparison2() As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document><![CDATA[
public class Class1
{
    void F(Class1 c)
    {
        if ({|Simplify:(Class1)c|} != null)
        {
            var x = {|Simplify:(Class1)c|};
        }
    }
}
]]>
        </Document>
    </Project>
</Workspace>

            Dim expected =
<code><![CDATA[
public class Class1
{
    void F(Class1 c)
    {
        if (c != null)
        {
            var x = c;
        }
    }
}]]>
</code>

            Await TestAsync(input, expected)

        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Simplification)>
        <WorkItem(529858)>
        Public Async Function TestCsharp_Remove_UnnecessaryCastFromEnumTypeToUnderlyingType() As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document><![CDATA[
using System;
 
class C
{
    static void Main()
    {
        DayOfWeek x = DayOfWeek.Monday;
        if ({|Simplify:(int)x|} == 0) { }
    }
}
]]>
        </Document>
    </Project>
</Workspace>

            Dim expected =
<code><![CDATA[
using System;
 
class C
{
    static void Main()
    {
        DayOfWeek x = DayOfWeek.Monday;
        if (x == 0) { }
    }
}
]]>
</code>

            Await TestAsync(input, expected)

        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Simplification)>
        <WorkItem(889341)>
        Public Async Function TestCSharp_DoNotRemove_CastInErroneousCode() As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document><![CDATA[
class C
{
    void M()
    {
        object x = null;
        M({|Simplify:(string)x|});
    }
}
]]>
        </Document>
    </Project>
</Workspace>

            Dim expected =
<code><![CDATA[
class C
{
    void M()
    {
        object x = null;
        M((string)x);
    }
}
]]>
</code>

            Await TestAsync(input, expected)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Simplification)>
        <WorkItem(870550)>
        Public Async Function TestCSharp_Remove_CastThatBreaksParentSyntaxUnlessParenthesized() As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document><![CDATA[
class Program
{
    static void Main()
    {
        var x = 1;
        object y = x;
        int i = 1;
        Foo(x < {|Simplify:(int)i|}, x > (int)y); // Remove Unnecessary Cast
    }
 
    static void Foo(bool a, bool b) { }
}
]]>
        </Document>
    </Project>
</Workspace>

            Dim expected =
<code><![CDATA[
class Program
{
    static void Main()
    {
        var x = 1;
        object y = x;
        int i = 1;
        Foo((x < i), x > (int)y); // Remove Unnecessary Cast
    }
 
    static void Foo(bool a, bool b) { }
}
]]>
</code>

            Await TestAsync(input, expected)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Simplification)>
        <WorkItem(529787)>
        Public Async Function TestCSharp_DoNotRemove_RequiredCastInCollectionInitializer() As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document><![CDATA[
using System;
using System.Collections.Generic;
 
class X : List<int>
{
    void Add(object x) { Console.WriteLine(1); }
    void Add(string x) { Console.WriteLine(2); }
 
    static void Main()
    {
        var z = new X { {|Simplify:(object)""|} };
    }
}
]]>
        </Document>
    </Project>
</Workspace>

            Dim expected =
<code><![CDATA[
using System;
using System.Collections.Generic;
 
class X : List<int>
{
    void Add(object x) { Console.WriteLine(1); }
    void Add(string x) { Console.WriteLine(2); }
 
    static void Main()
    {
        var z = new X { (object)"" };
    }
}
]]>
</code>

            Await TestAsync(input, expected)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Simplification)>
        <WorkItem(923296)>
        Public Async Function TestCSharp_DoNotRemove_RequiredCastInIfCondition() As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document><![CDATA[
class Program
{
    public static int Main()
    {
        object a = false, b = false;

        if ({|Simplify:(bool)a|})
        {
            return {|Simplify:(bool)b|} ? 0 : 1;
        }
        else if ({|Simplify:(bool)b|})
        {
            return 2;
        }
        else
        {
            return 3;
        }
    }
}
]]>
        </Document>
    </Project>
</Workspace>

            Dim expected =
<code><![CDATA[
class Program
{
    public static int Main()
    {
        object a = false, b = false;

        if ((bool)a)
        {
            return (bool)b ? 0 : 1;
        }
        else if ((bool)b)
        {
            return 2;
        }
        else
        {
            return 3;
        }
    }
}
]]>
</code>

            Await TestAsync(input, expected)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Simplification)>
        <WorkItem(995855)>
        Public Async Function TestCSharp_DoNotRemove_RequiredCastInConditionalExpression() As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document><![CDATA[
using System;
class C
{
  static void Main(string[] args)
  {
        byte s = 0;
        int i = 0;
        s += i == 0 ? {|Simplify:(byte)0|} : {|Simplify:(byte)0|};
    }
}
]]>
        </Document>
    </Project>
</Workspace>

            Dim expected =
<code><![CDATA[
using System;
class C
{
  static void Main(string[] args)
  {
        byte s = 0;
        int i = 0;
        s += i == 0 ? (byte)0 : (byte)0;
    }
}
]]>
</code>

            Await TestAsync(input, expected)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Simplification)>
        <WorkItem(995855)>
        Public Async Function TestCSharp_DoNotRemove_RequiredCastInConversion() As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document><![CDATA[
using System;
class C
{
    static void Main(string[] args)
    {
        byte b = 254;
        ushort u = (ushort){|Simplify:(sbyte)b|};
    }
}
]]>
        </Document>
    </Project>
</Workspace>

            Dim expected =
<code><![CDATA[
using System;
class C
{
    static void Main(string[] args)
    {
        byte b = 254;
        ushort u = (ushort)(sbyte)b;
    }
}
]]>
</code>

            Await TestAsync(input, expected)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Simplification)>
        <WorkItem(1007371)>
        Public Async Function TestCSharp_Remove_UnnecessaryCastAndParens() As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document><![CDATA[
class Program
{
    bool F(string a, string b)
    {
        return {|Simplify:(object)a == (object)b ? true : false|};
    }
}
]]>
        </Document>
    </Project>
</Workspace>

            Dim expected =
<code><![CDATA[
class Program
{
    bool F(string a, string b)
    {
        return a == (object)b ? true : false;
    }
}
]]>
</code>

            Await TestAsync(input, expected)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Simplification)>
        <WorkItem(1067214)>
        Public Async Function TestCSharp_Remove_UnnecessaryCastInExpressionBody_Property() As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document><![CDATA[
class Program
{
    public int X => {|Simplify:(int)0|};
}
]]>
        </Document>
    </Project>
</Workspace>

            Dim expected =
<code><![CDATA[
class Program
{
    public int X => 0;
}
]]>
</code>

            Await TestAsync(input, expected)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Simplification)>
        <WorkItem(1067214)>
        Public Async Function TestCSharp_Remove_UnnecessaryCastInExpressionBody_Method() As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document><![CDATA[
class Program
{
    public int X() => {|Simplify:(int)0|};
}
]]>
        </Document>
    </Project>
</Workspace>

            Dim expected =
<code><![CDATA[
class Program
{
    public int X() => 0;
}
]]>
</code>

            Await TestAsync(input, expected)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Simplification)>
        <WorkItem(253, "https://github.com/dotnet/roslyn/issues/253")>
        Public Async Function TestCSharp_DoNotRemove_NecessaryCastInConditionAccess() As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document><![CDATA[
public class Class1
{
    static void Test(object arg)
    {
        var identity = ({|Simplify:(B)arg|})?.A ?? (A)arg;
    }
}

class A { }
class B
{
    public A A { get { return null; } }
}
]]>
        </Document>
    </Project>
</Workspace>

            Dim expected =
<code><![CDATA[
public class Class1
{
    static void Test(object arg)
    {
        var identity = ((B)arg)?.A ?? (A)arg;
    }
}

class A { }
class B
{
    public A A { get { return null; } }
}
]]>
</code>

            Await TestAsync(input, expected)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Simplification)>
        Public Async Function TestCSharp_DoNotRemove_NecessaryCastInConditionalExpression() As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document><![CDATA[
public struct Subject<T>
{
    private readonly T _value;
    public Subject(T value)
    : this()
    {
        _value = value;
    }
    public T Value
    {
        get { return _value; }
    }
    public Subject<TResult>? Is<TResult>() where TResult : T
    {
        return _value is TResult ? {|Simplify:(Subject<TResult>?)|}new Subject<TResult>((TResult)_value) : null;
    }
}
]]>
        </Document>
    </Project>
</Workspace>

            Dim expected =
<code><![CDATA[
public struct Subject<T>
{
    private readonly T _value;
    public Subject(T value)
    : this()
    {
        _value = value;
    }
    public T Value
    {
        get { return _value; }
    }
    public Subject<TResult>? Is<TResult>() where TResult : T
    {
        return _value is TResult ? (Subject<TResult>?)new Subject<TResult>((TResult)_value) : null;
    }
}
]]>
</code>

            Await TestAsync(input, expected)
        End Function

        <WorkItem(4531, "https://github.com/dotnet/roslyn/issues/4531")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Simplification)>
        Public Async Function TestCSharp_DoNotRemove_NecessaryCastFromShortToUShort() As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document><![CDATA[
class C
{
    int M(short x)
    {
        return {|Simplify:(ushort)x|};
    }
}
]]>
        </Document>
    </Project>
</Workspace>

            Dim expected =
<code><![CDATA[
class C
{
    int M(short x)
    {
        return (ushort)x;
    }
}
]]>
</code>

            Await TestAsync(input, expected)
        End Function

        <WorkItem(4531, "https://github.com/dotnet/roslyn/issues/4531")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Simplification)>
        Public Async Function TestCSharp_DoNotRemove_NecessaryCastFromSByteToByte() As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document><![CDATA[
class C
{
    int M(sbyte x)
    {
        return {|Simplify:(byte)x|};
    }
}
]]>
        </Document>
    </Project>
</Workspace>

            Dim expected =
<code><![CDATA[
class C
{
    int M(sbyte x)
    {
        return (byte)x;
    }
}
]]>
</code>

            Await TestAsync(input, expected)
        End Function

        <WorkItem(4531, "https://github.com/dotnet/roslyn/issues/4531")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Simplification)>
        Public Async Function TestCSharp_DoNotRemove_NecessaryCastFromIntToUInt() As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document><![CDATA[
class C
{
    long M(int x)
    {
        return {|Simplify:(uint)x|};
    }
}
]]>
        </Document>
    </Project>
</Workspace>

            Dim expected =
<code><![CDATA[
class C
{
    long M(int x)
    {
        return (uint)x;
    }
}
]]>
</code>

            Await TestAsync(input, expected)
        End Function

        <WorkItem(4531, "https://github.com/dotnet/roslyn/issues/4531")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Simplification)>
        Public Async Function TestCSharp_Remove_UnnecessaryCastFromSByteToShort() As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document><![CDATA[
class C
{
    int M(sbyte x)
    {
        return {|Simplify:(short)x|};
    }
}
]]>
        </Document>
    </Project>
</Workspace>

            Dim expected =
<code><![CDATA[
class C
{
    int M(sbyte x)
    {
        return x;
    }
}
]]>
</code>

            Await TestAsync(input, expected)
        End Function

        <WorkItem(4531, "https://github.com/dotnet/roslyn/issues/4531")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Simplification)>
        Public Async Function TestCSharp_Remove_UnnecessaryCastFromByteToUShort() As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document><![CDATA[
class C
{
    int M(byte x)
    {
        return {|Simplify:(ushort)x|};
    }
}
]]>
        </Document>
    </Project>
</Workspace>

            Dim expected =
<code><![CDATA[
class C
{
    int M(byte x)
    {
        return x;
    }
}
]]>
</code>

            Await TestAsync(input, expected)
        End Function

        <WorkItem(4531, "https://github.com/dotnet/roslyn/issues/4531")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Simplification)>
        Public Async Function TestCSharp_Remove_UnnecessaryCastFromUShortToUInt() As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document><![CDATA[
class C
{
    long M(ushort x)
    {
        return {|Simplify:(uint)x|};
    }
}
]]>
        </Document>
    </Project>
</Workspace>

            Dim expected =
<code><![CDATA[
class C
{
    long M(ushort x)
    {
        return x;
    }
}
]]>
</code>

            Await TestAsync(input, expected)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Simplification)>
        <WorkItem(4037, "https://github.com/dotnet/roslyn/issues/4037")>
        Public Async Function TestCSharp_DoNotRemove_NecessaryCastOfEnumFromInInterpolation() As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document><![CDATA[
using System;

class Sample 
{
    public enum State: ushort
    {
        None = 0x00,
        State1 = 1 << 0,
    }

    public static void Main() 
    {
        State alarmState = State.State1;

        string str = $"State: {alarmState} [{{|Simplify:(ushort)alarmState|}:X4}]";

        Console.WriteLine(str);
    }
}
]]>
        </Document>
    </Project>
</Workspace>

            Dim expected =
<code><![CDATA[
using System;

class Sample 
{
    public enum State: ushort
    {
        None = 0x00,
        State1 = 1 << 0,
    }

    public static void Main() 
    {
        State alarmState = State.State1;

        string str = $"State: {alarmState} [{(ushort)alarmState:X4}]";

        Console.WriteLine(str);
    }
}
]]>
</code>

            Await TestAsync(input, expected)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Simplification)>
        <WorkItem(4037, "https://github.com/dotnet/roslyn/issues/4037")>
        Public Async Function TestCSharp_DoNotRemove_NecessaryCastOfEnumAndToString() As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document><![CDATA[
using System;

class Sample 
{
    public enum State: ushort
    {
        None = 0x00,
        State1 = 1 << 0,
    }

    public static void Main() 
    {
        State alarmState = State.State1;

        string str = ({|Simplify:(ushort)alarmState|}).ToString("X4");

        Console.WriteLine(str);
    }
}
]]>
        </Document>
    </Project>
</Workspace>

            Dim expected =
<code><![CDATA[
using System;

class Sample 
{
    public enum State: ushort
    {
        None = 0x00,
        State1 = 1 << 0,
    }

    public static void Main() 
    {
        State alarmState = State.State1;

        string str = ((ushort)alarmState).ToString("X4");

        Console.WriteLine(str);
    }
}
]]>
</code>

            Await TestAsync(input, expected)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Simplification)>
        <WorkItem(4037, "https://github.com/dotnet/roslyn/issues/4037")>
        Public Async Function TestCSharp_DoNotRemove_NecessaryCastOfEnum() As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document><![CDATA[
using System;

class Sample 
{
    public enum State: ushort
    {
        None = 0x00,
        State1 = 1 << 0,
    }

    public static void Main() 
    {
        State alarmState = State.State1;

        ushort val = {|Simplify:(ushort)alarmState|};

        Console.WriteLine(val);
    }
}
]]>
        </Document>
    </Project>
</Workspace>

            Dim expected =
<code><![CDATA[
using System;

class Sample 
{
    public enum State: ushort
    {
        None = 0x00,
        State1 = 1 << 0,
    }

    public static void Main() 
    {
        State alarmState = State.State1;

        ushort val = (ushort)alarmState;

        Console.WriteLine(val);
    }
}
]]>
</code>

            Await TestAsync(input, expected)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Simplification)>
        <WorkItem(4037, "https://github.com/dotnet/roslyn/issues/4037")>
        Public Async Function TestCSharp_Remove_UnnecessaryCastOfUShort() As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document><![CDATA[
class C
{
    void M() 
    {
        ushort x = 400;
        var s = $"Hello {{|Simplify:(object)x|}:x4}";
        System.Console.WriteLine(s);
    }
}
]]>
        </Document>
    </Project>
</Workspace>

            Dim expected =
<code><![CDATA[
class C
{
    void M() 
    {
        ushort x = 400;
        var s = $"Hello {x:x4}";
        System.Console.WriteLine(s);
    }
}
]]>
</code>

            Await TestAsync(input, expected)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Simplification)>
        <WorkItem(4037, "https://github.com/dotnet/roslyn/issues/4037")>
        Public Async Function TestCSharp_Remove_UnnecessaryCastOfDateTime1() As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document><![CDATA[
class C
{
    void M() 
    {
        var d = new System.DateTime(2015, 9, 8);
        var s = $"Hello {{|Simplify:(object)d|}:yyyy-MM-dd}";
        System.Console.WriteLine(s);
    }
}
]]>
        </Document>
    </Project>
</Workspace>

            Dim expected =
<code><![CDATA[
class C
{
    void M() 
    {
        var d = new System.DateTime(2015, 9, 8);
        var s = $"Hello {d:yyyy-MM-dd}";
        System.Console.WriteLine(s);
    }
}
]]>
</code>

            Await TestAsync(input, expected)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Simplification)>
        <WorkItem(4037, "https://github.com/dotnet/roslyn/issues/4037")>
        Public Async Function TestCSharp_Remove_UnnecessaryCastOfDateTime2() As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document><![CDATA[
class C
{
    void M() 
    {
        var d = new System.DateTime(2015, 9, 8);
        var s = $"Hello {({|Simplify:(object)d|}):yyyy-MM-dd}";
        System.Console.WriteLine(s);
    }
}
]]>
        </Document>
    </Project>
</Workspace>

            Dim expected =
<code><![CDATA[
class C
{
    void M() 
    {
        var d = new System.DateTime(2015, 9, 8);
        var s = $"Hello {(d):yyyy-MM-dd}";
        System.Console.WriteLine(s);
    }
}
]]>
</code>

            Await TestAsync(input, expected)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Simplification)>
        <WorkItem(5314, "https://github.com/dotnet/roslyn/issues/5314")>
        Public Async Function TestCSharp_DontRemove_NecessaryCastToObjectInConditionalExpression() As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document><![CDATA[
class C
{
    object M(bool cond, ulong value) 
    {
        return cond ? {|Simplify:(object)(uint)value|} : value;
    }
}
]]>
        </Document>
    </Project>
</Workspace>

            Dim expected =
<code><![CDATA[
class C
{
    object M(bool cond, ulong value) 
    {
        return cond ? (object)(uint)value : value;
    }
}
]]>
</code>

            Await TestAsync(input, expected)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Simplification)>
        <WorkItem(6490, "https://github.com/dotnet/roslyn/issues/6490")>
        Public Async Function TestCSharp_DontRemove_NecessaryCastOfLambdaToDelegateWithDynamic() As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document><![CDATA[
using System;
using System.Dynamic;
class C
{
    void M() 
    {
        dynamic d = new ExpandoObject();
        d.MyFunc = {|Simplify:(Func<int>)(() => 0)|};
    }
}
]]>
        </Document>
    </Project>
</Workspace>

            Dim expected =
<code><![CDATA[
using System;
using System.Dynamic;
class C
{
    void M() 
    {
        dynamic d = new ExpandoObject();
        d.MyFunc = (Func<int>)(() => 0);
    }
}
]]>
</code>

            Await TestAsync(input, expected)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Simplification)>
        <WorkItem(6966, "https://github.com/dotnet/roslyn/issues/6966")>
        Public Async Function TestCSharp_DontRemove_NecessaryCastToNullInImplicitlyTypedArray() As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document><![CDATA[
class C
{
    object M()
    {
        return new
        {
            Something = new[] { {|Simplify:(object)null|}, null, null, null }
        };
    }
}
]]>
        </Document>
    </Project>
</Workspace>

            Dim expected =
<code><![CDATA[
class C
{
    object M()
    {
        return new
        {
            Something = new[] { (object)null, null, null, null }
        };
    }
}
]]>
</code>

            Await TestAsync(input, expected)
        End Function

#End Region

#Region "Visual Basic tests"

        <WpfFact, Trait(Traits.Feature, Traits.Features.Simplification)>
        Public Async Function TestVisualBasic_DontRemove_IntToObj_Overloads1() As Task
            Dim input =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document>
Class C
    Sub Foo(o As Object)
    End Sub
    Sub Foo(i As Integer)
    End Sub

    Sub Test()
        Foo({|Simplify:CObj(1)|})
    End Sub
End Class
        </Document>
    </Project>
</Workspace>

            Dim expected =
<code>
Class C
    Sub Foo(o As Object)
    End Sub
    Sub Foo(i As Integer)
    End Sub

    Sub Test()
        Foo(CObj(1))
    End Sub
End Class
</code>

            Await TestAsync(input, expected)

        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Simplification)>
        Public Async Function TestVisualBasic_DontRemove_IntToLng_Overloads2() As Task
            Dim input =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document>
Class C
    Sub Foo(l As Long)
    End Sub
    Sub Foo(i As Integer)
    End Sub

    Sub Test()
        Foo({|Simplify:CLng(1)|})
    End Sub
End Class
        </Document>
    </Project>
</Workspace>

            Dim expected =
<code>
Class C
    Sub Foo(l As Long)
    End Sub
    Sub Foo(i As Integer)
    End Sub

    Sub Test()
        Foo(CLng(1))
    End Sub
End Class
</code>

            Await TestAsync(input, expected)

        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Simplification)>
        Public Async Function TestVisualBasic_Remove_IntToByte() As Task
            Dim input =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document>
Class C
    Sub M()
        Dim b As Integer = {|Simplify:CByte(0)|}
    End Sub
End Class
        </Document>
    </Project>
</Workspace>

            Dim expected =
<code>
Class C
    Sub M()
        Dim b As Integer = 0
    End Sub
End Class
</code>

            Await TestAsync(input, expected)

        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Simplification)>
        Public Async Function TestVisualBasic_Remove_IntToByteToInferred() As Task
            Dim input =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document>
Class C
    Sub M()
        Dim b = {|Simplify:CByte(0)|}
    End Sub
End Class
        </Document>
    </Project>
</Workspace>

            Dim expected =
<code>
Class C
    Sub M()
        Dim b = CByte(0)
    End Sub
End Class
</code>

            Await TestAsync(input, expected)

        End Function

        <WorkItem(530080)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Simplification)>
        Public Async Function TestVisualBasic_DoNotRemove_ForEachExpression() As Task
            ' Cast removal will change the GetEnumerator method being invoked.

            Dim input =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document>
Imports System
Class Program
    Shared Sub Main()
        Dim o As Object = {"1"}
        For Each i In {|Simplify:CType(o, Array)|}
            Console.WriteLine(i)
        Next
    End Sub
End Class
        </Document>
    </Project>
</Workspace>

            Dim expected =
<code>
Imports System
Class Program
    Shared Sub Main()
        Dim o As Object = {"1"}
        For Each i In CType(o, Array)
            Console.WriteLine(i)
        Next
    End Sub
End Class
</code>

            Await TestAsync(input, expected)

        End Function

        <WorkItem(529954)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Simplification)>
        Public Async Function TestVisualBasic_Remove_InsideCollectionInitializer() As Task
            Dim input =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document>
Imports System
Imports System.Collections.Generic
Class Program
    Shared Sub Main()
        Dim col = New List(Of Double) From {{|Simplify:CType(1, Double)|}}
    End Sub
End Class
        </Document>
    </Project>
</Workspace>

            Dim expected =
<code>
Imports System
Imports System.Collections.Generic
Class Program
    Shared Sub Main()
        Dim col = New List(Of Double) From {1}
    End Sub
End Class
</code>

            Await TestAsync(input, expected)

        End Function

        <WorkItem(530083)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Simplification)>
        Public Async Function TestVisualBasic_DoNotRemove_InsideThrowStatement() As Task
            Dim input =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document>
Imports System
Class C
    Shared Sub Main()
        Dim ex As Object = New Exception()
        Throw {|Simplify:DirectCast(ex, Exception)|}
    End Sub
End Class
        </Document>
    </Project>
</Workspace>

            Dim expected =
<code>
Imports System
Class C
    Shared Sub Main()
        Dim ex As Object = New Exception()
        Throw DirectCast(ex, Exception)
    End Sub
End Class
</code>

            Await TestAsync(input, expected)

        End Function

        <WorkItem(530083)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Simplification)>
        Public Async Function TestVisualBasic_Remove_InsideThrowStatement() As Task
            Dim input =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document>
Imports System
Class C
    Shared Sub Main()
        Dim ex = New ArgumentException()
        Throw {|Simplify:DirectCast(ex, Exception)|}
    End Sub
End Class
        </Document>
    </Project>
</Workspace>

            Dim expected =
<code>
Imports System
Class C
    Shared Sub Main()
        Dim ex = New ArgumentException()
        Throw ex
    End Sub
End Class
</code>

            Await TestAsync(input, expected)

        End Function

        <WorkItem(530083)>
        <WorkItem(2761, "https://github.com/dotnet/roslyn/issues/2761")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Simplification)>
        Public Async Function TestVisualBasic_DontRemove_InsideThrowStatement2() As Task
            ' We can't remove cast from base to derived, as we cannot be sure that the cast will succeed at runtime.
            Dim input =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document>
Imports System
Class C
    Shared Sub Main()
        Dim ex As Exception = New ArgumentException()
        Throw {|Simplify:DirectCast(ex, ArgumentException)|}
    End Sub
End Class
        </Document>
    </Project>
</Workspace>

            Dim expected =
<code>
Imports System
Class C
    Shared Sub Main()
        Dim ex As Exception = New ArgumentException()
        Throw DirectCast(ex, ArgumentException)
    End Sub
End Class
</code>

            Await TestAsync(input, expected)

        End Function

        <WorkItem(530931)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Simplification)>
        Public Async Function TestVisualBasic_DoNotRemove_InsideLateBoundInvocation() As Task
            Dim input =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document>
Option Strict Off
Option Infer On
Imports System

Module M
    Sub Main()
        Try
            Dim x = 1
            Foo({|Simplify:CObj(x)|})
        Catch
            Console.WriteLine("Catch")
        End Try
    End Sub
    Sub Foo(Of T, S)(x As Func(Of T))
    End Sub
End Module
        </Document>
    </Project>
</Workspace>

            Dim expected =
<code>
Option Strict Off
Option Infer On
Imports System

Module M
    Sub Main()
        Try
            Dim x = 1
            Foo(CObj(x))
        Catch
            Console.WriteLine("Catch")
        End Try
    End Sub
    Sub Foo(Of T, S)(x As Func(Of T))
    End Sub
End Module
</code>

            Await TestAsync(input, expected)

        End Function

        <WorkItem(604316)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Simplification)>
        Public Async Function TestVisualBasic_DoNotRemove_RequiredDefaultValueConversionToDate() As Task
            Dim input =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document>
Imports System
Class X
    Sub Foo(Optional x As Object = {|Simplify:CDate(Nothing)|})
        Console.WriteLine(x)
    End Sub

    Public Shared Sub Main()
        Dim y = New X()
        y.Foo()
    End Sub
End Class
        </Document>
    </Project>
</Workspace>

            Dim expected =
<code>
Imports System
Class X
    Sub Foo(Optional x As Object = CDate(Nothing))
        Console.WriteLine(x)
    End Sub

    Public Shared Sub Main()
        Dim y = New X()
        y.Foo()
    End Sub
End Class
</code>

            Await TestAsync(input, expected)

        End Function

        <WorkItem(604316)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Simplification)>
        Public Async Function TestVisualBasic_DoNotRemove_RequiredDefaultValueConversionToNumericType() As Task
            Dim input =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document>
Imports System
Class X
    Sub Foo(Optional x As Object = {|Simplify:CInt(Nothing)|})
        Console.WriteLine(x)
    End Sub

    Public Shared Sub Main()
        Dim y = New X()
        y.Foo()
    End Sub
End Class
        </Document>
    </Project>
</Workspace>

            Dim expected =
<code>
Imports System
Class X
    Sub Foo(Optional x As Object = CInt(Nothing))
        Console.WriteLine(x)
    End Sub

    Public Shared Sub Main()
        Dim y = New X()
        y.Foo()
    End Sub
End Class
</code>

            Await TestAsync(input, expected)

        End Function

        <WorkItem(604316)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Simplification)>
        Public Async Function TestVisualBasic_DoNotRemove_RequiredDefaultValueConversionToBooleanType() As Task
            Dim input =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document>
Imports System
Class X
    Sub Foo()
        Dim x As Object = {|Simplify:DirectCast(Nothing, Boolean)|}
        Console.WriteLine(x)
    End Sub

    Public Shared Sub Main()
        Dim y = New X()
        y.Foo()
    End Sub
End Class
        </Document>
    </Project>
</Workspace>

            Dim expected =
<code>
Imports System
Class X
    Sub Foo()
        Dim x As Object = DirectCast(Nothing, Boolean)
        Console.WriteLine(x)
    End Sub

    Public Shared Sub Main()
        Dim y = New X()
        y.Foo()
    End Sub
End Class
</code>

            Await TestAsync(input, expected)

        End Function

        <WorkItem(604316)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Simplification)>
        Public Async Function TestVisualBasic_Remove_UnnecessaryDefaultValueConversionToDate() As Task
            Dim input =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document>
Imports System
Class X
    Sub Foo(Optional x As DateTime = {|Simplify:CDate(Nothing)|})
        Console.WriteLine(x)
    End Sub

    Public Shared Sub Main()
        Dim y = New X()
        y.Foo()
    End Sub
End Class
        </Document>
    </Project>
</Workspace>

            Dim expected =
<code>
Imports System
Class X
    Sub Foo(Optional x As DateTime = Nothing)
        Console.WriteLine(x)
    End Sub

    Public Shared Sub Main()
        Dim y = New X()
        y.Foo()
    End Sub
End Class
</code>

            Await TestAsync(input, expected)

        End Function

        <WorkItem(604316)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Simplification)>
        Public Async Function TestVisualBasic_Remove_UnnecessaryDefaultValueConversionToNumericType() As Task
            Dim input =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document>
Imports System
Class X
    Sub Foo(Optional x As Double = {|Simplify:CInt(Nothing)|})
        Console.WriteLine(x)
    End Sub

    Public Shared Sub Main()
        Dim y = New X()
        y.Foo()
    End Sub
End Class
        </Document>
    </Project>
</Workspace>

            Dim expected =
<code>
Imports System
Class X
    Sub Foo(Optional x As Double = Nothing)
        Console.WriteLine(x)
    End Sub

    Public Shared Sub Main()
        Dim y = New X()
        y.Foo()
    End Sub
End Class
</code>

            Await TestAsync(input, expected)

        End Function

        <WorkItem(604316)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Simplification)>
        Public Async Function TestVisualBasic_Remove_UnnecessaryDefaultValueConversionToBooleanType() As Task
            Dim input =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document>
Imports System
Class X
    Sub Foo()
        Dim x As Integer = {|Simplify:DirectCast(Nothing, Boolean)|}
        Console.WriteLine(x)
    End Sub

    Public Shared Sub Main()
        Dim y = New X()
        y.Foo()
    End Sub
End Class
        </Document>
    </Project>
</Workspace>

            Dim expected =
<code>
Imports System
Class X
    Sub Foo()
        Dim x As Integer = Nothing
        Console.WriteLine(x)
    End Sub

    Public Shared Sub Main()
        Dim y = New X()
        y.Foo()
    End Sub
End Class
</code>

            Await TestAsync(input, expected)

        End Function

        <WorkItem(529956)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Simplification)>
        Public Async Function TestVisualBasic_DoNotRemove_NecessaryCastInForEachExpression() As Task
            Dim input =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document>
Imports System.Collections

Class C
	Private Shared Sub Main()
		For Each x As C In {|Simplify:DirectCast(New String() {Nothing}, IEnumerable)|}
			Console.WriteLine(x Is Nothing)
		Next
	End Sub

	Public Shared Widening Operator CType(s As String) As C
		Return New C()
	End Operator
End Class
        </Document>
    </Project>
</Workspace>

            Dim expected =
<code>
Imports System.Collections

Class C
	Private Shared Sub Main()
		For Each x As C In DirectCast(New String() {Nothing}, IEnumerable)
			Console.WriteLine(x Is Nothing)
		Next
	End Sub

	Public Shared Widening Operator CType(s As String) As C
		Return New C()
	End Operator
End Class
</code>

            Await TestAsync(input, expected)

        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Simplification)>
        <WorkItem(529968)>
        Public Async Function TestVisualBasic_DoNotRemove_NecessaryCastForParamsArgument() As Task
            Dim input =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document><![CDATA[
Imports System

Class A
    Public Shared Sub Main()
        Foo({|Simplify:DirectCast(New A(), Object)|})
    End Sub

    Private Shared Sub Foo(ParamArray x As Object())
		Console.WriteLine(x Is Nothing)
	End Sub

	Public Shared Widening Operator CType(a As A) As Object()
		Return Nothing
	End Operator
End Class
]]>
        </Document>
    </Project>
</Workspace>

            Dim expected =
<code><![CDATA[
Imports System

Class A
    Public Shared Sub Main()
        Foo(DirectCast(New A(), Object))
    End Sub

    Private Shared Sub Foo(ParamArray x As Object())
		Console.WriteLine(x Is Nothing)
	End Sub

	Public Shared Widening Operator CType(a As A) As Object()
		Return Nothing
	End Operator
End Class
]]>
</code>

            Await TestAsync(input, expected)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Simplification)>
        <WorkItem(529968)>
        Public Async Function TestVisualBasic_Remove_UnnecessaryCastsForParamsArguments() As Task
            Dim input =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document><![CDATA[
Imports System

Class A
    Public Shared Sub Main()
        Foo({|Simplify:DirectCast(New A(), Object)|}, {|Simplify:DirectCast(New A(), Object)|})
    End Sub

    Private Shared Sub Foo(ParamArray x As Object())
		Console.WriteLine(x Is Nothing)
	End Sub

	Public Shared Widening Operator CType(a As A) As Object()
		Return Nothing
	End Operator
End Class
]]>
        </Document>
    </Project>
</Workspace>

            Dim expected =
<code><![CDATA[
Imports System

Class A
    Public Shared Sub Main()
        Foo(New A(), New A())
    End Sub

    Private Shared Sub Foo(ParamArray x As Object())
		Console.WriteLine(x Is Nothing)
	End Sub

	Public Shared Widening Operator CType(a As A) As Object()
		Return Nothing
	End Operator
End Class
]]>
</code>

            Await TestAsync(input, expected)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Simplification)>
        <WorkItem(529985)>
        Public Async Function TestVisualBasic_DoNotRemove_NecessaryCastInMemberAccessExpression() As Task
            Dim input =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document><![CDATA[
Imports System
Class C
    Private Shared Sub Main()
        Dim c As C = Nothing
        Console.WriteLine({|Simplify:CType(c, Attribute)|}.GetType())
    End Sub

    Public Shared Widening Operator CType(x As C) As Attribute
        Return New ObsoleteAttribute()
    End Operator
End Class
]]>
        </Document>
    </Project>
</Workspace>

            Dim expected =
<code><![CDATA[
Imports System
Class C
    Private Shared Sub Main()
        Dim c As C = Nothing
        Console.WriteLine(CType(c, Attribute).GetType())
    End Sub

    Public Shared Widening Operator CType(x As C) As Attribute
        Return New ObsoleteAttribute()
    End Operator
End Class
]]>
</code>

            Await TestAsync(input, expected)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Simplification)>
        <WorkItem(529844)>
        Public Async Function TestVisualBasic_DoNotRemove_NecessaryCastInNumericConversion() As Task
            Dim input =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document><![CDATA[
Imports System

Class Program
	Private Shared Sub Main()
		Dim x As Integer = Integer.MaxValue
		Dim y As Double = x
		Dim z As Double = {|Simplify:CSng(x)|}
		Console.WriteLine(x)
		Console.WriteLine(y)
		Console.WriteLine(z)
		Console.WriteLine(y = z)
	End Sub
End Class
]]>
        </Document>
    </Project>
</Workspace>

            Dim expected =
<code><![CDATA[
Imports System

Class Program
	Private Shared Sub Main()
		Dim x As Integer = Integer.MaxValue
		Dim y As Double = x
		Dim z As Double = CSng(x)
		Console.WriteLine(x)
		Console.WriteLine(y)
		Console.WriteLine(z)
		Console.WriteLine(y = z)
	End Sub
End Class
]]>
</code>

            Await TestAsync(input, expected)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Simplification)>
        <WorkItem(529851)>
        Public Async Function TestVisualBasic_Remove_TryCast() As Task
            Dim input =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document><![CDATA[
Imports System
Interface I1
    Sub foo()
End Interface
Module Program
    Sub Main(args As String())
    End Sub
 
    Sub foo(o As I1)
        Dim i As I1 = {|Simplify:TryCast(o, I1)|}
    End Sub
End Module
]]>
        </Document>
    </Project>
</Workspace>

            Dim expected =
<code><![CDATA[
Imports System
Interface I1
    Sub foo()
End Interface
Module Program
    Sub Main(args As String())
    End Sub
 
    Sub foo(o As I1)
        Dim i As I1 = o
    End Sub
End Module
]]>
</code>

            Await TestAsync(input, expected)
        End Function

        <WorkItem(529919)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Simplification)>
        Public Async Function TestVisualBasic_Remove_DelegateVarianceConversions() As Task
            Dim input =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document>
Imports System

Class Program
    Private Shared Sub Main()
        Dim a As Action(Of Object) = AddressOf Console.WriteLine
        Dim b As Action(Of String) = {|Simplify:DirectCast(a, Action (Of String))|}
        Call {|Simplify:DirectCast(a, Action(Of String))|}("A")
        Call {|Simplify:DirectCast(a, Action(Of String))|}.Invoke("A")
    End Sub
End Class
        </Document>
    </Project>
</Workspace>

            Dim expected =
<code>
Imports System

Class Program
    Private Shared Sub Main()
        Dim a As Action(Of Object) = AddressOf Console.WriteLine
        Dim b As Action(Of String) = a
        Call a("A")
        Call a.Invoke("A")
    End Sub
End Class
</code>

            Await TestAsync(input, expected)

        End Function

        <WorkItem(529884)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Simplification)>
        Public Async Function TestVisualBasic_DoNotRemove_ParamDefaultValueNegativeZero() As Task
            Dim input =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document><![CDATA[
Imports System

Interface I
    Sub Foo(Optional x As Double = +0.0)
End Interface

NotInheritable Class C
    Implements I
    Public Sub Foo(Optional x As Double = -0.0) Implements I.Foo
        Console.WriteLine(1 / x > 0)
    End Sub

    Private Shared Sub Main()
        {|Simplify:DirectCast(New C(), I)|}.Foo()
    End Sub
End Class
]]>
        </Document>
    </Project>
</Workspace>

            Dim expected =
<code><![CDATA[
Imports System

Interface I
    Sub Foo(Optional x As Double = +0.0)
End Interface

NotInheritable Class C
    Implements I
    Public Sub Foo(Optional x As Double = -0.0) Implements I.Foo
        Console.WriteLine(1 / x > 0)
    End Sub

    Private Shared Sub Main()
        DirectCast(New C(), I).Foo()
    End Sub
End Class
]]>
</code>

            Await TestAsync(input, expected)

        End Function

        <WorkItem(529884)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Simplification)>
        Public Async Function TestVisualBasic_DoNotRemove_ParamDefaultValueNegativeZero2() As Task
            Dim input =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document><![CDATA[
Imports System

Interface I
    Sub Foo(Optional x As Double = -(-0.0))
End Interface

NotInheritable Class C
    Implements I
    Public Sub Foo(Optional x As Double = -0.0) Implements I.Foo
        Console.WriteLine(1 / x > 0)
    End Sub

    Private Shared Sub Main()
        {|Simplify:DirectCast(New C(), I)|}.Foo()
    End Sub
End Class
]]>
        </Document>
    </Project>
</Workspace>

            Dim expected =
<code><![CDATA[
Imports System

Interface I
    Sub Foo(Optional x As Double = -(-0.0))
End Interface

NotInheritable Class C
    Implements I
    Public Sub Foo(Optional x As Double = -0.0) Implements I.Foo
        Console.WriteLine(1 / x > 0)
    End Sub

    Private Shared Sub Main()
        DirectCast(New C(), I).Foo()
    End Sub
End Class
]]>
</code>

            Await TestAsync(input, expected)

        End Function

        <WorkItem(529884), WorkItem(529927)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Simplification)>
        Public Async Function TestVisualBasic_Remove_ParamDefaultValueZero() As Task
            Dim input =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document><![CDATA[
Imports System

Interface I
    Sub Foo(Optional x As Double = +0.0)
End Interface

NotInheritable Class C
    Implements I
    Public Sub Foo(Optional x As Double = -(-0.0)) Implements I.Foo
        Console.WriteLine(1 / x > 0)
    End Sub

    Private Shared Sub Main()
        Call {|Simplify:DirectCast(New C(), I)|}.Foo()
    End Sub
End Class
]]>
        </Document>
    </Project>
</Workspace>

            Dim expected =
<code><![CDATA[
Imports System

Interface I
    Sub Foo(Optional x As Double = +0.0)
End Interface

NotInheritable Class C
    Implements I
    Public Sub Foo(Optional x As Double = -(-0.0)) Implements I.Foo
        Console.WriteLine(1 / x > 0)
    End Sub

    Private Shared Sub Main()
        Call New C().Foo()
    End Sub
End Class
]]>
</code>

            Await TestAsync(input, expected)

        End Function

        <WorkItem(529791)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Simplification)>
        Public Async Function TestVisualBasic_Remove_UnnecessaryImplicitNullableCast() As Task
            Dim input =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document><![CDATA[
Class X
	Private Shared Sub Foo()
		Dim x As Object = {|Simplify:DirectCast(Nothing, String)|}
		Dim y As Object = {|Simplify:CType(Nothing, System.Nullable(Of Integer))|}
	End Sub
End Class
]]>
        </Document>
    </Project>
</Workspace>

            Dim expected =
<code><![CDATA[
Class X
	Private Shared Sub Foo()
		Dim x As Object = Nothing
		Dim y As Object = Nothing
	End Sub
End Class
]]>
</code>

            Await TestAsync(input, expected)

        End Function

        <WorkItem(529963)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Simplification)>
        Public Async Function TestVisualBasic_DoNotRemove_NecessaryCastInQueryForCollectionRangeVariable() As Task
            Dim input =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document><![CDATA[
Option Strict On
Imports System
Imports System.Collections.Generic

Module Module1
    Sub Main()
        Try
            Dim q1 As IEnumerable(Of Object) = From i In {1} Select o = {|Simplify:CObj(i)|}
        Catch
        Finally
        End Try
    End Sub
End Module
]]>
        </Document>
    </Project>
</Workspace>

            Dim expected =
<code><![CDATA[
Option Strict On
Imports System
Imports System.Collections.Generic

Module Module1
    Sub Main()
        Try
            Dim q1 As IEnumerable(Of Object) = From i In {1} Select o = CObj(i)
        Catch
        Finally
        End Try
    End Sub
End Module
]]>
</code>

            Await TestAsync(input, expected)

        End Function

        <WorkItem(530072)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Simplification)>
        Public Async Function TestVisualBasic_DoNotRemove_NecessaryCastInQueryForSelectMethod() As Task
            Dim input =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document><![CDATA[
Option Strict On
Imports System
Imports System.Collections.Generic

Class C
    Function [Select](x As Func(Of Integer, Long)) As String
        Return "Long"
    End Function
    Function [Select](x As Func(Of Integer, Integer)) As String
        Return "Integer"
    End Function
    Shared Sub Main()
        Dim query = From i In New C() Select {|Simplify:CType(i, Long)|}
        Console.WriteLine(query)
    End Sub
End Class
]]>
        </Document>
    </Project>
</Workspace>

            Dim expected =
<code><![CDATA[
Option Strict On
Imports System
Imports System.Collections.Generic

Class C
    Function [Select](x As Func(Of Integer, Long)) As String
        Return "Long"
    End Function
    Function [Select](x As Func(Of Integer, Integer)) As String
        Return "Integer"
    End Function
    Shared Sub Main()
        Dim query = From i In New C() Select CType(i, Long)
        Console.WriteLine(query)
    End Sub
End Class
]]>
</code>

            Await TestAsync(input, expected)

        End Function

        <WorkItem(529831)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Simplification)>
        Public Async Function TestVisualBasic_Remove_UnnecessaryInterfaceCast() As Task
            Dim input =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document><![CDATA[
Imports System

Interface IIncrementable
	ReadOnly Property Value() As Integer
	Sub Increment()
End Interface

Structure S
	Implements IIncrementable
	Public Property Value() As Integer Implements IIncrementable.Value
		Get
			Return m_Value
		End Get
		Private Set
			m_Value = Value
		End Set
	End Property
	Private m_Value As Integer
	Public Sub Increment() Implements IIncrementable.Increment
		Value += 1
	End Sub
End Structure

Class C
	Implements IIncrementable
	Public Property Value() As Integer Implements IIncrementable.Value
		Get
			Return m_Value
		End Get
		Private Set
			m_Value = Value
		End Set
	End Property
	Private m_Value As Integer
	Public Sub Increment() Implements IIncrementable.Increment
		Value += 1
	End Sub
End Class

NotInheritable Class Program
	Private Sub New()
	End Sub
	Private Shared Sub Main()
		Foo(New S(), New C(), New C(), New C())
	End Sub

    Private Shared Sub Foo(Of TAny As IIncrementable, TClass As {Class, IIncrementable, New}, TClass2 As IIncrementable, TClass3 As {TClass, TClass2})(x As TAny, y As TClass, z As TClass2, t As TClass3)
        Call {|Simplify:DirectCast(x, IIncrementable)|}.Increment() ' Necessary cast
        Call {|Simplify:DirectCast(y, IIncrementable)|}.Increment() ' Unnecessary Cast - OK
        Call {|Simplify:DirectCast(z, IIncrementable)|}.Increment() ' Necessary cast
        Call {|Simplify:DirectCast(t, IIncrementable)|}.Increment() ' Necessary cast

        Console.WriteLine(x.Value)
        Console.WriteLine(y.Value)
        Console.WriteLine(z.Value)
        Console.WriteLine(t.Value)
    End Sub
End Class
]]>
        </Document>
    </Project>
</Workspace>

            Dim expected =
<code><![CDATA[
Imports System

Interface IIncrementable
	ReadOnly Property Value() As Integer
	Sub Increment()
End Interface

Structure S
	Implements IIncrementable
	Public Property Value() As Integer Implements IIncrementable.Value
		Get
			Return m_Value
		End Get
		Private Set
			m_Value = Value
		End Set
	End Property
	Private m_Value As Integer
	Public Sub Increment() Implements IIncrementable.Increment
		Value += 1
	End Sub
End Structure

Class C
	Implements IIncrementable
	Public Property Value() As Integer Implements IIncrementable.Value
		Get
			Return m_Value
		End Get
		Private Set
			m_Value = Value
		End Set
	End Property
	Private m_Value As Integer
	Public Sub Increment() Implements IIncrementable.Increment
		Value += 1
	End Sub
End Class

NotInheritable Class Program
	Private Sub New()
	End Sub
	Private Shared Sub Main()
		Foo(New S(), New C(), New C(), New C())
	End Sub

    Private Shared Sub Foo(Of TAny As IIncrementable, TClass As {Class, IIncrementable, New}, TClass2 As IIncrementable, TClass3 As {TClass, TClass2})(x As TAny, y As TClass, z As TClass2, t As TClass3)
        Call DirectCast(x, IIncrementable).Increment() ' Necessary cast
        Call y.Increment() ' Unnecessary Cast - OK
        Call DirectCast(z, IIncrementable).Increment() ' Necessary cast
        Call DirectCast(t, IIncrementable).Increment() ' Necessary cast

        Console.WriteLine(x.Value)
        Console.WriteLine(y.Value)
        Console.WriteLine(z.Value)
        Console.WriteLine(t.Value)
    End Sub
End Class
]]>
</code>

            Await TestAsync(input, expected)

        End Function

        <WorkItem(529877)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Simplification)>
        Public Async Function TestVisualBasic_Remove_UnnecessarySealedClassToInterfaceCast() As Task
            Dim input =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document><![CDATA[
Imports System

Class C
    Implements IDisposable
    Public Sub Dispose() Implements IDisposable.Dispose
    End Sub
End Class

NotInheritable Class D
    Inherits C
    Private Shared Sub Main()
        Dim s As New D()
        Call {|Simplify:DirectCast(s, IDisposable)|}.Dispose()
    End Sub
End Class
]]>
        </Document>
    </Project>
</Workspace>

            Dim expected =
<code><![CDATA[
Imports System

Class C
    Implements IDisposable
    Public Sub Dispose() Implements IDisposable.Dispose
    End Sub
End Class

NotInheritable Class D
    Inherits C
    Private Shared Sub Main()
        Dim s As New D()
        Call s.Dispose()
    End Sub
End Class
]]>
</code>

            Await TestAsync(input, expected)

        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Simplification)>
        <WorkItem(529887)>
        Public Async Function TestVisualBasic_Remove_UnnecessaryReadOnlyValueTypeToInterfaceCast() As Task
            Dim input =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document><![CDATA[
Imports System

Interface IIncrementable
    ReadOnly Property Value() As Integer
    Sub Increment()
End Interface

Structure S
    Implements IIncrementable
    Public Property Value() As Integer Implements IIncrementable.Value
        Get
            Return m_Value
        End Get
        Private Set
            m_Value = Value
        End Set
    End Property
    Private m_Value As Integer
    Public Sub Increment() Implements IIncrementable.Increment
        Value += 1
    End Sub

    Shared ReadOnly s As New S()

    Private Shared Sub Main()
        ' Note: readonly modifier guarantees that a copy of a value type is always made before modification, so a boxing is not observable.

        Call {|Simplify:DirectCast(s, IIncrementable)|}.Increment()
        Console.WriteLine(s.Value)
    End Sub
End Structure
]]>
        </Document>
    </Project>
</Workspace>

            Dim expected =
<code><![CDATA[
Imports System

Interface IIncrementable
    ReadOnly Property Value() As Integer
    Sub Increment()
End Interface

Structure S
    Implements IIncrementable
    Public Property Value() As Integer Implements IIncrementable.Value
        Get
            Return m_Value
        End Get
        Private Set
            m_Value = Value
        End Set
    End Property
    Private m_Value As Integer
    Public Sub Increment() Implements IIncrementable.Increment
        Value += 1
    End Sub

    Shared ReadOnly s As New S()

    Private Shared Sub Main()
        ' Note: readonly modifier guarantees that a copy of a value type is always made before modification, so a boxing is not observable.

        Call s.Increment()
        Console.WriteLine(s.Value)
    End Sub
End Structure
]]>
</code>

            Await TestAsync(input, expected)

        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Simplification)>
        <WorkItem(529888)>
        Public Async Function TestVisualBasic_Remove_UnnecessaryObjectCreationToInterfaceCast() As Task
            Dim input =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document><![CDATA[
Imports System

Structure Y
    Implements IDisposable
    Public Sub Dispose() Implements IDisposable.Dispose
    End Sub
End Structure

Class X
    Implements IDisposable
    Private Shared Sub Main()
        Call {|Simplify:DirectCast(New X(), IDisposable)|}.Dispose()
        Call {|Simplify:DirectCast(New Y(), IDisposable)|}.Dispose()
    End Sub

    Public Sub Dispose() Implements IDisposable.Dispose
    End Sub
End Class]]>
        </Document>
    </Project>
</Workspace>

            Dim expected =
<code><![CDATA[
Imports System

Structure Y
    Implements IDisposable
    Public Sub Dispose() Implements IDisposable.Dispose
    End Sub
End Structure

Class X
    Implements IDisposable
    Private Shared Sub Main()
        Call New X().Dispose()
        Call New Y().Dispose()
    End Sub

    Public Sub Dispose() Implements IDisposable.Dispose
    End Sub
End Class
]]>
</code>

            Await TestAsync(input, expected)

        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Simplification)>
        <WorkItem(529912)>
        Public Async Function TestVisualBasic_Remove_UnnecessaryEffectivelySealedClassToInterfaceCast() As Task
            Dim input =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document><![CDATA[
Imports System

Class C
    Implements IDisposable
    Private Sub New()
    End Sub

    Public Sub Dispose() Implements IDisposable.Dispose
    End Sub

    Private Shared Sub Main()
        Dim x = New C()
        Call {|Simplify:DirectCast(x, IDisposable)|}.Dispose()
    End Sub
End Class
]]>
        </Document>
    </Project>
</Workspace>

            Dim expected =
<code><![CDATA[
Imports System

Class C
    Implements IDisposable
    Private Sub New()
    End Sub

    Public Sub Dispose() Implements IDisposable.Dispose
    End Sub

    Private Shared Sub Main()
        Dim x = New C()
        Call x.Dispose()
    End Sub
End Class
]]>
</code>

            Await TestAsync(input, expected)

        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Simplification)>
        <WorkItem(529912)>
        Public Async Function TestVisualBasic_Remove_UnnecessaryEffectivelySealedClassToInterfaceCast2() As Task
            Dim input =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document><![CDATA[
Imports System

Class C
    Implements IDisposable
    Private Sub New()
    End Sub

    Public Sub Dispose() Implements IDisposable.Dispose
    End Sub

    Private Shared Sub Main()
        Dim x = New C()
        Call {|Simplify:DirectCast(x, IDisposable)|}.Dispose()
    End Sub
End Class

Structure D
    Implements IDisposable
    Public Sub Dispose() Implements IDisposable.Dispose
    End Sub
End Structure

Class E
    Inherits C
    Implements IDisposable
    Public Overloads Sub Dispose() Implements IDisposable.Dispose
    End Sub
End Class
]]>
        </Document>
    </Project>
</Workspace>

            Dim expected =
<code><![CDATA[
Imports System

Class C
    Implements IDisposable
    Private Sub New()
    End Sub

    Public Sub Dispose() Implements IDisposable.Dispose
    End Sub

    Private Shared Sub Main()
        Dim x = New C()
        Call x.Dispose()
    End Sub
End Class

Structure D
    Implements IDisposable
    Public Sub Dispose() Implements IDisposable.Dispose
    End Sub
End Structure

Class E
    Inherits C
    Implements IDisposable
    Public Overloads Sub Dispose() Implements IDisposable.Dispose
    End Sub
End Class
]]>
</code>

            Await TestAsync(input, expected)

        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Simplification)>
        <WorkItem(529912)>
        Public Async Function TestVisualBasic_Remove_UnnecessaryEffectivelySealedClassToInterfaceCast3() As Task
            Dim input =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document><![CDATA[
Imports System

Class C
    Implements IDisposable
    Private Sub New()
    End Sub

    Public Sub Dispose() Implements IDisposable.Dispose
    End Sub

    Private Shared Sub Main()
        Dim x = New C()
        Call {|Simplify:DirectCast(x, IDisposable)|}.Dispose()
    End Sub

    Private Interface I
    End Interface
End Class

Structure D
    Implements IDisposable
    Public Sub Dispose() Implements IDisposable.Dispose
    End Sub
End Structure

Class E
    Inherits C
    Implements IDisposable
    Public Overloads Sub Dispose() Implements IDisposable.Dispose
    End Sub
End Class
]]>
        </Document>
    </Project>
</Workspace>

            Dim expected =
<code><![CDATA[
Imports System

Class C
    Implements IDisposable
    Private Sub New()
    End Sub

    Public Sub Dispose() Implements IDisposable.Dispose
    End Sub

    Private Shared Sub Main()
        Dim x = New C()
        Call x.Dispose()
    End Sub

    Private Interface I
    End Interface
End Class

Structure D
    Implements IDisposable
    Public Sub Dispose() Implements IDisposable.Dispose
    End Sub
End Structure

Class E
    Inherits C
    Implements IDisposable
    Public Overloads Sub Dispose() Implements IDisposable.Dispose
    End Sub
End Class
]]>
</code>

            Await TestAsync(input, expected)

        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Simplification)>
        <WorkItem(529913)>
        Public Async Function TestVisualBasic_Remove_UnnecessaryEffectivelySealedClassToInterface4() As Task
            Dim input =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document><![CDATA[
Imports System

Class A
    Private Class C
        Implements IDisposable
        Public Sub Dispose() Implements IDisposable.Dispose
        End Sub

        Private Shared Sub Main()
            Dim x = New C()
            Call {|Simplify:DirectCast(x, IDisposable)|}.Dispose()
        End Sub
    End Class
End Class
]]>
        </Document>
    </Project>
</Workspace>

            Dim expected =
<code><![CDATA[
Imports System

Class A
    Private Class C
        Implements IDisposable
        Public Sub Dispose() Implements IDisposable.Dispose
        End Sub

        Private Shared Sub Main()
            Dim x = New C()
            Call x.Dispose()
        End Sub
    End Class
End Class
]]>
</code>

            Await TestAsync(input, expected)

        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Simplification)>
        <WorkItem(529913)>
        Public Async Function TestVisualBasic_Remove_UnnecessaryEffectivelySealedClassToInterfaceCast5() As Task
            Dim input =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document><![CDATA[
Imports System

Class A
	Private Class C
		Implements IDisposable
		Public Sub Dispose() Implements IDisposable.Dispose
		End Sub

		Private Shared Sub Main()
			Dim x = New C()
			Call {|Simplify:DirectCast(x, IDisposable)|}.Dispose()
		End Sub
	End Class
End Class

Structure D
	Implements IDisposable
	Public Sub Dispose() Implements IDisposable.Dispose
	End Sub
End Structure

Class E
	Inherits C
	Implements IDisposable
	Public Sub Dispose() Implements IDisposable.Dispose
	End Sub
End Class
]]>
        </Document>
    </Project>
</Workspace>

            Dim expected =
<code><![CDATA[
Imports System

Class A
	Private Class C
		Implements IDisposable
		Public Sub Dispose() Implements IDisposable.Dispose
		End Sub

		Private Shared Sub Main()
			Dim x = New C()
			Call x.Dispose()
		End Sub
	End Class
End Class

Structure D
	Implements IDisposable
	Public Sub Dispose() Implements IDisposable.Dispose
	End Sub
End Structure

Class E
	Inherits C
	Implements IDisposable
	Public Sub Dispose() Implements IDisposable.Dispose
	End Sub
End Class
]]>
</code>

            Await TestAsync(input, expected)

        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Simplification)>
        <WorkItem(529912)>
        Public Async Function TestVisualBasic_DoNotRemove_NecessaryClassToInterfaceCast() As Task
            Dim input =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document><![CDATA[
Imports System

Class C
    Implements IDisposable
    Private Sub New()
    End Sub

    Public Sub Dispose() Implements IDisposable.Dispose
    End Sub

    Private Shared Sub Main()
        Dim x = New C()
        Call {|Simplify:DirectCast(x, IDisposable)|}.Dispose()
    End Sub

    Private Class E
        Inherits C
        Implements IDisposable
        Public Overloads Sub Dispose() Implements IDisposable.Dispose
        End Sub
    End Class
End Class

Structure D
    Implements IDisposable
    Public Sub Dispose() Implements IDisposable.Dispose
    End Sub
End Structure
]]>
        </Document>
    </Project>
</Workspace>

            Dim expected =
<code><![CDATA[
Imports System

Class C
    Implements IDisposable
    Private Sub New()
    End Sub

    Public Sub Dispose() Implements IDisposable.Dispose
    End Sub

    Private Shared Sub Main()
        Dim x = New C()
        Call DirectCast(x, IDisposable).Dispose()
    End Sub

    Private Class E
        Inherits C
        Implements IDisposable
        Public Overloads Sub Dispose() Implements IDisposable.Dispose
        End Sub
    End Class
End Class

Structure D
    Implements IDisposable
    Public Sub Dispose() Implements IDisposable.Dispose
    End Sub
End Structure
]]>
</code>

            Await TestAsync(input, expected)

        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Simplification)>
        <WorkItem(529913)>
        Public Async Function TestVisualBasic_DoNotRemove_NecessaryClassToInterfaceCast2() As Task
            Dim input =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document><![CDATA[
Class A
	Private Class C
		Implements IDisposable
		Public Sub Dispose() Implements IDisposable.Dispose
		End Sub

		Private Shared Sub Main()
			Dim x = New C()
			Call {|Simplify:DirectCast(x, IDisposable)|}.Dispose()
		End Sub

		Private Class E
			Inherits C
			Implements IDisposable
			Public Overloads Sub Dispose() Implements IDisposable.Dispose
			End Sub
		End Class
	End Class
End Class

Structure D
	Implements IDisposable
	Public Sub Dispose() Implements IDisposable.Dispose
	End Sub
End Structure
]]>
        </Document>
    </Project>
</Workspace>

            Dim expected =
<code><![CDATA[
Class A
	Private Class C
		Implements IDisposable
		Public Sub Dispose() Implements IDisposable.Dispose
		End Sub

		Private Shared Sub Main()
			Dim x = New C()
			Call DirectCast(x, IDisposable).Dispose()
		End Sub

		Private Class E
			Inherits C
			Implements IDisposable
			Public Overloads Sub Dispose() Implements IDisposable.Dispose
			End Sub
		End Class
	End Class
End Class

Structure D
	Implements IDisposable
	Public Sub Dispose() Implements IDisposable.Dispose
	End Sub
End Structure
]]>
</code>

            Await TestAsync(input, expected)

        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Simplification)>
        <WorkItem(529889)>
        Public Async Function TestVisualBasic_Remove_UnnecessaryCastFromImmutableValueTypeToInterface() As Task
            Dim input =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document><![CDATA[
Imports System

Class Program
    Private Shared Sub Main()
        Dim x As Integer = 1
        Dim y = {|Simplify:DirectCast(x, IComparable(Of Integer))|}.CompareTo(0)
    End Sub
End Class
]]>
        </Document>
    </Project>
</Workspace>

            Dim expected =
<code><![CDATA[
Imports System

Class Program
    Private Shared Sub Main()
        Dim x As Integer = 1
        Dim y = x.CompareTo(0)
    End Sub
End Class
]]>
</code>

            Await TestAsync(input, expected)

        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Simplification)>
        <WorkItem(529927)>
        Public Async Function TestVisualBasic_Remove_UnnecessaryCastFromImplementingClassToInterface() As Task
            Dim input =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document><![CDATA[
Interface I1
    Sub Foo()
End Interface

Class M
    Implements I1
    Shared Sub Main()
        Call {|Simplify:CType(New M(), I1)|}.Foo()
    End Sub
    Public Sub Foo() Implements I1.Foo
    End Sub
End Class
]]>
        </Document>
    </Project>
</Workspace>

            Dim expected =
<code><![CDATA[
Interface I1
    Sub Foo()
End Interface

Class M
    Implements I1
    Shared Sub Main()
        Call New M().Foo()
    End Sub
    Public Sub Foo() Implements I1.Foo
    End Sub
End Class
]]>
</code>

            Await TestAsync(input, expected)

        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Simplification)>
        <WorkItem(529988)>
        Public Async Function TestVisualBasic_DoNotRemove_NecessaryCastInDelegateCreationExpression() As Task
            ' Note: Removing the cast changes the lambda parameter type and invocation method symbol.

            Dim input =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document><![CDATA[
Imports System

Module Program
    Public Sub Main()
        Call DirectCast({|Simplify:DirectCast((Sub(y) Call New X().Foo(y)), Action(Of Object))|}, Action(Of String))("HI")
    End Sub

End Module

Public Class X
    Public Sub Foo(x As Object)
        Console.WriteLine(1)
    End Sub

    Public Sub Foo(x As String)
        Console.WriteLine(2)
    End Sub
End Class
]]>
        </Document>
    </Project>
</Workspace>

            Dim expected =
<code><![CDATA[
Imports System

Module Program
    Public Sub Main()
        Call DirectCast(DirectCast((Sub(y) Call New X().Foo(y)), Action(Of Object)), Action(Of String))("HI")
    End Sub

End Module

Public Class X
    Public Sub Foo(x As Object)
        Console.WriteLine(1)
    End Sub

    Public Sub Foo(x As String)
        Console.WriteLine(2)
    End Sub
End Class
]]>
</code>

            Await TestAsync(input, expected)

        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Simplification)>
        <WorkItem(529988)>
        Public Async Function TestVisualBasic_DoNotRemove_NecessaryCastInDelegateCreationExpression2() As Task
            ' Note: Removing the cast changes the lambda parameter type and invocation method symbol.

            Dim input =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document><![CDATA[
Imports System

Module Program
    Public Sub Main()
        Call DirectCast({|Simplify:DirectCast((Sub(y) 
                                                    Call New X().Foo(y)
                                               End Sub), Action(Of Object))|}, Action(Of String))("HI")
    End Sub

End Module

Public Class X
    Public Sub Foo(x As Object)
        Console.WriteLine(1)
    End Sub

    Public Sub Foo(x As String)
        Console.WriteLine(2)
    End Sub
End Class
]]>
        </Document>
    </Project>
</Workspace>

            Dim expected =
<code><![CDATA[
Imports System

Module Program
    Public Sub Main()
        Call DirectCast(DirectCast((Sub(y) 
                                                    Call New X().Foo(y)
                                               End Sub), Action(Of Object)), Action(Of String))("HI")
    End Sub

End Module

Public Class X
    Public Sub Foo(x As Object)
        Console.WriteLine(1)
    End Sub

    Public Sub Foo(x As String)
        Console.WriteLine(2)
    End Sub
End Class
]]>
</code>

            Await TestAsync(input, expected)

        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Simplification)>
        <WorkItem(529988)>
        Public Async Function TestVisualBasic_Remove_UnnecessaryCastInDelegateCreationExpression3() As Task
            ' Note: Removing the cast changes the lambda parameter type, but doesn't change the semantics of the lambda body.

            Dim input =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document><![CDATA[
Imports System

Module Program
    Public Sub Main()
        Call DirectCast({|Simplify:DirectCast((Sub(y) Call New X().Foo(1)), Action(Of Object))|}, Action(Of String))("HI")
    End Sub

End Module

Public Class X
    Public Sub Foo(x As Object)
        Console.WriteLine(1)
    End Sub

    Public Sub Foo(x As String)
        Console.WriteLine(2)
    End Sub
End Class
]]>
        </Document>
    </Project>
</Workspace>

            Dim expected =
<code><![CDATA[
Imports System

Module Program
    Public Sub Main()
        Call DirectCast((Sub(y) Call New X().Foo(1)), Action(Of String))("HI")
    End Sub

End Module

Public Class X
    Public Sub Foo(x As Object)
        Console.WriteLine(1)
    End Sub

    Public Sub Foo(x As String)
        Console.WriteLine(2)
    End Sub
End Class
]]>
</code>

            Await TestAsync(input, expected)

        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Simplification)>
        <WorkItem(529988)>
        Public Async Function TestVisualBasic_Remove_UnnecessaryCastInDelegateCreationExpression4() As Task
            ' Note: Removing the cast changes the lambda parameter type, but doesn't change the semantics of the lambda body.

            Dim input =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document><![CDATA[
Imports System

Module Program
    Public Sub Main()
        Call DirectCast({|Simplify:DirectCast((Sub(y) 
                                                    Call New X().Foo(1)
                                               End Sub), Action(Of Object))|}, Action(Of String))("HI")
    End Sub

End Module

Public Class X
    Public Sub Foo(x As Object)
        Console.WriteLine(1)
    End Sub

    Public Sub Foo(x As String)
        Console.WriteLine(2)
    End Sub
End Class
]]>
        </Document>
    </Project>
</Workspace>

            Dim expected =
<code><![CDATA[
Imports System

Module Program
    Public Sub Main()
        Call DirectCast((Sub(y) 
                                                    Call New X().Foo(1)
                                               End Sub), Action(Of String))("HI")
    End Sub

End Module

Public Class X
    Public Sub Foo(x As Object)
        Console.WriteLine(1)
    End Sub

    Public Sub Foo(x As String)
        Console.WriteLine(2)
    End Sub
End Class
]]>
</code>

            Await TestAsync(input, expected)

        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Simplification)>
        <WorkItem(529988)>
        Public Async Function TestVisualBasic_DoNotRemove_NecessaryCastInDelegateCreationExpression5() As Task
            ' Note: Removing the cast changes the lambda parameter type and hence changes the inferred type of lambda local "x".

            Dim input =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document><![CDATA[
Imports System

Module Program
    Public Sub Main()
        Call DirectCast({|Simplify:DirectCast((Sub(y) 
                                                    Dim x = y
                                                    Call New X().Foo(x)
                                               End Sub), Action(Of Object))|}, Action(Of String))("HI")
    End Sub

End Module

Public Class X
    Public Sub Foo(x As Object)
        Console.WriteLine(1)
    End Sub

    Public Sub Foo(x As String)
        Console.WriteLine(2)
    End Sub
End Class
]]>
        </Document>
    </Project>
</Workspace>

            Dim expected =
<code><![CDATA[
Imports System

Module Program
    Public Sub Main()
        Call DirectCast(DirectCast((Sub(y) 
                                                    Dim x = y
                                                    Call New X().Foo(x)
                                               End Sub), Action(Of Object)), Action(Of String))("HI")
    End Sub

End Module

Public Class X
    Public Sub Foo(x As Object)
        Console.WriteLine(1)
    End Sub

    Public Sub Foo(x As String)
        Console.WriteLine(2)
    End Sub
End Class
]]>
</code>

            Await TestAsync(input, expected)

        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Simplification)>
        <WorkItem(529988)>
        Public Async Function TestVisualBasic_DoNotRemove_NecessaryCastInDelegateCreationExpression6() As Task
            ' Note: Removing the cast changes the parameter type of lambda parameter "z"
            ' and changes the method symbol Foo invoked in the lambda body.

            Dim input =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document><![CDATA[
Imports System

Module Program
    Public Sub Main()
        Call DirectCast({|Simplify:DirectCast((Sub(y, z)
                                        Call New X().Foo(z)
                                    End Sub), Action(Of Object, Object))|}, Action(Of String, String))("HI", "HELLO")
    End Sub

End Module

Public Class X
    Public Sub Foo(x As Object)
        Console.WriteLine(1)
    End Sub

    Public Sub Foo(x As String)
        Console.WriteLine(2)
    End Sub
End Class
]]>
        </Document>
    </Project>
</Workspace>

            Dim expected =
<code><![CDATA[
Imports System

Module Program
    Public Sub Main()
        Call DirectCast(DirectCast((Sub(y, z)
                                        Call New X().Foo(z)
                                    End Sub), Action(Of Object, Object)), Action(Of String, String))("HI", "HELLO")
    End Sub

End Module

Public Class X
    Public Sub Foo(x As Object)
        Console.WriteLine(1)
    End Sub

    Public Sub Foo(x As String)
        Console.WriteLine(2)
    End Sub
End Class
]]>
</code>

            Await TestAsync(input, expected)

        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Simplification)>
        <WorkItem(529988)>
        Public Async Function TestVisualBasic_Remove_UnnecessaryCastInDelegateCreationExpression7() As Task
            ' Note: Removing the cast changes the parameter type of lambda parameter "z"
            ' but not that of parameter "y" and hence the semantics of the lambda body aren't changed.

            Dim input =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document><![CDATA[
Imports System

Module Program
    Public Sub Main()
        Call DirectCast({|Simplify:DirectCast((Sub(y, z)
                                        Dim x as Object = y
                                        Call New X().Foo(z)
                                    End Sub), Action(Of Object, Object))|}, Action(Of String, Object))("HI", "HELLO")
    End Sub

End Module

Public Class X
    Public Sub Foo(x As Object)
        Console.WriteLine(1)
    End Sub

    Public Sub Foo(x As String)
        Console.WriteLine(2)
    End Sub
End Class
]]>
        </Document>
    </Project>
</Workspace>

            Dim expected =
<code><![CDATA[
Imports System

Module Program
    Public Sub Main()
        Call DirectCast((Sub(y, z)
                                        Dim x as Object = y
                                        Call New X().Foo(z)
                                    End Sub), Action(Of String, Object))("HI", "HELLO")
    End Sub

End Module

Public Class X
    Public Sub Foo(x As Object)
        Console.WriteLine(1)
    End Sub

    Public Sub Foo(x As String)
        Console.WriteLine(2)
    End Sub
End Class
]]>
</code>

            Await TestAsync(input, expected)

        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Simplification)>
        <WorkItem(529988)>
        Public Async Function TestVisualBasic_Remove_UnnecessaryCastInDelegateCreationExpression8() As Task
            ' Note: Removing the cast changes the parameter type of lambda parameter "y"
            ' but doesn't change the built in operator invoked for "y + z".

            Dim input =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document><![CDATA[
Imports System

Module Program
    Public Sub Main()
        Call DirectCast({|Simplify:DirectCast((Sub(y, z)
                                        Console.WriteLine(y + z)
                                    End Sub), Action(Of Object, Object))|}, Action(Of Object, String))("HI", "HELLO")
    End Sub

End Module
]]>
        </Document>
    </Project>
</Workspace>

            Dim expected =
<code><![CDATA[
Imports System

Module Program
    Public Sub Main()
        Call DirectCast((Sub(y, z)
                                        Console.WriteLine(y + z)
                                    End Sub), Action(Of Object, String))("HI", "HELLO")
    End Sub

End Module
]]>
</code>

            Await TestAsync(input, expected)

        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Simplification)>
        <WorkItem(529988)>
        Public Async Function TestVisualBasic_DoNotRemove_NecessaryCastInDelegateCreationExpression9() As Task
            ' Note: Removing the cast changes the parameter type of lambda parameter "y"
            ' and changes the semantics of nested lambda body.

            Dim input =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document><![CDATA[
Imports System

Module Program
    Public Sub Main()
        Call DirectCast({|Simplify:DirectCast((Function(y, z)
                                        Dim a = (Sub(w)
                                                     Call New X().Foo(y)
                                                 End Sub)
                                        Return a
                                    End Function), Action(Of Object, Object))|}, Action(Of String, Object))("HI", "HELLO")
    End Sub

End Module

Public Class X
    Public Sub Foo(x As Object)
        Console.WriteLine(1)
    End Sub

    Public Sub Foo(x As String)
        Console.WriteLine(2)
    End Sub
End Class
]]>
        </Document>
    </Project>
</Workspace>

            Dim expected =
<code><![CDATA[
Imports System

Module Program
    Public Sub Main()
        Call DirectCast(DirectCast((Function(y, z)
                                        Dim a = (Sub(w)
                                                     Call New X().Foo(y)
                                                 End Sub)
                                        Return a
                                    End Function), Action(Of Object, Object)), Action(Of String, Object))("HI", "HELLO")
    End Sub

End Module

Public Class X
    Public Sub Foo(x As Object)
        Console.WriteLine(1)
    End Sub

    Public Sub Foo(x As String)
        Console.WriteLine(2)
    End Sub
End Class
]]>
</code>

            Await TestAsync(input, expected)

        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Simplification)>
        <WorkItem(529982)>
        Public Async Function TestVisualBasic_Remove_UnnecessaryExplicitCastForLambdaExpression_DirectCast() As Task
            Dim input =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document><![CDATA[
Option Strict On
Imports System
 
Module Module1
    Sub Main()
        Dim l As Func(Of Exception) = {|Simplify:DirectCast(Function()
                                                     Return New ArgumentException
                                                 End Function, Func(Of ArgumentException))|}
    End Sub
End Module
]]>
        </Document>
    </Project>
</Workspace>

            Dim expected =
<code><![CDATA[
Option Strict On
Imports System
 
Module Module1
    Sub Main()
        Dim l As Func(Of Exception) = Function()
                                                     Return New ArgumentException
                                                 End Function
    End Sub
End Module
]]>
</code>

            Await TestAsync(input, expected)

        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Simplification)>
        <WorkItem(529982)>
        Public Async Function TestVisualBasic_Remove_UnnecessaryExplicitCastForLambdaExpression_TryCast() As Task
            Dim input =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document><![CDATA[
Option Strict On
Imports System
 
Module Module1
    Sub Main()
        Dim l As Func(Of Exception) = {|Simplify:TryCast(Function()
                                                     Return New ArgumentException
                                                 End Function, Func(Of ArgumentException))|}
    End Sub
End Module
]]>
        </Document>
    </Project>
</Workspace>

            Dim expected =
<code><![CDATA[
Option Strict On
Imports System
 
Module Module1
    Sub Main()
        Dim l As Func(Of Exception) = Function()
                                                     Return New ArgumentException
                                                 End Function
    End Sub
End Module
]]>
</code>

            Await TestAsync(input, expected)

        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Simplification)>
        <WorkItem(680657)>
        Public Async Function TestVisualBasic_Remove_UnnecessaryCastWithinAsNewExpression() As Task
            Dim input =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document><![CDATA[
Imports System

Public Class X
    Dim field As New X({|Simplify:DirectCast(0, Integer)|})
    Property prop As New X({|Simplify:DirectCast(0, Integer)|})

    Public Sub New(i As Integer)
        Dim local As New X({|Simplify:DirectCast(0, Integer)|})
    End Sub
End Class
]]>
        </Document>
    </Project>
</Workspace>

            Dim expected =
<code><![CDATA[
Imports System

Public Class X
    Dim field As New X(0)
    Property prop As New X(0)

    Public Sub New(i As Integer)
        Dim local As New X(0)
    End Sub
End Class
]]>
</code>

            Await TestAsync(input, expected)

        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Simplification)>
        <WorkItem(835671)>
        Public Async Function TestVisualBasic_DoNotRemove_NecessaryCastInUnaryExpression() As Task
            Dim input =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document><![CDATA[
Class C
	Private Sub Method(d As Double)
		Method({|Simplify:CInt(d)|})		' not flagged because the cast changes the semantics
		Method(-{|Simplify:CInt(d)|})	' should not be flagged
	End Sub
End Class
]]>
        </Document>
    </Project>
</Workspace>

            Dim expected =
<code><![CDATA[
Class C
	Private Sub Method(d As Double)
		Method(CInt(d))		' not flagged because the cast changes the semantics
		Method(-CInt(d))	' should not be flagged
	End Sub
End Class
]]>
</code>

            Await TestAsync(input, expected)

        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Simplification)>
        <WorkItem(889341)>
        Public Async Function TestVisualBasic_DoNotRemove_CastInErroneousCode() As Task
            Dim input =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document><![CDATA[
Class C
	Private Sub M()
		Dim x As Object = Nothing
		M({|Simplify:DirectCast(x, String)|})
	End Sub
End Class
]]>
        </Document>
    </Project>
</Workspace>

            Dim expected =
<code><![CDATA[
Class C
	Private Sub M()
		Dim x As Object = Nothing
		M(DirectCast(x, String))
	End Sub
End Class
]]>
</code>

            Await TestAsync(input, expected)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Simplification)>
        <WorkItem(529787)>
        Public Async Function TestVisualBasic_DoNotRemove_RequiredCastInCollectionInitializer() As Task
            Dim input =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document><![CDATA[
Imports System
Imports System.Collections.Generic

Class X
	Inherits List(Of Integer)
	Private Overloads Sub Add(x As Object)
		Console.WriteLine(1)
	End Sub
	Private Overloads Sub Add(x As String)
		Console.WriteLine(2)
	End Sub

	Private Shared Sub Main()
		Dim z = New X() From { {|Simplify:DirectCast("", Object)|} }
	End Sub
End Class
]]>
        </Document>
    </Project>
</Workspace>

            Dim expected =
<code><![CDATA[
Imports System
Imports System.Collections.Generic

Class X
	Inherits List(Of Integer)
	Private Overloads Sub Add(x As Object)
		Console.WriteLine(1)
	End Sub
	Private Overloads Sub Add(x As String)
		Console.WriteLine(2)
	End Sub

	Private Shared Sub Main()
		Dim z = New X() From { DirectCast("", Object) }
	End Sub
End Class
]]>
</code>

            Await TestAsync(input, expected)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Simplification)>
        <WorkItem(995855)>
        Public Async Function TestVisualBasic_DontRemove_NecessaryCastInTernaryExpression1() As Task
            Dim input =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document><![CDATA[
Class C
    Private Shared Sub Main(args As String())
        Dim s As Byte = 0
        Dim i As Integer = 0
        s += If(i = 0, CByte(0), {|Simplify:CByte(0)|})
    End Sub
End Class
]]>
        </Document>
    </Project>
</Workspace>

            Dim expected =
<code><![CDATA[
Class C
    Private Shared Sub Main(args As String())
        Dim s As Byte = 0
        Dim i As Integer = 0
        s += If(i = 0, CByte(0), CByte(0))
    End Sub
End Class
]]>
</code>

            Await TestAsync(input, expected)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Simplification)>
        <WorkItem(995855)>
        Public Async Function TestVisualBasic_DontRemove_NecessaryCastInTernaryExpression2() As Task
            Dim input =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document><![CDATA[
Class C
    Private Shared Sub Main(args As String())
        Dim s As Byte = 0
        Dim i As Integer = 0
        s += If(i = 0, {|Simplify:CByte(0)|}, CByte(0))
    End Sub
End Class
]]>
        </Document>
    </Project>
</Workspace>

            Dim expected =
<code><![CDATA[
Class C
    Private Shared Sub Main(args As String())
        Dim s As Byte = 0
        Dim i As Integer = 0
        s += If(i = 0, CByte(0), CByte(0))
    End Sub
End Class
]]>
</code>

            Await TestAsync(input, expected)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Simplification)>
        <WorkItem(995855)>
        Public Async Function TestVisualBasic_Remove_UnnecessaryCastInTernaryExpression1() As Task
            Dim input =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document><![CDATA[
Class C
    Private Shared Sub Main(args As String())
        Dim s As Byte = 0
        Dim i As Integer = 0
        s += If(i = 0, 0, {|Simplify:CByte(0)|})
    End Sub
End Class
]]>
        </Document>
    </Project>
</Workspace>

            Dim expected =
<code><![CDATA[
Class C
    Private Shared Sub Main(args As String())
        Dim s As Byte = 0
        Dim i As Integer = 0
        s += If(i = 0, 0, 0)
    End Sub
End Class
]]>
</code>

            Await TestAsync(input, expected)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Simplification)>
        <WorkItem(995855)>
        Public Async Function TestVisualBasic_Remove_UnnecessaryCastInTernaryExpression2() As Task
            Dim input =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document><![CDATA[
Class C
    Private Shared Sub Main(args As String())
        Dim s As Byte = 0
        Dim i As Integer = 0
        s += If(i = 0, {|Simplify:CByte(0)|}, 0)
    End Sub
End Class
]]>
        </Document>
    </Project>
</Workspace>

            Dim expected =
<code><![CDATA[
Class C
    Private Shared Sub Main(args As String())
        Dim s As Byte = 0
        Dim i As Integer = 0
        s += If(i = 0, 0, 0)
    End Sub
End Class
]]>
</code>

            Await TestAsync(input, expected)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Simplification)>
        <WorkItem(1031406)>
        Public Async Function TestVisualBasic_DoNotRemove_NecessaryTryCast() As Task
            Dim input =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document><![CDATA[
Class Program
    Sub Main(args As String())
        Dim p As Object = 0
        Console.Write({|Simplify:TryCast(p, String)|} IsNot Nothing)
    End Sub
End Class
]]>
        </Document>
    </Project>
</Workspace>

            Dim expected =
<code><![CDATA[
Class Program
    Sub Main(args As String())
        Dim p As Object = 0
        Console.Write(TryCast(p, String) IsNot Nothing)
    End Sub
End Class
]]>
</code>

            Await TestAsync(input, expected)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Simplification)>
        <WorkItem(253, "https://github.com/dotnet/roslyn/issues/253")>
        Public Async Function TestVisualBasic_DoNotRemove_NecessaryCastInConditionAccess() As Task
            Dim input =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document><![CDATA[
Option Strict On

Public Class Class1
    Friend Shared Sub Test(arg As Object)
        Dim identity = If({|Simplify:TryCast(arg, B)|}?.A, TryCast(arg, A))
    End Sub
    Class A
    End Class
    Class B
        Public ReadOnly Property A As A
    End Class
End Class
]]>
        </Document>
    </Project>
</Workspace>

            Dim expected =
<code><![CDATA[
Option Strict On

Public Class Class1
    Friend Shared Sub Test(arg As Object)
        Dim identity = If(TryCast(arg, B)?.A, TryCast(arg, A))
    End Sub
    Class A
    End Class
    Class B
        Public ReadOnly Property A As A
    End Class
End Class
]]>
</code>

            Await TestAsync(input, expected)
        End Function
        <Fact, Trait(Traits.Feature, Traits.Features.Simplification)>
        <WorkItem(4037, "https://github.com/dotnet/roslyn/issues/4037")>
        Public Async Function TestVisualBasic_DoNotRemove_NecessaryCastOfEnumFromInInterpolation() As Task
            Dim input =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document><![CDATA[
Module Module1

    Enum State As UShort
        None = 0
        State1 = 1 << 0
    End Enum

    Sub Main()
        Dim alarmState = State.State1

        Dim str = $"State: {alarmState} [{{|Simplify:CUShort(alarmState)|}:x4}]"

        Console.WriteLine(str)
    End Sub

End Module
]]>
        </Document>
    </Project>
</Workspace>

            Dim expected =
<code><![CDATA[
Module Module1

    Enum State As UShort
        None = 0
        State1 = 1 << 0
    End Enum

    Sub Main()
        Dim alarmState = State.State1

        Dim str = $"State: {alarmState} [{CUShort(alarmState):x4}]"

        Console.WriteLine(str)
    End Sub

End Module
]]>
</code>

            Await TestAsync(input, expected)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Simplification)>
        <WorkItem(4037, "https://github.com/dotnet/roslyn/issues/4037")>
        Public Async Function TestVisualBasic_DoNotRemove_NecessaryCastOfEnumAndToString() As Task
            Dim input =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document><![CDATA[
Module Module1

    Enum State As UShort
        None = 0
        State1 = 1 << 0
    End Enum

    Sub Main()
        Dim alarmState = State.State1

        Dim str = {|Simplify:CUShort(alarmState)|}.ToString("X4")

        Console.WriteLine(str)
    End Sub

End Module
]]>
        </Document>
    </Project>
</Workspace>

            Dim expected =
<code><![CDATA[
Module Module1

    Enum State As UShort
        None = 0
        State1 = 1 << 0
    End Enum

    Sub Main()
        Dim alarmState = State.State1

        Dim str = CUShort(alarmState).ToString("X4")

        Console.WriteLine(str)
    End Sub

End Module
]]>
</code>

            Await TestAsync(input, expected)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Simplification)>
        <WorkItem(4037, "https://github.com/dotnet/roslyn/issues/4037")>
        Public Async Function TestVisualBasic_Remove_UnnecessaryCastOfEnum() As Task
            Dim input =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document><![CDATA[
Option Strict On

Module Module1

    Enum State As UShort
        None = 0
        State1 = 1 << 0
    End Enum

    Sub Main()
        Dim alarmState = State.State1

        Dim val As UShort = {|Simplify:CUShort(alarmState)|}

        Console.WriteLine(val)
    End Sub

End Module
]]>
        </Document>
    </Project>
</Workspace>

            Dim expected =
<code><![CDATA[
Option Strict On

Module Module1

    Enum State As UShort
        None = 0
        State1 = 1 << 0
    End Enum

    Sub Main()
        Dim alarmState = State.State1

        Dim val As UShort = alarmState

        Console.WriteLine(val)
    End Sub

End Module
]]>
</code>

            Await TestAsync(input, expected)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Simplification)>
        <WorkItem(4037, "https://github.com/dotnet/roslyn/issues/4037")>
        Public Async Function TestVisualBasic_Remove_UnnecessaryCastOfUShort() As Task
            Dim input =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document><![CDATA[
Class C
    Sub M()
        Dim x As UShort = 400
        Dim s = $"Hello {{|Simplify:CObj(x)|}:x4}"
        Console.WriteLine(s)
    End Sub
End Class
]]>
        </Document>
    </Project>
</Workspace>

            Dim expected =
<code><![CDATA[
Class C
    Sub M()
        Dim x As UShort = 400
        Dim s = $"Hello {x:x4}"
        Console.WriteLine(s)
    End Sub
End Class
]]>
</code>

            Await TestAsync(input, expected)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Simplification)>
        <WorkItem(4037, "https://github.com/dotnet/roslyn/issues/4037")>
        Public Async Function TestVisualBasic_Remove_UnnecessaryCastOfDateTime1() As Task
            Dim input =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document><![CDATA[
Class C
    Sub M()
        Dim d As DateTime = #2015-09-08#
        Dim s = $"Hello {{|Simplify:CObj(d)|}:yyyy-MM-dd}"
        Console.WriteLine(s)
    End Sub
End Class
]]>
        </Document>
    </Project>
</Workspace>

            Dim expected =
<code><![CDATA[
Class C
    Sub M()
        Dim d As DateTime = #2015-09-08#
        Dim s = $"Hello {d:yyyy-MM-dd}"
        Console.WriteLine(s)
    End Sub
End Class
]]>
</code>

            Await TestAsync(input, expected)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Simplification)>
        <WorkItem(4037, "https://github.com/dotnet/roslyn/issues/4037")>
        Public Async Function TestVisualBasic_Remove_UnnecessaryCastOfDateTime2() As Task
            Dim input =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document><![CDATA[
Class C
    Sub M()
        Dim d As DateTime = #2015-09-08#
        Dim s = $"Hello {({|Simplify:CObj(d)|}):yyyy-MM-dd}"
        Console.WriteLine(s)
    End Sub
End Class
]]>
        </Document>
    </Project>
</Workspace>

            Dim expected =
<code><![CDATA[
Class C
    Sub M()
        Dim d As DateTime = #2015-09-08#
        Dim s = $"Hello {(d):yyyy-MM-dd}"
        Console.WriteLine(s)
    End Sub
End Class
]]>
</code>

            Await TestAsync(input, expected)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Simplification)>
        <WorkItem(5314, "https://github.com/dotnet/roslyn/issues/5314")>
        Public Async Function TestVisualBasic_DontRemove_NecessaryCastToObjectInConditionalExpression() As Task
            Dim input =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document><![CDATA[
Class C
    Function Convert(cond As Boolean, value As ULong) As Object
        Return If(cond, {|Simplify:CObj(CUInt(value))|}, value)
    End Function
End Class
]]>
        </Document>
    </Project>
</Workspace>

            Dim expected =
<code><![CDATA[
Class C
    Function Convert(cond As Boolean, value As ULong) As Object
        Return If(cond, CObj(CUInt(value)), value)
    End Function
End Class
]]>
</code>

            Await TestAsync(input, expected)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Simplification)>
        <WorkItem(5685, "https://github.com/dotnet/roslyn/issues/5685")>
        Public Async Function TestVisualBasic_DontRemove_NecessaryCastToNullable1() As Task
            Dim input =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document><![CDATA[
Class C
    Sub M()
        Dim d As Date? = If(True, {|Simplify:DirectCast(Nothing, Date?)|}, CDate(""))
    End Sub
End Class
]]>
        </Document>
    </Project>
</Workspace>

            Dim expected =
<code><![CDATA[
Class C
    Sub M()
        Dim d As Date? = If(True, DirectCast(Nothing, Date?), CDate(""))
    End Sub
End Class
]]>
</code>

            Await TestAsync(input, expected)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Simplification)>
        <WorkItem(5685, "https://github.com/dotnet/roslyn/issues/5685")>
        Public Async Function TestVisualBasic_Remove_UnnecessaryCastToNullable1() As Task
            Dim input =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document><![CDATA[
Class C
    Sub M()
        Dim d As Date? = If(True, {|Simplify:DirectCast(Nothing, Date?)|}, CType(#10/6/2015#, Date?))
    End Sub
End Class
]]>
        </Document>
    </Project>
</Workspace>

            Dim expected =
<code><![CDATA[
Class C
    Sub M()
        Dim d As Date? = If(True, Nothing, CType(#10/6/2015#, Date?))
    End Sub
End Class
]]>
</code>

            Await TestAsync(input, expected)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Simplification)>
        <WorkItem(5685, "https://github.com/dotnet/roslyn/issues/5685")>
        Public Async Function TestVisualBasic_Remove_UnnecessaryCastToNullable2() As Task
            Dim input =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document><![CDATA[
Class C
    Sub M()
        Dim d As Date? = If(True, DirectCast(Nothing, Date?), {|Simplify:CType(#10/6/2015#, Date?)|})
    End Sub
End Class
]]>
        </Document>
    </Project>
</Workspace>

            Dim expected =
<code><![CDATA[
Class C
    Sub M()
        Dim d As Date? = If(True, DirectCast(Nothing, Date?), #10/6/2015#)
    End Sub
End Class
]]>
</code>

            Await TestAsync(input, expected)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Simplification)>
        <WorkItem(5685, "https://github.com/dotnet/roslyn/issues/5685")>
        Public Async Function TestVisualBasic_Remove_UnnecessaryCastToNullable3() As Task
            Dim input =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document><![CDATA[
Class C
    Sub M()
        Dim d As Date? = {|Simplify:DirectCast(Nothing, Date?)|}
    End Sub
End Class
]]>
        </Document>
    </Project>
</Workspace>

            Dim expected =
<code><![CDATA[
Class C
    Sub M()
        Dim d As Date? = Nothing
    End Sub
End Class
]]>
</code>

            Await TestAsync(input, expected)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Simplification)>
        Public Async Function TestVisualBasic_Remove_IntegerToByte_OptionStrictOff() As Task
            Dim input =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document>
Option Strict Off
Class C
    Sub M()
        Dim b As Byte
        b += {|Simplify:CByte(1)|}
    End Sub
End Class
        </Document>
    </Project>
</Workspace>

            Dim expected =
<code>
Option Strict Off
Class C
    Sub M()
        Dim b As Byte
        b += 1
    End Sub
End Class
</code>

            Await TestAsync(input, expected)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Simplification)>
        Public Async Function TestVisualBasic_DontRemove_IntegerToByte_OptionStrictOn1() As Task
            Dim input =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document>
Option Strict On
Class C
    Sub M()
        Dim b As Byte
        b += {|Simplify:CByte(1)|}
    End Sub
End Class
        </Document>
    </Project>
</Workspace>

            Dim expected =
<code>
Option Strict On
Class C
    Sub M()
        Dim b As Byte
        b += CByte(1)
    End Sub
End Class
</code>

            Await TestAsync(input, expected)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Simplification)>
        Public Async Function TestVisualBasic_DontRemove_IntegerToByte_OptionStrictOn2() As Task
            Dim input =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document>
Option Strict On
Class C
    Sub M()
        Dim b As Byte
        Const x = 1
        b += {|Simplify:CByte(x)|}
    End Sub
End Class
        </Document>
    </Project>
</Workspace>

            Dim expected =
<code>
Option Strict On
Class C
    Sub M()
        Dim b As Byte
        Const x = 1
        b += CByte(x)
    End Sub
End Class
</code>

            Await TestAsync(input, expected)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Simplification)>
        Public Async Function TestVisualBasic_DontRemove_IntegerToByte_OptionStrictOn3() As Task
            Dim input =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document>
Option Strict On
Class C
    Sub M()
        Dim b As Byte
        Dim x As Integer = 1
        b += {|Simplify:CByte(x)|}
    End Sub
End Class
        </Document>
    </Project>
</Workspace>

            Dim expected =
<code>
Option Strict On
Class C
    Sub M()
        Dim b As Byte
        Dim x As Integer = 1
        b += CByte(x)
    End Sub
End Class
</code>

            Await TestAsync(input, expected)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Simplification)>
        Public Async Function TestVisualBasic_DontRemove_IntegerToByte_OptionStrictOn4() As Task
            Dim input =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document>
Option Strict On
Class C
    Sub M()
        Dim b As Byte
        b = b + {|Simplify:CByte(1)|}
    End Sub
End Class
        </Document>
    </Project>
</Workspace>

            Dim expected =
<code>
Option Strict On
Class C
    Sub M()
        Dim b As Byte
        b = b + CByte(1)
    End Sub
End Class
</code>

            Await TestAsync(input, expected)
        End Function
#End Region

    End Class
End Namespace