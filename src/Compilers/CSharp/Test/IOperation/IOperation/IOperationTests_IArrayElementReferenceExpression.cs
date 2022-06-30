// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    public class IOperationTests_IArrayElementReferenceExpression : SemanticModelTestBase
    {
        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact, WorkItem(22006, "https://github.com/dotnet/roslyn/issues/22006")]
        public void ArrayElementReference_SingleDimensionArray_ConstantIndex()
        {
            string source = @"
class C
{
    public void F(string[] args)
    {
        var a = /*<bind>*/args[0]/*</bind>*/;
    }
}
";
            string expectedOperationTree = @"
IArrayElementReferenceOperation (OperationKind.ArrayElementReference, Type: System.String) (Syntax: 'args[0]')
  Array reference: 
    IParameterReferenceOperation: args (OperationKind.ParameterReference, Type: System.String[]) (Syntax: 'args')
  Indices(1):
      ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 0) (Syntax: '0')
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<ElementAccessExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact, WorkItem(22006, "https://github.com/dotnet/roslyn/issues/22006")]
        public void ArrayElementReference_SingleDimensionArray_NonConstantIndex()
        {
            string source = @"
class C
{
    public void F(string[] args, int x)
    {
        var a = /*<bind>*/args[x]/*</bind>*/;
    }
}
";
            string expectedOperationTree = @"
IArrayElementReferenceOperation (OperationKind.ArrayElementReference, Type: System.String) (Syntax: 'args[x]')
  Array reference: 
    IParameterReferenceOperation: args (OperationKind.ParameterReference, Type: System.String[]) (Syntax: 'args')
  Indices(1):
      IParameterReferenceOperation: x (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'x')
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<ElementAccessExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact, WorkItem(22006, "https://github.com/dotnet/roslyn/issues/22006")]
        public void ArrayElementReference_SingleDimensionArray_FunctionCallArrayReference()
        {
            string source = @"
class C
{
    public void F()
    {
        var a = /*<bind>*/F2()[0]/*</bind>*/;
    }

    public string[] F2() => null;
}
";
            string expectedOperationTree = @"
IArrayElementReferenceOperation (OperationKind.ArrayElementReference, Type: System.String) (Syntax: 'F2()[0]')
  Array reference: 
    IInvocationOperation ( System.String[] C.F2()) (OperationKind.Invocation, Type: System.String[]) (Syntax: 'F2()')
      Instance Receiver: 
        IInstanceReferenceOperation (ReferenceKind: ContainingTypeInstance) (OperationKind.InstanceReference, Type: C, IsImplicit) (Syntax: 'F2')
      Arguments(0)
  Indices(1):
      ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 0) (Syntax: '0')
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<ElementAccessExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact, WorkItem(22006, "https://github.com/dotnet/roslyn/issues/22006")]
        public void ArrayElementReference_MultiDimensionArray_ConstantIndices()
        {
            string source = @"
class C
{
    public void F(string[,] args)
    {
        var a = /*<bind>*/args[0, 1]/*</bind>*/;
    }
}
";
            string expectedOperationTree = @"
IArrayElementReferenceOperation (OperationKind.ArrayElementReference, Type: System.String) (Syntax: 'args[0, 1]')
  Array reference: 
    IParameterReferenceOperation: args (OperationKind.ParameterReference, Type: System.String[,]) (Syntax: 'args')
  Indices(2):
      ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 0) (Syntax: '0')
      ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<ElementAccessExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact, WorkItem(22006, "https://github.com/dotnet/roslyn/issues/22006")]
        public void ArrayElementReference_MultiDimensionArray_NonConstantIndices()
        {
            string source = @"
class C
{
    public void F(string[,] args, int x, int y)
    {
        var a = /*<bind>*/args[x, y]/*</bind>*/;
    }
}
";
            string expectedOperationTree = @"
IArrayElementReferenceOperation (OperationKind.ArrayElementReference, Type: System.String) (Syntax: 'args[x, y]')
  Array reference: 
    IParameterReferenceOperation: args (OperationKind.ParameterReference, Type: System.String[,]) (Syntax: 'args')
  Indices(2):
      IParameterReferenceOperation: x (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'x')
      IParameterReferenceOperation: y (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'y')
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<ElementAccessExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact, WorkItem(22006, "https://github.com/dotnet/roslyn/issues/22006")]
        public void ArrayElementReference_MultiDimensionArray_InvocationInIndex()
        {
            string source = @"
class C
{
    public void F(string[,] args)
    {
        int x = 0;
        var a = /*<bind>*/args[x, F2()]/*</bind>*/;
    }

    public int F2() => 0;
}
";
            string expectedOperationTree = @"
IArrayElementReferenceOperation (OperationKind.ArrayElementReference, Type: System.String) (Syntax: 'args[x, F2()]')
  Array reference: 
    IParameterReferenceOperation: args (OperationKind.ParameterReference, Type: System.String[,]) (Syntax: 'args')
  Indices(2):
      ILocalReferenceOperation: x (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'x')
      IInvocationOperation ( System.Int32 C.F2()) (OperationKind.Invocation, Type: System.Int32) (Syntax: 'F2()')
        Instance Receiver: 
          IInstanceReferenceOperation (ReferenceKind: ContainingTypeInstance) (OperationKind.InstanceReference, Type: C, IsImplicit) (Syntax: 'F2')
        Arguments(0)
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<ElementAccessExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact, WorkItem(22006, "https://github.com/dotnet/roslyn/issues/22006")]
        public void ArrayElementReference_JaggedArray_ConstantIndices()
        {
            string source = @"
class C
{
    public void F(string[][] args)
    {
        var a = /*<bind>*/args[0][0]/*</bind>*/;
    }
}
";
            string expectedOperationTree = @"
IArrayElementReferenceOperation (OperationKind.ArrayElementReference, Type: System.String) (Syntax: 'args[0][0]')
  Array reference: 
    IArrayElementReferenceOperation (OperationKind.ArrayElementReference, Type: System.String[]) (Syntax: 'args[0]')
      Array reference: 
        IParameterReferenceOperation: args (OperationKind.ParameterReference, Type: System.String[][]) (Syntax: 'args')
      Indices(1):
          ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 0) (Syntax: '0')
  Indices(1):
      ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 0) (Syntax: '0')
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<ElementAccessExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact, WorkItem(22006, "https://github.com/dotnet/roslyn/issues/22006")]
        public void ArrayElementReference_JaggedArray_NonConstantIndices()
        {
            string source = @"
class C
{
    public void F(string[][] args)
    {
        int x = 0;
        var a = /*<bind>*/args[F2()][x]/*</bind>*/;
    }

    public int F2() => 0;
}
";
            string expectedOperationTree = @"
IArrayElementReferenceOperation (OperationKind.ArrayElementReference, Type: System.String) (Syntax: 'args[F2()][x]')
  Array reference: 
    IArrayElementReferenceOperation (OperationKind.ArrayElementReference, Type: System.String[]) (Syntax: 'args[F2()]')
      Array reference: 
        IParameterReferenceOperation: args (OperationKind.ParameterReference, Type: System.String[][]) (Syntax: 'args')
      Indices(1):
          IInvocationOperation ( System.Int32 C.F2()) (OperationKind.Invocation, Type: System.Int32) (Syntax: 'F2()')
            Instance Receiver: 
              IInstanceReferenceOperation (ReferenceKind: ContainingTypeInstance) (OperationKind.InstanceReference, Type: C, IsImplicit) (Syntax: 'F2')
            Arguments(0)
  Indices(1):
      ILocalReferenceOperation: x (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'x')
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<ElementAccessExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact, WorkItem(22006, "https://github.com/dotnet/roslyn/issues/22006")]
        public void ArrayElementReference_JaggedArrayOfMultidimensionalArrays()
        {
            string source = @"
class C
{
    public void F(string[][,] args)
    {
        int x = 0;
        var a = /*<bind>*/args[x][0, F2()]/*</bind>*/;
    }

    public int F2() => 0;
}
";
            string expectedOperationTree = @"
IArrayElementReferenceOperation (OperationKind.ArrayElementReference, Type: System.String) (Syntax: 'args[x][0, F2()]')
  Array reference: 
    IArrayElementReferenceOperation (OperationKind.ArrayElementReference, Type: System.String[,]) (Syntax: 'args[x]')
      Array reference: 
        IParameterReferenceOperation: args (OperationKind.ParameterReference, Type: System.String[][,]) (Syntax: 'args')
      Indices(1):
          ILocalReferenceOperation: x (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'x')
  Indices(2):
      ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 0) (Syntax: '0')
      IInvocationOperation ( System.Int32 C.F2()) (OperationKind.Invocation, Type: System.Int32) (Syntax: 'F2()')
        Instance Receiver: 
          IInstanceReferenceOperation (ReferenceKind: ContainingTypeInstance) (OperationKind.InstanceReference, Type: C, IsImplicit) (Syntax: 'F2')
        Arguments(0)
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<ElementAccessExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact, WorkItem(22006, "https://github.com/dotnet/roslyn/issues/22006")]
        public void ArrayElementReference_ImplicitConversionInIndexExpression()
        {
            string source = @"
class C
{
    public void F(string[] args, byte b)
    {
        var a = /*<bind>*/args[b]/*</bind>*/;
    }
}
";
            string expectedOperationTree = @"
IArrayElementReferenceOperation (OperationKind.ArrayElementReference, Type: System.String) (Syntax: 'args[b]')
  Array reference: 
    IParameterReferenceOperation: args (OperationKind.ParameterReference, Type: System.String[]) (Syntax: 'args')
  Indices(1):
      IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Int32, IsImplicit) (Syntax: 'b')
        Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: True, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        Operand: 
          IParameterReferenceOperation: b (OperationKind.ParameterReference, Type: System.Byte) (Syntax: 'b')
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<ElementAccessExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact, WorkItem(22006, "https://github.com/dotnet/roslyn/issues/22006")]
        public void ArrayElementReference_ExplicitConversionInIndexExpression()
        {
            string source = @"
class C
{
    public void F(string[] args, double d)
    {
        var a = /*<bind>*/args[(int)d]/*</bind>*/;
    }
}
";
            string expectedOperationTree = @"
IArrayElementReferenceOperation (OperationKind.ArrayElementReference, Type: System.String) (Syntax: 'args[(int)d]')
  Array reference: 
    IParameterReferenceOperation: args (OperationKind.ParameterReference, Type: System.String[]) (Syntax: 'args')
  Indices(1):
      IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Int32) (Syntax: '(int)d')
        Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: True, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        Operand: 
          IParameterReferenceOperation: d (OperationKind.ParameterReference, Type: System.Double) (Syntax: 'd')
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<ElementAccessExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact, WorkItem(22006, "https://github.com/dotnet/roslyn/issues/22006")]
        public void ArrayElementReference_ImplicitUserDefinedConversionInIndexExpression()
        {
            string source = @"
class C
{
    public void F(string[] args, C c)
    {
        var a = /*<bind>*/args[c]/*</bind>*/;
    }

    public static implicit operator int(C c)
    {
        return 0;
    }
}
";
            string expectedOperationTree = @"
IArrayElementReferenceOperation (OperationKind.ArrayElementReference, Type: System.String) (Syntax: 'args[c]')
  Array reference: 
    IParameterReferenceOperation: args (OperationKind.ParameterReference, Type: System.String[]) (Syntax: 'args')
  Indices(1):
      IConversionOperation (TryCast: False, Unchecked) (OperatorMethod: System.Int32 C.op_Implicit(C c)) (OperationKind.Conversion, Type: System.Int32, IsImplicit) (Syntax: 'c')
        Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: True) (MethodSymbol: System.Int32 C.op_Implicit(C c))
        Operand: 
          IParameterReferenceOperation: c (OperationKind.ParameterReference, Type: C) (Syntax: 'c')
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<ElementAccessExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact, WorkItem(22006, "https://github.com/dotnet/roslyn/issues/22006")]
        public void ArrayElementReference_ExplicitUserDefinedConversionInIndexExpression()
        {
            string source = @"
class C
{
    public void F(string[] args, C c)
    {
        var a = /*<bind>*/args[(int)c]/*</bind>*/;
    }

    public static explicit operator int(C c)
    {
        return 0;
    }
}
";
            string expectedOperationTree = @"
IArrayElementReferenceOperation (OperationKind.ArrayElementReference, Type: System.String) (Syntax: 'args[(int)c]')
  Array reference: 
    IParameterReferenceOperation: args (OperationKind.ParameterReference, Type: System.String[]) (Syntax: 'args')
  Indices(1):
      IConversionOperation (TryCast: False, Unchecked) (OperatorMethod: System.Int32 C.op_Explicit(C c)) (OperationKind.Conversion, Type: System.Int32) (Syntax: '(int)c')
        Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: True) (MethodSymbol: System.Int32 C.op_Explicit(C c))
        Operand: 
          IParameterReferenceOperation: c (OperationKind.ParameterReference, Type: C) (Syntax: 'c')
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<ElementAccessExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact, WorkItem(22006, "https://github.com/dotnet/roslyn/issues/22006")]
        public void ArrayElementReference_ExplicitUserDefinedConversionInArrayReference()
        {
            string source = @"
class C
{
    public void F(C c, int x)
    {
        var a = /*<bind>*/((string[])c)[x]/*</bind>*/;
    }

    public static explicit operator string[](C c)
    {
        return null;
    }
}
";
            string expectedOperationTree = @"
IArrayElementReferenceOperation (OperationKind.ArrayElementReference, Type: System.String) (Syntax: '((string[])c)[x]')
  Array reference: 
    IConversionOperation (TryCast: False, Unchecked) (OperatorMethod: System.String[] C.op_Explicit(C c)) (OperationKind.Conversion, Type: System.String[]) (Syntax: '(string[])c')
      Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: True) (MethodSymbol: System.String[] C.op_Explicit(C c))
      Operand: 
        IParameterReferenceOperation: c (OperationKind.ParameterReference, Type: C) (Syntax: 'c')
  Indices(1):
      IParameterReferenceOperation: x (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'x')
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<ElementAccessExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact, WorkItem(22006, "https://github.com/dotnet/roslyn/issues/22006")]
        public void ArrayElementReferenceError_NoConversionInIndexExpression()
        {
            string source = @"
class C
{
    public void F(string[] args, C c)
    {
        var a = /*<bind>*/args[c]/*</bind>*/;
    }
}
";
            string expectedOperationTree = @"
IArrayElementReferenceOperation (OperationKind.ArrayElementReference, Type: System.String, IsInvalid) (Syntax: 'args[c]')
  Array reference: 
    IParameterReferenceOperation: args (OperationKind.ParameterReference, Type: System.String[]) (Syntax: 'args')
  Indices(1):
      IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Int32, IsInvalid, IsImplicit) (Syntax: 'c')
        Conversion: CommonConversion (Exists: False, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        Operand: 
          IParameterReferenceOperation: c (OperationKind.ParameterReference, Type: C, IsInvalid) (Syntax: 'c')
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS0029: Cannot implicitly convert type 'C' to 'int'
                //         var a = /*<bind>*/args[c]/*</bind>*/;
                Diagnostic(ErrorCode.ERR_NoImplicitConv, "c").WithArguments("C", "int").WithLocation(6, 32)
            };

            VerifyOperationTreeAndDiagnosticsForTest<ElementAccessExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact, WorkItem(22006, "https://github.com/dotnet/roslyn/issues/22006")]
        public void ArrayElementReferenceError_MissingExplicitCastInIndexExpression()
        {
            string source = @"
class C
{
    public void F(string[] args, C c)
    {
        var a = /*<bind>*/args[c]/*</bind>*/;
    }

    public static explicit operator int(C c)
    {
        return 0;
    }
}
";
            string expectedOperationTree = @"
IArrayElementReferenceOperation (OperationKind.ArrayElementReference, Type: System.String, IsInvalid) (Syntax: 'args[c]')
  Array reference: 
    IParameterReferenceOperation: args (OperationKind.ParameterReference, Type: System.String[]) (Syntax: 'args')
  Indices(1):
      IConversionOperation (TryCast: False, Unchecked) (OperatorMethod: System.Int32 C.op_Explicit(C c)) (OperationKind.Conversion, Type: System.Int32, IsInvalid, IsImplicit) (Syntax: 'c')
        Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: True) (MethodSymbol: System.Int32 C.op_Explicit(C c))
        Operand: 
          IParameterReferenceOperation: c (OperationKind.ParameterReference, Type: C, IsInvalid) (Syntax: 'c')
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS0266: Cannot implicitly convert type 'C' to 'int'. An explicit conversion exists (are you missing a cast?)
                //         var a = /*<bind>*/args[c]/*</bind>*/;
                Diagnostic(ErrorCode.ERR_NoImplicitConvCast, "c").WithArguments("C", "int").WithLocation(6, 32)
            };

            VerifyOperationTreeAndDiagnosticsForTest<ElementAccessExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact, WorkItem(22006, "https://github.com/dotnet/roslyn/issues/22006")]
        public void ArrayElementReferenceError_NoArrayReference()
        {
            string source = @"
class C
{
    public void F()
    {
        var a = /*<bind>*/[0]/*</bind>*/;
    }

    public string[] F2() => null;
}
";
            string expectedOperationTree = @"
IInvalidOperation (OperationKind.Invalid, Type: ?, IsInvalid) (Syntax: '[0]')
  Children(2):
      ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 0, IsInvalid) (Syntax: '0')
      IInvalidOperation (OperationKind.Invalid, Type: null, IsInvalid) (Syntax: '')
        Children(0)
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS1525: Invalid expression term '['
                //         var a = /*<bind>*/[0]/*</bind>*/;
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, "[").WithArguments("[").WithLocation(6, 27)
            };

            VerifyOperationTreeAndDiagnosticsForTest<ElementAccessExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact, WorkItem(22006, "https://github.com/dotnet/roslyn/issues/22006")]
        public void ArrayElementReferenceError_NoIndices()
        {
            string source = @"
class C
{
    public void F(string[] args)
    {
        var a = /*<bind>*/args[]/*</bind>*/;
    }
}
";
            string expectedOperationTree = @"
IArrayElementReferenceOperation (OperationKind.ArrayElementReference, Type: System.String, IsInvalid) (Syntax: 'args[]')
  Array reference: 
    IParameterReferenceOperation: args (OperationKind.ParameterReference, Type: System.String[]) (Syntax: 'args')
  Indices(1):
      IInvalidOperation (OperationKind.Invalid, Type: null, IsInvalid) (Syntax: '')
        Children(0)
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS0443: Syntax error; value expected
                //         var a = /*<bind>*/args[]/*</bind>*/;
                Diagnostic(ErrorCode.ERR_ValueExpected, "]").WithLocation(6, 32)
            };

            VerifyOperationTreeAndDiagnosticsForTest<ElementAccessExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact, WorkItem(22006, "https://github.com/dotnet/roslyn/issues/22006")]
        public void ArrayElementReferenceError_BadIndexing()
        {
            string source = @"
class C
{
    public void F(C c)
    {
        var a = /*<bind>*/c[0]/*</bind>*/;
    }

    public string[] F2() => null;
}
";
            string expectedOperationTree = @"
IInvalidOperation (OperationKind.Invalid, Type: ?, IsInvalid) (Syntax: 'c[0]')
  Children(2):
      ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 0, IsInvalid) (Syntax: '0')
      IParameterReferenceOperation: c (OperationKind.ParameterReference, Type: C, IsInvalid) (Syntax: 'c')
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS0021: Cannot apply indexing with [] to an expression of type 'C'
                //         var a = /*<bind>*/c[0]/*</bind>*/;
                Diagnostic(ErrorCode.ERR_BadIndexLHS, "c[0]").WithArguments("C").WithLocation(6, 27)
            };

            VerifyOperationTreeAndDiagnosticsForTest<ElementAccessExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact, WorkItem(22006, "https://github.com/dotnet/roslyn/issues/22006")]
        public void ArrayElementReferenceError_BadIndexCount()
        {
            string source = @"
class C
{
    public void F(string[] args)
    {
        var a = /*<bind>*/args[0, 0]/*</bind>*/;
    }
}
";
            string expectedOperationTree = @"
IArrayElementReferenceOperation (OperationKind.ArrayElementReference, Type: System.String, IsInvalid) (Syntax: 'args[0, 0]')
  Array reference: 
    IParameterReferenceOperation: args (OperationKind.ParameterReference, Type: System.String[], IsInvalid) (Syntax: 'args')
  Indices(2):
      ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 0, IsInvalid) (Syntax: '0')
      ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 0, IsInvalid) (Syntax: '0')
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS0022: Wrong number of indices inside []; expected 1
                //         var a = /*<bind>*/args[0, 0]/*</bind>*/;
                Diagnostic(ErrorCode.ERR_BadIndexCount, "args[0, 0]").WithArguments("1").WithLocation(6, 27)
            };

            VerifyOperationTreeAndDiagnosticsForTest<ElementAccessExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact, WorkItem(22006, "https://github.com/dotnet/roslyn/issues/22006")]
        public void ArrayElementReferenceError_ExtraElementAccessOperator()
        {
            string source = @"
class C
{
    public void F(string[] args)
    {
        var a = /*<bind>*/args[0][]/*</bind>*/;
    }
}
";
            string expectedOperationTree = @"
IInvalidOperation (OperationKind.Invalid, Type: System.Char, IsInvalid) (Syntax: 'args[0][]')
  Children(2):
      IArrayElementReferenceOperation (OperationKind.ArrayElementReference, Type: System.String) (Syntax: 'args[0]')
        Array reference: 
          IParameterReferenceOperation: args (OperationKind.ParameterReference, Type: System.String[]) (Syntax: 'args')
        Indices(1):
            ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 0) (Syntax: '0')
      IInvalidOperation (OperationKind.Invalid, Type: null, IsInvalid) (Syntax: '')
        Children(0)
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS0443: Syntax error; value expected
                //         var a = /*<bind>*/args[0][]/*</bind>*/;
                Diagnostic(ErrorCode.ERR_ValueExpected, "]").WithLocation(6, 35)
            };

            VerifyOperationTreeAndDiagnosticsForTest<ElementAccessExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact, WorkItem(22006, "https://github.com/dotnet/roslyn/issues/22006")]
        public void ArrayElementReferenceError_IndexErrorExpression()
        {
            string source = @"
class C
{
    public void F()
    {
        var a = /*<bind>*/ErrorExpression[0]/*</bind>*/;
    }
}
";
            string expectedOperationTree = @"
IInvalidOperation (OperationKind.Invalid, Type: ?, IsInvalid) (Syntax: 'ErrorExpression[0]')
  Children(2):
      ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 0) (Syntax: '0')
      IInvalidOperation (OperationKind.Invalid, Type: ?, IsInvalid) (Syntax: 'ErrorExpression')
        Children(0)
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS0103: The name 'ErrorExpression' does not exist in the current context
                //         var a = /*<bind>*/ErrorExpression[0]/*</bind>*/;
                Diagnostic(ErrorCode.ERR_NameNotInContext, "ErrorExpression").WithArguments("ErrorExpression").WithLocation(6, 27)
            };

            VerifyOperationTreeAndDiagnosticsForTest<ElementAccessExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact, WorkItem(22006, "https://github.com/dotnet/roslyn/issues/22006")]
        public void ArrayElementReferenceError_InvalidIndexerExpression()
        {
            string source = @"
class C
{
    public void F(string[] args)
    {
        var a = /*<bind>*/args[ErrorExpression]/*</bind>*/;
    }
}
";
            string expectedOperationTree = @"
IArrayElementReferenceOperation (OperationKind.ArrayElementReference, Type: System.String, IsInvalid) (Syntax: 'args[ErrorExpression]')
  Array reference: 
    IParameterReferenceOperation: args (OperationKind.ParameterReference, Type: System.String[]) (Syntax: 'args')
  Indices(1):
      IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Int32, IsInvalid, IsImplicit) (Syntax: 'ErrorExpression')
        Conversion: CommonConversion (Exists: False, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        Operand: 
          IInvalidOperation (OperationKind.Invalid, Type: ?, IsInvalid) (Syntax: 'ErrorExpression')
            Children(0)
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS0103: The name 'ErrorExpression' does not exist in the current context
                //         var a = /*<bind>*/args[ErrorExpression]/*</bind>*/;
                Diagnostic(ErrorCode.ERR_NameNotInContext, "ErrorExpression").WithArguments("ErrorExpression").WithLocation(6, 32)
            };

            VerifyOperationTreeAndDiagnosticsForTest<ElementAccessExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact, WorkItem(22006, "https://github.com/dotnet/roslyn/issues/22006")]
        public void ArrayElementReferenceError_SyntaxErrorInIndexer_MissingValue()
        {
            string source = @"
class C
{
    public void F(string[] args)
    {
        var a = /*<bind>*/args[0,]/*</bind>*/;
    }
}
";
            string expectedOperationTree = @"
IArrayElementReferenceOperation (OperationKind.ArrayElementReference, Type: System.String, IsInvalid) (Syntax: 'args[0,]')
  Array reference: 
    IParameterReferenceOperation: args (OperationKind.ParameterReference, Type: System.String[]) (Syntax: 'args')
  Indices(2):
      ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 0) (Syntax: '0')
      IInvalidOperation (OperationKind.Invalid, Type: null, IsInvalid) (Syntax: '')
        Children(0)
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS0443: Syntax error; value expected
                //         var a = /*<bind>*/args[0,]/*</bind>*/;
                Diagnostic(ErrorCode.ERR_ValueExpected, "]").WithLocation(6, 34)
            };

            VerifyOperationTreeAndDiagnosticsForTest<ElementAccessExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact, WorkItem(22006, "https://github.com/dotnet/roslyn/issues/22006")]
        public void ArrayElementReferenceError_SyntaxErrorInIndexer_MissingBracket()
        {
            string source = @"
class C
{
    public void F(string[] args)
    {
        var a = /*<bind>*/args[/*</bind>*/;
    }
}
";
            string expectedOperationTree = @"
IArrayElementReferenceOperation (OperationKind.ArrayElementReference, Type: System.String, IsInvalid) (Syntax: 'args[/*</bind>*/')
  Array reference: 
    IParameterReferenceOperation: args (OperationKind.ParameterReference, Type: System.String[], IsInvalid) (Syntax: 'args')
  Indices(0)
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS1003: Syntax error, ']' expected
                //         var a = /*<bind>*/args[/*</bind>*/;
                Diagnostic(ErrorCode.ERR_SyntaxError, ";").WithArguments("]").WithLocation(6, 43),
                // CS0022: Wrong number of indices inside []; expected 1
                //         var a = /*<bind>*/args[/*</bind>*/;
                Diagnostic(ErrorCode.ERR_BadIndexCount, "args[/*</bind>*/").WithArguments("1").WithLocation(6, 27)
            };

            VerifyOperationTreeAndDiagnosticsForTest<ElementAccessExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact, WorkItem(22006, "https://github.com/dotnet/roslyn/issues/22006")]
        public void ArrayElementReferenceError_SyntaxErrorInIndexer_MissingBracketAfterIndex()
        {
            string source = @"
class C
{
    public void F(string[] args)
    {
        var a = /*<bind>*/args[0/*</bind>*/;
    }
}
";
            string expectedOperationTree = @"
IArrayElementReferenceOperation (OperationKind.ArrayElementReference, Type: System.String, IsInvalid) (Syntax: 'args[0/*</bind>*/')
  Array reference: 
    IParameterReferenceOperation: args (OperationKind.ParameterReference, Type: System.String[]) (Syntax: 'args')
  Indices(1):
      ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 0) (Syntax: '0')
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS1003: Syntax error, ']' expected
                //         var a = /*<bind>*/args[0/*</bind>*/;
                Diagnostic(ErrorCode.ERR_SyntaxError, ";").WithArguments("]").WithLocation(6, 44)
            };

            VerifyOperationTreeAndDiagnosticsForTest<ElementAccessExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact, WorkItem(22006, "https://github.com/dotnet/roslyn/issues/22006")]
        public void ArrayElementReferenceError_SyntaxErrorInIndexer_DeeplyNestedParameterReference()
        {
            string source = @"
class C
{
    public void F(string[] args, int x, int y)
    {
        var a = /*<bind>*/args[y][][][][x]/*</bind>*/;
    }
}
";
            string expectedOperationTree = @"
IInvalidOperation (OperationKind.Invalid, Type: ?, IsInvalid) (Syntax: 'args[y][][][][x]')
  Children(2):
      IParameterReferenceOperation: x (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'x')
      IInvalidOperation (OperationKind.Invalid, Type: ?, IsInvalid) (Syntax: 'args[y][][][]')
        Children(2):
            IInvalidOperation (OperationKind.Invalid, Type: null, IsInvalid) (Syntax: '')
              Children(0)
            IInvalidOperation (OperationKind.Invalid, Type: ?, IsInvalid) (Syntax: 'args[y][][]')
              Children(2):
                  IInvalidOperation (OperationKind.Invalid, Type: null, IsInvalid) (Syntax: '')
                    Children(0)
                  IInvalidOperation (OperationKind.Invalid, Type: System.Char, IsInvalid) (Syntax: 'args[y][]')
                    Children(2):
                        IArrayElementReferenceOperation (OperationKind.ArrayElementReference, Type: System.String) (Syntax: 'args[y]')
                          Array reference: 
                            IParameterReferenceOperation: args (OperationKind.ParameterReference, Type: System.String[]) (Syntax: 'args')
                          Indices(1):
                              IParameterReferenceOperation: y (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'y')
                        IInvalidOperation (OperationKind.Invalid, Type: null, IsInvalid) (Syntax: '')
                          Children(0)
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS0443: Syntax error; value expected
                //         var a = /*<bind>*/args[y][][][][x]/*</bind>*/;
                Diagnostic(ErrorCode.ERR_ValueExpected, "]").WithLocation(6, 35),
                // CS0443: Syntax error; value expected
                //         var a = /*<bind>*/args[y][][][][x]/*</bind>*/;
                Diagnostic(ErrorCode.ERR_ValueExpected, "]").WithLocation(6, 37),
                // CS0443: Syntax error; value expected
                //         var a = /*<bind>*/args[y][][][][x]/*</bind>*/;
                Diagnostic(ErrorCode.ERR_ValueExpected, "]").WithLocation(6, 39)
            };

            VerifyOperationTreeAndDiagnosticsForTest<ElementAccessExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact, WorkItem(22006, "https://github.com/dotnet/roslyn/issues/22006")]
        public void ArrayElementReferenceError_NamedArgumentForArray()
        {
            string source = @"
class C
{
    public void F(string[] args)
    {
        var a = /*<bind>*/args[name: 0]/*</bind>*/;
    }
}
";
            string expectedOperationTree = @"
IArrayElementReferenceOperation (OperationKind.ArrayElementReference, Type: System.String, IsInvalid) (Syntax: 'args[name: 0]')
  Array reference: 
    IParameterReferenceOperation: args (OperationKind.ParameterReference, Type: System.String[], IsInvalid) (Syntax: 'args')
  Indices(1):
      ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 0, IsInvalid) (Syntax: '0')
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS1742: An array access may not have a named argument specifier
                //         var a = /*<bind>*/args[name: 0]/*</bind>*/;
                Diagnostic(ErrorCode.ERR_NamedArgumentForArray, "args[name: 0]").WithLocation(6, 27)
            };

            VerifyOperationTreeAndDiagnosticsForTest<ElementAccessExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact, WorkItem(22006, "https://github.com/dotnet/roslyn/issues/22006")]
        public void ArrayElementReferenceError_RefAndOutArguments()
        {
            string source = @"
class C
{
    public void F(string[,] args, ref int x, out int y)
    {
        var a = /*<bind>*/args[ref x, out y]/*</bind>*/;
    }
}
";
            string expectedOperationTree = @"
IArrayElementReferenceOperation (OperationKind.ArrayElementReference, Type: System.String, IsInvalid) (Syntax: 'args[ref x, out y]')
  Array reference: 
    IParameterReferenceOperation: args (OperationKind.ParameterReference, Type: System.String[,]) (Syntax: 'args')
  Indices(2):
      IParameterReferenceOperation: x (OperationKind.ParameterReference, Type: System.Int32, IsInvalid) (Syntax: 'x')
      IParameterReferenceOperation: y (OperationKind.ParameterReference, Type: System.Int32, IsInvalid) (Syntax: 'y')
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS1615: Argument 1 may not be passed with the 'ref' keyword
                //         var a = /*<bind>*/args[ref x, out y]/*</bind>*/;
                Diagnostic(ErrorCode.ERR_BadArgExtraRef, "x").WithArguments("1", "ref").WithLocation(6, 36),
                // CS0269: Use of unassigned out parameter 'y'
                //         var a = /*<bind>*/args[ref x, out y]/*</bind>*/;
                Diagnostic(ErrorCode.ERR_UseDefViolationOut, "y").WithArguments("y").WithLocation(6, 43),
                // CS0177: The out parameter 'y' must be assigned to before control leaves the current method
                //     public void F(string[,] args, ref int x, out int y)
                Diagnostic(ErrorCode.ERR_ParamUnassigned, "F").WithArguments("y").WithLocation(4, 17)
            };

            VerifyOperationTreeAndDiagnosticsForTest<ElementAccessExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact, WorkItem(22006, "https://github.com/dotnet/roslyn/issues/22006")]
        public void ArrayElementReferenceWarning_NegativeIndexExpression()
        {
            string source = @"
class C
{
    public void F(string[] args)
    {
        var a = /*<bind>*/args[-1]/*</bind>*/;
    }
}
";
            string expectedOperationTree = @"
IArrayElementReferenceOperation (OperationKind.ArrayElementReference, Type: System.String) (Syntax: 'args[-1]')
  Array reference: 
    IParameterReferenceOperation: args (OperationKind.ParameterReference, Type: System.String[]) (Syntax: 'args')
  Indices(1):
      IUnaryOperation (UnaryOperatorKind.Minus) (OperationKind.Unary, Type: System.Int32, Constant: -1) (Syntax: '-1')
        Operand: 
          ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS0251: Indexing an array with a negative index (array indices always start at zero)
                //         var a = /*<bind>*/args[-1]/*</bind>*/;
                Diagnostic(ErrorCode.WRN_NegativeArrayIndex, "-1").WithLocation(6, 32)
            };

            VerifyOperationTreeAndDiagnosticsForTest<ElementAccessExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void ArrayElementReference_NoControlFlow()
        {
            string source = @"
class C
{
    void M(int[] a1, int[,] a2, int i1, int i2, int i3, int result1, int result2)
    /*<bind>*/{
        result1 = a1[i1];
        result2 = a2[i2, i3];
    }/*</bind>*/
}
";
            string expectedFlowGraph = @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
Block[B1] - Block
    Predecessors: [B0]
    Statements (2)
        IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'result1 = a1[i1];')
          Expression: 
            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32) (Syntax: 'result1 = a1[i1]')
              Left: 
                IParameterReferenceOperation: result1 (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'result1')
              Right: 
                IArrayElementReferenceOperation (OperationKind.ArrayElementReference, Type: System.Int32) (Syntax: 'a1[i1]')
                  Array reference: 
                    IParameterReferenceOperation: a1 (OperationKind.ParameterReference, Type: System.Int32[]) (Syntax: 'a1')
                  Indices(1):
                      IParameterReferenceOperation: i1 (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'i1')

        IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'result2 = a2[i2, i3];')
          Expression: 
            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32) (Syntax: 'result2 = a2[i2, i3]')
              Left: 
                IParameterReferenceOperation: result2 (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'result2')
              Right: 
                IArrayElementReferenceOperation (OperationKind.ArrayElementReference, Type: System.Int32) (Syntax: 'a2[i2, i3]')
                  Array reference: 
                    IParameterReferenceOperation: a2 (OperationKind.ParameterReference, Type: System.Int32[,]) (Syntax: 'a2')
                  Indices(2):
                      IParameterReferenceOperation: i2 (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'i2')
                      IParameterReferenceOperation: i3 (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'i3')

    Next (Regular) Block[B2]
Block[B2] - Exit
    Predecessors: [B1]
    Statements (0)
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(source, expectedFlowGraph, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void ArrayElementReference_ControlFlowInArrayReference()
        {
            string source = @"
class C
{
    void M(int[] a1, int[] a2, int i, int result)
    /*<bind>*/{
        result = (a1 ?? a2)[i];
    }/*</bind>*/
}
";
            string expectedFlowGraph = @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
        Entering: {R1}

.locals {R1}
{
    CaptureIds: [0] [2]
    Block[B1] - Block
        Predecessors: [B0]
        Statements (1)
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'result')
              Value: 
                IParameterReferenceOperation: result (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'result')

        Next (Regular) Block[B2]
            Entering: {R2}

    .locals {R2}
    {
        CaptureIds: [1]
        Block[B2] - Block
            Predecessors: [B1]
            Statements (1)
                IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'a1')
                  Value: 
                    IParameterReferenceOperation: a1 (OperationKind.ParameterReference, Type: System.Int32[]) (Syntax: 'a1')

            Jump if True (Regular) to Block[B4]
                IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsImplicit) (Syntax: 'a1')
                  Operand: 
                    IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.Int32[], IsImplicit) (Syntax: 'a1')
                Leaving: {R2}

            Next (Regular) Block[B3]
        Block[B3] - Block
            Predecessors: [B2]
            Statements (1)
                IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'a1')
                  Value: 
                    IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.Int32[], IsImplicit) (Syntax: 'a1')

            Next (Regular) Block[B5]
                Leaving: {R2}
    }

    Block[B4] - Block
        Predecessors: [B2]
        Statements (1)
            IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'a2')
              Value: 
                IParameterReferenceOperation: a2 (OperationKind.ParameterReference, Type: System.Int32[]) (Syntax: 'a2')

        Next (Regular) Block[B5]
    Block[B5] - Block
        Predecessors: [B3] [B4]
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'result = (a1 ?? a2)[i];')
              Expression: 
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32) (Syntax: 'result = (a1 ?? a2)[i]')
                  Left: 
                    IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'result')
                  Right: 
                    IArrayElementReferenceOperation (OperationKind.ArrayElementReference, Type: System.Int32) (Syntax: '(a1 ?? a2)[i]')
                      Array reference: 
                        IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: System.Int32[], IsImplicit) (Syntax: 'a1 ?? a2')
                      Indices(1):
                          IParameterReferenceOperation: i (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'i')

        Next (Regular) Block[B6]
            Leaving: {R1}
}

Block[B6] - Exit
    Predecessors: [B5]
    Statements (0)
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(source, expectedFlowGraph, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void ArrayElementReference_ControlFlowInFirstIndex()
        {
            string source = @"
class C
{
    void M(int[,] a, int? i1, int i2, byte j, int result)
    /*<bind>*/{
        result = a[i1 ?? i2, j];
    }/*</bind>*/
}
";
            string expectedFlowGraph = @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
        Entering: {R1}

.locals {R1}
{
    CaptureIds: [0] [1] [3]
    Block[B1] - Block
        Predecessors: [B0]
        Statements (2)
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'result')
              Value: 
                IParameterReferenceOperation: result (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'result')

            IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'a')
              Value: 
                IParameterReferenceOperation: a (OperationKind.ParameterReference, Type: System.Int32[,]) (Syntax: 'a')

        Next (Regular) Block[B2]
            Entering: {R2}

    .locals {R2}
    {
        CaptureIds: [2]
        Block[B2] - Block
            Predecessors: [B1]
            Statements (1)
                IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'i1')
                  Value: 
                    IParameterReferenceOperation: i1 (OperationKind.ParameterReference, Type: System.Int32?) (Syntax: 'i1')

            Jump if True (Regular) to Block[B4]
                IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsImplicit) (Syntax: 'i1')
                  Operand: 
                    IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: System.Int32?, IsImplicit) (Syntax: 'i1')
                Leaving: {R2}

            Next (Regular) Block[B3]
        Block[B3] - Block
            Predecessors: [B2]
            Statements (1)
                IFlowCaptureOperation: 3 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'i1')
                  Value: 
                    IInvocationOperation ( System.Int32 System.Int32?.GetValueOrDefault()) (OperationKind.Invocation, Type: System.Int32, IsImplicit) (Syntax: 'i1')
                      Instance Receiver: 
                        IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: System.Int32?, IsImplicit) (Syntax: 'i1')
                      Arguments(0)

            Next (Regular) Block[B5]
                Leaving: {R2}
    }

    Block[B4] - Block
        Predecessors: [B2]
        Statements (1)
            IFlowCaptureOperation: 3 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'i2')
              Value: 
                IParameterReferenceOperation: i2 (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'i2')

        Next (Regular) Block[B5]
    Block[B5] - Block
        Predecessors: [B3] [B4]
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'result = a[i1 ?? i2, j];')
              Expression: 
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32) (Syntax: 'result = a[i1 ?? i2, j]')
                  Left: 
                    IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'result')
                  Right: 
                    IArrayElementReferenceOperation (OperationKind.ArrayElementReference, Type: System.Int32) (Syntax: 'a[i1 ?? i2, j]')
                      Array reference: 
                        IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.Int32[,], IsImplicit) (Syntax: 'a')
                      Indices(2):
                          IFlowCaptureReferenceOperation: 3 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'i1 ?? i2')
                          IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Int32, IsImplicit) (Syntax: 'j')
                            Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: True, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                              (ImplicitNumeric)
                            Operand: 
                              IParameterReferenceOperation: j (OperationKind.ParameterReference, Type: System.Byte) (Syntax: 'j')

        Next (Regular) Block[B6]
            Leaving: {R1}
}

Block[B6] - Exit
    Predecessors: [B5]
    Statements (0)
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(source, expectedFlowGraph, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void ArrayElementReference_ControlFlowInSecondIndex()
        {
            string source = @"
class C
{
    void M(int[,] a, int? i1, int i2, int j, int result)
    /*<bind>*/{
        result = a[j, i1 ?? i2];
    }/*</bind>*/
}
";
            string expectedFlowGraph = @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
        Entering: {R1}

.locals {R1}
{
    CaptureIds: [0] [1] [2] [4]
    Block[B1] - Block
        Predecessors: [B0]
        Statements (3)
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'result')
              Value: 
                IParameterReferenceOperation: result (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'result')

            IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'a')
              Value: 
                IParameterReferenceOperation: a (OperationKind.ParameterReference, Type: System.Int32[,]) (Syntax: 'a')

            IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'j')
              Value: 
                IParameterReferenceOperation: j (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'j')

        Next (Regular) Block[B2]
            Entering: {R2}

    .locals {R2}
    {
        CaptureIds: [3]
        Block[B2] - Block
            Predecessors: [B1]
            Statements (1)
                IFlowCaptureOperation: 3 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'i1')
                  Value: 
                    IParameterReferenceOperation: i1 (OperationKind.ParameterReference, Type: System.Int32?) (Syntax: 'i1')

            Jump if True (Regular) to Block[B4]
                IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsImplicit) (Syntax: 'i1')
                  Operand: 
                    IFlowCaptureReferenceOperation: 3 (OperationKind.FlowCaptureReference, Type: System.Int32?, IsImplicit) (Syntax: 'i1')
                Leaving: {R2}

            Next (Regular) Block[B3]
        Block[B3] - Block
            Predecessors: [B2]
            Statements (1)
                IFlowCaptureOperation: 4 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'i1')
                  Value: 
                    IInvocationOperation ( System.Int32 System.Int32?.GetValueOrDefault()) (OperationKind.Invocation, Type: System.Int32, IsImplicit) (Syntax: 'i1')
                      Instance Receiver: 
                        IFlowCaptureReferenceOperation: 3 (OperationKind.FlowCaptureReference, Type: System.Int32?, IsImplicit) (Syntax: 'i1')
                      Arguments(0)

            Next (Regular) Block[B5]
                Leaving: {R2}
    }

    Block[B4] - Block
        Predecessors: [B2]
        Statements (1)
            IFlowCaptureOperation: 4 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'i2')
              Value: 
                IParameterReferenceOperation: i2 (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'i2')

        Next (Regular) Block[B5]
    Block[B5] - Block
        Predecessors: [B3] [B4]
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'result = a[j, i1 ?? i2];')
              Expression: 
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32) (Syntax: 'result = a[j, i1 ?? i2]')
                  Left: 
                    IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'result')
                  Right: 
                    IArrayElementReferenceOperation (OperationKind.ArrayElementReference, Type: System.Int32) (Syntax: 'a[j, i1 ?? i2]')
                      Array reference: 
                        IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.Int32[,], IsImplicit) (Syntax: 'a')
                      Indices(2):
                          IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'j')
                          IFlowCaptureReferenceOperation: 4 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'i1 ?? i2')

        Next (Regular) Block[B6]
            Leaving: {R1}
}

Block[B6] - Exit
    Predecessors: [B5]
    Statements (0)
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(source, expectedFlowGraph, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void ArrayElementReference_ControlFlowInMultipleIndices()
        {
            string source = @"
class C
{
    void M(int[,] a, int? i1, int i2, int? j1, int j2, int result)
    /*<bind>*/{
        result = a[i1 ?? i2, j1 ?? j2];
    }/*</bind>*/
}
";
            string expectedFlowGraph = @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
        Entering: {R1}

.locals {R1}
{
    CaptureIds: [0] [1] [3] [5]
    Block[B1] - Block
        Predecessors: [B0]
        Statements (2)
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'result')
              Value: 
                IParameterReferenceOperation: result (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'result')

            IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'a')
              Value: 
                IParameterReferenceOperation: a (OperationKind.ParameterReference, Type: System.Int32[,]) (Syntax: 'a')

        Next (Regular) Block[B2]
            Entering: {R2}

    .locals {R2}
    {
        CaptureIds: [2]
        Block[B2] - Block
            Predecessors: [B1]
            Statements (1)
                IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'i1')
                  Value: 
                    IParameterReferenceOperation: i1 (OperationKind.ParameterReference, Type: System.Int32?) (Syntax: 'i1')

            Jump if True (Regular) to Block[B4]
                IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsImplicit) (Syntax: 'i1')
                  Operand: 
                    IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: System.Int32?, IsImplicit) (Syntax: 'i1')
                Leaving: {R2}

            Next (Regular) Block[B3]
        Block[B3] - Block
            Predecessors: [B2]
            Statements (1)
                IFlowCaptureOperation: 3 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'i1')
                  Value: 
                    IInvocationOperation ( System.Int32 System.Int32?.GetValueOrDefault()) (OperationKind.Invocation, Type: System.Int32, IsImplicit) (Syntax: 'i1')
                      Instance Receiver: 
                        IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: System.Int32?, IsImplicit) (Syntax: 'i1')
                      Arguments(0)

            Next (Regular) Block[B5]
                Leaving: {R2}
                Entering: {R3}
    }

    Block[B4] - Block
        Predecessors: [B2]
        Statements (1)
            IFlowCaptureOperation: 3 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'i2')
              Value: 
                IParameterReferenceOperation: i2 (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'i2')

        Next (Regular) Block[B5]
            Entering: {R3}

    .locals {R3}
    {
        CaptureIds: [4]
        Block[B5] - Block
            Predecessors: [B3] [B4]
            Statements (1)
                IFlowCaptureOperation: 4 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'j1')
                  Value: 
                    IParameterReferenceOperation: j1 (OperationKind.ParameterReference, Type: System.Int32?) (Syntax: 'j1')

            Jump if True (Regular) to Block[B7]
                IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsImplicit) (Syntax: 'j1')
                  Operand: 
                    IFlowCaptureReferenceOperation: 4 (OperationKind.FlowCaptureReference, Type: System.Int32?, IsImplicit) (Syntax: 'j1')
                Leaving: {R3}

            Next (Regular) Block[B6]
        Block[B6] - Block
            Predecessors: [B5]
            Statements (1)
                IFlowCaptureOperation: 5 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'j1')
                  Value: 
                    IInvocationOperation ( System.Int32 System.Int32?.GetValueOrDefault()) (OperationKind.Invocation, Type: System.Int32, IsImplicit) (Syntax: 'j1')
                      Instance Receiver: 
                        IFlowCaptureReferenceOperation: 4 (OperationKind.FlowCaptureReference, Type: System.Int32?, IsImplicit) (Syntax: 'j1')
                      Arguments(0)

            Next (Regular) Block[B8]
                Leaving: {R3}
    }

    Block[B7] - Block
        Predecessors: [B5]
        Statements (1)
            IFlowCaptureOperation: 5 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'j2')
              Value: 
                IParameterReferenceOperation: j2 (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'j2')

        Next (Regular) Block[B8]
    Block[B8] - Block
        Predecessors: [B6] [B7]
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'result = a[ ...  j1 ?? j2];')
              Expression: 
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32) (Syntax: 'result = a[ ... , j1 ?? j2]')
                  Left: 
                    IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'result')
                  Right: 
                    IArrayElementReferenceOperation (OperationKind.ArrayElementReference, Type: System.Int32) (Syntax: 'a[i1 ?? i2, j1 ?? j2]')
                      Array reference: 
                        IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.Int32[,], IsImplicit) (Syntax: 'a')
                      Indices(2):
                          IFlowCaptureReferenceOperation: 3 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'i1 ?? i2')
                          IFlowCaptureReferenceOperation: 5 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'j1 ?? j2')

        Next (Regular) Block[B9]
            Leaving: {R1}
}

Block[B9] - Exit
    Predecessors: [B8]
    Statements (0)
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(source, expectedFlowGraph, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void ArrayElementReference_ControlFlowInArrayReferenceAndIndices()
        {
            string source = @"
class C
{
    void M(int[,] a1, int[,] a2, int? i1, int i2, int? j1, int j2, int result)
    /*<bind>*/{
        result = (a1 ?? a2)[i1 ?? i2, j1 ?? j2];
    }/*</bind>*/
}
";
            string expectedFlowGraph = @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
        Entering: {R1}

.locals {R1}
{
    CaptureIds: [0] [2] [4] [6]
    Block[B1] - Block
        Predecessors: [B0]
        Statements (1)
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'result')
              Value: 
                IParameterReferenceOperation: result (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'result')

        Next (Regular) Block[B2]
            Entering: {R2}

    .locals {R2}
    {
        CaptureIds: [1]
        Block[B2] - Block
            Predecessors: [B1]
            Statements (1)
                IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'a1')
                  Value: 
                    IParameterReferenceOperation: a1 (OperationKind.ParameterReference, Type: System.Int32[,]) (Syntax: 'a1')

            Jump if True (Regular) to Block[B4]
                IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsImplicit) (Syntax: 'a1')
                  Operand: 
                    IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.Int32[,], IsImplicit) (Syntax: 'a1')
                Leaving: {R2}

            Next (Regular) Block[B3]
        Block[B3] - Block
            Predecessors: [B2]
            Statements (1)
                IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'a1')
                  Value: 
                    IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.Int32[,], IsImplicit) (Syntax: 'a1')

            Next (Regular) Block[B5]
                Leaving: {R2}
                Entering: {R3}
    }

    Block[B4] - Block
        Predecessors: [B2]
        Statements (1)
            IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'a2')
              Value: 
                IParameterReferenceOperation: a2 (OperationKind.ParameterReference, Type: System.Int32[,]) (Syntax: 'a2')

        Next (Regular) Block[B5]
            Entering: {R3}

    .locals {R3}
    {
        CaptureIds: [3]
        Block[B5] - Block
            Predecessors: [B3] [B4]
            Statements (1)
                IFlowCaptureOperation: 3 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'i1')
                  Value: 
                    IParameterReferenceOperation: i1 (OperationKind.ParameterReference, Type: System.Int32?) (Syntax: 'i1')

            Jump if True (Regular) to Block[B7]
                IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsImplicit) (Syntax: 'i1')
                  Operand: 
                    IFlowCaptureReferenceOperation: 3 (OperationKind.FlowCaptureReference, Type: System.Int32?, IsImplicit) (Syntax: 'i1')
                Leaving: {R3}

            Next (Regular) Block[B6]
        Block[B6] - Block
            Predecessors: [B5]
            Statements (1)
                IFlowCaptureOperation: 4 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'i1')
                  Value: 
                    IInvocationOperation ( System.Int32 System.Int32?.GetValueOrDefault()) (OperationKind.Invocation, Type: System.Int32, IsImplicit) (Syntax: 'i1')
                      Instance Receiver: 
                        IFlowCaptureReferenceOperation: 3 (OperationKind.FlowCaptureReference, Type: System.Int32?, IsImplicit) (Syntax: 'i1')
                      Arguments(0)

            Next (Regular) Block[B8]
                Leaving: {R3}
                Entering: {R4}
    }

    Block[B7] - Block
        Predecessors: [B5]
        Statements (1)
            IFlowCaptureOperation: 4 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'i2')
              Value: 
                IParameterReferenceOperation: i2 (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'i2')

        Next (Regular) Block[B8]
            Entering: {R4}

    .locals {R4}
    {
        CaptureIds: [5]
        Block[B8] - Block
            Predecessors: [B6] [B7]
            Statements (1)
                IFlowCaptureOperation: 5 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'j1')
                  Value: 
                    IParameterReferenceOperation: j1 (OperationKind.ParameterReference, Type: System.Int32?) (Syntax: 'j1')

            Jump if True (Regular) to Block[B10]
                IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsImplicit) (Syntax: 'j1')
                  Operand: 
                    IFlowCaptureReferenceOperation: 5 (OperationKind.FlowCaptureReference, Type: System.Int32?, IsImplicit) (Syntax: 'j1')
                Leaving: {R4}

            Next (Regular) Block[B9]
        Block[B9] - Block
            Predecessors: [B8]
            Statements (1)
                IFlowCaptureOperation: 6 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'j1')
                  Value: 
                    IInvocationOperation ( System.Int32 System.Int32?.GetValueOrDefault()) (OperationKind.Invocation, Type: System.Int32, IsImplicit) (Syntax: 'j1')
                      Instance Receiver: 
                        IFlowCaptureReferenceOperation: 5 (OperationKind.FlowCaptureReference, Type: System.Int32?, IsImplicit) (Syntax: 'j1')
                      Arguments(0)

            Next (Regular) Block[B11]
                Leaving: {R4}
    }

    Block[B10] - Block
        Predecessors: [B8]
        Statements (1)
            IFlowCaptureOperation: 6 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'j2')
              Value: 
                IParameterReferenceOperation: j2 (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'j2')

        Next (Regular) Block[B11]
    Block[B11] - Block
        Predecessors: [B9] [B10]
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'result = (a ...  j1 ?? j2];')
              Expression: 
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32) (Syntax: 'result = (a ... , j1 ?? j2]')
                  Left: 
                    IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'result')
                  Right: 
                    IArrayElementReferenceOperation (OperationKind.ArrayElementReference, Type: System.Int32) (Syntax: '(a1 ?? a2)[ ... , j1 ?? j2]')
                      Array reference: 
                        IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: System.Int32[,], IsImplicit) (Syntax: 'a1 ?? a2')
                      Indices(2):
                          IFlowCaptureReferenceOperation: 4 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'i1 ?? i2')
                          IFlowCaptureReferenceOperation: 6 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'j1 ?? j2')

        Next (Regular) Block[B12]
            Leaving: {R1}
}

Block[B12] - Exit
    Predecessors: [B11]
    Statements (0)
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(source, expectedFlowGraph, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void ArrayElementReference_ImplicitIndexIndexer()
        {
            string source = @"
class C
{
    public void F(string[] args, System.Index x)
    {
        var a = /*<bind>*/args[x]/*</bind>*/;
    }
}
";

            string expectedOperationTree = @"
IArrayElementReferenceOperation (OperationKind.ArrayElementReference, Type: System.String) (Syntax: 'args[x]')
  Array reference:
    IParameterReferenceOperation: args (OperationKind.ParameterReference, Type: System.String[]) (Syntax: 'args')
  Indices(1):
      IParameterReferenceOperation: x (OperationKind.ParameterReference, Type: System.Index) (Syntax: 'x')
";
            var expectedDiagnostics = DiagnosticDescription.None;

            var comp = CreateCompilationWithIndexAndRange(source);
            VerifyOperationTreeAndDiagnosticsForTest<ElementAccessExpressionSyntax>(comp, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void ArrayElementReference_ImplicitIndexIndexer_NoControlFlow()
        {
            string source = @"
class C
{
    void M(int[] a1, System.Index i1, int result1)
    /*<bind>*/{
        result1 = a1[i1];
    }/*</bind>*/
}
";
            string expectedFlowGraph = @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
Block[B1] - Block
    Predecessors: [B0]
    Statements (1)
        IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'result1 = a1[i1];')
          Expression:
            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32) (Syntax: 'result1 = a1[i1]')
              Left:
                IParameterReferenceOperation: result1 (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'result1')
              Right:
                IArrayElementReferenceOperation (OperationKind.ArrayElementReference, Type: System.Int32) (Syntax: 'a1[i1]')
                  Array reference:
                    IParameterReferenceOperation: a1 (OperationKind.ParameterReference, Type: System.Int32[]) (Syntax: 'a1')
                  Indices(1):
                      IParameterReferenceOperation: i1 (OperationKind.ParameterReference, Type: System.Index) (Syntax: 'i1')
    Next (Regular) Block[B2]
Block[B2] - Exit
    Predecessors: [B1]
    Statements (0)
";
            var expectedDiagnostics = DiagnosticDescription.None;

            var comp = CreateCompilationWithIndexAndRange(source);
            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(comp, expectedFlowGraph, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void ArrayElementReference_ImplicitIndexIndexer_ControlFlowInInstance()
        {
            string source = @"
class C
{
    void M(int[] a1, int[] a2, System.Index i1, int result)
    /*<bind>*/{
        result = (a1 ?? a2)[i1];
    }/*</bind>*/
}
";
            string expectedFlowGraph = @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
        Entering: {R1}
.locals {R1}
{
    CaptureIds: [0] [2]
    Block[B1] - Block
        Predecessors: [B0]
        Statements (1)
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'result')
              Value:
                IParameterReferenceOperation: result (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'result')
        Next (Regular) Block[B2]
            Entering: {R2}
    .locals {R2}
    {
        CaptureIds: [1]
        Block[B2] - Block
            Predecessors: [B1]
            Statements (1)
                IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'a1')
                  Value:
                    IParameterReferenceOperation: a1 (OperationKind.ParameterReference, Type: System.Int32[]) (Syntax: 'a1')
            Jump if True (Regular) to Block[B4]
                IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsImplicit) (Syntax: 'a1')
                  Operand:
                    IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.Int32[], IsImplicit) (Syntax: 'a1')
                Leaving: {R2}
            Next (Regular) Block[B3]
        Block[B3] - Block
            Predecessors: [B2]
            Statements (1)
                IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'a1')
                  Value:
                    IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.Int32[], IsImplicit) (Syntax: 'a1')
            Next (Regular) Block[B5]
                Leaving: {R2}
    }
    Block[B4] - Block
        Predecessors: [B2]
        Statements (1)
            IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'a2')
              Value:
                IParameterReferenceOperation: a2 (OperationKind.ParameterReference, Type: System.Int32[]) (Syntax: 'a2')
        Next (Regular) Block[B5]
    Block[B5] - Block
        Predecessors: [B3] [B4]
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'result = (a1 ?? a2)[i1];')
              Expression:
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32) (Syntax: 'result = (a1 ?? a2)[i1]')
                  Left:
                    IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'result')
                  Right:
                    IArrayElementReferenceOperation (OperationKind.ArrayElementReference, Type: System.Int32) (Syntax: '(a1 ?? a2)[i1]')
                      Array reference:
                        IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: System.Int32[], IsImplicit) (Syntax: 'a1 ?? a2')
                      Indices(1):
                          IParameterReferenceOperation: i1 (OperationKind.ParameterReference, Type: System.Index) (Syntax: 'i1')
        Next (Regular) Block[B6]
            Leaving: {R1}
}
Block[B6] - Exit
    Predecessors: [B5]
    Statements (0)
";
            var expectedDiagnostics = DiagnosticDescription.None;

            var comp = CreateCompilationWithIndexAndRange(source);
            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(comp, expectedFlowGraph, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void ArrayElementReference_ImplicitIndexIndexer_ControlFlowInArgument()
        {
            string source = @"
class C
{
    void M(int[] a, System.Index? i1, System.Index i2, int result)
    /*<bind>*/{
        result = a[i1 ?? i2];
    }/*</bind>*/
}
";
            string expectedFlowGraph = @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
        Entering: {R1}
.locals {R1}
{
    CaptureIds: [0] [1] [3]
    Block[B1] - Block
        Predecessors: [B0]
        Statements (2)
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'result')
              Value:
                IParameterReferenceOperation: result (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'result')
            IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'a')
              Value:
                IParameterReferenceOperation: a (OperationKind.ParameterReference, Type: System.Int32[]) (Syntax: 'a')
        Next (Regular) Block[B2]
            Entering: {R2}
    .locals {R2}
    {
        CaptureIds: [2]
        Block[B2] - Block
            Predecessors: [B1]
            Statements (1)
                IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'i1')
                  Value:
                    IParameterReferenceOperation: i1 (OperationKind.ParameterReference, Type: System.Index?) (Syntax: 'i1')
            Jump if True (Regular) to Block[B4]
                IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsImplicit) (Syntax: 'i1')
                  Operand:
                    IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: System.Index?, IsImplicit) (Syntax: 'i1')
                Leaving: {R2}
            Next (Regular) Block[B3]
        Block[B3] - Block
            Predecessors: [B2]
            Statements (1)
                IFlowCaptureOperation: 3 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'i1')
                  Value:
                    IInvocationOperation ( System.Index System.Index?.GetValueOrDefault()) (OperationKind.Invocation, Type: System.Index, IsImplicit) (Syntax: 'i1')
                      Instance Receiver:
                        IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: System.Index?, IsImplicit) (Syntax: 'i1')
                      Arguments(0)
            Next (Regular) Block[B5]
                Leaving: {R2}
    }
    Block[B4] - Block
        Predecessors: [B2]
        Statements (1)
            IFlowCaptureOperation: 3 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'i2')
              Value:
                IParameterReferenceOperation: i2 (OperationKind.ParameterReference, Type: System.Index) (Syntax: 'i2')
        Next (Regular) Block[B5]
    Block[B5] - Block
        Predecessors: [B3] [B4]
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'result = a[i1 ?? i2];')
              Expression:
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32) (Syntax: 'result = a[i1 ?? i2]')
                  Left:
                    IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'result')
                  Right:
                    IArrayElementReferenceOperation (OperationKind.ArrayElementReference, Type: System.Int32) (Syntax: 'a[i1 ?? i2]')
                      Array reference:
                        IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.Int32[], IsImplicit) (Syntax: 'a')
                      Indices(1):
                          IFlowCaptureReferenceOperation: 3 (OperationKind.FlowCaptureReference, Type: System.Index, IsImplicit) (Syntax: 'i1 ?? i2')
        Next (Regular) Block[B6]
            Leaving: {R1}
}
Block[B6] - Exit
    Predecessors: [B5]
    Statements (0)
";
            var expectedDiagnostics = DiagnosticDescription.None;

            var comp = CreateCompilationWithIndexAndRange(source);
            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(comp, expectedFlowGraph, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void ArrayElementReference_ImplicitIndexIndexer_ControlFlowInInstanceAndArgument()
        {
            string source = @"
class C
{
    void M(int[] a1, int[] a2, System.Index? i1, System.Index i2, int result)
    /*<bind>*/{
        result = (a1 ?? a2)[i1 ?? i2];
    }/*</bind>*/
}
";
            string expectedFlowGraph = @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
        Entering: {R1}
.locals {R1}
{
    CaptureIds: [0] [2] [4]
    Block[B1] - Block
        Predecessors: [B0]
        Statements (1)
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'result')
              Value:
                IParameterReferenceOperation: result (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'result')
        Next (Regular) Block[B2]
            Entering: {R2}
    .locals {R2}
    {
        CaptureIds: [1]
        Block[B2] - Block
            Predecessors: [B1]
            Statements (1)
                IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'a1')
                  Value:
                    IParameterReferenceOperation: a1 (OperationKind.ParameterReference, Type: System.Int32[]) (Syntax: 'a1')
            Jump if True (Regular) to Block[B4]
                IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsImplicit) (Syntax: 'a1')
                  Operand:
                    IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.Int32[], IsImplicit) (Syntax: 'a1')
                Leaving: {R2}
            Next (Regular) Block[B3]
        Block[B3] - Block
            Predecessors: [B2]
            Statements (1)
                IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'a1')
                  Value:
                    IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.Int32[], IsImplicit) (Syntax: 'a1')
            Next (Regular) Block[B5]
                Leaving: {R2}
                Entering: {R3}
    }
    Block[B4] - Block
        Predecessors: [B2]
        Statements (1)
            IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'a2')
              Value:
                IParameterReferenceOperation: a2 (OperationKind.ParameterReference, Type: System.Int32[]) (Syntax: 'a2')
        Next (Regular) Block[B5]
            Entering: {R3}
    .locals {R3}
    {
        CaptureIds: [3]
        Block[B5] - Block
            Predecessors: [B3] [B4]
            Statements (1)
                IFlowCaptureOperation: 3 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'i1')
                  Value:
                    IParameterReferenceOperation: i1 (OperationKind.ParameterReference, Type: System.Index?) (Syntax: 'i1')
            Jump if True (Regular) to Block[B7]
                IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsImplicit) (Syntax: 'i1')
                  Operand:
                    IFlowCaptureReferenceOperation: 3 (OperationKind.FlowCaptureReference, Type: System.Index?, IsImplicit) (Syntax: 'i1')
                Leaving: {R3}
            Next (Regular) Block[B6]
        Block[B6] - Block
            Predecessors: [B5]
            Statements (1)
                IFlowCaptureOperation: 4 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'i1')
                  Value:
                    IInvocationOperation ( System.Index System.Index?.GetValueOrDefault()) (OperationKind.Invocation, Type: System.Index, IsImplicit) (Syntax: 'i1')
                      Instance Receiver:
                        IFlowCaptureReferenceOperation: 3 (OperationKind.FlowCaptureReference, Type: System.Index?, IsImplicit) (Syntax: 'i1')
                      Arguments(0)
            Next (Regular) Block[B8]
                Leaving: {R3}
    }
    Block[B7] - Block
        Predecessors: [B5]
        Statements (1)
            IFlowCaptureOperation: 4 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'i2')
              Value:
                IParameterReferenceOperation: i2 (OperationKind.ParameterReference, Type: System.Index) (Syntax: 'i2')
        Next (Regular) Block[B8]
    Block[B8] - Block
        Predecessors: [B6] [B7]
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'result = (a ... [i1 ?? i2];')
              Expression:
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32) (Syntax: 'result = (a ... )[i1 ?? i2]')
                  Left:
                    IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'result')
                  Right:
                    IArrayElementReferenceOperation (OperationKind.ArrayElementReference, Type: System.Int32) (Syntax: '(a1 ?? a2)[i1 ?? i2]')
                      Array reference:
                        IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: System.Int32[], IsImplicit) (Syntax: 'a1 ?? a2')
                      Indices(1):
                          IFlowCaptureReferenceOperation: 4 (OperationKind.FlowCaptureReference, Type: System.Index, IsImplicit) (Syntax: 'i1 ?? i2')
        Next (Regular) Block[B9]
            Leaving: {R1}
}
Block[B9] - Exit
    Predecessors: [B8]
    Statements (0)
";
            var expectedDiagnostics = DiagnosticDescription.None;

            var comp = CreateCompilationWithIndexAndRange(source);
            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(comp, expectedFlowGraph, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void ArrayElementReference_ImplicitRangeIndexer()
        {
            string source = @"
class C
{
    public void F(string[] args, System.Range x)
    {
        var a = /*<bind>*/args[x]/*</bind>*/;
    }
}
";

            string expectedOperationTree = @"
IArrayElementReferenceOperation (OperationKind.ArrayElementReference, Type: System.String[]) (Syntax: 'args[x]')
  Array reference:
    IParameterReferenceOperation: args (OperationKind.ParameterReference, Type: System.String[]) (Syntax: 'args')
  Indices(1):
      IParameterReferenceOperation: x (OperationKind.ParameterReference, Type: System.Range) (Syntax: 'x')
";
            var expectedDiagnostics = DiagnosticDescription.None;

            var comp = CreateCompilationWithIndexAndRange(new[] { source, TestSources.GetSubArray });
            VerifyOperationTreeAndDiagnosticsForTest<ElementAccessExpressionSyntax>(comp, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void ArrayElementReference_ImplicitRangeIndexer_NoControlFlow()
        {
            string source = @"
class C
{
    void M(int[] a1, System.Range i1, int[] result1)
    /*<bind>*/{
        result1 = a1[i1];
    }/*</bind>*/
}
";
            string expectedFlowGraph = @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
Block[B1] - Block
    Predecessors: [B0]
    Statements (1)
        IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'result1 = a1[i1];')
          Expression:
            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32[]) (Syntax: 'result1 = a1[i1]')
              Left:
                IParameterReferenceOperation: result1 (OperationKind.ParameterReference, Type: System.Int32[]) (Syntax: 'result1')
              Right:
                IArrayElementReferenceOperation (OperationKind.ArrayElementReference, Type: System.Int32[]) (Syntax: 'a1[i1]')
                  Array reference:
                    IParameterReferenceOperation: a1 (OperationKind.ParameterReference, Type: System.Int32[]) (Syntax: 'a1')
                  Indices(1):
                      IParameterReferenceOperation: i1 (OperationKind.ParameterReference, Type: System.Range) (Syntax: 'i1')
    Next (Regular) Block[B2]
Block[B2] - Exit
    Predecessors: [B1]
    Statements (0)
";
            var expectedDiagnostics = DiagnosticDescription.None;

            var comp = CreateCompilationWithIndexAndRange(new[] { source, TestSources.GetSubArray });
            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(comp, expectedFlowGraph, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void ArrayElementReference_ImplicitRangeIndexer_ControlFlowInInstance()
        {
            string source = @"
class C
{
    void M(int[] a1, int[] a2, System.Range i1, int[] result)
    /*<bind>*/{
        result = (a1 ?? a2)[i1];
    }/*</bind>*/
}
";
            string expectedFlowGraph = @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
        Entering: {R1}
.locals {R1}
{
    CaptureIds: [0] [2]
    Block[B1] - Block
        Predecessors: [B0]
        Statements (1)
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'result')
              Value:
                IParameterReferenceOperation: result (OperationKind.ParameterReference, Type: System.Int32[]) (Syntax: 'result')
        Next (Regular) Block[B2]
            Entering: {R2}
    .locals {R2}
    {
        CaptureIds: [1]
        Block[B2] - Block
            Predecessors: [B1]
            Statements (1)
                IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'a1')
                  Value:
                    IParameterReferenceOperation: a1 (OperationKind.ParameterReference, Type: System.Int32[]) (Syntax: 'a1')
            Jump if True (Regular) to Block[B4]
                IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsImplicit) (Syntax: 'a1')
                  Operand:
                    IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.Int32[], IsImplicit) (Syntax: 'a1')
                Leaving: {R2}
            Next (Regular) Block[B3]
        Block[B3] - Block
            Predecessors: [B2]
            Statements (1)
                IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'a1')
                  Value:
                    IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.Int32[], IsImplicit) (Syntax: 'a1')
            Next (Regular) Block[B5]
                Leaving: {R2}
    }
    Block[B4] - Block
        Predecessors: [B2]
        Statements (1)
            IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'a2')
              Value:
                IParameterReferenceOperation: a2 (OperationKind.ParameterReference, Type: System.Int32[]) (Syntax: 'a2')
        Next (Regular) Block[B5]
    Block[B5] - Block
        Predecessors: [B3] [B4]
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'result = (a1 ?? a2)[i1];')
              Expression:
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32[]) (Syntax: 'result = (a1 ?? a2)[i1]')
                  Left:
                    IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Int32[], IsImplicit) (Syntax: 'result')
                  Right:
                    IArrayElementReferenceOperation (OperationKind.ArrayElementReference, Type: System.Int32[]) (Syntax: '(a1 ?? a2)[i1]')
                      Array reference:
                        IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: System.Int32[], IsImplicit) (Syntax: 'a1 ?? a2')
                      Indices(1):
                          IParameterReferenceOperation: i1 (OperationKind.ParameterReference, Type: System.Range) (Syntax: 'i1')
        Next (Regular) Block[B6]
            Leaving: {R1}
}
Block[B6] - Exit
    Predecessors: [B5]
    Statements (0)
";
            var expectedDiagnostics = DiagnosticDescription.None;

            var comp = CreateCompilationWithIndexAndRange(new[] { source, TestSources.GetSubArray });
            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(comp, expectedFlowGraph, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void ArrayElementReference_ImplicitRangeIndexer_ControlFlowInArgument()
        {
            string source = @"
class C
{
    void M(int[] a, System.Range? i1, System.Range i2, int[] result)
    /*<bind>*/{
        result = a[i1 ?? i2];
    }/*</bind>*/
}
";
            string expectedFlowGraph = @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
        Entering: {R1}
.locals {R1}
{
    CaptureIds: [0] [1] [3]
    Block[B1] - Block
        Predecessors: [B0]
        Statements (2)
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'result')
              Value:
                IParameterReferenceOperation: result (OperationKind.ParameterReference, Type: System.Int32[]) (Syntax: 'result')
            IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'a')
              Value:
                IParameterReferenceOperation: a (OperationKind.ParameterReference, Type: System.Int32[]) (Syntax: 'a')
        Next (Regular) Block[B2]
            Entering: {R2}
    .locals {R2}
    {
        CaptureIds: [2]
        Block[B2] - Block
            Predecessors: [B1]
            Statements (1)
                IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'i1')
                  Value:
                    IParameterReferenceOperation: i1 (OperationKind.ParameterReference, Type: System.Range?) (Syntax: 'i1')
            Jump if True (Regular) to Block[B4]
                IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsImplicit) (Syntax: 'i1')
                  Operand:
                    IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: System.Range?, IsImplicit) (Syntax: 'i1')
                Leaving: {R2}
            Next (Regular) Block[B3]
        Block[B3] - Block
            Predecessors: [B2]
            Statements (1)
                IFlowCaptureOperation: 3 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'i1')
                  Value:
                    IInvocationOperation ( System.Range System.Range?.GetValueOrDefault()) (OperationKind.Invocation, Type: System.Range, IsImplicit) (Syntax: 'i1')
                      Instance Receiver:
                        IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: System.Range?, IsImplicit) (Syntax: 'i1')
                      Arguments(0)
            Next (Regular) Block[B5]
                Leaving: {R2}
    }
    Block[B4] - Block
        Predecessors: [B2]
        Statements (1)
            IFlowCaptureOperation: 3 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'i2')
              Value:
                IParameterReferenceOperation: i2 (OperationKind.ParameterReference, Type: System.Range) (Syntax: 'i2')
        Next (Regular) Block[B5]
    Block[B5] - Block
        Predecessors: [B3] [B4]
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'result = a[i1 ?? i2];')
              Expression:
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32[]) (Syntax: 'result = a[i1 ?? i2]')
                  Left:
                    IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Int32[], IsImplicit) (Syntax: 'result')
                  Right:
                    IArrayElementReferenceOperation (OperationKind.ArrayElementReference, Type: System.Int32[]) (Syntax: 'a[i1 ?? i2]')
                      Array reference:
                        IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.Int32[], IsImplicit) (Syntax: 'a')
                      Indices(1):
                          IFlowCaptureReferenceOperation: 3 (OperationKind.FlowCaptureReference, Type: System.Range, IsImplicit) (Syntax: 'i1 ?? i2')
        Next (Regular) Block[B6]
            Leaving: {R1}
}
Block[B6] - Exit
    Predecessors: [B5]
    Statements (0)
";
            var expectedDiagnostics = DiagnosticDescription.None;

            var comp = CreateCompilationWithIndexAndRange(new[] { source, TestSources.GetSubArray });
            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(comp, expectedFlowGraph, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void ArrayElementReference_ImplicitRangeIndexer_ControlFlowInInstanceAndArgument()
        {
            string source = @"
class C
{
    void M(int[] a1, int[] a2, System.Range? i1, System.Range i2, int[] result)
    /*<bind>*/{
        result = (a1 ?? a2)[i1 ?? i2];
    }/*</bind>*/
}
";
            string expectedFlowGraph = @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
        Entering: {R1}
.locals {R1}
{
    CaptureIds: [0] [2] [4]
    Block[B1] - Block
        Predecessors: [B0]
        Statements (1)
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'result')
              Value:
                IParameterReferenceOperation: result (OperationKind.ParameterReference, Type: System.Int32[]) (Syntax: 'result')
        Next (Regular) Block[B2]
            Entering: {R2}
    .locals {R2}
    {
        CaptureIds: [1]
        Block[B2] - Block
            Predecessors: [B1]
            Statements (1)
                IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'a1')
                  Value:
                    IParameterReferenceOperation: a1 (OperationKind.ParameterReference, Type: System.Int32[]) (Syntax: 'a1')
            Jump if True (Regular) to Block[B4]
                IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsImplicit) (Syntax: 'a1')
                  Operand:
                    IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.Int32[], IsImplicit) (Syntax: 'a1')
                Leaving: {R2}
            Next (Regular) Block[B3]
        Block[B3] - Block
            Predecessors: [B2]
            Statements (1)
                IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'a1')
                  Value:
                    IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.Int32[], IsImplicit) (Syntax: 'a1')
            Next (Regular) Block[B5]
                Leaving: {R2}
                Entering: {R3}
    }
    Block[B4] - Block
        Predecessors: [B2]
        Statements (1)
            IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'a2')
              Value:
                IParameterReferenceOperation: a2 (OperationKind.ParameterReference, Type: System.Int32[]) (Syntax: 'a2')
        Next (Regular) Block[B5]
            Entering: {R3}
    .locals {R3}
    {
        CaptureIds: [3]
        Block[B5] - Block
            Predecessors: [B3] [B4]
            Statements (1)
                IFlowCaptureOperation: 3 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'i1')
                  Value:
                    IParameterReferenceOperation: i1 (OperationKind.ParameterReference, Type: System.Range?) (Syntax: 'i1')
            Jump if True (Regular) to Block[B7]
                IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsImplicit) (Syntax: 'i1')
                  Operand:
                    IFlowCaptureReferenceOperation: 3 (OperationKind.FlowCaptureReference, Type: System.Range?, IsImplicit) (Syntax: 'i1')
                Leaving: {R3}
            Next (Regular) Block[B6]
        Block[B6] - Block
            Predecessors: [B5]
            Statements (1)
                IFlowCaptureOperation: 4 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'i1')
                  Value:
                    IInvocationOperation ( System.Range System.Range?.GetValueOrDefault()) (OperationKind.Invocation, Type: System.Range, IsImplicit) (Syntax: 'i1')
                      Instance Receiver:
                        IFlowCaptureReferenceOperation: 3 (OperationKind.FlowCaptureReference, Type: System.Range?, IsImplicit) (Syntax: 'i1')
                      Arguments(0)
            Next (Regular) Block[B8]
                Leaving: {R3}
    }
    Block[B7] - Block
        Predecessors: [B5]
        Statements (1)
            IFlowCaptureOperation: 4 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'i2')
              Value:
                IParameterReferenceOperation: i2 (OperationKind.ParameterReference, Type: System.Range) (Syntax: 'i2')
        Next (Regular) Block[B8]
    Block[B8] - Block
        Predecessors: [B6] [B7]
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'result = (a ... [i1 ?? i2];')
              Expression:
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32[]) (Syntax: 'result = (a ... )[i1 ?? i2]')
                  Left:
                    IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Int32[], IsImplicit) (Syntax: 'result')
                  Right:
                    IArrayElementReferenceOperation (OperationKind.ArrayElementReference, Type: System.Int32[]) (Syntax: '(a1 ?? a2)[i1 ?? i2]')
                      Array reference:
                        IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: System.Int32[], IsImplicit) (Syntax: 'a1 ?? a2')
                      Indices(1):
                          IFlowCaptureReferenceOperation: 4 (OperationKind.FlowCaptureReference, Type: System.Range, IsImplicit) (Syntax: 'i1 ?? i2')
        Next (Regular) Block[B9]
            Leaving: {R1}
}
Block[B9] - Exit
    Predecessors: [B8]
    Statements (0)
";
            var expectedDiagnostics = DiagnosticDescription.None;

            var comp = CreateCompilationWithIndexAndRange(new[] { source, TestSources.GetSubArray });
            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(comp, expectedFlowGraph, expectedDiagnostics);
        }
    }
}
