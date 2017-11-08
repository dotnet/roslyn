// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Operations;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    public partial class IOperationTests : SemanticModelTestBase
    {
        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void ICompoundAssignment_NullArgumentToGetConversionThrows()
        {
            Assert.Throws<ArgumentNullException>("compoundAssignment", () => (null as ICompoundAssignmentOperation).GetInConversion());
            Assert.Throws<ArgumentNullException>("compoundAssignment", () => (null as ICompoundAssignmentOperation).GetOutConversion());
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
    }
}
