// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    public class IOperationTests_StackAllocArrayCreationAndInitializer : SemanticModelTestBase
    {
        [Fact]
        public void SimpleStackAllocArrayCreation_PrimitiveType()
        {
            string source = @"
class C
{
    public void F()
    {
        var a = /*<bind>*/stackalloc int[1]/*</bind>*/;
    }
}
";
            string expectedOperationTree = @"
IOperation:  (OperationKind.None, Type: System.Int32*, IsInvalid) (Syntax: 'stackalloc int[1]')
  Children(1):
      ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1, IsInvalid) (Syntax: '1')
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // file.cs(6,27): error CS0214: Pointers and fixed size buffers may only be used in an unsafe context
                //         var a = /*<bind>*/stackalloc int[1]/*</bind>*/;
                Diagnostic(ErrorCode.ERR_UnsafeNeeded, "stackalloc int[1]").WithLocation(6, 27)
            };

            VerifyOperationTreeAndDiagnosticsForTest<StackAllocArrayCreationExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [Fact]
        public void SimpleStackAllocArrayCreation_UserDefinedType()
        {
            string source = @"
struct M { }

class C
{
    public void F()
    {
        var a = /*<bind>*/stackalloc M[1]/*</bind>*/;
    }
}
";
            string expectedOperationTree = @"
IOperation:  (OperationKind.None, Type: M*, IsInvalid) (Syntax: 'stackalloc M[1]')
  Children(1):
      ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1, IsInvalid) (Syntax: '1')
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // file.cs(8,27): error CS0214: Pointers and fixed size buffers may only be used in an unsafe context
                //         var a = /*<bind>*/stackalloc M[1]/*</bind>*/;
                Diagnostic(ErrorCode.ERR_UnsafeNeeded, "stackalloc M[1]").WithLocation(8, 27)
            };

            VerifyOperationTreeAndDiagnosticsForTest<StackAllocArrayCreationExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [Fact]
        public void SimpleStackAllocArrayCreation_ConstantDimension()
        {
            string source = @"
struct M { }

class C
{
    public void F()
    {
        const int dimension = 1;
        var a = /*<bind>*/stackalloc M[dimension]/*</bind>*/;
    }
}
";
            string expectedOperationTree = @"
IOperation:  (OperationKind.None, Type: M*, IsInvalid) (Syntax: 'stackalloc M[dimension]')
  Children(1):
      ILocalReferenceOperation: dimension (OperationKind.LocalReference, Type: System.Int32, Constant: 1, IsInvalid) (Syntax: 'dimension')
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // file.cs(9,27): error CS0214: Pointers and fixed size buffers may only be used in an unsafe context
                //         var a = /*<bind>*/stackalloc M[dimension]/*</bind>*/;
                Diagnostic(ErrorCode.ERR_UnsafeNeeded, "stackalloc M[dimension]").WithLocation(9, 27)
            };

            VerifyOperationTreeAndDiagnosticsForTest<StackAllocArrayCreationExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [Fact]
        public void SimpleStackAllocArrayCreation_NonConstantDimension()
        {
            string source = @"
struct M { }

class C
{
    public void F(int dimension)
    {
        var a = /*<bind>*/stackalloc M[dimension]/*</bind>*/;
    }
}
";
            string expectedOperationTree = @"
IOperation:  (OperationKind.None, Type: M*, IsInvalid) (Syntax: 'stackalloc M[dimension]')
  Children(1):
      IParameterReferenceOperation: dimension (OperationKind.ParameterReference, Type: System.Int32, IsInvalid) (Syntax: 'dimension')
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // file.cs(8,27): error CS0214: Pointers and fixed size buffers may only be used in an unsafe context
                //         var a = /*<bind>*/stackalloc M[dimension]/*</bind>*/;
                Diagnostic(ErrorCode.ERR_UnsafeNeeded, "stackalloc M[dimension]").WithLocation(8, 27)
            };

            VerifyOperationTreeAndDiagnosticsForTest<StackAllocArrayCreationExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [Fact]
        public void SimpleStackAllocArrayCreation_DimensionWithImplicitConversion()
        {
            string source = @"
struct M { }

class C
{
    public void F(char dimension)
    {
        var a = /*<bind>*/stackalloc M[dimension]/*</bind>*/;
    }
}
";
            string expectedOperationTree = @"
IOperation:  (OperationKind.None, Type: M*, IsInvalid) (Syntax: 'stackalloc M[dimension]')
  Children(1):
      IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Int32, IsInvalid, IsImplicit) (Syntax: 'dimension')
        Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: True, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        Operand: 
          IParameterReferenceOperation: dimension (OperationKind.ParameterReference, Type: System.Char, IsInvalid) (Syntax: 'dimension')
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // file.cs(8,27): error CS0214: Pointers and fixed size buffers may only be used in an unsafe context
                //         var a = /*<bind>*/stackalloc M[dimension]/*</bind>*/;
                Diagnostic(ErrorCode.ERR_UnsafeNeeded, "stackalloc M[dimension]").WithLocation(8, 27)
            };

            VerifyOperationTreeAndDiagnosticsForTest<StackAllocArrayCreationExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [Fact]
        public void SimpleStackAllocArrayCreation_DimensionWithExplicitConversion()
        {
            string source = @"
struct M { }

class C
{
    public void F(object dimension)
    {
        var a = /*<bind>*/stackalloc M[(int)dimension]/*</bind>*/;
    }
}
";
            string expectedOperationTree = @"
IOperation:  (OperationKind.None, Type: M*, IsInvalid) (Syntax: 'stackalloc  ... )dimension]')
  Children(1):
      IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Int32, IsInvalid) (Syntax: '(int)dimension')
        Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        Operand: 
          IParameterReferenceOperation: dimension (OperationKind.ParameterReference, Type: System.Object, IsInvalid) (Syntax: 'dimension')
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // file.cs(8,27): error CS0214: Pointers and fixed size buffers may only be used in an unsafe context
                //         var a = /*<bind>*/stackalloc M[(int)dimension]/*</bind>*/;
                Diagnostic(ErrorCode.ERR_UnsafeNeeded, "stackalloc M[(int)dimension]").WithLocation(8, 27)
            };

            VerifyOperationTreeAndDiagnosticsForTest<StackAllocArrayCreationExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [Fact]
        public void StackAllocArrayCreationWithInitializer_PrimitiveType()
        {
            string source = @"
class C
{
    public void F()
    {
        var a = /*<bind>*/stackalloc int[] { 42 }/*</bind>*/;
    }
}
";
            string expectedOperationTree = @"
IOperation:  (OperationKind.None, Type: System.Int32*, IsInvalid) (Syntax: 'stackalloc int[] { 42 }')
  Children(2):
      ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1, IsInvalid, IsImplicit) (Syntax: 'stackalloc int[] { 42 }')
      ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 42, IsInvalid) (Syntax: '42')
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // file.cs(6,27): error CS0214: Pointers and fixed size buffers may only be used in an unsafe context
                //         var a = /*<bind>*/stackalloc int[] { 42 }/*</bind>*/;
                Diagnostic(ErrorCode.ERR_UnsafeNeeded, "stackalloc int[] { 42 }").WithLocation(6, 27)
            };

            VerifyOperationTreeAndDiagnosticsForTest<StackAllocArrayCreationExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [Fact]
        public void StackAllocArrayCreationWithInitializer_PrimitiveTypeWithExplicitDimension()
        {
            string source = @"
class C
{
    public void F()
    {
        var a = /*<bind>*/stackalloc int[1] { 42 }/*</bind>*/;
    }
}
";
            string expectedOperationTree = @"
IOperation:  (OperationKind.None, Type: System.Int32*, IsInvalid) (Syntax: 'stackalloc int[1] { 42 }')
  Children(2):
      ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1, IsInvalid) (Syntax: '1')
      ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 42, IsInvalid) (Syntax: '42')
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // file.cs(6,27): error CS0214: Pointers and fixed size buffers may only be used in an unsafe context
                //         var a = /*<bind>*/stackalloc int[1] { 42 }/*</bind>*/;
                Diagnostic(ErrorCode.ERR_UnsafeNeeded, "stackalloc int[1] { 42 }").WithLocation(6, 27)
            };

            VerifyOperationTreeAndDiagnosticsForTest<StackAllocArrayCreationExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [Fact]
        public void StackAllocArrayCreationWithInitializerErrorCase_PrimitiveTypeWithIncorrectExplicitDimension()
        {
            string source = @"
class C
{
    public void F()
    {
        var a = /*<bind>*/stackalloc int[2] { 42 }/*</bind>*/;
    }
}
";
            string expectedOperationTree = @"
IOperation:  (OperationKind.None, Type: System.Int32*, IsInvalid) (Syntax: 'stackalloc int[2] { 42 }')
  Children(2):
      ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 2, IsInvalid) (Syntax: '2')
      ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 42, IsInvalid) (Syntax: '42')
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // file.cs(6,27): error CS0847: An array initializer of length '2' is expected
                //         var a = /*<bind>*/stackalloc int[2] { 42 }/*</bind>*/;
                Diagnostic(ErrorCode.ERR_ArrayInitializerIncorrectLength, "stackalloc int[2] { 42 }").WithArguments("2").WithLocation(6, 27)
            };

            VerifyOperationTreeAndDiagnosticsForTest<StackAllocArrayCreationExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [Fact]
        public void StackAllocArrayCreationWithInitializerErrorCase_PrimitiveTypeWithNonConstantExplicitDimension()
        {
            string source = @"
class C
{
    public void F(int dimension)
    {
        var a = /*<bind>*/stackalloc int[dimension] { 42 }/*</bind>*/;
    }
}
";
            string expectedOperationTree = @"
IOperation:  (OperationKind.None, Type: System.Int32*, IsInvalid) (Syntax: 'stackalloc  ... ion] { 42 }')
  Children(2):
      IParameterReferenceOperation: dimension (OperationKind.ParameterReference, Type: System.Int32, IsInvalid) (Syntax: 'dimension')
      ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 42) (Syntax: '42')
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // file.cs(6,42): error CS0150: A constant value is expected
                //         var a = /*<bind>*/stackalloc int[dimension] { 42 }/*</bind>*/;
                Diagnostic(ErrorCode.ERR_ConstantExpected, "dimension").WithLocation(6, 42)
            };

            VerifyOperationTreeAndDiagnosticsForTest<StackAllocArrayCreationExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [Fact]
        public void StackAllocArrayCreationWithInitializer_UserDefinedType()
        {
            string source = @"
struct M { }

class C
{
    public void F()
    {
        var a = /*<bind>*/stackalloc M[] { new M() }/*</bind>*/;
    }
}
";
            string expectedOperationTree = @"
IOperation:  (OperationKind.None, Type: M*, IsInvalid) (Syntax: 'stackalloc  ... { new M() }')
  Children(2):
      ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1, IsInvalid, IsImplicit) (Syntax: 'stackalloc  ... { new M() }')
      IObjectCreationOperation (Constructor: M..ctor()) (OperationKind.ObjectCreation, Type: M, IsInvalid) (Syntax: 'new M()')
        Arguments(0)
        Initializer: 
          null
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // file.cs(8,27): error CS0214: Pointers and fixed size buffers may only be used in an unsafe context
                //         var a = /*<bind>*/stackalloc M[] { new M() }/*</bind>*/;
                Diagnostic(ErrorCode.ERR_UnsafeNeeded, "stackalloc M[] { new M() }").WithLocation(8, 27)
            };

            VerifyOperationTreeAndDiagnosticsForTest<StackAllocArrayCreationExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [Fact]
        public void StackAllocArrayCreationWithInitializer_ImplicitlyTyped()
        {
            string source = @"
struct M { }

class C
{
    public void F()
    {
        var a = /*<bind>*/stackalloc[] { new M() }/*</bind>*/;
    }
}
";
            string expectedOperationTree = @"
IOperation:  (OperationKind.None, Type: M*, IsInvalid) (Syntax: 'stackalloc[] { new M() }')
  Children(2):
      ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1, IsInvalid, IsImplicit) (Syntax: 'stackalloc[] { new M() }')
      IObjectCreationOperation (Constructor: M..ctor()) (OperationKind.ObjectCreation, Type: M, IsInvalid) (Syntax: 'new M()')
        Arguments(0)
        Initializer: 
          null
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // file.cs(8,27): error CS0214: Pointers and fixed size buffers may only be used in an unsafe context
                //         var a = /*<bind>*/stackalloc[] { new M() }/*</bind>*/;
                Diagnostic(ErrorCode.ERR_UnsafeNeeded, "stackalloc[] { new M() }").WithLocation(8, 27)
            };

            VerifyOperationTreeAndDiagnosticsForTest<ImplicitStackAllocArrayCreationExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [Fact]
        public void StackAllocArrayCreationWithInitializerErrorCase_ImplicitlyTypedWithoutInitializerAndDimension()
        {
            string source = @"
class C
{
    public void F(int dimension)
    {
        var x = /*<bind>*/stackalloc[]/*</bind>*/;
    }
}
";
            string expectedOperationTree = @"
IOperation:  (OperationKind.None, Type: ?*, IsInvalid) (Syntax: 'stackalloc[]/*</bind>*/')
  Children(1):
      ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 0, IsInvalid, IsImplicit) (Syntax: 'stackalloc[]/*</bind>*/')
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // file.cs(6,50): error CS1514: { expected
                //         var x = /*<bind>*/stackalloc[]/*</bind>*/;
                Diagnostic(ErrorCode.ERR_LbraceExpected, ";").WithLocation(6, 50),
                // file.cs(6,50): error CS1513: } expected
                //         var x = /*<bind>*/stackalloc[]/*</bind>*/;
                Diagnostic(ErrorCode.ERR_RbraceExpected, ";").WithLocation(6, 50),
                // file.cs(6,27): error CS0826: No best type found for implicitly-typed array
                //         var x = /*<bind>*/stackalloc[]/*</bind>*/;
                Diagnostic(ErrorCode.ERR_ImplicitlyTypedArrayNoBestType, "stackalloc[]/*</bind>*/").WithLocation(6, 27)
            };

            VerifyOperationTreeAndDiagnosticsForTest<ImplicitStackAllocArrayCreationExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [Fact]
        public void StackAllocArrayCreationWithInitializerErrorCase_ImplicitlyTypedWithoutInitializer()
        {
            string source = @"
class C
{
    public void F(int dimension)
    {
        var x = /*<bind>*/stackalloc[2]/*</bind>*/;
    }
}
";
            string expectedOperationTree = @"
IOperation:  (OperationKind.None, Type: ?*, IsInvalid) (Syntax: 'stackalloc[2]/*</bind>*/')
  Children(1):
      ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 0, IsInvalid, IsImplicit) (Syntax: 'stackalloc[2]/*</bind>*/')
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // file.cs(6,38): error CS8381: "Invalid rank specifier: expected ']'
                //         var x = /*<bind>*/stackalloc[2]/*</bind>*/;
                Diagnostic(ErrorCode.ERR_InvalidStackAllocArray, "2").WithLocation(6, 38),
                // file.cs(6,51): error CS1514: { expected
                //         var x = /*<bind>*/stackalloc[2]/*</bind>*/;
                Diagnostic(ErrorCode.ERR_LbraceExpected, ";").WithLocation(6, 51),
                // file.cs(6,51): error CS1513: } expected
                //         var x = /*<bind>*/stackalloc[2]/*</bind>*/;
                Diagnostic(ErrorCode.ERR_RbraceExpected, ";").WithLocation(6, 51),
                // file.cs(6,27): error CS0826: No best type found for implicitly-typed array
                //         var x = /*<bind>*/stackalloc[2]/*</bind>*/;
                Diagnostic(ErrorCode.ERR_ImplicitlyTypedArrayNoBestType, "stackalloc[2]/*</bind>*/").WithLocation(6, 27)
            };

            VerifyOperationTreeAndDiagnosticsForTest<ImplicitStackAllocArrayCreationExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [Fact]
        public void StackAllocArrayCreationWithInitializer_MultipleInitializersWithConversions()
        {
            string source = @"
class C
{
    public void F()
    {
        var a = 42;
        var b = /*<bind>*/stackalloc[] { 2, a, default }/*</bind>*/;
    }
}
";
            string expectedOperationTree = @"
IOperation:  (OperationKind.None, Type: System.Int32*, IsInvalid) (Syntax: 'stackalloc[ ... , default }')
  Children(4):
      ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 3, IsInvalid, IsImplicit) (Syntax: 'stackalloc[ ... , default }')
      ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 2, IsInvalid) (Syntax: '2')
      ILocalReferenceOperation: a (OperationKind.LocalReference, Type: System.Int32, IsInvalid) (Syntax: 'a')
      IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Int32, Constant: 0, IsInvalid, IsImplicit) (Syntax: 'default')
        Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        Operand: 
          IDefaultValueOperation (OperationKind.DefaultValue, Type: System.Int32, Constant: 0, IsInvalid) (Syntax: 'default')
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // file.cs(7,27): error CS0214: Pointers and fixed size buffers may only be used in an unsafe context
                //         var b = /*<bind>*/stackalloc[] { 2, a, default }/*</bind>*/;
                Diagnostic(ErrorCode.ERR_UnsafeNeeded, "stackalloc[] { 2, a, default }").WithLocation(7, 27)
            };

            VerifyOperationTreeAndDiagnosticsForTest<ImplicitStackAllocArrayCreationExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [Fact]
        public void StackAllocArrayCreationErrorCase_MissingDimension()
        {
            string source = @"
class C
{
    public void F()
    {
        var a = /*<bind>*/stackalloc int[]/*</bind>*/;
    }
}
";
            string expectedOperationTree = @"
IOperation:  (OperationKind.None, Type: System.Int32*, IsInvalid) (Syntax: 'stackalloc int[]')
  Children(1):
      IInvalidOperation (OperationKind.Invalid, Type: ?, IsInvalid) (Syntax: '')
        Children(0)
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // file.cs(6,41): error CS1586: Array creation must have array size or array initializer
                //         var a = /*<bind>*/stackalloc int[]/*</bind>*/;
                Diagnostic(ErrorCode.ERR_MissingArraySize, "[]").WithLocation(6, 41)
            };

            VerifyOperationTreeAndDiagnosticsForTest<StackAllocArrayCreationExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [Fact]
        public void StackAllocArrayCreationErrorCase_InvalidInitializer()
        {
            string source = @"
class C
{
    public void F()
    {
        var a = /*<bind>*/stackalloc int[] { 1 }/*</bind>*/;
    }
}
";
            string expectedOperationTree = @"
IOperation:  (OperationKind.None, Type: System.Int32*, IsInvalid) (Syntax: 'stackalloc int[] { 1 }')
  Children(2):
      ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1, IsInvalid, IsImplicit) (Syntax: 'stackalloc int[] { 1 }')
      ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1, IsInvalid) (Syntax: '1')
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // file.cs(6,27): error CS0214: Pointers and fixed size buffers may only be used in an unsafe context
                //         var a = /*<bind>*/stackalloc int[] { 1 }/*</bind>*/;
                Diagnostic(ErrorCode.ERR_UnsafeNeeded, "stackalloc int[] { 1 }").WithLocation(6, 27)
            };

            VerifyOperationTreeAndDiagnosticsForTest<StackAllocArrayCreationExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [Fact]
        public void StackAllocArrayCreationErrorCase_MissingExplicitCast()
        {
            string source = @"
class C
{
    public void F(object b)
    {
        var a = /*<bind>*/stackalloc int[b]/*</bind>*/;
    }
}
";
            string expectedOperationTree = @"
IOperation:  (OperationKind.None, Type: System.Int32*, IsInvalid) (Syntax: 'stackalloc int[b]')
  Children(1):
      IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Int32, IsInvalid, IsImplicit) (Syntax: 'b')
        Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        Operand: 
          IParameterReferenceOperation: b (OperationKind.ParameterReference, Type: System.Object, IsInvalid) (Syntax: 'b')
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // file.cs(6,42): error CS0266: Cannot implicitly convert type 'object' to 'int'. An explicit conversion exists (are you missing a cast?)
                //         var a = /*<bind>*/stackalloc int[b]/*</bind>*/;
                Diagnostic(ErrorCode.ERR_NoImplicitConvCast, "b").WithArguments("object", "int").WithLocation(6, 42),
                // file.cs(6,27): error CS0214: Pointers and fixed size buffers may only be used in an unsafe context
                //         var a = /*<bind>*/stackalloc int[b]/*</bind>*/;
                Diagnostic(ErrorCode.ERR_UnsafeNeeded, "stackalloc int[b]").WithLocation(6, 27)
            };

            VerifyOperationTreeAndDiagnosticsForTest<StackAllocArrayCreationExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [Fact]
        public void StackAllocArrayCreation_InvocationExpressionAsDimension()
        {
            string source = @"
class C
{
    public void F()
    {
        var a = /*<bind>*/stackalloc int[M()]/*</bind>*/;
    }

    public int M() => 1;
}
";
            string expectedOperationTree = @"
IOperation:  (OperationKind.None, Type: System.Int32*, IsInvalid) (Syntax: 'stackalloc int[M()]')
  Children(1):
      IInvocationOperation ( System.Int32 C.M()) (OperationKind.Invocation, Type: System.Int32, IsInvalid) (Syntax: 'M()')
        Instance Receiver: 
          IInstanceReferenceOperation (ReferenceKind: ContainingTypeInstance) (OperationKind.InstanceReference, Type: C, IsInvalid, IsImplicit) (Syntax: 'M')
        Arguments(0)
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // file.cs(6,27): error CS0214: Pointers and fixed size buffers may only be used in an unsafe context
                //         var a = /*<bind>*/stackalloc int[M()]/*</bind>*/;
                Diagnostic(ErrorCode.ERR_UnsafeNeeded, "stackalloc int[M()]").WithLocation(6, 27)
            };

            VerifyOperationTreeAndDiagnosticsForTest<StackAllocArrayCreationExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [Fact]
        public void StackAllocArrayCreation_InvocationExpressionWithConversionAsDimension()
        {
            string source = @"
class C
{
    public void F()
    {
        var a = /*<bind>*/stackalloc int[(int)M()]/*</bind>*/;
    }

    public object M() => null;
}
";
            string expectedOperationTree = @"
IOperation:  (OperationKind.None, Type: System.Int32*, IsInvalid) (Syntax: 'stackalloc int[(int)M()]')
  Children(1):
      IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Int32, IsInvalid) (Syntax: '(int)M()')
        Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        Operand: 
          IInvocationOperation ( System.Object C.M()) (OperationKind.Invocation, Type: System.Object, IsInvalid) (Syntax: 'M()')
            Instance Receiver: 
              IInstanceReferenceOperation (ReferenceKind: ContainingTypeInstance) (OperationKind.InstanceReference, Type: C, IsInvalid, IsImplicit) (Syntax: 'M')
            Arguments(0)
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // file.cs(6,27): error CS0214: Pointers and fixed size buffers may only be used in an unsafe context
                //         var a = /*<bind>*/stackalloc int[(int)M()]/*</bind>*/;
                Diagnostic(ErrorCode.ERR_UnsafeNeeded, "stackalloc int[(int)M()]").WithLocation(6, 27)
            };

            VerifyOperationTreeAndDiagnosticsForTest<StackAllocArrayCreationExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [Fact]
        public void StackAllocArrayCreationErrorCase_InvocationExpressionAsDimension()
        {
            string source = @"
class C
{
    public static void F()
    {
        var a = /*<bind>*/stackalloc int[M()]/*</bind>*/;
    }

    public object M() => null;
}
";
            string expectedOperationTree = @"
    IOperation:  (OperationKind.None, Type: System.Int32*, IsInvalid) (Syntax: 'stackalloc int[M()]')
      Children(1):
          IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Int32, IsInvalid, IsImplicit) (Syntax: 'M()')
            Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
            Operand: 
              IInvalidOperation (OperationKind.Invalid, Type: System.Object, IsInvalid) (Syntax: 'M()')
                Children(0)
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // file.cs(6,42): error CS0120: An object reference is required for the non-static field, method, or property 'C.M()'
                //         var a = /*<bind>*/stackalloc int[M()]/*</bind>*/;
                Diagnostic(ErrorCode.ERR_ObjectRequired, "M").WithArguments("C.M()").WithLocation(6, 42)
            };

            VerifyOperationTreeAndDiagnosticsForTest<StackAllocArrayCreationExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [Fact]
        public void StackAllocArrayCreationErrorCase_InvocationExpressionWithConversionAsDimension()
        {
            string source = @"
class C
{
    public void F()
    {
        var a = /*<bind>*/stackalloc int[(int)M()]/*</bind>*/;
    }

    public C M() => new C();
}
";
            string expectedOperationTree = @"
IOperation:  (OperationKind.None, Type: System.Int32*, IsInvalid) (Syntax: 'stackalloc int[(int)M()]')
  Children(1):
      IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Int32, IsInvalid) (Syntax: '(int)M()')
        Conversion: CommonConversion (Exists: False, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        Operand: 
          IInvocationOperation ( C C.M()) (OperationKind.Invocation, Type: C, IsInvalid) (Syntax: 'M()')
            Instance Receiver: 
              IInstanceReferenceOperation (ReferenceKind: ContainingTypeInstance) (OperationKind.InstanceReference, Type: C, IsInvalid, IsImplicit) (Syntax: 'M')
            Arguments(0)
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // file.cs(6,42): error CS0030: Cannot convert type 'C' to 'int'
                //         var a = /*<bind>*/stackalloc int[(int)M()]/*</bind>*/;
                Diagnostic(ErrorCode.ERR_NoExplicitConv, "(int)M()").WithArguments("C", "int").WithLocation(6, 42)
            };

            VerifyOperationTreeAndDiagnosticsForTest<StackAllocArrayCreationExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [Fact]
        public void SimpleStackAllocArrayCreation_ConstantConversion()
        {
            string source = @"
class C
{
    public void F()
    {
        var a = /*<bind>*/stackalloc int[0.0]/*</bind>*/;
    }
}
";
            string expectedOperationTree = @"
IOperation:  (OperationKind.None, Type: System.Int32*, IsInvalid) (Syntax: 'stackalloc int[0.0]')
  Children(1):
      IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Int32, Constant: 0, IsInvalid, IsImplicit) (Syntax: '0.0')
        Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: True, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        Operand: 
          ILiteralOperation (OperationKind.Literal, Type: System.Double, Constant: 0, IsInvalid) (Syntax: '0.0')
";
            var expectedDiagnostics = new DiagnosticDescription[] {
               // file.cs(6,42): error CS0266: Cannot implicitly convert type 'double' to 'int'. An explicit conversion exists (are you missing a cast?)
                //         var a = /*<bind>*/stackalloc int[0.0]/*</bind>*/;
                Diagnostic(ErrorCode.ERR_NoImplicitConvCast, "0.0").WithArguments("double", "int").WithLocation(6, 42),
                // file.cs(6,27): error CS0214: Pointers and fixed size buffers may only be used in an unsafe context
                //         var a = /*<bind>*/stackalloc int[0.0]/*</bind>*/;
                Diagnostic(ErrorCode.ERR_UnsafeNeeded, "stackalloc int[0.0]").WithLocation(6, 27)
            };

            VerifyOperationTreeAndDiagnosticsForTest<StackAllocArrayCreationExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }
    }
}
