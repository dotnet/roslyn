// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Operations;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    public class IOperationTests_IFieldReferenceExpression : SemanticModelTestBase
    {
        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact, WorkItem(8884, "https://github.com/dotnet/roslyn/issues/8884")]
        public void FieldReference_Attribute()
        {
            string source = @"
using System.Diagnostics;

class C
{
    private const string field = nameof(field);

    [/*<bind>*/Conditional(field)/*</bind>*/]
    void M()
    {
    }
}
";
            string expectedOperationTree = @"
IAttributeOperation (OperationKind.Attribute, Type: null) (Syntax: 'Conditional(field)')
  IObjectCreationOperation (Constructor: System.Diagnostics.ConditionalAttribute..ctor(System.String conditionString)) (OperationKind.ObjectCreation, Type: System.Diagnostics.ConditionalAttribute, IsImplicit) (Syntax: 'Conditional(field)')
    Arguments(1):
        IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: conditionString) (OperationKind.Argument, Type: null) (Syntax: 'field')
          IFieldReferenceOperation: System.String C.field (Static) (OperationKind.FieldReference, Type: System.String, Constant: ""field"") (Syntax: 'field')
            Instance Receiver:
              null
          InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
          OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
    Initializer:
      null
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<AttributeSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void IFieldReferenceExpression_OutVar_Script()
        {
            string source = @"
public void M2(out int i )
{
    i = 0;
}

M2(out /*<bind>*/int i/*</bind>*/);
";
            string expectedOperationTree = @"
IDeclarationExpressionOperation (OperationKind.DeclarationExpression, Type: System.Int32) (Syntax: 'int i')
  IFieldReferenceOperation: System.Int32 Script.i (IsDeclaration: True) (OperationKind.FieldReference, Type: System.Int32) (Syntax: 'i')
    Instance Receiver: 
      IInstanceReferenceOperation (ReferenceKind: ContainingTypeInstance) (OperationKind.InstanceReference, Type: Script, IsImplicit) (Syntax: 'i')
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<DeclarationExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics,
                parseOptions: TestOptions.Script);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void IFieldReferenceExpression_DeconstructionDeclaration_Script()
        {
            string source = @"
/*<bind>*/(int i1, int i2)/*</bind>*/ = (1, 2);
";
            string expectedOperationTree = @"
ITupleOperation (OperationKind.Tuple, Type: (System.Int32 i1, System.Int32 i2)) (Syntax: '(int i1, int i2)')
  NaturalType: (System.Int32 i1, System.Int32 i2)
  Elements(2):
      IDeclarationExpressionOperation (OperationKind.DeclarationExpression, Type: System.Int32) (Syntax: 'int i1')
        IFieldReferenceOperation: System.Int32 Script.i1 (IsDeclaration: True) (OperationKind.FieldReference, Type: System.Int32) (Syntax: 'i1')
          Instance Receiver: 
            IInstanceReferenceOperation (ReferenceKind: ContainingTypeInstance) (OperationKind.InstanceReference, Type: Script, IsImplicit) (Syntax: 'i1')
      IDeclarationExpressionOperation (OperationKind.DeclarationExpression, Type: System.Int32) (Syntax: 'int i2')
        IFieldReferenceOperation: System.Int32 Script.i2 (IsDeclaration: True) (OperationKind.FieldReference, Type: System.Int32) (Syntax: 'i2')
          Instance Receiver: 
            IInstanceReferenceOperation (ReferenceKind: ContainingTypeInstance) (OperationKind.InstanceReference, Type: Script, IsImplicit) (Syntax: 'i2')
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<TupleExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics,
                parseOptions: TestOptions.Script);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void IFieldReferenceExpression_InferenceOutVar_Script()
        {
            string source = @"
public void M2(out int i )
{
    i = 0;
}

M2(out /*<bind>*/var i/*</bind>*/);
";
            string expectedOperationTree = @"
IDeclarationExpressionOperation (OperationKind.DeclarationExpression, Type: System.Int32) (Syntax: 'var i')
  IFieldReferenceOperation: System.Int32 Script.i (IsDeclaration: True) (OperationKind.FieldReference, Type: System.Int32) (Syntax: 'i')
    Instance Receiver: 
      IInstanceReferenceOperation (ReferenceKind: ContainingTypeInstance) (OperationKind.InstanceReference, Type: Script, IsImplicit) (Syntax: 'i')
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<DeclarationExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics,
                parseOptions: TestOptions.Script);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void IFieldReferenceExpression_InferenceDeconstructionDeclaration_Script()
        {
            string source = @"
/*<bind>*/(var i1, var i2)/*</bind>*/ = (1, 2);
";
            string expectedOperationTree = @"
ITupleOperation (OperationKind.Tuple, Type: (System.Int32 i1, System.Int32 i2)) (Syntax: '(var i1, var i2)')
  NaturalType: (System.Int32 i1, System.Int32 i2)
  Elements(2):
      IDeclarationExpressionOperation (OperationKind.DeclarationExpression, Type: System.Int32) (Syntax: 'var i1')
        IFieldReferenceOperation: System.Int32 Script.i1 (IsDeclaration: True) (OperationKind.FieldReference, Type: System.Int32) (Syntax: 'i1')
          Instance Receiver: 
            IInstanceReferenceOperation (ReferenceKind: ContainingTypeInstance) (OperationKind.InstanceReference, Type: Script, IsImplicit) (Syntax: 'i1')
      IDeclarationExpressionOperation (OperationKind.DeclarationExpression, Type: System.Int32) (Syntax: 'var i2')
        IFieldReferenceOperation: System.Int32 Script.i2 (IsDeclaration: True) (OperationKind.FieldReference, Type: System.Int32) (Syntax: 'i2')
          Instance Receiver: 
            IInstanceReferenceOperation (ReferenceKind: ContainingTypeInstance) (OperationKind.InstanceReference, Type: Script, IsImplicit) (Syntax: 'i2')
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<TupleExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics,
                parseOptions: TestOptions.Script);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void IFieldReferenceExpression_InferenceDeconstructionDeclaration_AlternateSyntax_Script()
        {
            string source = @"
/*<bind>*/var (i1, i2)/*</bind>*/ = (1, 2);
";
            string expectedOperationTree = @"
IDeclarationExpressionOperation (OperationKind.DeclarationExpression, Type: (System.Int32 i1, System.Int32 i2)) (Syntax: 'var (i1, i2)')
  ITupleOperation (OperationKind.Tuple, Type: (System.Int32 i1, System.Int32 i2)) (Syntax: '(i1, i2)')
    NaturalType: (System.Int32 i1, System.Int32 i2)
    Elements(2):
        IFieldReferenceOperation: System.Int32 Script.i1 (IsDeclaration: True) (OperationKind.FieldReference, Type: System.Int32) (Syntax: 'i1')
          Instance Receiver: 
            IInstanceReferenceOperation (ReferenceKind: ContainingTypeInstance) (OperationKind.InstanceReference, Type: Script, IsImplicit) (Syntax: 'i1')
        IFieldReferenceOperation: System.Int32 Script.i2 (IsDeclaration: True) (OperationKind.FieldReference, Type: System.Int32) (Syntax: 'i2')
          Instance Receiver: 
            IInstanceReferenceOperation (ReferenceKind: ContainingTypeInstance) (OperationKind.InstanceReference, Type: Script, IsImplicit) (Syntax: 'i2')
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<DeclarationExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics,
                parseOptions: TestOptions.Script);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [WorkItem(7582, "https://github.com/dotnet/roslyn/issues/7582")]
        [Fact]
        public void IFieldReferenceExpression_ImplicitThis()
        {
            string source = @"
class C
{
    int i;
    void M()
    {
        /*<bind>*/i/*</bind>*/ = 1;
        i++;
    }
}
";
            string expectedOperationTree = @"
IFieldReferenceOperation: System.Int32 C.i (OperationKind.FieldReference, Type: System.Int32) (Syntax: 'i')
  Instance Receiver: 
    IInstanceReferenceOperation (ReferenceKind: ContainingTypeInstance) (OperationKind.InstanceReference, Type: C, IsImplicit) (Syntax: 'i')
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<IdentifierNameSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [WorkItem(7582, "https://github.com/dotnet/roslyn/issues/7582")]
        [Fact]
        public void IFieldReferenceExpression_ExplicitThis()
        {
            string source = @"
class C
{
    int i;
    void M()
    {
        /*<bind>*/this.i/*</bind>*/ = 1;
        i++;
    }
}
";
            string expectedOperationTree = @"
IFieldReferenceOperation: System.Int32 C.i (OperationKind.FieldReference, Type: System.Int32) (Syntax: 'this.i')
  Instance Receiver: 
    IInstanceReferenceOperation (ReferenceKind: ContainingTypeInstance) (OperationKind.InstanceReference, Type: C) (Syntax: 'this')
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<MemberAccessExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [WorkItem(7582, "https://github.com/dotnet/roslyn/issues/7582")]
        [Fact]
        public void IFieldReferenceExpression_base()
        {
            string source = @"
class C
{
    protected int i;
}
class B : C
{
    void M()
    {
        /*<bind>*/base.i/*</bind>*/ = 1;
        i++;
    }
}
";
            string expectedOperationTree = @"
IFieldReferenceOperation: System.Int32 C.i (OperationKind.FieldReference, Type: System.Int32) (Syntax: 'base.i')
  Instance Receiver: 
    IInstanceReferenceOperation (ReferenceKind: ContainingTypeInstance) (OperationKind.InstanceReference, Type: C) (Syntax: 'base')
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<MemberAccessExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void IFieldReferenceExpression_IndexingFixed_Unmovable()
        {
            string source = @"

unsafe struct S1
{
    public fixed int field[10];
}

unsafe class C
{
    void M()
    {
        S1 s = default;

        /*<bind>*/ s.field[3] = 1; /*</bind>*/
    }
}
";
            string expectedOperationTree = @"
ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32) (Syntax: 's.field[3] = 1')
  Left: 
    IOperation:  (OperationKind.None, Type: System.Int32) (Syntax: 's.field[3]')
      Children(2):
          IFieldReferenceOperation: System.Int32* S1.field (OperationKind.FieldReference, Type: System.Int32*) (Syntax: 's.field')
            Instance Receiver: 
              ILocalReferenceOperation: s (OperationKind.LocalReference, Type: S1) (Syntax: 's')
          ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 3) (Syntax: '3')
  Right: 
    ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<AssignmentExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics, compilationOptions: TestOptions.UnsafeDebugDll);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void IFieldReferenceExpression_IndexingFixed_Movable()
        {
            string source = @"

unsafe struct S1
{
    public fixed int field[10];
}

unsafe class C
{
    S1 s = default;

    void M()
    {
        /*<bind>*/ s.field[3] = 1; /*</bind>*/
    }
}
";
            string expectedOperationTree = @"
ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32) (Syntax: 's.field[3] = 1')
  Left: 
    IOperation:  (OperationKind.None, Type: System.Int32) (Syntax: 's.field[3]')
      Children(2):
          IFieldReferenceOperation: System.Int32* S1.field (OperationKind.FieldReference, Type: System.Int32*) (Syntax: 's.field')
            Instance Receiver: 
              IFieldReferenceOperation: S1 C.s (OperationKind.FieldReference, Type: S1) (Syntax: 's')
                Instance Receiver: 
                  IInstanceReferenceOperation (ReferenceKind: ContainingTypeInstance) (OperationKind.InstanceReference, Type: C, IsImplicit) (Syntax: 's')
          ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 3) (Syntax: '3')
  Right: 
    ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<AssignmentExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics, compilationOptions: TestOptions.UnsafeDebugDll);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void IFieldReference_StaticFieldWithInstanceReceiver()
        {
            string source = @"
class C
{
    static int i;

    public static void M()
    {
        var c = new C();
        var i1 = /*<bind>*/c.i/*</bind>*/;
    }
}
";
            string expectedOperationTree = @"
IFieldReferenceOperation: System.Int32 C.i (Static) (OperationKind.FieldReference, Type: System.Int32, IsInvalid) (Syntax: 'c.i')
  Instance Receiver: 
    ILocalReferenceOperation: c (OperationKind.LocalReference, Type: C, IsInvalid) (Syntax: 'c')
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS0176: Member 'C.i' cannot be accessed with an instance reference; qualify it with a type name instead
                //         var i1 = /*<bind>*/c.i/*</bind>*/;
                Diagnostic(ErrorCode.ERR_ObjectProhibited, "c.i").WithArguments("C.i").WithLocation(9, 28),
                // CS0649: Field 'C.i' is never assigned to, and will always have its default value 0
                //     static int i;
                Diagnostic(ErrorCode.WRN_UnassignedInternalField, "i").WithArguments("C.i", "0").WithLocation(4, 16)
            };

            VerifyOperationTreeAndDiagnosticsForTest<MemberAccessExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void IFieldReference_StaticFieldInObjectInitializer_NoInstance()
        {
            string source = @"
class C
{
    static int i1;
    public static void Main()
    {
        var c = new C { /*<bind>*/i1/*</bind>*/ = 1 };
    }
}
";
            string expectedOperationTree = @"
IFieldReferenceOperation: System.Int32 C.i1 (Static) (OperationKind.FieldReference, Type: System.Int32, IsInvalid) (Syntax: 'i1')
  Instance Receiver: 
    null
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS1914: Static field or property 'C.i1' cannot be assigned in an object initializer
                //         var c = new C { /*<bind>*/i1/*</bind>*/ = 1 };
                Diagnostic(ErrorCode.ERR_StaticMemberInObjectInitializer, "i1").WithArguments("C.i1").WithLocation(7, 35),
                // CS0414: The field 'C.i1' is assigned but its value is never used
                //     static int i1;
                Diagnostic(ErrorCode.WRN_UnreferencedFieldAssg, "i1").WithArguments("C.i1").WithLocation(4, 16)
            };

            VerifyOperationTreeAndDiagnosticsForTest<IdentifierNameSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void IFieldReference_StaticField()
        {
            string source = @"
class C
{
    static int i;

    public static void M()
    {
        var i1 = /*<bind>*/C.i/*</bind>*/;
    }
}
";
            string expectedOperationTree = @"
IFieldReferenceOperation: System.Int32 C.i (Static) (OperationKind.FieldReference, Type: System.Int32) (Syntax: 'C.i')
  Instance Receiver: 
    null
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS0649: Field 'C.i' is never assigned to, and will always have its default value 0
                //     static int i;
                Diagnostic(ErrorCode.WRN_UnassignedInternalField, "i").WithArguments("C.i", "0").WithLocation(4, 16)
            };

            VerifyOperationTreeAndDiagnosticsForTest<MemberAccessExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void IFieldReference_InstanceField_InvalidAccessOffOfClass()
        {
            string source = @"
class C
{
    int i;

    public static void M()
    {
        var i1 = /*<bind>*/C.i/*</bind>*/;
    }
}
";
            string expectedOperationTree = @"
IFieldReferenceOperation: System.Int32 C.i (OperationKind.FieldReference, Type: System.Int32, IsInvalid) (Syntax: 'C.i')
  Instance Receiver: 
    null
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS0120: An object reference is required for the non-static field, method, or property 'C.i'
                //         var i1 = /*<bind>*/C.i/*</bind>*/;
                Diagnostic(ErrorCode.ERR_ObjectRequired, "C.i").WithArguments("C.i").WithLocation(8, 28),
                // CS0649: Field 'C.i' is never assigned to, and will always have its default value 0
                //     int i;
                Diagnostic(ErrorCode.WRN_UnassignedInternalField, "i").WithArguments("C.i", "0").WithLocation(4, 9)
            };

            VerifyOperationTreeAndDiagnosticsForTest<MemberAccessExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void FieldReference_NoControlFlow()
        {
            // Verify mix of field references with implicit/explicit/null instance in lvalue/rvalue contexts.
            string source = @"
class C
{
    int i;
    static int j;
    void M(C c)
    /*<bind>*/{
        i = C.j;
        j = this.i + c.i;
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
        IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'i = C.j;')
          Expression: 
            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32) (Syntax: 'i = C.j')
              Left: 
                IFieldReferenceOperation: System.Int32 C.i (OperationKind.FieldReference, Type: System.Int32) (Syntax: 'i')
                  Instance Receiver: 
                    IInstanceReferenceOperation (ReferenceKind: ContainingTypeInstance) (OperationKind.InstanceReference, Type: C, IsImplicit) (Syntax: 'i')
              Right: 
                IFieldReferenceOperation: System.Int32 C.j (Static) (OperationKind.FieldReference, Type: System.Int32) (Syntax: 'C.j')
                  Instance Receiver: 
                    null

        IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'j = this.i + c.i;')
          Expression: 
            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32) (Syntax: 'j = this.i + c.i')
              Left: 
                IFieldReferenceOperation: System.Int32 C.j (Static) (OperationKind.FieldReference, Type: System.Int32) (Syntax: 'j')
                  Instance Receiver: 
                    null
              Right: 
                IBinaryOperation (BinaryOperatorKind.Add) (OperationKind.Binary, Type: System.Int32) (Syntax: 'this.i + c.i')
                  Left: 
                    IFieldReferenceOperation: System.Int32 C.i (OperationKind.FieldReference, Type: System.Int32) (Syntax: 'this.i')
                      Instance Receiver: 
                        IInstanceReferenceOperation (ReferenceKind: ContainingTypeInstance) (OperationKind.InstanceReference, Type: C) (Syntax: 'this')
                  Right: 
                    IFieldReferenceOperation: System.Int32 C.i (OperationKind.FieldReference, Type: System.Int32) (Syntax: 'c.i')
                      Instance Receiver: 
                        IParameterReferenceOperation: c (OperationKind.ParameterReference, Type: C) (Syntax: 'c')

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
        public void FieldReference_ControlFlowInReceiver()
        {
            string source = @"
class C
{
    public int i = 0;
    void M(C c1, C c2, int p)
    /*<bind>*/{
        p = (c1 ?? c2).i;
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
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'p')
              Value: 
                IParameterReferenceOperation: p (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'p')

        Next (Regular) Block[B2]
            Entering: {R2}

    .locals {R2}
    {
        CaptureIds: [1]
        Block[B2] - Block
            Predecessors: [B1]
            Statements (1)
                IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'c1')
                  Value: 
                    IParameterReferenceOperation: c1 (OperationKind.ParameterReference, Type: C) (Syntax: 'c1')

            Jump if True (Regular) to Block[B4]
                IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsImplicit) (Syntax: 'c1')
                  Operand: 
                    IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: C, IsImplicit) (Syntax: 'c1')
                Leaving: {R2}

            Next (Regular) Block[B3]
        Block[B3] - Block
            Predecessors: [B2]
            Statements (1)
                IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'c1')
                  Value: 
                    IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: C, IsImplicit) (Syntax: 'c1')

            Next (Regular) Block[B5]
                Leaving: {R2}
    }

    Block[B4] - Block
        Predecessors: [B2]
        Statements (1)
            IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'c2')
              Value: 
                IParameterReferenceOperation: c2 (OperationKind.ParameterReference, Type: C) (Syntax: 'c2')

        Next (Regular) Block[B5]
    Block[B5] - Block
        Predecessors: [B3] [B4]
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'p = (c1 ?? c2).i;')
              Expression: 
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32) (Syntax: 'p = (c1 ?? c2).i')
                  Left: 
                    IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'p')
                  Right: 
                    IFieldReferenceOperation: System.Int32 C.i (OperationKind.FieldReference, Type: System.Int32) (Syntax: '(c1 ?? c2).i')
                      Instance Receiver: 
                        IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: C, IsImplicit) (Syntax: 'c1 ?? c2')

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
        public void FieldReference_ControlFlowInReceiver_StaticField()
        {
            string source = @"
class C
{
    public static int i = 0;
    void M(C c1, C c2, int p1, int p2)
    /*<bind>*/{
        p1 = c1.i;
        p2 = (c1 ?? c2).i;
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
        IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null, IsInvalid) (Syntax: 'p1 = c1.i;')
          Expression: 
            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32, IsInvalid) (Syntax: 'p1 = c1.i')
              Left: 
                IParameterReferenceOperation: p1 (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'p1')
              Right: 
                IFieldReferenceOperation: System.Int32 C.i (Static) (OperationKind.FieldReference, Type: System.Int32, IsInvalid) (Syntax: 'c1.i')
                  Instance Receiver: 
                    null

        IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null, IsInvalid) (Syntax: 'p2 = (c1 ?? c2).i;')
          Expression: 
            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32, IsInvalid) (Syntax: 'p2 = (c1 ?? c2).i')
              Left: 
                IParameterReferenceOperation: p2 (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'p2')
              Right: 
                IFieldReferenceOperation: System.Int32 C.i (Static) (OperationKind.FieldReference, Type: System.Int32, IsInvalid) (Syntax: '(c1 ?? c2).i')
                  Instance Receiver: 
                    null

    Next (Regular) Block[B2]
Block[B2] - Exit
    Predecessors: [B1]
    Statements (0)
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // file.cs(7,14): error CS0176: Member 'C.i' cannot be accessed with an instance reference; qualify it with a type name instead
                //         p1 = c1.i;
                Diagnostic(ErrorCode.ERR_ObjectProhibited, "c1.i").WithArguments("C.i").WithLocation(7, 14),
                // file.cs(8,14): error CS0176: Member 'C.i' cannot be accessed with an instance reference; qualify it with a type name instead
                //         p2 = (c1 ?? c2).i;
                Diagnostic(ErrorCode.ERR_ObjectProhibited, "(c1 ?? c2).i").WithArguments("C.i").WithLocation(8, 14)
            };

            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(source, expectedFlowGraph, expectedDiagnostics);
        }

        [Fact]
        [WorkItem(38195, "https://github.com/dotnet/roslyn/issues/38195")]
        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.NullableReferenceTypes)]
        public void NullableFieldReference()
        {
            var program = @"
class C<T>
{
    private C<T> _field;
    public static void M(C<T> p)
    {
        _ = p._field;
    }
}";

            var compWithoutNullable = CreateCompilation(program);
            var compWithNullable = CreateCompilation(program, options: WithNullableEnable());

            testCore(compWithoutNullable);
            testCore(compWithNullable);

            static void testCore(CSharpCompilation comp)
            {
                var syntaxTree = comp.SyntaxTrees[0];
                var model = comp.GetSemanticModel(syntaxTree);
                var root = syntaxTree.GetRoot();
                var classDecl = root.DescendantNodes().OfType<ClassDeclarationSyntax>().Single();
                var classSym = (INamedTypeSymbol)model.GetDeclaredSymbol(classDecl);
                var fieldSym = classSym.GetMembers("_field").Single();

                var methodDecl = root.DescendantNodes().OfType<MethodDeclarationSyntax>().Single();
                var methodBlockOperation = model.GetOperation(methodDecl);
                var fieldReferenceOperation = methodBlockOperation.Descendants().OfType<IFieldReferenceOperation>().Single();
                Assert.True(fieldSym.Equals(fieldReferenceOperation.Field));
                Assert.Equal(fieldSym.GetHashCode(), fieldReferenceOperation.Field.GetHashCode());
            }
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/79201")]
        public void FieldKeyword_01()
        {
            var source = """
                class C
                {
                    public string Prop
                    {
                        get
                        {
                            /*<bind>*/
                            return field + "a";
                            /*</bind>*/
                        }
                    }
                }
                """;

            VerifyOperationTreeAndDiagnosticsForTest<StatementSyntax>(
                source,
                expectedOperationTree: """
                    IReturnOperation (OperationKind.Return, Type: null) (Syntax: 'return field + "a";')
                      ReturnedValue:
                        IBinaryOperation (BinaryOperatorKind.Add) (OperationKind.Binary, Type: System.String) (Syntax: 'field + "a"')
                          Left:
                            IFieldReferenceOperation: System.String C.<Prop>k__BackingField (OperationKind.FieldReference, Type: System.String) (Syntax: 'field')
                              Instance Receiver:
                                IInstanceReferenceOperation (ReferenceKind: ContainingTypeInstance) (OperationKind.InstanceReference, Type: C, IsImplicit) (Syntax: 'field')
                          Right:
                            ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: "a") (Syntax: '"a"')
                    """,
                expectedDiagnostics: []);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/79201")]
        public void FieldKeyword_02()
        {
            var source = """
                class C
                {
                    public string Prop
                    {
                        set
                        {
                            /*<bind>*/
                            field = value;
                            /*</bind>*/
                        }
                    }
                }
                """;

            VerifyOperationTreeAndDiagnosticsForTest<StatementSyntax>(
                source,
                expectedOperationTree: """
                    IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'field = value;')
                      Expression:
                        ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.String) (Syntax: 'field = value')
                          Left:
                            IFieldReferenceOperation: System.String C.<Prop>k__BackingField (OperationKind.FieldReference, Type: System.String) (Syntax: 'field')
                              Instance Receiver:
                                IInstanceReferenceOperation (ReferenceKind: ContainingTypeInstance) (OperationKind.InstanceReference, Type: C, IsImplicit) (Syntax: 'field')
                          Right:
                            IParameterReferenceOperation: value (OperationKind.ParameterReference, Type: System.String) (Syntax: 'value')
                    """,
                expectedDiagnostics: []);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/79201")]
        public void FieldKeyword_03()
        {
            var source = """
                class C
                {
                    public string Prop
                    {
                        get
                        {
                            /*<bind>*/
                            return field + "a";
                            /*</bind>*/
                        }
                    }
                }
                """;

            VerifyOperationTreeAndDiagnosticsForTest<StatementSyntax>(
                source,
                expectedOperationTree: """
                    IReturnOperation (OperationKind.Return, Type: null, IsInvalid) (Syntax: 'return field + "a";')
                      ReturnedValue:
                        IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.String, IsInvalid, IsImplicit) (Syntax: 'field + "a"')
                          Conversion: CommonConversion (Exists: False, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                          Operand:
                            IBinaryOperation (BinaryOperatorKind.Add) (OperationKind.Binary, Type: ?, IsInvalid) (Syntax: 'field + "a"')
                              Left:
                                IInvalidOperation (OperationKind.Invalid, Type: ?, IsInvalid) (Syntax: 'field')
                                  Children(0)
                              Right:
                                ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: "a") (Syntax: '"a"')
                    """,
                expectedDiagnostics: [
                    // (8,20): error CS0103: The name 'field' does not exist in the current context
                    //             return field + "a";
                    Diagnostic(ErrorCode.ERR_NameNotInContext, "field").WithArguments("field").WithLocation(8, 20)],
                parseOptions: TestOptions.Regular13);
        }
    }
}
