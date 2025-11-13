// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Formatting;
using Microsoft.CodeAnalysis.CSharp.Shared.Extensions;
using Microsoft.CodeAnalysis.Editor.UnitTests.CodeActions;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;
using static Microsoft.CodeAnalysis.CSharp.Formatting.CSharpFormattingOptions2;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.Formatting;

using static CSharpSyntaxTokens;

[Trait(Traits.Feature, Traits.Features.Formatting)]
public sealed class FormattingTests : CSharpFormattingTestBase
{
    [Fact]
    public async Task Format1()
        => await AssertFormatAsync("namespace A { }", "namespace A{}");

    [Fact]
    public Task Format2()
        => AssertFormatAsync("""
            class A
            {
            }
            """, """
            class A {
                        }
            """);

    [Fact]
    public Task Format3()
        => AssertFormatAsync("""
            class A
            {
                int i = 20;
            }
            """, """
            class A
                        {        
            int             i               =               20          ;           }
            """);

    [Fact]
    public Task Format4()
        => AssertFormatAsync("""
            class A
            {
                int i = 20; int j = 1 + 2;
                T.S           =           Test(           10              );
            }
            """, """
            class A
                        {        
            int             i               =               20          ;           int             j           =           1           +           2       ;
                                    T           .               S           =           Test            (           10              )           ;
                                    }
            """);

    [Fact]
    public Task Format5()
        => AssertFormatAsync("""
            class A
            {
                List<int> Method<TArg, TArg2>(TArg a, TArg2 b)
                {
                    int i = 20; int j = 1 + 2;
                    T.S = Test(10);
                }
            }
            """, """
            class A
                        {        
                List                    <           int             >                Method              <               TArg                ,           TArg2           >               (                   TArg                a,              TArg2                   b               )
                                {
            int             i               =               20          ;           int             j           =           1           +           2       ;
                                    T           .               S           =           Test            (           10              )           ;
                                    }           }
            """);

    [Fact]
    public Task Format6()
        => AssertFormatAsync("""
            class A
            {
                A a = new A
                {
                    Property1 = 1,
                    Property2 = 3,
                    Property3 = { 1, 2, 3 }
                };
            }
            """, """
            class A
                        {        
            A           a               =               new             A                   {
                               Property1             =                               1,                     Property2               =                       3,
                    Property3       =             {         1       ,           2           ,           3   }           };
                }
            """);

    [Fact]
    public Task Format7()
        => AssertFormatAsync("""
            class A
            {
                var a = from i in new[] { 1, 2, 3 } where i > 10 select i;
            }
            """, """
            class A
                        {        
                var             a           =           from        i       in          new        [  ]     {           1           ,       2           ,       3       }       where           i       >       10          select      i           ;           
            }
            """);

    [Fact]
    public Task Format8()
        => AssertFormatAsync("""
            class A
            {
                void Method()
                {
                    if (true)
                    {
                    }
                    else if (false)
                    {
                    }
                }
            }
            """, """
            class A
                        {        
            void Method()
            {
                                        if (true)
                                        {
                                        }
                                        else if (false)
                                        {
                                        }
            }
            }
            """);

    [Fact]
    public Task Format9()
        => AssertFormatAsync("""
            class A
            {
                void Method()
                {
                    if (true) { } else if (false) { }
                }
            }
            """, """
            class A
                        {        
            void Method()
            {
                                        if (true)                             {                             }                             else if (false)                              {                             }
            }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/16328")]
    public Task FormatElseIfOnSeparateLines()
        => AssertFormatAsync("""
            void Method()
            {
                if (true) { }
                else
                    if (false) { }
            }
            """, """
            void Method()
            {
                if (true) { }
                else
            if (false) { }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/16328")]
    public Task FormatElseReturnOnSeparateLines()
        => AssertFormatAsync("""
            void Method()
            {
                if (true) { }
                else
                    return;
            }
            """, """
            void Method()
            {
                if (true) { }
                else
            return;
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/16328")]
    public Task FormatElseWhileOnSeparateLines()
        => AssertFormatAsync("""
            void Method()
            {
                if (true) { }
                else
                    while (true) { }
            }
            """, """
            void Method()
            {
                if (true) { }
                else
            while (true) { }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/16328")]
    public Task FormatElseIfOnSameLineWithExtraSpaces()
        => AssertFormatAsync("""
            class A
            {
                void Method()
                {
                    if (true) { }
                    else if (false) { }
                }
            }
            """, """
            class A
            {
                void Method()
                {
                    if (true) { }
                    else     if (false) { }
                }
            }
            """);

    [Fact]
    public Task Format10()
        => AssertFormatAsync("""
            class A
            {
                var a = from i in new[] { 1, 2, 3 }
                        where i > 10
                        select i;
            }
            """, """
            class A
                        {        
                var             a           =           from        i       in          new        [  ]     {           1           ,       2           ,       3       }       
            where           i       >       10          select      i           ;           
            }
            """);

    [Fact]
    public Task ObjectInitializer()
        => AssertFormatAsync("""
            public class C
            {
                public C()
                {
                    C c = new C()
                    {
                        c = new C()
                        {
                            goo = 1,
                            bar = 2
                        }
                    };
                }
            }
            """, """
            public class C
            {
                public C()
                {
                    C c = new C()
                                    {
                                                    c = new C()
                    {
                                        goo = 1,
                            bar = 2
                    }
                                    };
                }
            }
            """);

    [Fact]
    public Task AnonymousType()
        => AssertFormatAsync("""
            class C
            {
                C()
                {
                    var anonType = new
                    {
                        p3 = new
                        {
                            p1 = 3,
                            p2 = null
                        },
                        p4 = true
                    };
                }
            }
            """, """
            class C
            {
                C()
                {
                    var anonType = new
                {
                                p3= new 
                    {
                                        p1 = 3,
                      p2 = null
                                     },
                p4 = true
                    };
                }
            }
            """);

    [Fact]
    public Task MultilineLambda()
        => AssertFormatAsync("""
            class C
            {
                C()
                {
                    System.Func<int, int> ret = x =>
                                {
                                    System.Func<int, int> ret2 = y =>
                                                        {
                                                            y++;
                                                            return y;
                                                        };
                                    return x + 1;
                                };
                }
            }
            """, """
            class C
            {
                C()
                {
                    System.Func<int, int> ret = x =>
                                {
            System.Func<int, int> ret2 = y =>
                                {
                                        y++;
                                        return y;
                };
                                    return x + 1;
                    };
                }
            }
            """);

    [Fact]
    public Task AnonymousMethod()
        => AssertFormatAsync("""
            class C
            {
                C()
                {
                    timer.Tick += delegate (object sender, EventArgs e)
                                    {
                                        MessageBox.Show(this, "Timer ticked");
                                    };
                }
            }
            """, """
            class C
            {
                C()
                {
                    timer.Tick += delegate(object sender, EventArgs e)
                                    {
              MessageBox.Show(this, "Timer ticked");
                                    };
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/10526")]
    public Task LambdaListWithComma()
        => AssertNoFormattingChangesAsync("""
            using System;

            class Test
            {
                void M()
                {
                    Action a = () => { },
                           b = () => { };
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/10526")]
    public Task LambdaListWithCommaMultipleVariables()
        => AssertNoFormattingChangesAsync("""
            using System;

            class Test
            {
                void M()
                {
                    Action a = () => { },
                           b = () => { },
                           c = () => { };
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/10526")]
    public Task AnonymousMethodListWithComma()
        => AssertNoFormattingChangesAsync("""
            using System;

            class Test
            {
                void M()
                {
                    Action a = delegate { },
                           b = delegate { };
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/10526")]
    public Task ExpressionLambdaListWithComma()
        => AssertNoFormattingChangesAsync("""
            using System;

            class Test
            {
                void M()
                {
                    int x = 1, y = 2;
                    Func<int> a = () => x,
                              b = () => y;
                }
            }
            """);

    [Fact]
    public Task Scen1()
        => AssertFormatAsync("""
            namespace Namespace1
            {
                class Program
                {
                    static int i = 1 + 2;

                    static void Main(string[] args)
                    {
                        Program p = new Program();

                        if (i < 5)
                            i = 0;

                        for (i = 0; i < 3; i++)
                            Console.WriteLine(i);

                        while (i < 4)
                            i++;

                        do
                        {
                        } while (i < 4);

                        Method(i, "hello", true);

                    }

                    static void Method(int i, string s, bool b)
                    {
                    }
                }
            }
            """, """
            namespace Namespace1
            {
            class Program
            {
            static int i=1+2;

            static void Main(string[] args)
            {
            Program p=new Program();

            if (i<5)
                                    i=0;
                        
            for (i=0;i<3;i++)
                                    Console.WriteLine(i);

            while (i<4)
                                        i++;

            do{
                                    }while(i<4);

            Method(i,"hello",true);

            }

            static void Method(int i, string s, bool b)
            {
            }
            }
            }
            """);

    [Fact]
    public Task Scen2()
        => AssertFormatAsync("""
            namespace MyNamespace
            {
                class Class1
                {
                }
                enum E
                {
                }
                namespace NestedNamespace
                {
                }
            }

            namespace Namespace1
            {
                class Class1<T>
                {
                    int i;
                    class NestedClass
                    {
                    }
                    T t;
                    T Method<RR>(RR r) where RR : Class1<T>
                    {
                        return default(T);
                    }
                }

                struct S
                {
                    string field1;
                    bool field2;
                    public void Method()
                    {
                    }
                }

                enum E
                {
                    Enum1 = 10,
                    Enum2,
                    Enum3
                }

                class Program
                {
                    static int i = 10;

                    class NestedClass
                    {
                        int field;
                        class NestedClass2
                        {
                            int field;
                            class NestedClass3
                            {
                                enum E
                                {
                                }
                            }
                            int Prop
                            {
                                get { return field; }
                                set { field = value; }
                            }
                            public void Method()
                            {
                            }
                        }
                    }

                    struct S
                    {
                        string field1;
                        bool field2;
                        public void Method()
                        {
                        }
                    }

                    enum E
                    {
                        Enum1 = 10,
                        Enum2,
                        Enum3
                    }

                    public int Prop
                    {
                        get { return i; }
                        set { i = value; }
                    }

                    static void Main()
                    {
                        {
                            Program p = new Program();
                            NestedClass n = new NestedClass();
                        }

                        if (i < 10)
                        {
                            Console.WriteLine(i);
                        }

                        switch (i)
                        {
                            case 1:
                                break;
                            case 2:
                                break;
                            default:
                                break;
                        }

                        for (i = 0; i < 10; i++)
                        {
                            i++;
                        }

                        while (i < 10)
                        {
                            i++;
                        }

                        try
                        {
                            Console.WriteLine();
                        }
                        catch
                        {
                            Console.WriteLine();
                        }
                        finally
                        {
                            Console.WriteLine();
                        }

                    }
                    public void Method<T, R>(T t)
                    {
                        Console.WriteLine(t.ToString());
                    }

                }
            }
            """, """
            namespace MyNamespace
            {
                            class Class1
                            {
                            }
            enum E
            {
            }
                                    namespace NestedNamespace
                                    {
                                    }
            }

            namespace Namespace1
            {
            class Class1<T>
            {
            int i;
            class NestedClass
            {
            }
            T t;
                                            T Method<RR>(RR r) where RR : Class1<T>
                                            {
                                                return default(T);
                                            }
                   }

            struct S
            {
            string field1;
                                        bool field2;
            public void Method()
            {
            }
            }

            enum E
            {
                                        Enum1=10,
            Enum2,
            Enum3
                 }

            class Program
            {
            static int i = 10;

            class NestedClass
            {
                                int field;
            class NestedClass2
            {
            int field;
                            class NestedClass3
            {
                            enum E
                            {
                            }
            }
            int Prop
            {
                                get {return field;}
                                set {field=value;}
            }
            public void Method()
            {
            }
            }
                }

            struct S
            {
                                    string field1;
                        bool field2;
            public void Method()
            {
            }
              }

            enum E
            {
                                Enum1 = 10,
                        Enum2,
            Enum3
               }

            public int Prop
            {
            get {return i;}
            set {i=value;}
                    }

            static void Main()
            {
            {
                            Program p=new Program();
            NestedClass n=new NestedClass();
                        }

            if (i<10)
            {
                                        Console.WriteLine(i);
            }

            switch (i)
            {
                                        case 1:
                            break;
                            case 2:
                                            break;
            default:
            break;
                }

            for (i=0;i<10;i++)
            {
                                        i++;
            }

            while (i<10)
            {
                                        i++;
                }

            try
            {
                                                    Console.WriteLine();
                }
            catch
            {
                                    Console.WriteLine();
                    }
            finally
            {
                                                Console.WriteLine();
                        }

            }
            public void Method<T,R>(T t)
            {
                                        Console.WriteLine(t.ToString());
                        }

            }
            }
            """);

    [Fact]
    public Task Scen3()
        => AssertFormatAsync("""
            namespace Namespace1
            {
                class Program
                {
                    static void Main()
                    {
                        Program p = new Program();
                    }
                }
            }
            """, """
            namespace Namespace1
            {
            class Program
            {
            static void Main()
            {
            Program p=new Program();
            }
            }
            }
            """);

    [Fact]
    public Task Scen4()
        => AssertFormatAsync("""
            class Class1
            {
                //	public void goo()
                //	{
                //		// TODO: Add the implementation for Class1.goo() here.
                //	
                //	}
            }
            """, """
            class Class1
            {
                //	public void goo()
            //	{
            //		// TODO: Add the implementation for Class1.goo() here.
            //	
            //	}
            }
            """);

    [Fact]
    public Task Scen5()
        => AssertFormatAsync("""
            class Class1
            {
                public void Method()
                {
                    {
                        int i = 0;
                        System.Console.WriteLine();
                    }
                }
            }
            """, """
            class Class1
            {
            public void Method()
            {
            {
            int i = 0;
                                System.Console.WriteLine();
            }
            }
            }
            """);

    [Fact]
    public Task Scen6()
        => AssertFormatAsync("""
            namespace Namespace1
            {
                class OuterClass
                {
                    class InnerClass
                    {
                    }
                }
            }
            """, """
            namespace Namespace1
            {
            class OuterClass
            {
            class InnerClass
            {
            }
            }
            }
            """);

    [Fact]
    public Task Scen7()
        => AssertFormatAsync("""
            class Class1
            {
                public void Method()
                {
                    int i = 0;
                    switch (i)
                    {
                        case 0:
                            break;
                    }
                    if (i > 0) goto z;
                    i = -i;
                z:
                    i = 2 * i;
                }
            }
            """, """
            class Class1
            {
            public void Method()
            {
            int i = 0;
            switch (i)
            {
            case 0:
            break;
            }
            if (i > 0) goto z;
            i = -i;
            z:
            i = 2 * i;
            }
            }
            """);

    [Fact]
    public Task Scen8()
        => AssertFormatAsync("""
            class Class1
            {
                public void Method()
                {
                    int i = 10;
                }
            }
            """, """
            class Class1
                  {
                            public void Method()
                    {
                                int i = 10;
                   }
            }
            """);

    [Fact]
    public Task IndentStatementsInMethod()
        => AssertFormatAsync("""
            class C
            {
                void Goo()
                {
                    int x = 0;
                    int y = 0;
                    int z = 0;
                }
            }
            """, """
            class C
            {
                void Goo()
                {
                    int x = 0;
                        int y = 0;
                  int z = 0;
                }
            }
            """);

    [Fact]
    public Task IndentFieldsInClass()
        => AssertFormatAsync("""
            class C
            {
                int a = 10;
                int b;
                int c;
            }
            """, """
            class C
            {
                    int a = 10;
                  int b;
              int c;
            }
            """);

    [Fact]
    public Task IndentUserDefaultSettingTest()
        => AssertFormatAsync("""
            class Class2
            {
                public void nothing()
                {
                    nothing_again(() =>
                        {
                            Console.WriteLine("Nothing");
                        });
                label1:
                    int f = 5;
                label2:
                    switch (f)
                    {
                        case 1:
                            {
                                break;
                            }
                        case 2:
                            int d = f + f;
                        label3:
                            d = d - f;
                            break;
                        default:
                            {
                                int g = f * f;
                                g = g - f;
                                break;
                            }
                    }
                    return;
                }

                public void nothing_again(Action a)
                {
                l:
                    goto l;
                }
            }
            """, """
            class Class2
                {
                public void nothing()
                    {
                nothing_again(() =>
                    {
                    Console.WriteLine("Nothing");
                    });
            label1:
                int f = 5;
            label2:
                switch (f)
                    {
                case 1:
                    {
                break;
                    }
                case 2:
                int d = f + f;
            label3:
                d = d - f;
                break;
                default:
                    {
                int g = f * f;
                g = g - f;
                break;
                    }
                    }
                return;
                    }

                public void nothing_again(Action a)
                    {
                    l:
                        goto l;
                    }
                }
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/766133")]
    public async Task RelativeIndentationToFirstTokenInBaseTokenWithObjectInitializers()
    {
        var changingOptions = new OptionsCollection(LanguageNames.CSharp)
        {
            { NewLineBeforeOpenBrace, NewLineBeforeOpenBrace.DefaultValue.WithFlagValue(NewLineBeforeOpenBracePlacement.ObjectCollectionArrayInitializers, false) },
        };
        await AssertFormatAsync("""
            class Program
            {
                static void Main(string[] args)
                {
                    var summa = new D {
                        A = 0,
                        B = 4
                    };
                }
            }

            class D
            {
                public int A { get; set; }
                public int B { get; set; }
            }
            """, """
            class Program
            {
                static void Main(string[] args)
                {
                    var summa = new D
                    {
                        A = 0,
                        B = 4
                    };
                }
            }

            class D
            {
                public int A { get; set; }
                public int B { get; set; }
            }
            """, changingOptions);
    }

    [Fact]
    public async Task RemoveSpacingAroundBinaryOperatorsShouldMakeAtLeastOneSpaceForIsAndAsKeywords()
    {
        var changingOptions = new OptionsCollection(LanguageNames.CSharp)
        {
            { CSharpFormattingOptions2.SpacingAroundBinaryOperator, BinaryOperatorSpacingOptions.Remove }
        };
        await AssertFormatAsync("""
            class Class2
            {
                public void nothing()
                {
                    var a = 1*2+3-4/5;
                    a+=1;
                    object o = null;
                    string s = o as string;
                    bool b = o is string;
                }
            }
            """, """
            class Class2
                {
                public void nothing()
                    {
                        var a = 1   *   2  +   3   -  4  /  5;
                        a    += 1;
                        object o = null;
                        string s = o        as       string;
                        bool b   = o        is       string;
                    }
                }
            """, changingOptions);
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/772298")]
    public async Task IndentUserSettingNonDefaultTest_OpenBracesOfLambdaWithNoNewLine()
    {
        var changingOptions = new OptionsCollection(LanguageNames.CSharp)
        {
            { IndentBraces, true },
            { IndentBlock, false },
            { IndentSwitchSection, false },
            { IndentSwitchCaseSection, false },
            { NewLineBeforeOpenBrace, NewLineBeforeOpenBrace.DefaultValue.WithFlagValue(NewLineBeforeOpenBracePlacement.LambdaExpressionBody, false) },
            { LabelPositioning, LabelPositionOptions.LeftMost }
        };

        await AssertFormatAsync("""
            class Class2
                {
                public void nothing()
                    {
                nothing_again(() => {
                Console.WriteLine("Nothing");
                });
                    }
                }
            """, """
            class Class2
            {
                public void nothing()
                {
                    nothing_again(() =>
                        {
                            Console.WriteLine("Nothing");
                        });
                }
            }
            """, changedOptionSet: changingOptions);
    }

    [Fact]
    public async Task IndentUserSettingNonDefaultTest()
    {
        var changingOptions = new OptionsCollection(LanguageNames.CSharp)
        {
            { IndentBraces, true },
            { IndentBlock, false },
            { IndentSwitchSection, false },
            { IndentSwitchCaseSection, false },
            { IndentSwitchCaseSectionWhenBlock, false },
            { LabelPositioning, LabelPositionOptions.LeftMost }
        };

        await AssertFormatAsync("""
            class Class2
                {
                public void nothing()
                    {
                nothing_again(() =>
                    {
                    Console.WriteLine("Nothing");
                    });
            label1:
                int f = 5;
            label2:
                switch (f)
                    {
                case 1:
                    {
                break;
                    }
                case 2:
                int d = f + f;
            label3:
                d = d - f;
                break;
                default:
                    {
                int g = f * f;
                g = g - f;
                break;
                    }
                    }
                return;
                    }

                public void nothing_again(Action a)
                    {
            l:
                goto l;
                    }
                }
            """, """
            class Class2
            {
                public void nothing()
                {
                    nothing_again(() =>
                        {
                            Console.WriteLine("Nothing");
                        });
                label1:
                    int f = 5;
                label2:
                    switch (f)
                    {
                        case 1:
                            {
                                break;
                            }
                        case 2:
                            int d = f + f;
                        label3:
                            d = d - f;
                            break;
                        default:
                            {
                                int g = f * f;
                                g = g - f;
                                break;
                            }
                    }
                    return;
                }

                public void nothing_again(Action a)
                {
                l:
                    goto l;
                }
            }
            """, changedOptionSet: changingOptions);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/20009")]
    public async Task IndentSwitch_IndentCase_IndentWhenBlock()
    {
        var changingOptions = new OptionsCollection(LanguageNames.CSharp)
        {
            { IndentSwitchSection, true },
            { IndentSwitchCaseSection, true },
            { IndentSwitchCaseSectionWhenBlock, true },
        };

        await AssertFormatAsync(
            """
            class Class2
            {
                void M()
                {
                    switch (i)
                    {
                        case 0:
                            {
                            }
                        case 1:
                            break;
                    }
                }
            }
            """,
            """
            class Class2
            {
                void M()
                {
                        switch (i) {
                    case 0: {
                }
                    case 1:
                break;
                        }
                }
            }
            """, changedOptionSet: changingOptions);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/20009")]
    public async Task IndentSwitch_IndentCase_NoIndentWhenBlock()
    {
        var changingOptions = new OptionsCollection(LanguageNames.CSharp)
        {
            { IndentSwitchSection, true },
            { IndentSwitchCaseSection, true },
            { IndentSwitchCaseSectionWhenBlock, false },
        };

        await AssertFormatAsync(
            """
            class Class2
            {
                void M()
                {
                    switch (i)
                    {
                        case 0:
                        {
                        }
                        case 1:
                            break;
                    }
                }
            }
            """,
            """
            class Class2
            {
                void M()
                {
                        switch (i) {
                    case 0: {
                }
                    case 1:
                break;
                        }
                }
            }
            """, changedOptionSet: changingOptions);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/20009")]
    public async Task IndentSwitch_NoIndentCase_IndentWhenBlock()
    {
        var changingOptions = new OptionsCollection(LanguageNames.CSharp)
        {
            { IndentSwitchSection, true },
            { IndentSwitchCaseSection, false },
            { IndentSwitchCaseSectionWhenBlock, true },
        };

        await AssertFormatAsync(
            """
            class Class2
            {
                void M()
                {
                    switch (i)
                    {
                        case 0:
                            {
                            }
                        case 1:
                        break;
                    }
                }
            }
            """,
            """
            class Class2
            {
                void M()
                {
                        switch (i) {
                    case 0: {
                }
                    case 1:
                break;
                        }
                }
            }
            """, changedOptionSet: changingOptions);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/20009")]
    public async Task IndentSwitch_NoIndentCase_NoIndentWhenBlock()
    {
        var changingOptions = new OptionsCollection(LanguageNames.CSharp)
        {
            { IndentSwitchSection, true },
            { IndentSwitchCaseSection, false },
            { IndentSwitchCaseSectionWhenBlock, false },
        };

        await AssertFormatAsync(
            """
            class Class2
            {
                void M()
                {
                    switch (i)
                    {
                        case 0:
                        {
                        }
                        case 1:
                        break;
                    }
                }
            }
            """,
            """
            class Class2
            {
                void M()
                {
                        switch (i) {
                    case 0: {
                }
                    case 1:
                break;
                        }
                }
            }
            """, changedOptionSet: changingOptions);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/20009")]
    public async Task NoIndentSwitch_IndentCase_IndentWhenBlock()
    {
        var changingOptions = new OptionsCollection(LanguageNames.CSharp)
        {
            { IndentSwitchSection, false },
            { IndentSwitchCaseSection, true },
            { IndentSwitchCaseSectionWhenBlock, true },
        };

        await AssertFormatAsync(
            """
            class Class2
            {
                void M()
                {
                    switch (i)
                    {
                    case 0:
                        {
                        }
                    case 1:
                        break;
                    }
                }
            }
            """,
            """
            class Class2
            {
                void M()
                {
                        switch (i) {
                    case 0: {
                }
                    case 1:
                break;
                        }
                }
            }
            """, changedOptionSet: changingOptions);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/20009")]
    public async Task NoIndentSwitch_IndentCase_NoIndentWhenBlock()
    {
        var changingOptions = new OptionsCollection(LanguageNames.CSharp)
        {
            { IndentSwitchSection, false },
            { IndentSwitchCaseSection, true },
            { IndentSwitchCaseSectionWhenBlock, false },
        };

        await AssertFormatAsync(
            """
            class Class2
            {
                void M()
                {
                    switch (i)
                    {
                    case 0:
                    {
                    }
                    case 1:
                        break;
                    }
                }
            }
            """,
            """
            class Class2
            {
                void M()
                {
                        switch (i) {
                    case 0: {
                }
                    case 1:
                break;
                        }
                }
            }
            """, changedOptionSet: changingOptions);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/20009")]
    public async Task NoIndentSwitch_NoIndentCase_IndentWhenBlock()
    {
        var changingOptions = new OptionsCollection(LanguageNames.CSharp)
        {
            { IndentSwitchSection, false },
            { IndentSwitchCaseSection, false },
            { IndentSwitchCaseSectionWhenBlock, true },
        };

        await AssertFormatAsync(
            """
            class Class2
            {
                void M()
                {
                    switch (i)
                    {
                    case 0:
                        {
                        }
                    case 1:
                    break;
                    }
                }
            }
            """,
            """
            class Class2
            {
                void M()
                {
                        switch (i) {
                    case 0: {
                }
                    case 1:
                break;
                        }
                }
            }
            """, changedOptionSet: changingOptions);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/20009")]
    public async Task NoIndentSwitch_NoIndentCase_NoIndentWhenBlock()
    {
        var changingOptions = new OptionsCollection(LanguageNames.CSharp)
        {
            { IndentSwitchSection, false },
            { IndentSwitchCaseSection, false },
            { IndentSwitchCaseSectionWhenBlock, false },
        };

        await AssertFormatAsync(
            """
            class Class2
            {
                void M()
                {
                    switch (i)
                    {
                    case 0:
                    {
                    }
                    case 1:
                    break;
                    }
                }
            }
            """,
            """
            class Class2
            {
                void M()
                {
                        switch (i) {
                    case 0: {
                }
                    case 1:
                break;
                        }
                }
            }
            """, changedOptionSet: changingOptions);
    }

    [Fact]
    public Task TestWrappingDefault()
        => AssertFormatAsync("""
            class Class5
            {
                delegate void Del(int x);
                public int Age { get { int age = 0; return age; } }
                public int Age2
                {
                    get { int age2 = 0; return age2; }
                    set { int age2 = value; }
                }
                void bar()
                {
                    int x = 0;
                    if (x == 1) x = 2; else x = 3;
                    do { x = 4; } while (x != 4);
                    switch (x) { case 1: break; case 2: break; default: break; }
                    Del d = delegate (int k) { Console.WriteLine(); Console.WriteLine(); };
                }
            }
            """, """
            class Class5
                {
                    delegate void Del(int x);
                    public int Age { get { int age = 0; return age; } }
                    public int Age2
                    {
                        get { int age2 = 0; return age2; }
                        set { int age2 = value; }
                    }
                    void bar()
                    {
                        int x = 0;
                        if(x == 1) x = 2; else x =3;
                        do { x = 4; } while (x != 4);
                        switch (x) { case 1: break; case 2: break; default: break; }
                        Del d = delegate(int k) { Console.WriteLine(); Console.WriteLine(); };
                    }
                }
            """);

    [Fact]
    public async Task TestWrappingNonDefault_FormatBlock()
    {
        var changingOptions = new OptionsCollection(LanguageNames.CSharp)
        {
            { CSharpFormattingOptions2.WrappingPreserveSingleLine, false }
        };
        await AssertFormatAsync("""
            class Class5
            {
                delegate void Del(int x);
                public int Age
                {
                    get
                    {
                        int age = 0; return age;
                    }
                }
                public int Age2
                {
                    get
                    {
                        int age2 = 0; return age2;
                    }
                    set
                    {
                        int age2 = value;
                    }
                }
                void bar()
                {
                    int x = 0;
                    if (x == 1) x = 2; else x = 3;
                    do { x = 4; } while (x != 4);
                    switch (x)
                    {
                        case 1: break;
                        case 2: break;
                        default: break;
                    }
                    Del d = delegate (int k) { Console.WriteLine(); Console.WriteLine(); };
                }
                void goo()
                {
                    int xx = 0; int zz = 0;
                }
            }
            class goo
            {
                int x = 0;
            }
            """, """
            class Class5
            {
                delegate void Del(int x);
                    public int Age { get { int age = 0; return age; } }
                    public int Age2
                    {
                        get { int age2 = 0; return age2; }
                        set { int age2 = value; }
                    }
                    void bar()
                    {
                        int x = 0;
                        if(x == 1) x = 2; else x =3;
                        do { x = 4; } while (x != 4);
                        switch (x) { case 1: break; case 2: break; default: break; }
                        Del d = delegate(int k) { Console.WriteLine(); Console.WriteLine(); };
                    }
                    void goo() { int xx = 0; int zz = 0;}
            }
            class goo{int x = 0;}
            """, changingOptions);
    }

    [Fact]
    public async Task TestWrappingNonDefault_FormatStatmtMethDecl()
    {
        var changingOptions = new OptionsCollection(LanguageNames.CSharp)
        {
            { CSharpFormattingOptions2.WrappingKeepStatementsOnSingleLine, false }
        };
        await AssertFormatAsync("""
            class Class5
            {
                delegate void Del(int x);
                public int Age { get { int age = 0; return age; } }
                public int Age2
                {
                    get { int age2 = 0; return age2; }
                    set { int age2 = value; }
                }
                void bar()
                {
                    int x = 0;
                    if (x == 1)
                        x = 2;
                    else
                        x = 3;
                    do
                    { x = 4; } while (x != 4);
                    switch (x)
                    {
                        case 1:
                            break;
                        case 2:
                            break;
                        default:
                            break;
                    }
                    Del d = delegate (int k)
                    { Console.WriteLine(); Console.WriteLine(); };
                }
                void goo() { int y = 0; int z = 0; }
            }
            class goo
            {
                int x = 0;
            }
            """, """
            class Class5
            {
                delegate void Del(int x);
                    public int Age { get { int age = 0; return age; } }
                    public int Age2
                    {
                        get { int age2 = 0; return age2; }
                        set { int age2 = value; }
                    }
                    void bar()
                    {
                        int x = 0;
                        if(x == 1) x = 2; else x =3;
                        do { x = 4; } while (x != 4);
                        switch (x) { case 1: break; case 2: break; default: break; }
                        Del d = delegate(int k) { Console.WriteLine(); Console.WriteLine(); };
                    }
                    void goo(){int y=0; int z =0 ;}
            }
            class goo
            {
                int x = 0;
            }
            """, changingOptions);
    }

    [Fact]
    public async Task TestWrappingNonDefault()
    {
        var changingOptions = new OptionsCollection(LanguageNames.CSharp)
        {
            { CSharpFormattingOptions2.WrappingPreserveSingleLine, false },
            { CSharpFormattingOptions2.WrappingKeepStatementsOnSingleLine, false }
        };
        await AssertFormatAsync("""
            class Class5
            {
                delegate void Del(int x);
                public int Age
                {
                    get
                    {
                        int age = 0;
                        return age;
                    }
                }
                public int Age2
                {
                    get
                    {
                        int age2 = 0;
                        return age2;
                    }
                    set
                    {
                        int age2 = value;
                    }
                }
                void bar()
                {
                    int x = 0;
                    if (x == 1)
                        x = 2;
                    else
                        x = 3;
                    do
                    {
                        x = 4;
                    } while (x != 4);
                    switch (x)
                    {
                        case 1:
                            break;
                        case 2:
                            break;
                        default:
                            break;
                    }
                    Del d = delegate (int k)
                    {
                        Console.WriteLine();
                        Console.WriteLine();
                    };
                }
            }
            class goo
            {
                int x = 0;
            }
            """, """
            class Class5
            {
                delegate void Del(int x);
                    public int Age { get { int age = 0; return age; } }
                    public int Age2
                    {
                        get { int age2 = 0; return age2; }
                        set { int age2 = value; }
                    }
                    void bar()
                    {
                        int x = 0;
                        if(x == 1) x = 2; else x =3;
                        do { x = 4; } while (x != 4);
                        switch (x) { case 1: break; case 2: break; default: break; }
                        Del d = delegate(int k) { Console.WriteLine(); Console.WriteLine(); };
                    }
            }
            class goo{int x = 0;}
            """, changingOptions);
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/991480")]
    public async Task TestLeaveStatementMethodDeclarationSameLineNotAffectingForStatement()
    {
        var changingOptions = new OptionsCollection(LanguageNames.CSharp)
        {
            { CSharpFormattingOptions2.WrappingKeepStatementsOnSingleLine, false }
        };
        await AssertFormatAsync("""
            class Program
            {
                static void Main(string[] args)
                {
                    for (int d = 0; d < 10; ++d)
                    { }
                }
            }
            """, """
            class Program
            {
                static void Main(string[] args)
                {
                    for (int d = 0; d < 10; ++d) { }
                }
            }
            """, changingOptions);
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/751789")]
    public Task NewLineForOpenBracesDefault()
        => AssertFormatAsync("""
            class f00
            {
                void br()
                {
                    Func<int, int> ret = x =>
                             {
                                 return x + 1;
                             };
                    var obj = new
                    {
                        // ...
                    };
                    if (true)
                    {
                        System.Console.WriteLine("");
                    }
                    else
                    {
                    }
                    timer.Tick += delegate (object sender, EventArgs e)


            {
                MessageBox.Show(this, "Timer ticked");
            };

                    var obj1 = new goo
                    {
                    };

                    async void LocalFunction()
                    {
                    }

                    try
                    {
                    }
                    catch (Exception e)
                    {
                    }
                    finally
                    { }

                    using (someVar)
                    {
                    }

                    switch (switchVar)
                    {
                        default:
                            break;
                    }
                }
            }

            namespace NS1
            {
                public class goo : System.Object



                {
                    public int f { get; set; }
                }
            }
            """, """
            class f00
            {
                    void br() { 
            Func<int, int> ret = x =>
                     {
                             return x + 1;
                         };
            var obj = new
             {
                    // ...
                };
            if(true) 
            {
                        System.Console.WriteLine("");
                    }
            else 
            {
            }
                    timer.Tick += delegate (object sender, EventArgs e)     


            {
                        MessageBox.Show(this, "Timer ticked");
                    };

            var obj1 = new goo         
                        {
                                        };

                    async void LocalFunction() {
                    }

                       try
                    {
                    }
                    catch (Exception e) 
                    {
                    }
                    finally 
                    {}

                    using (someVar) 
                    {
                    }

                    switch (switchVar)
             {
                        default: 
                            break;
                    }
            }
            }

            namespace NS1 {
            public class goo : System.Object



            {
                public int f { get; set; }
            }
            }
            """);

    [Fact, WorkItem("https://developercommunity.visualstudio.com/content/problem/8808/c-structure-guide-lines-for-unsafe-fixed.html")]
    [WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/751789")]
    public async Task NewLineForOpenBracesNonDefault()
    {
        var changingOptions = new OptionsCollection(LanguageNames.CSharp)
        {
            { NewLineBeforeOpenBrace, NewLineBeforeOpenBracePlacement.None }
        };
        await AssertFormatAsync("""
            class f00 {
                void br() {
                    Func<int, int> ret = x => {
                        return x + 1;
                    };
                    var obj = new {
                        // ...
                    };
                    if (true) {
                        System.Console.WriteLine("");
                    }
                    else {
                    }
                    timer.Tick += delegate (object sender, EventArgs e) {
                        MessageBox.Show(this, "Timer ticked");
                    };

                    var obj1 = new goo {
                    };

                    async void LocalFunction() {
                    }

                    try {
                    }
                    catch (Exception e) {
                    }
                    finally { }

                    using (someVar) {
                    }

                    switch (switchVar) {
                        default:
                            break;
                    }

                    unsafe {
                    }

                    fixed (int* p = &i) {
                    }
                }
            }

            namespace NS1 {
                public class goo : System.Object {
                }
            }
            """, """
            class f00
            {
                    void br() { 
            Func<int, int> ret = x =>
            {
                    return x + 1;
                };
            var obj = new
             {
                    // ...
                };
            if(true) 
            {
                        System.Console.WriteLine("");
                    }
            else 
            {
            }
                    timer.Tick += delegate (object sender, EventArgs e)     


            {
                        MessageBox.Show(this, "Timer ticked");
                    };

            var obj1 = new goo         
                        {
                                        };

                    async void LocalFunction() 
                        {
                }

                       try
                    {
                    }
                    catch (Exception e) 
                    {
                    }
                    finally 
                    {}

                    using (someVar) 
                    {
                    }

                    switch (switchVar)
             {
                        default: 
                            break;
                    }

                    unsafe
            {
                    }

                    fixed (int* p = &i)
            {
                    }
            }
            }

            namespace NS1 {
            public class goo : System.Object



            {
            }
            }
            """, changingOptions);
    }

    [Fact]
    public Task NewLineForKeywordDefault()
        => AssertFormatAsync("""
            class c
            {
                void f00()
                {

                    try
                    {
                        // ...
                    }
                    catch (Exception e)
                    {
                        // ...
                    }
                    finally
                    {
                        // ...
                    }

                    if (a > b)
                    {
                        return 3;
                    }
                    else
                    {
                        return 0;
                    }
                }
            }
            """,
            """
            class c
            {
            void f00(){

            try
            {
                // ...
            } catch (Exception e)
            {
                // ...
            } finally
            {
                // ...
            }

            if (a > b)
            {
                return 3;
            } else
            {
                return 0;
            }
            }
            }
            """);

    [Fact]
    public async Task NewLineForKeywordNonDefault()
    {
        var changingOptions = new OptionsCollection(LanguageNames.CSharp)
        {
            { CSharpFormattingOptions2.NewLineForElse, false },
            { CSharpFormattingOptions2.NewLineForCatch, false },
            { CSharpFormattingOptions2.NewLineForFinally, false }
        };
        await AssertFormatAsync("""
            class c
            {
                void f00()
                {

                    try
                    {
                        // ...
                    } catch (Exception e)
                    {
                        // ...
                    } finally
                    {
                        // ...
                    }
                    if (a > b)
                    {
                        return 3;
                    } else
                    {
                        return 0;
                    }
                }
            }
            """, """
            class c
            {
            void f00(){

            try
            {
                // ...
            }


            catch (Exception e)
            {
                // ...
            }


                        finally
            {
                // ...
            }
            if (a > b)
            {
                return 3;
            }

            else
            {
                return 0;
            }
            }
            }
            """, changingOptions);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/33458")]
    public async Task NoNewLineForElseChecksBraceOwner()
    {
        var changingOptions = new OptionsCollection(LanguageNames.CSharp)
        {
            { NewLineForElse, false },
            { NewLineBeforeOpenBrace, NewLineBeforeOpenBrace.DefaultValue.WithFlagValue(NewLineBeforeOpenBracePlacement.ControlBlocks, false) }
        };

        await AssertFormatAsync("""
            class Class
            {
                void Method()
                {
                    if (true)
                        for (int i = 0; i < 10; i++) {
                            Method();
                        }
                    else
                        return;
                }
            }
            """, """
            class Class
            {
                void Method()
                {
                    if (true)
                        for (int i = 0; i < 10; i++) {
                            Method();
                        } else
                        return;
                }
            }
            """, changedOptionSet: changingOptions);
    }

    [Fact]
    public Task NewLineForExpressionDefault()
        => AssertFormatAsync("""
            class f00
            {
                void br()
                {
                    var queryLowNums = from num in numbers
                                       where num < 5
                                       select num;

                    var queryLowNums =

                                from num in numbers
                                where num < 5
                                select num;

                    var q = from c in cust
                            from o in c.Orders
                            orderby o.Total descending
                            select new { c.Name, c.OrderID };
                    var obj = new
                    {
                        X1 = 0,
                        Y1 = 1,
                        X2 = 2,
                        Y2 = 3
                    };
                    var obj1 = new { X1 = 0, Y1 = 1, X2 = 2, Y2 = 3 };
                    MyObject obj = new MyObject
                    {
                        X1 = 0,
                        Y1 = 1,
                        X2 = 2,
                        Y2 = 3
                    };
                    MyObject obj = new MyObject { X1 = 0, Y1 = 1, X2 = 2, Y2 = 3 };
                }
            }
            """, """
            class f00
            {
                void br()
                {
            var queryLowNums =           from num in numbers            where num < 5
                        select num;

            var queryLowNums =      

                        from num in numbers            where num < 5
                        select num;

                              var q =  from c in cust
            from o in c.Orders orderby o.Total descending
                    select new { c.Name, c.OrderID };
            var obj = new {         X1 = 0,         Y1 = 1,
                    X2 = 2,
                    Y2 = 3
                };
            var obj1 = new {        X1 = 0,        Y1 = 1,        X2 = 2,        Y2 = 3    };
                    MyObject obj = new MyObject {       X1 = 0,        Y1 = 1,
                    X2 = 2,
                    Y2 = 3
                };
            MyObject obj = new MyObject {       X1 = 0,        Y1 = 1, X2 = 2,       Y2 = 3     };
                }
            }
            """);

    [Fact]
    public async Task NewLineForExpressionNonDefault()
    {
        var changingOptions = new OptionsCollection(LanguageNames.CSharp)
        {
            { CSharpFormattingOptions2.NewLineForMembersInObjectInit, false },
            { CSharpFormattingOptions2.NewLineForMembersInAnonymousTypes, false },
            { CSharpFormattingOptions2.NewLineForClausesInQuery, false }
        };
        await AssertFormatAsync("""
            class f00
            {
                void br()
                {

                    var queryLowNums = from num in numbers where num < 5
                                       select num;

                    var queryLowNums =

                                from num in numbers where num < 5
                                select num;

                    var q = from c in cust
                            from o in c.Orders orderby o.Total descending
                            select new { c.Name, c.OrderID };
                    var obj = new
                    {
                        X1 = 0, Y1 = 1,
                        X2 = 2,
                        Y2 = 3
                    };
                    MyObject obj = new MyObject
                    {
                        X1 = 0, Y1 = 1,
                        X2 = 2,
                        Y2 = 3
                    };
                }
            }
            """, """
            class f00
            {
                void br()
                {

            var queryLowNums =           from num in numbers            where num < 5
                        select num;

            var queryLowNums =      

                        from num in numbers            where num < 5
                        select num;

                              var q =  from c in cust
            from o in c.Orders orderby o.Total descending
                    select new { c.Name, c.OrderID };
            var obj = new {   X1 = 0,         Y1 = 1,
                    X2 = 2,
                    Y2 = 3
                };
                    MyObject obj = new MyObject {       X1 = 0,        Y1 = 1,
                    X2 = 2,
                              Y2 = 3
                };
                }
            }
            """, changingOptions);
    }

    [Fact]
    public Task Enums_Bug2586()
        => AssertFormatAsync("""
            enum E
            {
                a = 10,
                b,
                c
            }
            """, """
            enum E
            {
                    a = 10,
                  b,
              c
            }
            """);

    [Fact]
    public async Task DoNotInsertLineBreaksInSingleLineEnum()
        => await AssertFormatAsync(@"enum E { a = 10, b, c }", @"enum E { a = 10, b, c }");

    [Fact]
    public Task AlreadyFormattedSwitchIsNotFormatted_Bug2588()
        => AssertFormatAsync("""
            class C
            {
                void M()
                {
                    switch (3)
                    {
                        case 0:
                            break;
                    }
                }
            }
            """, """
            class C
            {
                void M()
                {
                    switch (3)
                    {
                        case 0:
                            break;
                    }
                }
            }
            """);

    [Fact]
    public Task BreaksAreAlignedInSwitchCasesFormatted_Bug2587()
        => AssertFormatAsync("""
            class C
            {
                void M()
                {
                    switch (3)
                    {
                        case 0:
                            break;
                    }
                }
            }
            """, """
            class C
            {
                void M()
                {
                    switch (3)
                    {
                        case 0:
                                break;
                    }
                }
            }
            """);

    [Fact]
    public Task BreaksAndBracesAreAlignedInSwitchCasesWithBracesFormatted_Bug2587()
        => AssertFormatAsync("""
            class C
            {
                void M()
                {
                    switch (3)
                    {
                        case 0:
                            {
                                break;
                            }
                    }
                }
            }
            """, """
            class C
            {
                void M()
                {
                    switch (3)
                    {
                        case 0:
                        {
                                    break;
                            }
                    }
                }
            }
            """);

    [Fact]
    public Task LineBreaksAreNotInsertedForSwitchCasesOnASingleLine1()
        => AssertFormatAsync("""
            class C
            {
                void M()
                {
                    switch (3)
                    {
                        case 0: break;
                        default: break;
                    }
                }
            }
            """, """
            class C
            {
                void M()
                {
                    switch (3)
                    {
                        case 0: break;
                        default: break;
                    }
                }
            }
            """);

    [Fact]
    public Task LineBreaksAreNotInsertedForSwitchCasesOnASingleLine2()
        => AssertFormatAsync("""
            class C
            {
                void M()
                {
                    switch (3)
                    {
                        case 0: { break; }
                        default: { break; }
                    }
                }
            }
            """, """
            class C
            {
                void M()
                {
                    switch (3)
                    {
                        case 0: { break; }
                        default: { break; }
                    }
                }
            }
            """);

    [Fact]
    public Task FormatLabelAndGoto1_Bug2588()
        => AssertFormatAsync("""
            class C
            {
                void M()
                {
                Goo:
                    goto Goo;
                }
            }
            """, """
            class C
            {
                void M()
                {
            Goo:
            goto Goo;
                }
            }
            """);

    [Fact]
    public Task FormatLabelAndGoto2_Bug2588()
        => AssertFormatAsync("""
            class C
            {
                void M()
                {
                    int x = 0;
                Goo:
                    goto Goo;
                }
            }
            """, """
            class C
            {
                void M()
                {
            int x = 0;
            Goo:
            goto Goo;
                }
            }
            """);

    [Fact]
    public Task FormatNestedLabelAndGoto1_Bug2588()
        => AssertFormatAsync("""
            class C
            {
                void M()
                {
                    if (true)
                    {
                    Goo:
                        goto Goo;
                    }
                }
            }
            """, """
            class C
            {
                void M()
                {
            if (true)
            {
            Goo:
            goto Goo;
            }
                }
            }
            """);

    [Fact]
    public Task FormatNestedLabelAndGoto2_Bug2588()
        => AssertFormatAsync("""
            class C
            {
                void M()
                {
                    if (true)
                    {
                        int x = 0;
                    Goo:
                        goto Goo;
                    }
                }
            }
            """, """
            class C
            {
                void M()
                {
            if (true)
            {
            int x = 0;
            Goo:
            goto Goo;
            }
                }
            }
            """);

    [Fact]
    public Task AlreadyFormattedGotoLabelIsNotFormatted1_Bug2588()
        => AssertFormatAsync("""
            class C
            {
                void M()
                {
                Goo:
                    goto Goo;
                }
            }
            """, """
            class C
            {
                void M()
                {
                Goo:
                    goto Goo;
                }
            }
            """);

    [Fact]
    public Task AlreadyFormattedGotoLabelIsNotFormatted2_Bug2588()
        => AssertFormatAsync("""
            class C
            {
                void M()
                {
                Goo: goto Goo;
                }
            }
            """, """
            class C
            {
                void M()
                {
                Goo: goto Goo;
                }
            }
            """);

    [Fact]
    public Task AlreadyFormattedGotoLabelIsNotFormatted3_Bug2588()
        => AssertFormatAsync("""
            class C
            {
                void M()
                {
                    int x = 0;
                Goo:
                    goto Goo;
                }
            }
            """, """
            class C
            {
                void M()
                {
                    int x = 0;
                Goo:
                    goto Goo;
                }
            }
            """);

    [Fact]
    public Task DoNotAddLineBreakBeforeWhere1_Bug2582()
        => AssertFormatAsync("""
            class C
            {
                void M<T>() where T : I
                {
                }
            }
            """, """
            class C
            {
                void M<T>() where T : I
                {
                }
            }
            """);

    [Fact]
    public Task DoNotAddLineBreakBeforeWhere2_Bug2582()
        => AssertFormatAsync("""
            class C<T> where T : I
            {
            }
            """, """
            class C<T> where T : I
            {
            }
            """);

    [Fact]
    public Task DoNotAddSpaceAfterUnaryMinus()
        => AssertFormatAsync("""
            class C
            {
                void M()
                {
                    int x = -1;
                }
            }
            """, """
            class C
            {
                void M()
                {
                    int x = -1;
                }
            }
            """);

    [Fact]
    public Task DoNotAddSpaceAfterUnaryPlus()
        => AssertFormatAsync("""
            class C
            {
                void M()
                {
                    int x = +1;
                }
            }
            """, """
            class C
            {
                void M()
                {
                    int x = +1;
                }
            }
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545909")]
    public async Task DoNotAddSpaceAfterIncrement()
    {
        var code = """
            class C
            {
                void M(int[] i)
                {
                    ++i[0];
                }
            }
            """;
        await AssertFormatAsync(code, code);
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545909")]
    public async Task DoNotAddSpaceBeforeIncrement()
    {
        var code = """
            class C
            {
                void M(int[] i)
                {
                    i[0]++;
                }
            }
            """;
        await AssertFormatAsync(code, code);
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545909")]
    public async Task DoNotAddSpaceAfterDecrement()
    {
        var code = """
            class C
            {
                void M(int[] i)
                {
                    --i[0];
                }
            }
            """;
        await AssertFormatAsync(code, code);
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545909")]
    public async Task DoNotAddSpaceBeforeDecrement()
    {
        var code = """
            class C
            {
                void M(int[] i)
                {
                    i[0]--;
                }
            }
            """;
        await AssertFormatAsync(code, code);
    }

    [Fact]
    public Task Anchoring()
        => AssertFormatAsync("""
            class C
            {
                void M()
                {
                    Console.WriteLine("Goo",
                        0, 1,
                            2);
                }
            }
            """, """
            class C
            {
                void M()
                {
                                Console.WriteLine("Goo",
                                    0, 1,
                                        2);
                }
            }
            """);

    [Fact]
    public Task Exclamation()
        => AssertFormatAsync("""
            class C
            {
                void M()
                {
                    if (!true) ;
                }
            }
            """, """
            class C
            {
                void M()
                {
                    if (    !           true        )           ;
                }
            }
            """);

    [Fact]
    public Task StartAndEndTrivia()
        => AssertFormatAsync("""



            class C { }





            """, """
                  
                    
                    
            class C { }     
                    
                    
                    
                                

            """);

    [Fact]
    public Task FirstTriviaAndAnchoring1()
        => AssertFormatAsync("""

            namespace N
            {
                class C
                {
                    void Method()
                    {
                        int i =
                            1
                                +
                                    3;
                    }
                }
            }




            """, """
                  
            namespace N {
                    class C {       
                void Method()           {
                                    int         i           =           
                                        1       
                                            +       
                                                3;
                    }
            }
            }       




            """);

    [Fact]
    public Task FirstTriviaAndAnchoring2()
        => AssertFormatAsync("""

            namespace N
            {
                class C
                {
                    int i =
                        1
                            +
                                3;
                }
            }




            """, """
                      
            namespace N {
                    class C {       
                                    int         i           =           
                                        1       
                                            +       
                                                3;
            }
            }               

                        


            """);

    [Fact]
    public Task FirstTriviaAndAnchoring3()
        => AssertFormatAsync("""


            class C
            {
                int i =
                    1
                        +
                            3;
            }




            """, """
                  
                        
                    class C {       
                                    int         i           =           
                                        1       
                                            +       
                                                3;
            }
                    



            """);

    [Fact]
    public Task Base1()
        => AssertFormatAsync("""
            class C
            {
                C() : base()
                {
                }
            }
            """, """
                  class             C
                        {
                C   (   )  :    base    (       )  
                        {
                        }
                }           
            """);

    [Fact]
    public Task This1()
        => AssertFormatAsync("""
            class C
            {
                C(int i) : this()
                {
                }

                C() { }
            }
            """, """
                  class             C
                        {
                C   (   int         i   )  :    this    (       )  
                        {
                        }

                    C       (           )               {                       }
                }           
            """);

    [Fact]
    public Task QueryExpression1()
        => AssertFormatAsync("""
            class C
            {
                int Method()
                {
                    var q =
                        from c in from b in cs select b select c;
                }
            }
            """, """
                  class             C
                        {
                    int Method()
                    {
                        var q = 
                            from c in                  from b in cs                         select b     select c;
                    }
                }           
            """);

    [Fact]
    public Task QueryExpression2()
        => AssertFormatAsync("""
            class C
            {
                int Method()
                {
                    var q = from c in
                                from b in cs
                                select b
                            select c;
                }
            }
            """, """
                  class             C
                        {
                    int Method()
                    {
                        var q = from c in 
                                        from b in cs
                                        select b
                select c;
                    }
                }           
            """);

    [Fact]
    public Task QueryExpression3()
        => AssertFormatAsync("""
            class C
            {
                int Method()
                {
                    var q = from c in Get(1 +
                                            2 +
                                            3)
                            from b in Get(1 +
                                        2 +
                                        3)
                            select new { b, c };
                }
            }
            """, """
            class C
            {
                int Method()
                {
                    var q =     from c in Get(1 +
                                            2 +
                                            3)
                from b in Get(1 +
                            2 +
                            3)
                    select new {                b,                 c };
                }
            }
            """);

    [Fact]
    public Task QueryExpression4()
        => AssertFormatAsync("""
            class C
            {
                int Method()
                {
                    var q =
                        from c in
                            from b in cs
                            select b
                        select c;
                }
            }
            """, """
                  class             C
                        {
                    int Method()
                    {
                        var q = 
                            from c in 
                                        from b in cs
                                        select b
                select c;
                    }
                }           
            """);

    [Fact]
    public Task Label1()
        => AssertFormatAsync("""
            class C
            {
                int Method()
                {
                L: int i = 10;
                }
            }
            """, """
                  class             C
                        {
                    int Method()
                    {
                            L           :                   int         i           =           10                  ;
                    }
                }           
            """);

    [Fact]
    public Task Label2()
        => AssertFormatAsync("""
            class C
            {
                int Method()
                {
                    int x = 1;
                L: int i = 10;
                }
            }
            """, """
                  class             C
                        {
                    int Method()
                    {
            int             x               =               1               ;
                            L           :                   int         i           =           10                  ;
                    }
                }           
            """);

    [Fact]
    public Task Label3()
        => AssertFormatAsync("""
            class C
            {
                int Method()
                {
                    int x = 1;
                L:
                    int i = 10;
                }
            }
            """, """
                  class             C
                        {
                    int Method()
                    {
            int             x               =               1               ;
                            L           :                   
            int         i           =           10                  ;
                    }
                }           
            """);

    [Fact]
    public Task Label4()
        => AssertFormatAsync("""
            class C
            {
                int Method()
                {
                    int x = 1;
                L: int i = 10;
                    int next = 30;
                }
            }
            """, """
                  class             C
                        {
                    int Method()
                    {
            int             x               =               1               ;
                            L           :                   int         i           =           10                  ;
                                                int             next            =                   30;
                    }
                }           
            """);

    [Fact]
    public Task Label5()
        => AssertFormatAsync("""
            class C
            {
                int Method()
                {
                L: int i = 10;
                    int next = 30;
                }
            }
            """, """
                  class             C
                        {
                    int Method()
                    {
                            L           :                   int         i           =           10                  ;
                                                int             next            =                   30;
                    }
                }           
            """);

    [Fact]
    public Task Label6()
        => AssertFormatAsync("""
            class C
            {
                int Method()
                {
                L:
                    int i = 10;
                    int next = 30;
                }
            }
            """, """
                  class             C
                        {
                    int Method()
                    {
                            L           :
            int         i           =           10                  ;
                                                int             next            =                   30;
                    }
                }           
            """);

    [Fact]
    public Task Label7()
        => AssertFormatAsync("""
            class C
            {
                int Method()
                {
                    int i2 = 1;
                L:
                    int i = 10;
                    int next = 30;
                }
            }
            """, """
                  class             C
                        {
                    int Method()
                    {
                int     i2              =       1   ;
                            L           :
            int         i           =           10                  ;
                                                int             next            =                   30;
                    }
                }           
            """);

    [Fact]
    public Task Label8()
        => AssertFormatAsync("""
            class C
            {
                int Method()
                {
                L:
                    int i =
                        10;
                }
            }
            """, """
                  class             C
                        {
                    int Method()
                    {
                            L:
                                int i =
                                    10;
                    }
                }           
            """);

    [Fact]
    public Task AutoProperty()
        => AssertFormatAsync("""
            class Class
            {
                private int Age { get; set; }
                public string Names { get; set; }
            }
            """, """
                     class Class
            {
                                              private       int Age{get;                set;                 }
                        public string Names                     {                        get;                      set;}
            }
            """);

    [Fact]
    public Task NormalPropertyGet()
        => AssertFormatAsync("""
            class Class
            {
                private string name;
                public string Names
                {
                    get
                    {
                        return name;
                    }
                }
            }
            """, """
            class Class
            {
                private string name;
                                      public string Names
                {
                                                     get
                                                {
                                                  return name;
                                               }
                }
            }
            """);

    [Fact]
    public Task NormalPropertyBoth()
        => AssertFormatAsync("""
            class Class
            {
                private string name;
                public string Names
                {
                    get
                    {
                        return name;
                    }
                    set
                    {
                        name = value;
                    }
                }
            }
            """, """
            class Class
            {
                private string name;
                                            public string Names
                {
                                        get
                    {
                                                               return name;
                                        }
                                                    set
                                         {
                        name = value;
                    }
                }
            }
            """);

    [Fact]
    public Task ErrorHandling1()
        => AssertFormatAsync("""
            class C
            {
                int Method()
                {
                    int a           b c;
                }
            }
            """, """
                  class             C
                        {
                    int Method()
                    {
                            int             a           b               c           ;
                    }
                }           
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/537763")]
    public Task NullableType()
        => AssertFormatAsync("""
            class Program
            {
                static void Main(string[] args)
                {
                    int? i = 10;
                }
            }
            """, """
            class Program
            {
                static void Main(string[] args)
                {
                    int         ? i = 10;
                }
            }
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/537766")]
    public Task SuppressWrappingOnBraces()
        => AssertFormatAsync("""
            class Class1
            { }

            """, """
            class Class1
            {}

            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/537824")]
    public Task DoWhile()
        => AssertFormatAsync("""
            public class Class1
            {
                void Goo()
                {
                    do
                    {
                    } while (true);
                }
            }

            """, """
            public class Class1
            {
                void Goo()
                {
                    do
                    {
                    }while (true);
                }
            }

            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/537774")]
    public Task SuppressWrappingBug()
        => AssertFormatAsync("""
            class Class1
            {
                int Goo()
                {
                    return 0;
                }
            }

            """, """
            class Class1
            {
                int Goo()
                {return 0;
                }
            }

            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/537768")]
    public Task PreserveLineForAttribute()
        => AssertFormatAsync("""
            class Class1
            {
                [STAThread]
                static void Main(string[] args)
                {
                }
            }

            """, """
            class Class1
            {
                [STAThread]
            static void Main(string[] args)
                {
                }
            }

            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/537878")]
    public Task NoFormattingOnMissingTokens()
        => AssertFormatAsync("""
            namespace ClassLibrary1
            {
                class Class1
                {
                    void Goo()
                    {
                        if (true)
                    }
                }
            }

            """, """
            namespace ClassLibrary1
            {
                class Class1
                {
                    void Goo()
                    {
                        if (true)
                    }
                }
            }

            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/537783")]
    public Task UnaryExpression()
        => AssertFormatAsync("""
            class Program
            {
                static void Main(string[] args)
                {
                    int a = 6;
                    a = a++ + 5;
                }
            }

            """, """
            class Program
            {
                static void Main(string[] args)
                {
                    int a = 6;
                    a = a++ + 5;
                }
            }

            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/537885")]
    public Task Pointer()
        => AssertFormatAsync("""
            class Program
            {
                static void Main(string[] args)
                {
                    int* p;
                }
            }

            """, """
            class Program
            {
                static void Main(string[] args)
                {
                    int* p;
                }
            }

            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/50723")]
    public async Task TuplePointer()
    {
        var properlyFormattedCode = """
            public unsafe static class Program
            {
                public static void Main(string[] args)
                {
                    int* intPointer = null;
                    (int, int)* intIntPointer = null;
                }
            }

            """;
        await AssertFormatAsync(properlyFormattedCode, properlyFormattedCode);
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/537886")]
    public Task Tild()
        => AssertFormatAsync("""
            class Program
            {
                static void Main(string[] args)
                {
                    int j = 103;
                    j = ~7;
                }
            }

            """, """
            class Program
            {
                static void Main(string[] args)
                {
                    int j = 103;
                    j = ~7;
                }
            }

            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/537884")]
    public Task ArrayInitializer1()
        => AssertFormatAsync("""
            class Program
            {
                static void Main(string[] args)
                {
                    int[] arr = {1,2,
                    3,4
                    };
                }
            }

            """, """
            class Program
            {
                static void Main(string[] args)
                {
                    int[] arr = {1,2,
                    3,4
                    };
                }
            }

            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/537884")]
    public Task ArrayInitializer2()
        => AssertFormatAsync("""
            class Program
            {
                static void Main(string[] args)
                {
                    int[] arr = new int[] {1,2,
                    3,4
                    };
                }
            }

            """, """
            class Program
            {
                static void Main(string[] args)
                {
                    int[] arr =  new int [] {1,2,
                    3,4
                    };
                }
            }

            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/537884")]
    public Task ImplicitArrayInitializer()
        => AssertFormatAsync("""
            class Program
            {
                static void Main(string[] args)
                {
                    var arr = new[] {1,2,
                    3,4
                    };
                }
            }

            """, """
            class Program
            {
                static void Main(string[] args)
                {
                    var arr = new [] {1,2,
                    3,4
                    }           ;
                }
            }

            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/65498")]
    public Task StackAllocArrayInitializer0()
        => AssertFormatAsync("""
            F(stackalloc int[]
                {
                    1,
                    2,
                });
            """, """
            F(stackalloc int[]
                {
                    1,
                    2,
                }                );
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/65498")]
    public Task StackAllocArrayInitializer0_Implicit()
        => AssertFormatAsync("""
            F(stackalloc[]
                {
                    1,
                    2,
                }
            );
            """, """
            F(                    stackalloc []
                {
                    1,
                    2,
                }
            );
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/65498")]
    public Task StackAllocArrayInitializer1()
        => AssertFormatAsync("""
            F(
                stackalloc int[]
                {
                    1,2,
                    3,4
                }
            );
            """, """
            F(
                stackalloc int[]
                {
                    1,2,
                    3,4
                }
            );
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/65498")]
    public Task StackAllocArrayInitializer1_Implicit()
        => AssertFormatAsync("""
            F(
                stackalloc[]
                {
                    1,2,
                    3,4
                }
            );
            """, """
            F(
                stackalloc []
                {
                    1,2,
                    3,4
                }
            );
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/65498")]
    public Task StackAllocArrayInitializer2()
        => AssertFormatAsync("""
            var x = (stackalloc int[] {1,2,
                 3
            });
            """, """
            var x = (stackalloc int[] {1,2,
                 3
            });
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/65498")]
    public Task StackAllocArrayInitializer2_Implicit()
        => AssertFormatAsync("""
            var x = (stackalloc[]
            {1,
                2, 3
            });
            """, """
            var x = (stackalloc []
            {1,
                2, 3
            });
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/537884")]
    public Task CollectionInitializer()
        => AssertFormatAsync("""
            class Program
            {
                static void Main(string[] args)
                {
                    var arr = new List<int> {1,2,
                    3,4
                    };
                }
            }

            """, """
            class Program
            {
                static void Main(string[] args)
                {
                    var arr = new List<int> {1,2,
                    3,4
                    };
                }
            }

            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/537916")]
    public Task AddressOfOperator()
        => AssertFormatAsync("""
            unsafe class Class1
            {
                void Method()
                {
                    int a = 12;
                    int* p = &a;
                }
            }

            """, """
            unsafe class Class1
            {
                void Method()
                {
                    int a = 12;
                    int* p = &a;
                }
            }

            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/537885")]
    public Task DereferenceOperator()
        => AssertFormatAsync("""
            unsafe class Class1
            {
                void Method()
                {
                    int a = 12;
                    int* p = &a;
                    Console.WriteLine(*p);
                }
            }

            """, """
            unsafe class Class1
            {
                void Method()
                {
                    int a = 12;
                    int* p = & a;
                    Console.WriteLine(* p);
                }
            }

            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/537905")]
    public Task Namespaces()
        => AssertFormatAsync("""
            using System;
            using System.Data;
            """, @"using System; using System.Data;");

    [Fact]
    public Task NamespaceDeclaration()
        => AssertFormatAsync("""
            namespace N
            {
            }
            """, """
            namespace N
                {
            }
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/537902")]
    public Task DoWhile1()
        => AssertFormatAsync("""
            class Program
            {
                static void Main(string[] args)
                {
                    do { }
                    while (i < 4);
                }
            }
            """, """
            class Program
            {
                static void Main(string[] args)
                {
                    do { }
                    while (i < 4);
                }
            }
            """);

    [Fact]
    public Task NewConstraint()
        => AssertFormatAsync("""
            class Program
            {
                void Test<T>(T t) where T : new()
                {
                }
            }
            """, """
            class Program
            {
                void Test<T>(T t) where T : new (   )
                {
                }
            }
            """);

    [Fact]
    public Task UnaryExpressionWithInitializer()
        => AssertFormatAsync("""
            using System;
            using System.Collections.Generic;
            using System.Linq;

            class Program
            {
                static void Main(string[] args)
                {
                    if ((new int[] { 1, 2, 3 }).Any())
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
                    if ((new int[] { 1, 2, 3 }          ).Any())
                    {
                        return;
                    }
                }
            }
            """);

    [Fact]
    public Task Attributes1()
        => AssertFormatAsync("""
            class Program
            {
                [Flags] public void Method() { }
            }
            """, """
            class Program
            {
                    [   Flags       ]       public       void       Method      (       )           {           }
            }
            """);

    [Fact]
    public Task Attributes2()
        => AssertFormatAsync("""
            class Program
            {
                [Flags]
                public void Method() { }
            }
            """, """
            class Program
            {
                    [   Flags       ]
            public       void       Method      (       )           {           }
            }
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538288")]
    public Task ColonColon1()
        => AssertFormatAsync("""
            class Program
            {
                public void Method()
                {
                    throw new global::System.NotImplementedException();
                }
            }
            """, """
            class Program
            {
            public       void       Method      (       )           {
                throw new global :: System.NotImplementedException();
            }
            }
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538354")]
    public Task BugFix3939()
        => AssertFormatAsync("""
            using
                  System.
                      Collections.
                          Generic;
            """, """
                              using
                                    System.
                                        Collections.
                                            Generic;
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538354")]
    public async Task Tab1()
        => await AssertFormatAsync(@"using System;", @"			using System;");

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538329")]
    public Task SuppressLinkBreakInIfElseStatement()
        => AssertFormatAsync("""
            class Program
            {
                static void Main(string[] args)
                {
                    int a;
                    if (true) a = 10;
                    else a = 11;
                }
            }
            """, """
            class Program
            {
                static void Main(string[] args)
                {
                    int a;
                    if (true) a = 10;
                    else a = 11;
                }
            }
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538464")]
    public Task BugFix4087()
        => AssertFormatAsync("""
            class Program
            {
                static void Main(string[] args)
                {
                    Func<int, int> fun = x => { return x + 1; }
                }
            }
            """, """
            class Program
            {
                static void Main(string[] args)
                {
                    Func<int, int> fun = x => { return x + 1; }
                }
            }
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538511")]
    public Task AttributeTargetSpecifier()
        => AssertFormatAsync("""
            public class Class1
            {
                [method:
                void Test()
                {
                }
            }
            """, """
            public class Class1
            {
                [method :
                void Test()
                {
                }
            }
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538635")]
    public Task Finalizer()
        => AssertFormatAsync("""
            public class Class1
            {
                ~Class1() { }
            }
            """, """
            public class Class1
            {
                ~ Class1() { }
            }
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538743")]
    public async Task BugFix4442()
    {
        var code = """
            class Program
            {
                static void Main(string[] args)
                {
                    string str = "ab,die|wo";
                    string[] a = str.Split(new char[] { ',', '|' })
                        ;
                }
            }
            """;

        await AssertFormatAsync(code, code);
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538658")]
    public Task BugFix4328()
        => AssertFormatAsync("""
            class Program
            {
                static void Main(string[] args)
                {
                    double d = new double();
                }
            }
            """, """
            class Program
            {
                static void Main(string[] args)
                {
                    double d = new double           ();
                }
            }
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538658")]
    public Task BugFix4515()
        => AssertFormatAsync("""
            class Program
            {
                static void Main(string[] args)
                {
                    var t = typeof(System.Object);
                    var t1 = default(System.Object);
                    var t2 = sizeof(System.Object);
                }
            }
            """, """
            class Program
            {
                static void Main(string[] args)
                {
                    var t = typeof ( System.Object )    ;
                    var t1 =    default     (   System.Object       )       ;
                    var t2 =        sizeof              (               System.Object       )   ;
                }
            }
            """);

    [Fact]
    public Task CastExpressionTest()
        => AssertFormatAsync("""
            class Program
            {
                static void Main(string[] args)
                {
                    var a = (int)1;
                }
            }
            """, """
            class Program
            {
                static void Main(string[] args)
                {
                    var a = (int) 1;
                }
            }
            """);

    [Fact]
    public Task NamedParameter()
        => AssertFormatAsync("""
            class Program
            {
                static void Main(string[] args)
                {
                    Main(args: null);
                }
            }
            """, """
            class Program
            {
                static void Main(string[] args)
                {
                    Main        (       args           :           null     )       ;  
                }
            }
            """);

    [Fact]
    public Task RefReadonlyParameters()
        => AssertFormatAsync("""
            class C
            {
                int this[ref readonly int x, ref readonly int y] { get; set; }
                void M(ref readonly int x, ref readonly int y) { }
            }
            """, """
            class C
            {
                int   this  [   ref     readonly    int      x   ,   ref    readonly   int   y   ]   {   get ;   set ;  }
                void    M  (   ref    readonly     int   x    ,   ref    readonly   int   y   )  {   }
            }
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539259")]
    public Task BugFix5143()
        => AssertFormatAsync("""
            class Program
            {
                static void Main(string[] args)
                {
                    int x = Goo(
                        delegate (int x) { return x; });
                }
            }
            """, """
            class Program
            {
                static void Main(string[] args)
                {
                    int x = Goo (   
                        delegate (  int     x   )   {   return  x    ; }    )   ;   
                }
            }
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539338")]
    public Task BugFix5251()
        => AssertFormatAsync("""
            class Program
            {
                public static string Goo { get; private set; }
            }
            """, """
            class Program
            {
                    public static string Goo { get; private set; }
            }
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539358")]
    public Task BugFix5277()
        => AssertFormatAsync("""

            #if true
            #endif

            """, """

            #if true
                        #endif

            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539542")]
    public Task BugFix5544()
        => AssertFormatAsync("""

            class Program
            {
                unsafe static void Main(string[] args)
                {
                    Program* p;
                    p->Goo = 5;
                }
            }

            """, """

            class Program
            {
                unsafe static void Main(string[] args)
                {
                    Program* p;
                    p -> Goo = 5;
                }
            }

            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539587")]
    public Task BugFix5602()
        => AssertFormatAsync("""
            class Bug
            {
                public static void func()
                {
                    long b = //
                    }
            }
            """, """
                class Bug
                {
                    public static void func()
                    {
                        long b = //
                    }
                }
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539616")]
    public Task BugFix5637()
        => AssertFormatAsync("""
            class Bug
            {
                // test
                public static void func()
                {
                }
            }
            """, """
            class Bug
            {
                // test
            	public static void func()
                {
                }
            }
            """);

    [Fact]
    public Task GenericType()
        => AssertFormatAsync("""
            class Bug<T>
            {
                class N : Bug<T[]>
                {
                }
            }
            """, """
            class Bug<T>
            {
                class N : Bug<  T   [   ]   >
                {
                }
            }
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539878")]
    public Task BugFix5978()
        => AssertFormatAsync("""
            class Program
            {
                static void Main(string[] args)
                {
                    int i = 3;
                label4:
                    if (i < 5)
                    {
                    label5:
                        if (i == 4)
                        {
                        }
                        else
                        {
                            System.Console.WriteLine("a");
                        }
                    }
                }
            }
            """, """
            class Program
            {
                static void Main(string[] args)
                {
                    int i = 3;
                label4:
                    if (i < 5)
                    {
                    label5:
            if (i == 4)
            {
            }
            else
            {
            System.Console.WriteLine("a");
            }
                    }
                }
            }
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539878")]
    public Task BugFix5979()
        => AssertFormatAsync("""
            delegate int del(int i);
            class Program
            {
                static void Main(string[] args)
                {
                    del q = x =>
                    {
                    label2: goto label1;
                    label1: return x;
                    };
                }
            }
            """, """
            delegate int del(int i);
            class Program
            {
                static void Main(string[] args)
                {
                    del q = x =>
                    {
                            label2: goto label1;
                            label1: return x;
                    };
                }
            }
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539891")]
    public Task BugFix5993()
        => AssertFormatAsync("""
            public class MyClass
            {
                public static void Main()
                {
                lab1:
                    {
                    lab1:// CS0158
                        goto lab1;
                    }
                }
            }
            """, """
            public class MyClass
            {
                public static void Main()
                {
                lab1:
                    {
                lab1:// CS0158
                            goto lab1;
                    }
                }
            }
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540315")]
    public Task BugFix6536()
        => AssertFormatAsync("""
            public class MyClass
            {
                public static void Main()
                {
                    int i = - -1 + + +1 + -+1 + -+1;
                }
            }
            """, """
            public class MyClass
            {
                public static void Main()
                {
                    int i = - - 1 + + + 1 + - + 1 + - + 1   ;
                }
            }
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540801")]
    public Task BugFix7211()
        => AssertFormatAsync("""
            class Program
            {
                static void Main(string[] args)
                {
                    while (0 > new int[] { 1 }.Length)
                    {
                        System.Console.WriteLine("Hello");
                    }
                }
            }
            """, """
            class Program
            {
                static void Main(string[] args)
                {
                    while (0 > new int[] { 1 }.Length)
                    {
                        System.Console.WriteLine("Hello");
                    }
                }
            }
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541035")]
    public Task BugFix7564_1()
        => AssertFormatAsync("""
            class Program
            {
                static void Main(string[] args)
                {
                    while (null != new int[] { 1 })
                    {
                        System.Console.WriteLine("Hello");
                    }
                }
            }
            """, """
            class Program
            {
                static void Main(string[] args)
                {
                    while (null != new int[] { 1 })
                    {
                        System.Console.WriteLine("Hello");
                    }
                }
            }
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541035")]
    public Task BugFix7564_2()
        => AssertFormatAsync("""
            class Program
            {
                static void Main(string[] args)
                {
                    foreach (var f in new int[] { 5 })
                    {
                        Console.WriteLine(f);
                    }
                }
            }
            """, """
            class Program
            {
                static void Main(string[] args)
                {
                    foreach (var f in new int[] { 5 })
                    {
                        Console.WriteLine(f);
                    }
                }
            }
            """);

    [Fact, WorkItem(8385, "DevDiv_Projects/Roslyn")]
    public Task NullCoalescingOperator()
        => AssertFormatAsync("""
            class C
            {
                void M()
                {
                    object o2 = null ?? null;
                }
            }
            """, """
            class C
            {
                void M()
                {
                    object o2 = null??null;
                }
            }
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541925")]
    public Task QueryContinuation()
        => AssertFormatAsync("""
            using System.Linq;
            class C
            {
                static void Main(string[] args)
                {
                    var temp = from x in "abc"
                               let z = x.ToString()
                               select z into w
                               select w;
                }
            }
            """, """
            using System.Linq;
            class C
            {
                static void Main(string[] args)
                {
                    var temp = from x in "abc"
                               let z = x.ToString()
                               select z into w
                    select w;
                }
            }
            """);

    [Fact]
    public Task QueryContinuation2()
        => AssertFormatAsync("""
            using System.Linq;
            class C
            {
                static void Main(string[] args)
                {
                    var temp = from x in "abc"
                               select x into
                }
            }
            """, """
            using System.Linq;
            class C
            {
                static void Main(string[] args)
                {
                    var temp = from x in "abc" select x into
                }
            }
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542305")]
    public Task AttributeFormatting1()
        => AssertFormatAsync("""
            class Program
            {
                void AddClass(string name, [OptionalAttribute] object position, [OptionalAttribute] object bases)
                {
                }
            }
            """, """
            class Program
            {
                void AddClass(string name,[OptionalAttribute]    object position,[OptionalAttribute]    object bases)
                {
                }
            }
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542304")]
    public Task CloseBracesInArgumentList()
        => AssertFormatAsync("""
            class Program
            {
                static void Main(string[] args)
                {
                    var relativeIndentationGetter = new Lazy<int>(() =>
                    {
                        var indentationDelta = operation.IndentationDeltaOrPosition * this.OptionSet.IndentationSize;
                        var baseIndentation = this.tokenStream.GetCurrentColumn(operation.BaseToken);

                        return baseIndentation + indentationDelta;
                    }, isThreadSafe: true);
                }
            }
            """, """
            class Program
            {
                static void Main(string[] args)
                {
                    var relativeIndentationGetter = new Lazy<int>(() =>
                    {
                        var indentationDelta = operation.IndentationDeltaOrPosition * this.OptionSet.IndentationSize;
                        var baseIndentation = this.tokenStream.GetCurrentColumn(operation.BaseToken);

                        return baseIndentation + indentationDelta;
                    }           ,           isThreadSafe: true);
                }
            }
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542538")]
    public Task MissingTokens()
        => AssertFormatAsync("""
            using System;
            delegate void myDelegate(int name = 1);
            class innerClass
            {
                public innerClass()
                {
                    myDelegate x = (int y = 1) => { return; };
                }
            }
            """, """
            using System;
            delegate void myDelegate(int name = 1);
            class innerClass
            {
                public innerClass()
                {
                    myDelegate x = (int y=1) => { return; };
                }
            }
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542199")]
    public Task ColumnOfVeryFirstToken()
        => AssertFormatAsync(@"W   )b", @"			       W   )b");

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542718")]
    public Task EmptySuppressionSpan()
        => AssertFormatAsync("""
            enum E
            {
                a,,
            }
            """, """
            enum E
                {
                    a,,
                }
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542790")]
    public Task LabelInSwitch()
        => AssertFormatAsync("""
            class test
            {
                public static void Main()
                {
                    string target = "t1";
                    switch (target)
                    {
                        case "t1":
                        label1:
                            goto label1;
                        case "t2":
                        label2:
                            goto label2;
                    }
                }
            }
            """, """
            class test
            {
                public static void Main()
                {
                    string target = "t1";
                    switch (target)
                    {
                        case "t1":
                    label1:
                        goto label1;
                        case "t2":
                            label2:
                                goto label2;
                    }
                }
            }
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543112")]
    public void FormatArbitaryNode()
    {
        var property = SyntaxFactory.PropertyDeclaration(
            attributeLists: [],
            [PublicKeyword],
            SyntaxFactory.ParseTypeName("int"),
            null,
            SyntaxFactory.Identifier("Prop"),
            SyntaxFactory.AccessorList([
                SyntaxFactory.AccessorDeclaration(
                    SyntaxKind.GetAccessorDeclaration,
                    SyntaxFactory.Block(SyntaxFactory.ParseStatement("return c;"))),
                SyntaxFactory.AccessorDeclaration(
                    SyntaxKind.SetAccessorDeclaration,
                    SyntaxFactory.Block(SyntaxFactory.ParseStatement("c = value;")))]));

        Assert.NotNull(property);
        using var workspace = new AdhocWorkspace();
        var newProperty = Formatter.Format(property, workspace.Services.SolutionServices, CSharpSyntaxFormattingOptions.Default, CancellationToken.None);

        Assert.Equal("""
            public int Prop
            {
                get
                {
                    return c;
                }

                set
                {
                    c = value;
                }
            }
            """, newProperty.ToFullString());
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543140")]
    public Task OmittedTypeArgument()
        => AssertFormatAsync("""
            using System;
            using System.Collections.Generic;
            using System.Linq;

            class Program
            {
                static void Main(string[] args)
                {
                    Console.WriteLine(typeof(Dictionary<,>).IsGenericTypeDefinition);
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
                    Console.WriteLine(typeof(Dictionary<, >).IsGenericTypeDefinition);
                }
            }
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543131")]
    public Task TryAfterLabel()
        => AssertFormatAsync("""
            using System;
            class Program
            {
                static object lockObj = new object();
                static int Main()
                {
                    int sum = 0;
                    lock (lockObj)
                        try
                        { sum = 0; }
                        catch (Exception ex)
                        { Console.WriteLine(ex); }
                    return sum;
                }
            }
            """, """
            using System;
            class Program
            {
                static object lockObj = new object();
                static int Main()
                {
                    int sum = 0;
                    lock (lockObj)
                        try
                    { sum = 0; }
                    catch (Exception ex)
                    { Console.WriteLine(ex); }
                    return sum;
                }
            }
            """);

    [Fact]
    public Task QueryContinuation1()
        => AssertFormatAsync("""
            using System.Linq;

            class Program
            {
                static void Main(string[] args)
                {
                    var q = from arg in args
                            group arg by arg.Length into final
                            where final
                                .Select(c => c)
                                .Distinct()
                                .Count() > 0
                            select final;
                }
            }
            """, """
            using System.Linq;

            class Program
            {
                static void Main(string[] args)
                {
                    var q =             from arg in args
                                        group arg by arg.Length into final
                                        where final
                                            .Select(c => c)
                                            .Distinct()
                                            .Count() > 0
                                        select final;
                }
            }
            """);

    [Fact]
    public Task TestCSharpFormattingSpacingOptions()
        => AssertFormatAsync("""

            interface f1
            { }

            interface f2 : f1 { }

            struct d2 : f1 { }

            class goo : System.Object
            {
                public int bar = 1 * 2;
                public void goobar()
                {
                    goobar();
                }
                public int toofoobar(int i, int j)
                {
                    int s = (int)(34);
                    if (i < 0)
                    {
                    }
                    return toofoobar(i, j);
                }
                public string parfoobar(string[] str)
                {
                    for (int i = 0; i < 28; i++) { }
                    return str[5];
                }
            }
            """, """

            interface f1
            { }

            interface f2     :    f1 { }

            struct d2   :    f1 { }

            class goo      :      System        .     Object
            {
                public     int     bar    =   1*   2;
                public void goobar      (         ) 
                {
                    goobar        (         );
                }
                public int toofoobar(   int i    ,    int j       )
                {
                    int s = (        int  )   (     34    );
                    if              (   i < 0    )
                    {
                    }
                    return toofoobar(      i,j      );
                }
                public string parfoobar(string       [      ] str)
                {
                    for(int i = 0       ;        i < 28  ;   i++) { }
                    return str[    5    ];
                }
            }
            """);

    [Fact]
    public Task SpacingFixInTokenBasedForIfAndSwitchCase()
        => AssertFormatAsync("""
            class Class5
            {
                void bar()
                {
                    if (x == 1)
                        x = 2;
                    else x = 3;
                    switch (x)
                    {
                        case 1: break;
                        case 2: break;
                        default: break;
                    }
                }
            }
            """, """
            class Class5{
            void bar()
            {
            if(x == 1) 
            x = 2; else x = 3;
            switch (x) { 
            case 1: break; case 2: break; default: break;}
            }
            }
            """);

    [Fact]
    public Task SpacingInDeconstruction()
        => AssertFormatAsync("""
            class Class5
            {
                void bar()
                {
                    var (x, y) = (1, 2);
                }
            }
            """, """
            class Class5{
            void bar()
            {
            var(x,y)=(1,2);
            }
            }
            """);

    [Fact]
    public Task SpacingInNullableTuple()
        => AssertFormatAsync("""
            class Class5
            {
                void bar()
                {
                    (int, string)? x = (1, "hello");
                }
            }
            """, """
            class Class5
            {
                void bar()
                {
                    (int, string) ? x = (1, "hello");
                }
            }
            """);

    [Fact]
    public Task SpacingInTupleArrayCreation()
        => AssertFormatAsync("""
            class C
            {
                void bar()
                {
                    (string a, string b)[] ab = new (string a, string b)[1];
                }
            }
            """, """
            class C
            {
                void bar()
                {
                    (string a, string b)[] ab = new(string a, string b) [1];
                }
            }
            """);

    [Fact]
    public Task SpacingInTupleArrayCreation2()
        => AssertFormatAsync("""
            class C
            {
                void bar()
                {
                    (string a, string b)[] ab = new(
                }
            }
            """, """
            class C
            {
                void bar()
                {
                    (string a, string b)[] ab = new(
                }
            }
            """);

    [Fact]
    public Task SpacingInImplicitObjectCreation()
        => AssertFormatAsync("""
            class C
            {
                void bar()
                {
                    C a = new();
                }
            }
            """, """
            class C
            {
                void bar()
                {
                    C a = new ();
                }
            }
            """);

    [Fact]
    public Task FormatRecursivePattern_Positional()
        => AssertFormatAsync("""
            class C
            {
                void M() { _ = this is (1, 2); }
            }
            """, """
            class C
            {
                void M() { _ = this is  ( 1 , 2 )  ; }
            }
            """);

    [Fact]
    public Task FormatRecursivePattern_Positional_Singleline()
        => AssertFormatAsync("""
            class C
            {
                void M()
                {
                    _ = this is (1, 2);
                }
            }
            """, """
            class C
            {
                void M() {
            _ = this is  ( 1 , 2 )  ; }
            }
            """);

    [Fact]
    public Task FormatRecursivePattern_Positional_Multiline()
        => AssertFormatAsync("""
            class C
            {
                void M()
                {
                    _ = this is (1,
                    2,
                    3);
                }
            }
            """, """
            class C
            {
                void M() {
            _ = this is  ( 1 ,
            2 ,
            3 )  ; }
            }
            """);

    [Fact]
    public Task FormatRecursivePattern_Positional_Multiline2()
        => AssertFormatAsync("""
            class C
            {
                void M()
                {
                    _ = this is (1,
                    2,
                    3);
                }
            }
            """, """
            class C
            {
                void M() {
            _ = this is  ( 1 ,
            2 ,
            3 )  ; }
            }
            """);

    [Fact]
    public Task FormatRecursivePattern_Positional_Multiline3()
        => AssertFormatAsync("""
            class C
            {
                void M()
                {
                    _ = this is
                    (1,
                    2,
                    3);
                }
            }
            """, """
            class C
            {
                void M() {
            _ = this is
            ( 1 ,
            2 ,
            3 )  ; }
            }
            """);

    [Fact]
    public Task FormatRecursivePattern_Positional_Multiline4()
        => AssertFormatAsync("""
            class C
            {
                void M()
                {
                    _ = this is
                    (1,
                    2, 3);
                }
            }
            """, """
            class C
            {
                void M() {
            _ = this is
            ( 1 ,
            2 , 3 )  ; }
            }
            """);

    [Fact]
    public Task FormatRecursivePattern_Properties_Singleline()
        => AssertFormatAsync("""
            class C
            {
                void M() { _ = this is C { P1: 1 }; }
            }
            """, """
            class C
            {
                void M() { _ = this is  C{  P1 :  1  } ; }
            }
            """);

    [Fact]
    public Task FormatRecursivePattern_Properties_Multiline()
        => AssertFormatAsync("""
            class C
            {
                void M()
                {
                    _ = this is
                    {
                        P1: 1,
                        P2: 2
                    };
                }
            }
            """, """
            class C
            {
                void M() {
            _ = this is
            {
            P1 :  1  ,
            P2 : 2
            } ;
            }
            }
            """);

    [Fact]
    public Task FormatRecursivePattern_Properties_Multiline2()
        => AssertFormatAsync("""
            class C
            {
                void M()
                {
                    _ = this is
                    {
                        P1: 1,
                        P2: 2
                    };
                }
            }
            """, """
            class C
            {
                void M() {
            _ = this is {
            P1 :  1  ,
            P2 : 2
            } ;
            }
            }
            """);

    [Fact]
    public Task FormatRecursivePattern_Properties_Multiline3()
        => AssertFormatAsync("""
            class C
            {
                void M()
                {
                    _ = this is
                    {
                        P1: 1,
                        P2: 2, P3: 3
                    };
                }
            }
            """, """
            class C
            {
                void M() {
            _ = this is {
            P1 :  1  ,
            P2 : 2, P3: 3
            } ;
            }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/27268")]
    public Task FormatRecursivePattern_NoSpaceBetweenTypeAndPositionalSubpattern()
        => AssertFormatAsync("""
            class C
            {
                void M()
                {
                    _ = this is C(1, 2) { };
                }
            }
            """, """
            class C
            {
                void M() {
            _ = this is  C( 1 , 2 ){}  ; }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/27268")]
    public async Task FormatRecursivePattern_PreferSpaceBetweenTypeAndPositionalSubpattern()
    {
        var changingOptions = new OptionsCollection(LanguageNames.CSharp)
        {
            { CSharpFormattingOptions2.SpaceAfterMethodCallName, true }
        };
        await AssertFormatAsync("""
            class C
            {
                void M()
                {
                    _ = this is C (1, 2) { };
                }
            }
            """, """
            class C
            {
                void M() {
            _ = this is  C( 1 , 2 ){}  ; }
            }
            """, changedOptionSet: changingOptions);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/27268")]
    public async Task FormatRecursivePattern_PreferSpaceInsidePositionalSubpatternParentheses()
    {
        var changingOptions = new OptionsCollection(LanguageNames.CSharp)
        {
            { CSharpFormattingOptions2.SpaceWithinMethodCallParentheses, true }
        };
        await AssertFormatAsync("""
            class C
            {
                void M()
                {
                    _ = this is C( 1, 2 ) { };
                    _ = this is C() { };
                }
            }
            """, """
            class C
            {
                void M() {
            _ = this is  C( 1 , 2 ){}  ;
            _ = this is  C(  ){}  ; }
            }
            """, changedOptionSet: changingOptions);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/27268")]
    public async Task FormatRecursivePattern_PreferSpaceInsideEmptyPositionalSubpatternParentheses()
    {
        var changingOptions = new OptionsCollection(LanguageNames.CSharp)
        {
            { CSharpFormattingOptions2.SpaceBetweenEmptyMethodCallParentheses, true }
        };
        await AssertFormatAsync("""
            class C
            {
                void M()
                {
                    _ = this is C(1, 2) { };
                    _ = this is C( ) { };
                }
            }
            """, """
            class C
            {
                void M() {
            _ = this is  C( 1 , 2 ){}  ;
            _ = this is  C(  ){}  ; }
            }
            """, changedOptionSet: changingOptions);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/34683")]
    public async Task FormatRecursivePattern_InBinaryOperation()
    {
        var changingOptions = new OptionsCollection(LanguageNames.CSharp)
        {
            { CSharpFormattingOptions2.SpaceWithinMethodCallParentheses, true }
        };
        var code = """
            class C
            {
                void M()
                {
                    return
                        typeWithAnnotations is { } && true;
                }
            }
            """;
        var expectedCode = code;
        await AssertFormatAsync(expectedCode, code, changedOptionSet: changingOptions);
    }

    [Fact]
    public Task FormatPropertyPattern_MultilineAndEmpty()
        => AssertFormatAsync("""
            class C
            {
                void M()
                {
                    _ = this is
                    {
                    };
                }
            }
            """, """
            class C
            {
                void M() {
            _ = this is
                        {
                            };
            }
            }
            """);

    [Fact]
    public Task FormatSwitchExpression_IndentArms()
        => AssertFormatAsync("""
            class C
            {
                void M()
                {
                    _ = this switch
                    {
                        { P1: 1 } => true,
                        (0, 1) => true,
                        _ => false
                    };

                }
            }
            """, """
            class C
            {
                void M() {
            _ = this switch
            {
            { P1: 1} => true,
            (0, 1) => true,
            _ => false
            };

            }
            }
            """);

    [Fact]
    public Task FormatPropertyPattern_FollowedByInvocation()
        => AssertFormatAsync("""
            class C
            {
                void M()
                {
                    _ = this is { }
                    M();
                }
            }
            """, """
            class C
            {
                void M() {
            _ = this is { }
            M();
            }
            }
            """);

    [Fact]
    public Task FormatPositionalPattern_FollowedByInvocation()
        => AssertFormatAsync("""
            class C
            {
                void M()
                {
                    _ = this is (1, 2) { }
                    M();
                }
            }
            """, """
            class C
            {
                void M() {
            _ = this is (1, 2) { }
            M();
            }
            }
            """);

    [Fact]
    public Task FormatPositionalPattern_FollowedByScope()
        => AssertFormatAsync("""
            class C
            {
                void M()
                {
                    _ = this is (1, 2)
                    {
                    M();
                }
            }
            }
            """, """
            class C
            {
                void M() {
            _ = this is (1, 2)
            {
                M();
            }
            }
            }
            """);

    [Fact]
    public Task FormatSwitchExpression_MultilineAndNoArms()
        => AssertFormatAsync("""
            class C
            {
                void M()
                {
                    _ = this switch
                    {
                    };
                }
            }
            """, """
            class C
            {
                void M() {
            _ = this switch
            {
                };
            }
            }
            """);

    [Fact]
    public Task FormatSwitchExpression_ExpressionAnchoredToArm()
        => AssertFormatAsync("""
            class C
            {
                void M()
                {
                    _ = this switch
                    {
                        { P1: 1 }
                        => true,
                        (0, 1)
                            => true,
                        _
                                => false
                    };

                }
            }
            """, """
            class C
            {
                void M() {
            _ = this switch
            {
            { P1: 1} 
            => true,
            (0, 1)
                => true,
            _
                    => false
            };

            }
            }
            """);

    [Fact]
    public Task FormatSwitchExpression_NoSpaceBeforeColonInArm()
        => AssertFormatAsync("""
            class C
            {
                void M()
                {
                    _ = this switch
                    {
                        { P1: 1 }
                        => true,
                        (0, 1)
                            => true,
                        _
                                => false
                    };

                }
            }
            """, """
            class C
            {
                void M() {
            _ = this switch
            {
            { P1: 1}
            => true,
            (0, 1)
                => true,
            _
                    => false
            };

            }
            }
            """);

    [Fact]
    public Task FormatSwitchExpression_ArmCommaWantsNewline()
        => AssertFormatAsync("""
            class C
            {
                void M()
                {
                    _ = this switch
                    {
                        { P1: 1 } => true,
                        (0, 1) => true,
                        _ => false
                    };

                }
            }
            """, """
            class C
            {
                void M() {
            _ = this switch
            {
            { P1: 1} => true,
            (0, 1) => true, _ => false
            };

            }
            }
            """);

    [Fact]
    public Task FormatSwitchExpression_ArmCommaPreservesLines()
        => AssertFormatAsync("""
            class C
            {
                void M()
                {
                    _ = this switch
                    {
                        { P1: 1 } => true,

                        (0, 1) => true,
                        _ => false
                    };

                }
            }
            """, """
            class C
            {
                void M() {
            _ = this switch
            {
            { P1: 1} => true,

            (0, 1) => true, _ => false
            };

            }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/33839")]
    public Task FormatSwitchExpression_ExpressionBody()
        => AssertFormatAsync("""

            public class Test
            {
                public object Method(int i)
                    => i switch
                    {
                        1 => 'a',
                        2 => 'b',
                        _ => null,
                    };
            }
            """, """

            public class Test
            {
                public object Method(int i)
                    => i switch
            {
            1 => 'a',
            2 => 'b',
            _ => null,
            };
            }
            """);

    [Theory, WorkItem("https://github.com/dotnet/roslyn/issues/72196")]
    [InlineData("[]")]
    [InlineData("[a]")]
    [InlineData("[a, b]")]
    [InlineData("[..]")]
    [InlineData("[var a, .., var b]")]
    [InlineData("[{ } a, null]")]
    [InlineData("[a, []]")]
    public Task FormatSwitchExpression_ListPatternAligned(string listPattern)
        => AssertFormatAsync($$"""
            class C
            {
                void M()
                {
                    _ = Array.Empty<string>() switch
                    {
                        {{listPattern}} => 0,
                        _ => 1,
                    };
                }
            }
            """, $$"""
            class C
            {
                void M()
                {
                    _ = Array.Empty<string>() switch
                    {
                    {{listPattern}} => 0,
                        _ => 1,
                    };
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/81203")]
    public Task FormatSwitchExpression_ListPatternInOrAndPattern()
        => AssertFormatAsync("""
            class C
            {
                void M()
                {
                    var x = "y" switch
                    {
                        ['a'] or "b" => 1,
                        ['c'] or ['d'] => 2,
                        ['e'] => 3,
                        "f" or ['g'] => 4,
                        "h" or "i" => 5,
                        _ => 0,
                    };
                }
            }
            """, """
            class C
            {
                void M()
                {
                    var x = "y" switch
                    {
            ['a'] or "b" => 1,
            ['c'] or ['d'] => 2,
                        ['e'] => 3,
                        "f" or ['g'] => 4,
                        "h" or "i" => 5,
                        _ => 0,
                    };
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/81203")]
    public Task FormatSwitchExpression_ListPatternInAndPattern()
        => AssertFormatAsync("""
            class C
            {
                void M()
                {
                    var x = new object() switch
                    {
                        [var a] and [> 0] => 1,
                        [1, 2] and [.., var b] => 2,
                        _ => 0,
                    };
                }
            }
            """, """
            class C
            {
                void M()
                {
                    var x = new object() switch
                    {
            [var a] and [> 0] => 1,
            [1, 2] and [.., var b] => 2,
                        _ => 0,
                    };
                }
            }
            """);

    [Fact]
    public Task FormatSwitchWithPropertyPattern()
        => AssertFormatAsync("""
            class C
            {
                void M()
                {
                    switch (this)
                    {
                        case { P1: 1, P2: { P3: 3, P4: 4 } }:
                            break;
                    }
                }
            }
            """, """
            class C
            {
                void M()
                {
                    switch (this)
                    {
                        case { P1: 1, P2: { P3: 3, P4: 4 } }:
                            break;
                    }
                }
            }
            """);

    [Fact]
    public Task FormatSwitchWithPropertyPattern_Singleline()
        => AssertFormatAsync("""
            class C
            {
                void M()
                {
                    switch (this)
                    {
                        case { P1: 1, P2: { P3: 3, P4: 4 } }: break;
                    }
                }
            }
            """, """
            class C
            {
                void M()
                {
                    switch (this)
                    {
                        case { P1: 1, P2: { P3: 3, P4: 4 } }: break;
                    }
                }
            }
            """);

    [Fact]
    public Task FormatSwitchWithPropertyPattern_Singleline2()
        => AssertFormatAsync("""
            class C
            {
                void M()
                {
                    switch (this)
                    {
                        case { P1: 1, P2: { P3: 3, P4: 4 } }:
                            System.Console.Write(1);
                            break;
                    }
                }
            }
            """, """
            class C
            {
                void M()
                {
                    switch (this)
                    {
                        case { P1: 1, P2: { P3: 3, P4: 4 } }: System.Console.Write(1);
                break;
                    }
                }
            }
            """);

    [Fact]
    public Task SpacingInTupleExtension()
        => AssertFormatAsync("""
            static class Class5
            {
                static void Extension(this (int, string) self) { }
            }
            """, """
            static class Class5
            {
                static void Extension(this(int, string) self) { }
            }
            """);

    [Fact]
    public Task SpacingInNestedDeconstruction()
        => AssertFormatAsync("""
            class Class5
            {
                void bar()
                {
                    (int x1, var (x2, x3)) = (1, (2, 3));
                }
            }
            """, """
            class Class5{
            void bar()
            {
            ( int x1 , var( x2,x3 ) )=(1,(2,3));
            }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/32207")]
    public Task TupleExpression_SpaceAfterComma_False()
        => AssertFormatAsync("""
            class C
            {
                void M()
                {
                    var x = (1,2,3);
                }
            }
            """, """
            class C
            {
                void M()
                {
                    var x = (1, 2, 3);
                }
            }
            """,
            LanguageNames.CSharp,
            new(LanguageNames.CSharp) { { SpaceAfterComma, false } });

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/32207")]
    public Task TupleExpression_SpaceAfterComma_True()
        => AssertFormatAsync("""
            class C
            {
                void M()
                {
                    var x = (1, 2, 3);
                }
            }
            """, """
            class C
            {
                void M()
                {
                    var x = (1,2,3);
                }
            }
            """,
            LanguageNames.CSharp,
            new(LanguageNames.CSharp) { { SpaceAfterComma, true } });

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/32207")]
    public Task TupleType_SpaceAfterComma_False()
        => AssertFormatAsync("""
            class C
            {
                void M((int,string,bool) tuple)
                {
                }
            }
            """, """
            class C
            {
                void M((int, string, bool) tuple)
                {
                }
            }
            """,
            LanguageNames.CSharp,
            new(LanguageNames.CSharp) { { SpaceAfterComma, false } });

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/32207")]
    public Task TupleType_SpaceAfterComma_True()
        => AssertFormatAsync("""
            class C
            {
                void M((int, string, bool) tuple)
                {
                }
            }
            """, """
            class C
            {
                void M((int,string,bool) tuple)
                {
                }
            }
            """,
            LanguageNames.CSharp,
            new(LanguageNames.CSharp) { { SpaceAfterComma, true } });

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/32207")]
    public Task NestedTuples_SpaceAfterComma_False()
        => AssertFormatAsync("""
            class C
            {
                void M()
                {
                    var x = (1,(2,3),4);
                    (int,(string,bool)) y;
                }
            }
            """, """
            class C
            {
                void M()
                {
                    var x = (1, (2, 3), 4);
                    (int, (string, bool)) y;
                }
            }
            """,
            LanguageNames.CSharp,
            new(LanguageNames.CSharp) { { SpaceAfterComma, false } });

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/32207")]
    public Task Deconstruction_SpaceAfterComma_False()
        => AssertFormatAsync("""
            class C
            {
                void M()
                {
                    (int x,string y) = (1,"hello");
                    var (a,b) = (1,2);
                }
            }
            """, """
            class C
            {
                void M()
                {
                    (int x, string y) = (1, "hello");
                    var (a, b) = (1, 2);
                }
            }
            """,
            LanguageNames.CSharp,
            new(LanguageNames.CSharp) { { SpaceAfterComma, false } });

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/32207")]
    public Task CollectionExpression_SpaceBeforeComma()
        => AssertFormatAsync("""
            class C
            {
                void M()
                {
                    int[] x = [1 , 2 , 3];
                }
            }
            """, """
            class C
            {
                void M()
                {
                    int[] x = [1, 2, 3];
                }
            }
            """,
            LanguageNames.CSharp,
            new(LanguageNames.CSharp) { { SpaceBeforeComma, true } });

    [Fact]
    public Task SpacingInSuppressNullableWarningExpression()
        => AssertFormatAsync("""
            class C
            {
                static object F()
                {
                    object? o[] = null;
                    object? x = null;
                    object? y = null;
                    return x! ?? (y)! ?? o[0]!;
                }
            }
            """, """
            class C
            {
                static object F()
                {
                    object? o[] = null;
                    object? x = null;
                    object? y = null;
                    return x ! ?? (y) ! ?? o[0] !;
                }
            }
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545335")]
    public Task PreprocessorOnSameLine()
        => AssertFormatAsync("""
            class C
            {
            }#line default

            #line hidden
            """, """
            class C
            {
            }#line default

            #line hidden
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545626")]
    public Task ArraysInAttributes()
        => AssertFormatAsync("""
            [A(X = new int[] { 1 })]
            public class A : Attribute
            {
                public int[] X;
            }
            """, """
            [A(X = new int[] { 1 })]
            public class A : Attribute
            {
                public int[] X;
            }
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530580")]
    public Task NoNewLineAfterBraceInExpression()
        => AssertFormatAsync("""
            public class A
            {
                void Method()
                {
                    var po = cancellationToken.CanBeCanceled ?
                       new ParallelOptions() { CancellationToken = cancellationToken } :
                       defaultParallelOptions;
                }
            }
            """, """
            public class A
            {
                void Method()
                {
                     var po = cancellationToken.CanBeCanceled ?
                        new ParallelOptions() { CancellationToken = cancellationToken }     :
                        defaultParallelOptions;
                }
            }
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530580")]
    public Task NoIndentForNestedUsingWithoutBraces()
        => AssertFormatAsync("""
            class C
            {
                void M()
                {
                    using (null)
                    using (null)
                    {
                    }
                }
            }

            """, """
            class C
            {
            void M()
            {
            using (null)
            using (null)
            { 
            }
            }
            }

            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530580")]
    public Task NoIndentForNestedUsingWithoutBraces2()
        => AssertFormatAsync("""
            class C
            {
                void M()
                {
                    using (null)
                    using (null)
                    using (null)
                    {
                    }
                }
            }

            """, """
            class C
            {
                void M()
                {
                    using (null)
                        using (null)
                        using (null)
                        {
                        }
                }
            }

            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530580")]
    public Task NoIndentForNestedUsingWithoutBraces3()
        => AssertFormatAsync("""
            class C
            {
                void M()
                {
                    using (null)
                    using (null)
                    using (null)
                    {
                    }
                }
            }

            """, """
            class C
            {
                void M()
                {
                    using (null)
                        using (null)
                        using (null)
                    {
                    }
                }
            }

            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546678")]
    public Task UnicodeWhitespace()
        => AssertFormatAsync("", "\u001A");

    [Fact, WorkItem(17431, "DevDiv_Projects/Roslyn")]
    public Task NoElasticRuleOnRegularFile()
        => AssertFormatAsync("""
            class Consumer
            {
                public int P
                {
                }
            }
            """, """
            class Consumer
            {
                public int P
                {
                            }
            }
            """);

    [Fact, WorkItem(584599, "DevDiv_Projects/Roslyn")]
    public Task CaseSection()
        => AssertFormatAsync("""
            class C
            {
                void Method()
                {
                    switch (i)
                    {
                        // test1
                        case 1:
                        // test2
                        case 2:
                            // test3
                            int i2 = 10;
                        // test 4
                        case 4:
                            // test 5
                    }
                }
            }
            """, """
            class C
            {
                void Method()
                {
                    switch(i)
                    {
                                // test1
                        case 1:
                                            // test2
                        case 2:
                                        // test3
                            int i2 = 10;
                                    // test 4
                        case 4:
            // test 5
                    }
                }
            }
            """);

    [Fact, WorkItem(553654, "DevDiv_Projects/Roslyn")]
    public async Task Bugfix_553654_LabelStatementIndenting()
    {
        var changingOptions = new OptionsCollection(LanguageNames.CSharp)
        {
            { CSharpFormattingOptions2.LabelPositioning, LabelPositionOptions.LeftMost }
        };
        await AssertFormatAsync("""
            class Program
            {
                void F()
                {
                    foreach (var x in new int[] { })
                    {
            goo:
                        int a = 1;
                    }
                }
            }
            """, """
            class Program
            {
                void F()
                {
                    foreach (var x in new int[] { })
                    {
                        goo:
                        int a = 1;
                    }
                }
            }
            """, changingOptions);
    }

    [Fact, WorkItem(707064, "DevDiv_Projects/Roslyn")]
    public Task Bugfix_707064_SpaceAfterSecondSemiColonInFor()
        => AssertFormatAsync("""
            class Program
            {
                void F()
                {
                    for (int i = 0; i < 5;)
                    {
                    }
                }
            }
            """, """
            class Program
            {
                void F()
                {
                    for (int i = 0; i < 5;)
                    {
                    }
                }
            }
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/772313")]
    public Task Bugfix_772313_ReturnKeywordBeforeQueryClauseDoesNotTriggerNewLineOnFormat()
        => AssertFormatAsync("""
            class C
            {
                int M()
                {
                    return from c in "
                           select c;
                }
            }
            """, """
            class C
            {
                int M()
                {
                    return             from c in "
                           select c;
                }
            }
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/772304")]
    public Task Bugfix_772313_PreserveMethodParameterIndentWhenAttributePresent()
        => AssertFormatAsync("""
            class C
            {
                void M
                (
                    [In]
                              bool b
                );
            }

            class C
            {
                void M
                (
                    [In]
               List<bool> b
                );
            }

            class C
            {
                void M
                (
                    [In]
                            [In, In]
               List<bool> b
                );
            }
            """, """
            class C
            {
                void M
                (
                    [In]
                              bool b
                );
            }

            class C
            {
                void M
                (
                    [In]
               List<bool> b
                );
            }

            class C
            {
                void M
                (
                    [In]
                            [In, In]
               List<bool> b
                );
            }
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/776513")]
    public Task Bugfix_776513_CheckBraceIfNotMissingBeforeApplyingOperationForBracedBlocks()
        => AssertFormatAsync("""
            var alwaysTriggerList = new[]
                Dim triggerOnlyWithLettersList =
            """, """
            var alwaysTriggerList = new[]
                Dim triggerOnlyWithLettersList =
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/769342")]
    public async Task ShouldFormatDocCommentWithIndentSameAsTabSizeWithUseTabTrue()
    {
        var optionSet = new OptionsCollection(LanguageNames.CSharp) { { FormattingOptions2.UseTabs, true } };

        await AssertFormatAsync("""
            namespace ConsoleApplication1
            {
            	/// <summary>
            	/// fka;jsgdflkhsjflgkhdsl;
            	/// </summary>
            	class Program
            	{
            	}
            }
            """, """
            namespace ConsoleApplication1
            {
                /// <summary>
                /// fka;jsgdflkhsjflgkhdsl;
                /// </summary>
                class Program
                {
                }
            }
            """, changedOptionSet: optionSet);
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/797278")]
    public async Task TestSpacingOptionAroundControlFlow()
    {
        var optionSet = new OptionsCollection(LanguageNames.CSharp)
        {
            { SpaceBetweenParentheses, SpaceBetweenParentheses.DefaultValue.WithFlagValue( SpacePlacementWithinParentheses.ControlFlowStatements, true) },
        };

        await AssertFormatAsync("""

            class Program
            {
                public void goo()
                {
                    int i;
                    for ( i = 0; i < 10; i++ )
                    { }

                    foreach ( i in new[] { 1, 2, 3 } )
                    { }

                    if ( i == 10 )
                    { }

                    while ( i == 10 )
                    { }

                    switch ( i )
                    {
                        default: break;
                    }

                    do { } while ( true );

                    try
                    { }
                    catch ( System.Exception )
                    { }
                    catch ( System.Exception e ) when ( true )
                    { }

                    using ( somevar )
                    { }

                    lock ( somevar )
                    { }

                    fixed ( char* p = str )
                    { }
                }
            }
            """, """

            class Program
            {
                public void goo()
                {
                    int i;
                    for(i=0; i<10; i++)
                    {}

                    foreach(i in new[] {1,2,3})
                    {}

                    if (i==10)
                    {}

                    while(i==10)
                    {}

                    switch(i)
                    {
                        default: break;
                    }

                    do {} while (true);

                    try
                    { }
                    catch (System.Exception)
                    { }
                    catch (System.Exception e) when (true)
                    { }

                    using(somevar)
                    { }

                    lock(somevar)
                    { }

                    fixed(char* p = str)
                    { }
                }
            }
            """, changedOptionSet: optionSet);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/37031")]
    [WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/176345")]
    public async Task TestSpacingOptionAfterControlFlowKeyword()
    {
        var optionSet = new OptionsCollection(LanguageNames.CSharp) { { CSharpFormattingOptions2.SpaceAfterControlFlowStatementKeyword, false } };
        await AssertFormatAsync("""

            class Program
            {
                public void goo()
                {
                    int i;
                    for(i = 0; i < 10; i++)
                    { }

                    foreach(i in new[] { 1, 2, 3 })
                    { }

                    if(i == 10)
                    { }

                    while(i == 10)
                    { }

                    switch(i)
                    {
                        default: break;
                    }

                    do { } while(true);

                    try
                    { }
                    catch(System.Exception e) when(true)
                    { }

                    using(somevar)
                    { }

                    lock(somevar)
                    { }

                    fixed(somevar)
                    { }
                }
            }
            """, """

            class Program
            {
                public void goo()
                {
                    int i;
                    for (i=0; i<10; i++)
                    {}

                    foreach (i in new[] {1,2,3})
                    {}

                    if (i==10)
                    {}

                    while (i==10)
                    {}

                    switch (i)
                    {
                        default: break;
                    }

                    do {} while (true);

                    try
                    { }
                    catch (System.Exception e) when (true)
                    { }

                    using (somevar)
                    { }

                    lock (somevar)
                    { }

                    fixed (somevar)
                    { }
                }
            }
            """, changedOptionSet: optionSet);
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/766212")]
    public async Task TestOptionForSpacingAroundCommas()
    {
        var code = """

            class Program
            {
                public void Main()
                {
                    var a = new[] {1,2,3};
                    var digits = new List<int> {1,2,3,4};
                }
            }
            """;
        await AssertFormatAsync("""

            class Program
            {
                public void Main()
                {
                    var a = new[] { 1, 2, 3 };
                    var digits = new List<int> { 1, 2, 3, 4 };
                }
            }
            """, code);
        var optionSet = new OptionsCollection(LanguageNames.CSharp) { { CSharpFormattingOptions2.SpaceAfterComma, false } };
        await AssertFormatAsync("""

            class Program
            {
                public void Main()
                {
                    var a = new[] { 1,2,3 };
                    var digits = new List<int> { 1,2,3,4 };
                }
            }
            """, code, changedOptionSet: optionSet);
        optionSet.Add(CSharpFormattingOptions2.SpaceBeforeComma, true);
        await AssertFormatAsync("""

            class Program
            {
                public void Main()
                {
                    var a = new[] { 1 ,2 ,3 };
                    var digits = new List<int> { 1 ,2 ,3 ,4 };
                }
            }
            """, code, changedOptionSet: optionSet);
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/772308")]
    public Task Bugfix_772308_SeparateSuppressionForEachCaseLabelEvenIfEmpty()
        => AssertFormatAsync("""

            class C
            {
                int M()
                {
                    switch (1)
                    {
                        case 1: return 1;
                        case 2: return 2;
                        case 3:
                        case 4: return 4;
                        default:
                    }
                }
            }

            """, """

            class C
            {
                int M()
                {
                    switch (1)
                    {
                        case 1: return 1;
                        case 2: return 2;
                        case 3:
                        case 4:                           return 4;
                        default:
                    }
                }
            }

            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/844913")]
    public Task QueryExpressionInExpression()
        => AssertFormatAsync("""

            class C
            {
                public void CreateSettingsFile(string path, string comment)
                {
                    var xml = new XDocument(
                        new XDeclaration(1.0, utf8, yes),
                        new XComment(comment),
                        new XElement(UserSettings,
                            new XElement(ToolsOptions,
                                from t in KnownSettings.DefaultCategories
                                group t by t.Item1 into cat
                                select new XElement(ToolsOptionsCategory,
                                    new XAttribute(name, cat.Key),
                                    cat.Select(sc => new XElement(ToolsOptionsSubCategory, new XAttribute(name, sc.Item2)))
                                    )
                            )
                        )
                    );
                    UpdateSettingsXml(xml);
                    xml.Save(path);
                    SettingsPath = path;
                }
            }

            """, """

            class C
            {
                public void CreateSettingsFile(string path, string comment) {
            			var xml = new XDocument(
            				new XDeclaration(1.0, utf8, yes),
                            new XComment(comment),
            				new XElement(UserSettings,
                                new XElement(ToolsOptions,
                                    from t in KnownSettings.DefaultCategories
                        group t by t.Item1 into cat
                        select new XElement(ToolsOptionsCategory,
                        new XAttribute(name, cat.Key),
                        cat.Select(sc => new XElement(ToolsOptionsSubCategory, new XAttribute(name, sc.Item2)))
                        )
                                )
                            )
            			);
                        UpdateSettingsXml(xml);
                        xml.Save(path);
                        SettingsPath = path;
                    }
                }

            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/843479")]
    public async Task EmbeddedStatementElse()
    {
        var changingOptions = new OptionsCollection(LanguageNames.CSharp)
        {
            { CSharpFormattingOptions2.NewLineForElse, false }
        };
        await AssertFormatAsync("""

            class C
            {
                void Method()
                {
                    if (true)
                        Console.WriteLine();
                    else
                        Console.WriteLine();
                }
            }

            """, """

            class C
            {
                void Method()
                {
                    if (true)
                            Console.WriteLine();              else
                            Console.WriteLine();
                }
            }

            """, changingOptions);
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/772311")]
    public Task LineCommentAtTheEndOfLine()
        => AssertFormatAsync("""

            using System;

            class Program
            {
                static void Main(string[] args)
                {
                    Console.WriteLine(); // this is a comment
                                         // that I would like to keep

                    // properly indented
                }
            }

            """, """

            using System;

            class Program
            {
                static void Main(string[] args)
                {
                    Console.WriteLine(); // this is a comment
                                                    // that I would like to keep

                            // properly indented
                }
            }

            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/38224")]
    public Task BlockCommentAtTheEndOfLine1()
        => AssertFormatAsync("""

            using System;

            class Program
            {
                static void Main(string[] args)
                {
                    Console.WriteLine(); /* this is a comment */
                    // that I would like to keep

                    // properly indented
                }
            }

            """, """

            using System;

            class Program
            {
                static void Main(string[] args)
                {
                    Console.WriteLine(); /* this is a comment */
                                                    // that I would like to keep

                            // properly indented
                }
            }

            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/38224")]
    public Task BlockCommentAtTheEndOfLine2()
        => AssertFormatAsync("""

            using System;

            class Program
            {
                static void Main(string[] args)
                {
                    Console.WriteLine(); // this is a comment
                    /* that I would like to keep */

                    // properly indented
                }
            }

            """, """

            using System;

            class Program
            {
                static void Main(string[] args)
                {
                    Console.WriteLine(); // this is a comment
                                                    /* that I would like to keep */

                            // properly indented
                }
            }

            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/38224")]
    public async Task BlockCommentAtBeginningOfLine()
    {
        var code = """

            using System;

            class Program
            {
                static void Main(
                    int x, // Some comment
                    /*A*/ int y)
                {
                }
            }

            """;
        await AssertFormatAsync(code, code);
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/772311")]
    public async Task TestTab()
    {
        var expected = """

            using System;

            class Program
            {
            	/// <summary>
            	/// This function is the callback used to execute a command when a menu item is clicked.
            	/// See the Initialize method to see how the menu item is associated to this function using
            	/// the OleMenuCommandService service and the MenuCommand class.
            	/// </summary>
            	private void MenuItemCallback(object sender, EventArgs e)
            	{
            		// Show a Message Box to prove we were here
            		IVsUIShell uiShell = (IVsUIShell)GetService(typeof(SVsUIShell));
            		Guid clsid = Guid.Empty;
            		int result;
            		Microsoft.VisualStudio.ErrorHandler.ThrowOnFailure(uiShell.ShowMessageBox(
            				   0,
            				   ref clsid,
            				   Rebracer,
            				   string.Format(CultureInfo.CurrentCulture, Inside { 0}.MenuItemCallback(), this.ToString()),
            					   string.Empty,
            					   0,
            					   OLEMSGBUTTON.OLEMSGBUTTON_OK,
            					   OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST,
            					   OLEMSGICON.OLEMSGICON_INFO,
            					   0,        // false
            					   out result));
            	}
            }

            """;
        var optionSet = new OptionsCollection(LanguageNames.CSharp) { { FormattingOptions2.UseTabs, true } };

        await AssertFormatAsync(expected, """

            using System;

            class Program
            {
            		/// <summary>
            		/// This function is the callback used to execute a command when a menu item is clicked.
            		/// See the Initialize method to see how the menu item is associated to this function using
            		/// the OleMenuCommandService service and the MenuCommand class.
            		/// </summary>
            		private void MenuItemCallback(object sender, EventArgs e) {
            			// Show a Message Box to prove we were here
            			IVsUIShell uiShell = (IVsUIShell)GetService(typeof(SVsUIShell));
            			Guid clsid = Guid.Empty;
            			int result;
            			Microsoft.VisualStudio.ErrorHandler.ThrowOnFailure(uiShell.ShowMessageBox(
            					   0,
            					   ref clsid,
            					   Rebracer,
                                   string.Format(CultureInfo.CurrentCulture, Inside {0}.MenuItemCallback(), this.ToString()),
            					   string.Empty,
            					   0,
            					   OLEMSGBUTTON.OLEMSGBUTTON_OK,
            					   OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST,
            					   OLEMSGICON.OLEMSGICON_INFO,
            					   0,        // false
            					   out result));
                    }
                }

            """, changedOptionSet: optionSet);
        await AssertFormatAsync(expected, expected, changedOptionSet: optionSet);
    }

    [Fact]
    public async Task LeaveBlockSingleLine_False()
    {
        var options = new OptionsCollection(LanguageNames.CSharp) { { CSharpFormattingOptions2.WrappingPreserveSingleLine, false } };
        await AssertFormatAsync("""

            namespace N
            {
                class C
                {
                    int x;
                }
            }
            """, """

            namespace N { class C { int x; } }
            """, changedOptionSet: options);
    }

    [Fact]
    public async Task LeaveBlockSingleLine_False2()
    {
        var options = new OptionsCollection(LanguageNames.CSharp) { { CSharpFormattingOptions2.WrappingPreserveSingleLine, false } };
        await AssertFormatAsync("""

            class C
            {
                void goo()
                {
                }
            }
            """, """

            class C { void goo() { } }
            """, changedOptionSet: options);
    }

    [Fact]
    public async Task LeaveStatementMethodDeclarationSameLine_False()
    {
        var options = new OptionsCollection(LanguageNames.CSharp) { { CSharpFormattingOptions2.WrappingKeepStatementsOnSingleLine, false } };
        await AssertFormatAsync("""

            class Program
            {
                void goo()
                {
                    int x = 0;
                    int y = 0;
                }
            }
            """, """

            class Program
            {
                void goo()
                {
                    int x = 0; int y = 0;
                }
            }
            """, changedOptionSet: options);
    }

    [Fact]
    public async Task SpaceWithinEmptyBracketPrecedencesSpaceBeforeOrAfterComma_0000()
    {
        var options = new OptionsCollection(LanguageNames.CSharp)
        {
            { SpaceBetweenEmptySquareBrackets, false },
            { SpaceWithinSquareBrackets, false },
            { SpaceBeforeComma, false },
            { SpaceAfterComma, false },
        };
        await AssertFormatAsync("""

            class Program
            {
                int[] x;
                int[,] y;
                int[,,] z = new int[1,2,3];
                var a = new[] { 0 };
            }
            """, """

            class Program
            {
                int[ ] x;
                int[ , ] y;
                int[ , , ] z = new int[1,2,3];
                var a = new[ ] { 0 };
            }
            """, changedOptionSet: options);
    }

    [Fact]
    public async Task SpaceWithinEmptyBracketPrecedencesSpaceBeforeOrAfterComma_0001()
    {
        var options = new OptionsCollection(LanguageNames.CSharp)
        {
            { SpaceBetweenEmptySquareBrackets, false },
            { SpaceWithinSquareBrackets, false },
            { SpaceBeforeComma, false },
            { SpaceAfterComma, true },
        };
        await AssertFormatAsync("""

            class Program
            {
                int[] x;
                int[,] y;
                int[,,] z = new int[1, 2, 3];
                var a = new[] { 0 };
            }
            """, """

            class Program
            {
                int[ ] x;
                int[ , ] y;
                int[ , , ] z = new int[1,2,3];
                var a = new[ ] { 0 };
            }
            """, changedOptionSet: options);
    }

    [Fact]
    public async Task SpaceWithinEmptyBracketPrecedencesSpaceBeforeOrAfterComma_0010()
    {
        var options = new OptionsCollection(LanguageNames.CSharp)
        {
            { SpaceBetweenEmptySquareBrackets, false },
            { SpaceWithinSquareBrackets, false },
            { SpaceBeforeComma, true },
            { SpaceAfterComma, false },
        };
        await AssertFormatAsync("""

            class Program
            {
                int[] x;
                int[,] y;
                int[,,] z = new int[1 ,2 ,3];
                var a = new[] { 0 };
            }
            """, """

            class Program
            {
                int[ ] x;
                int[ , ] y;
                int[ , , ] z = new int[1,2,3];
                var a = new[ ] { 0 };
            }
            """, changedOptionSet: options);
    }

    [Fact]
    public async Task SpaceWithinEmptyBracketPrecedencesSpaceBeforeOrAfterComma_0011()
    {
        var options = new OptionsCollection(LanguageNames.CSharp)
        {
            { SpaceBetweenEmptySquareBrackets, false },
            { SpaceWithinSquareBrackets, false },
            { SpaceBeforeComma, true },
            { SpaceAfterComma, true },
        };
        await AssertFormatAsync("""

            class Program
            {
                int[] x;
                int[,] y;
                int[,,] z = new int[1 , 2 , 3];
                var a = new[] { 0 };
            }
            """, """

            class Program
            {
                int[ ] x;
                int[ , ] y;
                int[ , , ] z = new int[1,2,3];
                var a = new[ ] { 0 };
            }
            """, changedOptionSet: options);
    }

    [Fact]
    public async Task SpaceWithinEmptyBracketPrecedencesSpaceBeforeOrAfterComma_0100()
    {
        var options = new OptionsCollection(LanguageNames.CSharp)
        {
            { SpaceBetweenEmptySquareBrackets, false },
            { SpaceWithinSquareBrackets, true },
            { SpaceBeforeComma, false },
            { SpaceAfterComma, false },
        };
        await AssertFormatAsync("""

            class Program
            {
                int[] x;
                int[,] y;
                int[,,] z = new int[ 1,2,3 ];
                var a = new[] { 0 };
            }
            """, """

            class Program
            {
                int[ ] x;
                int[, ] y;
                int[, , ] z = new int[1,2,3];
                var a = new[ ] { 0 };
            }
            """, changedOptionSet: options);
    }

    [Fact]
    public async Task SpaceWithinEmptyBracketPrecedencesSpaceBeforeOrAfterComma_0101()
    {
        var options = new OptionsCollection(LanguageNames.CSharp)
        {
            { SpaceBetweenEmptySquareBrackets, false },
            { SpaceWithinSquareBrackets, true },
            { SpaceBeforeComma, false },
            { SpaceAfterComma, true },
        };
        await AssertFormatAsync("""

            class Program
            {
                int[] x;
                int[,] y;
                int[,,] z = new int[ 1, 2, 3 ];
                var a = new[] { 0 };
            }
            """, """

            class Program
            {
                int[ ] x;
                int[, ] y;
                int[, , ] z = new int[1,2,3];
                var a = new[ ] { 0 };
            }
            """, changedOptionSet: options);
    }

    [Fact]
    public async Task SpaceWithinEmptyBracketPrecedencesSpaceBeforeOrAfterComma_0110()
    {
        var options = new OptionsCollection(LanguageNames.CSharp)
        {
            { SpaceBetweenEmptySquareBrackets, false },
            { SpaceWithinSquareBrackets, true },
            { SpaceBeforeComma, true },
            { SpaceAfterComma, false },
        };
        await AssertFormatAsync("""

            class Program
            {
                int[] x;
                int[,] y;
                int[,,] z = new int[ 1 ,2 ,3 ];
                var a = new[] { 0 };
            }
            """, """

            class Program
            {
                int[ ] x;
                int[, ] y;
                int[, , ] z = new int[1,2,3];
                var a = new[ ] { 0 };
            }
            """, changedOptionSet: options);
    }

    [Fact]
    public async Task SpaceWithinEmptyBracketPrecedencesSpaceBeforeOrAfterComma_0111()
    {
        var options = new OptionsCollection(LanguageNames.CSharp)
        {
            { SpaceBetweenEmptySquareBrackets, false },
            { SpaceWithinSquareBrackets, true },
            { SpaceBeforeComma, true },
            { SpaceAfterComma, true },
        };
        await AssertFormatAsync("""

            class Program
            {
                int[] x;
                int[,] y;
                int[,,] z = new int[ 1 , 2 , 3 ];
                var a = new[] { 0 };
            }
            """, """

            class Program
            {
                int[ ] x;
                int[, ] y;
                int[, , ] z = new int[1,2,3];
                var a = new[ ] { 0 };
            }
            """, changedOptionSet: options);
    }

    [Fact]
    public async Task SpaceWithinEmptyBracketPrecedencesSpaceBeforeOrAfterComma_1000()
    {
        var options = new OptionsCollection(LanguageNames.CSharp)
        {
            { SpaceBetweenEmptySquareBrackets, true },
            { SpaceWithinSquareBrackets, false },
            { SpaceBeforeComma, false },
            { SpaceAfterComma, false },
        };
        await AssertFormatAsync("""

            class Program
            {
                int[ ] x;
                int[ , ] y;
                int[ , , ] z = new int[1,2,3];
                var a = new[ ] { 0 };
            }
            """, """

            class Program
            {
                int[] x;
                int[ ,] y;
                int[ , ,] z = new int[1,2,3];
                var a = new[] { 0 };
            }
            """, changedOptionSet: options);
    }

    [Fact]
    public async Task SpaceWithinEmptyBracketPrecedencesSpaceBeforeOrAfterComma_1001()
    {
        var options = new OptionsCollection(LanguageNames.CSharp)
        {
            { SpaceBetweenEmptySquareBrackets, true },
            { SpaceWithinSquareBrackets, false },
            { SpaceBeforeComma, false },
            { SpaceAfterComma, true },
        };
        await AssertFormatAsync("""

            class Program
            {
                int[ ] x;
                int[ , ] y;
                int[ , , ] z = new int[1, 2, 3];
                var a = new[ ] { 0 };
            }
            """, """

            class Program
            {
                int[] x;
                int[ ,] y;
                int[ , ,] z = new int[1,2,3];
                var a = new[] { 0 };
            }
            """, changedOptionSet: options);
    }

    [Fact]
    public async Task SpaceWithinEmptyBracketPrecedencesSpaceBeforeOrAfterComma_1010()
    {
        var options = new OptionsCollection(LanguageNames.CSharp)
        {
            { SpaceBetweenEmptySquareBrackets, true },
            { SpaceWithinSquareBrackets, false },
            { SpaceBeforeComma, true },
            { SpaceAfterComma, false },
        };
        await AssertFormatAsync("""

            class Program
            {
                int[ ] x;
                int[ , ] y;
                int[ , , ] z = new int[1 ,2 ,3];
                var a = new[ ] { 0 };
            }
            """, """

            class Program
            {
                int[] x;
                int[ ,] y;
                int[ , ,] z = new int[1,2,3];
                var a = new[] { 0 };
            }
            """, changedOptionSet: options);
    }

    [Fact]
    public async Task SpaceWithinEmptyBracketPrecedencesSpaceBeforeOrAfterComma_1011()
    {
        var options = new OptionsCollection(LanguageNames.CSharp)
        {
            { SpaceBetweenEmptySquareBrackets, true },
            { SpaceWithinSquareBrackets, false },
            { SpaceBeforeComma, true },
            { SpaceAfterComma, true },
        };
        await AssertFormatAsync("""

            class Program
            {
                int[ ] x;
                int[ , ] y;
                int[ , , ] z = new int[1 , 2 , 3];
                var a = new[ ] { 0 };
            }
            """, """

            class Program
            {
                int[] x;
                int[ ,] y;
                int[ , ,] z = new int[1,2,3];
                var a = new[] { 0 };
            }
            """, changedOptionSet: options);
    }

    [Fact]
    public async Task SpaceWithinEmptyBracketPrecedencesSpaceBeforeOrAfterComma_1100()
    {
        var options = new OptionsCollection(LanguageNames.CSharp)
        {
            { SpaceBetweenEmptySquareBrackets, true },
            { SpaceWithinSquareBrackets, true },
            { SpaceBeforeComma, false },
            { SpaceAfterComma, false },
        };
        await AssertFormatAsync("""

            class Program
            {
                int[ ] x;
                int[ , ] y;
                int[ , , ] z = new int[ 1,2,3 ];
                var a = new[ ] { 0 };
            }
            """, """

            class Program
            {
                int[ ] x;
                int[ , ] y;
                int[ , , ] z = new int[1,2,3];
                var a = new[ ] { 0 };
            }
            """, changedOptionSet: options);
    }

    [Fact]
    public async Task SpaceWithinEmptyBracketPrecedencesSpaceBeforeOrAfterComma_1101()
    {
        var options = new OptionsCollection(LanguageNames.CSharp)
        {
            { SpaceBetweenEmptySquareBrackets, true },
            { SpaceWithinSquareBrackets, true },
            { SpaceBeforeComma, false },
            { SpaceAfterComma, true },
        };
        await AssertFormatAsync("""

            class Program
            {
                int[ ] x;
                int[ , ] y;
                int[ , , ] z = new int[ 1, 2, 3 ];
                var a = new[ ] { 0 };
            }
            """, """

            class Program
            {
                int[ ] x;
                int[ , ] y;
                int[ , , ] z = new int[1,2,3];
                var a = new[ ] { 0 };
            }
            """, changedOptionSet: options);
    }

    [Fact]
    public async Task SpaceWithinEmptyBracketPrecedencesSpaceBeforeOrAfterComma_1110()
    {
        var options = new OptionsCollection(LanguageNames.CSharp)
        {
            { SpaceBetweenEmptySquareBrackets, true },
            { SpaceWithinSquareBrackets, true },
            { SpaceBeforeComma, true },
            { SpaceAfterComma, false },
        };
        await AssertFormatAsync("""

            class Program
            {
                int[ ] x;
                int[ , ] y;
                int[ , , ] z = new int[ 1 ,2 ,3 ];
                var a = new[ ] { 0 };
            }
            """, """

            class Program
            {
                int[ ] x;
                int[ , ] y;
                int[ , , ] z = new int[1,2,3];
                var a = new[ ] { 0 };
            }
            """, changedOptionSet: options);
    }

    [Fact]
    public async Task SpaceWithinEmptyBracketPrecedencesSpaceBeforeOrAfterComma_1111()
    {
        var options = new OptionsCollection(LanguageNames.CSharp)
        {
            { SpaceBetweenEmptySquareBrackets, true },
            { SpaceWithinSquareBrackets, true },
            { SpaceBeforeComma, true },
            { SpaceAfterComma, true },
        };
        await AssertFormatAsync("""

            class Program
            {
                int[ ] x;
                int[ , ] y;
                int[ , , ] z = new int[ 1 , 2 , 3 ];
                var a = new[ ] { 0 };
            }
            """, """

            class Program
            {
                int[ ] x;
                int[ , ] y;
                int[ , , ] z = new int[1,2,3];
                var a = new[ ] { 0 };
            }
            """, changedOptionSet: options);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/14128")]
    public async Task SpaceBeforeCommasInLocalFunctionParameters()
    {
        var options = new OptionsCollection(LanguageNames.CSharp)
        {
            { SpaceBeforeComma, true },
        };
        await AssertFormatAsync("""

            class Program
            {
                void Goo()
                {
                    void LocalFunction(int i , string s)
                    {
                    }
                }
            }
            """, """

            class Program
            {
                void Goo()
                {
                    void LocalFunction(int i, string s)
                    {
                    }
                }
            }
            """, changedOptionSet: options);
    }

    [Fact]
    public async Task ArrayDeclarationShouldFollowEmptySquareBrackets()
    {
        var options = new OptionsCollection(LanguageNames.CSharp)
        {
            { CSharpFormattingOptions2.SpaceWithinSquareBrackets, true },
            { CSharpFormattingOptions2.SpaceBetweenEmptySquareBrackets, false }
        };
        await AssertFormatAsync("""

            class Program
            {
                var t = new Goo(new[] { "a", "b" });
            }
            """, """

            class Program
            {
               var t = new Goo(new[ ] { "a", "b" });
            }
            """, changedOptionSet: options);
    }

    [Fact]
    public async Task SquareBracesBefore_True()
    {
        var options = new OptionsCollection(LanguageNames.CSharp) { { CSharpFormattingOptions2.SpaceBeforeOpenSquareBracket, true } };
        await AssertFormatAsync("""

            class Program
            {
                int [] x;
            }
            """, """

            class Program
            {
                int[] x;
            }
            """, changedOptionSet: options);
    }

    [Fact]
    public async Task SquareBracesAndValue_True()
    {
        var options = new OptionsCollection(LanguageNames.CSharp) { { CSharpFormattingOptions2.SpaceWithinSquareBrackets, true } };
        await AssertFormatAsync("""

            class Program
            {
                int[ 3 ] x;
            }
            """, """

            class Program
            {
                int[3] x;
            }
            """, changedOptionSet: options);
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/917351")]
    public async Task TestLockStatement()
    {
        var options = new OptionsCollection(LanguageNames.CSharp)
        {
            { NewLineBeforeOpenBrace, NewLineBeforeOpenBrace.DefaultValue.WithFlagValue(NewLineBeforeOpenBracePlacement.ControlBlocks, false) }
        };

        await AssertFormatAsync("""

            class Program
            {
                public void Method()
                {
                    lock (expression) {
                        // goo
                    }
                }
            }
            """, """

            class Program
            {
                public void Method()
                {
                    lock (expression)
                        {
                    // goo
            }
                }
            }
            """, changedOptionSet: options);
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/962416")]
    public async Task TestCheckedAndUncheckedStatement()
    {
        var options = new OptionsCollection(LanguageNames.CSharp)
        {
            { NewLineBeforeOpenBrace , NewLineBeforeOpenBrace.DefaultValue.WithFlagValue(NewLineBeforeOpenBracePlacement.ControlBlocks, false) }
        };

        await AssertFormatAsync("""

            class Program
            {
                public void Method()
                {
                    checked {
                        // goo
                    }
                    unchecked {
                    }
                }
            }
            """, """

            class Program
            {
                public void Method()
                {
                    checked
                        {
                    // goo
            }
                        unchecked 
                                {
                        }
                }
            }
            """, changedOptionSet: options);
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/953535")]
    public async Task ConditionalMemberAccess()
    {
        var parseOptions = new CSharpParseOptions();
        await AssertFormatAsync("""

            using System;
            class A
            {
                public A a;
            }

            class Program
            {
                static void Main(string[] args)
                {
                    A a = null;
                    A?.a = null;
                    System.Console.WriteLine(args?[0]);
                    System.Console.WriteLine(args?.Length);
                }
            }
            """, """

            using System;
            class A
            {
                public A a;
            }

            class Program
            {
                static void Main(string[] args)
                {
                    A a = null;
                    A         ?.a = null;
                    System.Console.WriteLine(args       ?[0]);
                    System.Console.WriteLine(args                   ?.Length);
                }
            }
            """, parseOptions: parseOptions);
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/924172")]
    public async Task IgnoreSpacesInDeclarationStatementEnabled()
    {
        var changingOptions = new OptionsCollection(LanguageNames.CSharp)
        {
            { CSharpFormattingOptions2.SpacesIgnoreAroundVariableDeclaration, true }
        };
        await AssertFormatAsync("""

            class Program
            {
                static void Main(string[] args)
                {
                    int       s;
                }
            }
            """, """

            class Program
            {
                static void Main(string[] args)
                {
                    int       s;
                }
            }
            """, changedOptionSet: changingOptions);
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/899492")]
    public Task CommentIsLeadingTriviaOfStatementNotLabel()
        => AssertFormatAsync("""

            class C
            {
                void M()
                {
                label:
                    // comment
                    M();
                    M();
                }
            }
            """, """

            class C
            {
                void M()
                {
                label:
                    // comment
                    M();
                    M();
                }
            }
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/991547")]
    public Task DoNotWrappingTryCatchFinallyIfOnSingleLine()
        => AssertFormatAsync("""

            class C
            {
                void M()
                {
                    try { }
                    catch { }
                    finally { }
                }
            }
            """, """

            class C
            {
                void M()
                {
                    try { }
                    catch { }
                    finally { }
                }
            }
            """);

    [Fact]
    public Task InterpolatedStrings1()
        => AssertFormatAsync("""

            class C
            {
                void M()
                {
                    var a = "World";
                    var b = $"Hello, {a}";
                }
            }
            """, """

            class C
            {
                void M()
                {
                    var a = "World";
                    var b = $"Hello, {a}";
                }
            }
            """);

    [Fact]
    public Task InterpolatedStrings2()
        => AssertFormatAsync("""

            class C
            {
                void M()
                {
                    var a = "Hello";
                    var b = "World";
                    var c = $"{a}, {b}";
                }
            }
            """, """

            class C
            {
                void M()
                {
                    var a = "Hello";
                    var b = "World";
                    var c = $"{a}, {b}";
                }
            }
            """);

    [Fact]
    public Task InterpolatedStrings3()
        => AssertFormatAsync("""

            class C
            {
                void M()
                {
                    var a = "World";
                    var b = $"Hello, {a}";
                }
            }
            """, """

            class C
            {
                void M()
                {
                    var a = "World";
                    var b = $"Hello, { a }";
                }
            }
            """);

    [Fact]
    public Task InterpolatedRawStrings3()
        => AssertFormatAsync(""""

            class C
            {
                void M()
                {
                    var a = "World";
                    var b = $"""Hello, {a}""";
                }
            }
            """", """"

            class C
            {
                void M()
                {
                    var a = "World";
                    var b = $"""Hello, { a }""";
                }
            }
            """");

    [Fact]
    public Task InterpolatedStrings4()
        => AssertFormatAsync("""

            class C
            {
                void M()
                {
                    var a = "Hello";
                    var b = "World";
                    var c = $"{a}, {b}";
                }
            }
            """, """

            class C
            {
                void M()
                {
                    var a = "Hello";
                    var b = "World";
                    var c = $"{ a }, { b }";
                }
            }
            """);

    [Fact]
    public Task InterpolatedStrings5()
        => AssertFormatAsync("""

            class C
            {
                void M()
                {
                    var a = "World";
                    var b = $@"Hello, {a}";
                }
            }
            """, """

            class C
            {
                void M()
                {
                    var a = "World";
                    var b = $@"Hello, {a}";
                }
            }
            """);

    [Fact]
    public Task InterpolatedStrings6()
        => AssertFormatAsync("""

            class C
            {
                void M()
                {
                    var a = "Hello";
                    var b = "World";
                    var c = $@"{a}, {b}";
                }
            }
            """, """

            class C
            {
                void M()
                {
                    var a = "Hello";
                    var b = "World";
                    var c = $@"{a}, {b}";
                }
            }
            """);

    [Fact]
    public Task InterpolatedStrings7()
        => AssertFormatAsync("""

            class C
            {
                void M()
                {
                    var a = "World";
                    var b = $@"Hello, {a}";
                }
            }
            """, """

            class C
            {
                void M()
                {
                    var a = "World";
                    var b = $@"Hello, { a }";
                }
            }
            """);

    [Fact]
    public Task InterpolatedStrings8()
        => AssertFormatAsync("""

            class C
            {
                void M()
                {
                    var a = "Hello";
                    var b = "World";
                    var c = $@"{a}, {b}";
                }
            }
            """, """

            class C
            {
                void M()
                {
                    var a = "Hello";
                    var b = "World";
                    var c = $@"{ a }, { b }";
                }
            }
            """);

    [Fact]
    public Task InterpolatedStrings9()
        => AssertFormatAsync("""

            class C
            {
                void M()
                {
                    var a = "Hello";
                    var c = $"{a}, World";
                }
            }
            """, """

            class C
            {
                void M()
                {
                    var a = "Hello";
                    var c = $"{ a }, World";
                }
            }
            """);

    [Fact]
    public Task InterpolatedStrings10()
        => AssertFormatAsync("""

            class C
            {
                void M()
                {
                    var s = $"{42,-4:x}";
                }
            }
            """, """

            class C
            {
                void M()
                {
                    var s = $"{42 , -4 :x}";
                }
            }
            """);

    [Fact]
    public Task InterpolatedRawStrings10()
        => AssertFormatAsync(""""

            class C
            {
                void M()
                {
                    var s = $"""{42,-4:x}""";
                }
            }
            """", """"

            class C
            {
                void M()
                {
                    var s = $"""{42 , -4 :x}""";
                }
            }
            """");

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/59811")]
    public Task InterpolatedStrings11()
        => AssertFormatAsync("""

            class C
            {
                void M()
                {
                    var hostAddress = "host";
                    var nasTypeId = "nas";
                    var version = "1.2";
                    var c = $"{hostAddress ?? ""}/{nasTypeId}/{version ?? ""}";
                }
            }
            """, """

            class C
            {
                void M()
                {
                    var hostAddress = "host";
                    var nasTypeId = "nas";
                    var version = "1.2";
                    var c = $"{      hostAddress?? ""}/{nasTypeId   }/{version??""}";
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/59811")]
    public Task InterpolatedStrings12()
        => AssertFormatAsync("""

            class C
            {
                void M()
                {
                    var a = 1.2M;
                    var c = $"{a: 000.00 }";
                }
            }
            """, """

            class C
            {
                void M()
                {
                    var a = 1.2M;
                    var c = $"{   a : 000.00 }";
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/59811")]
    public Task InterpolatedStrings13()
        => AssertFormatAsync("""

            class C
            {
                void M()
                {
                    var a = 1.2M;
                    var c = $"{(a > 2 ? "a" : "b"}";
                }
            }
            """, """

            class C
            {
                void M()
                {
                    var a = 1.2M;
                    var c = $"{ (a > 2?"a":"b"}";
                }
            }
            """);

    [Fact]
    public Task InterpolatedStrings14()
        => AssertFormatAsync("""

            class C
            {
                void M()
                {
                    var s = $"{42,-4:x}";
                }
            }
            """, """

            class C
            {
                void M()
                {
                    var s = $"{ 42 , -4 :x}";
                }
            }
            """);

    [Fact]
    public Task InterpolatedStrings15()
        => AssertFormatAsync("""

            class C
            {
                void M()
                {
                    var s = $"{42,-4}";
                }
            }
            """, """

            class C
            {
                void M()
                {
                    var s = $"{   42 , -4   }";
                }
            }
            """);

    [Fact]
    public Task InterpolatedStrings16()
        => AssertFormatAsync("""

            class C
            {
                void M()
                {
                    var s = $"{42,-4: x }";
                }
            }
            """, """

            class C
            {
                void M()
                {
                    var s = $"{  42 , -4 : x }";
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/1151")]
    [WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1041787")]
    public async Task ReconstructWhitespaceStringUsingTabs_SingleLineComment()
    {
        var optionSet = new OptionsCollection(LanguageNames.CSharp) { { FormattingOptions2.UseTabs, true } };
        await AssertFormatAsync("""
            using System;

            class Program
            {
            	static void Main(string[] args)
            	{
            		Console.WriteLine("");        // GooBar
            	}
            }
            """, """
            using System;

            class Program
            {
                static void Main(string[] args)
                {
                    Console.WriteLine("");        // GooBar
                }
            }
            """, optionSet);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/1151")]
    [WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/961559")]
    [WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1041787")]
    public async Task ReconstructWhitespaceStringUsingTabs_MultiLineComment()
    {
        var optionSet = new OptionsCollection(LanguageNames.CSharp) { { FormattingOptions2.UseTabs, true } };
        await AssertFormatAsync("""
            using System;

            class Program
            {
            	static void Main(string[] args)
            	{
            		Console.WriteLine("");        /* GooBar */
            	}
            }
            """, """
            using System;

            class Program
            {
                static void Main(string[] args)
                {
                    Console.WriteLine("");        /* GooBar */
                }
            }
            """, optionSet);
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1100920")]
    public Task NoLineOperationAroundInterpolationSyntax()
        => AssertFormatAsync("""
            class Program
            {
                static string F(int a, int b, int c)
                {
                    return $"{a} (index: 0x{b}, size: {c}): "
                }
            }
            """, """
            class Program
            {
                static string F(int a, int b, int c)
                {
                    return $"{a} (index: 0x{ b}, size: { c}): "
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/62")]
    public Task SpaceAfterWhenInExceptionFilter()
        => AssertFormatAsync("""
            class C
            {
                void M()
                {
                    try
                    {
                        if (x)
                        {
                            G();
                        }
                    }
                    catch (Exception e) when (H(e))
                    {

                    }
                }
            }
            """, """
            class C
            {
                void M()
                {
                    try
                    {
                        if(x){
                            G();
                        }
                    }
                    catch(Exception e) when (H(e))
                    {

                    }
                }
            }
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1089196")]
    [WorkItem("https://github.com/dotnet/roslyn/issues/285")]
    public Task FormatHashInBadDirectiveToZeroColumnAnywhereInsideIfDef()
        => AssertFormatAsync("""
            class MyClass
            {
                static void Main(string[] args)
                {
            #if false

            #

            #endif
                }
            }
            """, """
            class MyClass
            {
                static void Main(string[] args)
                {
            #if false

                        #

            #endif
                }
            }
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1089196")]
    [WorkItem("https://github.com/dotnet/roslyn/issues/285")]
    public Task FormatHashElseToZeroColumnAnywhereInsideIfDef()
        => AssertFormatAsync("""
            class MyClass
            {
                static void Main(string[] args)
                {
            #if false

            #else
                    Appropriate indentation should be here though #
            #endif
                }
            }
            """, """
            class MyClass
            {
                static void Main(string[] args)
                {
            #if false

                        #else
                    Appropriate indentation should be here though #
            #endif
                }
            }
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1089196")]
    [WorkItem("https://github.com/dotnet/roslyn/issues/285")]
    public Task FormatHashsToZeroColumnAnywhereInsideIfDef()
        => AssertFormatAsync("""
            class MyClass
            {
                static void Main(string[] args)
                {
            #if false

            #else
            #

            #endif
                }
            }
            """, """
            class MyClass
            {
                static void Main(string[] args)
                {
            #if false

                        #else
                    #

            #endif
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/1118")]
    public void DoNotAssumeCertainNodeAreAlwaysParented()
    {
        var block = SyntaxFactory.Block();
        Formatter.Format(block, new AdhocWorkspace().Services.SolutionServices, CSharpSyntaxFormattingOptions.Default, CancellationToken.None);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/776")]
    public async Task SpacingRulesAroundMethodCallAndParenthesisAppliedInAttributeNonDefault()
    {
        var changingOptions = new OptionsCollection(LanguageNames.CSharp)
        {
            { CSharpFormattingOptions2.SpaceAfterMethodCallName, true },
            { CSharpFormattingOptions2.SpaceBetweenEmptyMethodCallParentheses, true },
            { CSharpFormattingOptions2.SpaceWithinMethodCallParentheses, true }
        };
        await AssertFormatAsync("""
            [Obsolete ( "Test" ), Obsolete ( )]
            class Program
            {
                static void Main(string[] args)
                {
                }
            }
            """, """
            [Obsolete("Test"), Obsolete()]
            class Program
            {
                static void Main(string[] args)
                {
                }
            }
            """, changingOptions);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/776")]
    public async Task SpacingRulesAroundMethodCallAndParenthesisAppliedInAttribute()
    {
        var code = """
            [Obsolete("Test"), Obsolete()]
            class Program
            {
                static void Main(string[] args)
                {
                }
            }
            """;
        await AssertFormatAsync(code, code);
    }

    [Fact]
    public async Task SpacingInMethodCallArguments_True()
    {
        var optionSet = new OptionsCollection(LanguageNames.CSharp)
        {
            { CSharpFormattingOptions2.SpaceWithinMethodCallParentheses, true },
            { CSharpFormattingOptions2.SpaceAfterMethodCallName, true },
            { CSharpFormattingOptions2.SpaceBetweenEmptyMethodCallParentheses, true },
        };
        await AssertFormatAsync("""

            [Bar ( A = 1, B = 2 )]
            class Program
            {
                public void goo()
                {
                    var a = typeof ( A );
                    var b = M ( a );
                    var c = default ( A );
                    var d = sizeof ( A );
                    M ( );
                }
            }
            """, """

            [Bar(A=1,B=2)]
            class Program
            {
                public void goo()
                {
                    var a = typeof(A);
                    var b = M(a);
                    var c = default(A);
                    var d = sizeof(A);
                    M();
                }
            }
            """, changedOptionSet: optionSet);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/1298")]
    public Task DoNotforceAccessorsToNewLineWithPropertyInitializers()
        => AssertFormatAsync("""
            using System.Collections.Generic;

            class Program
            {
                public List<ExcludeValidation> ValidationExcludeFilters { get; }
                = new List<ExcludeValidation>();
            }

            public class ExcludeValidation
            {
            }
            """, """
            using System.Collections.Generic;

            class Program
            {
                public List<ExcludeValidation> ValidationExcludeFilters { get; }
                = new List<ExcludeValidation>();
            }

            public class ExcludeValidation
            {
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/1339")]
    public async Task DoNotFormatAutoPropertyInitializerIfNotDifferentLine()
    {
        var code = """
            class Program
            {
                public int d { get; }
                        = 3;
                static void Main(string[] args)
                {
                }
            }
            """;
        await AssertFormatAsync(code, code);
    }

    [Fact]
    public Task SpacingForForStatementInfiniteLoop()
        => AssertFormatAsync("""

            class Program
            {
                void Main()
                {
                    for (; ; )
                    {
                    }
                }
            }
            """, """

            class Program
            {
                void Main()
                {
                    for ( ;;)
                    {
                    }
                }
            }
            """);

    [Fact]
    public async Task SpacingForForStatementInfiniteLoopWithNoSpaces()
    {
        var optionSet = new OptionsCollection(LanguageNames.CSharp)
        {
            { CSharpFormattingOptions2.SpaceAfterSemicolonsInForStatement, false },
        };

        await AssertFormatAsync("""

            class Program
            {
                void Main()
                {
                    for (;;)
                    {
                    }
                }
            }
            """, """

            class Program
            {
                void Main()
                {
                    for ( ; ; )
                    {
                    }
                }
            }
            """, changedOptionSet: optionSet);
    }

    [Fact]
    public async Task SpacingForForStatementInfiniteLoopWithSpacesBefore()
    {
        var optionSet = new OptionsCollection(LanguageNames.CSharp)
        {
            { CSharpFormattingOptions2.SpaceBeforeSemicolonsInForStatement, true },
            { CSharpFormattingOptions2.SpaceAfterSemicolonsInForStatement, false },
        };

        await AssertFormatAsync("""

            class Program
            {
                void Main()
                {
                    for ( ; ;)
                    {
                    }
                }
            }
            """, """

            class Program
            {
                void Main()
                {
                    for (;; )
                    {
                    }
                }
            }
            """, changedOptionSet: optionSet);
    }

    [Fact]
    public async Task SpacingForForStatementInfiniteLoopWithSpacesBeforeAndAfter()
    {
        var optionSet = new OptionsCollection(LanguageNames.CSharp)
        {
            { CSharpFormattingOptions2.SpaceBeforeSemicolonsInForStatement, true },
        };

        await AssertFormatAsync("""

            class Program
            {
                void Main()
                {
                    for ( ; ; )
                    {
                    }
                }
            }
            """, """

            class Program
            {
                void Main()
                {
                    for (;;)
                    {
                    }
                }
            }
            """, changedOptionSet: optionSet);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/4240")]
    [WorkItem("https://github.com/dotnet/roslyn/issues/4421")]
    public Task VerifySpacingAfterMethodDeclarationName_Default()
        => AssertFormatAsync("""
            class Program<T>
            {
                public static Program operator +(Program p1, Program p2) { return null; }
                public static implicit operator string(Program p) { return null; }
                public static void M() { }
                public void F<T>() { }
            }
            """, """
            class Program<T>
            {
                public static Program operator +   (Program p1, Program p2) { return null; }
                public static implicit operator string (Program p) { return null; }
                public static void M  () { }
                public void F<T>    () { }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/4421")]
    [WorkItem("https://github.com/dotnet/roslyn/issues/4240")]
    public async Task VerifySpacingAfterMethodDeclarationName_NonDefault()
    {
        var changingOptions = new OptionsCollection(LanguageNames.CSharp)
        {
            { CSharpFormattingOptions2.SpacingAfterMethodDeclarationName, true }
        };
        await AssertFormatAsync("""
            class Program<T>
            {
                public static Program operator + (Program p1, Program p2) { return null; }
                public static implicit operator string (Program p) { return null; }
                public static void M () { }
                public void F<T> () { }
            }
            """, """
            class Program<T>
            {
                public static Program operator +   (Program p1, Program p2) { return null; }
                public static implicit operator string     (Program p) { return null; }
                public static void M  () { }
                public void F<T>   () { }
            }
            """, changedOptionSet: changingOptions);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/939")]
    public async Task DoNotFormatInsideArrayInitializers()
    {
        var code = """
            class Program
            {
                static void Main(string[] args)
                {
                    int[] sss = new[] {
                                   //Comment1
                            2,
                        5,            324534,    345345,
                                    //Comment2
                                        //This comment should not line up with the previous comment
                                234234
                                 //Comment3
                            ,         234,
                        234234
                                            /*
                                            This is a multiline comment
                                            */
                                //Comment4
                          };
                }
            }
            """;
        await AssertFormatAsync(code, code);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/4280")]
    [WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1184285")]
    public Task FormatDictionaryInitializers()
        => AssertFormatAsync("""
            class Program
            {
                void Main()
                {
                    var sample = new Dictionary<string, string> { ["x"] = "d", ["z"] = "XX" };
                }
            }
            """, """
            class Program
            {
                void Main()
                {
                    var sample = new Dictionary<string, string> {["x"] = "d"    ,["z"]   =  "XX" };
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/3256")]
    public Task SwitchSectionHonorsNewLineForBracesinControlBlockOption_Default()
        => AssertFormatAsync("""
            class Program
            {
                public void goo()
                {
                    int f = 1;
                    switch (f)
                    {
                        case 1:
                            {
                                // DO nothing
                                break;
                            }
                    }
                }
            }
            """, """
            class Program
            {
                public void goo()
                {
                    int f = 1;
                    switch (f) {
                        case 1: {
                                // DO nothing
                                break;
                            }
                    }
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/3256")]
    public async Task SwitchSectionHonorsNewLineForBracesinControlBlockOption_NonDefault()
    {
        var changingOptions = new OptionsCollection(LanguageNames.CSharp)
        {
            { NewLineBeforeOpenBrace, NewLineBeforeOpenBrace.DefaultValue.WithFlagValue(NewLineBeforeOpenBracePlacement.ControlBlocks, false) }
        };
        await AssertFormatAsync("""
            class Program
            {
                public void goo()
                {
                    int f = 1;
                    switch (f) {
                        case 1: {
                                // DO nothing
                                break;
                            }
                    }
                }
            }
            """, """
            class Program
            {
                public void goo()
                {
                    int f = 1;
                    switch (f)
                    {
                        case 1:
                            {
                                // DO nothing
                                break;
                            }
                    }
                }
            }
            """, changedOptionSet: changingOptions);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/4014")]
    public async Task FormattingCodeWithMissingTokensShouldRespectFormatTabsOption1()
    {
        var optionSet = new OptionsCollection(LanguageNames.CSharp) { { FormattingOptions2.UseTabs, true } };

        await AssertFormatAsync("""
            class Program
            {
            	static void Main()
            	{
            		return // Note the missing semicolon
            	} // The tab here should stay a tab
            }
            """, """
            class Program
            {
            	static void Main()
            	{
            		return // Note the missing semicolon
            	} // The tab here should stay a tab
            }
            """, changedOptionSet: optionSet);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/4014")]
    public async Task FormattingCodeWithMissingTokensShouldRespectFormatTabsOption2()
    {
        var optionSet = new OptionsCollection(LanguageNames.CSharp) { { FormattingOptions2.UseTabs, true } };

        await AssertFormatAsync("""
            struct Goo
            {
            	private readonly string bar;

            	public Goo(readonly string bar)
            	{
            	}
            }
            """, """
            struct Goo
            {
            	private readonly string bar;

            	public Goo(readonly string bar)
            	{
            	}
            }
            """, changedOptionSet: optionSet);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/4014")]
    public async Task FormattingCodeWithBrokenLocalDeclarationShouldRespectFormatTabsOption()
    {
        var optionSet = new OptionsCollection(LanguageNames.CSharp) { { FormattingOptions2.UseTabs, true } };

        await AssertFormatAsync("""
            class AClass
            {
            	void AMethod(Object anArgument)
            	{
            		if (anArgument == null)
            		{
            			throw new ArgumentNullException(nameof(anArgument));
            		}
            		anArgument

            		DoSomething();
            	}

            	void DoSomething()
            	{
            	}
            }
            """, """
            class AClass
            {
            	void AMethod(Object anArgument)
            	{
            		if (anArgument == null)
            		{
            			throw new ArgumentNullException(nameof(anArgument));
            		}anArgument

            		DoSomething();
            	}

            	void DoSomething()
            	{
            	}
            }
            """, changedOptionSet: optionSet);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/4014")]
    public async Task FormattingCodeWithBrokenInterpolatedStringShouldRespectFormatTabsOption()
    {
        var optionSet = new OptionsCollection(LanguageNames.CSharp) { { FormattingOptions2.UseTabs, true } };

        await AssertFormatAsync("""
            class AClass
            {
            	void Main()
            	{
            		Test($"\"_{\"");
            		Console.WriteLine(args);
            	}
            }
            """, """
            class AClass
            {
            	void Main()
            	{
            		Test($"\"_{\"");
            		Console.WriteLine(args);
            	}
            }
            """, changedOptionSet: optionSet);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/84")]
    [WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/849870")]
    public async Task NewLinesForBracesInPropertiesTest()
    {
        var changingOptions = new OptionsCollection(LanguageNames.CSharp)
        {
            { NewLineBeforeOpenBrace, NewLineBeforeOpenBrace.DefaultValue.WithFlagValue(NewLineBeforeOpenBracePlacement.Properties, false) },
        };
        await AssertFormatAsync("""
            class Class2
            {
                int Goo {
                    get
                    {
                        return 1;
                    }
                }

                int MethodGoo()
                {
                    return 42;
                }
            }
            """, """
            class Class2
            {
                int Goo
                {
                    get
                    {
                        return 1;
                    }
                }

                int MethodGoo()
                {
                    return 42; 
                }
            }
            """, changingOptions);
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/849870")]
    [WorkItem("https://github.com/dotnet/roslyn/issues/84")]
    public async Task NewLinesForBracesInAccessorsTest()
    {
        var changingOptions = new OptionsCollection(LanguageNames.CSharp)
        {
            { NewLineBeforeOpenBrace, NewLineBeforeOpenBrace.DefaultValue.WithFlagValue(NewLineBeforeOpenBracePlacement.Accessors, false) },
        };
        await AssertFormatAsync("""
            class Class2
            {
                int Goo
                {
                    get {
                        return 1;
                    }
                }

                int MethodGoo()
                {
                    return 42;
                }
            }
            """, """
            class Class2
            {
                int Goo
                {
                    get
                    {
                        return 1;
                    }
                }

                int MethodGoo()
                {
                    return 42; 
                }
            }
            """, changingOptions);
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/849870")]
    [WorkItem("https://github.com/dotnet/roslyn/issues/84")]
    public async Task NewLinesForBracesInPropertiesAndAccessorsTest()
    {
        var changingOptions = new OptionsCollection(LanguageNames.CSharp)
        {
            { NewLineBeforeOpenBrace, NewLineBeforeOpenBrace.DefaultValue
                .WithFlagValue(NewLineBeforeOpenBracePlacement.Properties, false)
                .WithFlagValue(NewLineBeforeOpenBracePlacement.Accessors, false)},
        };
        await AssertFormatAsync("""
            class Class2
            {
                int Goo {
                    get {
                        return 1;
                    }
                }

                int MethodGoo()
                {
                    return 42;
                }
            }
            """, """
            class Class2
            {
                int Goo
                {
                    get
                    {
                        return 1;
                    }
                }

                int MethodGoo()
                {
                    return 42; 
                }
            }
            """, changingOptions);
    }

    [Fact, WorkItem(111079, "devdiv.visualstudio.com")]
    public async Task TestThrowInIfOnSingleLine()
    {
        var code = """

            class C
            {
                void M()
                {
                    if (true) throw new Exception(
                        "message");
                }
            }

            """;

        await AssertFormatAsync(code, code);
    }

    [Fact, WorkItem("https://connect.microsoft.com/VisualStudio/feedback/details/1711675/autoformatting-issues")]
    public async Task SingleLinePropertiesPreservedWithLeaveStatementsAndMembersOnSingleLineFalse()
    {
        var changedOptionSet = new OptionsCollection(LanguageNames.CSharp)
        {
            { CSharpFormattingOptions2.WrappingPreserveSingleLine, true },
            { CSharpFormattingOptions2.WrappingKeepStatementsOnSingleLine, false},
        };

        await AssertFormatAsync("""

            class C
            {
                string Name { get; set; }
            }
            """, """

            class C
            {
                string  Name    {    get    ;   set     ;    }
            }
            """, changedOptionSet: changedOptionSet);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/4720")]
    public Task KeepAccessorWithAttributeOnSingleLine()
        => AssertFormatAsync("""

            class Program
            {
                public Int32 PaymentMethodID
                {
                    [System.Diagnostics.DebuggerStepThrough]
                    get { return 10; }
                }
            }
            """, """

            class Program
            {
                public Int32 PaymentMethodID
                {
                    [System.Diagnostics.DebuggerStepThrough]
                    get { return 10; }
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/6905")]
    public async Task KeepConstructorBodyInSameLineAsBaseConstructorInitializer()
    {
        var code = """

            class C
            {
                public C(int s)
                    : base() { }
                public C()
                {
                }
            }
            """;
        await AssertFormatAsync(code, code);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/6905")]
    public async Task KeepConstructorBodyInSameLineAsThisConstructorInitializer()
    {
        var code = """

            class C
            {
                public C(int s)
                    : this() { }
                public C()
                {
                }
            }
            """;
        await AssertFormatAsync(code, code);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/6905")]
    public Task KeepConstructorBodyInSameLineAsThisConstructorInitializerAdjustSpace()
        => AssertFormatAsync("""

            class C
            {
                public C(int s)
                    : this() { }
                public C()
                {
                }
            }
            """, """

            class C
            {
                public C(int s)
                    : this()      { }
                public C()
                {
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/4720")]
    public Task OneSpaceBetweenAccessorsAndAttributes()
        => AssertFormatAsync("""

            class Program
            {
                public int SomeProperty { [SomeAttribute] get; [SomeAttribute] private set; }
            }
            """, """

            class Program
            {
                public int SomeProperty {    [SomeAttribute] get;    [SomeAttribute] private set; }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/7900")]
    public Task FormatEmbeddedStatementInsideLockStatement()
        => AssertFormatAsync("""

            class C
            {
                private object _l = new object();
                public void M()
                {
                    lock (_l) Console.WriteLine("d");
                }
            }
            """, """

            class C
            {
                private object _l = new object();
                public void M()
                {
                    lock (_l)     Console.WriteLine("d");
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/7900")]
    public Task FormatEmbeddedStatementInsideLockStatementDifferentLine()
        => AssertFormatAsync("""

            class C
            {
                private object _l = new object();
                public void M()
                {
                    lock (_l)
                        Console.WriteLine("d");
                }
            }
            """, """

            class C
            {
                private object _l = new object();
                public void M()
                {
                    lock (_l)
                Console.WriteLine("d");
                }
            }
            """);

    [Fact]
    public async Task PropertyDeclarationSimple()
    {
        var expected = @"if (o is Point p)";
        await AssertFormatBodyAsync(expected, expected);
        await AssertFormatBodyAsync(expected, @"if (o is Point   p)");
        await AssertFormatBodyAsync(expected, @"if (o is Point p  )");
    }

    [Fact]
    public async Task PropertyDeclarationTypeOnNewLine()
    {
        var expected = """

            var y = o is
            Point p;
            """;
        await AssertFormatBodyAsync(expected, expected);
        await AssertFormatBodyAsync(expected, """

            var y = o is
            Point p;    
            """);

        await AssertFormatBodyAsync(expected, """

            var y = o   is
            Point p    ;
            """);

        await AssertFormatBodyAsync(expected, """

            var y = o   is
            Point     p    ;
            """);
    }

    [Fact]
    public async Task CasePatternDeclarationSimple()
    {
        var expected = """

            switch (o)
            {
                case Point p:
            }
            """;

        await AssertFormatBodyAsync(expected, expected);
        await AssertFormatBodyAsync(expected, """

            switch (o)
            {
                case Point p   :
            }
            """);

        await AssertFormatBodyAsync(expected, """

            switch (o)
            {
                case Point    p   :
            }
            """);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/23703")]
    public async Task FormatNullableArray()
    {
        var code = """

            class C
            {
                object[]? F = null;
            }
            """;
        await AssertFormatAsync(code, code);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/23703")]
    public async Task FormatConditionalWithArrayAccess()
    {
        var code = """

            class C
            {
                void M()
                {
                    _ = array[1] ? 2 : 3;
                }
            }
            """;
        await AssertFormatAsync(code, code);
    }

    private Task AssertFormatBodyAsync(string expected, string input)
    {
        static string transform(string s)
        {
            var lines = s.Split([Environment.NewLine], StringSplitOptions.None);
            for (var i = 0; i < lines.Length; i++)
            {
                if (!string.IsNullOrEmpty(lines[i]))
                {
                    lines[i] = new string(' ', count: 8) + lines[i];
                }
            }

            return string.Join(Environment.NewLine, lines);
        }

        var pattern = """

            class C
            {{
                void M()
                {{
            {0}
                }}
            }}
            """;

        expected = string.Format(pattern, transform(expected));
        input = string.Format(pattern, transform(input));
        return AssertFormatAsync(expected, input);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/6628")]
    public Task FormatElseBlockBracesOnDifferentLineToNewLines()
        => AssertFormatAsync("""

            class C
            {
                public void M()
                {
                    if (true)
                    {
                    }
                    else
                    {
                    }
                }
            }
            """, """

            class C
            {
                public void M()
                {
                    if (true)
                    {
                    }
                    else {
                    }
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/6628")]
    public async Task FormatOnElseBlockBracesOnSameLineRemainsInSameLine_1()
    {
        var code = """

            class C
            {
                public void M()
                {
                    if (true)
                    {
                    }
                    else { }
                }
            }
            """;
        await AssertFormatAsync(code, code);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/11572")]
    public Task FormatAttributeOnSameLineAsField()
        => AssertFormatAsync(
            """

            class C
            {
                [Attr] int i;
            }
            """,
            """

            class C {
                [Attr]   int   i;
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/21789")]
    public Task FormatMultipleAttributeOnSameLineAsField1()
        => AssertFormatAsync(
            """

            class C
            {
                [Attr1]
                [Attr2]
                [Attr3][Attr4] int i;
            }
            """,
            """

            class C {
                [Attr1]
                [Attr2]
                [Attr3][Attr4]   int   i;
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/21789")]
    public Task FormatMultipleAttributesOnSameLineAsField2()
        => AssertFormatAsync(
            """

            class C
            {
                [Attr1]
                [Attr2]
                [Attr3][Attr4] int i;
            }
            """,
            """

            class C {
                [Attr1][Attr2]
                [Attr3][Attr4]   int   i;
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/21789")]
    public Task FormatMultipleAttributeOnSameLineAndFieldOnNewLine()
        => AssertFormatAsync(
            """

            class C
            {
                [Attr1]
                [Attr2]
                int i;
            }
            """,
            """

            class C {
                [Attr1][Attr2]
                int   i;
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/6628")]
    public async Task FormatOnElseBlockBracesOnSameLineRemainsInSameLine_2()
    {
        var code = """

            class C
            {
                public void M()
                {
                    if (true)
                    {
                    }
                    else
                    { }
                }
            }
            """;
        await AssertFormatAsync(code, code);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/25098")]
    public void FormatSingleStructDeclaration()
        => Formatter.Format(SyntaxFactory.StructDeclaration("S"), DefaultWorkspace.Services.SolutionServices, CSharpSyntaxFormattingOptions.Default, CancellationToken.None);

    [Fact]
    public Task FormatIndexExpression()
        => AssertFormatAsync("""

            class C
            {
                void M()
                {
                    object x = ^1;
                    object y = ^1
                }
            }
            """, """

            class C
            {
                void M()
                {
                    object x = ^1;
                    object y = ^1
                }
            }
            """);

    [Fact]
    public Task FormatRangeExpression_NoOperands()
        => AssertFormatAsync("""

            class C
            {
                void M()
                {
                    object x = ..;
                    object y = ..
                }
            }
            """, """

            class C
            {
                void M()
                {
                    object x = ..;
                    object y = ..
                }
            }
            """);

    [Fact]
    public Task FormatRangeExpression_RightOperand()
        => AssertFormatAsync("""

            class C
            {
                void M()
                {
                    object x = ..1;
                    object y = ..1
                }
            }
            """, """

            class C
            {
                void M()
                {
                    object x = ..1;
                    object y = ..1
                }
            }
            """);

    [Fact]
    public Task FormatRangeExpression_LeftOperand()
        => AssertFormatAsync("""

            class C
            {
                void M()
                {
                    object x = 1..;
                    object y = 1..
                }
            }
            """, """

            class C
            {
                void M()
                {
                    object x = 1..;
                    object y = 1..
                }
            }
            """);

    [Fact]
    public Task FormatRangeExpression_BothOperands()
        => AssertFormatAsync("""

            class C
            {
                void M()
                {
                    object x = 1..2;
                    object y = 1..2
                }
            }
            """, """

            class C
            {
                void M()
                {
                    object x = 1..2;
                    object y = 1..2
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/32113")]
    public Task FormatCommaAfterCloseBrace_CommaRemainIntheSameLine()
        => AssertFormatAsync(
            """

            public class Test
            {
                public void Foo()
                {
                    (Action, Action, Action) tuple = (
                        () => { Console.WriteLine(2.997e8); },
                        () => { Console.WriteLine(6.67e-11); },
                        () => { Console.WriteLine(1.602e-19); }
                    );
                }
            }
            """,
            """

            public class Test
            {
                public void Foo()
                {
                    (Action, Action, Action) tuple = (
                        () => { Console.WriteLine(2.997e8); },
                        () => { Console.WriteLine(6.67e-11); },
                        () => { Console.WriteLine(1.602e-19); }
                    );
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/32113")]
    public Task FormatCommaAfterCloseBrace_SpaceSurroundWillBeRemoved()
        => AssertFormatAsync(
            """

            public class Test
            {
                public void Foo()
                {
                    (Action, Action, Action) tuple = (
                        () => { Console.WriteLine(2.997e8); },
                        () => { Console.WriteLine(6.67e-11); },
                        () => { Console.WriteLine(1.602e-19); }
                    );
                }
            }
            """,
            """

            public class Test
            {
                public void Foo()
                {
                    (Action, Action, Action) tuple = (
                        () => { Console.WriteLine(2.997e8); }                             ,        
                        () => { Console.WriteLine(6.67e-11); }   ,    
                        () => { Console.WriteLine(1.602e-19); }
                    );
                }
            }
            """);

    [Theory, WorkItem("https://github.com/dotnet/roslyn/issues/31571")]
    [WorkItem("https://github.com/dotnet/roslyn/issues/33910")]
    [CombinatorialData]
    public async Task ConversionOperator_CorrectlySpaceArgumentList(
        [CombinatorialValues("implicit", "explicit")] string operatorType,
        [CombinatorialValues("string", "string[]", "System.Action<int>", "int?", "int*", "(int, int)")] string targetType,
        bool spacingAfterMethodDeclarationName)
    {
        var expectedSpacing = spacingAfterMethodDeclarationName ? " " : "";
        var initialSpacing = spacingAfterMethodDeclarationName ? "" : " ";
        var changedOptionSet = new OptionsCollection(LanguageNames.CSharp) { { SpacingAfterMethodDeclarationName, spacingAfterMethodDeclarationName } };
        await AssertFormatAsync(
            $$"""

            public unsafe class Test
            {
                public static {{operatorType}} operator {{targetType}}{{expectedSpacing}}() => throw null;
            }
            """,
            $$"""

            public unsafe class Test
            {
                public static {{operatorType}} operator {{targetType}}{{initialSpacing}}() => throw null;
            }
            """,
            changedOptionSet: changedOptionSet);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/31868")]
    public async Task SpaceAroundDeclaration()
    {
        var changingOptions = new OptionsCollection(LanguageNames.CSharp)
        {
            { CSharpFormattingOptions2.SpacesIgnoreAroundVariableDeclaration, true }
        };
        await AssertFormatAsync(
            """

            class Program
            {
                public void FixMyType()
                {
                    var    myint    =    0;
                }
            }
            """,
            """

            class Program
            {
                public void FixMyType()
                {
                    var    myint    =    0;
                }
            }
            """, changedOptionSet: changingOptions);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/31868")]
    public async Task SpaceAroundDeclarationAndPreserveSingleLine()
    {
        var changingOptions = new OptionsCollection(LanguageNames.CSharp)
        {
            { CSharpFormattingOptions2.SpacesIgnoreAroundVariableDeclaration, true },
            { CSharpFormattingOptions2.WrappingKeepStatementsOnSingleLine, false }
        };
        await AssertFormatAsync(
            """

            class Program
            {
                public void FixMyType()
                {
                    var    myint    =    0;
                }
            }
            """,
            """

            class Program
            {
                public void FixMyType()
                {
                    var    myint    =    0;
                }
            }
            """, changedOptionSet: changingOptions);
    }

    [Fact]
    public Task ClassConstraint()
        => AssertFormatAsync(
            """

            class Program<T>
                where T : class?
            {
            }
            """,
            """

            class Program<T>
                where T : class ?
            {
            }
            """);

    [Fact]
    public Task SingleLinePropertyPattern1()
        => AssertFormatAsync(
            """

            using System.Collections.Generic;
            class Program
            {
                public void FixMyType()
                {
                    _ = new List<int>() is
                    {
                        Count: { },
                    };
                }
            }
            """,
            """

            using System.Collections.Generic;
            class Program
            {
                public void FixMyType()
                {
                    _ = new List<int>() is
                    {
                        Count:{},
                    };
                }
            }
            """);

    [Fact]
    public Task SingleLinePropertyPattern2()
        => AssertFormatAsync(
            """

            using System.Collections.Generic;
            class Program
            {
                public void FixMyType(object o)
                {
                    _ = o is List<int> { Count: { } };
                }
            }
            """,
            """

            using System.Collections.Generic;
            class Program
            {
                public void FixMyType(object o)
                {
                    _ = o is List<int>{Count:{}};
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/37030")]
    public async Task SpaceAroundEnumMemberDeclarationIgnored()
    {
        var changingOptions = new OptionsCollection(LanguageNames.CSharp)
        {
            { CSharpFormattingOptions2.SpacesIgnoreAroundVariableDeclaration, true }
        };
        await AssertFormatAsync(
            """

            enum TestEnum
            {
                Short           = 1,
                LongItemName    = 2
            }
            """,
            """

            enum TestEnum
            {
                Short           = 1,
                LongItemName    = 2
            }
            """, changedOptionSet: changingOptions);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/37030")]
    public async Task SpaceAroundEnumMemberDeclarationSingle()
    {
        var changingOptions = new OptionsCollection(LanguageNames.CSharp)
        {
            { CSharpFormattingOptions2.SpacesIgnoreAroundVariableDeclaration, false }
        };
        await AssertFormatAsync(
            """

            enum TestEnum
            {
                Short = 1,
                LongItemName = 2
            }
            """,
            """

            enum TestEnum
            {
                Short           = 1,
                LongItemName    = 2
            }
            """, changedOptionSet: changingOptions);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/38895")]
    public Task FormattingNbsp()
        => AssertFormatAsync(
            """

            class C
            {
                List<C> list = new List<C>
                {
            new C()
                };
            }
            """,
            """

            class C
            {
                List<C> list = new List<C>
                {
            &nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;new&nbsp;C()
                };
            }
            """.Replace("&nbsp;", "\u00A0"));

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/47438")]
    public Task IndentationForMultilineWith()
        => AssertFormatAsync("""
            record C(int X)
            {
                C M()
                {
                    return this with
                    {
                        X = 1
                    };
                }
            }
            """, """
            record C(int X)
            {
                C M()
                {
                    return this with
            {
            X = 1
            };
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/47438")]
    public Task IndentationForMultilineWith_ArrowBody()
        => AssertFormatAsync("""
            record C(int X)
            {
                C M()
                    => this with
                    {
                        X = 1
                    };
            }
            """, """
            record C(int X)
            {
                C M()
                    => this with
            {
            X = 1
            };
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/47438")]
    public Task IndentationForMultilineWith_ArrowBody_WithTrailingComma()
        => AssertFormatAsync("""
            record C(int X)
            {
                C M()
                    => this with
                    {
                        X = 1,
                    };
            }
            """, """
            record C(int X)
            {
                C M()
                    => this with
            {
            X = 1,
            };
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/41022")]
    public Task SpacingAfterAttribute()
        => AssertFormatAsync("""
            class C
            {
                void M([My] string?[]?[] x)
                {
                }
            }
            """, """
            class C
            {
                void M([My]string?[]?[] x)
                {
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/41022")]
    public Task SpacingAfterAttribute_Multiple()
        => AssertFormatAsync("""
            class C
            {
                void M([My][My] int x)
                {
                }
            }
            """, """
            class C
            {
                void M([My][My]  int x)
                {
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/41022")]
    public Task SpacingAfterAttribute_Multiple2()
        => AssertFormatAsync("""
            class C
            {
                void M([My][My] int x)
                {
                }
            }
            """, """
            class C
            {
                void M([My] [My]  int x)
                {
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/41022")]
    public Task SpacingAfterAttribute_MultipleOnDeclaration()
        => AssertFormatAsync("""
            class C
            {
                [My]
                [My]
                void M()
                {
                }
            }
            """, """
            class C
            {
                [My] [My]  void M()
                {
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/47442")]
    public Task IndentImplicitObjectCreationInitializer()
        => AssertFormatAsync("""

            class C
            {
                public string Name { get; set; }
                public static C Create1(string name)
                    => new C()
                    {
                        Name = name
                    };
                public static C Create2(string name)
                    => new()
                    {
                        Name = name
                    };
            }
            """, """

            class C
            {
                public string Name { get; set; }
                public static C Create1(string name)
                    => new C()
                {
                    Name = name
                };
                public static C Create2(string name)
                    => new()
                {
                    Name = name
                };
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/36913")]
    public Task NewLinesForBraces_SwitchExpression_Default()
        => AssertFormatAsync(
            """

            class A
            {
                void br()
                {
                    var msg = 1 switch
                    {
                        _ => null
                    };
                }
            }
            """,
            """

            class A
            {
                void br()
                {
                    var msg = 1 switch {
                        _ => null
                    };
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/36913")]
    public async Task NewLinesForBraces_SwitchExpression_NonDefault()
    {
        var changingOptions = new OptionsCollection(LanguageNames.CSharp)
        {
            { NewLineBeforeOpenBrace, NewLineBeforeOpenBrace.DefaultValue.WithFlagValue(NewLineBeforeOpenBracePlacement.ObjectCollectionArrayInitializers, false) },
        };
        await AssertFormatAsync(
            """

            class A
            {
                void br()
                {
                    var msg = 1 switch {
                        _ => null
                    };
                }
            }
            """,
            """

            class A
            {
                void br()
                {
                    var msg = 1 switch
                    {
                        _ => null
                    };
                }
            }
            """, changedOptionSet: changingOptions);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/discussions/49725")]
    public Task NewLinesForBraces_RecordWithInitializer_Default()
        => AssertFormatAsync(
            """

            record R(int X);
            class C
            {
                void Goo(R r)
                {
                    var r2 = r with
                    {
                        X = 0
                    };
                }
            }
            """,
            """

            record R(int X);
            class C
            {
                void Goo(R r)
                {
                    var r2 = r with {
                        X = 0
                    };
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/discussions/49725")]
    public async Task NewLinesForBraces_RecordWithInitializer_NonDefault()
    {
        var changingOptions = new OptionsCollection(LanguageNames.CSharp)
        {
            { NewLineBeforeOpenBrace, NewLineBeforeOpenBrace.DefaultValue.WithFlagValue(NewLineBeforeOpenBracePlacement.ObjectCollectionArrayInitializers, false) },
        };
        await AssertFormatAsync(
            """

            record R(int X);
            class C
            {
                void Goo(R r)
                {
                    var r2 = r with {
                        X = 0
                    };
                }
            }
            """,
            """

            record R(int X);
            class C
            {
                void Goo(R r)
                {
                    var r2 = r with
                    {
                        X = 0
                    };
                }
            }
            """, changedOptionSet: changingOptions);
    }

    [Fact]
    public Task NoSpacesInPropertyPatterns()
        => AssertFormatAsync("""
            class C
            {
                int IntProperty { get; set; }
                void M()
                {
                    _ = this is { IntProperty: 2 };
                }
            }
            """, """
            class C
            {
                int IntProperty { get; set; }
                void M()
                {
                    _ = this is {  IntProperty : 2 };
                }
            }
            """);

    [Fact]
    public Task NoSpacesInExtendedPropertyPatterns()
        => AssertFormatAsync("""
            class C
            {
                C CProperty { get; set; }
                int IntProperty { get; set; }
                void M()
                {
                    _ = this is { CProperty.IntProperty: 2 };
                }
            }
            """, """
            class C
            {
                C CProperty { get; set; }
                int IntProperty { get; set; }
                void M()
                {
                    _ = this is {  CProperty . IntProperty : 2 };
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/52413")]
    public Task NewLinesForBraces_PropertyPatternClauses_Default()
        => AssertFormatAsync(
            """

            class A
            {
                public string Name { get; }

                public bool IsFoo(A a)
                {
                    return a is
                    {
                        Name: "foo",
                    };
                }
            }
            """,
            """

            class A
            {
                public string Name { get; }

                public bool IsFoo(A a)
                {
                    return a is {
                        Name: "foo",
                    };
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/52413")]
    public async Task NewLinesForBraces_PropertyPatternClauses_NonDefault()
    {
        var changingOptions = new OptionsCollection(LanguageNames.CSharp)
        {
            { NewLineBeforeOpenBrace, NewLineBeforeOpenBrace.DefaultValue.WithFlagValue(NewLineBeforeOpenBracePlacement.ObjectCollectionArrayInitializers, false) },
        };
        await AssertFormatAsync(
            """

            class A
            {
                public string Name { get; }

                public bool IsFoo(A a)
                {
                    return a is {
                        Name: "foo",
                    };
                }
            }
            """,
            """

            class A
            {
                public string Name { get; }

                public bool IsFoo(A a)
                {
                    return a is
                    {
                        Name: "foo",
                    };
                }
            }
            """, changedOptionSet: changingOptions);
    }

    [Fact, Trait(Traits.Feature, Traits.Features.Formatting)]
    [WorkItem(57854, "https://github.com/dotnet/roslyn/issues/57854")]
    public async Task NewLinesForBraces_PropertyPatternClauses_NonDefaultInSwitchExpression()
    {
        var changingOptions = new OptionsCollection(LanguageNames.CSharp)
        {
            { NewLineBeforeOpenBrace, NewLineBeforeOpenBrace.DefaultValue.WithFlagValue(NewLineBeforeOpenBracePlacement.ObjectCollectionArrayInitializers, false) },
        };
        await AssertFormatAsync(
            """

            class A
            {
                public string Name { get; }

                public bool IsFoo(A a)
                {
                    return a switch {
                        { Name: "foo" } => true,
                        _ => false,
                    };
                }
            }
            """,
            """

            class A
            {
                public string Name { get; }

                public bool IsFoo(A a)
                {
                    return a switch
                    {
                        { Name: "foo" } => true,
                        _ => false,
                    };
                }
            }
            """, changedOptionSet: changingOptions);
    }

    [Theory, CombinatorialData]
    [WorkItem("https://github.com/dotnet/roslyn/issues/52413")]
    public async Task NewLinesForBraces_PropertyPatternClauses_SingleLine(bool option)
    {
        var changingOptions = new OptionsCollection(LanguageNames.CSharp)
        {
            { NewLineBeforeOpenBrace, NewLineBeforeOpenBrace.DefaultValue.WithFlagValue(NewLineBeforeOpenBracePlacement.ObjectCollectionArrayInitializers, option) },
        };
        var code = """

            class A
            {
                public string Name { get; }

                public bool IsFoo(A a)
                {
                    return a is { Name: "foo" };
                }
            }
            """;
        await AssertFormatAsync(code, code, changedOptionSet: changingOptions);
    }

    [Fact]
    public Task RecordClass()
        => AssertFormatAsync(
            """

            record class R(int X);

            """,
            """

            record  class  R(int X);

            """);

    [Fact]
    public Task Class()
        => AssertFormatAsync(
            """

            class R(int X);

            """,
            """

            class  R(int X)  ;

            """);

    [Fact]
    public Task Interface()
        => AssertFormatAsync(
            """

            interface R(int X);

            """,
            """

            interface  R(int X)  ;

            """);

    [Fact]
    public Task RecordStruct()
        => AssertFormatAsync(
            """

            record struct R(int X);

            """,
            """

            record  struct  R(int X);

            """);

    [Fact]
    public Task Struct()
        => AssertFormatAsync(
            """

            struct R(int X);

            """,
            """

            struct  R(int X)  ;

            """);

    [Fact]
    public async Task FormatListPattern()
    {
        var code = """

            class C
            {
                void M() {
            _ = this is[1,2,>=3];
            }
            }
            """;
        await AssertFormatAsync(code: code, expected: """

            class C
            {
                void M()
                {
                    _ = this is [1, 2, >= 3];
                }
            }
            """);

        var options = new OptionsCollection(LanguageNames.CSharp)
        {
            { SpaceBetweenEmptySquareBrackets, false },
            { SpaceWithinSquareBrackets, false },
            { SpaceBeforeComma, false },
            { SpaceAfterComma, false },
        };

        await AssertFormatAsync(code: code, changedOptionSet: options, expected: """

            class C
            {
                void M()
                {
                    _ = this is [1,2,>= 3];
                }
            }
            """);

        options = new OptionsCollection(LanguageNames.CSharp)
        {
            { SpaceBeforeOpenSquareBracket, false }, // ignored
            { SpaceBetweenEmptySquareBrackets, true },
            { SpaceWithinSquareBrackets, true },
            { SpaceBeforeComma, true },
            { SpaceAfterComma, true },
        };

        await AssertFormatAsync(code: code, changedOptionSet: options, expected: """

            class C
            {
                void M()
                {
                    _ = this is [ 1 , 2 , >= 3 ];
                }
            }
            """);
    }

    [Fact]
    public async Task FormatListPattern_Parentheses()
    {
        var code = """

            class C
            {
                void M((int[], int[]) a) {
            _ = a is([1,2,>=3],[1,2]);
            }
            }
            """;
        await AssertFormatAsync(code: code, expected: """

            class C
            {
                void M((int[], int[]) a)
                {
                    _ = a is ([1, 2, >= 3], [1, 2]);
                }
            }
            """);

        var options = new OptionsCollection(LanguageNames.CSharp)
        {
            { SpaceBetweenEmptySquareBrackets, false },
            { SpaceWithinSquareBrackets, false },
            { SpaceBeforeComma, false },
            { SpaceAfterComma, false },
        };

        await AssertFormatAsync(code: code, changedOptionSet: options, expected: """

            class C
            {
                void M((int[],int[]) a)
                {
                    _ = a is ([1,2,>= 3],[1,2]);
                }
            }
            """);

        options = new OptionsCollection(LanguageNames.CSharp)
        {
            { SpaceBeforeOpenSquareBracket, false }, // ignored
            { SpaceBetweenEmptySquareBrackets, true },
            { SpaceWithinSquareBrackets, true },
            { SpaceBeforeComma, true },
            { SpaceAfterComma, true },
        };

        await AssertFormatAsync(code: code, changedOptionSet: options, expected: """

            class C
            {
                void M((int[ ] , int[ ]) a)
                {
                    _ = a is ([ 1 , 2 , >= 3 ], [ 1 , 2 ]);
                }
            }
            """);
    }

    [Fact]
    public async Task FormatListPattern_TrailingComma()
    {
        var code = """

            class C
            {
                void M() {
            _ = this is[1,2,>=3,];
            }
            }
            """;
        await AssertFormatAsync(code: code, expected: """

            class C
            {
                void M()
                {
                    _ = this is [1, 2, >= 3,];
                }
            }
            """);

        var options = new OptionsCollection(LanguageNames.CSharp)
        {
            { SpaceBetweenEmptySquareBrackets, false },
            { SpaceWithinSquareBrackets, false },
            { SpaceBeforeComma, false },
            { SpaceAfterComma, false },
        };

        await AssertFormatAsync(code: code, changedOptionSet: options, expected: """

            class C
            {
                void M()
                {
                    _ = this is [1,2,>= 3,];
                }
            }
            """);

        options = new OptionsCollection(LanguageNames.CSharp)
        {
            { SpaceBeforeOpenSquareBracket, false }, // ignored
            { SpaceBetweenEmptySquareBrackets, true },
            { SpaceWithinSquareBrackets, true },
            { SpaceBeforeComma, true },
            { SpaceAfterComma, true },
        };

        await AssertFormatAsync(code: code, changedOptionSet: options, expected: """

            class C
            {
                void M()
                {
                    _ = this is [ 1 , 2 , >= 3 , ];
                }
            }
            """);
    }

    [Fact]
    public async Task FormatListPattern_WithNewline()
    {
        var code = """

            class C
            {
                void M() {
            _ = this is
            [1,2,>=3
            ];
            }
            }
            """;
        await AssertFormatAsync(code: code, expected: """

            class C
            {
                void M()
                {
                    _ = this is
                    [1, 2, >= 3
                    ];
                }
            }
            """);

        var options = new OptionsCollection(LanguageNames.CSharp)
        {
            { SpaceBetweenEmptySquareBrackets, false },
            { SpaceWithinSquareBrackets, false },
            { SpaceBeforeComma, false },
            { SpaceAfterComma, false },
        };

        await AssertFormatAsync(code: code, changedOptionSet: options, expected: """

            class C
            {
                void M()
                {
                    _ = this is
                    [1,2,>= 3
                    ];
                }
            }
            """);

        options = new OptionsCollection(LanguageNames.CSharp)
        {
            { SpaceBeforeOpenSquareBracket, false }, // ignored
            { SpaceBetweenEmptySquareBrackets, true },
            { SpaceWithinSquareBrackets, true },
            { SpaceBeforeComma, true },
            { SpaceAfterComma, true },
        };

        await AssertFormatAsync(code: code, changedOptionSet: options, expected: """

            class C
            {
                void M()
                {
                    _ = this is
                    [ 1 , 2 , >= 3
                    ];
                }
            }
            """);
    }

    [Fact]
    public Task FormatSlicePattern()
        => AssertFormatAsync("""
            class C
            {
                void M()
                {
                    _ = this is [0, .. var rest];
                }
            }
            """, """
            class C
            {
                void M() {
            _ = this is[ 0,.. var  rest ];
            }
            }
            """);

    [Fact]
    public Task FormatSlicePattern_NoSpace()
        => AssertFormatAsync("""
            class C
            {
                void M()
                {
                    _ = this is [0, .. var rest];
                }
            }
            """, """
            class C
            {
                void M() {
            _ = this is[ 0,..var  rest ];
            }
            }
            """);

    [Fact]
    public Task FormatSlicePatternWithAnd()
        => AssertFormatAsync("""
            class C
            {
                void M()
                {
                    _ = this is [0, .. { Count: > 0 } and var rest];
                }
            }
            """, """
            class C
            {
                void M() {
            _ = this is[ 0,.. {Count: >0} and var  rest ];
            }
            }
            """);

    [Fact]
    public Task FormatLengthAndListPattern()
        => AssertFormatAsync("""
            class C
            {
                void M()
                {
                    _ = this is { Count: > 0 and var x } and [1, 2, 3];
                }
            }
            """, """
            class C
            {
                void M() {
            _ = this is{Count:>0 and var x}and[ 1,2,3 ];
            }
            }
            """);

    [Fact]
    public Task LambdaReturnType_01()
        => AssertFormatAsync(
            """
            class Program
            {
                Delegate D = void () => { };
            }
            """,
            """
            class Program
            {
                Delegate D = void  ()  =>  {  };
            }
            """);

    [Fact]
    public Task LambdaReturnType_02()
        => AssertFormatAsync(
            """
            class Program
            {
                Delegate D = A.B () => { };
            }
            """,
            """
            class Program
            {
                Delegate D = A.B()=>{  };
            }
            """);

    [Fact]
    public Task LambdaReturnType_03()
        => AssertFormatAsync(
            """
            class Program
            {
                Delegate D = A<B> (x) => x;
            }
            """,
            """
            class Program
            {
                Delegate D = A < B >  ( x ) => x;
            }
            """);

    [Fact]
    public Task LambdaReturnType_04()
        => AssertFormatAsync(
            """
            class Program
            {
                object F = Func((A, B) ((A, B) t) => t);
            }
            """,
            """
            class Program
            {
                object F = Func((A,B)((A,B)t)=>t);
            }
            """);

    [Fact]
    public async Task LineSpanDirective()
    {
        var optionSet = new OptionsCollection(LanguageNames.CSharp) { { FormattingOptions2.UseTabs, true } };
        await AssertFormatAsync(
            """
            class Program
            {
            	static void Main()
            	{
            #line (1, 1) - (1, 100) 5 "a.razor"
            	}
            }
            """,
            """
            class Program
            {
                static void Main()
                {
            #line (1,1)-(1,100) 5 "a.razor"
                }
            }
            """, changedOptionSet: optionSet);
    }

    [Fact]
    public Task FileScopedNamespace()
        => AssertFormatAsync(
            expected: """

            namespace NS;

            class C { }

            """,
            code: """

            namespace NS;

                class C { }

            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/67400")]
    public Task FileScopedNamespaceNewline()
        => AssertFormatAsync(
            expected: """
            namespace Some.Namespace;

            public class MyClass
            {
            }
            """,
            code: """
            namespace Some.Namespace;
            public class MyClass
            {
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/56498")]
    public Task NewInImplicitObjectCreation()
        => AssertFormatAsync(
            expected: """

            class C
            {
                void M()
                {
                    string v = new();
                }
            }

            """,
            code: """

            class C
            {
                void M() {
                    string  v     =    new   ();
                }
            }

            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/56498")]
    public Task NewInTupleArrayCreation()
        => AssertFormatAsync(
            expected: """

            class C
            {
                void M()
                {
                    var v = new (int, int)[];
                }
            }

            """,
            code: """

            class C
            {
                void M() {
                    var  v     =    new   (int,   int)  [ ];
                }
            }

            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/56498")]
    public Task NewInArrayCreation()
        => AssertFormatAsync(
            expected: """

            class C
            {
                void M()
                {
                    var v = new int[1];
                }
            }

            """,
            code: """

            class C
            {
                void M() {
                    var  v     =    new   int  [  1  ];
                }
            }

            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/56498")]
    public Task NewInImplicitArrayCreation()
        => AssertFormatAsync(
            expected: """

            class C
            {
                void M()
                {
                    var v = new[] { 1, 2, 3 };
                }
            }

            """,
            code: """

            class C
            {
                void M() {
                    var  v     =    new     [ ] {  1,  2,  3 };
                }
            }

            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/56498")]
    public Task NewInConstructorConstraint()
        => AssertFormatAsync(
            expected: """

            class C
            {
                void M<T>() where T : new()
                {
                }
            }

            """,
            code: """

            class C
            {
                void M<T>()   where   T   :   new    (   ) {
                }
            }

            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/56498")]
    public Task NewMethodOverloadWithTupleReturnType()
        => AssertFormatAsync(
            expected: """

            class C
            {
                new (int, int) M() { }
            }

            """,
            code: """

            class C
            {
                new  (int, int) M() { }
            }

            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/56498")]
    public Task NewPropertyWithTupleReturnType()
        => AssertFormatAsync(
            expected: """

            class C
            {
                new (int, int) Property { get; set; }
            }

            """,
            code: """

            class C
            {
                new  (int, int) Property { get; set; }
            }

            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/56498")]
    public Task NewIndexerWithTupleReturnType()
        => AssertFormatAsync(
            expected: """

            class C
            {
                new (int, int) this[int i] { get => throw null; }
            }

            """,
            code: """

            class C
            {
                new  (int, int) this[int i] { get => throw null; }
            }

            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/56543")]
    public Task FormatAttributeOnLambda()
        => AssertFormatAsync(
            expected: """

            var f = [Attribute] () => { };

            """,
            code: """

            var f =  [Attribute] () => { };

            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/56543")]
    public Task FormatAttributeOnLambda_TwoAttributes()
        => AssertFormatAsync(
            expected: """

            var f = [Attribute][Attribute2] () => { };

            """,
            code: """

            var f =  [Attribute]  [Attribute2] () => { };

            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/56543")]
    public Task FormatAttributeOnMethod_TwoAttributes()
        => AssertFormatAsync(
            expected: """

            [Attribute][Attribute2]
            void M()
            { }

            """,
            code: """

              [Attribute]  [Attribute2]
            void M()
            { }

            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/56543")]
    public Task FormatAttributeOnTypeParameter_TwoAttributes()
        => AssertFormatAsync(
            expected: """

            class C<[Attribute][Attribute2] T> { }

            """,
            code: """

            class C<  [Attribute]  [Attribute2]  T  > { }

            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/56543")]
    public Task FormatAttributeOnTypeParameter_TwoAttributes_Method()
        => AssertFormatAsync(
            expected: """

            class C
            {
                void M<[Attribute][Attribute2] T>() { }
            }

            """,
            code: """

            class C
            {
                void M<  [Attribute]  [Attribute2]  T  > ( ) { }
            }

            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/56543")]
    public Task FormatAttributeOnParameter_TwoAttributes()
        => AssertFormatAsync(
            expected: """

            class C
            {
                void M([Attribute][Attribute2] T t) { }
            }

            """,
            code: """

            class C
            {
                void M(  [Attribute]  [Attribute2]  T  t  ) { }
            }

            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/56543")]
    public Task FormatAttributeOnLambdaWithExplicitType()
        => AssertFormatAsync(
            expected: """

            var f = [Attribute] int () => 1;

            """,
            code: """

            var f =  [Attribute] int () => 1;

            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/56543")]
    public Task FormatAttributeOnLambdaInInvocation()
        => AssertFormatAsync(
            expected: """

            f([Attribute] () => { });

            """,
            code: """

            f( [Attribute] () => { });

            """);

    [Fact]
    public Task FormatAttributeOnLambdaParameter()
        => AssertFormatAsync(expected: """
            var f = ([Attribute] int x = 1) => x;
            """, code: """
            var f = (  [ Attribute ]int x=1)=>x;
            """);

    [Fact]
    public Task FormatRawStringInterpolation()
        => AssertFormatAsync(
            expected: """"

            var s = $"""{s}"""

            """",
            code: """"

            var s = $"""{s}"""

            """");

    [Fact]
    public Task FormatRawStringInterpolation2()
        => AssertFormatAsync(
            expected: """"

            var s = $"""{s,0: x }"""

            """",
            code: """"

            var s = $"""{s, 0 : x }"""

            """");

    [Fact]
    public Task FormatUsingAliasToType1()
        => AssertFormatAsync(
            expected: """

            f([Attribute] () => { });

            """,
            code: """

            f( [Attribute] () => { });

            """);

    [Theory]
    [InlineData("using X=int ;", "using X = int;")]
    [InlineData("global   using X=int ;", "global using X = int;")]
    [InlineData("using X=nint;", "using X = nint;")]
    [InlineData("using X=dynamic;", "using X = dynamic;")]
    [InlineData("using X=int [] ;", "using X = int[];")]
    [InlineData("using X=(int,int) ;", "using X = (int, int);")]
    [InlineData("using  unsafe  X=int * ;", "using unsafe X = int*;")]
    [InlineData("global   using  unsafe  X=int * ;", "global using unsafe X = int*;")]
    [InlineData("using X=int ?;", "using X = int?;")]
    [InlineData("using X=delegate * <int,int> ;", "using X = delegate*<int, int>;")]
    public Task TestNormalizeUsingAlias(string text, string expected)
        => AssertFormatAsync(expected, text);

    [Fact]
    public Task FormatNullConditionalAssignment()
        => AssertFormatAsync(
            expected: """
            x?.y = z;
            """,
            code: """
             x ? . y  =  z ;
            """);

    [Fact]
    public Task TestExtension1()
        => AssertFormatAsync(
            """
            static class C
            {
                extension(string s)
                {
                    public void M()
                    {
                    }
                }
            }
            """,
            """
            static class C
            {
                    extension   (   string   s   )
                        {
                            public  void    M   (   )
                                {
                                }
                        }
            }
            """,
            parseOptions: CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.CSharp14));

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/13163")]
    public Task BlockFollowedByParenthesizedExpression()
        => AssertFormatAsync("""
            class C
            {
                void M()
                {
                    { }
                    (0).ToString();
                }
            }
            """, """
            class C
            {
                void M()
                {
                    { }
                     (0).ToString();
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/13163")]
    public Task BlockFollowedBySimpleExpression()
        => AssertFormatAsync("""
            class C
            {
                void M()
                {
                    { }
                    0.ToString();
                }
            }
            """, """
            class C
            {
                void M()
                {
                    { }
                     0.ToString();
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/13163")]
    public Task IfStatementFollowedByParenthesizedExpression()
        => AssertFormatAsync("""
            class C
            {
                void M()
                {
                    if (true) { }
                    (0).ToString();
                }
            }
            """, """
            class C
            {
                void M()
                {
                    if (true) { }
                     (0).ToString();
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/13163")]
    public Task BlockFollowedByCastExpression()
        => AssertFormatAsync("""
            class C
            {
                void M()
                {
                    { }
                    ((IDisposable)null).Dispose();
                }
            }
            """, """
            class C
            {
                void M()
                {
                    { }
                     ((IDisposable)null).Dispose();
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/25010")]
    public Task LambdaWithCommentAndStatement()
        => AssertFormatAsync("""
            using System;

            public static class Program
            {
                public static void Main()
                {
                    Action x = () =>
                    {
                        // Comment
                        var a = 1;
                        var b = 2;
                    };
                }
            }
            """, """
            using System;

            public static class Program
            {
                public static void Main()
                {
                        Action x = () =>
                        {
                            // Comment
                            var a = 1;
                            var b = 2;
                        };
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/25010")]
    public Task LambdaWithMultipleCommentsAndStatements()
        => AssertFormatAsync("""
            using System;

            public static class Program
            {
                public static void Main()
                {
                    Action x = () =>
                    {
                        var a = 1;

                        // comment
                        var b = 2;
                    };
                }
            }
            """, """
            using System;

            public static class Program
            {
                public static void Main()
                {
                                Action x = () =>
                                {
                                    var a = 1;

                                    // comment
                                    var b = 2;
                                };
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/25010")]
    public Task IfStatementWithCommentNotAffected()
        => AssertFormatAsync("""
            using System;

            public static class Program
            {
                public static void Main()
                {
                    if (true)
                    {
                        var a = 1;

                        // comment
                        var b = 2;
                    }
                }
            }
            """, """
            using System;

            public static class Program
            {
                public static void Main()
                {
                                if (true)
                                {
                                    var a = 1;

                                    // comment
                                    var b = 2;
                                }
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/10500")]
    public Task NestedEmbeddedStatementsOnSameLine_IfIf()
        => AssertFormatAsync("""
            class C
            {
                void M()
                {
                    if (true) if (true)
                    {
                        /* ... */
                    }
                }
            }
            """, """
            class C
            {
                void M()
                {
                    if (true) if (true)
                    {
                        /* ... */
                    }
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/10500")]
    public Task NestedEmbeddedStatementsOnDifferentLines_IfIf()
        => AssertFormatAsync("""
            class C
            {
                void M()
                {
                    if (true)
                        if (true)
                        {
                            /* ... */
                        }
                }
            }
            """, """
            class C
            {
                void M()
                {
                    if (true)
                if (true)
                {
                    /* ... */
                }
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/10500")]
    public Task NestedEmbeddedStatementsOnSameLine_IfUsing()
        => AssertFormatAsync("""
            class C
            {
                void M()
                {
                    if (true) using (null)
                    {
                        /* ... */
                    }
                }
            }
            """, """
            class C
            {
                void M()
                {
                    if (true) using (null)
                    {
                        /* ... */
                    }
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/10500")]
    public Task NestedEmbeddedStatementsOnDifferentLines_IfUsing()
        => AssertFormatAsync("""
            class C
            {
                void M()
                {
                    if (true)
                        using (null)
                        {
                            /* ... */
                        }
                }
            }
            """, """
            class C
            {
                void M()
                {
                    if (true)
                using (null)
                {
                    /* ... */
                }
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/10500")]
    public Task NestedEmbeddedStatementsOnSameLine_WhileFor()
        => AssertFormatAsync("""
            class C
            {
                void M()
                {
                    while (true) for (; ; )
                    {
                        /* ... */
                    }
                }
            }
            """, """
            class C
            {
                void M()
                {
                    while (true) for (;;)
                    {
                        /* ... */
                    }
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/10500")]
    public Task NestedEmbeddedStatementsOnDifferentLines_WhileFor()
        => AssertFormatAsync("""
            class C
            {
                void M()
                {
                    while (true)
                        for (; ; )
                        {
                            /* ... */
                        }
                }
            }
            """, """
            class C
            {
                void M()
                {
                    while (true)
                for (;;)
                {
                    /* ... */
                }
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/10500")]
    public Task NestedEmbeddedStatementsOnSameLine_LockForeach()
        => AssertFormatAsync("""
            class C
            {
                void M()
                {
                    lock (null) foreach (var x in y)
                    {
                        /* ... */
                    }
                }
            }
            """, """
            class C
            {
                void M()
                {
                    lock (null) foreach (var x in y)
                    {
                        /* ... */
                    }
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/10500")]
    public Task NestedEmbeddedStatementsOnDifferentLines_LockForeach()
        => AssertFormatAsync("""
            class C
            {
                void M()
                {
                    lock (null)
                        foreach (var x in y)
                        {
                            /* ... */
                        }
                }
            }
            """, """
            class C
            {
                void M()
                {
                    lock (null)
                foreach (var x in y)
                {
                    /* ... */
                }
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/10500")]
    public Task NestedEmbeddedStatementsOnSameLine_FixedDo()
        => AssertFormatAsync("""
            class C
            {
                void M()
                {
                    unsafe
                    {
                        fixed (int* p = &i) do
                        {
                            /* ... */
                        } while (true);
                    }
                }
            }
            """, """
            class C
            {
                void M()
                {
                    unsafe
                    {
                        fixed (int* p = &i) do
                        {
                            /* ... */
                        } while (true);
                    }
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/10500")]
    public Task NestedEmbeddedStatementsOnDifferentLines_FixedDo()
        => AssertFormatAsync("""
            class C
            {
                void M()
                {
                    unsafe
                    {
                        fixed (int* p = &i)
                            do
                            {
                                /* ... */
                            } while (true);
                    }
                }
            }
            """, """
            class C
            {
                void M()
                {
                    unsafe
                    {
                        fixed (int* p = &i)
                do
                {
                    /* ... */
                } while (true);
                    }
                }
            }
            """);
}
