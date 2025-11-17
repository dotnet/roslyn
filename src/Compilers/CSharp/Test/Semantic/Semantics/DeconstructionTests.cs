// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.Semantics
{
    [CompilerTrait(CompilerFeature.Tuples)]
    public class DeconstructionTests : CompilingTestBase
    {
        private static readonly MetadataReference[] s_valueTupleRefs = new[] { SystemRuntimeFacadeRef, ValueTupleRef };

        const string commonSource =
@"public class Pair<T1, T2>
{
    T1 item1;
    T2 item2;

    public Pair(T1 item1, T2 item2)
    {
        this.item1 = item1;
        this.item2 = item2;
    }

    public void Deconstruct(out T1 item1, out T2 item2)
    {
        System.Console.WriteLine($""Deconstructing {ToString()}"");
        item1 = this.item1;
        item2 = this.item2;
    }

    public override string ToString() { return $""({item1.ToString()}, {item2.ToString()})""; }
}

public static class Pair
{
    public static Pair<T1, T2> Create<T1, T2>(T1 item1, T2 item2) { return new Pair<T1, T2>(item1, item2); }
}

public class Integer
{
    public int state;
    public override string ToString() { return state.ToString(); }
    public Integer(int i) { state = i; }
    public static implicit operator LongInteger(Integer i) { System.Console.WriteLine($""Converting {i}""); return new LongInteger(i.state); }
}

public class LongInteger
{
    long state;
    public LongInteger(long l) { state = l; }
    public override string ToString() { return state.ToString(); }
}";

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void DeconstructMethodMissing()
        {
            string source = @"
class C
{
    static void Main()
    {
        long x;
        string y;
        /*<bind>*/(x, y) = new C()/*</bind>*/;
    }
}
";
            string expectedOperationTree = @"
IDeconstructionAssignmentOperation (OperationKind.DeconstructionAssignment, Type: ?, IsInvalid) (Syntax: '(x, y) = new C()')
  Left: 
    ITupleOperation (OperationKind.Tuple, Type: (System.Int64 x, System.String y)) (Syntax: '(x, y)')
      NaturalType: (System.Int64 x, System.String y)
      Elements(2):
          ILocalReferenceOperation: x (OperationKind.LocalReference, Type: System.Int64) (Syntax: 'x')
          ILocalReferenceOperation: y (OperationKind.LocalReference, Type: System.String) (Syntax: 'y')
  Right: 
    IObjectCreationOperation (Constructor: C..ctor()) (OperationKind.ObjectCreation, Type: C, IsInvalid) (Syntax: 'new C()')
      Arguments(0)
      Initializer: 
        null
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS1061: 'C' does not contain a definition for 'Deconstruct' and no extension method 'Deconstruct' accepting a first argument of type 'C' could be found (are you missing a using directive or an assembly reference?)
                //         /*<bind>*/(x, y) = new C()/*</bind>*/;
                Diagnostic(ErrorCode.ERR_NoSuchMemberOrExtension, "new C()").WithArguments("C", "Deconstruct").WithLocation(8, 28)
            };

            VerifyOperationTreeAndDiagnosticsForTest<AssignmentExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [Fact, CompilerTrait(CompilerFeature.IOperation)]
        public void DeconstructMethodMissing_WithImplicitlyTypedVariables()
        {
            string source = """
                class C
                {
                    public static void M(object o)
                    {
                        /*<bind>*/var (x, y) = o/*</bind>*/;
                    }
                }
                """;
            string expectedOperationTree = """
                IDeconstructionAssignmentOperation (OperationKind.DeconstructionAssignment, Type: ?, IsInvalid) (Syntax: 'var (x, y) = o')
                  Left: 
                    IDeclarationExpressionOperation (OperationKind.DeclarationExpression, Type: (var x, var y)) (Syntax: 'var (x, y)')
                      ITupleOperation (OperationKind.Tuple, Type: (var x, var y)) (Syntax: '(x, y)')
                        NaturalType: (var x, var y)
                        Elements(2):
                            ILocalReferenceOperation: x (IsDeclaration: True) (OperationKind.LocalReference, Type: var) (Syntax: 'x')
                            ILocalReferenceOperation: y (IsDeclaration: True) (OperationKind.LocalReference, Type: var) (Syntax: 'y')
                  Right: 
                    IParameterReferenceOperation: o (OperationKind.ParameterReference, Type: System.Object, IsInvalid) (Syntax: 'o')
                """;

            VerifyOperationTreeAndDiagnosticsForTest<AssignmentExpressionSyntax>(source, expectedOperationTree, [
                // (5,32): error CS1061: 'object' does not contain a definition for 'Deconstruct' and no accessible extension method 'Deconstruct' accepting a first argument of type 'object' could be found (are you missing a using directive or an assembly reference?)
                //         /*<bind>*/var (x, y) = o/*</bind>*/;
                Diagnostic(ErrorCode.ERR_NoSuchMemberOrExtension, "o").WithArguments("object", "Deconstruct").WithLocation(5, 32)]);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void DeconstructWrongParams()
        {
            string source = @"
class C
{
    static void Main()
    {
        long x;
        string y;
        /*<bind>*/(x, y) = new C()/*</bind>*/;
    }
    public void Deconstruct(out int a) // too few arguments
    {
        a = 1;
    }
}
";
            string expectedOperationTree = @"
IDeconstructionAssignmentOperation (OperationKind.DeconstructionAssignment, Type: ?, IsInvalid) (Syntax: '(x, y) = new C()')
  Left: 
    ITupleOperation (OperationKind.Tuple, Type: (System.Int64 x, System.String y)) (Syntax: '(x, y)')
      NaturalType: (System.Int64 x, System.String y)
      Elements(2):
          ILocalReferenceOperation: x (OperationKind.LocalReference, Type: System.Int64) (Syntax: 'x')
          ILocalReferenceOperation: y (OperationKind.LocalReference, Type: System.String) (Syntax: 'y')
  Right: 
    IObjectCreationOperation (Constructor: C..ctor()) (OperationKind.ObjectCreation, Type: C, IsInvalid) (Syntax: 'new C()')
      Arguments(0)
      Initializer: 
        null
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // (8,28): error CS1501: No overload for method 'Deconstruct' takes 2 arguments
                //         /*<bind>*/(x, y) = new C()/*</bind>*/;
                Diagnostic(ErrorCode.ERR_BadArgCount, "new C()").WithArguments("Deconstruct", "2").WithLocation(8, 28)
            };

            VerifyOperationTreeAndDiagnosticsForTest<AssignmentExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void DeconstructWrongParams2()
        {
            string source = @"
class C
{
    static void Main()
    {
        long x, y;
        /*<bind>*/(x, y) = new C()/*</bind>*/;
    }
    public void Deconstruct(out int a, out int b, out int c) // too many arguments
    {
        a = b = c = 1;
    }
}
";
            string expectedOperationTree = @"
IDeconstructionAssignmentOperation (OperationKind.DeconstructionAssignment, Type: ?, IsInvalid) (Syntax: '(x, y) = new C()')
  Left: 
    ITupleOperation (OperationKind.Tuple, Type: (System.Int64 x, System.Int64 y)) (Syntax: '(x, y)')
      NaturalType: (System.Int64 x, System.Int64 y)
      Elements(2):
          ILocalReferenceOperation: x (OperationKind.LocalReference, Type: System.Int64) (Syntax: 'x')
          ILocalReferenceOperation: y (OperationKind.LocalReference, Type: System.Int64) (Syntax: 'y')
  Right: 
    IObjectCreationOperation (Constructor: C..ctor()) (OperationKind.ObjectCreation, Type: C, IsInvalid) (Syntax: 'new C()')
      Arguments(0)
      Initializer: 
        null
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // (7,28): error CS7036: There is no argument given that corresponds to the required parameter 'c' of 'C.Deconstruct(out int, out int, out int)'
                //         /*<bind>*/(x, y) = new C()/*</bind>*/;
                Diagnostic(ErrorCode.ERR_NoCorrespondingArgument, "new C()").WithArguments("c", "C.Deconstruct(out int, out int, out int)").WithLocation(7, 28)
            };

            VerifyOperationTreeAndDiagnosticsForTest<AssignmentExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void AssignmentWithLeftHandSideErrors()
        {
            string source = @"
class C
{
    static void Main()
    {
        long x = 1;
        string y = ""hello"";
        /*<bind>*/(x.f, y.g) = new C()/*</bind>*/;
    }
    public void Deconstruct() { }
}
";
            string expectedOperationTree = @"
IDeconstructionAssignmentOperation (OperationKind.DeconstructionAssignment, Type: ?, IsInvalid) (Syntax: '(x.f, y.g) = new C()')
  Left: 
    ITupleOperation (OperationKind.Tuple, Type: (? f, ? g), IsInvalid) (Syntax: '(x.f, y.g)')
      NaturalType: (? f, ? g)
      Elements(2):
          IInvalidOperation (OperationKind.Invalid, Type: ?, IsInvalid) (Syntax: 'x.f')
            Children(1):
                ILocalReferenceOperation: x (OperationKind.LocalReference, Type: System.Int64) (Syntax: 'x')
          IInvalidOperation (OperationKind.Invalid, Type: ?, IsInvalid) (Syntax: 'y.g')
            Children(1):
                ILocalReferenceOperation: y (OperationKind.LocalReference, Type: System.String) (Syntax: 'y')
  Right: 
    IObjectCreationOperation (Constructor: C..ctor()) (OperationKind.ObjectCreation, Type: C, IsInvalid) (Syntax: 'new C()')
      Arguments(0)
      Initializer: 
        null
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // (8,22): error CS1061: 'long' does not contain a definition for 'f' and no accessible extension method 'f' accepting a first argument of type 'long' could be found (are you missing a using directive or an assembly reference?)
                //         /*<bind>*/(x.f, y.g) = new C()/*</bind>*/;
                Diagnostic(ErrorCode.ERR_NoSuchMemberOrExtension, "f").WithArguments("long", "f").WithLocation(8, 22),
                // (8,27): error CS1061: 'string' does not contain a definition for 'g' and no accessible extension method 'g' accepting a first argument of type 'string' could be found (are you missing a using directive or an assembly reference?)
                //         /*<bind>*/(x.f, y.g) = new C()/*</bind>*/;
                Diagnostic(ErrorCode.ERR_NoSuchMemberOrExtension, "g").WithArguments("string", "g").WithLocation(8, 27),
                // (8,32): error CS1501: No overload for method 'Deconstruct' takes 2 arguments
                //         /*<bind>*/(x.f, y.g) = new C()/*</bind>*/;
                Diagnostic(ErrorCode.ERR_BadArgCount, "new C()").WithArguments("Deconstruct", "2").WithLocation(8, 32)
            };

            VerifyOperationTreeAndDiagnosticsForTest<AssignmentExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void DeconstructWithInParam()
        {
            string source = @"
class C
{
    static void Main()
    {
        int x;
        int y;
        /*<bind>*/(x, y) = new C()/*</bind>*/;
    }
    public void Deconstruct(out int x, int y) { x = 1; }
}
";
            string expectedOperationTree = @"
IDeconstructionAssignmentOperation (OperationKind.DeconstructionAssignment, Type: ?, IsInvalid) (Syntax: '(x, y) = new C()')
  Left: 
    ITupleOperation (OperationKind.Tuple, Type: (System.Int32 x, System.Int32 y), IsInvalid) (Syntax: '(x, y)')
      NaturalType: (System.Int32 x, System.Int32 y)
      Elements(2):
          ILocalReferenceOperation: x (OperationKind.LocalReference, Type: System.Int32, IsInvalid) (Syntax: 'x')
          ILocalReferenceOperation: y (OperationKind.LocalReference, Type: System.Int32, IsInvalid) (Syntax: 'y')
  Right: 
    IObjectCreationOperation (Constructor: C..ctor()) (OperationKind.ObjectCreation, Type: C, IsInvalid) (Syntax: 'new C()')
      Arguments(0)
      Initializer: 
        null
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // (8,19): error CS1615: Argument 2 may not be passed with the 'out' keyword
                //         /*<bind>*/(x, y) = new C()/*</bind>*/;
                Diagnostic(ErrorCode.ERR_BadArgExtraRef, "(x, y) = new C()").WithArguments("2", "out").WithLocation(8, 19)
            };

            VerifyOperationTreeAndDiagnosticsForTest<AssignmentExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void DeconstructWithRefParam()
        {
            string source = @"
class C
{
    static void Main()
    {
        int x;
        int y;
        /*<bind>*/(x, y) = new C()/*</bind>*/;
    }
    public void Deconstruct(ref int x, out int y) { x = 1; y = 2; }
}
";
            string expectedOperationTree = @"
IDeconstructionAssignmentOperation (OperationKind.DeconstructionAssignment, Type: ?, IsInvalid) (Syntax: '(x, y) = new C()')
  Left: 
    ITupleOperation (OperationKind.Tuple, Type: (System.Int32 x, System.Int32 y), IsInvalid) (Syntax: '(x, y)')
      NaturalType: (System.Int32 x, System.Int32 y)
      Elements(2):
          ILocalReferenceOperation: x (OperationKind.LocalReference, Type: System.Int32, IsInvalid) (Syntax: 'x')
          ILocalReferenceOperation: y (OperationKind.LocalReference, Type: System.Int32, IsInvalid) (Syntax: 'y')
  Right: 
    IObjectCreationOperation (Constructor: C..ctor()) (OperationKind.ObjectCreation, Type: C, IsInvalid) (Syntax: 'new C()')
      Arguments(0)
      Initializer: 
        null
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // (8,19): error CS1620: Argument 1 must be passed with the 'ref' keyword
                //         /*<bind>*/(x, y) = new C()/*</bind>*/;
                Diagnostic(ErrorCode.ERR_BadArgRef, "(x, y) = new C()").WithArguments("1", "ref").WithLocation(8, 19)
            };

            VerifyOperationTreeAndDiagnosticsForTest<AssignmentExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void DeconstructManually()
        {
            string source = @"
struct C
{
    static void Main()
    {
        long x;
        string y;
        C c = new C();

        c.Deconstruct(out x, out y); // error
        /*<bind>*/(x, y) = c/*</bind>*/;
    }

    void Deconstruct(out int a, out string b)
    {
        a = 1;
        b = ""hello"";
    }
}
";
            string expectedOperationTree = @"
IDeconstructionAssignmentOperation (OperationKind.DeconstructionAssignment, Type: (System.Int64 x, System.String y)) (Syntax: '(x, y) = c')
  Left: 
    ITupleOperation (OperationKind.Tuple, Type: (System.Int64 x, System.String y)) (Syntax: '(x, y)')
      NaturalType: (System.Int64 x, System.String y)
      Elements(2):
          ILocalReferenceOperation: x (OperationKind.LocalReference, Type: System.Int64) (Syntax: 'x')
          ILocalReferenceOperation: y (OperationKind.LocalReference, Type: System.String) (Syntax: 'y')
  Right: 
    ILocalReferenceOperation: c (OperationKind.LocalReference, Type: C) (Syntax: 'c')
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS1503: Argument 1: cannot convert from 'out long' to 'out int'
                //         c.Deconstruct(out x, out y); // error
                Diagnostic(ErrorCode.ERR_BadArgType, "x").WithArguments("1", "out long", "out int").WithLocation(10, 27)
            };

            VerifyOperationTreeAndDiagnosticsForTest<AssignmentExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void DeconstructMethodHasOptionalParam()
        {
            string source = @"
class C
{
    static void Main()
    {
        long x;
        string y;

        /*<bind>*/(x, y) = new C()/*</bind>*/;
        System.Console.WriteLine(x + "" "" + y);
    }

    public void Deconstruct(out int a, out string b, int c = 42) // not a Deconstruct operator
    {
        a = 1;
        b = ""hello"";
    }
}
";
            string expectedOperationTree = @"
IDeconstructionAssignmentOperation (OperationKind.DeconstructionAssignment, Type: ?, IsInvalid) (Syntax: '(x, y) = new C()')
  Left: 
    ITupleOperation (OperationKind.Tuple, Type: (System.Int64 x, System.String y)) (Syntax: '(x, y)')
      NaturalType: (System.Int64 x, System.String y)
      Elements(2):
          ILocalReferenceOperation: x (OperationKind.LocalReference, Type: System.Int64) (Syntax: 'x')
          ILocalReferenceOperation: y (OperationKind.LocalReference, Type: System.String) (Syntax: 'y')
  Right: 
    IObjectCreationOperation (Constructor: C..ctor()) (OperationKind.ObjectCreation, Type: C, IsInvalid) (Syntax: 'new C()')
      Arguments(0)
      Initializer: 
        null
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS8129: No suitable Deconstruct instance or extension method was found for type 'C', with 2 out parameters and a void return type.
                //         /*<bind>*/(x, y) = new C()/*</bind>*/;
                Diagnostic(ErrorCode.ERR_MissingDeconstruct, "new C()").WithArguments("C", "2").WithLocation(9, 28)
            };

            VerifyOperationTreeAndDiagnosticsForTest<AssignmentExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void BadDeconstructShadowsBaseDeconstruct()
        {
            string source = @"
class D
{
    public void Deconstruct(out int a, out string b) { a = 2; b = ""world""; }
}
class C : D
{
    static void Main()
    {
        long x;
        string y;

        /*<bind>*/(x, y) = new C()/*</bind>*/;
        System.Console.WriteLine(x + "" "" + y);
    }

    public void Deconstruct(out int a, out string b, int c = 42) // not a Deconstruct operator
    {
        a = 1;
        b = ""hello"";
    }
}
";
            string expectedOperationTree = @"
IDeconstructionAssignmentOperation (OperationKind.DeconstructionAssignment, Type: ?, IsInvalid) (Syntax: '(x, y) = new C()')
  Left: 
    ITupleOperation (OperationKind.Tuple, Type: (System.Int64 x, System.String y)) (Syntax: '(x, y)')
      NaturalType: (System.Int64 x, System.String y)
      Elements(2):
          ILocalReferenceOperation: x (OperationKind.LocalReference, Type: System.Int64) (Syntax: 'x')
          ILocalReferenceOperation: y (OperationKind.LocalReference, Type: System.String) (Syntax: 'y')
  Right: 
    IObjectCreationOperation (Constructor: C..ctor()) (OperationKind.ObjectCreation, Type: C, IsInvalid) (Syntax: 'new C()')
      Arguments(0)
      Initializer: 
        null
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS8129: No suitable Deconstruct instance or extension method was found for type 'C', with 2 out parameters and a void return type.
                //         /*<bind>*/(x, y) = new C()/*</bind>*/;
                Diagnostic(ErrorCode.ERR_MissingDeconstruct, "new C()").WithArguments("C", "2").WithLocation(13, 28)
            };

            VerifyOperationTreeAndDiagnosticsForTest<AssignmentExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void DeconstructMethodHasParams()
        {
            string source = @"
class C
{
    static void Main()
    {
        long x;
        string y;

        /*<bind>*/(x, y) = new C()/*</bind>*/;
        System.Console.WriteLine(x + "" "" + y);
    }

    public void Deconstruct(out int a, out string b, params int[] c) // not a Deconstruct operator
    {
        a = 1;
        b = ""hello"";
    }
}
";
            string expectedOperationTree = @"
IDeconstructionAssignmentOperation (OperationKind.DeconstructionAssignment, Type: ?, IsInvalid) (Syntax: '(x, y) = new C()')
  Left: 
    ITupleOperation (OperationKind.Tuple, Type: (System.Int64 x, System.String y)) (Syntax: '(x, y)')
      NaturalType: (System.Int64 x, System.String y)
      Elements(2):
          ILocalReferenceOperation: x (OperationKind.LocalReference, Type: System.Int64) (Syntax: 'x')
          ILocalReferenceOperation: y (OperationKind.LocalReference, Type: System.String) (Syntax: 'y')
  Right: 
    IObjectCreationOperation (Constructor: C..ctor()) (OperationKind.ObjectCreation, Type: C, IsInvalid) (Syntax: 'new C()')
      Arguments(0)
      Initializer: 
        null
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS8129: No suitable Deconstruct instance or extension method was found for type 'C', with 2 out parameters and a void return type.
                //         /*<bind>*/(x, y) = new C()/*</bind>*/;
                Diagnostic(ErrorCode.ERR_MissingDeconstruct, "new C()").WithArguments("C", "2").WithLocation(9, 28)
            };

            VerifyOperationTreeAndDiagnosticsForTest<AssignmentExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void DeconstructMethodHasArglist()
        {
            string source = @"
class C
{
    static void Main()
    {
        long x;
        string y;

        /*<bind>*/(x, y) = new C()/*</bind>*/;
    }

    public void Deconstruct(out int a, out string b, __arglist) // not a Deconstruct operator
    {
        a = 1;
        b = ""hello"";
    }
}
";
            string expectedOperationTree = @"
IDeconstructionAssignmentOperation (OperationKind.DeconstructionAssignment, Type: ?, IsInvalid) (Syntax: '(x, y) = new C()')
  Left: 
    ITupleOperation (OperationKind.Tuple, Type: (System.Int64 x, System.String y)) (Syntax: '(x, y)')
      NaturalType: (System.Int64 x, System.String y)
      Elements(2):
          ILocalReferenceOperation: x (OperationKind.LocalReference, Type: System.Int64) (Syntax: 'x')
          ILocalReferenceOperation: y (OperationKind.LocalReference, Type: System.String) (Syntax: 'y')
  Right: 
    IObjectCreationOperation (Constructor: C..ctor()) (OperationKind.ObjectCreation, Type: C, IsInvalid) (Syntax: 'new C()')
      Arguments(0)
      Initializer: 
        null
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // (9,28): error CS7036: There is no argument given that corresponds to the required parameter '__arglist' of 'C.Deconstruct(out int, out string, __arglist)'
                //         /*<bind>*/(x, y) = new C()/*</bind>*/;
                Diagnostic(ErrorCode.ERR_NoCorrespondingArgument, "new C()").WithArguments("__arglist", "C.Deconstruct(out int, out string, __arglist)").WithLocation(9, 28)
            };

            VerifyOperationTreeAndDiagnosticsForTest<AssignmentExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void DeconstructDelegate()
        {
            string source = @"
delegate void D1(out int x, out int y);

class C
{
    public D1 Deconstruct; // not a Deconstruct operator

    static void Main()
    {
        int x, y;
        /*<bind>*/(x, y) = new C() { Deconstruct = DeconstructMethod }/*</bind>*/;
    }

    public static void DeconstructMethod(out int a, out int b) { a = 1; b = 2; }
}
";
            string expectedOperationTree = @"
IDeconstructionAssignmentOperation (OperationKind.DeconstructionAssignment, Type: ?, IsInvalid) (Syntax: '(x, y) = ne ... uctMethod }')
  Left: 
    ITupleOperation (OperationKind.Tuple, Type: (System.Int32 x, System.Int32 y)) (Syntax: '(x, y)')
      NaturalType: (System.Int32 x, System.Int32 y)
      Elements(2):
          ILocalReferenceOperation: x (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'x')
          ILocalReferenceOperation: y (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'y')
  Right: 
    IObjectCreationOperation (Constructor: C..ctor()) (OperationKind.ObjectCreation, Type: C, IsInvalid) (Syntax: 'new C() { D ... uctMethod }')
      Arguments(0)
      Initializer: 
        IObjectOrCollectionInitializerOperation (OperationKind.ObjectOrCollectionInitializer, Type: C, IsInvalid) (Syntax: '{ Deconstru ... uctMethod }')
          Initializers(1):
              ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: D1, IsInvalid) (Syntax: 'Deconstruct ... tructMethod')
                Left: 
                  IFieldReferenceOperation: D1 C.Deconstruct (OperationKind.FieldReference, Type: D1, IsInvalid) (Syntax: 'Deconstruct')
                    Instance Receiver: 
                      IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: C, IsInvalid, IsImplicit) (Syntax: 'Deconstruct')
                Right: 
                  IDelegateCreationOperation (OperationKind.DelegateCreation, Type: D1, IsInvalid, IsImplicit) (Syntax: 'DeconstructMethod')
                    Target: 
                      IMethodReferenceOperation: void C.DeconstructMethod(out System.Int32 a, out System.Int32 b) (Static) (OperationKind.MethodReference, Type: null, IsInvalid) (Syntax: 'DeconstructMethod')
                        Instance Receiver: 
                          null
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS8129: No suitable Deconstruct instance or extension method was found for type 'C', with 2 out parameters and a void return type.
                //         /*<bind>*/(x, y) = new C() { Deconstruct = DeconstructMethod }/*</bind>*/;
                Diagnostic(ErrorCode.ERR_MissingDeconstruct, "new C() { Deconstruct = DeconstructMethod }").WithArguments("C", "2").WithLocation(11, 28)
            };

            VerifyOperationTreeAndDiagnosticsForTest<AssignmentExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void DeconstructDelegate2()
        {
            string source = @"
delegate void D1(out int x, out int y);

class C
{
    public D1 Deconstruct;

    static void Main()
    {
        int x, y;
        /*<bind>*/(x, y) = new C() { Deconstruct = DeconstructMethod }/*</bind>*/;
    }

    public static void DeconstructMethod(out int a, out int b) { a = 1; b = 2; }

    public void Deconstruct(out int a, out int b) { a = 1; b = 2; }
}
";
            string expectedOperationTree = @"
IDeconstructionAssignmentOperation (OperationKind.DeconstructionAssignment, Type: (System.Int32 x, System.Int32 y), IsInvalid) (Syntax: '(x, y) = ne ... uctMethod }')
  Left: 
    ITupleOperation (OperationKind.Tuple, Type: (System.Int32 x, System.Int32 y)) (Syntax: '(x, y)')
      NaturalType: (System.Int32 x, System.Int32 y)
      Elements(2):
          ILocalReferenceOperation: x (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'x')
          ILocalReferenceOperation: y (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'y')
  Right: 
    IObjectCreationOperation (Constructor: C..ctor()) (OperationKind.ObjectCreation, Type: C, IsInvalid) (Syntax: 'new C() { D ... uctMethod }')
      Arguments(0)
      Initializer: 
        IObjectOrCollectionInitializerOperation (OperationKind.ObjectOrCollectionInitializer, Type: C, IsInvalid) (Syntax: '{ Deconstru ... uctMethod }')
          Initializers(1):
              ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: ?, IsInvalid) (Syntax: 'Deconstruct ... tructMethod')
                Left: 
                  IInvalidOperation (OperationKind.Invalid, Type: ?, IsInvalid, IsImplicit) (Syntax: 'Deconstruct')
                    Children(1):
                        IOperation:  (OperationKind.None, Type: null, IsInvalid) (Syntax: 'Deconstruct')
                          Children(1):
                              IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: C, IsImplicit) (Syntax: 'C')
                Right: 
                  IOperation:  (OperationKind.None, Type: null) (Syntax: 'DeconstructMethod')
                    Children(1):
                        IInstanceReferenceOperation (ReferenceKind: ContainingTypeInstance) (OperationKind.InstanceReference, Type: C, IsImplicit) (Syntax: 'DeconstructMethod')
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS0102: The type 'C' already contains a definition for 'Deconstruct'
                //     public void Deconstruct(out int a, out int b) { a = 1; b = 2; }
                Diagnostic(ErrorCode.ERR_DuplicateNameInClass, "Deconstruct").WithArguments("C", "Deconstruct").WithLocation(16, 17),
                // CS1913: Member 'Deconstruct' cannot be initialized. It is not a field or property.
                //         /*<bind>*/(x, y) = new C() { Deconstruct = DeconstructMethod }/*</bind>*/;
                Diagnostic(ErrorCode.ERR_MemberCannotBeInitialized, "Deconstruct").WithArguments("Deconstruct").WithLocation(11, 38),
                // CS0649: Field 'C.Deconstruct' is never assigned to, and will always have its default value null
                //     public D1 Deconstruct;
                Diagnostic(ErrorCode.WRN_UnassignedInternalField, "Deconstruct").WithArguments("C.Deconstruct", "null").WithLocation(6, 15)
            };

            VerifyOperationTreeAndDiagnosticsForTest<AssignmentExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void DeconstructEvent()
        {
            string source = @"
delegate void D1(out int x, out int y);

class C
{
    public event D1 Deconstruct;  // not a Deconstruct operator

    static void Main()
    {
        long x;
        int y;
        C c = new C();
        c.Deconstruct += DeconstructMethod;
        /*<bind>*/(x, y) = c/*</bind>*/;
    }

    public static void DeconstructMethod(out int a, out int b)
    {
        a = 1;
        b = 2;
    }
}
";
            string expectedOperationTree = @"
IDeconstructionAssignmentOperation (OperationKind.DeconstructionAssignment, Type: ?, IsInvalid) (Syntax: '(x, y) = c')
  Left: 
    ITupleOperation (OperationKind.Tuple, Type: (System.Int64 x, System.Int32 y)) (Syntax: '(x, y)')
      NaturalType: (System.Int64 x, System.Int32 y)
      Elements(2):
          ILocalReferenceOperation: x (OperationKind.LocalReference, Type: System.Int64) (Syntax: 'x')
          ILocalReferenceOperation: y (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'y')
  Right: 
    ILocalReferenceOperation: c (OperationKind.LocalReference, Type: C, IsInvalid) (Syntax: 'c')
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS8129: No suitable Deconstruct instance or extension method was found for type 'C', with 2 out parameters and a void return type.
                //         /*<bind>*/(x, y) = c/*</bind>*/;
                Diagnostic(ErrorCode.ERR_MissingDeconstruct, "c").WithArguments("C", "2").WithLocation(14, 28),
                // CS0067: The event 'C.Deconstruct' is never used
                //     public event D1 Deconstruct;  // not a Deconstruct operator
                Diagnostic(ErrorCode.WRN_UnreferencedEvent, "Deconstruct").WithArguments("C.Deconstruct").WithLocation(6, 21)
            };

            VerifyOperationTreeAndDiagnosticsForTest<AssignmentExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void ConversionErrors()
        {
            string source = @"
class C
{
    static void Main()
    {
        byte x;
        string y;
        /*<bind>*/(x, y) = new C()/*</bind>*/;
    }

    public void Deconstruct(out int a, out int b)
    {
        a = b = 1;
    }
}
";
            string expectedOperationTree = @"
IDeconstructionAssignmentOperation (OperationKind.DeconstructionAssignment, Type: ?, IsInvalid) (Syntax: '(x, y) = new C()')
  Left: 
    ITupleOperation (OperationKind.Tuple, Type: (System.Byte x, System.String y), IsInvalid) (Syntax: '(x, y)')
      NaturalType: (System.Byte x, System.String y)
      Elements(2):
          ILocalReferenceOperation: x (OperationKind.LocalReference, Type: System.Byte, IsInvalid) (Syntax: 'x')
          ILocalReferenceOperation: y (OperationKind.LocalReference, Type: System.String, IsInvalid) (Syntax: 'y')
  Right: 
    IObjectCreationOperation (Constructor: C..ctor()) (OperationKind.ObjectCreation, Type: C) (Syntax: 'new C()')
      Arguments(0)
      Initializer: 
        null
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS0266: Cannot implicitly convert type 'int' to 'byte'. An explicit conversion exists (are you missing a cast?)
                //         /*<bind>*/(x, y) = new C()/*</bind>*/;
                Diagnostic(ErrorCode.ERR_NoImplicitConvCast, "x").WithArguments("int", "byte").WithLocation(8, 20),
                // CS0029: Cannot implicitly convert type 'int' to 'string'
                //         /*<bind>*/(x, y) = new C()/*</bind>*/;
                Diagnostic(ErrorCode.ERR_NoImplicitConv, "y").WithArguments("int", "string").WithLocation(8, 23)
            };

            VerifyOperationTreeAndDiagnosticsForTest<AssignmentExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [Fact]
        public void ExpressionType()
        {
            string source = @"
class C
{
    static void Main()
    {
        int x, y;
        var type = ((x, y) = new C()).GetType();
        System.Console.Write(type.ToString());
    }

    public void Deconstruct(out int a, out int b)
    {
        a = b = 1;
    }
}
";

            var comp = CreateCompilation(source, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics();
            CompileAndVerify(comp, expectedOutput: "System.ValueTuple`2[System.Int32,System.Int32]");
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void ExpressionType_IOperation()
        {
            string source = @"
class C
{
    static void Main()
    {
        int x, y;
        var type = (/*<bind>*/(x, y) = new C()/*</bind>*/).GetType();
        System.Console.Write(type.ToString());
    }

    public void Deconstruct(out int a, out int b)
    {
        a = b = 1;
    }
}
";
            string expectedOperationTree = @"
IDeconstructionAssignmentOperation (OperationKind.DeconstructionAssignment, Type: (System.Int32 x, System.Int32 y)) (Syntax: '(x, y) = new C()')
  Left: 
    ITupleOperation (OperationKind.Tuple, Type: (System.Int32 x, System.Int32 y)) (Syntax: '(x, y)')
      NaturalType: (System.Int32 x, System.Int32 y)
      Elements(2):
          ILocalReferenceOperation: x (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'x')
          ILocalReferenceOperation: y (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'y')
  Right: 
    IObjectCreationOperation (Constructor: C..ctor()) (OperationKind.ObjectCreation, Type: C) (Syntax: 'new C()')
      Arguments(0)
      Initializer: 
        null
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<AssignmentExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void LambdaStillNotValidStatement()
        {
            string source = @"
class C
{
    static void Main()
    {
        (a) => a;
    }
}
";

            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (6,9): error CS0201: Only assignment, call, increment, decrement, await, and new object expressions can be used as a statement
                //         (a) => a;
                Diagnostic(ErrorCode.ERR_IllegalStatement, "(a) => a").WithLocation(6, 9)
                );
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void LambdaWithBodyStillNotValidStatement()
        {
            string source = @"
class C
{
    static void Main()
    {
        /*<bind>*/(a, b) => { }/*</bind>*/;
    }
}
";
            string expectedOperationTree = @"
IAnonymousFunctionOperation (Symbol: lambda expression) (OperationKind.AnonymousFunction, Type: null, IsInvalid) (Syntax: '(a, b) => { }')
  IBlockOperation (0 statements) (OperationKind.Block, Type: null, IsInvalid) (Syntax: '{ }')
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS0201: Only assignment, call, increment, decrement, await, and new object expressions can be used as a statement
                //         /*<bind>*/(a, b) => { }/*</bind>*/;
                Diagnostic(ErrorCode.ERR_IllegalStatement, "(a, b) => { }").WithLocation(6, 19)
            };

            VerifyOperationTreeAndDiagnosticsForTest<ParenthesizedLambdaExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void CastButNotCast()
        {
            // int and string must be types, so (int, string) must be type and ((int, string)) a cast, but then .String() cannot follow a cast...
            string source = @"
class C
{
    static void Main()
    {
        /*<bind>*/((int, string)).ToString()/*</bind>*/;
    }
}
";
            string expectedOperationTree = @"
IInvocationOperation (virtual System.String (System.Int32, System.String).ToString()) (OperationKind.Invocation, Type: System.String, IsInvalid) (Syntax: '((int, stri ... .ToString()')
  Instance Receiver: 
    ITupleOperation (OperationKind.Tuple, Type: (System.Int32, System.String), IsInvalid) (Syntax: '(int, string)')
      NaturalType: (System.Int32, System.String)
      Elements(2):
          IOperation:  (OperationKind.None, Type: System.Int32, IsInvalid) (Syntax: 'int')
          IOperation:  (OperationKind.None, Type: System.String, IsInvalid) (Syntax: 'string')
  Arguments(0)
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS1525: Invalid expression term 'int'
                //         /*<bind>*/((int, string)).ToString()/*</bind>*/;
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, "int").WithArguments("int").WithLocation(6, 21),
                // CS1525: Invalid expression term 'string'
                //         /*<bind>*/((int, string)).ToString()/*</bind>*/;
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, "string").WithArguments("string").WithLocation(6, 26)
            };

            VerifyOperationTreeAndDiagnosticsForTest<InvocationExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [Fact, CompilerTrait(CompilerFeature.RefLocalsReturns)]
        [WorkItem(12283, "https://github.com/dotnet/roslyn/issues/12283")]
        public void RefReturningMethod2()
        {
            string source = @"
class C
{
    static int i;

    static void Main()
    {
        (M(), M()) = new C();
        System.Console.Write(i);
    }

    static ref int M()
    {
        System.Console.Write(""M "");
        return ref i;
    }

    void Deconstruct(out int i, out int j)
    {
        i = 42;
        j = 43;
    }
}
";

            var comp = CompileAndVerify(source, expectedOutput: "M M 43");
            comp.VerifyDiagnostics(
                );
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact, CompilerTrait(CompilerFeature.RefLocalsReturns)]
        [WorkItem(12283, "https://github.com/dotnet/roslyn/issues/12283")]
        public void RefReturningMethod2_IOperation()
        {
            string source = @"
class C
{
    static int i;

    static void Main()
    {
        /*<bind>*/(M(), M()) = new C()/*</bind>*/;
        System.Console.Write(i);
    }

    static ref int M()
    {
        System.Console.Write(""M "");
        return ref i;
    }

    void Deconstruct(out int i, out int j)
    {
        i = 42;
        j = 43;
    }
}
";
            string expectedOperationTree = @"
IDeconstructionAssignmentOperation (OperationKind.DeconstructionAssignment, Type: (System.Int32, System.Int32)) (Syntax: '(M(), M()) = new C()')
  Left: 
    ITupleOperation (OperationKind.Tuple, Type: (System.Int32, System.Int32)) (Syntax: '(M(), M())')
      NaturalType: (System.Int32, System.Int32)
      Elements(2):
          IInvocationOperation (ref System.Int32 C.M()) (OperationKind.Invocation, Type: System.Int32) (Syntax: 'M()')
            Instance Receiver: 
              null
            Arguments(0)
          IInvocationOperation (ref System.Int32 C.M()) (OperationKind.Invocation, Type: System.Int32) (Syntax: 'M()')
            Instance Receiver: 
              null
            Arguments(0)
  Right: 
    IObjectCreationOperation (Constructor: C..ctor()) (OperationKind.ObjectCreation, Type: C) (Syntax: 'new C()')
      Arguments(0)
      Initializer: 
        null
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<AssignmentExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void UninitializedRight()
        {
            string source = @"
class C
{
    static void Main()
    {
        int x;
        /*<bind>*/(x, x) = x/*</bind>*/;
    }
}
static class D
{
    public static void Deconstruct(this int input, out int output, out int output2) { output = input; output2 = input; }
}
";
            string expectedOperationTree = @"
IDeconstructionAssignmentOperation (OperationKind.DeconstructionAssignment, Type: (System.Int32, System.Int32), IsInvalid) (Syntax: '(x, x) = x')
  Left: 
    ITupleOperation (OperationKind.Tuple, Type: (System.Int32, System.Int32)) (Syntax: '(x, x)')
      NaturalType: (System.Int32, System.Int32)
      Elements(2):
          ILocalReferenceOperation: x (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'x')
          ILocalReferenceOperation: x (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'x')
  Right: 
    ILocalReferenceOperation: x (OperationKind.LocalReference, Type: System.Int32, IsInvalid) (Syntax: 'x')
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS0165: Use of unassigned local variable 'x'
                //         /*<bind>*/(x, x) = x/*</bind>*/;
                Diagnostic(ErrorCode.ERR_UseDefViolation, "x").WithArguments("x").WithLocation(7, 28)
            };

            VerifyOperationTreeAndDiagnosticsForTest<AssignmentExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void NullRight()
        {
            string source = @"
class C
{
    static void Main()
    {
        int x;
        /*<bind>*/(x, x) = null/*</bind>*/;
    }
}

";
            string expectedOperationTree = @"
IDeconstructionAssignmentOperation (OperationKind.DeconstructionAssignment, Type: System.Void, IsInvalid) (Syntax: '(x, x) = null')
  Left: 
    ITupleOperation (OperationKind.Tuple, Type: (System.Int32, System.Int32)) (Syntax: '(x, x)')
      NaturalType: (System.Int32, System.Int32)
      Elements(2):
          ILocalReferenceOperation: x (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'x')
          ILocalReferenceOperation: x (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'x')
  Right: 
    ILiteralOperation (OperationKind.Literal, Type: null, Constant: null, IsInvalid) (Syntax: 'null')
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS8131: Deconstruct assignment requires an expression with a type on the right-hand-side.
                //         /*<bind>*/(x, x) = null/*</bind>*/;
                Diagnostic(ErrorCode.ERR_DeconstructRequiresExpression, "null").WithLocation(7, 28)
            };

            VerifyOperationTreeAndDiagnosticsForTest<AssignmentExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void ErrorRight()
        {
            string source = @"
class C
{
    static void Main()
    {
        int x;
        /*<bind>*/(x, x) = undeclared/*</bind>*/;
    }
}
";
            string expectedOperationTree = @"
IDeconstructionAssignmentOperation (OperationKind.DeconstructionAssignment, Type: System.Void, IsInvalid) (Syntax: '(x, x) = undeclared')
  Left: 
    ITupleOperation (OperationKind.Tuple, Type: (System.Int32, System.Int32)) (Syntax: '(x, x)')
      NaturalType: (System.Int32, System.Int32)
      Elements(2):
          ILocalReferenceOperation: x (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'x')
          ILocalReferenceOperation: x (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'x')
  Right: 
    IInvalidOperation (OperationKind.Invalid, Type: ?, IsInvalid) (Syntax: 'undeclared')
      Children(0)
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS0103: The name 'undeclared' does not exist in the current context
                //         /*<bind>*/(x, x) = undeclared/*</bind>*/;
                Diagnostic(ErrorCode.ERR_NameNotInContext, "undeclared").WithArguments("undeclared").WithLocation(7, 28)
            };

            VerifyOperationTreeAndDiagnosticsForTest<AssignmentExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void VoidRight()
        {
            string source = @"
class C
{
    static void Main()
    {
        int x;
        /*<bind>*/(x, x) = M()/*</bind>*/;
    }
    static void M() { }
}
";
            string expectedOperationTree = @"
IDeconstructionAssignmentOperation (OperationKind.DeconstructionAssignment, Type: ?, IsInvalid) (Syntax: '(x, x) = M()')
  Left: 
    ITupleOperation (OperationKind.Tuple, Type: (System.Int32, System.Int32)) (Syntax: '(x, x)')
      NaturalType: (System.Int32, System.Int32)
      Elements(2):
          ILocalReferenceOperation: x (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'x')
          ILocalReferenceOperation: x (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'x')
  Right: 
    IInvocationOperation (void C.M()) (OperationKind.Invocation, Type: System.Void, IsInvalid) (Syntax: 'M()')
      Instance Receiver: 
        null
      Arguments(0)
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS1061: 'void' does not contain a definition for 'Deconstruct' and no extension method 'Deconstruct' accepting a first argument of type 'void' could be found (are you missing a using directive or an assembly reference?)
                //         /*<bind>*/(x, x) = M()/*</bind>*/;
                Diagnostic(ErrorCode.ERR_NoSuchMemberOrExtension, "M()").WithArguments("void", "Deconstruct").WithLocation(7, 28)
            };

            VerifyOperationTreeAndDiagnosticsForTest<AssignmentExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void AssigningTupleWithNoConversion()
        {
            string source = @"
class C
{
    static void Main()
    {
        byte x;
        string y;

        /*<bind>*/(x, y) = (1, 2)/*</bind>*/;
    }
}
";
            string expectedOperationTree = @"
IDeconstructionAssignmentOperation (OperationKind.DeconstructionAssignment, Type: (System.Byte x, System.String y), IsInvalid) (Syntax: '(x, y) = (1, 2)')
  Left: 
    ITupleOperation (OperationKind.Tuple, Type: (System.Byte x, System.String y)) (Syntax: '(x, y)')
      NaturalType: (System.Byte x, System.String y)
      Elements(2):
          ILocalReferenceOperation: x (OperationKind.LocalReference, Type: System.Byte) (Syntax: 'x')
          ILocalReferenceOperation: y (OperationKind.LocalReference, Type: System.String) (Syntax: 'y')
  Right: 
    IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: (System.Byte, System.String), IsInvalid, IsImplicit) (Syntax: '(1, 2)')
      Conversion: CommonConversion (Exists: False, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
      Operand: 
        ITupleOperation (OperationKind.Tuple, Type: (System.Int32, System.Int32), IsInvalid) (Syntax: '(1, 2)')
          NaturalType: (System.Int32, System.Int32)
          Elements(2):
              ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
              ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 2, IsInvalid) (Syntax: '2')
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS0029: Cannot implicitly convert type 'int' to 'string'
                //         /*<bind>*/(x, y) = (1, 2)/*</bind>*/;
                Diagnostic(ErrorCode.ERR_NoImplicitConv, "2").WithArguments("int", "string").WithLocation(9, 32)
            };

            VerifyOperationTreeAndDiagnosticsForTest<AssignmentExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void NotAssignable()
        {
            string source = @"
class C
{
    static void Main()
    {
        /*<bind>*/(1, P) = (1, 2)/*</bind>*/;
    }
    static int P { get { return 1; } }
}
";
            string expectedOperationTree = @"
IDeconstructionAssignmentOperation (OperationKind.DeconstructionAssignment, Type: (System.Int32, System.Int32 P), IsInvalid) (Syntax: '(1, P) = (1, 2)')
  Left: 
    ITupleOperation (OperationKind.Tuple, Type: (System.Int32, System.Int32 P), IsInvalid) (Syntax: '(1, P)')
      NaturalType: (System.Int32, System.Int32 P)
      Elements(2):
          IInvalidOperation (OperationKind.Invalid, Type: System.Int32, IsInvalid, IsImplicit) (Syntax: '1')
            Children(1):
                ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1, IsInvalid) (Syntax: '1')
          IInvalidOperation (OperationKind.Invalid, Type: System.Int32, IsInvalid, IsImplicit) (Syntax: 'P')
            Children(1):
                IPropertyReferenceOperation: System.Int32 C.P { get; } (Static) (OperationKind.PropertyReference, Type: System.Int32, IsInvalid) (Syntax: 'P')
                  Instance Receiver: 
                    null
  Right: 
    ITupleOperation (OperationKind.Tuple, Type: (System.Int32, System.Int32)) (Syntax: '(1, 2)')
      NaturalType: (System.Int32, System.Int32)
      Elements(2):
          ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
          ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 2) (Syntax: '2')
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS0131: The left-hand side of an assignment must be a variable, property or indexer
                //         /*<bind>*/(1, P) = (1, 2)/*</bind>*/;
                Diagnostic(ErrorCode.ERR_AssgLvalueExpected, "1").WithLocation(6, 20),
                // CS0200: Property or indexer 'C.P' cannot be assigned to -- it is read only
                //         /*<bind>*/(1, P) = (1, 2)/*</bind>*/;
                Diagnostic(ErrorCode.ERR_AssgReadonlyProp, "P").WithArguments("C.P").WithLocation(6, 23)
            };

            VerifyOperationTreeAndDiagnosticsForTest<AssignmentExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [Fact]
        public void TupleWithUseSiteError()
        {
            string source = @"

namespace System
{
    public struct ValueTuple<T1, T2>
    {
        public T1 Item1;

        public ValueTuple(T1 item1, T2 item2)
        {
            this.Item1 = item1;
        }
    }
}
class C
{
    static void Main()
    {
        int x;
        int y;

        (x, y) = (1, 2);
        System.Console.WriteLine($""{x} {y}"");
    }
}
";

            var comp = CreateCompilation(source, assemblyName: "comp", options: TestOptions.DebugExe);
            comp.VerifyDiagnostics();
            comp.VerifyEmitDiagnostics();
            CompileAndVerify(comp, expectedOutput: "1 2");
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void TupleWithUseSiteError_IOperation()
        {
            string source = @"
namespace System
{
    struct ValueTuple<T1, T2>
    {
        public T1 Item1;

        public ValueTuple(T1 item1, T2 item2)
        {
            this.Item1 = item1;
        }
    }
}
class C
{
    static void Main()
    {
        int x;
        int y;

        /*<bind>*/(x, y) = (1, 2)/*</bind>*/;
        System.Console.WriteLine($""{x} {y}"");
    }
}
";
            string expectedOperationTree = @"
IDeconstructionAssignmentOperation (OperationKind.DeconstructionAssignment, Type: (System.Int32 x, System.Int32 y)) (Syntax: '(x, y) = (1, 2)')
  Left: 
    ITupleOperation (OperationKind.Tuple, Type: (System.Int32 x, System.Int32 y)) (Syntax: '(x, y)')
      NaturalType: (System.Int32 x, System.Int32 y)
      Elements(2):
          ILocalReferenceOperation: x (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'x')
          ILocalReferenceOperation: y (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'y')
  Right: 
    ITupleOperation (OperationKind.Tuple, Type: (System.Int32, System.Int32)) (Syntax: '(1, 2)')
      NaturalType: (System.Int32, System.Int32)
      Elements(2):
          ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
          ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 2) (Syntax: '2')
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<AssignmentExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void AssignUsingAmbiguousDeconstruction()
        {
            string source = @"
class Base
{
    public void Deconstruct(out int a, out int b) { a = 1; b = 2; }
    public void Deconstruct(out long a, out long b) { a = 1; b = 2; }
}
class C : Base
{
    static void Main()
    {
        int x, y;
        /*<bind>*/(x, y) = new C()/*</bind>*/;

        System.Console.WriteLine(x + "" "" + y);
    }
}
";
            string expectedOperationTree = @"
IDeconstructionAssignmentOperation (OperationKind.DeconstructionAssignment, Type: ?, IsInvalid) (Syntax: '(x, y) = new C()')
  Left: 
    ITupleOperation (OperationKind.Tuple, Type: (System.Int32 x, System.Int32 y)) (Syntax: '(x, y)')
      NaturalType: (System.Int32 x, System.Int32 y)
      Elements(2):
          ILocalReferenceOperation: x (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'x')
          ILocalReferenceOperation: y (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'y')
  Right: 
    IObjectCreationOperation (Constructor: C..ctor()) (OperationKind.ObjectCreation, Type: C, IsInvalid) (Syntax: 'new C()')
      Arguments(0)
      Initializer: 
        null
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // file.cs(12,28): error CS0121: The call is ambiguous between the following methods or properties: 'Base.Deconstruct(out int, out int)' and 'Base.Deconstruct(out long, out long)'
                //         /*<bind>*/(x, y) = new C()/*</bind>*/;
                Diagnostic(ErrorCode.ERR_AmbigCall, "new C()").WithArguments("Base.Deconstruct(out int, out int)", "Base.Deconstruct(out long, out long)").WithLocation(12, 28)
            };

            VerifyOperationTreeAndDiagnosticsForTest<AssignmentExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void DeconstructIsDynamicField()
        {
            string source = @"
class C
{
    static void Main()
    {
        int x, y;
        /*<bind>*/(x, y) = new C()/*</bind>*/;

    }
    public dynamic Deconstruct = null;
}
";
            string expectedOperationTree = @"
IDeconstructionAssignmentOperation (OperationKind.DeconstructionAssignment, Type: ?, IsInvalid) (Syntax: '(x, y) = new C()')
  Left: 
    ITupleOperation (OperationKind.Tuple, Type: (System.Int32 x, System.Int32 y)) (Syntax: '(x, y)')
      NaturalType: (System.Int32 x, System.Int32 y)
      Elements(2):
          ILocalReferenceOperation: x (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'x')
          ILocalReferenceOperation: y (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'y')
  Right: 
    IObjectCreationOperation (Constructor: C..ctor()) (OperationKind.ObjectCreation, Type: C, IsInvalid) (Syntax: 'new C()')
      Arguments(0)
      Initializer: 
        null
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS8129: No suitable Deconstruct instance or extension method was found for type 'C', with 2 out parameters and a void return type.
                //         /*<bind>*/(x, y) = new C()/*</bind>*/;
                Diagnostic(ErrorCode.ERR_MissingDeconstruct, "new C()").WithArguments("C", "2").WithLocation(7, 28)
            };

            VerifyOperationTreeAndDiagnosticsForTest<AssignmentExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void DeconstructIsField()
        {
            string source = @"
class C
{
    static void Main()
    {
        int x, y;
        /*<bind>*/(x, y) = new C()/*</bind>*/;

    }
    public object Deconstruct = null;
}
";
            string expectedOperationTree = @"
IDeconstructionAssignmentOperation (OperationKind.DeconstructionAssignment, Type: ?, IsInvalid) (Syntax: '(x, y) = new C()')
  Left: 
    ITupleOperation (OperationKind.Tuple, Type: (System.Int32 x, System.Int32 y)) (Syntax: '(x, y)')
      NaturalType: (System.Int32 x, System.Int32 y)
      Elements(2):
          ILocalReferenceOperation: x (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'x')
          ILocalReferenceOperation: y (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'y')
  Right: 
    IObjectCreationOperation (Constructor: C..ctor()) (OperationKind.ObjectCreation, Type: C, IsInvalid) (Syntax: 'new C()')
      Arguments(0)
      Initializer: 
        null
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // (7,28): error CS1955: Non-invocable member 'C.Deconstruct' cannot be used like a method.
                //         /*<bind>*/(x, y) = new C()/*</bind>*/;
                Diagnostic(ErrorCode.ERR_NonInvocableMemberCalled, "new C()").WithArguments("C.Deconstruct").WithLocation(7, 28)
            };

            VerifyOperationTreeAndDiagnosticsForTest<AssignmentExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [Fact]
        public void CannotDeconstructRefTuple22()
        {
            string template = @"
using System;
class C
{
    static void Main()
    {
        int VARIABLES; // int x1, x2, ...
        (VARIABLES) = CreateLongRef(1, 2, 3, 4, 5, 6, 7, CreateLongRef(8, 9, 10, 11, 12, 13, 14, Tuple.Create(15, 16, 17, 18, 19, 20, 21, 22)));
    }

    public static Tuple<T1, T2, T3, T4, T5, T6, T7, TRest> CreateLongRef<T1, T2, T3, T4, T5, T6, T7, TRest>(T1 item1, T2 item2, T3 item3, T4 item4, T5 item5, T6 item6, T7 item7, TRest rest) =>
        new Tuple<T1, T2, T3, T4, T5, T6, T7, TRest>(item1, item2, item3, item4, item5, item6, item7, rest);
}
";
            var tuple = String.Join(", ", Enumerable.Range(1, 22).Select(n => n.ToString()));
            var variables = String.Join(", ", Enumerable.Range(1, 22).Select(n => $"x{n}"));

            var source = template.Replace("VARIABLES", variables).Replace("TUPLE", tuple);

            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (8,113): error CS1501: No overload for method 'Deconstruct' takes 22 arguments
                //         (x1, x2, x3, x4, x5, x6, x7, x8, x9, x10, x11, x12, x13, x14, x15, x16, x17, x18, x19, x20, x21, x22) = CreateLongRef(1, 2, 3, 4, 5, 6, 7, CreateLongRef(8, 9, 10, 11, 12, 13, 14, Tuple.Create(15, 16, 17, 18, 19, 20, 21, 22)));
                Diagnostic(ErrorCode.ERR_BadArgCount, "CreateLongRef(1, 2, 3, 4, 5, 6, 7, CreateLongRef(8, 9, 10, 11, 12, 13, 14, Tuple.Create(15, 16, 17, 18, 19, 20, 21, 22)))").WithArguments("Deconstruct", "22").WithLocation(8, 113));
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void DeconstructUsingDynamicMethod()
        {
            string source = @"
class C
{
    static void Main()
    {
        int x;
        string y;

        dynamic c = new C();
        /*<bind>*/(x, y) = c/*</bind>*/;
    }
    public void Deconstruct(out int a, out string b) { a = 1; b = ""hello""; }
}
";
            string expectedOperationTree = @"
IDeconstructionAssignmentOperation (OperationKind.DeconstructionAssignment, Type: ?, IsInvalid) (Syntax: '(x, y) = c')
  Left: 
    ITupleOperation (OperationKind.Tuple, Type: (System.Int32 x, System.String y)) (Syntax: '(x, y)')
      NaturalType: (System.Int32 x, System.String y)
      Elements(2):
          ILocalReferenceOperation: x (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'x')
          ILocalReferenceOperation: y (OperationKind.LocalReference, Type: System.String) (Syntax: 'y')
  Right: 
    ILocalReferenceOperation: c (OperationKind.LocalReference, Type: dynamic, IsInvalid) (Syntax: 'c')
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS8133: Cannot deconstruct dynamic objects.
                //         /*<bind>*/(x, y) = c/*</bind>*/;
                Diagnostic(ErrorCode.ERR_CannotDeconstructDynamic, "c").WithLocation(10, 28)
            };

            VerifyOperationTreeAndDiagnosticsForTest<AssignmentExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void DeconstructMethodInaccessible()
        {
            string source = @"
class C
{
    static void Main()
    {
        int x;
        string y;

        /*<bind>*/(x, y) = new C1()/*</bind>*/;
    }
}
class C1
{
    protected void Deconstruct(out int a, out string b) { a = 1; b = ""hello""; }
}
";
            string expectedOperationTree = @"
IDeconstructionAssignmentOperation (OperationKind.DeconstructionAssignment, Type: ?, IsInvalid) (Syntax: '(x, y) = new C1()')
  Left: 
    ITupleOperation (OperationKind.Tuple, Type: (System.Int32 x, System.String y)) (Syntax: '(x, y)')
      NaturalType: (System.Int32 x, System.String y)
      Elements(2):
          ILocalReferenceOperation: x (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'x')
          ILocalReferenceOperation: y (OperationKind.LocalReference, Type: System.String) (Syntax: 'y')
  Right: 
    IObjectCreationOperation (Constructor: C1..ctor()) (OperationKind.ObjectCreation, Type: C1, IsInvalid) (Syntax: 'new C1()')
      Arguments(0)
      Initializer: 
        null
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // (9,28): error CS0122: 'C1.Deconstruct(out int, out string)' is inaccessible due to its protection level
                //         /*<bind>*/(x, y) = new C1()/*</bind>*/;
                Diagnostic(ErrorCode.ERR_BadAccess, "new C1()").WithArguments("C1.Deconstruct(out int, out string)").WithLocation(9, 28)
            };

            VerifyOperationTreeAndDiagnosticsForTest<AssignmentExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [Fact]
        public void DeconstructHasUseSiteError()
        {
            string libMissingSource = @"public class Missing { }";

            string libSource = @"
public class C
{
    public void Deconstruct(out Missing a, out Missing b) { a = new Missing(); b = new Missing(); }
}
";

            string source = @"
class C1
{
    static void Main()
    {
        object x, y;
        (x, y) = new C();
    }
}
";
            var libMissingComp = CreateCompilation(new string[] { libMissingSource }, assemblyName: "libMissingComp").VerifyDiagnostics();
            var libMissingRef = libMissingComp.EmitToImageReference();

            var libComp = CreateCompilation(new string[] { libSource }, references: new[] { libMissingRef }, parseOptions: TestOptions.Regular).VerifyDiagnostics();
            var libRef = libComp.EmitToImageReference();

            var comp = CreateCompilation(new string[] { source }, references: new[] { libRef });
            comp.VerifyDiagnostics(
                // 0.cs(7,18): error CS0012: The type 'Missing' is defined in an assembly that is not referenced. You must add a reference to assembly 'libMissingComp, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'.
                //         (x, y) = new C();
                Diagnostic(ErrorCode.ERR_NoTypeDef, "new C()").WithArguments("Missing", "libMissingComp, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null").WithLocation(7, 18));
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void StaticDeconstruct()
        {
            string source = @"
class C
{
    static void Main()
    {
        int x;
        string y;

        /*<bind>*/(x, y) = new C()/*</bind>*/;
    }
    public static void Deconstruct(out int a, out string b) { a = 1; b = ""hello""; }
}
";
            string expectedOperationTree = @"
IDeconstructionAssignmentOperation (OperationKind.DeconstructionAssignment, Type: ?, IsInvalid) (Syntax: '(x, y) = new C()')
  Left: 
    ITupleOperation (OperationKind.Tuple, Type: (System.Int32 x, System.String y)) (Syntax: '(x, y)')
      NaturalType: (System.Int32 x, System.String y)
      Elements(2):
          ILocalReferenceOperation: x (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'x')
          ILocalReferenceOperation: y (OperationKind.LocalReference, Type: System.String) (Syntax: 'y')
  Right: 
    IObjectCreationOperation (Constructor: C..ctor()) (OperationKind.ObjectCreation, Type: C, IsInvalid) (Syntax: 'new C()')
      Arguments(0)
      Initializer: 
        null
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // (9,28): error CS0176: Member 'C.Deconstruct(out int, out string)' cannot be accessed with an instance reference; qualify it with a type name instead
                //         /*<bind>*/(x, y) = new C()/*</bind>*/;
                Diagnostic(ErrorCode.ERR_ObjectProhibited, "new C()").WithArguments("C.Deconstruct(out int, out string)").WithLocation(9, 28)
            };

            VerifyOperationTreeAndDiagnosticsForTest<AssignmentExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [Fact]
        public void AssignmentTypeIsValueTuple()
        {
            string source = @"
class C
{
    public static void Main()
    {
        long x; string y;

        var z1 = ((x, y) = new C()).ToString();

        var z2 = ((x, y) = new C());
        var z3 = (x, y) = new C();

        System.Console.Write($""{z1} {z2.ToString()} {z3.ToString()}"");
    }

    public void Deconstruct(out int a, out string b)
    {
        a = 1;
        b = ""hello"";
    }
}
";
            var comp = CompileAndVerify(source, expectedOutput: "(1, hello) (1, hello) (1, hello)");
            comp.VerifyDiagnostics();
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void AssignmentTypeIsValueTuple_IOperation()
        {
            string source = @"
class C
{
    public static void Main()
    {
        long x; string y;

        var z1 = ((x, y) = new C()).ToString();

        var z2 = (/*<bind>*/(x, y) = new C()/*</bind>*/);
        var z3 = (x, y) = new C();

        System.Console.Write($""{z1} {z2.ToString()} {z3.ToString()}"");
    }

    public void Deconstruct(out int a, out string b)
    {
        a = 1;
        b = ""hello"";
    }
}
";
            string expectedOperationTree = @"
IDeconstructionAssignmentOperation (OperationKind.DeconstructionAssignment, Type: (System.Int64 x, System.String y)) (Syntax: '(x, y) = new C()')
  Left: 
    ITupleOperation (OperationKind.Tuple, Type: (System.Int64 x, System.String y)) (Syntax: '(x, y)')
      NaturalType: (System.Int64 x, System.String y)
      Elements(2):
          ILocalReferenceOperation: x (OperationKind.LocalReference, Type: System.Int64) (Syntax: 'x')
          ILocalReferenceOperation: y (OperationKind.LocalReference, Type: System.String) (Syntax: 'y')
  Right: 
    IObjectCreationOperation (Constructor: C..ctor()) (OperationKind.ObjectCreation, Type: C) (Syntax: 'new C()')
      Arguments(0)
      Initializer: 
        null
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<AssignmentExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [Fact]
        public void NestedAssignmentTypeIsValueTuple()
        {
            string source = @"
class C
{
    public static void Main()
    {
        long x1; string x2; int x3;

        var y = ((x1, x2), x3) = (new C(), 3);

        System.Console.Write($""{y.ToString()}"");
    }

    public void Deconstruct(out int a, out string b)
    {
        a = 1;
        b = ""hello"";
    }
}
";
            var comp = CompileAndVerify(source, expectedOutput: "((1, hello), 3)");
            comp.VerifyDiagnostics();
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void NestedAssignmentTypeIsValueTuple_IOperation()
        {
            string source = @"
class C
{
    public static void Main()
    {
        long x1; string x2; int x3;

        var y = /*<bind>*/((x1, x2), x3) = (new C(), 3)/*</bind>*/;

        System.Console.Write($""{y.ToString()}"");
    }

    public void Deconstruct(out int a, out string b)
    {
        a = 1;
        b = ""hello"";
    }
}

";
            string expectedOperationTree = @"
IDeconstructionAssignmentOperation (OperationKind.DeconstructionAssignment, Type: ((System.Int64 x1, System.String x2), System.Int32 x3)) (Syntax: '((x1, x2),  ... new C(), 3)')
  Left: 
    ITupleOperation (OperationKind.Tuple, Type: ((System.Int64 x1, System.String x2), System.Int32 x3)) (Syntax: '((x1, x2), x3)')
      NaturalType: ((System.Int64 x1, System.String x2), System.Int32 x3)
      Elements(2):
          ITupleOperation (OperationKind.Tuple, Type: (System.Int64 x1, System.String x2)) (Syntax: '(x1, x2)')
            NaturalType: (System.Int64 x1, System.String x2)
            Elements(2):
                ILocalReferenceOperation: x1 (OperationKind.LocalReference, Type: System.Int64) (Syntax: 'x1')
                ILocalReferenceOperation: x2 (OperationKind.LocalReference, Type: System.String) (Syntax: 'x2')
          ILocalReferenceOperation: x3 (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'x3')
  Right: 
    ITupleOperation (OperationKind.Tuple, Type: (C, System.Int32)) (Syntax: '(new C(), 3)')
      NaturalType: (C, System.Int32)
      Elements(2):
          IObjectCreationOperation (Constructor: C..ctor()) (OperationKind.ObjectCreation, Type: C) (Syntax: 'new C()')
            Arguments(0)
            Initializer: 
              null
          ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 3) (Syntax: '3')
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<AssignmentExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [Fact]
        public void AssignmentReturnsLongValueTuple()
        {
            string source = @"
class C
{
    public static void Main()
    {
        long x;
        var y = (x, x, x, x, x, x, x, x, x) = new C();
        System.Console.Write($""{y.ToString()}"");
    }

    public void Deconstruct(out int x1, out int x2, out int x3, out int x4, out int x5, out int x6, out int x7, out int x8, out int x9)
    {
        x1 = x2 = x3 = x4 = x5 = x6 = x7 = x8 = 1;
        x9 = 9;
    }
}
";
            var comp = CompileAndVerify(source, expectedOutput: "(1, 1, 1, 1, 1, 1, 1, 1, 9)");
            comp.VerifyDiagnostics();

            var tree = comp.Compilation.SyntaxTrees.First();
            var model = comp.Compilation.GetSemanticModel(tree, ignoreAccessibility: false);
            var nodes = tree.GetCompilationUnitRoot().DescendantNodes();
            var y = nodes.OfType<VariableDeclaratorSyntax>().Skip(1).First();

            Assert.Equal("y = (x, x, x, x, x, x, x, x, x) = new C()", y.ToFullString());

            Assert.Equal("(System.Int64, System.Int64, System.Int64, System.Int64, System.Int64, System.Int64, System.Int64, System.Int64, System.Int64) y",
                model.GetDeclaredSymbol(y).ToTestDisplayString());
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void AssignmentReturnsLongValueTuple_IOperation()
        {
            string source = @"
class C
{
    public static void Main()
    {
        long x;
        var /*<bind>*/y = (x, x, x, x, x, x, x, x, x) = new C()/*</bind>*/;
        System.Console.Write($""{y.ToString()}"");
    }

    public void Deconstruct(out int x1, out int x2, out int x3, out int x4, out int x5, out int x6, out int x7, out int x8, out int x9)
    {
        x1 = x2 = x3 = x4 = x5 = x6 = x7 = x8 = 1;
        x9 = 9;
    }
}
";
            string expectedOperationTree = @"
IVariableDeclaratorOperation (Symbol: (System.Int64, System.Int64, System.Int64, System.Int64, System.Int64, System.Int64, System.Int64, System.Int64, System.Int64) y) (OperationKind.VariableDeclarator, Type: null) (Syntax: 'y = (x, x,  ... ) = new C()')
  Initializer: 
    IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null) (Syntax: '= (x, x, x, ... ) = new C()')
      IDeconstructionAssignmentOperation (OperationKind.DeconstructionAssignment, Type: (System.Int64, System.Int64, System.Int64, System.Int64, System.Int64, System.Int64, System.Int64, System.Int64, System.Int64)) (Syntax: '(x, x, x, x ... ) = new C()')
        Left: 
          ITupleOperation (OperationKind.Tuple, Type: (System.Int64, System.Int64, System.Int64, System.Int64, System.Int64, System.Int64, System.Int64, System.Int64, System.Int64)) (Syntax: '(x, x, x, x ... x, x, x, x)')
            NaturalType: (System.Int64, System.Int64, System.Int64, System.Int64, System.Int64, System.Int64, System.Int64, System.Int64, System.Int64)
            Elements(9):
                ILocalReferenceOperation: x (OperationKind.LocalReference, Type: System.Int64) (Syntax: 'x')
                ILocalReferenceOperation: x (OperationKind.LocalReference, Type: System.Int64) (Syntax: 'x')
                ILocalReferenceOperation: x (OperationKind.LocalReference, Type: System.Int64) (Syntax: 'x')
                ILocalReferenceOperation: x (OperationKind.LocalReference, Type: System.Int64) (Syntax: 'x')
                ILocalReferenceOperation: x (OperationKind.LocalReference, Type: System.Int64) (Syntax: 'x')
                ILocalReferenceOperation: x (OperationKind.LocalReference, Type: System.Int64) (Syntax: 'x')
                ILocalReferenceOperation: x (OperationKind.LocalReference, Type: System.Int64) (Syntax: 'x')
                ILocalReferenceOperation: x (OperationKind.LocalReference, Type: System.Int64) (Syntax: 'x')
                ILocalReferenceOperation: x (OperationKind.LocalReference, Type: System.Int64) (Syntax: 'x')
        Right: 
          IObjectCreationOperation (Constructor: C..ctor()) (OperationKind.ObjectCreation, Type: C) (Syntax: 'new C()')
            Arguments(0)
            Initializer: 
              null
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<VariableDeclaratorSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [Fact]
        public void DeconstructWithoutValueTupleLibrary()
        {
            string source = @"
class C
{
    public static void Main()
    {
        long x;
        var y = (x, x) = new C();
        System.Console.Write(y.ToString());
    }

    public void Deconstruct(out int x1, out int x2)
    {
        x1 = x2 = 1;
    }
}
";
            var comp = CreateCompilationWithMscorlib40(source);
            comp.VerifyDiagnostics(
                // (7,17): error CS8179: Predefined type 'System.ValueTuple`2' is not defined or imported
                //         var y = (x, x) = new C();
                Diagnostic(ErrorCode.ERR_PredefinedValueTupleTypeNotFound, "(x, x)").WithArguments("System.ValueTuple`2").WithLocation(7, 17)
                );
        }

        [Fact]
        public void ChainedAssignment()
        {
            string source = @"
class C
{
    public static void Main()
    {
        long x1, x2;
        var y = (x1, x1) = (x2, x2) = new C();
        System.Console.Write($""{y.ToString()} {x1} {x2}"");
    }

    public void Deconstruct(out int a, out int b)
    {
        a = b = 1;
    }
}
";
            var comp = CreateCompilation(source, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics();
            CompileAndVerify(comp, expectedOutput: "(1, 1) 1 1");
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void ChainedAssignment_IOperation()
        {
            string source = @"
class C
{
    public static void Main()
    {
        long x1, x2;
        var y = /*<bind>*/(x1, x1) = (x2, x2) = new C()/*</bind>*/;
        System.Console.Write($""{y.ToString()} {x1} {x2}"");
    }

    public void Deconstruct(out int a, out int b)
    {
        a = b = 1;
    }
}
";
            string expectedOperationTree = @"
IDeconstructionAssignmentOperation (OperationKind.DeconstructionAssignment, Type: (System.Int64, System.Int64)) (Syntax: '(x1, x1) =  ... ) = new C()')
  Left: 
    ITupleOperation (OperationKind.Tuple, Type: (System.Int64, System.Int64)) (Syntax: '(x1, x1)')
      NaturalType: (System.Int64, System.Int64)
      Elements(2):
          ILocalReferenceOperation: x1 (OperationKind.LocalReference, Type: System.Int64) (Syntax: 'x1')
          ILocalReferenceOperation: x1 (OperationKind.LocalReference, Type: System.Int64) (Syntax: 'x1')
  Right: 
    IDeconstructionAssignmentOperation (OperationKind.DeconstructionAssignment, Type: (System.Int64, System.Int64)) (Syntax: '(x2, x2) = new C()')
      Left: 
        ITupleOperation (OperationKind.Tuple, Type: (System.Int64, System.Int64)) (Syntax: '(x2, x2)')
          NaturalType: (System.Int64, System.Int64)
          Elements(2):
              ILocalReferenceOperation: x2 (OperationKind.LocalReference, Type: System.Int64) (Syntax: 'x2')
              ILocalReferenceOperation: x2 (OperationKind.LocalReference, Type: System.Int64) (Syntax: 'x2')
      Right: 
        IObjectCreationOperation (Constructor: C..ctor()) (OperationKind.ObjectCreation, Type: C) (Syntax: 'new C()')
          Arguments(0)
          Initializer: 
            null
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<AssignmentExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void NestedTypelessTupleAssignment2()
        {
            string source = @"
class C
{
    static void Main()
    {
        int x, y, z; // int cannot be null

        /*<bind>*/(x, (y, z)) = (null, (null, null))/*</bind>*/;
        System.Console.WriteLine(""nothing"" + x + y + z);
    }
}
";
            string expectedOperationTree = @"
IDeconstructionAssignmentOperation (OperationKind.DeconstructionAssignment, Type: (System.Int32 x, (System.Int32 y, System.Int32 z)), IsInvalid) (Syntax: '(x, (y, z)) ... ull, null))')
  Left: 
    ITupleOperation (OperationKind.Tuple, Type: (System.Int32 x, (System.Int32 y, System.Int32 z))) (Syntax: '(x, (y, z))')
      NaturalType: (System.Int32 x, (System.Int32 y, System.Int32 z))
      Elements(2):
          ILocalReferenceOperation: x (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'x')
          ITupleOperation (OperationKind.Tuple, Type: (System.Int32 y, System.Int32 z)) (Syntax: '(y, z)')
            NaturalType: (System.Int32 y, System.Int32 z)
            Elements(2):
                ILocalReferenceOperation: y (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'y')
                ILocalReferenceOperation: z (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'z')
  Right: 
    IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: (System.Int32, (System.Int32, System.Int32)), IsInvalid, IsImplicit) (Syntax: '(null, (null, null))')
      Conversion: CommonConversion (Exists: False, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
      Operand: 
        ITupleOperation (OperationKind.Tuple, Type: null, IsInvalid) (Syntax: '(null, (null, null))')
          NaturalType: null
          Elements(2):
              ILiteralOperation (OperationKind.Literal, Type: null, Constant: null, IsInvalid) (Syntax: 'null')
              ITupleOperation (OperationKind.Tuple, Type: null, IsInvalid) (Syntax: '(null, null)')
                NaturalType: null
                Elements(2):
                    ILiteralOperation (OperationKind.Literal, Type: null, Constant: null, IsInvalid) (Syntax: 'null')
                    ILiteralOperation (OperationKind.Literal, Type: null, Constant: null, IsInvalid) (Syntax: 'null')
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS0037: Cannot convert null to 'int' because it is a non-nullable value type
                //         /*<bind>*/(x, (y, z)) = (null, (null, null))/*</bind>*/;
                Diagnostic(ErrorCode.ERR_ValueCantBeNull, "null").WithArguments("int").WithLocation(8, 34),
                // CS0037: Cannot convert null to 'int' because it is a non-nullable value type
                //         /*<bind>*/(x, (y, z)) = (null, (null, null))/*</bind>*/;
                Diagnostic(ErrorCode.ERR_ValueCantBeNull, "null").WithArguments("int").WithLocation(8, 41),
                // CS0037: Cannot convert null to 'int' because it is a non-nullable value type
                //         /*<bind>*/(x, (y, z)) = (null, (null, null))/*</bind>*/;
                Diagnostic(ErrorCode.ERR_ValueCantBeNull, "null").WithArguments("int").WithLocation(8, 47)
            };

            VerifyOperationTreeAndDiagnosticsForTest<AssignmentExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void TupleWithWrongCardinality()
        {
            string source = @"
class C
{
    static void Main()
    {
        int x, y, z;

        /*<bind>*/(x, y, z) = MakePair()/*</bind>*/;
    }

    public static (int, int) MakePair()
    {
        return (42, 42);
    }
}
";
            string expectedOperationTree = @"
IDeconstructionAssignmentOperation (OperationKind.DeconstructionAssignment, Type: ?, IsInvalid) (Syntax: '(x, y, z) = MakePair()')
  Left: 
    ITupleOperation (OperationKind.Tuple, Type: (System.Int32 x, System.Int32 y, System.Int32 z), IsInvalid) (Syntax: '(x, y, z)')
      NaturalType: (System.Int32 x, System.Int32 y, System.Int32 z)
      Elements(3):
          ILocalReferenceOperation: x (OperationKind.LocalReference, Type: System.Int32, IsInvalid) (Syntax: 'x')
          ILocalReferenceOperation: y (OperationKind.LocalReference, Type: System.Int32, IsInvalid) (Syntax: 'y')
          ILocalReferenceOperation: z (OperationKind.LocalReference, Type: System.Int32, IsInvalid) (Syntax: 'z')
  Right: 
    IInvocationOperation ((System.Int32, System.Int32) C.MakePair()) (OperationKind.Invocation, Type: (System.Int32, System.Int32), IsInvalid) (Syntax: 'MakePair()')
      Instance Receiver: 
        null
      Arguments(0)
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS8132: Cannot deconstruct a tuple of '2' elements into '3' variables.
                //         /*<bind>*/(x, y, z) = MakePair()/*</bind>*/;
                Diagnostic(ErrorCode.ERR_DeconstructWrongCardinality, "(x, y, z) = MakePair()").WithArguments("2", "3").WithLocation(8, 19)
            };

            VerifyOperationTreeAndDiagnosticsForTest<AssignmentExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void NestedTupleWithWrongCardinality()
        {
            string source = @"
class C
{
    static void Main()
    {
        int x, y, z, w;

        /*<bind>*/(x, (y, z, w)) = Pair.Create(42, (43, 44))/*</bind>*/;
    }
}
" + commonSource;

            string expectedOperationTree = @"
IDeconstructionAssignmentOperation (OperationKind.DeconstructionAssignment, Type: ?, IsInvalid) (Syntax: '(x, (y, z,  ... , (43, 44))')
  Left: 
    ITupleOperation (OperationKind.Tuple, Type: (System.Int32 x, (System.Int32 y, System.Int32 z, System.Int32 w)), IsInvalid) (Syntax: '(x, (y, z, w))')
      NaturalType: (System.Int32 x, (System.Int32 y, System.Int32 z, System.Int32 w))
      Elements(2):
          ILocalReferenceOperation: x (OperationKind.LocalReference, Type: System.Int32, IsInvalid) (Syntax: 'x')
          ITupleOperation (OperationKind.Tuple, Type: (System.Int32 y, System.Int32 z, System.Int32 w), IsInvalid) (Syntax: '(y, z, w)')
            NaturalType: (System.Int32 y, System.Int32 z, System.Int32 w)
            Elements(3):
                ILocalReferenceOperation: y (OperationKind.LocalReference, Type: System.Int32, IsInvalid) (Syntax: 'y')
                ILocalReferenceOperation: z (OperationKind.LocalReference, Type: System.Int32, IsInvalid) (Syntax: 'z')
                ILocalReferenceOperation: w (OperationKind.LocalReference, Type: System.Int32, IsInvalid) (Syntax: 'w')
  Right: 
    IInvocationOperation (Pair<System.Int32, (System.Int32, System.Int32)> Pair.Create<System.Int32, (System.Int32, System.Int32)>(System.Int32 item1, (System.Int32, System.Int32) item2)) (OperationKind.Invocation, Type: Pair<System.Int32, (System.Int32, System.Int32)>, IsInvalid) (Syntax: 'Pair.Create ... , (43, 44))')
      Instance Receiver: 
        null
      Arguments(2):
          IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: item1) (OperationKind.Argument, Type: null, IsInvalid) (Syntax: '42')
            ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 42, IsInvalid) (Syntax: '42')
            InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
            OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
          IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: item2) (OperationKind.Argument, Type: null, IsInvalid) (Syntax: '(43, 44)')
            ITupleOperation (OperationKind.Tuple, Type: (System.Int32, System.Int32), IsInvalid) (Syntax: '(43, 44)')
              NaturalType: (System.Int32, System.Int32)
              Elements(2):
                  ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 43, IsInvalid) (Syntax: '43')
                  ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 44, IsInvalid) (Syntax: '44')
            InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
            OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS8132: Cannot deconstruct a tuple of '2' elements into '3' variables.
                //         /*<bind>*/(x, (y, z, w)) = Pair.Create(42, (43, 44))/*</bind>*/;
                Diagnostic(ErrorCode.ERR_DeconstructWrongCardinality, "(x, (y, z, w)) = Pair.Create(42, (43, 44))").WithArguments("2", "3").WithLocation(8, 19)
            };

            VerifyOperationTreeAndDiagnosticsForTest<AssignmentExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void DeconstructionTooFewElements()
        {
            string source = @"
class C
{
    static void Main()
    {
        for (/*<bind>*/(var(x, y)) = Pair.Create(1, 2)/*</bind>*/; ;) { }
    }
}
" + commonSource;

            string expectedOperationTree = @"
ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: ?, IsInvalid) (Syntax: '(var(x, y)) ... reate(1, 2)')
  Left: 
    IInvalidOperation (OperationKind.Invalid, Type: ?, IsInvalid) (Syntax: 'var(x, y)')
      Children(3):
          IInvalidOperation (OperationKind.Invalid, Type: ?, IsInvalid) (Syntax: 'var')
            Children(0)
          IInvalidOperation (OperationKind.Invalid, Type: ?, IsInvalid) (Syntax: 'x')
            Children(0)
          IInvalidOperation (OperationKind.Invalid, Type: ?, IsInvalid) (Syntax: 'y')
            Children(0)
  Right: 
    IInvocationOperation (Pair<System.Int32, System.Int32> Pair.Create<System.Int32, System.Int32>(System.Int32 item1, System.Int32 item2)) (OperationKind.Invocation, Type: Pair<System.Int32, System.Int32>) (Syntax: 'Pair.Create(1, 2)')
      Instance Receiver: 
        null
      Arguments(2):
          IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: item1) (OperationKind.Argument, Type: null) (Syntax: '1')
            ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
            InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
            OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
          IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: item2) (OperationKind.Argument, Type: null) (Syntax: '2')
            ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 2) (Syntax: '2')
            InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
            OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS0103: The name 'var' does not exist in the current context
                //         for (/*<bind>*/(var(x, y)) = Pair.Create(1, 2)/*</bind>*/; ;) { }
                Diagnostic(ErrorCode.ERR_NameNotInContext, "var").WithArguments("var").WithLocation(6, 25),
                // CS0103: The name 'x' does not exist in the current context
                //         for (/*<bind>*/(var(x, y)) = Pair.Create(1, 2)/*</bind>*/; ;) { }
                Diagnostic(ErrorCode.ERR_NameNotInContext, "x").WithArguments("x").WithLocation(6, 29),
                // CS0103: The name 'y' does not exist in the current context
                //         for (/*<bind>*/(var(x, y)) = Pair.Create(1, 2)/*</bind>*/; ;) { }
                Diagnostic(ErrorCode.ERR_NameNotInContext, "y").WithArguments("y").WithLocation(6, 32)
            };

            VerifyOperationTreeAndDiagnosticsForTest<AssignmentExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [Fact]
        public void DeconstructionDeclarationInCSharp6()
        {
            string source = @"
class C
{
    static void Main()
    {
        var (x1, x2) = Pair.Create(1, 2);
        (int x3, int x4) = Pair.Create(1, 2);
        foreach ((int x5, var (x6, x7)) in new[] { Pair.Create(1, Pair.Create(2, 3)) }) { }
        for ((int x8, var (x9, x10)) = Pair.Create(1, Pair.Create(2, 3)); ; ) { }
    }
}
" + commonSource;

            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular6);
            comp.VerifyDiagnostics(
                // (6,13): error CS8059: Feature 'tuples' is not available in C# 6. Please use language version 7.0 or greater.
                //         var (x1, x2) = Pair.Create(1, 2);
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion6, "(x1, x2)").WithArguments("tuples", "7.0").WithLocation(6, 13),
                // (7,9): error CS8059: Feature 'tuples' is not available in C# 6. Please use language version 7.0 or greater.
                //         (int x3, int x4) = Pair.Create(1, 2);
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion6, "(int x3, int x4)").WithArguments("tuples", "7.0").WithLocation(7, 9),
                // (8,18): error CS8059: Feature 'tuples' is not available in C# 6. Please use language version 7.0 or greater.
                //         foreach ((int x5, var (x6, x7)) in new[] { Pair.Create(1, Pair.Create(2, 3)) }) { }
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion6, "(int x5, var (x6, x7))").WithArguments("tuples", "7.0").WithLocation(8, 18),
                // (9,14): error CS8059: Feature 'tuples' is not available in C# 6. Please use language version 7.0 or greater.
                //         for ((int x8, var (x9, x10)) = Pair.Create(1, Pair.Create(2, 3)); ; ) { }
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion6, "(int x8, var (x9, x10))").WithArguments("tuples", "7.0").WithLocation(9, 14)
                );
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void DeclareLocalTwice()
        {
            string source = @"
class C
{
    static void Main()
    {
        /*<bind>*/var (x1, x1) = (1, 2)/*</bind>*/;
    }
}
";
            string expectedOperationTree = @"
IDeconstructionAssignmentOperation (OperationKind.DeconstructionAssignment, Type: (System.Int32, System.Int32), IsInvalid) (Syntax: 'var (x1, x1) = (1, 2)')
  Left: 
    IDeclarationExpressionOperation (OperationKind.DeclarationExpression, Type: (System.Int32, System.Int32), IsInvalid) (Syntax: 'var (x1, x1)')
      ITupleOperation (OperationKind.Tuple, Type: (System.Int32, System.Int32), IsInvalid) (Syntax: '(x1, x1)')
        NaturalType: (System.Int32, System.Int32)
        Elements(2):
            ILocalReferenceOperation: x1 (IsDeclaration: True) (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'x1')
            ILocalReferenceOperation: x1 (IsDeclaration: True) (OperationKind.LocalReference, Type: System.Int32, IsInvalid) (Syntax: 'x1')
  Right: 
    ITupleOperation (OperationKind.Tuple, Type: (System.Int32, System.Int32)) (Syntax: '(1, 2)')
      NaturalType: (System.Int32, System.Int32)
      Elements(2):
          ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
          ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 2) (Syntax: '2')
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS0128: A local variable or function named 'x1' is already defined in this scope
                //         /*<bind>*/var (x1, x1) = (1, 2)/*</bind>*/;
                Diagnostic(ErrorCode.ERR_LocalDuplicate, "x1").WithArguments("x1").WithLocation(6, 28)
            };

            VerifyOperationTreeAndDiagnosticsForTest<AssignmentExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void DeclareLocalTwice2()
        {
            string source = @"
class C
{
    static void Main()
    {
        string x1 = null;
        /*<bind>*/var (x1, x2) = (1, 2)/*</bind>*/;
        System.Console.WriteLine(x1);
    }
}
";
            string expectedOperationTree = @"
IDeconstructionAssignmentOperation (OperationKind.DeconstructionAssignment, Type: (System.Int32 x1, System.Int32 x2), IsInvalid) (Syntax: 'var (x1, x2) = (1, 2)')
  Left: 
    IDeclarationExpressionOperation (OperationKind.DeclarationExpression, Type: (System.Int32 x1, System.Int32 x2), IsInvalid) (Syntax: 'var (x1, x2)')
      ITupleOperation (OperationKind.Tuple, Type: (System.Int32 x1, System.Int32 x2), IsInvalid) (Syntax: '(x1, x2)')
        NaturalType: (System.Int32 x1, System.Int32 x2)
        Elements(2):
            ILocalReferenceOperation: x1 (IsDeclaration: True) (OperationKind.LocalReference, Type: System.Int32, IsInvalid) (Syntax: 'x1')
            ILocalReferenceOperation: x2 (IsDeclaration: True) (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'x2')
  Right: 
    ITupleOperation (OperationKind.Tuple, Type: (System.Int32, System.Int32)) (Syntax: '(1, 2)')
      NaturalType: (System.Int32, System.Int32)
      Elements(2):
          ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
          ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 2) (Syntax: '2')
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS0128: A local variable or function named 'x1' is already defined in this scope
                //         /*<bind>*/var (x1, x2) = (1, 2)/*</bind>*/;
                Diagnostic(ErrorCode.ERR_LocalDuplicate, "x1").WithArguments("x1").WithLocation(7, 24)
            };

            VerifyOperationTreeAndDiagnosticsForTest<AssignmentExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void VarMethodMissing()
        {
            string source = @"
class C
{
    static void Main()
    {
        int x1 = 1;
        int x2 = 1;
        /*<bind>*/var(x1, x2)/*</bind>*/;
    }
}
";
            string expectedOperationTree = @"
IInvalidOperation (OperationKind.Invalid, Type: ?, IsInvalid) (Syntax: 'var(x1, x2)')
  Children(3):
      IInvalidOperation (OperationKind.Invalid, Type: ?, IsInvalid) (Syntax: 'var')
        Children(0)
      ILocalReferenceOperation: x1 (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'x1')
      ILocalReferenceOperation: x2 (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'x2')
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS0103: The name 'var' does not exist in the current context
                //         /*<bind>*/var(x1, x2)/*</bind>*/;
                Diagnostic(ErrorCode.ERR_NameNotInContext, "var").WithArguments("var").WithLocation(8, 19)
            };

            VerifyOperationTreeAndDiagnosticsForTest<InvocationExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void UseBeforeDeclared()
        {
            string source = @"
class C
{
    static void Main()
    {
        /*<bind>*/(int x1, int x2) = M(x1)/*</bind>*/;
    }
    static (int, int) M(int a) { return (1, 2); }
}
";
            string expectedOperationTree = @"
IDeconstructionAssignmentOperation (OperationKind.DeconstructionAssignment, Type: (System.Int32 x1, System.Int32 x2), IsInvalid) (Syntax: '(int x1, int x2) = M(x1)')
  Left: 
    ITupleOperation (OperationKind.Tuple, Type: (System.Int32 x1, System.Int32 x2)) (Syntax: '(int x1, int x2)')
      NaturalType: (System.Int32 x1, System.Int32 x2)
      Elements(2):
          IDeclarationExpressionOperation (OperationKind.DeclarationExpression, Type: System.Int32) (Syntax: 'int x1')
            ILocalReferenceOperation: x1 (IsDeclaration: True) (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'x1')
          IDeclarationExpressionOperation (OperationKind.DeclarationExpression, Type: System.Int32) (Syntax: 'int x2')
            ILocalReferenceOperation: x2 (IsDeclaration: True) (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'x2')
  Right: 
    IInvocationOperation ((System.Int32, System.Int32) C.M(System.Int32 a)) (OperationKind.Invocation, Type: (System.Int32, System.Int32), IsInvalid) (Syntax: 'M(x1)')
      Instance Receiver: 
        null
      Arguments(1):
          IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: a) (OperationKind.Argument, Type: null, IsInvalid) (Syntax: 'x1')
            ILocalReferenceOperation: x1 (OperationKind.LocalReference, Type: System.Int32, IsInvalid) (Syntax: 'x1')
            InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
            OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS0165: Use of unassigned local variable 'x1'
                //         /*<bind>*/(int x1, int x2) = M(x1)/*</bind>*/;
                Diagnostic(ErrorCode.ERR_UseDefViolation, "x1").WithArguments("x1").WithLocation(6, 40)
            };

            VerifyOperationTreeAndDiagnosticsForTest<AssignmentExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void DeclareWithVoidType()
        {
            string source = @"
class C
{
    static void Main()
    {
        /*<bind>*/(int x1, int x2) = M(x1)/*</bind>*/;
    }
    static void M(int a) { }
}
";
            string expectedOperationTree = @"
IDeconstructionAssignmentOperation (OperationKind.DeconstructionAssignment, Type: ?, IsInvalid) (Syntax: '(int x1, int x2) = M(x1)')
  Left: 
    ITupleOperation (OperationKind.Tuple, Type: (System.Int32 x1, System.Int32 x2)) (Syntax: '(int x1, int x2)')
      NaturalType: (System.Int32 x1, System.Int32 x2)
      Elements(2):
          IDeclarationExpressionOperation (OperationKind.DeclarationExpression, Type: System.Int32) (Syntax: 'int x1')
            ILocalReferenceOperation: x1 (IsDeclaration: True) (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'x1')
          IDeclarationExpressionOperation (OperationKind.DeclarationExpression, Type: System.Int32) (Syntax: 'int x2')
            ILocalReferenceOperation: x2 (IsDeclaration: True) (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'x2')
  Right: 
    IInvocationOperation (void C.M(System.Int32 a)) (OperationKind.Invocation, Type: System.Void, IsInvalid) (Syntax: 'M(x1)')
      Instance Receiver: 
        null
      Arguments(1):
          IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: a) (OperationKind.Argument, Type: null, IsInvalid) (Syntax: 'x1')
            ILocalReferenceOperation: x1 (OperationKind.LocalReference, Type: System.Int32, IsInvalid) (Syntax: 'x1')
            InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
            OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS1061: 'void' does not contain a definition for 'Deconstruct' and no extension method 'Deconstruct' accepting a first argument of type 'void' could be found (are you missing a using directive or an assembly reference?)
                //         /*<bind>*/(int x1, int x2) = M(x1)/*</bind>*/;
                Diagnostic(ErrorCode.ERR_NoSuchMemberOrExtension, "M(x1)").WithArguments("void", "Deconstruct").WithLocation(6, 38),
                // CS0165: Use of unassigned local variable 'x1'
                //         /*<bind>*/(int x1, int x2) = M(x1)/*</bind>*/;
                Diagnostic(ErrorCode.ERR_UseDefViolation, "x1").WithArguments("x1").WithLocation(6, 40)
            };

            VerifyOperationTreeAndDiagnosticsForTest<AssignmentExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void UseBeforeDeclared2()
        {
            string source = @"
class C
{
    static void Main()
    {
        System.Console.WriteLine(x1);
        /*<bind>*/(int x1, int x2) = (1, 2)/*</bind>*/;
    }
}
";
            string expectedOperationTree = @"
IDeconstructionAssignmentOperation (OperationKind.DeconstructionAssignment, Type: (System.Int32 x1, System.Int32 x2)) (Syntax: '(int x1, in ... 2) = (1, 2)')
  Left: 
    ITupleOperation (OperationKind.Tuple, Type: (System.Int32 x1, System.Int32 x2)) (Syntax: '(int x1, int x2)')
      NaturalType: (System.Int32 x1, System.Int32 x2)
      Elements(2):
          IDeclarationExpressionOperation (OperationKind.DeclarationExpression, Type: System.Int32) (Syntax: 'int x1')
            ILocalReferenceOperation: x1 (IsDeclaration: True) (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'x1')
          IDeclarationExpressionOperation (OperationKind.DeclarationExpression, Type: System.Int32) (Syntax: 'int x2')
            ILocalReferenceOperation: x2 (IsDeclaration: True) (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'x2')
  Right: 
    ITupleOperation (OperationKind.Tuple, Type: (System.Int32, System.Int32)) (Syntax: '(1, 2)')
      NaturalType: (System.Int32, System.Int32)
      Elements(2):
          ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
          ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 2) (Syntax: '2')
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS0841: Cannot use local variable 'x1' before it is declared
                //         System.Console.WriteLine(x1);
                Diagnostic(ErrorCode.ERR_VariableUsedBeforeDeclaration, "x1").WithArguments("x1").WithLocation(6, 34)
            };

            VerifyOperationTreeAndDiagnosticsForTest<AssignmentExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void NullAssignmentInDeclaration()
        {
            string source = @"
class C
{
    static void Main()
    {
        /*<bind>*/(int x1, int x2) = null/*</bind>*/;
    }
}
";
            string expectedOperationTree = @"
IDeconstructionAssignmentOperation (OperationKind.DeconstructionAssignment, Type: System.Void, IsInvalid) (Syntax: '(int x1, int x2) = null')
  Left: 
    ITupleOperation (OperationKind.Tuple, Type: (System.Int32 x1, System.Int32 x2)) (Syntax: '(int x1, int x2)')
      NaturalType: (System.Int32 x1, System.Int32 x2)
      Elements(2):
          IDeclarationExpressionOperation (OperationKind.DeclarationExpression, Type: System.Int32) (Syntax: 'int x1')
            ILocalReferenceOperation: x1 (IsDeclaration: True) (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'x1')
          IDeclarationExpressionOperation (OperationKind.DeclarationExpression, Type: System.Int32) (Syntax: 'int x2')
            ILocalReferenceOperation: x2 (IsDeclaration: True) (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'x2')
  Right: 
    ILiteralOperation (OperationKind.Literal, Type: null, Constant: null, IsInvalid) (Syntax: 'null')
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS8131: Deconstruct assignment requires an expression with a type on the right-hand-side.
                //         /*<bind>*/(int x1, int x2) = null/*</bind>*/;
                Diagnostic(ErrorCode.ERR_DeconstructRequiresExpression, "null").WithLocation(6, 38)
            };

            VerifyOperationTreeAndDiagnosticsForTest<AssignmentExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void NullAssignmentInVarDeclaration()
        {
            string source = @"
class C
{
    static void Main()
    {
        /*<bind>*/var (x1, x2) = null/*</bind>*/;
    }
}
";
            string expectedOperationTree = @"
IDeconstructionAssignmentOperation (OperationKind.DeconstructionAssignment, Type: System.Void, IsInvalid) (Syntax: 'var (x1, x2) = null')
  Left: 
    IDeclarationExpressionOperation (OperationKind.DeclarationExpression, Type: (var x1, var x2), IsInvalid) (Syntax: 'var (x1, x2)')
      ITupleOperation (OperationKind.Tuple, Type: (var x1, var x2), IsInvalid) (Syntax: '(x1, x2)')
        NaturalType: (var x1, var x2)
        Elements(2):
            ILocalReferenceOperation: x1 (IsDeclaration: True) (OperationKind.LocalReference, Type: var, IsInvalid) (Syntax: 'x1')
            ILocalReferenceOperation: x2 (IsDeclaration: True) (OperationKind.LocalReference, Type: var, IsInvalid) (Syntax: 'x2')
  Right: 
    ILiteralOperation (OperationKind.Literal, Type: null, Constant: null, IsInvalid) (Syntax: 'null')
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS8131: Deconstruct assignment requires an expression with a type on the right-hand-side.
                //         /*<bind>*/var (x1, x2) = null/*</bind>*/;
                Diagnostic(ErrorCode.ERR_DeconstructRequiresExpression, "null").WithLocation(6, 34),
                // CS8130: Cannot infer the type of implicitly-typed deconstruction variable 'x1'.
                //         /*<bind>*/var (x1, x2) = null/*</bind>*/;
                Diagnostic(ErrorCode.ERR_TypeInferenceFailedForImplicitlyTypedDeconstructionVariable, "x1").WithArguments("x1").WithLocation(6, 24),
                // CS8130: Cannot infer the type of implicitly-typed deconstruction variable 'x2'.
                //         /*<bind>*/var (x1, x2) = null/*</bind>*/;
                Diagnostic(ErrorCode.ERR_TypeInferenceFailedForImplicitlyTypedDeconstructionVariable, "x2").WithArguments("x2").WithLocation(6, 28)
            };

            VerifyOperationTreeAndDiagnosticsForTest<AssignmentExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void TypelessDeclaration()
        {
            string source = @"
class C
{
    static void Main()
    {
        /*<bind>*/var (x1, x2) = (1, null)/*</bind>*/;
    }
}
";
            string expectedOperationTree = @"
IDeconstructionAssignmentOperation (OperationKind.DeconstructionAssignment, Type: System.Void, IsInvalid) (Syntax: 'var (x1, x2) = (1, null)')
  Left: 
    IDeclarationExpressionOperation (OperationKind.DeclarationExpression, Type: (var x1, var x2), IsInvalid) (Syntax: 'var (x1, x2)')
      ITupleOperation (OperationKind.Tuple, Type: (var x1, var x2), IsInvalid) (Syntax: '(x1, x2)')
        NaturalType: (var x1, var x2)
        Elements(2):
            ILocalReferenceOperation: x1 (IsDeclaration: True) (OperationKind.LocalReference, Type: var, IsInvalid) (Syntax: 'x1')
            ILocalReferenceOperation: x2 (IsDeclaration: True) (OperationKind.LocalReference, Type: var, IsInvalid) (Syntax: 'x2')
  Right: 
    ITupleOperation (OperationKind.Tuple, Type: null) (Syntax: '(1, null)')
      NaturalType: null
      Elements(2):
          ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
          ILiteralOperation (OperationKind.Literal, Type: null, Constant: null) (Syntax: 'null')
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS8130: Cannot infer the type of implicitly-typed deconstruction variable 'x1'.
                //         /*<bind>*/var (x1, x2) = (1, null)/*</bind>*/;
                Diagnostic(ErrorCode.ERR_TypeInferenceFailedForImplicitlyTypedDeconstructionVariable, "x1").WithArguments("x1").WithLocation(6, 24),
                // CS8130: Cannot infer the type of implicitly-typed deconstruction variable 'x2'.
                //         /*<bind>*/var (x1, x2) = (1, null)/*</bind>*/;
                Diagnostic(ErrorCode.ERR_TypeInferenceFailedForImplicitlyTypedDeconstructionVariable, "x2").WithArguments("x2").WithLocation(6, 28)
            };

            VerifyOperationTreeAndDiagnosticsForTest<AssignmentExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void TypeMergingWithMultipleAmbiguousVars()
        {
            string source = @"
class C
{
    static void Main()
    {
        /*<bind>*/(string x1, (byte x2, var x3), var x4) = (null, (2, null), null)/*</bind>*/;
    }
}
";
            string expectedOperationTree = @"
IDeconstructionAssignmentOperation (OperationKind.DeconstructionAssignment, Type: System.Void, IsInvalid) (Syntax: '(string x1, ... ull), null)')
  Left: 
    ITupleOperation (OperationKind.Tuple, Type: (System.String x1, (System.Byte x2, var x3), var x4), IsInvalid) (Syntax: '(string x1, ... 3), var x4)')
      NaturalType: (System.String x1, (System.Byte x2, var x3), var x4)
      Elements(3):
          IDeclarationExpressionOperation (OperationKind.DeclarationExpression, Type: System.String) (Syntax: 'string x1')
            ILocalReferenceOperation: x1 (IsDeclaration: True) (OperationKind.LocalReference, Type: System.String) (Syntax: 'x1')
          ITupleOperation (OperationKind.Tuple, Type: (System.Byte x2, var x3), IsInvalid) (Syntax: '(byte x2, var x3)')
            NaturalType: (System.Byte x2, var x3)
            Elements(2):
                IDeclarationExpressionOperation (OperationKind.DeclarationExpression, Type: System.Byte) (Syntax: 'byte x2')
                  ILocalReferenceOperation: x2 (IsDeclaration: True) (OperationKind.LocalReference, Type: System.Byte) (Syntax: 'x2')
                IDeclarationExpressionOperation (OperationKind.DeclarationExpression, Type: var, IsInvalid) (Syntax: 'var x3')
                  ILocalReferenceOperation: x3 (IsDeclaration: True) (OperationKind.LocalReference, Type: var, IsInvalid) (Syntax: 'x3')
          IDeclarationExpressionOperation (OperationKind.DeclarationExpression, Type: var, IsInvalid) (Syntax: 'var x4')
            ILocalReferenceOperation: x4 (IsDeclaration: True) (OperationKind.LocalReference, Type: var, IsInvalid) (Syntax: 'x4')
  Right: 
    ITupleOperation (OperationKind.Tuple, Type: null) (Syntax: '(null, (2, null), null)')
      NaturalType: null
      Elements(3):
          ILiteralOperation (OperationKind.Literal, Type: null, Constant: null) (Syntax: 'null')
          ITupleOperation (OperationKind.Tuple, Type: null) (Syntax: '(2, null)')
            NaturalType: null
            Elements(2):
                ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 2) (Syntax: '2')
                ILiteralOperation (OperationKind.Literal, Type: null, Constant: null) (Syntax: 'null')
          ILiteralOperation (OperationKind.Literal, Type: null, Constant: null) (Syntax: 'null')
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS8130: Cannot infer the type of implicitly-typed deconstruction variable 'x3'.
                //         /*<bind>*/(string x1, (byte x2, var x3), var x4) = (null, (2, null), null)/*</bind>*/;
                Diagnostic(ErrorCode.ERR_TypeInferenceFailedForImplicitlyTypedDeconstructionVariable, "x3").WithArguments("x3").WithLocation(6, 45),
                // CS8130: Cannot infer the type of implicitly-typed deconstruction variable 'x4'.
                //         /*<bind>*/(string x1, (byte x2, var x3), var x4) = (null, (2, null), null)/*</bind>*/;
                Diagnostic(ErrorCode.ERR_TypeInferenceFailedForImplicitlyTypedDeconstructionVariable, "x4").WithArguments("x4").WithLocation(6, 54)
            };

            VerifyOperationTreeAndDiagnosticsForTest<AssignmentExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void TypeMergingWithTooManyLeftNestings()
        {
            string source = @"
class C
{
    static void Main()
    {
        /*<bind>*/((string x1, byte x2, var x3), int x4) = (null, 4)/*</bind>*/;
    }
}
";
            string expectedOperationTree = @"
IDeconstructionAssignmentOperation (OperationKind.DeconstructionAssignment, Type: System.Void, IsInvalid) (Syntax: '((string x1 ... = (null, 4)')
  Left: 
    ITupleOperation (OperationKind.Tuple, Type: ((System.String x1, System.Byte x2, var x3), System.Int32 x4), IsInvalid) (Syntax: '((string x1 ... 3), int x4)')
      NaturalType: ((System.String x1, System.Byte x2, var x3), System.Int32 x4)
      Elements(2):
          ITupleOperation (OperationKind.Tuple, Type: (System.String x1, System.Byte x2, var x3), IsInvalid) (Syntax: '(string x1, ... x2, var x3)')
            NaturalType: (System.String x1, System.Byte x2, var x3)
            Elements(3):
                IDeclarationExpressionOperation (OperationKind.DeclarationExpression, Type: System.String) (Syntax: 'string x1')
                  ILocalReferenceOperation: x1 (IsDeclaration: True) (OperationKind.LocalReference, Type: System.String) (Syntax: 'x1')
                IDeclarationExpressionOperation (OperationKind.DeclarationExpression, Type: System.Byte) (Syntax: 'byte x2')
                  ILocalReferenceOperation: x2 (IsDeclaration: True) (OperationKind.LocalReference, Type: System.Byte) (Syntax: 'x2')
                IDeclarationExpressionOperation (OperationKind.DeclarationExpression, Type: var, IsInvalid) (Syntax: 'var x3')
                  ILocalReferenceOperation: x3 (IsDeclaration: True) (OperationKind.LocalReference, Type: var, IsInvalid) (Syntax: 'x3')
          IDeclarationExpressionOperation (OperationKind.DeclarationExpression, Type: System.Int32) (Syntax: 'int x4')
            ILocalReferenceOperation: x4 (IsDeclaration: True) (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'x4')
  Right: 
    ITupleOperation (OperationKind.Tuple, Type: null, IsInvalid) (Syntax: '(null, 4)')
      NaturalType: null
      Elements(2):
          ILiteralOperation (OperationKind.Literal, Type: null, Constant: null, IsInvalid) (Syntax: 'null')
          ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 4) (Syntax: '4')
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS8131: Deconstruct assignment requires an expression with a type on the right-hand-side.
                //         /*<bind>*/((string x1, byte x2, var x3), int x4) = (null, 4)/*</bind>*/;
                Diagnostic(ErrorCode.ERR_DeconstructRequiresExpression, "null").WithLocation(6, 61),
                // CS8130: Cannot infer the type of implicitly-typed deconstruction variable 'x3'.
                //         /*<bind>*/((string x1, byte x2, var x3), int x4) = (null, 4)/*</bind>*/;
                Diagnostic(ErrorCode.ERR_TypeInferenceFailedForImplicitlyTypedDeconstructionVariable, "x3").WithArguments("x3").WithLocation(6, 45)
            };

            VerifyOperationTreeAndDiagnosticsForTest<AssignmentExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void TypeMergingWithTooManyRightNestings()
        {
            string source = @"
class C
{
    static void Main()
    {
        /*<bind>*/(string x1, var x2) = (null, (null, 2))/*</bind>*/;
    }
}
";
            string expectedOperationTree = @"
IDeconstructionAssignmentOperation (OperationKind.DeconstructionAssignment, Type: System.Void, IsInvalid) (Syntax: '(string x1, ...  (null, 2))')
  Left: 
    ITupleOperation (OperationKind.Tuple, Type: (System.String x1, var x2), IsInvalid) (Syntax: '(string x1, var x2)')
      NaturalType: (System.String x1, var x2)
      Elements(2):
          IDeclarationExpressionOperation (OperationKind.DeclarationExpression, Type: System.String) (Syntax: 'string x1')
            ILocalReferenceOperation: x1 (IsDeclaration: True) (OperationKind.LocalReference, Type: System.String) (Syntax: 'x1')
          IDeclarationExpressionOperation (OperationKind.DeclarationExpression, Type: var, IsInvalid) (Syntax: 'var x2')
            ILocalReferenceOperation: x2 (IsDeclaration: True) (OperationKind.LocalReference, Type: var, IsInvalid) (Syntax: 'x2')
  Right: 
    ITupleOperation (OperationKind.Tuple, Type: null) (Syntax: '(null, (null, 2))')
      NaturalType: null
      Elements(2):
          ILiteralOperation (OperationKind.Literal, Type: null, Constant: null) (Syntax: 'null')
          ITupleOperation (OperationKind.Tuple, Type: null) (Syntax: '(null, 2)')
            NaturalType: null
            Elements(2):
                ILiteralOperation (OperationKind.Literal, Type: null, Constant: null) (Syntax: 'null')
                ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 2) (Syntax: '2')
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS8130: Cannot infer the type of implicitly-typed deconstruction variable 'x2'.
                //         /*<bind>*/(string x1, var x2) = (null, (null, 2))/*</bind>*/;
                Diagnostic(ErrorCode.ERR_TypeInferenceFailedForImplicitlyTypedDeconstructionVariable, "x2").WithArguments("x2").WithLocation(6, 35)
            };

            VerifyOperationTreeAndDiagnosticsForTest<AssignmentExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void TypeMergingWithTooManyLeftVariables()
        {
            string source = @"
class C
{
    static void Main()
    {
        /*<bind>*/(string x1, var x2, int x3) = (null, ""hello"")/*</bind>*/;
    }
}
";
            string expectedOperationTree = @"
IDeconstructionAssignmentOperation (OperationKind.DeconstructionAssignment, Type: ?, IsInvalid) (Syntax: '(string x1, ... l, ""hello"")')
  Left: 
    ITupleOperation (OperationKind.Tuple, Type: (System.String x1, System.String x2, System.Int32 x3), IsInvalid) (Syntax: '(string x1, ... x2, int x3)')
      NaturalType: (System.String x1, System.String x2, System.Int32 x3)
      Elements(3):
          IDeclarationExpressionOperation (OperationKind.DeclarationExpression, Type: System.String, IsInvalid) (Syntax: 'string x1')
            ILocalReferenceOperation: x1 (IsDeclaration: True) (OperationKind.LocalReference, Type: System.String, IsInvalid) (Syntax: 'x1')
          IDeclarationExpressionOperation (OperationKind.DeclarationExpression, Type: System.String, IsInvalid) (Syntax: 'var x2')
            ILocalReferenceOperation: x2 (IsDeclaration: True) (OperationKind.LocalReference, Type: System.String, IsInvalid) (Syntax: 'x2')
          IDeclarationExpressionOperation (OperationKind.DeclarationExpression, Type: System.Int32, IsInvalid) (Syntax: 'int x3')
            ILocalReferenceOperation: x3 (IsDeclaration: True) (OperationKind.LocalReference, Type: System.Int32, IsInvalid) (Syntax: 'x3')
  Right: 
    ITupleOperation (OperationKind.Tuple, Type: (System.String, System.String), IsInvalid) (Syntax: '(null, ""hello"")')
      NaturalType: null
      Elements(2):
          IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.String, Constant: null, IsInvalid, IsImplicit) (Syntax: 'null')
            Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
            Operand: 
              ILiteralOperation (OperationKind.Literal, Type: null, Constant: null, IsInvalid) (Syntax: 'null')
          ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: ""hello"", IsInvalid) (Syntax: '""hello""')
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS8132: Cannot deconstruct a tuple of '2' elements into '3' variables.
                //         /*<bind>*/(string x1, var x2, int x3) = (null, "hello")/*</bind>*/;
                Diagnostic(ErrorCode.ERR_DeconstructWrongCardinality, @"(string x1, var x2, int x3) = (null, ""hello"")").WithArguments("2", "3").WithLocation(6, 19)
            };

            VerifyOperationTreeAndDiagnosticsForTest<AssignmentExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void TypeMergingWithTooManyRightElements()
        {
            string source = @"
class C
{
    static void Main()
    {
        /*<bind>*/(string x1, var y1) = (null, ""hello"", 3)/*</bind>*/;
        (string x2, var y2) = (null, ""hello"", null);
    }
}
";
            string expectedOperationTree = @"
IDeconstructionAssignmentOperation (OperationKind.DeconstructionAssignment, Type: ?, IsInvalid) (Syntax: '(string x1, ... ""hello"", 3)')
  Left: 
    ITupleOperation (OperationKind.Tuple, Type: (System.String x1, System.String y1), IsInvalid) (Syntax: '(string x1, var y1)')
      NaturalType: (System.String x1, System.String y1)
      Elements(2):
          IDeclarationExpressionOperation (OperationKind.DeclarationExpression, Type: System.String, IsInvalid) (Syntax: 'string x1')
            ILocalReferenceOperation: x1 (IsDeclaration: True) (OperationKind.LocalReference, Type: System.String, IsInvalid) (Syntax: 'x1')
          IDeclarationExpressionOperation (OperationKind.DeclarationExpression, Type: System.String, IsInvalid) (Syntax: 'var y1')
            ILocalReferenceOperation: y1 (IsDeclaration: True) (OperationKind.LocalReference, Type: System.String, IsInvalid) (Syntax: 'y1')
  Right: 
    ITupleOperation (OperationKind.Tuple, Type: (System.String, System.String, System.Int32), IsInvalid) (Syntax: '(null, ""hello"", 3)')
      NaturalType: null
      Elements(3):
          IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.String, Constant: null, IsInvalid, IsImplicit) (Syntax: 'null')
            Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
            Operand: 
              ILiteralOperation (OperationKind.Literal, Type: null, Constant: null, IsInvalid) (Syntax: 'null')
          ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: ""hello"", IsInvalid) (Syntax: '""hello""')
          ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 3, IsInvalid) (Syntax: '3')
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS8132: Cannot deconstruct a tuple of '3' elements into '2' variables.
                //         /*<bind>*/(string x1, var y1) = (null, "hello", 3)/*</bind>*/;
                Diagnostic(ErrorCode.ERR_DeconstructWrongCardinality, @"(string x1, var y1) = (null, ""hello"", 3)").WithArguments("3", "2").WithLocation(6, 19),
                // CS8131: Deconstruct assignment requires an expression with a type on the right-hand-side.
                //         (string x2, var y2) = (null, "hello", null);
                Diagnostic(ErrorCode.ERR_DeconstructRequiresExpression, "null").WithLocation(7, 47),
                // CS8130: Cannot infer the type of implicitly-typed deconstruction variable 'y2'.
                //         (string x2, var y2) = (null, "hello", null);
                Diagnostic(ErrorCode.ERR_TypeInferenceFailedForImplicitlyTypedDeconstructionVariable, "y2").WithArguments("y2").WithLocation(7, 25)
            };

            VerifyOperationTreeAndDiagnosticsForTest<AssignmentExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void DeclarationVarFormWithActualVarType()
        {
            string source = @"
class C
{
    static void Main()
    {
        /*<bind>*/var (x1, x2) = (1, 2)/*</bind>*/;
    }
}
class @var { }
";
            string expectedOperationTree = @"
IDeconstructionAssignmentOperation (OperationKind.DeconstructionAssignment, Type: (var x1, var x2), IsInvalid) (Syntax: 'var (x1, x2) = (1, 2)')
  Left: 
    IDeclarationExpressionOperation (OperationKind.DeclarationExpression, Type: (var x1, var x2), IsInvalid) (Syntax: 'var (x1, x2)')
      ITupleOperation (OperationKind.Tuple, Type: (var x1, var x2), IsInvalid) (Syntax: '(x1, x2)')
        NaturalType: (var x1, var x2)
        Elements(2):
            ILocalReferenceOperation: x1 (IsDeclaration: True) (OperationKind.LocalReference, Type: var, IsInvalid) (Syntax: 'x1')
            ILocalReferenceOperation: x2 (IsDeclaration: True) (OperationKind.LocalReference, Type: var, IsInvalid) (Syntax: 'x2')
  Right: 
    IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: (var, var), IsInvalid, IsImplicit) (Syntax: '(1, 2)')
      Conversion: CommonConversion (Exists: False, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
      Operand: 
        ITupleOperation (OperationKind.Tuple, Type: (System.Int32, System.Int32), IsInvalid) (Syntax: '(1, 2)')
          NaturalType: (System.Int32, System.Int32)
          Elements(2):
              ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1, IsInvalid) (Syntax: '1')
              ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 2, IsInvalid) (Syntax: '2')
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS8136: Deconstruction 'var (...)' form disallows a specific type for 'var'.
                //         /*<bind>*/var (x1, x2) = (1, 2)/*</bind>*/;
                Diagnostic(ErrorCode.ERR_DeconstructionVarFormDisallowsSpecificType, "(x1, x2)").WithLocation(6, 23),
                // CS0029: Cannot implicitly convert type 'int' to 'var'
                //         /*<bind>*/var (x1, x2) = (1, 2)/*</bind>*/;
                Diagnostic(ErrorCode.ERR_NoImplicitConv, "1").WithArguments("int", "var").WithLocation(6, 35),
                // CS0029: Cannot implicitly convert type 'int' to 'var'
                //         /*<bind>*/var (x1, x2) = (1, 2)/*</bind>*/;
                Diagnostic(ErrorCode.ERR_NoImplicitConv, "2").WithArguments("int", "var").WithLocation(6, 38)
            };

            VerifyOperationTreeAndDiagnosticsForTest<AssignmentExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void DeclarationVarFormWithAliasedVarType()
        {
            string source = @"
using @var = D;
class C
{
    static void Main()
    {
        /*<bind>*/var (x3, x4) = (3, 4)/*</bind>*/;
    }
}
class D
{
    public override string ToString() { return ""var""; }
}
";
            string expectedOperationTree = @"
IDeconstructionAssignmentOperation (OperationKind.DeconstructionAssignment, Type: (D x3, D x4), IsInvalid) (Syntax: 'var (x3, x4) = (3, 4)')
  Left: 
    IDeclarationExpressionOperation (OperationKind.DeclarationExpression, Type: (D x3, D x4), IsInvalid) (Syntax: 'var (x3, x4)')
      ITupleOperation (OperationKind.Tuple, Type: (D x3, D x4), IsInvalid) (Syntax: '(x3, x4)')
        NaturalType: (D x3, D x4)
        Elements(2):
            ILocalReferenceOperation: x3 (IsDeclaration: True) (OperationKind.LocalReference, Type: D, IsInvalid) (Syntax: 'x3')
            ILocalReferenceOperation: x4 (IsDeclaration: True) (OperationKind.LocalReference, Type: D, IsInvalid) (Syntax: 'x4')
  Right: 
    IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: (D, D), IsInvalid, IsImplicit) (Syntax: '(3, 4)')
      Conversion: CommonConversion (Exists: False, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
      Operand: 
        ITupleOperation (OperationKind.Tuple, Type: (System.Int32, System.Int32), IsInvalid) (Syntax: '(3, 4)')
          NaturalType: (System.Int32, System.Int32)
          Elements(2):
              ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 3, IsInvalid) (Syntax: '3')
              ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 4, IsInvalid) (Syntax: '4')
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS8136: Deconstruction 'var (...)' form disallows a specific type for 'var'.
                //         /*<bind>*/var (x3, x4) = (3, 4)/*</bind>*/;
                Diagnostic(ErrorCode.ERR_DeconstructionVarFormDisallowsSpecificType, "(x3, x4)").WithLocation(7, 23),
                // CS0029: Cannot implicitly convert type 'int' to 'D'
                //         /*<bind>*/var (x3, x4) = (3, 4)/*</bind>*/;
                Diagnostic(ErrorCode.ERR_NoImplicitConv, "3").WithArguments("int", "D").WithLocation(7, 35),
                // CS0029: Cannot implicitly convert type 'int' to 'D'
                //         /*<bind>*/var (x3, x4) = (3, 4)/*</bind>*/;
                Diagnostic(ErrorCode.ERR_NoImplicitConv, "4").WithArguments("int", "D").WithLocation(7, 38)
            };

            VerifyOperationTreeAndDiagnosticsForTest<AssignmentExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void DeclarationWithWrongCardinality()
        {
            string source = @"
class C
{
    static void Main()
    {
        /*<bind>*/(var (x1, x2), var x3) = (1, 2, 3)/*</bind>*/;
    }
}
";
            string expectedOperationTree = @"
IDeconstructionAssignmentOperation (OperationKind.DeconstructionAssignment, Type: ?, IsInvalid) (Syntax: '(var (x1, x ... = (1, 2, 3)')
  Left: 
    ITupleOperation (OperationKind.Tuple, Type: ((var x1, var x2), System.Int32 x3), IsInvalid) (Syntax: '(var (x1, x2), var x3)')
      NaturalType: ((var x1, var x2), System.Int32 x3)
      Elements(2):
          IDeclarationExpressionOperation (OperationKind.DeclarationExpression, Type: (var x1, var x2), IsInvalid) (Syntax: 'var (x1, x2)')
            ITupleOperation (OperationKind.Tuple, Type: (var x1, var x2), IsInvalid) (Syntax: '(x1, x2)')
              NaturalType: (var x1, var x2)
              Elements(2):
                  ILocalReferenceOperation: x1 (IsDeclaration: True) (OperationKind.LocalReference, Type: var, IsInvalid) (Syntax: 'x1')
                  ILocalReferenceOperation: x2 (IsDeclaration: True) (OperationKind.LocalReference, Type: var, IsInvalid) (Syntax: 'x2')
          IDeclarationExpressionOperation (OperationKind.DeclarationExpression, Type: System.Int32, IsInvalid) (Syntax: 'var x3')
            ILocalReferenceOperation: x3 (IsDeclaration: True) (OperationKind.LocalReference, Type: System.Int32, IsInvalid) (Syntax: 'x3')
  Right: 
    ITupleOperation (OperationKind.Tuple, Type: (System.Int32, System.Int32, System.Int32), IsInvalid) (Syntax: '(1, 2, 3)')
      NaturalType: (System.Int32, System.Int32, System.Int32)
      Elements(3):
          ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1, IsInvalid) (Syntax: '1')
          ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 2, IsInvalid) (Syntax: '2')
          ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 3, IsInvalid) (Syntax: '3')
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS8132: Cannot deconstruct a tuple of '3' elements into '2' variables.
                //         /*<bind>*/(var (x1, x2), var x3) = (1, 2, 3)/*</bind>*/;
                Diagnostic(ErrorCode.ERR_DeconstructWrongCardinality, "(var (x1, x2), var x3) = (1, 2, 3)").WithArguments("3", "2").WithLocation(6, 19)
            };

            VerifyOperationTreeAndDiagnosticsForTest<AssignmentExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void DeclarationWithCircularity1()
        {
            string source = @"
class C
{
    static void Main()
    {
        /*<bind>*/var (x1, x2) = (1, x1)/*</bind>*/;
    }
}
";
            string expectedOperationTree = @"
IDeconstructionAssignmentOperation (OperationKind.DeconstructionAssignment, Type: (System.Int32 x1, var x2), IsInvalid) (Syntax: 'var (x1, x2) = (1, x1)')
  Left: 
    IDeclarationExpressionOperation (OperationKind.DeclarationExpression, Type: (System.Int32 x1, var x2)) (Syntax: 'var (x1, x2)')
      ITupleOperation (OperationKind.Tuple, Type: (System.Int32 x1, var x2)) (Syntax: '(x1, x2)')
        NaturalType: (System.Int32 x1, var x2)
        Elements(2):
            ILocalReferenceOperation: x1 (IsDeclaration: True) (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'x1')
            ILocalReferenceOperation: x2 (IsDeclaration: True) (OperationKind.LocalReference, Type: var) (Syntax: 'x2')
  Right: 
    IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: (System.Int32, var), IsInvalid, IsImplicit) (Syntax: '(1, x1)')
      Conversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
      Operand: 
        ITupleOperation (OperationKind.Tuple, Type: (System.Int32, var x1), IsInvalid) (Syntax: '(1, x1)')
          NaturalType: (System.Int32, var x1)
          Elements(2):
              ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
              ILocalReferenceOperation: x1 (OperationKind.LocalReference, Type: var, IsInvalid) (Syntax: 'x1')
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS0841: Cannot use local variable 'x1' before it is declared
                //         /*<bind>*/var (x1, x2) = (1, x1)/*</bind>*/;
                Diagnostic(ErrorCode.ERR_VariableUsedBeforeDeclaration, "x1").WithArguments("x1").WithLocation(6, 38)
            };

            VerifyOperationTreeAndDiagnosticsForTest<AssignmentExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void DeclarationWithCircularity2()
        {
            string source = @"
class C
{
    static void Main()
    {
        /*<bind>*/var (x1, x2) = (x2, 2)/*</bind>*/;
    }
}
";
            string expectedOperationTree = @"
IDeconstructionAssignmentOperation (OperationKind.DeconstructionAssignment, Type: (var x1, System.Int32 x2), IsInvalid) (Syntax: 'var (x1, x2) = (x2, 2)')
  Left: 
    IDeclarationExpressionOperation (OperationKind.DeclarationExpression, Type: (var x1, System.Int32 x2)) (Syntax: 'var (x1, x2)')
      ITupleOperation (OperationKind.Tuple, Type: (var x1, System.Int32 x2)) (Syntax: '(x1, x2)')
        NaturalType: (var x1, System.Int32 x2)
        Elements(2):
            ILocalReferenceOperation: x1 (IsDeclaration: True) (OperationKind.LocalReference, Type: var) (Syntax: 'x1')
            ILocalReferenceOperation: x2 (IsDeclaration: True) (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'x2')
  Right: 
    IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: (var, System.Int32), IsInvalid, IsImplicit) (Syntax: '(x2, 2)')
      Conversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
      Operand: 
        ITupleOperation (OperationKind.Tuple, Type: (var x2, System.Int32), IsInvalid) (Syntax: '(x2, 2)')
          NaturalType: (var x2, System.Int32)
          Elements(2):
              ILocalReferenceOperation: x2 (OperationKind.LocalReference, Type: var, IsInvalid) (Syntax: 'x2')
              ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 2) (Syntax: '2')
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS0841: Cannot use local variable 'x2' before it is declared
                //         /*<bind>*/var (x1, x2) = (x2, 2)/*</bind>*/;
                Diagnostic(ErrorCode.ERR_VariableUsedBeforeDeclaration, "x2").WithArguments("x2").WithLocation(6, 35)
            };

            VerifyOperationTreeAndDiagnosticsForTest<AssignmentExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact, CompilerTrait(CompilerFeature.RefLocalsReturns)]
        [WorkItem(12283, "https://github.com/dotnet/roslyn/issues/12283")]
        public void RefReturningVarInvocation()
        {
            string source = @"
class C
{
    static int i;

    static void Main()
    {
        int x = 0, y = 0;
        /*<bind>*/var (x, y) = 42/*</bind>*/; // parsed as deconstruction
        System.Console.WriteLine(i);
    }
    static ref int var(int a, int b) { return ref i; }
}
";
            string expectedOperationTree = @"
IDeconstructionAssignmentOperation (OperationKind.DeconstructionAssignment, Type: ?, IsInvalid) (Syntax: 'var (x, y) = 42')
  Left: 
    IDeclarationExpressionOperation (OperationKind.DeclarationExpression, Type: (var x, var y), IsInvalid) (Syntax: 'var (x, y)')
      ITupleOperation (OperationKind.Tuple, Type: (var x, var y), IsInvalid) (Syntax: '(x, y)')
        NaturalType: (var x, var y)
        Elements(2):
            ILocalReferenceOperation: x (IsDeclaration: True) (OperationKind.LocalReference, Type: var, IsInvalid) (Syntax: 'x')
            ILocalReferenceOperation: y (IsDeclaration: True) (OperationKind.LocalReference, Type: var, IsInvalid) (Syntax: 'y')
  Right: 
    ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 42, IsInvalid) (Syntax: '42')
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS0128: A local variable or function named 'x' is already defined in this scope
                //         /*<bind>*/var (x, y) = 42/*</bind>*/; // parsed as deconstruction
                Diagnostic(ErrorCode.ERR_LocalDuplicate, "x").WithArguments("x").WithLocation(9, 24),
                // CS0128: A local variable or function named 'y' is already defined in this scope
                //         /*<bind>*/var (x, y) = 42/*</bind>*/; // parsed as deconstruction
                Diagnostic(ErrorCode.ERR_LocalDuplicate, "y").WithArguments("y").WithLocation(9, 27),
                // CS1061: 'int' does not contain a definition for 'Deconstruct' and no extension method 'Deconstruct' accepting a first argument of type 'int' could be found (are you missing a using directive or an assembly reference?)
                //         /*<bind>*/var (x, y) = 42/*</bind>*/; // parsed as deconstruction
                Diagnostic(ErrorCode.ERR_NoSuchMemberOrExtension, "42").WithArguments("int", "Deconstruct").WithLocation(9, 32),
                // CS0219: The variable 'x' is assigned but its value is never used
                //         int x = 0, y = 0;
                Diagnostic(ErrorCode.WRN_UnreferencedVarAssg, "x").WithArguments("x").WithLocation(8, 13),
                // CS0219: The variable 'y' is assigned but its value is never used
                //         int x = 0, y = 0;
                Diagnostic(ErrorCode.WRN_UnreferencedVarAssg, "y").WithArguments("y").WithLocation(8, 20)
            };

            VerifyOperationTreeAndDiagnosticsForTest<AssignmentExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [Fact(Skip = "https://github.com/dotnet/roslyn/issues/12468"), CompilerTrait(CompilerFeature.RefLocalsReturns)]
        [WorkItem(12468, "https://github.com/dotnet/roslyn/issues/12468")]
        public void RefReturningVarInvocation2()
        {
            string source = @"
class C
{
    static int i = 0;

    static void Main()
    {
        int x = 0, y = 0;
        @var(x, y) = 42; // parsed as invocation
        System.Console.Write(i + "" "");
        (var(x, y)) = 43; // parsed as invocation
        System.Console.Write(i + "" "");
        (var(x, y) = 44); // parsed as invocation
        System.Console.Write(i);
    }
    static ref int var(int a, int b) { return ref i; }
}
";
            // The correct expectation is for the code to compile and execute
            //var comp = CompileAndVerify(source, expectedOutput: "42 43 44");
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (11,9): error CS8134: Deconstruction must contain at least two variables.
                //         (var(x, y)) = 43; // parsed as invocation
                Diagnostic(ErrorCode.ERR_DeconstructTooFewElements, "(var(x, y)) = 43").WithLocation(11, 9),
                // (13,20): error CS1026: ) expected
                //         (var(x, y) = 44); // parsed as invocation
                Diagnostic(ErrorCode.ERR_CloseParenExpected, "=").WithLocation(13, 20),
                // (13,24): error CS1002: ; expected
                //         (var(x, y) = 44); // parsed as invocation
                Diagnostic(ErrorCode.ERR_SemicolonExpected, ")").WithLocation(13, 24),
                // (13,24): error CS1513: } expected
                //         (var(x, y) = 44); // parsed as invocation
                Diagnostic(ErrorCode.ERR_RbraceExpected, ")").WithLocation(13, 24),
                // (9,14): error CS0128: A local variable named 'x' is already defined in this scope
                //         @var(x, y) = 42; // parsed as invocation
                Diagnostic(ErrorCode.ERR_LocalDuplicate, "x").WithArguments("x").WithLocation(9, 14),
                // (9,9): error CS0246: The type or namespace name 'var' could not be found (are you missing a using directive or an assembly reference?)
                //         @var(x, y) = 42; // parsed as invocation
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "@var").WithArguments("var").WithLocation(9, 9),
                // (9,14): error CS8136: Deconstruction `var (...)` form disallows a specific type for 'var'.
                //         @var(x, y) = 42; // parsed as invocation
                Diagnostic(ErrorCode.ERR_DeconstructionVarFormDisallowsSpecificType, "x").WithLocation(9, 14),
                // (9,17): error CS0128: A local variable named 'y' is already defined in this scope
                //         @var(x, y) = 42; // parsed as invocation
                Diagnostic(ErrorCode.ERR_LocalDuplicate, "y").WithArguments("y").WithLocation(9, 17),
                // (9,9): error CS0246: The type or namespace name 'var' could not be found (are you missing a using directive or an assembly reference?)
                //         @var(x, y) = 42; // parsed as invocation
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "@var").WithArguments("var").WithLocation(9, 9),
                // (9,17): error CS8136: Deconstruction `var (...)` form disallows a specific type for 'var'.
                //         @var(x, y) = 42; // parsed as invocation
                Diagnostic(ErrorCode.ERR_DeconstructionVarFormDisallowsSpecificType, "y").WithLocation(9, 17),
                // (9,22): error CS1061: 'int' does not contain a definition for 'Deconstruct' and no extension method 'Deconstruct' accepting a first argument of type 'int' could be found (are you missing a using directive or an assembly reference?)
                //         @var(x, y) = 42; // parsed as invocation
                Diagnostic(ErrorCode.ERR_NoSuchMemberOrExtension, "42").WithArguments("int", "Deconstruct").WithLocation(9, 22),
                // (11,14): error CS0128: A local variable named 'x' is already defined in this scope
                //         (var(x, y)) = 43; // parsed as invocation
                Diagnostic(ErrorCode.ERR_LocalDuplicate, "x").WithArguments("x").WithLocation(11, 14),
                // (11,17): error CS0128: A local variable named 'y' is already defined in this scope
                //         (var(x, y)) = 43; // parsed as invocation
                Diagnostic(ErrorCode.ERR_LocalDuplicate, "y").WithArguments("y").WithLocation(11, 17),
                // (11,23): error CS1061: 'int' does not contain a definition for 'Deconstruct' and no extension method 'Deconstruct' accepting a first argument of type 'int' could be found (are you missing a using directive or an assembly reference?)
                //         (var(x, y)) = 43; // parsed as invocation
                Diagnostic(ErrorCode.ERR_NoSuchMemberOrExtension, "43").WithArguments("int", "Deconstruct").WithLocation(11, 23),
                // (13,14): error CS0128: A local variable named 'x' is already defined in this scope
                //         (var(x, y) = 44); // parsed as invocation
                Diagnostic(ErrorCode.ERR_LocalDuplicate, "x").WithArguments("x").WithLocation(13, 14),
                // (13,17): error CS0128: A local variable named 'y' is already defined in this scope
                //         (var(x, y) = 44); // parsed as invocation
                Diagnostic(ErrorCode.ERR_LocalDuplicate, "y").WithArguments("y").WithLocation(13, 17),
                // (13,22): error CS1061: 'int' does not contain a definition for 'Deconstruct' and no extension method 'Deconstruct' accepting a first argument of type 'int' could be found (are you missing a using directive or an assembly reference?)
                //         (var(x, y) = 44); // parsed as invocation
                Diagnostic(ErrorCode.ERR_NoSuchMemberOrExtension, "44").WithArguments("int", "Deconstruct").WithLocation(13, 22),
                // (8,13): warning CS0219: The variable 'x' is assigned but its value is never used
                //         int x = 0, y = 0;
                Diagnostic(ErrorCode.WRN_UnreferencedVarAssg, "x").WithArguments("x").WithLocation(8, 13),
                // (8,20): warning CS0219: The variable 'y' is assigned but its value is never used
                //         int x = 0, y = 0;
                Diagnostic(ErrorCode.WRN_UnreferencedVarAssg, "y").WithArguments("y").WithLocation(8, 20)
                );
        }

        [Fact, CompilerTrait(CompilerFeature.RefLocalsReturns)]
        [WorkItem(12283, "https://github.com/dotnet/roslyn/issues/12283")]
        public void RefReturningInvocation()
        {
            string source = @"
class C
{
    static int i;

    static void Main()
    {
        int x = 0, y = 0;
        M(x, y) = 42;
        System.Console.WriteLine(i);
    }
    static ref int M(int a, int b) { return ref i; }
}
";
            var comp = CompileAndVerify(source, expectedOutput: "42");
            comp.VerifyDiagnostics();
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void DeclarationWithTypeInsideVarForm()
        {
            string source = @"
class C
{
    static void Main()
    {
        var(int x1, x2) = (1, 2);
        var(var x3, x4) = (1, 2);
        /*<bind>*/var(x5, var(x6, x7)) = (1, (2, 3))/*</bind>*/;
    }
}
";
            string expectedOperationTree = @"
ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: ?, IsInvalid) (Syntax: 'var(x5, var ... (1, (2, 3))')
  Left: 
    IInvalidOperation (OperationKind.Invalid, Type: ?, IsInvalid) (Syntax: 'var(x5, var(x6, x7))')
      Children(3):
          IInvalidOperation (OperationKind.Invalid, Type: ?, IsInvalid) (Syntax: 'var')
            Children(0)
          IInvalidOperation (OperationKind.Invalid, Type: ?, IsInvalid) (Syntax: 'x5')
            Children(0)
          IInvalidOperation (OperationKind.Invalid, Type: ?, IsInvalid) (Syntax: 'var(x6, x7)')
            Children(3):
                IInvalidOperation (OperationKind.Invalid, Type: ?, IsInvalid) (Syntax: 'var')
                  Children(0)
                IInvalidOperation (OperationKind.Invalid, Type: ?, IsInvalid) (Syntax: 'x6')
                  Children(0)
                IInvalidOperation (OperationKind.Invalid, Type: ?, IsInvalid) (Syntax: 'x7')
                  Children(0)
  Right: 
    ITupleOperation (OperationKind.Tuple, Type: (System.Int32, (System.Int32, System.Int32))) (Syntax: '(1, (2, 3))')
      NaturalType: (System.Int32, (System.Int32, System.Int32))
      Elements(2):
          ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
          ITupleOperation (OperationKind.Tuple, Type: (System.Int32, System.Int32)) (Syntax: '(2, 3)')
            NaturalType: (System.Int32, System.Int32)
            Elements(2):
                ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 2) (Syntax: '2')
                ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 3) (Syntax: '3')
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS1525: Invalid expression term 'int'
                //         var(int x1, x2) = (1, 2);
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, "int").WithArguments("int").WithLocation(6, 13),
                // CS1003: Syntax error, ',' expected
                //         var(int x1, x2) = (1, 2);
                Diagnostic(ErrorCode.ERR_SyntaxError, "x1").WithArguments(",").WithLocation(6, 17),
                // CS1003: Syntax error, ',' expected
                //         var(var x3, x4) = (1, 2);
                Diagnostic(ErrorCode.ERR_SyntaxError, "x3").WithArguments(",").WithLocation(7, 17),
                // CS8199: The syntax 'var (...)' as an lvalue is reserved.
                //         var(int x1, x2) = (1, 2);
                Diagnostic(ErrorCode.ERR_VarInvocationLvalueReserved, "var(int x1, x2)").WithLocation(6, 9),
                // CS0103: The name 'var' does not exist in the current context
                //         var(int x1, x2) = (1, 2);
                Diagnostic(ErrorCode.ERR_NameNotInContext, "var").WithArguments("var").WithLocation(6, 9),
                // CS0103: The name 'x1' does not exist in the current context
                //         var(int x1, x2) = (1, 2);
                Diagnostic(ErrorCode.ERR_NameNotInContext, "x1").WithArguments("x1").WithLocation(6, 17),
                // CS0103: The name 'x2' does not exist in the current context
                //         var(int x1, x2) = (1, 2);
                Diagnostic(ErrorCode.ERR_NameNotInContext, "x2").WithArguments("x2").WithLocation(6, 21),
                // CS8199: The syntax 'var (...)' as an lvalue is reserved.
                //         var(var x3, x4) = (1, 2);
                Diagnostic(ErrorCode.ERR_VarInvocationLvalueReserved, "var(var x3, x4)").WithLocation(7, 9),
                // CS0103: The name 'var' does not exist in the current context
                //         var(var x3, x4) = (1, 2);
                Diagnostic(ErrorCode.ERR_NameNotInContext, "var").WithArguments("var").WithLocation(7, 9),
                // CS0103: The name 'var' does not exist in the current context
                //         var(var x3, x4) = (1, 2);
                Diagnostic(ErrorCode.ERR_NameNotInContext, "var").WithArguments("var").WithLocation(7, 13),
                // CS0103: The name 'x3' does not exist in the current context
                //         var(var x3, x4) = (1, 2);
                Diagnostic(ErrorCode.ERR_NameNotInContext, "x3").WithArguments("x3").WithLocation(7, 17),
                // CS0103: The name 'x4' does not exist in the current context
                //         var(var x3, x4) = (1, 2);
                Diagnostic(ErrorCode.ERR_NameNotInContext, "x4").WithArguments("x4").WithLocation(7, 21),
                // CS8199: The syntax 'var (...)' as an lvalue is reserved.
                //         /*<bind>*/var(x5, var(x6, x7)) = (1, (2, 3))/*</bind>*/;
                Diagnostic(ErrorCode.ERR_VarInvocationLvalueReserved, "var(x5, var(x6, x7))").WithLocation(8, 19),
                // CS0103: The name 'var' does not exist in the current context
                //         /*<bind>*/var(x5, var(x6, x7)) = (1, (2, 3))/*</bind>*/;
                Diagnostic(ErrorCode.ERR_NameNotInContext, "var").WithArguments("var").WithLocation(8, 19),
                // CS0103: The name 'x5' does not exist in the current context
                //         /*<bind>*/var(x5, var(x6, x7)) = (1, (2, 3))/*</bind>*/;
                Diagnostic(ErrorCode.ERR_NameNotInContext, "x5").WithArguments("x5").WithLocation(8, 23),
                // CS0103: The name 'var' does not exist in the current context
                //         /*<bind>*/var(x5, var(x6, x7)) = (1, (2, 3))/*</bind>*/;
                Diagnostic(ErrorCode.ERR_NameNotInContext, "var").WithArguments("var").WithLocation(8, 27),
                // CS0103: The name 'x6' does not exist in the current context
                //         /*<bind>*/var(x5, var(x6, x7)) = (1, (2, 3))/*</bind>*/;
                Diagnostic(ErrorCode.ERR_NameNotInContext, "x6").WithArguments("x6").WithLocation(8, 31),
                // CS0103: The name 'x7' does not exist in the current context
                //         /*<bind>*/var(x5, var(x6, x7)) = (1, (2, 3))/*</bind>*/;
                Diagnostic(ErrorCode.ERR_NameNotInContext, "x7").WithArguments("x7").WithLocation(8, 35)
            };

            VerifyOperationTreeAndDiagnosticsForTest<AssignmentExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void ForWithCircularity1()
        {
            string source = @"
class C
{
    static void Main()
    {
        for (/*<bind>*/var (x1, x2) = (1, x1)/*</bind>*/; ;) { }
    }
}
";
            string expectedOperationTree = @"
IDeconstructionAssignmentOperation (OperationKind.DeconstructionAssignment, Type: (System.Int32 x1, var x2), IsInvalid) (Syntax: 'var (x1, x2) = (1, x1)')
  Left: 
    IDeclarationExpressionOperation (OperationKind.DeclarationExpression, Type: (System.Int32 x1, var x2)) (Syntax: 'var (x1, x2)')
      ITupleOperation (OperationKind.Tuple, Type: (System.Int32 x1, var x2)) (Syntax: '(x1, x2)')
        NaturalType: (System.Int32 x1, var x2)
        Elements(2):
            ILocalReferenceOperation: x1 (IsDeclaration: True) (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'x1')
            ILocalReferenceOperation: x2 (IsDeclaration: True) (OperationKind.LocalReference, Type: var) (Syntax: 'x2')
  Right: 
    IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: (System.Int32, var), IsInvalid, IsImplicit) (Syntax: '(1, x1)')
      Conversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
      Operand: 
        ITupleOperation (OperationKind.Tuple, Type: (System.Int32, var x1), IsInvalid) (Syntax: '(1, x1)')
          NaturalType: (System.Int32, var x1)
          Elements(2):
              ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
              ILocalReferenceOperation: x1 (OperationKind.LocalReference, Type: var, IsInvalid) (Syntax: 'x1')
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS0841: Cannot use local variable 'x1' before it is declared
                //         for (/*<bind>*/var (x1, x2) = (1, x1)/*</bind>*/; ;) { }
                Diagnostic(ErrorCode.ERR_VariableUsedBeforeDeclaration, "x1").WithArguments("x1").WithLocation(6, 43)
            };

            VerifyOperationTreeAndDiagnosticsForTest<AssignmentExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void ForWithCircularity2()
        {
            string source = @"
class C
{
    static void Main()
    {
        for (/*<bind>*/var (x1, x2) = (x2, 2)/*</bind>*/; ;) { }
    }
}
";
            string expectedOperationTree = @"
IDeconstructionAssignmentOperation (OperationKind.DeconstructionAssignment, Type: (var x1, System.Int32 x2), IsInvalid) (Syntax: 'var (x1, x2) = (x2, 2)')
  Left: 
    IDeclarationExpressionOperation (OperationKind.DeclarationExpression, Type: (var x1, System.Int32 x2)) (Syntax: 'var (x1, x2)')
      ITupleOperation (OperationKind.Tuple, Type: (var x1, System.Int32 x2)) (Syntax: '(x1, x2)')
        NaturalType: (var x1, System.Int32 x2)
        Elements(2):
            ILocalReferenceOperation: x1 (IsDeclaration: True) (OperationKind.LocalReference, Type: var) (Syntax: 'x1')
            ILocalReferenceOperation: x2 (IsDeclaration: True) (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'x2')
  Right: 
    IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: (var, System.Int32), IsInvalid, IsImplicit) (Syntax: '(x2, 2)')
      Conversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
      Operand: 
        ITupleOperation (OperationKind.Tuple, Type: (var x2, System.Int32), IsInvalid) (Syntax: '(x2, 2)')
          NaturalType: (var x2, System.Int32)
          Elements(2):
              ILocalReferenceOperation: x2 (OperationKind.LocalReference, Type: var, IsInvalid) (Syntax: 'x2')
              ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 2) (Syntax: '2')
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS0841: Cannot use local variable 'x2' before it is declared
                //         for (/*<bind>*/var (x1, x2) = (x2, 2)/*</bind>*/; ;) { }
                Diagnostic(ErrorCode.ERR_VariableUsedBeforeDeclaration, "x2").WithArguments("x2").WithLocation(6, 40)
            };

            VerifyOperationTreeAndDiagnosticsForTest<AssignmentExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void ForEachNameConflict()
        {
            string source = @"
class C
{
    static void Main()
    {
        int x1 = 1;
        /*<bind>*/foreach ((int x1, int x2) in M()) { }/*</bind>*/
        System.Console.Write(x1);
    }
    static (int, int)[] M() { return new[] { (1, 2) }; }
}
";
            string expectedOperationTree = @"
IForEachLoopOperation (LoopKind.ForEach, Continue Label Id: 0, Exit Label Id: 1) (OperationKind.Loop, Type: null, IsInvalid) (Syntax: 'foreach ((i ... in M()) { }')
  Locals: Local_1: System.Int32 x1
    Local_2: System.Int32 x2
  LoopControlVariable: 
    ITupleOperation (OperationKind.Tuple, Type: (System.Int32 x1, System.Int32 x2), IsInvalid) (Syntax: '(int x1, int x2)')
      NaturalType: (System.Int32 x1, System.Int32 x2)
      Elements(2):
          IDeclarationExpressionOperation (OperationKind.DeclarationExpression, Type: System.Int32, IsInvalid) (Syntax: 'int x1')
            ILocalReferenceOperation: x1 (IsDeclaration: True) (OperationKind.LocalReference, Type: System.Int32, IsInvalid) (Syntax: 'x1')
          IDeclarationExpressionOperation (OperationKind.DeclarationExpression, Type: System.Int32) (Syntax: 'int x2')
            ILocalReferenceOperation: x2 (IsDeclaration: True) (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'x2')
  Collection: 
    IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Collections.IEnumerable, IsImplicit) (Syntax: 'M()')
      Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
      Operand: 
        IInvocationOperation ((System.Int32, System.Int32)[] C.M()) (OperationKind.Invocation, Type: (System.Int32, System.Int32)[]) (Syntax: 'M()')
          Instance Receiver: 
            null
          Arguments(0)
  Body: 
    IBlockOperation (0 statements) (OperationKind.Block, Type: null) (Syntax: '{ }')
  NextVariables(0)
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS0136: A local or parameter named 'x1' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
                //         /*<bind>*/foreach ((int x1, int x2) in M()) { }/*</bind>*/
                Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "x1").WithArguments("x1").WithLocation(7, 33)
            };

            VerifyOperationTreeAndDiagnosticsForTest<ForEachVariableStatementSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void ForEachNameConflict2()
        {
            string source = @"
class C
{
    static void Main()
    {
        /*<bind>*/foreach ((int x1, int x2) in M(out int x1)) { }/*</bind>*/
    }
    static (int, int)[] M(out int a) { a = 1; return new[] { (1, 2) }; }
}
";
            string expectedOperationTree = @"
IForEachLoopOperation (LoopKind.ForEach, Continue Label Id: 0, Exit Label Id: 1) (OperationKind.Loop, Type: null, IsInvalid) (Syntax: 'foreach ((i ... nt x1)) { }')
  Locals: Local_1: System.Int32 x1
    Local_2: System.Int32 x2
  LoopControlVariable: 
    ITupleOperation (OperationKind.Tuple, Type: (System.Int32 x1, System.Int32 x2), IsInvalid) (Syntax: '(int x1, int x2)')
      NaturalType: (System.Int32 x1, System.Int32 x2)
      Elements(2):
          IDeclarationExpressionOperation (OperationKind.DeclarationExpression, Type: System.Int32, IsInvalid) (Syntax: 'int x1')
            ILocalReferenceOperation: x1 (IsDeclaration: True) (OperationKind.LocalReference, Type: System.Int32, IsInvalid) (Syntax: 'x1')
          IDeclarationExpressionOperation (OperationKind.DeclarationExpression, Type: System.Int32) (Syntax: 'int x2')
            ILocalReferenceOperation: x2 (IsDeclaration: True) (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'x2')
  Collection: 
    IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Collections.IEnumerable, IsImplicit) (Syntax: 'M(out int x1)')
      Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
      Operand: 
        IInvocationOperation ((System.Int32, System.Int32)[] C.M(out System.Int32 a)) (OperationKind.Invocation, Type: (System.Int32, System.Int32)[]) (Syntax: 'M(out int x1)')
          Instance Receiver: 
            null
          Arguments(1):
              IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: a) (OperationKind.Argument, Type: null) (Syntax: 'out int x1')
                IDeclarationExpressionOperation (OperationKind.DeclarationExpression, Type: System.Int32) (Syntax: 'int x1')
                  ILocalReferenceOperation: x1 (IsDeclaration: True) (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'x1')
                InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
  Body: 
    IBlockOperation (0 statements) (OperationKind.Block, Type: null) (Syntax: '{ }')
  NextVariables(0)
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS0136: A local or parameter named 'x1' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
                //         /*<bind>*/foreach ((int x1, int x2) in M(out int x1)) { }/*</bind>*/
                Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "x1").WithArguments("x1").WithLocation(6, 33)
            };

            VerifyOperationTreeAndDiagnosticsForTest<ForEachVariableStatementSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [Fact]
        public void ForEachNameConflict3()
        {
            string source = @"
class C
{
    static void Main()
    {
        foreach ((int x1, int x2) in M())
        {
            int x1 = 1;
            System.Console.Write(x1);
        }
    }
    static (int, int)[] M() { return new[] { (1, 2) }; }
}
";

            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (8,17): error CS0136: A local or parameter named 'x1' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
                //             int x1 = 1;
                Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "x1").WithArguments("x1").WithLocation(8, 17)
                );
        }

        [Fact]
        public void ForEachUseBeforeDeclared()
        {
            string source = @"
class C
{
    static void Main()
    {
        foreach ((int x1, int x2) in M(x1)) { }
    }
    static (int, int)[] M(int a) { return new[] { (1, 2) }; }
}
";

            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (6,40): error CS0103: The name 'x1' does not exist in the current context
                //         foreach ((int x1, int x2) in M(x1))
                Diagnostic(ErrorCode.ERR_NameNotInContext, "x1").WithArguments("x1").WithLocation(6, 40)
                );
        }

        [Fact]
        public void ForEachUseOutsideScope()
        {
            string source = @"
class C
{
    static void Main()
    {
        foreach ((int x1, int x2) in M()) { }
        System.Console.Write(x1);
    }
    static (int, int)[] M() { return new[] { (1, 2) }; }
}
";

            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (7,30): error CS0103: The name 'x1' does not exist in the current context
                //         System.Console.Write(x1);
                Diagnostic(ErrorCode.ERR_NameNotInContext, "x1").WithArguments("x1").WithLocation(7, 30)
                );
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void ForEachNoIEnumerable()
        {
            string source = @"
class C
{
    static void Main()
    {
        foreach (/*<bind>*/var (x1, x2)/*</bind>*/ in 1)
        {
            System.Console.WriteLine(x1 + "" "" + x2);
        }
    }
}
";
            string expectedOperationTree = @"
IDeclarationExpressionOperation (OperationKind.DeclarationExpression, Type: (var x1, var x2), IsInvalid) (Syntax: 'var (x1, x2)')
  ITupleOperation (OperationKind.Tuple, Type: (var x1, var x2), IsInvalid) (Syntax: '(x1, x2)')
    NaturalType: (var x1, var x2)
    Elements(2):
        ILocalReferenceOperation: x1 (IsDeclaration: True) (OperationKind.LocalReference, Type: var, IsInvalid) (Syntax: 'x1')
        ILocalReferenceOperation: x2 (IsDeclaration: True) (OperationKind.LocalReference, Type: var, IsInvalid) (Syntax: 'x2')
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS1579: foreach statement cannot operate on variables of type 'int' because 'int' does not contain a public definition for 'GetEnumerator'
                //         foreach (/*<bind>*/var (x1, x2)/*</bind>*/ in 1)
                Diagnostic(ErrorCode.ERR_ForEachMissingMember, "1").WithArguments("int", "GetEnumerator").WithLocation(6, 55),
                // CS8130: Cannot infer the type of implicitly-typed deconstruction variable 'x1'.
                //         foreach (/*<bind>*/var (x1, x2)/*</bind>*/ in 1)
                Diagnostic(ErrorCode.ERR_TypeInferenceFailedForImplicitlyTypedDeconstructionVariable, "x1").WithArguments("x1").WithLocation(6, 33),
                // CS8130: Cannot infer the type of implicitly-typed deconstruction variable 'x2'.
                //         foreach (/*<bind>*/var (x1, x2)/*</bind>*/ in 1)
                Diagnostic(ErrorCode.ERR_TypeInferenceFailedForImplicitlyTypedDeconstructionVariable, "x2").WithArguments("x2").WithLocation(6, 37)
            };

            VerifyOperationTreeAndDiagnosticsForTest<DeclarationExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void ForEachIterationVariablesAreReadonly()
        {
            string source = @"
class C
{
    static void Main()
    {
        foreach (/*<bind>*/(int x1, var (x2, x3))/*</bind>*/ in new[] { (1, (1, 1)) })
        {
            x1 = 1;
            x2 = 2;
            x3 = 3;
        }
    }
}
";
            string expectedOperationTree = @"
ITupleOperation (OperationKind.Tuple, Type: (System.Int32 x1, (System.Int32 x2, System.Int32 x3))) (Syntax: '(int x1, var (x2, x3))')
  NaturalType: (System.Int32 x1, (System.Int32 x2, System.Int32 x3))
  Elements(2):
      IDeclarationExpressionOperation (OperationKind.DeclarationExpression, Type: System.Int32) (Syntax: 'int x1')
        ILocalReferenceOperation: x1 (IsDeclaration: True) (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'x1')
      IDeclarationExpressionOperation (OperationKind.DeclarationExpression, Type: (System.Int32 x2, System.Int32 x3)) (Syntax: 'var (x2, x3)')
        ITupleOperation (OperationKind.Tuple, Type: (System.Int32 x2, System.Int32 x3)) (Syntax: '(x2, x3)')
          NaturalType: (System.Int32 x2, System.Int32 x3)
          Elements(2):
              ILocalReferenceOperation: x2 (IsDeclaration: True) (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'x2')
              ILocalReferenceOperation: x3 (IsDeclaration: True) (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'x3')
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS1656: Cannot assign to 'x1' because it is a 'foreach iteration variable'
                //             x1 = 1;
                Diagnostic(ErrorCode.ERR_AssgReadonlyLocalCause, "x1").WithArguments("x1", "foreach iteration variable").WithLocation(8, 13),
                // CS1656: Cannot assign to 'x2' because it is a 'foreach iteration variable'
                //             x2 = 2;
                Diagnostic(ErrorCode.ERR_AssgReadonlyLocalCause, "x2").WithArguments("x2", "foreach iteration variable").WithLocation(9, 13),
                // CS1656: Cannot assign to 'x3' because it is a 'foreach iteration variable'
                //             x3 = 3;
                Diagnostic(ErrorCode.ERR_AssgReadonlyLocalCause, "x3").WithArguments("x3", "foreach iteration variable").WithLocation(10, 13)
            };

            VerifyOperationTreeAndDiagnosticsForTest<TupleExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [Fact]
        public void ForEachScoping()
        {
            string source = @"
class C
{
    static void Main()
    {
        foreach (var (x1, x2) in M(x1)) { }
    }
    static (int, int) M(int i) { return (1, 2); }
}
";

            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (6,36): error CS0103: The name 'x1' does not exist in the current context
                //         foreach (var (x1, x2) in M(x1)) { }
                Diagnostic(ErrorCode.ERR_NameNotInContext, "x1").WithArguments("x1").WithLocation(6, 36),
                // (6,34): error CS1579: foreach statement cannot operate on variables of type '(int, int)' because '(int, int)' does not contain a public instance or extension definition for 'GetEnumerator'
                //         foreach (var (x1, x2) in M(x1)) { }
                Diagnostic(ErrorCode.ERR_ForEachMissingMember, "M(x1)").WithArguments("(int, int)", "GetEnumerator").WithLocation(6, 34),
                // (6,23): error CS8130: Cannot infer the type of implicitly-typed deconstruction variable 'x1'.
                //         foreach (var (x1, x2) in M(x1)) { }
                Diagnostic(ErrorCode.ERR_TypeInferenceFailedForImplicitlyTypedDeconstructionVariable, "x1").WithArguments("x1").WithLocation(6, 23),
                // (6,27): error CS8130: Cannot infer the type of implicitly-typed deconstruction variable 'x2'.
                //         foreach (var (x1, x2) in M(x1)) { }
                Diagnostic(ErrorCode.ERR_TypeInferenceFailedForImplicitlyTypedDeconstructionVariable, "x2").WithArguments("x2").WithLocation(6, 27)
                );
        }

        [Fact]
        public void AssignmentDataFlow()
        {
            string source = @"
class C
{
    static void Main()
    {
        int x, y;
        (x, y) = new C(); // x and y are assigned here, so no complaints on usage of un-initialized locals on the line below
        System.Console.WriteLine(x + "" "" + y);
    }

    public void Deconstruct(out int a, out int b)
    {
        a = 1;
        b = 2;
    }
}
";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics();
        }

        [Fact]
        public void GetTypeInfoForTupleLiteral()
        {
            var source = @"
class C
{
    static void Main()
    {
        var x1 = (1, 2);
        var (x2, x3) = (1, 2);
        System.Console.Write($""{x1} {x2} {x3}"");
    }
}
";
            Action<ModuleSymbol> validator = module =>
            {
                var sourceModule = (SourceModuleSymbol)module;
                var compilation = sourceModule.DeclaringCompilation;
                var tree = compilation.SyntaxTrees.First();
                var model = compilation.GetSemanticModel(tree);
                var nodes = tree.GetCompilationUnitRoot().DescendantNodes();

                var literal1 = nodes.OfType<TupleExpressionSyntax>().First();
                Assert.Equal("(int, int)", model.GetTypeInfo(literal1).Type.ToDisplayString());

                var literal2 = nodes.OfType<TupleExpressionSyntax>().Skip(1).First();
                Assert.Equal("(int, int)", model.GetTypeInfo(literal2).Type.ToDisplayString());
            };

            var verifier = CompileAndVerify(source, sourceSymbolValidator: validator);
            verifier.VerifyDiagnostics();
        }

        [Fact]
        public void DeclarationWithCircularity3()
        {
            string source = @"
class C
{
    static void Main()
    {
        var (x1, x2) = (M(out x2), M(out x1));
    }
    static T M<T>(out T x) { x = default(T); return x; }
}
";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (6,31): error CS0841: Cannot use local variable 'x2' before it is declared
                //         var (x1, x2) = (M(out x2), M(out x1));
                Diagnostic(ErrorCode.ERR_VariableUsedBeforeDeclaration, "x2").WithArguments("x2").WithLocation(6, 31),
                // (6,42): error CS0841: Cannot use local variable 'x1' before it is declared
                //         var (x1, x2) = (M(out x2), M(out x1));
                Diagnostic(ErrorCode.ERR_VariableUsedBeforeDeclaration, "x1").WithArguments("x1").WithLocation(6, 42)
                );
        }

        [Fact, WorkItem(13081, "https://github.com/dotnet/roslyn/issues/13081")]
        public void GettingDiagnosticsWhenValueTupleIsMissing()
        {
            var source = @"
class C1
{
    static void Test(int arg1, (byte, byte) arg2)
    {
        foreach ((int, int) e in new (int, int)[10])
        {
        }
    }
}
";
            var comp = CreateCompilationWithMscorlib40(source);
            comp.VerifyDiagnostics(
                // (4,32): error CS8179: Predefined type 'System.ValueTuple`2' is not defined or imported
                //     static void Test(int arg1, (byte, byte) arg2)
                Diagnostic(ErrorCode.ERR_PredefinedValueTupleTypeNotFound, "(byte, byte)").WithArguments("System.ValueTuple`2").WithLocation(4, 32),
                // (6,38): error CS8179: Predefined type 'System.ValueTuple`2' is not defined or imported
                //         foreach ((int, int) e in new (int, int)[10])
                Diagnostic(ErrorCode.ERR_PredefinedValueTupleTypeNotFound, "(int, int)").WithArguments("System.ValueTuple`2").WithLocation(6, 38),
                // (6,18): error CS8179: Predefined type 'System.ValueTuple`2' is not defined or imported
                //         foreach ((int, int) e in new (int, int)[10])
                Diagnostic(ErrorCode.ERR_PredefinedValueTupleTypeNotFound, "(int, int)").WithArguments("System.ValueTuple`2").WithLocation(6, 18)
                );
            // no crash
        }

        [Fact]
        public void DeconstructionMayBeEmbedded()
        {
            var source = @"
class C1
{
    void M()
    {
        if (true)
            var (x, y) = (1, 2);
    }
}
";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // this is no longer considered a declaration statement,
                // but rather is an assignment expression. So no error.
                );
        }

        [Fact]
        public void AssignmentExpressionCanBeUsedInEmbeddedStatement()
        {
            var source = @"
class C1
{
    void M()
    {
        int x, y;
        if (true)
            (x, y) = (1, 2);
    }
}
";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics();
        }

        [Fact]
        public void DeconstructObsoleteWarning()
        {
            var source = @"
class C
{
    void M()
    {
       (int y1, int y2) = new C();
    }
    [System.Obsolete()]
    void Deconstruct(out int x1, out int x2) { x1 = 1; x2 = 2; }
}
";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (6,27): warning CS0612: 'C.Deconstruct(out int, out int)' is obsolete
                //        (int y1, int y2) = new C();
                Diagnostic(ErrorCode.WRN_DeprecatedSymbol, "new C()").WithArguments("C.Deconstruct(out int, out int)").WithLocation(6, 27)
                );
        }

        [Fact]
        public void DeconstructObsoleteError()
        {
            var source = @"
class C
{
    void M()
    {
       (int y1, int y2) = new C();
    }
    [System.Obsolete(""Deprecated"", error: true)]
    void Deconstruct(out int x1, out int x2) { x1 = 1; x2 = 2; }
}
";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (6,27): error CS0619: 'C.Deconstruct(out int, out int)' is obsolete: 'Deprecated'
                //        (int y1, int y2) = new C();
                Diagnostic(ErrorCode.ERR_DeprecatedSymbolStr, "new C()").WithArguments("C.Deconstruct(out int, out int)", "Deprecated").WithLocation(6, 27)
                );
        }

        [Fact]
        public void DeconstructionLocalsDeclaredNotUsed()
        {
            // Check that there are no *use sites* within this code for local variables.
            // They are not declared. So they should not be returned
            // by SemanticModel.GetSymbolInfo. Similarly, check that all designation syntax
            // forms declare deconstruction locals.
            string source = @"
class Program
{
    static void Main()
    {
        var (x1, y1) = (1, 2);

        (var x2, var y2) = (1, 2);
    }

    static void M((int, int) t)
    {
        var (x3, y3) = t;

        (var x4, var y4) = t;
    }
}
";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics();

            var tree = comp.SyntaxTrees.First();
            var model = comp.GetSemanticModel(tree, ignoreAccessibility: false);
            var nodes = tree.GetCompilationUnitRoot().DescendantNodes();
            foreach (var node in nodes)
            {
                var si = model.GetSymbolInfo(node);
                var symbol = si.Symbol;
                if ((object)symbol != null)
                {
                    if (node is DeclarationExpressionSyntax)
                    {
                        Assert.Equal(SymbolKind.Local, symbol.Kind);
                        Assert.Equal(LocalDeclarationKind.DeconstructionVariable, symbol.GetSymbol<LocalSymbol>().DeclarationKind);
                    }
                    else
                    {
                        Assert.NotEqual(SymbolKind.Local, symbol.Kind);
                    }
                }

                symbol = model.GetDeclaredSymbol(node);
                if ((object)symbol != null)
                {
                    if (node is SingleVariableDesignationSyntax)
                    {
                        Assert.Equal(SymbolKind.Local, symbol.Kind);
                        Assert.Equal(LocalDeclarationKind.DeconstructionVariable, symbol.GetSymbol<LocalSymbol>().DeclarationKind);
                    }
                    else
                    {
                        Assert.NotEqual(SymbolKind.Local, symbol.Kind);
                    }
                }
            }
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact, WorkItem(14287, "https://github.com/dotnet/roslyn/issues/14287")]
        public void TupleDeconstructionStatementWithTypesCannotBeConst()
        {
            string source = @"
class C
{
    static void Main()
    {
        /*<bind>*/const (int x, int y) = (1, 2);/*</bind>*/
    }
}
";
            string expectedOperationTree = @"
IVariableDeclarationGroupOperation (1 declarations) (OperationKind.VariableDeclarationGroup, Type: null, IsInvalid) (Syntax: 'const (int  ... ) = (1, 2);')
  IVariableDeclarationOperation (1 declarators) (OperationKind.VariableDeclaration, Type: null, IsInvalid) (Syntax: '(int x, int y) = (1, 2)')
    Declarators:
        IVariableDeclaratorOperation (Symbol: (System.Int32 x, System.Int32 y) ) (OperationKind.VariableDeclarator, Type: null, IsInvalid) (Syntax: '= (1, 2)')
          Initializer: 
            IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null, IsInvalid) (Syntax: '= (1, 2)')
              IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: (System.Int32 x, System.Int32 y), IsImplicit) (Syntax: '(1, 2)')
                Conversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                Operand: 
                  ITupleOperation (OperationKind.Tuple, Type: (System.Int32, System.Int32)) (Syntax: '(1, 2)')
                    NaturalType: (System.Int32, System.Int32)
                    Elements(2):
                        ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
                        ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 2) (Syntax: '2')
    Initializer: 
      null
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS1001: Identifier expected
                //         const /*<bind>*/(int x, int y) = (1, 2)/*</bind>*/;
                Diagnostic(ErrorCode.ERR_IdentifierExpected, "=").WithLocation(6, 40),
                // CS0283: The type '(int x, int y)' cannot be declared const
                //         const /*<bind>*/(int x, int y) = (1, 2)/*</bind>*/;
                Diagnostic(ErrorCode.ERR_BadConstType, "(int x, int y)").WithArguments("(int x, int y)").WithLocation(6, 25)
            };

            VerifyOperationTreeAndDiagnosticsForTest<LocalDeclarationStatementSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [Fact, WorkItem(14287, "https://github.com/dotnet/roslyn/issues/14287")]
        public void TupleDeconstructionStatementWithoutTypesCannotBeConst()
        {
            string source = @"
class C
{
    static void Main()
    {
        const var (x, y) = (1, 2);
    }
}
";

            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (6,9): error CS0106: The modifier 'const' is not valid for this item
                //         const var (x, y) = (1, 2);
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "const").WithArguments("const").WithLocation(6, 9),
                // (6,19): error CS1001: Identifier expected
                //         const var (x, y) = (1, 2);
                Diagnostic(ErrorCode.ERR_IdentifierExpected, "(").WithLocation(6, 19),
                // (6,21): error CS1001: Identifier expected
                //         const var (x, y) = (1, 2);
                Diagnostic(ErrorCode.ERR_IdentifierExpected, ",").WithLocation(6, 21),
                // (6,24): error CS1001: Identifier expected
                //         const var (x, y) = (1, 2);
                Diagnostic(ErrorCode.ERR_IdentifierExpected, ")").WithLocation(6, 24),
                // (6,26): error CS1002: ; expected
                //         const var (x, y) = (1, 2);
                Diagnostic(ErrorCode.ERR_SemicolonExpected, "=").WithLocation(6, 26),
                // (6,26): error CS1525: Invalid expression term '='
                //         const var (x, y) = (1, 2);
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, "=").WithArguments("=").WithLocation(6, 26),
                // (6,19): error CS8112: '(x, y)' is a local function and must therefore always have a body.
                //         const var (x, y) = (1, 2);
                Diagnostic(ErrorCode.ERR_LocalFunctionMissingBody, "").WithArguments("(x, y)").WithLocation(6, 19),
                // (6,20): error CS0246: The type or namespace name 'x' could not be found (are you missing a using directive or an assembly reference?)
                //         const var (x, y) = (1, 2);
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "x").WithArguments("x").WithLocation(6, 20),
                // (6,23): error CS0246: The type or namespace name 'y' could not be found (are you missing a using directive or an assembly reference?)
                //         const var (x, y) = (1, 2);
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "y").WithArguments("y").WithLocation(6, 23),
                // (6,15): error CS0825: The contextual keyword 'var' may only appear within a local variable declaration or in script code
                //         const var (x, y) = (1, 2);
                Diagnostic(ErrorCode.ERR_TypeVarNotFound, "var").WithLocation(6, 15)
            );
        }

        [Fact, WorkItem(15934, "https://github.com/dotnet/roslyn/issues/15934")]
        public void PointerTypeInDeconstruction()
        {
            string source = @"
unsafe class C
{
    static void Main(C c)
    {
        (int* x1, int y1) = c;
        (var* x2, int y2) = c;
        (int*[] x3, int y3) = c;
        (var*[] x4, int y4) = c;
    }
    public void Deconstruct(out dynamic x, out dynamic y)
    {
        x = y = null;
    }
}
";
            var comp = CreateCompilationWithMscorlib40AndSystemCore(source,
                references: new[] { ValueTupleRef, SystemRuntimeFacadeRef },
                options: TestOptions.UnsafeDebugDll,
                parseOptions: TestOptions.RegularPreview);

            // The precise diagnostics here are not important, and may be sensitive to parser
            // adjustments. This is a test that we don't crash. The errors here are likely to
            // change as we adjust the parser and semantic analysis of error cases.
            comp.VerifyDiagnostics(
                // (6,10): error CS1525: Invalid expression term 'int'
                //         (int* x1, int y1) = c;
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, "int").WithArguments("int").WithLocation(6, 10),
                // (6,15): error CS0103: The name 'x1' does not exist in the current context
                //         (int* x1, int y1) = c;
                Diagnostic(ErrorCode.ERR_NameNotInContext, "x1").WithArguments("x1").WithLocation(6, 15),
                // (6,19): error CS0266: Cannot implicitly convert type 'dynamic' to 'int'. An explicit conversion exists (are you missing a cast?)
                //         (int* x1, int y1) = c;
                Diagnostic(ErrorCode.ERR_NoImplicitConvCast, "int y1").WithArguments("dynamic", "int").WithLocation(6, 19),
                // (7,10): error CS0103: The name 'var' does not exist in the current context
                //         (var* x2, int y2) = c;
                Diagnostic(ErrorCode.ERR_NameNotInContext, "var").WithArguments("var").WithLocation(7, 10),
                // (7,15): error CS0103: The name 'x2' does not exist in the current context
                //         (var* x2, int y2) = c;
                Diagnostic(ErrorCode.ERR_NameNotInContext, "x2").WithArguments("x2").WithLocation(7, 15),
                // (7,19): error CS0266: Cannot implicitly convert type 'dynamic' to 'int'. An explicit conversion exists (are you missing a cast?)
                //         (var* x2, int y2) = c;
                Diagnostic(ErrorCode.ERR_NoImplicitConvCast, "int y2").WithArguments("dynamic", "int").WithLocation(7, 19),
                // (8,10): error CS0266: Cannot implicitly convert type 'dynamic' to 'int*[]'. An explicit conversion exists (are you missing a cast?)
                //         (int*[] x3, int y3) = c;
                Diagnostic(ErrorCode.ERR_NoImplicitConvCast, "int*[] x3").WithArguments("dynamic", "int*[]").WithLocation(8, 10),
                // (8,21): error CS0266: Cannot implicitly convert type 'dynamic' to 'int'. An explicit conversion exists (are you missing a cast?)
                //         (int*[] x3, int y3) = c;
                Diagnostic(ErrorCode.ERR_NoImplicitConvCast, "int y3").WithArguments("dynamic", "int").WithLocation(8, 21),
                // (9,10): error CS0825: The contextual keyword 'var' may only appear within a local variable declaration or in script code
                //         (var*[] x4, int y4) = c;
                Diagnostic(ErrorCode.ERR_TypeVarNotFound, "var").WithLocation(9, 10),
                // (9,21): error CS0266: Cannot implicitly convert type 'dynamic' to 'int'. An explicit conversion exists (are you missing a cast?)
                //         (var*[] x4, int y4) = c;
                Diagnostic(ErrorCode.ERR_NoImplicitConvCast, "int y4").WithArguments("dynamic", "int").WithLocation(9, 21)
                );
        }

        [Fact]
        public void DeclarationInsideNameof()
        {
            string source = @"
class Program
{
    static void Main()
    {
        string s = nameof((int x1, var x2) = (1, 2)).ToString();
        string s1 = x1, s2 = x2;
    }
}
";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (6,28): error CS8185: A declaration is not allowed in this context.
                //         string s = nameof((int x1, var x2) = (1, 2)).ToString();
                Diagnostic(ErrorCode.ERR_DeclarationExpressionNotPermitted, "int x1").WithLocation(6, 28),
                // (6,27): error CS8081: Expression does not have a name.
                //         string s = nameof((int x1, var x2) = (1, 2)).ToString();
                Diagnostic(ErrorCode.ERR_ExpressionHasNoName, "(int x1, var x2) = (1, 2)").WithLocation(6, 27),
                // (7,21): error CS0029: Cannot implicitly convert type 'int' to 'string'
                //         string s1 = x1, s2 = x2;
                Diagnostic(ErrorCode.ERR_NoImplicitConv, "x1").WithArguments("int", "string").WithLocation(7, 21),
                // (7,30): error CS0029: Cannot implicitly convert type 'int' to 'string'
                //         string s1 = x1, s2 = x2;
                Diagnostic(ErrorCode.ERR_NoImplicitConv, "x2").WithArguments("int", "string").WithLocation(7, 30),
                // (7,21): error CS0165: Use of unassigned local variable 'x1'
                //         string s1 = x1, s2 = x2;
                Diagnostic(ErrorCode.ERR_UseDefViolation, "x1").WithArguments("x1").WithLocation(7, 21),
                // (7,30): error CS0165: Use of unassigned local variable 'x2'
                //         string s1 = x1, s2 = x2;
                Diagnostic(ErrorCode.ERR_UseDefViolation, "x2").WithArguments("x2").WithLocation(7, 30)
                );

            var tree = comp.SyntaxTrees.First();
            var model = comp.GetSemanticModel(tree);
            var designations = tree.GetCompilationUnitRoot().DescendantNodes().OfType<SingleVariableDesignationSyntax>().ToArray();
            Assert.Equal(2, designations.Count());
            var refs = tree.GetCompilationUnitRoot().DescendantNodes().OfType<IdentifierNameSyntax>();

            var x1 = model.GetDeclaredSymbol(designations[0]);
            Assert.Equal("x1", x1.Name);
            Assert.Equal("System.Int32", ((ILocalSymbol)x1).Type.ToTestDisplayString());
            Assert.Same(x1, model.GetSymbolInfo(refs.Where(r => r.Identifier.ValueText == "x1").Single()).Symbol);

            var x2 = model.GetDeclaredSymbol(designations[1]);
            Assert.Equal("x2", x2.Name);
            Assert.Equal("System.Int32", ((ILocalSymbol)x2).Type.ToTestDisplayString());
            Assert.Same(x2, model.GetSymbolInfo(refs.Where(r => r.Identifier.ValueText == "x2").Single()).Symbol);
        }

        [Fact, WorkItem(17572, "https://github.com/dotnet/roslyn/issues/17572")]
        public void StandAlone_01()
        {
            string source1 = @"
class C
{
    static void Main()
    {
        (var (a,b), var c, int d);
    }
}
";

            var comp1 = CreateCompilation(source1);
            comp1.VerifyDiagnostics(
                // (6,10): error CS8185: A declaration is not allowed in this context.
                //         (var (a,b), var c, int d);
                Diagnostic(ErrorCode.ERR_DeclarationExpressionNotPermitted, "var (a,b)"),
                // (6,21): error CS8185: A declaration is not allowed in this context.
                //         (var (a,b), var c, int d);
                Diagnostic(ErrorCode.ERR_DeclarationExpressionNotPermitted, "var c"),
                // (6,28): error CS8185: A declaration is not allowed in this context.
                //         (var (a,b), var c, int d);
                Diagnostic(ErrorCode.ERR_DeclarationExpressionNotPermitted, "int d").WithLocation(6, 28),
                // (6,9): error CS0201: Only assignment, call, increment, decrement, await, and new object expressions can be used as a statement
                //         (var (a,b), var c, int d);
                Diagnostic(ErrorCode.ERR_IllegalStatement, "(var (a,b), var c, int d)").WithLocation(6, 9),
                // (6,28): error CS0165: Use of unassigned local variable 'd'
                //         (var (a,b), var c, int d);
                Diagnostic(ErrorCode.ERR_UseDefViolation, "int d").WithArguments("d").WithLocation(6, 28)
                );

            StandAlone_01_VerifySemanticModel(comp1, LocalDeclarationKind.DeclarationExpressionVariable);

            string source2 = @"
class C
{
    static void Main()
    {
        (var (a,b), var c, int d) = D;
    }
}
";

            var comp2 = CreateCompilation(source2);

            StandAlone_01_VerifySemanticModel(comp2, LocalDeclarationKind.DeconstructionVariable);
        }

        private static void StandAlone_01_VerifySemanticModel(CSharpCompilation comp, LocalDeclarationKind localDeclarationKind)
        {
            var tree = comp.SyntaxTrees.First();
            var model = comp.GetSemanticModel(tree);
            var designations = tree.GetCompilationUnitRoot().DescendantNodes().OfType<SingleVariableDesignationSyntax>().ToArray();
            Assert.Equal(4, designations.Count());

            var a = model.GetDeclaredSymbol(designations[0]);
            Assert.Equal("var a", a.ToTestDisplayString());
            Assert.Equal(localDeclarationKind, a.GetSymbol<LocalSymbol>().DeclarationKind);

            var b = model.GetDeclaredSymbol(designations[1]);
            Assert.Equal("var b", b.ToTestDisplayString());
            Assert.Equal(localDeclarationKind, b.GetSymbol<LocalSymbol>().DeclarationKind);

            var c = model.GetDeclaredSymbol(designations[2]);
            Assert.Equal("var c", c.ToTestDisplayString());
            Assert.Equal(localDeclarationKind, c.GetSymbol<LocalSymbol>().DeclarationKind);

            var d = model.GetDeclaredSymbol(designations[3]);
            Assert.Equal("System.Int32 d", d.ToTestDisplayString());
            Assert.Equal(localDeclarationKind, d.GetSymbol<LocalSymbol>().DeclarationKind);

            var declarations = tree.GetCompilationUnitRoot().DescendantNodes().OfType<DeclarationExpressionSyntax>().ToArray();
            Assert.Equal(3, declarations.Count());

            Assert.Equal("var (a,b)", declarations[0].ToString());
            var typeInfo = model.GetTypeInfo(declarations[0]);
            Assert.Equal("(var a, var b)", typeInfo.Type.ToTestDisplayString());
            Assert.Equal(typeInfo.Type, typeInfo.ConvertedType);
            Assert.True(model.GetConversion(declarations[0]).IsIdentity);
            var symbolInfo = model.GetSymbolInfo(declarations[0]);
            Assert.Null(symbolInfo.Symbol);
            Assert.Empty(symbolInfo.CandidateSymbols);
            Assert.Equal(CandidateReason.None, symbolInfo.CandidateReason);
            typeInfo = model.GetTypeInfo(declarations[0].Type);
            Assert.Equal("(var a, var b)", typeInfo.Type.ToTestDisplayString());
            Assert.Equal("(var a, var b)", typeInfo.ConvertedType.ToTestDisplayString());
            Assert.Equal(typeInfo.Type, typeInfo.ConvertedType);
            Assert.Equal(TypeKind.Struct, typeInfo.Type.TypeKind);
            Assert.Equal(TypeKind.Error, ((INamedTypeSymbol)typeInfo.Type).TypeArguments[0].TypeKind);
            Assert.Equal(TypeKind.Error, ((INamedTypeSymbol)typeInfo.Type).TypeArguments[1].TypeKind);
            Assert.True(model.GetConversion(declarations[0].Type).IsIdentity);
            symbolInfo = model.GetSymbolInfo(declarations[0].Type);
            Assert.Equal("(var a, var b)", symbolInfo.Symbol.ToTestDisplayString());
            Assert.Empty(symbolInfo.CandidateSymbols);
            Assert.Equal(CandidateReason.None, symbolInfo.CandidateReason);
            Assert.Null(model.GetAliasInfo(declarations[0].Type));

            Assert.Equal("var c", declarations[1].ToString());
            typeInfo = model.GetTypeInfo(declarations[1]);
            Assert.Equal("var", typeInfo.Type.ToTestDisplayString());
            Assert.Equal(SymbolKind.ErrorType, typeInfo.Type.Kind);
            Assert.Equal(typeInfo.Type, typeInfo.ConvertedType);
            Assert.True(model.GetConversion(declarations[1]).IsIdentity);
            Assert.Equal("var c", model.GetSymbolInfo(declarations[1]).Symbol.ToTestDisplayString());
            typeInfo = model.GetTypeInfo(declarations[1].Type);
            Assert.Equal("var", typeInfo.Type.ToTestDisplayString());
            Assert.Equal(SymbolKind.ErrorType, typeInfo.Type.Kind);
            Assert.Equal(typeInfo.Type, typeInfo.ConvertedType);
            Assert.True(model.GetConversion(declarations[1].Type).IsIdentity);
            symbolInfo = model.GetSymbolInfo(declarations[1].Type);
            Assert.Null(symbolInfo.Symbol);
            Assert.Empty(symbolInfo.CandidateSymbols);
            Assert.Equal(CandidateReason.None, symbolInfo.CandidateReason);
            Assert.Null(model.GetAliasInfo(declarations[1].Type));

            Assert.Equal("int d", declarations[2].ToString());
            typeInfo = model.GetTypeInfo(declarations[2]);
            Assert.Equal("System.Int32", typeInfo.Type.ToTestDisplayString());
            Assert.Equal(typeInfo.Type, typeInfo.ConvertedType);
            Assert.True(model.GetConversion(declarations[2]).IsIdentity);
            Assert.Equal("System.Int32 d", model.GetSymbolInfo(declarations[2]).Symbol.ToTestDisplayString());
            typeInfo = model.GetTypeInfo(declarations[2].Type);
            Assert.Equal("System.Int32", typeInfo.Type.ToTestDisplayString());
            Assert.Equal(typeInfo.Type, typeInfo.ConvertedType);
            Assert.True(model.GetConversion(declarations[2].Type).IsIdentity);
            symbolInfo = model.GetSymbolInfo(declarations[2].Type);
            Assert.Equal("System.Int32", symbolInfo.Symbol.ToTestDisplayString());
            Assert.Null(model.GetAliasInfo(declarations[2].Type));

            var tuple = tree.GetCompilationUnitRoot().DescendantNodes().OfType<TupleExpressionSyntax>().Single();
            typeInfo = model.GetTypeInfo(tuple);
            Assert.Equal("((var a, var b), var c, System.Int32 d)", typeInfo.Type.ToTestDisplayString());
            Assert.Equal(typeInfo.Type, typeInfo.ConvertedType);
            Assert.True(model.GetConversion(tuple).IsIdentity);
            symbolInfo = model.GetSymbolInfo(tuple);
            Assert.Null(symbolInfo.Symbol);
            Assert.Empty(symbolInfo.CandidateSymbols);
            Assert.Equal(CandidateReason.None, symbolInfo.CandidateReason);
        }

        [Fact, WorkItem(17572, "https://github.com/dotnet/roslyn/issues/17572")]
        public void StandAlone_02()
        {
            string source1 = @"
(var (a,b), var c, int d);
";

            var comp1 = CreateCompilation(source1, parseOptions: TestOptions.Script);
            comp1.VerifyDiagnostics(
                // (2,7): error CS7019: Type of 'a' cannot be inferred since its initializer directly or indirectly refers to the definition.
                // (var (a,b), var c, int d);
                Diagnostic(ErrorCode.ERR_RecursivelyTypedVariable, "a").WithArguments("a"),
                // (2,9): error CS7019: Type of 'b' cannot be inferred since its initializer directly or indirectly refers to the definition.
                // (var (a,b), var c, int d);
                Diagnostic(ErrorCode.ERR_RecursivelyTypedVariable, "b").WithArguments("b"),
                // (2,17): error CS7019: Type of 'c' cannot be inferred since its initializer directly or indirectly refers to the definition.
                // (var (a,b), var c, int d);
                Diagnostic(ErrorCode.ERR_RecursivelyTypedVariable, "c").WithArguments("c"),
                // (2,2): error CS8185: A declaration is not allowed in this context.
                // (var (a,b), var c, int d);
                Diagnostic(ErrorCode.ERR_DeclarationExpressionNotPermitted, "var (a,b)"),
                // (2,13): error CS8185: A declaration is not allowed in this context.
                // (var (a,b), var c, int d);
                Diagnostic(ErrorCode.ERR_DeclarationExpressionNotPermitted, "var c"),
                // (2,20): error CS8185: A declaration is not allowed in this context.
                // (var (a,b), var c, int d);
                Diagnostic(ErrorCode.ERR_DeclarationExpressionNotPermitted, "int d").WithLocation(2, 20),
                // (2,1): error CS0201: Only assignment, call, increment, decrement, await, and new object expressions can be used as a statement
                // (var (a,b), var c, int d);
                Diagnostic(ErrorCode.ERR_IllegalStatement, "(var (a,b), var c, int d)").WithLocation(2, 1)
                );

            StandAlone_02_VerifySemanticModel(comp1);

            string source2 = @"
(var (a,b), var c, int d) = D;
";

            var comp2 = CreateCompilation(source2, parseOptions: TestOptions.Script);

            StandAlone_02_VerifySemanticModel(comp2);
        }

        private static void StandAlone_02_VerifySemanticModel(CSharpCompilation comp)
        {
            var tree = comp.SyntaxTrees.First();
            var model = comp.GetSemanticModel(tree);
            var designations = tree.GetCompilationUnitRoot().DescendantNodes().OfType<SingleVariableDesignationSyntax>().ToArray();
            Assert.Equal(4, designations.Count());

            var a = model.GetDeclaredSymbol(designations[0]);
            Assert.Equal("var Script.a", a.ToTestDisplayString());
            Assert.Equal(SymbolKind.Field, a.Kind);

            var b = model.GetDeclaredSymbol(designations[1]);
            Assert.Equal("var Script.b", b.ToTestDisplayString());
            Assert.Equal(SymbolKind.Field, b.Kind);

            var c = model.GetDeclaredSymbol(designations[2]);
            Assert.Equal("var Script.c", c.ToTestDisplayString());
            Assert.Equal(SymbolKind.Field, c.Kind);

            var d = model.GetDeclaredSymbol(designations[3]);
            Assert.Equal("System.Int32 Script.d", d.ToTestDisplayString());
            Assert.Equal(SymbolKind.Field, d.Kind);

            var declarations = tree.GetCompilationUnitRoot().DescendantNodes().OfType<DeclarationExpressionSyntax>().ToArray();
            Assert.Equal(3, declarations.Count());

            Assert.Equal("var (a,b)", declarations[0].ToString());
            var typeInfo = model.GetTypeInfo(declarations[0]);
            Assert.Equal("(var a, var b)", typeInfo.Type.ToTestDisplayString());
            Assert.Equal(typeInfo.Type, typeInfo.ConvertedType);
            Assert.True(model.GetConversion(declarations[0]).IsIdentity);
            var symbolInfo = model.GetSymbolInfo(declarations[0]);
            Assert.Null(symbolInfo.Symbol);
            Assert.Empty(symbolInfo.CandidateSymbols);
            Assert.Equal(CandidateReason.None, symbolInfo.CandidateReason);
            typeInfo = model.GetTypeInfo(declarations[0].Type);
            Assert.Equal("(var a, var b)", typeInfo.Type.ToTestDisplayString());
            Assert.Equal("(var a, var b)", typeInfo.ConvertedType.ToTestDisplayString());
            Assert.True(model.GetConversion(declarations[0].Type).IsIdentity);
            symbolInfo = model.GetSymbolInfo(declarations[0].Type);
            Assert.Equal("(var a, var b)", symbolInfo.Symbol.ToTestDisplayString());
            Assert.Empty(symbolInfo.CandidateSymbols);
            Assert.Equal(CandidateReason.None, symbolInfo.CandidateReason);
            Assert.Null(model.GetAliasInfo(declarations[0].Type));

            Assert.Equal("var c", declarations[1].ToString());
            typeInfo = model.GetTypeInfo(declarations[1]);
            Assert.Equal("var", typeInfo.Type.ToTestDisplayString());
            Assert.Equal(SymbolKind.ErrorType, typeInfo.Type.Kind);
            Assert.Equal(typeInfo.Type, typeInfo.ConvertedType);
            Assert.True(model.GetConversion(declarations[1]).IsIdentity);
            Assert.Equal("var Script.c", model.GetSymbolInfo(declarations[1]).Symbol.ToTestDisplayString());
            typeInfo = model.GetTypeInfo(declarations[1].Type);
            Assert.Equal("var", typeInfo.Type.ToTestDisplayString());
            Assert.Equal(SymbolKind.ErrorType, typeInfo.Type.Kind);
            Assert.Equal(typeInfo.Type, typeInfo.ConvertedType);
            Assert.True(model.GetConversion(declarations[1].Type).IsIdentity);
            symbolInfo = model.GetSymbolInfo(declarations[1].Type);
            Assert.Null(symbolInfo.Symbol);
            Assert.Empty(symbolInfo.CandidateSymbols);
            Assert.Equal(CandidateReason.None, symbolInfo.CandidateReason);
            Assert.Null(model.GetAliasInfo(declarations[1].Type));

            Assert.Equal("int d", declarations[2].ToString());
            typeInfo = model.GetTypeInfo(declarations[2]);
            Assert.Equal("System.Int32", typeInfo.Type.ToTestDisplayString());
            Assert.Equal(typeInfo.Type, typeInfo.ConvertedType);
            Assert.True(model.GetConversion(declarations[2]).IsIdentity);
            Assert.Equal("System.Int32 Script.d", model.GetSymbolInfo(declarations[2]).Symbol.ToTestDisplayString());
            typeInfo = model.GetTypeInfo(declarations[2].Type);
            Assert.Equal("System.Int32", typeInfo.Type.ToTestDisplayString());
            Assert.Equal(typeInfo.Type, typeInfo.ConvertedType);
            Assert.True(model.GetConversion(declarations[2].Type).IsIdentity);
            symbolInfo = model.GetSymbolInfo(declarations[2].Type);
            Assert.Equal("System.Int32", symbolInfo.Symbol.ToTestDisplayString());
            Assert.Null(model.GetAliasInfo(declarations[2].Type));

            var tuple = tree.GetCompilationUnitRoot().DescendantNodes().OfType<TupleExpressionSyntax>().Single();
            typeInfo = model.GetTypeInfo(tuple);
            Assert.Equal("((var a, var b), var c, System.Int32 d)", typeInfo.Type.ToTestDisplayString());
            Assert.Equal(typeInfo.Type, typeInfo.ConvertedType);
            Assert.True(model.GetConversion(tuple).IsIdentity);
            symbolInfo = model.GetSymbolInfo(tuple);
            Assert.Null(symbolInfo.Symbol);
            Assert.Empty(symbolInfo.CandidateSymbols);
            Assert.Equal(CandidateReason.None, symbolInfo.CandidateReason);
        }

        [Fact, WorkItem(17572, "https://github.com/dotnet/roslyn/issues/17572")]
        public void StandAlone_03()
        {
            string source1 = @"
class C
{
    static void Main()
    {
        (var (_, _), var _, int _);
    }
}
";

            var comp1 = CreateCompilation(source1);
            comp1.VerifyDiagnostics(
                // (6,10): error CS8185: A declaration is not allowed in this context.
                //         (var (_, _), var _, int _);
                Diagnostic(ErrorCode.ERR_DeclarationExpressionNotPermitted, "var (_, _)"),
                // (6,22): error CS8185: A declaration is not allowed in this context.
                //         (var (_, _), var _, int _);
                Diagnostic(ErrorCode.ERR_DeclarationExpressionNotPermitted, "var _"),
                // (6,29): error CS8185: A declaration is not allowed in this context.
                //         (var (_, _), var _, int _);
                Diagnostic(ErrorCode.ERR_DeclarationExpressionNotPermitted, "int _").WithLocation(6, 29),
                // (6,9): error CS0201: Only assignment, call, increment, decrement, await, and new object expressions can be used as a statement
                //         (var (_, _), var _, int _);
                Diagnostic(ErrorCode.ERR_IllegalStatement, "(var (_, _), var _, int _)").WithLocation(6, 9)
                );

            StandAlone_03_VerifySemanticModel(comp1);

            string source2 = @"
class C
{
    static void Main()
    {
        (var (_, _), var _, int _) = D;
    }
}
";

            var comp2 = CreateCompilation(source2);

            StandAlone_03_VerifySemanticModel(comp2);
        }

        private static void StandAlone_03_VerifySemanticModel(CSharpCompilation comp)
        {
            var tree = comp.SyntaxTrees.First();
            var model = comp.GetSemanticModel(tree);
            int count = 0;
            foreach (var designation in tree.GetCompilationUnitRoot().DescendantNodes().OfType<DiscardDesignationSyntax>())
            {
                Assert.Null(model.GetDeclaredSymbol(designation));
                count++;
            }

            Assert.Equal(4, count);

            var declarations = tree.GetCompilationUnitRoot().DescendantNodes().OfType<DeclarationExpressionSyntax>().ToArray();
            Assert.Equal(3, declarations.Count());

            Assert.Equal("var (_, _)", declarations[0].ToString());
            var typeInfo = model.GetTypeInfo(declarations[0]);
            Assert.Equal("(var, var)", typeInfo.Type.ToTestDisplayString());
            Assert.Equal(typeInfo.Type, typeInfo.ConvertedType);
            Assert.True(model.GetConversion(declarations[0]).IsIdentity);
            var symbolInfo = model.GetSymbolInfo(declarations[0]);
            Assert.Null(symbolInfo.Symbol);
            Assert.Empty(symbolInfo.CandidateSymbols);
            Assert.Equal(CandidateReason.None, symbolInfo.CandidateReason);
            typeInfo = model.GetTypeInfo(declarations[0].Type);
            Assert.Equal("(var, var)", typeInfo.Type.ToTestDisplayString());
            Assert.Equal("(var, var)", typeInfo.ConvertedType.ToTestDisplayString());
            Assert.Equal(typeInfo.Type, typeInfo.ConvertedType);
            Assert.Equal(TypeKind.Struct, typeInfo.Type.TypeKind);
            Assert.Equal(TypeKind.Error, ((INamedTypeSymbol)typeInfo.Type).TypeArguments[0].TypeKind);
            Assert.Equal(TypeKind.Error, ((INamedTypeSymbol)typeInfo.Type).TypeArguments[1].TypeKind);
            Assert.True(model.GetConversion(declarations[0].Type).IsIdentity);
            symbolInfo = model.GetSymbolInfo(declarations[0].Type);
            Assert.Equal("(var, var)", symbolInfo.Symbol.ToTestDisplayString());
            Assert.Empty(symbolInfo.CandidateSymbols);
            Assert.Equal(CandidateReason.None, symbolInfo.CandidateReason);
            Assert.Null(model.GetAliasInfo(declarations[0].Type));

            Assert.Equal("var _", declarations[1].ToString());
            typeInfo = model.GetTypeInfo(declarations[1]);
            Assert.Equal("var", typeInfo.Type.ToTestDisplayString());
            Assert.Equal(SymbolKind.ErrorType, typeInfo.Type.Kind);
            Assert.Equal(typeInfo.Type, typeInfo.ConvertedType);
            Assert.True(model.GetConversion(declarations[1]).IsIdentity);
            symbolInfo = model.GetSymbolInfo(declarations[1]);
            Assert.Null(symbolInfo.Symbol);
            Assert.Empty(symbolInfo.CandidateSymbols);
            Assert.Equal(CandidateReason.None, symbolInfo.CandidateReason);
            typeInfo = model.GetTypeInfo(declarations[1].Type);
            Assert.Equal("var", typeInfo.Type.ToTestDisplayString());
            Assert.Equal(SymbolKind.ErrorType, typeInfo.Type.Kind);
            Assert.Equal(typeInfo.Type, typeInfo.ConvertedType);
            Assert.True(model.GetConversion(declarations[1].Type).IsIdentity);
            symbolInfo = model.GetSymbolInfo(declarations[1].Type);
            Assert.Null(symbolInfo.Symbol);
            Assert.Empty(symbolInfo.CandidateSymbols);
            Assert.Equal(CandidateReason.None, symbolInfo.CandidateReason);
            Assert.Null(model.GetAliasInfo(declarations[1].Type));

            Assert.Equal("int _", declarations[2].ToString());
            typeInfo = model.GetTypeInfo(declarations[2]);
            Assert.Equal("System.Int32", typeInfo.Type.ToTestDisplayString());
            Assert.Equal(typeInfo.Type, typeInfo.ConvertedType);
            Assert.True(model.GetConversion(declarations[2]).IsIdentity);
            symbolInfo = model.GetSymbolInfo(declarations[2]);
            Assert.Null(symbolInfo.Symbol);
            Assert.Empty(symbolInfo.CandidateSymbols);
            Assert.Equal(CandidateReason.None, symbolInfo.CandidateReason);
            typeInfo = model.GetTypeInfo(declarations[2].Type);
            Assert.Equal("System.Int32", typeInfo.Type.ToTestDisplayString());
            Assert.Equal(typeInfo.Type, typeInfo.ConvertedType);
            Assert.True(model.GetConversion(declarations[2].Type).IsIdentity);
            symbolInfo = model.GetSymbolInfo(declarations[2].Type);
            Assert.Equal("System.Int32", symbolInfo.Symbol.ToTestDisplayString());
            Assert.Null(model.GetAliasInfo(declarations[2].Type));

            var tuple = tree.GetCompilationUnitRoot().DescendantNodes().OfType<TupleExpressionSyntax>().Single();
            typeInfo = model.GetTypeInfo(tuple);
            Assert.Equal("((var, var), var, System.Int32)", typeInfo.Type.ToTestDisplayString());
            Assert.Equal(typeInfo.Type, typeInfo.ConvertedType);
            Assert.True(model.GetConversion(tuple).IsIdentity);
            symbolInfo = model.GetSymbolInfo(tuple);
            Assert.Null(symbolInfo.Symbol);
            Assert.Empty(symbolInfo.CandidateSymbols);
            Assert.Equal(CandidateReason.None, symbolInfo.CandidateReason);
        }

        [Fact, WorkItem(17572, "https://github.com/dotnet/roslyn/issues/17572")]
        public void StandAlone_04()
        {
            string source1 = @"
(var (_, _), var _, int _);
";

            var comp1 = CreateCompilation(source1, parseOptions: TestOptions.Script);
            comp1.VerifyDiagnostics(
                // (2,2): error CS8185: A declaration is not allowed in this context.
                // (var (_, _), var _, int _);
                Diagnostic(ErrorCode.ERR_DeclarationExpressionNotPermitted, "var (_, _)"),
                // (2,14): error CS8185: A declaration is not allowed in this context.
                // (var (_, _), var _, int _);
                Diagnostic(ErrorCode.ERR_DeclarationExpressionNotPermitted, "var _"),
                // (2,21): error CS8185: A declaration is not allowed in this context.
                // (var (_, _), var _, int _);
                Diagnostic(ErrorCode.ERR_DeclarationExpressionNotPermitted, "int _").WithLocation(2, 21),
                // (2,1): error CS0201: Only assignment, call, increment, decrement, await, and new object expressions can be used as a statement
                // (var (_, _), var _, int _);
                Diagnostic(ErrorCode.ERR_IllegalStatement, "(var (_, _), var _, int _)").WithLocation(2, 1)
                );

            StandAlone_03_VerifySemanticModel(comp1);

            string source2 = @"
(var (_, _), var _, int _) = D;
";

            var comp2 = CreateCompilation(source2, parseOptions: TestOptions.Script);

            StandAlone_03_VerifySemanticModel(comp2);
        }

        [Fact, WorkItem(17572, "https://github.com/dotnet/roslyn/issues/17572")]
        public void StandAlone_05()
        {
            string source1 = @"
using var = System.Int32;

class C
{
    static void Main()
    {
        (var (a,b), var c);
    }
}
";

            var comp1 = CreateCompilation(source1);

            StandAlone_05_VerifySemanticModel(comp1);

            string source2 = @"
using var = System.Int32;

class C
{
    static void Main()
    {
        (var (a,b), var c) = D;
    }
}
";

            var comp2 = CreateCompilation(source2);

            StandAlone_05_VerifySemanticModel(comp2);
        }

        private static void StandAlone_05_VerifySemanticModel(CSharpCompilation comp)
        {
            var tree = comp.SyntaxTrees.First();
            var model = comp.GetSemanticModel(tree);

            var declarations = tree.GetCompilationUnitRoot().DescendantNodes().OfType<DeclarationExpressionSyntax>().ToArray();
            Assert.Equal(2, declarations.Count());

            Assert.Equal("var (a,b)", declarations[0].ToString());
            var typeInfo = model.GetTypeInfo(declarations[0]);
            Assert.Equal("(System.Int32 a, System.Int32 b)", typeInfo.Type.ToTestDisplayString());
            Assert.Equal(typeInfo.Type, typeInfo.ConvertedType);
            Assert.True(model.GetConversion(declarations[0]).IsIdentity);
            var symbolInfo = model.GetSymbolInfo(declarations[0]);
            Assert.Null(symbolInfo.Symbol);
            Assert.Empty(symbolInfo.CandidateSymbols);
            Assert.Equal(CandidateReason.None, symbolInfo.CandidateReason);
            typeInfo = model.GetTypeInfo(declarations[0].Type);
            Assert.Equal("System.Int32", typeInfo.Type.ToTestDisplayString());
            Assert.Equal("System.Int32", typeInfo.ConvertedType.ToTestDisplayString());
            Assert.True(model.GetConversion(declarations[0].Type).IsIdentity);
            symbolInfo = model.GetSymbolInfo(declarations[0].Type);
            Assert.Equal("System.Int32", symbolInfo.Symbol.ToTestDisplayString());
            Assert.Empty(symbolInfo.CandidateSymbols);
            Assert.Equal(CandidateReason.None, symbolInfo.CandidateReason);
            Assert.Equal("var=System.Int32", model.GetAliasInfo(declarations[0].Type).ToTestDisplayString());

            Assert.Equal("var c", declarations[1].ToString());
            typeInfo = model.GetTypeInfo(declarations[1]);
            Assert.Equal("System.Int32", typeInfo.Type.ToTestDisplayString());
            Assert.Equal(typeInfo.Type, typeInfo.ConvertedType);
            Assert.True(model.GetConversion(declarations[1]).IsIdentity);
            Assert.Equal("System.Int32 c", model.GetSymbolInfo(declarations[1]).Symbol.ToTestDisplayString());
            typeInfo = model.GetTypeInfo(declarations[1].Type);
            Assert.Equal("System.Int32", typeInfo.Type.ToTestDisplayString());
            Assert.Equal(typeInfo.Type, typeInfo.ConvertedType);
            Assert.True(model.GetConversion(declarations[1].Type).IsIdentity);
            symbolInfo = model.GetSymbolInfo(declarations[1].Type);
            Assert.Equal("System.Int32", symbolInfo.Symbol.ToTestDisplayString());
            Assert.Equal("var=System.Int32", model.GetAliasInfo(declarations[1].Type).ToTestDisplayString());
        }

        [Fact]
        [WorkItem(23651, "https://github.com/dotnet/roslyn/issues/23651")]
        public void StandAlone_05_WithDuplicateNames()
        {
            string source1 = @"
using var = System.Int32;

class C
{
    static void Main()
    {
        (var (a, a), var c);
    }
}
";

            var comp1 = CreateCompilation(source1);

            var tree = comp1.SyntaxTrees.Single();
            var model = comp1.GetSemanticModel(tree);
            var nodes = tree.GetCompilationUnitRoot().DescendantNodes();

            var aa = nodes.OfType<DeclarationExpressionSyntax>().ElementAt(0);
            Assert.Equal("var (a, a)", aa.ToString());
            var aaType = model.GetTypeInfo(aa).Type.GetSymbol();
            Assert.True(aaType.TupleElementNames.IsDefault);
        }

        [Fact, WorkItem(17572, "https://github.com/dotnet/roslyn/issues/17572")]
        public void StandAlone_06()
        {
            string source1 = @"
using var = System.Int32;

(var (a,b), var c);
";

            var comp1 = CreateCompilation(source1, parseOptions: TestOptions.Script);

            StandAlone_06_VerifySemanticModel(comp1);

            string source2 = @"
using var = System.Int32;

(var (a,b), var c) = D;
";

            var comp2 = CreateCompilation(source2, parseOptions: TestOptions.Script);

            StandAlone_06_VerifySemanticModel(comp2);
        }

        private static void StandAlone_06_VerifySemanticModel(CSharpCompilation comp)
        {
            var tree = comp.SyntaxTrees.First();
            var model = comp.GetSemanticModel(tree);

            var declarations = tree.GetCompilationUnitRoot().DescendantNodes().OfType<DeclarationExpressionSyntax>().ToArray();
            Assert.Equal(2, declarations.Count());

            Assert.Equal("var (a,b)", declarations[0].ToString());
            var typeInfo = model.GetTypeInfo(declarations[0]);
            Assert.Equal("(System.Int32 a, System.Int32 b)", typeInfo.Type.ToTestDisplayString());
            Assert.Equal(typeInfo.Type, typeInfo.ConvertedType);
            Assert.True(model.GetConversion(declarations[0]).IsIdentity);
            var symbolInfo = model.GetSymbolInfo(declarations[0]);
            Assert.Null(symbolInfo.Symbol);
            Assert.Empty(symbolInfo.CandidateSymbols);
            Assert.Equal(CandidateReason.None, symbolInfo.CandidateReason);
            typeInfo = model.GetTypeInfo(declarations[0].Type);
            Assert.Equal("System.Int32", typeInfo.Type.ToTestDisplayString());
            Assert.Equal("System.Int32", typeInfo.ConvertedType.ToTestDisplayString());
            Assert.True(model.GetConversion(declarations[0].Type).IsIdentity);
            symbolInfo = model.GetSymbolInfo(declarations[0].Type);
            Assert.Equal("System.Int32", symbolInfo.Symbol.ToTestDisplayString());
            Assert.Empty(symbolInfo.CandidateSymbols);
            Assert.Equal(CandidateReason.None, symbolInfo.CandidateReason);
            Assert.Equal("var=System.Int32", model.GetAliasInfo(declarations[0].Type).ToTestDisplayString());

            Assert.Equal("var c", declarations[1].ToString());
            typeInfo = model.GetTypeInfo(declarations[1]);
            Assert.Equal("System.Int32", typeInfo.Type.ToTestDisplayString());
            Assert.Equal(typeInfo.Type, typeInfo.ConvertedType);
            Assert.True(model.GetConversion(declarations[1]).IsIdentity);
            Assert.Equal("System.Int32 Script.c", model.GetSymbolInfo(declarations[1]).Symbol.ToTestDisplayString());
            typeInfo = model.GetTypeInfo(declarations[1].Type);
            Assert.Equal("System.Int32", typeInfo.Type.ToTestDisplayString());
            Assert.Equal(typeInfo.Type, typeInfo.ConvertedType);
            Assert.True(model.GetConversion(declarations[1].Type).IsIdentity);
            symbolInfo = model.GetSymbolInfo(declarations[1].Type);
            Assert.Equal("System.Int32", symbolInfo.Symbol.ToTestDisplayString());
            Assert.Equal("var=System.Int32", model.GetAliasInfo(declarations[1].Type).ToTestDisplayString());
        }

        [Fact, WorkItem(17572, "https://github.com/dotnet/roslyn/issues/17572")]
        public void StandAlone_07()
        {
            string source1 = @"
using var = System.Int32;

class C
{
    static void Main()
    {
        (var (_, _), var _);
    }
}
";

            var comp1 = CreateCompilation(source1);

            StandAlone_07_VerifySemanticModel(comp1);

            string source2 = @"
using var = System.Int32;

class C
{
    static void Main()
    {
        (var (_, _), var _) = D;
    }
}
";

            var comp2 = CreateCompilation(source2);

            StandAlone_07_VerifySemanticModel(comp2);
        }

        private static void StandAlone_07_VerifySemanticModel(CSharpCompilation comp)
        {
            var tree = comp.SyntaxTrees.First();
            var model = comp.GetSemanticModel(tree);

            var declarations = tree.GetCompilationUnitRoot().DescendantNodes().OfType<DeclarationExpressionSyntax>().ToArray();
            Assert.Equal(2, declarations.Count());

            Assert.Equal("var (_, _)", declarations[0].ToString());
            var typeInfo = model.GetTypeInfo(declarations[0]);
            Assert.Equal("(System.Int32, System.Int32)", typeInfo.Type.ToTestDisplayString());
            Assert.Equal(typeInfo.Type, typeInfo.ConvertedType);
            Assert.True(model.GetConversion(declarations[0]).IsIdentity);
            var symbolInfo = model.GetSymbolInfo(declarations[0]);
            Assert.Null(symbolInfo.Symbol);
            Assert.Empty(symbolInfo.CandidateSymbols);
            Assert.Equal(CandidateReason.None, symbolInfo.CandidateReason);
            typeInfo = model.GetTypeInfo(declarations[0].Type);
            Assert.Equal("System.Int32", typeInfo.Type.ToTestDisplayString());
            Assert.Equal("System.Int32", typeInfo.ConvertedType.ToTestDisplayString());
            Assert.True(model.GetConversion(declarations[0].Type).IsIdentity);
            symbolInfo = model.GetSymbolInfo(declarations[0].Type);
            Assert.Equal("System.Int32", symbolInfo.Symbol.ToTestDisplayString());
            Assert.Empty(symbolInfo.CandidateSymbols);
            Assert.Equal(CandidateReason.None, symbolInfo.CandidateReason);
            Assert.Equal("var=System.Int32", model.GetAliasInfo(declarations[0].Type).ToTestDisplayString());

            Assert.Equal("var _", declarations[1].ToString());
            typeInfo = model.GetTypeInfo(declarations[1]);
            Assert.Equal("System.Int32", typeInfo.Type.ToTestDisplayString());
            Assert.Equal(typeInfo.Type, typeInfo.ConvertedType);
            Assert.True(model.GetConversion(declarations[1]).IsIdentity);
            symbolInfo = model.GetSymbolInfo(declarations[1]);
            Assert.Null(symbolInfo.Symbol);
            Assert.Empty(symbolInfo.CandidateSymbols);
            Assert.Equal(CandidateReason.None, symbolInfo.CandidateReason);
            typeInfo = model.GetTypeInfo(declarations[1].Type);
            Assert.Equal("System.Int32", typeInfo.Type.ToTestDisplayString());
            Assert.Equal(typeInfo.Type, typeInfo.ConvertedType);
            Assert.True(model.GetConversion(declarations[1].Type).IsIdentity);
            symbolInfo = model.GetSymbolInfo(declarations[1].Type);
            Assert.Equal("System.Int32", symbolInfo.Symbol.ToTestDisplayString());
            Assert.Equal("var=System.Int32", model.GetAliasInfo(declarations[1].Type).ToTestDisplayString());
        }

        [Fact, WorkItem(17572, "https://github.com/dotnet/roslyn/issues/17572")]
        public void StandAlone_08()
        {
            string source1 = @"
using var = System.Int32;

(var (_, _), var _);
";

            var comp1 = CreateCompilation(source1, parseOptions: TestOptions.Script);

            StandAlone_07_VerifySemanticModel(comp1);

            string source2 = @"
using var = System.Int32;

(var (_, _), var _) = D;
";

            var comp2 = CreateCompilation(source2, parseOptions: TestOptions.Script);

            StandAlone_07_VerifySemanticModel(comp2);
        }

        [Fact, WorkItem(17572, "https://github.com/dotnet/roslyn/issues/17572")]
        public void StandAlone_09()
        {
            string source1 = @"
using al = System.Int32;

class C
{
    static void Main()
    {
        (al (a,b), al c);
    }
}
";

            var comp1 = CreateCompilation(source1);

            StandAlone_09_VerifySemanticModel(comp1);

            string source2 = @"
using al = System.Int32;

class C
{
    static void Main()
    {
        (al (a,b), al c) = D;
    }
}
";

            var comp2 = CreateCompilation(source2);

            StandAlone_09_VerifySemanticModel(comp2);
        }

        private static void StandAlone_09_VerifySemanticModel(CSharpCompilation comp)
        {
            var tree = comp.SyntaxTrees.First();
            var model = comp.GetSemanticModel(tree);

            var declaration = tree.GetCompilationUnitRoot().DescendantNodes().OfType<DeclarationExpressionSyntax>().Single();

            Assert.Equal("al c", declaration.ToString());
            var typeInfo = model.GetTypeInfo(declaration);
            Assert.Equal("System.Int32", typeInfo.Type.ToTestDisplayString());
            Assert.Equal(typeInfo.Type, typeInfo.ConvertedType);
            Assert.True(model.GetConversion(declaration).IsIdentity);
            Assert.Equal("System.Int32 c", model.GetSymbolInfo(declaration).Symbol.ToTestDisplayString());
            typeInfo = model.GetTypeInfo(declaration.Type);
            Assert.Equal("System.Int32", typeInfo.Type.ToTestDisplayString());
            Assert.Equal(typeInfo.Type, typeInfo.ConvertedType);
            Assert.True(model.GetConversion(declaration.Type).IsIdentity);
            var symbolInfo = model.GetSymbolInfo(declaration.Type);
            Assert.Equal("System.Int32", symbolInfo.Symbol.ToTestDisplayString());
            Assert.Equal("al=System.Int32", model.GetAliasInfo(declaration.Type).ToTestDisplayString());
        }

        [Fact, WorkItem(17572, "https://github.com/dotnet/roslyn/issues/17572")]
        public void StandAlone_10()
        {
            string source1 = @"
using al = System.Int32;

(al (a,b), al c);
";

            var comp1 = CreateCompilation(source1, parseOptions: TestOptions.Script);

            StandAlone_10_VerifySemanticModel(comp1);

            string source2 = @"
using al = System.Int32;

(al (a,b), al c) = D;
";

            var comp2 = CreateCompilation(source2, parseOptions: TestOptions.Script);

            StandAlone_10_VerifySemanticModel(comp2);
        }

        private static void StandAlone_10_VerifySemanticModel(CSharpCompilation comp)
        {
            var tree = comp.SyntaxTrees.First();
            var model = comp.GetSemanticModel(tree);

            var declaration = tree.GetCompilationUnitRoot().DescendantNodes().OfType<DeclarationExpressionSyntax>().Single();

            Assert.Equal("al c", declaration.ToString());
            var typeInfo = model.GetTypeInfo(declaration);
            Assert.Equal("System.Int32", typeInfo.Type.ToTestDisplayString());
            Assert.Equal(typeInfo.Type, typeInfo.ConvertedType);
            Assert.True(model.GetConversion(declaration).IsIdentity);
            Assert.Equal("System.Int32 Script.c", model.GetSymbolInfo(declaration).Symbol.ToTestDisplayString());
            typeInfo = model.GetTypeInfo(declaration.Type);
            Assert.Equal("System.Int32", typeInfo.Type.ToTestDisplayString());
            Assert.Equal(typeInfo.Type, typeInfo.ConvertedType);
            Assert.True(model.GetConversion(declaration.Type).IsIdentity);
            var symbolInfo = model.GetSymbolInfo(declaration.Type);
            Assert.Equal("System.Int32", symbolInfo.Symbol.ToTestDisplayString());
            Assert.Equal("al=System.Int32", model.GetAliasInfo(declaration.Type).ToTestDisplayString());
        }

        [Fact, WorkItem(17572, "https://github.com/dotnet/roslyn/issues/17572")]
        public void StandAlone_11()
        {
            string source1 = @"
using al = System.Int32;

class C
{
    static void Main()
    {
        (al (_, _), al _);
    }
}
";

            var comp1 = CreateCompilation(source1);

            StandAlone_11_VerifySemanticModel(comp1);

            string source2 = @"
using al = System.Int32;

class C
{
    static void Main()
    {
        (al (_, _), al _) = D;
    }
}
";

            var comp2 = CreateCompilation(source2);

            StandAlone_11_VerifySemanticModel(comp2);
        }

        private static void StandAlone_11_VerifySemanticModel(CSharpCompilation comp)
        {
            var tree = comp.SyntaxTrees.First();
            var model = comp.GetSemanticModel(tree);

            var declaration = tree.GetCompilationUnitRoot().DescendantNodes().OfType<DeclarationExpressionSyntax>().Single();

            Assert.Equal("al _", declaration.ToString());
            var typeInfo = model.GetTypeInfo(declaration);
            Assert.Equal("System.Int32", typeInfo.Type.ToTestDisplayString());
            Assert.Equal(typeInfo.Type, typeInfo.ConvertedType);
            Assert.True(model.GetConversion(declaration).IsIdentity);
            var symbolInfo = model.GetSymbolInfo(declaration);
            Assert.Null(symbolInfo.Symbol);
            Assert.Empty(symbolInfo.CandidateSymbols);
            Assert.Equal(CandidateReason.None, symbolInfo.CandidateReason);
            typeInfo = model.GetTypeInfo(declaration.Type);
            Assert.Equal("System.Int32", typeInfo.Type.ToTestDisplayString());
            Assert.Equal(typeInfo.Type, typeInfo.ConvertedType);
            Assert.True(model.GetConversion(declaration.Type).IsIdentity);
            symbolInfo = model.GetSymbolInfo(declaration.Type);
            Assert.Equal("System.Int32", symbolInfo.Symbol.ToTestDisplayString());
            Assert.Equal("al=System.Int32", model.GetAliasInfo(declaration.Type).ToTestDisplayString());
        }

        [Fact, WorkItem(17572, "https://github.com/dotnet/roslyn/issues/17572")]
        public void StandAlone_12()
        {
            string source1 = @"
using al = System.Int32;

(al (_, _), al _);
";

            var comp1 = CreateCompilation(source1, parseOptions: TestOptions.Script);

            StandAlone_11_VerifySemanticModel(comp1);

            string source2 = @"
using al = System.Int32;

(al (_, _), al _) = D;
";

            var comp2 = CreateCompilation(source2, parseOptions: TestOptions.Script);

            StandAlone_11_VerifySemanticModel(comp2);
        }

        [Fact, WorkItem(17572, "https://github.com/dotnet/roslyn/issues/17572")]
        public void StandAlone_13()
        {
            string source1 = @"
class C
{
    static void Main()
    {
        var (a, b);
        var (c, d)
    }
}
";

            var comp1 = CreateCompilation(source1);
            comp1.VerifyDiagnostics(
                // (7,19): error CS1002: ; expected
                //         var (c, d)
                Diagnostic(ErrorCode.ERR_SemicolonExpected, "").WithLocation(7, 19),
                // (6,9): error CS0103: The name 'var' does not exist in the current context
                //         var (a, b);
                Diagnostic(ErrorCode.ERR_NameNotInContext, "var").WithArguments("var").WithLocation(6, 9),
                // (6,14): error CS0103: The name 'a' does not exist in the current context
                //         var (a, b);
                Diagnostic(ErrorCode.ERR_NameNotInContext, "a").WithArguments("a").WithLocation(6, 14),
                // (6,17): error CS0103: The name 'b' does not exist in the current context
                //         var (a, b);
                Diagnostic(ErrorCode.ERR_NameNotInContext, "b").WithArguments("b").WithLocation(6, 17),
                // (7,9): error CS0103: The name 'var' does not exist in the current context
                //         var (c, d)
                Diagnostic(ErrorCode.ERR_NameNotInContext, "var").WithArguments("var").WithLocation(7, 9),
                // (7,14): error CS0103: The name 'c' does not exist in the current context
                //         var (c, d)
                Diagnostic(ErrorCode.ERR_NameNotInContext, "c").WithArguments("c").WithLocation(7, 14),
                // (7,17): error CS0103: The name 'd' does not exist in the current context
                //         var (c, d)
                Diagnostic(ErrorCode.ERR_NameNotInContext, "d").WithArguments("d").WithLocation(7, 17)
                );

            var tree = comp1.SyntaxTrees.First();
            Assert.False(tree.GetCompilationUnitRoot().DescendantNodes().OfType<DeclarationExpressionSyntax>().Any());
        }

        [Fact, WorkItem(17572, "https://github.com/dotnet/roslyn/issues/17572")]
        public void StandAlone_14()
        {
            string source1 = @"
class C
{
    static void Main()
    {
        ((var (a,b), var c), int d);
    }
}
";

            var comp1 = CreateCompilation(source1);
            comp1.VerifyDiagnostics(
                // (6,11): error CS8185: A declaration is not allowed in this context.
                //         ((var (a,b), var c), int d);
                Diagnostic(ErrorCode.ERR_DeclarationExpressionNotPermitted, "var (a,b)").WithLocation(6, 11),
                // (6,22): error CS8185: A declaration is not allowed in this context.
                //         ((var (a,b), var c), int d);
                Diagnostic(ErrorCode.ERR_DeclarationExpressionNotPermitted, "var c").WithLocation(6, 22),
                // (6,30): error CS8185: A declaration is not allowed in this context.
                //         ((var (a,b), var c), int d);
                Diagnostic(ErrorCode.ERR_DeclarationExpressionNotPermitted, "int d").WithLocation(6, 30),
                // (6,9): error CS0201: Only assignment, call, increment, decrement, await, and new object expressions can be used as a statement
                //         ((var (a,b), var c), int d);
                Diagnostic(ErrorCode.ERR_IllegalStatement, "((var (a,b), var c), int d)").WithLocation(6, 9),
                // (6,30): error CS0165: Use of unassigned local variable 'd'
                //         ((var (a,b), var c), int d);
                Diagnostic(ErrorCode.ERR_UseDefViolation, "int d").WithArguments("d").WithLocation(6, 30)
                );

            StandAlone_14_VerifySemanticModel(comp1, LocalDeclarationKind.DeclarationExpressionVariable);

            string source2 = @"
class C
{
    static void Main()
    {
        ((var (a,b), var c), int d) = D;
    }
}
";

            var comp2 = CreateCompilation(source2);

            StandAlone_14_VerifySemanticModel(comp2, LocalDeclarationKind.DeconstructionVariable);
        }

        private static void StandAlone_14_VerifySemanticModel(CSharpCompilation comp, LocalDeclarationKind localDeclarationKind)
        {
            var tree = comp.SyntaxTrees.First();
            var model = comp.GetSemanticModel(tree);
            var designations = tree.GetCompilationUnitRoot().DescendantNodes().OfType<SingleVariableDesignationSyntax>().ToArray();
            Assert.Equal(4, designations.Count());

            var a = model.GetDeclaredSymbol(designations[0]);
            Assert.Equal("var a", a.ToTestDisplayString());
            Assert.Equal(localDeclarationKind, a.GetSymbol<LocalSymbol>().DeclarationKind);

            var b = model.GetDeclaredSymbol(designations[1]);
            Assert.Equal("var b", b.ToTestDisplayString());
            Assert.Equal(localDeclarationKind, b.GetSymbol<LocalSymbol>().DeclarationKind);

            var c = model.GetDeclaredSymbol(designations[2]);
            Assert.Equal("var c", c.ToTestDisplayString());
            Assert.Equal(localDeclarationKind, c.GetSymbol<LocalSymbol>().DeclarationKind);

            var d = model.GetDeclaredSymbol(designations[3]);
            Assert.Equal("System.Int32 d", d.ToTestDisplayString());
            Assert.Equal(localDeclarationKind, d.GetSymbol<LocalSymbol>().DeclarationKind);

            var declarations = tree.GetCompilationUnitRoot().DescendantNodes().OfType<DeclarationExpressionSyntax>().ToArray();
            Assert.Equal(3, declarations.Count());

            Assert.Equal("var (a,b)", declarations[0].ToString());
            var typeInfo = model.GetTypeInfo(declarations[0]);
            Assert.Equal("(var a, var b)", typeInfo.Type.ToTestDisplayString());
            Assert.Equal(typeInfo.Type, typeInfo.ConvertedType);
            Assert.True(model.GetConversion(declarations[0]).IsIdentity);
            var symbolInfo = model.GetSymbolInfo(declarations[0]);
            Assert.Null(symbolInfo.Symbol);
            Assert.Empty(symbolInfo.CandidateSymbols);
            Assert.Equal(CandidateReason.None, symbolInfo.CandidateReason);
            typeInfo = model.GetTypeInfo(declarations[0].Type);
            Assert.Equal("(var a, var b)", typeInfo.Type.ToTestDisplayString());
            Assert.Equal("(var a, var b)", typeInfo.ConvertedType.ToTestDisplayString());
            Assert.True(model.GetConversion(declarations[0].Type).IsIdentity);
            symbolInfo = model.GetSymbolInfo(declarations[0].Type);
            Assert.Equal("(var a, var b)", symbolInfo.Symbol.ToTestDisplayString());
            Assert.Empty(symbolInfo.CandidateSymbols);
            Assert.Equal(CandidateReason.None, symbolInfo.CandidateReason);
            Assert.Null(model.GetAliasInfo(declarations[0].Type));

            Assert.Equal("var c", declarations[1].ToString());
            typeInfo = model.GetTypeInfo(declarations[1]);
            Assert.Equal("var", typeInfo.Type.ToTestDisplayString());
            Assert.Equal(SymbolKind.ErrorType, typeInfo.Type.Kind);
            Assert.Equal(typeInfo.Type, typeInfo.ConvertedType);
            Assert.True(model.GetConversion(declarations[1]).IsIdentity);
            Assert.Equal("var c", model.GetSymbolInfo(declarations[1]).Symbol.ToTestDisplayString());
            typeInfo = model.GetTypeInfo(declarations[1].Type);
            Assert.Equal("var", typeInfo.Type.ToTestDisplayString());
            Assert.Equal(SymbolKind.ErrorType, typeInfo.Type.Kind);
            Assert.Equal(typeInfo.Type, typeInfo.ConvertedType);
            Assert.True(model.GetConversion(declarations[1].Type).IsIdentity);
            symbolInfo = model.GetSymbolInfo(declarations[1].Type);
            Assert.Null(symbolInfo.Symbol);
            Assert.Empty(symbolInfo.CandidateSymbols);
            Assert.Equal(CandidateReason.None, symbolInfo.CandidateReason);
            Assert.Null(model.GetAliasInfo(declarations[1].Type));

            Assert.Equal("int d", declarations[2].ToString());
            typeInfo = model.GetTypeInfo(declarations[2]);
            Assert.Equal("System.Int32", typeInfo.Type.ToTestDisplayString());
            Assert.Equal(typeInfo.Type, typeInfo.ConvertedType);
            Assert.True(model.GetConversion(declarations[2]).IsIdentity);
            Assert.Equal("System.Int32 d", model.GetSymbolInfo(declarations[2]).Symbol.ToTestDisplayString());
            typeInfo = model.GetTypeInfo(declarations[2].Type);
            Assert.Equal("System.Int32", typeInfo.Type.ToTestDisplayString());
            Assert.Equal(typeInfo.Type, typeInfo.ConvertedType);
            Assert.True(model.GetConversion(declarations[2].Type).IsIdentity);
            symbolInfo = model.GetSymbolInfo(declarations[2].Type);
            Assert.Equal("System.Int32", symbolInfo.Symbol.ToTestDisplayString());
            Assert.Null(model.GetAliasInfo(declarations[2].Type));

            var tuples = tree.GetCompilationUnitRoot().DescendantNodes().OfType<TupleExpressionSyntax>().ToArray();
            Assert.Equal(2, tuples.Length);

            Assert.Equal("((var (a,b), var c), int d)", tuples[0].ToString());
            typeInfo = model.GetTypeInfo(tuples[0]);
            Assert.Equal("(((var a, var b), var c), System.Int32 d)", typeInfo.Type.ToTestDisplayString());
            Assert.Equal(typeInfo.Type, typeInfo.ConvertedType);
            Assert.True(model.GetConversion(tuples[0]).IsIdentity);
            symbolInfo = model.GetSymbolInfo(tuples[0]);
            Assert.Null(symbolInfo.Symbol);
            Assert.Empty(symbolInfo.CandidateSymbols);
            Assert.Equal(CandidateReason.None, symbolInfo.CandidateReason);

            Assert.Equal("(var (a,b), var c)", tuples[1].ToString());
            typeInfo = model.GetTypeInfo(tuples[1]);
            Assert.Equal("((var a, var b), var c)", typeInfo.Type.ToTestDisplayString());
            Assert.Equal(typeInfo.Type, typeInfo.ConvertedType);
            Assert.True(model.GetConversion(tuples[1]).IsIdentity);
            symbolInfo = model.GetSymbolInfo(tuples[1]);
            Assert.Null(symbolInfo.Symbol);
            Assert.Empty(symbolInfo.CandidateSymbols);
            Assert.Equal(CandidateReason.None, symbolInfo.CandidateReason);
        }

        [Fact, WorkItem(17572, "https://github.com/dotnet/roslyn/issues/17572")]
        public void StandAlone_15()
        {
            string source1 = @"
((var (a,b), var c), int d);
";

            var comp1 = CreateCompilation(source1, parseOptions: TestOptions.Script);
            comp1.VerifyDiagnostics(
                // (2,8): error CS7019: Type of 'a' cannot be inferred since its initializer directly or indirectly refers to the definition.
                // ((var (a,b), var c), int d);
                Diagnostic(ErrorCode.ERR_RecursivelyTypedVariable, "a").WithArguments("a").WithLocation(2, 8),
                // (2,10): error CS7019: Type of 'b' cannot be inferred since its initializer directly or indirectly refers to the definition.
                // ((var (a,b), var c), int d);
                Diagnostic(ErrorCode.ERR_RecursivelyTypedVariable, "b").WithArguments("b").WithLocation(2, 10),
                // (2,18): error CS7019: Type of 'c' cannot be inferred since its initializer directly or indirectly refers to the definition.
                // ((var (a,b), var c), int d);
                Diagnostic(ErrorCode.ERR_RecursivelyTypedVariable, "c").WithArguments("c").WithLocation(2, 18),
                // (2,3): error CS8185: A declaration is not allowed in this context.
                // ((var (a,b), var c), int d);
                Diagnostic(ErrorCode.ERR_DeclarationExpressionNotPermitted, "var (a,b)").WithLocation(2, 3),
                // (2,14): error CS8185: A declaration is not allowed in this context.
                // ((var (a,b), var c), int d);
                Diagnostic(ErrorCode.ERR_DeclarationExpressionNotPermitted, "var c").WithLocation(2, 14),
                // (2,22): error CS8185: A declaration is not allowed in this context.
                // ((var (a,b), var c), int d);
                Diagnostic(ErrorCode.ERR_DeclarationExpressionNotPermitted, "int d").WithLocation(2, 22),
                // (2,1): error CS0201: Only assignment, call, increment, decrement, await, and new object expressions can be used as a statement
                // ((var (a,b), var c), int d);
                Diagnostic(ErrorCode.ERR_IllegalStatement, "((var (a,b), var c), int d)").WithLocation(2, 1)
                );

            StandAlone_15_VerifySemanticModel(comp1);

            string source2 = @"
((var (a,b), var c), int d) = D;
";

            var comp2 = CreateCompilation(source2, parseOptions: TestOptions.Script);

            StandAlone_15_VerifySemanticModel(comp2);
        }

        private static void StandAlone_15_VerifySemanticModel(CSharpCompilation comp)
        {
            var tree = comp.SyntaxTrees.First();
            var model = comp.GetSemanticModel(tree);
            var designations = tree.GetCompilationUnitRoot().DescendantNodes().OfType<SingleVariableDesignationSyntax>().ToArray();
            Assert.Equal(4, designations.Count());

            var a = model.GetDeclaredSymbol(designations[0]);
            Assert.Equal("var Script.a", a.ToTestDisplayString());
            Assert.Equal(SymbolKind.Field, a.Kind);

            var b = model.GetDeclaredSymbol(designations[1]);
            Assert.Equal("var Script.b", b.ToTestDisplayString());
            Assert.Equal(SymbolKind.Field, b.Kind);

            var c = model.GetDeclaredSymbol(designations[2]);
            Assert.Equal("var Script.c", c.ToTestDisplayString());
            Assert.Equal(SymbolKind.Field, c.Kind);

            var d = model.GetDeclaredSymbol(designations[3]);
            Assert.Equal("System.Int32 Script.d", d.ToTestDisplayString());
            Assert.Equal(SymbolKind.Field, d.Kind);

            var declarations = tree.GetCompilationUnitRoot().DescendantNodes().OfType<DeclarationExpressionSyntax>().ToArray();
            Assert.Equal(3, declarations.Count());

            Assert.Equal("var (a,b)", declarations[0].ToString());
            var typeInfo = model.GetTypeInfo(declarations[0]);
            Assert.Equal("(var a, var b)", typeInfo.Type.ToTestDisplayString());
            Assert.Equal(typeInfo.Type, typeInfo.ConvertedType);
            Assert.True(model.GetConversion(declarations[0]).IsIdentity);
            var symbolInfo = model.GetSymbolInfo(declarations[0]);
            Assert.Null(symbolInfo.Symbol);
            Assert.Empty(symbolInfo.CandidateSymbols);
            Assert.Equal(CandidateReason.None, symbolInfo.CandidateReason);
            typeInfo = model.GetTypeInfo(declarations[0].Type);
            Assert.Equal("(var a, var b)", typeInfo.Type.ToTestDisplayString());
            Assert.Equal("(var a, var b)", typeInfo.ConvertedType.ToTestDisplayString());
            Assert.True(model.GetConversion(declarations[0].Type).IsIdentity);
            symbolInfo = model.GetSymbolInfo(declarations[0].Type);
            Assert.Equal("(var a, var b)", symbolInfo.Symbol.ToTestDisplayString());
            Assert.Empty(symbolInfo.CandidateSymbols);
            Assert.Equal(CandidateReason.None, symbolInfo.CandidateReason);
            Assert.Null(model.GetAliasInfo(declarations[0].Type));

            Assert.Equal("var c", declarations[1].ToString());
            typeInfo = model.GetTypeInfo(declarations[1]);
            Assert.Equal("var", typeInfo.Type.ToTestDisplayString());
            Assert.Equal(SymbolKind.ErrorType, typeInfo.Type.Kind);
            Assert.Equal(typeInfo.Type, typeInfo.ConvertedType);
            Assert.True(model.GetConversion(declarations[1]).IsIdentity);
            Assert.Equal("var Script.c", model.GetSymbolInfo(declarations[1]).Symbol.ToTestDisplayString());
            typeInfo = model.GetTypeInfo(declarations[1].Type);
            Assert.Equal("var", typeInfo.Type.ToTestDisplayString());
            Assert.Equal(SymbolKind.ErrorType, typeInfo.Type.Kind);
            Assert.Equal(typeInfo.Type, typeInfo.ConvertedType);
            Assert.True(model.GetConversion(declarations[1].Type).IsIdentity);
            symbolInfo = model.GetSymbolInfo(declarations[1].Type);
            Assert.Null(symbolInfo.Symbol);
            Assert.Empty(symbolInfo.CandidateSymbols);
            Assert.Equal(CandidateReason.None, symbolInfo.CandidateReason);
            Assert.Null(model.GetAliasInfo(declarations[1].Type));

            Assert.Equal("int d", declarations[2].ToString());
            typeInfo = model.GetTypeInfo(declarations[2]);
            Assert.Equal("System.Int32", typeInfo.Type.ToTestDisplayString());
            Assert.Equal(typeInfo.Type, typeInfo.ConvertedType);
            Assert.True(model.GetConversion(declarations[2]).IsIdentity);
            Assert.Equal("System.Int32 Script.d", model.GetSymbolInfo(declarations[2]).Symbol.ToTestDisplayString());
            typeInfo = model.GetTypeInfo(declarations[2].Type);
            Assert.Equal("System.Int32", typeInfo.Type.ToTestDisplayString());
            Assert.Equal(typeInfo.Type, typeInfo.ConvertedType);
            Assert.True(model.GetConversion(declarations[2].Type).IsIdentity);
            symbolInfo = model.GetSymbolInfo(declarations[2].Type);
            Assert.Equal("System.Int32", symbolInfo.Symbol.ToTestDisplayString());
            Assert.Null(model.GetAliasInfo(declarations[2].Type));

            var tuples = tree.GetCompilationUnitRoot().DescendantNodes().OfType<TupleExpressionSyntax>().ToArray();
            Assert.Equal(2, tuples.Length);

            Assert.Equal("((var (a,b), var c), int d)", tuples[0].ToString());
            typeInfo = model.GetTypeInfo(tuples[0]);
            Assert.Equal("(((var a, var b), var c), System.Int32 d)", typeInfo.Type.ToTestDisplayString());
            Assert.Equal(typeInfo.Type, typeInfo.ConvertedType);
            Assert.True(model.GetConversion(tuples[0]).IsIdentity);
            symbolInfo = model.GetSymbolInfo(tuples[0]);
            Assert.Null(symbolInfo.Symbol);
            Assert.Empty(symbolInfo.CandidateSymbols);
            Assert.Equal(CandidateReason.None, symbolInfo.CandidateReason);

            Assert.Equal("(var (a,b), var c)", tuples[1].ToString());
            typeInfo = model.GetTypeInfo(tuples[1]);
            Assert.Equal("((var a, var b), var c)", typeInfo.Type.ToTestDisplayString());
            Assert.Equal(typeInfo.Type, typeInfo.ConvertedType);
            Assert.True(model.GetConversion(tuples[1]).IsIdentity);
            symbolInfo = model.GetSymbolInfo(tuples[1]);
            Assert.Null(symbolInfo.Symbol);
            Assert.Empty(symbolInfo.CandidateSymbols);
            Assert.Equal(CandidateReason.None, symbolInfo.CandidateReason);
        }

        [Fact, WorkItem(17572, "https://github.com/dotnet/roslyn/issues/17572")]
        public void StandAlone_16()
        {
            string source1 = @"
class C
{
    static void Main()
    {
        ((var (_, _), var _), int _);
    }
}
";

            var comp1 = CreateCompilation(source1);
            comp1.VerifyDiagnostics(
                // (6,11): error CS8185: A declaration is not allowed in this context.
                //         ((var (_, _), var _), int _);
                Diagnostic(ErrorCode.ERR_DeclarationExpressionNotPermitted, "var (_, _)").WithLocation(6, 11),
                // (6,23): error CS8185: A declaration is not allowed in this context.
                //         ((var (_, _), var _), int _);
                Diagnostic(ErrorCode.ERR_DeclarationExpressionNotPermitted, "var _").WithLocation(6, 23),
                // (6,31): error CS8185: A declaration is not allowed in this context.
                //         ((var (_, _), var _), int _);
                Diagnostic(ErrorCode.ERR_DeclarationExpressionNotPermitted, "int _").WithLocation(6, 31),
                // (6,9): error CS0201: Only assignment, call, increment, decrement, await, and new object expressions can be used as a statement
                //         ((var (_, _), var _), int _);
                Diagnostic(ErrorCode.ERR_IllegalStatement, "((var (_, _), var _), int _)").WithLocation(6, 9)
                );

            StandAlone_16_VerifySemanticModel(comp1);

            string source2 = @"
class C
{
    static void Main()
    {
        ((var (_, _), var _), int _) = D;
    }
}
";

            var comp2 = CreateCompilation(source2);

            StandAlone_16_VerifySemanticModel(comp2);
        }

        private static void StandAlone_16_VerifySemanticModel(CSharpCompilation comp)
        {
            var tree = comp.SyntaxTrees.First();
            var model = comp.GetSemanticModel(tree);
            int count = 0;
            foreach (var designation in tree.GetCompilationUnitRoot().DescendantNodes().OfType<DiscardDesignationSyntax>())
            {
                Assert.Null(model.GetDeclaredSymbol(designation));
                count++;
            }

            Assert.Equal(4, count);

            var declarations = tree.GetCompilationUnitRoot().DescendantNodes().OfType<DeclarationExpressionSyntax>().ToArray();
            Assert.Equal(3, declarations.Count());

            Assert.Equal("var (_, _)", declarations[0].ToString());
            var typeInfo = model.GetTypeInfo(declarations[0]);
            Assert.Equal("(var, var)", typeInfo.Type.ToTestDisplayString());
            Assert.Equal(typeInfo.Type, typeInfo.ConvertedType);
            Assert.True(model.GetConversion(declarations[0]).IsIdentity);
            var symbolInfo = model.GetSymbolInfo(declarations[0]);
            Assert.Null(symbolInfo.Symbol);
            Assert.Empty(symbolInfo.CandidateSymbols);
            Assert.Equal(CandidateReason.None, symbolInfo.CandidateReason);
            typeInfo = model.GetTypeInfo(declarations[0].Type);
            Assert.Equal("(var, var)", typeInfo.Type.ToTestDisplayString());
            Assert.Equal("(var, var)", typeInfo.ConvertedType.ToTestDisplayString());
            Assert.True(model.GetConversion(declarations[0].Type).IsIdentity);
            symbolInfo = model.GetSymbolInfo(declarations[0].Type);
            Assert.Equal("(var, var)", symbolInfo.Symbol.ToTestDisplayString());
            Assert.Empty(symbolInfo.CandidateSymbols);
            Assert.Equal(CandidateReason.None, symbolInfo.CandidateReason);
            Assert.Null(model.GetAliasInfo(declarations[0].Type));

            Assert.Equal("var _", declarations[1].ToString());
            typeInfo = model.GetTypeInfo(declarations[1]);
            Assert.Equal("var", typeInfo.Type.ToTestDisplayString());
            Assert.Equal(SymbolKind.ErrorType, typeInfo.Type.Kind);
            Assert.Equal(typeInfo.Type, typeInfo.ConvertedType);
            Assert.True(model.GetConversion(declarations[1]).IsIdentity);
            symbolInfo = model.GetSymbolInfo(declarations[1]);
            Assert.Null(symbolInfo.Symbol);
            Assert.Empty(symbolInfo.CandidateSymbols);
            Assert.Equal(CandidateReason.None, symbolInfo.CandidateReason);
            typeInfo = model.GetTypeInfo(declarations[1].Type);
            Assert.Equal("var", typeInfo.Type.ToTestDisplayString());
            Assert.Equal(SymbolKind.ErrorType, typeInfo.Type.Kind);
            Assert.Equal(typeInfo.Type, typeInfo.ConvertedType);
            Assert.True(model.GetConversion(declarations[1].Type).IsIdentity);
            symbolInfo = model.GetSymbolInfo(declarations[1].Type);
            Assert.Null(symbolInfo.Symbol);
            Assert.Empty(symbolInfo.CandidateSymbols);
            Assert.Equal(CandidateReason.None, symbolInfo.CandidateReason);
            Assert.Null(model.GetAliasInfo(declarations[1].Type));

            Assert.Equal("int _", declarations[2].ToString());
            typeInfo = model.GetTypeInfo(declarations[2]);
            Assert.Equal("System.Int32", typeInfo.Type.ToTestDisplayString());
            Assert.Equal(typeInfo.Type, typeInfo.ConvertedType);
            Assert.True(model.GetConversion(declarations[2]).IsIdentity);
            symbolInfo = model.GetSymbolInfo(declarations[2]);
            Assert.Null(symbolInfo.Symbol);
            Assert.Empty(symbolInfo.CandidateSymbols);
            Assert.Equal(CandidateReason.None, symbolInfo.CandidateReason);
            typeInfo = model.GetTypeInfo(declarations[2].Type);
            Assert.Equal("System.Int32", typeInfo.Type.ToTestDisplayString());
            Assert.Equal(typeInfo.Type, typeInfo.ConvertedType);
            Assert.True(model.GetConversion(declarations[2].Type).IsIdentity);
            symbolInfo = model.GetSymbolInfo(declarations[2].Type);
            Assert.Equal("System.Int32", symbolInfo.Symbol.ToTestDisplayString());
            Assert.Null(model.GetAliasInfo(declarations[2].Type));

            var tuples = tree.GetCompilationUnitRoot().DescendantNodes().OfType<TupleExpressionSyntax>().ToArray();
            Assert.Equal(2, tuples.Length);

            Assert.Equal("((var (_, _), var _), int _)", tuples[0].ToString());
            typeInfo = model.GetTypeInfo(tuples[0]);
            Assert.Equal("(((var, var), var), System.Int32)", typeInfo.Type.ToTestDisplayString());
            Assert.Equal(typeInfo.Type, typeInfo.ConvertedType);
            Assert.True(model.GetConversion(tuples[0]).IsIdentity);
            symbolInfo = model.GetSymbolInfo(tuples[0]);
            Assert.Null(symbolInfo.Symbol);
            Assert.Empty(symbolInfo.CandidateSymbols);
            Assert.Equal(CandidateReason.None, symbolInfo.CandidateReason);

            Assert.Equal("(var (_, _), var _)", tuples[1].ToString());
            typeInfo = model.GetTypeInfo(tuples[1]);
            Assert.Equal("((var, var), var)", typeInfo.Type.ToTestDisplayString());
            Assert.Equal(typeInfo.Type, typeInfo.ConvertedType);
            Assert.True(model.GetConversion(tuples[1]).IsIdentity);
            symbolInfo = model.GetSymbolInfo(tuples[1]);
            Assert.Null(symbolInfo.Symbol);
            Assert.Empty(symbolInfo.CandidateSymbols);
            Assert.Equal(CandidateReason.None, symbolInfo.CandidateReason);
        }

        [Fact, WorkItem(17572, "https://github.com/dotnet/roslyn/issues/17572")]
        public void StandAlone_17()
        {
            string source1 = @"
((var (_, _), var _), int _);
";

            var comp1 = CreateCompilation(source1, parseOptions: TestOptions.Script);
            comp1.VerifyDiagnostics(
                // (2,3): error CS8185: A declaration is not allowed in this context.
                // ((var (_, _), var _), int _);
                Diagnostic(ErrorCode.ERR_DeclarationExpressionNotPermitted, "var (_, _)").WithLocation(2, 3),
                // (2,15): error CS8185: A declaration is not allowed in this context.
                // ((var (_, _), var _), int _);
                Diagnostic(ErrorCode.ERR_DeclarationExpressionNotPermitted, "var _").WithLocation(2, 15),
                // (2,23): error CS8185: A declaration is not allowed in this context.
                // ((var (_, _), var _), int _);
                Diagnostic(ErrorCode.ERR_DeclarationExpressionNotPermitted, "int _").WithLocation(2, 23),
                // (2,1): error CS0201: Only assignment, call, increment, decrement, await, and new object expressions can be used as a statement
                // ((var (_, _), var _), int _);
                Diagnostic(ErrorCode.ERR_IllegalStatement, "((var (_, _), var _), int _)").WithLocation(2, 1)
                );

            StandAlone_16_VerifySemanticModel(comp1);

            string source2 = @"
((var (_, _), var _), int _) = D;
";

            var comp2 = CreateCompilation(source2, parseOptions: TestOptions.Script);

            StandAlone_16_VerifySemanticModel(comp2);
        }

        [Fact, WorkItem(17572, "https://github.com/dotnet/roslyn/issues/17572")]
        public void StandAlone_18()
        {
            string source1 = @"
class C
{
    static void Main()
    {
        (var ((a,b), c), int d);
    }
}
";

            var comp1 = CreateCompilation(source1);
            comp1.VerifyDiagnostics(
                // (6,10): error CS8185: A declaration is not allowed in this context.
                //         (var ((a,b), c), int d);
                Diagnostic(ErrorCode.ERR_DeclarationExpressionNotPermitted, "var ((a,b), c)").WithLocation(6, 10),
                // (6,26): error CS8185: A declaration is not allowed in this context.
                //         (var ((a,b), c), int d);
                Diagnostic(ErrorCode.ERR_DeclarationExpressionNotPermitted, "int d").WithLocation(6, 26),
                // (6,9): error CS0201: Only assignment, call, increment, decrement, await, and new object expressions can be used as a statement
                //         (var ((a,b), c), int d);
                Diagnostic(ErrorCode.ERR_IllegalStatement, "(var ((a,b), c), int d)").WithLocation(6, 9),
                // (6,26): error CS0165: Use of unassigned local variable 'd'
                //         (var ((a,b), c), int d);
                Diagnostic(ErrorCode.ERR_UseDefViolation, "int d").WithArguments("d").WithLocation(6, 26)
                );

            StandAlone_18_VerifySemanticModel(comp1, LocalDeclarationKind.DeclarationExpressionVariable);

            string source2 = @"
class C
{
    static void Main()
    {
        (var ((a,b), c), int d) = D;
    }
}
";

            var comp2 = CreateCompilation(source2);

            StandAlone_18_VerifySemanticModel(comp2, LocalDeclarationKind.DeconstructionVariable);
        }

        private static void StandAlone_18_VerifySemanticModel(CSharpCompilation comp, LocalDeclarationKind localDeclarationKind)
        {
            var tree = comp.SyntaxTrees.First();
            var model = comp.GetSemanticModel(tree);
            var designations = tree.GetCompilationUnitRoot().DescendantNodes().OfType<SingleVariableDesignationSyntax>().ToArray();
            Assert.Equal(4, designations.Count());

            var a = model.GetDeclaredSymbol(designations[0]);
            Assert.Equal("var a", a.ToTestDisplayString());
            Assert.Equal(localDeclarationKind, a.GetSymbol<LocalSymbol>().DeclarationKind);

            var b = model.GetDeclaredSymbol(designations[1]);
            Assert.Equal("var b", b.ToTestDisplayString());
            Assert.Equal(localDeclarationKind, b.GetSymbol<LocalSymbol>().DeclarationKind);

            var c = model.GetDeclaredSymbol(designations[2]);
            Assert.Equal("var c", c.ToTestDisplayString());
            Assert.Equal(localDeclarationKind, c.GetSymbol<LocalSymbol>().DeclarationKind);

            var d = model.GetDeclaredSymbol(designations[3]);
            Assert.Equal("System.Int32 d", d.ToTestDisplayString());
            Assert.Equal(localDeclarationKind, d.GetSymbol<LocalSymbol>().DeclarationKind);

            var declarations = tree.GetCompilationUnitRoot().DescendantNodes().OfType<DeclarationExpressionSyntax>().ToArray();
            Assert.Equal(2, declarations.Count());

            Assert.Equal("var ((a,b), c)", declarations[0].ToString());
            var typeInfo = model.GetTypeInfo(declarations[0]);
            Assert.Equal("((var a, var b), var c)", typeInfo.Type.ToTestDisplayString());
            Assert.Equal(typeInfo.Type, typeInfo.ConvertedType);
            Assert.True(model.GetConversion(declarations[0]).IsIdentity);
            var symbolInfo = model.GetSymbolInfo(declarations[0]);
            Assert.Null(symbolInfo.Symbol);
            Assert.Empty(symbolInfo.CandidateSymbols);
            Assert.Equal(CandidateReason.None, symbolInfo.CandidateReason);
            typeInfo = model.GetTypeInfo(declarations[0].Type);
            Assert.Equal("((var a, var b), var c)", typeInfo.Type.ToTestDisplayString());
            Assert.Equal("((var a, var b), var c)", typeInfo.ConvertedType.ToTestDisplayString());
            Assert.True(model.GetConversion(declarations[0].Type).IsIdentity);
            symbolInfo = model.GetSymbolInfo(declarations[0].Type);
            Assert.Equal("((var a, var b), var c)", symbolInfo.Symbol.ToTestDisplayString());
            Assert.Empty(symbolInfo.CandidateSymbols);
            Assert.Equal(CandidateReason.None, symbolInfo.CandidateReason);
            Assert.Null(model.GetAliasInfo(declarations[0].Type));

            Assert.Equal("int d", declarations[1].ToString());
            typeInfo = model.GetTypeInfo(declarations[1]);
            Assert.Equal("System.Int32", typeInfo.Type.ToTestDisplayString());
            Assert.Equal(typeInfo.Type, typeInfo.ConvertedType);
            Assert.True(model.GetConversion(declarations[1]).IsIdentity);
            Assert.Equal("System.Int32 d", model.GetSymbolInfo(declarations[1]).Symbol.ToTestDisplayString());
            typeInfo = model.GetTypeInfo(declarations[1].Type);
            Assert.Equal("System.Int32", typeInfo.Type.ToTestDisplayString());
            Assert.Equal(typeInfo.Type, typeInfo.ConvertedType);
            Assert.True(model.GetConversion(declarations[1].Type).IsIdentity);
            symbolInfo = model.GetSymbolInfo(declarations[1].Type);
            Assert.Equal("System.Int32", symbolInfo.Symbol.ToTestDisplayString());
            Assert.Null(model.GetAliasInfo(declarations[1].Type));

            var tuple = tree.GetCompilationUnitRoot().DescendantNodes().OfType<TupleExpressionSyntax>().Single();
            typeInfo = model.GetTypeInfo(tuple);
            Assert.Equal("(((var a, var b), var c), System.Int32 d)", typeInfo.Type.ToTestDisplayString());
            Assert.Equal(typeInfo.Type, typeInfo.ConvertedType);
            Assert.True(model.GetConversion(tuple).IsIdentity);
            symbolInfo = model.GetSymbolInfo(tuple);
            Assert.Null(symbolInfo.Symbol);
            Assert.Empty(symbolInfo.CandidateSymbols);
            Assert.Equal(CandidateReason.None, symbolInfo.CandidateReason);
        }

        [Fact, WorkItem(17572, "https://github.com/dotnet/roslyn/issues/17572")]
        public void StandAlone_19()
        {
            string source1 = @"
(var ((a,b), c), int d);
";

            var comp1 = CreateCompilation(source1, parseOptions: TestOptions.Script);
            comp1.VerifyDiagnostics(
                // (2,8): error CS7019: Type of 'a' cannot be inferred since its initializer directly or indirectly refers to the definition.
                // (var ((a,b), c), int d);
                Diagnostic(ErrorCode.ERR_RecursivelyTypedVariable, "a").WithArguments("a").WithLocation(2, 8),
                // (2,10): error CS7019: Type of 'b' cannot be inferred since its initializer directly or indirectly refers to the definition.
                // (var ((a,b), c), int d);
                Diagnostic(ErrorCode.ERR_RecursivelyTypedVariable, "b").WithArguments("b").WithLocation(2, 10),
                // (2,14): error CS7019: Type of 'c' cannot be inferred since its initializer directly or indirectly refers to the definition.
                // (var ((a,b), c), int d);
                Diagnostic(ErrorCode.ERR_RecursivelyTypedVariable, "c").WithArguments("c").WithLocation(2, 14),
                // (2,2): error CS8185: A declaration is not allowed in this context.
                // (var ((a,b), c), int d);
                Diagnostic(ErrorCode.ERR_DeclarationExpressionNotPermitted, "var ((a,b), c)").WithLocation(2, 2),
                // (2,18): error CS8185: A declaration is not allowed in this context.
                // (var ((a,b), c), int d);
                Diagnostic(ErrorCode.ERR_DeclarationExpressionNotPermitted, "int d").WithLocation(2, 18),
                // (2,1): error CS0201: Only assignment, call, increment, decrement, await, and new object expressions can be used as a statement
                // (var ((a,b), c), int d);
                Diagnostic(ErrorCode.ERR_IllegalStatement, "(var ((a,b), c), int d)").WithLocation(2, 1)
                );

            StandAlone_19_VerifySemanticModel(comp1);

            string source2 = @"
(var ((a,b), c), int d) = D;
";

            var comp2 = CreateCompilation(source2, parseOptions: TestOptions.Script);

            StandAlone_19_VerifySemanticModel(comp2);
        }

        private static void StandAlone_19_VerifySemanticModel(CSharpCompilation comp)
        {
            var tree = comp.SyntaxTrees.First();
            var model = comp.GetSemanticModel(tree);
            var designations = tree.GetCompilationUnitRoot().DescendantNodes().OfType<SingleVariableDesignationSyntax>().ToArray();
            Assert.Equal(4, designations.Count());

            var a = model.GetDeclaredSymbol(designations[0]);
            Assert.Equal("var Script.a", a.ToTestDisplayString());
            Assert.Equal(SymbolKind.Field, a.Kind);

            var b = model.GetDeclaredSymbol(designations[1]);
            Assert.Equal("var Script.b", b.ToTestDisplayString());
            Assert.Equal(SymbolKind.Field, b.Kind);

            var c = model.GetDeclaredSymbol(designations[2]);
            Assert.Equal("var Script.c", c.ToTestDisplayString());
            Assert.Equal(SymbolKind.Field, c.Kind);

            var d = model.GetDeclaredSymbol(designations[3]);
            Assert.Equal("System.Int32 Script.d", d.ToTestDisplayString());
            Assert.Equal(SymbolKind.Field, d.Kind);

            var declarations = tree.GetCompilationUnitRoot().DescendantNodes().OfType<DeclarationExpressionSyntax>().ToArray();
            Assert.Equal(2, declarations.Count());

            Assert.Equal("var ((a,b), c)", declarations[0].ToString());
            var typeInfo = model.GetTypeInfo(declarations[0]);
            Assert.Equal("((var a, var b), var c)", typeInfo.Type.ToTestDisplayString());
            Assert.Equal(typeInfo.Type, typeInfo.ConvertedType);
            Assert.True(model.GetConversion(declarations[0]).IsIdentity);
            var symbolInfo = model.GetSymbolInfo(declarations[0]);
            Assert.Null(symbolInfo.Symbol);
            Assert.Empty(symbolInfo.CandidateSymbols);
            Assert.Equal(CandidateReason.None, symbolInfo.CandidateReason);
            typeInfo = model.GetTypeInfo(declarations[0].Type);
            Assert.Equal("((var a, var b), var c)", typeInfo.Type.ToTestDisplayString());
            Assert.Equal("((var a, var b), var c)", typeInfo.ConvertedType.ToTestDisplayString());
            Assert.True(model.GetConversion(declarations[0].Type).IsIdentity);
            symbolInfo = model.GetSymbolInfo(declarations[0].Type);
            Assert.Equal("((var a, var b), var c)", symbolInfo.Symbol.ToTestDisplayString());
            Assert.Empty(symbolInfo.CandidateSymbols);
            Assert.Equal(CandidateReason.None, symbolInfo.CandidateReason);
            Assert.Null(model.GetAliasInfo(declarations[0].Type));

            Assert.Equal("int d", declarations[1].ToString());
            typeInfo = model.GetTypeInfo(declarations[1]);
            Assert.Equal("System.Int32", typeInfo.Type.ToTestDisplayString());
            Assert.Equal(typeInfo.Type, typeInfo.ConvertedType);
            Assert.True(model.GetConversion(declarations[1]).IsIdentity);
            Assert.Equal("System.Int32 Script.d", model.GetSymbolInfo(declarations[1]).Symbol.ToTestDisplayString());
            typeInfo = model.GetTypeInfo(declarations[1].Type);
            Assert.Equal("System.Int32", typeInfo.Type.ToTestDisplayString());
            Assert.Equal(typeInfo.Type, typeInfo.ConvertedType);
            Assert.True(model.GetConversion(declarations[1].Type).IsIdentity);
            symbolInfo = model.GetSymbolInfo(declarations[1].Type);
            Assert.Equal("System.Int32", symbolInfo.Symbol.ToTestDisplayString());
            Assert.Null(model.GetAliasInfo(declarations[1].Type));

            var tuple = tree.GetCompilationUnitRoot().DescendantNodes().OfType<TupleExpressionSyntax>().Single();
            typeInfo = model.GetTypeInfo(tuple);
            Assert.Equal("(((var a, var b), var c), System.Int32 d)", typeInfo.Type.ToTestDisplayString());
            Assert.Equal(typeInfo.Type, typeInfo.ConvertedType);
            Assert.True(model.GetConversion(tuple).IsIdentity);
            symbolInfo = model.GetSymbolInfo(tuple);
            Assert.Null(symbolInfo.Symbol);
            Assert.Empty(symbolInfo.CandidateSymbols);
            Assert.Equal(CandidateReason.None, symbolInfo.CandidateReason);
        }

        [Fact, WorkItem(17572, "https://github.com/dotnet/roslyn/issues/17572")]
        public void StandAlone_20()
        {
            string source1 = @"
class C
{
    static void Main()
    {
        (var ((_, _), _), int _);
    }
}
";

            var comp1 = CreateCompilation(source1);
            comp1.VerifyDiagnostics(
                // (6,10): error CS8185: A declaration is not allowed in this context.
                //         (var ((_, _), _), int _);
                Diagnostic(ErrorCode.ERR_DeclarationExpressionNotPermitted, "var ((_, _), _)").WithLocation(6, 10),
                // (6,27): error CS8185: A declaration is not allowed in this context.
                //         (var ((_, _), _), int _);
                Diagnostic(ErrorCode.ERR_DeclarationExpressionNotPermitted, "int _").WithLocation(6, 27),
                // (6,9): error CS0201: Only assignment, call, increment, decrement, await, and new object expressions can be used as a statement
                //         (var ((_, _), _), int _);
                Diagnostic(ErrorCode.ERR_IllegalStatement, "(var ((_, _), _), int _)").WithLocation(6, 9)
                );

            StandAlone_20_VerifySemanticModel(comp1);

            string source2 = @"
class C
{
    static void Main()
    {
        (var ((_, _), _), int _) = D;
    }
}
";

            var comp2 = CreateCompilation(source2);

            StandAlone_20_VerifySemanticModel(comp2);
        }

        private static void StandAlone_20_VerifySemanticModel(CSharpCompilation comp)
        {
            var tree = comp.SyntaxTrees.First();
            var model = comp.GetSemanticModel(tree);
            int count = 0;
            foreach (var designation in tree.GetCompilationUnitRoot().DescendantNodes().OfType<DiscardDesignationSyntax>())
            {
                Assert.Null(model.GetDeclaredSymbol(designation));
                count++;
            }

            Assert.Equal(4, count);

            var declarations = tree.GetCompilationUnitRoot().DescendantNodes().OfType<DeclarationExpressionSyntax>().ToArray();
            Assert.Equal(2, declarations.Count());

            Assert.Equal("var ((_, _), _)", declarations[0].ToString());
            var typeInfo = model.GetTypeInfo(declarations[0]);
            Assert.Equal("((var, var), var)", typeInfo.Type.ToTestDisplayString());
            Assert.Equal(typeInfo.Type, typeInfo.ConvertedType);
            Assert.True(model.GetConversion(declarations[0]).IsIdentity);
            var symbolInfo = model.GetSymbolInfo(declarations[0]);
            Assert.Null(symbolInfo.Symbol);
            Assert.Empty(symbolInfo.CandidateSymbols);
            Assert.Equal(CandidateReason.None, symbolInfo.CandidateReason);
            typeInfo = model.GetTypeInfo(declarations[0].Type);
            Assert.Equal("((var, var), var)", typeInfo.Type.ToTestDisplayString());
            Assert.Equal("((var, var), var)", typeInfo.ConvertedType.ToTestDisplayString());
            Assert.True(model.GetConversion(declarations[0].Type).IsIdentity);
            symbolInfo = model.GetSymbolInfo(declarations[0].Type);
            Assert.Equal("((var, var), var)", symbolInfo.Symbol.ToTestDisplayString());
            Assert.Empty(symbolInfo.CandidateSymbols);
            Assert.Equal(CandidateReason.None, symbolInfo.CandidateReason);
            Assert.Null(model.GetAliasInfo(declarations[0].Type));

            Assert.Equal("int _", declarations[1].ToString());
            typeInfo = model.GetTypeInfo(declarations[1]);
            Assert.Equal("System.Int32", typeInfo.Type.ToTestDisplayString());
            Assert.Equal(typeInfo.Type, typeInfo.ConvertedType);
            Assert.True(model.GetConversion(declarations[1]).IsIdentity);
            symbolInfo = model.GetSymbolInfo(declarations[1]);
            Assert.Null(symbolInfo.Symbol);
            Assert.Empty(symbolInfo.CandidateSymbols);
            Assert.Equal(CandidateReason.None, symbolInfo.CandidateReason);
            typeInfo = model.GetTypeInfo(declarations[1].Type);
            Assert.Equal("System.Int32", typeInfo.Type.ToTestDisplayString());
            Assert.Equal(typeInfo.Type, typeInfo.ConvertedType);
            Assert.True(model.GetConversion(declarations[1].Type).IsIdentity);
            symbolInfo = model.GetSymbolInfo(declarations[1].Type);
            Assert.Equal("System.Int32", symbolInfo.Symbol.ToTestDisplayString());
            Assert.Null(model.GetAliasInfo(declarations[1].Type));

            var tuple = tree.GetCompilationUnitRoot().DescendantNodes().OfType<TupleExpressionSyntax>().Single();
            typeInfo = model.GetTypeInfo(tuple);
            Assert.Equal("(((var, var), var), System.Int32)", typeInfo.Type.ToTestDisplayString());
            Assert.Equal(typeInfo.Type, typeInfo.ConvertedType);
            Assert.True(model.GetConversion(tuple).IsIdentity);
            symbolInfo = model.GetSymbolInfo(tuple);
            Assert.Null(symbolInfo.Symbol);
            Assert.Empty(symbolInfo.CandidateSymbols);
            Assert.Equal(CandidateReason.None, symbolInfo.CandidateReason);
        }

        [Fact, WorkItem(17572, "https://github.com/dotnet/roslyn/issues/17572")]
        public void StandAlone_21()
        {
            string source1 = @"
(var ((_, _), _), int _);
";

            var comp1 = CreateCompilation(source1, parseOptions: TestOptions.Script);
            comp1.VerifyDiagnostics(
                // (2,2): error CS8185: A declaration is not allowed in this context.
                // (var ((_, _), _), int _);
                Diagnostic(ErrorCode.ERR_DeclarationExpressionNotPermitted, "var ((_, _), _)").WithLocation(2, 2),
                // (2,19): error CS8185: A declaration is not allowed in this context.
                // (var ((_, _), _), int _);
                Diagnostic(ErrorCode.ERR_DeclarationExpressionNotPermitted, "int _").WithLocation(2, 19),
                // (2,1): error CS0201: Only assignment, call, increment, decrement, await, and new object expressions can be used as a statement
                // (var ((_, _), _), int _);
                Diagnostic(ErrorCode.ERR_IllegalStatement, "(var ((_, _), _), int _)").WithLocation(2, 1)
                );

            StandAlone_20_VerifySemanticModel(comp1);

            string source2 = @"
(var ((_, _), _), int _) = D;
";

            var comp2 = CreateCompilation(source2, parseOptions: TestOptions.Script);

            StandAlone_20_VerifySemanticModel(comp2);
        }

        [Fact, WorkItem(17921, "https://github.com/dotnet/roslyn/issues/17921")]
        public void DiscardVoid_01()
        {
            var source = @"class C
{
    static void Main()
    {
        (_, _) = (1, Main());
    }
}";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (5,22): error CS8210: A tuple may not contain a value of type 'void'.
                //         (_, _) = (1, Main());
                Diagnostic(ErrorCode.ERR_VoidInTuple, "Main()").WithLocation(5, 22)
                );
            var main = comp.GetMember<MethodSymbol>("C.Main");
            var tree = comp.SyntaxTrees[0];
            var model = comp.GetSemanticModel(tree);
            var mainCall = tree.GetRoot().DescendantNodes().OfType<ExpressionSyntax>().Where(n => n.ToString() == "Main()").Single();
            var type = model.GetTypeInfo(mainCall);
            Assert.Equal(SpecialType.System_Void, type.Type.SpecialType);
            Assert.Equal(SpecialType.System_Void, type.ConvertedType.SpecialType);
            Assert.Equal(ConversionKind.Identity, model.GetConversion(mainCall).Kind);
            var symbols = model.GetSymbolInfo(mainCall);
            Assert.Equal(symbols.Symbol, main.GetPublicSymbol());
            Assert.Empty(symbols.CandidateSymbols);
            Assert.Equal(CandidateReason.None, symbols.CandidateReason);

            // the ArgumentSyntax above a tuple element doesn't support GetTypeInfo or GetSymbolInfo.
            var argument = (ArgumentSyntax)mainCall.Parent;
            type = model.GetTypeInfo(argument);
            Assert.Null(type.Type);
            Assert.Null(type.ConvertedType);
            symbols = model.GetSymbolInfo(argument);
            Assert.Null(symbols.Symbol);
            Assert.Empty(symbols.CandidateSymbols);
            Assert.Equal(CandidateReason.None, symbols.CandidateReason);
        }

        [Fact, WorkItem(17921, "https://github.com/dotnet/roslyn/issues/17921")]
        public void DeconstructVoid_01()
        {
            var source = @"class C
{
    static void Main()
    {
        (int x, void y) = (1, Main());
    }
}";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (5,17): error CS1547: Keyword 'void' cannot be used in this context
                //         (int x, void y) = (1, Main());
                Diagnostic(ErrorCode.ERR_NoVoidHere, "void").WithLocation(5, 17),
                // (5,31): error CS8210: A tuple may not contain a value of type 'void'.
                //         (int x, void y) = (1, Main());
                Diagnostic(ErrorCode.ERR_VoidInTuple, "Main()").WithLocation(5, 31)
                );
            var main = comp.GetMember<MethodSymbol>("C.Main");
            var tree = comp.SyntaxTrees[0];
            var model = comp.GetSemanticModel(tree);
            var mainCall = tree.GetRoot().DescendantNodes().OfType<ExpressionSyntax>().Where(n => n.ToString() == "Main()").Single();
            var type = model.GetTypeInfo(mainCall);
            Assert.Equal(SpecialType.System_Void, type.Type.SpecialType);
            Assert.Equal(SpecialType.System_Void, type.ConvertedType.SpecialType);
            Assert.Equal(ConversionKind.Identity, model.GetConversion(mainCall).Kind);
            var symbols = model.GetSymbolInfo(mainCall);
            Assert.Equal(symbols.Symbol, main.GetPublicSymbol());
            Assert.Empty(symbols.CandidateSymbols);
            Assert.Equal(CandidateReason.None, symbols.CandidateReason);

            // the ArgumentSyntax above a tuple element doesn't support GetTypeInfo or GetSymbolInfo.
            var argument = (ArgumentSyntax)mainCall.Parent;
            type = model.GetTypeInfo(argument);
            Assert.Null(type.Type);
            Assert.Null(type.ConvertedType);
            symbols = model.GetSymbolInfo(argument);
            Assert.Null(symbols.Symbol);
            Assert.Empty(symbols.CandidateSymbols);
            Assert.Equal(CandidateReason.None, symbols.CandidateReason);
        }

        [Fact, WorkItem(17921, "https://github.com/dotnet/roslyn/issues/17921")]
        public void DeconstructVoid_02()
        {
            var source = @"class C
{
    static void Main()
    {
        var (x, y) = (1, Main());
    }
}";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (5,26): error CS8210: A tuple may not contain a value of type 'void'.
                //         var (x, y) = (1, Main());
                Diagnostic(ErrorCode.ERR_VoidInTuple, "Main()").WithLocation(5, 26)
                );
            var main = comp.GetMember<MethodSymbol>("C.Main");
            var tree = comp.SyntaxTrees[0];
            var model = comp.GetSemanticModel(tree);
            var mainCall = tree.GetRoot().DescendantNodes().OfType<ExpressionSyntax>().Where(n => n.ToString() == "Main()").Single();
            var type = model.GetTypeInfo(mainCall);
            Assert.Equal(SpecialType.System_Void, type.Type.SpecialType);
            Assert.Equal(SpecialType.System_Void, type.ConvertedType.SpecialType);
            Assert.Equal(ConversionKind.Identity, model.GetConversion(mainCall).Kind);
            var symbols = model.GetSymbolInfo(mainCall);
            Assert.Equal(symbols.Symbol, main.GetPublicSymbol());
            Assert.Empty(symbols.CandidateSymbols);
            Assert.Equal(CandidateReason.None, symbols.CandidateReason);

            // the ArgumentSyntax above a tuple element doesn't support GetTypeInfo or GetSymbolInfo.
            var argument = (ArgumentSyntax)mainCall.Parent;
            type = model.GetTypeInfo(argument);
            Assert.Null(type.Type);
            Assert.Null(type.ConvertedType);
            symbols = model.GetSymbolInfo(argument);
            Assert.Null(symbols.Symbol);
            Assert.Empty(symbols.CandidateSymbols);
            Assert.Equal(CandidateReason.None, symbols.CandidateReason);
        }

        [Fact, WorkItem(17921, "https://github.com/dotnet/roslyn/issues/17921")]
        public void DeconstructVoid_03()
        {
            var source = @"class C
{
    static void Main()
    {
        (int x, void y) = (1, 2);
    }
}";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (5,17): error CS1547: Keyword 'void' cannot be used in this context
                //         (int x, void y) = (1, 2);
                Diagnostic(ErrorCode.ERR_NoVoidHere, "void").WithLocation(5, 17),
                // (5,31): error CS0029: Cannot implicitly convert type 'int' to 'void'
                //         (int x, void y) = (1, 2);
                Diagnostic(ErrorCode.ERR_NoImplicitConv, "2").WithArguments("int", "void").WithLocation(5, 31)
                );
            var tree = comp.SyntaxTrees[0];
            var model = comp.GetSemanticModel(tree);
            var two = tree.GetRoot().DescendantNodes().OfType<ExpressionSyntax>().Where(n => n.ToString() == "2").Single();
            var type = model.GetTypeInfo(two);
            Assert.Equal(SpecialType.System_Int32, type.Type.SpecialType);
            Assert.Equal(SpecialType.System_Int32, type.ConvertedType.SpecialType);
            Assert.Equal(ConversionKind.Identity, model.GetConversion(two).Kind);
            var symbols = model.GetSymbolInfo(two);
            Assert.Null(symbols.Symbol);
            Assert.Empty(symbols.CandidateSymbols);
            Assert.Equal(CandidateReason.None, symbols.CandidateReason);

            // the ArgumentSyntax above a tuple element doesn't support GetTypeInfo or GetSymbolInfo.
            var argument = (ArgumentSyntax)two.Parent;
            type = model.GetTypeInfo(argument);
            Assert.Null(type.Type);
            Assert.Null(type.ConvertedType);
            symbols = model.GetSymbolInfo(argument);
            Assert.Null(symbols.Symbol);
            Assert.Empty(symbols.CandidateSymbols);
            Assert.Equal(CandidateReason.None, symbols.CandidateReason);
        }

        [Fact, WorkItem(17921, "https://github.com/dotnet/roslyn/issues/17921")]
        public void DeconstructVoid_04()
        {
            var source = @"class C
{
    static void Main()
    {
        (int x, int y) = (1, Main());
    }
}";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (5,30): error CS8210: A tuple may not contain a value of type 'void'.
                //         (int x, int y) = (1, Main());
                Diagnostic(ErrorCode.ERR_VoidInTuple, "Main()").WithLocation(5, 30)
                );
            var main = comp.GetMember<MethodSymbol>("C.Main");
            var tree = comp.SyntaxTrees[0];
            var model = comp.GetSemanticModel(tree);
            var mainCall = tree.GetRoot().DescendantNodes().OfType<ExpressionSyntax>().Where(n => n.ToString() == "Main()").Single();
            var type = model.GetTypeInfo(mainCall);
            Assert.Equal(SpecialType.System_Void, type.Type.SpecialType);
            Assert.Equal(SpecialType.System_Void, type.ConvertedType.SpecialType);
            Assert.Equal(ConversionKind.Identity, model.GetConversion(mainCall).Kind);
            var symbols = model.GetSymbolInfo(mainCall);
            Assert.Equal(symbols.Symbol, main.GetPublicSymbol());
            Assert.Empty(symbols.CandidateSymbols);
            Assert.Equal(CandidateReason.None, symbols.CandidateReason);

            // the ArgumentSyntax above a tuple element doesn't support GetTypeInfo or GetSymbolInfo.
            var argument = (ArgumentSyntax)mainCall.Parent;
            type = model.GetTypeInfo(argument);
            Assert.Null(type.Type);
            Assert.Null(type.ConvertedType);
            symbols = model.GetSymbolInfo(argument);
            Assert.Null(symbols.Symbol);
            Assert.Empty(symbols.CandidateSymbols);
            Assert.Equal(CandidateReason.None, symbols.CandidateReason);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void DiscardDeclarationExpression_IOperation()
        {
            string source = @"
class C
{
    void M()
    {
        /*<bind>*/var (_, _) = (0, 0)/*</bind>*/;
    }
}
";
            string expectedOperationTree = @"
IDeconstructionAssignmentOperation (OperationKind.DeconstructionAssignment, Type: (System.Int32, System.Int32)) (Syntax: 'var (_, _) = (0, 0)')
  Left: 
    IDeclarationExpressionOperation (OperationKind.DeclarationExpression, Type: (System.Int32, System.Int32)) (Syntax: 'var (_, _)')
      ITupleOperation (OperationKind.Tuple, Type: (System.Int32, System.Int32)) (Syntax: '(_, _)')
        NaturalType: (System.Int32, System.Int32)
        Elements(2):
            IDiscardOperation (Symbol: System.Int32 _) (OperationKind.Discard, Type: System.Int32) (Syntax: '_')
            IDiscardOperation (Symbol: System.Int32 _) (OperationKind.Discard, Type: System.Int32) (Syntax: '_')
  Right: 
    ITupleOperation (OperationKind.Tuple, Type: (System.Int32, System.Int32)) (Syntax: '(0, 0)')
      NaturalType: (System.Int32, System.Int32)
      Elements(2):
          ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 0) (Syntax: '0')
          ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 0) (Syntax: '0')
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<AssignmentExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void DiscardDeclarationAssignment_IOperation()
        {
            string source = @"
class C
{
    void M()
    {
        int x;
        /*<bind>*/(x, _) = (0, 0)/*</bind>*/;
    }
}
";
            string expectedOperationTree = @"
IDeconstructionAssignmentOperation (OperationKind.DeconstructionAssignment, Type: (System.Int32 x, System.Int32)) (Syntax: '(x, _) = (0, 0)')
  Left: 
    ITupleOperation (OperationKind.Tuple, Type: (System.Int32 x, System.Int32)) (Syntax: '(x, _)')
      NaturalType: (System.Int32 x, System.Int32)
      Elements(2):
          ILocalReferenceOperation: x (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'x')
          IDiscardOperation (Symbol: System.Int32 _) (OperationKind.Discard, Type: System.Int32) (Syntax: '_')
  Right: 
    ITupleOperation (OperationKind.Tuple, Type: (System.Int32, System.Int32)) (Syntax: '(0, 0)')
      NaturalType: (System.Int32, System.Int32)
      Elements(2):
          ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 0) (Syntax: '0')
          ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 0) (Syntax: '0')
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<AssignmentExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void DiscardOutVarDeclaration_IOperation()
        {
            string source = @"
class C
{
    void M()
    {
        M2(out /*<bind>*/var _/*</bind>*/);
    }

    void M2(out int x)
    {
        x = 0;
    }
}
";
            string expectedOperationTree = @"
IDiscardOperation (Symbol: System.Int32 _) (OperationKind.Discard, Type: System.Int32) (Syntax: 'var _')
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<DeclarationExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [Fact]
        [WorkItem(46165, "https://github.com/dotnet/roslyn/issues/46165")]
        public void Issue46165_1()
        {
            var text = @"
class C
{
    static void Main()
    {
        foreach ((var i, i))
    }
}";

            CreateCompilation(text).VerifyEmitDiagnostics(
                // (6,18): error CS8186: A foreach loop must declare its iteration variables.
                //         foreach ((var i, i))
                Diagnostic(ErrorCode.ERR_MustDeclareForeachIteration, "(var i, i)").WithLocation(6, 18),
                // (6,23): error CS8130: Cannot infer the type of implicitly-typed deconstruction variable 'i'.
                //         foreach ((var i, i))
                Diagnostic(ErrorCode.ERR_TypeInferenceFailedForImplicitlyTypedDeconstructionVariable, "i").WithArguments("i").WithLocation(6, 23),
                // (6,26): error CS0841: Cannot use local variable 'i' before it is declared
                //         foreach ((var i, i))
                Diagnostic(ErrorCode.ERR_VariableUsedBeforeDeclaration, "i").WithArguments("i").WithLocation(6, 26),
                // (6,28): error CS1515: 'in' expected
                //         foreach ((var i, i))
                Diagnostic(ErrorCode.ERR_InExpected, ")").WithLocation(6, 28),
                // (6,28): error CS1525: Invalid expression term ')'
                //         foreach ((var i, i))
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, ")").WithArguments(")").WithLocation(6, 28),
                // (6,29): error CS1525: Invalid expression term '}'
                //         foreach ((var i, i))
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, "").WithArguments("}").WithLocation(6, 29),
                // (6,29): error CS1002: ; expected
                //         foreach ((var i, i))
                Diagnostic(ErrorCode.ERR_SemicolonExpected, "").WithLocation(6, 29)
                );
        }

        [Fact]
        [WorkItem(46165, "https://github.com/dotnet/roslyn/issues/46165")]
        public void Issue46165_2()
        {
            var text = @"
class C
{
    static void Main()
    {
        (var i, i) = ;
    }
}";

            CreateCompilation(text).VerifyEmitDiagnostics(
                // (6,14): error CS8130: Cannot infer the type of implicitly-typed deconstruction variable 'i'.
                //         (var i, i) = ;
                Diagnostic(ErrorCode.ERR_TypeInferenceFailedForImplicitlyTypedDeconstructionVariable, "i").WithArguments("i").WithLocation(6, 14),
                // (6,17): error CS0841: Cannot use local variable 'i' before it is declared
                //         (var i, i) = ;
                Diagnostic(ErrorCode.ERR_VariableUsedBeforeDeclaration, "i").WithArguments("i").WithLocation(6, 17),
                // (6,22): error CS1525: Invalid expression term ';'
                //         (var i, i) = ;
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, ";").WithArguments(";").WithLocation(6, 22)
                );
        }

        [Fact]
        [WorkItem(46165, "https://github.com/dotnet/roslyn/issues/46165")]
        public void Issue46165_3()
        {
            var text = @"
class C
{
    static void Main()
    {
        foreach ((int i, i))
    }
}";

            CreateCompilation(text).VerifyEmitDiagnostics(
                // (6,18): error CS8186: A foreach loop must declare its iteration variables.
                //         foreach ((int i, i))
                Diagnostic(ErrorCode.ERR_MustDeclareForeachIteration, "(int i, i)").WithLocation(6, 18),
                // (6,26): error CS1656: Cannot assign to 'i' because it is a 'foreach iteration variable'
                //         foreach ((int i, i))
                Diagnostic(ErrorCode.ERR_AssgReadonlyLocalCause, "i").WithArguments("i", "foreach iteration variable").WithLocation(6, 26),
                // (6,28): error CS1515: 'in' expected
                //         foreach ((int i, i))
                Diagnostic(ErrorCode.ERR_InExpected, ")").WithLocation(6, 28),
                // (6,28): error CS1525: Invalid expression term ')'
                //         foreach ((int i, i))
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, ")").WithArguments(")").WithLocation(6, 28),
                // (6,29): error CS1525: Invalid expression term '}'
                //         foreach ((int i, i))
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, "").WithArguments("}").WithLocation(6, 29),
                // (6,29): error CS1002: ; expected
                //         foreach ((int i, i))
                Diagnostic(ErrorCode.ERR_SemicolonExpected, "").WithLocation(6, 29)
                );
        }

        [Fact]
        [WorkItem(46165, "https://github.com/dotnet/roslyn/issues/46165")]
        public void Issue46165_4()
        {
            var text = @"
class C
{
    static void Main()
    {
        (int i, i) = ;
    }
}";

            CreateCompilation(text).VerifyEmitDiagnostics(
                // (6,22): error CS1525: Invalid expression term ';'
                //         (int i, i) = ;
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, ";").WithArguments(";").WithLocation(6, 22)
                );
        }

        [Fact]
        public void ObsoleteConversions_01()
        {
            var source = @"
var x = (1, new C());

(int i, bool c) = x;
(i, c) = (1, new C());
(i, c) = new C2();

class C
{
    [System.Obsolete()]
    public static implicit operator bool(C c) => true;
}

class C2
{
    public void Deconstruct(out int i, out C c) => throw null;
}";

            CreateCompilation(source).VerifyEmitDiagnostics(
                // (4,1): warning CS0612: 'C.implicit operator bool(C)' is obsolete
                // (int i, bool c) = x;
                Diagnostic(ErrorCode.WRN_DeprecatedSymbol, "(int i, bool c) = x").WithArguments("C.implicit operator bool(C)").WithLocation(4, 1),
                // (5,14): warning CS0612: 'C.implicit operator bool(C)' is obsolete
                // (i, c) = (1, new C());
                Diagnostic(ErrorCode.WRN_DeprecatedSymbol, "new C()").WithArguments("C.implicit operator bool(C)").WithLocation(5, 14),
                // (6,1): warning CS0612: 'C.implicit operator bool(C)' is obsolete
                // (i, c) = new C2();
                Diagnostic(ErrorCode.WRN_DeprecatedSymbol, "(i, c) = new C2()").WithArguments("C.implicit operator bool(C)").WithLocation(6, 1)
                );
        }

        [Fact]
        public void ObsoleteConversions_02()
        {
            var source = @"
var x = (1, new C());

(int i, bool c) = x;
(i, c) = (1, new C());
(i, c) = new C2();

class C
{
    [System.Obsolete(""Obsolete error"", true)]
    public static implicit operator bool(C c) => true;
}

class C2
{
    public void Deconstruct(out int i, out C c) => throw null;
}";

            CreateCompilation(source).VerifyEmitDiagnostics(
                // (4,1): error CS0619: 'C.implicit operator bool(C)' is obsolete: 'Obsolete error'
                // (int i, bool c) = x;
                Diagnostic(ErrorCode.ERR_DeprecatedSymbolStr, "(int i, bool c) = x").WithArguments("C.implicit operator bool(C)", "Obsolete error").WithLocation(4, 1),
                // (5,14): error CS0619: 'C.implicit operator bool(C)' is obsolete: 'Obsolete error'
                // (i, c) = (1, new C());
                Diagnostic(ErrorCode.ERR_DeprecatedSymbolStr, "new C()").WithArguments("C.implicit operator bool(C)", "Obsolete error").WithLocation(5, 14),
                // (6,1): error CS0619: 'C.implicit operator bool(C)' is obsolete: 'Obsolete error'
                // (i, c) = new C2();
                Diagnostic(ErrorCode.ERR_DeprecatedSymbolStr, "(i, c) = new C2()").WithArguments("C.implicit operator bool(C)", "Obsolete error").WithLocation(6, 1)
                );
        }

        [Fact, WorkItem(58472, "https://github.com/dotnet/roslyn/issues/58472")]
        public void DeconstructionIntoImplicitIndexers()
        {
            var source = @"
var x = new int[1];
C.M(x);

var y = new int[1];
C.M2(y);

System.Console.Write((x[^1], y[^1]));

class C
{
    public static void M<T>(T[] a)
    {
        (a[0], a[^1]) = (default, default);
    }

    public static void M2(int[] a)
    {
        (a[0], a[^1]) = (default, default);
    }
}
";

            var comp = CreateCompilationWithIndex(source);
            // No IndexOutOfRangeException thrown
            var verifier = CompileAndVerify(comp, expectedOutput: "(0, 0)");
            verifier.VerifyDiagnostics();
            verifier.VerifyIL("C.M<T>(T[])", @"
{
  // Code size       41 (0x29)
  .maxstack  3
  .locals init (T[] V_0,
                int V_1,
                T V_2)
  IL_0000:  ldarg.0
  IL_0001:  ldarg.0
  IL_0002:  dup
  IL_0003:  stloc.0
  IL_0004:  ldlen
  IL_0005:  conv.i4
  IL_0006:  ldc.i4.1
  IL_0007:  sub
  IL_0008:  stloc.1
  IL_0009:  ldc.i4.0
  IL_000a:  ldloca.s   V_2
  IL_000c:  initobj    ""T""
  IL_0012:  ldloc.2
  IL_0013:  stelem     ""T""
  IL_0018:  ldloc.0
  IL_0019:  ldloc.1
  IL_001a:  ldloca.s   V_2
  IL_001c:  initobj    ""T""
  IL_0022:  ldloc.2
  IL_0023:  stelem     ""T""
  IL_0028:  ret
}
");
            verifier.VerifyIL("C.M2", @"
{
  // Code size       25 (0x19)
  .maxstack  3
  .locals init (int& V_0)
  IL_0000:  ldarg.0
  IL_0001:  ldc.i4.0
  IL_0002:  ldelema    ""int""
  IL_0007:  stloc.0
  IL_0008:  ldarg.0
  IL_0009:  dup
  IL_000a:  ldlen
  IL_000b:  conv.i4
  IL_000c:  ldc.i4.1
  IL_000d:  sub
  IL_000e:  ldelema    ""int""
  IL_0013:  ldloc.0
  IL_0014:  ldc.i4.0
  IL_0015:  stind.i4
  IL_0016:  ldc.i4.0
  IL_0017:  stind.i4
  IL_0018:  ret
}
");
        }

        [Fact, WorkItem(61332, "https://github.com/dotnet/roslyn/issues/61332")]
        public void NestedNullableConversions()
        {
            var code = """
            float? _startScrollPosition, _endScrollPosition;
            (_startScrollPosition, _endScrollPosition) = GetScrollPositions();

            (float, float) GetScrollPositions() => (0, 0);
            """;

            var comp = CreateCompilation(code);
            comp.VerifyDiagnostics();

            var tree = comp.SyntaxTrees[0];
            var model = comp.GetSemanticModel(tree);
            var assignment = tree.GetRoot().DescendantNodes().OfType<AssignmentExpressionSyntax>().Single();
            var deconstructionInfo = model.GetDeconstructionInfo(assignment);
            var nestedConversions = deconstructionInfo.Nested;
            Assert.Equal(2, nestedConversions.Length);
            Assert.All(nestedConversions, n => Assert.Empty(n.Nested));
        }

        [Fact]
        [WorkItem("https://github.com/dotnet/roslyn/issues/68026")]
        public void ErrorForeachVariable_01()
        {
            var source = @"
foreach
Console.Write($""{1 switch { _ => 1 }}"");
";
            CreateCompilation(source).VerifyDiagnostics(
                // (2,8): error CS1003: Syntax error, '(' expected
                // foreach
                Diagnostic(ErrorCode.ERR_SyntaxError, "").WithArguments("(").WithLocation(2, 8),
                // (3,40): error CS1515: 'in' expected
                // Console.Write($"{1 switch { _ => 1 }}");
                Diagnostic(ErrorCode.ERR_InExpected, ";").WithLocation(3, 40),
                // (3,40): error CS0230: Type and identifier are both required in a foreach statement
                // Console.Write($"{1 switch { _ => 1 }}");
                Diagnostic(ErrorCode.ERR_BadForeachDecl, ";").WithLocation(3, 40),
                // (3,40): error CS1525: Invalid expression term ';'
                // Console.Write($"{1 switch { _ => 1 }}");
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, ";").WithArguments(";").WithLocation(3, 40),
                // (3,40): error CS1026: ) expected
                // Console.Write($"{1 switch { _ => 1 }}");
                Diagnostic(ErrorCode.ERR_CloseParenExpected, ";").WithLocation(3, 40)
                );
        }

        [Fact]
        [WorkItem("https://github.com/dotnet/roslyn/issues/68026")]
        public void ErrorForeachVariable_02()
        {
            var source = @"
foreach (m(out var x) in new[]{1,2})
{ 
    x++; // 1
}

x++; // 2

void m(out int x) => x = 0;
";
            CreateCompilation(source).VerifyDiagnostics(
                // (2,23): error CS0230: Type and identifier are both required in a foreach statement
                // foreach (m(out var x) in new[]{1,2})
                Diagnostic(ErrorCode.ERR_BadForeachDecl, "in").WithLocation(2, 23),
                // (7,1): error CS0103: The name 'x' does not exist in the current context
                // x++; // 2
                Diagnostic(ErrorCode.ERR_NameNotInContext, "x").WithArguments("x").WithLocation(7, 1),
                // (9,6): warning CS8321: The local function 'm' is declared but never used
                // void m(out int x) => x = 0;
                Diagnostic(ErrorCode.WRN_UnreferencedLocalFunction, "m").WithArguments("m").WithLocation(9, 6)
                );
        }
    }
}
