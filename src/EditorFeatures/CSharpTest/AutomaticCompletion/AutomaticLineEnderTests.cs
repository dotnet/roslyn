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
    pri$$vate re$$adonly i$$nt i = 3$$
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
    e$$vent System.Even$$tHandler e$$ = null$$
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
    event System.EventHandler e
    {
        $$
    }
}", @"class C
{
    eve$$nt System.E$$ventHandler e$$
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

        [WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/57323")]
        public void EmbededStatementFollowedByStatement()
        {
            Test(@"class C
{
    void Method()
    {
        if (true)
        {
        }
        if (true)
        {
            $$
        }
        if (true)
        {
        }
    }
}", @"class C
{
    void Method()
    {
        if (true)
        {
        }
        if (true$$)
        if (true)
        {
        }
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

        [WorkItem("https://github.com/dotnet/roslyn/issues/3944")]
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

        [WorkItem("https://github.com/dotnet/roslyn/issues/3944")]
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

        [WorkItem("https://github.com/dotnet/roslyn/issues/3944")]
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

        [WorkItem("https://github.com/dotnet/roslyn/issues/3944")]
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

        [WorkItem("https://github.com/dotnet/roslyn/issues/3944")]
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

        [WorkItem("https://github.com/dotnet/roslyn/issues/3944")]
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

        [WorkItem("https://github.com/dotnet/roslyn/issues/3944")]
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

        [WorkItem("https://github.com/dotnet/roslyn/issues/3944")]
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

        [WorkItem("https://github.com/dotnet/roslyn/issues/3944")]
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

        [WorkItem("https://github.com/dotnet/roslyn/issues/3944")]
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

        [WorkItem("https://github.com/dotnet/roslyn/issues/3944")]
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

        [WorkItem("https://github.com/dotnet/roslyn/issues/3944")]
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

        [WorkItem("https://github.com/dotnet/roslyn/issues/3944")]
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

        [WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530352")]
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

        [WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530716")]
        [WpfFact]
        public void DoNotAssertOnMultilineToken()
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

        [WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530718")]
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

        [WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/150480")]
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

        [WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/150480")]
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

        [WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/150480")]
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

        [WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/150480")]
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

        [WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/150480")]
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

        [WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/150480")]
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

        [WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/150480")]
        [WpfFact]
        public void NotDelegatedAfterOpenBraceObjectCreationExpression()
        {
            Test(@"class TestClass
{
    void Method()
    {
        var pet = new List<int>();
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

        [WpfFact]
        public void TestMulitpleNamespace()
        {
            Test($@"
namespace Bar2
{{
    $$
}}
namespace Bar
{{
}}", $@"
namespace B$$ar2$$
namespace Bar
{{
}}");
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
pu$$blic {typeKeyword} $$Bar$$");
        }

        [WpfTheory]
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
pub$$lic {typeKeyword} B$$ar2$$
public {typeKeyword} Bar
{{
}}");
        }

        [WpfFact]
        public void TestNestedTypeDeclaration()
        {
            Test(@"
public class Bar1
{
    public class Bar2
    {
        $$
    }
}",
@"
public class Bar1
{
    pu$$blic cla$$ss B$$ar2$$
}");
        }

        [WpfFact]
        public void TestNestedNamespace()
        {
            Test(@"
namespace Bar1
{
    namespace Bar2
    {
        $$
    }
}",
@"
namespace Bar1
{
    namespa$$ce $$B$$ar2$$
}");
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
pub$$lic {typeKeyword} B$$ar {{$$");
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
                $@"public {typeKeyword}$$ Ba$$r {{}}$$");
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
    v$$oid Ma$$in($$)$$
}");
        }

        [WpfFact]
        public void TestConstructor()
        {
            Test(@"
public class Bar
{
    void Bar()
    {
        $$
    }
}", @"
public class Bar
{
    v$$oid Ba$$r($$)$$
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
    v$$oid Mai$$n($$)$$;
}");
        }

        [WpfFact]
        public void TestMissingSemicolonMethodInInterface()
        {
            Test(@"
public interface Bar
{
    void Main()
        $$
}", @"
public interface Bar
{
    v$$oid Mai$$n($$)$$
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
        v$$oid Loc$$al($$)$$
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
        v$$oid Loca$$l($$)$$
    }
}");
        }

        [WpfFact]
        public void TestIndexerAsLastElementInClass()
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
    p$$ublic in$$t thi$$s[in$$t i]$$
}");
        }

        [WpfFact]
        public void TestIndexerNotAsLastElementInClass()
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
    p$$ublic in$$t thi$$s[in$$t i]$$
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
    p$$ublic i$$nt thi$$s[in$$t i]$$
    {
    }
}");
        }

        [WpfFact]
        public void TestGetAccessorOfProperty()
        {
            var initialMarkup = @"
public class Bar
{
    public int P
    {
        ge$$t$$
    }
}";

            var firstResult = @"
public class Bar
{
    public int P
    {
        get
        {
            $$
        }
    }
}";
            var secondResult = @"
public class Bar
{
    public int P
    {
        get;
        $$
    }
}";
            Test(firstResult, initialMarkup);
            Test(secondResult, firstResult);
        }

        [WpfFact]
        public void TestSetAccessorOfProperty()
        {
            var initialMarkup = @"
public class Bar
{
    public int P
    {
        set$$
    }
}";
            var firstResult = @"
public class Bar
{
    public int P
    {
        set
        {
            $$
        }
    }
}";
            var secondResult = @"
public class Bar
{
    public int P
    {
        set;
        $$
    }
}";
            Test(firstResult, initialMarkup);
            Test(secondResult, firstResult);
        }

        [WpfFact]
        public void TestGetAccessorOfIndexer()
        {
            Test(@"
public class Bar
{
    public int this[int i]
    {
        get
        {
            $$
        }
    }
}", @"
public class Bar
{
    public int this[int i]
    {
        ge$$t$$
    }
}");
        }

        [WpfFact]
        public void TestValidGetAccessorOfIndexer()
        {
            Test(@"
public class Bar
{
    public int this[int i]
    {
        get
        {

            $$
        }
    }
}", @"
public class Bar
{
    public int this[int i]
    {
        get
        {
            $$
        }
    }
}");
        }

        [WpfFact]
        public void TestNonEmptyGetAccessor()
        {
            Test(@"
public Class Bar
{
    public int P
    {
        get
        {
            if (true)
            $$
            {
                return 1;
            }
        }
    }   
}",
                @"
public Class Bar
{
    public int P
    {
        get
        {
            i$$f ($$true$$)$$
            {
                return 1;
            }
        }
    }   
}");
        }

        [WpfFact]
        public void TestNonEmptySetAccessor()
        {
            Test(@"
public Class Bar
{
    public int P
    {
        get;
        set
        {
            if (true)
            $$
            {
            }
        }
    }   
}",
                @"
public Class Bar
{
    public int P
    {
        get;
        set
        {
            i$$f (t$$rue)$$
            {
            }
        }
    }   
}");
        }

        [WpfFact]
        public void TestSetAccessorOfIndexer()
        {
            Test(@"
public class Bar
{
    public int this[int i]
    {
        get;
        set
        {
            $$
        }
    }
}", @"
public class Bar
{
    public int this[int i]
    {
        get;
        se$$t$$
    }
}");
        }

        [WpfFact]
        public void TestValidSetAccessorOfIndexer()
        {
            Test(@"
public class Bar
{
    public int this[int i]
    {
        get;
        set
        {

            $$
        }
    }
}", @"
public class Bar
{
    public int this[int i]
    {
        get;
        set
        {
            $$
        }
    }
}");
        }

        [WpfFact]
        public void TestAddAccessorInEventDeclaration()
        {
            Test(@"
using System;
public class Bar
{
    public event EventHandler e
    {
        add
        {
            $$
        }
        remove
    }
}", @"
using System;
public class Bar
{
    public event EventHandler e
    {
        ad$$d$$
        remove
    }
}");
        }

        [WpfFact]
        public void TestValidAddAccessorInEventDeclaration()
        {
            Test(@"
using System;
public class Bar
{
    public event EventHandler e
    {
        add
        {

            $$
        }
        remove { }
    }
}", @"
using System;
public class Bar
{
    public event EventHandler e
    {
        add
        {
            $$
        }
        remove { }
    }
}");
        }

        [WpfFact]
        public void TestRemoveAccessor()
        {
            Test(@"
using System;
public class Bar
{
    public event EventHandler e
    {
        add
        remove
        {
            $$
        }
    }
}", @"
using System;
public class Bar
{
    public event EventHandler e
    {
        add
        remo$$ve$$
    }
}");
        }

        [WpfFact]
        public void TestValidRemoveAccessor()
        {
            Test(@"
using System;
public class Bar
{
    public event EventHandler e
    {
        add { }
        remove
        {

            $$
        }
    }
}", @"
using System;
public class Bar
{
    public event EventHandler e
    {
        add { }
        remove
        {
            $$
        }
    }
}");
        }

        [WpfFact]
        public void TestField()
        {
            var initialMarkup = @"
public class Bar
{
    p$$ublic i$$nt i$$ii$$
}";
            var firstResult = @"
public class Bar
{
    public int iii
    {
        $$
    }
}";
            var secondResult = @"
public class Bar
{
    public int iii;
    $$
}";

            Test(firstResult, initialMarkup);
            Test(secondResult, firstResult);
        }

        [WpfFact]
        public void TestReadonlyField()
        {
            Test(@"
public class Bar
{
    public readonly int iii;
    $$
}", @"
public class Bar
{
    p$$ublic reado$$nly i$$nt i$$ii$$
}");
        }

        [WpfFact]
        public void TestNonEmptyProperty()
        {
            Test(@"
public class Bar
{
    public int Foo
    {
        get { }
        $$
    }
}", @"
public class Bar
{
    public int Foo
    {
        $$get$$ { }$$
    }
}");
        }

        [WpfFact]
        public void TestMulitpleFields()
        {
            Test(@"
public class Bar
{
    public int apple, banana;
    $$
}", @"
public class Bar
{
    p$$ublic i$$nt ap$$ple$$, ba$$nana;$$
}");
        }

        [WpfFact]
        public void TestMultipleEvents()
        {
            Test(@"
using System;
public class Bar
{
    public event EventHandler apple, banana;
    $$
}", @"
using System;
public class Bar
{
    p$$ublic event EventHandler ap$$ple$$, ba$$nana$$;$$
}");
        }

        [WpfFact]
        public void TestEvent()
        {
            var initialMarkup = @"
using System;
public class Bar
{
    pu$$blic e$$vent EventHand$$ler c$$c$$
}";
            var firstResult = @"
using System;
public class Bar
{
    public event EventHandler cc
    {
        $$
    }
}";
            var secondResult = @"
using System;
public class Bar
{
    public event EventHandler cc;
    $$
}";
            Test(firstResult, initialMarkup);
            Test(secondResult, firstResult);
        }

        [WpfFact]
        public void TestNonEmptyEvent()
        {
            Test(@"
using System;
public class Bar
{
    public event EventHandler Foo
    {
        add { }
        $$
    }
}", @"
using System;
public class Bar
{
    public event EventHandler Foo
    {
        $$add$$ {$$ }$$
    }
}");
        }

        [WpfFact]
        public void TestObjectCreationExpressionWithParenthesis()
        {
            var initialMarkup = @"
public class Bar
{
    public void M()
    {
        var f = n$$ew F$$oo($$)$$
    }
}
public class Foo
{
    public int HH { get; set; }
    public int PP { get; set; }
}";

            var firstResult = @"
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
}";

            var secondResult = @"
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
}";

            Test(firstResult, initialMarkup);
            Test(secondResult, firstResult);
        }

        [WpfFact]
        public void TestObjectCreationExpressionWithNoParenthesis()
        {
            var initialMarkUp = @"
public class Bar
{
    public void M()
    {
        var f = n$$ew F$$oo$$
    }
}
public class Foo
{
    public int HH { get; set; }
    public int PP { get; set; }
}";

            var firstResult = @"
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
}";

            var secondResult = @"
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
}";

            Test(firstResult, initialMarkUp);
            Test(secondResult, firstResult);
        }

        [WpfFact]
        public void TestObjectCreationExpressionWithCorrectSemicolon()
        {
            var initialMarkUp = @"
public class Bar
{
    public void M()
    {
        var f = n$$ew F$$oo$$;
    }
}
public class Foo
{
    public int HH { get; set; }
    public int PP { get; set; }
}";

            var firstResult = @"
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
}";

            var secondResult = @"
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
}";

            Test(firstResult, initialMarkUp);
            Test(secondResult, firstResult);
        }

        [WpfFact]
        public void TestObjectCreationExpressionUsedAsExpression()
        {
            var initialMarkUp = @"
public class Bar
{
    public void M()
    {
        N(ne$$w Fo$$o$$);
    }

    private void N(Foo f)
    {
    }
}
public class Foo
{
    public int HH { get; set; }
    public int PP { get; set; }
}";

            var firstResult = @"
public class Bar
{
    public void M()
    {
        N(new Foo()
        {
            $$
        });
    }

    private void N(Foo f)
    {
    }
}
public class Foo
{
    public int HH { get; set; }
    public int PP { get; set; }
}";

            var secondResult = @"
public class Bar
{
    public void M()
    {
        N(new Foo());
        $$
    }

    private void N(Foo f)
    {
    }
}
public class Foo
{
    public int HH { get; set; }
    public int PP { get; set; }
}";

            Test(firstResult, initialMarkUp);
            Test(secondResult, firstResult);
        }

        [WpfFact]
        public void TestObjectCreationExpressionInUsingStatement()
        {
            var initialMarkup = @"
public class Bar
{
    public void M()
    {
        using(var a = n$$ew F$$oo($$)$$)
    }
}
public class Foo
{
    public int HH { get; set; }
    public int PP { get; set; }
}";

            var firstResult = @"
public class Bar
{
    public void M()
    {
        using(var a = new Foo()
        {
            $$
        })
    }
}
public class Foo
{
    public int HH { get; set; }
    public int PP { get; set; }
}";

            var secondResult = @"
public class Bar
{
    public void M()
    {
        using(var a = new Foo())
            $$
    }
}
public class Foo
{
    public int HH { get; set; }
    public int PP { get; set; }
}";

            Test(firstResult, initialMarkup);
            Test(secondResult, firstResult);
        }

        [WpfFact]
        public void TestObjectCreationExpressionWithNonEmptyInitializer()
        {
            Test(
                @"
public class Bar
{
    public void M()
    {
        var a = new Foo() { HH = 1, PP = 2 };
        $$
    }
}
public class Foo
{
    public int HH { get; set; }
    public int PP { get; set; }
}",
                @"
public class Bar
{
    public void M()
    {
        var a = n$$ew Fo$$o($$) {$$ HH = 1$$, PP = 2 $$};
    }
}
public class Foo
{
    public int HH { get; set; }
    public int PP { get; set; }
}");

        }

        [WpfFact]
        public void TestArrayInitializer1()
        {
            Test(
                """
                using System.Collections.Generic;
                public class Bar
                {
                    public void M()
                    {
                        int[] a = new int[] { 1, 2 };
                        $$
                    }
                }
                """,
                """
                using System.Collections.Generic;
                public class Bar
                {
                    public void M()
                    {
                        int[] a = n$$ew in$$t[$$]$$ {$$ 1$$, 2 $$};
                    }
                }
                """);
        }

        [WpfFact]
        public void TestArrayInitializer2()
        {
            Test(
                """
                using System.Collections.Generic;
                public class Bar
                {
                    public void M()
                    {
                        int[] a = new[] { 1, 2 };
                        $$
                    }
                }
                """,
                """
                using System.Collections.Generic;
                public class Bar
                {
                    public void M()
                    {
                        int[] a = n$$ew[$$]$$ {$$ 1$$, 2 $$};
                    }
                }
                """);
        }

        [WpfFact]
        public void TestCollectionInitializerWithNonEmptyInitializer()
        {
            Test(
                """
                using System.Collections.Generic;
                public class Bar
                {
                    public void M()
                    {
                        var a = new List<int>() { 1, 2 };
                        $$
                    }
                }
                """,
                """
                using System.Collections.Generic;
                public class Bar
                {
                    public void M()
                    {
                        var a = n$$ew Lis$$t<int$$>($$) {$$ 1$$, 2 $$};
                    }
                }
                """);
        }

        [WpfFact]
        public void TestCollectionExpression()
        {
            Test(
                """
                using System.Collections.Generic;
                public class Bar
                {
                    public void M()
                    {
                        int[] a = [1, 2];
                        $$
                    }
                }
                """,
                """
                using System.Collections.Generic;
                public class Bar
                {
                    public void M()
                    {
                        int[] a = $$[$$ 1$$, 2 $$];
                    }
                }
                """);
        }

        [WpfFact]
        public void TestIfStatementWithInnerStatement()
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
        var z = 1;
    }
}", @"
public class Bar
{
    public void Main(bool x)
    {
        i$$f$$ ($$x)$$
        var z = 1;
    }
}");
        }

        [WpfFact]
        public void TestIfStatementWithFollowingElseClause()
        {
            Test(@"
public class Bar
{
    public void Main(bool x)
    {
        if (x)
        {
            $$
            var z = 1;
        }
        else if (!x)
    }
}", @"
public class Bar
{
    public void Main(bool x)
    {
        i$$f$$ ($$x)$$
        var z = 1;
        else if (!x)
    }
}");
        }

        [WpfFact]
        public void TestIfStatementWithoutStatement()
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
    }
}", @"
public class Bar
{
    public void Main(bool x)
    {
        i$$f$$ ($$x)$$
    }
}");
        }

        [WpfFact]
        public void TestNestIfStatementWithInnerStatement()
        {
            Test(@"
public class Bar
{
    public void Main(int x)
    {
        if (x == 1)
            if (x == 2)
                if (x == 3)
                    if (x == 4)
                    {
                        $$
                        var a = 1000;
                    }
    }
}", @"
public class Bar
{
    public void Main(int x)
    {
        if (x == 1)
            if (x == 2)
                if (x == 3)
                    i$$f ($$x =$$= 4)$$
                        var a = 1000;
    }
}");
        }

        [WpfFact]
        public void TestNestIfStatementWithoutInnerStatement()
        {
            Test(@"
public class Bar
{
    public void Main(int x)
    {
        if (x == 1)
            if (x == 2)
                if (x == 3)
                    if (x == 4)
                    {
                        $$
                    }
    }
}", @"
public class Bar
{
    public void Main(int x)
    {
        if (x == 1)
            if (x == 2)
                if (x == 3)
                    i$$f ($$x =$$= 4)$$
    }
}");
        }

        [WpfFact]
        public void TestNestedElseIfStatementWithInnerStatement()
        {
            Test(@"
public class Bar
{
    public void Fo(int i)
    {
        if (i == 1)
        {
        }
        else if (i == 2)
            if (i == 3)
            {
                $$
                var i = 10;
            }
            else
            {
            }
    }
}", @"
public class Bar
{
    public void Fo(int i)
    {
        if (i == 1)
        {
        }
        else if (i == 2)
            i$$f (i$$ == 3)$$
                var i = 10;
            else
            {
            }
    }
}");
        }

        [WpfFact]
        public void TestNestIfElseStatementWithBlockWithInnerStatement()
        {
            Test(@"
public class Bar
{
    public void Main(int x)
    {
        if (x == 1)
            if (x == 2)
                if (x == 3)
                {
                    if (x == 4)
                    {
                        $$
                    }
                    var i = 10;
                }
                else
                {
                }
    }
}", @"
public class Bar
{
    public void Main(int x)
    {
        if (x == 1)
            if (x == 2)
                if (x == 3)
                {
                    i$$f ($$x =$$= 4)$$
                    var i = 10;
                }
                else
                {
                }
    }
}");
        }

        [WpfFact]
        public void TestEmptyDoStatement()
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
        d$$o$$
    }
}");
        }

        [WpfFact]
        public void TestDoStatementWithInnerStatement()
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
        var c = 10;
    }
}", @"
public class Bar
{
    public void Main()
    {
        d$$o$$
        var c = 10;
    }
}");
        }

        [WpfFact]
        public void TestDoStatementWithWhileClause()
        {
            Test(@"
public class Bar
{
    public void Main()
    {
        do
        {
            $$
            var c = 10;
        }
        while (true);
    }
}", @"
public class Bar
{
    public void Main()
    {
        d$$o$$
        var c = 10;
        while (true);
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
        e$$lse$$
    }
}");
        }

        [WpfFact]
        public void TestElseStatementWithInnerStatement()
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
        var c = 10;
    }
}", @"
public class Bar
{
    public void Fo()
    {
        if (true)
        {
        }
        e$$lse$$
        var c = 10;
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
        e$$lse i$$f ($$false)$$
    }
}");
        }

        [WpfFact]
        public void TestElseIfInTheMiddleWithInnerStatement()
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
            var i = 10;
        }
        else
        {
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
        e$$lse i$$f ($$false)$$
        var i = 10;
        else
        {
        }
    }
}");
        }

        [WpfFact]
        public void TestElseClauseInNestedIfStatement()
        {
            Test(@"
public class Bar
{
    public void Fo(int i)
    {
        if (i == 1)
        {
            if (i == 2)
                var i = 10;
            else
            {
                $$
            }
            var c = 100;
        }
    }
}", @"
public class Bar
{
    public void Fo(int i)
    {
        if (i == 1)
        {
            if (i == 2)
                var i = 10;
            el$$se
            var c = 100;
        }
    }
}");
        }

        [WpfFact]
        public void TestForStatementWithoutStatement()
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
        f$$or (i$$nt i; i < 10;$$ i++)$$
    }
}");
        }

        [WpfFact]
        public void TestForStatementWithInnerStatement()
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
        var c = 10;
    }
}", @"
public class Bar
{
    public void Fo()
    {
        f$$or (i$$nt i; i < 10;$$ i++)$$
        var c = 10;
    }
}");
        }

        [WpfFact]
        public void TestForEachStatementWithoutInnerStatement()
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
        var c = 10;
    }
}", @"
public class Bar
{
    public void Fo()
    {
        forea$$ch (var x $$in """")$$
        var c = 10;
    }
}");
        }

        [WpfFact]
        public void TestLockStatementWithoutInnerStatement()
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
        l$$ock$$(o)$$
    }
}");
        }

        [WpfFact]
        public void TestLockStatementWithInnerStatement()
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
        var i = 10;
    }
}", @"
public class Bar
{
    object o = new object();
    public void Fo()
    {
        l$$ock$$(o)$$
        var i = 10;
    }
}");
        }

        [WpfFact]
        public void TestUsingStatementWithoutInnerStatement()
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
        usi$$ng (va$$r d = new D())$$
    }
}
public class D : IDisposable
{
    public void Dispose()
    {}
}");
        }

        [WpfFact]
        public void TestUsingStatementWithInnerStatement()
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
        var c = 10;
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
        usi$$ng (va$$r d = new D())$$
        var c = 10;
    }
}
public class D : IDisposable
{
    public void Dispose()
    {}
}");
        }

        [WpfFact]
        public void TestUsingInLocalDeclarationStatement()
        {
            Test(@"
using System;
public class Bar
{
    public void Fo()
    {
        using var d = new D();
        $$
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
        usi$$ng v$$ar$$ d = new D()
    }
}
public class D : IDisposable
{
    public void Dispose()
    {}
}");
        }

        [WpfFact]
        public void TestWhileStatementWithoutInnerStatement()
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
        wh$$ile (tr$$ue)$$
    }
}");
        }

        [WpfFact]
        public void TestWhileStatementWithInnerStatement()
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
        var c = 10;
    }
}", @"
public class Bar
{
    public void Fo()
    {
        wh$$ile (tr$$ue)$$
        var c = 10;
    }
}");
        }

        [WpfFact]
        public void TestSwitchExpression1()
        {
            Test(@"
public class Bar
{
    public void Goo(int c)
    {
        var d = c switch
        {
            $$
        }
    }
}",
                @"
public class Bar
{
    public void Goo(int c)
    {
        var d = c swi$$tch$$
    }
}");

        }

        [WpfFact]
        public void TestSwitchExpression2()
        {
            Test(@"
public class Bar
{
    public void Goo(int c)
    {
        var d = (c + 1) switch
        {
            $$
        }
    }
}",
                @"
public class Bar
{
    public void Goo(int c)
    {
        var d = (c + 1) swi$$tch$$
    }
}");

        }

        [WpfFact]
        public void TestSwitchStatementWithOnlyOpenParenthesis()
        {
            // This test is to make sure {} will be added to the switch statement,
            // but our formatter now can't format the case when the CloseParenthesis token is missing.
            // If any future formatter improvement can handle this case, this test can be modified safely
            Test(@"
public class bar
{
    public void TT()
    {
        switch (
{
            $$
        }
    }
}", @"
public class bar
{
    public void TT()
    {
        swi$$tch ($$
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
        switc$$h ($$i)$$
    }
}");
        }

        [WpfFact]
        public void TestValidSwitchStatement()
        {
            Test(@"
public class bar
{
    public void TT()
    {
        int i = 10;
        switch (i)
            $$
        {
        }
    }
}", @"
public class bar
{
    public void TT()
    {
        int i = 10;
        switc$$h ($$i)$$
        {
        }
    }
}");
        }

        [WpfFact]
        public void TestValidTryStatement()
        {
            Test(@"
public class bar
{
    public void TT()
    {
        try
            $$
        {
        }
    }
}", @"
public class bar
{
    public void TT()
    {
        tr$$y$$
        {
        }
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
        tr$$y$$
    }
}");
        }

        [WpfFact]
        public void TestValidCatchClause()
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
        $$
        {
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
        cat$$ch (Syste$$m.Exception)$$
        {
        }
    }
}");
        }

        [WpfFact]
        public void TestCatchClauseWithException()
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
        cat$$ch (Syste$$m.Exception)$$
    }
}");
        }

        [WpfFact]
        public void TestSingleCatchClause()
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
        cat$$ch$$
    }
}");
        }

        [WpfFact]
        public void TestCatchClauseWithWhenClause()
        {
            Test(@"
public class bar
{
    public void TT()
    {
        try
        {
        }
        catch (Exception) when (true)
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
        c$$atch (Ex$$ception) whe$$n (tru$$e)$$
    }
}");
        }

        [WpfFact]
        public void TestFinallyCaluse()
        {
            Test(@"
public class Bar
{
    public void Bar2()
    {
        try
        {
        }
        catch (System.Exception)
        {
        }
        finally
        {
            $$
        }
    }
}", @"
public class Bar
{
    public void Bar2()
    {
        try
        {
        }
        catch (System.Exception)
        {
        }
        fin$$ally$$
    }
}");
        }

        [WpfFact]
        public void TestValidFinallyCaluse()
        {
            Test(@"
public class Bar
{
    public void Bar2()
    {
        try
        {
        }
        catch (System.Exception)
        {
        }
        finally
        $$
        {
        }
    }
}", @"
public class Bar
{
    public void Bar2()
    {
        try
        {
        }
        catch (System.Exception)
        {
        }
        fin$$ally$$
        {
        }
    }
}");
        }

        [WpfFact]
        public void TestObjectCreationExpressionWithMissingType()
        {
            Test(@"
public class Bar
{
    public void Bar2()
    {
        Bar b = new()
        {
            $$
        };
    }
}",
@"
public class Bar
{
    public void Bar2()
    {
        Bar b = new$$
    }
}");
        }

        [WpfFact]
        public void TestRemoveInitializerForImplicitObjectCreationExpression()
        {
            Test(@"
public class Bar
{
    public void Bar2()
    {
        Bar b = new();
        $$
    }
}",
@"
public class Bar
{
    public void Bar2()
    {
        Bar b = new()
        {
            $$
        };
    }
}");
        }

        [WpfTheory]
        [InlineData("checked")]
        [InlineData("unchecked")]
        public void TestCheckedStatement(string keywordToken)
        {
            Test($@"
public class Bar
{{
    public void Bar2()
    {{
        {keywordToken}
        {{
            $$
        }}
    }}
}}",
$@"
public class Bar
{{
    public void Bar2()
    {{
        {keywordToken}$$
    }}
}}");
        }

        [WpfTheory]
        [InlineData("checked")]
        [InlineData("unchecked")]
        public void TextCheckedExpression(string keywordToken)
        {
            Test($@"
public class Bar
{{
    public void Bar2()
    {{
        var i = {keywordToken}(1 + 1);
        $$
    }}
}}",
$@"
public class Bar
{{
    public void Bar2()
    {{
        var i = {keywordToken}$$(1 +$$ 1)$$
    }}
}}");
        }

        [WpfFact]
        public void TestConvertFieldToPropertyWithAttributeAndComment()
        {
            Test(@"
public class Bar
{
    public int Property
    {
        $$
    }

    /// <summary>
    /// </summary>
    [SomeAttri]
    public void Method() { }
}",
@"
public class Bar
{
    public int Property$$

    /// <summary>
    /// </summary>
    [SomeAttri]
    public void Method() { }
}");
        }

        [WpfFact]
        public void TestConvertEventFieldToPropertyWithAttributeAndComment()
        {
            Test(@"
public class Bar
{
    public event EventHandler MyEvent
    {
        $$
    }

    /// <summary>
    /// </summary>
    [SomeAttri]
    public void Method() { }
}",
@"
public class Bar
{
    public event EventHandler MyEvent$$

    /// <summary>
    /// </summary>
    [SomeAttri]
    public void Method() { }
}");
        }

        protected override string Language => LanguageNames.CSharp;

        protected override Action CreateNextHandler(EditorTestWorkspace workspace)
            => () => { };

        internal override IChainedCommandHandler<AutomaticLineEnderCommandArgs> GetCommandHandler(EditorTestWorkspace workspace)
        {
            return Assert.IsType<AutomaticLineEnderCommandHandler>(
                workspace.GetService<ICommandHandler>(
                    ContentTypeNames.CSharpContentType,
                    PredefinedCommandHandlerNames.AutomaticLineEnder));
        }
    }
}
