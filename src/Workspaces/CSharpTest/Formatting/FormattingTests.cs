// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Formatting;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;
using static Microsoft.CodeAnalysis.CSharp.Formatting.CSharpFormattingOptions;
using System;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.Formatting
{
    public class FormattingEngineTests : CSharpFormattingTestBase
    {
        [Fact, Trait(Traits.Feature, Traits.Features.Formatting)]
        public async Task Format1()
        {
            await AssertFormatAsync("namespace A { }", "namespace A{}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Formatting)]
        public async Task Format2()
        {
            var content = @"class A {
            }";

            var expected = @"class A
{
}";
            await AssertFormatAsync(expected, content);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Formatting)]
        public async Task Format3()
        {
            var content = @"class A
            {        
int             i               =               20          ;           }";

            var expected = @"class A
{
    int i = 20;
}";

            await AssertFormatAsync(expected, content);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Formatting)]
        public async Task Format4()
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

            await AssertFormatAsync(expected, content);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Formatting)]
        public async Task Format5()
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

            await AssertFormatAsync(expected, content);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Formatting)]
        public async Task Format6()
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

            await AssertFormatAsync(expected, content);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Formatting)]
        public async Task Format7()
        {
            var content = @"class A
            {        
    var             a           =           from        i       in          new        [  ]     {           1           ,       2           ,       3       }       where           i       >       10          select      i           ;           
}";

            var expected = @"class A
{
    var a = from i in new[] { 1, 2, 3 } where i > 10 select i;
}";

            await AssertFormatAsync(expected, content);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Formatting)]
        public async Task Format8()
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

            await AssertFormatAsync(expected, content);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Formatting)]
        public async Task Format9()
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

            await AssertFormatAsync(expected, content);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Formatting)]
        public async Task Format10()
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

            await AssertFormatAsync(expected, content);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Formatting)]
        public async Task ObjectInitializer()
        {
            await AssertFormatAsync(@"public class C
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
}", @"public class C
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
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Formatting)]
        public async Task AnonymousType()
        {
            await AssertFormatAsync(@"class C
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
        public async Task MultilineLambda()
        {
            await AssertFormatAsync(@"class C
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
        public async Task AnonymousMethod()
        {
            await AssertFormatAsync(@"class C
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
        public async Task Scen1()
        {
            await AssertFormatAsync(@"namespace Namespace1
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
        public async Task Scen2()
        {
            await AssertFormatAsync(@"namespace MyNamespace
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
        public async Task Scen3()
        {
            await AssertFormatAsync(@"namespace Namespace1
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
        public async Task Scen4()
        {
            await AssertFormatAsync(@"class Class1
{
    //	public void goo()
    //	{
    //		// TODO: Add the implementation for Class1.goo() here.
    //	
    //	}
}", @"class Class1
{
    //	public void goo()
//	{
//		// TODO: Add the implementation for Class1.goo() here.
//	
//	}
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Formatting)]
        public async Task Scen5()
        {
            await AssertFormatAsync(@"class Class1
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
        public async Task Scen6()
        {
            await AssertFormatAsync(@"namespace Namespace1
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
        public async Task Scen7()
        {
            await AssertFormatAsync(@"class Class1
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
        public async Task Scen8()
        {
            await AssertFormatAsync(@"class Class1
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
        public async Task IndentStatementsInMethod()
        {
            await AssertFormatAsync(@"class C
{
    void Goo()
    {
        int x = 0;
        int y = 0;
        int z = 0;
    }
}", @"class C
{
    void Goo()
    {
        int x = 0;
            int y = 0;
      int z = 0;
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Formatting)]
        public async Task IndentFieldsInClass()
        {
            await AssertFormatAsync(@"class C
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
        public async Task IndentUserDefaultSettingTest()
        {
            await AssertFormatAsync(@"class Class2
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

        [WorkItem(766133, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/766133")]
        [Fact, Trait(Traits.Feature, Traits.Features.Formatting)]
        public async Task RelativeIndentationToFirstTokenInBaseTokenWithObjectInitializers()
        {
            var changingOptions = new Dictionary<OptionKey, object>
            {
                { CSharpFormattingOptions.NewLinesForBracesInObjectCollectionArrayInitializers, false }
            };
            await AssertFormatAsync(@"class Program
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
        public async Task RemoveSpacingAroundBinaryOperatorsShouldMakeAtLeastOneSpaceForIsAndAsKeywords()
        {
            var changingOptions = new Dictionary<OptionKey, object>
            {
                { CSharpFormattingOptions.SpacingAroundBinaryOperator, BinaryOperatorSpacingOptions.Remove }
            };
            await AssertFormatAsync(@"class Class2
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

        [WorkItem(772298, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/772298")]
        [Fact, Trait(Traits.Feature, Traits.Features.Formatting)]
        public async Task IndentUserSettingNonDefaultTest_OpenBracesOfLambdaWithNoNewLine()
        {
            var changingOptions = new Dictionary<OptionKey, object>
            {
                { IndentBraces, true },
                { IndentBlock, false },
                { IndentSwitchSection, false },
                { IndentSwitchCaseSection, false },
                { NewLinesForBracesInLambdaExpressionBody, false },
                { LabelPositioning, LabelPositionOptions.LeftMost }
            };

            await AssertFormatAsync(@"class Class2
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
}", changedOptionSet: changingOptions);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Formatting)]
        public async Task IndentUserSettingNonDefaultTest()
        {
            var changingOptions = new Dictionary<OptionKey, object>
            {
                { IndentBraces, true },
                { IndentBlock, false },
                { IndentSwitchSection, false },
                { IndentSwitchCaseSection, false },
                { IndentSwitchCaseSectionWhenBlock, false },
                { LabelPositioning, LabelPositionOptions.LeftMost }
            };

            await AssertFormatAsync(@"class Class2
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
}", changedOptionSet: changingOptions);
        }

        [WorkItem(20009, "https://github.com/dotnet/roslyn/issues/20009")]
        [Fact, Trait(Traits.Feature, Traits.Features.Formatting)]
        public async Task IndentSwitch_IndentCase_IndentWhenBlock()
        {
            var changingOptions = new Dictionary<OptionKey, object>
            {
                { IndentSwitchSection, true },
                { IndentSwitchCaseSection, true },
                { IndentSwitchCaseSectionWhenBlock, true },
            };

            await AssertFormatAsync(
@"class Class2
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
}",
@"class Class2
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
}", changedOptionSet: changingOptions);
        }

        [WorkItem(20009, "https://github.com/dotnet/roslyn/issues/20009")]
        [Fact, Trait(Traits.Feature, Traits.Features.Formatting)]
        public async Task IndentSwitch_IndentCase_NoIndentWhenBlock()
        {
            var changingOptions = new Dictionary<OptionKey, object>
            {
                { IndentSwitchSection, true },
                { IndentSwitchCaseSection, true },
                { IndentSwitchCaseSectionWhenBlock, false },
            };

            await AssertFormatAsync(
@"class Class2
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
}",
@"class Class2
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
}", changedOptionSet: changingOptions);
        }

        [WorkItem(20009, "https://github.com/dotnet/roslyn/issues/20009")]
        [Fact, Trait(Traits.Feature, Traits.Features.Formatting)]
        public async Task IndentSwitch_NoIndentCase_IndentWhenBlock()
        {
            var changingOptions = new Dictionary<OptionKey, object>
            {
                { IndentSwitchSection, true },
                { IndentSwitchCaseSection, false },
                { IndentSwitchCaseSectionWhenBlock, true },
            };

            await AssertFormatAsync(
@"class Class2
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
}",
@"class Class2
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
}", changedOptionSet: changingOptions);
        }

        [WorkItem(20009, "https://github.com/dotnet/roslyn/issues/20009")]
        [Fact, Trait(Traits.Feature, Traits.Features.Formatting)]
        public async Task IndentSwitch_NoIndentCase_NoIndentWhenBlock()
        {
            var changingOptions = new Dictionary<OptionKey, object>
            {
                { IndentSwitchSection, true },
                { IndentSwitchCaseSection, false },
                { IndentSwitchCaseSectionWhenBlock, false },
            };

            await AssertFormatAsync(
@"class Class2
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
}",
@"class Class2
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
}", changedOptionSet: changingOptions);
        }

        [WorkItem(20009, "https://github.com/dotnet/roslyn/issues/20009")]
        [Fact, Trait(Traits.Feature, Traits.Features.Formatting)]
        public async Task NoIndentSwitch_IndentCase_IndentWhenBlock()
        {
            var changingOptions = new Dictionary<OptionKey, object>
            {
                { IndentSwitchSection, false },
                { IndentSwitchCaseSection, true },
                { IndentSwitchCaseSectionWhenBlock, true },
            };

            await AssertFormatAsync(
@"class Class2
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
}",
@"class Class2
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
}", changedOptionSet: changingOptions);
        }

        [WorkItem(20009, "https://github.com/dotnet/roslyn/issues/20009")]
        [Fact, Trait(Traits.Feature, Traits.Features.Formatting)]
        public async Task NoIndentSwitch_IndentCase_NoIndentWhenBlock()
        {
            var changingOptions = new Dictionary<OptionKey, object>
            {
                { IndentSwitchSection, false },
                { IndentSwitchCaseSection, true },
                { IndentSwitchCaseSectionWhenBlock, false },
            };

            await AssertFormatAsync(
@"class Class2
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
}",
@"class Class2
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
}", changedOptionSet: changingOptions);
        }

        [WorkItem(20009, "https://github.com/dotnet/roslyn/issues/20009")]
        [Fact, Trait(Traits.Feature, Traits.Features.Formatting)]
        public async Task NoIndentSwitch_NoIndentCase_IndentWhenBlock()
        {
            var changingOptions = new Dictionary<OptionKey, object>
            {
                { IndentSwitchSection, false },
                { IndentSwitchCaseSection, false },
                { IndentSwitchCaseSectionWhenBlock, true },
            };

            await AssertFormatAsync(
@"class Class2
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
}",
@"class Class2
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
}", changedOptionSet: changingOptions);
        }

        [WorkItem(20009, "https://github.com/dotnet/roslyn/issues/20009")]
        [Fact, Trait(Traits.Feature, Traits.Features.Formatting)]
        public async Task NoIndentSwitch_NoIndentCase_NoIndentWhenBlock()
        {
            var changingOptions = new Dictionary<OptionKey, object>
            {
                { IndentSwitchSection, false },
                { IndentSwitchCaseSection, false },
                { IndentSwitchCaseSectionWhenBlock, false },
            };

            await AssertFormatAsync(
@"class Class2
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
}",
@"class Class2
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
}", changedOptionSet: changingOptions);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Formatting)]
        public async Task TestWrappingDefault()
        {
            await AssertFormatAsync(@"class Class5
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
        public async Task TestWrappingNonDefault_FormatBlock()
        {
            var changingOptions = new Dictionary<OptionKey, object>
            {
                { CSharpFormattingOptions.WrappingPreserveSingleLine, false }
            };
            await AssertFormatAsync(@"class Class5
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
        void goo() { int xx = 0; int zz = 0;}
}
class goo{int x = 0;}", false, changingOptions);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Formatting)]
        public async Task TestWrappingNonDefault_FormatStatmtMethDecl()
        {
            var changingOptions = new Dictionary<OptionKey, object>
            {
                { CSharpFormattingOptions.WrappingKeepStatementsOnSingleLine, false }
            };
            await AssertFormatAsync(@"class Class5
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
        void goo(){int y=0; int z =0 ;}
}
class goo
{
    int x = 0;
}", false, changingOptions);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Formatting)]
        public async Task TestWrappingNonDefault()
        {
            var changingOptions = new Dictionary<OptionKey, object>
            {
                { CSharpFormattingOptions.WrappingPreserveSingleLine, false },
                { CSharpFormattingOptions.WrappingKeepStatementsOnSingleLine, false }
            };
            await AssertFormatAsync(@"class Class5
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
class goo{int x = 0;}", false, changingOptions);
        }

        [WorkItem(991480, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/991480")]
        [Fact, Trait(Traits.Feature, Traits.Features.Formatting)]
        public async Task TestLeaveStatementMethodDeclarationSameLineNotAffectingForStatement()
        {
            var changingOptions = new Dictionary<OptionKey, object>
            {
                { CSharpFormattingOptions.WrappingKeepStatementsOnSingleLine, false }
            };
            await AssertFormatAsync(@"class Program
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

        [WorkItem(751789, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/751789")]
        [Fact, Trait(Traits.Feature, Traits.Features.Formatting)]
        public async Task NewLineForOpenBracesDefault()
        {
            await AssertFormatAsync(@"class f00
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
}");
        }

        [WorkItem(751789, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/751789")]
        [WorkItem(8808, "https://developercommunity.visualstudio.com/content/problem/8808/c-structure-guide-lines-for-unsafe-fixed.html")]
        [Fact, Trait(Traits.Feature, Traits.Features.Formatting)]
        public async Task NewLineForOpenBracesNonDefault()
        {
            var changingOptions = new Dictionary<OptionKey, object>
            {
                { NewLinesForBracesInTypes, false },
                { NewLinesForBracesInMethods, false },
                { NewLinesForBracesInAnonymousMethods, false },
                { NewLinesForBracesInControlBlocks, false },
                { NewLinesForBracesInAnonymousTypes, false },
                { NewLinesForBracesInObjectCollectionArrayInitializers, false },
                { NewLinesForBracesInLambdaExpressionBody, false }
            };
            await AssertFormatAsync(@"class f00 {
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
}", false, changingOptions);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Formatting)]
        public async Task NewLineForKeywordDefault()
        {
            await AssertFormatAsync(@"class c
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
        public async Task NewLineForKeywordNonDefault()
        {
            var changingOptions = new Dictionary<OptionKey, object>
            {
                { CSharpFormattingOptions.NewLineForElse, false },
                { CSharpFormattingOptions.NewLineForCatch, false },
                { CSharpFormattingOptions.NewLineForFinally, false }
            };
            await AssertFormatAsync(@"class c
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
        [WorkItem(33458, "https://github.com/dotnet/roslyn/issues/33458")]
        public async Task NoNewLineForElseChecksBraceOwner()
        {
            var changingOptions = new Dictionary<OptionKey, object>
            {
                { NewLineForElse, false },
                { NewLinesForBracesInControlBlocks, false },
            };

            await AssertFormatAsync(@"class Class
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
}", @"class Class
{
    void Method()
    {
        if (true)
            for (int i = 0; i < 10; i++) {
                Method();
            } else
            return;
    }
}", changedOptionSet: changingOptions);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Formatting)]
        public async Task NewLineForExpressionDefault()
        {
            await AssertFormatAsync(@"class f00
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
        public async Task NewLineForExpressionNonDefault()
        {
            var changingOptions = new Dictionary<OptionKey, object>
            {
                { CSharpFormattingOptions.NewLineForMembersInObjectInit, false },
                { CSharpFormattingOptions.NewLineForMembersInAnonymousTypes, false },
                { CSharpFormattingOptions.NewLineForClausesInQuery, false }
            };
            await AssertFormatAsync(@"class f00
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
        public async Task Enums_Bug2586()
        {
            await AssertFormatAsync(@"enum E
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
        public async Task DontInsertLineBreaksInSingleLineEnum()
        {
            await AssertFormatAsync(@"enum E { a = 10, b, c }", @"enum E { a = 10, b, c }");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Formatting)]
        public async Task AlreadyFormattedSwitchIsNotFormatted_Bug2588()
        {
            await AssertFormatAsync(@"class C
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
        public async Task BreaksAreAlignedInSwitchCasesFormatted_Bug2587()
        {
            await AssertFormatAsync(@"class C
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
        public async Task BreaksAndBracesAreAlignedInSwitchCasesWithBracesFormatted_Bug2587()
        {
            await AssertFormatAsync(@"class C
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
        public async Task LineBreaksAreNotInsertedForSwitchCasesOnASingleLine1()
        {
            await AssertFormatAsync(@"class C
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
        public async Task LineBreaksAreNotInsertedForSwitchCasesOnASingleLine2()
        {
            await AssertFormatAsync(@"class C
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
        public async Task FormatLabelAndGoto1_Bug2588()
        {
            await AssertFormatAsync(@"class C
{
    void M()
    {
    Goo:
        goto Goo;
    }
}", @"class C
{
    void M()
    {
Goo:
goto Goo;
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Formatting)]
        public async Task FormatLabelAndGoto2_Bug2588()
        {
            await AssertFormatAsync(@"class C
{
    void M()
    {
        int x = 0;
    Goo:
        goto Goo;
    }
}", @"class C
{
    void M()
    {
int x = 0;
Goo:
goto Goo;
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Formatting)]
        public async Task FormatNestedLabelAndGoto1_Bug2588()
        {
            await AssertFormatAsync(@"class C
{
    void M()
    {
        if (true)
        {
        Goo:
            goto Goo;
        }
    }
}", @"class C
{
    void M()
    {
if (true)
{
Goo:
goto Goo;
}
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Formatting)]
        public async Task FormatNestedLabelAndGoto2_Bug2588()
        {
            await AssertFormatAsync(@"class C
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
}", @"class C
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
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Formatting)]
        public async Task AlreadyFormattedGotoLabelIsNotFormatted1_Bug2588()
        {
            await AssertFormatAsync(@"class C
{
    void M()
    {
    Goo:
        goto Goo;
    }
}", @"class C
{
    void M()
    {
    Goo:
        goto Goo;
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Formatting)]
        public async Task AlreadyFormattedGotoLabelIsNotFormatted2_Bug2588()
        {
            await AssertFormatAsync(@"class C
{
    void M()
    {
    Goo: goto Goo;
    }
}", @"class C
{
    void M()
    {
    Goo: goto Goo;
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Formatting)]
        public async Task AlreadyFormattedGotoLabelIsNotFormatted3_Bug2588()
        {
            await AssertFormatAsync(@"class C
{
    void M()
    {
        int x = 0;
    Goo:
        goto Goo;
    }
}", @"class C
{
    void M()
    {
        int x = 0;
    Goo:
        goto Goo;
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Formatting)]
        public async Task DontAddLineBreakBeforeWhere1_Bug2582()
        {
            await AssertFormatAsync(@"class C
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
        public async Task DontAddLineBreakBeforeWhere2_Bug2582()
        {
            await AssertFormatAsync(@"class C<T> where T : I
{
}", @"class C<T> where T : I
{
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Formatting)]
        public async Task DontAddSpaceAfterUnaryMinus()
        {
            await AssertFormatAsync(@"class C
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
        public async Task DontAddSpaceAfterUnaryPlus()
        {
            await AssertFormatAsync(@"class C
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
        [WorkItem(545909, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545909")]
        public async Task DontAddSpaceAfterIncrement()
        {
            var code = @"class C
{
    void M(int[] i)
    {
        ++i[0];
    }
}";
            await AssertFormatAsync(code, code);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Formatting)]
        [WorkItem(545909, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545909")]
        public async Task DontAddSpaceBeforeIncrement()
        {
            var code = @"class C
{
    void M(int[] i)
    {
        i[0]++;
    }
}";
            await AssertFormatAsync(code, code);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Formatting)]
        [WorkItem(545909, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545909")]
        public async Task DontAddSpaceAfterDecrement()
        {
            var code = @"class C
{
    void M(int[] i)
    {
        --i[0];
    }
}";
            await AssertFormatAsync(code, code);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Formatting)]
        [WorkItem(545909, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545909")]
        public async Task DontAddSpaceBeforeDecrement()
        {
            var code = @"class C
{
    void M(int[] i)
    {
        i[0]--;
    }
}";
            await AssertFormatAsync(code, code);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Formatting)]
        public async Task Anchoring()
        {
            await AssertFormatAsync(@"class C
{
    void M()
    {
        Console.WriteLine(""Goo"",
            0, 1,
                2);
    }
}", @"class C
{
    void M()
    {
                    Console.WriteLine(""Goo"",
                        0, 1,
                            2);
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Formatting)]
        public async Task Exclamation()
        {
            await AssertFormatAsync(@"class C
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
        public async Task StartAndEndTrivia()
        {
            await AssertFormatAsync(@"


class C { }




", @"      
        
        
class C { }     
        
        
        
                    
");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Formatting)]
        public async Task FirstTriviaAndAnchoring1()
        {
            await AssertFormatAsync(@"
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
        public async Task FirstTriviaAndAnchoring2()
        {
            await AssertFormatAsync(@"
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
        public async Task FirstTriviaAndAnchoring3()
        {
            await AssertFormatAsync(@"

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
        public async Task Base1()
        {
            await AssertFormatAsync(@"class C
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
        public async Task This1()
        {
            await AssertFormatAsync(@"class C
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
        public async Task QueryExpression1()
        {
            await AssertFormatAsync(@"class C
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
        public async Task QueryExpression2()
        {
            await AssertFormatAsync(@"class C
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
        public async Task QueryExpression3()
        {
            await AssertFormatAsync(@"class C
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
        public async Task QueryExpression4()
        {
            await AssertFormatAsync(@"class C
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
        public async Task Label1()
        {
            await AssertFormatAsync(@"class C
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
        public async Task Label2()
        {
            await AssertFormatAsync(@"class C
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
        public async Task Label3()
        {
            await AssertFormatAsync(@"class C
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
        public async Task Label4()
        {
            await AssertFormatAsync(@"class C
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
        public async Task Label5()
        {
            await AssertFormatAsync(@"class C
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
        public async Task Label6()
        {
            await AssertFormatAsync(@"class C
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
        public async Task Label7()
        {
            await AssertFormatAsync(@"class C
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
        public async Task Label8()
        {
            await AssertFormatAsync(@"class C
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
        public async Task AutoProperty()
        {
            await AssertFormatAsync(@"class Class
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
        public async Task NormalPropertyGet()
        {
            await AssertFormatAsync(@"class Class
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
        public async Task NormalPropertyBoth()
        {
            await AssertFormatAsync(@"class Class
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
        public async Task ErrorHandling1()
        {
            await AssertFormatAsync(@"class C
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

        [WorkItem(537763, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/537763")]
        [Fact, Trait(Traits.Feature, Traits.Features.Formatting)]
        public async Task NullableType()
        {
            await AssertFormatAsync(@"class Program
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

        [WorkItem(537766, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/537766")]
        [Fact, Trait(Traits.Feature, Traits.Features.Formatting)]
        public async Task SuppressWrappingOnBraces()
        {
            await AssertFormatAsync(@"class Class1
{ }
", @"class Class1
{}
");
        }

        [WorkItem(537824, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/537824")]
        [Fact, Trait(Traits.Feature, Traits.Features.Formatting)]
        public async Task DoWhile()
        {
            await AssertFormatAsync(@"public class Class1
{
    void Goo()
    {
        do
        {
        } while (true);
    }
}
", @"public class Class1
{
    void Goo()
    {
        do
        {
        }while (true);
    }
}
");
        }

        [WorkItem(537774, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/537774")]
        [Fact, Trait(Traits.Feature, Traits.Features.Formatting)]
        public async Task SuppressWrappingBug()
        {
            await AssertFormatAsync(@"class Class1
{
    int Goo()
    {
        return 0;
    }
}
", @"class Class1
{
    int Goo()
    {return 0;
    }
}
");
        }

        [WorkItem(537768, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/537768")]
        [Fact, Trait(Traits.Feature, Traits.Features.Formatting)]
        public async Task PreserveLineForAttribute()
        {
            await AssertFormatAsync(@"class Class1
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

        [WorkItem(537878, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/537878")]
        [Fact, Trait(Traits.Feature, Traits.Features.Formatting)]
        public async Task NoFormattingOnMissingTokens()
        {
            await AssertFormatAsync(@"namespace ClassLibrary1
{
    class Class1
    {
        void Goo()
        {
            if (true)
        }
    }
}
", @"namespace ClassLibrary1
{
    class Class1
    {
        void Goo()
        {
            if (true)
        }
    }
}
");
        }

        [WorkItem(537783, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/537783")]
        [Fact, Trait(Traits.Feature, Traits.Features.Formatting)]
        public async Task UnaryExpression()
        {
            await AssertFormatAsync(@"class Program
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

        [WorkItem(537885, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/537885")]
        [Fact, Trait(Traits.Feature, Traits.Features.Formatting)]
        public async Task Pointer()
        {
            await AssertFormatAsync(@"class Program
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

        [WorkItem(537886, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/537886")]
        [Fact, Trait(Traits.Feature, Traits.Features.Formatting)]
        public async Task Tild()
        {
            await AssertFormatAsync(@"class Program
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

        [WorkItem(537884, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/537884")]
        [Fact, Trait(Traits.Feature, Traits.Features.Formatting)]
        public async Task ArrayInitializer1()
        {
            await AssertFormatAsync(@"class Program
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

        [WorkItem(537884, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/537884")]
        [Fact, Trait(Traits.Feature, Traits.Features.Formatting)]
        public async Task ArrayInitializer2()
        {
            await AssertFormatAsync(@"class Program
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

        [WorkItem(537884, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/537884")]
        [Fact, Trait(Traits.Feature, Traits.Features.Formatting)]
        public async Task ImplicitArrayInitializer()
        {
            await AssertFormatAsync(@"class Program
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

        [WorkItem(537884, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/537884")]
        [Fact, Trait(Traits.Feature, Traits.Features.Formatting)]
        public async Task CollectionInitializer()
        {
            await AssertFormatAsync(@"class Program
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

        [WorkItem(537916, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/537916")]
        [Fact, Trait(Traits.Feature, Traits.Features.Formatting)]
        public async Task AddressOfOperator()
        {
            await AssertFormatAsync(@"unsafe class Class1
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

        [WorkItem(537885, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/537885")]
        [Fact, Trait(Traits.Feature, Traits.Features.Formatting)]
        public async Task DereferenceOperator()
        {
            await AssertFormatAsync(@"unsafe class Class1
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

        [WorkItem(537905, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/537905")]
        [Fact, Trait(Traits.Feature, Traits.Features.Formatting)]
        public async Task Namespaces()
        {
            await AssertFormatAsync(@"using System;
using System.Data;", @"using System; using System.Data;");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Formatting)]
        public async Task NamespaceDeclaration()
        {
            await AssertFormatAsync(@"namespace N
{
}", @"namespace N
    {
}");
        }

        [WorkItem(537902, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/537902")]
        [Fact, Trait(Traits.Feature, Traits.Features.Formatting)]
        public async Task DoWhile1()
        {
            await AssertFormatAsync(@"class Program
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
        public async Task NewConstraint()
        {
            await AssertFormatAsync(@"class Program
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
        public async Task UnaryExpressionWithInitializer()
        {
            await AssertFormatAsync(@"using System;
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
        public async Task Attributes1()
        {
            await AssertFormatAsync(@"class Program
{
    [Flags] public void Method() { }
}", @"class Program
{
        [   Flags       ]       public       void       Method      (       )           {           }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Formatting)]
        public async Task Attributes2()
        {
            await AssertFormatAsync(@"class Program
{
    [Flags]
    public void Method() { }
}", @"class Program
{
        [   Flags       ]
public       void       Method      (       )           {           }
}");
        }

        [WorkItem(538288, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538288")]
        [Fact, Trait(Traits.Feature, Traits.Features.Formatting)]
        public async Task ColonColon1()
        {
            await AssertFormatAsync(@"class Program
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

        [WorkItem(538354, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538354")]
        [Fact, Trait(Traits.Feature, Traits.Features.Formatting)]
        public async Task BugFix3939()
        {
            await AssertFormatAsync(@"using
      System.
          Collections.
              Generic;", @"                  using
                        System.
                            Collections.
                                Generic;");
        }

        [WorkItem(538354, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538354")]
        [Fact, Trait(Traits.Feature, Traits.Features.Formatting)]
        public async Task Tab1()
        {
            await AssertFormatAsync(@"using System;", @"			using System;");
        }

        [WorkItem(538329, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538329")]
        [Fact, Trait(Traits.Feature, Traits.Features.Formatting)]
        public async Task SuppressLinkBreakInIfElseStatement()
        {
            await AssertFormatAsync(@"class Program
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

        [WorkItem(538464, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538464")]
        [Fact, Trait(Traits.Feature, Traits.Features.Formatting)]
        public async Task BugFix4087()
        {
            await AssertFormatAsync(@"class Program
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
        [WorkItem(538511, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538511")]
        [Trait(Traits.Feature, Traits.Features.Formatting)]
        public async Task AttributeTargetSpecifier()
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

            await AssertFormatAsync(expected, code);
        }

        [Fact]
        [WorkItem(538635, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538635")]
        [Trait(Traits.Feature, Traits.Features.Formatting)]
        public async Task Finalizer()
        {
            var code = @"public class Class1
{
    ~ Class1() { }
}";

            var expected = @"public class Class1
{
    ~Class1() { }
}";

            await AssertFormatAsync(expected, code);
        }

        [Fact]
        [WorkItem(538743, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538743")]
        [Trait(Traits.Feature, Traits.Features.Formatting)]
        public async Task BugFix4442()
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

            await AssertFormatAsync(code, code);
        }

        [Fact]
        [WorkItem(538658, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538658")]
        [Trait(Traits.Feature, Traits.Features.Formatting)]
        public async Task BugFix4328()
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
            await AssertFormatAsync(expected, code);
        }

        [Fact]
        [WorkItem(538658, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538658")]
        [Trait(Traits.Feature, Traits.Features.Formatting)]
        public async Task BugFix4515()
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
            await AssertFormatAsync(expected, code);
        }

        [Fact]
        [Trait(Traits.Feature, Traits.Features.Formatting)]
        public async Task CastExpressionTest()
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
            await AssertFormatAsync(expected, code);
        }

        [Fact]
        [Trait(Traits.Feature, Traits.Features.Formatting)]
        public async Task NamedParameter()
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
            await AssertFormatAsync(expected, code);
        }

        [WorkItem(539259, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539259")]
        [Fact]
        [Trait(Traits.Feature, Traits.Features.Formatting)]
        public async Task BugFix5143()
        {
            var code = @"class Program
{
    static void Main(string[] args)
    {
        int x = Goo (   
            delegate (  int     x   )   {   return  x    ; }    )   ;   
    }
}";
            var expected = @"class Program
{
    static void Main(string[] args)
    {
        int x = Goo(
            delegate (int x) { return x; });
    }
}";
            await AssertFormatAsync(expected, code);
        }

        [WorkItem(539338, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539338")]
        [Fact]
        [Trait(Traits.Feature, Traits.Features.Formatting)]
        public async Task BugFix5251()
        {
            var code = @"class Program
{
        public static string Goo { get; private set; }
}";
            var expected = @"class Program
{
    public static string Goo { get; private set; }
}";
            await AssertFormatAsync(expected, code);
        }

        [WorkItem(539358, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539358")]
        [Fact]
        [Trait(Traits.Feature, Traits.Features.Formatting)]
        public async Task BugFix5277()
        {
            var code = @"
#if true
            #endif
";
            var expected = @"
#if true
#endif
";
            await AssertFormatAsync(expected, code);
        }

        [WorkItem(539542, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539542")]
        [Fact]
        [Trait(Traits.Feature, Traits.Features.Formatting)]
        public async Task BugFix5544()
        {
            var code = @"
class Program
{
    unsafe static void Main(string[] args)
    {
        Program* p;
        p -> Goo = 5;
    }
}
";
            var expected = @"
class Program
{
    unsafe static void Main(string[] args)
    {
        Program* p;
        p->Goo = 5;
    }
}
";
            await AssertFormatAsync(expected, code);
        }

        [WorkItem(539587, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539587")]
        [Fact]
        [Trait(Traits.Feature, Traits.Features.Formatting)]
        public async Task BugFix5602()
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
            await AssertFormatAsync(expected, code);
        }

        [WorkItem(539616, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539616")]
        [Fact]
        [Trait(Traits.Feature, Traits.Features.Formatting)]
        public async Task BugFix5637()
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
            await AssertFormatAsync(expected, code);
        }

        [Fact]
        [Trait(Traits.Feature, Traits.Features.Formatting)]
        public async Task GenericType()
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
            await AssertFormatAsync(expected, code);
        }

        [Fact]
        [WorkItem(539878, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539878")]
        [Trait(Traits.Feature, Traits.Features.Formatting)]
        public async Task BugFix5978()
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
            await AssertFormatAsync(expected, code);
        }

        [Fact]
        [WorkItem(539878, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539878")]
        [Trait(Traits.Feature, Traits.Features.Formatting)]
        public async Task BugFix5979()
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
            await AssertFormatAsync(expected, code);
        }

        [Fact]
        [WorkItem(539891, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539891")]
        [Trait(Traits.Feature, Traits.Features.Formatting)]
        public async Task BugFix5993()
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
            await AssertFormatAsync(expected, code);
        }

        [Fact]
        [WorkItem(540315, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540315")]
        [Trait(Traits.Feature, Traits.Features.Formatting)]
        public async Task BugFix6536()
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
            await AssertFormatAsync(expected, code);
        }

        [Fact]
        [WorkItem(540801, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540801")]
        [Trait(Traits.Feature, Traits.Features.Formatting)]
        public async Task BugFix7211()
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
            await AssertFormatAsync(expected, code);
        }

        [Fact]
        [WorkItem(541035, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541035")]
        [Trait(Traits.Feature, Traits.Features.Formatting)]
        public async Task BugFix7564_1()
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
            await AssertFormatAsync(expected, code);
        }

        [Fact]
        [WorkItem(541035, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541035")]
        [Trait(Traits.Feature, Traits.Features.Formatting)]
        public async Task BugFix7564_2()
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
            await AssertFormatAsync(expected, code);
        }

        [Fact]
        [WorkItem(8385, "DevDiv_Projects/Roslyn")]
        [Trait(Traits.Feature, Traits.Features.Formatting)]
        public async Task NullCoalescingOperator()
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
            await AssertFormatAsync(expected, code);
        }

        [Fact]
        [WorkItem(541925, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541925")]
        [Trait(Traits.Feature, Traits.Features.Formatting)]
        public async Task QueryContinuation()
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
            await AssertFormatAsync(expected, code);
        }

        [Fact]
        [Trait(Traits.Feature, Traits.Features.Formatting)]
        public async Task QueryContinuation2()
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
            await AssertFormatAsync(expected, code);
        }

        [Fact]
        [WorkItem(542305, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542305")]
        [Trait(Traits.Feature, Traits.Features.Formatting)]
        public async Task AttributeFormatting1()
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
            await AssertFormatAsync(expected, code);
        }

        [Fact]
        [WorkItem(542304, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542304")]
        [Trait(Traits.Feature, Traits.Features.Formatting)]
        public async Task CloseBracesInArgumentList()
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
            await AssertFormatAsync(expected, code);
        }

        [Fact]
        [WorkItem(542538, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542538")]
        [Trait(Traits.Feature, Traits.Features.Formatting)]
        public async Task MissingTokens()
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

            await AssertFormatAsync(expected, code);
        }

        [Fact]
        [WorkItem(542199, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542199")]
        [Trait(Traits.Feature, Traits.Features.Formatting)]
        public async Task ColumnOfVeryFirstToken()
        {
            var code = @"			       W   )b";

            var expected = @"			       W   )b";

            await AssertFormatAsync(expected, code);
        }

        [Fact]
        [WorkItem(542718, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542718")]
        [Trait(Traits.Feature, Traits.Features.Formatting)]
        public async Task EmptySuppressionSpan()
        {
            var code = @"enum E
    {
        a,,
    }";

            var expected = @"enum E
{
    a,,
}";

            await AssertFormatAsync(expected, code);
        }

        [Fact]
        [WorkItem(542790, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542790")]
        [Trait(Traits.Feature, Traits.Features.Formatting)]
        public async Task LabelInSwitch()
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

            await AssertFormatAsync(expected, code);
        }

        [Fact]
        [WorkItem(543112, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543112")]
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
                default,
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
        [WorkItem(543140, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543140")]
        [Trait(Traits.Feature, Traits.Features.Formatting)]
        public async Task OmittedTypeArgument()
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

            await AssertFormatAsync(expected, code);
        }

        [Fact]
        [WorkItem(543131, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543131")]
        [Trait(Traits.Feature, Traits.Features.Formatting)]
        public async Task TryAfterLabel()
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

            await AssertFormatAsync(expected, code);
        }

        [Fact]
        [Trait(Traits.Feature, Traits.Features.Formatting)]
        public async Task QueryContinuation1()
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

            await AssertFormatAsync(expected, code);
        }

        [Fact]
        [Trait(Traits.Feature, Traits.Features.Formatting)]
        public async Task TestCSharpFormattingSpacingOptions()
        {
            var text =
@"
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
}";
            var expectedFormattedText =
@"
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
}";

            await AssertFormatAsync(expectedFormattedText, text);
        }

        [Fact]
        [Trait(Traits.Feature, Traits.Features.Formatting)]
        public async Task SpacingFixInTokenBasedForIfAndSwitchCase()
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
            await AssertFormatAsync(expectedCode, code);
        }

        [Fact]
        [Trait(Traits.Feature, Traits.Features.Formatting)]
        public async Task SpacingInDeconstruction()
        {
            var code = @"class Class5{
void bar()
{
var(x,y)=(1,2);
}
}";
            var expectedCode = @"class Class5
{
    void bar()
    {
        var (x, y) = (1, 2);
    }
}";

            await AssertFormatAsync(expectedCode, code);
        }

        [Fact]
        [Trait(Traits.Feature, Traits.Features.Formatting)]
        public async Task SpacingInNullableTuple()
        {
            var code = @"class Class5
{
    void bar()
    {
        (int, string) ? x = (1, ""hello"");
    }
}";
            var expectedCode = @"class Class5
{
    void bar()
    {
        (int, string)? x = (1, ""hello"");
    }
}";

            await AssertFormatAsync(expectedCode, code);
        }

        [Fact]
        [Trait(Traits.Feature, Traits.Features.Formatting)]
        public async Task SpacingInTupleArrayCreation()
        {
            var code = @"class C
{
    void bar()
    {
        (string a, string b)[] ab = new(string a, string b) [1];
    }
}";
            var expectedCode = @"class C
{
    void bar()
    {
        (string a, string b)[] ab = new (string a, string b)[1];
    }
}";

            await AssertFormatAsync(expectedCode, code);
        }

        [Fact]
        [Trait(Traits.Feature, Traits.Features.Formatting)]
        public async Task SpacingInTupleArrayCreation2()
        {
            var code = @"class C
{
    void bar()
    {
        (string a, string b)[] ab = new(
    }
}";
            var expectedCode = @"class C
{
    void bar()
    {
        (string a, string b)[] ab = new(
    }
}";

            await AssertFormatAsync(expectedCode, code);
        }

        [Fact]
        [Trait(Traits.Feature, Traits.Features.Formatting)]
        public async Task FormatRecursivePattern_Positional()
        {
            var code = @"class C
{
    void M() { _ = this is  ( 1 , 2 )  ; }
}";
            var expectedCode = @"class C
{
    void M() { _ = this is (1, 2); }
}";

            await AssertFormatAsync(expectedCode, code);
        }

        [Fact]
        [Trait(Traits.Feature, Traits.Features.Formatting)]
        public async Task FormatRecursivePattern_Positional_Singleline()
        {
            var code = @"class C
{
    void M() {
_ = this is  ( 1 , 2 )  ; }
}";
            var expectedCode = @"class C
{
    void M()
    {
        _ = this is (1, 2);
    }
}";

            await AssertFormatAsync(expectedCode, code);
        }

        [Fact]
        [Trait(Traits.Feature, Traits.Features.Formatting)]
        public async Task FormatRecursivePattern_Positional_Multiline()
        {
            var code = @"class C
{
    void M() {
_ = this is  ( 1 ,
2 ,
3 )  ; }
}";
            var expectedCode = @"class C
{
    void M()
    {
        _ = this is (1,
        2,
        3);
    }
}";
            await AssertFormatAsync(expectedCode, code);
        }

        [Fact]
        [Trait(Traits.Feature, Traits.Features.Formatting)]
        public async Task FormatRecursivePattern_Positional_Multiline2()
        {
            var code = @"class C
{
    void M() {
_ = this is  ( 1 ,
2 ,
3 )  ; }
}";
            var expectedCode = @"class C
{
    void M()
    {
        _ = this is (1,
        2,
        3);
    }
}";
            await AssertFormatAsync(expectedCode, code);
        }

        [Fact]
        [Trait(Traits.Feature, Traits.Features.Formatting)]
        public async Task FormatRecursivePattern_Positional_Multiline3()
        {
            var code = @"class C
{
    void M() {
_ = this is
( 1 ,
2 ,
3 )  ; }
}";
            var expectedCode = @"class C
{
    void M()
    {
        _ = this is
        (1,
        2,
        3);
    }
}";
            await AssertFormatAsync(expectedCode, code);
        }

        [Fact]
        [Trait(Traits.Feature, Traits.Features.Formatting)]
        public async Task FormatRecursivePattern_Positional_Multiline4()
        {
            var code = @"class C
{
    void M() {
_ = this is
( 1 ,
2 , 3 )  ; }
}";
            var expectedCode = @"class C
{
    void M()
    {
        _ = this is
        (1,
        2, 3);
    }
}";
            await AssertFormatAsync(expectedCode, code);
        }

        [Fact]
        [Trait(Traits.Feature, Traits.Features.Formatting)]
        public async Task FormatRecursivePattern_Properties_Singleline()
        {
            var code = @"class C
{
    void M() { _ = this is  C{  P1 :  1  } ; }
}";
            var expectedCode = @"class C
{
    void M() { _ = this is C { P1: 1 }; }
}";

            await AssertFormatAsync(expectedCode, code);
        }

        [Fact]
        [Trait(Traits.Feature, Traits.Features.Formatting)]
        public async Task FormatRecursivePattern_Properties_Multiline()
        {
            var code = @"class C
{
    void M() {
_ = this is
{
P1 :  1  ,
P2 : 2
} ;
}
}";
            var expectedCode = @"class C
{
    void M()
    {
        _ = this is
        {
            P1: 1,
            P2: 2
        };
    }
}";

            await AssertFormatAsync(expectedCode, code);
        }

        [Fact]
        [Trait(Traits.Feature, Traits.Features.Formatting)]
        public async Task FormatRecursivePattern_Properties_Multiline2()
        {
            var code = @"class C
{
    void M() {
_ = this is {
P1 :  1  ,
P2 : 2
} ;
}
}";
            var expectedCode = @"class C
{
    void M()
    {
        _ = this is
        {
            P1: 1,
            P2: 2
        };
    }
}";

            await AssertFormatAsync(expectedCode, code);
        }

        [Fact]
        [Trait(Traits.Feature, Traits.Features.Formatting)]
        public async Task FormatRecursivePattern_Properties_Multiline3()
        {
            var code = @"class C
{
    void M() {
_ = this is {
P1 :  1  ,
P2 : 2, P3: 3
} ;
}
}";
            var expectedCode = @"class C
{
    void M()
    {
        _ = this is
        {
            P1: 1,
            P2: 2, P3: 3
        };
    }
}";

            await AssertFormatAsync(expectedCode, code);
        }

        [Fact]
        [Trait(Traits.Feature, Traits.Features.Formatting)]
        [WorkItem(27268, "https://github.com/dotnet/roslyn/issues/27268")]
        public async Task FormatRecursivePattern_NoSpaceBetweenTypeAndPositionalSubpattern()
        {
            var code = @"class C
{
    void M() {
_ = this is  C( 1 , 2 ){}  ; }
}";
            var expectedCode = @"class C
{
    void M()
    {
        _ = this is C(1, 2) { };
    }
}";
            // no space separates the type and the positional pattern
            await AssertFormatAsync(expectedCode, code);
        }

        [Fact]
        [Trait(Traits.Feature, Traits.Features.Formatting)]
        [WorkItem(27268, "https://github.com/dotnet/roslyn/issues/27268")]
        public async Task FormatRecursivePattern_PreferSpaceBetweenTypeAndPositionalSubpattern()
        {
            var changingOptions = new Dictionary<OptionKey, object>
            {
                { CSharpFormattingOptions.SpaceAfterMethodCallName, true }
            };
            var code = @"class C
{
    void M() {
_ = this is  C( 1 , 2 ){}  ; }
}";
            var expectedCode = @"class C
{
    void M()
    {
        _ = this is C (1, 2) { };
    }
}";
            await AssertFormatAsync(expectedCode, code, changedOptionSet: changingOptions);
        }

        [Fact]
        [Trait(Traits.Feature, Traits.Features.Formatting)]
        [WorkItem(27268, "https://github.com/dotnet/roslyn/issues/27268")]
        public async Task FormatRecursivePattern_PreferSpaceInsidePositionalSubpatternParentheses()
        {
            var changingOptions = new Dictionary<OptionKey, object>
            {
                { CSharpFormattingOptions.SpaceWithinMethodCallParentheses, true }
            };
            var code = @"class C
{
    void M() {
_ = this is  C( 1 , 2 ){}  ;
_ = this is  C(  ){}  ; }
}";
            var expectedCode = @"class C
{
    void M()
    {
        _ = this is C( 1, 2 ) { };
        _ = this is C() { };
    }
}";
            await AssertFormatAsync(expectedCode, code, changedOptionSet: changingOptions);
        }

        [Fact]
        [Trait(Traits.Feature, Traits.Features.Formatting)]
        [WorkItem(27268, "https://github.com/dotnet/roslyn/issues/27268")]
        public async Task FormatRecursivePattern_PreferSpaceInsideEmptyPositionalSubpatternParentheses()
        {
            var changingOptions = new Dictionary<OptionKey, object>
            {
                { CSharpFormattingOptions.SpaceBetweenEmptyMethodCallParentheses, true }
            };
            var code = @"class C
{
    void M() {
_ = this is  C( 1 , 2 ){}  ;
_ = this is  C(  ){}  ; }
}";
            var expectedCode = @"class C
{
    void M()
    {
        _ = this is C(1, 2) { };
        _ = this is C( ) { };
    }
}";
            await AssertFormatAsync(expectedCode, code, changedOptionSet: changingOptions);
        }

        [Fact]
        [Trait(Traits.Feature, Traits.Features.Formatting)]
        [WorkItem(34683, "https://github.com/dotnet/roslyn/issues/34683")]
        public async Task FormatRecursivePattern_InBinaryOperation()
        {
            var changingOptions = new Dictionary<OptionKey, object>();
            changingOptions.Add(CSharpFormattingOptions.SpaceWithinMethodCallParentheses, true);
            var code = @"class C
{
    void M()
    {
        return
            typeWithAnnotations is { } && true;
    }
}";
            var expectedCode = code;
            await AssertFormatAsync(expectedCode, code, changedOptionSet: changingOptions);
        }

        [Fact]
        [Trait(Traits.Feature, Traits.Features.Formatting)]
        public async Task FormatPropertyPattern_MultilineAndEmpty()
        {
            var code = @"class C
{
    void M() {
_ = this is
            {
                };
}
}";
            var expectedCode = @"class C
{
    void M()
    {
        _ = this is
        {
        };
    }
}";
            await AssertFormatAsync(expectedCode, code);
        }

        [Fact]
        [Trait(Traits.Feature, Traits.Features.Formatting)]
        public async Task FormatSwitchExpression_IndentArms()
        {
            var code = @"class C
{
    void M() {
_ = this switch
{
{ P1: 1} => true,
(0, 1) => true,
_ => false
};

}
}";
            var expectedCode = @"class C
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
}";
            await AssertFormatAsync(expectedCode, code);
        }

        [Fact]
        [Trait(Traits.Feature, Traits.Features.Formatting)]
        public async Task FormatPropertyPattern_FollowedByInvocation()
        {
            var code = @"class C
{
    void M() {
_ = this is { }
M();
}
}";
            var expectedCode = @"class C
{
    void M()
    {
        _ = this is { }
        M();
    }
}";
            // although 'M' will be parsed into the pattern on line above, we should not wrap the pattern
            await AssertFormatAsync(expectedCode, code);
        }

        [Fact]
        [Trait(Traits.Feature, Traits.Features.Formatting)]
        public async Task FormatPositionalPattern_FollowedByInvocation()
        {
            var code = @"class C
{
    void M() {
_ = this is (1, 2) { }
M();
}
}";
            var expectedCode = @"class C
{
    void M()
    {
        _ = this is (1, 2) { }
        M();
    }
}";
            // although 'M' will be parsed into the pattern on line above, we should not wrap the pattern
            await AssertFormatAsync(expectedCode, code);
        }

        [Fact]
        [Trait(Traits.Feature, Traits.Features.Formatting)]
        public async Task FormatPositionalPattern_FollowedByScope()
        {
            var code = @"class C
{
    void M() {
_ = this is (1, 2)
{
    M();
}
}
}";
            var expectedCode = @"class C
{
    void M()
    {
        _ = this is (1, 2)
        {
        M();
    }
}
}";
            // You should not invoke Format on incomplete code and expect nice results
            await AssertFormatAsync(expectedCode, code);
        }

        [Fact]
        [Trait(Traits.Feature, Traits.Features.Formatting)]
        public async Task FormatSwitchExpression_MultilineAndNoArms()
        {
            var code = @"class C
{
    void M() {
_ = this switch
{
    };
}
}";
            var expectedCode = @"class C
{
    void M()
    {
        _ = this switch
        {
        };
    }
}";
            await AssertFormatAsync(expectedCode, code);
        }

        [Fact]
        [Trait(Traits.Feature, Traits.Features.Formatting)]
        public async Task FormatSwitchExpression_ExpressionAnchoredToArm()
        {
            var code = @"class C
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
}";
            var expectedCode = @"class C
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
}";
            await AssertFormatAsync(expectedCode, code);
        }

        [Fact]
        [Trait(Traits.Feature, Traits.Features.Formatting)]
        public async Task FormatSwitchExpression_NoSpaceBeforeColonInArm()
        {
            var code = @"class C
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
}";
            var expectedCode = @"class C
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
}";
            await AssertFormatAsync(expectedCode, code);
        }

        [Fact]
        [Trait(Traits.Feature, Traits.Features.Formatting)]
        public async Task FormatSwitchExpression_ArmCommaWantsNewline()
        {
            var code = @"class C
{
    void M() {
_ = this switch
{
{ P1: 1} => true,
(0, 1) => true, _ => false
};

}
}";
            var expectedCode = @"class C
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
}";
            await AssertFormatAsync(expectedCode, code);
        }

        [Fact]
        [Trait(Traits.Feature, Traits.Features.Formatting)]
        public async Task FormatSwitchExpression_ArmCommaPreservesLines()
        {
            var code = @"class C
{
    void M() {
_ = this switch
{
{ P1: 1} => true,

(0, 1) => true, _ => false
};

}
}";
            var expectedCode = @"class C
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
}";
            await AssertFormatAsync(expectedCode, code);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Formatting)]
        [WorkItem(33839, "https://github.com/dotnet/roslyn/issues/33839")]
        public async Task FormatSwitchExpression_ExpressionBody()
        {
            var code = @"
public class Test
{
    public object Method(int i)
        => i switch
{
1 => 'a',
2 => 'b',
_ => null,
};
}";
            var expectedCode = @"
public class Test
{
    public object Method(int i)
        => i switch
        {
            1 => 'a',
            2 => 'b',
            _ => null,
        };
}";

            await AssertFormatAsync(expectedCode, code);
        }

        [Fact]
        [Trait(Traits.Feature, Traits.Features.Formatting)]
        public async Task FormatSwitchWithPropertyPattern()
        {
            var code = @"class C
{
    void M()
    {
        switch (this)
        {
            case { P1: 1, P2: { P3: 3, P4: 4 } }:
                break;
        }
    }
}";
            var expectedCode = @"class C
{
    void M()
    {
        switch (this)
        {
            case { P1: 1, P2: { P3: 3, P4: 4 } }:
                break;
        }
    }
}";
            await AssertFormatAsync(expectedCode, code);
        }

        [Fact]
        [Trait(Traits.Feature, Traits.Features.Formatting)]
        public async Task FormatSwitchWithPropertyPattern_Singleline()
        {
            var code = @"class C
{
    void M()
    {
        switch (this)
        {
            case { P1: 1, P2: { P3: 3, P4: 4 } }: break;
        }
    }
}";
            var expectedCode = @"class C
{
    void M()
    {
        switch (this)
        {
            case { P1: 1, P2: { P3: 3, P4: 4 } }: break;
        }
    }
}";

            await AssertFormatAsync(expectedCode, code);
        }

        [Fact]
        [Trait(Traits.Feature, Traits.Features.Formatting)]
        public async Task FormatSwitchWithPropertyPattern_Singleline2()
        {
            var code = @"class C
{
    void M()
    {
        switch (this)
        {
            case { P1: 1, P2: { P3: 3, P4: 4 } }: System.Console.Write(1);
    break;
        }
    }
}";
            var expectedCode = @"class C
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
}";
            await AssertFormatAsync(expectedCode, code);
        }

        [Fact]
        [Trait(Traits.Feature, Traits.Features.Formatting)]
        public async Task SpacingInTupleExtension()
        {
            var code = @"static class Class5
{
    static void Extension(this(int, string) self) { }
}";
            var expectedCode = @"static class Class5
{
    static void Extension(this (int, string) self) { }
}";

            await AssertFormatAsync(expectedCode, code);
        }

        [Fact]
        [Trait(Traits.Feature, Traits.Features.Formatting)]
        public async Task SpacingInNestedDeconstruction()
        {
            var code = @"class Class5{
void bar()
{
( int x1 , var( x2,x3 ) )=(1,(2,3));
}
}";
            var expectedCode = @"class Class5
{
    void bar()
    {
        (int x1, var (x2, x3)) = (1, (2, 3));
    }
}";

            await AssertFormatAsync(expectedCode, code);
        }

        [Fact]
        [Trait(Traits.Feature, Traits.Features.Formatting)]
        public async Task SpacingInSuppressNullableWarningExpression()
        {
            var code =
@"class C
{
    static object F()
    {
        object? o[] = null;
        object? x = null;
        object? y = null;
        return x ! ?? (y) ! ?? o[0] !;
    }
}";
            var expectedCode =
@"class C
{
    static object F()
    {
        object? o[] = null;
        object? x = null;
        object? y = null;
        return x! ?? (y)! ?? o[0]!;
    }
}";

            await AssertFormatAsync(expectedCode, code);
        }

        [Fact]
        [WorkItem(545335, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545335")]
        [Trait(Traits.Feature, Traits.Features.Formatting)]
        public async Task PreprocessorOnSameLine()
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

            await AssertFormatAsync(expected, code);
        }

        [Fact]
        [WorkItem(545626, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545626")]
        [Trait(Traits.Feature, Traits.Features.Formatting)]
        public async Task ArraysInAttributes()
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

            await AssertFormatAsync(expected, code);
        }

        [Fact]
        [WorkItem(530580, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530580")]
        [Trait(Traits.Feature, Traits.Features.Formatting)]
        public async Task NoNewLineAfterBraceInExpression()
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

            await AssertFormatAsync(expected, code);
        }

        [Fact]
        [WorkItem(530580, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530580")]
        [Trait(Traits.Feature, Traits.Features.Formatting)]
        public async Task NoIndentForNestedUsingWithoutBraces()
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

            await AssertFormatAsync(expected, code);
        }

        [Fact]
        [WorkItem(530580, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530580")]
        [Trait(Traits.Feature, Traits.Features.Formatting)]
        public async Task NoIndentForNestedUsingWithoutBraces2()
        {
            var code = @"class C
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
";

            var expected = @"class C
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
";

            await AssertFormatAsync(expected, code);
        }

        [Fact]
        [WorkItem(530580, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530580")]
        [Trait(Traits.Feature, Traits.Features.Formatting)]
        public async Task NoIndentForNestedUsingWithoutBraces3()
        {
            var code = @"class C
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
";

            var expected = @"class C
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
";

            await AssertFormatAsync(expected, code);
        }

        [Fact]
        [WorkItem(546678, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546678")]
        [Trait(Traits.Feature, Traits.Features.Formatting)]
        public async Task UnicodeWhitespace()
        {
            var code = "\u001A";

            await AssertFormatAsync("", code);
        }

        [Fact]
        [WorkItem(17431, "DevDiv_Projects/Roslyn")]
        [Trait(Traits.Feature, Traits.Features.Formatting)]
        public async Task NoElasticRuleOnRegularFile()
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

            await AssertFormatAsync(expected, code);
        }

        [Fact]
        [WorkItem(584599, "DevDiv_Projects/Roslyn")]
        [Trait(Traits.Feature, Traits.Features.Formatting)]
        public async Task CaseSection()
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

            await AssertFormatAsync(expected, code);
        }

        [Fact]
        [WorkItem(553654, "DevDiv_Projects/Roslyn")]
        [Trait(Traits.Feature, Traits.Features.Formatting)]
        public async Task Bugfix_553654_LabelStatementIndenting()
        {
            var changingOptions = new Dictionary<OptionKey, object>
            {
                { CSharpFormattingOptions.LabelPositioning, LabelPositionOptions.LeftMost }
            };

            var code = @"class Program
{
    void F()
    {
        foreach (var x in new int[] { })
        {
            goo:
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
goo:
            int a = 1;
        }
    }
}";
            await AssertFormatAsync(expected, code, false, changingOptions);
        }

        [Fact]
        [WorkItem(707064, "DevDiv_Projects/Roslyn")]
        [Trait(Traits.Feature, Traits.Features.Formatting)]
        public async Task Bugfix_707064_SpaceAfterSecondSemiColonInFor()
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
            await AssertFormatAsync(expected, code);
        }

        [Fact]
        [WorkItem(772313, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/772313")]
        [Trait(Traits.Feature, Traits.Features.Formatting)]
        public async Task Bugfix_772313_ReturnKeywordBeforeQueryClauseDoesNotTriggerNewLineOnFormat()
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
            await AssertFormatAsync(expected, code);
        }

        [Fact]
        [WorkItem(772304, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/772304")]
        [Trait(Traits.Feature, Traits.Features.Formatting)]
        public async Task Bugfix_772313_PreserveMethodParameterIndentWhenAttributePresent()
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
            await AssertFormatAsync(expected, code);
        }

        [Fact]
        [WorkItem(776513, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/776513")]
        [Trait(Traits.Feature, Traits.Features.Formatting)]
        public async Task Bugfix_776513_CheckBraceIfNotMissingBeforeApplyingOperationForBracedBlocks()
        {
            var code = @"var alwaysTriggerList = new[]
    Dim triggerOnlyWithLettersList =";

            var expected = @"var alwaysTriggerList = new[]
    Dim triggerOnlyWithLettersList =";
            await AssertFormatAsync(expected, code);
        }

        [WorkItem(769342, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/769342")]
        [Fact, Trait(Traits.Feature, Traits.Features.Formatting)]
        public async Task ShouldFormatDocCommentWithIndentSameAsTabSizeWithUseTabTrue()
        {
            var optionSet = new Dictionary<OptionKey, object> { { new OptionKey(FormattingOptions.UseTabs, LanguageNames.CSharp), true } };

            await AssertFormatAsync(@"namespace ConsoleApplication1
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

        [WorkItem(797278, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/797278")]
        [Fact, Trait(Traits.Feature, Traits.Features.Formatting)]
        public async Task TestSpacingOptionAroundControlFlow()
        {
            const string code = @"
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
}";
            const string expected = @"
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
}";
            var optionSet = new Dictionary<OptionKey, object> { { CSharpFormattingOptions.SpaceWithinOtherParentheses, true } };
            await AssertFormatAsync(expected, code, changedOptionSet: optionSet);
        }

        [WorkItem(176345, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/176345")]
        [WorkItem(37031, "https://github.com/dotnet/roslyn/issues/37031")]
        [Fact, Trait(Traits.Feature, Traits.Features.Formatting)]
        public async Task TestSpacingOptionAfterControlFlowKeyword()
        {
            var code = @"
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
}";
            var expected = @"
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
}";
            var optionSet = new Dictionary<OptionKey, object> { { CSharpFormattingOptions.SpaceAfterControlFlowStatementKeyword, false } };
            await AssertFormatAsync(expected, code, changedOptionSet: optionSet);
        }

        [WorkItem(766212, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/766212")]
        [Fact, Trait(Traits.Feature, Traits.Features.Formatting)]
        public async Task TestOptionForSpacingAroundCommas()
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
            await AssertFormatAsync(expectedDefault, code);

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
            await AssertFormatAsync(expectedAfterCommaDisabled, code, changedOptionSet: optionSet);

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
            await AssertFormatAsync(expectedBeforeCommaEnabled, code, changedOptionSet: optionSet);
        }

        [Fact]
        [WorkItem(772308, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/772308")]
        [Trait(Traits.Feature, Traits.Features.Formatting)]
        public async Task Bugfix_772308_SeparateSuppressionForEachCaseLabelEvenIfEmpty()
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
            await AssertFormatAsync(expected, code);
        }

        [Fact]
        [WorkItem(844913, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/844913")]
        [Trait(Traits.Feature, Traits.Features.Formatting)]
        public async Task QueryExpressionInExpression()
        {
            var changingOptions = new Dictionary<OptionKey, object>
            {
                { CSharpFormattingOptions.NewLinesForBracesInMethods, false }
            };

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
            await AssertFormatAsync(expected, code);
        }

        [Fact]
        [WorkItem(843479, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/843479")]
        [Trait(Traits.Feature, Traits.Features.Formatting)]
        public async Task EmbeddedStatementElse()
        {
            var changingOptions = new Dictionary<OptionKey, object>
            {
                { CSharpFormattingOptions.NewLineForElse, false }
            };

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
            await AssertFormatAsync(expected, code, false, changingOptions);
        }

        [Fact]
        [WorkItem(772311, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/772311")]
        [Trait(Traits.Feature, Traits.Features.Formatting)]
        public async Task LineCommentAtTheEndOfLine()
        {
            var code = @"
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
";

            var expected = @"
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
";
            await AssertFormatAsync(expected, code);
        }

        [Fact]
        [WorkItem(38224, "https://github.com/dotnet/roslyn/issues/38224")]
        [Trait(Traits.Feature, Traits.Features.Formatting)]
        public async Task BlockCommentAtTheEndOfLine1()
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
            await AssertFormatAsync(expected, code);
        }

        [Fact]
        [WorkItem(38224, "https://github.com/dotnet/roslyn/issues/38224")]
        [Trait(Traits.Feature, Traits.Features.Formatting)]
        public async Task BlockCommentAtTheEndOfLine2()
        {
            var code = @"
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
";

            var expected = @"
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
";
            await AssertFormatAsync(expected, code);
        }

        [Fact]
        [WorkItem(38224, "https://github.com/dotnet/roslyn/issues/38224")]
        [Trait(Traits.Feature, Traits.Features.Formatting)]
        public async Task BlockCommentAtBeginningOfLine()
        {
            var code = @"
using System;

class Program
{
    static void Main(
        int x, // Some comment
        /*A*/ int y)
    {
    }
}
";
            await AssertFormatAsync(code, code);
        }

        [Fact]
        [WorkItem(772311, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/772311")]
        [Trait(Traits.Feature, Traits.Features.Formatting)]
        public async Task TestTab()
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

            await AssertFormatAsync(expected, code, changedOptionSet: optionSet);
            await AssertFormatAsync(expected, expected, changedOptionSet: optionSet);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Formatting)]
        public async Task LeaveBlockSingleLine_False()
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
            await AssertFormatAsync(expected, code, changedOptionSet: options);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Formatting)]
        public async Task LeaveBlockSingleLine_False2()
        {
            var code = @"
class C { void goo() { } }";

            var expected = @"
class C
{
    void goo()
    {
    }
}";

            var options = new Dictionary<OptionKey, object>() { { CSharpFormattingOptions.WrappingPreserveSingleLine, false } };
            await AssertFormatAsync(expected, code, changedOptionSet: options);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Formatting)]
        public async Task LeaveStatementMethodDeclarationSameLine_False()
        {
            var code = @"
class Program
{
    void goo()
    {
        int x = 0; int y = 0;
    }
}";

            var expected = @"
class Program
{
    void goo()
    {
        int x = 0;
        int y = 0;
    }
}";

            var options = new Dictionary<OptionKey, object>() { { CSharpFormattingOptions.WrappingKeepStatementsOnSingleLine, false } };
            await AssertFormatAsync(expected, code, changedOptionSet: options);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Formatting)]
        public async Task SpaceWithinEmptyBracketPrecedencesSpaceBeforeOrAfterComma_0000()
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
            await AssertFormatAsync(expected, code, changedOptionSet: options);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Formatting)]
        public async Task SpaceWithinEmptyBracketPrecedencesSpaceBeforeOrAfterComma_0001()
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
            await AssertFormatAsync(expected, code, changedOptionSet: options);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Formatting)]
        public async Task SpaceWithinEmptyBracketPrecedencesSpaceBeforeOrAfterComma_0010()
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
            await AssertFormatAsync(expected, code, changedOptionSet: options);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Formatting)]
        public async Task SpaceWithinEmptyBracketPrecedencesSpaceBeforeOrAfterComma_0011()
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
            await AssertFormatAsync(expected, code, changedOptionSet: options);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Formatting)]
        public async Task SpaceWithinEmptyBracketPrecedencesSpaceBeforeOrAfterComma_0100()
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
            await AssertFormatAsync(expected, code, changedOptionSet: options);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Formatting)]
        public async Task SpaceWithinEmptyBracketPrecedencesSpaceBeforeOrAfterComma_0101()
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
            await AssertFormatAsync(expected, code, changedOptionSet: options);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Formatting)]
        public async Task SpaceWithinEmptyBracketPrecedencesSpaceBeforeOrAfterComma_0110()
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
            await AssertFormatAsync(expected, code, changedOptionSet: options);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Formatting)]
        public async Task SpaceWithinEmptyBracketPrecedencesSpaceBeforeOrAfterComma_0111()
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
            await AssertFormatAsync(expected, code, changedOptionSet: options);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Formatting)]
        public async Task SpaceWithinEmptyBracketPrecedencesSpaceBeforeOrAfterComma_1000()
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
            await AssertFormatAsync(expected, code, changedOptionSet: options);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Formatting)]
        public async Task SpaceWithinEmptyBracketPrecedencesSpaceBeforeOrAfterComma_1001()
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
            await AssertFormatAsync(expected, code, changedOptionSet: options);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Formatting)]
        public async Task SpaceWithinEmptyBracketPrecedencesSpaceBeforeOrAfterComma_1010()
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
            await AssertFormatAsync(expected, code, changedOptionSet: options);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Formatting)]
        public async Task SpaceWithinEmptyBracketPrecedencesSpaceBeforeOrAfterComma_1011()
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
            await AssertFormatAsync(expected, code, changedOptionSet: options);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Formatting)]
        public async Task SpaceWithinEmptyBracketPrecedencesSpaceBeforeOrAfterComma_1100()
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
            await AssertFormatAsync(expected, code, changedOptionSet: options);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Formatting)]
        public async Task SpaceWithinEmptyBracketPrecedencesSpaceBeforeOrAfterComma_1101()
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
            await AssertFormatAsync(expected, code, changedOptionSet: options);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Formatting)]
        public async Task SpaceWithinEmptyBracketPrecedencesSpaceBeforeOrAfterComma_1110()
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
            await AssertFormatAsync(expected, code, changedOptionSet: options);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Formatting)]
        public async Task SpaceWithinEmptyBracketPrecedencesSpaceBeforeOrAfterComma_1111()
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
            await AssertFormatAsync(expected, code, changedOptionSet: options);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Formatting)]
        public async Task ArrayDeclarationShouldFollowEmptySquareBrackets()
        {
            const string code = @"
class Program
{
   var t = new Goo(new[ ] { ""a"", ""b"" });
}";

            const string expected = @"
class Program
{
    var t = new Goo(new[] { ""a"", ""b"" });
}";

            var options = new Dictionary<OptionKey, object>
            {
                { CSharpFormattingOptions.SpaceWithinSquareBrackets, true },
                { CSharpFormattingOptions.SpaceBetweenEmptySquareBrackets, false }
            };
            await AssertFormatAsync(expected, code, changedOptionSet: options);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Formatting)]
        public async Task SquareBracesBefore_True()
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
            await AssertFormatAsync(expected, code, changedOptionSet: options);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Formatting)]
        public async Task SquareBracesAndValue_True()
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
            await AssertFormatAsync(expected, code, changedOptionSet: options);
        }

        [WorkItem(917351, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/917351")]
        [Fact, Trait(Traits.Feature, Traits.Features.Formatting)]
        public async Task TestLockStatement()
        {
            var code = @"
class Program
{
    public void Method()
    {
        lock (expression)
            {
        // goo
}
    }
}";

            var expected = @"
class Program
{
    public void Method()
    {
        lock (expression) {
            // goo
        }
    }
}";

            var options = new Dictionary<OptionKey, object>()
            {
                { CSharpFormattingOptions.NewLinesForBracesInControlBlocks, false }
            };

            await AssertFormatAsync(expected, code, changedOptionSet: options);
        }

        [WorkItem(962416, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/962416")]
        [Fact, Trait(Traits.Feature, Traits.Features.Formatting)]
        public async Task TestCheckedAndUncheckedStatement()
        {
            var code = @"
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
}";

            var expected = @"
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
}";

            var options = new Dictionary<OptionKey, object>()
            {
                { CSharpFormattingOptions.NewLinesForBracesInControlBlocks, false }
            };

            await AssertFormatAsync(expected, code, changedOptionSet: options);
        }

        [WorkItem(953535, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/953535")]
        [Fact, Trait(Traits.Feature, Traits.Features.Formatting)]
        public async Task ConditionalMemberAccess()
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
            await AssertFormatAsync(expected, code, parseOptions: parseOptions);
        }

        [WorkItem(924172, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/924172")]
        [Fact, Trait(Traits.Feature, Traits.Features.Formatting)]
        public async Task IgnoreSpacesInDeclarationStatementEnabled()
        {
            var changingOptions = new Dictionary<OptionKey, object>
            {
                { CSharpFormattingOptions.SpacesIgnoreAroundVariableDeclaration, true }
            };
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
            await AssertFormatAsync(expected, code, changedOptionSet: changingOptions);
        }

        [WorkItem(899492, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/899492")]
        [Fact, Trait(Traits.Feature, Traits.Features.Formatting)]
        public async Task CommentIsLeadingTriviaOfStatementNotLabel()
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
            await AssertFormatAsync(expected, code);
        }

        [WorkItem(991547, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/991547")]
        [Fact, Trait(Traits.Feature, Traits.Features.Formatting)]
        public async Task DontWrappingTryCatchFinallyIfOnSingleLine()
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
            await AssertFormatAsync(expected, code);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Formatting)]
        public async Task InterpolatedStrings1()
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

            await AssertFormatAsync(expected, code);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Formatting)]
        public async Task InterpolatedStrings2()
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

            await AssertFormatAsync(expected, code);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Formatting)]
        public async Task InterpolatedStrings3()
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

            await AssertFormatAsync(expected, code);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Formatting)]
        public async Task InterpolatedStrings4()
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

            await AssertFormatAsync(expected, code);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Formatting)]
        public async Task InterpolatedStrings5()
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

            await AssertFormatAsync(expected, code);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Formatting)]
        public async Task InterpolatedStrings6()
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

            await AssertFormatAsync(expected, code);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Formatting)]
        public async Task InterpolatedStrings7()
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

            await AssertFormatAsync(expected, code);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Formatting)]
        public async Task InterpolatedStrings8()
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

            await AssertFormatAsync(expected, code);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Formatting)]
        public async Task InterpolatedStrings9()
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

            await AssertFormatAsync(expected, code);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Formatting)]
        public async Task InterpolatedStrings10()
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

            await AssertFormatAsync(expected, code);
        }

        [WorkItem(1041787, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1041787")]
        [WorkItem(1151, "https://github.com/dotnet/roslyn/issues/1151")]
        [Fact, Trait(Traits.Feature, Traits.Features.Formatting)]
        public async Task ReconstructWhitespaceStringUsingTabs_SingleLineComment()
        {
            var optionSet = new Dictionary<OptionKey, object> { { new OptionKey(FormattingOptions.UseTabs, LanguageNames.CSharp), true } };
            await AssertFormatAsync(@"using System;

class Program
{
	static void Main(string[] args)
	{
		Console.WriteLine("""");        // GooBar
	}
}", @"using System;

class Program
{
    static void Main(string[] args)
    {
        Console.WriteLine("""");        // GooBar
    }
}", false, optionSet);
        }

        [WorkItem(961559, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/961559")]
        [WorkItem(1041787, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1041787")]
        [WorkItem(1151, "https://github.com/dotnet/roslyn/issues/1151")]
        [Fact, Trait(Traits.Feature, Traits.Features.Formatting)]
        public async Task ReconstructWhitespaceStringUsingTabs_MultiLineComment()
        {
            var optionSet = new Dictionary<OptionKey, object> { { new OptionKey(FormattingOptions.UseTabs, LanguageNames.CSharp), true } };
            await AssertFormatAsync(@"using System;

class Program
{
	static void Main(string[] args)
	{
		Console.WriteLine("""");        /* GooBar */
	}
}", @"using System;

class Program
{
    static void Main(string[] args)
    {
        Console.WriteLine("""");        /* GooBar */
    }
}", false, optionSet);
        }

        [WorkItem(1100920, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1100920")]
        [Fact, Trait(Traits.Feature, Traits.Features.Formatting)]
        public async Task NoLineOperationAroundInterpolationSyntax()
        {
            await AssertFormatAsync(@"class Program
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

        [WorkItem(62, "https://github.com/dotnet/roslyn/issues/62")]
        [Fact, Trait(Traits.Feature, Traits.Features.Formatting)]
        public async Task SpaceAfterWhenInExceptionFilter()
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
            await AssertFormatAsync(expected, code);
        }

        [WorkItem(285, "https://github.com/dotnet/roslyn/issues/285")]
        [WorkItem(1089196, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1089196")]
        [Fact, Trait(Traits.Feature, Traits.Features.Formatting)]
        public async Task FormatHashInBadDirectiveToZeroColumnAnywhereInsideIfDef()
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
            await AssertFormatAsync(expected, code);
        }

        [WorkItem(285, "https://github.com/dotnet/roslyn/issues/285")]
        [WorkItem(1089196, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1089196")]
        [Fact, Trait(Traits.Feature, Traits.Features.Formatting)]
        public async Task FormatHashElseToZeroColumnAnywhereInsideIfDef()
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
            await AssertFormatAsync(expected, code);
        }

        [WorkItem(285, "https://github.com/dotnet/roslyn/issues/285")]
        [WorkItem(1089196, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1089196")]
        [Fact, Trait(Traits.Feature, Traits.Features.Formatting)]
        public async Task FormatHashsToZeroColumnAnywhereInsideIfDef()
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
            await AssertFormatAsync(expected, code);
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
        public async Task SpacingRulesAroundMethodCallAndParenthesisAppliedInAttributeNonDefault()
        {
            var changingOptions = new Dictionary<OptionKey, object>
            {
                { CSharpFormattingOptions.SpaceAfterMethodCallName, true },
                { CSharpFormattingOptions.SpaceBetweenEmptyMethodCallParentheses, true },
                { CSharpFormattingOptions.SpaceWithinMethodCallParentheses, true }
            };
            await AssertFormatAsync(@"[Obsolete ( ""Test"" ), Obsolete ( )]
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
        public async Task SpacingRulesAroundMethodCallAndParenthesisAppliedInAttribute()
        {
            var code = @"[Obsolete(""Test""), Obsolete()]
class Program
{
    static void Main(string[] args)
    {
    }
}";
            await AssertFormatAsync(code, code);
        }

        [Fact]
        public async Task SpacingInMethodCallArguments_True()
        {
            const string code = @"
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
}";
            const string expected = @"
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
}";
            var optionSet = new Dictionary<OptionKey, object>
            {
                { CSharpFormattingOptions.SpaceWithinMethodCallParentheses, true },
                { CSharpFormattingOptions.SpaceAfterMethodCallName, true },
                { CSharpFormattingOptions.SpaceBetweenEmptyMethodCallParentheses, true },
            };
            await AssertFormatAsync(expected, code, changedOptionSet: optionSet);
        }

        [WorkItem(1298, "https://github.com/dotnet/roslyn/issues/1298")]
        [Fact, Trait(Traits.Feature, Traits.Features.Formatting)]
        public async Task DontforceAccessorsToNewLineWithPropertyInitializers()
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
            await AssertFormatAsync(expected, code);
        }

        [WorkItem(1339, "https://github.com/dotnet/roslyn/issues/1339")]
        [Fact, Trait(Traits.Feature, Traits.Features.Formatting)]
        public async Task DontFormatAutoPropertyInitializerIfNotDifferentLine()
        {
            var code = @"class Program
{
    public int d { get; }
            = 3;
    static void Main(string[] args)
    {
    }
}";
            await AssertFormatAsync(code, code);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Formatting)]
        public async Task SpacingForForStatementInfiniteLoop()
        {
            var code = @"
class Program
{
    void Main()
    {
        for ( ;;)
        {
        }
    }
}";
            var expected = @"
class Program
{
    void Main()
    {
        for (; ; )
        {
        }
    }
}";
            await AssertFormatAsync(expected, code);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Formatting)]
        public async Task SpacingForForStatementInfiniteLoopWithNoSpaces()
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
            var optionSet = new Dictionary<OptionKey, object>
            {
                { CSharpFormattingOptions.SpaceAfterSemicolonsInForStatement, false },
            };

            await AssertFormatAsync(expected, code, changedOptionSet: optionSet);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Formatting)]
        public async Task SpacingForForStatementInfiniteLoopWithSpacesBefore()
        {
            var code = @"
class Program
{
    void Main()
    {
        for (;; )
        {
        }
    }
}";
            var expected = @"
class Program
{
    void Main()
    {
        for ( ; ;)
        {
        }
    }
}";
            var optionSet = new Dictionary<OptionKey, object>
            {
                { CSharpFormattingOptions.SpaceBeforeSemicolonsInForStatement, true },
                { CSharpFormattingOptions.SpaceAfterSemicolonsInForStatement, false },
            };

            await AssertFormatAsync(expected, code, changedOptionSet: optionSet);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Formatting)]
        public async Task SpacingForForStatementInfiniteLoopWithSpacesBeforeAndAfter()
        {
            var code = @"
class Program
{
    void Main()
    {
        for (;;)
        {
        }
    }
}";
            var expected = @"
class Program
{
    void Main()
    {
        for ( ; ; )
        {
        }
    }
}";
            var optionSet = new Dictionary<OptionKey, object>
            {
                { CSharpFormattingOptions.SpaceBeforeSemicolonsInForStatement, true },
            };

            await AssertFormatAsync(expected, code, changedOptionSet: optionSet);
        }

        [WorkItem(4421, "https://github.com/dotnet/roslyn/issues/4421")]
        [WorkItem(4240, "https://github.com/dotnet/roslyn/issues/4240")]
        [Fact, Trait(Traits.Feature, Traits.Features.Formatting)]
        public async Task VerifySpacingAfterMethodDeclarationName_Default()
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
            await AssertFormatAsync(expected, code);
        }

        [WorkItem(4240, "https://github.com/dotnet/roslyn/issues/4240")]
        [WorkItem(4421, "https://github.com/dotnet/roslyn/issues/4421")]
        [Fact, Trait(Traits.Feature, Traits.Features.Formatting)]
        public async Task VerifySpacingAfterMethodDeclarationName_NonDefault()
        {
            var changingOptions = new Dictionary<OptionKey, object>
            {
                { CSharpFormattingOptions.SpacingAfterMethodDeclarationName, true }
            };
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
            await AssertFormatAsync(expected, code, changedOptionSet: changingOptions);
        }

        [WorkItem(939, "https://github.com/dotnet/roslyn/issues/939")]
        [Fact, Trait(Traits.Feature, Traits.Features.Formatting)]
        public async Task DontFormatInsideArrayInitializers()
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
            await AssertFormatAsync(code, code);
        }

        [WorkItem(1184285, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1184285")]
        [WorkItem(4280, "https://github.com/dotnet/roslyn/issues/4280")]
        [Fact, Trait(Traits.Feature, Traits.Features.Formatting)]
        public async Task FormatDictionaryInitializers()
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
            await AssertFormatAsync(expected, code);
        }

        [WorkItem(3256, "https://github.com/dotnet/roslyn/issues/3256")]
        [Fact, Trait(Traits.Feature, Traits.Features.Formatting)]
        public async Task SwitchSectionHonorsNewLineForBracesinControlBlockOption_Default()
        {
            var code = @"class Program
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
}";
            var expected = @"class Program
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
}";
            await AssertFormatAsync(expected, code);
        }

        [WorkItem(3256, "https://github.com/dotnet/roslyn/issues/3256")]
        [Fact, Trait(Traits.Feature, Traits.Features.Formatting)]
        public async Task SwitchSectionHonorsNewLineForBracesinControlBlockOption_NonDefault()
        {
            var changingOptions = new Dictionary<OptionKey, object>
            {
                { CSharpFormattingOptions.NewLinesForBracesInControlBlocks, false }
            };
            var code = @"class Program
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
}";

            var expected = @"class Program
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
}";
            await AssertFormatAsync(expected, code, changedOptionSet: changingOptions);
        }

        [WorkItem(4014, "https://github.com/dotnet/roslyn/issues/4014")]
        [Fact, Trait(Traits.Feature, Traits.Features.Formatting)]
        public async Task FormattingCodeWithMissingTokensShouldRespectFormatTabsOption1()
        {
            var optionSet = new Dictionary<OptionKey, object> { { new OptionKey(FormattingOptions.UseTabs, LanguageNames.CSharp), true } };

            await AssertFormatAsync(@"class Program
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
        public async Task FormattingCodeWithMissingTokensShouldRespectFormatTabsOption2()
        {
            var optionSet = new Dictionary<OptionKey, object> { { new OptionKey(FormattingOptions.UseTabs, LanguageNames.CSharp), true } };

            await AssertFormatAsync(@"struct Goo
{
	private readonly string bar;

	public Goo(readonly string bar)
	{
	}
}", @"struct Goo
{
	private readonly string bar;

	public Goo(readonly string bar)
	{
	}
}", changedOptionSet: optionSet);
        }

        [WorkItem(4014, "https://github.com/dotnet/roslyn/issues/4014")]
        [Fact, Trait(Traits.Feature, Traits.Features.Formatting)]
        public async Task FormattingCodeWithBrokenLocalDeclarationShouldRespectFormatTabsOption()
        {
            var optionSet = new Dictionary<OptionKey, object> { { new OptionKey(FormattingOptions.UseTabs, LanguageNames.CSharp), true } };

            await AssertFormatAsync(@"class AClass
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
}", @"class AClass
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
}", changedOptionSet: optionSet);
        }

        [WorkItem(4014, "https://github.com/dotnet/roslyn/issues/4014")]
        [Fact, Trait(Traits.Feature, Traits.Features.Formatting)]
        public async Task FormattingCodeWithBrokenInterpolatedStringShouldRespectFormatTabsOption()
        {
            var optionSet = new Dictionary<OptionKey, object> { { new OptionKey(FormattingOptions.UseTabs, LanguageNames.CSharp), true } };

            await AssertFormatAsync(@"class AClass
{
	void Main()
	{
		Test($""\""_{\"""");
		Console.WriteLine(args);
	}
}", @"class AClass
{
	void Main()
	{
		Test($""\""_{\"""");
		Console.WriteLine(args);
	}
}", changedOptionSet: optionSet);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Formatting)]
        [WorkItem(84, "https://github.com/dotnet/roslyn/issues/84")]
        [WorkItem(849870, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/849870")]
        public async Task NewLinesForBracesInPropertiesTest()
        {
            var changingOptions = new Dictionary<OptionKey, object>
            {
                { CSharpFormattingOptions.NewLinesForBracesInProperties, false }
            };
            await AssertFormatAsync(@"class Class2
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
}", @"class Class2
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
}", false, changingOptions);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Formatting)]
        [WorkItem(849870, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/849870")]
        [WorkItem(84, "https://github.com/dotnet/roslyn/issues/84")]
        public async Task NewLinesForBracesInAccessorsTest()
        {
            var changingOptions = new Dictionary<OptionKey, object>
            {
                { CSharpFormattingOptions.NewLinesForBracesInAccessors, false }
            };
            await AssertFormatAsync(@"class Class2
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
}", @"class Class2
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
}", false, changingOptions);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Formatting)]
        [WorkItem(849870, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/849870")]
        [WorkItem(84, "https://github.com/dotnet/roslyn/issues/84")]
        public async Task NewLinesForBracesInPropertiesAndAccessorsTest()
        {
            var changingOptions = new Dictionary<OptionKey, object>
            {
                { CSharpFormattingOptions.NewLinesForBracesInProperties, false },
                { CSharpFormattingOptions.NewLinesForBracesInAccessors, false }
            };
            await AssertFormatAsync(@"class Class2
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
}", @"class Class2
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
}", false, changingOptions);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Formatting)]
        [WorkItem(111079, "devdiv.visualstudio.com")]
        public async Task TestThrowInIfOnSingleLine()
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

            await AssertFormatAsync(code, code);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Formatting)]
        [WorkItem(1711675, "https://connect.microsoft.com/VisualStudio/feedback/details/1711675/autoformatting-issues")]
        public async Task SingleLinePropertiesPreservedWithLeaveStatementsAndMembersOnSingleLineFalse()
        {
            var changedOptionSet = new Dictionary<OptionKey, object>
            {
                { CSharpFormattingOptions.WrappingPreserveSingleLine, true },
                { CSharpFormattingOptions.WrappingKeepStatementsOnSingleLine, false},
            };

            await AssertFormatAsync(@"
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
        public async Task KeepAccessorWithAttributeOnSingleLine()
        {
            await AssertFormatAsync(@"
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
        [WorkItem(6905, "https://github.com/dotnet/roslyn/issues/6905")]
        public async Task KeepConstructorBodyInSameLineAsBaseConstructorInitializer()
        {
            var code = @"
class C
{
    public C(int s)
        : base() { }
    public C()
    {
    }
}";
            await AssertFormatAsync(code, code);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Formatting)]
        [WorkItem(6905, "https://github.com/dotnet/roslyn/issues/6905")]
        public async Task KeepConstructorBodyInSameLineAsThisConstructorInitializer()
        {
            var code = @"
class C
{
    public C(int s)
        : this() { }
    public C()
    {
    }
}";
            await AssertFormatAsync(code, code);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Formatting)]
        [WorkItem(6905, "https://github.com/dotnet/roslyn/issues/6905")]
        public async Task KeepConstructorBodyInSameLineAsThisConstructorInitializerAdjustSpace()
        {
            await AssertFormatAsync(@"
class C
{
    public C(int s)
        : this() { }
    public C()
    {
    }
}", @"
class C
{
    public C(int s)
        : this()      { }
    public C()
    {
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Formatting)]
        [WorkItem(4720, "https://github.com/dotnet/roslyn/issues/4720")]
        public async Task OneSpaceBetweenAccessorsAndAttributes()
        {
            await AssertFormatAsync(@"
class Program
{
    public int SomeProperty { [SomeAttribute] get; [SomeAttribute] private set; }
}", @"
class Program
{
    public int SomeProperty {    [SomeAttribute] get;    [SomeAttribute] private set; }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Formatting)]
        [WorkItem(7900, "https://github.com/dotnet/roslyn/issues/7900")]
        public async Task FormatEmbeddedStatementInsideLockStatement()
        {
            await AssertFormatAsync(@"
class C
{
    private object _l = new object();
    public void M()
    {
        lock (_l) Console.WriteLine(""d"");
    }
}", @"
class C
{
    private object _l = new object();
    public void M()
    {
        lock (_l)     Console.WriteLine(""d"");
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Formatting)]
        [WorkItem(7900, "https://github.com/dotnet/roslyn/issues/7900")]
        public async Task FormatEmbeddedStatementInsideLockStatementDifferentLine()
        {
            await AssertFormatAsync(@"
class C
{
    private object _l = new object();
    public void M()
    {
        lock (_l)
            Console.WriteLine(""d"");
    }
}", @"
class C
{
    private object _l = new object();
    public void M()
    {
        lock (_l)
    Console.WriteLine(""d"");
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Formatting)]
        public async Task PropertyDeclarationSimple()
        {
            var expected = @"if (o is Point p)";
            await AssertFormatBodyAsync(expected, expected);
            await AssertFormatBodyAsync(expected, @"if (o is Point   p)");
            await AssertFormatBodyAsync(expected, @"if (o is Point p  )");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Formatting)]
        public async Task PropertyDeclarationTypeOnNewLine()
        {
            var expected = @"
var y = o is
Point p;";
            await AssertFormatBodyAsync(expected, expected);
            await AssertFormatBodyAsync(expected, @"
var y = o is
Point p;    ");

            await AssertFormatBodyAsync(expected, @"
var y = o   is
Point p    ;");

            await AssertFormatBodyAsync(expected, @"
var y = o   is
Point     p    ;");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Formatting)]
        public async Task CasePatternDeclarationSimple()
        {
            var expected = @"
switch (o)
{
    case Point p:
}";

            await AssertFormatBodyAsync(expected, expected);
            await AssertFormatBodyAsync(expected, @"
switch (o)
{
    case Point p   :
}");

            await AssertFormatBodyAsync(expected, @"
switch (o)
{
    case Point    p   :
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Formatting)]
        [WorkItem(23703, "https://github.com/dotnet/roslyn/issues/23703")]
        public async Task FormatNullableArray()
        {
            var code = @"
class C
{
    object[]? F = null;
}";
            await AssertFormatAsync(code, code);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Formatting)]
        [WorkItem(23703, "https://github.com/dotnet/roslyn/issues/23703")]
        public async Task FormatConditionalWithArrayAccess()
        {
            var code = @"
class C
{
    void M()
    {
        _ = array[1] ? 2 : 3;
    }
}";
            await AssertFormatAsync(code, code);
        }

        private Task AssertFormatBodyAsync(string expected, string input)
        {
            static string transform(string s)
            {
                var lines = s.Split(new[] { Environment.NewLine }, StringSplitOptions.None);
                for (var i = 0; i < lines.Length; i++)
                {
                    if (!string.IsNullOrEmpty(lines[i]))
                    {
                        lines[i] = new string(' ', count: 8) + lines[i];
                    }
                }
                return string.Join(Environment.NewLine, lines);
            }

            var pattern = @"
class C
{{
    void M()
    {{
{0}
    }}
}}";

            expected = string.Format(pattern, transform(expected));
            input = string.Format(pattern, transform(input));
            return AssertFormatAsync(expected, input);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Formatting)]
        [WorkItem(6628, "https://github.com/dotnet/roslyn/issues/6628")]
        public async Task FormatElseBlockBracesOnDifferentLineToNewLines()
        {
            await AssertFormatAsync(@"
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
}", @"
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
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Formatting)]
        [WorkItem(6628, "https://github.com/dotnet/roslyn/issues/6628")]
        public async Task FormatOnElseBlockBracesOnSameLineRemainsInSameLine_1()
        {
            var code = @"
class C
{
    public void M()
    {
        if (true)
        {
        }
        else { }
    }
}";
            await AssertFormatAsync(code, code);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Formatting)]
        [WorkItem(11572, "https://github.com/dotnet/roslyn/issues/11572")]
        public async Task FormatAttributeOnSameLineAsField()
        {
            await AssertFormatAsync(
@"
class C
{
    [Attr] int i;
}",
@"
class C {
    [Attr]   int   i;
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Formatting)]
        [WorkItem(21789, "https://github.com/dotnet/roslyn/issues/21789")]
        public async Task FormatMultipleAttributeOnSameLineAsField1()
        {
            await AssertFormatAsync(
@"
class C
{
    [Attr1]
    [Attr2]
    [Attr3] [Attr4] int i;
}",
@"
class C {
    [Attr1]
    [Attr2]
    [Attr3][Attr4]   int   i;
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Formatting)]
        [WorkItem(21789, "https://github.com/dotnet/roslyn/issues/21789")]
        public async Task FormatMultipleAttributesOnSameLineAsField2()
        {
            await AssertFormatAsync(
@"
class C
{
    [Attr1]
    [Attr2]
    [Attr3] [Attr4] int i;
}",
@"
class C {
    [Attr1][Attr2]
    [Attr3][Attr4]   int   i;
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Formatting)]
        [WorkItem(21789, "https://github.com/dotnet/roslyn/issues/21789")]
        public async Task FormatMultipleAttributeOnSameLineAndFieldOnNewLine()
        {
            await AssertFormatAsync(
@"
class C
{
    [Attr1]
    [Attr2]
    int i;
}",
@"
class C {
    [Attr1][Attr2]
    int   i;
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Formatting)]
        [WorkItem(6628, "https://github.com/dotnet/roslyn/issues/6628")]
        public async Task FormatOnElseBlockBracesOnSameLineRemainsInSameLine_2()
        {
            var code = @"
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
}";
            await AssertFormatAsync(code, code);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Formatting)]
        [WorkItem(25098, "https://github.com/dotnet/roslyn/issues/25098")]
        public void FormatSingleStructDeclaration()
        {
            Formatter.Format(SyntaxFactory.StructDeclaration("S"), DefaultWorkspace);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Formatting)]
        public async Task FormatIndexExpression()
        {
            await AssertFormatAsync(@"
class C
{
    void M()
    {
        object x = ^1;
        object y = ^1
    }
}", @"
class C
{
    void M()
    {
        object x = ^1;
        object y = ^1
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Formatting)]
        public async Task FormatRangeExpression_NoOperands()
        {
            await AssertFormatAsync(@"
class C
{
    void M()
    {
        object x = ..;
        object y = ..
    }
}", @"
class C
{
    void M()
    {
        object x = ..;
        object y = ..
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Formatting)]
        public async Task FormatRangeExpression_RightOperand()
        {
            await AssertFormatAsync(@"
class C
{
    void M()
    {
        object x = ..1;
        object y = ..1
    }
}", @"
class C
{
    void M()
    {
        object x = ..1;
        object y = ..1
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Formatting)]
        public async Task FormatRangeExpression_LeftOperand()
        {
            await AssertFormatAsync(@"
class C
{
    void M()
    {
        object x = 1..;
        object y = 1..
    }
}", @"
class C
{
    void M()
    {
        object x = 1..;
        object y = 1..
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Formatting)]
        public async Task FormatRangeExpression_BothOperands()
        {
            await AssertFormatAsync(@"
class C
{
    void M()
    {
        object x = 1..2;
        object y = 1..2
    }
}", @"
class C
{
    void M()
    {
        object x = 1..2;
        object y = 1..2
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Formatting)]
        [WorkItem(32113, "https://github.com/dotnet/roslyn/issues/32113")]
        public async Task FormatCommaAfterCloseBrace_CommaRemainIntheSameLine()
        {
            await AssertFormatAsync(
                @"
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
}",
                @"
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
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Formatting)]
        [WorkItem(32113, "https://github.com/dotnet/roslyn/issues/32113")]
        public async Task FormatCommaAfterCloseBrace_SpaceSurroundWillBeRemoved()
        {
            await AssertFormatAsync(
                @"
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
}",
                @"
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
}");
        }

        [Theory, Trait(Traits.Feature, Traits.Features.Formatting)]
        [WorkItem(31571, "https://github.com/dotnet/roslyn/issues/31571")]
        [WorkItem(33910, "https://github.com/dotnet/roslyn/issues/33910")]
        [CombinatorialData]
        public async Task ConversionOperator_CorrectlySpaceArgumentList(
            [CombinatorialValues("implicit", "explicit")] string operatorType,
            [CombinatorialValues("string", "string[]", "System.Action<int>", "int?", "int*", "(int, int)")] string targetType,
            bool spacingAfterMethodDeclarationName)
        {
            var expectedSpacing = spacingAfterMethodDeclarationName ? " " : "";
            var initialSpacing = spacingAfterMethodDeclarationName ? "" : " ";
            var changedOptionSet = new Dictionary<OptionKey, object> { { SpacingAfterMethodDeclarationName, spacingAfterMethodDeclarationName } };
            await AssertFormatAsync(
                $@"
public unsafe class Test
{{
    public static {operatorType} operator {targetType}{expectedSpacing}() => throw null;
}}",
                $@"
public unsafe class Test
{{
    public static {operatorType} operator {targetType}{initialSpacing}() => throw null;
}}",
                changedOptionSet: changedOptionSet);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Formatting)]
        [WorkItem(31868, "https://github.com/dotnet/roslyn/issues/31868")]
        public async Task SpaceAroundDeclaration()
        {
            var changingOptions = new Dictionary<OptionKey, object>
            {
                { CSharpFormattingOptions.SpacesIgnoreAroundVariableDeclaration, true }
            };
            await AssertFormatAsync(
                @"
class Program
{
    public void FixMyType()
    {
        var    myint    =    0;
    }
}",
                @"
class Program
{
    public void FixMyType()
    {
        var    myint    =    0;
    }
}", changedOptionSet: changingOptions);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Formatting)]
        [WorkItem(31868, "https://github.com/dotnet/roslyn/issues/31868")]
        public async Task SpaceAroundDeclarationAndPreserveSingleLine()
        {
            var changingOptions = new Dictionary<OptionKey, object>
            {
                { CSharpFormattingOptions.SpacesIgnoreAroundVariableDeclaration, true },
                { CSharpFormattingOptions.WrappingKeepStatementsOnSingleLine, false }
            };
            await AssertFormatAsync(
                @"
class Program
{
    public void FixMyType()
    {
        var    myint    =    0;
    }
}",
                @"
class Program
{
    public void FixMyType()
    {
        var    myint    =    0;
    }
}", changedOptionSet: changingOptions);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Formatting)]
        public async Task ClassConstraint()
        {
            await AssertFormatAsync(
                @"
class Program<T>
    where T : class?
{
}",
                @"
class Program<T>
    where T : class ?
{
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Formatting)]
        public async Task SingleLinePropertyPattern1()
        {
            await AssertFormatAsync(
                @"
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
}",
                @"
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
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Formatting)]
        public async Task SingleLinePropertyPattern2()
        {
            await AssertFormatAsync(
                @"
using System.Collections.Generic;
class Program
{
    public void FixMyType(object o)
    {
        _ = o is List<int> { Count: { } };
    }
}",
                @"
using System.Collections.Generic;
class Program
{
    public void FixMyType(object o)
    {
        _ = o is List<int>{Count:{}};
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Formatting)]
        [WorkItem(37030, "https://github.com/dotnet/roslyn/issues/37030")]
        public async Task SpaceAroundEnumMemberDeclarationIgnored()
        {
            var changingOptions = new Dictionary<OptionKey, object>
            {
                { CSharpFormattingOptions.SpacesIgnoreAroundVariableDeclaration, true }
            };
            await AssertFormatAsync(
                @"
enum TestEnum
{
    Short           = 1,
    LongItemName    = 2
}",
                @"
enum TestEnum
{
    Short           = 1,
    LongItemName    = 2
}", changedOptionSet: changingOptions);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Formatting)]
        [WorkItem(37030, "https://github.com/dotnet/roslyn/issues/37030")]
        public async Task SpaceAroundEnumMemberDeclarationSingle()
        {
            var changingOptions = new Dictionary<OptionKey, object>
            {
                { CSharpFormattingOptions.SpacesIgnoreAroundVariableDeclaration, false }
            };
            await AssertFormatAsync(
                @"
enum TestEnum
{
    Short = 1,
    LongItemName = 2
}",
                @"
enum TestEnum
{
    Short           = 1,
    LongItemName    = 2
}", changedOptionSet: changingOptions);
        }

    }
}
