// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using Microsoft.CodeAnalysis.CSharp.UnitTests;
using Microsoft.VisualStudio.Debugger.Contracts.EditAndContinue;
using Microsoft.CodeAnalysis.EditAndContinue;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.EditAndContinue.UnitTests
{
    [UseExportProvider]
    public class LineEditTests : EditingTestBase
    {
        #region Methods

        [Fact]
        public void Method_Reorder1()
        {
            var src1 = @"
class C
{
    static void Goo()
    {
        Console.ReadLine(1);
    }

    static void Bar()
    {
        Console.ReadLine(2);
    }
}
";
            var src2 = @"
class C
{
    static void Bar()
    {
        Console.ReadLine(2);
    }

    static void Goo()
    {
        Console.ReadLine(1);
    }
}";
            var edits = GetTopEdits(src1, src2);
            edits.VerifyLineEdits(
                new[] { new SourceLineUpdate(4, 9), new SourceLineUpdate(9, 4) },
                Array.Empty<string>());
        }

        [Fact]
        public void Method_Reorder2()
        {
            var src1 = @"
class Program
{
    static void Main()
    {
        Goo();
        Bar();
    }

    static int Goo()
    {
        return 1;
    }

    static int Bar()
    {
        return 2;
    }
}";
            var src2 = @"
class Program
{
    static int Goo()
    {
        return 1;
    }

    static void Main()
    {
        Goo();
        Bar();
    }

    static int Bar()
    {
        return 2;
    }
}";
            var edits = GetTopEdits(src1, src2);
            edits.VerifyLineEdits(
                new[] { new SourceLineUpdate(4, 9), new SourceLineUpdate(10, 4) },
                Array.Empty<string>());
        }

        [Fact]
        public void Method_Update()
        {
            var src1 = @"
class C
{
    static void Bar()
    {
        Console.ReadLine(1);
    }
}
";
            var src2 = @"
class C
{
    static void Bar()
    {


        Console.ReadLine(2);
    }
}";
            var edits = GetTopEdits(src1, src2);
            edits.VerifyLineEdits(
                Array.Empty<SourceLineUpdate>(),
                Array.Empty<string>());
        }

        [Fact]
        public void Method_LineChange1()
        {
            var src1 = @"
class C
{
    static void Bar()
    {
        Console.ReadLine(2);
    }
}
";
            var src2 = @"
class C
{


    static void Bar()
    {
        Console.ReadLine(2);
    }
}";
            var edits = GetTopEdits(src1, src2);
            edits.VerifyLineEdits(
                new[] { new SourceLineUpdate(4, 6) },
                Array.Empty<string>());
        }

        [Fact]
        public void Method_LineChange2()
        {
            var src1 = @"
class C
{
    static void Bar()
    {
        Console.ReadLine(2);
    }
}
";
            var src2 = @"
class C
{
    static void Bar()

    {
        Console.ReadLine(2);
    }
}";
            var edits = GetTopEdits(src1, src2);
            edits.VerifyLineEdits(
                new[] { new SourceLineUpdate(4, 5) },
                Array.Empty<string>());
        }

        [Fact]
        public void Method_Recompile1()
        {
            var src1 = @"
class C
{
    static void Bar()
    {
        Console.ReadLine(2);
    }
}
";
            var src2 = @"
class C
{
    static void Bar()
    {
        /**/Console.ReadLine(2);
    }
}";
            var edits = GetTopEdits(src1, src2);
            edits.VerifyLineEdits(
                Array.Empty<SourceLineUpdate>(),
                new string[] { "static void Bar()" });
        }

        [Fact]
        public void Method_Recompile2()
        {
            var src1 = @"
class C
{
    static void Bar()
    {

        Console.ReadLine(2);
    }
}
";
            var src2 = @"
class C
{
    static void Bar()
    {
        Console.ReadLine(2);
    }
}";
            var edits = GetTopEdits(src1, src2);
            edits.VerifyLineEdits(
                Array.Empty<SourceLineUpdate>(),
                new string[] { "static void Bar()" });
        }

        [Fact]
        public void Method_Recompile3()
        {
            var src1 = @"
class C
{
    static void Bar()
    /*1*/
    {
        Console.ReadLine(2);
    }
}
";
            var src2 = @"
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
                Array.Empty<SourceLineUpdate>(),
                new string[] { "static void Bar()" });
        }

        [Fact]
        public void Method_Recompile4()
        {
            var src1 = @"
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
            var src2 = @"
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
                Array.Empty<SourceLineUpdate>(),
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
            var src1 = @"
class C { static void Bar() { } }
";
            var src2 = @"
class C { /*--*/static void Bar() { } }";

            var edits = GetTopEdits(src1, src2);
            edits.VerifyLineEdits(
                Array.Empty<SourceLineUpdate>(),
                new string[] { "static void Bar() { }" });
        }

        [Fact]
        public void Method_RudeRecompile1()
        {
            var src1 = @"
class C<T>
{
    static void Bar()
    {
        /*edit*/
        Console.ReadLine(2);
    }
}
";
            var src2 = @"
class C<T>
{
    static void Bar()
    {
        Console.ReadLine(2);
    }
}";

            var edits = GetTopEdits(src1, src2);
            edits.VerifyLineEdits(
                Array.Empty<SourceLineUpdate>(),
                new string[] { "static void Bar()" },
                Diagnostic(RudeEditKind.GenericTypeTriviaUpdate, "\r\n        ", FeaturesResources.method));
        }

        [Fact]
        public void Method_RudeRecompile2()
        {
            var src1 = @"
class C<T>
{
    static void Bar()
    {
        Console.ReadLine(2);
    }
}
";
            var src2 = @"
class C<T>
{
    static void Bar()
    {
        /*edit*/Console.ReadLine(2);
    }
}";

            var edits = GetTopEdits(src1, src2);
            edits.VerifyLineEdits(
                Array.Empty<SourceLineUpdate>(),
                new string[] { "static void Bar()" },
                Diagnostic(RudeEditKind.GenericTypeTriviaUpdate, "\r\n        /*edit*/", FeaturesResources.method));
        }

        [Fact]
        public void Method_RudeRecompile3()
        {
            var src1 = @"
class C
{
    static void Bar<T>()
    {
        /*edit*/Console.ReadLine(2);
    }
}
";
            var src2 = @"
class C
{
    static void Bar<T>()
    {
        Console.ReadLine(2);
    }
}";

            var edits = GetTopEdits(src1, src2);
            edits.VerifyLineEdits(
                Array.Empty<SourceLineUpdate>(),
                new string[] { "static void Bar<T>()" },
                Diagnostic(RudeEditKind.GenericMethodTriviaUpdate, "\r\n        ", FeaturesResources.method));
        }

        [Fact]
        public void Method_RudeRecompile4()
        {
            var src1 = @"
class C
{
    static async Task<int> Bar()
    {
        Console.ReadLine(2);
    }
}
";
            var src2 = @"
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
                Array.Empty<SourceLineUpdate>(),
                new string[] { "static async Task<int> Bar()" });
        }

        #endregion

        #region Constructors

        [Fact]
        public void Constructor_Reorder()
        {
            var src1 = @"
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
            var src2 = @"
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
                new[] { new SourceLineUpdate(4, 8), new SourceLineUpdate(8, 4) },
                Array.Empty<string>());
        }

        [Fact]
        public void Constructor_LineChange1()
        {
            var src1 = @"
class C
{
    public C(int a)
      : base()
    {
    }
}
";
            var src2 = @"
class C
{
    public C(int a) 

      : base()
    {
    }
}";
            var edits = GetTopEdits(src1, src2);
            edits.VerifyLineEdits(
                new[] { new SourceLineUpdate(4, 5) },
                Array.Empty<string>());
        }

        [Fact]
        public void Constructor_ExpressionBodied_LineChange1()
        {
            var src1 = @"
class C
{
    int _a;
    public C(int a) => 
      _a = a;
}
";
            var src2 = @"
class C
{
    int _a;
    public C(int a) =>

      _a = a;
}";
            var edits = GetTopEdits(src1, src2);
            edits.VerifyLineEdits(
                new[] { new SourceLineUpdate(5, 6) },
                Array.Empty<string>());
        }

        [Fact]
        public void Constructor_ExpressionBodied_LineChange2()
        {
            var src1 = @"
class C
{
    int _a;
    public C(int a) 
      => _a = a;
}
";
            var src2 = @"
class C
{
    int _a;
    public C(int a)

      => _a = a;
}";
            var edits = GetTopEdits(src1, src2);
            edits.VerifyLineEdits(
                new[] { new SourceLineUpdate(5, 6) },
                Array.Empty<string>());
        }

        [Fact]
        public void Constructor_ExpressionBodied_LineChange3()
        {
            var src1 = @"
class C
{
    int _a;
    public C(int a) => 
      _a = a;
}
";
            var src2 = @"
class C
{
    int _a;
    public C(int a) => 

      _a = a;
}";
            var edits = GetTopEdits(src1, src2);
            edits.VerifyLineEdits(
                new[] { new SourceLineUpdate(5, 6) },
                Array.Empty<string>());
        }

        [Fact]
        public void Constructor_ExpressionBodied_LineChange4()
        {
            var src1 = @"
class C
{
    int _a;
    public C(int a) 
      => 
      _a = a;
}
";
            var src2 = @"
class C
{
    int _a;
    public C(int a) 
      
      => 

      _a = a;
}";
            var edits = GetTopEdits(src1, src2);
            edits.VerifyLineEdits(
                new[] { new SourceLineUpdate(6, 8) },
                Array.Empty<string>());
        }

        [Fact]
        public void Constructor_ExpressionBodiedWithBase_LineChange1()
        {
            var src1 = @"
class C
{
    int _a;
    public C(int a)
      : base() => _a = a;
}
";
            var src2 = @"
class C
{
    int _a;
    public C(int a)

      : base() => _a = a;
}";
            var edits = GetTopEdits(src1, src2);
            edits.VerifyLineEdits(
                new[] { new SourceLineUpdate(5, 6) },
                Array.Empty<string>());
        }

        [Fact]
        public void Constructor_ExpressionBodiedWithBase_Recompile1()
        {
            var src1 = @"
class C
{
    int _a;
    public C(int a)
      : base() => 
                  _a = a;
}
";
            var src2 = @"
class C
{
    int _a;
    public C(int a)

      : base() => _a = a;
}";
            var edits = GetTopEdits(src1, src2);
            edits.VerifyLineEdits(
                Array.Empty<SourceLineUpdate>(),
                new string[] { "public C(int a)" });
        }

        [Fact]
        public void Constructor_Recompile1()
        {
            var src1 = @"
class C
{
    public C(int a)
      : base()
    {
    }
}
";
            var src2 = @"
class C
{
    public C(int a)
      : base()

    {
    }
}";
            var edits = GetTopEdits(src1, src2);
            edits.VerifyLineEdits(
                Array.Empty<SourceLineUpdate>(),
                new string[] { "public C(int a)" });
        }

        [Fact]
        public void Constructor_Recompile2()
        {
            var src1 = @"
class C
{
    public C(int a)
      : base()
    {
    }
}
";
            var src2 = @"
class C
{
    public C(int a)
          : base()
    {
    }
}";
            var edits = GetTopEdits(src1, src2);
            edits.VerifyLineEdits(
                Array.Empty<SourceLineUpdate>(),
                new string[] { "public C(int a)" });
        }

        [Fact]
        public void Constructor_RudeRecompile1()
        {
            var src1 = @"
class C<T>
{
    public C(int a)
      : base()
    {
    }
}
";
            var src2 = @"
class C<T>
{
    public C(int a)
          : base()
    {
    }
}";
            var edits = GetTopEdits(src1, src2);
            edits.VerifyLineEdits(
                Array.Empty<SourceLineUpdate>(),
                new string[] { "public C(int a)" },
                Diagnostic(RudeEditKind.GenericTypeTriviaUpdate, "          ", FeaturesResources.constructor));
        }

        #endregion

        #region Destructors

        [Fact]
        public void Destructor_LineChange1()
        {
            var src1 = @"
class C
{
    ~C()

    {
    }
}
";
            var src2 = @"
class C
{
    ~C()
    {
    }
}";
            var edits = GetTopEdits(src1, src2);
            edits.VerifyLineEdits(
                new[] { new SourceLineUpdate(5, 4) },
                Array.Empty<string>());
        }

        [Fact]
        public void Destructor_ExpressionBodied_LineChange1()
        {
            var src1 = @"
class C
{
    ~C() => F();
}
";
            var src2 = @"
class C
{
    ~C() => 
            F();
}";
            var edits = GetTopEdits(src1, src2);
            edits.VerifyLineEdits(
                new[] { new SourceLineUpdate(3, 4) },
                Array.Empty<string>());
        }

        [Fact]
        public void Destructor_ExpressionBodied_LineChange2()
        {
            var src1 = @"
class C
{
    ~C() => F();
}
";
            var src2 = @"
class C
{
    ~C() 
         => F();
}";
            var edits = GetTopEdits(src1, src2);
            edits.VerifyLineEdits(
                new[] { new SourceLineUpdate(3, 4) },
                Array.Empty<string>());
        }

        #endregion

        #region Field Initializers

        [Fact]
        public void ConstantField()
        {
            var src1 = @"
class C
{
    const int Goo = 1;
}
";
            var src2 = @"
class C
{
    const int Goo = 
                    1;
}";
            var edits = GetTopEdits(src1, src2);
            edits.VerifyLineEdits(
                Array.Empty<SourceLineUpdate>(),
                Array.Empty<string>());
        }

        [Fact]
        public void NoInitializer()
        {
            var src1 = @"
class C
{
    int Goo;
}
";
            var src2 = @"
class C
{
    int 
        Goo;
}";
            var edits = GetTopEdits(src1, src2);
            edits.VerifyLineEdits(
                Array.Empty<SourceLineUpdate>(),
                Array.Empty<string>());
        }

        [Fact]
        public void Field_Reorder()
        {
            var src1 = @"
class C
{
    static int Goo = 1;
    static int Bar = 2;
}
";
            var src2 = @"
class C
{
    static int Bar = 2;
    static int Goo = 1;
}";
            var edits = GetTopEdits(src1, src2);
            edits.VerifyLineEdits(
                new[] { new SourceLineUpdate(3, 4), new SourceLineUpdate(4, 3) },
                Array.Empty<string>());
        }

        [Fact]
        public void Field_LineChange1()
        {
            var src1 = @"
class C
{
    static int Goo = 1;
}
";
            var src2 = @"
class C
{



    static int Goo = 1;
}";
            var edits = GetTopEdits(src1, src2);
            edits.VerifyLineEdits(
                new[] { new SourceLineUpdate(3, 6) },
                Array.Empty<string>());
        }

        [Fact]
        public void Field_LineChange2()
        {
            var src1 = @"
class C
{
    int Goo = 1, Bar = 2;
}
";
            var src2 = @"
class C
{
    int Goo = 1,
                 Bar = 2;
}";
            var edits = GetTopEdits(src1, src2);
            edits.VerifyLineEdits(
                new SourceLineUpdate[] { new SourceLineUpdate(3, 4) },
                Array.Empty<string>());
        }

        [Fact]
        public void Field_LineChange3()
        {
            var src1 = @"
class C
{
    [A]static int Goo = 1, Bar = 2;
}
";
            var src2 = @"
class C
{
    [A]
       static int Goo = 1, Bar = 2;
}";
            var edits = GetTopEdits(src1, src2);
            edits.VerifyLineEdits(
                new SourceLineUpdate[] { new SourceLineUpdate(3, 4), new SourceLineUpdate(3, 4) },
                Array.Empty<string>());
        }

        [Fact]
        public void Field_Recompile1a()
        {
            var src1 = @"
class C
{
    static int Goo = 1;
}
";
            var src2 = @"
class C
{
    static int Goo = 
                     1;
}";
            var edits = GetTopEdits(src1, src2);
            edits.VerifyLineEdits(
                Array.Empty<SourceLineUpdate>(),
                new string[] { "Goo = " });
        }

        [Fact]
        public void Field_Recompile1b()
        {
            var src1 = @"
class C
{
    static int Goo = 1;
}
";
            var src2 = @"
class C
{
    static int Goo 
                   = 1;
}";
            var edits = GetTopEdits(src1, src2);
            edits.VerifyLineEdits(
                Array.Empty<SourceLineUpdate>(),
                new string[] { "Goo " });
        }

        [Fact]
        public void Field_Recompile1c()
        {
            var src1 = @"
class C
{
    static int Goo = 1;
}
";
            var src2 = @"
class C
{
    static int 
               Goo = 1;
}";
            var edits = GetTopEdits(src1, src2);
            edits.VerifyLineEdits(
                Array.Empty<SourceLineUpdate>(),
                new string[] { "Goo = 1" });
        }

        [Fact]
        public void Field_Recompile1d()
        {
            var src1 = @"
class C
{
    static int Goo = 1;
}
";
            var src2 = @"
class C
{
    static 
           int Goo = 1;
}";
            var edits = GetTopEdits(src1, src2);
            edits.VerifyLineEdits(
                Array.Empty<SourceLineUpdate>(),
                new string[] { "Goo = 1" });
        }

        [Fact]
        public void Field_Recompile1e()
        {
            var src1 = @"
class C
{
    static int Goo = 1;
}
";
            var src2 = @"
class C
{
    static int Goo = 1
                      ;
}";
            var edits = GetTopEdits(src1, src2);
            edits.VerifyLineEdits(
                Array.Empty<SourceLineUpdate>(),
                new string[] { "Goo = 1" });
        }

        [Fact]
        public void Field_Recompile2()
        {
            var src1 = @"
class C
{
    static int Goo = 1 + 1;
}
";
            var src2 = @"
class C
{
    static int Goo = 1 +  1;
}";
            var edits = GetTopEdits(src1, src2);
            edits.VerifyLineEdits(
                Array.Empty<SourceLineUpdate>(),
                new string[] { "Goo = 1 +  1" });
        }

        [Fact]
        public void Field_RudeRecompile2()
        {
            var src1 = @"
class C<T>
{
    static int Goo = 1 + 1;
}
";
            var src2 = @"
class C<T>
{
    static int Goo = 1 +  1;
}";
            var edits = GetTopEdits(src1, src2);
            edits.VerifyLineEdits(
                Array.Empty<SourceLineUpdate>(),
                new string[] { "Goo = 1 +  1" },
                Diagnostic(RudeEditKind.GenericTypeTriviaUpdate, "  ", FeaturesResources.field));
        }

        #endregion

        #region Properties

        [Fact]
        public void Property1()
        {
            var src1 = @"
class C
{
    int P { get { return 1; } }
}
";
            var src2 = @"
class C
{
    int P { get { return 
                         1; } }
}";
            var edits = GetTopEdits(src1, src2);
            edits.VerifyLineEdits(
                Array.Empty<SourceLineUpdate>(),
                new string[] { "get { return " });
        }

        [Fact]
        public void Property2()
        {
            var src1 = @"
class C
{
    int P { get { return 1; } }
}
";
            var src2 = @"
class C
{
    int P { get 
                { return 1; } }
}";
            var edits = GetTopEdits(src1, src2);
            edits.VerifyLineEdits(
                new[] { new SourceLineUpdate(3, 4) },
                Array.Empty<string>());
        }

        [Fact]
        public void Property3()
        {
            var src1 = @"
class C
{
    int P { get { return 1; } set { } }
}
";
            var src2 = @"
class C
{
    
    int P { get { return 1; } set { } }
}";
            var edits = GetTopEdits(src1, src2);
            edits.VerifyLineEdits(
                new[] { new SourceLineUpdate(3, 4), new SourceLineUpdate(3, 4) },
                Array.Empty<string>());
        }

        [Fact]
        public void Property_ExpressionBody1()
        {
            var src1 = @"
class C
{
    int P => 1;
}
";
            var src2 = @"
class C
{
    int P => 
             1;
}";
            var edits = GetTopEdits(src1, src2);
            edits.VerifyLineEdits(
                new[] { new SourceLineUpdate(3, 4) },
                Array.Empty<string>());
        }

        [Fact]
        public void Property_GetterExpressionBody1()
        {
            var src1 = @"
class C
{
    int P { get => 1; }
}
";
            var src2 = @"
class C
{
    int P { get => 
                   1; }
}";
            var edits = GetTopEdits(src1, src2);
            edits.VerifyLineEdits(
                new[] { new SourceLineUpdate(3, 4) },
                Array.Empty<string>());
        }

        [Fact]
        public void Property_SetterExpressionBody1()
        {
            var src1 = @"
class C
{
    int P { set => F(); }
}
";
            var src2 = @"
class C
{
    int P { set => 
                   F(); }
}";
            var edits = GetTopEdits(src1, src2);
            edits.VerifyLineEdits(
                new[] { new SourceLineUpdate(3, 4) },
                Array.Empty<string>());
        }

        [Fact]
        public void Property_Initializer1()
        {
            var src1 = @"
class C
{
    int P { get; } = 1;
}
";
            var src2 = @"
class C
{
    int P { 
            get; } = 1;
}";
            var edits = GetTopEdits(src1, src2);
            edits.VerifyLineEdits(
                new[] { new SourceLineUpdate(3, 4) },
                Array.Empty<string>());
        }

        [Fact]
        public void Property_Initializer2()
        {
            var src1 = @"
class C
{
    int P { get; } = 1;
}
";
            var src2 = @"
class C
{
    int P { get; } = 
                     1;
}";
            var edits = GetTopEdits(src1, src2);
            edits.VerifyLineEdits(
                new[] { new SourceLineUpdate(3, 4) },
                Array.Empty<string>());
        }

        [Fact]
        public void Property_Initializer3()
        {
            var src1 = @"
class C
{
    int P { get; } = 1;
}
";
            var src2 = @"
class C
{
    int P { get; } =  1;
}";
            var edits = GetTopEdits(src1, src2);
            edits.VerifyLineEdits(
                Array.Empty<SourceLineUpdate>(),
                new string[] { "int P { get; } =  1;" });
        }

        #endregion

        #region Events

        [Fact]
        public void Event_LineChange1()
        {
            var src1 = @"
class C
{
    event Action E { add { } remove { } }
}
";
            var src2 = @"
class C
{

    event Action E { add { } remove { } }
}";
            var edits = GetTopEdits(src1, src2);
            edits.VerifyLineEdits(
                new[] { new SourceLineUpdate(3, 4), new SourceLineUpdate(3, 4) },
                Array.Empty<string>());
        }

        [Fact]
        public void EventAdder_LineChangeAndRecompile1()
        {
            var src1 = @"
class C
{
    event Action E { add {
                           } remove { } }
}
";
            var src2 = @"
class C
{
    event Action E { add { } remove { } }
}";
            var edits = GetTopEdits(src1, src2);
            edits.VerifyLineEdits(
                new[] { new SourceLineUpdate(4, 3) },
                new string[] { "add { }" });
        }

        [Fact]
        public void EventRemover_Recompile1()
        {
            var src1 = @"
class C
{
    event Action E { add { } remove {
                                      } }
}
";
            var src2 = @"
class C
{
    event Action E { add { } remove { } }
}";
            var edits = GetTopEdits(src1, src2);
            edits.VerifyLineEdits(
                Array.Empty<SourceLineUpdate>(),
                new string[] { "remove { }" });
        }

        [Fact]
        public void EventAdder_LineChange1()
        {
            var src1 = @"
class C
{
    event Action E { add 
                         { } remove { } }
}
";
            var src2 = @"
class C
{
    event Action E { add { } remove { } }
}";
            var edits = GetTopEdits(src1, src2);
            edits.VerifyLineEdits(
                new[] { new SourceLineUpdate(4, 3), new SourceLineUpdate(4, 3) },
                Array.Empty<string>());
        }

        [Fact]
        public void EventRemover_LineChange1()
        {
            var src1 = @"
class C
{
    event Action E { add { } remove { } }
}
";
            var src2 = @"
class C
{
    event Action E { add { } remove 
                                    { } }
}";
            var edits = GetTopEdits(src1, src2);
            edits.VerifyLineEdits(
                new[] { new SourceLineUpdate(3, 4) },
                Array.Empty<string>());
        }

        [Fact]
        public void Event_ExpressionBody1()
        {
            var src1 = @"
class C
{
    event Action E { add => F(); remove => F(); }
}
";
            var src2 = @"
class C
{
    event Action E { add => 
                            F(); remove => 
                                           F(); }
}";
            var edits = GetTopEdits(src1, src2);
            edits.VerifyLineEdits(
                new[] { new SourceLineUpdate(3, 4), new SourceLineUpdate(3, 5) },
                Array.Empty<string>());
        }

        [Fact]
        public void Event_ExpressionBody2()
        {
            var src1 = @"
class C
{
    event Action E { add 
                         => F(); remove 
                                        => F(); }
}
";
            var src2 = @"
class C
{
    event Action E { add => F(); remove => F(); }
}";
            var edits = GetTopEdits(src1, src2);
            edits.VerifyLineEdits(
                new[] { new SourceLineUpdate(4, 3), new SourceLineUpdate(5, 3) },
                Array.Empty<string>());
        }

        #endregion

        #region Types

        [Fact]
        public void Type_Reorder1()
        {
            var src1 = @"
class C
{
    static int F1() => 1;
    static int F2() => 1;
}

class D
{
    static int G1() => 1;
    static int G2() => 1;
}
";
            var src2 = @"
class D
{
    static int G1() => 1;
    static int G2() => 1;
}

class C
{
    static int F1() => 1;
    static int F2() => 1;
}
";
            var edits = GetTopEdits(src1, src2);
            edits.VerifyLineEdits(
                new[] { new SourceLineUpdate(3, 9), new SourceLineUpdate(4, 10), new SourceLineUpdate(9, 3), new SourceLineUpdate(10, 4) },
                Array.Empty<string>());
        }

        #endregion
    }
}
