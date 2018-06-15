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

            var compilation = CreateCompilation(source, parseOptions: TestOptions.RegularWithFlowAnalysisFeature);

            compilation.VerifyDiagnostics();

            string expectedGraph = @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
        Entering: {R1}

.locals {R1}
{
    Locals: [System.Int32 i]
    Block[B1] - Block
        Predecessors: [B0]
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'i = 1;')
              Expression: 
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32) (Syntax: 'i = 1')
                  Left: 
                    ILocalReferenceOperation: i (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'i')
                  Right: 
                    ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')

        Next (Regular) Block[B2]
            Leaving: {R1}
}

Block[B2] - Exit
    Predecessors: [B1]
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

            var compilation = CreateCompilation(source, parseOptions: TestOptions.RegularWithFlowAnalysisFeature);

            compilation.VerifyDiagnostics();

            string expectedGraph = @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
Block[B1] - Exit
    Predecessors: [B0]
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

            var compilation = CreateCompilation(source, parseOptions: TestOptions.RegularWithFlowAnalysisFeature);

            compilation.VerifyDiagnostics();

            string expectedGraph = @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
        Entering: {R1}

.locals {R1}
{
    Locals: [System.Int32 i]
    Block[B1] - Block
        Predecessors: [B0]
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'i = 1;')
              Expression: 
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32) (Syntax: 'i = 1')
                  Left: 
                    ILocalReferenceOperation: i (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'i')
                  Right: 
                    ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')

        Next (Regular) Block[B2]
            Entering: {R2}

    .locals {R2}
    {
        Locals: [System.Int32 j]
        Block[B2] - Block
            Predecessors: [B1]
            Statements (1)
                IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'j = 1;')
                  Expression: 
                    ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32) (Syntax: 'j = 1')
                      Left: 
                        ILocalReferenceOperation: j (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'j')
                      Right: 
                        ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')

            Next (Regular) Block[B3]
                Leaving: {R2} {R1}
    }
}

Block[B3] - Exit
    Predecessors: [B2]
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

            var compilation = CreateCompilation(source, parseOptions: TestOptions.RegularWithFlowAnalysisFeature);

            compilation.VerifyDiagnostics();

            string expectedGraph = @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
        Entering: {R1}

.locals {R1}
{
    Locals: [System.Int32 i] [System.Int32 j]
    Block[B1] - Block
        Predecessors: [B0]
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

        Next (Regular) Block[B2]
            Leaving: {R1}
}

Block[B2] - Exit
    Predecessors: [B1]
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

            var compilation = CreateCompilation(source, parseOptions: TestOptions.RegularWithFlowAnalysisFeature);

            compilation.VerifyDiagnostics();

            string expectedGraph = @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
Block[B1] - Exit
    Predecessors: [B0]
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

            var compilation = CreateCompilation(source, parseOptions: TestOptions.RegularWithFlowAnalysisFeature);

            compilation.VerifyDiagnostics();

            string expectedGraph = @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
        Entering: {R1}

.locals {R1}
{
    Locals: [System.Int32 i]
    Block[B1] - Block
        Predecessors: [B0]
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'i = 1;')
              Expression: 
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32) (Syntax: 'i = 1')
                  Left: 
                    ILocalReferenceOperation: i (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'i')
                  Right: 
                    ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')

        Next (Regular) Block[B2]
            Leaving: {R1}
            Entering: {R2}
}
.locals {R2}
{
    Locals: [System.Int32 j]
    Block[B2] - Block
        Predecessors: [B1]
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'j = 1;')
              Expression: 
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32) (Syntax: 'j = 1')
                  Left: 
                    ILocalReferenceOperation: j (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'j')
                  Right: 
                    ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')

        Next (Regular) Block[B3]
            Leaving: {R2}
}

Block[B3] - Exit
    Predecessors: [B2]
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

            var compilation = CreateCompilation(source, parseOptions: TestOptions.RegularWithFlowAnalysisFeature);

            compilation.VerifyDiagnostics();

            string expectedGraph = @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
Block[B1] - Exit
    Predecessors: [B0]
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

            var compilation = CreateCompilation(source, parseOptions: TestOptions.RegularWithFlowAnalysisFeature);

            compilation.VerifyDiagnostics();

            string expectedGraph = @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
Block[B1] - Exit
    Predecessors: [B0]
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

            var compilation = CreateCompilation(source, parseOptions: TestOptions.RegularWithFlowAnalysisFeature);

            compilation.VerifyDiagnostics();

            string expectedGraph = @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
        Entering: {R1}

.locals {R1}
{
    Locals: [System.Int32 i] [System.Int32 j] [System.Int32 k] [System.Int32 l]
    Block[B1] - Block
        Predecessors: [B0]
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

        Next (Regular) Block[B2]
            Leaving: {R1}
}

Block[B2] - Exit
    Predecessors: [B1]
    Statements (0)
";
            VerifyFlowGraphForTest<BlockSyntax>(compilation, expectedGraph);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void BlockFlow_10()
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

        i = 2;
    }/*</bind>*/
}";

            var compilation = CreateCompilation(source, parseOptions: TestOptions.RegularWithFlowAnalysisFeature);

            compilation.VerifyDiagnostics();

            string expectedGraph = @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
        Entering: {R1} {R2}

.locals {R1}
{
    Locals: [System.Int32 i]
    .locals {R2}
    {
        Locals: [System.Int32 j]
        Block[B1] - Block
            Predecessors: [B0]
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

            Next (Regular) Block[B2]
                Leaving: {R2}
    }

    Block[B2] - Block
        Predecessors: [B1]
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'i = 2;')
              Expression: 
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32) (Syntax: 'i = 2')
                  Left: 
                    ILocalReferenceOperation: i (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'i')
                  Right: 
                    ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 2) (Syntax: '2')

        Next (Regular) Block[B3]
            Leaving: {R1}
}

Block[B3] - Exit
    Predecessors: [B2]
    Statements (0)
";
            VerifyFlowGraphForTest<BlockSyntax>(compilation, expectedGraph);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void BlockFlow_11()
        {
            var source = @"
#pragma warning disable CS0219

class C
{
    void F(int j)
    /*<bind>*/{
        j = 2; 
        int i;
        i = 1;
    }/*</bind>*/
}";

            var compilation = CreateCompilation(source, parseOptions: TestOptions.RegularWithFlowAnalysisFeature);

            compilation.VerifyDiagnostics();

            string expectedGraph = @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
        Entering: {R1}

.locals {R1}
{
    Locals: [System.Int32 i]
    Block[B1] - Block
        Predecessors: [B0]
        Statements (2)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'j = 2;')
              Expression: 
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32) (Syntax: 'j = 2')
                  Left: 
                    IParameterReferenceOperation: j (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'j')
                  Right: 
                    ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 2) (Syntax: '2')

            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'i = 1;')
              Expression: 
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32) (Syntax: 'i = 1')
                  Left: 
                    ILocalReferenceOperation: i (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'i')
                  Right: 
                    ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')

        Next (Regular) Block[B2]
            Leaving: {R1}
}

Block[B2] - Exit
    Predecessors: [B1]
    Statements (0)
";
            VerifyFlowGraphForTest<BlockSyntax>(compilation, expectedGraph);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void BlockFlow_12()
        {
            var source = @"
#pragma warning disable CS0168
#pragma warning disable CS0219
class C
{
    void F()
    /*<bind>*/{
        int a;
        {
            a = 1;
            {
                int j = 2;
            }
            int k = 3;
            {
                int l = 4;
            }
            int m = 5;
        }
    }/*</bind>*/
}";

            var compilation = CreateCompilation(source, parseOptions: TestOptions.RegularWithFlowAnalysisFeature);

            compilation.VerifyDiagnostics();

            string expectedGraph = @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
        Entering: {R1}

.locals {R1}
{
    Locals: [System.Int32 a] [System.Int32 k] [System.Int32 m]
    Block[B1] - Block
        Predecessors: [B0]
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'a = 1;')
              Expression: 
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32) (Syntax: 'a = 1')
                  Left: 
                    ILocalReferenceOperation: a (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'a')
                  Right: 
                    ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')

        Next (Regular) Block[B2]
            Entering: {R2}

    .locals {R2}
    {
        Locals: [System.Int32 j]
        Block[B2] - Block
            Predecessors: [B1]
            Statements (1)
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32, IsImplicit) (Syntax: 'j = 2')
                  Left: 
                    ILocalReferenceOperation: j (IsDeclaration: True) (OperationKind.LocalReference, Type: System.Int32, IsImplicit) (Syntax: 'j = 2')
                  Right: 
                    ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 2) (Syntax: '2')

            Next (Regular) Block[B3]
                Leaving: {R2}
    }

    Block[B3] - Block
        Predecessors: [B2]
        Statements (1)
            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32, IsImplicit) (Syntax: 'k = 3')
              Left: 
                ILocalReferenceOperation: k (IsDeclaration: True) (OperationKind.LocalReference, Type: System.Int32, IsImplicit) (Syntax: 'k = 3')
              Right: 
                ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 3) (Syntax: '3')

        Next (Regular) Block[B4]
            Entering: {R3}

    .locals {R3}
    {
        Locals: [System.Int32 l]
        Block[B4] - Block
            Predecessors: [B3]
            Statements (1)
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32, IsImplicit) (Syntax: 'l = 4')
                  Left: 
                    ILocalReferenceOperation: l (IsDeclaration: True) (OperationKind.LocalReference, Type: System.Int32, IsImplicit) (Syntax: 'l = 4')
                  Right: 
                    ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 4) (Syntax: '4')

            Next (Regular) Block[B5]
                Leaving: {R3}
    }

    Block[B5] - Block
        Predecessors: [B4]
        Statements (1)
            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32, IsImplicit) (Syntax: 'm = 5')
              Left: 
                ILocalReferenceOperation: m (IsDeclaration: True) (OperationKind.LocalReference, Type: System.Int32, IsImplicit) (Syntax: 'm = 5')
              Right: 
                ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 5) (Syntax: '5')

        Next (Regular) Block[B6]
            Leaving: {R1}
}

Block[B6] - Exit
    Predecessors: [B5]
    Statements (0)
";
            VerifyFlowGraphForTest<BlockSyntax>(compilation, expectedGraph);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void BlockFlow_13()
        {
            var source = @"
#pragma warning disable CS0168
#pragma warning disable CS0219
class C
{
    void F()
    /*<bind>*/{
        int a;
        {
            {
                int i = 1;
            }
            a = 2;
            {
                int k = 3;
            }
            int l = 4;
            {
                int m = 5;
            }
        }
    }/*</bind>*/
}";

            var compilation = CreateCompilation(source, parseOptions: TestOptions.RegularWithFlowAnalysisFeature);

            compilation.VerifyDiagnostics();

            string expectedGraph = @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
        Entering: {R1} {R2}

.locals {R1}
{
    Locals: [System.Int32 a] [System.Int32 l]
    .locals {R2}
    {
        Locals: [System.Int32 i]
        Block[B1] - Block
            Predecessors: [B0]
            Statements (1)
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32, IsImplicit) (Syntax: 'i = 1')
                  Left: 
                    ILocalReferenceOperation: i (IsDeclaration: True) (OperationKind.LocalReference, Type: System.Int32, IsImplicit) (Syntax: 'i = 1')
                  Right: 
                    ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')

            Next (Regular) Block[B2]
                Leaving: {R2}
    }

    Block[B2] - Block
        Predecessors: [B1]
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'a = 2;')
              Expression: 
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32) (Syntax: 'a = 2')
                  Left: 
                    ILocalReferenceOperation: a (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'a')
                  Right: 
                    ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 2) (Syntax: '2')

        Next (Regular) Block[B3]
            Entering: {R3}

    .locals {R3}
    {
        Locals: [System.Int32 k]
        Block[B3] - Block
            Predecessors: [B2]
            Statements (1)
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32, IsImplicit) (Syntax: 'k = 3')
                  Left: 
                    ILocalReferenceOperation: k (IsDeclaration: True) (OperationKind.LocalReference, Type: System.Int32, IsImplicit) (Syntax: 'k = 3')
                  Right: 
                    ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 3) (Syntax: '3')

            Next (Regular) Block[B4]
                Leaving: {R3}
    }

    Block[B4] - Block
        Predecessors: [B3]
        Statements (1)
            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32, IsImplicit) (Syntax: 'l = 4')
              Left: 
                ILocalReferenceOperation: l (IsDeclaration: True) (OperationKind.LocalReference, Type: System.Int32, IsImplicit) (Syntax: 'l = 4')
              Right: 
                ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 4) (Syntax: '4')

        Next (Regular) Block[B5]
            Entering: {R4}

    .locals {R4}
    {
        Locals: [System.Int32 m]
        Block[B5] - Block
            Predecessors: [B4]
            Statements (1)
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32, IsImplicit) (Syntax: 'm = 5')
                  Left: 
                    ILocalReferenceOperation: m (IsDeclaration: True) (OperationKind.LocalReference, Type: System.Int32, IsImplicit) (Syntax: 'm = 5')
                  Right: 
                    ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 5) (Syntax: '5')

            Next (Regular) Block[B6]
                Leaving: {R4} {R1}
    }
}

Block[B6] - Exit
    Predecessors: [B5]
    Statements (0)
";
            VerifyFlowGraphForTest<BlockSyntax>(compilation, expectedGraph);
        }
    }
}
