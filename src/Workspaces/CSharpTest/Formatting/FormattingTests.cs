// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.CodeAnalysis.CSharp.Formatting;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;
using static Microsoft.CodeAnalysis.CSharp.Formatting.CSharpFormattingOptions;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.Formatting
{
    public class FormattingEngineTests : CSharpFormattingTestBase
    {
        [Fact, Trait(Traits.Feature, Traits.Features.Formatting)]
        public void Format1()
        {
            AssertFormat("namespace A { }", "namespace A{}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Formatting)]
        public void Format2()
        {
            var content = @"class A {
            }";

            var expected = @"class A
{
}";
            AssertFormat(expected, content);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Formatting)]
        public void Format3()
        {
            var content = @"class A
            {        
int             i               =               20          ;           }";

            var expected = @"class A
{
    int i = 20;
}";

            AssertFormat(expected, content);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Formatting)]
        public void Format4()
        {
            var content = @"class A
            {        
int             i               =               20          ;           int             j           =           1           +           2       ;
                        T           .               S           =           Test            (           10              )           ;
                        }";

            var expected = @"class A
{
    int i = 20; int j = 1 + 2;
    T.S           =           Test(           10              );
}";

            AssertFormat(expected, content);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Formatting)]
        public void Format5()
        {
            var content = @"class A
            {        
    List                    <           int             >                Method              <               TArg                ,           TArg2           >               (                   TArg                a,              TArg2                   b               )
                    {
int             i               =               20          ;           int             j           =           1           +           2       ;
                        T           .               S           =           Test            (           10              )           ;
                        }           }";

            var expected = @"class A
{
    List<int> Method<TArg, TArg2>(TArg a, TArg2 b)
    {
        int i = 20; int j = 1 + 2;
        T.S = Test(10);
    }
}";

            AssertFormat(expected, content);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Formatting)]
        public void Format6()
        {
            var content = @"class A
            {        
A           a               =               new             A                   {
                   Property1             =                               1,                     Property2               =                       3,
        Property3       =             {         1       ,           2           ,           3   }           };
    }";

            var expected = @"class A
{
    A a = new A
    {
        Property1 = 1,
        Property2 = 3,
        Property3 = { 1, 2, 3 }
    };
}";

            AssertFormat(expected, content);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Formatting)]
        public void Format7()
        {
            var content = @"class A
            {        
    var             a           =           from        i       in          new        [  ]     {           1           ,       2           ,       3       }       where           i       >       10          select      i           ;           
}";

            var expected = @"class A
{
    var a = from i in new[] { 1, 2, 3 } where i > 10 select i;
}";

            AssertFormat(expected, content);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Formatting)]
        public void Format8()
        {
            var content = @"class A
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
}";

            var expected = @"class A
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
}";

            AssertFormat(expected, content);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Formatting)]
        public void Format9()
        {
            var content = @"class A
            {        
void Method()
{
                            if (true)                             {                             }                             else if (false)                              {                             }
}
}";

            var expected = @"class A
{
    void Method()
    {
        if (true) { } else if (false) { }
    }
}";

            AssertFormat(expected, content);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Formatting)]
        public void Format10()
        {
            var content = @"class A
            {        
    var             a           =           from        i       in          new        [  ]     {           1           ,       2           ,       3       }       
where           i       >       10          select      i           ;           
}";

            var expected = @"class A
{
    var a = from i in new[] { 1, 2, 3 }
            where i > 10
            select i;
}";

            AssertFormat(expected, content);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Formatting)]
        public void ObjectInitializer()
        {
            AssertFormat(@"public class C
{
    public C()
    {
        C c = new C()
        {
            c = new C()
            {
                foo = 1,
                bar = 2
            }
        };
    }
}", @"public class C
{
    public C()
    {
        C c = new C()
                        {
                                        c = new C()
        {
                            foo = 1,
                bar = 2
        }
                        };
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Formatting)]
        public void AnonymousType()
        {
            AssertFormat(@"class C
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
}", @"class C
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
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Formatting)]
        public void MultilineLambda()
        {
            AssertFormat(@"class C
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
}", @"class C
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
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Formatting)]
        public void AnonymousMethod()
        {
            AssertFormat(@"class C
{
    C()
    {
        timer.Tick += delegate (object sender, EventArgs e)
                        {
                            MessageBox.Show(this, ""Timer ticked"");
                        };
    }
}", @"class C
{
    C()
    {
        timer.Tick += delegate(object sender, EventArgs e)
                        {
  MessageBox.Show(this, ""Timer ticked"");
                        };
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Formatting)]
        public void Scen1()
        {
            AssertFormat(@"namespace Namespace1
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

            Method(i, ""hello"", true);

        }

        static void Method(int i, string s, bool b)
        {
        }
    }
}", @"namespace Namespace1
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

Method(i,""hello"",true);

}

static void Method(int i, string s, bool b)
{
}
}
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Formatting)]
        public void Scen2()
        {
            AssertFormat(@"namespace MyNamespace
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
}", @"namespace MyNamespace
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
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Formatting)]
        public void Scen3()
        {
            AssertFormat(@"namespace Namespace1
{
    class Program
    {
        static void Main()
        {
            Program p = new Program();
        }
    }
}", @"namespace Namespace1
{
class Program
{
static void Main()
{
Program p=new Program();
}
}
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Formatting)]
        public void Scen4()
        {
            AssertFormat(@"class Class1
{
    //	public void foo()
    //	{
    //		// TODO: Add the implementation for Class1.foo() here.
    //	
    //	}
}", @"class Class1
{
    //	public void foo()
//	{
//		// TODO: Add the implementation for Class1.foo() here.
//	
//	}
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Formatting)]
        public void Scen5()
        {
            AssertFormat(@"class Class1
{
    public void Method()
    {
        {
            int i = 0;
            System.Console.WriteLine();
        }
    }
}", @"class Class1
{
public void Method()
{
{
int i = 0;
                    System.Console.WriteLine();
}
}
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Formatting)]
        public void Scen6()
        {
            AssertFormat(@"namespace Namespace1
{
    class OuterClass
    {
        class InnerClass
        {
        }
    }
}", @"namespace Namespace1
{
class OuterClass
{
class InnerClass
{
}
}
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Formatting)]
        public void Scen7()
        {
            AssertFormat(@"class Class1
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
}", @"class Class1
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
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Formatting)]
        public void Scen8()
        {
            AssertFormat(@"class Class1
{
    public void Method()
    {
        int i = 10;
    }
}", @"class Class1
      {
                public void Method()
        {
                    int i = 10;
       }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Formatting)]
        public void IndentStatementsInMethod()
        {
            AssertFormat(@"class C
{
    void Foo()
    {
        int x = 0;
        int y = 0;
        int z = 0;
    }
}", @"class C
{
    void Foo()
    {
        int x = 0;
            int y = 0;
      int z = 0;
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Formatting)]
        public void IndentFieldsInClass()
        {
            AssertFormat(@"class C
{
    int a = 10;
    int b;
    int c;
}", @"class C
{
        int a = 10;
      int b;
  int c;
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Formatting)]
        public void IndentUserDefaultSettingTest()
        {
            AssertFormat(@"class Class2
{
    public void nothing()
    {
        nothing_again(() =>
            {
                Console.WriteLine(""Nothing"");
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
}", @"class Class2
    {
    public void nothing()
        {
    nothing_again(() =>
        {
        Console.WriteLine(""Nothing"");
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
    }");
        }

        [WorkItem(766133, "DevDiv")]
        [Fact, Trait(Traits.Feature, Traits.Features.Formatting)]
        public void RelativeIndentationToFirstTokenInBaseTokenWithObjectInitializers()
        {
            var changingOptions = new Dictionary<OptionKey, object>();
            changingOptions.Add(CSharpFormattingOptions.NewLinesForBracesInObjectCollectionArrayInitializers, false);
            AssertFormat(@"class Program
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
}", @"class Program
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
}", false, changingOptions);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Formatting)]
        public void RemoveSpacingAroundBinaryOperatorsShouldMakeAtLeastOneSpaceForIsAndAsKeywords()
        {
            var changingOptions = new Dictionary<OptionKey, object>();
            changingOptions.Add(CSharpFormattingOptions.SpacingAroundBinaryOperator, BinaryOperatorSpacingOptions.Remove);
            AssertFormat(@"class Class2
{
    public void nothing()
    {
        var a = 1*2+3-4/5;
        a+=1;
        object o = null;
        string s = o as string;
        bool b = o is string;
    }
}", @"class Class2
    {
    public void nothing()
        {
            var a = 1   *   2  +   3   -  4  /  5;
            a    += 1;
            object o = null;
            string s = o        as       string;
            bool b   = o        is       string;
        }
    }", false, changingOptions);
        }

        [WorkItem(772298, "DevDiv")]
        [Fact, Trait(Traits.Feature, Traits.Features.Formatting)]
        public void IndentUserSettingNonDefaultTest_OpenBracesOfLambdaWithNoNewLine()
        {
            var changingOptions = new Dictionary<OptionKey, object>();
            changingOptions.Add(CSharpFormattingOptions.IndentBraces, true);
            changingOptions.Add(CSharpFormattingOptions.IndentBlock, false);
            changingOptions.Add(CSharpFormattingOptions.IndentSwitchSection, false);
            changingOptions.Add(CSharpFormattingOptions.IndentSwitchCaseSection, false);
            changingOptions.Add(CSharpFormattingOptions.NewLinesForBracesInLambdaExpressionBody, false);
            changingOptions.Add(CSharpFormattingOptions.LabelPositioning, LabelPositionOptions.LeftMost);
            AssertFormat(@"class Class2
    {
    public void nothing()
        {
    nothing_again(() => {
    Console.WriteLine(""Nothing"");
    });
        }
    }", @"class Class2
{
    public void nothing()
    {
        nothing_again(() =>
            {
                Console.WriteLine(""Nothing"");
            });
    }
}", false, changingOptions);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Formatting)]
        public void IndentUserSettingNonDefaultTest()
        {
            var changingOptions = new Dictionary<OptionKey, object>();
            changingOptions.Add(CSharpFormattingOptions.IndentBraces, true);
            changingOptions.Add(CSharpFormattingOptions.IndentBlock, false);
            changingOptions.Add(CSharpFormattingOptions.IndentSwitchSection, false);
            changingOptions.Add(CSharpFormattingOptions.IndentSwitchCaseSection, false);
            changingOptions.Add(CSharpFormattingOptions.LabelPositioning, LabelPositionOptions.LeftMost);
            AssertFormat(@"class Class2
    {
    public void nothing()
        {
    nothing_again(() =>
        {
        Console.WriteLine(""Nothing"");
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
    }", @"class Class2
{
    public void nothing()
    {
        nothing_again(() =>
            {
                Console.WriteLine(""Nothing"");
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
}", false, changingOptions);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Formatting)]
        public void TestWrappingDefault()
        {
            AssertFormat(@"class Class5
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
}", @"class Class5
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
    }");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Formatting)]
        public void TestWrappingNonDefault_FormatBlock()
        {
            var changingOptions = new Dictionary<OptionKey, object>();
            changingOptions.Add(CSharpFormattingOptions.WrappingPreserveSingleLine, false);
            AssertFormat(@"class Class5
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
    void foo()
    {
        int xx = 0; int zz = 0;
    }
}
class foo
{
    int x = 0;
}", @"class Class5
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
        void foo() { int xx = 0; int zz = 0;}
}
class foo{int x = 0;}", false, changingOptions);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Formatting)]
        public void TestWrappingNonDefault_FormatStatmtMethDecl()
        {
            var changingOptions = new Dictionary<OptionKey, object>();
            changingOptions.Add(CSharpFormattingOptions.WrappingKeepStatementsOnSingleLine, false);
            AssertFormat(@"class Class5
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
    void foo() { int y = 0; int z = 0; }
}
class foo
{
    int x = 0;
}", @"class Class5
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
        void foo(){int y=0; int z =0 ;}
}
class foo
{
    int x = 0;
}", false, changingOptions);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Formatting)]
        public void TestWrappingNonDefault()
        {
            var changingOptions = new Dictionary<OptionKey, object>();
            changingOptions.Add(CSharpFormattingOptions.WrappingPreserveSingleLine, false);
            changingOptions.Add(CSharpFormattingOptions.WrappingKeepStatementsOnSingleLine, false);
            AssertFormat(@"class Class5
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
class foo
{
    int x = 0;
}", @"class Class5
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
class foo{int x = 0;}", false, changingOptions);
        }

        [WorkItem(991480)]
        [Fact, Trait(Traits.Feature, Traits.Features.Formatting)]
        public void TestLeaveStatementMethodDeclarationSameLineNotAffectingForStatement()
        {
            var changingOptions = new Dictionary<OptionKey, object>();
            changingOptions.Add(CSharpFormattingOptions.WrappingKeepStatementsOnSingleLine, false);
            AssertFormat(@"class Program
{
    static void Main(string[] args)
    {
        for (int d = 0; d < 10; ++d)
        { }
    }
}", @"class Program
{
    static void Main(string[] args)
    {
        for (int d = 0; d < 10; ++d) { }
    }
}", false, changingOptions);
        }

        [WorkItem(751789, "DevDiv")]
        [Fact, Trait(Traits.Feature, Traits.Features.Formatting)]
        public void NewLineForOpenBracesDefault()
        {
            AssertFormat(@"class f00
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
            System.Console.WriteLine("""");
        }
        else
        {
        }
        timer.Tick += delegate (object sender, EventArgs e)


{
    MessageBox.Show(this, ""Timer ticked"");
};

        var obj1 = new foo
        {
        };

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
    public class foo : System.Object



    {
        public int f { get; set; }
    }
}", @"class f00
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
            System.Console.WriteLine("""");
        }
else 
{
}
        timer.Tick += delegate (object sender, EventArgs e)     


{
            MessageBox.Show(this, ""Timer ticked"");
        };

var obj1 = new foo         
            {
                            };

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
public class foo : System.Object



{
    public int f { get; set; }
}
}");
        }

        [WorkItem(751789, "DevDiv")]
        [Fact, Trait(Traits.Feature, Traits.Features.Formatting)]
        public void NewLineForOpenBracesNonDefault()
        {
            var changingOptions = new Dictionary<OptionKey, object>();
            changingOptions.Add(CSharpFormattingOptions.NewLinesForBracesInTypes, false);
            changingOptions.Add(CSharpFormattingOptions.NewLinesForBracesInMethods, false);
            changingOptions.Add(CSharpFormattingOptions.NewLinesForBracesInAnonymousMethods, false);
            changingOptions.Add(CSharpFormattingOptions.NewLinesForBracesInControlBlocks, false);
            changingOptions.Add(CSharpFormattingOptions.NewLinesForBracesInAnonymousTypes, false);
            changingOptions.Add(CSharpFormattingOptions.NewLinesForBracesInObjectCollectionArrayInitializers, false);
            changingOptions.Add(CSharpFormattingOptions.NewLinesForBracesInLambdaExpressionBody, false);
            AssertFormat(@"class f00 {
    void br() {
        Func<int, int> ret = x => {
            return x + 1;
        };
        var obj = new {
            // ...
        };
        if (true) {
            System.Console.WriteLine("""");
        }
        else {
        }
        timer.Tick += delegate (object sender, EventArgs e) {
            MessageBox.Show(this, ""Timer ticked"");
        };

        var obj1 = new foo {
        };

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
    }
}

namespace NS1 {
    public class foo : System.Object {
    }
}", @"class f00
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
            System.Console.WriteLine("""");
        }
else 
{
}
        timer.Tick += delegate (object sender, EventArgs e)     


{
            MessageBox.Show(this, ""Timer ticked"");
        };

var obj1 = new foo         
            {
                            };

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
public class foo : System.Object



{
}
}", false, changingOptions);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Formatting)]
        public void NewLineForKeywordDefault()
        {
            AssertFormat(@"class c
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
}",
                @"class c
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
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Formatting)]
        public void NewLineForKeywordNonDefault()
        {
            var changingOptions = new Dictionary<OptionKey, object>();
            changingOptions.Add(CSharpFormattingOptions.NewLineForElse, false);
            changingOptions.Add(CSharpFormattingOptions.NewLineForCatch, false);
            changingOptions.Add(CSharpFormattingOptions.NewLineForFinally, false);
            AssertFormat(@"class c
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
}", @"class c
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
}", false, changingOptions);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Formatting)]
        public void NewLineForExpressionDefault()
        {
            AssertFormat(@"class f00
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
}", @"class f00
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
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Formatting)]
        public void NewLineForExpressionNonDefault()
        {
            var changingOptions = new Dictionary<OptionKey, object>();
            changingOptions.Add(CSharpFormattingOptions.NewLineForMembersInObjectInit, false);
            changingOptions.Add(CSharpFormattingOptions.NewLineForMembersInAnonymousTypes, false);
            changingOptions.Add(CSharpFormattingOptions.NewLineForClausesInQuery, false);
            AssertFormat(@"class f00
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
}", @"class f00
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
}", false, changingOptions);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Formatting)]
        public void Enums_Bug2586()
        {
            AssertFormat(@"enum E
{
    a = 10,
    b,
    c
}", @"enum E
{
        a = 10,
      b,
  c
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Formatting)]
        public void DontInsertLineBreaksInSingleLineEnum()
        {
            AssertFormat(@"enum E { a = 10, b, c }", @"enum E { a = 10, b, c }");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Formatting)]
        public void AlreadyFormattedSwitchIsNotFormatted_Bug2588()
        {
            AssertFormat(@"class C
{
    void M()
    {
        switch (3)
        {
            case 0:
                break;
        }
    }
}", @"class C
{
    void M()
    {
        switch (3)
        {
            case 0:
                break;
        }
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Formatting)]
        public void BreaksAreAlignedInSwitchCasesFormatted_Bug2587()
        {
            AssertFormat(@"class C
{
    void M()
    {
        switch (3)
        {
            case 0:
                break;
        }
    }
}", @"class C
{
    void M()
    {
        switch (3)
        {
            case 0:
                    break;
        }
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Formatting)]
        public void BreaksAndBracesAreAlignedInSwitchCasesWithBracesFormatted_Bug2587()
        {
            AssertFormat(@"class C
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
}", @"class C
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
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Formatting)]
        public void LineBreaksAreNotInsertedForSwitchCasesOnASingleLine1()
        {
            AssertFormat(@"class C
{
    void M()
    {
        switch (3)
        {
            case 0: break;
            default: break;
        }
    }
}", @"class C
{
    void M()
    {
        switch (3)
        {
            case 0: break;
            default: break;
        }
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Formatting)]
        public void LineBreaksAreNotInsertedForSwitchCasesOnASingleLine2()
        {
            AssertFormat(@"class C
{
    void M()
    {
        switch (3)
        {
            case 0: { break; }
            default: { break; }
        }
    }
}", @"class C
{
    void M()
    {
        switch (3)
        {
            case 0: { break; }
            default: { break; }
        }
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Formatting)]
        public void FormatLabelAndGoto1_Bug2588()
        {
            AssertFormat(@"class C
{
    void M()
    {
    Foo:
        goto Foo;
    }
}", @"class C
{
    void M()
    {
Foo:
goto Foo;
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Formatting)]
        public void FormatLabelAndGoto2_Bug2588()
        {
            AssertFormat(@"class C
{
    void M()
    {
        int x = 0;
    Foo:
        goto Foo;
    }
}", @"class C
{
    void M()
    {
int x = 0;
Foo:
goto Foo;
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Formatting)]
        public void FormatNestedLabelAndGoto1_Bug2588()
        {
            AssertFormat(@"class C
{
    void M()
    {
        if (true)
        {
        Foo:
            goto Foo;
        }
    }
}", @"class C
{
    void M()
    {
if (true)
{
Foo:
goto Foo;
}
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Formatting)]
        public void FormatNestedLabelAndGoto2_Bug2588()
        {
            AssertFormat(@"class C
{
    void M()
    {
        if (true)
        {
            int x = 0;
        Foo:
            goto Foo;
        }
    }
}", @"class C
{
    void M()
    {
if (true)
{
int x = 0;
Foo:
goto Foo;
}
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Formatting)]
        public void AlreadyFormattedGotoLabelIsNotFormatted1_Bug2588()
        {
            AssertFormat(@"class C
{
    void M()
    {
    Foo:
        goto Foo;
    }
}", @"class C
{
    void M()
    {
    Foo:
        goto Foo;
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Formatting)]
        public void AlreadyFormattedGotoLabelIsNotFormatted2_Bug2588()
        {
            AssertFormat(@"class C
{
    void M()
    {
    Foo: goto Foo;
    }
}", @"class C
{
    void M()
    {
    Foo: goto Foo;
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Formatting)]
        public void AlreadyFormattedGotoLabelIsNotFormatted3_Bug2588()
        {
            AssertFormat(@"class C
{
    void M()
    {
        int x = 0;
    Foo:
        goto Foo;
    }
}", @"class C
{
    void M()
    {
        int x = 0;
    Foo:
        goto Foo;
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Formatting)]
        public void DontAddLineBreakBeforeWhere1_Bug2582()
        {
            AssertFormat(@"class C
{
    void M<T>() where T : I
    {
    }
}", @"class C
{
    void M<T>() where T : I
    {
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Formatting)]
        public void DontAddLineBreakBeforeWhere2_Bug2582()
        {
            AssertFormat(@"class C<T> where T : I
{
}", @"class C<T> where T : I
{
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Formatting)]
        public void DontAddSpaceAfterUnaryMinus()
        {
            AssertFormat(@"class C
{
    void M()
    {
        int x = -1;
    }
}", @"class C
{
    void M()
    {
        int x = -1;
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Formatting)]
        public void DontAddSpaceAfterUnaryPlus()
        {
            AssertFormat(@"class C
{
    void M()
    {
        int x = +1;
    }
}", @"class C
{
    void M()
    {
        int x = +1;
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Formatting)]
        [WorkItem(545909, "DevDiv")]
        public void DontAddSpaceAfterIncrement()
        {
            var code = @"class C
{
    void M(int[] i)
    {
        ++i[0];
    }
}";
            AssertFormat(code, code);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Formatting)]
        [WorkItem(545909, "DevDiv")]
        public void DontAddSpaceBeforeIncrement()
        {
            var code = @"class C
{
    void M(int[] i)
    {
        i[0]++;
    }
}";
            AssertFormat(code, code);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Formatting)]
        [WorkItem(545909, "DevDiv")]
        public void DontAddSpaceAfterDecrement()
        {
            var code = @"class C
{
    void M(int[] i)
    {
        --i[0];
    }
}";
            AssertFormat(code, code);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Formatting)]
        [WorkItem(545909, "DevDiv")]
        public void DontAddSpaceBeforeDecrement()
        {
            var code = @"class C
{
    void M(int[] i)
    {
        i[0]--;
    }
}";
            AssertFormat(code, code);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Formatting)]
        public void Anchoring()
        {
            AssertFormat(@"class C
{
    void M()
    {
        Console.WriteLine(""Foo"",
            0, 1,
                2);
    }
}", @"class C
{
    void M()
    {
                    Console.WriteLine(""Foo"",
                        0, 1,
                            2);
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Formatting)]
        public void Exclamation()
        {
            AssertFormat(@"class C
{
    void M()
    {
        if (!true) ;
    }
}", @"class C
{
    void M()
    {
        if (    !           true        )           ;
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Formatting)]
        public void StartAndEndTrivia()
        {
            AssertFormat(@"


class C { }




", @"      
        
        
class C { }     
        
        
        
                    
");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Formatting)]
        public void FirstTriviaAndAnchoring1()
        {
            AssertFormat(@"
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



", @"      
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



");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Formatting)]
        public void FirstTriviaAndAnchoring2()
        {
            AssertFormat(@"
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



", @"          
namespace N {
        class C {       
                        int         i           =           
                            1       
                                +       
                                    3;
}
}               

            

");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Formatting)]
        public void FirstTriviaAndAnchoring3()
        {
            AssertFormat(@"

class C
{
    int i =
        1
            +
                3;
}



", @"      
            
        class C {       
                        int         i           =           
                            1       
                                +       
                                    3;
}
        


");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Formatting)]
        public void Base1()
        {
            AssertFormat(@"class C
{
    C() : base()
    {
    }
}", @"      class             C
            {
    C   (   )  :    base    (       )  
            {
            }
    }           ");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Formatting)]
        public void This1()
        {
            AssertFormat(@"class C
{
    C(int i) : this()
    {
    }

    C() { }
}", @"      class             C
            {
    C   (   int         i   )  :    this    (       )  
            {
            }

        C       (           )               {                       }
    }           ");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Formatting)]
        public void QueryExpression1()
        {
            AssertFormat(@"class C
{
    int Method()
    {
        var q =
            from c in from b in cs select b select c;
    }
}", @"      class             C
            {
        int Method()
        {
            var q = 
                from c in                  from b in cs                         select b     select c;
        }
    }           ");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Formatting)]
        public void QueryExpression2()
        {
            AssertFormat(@"class C
{
    int Method()
    {
        var q = from c in
                    from b in cs
                    select b
                select c;
    }
}", @"      class             C
            {
        int Method()
        {
            var q = from c in 
                            from b in cs
                            select b
    select c;
        }
    }           ");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Formatting)]
        public void QueryExpression3()
        {
            AssertFormat(@"class C
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
}", @"class C
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
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Formatting)]
        public void QueryExpression4()
        {
            AssertFormat(@"class C
{
    int Method()
    {
        var q =
            from c in
                from b in cs
                select b
            select c;
    }
}", @"      class             C
            {
        int Method()
        {
            var q = 
                from c in 
                            from b in cs
                            select b
    select c;
        }
    }           ");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Formatting)]
        public void Label1()
        {
            AssertFormat(@"class C
{
    int Method()
    {
    L: int i = 10;
    }
}", @"      class             C
            {
        int Method()
        {
                L           :                   int         i           =           10                  ;
        }
    }           ");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Formatting)]
        public void Label2()
        {
            AssertFormat(@"class C
{
    int Method()
    {
        int x = 1;
    L: int i = 10;
    }
}", @"      class             C
            {
        int Method()
        {
int             x               =               1               ;
                L           :                   int         i           =           10                  ;
        }
    }           ");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Formatting)]
        public void Label3()
        {
            AssertFormat(@"class C
{
    int Method()
    {
        int x = 1;
    L:
        int i = 10;
    }
}", @"      class             C
            {
        int Method()
        {
int             x               =               1               ;
                L           :                   
int         i           =           10                  ;
        }
    }           ");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Formatting)]
        public void Label4()
        {
            AssertFormat(@"class C
{
    int Method()
    {
        int x = 1;
    L: int i = 10;
        int next = 30;
    }
}", @"      class             C
            {
        int Method()
        {
int             x               =               1               ;
                L           :                   int         i           =           10                  ;
                                    int             next            =                   30;
        }
    }           ");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Formatting)]
        public void Label5()
        {
            AssertFormat(@"class C
{
    int Method()
    {
    L: int i = 10;
        int next = 30;
    }
}", @"      class             C
            {
        int Method()
        {
                L           :                   int         i           =           10                  ;
                                    int             next            =                   30;
        }
    }           ");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Formatting)]
        public void Label6()
        {
            AssertFormat(@"class C
{
    int Method()
    {
    L:
        int i = 10;
        int next = 30;
    }
}", @"      class             C
            {
        int Method()
        {
                L           :
int         i           =           10                  ;
                                    int             next            =                   30;
        }
    }           ");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Formatting)]
        public void Label7()
        {
            AssertFormat(@"class C
{
    int Method()
    {
        int i2 = 1;
    L:
        int i = 10;
        int next = 30;
    }
}", @"      class             C
            {
        int Method()
        {
    int     i2              =       1   ;
                L           :
int         i           =           10                  ;
                                    int             next            =                   30;
        }
    }           ");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Formatting)]
        public void Label8()
        {
            AssertFormat(@"class C
{
    int Method()
    {
    L:
        int i =
            10;
    }
}", @"      class             C
            {
        int Method()
        {
                L:
                    int i =
                        10;
        }
    }           ");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Formatting)]
        public void AutoProperty()
        {
            AssertFormat(@"class Class
{
    private int Age { get; set; }
    public string Names { get; set; }
}", @"         class Class
{
                                  private       int Age{get;                set;                 }
            public string Names                     {                        get;                      set;}
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Formatting)]
        public void NormalPropertyGet()
        {
            AssertFormat(@"class Class
{
    private string name;
    public string Names
    {
        get
        {
            return name;
        }
    }
}", @"class Class
{
    private string name;
                          public string Names
    {
                                         get
                                    {
                                      return name;
                                   }
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Formatting)]
        public void NormalPropertyBoth()
        {
            AssertFormat(@"class Class
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
}", @"class Class
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
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Formatting)]
        public void ErrorHandling1()
        {
            AssertFormat(@"class C
{
    int Method()
    {
        int a           b c;
    }
}", @"      class             C
            {
        int Method()
        {
                int             a           b               c           ;
        }
    }           ");
        }

        [WorkItem(537763, "DevDiv")]
        [Fact, Trait(Traits.Feature, Traits.Features.Formatting)]
        public void NullableType()
        {
            AssertFormat(@"class Program
{
    static void Main(string[] args)
    {
        int? i = 10;
    }
}", @"class Program
{
    static void Main(string[] args)
    {
        int         ? i = 10;
    }
}");
        }

        [WorkItem(537766, "DevDiv")]
        [Fact, Trait(Traits.Feature, Traits.Features.Formatting)]
        public void SuppressWrappingOnBraces()
        {
            AssertFormat(@"class Class1
{ }
", @"class Class1
{}
");
        }

        [WorkItem(537824, "DevDiv")]
        [Fact, Trait(Traits.Feature, Traits.Features.Formatting)]
        public void DoWhile()
        {
            AssertFormat(@"public class Class1
{
    void Foo()
    {
        do
        {
        } while (true);
    }
}
", @"public class Class1
{
    void Foo()
    {
        do
        {
        }while (true);
    }
}
");
        }

        [WorkItem(537774, "DevDiv")]
        [Fact, Trait(Traits.Feature, Traits.Features.Formatting)]
        public void SuppressWrappingBug()
        {
            AssertFormat(@"class Class1
{
    int Foo()
    {
        return 0;
    }
}
", @"class Class1
{
    int Foo()
    {return 0;
    }
}
");
        }

        [WorkItem(537768, "DevDiv")]
        [Fact, Trait(Traits.Feature, Traits.Features.Formatting)]
        public void PreserveLineForAttribute()
        {
            AssertFormat(@"class Class1
{
    [STAThread]
    static void Main(string[] args)
    {
    }
}
", @"class Class1
{
    [STAThread]
static void Main(string[] args)
    {
    }
}
");
        }

        [WorkItem(537878, "DevDiv")]
        [Fact, Trait(Traits.Feature, Traits.Features.Formatting)]
        public void NoFormattingOnMissingTokens()
        {
            AssertFormat(@"namespace ClassLibrary1
{
    class Class1
    {
        void Foo()
        {
            if (true)
        }
    }
}
", @"namespace ClassLibrary1
{
    class Class1
    {
        void Foo()
        {
            if (true)
        }
    }
}
");
        }

        [WorkItem(537783, "DevDiv")]
        [Fact, Trait(Traits.Feature, Traits.Features.Formatting)]
        public void UnaryExpression()
        {
            AssertFormat(@"class Program
{
    static void Main(string[] args)
    {
        int a = 6;
        a = a++ + 5;
    }
}
", @"class Program
{
    static void Main(string[] args)
    {
        int a = 6;
        a = a++ + 5;
    }
}
");
        }

        [WorkItem(537885, "DevDiv")]
        [Fact, Trait(Traits.Feature, Traits.Features.Formatting)]
        public void Pointer()
        {
            AssertFormat(@"class Program
{
    static void Main(string[] args)
    {
        int* p;
    }
}
", @"class Program
{
    static void Main(string[] args)
    {
        int* p;
    }
}
");
        }

        [WorkItem(537886, "DevDiv")]
        [Fact, Trait(Traits.Feature, Traits.Features.Formatting)]
        public void Tild()
        {
            AssertFormat(@"class Program
{
    static void Main(string[] args)
    {
        int j = 103;
        j = ~7;
    }
}
", @"class Program
{
    static void Main(string[] args)
    {
        int j = 103;
        j = ~7;
    }
}
");
        }

        [WorkItem(537884, "DevDiv")]
        [Fact, Trait(Traits.Feature, Traits.Features.Formatting)]
        public void ArrayInitializer1()
        {
            AssertFormat(@"class Program
{
    static void Main(string[] args)
    {
        int[] arr = {1,2,
        3,4
        };
    }
}
", @"class Program
{
    static void Main(string[] args)
    {
        int[] arr = {1,2,
        3,4
        };
    }
}
");
        }

        [WorkItem(537884, "DevDiv")]
        [Fact, Trait(Traits.Feature, Traits.Features.Formatting)]
        public void ArrayInitializer2()
        {
            AssertFormat(@"class Program
{
    static void Main(string[] args)
    {
        int[] arr = new int[] {1,2,
        3,4
        };
    }
}
", @"class Program
{
    static void Main(string[] args)
    {
        int[] arr =  new int [] {1,2,
        3,4
        };
    }
}
");
        }

        [WorkItem(537884, "DevDiv")]
        [Fact, Trait(Traits.Feature, Traits.Features.Formatting)]
        public void ImplicitArrayInitializer()
        {
            AssertFormat(@"class Program
{
    static void Main(string[] args)
    {
        var arr = new[] {1,2,
        3,4
        };
    }
}
", @"class Program
{
    static void Main(string[] args)
    {
        var arr = new [] {1,2,
        3,4
        }           ;
    }
}
");
        }

        [WorkItem(537884, "DevDiv")]
        [Fact, Trait(Traits.Feature, Traits.Features.Formatting)]
        public void CollectionInitializer()
        {
            AssertFormat(@"class Program
{
    static void Main(string[] args)
    {
        var arr = new List<int> {1,2,
        3,4
        };
    }
}
", @"class Program
{
    static void Main(string[] args)
    {
        var arr = new List<int> {1,2,
        3,4
        };
    }
}
");
        }

        [WorkItem(537916, "DevDiv")]
        [Fact, Trait(Traits.Feature, Traits.Features.Formatting)]
        public void AddressOfOperator()
        {
            AssertFormat(@"unsafe class Class1
{
    void Method()
    {
        int a = 12;
        int* p = &a;
    }
}
", @"unsafe class Class1
{
    void Method()
    {
        int a = 12;
        int* p = &a;
    }
}
");
        }

        [WorkItem(537885, "DevDiv")]
        [Fact, Trait(Traits.Feature, Traits.Features.Formatting)]
        public void DereferenceOperator()
        {
            AssertFormat(@"unsafe class Class1
{
    void Method()
    {
        int a = 12;
        int* p = &a;
        Console.WriteLine(*p);
    }
}
", @"unsafe class Class1
{
    void Method()
    {
        int a = 12;
        int* p = & a;
        Console.WriteLine(* p);
    }
}
");
        }

        [WorkItem(537905, "DevDiv")]
        [Fact, Trait(Traits.Feature, Traits.Features.Formatting)]
        public void Namespaces()
        {
            AssertFormat(@"using System;
using System.Data;", @"using System; using System.Data;");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Formatting)]
        public void NamespaceDeclaration()
        {
            AssertFormat(@"namespace N
{
}", @"namespace N
    {
}");
        }

        [WorkItem(537902, "DevDiv")]
        [Fact, Trait(Traits.Feature, Traits.Features.Formatting)]
        public void DoWhile1()
        {
            AssertFormat(@"class Program
{
    static void Main(string[] args)
    {
        do { }
        while (i < 4);
    }
}", @"class Program
{
    static void Main(string[] args)
    {
        do { }
        while (i < 4);
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Formatting)]
        public void NewConstraint()
        {
            AssertFormat(@"class Program
{
    void Test<T>(T t) where T : new()
    {
    }
}", @"class Program
{
    void Test<T>(T t) where T : new (   )
    {
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Formatting)]
        public void UnaryExpressionWithInitializer()
        {
            AssertFormat(@"using System;
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
}", @"using System;
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
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Formatting)]
        public void Attributes1()
        {
            AssertFormat(@"class Program
{
    [Flags]
    public void Method() { }
}", @"class Program
{
        [   Flags       ]       public       void       Method      (       )           {           }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Formatting)]
        public void Attributes2()
        {
            AssertFormat(@"class Program
{
    [Flags]
    public void Method() { }
}", @"class Program
{
        [   Flags       ]
public       void       Method      (       )           {           }
}");
        }

        [WorkItem(538288, "DevDiv")]
        [Fact, Trait(Traits.Feature, Traits.Features.Formatting)]
        public void ColonColon1()
        {
            AssertFormat(@"class Program
{
    public void Method()
    {
        throw new global::System.NotImplementedException();
    }
}", @"class Program
{
public       void       Method      (       )           {
    throw new global :: System.NotImplementedException();
}
}");
        }

        [WorkItem(538354, "DevDiv")]
        [Fact, Trait(Traits.Feature, Traits.Features.Formatting)]
        public void BugFix3939()
        {
            AssertFormat(@"using
      System.
          Collections.
              Generic;", @"                  using
                        System.
                            Collections.
                                Generic;");
        }

        [WorkItem(538354, "DevDiv")]
        [Fact, Trait(Traits.Feature, Traits.Features.Formatting)]
        public void Tab1()
        {
            AssertFormat(@"using System;", @"			using System;");
        }

        [WorkItem(538329, "DevDiv")]
        [Fact, Trait(Traits.Feature, Traits.Features.Formatting)]
        public void SuppressLinkBreakInIfElseStatement()
        {
            AssertFormat(@"class Program
{
    static void Main(string[] args)
    {
        int a;
        if (true) a = 10;
        else a = 11;
    }
}", @"class Program
{
    static void Main(string[] args)
    {
        int a;
        if (true) a = 10;
        else a = 11;
    }
}");
        }

        [WorkItem(538464, "DevDiv")]
        [Fact, Trait(Traits.Feature, Traits.Features.Formatting)]
        public void BugFix4087()
        {
            AssertFormat(@"class Program
{
    static void Main(string[] args)
    {
        Func<int, int> fun = x => { return x + 1; }
    }
}", @"class Program
{
    static void Main(string[] args)
    {
        Func<int, int> fun = x => { return x + 1; }
    }
}");
        }

        [Fact]
        [WorkItem(538511, "DevDiv")]
        [Trait(Traits.Feature, Traits.Features.Formatting)]
        public void AttributeTargetSpecifier()
        {
            var code = @"public class Class1
{
    [method :
    void Test()
    {
    }
}";

            var expected = @"public class Class1
{
    [method:
    void Test()
    {
    }
}";

            AssertFormat(expected, code);
        }

        [Fact]
        [WorkItem(538635, "DevDiv")]
        [Trait(Traits.Feature, Traits.Features.Formatting)]
        public void Finalizer()
        {
            var code = @"public class Class1
{
    ~ Class1() { }
}";

            var expected = @"public class Class1
{
    ~Class1() { }
}";

            AssertFormat(expected, code);
        }

        [Fact]
        [WorkItem(538743, "DevDiv")]
        [Trait(Traits.Feature, Traits.Features.Formatting)]
        public void BugFix4442()
        {
            var code = @"class Program
{
    static void Main(string[] args)
    {
        string str = ""ab,die|wo"";
        string[] a = str.Split(new char[] { ',', '|' })
            ;
    }
}";

            AssertFormat(code, code);
        }

        [Fact]
        [WorkItem(538658, "DevDiv")]
        [Trait(Traits.Feature, Traits.Features.Formatting)]
        public void BugFix4328()
        {
            var code = @"class Program
{
    static void Main(string[] args)
    {
        double d = new double           ();
    }
}";
            var expected = @"class Program
{
    static void Main(string[] args)
    {
        double d = new double();
    }
}";
            AssertFormat(expected, code);
        }

        [Fact]
        [WorkItem(538658, "DevDiv")]
        [Trait(Traits.Feature, Traits.Features.Formatting)]
        public void BugFix4515()
        {
            var code = @"class Program
{
    static void Main(string[] args)
    {
        var t = typeof ( System.Object )    ;
        var t1 =    default     (   System.Object       )       ;
        var t2 =        sizeof              (               System.Object       )   ;
    }
}";
            var expected = @"class Program
{
    static void Main(string[] args)
    {
        var t = typeof(System.Object);
        var t1 = default(System.Object);
        var t2 = sizeof(System.Object);
    }
}";
            AssertFormat(expected, code);
        }

        [Fact]
        [Trait(Traits.Feature, Traits.Features.Formatting)]
        public void CastExpressionTest()
        {
            var code = @"class Program
{
    static void Main(string[] args)
    {
        var a = (int) 1;
    }
}";
            var expected = @"class Program
{
    static void Main(string[] args)
    {
        var a = (int)1;
    }
}";
            AssertFormat(expected, code);
        }

        [Fact]
        [Trait(Traits.Feature, Traits.Features.Formatting)]
        public void NamedParameter()
        {
            var code = @"class Program
{
    static void Main(string[] args)
    {
        Main        (       args           :           null     )       ;  
    }
}";
            var expected = @"class Program
{
    static void Main(string[] args)
    {
        Main(args: null);
    }
}";
            AssertFormat(expected, code);
        }

        [WorkItem(539259, "DevDiv")]
        [Fact]
        [Trait(Traits.Feature, Traits.Features.Formatting)]
        public void BugFix5143()
        {
            var code = @"class Program
{
    static void Main(string[] args)
    {
        int x = Foo (   
            delegate (  int     x   )   {   return  x    ; }    )   ;   
    }
}";
            var expected = @"class Program
{
    static void Main(string[] args)
    {
        int x = Foo(
            delegate (int x) { return x; });
    }
}";
            AssertFormat(expected, code);
        }

        [WorkItem(539338, "DevDiv")]
        [Fact]
        [Trait(Traits.Feature, Traits.Features.Formatting)]
        public void BugFix5251()
        {
            var code = @"class Program
{
        public static string Foo { get; private set; }
}";
            var expected = @"class Program
{
    public static string Foo { get; private set; }
}";
            AssertFormat(expected, code);
        }

        [WorkItem(539358, "DevDiv")]
        [Fact]
        [Trait(Traits.Feature, Traits.Features.Formatting)]
        public void BugFix5277()
        {
            var code = @"
#if true
            #endif
";
            var expected = @"
#if true
#endif
";
            AssertFormat(expected, code);
        }

        [WorkItem(539542, "DevDiv")]
        [Fact]
        [Trait(Traits.Feature, Traits.Features.Formatting)]
        public void BugFix5544()
        {
            var code = @"
class Program
{
    unsafe static void Main(string[] args)
    {
        Program* p;
        p -> Foo = 5;
    }
}
";
            var expected = @"
class Program
{
    unsafe static void Main(string[] args)
    {
        Program* p;
        p->Foo = 5;
    }
}
";
            AssertFormat(expected, code);
        }

        [WorkItem(539587, "DevDiv")]
        [Fact]
        [Trait(Traits.Feature, Traits.Features.Formatting)]
        public void BugFix5602()
        {
            var code = @"    class Bug
    {
        public static void func()
        {
            long b = //
        }
    }";
            var expected = @"class Bug
{
    public static void func()
    {
        long b = //
        }
}";
            AssertFormat(expected, code);
        }

        [WorkItem(539616, "DevDiv")]
        [Fact]
        [Trait(Traits.Feature, Traits.Features.Formatting)]
        public void BugFix5637()
        {
            var code = @"class Bug
{
    // test
	public static void func()
    {
    }
}";
            var expected = @"class Bug
{
    // test
    public static void func()
    {
    }
}";
            AssertFormat(expected, code);
        }

        [Fact]
        [Trait(Traits.Feature, Traits.Features.Formatting)]
        public void GenericType()
        {
            var code = @"class Bug<T>
{
    class N : Bug<  T   [   ]   >
    {
    }
}";
            var expected = @"class Bug<T>
{
    class N : Bug<T[]>
    {
    }
}";
            AssertFormat(expected, code);
        }

        [Fact]
        [WorkItem(539878, "DevDiv")]
        [Trait(Traits.Feature, Traits.Features.Formatting)]
        public void BugFix5978()
        {
            var code = @"class Program
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
System.Console.WriteLine(""a"");
}
        }
    }
}";
            var expected = @"class Program
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
                System.Console.WriteLine(""a"");
            }
        }
    }
}";
            AssertFormat(expected, code);
        }

        [Fact]
        [WorkItem(539878, "DevDiv")]
        [Trait(Traits.Feature, Traits.Features.Formatting)]
        public void BugFix5979()
        {
            var code = @"delegate int del(int i);
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
}";
            var expected = @"delegate int del(int i);
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
}";
            AssertFormat(expected, code);
        }

        [Fact]
        [WorkItem(539891, "DevDiv")]
        [Trait(Traits.Feature, Traits.Features.Formatting)]
        public void BugFix5993()
        {
            var code = @"public class MyClass
{
    public static void Main()
    {
    lab1:
        {
    lab1:// CS0158
                goto lab1;
        }
    }
}";
            var expected = @"public class MyClass
{
    public static void Main()
    {
    lab1:
        {
        lab1:// CS0158
            goto lab1;
        }
    }
}";
            AssertFormat(expected, code);
        }

        [Fact]
        [WorkItem(540315, "DevDiv")]
        [Trait(Traits.Feature, Traits.Features.Formatting)]
        public void BugFix6536()
        {
            var code = @"public class MyClass
{
    public static void Main()
    {
        int i = - - 1 + + + 1 + - + 1 + - + 1   ;
    }
}";
            var expected = @"public class MyClass
{
    public static void Main()
    {
        int i = - -1 + + +1 + -+1 + -+1;
    }
}";
            AssertFormat(expected, code);
        }

        [Fact]
        [WorkItem(540801, "DevDiv")]
        [Trait(Traits.Feature, Traits.Features.Formatting)]
        public void BugFix7211()
        {
            var code = @"class Program
{
    static void Main(string[] args)
    {
        while (0 > new int[] { 1 }.Length)
        {
            System.Console.WriteLine(""Hello"");
        }
    }
}";

            var expected = @"class Program
{
    static void Main(string[] args)
    {
        while (0 > new int[] { 1 }.Length)
        {
            System.Console.WriteLine(""Hello"");
        }
    }
}";
            AssertFormat(expected, code);
        }

        [Fact]
        [WorkItem(541035, "DevDiv")]
        [Trait(Traits.Feature, Traits.Features.Formatting)]
        public void BugFix7564_1()
        {
            var code = @"class Program
{
    static void Main(string[] args)
    {
        while (null != new int[] { 1 })
        {
            System.Console.WriteLine(""Hello"");
        }
    }
}";

            var expected = @"class Program
{
    static void Main(string[] args)
    {
        while (null != new int[] { 1 })
        {
            System.Console.WriteLine(""Hello"");
        }
    }
}";
            AssertFormat(expected, code);
        }

        [Fact]
        [WorkItem(541035, "DevDiv")]
        [Trait(Traits.Feature, Traits.Features.Formatting)]
        public void BugFix7564_2()
        {
            var code = @"class Program
{
    static void Main(string[] args)
    {
        foreach (var f in new int[] { 5 })
        {
            Console.WriteLine(f);
        }
    }
}";

            var expected = @"class Program
{
    static void Main(string[] args)
    {
        foreach (var f in new int[] { 5 })
        {
            Console.WriteLine(f);
        }
    }
}";
            AssertFormat(expected, code);
        }

        [Fact]
        [WorkItem(8385, "DevDiv_Projects/Roslyn")]
        [Trait(Traits.Feature, Traits.Features.Formatting)]
        public void NullCoalescingOperator()
        {
            var code = @"class C
{
    void M()
    {
        object o2 = null??null;
    }
}";

            var expected = @"class C
{
    void M()
    {
        object o2 = null ?? null;
    }
}";
            AssertFormat(expected, code);
        }

        [Fact]
        [WorkItem(541925, "DevDiv")]
        [Trait(Traits.Feature, Traits.Features.Formatting)]
        public void QueryContinuation()
        {
            var code = @"using System.Linq;
class C
{
    static void Main(string[] args)
    {
        var temp = from x in ""abc""
                   let z = x.ToString()
                   select z into w
        select w;
    }
}";

            var expected = @"using System.Linq;
class C
{
    static void Main(string[] args)
    {
        var temp = from x in ""abc""
                   let z = x.ToString()
                   select z into w
                   select w;
    }
}";
            AssertFormat(expected, code);
        }

        [Fact]
        [Trait(Traits.Feature, Traits.Features.Formatting)]
        public void QueryContinuation2()
        {
            var code = @"using System.Linq;
class C
{
    static void Main(string[] args)
    {
        var temp = from x in ""abc"" select x into
    }
}";

            var expected = @"using System.Linq;
class C
{
    static void Main(string[] args)
    {
        var temp = from x in ""abc""
                   select x into
    }
}";
            AssertFormat(expected, code);
        }

        [Fact]
        [WorkItem(542305, "DevDiv")]
        [Trait(Traits.Feature, Traits.Features.Formatting)]
        public void AttributeFormatting1()
        {
            var code = @"class Program
{
    void AddClass(string name,[OptionalAttribute]    object position,[OptionalAttribute]    object bases)
    {
    }
}";

            var expected = @"class Program
{
    void AddClass(string name, [OptionalAttribute]    object position, [OptionalAttribute]    object bases)
    {
    }
}";
            AssertFormat(expected, code);
        }

        [Fact]
        [WorkItem(542304, "DevDiv")]
        [Trait(Traits.Feature, Traits.Features.Formatting)]
        public void CloseBracesInArgumentList()
        {
            var code = @"class Program
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
}";

            var expected = @"class Program
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
}";
            AssertFormat(expected, code);
        }

        [Fact]
        [WorkItem(542538, "DevDiv")]
        [Trait(Traits.Feature, Traits.Features.Formatting)]
        public void MissingTokens()
        {
            var code = @"using System;
delegate void myDelegate(int name = 1);
class innerClass
{
    public innerClass()
    {
        myDelegate x = (int y=1) => { return; };
    }
}";

            var expected = @"using System;
delegate void myDelegate(int name = 1);
class innerClass
{
    public innerClass()
    {
        myDelegate x = (int y = 1) => { return; };
    }
}";

            AssertFormat(expected, code);
        }

        [Fact]
        [WorkItem(542199, "DevDiv")]
        [Trait(Traits.Feature, Traits.Features.Formatting)]
        public void ColumnOfVeryFirstToken()
        {
            var code = @"			       W   )b";

            var expected = @"			       W   )b";

            AssertFormat(expected, code);
        }

        [Fact]
        [WorkItem(542718, "DevDiv")]
        [Trait(Traits.Feature, Traits.Features.Formatting)]
        public void EmptySuppressionSpan()
        {
            var code = @"enum E
    {
        a,,
    }";

            var expected = @"enum E
{
    a,,
}";

            AssertFormat(expected, code);
        }

        [Fact]
        [WorkItem(542790, "DevDiv")]
        [Trait(Traits.Feature, Traits.Features.Formatting)]
        public void LabelInSwitch()
        {
            var code = @"class test
{
    public static void Main()
    {
        string target = ""t1"";
        switch (target)
        {
            case ""t1"":
        label1:
            goto label1;
            case ""t2"":
                label2:
                    goto label2;
        }
    }
}";

            var expected = @"class test
{
    public static void Main()
    {
        string target = ""t1"";
        switch (target)
        {
            case ""t1"":
            label1:
                goto label1;
            case ""t2"":
            label2:
                goto label2;
        }
    }
}";

            AssertFormat(expected, code);
        }

        [Fact]
        [WorkItem(543112, "DevDiv")]
        [Trait(Traits.Feature, Traits.Features.Formatting)]
        public void FormatArbitaryNode()
        {
            var expected = @"public int Prop
{
    get
    {
        return c;
    }

    set
    {
        c = value;
    }
}";

            var property = SyntaxFactory.PropertyDeclaration(
                SyntaxFactory.List<AttributeListSyntax>(),
                SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.PublicKeyword)),
                SyntaxFactory.ParseTypeName("int"),
                default(ExplicitInterfaceSpecifierSyntax),
                SyntaxFactory.Identifier("Prop"),
                SyntaxFactory.AccessorList(
                    SyntaxFactory.List(
                        new AccessorDeclarationSyntax[]
                        {
                        SyntaxFactory.AccessorDeclaration(
                            SyntaxKind.GetAccessorDeclaration,
                            SyntaxFactory.Block(SyntaxFactory.SingletonList(SyntaxFactory.ParseStatement("return c;")))),
                        SyntaxFactory.AccessorDeclaration(
                            SyntaxKind.SetAccessorDeclaration,
                            SyntaxFactory.Block(SyntaxFactory.SingletonList(SyntaxFactory.ParseStatement("c = value;"))))
                        })));

            Assert.NotNull(property);

            var newProperty = Formatter.Format(property, new AdhocWorkspace());

            Assert.Equal(expected, newProperty.ToFullString());
        }

        [Fact]
        [WorkItem(543140, "DevDiv")]
        [Trait(Traits.Feature, Traits.Features.Formatting)]
        public void OmittedTypeArgument()
        {
            var code = @"using System;
using System.Collections.Generic;
using System.Linq;
 
class Program
{
    static void Main(string[] args)
    {
        Console.WriteLine(typeof(Dictionary<, >).IsGenericTypeDefinition);
    }
}";

            var expected = @"using System;
using System.Collections.Generic;
using System.Linq;

class Program
{
    static void Main(string[] args)
    {
        Console.WriteLine(typeof(Dictionary<,>).IsGenericTypeDefinition);
    }
}";

            AssertFormat(expected, code);
        }

        [Fact]
        [WorkItem(543131, "DevDiv")]
        [Trait(Traits.Feature, Traits.Features.Formatting)]
        public void TryAfterLabel()
        {
            var code = @"using System;
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
}";

            var expected = @"using System;
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
}";

            AssertFormat(expected, code);
        }

        [Fact]
        [Trait(Traits.Feature, Traits.Features.Formatting)]
        public void QueryContinuation1()
        {
            var code = @"using System.Linq;

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
}";

            var expected = @"using System.Linq;

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
}";

            AssertFormat(expected, code);
        }

        [Fact]
        [Trait(Traits.Feature, Traits.Features.Formatting)]
        public void TestCSharpFormattingSpacingOptions()
        {
            var text =
@"
interface f1
{ }

interface f2     :    f1 { }

struct d2   :    f1 { }

class foo      :      System        .     Object
{
    public     int     bar    =   1*   2;
    public void foobar      (         ) 
    {
        foobar        (         );
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
}";
            var expectedFormattedText =
@"
interface f1
{ }

interface f2 : f1 { }

struct d2 : f1 { }

class foo : System.Object
{
    public int bar = 1 * 2;
    public void foobar()
    {
        foobar();
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
}";

            AssertFormat(expectedFormattedText, text);
        }

        [Fact]
        [Trait(Traits.Feature, Traits.Features.Formatting)]
        public void SpacingFixInTokenBasedForIfAndSwitchCase()
        {
            var code = @"class Class5{
void bar()
{
if(x == 1) 
x = 2; else x = 3;
switch (x) { 
case 1: break; case 2: break; default: break;}
}
}";
            var expectedCode = @"class Class5
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
}";
            AssertFormat(expectedCode, code);
        }

        [Fact]
        [WorkItem(545335, "DevDiv")]
        [Trait(Traits.Feature, Traits.Features.Formatting)]
        public void PreprocessorOnSameLine()
        {
            var code = @"class C
{
}#line default

#line hidden";

            var expected = @"class C
{
}
#line default

#line hidden";

            AssertFormat(expected, code);
        }

        [Fact]
        [WorkItem(545626, "DevDiv")]
        [Trait(Traits.Feature, Traits.Features.Formatting)]
        public void ArraysInAttributes()
        {
            var code = @"[A(X = new int[] { 1 })]
public class A : Attribute
{
    public int[] X;
}";

            var expected = @"[A(X = new int[] { 1 })]
public class A : Attribute
{
    public int[] X;
}";

            AssertFormat(expected, code);
        }

        [Fact]
        [WorkItem(530580, "DevDiv")]
        [Trait(Traits.Feature, Traits.Features.Formatting)]
        public void NoNewLineAfterBraceInExpression()
        {
            var code = @"public class A
{
    void Method()
    {
         var po = cancellationToken.CanBeCanceled ?
            new ParallelOptions() { CancellationToken = cancellationToken }     :
            defaultParallelOptions;
    }
}";

            var expected = @"public class A
{
    void Method()
    {
        var po = cancellationToken.CanBeCanceled ?
           new ParallelOptions() { CancellationToken = cancellationToken } :
           defaultParallelOptions;
    }
}";

            AssertFormat(expected, code);
        }

        [Fact]
        [WorkItem(530580, "DevDiv")]
        [Trait(Traits.Feature, Traits.Features.Formatting)]
        public void NoIndentForNestedUsingWithoutBraces()
        {
            var code = @"class C
{
void M()
{
using (null)
using (null)
{ 
}
}
}
";

            var expected = @"class C
{
    void M()
    {
        using (null)
        using (null)
        {
        }
    }
}
";

            AssertFormat(expected, code);
        }

        [Fact]
        [WorkItem(546678, "DevDiv")]
        [Trait(Traits.Feature, Traits.Features.Formatting)]
        public void UnicodeWhitespace()
        {
            var code = "\u001A";

            AssertFormat("", code);
        }

        [Fact]
        [WorkItem(17431, "DevDiv_Projects/Roslyn")]
        [Trait(Traits.Feature, Traits.Features.Formatting)]
        public void NoElasticRuleOnRegularFile()
        {
            var code = @"class Consumer
{
    public int P
    {
                }
}";

            var expected = @"class Consumer
{
    public int P
    {
    }
}";

            AssertFormat(expected, code);
        }

        [Fact]
        [WorkItem(584599, "DevDiv_Projects/Roslyn")]
        [Trait(Traits.Feature, Traits.Features.Formatting)]
        public void CaseSection()
        {
            var code = @"class C
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
}";

            var expected = @"class C
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
}";

            AssertFormat(expected, code);
        }

        [Fact]
        [WorkItem(553654, "DevDiv_Projects/Roslyn")]
        [Trait(Traits.Feature, Traits.Features.Formatting)]
        public void Bugfix_553654_LabelStatementIndenting()
        {
            var changingOptions = new Dictionary<OptionKey, object>();
            changingOptions.Add(CSharpFormattingOptions.LabelPositioning, LabelPositionOptions.LeftMost);

            var code = @"class Program
{
    void F()
    {
        foreach (var x in new int[] { })
        {
            foo:
            int a = 1;
        }
    }
}";

            var expected = @"class Program
{
    void F()
    {
        foreach (var x in new int[] { })
        {
foo:
            int a = 1;
        }
    }
}";
            AssertFormat(expected, code, false, changingOptions);
        }

        [Fact]
        [WorkItem(707064, "DevDiv_Projects/Roslyn")]
        [Trait(Traits.Feature, Traits.Features.Formatting)]
        public void Bugfix_707064_SpaceAfterSecondSemiColonInFor()
        {
            var code = @"class Program
{
    void F()
    {
        for (int i = 0; i < 5;)
        {
        }
    }
}";

            var expected = @"class Program
{
    void F()
    {
        for (int i = 0; i < 5;)
        {
        }
    }
}";
            AssertFormat(expected, code);
        }

        [Fact]
        [WorkItem(772313, "DevDiv")]
        [Trait(Traits.Feature, Traits.Features.Formatting)]
        public void Bugfix_772313_ReturnKeywordBeforeQueryClauseDoesNotTriggerNewLineOnFormat()
        {
            var code = @"class C
{
    int M()
    {
        return             from c in ""
               select c;
    }
}";

            var expected = @"class C
{
    int M()
    {
        return from c in ""
               select c;
    }
}";
            AssertFormat(expected, code);
        }

        [Fact]
        [WorkItem(772304, "DevDiv")]
        [Trait(Traits.Feature, Traits.Features.Formatting)]
        public void Bugfix_772313_PreserveMethodParameterIndentWhenAttributePresent()
        {
            var code = @"class C
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
}";

            var expected = @"class C
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
}";
            AssertFormat(expected, code);
        }

        [Fact]
        [WorkItem(776513, "DevDiv")]
        [Trait(Traits.Feature, Traits.Features.Formatting)]
        public void Bugfix_776513_CheckBraceIfNotMissingBeforeApplyingOperationForBracedBlocks()
        {
            var code = @"var alwaysTriggerList = new[]
    Dim triggerOnlyWithLettersList =";

            var expected = @"var alwaysTriggerList = new[]
    Dim triggerOnlyWithLettersList =";
            AssertFormat(expected, code);
        }

        [WorkItem(769342, "DevDiv")]
        [Fact, Trait(Traits.Feature, Traits.Features.Formatting)]
        public void ShouldFormatDocCommentWithIndentSameAsTabSizeWithUseTabTrue()
        {
            var optionSet = new Dictionary<OptionKey, object> { { new OptionKey(FormattingOptions.UseTabs, LanguageNames.CSharp), true } };

            AssertFormat(@"namespace ConsoleApplication1
{
	/// <summary>
	/// fka;jsgdflkhsjflgkhdsl;
	/// </summary>
	class Program
	{
	}
}", @"namespace ConsoleApplication1
{
    /// <summary>
    /// fka;jsgdflkhsjflgkhdsl;
    /// </summary>
    class Program
    {
    }
}", changedOptionSet: optionSet);
        }

        [WorkItem(797278, "DevDiv")]
        [Fact, Trait(Traits.Feature, Traits.Features.Formatting)]
        public void TestSpacingOptionAroundControlFlow()
        {
            const string code = @"
class Program
{
    public void foo()
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
}";
            const string expected = @"
class Program
{
    public void foo()
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
}";
            var optionSet = new Dictionary<OptionKey, object> { { CSharpFormattingOptions.SpaceWithinOtherParentheses, true } };
            AssertFormat(expected, code, changedOptionSet: optionSet);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Formatting)]
        public void TestSpacingOptionAfterControlFlowKeyword()
        {
            var code = @"
class Program
{
    public void foo()
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
    }
}";
            var expected = @"
class Program
{
    public void foo()
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
    }
}";
            var optionSet = new Dictionary<OptionKey, object> { { CSharpFormattingOptions.SpaceAfterControlFlowStatementKeyword, false } };
            AssertFormat(expected, code, changedOptionSet: optionSet);
        }

        [WorkItem(766212, "DevDiv")]
        [Fact, Trait(Traits.Feature, Traits.Features.Formatting)]
        public void TestOptionForSpacingAroundCommas()
        {
            var code = @"
class Program
{
    public void Main()
    {
        var a = new[] {1,2,3};
        var digits = new List<int> {1,2,3,4};
    }
}";
            var expectedDefault = @"
class Program
{
    public void Main()
    {
        var a = new[] { 1, 2, 3 };
        var digits = new List<int> { 1, 2, 3, 4 };
    }
}";
            AssertFormat(expectedDefault, code);

            var expectedAfterCommaDisabled = @"
class Program
{
    public void Main()
    {
        var a = new[] { 1,2,3 };
        var digits = new List<int> { 1,2,3,4 };
    }
}";
            var optionSet = new Dictionary<OptionKey, object> { { CSharpFormattingOptions.SpaceAfterComma, false } };
            AssertFormat(expectedAfterCommaDisabled, code, changedOptionSet: optionSet);

            var expectedBeforeCommaEnabled = @"
class Program
{
    public void Main()
    {
        var a = new[] { 1 ,2 ,3 };
        var digits = new List<int> { 1 ,2 ,3 ,4 };
    }
}";
            optionSet.Add(CSharpFormattingOptions.SpaceBeforeComma, true);
            AssertFormat(expectedBeforeCommaEnabled, code, changedOptionSet: optionSet);
        }

        [Fact]
        [WorkItem(772308, "DevDiv")]
        [Trait(Traits.Feature, Traits.Features.Formatting)]
        public void Bugfix_772308_SeparateSuppressionForEachCaseLabelEvenIfEmpty()
        {
            var code = @"
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
";

            var expected = @"
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
";
            AssertFormat(expected, code);
        }

        [Fact]
        [WorkItem(844913, "DevDiv")]
        [Trait(Traits.Feature, Traits.Features.Formatting)]
        public void QueryExpressionInExpression()
        {
            var changingOptions = new Dictionary<OptionKey, object>();
            changingOptions.Add(CSharpFormattingOptions.NewLinesForBracesInMethods, false);

            var code = @"
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
";

            var expected = @"
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
";
            AssertFormat(expected, code);
        }

        [Fact]
        [WorkItem(843479, "DevDiv")]
        [Trait(Traits.Feature, Traits.Features.Formatting)]
        public void EmbeddedStatementElse()
        {
            var changingOptions = new Dictionary<OptionKey, object>();
            changingOptions.Add(CSharpFormattingOptions.NewLineForElse, false);

            var code = @"
class C
{
    void Method()
    {
        if (true)
                Console.WriteLine();              else
                Console.WriteLine();
    }
}
";

            var expected = @"
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
";
            AssertFormat(expected, code, false, changingOptions);
        }

        [Fact]
        [WorkItem(772311, "DevDiv")]
        [Trait(Traits.Feature, Traits.Features.Formatting)]
        public void CommentAtTheEndOfLine()
        {
            var code = @"
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
";

            var expected = @"
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
";
            AssertFormat(expected, code);
        }

        [Fact]
        [WorkItem(772311, "DevDiv")]
        [Trait(Traits.Feature, Traits.Features.Formatting)]
        public void TestTab()
        {
            var code = @"
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
";

            var expected = @"
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
";
            var optionSet = new Dictionary<OptionKey, object> { { new OptionKey(FormattingOptions.UseTabs, LanguageNames.CSharp), true } };

            AssertFormat(expected, code, changedOptionSet: optionSet);
            AssertFormat(expected, expected, changedOptionSet: optionSet);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Formatting)]
        public void LeaveBlockSingleLine_False()
        {
            var code = @"
namespace N { class C { int x; } }";

            var expected = @"
namespace N
{
    class C
    {
        int x;
    }
}";

            var options = new Dictionary<OptionKey, object>() { { CSharpFormattingOptions.WrappingPreserveSingleLine, false } };
            AssertFormat(expected, code, changedOptionSet: options);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Formatting)]
        public void LeaveBlockSingleLine_False2()
        {
            var code = @"
class C { void foo() { } }";

            var expected = @"
class C
{
    void foo()
    {
    }
}";

            var options = new Dictionary<OptionKey, object>() { { CSharpFormattingOptions.WrappingPreserveSingleLine, false } };
            AssertFormat(expected, code, changedOptionSet: options);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Formatting)]
        public void LeaveStatementMethodDeclarationSameLine_False()
        {
            var code = @"
class Program
{
    void foo()
    {
        int x = 0; int y = 0;
    }
}";

            var expected = @"
class Program
{
    void foo()
    {
        int x = 0;
        int y = 0;
    }
}";

            var options = new Dictionary<OptionKey, object>() { { CSharpFormattingOptions.WrappingKeepStatementsOnSingleLine, false } };
            AssertFormat(expected, code, changedOptionSet: options);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Formatting)]
        public void SpaceWithinEmptyBracketPrecedencesSpaceBeforeOrAfterComma_0000()
        {
            var code = @"
class Program
{
    int[ ] x;
    int[ , ] y;
    int[ , , ] z = new int[1,2,3];
    var a = new[ ] { 0 };
}";

            var expected = @"
class Program
{
    int[] x;
    int[,] y;
    int[,,] z = new int[1,2,3];
    var a = new[] { 0 };
}";

            var options = new Dictionary<OptionKey, object>()
            {
                { SpaceBetweenEmptySquareBrackets, false },
                { SpaceWithinSquareBrackets, false },
                { SpaceBeforeComma, false },
                { SpaceAfterComma, false },
            };
            AssertFormat(expected, code, changedOptionSet: options);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Formatting)]
        public void SpaceWithinEmptyBracketPrecedencesSpaceBeforeOrAfterComma_0001()
        {
            var code = @"
class Program
{
    int[ ] x;
    int[ , ] y;
    int[ , , ] z = new int[1,2,3];
    var a = new[ ] { 0 };
}";

            var expected = @"
class Program
{
    int[] x;
    int[,] y;
    int[,,] z = new int[1, 2, 3];
    var a = new[] { 0 };
}";

            var options = new Dictionary<OptionKey, object>()
            {
                { SpaceBetweenEmptySquareBrackets, false },
                { SpaceWithinSquareBrackets, false },
                { SpaceBeforeComma, false },
                { SpaceAfterComma, true },
            };
            AssertFormat(expected, code, changedOptionSet: options);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Formatting)]
        public void SpaceWithinEmptyBracketPrecedencesSpaceBeforeOrAfterComma_0010()
        {
            var code = @"
class Program
{
    int[ ] x;
    int[ , ] y;
    int[ , , ] z = new int[1,2,3];
    var a = new[ ] { 0 };
}";

            var expected = @"
class Program
{
    int[] x;
    int[,] y;
    int[,,] z = new int[1 ,2 ,3];
    var a = new[] { 0 };
}";

            var options = new Dictionary<OptionKey, object>()
            {
                { SpaceBetweenEmptySquareBrackets, false },
                { SpaceWithinSquareBrackets, false },
                { SpaceBeforeComma, true },
                { SpaceAfterComma, false },
            };
            AssertFormat(expected, code, changedOptionSet: options);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Formatting)]
        public void SpaceWithinEmptyBracketPrecedencesSpaceBeforeOrAfterComma_0011()
        {
            var code = @"
class Program
{
    int[ ] x;
    int[ , ] y;
    int[ , , ] z = new int[1,2,3];
    var a = new[ ] { 0 };
}";

            var expected = @"
class Program
{
    int[] x;
    int[,] y;
    int[,,] z = new int[1 , 2 , 3];
    var a = new[] { 0 };
}";

            var options = new Dictionary<OptionKey, object>()
            {
                { SpaceBetweenEmptySquareBrackets, false },
                { SpaceWithinSquareBrackets, false },
                { SpaceBeforeComma, true },
                { SpaceAfterComma, true },
            };
            AssertFormat(expected, code, changedOptionSet: options);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Formatting)]
        public void SpaceWithinEmptyBracketPrecedencesSpaceBeforeOrAfterComma_0100()
        {
            var code = @"
class Program
{
    int[ ] x;
    int[, ] y;
    int[, , ] z = new int[1,2,3];
    var a = new[ ] { 0 };
}";

            var expected = @"
class Program
{
    int[] x;
    int[,] y;
    int[,,] z = new int[ 1,2,3 ];
    var a = new[] { 0 };
}";

            var options = new Dictionary<OptionKey, object>()
            {
                { SpaceBetweenEmptySquareBrackets, false },
                { SpaceWithinSquareBrackets, true },
                { SpaceBeforeComma, false },
                { SpaceAfterComma, false },
            };
            AssertFormat(expected, code, changedOptionSet: options);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Formatting)]
        public void SpaceWithinEmptyBracketPrecedencesSpaceBeforeOrAfterComma_0101()
        {
            var code = @"
class Program
{
    int[ ] x;
    int[, ] y;
    int[, , ] z = new int[1,2,3];
    var a = new[ ] { 0 };
}";

            var expected = @"
class Program
{
    int[] x;
    int[,] y;
    int[,,] z = new int[ 1, 2, 3 ];
    var a = new[] { 0 };
}";

            var options = new Dictionary<OptionKey, object>()
            {
                { SpaceBetweenEmptySquareBrackets, false },
                { SpaceWithinSquareBrackets, true },
                { SpaceBeforeComma, false },
                { SpaceAfterComma, true },
            };
            AssertFormat(expected, code, changedOptionSet: options);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Formatting)]
        public void SpaceWithinEmptyBracketPrecedencesSpaceBeforeOrAfterComma_0110()
        {
            var code = @"
class Program
{
    int[ ] x;
    int[, ] y;
    int[, , ] z = new int[1,2,3];
    var a = new[ ] { 0 };
}";

            var expected = @"
class Program
{
    int[] x;
    int[,] y;
    int[,,] z = new int[ 1 ,2 ,3 ];
    var a = new[] { 0 };
}";

            var options = new Dictionary<OptionKey, object>()
            {
                { SpaceBetweenEmptySquareBrackets, false },
                { SpaceWithinSquareBrackets, true },
                { SpaceBeforeComma, true },
                { SpaceAfterComma, false },
            };
            AssertFormat(expected, code, changedOptionSet: options);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Formatting)]
        public void SpaceWithinEmptyBracketPrecedencesSpaceBeforeOrAfterComma_0111()
        {
            var code = @"
class Program
{
    int[ ] x;
    int[, ] y;
    int[, , ] z = new int[1,2,3];
    var a = new[ ] { 0 };
}";

            var expected = @"
class Program
{
    int[] x;
    int[,] y;
    int[,,] z = new int[ 1 , 2 , 3 ];
    var a = new[] { 0 };
}";

            var options = new Dictionary<OptionKey, object>()
            {
                { SpaceBetweenEmptySquareBrackets, false },
                { SpaceWithinSquareBrackets, true },
                { SpaceBeforeComma, true },
                { SpaceAfterComma, true },
            };
            AssertFormat(expected, code, changedOptionSet: options);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Formatting)]
        public void SpaceWithinEmptyBracketPrecedencesSpaceBeforeOrAfterComma_1000()
        {
            var code = @"
class Program
{
    int[] x;
    int[ ,] y;
    int[ , ,] z = new int[1,2,3];
    var a = new[] { 0 };
}";

            var expected = @"
class Program
{
    int[ ] x;
    int[ , ] y;
    int[ , , ] z = new int[1,2,3];
    var a = new[ ] { 0 };
}";

            var options = new Dictionary<OptionKey, object>()
            {
                { SpaceBetweenEmptySquareBrackets, true },
                { SpaceWithinSquareBrackets, false },
                { SpaceBeforeComma, false },
                { SpaceAfterComma, false },
            };
            AssertFormat(expected, code, changedOptionSet: options);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Formatting)]
        public void SpaceWithinEmptyBracketPrecedencesSpaceBeforeOrAfterComma_1001()
        {
            var code = @"
class Program
{
    int[] x;
    int[ ,] y;
    int[ , ,] z = new int[1,2,3];
    var a = new[] { 0 };
}";

            var expected = @"
class Program
{
    int[ ] x;
    int[ , ] y;
    int[ , , ] z = new int[1, 2, 3];
    var a = new[ ] { 0 };
}";

            var options = new Dictionary<OptionKey, object>()
            {
                { SpaceBetweenEmptySquareBrackets, true },
                { SpaceWithinSquareBrackets, false },
                { SpaceBeforeComma, false },
                { SpaceAfterComma, true },
            };
            AssertFormat(expected, code, changedOptionSet: options);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Formatting)]
        public void SpaceWithinEmptyBracketPrecedencesSpaceBeforeOrAfterComma_1010()
        {
            var code = @"
class Program
{
    int[] x;
    int[ ,] y;
    int[ , ,] z = new int[1,2,3];
    var a = new[] { 0 };
}";

            var expected = @"
class Program
{
    int[ ] x;
    int[ , ] y;
    int[ , , ] z = new int[1 ,2 ,3];
    var a = new[ ] { 0 };
}";

            var options = new Dictionary<OptionKey, object>()
            {
                { SpaceBetweenEmptySquareBrackets, true },
                { SpaceWithinSquareBrackets, false },
                { SpaceBeforeComma, true },
                { SpaceAfterComma, false },
            };
            AssertFormat(expected, code, changedOptionSet: options);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Formatting)]
        public void SpaceWithinEmptyBracketPrecedencesSpaceBeforeOrAfterComma_1011()
        {
            var code = @"
class Program
{
    int[] x;
    int[ ,] y;
    int[ , ,] z = new int[1,2,3];
    var a = new[] { 0 };
}";

            var expected = @"
class Program
{
    int[ ] x;
    int[ , ] y;
    int[ , , ] z = new int[1 , 2 , 3];
    var a = new[ ] { 0 };
}";

            var options = new Dictionary<OptionKey, object>()
            {
                { SpaceBetweenEmptySquareBrackets, true },
                { SpaceWithinSquareBrackets, false },
                { SpaceBeforeComma, true },
                { SpaceAfterComma, true },
            };
            AssertFormat(expected, code, changedOptionSet: options);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Formatting)]
        public void SpaceWithinEmptyBracketPrecedencesSpaceBeforeOrAfterComma_1100()
        {
            var code = @"
class Program
{
    int[ ] x;
    int[ , ] y;
    int[ , , ] z = new int[1,2,3];
    var a = new[ ] { 0 };
}";

            var expected = @"
class Program
{
    int[ ] x;
    int[ , ] y;
    int[ , , ] z = new int[ 1,2,3 ];
    var a = new[ ] { 0 };
}";

            var options = new Dictionary<OptionKey, object>()
            {
                { SpaceBetweenEmptySquareBrackets, true },
                { SpaceWithinSquareBrackets, true },
                { SpaceBeforeComma, false },
                { SpaceAfterComma, false },
            };
            AssertFormat(expected, code, changedOptionSet: options);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Formatting)]
        public void SpaceWithinEmptyBracketPrecedencesSpaceBeforeOrAfterComma_1101()
        {
            var code = @"
class Program
{
    int[ ] x;
    int[ , ] y;
    int[ , , ] z = new int[1,2,3];
    var a = new[ ] { 0 };
}";

            var expected = @"
class Program
{
    int[ ] x;
    int[ , ] y;
    int[ , , ] z = new int[ 1, 2, 3 ];
    var a = new[ ] { 0 };
}";

            var options = new Dictionary<OptionKey, object>()
            {
                { SpaceBetweenEmptySquareBrackets, true },
                { SpaceWithinSquareBrackets, true },
                { SpaceBeforeComma, false },
                { SpaceAfterComma, true },
            };
            AssertFormat(expected, code, changedOptionSet: options);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Formatting)]
        public void SpaceWithinEmptyBracketPrecedencesSpaceBeforeOrAfterComma_1110()
        {
            var code = @"
class Program
{
    int[ ] x;
    int[ , ] y;
    int[ , , ] z = new int[1,2,3];
    var a = new[ ] { 0 };
}";

            var expected = @"
class Program
{
    int[ ] x;
    int[ , ] y;
    int[ , , ] z = new int[ 1 ,2 ,3 ];
    var a = new[ ] { 0 };
}";

            var options = new Dictionary<OptionKey, object>()
            {
                { SpaceBetweenEmptySquareBrackets, true },
                { SpaceWithinSquareBrackets, true },
                { SpaceBeforeComma, true },
                { SpaceAfterComma, false },
            };
            AssertFormat(expected, code, changedOptionSet: options);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Formatting)]
        public void SpaceWithinEmptyBracketPrecedencesSpaceBeforeOrAfterComma_1111()
        {
            var code = @"
class Program
{
    int[ ] x;
    int[ , ] y;
    int[ , , ] z = new int[1,2,3];
    var a = new[ ] { 0 };
}";

            var expected = @"
class Program
{
    int[ ] x;
    int[ , ] y;
    int[ , , ] z = new int[ 1 , 2 , 3 ];
    var a = new[ ] { 0 };
}";

            var options = new Dictionary<OptionKey, object>()
            {
                { SpaceBetweenEmptySquareBrackets, true },
                { SpaceWithinSquareBrackets, true },
                { SpaceBeforeComma, true },
                { SpaceAfterComma, true },
            };
            AssertFormat(expected, code, changedOptionSet: options);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Formatting)]
        public void ArrayDeclarationShouldFollowEmptySquareBrackets()
        {
            const string code = @"
class Program
{
   var t = new Foo(new[ ] { ""a"", ""b"" });
}";

            const string expected = @"
class Program
{
    var t = new Foo(new[] { ""a"", ""b"" });
}";

            var options = new Dictionary<OptionKey, object>
            {
                { CSharpFormattingOptions.SpaceWithinSquareBrackets, true },
                { CSharpFormattingOptions.SpaceBetweenEmptySquareBrackets, false }
            };
            AssertFormat(expected, code, changedOptionSet: options);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Formatting)]
        public void SquareBracesBefore_True()
        {
            var code = @"
class Program
{
    int[] x;
}";

            var expected = @"
class Program
{
    int [] x;
}";

            var options = new Dictionary<OptionKey, object>() { { CSharpFormattingOptions.SpaceBeforeOpenSquareBracket, true } };
            AssertFormat(expected, code, changedOptionSet: options);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Formatting)]
        public void SquareBracesAndValue_True()
        {
            var code = @"
class Program
{
    int[3] x;
}";

            var expected = @"
class Program
{
    int[ 3 ] x;
}";

            var options = new Dictionary<OptionKey, object>() { { CSharpFormattingOptions.SpaceWithinSquareBrackets, true } };
            AssertFormat(expected, code, changedOptionSet: options);
        }

        [WorkItem(917351, "DevDiv")]
        [Fact, Trait(Traits.Feature, Traits.Features.Formatting)]
        public void TestLockStatement()
        {
            var code = @"
class Program
{
    public void Method()
    {
        lock (expression)
            {
        // foo
}
    }
}";

            var expected = @"
class Program
{
    public void Method()
    {
        lock (expression) {
            // foo
        }
    }
}";

            var options = new Dictionary<OptionKey, object>()
            {
                { CSharpFormattingOptions.NewLinesForBracesInControlBlocks, false }
            };

            AssertFormat(expected, code, changedOptionSet: options);
        }

        [WorkItem(962416, "DevDiv")]
        [Fact, Trait(Traits.Feature, Traits.Features.Formatting)]
        public void TestCheckedAndUncheckedStatement()
        {
            var code = @"
class Program
{
    public void Method()
    {
        checked
            {
        // foo
}
            unchecked 
                    {
            }
    }
}";

            var expected = @"
class Program
{
    public void Method()
    {
        checked {
            // foo
        }
        unchecked {
        }
    }
}";

            var options = new Dictionary<OptionKey, object>()
            {
                { CSharpFormattingOptions.NewLinesForBracesInControlBlocks, false }
            };

            AssertFormat(expected, code, changedOptionSet: options);
        }

        [WorkItem(953535, "DevDiv")]
        [Fact, Trait(Traits.Feature, Traits.Features.Formatting)]
        public void ConditionalMemberAccess()
        {
            var code = @"
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
}";

            var expected = @"
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
}";
            var parseOptions = new CSharpParseOptions();
            AssertFormat(expected, code, parseOptions: parseOptions);
        }

        [WorkItem(924172, "DevDiv")]
        [Fact, Trait(Traits.Feature, Traits.Features.Formatting)]
        public void IgnoreSpacesInDeclarationStatementEnabled()
        {
            var changingOptions = new Dictionary<OptionKey, object>();
            changingOptions.Add(CSharpFormattingOptions.SpacesIgnoreAroundVariableDeclaration, true);
            var code = @"
class Program
{
    static void Main(string[] args)
    {
        int       s;
    }
}";

            var expected = @"
class Program
{
    static void Main(string[] args)
    {
        int       s;
    }
}";
            AssertFormat(expected, code, changedOptionSet: changingOptions);
        }

        [WorkItem(899492, "DevDiv")]
        [Fact, Trait(Traits.Feature, Traits.Features.Formatting)]
        public void CommentIsLeadingTriviaOfStatementNotLabel()
        {
            var code = @"
class C
{
    void M()
    {
    label:
        // comment
        M();
        M();
    }
}";

            var expected = @"
class C
{
    void M()
    {
    label:
        // comment
        M();
        M();
    }
}";
            AssertFormat(expected, code);
        }

        [WorkItem(991547, "DevDiv")]
        [Fact, Trait(Traits.Feature, Traits.Features.Formatting)]
        public void DontWrappingTryCatchFinallyIfOnSingleLine()
        {
            var code = @"
class C
{
    void M()
    {
        try { }
        catch { }
        finally { }
    }
}";

            var expected = @"
class C
{
    void M()
    {
        try { }
        catch { }
        finally { }
    }
}";
            AssertFormat(expected, code);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Formatting)]
        public void InterpolatedStrings1()
        {
            var code = @"
class C
{
    void M()
    {
        var a = ""World"";
        var b = $""Hello, {a}"";
    }
}";

            var expected = @"
class C
{
    void M()
    {
        var a = ""World"";
        var b = $""Hello, {a}"";
    }
}";

            AssertFormat(expected, code);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Formatting)]
        public void InterpolatedStrings2()
        {
            var code = @"
class C
{
    void M()
    {
        var a = ""Hello"";
        var b = ""World"";
        var c = $""{a}, {b}"";
    }
}";

            var expected = @"
class C
{
    void M()
    {
        var a = ""Hello"";
        var b = ""World"";
        var c = $""{a}, {b}"";
    }
}";

            AssertFormat(expected, code);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Formatting)]
        public void InterpolatedStrings3()
        {
            var code = @"
class C
{
    void M()
    {
        var a = ""World"";
        var b = $""Hello, { a }"";
    }
}";

            var expected = @"
class C
{
    void M()
    {
        var a = ""World"";
        var b = $""Hello, { a }"";
    }
}";

            AssertFormat(expected, code);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Formatting)]
        public void InterpolatedStrings4()
        {
            var code = @"
class C
{
    void M()
    {
        var a = ""Hello"";
        var b = ""World"";
        var c = $""{ a }, { b }"";
    }
}";

            var expected = @"
class C
{
    void M()
    {
        var a = ""Hello"";
        var b = ""World"";
        var c = $""{ a }, { b }"";
    }
}";

            AssertFormat(expected, code);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Formatting)]
        public void InterpolatedStrings5()
        {
            var code = @"
class C
{
    void M()
    {
        var a = ""World"";
        var b = $@""Hello, {a}"";
    }
}";

            var expected = @"
class C
{
    void M()
    {
        var a = ""World"";
        var b = $@""Hello, {a}"";
    }
}";

            AssertFormat(expected, code);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Formatting)]
        public void InterpolatedStrings6()
        {
            var code = @"
class C
{
    void M()
    {
        var a = ""Hello"";
        var b = ""World"";
        var c = $@""{a}, {b}"";
    }
}";

            var expected = @"
class C
{
    void M()
    {
        var a = ""Hello"";
        var b = ""World"";
        var c = $@""{a}, {b}"";
    }
}";

            AssertFormat(expected, code);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Formatting)]
        public void InterpolatedStrings7()
        {
            var code = @"
class C
{
    void M()
    {
        var a = ""World"";
        var b = $@""Hello, { a }"";
    }
}";

            var expected = @"
class C
{
    void M()
    {
        var a = ""World"";
        var b = $@""Hello, { a }"";
    }
}";

            AssertFormat(expected, code);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Formatting)]
        public void InterpolatedStrings8()
        {
            var code = @"
class C
{
    void M()
    {
        var a = ""Hello"";
        var b = ""World"";
        var c = $@""{ a }, { b }"";
    }
}";

            var expected = @"
class C
{
    void M()
    {
        var a = ""Hello"";
        var b = ""World"";
        var c = $@""{ a }, { b }"";
    }
}";

            AssertFormat(expected, code);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Formatting)]
        public void InterpolatedStrings9()
        {
            var code = @"
class C
{
    void M()
    {
        var a = ""Hello"";
        var c = $""{ a }, World"";
    }
}";

            var expected = @"
class C
{
    void M()
    {
        var a = ""Hello"";
        var c = $""{ a }, World"";
    }
}";

            AssertFormat(expected, code);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Formatting)]
        public void InterpolatedStrings10()
        {
            var code = @"
class C
{
    void M()
    {
        var s = $""{42 , -4 :x}"";
    }
}";

            var expected = @"
class C
{
    void M()
    {
        var s = $""{42,-4:x}"";
    }
}";

            AssertFormat(expected, code);
        }

        [WorkItem(1041787)]
        [WorkItem(1151, "https://github.com/dotnet/roslyn/issues/1151")]
        [Fact, Trait(Traits.Feature, Traits.Features.Formatting)]
        public void ReconstructWhitespaceStringUsingTabs_SingleLineComment()
        {
            var optionSet = new Dictionary<OptionKey, object> { { new OptionKey(FormattingOptions.UseTabs, LanguageNames.CSharp), true } };
            AssertFormat(@"using System;

class Program
{
	static void Main(string[] args)
	{
		Console.WriteLine("""");        // FooBar
	}
}", @"using System;

class Program
{
    static void Main(string[] args)
    {
        Console.WriteLine("""");        // FooBar
    }
}", false, optionSet);
        }

        [WorkItem(961559)]
        [WorkItem(1041787)]
        [WorkItem(1151, "https://github.com/dotnet/roslyn/issues/1151")]
        [Fact, Trait(Traits.Feature, Traits.Features.Formatting)]
        public void ReconstructWhitespaceStringUsingTabs_MultiLineComment()
        {
            var optionSet = new Dictionary<OptionKey, object> { { new OptionKey(FormattingOptions.UseTabs, LanguageNames.CSharp), true } };
            AssertFormat(@"using System;

class Program
{
	static void Main(string[] args)
	{
		Console.WriteLine("""");        /* FooBar */
	}
}", @"using System;

class Program
{
    static void Main(string[] args)
    {
        Console.WriteLine("""");        /* FooBar */
    }
}", false, optionSet);
        }

        [WorkItem(1100920)]
        [Fact, Trait(Traits.Feature, Traits.Features.Formatting)]
        public void NoLineOperationAroundInterpolationSyntax()
        {
            AssertFormat(@"class Program
{
    static string F(int a, int b, int c)
    {
        return $""{a} (index: 0x{ b}, size: { c}): ""
    }
}", @"class Program
{
    static string F(int a, int b, int c)
    {
        return $""{a} (index: 0x{ b}, size: { c}): ""
    }
}");
        }

        [WorkItem(62)]
        [Fact, Trait(Traits.Feature, Traits.Features.Formatting)]
        public void SpaceAfterWhenInExceptionFilter()
        {
            const string expected = @"class C
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
}";

            const string code = @"class C
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
}";
            AssertFormat(expected, code);
        }

        [WorkItem(285)]
        [WorkItem(1089196)]
        [Fact, Trait(Traits.Feature, Traits.Features.Formatting)]
        public void FormatHashInBadDirectiveToZeroColumnAnywhereInsideIfDef()
        {
            const string code = @"class MyClass
{
    static void Main(string[] args)
    {
#if false

            #

#endif
    }
}";

            const string expected = @"class MyClass
{
    static void Main(string[] args)
    {
#if false

#

#endif
    }
}";
            AssertFormat(expected, code);
        }

        [WorkItem(285)]
        [WorkItem(1089196)]
        [Fact, Trait(Traits.Feature, Traits.Features.Formatting)]
        public void FormatHashElseToZeroColumnAnywhereInsideIfDef()
        {
            const string code = @"class MyClass
{
    static void Main(string[] args)
    {
#if false

            #else
        Appropriate indentation should be here though #
#endif
    }
}";

            const string expected = @"class MyClass
{
    static void Main(string[] args)
    {
#if false

#else
        Appropriate indentation should be here though #
#endif
    }
}";
            AssertFormat(expected, code);
        }

        [WorkItem(285)]
        [WorkItem(1089196)]
        [Fact, Trait(Traits.Feature, Traits.Features.Formatting)]
        public void FormatHashsToZeroColumnAnywhereInsideIfDef()
        {
            const string code = @"class MyClass
{
    static void Main(string[] args)
    {
#if false

            #else
        #

#endif
    }
}";

            const string expected = @"class MyClass
{
    static void Main(string[] args)
    {
#if false

#else
#

#endif
    }
}";
            AssertFormat(expected, code);
        }

        [WorkItem(1118, "https://github.com/dotnet/roslyn/issues/1118")]
        [Fact, Trait(Traits.Feature, Traits.Features.Formatting)]
        public void DontAssumeCertainNodeAreAlwaysParented()
        {
            var block = SyntaxFactory.Block();
            Formatter.Format(block, new AdhocWorkspace());
        }

        [WorkItem(776, "https://github.com/dotnet/roslyn/issues/776")]
        [Fact, Trait(Traits.Feature, Traits.Features.Formatting)]
        public void SpacingRulesAroundMethodCallAndParenthesisAppliedInAttributeNonDefault()
        {
            var changingOptions = new Dictionary<OptionKey, object>();
            changingOptions.Add(CSharpFormattingOptions.SpaceAfterMethodCallName, true);
            changingOptions.Add(CSharpFormattingOptions.SpaceBetweenEmptyMethodCallParentheses, true);
            changingOptions.Add(CSharpFormattingOptions.SpaceWithinMethodCallParentheses, true);
            AssertFormat(@"[Obsolete ( ""Test"" ), Obsolete ( )]
class Program
{
    static void Main(string[] args)
    {
    }
}", @"[Obsolete(""Test""), Obsolete()]
class Program
{
    static void Main(string[] args)
    {
    }
}", false, changingOptions);
        }

        [WorkItem(776, "https://github.com/dotnet/roslyn/issues/776")]
        [Fact, Trait(Traits.Feature, Traits.Features.Formatting)]
        public void SpacingRulesAroundMethodCallAndParenthesisAppliedInAttribute()
        {
            var code = @"[Obsolete(""Test""), Obsolete()]
class Program
{
    static void Main(string[] args)
    {
    }
}";
            AssertFormat(code, code);
        }

        [Fact]
        public void SpacingInMethodCallArguments_True()
        {
            const string code = @"
[Bar(A=1,B=2)]
class Program
{
    public void foo()
    {
        var a = typeof(A);
        var b = M(a);
        var c = default(A);
        var d = sizeof(A);
        M();
    }
}";
            const string expected = @"
[Bar ( A = 1, B = 2 )]
class Program
{
    public void foo()
    {
        var a = typeof ( A );
        var b = M ( a );
        var c = default ( A );
        var d = sizeof ( A );
        M ( );
    }
}";
            var optionSet = new Dictionary<OptionKey, object>
            {
                { CSharpFormattingOptions.SpaceWithinMethodCallParentheses, true },
                { CSharpFormattingOptions.SpaceAfterMethodCallName, true },
                { CSharpFormattingOptions.SpaceBetweenEmptyMethodCallParentheses, true },
            };
            AssertFormat(expected, code, changedOptionSet: optionSet);
        }

        [WorkItem(1298, "https://github.com/dotnet/roslyn/issues/1298")]
        [Fact, Trait(Traits.Feature, Traits.Features.Formatting)]
        public void DontforceAccessorsToNewLineWithPropertyInitializers()
        {
            var code = @"using System.Collections.Generic;

class Program
{
    public List<ExcludeValidation> ValidationExcludeFilters { get; }
    = new List<ExcludeValidation>();
}

public class ExcludeValidation
{
}";

            var expected = @"using System.Collections.Generic;

class Program
{
    public List<ExcludeValidation> ValidationExcludeFilters { get; }
    = new List<ExcludeValidation>();
}

public class ExcludeValidation
{
}";
            AssertFormat(expected, code);
        }

        [WorkItem(1339, "https://github.com/dotnet/roslyn/issues/1339")]
        [Fact, Trait(Traits.Feature, Traits.Features.Formatting)]
        public void DontFormatAutoPropertyInitializerIfNotDifferentLine()
        {
            var code = @"class Program
{
    public int d { get; }
            = 3;
    static void Main(string[] args)
    {
    }
}";
            AssertFormat(code, code);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Formatting)]
        public void SpacingForForStatementInfiniteLoop()
        {
            var code = @"
class Program
{
    void Main()
    {
        for ( ; ; )
        {
        }
    }
}";
            var expected = @"
class Program
{
    void Main()
    {
        for (;;)
        {
        }
    }
}";
            AssertFormat(expected, code);
        }

        [WorkItem(4421, "https://github.com/dotnet/roslyn/issues/4421")]
        [WorkItem(4240, "https://github.com/dotnet/roslyn/issues/4240")]
        [Fact, Trait(Traits.Feature, Traits.Features.Formatting)]
        public void VerifySpacingAfterMethodDeclarationName_Default()
        {
            var code = @"class Program<T>
{
    public static Program operator +   (Program p1, Program p2) { return null; }
    public static implicit operator string (Program p) { return null; }
    public static void M  () { }
    public void F<T>    () { }
}";
            var expected = @"class Program<T>
{
    public static Program operator +(Program p1, Program p2) { return null; }
    public static implicit operator string(Program p) { return null; }
    public static void M() { }
    public void F<T>() { }
}";
            AssertFormat(expected, code);
        }

        [WorkItem(4240, "https://github.com/dotnet/roslyn/issues/4240")]
        [WorkItem(4421, "https://github.com/dotnet/roslyn/issues/4421")]
        [Fact, Trait(Traits.Feature, Traits.Features.Formatting)]
        public void VerifySpacingAfterMethodDeclarationName_NonDefault()
        {
            var changingOptions = new Dictionary<OptionKey, object>();
            changingOptions.Add(CSharpFormattingOptions.SpacingAfterMethodDeclarationName, true);
            var code = @"class Program<T>
{
    public static Program operator +   (Program p1, Program p2) { return null; }
    public static implicit operator string     (Program p) { return null; }
    public static void M  () { }
    public void F<T>   () { }
}";
            var expected = @"class Program<T>
{
    public static Program operator + (Program p1, Program p2) { return null; }
    public static implicit operator string (Program p) { return null; }
    public static void M () { }
    public void F<T> () { }
}";
            AssertFormat(expected, code, changedOptionSet: changingOptions);
        }

        [WorkItem(939, "https://github.com/dotnet/roslyn/issues/939")]
        [Fact, Trait(Traits.Feature, Traits.Features.Formatting)]
        public void DontFormatInsideArrayInitializers()
        {
            var code = @"class Program
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
}";
            AssertFormat(code, code);
        }

        [WorkItem(1184285)]
        [WorkItem(4280, "https://github.com/dotnet/roslyn/issues/4280")]
        [Fact, Trait(Traits.Feature, Traits.Features.Formatting)]
        public void FormatDictionaryInitializers()
        {
            var code = @"class Program
{
    void Main()
    {
        var sample = new Dictionary<string, string> {[""x""] = ""d""    ,[""z""]   =  ""XX"" };
    }
}";
            var expected = @"class Program
{
    void Main()
    {
        var sample = new Dictionary<string, string> { [""x""] = ""d"", [""z""] = ""XX"" };
    }
}";
            AssertFormat(expected, code);
        }

        [WorkItem(3256, "https://github.com/dotnet/roslyn/issues/3256")]
        [Fact, Trait(Traits.Feature, Traits.Features.Formatting)]
        public void SwitchSectionHonorsNewLineForBracesinControlBlockOption_Default()
        {
            var code = @"class Program
{
    public void foo()
    {
        int f = 1;
        switch (f) {
            case 1: {
                    // DO nothing
                    break;
                }
        }
    }
}";
            var expected = @"class Program
{
    public void foo()
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
}";
            AssertFormat(expected, code);
        }

        [WorkItem(3256, "https://github.com/dotnet/roslyn/issues/3256")]
        [Fact, Trait(Traits.Feature, Traits.Features.Formatting)]
        public void SwitchSectionHonorsNewLineForBracesinControlBlockOption_NonDefault()
        {
            var changingOptions = new Dictionary<OptionKey, object>();
            changingOptions.Add(CSharpFormattingOptions.NewLinesForBracesInControlBlocks, false);
            var code = @"class Program
{
    public void foo()
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
}";

            var expected = @"class Program
{
    public void foo()
    {
        int f = 1;
        switch (f) {
            case 1: {
                    // DO nothing
                    break;
                }
        }
    }
}";
            AssertFormat(expected, code, changedOptionSet: changingOptions);
        }

        [WorkItem(4014, "https://github.com/dotnet/roslyn/issues/4014")]
        [Fact, Trait(Traits.Feature, Traits.Features.Formatting)]
        public void FormattingCodeWithMissingTokensShouldRespectFormatTabsOption1()
        {
            var optionSet = new Dictionary<OptionKey, object> { { new OptionKey(FormattingOptions.UseTabs, LanguageNames.CSharp), true } };

            AssertFormat(@"class Program
{
	static void Main()
	{
		return // Note the missing semicolon
	} // The tab here should stay a tab
}", @"class Program
{
	static void Main()
	{
		return // Note the missing semicolon
	} // The tab here should stay a tab
}", changedOptionSet: optionSet);
        }

        [WorkItem(4014, "https://github.com/dotnet/roslyn/issues/4014")]
        [Fact, Trait(Traits.Feature, Traits.Features.Formatting)]
        public void FormattingCodeWithMissingTokensShouldRespectFormatTabsOption2()
        {
            var optionSet = new Dictionary<OptionKey, object> { { new OptionKey(FormattingOptions.UseTabs, LanguageNames.CSharp), true } };

            AssertFormat(@"struct Foo
{
	private readonly string bar;

	public Foo(readonly string bar)
	{
	}
}", @"struct Foo
{
	private readonly string bar;

	public Foo(readonly string bar)
	{
	}
}", changedOptionSet: optionSet);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Formatting)]
        [WorkItem(84, "https://github.com/dotnet/roslyn/issues/84")]
        [WorkItem(849870, "DevDiv")]
        public void NewLinesForBracesInPropertiesTest()
        {
            var changingOptions = new Dictionary<OptionKey, object>();
            changingOptions.Add(CSharpFormattingOptions.NewLinesForBracesInProperties, false);
            AssertFormat(@"class Class2
{
    int Foo {
        get
        {
            return 1;
        }
    }

    int MethodFoo()
    {
        return 42;
    }
}", @"class Class2
{
    int Foo
    {
        get
        {
            return 1;
        }
    }

    int MethodFoo()
    {
        return 42; 
    }
}", false, changingOptions);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Formatting)]
        [WorkItem(849870, "DevDiv")]
        [WorkItem(84, "https://github.com/dotnet/roslyn/issues/84")]
        public void NewLinesForBracesInAccessorsTest()
        {
            var changingOptions = new Dictionary<OptionKey, object>();
            changingOptions.Add(CSharpFormattingOptions.NewLinesForBracesInAccessors, false);
            AssertFormat(@"class Class2
{
    int Foo
    {
        get {
            return 1;
        }
    }

    int MethodFoo()
    {
        return 42;
    }
}", @"class Class2
{
    int Foo
    {
        get
        {
            return 1;
        }
    }

    int MethodFoo()
    {
        return 42; 
    }
}", false, changingOptions);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Formatting)]
        [WorkItem(849870, "DevDiv")]
        [WorkItem(84, "https://github.com/dotnet/roslyn/issues/84")]
        public void NewLinesForBracesInPropertiesAndAccessorsTest()
        {
            var changingOptions = new Dictionary<OptionKey, object>();
            changingOptions.Add(CSharpFormattingOptions.NewLinesForBracesInProperties, false);
            changingOptions.Add(CSharpFormattingOptions.NewLinesForBracesInAccessors, false);
            AssertFormat(@"class Class2
{
    int Foo {
        get {
            return 1;
        }
    }

    int MethodFoo()
    {
        return 42;
    }
}", @"class Class2
{
    int Foo
    {
        get
        {
            return 1;
        }
    }

    int MethodFoo()
    {
        return 42; 
    }
}", false, changingOptions);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Formatting)]
        [WorkItem(111079, "devdiv.visualstudio.com")]
        public void TestThrowInIfOnSingleLine()
        {
            var code = @"
class C
{
    void M()
    {
        if (true) throw new Exception(
            ""message"");
    }
}
";

            AssertFormat(code, code);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Formatting)]
        [WorkItem(1711675, "https://connect.microsoft.com/VisualStudio/feedback/details/1711675/autoformatting-issues")]
        public void SingleLinePropertiesPreservedWithLeaveStatementsAndMembersOnSingleLineFalse()
        {
            var changedOptionSet = new Dictionary<OptionKey, object>
            {
                { CSharpFormattingOptions.WrappingPreserveSingleLine, true },
                { CSharpFormattingOptions.WrappingKeepStatementsOnSingleLine, false},
            };

            AssertFormat(@"
class C
{
    string Name { get; set; }
}", @"
class C
{
    string  Name    {    get    ;   set     ;    }
}", changedOptionSet: changedOptionSet);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Formatting)]
        [WorkItem(4720, "https://github.com/dotnet/roslyn/issues/4720")]
        public void KeepAccessorWithAttributeOnSingleLine()
        {
            AssertFormat(@"
class Program
{
    public Int32 PaymentMethodID
    {
        [System.Diagnostics.DebuggerStepThrough]
        get { return 10; }
    }
}", @"
class Program
{
    public Int32 PaymentMethodID
    {
        [System.Diagnostics.DebuggerStepThrough]
        get { return 10; }
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Formatting)]
        [WorkItem(4720, "https://github.com/dotnet/roslyn/issues/4720")]
        public void OneSpaceBetweenAccessorsAndAttributes()
        {
            AssertFormat(@"
class Program
{
    public int SomeProperty { [SomeAttribute] get; [SomeAttribute] private set; }
}", @"
class Program
{
    public int SomeProperty {    [SomeAttribute] get;    [SomeAttribute] private set; }
}");
        }
    }
}
