// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.


using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    public partial class IOperationTests : SemanticModelTestBase
    {
        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void ILocalReferenceExpression_OutVar()
        {
            string source = @"
using System;

public class C1
{
    public virtual void M1()
    {
        M2(out /*<bind>*/var i/*</bind>*/);
    }

    public void M2(out int i )
    {
        i = 0;
    }
}
";
            string expectedOperationTree = @"
IDeclarationExpressionOperation (OperationKind.DeclarationExpression, Type: System.Int32) (Syntax: 'var i')
  ILocalReferenceOperation: i (IsDeclaration: True) (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'i')
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<DeclarationExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void ILocalReferenceExpression_DeconstructionDeclaration()
        {
            string source = @"
using System;

public class C1
{
    public virtual void M1()
    {
        /*<bind>*/(var i1, var i2)/*</bind>*/ = (1, 2);
    }
}
";
            string expectedOperationTree = @"
ITupleOperation (OperationKind.Tuple, Type: (System.Int32 i1, System.Int32 i2)) (Syntax: '(var i1, var i2)')
  NaturalType: (System.Int32 i1, System.Int32 i2)
  Elements(2):
      IDeclarationExpressionOperation (OperationKind.DeclarationExpression, Type: System.Int32) (Syntax: 'var i1')
        ILocalReferenceOperation: i1 (IsDeclaration: True) (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'i1')
      IDeclarationExpressionOperation (OperationKind.DeclarationExpression, Type: System.Int32) (Syntax: 'var i2')
        ILocalReferenceOperation: i2 (IsDeclaration: True) (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'i2')
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<TupleExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void ILocalReferenceExpression_DeconstructionDeclaration_AlternateSyntax()
        {
            string source = @"
using System;

public class C1
{
    public virtual void M1()
    {
        /*<bind>*/var (i1, i2)/*</bind>*/ = (1, 2);
    }
}
";
            string expectedOperationTree = @"
IDeclarationExpressionOperation (OperationKind.DeclarationExpression, Type: (System.Int32 i1, System.Int32 i2)) (Syntax: 'var (i1, i2)')
  ITupleOperation (OperationKind.Tuple, Type: (System.Int32 i1, System.Int32 i2)) (Syntax: '(i1, i2)')
    NaturalType: (System.Int32 i1, System.Int32 i2)
    Elements(2):
        ILocalReferenceOperation: i1 (IsDeclaration: True) (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'i1')
        ILocalReferenceOperation: i2 (IsDeclaration: True) (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'i2')
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<DeclarationExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void LocalReference_ParameterReference_Flow()
        {
            string source = @"
public class C
{
    public void M(int p)
    /*<bind>*/{
        var l = p;
        p = l;
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
    Locals: [System.Int32 l]
    Block[B1] - Block
        Predecessors: [B0]
        Statements (2)
            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32, IsImplicit) (Syntax: 'l = p')
              Left: 
                ILocalReferenceOperation: l (IsDeclaration: True) (OperationKind.LocalReference, Type: System.Int32, IsImplicit) (Syntax: 'l = p')
              Right: 
                IParameterReferenceOperation: p (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'p')

            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'p = l;')
              Expression: 
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32) (Syntax: 'p = l')
                  Left: 
                    IParameterReferenceOperation: p (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'p')
                  Right: 
                    ILocalReferenceOperation: l (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'l')

        Next (Regular) Block[B2]
            Leaving: {R1}
}

Block[B2] - Exit
    Predecessors: [B1]
    Statements (0)
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(source, expectedFlowGraph, expectedDiagnostics);
        }
    }
}
