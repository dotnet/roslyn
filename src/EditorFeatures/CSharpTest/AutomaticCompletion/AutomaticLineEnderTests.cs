// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor.Commands;
using Microsoft.CodeAnalysis.Editor.CSharp.AutomaticCompletion;
using Microsoft.CodeAnalysis.Editor.UnitTests.AutomaticCompletion;
using Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;
using Microsoft.VisualStudio.Text.Operations;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.AutomaticCompletion
{
    public class AutomaticLineEnderTests : AbstractAutomaticLineEnderTests
    {
        [WpfFact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
        public async Task Creation()
        {
            Test(@"
$$", "$$");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
        public async Task Usings()
        {
            Test(@"using System;
$$", @"using System$$");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
        public async Task Namespace()
        {
            Test(@"namespace {}
$$", @"namespace {$$}");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
        public async Task Class()
        {
            Test(@"class {}
$$", "class {$$}");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
        public async Task Method()
        {
            Test(@"class C
{
    void Method() {$$}
}", @"class C
{
    void Method() {$$}
}", assertNextHandlerInvoked: true);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
        public async Task Field()
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

        [WpfFact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
        public async Task EventField()
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

        [WpfFact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
        public async Task Field2()
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

        [WpfFact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
        public async Task EventField2()
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

        [WpfFact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
        public async Task Field3()
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

        [WpfFact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
        public async Task EventField3()
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

        [WpfFact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
        public async Task EmbededStatement()
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

        [WpfFact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
        public async Task EmbededStatement1()
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

        [WpfFact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
        public async Task EmbededStatement2()
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

        [WpfFact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
        public async Task Statement()
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

        [WpfFact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
        public async Task Statement1()
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
        [WpfFact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
        public async Task ExpressionBodiedMethod()
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
        [WpfFact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
        public async Task ExpressionBodiedOperator()
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
        [WpfFact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
        public async Task ExpressionBodiedConversionOperator()
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
        [WpfFact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
        public async Task ExpressionBodiedProperty()
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
        [WpfFact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
        public async Task ExpressionBodiedIndexer()
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
        [WpfFact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
        public async Task ExpressionBodiedMethodWithBlockBodiedAnonymousMethodExpression()
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
        [WpfFact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
        public async Task ExpressionBodiedMethodWithSingleLineBlockBodiedAnonymousMethodExpression()
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
        [WpfFact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
        public async Task ExpressionBodiedMethodWithBlockBodiedSimpleLambdaExpression()
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
        [WpfFact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
        public async Task ExpressionBodiedMethodWithExpressionBodiedSimpleLambdaExpression()
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
        [WpfFact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
        public async Task ExpressionBodiedMethodWithBlockBodiedAnonymousMethodExpressionInMethodArgs()
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
        [WpfFact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
        public async Task Format_SimpleExpressionBodiedMember()
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
        [WpfFact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
        public async Task Format_ExpressionBodiedMemberWithSingleLineBlock()
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
        [WpfFact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
        public async Task Format_ExpressionBodiedMemberWithMultiLineBlock()
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

        [WpfFact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
        public async Task Format_Statement()
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

        [WpfFact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
        public async Task Format_Using()
        {
            Test(@"using System.Linq;
$$", @"         using           System          .                   Linq            $$");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
        public async Task Format_Using2()
        {
            Test(@"using
    System.Linq;
$$", @"         using           
             System          .                   Linq            $$");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
        public async Task Format_Field()
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

        [WpfFact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
        public async Task Statement_Trivia()
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

        [WpfFact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
        public async Task TrailingText_Negative()
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

        [WpfFact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
        public async Task CompletionSetUp()
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

        [WorkItem(530352, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530352")]
        [WpfFact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
        public async Task EmbededStatement3()
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

        [WorkItem(530716, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530716")]
        [WpfFact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
        public async Task DontAssertOnMultilineToken()
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
        [WpfFact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
        public async Task AutomaticLineFormat()
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

        [WpfFact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
        public async Task NotAfterExisitingSemicolon()
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

        [WpfFact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
        public async Task NotAfterCloseBraceInMethod()
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

        [WpfFact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
        public async Task NotAfterCloseBraceInStatement()
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

        [WpfFact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
        public async Task NotAfterAutoPropertyAccessor()
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

        [WpfFact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
        public async Task NotAfterAutoPropertyDeclaration()
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
        [WpfFact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
        public async Task DelegatedInEmptyBlock()
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
        [WpfFact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
        public async Task DelegatedInEmptyBlock2()
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
        [WpfFact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
        public async Task NotDelegatedOutsideEmptyBlock()
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
        [WpfFact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
        public async Task NotDelegatedAfterOpenBraceAndMissingCloseBrace()
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
        [WpfFact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
        public async Task NotDelegatedInNonEmptyBlock()
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
        [WpfFact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
        public async Task NotDelegatedAfterOpenBraceInAnonymousObjectCreationExpression()
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
        [WpfFact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
        public async Task NotDelegatedAfterOpenBraceObjectCreationExpression()
        {
            Test(@"class TestClass
{
    void Method()
    {
        var pet = new List<int> { };
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

        protected override TestWorkspace CreateWorkspace(string code)
            => TestWorkspace.CreateCSharp(code);

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
