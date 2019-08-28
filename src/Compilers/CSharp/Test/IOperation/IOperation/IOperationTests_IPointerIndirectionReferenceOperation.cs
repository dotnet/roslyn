// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.CSharp.UnitTests;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.Semantic.UnitTests.IOperation
{
    public partial class IOperationTests : SemanticModelTestBase
    {
        //Currently, we are not creating the IPointerIndirectionReferenceOperation node
        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void PointerIndirectionFlow_01()
        {
            string source = @"
class C
{
    unsafe static void M(S s, S* sp)
    /*<bind>*/
    {
        s = *sp;
    }/*</bind>*/

     struct S { }
}
";
            var expectedDiagnostics = DiagnosticDescription.None;

            string expectedFlowGraph = @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
Block[B1] - Block
    Predecessors: [B0]
    Statements (1)
        IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 's = *sp;')
          Expression: 
            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: C.S) (Syntax: 's = *sp')
              Left: 
                IParameterReferenceOperation: s (OperationKind.ParameterReference, Type: C.S) (Syntax: 's')
              Right: 
                IOperation:  (OperationKind.None, Type: null) (Syntax: '*sp')
                  Children(1):
                      IParameterReferenceOperation: sp (OperationKind.ParameterReference, Type: C.S*) (Syntax: 'sp')

    Next (Regular) Block[B2]
Block[B2] - Exit
    Predecessors: [B1]
    Statements (0)
";
            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(source, expectedFlowGraph, expectedDiagnostics, compilationOptions: TestOptions.UnsafeDebugDll);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void PointerIndirectionFlow_02()
        {
            string source = @"
class C
{
    unsafe static void M(S* sp, int i)
    /*<bind>*/
    {
        sp->x = 1;
        i = sp->x;
    }/*</bind>*/

     struct S { public int x; }
}
";
            var expectedDiagnostics = DiagnosticDescription.None;

            string expectedFlowGraph = @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
Block[B1] - Block
    Predecessors: [B0]
    Statements (2)
        IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'sp->x = 1;')
          Expression: 
            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32) (Syntax: 'sp->x = 1')
              Left: 
                IFieldReferenceOperation: System.Int32 C.S.x (OperationKind.FieldReference, Type: System.Int32) (Syntax: 'sp->x')
                  Instance Receiver: 
                    IOperation:  (OperationKind.None, Type: null, IsImplicit) (Syntax: 'sp')
                      Children(1):
                          IParameterReferenceOperation: sp (OperationKind.ParameterReference, Type: C.S*) (Syntax: 'sp')
              Right: 
                ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')

        IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'i = sp->x;')
          Expression: 
            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32) (Syntax: 'i = sp->x')
              Left: 
                IParameterReferenceOperation: i (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'i')
              Right: 
                IFieldReferenceOperation: System.Int32 C.S.x (OperationKind.FieldReference, Type: System.Int32) (Syntax: 'sp->x')
                  Instance Receiver: 
                    IOperation:  (OperationKind.None, Type: null, IsImplicit) (Syntax: 'sp')
                      Children(1):
                          IParameterReferenceOperation: sp (OperationKind.ParameterReference, Type: C.S*) (Syntax: 'sp')

    Next (Regular) Block[B2]
Block[B2] - Exit
    Predecessors: [B1]
    Statements (0)
";
            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(source, expectedFlowGraph, expectedDiagnostics, compilationOptions: TestOptions.UnsafeDebugDll);
        }
    }
}