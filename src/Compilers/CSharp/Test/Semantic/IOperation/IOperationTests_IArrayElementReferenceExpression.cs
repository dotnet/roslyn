// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    public partial class IOperationTests : SemanticModelTestBase
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
IArrayElementReferenceExpression (OperationKind.ArrayElementReferenceExpression, Type: System.String) (Syntax: 'args[0]')
  Array reference: IParameterReferenceExpression: args (OperationKind.ParameterReferenceExpression, Type: System.String[]) (Syntax: 'args')
  Indices(1):
      ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 0) (Syntax: '0')
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
IArrayElementReferenceExpression (OperationKind.ArrayElementReferenceExpression, Type: System.String) (Syntax: 'args[x]')
  Array reference: IParameterReferenceExpression: args (OperationKind.ParameterReferenceExpression, Type: System.String[]) (Syntax: 'args')
  Indices(1):
      IParameterReferenceExpression: x (OperationKind.ParameterReferenceExpression, Type: System.Int32) (Syntax: 'x')
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
IArrayElementReferenceExpression (OperationKind.ArrayElementReferenceExpression, Type: System.String) (Syntax: 'F2()[0]')
  Array reference: IInvocationExpression ( System.String[] C.F2()) (OperationKind.InvocationExpression, Type: System.String[]) (Syntax: 'F2()')
      Instance Receiver: IInstanceReferenceExpression (OperationKind.InstanceReferenceExpression, Type: C) (Syntax: 'F2')
      Arguments(0)
  Indices(1):
      ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 0) (Syntax: '0')
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
IArrayElementReferenceExpression (OperationKind.ArrayElementReferenceExpression, Type: System.String) (Syntax: 'args[0, 1]')
  Array reference: IParameterReferenceExpression: args (OperationKind.ParameterReferenceExpression, Type: System.String[,]) (Syntax: 'args')
  Indices(2):
      ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 0) (Syntax: '0')
      ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1) (Syntax: '1')
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
IArrayElementReferenceExpression (OperationKind.ArrayElementReferenceExpression, Type: System.String) (Syntax: 'args[x, y]')
  Array reference: IParameterReferenceExpression: args (OperationKind.ParameterReferenceExpression, Type: System.String[,]) (Syntax: 'args')
  Indices(2):
      IParameterReferenceExpression: x (OperationKind.ParameterReferenceExpression, Type: System.Int32) (Syntax: 'x')
      IParameterReferenceExpression: y (OperationKind.ParameterReferenceExpression, Type: System.Int32) (Syntax: 'y')
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
IArrayElementReferenceExpression (OperationKind.ArrayElementReferenceExpression, Type: System.String) (Syntax: 'args[x, F2()]')
  Array reference: IParameterReferenceExpression: args (OperationKind.ParameterReferenceExpression, Type: System.String[,]) (Syntax: 'args')
  Indices(2):
      ILocalReferenceExpression: x (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'x')
      IInvocationExpression ( System.Int32 C.F2()) (OperationKind.InvocationExpression, Type: System.Int32) (Syntax: 'F2()')
        Instance Receiver: IInstanceReferenceExpression (OperationKind.InstanceReferenceExpression, Type: C) (Syntax: 'F2')
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
IArrayElementReferenceExpression (OperationKind.ArrayElementReferenceExpression, Type: System.String) (Syntax: 'args[0][0]')
  Array reference: IArrayElementReferenceExpression (OperationKind.ArrayElementReferenceExpression, Type: System.String[]) (Syntax: 'args[0]')
      Array reference: IParameterReferenceExpression: args (OperationKind.ParameterReferenceExpression, Type: System.String[][]) (Syntax: 'args')
      Indices(1):
          ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 0) (Syntax: '0')
  Indices(1):
      ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 0) (Syntax: '0')
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
IArrayElementReferenceExpression (OperationKind.ArrayElementReferenceExpression, Type: System.String) (Syntax: 'args[F2()][x]')
  Array reference: IArrayElementReferenceExpression (OperationKind.ArrayElementReferenceExpression, Type: System.String[]) (Syntax: 'args[F2()]')
      Array reference: IParameterReferenceExpression: args (OperationKind.ParameterReferenceExpression, Type: System.String[][]) (Syntax: 'args')
      Indices(1):
          IInvocationExpression ( System.Int32 C.F2()) (OperationKind.InvocationExpression, Type: System.Int32) (Syntax: 'F2()')
            Instance Receiver: IInstanceReferenceExpression (OperationKind.InstanceReferenceExpression, Type: C) (Syntax: 'F2')
            Arguments(0)
  Indices(1):
      ILocalReferenceExpression: x (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'x')
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
IArrayElementReferenceExpression (OperationKind.ArrayElementReferenceExpression, Type: System.String) (Syntax: 'args[x][0, F2()]')
  Array reference: IArrayElementReferenceExpression (OperationKind.ArrayElementReferenceExpression, Type: System.String[,]) (Syntax: 'args[x]')
      Array reference: IParameterReferenceExpression: args (OperationKind.ParameterReferenceExpression, Type: System.String[][,]) (Syntax: 'args')
      Indices(1):
          ILocalReferenceExpression: x (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'x')
  Indices(2):
      ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 0) (Syntax: '0')
      IInvocationExpression ( System.Int32 C.F2()) (OperationKind.InvocationExpression, Type: System.Int32) (Syntax: 'F2()')
        Instance Receiver: IInstanceReferenceExpression (OperationKind.InstanceReferenceExpression, Type: C) (Syntax: 'F2')
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
IArrayElementReferenceExpression (OperationKind.ArrayElementReferenceExpression, Type: System.String) (Syntax: 'args[b]')
  Array reference: IParameterReferenceExpression: args (OperationKind.ParameterReferenceExpression, Type: System.String[]) (Syntax: 'args')
  Indices(1):
      IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.Int32) (Syntax: 'b')
        Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: True, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        Operand: IParameterReferenceExpression: b (OperationKind.ParameterReferenceExpression, Type: System.Byte) (Syntax: 'b')
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
IArrayElementReferenceExpression (OperationKind.ArrayElementReferenceExpression, Type: System.String) (Syntax: 'args[(int)d]')
  Array reference: IParameterReferenceExpression: args (OperationKind.ParameterReferenceExpression, Type: System.String[]) (Syntax: 'args')
  Indices(1):
      IConversionExpression (Explicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.Int32) (Syntax: '(int)d')
        Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: True, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        Operand: IParameterReferenceExpression: d (OperationKind.ParameterReferenceExpression, Type: System.Double) (Syntax: 'd')
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
IArrayElementReferenceExpression (OperationKind.ArrayElementReferenceExpression, Type: System.String) (Syntax: 'args[c]')
  Array reference: IParameterReferenceExpression: args (OperationKind.ParameterReferenceExpression, Type: System.String[]) (Syntax: 'args')
  Indices(1):
      IConversionExpression (Implicit, TryCast: False, Unchecked) (OperatorMethod: System.Int32 C.op_Implicit(C c)) (OperationKind.ConversionExpression, Type: System.Int32) (Syntax: 'c')
        Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: True) (MethodSymbol: System.Int32 C.op_Implicit(C c))
        Operand: IParameterReferenceExpression: c (OperationKind.ParameterReferenceExpression, Type: C) (Syntax: 'c')
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
IArrayElementReferenceExpression (OperationKind.ArrayElementReferenceExpression, Type: System.String) (Syntax: 'args[(int)c]')
  Array reference: IParameterReferenceExpression: args (OperationKind.ParameterReferenceExpression, Type: System.String[]) (Syntax: 'args')
  Indices(1):
      IConversionExpression (Explicit, TryCast: False, Unchecked) (OperatorMethod: System.Int32 C.op_Explicit(C c)) (OperationKind.ConversionExpression, Type: System.Int32) (Syntax: '(int)c')
        Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: True) (MethodSymbol: System.Int32 C.op_Explicit(C c))
        Operand: IParameterReferenceExpression: c (OperationKind.ParameterReferenceExpression, Type: C) (Syntax: 'c')
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
IArrayElementReferenceExpression (OperationKind.ArrayElementReferenceExpression, Type: System.String) (Syntax: '((string[])c)[x]')
  Array reference: IConversionExpression (Explicit, TryCast: False, Unchecked) (OperatorMethod: System.String[] C.op_Explicit(C c)) (OperationKind.ConversionExpression, Type: System.String[]) (Syntax: '(string[])c')
      Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: True) (MethodSymbol: System.String[] C.op_Explicit(C c))
      Operand: IParameterReferenceExpression: c (OperationKind.ParameterReferenceExpression, Type: C) (Syntax: 'c')
  Indices(1):
      IParameterReferenceExpression: x (OperationKind.ParameterReferenceExpression, Type: System.Int32) (Syntax: 'x')
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
IArrayElementReferenceExpression (OperationKind.ArrayElementReferenceExpression, Type: System.String, IsInvalid) (Syntax: 'args[c]')
  Array reference: IParameterReferenceExpression: args (OperationKind.ParameterReferenceExpression, Type: System.String[], IsInvalid) (Syntax: 'args')
  Indices(1):
      IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.Int32, IsInvalid) (Syntax: 'c')
        Conversion: CommonConversion (Exists: False, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        Operand: IParameterReferenceExpression: c (OperationKind.ParameterReferenceExpression, Type: C, IsInvalid) (Syntax: 'c')
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS0029: Cannot implicitly convert type 'C' to 'int'
                //         var a = /*<bind>*/args[c]/*</bind>*/;
                Diagnostic(ErrorCode.ERR_NoImplicitConv, "args[c]").WithArguments("C", "int").WithLocation(6, 27)
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
IArrayElementReferenceExpression (OperationKind.ArrayElementReferenceExpression, Type: System.String, IsInvalid) (Syntax: 'args[c]')
  Array reference: IParameterReferenceExpression: args (OperationKind.ParameterReferenceExpression, Type: System.String[], IsInvalid) (Syntax: 'args')
  Indices(1):
      IConversionExpression (Implicit, TryCast: False, Unchecked) (OperatorMethod: System.Int32 C.op_Explicit(C c)) (OperationKind.ConversionExpression, Type: System.Int32, IsInvalid) (Syntax: 'c')
        Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: True) (MethodSymbol: System.Int32 C.op_Explicit(C c))
        Operand: IParameterReferenceExpression: c (OperationKind.ParameterReferenceExpression, Type: C, IsInvalid) (Syntax: 'c')
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS0266: Cannot implicitly convert type 'C' to 'int'. An explicit conversion exists (are you missing a cast?)
                //         var a = /*<bind>*/args[c]/*</bind>*/;
                Diagnostic(ErrorCode.ERR_NoImplicitConvCast, "args[c]").WithArguments("C", "int").WithLocation(6, 27)
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
IInvalidExpression (OperationKind.InvalidExpression, Type: ?, IsInvalid) (Syntax: '[0]')
  Children(2):
      ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 0, IsInvalid) (Syntax: '0')
      IInvalidExpression (OperationKind.InvalidExpression, Type: null, IsInvalid) (Syntax: '')
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
IArrayElementReferenceExpression (OperationKind.ArrayElementReferenceExpression, Type: System.String, IsInvalid) (Syntax: 'args[]')
  Array reference: IParameterReferenceExpression: args (OperationKind.ParameterReferenceExpression, Type: System.String[]) (Syntax: 'args')
  Indices(1):
      IInvalidExpression (OperationKind.InvalidExpression, Type: null, IsInvalid) (Syntax: '')
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
IInvalidExpression (OperationKind.InvalidExpression, Type: ?, IsInvalid) (Syntax: 'c[0]')
  Children(2):
      ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 0, IsInvalid) (Syntax: '0')
      IParameterReferenceExpression: c (OperationKind.ParameterReferenceExpression, Type: C, IsInvalid) (Syntax: 'c')
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
IArrayElementReferenceExpression (OperationKind.ArrayElementReferenceExpression, Type: System.String, IsInvalid) (Syntax: 'args[0, 0]')
  Array reference: IParameterReferenceExpression: args (OperationKind.ParameterReferenceExpression, Type: System.String[], IsInvalid) (Syntax: 'args')
  Indices(2):
      ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 0, IsInvalid) (Syntax: '0')
      ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 0, IsInvalid) (Syntax: '0')
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
IPropertyReferenceExpression: System.Char System.String.this[System.Int32 index] { get; } (OperationKind.PropertyReferenceExpression, Type: System.Char, IsInvalid) (Syntax: 'args[0][]')
  Instance Receiver: IArrayElementReferenceExpression (OperationKind.ArrayElementReferenceExpression, Type: System.String) (Syntax: 'args[0]')
      Array reference: IParameterReferenceExpression: args (OperationKind.ParameterReferenceExpression, Type: System.String[]) (Syntax: 'args')
      Indices(1):
          ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 0) (Syntax: '0')
  Arguments(1):
      IArgument (ArgumentKind.Explicit, Matching Parameter: null) (OperationKind.Argument, IsInvalid) (Syntax: '')
        IInvalidExpression (OperationKind.InvalidExpression, Type: null, IsInvalid) (Syntax: '')
          Children(0)
        InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
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
IInvalidExpression (OperationKind.InvalidExpression, Type: ?, IsInvalid) (Syntax: 'ErrorExpression[0]')
  Children(2):
      ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 0) (Syntax: '0')
      IInvalidExpression (OperationKind.InvalidExpression, Type: ?, IsInvalid) (Syntax: 'ErrorExpression')
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
IArrayElementReferenceExpression (OperationKind.ArrayElementReferenceExpression, Type: System.String, IsInvalid) (Syntax: 'args[ErrorExpression]')
  Array reference: IParameterReferenceExpression: args (OperationKind.ParameterReferenceExpression, Type: System.String[]) (Syntax: 'args')
  Indices(1):
      IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.Int32, IsInvalid) (Syntax: 'ErrorExpression')
        Conversion: CommonConversion (Exists: False, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        Operand: IInvalidExpression (OperationKind.InvalidExpression, Type: ?, IsInvalid) (Syntax: 'ErrorExpression')
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
IArrayElementReferenceExpression (OperationKind.ArrayElementReferenceExpression, Type: System.String, IsInvalid) (Syntax: 'args[0,]')
  Array reference: IParameterReferenceExpression: args (OperationKind.ParameterReferenceExpression, Type: System.String[]) (Syntax: 'args')
  Indices(2):
      ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 0) (Syntax: '0')
      IInvalidExpression (OperationKind.InvalidExpression, Type: null, IsInvalid) (Syntax: '')
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
IArrayElementReferenceExpression (OperationKind.ArrayElementReferenceExpression, Type: System.String, IsInvalid) (Syntax: 'args[/*</bind>*/')
  Array reference: IParameterReferenceExpression: args (OperationKind.ParameterReferenceExpression, Type: System.String[], IsInvalid) (Syntax: 'args')
  Indices(0)
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS1003: Syntax error, ']' expected
                //         var a = /*<bind>*/args[/*</bind>*/;
                Diagnostic(ErrorCode.ERR_SyntaxError, ";").WithArguments("]", ";").WithLocation(6, 43),
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
IArrayElementReferenceExpression (OperationKind.ArrayElementReferenceExpression, Type: System.String, IsInvalid) (Syntax: 'args[0/*</bind>*/')
  Array reference: IParameterReferenceExpression: args (OperationKind.ParameterReferenceExpression, Type: System.String[]) (Syntax: 'args')
  Indices(1):
      ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 0) (Syntax: '0')
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS1003: Syntax error, ']' expected
                //         var a = /*<bind>*/args[0/*</bind>*/;
                Diagnostic(ErrorCode.ERR_SyntaxError, ";").WithArguments("]", ";").WithLocation(6, 44)
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
IInvalidExpression (OperationKind.InvalidExpression, Type: ?, IsInvalid) (Syntax: 'args[y][][][][x]')
  Children(2):
      IParameterReferenceExpression: x (OperationKind.ParameterReferenceExpression, Type: System.Int32) (Syntax: 'x')
      IInvalidExpression (OperationKind.InvalidExpression, Type: ?, IsInvalid) (Syntax: 'args[y][][][]')
        Children(2):
            IInvalidExpression (OperationKind.InvalidExpression, Type: null, IsInvalid) (Syntax: '')
              Children(0)
            IInvalidExpression (OperationKind.InvalidExpression, Type: ?, IsInvalid) (Syntax: 'args[y][][]')
              Children(2):
                  IInvalidExpression (OperationKind.InvalidExpression, Type: null, IsInvalid) (Syntax: '')
                    Children(0)
                  IPropertyReferenceExpression: System.Char System.String.this[System.Int32 index] { get; } (OperationKind.PropertyReferenceExpression, Type: System.Char, IsInvalid) (Syntax: 'args[y][]')
                    Instance Receiver: IArrayElementReferenceExpression (OperationKind.ArrayElementReferenceExpression, Type: System.String) (Syntax: 'args[y]')
                        Array reference: IParameterReferenceExpression: args (OperationKind.ParameterReferenceExpression, Type: System.String[]) (Syntax: 'args')
                        Indices(1):
                            IParameterReferenceExpression: y (OperationKind.ParameterReferenceExpression, Type: System.Int32) (Syntax: 'y')
                    Arguments(1):
                        IArgument (ArgumentKind.Explicit, Matching Parameter: null) (OperationKind.Argument, IsInvalid) (Syntax: '')
                          IInvalidExpression (OperationKind.InvalidExpression, Type: null, IsInvalid) (Syntax: '')
                            Children(0)
                          InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                          OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
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
IArrayElementReferenceExpression (OperationKind.ArrayElementReferenceExpression, Type: System.String, IsInvalid) (Syntax: 'args[name: 0]')
  Array reference: IParameterReferenceExpression: args (OperationKind.ParameterReferenceExpression, Type: System.String[], IsInvalid) (Syntax: 'args')
  Indices(1):
      ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 0, IsInvalid) (Syntax: '0')
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
IArrayElementReferenceExpression (OperationKind.ArrayElementReferenceExpression, Type: System.String, IsInvalid) (Syntax: 'args[ref x, out y]')
  Array reference: IParameterReferenceExpression: args (OperationKind.ParameterReferenceExpression, Type: System.String[,]) (Syntax: 'args')
  Indices(2):
      IParameterReferenceExpression: x (OperationKind.ParameterReferenceExpression, Type: System.Int32, IsInvalid) (Syntax: 'x')
      IParameterReferenceExpression: y (OperationKind.ParameterReferenceExpression, Type: System.Int32, IsInvalid) (Syntax: 'y')
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
IArrayElementReferenceExpression (OperationKind.ArrayElementReferenceExpression, Type: System.String) (Syntax: 'args[-1]')
  Array reference: IParameterReferenceExpression: args (OperationKind.ParameterReferenceExpression, Type: System.String[]) (Syntax: 'args')
  Indices(1):
      IUnaryOperatorExpression (UnaryOperatorKind.Minus) (OperationKind.UnaryOperatorExpression, Type: System.Int32, Constant: -1) (Syntax: '-1')
        Operand: ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1) (Syntax: '1')
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS0251: Indexing an array with a negative index (array indices always start at zero)
                //         var a = /*<bind>*/args[-1]/*</bind>*/;
                Diagnostic(ErrorCode.WRN_NegativeArrayIndex, "-1").WithLocation(6, 32)
            };

            VerifyOperationTreeAndDiagnosticsForTest<ElementAccessExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }
    }
}
