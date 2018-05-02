// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;


namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    public partial class IOperationTests : SemanticModelTestBase
    {
        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void NameOfFlow_01()
        {
            string source = @"
class C
{
    void M(bool b, int i1, int i2)
    /*<bind>*/{
        string test = nameof(b ? i1 : i2);
    }/*</bind>*/
}
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS8081: Expression does not have a name.
                //         string test = nameof(b ? i1 : i2);
                Diagnostic(ErrorCode.ERR_ExpressionHasNoName, "b ? i1 : i2").WithLocation(6, 30),
                // CS0219: The variable 'test' is assigned but its value is never used
                //         string test = nameof(b ? i1 : i2);
                Diagnostic(ErrorCode.WRN_UnreferencedVarAssg, "test").WithArguments("test").WithLocation(6, 16)
            };

            string expectedFlowGraph = @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
        Entering: {R1}

.locals {R1}
{
    Locals: [System.String test]
    Block[B1] - Block
        Predecessors: [B0]
        Statements (1)
            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.String, IsInvalid, IsImplicit) (Syntax: 'test = name ...  ? i1 : i2)')
              Left: 
                ILocalReferenceOperation: test (IsDeclaration: True) (OperationKind.LocalReference, Type: System.String, IsInvalid, IsImplicit) (Syntax: 'test = name ...  ? i1 : i2)')
              Right: 
                ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: """", IsInvalid) (Syntax: 'nameof(b ? i1 : i2)')

        Next (Regular) Block[B2]
            Leaving: {R1}
}

Block[B2] - Exit
    Predecessors: [B1]
    Statements (0)";
            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(source, expectedFlowGraph, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void NameOfFlow_02()
        {
            string source = @"
class C
{
    void M(int i1)
    /*<bind>*/{
        string test = nameof(i1);
    }/*</bind>*/
}
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS0219: The variable 'test' is assigned but its value is never used
                //         string test = nameof(i1);
                Diagnostic(ErrorCode.WRN_UnreferencedVarAssg, "test").WithArguments("test").WithLocation(6, 16)
            };

            string expectedFlowGraph = @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
        Entering: {R1}

.locals {R1}
{
    Locals: [System.String test]
    Block[B1] - Block
        Predecessors: [B0]
        Statements (1)
            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.String, IsImplicit) (Syntax: 'test = nameof(i1)')
              Left: 
                ILocalReferenceOperation: test (IsDeclaration: True) (OperationKind.LocalReference, Type: System.String, IsImplicit) (Syntax: 'test = nameof(i1)')
              Right: 
                ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: ""i1"") (Syntax: 'nameof(i1)')

        Next (Regular) Block[B2]
            Leaving: {R1}
}

Block[B2] - Exit
    Predecessors: [B1]
    Statements (0)
";
            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(source, expectedFlowGraph, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void NameOfFlow_03()
        {
            string source = @"
class C
{
    void M(bool b, int i1, int i2)
    /*<bind>*/{
        string test = b ? nameof(i1) : nameof(i2);
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
    Locals: [System.String test]
    Block[B1] - Block
        Predecessors: [B0]
        Statements (0)
        Jump if False (Regular) to Block[B3]
            IParameterReferenceOperation: b (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'b')

        Next (Regular) Block[B2]
    Block[B2] - Block
        Predecessors: [B1]
        Statements (1)
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'nameof(i1)')
              Value: 
                ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: ""i1"") (Syntax: 'nameof(i1)')

        Next (Regular) Block[B4]
    Block[B3] - Block
        Predecessors: [B1]
        Statements (1)
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'nameof(i2)')
              Value: 
                ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: ""i2"") (Syntax: 'nameof(i2)')

        Next (Regular) Block[B4]
    Block[B4] - Block
        Predecessors: [B2] [B3]
        Statements (1)
            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.String, IsImplicit) (Syntax: 'test = b ?  ...  nameof(i2)')
              Left: 
                ILocalReferenceOperation: test (IsDeclaration: True) (OperationKind.LocalReference, Type: System.String, IsImplicit) (Syntax: 'test = b ?  ...  nameof(i2)')
              Right: 
                IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.String, IsImplicit) (Syntax: 'b ? nameof( ...  nameof(i2)')

        Next (Regular) Block[B5]
            Leaving: {R1}
}

Block[B5] - Exit
    Predecessors: [B4]
    Statements (0)
";
            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(source, expectedFlowGraph, expectedDiagnostics);
        }
    }
}
