// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    public partial class IOperationTests : SemanticModelTestBase
    {
        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void DynamicObjectCreation_DynamicArgument()
        {
            string source = @"
class C
{
    public C(int i)
    {
    }

    void M(dynamic d)
    {
        var x = /*<bind>*/new C(d)/*</bind>*/;
    }
}
";
            string expectedOperationTree = @"
IDynamicObjectCreationOperation (OperationKind.DynamicObjectCreation, Type: C) (Syntax: 'new C(d)')
  Arguments(1):
      IParameterReferenceOperation: d (OperationKind.ParameterReference, Type: dynamic) (Syntax: 'd')
  ArgumentNames(0)
  ArgumentRefKinds(0)
  Initializer: 
    null
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<ObjectCreationExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void DynamicObjectCreation_MultipleApplicableSymbols()
        {
            string source = @"
class C
{
    public C(int i)
    {
    }

    public C(long i)
    {
    }

    void M(dynamic d)
    {
        var x = /*<bind>*/new C(d)/*</bind>*/;
    }
}
";
            string expectedOperationTree = @"
IDynamicObjectCreationOperation (OperationKind.DynamicObjectCreation, Type: C) (Syntax: 'new C(d)')
  Arguments(1):
      IParameterReferenceOperation: d (OperationKind.ParameterReference, Type: dynamic) (Syntax: 'd')
  ArgumentNames(0)
  ArgumentRefKinds(0)
  Initializer: 
    null
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<ObjectCreationExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void DynamicObjectCreation_MultipleArgumentsAndApplicableSymbols()
        {
            string source = @"
class C
{
    public C(int i, char c)
    {
    }

    public C(long i, char c)
    {
    }

    void M(dynamic d)
    {
        char c = 'c';
        var x = /*<bind>*/new C(d, c)/*</bind>*/;
    }
}
";
            string expectedOperationTree = @"
IDynamicObjectCreationOperation (OperationKind.DynamicObjectCreation, Type: C) (Syntax: 'new C(d, c)')
  Arguments(2):
      IParameterReferenceOperation: d (OperationKind.ParameterReference, Type: dynamic) (Syntax: 'd')
      ILocalReferenceOperation: c (OperationKind.LocalReference, Type: System.Char) (Syntax: 'c')
  ArgumentNames(0)
  ArgumentRefKinds(0)
  Initializer: 
    null
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<ObjectCreationExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void DynamicObjectCreation_ArgumentNames()
        {
            string source = @"
class C
{
    public C(int i, char c)
    {
    }

    public C(long i, char c)
    {
    }

    void M(dynamic d, dynamic e)
    {
        var x = /*<bind>*/new C(i: d, c: e)/*</bind>*/;
    }
}
";
            string expectedOperationTree = @"
IDynamicObjectCreationOperation (OperationKind.DynamicObjectCreation, Type: C) (Syntax: 'new C(i: d, c: e)')
  Arguments(2):
      IParameterReferenceOperation: d (OperationKind.ParameterReference, Type: dynamic) (Syntax: 'd')
      IParameterReferenceOperation: e (OperationKind.ParameterReference, Type: dynamic) (Syntax: 'e')
  ArgumentNames(2):
    ""i""
    ""c""
  ArgumentRefKinds(0)
  Initializer: 
    null
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<ObjectCreationExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void DynamicObjectCreation_ArgumentRefKinds()
        {
            string source = @"
class C
{
    public C(ref object i, out int j, char c)
    {
        j = 0;
    }

    void M(object d, dynamic e)
    {
        int k;
        var x = /*<bind>*/new C(ref d, out k, e)/*</bind>*/;
    }
}
";
            string expectedOperationTree = @"
IDynamicObjectCreationOperation (OperationKind.DynamicObjectCreation, Type: C) (Syntax: 'new C(ref d, out k, e)')
  Arguments(3):
      IParameterReferenceOperation: d (OperationKind.ParameterReference, Type: System.Object) (Syntax: 'd')
      ILocalReferenceOperation: k (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'k')
      IParameterReferenceOperation: e (OperationKind.ParameterReference, Type: dynamic) (Syntax: 'e')
  ArgumentNames(0)
  ArgumentRefKinds(3):
    Ref
    Out
    None
  Initializer: 
    null
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<ObjectCreationExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void DynamicObjectCreation_Initializer()
        {
            string source = @"
class C
{
    public int X;

    public C(char c)
    {
    }

    void M(dynamic d)
    {
        var x = /*<bind>*/new C(d) { X = 0 }/*</bind>*/;
    }
}
";
            string expectedOperationTree = @"
IDynamicObjectCreationOperation (OperationKind.DynamicObjectCreation, Type: C) (Syntax: 'new C(d) { X = 0 }')
  Arguments(1):
      IParameterReferenceOperation: d (OperationKind.ParameterReference, Type: dynamic) (Syntax: 'd')
  ArgumentNames(0)
  ArgumentRefKinds(0)
  Initializer: 
    IObjectOrCollectionInitializerOperation (OperationKind.ObjectOrCollectionInitializer, Type: C) (Syntax: '{ X = 0 }')
      Initializers(1):
          ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32) (Syntax: 'X = 0')
            Left: 
              IFieldReferenceOperation: System.Int32 C.X (OperationKind.FieldReference, Type: System.Int32) (Syntax: 'X')
                Instance Receiver: 
                  IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: C, IsImplicit) (Syntax: 'X')
            Right: 
              ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 0) (Syntax: '0')
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<ObjectCreationExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void DynamicObjectCreation_AllFields()
        {
            string source = @"
class C
{
    public int X;

    public C(ref int i, char c)
    {
    }

    public C(ref int i, long c)
    {
    }

    void M(dynamic d)
    {
        int i = 0;
        var x = /*<bind>*/new C(ref i, c: d) { X = 0 }/*</bind>*/;
    }
}
";
            string expectedOperationTree = @"
IDynamicObjectCreationOperation (OperationKind.DynamicObjectCreation, Type: C) (Syntax: 'new C(ref i ... ) { X = 0 }')
  Arguments(2):
      ILocalReferenceOperation: i (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'i')
      IParameterReferenceOperation: d (OperationKind.ParameterReference, Type: dynamic) (Syntax: 'd')
  ArgumentNames(2):
    ""null""
    ""c""
  ArgumentRefKinds(2):
    Ref
    None
  Initializer: 
    IObjectOrCollectionInitializerOperation (OperationKind.ObjectOrCollectionInitializer, Type: C) (Syntax: '{ X = 0 }')
      Initializers(1):
          ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32) (Syntax: 'X = 0')
            Left: 
              IFieldReferenceOperation: System.Int32 C.X (OperationKind.FieldReference, Type: System.Int32) (Syntax: 'X')
                Instance Receiver: 
                  IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: C, IsImplicit) (Syntax: 'X')
            Right: 
              ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 0) (Syntax: '0')
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<ObjectCreationExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void DynamicObjectCreation_ErrorBadDynamicMethodArgLambda()
        {
            string source = @"
using System;

class C
{
    static void Main()
    {
        dynamic y = null;
        /*<bind>*/new C(delegate { }, y)/*</bind>*/;
    }

    public C(Action a, Action y)
    {
    }
}
";
            string expectedOperationTree = @"
IDynamicObjectCreationOperation (OperationKind.DynamicObjectCreation, Type: C, IsInvalid) (Syntax: 'new C(delegate { }, y)')
  Arguments(2):
      IAnonymousFunctionOperation (Symbol: lambda expression) (OperationKind.AnonymousFunction, Type: null, IsInvalid) (Syntax: 'delegate { }')
        IBlockOperation (1 statements) (OperationKind.Block, Type: null, IsInvalid) (Syntax: '{ }')
          IReturnOperation (OperationKind.Return, Type: null, IsInvalid, IsImplicit) (Syntax: '{ }')
            ReturnedValue: 
              null
      ILocalReferenceOperation: y (OperationKind.LocalReference, Type: dynamic) (Syntax: 'y')
  ArgumentNames(0)
  ArgumentRefKinds(0)
  Initializer: 
    null
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS1977: Cannot use a lambda expression as an argument to a dynamically dispatched operation without first casting it to a delegate or expression tree type.
                //         /*<bind>*/new C(delegate { }, y)/*</bind>*/;
                Diagnostic(ErrorCode.ERR_BadDynamicMethodArgLambda, "delegate { }").WithLocation(9, 25)
            };

            VerifyOperationTreeAndDiagnosticsForTest<ObjectCreationExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void DynamicObjectCreation_OVerloadResolutionFailure()
        {
            string source = @"
class C
{
    public C()
    {
    }

    public C(int i, int j)
    {
    }

    void M(dynamic d)
    {
        var x = /*<bind>*/new C(d)/*</bind>*/;
    }
}
";
            string expectedOperationTree = @"
IInvalidOperation (OperationKind.Invalid, Type: C, IsInvalid) (Syntax: 'new C(d)')
  Children(1):
      IParameterReferenceOperation: d (OperationKind.ParameterReference, Type: dynamic) (Syntax: 'd')
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS7036: There is no argument given that corresponds to the required formal parameter 'j' of 'C.C(int, int)'
                //         var x = /*<bind>*/new C(d)/*</bind>*/;
                Diagnostic(ErrorCode.ERR_NoCorrespondingArgument, "C").WithArguments("j", "C.C(int, int)").WithLocation(14, 31)
            };

            VerifyOperationTreeAndDiagnosticsForTest<ObjectCreationExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void DynamicObjectCreationFlow_01()
        {
            string source = @"
class C1
{
    C1(int i) { }
    /*<bind>*/void M(C1 c1, dynamic d)
    {
        c1 = new C1(d);
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
        IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'c1 = new C1(d);')
          Expression: 
            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: C1) (Syntax: 'c1 = new C1(d)')
              Left: 
                IParameterReferenceOperation: c1 (OperationKind.ParameterReference, Type: C1) (Syntax: 'c1')
              Right: 
                IDynamicObjectCreationOperation (OperationKind.DynamicObjectCreation, Type: C1) (Syntax: 'new C1(d)')
                  Arguments(1):
                      IParameterReferenceOperation: d (OperationKind.ParameterReference, Type: dynamic) (Syntax: 'd')
                  ArgumentNames(0)
                  ArgumentRefKinds(0)
                  Initializer: 
                    null

    Next (Regular) Block[B2]
Block[B2] - Exit
    Predecessors: [B1]
    Statements (0)
";
            VerifyFlowGraphAndDiagnosticsForTest<MethodDeclarationSyntax>(source, expectedFlowGraph, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void DynamicObjectCreationFlow_02()
        {
            string source = @"
class C1
{
    C1(int i) { }
    /*<bind>*/void M(C1 c1, dynamic d, bool b)
    {
        c1 = new C1(d) { I1 = 1, I2 = b ? 2 : 3 };
    }/*</bind>*/
    int I1 { get; set; }
    int I2 { get; set; }
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
    CaptureIds: [0] [1]
    Block[B1] - Block
        Predecessors: [B0]
        Statements (3)
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'c1')
              Value: 
                IParameterReferenceOperation: c1 (OperationKind.ParameterReference, Type: C1) (Syntax: 'c1')

            IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'new C1(d) { ... b ? 2 : 3 }')
              Value: 
                IDynamicObjectCreationOperation (OperationKind.DynamicObjectCreation, Type: C1) (Syntax: 'new C1(d) { ... b ? 2 : 3 }')
                  Arguments(1):
                      IParameterReferenceOperation: d (OperationKind.ParameterReference, Type: dynamic) (Syntax: 'd')
                  ArgumentNames(0)
                  ArgumentRefKinds(0)
                  Initializer: 
                    null

            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32) (Syntax: 'I1 = 1')
              Left: 
                IPropertyReferenceOperation: System.Int32 C1.I1 { get; set; } (OperationKind.PropertyReference, Type: System.Int32) (Syntax: 'I1')
                  Instance Receiver: 
                    IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: C1, IsImplicit) (Syntax: 'new C1(d) { ... b ? 2 : 3 }')
              Right: 
                ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')

        Next (Regular) Block[B2]
            Entering: {R2}

    .locals {R2}
    {
        CaptureIds: [2]
        Block[B2] - Block
            Predecessors: [B1]
            Statements (0)
            Jump if False (Regular) to Block[B4]
                IParameterReferenceOperation: b (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'b')

            Next (Regular) Block[B3]
        Block[B3] - Block
            Predecessors: [B2]
            Statements (1)
                IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: '2')
                  Value: 
                    ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 2) (Syntax: '2')

            Next (Regular) Block[B5]
        Block[B4] - Block
            Predecessors: [B2]
            Statements (1)
                IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: '3')
                  Value: 
                    ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 3) (Syntax: '3')

            Next (Regular) Block[B5]
        Block[B5] - Block
            Predecessors: [B3] [B4]
            Statements (1)
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32) (Syntax: 'I2 = b ? 2 : 3')
                  Left: 
                    IPropertyReferenceOperation: System.Int32 C1.I2 { get; set; } (OperationKind.PropertyReference, Type: System.Int32) (Syntax: 'I2')
                      Instance Receiver: 
                        IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: C1, IsImplicit) (Syntax: 'new C1(d) { ... b ? 2 : 3 }')
                  Right: 
                    IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'b ? 2 : 3')

            Next (Regular) Block[B6]
                Leaving: {R2}
    }

    Block[B6] - Block
        Predecessors: [B5]
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'c1 = new C1 ...  ? 2 : 3 };')
              Expression: 
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: C1) (Syntax: 'c1 = new C1 ... b ? 2 : 3 }')
                  Left: 
                    IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: C1, IsImplicit) (Syntax: 'c1')
                  Right: 
                    IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: C1, IsImplicit) (Syntax: 'new C1(d) { ... b ? 2 : 3 }')

        Next (Regular) Block[B7]
            Leaving: {R1}
}

Block[B7] - Exit
    Predecessors: [B6]
    Statements (0)
";
            VerifyFlowGraphAndDiagnosticsForTest<MethodDeclarationSyntax>(source, expectedFlowGraph, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void DynamicObjectCreationFlow_03()
        {
            string source = @"
using System.Collections;
using System.Collections.Generic;

class C1 : IEnumerable<int>
{
    C1(int i) { }
    /*<bind>*/void M(C1 c1, dynamic d, bool b)
    {
        c1 = new C1(d) { 1, b ? 2 : 3 };
    }/*</bind>*/
    public IEnumerator<int> GetEnumerator() => throw new System.NotImplementedException();
    IEnumerator IEnumerable.GetEnumerator() => throw new System.NotImplementedException();
    public void Add(int i) { }
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
    CaptureIds: [0] [1]
    Block[B1] - Block
        Predecessors: [B0]
        Statements (3)
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'c1')
              Value: 
                IParameterReferenceOperation: c1 (OperationKind.ParameterReference, Type: C1) (Syntax: 'c1')

            IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'new C1(d) { ... b ? 2 : 3 }')
              Value: 
                IDynamicObjectCreationOperation (OperationKind.DynamicObjectCreation, Type: C1) (Syntax: 'new C1(d) { ... b ? 2 : 3 }')
                  Arguments(1):
                      IParameterReferenceOperation: d (OperationKind.ParameterReference, Type: dynamic) (Syntax: 'd')
                  ArgumentNames(0)
                  ArgumentRefKinds(0)
                  Initializer: 
                    null

            IInvocationOperation ( void C1.Add(System.Int32 i)) (OperationKind.Invocation, Type: System.Void, IsImplicit) (Syntax: '1')
              Instance Receiver: 
                IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: C1, IsImplicit) (Syntax: 'new C1(d) { ... b ? 2 : 3 }')
              Arguments(1):
                  IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: i) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: '1')
                    ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
                    InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)

        Next (Regular) Block[B2]
            Entering: {R2}

    .locals {R2}
    {
        CaptureIds: [2]
        Block[B2] - Block
            Predecessors: [B1]
            Statements (0)
            Jump if False (Regular) to Block[B4]
                IParameterReferenceOperation: b (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'b')

            Next (Regular) Block[B3]
        Block[B3] - Block
            Predecessors: [B2]
            Statements (1)
                IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: '2')
                  Value: 
                    ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 2) (Syntax: '2')

            Next (Regular) Block[B5]
        Block[B4] - Block
            Predecessors: [B2]
            Statements (1)
                IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: '3')
                  Value: 
                    ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 3) (Syntax: '3')

            Next (Regular) Block[B5]
        Block[B5] - Block
            Predecessors: [B3] [B4]
            Statements (1)
                IInvocationOperation ( void C1.Add(System.Int32 i)) (OperationKind.Invocation, Type: System.Void, IsImplicit) (Syntax: 'b ? 2 : 3')
                  Instance Receiver: 
                    IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: C1, IsImplicit) (Syntax: 'new C1(d) { ... b ? 2 : 3 }')
                  Arguments(1):
                      IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: i) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 'b ? 2 : 3')
                        IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'b ? 2 : 3')
                        InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)

            Next (Regular) Block[B6]
                Leaving: {R2}
    }

    Block[B6] - Block
        Predecessors: [B5]
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'c1 = new C1 ...  ? 2 : 3 };')
              Expression: 
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: C1) (Syntax: 'c1 = new C1 ... b ? 2 : 3 }')
                  Left: 
                    IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: C1, IsImplicit) (Syntax: 'c1')
                  Right: 
                    IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: C1, IsImplicit) (Syntax: 'new C1(d) { ... b ? 2 : 3 }')

        Next (Regular) Block[B7]
            Leaving: {R1}
}

Block[B7] - Exit
    Predecessors: [B6]
    Statements (0)
";
            VerifyFlowGraphAndDiagnosticsForTest<MethodDeclarationSyntax>(source, expectedFlowGraph, expectedDiagnostics);
        }


    }
}
