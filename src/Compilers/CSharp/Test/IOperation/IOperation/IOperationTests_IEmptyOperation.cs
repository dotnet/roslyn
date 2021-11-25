// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    public class IOperationTests_IEmptyOperation : SemanticModelTestBase
    {
        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void TestEmptyStatementInWhile()
        {
            string source = @"
using System;

class C
{
    void M(string s)
    {
        while (true)
        /*<bind>*/{
            ;
        }/*</bind>*/
    }
}
";
            var expectedDiagnostics = DiagnosticDescription.None;

            string expectedOperationTree = @"
IBlockOperation (1 statements) (OperationKind.Block, Type: null) (Syntax: '{ ... }')
  IEmptyOperation (OperationKind.Empty, Type: null) (Syntax: ';')
";
            VerifyOperationTreeAndDiagnosticsForTest<BlockSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void EmptyFlow_01()
        {
            string source = @"
using System;

class C
{
    void M(string s)
    /*<bind>*/{
        while (true)
        {
            ;
        }
    }/*</bind>*/
}
";
            var expectedDiagnostics = DiagnosticDescription.None;

            string expectedFlowGraph = @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
Block[B1] - Block
    Predecessors: [B0] [B1]
    Statements (0)
    Jump if False (Regular) to Block[B2]
        ILiteralOperation (OperationKind.Literal, Type: System.Boolean, Constant: True) (Syntax: 'true')

    Next (Regular) Block[B1]
Block[B2] - Exit [UnReachable]
    Predecessors: [B1]
    Statements (0)
";
            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(source, expectedFlowGraph, expectedDiagnostics);
        }
    }
}
