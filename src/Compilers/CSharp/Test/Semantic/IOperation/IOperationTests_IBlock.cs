// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    public partial class IOperationTests : SemanticModelTestBase
    {

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void BlockFlow_01()
        {
            var source = @"
#pragma warning disable CS0219

class C
{
    void F()
    /*<bind>*/{
        int i;
        i = 1;
    }/*</bind>*/
}";

            var compilation = CreateStandardCompilation(source, parseOptions: TestOptions.RegularWithFlowAnalysisFeature);

            compilation.VerifyDiagnostics();

            string expectedGraph = @"
Block[0] - Entry
    Statements (0)
    Next (Regular) Block[1]
        Entering: {1}

.locals {1}
{
    Locals: [System.Int32 i]
    Block[1] - Block
        Predecessors: [0]
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'i = 1;')
              Expression: 
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32) (Syntax: 'i = 1')
                  Left: 
                    ILocalReferenceOperation: i (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'i')
                  Right: 
                    ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')

        Next (Regular) Block[2]
            Leaving: {1}
}

Block[2] - Exit
    Predecessors: [1]
    Statements (0)
";
            VerifyFlowGraphForTest<BlockSyntax>(compilation, expectedGraph);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void BlockFlow_02()
        {
            var source = @"
#pragma warning disable CS0168

class C
{
    void F()
    /*<bind>*/{
        int i;
    }/*</bind>*/
}";

            var compilation = CreateStandardCompilation(source, parseOptions: TestOptions.RegularWithFlowAnalysisFeature);

            compilation.VerifyDiagnostics();

            string expectedGraph = @"
Block[0] - Entry
    Statements (0)
    Next (Regular) Block[1]
Block[1] - Exit
    Predecessors: [0]
    Statements (0)
";
            VerifyFlowGraphForTest<BlockSyntax>(compilation, expectedGraph);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void BlockFlow_03()
        {
            var source = @"
#pragma warning disable CS0219

class C
{
    void F()
    /*<bind>*/{
        int i;
        i = 1;
        {
            int j;
            j = 1;
        }
    }/*</bind>*/
}";

            var compilation = CreateStandardCompilation(source, parseOptions: TestOptions.RegularWithFlowAnalysisFeature);

            compilation.VerifyDiagnostics();

            string expectedGraph = @"
Block[0] - Entry
    Statements (0)
    Next (Regular) Block[1]
        Entering: {1}

.locals {1}
{
    Locals: [System.Int32 i]
    Block[1] - Block
        Predecessors: [0]
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'i = 1;')
              Expression: 
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32) (Syntax: 'i = 1')
                  Left: 
                    ILocalReferenceOperation: i (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'i')
                  Right: 
                    ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')

        Next (Regular) Block[2]
            Entering: {2}

    .locals {2}
    {
        Locals: [System.Int32 j]
        Block[2] - Block
            Predecessors: [1]
            Statements (1)
                IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'j = 1;')
                  Expression: 
                    ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32) (Syntax: 'j = 1')
                      Left: 
                        ILocalReferenceOperation: j (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'j')
                      Right: 
                        ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')

            Next (Regular) Block[3]
                Leaving: {2} {1}
    }
}

Block[3] - Exit
    Predecessors: [2]
    Statements (0)
";
            VerifyFlowGraphForTest<BlockSyntax>(compilation, expectedGraph);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void BlockFlow_04()
        {
            var source = @"
#pragma warning disable CS0219

class C
{
    void F()
    /*<bind>*/{
        int i;
        {
            int j;
            i = 1;
            j = 1;
        }
    }/*</bind>*/
}";

            var compilation = CreateStandardCompilation(source, parseOptions: TestOptions.RegularWithFlowAnalysisFeature);

            compilation.VerifyDiagnostics();

            string expectedGraph = @"
Block[0] - Entry
    Statements (0)
    Next (Regular) Block[1]
        Entering: {1}

.locals {1}
{
    Locals: [System.Int32 i] [System.Int32 j]
    Block[1] - Block
        Predecessors: [0]
        Statements (2)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'i = 1;')
              Expression: 
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32) (Syntax: 'i = 1')
                  Left: 
                    ILocalReferenceOperation: i (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'i')
                  Right: 
                    ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')

            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'j = 1;')
              Expression: 
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32) (Syntax: 'j = 1')
                  Left: 
                    ILocalReferenceOperation: j (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'j')
                  Right: 
                    ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')

        Next (Regular) Block[2]
            Leaving: {1}
}

Block[2] - Exit
    Predecessors: [1]
    Statements (0)
";
            VerifyFlowGraphForTest<BlockSyntax>(compilation, expectedGraph);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void BlockFlow_05()
        {
            var source = @"
#pragma warning disable CS0168

class C
{
    void F()
    /*<bind>*/{
        int i;
        {
            int j;
        }
    }/*</bind>*/
}";

            var compilation = CreateStandardCompilation(source, parseOptions: TestOptions.RegularWithFlowAnalysisFeature);

            compilation.VerifyDiagnostics();

            string expectedGraph = @"
Block[0] - Entry
    Statements (0)
    Next (Regular) Block[1]
Block[1] - Exit
    Predecessors: [0]
    Statements (0)
";
            VerifyFlowGraphForTest<BlockSyntax>(compilation, expectedGraph);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void BlockFlow_06()
        {
            var source = @"
#pragma warning disable CS0219

class C
{
    void F()
    /*<bind>*/{
        {
            int i;
            i = 1;
        }
        {
            int j;
            j = 1;
        }
    }/*</bind>*/
}";

            var compilation = CreateStandardCompilation(source, parseOptions: TestOptions.RegularWithFlowAnalysisFeature);

            compilation.VerifyDiagnostics();

            string expectedGraph = @"
Block[0] - Entry
    Statements (0)
    Next (Regular) Block[1]
        Entering: {1}

.locals {1}
{
    Locals: [System.Int32 i]
    Block[1] - Block
        Predecessors: [0]
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'i = 1;')
              Expression: 
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32) (Syntax: 'i = 1')
                  Left: 
                    ILocalReferenceOperation: i (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'i')
                  Right: 
                    ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')

        Next (Regular) Block[2]
            Leaving: {1}
            Entering: {2}
}
.locals {2}
{
    Locals: [System.Int32 j]
    Block[2] - Block
        Predecessors: [1]
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'j = 1;')
              Expression: 
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32) (Syntax: 'j = 1')
                  Left: 
                    ILocalReferenceOperation: j (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'j')
                  Right: 
                    ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')

        Next (Regular) Block[3]
            Leaving: {2}
}

Block[3] - Exit
    Predecessors: [2]
    Statements (0)
";
            VerifyFlowGraphForTest<BlockSyntax>(compilation, expectedGraph);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void BlockFlow_07()
        {
            var source = @"
#pragma warning disable CS0168

class C
{
    void F()
    /*<bind>*/{
        {
            int i;
        }
        {
            int j;
        }
    }/*</bind>*/
}";

            var compilation = CreateStandardCompilation(source, parseOptions: TestOptions.RegularWithFlowAnalysisFeature);

            compilation.VerifyDiagnostics();

            string expectedGraph = @"
Block[0] - Entry
    Statements (0)
    Next (Regular) Block[1]
Block[1] - Exit
    Predecessors: [0]
    Statements (0)
";
            VerifyFlowGraphForTest<BlockSyntax>(compilation, expectedGraph);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void BlockFlow_08()
        {
            var source = @"
#pragma warning disable CS0168

class C
{
    void F()
    /*<bind>*/{
        int i;
        {
            int j;
            {
                int k;
                {
                    int l;
                }
                {
                    int m;
                }
            }
            {
                int n;
            }
        }
        {
            int o;
        }
    }/*</bind>*/
}";

            var compilation = CreateStandardCompilation(source, parseOptions: TestOptions.RegularWithFlowAnalysisFeature);

            compilation.VerifyDiagnostics();

            string expectedGraph = @"
Block[0] - Entry
    Statements (0)
    Next (Regular) Block[1]
Block[1] - Exit
    Predecessors: [0]
    Statements (0)
";
            VerifyFlowGraphForTest<BlockSyntax>(compilation, expectedGraph);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void BlockFlow_09()
        {
            var source = @"
#pragma warning disable CS0219

class C
{
    void F()
    /*<bind>*/{
        int i;
        {
            int j;
            {
                int k;
                {
                    int l;
                    {
                        i = 1;
                        j = 1;
                        k = 1;
                        l = 1;
                    }
                }
            }
        }
    }/*</bind>*/
}";

            var compilation = CreateStandardCompilation(source, parseOptions: TestOptions.RegularWithFlowAnalysisFeature);

            compilation.VerifyDiagnostics();

            string expectedGraph = @"
Block[0] - Entry
    Statements (0)
    Next (Regular) Block[1]
        Entering: {1}

.locals {1}
{
    Locals: [System.Int32 i] [System.Int32 j] [System.Int32 k] [System.Int32 l]
    Block[1] - Block
        Predecessors: [0]
        Statements (4)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'i = 1;')
              Expression: 
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32) (Syntax: 'i = 1')
                  Left: 
                    ILocalReferenceOperation: i (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'i')
                  Right: 
                    ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')

            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'j = 1;')
              Expression: 
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32) (Syntax: 'j = 1')
                  Left: 
                    ILocalReferenceOperation: j (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'j')
                  Right: 
                    ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')

            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'k = 1;')
              Expression: 
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32) (Syntax: 'k = 1')
                  Left: 
                    ILocalReferenceOperation: k (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'k')
                  Right: 
                    ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')

            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'l = 1;')
              Expression: 
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32) (Syntax: 'l = 1')
                  Left: 
                    ILocalReferenceOperation: l (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'l')
                  Right: 
                    ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')

        Next (Regular) Block[2]
            Leaving: {1}
}

Block[2] - Exit
    Predecessors: [1]
    Statements (0)
";
            VerifyFlowGraphForTest<BlockSyntax>(compilation, expectedGraph);
        }

        // PROTOTYPE(dataflow): Add flow graph tests to VB.
    }
}
