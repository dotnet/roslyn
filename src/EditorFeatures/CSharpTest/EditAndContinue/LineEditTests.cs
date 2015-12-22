// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Microsoft.CodeAnalysis.CSharp.UnitTests;
using Microsoft.CodeAnalysis.Differencing;
using Microsoft.CodeAnalysis.EditAndContinue;
using Microsoft.CodeAnalysis.Emit;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.EditAndContinue.UnitTests
{
    public class LineEditTests : RudeEditTestBase
    {
        #region Methods

        [Fact]
        public void Method_Reorder1()
        {
            string src1 = @"
class C
{
    static void Foo()
    {
        Console.ReadLine(1);
    }

    static void Bar()
    {
        Console.ReadLine(2);
    }
}
";
            string src2 = @"
class C
{
    static void Bar()
    {
        Console.ReadLine(2);
    }

    static void Foo()
    {
        Console.ReadLine(1);
    }
}";
            var edits = GetTopEdits(src1, src2);
            edits.VerifyLineEdits(
                new[] { new LineChange(4, 9), new LineChange(9, 4) },
                Array.Empty<string>());
        }

        [Fact]
        public void Method_Reorder2()
        {
            string src1 = @"
class Program
{
    static void Main()
    {
        Foo();
        Bar();
    }

    static int Foo()
    {
        return 1;
    }

    static int Bar()
    {
        return 2;
    }
}";
            string src2 = @"
class Program
{
    static int Foo()
    {
        return 1;
    }

    static void Main()
    {
        Foo();
        Bar();
    }

    static int Bar()
    {
        return 2;
    }
}";
            var edits = GetTopEdits(src1, src2);
            edits.VerifyLineEdits(
                new[] { new LineChange(4, 9), new LineChange(10, 4) },
                Array.Empty<string>());
        }

        [Fact]
        public void Method_Update()
        {
            string src1 = @"
class C
{
    static void Bar()
    {
        Console.ReadLine(1);
    }
}
";
            string src2 = @"
class C
{
    static void Bar()
    {


        Console.ReadLine(2);
    }
}";
            var edits = GetTopEdits(src1, src2);
            edits.VerifyLineEdits(
                Array.Empty<LineChange>(),
                Array.Empty<string>());
        }

        [Fact]
        public void Method_LineChange1()
        {
            string src1 = @"
class C
{
    static void Bar()
    {
        Console.ReadLine(2);
    }
}
";
            string src2 = @"
class C
{


    static void Bar()
    {
        Console.ReadLine(2);
    }
}";
            var edits = GetTopEdits(src1, src2);
            edits.VerifyLineEdits(
                new[] { new LineChange(4, 6) },
                Array.Empty<string>());
        }

        [Fact]
        public void Method_LineChange2()
        {
            string src1 = @"
class C
{
    static void Bar()
    {
        Console.ReadLine(2);
    }
}
";
            string src2 = @"
class C
{
    static void Bar()

    {
        Console.ReadLine(2);
    }
}";
            var edits = GetTopEdits(src1, src2);
            edits.VerifyLineEdits(
                new[] { new LineChange(4, 5) },
                Array.Empty<string>());
        }

        [Fact]
        public void Method_Recompile1()
        {
            string src1 = @"
class C
{
    static void Bar()
    {
        Console.ReadLine(2);
    }
}
";
            string src2 = @"
class C
{
    static void Bar()
    {
        /**/Console.ReadLine(2);
    }
}";
            var edits = GetTopEdits(src1, src2);
            edits.VerifyLineEdits(
                Array.Empty<LineChange>(),
                new string[] { "static void Bar()" });
        }

        [Fact]
        public void Method_Recompile2()
        {
            string src1 = @"
class C
{
    static void Bar()
    {

        Console.ReadLine(2);
    }
}
";
            string src2 = @"
class C
{
    static void Bar()
    {
        Console.ReadLine(2);
    }
}";
            var edits = GetTopEdits(src1, src2);
            edits.VerifyLineEdits(
                Array.Empty<LineChange>(),
                new string[] { "static void Bar()" });
        }

        [Fact]
        public void Method_Recompile3()
        {
            string src1 = @"
class C
{
    static void Bar()
    /*1*/
    {
        Console.ReadLine(2);
    }
}
";
            string src2 = @"
class C
{
    static void Bar()
    {
        /*2*/
        Console.ReadLine(2);
    }
}";
            var edits = GetTopEdits(src1, src2);
            edits.VerifyLineEdits(
                Array.Empty<LineChange>(),
                new string[] { "static void Bar()" });
        }

        [Fact]
        public void Method_Recompile4()
        {
            string src1 = @"
class C
{
    static void Bar()
    {
        int <N:0.0>a = 1</N:0.0>;
        int <N:0.1>b = 2</N:0.1>;
        <AS:0>System.Console.WriteLine(1);</AS:0>
    }
}
";
            string src2 = @"
class C
{
    static void Bar()
    {
             int <N:0.0>a = 1</N:0.0>;
        int <N:0.1>b = 2</N:0.1>;
        <AS:0>System.Console.WriteLine(1);</AS:0>
    }
}";
            var edits = GetTopEdits(src1, src2);
            edits.VerifyLineEdits(
                Array.Empty<LineChange>(),
                new string[] { "static void Bar()" });

            var active = GetActiveStatements(src1, src2);
            var syntaxMap = GetSyntaxMap(src1, src2);

            edits.VerifySemantics(
                active,
                new[] { SemanticEdit(SemanticEditKind.Update, c => c.GetMember("C.Bar"), syntaxMap[0]) });
        }

        [Fact]
        public void Method_Recompile5()
        {
            string src1 = @"
class C { static void Bar() { } }
";
            string src2 = @"
class C { /*--*/static void Bar() { } }";

            var edits = GetTopEdits(src1, src2);
            edits.VerifyLineEdits(
                Array.Empty<LineChange>(),
                new string[] { "static void Bar() { }" });
        }

        [Fact]
        public void Method_RudeRecompile1()
        {
            string src1 = @"
class C<T>
{
    static void Bar()
    {
        /*edit*/
        Console.ReadLine(2);
    }
}
";
            string src2 = @"
class C<T>
{
    static void Bar()
    {
        Console.ReadLine(2);
    }
}";

            var edits = GetTopEdits(src1, src2);
            edits.VerifyLineEdits(
                Array.Empty<LineChange>(),
                new string[] { "static void Bar()" },
                Diagnostic(RudeEditKind.GenericTypeTriviaUpdate, "\r\n        ", FeaturesResources.Method));
        }

        [Fact]
        public void Method_RudeRecompile2()
        {
            string src1 = @"
class C<T>
{
    static void Bar()
    {
        Console.ReadLine(2);
    }
}
";
            string src2 = @"
class C<T>
{
    static void Bar()
    {
        /*edit*/Console.ReadLine(2);
    }
}";

            var edits = GetTopEdits(src1, src2);
            edits.VerifyLineEdits(
                Array.Empty<LineChange>(),
                new string[] { "static void Bar()" },
                Diagnostic(RudeEditKind.GenericTypeTriviaUpdate, "\r\n        /*edit*/", FeaturesResources.Method));
        }

        [Fact]
        public void Method_RudeRecompile3()
        {
            string src1 = @"
class C
{
    static void Bar<T>()
    {
        /*edit*/Console.ReadLine(2);
    }
}
";
            string src2 = @"
class C
{
    static void Bar<T>()
    {
        Console.ReadLine(2);
    }
}";

            var edits = GetTopEdits(src1, src2);
            edits.VerifyLineEdits(
                Array.Empty<LineChange>(),
                new string[] { "static void Bar<T>()" },
                Diagnostic(RudeEditKind.GenericMethodTriviaUpdate, "\r\n        ", FeaturesResources.Method));
        }

        [Fact]
        public void Method_RudeRecompile4()
        {
            string src1 = @"
class C
{
    static async Task<int> Bar()
    {
        Console.ReadLine(2);
    }
}
";
            string src2 = @"
class C
{
    static async Task<int> Bar()
    {
        Console.ReadLine( 

2);
    }
}";

            var edits = GetTopEdits(src1, src2);
            edits.VerifyLineEdits(
                Array.Empty<LineChange>(),
                new string[] { "static async Task<int> Bar()" });
        }

        #endregion

        #region Constructors

        [Fact]
        public void Constructor_Reorder()
        {
            string src1 = @"
class C
{
    public C(int a)
    {
    }

    public C(bool a)
    {
    }
}
";
            string src2 = @"
class C
{
    public C(bool a)
    {
    }

    public C(int a)
    {
    }
}";
            var edits = GetTopEdits(src1, src2);
            edits.VerifyLineEdits(
                new[] { new LineChange(4, 8), new LineChange(8, 4) },
                Array.Empty<string>());
        }

        [Fact]
        public void Constructor_LineChange1()
        {
            string src1 = @"
class C
{
    public C(int a)
      : base()
    {
    }
}
";
            string src2 = @"
class C
{
    public C(int a) 

      : base()
    {
    }
}";
            var edits = GetTopEdits(src1, src2);
            edits.VerifyLineEdits(
                new[] { new LineChange(4, 5) },
                Array.Empty<string>());
        }

        [Fact]
        public void Constructor_Recompile1()
        {
            string src1 = @"
class C
{
    public C(int a)
      : base()
    {
    }
}
";
            string src2 = @"
class C
{
    public C(int a)
      : base()

    {
    }
}";
            var edits = GetTopEdits(src1, src2);
            edits.VerifyLineEdits(
                Array.Empty<LineChange>(),
                new string[] { "public C(int a)" });
        }

        [Fact]
        public void Constructor_Recompile2()
        {
            string src1 = @"
class C
{
    public C(int a)
      : base()
    {
    }
}
";
            string src2 = @"
class C
{
    public C(int a)
          : base()
    {
    }
}";
            var edits = GetTopEdits(src1, src2);
            edits.VerifyLineEdits(
                Array.Empty<LineChange>(),
                new string[] { "public C(int a)" });
        }

        [Fact]
        public void Constructor_RudeRecompile1()
        {
            string src1 = @"
class C<T>
{
    public C(int a)
      : base()
    {
    }
}
";
            string src2 = @"
class C<T>
{
    public C(int a)
          : base()
    {
    }
}";
            var edits = GetTopEdits(src1, src2);
            edits.VerifyLineEdits(
                Array.Empty<LineChange>(),
                new string[] { "public C(int a)" },
                Diagnostic(RudeEditKind.GenericTypeTriviaUpdate, "          ", FeaturesResources.Constructor));
        }

        #endregion

        #region Field Initializers

        [Fact]
        public void ConstantField()
        {
            string src1 = @"
class C
{
    const int Foo = 1;
}
";
            string src2 = @"
class C
{
    const int Foo = 
                    1;
}";
            var edits = GetTopEdits(src1, src2);
            edits.VerifyLineEdits(
                Array.Empty<LineChange>(),
                Array.Empty<string>());
        }

        [Fact]
        public void NoInitializer()
        {
            string src1 = @"
class C
{
    int Foo;
}
";
            string src2 = @"
class C
{
    int 
        Foo;
}";
            var edits = GetTopEdits(src1, src2);
            edits.VerifyLineEdits(
                Array.Empty<LineChange>(),
                Array.Empty<string>());
        }

        [Fact]
        public void Field_Reorder()
        {
            string src1 = @"
class C
{
    static int Foo = 1;
    static int Bar = 2;
}
";
            string src2 = @"
class C
{
    static int Bar = 2;
    static int Foo = 1;
}";
            var edits = GetTopEdits(src1, src2);
            edits.VerifyLineEdits(
                new[] { new LineChange(3, 4), new LineChange(4, 3) },
                Array.Empty<string>());
        }

        [Fact]
        public void Field_LineChange1()
        {
            string src1 = @"
class C
{
    static int Foo = 1;
}
";
            string src2 = @"
class C
{



    static int Foo = 1;
}";
            var edits = GetTopEdits(src1, src2);
            edits.VerifyLineEdits(
                new[] { new LineChange(3, 6) },
                Array.Empty<string>());
        }

        [Fact]
        public void Field_LineChange2()
        {
            string src1 = @"
class C
{
    int Foo = 1, Bar = 2;
}
";
            string src2 = @"
class C
{
    int Foo = 1,
                 Bar = 2;
}";
            var edits = GetTopEdits(src1, src2);
            edits.VerifyLineEdits(
                new LineChange[] { new LineChange(3, 4) },
                Array.Empty<string>());
        }

        [Fact]
        public void Field_LineChange3()
        {
            string src1 = @"
class C
{
    [A]static int Foo = 1, Bar = 2;
}
";
            string src2 = @"
class C
{
    [A]
       static int Foo = 1, Bar = 2;
}";
            var edits = GetTopEdits(src1, src2);
            edits.VerifyLineEdits(
                new LineChange[] { new LineChange(3, 4), new LineChange(3, 4) },
                Array.Empty<string>());
        }

        [Fact]
        public void Field_Recompile1a()
        {
            string src1 = @"
class C
{
    static int Foo = 1;
}
";
            string src2 = @"
class C
{
    static int Foo = 
                     1;
}";
            var edits = GetTopEdits(src1, src2);
            edits.VerifyLineEdits(
                Array.Empty<LineChange>(),
                new string[] { "Foo = " });
        }

        [Fact]
        public void Field_Recompile1b()
        {
            string src1 = @"
class C
{
    static int Foo = 1;
}
";
            string src2 = @"
class C
{
    static int Foo 
                   = 1;
}";
            var edits = GetTopEdits(src1, src2);
            edits.VerifyLineEdits(
                Array.Empty<LineChange>(),
                new string[] { "Foo " });
        }

        [Fact]
        public void Field_Recompile1c()
        {
            string src1 = @"
class C
{
    static int Foo = 1;
}
";
            string src2 = @"
class C
{
    static int 
               Foo = 1;
}";
            var edits = GetTopEdits(src1, src2);
            edits.VerifyLineEdits(
                Array.Empty<LineChange>(),
                new string[] { "Foo = 1" });
        }

        [Fact]
        public void Field_Recompile1d()
        {
            string src1 = @"
class C
{
    static int Foo = 1;
}
";
            string src2 = @"
class C
{
    static 
           int Foo = 1;
}";
            var edits = GetTopEdits(src1, src2);
            edits.VerifyLineEdits(
                Array.Empty<LineChange>(),
                new string[] { "Foo = 1" });
        }

        [Fact]
        public void Field_Recompile1e()
        {
            string src1 = @"
class C
{
    static int Foo = 1;
}
";
            string src2 = @"
class C
{
    static int Foo = 1
                      ;
}";
            var edits = GetTopEdits(src1, src2);
            edits.VerifyLineEdits(
                Array.Empty<LineChange>(),
                new string[] { "Foo = 1" });
        }

        [Fact]
        public void Field_Recompile2()
        {
            string src1 = @"
class C
{
    static int Foo = 1 + 1;
}
";
            string src2 = @"
class C
{
    static int Foo = 1 +  1;
}";
            var edits = GetTopEdits(src1, src2);
            edits.VerifyLineEdits(
                Array.Empty<LineChange>(),
                new string[] { "Foo = 1 +  1" });
        }

        [Fact]
        public void Field_RudeRecompile2()
        {
            string src1 = @"
class C<T>
{
    static int Foo = 1 + 1;
}
";
            string src2 = @"
class C<T>
{
    static int Foo = 1 +  1;
}";
            var edits = GetTopEdits(src1, src2);
            edits.VerifyLineEdits(
                Array.Empty<LineChange>(),
                new string[] { "Foo = 1 +  1" },
                Diagnostic(RudeEditKind.GenericTypeTriviaUpdate, "  ", FeaturesResources.Field));
        }

        #endregion

        #region Properties

        [Fact]
        public void Property1()
        {
            string src1 = @"
class C
{
    int P { get { return 1; } }
}
";
            string src2 = @"
class C
{
    int P { get { return 
                         1; } }
}";
            var edits = GetTopEdits(src1, src2);
            edits.VerifyLineEdits(
                Array.Empty<LineChange>(),
                new string[] { "get { return " });
        }

        [Fact]
        public void Property2()
        {
            string src1 = @"
class C
{
    int P { get { return 1; } }
}
";
            string src2 = @"
class C
{
    int P { get 
                { return 1; } }
}";
            var edits = GetTopEdits(src1, src2);
            edits.VerifyLineEdits(
                new[] { new LineChange(3, 4) },
                Array.Empty<string>());
        }

        [Fact]
        public void Property3()
        {
            string src1 = @"
class C
{
    int P { get { return 1; } set { } }
}
";
            string src2 = @"
class C
{
    
    int P { get { return 1; } set { } }
}";
            var edits = GetTopEdits(src1, src2);
            edits.VerifyLineEdits(
                new[] { new LineChange(3, 4), new LineChange(3, 4) },
                Array.Empty<string>());
        }

        [Fact]
        public void Property_ExpressionBody1()
        {
            string src1 = @"
class C
{
    int P => 1;
}
";
            string src2 = @"
class C
{
    int P => 
             1;
}";
            var edits = GetTopEdits(src1, src2);
            edits.VerifyLineEdits(
                new[] { new LineChange(3, 4) },
                Array.Empty<string>());
        }

        [Fact]
        public void Property_Initializer1()
        {
            string src1 = @"
class C
{
    int P { get; } = 1;
}
";
            string src2 = @"
class C
{
    int P { 
            get; } = 1;
}";
            var edits = GetTopEdits(src1, src2);
            edits.VerifyLineEdits(
                new[] { new LineChange(3, 4) },
                Array.Empty<string>());
        }

        [Fact]
        public void Property_Initializer2()
        {
            string src1 = @"
class C
{
    int P { get; } = 1;
}
";
            string src2 = @"
class C
{
    int P { get; } = 
                     1;
}";
            var edits = GetTopEdits(src1, src2);
            edits.VerifyLineEdits(
                new[] { new LineChange(3, 4) },
                Array.Empty<string>());
        }

        [Fact]
        public void Property_Initializer3()
        {
            string src1 = @"
class C
{
    int P { get; } = 1;
}
";
            string src2 = @"
class C
{
    int P { get; } =  1;
}";
            var edits = GetTopEdits(src1, src2);
            edits.VerifyLineEdits(
                Array.Empty<LineChange>(),
                new string[] { "int P { get; } =  1;" });
        }

        #endregion
    }
}
