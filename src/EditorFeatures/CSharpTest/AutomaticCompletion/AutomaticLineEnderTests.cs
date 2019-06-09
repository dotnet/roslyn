// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Microsoft.CodeAnalysis.Editor.CSharp.AutomaticCompletion;
using Microsoft.CodeAnalysis.Editor.UnitTests.AutomaticCompletion;
using Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.VisualStudio.Commanding;
using Microsoft.VisualStudio.Text.Editor.Commanding.Commands;
using Microsoft.VisualStudio.Text.Operations;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.AutomaticCompletion
{
    public class AutomaticLineEnderTests : AbstractAutomaticLineEnderTests
    {
        [WpfFact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
        public void Creation()
        {
            Test(@"
$$", "$$");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
        public void Usings()
        {
            Test(@"using System;
$$", @"using System$$");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
        public void Namespace()
        {
            Test(@"namespace {}
$$", @"namespace {$$}");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
        public void Class()
        {
            Test(@"class {}
$$", "class {$$}");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
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

        [WpfFact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
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

        [WpfFact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
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

        [WpfFact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
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

        [WpfFact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
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

        [WpfFact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
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

        [WpfFact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
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

        [WpfFact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
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

        [WpfFact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
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

        [WpfFact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
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

        [WpfFact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
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

        [WpfFact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
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
        [WpfFact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
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
        [WpfFact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
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
        [WpfFact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
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
        [WpfFact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
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
        [WpfFact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
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
        [WpfFact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
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
        [WpfFact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
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
        [WpfFact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
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
        [WpfFact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
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
        [WpfFact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
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
        [WpfFact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
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
        [WpfFact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
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
        [WpfFact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
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

        [WpfFact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
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

        [WpfFact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
        public void Format_Using()
        {
            Test(@"using System.Linq;
$$", @"         using           System          .                   Linq            $$");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
        public void Format_Using2()
        {
            Test(@"using
    System.Linq;
$$", @"         using           
             System          .                   Linq            $$");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
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

        [WpfFact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
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

        [WpfFact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
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

        [WpfFact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
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
        [WpfFact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
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

        [WorkItem(530716, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530716")]
        [WpfFact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
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
        [WpfFact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
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

        [WpfFact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
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

        [WpfFact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
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

        [WpfFact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
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

        [WpfFact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
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

        [WpfFact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
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
        [WpfFact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
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
        [WpfFact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
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
        [WpfFact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
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
        [WpfFact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
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
        [WpfFact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
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
        [WpfFact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
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
        [WpfFact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
        public void NotDelegatedAfterOpenBraceObjectCreationExpression()
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

        internal override IChainedCommandHandler<AutomaticLineEnderCommandArgs> CreateCommandHandler(
            ITextUndoHistoryRegistry undoRegistry,
            IEditorOperationsFactoryService editorOperations)
        {
            return new AutomaticLineEnderCommandHandler(undoRegistry, editorOperations);
        }
    }
}
