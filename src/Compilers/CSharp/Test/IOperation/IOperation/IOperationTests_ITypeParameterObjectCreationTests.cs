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
        public void TypeParameterObjectCreation_Simple()
        {
            string source = @"
class C1
{
    void M<T>(T t1) where T : C1, new()
    {
        t1 = /*<bind>*/new T()/*</bind>*/;
    }
}
";
            var expectedDiagnostics = DiagnosticDescription.None;

            string expectedOperationTree = @"
ITypeParameterObjectCreationOperation (OperationKind.TypeParameterObjectCreation, Type: T) (Syntax: 'new T()')
  Initializer: 
    null
";
            VerifyOperationTreeAndDiagnosticsForTest<ObjectCreationExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void TypeParameterObjectCreation_WithObjectInitializer()
        {
            string source = @"
class C1
{
    void M<T>(T t1) where T : C1, new()
    {
        t1 = /*<bind>*/new T() { I = 1 }/*</bind>*/;
    }
    int I { get; set; }
}
";
            var expectedDiagnostics = DiagnosticDescription.None;

            string expectedOperationTree = @"
ITypeParameterObjectCreationOperation (OperationKind.TypeParameterObjectCreation, Type: T) (Syntax: 'new T() { I = 1 }')
  Initializer: 
    IObjectOrCollectionInitializerOperation (OperationKind.ObjectOrCollectionInitializer, Type: T) (Syntax: '{ I = 1 }')
      Initializers(1):
          ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32) (Syntax: 'I = 1')
            Left: 
              IPropertyReferenceOperation: System.Int32 C1.I { get; set; } (OperationKind.PropertyReference, Type: System.Int32) (Syntax: 'I')
                Instance Receiver: 
                  IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: T, IsImplicit) (Syntax: 'I')
            Right: 
              ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
";
            VerifyOperationTreeAndDiagnosticsForTest<ObjectCreationExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void TypeParameterObjectCreation_WithCollectionInitializer()
        {
            string source = @"
using System.Collections.Generic;

class C1
{
    void M<T>(T t1) where T : C1, IEnumerable<int>, new()
    {
        t1 = /*<bind>*/new T() { 1 }/*</bind>*/;
    }
    void Add(int i) { }
}
";
            var expectedDiagnostics = DiagnosticDescription.None;

            string expectedOperationTree = @"
ITypeParameterObjectCreationOperation (OperationKind.TypeParameterObjectCreation, Type: T) (Syntax: 'new T() { 1 }')
  Initializer: 
    IObjectOrCollectionInitializerOperation (OperationKind.ObjectOrCollectionInitializer, Type: T) (Syntax: '{ 1 }')
      Initializers(1):
          IInvocationOperation ( void C1.Add(System.Int32 i)) (OperationKind.Invocation, Type: System.Void, IsImplicit) (Syntax: '1')
            Instance Receiver: 
              IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: T, IsImplicit) (Syntax: 'T')
            Arguments(1):
                IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: i) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: '1')
                  ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
                  InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
";
            VerifyOperationTreeAndDiagnosticsForTest<ObjectCreationExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void TypeParameterObjectCreation_NoNewConstraint()
        {
            string source = @"
using System.Collections;
using System.Collections.Generic;

class C1
{
    void M<T>(T t1, bool b) where T : C1, IEnumerable<int>
    {
        t1 = /*<bind>*/new T() { 1, b ? 2 : 3 }/*</bind>*/;
    }
    void Add(int i) { }
}
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS0304: Cannot create an instance of the variable type 'T' because it does not have the new() constraint
                //         t1 = /*<bind>*/new T() { 1, b ? 2 : 3 }/*</bind>*/;
                Diagnostic(ErrorCode.ERR_NoNewTyvar, "new T() { 1, b ? 2 : 3 }").WithArguments("T").WithLocation(9, 24)
            };

            // Asserts that no ITypeParameterObjectCreationOperation is generated in this tree
            string expectedOperationTree = @"
IInvalidOperation (OperationKind.Invalid, Type: T, IsInvalid) (Syntax: 'new T() { 1, b ? 2 : 3 }')
  Children(1):
      IObjectOrCollectionInitializerOperation (OperationKind.ObjectOrCollectionInitializer, Type: T, IsInvalid) (Syntax: '{ 1, b ? 2 : 3 }')
        Initializers(2):
            IInvocationOperation ( void C1.Add(System.Int32 i)) (OperationKind.Invocation, Type: System.Void, IsInvalid, IsImplicit) (Syntax: '1')
              Instance Receiver: 
                IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: T, IsInvalid, IsImplicit) (Syntax: 'T')
              Arguments(1):
                  IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: i) (OperationKind.Argument, Type: null, IsInvalid, IsImplicit) (Syntax: '1')
                    ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1, IsInvalid) (Syntax: '1')
                    InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
            IInvocationOperation ( void C1.Add(System.Int32 i)) (OperationKind.Invocation, Type: System.Void, IsInvalid, IsImplicit) (Syntax: 'b ? 2 : 3')
              Instance Receiver: 
                IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: T, IsInvalid, IsImplicit) (Syntax: 'T')
              Arguments(1):
                  IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: i) (OperationKind.Argument, Type: null, IsInvalid, IsImplicit) (Syntax: 'b ? 2 : 3')
                    IConditionalOperation (OperationKind.Conditional, Type: System.Int32, IsInvalid) (Syntax: 'b ? 2 : 3')
                      Condition: 
                        IParameterReferenceOperation: b (OperationKind.ParameterReference, Type: System.Boolean, IsInvalid) (Syntax: 'b')
                      WhenTrue: 
                        ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 2, IsInvalid) (Syntax: '2')
                      WhenFalse: 
                        ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 3, IsInvalid) (Syntax: '3')
                    InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
";
            VerifyOperationTreeAndDiagnosticsForTest<ObjectCreationExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void TypeParameterObjectCreationFlow_01()
        {
            string source = @"
using System.Collections;
using System.Collections.Generic;

class C1
{
    /*<bind>*/void M<T>(T t1, bool b) where T : C1, IEnumerable<int>, new()
    {
        t1 = new T() { 1, b ? 2 : 3 };
    }/*</bind>*/
    void Add(int i) { }
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
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 't1')
              Value: 
                IParameterReferenceOperation: t1 (OperationKind.ParameterReference, Type: T) (Syntax: 't1')

            IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'new T() { 1, b ? 2 : 3 }')
              Value: 
                ITypeParameterObjectCreationOperation (OperationKind.TypeParameterObjectCreation, Type: T) (Syntax: 'new T() { 1, b ? 2 : 3 }')
                  Initializer: 
                    null

            IInvocationOperation ( void C1.Add(System.Int32 i)) (OperationKind.Invocation, Type: System.Void, IsImplicit) (Syntax: '1')
              Instance Receiver: 
                IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: T, IsImplicit) (Syntax: 'new T() { 1, b ? 2 : 3 }')
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
                    IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: T, IsImplicit) (Syntax: 'new T() { 1, b ? 2 : 3 }')
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
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 't1 = new T( ...  ? 2 : 3 };')
              Expression: 
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: T) (Syntax: 't1 = new T( ... b ? 2 : 3 }')
                  Left: 
                    IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: T, IsImplicit) (Syntax: 't1')
                  Right: 
                    IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: T, IsImplicit) (Syntax: 'new T() { 1, b ? 2 : 3 }')

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
        public void TypeParameterObjectCreationFlow_02()
        {
            string source = @"
using System.Collections;
using System.Collections.Generic;

class C1
{
    /*<bind>*/void M<T>(T t1, bool b) where T : C1, new()
    {
        t1 = new T() { I1 = 1, I2 = b ? 2 : 3 };
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
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 't1')
              Value: 
                IParameterReferenceOperation: t1 (OperationKind.ParameterReference, Type: T) (Syntax: 't1')

            IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'new T() { I ... b ? 2 : 3 }')
              Value: 
                ITypeParameterObjectCreationOperation (OperationKind.TypeParameterObjectCreation, Type: T) (Syntax: 'new T() { I ... b ? 2 : 3 }')
                  Initializer: 
                    null

            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32) (Syntax: 'I1 = 1')
              Left: 
                IPropertyReferenceOperation: System.Int32 C1.I1 { get; set; } (OperationKind.PropertyReference, Type: System.Int32) (Syntax: 'I1')
                  Instance Receiver: 
                    IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: T, IsImplicit) (Syntax: 'new T() { I ... b ? 2 : 3 }')
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
                        IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: T, IsImplicit) (Syntax: 'new T() { I ... b ? 2 : 3 }')
                  Right: 
                    IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'b ? 2 : 3')

            Next (Regular) Block[B6]
                Leaving: {R2}
    }

    Block[B6] - Block
        Predecessors: [B5]
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 't1 = new T( ...  ? 2 : 3 };')
              Expression: 
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: T) (Syntax: 't1 = new T( ... b ? 2 : 3 }')
                  Left: 
                    IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: T, IsImplicit) (Syntax: 't1')
                  Right: 
                    IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: T, IsImplicit) (Syntax: 'new T() { I ... b ? 2 : 3 }')

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
        public void TypeParameterObjectCreationFlow_03()
        {
            string source = @"
using System.Collections;
using System.Collections.Generic;

class C1
{
    /*<bind>*/void M<T>(T t1, bool b) where T : C1, IEnumerable<int>, new()
    {
        t1 = new T() { 1, 2 };
    }/*</bind>*/
    void Add(int i) { }
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
        Statements (5)
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 't1')
              Value: 
                IParameterReferenceOperation: t1 (OperationKind.ParameterReference, Type: T) (Syntax: 't1')

            IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'new T() { 1, 2 }')
              Value: 
                ITypeParameterObjectCreationOperation (OperationKind.TypeParameterObjectCreation, Type: T) (Syntax: 'new T() { 1, 2 }')
                  Initializer: 
                    null

            IInvocationOperation ( void C1.Add(System.Int32 i)) (OperationKind.Invocation, Type: System.Void, IsImplicit) (Syntax: '1')
              Instance Receiver: 
                IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: T, IsImplicit) (Syntax: 'new T() { 1, 2 }')
              Arguments(1):
                  IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: i) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: '1')
                    ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
                    InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)

            IInvocationOperation ( void C1.Add(System.Int32 i)) (OperationKind.Invocation, Type: System.Void, IsImplicit) (Syntax: '2')
              Instance Receiver: 
                IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: T, IsImplicit) (Syntax: 'new T() { 1, 2 }')
              Arguments(1):
                  IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: i) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: '2')
                    ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 2) (Syntax: '2')
                    InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)

            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 't1 = new T() { 1, 2 };')
              Expression: 
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: T) (Syntax: 't1 = new T() { 1, 2 }')
                  Left: 
                    IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: T, IsImplicit) (Syntax: 't1')
                  Right: 
                    IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: T, IsImplicit) (Syntax: 'new T() { 1, 2 }')

        Next (Regular) Block[B2]
            Leaving: {R1}
}

Block[B2] - Exit
    Predecessors: [B1]
    Statements (0)
";
            VerifyFlowGraphAndDiagnosticsForTest<MethodDeclarationSyntax>(source, expectedFlowGraph, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void TypeParameterObjectCreationFlow_04()
        {
            string source = @"
using System.Collections;
using System.Collections.Generic;

class C1
{
    /*<bind>*/void M<T>(T t1, bool b) where T : C1, IEnumerable<int>
    {
        t1 = new T() { 1, 2 };
    }/*</bind>*/
    void Add(int i) { }
}
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS0304: Cannot create an instance of the variable type 'T' because it does not have the new() constraint
                //         t1 = /*<bind>*/new T() { 1, b ? 2 : 3 };
                Diagnostic(ErrorCode.ERR_NoNewTyvar, "new T() { 1, 2 }").WithArguments("T").WithLocation(9, 14)
            };

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
        Statements (5)
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 't1')
              Value: 
                IParameterReferenceOperation: t1 (OperationKind.ParameterReference, Type: T) (Syntax: 't1')

            IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsInvalid, IsImplicit) (Syntax: 'new T() { 1, 2 }')
              Value: 
                IInvalidOperation (OperationKind.Invalid, Type: T, IsInvalid) (Syntax: 'new T() { 1, 2 }')
                  Children(0)

            IInvocationOperation ( void C1.Add(System.Int32 i)) (OperationKind.Invocation, Type: System.Void, IsInvalid, IsImplicit) (Syntax: '1')
              Instance Receiver: 
                IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: T, IsInvalid, IsImplicit) (Syntax: 'new T() { 1, 2 }')
              Arguments(1):
                  IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: i) (OperationKind.Argument, Type: null, IsInvalid, IsImplicit) (Syntax: '1')
                    ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1, IsInvalid) (Syntax: '1')
                    InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)

            IInvocationOperation ( void C1.Add(System.Int32 i)) (OperationKind.Invocation, Type: System.Void, IsInvalid, IsImplicit) (Syntax: '2')
              Instance Receiver: 
                IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: T, IsInvalid, IsImplicit) (Syntax: 'new T() { 1, 2 }')
              Arguments(1):
                  IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: i) (OperationKind.Argument, Type: null, IsInvalid, IsImplicit) (Syntax: '2')
                    ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 2, IsInvalid) (Syntax: '2')
                    InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)

            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null, IsInvalid) (Syntax: 't1 = new T() { 1, 2 };')
              Expression: 
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: T, IsInvalid) (Syntax: 't1 = new T() { 1, 2 }')
                  Left: 
                    IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: T, IsImplicit) (Syntax: 't1')
                  Right: 
                    IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: T, IsInvalid, IsImplicit) (Syntax: 'new T() { 1, 2 }')

        Next (Regular) Block[B2]
            Leaving: {R1}
}

Block[B2] - Exit
    Predecessors: [B1]
    Statements (0)
";
            VerifyFlowGraphAndDiagnosticsForTest<MethodDeclarationSyntax>(source, expectedFlowGraph, expectedDiagnostics);
        }
    }
}
