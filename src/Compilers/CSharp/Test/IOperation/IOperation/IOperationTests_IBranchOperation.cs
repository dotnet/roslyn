// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    public partial class IOperationTests : SemanticModelTestBase
    {

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void BranchFlow_01()
        {
            var source = @"
class C
{
    void F()
    /*<bind>*/{
label1: ;
    }/*</bind>*/
}";

            var compilation = CreateCompilation(source, parseOptions: TestOptions.RegularWithFlowAnalysisFeature);

            compilation.VerifyDiagnostics(
                // (6,1): warning CS0164: This label has not been referenced
                // label1: ;
                Diagnostic(ErrorCode.WRN_UnreferencedLabel, "label1").WithLocation(6, 1)
                );

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
        public void BranchFlow_02()
        {
            var source = @"
class C
{
    void F(bool a, bool b, bool c)
    /*<bind>*/{
label1: if (a) goto label2;
        c = true;
label2: if (b) goto label1;
        if (c) goto label1;
    }/*</bind>*/
}";

            var compilation = CreateCompilation(source, parseOptions: TestOptions.RegularWithFlowAnalysisFeature);

            compilation.VerifyDiagnostics();

            string expectedGraph = @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
Block[B1] - Block
    Predecessors: [B0] [B3] [B4]
    Statements (0)
    Jump if False (Regular) to Block[B2]
        IParameterReferenceOperation: a (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'a')

    Next (Regular) Block[B3]
Block[B2] - Block
    Predecessors: [B1]
    Statements (1)
        IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'c = true;')
          Expression: 
            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Boolean) (Syntax: 'c = true')
              Left: 
                IParameterReferenceOperation: c (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'c')
              Right: 
                ILiteralOperation (OperationKind.Literal, Type: System.Boolean, Constant: True) (Syntax: 'true')

    Next (Regular) Block[B3]
Block[B3] - Block
    Predecessors: [B1] [B2]
    Statements (0)
    Jump if False (Regular) to Block[B4]
        IParameterReferenceOperation: b (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'b')

    Next (Regular) Block[B1]
Block[B4] - Block
    Predecessors: [B3]
    Statements (0)
    Jump if False (Regular) to Block[B5]
        IParameterReferenceOperation: c (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'c')

    Next (Regular) Block[B1]
Block[B5] - Exit
    Predecessors: [B4]
    Statements (0)
";
            VerifyFlowGraphForTest<BlockSyntax>(compilation, expectedGraph);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void BranchFlow_03()
        {
            var source = @"
class C
{
    void F(bool a, bool b, bool c)
    /*<bind>*/{
label1: if (a) goto label2;
        if (b) goto label2;
        c = true;
label2: if (c) goto label1;
    }/*</bind>*/
}";

            var compilation = CreateCompilation(source, parseOptions: TestOptions.RegularWithFlowAnalysisFeature);

            compilation.VerifyDiagnostics();

            string expectedGraph = @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
Block[B1] - Block
    Predecessors: [B0] [B4]
    Statements (0)
    Jump if False (Regular) to Block[B2]
        IParameterReferenceOperation: a (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'a')

    Next (Regular) Block[B4]
Block[B2] - Block
    Predecessors: [B1]
    Statements (0)
    Jump if False (Regular) to Block[B3]
        IParameterReferenceOperation: b (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'b')

    Next (Regular) Block[B4]
Block[B3] - Block
    Predecessors: [B2]
    Statements (1)
        IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'c = true;')
          Expression: 
            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Boolean) (Syntax: 'c = true')
              Left: 
                IParameterReferenceOperation: c (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'c')
              Right: 
                ILiteralOperation (OperationKind.Literal, Type: System.Boolean, Constant: True) (Syntax: 'true')

    Next (Regular) Block[B4]
Block[B4] - Block
    Predecessors: [B1] [B2] [B3]
    Statements (0)
    Jump if False (Regular) to Block[B5]
        IParameterReferenceOperation: c (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'c')

    Next (Regular) Block[B1]
Block[B5] - Exit
    Predecessors: [B4]
    Statements (0)
";
            VerifyFlowGraphForTest<BlockSyntax>(compilation, expectedGraph);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void BranchFlow_04()
        {
            var source = @"
class C
{
    void F(bool a, bool b, bool c)
    /*<bind>*/{
label1: if (a) goto label2;
        goto label3;
label2: c = true;
label3: if (b) goto label4;
        goto label1;
label4: if (c) goto label5;
        goto label1;
label5: ;
    }/*</bind>*/
}";

            var compilation = CreateCompilation(source, parseOptions: TestOptions.RegularWithFlowAnalysisFeature);

            compilation.VerifyDiagnostics();

            string expectedGraph = @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
Block[B1] - Block
    Predecessors: [B0] [B3] [B4]
    Statements (0)
    Jump if False (Regular) to Block[B3]
        IParameterReferenceOperation: a (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'a')

    Next (Regular) Block[B2]
Block[B2] - Block
    Predecessors: [B1]
    Statements (1)
        IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'c = true;')
          Expression: 
            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Boolean) (Syntax: 'c = true')
              Left: 
                IParameterReferenceOperation: c (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'c')
              Right: 
                ILiteralOperation (OperationKind.Literal, Type: System.Boolean, Constant: True) (Syntax: 'true')

    Next (Regular) Block[B3]
Block[B3] - Block
    Predecessors: [B1] [B2]
    Statements (0)
    Jump if False (Regular) to Block[B1]
        IParameterReferenceOperation: b (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'b')

    Next (Regular) Block[B4]
Block[B4] - Block
    Predecessors: [B3]
    Statements (0)
    Jump if False (Regular) to Block[B1]
        IParameterReferenceOperation: c (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'c')

    Next (Regular) Block[B5]
Block[B5] - Exit
    Predecessors: [B4]
    Statements (0)
";
            VerifyFlowGraphForTest<BlockSyntax>(compilation, expectedGraph);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void BranchFlow_05()
        {
            var source = @"
class C
{
    void F(bool a, bool b, bool c)
    /*<bind>*/{
label1: if (a) goto label2;
        goto label4;
label2: if (b) goto label3;
        goto label4;
label3: c = true;
label4: if (c) goto label5;
        goto label1;
label5: ;
    }/*</bind>*/
}";

            var compilation = CreateCompilation(source, parseOptions: TestOptions.RegularWithFlowAnalysisFeature);

            compilation.VerifyDiagnostics();

            string expectedGraph = @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
Block[B1] - Block
    Predecessors: [B0] [B4]
    Statements (0)
    Jump if False (Regular) to Block[B4]
        IParameterReferenceOperation: a (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'a')

    Next (Regular) Block[B2]
Block[B2] - Block
    Predecessors: [B1]
    Statements (0)
    Jump if False (Regular) to Block[B4]
        IParameterReferenceOperation: b (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'b')

    Next (Regular) Block[B3]
Block[B3] - Block
    Predecessors: [B2]
    Statements (1)
        IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'c = true;')
          Expression: 
            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Boolean) (Syntax: 'c = true')
              Left: 
                IParameterReferenceOperation: c (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'c')
              Right: 
                ILiteralOperation (OperationKind.Literal, Type: System.Boolean, Constant: True) (Syntax: 'true')

    Next (Regular) Block[B4]
Block[B4] - Block
    Predecessors: [B1] [B2] [B3]
    Statements (0)
    Jump if False (Regular) to Block[B1]
        IParameterReferenceOperation: c (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'c')

    Next (Regular) Block[B5]
Block[B5] - Exit
    Predecessors: [B4]
    Statements (0)
";
            VerifyFlowGraphForTest<BlockSyntax>(compilation, expectedGraph);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void BranchFlow_06()
        {
            var source = @"
class C
{
    void F()
    /*<bind>*/{
        goto label1;
    }/*</bind>*/
}";

            var compilation = CreateCompilation(source, parseOptions: TestOptions.RegularWithFlowAnalysisFeature);

            compilation.VerifyDiagnostics(
                // (6,14): error CS0159: No such label 'label1' within the scope of the goto statement
                //         goto label1;
                Diagnostic(ErrorCode.ERR_LabelNotFound, "label1").WithArguments("label1").WithLocation(6, 14)
                );

            string expectedGraph = @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
Block[B1] - Block
    Predecessors: [B0]
    Statements (2)
        IInvalidOperation (OperationKind.Invalid, Type: ?, IsInvalid) (Syntax: 'label1')
          Children(0)

        IInvalidOperation (OperationKind.Invalid, Type: null, IsInvalid) (Syntax: 'goto label1;')
          Children(0)

    Next (Regular) Block[B2]
Block[B2] - Exit
    Predecessors: [B1]
    Statements (0)
";
            VerifyFlowGraphForTest<BlockSyntax>(compilation, expectedGraph);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void BranchFlow_07()
        {
            var source = @"
class C
{
    void F()
    /*<bind>*/{
        goto label1;
        goto label1;
    }/*</bind>*/
}";

            var compilation = CreateCompilation(source, parseOptions: TestOptions.RegularWithFlowAnalysisFeature);

            compilation.VerifyDiagnostics(
                // (6,14): error CS0159: No such label 'label1' within the scope of the goto statement
                //         goto label1;
                Diagnostic(ErrorCode.ERR_LabelNotFound, "label1").WithArguments("label1").WithLocation(6, 14),
                // (7,14): error CS0159: No such label 'label1' within the scope of the goto statement
                //         goto label1;
                Diagnostic(ErrorCode.ERR_LabelNotFound, "label1").WithArguments("label1").WithLocation(7, 14)
                );

            string expectedGraph = @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
Block[B1] - Block
    Predecessors: [B0]
    Statements (4)
        IInvalidOperation (OperationKind.Invalid, Type: ?, IsInvalid) (Syntax: 'label1')
          Children(0)

        IInvalidOperation (OperationKind.Invalid, Type: null, IsInvalid) (Syntax: 'goto label1;')
          Children(0)

        IInvalidOperation (OperationKind.Invalid, Type: ?, IsInvalid) (Syntax: 'label1')
          Children(0)

        IInvalidOperation (OperationKind.Invalid, Type: null, IsInvalid) (Syntax: 'goto label1;')
          Children(0)

    Next (Regular) Block[B2]
Block[B2] - Exit
    Predecessors: [B1]
    Statements (0)
";
            VerifyFlowGraphForTest<BlockSyntax>(compilation, expectedGraph);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void BranchFlow_08()
        {
            var source = @"
class C
{
    void F(bool a)
    /*<bind>*/{
        if (a) goto label2;
    }/*</bind>*/
}";

            var compilation = CreateCompilation(source, parseOptions: TestOptions.RegularWithFlowAnalysisFeature);

            compilation.VerifyDiagnostics(
                // (6,21): error CS0159: No such label 'label2' within the scope of the goto statement
                //         if (a) goto label2;
                Diagnostic(ErrorCode.ERR_LabelNotFound, "label2").WithArguments("label2").WithLocation(6, 21)
                );

            string expectedGraph = @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
Block[B1] - Block
    Predecessors: [B0]
    Statements (0)
    Jump if False (Regular) to Block[B3]
        IParameterReferenceOperation: a (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'a')

    Next (Regular) Block[B2]
Block[B2] - Block
    Predecessors: [B1]
    Statements (2)
        IInvalidOperation (OperationKind.Invalid, Type: ?, IsInvalid) (Syntax: 'label2')
          Children(0)

        IInvalidOperation (OperationKind.Invalid, Type: null, IsInvalid) (Syntax: 'goto label2;')
          Children(0)

    Next (Regular) Block[B3]
Block[B3] - Exit
    Predecessors: [B1] [B2]
    Statements (0)
";
            VerifyFlowGraphForTest<BlockSyntax>(compilation, expectedGraph);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void BranchFlow_09()
        {
            var source = @"
class C
{
    void F(bool a, bool b)
    /*<bind>*/{
        if (a) goto label2;
        if (b) goto label2;
    }/*</bind>*/
}";

            var compilation = CreateCompilation(source, parseOptions: TestOptions.RegularWithFlowAnalysisFeature);

            compilation.VerifyDiagnostics(
                // (6,21): error CS0159: No such label 'label2' within the scope of the goto statement
                //         if (a) goto label2;
                Diagnostic(ErrorCode.ERR_LabelNotFound, "label2").WithArguments("label2").WithLocation(6, 21),
                // (7,21): error CS0159: No such label 'label2' within the scope of the goto statement
                //         if (b) goto label2;
                Diagnostic(ErrorCode.ERR_LabelNotFound, "label2").WithArguments("label2").WithLocation(7, 21)
                );

            string expectedGraph = @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
Block[B1] - Block
    Predecessors: [B0]
    Statements (0)
    Jump if False (Regular) to Block[B3]
        IParameterReferenceOperation: a (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'a')

    Next (Regular) Block[B2]
Block[B2] - Block
    Predecessors: [B1]
    Statements (2)
        IInvalidOperation (OperationKind.Invalid, Type: ?, IsInvalid) (Syntax: 'label2')
          Children(0)

        IInvalidOperation (OperationKind.Invalid, Type: null, IsInvalid) (Syntax: 'goto label2;')
          Children(0)

    Next (Regular) Block[B3]
Block[B3] - Block
    Predecessors: [B1] [B2]
    Statements (0)
    Jump if False (Regular) to Block[B5]
        IParameterReferenceOperation: b (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'b')

    Next (Regular) Block[B4]
Block[B4] - Block
    Predecessors: [B3]
    Statements (2)
        IInvalidOperation (OperationKind.Invalid, Type: ?, IsInvalid) (Syntax: 'label2')
          Children(0)

        IInvalidOperation (OperationKind.Invalid, Type: null, IsInvalid) (Syntax: 'goto label2;')
          Children(0)

    Next (Regular) Block[B5]
Block[B5] - Exit
    Predecessors: [B3] [B4]
    Statements (0)
";
            VerifyFlowGraphForTest<BlockSyntax>(compilation, expectedGraph);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void BranchFlow_10()
        {
            var source = @"
class C
{
    void F(bool a)
    /*<bind>*/{
        if (a) goto label2;
        goto label1;
label2: ;
    }/*</bind>*/
}";

            var compilation = CreateCompilation(source, parseOptions: TestOptions.RegularWithFlowAnalysisFeature);

            compilation.VerifyDiagnostics(
                // (7,14): error CS0159: No such label 'label1' within the scope of the goto statement
                //         goto label1;
                Diagnostic(ErrorCode.ERR_LabelNotFound, "label1").WithArguments("label1").WithLocation(7, 14)
                );

            string expectedGraph = @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
Block[B1] - Block
    Predecessors: [B0]
    Statements (0)
    Jump if False (Regular) to Block[B2]
        IParameterReferenceOperation: a (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'a')

    Next (Regular) Block[B3]
Block[B2] - Block
    Predecessors: [B1]
    Statements (2)
        IInvalidOperation (OperationKind.Invalid, Type: ?, IsInvalid) (Syntax: 'label1')
          Children(0)

        IInvalidOperation (OperationKind.Invalid, Type: null, IsInvalid) (Syntax: 'goto label1;')
          Children(0)

    Next (Regular) Block[B3]
Block[B3] - Exit
    Predecessors: [B1] [B2]
    Statements (0)
";
            VerifyFlowGraphForTest<BlockSyntax>(compilation, expectedGraph);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void BranchFlow_11()
        {
            var source = @"
class C
{
    void F(bool a, bool b)
    /*<bind>*/{
        if (a) goto label2;
        goto label1;
label2: if (b) goto label3;
        goto label1;
label3: ;
    }/*</bind>*/
}";

            var compilation = CreateCompilation(source, parseOptions: TestOptions.RegularWithFlowAnalysisFeature);

            compilation.VerifyDiagnostics(
                // (7,14): error CS0159: No such label 'label1' within the scope of the goto statement
                //         goto label1;
                Diagnostic(ErrorCode.ERR_LabelNotFound, "label1").WithArguments("label1").WithLocation(7, 14),
                // (9,14): error CS0159: No such label 'label1' within the scope of the goto statement
                //         goto label1;
                Diagnostic(ErrorCode.ERR_LabelNotFound, "label1").WithArguments("label1").WithLocation(9, 14)
                );

            string expectedGraph = @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
Block[B1] - Block
    Predecessors: [B0]
    Statements (0)
    Jump if False (Regular) to Block[B2]
        IParameterReferenceOperation: a (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'a')

    Next (Regular) Block[B3]
Block[B2] - Block
    Predecessors: [B1]
    Statements (2)
        IInvalidOperation (OperationKind.Invalid, Type: ?, IsInvalid) (Syntax: 'label1')
          Children(0)

        IInvalidOperation (OperationKind.Invalid, Type: null, IsInvalid) (Syntax: 'goto label1;')
          Children(0)

    Next (Regular) Block[B3]
Block[B3] - Block
    Predecessors: [B1] [B2]
    Statements (0)
    Jump if False (Regular) to Block[B4]
        IParameterReferenceOperation: b (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'b')

    Next (Regular) Block[B5]
Block[B4] - Block
    Predecessors: [B3]
    Statements (2)
        IInvalidOperation (OperationKind.Invalid, Type: ?, IsInvalid) (Syntax: 'label1')
          Children(0)

        IInvalidOperation (OperationKind.Invalid, Type: null, IsInvalid) (Syntax: 'goto label1;')
          Children(0)

    Next (Regular) Block[B5]
Block[B5] - Exit
    Predecessors: [B3] [B4]
    Statements (0)
";
            VerifyFlowGraphForTest<BlockSyntax>(compilation, expectedGraph);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void BranchFlow_12()
        {
            var source = @"
class C
{
    void F(bool a, bool b)
    /*<bind>*/{
        while (a)
        {
            if (b) break;
            a = false;
        }
    }/*</bind>*/
}";

            var compilation = CreateCompilation(source, parseOptions: TestOptions.RegularWithFlowAnalysisFeature);

            compilation.VerifyDiagnostics();

            string expectedGraph = @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
Block[B1] - Block
    Predecessors: [B0] [B3]
    Statements (0)
    Jump if False (Regular) to Block[B4]
        IParameterReferenceOperation: a (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'a')

    Next (Regular) Block[B2]
Block[B2] - Block
    Predecessors: [B1]
    Statements (0)
    Jump if False (Regular) to Block[B3]
        IParameterReferenceOperation: b (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'b')

    Next (Regular) Block[B4]
Block[B3] - Block
    Predecessors: [B2]
    Statements (1)
        IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'a = false;')
          Expression: 
            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Boolean) (Syntax: 'a = false')
              Left: 
                IParameterReferenceOperation: a (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'a')
              Right: 
                ILiteralOperation (OperationKind.Literal, Type: System.Boolean, Constant: False) (Syntax: 'false')

    Next (Regular) Block[B1]
Block[B4] - Exit
    Predecessors: [B1] [B2]
    Statements (0)
";
            VerifyFlowGraphForTest<BlockSyntax>(compilation, expectedGraph);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void BranchFlow_15()
        {
            var source = @"
class C
{
    void F()
    /*<bind>*/{
        break;
    }/*</bind>*/
}";

            var compilation = CreateCompilation(source, parseOptions: TestOptions.RegularWithFlowAnalysisFeature);

            compilation.VerifyDiagnostics(
                // (6,9): error CS0139: No enclosing loop out of which to break or continue
                //         break;
                Diagnostic(ErrorCode.ERR_NoBreakOrCont, "break;").WithLocation(6, 9)
                );

            string expectedGraph = @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
Block[B1] - Block
    Predecessors: [B0]
    Statements (1)
        IInvalidOperation (OperationKind.Invalid, Type: null, IsInvalid) (Syntax: 'break;')
          Children(0)

    Next (Regular) Block[B2]
Block[B2] - Exit
    Predecessors: [B1]
    Statements (0)
";
            VerifyFlowGraphForTest<BlockSyntax>(compilation, expectedGraph);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void BranchFlow_16()
        {
            string source = @"
class P
{
    void M(bool x, bool y)
/*<bind>*/{
        while (filter(out var j))
        {
            if (x) continue;
            y = false;
        }
    }/*</bind>*/
    bool filter(out int i) => throw null;
}
";
            string expectedGraph = @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
Block[B1] - Block
    Predecessors: [B0] [B3] [B4]
    Statements (0)
    Next (Regular) Block[B2]
        Entering: {R1}

.locals {R1}
{
    Locals: [System.Int32 j]
    Block[B2] - Block
        Predecessors: [B1]
        Statements (0)
        Jump if False (Regular) to Block[B5]
            IInvocationOperation ( System.Boolean P.filter(out System.Int32 i)) (OperationKind.Invocation, Type: System.Boolean) (Syntax: 'filter(out var j)')
              Instance Receiver: 
                IInstanceReferenceOperation (ReferenceKind: ContainingTypeInstance) (OperationKind.InstanceReference, Type: P, IsImplicit) (Syntax: 'filter')
              Arguments(1):
                  IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: i) (OperationKind.Argument, Type: null) (Syntax: 'out var j')
                    IDeclarationExpressionOperation (OperationKind.DeclarationExpression, Type: System.Int32) (Syntax: 'var j')
                      ILocalReferenceOperation: j (IsDeclaration: True) (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'j')
                    InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
            Leaving: {R1}

        Next (Regular) Block[B3]
    Block[B3] - Block
        Predecessors: [B2]
        Statements (0)
        Jump if False (Regular) to Block[B4]
            IParameterReferenceOperation: x (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'x')

        Next (Regular) Block[B1]
            Leaving: {R1}
    Block[B4] - Block
        Predecessors: [B3]
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'y = false;')
              Expression: 
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Boolean) (Syntax: 'y = false')
                  Left: 
                    IParameterReferenceOperation: y (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'y')
                  Right: 
                    ILiteralOperation (OperationKind.Literal, Type: System.Boolean, Constant: False) (Syntax: 'false')

        Next (Regular) Block[B1]
            Leaving: {R1}
}

Block[B5] - Exit
    Predecessors: [B2]
    Statements (0)
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(source, expectedGraph, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void BranchFlow_17()
        {
            string source = @"
class P
{
    void M(bool x, bool y)
/*<bind>*/{
        do
        {
            if (x) continue;
            y = false;
        }
        while  (filter(out var j));
    }/*</bind>*/
    bool filter(out int i) => throw null;
}
";
            string expectedGraph = @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
Block[B1] - Block
    Predecessors: [B0] [B4]
    Statements (0)
    Next (Regular) Block[B2]
        Entering: {R1}

.locals {R1}
{
    Locals: [System.Int32 j]
    Block[B2] - Block
        Predecessors: [B1]
        Statements (0)
        Jump if False (Regular) to Block[B3]
            IParameterReferenceOperation: x (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'x')

        Next (Regular) Block[B4]
    Block[B3] - Block
        Predecessors: [B2]
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'y = false;')
              Expression: 
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Boolean) (Syntax: 'y = false')
                  Left: 
                    IParameterReferenceOperation: y (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'y')
                  Right: 
                    ILiteralOperation (OperationKind.Literal, Type: System.Boolean, Constant: False) (Syntax: 'false')

        Next (Regular) Block[B4]
    Block[B4] - Block
        Predecessors: [B2] [B3]
        Statements (0)
        Jump if True (Regular) to Block[B1]
            IInvocationOperation ( System.Boolean P.filter(out System.Int32 i)) (OperationKind.Invocation, Type: System.Boolean) (Syntax: 'filter(out var j)')
              Instance Receiver: 
                IInstanceReferenceOperation (ReferenceKind: ContainingTypeInstance) (OperationKind.InstanceReference, Type: P, IsImplicit) (Syntax: 'filter')
              Arguments(1):
                  IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: i) (OperationKind.Argument, Type: null) (Syntax: 'out var j')
                    IDeclarationExpressionOperation (OperationKind.DeclarationExpression, Type: System.Int32) (Syntax: 'var j')
                      ILocalReferenceOperation: j (IsDeclaration: True) (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'j')
                    InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
            Leaving: {R1}

        Next (Regular) Block[B5]
            Leaving: {R1}
}

Block[B5] - Exit
    Predecessors: [B4]
    Statements (0)
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(source, expectedGraph, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void BranchFlow_19()
        {
            string source = @"
class P
{
    void M()
/*<bind>*/{
        continue;
    }/*</bind>*/
}
";
            string expectedGraph = @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
Block[B1] - Block
    Predecessors: [B0]
    Statements (1)
        IInvalidOperation (OperationKind.Invalid, Type: null, IsInvalid) (Syntax: 'continue;')
          Children(0)

    Next (Regular) Block[B2]
Block[B2] - Exit
    Predecessors: [B1]
    Statements (0)
";
            var expectedDiagnostics = new[] {
                // file.cs(6,9): error CS0139: No enclosing loop out of which to break or continue
                //         continue;
                Diagnostic(ErrorCode.ERR_NoBreakOrCont, "continue;").WithLocation(6, 9)
            };

            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(source, expectedGraph, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void BranchFlow_25()
        {
            string source = @"
class P
{
    void M(bool x)
/*<bind>*/{
        goto finallyLabel;
        goto catchlabel;
        goto trylabel;

        try
        {
trylabel:
            goto finallyLabel;
            goto catchlabel;
            goto outsideLabel;
        }
        catch
        {
catchlabel:
            goto finallyLabel;
            goto trylabel;
            goto outsideLabel;
        }
        finally
        {
finallyLabel:
            goto catchlabel;
            goto trylabel;
            goto outsideLabel;
        }

        x = true;
outsideLabel:;
    }/*</bind>*/
}
";
            string expectedGraph = @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
Block[B1] - Block
    Predecessors: [B0]
    Statements (6)
        IInvalidOperation (OperationKind.Invalid, Type: ?, IsInvalid) (Syntax: 'finallyLabel')
          Children(0)

        IInvalidOperation (OperationKind.Invalid, Type: null, IsInvalid) (Syntax: 'goto finallyLabel;')
          Children(0)

        IInvalidOperation (OperationKind.Invalid, Type: ?, IsInvalid) (Syntax: 'catchlabel')
          Children(0)

        IInvalidOperation (OperationKind.Invalid, Type: null, IsInvalid) (Syntax: 'goto catchlabel;')
          Children(0)

        IInvalidOperation (OperationKind.Invalid, Type: ?, IsInvalid) (Syntax: 'trylabel')
          Children(0)

        IInvalidOperation (OperationKind.Invalid, Type: null, IsInvalid) (Syntax: 'goto trylabel;')
          Children(0)

    Next (Regular) Block[B2]
        Entering: {R1} {R2} {R3} {R4}

.try {R1, R2}
{
    .try {R3, R4}
    {
        Block[B2] - Block
            Predecessors: [B1]
            Statements (4)
                IInvalidOperation (OperationKind.Invalid, Type: ?, IsInvalid) (Syntax: 'finallyLabel')
                  Children(0)

                IInvalidOperation (OperationKind.Invalid, Type: null, IsInvalid) (Syntax: 'goto finallyLabel;')
                  Children(0)

                IInvalidOperation (OperationKind.Invalid, Type: ?, IsInvalid) (Syntax: 'catchlabel')
                  Children(0)

                IInvalidOperation (OperationKind.Invalid, Type: null, IsInvalid) (Syntax: 'goto catchlabel;')
                  Children(0)

            Next (Regular) Block[B6]
                Finalizing: {R6}
                Leaving: {R4} {R3} {R2} {R1}
    }
    .catch {R5} (System.Object)
    {
        Block[B3] - Block
            Predecessors (0)
            Statements (4)
                IInvalidOperation (OperationKind.Invalid, Type: ?, IsInvalid) (Syntax: 'finallyLabel')
                  Children(0)

                IInvalidOperation (OperationKind.Invalid, Type: null, IsInvalid) (Syntax: 'goto finallyLabel;')
                  Children(0)

                IInvalidOperation (OperationKind.Invalid, Type: ?, IsInvalid) (Syntax: 'trylabel')
                  Children(0)

                IInvalidOperation (OperationKind.Invalid, Type: null, IsInvalid) (Syntax: 'goto trylabel;')
                  Children(0)

            Next (Regular) Block[B6]
                Finalizing: {R6}
                Leaving: {R5} {R3} {R2} {R1}
    }
}
.finally {R6}
{
    Block[B4] - Block
        Predecessors (0)
        Statements (4)
            IInvalidOperation (OperationKind.Invalid, Type: ?, IsInvalid) (Syntax: 'catchlabel')
              Children(0)

            IInvalidOperation (OperationKind.Invalid, Type: null, IsInvalid) (Syntax: 'goto catchlabel;')
              Children(0)

            IInvalidOperation (OperationKind.Invalid, Type: ?, IsInvalid) (Syntax: 'trylabel')
              Children(0)

            IInvalidOperation (OperationKind.Invalid, Type: null, IsInvalid) (Syntax: 'goto trylabel;')
              Children(0)

        Next (Regular) Block[B6]
            Leaving: {R6} {R1}
}

Block[B5] - Block [UnReachable]
    Predecessors (0)
    Statements (1)
        IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'x = true;')
          Expression: 
            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Boolean) (Syntax: 'x = true')
              Left: 
                IParameterReferenceOperation: x (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'x')
              Right: 
                ILiteralOperation (OperationKind.Literal, Type: System.Boolean, Constant: True) (Syntax: 'true')

    Next (Regular) Block[B6]
Block[B6] - Exit
    Predecessors: [B2] [B3] [B4] [B5]
    Statements (0)
";
            var expectedDiagnostics = new[] {
                // file.cs(6,14): error CS0159: No such label 'finallyLabel' within the scope of the goto statement
                //         goto finallyLabel;
                Diagnostic(ErrorCode.ERR_LabelNotFound, "finallyLabel").WithArguments("finallyLabel").WithLocation(6, 14),
                // file.cs(7,14): error CS0159: No such label 'catchlabel' within the scope of the goto statement
                //         goto catchlabel;
                Diagnostic(ErrorCode.ERR_LabelNotFound, "catchlabel").WithArguments("catchlabel").WithLocation(7, 14),
                // file.cs(8,14): error CS0159: No such label 'trylabel' within the scope of the goto statement
                //         goto trylabel;
                Diagnostic(ErrorCode.ERR_LabelNotFound, "trylabel").WithArguments("trylabel").WithLocation(8, 14),
                // file.cs(13,18): error CS0159: No such label 'finallyLabel' within the scope of the goto statement
                //             goto finallyLabel;
                Diagnostic(ErrorCode.ERR_LabelNotFound, "finallyLabel").WithArguments("finallyLabel").WithLocation(13, 18),
                // file.cs(14,18): error CS0159: No such label 'catchlabel' within the scope of the goto statement
                //             goto catchlabel;
                Diagnostic(ErrorCode.ERR_LabelNotFound, "catchlabel").WithArguments("catchlabel").WithLocation(14, 18),
                // file.cs(20,18): error CS0159: No such label 'finallyLabel' within the scope of the goto statement
                //             goto finallyLabel;
                Diagnostic(ErrorCode.ERR_LabelNotFound, "finallyLabel").WithArguments("finallyLabel").WithLocation(20, 18),
                // file.cs(21,18): error CS0159: No such label 'trylabel' within the scope of the goto statement
                //             goto trylabel;
                Diagnostic(ErrorCode.ERR_LabelNotFound, "trylabel").WithArguments("trylabel").WithLocation(21, 18),
                // file.cs(27,18): error CS0159: No such label 'catchlabel' within the scope of the goto statement
                //             goto catchlabel;
                Diagnostic(ErrorCode.ERR_LabelNotFound, "catchlabel").WithArguments("catchlabel").WithLocation(27, 18),
                // file.cs(28,18): error CS0159: No such label 'trylabel' within the scope of the goto statement
                //             goto trylabel;
                Diagnostic(ErrorCode.ERR_LabelNotFound, "trylabel").WithArguments("trylabel").WithLocation(28, 18),
                // file.cs(29,13): error CS0157: Control cannot leave the body of a finally clause
                //             goto outsideLabel;
                Diagnostic(ErrorCode.ERR_BadFinallyLeave, "goto").WithLocation(29, 13),
                // file.cs(32,9): warning CS0162: Unreachable code detected
                //         x = true;
                Diagnostic(ErrorCode.WRN_UnreachableCode, "x").WithLocation(32, 9),
                // file.cs(12,1): warning CS0164: This label has not been referenced
                // trylabel:
                Diagnostic(ErrorCode.WRN_UnreferencedLabel, "trylabel").WithLocation(12, 1),
                // file.cs(19,1): warning CS0164: This label has not been referenced
                // catchlabel:
                Diagnostic(ErrorCode.WRN_UnreferencedLabel, "catchlabel").WithLocation(19, 1),
                // file.cs(26,1): warning CS0164: This label has not been referenced
                // finallyLabel:
                Diagnostic(ErrorCode.WRN_UnreferencedLabel, "finallyLabel").WithLocation(26, 1)
            };

            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(source, expectedGraph, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void BranchFlow_49()
        {
            var source = @"
class C
{
    void F(bool a)
    /*<bind>*/{
        if (a) goto label1;
        goto label1;

label1: return;
    }/*</bind>*/
}";

            var compilation = CreateCompilation(source, parseOptions: TestOptions.RegularWithFlowAnalysisFeature);

            compilation.VerifyDiagnostics();

            string expectedGraph = @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
Block[B1] - Block
    Predecessors: [B0]
    Statements (0)
    Jump if False (Regular) to Block[B2]
        IParameterReferenceOperation: a (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'a')

    Next (Regular) Block[B2]
Block[B2] - Exit
    Predecessors: [B1*2]
    Statements (0)
";
            VerifyFlowGraphForTest<BlockSyntax>(compilation, expectedGraph);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void BranchFlow_50()
        {
            var source = @"
class C
{
    void F(bool a, bool b)
    /*<bind>*/{
        goto label2;
label1:
        if (a) goto label3;
        goto label3;
label2:
        if (b) goto label1;
        goto label1;

label3: ;
    }/*</bind>*/
}";

            var compilation = CreateCompilation(source, parseOptions: TestOptions.RegularWithFlowAnalysisFeature);

            compilation.VerifyDiagnostics();

            string expectedGraph = @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B2]
Block[B1] - Block
    Predecessors: [B2*2]
    Statements (0)
    Jump if False (Regular) to Block[B3]
        IParameterReferenceOperation: a (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'a')

    Next (Regular) Block[B3]
Block[B2] - Block
    Predecessors: [B0]
    Statements (0)
    Jump if False (Regular) to Block[B1]
        IParameterReferenceOperation: b (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'b')

    Next (Regular) Block[B1]
Block[B3] - Exit
    Predecessors: [B1*2]
    Statements (0)
";
            VerifyFlowGraphForTest<BlockSyntax>(compilation, expectedGraph);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void BranchFlow_51()
        {
            var source = @"
class C
{
    void F(bool a, bool b)
    /*<bind>*/{
        goto label1;
label1:
        a = true;
        goto label3;
label1:
        b = false;

label3: ;
    }/*</bind>*/
}";

            var compilation = CreateCompilation(source, parseOptions: TestOptions.RegularWithFlowAnalysisFeature);

            compilation.VerifyDiagnostics(
                // (10,1): error CS0140: The label 'label1' is a duplicate
                // label1:
                Diagnostic(ErrorCode.ERR_DuplicateLabel, "label1").WithArguments("label1").WithLocation(10, 1)
                );

            string expectedGraph = @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
Block[B1] - Block
    Predecessors: [B0]
    Statements (1)
        IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'a = true;')
          Expression: 
            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Boolean) (Syntax: 'a = true')
              Left: 
                IParameterReferenceOperation: a (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'a')
              Right: 
                ILiteralOperation (OperationKind.Literal, Type: System.Boolean, Constant: True) (Syntax: 'true')

    Next (Regular) Block[B3]
Block[B2] - Block [UnReachable]
    Predecessors (0)
    Statements (1)
        IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'b = false;')
          Expression: 
            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Boolean) (Syntax: 'b = false')
              Left: 
                IParameterReferenceOperation: b (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'b')
              Right: 
                ILiteralOperation (OperationKind.Literal, Type: System.Boolean, Constant: False) (Syntax: 'false')

    Next (Regular) Block[B3]
Block[B3] - Exit
    Predecessors: [B1] [B2]
    Statements (0)
";
            VerifyFlowGraphForTest<BlockSyntax>(compilation, expectedGraph);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void BranchFlow_52()
        {
            var source = @"
class C
{
    void F(bool a, bool b)
    /*<bind>*/{
label1:
        a = true;
        goto label3;
label1:
        b = false;
        goto label1;

label3: ;
    }/*</bind>*/
}";

            var compilation = CreateCompilation(source, parseOptions: TestOptions.RegularWithFlowAnalysisFeature);

            compilation.VerifyDiagnostics(
                // (9,1): error CS0140: The label 'label1' is a duplicate
                // label1:
                Diagnostic(ErrorCode.ERR_DuplicateLabel, "label1").WithArguments("label1").WithLocation(9, 1)
                );

            string expectedGraph = @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
Block[B1] - Block
    Predecessors: [B0] [B2]
    Statements (1)
        IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'a = true;')
          Expression: 
            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Boolean) (Syntax: 'a = true')
              Left: 
                IParameterReferenceOperation: a (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'a')
              Right: 
                ILiteralOperation (OperationKind.Literal, Type: System.Boolean, Constant: True) (Syntax: 'true')

    Next (Regular) Block[B3]
Block[B2] - Block [UnReachable]
    Predecessors (0)
    Statements (1)
        IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'b = false;')
          Expression: 
            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Boolean) (Syntax: 'b = false')
              Left: 
                IParameterReferenceOperation: b (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'b')
              Right: 
                ILiteralOperation (OperationKind.Literal, Type: System.Boolean, Constant: False) (Syntax: 'false')

    Next (Regular) Block[B1]
Block[B3] - Exit
    Predecessors: [B1]
    Statements (0)
";
            VerifyFlowGraphForTest<BlockSyntax>(compilation, expectedGraph);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void BranchFlow_54()
        {
            string source = @"
#pragma warning disable CS8321
struct C
{
    void M()
/*<bind>*/{
        void local()
        {
            goto label;
        } 

label:  ;
    }/*</bind>*/
}
";
            string expectedGraph = @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
        Entering: {R1}

.locals {R1}
{
    Methods: [void local()]
    Block[B1] - Block
        Predecessors: [B0]
        Statements (0)
        Next (Regular) Block[B2]
            Leaving: {R1}
    
    {   void local()
    
        Block[B0#0R1] - Entry
            Statements (0)
            Next (Regular) Block[B1#0R1]
        Block[B1#0R1] - Block
            Predecessors: [B0#0R1]
            Statements (0)
            Next (Error) Block[null]
        Block[B2#0R1] - Exit [UnReachable]
            Predecessors (0)
            Statements (0)
    }
}

Block[B2] - Exit
    Predecessors: [B1]
    Statements (0)
";
            var expectedDiagnostics = new[] {
                // file.cs(9,13): error CS0159: No such label 'label' within the scope of the goto statement
                //             goto label;
                Diagnostic(ErrorCode.ERR_LabelNotFound, "goto").WithArguments("label").WithLocation(9, 13),
                // file.cs(12,1): warning CS0164: This label has not been referenced
                // label:  ;
                Diagnostic(ErrorCode.WRN_UnreferencedLabel, "label").WithLocation(12, 1)
            };

            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(source, expectedGraph, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void BranchFlow_55()
        {
            string source = @"
#pragma warning disable CS8321
struct C
{
    void M()
/*<bind>*/{
        void local(bool input)
        {
            if (input) return;

            goto label;
        } 

label:  ;
    }/*</bind>*/
}
";
            string expectedGraph = @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
        Entering: {R1}

.locals {R1}
{
    Methods: [void local(System.Boolean input)]
    Block[B1] - Block
        Predecessors: [B0]
        Statements (0)
        Next (Regular) Block[B2]
            Leaving: {R1}
    
    {   void local(System.Boolean input)
    
        Block[B0#0R1] - Entry
            Statements (0)
            Next (Regular) Block[B1#0R1]
        Block[B1#0R1] - Block
            Predecessors: [B0#0R1]
            Statements (0)
            Jump if False (Error) to Block[null]
                IParameterReferenceOperation: input (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'input')

            Next (Regular) Block[B2#0R1]
        Block[B2#0R1] - Exit
            Predecessors: [B1#0R1]
            Statements (0)
    }
}

Block[B2] - Exit
    Predecessors: [B1]
    Statements (0)
";
            var expectedDiagnostics = new[] {
                // file.cs(11,13): error CS0159: No such label 'label' within the scope of the goto statement
                //             goto label;
                Diagnostic(ErrorCode.ERR_LabelNotFound, "goto").WithArguments("label").WithLocation(11, 13),
                // file.cs(14,1): warning CS0164: This label has not been referenced
                // label:  ;
                Diagnostic(ErrorCode.WRN_UnreferencedLabel, "label").WithLocation(14, 1)
            };

            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(source, expectedGraph, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void BranchFlow_56()
        {
            string source = @"
struct C
{
    void M(System.Action<bool> d)
/*<bind>*/{
        d = (bool input) =>
        {
            if (input) return;

            goto label;
        };

label:  ;
    }/*</bind>*/
}
";
            string expectedGraph = @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
Block[B1] - Block
    Predecessors: [B0]
    Statements (1)
        IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null, IsInvalid) (Syntax: 'd = (bool i ... };')
          Expression: 
            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Action<System.Boolean>, IsInvalid) (Syntax: 'd = (bool i ... }')
              Left: 
                IParameterReferenceOperation: d (OperationKind.ParameterReference, Type: System.Action<System.Boolean>) (Syntax: 'd')
              Right: 
                IDelegateCreationOperation (OperationKind.DelegateCreation, Type: System.Action<System.Boolean>, IsInvalid, IsImplicit) (Syntax: '(bool input ... }')
                  Target: 
                    IFlowAnonymousFunctionOperation (Symbol: lambda expression) (OperationKind.FlowAnonymousFunction, Type: null, IsInvalid) (Syntax: '(bool input ... }')
                    {
                        Block[B0#A0] - Entry
                            Statements (0)
                            Next (Regular) Block[B1#A0]
                        Block[B1#A0] - Block
                            Predecessors: [B0#A0]
                            Statements (0)
                            Jump if False (Error) to Block[null]
                                IParameterReferenceOperation: input (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'input')

                            Next (Regular) Block[B2#A0]
                        Block[B2#A0] - Exit
                            Predecessors: [B1#A0]
                            Statements (0)
                    }

    Next (Regular) Block[B2]
Block[B2] - Exit
    Predecessors: [B1]
    Statements (0)
";
            var expectedDiagnostics = new[] {
                // file.cs(10,13): error CS0159: No such label 'label' within the scope of the goto statement
                //             goto label;
                Diagnostic(ErrorCode.ERR_LabelNotFound, "goto").WithArguments("label").WithLocation(10, 13),
                // file.cs(13,1): warning CS0164: This label has not been referenced
                // label:  ;
                Diagnostic(ErrorCode.WRN_UnreferencedLabel, "label").WithLocation(13, 1)
            };

            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(source, expectedGraph, expectedDiagnostics);
        }
    }
}
