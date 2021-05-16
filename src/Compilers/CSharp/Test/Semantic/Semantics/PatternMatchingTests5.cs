// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    public class PatternMatchingTests5 : PatternMatchingTestBase
    {
        [Fact]
        public void ExtendedPropertyPatterns_01()
        {
            var program = @"
using System;
class C
{
    public C Prop1 { get; set; }
    public C Prop2 { get; set; }
    public C Prop3 { get; set; }
    
    static bool Test1(C o) => o is { Prop1.Prop2.Prop3: null };
    static bool Test2(S o) => o is { Prop1.Prop2.Prop3: null };
    static bool Test3(S? o) => o is { Prop1.Prop2.Prop3: null };
    static bool Test4(S0 o) => o is { Prop1.Prop2.Prop3: 420 };

    public static void Main()
    {        
        Console.WriteLine(Test1(new() { Prop1 = new() { Prop2 = new() { Prop3 = null }}}));
        Console.WriteLine(Test2(new() { Prop1 = new() { Prop2 = new() { Prop3 = null }}}));
        Console.WriteLine(Test3(new() { Prop1 = new() { Prop2 = new() { Prop3 = null }}}));

        Console.WriteLine(Test1(new() { Prop1 = new() { Prop2 = null }}));
        Console.WriteLine(Test2(new() { Prop1 = new() { Prop2 = null }}));
        Console.WriteLine(Test3(new() { Prop1 = new() { Prop2 = null }}));

        Console.WriteLine(Test1(new() { Prop1 = null }));
        Console.WriteLine(Test2(new() { Prop1 = null }));
        Console.WriteLine(Test3(new() { Prop1 = null }));

        Console.WriteLine(Test1(default));
        Console.WriteLine(Test2(default));
        Console.WriteLine(Test3(default));

        Console.WriteLine(Test4(new() { Prop1 = new() { Prop2 = new() { Prop3 = 421 }}}));
        Console.WriteLine(Test4(new() { Prop1 = new() { Prop2 = new() { Prop3 = 420 }}}));
    }
}
struct S { public A? Prop1; }
struct A { public B? Prop2; }
struct B { public int? Prop3; }

struct S0 { public A0 Prop1; }
struct A0 { public B0 Prop2; }
struct B0 { public int Prop3; }
";
            var compilation = CreateCompilation(program, parseOptions: TestOptions.RegularWithExtendedPropertyPatterns, options: TestOptions.ReleaseExe);
            compilation.VerifyDiagnostics();
            var expectedOutput = @"
True
True
True
False
False
False
False
False
False
False
False
False
False
True
";
            var verifier = CompileAndVerify(compilation, expectedOutput: expectedOutput);
            verifier.VerifyIL("C.Test1", @"
{
  // Code size       35 (0x23)
  .maxstack  2
  .locals init (C V_0,
                C V_1)
  IL_0000:  ldarg.0
  IL_0001:  brfalse.s  IL_0021
  IL_0003:  ldarg.0
  IL_0004:  callvirt   ""C C.Prop1.get""
  IL_0009:  stloc.0
  IL_000a:  ldloc.0
  IL_000b:  brfalse.s  IL_0021
  IL_000d:  ldloc.0
  IL_000e:  callvirt   ""C C.Prop2.get""
  IL_0013:  stloc.1
  IL_0014:  ldloc.1
  IL_0015:  brfalse.s  IL_0021
  IL_0017:  ldloc.1
  IL_0018:  callvirt   ""C C.Prop3.get""
  IL_001d:  ldnull
  IL_001e:  ceq
  IL_0020:  ret
  IL_0021:  ldc.i4.0
  IL_0022:  ret
}");
            verifier.VerifyIL("C.Test2", @"
{
  // Code size       64 (0x40)
  .maxstack  2
  .locals init (A? V_0,
                B? V_1,
                int? V_2)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""A? S.Prop1""
  IL_0006:  stloc.0
  IL_0007:  ldloca.s   V_0
  IL_0009:  call       ""bool A?.HasValue.get""
  IL_000e:  brfalse.s  IL_003e
  IL_0010:  ldloca.s   V_0
  IL_0012:  call       ""A A?.GetValueOrDefault()""
  IL_0017:  ldfld      ""B? A.Prop2""
  IL_001c:  stloc.1
  IL_001d:  ldloca.s   V_1
  IL_001f:  call       ""bool B?.HasValue.get""
  IL_0024:  brfalse.s  IL_003e
  IL_0026:  ldloca.s   V_1
  IL_0028:  call       ""B B?.GetValueOrDefault()""
  IL_002d:  ldfld      ""int? B.Prop3""
  IL_0032:  stloc.2
  IL_0033:  ldloca.s   V_2
  IL_0035:  call       ""bool int?.HasValue.get""
  IL_003a:  ldc.i4.0
  IL_003b:  ceq
  IL_003d:  ret
  IL_003e:  ldc.i4.0
  IL_003f:  ret
}");
            verifier.VerifyIL("C.Test4", @"
{
  // Code size       24 (0x18)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""A0 S0.Prop1""
  IL_0006:  ldfld      ""B0 A0.Prop2""
  IL_000b:  ldfld      ""int B0.Prop3""
  IL_0010:  ldc.i4     0x1a4
  IL_0015:  ceq
  IL_0017:  ret
}");
        }

        [Fact]
        public void ExtendedPropertyPatterns_02()
        {
            var program = @"
class C
{
    public C Prop1 { get; set; }
    public C Prop2 { get; set; }

    public static void Main()
    {        
        _ = new C() is { Prop1: null } and { Prop1.Prop2: null };
        _ = new C() is { Prop1: null, Prop1.Prop2: null };
    }
}
";
            var compilation = CreateCompilation(program, parseOptions: TestOptions.RegularWithExtendedPropertyPatterns, options: TestOptions.ReleaseExe);
            compilation.VerifyDiagnostics(
                // (9,13): error CS8518: An expression of type 'C' can never match the provided pattern.
                //         _ = new C() is { Prop1: null } and { Prop1.Prop2: null };
                Diagnostic(ErrorCode.ERR_IsPatternImpossible, "new C() is { Prop1: null } and { Prop1.Prop2: null }").WithArguments("C").WithLocation(9, 13),
                // (10,13): error CS8518: An expression of type 'C' can never match the provided pattern.
                //         _ = new C() is { Prop1: null, Prop1.Prop2: null };
                Diagnostic(ErrorCode.ERR_IsPatternImpossible, "new C() is { Prop1: null, Prop1.Prop2: null }").WithArguments("C").WithLocation(10, 13)
                );
        }

        [Fact]
        public void ExtendedPropertyPatterns_03()
        {
            var program = @"
using System;
class C
{
    C _prop1;
    C _prop2;

    C Prop1
    {
        get { Console.WriteLine(nameof(Prop1)); return _prop1; }
        set => _prop1 = value;
    }
    C Prop2
    {
        get { Console.WriteLine(nameof(Prop2)); return _prop2; }
        set => _prop2 = value;
    }

    public static void Main()
    {  
        Test(null);
        Test(new());
        Test(new() { Prop1 = new() });
        Test(new() { Prop1 = new() { Prop2 = new() } });
    }
    static void Test(C o)
    {
        Console.WriteLine(nameof(Test));
        var result = o switch
        {
            {Prop1: null} => 1,
            {Prop1.Prop2: null} => 2,
            _ => -1,
        };
        Console.WriteLine(result);
    }
}
";
            var compilation = CreateCompilation(program, parseOptions: TestOptions.RegularWithExtendedPropertyPatterns, options: TestOptions.ReleaseExe);
            compilation.VerifyDiagnostics();
            var expectedOutput = @"
Test
-1
Test
Prop1
1
Test
Prop1
Prop2
2
Test
Prop1
Prop2
-1";
            var verifier = CompileAndVerify(compilation, expectedOutput: expectedOutput);
            verifier.VerifyIL("C.Test", @"
{
  // Code size       50 (0x32)
  .maxstack  1
  .locals init (int V_0,
                C V_1)
  IL_0000:  ldstr      ""Test""
  IL_0005:  call       ""void System.Console.WriteLine(string)""
  IL_000a:  ldarg.0
  IL_000b:  brfalse.s  IL_0029
  IL_000d:  ldarg.0
  IL_000e:  callvirt   ""C C.Prop1.get""
  IL_0013:  stloc.1
  IL_0014:  ldloc.1
  IL_0015:  brfalse.s  IL_0021
  IL_0017:  ldloc.1
  IL_0018:  callvirt   ""C C.Prop2.get""
  IL_001d:  brfalse.s  IL_0025
  IL_001f:  br.s       IL_0029
  IL_0021:  ldc.i4.1
  IL_0022:  stloc.0
  IL_0023:  br.s       IL_002b
  IL_0025:  ldc.i4.2
  IL_0026:  stloc.0
  IL_0027:  br.s       IL_002b
  IL_0029:  ldc.i4.m1
  IL_002a:  stloc.0
  IL_002b:  ldloc.0
  IL_002c:  call       ""void System.Console.WriteLine(int)""
  IL_0031:  ret
}");
        }

        [Fact]
        public void ExtendedPropertyPatterns_04()
        {
            var program = @"
class C
{
    public static void Main()
    {        
        _ = new C() is { Prop1<int>.Prop2: {} };
        _ = new C() is { Prop1->Prop2: {} };
        _ = new C() is { Prop1!.Prop2: {} };
        _ = new C() is { Prop1?.Prop2: {} };
        _ = new C() is { Prop1[0]: {} };
        _ = new C() is { 1: {} };
    }
}
";
            var compilation = CreateCompilation(program, parseOptions: TestOptions.RegularWithExtendedPropertyPatterns, options: TestOptions.ReleaseExe);
            compilation.VerifyDiagnostics(
                    // (6,26): error CS9000: Identifier or a simple member access expected.
                    //         _ = new C() is { Prop1<int>.Prop2: {} };
                    Diagnostic(ErrorCode.ERR_InvalidNameInSubpattern, "Prop1<int>").WithLocation(6, 26),
                    // (7,26): error CS9000: Identifier or a simple member access expected.
                    //         _ = new C() is { Prop1->Prop2: {} };
                    Diagnostic(ErrorCode.ERR_InvalidNameInSubpattern, "Prop1->Prop2").WithLocation(7, 26),
                    // (8,26): error CS9000: Identifier or a simple member access expected.
                    //         _ = new C() is { Prop1!.Prop2: {} };
                    Diagnostic(ErrorCode.ERR_InvalidNameInSubpattern, "Prop1!").WithLocation(8, 26),
                    // (9,26): error CS9000: Identifier or a simple member access expected.
                    //         _ = new C() is { Prop1?.Prop2: {} };
                    Diagnostic(ErrorCode.ERR_InvalidNameInSubpattern, "Prop1?.Prop2").WithLocation(9, 26),
                    // (10,26): error CS8503: A property subpattern requires a reference to the property or field to be matched, e.g. '{ Name: Prop1[0] }'
                    //         _ = new C() is { Prop1[0]: {} };
                    Diagnostic(ErrorCode.ERR_PropertyPatternNameMissing, "Prop1[0]").WithArguments("Prop1[0]").WithLocation(10, 26),
                    // (10,26): error CS0246: The type or namespace name 'Prop1' could not be found (are you missing a using directive or an assembly reference?)
                    //         _ = new C() is { Prop1[0]: {} };
                    Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "Prop1").WithArguments("Prop1").WithLocation(10, 26),
                    // (10,31): error CS0270: Array size cannot be specified in a variable declaration (try initializing with a 'new' expression)
                    //         _ = new C() is { Prop1[0]: {} };
                    Diagnostic(ErrorCode.ERR_ArraySizeInDeclaration, "[0]").WithLocation(10, 31),
                    // (10,34): error CS1003: Syntax error, ',' expected
                    //         _ = new C() is { Prop1[0]: {} };
                    Diagnostic(ErrorCode.ERR_SyntaxError, ":").WithArguments(",", ":").WithLocation(10, 34),
                    // (10,36): error CS1003: Syntax error, ',' expected
                    //         _ = new C() is { Prop1[0]: {} };
                    Diagnostic(ErrorCode.ERR_SyntaxError, "{").WithArguments(",", "{").WithLocation(10, 36),
                    // (11,26): error CS9000: Identifier or a simple member access expected.
                    //         _ = new C() is { 1: {} };
                    Diagnostic(ErrorCode.ERR_InvalidNameInSubpattern, "1").WithLocation(11, 26));
        }

        [Fact]
        public void ExtendedPropertyPatterns_05()
        {
            var program = @"
class C
{
    C Prop1, Prop2;
    public static void Main()
    {        
        _ = new C() is { Prop1.Prop2: {} };
        _ = new C() is { Prop1?.Prop2: {} };
        _ = new C() is { Missing: null, Prop1.Prop2: {} };
    }
}
";
            var compilation = CreateCompilation(program, parseOptions: TestOptions.Regular9, options: TestOptions.ReleaseExe);
            // PROTOTYPE(extended-property-patterns) False warning: https://github.com/dotnet/roslyn/issues/52956
            compilation.VerifyDiagnostics(
                    // (4,7): warning CS0169: The field 'C.Prop1' is never used
                    //     C Prop1, Prop2;
                    Diagnostic(ErrorCode.WRN_UnreferencedField, "Prop1").WithArguments("C.Prop1").WithLocation(4, 7),
                    // (4,14): warning CS0169: The field 'C.Prop2' is never used
                    //     C Prop1, Prop2;
                    Diagnostic(ErrorCode.WRN_UnreferencedField, "Prop2").WithArguments("C.Prop2").WithLocation(4, 14),
                    // (7,26): error CS8652: The feature 'extended property patterns' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                    //         _ = new C() is { Prop1.Prop2: {} };
                    Diagnostic(ErrorCode.ERR_FeatureInPreview, "Prop1.Prop2").WithArguments("extended property patterns").WithLocation(7, 26),
                    // (8,26): error CS8652: The feature 'extended property patterns' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                    //         _ = new C() is { Prop1?.Prop2: {} };
                    Diagnostic(ErrorCode.ERR_FeatureInPreview, "Prop1?.Prop2").WithArguments("extended property patterns").WithLocation(8, 26),
                    // (8,26): error CS9000: Identifier or a simple member access expected.
                    //         _ = new C() is { Prop1?.Prop2: {} };
                    Diagnostic(ErrorCode.ERR_InvalidNameInSubpattern, "Prop1?.Prop2").WithLocation(8, 26),
                    // (9,26): error CS0117: 'C' does not contain a definition for 'Missing'
                    //         _ = new C() is { Missing: null, Prop1.Prop2: {} };
                    Diagnostic(ErrorCode.ERR_NoSuchMember, "Missing").WithArguments("C", "Missing").WithLocation(9, 26),
                    // (9,41): error CS8652: The feature 'extended property patterns' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                    //         _ = new C() is { Missing: null, Prop1.Prop2: {} };
                    Diagnostic(ErrorCode.ERR_FeatureInPreview, "Prop1.Prop2").WithArguments("extended property patterns").WithLocation(9, 41));
        }

        [Fact]
        public void ExtendedPropertyPatterns_SymbolInfo_01()
        {
            var source =
@"
using System;
class Program
{
    public static void Main()
    {
        P p = new P();
        Console.WriteLine(p is { X.Y: {}, Y.X: {} });
    }
}
class P
{
    public P X { get; }
    public P Y;
}
";
            var compilation = CreatePatternCompilation(source);
            compilation.VerifyDiagnostics(
                    // (14,14): warning CS0649: Field 'P.Y' is never assigned to, and will always have its default value null
                    //     public P Y;
                    Diagnostic(ErrorCode.WRN_UnassignedInternalField, "Y").WithArguments("P.Y", "null").WithLocation(14, 14)
                );
            var tree = compilation.SyntaxTrees[0];
            var model = compilation.GetSemanticModel(tree);

            var subpatterns = tree.GetRoot().DescendantNodes().OfType<SubpatternSyntax>().ToArray();
            Assert.Equal(2, subpatterns.Length);

            AssertEmpty(model.GetSymbolInfo(subpatterns[0]));
            AssertEmpty(model.GetSymbolInfo(subpatterns[0].ExpressionColon));
            var xy = subpatterns[0].ExpressionColon.Expression;
            var xySymbol = model.GetSymbolInfo(xy);
            Assert.Equal(CandidateReason.None, xySymbol.CandidateReason);
            Assert.Equal("P P.Y", xySymbol.Symbol.ToTestDisplayString());

            var x = ((MemberAccessExpressionSyntax)subpatterns[0].ExpressionColon.Expression).Expression;
            var xSymbol = model.GetSymbolInfo(x);
            Assert.Equal(CandidateReason.None, xSymbol.CandidateReason);
            Assert.Equal("P P.X { get; }", xSymbol.Symbol.ToTestDisplayString());

            var yName = ((MemberAccessExpressionSyntax)subpatterns[0].ExpressionColon.Expression).Name;
            var yNameSymbol = model.GetSymbolInfo(yName);
            Assert.Equal(CandidateReason.None, yNameSymbol.CandidateReason);
            Assert.Equal("P P.Y", yNameSymbol.Symbol.ToTestDisplayString());

            AssertEmpty(model.GetSymbolInfo(subpatterns[1]));
            AssertEmpty(model.GetSymbolInfo(subpatterns[1].ExpressionColon));
            var yx = subpatterns[1].ExpressionColon.Expression;
            var yxSymbol = model.GetSymbolInfo(yx);
            Assert.NotEqual(default, yxSymbol);
            Assert.Equal(CandidateReason.None, yxSymbol.CandidateReason);
            Assert.Equal("P P.X { get; }", yxSymbol.Symbol.ToTestDisplayString());

            var y = ((MemberAccessExpressionSyntax)subpatterns[1].ExpressionColon.Expression).Expression;
            var ySymbol = model.GetSymbolInfo(y);
            Assert.Equal(CandidateReason.None, ySymbol.CandidateReason);
            Assert.Equal("P P.Y", ySymbol.Symbol.ToTestDisplayString());

            var xName = ((MemberAccessExpressionSyntax)subpatterns[1].ExpressionColon.Expression).Name;
            var xNameSymbol = model.GetSymbolInfo(xName);
            Assert.Equal(CandidateReason.None, xNameSymbol.CandidateReason);
            Assert.Equal("P P.X { get; }", xNameSymbol.Symbol.ToTestDisplayString());
        }

        [Fact]
        public void ExtendedPropertyPatterns_SymbolInfo_02()
        {
            var source =
@"
using System;
class Program
{
    public static void Main()
    {
        P p = null;
        Console.WriteLine(p is { X.Y: {}, Y.X: {}, });
    }
}
interface I1
{
    P X { get; }
    P Y { get; }
}
interface I2
{
    P X { get; }
    P Y { get; }
}
interface P : I1, I2
{
    // X and Y inherited ambiguously
}
";
            var compilation = CreatePatternCompilation(source);
            compilation.VerifyDiagnostics(
                    // (8,34): error CS0229: Ambiguity between 'I1.X' and 'I2.X'
                    //         Console.WriteLine(p is { X.Y: {}, Y.X: {}, });
                    Diagnostic(ErrorCode.ERR_AmbigMember, "X").WithArguments("I1.X", "I2.X").WithLocation(8, 34),
                    // (8,43): error CS0229: Ambiguity between 'I1.Y' and 'I2.Y'
                    //         Console.WriteLine(p is { X.Y: {}, Y.X: {}, });
                    Diagnostic(ErrorCode.ERR_AmbigMember, "Y").WithArguments("I1.Y", "I2.Y").WithLocation(8, 43)
                );
            var tree = compilation.SyntaxTrees[0];
            var model = compilation.GetSemanticModel(tree);

            var subpatterns = tree.GetRoot().DescendantNodes().OfType<SubpatternSyntax>().ToArray();
            Assert.Equal(2, subpatterns.Length);

            AssertEmpty(model.GetSymbolInfo(subpatterns[0]));
            AssertEmpty(model.GetSymbolInfo(subpatterns[0].ExpressionColon));
            var x = ((MemberAccessExpressionSyntax)subpatterns[0].ExpressionColon.Expression).Expression;
            var xSymbol = model.GetSymbolInfo(x);
            Assert.Equal(CandidateReason.Ambiguous, xSymbol.CandidateReason);
            Assert.Null(xSymbol.Symbol);
            Assert.Equal(2, xSymbol.CandidateSymbols.Length);
            Assert.Equal("P I1.X { get; }", xSymbol.CandidateSymbols[0].ToTestDisplayString());
            Assert.Equal("P I2.X { get; }", xSymbol.CandidateSymbols[1].ToTestDisplayString());

            AssertEmpty(model.GetSymbolInfo(subpatterns[1]));
            AssertEmpty(model.GetSymbolInfo(subpatterns[1].ExpressionColon));
            var y = ((MemberAccessExpressionSyntax)subpatterns[1].ExpressionColon.Expression).Expression;
            var ySymbol = model.GetSymbolInfo(y);
            Assert.Equal(CandidateReason.Ambiguous, ySymbol.CandidateReason);
            Assert.Null(ySymbol.Symbol);
            Assert.Equal(2, ySymbol.CandidateSymbols.Length);
            Assert.Equal("P I1.Y { get; }", ySymbol.CandidateSymbols[0].ToTestDisplayString());
            Assert.Equal("P I2.Y { get; }", ySymbol.CandidateSymbols[1].ToTestDisplayString());
        }

        [Fact]
        public void ExtendedPropertyPatterns_SymbolInfo_03()
        {
            var source =
@"
using System;
class Program
{
    public static void Main()
    {
        P p = null;
        Console.WriteLine(p is { X.Y: {}, Y.X: {}, });
    }
}
class P
{
}
";
            var compilation = CreatePatternCompilation(source);
            compilation.VerifyDiagnostics(
                // (8,34): error CS0117: 'P' does not contain a definition for 'X'
                //         Console.WriteLine(p is { X: 3, Y: 4 });
                Diagnostic(ErrorCode.ERR_NoSuchMember, "X").WithArguments("P", "X").WithLocation(8, 34)
                );
            var tree = compilation.SyntaxTrees[0];
            var model = compilation.GetSemanticModel(tree);

            var subpatterns = tree.GetRoot().DescendantNodes().OfType<SubpatternSyntax>().ToArray();
            Assert.Equal(2, subpatterns.Length);

            AssertEmpty(model.GetSymbolInfo(subpatterns[0]));
            AssertEmpty(model.GetSymbolInfo(subpatterns[0].ExpressionColon));
            var x = subpatterns[0].ExpressionColon.Expression;
            var xSymbol = model.GetSymbolInfo(x);
            Assert.Equal(CandidateReason.None, xSymbol.CandidateReason);
            Assert.Null(xSymbol.Symbol);
            Assert.Empty(xSymbol.CandidateSymbols);

            AssertEmpty(model.GetSymbolInfo(subpatterns[1]));
            AssertEmpty(model.GetSymbolInfo(subpatterns[1].ExpressionColon));
            var y = subpatterns[1].ExpressionColon.Expression;
            var ySymbol = model.GetSymbolInfo(y);
            Assert.Equal(CandidateReason.None, ySymbol.CandidateReason);
            Assert.Null(ySymbol.Symbol);
            Assert.Empty(ySymbol.CandidateSymbols);
        }

        [Fact]
        public void ExtendedPropertyPatterns_SymbolInfo_04()
        {
            var source =
@"
using System;
class Program
{
    public static void Main()
    {
        Console.WriteLine(new C() is { X.Y: {} });
        Console.WriteLine(new S() is { Y.X: {} });
    }
}
class C
{
    public S? X { get; }
}
struct S
{
    public C Y;
}
";
            var compilation = CreatePatternCompilation(source);
            compilation.VerifyDiagnostics(
                    // (17,14): warning CS0649: Field 'S.Y' is never assigned to, and will always have its default value null
                    //     public C Y;
                    Diagnostic(ErrorCode.WRN_UnassignedInternalField, "Y").WithArguments("S.Y", "null").WithLocation(17, 14)
                );
            var tree = compilation.SyntaxTrees[0];
            var model = compilation.GetSemanticModel(tree);

            var subpatterns = tree.GetRoot().DescendantNodes().OfType<SubpatternSyntax>().ToArray();
            Assert.Equal(2, subpatterns.Length);

            AssertEmpty(model.GetSymbolInfo(subpatterns[0]));
            AssertEmpty(model.GetSymbolInfo(subpatterns[0].ExpressionColon));
            var xy = subpatterns[0].ExpressionColon.Expression;
            var xySymbol = model.GetSymbolInfo(xy);
            Assert.Equal(CandidateReason.None, xySymbol.CandidateReason);
            Assert.Equal("C S.Y", xySymbol.Symbol.ToTestDisplayString());
            var xyType = model.GetTypeInfo(xy);
            Assert.Equal("C", xyType.Type.ToTestDisplayString());
            Assert.Equal("C", xyType.ConvertedType.ToTestDisplayString());

            var x = ((MemberAccessExpressionSyntax)subpatterns[0].ExpressionColon.Expression).Expression;
            var xSymbol = model.GetSymbolInfo(x);
            Assert.Equal(CandidateReason.None, xSymbol.CandidateReason);
            Assert.Equal("S? C.X { get; }", xSymbol.Symbol.ToTestDisplayString());
            var xType = model.GetTypeInfo(x);
            Assert.Equal("S?", xType.Type.ToTestDisplayString());
            Assert.Equal("S?", xType.ConvertedType.ToTestDisplayString());

            var yName = ((MemberAccessExpressionSyntax)subpatterns[0].ExpressionColon.Expression).Name;
            var yNameSymbol = model.GetSymbolInfo(yName);
            Assert.Equal(CandidateReason.None, yNameSymbol.CandidateReason);
            Assert.Equal("C S.Y", yNameSymbol.Symbol.ToTestDisplayString());
            var yNameType = model.GetTypeInfo(yName);
            Assert.Equal("C", yNameType.Type.ToTestDisplayString());
            Assert.Equal("C", yNameType.ConvertedType.ToTestDisplayString());

            AssertEmpty(model.GetSymbolInfo(subpatterns[1]));
            AssertEmpty(model.GetSymbolInfo(subpatterns[1].ExpressionColon));
            var yx = subpatterns[1].ExpressionColon.Expression;
            var yxSymbol = model.GetSymbolInfo(yx);
            Assert.NotEqual(default, yxSymbol);
            Assert.Equal(CandidateReason.None, yxSymbol.CandidateReason);
            Assert.Equal("S? C.X { get; }", yxSymbol.Symbol.ToTestDisplayString());
            var yxType = model.GetTypeInfo(yx);
            Assert.Equal("S?", yxType.Type.ToTestDisplayString());
            Assert.Equal("S?", yxType.ConvertedType.ToTestDisplayString());

            var y = ((MemberAccessExpressionSyntax)subpatterns[1].ExpressionColon.Expression).Expression;
            var ySymbol = model.GetSymbolInfo(y);
            Assert.Equal(CandidateReason.None, ySymbol.CandidateReason);
            Assert.Equal("C S.Y", ySymbol.Symbol.ToTestDisplayString());
            var yType = model.GetTypeInfo(y);
            Assert.Equal("C", yType.Type.ToTestDisplayString());
            Assert.Equal("C", yType.ConvertedType.ToTestDisplayString());

            var xName = ((MemberAccessExpressionSyntax)subpatterns[1].ExpressionColon.Expression).Name;
            var xNameSymbol = model.GetSymbolInfo(xName);
            Assert.Equal(CandidateReason.None, xNameSymbol.CandidateReason);
            Assert.Equal("S? C.X { get; }", xNameSymbol.Symbol.ToTestDisplayString());
            var xNameType = model.GetTypeInfo(xName);
            Assert.Equal("S?", xNameType.Type.ToTestDisplayString());
            Assert.Equal("S?", xNameType.ConvertedType.ToTestDisplayString());
        }

        [Fact]
        public void ExtendedPropertyPatterns_Nullability_01()
        {
            var program = @"
#nullable enable
class C {
    C? Prop { get; }
    public void M() {
        if (this is { Prop.Prop: null }) 
        {
            this.Prop.ToString();
            this.Prop.Prop.ToString(); // 1
        }
        if (this is { Prop.Prop: {} }) 
        {
            this.Prop.ToString();
            this.Prop.Prop.ToString();
        }
        if (this is { Prop: null } && 
            this is { Prop.Prop: null }) 
        {
            this.Prop.ToString();
            this.Prop.Prop.ToString(); // 2
        }
        if (this is { Prop: null } || 
            this is { Prop.Prop: null }) 
        {
            this.Prop.ToString();      // 3
            this.Prop.Prop.ToString(); // 4
        }
    }
}
";
            var compilation = CreateCompilation(program, parseOptions: TestOptions.RegularWithExtendedPropertyPatterns);
            compilation.VerifyDiagnostics(
                    // (9,13): warning CS8602: Dereference of a possibly null reference.
                    //             this.Prop.Prop.ToString(); // 1
                    Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "this.Prop.Prop").WithLocation(9, 13),
                    // (20,13): warning CS8602: Dereference of a possibly null reference.
                    //             this.Prop.Prop.ToString(); // 2
                    Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "this.Prop.Prop").WithLocation(20, 13),
                    // (25,13): warning CS8602: Dereference of a possibly null reference.
                    //             this.Prop.ToString();      // 3
                    Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "this.Prop").WithLocation(25, 13),
                    // (26,13): warning CS8602: Dereference of a possibly null reference.
                    //             this.Prop.Prop.ToString(); // 4
                    Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "this.Prop.Prop").WithLocation(26, 13));
        }

        [Fact]
        public void ExtendedPropertyPatterns_Nullability_02()
        {
            var program = @"
#nullable enable
class C {
    public void M(C1 c1) {
        if (c1 is { Prop.Prop: null }) 
        {
            c1.Prop.ToString();
            c1.Prop.Prop.ToString(); // 1
        }
        if (c1 is { Prop.Prop: {} }) 
        {
            c1.Prop.ToString();
            c1.Prop.Prop.ToString();
        }
        if (c1 is { Prop: null } && 
            c1 is { Prop.Prop: null }) 
        {
            c1.Prop.ToString();
            c1.Prop.Prop.ToString(); // 2
        }
        if (c1 is { Prop: null } || 
            c1 is { Prop.Prop: null }) 
        {
            c1.Prop.ToString();      // 3
            c1.Prop.Prop.ToString(); // 4
        }
    }
}
class C1 { public C2? Prop; }
class C2 { public object? Prop; }
";
            var compilation = CreateCompilation(program, parseOptions: TestOptions.RegularWithExtendedPropertyPatterns);
            compilation.VerifyDiagnostics(
                    // (8,13): warning CS8602: Dereference of a possibly null reference.
                    //             c1.Prop.Prop.ToString(); // 1
                    Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "c1.Prop.Prop").WithLocation(8, 13),
                    // (19,13): warning CS8602: Dereference of a possibly null reference.
                    //             c1.Prop.Prop.ToString(); // 2
                    Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "c1.Prop.Prop").WithLocation(19, 13),
                    // (24,13): warning CS8602: Dereference of a possibly null reference.
                    //             c1.Prop.ToString();      // 3
                    Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "c1.Prop").WithLocation(24, 13),
                    // (25,13): warning CS8602: Dereference of a possibly null reference.
                    //             c1.Prop.Prop.ToString(); // 4
                    Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "c1.Prop.Prop").WithLocation(25, 13),
                    // (29,23): warning CS0649: Field 'C1.Prop' is never assigned to, and will always have its default value null
                    // class C1 { public C2? Prop; }
                    Diagnostic(ErrorCode.WRN_UnassignedInternalField, "Prop").WithArguments("C1.Prop", "null").WithLocation(29, 23),
                    // (30,27): warning CS0649: Field 'C2.Prop' is never assigned to, and will always have its default value null
                    // class C2 { public object? Prop; }
                    Diagnostic(ErrorCode.WRN_UnassignedInternalField, "Prop").WithArguments("C2.Prop", "null").WithLocation(30, 27));
        }
    }
}
