// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    public class IOperationTests_IUTF8StringOperation : SemanticModelTestBase
    {

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void UTF8String_01()
        {
            string source = @"
class Program
{
    static byte[] Test()
    {
        /*<bind>*/return ""Abc""u8;/*</bind>*/
    }
}
";
            string expectedOperationTree = @"
IReturnOperation (OperationKind.Return, Type: null) (Syntax: 'return ""Abc""u8;')
  ReturnedValue:
    IUTF8StringOperation (Abc) (OperationKind.UTF8String, Type: System.Byte[]) (Syntax: '""Abc""u8')
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<ReturnStatementSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void UTF8StringFlow_01()
        {
            string source = @"
class C
{
    void M(byte[] b)
    /*<bind>*/{
        b = ""ABC""u8;
    }/*</bind>*/
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
        IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'b = ""ABC""u8;')
          Expression: 
            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Byte[]) (Syntax: 'b = ""ABC""u8')
              Left: 
                IParameterReferenceOperation: b (OperationKind.ParameterReference, Type: System.Byte[]) (Syntax: 'b')
              Right: 
                IUTF8StringOperation (ABC) (OperationKind.UTF8String, Type: System.Byte[]) (Syntax: '""ABC""u8')

    Next (Regular) Block[B2]
Block[B2] - Exit
    Predecessors: [B1]
    Statements (0)
";
            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(source, expectedFlowGraph, expectedDiagnostics);
        }
    }
}
