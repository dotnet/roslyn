// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    public class IOperationTests_IUtf8StringOperation : SemanticModelTestBase
    {

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void Utf8String_01()
        {
            string source = @"
class Program
{
    static System.ReadOnlySpan<byte> Test()
    {
        /*<bind>*/return ""Abc""u8;/*</bind>*/
    }
}
";
            string expectedOperationTree = @"
IReturnOperation (OperationKind.Return, Type: null) (Syntax: 'return ""Abc""u8;')
  ReturnedValue:
    IUtf8StringOperation (Abc) (OperationKind.Utf8String, Type: System.ReadOnlySpan<System.Byte>) (Syntax: '""Abc""u8')
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<ReturnStatementSyntax>(source, expectedOperationTree, expectedDiagnostics, targetFramework: Roslyn.Test.Utilities.TargetFramework.NetCoreApp);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void Utf8StringFlow_01()
        {
            string source = @"
class C
{
    void M(System.ReadOnlySpan<byte> b)
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
            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.ReadOnlySpan<System.Byte>) (Syntax: 'b = ""ABC""u8')
              Left: 
                IParameterReferenceOperation: b (OperationKind.ParameterReference, Type: System.ReadOnlySpan<System.Byte>) (Syntax: 'b')
              Right: 
                IUtf8StringOperation (ABC) (OperationKind.Utf8String, Type: System.ReadOnlySpan<System.Byte>) (Syntax: '""ABC""u8')

    Next (Regular) Block[B2]
Block[B2] - Exit
    Predecessors: [B1]
    Statements (0)
";
            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(source, expectedFlowGraph, expectedDiagnostics, targetFramework: Roslyn.Test.Utilities.TargetFramework.NetCoreApp);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void Utf8String_02()
        {
            string source = @"
class Program
{
    static System.ReadOnlySpan<byte> Test()
    {
        /*<bind>*/return ""Ab""u8 + ""c""u8;/*</bind>*/
    }
}
";
            string expectedOperationTree = @"
IReturnOperation (OperationKind.Return, Type: null) (Syntax: 'return ""Ab""u8 + ""c""u8;')
  ReturnedValue:
    IBinaryOperation (BinaryOperatorKind.Add) (OperationKind.Binary, Type: System.ReadOnlySpan<System.Byte>) (Syntax: '""Ab""u8 + ""c""u8')
      Left:
        IUtf8StringOperation (Ab) (OperationKind.Utf8String, Type: System.ReadOnlySpan<System.Byte>) (Syntax: '""Ab""u8')
      Right:
        IUtf8StringOperation (c) (OperationKind.Utf8String, Type: System.ReadOnlySpan<System.Byte>) (Syntax: '""c""u8')
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<ReturnStatementSyntax>(source, expectedOperationTree, expectedDiagnostics, targetFramework: Roslyn.Test.Utilities.TargetFramework.NetCoreApp);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void Utf8StringFlow_02()
        {
            string source = @"
class C
{
    void M(System.ReadOnlySpan<byte> b)
    /*<bind>*/{
        b = ""AB""u8 + ""C""u8;
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
        IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'b = ""AB""u8 + ""C""u8;')
          Expression:
            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.ReadOnlySpan<System.Byte>) (Syntax: 'b = ""AB""u8 + ""C""u8')
              Left:
                IParameterReferenceOperation: b (OperationKind.ParameterReference, Type: System.ReadOnlySpan<System.Byte>) (Syntax: 'b')
              Right:
                IBinaryOperation (BinaryOperatorKind.Add) (OperationKind.Binary, Type: System.ReadOnlySpan<System.Byte>) (Syntax: '""AB""u8 + ""C""u8')
                  Left:
                    IUtf8StringOperation (AB) (OperationKind.Utf8String, Type: System.ReadOnlySpan<System.Byte>) (Syntax: '""AB""u8')
                  Right:
                    IUtf8StringOperation (C) (OperationKind.Utf8String, Type: System.ReadOnlySpan<System.Byte>) (Syntax: '""C""u8')
    Next (Regular) Block[B2]
Block[B2] - Exit
    Predecessors: [B1]
    Statements (0)
";
            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(source, expectedFlowGraph, expectedDiagnostics, targetFramework: Roslyn.Test.Utilities.TargetFramework.NetCoreApp);
        }
    }
}
