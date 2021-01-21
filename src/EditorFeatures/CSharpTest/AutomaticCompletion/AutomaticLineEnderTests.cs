// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using Microsoft.CodeAnalysis.Editor.CSharp.AutomaticCompletion;
using Microsoft.CodeAnalysis.Editor.UnitTests.AutomaticCompletion;
using Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.VisualStudio.Commanding;
using Microsoft.VisualStudio.Text.Editor.Commanding.Commands;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.AutomaticCompletion
{
    [Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
    public class AutomaticLineEnderTests : AbstractAutomaticLineEnderTests
    {
        [WpfFact]
        public void Creation()
        {
            Test(@"
$$", "$$");
        }

        [WpfFact]
        public void Usings()
        {
            Test(@"using System;
$$", @"using System$$");
        }

        [WpfFact]
        public void Namespace()
        {
            Test(@"namespace {}
$$", @"namespace {$$}");
        }

        [WpfFact]
        public void Class()
        {
            Test(@"class {}
$$", "class {$$}");
        }

        [WpfFact]
        public void Method()
        {
            Test(@"class C
{
    void Method() {$$}
}", @"class C
{
    void Method() {$$}
}", assertNextHandlerInvoked: true);
        }

        [WpfFact]
        public void Field()
        {
            Test(@"class C
{
    private readonly int i = 3;
    $$
}", @"class C
{
    private readonly int i = 3$$
}");
        }

        [WpfFact]
        public void EventField()
        {
            Test(@"class C
{
    event System.EventHandler e = null;
    $$
}", @"class C
{
    event System.EventHandler e = null$$
}");
        }

        [WpfFact]
        public void Field2()
        {
            Test(@"class C
{
    private readonly int i;
    $$
}", @"class C
{
    private readonly int i$$
}");
        }

        [WpfFact]
        public void EventField2()
        {
            Test(@"class C
{
    event System.EventHandler e;
    $$
}", @"class C
{
    event System.EventHandler e$$
}");
        }

        [WpfFact]
        public void Field3()
        {
            Test(@"class C
{
    private readonly int
        $$
}", @"class C
{
    private readonly int$$
}");
        }

        [WpfFact]
        public void EventField3()
        {
            Test(@"class C
{
    event System.EventHandler
        $$
}", @"class C
{
    event System.EventHandler$$
}");
        }

        [WpfFact]
        public void EmbededStatement()
        {
            Test(@"class C
{
    void Method()
    {
        if (true)
        {
            $$
        }
    }
}", @"class C
{
    void Method()
    {
        if (true) $$
    }
}");
        }

        [WpfFact]
        public void EmbededStatement1()
        {
            Test(@"class C
{
    void Method()
    {
        if (true) 
            Console.WriteLine()
                $$
    }
}", @"class C
{
    void Method()
    {
        if (true) 
            Console.WriteLine()$$
    }
}");
        }

        [WpfFact]
        public void EmbededStatement2()
        {
            Test(@"class C
{
    void Method()
    {
        if (true)
            Console.WriteLine();
        $$
    }
}", @"class C
{
    void Method()
    {
        if (true) 
            Console.WriteLine($$)
    }
}");
        }

        [WpfFact]
        public void Statement()
        {
            Test(@"class C
{
    void Method()
    {
        int i;
        $$
    }
}", @"class C
{
    void Method()
    {
        int i$$
    }
}");
        }

        [WpfFact]
        public void Statement1()
        {
            Test(@"class C
{
    void Method()
    {
        int
            $$
    }
}", @"class C
{
    void Method()
    {
        int$$
    }
}");
        }

        [WorkItem(3944, "https://github.com/dotnet/roslyn/issues/3944")]
        [WpfFact]
        public void ExpressionBodiedMethod()
        {
            Test(@"class T
{
    int M() => 1 + 2;
    $$
}", @"class T
{
    int M() => 1 + 2$$
}");
        }

        [WorkItem(3944, "https://github.com/dotnet/roslyn/issues/3944")]
        [WpfFact]
        public void ExpressionBodiedOperator()
        {
            Test(@"class Complex
{
    int real; int imaginary;
    public static Complex operator +(Complex a, Complex b) => a.Add(b.real + 1);
    $$
    private Complex Add(int b) => null;
}", @"class Complex
{
    int real; int imaginary;
    public static Complex operator +(Complex a, Complex b) => a.Add(b.real + 1)$$
    private Complex Add(int b) => null;
}");
        }

        [WorkItem(3944, "https://github.com/dotnet/roslyn/issues/3944")]
        [WpfFact]
        public void ExpressionBodiedConversionOperator()
        {
            Test(@"using System;
public struct DBBool
{
    public static readonly DBBool dbFalse = new DBBool(-1);
    int value;

    DBBool(int value)
    {
        this.value = value;
    }

    public static implicit operator DBBool(bool x) => x ? new DBBool(1) : dbFalse;
    $$
}", @"using System;
public struct DBBool
{
    public static readonly DBBool dbFalse = new DBBool(-1);
    int value;

    DBBool(int value)
    {
        this.value = value;
    }

    public static implicit operator DBBool(bool x) => x ? new DBBool(1) : dbFalse$$
}");
        }

        [WorkItem(3944, "https://github.com/dotnet/roslyn/issues/3944")]
        [WpfFact]
        public void ExpressionBodiedProperty()
        {
            Test(@"class T
{
    int P1 => 1 + 2;
    $$
}", @"class T
{
    int P1 => 1 + 2$$
}");
        }

        [WorkItem(3944, "https://github.com/dotnet/roslyn/issues/3944")]
        [WpfFact]
        public void ExpressionBodiedIndexer()
        {
            Test(@"using System;
class SampleCollection<T>
{
    private T[] arr = new T[100];
    public T this[int i] => i > 0 ? arr[i + 1] : arr[i + 2];
    $$
}", @"using System;
class SampleCollection<T>
{
    private T[] arr = new T[100];
    public T this[int i] => i > 0 ? arr[i + 1] : arr[i + 2]$$
}");
        }

        [WorkItem(3944, "https://github.com/dotnet/roslyn/issues/3944")]
        [WpfFact]
        public void ExpressionBodiedMethodWithBlockBodiedAnonymousMethodExpression()
        {
            Test(@"using System;
class TestClass
{
    Func<int, int> Y() => delegate (int x)
    {
        return 9;
    };
    $$
}", @"using System;
class TestClass
{
    Func<int, int> Y() => delegate (int x)
    {
        return 9;
    }$$
}");
        }

        [WorkItem(3944, "https://github.com/dotnet/roslyn/issues/3944")]
        [WpfFact]
        public void ExpressionBodiedMethodWithSingleLineBlockBodiedAnonymousMethodExpression()
        {
            Test(@"using System;
class TestClass
{
    Func<int, int> Y() => delegate (int x) { return 9; };
    $$
}", @"using System;
class TestClass
{
    Func<int, int> Y() => delegate (int x) { return 9; }$$
}");
        }

        [WorkItem(3944, "https://github.com/dotnet/roslyn/issues/3944")]
        [WpfFact]
        public void ExpressionBodiedMethodWithBlockBodiedSimpleLambdaExpression()
        {
            Test(@"using System;
class TestClass
{
    Func<int, int> Y() => f =>
    {
        return f * 9;
    };
    $$
}", @"using System;
class TestClass
{
    Func<int, int> Y() => f =>
    {
        return f * 9;
    }$$
}");
        }

        [WorkItem(3944, "https://github.com/dotnet/roslyn/issues/3944")]
        [WpfFact]
        public void ExpressionBodiedMethodWithExpressionBodiedSimpleLambdaExpression()
        {
            Test(@"using System;
class TestClass
{
    Func<int, int> Y() => f => f * 9;
    $$
}", @"using System;
class TestClass
{
    Func<int, int> Y() => f => f * 9$$
}");
        }

        [WorkItem(3944, "https://github.com/dotnet/roslyn/issues/3944")]
        [WpfFact]
        public void ExpressionBodiedMethodWithBlockBodiedAnonymousMethodExpressionInMethodArgs()
        {
            Test(@"using System;
class TestClass
{
    public int Prop => Method1(delegate ()
    {
        return 8;
    });
    $$

    private int Method1(Func<int> p) => null;
}", @"using System;
class TestClass
{
    public int Prop => Method1(delegate()
    {
        return 8;
    })$$

    private int Method1(Func<int> p) => null;
}");
        }

        [WorkItem(3944, "https://github.com/dotnet/roslyn/issues/3944")]
        [WpfFact]
        public void Format_SimpleExpressionBodiedMember()
        {
            Test(@"class T
{
    int M() => 1 + 2;
    $$
}", @"class T
{
         int   M()   =>    1       +     2$$
}");
        }

        [WorkItem(3944, "https://github.com/dotnet/roslyn/issues/3944")]
        [WpfFact]
        public void Format_ExpressionBodiedMemberWithSingleLineBlock()
        {
            Test(@"using System;
class TestClass
{
    Func<int, int> Y() => delegate (int x) { return 9; };
    $$
}", @"using System;
class TestClass
{
                Func<int, int>  Y ()   =>   delegate(int x) { return     9  ; }$$
}");
        }

        [WorkItem(3944, "https://github.com/dotnet/roslyn/issues/3944")]
        [WpfFact]
        public void Format_ExpressionBodiedMemberWithMultiLineBlock()
        {
            Test(@"using System;
class TestClass
{
    Func<int, int> Y() => delegate (int x)
    {
        return 9;
    };
    $$
}", @"using System;
class TestClass
{
    Func<int, int> Y() => delegate(int x)
    {
        return 9;
        }$$
}");
        }

        [WpfFact]
        public void Format_Statement()
        {
            Test(@"class C
{
    void Method()
    {
        int i = 1;
        $$
    }
}", @"class C
{
    void Method()
    {
                    int         i           =           1               $$
    }
}");
        }

        [WpfFact]
        public void Format_Using()
        {
            Test(@"using System.Linq;
$$", @"         using           System          .                   Linq            $$");
        }

        [WpfFact]
        public void Format_Using2()
        {
            Test(@"using
    System.Linq;
$$", @"         using           
             System          .                   Linq            $$");
        }

        [WpfFact]
        public void Format_Field()
        {
            Test(@"class C
{
    int i = 1;
    $$
}", @"class C
{
            int         i           =               1           $$
}");
        }

        [WpfFact]
        public void Statement_Trivia()
        {
            Test(@"class C
{
    void goo()
    {
        goo(); //comment
        $$
    }
}", @"class C
{
    void goo()
    {
        goo()$$ //comment
    }
}");
        }

        [WpfFact]
        public void TrailingText_Negative()
        {
            Test(@"class C
{
    event System.EventHandler e = null  int i = 2;
    $$
}", @"class C
{
    event System.EventHandler e = null$$  int i = 2;  
}");
        }

        [WpfFact]
        public void CompletionSetUp()
        {
            Test(@"class Program
{
    object goo(object o)
    {
        return goo();
        $$
    }
}", @"class Program
{
    object goo(object o)
    {
        return goo($$)
    }
}", completionActive: true);
        }

        [WorkItem(530352, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530352")]
        [WpfFact]
        public void EmbededStatement3()
        {
            Test(@"class Program
{
    void Method()
    {
        foreach (var x in y)
        {
            $$
        }
    }
}", @"class Program
{
    void Method()
    {
        foreach (var x in y$$)
    }
}");
        }

        [WorkItem(530716, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530716")]
        [WpfFact]
        public void DontAssertOnMultilineToken()
        {
            Test(@"interface I
{
    void M(string s = @""""""
$$
}", @"interface I
{
    void M(string s = @""""""$$
}");
        }

        [WorkItem(530718, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530718")]
        [WpfFact]
        public void AutomaticLineFormat()
        {
            Test(@"class C
{
    public string P { set; get; }
    $$
}", @"class C
{
    public string P {set;get;$$}
}");
        }

        [WpfFact]
        public void NotAfterExisitingSemicolon()
        {
            Test(@"class TestClass
{
    private int i;
    $$
}", @"class TestClass
{
    private int i;$$
}");
        }

        [WpfFact]
        public void NotAfterCloseBraceInMethod()
        {
            Test(@"class TestClass
{
    void Test() { }
    $$
}", @"class TestClass
{
    void Test() { }$$
}");
        }

        [WpfFact]
        public void NotAfterCloseBraceInStatement()
        {
            Test(@"class TestClass
{
    void Test()
    {
        if (true) { }
        $$
    }
}", @"class TestClass
{
    void Test()
    {
        if (true) { }$$
    }
}");
        }

        [WpfFact]
        public void NotAfterAutoPropertyAccessor()
        {
            Test(@"class TestClass
{
    public int A { get; set }
    $$
}", @"class TestClass
{
    public int A { get; set$$ }
}");
        }

        [WpfFact]
        public void NotAfterAutoPropertyDeclaration()
        {
            Test(@"class TestClass
{
    public int A { get; set; }
    $$
}", @"class TestClass
{
    public int A { get; set; }$$
}");
        }

        [WorkItem(150480, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/150480")]
        [WpfFact]
        public void DelegatedInEmptyBlock()
        {
            Test(@"class TestClass
{
    void Method()
    {
        try { $$}
    }
}", @"class TestClass
{
    void Method()
    {
        try { $$}
    }
}", assertNextHandlerInvoked: true);
        }

        [WorkItem(150480, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/150480")]
        [WpfFact]
        public void DelegatedInEmptyBlock2()
        {
            Test(@"class TestClass
{
    void Method()
    {
        if (true) { $$}
    }
}", @"class TestClass
{
    void Method()
    {
        if (true) { $$}
    }
}", assertNextHandlerInvoked: true);
        }

        [WorkItem(150480, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/150480")]
        [WpfFact]
        public void NotDelegatedOutsideEmptyBlock()
        {
            Test(@"class TestClass
{
    void Method()
    {
        try { }
        $$
    }
}", @"class TestClass
{
    void Method()
    {
        try { }$$
    }
}");
        }

        [WorkItem(150480, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/150480")]
        [WpfFact]
        public void NotDelegatedAfterOpenBraceAndMissingCloseBrace()
        {
            Test(@"class TestClass
{
    void Method()
    {
        try {
            $$
    }
}", @"class TestClass
{
    void Method()
    {
        try {$$
    }
}");
        }

        [WorkItem(150480, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/150480")]
        [WpfFact]
        public void NotDelegatedInNonEmptyBlock()
        {
            Test(@"class TestClass
{
    void Method()
    {
        try { x }
        $$
    }
}", @"class TestClass
{
    void Method()
    {
        try { x$$ }
    }
}");
        }

        [WorkItem(150480, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/150480")]
        [WpfFact]
        public void NotDelegatedAfterOpenBraceInAnonymousObjectCreationExpression()
        {
            Test(@"class TestClass
{
    void Method()
    {
        var pet = new { };
        $$
    }
}", @"class TestClass
{
    void Method()
    {
        var pet = new { $$}
    }
}");
        }

        [WorkItem(150480, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/150480")]
        [WpfFact]
        public void NotDelegatedAfterOpenBraceObjectCreationExpression()
        {
            Test(@"class TestClass
{
    void Method()
    {
        var pet = new List<int> 
            $$
    }
}", @"class TestClass
{
    void Method()
    {
        var pet = new List<int> { $$}
    }
}");
        }

        [WpfTheory]
        [InlineData("namespace")]
        [InlineData("class")]
        [InlineData("struct")]
        [InlineData("record")]
        [InlineData("enum")]
        [InlineData("interface")]
        public void TestEmptyBaseTypeDeclarationAndNamespace(string typeKeyword)
        {
            Test($@"
public {typeKeyword} Bar
{{
    $$
}}", $@"
public {typeKeyword} $$Bar");
        }

        [WpfTheory]
        [InlineData("namespace")]
        [InlineData("class")]
        [InlineData("struct")]
        [InlineData("record")]
        [InlineData("enum")]
        [InlineData("interface")]
        public void TestMultipleBaseTypeDeclaration(string typeKeyword)
        {
            Test($@"
public {typeKeyword} Bar2
{{
    $$
}}
public {typeKeyword} Bar
{{
}}", $@"
public {typeKeyword} B$$ar2
public {typeKeyword} Bar
{{
}}");
        }

        [WpfTheory]
        [InlineData("namespace")]
        [InlineData("class")]
        public void TestNestedNamespaceAndTypeDeclaration(string typeKeyword)
        {
            Test($@"
public {typeKeyword} Bar1
{{
    public {typeKeyword} Bar2
    {{
        $$
    }}
}}",
$@"
public {typeKeyword} Bar1
{{
    public {typeKeyword} B$$ar2
}}");
        }

        [WpfTheory]
        [InlineData("namespace")]
        [InlineData("class")]
        [InlineData("struct")]
        [InlineData("record")]
        [InlineData("enum")]
        [InlineData("interface")]
        public void TestBaseTypeDeclarationAndNamespaceWithOpenBrace(string typeKeyword)
        {
            Test($@"
public {typeKeyword} Bar {{
    $$", $@"
public {typeKeyword} B$$ar {{");
        }

        [WpfTheory]
        [InlineData("namespace")]
        [InlineData("class")]
        [InlineData("struct")]
        [InlineData("record")]
        [InlineData("enum")]
        [InlineData("interface")]
        public void TestValidTypeDeclarationAndNamespace(string typeKeyword)
        {
            Test($@"public {typeKeyword} Bar {{}}
$$",
                $@"public {typeKeyword} Ba$$r {{}}");
        }

        [WpfFact]
        public void TestMethod()
        {
            Test(@"
public class Bar
{
    void Main()
    {
        $$
    }
}", @"
public class Bar
{
    void Ma$$in()
}");
        }

        [WpfFact]
        public void TestValidMethodInInterface()
        {
            Test(@"
public interface Bar
{
    void Main();
    $$
}", @"
public interface Bar
{
    void Mai$$n();
}");
        }

        [WpfFact]
        public void TestValidLocalFunction()
        {
            Test(@"
public class Bar
{
    void Main()
    {
        void Local()
            $$
        {
        }
    }
}", @"
public class Bar
{
    void Main()
    {
        void Loc$$al()
        {
        }
    }
}");
        }

        [WpfFact]
        public void TestLocalFunction()
        {
            Test(@"
public class Bar
{
    void Main()
    {
        void Local()
        {
            $$
        }
    }
}", @"
public class Bar
{
    void Main()
    {
        void Loca$$l()
    }
}");
        }

        [WpfFact]
        public void TestIndexer1()
        {
            Test(@"
public class Bar
{
    public int this[int i]
    {
        $$
    }
}", @"
public class Bar
{
    public int thi$$s[int i]
}");
        }

        [WpfFact]
        public void TestIndexer2()
        {
            Test(@"
public class Bar
{
    public int this[int i]
    {
        $$
    }
    void Main() {}
}", @"
public class Bar
{
    public int thi$$s[int i]
    void Main() {}
}");
        }

        [WpfFact]
        public void TestValidIndexer()
        {
            Test(@"
public class Bar
{
    public int this[int i]
        $$
    {
    }
}", @"
public class Bar
{
    public int thi$$s[int i]
    {
    }
}");
        }

        [WpfFact]
        public void TestAddBracesForObjectCreationExpression1()
        {
            Test(@"
public class Bar
{
    public void M()
    {
        var f = new Foo()
        {
            $$
        };
    }
}
public class Foo
{
    public int HH { get; set; }
    public int PP { get; set; }
}", @"
public class Bar
{
    public void M()
    {
        var f = new Foo()$$
    }
}
public class Foo
{
    public int HH { get; set; }
    public int PP { get; set; }
}");
        }

        [WpfFact]
        public void TestAddBracesForObjectCreationExpression2()
        {
            Test(@"
public class Bar
{
    public void M()
    {
        var f = new Foo
        {
            $$
        };
    }
}
public class Foo
{
    public int HH { get; set; }
    public int PP { get; set; }
}", @"
public class Bar
{
    public void M()
    {
        var f = new Foo$$
    }
}
public class Foo
{
    public int HH { get; set; }
    public int PP { get; set; }
}");
        }

        [WpfFact]
        public void TestRemoveBraceForObjectCreationExpression()
        {
            Test(@"
public class Bar
{
    public void M()
    {
        var f = new Foo();
        $$
    }
}
public class Foo
{
    public int HH { get; set; }
    public int PP { get; set; }
}", @"
public class Bar
{
    public void M()
    {
        var f = new Foo() { $$ };
    }
}
public class Foo
{
    public int HH { get; set; }
    public int PP { get; set; }
}");
        }

        [WpfFact]
        public void TestIfStatement()
        {
            Test(@"
public class Bar
{
    public void Main(bool x)
    {
        if (x)
        {
            $$
        }
        var x = 1;
    }
}", @"
public class Bar
{
    public void Main(bool x)
    {
        if$$ (x)
        var x = 1;
    }
}");
        }

        [WpfFact]
        public void TestDoStatement()
        {
            Test(@"
public class Bar
{
    public void Main()
    {
        do
        {
            $$
        }
    }
}", @"
public class Bar
{
    public void Main()
    {
        do$$
    }
}");
        }

        [WpfFact]
        public void TestSingleElseStatement()
        {
            Test(@"
public class Bar
{
    public void Fo()
    {
        if (true)
        {
        }
        else
        {
            $$
        }
    }
}", @"
public class Bar
{
    public void Fo()
    {
        if (true)
        {
        }
        else$$
    }
}");
        }

        [WpfFact]
        public void TestElseIfStatement()
        {
            Test(@"
public class Bar
{
    public void Fo()
    {
        if (true)
        {
        }
        else if (false)
        {
            $$
        }
    }
}", @"
public class Bar
{
    public void Fo()
    {
        if (true)
        {
        }
        e$$lse if (false)
    }
}");
        }

        [WpfFact]
        public void TestForStatement()
        {
            Test(@"
public class Bar
{
    public void Fo()
    {
        for (int i; i < 10; i++)
        {
            $$
        }
    }
}", @"
public class Bar
{
    public void Fo()
    {
        for (int i; i < 10;$$ i++)
    }
}");
        }

        [WpfFact]
        public void TestForEachStatement()
        {
            Test(@"
public class Bar
{
    public void Fo()
    {
        foreach (var x in """")
        {
            $$
        }
    }
}", @"
public class Bar
{
    public void Fo()
    {
        foreach (var x $$in """")
    }
}");
        }

        [WpfFact]
        public void TestLockStatement()
        {
            Test(@"
public class Bar
{
    object o = new object();
    public void Fo()
    {
        lock (o)
        {
            $$
        }
    }
}", @"
public class Bar
{
    object o = new object();
    public void Fo()
    {
        lock$$(o)
    }
}");
        }

        [WpfFact]
        public void TestUsingStatement()
        {
            Test(@"
using System;
public class Bar
{
    public void Fo()
    {
        using (var d = new D())
        {
            $$
        }
    }
}
public class D : IDisposable
{
    public void Dispose()
    {}
}", @"
using System;
public class Bar
{
    public void Fo()
    {
        usi$$ng (var d = new D())
    }
}
public class D : IDisposable
{
    public void Dispose()
    {}
}");
        }

        [WpfFact]
        public void TestWhileStatement()
        {
            Test(@"
public class Bar
{
    public void Fo()
    {
        while (true)
        {
            $$
        }
    }
}", @"
public class Bar
{
    public void Fo()
    {
        while (tr$$ue)
    }
}");
        }

        [WpfFact]
        public void TestSwitchStatement()
        {
            Test(@"
public class bar
{
    public void TT()
    {
        int i = 10;
        switch (i)
        {
            $$
        }
    }
}", @"
public class bar
{
    public void TT()
    {
        int i = 10;
        switc$$h (i)
    }
}");
        }

        [WpfFact]
        public void TestTryStatement()
        {
            Test(@"
public class bar
{
    public void TT()
    {
        try
        {
            $$
        }
    }
}", @"
public class bar
{
    public void TT()
    {
        tr$$y
    }
}");
        }

        [WpfFact]
        public void TestCatchClause1()
        {
            Test(@"
public class Bar
{
    public void TT()
    {
        try
        {
        }
        catch (System.Exception)
        {
            $$
        }
    }
}", @"
public class Bar
{
    public void TT()
    {
        try
        {
        }
        cat$$ch (System.Exception)
    }
}");
        }

        [WpfFact]
        public void TestCatchClause2()
        {
            Test(@"
public class bar
{
    public void TT()
    {
        try
        {
        }
        catch
        {
            $$
        }
    }
}", @"
public class bar
{
    public void TT()
    {
        try
        {
        }
        cat$$ch
    }
}");
        }

        protected override string Language => LanguageNames.CSharp;

        protected override Action CreateNextHandler(TestWorkspace workspace)
            => () => { };

        internal override IChainedCommandHandler<AutomaticLineEnderCommandArgs> GetCommandHandler(TestWorkspace workspace)
        {
            return Assert.IsType<AutomaticLineEnderCommandHandler>(
                workspace.GetService<ICommandHandler>(
                    ContentTypeNames.CSharpContentType,
                    PredefinedCommandHandlerNames.AutomaticLineEnder));
        }
    }
}
