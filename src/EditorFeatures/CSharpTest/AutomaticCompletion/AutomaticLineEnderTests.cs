// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Microsoft.CodeAnalysis.Editor.Commands;
using Microsoft.CodeAnalysis.Editor.CSharp.AutomaticCompletion;
using Microsoft.CodeAnalysis.Editor.Host;
using Microsoft.CodeAnalysis.Editor.UnitTests.AutomaticCompletion;
using Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text.Operations;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.AutomaticCompletion
{
    public class AutomaticLineEnderTests : AbstractAutomaticLineEnderTests
    {
        [Fact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
        public void Creation()
        {
            Test(@"
$$", "$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
        public void Usings()
        {
            Test(@"using System;
$$", @"using System$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
        public void Namespace()
        {
            Test(@"namespace {}
$$", @"namespace {$$}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
        public void Class()
        {
            Test(@"class {}
$$", "class {$$}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
        public void Method()
        {
            Test(@"class C
{
    void Method() { }
    $$
}", @"class C
{
    void Method() {$$}
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
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

        [Fact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
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

        [Fact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
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

        [Fact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
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

        [Fact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
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

        [Fact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
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

        [Fact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
        public void EmbededStatement()
        {
            Test(@"class C
{
    void Method()
    {
        if (true) 
            $$
    }
}", @"class C
{
    void Method()
    {
        if (true) $$
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
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

        [Fact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
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

        [Fact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
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

        [Fact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
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
        [Fact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
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
        [Fact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
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
        [Fact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
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
        [Fact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
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
        [Fact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
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
        [Fact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
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
        [Fact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
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
        [Fact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
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
        [Fact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
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
        [Fact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
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
        [Fact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
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
        [Fact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
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
        [Fact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
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

        [Fact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
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

        [Fact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
        public void Format_Using()
        {
            Test(@"using System.Linq;
$$", @"         using           System          .                   Linq            $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
        public void Format_Using2()
        {
            Test(@"using
    System.Linq;
$$", @"         using           
             System          .                   Linq            $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
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

        [Fact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
        public void Statement_Trivia()
        {
            Test(@"class C
{
    void foo()
    {
        foo(); //comment
        $$
    }
}", @"class C
{
    void foo()
    {
        foo()$$ //comment
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
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

        [Fact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
        public void CompletionSetUp()
        {
            Test(@"class Program
{
    object foo(object o)
    {
        return foo();
        $$
    }
}", @"class Program
{
    object foo(object o)
    {
        return foo($$)
    }
}", completionActive: true);
        }

        [WorkItem(530352)]
        [Fact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
        public void EmbededStatement3()
        {
            Test(@"class Program
{
    void Method()
    {
        foreach (var x in y)
            $$
    }
}", @"class Program
{
    void Method()
    {
        foreach (var x in y$$)
    }
}");
        }

        [WorkItem(530716)]
        [Fact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
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

        [WorkItem(530718)]
        [Fact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
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

        [Fact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
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

        [Fact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
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

        [Fact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
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

        [Fact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
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

        [Fact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
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

        protected override TestWorkspace CreateWorkspace(string[] code)
        {
            return CSharpWorkspaceFactory.CreateWorkspaceFromLines(code);
        }

        protected override Action CreateNextHandler(TestWorkspace workspace)
        {
            return () => { };
        }

        internal override ICommandHandler<AutomaticLineEnderCommandArgs> CreateCommandHandler(
            Microsoft.CodeAnalysis.Editor.Host.IWaitIndicator waitIndicator,
            ITextUndoHistoryRegistry undoRegistry,
            IEditorOperationsFactoryService editorOperations)
        {
            return new AutomaticLineEnderCommandHandler(waitIndicator, undoRegistry, editorOperations);
        }
    }
}
