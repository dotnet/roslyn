// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.UnitTests;
using Microsoft.CodeAnalysis.EditAndContinue;
using Microsoft.CodeAnalysis.Contracts.EditAndContinue;
using Microsoft.CodeAnalysis.EditAndContinue.UnitTests;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.EditAndContinue.UnitTests
{
    [UseExportProvider]
    public class LineEditTests : EditingTestBase
    {
        #region Top-level Code

        [Fact, WorkItem("https://dev.azure.com/devdiv/DevDiv/_workitems/edit/1426286")]
        public void TopLevelCode_LineChange()
        {
            var src1 = @"
Console.ReadLine(1);
";
            var src2 = @"

Console.ReadLine(1);
";
            var edits = GetTopEdits(src1, src2);
            edits.VerifyLineEdits(
                new[] { new SourceLineUpdate(1, 2) });
        }

        [Fact, WorkItem("https://dev.azure.com/devdiv/DevDiv/_workitems/edit/1426286")]
        public void TopLevelCode_LocalFunction_LineChange()
        {
            var src1 = @"
void F()
{
    Console.ReadLine(1);
}
";
            var src2 = @"
void F()
{

    Console.ReadLine(1);
}
";
            var edits = GetTopEdits(src1, src2);
            edits.VerifyLineEdits(
                new[] { new SourceLineUpdate(3, 4) });
        }

        #endregion

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
                new[]
                {
                    new SourceLineUpdate(4, 9),
                    new SourceLineUpdate(7, 7),
                    new SourceLineUpdate(9, 4)
                });
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
                new[]
                {
                    new SourceLineUpdate(4, 9),
                    new SourceLineUpdate(8, 8),
                    new SourceLineUpdate(10, 4),
                    new SourceLineUpdate(13, 13),
                });
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
                Array.Empty<SequencePointUpdates>(),
                semanticEdits: new[] { SemanticEdit(SemanticEditKind.Update, c => c.GetMember("C.Bar")) });
        }

        [Fact]
        public void Method_MultilineBreakpointSpans()
        {
            var src1 = @"
class C
{
    void F()
    {
        var x =
1;
    }
}
";
            var src2 = @"
class C
{
    void F()
    {
        var x =

1;
    }
}";
            // We need to recompile the method since an active statement span [|var x = 1;|]
            // needs to be updated but can't be by a line update.
            var edits = GetTopEdits(src1, src2);
            edits.VerifyLineEdits(
                Array.Empty<SequencePointUpdates>(),
                semanticEdits: new[] { SemanticEdit(SemanticEditKind.Update, c => c.GetMember("C.F")) });
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
                new[] { new SourceLineUpdate(4, 6) });
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
                new[] { new SourceLineUpdate(4, 5) });
        }

        [Fact]
        public void Method_LineChange3()
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
                new[] { new SourceLineUpdate(5, 6) });
        }

        [Fact]
        public void Method_LineChange4()
        {
            var src1 = @"
class C
{
    static int X() => 1;

    static int Y() => 1;
}
";
            var src2 = @"
class C
{

    static int X() => 1;
    static int Y() => 1;
}";
            var edits = GetTopEdits(src1, src2);
            edits.VerifyLineEdits(
                new[]
                {
                    new SourceLineUpdate(3, 4),
                    new SourceLineUpdate(4, 4)
                });
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
                Array.Empty<SequencePointUpdates>(),
                semanticEdits: new[] { SemanticEdit(SemanticEditKind.Update, c => c.GetMember("C.Bar")) });
        }

        [Fact]
        public void Method_PartialBodyLineUpdate1()
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
                new[] { new SourceLineUpdate(6, 5) });
        }

        [Fact]
        public void Method_PartialBodyLineUpdate2()
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
                new[] { new SourceLineUpdate(5, 4) });
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
                Array.Empty<SequencePointUpdates>(),
                semanticEdits: new[] { SemanticEdit(SemanticEditKind.Update, c => c.GetMember("C.Bar")) });

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
                Array.Empty<SequencePointUpdates>(),
                semanticEdits: new[] { SemanticEdit(SemanticEditKind.Update, c => c.GetMember("C.Bar")) });
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
                new[] { new SourceLineUpdate(6, 5) });
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
                Array.Empty<SequencePointUpdates>(),
                diagnostics: new[] { Diagnostic(RudeEditKind.UpdatingGenericNotSupportedByRuntime, "\r\n        /*edit*/", FeaturesResources.method) },
                capabilities: EditAndContinueCapabilities.Baseline);

            edits.VerifyLineEdits(
                Array.Empty<SequencePointUpdates>(),
                semanticEdits: new[] { SemanticEdit(SemanticEditKind.Update, c => c.GetMember("C.Bar")) },
                capabilities: EditAndContinueCapabilities.GenericUpdateMethod);
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
                Array.Empty<SequencePointUpdates>(),
                diagnostics: new[] { Diagnostic(RudeEditKind.UpdatingGenericNotSupportedByRuntime, "\r\n        ", FeaturesResources.method) },
                capabilities: EditAndContinueCapabilities.Baseline);

            edits.VerifyLineEdits(
                Array.Empty<SequencePointUpdates>(),
                semanticEdits: new[] { SemanticEdit(SemanticEditKind.Update, c => c.GetMember("C.Bar")) },
                capabilities: EditAndContinueCapabilities.GenericUpdateMethod);
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
                Array.Empty<SequencePointUpdates>(),
                semanticEdits: new[] { SemanticEdit(SemanticEditKind.Update, c => c.GetMember("C.Bar"), preserveLocalVariables: true) });
        }

        [Fact]
        public void Lambda_Recompile()
        {
            var src1 = @"
class C
{
    void F()
    {
        var x = new System.Func<int>(
            () => 1

        );
    }
}";
            var src2 = @"
class C
{
    void F()
    {
        var x = new System.Func<int>(
            () =>
                  1
        );
    }
}";

            var edits = GetTopEdits(src1, src2);

            edits.VerifyLineEdits(
                Array.Empty<SequencePointUpdates>(),
                semanticEdits: new[] { SemanticEdit(SemanticEditKind.Update, c => c.GetMember("C.F"), preserveLocalVariables: true) });
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
                new[]
                {
                    new SourceLineUpdate(3, 7),
                    new SourceLineUpdate(6, 6),
                    new SourceLineUpdate(7, 3)
                });
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
                new[] { new SourceLineUpdate(4, 5) });
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
                new[] { new SourceLineUpdate(5, 6) });
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
                new[] { new SourceLineUpdate(5, 6) });
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
                new[] { new SourceLineUpdate(5, 6) });
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
                new[] { new SourceLineUpdate(6, 8) });
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
                new[] { new SourceLineUpdate(5, 6) });
        }

        [Fact]
        public void Constructor_ExpressionBodiedWithBase_LineChange2()
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
                new SourceLineUpdate[] { new(5, 6) });
        }

        [Fact]
        public void Constructor_ExpressionBodiedWithBase_Recompile1()
        {
            var src1 = @"
class C
{
    int _a;
    public C(int a)
      : base() => _a 
                     = a;
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
                Array.Empty<SequencePointUpdates>(),
                semanticEdits: new[] { SemanticEdit(SemanticEditKind.Update, c => c.GetMember<INamedTypeSymbol>("C").InstanceConstructors.Single(), preserveLocalVariables: true) });
        }

        [Fact]
        public void Constructor_PartialBodyLineChange1()
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
                new SourceLineUpdate[] { new(5, 6) });
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
                Array.Empty<SequencePointUpdates>(),
                semanticEdits: new[] { SemanticEdit(SemanticEditKind.Update, c => c.GetMember<INamedTypeSymbol>("C").InstanceConstructors.Single(), preserveLocalVariables: true) });
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
                Array.Empty<SequencePointUpdates>(),
                diagnostics: new[] { Diagnostic(RudeEditKind.UpdatingGenericNotSupportedByRuntime, "public C(int a)", GetResource("constructor")) },
                capabilities: EditAndContinueCapabilities.Baseline);

            edits.VerifyLineEdits(
                Array.Empty<SequencePointUpdates>(),
                semanticEdits: new[] { SemanticEdit(SemanticEditKind.Update, c => c.GetMember<INamedTypeSymbol>("C").InstanceConstructors.Single(), preserveLocalVariables: true) },
                capabilities: EditAndContinueCapabilities.GenericUpdateMethod);
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
                new[] { new SourceLineUpdate(5, 4) });
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
                new[] { new SourceLineUpdate(3, 4) });
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
                new[] { new SourceLineUpdate(3, 4) });
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
                Array.Empty<SequencePointUpdates>());
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
                Array.Empty<SequencePointUpdates>());
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
                Array.Empty<SequencePointUpdates>(),
                diagnostics: new[] { Diagnostic(RudeEditKind.Move, "static int Bar = 2", FeaturesResources.field) });
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
                new[] { new SourceLineUpdate(3, 6) });
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
                Array.Empty<SequencePointUpdates>(),
                semanticEdits: new[]
                {
                    SemanticEdit(SemanticEditKind.Update, c => c.GetMember<INamedTypeSymbol>("C").InstanceConstructors.Single(), preserveLocalVariables: true)
                });
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
                new SourceLineUpdate[] { new SourceLineUpdate(3, 4) });
        }

        [Fact]
        public void Field_LineChange_Reloadable()
        {
            var src1 = ReloadableAttributeSrc + @"
[CreateNewOnMetadataUpdate]
class C
{
    int Goo = 1, Bar = 2;
}
";
            var src2 = ReloadableAttributeSrc + @"
[CreateNewOnMetadataUpdate]
class C
{
    int Goo = 1,
                 Bar = 2;
}";
            var edits = GetTopEdits(src1, src2);
            edits.VerifyLineEdits(
                Array.Empty<SequencePointUpdates>(),
                semanticEdits: new[]
                {
                    SemanticEdit(SemanticEditKind.Replace, c => c.GetMember("C"))
                },
                capabilities: EditAndContinueCapabilities.NewTypeDefinition);
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
                Array.Empty<SequencePointUpdates>(),
                semanticEdits: new[]
                {
                    SemanticEdit(SemanticEditKind.Update, c => c.GetMember<INamedTypeSymbol>("C").StaticConstructors.Single(), preserveLocalVariables: true)
                });
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
                Array.Empty<SequencePointUpdates>(),
                semanticEdits: new[]
                {
                    SemanticEdit(SemanticEditKind.Update, c => c.GetMember<INamedTypeSymbol>("C").StaticConstructors.Single(), preserveLocalVariables: true)
                });
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
                Array.Empty<SequencePointUpdates>(),
                semanticEdits: new[]
                {
                    SemanticEdit(SemanticEditKind.Update, c => c.GetMember<INamedTypeSymbol>("C").StaticConstructors.Single(), preserveLocalVariables: true)
                });
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
                Array.Empty<SequencePointUpdates>(),
                semanticEdits: new[]
                {
                    SemanticEdit(SemanticEditKind.Update, c => c.GetMember<INamedTypeSymbol>("C").StaticConstructors.Single(), preserveLocalVariables: true)
                });
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
                Array.Empty<SequencePointUpdates>(),
                semanticEdits: new[]
                {
                    SemanticEdit(SemanticEditKind.Update, c => c.GetMember<INamedTypeSymbol>("C").StaticConstructors.Single(), preserveLocalVariables: true)
                });
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
                Array.Empty<SequencePointUpdates>(),
                semanticEdits: new[]
                {
                    SemanticEdit(SemanticEditKind.Update, c => c.GetMember<INamedTypeSymbol>("C").StaticConstructors.Single(), preserveLocalVariables: true)
                });
        }

        [Fact]
        public void Field_RudeRecompile1()
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
                Array.Empty<SequencePointUpdates>(),
                diagnostics: new[]
                {
                    Diagnostic(RudeEditKind.UpdatingGenericNotSupportedByRuntime, "class C<T>", GetResource("static constructor", "C()"))
                },
                capabilities: EditAndContinueCapabilities.Baseline);

            edits.VerifyLineEdits(
                Array.Empty<SequencePointUpdates>(),
                semanticEdits: new[]
                {
                    SemanticEdit(SemanticEditKind.Update, c => c.GetMember<INamedTypeSymbol>("C").StaticConstructors.Single(), preserveLocalVariables: true)
                },
                capabilities: EditAndContinueCapabilities.GenericUpdateMethod);
        }

        [Fact]
        public void Field_Generic_Reloadable()
        {
            var src1 = ReloadableAttributeSrc + @"
[CreateNewOnMetadataUpdate]
class C<T>
{
    static int Goo = 1 + 1;
}
";
            var src2 = ReloadableAttributeSrc + @"
[CreateNewOnMetadataUpdate]
class C<T>
{
    static int Goo = 1 +  1;
}";
            var edits = GetTopEdits(src1, src2);
            edits.VerifyLineEdits(
                Array.Empty<SequencePointUpdates>(),
                semanticEdits: new[]
                {
                    SemanticEdit(SemanticEditKind.Replace, c => c.GetMember("C"))
                },
                capabilities: EditAndContinueCapabilities.NewTypeDefinition);
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
                Array.Empty<SequencePointUpdates>(),
                semanticEdits: new[] { SemanticEdit(SemanticEditKind.Update, c => c.GetMember<IPropertySymbol>("C.P").GetMethod) });
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
            edits.VerifyLineEdits(new[] { new SourceLineUpdate(3, 4) });
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
                new[] { new SourceLineUpdate(3, 4) });
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
                new[] { new SourceLineUpdate(3, 4) });
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
                new[] { new SourceLineUpdate(3, 4) });
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
                new[] { new SourceLineUpdate(3, 4) });
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
                new[] { new SourceLineUpdate(3, 4) });
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
                new[] { new SourceLineUpdate(3, 4) });
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
                Array.Empty<SequencePointUpdates>(),
                semanticEdits: new[] { SemanticEdit(SemanticEditKind.Update, c => c.GetMember<INamedTypeSymbol>("C").InstanceConstructors.Single(), preserveLocalVariables: true) });
        }

        #endregion

        #region Properties

        [Fact]
        public void Indexer1()
        {
            var src1 = @"
class C
{
    int this[int a] { get { return 1; } }
}
";
            var src2 = @"
class C
{
    int this[int a] { get { return 
                                   1; } }
}";
            var edits = GetTopEdits(src1, src2);
            edits.VerifyLineEdits(
                Array.Empty<SequencePointUpdates>(),
                semanticEdits: new[] { SemanticEdit(SemanticEditKind.Update, c => c.GetMember<IPropertySymbol>("C.this[]").GetMethod) });
        }

        [Fact]
        public void Indexer2()
        {
            var src1 = @"
class C
{
    int this[int a] { get { return 1; } }
}
";
            var src2 = @"
class C
{
    int this[int a] { get 
                          { return 1; } }
}";
            var edits = GetTopEdits(src1, src2);
            edits.VerifyLineEdits(new[] { new SourceLineUpdate(3, 4) });
        }

        [Fact]
        public void Indexer3()
        {
            var src1 = @"
class C
{
    int this[int a] { get { return 1; } set { } }
}
";
            var src2 = @"
class C
{
    
    int this[int a] { get { return 1; } set { } }
}";
            var edits = GetTopEdits(src1, src2);
            edits.VerifyLineEdits(new[] { new SourceLineUpdate(3, 4) });
        }

        [Fact]
        public void Indexer_ExpressionBody1()
        {
            var src1 = @"
class C
{
    int this[int a] => 1;
}
";
            var src2 = @"
class C
{
    int this[int a] => 
                       1;
}";
            var edits = GetTopEdits(src1, src2);
            edits.VerifyLineEdits(
                new[] { new SourceLineUpdate(3, 4) });
        }

        [Fact]
        public void Indexer_GetterExpressionBody1()
        {
            var src1 = @"
class C
{
    int this[int a] { get => 1; }
}
";
            var src2 = @"
class C
{
    int this[int a] { get => 
                             1; }
}";
            var edits = GetTopEdits(src1, src2);
            edits.VerifyLineEdits(
                new[] { new SourceLineUpdate(3, 4) });
        }

        [Fact]
        public void Indexer_SetterExpressionBody1()
        {
            var src1 = @"
class C
{
    int this[int a] { set => F(); }
}
";
            var src2 = @"
class C
{
    int this[int a] { set => 
                             F(); }
}";
            var edits = GetTopEdits(src1, src2);
            edits.VerifyLineEdits(
                new[] { new SourceLineUpdate(3, 4) });
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
                new[] { new SourceLineUpdate(3, 4) });
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
                semanticEdits: new[] { SemanticEdit(SemanticEditKind.Update, c => c.GetMember<IEventSymbol>("C.E").AddMethod) });
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
                Array.Empty<SequencePointUpdates>(),
                semanticEdits: new[] { SemanticEdit(SemanticEditKind.Update, c => c.GetMember<IEventSymbol>("C.E").RemoveMethod) });
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
                new[] { new SourceLineUpdate(4, 3) });
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
            // we can only apply one delta per line, but that would affect add and remove differently, so need to recompile
            var edits = GetTopEdits(src1, src2);
            edits.VerifyLineEdits(
                Array.Empty<SequencePointUpdates>(),
                semanticEdits: new[] { SemanticEdit(SemanticEditKind.Update, c => c.GetMember<IEventSymbol>("C.E").RemoveMethod) });
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/53263")]
        public void Event_ExpressionBody_MultipleBodiesOnTheSameLine1()
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
                new[] { new SourceLineUpdate(3, 4) },
                semanticEdits: new[] { SemanticEdit(SemanticEditKind.Update, c => c.GetMember<IEventSymbol>("C.E").RemoveMethod) });
        }

        [Fact]
        public void Event_ExpressionBody()
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
                new[] { new SourceLineUpdate(4, 3), new SourceLineUpdate(5, 3) });
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
                new[]
                {
                    new SourceLineUpdate(3, 9),
                    new SourceLineUpdate(5, 5),
                    new SourceLineUpdate(9, 3)
                });
        }

        #endregion

        #region Line Mappings

        [Fact]
        public void LineMapping_ChangeLineNumber_WithinMethod_NoSequencePointImpact()
        {
            var src1 = @"
class C
{
    static void F()
    {
        G(
#line 2 ""c""
            123
#line default
        );
    }
}";
            var src2 = @"
class C
{
    static void F()
    {
        G(
#line 3 ""c""
            123
#line default
        );
    }
}";
            var edits = GetTopEdits(src1, src2);

            // Line deltas can't be applied on the whole breakpoint span hence recompilation.
            edits.VerifyLineEdits(
                Array.Empty<SequencePointUpdates>(),
                semanticEdits: new[] { SemanticEdit(SemanticEditKind.Update, c => c.GetMember("C.F")) });
        }

        /// <summary>
        /// Validates that changes in #line directives produce semantic updates of the containing method.
        /// </summary>
        [Fact]
        public void LineMapping_ChangeLineNumber_OutsideOfMethod()
        {
            var src1 = @"
#line 1 ""a""
class C
{
    int x = 1;
    static int y = 1;
    void F1() { }
    void F2() { }
}
class D
{
    public D() {}

#line 5 ""a""
    void F3() {}

#line 6 ""a""
    void F4() {}
}";
            var src2 = @"
#line 11 ""a""
class C
{
    int x = 1;
    static int y = 1;
    void F1() { }
    void F2() { }
}
class D
{
    public D() {}

#line 5 ""a""
    void F3() {}
    void F4() {}
}
";
            var edits = GetTopEdits(src1, src2);

            edits.VerifyLineEdits(
                new SequencePointUpdates[]
                {
                    new("a", ImmutableArray.Create(
                        new SourceLineUpdate(2, 12), // x, y, F1, F2
                        new SourceLineUpdate(6, 6), // lines between F2 and D ctor
                        new SourceLineUpdate(9, 19))) // D ctor
                },
                semanticEdits: new[]
                {
                    SemanticEdit(SemanticEditKind.Update, c => c.GetMember("D.F3")), // overlaps with "void F1() { }"
                    SemanticEdit(SemanticEditKind.Update, c => c.GetMember("D.F4")), // overlaps with "void F2() { }"
                });
        }

        [Fact]
        public void LineMapping_LineDirectivesAndWhitespace()
        {
            var src1 = @"
class C
{
#line 5 ""a""
#line 6 ""a""



    static void F() { } // line 9
}";
            var src2 = @"
class C
{
#line 9 ""a""
    static void F() { }
}";
            var edits = GetTopEdits(src1, src2);

            edits.VerifySemantics();
        }

        [Fact]
        public void LineMapping_MultipleFiles()
        {
            var src1 = @"
class C
{
    static void F()
    {
#line 1 ""a""
        A();
#line 1 ""b""
        B();
#line default
    }
}";
            var src2 = @"
class C
{
    static void F()
    {
#line 2 ""a""
        A();
#line 2 ""b""
        B();
#line default
    }
}";
            var edits = GetTopEdits(src1, src2);

            edits.VerifyLineEdits(
                new SequencePointUpdates[]
                {
                    new("a", ImmutableArray.Create(new SourceLineUpdate(0, 1))),
                    new("b", ImmutableArray.Create(new SourceLineUpdate(0, 1))),
                });
        }

        [Fact]
        public void LineMapping_FileChange_Recompile()
        {
            var src1 = @"
class C
{
    static void F()
    {
        A();
#line 1 ""a""
        B();
#line 3 ""a""
        C();
    }


    int x = 1;
}";
            var src2 = @"
class C
{
    static void F()
    {
        A();
#line 1 ""b""
        B();
#line 2 ""a""
        C();
    }

    int x = 1;
}";
            var edits = GetTopEdits(src1, src2);

            edits.VerifyLineEdits(
                new SequencePointUpdates[]
                {
                    new("a", ImmutableArray.Create(new SourceLineUpdate(6, 4))),
                },
                semanticEdits: new[]
                {
                    SemanticEdit(SemanticEditKind.Update, c => c.GetMember("C.F"))
                });
        }

        [Fact]
        public void LineMapping_FileChange_RudeEdit()
        {
            var src1 = @"
#line 1 ""a""
class C { static void F<T>() { } }
";
            var src2 = @"
#line 1 ""b""
class C { static void F<T>() { } }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifyLineEdits(
                 Array.Empty<SequencePointUpdates>(),
                 diagnostics: new[]
                 {
                     Diagnostic(RudeEditKind.UpdatingGenericNotSupportedByRuntime, "{", GetResource("method"))
                 },
                 capabilities: EditAndContinueCapabilities.Baseline);

            edits.VerifyLineEdits(
                 Array.Empty<SequencePointUpdates>(),
                 semanticEdits: new[]
                 {
                     SemanticEdit(SemanticEditKind.Update, c => c.GetMember("C.F"))
                 },
                 capabilities: EditAndContinueCapabilities.GenericUpdateMethod);
        }

        #endregion
    }
}
