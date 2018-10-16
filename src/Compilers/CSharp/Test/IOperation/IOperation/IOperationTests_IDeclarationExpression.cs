// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    public partial class IOperationTests : SemanticModelTestBase
    {
        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void DeclarationFlow_01()
        {
            string source = @"
class C
{
    void M(bool b)
    /*<bind>*/{
        M2(b ? out var i1 : out var i2);
    }/*</bind>*/

    public void M2(out int i)
    {
        i = 0;
    }
}

";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS1525: Invalid expression term 'out'
                //         M2(b ? out var i1 : out var i2);
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, "out").WithArguments("out").WithLocation(6, 16),
                // CS1003: Syntax error, ':' expected
                //         M2(b ? out var i1 : out var i2);
                Diagnostic(ErrorCode.ERR_SyntaxError, "out").WithArguments(":", "out").WithLocation(6, 16),
                // CS1525: Invalid expression term 'out'
                //         M2(b ? out var i1 : out var i2);
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, "out").WithArguments("out").WithLocation(6, 16),
                // CS1003: Syntax error, ',' expected
                //         M2(b ? out var i1 : out var i2);
                Diagnostic(ErrorCode.ERR_SyntaxError, "out").WithArguments(",", "out").WithLocation(6, 16),
                // CS1003: Syntax error, ',' expected
                //         M2(b ? out var i1 : out var i2);
                Diagnostic(ErrorCode.ERR_SyntaxError, ":").WithArguments(",", ":").WithLocation(6, 27),
                // CS1003: Syntax error, ',' expected
                //         M2(b ? out var i1 : out var i2);
                Diagnostic(ErrorCode.ERR_SyntaxError, "out").WithArguments(",", "out").WithLocation(6, 29)
            };

            string expectedFlowGraph = @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
        Entering: {R1}

.locals {R1}
{
    Locals: [var i1] [var i2]
    CaptureIds: [0]
    Block[B1] - Block
        Predecessors: [B0]
        Statements (0)
        Jump if False (Regular) to Block[B3]
            IParameterReferenceOperation: b (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'b')

        Next (Regular) Block[B2]
    Block[B2] - Block
        Predecessors: [B1]
        Statements (1)
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsInvalid, IsImplicit) (Syntax: '')
              Value: 
                IInvalidOperation (OperationKind.Invalid, Type: null, IsInvalid) (Syntax: '')
                  Children(0)

        Next (Regular) Block[B4]
    Block[B3] - Block
        Predecessors: [B1]
        Statements (1)
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsInvalid, IsImplicit) (Syntax: '')
              Value: 
                IInvalidOperation (OperationKind.Invalid, Type: null, IsInvalid) (Syntax: '')
                  Children(0)

        Next (Regular) Block[B4]
    Block[B4] - Block
        Predecessors: [B2] [B3]
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null, IsInvalid) (Syntax: 'M2(b ? out  ... ut var i2);')
              Expression: 
                IInvalidOperation (OperationKind.Invalid, Type: System.Void, IsInvalid) (Syntax: 'M2(b ? out  ... out var i2)')
                  Children(3):
                      IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: ?, IsInvalid, IsImplicit) (Syntax: 'b ? ')
                      IDeclarationExpressionOperation (OperationKind.DeclarationExpression, Type: var) (Syntax: 'var i1')
                        ILocalReferenceOperation: i1 (IsDeclaration: True) (OperationKind.LocalReference, Type: var) (Syntax: 'i1')
                      IDeclarationExpressionOperation (OperationKind.DeclarationExpression, Type: var) (Syntax: 'var i2')
                        ILocalReferenceOperation: i2 (IsDeclaration: True) (OperationKind.LocalReference, Type: var) (Syntax: 'i2')

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
