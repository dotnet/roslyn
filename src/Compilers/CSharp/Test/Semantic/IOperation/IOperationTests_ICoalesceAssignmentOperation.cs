// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    [CompilerTrait(CompilerFeature.IOperation)]
    public partial class IOperationTests : SemanticModelTestBase
    {
        [Fact]
        public void CoalesceAssignment_SimpleCase()
        {

            string source = @"
class C
{
    static void M(object o1, object o2)
    {
        /*<bind>*/o1 ??= o2/*</bind>*/;
    }
}
";
            string expectedOperationTree = @"
ICoalesceAssignmentOperation (OperationKind.CoalesceAssignment, Type: System.Object) (Syntax: 'o1 ??= o2')
  Target: 
    IParameterReferenceOperation: o1 (OperationKind.ParameterReference, Type: System.Object) (Syntax: 'o1')
  WhenNull: 
    IParameterReferenceOperation: o2 (OperationKind.ParameterReference, Type: System.Object) (Syntax: 'o2')
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<AssignmentExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [Fact]
        public void CoalesceAssignment_WithConversion()
        {
            string source = @"
class C
{
    static void M(object o1, string s1)
    {
        /*<bind>*/o1 ??= s1/*</bind>*/;
    }
}
";
            string expectedOperationTree = @"
ICoalesceAssignmentOperation (OperationKind.CoalesceAssignment, Type: System.Object) (Syntax: 'o1 ??= s1')
  Target: 
    IParameterReferenceOperation: o1 (OperationKind.ParameterReference, Type: System.Object) (Syntax: 'o1')
  WhenNull: 
    IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Object, IsImplicit) (Syntax: 's1')
      Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
      Operand: 
        IParameterReferenceOperation: s1 (OperationKind.ParameterReference, Type: System.String) (Syntax: 's1')
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<AssignmentExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [Fact]
        public void CoalesceAssignment_NoConversion()
        {
            string source = @"
class C
{
    static void M(C c1, string s1)
    {
        /*<bind>*/c1 ??= s1/*</bind>*/;
    }
}
";
            string expectedOperationTree = @"
ICoalesceAssignmentOperation (OperationKind.CoalesceAssignment, Type: ?, IsInvalid) (Syntax: 'c1 ??= s1')
  Target: 
    IParameterReferenceOperation: c1 (OperationKind.ParameterReference, Type: C, IsInvalid) (Syntax: 'c1')
  WhenNull: 
    IParameterReferenceOperation: s1 (OperationKind.ParameterReference, Type: System.String, IsInvalid) (Syntax: 's1')
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // file.cs(6,19): error CS0019: Operator '??=' cannot be applied to operands of type 'C' and 'string'
                //         /*<bind>*/c1 ??= s1/*</bind>*/;
                Diagnostic(ErrorCode.ERR_BadBinaryOps, "c1 ??= s1").WithArguments("??=", "C", "string").WithLocation(6, 19)
            };

            VerifyOperationTreeAndDiagnosticsForTest<AssignmentExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [Fact]
        public void CoalesceAssignment_ValueTypeLeft()
        {
            string source = @"
class C
{
    static void M(int i1, string s1)
    {
        /*<bind>*/i1 ??= s1/*</bind>*/;
    }
}
";
            string expectedOperationTree = @"
ICoalesceAssignmentOperation (OperationKind.CoalesceAssignment, Type: ?, IsInvalid) (Syntax: 'i1 ??= s1')
  Target: 
    IParameterReferenceOperation: i1 (OperationKind.ParameterReference, Type: System.Int32, IsInvalid) (Syntax: 'i1')
  WhenNull: 
    IParameterReferenceOperation: s1 (OperationKind.ParameterReference, Type: System.String, IsInvalid) (Syntax: 's1')
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // file.cs(6,19): error CS0019: Operator '??=' cannot be applied to operands of type 'int' and 'string'
                //         /*<bind>*/i1 ??= s1/*</bind>*/;
                Diagnostic(ErrorCode.ERR_BadBinaryOps, "i1 ??= s1").WithArguments("??=", "int", "string").WithLocation(6, 19)
            };

            VerifyOperationTreeAndDiagnosticsForTest<AssignmentExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [Fact]
        public void CoalesceAssignment_MissingLeftAndRight()
        {

            string source = @"
class C
{
    static void M()
    {
        /*<bind>*/o1 ??= o2/*</bind>*/;
    }
}
";
            string expectedOperationTree = @"
ICoalesceAssignmentOperation (OperationKind.CoalesceAssignment, Type: ?, IsInvalid) (Syntax: 'o1 ??= o2')
  Target: 
    IInvalidOperation (OperationKind.Invalid, Type: ?, IsInvalid) (Syntax: 'o1')
      Children(0)
  WhenNull: 
    IInvalidOperation (OperationKind.Invalid, Type: ?, IsInvalid) (Syntax: 'o2')
      Children(0)
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // file.cs(6,19): error CS0103: The name 'o1' does not exist in the current context
                //         /*<bind>*/o1 ??= o2/*</bind>*/;
                Diagnostic(ErrorCode.ERR_NameNotInContext, "o1").WithArguments("o1").WithLocation(6, 19),
                // file.cs(6,26): error CS0103: The name 'o2' does not exist in the current context
                //         /*<bind>*/o1 ??= o2/*</bind>*/;
                Diagnostic(ErrorCode.ERR_NameNotInContext, "o2").WithArguments("o2").WithLocation(6, 26)
            };

            VerifyOperationTreeAndDiagnosticsForTest<AssignmentExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [Fact]
        public void CoalesceAssignment_AsExpression()
        {

            string source = @"
class C
{
    static void M(object o1, object o2)
    {
        /*<bind>*/M2(o1 ??= o2)/*</bind>*/;
    }
    static void M2(object o) {}
}
";
            string expectedOperationTree = @"
IInvocationOperation (void C.M2(System.Object o)) (OperationKind.Invocation, Type: System.Void) (Syntax: 'M2(o1 ??= o2)')
  Instance Receiver: 
    null
  Arguments(1):
      IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: o) (OperationKind.Argument, Type: null) (Syntax: 'o1 ??= o2')
        ICoalesceAssignmentOperation (OperationKind.CoalesceAssignment, Type: System.Object) (Syntax: 'o1 ??= o2')
          Target: 
            IParameterReferenceOperation: o1 (OperationKind.ParameterReference, Type: System.Object) (Syntax: 'o1')
          WhenNull: 
            IParameterReferenceOperation: o2 (OperationKind.ParameterReference, Type: System.Object) (Syntax: 'o2')
        InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<InvocationExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [Fact]
        public void CoalesceAssignment_CheckedDynamic()
        {

            string source = @"
class C
{
    static void M(dynamic d1, dynamic d2)
    {
        checked
        {
            /*<bind>*/d1 ??= d2/*</bind>*/;
        }
    }
}
";
            string expectedOperationTree = @"
ICoalesceAssignmentOperation(IsChecked: True) (OperationKind.CoalesceAssignment, Type: dynamic) (Syntax: 'd1 ??= d2')
  Target: 
    IParameterReferenceOperation: d1 (OperationKind.ParameterReference, Type: dynamic) (Syntax: 'd1')
  WhenNull: 
    IParameterReferenceOperation: d2 (OperationKind.ParameterReference, Type: dynamic) (Syntax: 'd2')
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<AssignmentExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }
    }
}
