// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Operations;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    public class IOperationTests_ICompoundAssignmentOperation : SemanticModelTestBase
    {
        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void ICompoundAssignment_NullArgumentToGetConversionThrows()
        {
            ICompoundAssignmentOperation nullAssignment = null;
            Assert.Throws<ArgumentNullException>("compoundAssignment", () => nullAssignment.GetInConversion());
            Assert.Throws<ArgumentNullException>("compoundAssignment", () => nullAssignment.GetOutConversion());
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void ICompoundAssignment_GetConversionOnValidNode_IdentityConversion()
        {
            string source = @"
class C
{
    static void M()
    {
        int x = 1, y = 1;
        /*<bind>*/x += y/*</bind>*/;
    }
}
";

            var syntaxTree = Parse(source);
            var compilation = CreateCompilationWithMscorlib461(new[] { syntaxTree });
            (IOperation operation, _) = GetOperationAndSyntaxForTest<AssignmentExpressionSyntax>(compilation);
            var compoundAssignment = (ICompoundAssignmentOperation)operation;

            Assert.Equal(Conversion.Identity, compoundAssignment.GetInConversion());
            Assert.Equal(Conversion.Identity, compoundAssignment.GetOutConversion());
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void ICompoundAssignment_GetConversionOnValidNode_InOutConversion()
        {
            string source = @"
class C
{
    static void M()
    {
        var c = new C();
        var x = 1;
        /*<bind>*/c += x/*</bind>*/;
    }

    public static implicit operator C(int i)
    {
        return null;
    }

    public static implicit operator int(C c)
    {
        return 0;
    }
}

";

            var syntaxTree = Parse(source);
            var compilation = CreateCompilationWithMscorlib461(new[] { syntaxTree });
            (IOperation operation, SyntaxNode node) = GetOperationAndSyntaxForTest<AssignmentExpressionSyntax>(compilation);
            var compoundAssignment = (ICompoundAssignmentOperation)operation;

            var typeSymbol = compilation.GetTypeByMetadataName("C");
            var implicitSymbols = typeSymbol.GetMembers("op_Implicit").Cast<MethodSymbol>();
            var inSymbol = implicitSymbols.Where(sym => sym.ReturnType.SpecialType == SpecialType.System_Int32).Single();
            var outSymbol = implicitSymbols.Where(sym => sym != inSymbol).Single();

            var inConversion = compoundAssignment.GetInConversion();
            Assert.Equal(ConversionKind.ImplicitUserDefined, inConversion.Kind);
            Assert.Equal(inSymbol, inConversion.Method);

            var outConversion = compoundAssignment.GetOutConversion();
            Assert.Equal(ConversionKind.ImplicitUserDefined, outConversion.Kind);
            Assert.Equal(outSymbol, outConversion.Method);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void ICompoundAssignment_BinaryOperatorInOutConversion()
        {
            string source = @"
class C
{
    static void M()
    {
        var c = new C();
        var x = 1;
        /*<bind>*/c += x/*</bind>*/;
    }

    public static implicit operator C(int i)
    {
        return null;
    }

    public static implicit operator int(C c)
    {
        return 0;
    }
}
";
            string expectedOperationTree = @"
ICompoundAssignmentOperation (BinaryOperatorKind.Add) (OperationKind.CompoundAssignment, Type: C) (Syntax: 'c += x')
  InConversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: True) (MethodSymbol: System.Int32 C.op_Implicit(C c))
  OutConversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: True) (MethodSymbol: C C.op_Implicit(System.Int32 i))
  Left: 
    ILocalReferenceOperation: c (OperationKind.LocalReference, Type: C) (Syntax: 'c')
  Right: 
    ILocalReferenceOperation: x (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'x')
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<AssignmentExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void ICompoundAssignment_UnsignedRightShift()
        {
            string source = @"
class C
{
    static void M()
    {
        int c = 1;
        int x = 1;
        /*<bind>*/c >>>= x/*</bind>*/;
    }
}
";
            string expectedOperationTree = @"
ICompoundAssignmentOperation (BinaryOperatorKind.UnsignedRightShift) (OperationKind.CompoundAssignment, Type: System.Int32) (Syntax: 'c >>>= x')
  InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
  OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
  Left:
    ILocalReferenceOperation: c (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'c')
  Right:
    ILocalReferenceOperation: x (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'x')
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<AssignmentExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void ICompoundAssignment_BinaryOperatorInConversion_InvalidMissingOutConversion()
        {
            string source = @"
class C
{
    static void M()
    {
        var c = new C();
        var x = 1;
        /*<bind>*/c += x/*</bind>*/;
    }

    public static implicit operator int(C c)
    {
        return 0;
    }
}
";
            string expectedOperationTree = @"
ICompoundAssignmentOperation (BinaryOperatorKind.Add) (OperationKind.CompoundAssignment, Type: C, IsInvalid) (Syntax: 'c += x')
  InConversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: True) (MethodSymbol: System.Int32 C.op_Implicit(C c))
  OutConversion: CommonConversion (Exists: False, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
  Left: 
    ILocalReferenceOperation: c (OperationKind.LocalReference, Type: C, IsInvalid) (Syntax: 'c')
  Right: 
    ILocalReferenceOperation: x (OperationKind.LocalReference, Type: System.Int32, IsInvalid) (Syntax: 'x')
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS0029: Cannot implicitly convert type 'int' to 'C'
                //         /*<bind>*/c += x/*</bind>*/;
                Diagnostic(ErrorCode.ERR_NoImplicitConv, "c += x").WithArguments("int", "C").WithLocation(8, 19)
            };

            VerifyOperationTreeAndDiagnosticsForTest<AssignmentExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void ICompoundAssignment_BinaryOperatorOutConversion_InvalidMissingInConversion()
        {
            string source = @"
class C
{
    static void M()
    {
        var c = new C();
        var x = 1;
        /*<bind>*/c += x/*</bind>*/;
    }

    public static implicit operator C(int i)
    {
        return null;
    }
}
";
            string expectedOperationTree = @"
ICompoundAssignmentOperation (BinaryOperatorKind.None) (OperationKind.CompoundAssignment, Type: ?, IsInvalid) (Syntax: 'c += x')
  InConversion: CommonConversion (Exists: False, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
  OutConversion: CommonConversion (Exists: False, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
  Left: 
    ILocalReferenceOperation: c (OperationKind.LocalReference, Type: C, IsInvalid) (Syntax: 'c')
  Right: 
    ILocalReferenceOperation: x (OperationKind.LocalReference, Type: System.Int32, IsInvalid) (Syntax: 'x')
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS0019: Operator '+=' cannot be applied to operands of type 'C' and 'int'
                //         /*<bind>*/c += x/*</bind>*/;
                Diagnostic(ErrorCode.ERR_BadBinaryOps, "c += x").WithArguments("+=", "C", "int").WithLocation(8, 19)
            };

            VerifyOperationTreeAndDiagnosticsForTest<AssignmentExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void ICompoundAssignment_UserDefinedBinaryOperator_InConversion()
        {
            string source = @"
class C
{
    static void M()
    {
        var c = new C();
        var x = 1;
        /*<bind>*/c += x/*</bind>*/;
    }

    public static implicit operator C(int i)
    {
        return null;
    }

    public static implicit operator int(C c)
    {
        return 0;
    }

    public static C operator +(int c1, C c2)
    {
        return null;
    }
}
";
            string expectedOperationTree = @"
ICompoundAssignmentOperation (BinaryOperatorKind.Add) (OperatorMethod: C C.op_Addition(System.Int32 c1, C c2)) (OperationKind.CompoundAssignment, Type: C) (Syntax: 'c += x')
  InConversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: True) (MethodSymbol: System.Int32 C.op_Implicit(C c))
  OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
  Left: 
    ILocalReferenceOperation: c (OperationKind.LocalReference, Type: C) (Syntax: 'c')
  Right: 
    IConversionOperation (TryCast: False, Unchecked) (OperatorMethod: C C.op_Implicit(System.Int32 i)) (OperationKind.Conversion, Type: C, IsImplicit) (Syntax: 'x')
      Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: True) (MethodSymbol: C C.op_Implicit(System.Int32 i))
      Operand: 
        ILocalReferenceOperation: x (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'x')
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<AssignmentExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void ICompoundAssignment_UserDefinedBinaryOperator_UnsignedRightShift()
        {
            string source = @"
class C
{
    static void M()
    {
        var c = new C();
        var x = 1;
        /*<bind>*/c >>>= x/*</bind>*/;
    }

    public static C operator >>>(C c1, int c2)
    {
        return null;
    }
}
";
            string expectedOperationTree = @"
ICompoundAssignmentOperation (BinaryOperatorKind.UnsignedRightShift) (OperatorMethod: C C.op_UnsignedRightShift(C c1, System.Int32 c2)) (OperationKind.CompoundAssignment, Type: C) (Syntax: 'c >>>= x')
  InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
  OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
  Left:
    ILocalReferenceOperation: c (OperationKind.LocalReference, Type: C) (Syntax: 'c')
  Right:
    ILocalReferenceOperation: x (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'x')
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<AssignmentExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void ICompoundAssignment_UserDefinedBinaryOperator_OutConversion()
        {
            string source = @"
class C
{
    static void M()
    {
        var c = new C();
        var x = 1;
        /*<bind>*/c += x/*</bind>*/;
    }

    public static implicit operator C(int i)
    {
        return null;
    }

    public static implicit operator int(C c)
    {
        return 0;
    }

    public static int operator +(C c1, C c2)
    {
        return 0;
    }
}
";
            string expectedOperationTree = @"
ICompoundAssignmentOperation (BinaryOperatorKind.Add) (OperatorMethod: System.Int32 C.op_Addition(C c1, C c2)) (OperationKind.CompoundAssignment, Type: C) (Syntax: 'c += x')
  InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
  OutConversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: True) (MethodSymbol: C C.op_Implicit(System.Int32 i))
  Left: 
    ILocalReferenceOperation: c (OperationKind.LocalReference, Type: C) (Syntax: 'c')
  Right: 
    IConversionOperation (TryCast: False, Unchecked) (OperatorMethod: C C.op_Implicit(System.Int32 i)) (OperationKind.Conversion, Type: C, IsImplicit) (Syntax: 'x')
      Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: True) (MethodSymbol: C C.op_Implicit(System.Int32 i))
      Operand: 
        ILocalReferenceOperation: x (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'x')
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<AssignmentExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void ICompoundAssignment_UserDefinedBinaryOperator_InOutConversion()
        {
            string source = @"
class C
{
    static void M()
    {
        var c = new C();
        var x = 1;
        /*<bind>*/c += x/*</bind>*/;
    }

    public static implicit operator C(int i)
    {
        return null;
    }

    public static implicit operator int(C c)
    {
        return 0;
    }

    public static int operator +(int c1, C c2)
    {
        return 0;
    }
}
";
            string expectedOperationTree = @"
ICompoundAssignmentOperation (BinaryOperatorKind.Add) (OperatorMethod: System.Int32 C.op_Addition(System.Int32 c1, C c2)) (OperationKind.CompoundAssignment, Type: C) (Syntax: 'c += x')
  InConversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: True) (MethodSymbol: System.Int32 C.op_Implicit(C c))
  OutConversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: True) (MethodSymbol: C C.op_Implicit(System.Int32 i))
  Left: 
    ILocalReferenceOperation: c (OperationKind.LocalReference, Type: C) (Syntax: 'c')
  Right: 
    IConversionOperation (TryCast: False, Unchecked) (OperatorMethod: C C.op_Implicit(System.Int32 i)) (OperationKind.Conversion, Type: C, IsImplicit) (Syntax: 'x')
      Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: True) (MethodSymbol: C C.op_Implicit(System.Int32 i))
      Operand: 
        ILocalReferenceOperation: x (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'x')
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<AssignmentExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void ICompoundAssignment_UserDefinedBinaryOperator_InvalidMissingInConversion()
        {
            string source = @"
class C
{
    static void M()
    {
        var c = new C();
        var x = 1;
        /*<bind>*/c += x/*</bind>*/;
    }

    public static implicit operator C(int i)
    {
        return null;
    }

    public static int operator +(int c1, C c2)
    {
        return 0;
    }
}
";
            string expectedOperationTree = @"
ICompoundAssignmentOperation (BinaryOperatorKind.None) (OperationKind.CompoundAssignment, Type: ?, IsInvalid) (Syntax: 'c += x')
  InConversion: CommonConversion (Exists: False, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
  OutConversion: CommonConversion (Exists: False, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
  Left: 
    ILocalReferenceOperation: c (OperationKind.LocalReference, Type: C, IsInvalid) (Syntax: 'c')
  Right: 
    ILocalReferenceOperation: x (OperationKind.LocalReference, Type: System.Int32, IsInvalid) (Syntax: 'x')
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS0019: Operator '+=' cannot be applied to operands of type 'C' and 'int'
                //         /*<bind>*/c += x/*</bind>*/;
                Diagnostic(ErrorCode.ERR_BadBinaryOps, "c += x").WithArguments("+=", "C", "int").WithLocation(8, 19)
            };

            VerifyOperationTreeAndDiagnosticsForTest<AssignmentExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void ICompoundAssignment_UserDefinedBinaryOperator_InvalidMissingOutConversion()
        {
            string source = @"
class C
{
    static void M()
    {
        var c = new C();
        var x = 1;
        /*<bind>*/c += x/*</bind>*/;
    }

    public static implicit operator int(C c)
    {
        return 0;
    }

    public static int operator +(int c1, C c2)
    {
        return 0;
    }
}
";
            string expectedOperationTree = @"
ICompoundAssignmentOperation (BinaryOperatorKind.Add) (OperationKind.CompoundAssignment, Type: C, IsInvalid) (Syntax: 'c += x')
  InConversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: True) (MethodSymbol: System.Int32 C.op_Implicit(C c))
  OutConversion: CommonConversion (Exists: False, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
  Left: 
    ILocalReferenceOperation: c (OperationKind.LocalReference, Type: C, IsInvalid) (Syntax: 'c')
  Right: 
    ILocalReferenceOperation: x (OperationKind.LocalReference, Type: System.Int32, IsInvalid) (Syntax: 'x')
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS0029: Cannot implicitly convert type 'int' to 'C'
                //         /*<bind>*/c += x/*</bind>*/;
                Diagnostic(ErrorCode.ERR_NoImplicitConv, "c += x").WithArguments("int", "C").WithLocation(8, 19)
            };

            VerifyOperationTreeAndDiagnosticsForTest<AssignmentExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void CompoundAssignment_01()
        {
            string source = @"
using System;
class C
{
    void M(int[] a, int? b, int c)
    /*<bind>*/{
        a[0] += b ?? c;
    }/*</bind>*/
}
";
            var expectedDiagnostics = DiagnosticDescription.None;

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
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'a[0]')
              Value: 
                IArrayElementReferenceOperation (OperationKind.ArrayElementReference, Type: System.Int32) (Syntax: 'a[0]')
                  Array reference: 
                    IParameterReferenceOperation: a (OperationKind.ParameterReference, Type: System.Int32[]) (Syntax: 'a')
                  Indices(1):
                      ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 0) (Syntax: '0')

        Next (Regular) Block[B2]
            Entering: {R2}

    .locals {R2}
    {
        CaptureIds: [1]
        Block[B2] - Block
            Predecessors: [B1]
            Statements (1)
                IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'b')
                  Value: 
                    IParameterReferenceOperation: b (OperationKind.ParameterReference, Type: System.Int32?) (Syntax: 'b')

            Jump if True (Regular) to Block[B4]
                IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsImplicit) (Syntax: 'b')
                  Operand: 
                    IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.Int32?, IsImplicit) (Syntax: 'b')
                Leaving: {R2}

            Next (Regular) Block[B3]
        Block[B3] - Block
            Predecessors: [B2]
            Statements (1)
                IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'b')
                  Value: 
                    IInvocationOperation ( System.Int32 System.Int32?.GetValueOrDefault()) (OperationKind.Invocation, Type: System.Int32, IsImplicit) (Syntax: 'b')
                      Instance Receiver: 
                        IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.Int32?, IsImplicit) (Syntax: 'b')
                      Arguments(0)

            Next (Regular) Block[B5]
                Leaving: {R2}
    }

    Block[B4] - Block
        Predecessors: [B2]
        Statements (1)
            IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'c')
              Value: 
                IParameterReferenceOperation: c (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'c')

        Next (Regular) Block[B5]
    Block[B5] - Block
        Predecessors: [B3] [B4]
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'a[0] += b ?? c;')
              Expression: 
                ICompoundAssignmentOperation (BinaryOperatorKind.Add) (OperationKind.CompoundAssignment, Type: System.Int32) (Syntax: 'a[0] += b ?? c')
                  InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  Left: 
                    IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'a[0]')
                  Right: 
                    IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'b ?? c')

        Next (Regular) Block[B6]
            Leaving: {R1}
}

Block[B6] - Exit
    Predecessors: [B5]
    Statements (0)
";
            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(source, expectedFlowGraph, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void CompoundAssignment_02()
        {
            string source = @"
using System;
class C
{
    void M(int[] a, int? b, int c)
    /*<bind>*/{
        a[0] -= b ?? c;
    }/*</bind>*/
}
";
            var expectedDiagnostics = DiagnosticDescription.None;

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
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'a[0]')
              Value: 
                IArrayElementReferenceOperation (OperationKind.ArrayElementReference, Type: System.Int32) (Syntax: 'a[0]')
                  Array reference: 
                    IParameterReferenceOperation: a (OperationKind.ParameterReference, Type: System.Int32[]) (Syntax: 'a')
                  Indices(1):
                      ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 0) (Syntax: '0')

        Next (Regular) Block[B2]
            Entering: {R2}

    .locals {R2}
    {
        CaptureIds: [1]
        Block[B2] - Block
            Predecessors: [B1]
            Statements (1)
                IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'b')
                  Value: 
                    IParameterReferenceOperation: b (OperationKind.ParameterReference, Type: System.Int32?) (Syntax: 'b')

            Jump if True (Regular) to Block[B4]
                IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsImplicit) (Syntax: 'b')
                  Operand: 
                    IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.Int32?, IsImplicit) (Syntax: 'b')
                Leaving: {R2}

            Next (Regular) Block[B3]
        Block[B3] - Block
            Predecessors: [B2]
            Statements (1)
                IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'b')
                  Value: 
                    IInvocationOperation ( System.Int32 System.Int32?.GetValueOrDefault()) (OperationKind.Invocation, Type: System.Int32, IsImplicit) (Syntax: 'b')
                      Instance Receiver: 
                        IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.Int32?, IsImplicit) (Syntax: 'b')
                      Arguments(0)

            Next (Regular) Block[B5]
                Leaving: {R2}
    }

    Block[B4] - Block
        Predecessors: [B2]
        Statements (1)
            IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'c')
              Value: 
                IParameterReferenceOperation: c (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'c')

        Next (Regular) Block[B5]
    Block[B5] - Block
        Predecessors: [B3] [B4]
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'a[0] -= b ?? c;')
              Expression: 
                ICompoundAssignmentOperation (BinaryOperatorKind.Subtract) (OperationKind.CompoundAssignment, Type: System.Int32) (Syntax: 'a[0] -= b ?? c')
                  InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  Left: 
                    IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'a[0]')
                  Right: 
                    IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'b ?? c')

        Next (Regular) Block[B6]
            Leaving: {R1}
}

Block[B6] - Exit
    Predecessors: [B5]
    Statements (0)
";
            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(source, expectedFlowGraph, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void CompoundAssignment_03()
        {
            string source = @"
using System;
class C
{
    void M(int[] a, int? b, int c)
    /*<bind>*/{
        a[0] *= b ?? c;
    }/*</bind>*/
}
";
            var expectedDiagnostics = DiagnosticDescription.None;

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
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'a[0]')
              Value: 
                IArrayElementReferenceOperation (OperationKind.ArrayElementReference, Type: System.Int32) (Syntax: 'a[0]')
                  Array reference: 
                    IParameterReferenceOperation: a (OperationKind.ParameterReference, Type: System.Int32[]) (Syntax: 'a')
                  Indices(1):
                      ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 0) (Syntax: '0')

        Next (Regular) Block[B2]
            Entering: {R2}

    .locals {R2}
    {
        CaptureIds: [1]
        Block[B2] - Block
            Predecessors: [B1]
            Statements (1)
                IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'b')
                  Value: 
                    IParameterReferenceOperation: b (OperationKind.ParameterReference, Type: System.Int32?) (Syntax: 'b')

            Jump if True (Regular) to Block[B4]
                IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsImplicit) (Syntax: 'b')
                  Operand: 
                    IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.Int32?, IsImplicit) (Syntax: 'b')
                Leaving: {R2}

            Next (Regular) Block[B3]
        Block[B3] - Block
            Predecessors: [B2]
            Statements (1)
                IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'b')
                  Value: 
                    IInvocationOperation ( System.Int32 System.Int32?.GetValueOrDefault()) (OperationKind.Invocation, Type: System.Int32, IsImplicit) (Syntax: 'b')
                      Instance Receiver: 
                        IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.Int32?, IsImplicit) (Syntax: 'b')
                      Arguments(0)

            Next (Regular) Block[B5]
                Leaving: {R2}
    }

    Block[B4] - Block
        Predecessors: [B2]
        Statements (1)
            IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'c')
              Value: 
                IParameterReferenceOperation: c (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'c')

        Next (Regular) Block[B5]
    Block[B5] - Block
        Predecessors: [B3] [B4]
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'a[0] *= b ?? c;')
              Expression: 
                ICompoundAssignmentOperation (BinaryOperatorKind.Multiply) (OperationKind.CompoundAssignment, Type: System.Int32) (Syntax: 'a[0] *= b ?? c')
                  InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  Left: 
                    IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'a[0]')
                  Right: 
                    IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'b ?? c')

        Next (Regular) Block[B6]
            Leaving: {R1}
}

Block[B6] - Exit
    Predecessors: [B5]
    Statements (0)
";
            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(source, expectedFlowGraph, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void CompoundAssignment_04()
        {
            string source = @"
using System;
class C
{
    void M(int[] a, int? b, int c)
    /*<bind>*/{
        a[0] /= b ?? c;
    }/*</bind>*/
}
";
            var expectedDiagnostics = DiagnosticDescription.None;

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
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'a[0]')
              Value: 
                IArrayElementReferenceOperation (OperationKind.ArrayElementReference, Type: System.Int32) (Syntax: 'a[0]')
                  Array reference: 
                    IParameterReferenceOperation: a (OperationKind.ParameterReference, Type: System.Int32[]) (Syntax: 'a')
                  Indices(1):
                      ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 0) (Syntax: '0')

        Next (Regular) Block[B2]
            Entering: {R2}

    .locals {R2}
    {
        CaptureIds: [1]
        Block[B2] - Block
            Predecessors: [B1]
            Statements (1)
                IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'b')
                  Value: 
                    IParameterReferenceOperation: b (OperationKind.ParameterReference, Type: System.Int32?) (Syntax: 'b')

            Jump if True (Regular) to Block[B4]
                IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsImplicit) (Syntax: 'b')
                  Operand: 
                    IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.Int32?, IsImplicit) (Syntax: 'b')
                Leaving: {R2}

            Next (Regular) Block[B3]
        Block[B3] - Block
            Predecessors: [B2]
            Statements (1)
                IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'b')
                  Value: 
                    IInvocationOperation ( System.Int32 System.Int32?.GetValueOrDefault()) (OperationKind.Invocation, Type: System.Int32, IsImplicit) (Syntax: 'b')
                      Instance Receiver: 
                        IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.Int32?, IsImplicit) (Syntax: 'b')
                      Arguments(0)

            Next (Regular) Block[B5]
                Leaving: {R2}
    }

    Block[B4] - Block
        Predecessors: [B2]
        Statements (1)
            IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'c')
              Value: 
                IParameterReferenceOperation: c (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'c')

        Next (Regular) Block[B5]
    Block[B5] - Block
        Predecessors: [B3] [B4]
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'a[0] /= b ?? c;')
              Expression: 
                ICompoundAssignmentOperation (BinaryOperatorKind.Divide) (OperationKind.CompoundAssignment, Type: System.Int32) (Syntax: 'a[0] /= b ?? c')
                  InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  Left: 
                    IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'a[0]')
                  Right: 
                    IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'b ?? c')

        Next (Regular) Block[B6]
            Leaving: {R1}
}

Block[B6] - Exit
    Predecessors: [B5]
    Statements (0)
";
            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(source, expectedFlowGraph, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void CompoundAssignment_05()
        {
            string source = @"
using System;
class C
{
    void M(int[] a, int? b, int c)
    /*<bind>*/{
        a[0] %= b ?? c;
    }/*</bind>*/
}
";
            var expectedDiagnostics = DiagnosticDescription.None;

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
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'a[0]')
              Value: 
                IArrayElementReferenceOperation (OperationKind.ArrayElementReference, Type: System.Int32) (Syntax: 'a[0]')
                  Array reference: 
                    IParameterReferenceOperation: a (OperationKind.ParameterReference, Type: System.Int32[]) (Syntax: 'a')
                  Indices(1):
                      ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 0) (Syntax: '0')

        Next (Regular) Block[B2]
            Entering: {R2}

    .locals {R2}
    {
        CaptureIds: [1]
        Block[B2] - Block
            Predecessors: [B1]
            Statements (1)
                IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'b')
                  Value: 
                    IParameterReferenceOperation: b (OperationKind.ParameterReference, Type: System.Int32?) (Syntax: 'b')

            Jump if True (Regular) to Block[B4]
                IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsImplicit) (Syntax: 'b')
                  Operand: 
                    IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.Int32?, IsImplicit) (Syntax: 'b')
                Leaving: {R2}

            Next (Regular) Block[B3]
        Block[B3] - Block
            Predecessors: [B2]
            Statements (1)
                IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'b')
                  Value: 
                    IInvocationOperation ( System.Int32 System.Int32?.GetValueOrDefault()) (OperationKind.Invocation, Type: System.Int32, IsImplicit) (Syntax: 'b')
                      Instance Receiver: 
                        IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.Int32?, IsImplicit) (Syntax: 'b')
                      Arguments(0)

            Next (Regular) Block[B5]
                Leaving: {R2}
    }

    Block[B4] - Block
        Predecessors: [B2]
        Statements (1)
            IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'c')
              Value: 
                IParameterReferenceOperation: c (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'c')

        Next (Regular) Block[B5]
    Block[B5] - Block
        Predecessors: [B3] [B4]
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'a[0] %= b ?? c;')
              Expression: 
                ICompoundAssignmentOperation (BinaryOperatorKind.Remainder) (OperationKind.CompoundAssignment, Type: System.Int32) (Syntax: 'a[0] %= b ?? c')
                  InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  Left: 
                    IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'a[0]')
                  Right: 
                    IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'b ?? c')

        Next (Regular) Block[B6]
            Leaving: {R1}
}

Block[B6] - Exit
    Predecessors: [B5]
    Statements (0)
";
            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(source, expectedFlowGraph, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void CompoundAssignment_06()
        {
            string source = @"
using System;
class C
{
    void M(int[] a, int? b, int c)
    /*<bind>*/{
        a[0] &= b ?? c;
    }/*</bind>*/
}
";
            var expectedDiagnostics = DiagnosticDescription.None;

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
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'a[0]')
              Value: 
                IArrayElementReferenceOperation (OperationKind.ArrayElementReference, Type: System.Int32) (Syntax: 'a[0]')
                  Array reference: 
                    IParameterReferenceOperation: a (OperationKind.ParameterReference, Type: System.Int32[]) (Syntax: 'a')
                  Indices(1):
                      ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 0) (Syntax: '0')

        Next (Regular) Block[B2]
            Entering: {R2}

    .locals {R2}
    {
        CaptureIds: [1]
        Block[B2] - Block
            Predecessors: [B1]
            Statements (1)
                IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'b')
                  Value: 
                    IParameterReferenceOperation: b (OperationKind.ParameterReference, Type: System.Int32?) (Syntax: 'b')

            Jump if True (Regular) to Block[B4]
                IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsImplicit) (Syntax: 'b')
                  Operand: 
                    IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.Int32?, IsImplicit) (Syntax: 'b')
                Leaving: {R2}

            Next (Regular) Block[B3]
        Block[B3] - Block
            Predecessors: [B2]
            Statements (1)
                IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'b')
                  Value: 
                    IInvocationOperation ( System.Int32 System.Int32?.GetValueOrDefault()) (OperationKind.Invocation, Type: System.Int32, IsImplicit) (Syntax: 'b')
                      Instance Receiver: 
                        IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.Int32?, IsImplicit) (Syntax: 'b')
                      Arguments(0)

            Next (Regular) Block[B5]
                Leaving: {R2}
    }

    Block[B4] - Block
        Predecessors: [B2]
        Statements (1)
            IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'c')
              Value: 
                IParameterReferenceOperation: c (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'c')

        Next (Regular) Block[B5]
    Block[B5] - Block
        Predecessors: [B3] [B4]
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'a[0] &= b ?? c;')
              Expression: 
                ICompoundAssignmentOperation (BinaryOperatorKind.And) (OperationKind.CompoundAssignment, Type: System.Int32) (Syntax: 'a[0] &= b ?? c')
                  InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  Left: 
                    IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'a[0]')
                  Right: 
                    IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'b ?? c')

        Next (Regular) Block[B6]
            Leaving: {R1}
}

Block[B6] - Exit
    Predecessors: [B5]
    Statements (0)
";
            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(source, expectedFlowGraph, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void CompoundAssignment_07()
        {
            string source = @"
using System;
class C
{
    void M(int[] a, int? b, int c)
    /*<bind>*/{
        a[0] |= b ?? c;
    }/*</bind>*/
}
";
            var expectedDiagnostics = DiagnosticDescription.None;

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
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'a[0]')
              Value: 
                IArrayElementReferenceOperation (OperationKind.ArrayElementReference, Type: System.Int32) (Syntax: 'a[0]')
                  Array reference: 
                    IParameterReferenceOperation: a (OperationKind.ParameterReference, Type: System.Int32[]) (Syntax: 'a')
                  Indices(1):
                      ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 0) (Syntax: '0')

        Next (Regular) Block[B2]
            Entering: {R2}

    .locals {R2}
    {
        CaptureIds: [1]
        Block[B2] - Block
            Predecessors: [B1]
            Statements (1)
                IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'b')
                  Value: 
                    IParameterReferenceOperation: b (OperationKind.ParameterReference, Type: System.Int32?) (Syntax: 'b')

            Jump if True (Regular) to Block[B4]
                IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsImplicit) (Syntax: 'b')
                  Operand: 
                    IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.Int32?, IsImplicit) (Syntax: 'b')
                Leaving: {R2}

            Next (Regular) Block[B3]
        Block[B3] - Block
            Predecessors: [B2]
            Statements (1)
                IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'b')
                  Value: 
                    IInvocationOperation ( System.Int32 System.Int32?.GetValueOrDefault()) (OperationKind.Invocation, Type: System.Int32, IsImplicit) (Syntax: 'b')
                      Instance Receiver: 
                        IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.Int32?, IsImplicit) (Syntax: 'b')
                      Arguments(0)

            Next (Regular) Block[B5]
                Leaving: {R2}
    }

    Block[B4] - Block
        Predecessors: [B2]
        Statements (1)
            IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'c')
              Value: 
                IParameterReferenceOperation: c (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'c')

        Next (Regular) Block[B5]
    Block[B5] - Block
        Predecessors: [B3] [B4]
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'a[0] |= b ?? c;')
              Expression: 
                ICompoundAssignmentOperation (BinaryOperatorKind.Or) (OperationKind.CompoundAssignment, Type: System.Int32) (Syntax: 'a[0] |= b ?? c')
                  InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  Left: 
                    IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'a[0]')
                  Right: 
                    IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'b ?? c')

        Next (Regular) Block[B6]
            Leaving: {R1}
}

Block[B6] - Exit
    Predecessors: [B5]
    Statements (0)
";
            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(source, expectedFlowGraph, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void CompoundAssignment_08()
        {
            string source = @"
using System;
class C
{
    void M(int[] a, int? b, int c)
    /*<bind>*/{
        a[0] ^= b ?? c;
    }/*</bind>*/
}
";
            var expectedDiagnostics = DiagnosticDescription.None;

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
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'a[0]')
              Value: 
                IArrayElementReferenceOperation (OperationKind.ArrayElementReference, Type: System.Int32) (Syntax: 'a[0]')
                  Array reference: 
                    IParameterReferenceOperation: a (OperationKind.ParameterReference, Type: System.Int32[]) (Syntax: 'a')
                  Indices(1):
                      ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 0) (Syntax: '0')

        Next (Regular) Block[B2]
            Entering: {R2}

    .locals {R2}
    {
        CaptureIds: [1]
        Block[B2] - Block
            Predecessors: [B1]
            Statements (1)
                IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'b')
                  Value: 
                    IParameterReferenceOperation: b (OperationKind.ParameterReference, Type: System.Int32?) (Syntax: 'b')

            Jump if True (Regular) to Block[B4]
                IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsImplicit) (Syntax: 'b')
                  Operand: 
                    IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.Int32?, IsImplicit) (Syntax: 'b')
                Leaving: {R2}

            Next (Regular) Block[B3]
        Block[B3] - Block
            Predecessors: [B2]
            Statements (1)
                IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'b')
                  Value: 
                    IInvocationOperation ( System.Int32 System.Int32?.GetValueOrDefault()) (OperationKind.Invocation, Type: System.Int32, IsImplicit) (Syntax: 'b')
                      Instance Receiver: 
                        IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.Int32?, IsImplicit) (Syntax: 'b')
                      Arguments(0)

            Next (Regular) Block[B5]
                Leaving: {R2}
    }

    Block[B4] - Block
        Predecessors: [B2]
        Statements (1)
            IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'c')
              Value: 
                IParameterReferenceOperation: c (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'c')

        Next (Regular) Block[B5]
    Block[B5] - Block
        Predecessors: [B3] [B4]
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'a[0] ^= b ?? c;')
              Expression: 
                ICompoundAssignmentOperation (BinaryOperatorKind.ExclusiveOr) (OperationKind.CompoundAssignment, Type: System.Int32) (Syntax: 'a[0] ^= b ?? c')
                  InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  Left: 
                    IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'a[0]')
                  Right: 
                    IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'b ?? c')

        Next (Regular) Block[B6]
            Leaving: {R1}
}

Block[B6] - Exit
    Predecessors: [B5]
    Statements (0)
";
            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(source, expectedFlowGraph, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void CompoundAssignment_09()
        {
            string source = @"
using System;
class C
{
    void M(int[] a, int? b, int c)
    /*<bind>*/{
        a[0] <<= b ?? c;
    }/*</bind>*/
}
";
            var expectedDiagnostics = DiagnosticDescription.None;

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
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'a[0]')
              Value: 
                IArrayElementReferenceOperation (OperationKind.ArrayElementReference, Type: System.Int32) (Syntax: 'a[0]')
                  Array reference: 
                    IParameterReferenceOperation: a (OperationKind.ParameterReference, Type: System.Int32[]) (Syntax: 'a')
                  Indices(1):
                      ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 0) (Syntax: '0')

        Next (Regular) Block[B2]
            Entering: {R2}

    .locals {R2}
    {
        CaptureIds: [1]
        Block[B2] - Block
            Predecessors: [B1]
            Statements (1)
                IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'b')
                  Value: 
                    IParameterReferenceOperation: b (OperationKind.ParameterReference, Type: System.Int32?) (Syntax: 'b')

            Jump if True (Regular) to Block[B4]
                IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsImplicit) (Syntax: 'b')
                  Operand: 
                    IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.Int32?, IsImplicit) (Syntax: 'b')
                Leaving: {R2}

            Next (Regular) Block[B3]
        Block[B3] - Block
            Predecessors: [B2]
            Statements (1)
                IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'b')
                  Value: 
                    IInvocationOperation ( System.Int32 System.Int32?.GetValueOrDefault()) (OperationKind.Invocation, Type: System.Int32, IsImplicit) (Syntax: 'b')
                      Instance Receiver: 
                        IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.Int32?, IsImplicit) (Syntax: 'b')
                      Arguments(0)

            Next (Regular) Block[B5]
                Leaving: {R2}
    }

    Block[B4] - Block
        Predecessors: [B2]
        Statements (1)
            IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'c')
              Value: 
                IParameterReferenceOperation: c (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'c')

        Next (Regular) Block[B5]
    Block[B5] - Block
        Predecessors: [B3] [B4]
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'a[0] <<= b ?? c;')
              Expression: 
                ICompoundAssignmentOperation (BinaryOperatorKind.LeftShift) (OperationKind.CompoundAssignment, Type: System.Int32) (Syntax: 'a[0] <<= b ?? c')
                  InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  Left: 
                    IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'a[0]')
                  Right: 
                    IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'b ?? c')

        Next (Regular) Block[B6]
            Leaving: {R1}
}

Block[B6] - Exit
    Predecessors: [B5]
    Statements (0)
";
            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(source, expectedFlowGraph, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void CompoundAssignment_10()
        {
            string source = @"
using System;
class C
{
    void M(int[] a, int? b, int c)
    /*<bind>*/{
        a[0] >>= b ?? c;
    }/*</bind>*/
}
";
            var expectedDiagnostics = DiagnosticDescription.None;

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
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'a[0]')
              Value: 
                IArrayElementReferenceOperation (OperationKind.ArrayElementReference, Type: System.Int32) (Syntax: 'a[0]')
                  Array reference: 
                    IParameterReferenceOperation: a (OperationKind.ParameterReference, Type: System.Int32[]) (Syntax: 'a')
                  Indices(1):
                      ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 0) (Syntax: '0')

        Next (Regular) Block[B2]
            Entering: {R2}

    .locals {R2}
    {
        CaptureIds: [1]
        Block[B2] - Block
            Predecessors: [B1]
            Statements (1)
                IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'b')
                  Value: 
                    IParameterReferenceOperation: b (OperationKind.ParameterReference, Type: System.Int32?) (Syntax: 'b')

            Jump if True (Regular) to Block[B4]
                IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsImplicit) (Syntax: 'b')
                  Operand: 
                    IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.Int32?, IsImplicit) (Syntax: 'b')
                Leaving: {R2}

            Next (Regular) Block[B3]
        Block[B3] - Block
            Predecessors: [B2]
            Statements (1)
                IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'b')
                  Value: 
                    IInvocationOperation ( System.Int32 System.Int32?.GetValueOrDefault()) (OperationKind.Invocation, Type: System.Int32, IsImplicit) (Syntax: 'b')
                      Instance Receiver: 
                        IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.Int32?, IsImplicit) (Syntax: 'b')
                      Arguments(0)

            Next (Regular) Block[B5]
                Leaving: {R2}
    }

    Block[B4] - Block
        Predecessors: [B2]
        Statements (1)
            IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'c')
              Value: 
                IParameterReferenceOperation: c (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'c')

        Next (Regular) Block[B5]
    Block[B5] - Block
        Predecessors: [B3] [B4]
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'a[0] >>= b ?? c;')
              Expression: 
                ICompoundAssignmentOperation (BinaryOperatorKind.RightShift) (OperationKind.CompoundAssignment, Type: System.Int32) (Syntax: 'a[0] >>= b ?? c')
                  InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  Left: 
                    IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'a[0]')
                  Right: 
                    IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'b ?? c')

        Next (Regular) Block[B6]
            Leaving: {R1}
}

Block[B6] - Exit
    Predecessors: [B5]
    Statements (0)
";
            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(source, expectedFlowGraph, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void CompoundAssignment_11()
        {
            string source = @"
using System;
class C
{
    void M(int[] a, int? x, int y, int? b, int c)
    /*<bind>*/{
        a[x ?? y] += b ?? c;
    }/*</bind>*/
}
";
            var expectedDiagnostics = DiagnosticDescription.None;

            string expectedFlowGraph = @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
        Entering: {R1} {R2}
.locals {R1}
{
    CaptureIds: [3] [5]
    .locals {R2}
    {
        CaptureIds: [0] [2]
        Block[B1] - Block
            Predecessors: [B0]
            Statements (1)
                IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'a')
                  Value:
                    IParameterReferenceOperation: a (OperationKind.ParameterReference, Type: System.Int32[]) (Syntax: 'a')
            Next (Regular) Block[B2]
                Entering: {R3}
        .locals {R3}
        {
            CaptureIds: [1]
            Block[B2] - Block
                Predecessors: [B1]
                Statements (1)
                    IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'x')
                      Value:
                        IParameterReferenceOperation: x (OperationKind.ParameterReference, Type: System.Int32?) (Syntax: 'x')
                Jump if True (Regular) to Block[B4]
                    IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsImplicit) (Syntax: 'x')
                      Operand:
                        IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.Int32?, IsImplicit) (Syntax: 'x')
                    Leaving: {R3}
                Next (Regular) Block[B3]
            Block[B3] - Block
                Predecessors: [B2]
                Statements (1)
                    IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'x')
                      Value:
                        IInvocationOperation ( System.Int32 System.Int32?.GetValueOrDefault()) (OperationKind.Invocation, Type: System.Int32, IsImplicit) (Syntax: 'x')
                          Instance Receiver:
                            IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.Int32?, IsImplicit) (Syntax: 'x')
                          Arguments(0)
                Next (Regular) Block[B5]
                    Leaving: {R3}
        }
        Block[B4] - Block
            Predecessors: [B2]
            Statements (1)
                IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'y')
                  Value:
                    IParameterReferenceOperation: y (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'y')
            Next (Regular) Block[B5]
        Block[B5] - Block
            Predecessors: [B3] [B4]
            Statements (1)
                IFlowCaptureOperation: 3 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'a[x ?? y]')
                  Value:
                    IArrayElementReferenceOperation (OperationKind.ArrayElementReference, Type: System.Int32) (Syntax: 'a[x ?? y]')
                      Array reference:
                        IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Int32[], IsImplicit) (Syntax: 'a')
                      Indices(1):
                          IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'x ?? y')
            Next (Regular) Block[B6]
                Leaving: {R2}
                Entering: {R4}
    }
    .locals {R4}
    {
        CaptureIds: [4]
        Block[B6] - Block
            Predecessors: [B5]
            Statements (1)
                IFlowCaptureOperation: 4 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'b')
                  Value:
                    IParameterReferenceOperation: b (OperationKind.ParameterReference, Type: System.Int32?) (Syntax: 'b')
            Jump if True (Regular) to Block[B8]
                IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsImplicit) (Syntax: 'b')
                  Operand:
                    IFlowCaptureReferenceOperation: 4 (OperationKind.FlowCaptureReference, Type: System.Int32?, IsImplicit) (Syntax: 'b')
                Leaving: {R4}
            Next (Regular) Block[B7]
        Block[B7] - Block
            Predecessors: [B6]
            Statements (1)
                IFlowCaptureOperation: 5 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'b')
                  Value:
                    IInvocationOperation ( System.Int32 System.Int32?.GetValueOrDefault()) (OperationKind.Invocation, Type: System.Int32, IsImplicit) (Syntax: 'b')
                      Instance Receiver:
                        IFlowCaptureReferenceOperation: 4 (OperationKind.FlowCaptureReference, Type: System.Int32?, IsImplicit) (Syntax: 'b')
                      Arguments(0)
            Next (Regular) Block[B9]
                Leaving: {R4}
    }
    Block[B8] - Block
        Predecessors: [B6]
        Statements (1)
            IFlowCaptureOperation: 5 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'c')
              Value:
                IParameterReferenceOperation: c (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'c')
        Next (Regular) Block[B9]
    Block[B9] - Block
        Predecessors: [B7] [B8]
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'a[x ?? y] += b ?? c;')
              Expression:
                ICompoundAssignmentOperation (BinaryOperatorKind.Add) (OperationKind.CompoundAssignment, Type: System.Int32) (Syntax: 'a[x ?? y] += b ?? c')
                  InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  Left:
                    IFlowCaptureReferenceOperation: 3 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'a[x ?? y]')
                  Right:
                    IFlowCaptureReferenceOperation: 5 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'b ?? c')
        Next (Regular) Block[B10]
            Leaving: {R1}
}
Block[B10] - Exit
    Predecessors: [B9]
    Statements (0)
";
            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(source, expectedFlowGraph, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void CompoundAssignment_12()
        {
            string source = @"
using System;
class C
{
    void M(S1[] a, int? x, int y, int? b, int c)
    /*<bind>*/{
        a[x ?? y] += b ?? c;
    }/*</bind>*/
}

struct S1
{
    public void operator +=(int i) {}
}
" + CompilerFeatureRequiredAttribute;

            var expectedDiagnostics = DiagnosticDescription.None;

            string expectedFlowGraph = @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
        Entering: {R1} {R2}
.locals {R1}
{
    CaptureIds: [3] [5]
    .locals {R2}
    {
        CaptureIds: [0] [2]
        Block[B1] - Block
            Predecessors: [B0]
            Statements (1)
                IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'a')
                  Value:
                    IParameterReferenceOperation: a (OperationKind.ParameterReference, Type: S1[]) (Syntax: 'a')
            Next (Regular) Block[B2]
                Entering: {R3}
        .locals {R3}
        {
            CaptureIds: [1]
            Block[B2] - Block
                Predecessors: [B1]
                Statements (1)
                    IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'x')
                      Value:
                        IParameterReferenceOperation: x (OperationKind.ParameterReference, Type: System.Int32?) (Syntax: 'x')
                Jump if True (Regular) to Block[B4]
                    IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsImplicit) (Syntax: 'x')
                      Operand:
                        IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.Int32?, IsImplicit) (Syntax: 'x')
                    Leaving: {R3}
                Next (Regular) Block[B3]
            Block[B3] - Block
                Predecessors: [B2]
                Statements (1)
                    IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'x')
                      Value:
                        IInvocationOperation ( System.Int32 System.Int32?.GetValueOrDefault()) (OperationKind.Invocation, Type: System.Int32, IsImplicit) (Syntax: 'x')
                          Instance Receiver:
                            IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.Int32?, IsImplicit) (Syntax: 'x')
                          Arguments(0)
                Next (Regular) Block[B5]
                    Leaving: {R3}
        }
        Block[B4] - Block
            Predecessors: [B2]
            Statements (1)
                IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'y')
                  Value:
                    IParameterReferenceOperation: y (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'y')
            Next (Regular) Block[B5]
        Block[B5] - Block
            Predecessors: [B3] [B4]
            Statements (1)
                IFlowCaptureOperation: 3 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'a[x ?? y]')
                  Value:
                    IArrayElementReferenceOperation (OperationKind.ArrayElementReference, Type: S1) (Syntax: 'a[x ?? y]')
                      Array reference:
                        IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: S1[], IsImplicit) (Syntax: 'a')
                      Indices(1):
                          IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'x ?? y')
            Next (Regular) Block[B6]
                Leaving: {R2}
                Entering: {R4}
    }
    .locals {R4}
    {
        CaptureIds: [4]
        Block[B6] - Block
            Predecessors: [B5]
            Statements (1)
                IFlowCaptureOperation: 4 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'b')
                  Value:
                    IParameterReferenceOperation: b (OperationKind.ParameterReference, Type: System.Int32?) (Syntax: 'b')
            Jump if True (Regular) to Block[B8]
                IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsImplicit) (Syntax: 'b')
                  Operand:
                    IFlowCaptureReferenceOperation: 4 (OperationKind.FlowCaptureReference, Type: System.Int32?, IsImplicit) (Syntax: 'b')
                Leaving: {R4}
            Next (Regular) Block[B7]
        Block[B7] - Block
            Predecessors: [B6]
            Statements (1)
                IFlowCaptureOperation: 5 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'b')
                  Value:
                    IInvocationOperation ( System.Int32 System.Int32?.GetValueOrDefault()) (OperationKind.Invocation, Type: System.Int32, IsImplicit) (Syntax: 'b')
                      Instance Receiver:
                        IFlowCaptureReferenceOperation: 4 (OperationKind.FlowCaptureReference, Type: System.Int32?, IsImplicit) (Syntax: 'b')
                      Arguments(0)
            Next (Regular) Block[B9]
                Leaving: {R4}
    }
    Block[B8] - Block
        Predecessors: [B6]
        Statements (1)
            IFlowCaptureOperation: 5 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'c')
              Value:
                IParameterReferenceOperation: c (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'c')
        Next (Regular) Block[B9]
    Block[B9] - Block
        Predecessors: [B7] [B8]
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'a[x ?? y] += b ?? c;')
              Expression:
                ICompoundAssignmentOperation (BinaryOperatorKind.Add) (OperatorMethod: void S1.op_AdditionAssignment(System.Int32 i)) (OperationKind.CompoundAssignment, Type: System.Void) (Syntax: 'a[x ?? y] += b ?? c')
                  InConversion: CommonConversion (Exists: False, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  OutConversion: CommonConversion (Exists: False, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  Left:
                    IFlowCaptureReferenceOperation: 3 (OperationKind.FlowCaptureReference, Type: S1, IsImplicit) (Syntax: 'a[x ?? y]')
                  Right:
                    IFlowCaptureReferenceOperation: 5 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'b ?? c')
        Next (Regular) Block[B10]
            Leaving: {R1}
}
Block[B10] - Exit
    Predecessors: [B9]
    Statements (0)
";
            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(source, expectedFlowGraph, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void Extensions_01()
        {
            string source = @"
class A
{
    CustomType Method(CustomType x)
    {
        return /*<bind>*/x += 1/*</bind>*/;
    }
}
public struct CustomType
{
}

static class Extensions
{
    extension(CustomType)
    {
        public static CustomType operator +(CustomType x, int y)
        {
            return x;
        }
    }
}
";
            string expectedOperationTree = @"
ICompoundAssignmentOperation (BinaryOperatorKind.Add) (OperatorMethod: CustomType Extensions.<G>$9F7826FAF592F1266BEA2CA4AC24ECDD.op_Addition(CustomType x, System.Int32 y)) (OperationKind.CompoundAssignment, Type: CustomType) (Syntax: 'x += 1')
  InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
  OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
  Left:
    IParameterReferenceOperation: x (OperationKind.ParameterReference, Type: CustomType) (Syntax: 'x')
  Right:
    ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
";
            VerifyOperationTreeForTest<AssignmentExpressionSyntax>(source, expectedOperationTree);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void Extensions_02()
        {
            string source = @"
class A
{
    CustomType Method(CustomType x)
    {
        return /*<bind>*/x += 1/*</bind>*/;
    }
}
public struct CustomType
{
}

static class Extensions
{
    extension(ref CustomType x)
    {
        public void operator +=(int y)
        {
            return x;
        }
    }
}

" + CompilerFeatureRequiredAttribute;

            string expectedOperationTree = @"
ICompoundAssignmentOperation (BinaryOperatorKind.Add) (OperatorMethod: void Extensions.<G>$9F7826FAF592F1266BEA2CA4AC24ECDD.op_AdditionAssignment(System.Int32 y)) (OperationKind.CompoundAssignment, Type: CustomType) (Syntax: 'x += 1')
  InConversion: CommonConversion (Exists: False, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
  OutConversion: CommonConversion (Exists: False, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
  Left:
    IParameterReferenceOperation: x (OperationKind.ParameterReference, Type: CustomType) (Syntax: 'x')
  Right:
    ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
";
            VerifyOperationTreeForTest<AssignmentExpressionSyntax>(source, expectedOperationTree);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void Extensions_03()
        {
            string source = @"
class A
{
    CustomType Method(CustomType x)
    {
        return /*<bind>*/x += 1/*</bind>*/;
    }
}
public class CustomType
{
}

static class Extensions
{
    extension(object)
    {
        public void operator +=(int y)
        {
            return x;
        }
    }
}

" + CompilerFeatureRequiredAttribute;

            string expectedOperationTree = @"
ICompoundAssignmentOperation (BinaryOperatorKind.Add) (OperatorMethod: void Extensions.<G>$C43E2675C7BBF9284AF22FB8A9BF0280.op_AdditionAssignment(System.Int32 y)) (OperationKind.CompoundAssignment, Type: CustomType) (Syntax: 'x += 1')
  InConversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
  OutConversion: CommonConversion (Exists: False, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
  Left:
    IParameterReferenceOperation: x (OperationKind.ParameterReference, Type: CustomType) (Syntax: 'x')
  Right:
    ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
";
            VerifyOperationTreeForTest<AssignmentExpressionSyntax>(source, expectedOperationTree);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void Extensions_04_Flow()
        {
            string source = @"
using System;
class C
{
    void M(S1[] a, int? x, int y, int? b, int c)
    /*<bind>*/{
        a[x ?? y] += b ?? c;
    }/*</bind>*/
}

struct S1
{
}

static class Extensions
{
    extension(S1)
    {
        public static S1 operator +(S1 x, int i) => throw null;
    }
}
";

            var expectedDiagnostics = DiagnosticDescription.None;

            string expectedFlowGraph = @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
        Entering: {R1} {R2}
.locals {R1}
{
    CaptureIds: [3] [5]
    .locals {R2}
    {
        CaptureIds: [0] [2]
        Block[B1] - Block
            Predecessors: [B0]
            Statements (1)
                IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'a')
                  Value:
                    IParameterReferenceOperation: a (OperationKind.ParameterReference, Type: S1[]) (Syntax: 'a')
            Next (Regular) Block[B2]
                Entering: {R3}
        .locals {R3}
        {
            CaptureIds: [1]
            Block[B2] - Block
                Predecessors: [B1]
                Statements (1)
                    IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'x')
                      Value:
                        IParameterReferenceOperation: x (OperationKind.ParameterReference, Type: System.Int32?) (Syntax: 'x')
                Jump if True (Regular) to Block[B4]
                    IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsImplicit) (Syntax: 'x')
                      Operand:
                        IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.Int32?, IsImplicit) (Syntax: 'x')
                    Leaving: {R3}
                Next (Regular) Block[B3]
            Block[B3] - Block
                Predecessors: [B2]
                Statements (1)
                    IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'x')
                      Value:
                        IInvocationOperation ( System.Int32 System.Int32?.GetValueOrDefault()) (OperationKind.Invocation, Type: System.Int32, IsImplicit) (Syntax: 'x')
                          Instance Receiver:
                            IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.Int32?, IsImplicit) (Syntax: 'x')
                          Arguments(0)
                Next (Regular) Block[B5]
                    Leaving: {R3}
        }
        Block[B4] - Block
            Predecessors: [B2]
            Statements (1)
                IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'y')
                  Value:
                    IParameterReferenceOperation: y (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'y')
            Next (Regular) Block[B5]
        Block[B5] - Block
            Predecessors: [B3] [B4]
            Statements (1)
                IFlowCaptureOperation: 3 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'a[x ?? y]')
                  Value:
                    IArrayElementReferenceOperation (OperationKind.ArrayElementReference, Type: S1) (Syntax: 'a[x ?? y]')
                      Array reference:
                        IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: S1[], IsImplicit) (Syntax: 'a')
                      Indices(1):
                          IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'x ?? y')
            Next (Regular) Block[B6]
                Leaving: {R2}
                Entering: {R4}
    }
    .locals {R4}
    {
        CaptureIds: [4]
        Block[B6] - Block
            Predecessors: [B5]
            Statements (1)
                IFlowCaptureOperation: 4 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'b')
                  Value:
                    IParameterReferenceOperation: b (OperationKind.ParameterReference, Type: System.Int32?) (Syntax: 'b')
            Jump if True (Regular) to Block[B8]
                IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsImplicit) (Syntax: 'b')
                  Operand:
                    IFlowCaptureReferenceOperation: 4 (OperationKind.FlowCaptureReference, Type: System.Int32?, IsImplicit) (Syntax: 'b')
                Leaving: {R4}
            Next (Regular) Block[B7]
        Block[B7] - Block
            Predecessors: [B6]
            Statements (1)
                IFlowCaptureOperation: 5 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'b')
                  Value:
                    IInvocationOperation ( System.Int32 System.Int32?.GetValueOrDefault()) (OperationKind.Invocation, Type: System.Int32, IsImplicit) (Syntax: 'b')
                      Instance Receiver:
                        IFlowCaptureReferenceOperation: 4 (OperationKind.FlowCaptureReference, Type: System.Int32?, IsImplicit) (Syntax: 'b')
                      Arguments(0)
            Next (Regular) Block[B9]
                Leaving: {R4}
    }
    Block[B8] - Block
        Predecessors: [B6]
        Statements (1)
            IFlowCaptureOperation: 5 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'c')
              Value:
                IParameterReferenceOperation: c (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'c')
        Next (Regular) Block[B9]
    Block[B9] - Block
        Predecessors: [B7] [B8]
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'a[x ?? y] += b ?? c;')
              Expression:
                ICompoundAssignmentOperation (BinaryOperatorKind.Add) (OperatorMethod: S1 Extensions.<G>$78CFE6F93D970DBBE44B05C24FFEB91E.op_Addition(S1 x, System.Int32 i)) (OperationKind.CompoundAssignment, Type: S1) (Syntax: 'a[x ?? y] += b ?? c')
                  InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  Left:
                    IFlowCaptureReferenceOperation: 3 (OperationKind.FlowCaptureReference, Type: S1, IsImplicit) (Syntax: 'a[x ?? y]')
                  Right:
                    IFlowCaptureReferenceOperation: 5 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'b ?? c')
        Next (Regular) Block[B10]
            Leaving: {R1}
}
Block[B10] - Exit
    Predecessors: [B9]
    Statements (0)
";
            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(source, expectedFlowGraph, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void Extensions_05_Flow()
        {
            string source = @"
using System;
class C
{
    void M(S1[] a, int? x, int y, int? b, int c)
    /*<bind>*/{
        a[x ?? y] += b ?? c;
    }/*</bind>*/
}

struct S1
{
}

static class Extensions
{
    extension(ref S1 x)
    {
        public void operator +=(int i) {}
    }
}
" + CompilerFeatureRequiredAttribute;

            var expectedDiagnostics = DiagnosticDescription.None;

            string expectedFlowGraph = @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
        Entering: {R1} {R2}
.locals {R1}
{
    CaptureIds: [3] [5]
    .locals {R2}
    {
        CaptureIds: [0] [2]
        Block[B1] - Block
            Predecessors: [B0]
            Statements (1)
                IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'a')
                  Value:
                    IParameterReferenceOperation: a (OperationKind.ParameterReference, Type: S1[]) (Syntax: 'a')
            Next (Regular) Block[B2]
                Entering: {R3}
        .locals {R3}
        {
            CaptureIds: [1]
            Block[B2] - Block
                Predecessors: [B1]
                Statements (1)
                    IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'x')
                      Value:
                        IParameterReferenceOperation: x (OperationKind.ParameterReference, Type: System.Int32?) (Syntax: 'x')
                Jump if True (Regular) to Block[B4]
                    IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsImplicit) (Syntax: 'x')
                      Operand:
                        IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.Int32?, IsImplicit) (Syntax: 'x')
                    Leaving: {R3}
                Next (Regular) Block[B3]
            Block[B3] - Block
                Predecessors: [B2]
                Statements (1)
                    IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'x')
                      Value:
                        IInvocationOperation ( System.Int32 System.Int32?.GetValueOrDefault()) (OperationKind.Invocation, Type: System.Int32, IsImplicit) (Syntax: 'x')
                          Instance Receiver:
                            IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.Int32?, IsImplicit) (Syntax: 'x')
                          Arguments(0)
                Next (Regular) Block[B5]
                    Leaving: {R3}
        }
        Block[B4] - Block
            Predecessors: [B2]
            Statements (1)
                IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'y')
                  Value:
                    IParameterReferenceOperation: y (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'y')
            Next (Regular) Block[B5]
        Block[B5] - Block
            Predecessors: [B3] [B4]
            Statements (1)
                IFlowCaptureOperation: 3 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'a[x ?? y]')
                  Value:
                    IArrayElementReferenceOperation (OperationKind.ArrayElementReference, Type: S1) (Syntax: 'a[x ?? y]')
                      Array reference:
                        IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: S1[], IsImplicit) (Syntax: 'a')
                      Indices(1):
                          IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'x ?? y')
            Next (Regular) Block[B6]
                Leaving: {R2}
                Entering: {R4}
    }
    .locals {R4}
    {
        CaptureIds: [4]
        Block[B6] - Block
            Predecessors: [B5]
            Statements (1)
                IFlowCaptureOperation: 4 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'b')
                  Value:
                    IParameterReferenceOperation: b (OperationKind.ParameterReference, Type: System.Int32?) (Syntax: 'b')
            Jump if True (Regular) to Block[B8]
                IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsImplicit) (Syntax: 'b')
                  Operand:
                    IFlowCaptureReferenceOperation: 4 (OperationKind.FlowCaptureReference, Type: System.Int32?, IsImplicit) (Syntax: 'b')
                Leaving: {R4}
            Next (Regular) Block[B7]
        Block[B7] - Block
            Predecessors: [B6]
            Statements (1)
                IFlowCaptureOperation: 5 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'b')
                  Value:
                    IInvocationOperation ( System.Int32 System.Int32?.GetValueOrDefault()) (OperationKind.Invocation, Type: System.Int32, IsImplicit) (Syntax: 'b')
                      Instance Receiver:
                        IFlowCaptureReferenceOperation: 4 (OperationKind.FlowCaptureReference, Type: System.Int32?, IsImplicit) (Syntax: 'b')
                      Arguments(0)
            Next (Regular) Block[B9]
                Leaving: {R4}
    }
    Block[B8] - Block
        Predecessors: [B6]
        Statements (1)
            IFlowCaptureOperation: 5 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'c')
              Value:
                IParameterReferenceOperation: c (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'c')
        Next (Regular) Block[B9]
    Block[B9] - Block
        Predecessors: [B7] [B8]
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'a[x ?? y] += b ?? c;')
              Expression:
                ICompoundAssignmentOperation (BinaryOperatorKind.Add) (OperatorMethod: void Extensions.<G>$78CFE6F93D970DBBE44B05C24FFEB91E.op_AdditionAssignment(System.Int32 i)) (OperationKind.CompoundAssignment, Type: System.Void) (Syntax: 'a[x ?? y] += b ?? c')
                  InConversion: CommonConversion (Exists: False, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  OutConversion: CommonConversion (Exists: False, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  Left:
                    IFlowCaptureReferenceOperation: 3 (OperationKind.FlowCaptureReference, Type: S1, IsImplicit) (Syntax: 'a[x ?? y]')
                  Right:
                    IFlowCaptureReferenceOperation: 5 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'b ?? c')
        Next (Regular) Block[B10]
            Leaving: {R1}
}
Block[B10] - Exit
    Predecessors: [B9]
    Statements (0)
";
            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(source, expectedFlowGraph, expectedDiagnostics);
        }
    }
}
