﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
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
                    Diagnostic(ErrorCode.ERR_SyntaxError, ":").WithArguments(",", ":").WithLocation(10, 34),
                    // (10,36): error CS1003: Syntax error, ',' expected
                    //         _ = new C() is { Prop1[0]: {} };
                    Diagnostic(ErrorCode.ERR_SyntaxError, "{").WithArguments(",", "{").WithLocation(10, 36),
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
        public void M()
        {
            const Type c = null!;
            if (this is c!) {}
            if (this is (c!)) {}
            if (this is Type!) {} // 1
            if (this is ContainerType!.Type) {} // 2
            if (this is ContainerType.Type!) {} // 3
        }
    }
}
";
            var compilation = CreateCompilation(program, parseOptions: TestOptions.RegularWithExtendedPropertyPatterns);
            compilation.VerifyEmitDiagnostics(
                // (12,25): error CS8598: The suppression operator is not allowed in this context
                //             if (this is Type!) {} // 1
                Diagnostic(ErrorCode.ERR_IllegalSuppression, "Type!").WithLocation(12, 25),
                // (13,25): error CS8598: The suppression operator is not allowed in this context
                //             if (this is ContainerType!.Type) {} // 2
                Diagnostic(ErrorCode.ERR_IllegalSuppression, "ContainerType").WithLocation(13, 25),
                // (14,25): error CS8598: The suppression operator is not allowed in this context
                //             if (this is ContainerType.Type!) {} // 3
                Diagnostic(ErrorCode.ERR_IllegalSuppression, "ContainerType.Type!").WithLocation(14, 25)
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
                // (6,18): error CS0150: A constant value is expected
                //         if (0 is s->X) {}
                Diagnostic(ErrorCode.ERR_ConstantExpected, "s->X").WithLocation(6, 18)
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
                // (8,22): error CS8773: Feature 'extended property patterns' is not available in C# 9.0. Please use language version 10.0 or greater.
                //         _ = this is (Property.Property: null, Property: null);
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion9, "Property.Property").WithArguments("extended property patterns", "10.0").WithLocation(8, 22),
                // (8,22): error CS1001: Identifier expected
                //         _ = this is (Property.Property: null, Property: null);
                Diagnostic(ErrorCode.ERR_IdentifierExpected, "Property.Property").WithLocation(8, 22),
                // (8,47): error CS8517: The name 'Property' does not match the corresponding 'Deconstruct' parameter 'c2'.
                //         _ = this is (Property.Property: null, Property: null);
                Diagnostic(ErrorCode.ERR_DeconstructParameterNameMismatch, "Property").WithArguments("Property", "c2").WithLocation(8, 47)
                );
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
                // (14,13): warning CS8509: The switch expression does not handle all possible values of its input type (it is not exhaustive). For example, the pattern '{ Prop: { Prop: not null } }' is not covered.
                // _ = new C() switch // 2
                Diagnostic(ErrorCode.WRN_SwitchExpressionNotExhaustive, "switch").WithArguments("{ Prop: { Prop: not null } }").WithLocation(14, 13)
                );
        }

        public class FlowAnalysisTests : FlowTestBase
        {
            [Fact]
            public void RegionInIsPattern01()
            {
                var dataFlowAnalysisResults = CompileAndAnalyzeDataFlowExpression(@"
class C
{
    static void M(object o)
    {
        _ = o switch
        {
            string { Length: 0 } s => /*<bind>*/s.ToString()/*</bind>*/,
            _ = throw null
        };
    }
}");
                Assert.Null(GetSymbolNamesJoined(dataFlowAnalysisResults.VariablesDeclared));
                Assert.Equal("s", GetSymbolNamesJoined(dataFlowAnalysisResults.DataFlowsIn));
                Assert.Null(GetSymbolNamesJoined(dataFlowAnalysisResults.DataFlowsOut));
                Assert.Null(GetSymbolNamesJoined(dataFlowAnalysisResults.AlwaysAssigned));
                Assert.Equal("s", GetSymbolNamesJoined(dataFlowAnalysisResults.ReadInside));
                Assert.Null(GetSymbolNamesJoined(dataFlowAnalysisResults.WrittenInside));
                Assert.Equal("o", GetSymbolNamesJoined(dataFlowAnalysisResults.ReadOutside));
                Assert.Equal("o, s", GetSymbolNamesJoined(dataFlowAnalysisResults.WrittenOutside));
                Assert.Null(GetSymbolNamesJoined(dataFlowAnalysisResults.Captured));
                Assert.Null(GetSymbolNamesJoined(dataFlowAnalysisResults.CapturedInside));
                Assert.Null(GetSymbolNamesJoined(dataFlowAnalysisResults.CapturedOutside));
                Assert.Null(GetSymbolNamesJoined(dataFlowAnalysisResults.UnsafeAddressTaken));
            }
        }
    }
}
