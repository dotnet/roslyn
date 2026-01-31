// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    [CompilerTrait(CompilerFeature.Patterns)]
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
                    // (6,26): error CS8918: Identifier or a simple member access expected.
                    //         _ = new C() is { Prop1<int>.Prop2: {} };
                    Diagnostic(ErrorCode.ERR_InvalidNameInSubpattern, "Prop1<int>").WithLocation(6, 26),
                    // (7,26): error CS8918: Identifier or a simple member access expected.
                    //         _ = new C() is { Prop1->Prop2: {} };
                    Diagnostic(ErrorCode.ERR_InvalidNameInSubpattern, "Prop1->Prop2").WithLocation(7, 26),
                    // (8,26): error CS8918: Identifier or a simple member access expected.
                    //         _ = new C() is { Prop1!.Prop2: {} };
                    Diagnostic(ErrorCode.ERR_InvalidNameInSubpattern, "Prop1!").WithLocation(8, 26),
                    // (9,26): error CS8918: Identifier or a simple member access expected.
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
                    Diagnostic(ErrorCode.ERR_SyntaxError, ":").WithArguments(",").WithLocation(10, 34),
                    // (10,36): error CS1003: Syntax error, ',' expected
                    //         _ = new C() is { Prop1[0]: {} };
                    Diagnostic(ErrorCode.ERR_SyntaxError, "{").WithArguments(",").WithLocation(10, 36),
                    // (11,26): error CS8918: Identifier or a simple member access expected.
                    //         _ = new C() is { 1: {} };
                    Diagnostic(ErrorCode.ERR_InvalidNameInSubpattern, "1").WithLocation(11, 26));
        }

        [Fact, WorkItem(52956, "https://github.com/dotnet/roslyn/issues/52956")]
        public void ExtendedPropertyPatterns_05()
        {
            var program = @"
class C
{
    C Field1, Field2, Field3, Field4;
    public void M()
    {
        _ = this is { Field1.Field2.Field3: {} };
        _ = this is { Field4: {} };
    }
}
";
            var compilation = CreateCompilation(program, parseOptions: TestOptions.RegularWithExtendedPropertyPatterns);
            compilation.VerifyEmitDiagnostics(
                // (4,7): warning CS0649: Field 'C.Field1' is never assigned to, and will always have its default value null
                //     C Field1, Field2, Field3, Field4;
                Diagnostic(ErrorCode.WRN_UnassignedInternalField, "Field1").WithArguments("C.Field1", "null").WithLocation(4, 7),
                // (4,15): warning CS0649: Field 'C.Field2' is never assigned to, and will always have its default value null
                //     C Field1, Field2, Field3, Field4;
                Diagnostic(ErrorCode.WRN_UnassignedInternalField, "Field2").WithArguments("C.Field2", "null").WithLocation(4, 15),
                // (4,23): warning CS0649: Field 'C.Field3' is never assigned to, and will always have its default value null
                //     C Field1, Field2, Field3, Field4;
                Diagnostic(ErrorCode.WRN_UnassignedInternalField, "Field3").WithArguments("C.Field3", "null").WithLocation(4, 23),
                // (4,31): warning CS0649: Field 'C.Field4' is never assigned to, and will always have its default value null
                //     C Field1, Field2, Field3, Field4;
                Diagnostic(ErrorCode.WRN_UnassignedInternalField, "Field4").WithArguments("C.Field4", "null").WithLocation(4, 31)
                );
        }

        [Fact, WorkItem(52956, "https://github.com/dotnet/roslyn/issues/52956")]
        public void ExtendedPropertyPatterns_05_NestedRecursivePattern()
        {
            var program = @"
class C
{
    C Field1, Field2, Field3, Field4;
    public void M()
    {
        _ = this is { Field1: { Field2.Field3.Field4: not null } };
    }
}
";
            var compilation = CreateCompilation(program, parseOptions: TestOptions.RegularWithExtendedPropertyPatterns);
            compilation.VerifyEmitDiagnostics(
                // (4,7): warning CS0649: Field 'C.Field1' is never assigned to, and will always have its default value null
                //     C Field1, Field2, Field3, Field4;
                Diagnostic(ErrorCode.WRN_UnassignedInternalField, "Field1").WithArguments("C.Field1", "null").WithLocation(4, 7),
                // (4,15): warning CS0649: Field 'C.Field2' is never assigned to, and will always have its default value null
                //     C Field1, Field2, Field3, Field4;
                Diagnostic(ErrorCode.WRN_UnassignedInternalField, "Field2").WithArguments("C.Field2", "null").WithLocation(4, 15),
                // (4,23): warning CS0649: Field 'C.Field3' is never assigned to, and will always have its default value null
                //     C Field1, Field2, Field3, Field4;
                Diagnostic(ErrorCode.WRN_UnassignedInternalField, "Field3").WithArguments("C.Field3", "null").WithLocation(4, 23),
                // (4,31): warning CS0649: Field 'C.Field4' is never assigned to, and will always have its default value null
                //     C Field1, Field2, Field3, Field4;
                Diagnostic(ErrorCode.WRN_UnassignedInternalField, "Field4").WithArguments("C.Field4", "null").WithLocation(4, 31)
                );
        }

        [Fact, WorkItem(52956, "https://github.com/dotnet/roslyn/issues/52956")]
        public void ExtendedPropertyPatterns_05_Properties()
        {
            var program = @"
class C
{
    C Prop1 { get; set; }
    C Prop2 { get; set; }
    C Prop3 { get; set; }
    C Prop4 { get; set; }
    public void M()
    {
        _ = this is { Prop1.Prop2.Prop3: {} };
        _ = this is { Prop4: {} };
    }
}
";
            var compilation = CreateCompilation(program, parseOptions: TestOptions.RegularWithExtendedPropertyPatterns);
            compilation.VerifyEmitDiagnostics();
        }

        [Fact]
        public void ExtendedPropertyPatterns_IOperation_Properties()
        {
            var src = @"
class Program
{
    static void M(A a)
    /*<bind>*/{
        _ = a is { Prop1.Prop2.Prop3: null };
    }/*</bind>*/
}
class A { public B Prop1 => null; }
class B { public C Prop2 => null; }
class C { public object Prop3 => null; }
";
            var comp = CreateCompilation(src, parseOptions: TestOptions.RegularWithExtendedPropertyPatterns);
            comp.VerifyDiagnostics();

            var tree = comp.SyntaxTrees.Single();
            var model = comp.GetSemanticModel(tree, ignoreAccessibility: false);
            var isPattern = tree.GetRoot().DescendantNodes().OfType<IsPatternExpressionSyntax>().Single();

            VerifyOperationTree(comp, model.GetOperation(isPattern), @"
IIsPatternOperation (OperationKind.IsPattern, Type: System.Boolean) (Syntax: 'a is { Prop ... op3: null }')
  Value:
    IParameterReferenceOperation: a (OperationKind.ParameterReference, Type: A) (Syntax: 'a')
  Pattern:
    IRecursivePatternOperation (OperationKind.RecursivePattern, Type: null) (Syntax: '{ Prop1.Pro ... op3: null }') (InputType: A, NarrowedType: A, DeclaredSymbol: null, MatchedType: A, DeconstructSymbol: null)
      DeconstructionSubpatterns (0)
      PropertySubpatterns (1):
          IPropertySubpatternOperation (OperationKind.PropertySubpattern, Type: null, IsImplicit) (Syntax: 'Prop1')
            Member:
              IPropertyReferenceOperation: B A.Prop1 { get; } (OperationKind.PropertyReference, Type: B) (Syntax: 'Prop1')
                Instance Receiver:
                  IInstanceReferenceOperation (ReferenceKind: PatternInput) (OperationKind.InstanceReference, Type: A, IsImplicit) (Syntax: 'Prop1')
            Pattern:
              IRecursivePatternOperation (OperationKind.RecursivePattern, Type: null, IsImplicit) (Syntax: 'Prop1') (InputType: B, NarrowedType: B, DeclaredSymbol: null, MatchedType: B, DeconstructSymbol: null)
                DeconstructionSubpatterns (0)
                PropertySubpatterns (1):
                    IPropertySubpatternOperation (OperationKind.PropertySubpattern, Type: null, IsImplicit) (Syntax: 'Prop1.Prop2')
                      Member:
                        IPropertyReferenceOperation: C B.Prop2 { get; } (OperationKind.PropertyReference, Type: C) (Syntax: 'Prop1.Prop2')
                          Instance Receiver:
                            IInstanceReferenceOperation (ReferenceKind: PatternInput) (OperationKind.InstanceReference, Type: B, IsImplicit) (Syntax: 'Prop1.Prop2')
                      Pattern:
                        IRecursivePatternOperation (OperationKind.RecursivePattern, Type: null, IsImplicit) (Syntax: 'Prop1.Prop2') (InputType: C, NarrowedType: C, DeclaredSymbol: null, MatchedType: C, DeconstructSymbol: null)
                          DeconstructionSubpatterns (0)
                          PropertySubpatterns (1):
                              IPropertySubpatternOperation (OperationKind.PropertySubpattern, Type: null, IsImplicit) (Syntax: 'Prop1.Prop2.Prop3')
                                Member:
                                  IPropertyReferenceOperation: System.Object C.Prop3 { get; } (OperationKind.PropertyReference, Type: System.Object) (Syntax: 'Prop1.Prop2.Prop3')
                                    Instance Receiver:
                                      IInstanceReferenceOperation (ReferenceKind: PatternInput) (OperationKind.InstanceReference, Type: C, IsImplicit) (Syntax: 'Prop1.Prop2.Prop3')
                                Pattern:
                                  IConstantPatternOperation (OperationKind.ConstantPattern, Type: null) (Syntax: 'null') (InputType: System.Object, NarrowedType: System.Object)
                                    Value:
                                      IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Object, Constant: null, IsImplicit) (Syntax: 'null')
                                        Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
                                        Operand:
                                          ILiteralOperation (OperationKind.Literal, Type: null, Constant: null) (Syntax: 'null')
");

            var expectedFlowGraph = @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
Block[B1] - Block
    Predecessors: [B0]
    Statements (1)
        IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: '_ = a is {  ... p3: null };')
          Expression:
            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Boolean) (Syntax: '_ = a is {  ... op3: null }')
              Left:
                IDiscardOperation (Symbol: System.Boolean _) (OperationKind.Discard, Type: System.Boolean) (Syntax: '_')
              Right:
                IIsPatternOperation (OperationKind.IsPattern, Type: System.Boolean) (Syntax: 'a is { Prop ... op3: null }')
                  Value:
                    IParameterReferenceOperation: a (OperationKind.ParameterReference, Type: A) (Syntax: 'a')
                  Pattern:
                    IRecursivePatternOperation (OperationKind.RecursivePattern, Type: null) (Syntax: '{ Prop1.Pro ... op3: null }') (InputType: A, NarrowedType: A, DeclaredSymbol: null, MatchedType: A, DeconstructSymbol: null)
                      DeconstructionSubpatterns (0)
                      PropertySubpatterns (1):
                          IPropertySubpatternOperation (OperationKind.PropertySubpattern, Type: null, IsImplicit) (Syntax: 'Prop1')
                            Member:
                              IPropertyReferenceOperation: B A.Prop1 { get; } (OperationKind.PropertyReference, Type: B) (Syntax: 'Prop1')
                                Instance Receiver:
                                  IInstanceReferenceOperation (ReferenceKind: PatternInput) (OperationKind.InstanceReference, Type: A, IsImplicit) (Syntax: 'Prop1')
                            Pattern:
                              IRecursivePatternOperation (OperationKind.RecursivePattern, Type: null, IsImplicit) (Syntax: 'Prop1') (InputType: B, NarrowedType: B, DeclaredSymbol: null, MatchedType: B, DeconstructSymbol: null)
                                DeconstructionSubpatterns (0)
                                PropertySubpatterns (1):
                                    IPropertySubpatternOperation (OperationKind.PropertySubpattern, Type: null, IsImplicit) (Syntax: 'Prop1.Prop2')
                                      Member:
                                        IPropertyReferenceOperation: C B.Prop2 { get; } (OperationKind.PropertyReference, Type: C) (Syntax: 'Prop1.Prop2')
                                          Instance Receiver:
                                            IInstanceReferenceOperation (ReferenceKind: PatternInput) (OperationKind.InstanceReference, Type: B, IsImplicit) (Syntax: 'Prop1.Prop2')
                                      Pattern:
                                        IRecursivePatternOperation (OperationKind.RecursivePattern, Type: null, IsImplicit) (Syntax: 'Prop1.Prop2') (InputType: C, NarrowedType: C, DeclaredSymbol: null, MatchedType: C, DeconstructSymbol: null)
                                          DeconstructionSubpatterns (0)
                                          PropertySubpatterns (1):
                                              IPropertySubpatternOperation (OperationKind.PropertySubpattern, Type: null, IsImplicit) (Syntax: 'Prop1.Prop2.Prop3')
                                                Member:
                                                  IPropertyReferenceOperation: System.Object C.Prop3 { get; } (OperationKind.PropertyReference, Type: System.Object) (Syntax: 'Prop1.Prop2.Prop3')
                                                    Instance Receiver:
                                                      IInstanceReferenceOperation (ReferenceKind: PatternInput) (OperationKind.InstanceReference, Type: C, IsImplicit) (Syntax: 'Prop1.Prop2.Prop3')
                                                Pattern:
                                                  IConstantPatternOperation (OperationKind.ConstantPattern, Type: null) (Syntax: 'null') (InputType: System.Object, NarrowedType: System.Object)
                                                    Value:
                                                      IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Object, Constant: null, IsImplicit) (Syntax: 'null')
                                                        Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
                                                          (ImplicitReference)
                                                        Operand:
                                                          ILiteralOperation (OperationKind.Literal, Type: null, Constant: null) (Syntax: 'null')
    Next (Regular) Block[B2]
Block[B2] - Exit
    Predecessors: [B1]
    Statements (0)
";

            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(src, expectedFlowGraph, DiagnosticDescription.None, parseOptions: TestOptions.Regular10);
        }

        [Fact]
        public void ExtendedPropertyPatterns_IOperation_FieldsInStructs()
        {
            var src = @"
class Program
{
    static void M(A a)
    /*<bind>*/{
        _ = a is { Field1.Field2.Field3: null, Field4: null };
    }/*</bind>*/
}
struct A { public B? Field1; public B? Field4; }
struct B { public C? Field2; }
struct C { public object Field3; }
";
            var expectedDiagnostics = new[]
            {
                // (9,22): warning CS0649: Field 'A.Field1' is never assigned to, and will always have its default value
                // struct A { public B? Field1; public B? Field4; }
                Diagnostic(ErrorCode.WRN_UnassignedInternalField, "Field1").WithArguments("A.Field1", "").WithLocation(9, 22),
                // (9,40): warning CS0649: Field 'A.Field4' is never assigned to, and will always have its default value
                // struct A { public B? Field1; public B? Field4; }
                Diagnostic(ErrorCode.WRN_UnassignedInternalField, "Field4").WithArguments("A.Field4", "").WithLocation(9, 40),
                // (10,22): warning CS0649: Field 'B.Field2' is never assigned to, and will always have its default value
                // struct B { public C? Field2; }
                Diagnostic(ErrorCode.WRN_UnassignedInternalField, "Field2").WithArguments("B.Field2", "").WithLocation(10, 22),
                // (11,26): warning CS0649: Field 'C.Field3' is never assigned to, and will always have its default value null
                // struct C { public object Field3; }
                Diagnostic(ErrorCode.WRN_UnassignedInternalField, "Field3").WithArguments("C.Field3", "null").WithLocation(11, 26)
            };
            var comp = CreateCompilation(src, parseOptions: TestOptions.RegularWithExtendedPropertyPatterns);
            comp.VerifyDiagnostics(expectedDiagnostics);

            var tree = comp.SyntaxTrees.Single();
            var model = comp.GetSemanticModel(tree, ignoreAccessibility: false);
            var isPattern = tree.GetRoot().DescendantNodes().OfType<IsPatternExpressionSyntax>().Single();

            VerifyOperationTree(comp, model.GetOperation(isPattern), @"
IIsPatternOperation (OperationKind.IsPattern, Type: System.Boolean) (Syntax: 'a is { Fiel ... ld4: null }')
  Value:
    IParameterReferenceOperation: a (OperationKind.ParameterReference, Type: A) (Syntax: 'a')
  Pattern:
    IRecursivePatternOperation (OperationKind.RecursivePattern, Type: null) (Syntax: '{ Field1.Fi ... ld4: null }') (InputType: A, NarrowedType: A, DeclaredSymbol: null, MatchedType: A, DeconstructSymbol: null)
      DeconstructionSubpatterns (0)
      PropertySubpatterns (2):
          IPropertySubpatternOperation (OperationKind.PropertySubpattern, Type: null, IsImplicit) (Syntax: 'Field1')
            Member:
              IFieldReferenceOperation: B? A.Field1 (OperationKind.FieldReference, Type: B?) (Syntax: 'Field1')
                Instance Receiver:
                  IInstanceReferenceOperation (ReferenceKind: PatternInput) (OperationKind.InstanceReference, Type: A, IsImplicit) (Syntax: 'Field1')
            Pattern:
              IRecursivePatternOperation (OperationKind.RecursivePattern, Type: null, IsImplicit) (Syntax: 'Field1') (InputType: B, NarrowedType: B, DeclaredSymbol: null, MatchedType: B, DeconstructSymbol: null)
                DeconstructionSubpatterns (0)
                PropertySubpatterns (1):
                    IPropertySubpatternOperation (OperationKind.PropertySubpattern, Type: null, IsImplicit) (Syntax: 'Field1.Field2')
                      Member:
                        IFieldReferenceOperation: C? B.Field2 (OperationKind.FieldReference, Type: C?) (Syntax: 'Field1.Field2')
                          Instance Receiver:
                            IInstanceReferenceOperation (ReferenceKind: PatternInput) (OperationKind.InstanceReference, Type: B, IsImplicit) (Syntax: 'Field1.Field2')
                      Pattern:
                        IRecursivePatternOperation (OperationKind.RecursivePattern, Type: null, IsImplicit) (Syntax: 'Field1.Field2') (InputType: C, NarrowedType: C, DeclaredSymbol: null, MatchedType: C, DeconstructSymbol: null)
                          DeconstructionSubpatterns (0)
                          PropertySubpatterns (1):
                              IPropertySubpatternOperation (OperationKind.PropertySubpattern, Type: null, IsImplicit) (Syntax: 'Field1.Field2.Field3')
                                Member:
                                  IFieldReferenceOperation: System.Object C.Field3 (OperationKind.FieldReference, Type: System.Object) (Syntax: 'Field1.Field2.Field3')
                                    Instance Receiver:
                                      IInstanceReferenceOperation (ReferenceKind: PatternInput) (OperationKind.InstanceReference, Type: C, IsImplicit) (Syntax: 'Field1.Field2.Field3')
                                Pattern:
                                  IConstantPatternOperation (OperationKind.ConstantPattern, Type: null) (Syntax: 'null') (InputType: System.Object, NarrowedType: System.Object)
                                    Value:
                                      IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Object, Constant: null, IsImplicit) (Syntax: 'null')
                                        Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
                                        Operand:
                                          ILiteralOperation (OperationKind.Literal, Type: null, Constant: null) (Syntax: 'null')
          IPropertySubpatternOperation (OperationKind.PropertySubpattern, Type: null) (Syntax: 'Field4: null')
            Member:
              IFieldReferenceOperation: B? A.Field4 (OperationKind.FieldReference, Type: B?) (Syntax: 'Field4')
                Instance Receiver:
                  IInstanceReferenceOperation (ReferenceKind: PatternInput) (OperationKind.InstanceReference, Type: A, IsImplicit) (Syntax: 'Field4')
            Pattern:
              IConstantPatternOperation (OperationKind.ConstantPattern, Type: null) (Syntax: 'null') (InputType: B?, NarrowedType: B?)
                Value:
                  IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: B?, Constant: null, IsImplicit) (Syntax: 'null')
                    Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    Operand:
                      ILiteralOperation (OperationKind.Literal, Type: null, Constant: null) (Syntax: 'null')
");

            var expectedFlowGraph = @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
Block[B1] - Block
    Predecessors: [B0]
    Statements (1)
        IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: '_ = a is {  ... d4: null };')
          Expression:
            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Boolean) (Syntax: '_ = a is {  ... ld4: null }')
              Left:
                IDiscardOperation (Symbol: System.Boolean _) (OperationKind.Discard, Type: System.Boolean) (Syntax: '_')
              Right:
                IIsPatternOperation (OperationKind.IsPattern, Type: System.Boolean) (Syntax: 'a is { Fiel ... ld4: null }')
                  Value:
                    IParameterReferenceOperation: a (OperationKind.ParameterReference, Type: A) (Syntax: 'a')
                  Pattern:
                    IRecursivePatternOperation (OperationKind.RecursivePattern, Type: null) (Syntax: '{ Field1.Fi ... ld4: null }') (InputType: A, NarrowedType: A, DeclaredSymbol: null, MatchedType: A, DeconstructSymbol: null)
                      DeconstructionSubpatterns (0)
                      PropertySubpatterns (2):
                          IPropertySubpatternOperation (OperationKind.PropertySubpattern, Type: null, IsImplicit) (Syntax: 'Field1')
                            Member:
                              IFieldReferenceOperation: B? A.Field1 (OperationKind.FieldReference, Type: B?) (Syntax: 'Field1')
                                Instance Receiver:
                                  IInstanceReferenceOperation (ReferenceKind: PatternInput) (OperationKind.InstanceReference, Type: A, IsImplicit) (Syntax: 'Field1')
                            Pattern:
                              IRecursivePatternOperation (OperationKind.RecursivePattern, Type: null, IsImplicit) (Syntax: 'Field1') (InputType: B, NarrowedType: B, DeclaredSymbol: null, MatchedType: B, DeconstructSymbol: null)
                                DeconstructionSubpatterns (0)
                                PropertySubpatterns (1):
                                    IPropertySubpatternOperation (OperationKind.PropertySubpattern, Type: null, IsImplicit) (Syntax: 'Field1.Field2')
                                      Member:
                                        IFieldReferenceOperation: C? B.Field2 (OperationKind.FieldReference, Type: C?) (Syntax: 'Field1.Field2')
                                          Instance Receiver:
                                            IInstanceReferenceOperation (ReferenceKind: PatternInput) (OperationKind.InstanceReference, Type: B, IsImplicit) (Syntax: 'Field1.Field2')
                                      Pattern:
                                        IRecursivePatternOperation (OperationKind.RecursivePattern, Type: null, IsImplicit) (Syntax: 'Field1.Field2') (InputType: C, NarrowedType: C, DeclaredSymbol: null, MatchedType: C, DeconstructSymbol: null)
                                          DeconstructionSubpatterns (0)
                                          PropertySubpatterns (1):
                                              IPropertySubpatternOperation (OperationKind.PropertySubpattern, Type: null, IsImplicit) (Syntax: 'Field1.Field2.Field3')
                                                Member:
                                                  IFieldReferenceOperation: System.Object C.Field3 (OperationKind.FieldReference, Type: System.Object) (Syntax: 'Field1.Field2.Field3')
                                                    Instance Receiver:
                                                      IInstanceReferenceOperation (ReferenceKind: PatternInput) (OperationKind.InstanceReference, Type: C, IsImplicit) (Syntax: 'Field1.Field2.Field3')
                                                Pattern:
                                                  IConstantPatternOperation (OperationKind.ConstantPattern, Type: null) (Syntax: 'null') (InputType: System.Object, NarrowedType: System.Object)
                                                    Value:
                                                      IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Object, Constant: null, IsImplicit) (Syntax: 'null')
                                                        Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
                                                          (ImplicitReference)
                                                        Operand:
                                                          ILiteralOperation (OperationKind.Literal, Type: null, Constant: null) (Syntax: 'null')
                          IPropertySubpatternOperation (OperationKind.PropertySubpattern, Type: null) (Syntax: 'Field4: null')
                            Member:
                              IFieldReferenceOperation: B? A.Field4 (OperationKind.FieldReference, Type: B?) (Syntax: 'Field4')
                                Instance Receiver:
                                  IInstanceReferenceOperation (ReferenceKind: PatternInput) (OperationKind.InstanceReference, Type: A, IsImplicit) (Syntax: 'Field4')
                            Pattern:
                              IConstantPatternOperation (OperationKind.ConstantPattern, Type: null) (Syntax: 'null') (InputType: B?, NarrowedType: B?)
                                Value:
                                  IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: B?, Constant: null, IsImplicit) (Syntax: 'null')
                                    Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                      (NullLiteral)
                                    Operand:
                                      ILiteralOperation (OperationKind.Literal, Type: null, Constant: null) (Syntax: 'null')
    Next (Regular) Block[B2]
Block[B2] - Exit
    Predecessors: [B1]
    Statements (0)
";

            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(src, expectedFlowGraph, expectedDiagnostics, parseOptions: TestOptions.Regular10);
        }

        [Fact]
        public void ExtendedPropertyPatterns_Explainer()
        {
            var src = @"
class Program
{
    void M(A a)
    {
        _ = a switch // 1
        {
            { BProp.BoolProp: true } => 1
        };

        _ = a switch // 2
        {
            { BProp.IntProp: <= 0 } => 1
        };
     }
}
class A { public B BProp => null; }
class B
{
    public bool BoolProp => true;
    public int IntProp => 0;
}
";
            var comp = CreateCompilation(src, parseOptions: TestOptions.RegularWithExtendedPropertyPatterns);
            comp.VerifyDiagnostics(
                // (6,15): warning CS8509: The switch expression does not handle all possible values of its input type (it is not exhaustive). For example, the pattern '{ BProp: { BoolProp: false } }' is not covered.
                //         _ = a switch // 1
                Diagnostic(ErrorCode.WRN_SwitchExpressionNotExhaustive, "switch").WithArguments("{ BProp: { BoolProp: false } }").WithLocation(6, 15),
                // (11,15): warning CS8509: The switch expression does not handle all possible values of its input type (it is not exhaustive). For example, the pattern '{ BProp: { IntProp: 1 } }' is not covered.
                //         _ = a switch // 2
                Diagnostic(ErrorCode.WRN_SwitchExpressionNotExhaustive, "switch").WithArguments("{ BProp: { IntProp: 1 } }").WithLocation(11, 15)
                );
        }

        [Fact, WorkItem(52956, "https://github.com/dotnet/roslyn/issues/52956")]
        public void ExtendedPropertyPatterns_BadMemberAccess()
        {
            var program = @"
class C
{
    C Field1, Field2, Field3;
    public static void Main()
    {
        _ = new C() is { Field1?.Field2: {} }; // 1
        _ = new C() is { Field1!.Field2: {} }; // 2
        _ = new C() is { Missing: null }; // 3
        _ = new C() is { Field3.Missing: {} }; // 4
        _ = new C() is { Missing1.Missing2: {} }; // 5
    }
}
";
            var compilation = CreateCompilation(program, parseOptions: TestOptions.RegularWithExtendedPropertyPatterns);
            compilation.VerifyEmitDiagnostics(
                // (4,7): warning CS0169: The field 'C.Field1' is never used
                //     C Field1, Field2, Field3;
                Diagnostic(ErrorCode.WRN_UnreferencedField, "Field1").WithArguments("C.Field1").WithLocation(4, 7),
                // (4,15): warning CS0169: The field 'C.Field2' is never used
                //     C Field1, Field2, Field3;
                Diagnostic(ErrorCode.WRN_UnreferencedField, "Field2").WithArguments("C.Field2").WithLocation(4, 15),
                // (4,23): warning CS0649: Field 'C.Field3' is never assigned to, and will always have its default value null
                //     C Field1, Field2, Field3;
                Diagnostic(ErrorCode.WRN_UnassignedInternalField, "Field3").WithArguments("C.Field3", "null").WithLocation(4, 23),
                // (7,26): error CS8918: Identifier or a simple member access expected.
                //         _ = new C() is { Field1?.Field2: {} }; // 1
                Diagnostic(ErrorCode.ERR_InvalidNameInSubpattern, "Field1?.Field2").WithLocation(7, 26),
                // (8,26): error CS8918: Identifier or a simple member access expected.
                //         _ = new C() is { Field1!.Field2: {} }; // 2
                Diagnostic(ErrorCode.ERR_InvalidNameInSubpattern, "Field1!").WithLocation(8, 26),
                // (9,26): error CS0117: 'C' does not contain a definition for 'Missing'
                //         _ = new C() is { Missing: null }; // 3
                Diagnostic(ErrorCode.ERR_NoSuchMember, "Missing").WithArguments("C", "Missing").WithLocation(9, 26),
                // (10,33): error CS0117: 'C' does not contain a definition for 'Missing'
                //         _ = new C() is { Field3.Missing: {} }; // 4
                Diagnostic(ErrorCode.ERR_NoSuchMember, "Missing").WithArguments("C", "Missing").WithLocation(10, 33),
                // (11,26): error CS0117: 'C' does not contain a definition for 'Missing1'
                //         _ = new C() is { Missing1.Missing2: {} }; // 5
                Diagnostic(ErrorCode.ERR_NoSuchMember, "Missing1").WithArguments("C", "Missing1").WithLocation(11, 26)
                );
        }

        [Fact]
        public void ExtendedPropertyPatterns_IOperationOnMissing()
        {
            var program = @"
class C
{
    public void M()
    {
        _ = this is { Missing: null };
    }
}
";
            var comp = CreateCompilation(program, parseOptions: TestOptions.RegularWithExtendedPropertyPatterns);
            comp.VerifyEmitDiagnostics(
                // (6,23): error CS0117: 'C' does not contain a definition for 'Missing'
                //         _ = this is { Missing: null };
                Diagnostic(ErrorCode.ERR_NoSuchMember, "Missing").WithArguments("C", "Missing").WithLocation(6, 23)
                );

            var tree = comp.SyntaxTrees.Single();
            var model = comp.GetSemanticModel(tree, ignoreAccessibility: false);
            var isPattern = tree.GetRoot().DescendantNodes().OfType<IsPatternExpressionSyntax>().Single();

            VerifyOperationTree(comp, model.GetOperation(isPattern), @"
IIsPatternOperation (OperationKind.IsPattern, Type: System.Boolean, IsInvalid) (Syntax: 'this is { M ... ing: null }')
  Value:
    IInstanceReferenceOperation (ReferenceKind: ContainingTypeInstance) (OperationKind.InstanceReference, Type: C) (Syntax: 'this')
  Pattern:
    IRecursivePatternOperation (OperationKind.RecursivePattern, Type: null, IsInvalid) (Syntax: '{ Missing: null }') (InputType: C, NarrowedType: C, DeclaredSymbol: null, MatchedType: C, DeconstructSymbol: null)
      DeconstructionSubpatterns (0)
      PropertySubpatterns (1):
          IPropertySubpatternOperation (OperationKind.PropertySubpattern, Type: null, IsInvalid) (Syntax: 'Missing: null')
            Member:
              IInvalidOperation (OperationKind.Invalid, Type: null, IsInvalid) (Syntax: 'Missing')
                Children(0)
            Pattern:
              IConstantPatternOperation (OperationKind.ConstantPattern, Type: null) (Syntax: 'null') (InputType: ?, NarrowedType: ?)
                Value:
                  IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: ?, Constant: null, IsImplicit) (Syntax: 'null')
                    Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
                    Operand:
                      ILiteralOperation (OperationKind.Literal, Type: null, Constant: null) (Syntax: 'null')
");
        }

        [Fact]
        public void ExtendedPropertyPatterns_IOperationOnNestedMissing()
        {
            var program = @"
class C
{
    int Property { get; set; }
    public void M()
    {
        _ = this is { Property.Missing: null };
    }
}
";
            var comp = CreateCompilation(program, parseOptions: TestOptions.RegularWithExtendedPropertyPatterns);
            comp.VerifyEmitDiagnostics(
                // (7,32): error CS0117: 'int' does not contain a definition for 'Missing'
                //         _ = this is { Property.Missing: null };
                Diagnostic(ErrorCode.ERR_NoSuchMember, "Missing").WithArguments("int", "Missing").WithLocation(7, 32)
                );

            var tree = comp.SyntaxTrees.Single();
            var model = comp.GetSemanticModel(tree, ignoreAccessibility: false);
            var isPattern = tree.GetRoot().DescendantNodes().OfType<IsPatternExpressionSyntax>().Single();

            VerifyOperationTree(comp, model.GetOperation(isPattern), @"
IIsPatternOperation (OperationKind.IsPattern, Type: System.Boolean, IsInvalid) (Syntax: 'this is { P ... ing: null }')
  Value:
    IInstanceReferenceOperation (ReferenceKind: ContainingTypeInstance) (OperationKind.InstanceReference, Type: C) (Syntax: 'this')
  Pattern:
    IRecursivePatternOperation (OperationKind.RecursivePattern, Type: null, IsInvalid) (Syntax: '{ Property. ... ing: null }') (InputType: C, NarrowedType: C, DeclaredSymbol: null, MatchedType: C, DeconstructSymbol: null)
      DeconstructionSubpatterns (0)
      PropertySubpatterns (1):
          IPropertySubpatternOperation (OperationKind.PropertySubpattern, Type: null, IsImplicit) (Syntax: 'Property')
            Member:
              IPropertyReferenceOperation: System.Int32 C.Property { get; set; } (OperationKind.PropertyReference, Type: System.Int32) (Syntax: 'Property')
                Instance Receiver:
                  IInstanceReferenceOperation (ReferenceKind: PatternInput) (OperationKind.InstanceReference, Type: C, IsImplicit) (Syntax: 'Property')
            Pattern:
              IRecursivePatternOperation (OperationKind.RecursivePattern, Type: null, IsImplicit) (Syntax: 'Property') (InputType: System.Int32, NarrowedType: System.Int32, DeclaredSymbol: null, MatchedType: System.Int32, DeconstructSymbol: null)
                DeconstructionSubpatterns (0)
                PropertySubpatterns (1):
                    IPropertySubpatternOperation (OperationKind.PropertySubpattern, Type: null, IsInvalid, IsImplicit) (Syntax: 'Property.Missing')
                      Member:
                        IInvalidOperation (OperationKind.Invalid, Type: null, IsInvalid) (Syntax: 'Property.Missing')
                          Children(0)
                      Pattern:
                        IConstantPatternOperation (OperationKind.ConstantPattern, Type: null) (Syntax: 'null') (InputType: ?, NarrowedType: ?)
                          Value:
                            IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: ?, Constant: null, IsImplicit) (Syntax: 'null')
                              Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
                              Operand:
                                ILiteralOperation (OperationKind.Literal, Type: null, Constant: null) (Syntax: 'null')
");
        }

        [Fact]
        public void ExtendedPropertyPatterns_IOperationOnTwoMissing()
        {
            var program = @"
class C
{
    public void M()
    {
        _ = this is { Missing1.Missing2: null };
    }
}
";
            var comp = CreateCompilation(program, parseOptions: TestOptions.RegularWithExtendedPropertyPatterns);
            comp.VerifyEmitDiagnostics(
                // (6,23): error CS0117: 'C' does not contain a definition for 'Missing1'
                //         _ = this is { Missing1.Missing2: null };
                Diagnostic(ErrorCode.ERR_NoSuchMember, "Missing1").WithArguments("C", "Missing1").WithLocation(6, 23)
                );

            var tree = comp.SyntaxTrees.Single();
            var model = comp.GetSemanticModel(tree, ignoreAccessibility: false);
            var isPattern = tree.GetRoot().DescendantNodes().OfType<IsPatternExpressionSyntax>().Single();

            VerifyOperationTree(comp, model.GetOperation(isPattern), @"
IIsPatternOperation (OperationKind.IsPattern, Type: System.Boolean, IsInvalid) (Syntax: 'this is { M ... ng2: null }')
  Value:
    IInstanceReferenceOperation (ReferenceKind: ContainingTypeInstance) (OperationKind.InstanceReference, Type: C) (Syntax: 'this')
  Pattern:
    IRecursivePatternOperation (OperationKind.RecursivePattern, Type: null, IsInvalid) (Syntax: '{ Missing1. ... ng2: null }') (InputType: C, NarrowedType: C, DeclaredSymbol: null, MatchedType: C, DeconstructSymbol: null)
      DeconstructionSubpatterns (0)
      PropertySubpatterns (1):
          IPropertySubpatternOperation (OperationKind.PropertySubpattern, Type: null, IsInvalid, IsImplicit) (Syntax: 'Missing1')
            Member:
              IInvalidOperation (OperationKind.Invalid, Type: null, IsInvalid) (Syntax: 'Missing1')
                Children(0)
            Pattern:
              IRecursivePatternOperation (OperationKind.RecursivePattern, Type: null, IsInvalid, IsImplicit) (Syntax: 'Missing1') (InputType: ?, NarrowedType: ?, DeclaredSymbol: null, MatchedType: ?, DeconstructSymbol: null)
                DeconstructionSubpatterns (0)
                PropertySubpatterns (1):
                    IPropertySubpatternOperation (OperationKind.PropertySubpattern, Type: null, IsInvalid, IsImplicit) (Syntax: 'Missing1.Missing2')
                      Member:
                        IInvalidOperation (OperationKind.Invalid, Type: null, IsInvalid) (Syntax: 'Missing1.Missing2')
                          Children(0)
                      Pattern:
                        IConstantPatternOperation (OperationKind.ConstantPattern, Type: null) (Syntax: 'null') (InputType: ?, NarrowedType: ?)
                          Value:
                            IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: ?, Constant: null, IsImplicit) (Syntax: 'null')
                              Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
                              Operand:
                                ILiteralOperation (OperationKind.Literal, Type: null, Constant: null) (Syntax: 'null')
");
        }

        [Fact, WorkItem(53484, "https://github.com/dotnet/roslyn/issues/53484")]
        public void ExtendedPropertyPatterns_SuppressionOnPattern()
        {
            var program = @"
#nullable enable
public class ContainerType
{
    public class Type
    {
        public void M(object o)
        {
            const Type c = null!;
            if (o is c!) {}                      // a1
            if (o is 1!) {}                      // a2
            if (o is (c!)) {}                    // a3
            if (o is (1!)) {}                    // a4
            if (o is Type!) {}                   // a5
            if (o is ContainerType!.Type) {}     // a6
            if (o is ContainerType.Type!) {}     // a7
            if (o is < c!) {}                    // a8

            switch (o)
            {
                case c!: break;                  // b1
                case 1!: break;                  // b2
                case (c!): break;                // b3
                case (1!): break;                // b4
                case Type!: break;               // b5
                case ContainerType!.Type: break; // b6
                case ContainerType.Type!: break; // b7
                case < c!: break;                // b8
            }

            _ = o switch
            {
                c! => 0,                         // c1
                1! => 0,                         // c2
                (c!) => 0,                       // c3
                (1!) => 0,                       // c4
                Type! => 0,                      // c5
                ContainerType!.Type => 0,        // c6
                ContainerType.Type! => 0,        // c7
                < c! => 0,                       // c8
            };
        }
    }
}
";
            var compilation = CreateCompilation(program, parseOptions: TestOptions.RegularWithExtendedPropertyPatterns);
            compilation.VerifyEmitDiagnostics(
                // (10,22): error CS8598: The suppression operator is not allowed in this context
                //             if (o is c!) {}                      // a1
                Diagnostic(ErrorCode.ERR_IllegalSuppression, "c!").WithLocation(10, 22),
                // (11,22): error CS8598: The suppression operator is not allowed in this context
                //             if (o is 1!) {}                      // a2
                Diagnostic(ErrorCode.ERR_IllegalSuppression, "1!").WithLocation(11, 22),
                // (12,23): error CS8598: The suppression operator is not allowed in this context
                //             if (o is (c!)) {}                    // a3
                Diagnostic(ErrorCode.ERR_IllegalSuppression, "c!").WithLocation(12, 23),
                // (13,23): error CS8598: The suppression operator is not allowed in this context
                //             if (o is (1!)) {}                    // a4
                Diagnostic(ErrorCode.ERR_IllegalSuppression, "1!").WithLocation(13, 23),
                // (14,22): error CS8598: The suppression operator is not allowed in this context
                //             if (o is Type!) {}                   // a5
                Diagnostic(ErrorCode.ERR_IllegalSuppression, "Type!").WithLocation(14, 22),
                // (15,22): error CS8598: The suppression operator is not allowed in this context
                //             if (o is ContainerType!.Type) {}     // a6
                Diagnostic(ErrorCode.ERR_IllegalSuppression, "ContainerType").WithLocation(15, 22),
                // (16,22): error CS8598: The suppression operator is not allowed in this context
                //             if (o is ContainerType.Type!) {}     // a7
                Diagnostic(ErrorCode.ERR_IllegalSuppression, "ContainerType.Type!").WithLocation(16, 22),
                // (17,24): error CS8598: The suppression operator is not allowed in this context
                //             if (o is < c!) {}                    // a8
                Diagnostic(ErrorCode.ERR_IllegalSuppression, "c!").WithLocation(17, 24),
                // (21,22): error CS8598: The suppression operator is not allowed in this context
                //                 case c!: break;                  // b1
                Diagnostic(ErrorCode.ERR_IllegalSuppression, "c!").WithLocation(21, 22),
                // (22,22): error CS8598: The suppression operator is not allowed in this context
                //                 case 1!: break;                  // b2
                Diagnostic(ErrorCode.ERR_IllegalSuppression, "1!").WithLocation(22, 22),
                // (23,23): error CS8598: The suppression operator is not allowed in this context
                //                 case (c!): break;                // b3
                Diagnostic(ErrorCode.ERR_IllegalSuppression, "c!").WithLocation(23, 23),
                // (24,23): error CS8598: The suppression operator is not allowed in this context
                //                 case (1!): break;                // b4
                Diagnostic(ErrorCode.ERR_IllegalSuppression, "1!").WithLocation(24, 23),
                // (25,22): error CS8598: The suppression operator is not allowed in this context
                //                 case Type!: break;               // b5
                Diagnostic(ErrorCode.ERR_IllegalSuppression, "Type!").WithLocation(25, 22),
                // (26,22): error CS8598: The suppression operator is not allowed in this context
                //                 case ContainerType!.Type: break; // b6
                Diagnostic(ErrorCode.ERR_IllegalSuppression, "ContainerType").WithLocation(26, 22),
                // (27,22): error CS8598: The suppression operator is not allowed in this context
                //                 case ContainerType.Type!: break; // b7
                Diagnostic(ErrorCode.ERR_IllegalSuppression, "ContainerType.Type!").WithLocation(27, 22),
                // (28,24): error CS8598: The suppression operator is not allowed in this context
                //                 case < c!: break;                // b8
                Diagnostic(ErrorCode.ERR_IllegalSuppression, "c!").WithLocation(28, 24),
                // (33,17): error CS8598: The suppression operator is not allowed in this context
                //                 c! => 0,                         // c1
                Diagnostic(ErrorCode.ERR_IllegalSuppression, "c!").WithLocation(33, 17),
                // (34,17): error CS8598: The suppression operator is not allowed in this context
                //                 1! => 0,                         // c2
                Diagnostic(ErrorCode.ERR_IllegalSuppression, "1!").WithLocation(34, 17),
                // (35,18): error CS8598: The suppression operator is not allowed in this context
                //                 (c!) => 0,                       // c3
                Diagnostic(ErrorCode.ERR_IllegalSuppression, "c!").WithLocation(35, 18),
                // (36,18): error CS8598: The suppression operator is not allowed in this context
                //                 (1!) => 0,                       // c4
                Diagnostic(ErrorCode.ERR_IllegalSuppression, "1!").WithLocation(36, 18),
                // (37,17): error CS8598: The suppression operator is not allowed in this context
                //                 Type! => 0,                      // c5
                Diagnostic(ErrorCode.ERR_IllegalSuppression, "Type!").WithLocation(37, 17),
                // (38,17): error CS8598: The suppression operator is not allowed in this context
                //                 ContainerType!.Type => 0,        // c6
                Diagnostic(ErrorCode.ERR_IllegalSuppression, "ContainerType").WithLocation(38, 17),
                // (39,17): error CS8598: The suppression operator is not allowed in this context
                //                 ContainerType.Type! => 0,        // c7
                Diagnostic(ErrorCode.ERR_IllegalSuppression, "ContainerType.Type!").WithLocation(39, 17),
                // (40,19): error CS8598: The suppression operator is not allowed in this context
                //                 < c! => 0,                       // c8
                Diagnostic(ErrorCode.ERR_IllegalSuppression, "c!").WithLocation(40, 19)
                );
        }

        [Fact, WorkItem(53484, "https://github.com/dotnet/roslyn/issues/53484")]
        public void ExtendedPropertyPatterns_PointerAccessInPattern()
        {
            var program = @"
public class Type
{
    public unsafe void M(S* s)
    {
        if (0 is s->X) {}
    }
}

public struct S
{
    public int X;
}
";
            var compilation = CreateCompilation(program, parseOptions: TestOptions.RegularWithExtendedPropertyPatterns, options: TestOptions.UnsafeDebugDll);
            compilation.VerifyEmitDiagnostics(
                // (6,18): error CS9133: A constant value of type 'int' is expected
                //         if (0 is s->X) {}
                Diagnostic(ErrorCode.ERR_ConstantValueOfTypeExpected, "s->X").WithArguments("int").WithLocation(6, 18)
            );
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
            compilation.VerifyEmitDiagnostics(
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
            compilation.VerifyEmitDiagnostics(
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
            compilation.VerifyEmitDiagnostics(
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
            compilation.VerifyEmitDiagnostics(
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

            var verifier = CompileAndVerify(compilation);
            verifier.VerifyIL("Program.Main", @"
{
  // Code size       92 (0x5c)
  .maxstack  2
  .locals init (C V_0,
                S? V_1,
                S V_2)
  IL_0000:  nop
  IL_0001:  newobj     ""C..ctor()""
  IL_0006:  stloc.0
  IL_0007:  ldloc.0
  IL_0008:  brfalse.s  IL_002b
  IL_000a:  ldloc.0
  IL_000b:  callvirt   ""S? C.X.get""
  IL_0010:  stloc.1
  IL_0011:  ldloca.s   V_1
  IL_0013:  call       ""bool S?.HasValue.get""
  IL_0018:  brfalse.s  IL_002b
  IL_001a:  ldloca.s   V_1
  IL_001c:  call       ""S S?.GetValueOrDefault()""
  IL_0021:  ldfld      ""C S.Y""
  IL_0026:  ldnull
  IL_0027:  cgt.un
  IL_0029:  br.s       IL_002c
  IL_002b:  ldc.i4.0
  IL_002c:  call       ""void System.Console.WriteLine(bool)""
  IL_0031:  nop
  IL_0032:  ldloca.s   V_2
  IL_0034:  initobj    ""S""
  IL_003a:  ldloc.2
  IL_003b:  ldfld      ""C S.Y""
  IL_0040:  stloc.0
  IL_0041:  ldloc.0
  IL_0042:  brfalse.s  IL_0054
  IL_0044:  ldloc.0
  IL_0045:  callvirt   ""S? C.X.get""
  IL_004a:  stloc.1
  IL_004b:  ldloca.s   V_1
  IL_004d:  call       ""bool S?.HasValue.get""
  IL_0052:  br.s       IL_0055
  IL_0054:  ldc.i4.0
  IL_0055:  call       ""void System.Console.WriteLine(bool)""
  IL_005a:  nop
  IL_005b:  ret
}
");
        }

        [Fact]
        public void ExtendedPropertyPatterns_Nullability_Properties()
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
            compilation.VerifyEmitDiagnostics(
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
        public void ExtendedPropertyPatterns_Nullability_AnnotatedFields()
        {
            var program = @"
#nullable enable
class C {
    public void M(C1 c1) {
        if (c1 is { Prop1.Prop2: null })
        {
            c1.Prop1.ToString();
            c1.Prop1.Prop2.ToString(); // 1
        }
        if (c1 is { Prop1.Prop2: {} })
        {
            c1.Prop1.ToString();
            c1.Prop1.Prop2.ToString();
        }
        if (c1 is { Prop1: null } &&
            c1 is { Prop1.Prop2: null })
        {
            c1.Prop1.ToString();
            c1.Prop1.Prop2.ToString(); // 2
        }
        if (c1 is { Prop1: null } ||
            c1 is { Prop1.Prop2: null })
        {
            c1.Prop1.ToString();      // 3
            c1.Prop1.Prop2.ToString(); // 4
        }
    }
}
class C1 { public C2? Prop1 = null; }
class C2 { public object? Prop2 = null; }
";
            var compilation = CreateCompilation(program, parseOptions: TestOptions.RegularWithExtendedPropertyPatterns);
            compilation.VerifyEmitDiagnostics(
                // (8,13): warning CS8602: Dereference of a possibly null reference.
                //             c1.Prop1.Prop2.ToString(); // 1
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "c1.Prop1.Prop2").WithLocation(8, 13),
                // (19,13): warning CS8602: Dereference of a possibly null reference.
                //             c1.Prop1.Prop2.ToString(); // 2
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "c1.Prop1.Prop2").WithLocation(19, 13),
                // (24,13): warning CS8602: Dereference of a possibly null reference.
                //             c1.Prop1.ToString();      // 3
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "c1.Prop1").WithLocation(24, 13),
                // (25,13): warning CS8602: Dereference of a possibly null reference.
                //             c1.Prop1.Prop2.ToString(); // 4
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "c1.Prop1.Prop2").WithLocation(25, 13)
                );
        }

        [Fact]
        public void ExtendedPropertyPatterns_Nullability_UnannotatedFields()
        {
            var program = @"
#nullable enable
class C
{
    public void M1(C1 c1)
    {
        if (c1 is { Prop1.Prop2: null })
        {
            c1.Prop1.ToString();
            c1.Prop1.Prop2.ToString(); // 1
        }
        else
        {
            c1.Prop1.ToString();
            c1.Prop1.Prop2.ToString();
        }
    }

    public void M2(C1 c1)
    {
        if (c1 is { Prop1.Prop2: {} })
        {
            c1.Prop1.ToString();
            c1.Prop1.Prop2.ToString();
        }
    }

    public void M3(C1 c1)
    {
        if (c1 is { Prop1: null } &&
            c1 is { Prop1.Prop2: null })
        {
            c1.Prop1.ToString();
            c1.Prop1.Prop2.ToString(); // 2
        }
        else
        {
            c1.Prop1.ToString(); // 3
            c1.Prop1.Prop2.ToString();
        }
    }

    public void M4(C1 c1)
    {
        if (c1 is { Prop1: null } ||
            c1 is { Prop1.Prop2: null })
        {
            c1.Prop1.ToString();      // 4
            c1.Prop1.Prop2.ToString(); // 5
        }
        else
        {
            c1.Prop1.ToString();
            c1.Prop1.Prop2.ToString();
        }
    }
}

class C1 { public C2 Prop1 = null!; }
class C2 { public object Prop2 = null!; }
";
            var compilation = CreateCompilation(program, parseOptions: TestOptions.RegularWithExtendedPropertyPatterns);
            compilation.VerifyEmitDiagnostics(
                // (10,13): warning CS8602: Dereference of a possibly null reference.
                //             c1.Prop1.Prop2.ToString(); // 1
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "c1.Prop1.Prop2").WithLocation(10, 13),
                // (34,13): warning CS8602: Dereference of a possibly null reference.
                //             c1.Prop1.Prop2.ToString(); // 2
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "c1.Prop1.Prop2").WithLocation(34, 13),
                // (38,13): warning CS8602: Dereference of a possibly null reference.
                //             c1.Prop1.ToString(); // 3
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "c1.Prop1").WithLocation(38, 13),
                // (48,13): warning CS8602: Dereference of a possibly null reference.
                //             c1.Prop1.ToString();      // 4
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "c1.Prop1").WithLocation(48, 13),
                // (49,13): warning CS8602: Dereference of a possibly null reference.
                //             c1.Prop1.Prop2.ToString(); // 5
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "c1.Prop1.Prop2").WithLocation(49, 13)
                );
        }

        [Fact]
        public void ExtendedPropertyPatterns_ExpressionColonInPositionalPattern()
        {
            var source = @"
class C
{
    C Property { get; set; }

    void M()
    {
        _ = this is (Property.Property: null, Property: null);
    }

    public void Deconstruct(out C c1, out C c2)
        => throw null;
}
";
            var compilation = CreateCompilation(source, parseOptions: TestOptions.Regular9);
            compilation.VerifyEmitDiagnostics(
                // (8,22): error CS1001: Identifier expected
                //         _ = this is (Property.Property: null, Property: null);
                Diagnostic(ErrorCode.ERR_IdentifierExpected, "Property.Property").WithLocation(8, 22),
                // (8,39): error CS8773: Feature 'extended property patterns' is not available in C# 9.0. Please use language version 10.0 or greater.
                //         _ = this is (Property.Property: null, Property: null);
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion9, ":").WithArguments("extended property patterns", "10.0").WithLocation(8, 39),
                // (8,47): error CS8517: The name 'Property' does not match the corresponding 'Deconstruct' parameter 'c2'.
                //         _ = this is (Property.Property: null, Property: null);
                Diagnostic(ErrorCode.ERR_DeconstructParameterNameMismatch, "Property").WithArguments("Property", "c2").WithLocation(8, 47));
        }

        [Fact]
        public void ExtendedPropertyPatterns_ExpressionColonInITuplePattern()
        {
            var source = @"
class C
{
    void M()
    {
        System.Runtime.CompilerServices.ITuple t = null;
        var r = t is (X.Y: 3, Y.Z: 4);
    }
}
namespace System.Runtime.CompilerServices
{
    public interface ITuple
    {
        int Length { get; }
        object this[int index] { get; }
    }
}
";
            var compilation = CreateCompilation(source, parseOptions: TestOptions.RegularWithExtendedPropertyPatterns);
            compilation.VerifyEmitDiagnostics(
                // (7,23): error CS1001: Identifier expected
                //         var r = t is (X.Y: 3, Y.Z: 4);
                Diagnostic(ErrorCode.ERR_IdentifierExpected, "X.Y").WithLocation(7, 23),
                // (7,31): error CS1001: Identifier expected
                //         var r = t is (X.Y: 3, Y.Z: 4);
                Diagnostic(ErrorCode.ERR_IdentifierExpected, "Y.Z").WithLocation(7, 31)
                );
        }

        [Fact]
        public void ExtendedPropertyPatterns_ExpressionColonInValueTuplePattern()
        {
            var source = @"
class C
{
    void M()
    {
        _ = (1, 2) is (X.Y: 3, Y.Z: 4);
    }
}
namespace System.Runtime.CompilerServices
{
    public interface ITuple
    {
        int Length { get; }
        object this[int index] { get; }
    }
}
";
            var compilation = CreateCompilation(source, parseOptions: TestOptions.RegularWithExtendedPropertyPatterns);
            compilation.VerifyEmitDiagnostics(
                // (6,24): error CS1001: Identifier expected
                //         _ = (1, 2) is (X.Y: 3, Y.Z: 4);
                Diagnostic(ErrorCode.ERR_IdentifierExpected, "X.Y").WithLocation(6, 24),
                // (6,32): error CS1001: Identifier expected
                //         _ = (1, 2) is (X.Y: 3, Y.Z: 4);
                Diagnostic(ErrorCode.ERR_IdentifierExpected, "Y.Z").WithLocation(6, 32)
                );
        }

        [Fact]
        public void ExtendedPropertyPatterns_ObsoleteProperty()
        {
            var program = @"
using System;
class C
{
    public void M1(C1 c1)
    {
        _ = c1 is { Prop1.Prop2: null };
    }
}

class C1
{
    [ObsoleteAttribute(""error Prop1"", true)]
    public C2 Prop1 { get; set; }
}
class C2
{
    [ObsoleteAttribute(""error Prop2"", true)]
    public object Prop2 { get; set; }
}
";
            var compilation = CreateCompilation(program, parseOptions: TestOptions.RegularWithExtendedPropertyPatterns);
            compilation.VerifyEmitDiagnostics(
                // (7,21): error CS0619: 'C1.Prop1' is obsolete: 'error Prop1'
                //         _ = c1 is { Prop1.Prop2: null };
                Diagnostic(ErrorCode.ERR_DeprecatedSymbolStr, "Prop1").WithArguments("C1.Prop1", "error Prop1").WithLocation(7, 21),
                // (7,27): error CS0619: 'C2.Prop2' is obsolete: 'error Prop2'
                //         _ = c1 is { Prop1.Prop2: null };
                Diagnostic(ErrorCode.ERR_DeprecatedSymbolStr, "Prop2").WithArguments("C2.Prop2", "error Prop2").WithLocation(7, 27)
                );
        }

        [Fact]
        public void ExtendedPropertyPatterns_ObsoleteAccessor()
        {
            var program = @"
using System;
class C
{
    public void M1(C1 c1)
    {
        _ = c1 is { Prop1.Prop2: null };
    }
}

class C1
{
    public C2 Prop1
    {
        [ObsoleteAttribute(""error Prop1"", true)]
        get;
        set;
    }
}
class C2
{
    public object Prop2
    {
        get;
        [ObsoleteAttribute(""error Prop2"", true)]
        set;
    }
}
";
            var compilation = CreateCompilation(program, parseOptions: TestOptions.RegularWithExtendedPropertyPatterns);
            compilation.VerifyEmitDiagnostics(
                // (7,21): error CS0619: 'C1.Prop1.get' is obsolete: 'error Prop1'
                //         _ = c1 is { Prop1.Prop2: null };
                Diagnostic(ErrorCode.ERR_DeprecatedSymbolStr, "Prop1.Prop2").WithArguments("C1.Prop1.get", "error Prop1").WithLocation(7, 21)
                );
        }

        [Fact]
        public void ExtendedPropertyPatterns_InaccessibleProperty()
        {
            var program = @"
using System;
class C
{
    public void M1(C1 c1)
    {
        _ = c1 is { Prop1.Prop2: null };
    }
}

class C1
{
    private C2 Prop1 { get; set; }
}
class C2
{
    private object Prop2 { get; set; }
}
";
            var compilation = CreateCompilation(program, parseOptions: TestOptions.RegularWithExtendedPropertyPatterns);
            compilation.VerifyEmitDiagnostics(
                // (7,21): error CS0122: 'C1.Prop1' is inaccessible due to its protection level
                //         _ = c1 is { Prop1.Prop2: null };
                Diagnostic(ErrorCode.ERR_BadAccess, "Prop1").WithArguments("C1.Prop1").WithLocation(7, 21)
                );
        }

        [Fact]
        public void ExtendedPropertyPatterns_ExpressionTree()
        {
            var program = @"
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
class C
{
    public void M1(C1 c1)
    {
        Expression<Func<C1, bool>> f = (c1) => c1 is { Prop1.Prop2: null };
    }
}

class C1
{
    public C2 Prop1 { get; set; }
}
class C2
{
    public object Prop2 { get; set; }
}
";
            var compilation = CreateCompilation(program, parseOptions: TestOptions.RegularWithExtendedPropertyPatterns);
            compilation.VerifyEmitDiagnostics(
                // (9,48): error CS8122: An expression tree may not contain an 'is' pattern-matching operator.
                //         Expression<Func<C1, bool>> f = (c1) => c1 is { Prop1.Prop2: null };
                Diagnostic(ErrorCode.ERR_ExpressionTreeContainsIsMatch, "c1 is { Prop1.Prop2: null }").WithLocation(9, 48)
                );
        }

        [Fact]
        public void ExtendedPropertyPatterns_EvaluationOrder()
        {
            var program = @"
if (new C() is { Prop1.True: true, Prop1.Prop2: not null })
{
    System.Console.WriteLine(""matched1"");
}

if (new C() is { Prop1.Prop2: not null, Prop1.True: true })
{
    System.Console.WriteLine(""matched2"");
}

if (new C() is { Prop1.Prop2: not null, Prop2.True: true })
{
    System.Console.WriteLine(""matched3"");
}

if (new C() is { Prop1.Prop2.Prop3.True: true })
{
    System.Console.WriteLine(""matched3"");
}

if (new C() is { Prop1: { Prop2.Prop3.True: true } })
{
    System.Console.WriteLine(""matched4"");
}

if (new C() is { Prop1.True: false, Prop1.Prop2: not null })
{
    throw null;
}

class C
{
    public C Prop1 { get { System.Console.Write(""Prop1 ""); return this; } }
    public C Prop2 { get { System.Console.Write(""Prop2 ""); return this; } }
    public C Prop3 { get { System.Console.Write(""Prop3 ""); return this; } }
    public bool True { get { System.Console.Write(""True ""); return true; } }
}
";
            CompileAndVerify(program, parseOptions: TestOptions.RegularWithExtendedPropertyPatterns, expectedOutput: @"
Prop1 True Prop2 matched1
Prop1 Prop2 True matched2
Prop1 Prop2 Prop2 True matched3
Prop1 Prop2 Prop3 True matched3
Prop1 Prop2 Prop3 True matched4
Prop1 True");
        }

        [Fact]
        public void ExtendedPropertyPatterns_StaticMembers()
        {
            var program = @"
_ = new C() is { Static: null }; // 1
_ = new C() is { Instance.Static: null }; // 2
_ = new C() is { Static.Instance: null }; // 3

class C
{
    public C Instance { get; set; }
    public static C Static { get; set; }
}
";
            var comp = CreateCompilation(program, parseOptions: TestOptions.RegularWithExtendedPropertyPatterns);
            comp.VerifyEmitDiagnostics(
                // (2,18): error CS0176: Member 'C.Static' cannot be accessed with an instance reference; qualify it with a type name instead
                // _ = new C() is { Static: null }; // 1
                Diagnostic(ErrorCode.ERR_ObjectProhibited, "Static").WithArguments("C.Static").WithLocation(2, 18),
                // (3,27): error CS0176: Member 'C.Static' cannot be accessed with an instance reference; qualify it with a type name instead
                // _ = new C() is { Instance.Static: null }; // 2
                Diagnostic(ErrorCode.ERR_ObjectProhibited, "Static").WithArguments("C.Static").WithLocation(3, 27),
                // (4,18): error CS0176: Member 'C.Static' cannot be accessed with an instance reference; qualify it with a type name instead
                // _ = new C() is { Static.Instance: null }; // 3
                Diagnostic(ErrorCode.ERR_ObjectProhibited, "Static").WithArguments("C.Static").WithLocation(4, 18)
                );
        }

        [Fact]
        public void ExtendedPropertyPatterns_Exhaustiveness()
        {
            var program = @"
_ = new C() switch // 1
{
    { Prop.True: true } => 0
};

_ = new C() switch
{
    { Prop.True: true } => 0,
    { Prop.True: false } => 0
};

#nullable enable
_ = new C() switch // 2
{
    { Prop.Prop: null } => 0
};

class C
{
    public C Prop { get => throw null!; }
    public bool True { get => throw null!; }
}
";
            var comp = CreateCompilation(program, parseOptions: TestOptions.RegularWithExtendedPropertyPatterns);
            comp.VerifyEmitDiagnostics(
                // (2,13): warning CS8509: The switch expression does not handle all possible values of its input type (it is not exhaustive). For example, the pattern '{ Prop: { True: false } }' is not covered.
                // _ = new C() switch // 1
                Diagnostic(ErrorCode.WRN_SwitchExpressionNotExhaustive, "switch").WithArguments("{ Prop: { True: false } }").WithLocation(2, 13),
                // (10,18): hidden CS9335: The pattern is redundant.
                //     { Prop.True: false } => 0
                Diagnostic(ErrorCode.HDN_RedundantPattern, "false").WithLocation(10, 18),
                // (14,13): warning CS8509: The switch expression does not handle all possible values of its input type (it is not exhaustive). For example, the pattern '{ Prop: { Prop: not null } }' is not covered.
                // _ = new C() switch // 2
                Diagnostic(ErrorCode.WRN_SwitchExpressionNotExhaustive, "switch").WithArguments("{ Prop: { Prop: not null } }").WithLocation(14, 13)
                );
        }

        [Fact, WorkItem(55184, "https://github.com/dotnet/roslyn/issues/55184")]
        public void Repro55184()
        {
            var source = @"
var x = """";

_ = x is { Error: { Length: > 0 } };
_ = x is { Error.Length: > 0 };
_ = x is { Length: { Error: > 0 } };
_ = x is { Length.Error: > 0 };
";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (4,12): error CS0117: 'string' does not contain a definition for 'Error'
                // _ = x is { Error: { Length: > 0 } };
                Diagnostic(ErrorCode.ERR_NoSuchMember, "Error").WithArguments("string", "Error").WithLocation(4, 12),
                // (5,12): error CS0117: 'string' does not contain a definition for 'Error'
                // _ = x is { Error.Length: > 0 };
                Diagnostic(ErrorCode.ERR_NoSuchMember, "Error").WithArguments("string", "Error").WithLocation(5, 12),
                // (6,22): error CS0117: 'int' does not contain a definition for 'Error'
                // _ = x is { Length: { Error: > 0 } };
                Diagnostic(ErrorCode.ERR_NoSuchMember, "Error").WithArguments("int", "Error").WithLocation(6, 22),
                // (7,19): error CS0117: 'int' does not contain a definition for 'Error'
                // _ = x is { Length.Error: > 0 };
                Diagnostic(ErrorCode.ERR_NoSuchMember, "Error").WithArguments("int", "Error").WithLocation(7, 19)
                );
        }

        private const string INumberBaseDefinition = """
            namespace System.Numerics;
            public interface INumberBase<T> where T : INumberBase<T> {}
            """;

        [Fact]
        public void ForbiddenOnTypeParametersConstrainedToINumberBase_01()
        {
            var source = """
                #pragma warning disable 8321 // Unused local function
                using System.Numerics;

                void M<T>(T t) where T : INumberBase<T>
                {
                    int o = t switch
                    {
                        1 => 1, // 1
                        > 1 => 2, // 2
                        int => 3, // OK
                        [] => 4, // 3
                        (_) => 5, // OK
                        "" => 6, // OK
                        { } => 7, // OK
                        var x => 8, // OK
                        _ => 9 // Ok
                    };
                }
                """;

            var comp = CreateCompilation(new[] { source, INumberBaseDefinition });
            comp.VerifyDiagnostics(
                // (8,9): error CS9060: Cannot use a numeric constant or relational pattern on 'T' because it inherits from or extends 'INumberBase<T>'. Consider using a type pattern to narrow to a specific numeric type.
                //         1 => 1, // 1
                Diagnostic(ErrorCode.ERR_CannotMatchOnINumberBase, "1").WithArguments("T").WithLocation(8, 9),
                // (9,9): error CS9060: Cannot use a numeric constant or relational pattern on 'T' because it inherits from or extends 'INumberBase<T>'. Consider using a type pattern to narrow to a specific numeric type.
                //         > 1 => 2, // 2
                Diagnostic(ErrorCode.ERR_CannotMatchOnINumberBase, "> 1").WithArguments("T").WithLocation(9, 9),
                // (11,9): error CS8985: List patterns may not be used for a value of type 'T'. No suitable 'Length' or 'Count' property was found.
                //         [] => 4, // 3
                Diagnostic(ErrorCode.ERR_ListPatternRequiresLength, "[]").WithArguments("T").WithLocation(11, 9),
                // (11,9): error CS0518: Predefined type 'System.Index' is not defined or imported
                //         [] => 4, // 3
                Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound, "[]").WithArguments("System.Index").WithLocation(11, 9),
                // (11,9): error CS0021: Cannot apply indexing with [] to an expression of type 'T'
                //         [] => 4, // 3
                Diagnostic(ErrorCode.ERR_BadIndexLHS, "[]").WithArguments("T").WithLocation(11, 9)
            );
        }

        [Fact]
        public void ForbiddenOnTypeParametersConstrainedToINumberBase_02()
        {
            var source = """
                #pragma warning disable 8321 // Unused local function
                using System.Numerics;

                void M<T>(T t) where T : struct, INumberBase<T>
                {
                    int o = t switch
                    {
                        1 => 1, // 1
                        > 1 => 2, // 2
                        int => 3, // OK
                        [] => 4, // 3
                        (_) => 5, // OK
                        "" => 6, // 4
                        { } => 7, // OK
                        var x => 8, // OK
                        _ => 9 // Ok
                    };
                }
                """;

            var comp = CreateCompilation(new[] { source, INumberBaseDefinition });
            comp.VerifyDiagnostics(
                // (8,9): error CS9060: Cannot use a numeric constant or relational pattern on 'T' because it inherits from or extends 'INumberBase<T>'. Consider using a type pattern to narrow to a specific numeric type.
                //         1 => 1, // 1
                Diagnostic(ErrorCode.ERR_CannotMatchOnINumberBase, "1").WithArguments("T").WithLocation(8, 9),
                // (9,9): error CS9060: Cannot use a numeric constant or relational pattern on 'T' because it inherits from or extends 'INumberBase<T>'. Consider using a type pattern to narrow to a specific numeric type.
                //         > 1 => 2, // 2
                Diagnostic(ErrorCode.ERR_CannotMatchOnINumberBase, "> 1").WithArguments("T").WithLocation(9, 9),
                // (11,9): error CS8985: List patterns may not be used for a value of type 'T'. No suitable 'Length' or 'Count' property was found.
                //         [] => 4, // 3
                Diagnostic(ErrorCode.ERR_ListPatternRequiresLength, "[]").WithArguments("T").WithLocation(11, 9),
                // (11,9): error CS0518: Predefined type 'System.Index' is not defined or imported
                //         [] => 4, // 3
                Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound, "[]").WithArguments("System.Index").WithLocation(11, 9),
                // (11,9): error CS0021: Cannot apply indexing with [] to an expression of type 'T'
                //         [] => 4, // 3
                Diagnostic(ErrorCode.ERR_BadIndexLHS, "[]").WithArguments("T").WithLocation(11, 9),
                // (13,9): error CS8121: An expression of type 'T' cannot be handled by a pattern of type 'string'.
                //         "" => 6, // 4
                Diagnostic(ErrorCode.ERR_PatternWrongType, @"""""").WithArguments("T", "string").WithLocation(13, 9)
            );
        }

        [Fact]
        public void ForbiddenOnTypeParametersConstrainedToINumberBase_MultipleReferences_01()
        {
            var source = """
                #pragma warning disable 8321 // Unused local function
                using System.Numerics;

                void M<T>(T t) where T : struct, INumberBase<T>
                {
                    int o = t switch
                    {
                        1 => 1, // 1
                        > 1 => 2, // 2
                        int => 3, // OK
                        [] => 4, // 3
                        (_) => 5, // OK
                        "" => 6, // 4
                        { } => 7, // OK
                        var x => 8, // OK
                        _ => 9 // Ok
                    };
                }
                """;

            var ref1 = CreateCompilation(INumberBaseDefinition, assemblyName: "A").EmitToImageReference();
            var ref2 = CreateCompilation(INumberBaseDefinition, assemblyName: "B").EmitToImageReference();

            var comp = CreateCompilation(new[] { source }, references: new[] { ref1, ref2 });
            comp.VerifyDiagnostics(
                // (4,34): error CS0433: The type 'INumberBase<T>' exists in both 'A, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null' and 'B, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'
                // void M<T>(T t) where T : struct, INumberBase<T>
                Diagnostic(ErrorCode.ERR_SameFullNameAggAgg, "INumberBase<T>").WithArguments("A, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null", "System.Numerics.INumberBase<T>", "B, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null").WithLocation(4, 34),
                // (11,9): error CS8985: List patterns may not be used for a value of type 'T'. No suitable 'Length' or 'Count' property was found.
                //         [] => 4, // 3
                Diagnostic(ErrorCode.ERR_ListPatternRequiresLength, "[]").WithArguments("T").WithLocation(11, 9),
                // (11,9): error CS0518: Predefined type 'System.Index' is not defined or imported
                //         [] => 4, // 3
                Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound, "[]").WithArguments("System.Index").WithLocation(11, 9),
                // (11,9): error CS0021: Cannot apply indexing with [] to an expression of type 'T'
                //         [] => 4, // 3
                Diagnostic(ErrorCode.ERR_BadIndexLHS, "[]").WithArguments("T").WithLocation(11, 9),
                // (13,9): error CS8121: An expression of type 'T' cannot be handled by a pattern of type 'string'.
                //         "" => 6, // 4
                Diagnostic(ErrorCode.ERR_PatternWrongType, @"""""").WithArguments("T", "string").WithLocation(13, 9)
            );
        }

        [Fact]
        public void ForbiddenOnTypeParametersConstrainedToINumberBase_MultipleReferences_02()
        {
            var source = """
                extern alias A;
                #pragma warning disable 8321 // Unused local function

                void M<T>(T t) where T : struct, A::System.Numerics.INumberBase<T>
                {
                    int o = t switch
                    {
                        1 => 1, // 1
                        > 1 => 2, // 2
                        int => 3, // OK
                        [] => 4, // 3
                        (_) => 5, // OK
                        "" => 6, // 4
                        { } => 7, // OK
                        var x => 8, // OK
                        _ => 9 // Ok
                    };
                }
                """;

            var ref1 = CreateCompilation(INumberBaseDefinition, assemblyName: "A").EmitToImageReference(aliases: ImmutableArray.Create("A"));
            var ref2 = CreateCompilation(INumberBaseDefinition, assemblyName: "B").EmitToImageReference();

            var comp = CreateCompilation(new[] { source }, references: new[] { ref1, ref2 });
            comp.VerifyDiagnostics(
                // (8,9): error CS9060: Cannot use a numeric constant or relational pattern on 'T' because it inherits from or extends 'INumberBase<T>'. Consider using a type pattern to narrow to a specific numeric type.
                //         1 => 1, // 1
                Diagnostic(ErrorCode.ERR_CannotMatchOnINumberBase, "1").WithArguments("T").WithLocation(8, 9),
                // (9,9): error CS9060: Cannot use a numeric constant or relational pattern on 'T' because it inherits from or extends 'INumberBase<T>'. Consider using a type pattern to narrow to a specific numeric type.
                //         > 1 => 2, // 2
                Diagnostic(ErrorCode.ERR_CannotMatchOnINumberBase, "> 1").WithArguments("T").WithLocation(9, 9),
                // (11,9): error CS8985: List patterns may not be used for a value of type 'T'. No suitable 'Length' or 'Count' property was found.
                //         [] => 4, // 3
                Diagnostic(ErrorCode.ERR_ListPatternRequiresLength, "[]").WithArguments("T").WithLocation(11, 9),
                // (11,9): error CS0518: Predefined type 'System.Index' is not defined or imported
                //         [] => 4, // 3
                Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound, "[]").WithArguments("System.Index").WithLocation(11, 9),
                // (11,9): error CS0021: Cannot apply indexing with [] to an expression of type 'T'
                //         [] => 4, // 3
                Diagnostic(ErrorCode.ERR_BadIndexLHS, "[]").WithArguments("T").WithLocation(11, 9),
                // (13,9): error CS8121: An expression of type 'T' cannot be handled by a pattern of type 'string'.
                //         "" => 6, // 4
                Diagnostic(ErrorCode.ERR_PatternWrongType, @"""""").WithArguments("T", "string").WithLocation(13, 9)
            );
        }

        [Theory]
        [InlineData("class")]
        [InlineData("struct")]
        [InlineData("interface")]
        public void ForbiddenOnTypesInheritingFromINumberBase(string type)
        {
            var source = $$"""
                #pragma warning disable 8321 // Unused local function
                using System.Numerics;

                C c = default(C);
                int o = c switch
                {
                    1 => 1, // 1
                    > 1 => 2, // 2
                    int => 3, // 3
                    [] => 4, // 4
                    (_) => 5, // OK
                    "" => 6, // 5
                    { } => 7, // OK
                    var x => 8, // OK
                    _ => 9 // Ok
                };

                {{type}} C : INumberBase<C>
                {
                }
                """;

            var comp = CreateCompilation(new[] { source, INumberBaseDefinition });
            comp.VerifyDiagnostics(
                // (7,5): error CS0029: Cannot implicitly convert type 'int' to 'C'
                //     1 => 1, // 1
                Diagnostic(ErrorCode.ERR_NoImplicitConv, "1").WithArguments("int", "C").WithLocation(7, 5),
                // (8,5): error CS8781: Relational patterns may not be used for a value of type 'C'.
                //     > 1 => 2, // 2
                Diagnostic(ErrorCode.ERR_UnsupportedTypeForRelationalPattern, "> 1").WithArguments("C").WithLocation(8, 5),
                // (8,7): error CS0029: Cannot implicitly convert type 'int' to 'C'
                //     > 1 => 2, // 2
                Diagnostic(ErrorCode.ERR_NoImplicitConv, "1").WithArguments("int", "C").WithLocation(8, 7),
                // (9,5): error CS8121: An expression of type 'C' cannot be handled by a pattern of type 'int'.
                //     int => 3, // 3
                Diagnostic(ErrorCode.ERR_PatternWrongType, "int").WithArguments("C", "int").WithLocation(9, 5),
                // (10,5): error CS8985: List patterns may not be used for a value of type 'C'. No suitable 'Length' or 'Count' property was found.
                //     [] => 4, // 4
                Diagnostic(ErrorCode.ERR_ListPatternRequiresLength, "[]").WithArguments("C").WithLocation(10, 5),
                // (10,5): error CS0518: Predefined type 'System.Index' is not defined or imported
                //     [] => 4, // 4
                Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound, "[]").WithArguments("System.Index").WithLocation(10, 5),
                // (10,5): error CS0021: Cannot apply indexing with [] to an expression of type 'C'
                //     [] => 4, // 4
                Diagnostic(ErrorCode.ERR_BadIndexLHS, "[]").WithArguments("C").WithLocation(10, 5),
                // (12,5): error CS0029: Cannot implicitly convert type 'string' to 'C'
                //     "" => 6, // 5
                Diagnostic(ErrorCode.ERR_NoImplicitConv, @"""""").WithArguments("string", "C").WithLocation(12, 5)
            );
        }

        [Theory]
        [InlineData("class")]
        [InlineData("struct")]
        [InlineData("interface")]
        public void ForbiddenOnTypesInheritingFromINumberBase_MultipleReferences01(string type)
        {
            var source = $$"""
                #pragma warning disable 8321 // Unused local function
                using System.Numerics;

                C c = default(C);
                int o = c switch
                {
                    1 => 1, // 1
                    > 1 => 2, // 2
                    int => 3, // 3
                    [] => 4, // 4
                    (_) => 5, // OK
                    "" => 6, // 5
                    { } => 7, // OK
                    var x => 8, // OK
                    _ => 9 // Ok
                };

                {{type}} C :
                    INumberBase<C>
                {
                }
                """;

            var ref1 = CreateCompilation(INumberBaseDefinition, assemblyName: "A").EmitToImageReference();
            var ref2 = CreateCompilation(INumberBaseDefinition, assemblyName: "B").EmitToImageReference();

            var comp = CreateCompilation(new[] { source }, references: new[] { ref1, ref2 });
            comp.VerifyDiagnostics(
                // (7,5): error CS0029: Cannot implicitly convert type 'int' to 'C'
                //     1 => 1, // 1
                Diagnostic(ErrorCode.ERR_NoImplicitConv, "1").WithArguments("int", "C").WithLocation(7, 5),
                // (8,5): error CS8781: Relational patterns may not be used for a value of type 'C'.
                //     > 1 => 2, // 2
                Diagnostic(ErrorCode.ERR_UnsupportedTypeForRelationalPattern, "> 1").WithArguments("C").WithLocation(8, 5),
                // (8,7): error CS0029: Cannot implicitly convert type 'int' to 'C'
                //     > 1 => 2, // 2
                Diagnostic(ErrorCode.ERR_NoImplicitConv, "1").WithArguments("int", "C").WithLocation(8, 7),
                // (9,5): error CS8121: An expression of type 'C' cannot be handled by a pattern of type 'int'.
                //     int => 3, // 3
                Diagnostic(ErrorCode.ERR_PatternWrongType, "int").WithArguments("C", "int").WithLocation(9, 5),
                // (10,5): error CS8985: List patterns may not be used for a value of type 'C'. No suitable 'Length' or 'Count' property was found.
                //     [] => 4, // 4
                Diagnostic(ErrorCode.ERR_ListPatternRequiresLength, "[]").WithArguments("C").WithLocation(10, 5),
                // (10,5): error CS0518: Predefined type 'System.Index' is not defined or imported
                //     [] => 4, // 4
                Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound, "[]").WithArguments("System.Index").WithLocation(10, 5),
                // (10,5): error CS0021: Cannot apply indexing with [] to an expression of type 'C'
                //     [] => 4, // 4
                Diagnostic(ErrorCode.ERR_BadIndexLHS, "[]").WithArguments("C").WithLocation(10, 5),
                // (12,5): error CS0029: Cannot implicitly convert type 'string' to 'C'
                //     "" => 6, // 5
                Diagnostic(ErrorCode.ERR_NoImplicitConv, @"""""").WithArguments("string", "C").WithLocation(12, 5),
                // (19,5): error CS0433: The type 'INumberBase<T>' exists in both 'A, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null' and 'B, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'
                //     INumberBase<C>
                Diagnostic(ErrorCode.ERR_SameFullNameAggAgg, "INumberBase<C>").WithArguments("A, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null", "System.Numerics.INumberBase<T>", "B, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null").WithLocation(19, 5)
            );
        }

        [Theory]
        [InlineData("class")]
        [InlineData("struct")]
        [InlineData("interface")]
        public void ForbiddenOnTypeParametersInheritingFromINumberBase_MultipleReferences02(string type)
        {
            var source = $$"""
                extern alias A;
                #pragma warning disable 8321 // Unused local function

                C c = default(C);
                int o = c switch
                {
                    1 => 1, // 1
                    > 1 => 2, // 2
                    int => 3, // 3
                    [] => 4, // 4
                    (_) => 5, // OK
                    "" => 6, // 5
                    { } => 7, // OK
                    var x => 8, // OK
                    _ => 9 // Ok
                };

                {{type}} C : A::System.Numerics.INumberBase<C>
                {
                }
                """;

            var ref1 = CreateCompilation(INumberBaseDefinition).EmitToImageReference(aliases: ImmutableArray.Create("A"));
            var ref2 = CreateCompilation(INumberBaseDefinition).EmitToImageReference();

            var comp = CreateCompilation(new[] { source }, references: new[] { ref1, ref2 });
            comp.VerifyDiagnostics(
                // (7,5): error CS0029: Cannot implicitly convert type 'int' to 'C'
                //     1 => 1, // 1
                Diagnostic(ErrorCode.ERR_NoImplicitConv, "1").WithArguments("int", "C").WithLocation(7, 5),
                // (8,5): error CS8781: Relational patterns may not be used for a value of type 'C'.
                //     > 1 => 2, // 2
                Diagnostic(ErrorCode.ERR_UnsupportedTypeForRelationalPattern, "> 1").WithArguments("C").WithLocation(8, 5),
                // (8,7): error CS0029: Cannot implicitly convert type 'int' to 'C'
                //     > 1 => 2, // 2
                Diagnostic(ErrorCode.ERR_NoImplicitConv, "1").WithArguments("int", "C").WithLocation(8, 7),
                // (9,5): error CS8121: An expression of type 'C' cannot be handled by a pattern of type 'int'.
                //     int => 3, // 3
                Diagnostic(ErrorCode.ERR_PatternWrongType, "int").WithArguments("C", "int").WithLocation(9, 5),
                // (10,5): error CS8985: List patterns may not be used for a value of type 'C'. No suitable 'Length' or 'Count' property was found.
                //     [] => 4, // 4
                Diagnostic(ErrorCode.ERR_ListPatternRequiresLength, "[]").WithArguments("C").WithLocation(10, 5),
                // (10,5): error CS0518: Predefined type 'System.Index' is not defined or imported
                //     [] => 4, // 4
                Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound, "[]").WithArguments("System.Index").WithLocation(10, 5),
                // (10,5): error CS0021: Cannot apply indexing with [] to an expression of type 'C'
                //     [] => 4, // 4
                Diagnostic(ErrorCode.ERR_BadIndexLHS, "[]").WithArguments("C").WithLocation(10, 5),
                // (12,5): error CS0029: Cannot implicitly convert type 'string' to 'C'
                //     "" => 6, // 5
                Diagnostic(ErrorCode.ERR_NoImplicitConv, @"""""").WithArguments("string", "C").WithLocation(12, 5)
            );
        }

        private const string INumberBaseBCL = """
            namespace System
            {
                using System.Numerics;

                public class Object {}
                public class Void {}
                public class ValueType {}
                public class String {}
                public class Enum {}
                public struct Nullable<T> where T : struct {}
                public struct Byte : INumberBase<Byte> {}
                public struct SByte : INumberBase<SByte> {}
                public struct Int16 : INumberBase<Int16> {}
                public struct Char : INumberBase<Char> {}
                public struct UInt16 : INumberBase<UInt16> {}
                public struct Int32 : INumberBase<Int32> {}
                public struct UInt32 : INumberBase<UInt32> {}
                public struct Int64 : INumberBase<Int64> {}
                public struct UInt64 : INumberBase<UInt64> {}
                public struct Single : INumberBase<Single> {}
                public struct Double : INumberBase<Double> {}
                public struct Decimal : INumberBase<Decimal> { public Decimal(int value) {} }
                public struct IntPtr : INumberBase<IntPtr> {}
                public struct UIntPtr : INumberBase<UIntPtr> {}
            }
            """;

        [Theory]
        [InlineData("byte")]
        [InlineData("sbyte")]
        [InlineData("short")]
        [InlineData("ushort")]
        [InlineData("int")]
        [InlineData("uint")]
        [InlineData("nint")]
        [InlineData("nuint")]
        [InlineData("long")]
        [InlineData("ulong")]
        [InlineData("float")]
        [InlineData("double")]
        [InlineData("decimal")]
        public void MatchingOnConstantConversionsWithINumberBaseIsAllowed(string inputType)
        {
            var source = $$"""
                {{inputType}} i = 1;
                _ = i switch
                {
                    1 => 1,
                    > 1 => 2,
                    _ => 3
                };
                """;

            var comp = CreateEmptyCompilation(new[] { source, INumberBaseBCL, INumberBaseDefinition });
            comp.VerifyDiagnostics();
        }

        [Theory]
        [InlineData("byte")]
        [InlineData("sbyte")]
        [InlineData("short")]
        [InlineData("ushort")]
        [InlineData("int")]
        [InlineData("uint")]
        [InlineData("nint")]
        [InlineData("nuint")]
        [InlineData("long")]
        [InlineData("ulong")]
        [InlineData("float")]
        [InlineData("double")]
        [InlineData("decimal")]
        public void MatchingOnConstantConversionsWithINumberBaseIsAllowed_Nullable(string inputType)
        {
            var source = $$"""
                {{inputType}}? i = 1;
                _ = i switch
                {
                    1 => 1,
                    > 1 => 2,
                    _ => 3
                };
                """;

            var comp = CreateEmptyCompilation(new[] { source, INumberBaseBCL, INumberBaseDefinition });
            comp.VerifyDiagnostics();
        }

        [Fact]
        public void MatchingOnConstantConversionsWithINumberBaseIsDisallowed_TypePatternToINumberBaseInt()
        {
            var source = """
                using System.Numerics;
                int i = 1;
                _ = ((INumberBase<int>)i) switch
                {
                    1 => 1,
                    > 1 => 2,
                    _ => 3
                };
                """;

            var comp = CreateEmptyCompilation(new[] { source, INumberBaseBCL, INumberBaseDefinition });
            comp.VerifyDiagnostics(
                // (5,5): error CS9060: Cannot use a numeric constant or relational pattern on 'INumberBase<int>' because it inherits from or extends 'INumberBase<T>'. Consider using a type pattern to narrow to a specific numeric type.
                //     1 => 1,
                Diagnostic(ErrorCode.ERR_CannotMatchOnINumberBase, "1").WithArguments("System.Numerics.INumberBase<int>").WithLocation(5, 5),
                // (6,5): error CS9060: Cannot use a numeric constant or relational pattern on 'INumberBase<int>' because it inherits from or extends 'INumberBase<T>'. Consider using a type pattern to narrow to a specific numeric type.
                //     > 1 => 2,
                Diagnostic(ErrorCode.ERR_CannotMatchOnINumberBase, "> 1").WithArguments("System.Numerics.INumberBase<int>").WithLocation(6, 5)
            );
        }

        [Theory]
        [InlineData("byte")]
        [InlineData("sbyte")]
        [InlineData("short")]
        [InlineData("ushort")]
        [InlineData("uint")]
        [InlineData("nint")]
        [InlineData("nuint")]
        [InlineData("long")]
        [InlineData("ulong")]
        [InlineData("float")]
        [InlineData("double")]
        [InlineData("decimal")]
        public void MatchingOnConstantConversionsWithINumberBaseIsAllowed_TypePatternToINumberBaseT(string inputType)
        {
            var source = $$"""
                using System.Numerics;
                {{inputType}} i = 1;
                _ = ((INumberBase<{{inputType}}>)i) switch
                {
                    1 => 1,
                    > 1 => 2,
                    _ => 3
                };
                """;

            var comp = CreateEmptyCompilation(new[] { source, INumberBaseBCL, INumberBaseDefinition });
            comp.VerifyDiagnostics(
                // (5,5): error CS0029: Cannot implicitly convert type 'int' to 'System.Numerics.INumberBase<nint>'
                //     1 => 1,
                Diagnostic(ErrorCode.ERR_NoImplicitConv, "1").WithArguments("int", $"System.Numerics.INumberBase<{inputType}>").WithLocation(5, 5),
                // (6,5): error CS8781: Relational patterns may not be used for a value of type 'System.Numerics.INumberBase<nint>'.
                //     > 1 => 2,
                Diagnostic(ErrorCode.ERR_UnsupportedTypeForRelationalPattern, "> 1").WithArguments($"System.Numerics.INumberBase<{inputType}>").WithLocation(6, 5),
                // (6,7): error CS0029: Cannot implicitly convert type 'int' to 'System.Numerics.INumberBase<nint>'
                //     > 1 => 2,
                Diagnostic(ErrorCode.ERR_NoImplicitConv, "1").WithArguments("int", $"System.Numerics.INumberBase<{inputType}>").WithLocation(6, 7)
            );
        }

        [Fact]
        public void MatchingOnINumberBaseIsAllowed_ClassNotInterface()
        {
            var source = """
                #pragma warning disable 8321 // Unused local function
                using System.Numerics;

                void M<T>(T t) where T : INumberBase<T>
                {
                    int o = t switch
                    {
                        1 => 1,
                        > 1 => 2,
                        _ => 3
                    };
                }

                namespace System.Numerics
                {
                    public class INumberBase<T> where T : INumberBase<T>
                    {
                    }
                }
                """;

            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics();
        }

        [Fact]
        public void MatchingOnINumberBaseIsAllowed_WrongArity()
        {
            var source = """
                #pragma warning disable 8321 // Unused local function
                using System.Numerics;

                void M1<T1, T2>(T1 t) where T1 : INumberBase<T1, T2>
                {
                    int o = t switch
                    {
                        1 => 1,
                        > 1 => 2,
                        _ => 3
                    };
                }

                void M2<T>(T t) where T : INumberBase
                {
                    int o = t switch
                    {
                        1 => 1,
                        > 1 => 2,
                        _ => 3
                    };
                }

                namespace System.Numerics
                {
                    public interface INumberBase<T1, T2> where T1 : INumberBase<T1, T2>
                    {
                    }

                    public interface INumberBase
                    {
                    }
                }
                """;

            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics();
        }

        [Fact]
        public void MatchingOnINumberBaseIsAllowed_WrongNamespace()
        {
            var source = """
                #pragma warning disable 8321 // Unused local function
                using System;

                void M<T>(T t) where T : INumberBase<T>
                {
                    int o = t switch
                    {
                        1 => 1,
                        > 1 => 2,
                        _ => 3
                    };
                }

                namespace System
                {
                    public interface INumberBase<T> where T : INumberBase<T>
                    {
                    }
                }
                """;

            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics();
        }

        [Fact]
        public void MatchingOnINumberBaseIsAllowed_NestedType()
        {
            var source = """
                #pragma warning disable 8321 // Unused local function
                using static System.Numerics.Outer;

                void M<T>(T t) where T : INumberBase<T>
                {
                    int o = t switch
                    {
                        1 => 1,
                        > 1 => 2,
                        _ => 3
                    };
                }

                namespace System.Numerics
                {
                    public interface Outer
                    {
                        public interface INumberBase<T> where T : INumberBase<T>
                        {
                        }
                    }
                }
                """;

            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics();
        }

        [Fact, WorkItem(64399, "https://github.com/dotnet/roslyn/issues/64399")]
        public void ShortestPathToDefaultNodeYieldsNoRemainingValues()
        {
            var source = """
public enum Enum
{
    Zero = 0,
    One = 1,
    Two = 2,
}

class N
{
    private static int M(Enum e1, Enum e2)
    {
        return (e1, e2) switch
        {
            (Enum.Two, _) => 0,
            (_, Enum.Two) => 0,
            (Enum.Zero, Enum.Zero) => 0,
            (Enum.Zero, Enum.One) => 0,
            (Enum.One, Enum.Zero) => 0,
            ( < 0 or > Enum.Two, _) => 0,
            (_, < 0 or > Enum.Two) => 0,
        };
    }
}
""";

            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (12,25): warning CS8509: The switch expression does not handle all possible values of its input type (it is not exhaustive). For example, the pattern '(Enum.One, Enum.One)' is not covered.
                //         return (e1, e2) switch
                Diagnostic(ErrorCode.WRN_SwitchExpressionNotExhaustive, "switch").WithArguments("(Enum.One, Enum.One)").WithLocation(12, 25)
                );
        }

        [Fact, WorkItem(64399, "https://github.com/dotnet/roslyn/issues/64399")]
        public void ShortestPathToDefaultNodeYieldsNoRemainingValues_RequiringFalseWhenClause()
        {
            var source = """
public enum Enum
{
    Zero = 0,
    One = 1,
    Two = 2,
}

class N
{
    private static int M(Enum e1, Enum e2, bool b)
    {
        return (e1, e2) switch
        {
            (Enum.One, Enum.One) when b => 0,
            (Enum.Two, _) => 0,
            (_, Enum.Two) => 0,
            (Enum.Zero, Enum.Zero) => 0,
            (Enum.Zero, Enum.One) => 0,
            (Enum.One, Enum.Zero) => 0,
            ( < 0 or > Enum.Two, _) => 0,
            (_, < 0 or > Enum.Two) => 0,
        };
    }
}
""";

            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (12,25): warning CS8846: The switch expression does not handle all possible values of its input type (it is not exhaustive). For example, the pattern '(Enum.One, Enum.One)' is not covered. However, a pattern with a 'when' clause might successfully match this value.
                //         return (e1, e2) switch
                Diagnostic(ErrorCode.WRN_SwitchExpressionNotExhaustiveWithWhen, "switch").WithArguments("(Enum.One, Enum.One)").WithLocation(12, 25)
                );
        }

        [Fact, WorkItem(64399, "https://github.com/dotnet/roslyn/issues/64399")]
        public void ShortestPathToDefaultNodeYieldsNoRemainingValues_NullBranch()
        {
            var source = """
public enum Enum
{
    Zero = 0,
    One = 1,
    Two = 2,
}

class N
{
    private static int M(Enum e1, Enum e2, object o)
    {
        return (e1, e2, o) switch
        {
            (Enum.One, Enum.One, _) => 0,
            (Enum.Two, _, _) => 0,
            (_, Enum.Two, _) => 0,
            (Enum.Zero, Enum.Zero, _) => 0,
            (Enum.Zero, Enum.One, _) => 0,
            (Enum.One, Enum.Zero, _) => 0,
            ( < 0 or > Enum.Two, _, _) => 0,
            (_, < 0 or > Enum.Two, string s) => 0,
        };
    }
}
""";

            var comp = CreateCompilation(source);

            VerifyDecisionDagDump<SwitchExpressionSyntax>(comp,
@"[0]: t1 = t0.e1; [1]
[1]: t1 == 1 ? [2] : [8]
[2]: t2 = t0.e2; [3]
[3]: t2 == 1 ? [4] : [5]
[4]: leaf <arm> `(Enum.One, Enum.One, _) => 0`
[5]: t2 == 2 ? [12] : [6]
[6]: t2 == 0 ? [7] : [18]
[7]: leaf <arm> `(Enum.One, Enum.Zero, _) => 0`
[8]: t1 == 2 ? [9] : [10]
[9]: leaf <arm> `(Enum.Two, _, _) => 0`
[10]: t2 = t0.e2; [11]
[11]: t2 == 2 ? [12] : [13]
[12]: leaf <arm> `(_, Enum.Two, _) => 0`
[13]: t1 == 0 ? [14] : [24]
[14]: t2 == 0 ? [15] : [16]
[15]: leaf <arm> `(Enum.Zero, Enum.Zero, _) => 0`
[16]: t2 == 1 ? [17] : [18]
[17]: leaf <arm> `(Enum.Zero, Enum.One, _) => 0`
[18]: t3 = t0.o; [19]
[19]: t3 is string ? [20] : [23]
[20]: t4 = (string)t3; [21]
[21]: when <true> ? [22] : <unreachable>
[22]: leaf <arm> `(_, < 0 or > Enum.Two, string s) => 0`
[23]: leaf <default> `(e1, e2, o) switch
        {
            (Enum.One, Enum.One, _) => 0,
            (Enum.Two, _, _) => 0,
            (_, Enum.Two, _) => 0,
            (Enum.Zero, Enum.Zero, _) => 0,
            (Enum.Zero, Enum.One, _) => 0,
            (Enum.One, Enum.Zero, _) => 0,
            ( < 0 or > Enum.Two, _, _) => 0,
            (_, < 0 or > Enum.Two, string s) => 0,
        }`
[24]: leaf <arm> `( < 0 or > Enum.Two, _, _) => 0`
");

            comp.VerifyDiagnostics(
                // (12,28): warning CS8524: The switch expression does not handle some values of its input type (it is not exhaustive) involving an unnamed enum value. For example, the pattern '(Enum.One, (Enum)3, _)' is not covered.
                //         return (e1, e2, o) switch
                Diagnostic(ErrorCode.WRN_SwitchExpressionNotExhaustiveWithUnnamedEnumValue, "switch").WithArguments("(Enum.One, (Enum)3, _)").WithLocation(12, 28)
                );
        }

        [Fact, WorkItem(64399, "https://github.com/dotnet/roslyn/issues/64399")]
        public void ShortestPathToDefaultNodeYieldsNoRemainingValues_Nullability()
        {
            var source = """
#nullable enable

public enum Enum
{
    Zero = 0,
    One = 1,
    Two = 2,
}

class N
{
    private static int M(Enum e1, Enum e2, string s)
    {
        return (e1, e2, s) switch
        {
            (Enum.Two, _, _) => 0,
            (_, Enum.Two, _) => 0,
            (Enum.Zero, Enum.Zero, _) => 0,
            (Enum.Zero, Enum.One, _) => 0,
            (Enum.One, Enum.Zero, _) => 0,
            ( < 0 or > Enum.Two, _, _) => 0,
            (_, < 0 or > Enum.Two, _) => 0,
            (_, _, string) => 0,
        };
    }
}
""";

            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics();
        }

        [Fact, WorkItem(64399, "https://github.com/dotnet/roslyn/issues/64399")]
        public void ShortestPathToDefaultNodeYieldsNoRemainingValues_Nullability_NullableString()
        {
            var source = """
#nullable enable

public enum Enum
{
    Zero = 0,
    One = 1,
    Two = 2,
}

class N
{
    private static int M1(Enum e1, Enum e2, string? s)
    {
        return (e1, e2, s) switch // 1
        {
            (Enum.Two, _, _) => 0,
            (_, Enum.Two, _) => 0,
            (Enum.Zero, Enum.Zero, _) => 0,
            (Enum.Zero, Enum.One, _) => 0,
            (Enum.One, Enum.Zero, _) => 0,
            ( < 0 or > Enum.Two, _, _) => 0,
            (_, < 0 or > Enum.Two, null) => 0,
            (_, _, null) => 1,
        };
    }

    private static int M2(Enum e1, Enum e2, string? s)
    {
        return (e1, e2, s) switch // 2
        {
            (Enum.Two, _, _) => 0,
            (_, Enum.Two, _) => 0,
            (Enum.Zero, Enum.Zero, _) => 0,
            (Enum.Zero, Enum.One, _) => 0,
            (Enum.One, Enum.Zero, _) => 0,
            ( < 0 or > Enum.Two, _, _) => 0,
            (_, < 0 or > Enum.Two, _) => 0,
            (_, _, "") => 1,
        };
    }

    private static int M3(Enum e1, Enum e2, string? s)
    {
        return (e1, e2, s) switch // 3
        {
            (Enum.Two, _, _) => 0,
            (_, Enum.Two, _) => 0,
            (Enum.Zero, Enum.Zero, _) => 0,
            (Enum.Zero, Enum.One, _) => 0,
            (Enum.One, Enum.Zero, _) => 0,
            ( < 0 or > Enum.Two, _, _) => 0,
            (_, < 0 or > Enum.Two, _) => 0,
            (_, _, string) => 1,
        };
    }

    private static int M4(Enum e1, Enum e2, string? s)
    {
        return (e1, e2, s) switch // 4
        {
            (Enum.Two, _, _) => 0,
            (_, Enum.Two, _) => 0,
            (Enum.Zero, Enum.Zero, _) => 0,
            (Enum.Zero, Enum.One, _) => 0,
            (Enum.One, Enum.Zero, _) => 0,
            ( < 0 or > Enum.Two, _, _) => 0,
            (_, < 0 or > Enum.Two, null) => 1,
        };
    }

    private static int M5(Enum e1, Enum e2, string? s)
    {
        return (e1, e2, s) switch // 5
        {
            (Enum.Two, _, _) => 0,
            (_, Enum.Two, _) => 0,
            (Enum.Zero, Enum.Zero, _) => 0,
            (Enum.Zero, Enum.One, _) => 0,
            (Enum.One, Enum.Zero, _) => 0,
            ( < 0 or > Enum.Two, _, _) => 0,
            (_, < 0 or > Enum.Two, "") => 1,
        };
    }

    private static int M6(Enum e1, Enum e2, string? s)
    {
        return (e1, e2, s) switch // 6
        {
            (Enum.Two, _, _) => 0,
            (_, Enum.Two, _) => 0,
            (Enum.Zero, Enum.Zero, _) => 0,
            (Enum.Zero, Enum.One, _) => 0,
            (Enum.One, Enum.Zero, _) => 0,
            ( < 0 or > Enum.Two, _, _) => 0,
            (_, < 0 or > Enum.Two, string) => 1,
        };
    }
}
""";

            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (14,28): warning CS8524: The switch expression does not handle some values of its input type (it is not exhaustive) involving an unnamed enum value. For example, the pattern '(Enum.Zero, (Enum)-1, not null)' is not covered.
                //         return (e1, e2, s) switch // 1
                Diagnostic(ErrorCode.WRN_SwitchExpressionNotExhaustiveWithUnnamedEnumValue, "switch").WithArguments("(Enum.Zero, (Enum)-1, not null)").WithLocation(14, 28),
                // (29,28): warning CS8509: The switch expression does not handle all possible values of its input type (it is not exhaustive). For example, the pattern '(Enum.One, Enum.One, "A")' is not covered.
                //         return (e1, e2, s) switch // 2
                Diagnostic(ErrorCode.WRN_SwitchExpressionNotExhaustive, "switch").WithArguments("(Enum.One, Enum.One, \"A\")").WithLocation(29, 28),
                // (44,28): warning CS8655: The switch expression does not handle some null inputs (it is not exhaustive). For example, the pattern '(Enum.One, Enum.One, null)' is not covered.
                //         return (e1, e2, s) switch // 3
                Diagnostic(ErrorCode.WRN_SwitchExpressionNotExhaustiveForNull, "switch").WithArguments("(Enum.One, Enum.One, null)").WithLocation(44, 28),
                // (59,28): warning CS8524: The switch expression does not handle some values of its input type (it is not exhaustive) involving an unnamed enum value. For example, the pattern '(Enum.Zero, (Enum)-1, not null)' is not covered.
                //         return (e1, e2, s) switch // 4
                Diagnostic(ErrorCode.WRN_SwitchExpressionNotExhaustiveWithUnnamedEnumValue, "switch").WithArguments("(Enum.Zero, (Enum)-1, not null)").WithLocation(59, 28),
                // (73,28): warning CS8524: The switch expression does not handle some values of its input type (it is not exhaustive) involving an unnamed enum value. For example, the pattern '(Enum.Zero, (Enum)-1, "A")' is not covered.
                //         return (e1, e2, s) switch // 5
                Diagnostic(ErrorCode.WRN_SwitchExpressionNotExhaustiveWithUnnamedEnumValue, "switch").WithArguments("(Enum.Zero, (Enum)-1, \"A\")").WithLocation(73, 28),
                // (87,28): warning CS8509: The switch expression does not handle all possible values of its input type (it is not exhaustive). For example, the pattern '(Enum.One, Enum.One, _)' is not covered.
                //         return (e1, e2, s) switch // 6
                Diagnostic(ErrorCode.WRN_SwitchExpressionNotExhaustive, "switch").WithArguments("(Enum.One, Enum.One, _)").WithLocation(87, 28)
                );
        }

        [Theory, CombinatorialData, WorkItem(64399, "https://github.com/dotnet/roslyn/issues/64399")]
        public void ShortestPathToDefaultNodeYieldsNoRemainingValues_Nullability_Deconstruction(bool nullableEnable)
        {
            var source = """
public enum Enum
{
    Zero = 0,
    One = 1,
    Two = 2,
}

class N
{
    private int M1()
    {
        return this switch // 1
        {
            (Enum.Two, _, _) => 0,
            (_, Enum.Two, _) => 0,
            (Enum.Zero, Enum.Zero, _) => 0,
            (Enum.Zero, Enum.One, _) => 0,
            (Enum.One, Enum.Zero, _) => 0,
            ( < 0 or > Enum.Two, _, _) => 0,
            (_, < 0 or > Enum.Two, _) => 0,
            (_, _, not null) => 1,
        };
    }

    private int M2()
    {
        return this switch // 2
        {
            (Enum.Two, _, _) => 0,
            (_, Enum.Two, _) => 0,
            (Enum.Zero, Enum.Zero, _) => 0,
            (Enum.Zero, Enum.One, _) => 0,
            (Enum.One, Enum.Zero, _) => 0,
            ( < 0 or > Enum.Two, _, _) => 0,
            (_, < 0 or > Enum.Two, _) => 0,
            (_, _, true) => 1,
            (_, _, false) => 1,
        };
    }

    private int M3()
    {
        return this switch // 3
        {
            null => 1,
            (Enum.Two, _, _) => 0,
            (_, Enum.Two, _) => 0,
            (Enum.Zero, Enum.Zero, _) => 0,
            (Enum.Zero, Enum.One, _) => 0,
            (Enum.One, Enum.Zero, _) => 0,
            ( < 0 or > Enum.Two, _, _) => 0,
            (_, < 0 or > Enum.Two, _) => 0,
            (_, _, true) => 1,
            (_, _, false) => 1,
        };
    }

    private int M4()
    {
        return this switch // 4
        {
            null => 1,
            (Enum.Two, _, _) => 0,
            (_, Enum.Two, _) => 0,
            (Enum.Zero, Enum.Zero, _) => 0,
            (Enum.Zero, Enum.One, _) => 0,
            (Enum.One, Enum.Zero, _) => 0,
            ( < 0 or > Enum.Two, _, _) => 0,
            (_, < 0 or > Enum.Two, _) => 0,
            (_, _, true) => 1,
        };
    }

    private int M5()
    {
        return this switch // 5
        {
            (Enum.Two, _, _) => 0,
            (_, Enum.Two, _) => 0,
            (Enum.Zero, Enum.Zero, _) => 0,
            (Enum.Zero, Enum.One, _) => 0,
            (Enum.One, Enum.Zero, _) => 0,
            ( < 0 or > Enum.Two, _, _) => 0,
            (_, < 0 or > Enum.Two, not null) => 1,
        };
    }

    private int M6()
    {
        return this switch // 6
        {
            (Enum.Two, _, _) => 0,
            (_, Enum.Two, _) => 0,
            (Enum.Zero, Enum.Zero, _) => 0,
            (Enum.Zero, Enum.One, _) => 0,
            (Enum.One, Enum.Zero, _) => 0,
            ( < 0 or > Enum.Two, _, _) => 0,
            (_, < 0 or > Enum.Two, true) => 1,
            (_, < 0 or > Enum.Two, false) => 1,
        };
    }

    private int M7()
    {
        return this switch // 7
        {
            null => 1,
            (Enum.Two, _, _) => 0,
            (_, Enum.Two, _) => 0,
            (Enum.Zero, Enum.Zero, _) => 0,
            (Enum.Zero, Enum.One, _) => 0,
            (Enum.One, Enum.Zero, _) => 0,
            ( < 0 or > Enum.Two, _, _) => 0,
            (_, < 0 or > Enum.Two, true) => 1,
            (_, < 0 or > Enum.Two, false) => 1,
        };
    }

    private int M8()
    {
        return this switch // 8
        {
            null => 1,
            (Enum.Two, _, _) => 0,
            (_, Enum.Two, _) => 0,
            (Enum.Zero, Enum.Zero, _) => 0,
            (Enum.Zero, Enum.One, _) => 0,
            (Enum.One, Enum.Zero, _) => 0,
            ( < 0 or > Enum.Two, _, _) => 0,
            (_, < 0 or > Enum.Two, true) => 1,
        };
    }

    void Deconstruct(out Enum e1, out Enum e2, out bool? s) => throw null!;
}
""";

            var comp = CreateCompilation(source, options: TestOptions.DebugDll.WithNullableContextOptions(nullableEnable ? NullableContextOptions.Enable : NullableContextOptions.Disable));
            if (nullableEnable)
            {
                comp.VerifyDiagnostics(
                    // (12,21): warning CS8655: The switch expression does not handle some null inputs (it is not exhaustive). For example, the pattern 'null' is not covered.
                    //         return this switch // 1
                    Diagnostic(ErrorCode.WRN_SwitchExpressionNotExhaustiveForNull, "switch").WithArguments("null").WithLocation(12, 21),
                    // (27,21): warning CS8655: The switch expression does not handle some null inputs (it is not exhaustive). For example, the pattern 'null' is not covered.
                    //         return this switch // 2
                    Diagnostic(ErrorCode.WRN_SwitchExpressionNotExhaustiveForNull, "switch").WithArguments("null").WithLocation(27, 21),
                    // (43,21): warning CS8655: The switch expression does not handle some null inputs (it is not exhaustive). For example, the pattern '(Enum.One, Enum.One, null)' is not covered.
                    //         return this switch // 3
                    Diagnostic(ErrorCode.WRN_SwitchExpressionNotExhaustiveForNull, "switch").WithArguments("(Enum.One, Enum.One, null)").WithLocation(43, 21),
                    // (60,21): warning CS8509: The switch expression does not handle all possible values of its input type (it is not exhaustive). For example, the pattern '(Enum.One, Enum.One, false)' is not covered.
                    //         return this switch // 4
                    Diagnostic(ErrorCode.WRN_SwitchExpressionNotExhaustive, "switch").WithArguments("(Enum.One, Enum.One, false)").WithLocation(60, 21),
                    // (76,21): warning CS8509: The switch expression does not handle all possible values of its input type (it is not exhaustive). For example, the pattern '(Enum.One, Enum.One, _)' is not covered.
                    //         return this switch // 5
                    Diagnostic(ErrorCode.WRN_SwitchExpressionNotExhaustive, "switch").WithArguments("(Enum.One, Enum.One, _)").WithLocation(76, 21),
                    // (90,21): warning CS8509: The switch expression does not handle all possible values of its input type (it is not exhaustive). For example, the pattern '(Enum.One, Enum.One, _)' is not covered.
                    //         return this switch // 6
                    Diagnostic(ErrorCode.WRN_SwitchExpressionNotExhaustive, "switch").WithArguments("(Enum.One, Enum.One, _)").WithLocation(90, 21),
                    // (105,21): warning CS8509: The switch expression does not handle all possible values of its input type (it is not exhaustive). For example, the pattern '(Enum.One, Enum.One, _)' is not covered.
                    //         return this switch // 7
                    Diagnostic(ErrorCode.WRN_SwitchExpressionNotExhaustive, "switch").WithArguments("(Enum.One, Enum.One, _)").WithLocation(105, 21),
                    // (121,21): warning CS8524: The switch expression does not handle some values of its input type (it is not exhaustive) involving an unnamed enum value. For example, the pattern '(Enum.Zero, (Enum)-1, false)' is not covered.
                    //         return this switch // 8
                    Diagnostic(ErrorCode.WRN_SwitchExpressionNotExhaustiveWithUnnamedEnumValue, "switch").WithArguments("(Enum.Zero, (Enum)-1, false)").WithLocation(121, 21)
                    );
            }
            else
            {
                comp.VerifyDiagnostics(
                    // (60,21): warning CS8509: The switch expression does not handle all possible values of its input type (it is not exhaustive). For example, the pattern '(Enum.One, Enum.One, false)' is not covered.
                    //         return this switch // 4
                    Diagnostic(ErrorCode.WRN_SwitchExpressionNotExhaustive, "switch").WithArguments("(Enum.One, Enum.One, false)").WithLocation(60, 21),
                    // (76,21): warning CS8509: The switch expression does not handle all possible values of its input type (it is not exhaustive). For example, the pattern '(Enum.One, Enum.One, _)' is not covered.
                    //         return this switch // 5
                    Diagnostic(ErrorCode.WRN_SwitchExpressionNotExhaustive, "switch").WithArguments("(Enum.One, Enum.One, _)").WithLocation(76, 21),
                    // (90,21): warning CS8509: The switch expression does not handle all possible values of its input type (it is not exhaustive). For example, the pattern '(Enum.One, Enum.One, _)' is not covered.
                    //         return this switch // 6
                    Diagnostic(ErrorCode.WRN_SwitchExpressionNotExhaustive, "switch").WithArguments("(Enum.One, Enum.One, _)").WithLocation(90, 21),
                    // (105,21): warning CS8509: The switch expression does not handle all possible values of its input type (it is not exhaustive). For example, the pattern '(Enum.One, Enum.One, _)' is not covered.
                    //         return this switch // 7
                    Diagnostic(ErrorCode.WRN_SwitchExpressionNotExhaustive, "switch").WithArguments("(Enum.One, Enum.One, _)").WithLocation(105, 21),
                    // (121,21): warning CS8524: The switch expression does not handle some values of its input type (it is not exhaustive) involving an unnamed enum value. For example, the pattern '(Enum.Zero, (Enum)-1, false)' is not covered.
                    //         return this switch // 8
                    Diagnostic(ErrorCode.WRN_SwitchExpressionNotExhaustiveWithUnnamedEnumValue, "switch").WithArguments("(Enum.Zero, (Enum)-1, false)").WithLocation(121, 21)
                    );
            }
        }

        [Theory, CombinatorialData, WorkItem(64399, "https://github.com/dotnet/roslyn/issues/64399")]
        public void ShortestPathToDefaultNodeYieldsNoRemainingValues_Nullability_NullableBool(bool nullableEnable)
        {
            var source = """
public enum Enum
{
    Zero = 0,
    One = 1,
    Two = 2,
}

class N
{
    private static int M1(Enum e1, Enum e2, bool? i)
    {
        return (e1, e2, i) switch // 1
        {
            (Enum.Two, _, _) => 0,
            (_, Enum.Two, _) => 0,
            (Enum.Zero, Enum.Zero, _) => 0,
            (Enum.Zero, Enum.One, _) => 0,
            (Enum.One, Enum.Zero, _) => 0,
            ( < 0 or > Enum.Two, _, _) => 0,
            (_, < 0 or > Enum.Two, _) => 0,
            (_, _, true) => 1,
        };
    }

    private static int M2(Enum e1, Enum e2, bool? i)
    {
        return (e1, e2, i) switch // 2
        {
            (Enum.Two, _, _) => 0,
            (_, Enum.Two, _) => 0,
            (Enum.Zero, Enum.Zero, _) => 0,
            (Enum.Zero, Enum.One, _) => 0,
            (Enum.One, Enum.Zero, _) => 0,
            ( < 0 or > Enum.Two, _, _) => 0,
            (_, < 0 or > Enum.Two, _) => 0,
            (_, _, true) => 1,
            (_, _, false) => 1,
        };
    }
}
""";
            var comp = CreateCompilation(source, options: TestOptions.DebugDll.WithNullableContextOptions(nullableEnable ? NullableContextOptions.Enable : NullableContextOptions.Disable));
            if (nullableEnable)
            {
                comp.VerifyDiagnostics(
                    // (12,28): warning CS8509: The switch expression does not handle all possible values of its input type (it is not exhaustive). For example, the pattern '(Enum.One, Enum.One, false)' is not covered.
                    //         return (e1, e2, i) switch // 1
                    Diagnostic(ErrorCode.WRN_SwitchExpressionNotExhaustive, "switch").WithArguments("(Enum.One, Enum.One, false)").WithLocation(12, 28),
                    // (27,28): warning CS8655: The switch expression does not handle some null inputs (it is not exhaustive). For example, the pattern '(Enum.One, Enum.One, null)' is not covered.
                    //         return (e1, e2, i) switch // 2
                    Diagnostic(ErrorCode.WRN_SwitchExpressionNotExhaustiveForNull, "switch").WithArguments("(Enum.One, Enum.One, null)").WithLocation(27, 28)
                    );
            }
            else
            {
                comp.VerifyDiagnostics(
                    // (12,28): warning CS8509: The switch expression does not handle all possible values of its input type (it is not exhaustive). For example, the pattern '(Enum.One, Enum.One, false)' is not covered.
                    //         return (e1, e2, i) switch // 1
                    Diagnostic(ErrorCode.WRN_SwitchExpressionNotExhaustive, "switch").WithArguments("(Enum.One, Enum.One, false)").WithLocation(12, 28)
                    );
            }
        }

        [Fact, WorkItem(45679, "https://github.com/dotnet/roslyn/issues/45679")]
        public void IsExpression_SwitchDispatch_Numeric()
        {
            var source = """
class C
{
    public static void Main()
    {
        System.Console.Write(
              Test(0) == false
            & Test(1)
            & Test(2)
            & Test(3)
            & Test(4)
            & Test(5)
            & Test(6)
            & Test(7)
            & Test(8)
        );
    }  
    public static bool Test(int a)
    {
        return (a is 1 or 2 or 3 or 4 or 5 or 6 or 7 or 8);
    }
}
""";
            var compilation = CompileAndVerify(source, expectedOutput: "True");
            compilation.VerifyIL("C.Test", """
{
  // Code size       14 (0xe)
  .maxstack  2
  .locals init (bool V_0)
  IL_0000:  ldarg.0
  IL_0001:  ldc.i4.1
  IL_0002:  sub
  IL_0003:  ldc.i4.7
  IL_0004:  bgt.un.s   IL_000a
  IL_0006:  ldc.i4.1
  IL_0007:  stloc.0
  IL_0008:  br.s       IL_000c
  IL_000a:  ldc.i4.0
  IL_000b:  stloc.0
  IL_000c:  ldloc.0
  IL_000d:  ret
}
""");
        }

        [Fact, WorkItem(45679, "https://github.com/dotnet/roslyn/issues/45679")]
        public void IsExpression_SwitchDispatch_SwitchIL()
        {
            var source = """
class C
{
    public static void Main()
    {
        System.Console.Write(
              Test(1, null) == false
            & Test(1, default(int))
            & Test(2, default(bool))
            & Test(3, default(double))
            & Test(4, default(long))
            & Test(5, default(long)) == false
        );
    }
    public static bool Test(int a, object b)
    {
        return (a, b) is 
                (1, int) or
                (2, bool) or
                (3, double) or
                (4, long);
    }
}
""";
            var compilation = CompileAndVerify(source, expectedOutput: "True");
            compilation.VerifyIL("C.Test", """
{
  // Code size       72 (0x48)
  .maxstack  2
  .locals init (bool V_0)
  IL_0000:  ldarg.0
  IL_0001:  ldc.i4.1
  IL_0002:  sub
  IL_0003:  switch    (
        IL_001a,
        IL_0024,
        IL_002e,
        IL_0038)
  IL_0018:  br.s       IL_0044
  IL_001a:  ldarg.1
  IL_001b:  isinst     "int"
  IL_0020:  brtrue.s   IL_0040
  IL_0022:  br.s       IL_0044
  IL_0024:  ldarg.1
  IL_0025:  isinst     "bool"
  IL_002a:  brtrue.s   IL_0040
  IL_002c:  br.s       IL_0044
  IL_002e:  ldarg.1
  IL_002f:  isinst     "double"
  IL_0034:  brtrue.s   IL_0040
  IL_0036:  br.s       IL_0044
  IL_0038:  ldarg.1
  IL_0039:  isinst     "long"
  IL_003e:  brfalse.s  IL_0044
  IL_0040:  ldc.i4.1
  IL_0041:  stloc.0
  IL_0042:  br.s       IL_0046
  IL_0044:  ldc.i4.0
  IL_0045:  stloc.0
  IL_0046:  ldloc.0
  IL_0047:  ret
}
""");
        }

        [Fact, WorkItem(45679, "https://github.com/dotnet/roslyn/issues/45679")]
        public void IsExpression_SwitchDispatch_String()
        {
            var source = """
class C
{
    public static void Main()
    {
        System.Console.Write(
              Test("0") == false
            & Test("1")
            & Test("2")
            & Test("3")
            & Test("4")
            & Test("5")
            & Test("6")
            & Test("7")
            & Test("8")
        );
    }  
    public static bool Test(string a)
    {
        return (a is "1" or "2" or "3" or "4" or "5" or "6" or "7" or "8");
    }
}
""";
            var compilation = CompileAndVerify(source, expectedOutput: "True");
            compilation.VerifyIL("C.Test", """
{
  // Code size       73 (0x49)
  .maxstack  2
  .locals init (bool V_0,
                int V_1,
                char V_2)
  IL_0000:  ldarg.0
  IL_0001:  brfalse.s  IL_0045
  IL_0003:  ldarg.0
  IL_0004:  call       "int string.Length.get"
  IL_0009:  stloc.1
  IL_000a:  ldloc.1
  IL_000b:  ldc.i4.1
  IL_000c:  bne.un.s   IL_0045
  IL_000e:  ldarg.0
  IL_000f:  ldc.i4.0
  IL_0010:  call       "char string.this[int].get"
  IL_0015:  stloc.2
  IL_0016:  ldloc.2
  IL_0017:  ldc.i4.s   49
  IL_0019:  sub
  IL_001a:  switch    (
        IL_0041,
        IL_0041,
        IL_0041,
        IL_0041,
        IL_0041,
        IL_0041,
        IL_0041,
        IL_0041)
  IL_003f:  br.s       IL_0045
  IL_0041:  ldc.i4.1
  IL_0042:  stloc.0
  IL_0043:  br.s       IL_0047
  IL_0045:  ldc.i4.0
  IL_0046:  stloc.0
  IL_0047:  ldloc.0
  IL_0048:  ret
}
""");
        }

        [Theory]
        [InlineData("object", "new {}")]
        [InlineData("dynamic", "new {}")]
        [InlineData("System.ValueType", "((int)x0)/2")]
        public void ConstantsExpectedInPatternExpression(string type, string expression)
        {
            var source = @$"
class Outer
{{
    bool M0({type} x0)
    {{
        return x0 is {expression};
    }}
}}
";
            CreateCompilation(source).VerifyDiagnostics(
                // (6,22): error CS0150: A constant value is expected
                //         return x0 is {expression};
                Diagnostic(ErrorCode.ERR_ConstantExpected, expression).WithLocation(6, 22)
            );
        }

        [Fact]
        [WorkItem("https://github.com/dotnet/roslyn/issues/82063")]
        public void SideeffectEvaluations_01()
        {
            var src = @"
interface I1
{
    int F {get;}
}

class C11;
class C12;
class C13(int f) : C12, I1
{
    public int F
    {
        get
        {
            System.Console.Write(""Evaluated C13.F "");
            return f;   
        }
    }
}
class C14(int f) : I1
{
    public int F
    {
        get
        {
            System.Console.Write(""Evaluated C14.F "");
            return f;   
        }
    }
}
class C15(int f) : I1
{
    public int F
    {
        get
        {
            System.Console.Write(""Evaluated C15.F "");
            return f;   
        }
    }
}

class Program
{
    static void Main()
    {
        object[] s = [null, new C11(), new C12(), new C13(1), new C14(1), new C15(1), new C13(2), new C14(2), new C15(2), new C13(3), new C14(3), new C15(3)]; 
        int[] i = [1, 2, 3];
        foreach (var s1 in s)
        {
            foreach (var j in i)
            {
                var t = Test1((s1, j));
                System.Console.WriteLine(t);
            }
        }
    }

    static bool Test1((object, int) u)
    {
        return u is (C12 and I1 and { F: 1 }, 2) or (I1 and { F: 1 or 2 }, 1);
    }   
}
";
            var comp = CreateCompilation(src, options: TestOptions.ReleaseExe);

            VerifyDecisionDagDump<IsPatternExpressionSyntax>(comp,
@"[0]: t1 = t0.Item1; [1]
[1]: t1 is C12 ? [2] : [9]
[2]: t2 = (C12)t1; [3]
[3]: t2 is I1 ? [4] : [17]
[4]: t3 = (I1)t2; [5]
[5]: t4 = t3.F; [6]
[6]: t4 == 1 ? [7] : [13]
[7]: t5 = t0.Item2; [8]
[8]: t5 == 2 ? [16] : [15]
[9]: t1 is I1 ? [10] : [17]
[10]: t3 = (I1)t1; [11]
[11]: t4 = t3.F; [12]
[12]: t4 == 1 ? [14] : [13]
[13]: t4 == 2 ? [14] : [17]
[14]: t5 = t0.Item2; [15]
[15]: t5 == 1 ? [16] : [17]
[16]: leaf <isPatternSuccess> `(C12 and I1 and { F: 1 }, 2) or (I1 and { F: 1 or 2 }, 1)`
[17]: leaf <isPatternFailure> `(C12 and I1 and { F: 1 }, 2) or (I1 and { F: 1 or 2 }, 1)`
");

            var verifier = CompileAndVerify(
                comp,
                expectedOutput: @"
False
False
False
False
False
False
False
False
False
Evaluated C13.F True
Evaluated C13.F True
Evaluated C13.F False
Evaluated C14.F True
Evaluated C14.F False
Evaluated C14.F False
Evaluated C15.F True
Evaluated C15.F False
Evaluated C15.F False
Evaluated C13.F True
Evaluated C13.F False
Evaluated C13.F False
Evaluated C14.F True
Evaluated C14.F False
Evaluated C14.F False
Evaluated C15.F True
Evaluated C15.F False
Evaluated C15.F False
Evaluated C13.F False
Evaluated C13.F False
Evaluated C13.F False
Evaluated C14.F False
Evaluated C14.F False
Evaluated C14.F False
Evaluated C15.F False
Evaluated C15.F False
Evaluated C15.F False
").VerifyDiagnostics();

            verifier.VerifyIL("Program.Test1", @"
{
  // Code size      102 (0x66)
  .maxstack  2
  .locals init (object V_0,
                C12 V_1,
                I1 V_2,
                int V_3,
                int V_4,
                bool V_5)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""object System.ValueTuple<object, int>.Item1""
  IL_0006:  stloc.0
  IL_0007:  ldloc.0
  IL_0008:  isinst     ""C12""
  IL_000d:  stloc.1
  IL_000e:  ldloc.1
  IL_000f:  brfalse.s  IL_0035
  IL_0011:  ldloc.1
  IL_0012:  isinst     ""I1""
  IL_0017:  stloc.2
  IL_0018:  ldloc.2
  IL_0019:  brfalse.s  IL_0060
  IL_001b:  ldloc.2
  IL_001c:  callvirt   ""int I1.F.get""
  IL_0021:  stloc.3
  IL_0022:  ldloc.3
  IL_0023:  ldc.i4.1
  IL_0024:  bne.un.s   IL_004a
  IL_0026:  ldarg.0
  IL_0027:  ldfld      ""int System.ValueTuple<object, int>.Item2""
  IL_002c:  stloc.s    V_4
  IL_002e:  ldloc.s    V_4
  IL_0030:  ldc.i4.2
  IL_0031:  beq.s      IL_005b
  IL_0033:  br.s       IL_0056
  IL_0035:  ldloc.0
  IL_0036:  isinst     ""I1""
  IL_003b:  stloc.2
  IL_003c:  ldloc.2
  IL_003d:  brfalse.s  IL_0060
  IL_003f:  ldloc.2
  IL_0040:  callvirt   ""int I1.F.get""
  IL_0045:  stloc.3
  IL_0046:  ldloc.3
  IL_0047:  ldc.i4.1
  IL_0048:  beq.s      IL_004e
  IL_004a:  ldloc.3
  IL_004b:  ldc.i4.2
  IL_004c:  bne.un.s   IL_0060
  IL_004e:  ldarg.0
  IL_004f:  ldfld      ""int System.ValueTuple<object, int>.Item2""
  IL_0054:  stloc.s    V_4
  IL_0056:  ldloc.s    V_4
  IL_0058:  ldc.i4.1
  IL_0059:  bne.un.s   IL_0060
  IL_005b:  ldc.i4.1
  IL_005c:  stloc.s    V_5
  IL_005e:  br.s       IL_0063
  IL_0060:  ldc.i4.0
  IL_0061:  stloc.s    V_5
  IL_0063:  ldloc.s    V_5
  IL_0065:  ret
}
");
        }

        [Fact]
        [WorkItem("https://github.com/dotnet/roslyn/issues/82063")]
        public void SideeffectEvaluations_02()
        {
            var src = @"
interface I1
{
    int F {get;}
}

class C11;
class C12;
class C13(int f) : C12, I1
{
    public int F
    {
        get
        {
            System.Console.Write(""Evaluated C13.F "");
            return f;   
        }
    }
}
class C14(int f) : I1
{
    public int F
    {
        get
        {
            System.Console.Write(""Evaluated C14.F "");
            return f;   
        }
    }
}
class C15(int f) : I1
{
    public int F
    {
        get
        {
            System.Console.Write(""Evaluated C15.F "");
            return f;   
        }
    }
}

class Program
{
    static void Main()
    {
        object[] s = [null, new C11(), new C12(), new C13(1), new C14(1), new C15(1), new C13(2), new C14(2), new C15(2), new C13(3), new C14(3), new C15(3)]; 
        int[] i = [1, 2, 3];
        foreach (var s1 in s)
        {
            foreach (var j in i)
            {
                var t = Test1((s1, j));
                System.Console.WriteLine(t);
            }
        }
    }

    static bool Test1((object, int) u)
    {
        return u is (I1 and { F: 1 or 2 }, 1) or (C12 and I1 and { F: 1 }, 2);
    }   
}
";
            var comp = CreateCompilation(src, options: TestOptions.ReleaseExe);

            VerifyDecisionDagDump<IsPatternExpressionSyntax>(comp,
@"[0]: t1 = t0.Item1; [1]
[1]: t1 is I1 ? [2] : [13]
[2]: t2 = (I1)t1; [3]
[3]: t3 = t2.F; [4]
[4]: t3 == 1 ? [5] : [9]
[5]: t4 = t0.Item2; [6]
[6]: t4 == 1 ? [12] : [7]
[7]: t1 is C12 ? [8] : [13]
[8]: t4 == 2 ? [12] : [13]
[9]: t3 == 2 ? [10] : [13]
[10]: t4 = t0.Item2; [11]
[11]: t4 == 1 ? [12] : [13]
[12]: leaf <isPatternSuccess> `(I1 and { F: 1 or 2 }, 1) or (C12 and I1 and { F: 1 }, 2)`
[13]: leaf <isPatternFailure> `(I1 and { F: 1 or 2 }, 1) or (C12 and I1 and { F: 1 }, 2)`
");

            var verifier = CompileAndVerify(
                comp,
                expectedOutput: @"
False
False
False
False
False
False
False
False
False
Evaluated C13.F True
Evaluated C13.F True
Evaluated C13.F False
Evaluated C14.F True
Evaluated C14.F False
Evaluated C14.F False
Evaluated C15.F True
Evaluated C15.F False
Evaluated C15.F False
Evaluated C13.F True
Evaluated C13.F False
Evaluated C13.F False
Evaluated C14.F True
Evaluated C14.F False
Evaluated C14.F False
Evaluated C15.F True
Evaluated C15.F False
Evaluated C15.F False
Evaluated C13.F False
Evaluated C13.F False
Evaluated C13.F False
Evaluated C14.F False
Evaluated C14.F False
Evaluated C14.F False
Evaluated C15.F False
Evaluated C15.F False
Evaluated C15.F False
").VerifyDiagnostics();

            verifier.VerifyIL("Program.Test1", @"
{
  // Code size       81 (0x51)
  .maxstack  2
  .locals init (object V_0,
                I1 V_1,
                int V_2,
                int V_3,
                bool V_4)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""object System.ValueTuple<object, int>.Item1""
  IL_0006:  stloc.0
  IL_0007:  ldloc.0
  IL_0008:  isinst     ""I1""
  IL_000d:  stloc.1
  IL_000e:  ldloc.1
  IL_000f:  brfalse.s  IL_004b
  IL_0011:  ldloc.1
  IL_0012:  callvirt   ""int I1.F.get""
  IL_0017:  stloc.2
  IL_0018:  ldloc.2
  IL_0019:  ldc.i4.1
  IL_001a:  beq.s      IL_0022
  IL_001c:  ldloc.2
  IL_001d:  ldc.i4.2
  IL_001e:  beq.s      IL_003b
  IL_0020:  br.s       IL_004b
  IL_0022:  ldarg.0
  IL_0023:  ldfld      ""int System.ValueTuple<object, int>.Item2""
  IL_0028:  stloc.3
  IL_0029:  ldloc.3
  IL_002a:  ldc.i4.1
  IL_002b:  beq.s      IL_0046
  IL_002d:  ldloc.0
  IL_002e:  isinst     ""C12""
  IL_0033:  brfalse.s  IL_004b
  IL_0035:  ldloc.3
  IL_0036:  ldc.i4.2
  IL_0037:  beq.s      IL_0046
  IL_0039:  br.s       IL_004b
  IL_003b:  ldarg.0
  IL_003c:  ldfld      ""int System.ValueTuple<object, int>.Item2""
  IL_0041:  stloc.3
  IL_0042:  ldloc.3
  IL_0043:  ldc.i4.1
  IL_0044:  bne.un.s   IL_004b
  IL_0046:  ldc.i4.1
  IL_0047:  stloc.s    V_4
  IL_0049:  br.s       IL_004e
  IL_004b:  ldc.i4.0
  IL_004c:  stloc.s    V_4
  IL_004e:  ldloc.s    V_4
  IL_0050:  ret
}
");
        }

        [Fact]
        [WorkItem("https://github.com/dotnet/roslyn/issues/82063")]
        public void SideeffectEvaluations_03()
        {
            var src = @"
interface I1
{
    int F {get;}
}

interface I2 : I1
{
}

class C11;
class C12;
class C13(int f) : C12, I1
{
    public int F
    {
        get
        {
            System.Console.Write(""Evaluated C13.F "");
            return f;   
        }
    }
}
class C14(int f) : I2
{
    public int F
    {
        get
        {
            System.Console.Write(""Evaluated C14.F "");
            return f;   
        }
    }
}
class C15(int f) : I1
{
    public int F
    {
        get
        {
            System.Console.Write(""Evaluated C15.F "");
            return f;   
        }
    }
}
class C16(int f) : C12, I2
{
    public int F
    {
        get
        {
            System.Console.Write(""Evaluated C15.F "");
            return f;   
        }
    }
}

class Program
{
    static void Main()
    {
        object[] s = [null, new C11(), new C12(), new C13(1), new C14(1), new C15(1), new C16(1), new C13(2), new C14(2), new C15(2), new C16(2), new C13(3), new C14(3), new C15(3), new C16(3)]; 
        int[] i = [1, 2, 3];
        foreach (var s1 in s)
        {
            foreach (var j in i)
            {
                var t = Test1((s1, j));
                System.Console.WriteLine(t);
            }
        }
    }

    static bool Test1((object, int) u)
    {
        return u is (C12 and I1 and { F: 1 }, 2) or (I2 and { F: 1 or 2 }, 1);
    }   
}
";
            var comp = CreateCompilation(src, options: TestOptions.ReleaseExe);

            VerifyDecisionDagDump<IsPatternExpressionSyntax>(comp,
@"[0]: t1 = t0.Item1; [1]
[1]: t1 is C12 ? [2] : [11]
[2]: t2 = (C12)t1; [3]
[3]: t2 is I1 ? [4] : [19]
[4]: t3 = (I1)t2; [5]
[5]: t4 = t3.F; [6]
[6]: t4 == 1 ? [7] : [10]
[7]: t5 = t0.Item2; [8]
[8]: t5 == 2 ? [18] : [9]
[9]: t1 is I2 ? [17] : [19]
[10]: t1 is I2 ? [15] : [19]
[11]: t1 is I2 ? [12] : [19]
[12]: t6 = (I2)t1; [13]
[13]: t4 = t6.F; [14]
[14]: t4 == 1 ? [16] : [15]
[15]: t4 == 2 ? [16] : [19]
[16]: t5 = t0.Item2; [17]
[17]: t5 == 1 ? [18] : [19]
[18]: leaf <isPatternSuccess> `(C12 and I1 and { F: 1 }, 2) or (I2 and { F: 1 or 2 }, 1)`
[19]: leaf <isPatternFailure> `(C12 and I1 and { F: 1 }, 2) or (I2 and { F: 1 or 2 }, 1)`
");

            var verifier = CompileAndVerify(
                comp,
                expectedOutput: @"
False
False
False
False
False
False
False
False
False
Evaluated C13.F False
Evaluated C13.F True
Evaluated C13.F False
Evaluated C14.F True
Evaluated C14.F False
Evaluated C14.F False
False
False
False
Evaluated C15.F True
Evaluated C15.F True
Evaluated C15.F False
Evaluated C13.F False
Evaluated C13.F False
Evaluated C13.F False
Evaluated C14.F True
Evaluated C14.F False
Evaluated C14.F False
False
False
False
Evaluated C15.F True
Evaluated C15.F False
Evaluated C15.F False
Evaluated C13.F False
Evaluated C13.F False
Evaluated C13.F False
Evaluated C14.F False
Evaluated C14.F False
Evaluated C14.F False
False
False
False
Evaluated C15.F False
Evaluated C15.F False
Evaluated C15.F False
").VerifyDiagnostics();

            verifier.VerifyIL("Program.Test1", @"
{
  // Code size      123 (0x7b)
  .maxstack  2
  .locals init (object V_0,
                C12 V_1,
                I1 V_2,
                int V_3,
                int V_4,
                I2 V_5,
                bool V_6)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""object System.ValueTuple<object, int>.Item1""
  IL_0006:  stloc.0
  IL_0007:  ldloc.0
  IL_0008:  isinst     ""C12""
  IL_000d:  stloc.1
  IL_000e:  ldloc.1
  IL_000f:  brfalse.s  IL_0047
  IL_0011:  ldloc.1
  IL_0012:  isinst     ""I1""
  IL_0017:  stloc.2
  IL_0018:  ldloc.2
  IL_0019:  brfalse.s  IL_0075
  IL_001b:  ldloc.2
  IL_001c:  callvirt   ""int I1.F.get""
  IL_0021:  stloc.3
  IL_0022:  ldloc.3
  IL_0023:  ldc.i4.1
  IL_0024:  bne.un.s   IL_003d
  IL_0026:  ldarg.0
  IL_0027:  ldfld      ""int System.ValueTuple<object, int>.Item2""
  IL_002c:  stloc.s    V_4
  IL_002e:  ldloc.s    V_4
  IL_0030:  ldc.i4.2
  IL_0031:  beq.s      IL_0070
  IL_0033:  ldloc.0
  IL_0034:  isinst     ""I2""
  IL_0039:  brtrue.s   IL_006b
  IL_003b:  br.s       IL_0075
  IL_003d:  ldloc.0
  IL_003e:  isinst     ""I2""
  IL_0043:  brtrue.s   IL_005f
  IL_0045:  br.s       IL_0075
  IL_0047:  ldloc.0
  IL_0048:  isinst     ""I2""
  IL_004d:  stloc.s    V_5
  IL_004f:  ldloc.s    V_5
  IL_0051:  brfalse.s  IL_0075
  IL_0053:  ldloc.s    V_5
  IL_0055:  callvirt   ""int I1.F.get""
  IL_005a:  stloc.3
  IL_005b:  ldloc.3
  IL_005c:  ldc.i4.1
  IL_005d:  beq.s      IL_0063
  IL_005f:  ldloc.3
  IL_0060:  ldc.i4.2
  IL_0061:  bne.un.s   IL_0075
  IL_0063:  ldarg.0
  IL_0064:  ldfld      ""int System.ValueTuple<object, int>.Item2""
  IL_0069:  stloc.s    V_4
  IL_006b:  ldloc.s    V_4
  IL_006d:  ldc.i4.1
  IL_006e:  bne.un.s   IL_0075
  IL_0070:  ldc.i4.1
  IL_0071:  stloc.s    V_6
  IL_0073:  br.s       IL_0078
  IL_0075:  ldc.i4.0
  IL_0076:  stloc.s    V_6
  IL_0078:  ldloc.s    V_6
  IL_007a:  ret
}
");
        }

        [Fact]
        [WorkItem("https://github.com/dotnet/roslyn/issues/82063")]
        public void SideeffectEvaluations_04()
        {
            var src = @"
interface I1
{
    int F {get;}
}

interface I2 : I1
{
}

class C11;
class C12;
class C13(int f) : C12, I1
{
    public int F
    {
        get
        {
            System.Console.Write(""Evaluated C13.F "");
            return f;   
        }
    }
}
class C14(int f) : I2
{
    public int F
    {
        get
        {
            System.Console.Write(""Evaluated C14.F "");
            return f;   
        }
    }
}
class C15(int f) : I1
{
    public int F
    {
        get
        {
            System.Console.Write(""Evaluated C15.F "");
            return f;   
        }
    }
}
class C16(int f) : C12, I2
{
    public int F
    {
        get
        {
            System.Console.Write(""Evaluated C15.F "");
            return f;   
        }
    }
}

class Program
{
    static void Main()
    {
        object[] s = [null, new C11(), new C12(), new C13(1), new C14(1), new C15(1), new C16(1), new C13(2), new C14(2), new C15(2), new C16(2), new C13(3), new C14(3), new C15(3), new C16(3)]; 
        int[] i = [1, 2, 3];
        foreach (var s1 in s)
        {
            foreach (var j in i)
            {
                var t = Test1((s1, j));
                System.Console.WriteLine(t);
            }
        }
    }

    static bool Test1((object, int) u)
    {
        return u is (C12 and I2 and { F: 1 }, 2) or (I1 and { F: 1 or 2 }, 1);
    }   
}
";
            var comp = CreateCompilation(src, options: TestOptions.ReleaseExe);

            VerifyDecisionDagDump<IsPatternExpressionSyntax>(comp,
@"[0]: t1 = t0.Item1; [1]
[1]: t1 is C12 ? [2] : [9]
[2]: t2 = (C12)t1; [3]
[3]: t2 is I2 ? [4] : [9]
[4]: t3 = (I2)t2; [5]
[5]: t4 = t3.F; [6]
[6]: t4 == 1 ? [7] : [13]
[7]: t5 = t0.Item2; [8]
[8]: t5 == 2 ? [16] : [15]
[9]: t1 is I1 ? [10] : [17]
[10]: t6 = (I1)t1; [11]
[11]: t4 = t6.F; [12]
[12]: t4 == 1 ? [14] : [13]
[13]: t4 == 2 ? [14] : [17]
[14]: t5 = t0.Item2; [15]
[15]: t5 == 1 ? [16] : [17]
[16]: leaf <isPatternSuccess> `(C12 and I2 and { F: 1 }, 2) or (I1 and { F: 1 or 2 }, 1)`
[17]: leaf <isPatternFailure> `(C12 and I2 and { F: 1 }, 2) or (I1 and { F: 1 or 2 }, 1)`
");

            var verifier = CompileAndVerify(
                comp,
                expectedOutput: @"
False
False
False
False
False
False
False
False
False
Evaluated C13.F True
Evaluated C13.F False
Evaluated C13.F False
Evaluated C14.F True
Evaluated C14.F False
Evaluated C14.F False
Evaluated C15.F True
Evaluated C15.F False
Evaluated C15.F False
Evaluated C15.F True
Evaluated C15.F True
Evaluated C15.F False
Evaluated C13.F True
Evaluated C13.F False
Evaluated C13.F False
Evaluated C14.F True
Evaluated C14.F False
Evaluated C14.F False
Evaluated C15.F True
Evaluated C15.F False
Evaluated C15.F False
Evaluated C15.F True
Evaluated C15.F False
Evaluated C15.F False
Evaluated C13.F False
Evaluated C13.F False
Evaluated C13.F False
Evaluated C14.F False
Evaluated C14.F False
Evaluated C14.F False
Evaluated C15.F False
Evaluated C15.F False
Evaluated C15.F False
Evaluated C15.F False
Evaluated C15.F False
Evaluated C15.F False
").VerifyDiagnostics();

            verifier.VerifyIL("Program.Test1", @"
{
  // Code size      105 (0x69)
  .maxstack  2
  .locals init (object V_0,
                C12 V_1,
                I2 V_2,
                int V_3,
                int V_4,
                I1 V_5,
                bool V_6)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""object System.ValueTuple<object, int>.Item1""
  IL_0006:  stloc.0
  IL_0007:  ldloc.0
  IL_0008:  isinst     ""C12""
  IL_000d:  stloc.1
  IL_000e:  ldloc.1
  IL_000f:  brfalse.s  IL_0035
  IL_0011:  ldloc.1
  IL_0012:  isinst     ""I2""
  IL_0017:  stloc.2
  IL_0018:  ldloc.2
  IL_0019:  brfalse.s  IL_0035
  IL_001b:  ldloc.2
  IL_001c:  callvirt   ""int I1.F.get""
  IL_0021:  stloc.3
  IL_0022:  ldloc.3
  IL_0023:  ldc.i4.1
  IL_0024:  bne.un.s   IL_004d
  IL_0026:  ldarg.0
  IL_0027:  ldfld      ""int System.ValueTuple<object, int>.Item2""
  IL_002c:  stloc.s    V_4
  IL_002e:  ldloc.s    V_4
  IL_0030:  ldc.i4.2
  IL_0031:  beq.s      IL_005e
  IL_0033:  br.s       IL_0059
  IL_0035:  ldloc.0
  IL_0036:  isinst     ""I1""
  IL_003b:  stloc.s    V_5
  IL_003d:  ldloc.s    V_5
  IL_003f:  brfalse.s  IL_0063
  IL_0041:  ldloc.s    V_5
  IL_0043:  callvirt   ""int I1.F.get""
  IL_0048:  stloc.3
  IL_0049:  ldloc.3
  IL_004a:  ldc.i4.1
  IL_004b:  beq.s      IL_0051
  IL_004d:  ldloc.3
  IL_004e:  ldc.i4.2
  IL_004f:  bne.un.s   IL_0063
  IL_0051:  ldarg.0
  IL_0052:  ldfld      ""int System.ValueTuple<object, int>.Item2""
  IL_0057:  stloc.s    V_4
  IL_0059:  ldloc.s    V_4
  IL_005b:  ldc.i4.1
  IL_005c:  bne.un.s   IL_0063
  IL_005e:  ldc.i4.1
  IL_005f:  stloc.s    V_6
  IL_0061:  br.s       IL_0066
  IL_0063:  ldc.i4.0
  IL_0064:  stloc.s    V_6
  IL_0066:  ldloc.s    V_6
  IL_0068:  ret
}
");
        }

        [Fact]
        [WorkItem("https://github.com/dotnet/roslyn/issues/82063")]
        public void SideeffectEvaluations_05()
        {
            var src = @"
interface I1
{
    int F {get;}
}

interface I2 : I1
{
}

class C11;
class C12;
class C13(int f) : C12, I1
{
    public int F
    {
        get
        {
            System.Console.Write(""Evaluated C13.F "");
            return f;   
        }
    }
}
class C14(int f) : I2
{
    public int F
    {
        get
        {
            System.Console.Write(""Evaluated C14.F "");
            return f;   
        }
    }
}
class C15(int f) : I1
{
    public int F
    {
        get
        {
            System.Console.Write(""Evaluated C15.F "");
            return f;   
        }
    }
}
class C16(int f) : C12, I2
{
    public int F
    {
        get
        {
            System.Console.Write(""Evaluated C15.F "");
            return f;   
        }
    }
}

class Program
{
    static void Main()
    {
        object[] s = [null, new C11(), new C12(), new C13(1), new C14(1), new C15(1), new C16(1), new C13(2), new C14(2), new C15(2), new C16(2), new C13(3), new C14(3), new C15(3), new C16(3)]; 
        int[] i = [1, 2, 3];
        foreach (var s1 in s)
        {
            foreach (var j in i)
            {
                var t = Test1((s1, j));
                System.Console.WriteLine(t);
            }
        }
    }

    static int Test1((object, int) u)
    {
        return u switch { (C12 and I1 and { F: 1 and var x1 }, 2) => x1 ,(I2 and { F: (1 or 2) and var x2 }, 1) => x2, _ => -100 };
    }   
}
";
            var comp = CreateCompilation(src, options: TestOptions.ReleaseExe);

            VerifyDecisionDagDump<SwitchExpressionSyntax>(comp,
@"[0]: t1 = t0.Item1; [1]
[1]: t1 is C12 ? [2] : [13]
[2]: t2 = (C12)t1; [3]
[3]: t2 is I1 ? [4] : [22]
[4]: t3 = (I1)t2; [5]
[5]: t4 = t3.F; [6]
[6]: t4 == 1 ? [7] : [12]
[7]: t5 = t0.Item2; [8]
[8]: t5 == 2 ? [9] : [11]
[9]: when <true> ? [10] : <unreachable>
[10]: leaf <arm> `(C12 and I1 and { F: 1 and var x1 }, 2) => x1`
[11]: t1 is I2 ? [19] : [22]
[12]: t1 is I2 ? [17] : [22]
[13]: t1 is I2 ? [14] : [22]
[14]: t6 = (I2)t1; [15]
[15]: t4 = t6.F; [16]
[16]: t4 == 1 ? [18] : [17]
[17]: t4 == 2 ? [18] : [22]
[18]: t5 = t0.Item2; [19]
[19]: t5 == 1 ? [20] : [22]
[20]: when <true> ? [21] : <unreachable>
[21]: leaf <arm> `(I2 and { F: (1 or 2) and var x2 }, 1) => x2`
[22]: leaf <arm> `_ => -100`
");

            var verifier = CompileAndVerify(
                comp,
                expectedOutput: @"
-100
-100
-100
-100
-100
-100
-100
-100
-100
Evaluated C13.F -100
Evaluated C13.F 1
Evaluated C13.F -100
Evaluated C14.F 1
Evaluated C14.F -100
Evaluated C14.F -100
-100
-100
-100
Evaluated C15.F 1
Evaluated C15.F 1
Evaluated C15.F -100
Evaluated C13.F -100
Evaluated C13.F -100
Evaluated C13.F -100
Evaluated C14.F 2
Evaluated C14.F -100
Evaluated C14.F -100
-100
-100
-100
Evaluated C15.F 2
Evaluated C15.F -100
Evaluated C15.F -100
Evaluated C13.F -100
Evaluated C13.F -100
Evaluated C13.F -100
Evaluated C14.F -100
Evaluated C14.F -100
Evaluated C14.F -100
-100
-100
-100
Evaluated C15.F -100
Evaluated C15.F -100
Evaluated C15.F -100
").VerifyDiagnostics();

            verifier.VerifyIL("Program.Test1", @"
{
  // Code size      130 (0x82)
  .maxstack  2
  .locals init (int V_0, //x1
                int V_1,
                object V_2,
                C12 V_3,
                I1 V_4,
                int V_5,
                I2 V_6)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""object System.ValueTuple<object, int>.Item1""
  IL_0006:  stloc.2
  IL_0007:  ldloc.2
  IL_0008:  isinst     ""C12""
  IL_000d:  stloc.3
  IL_000e:  ldloc.3
  IL_000f:  brfalse.s  IL_004a
  IL_0011:  ldloc.3
  IL_0012:  isinst     ""I1""
  IL_0017:  stloc.s    V_4
  IL_0019:  ldloc.s    V_4
  IL_001b:  brfalse.s  IL_007d
  IL_001d:  ldloc.s    V_4
  IL_001f:  callvirt   ""int I1.F.get""
  IL_0024:  stloc.0
  IL_0025:  ldloc.0
  IL_0026:  ldc.i4.1
  IL_0027:  bne.un.s   IL_0040
  IL_0029:  ldarg.0
  IL_002a:  ldfld      ""int System.ValueTuple<object, int>.Item2""
  IL_002f:  stloc.s    V_5
  IL_0031:  ldloc.s    V_5
  IL_0033:  ldc.i4.2
  IL_0034:  beq.s      IL_0075
  IL_0036:  ldloc.2
  IL_0037:  isinst     ""I2""
  IL_003c:  brtrue.s   IL_006e
  IL_003e:  br.s       IL_007d
  IL_0040:  ldloc.2
  IL_0041:  isinst     ""I2""
  IL_0046:  brtrue.s   IL_0062
  IL_0048:  br.s       IL_007d
  IL_004a:  ldloc.2
  IL_004b:  isinst     ""I2""
  IL_0050:  stloc.s    V_6
  IL_0052:  ldloc.s    V_6
  IL_0054:  brfalse.s  IL_007d
  IL_0056:  ldloc.s    V_6
  IL_0058:  callvirt   ""int I1.F.get""
  IL_005d:  stloc.0
  IL_005e:  ldloc.0
  IL_005f:  ldc.i4.1
  IL_0060:  beq.s      IL_0066
  IL_0062:  ldloc.0
  IL_0063:  ldc.i4.2
  IL_0064:  bne.un.s   IL_007d
  IL_0066:  ldarg.0
  IL_0067:  ldfld      ""int System.ValueTuple<object, int>.Item2""
  IL_006c:  stloc.s    V_5
  IL_006e:  ldloc.s    V_5
  IL_0070:  ldc.i4.1
  IL_0071:  beq.s      IL_0079
  IL_0073:  br.s       IL_007d
  IL_0075:  ldloc.0
  IL_0076:  stloc.1
  IL_0077:  br.s       IL_0080
  IL_0079:  ldloc.0
  IL_007a:  stloc.1
  IL_007b:  br.s       IL_0080
  IL_007d:  ldc.i4.s   -100
  IL_007f:  stloc.1
  IL_0080:  ldloc.1
  IL_0081:  ret
}
");
        }

        [Fact]
        [WorkItem("https://github.com/dotnet/roslyn/issues/82063")]
        public void SideeffectEvaluations_06()
        {
            var src = @"
#nullable enable

interface I1
{
    C2? F { get; }
}

class C2
{
    public string? S = null;
}

interface I2 : I1
{
}

class C11;
class C12;
class C13 : C12, I1
{
    public C2? F => null;
}
class C14 : I2
{
    public C2? F => null;
}
class C15 : I1
{
    public C2? F => null;
}
class C16 : C12, I2
{
    public C2? F => null;
}

class Program
{
    static int Test1((object, int) u)
    {
        return u switch 
        {
            (C12 and I1 and { F: { S: ""1"" and var x1 } }, 2) => x1.Length ,
            (I2 and { F: { S: (""1"" or ""2"") and var x2 } }, 1) => x2.Length,
            _ => -100
        };
    }   

    static int Test2((object, int) u)
    {
        return u switch 
        {
            (C12 and I1 and { F: { S: ""1"" } x3 }, 2) => x3.S.Length ,
            (I2 and { F: { S: (""1"" or ""2"") } x4 }, 1) => x4.S.Length,
            _ => -100
        };
    }   
}
";
            var comp = CreateCompilation(src, options: TestOptions.ReleaseDll);
            comp.VerifyDiagnostics();
        }
    }
}
