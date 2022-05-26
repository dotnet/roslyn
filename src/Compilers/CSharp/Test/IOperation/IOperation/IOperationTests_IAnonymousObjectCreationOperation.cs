// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    public class IOperationTests_IAnonymousObjectCreationOperation : SemanticModelTestBase
    {
        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void AnonymousObjectCreation_NoControlFlow_01()
        {
            string source = @"
using System;

class C
{
    void M(object p, int i, int j)
    /*<bind>*/{
        p = new { a = i, b = j };
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
    CaptureIds: [0] [1] [2]
    Block[B1] - Block
        Predecessors: [B0]
        Statements (4)
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'p')
              Value: 
                IParameterReferenceOperation: p (OperationKind.ParameterReference, Type: System.Object) (Syntax: 'p')

            IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'i')
              Value: 
                IParameterReferenceOperation: i (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'i')

            IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'j')
              Value: 
                IParameterReferenceOperation: j (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'j')

            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'p = new { a ... i, b = j };')
              Expression: 
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Object) (Syntax: 'p = new { a = i, b = j }')
                  Left: 
                    IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Object, IsImplicit) (Syntax: 'p')
                  Right: 
                    IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Object, IsImplicit) (Syntax: 'new { a = i, b = j }')
                      Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
                        (ImplicitReference)
                      Operand: 
                        IAnonymousObjectCreationOperation (OperationKind.AnonymousObjectCreation, Type: <anonymous type: System.Int32 a, System.Int32 b>) (Syntax: 'new { a = i, b = j }')
                          Initializers(2):
                              ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32) (Syntax: 'a = i')
                                Left: 
                                  IPropertyReferenceOperation: System.Int32 <anonymous type: System.Int32 a, System.Int32 b>.a { get; } (OperationKind.PropertyReference, Type: System.Int32) (Syntax: 'a')
                                    Instance Receiver: 
                                      IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: <anonymous type: System.Int32 a, System.Int32 b>, IsImplicit) (Syntax: 'new { a = i, b = j }')
                                Right: 
                                  IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'i')
                              ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32) (Syntax: 'b = j')
                                Left: 
                                  IPropertyReferenceOperation: System.Int32 <anonymous type: System.Int32 a, System.Int32 b>.b { get; } (OperationKind.PropertyReference, Type: System.Int32) (Syntax: 'b')
                                    Instance Receiver: 
                                      IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: <anonymous type: System.Int32 a, System.Int32 b>, IsImplicit) (Syntax: 'new { a = i, b = j }')
                                Right: 
                                  IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'j')

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

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void AnonymousObjectCreation_NoControlFlow_02()
        {
            // Verify initializers that are not simple assignments.
            string source = @"
using System;

class B
{
    public int j = 0;
}

class C
{
    private B b = new B();
    void M(object p, int i)
    /*<bind>*/{
        p = new { i, b.j };
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
    CaptureIds: [0] [1] [2]
    Block[B1] - Block
        Predecessors: [B0]
        Statements (4)
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'p')
              Value: 
                IParameterReferenceOperation: p (OperationKind.ParameterReference, Type: System.Object) (Syntax: 'p')

            IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'i')
              Value: 
                IParameterReferenceOperation: i (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'i')

            IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'b.j')
              Value: 
                IFieldReferenceOperation: System.Int32 B.j (OperationKind.FieldReference, Type: System.Int32) (Syntax: 'b.j')
                  Instance Receiver: 
                    IFieldReferenceOperation: B C.b (OperationKind.FieldReference, Type: B) (Syntax: 'b')
                      Instance Receiver: 
                        IInstanceReferenceOperation (ReferenceKind: ContainingTypeInstance) (OperationKind.InstanceReference, Type: C, IsImplicit) (Syntax: 'b')

            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'p = new { i, b.j };')
              Expression: 
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Object) (Syntax: 'p = new { i, b.j }')
                  Left: 
                    IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Object, IsImplicit) (Syntax: 'p')
                  Right: 
                    IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Object, IsImplicit) (Syntax: 'new { i, b.j }')
                      Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
                        (ImplicitReference)
                      Operand: 
                        IAnonymousObjectCreationOperation (OperationKind.AnonymousObjectCreation, Type: <anonymous type: System.Int32 i, System.Int32 j>) (Syntax: 'new { i, b.j }')
                          Initializers(2):
                              ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32, IsImplicit) (Syntax: 'i')
                                Left: 
                                  IPropertyReferenceOperation: System.Int32 <anonymous type: System.Int32 i, System.Int32 j>.i { get; } (OperationKind.PropertyReference, Type: System.Int32, IsImplicit) (Syntax: 'i')
                                    Instance Receiver: 
                                      IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: <anonymous type: System.Int32 i, System.Int32 j>, IsImplicit) (Syntax: 'new { i, b.j }')
                                Right: 
                                  IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'i')
                              ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32, IsImplicit) (Syntax: 'b.j')
                                Left: 
                                  IPropertyReferenceOperation: System.Int32 <anonymous type: System.Int32 i, System.Int32 j>.j { get; } (OperationKind.PropertyReference, Type: System.Int32, IsImplicit) (Syntax: 'b.j')
                                    Instance Receiver: 
                                      IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: <anonymous type: System.Int32 i, System.Int32 j>, IsImplicit) (Syntax: 'new { i, b.j }')
                                Right: 
                                  IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'b.j')

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

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void AnonymousObjectCreation_NoControlFlow_03()
        {
            // Verify initializers that are mix of simple assignments and non-assignments.
            string source = @"
using System;

class C
{
    void M(object p, int a, int i)
    /*<bind>*/{
        p = new { a, b = i };
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
    CaptureIds: [0] [1] [2]
    Block[B1] - Block
        Predecessors: [B0]
        Statements (4)
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'p')
              Value: 
                IParameterReferenceOperation: p (OperationKind.ParameterReference, Type: System.Object) (Syntax: 'p')

            IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'a')
              Value: 
                IParameterReferenceOperation: a (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'a')

            IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'i')
              Value: 
                IParameterReferenceOperation: i (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'i')

            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'p = new { a, b = i };')
              Expression: 
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Object) (Syntax: 'p = new { a, b = i }')
                  Left: 
                    IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Object, IsImplicit) (Syntax: 'p')
                  Right: 
                    IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Object, IsImplicit) (Syntax: 'new { a, b = i }')
                      Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
                        (ImplicitReference)
                      Operand: 
                        IAnonymousObjectCreationOperation (OperationKind.AnonymousObjectCreation, Type: <anonymous type: System.Int32 a, System.Int32 b>) (Syntax: 'new { a, b = i }')
                          Initializers(2):
                              ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32, IsImplicit) (Syntax: 'a')
                                Left: 
                                  IPropertyReferenceOperation: System.Int32 <anonymous type: System.Int32 a, System.Int32 b>.a { get; } (OperationKind.PropertyReference, Type: System.Int32, IsImplicit) (Syntax: 'a')
                                    Instance Receiver: 
                                      IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: <anonymous type: System.Int32 a, System.Int32 b>, IsImplicit) (Syntax: 'new { a, b = i }')
                                Right: 
                                  IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'a')
                              ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32) (Syntax: 'b = i')
                                Left: 
                                  IPropertyReferenceOperation: System.Int32 <anonymous type: System.Int32 a, System.Int32 b>.b { get; } (OperationKind.PropertyReference, Type: System.Int32) (Syntax: 'b')
                                    Instance Receiver: 
                                      IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: <anonymous type: System.Int32 a, System.Int32 b>, IsImplicit) (Syntax: 'new { a, b = i }')
                                Right: 
                                  IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'i')

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

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void AnonymousObjectCreation_NoControlFlow_04()
        {
            // Verify anonymous object creation in query.
            string source = @"
using System;
using System.Collections.Generic;
using System.Linq;

class C
{
    void M(object p, List<int> a, List<string> b)
    /*<bind>*/{
        p = from x in a
            from y in b
            where x == 0
            select 0;
    }/*</bind>*/
}
";

            string expectedFlowGraph = @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
Block[B1] - Block
    Predecessors: [B0]
    Statements (1)
        IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'p = from x  ... select 0;')
          Expression: 
            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Object) (Syntax: 'p = from x  ... select 0')
              Left: 
                IParameterReferenceOperation: p (OperationKind.ParameterReference, Type: System.Object) (Syntax: 'p')
              Right: 
                IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Object, IsImplicit) (Syntax: 'from x in a ... select 0')
                  Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
                    (ImplicitReference)
                  Operand: 
                    ITranslatedQueryOperation (OperationKind.TranslatedQuery, Type: System.Collections.Generic.IEnumerable<System.Int32>) (Syntax: 'from x in a ... select 0')
                      Expression: 
                        IInvocationOperation (System.Collections.Generic.IEnumerable<System.Int32> System.Linq.Enumerable.Select<<anonymous type: System.Int32 x, System.String y>, System.Int32>(this System.Collections.Generic.IEnumerable<<anonymous type: System.Int32 x, System.String y>> source, System.Func<<anonymous type: System.Int32 x, System.String y>, System.Int32> selector)) (OperationKind.Invocation, Type: System.Collections.Generic.IEnumerable<System.Int32>, IsImplicit) (Syntax: 'select 0')
                          Instance Receiver: 
                            null
                          Arguments(2):
                              IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: source) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 'where x == 0')
                                IInvocationOperation (System.Collections.Generic.IEnumerable<<anonymous type: System.Int32 x, System.String y>> System.Linq.Enumerable.Where<<anonymous type: System.Int32 x, System.String y>>(this System.Collections.Generic.IEnumerable<<anonymous type: System.Int32 x, System.String y>> source, System.Func<<anonymous type: System.Int32 x, System.String y>, System.Boolean> predicate)) (OperationKind.Invocation, Type: System.Collections.Generic.IEnumerable<<anonymous type: System.Int32 x, System.String y>>, IsImplicit) (Syntax: 'where x == 0')
                                  Instance Receiver: 
                                    null
                                  Arguments(2):
                                      IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: source) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 'from y in b')
                                        IInvocationOperation (System.Collections.Generic.IEnumerable<<anonymous type: System.Int32 x, System.String y>> System.Linq.Enumerable.SelectMany<System.Int32, System.String, <anonymous type: System.Int32 x, System.String y>>(this System.Collections.Generic.IEnumerable<System.Int32> source, System.Func<System.Int32, System.Collections.Generic.IEnumerable<System.String>> collectionSelector, System.Func<System.Int32, System.String, <anonymous type: System.Int32 x, System.String y>> resultSelector)) (OperationKind.Invocation, Type: System.Collections.Generic.IEnumerable<<anonymous type: System.Int32 x, System.String y>>, IsImplicit) (Syntax: 'from y in b')
                                          Instance Receiver: 
                                            null
                                          Arguments(3):
                                              IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: source) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 'from x in a')
                                                IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Collections.Generic.IEnumerable<System.Int32>, IsImplicit) (Syntax: 'from x in a')
                                                  Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
                                                    (ImplicitReference)
                                                  Operand: 
                                                    IParameterReferenceOperation: a (OperationKind.ParameterReference, Type: System.Collections.Generic.List<System.Int32>) (Syntax: 'a')
                                                InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                                OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                              IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: collectionSelector) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 'b')
                                                IDelegateCreationOperation (OperationKind.DelegateCreation, Type: System.Func<System.Int32, System.Collections.Generic.IEnumerable<System.String>>, IsImplicit) (Syntax: 'b')
                                                  Target: 
                                                    IFlowAnonymousFunctionOperation (Symbol: lambda expression) (OperationKind.FlowAnonymousFunction, Type: null, IsImplicit) (Syntax: 'b')
                                                    {
                                                        Block[B0#A0] - Entry
                                                            Statements (0)
                                                            Next (Regular) Block[B1#A0]
                                                        Block[B1#A0] - Block
                                                            Predecessors: [B0#A0]
                                                            Statements (0)
                                                            Next (Return) Block[B2#A0]
                                                                IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Collections.Generic.IEnumerable<System.String>, IsImplicit) (Syntax: 'b')
                                                                  Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
                                                                    (ImplicitReference)
                                                                  Operand: 
                                                                    IParameterReferenceOperation: b (OperationKind.ParameterReference, Type: System.Collections.Generic.List<System.String>) (Syntax: 'b')
                                                        Block[B2#A0] - Exit
                                                            Predecessors: [B1#A0]
                                                            Statements (0)
                                                    }
                                                InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                                OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                              IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: resultSelector) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 'from y in b')
                                                IDelegateCreationOperation (OperationKind.DelegateCreation, Type: System.Func<System.Int32, System.String, <anonymous type: System.Int32 x, System.String y>>, IsImplicit) (Syntax: 'from y in b')
                                                  Target: 
                                                    IFlowAnonymousFunctionOperation (Symbol: lambda expression) (OperationKind.FlowAnonymousFunction, Type: null, IsImplicit) (Syntax: 'from y in b')
                                                    {
                                                        Block[B0#A1] - Entry
                                                            Statements (0)
                                                            Next (Regular) Block[B1#A1]
                                                                Entering: {R1#A1}

                                                        .locals {R1#A1}
                                                        {
                                                            CaptureIds: [0] [1]
                                                            Block[B1#A1] - Block
                                                                Predecessors: [B0#A1]
                                                                Statements (2)
                                                                    IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'from y in b')
                                                                      Value: 
                                                                        IParameterReferenceOperation: x (OperationKind.ParameterReference, Type: System.Int32, IsImplicit) (Syntax: 'from y in b')

                                                                    IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'from y in b')
                                                                      Value: 
                                                                        IParameterReferenceOperation: y (OperationKind.ParameterReference, Type: System.String, IsImplicit) (Syntax: 'from y in b')

                                                                Next (Return) Block[B2#A1]
                                                                    IAnonymousObjectCreationOperation (OperationKind.AnonymousObjectCreation, Type: <anonymous type: System.Int32 x, System.String y>, IsImplicit) (Syntax: 'from y in b')
                                                                      Initializers(2):
                                                                          ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32, IsImplicit) (Syntax: 'from y in b ... select 0')
                                                                            Left: 
                                                                              IPropertyReferenceOperation: System.Int32 <anonymous type: System.Int32 x, System.String y>.x { get; } (OperationKind.PropertyReference, Type: System.Int32, IsImplicit) (Syntax: 'from y in b')
                                                                                Instance Receiver: 
                                                                                  IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: <anonymous type: System.Int32 x, System.String y>, IsImplicit) (Syntax: 'from y in b')
                                                                            Right: 
                                                                              IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'from y in b')
                                                                          ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.String, IsImplicit) (Syntax: 'from y in b ... select 0')
                                                                            Left: 
                                                                              IPropertyReferenceOperation: System.String <anonymous type: System.Int32 x, System.String y>.y { get; } (OperationKind.PropertyReference, Type: System.String, IsImplicit) (Syntax: 'from y in b')
                                                                                Instance Receiver: 
                                                                                  IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: <anonymous type: System.Int32 x, System.String y>, IsImplicit) (Syntax: 'from y in b')
                                                                            Right: 
                                                                              IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.String, IsImplicit) (Syntax: 'from y in b')
                                                                    Leaving: {R1#A1}
                                                        }

                                                        Block[B2#A1] - Exit
                                                            Predecessors: [B1#A1]
                                                            Statements (0)
                                                    }
                                                InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                                OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                        InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                        OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                      IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: predicate) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 'x == 0')
                                        IDelegateCreationOperation (OperationKind.DelegateCreation, Type: System.Func<<anonymous type: System.Int32 x, System.String y>, System.Boolean>, IsImplicit) (Syntax: 'x == 0')
                                          Target: 
                                            IFlowAnonymousFunctionOperation (Symbol: lambda expression) (OperationKind.FlowAnonymousFunction, Type: null, IsImplicit) (Syntax: 'x == 0')
                                            {
                                                Block[B0#A2] - Entry
                                                    Statements (0)
                                                    Next (Regular) Block[B1#A2]
                                                Block[B1#A2] - Block
                                                    Predecessors: [B0#A2]
                                                    Statements (0)
                                                    Next (Return) Block[B2#A2]
                                                        IBinaryOperation (BinaryOperatorKind.Equals) (OperationKind.Binary, Type: System.Boolean) (Syntax: 'x == 0')
                                                          Left: 
                                                            IPropertyReferenceOperation: System.Int32 <anonymous type: System.Int32 x, System.String y>.x { get; } (OperationKind.PropertyReference, Type: System.Int32) (Syntax: 'x')
                                                              Instance Receiver: 
                                                                IParameterReferenceOperation: <>h__TransparentIdentifier0 (OperationKind.ParameterReference, Type: <anonymous type: System.Int32 x, System.String y>, IsImplicit) (Syntax: 'x')
                                                          Right: 
                                                            ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 0) (Syntax: '0')
                                                Block[B2#A2] - Exit
                                                    Predecessors: [B1#A2]
                                                    Statements (0)
                                            }
                                        InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                        OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                              IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: selector) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: '0')
                                IDelegateCreationOperation (OperationKind.DelegateCreation, Type: System.Func<<anonymous type: System.Int32 x, System.String y>, System.Int32>, IsImplicit) (Syntax: '0')
                                  Target: 
                                    IFlowAnonymousFunctionOperation (Symbol: lambda expression) (OperationKind.FlowAnonymousFunction, Type: null, IsImplicit) (Syntax: '0')
                                    {
                                        Block[B0#A3] - Entry
                                            Statements (0)
                                            Next (Regular) Block[B1#A3]
                                        Block[B1#A3] - Block
                                            Predecessors: [B0#A3]
                                            Statements (0)
                                            Next (Return) Block[B2#A3]
                                                ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 0) (Syntax: '0')
                                        Block[B2#A3] - Exit
                                            Predecessors: [B1#A3]
                                            Statements (0)
                                    }
                                InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)

    Next (Regular) Block[B2]
Block[B2] - Exit
    Predecessors: [B1]
    Statements (0)
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(source, expectedFlowGraph, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void AnonymousObjectCreation_NoControlFlow_05()
        {
            // Verify anonymous object creation in query with transparent identifiers.
            string source = @"
using System;
using System.Collections.Generic;
using System.Linq;

class C
{
    void M(object p, List<int> a)
    /*<bind>*/{
        p = from x in a
            let y = x
            where x == 0
            select 0;
    }/*</bind>*/
}
";
            string expectedFlowGraph = @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
Block[B1] - Block
    Predecessors: [B0]
    Statements (1)
        IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'p = from x  ... select 0;')
          Expression: 
            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Object) (Syntax: 'p = from x  ... select 0')
              Left: 
                IParameterReferenceOperation: p (OperationKind.ParameterReference, Type: System.Object) (Syntax: 'p')
              Right: 
                IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Object, IsImplicit) (Syntax: 'from x in a ... select 0')
                  Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
                    (ImplicitReference)
                  Operand: 
                    ITranslatedQueryOperation (OperationKind.TranslatedQuery, Type: System.Collections.Generic.IEnumerable<System.Int32>) (Syntax: 'from x in a ... select 0')
                      Expression: 
                        IInvocationOperation (System.Collections.Generic.IEnumerable<System.Int32> System.Linq.Enumerable.Select<<anonymous type: System.Int32 x, System.Int32 y>, System.Int32>(this System.Collections.Generic.IEnumerable<<anonymous type: System.Int32 x, System.Int32 y>> source, System.Func<<anonymous type: System.Int32 x, System.Int32 y>, System.Int32> selector)) (OperationKind.Invocation, Type: System.Collections.Generic.IEnumerable<System.Int32>, IsImplicit) (Syntax: 'select 0')
                          Instance Receiver: 
                            null
                          Arguments(2):
                              IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: source) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 'where x == 0')
                                IInvocationOperation (System.Collections.Generic.IEnumerable<<anonymous type: System.Int32 x, System.Int32 y>> System.Linq.Enumerable.Where<<anonymous type: System.Int32 x, System.Int32 y>>(this System.Collections.Generic.IEnumerable<<anonymous type: System.Int32 x, System.Int32 y>> source, System.Func<<anonymous type: System.Int32 x, System.Int32 y>, System.Boolean> predicate)) (OperationKind.Invocation, Type: System.Collections.Generic.IEnumerable<<anonymous type: System.Int32 x, System.Int32 y>>, IsImplicit) (Syntax: 'where x == 0')
                                  Instance Receiver: 
                                    null
                                  Arguments(2):
                                      IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: source) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 'let y = x')
                                        IInvocationOperation (System.Collections.Generic.IEnumerable<<anonymous type: System.Int32 x, System.Int32 y>> System.Linq.Enumerable.Select<System.Int32, <anonymous type: System.Int32 x, System.Int32 y>>(this System.Collections.Generic.IEnumerable<System.Int32> source, System.Func<System.Int32, <anonymous type: System.Int32 x, System.Int32 y>> selector)) (OperationKind.Invocation, Type: System.Collections.Generic.IEnumerable<<anonymous type: System.Int32 x, System.Int32 y>>, IsImplicit) (Syntax: 'let y = x')
                                          Instance Receiver: 
                                            null
                                          Arguments(2):
                                              IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: source) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 'from x in a')
                                                IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Collections.Generic.IEnumerable<System.Int32>, IsImplicit) (Syntax: 'from x in a')
                                                  Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
                                                    (ImplicitReference)
                                                  Operand: 
                                                    IParameterReferenceOperation: a (OperationKind.ParameterReference, Type: System.Collections.Generic.List<System.Int32>) (Syntax: 'a')
                                                InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                                OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                              IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: selector) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 'x')
                                                IDelegateCreationOperation (OperationKind.DelegateCreation, Type: System.Func<System.Int32, <anonymous type: System.Int32 x, System.Int32 y>>, IsImplicit) (Syntax: 'x')
                                                  Target: 
                                                    IFlowAnonymousFunctionOperation (Symbol: lambda expression) (OperationKind.FlowAnonymousFunction, Type: null, IsImplicit) (Syntax: 'x')
                                                    {
                                                        Block[B0#A0] - Entry
                                                            Statements (0)
                                                            Next (Regular) Block[B1#A0]
                                                                Entering: {R1#A0}

                                                        .locals {R1#A0}
                                                        {
                                                            CaptureIds: [0] [1]
                                                            Block[B1#A0] - Block
                                                                Predecessors: [B0#A0]
                                                                Statements (2)
                                                                    IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'let y = x')
                                                                      Value: 
                                                                        IParameterReferenceOperation: x (OperationKind.ParameterReference, Type: System.Int32, IsImplicit) (Syntax: 'let y = x')

                                                                    IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'x')
                                                                      Value: 
                                                                        IParameterReferenceOperation: x (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'x')

                                                                Next (Return) Block[B2#A0]
                                                                    IAnonymousObjectCreationOperation (OperationKind.AnonymousObjectCreation, Type: <anonymous type: System.Int32 x, System.Int32 y>, IsImplicit) (Syntax: 'let y = x')
                                                                      Initializers(2):
                                                                          ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32, IsImplicit) (Syntax: 'let y = x ... select 0')
                                                                            Left: 
                                                                              IPropertyReferenceOperation: System.Int32 <anonymous type: System.Int32 x, System.Int32 y>.x { get; } (OperationKind.PropertyReference, Type: System.Int32, IsImplicit) (Syntax: 'let y = x')
                                                                                Instance Receiver: 
                                                                                  IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: <anonymous type: System.Int32 x, System.Int32 y>, IsImplicit) (Syntax: 'let y = x')
                                                                            Right: 
                                                                              IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'let y = x')
                                                                          ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32, IsImplicit) (Syntax: 'let y = x')
                                                                            Left: 
                                                                              IPropertyReferenceOperation: System.Int32 <anonymous type: System.Int32 x, System.Int32 y>.y { get; } (OperationKind.PropertyReference, Type: System.Int32, IsImplicit) (Syntax: 'x')
                                                                                Instance Receiver: 
                                                                                  IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: <anonymous type: System.Int32 x, System.Int32 y>, IsImplicit) (Syntax: 'let y = x')
                                                                            Right: 
                                                                              IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'x')
                                                                    Leaving: {R1#A0}
                                                        }

                                                        Block[B2#A0] - Exit
                                                            Predecessors: [B1#A0]
                                                            Statements (0)
                                                    }
                                                InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                                OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                        InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                        OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                      IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: predicate) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 'x == 0')
                                        IDelegateCreationOperation (OperationKind.DelegateCreation, Type: System.Func<<anonymous type: System.Int32 x, System.Int32 y>, System.Boolean>, IsImplicit) (Syntax: 'x == 0')
                                          Target: 
                                            IFlowAnonymousFunctionOperation (Symbol: lambda expression) (OperationKind.FlowAnonymousFunction, Type: null, IsImplicit) (Syntax: 'x == 0')
                                            {
                                                Block[B0#A1] - Entry
                                                    Statements (0)
                                                    Next (Regular) Block[B1#A1]
                                                Block[B1#A1] - Block
                                                    Predecessors: [B0#A1]
                                                    Statements (0)
                                                    Next (Return) Block[B2#A1]
                                                        IBinaryOperation (BinaryOperatorKind.Equals) (OperationKind.Binary, Type: System.Boolean) (Syntax: 'x == 0')
                                                          Left: 
                                                            IPropertyReferenceOperation: System.Int32 <anonymous type: System.Int32 x, System.Int32 y>.x { get; } (OperationKind.PropertyReference, Type: System.Int32) (Syntax: 'x')
                                                              Instance Receiver: 
                                                                IParameterReferenceOperation: <>h__TransparentIdentifier0 (OperationKind.ParameterReference, Type: <anonymous type: System.Int32 x, System.Int32 y>, IsImplicit) (Syntax: 'x')
                                                          Right: 
                                                            ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 0) (Syntax: '0')
                                                Block[B2#A1] - Exit
                                                    Predecessors: [B1#A1]
                                                    Statements (0)
                                            }
                                        InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                        OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                              IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: selector) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: '0')
                                IDelegateCreationOperation (OperationKind.DelegateCreation, Type: System.Func<<anonymous type: System.Int32 x, System.Int32 y>, System.Int32>, IsImplicit) (Syntax: '0')
                                  Target: 
                                    IFlowAnonymousFunctionOperation (Symbol: lambda expression) (OperationKind.FlowAnonymousFunction, Type: null, IsImplicit) (Syntax: '0')
                                    {
                                        Block[B0#A2] - Entry
                                            Statements (0)
                                            Next (Regular) Block[B1#A2]
                                        Block[B1#A2] - Block
                                            Predecessors: [B0#A2]
                                            Statements (0)
                                            Next (Return) Block[B2#A2]
                                                ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 0) (Syntax: '0')
                                        Block[B2#A2] - Exit
                                            Predecessors: [B1#A2]
                                            Statements (0)
                                    }
                                InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)

    Next (Regular) Block[B2]
Block[B2] - Exit
    Predecessors: [B1]
    Statements (0)
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(source, expectedFlowGraph, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void AnonymousObjectCreation_NoControlFlow_06()
        {
            // Verify anonymous object creation nested in object creation initializer.
            string source = @"
using System;

class C
{
    public int a = 1;
    public object b;
    void M(object p, int i1, int i2, int i3)
    /*<bind>*/{
        p = new C() { a = i1, b = new { a = i2, b = i3 }};
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
    CaptureIds: [0] [1]
    Block[B1] - Block
        Predecessors: [B0]
        Statements (3)
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'p')
              Value: 
                IParameterReferenceOperation: p (OperationKind.ParameterReference, Type: System.Object) (Syntax: 'p')

            IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'new C() { a ... , b = i3 }}')
              Value: 
                IObjectCreationOperation (Constructor: C..ctor()) (OperationKind.ObjectCreation, Type: C) (Syntax: 'new C() { a ... , b = i3 }}')
                  Arguments(0)
                  Initializer: 
                    null

            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32) (Syntax: 'a = i1')
              Left: 
                IFieldReferenceOperation: System.Int32 C.a (OperationKind.FieldReference, Type: System.Int32) (Syntax: 'a')
                  Instance Receiver: 
                    IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: C, IsImplicit) (Syntax: 'new C() { a ... , b = i3 }}')
              Right: 
                IParameterReferenceOperation: i1 (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'i1')

        Next (Regular) Block[B2]
            Entering: {R2}

    .locals {R2}
    {
        CaptureIds: [2] [3]
        Block[B2] - Block
            Predecessors: [B1]
            Statements (3)
                IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'i2')
                  Value: 
                    IParameterReferenceOperation: i2 (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'i2')

                IFlowCaptureOperation: 3 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'i3')
                  Value: 
                    IParameterReferenceOperation: i3 (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'i3')

                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Object) (Syntax: 'b = new { a ... 2, b = i3 }')
                  Left: 
                    IFieldReferenceOperation: System.Object C.b (OperationKind.FieldReference, Type: System.Object) (Syntax: 'b')
                      Instance Receiver: 
                        IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: C, IsImplicit) (Syntax: 'new C() { a ... , b = i3 }}')
                  Right: 
                    IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Object, IsImplicit) (Syntax: 'new { a = i2, b = i3 }')
                      Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
                        (ImplicitReference)
                      Operand: 
                        IAnonymousObjectCreationOperation (OperationKind.AnonymousObjectCreation, Type: <anonymous type: System.Int32 a, System.Int32 b>) (Syntax: 'new { a = i2, b = i3 }')
                          Initializers(2):
                              ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32) (Syntax: 'a = i2')
                                Left: 
                                  IPropertyReferenceOperation: System.Int32 <anonymous type: System.Int32 a, System.Int32 b>.a { get; } (OperationKind.PropertyReference, Type: System.Int32) (Syntax: 'a')
                                    Instance Receiver: 
                                      IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: <anonymous type: System.Int32 a, System.Int32 b>, IsImplicit) (Syntax: 'new { a = i2, b = i3 }')
                                Right: 
                                  IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'i2')
                              ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32) (Syntax: 'b = i3')
                                Left: 
                                  IPropertyReferenceOperation: System.Int32 <anonymous type: System.Int32 a, System.Int32 b>.b { get; } (OperationKind.PropertyReference, Type: System.Int32) (Syntax: 'b')
                                    Instance Receiver: 
                                      IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: <anonymous type: System.Int32 a, System.Int32 b>, IsImplicit) (Syntax: 'new { a = i2, b = i3 }')
                                Right: 
                                  IFlowCaptureReferenceOperation: 3 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'i3')

            Next (Regular) Block[B3]
                Leaving: {R2}
    }

    Block[B3] - Block
        Predecessors: [B2]
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'p = new C() ...  b = i3 }};')
              Expression: 
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Object) (Syntax: 'p = new C() ... , b = i3 }}')
                  Left: 
                    IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Object, IsImplicit) (Syntax: 'p')
                  Right: 
                    IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Object, IsImplicit) (Syntax: 'new C() { a ... , b = i3 }}')
                      Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
                        (ImplicitReference)
                      Operand: 
                        IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: C, IsImplicit) (Syntax: 'new C() { a ... , b = i3 }}')

        Next (Regular) Block[B4]
            Leaving: {R1}
}

Block[B4] - Exit
    Predecessors: [B3]
    Statements (0)
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(source, expectedFlowGraph, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void AnonymousObjectCreation_NoControlFlow_07()
        {
            // Verify anonymous object creation nested in anonymous object creation initializer.
            string source = @"
using System;

class C
{
    void M(object p, int i1, int i2, int i3)
    /*<bind>*/{
        p = new { a = i1, b = new { a = i2, b = i3 }};
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
    CaptureIds: [0] [1] [4]
    Block[B1] - Block
        Predecessors: [B0]
        Statements (2)
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'p')
              Value: 
                IParameterReferenceOperation: p (OperationKind.ParameterReference, Type: System.Object) (Syntax: 'p')

            IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'i1')
              Value: 
                IParameterReferenceOperation: i1 (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'i1')

        Next (Regular) Block[B2]
            Entering: {R2}

    .locals {R2}
    {
        CaptureIds: [2] [3]
        Block[B2] - Block
            Predecessors: [B1]
            Statements (3)
                IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'i2')
                  Value: 
                    IParameterReferenceOperation: i2 (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'i2')

                IFlowCaptureOperation: 3 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'i3')
                  Value: 
                    IParameterReferenceOperation: i3 (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'i3')

                IFlowCaptureOperation: 4 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'new { a = i2, b = i3 }')
                  Value: 
                    IAnonymousObjectCreationOperation (OperationKind.AnonymousObjectCreation, Type: <anonymous type: System.Int32 a, System.Int32 b>) (Syntax: 'new { a = i2, b = i3 }')
                      Initializers(2):
                          ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32) (Syntax: 'a = i2')
                            Left: 
                              IPropertyReferenceOperation: System.Int32 <anonymous type: System.Int32 a, System.Int32 b>.a { get; } (OperationKind.PropertyReference, Type: System.Int32) (Syntax: 'a')
                                Instance Receiver: 
                                  IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: <anonymous type: System.Int32 a, System.Int32 b>, IsImplicit) (Syntax: 'new { a = i2, b = i3 }')
                            Right: 
                              IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'i2')
                          ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32) (Syntax: 'b = i3')
                            Left: 
                              IPropertyReferenceOperation: System.Int32 <anonymous type: System.Int32 a, System.Int32 b>.b { get; } (OperationKind.PropertyReference, Type: System.Int32) (Syntax: 'b')
                                Instance Receiver: 
                                  IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: <anonymous type: System.Int32 a, System.Int32 b>, IsImplicit) (Syntax: 'new { a = i2, b = i3 }')
                            Right: 
                              IFlowCaptureReferenceOperation: 3 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'i3')

            Next (Regular) Block[B3]
                Leaving: {R2}
    }

    Block[B3] - Block
        Predecessors: [B2]
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'p = new { a ...  b = i3 }};')
              Expression: 
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Object) (Syntax: 'p = new { a ... , b = i3 }}')
                  Left: 
                    IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Object, IsImplicit) (Syntax: 'p')
                  Right: 
                    IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Object, IsImplicit) (Syntax: 'new { a = i ... , b = i3 }}')
                      Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
                        (ImplicitReference)
                      Operand: 
                        IAnonymousObjectCreationOperation (OperationKind.AnonymousObjectCreation, Type: <anonymous type: System.Int32 a, <anonymous type: System.Int32 a, System.Int32 b> b>) (Syntax: 'new { a = i ... , b = i3 }}')
                          Initializers(2):
                              ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32) (Syntax: 'a = i1')
                                Left: 
                                  IPropertyReferenceOperation: System.Int32 <anonymous type: System.Int32 a, <anonymous type: System.Int32 a, System.Int32 b> b>.a { get; } (OperationKind.PropertyReference, Type: System.Int32) (Syntax: 'a')
                                    Instance Receiver: 
                                      IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: <anonymous type: System.Int32 a, <anonymous type: System.Int32 a, System.Int32 b> b>, IsImplicit) (Syntax: 'new { a = i ... , b = i3 }}')
                                Right: 
                                  IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'i1')
                              ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: <anonymous type: System.Int32 a, System.Int32 b>) (Syntax: 'b = new { a ... 2, b = i3 }')
                                Left: 
                                  IPropertyReferenceOperation: <anonymous type: System.Int32 a, System.Int32 b> <anonymous type: System.Int32 a, <anonymous type: System.Int32 a, System.Int32 b> b>.b { get; } (OperationKind.PropertyReference, Type: <anonymous type: System.Int32 a, System.Int32 b>) (Syntax: 'b')
                                    Instance Receiver: 
                                      IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: <anonymous type: System.Int32 a, <anonymous type: System.Int32 a, System.Int32 b> b>, IsImplicit) (Syntax: 'new { a = i ... , b = i3 }}')
                                Right: 
                                  IFlowCaptureReferenceOperation: 4 (OperationKind.FlowCaptureReference, Type: <anonymous type: System.Int32 a, System.Int32 b>, IsImplicit) (Syntax: 'new { a = i2, b = i3 }')

        Next (Regular) Block[B4]
            Leaving: {R1}
}

Block[B4] - Exit
    Predecessors: [B3]
    Statements (0)
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(source, expectedFlowGraph, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void AnonymousObjectCreation_NoControlFlow_08()
        {
            // Verify anonymous object creation with no initializers.
            string source = @"
using System;

class C
{
    void M(object p)
    /*<bind>*/{
        p = new { };
    }/*</bind>*/
}
";
            string expectedFlowGraph = @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
Block[B1] - Block
    Predecessors: [B0]
    Statements (1)
        IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'p = new { };')
          Expression: 
            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Object) (Syntax: 'p = new { }')
              Left: 
                IParameterReferenceOperation: p (OperationKind.ParameterReference, Type: System.Object) (Syntax: 'p')
              Right: 
                IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Object, IsImplicit) (Syntax: 'new { }')
                  Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
                    (ImplicitReference)
                  Operand: 
                    IAnonymousObjectCreationOperation (OperationKind.AnonymousObjectCreation, Type: <empty anonymous type>) (Syntax: 'new { }')
                      Initializers(0)

    Next (Regular) Block[B2]
Block[B2] - Exit
    Predecessors: [B1]
    Statements (0)
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(source, expectedFlowGraph, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void AnonymousObjectCreation_NoControlFlow_Error01()
        {
            // Duplicate property name, ensure we have same number of initializers as properties.
            string source = @"
using System;

class C
{
    void M(object p, int i)
    /*<bind>*/{
        p = new { i, i };
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
    CaptureIds: [0] [1] [2]
    Block[B1] - Block
        Predecessors: [B0]
        Statements (4)
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'p')
              Value: 
                IParameterReferenceOperation: p (OperationKind.ParameterReference, Type: System.Object) (Syntax: 'p')

            IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'i')
              Value: 
                IParameterReferenceOperation: i (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'i')

            IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsInvalid, IsImplicit) (Syntax: 'i')
              Value: 
                IParameterReferenceOperation: i (OperationKind.ParameterReference, Type: System.Int32, IsInvalid) (Syntax: 'i')

            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null, IsInvalid) (Syntax: 'p = new { i, i };')
              Expression: 
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Object, IsInvalid) (Syntax: 'p = new { i, i }')
                  Left: 
                    IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Object, IsImplicit) (Syntax: 'p')
                  Right: 
                    IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Object, IsInvalid, IsImplicit) (Syntax: 'new { i, i }')
                      Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
                        (ImplicitReference)
                      Operand: 
                        IAnonymousObjectCreationOperation (OperationKind.AnonymousObjectCreation, Type: <anonymous type: System.Int32 i, System.Int32 $1>, IsInvalid) (Syntax: 'new { i, i }')
                          Initializers(2):
                              ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32, IsImplicit) (Syntax: 'i')
                                Left: 
                                  IPropertyReferenceOperation: System.Int32 <anonymous type: System.Int32 i, System.Int32 $1>.i { get; } (OperationKind.PropertyReference, Type: System.Int32, IsImplicit) (Syntax: 'i')
                                    Instance Receiver: 
                                      IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: <anonymous type: System.Int32 i, System.Int32 $1>, IsInvalid, IsImplicit) (Syntax: 'new { i, i }')
                                Right: 
                                  IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'i')
                              ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32, IsInvalid, IsImplicit) (Syntax: 'i')
                                Left: 
                                  IPropertyReferenceOperation: System.Int32 <anonymous type: System.Int32 i, System.Int32 $1>.$1 { get; } (OperationKind.PropertyReference, Type: System.Int32, IsInvalid, IsImplicit) (Syntax: 'i')
                                    Instance Receiver: 
                                      IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: <anonymous type: System.Int32 i, System.Int32 $1>, IsInvalid, IsImplicit) (Syntax: 'new { i, i }')
                                Right: 
                                  IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: System.Int32, IsInvalid, IsImplicit) (Syntax: 'i')

        Next (Regular) Block[B2]
            Leaving: {R1}
}

Block[B2] - Exit
    Predecessors: [B1]
    Statements (0)
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // file.cs(8,22): error CS0833: An anonymous type cannot have multiple properties with the same name
                //         p = new { i, i };
                Diagnostic(ErrorCode.ERR_AnonymousTypeDuplicatePropertyName, "i").WithLocation(8, 22)
            };

            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(source, expectedFlowGraph, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void AnonymousObjectCreation_NoControlFlow_Error02()
        {
            // Missing value for property assignment.
            string source = @"
using System;

class C
{
    void M(object p)
    /*<bind>*/{
        p = new { a = };
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
    CaptureIds: [0] [1]
    Block[B1] - Block
        Predecessors: [B0]
        Statements (3)
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'p')
              Value: 
                IParameterReferenceOperation: p (OperationKind.ParameterReference, Type: System.Object) (Syntax: 'p')

            IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsInvalid, IsImplicit) (Syntax: '')
              Value: 
                IInvalidOperation (OperationKind.Invalid, Type: null, IsInvalid) (Syntax: '')
                  Children(0)

            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null, IsInvalid) (Syntax: 'p = new { a = };')
              Expression: 
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Object, IsInvalid) (Syntax: 'p = new { a = }')
                  Left: 
                    IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Object, IsImplicit) (Syntax: 'p')
                  Right: 
                    IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Object, IsInvalid, IsImplicit) (Syntax: 'new { a = }')
                      Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
                        (ImplicitReference)
                      Operand: 
                        IAnonymousObjectCreationOperation (OperationKind.AnonymousObjectCreation, Type: <anonymous type: ? a>, IsInvalid) (Syntax: 'new { a = }')
                          Initializers(1):
                              ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: ?, IsInvalid) (Syntax: 'a = ')
                                Left: 
                                  IPropertyReferenceOperation: ? <anonymous type: ? a>.a { get; } (OperationKind.PropertyReference, Type: ?) (Syntax: 'a')
                                    Instance Receiver: 
                                      IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: <anonymous type: ? a>, IsInvalid, IsImplicit) (Syntax: 'new { a = }')
                                Right: 
                                  IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: null, IsInvalid, IsImplicit) (Syntax: '')

        Next (Regular) Block[B2]
            Leaving: {R1}
}

Block[B2] - Exit
    Predecessors: [B1]
    Statements (0)
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // file.cs(8,23): error CS1525: Invalid expression term '}'
                //         p = new { a = };
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, "}").WithArguments("}").WithLocation(8, 23)
            };

            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(source, expectedFlowGraph, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void AnonymousObjectCreation_NoControlFlow_Error03()
        {
            // Invalid expression as initializer target, ensure we don't drop this expression from the flow graph.
            string source = @"
using System;

class C
{
    void M(object p, int i)
    /*<bind>*/{
        p = new { M2() = i };
    }/*</bind>*/

    int M2() => 0;
}
";
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
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'p')
              Value: 
                IParameterReferenceOperation: p (OperationKind.ParameterReference, Type: System.Object) (Syntax: 'p')

            IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsInvalid, IsImplicit) (Syntax: 'M2() = i')
              Value: 
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32, IsInvalid) (Syntax: 'M2() = i')
                  Left: 
                    IInvalidOperation (OperationKind.Invalid, Type: System.Int32, IsInvalid, IsImplicit) (Syntax: 'M2()')
                      Children(1):
                          IInvocationOperation ( System.Int32 C.M2()) (OperationKind.Invocation, Type: System.Int32, IsInvalid) (Syntax: 'M2()')
                            Instance Receiver: 
                              IInstanceReferenceOperation (ReferenceKind: ContainingTypeInstance) (OperationKind.InstanceReference, Type: C, IsInvalid, IsImplicit) (Syntax: 'M2')
                            Arguments(0)
                  Right: 
                    IParameterReferenceOperation: i (OperationKind.ParameterReference, Type: System.Int32, IsInvalid) (Syntax: 'i')

            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null, IsInvalid) (Syntax: 'p = new { M2() = i };')
              Expression: 
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Object, IsInvalid) (Syntax: 'p = new { M2() = i }')
                  Left: 
                    IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Object, IsImplicit) (Syntax: 'p')
                  Right: 
                    IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Object, IsInvalid, IsImplicit) (Syntax: 'new { M2() = i }')
                      Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
                        (ImplicitReference)
                      Operand: 
                        IAnonymousObjectCreationOperation (OperationKind.AnonymousObjectCreation, Type: <anonymous type: System.Int32 $0>, IsInvalid) (Syntax: 'new { M2() = i }')
                          Initializers(1):
                              ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32, IsInvalid, IsImplicit) (Syntax: 'M2() = i')
                                Left: 
                                  IPropertyReferenceOperation: System.Int32 <anonymous type: System.Int32 $0>.$0 { get; } (OperationKind.PropertyReference, Type: System.Int32, IsInvalid, IsImplicit) (Syntax: 'M2() = i')
                                    Instance Receiver: 
                                      IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: <anonymous type: System.Int32 $0>, IsInvalid, IsImplicit) (Syntax: 'new { M2() = i }')
                                Right: 
                                  IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.Int32, IsInvalid, IsImplicit) (Syntax: 'M2() = i')

        Next (Regular) Block[B2]
            Leaving: {R1}
}

Block[B2] - Exit
    Predecessors: [B1]
    Statements (0)
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // file.cs(8,19): error CS0746: Invalid anonymous type member declarator. Anonymous type members must be declared with a member assignment, simple name or member access.
                //         p = new { M2() = i };
                Diagnostic(ErrorCode.ERR_InvalidAnonymousTypeMemberDeclarator, "M2() = i").WithLocation(8, 19),
                // file.cs(8,19): error CS0131: The left-hand side of an assignment must be a variable, property or indexer
                //         p = new { M2() = i };
                Diagnostic(ErrorCode.ERR_AssgLvalueExpected, "M2()").WithLocation(8, 19)
            };

            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(source, expectedFlowGraph, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void AnonymousObjectCreation_NoControlFlow_Error04()
        {
            // Property reference with argument as an assignment target.
            string source = @"
using System;

class C
{
    void M(object p, int i, int j)
    /*<bind>*/{
        p = new { a[i] = j };
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
    CaptureIds: [0] [1]
    Block[B1] - Block
        Predecessors: [B0]
        Statements (3)
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'p')
              Value: 
                IParameterReferenceOperation: p (OperationKind.ParameterReference, Type: System.Object) (Syntax: 'p')

            IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsInvalid, IsImplicit) (Syntax: 'a[i] = j')
              Value: 
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: ?, IsInvalid) (Syntax: 'a[i] = j')
                  Left: 
                    IInvalidOperation (OperationKind.Invalid, Type: ?, IsInvalid) (Syntax: 'a[i]')
                      Children(2):
                          IParameterReferenceOperation: i (OperationKind.ParameterReference, Type: System.Int32, IsInvalid) (Syntax: 'i')
                          IInvalidOperation (OperationKind.Invalid, Type: ?, IsInvalid) (Syntax: 'a')
                            Children(0)
                  Right: 
                    IParameterReferenceOperation: j (OperationKind.ParameterReference, Type: System.Int32, IsInvalid) (Syntax: 'j')

            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null, IsInvalid) (Syntax: 'p = new { a[i] = j };')
              Expression: 
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Object, IsInvalid) (Syntax: 'p = new { a[i] = j }')
                  Left: 
                    IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Object, IsImplicit) (Syntax: 'p')
                  Right: 
                    IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Object, IsInvalid, IsImplicit) (Syntax: 'new { a[i] = j }')
                      Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
                        (ImplicitReference)
                      Operand: 
                        IAnonymousObjectCreationOperation (OperationKind.AnonymousObjectCreation, Type: <anonymous type: ? $0>, IsInvalid) (Syntax: 'new { a[i] = j }')
                          Initializers(1):
                              ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: ?, IsInvalid, IsImplicit) (Syntax: 'a[i] = j')
                                Left: 
                                  IPropertyReferenceOperation: ? <anonymous type: ? $0>.$0 { get; } (OperationKind.PropertyReference, Type: ?, IsInvalid, IsImplicit) (Syntax: 'a[i] = j')
                                    Instance Receiver: 
                                      IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: <anonymous type: ? $0>, IsInvalid, IsImplicit) (Syntax: 'new { a[i] = j }')
                                Right: 
                                  IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: ?, IsInvalid, IsImplicit) (Syntax: 'a[i] = j')

        Next (Regular) Block[B2]
            Leaving: {R1}
}

Block[B2] - Exit
    Predecessors: [B1]
    Statements (0)
";
            var expectedDiagnostics = new DiagnosticDescription[] {
               // file.cs(8,19): error CS0746: Invalid anonymous type member declarator. Anonymous type members must be declared with a member assignment, simple name or member access.
                //         p = new { a[i] = j };
                Diagnostic(ErrorCode.ERR_InvalidAnonymousTypeMemberDeclarator, "a[i] = j").WithLocation(8, 19),
                // file.cs(8,19): error CS0103: The name 'a' does not exist in the current context
                //         p = new { a[i] = j };
                Diagnostic(ErrorCode.ERR_NameNotInContext, "a").WithArguments("a").WithLocation(8, 19)
            };

            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(source, expectedFlowGraph, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void AnonymousObjectCreation_ControlFlowInFirstInitializer()
        {
            string source = @"
using System;

class C
{
    void M(object p, int? i1, int i2, int j)
    /*<bind>*/{
        p = new { a = i1 ?? i2, b = j };
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
    CaptureIds: [0] [2] [3]
    Block[B1] - Block
        Predecessors: [B0]
        Statements (1)
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'p')
              Value: 
                IParameterReferenceOperation: p (OperationKind.ParameterReference, Type: System.Object) (Syntax: 'p')

        Next (Regular) Block[B2]
            Entering: {R2}

    .locals {R2}
    {
        CaptureIds: [1]
        Block[B2] - Block
            Predecessors: [B1]
            Statements (1)
                IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'i1')
                  Value: 
                    IParameterReferenceOperation: i1 (OperationKind.ParameterReference, Type: System.Int32?) (Syntax: 'i1')

            Jump if True (Regular) to Block[B4]
                IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsImplicit) (Syntax: 'i1')
                  Operand: 
                    IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.Int32?, IsImplicit) (Syntax: 'i1')
                Leaving: {R2}

            Next (Regular) Block[B3]
        Block[B3] - Block
            Predecessors: [B2]
            Statements (1)
                IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'i1')
                  Value: 
                    IInvocationOperation ( System.Int32 System.Int32?.GetValueOrDefault()) (OperationKind.Invocation, Type: System.Int32, IsImplicit) (Syntax: 'i1')
                      Instance Receiver: 
                        IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.Int32?, IsImplicit) (Syntax: 'i1')
                      Arguments(0)

            Next (Regular) Block[B5]
                Leaving: {R2}
    }

    Block[B4] - Block
        Predecessors: [B2]
        Statements (1)
            IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'i2')
              Value: 
                IParameterReferenceOperation: i2 (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'i2')

        Next (Regular) Block[B5]
    Block[B5] - Block
        Predecessors: [B3] [B4]
        Statements (2)
            IFlowCaptureOperation: 3 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'j')
              Value: 
                IParameterReferenceOperation: j (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'j')

            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'p = new { a ... 2, b = j };')
              Expression: 
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Object) (Syntax: 'p = new { a ... i2, b = j }')
                  Left: 
                    IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Object, IsImplicit) (Syntax: 'p')
                  Right: 
                    IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Object, IsImplicit) (Syntax: 'new { a = i ... i2, b = j }')
                      Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
                        (ImplicitReference)
                      Operand: 
                        IAnonymousObjectCreationOperation (OperationKind.AnonymousObjectCreation, Type: <anonymous type: System.Int32 a, System.Int32 b>) (Syntax: 'new { a = i ... i2, b = j }')
                          Initializers(2):
                              ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32) (Syntax: 'a = i1 ?? i2')
                                Left: 
                                  IPropertyReferenceOperation: System.Int32 <anonymous type: System.Int32 a, System.Int32 b>.a { get; } (OperationKind.PropertyReference, Type: System.Int32) (Syntax: 'a')
                                    Instance Receiver: 
                                      IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: <anonymous type: System.Int32 a, System.Int32 b>, IsImplicit) (Syntax: 'new { a = i ... i2, b = j }')
                                Right: 
                                  IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'i1 ?? i2')
                              ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32) (Syntax: 'b = j')
                                Left: 
                                  IPropertyReferenceOperation: System.Int32 <anonymous type: System.Int32 a, System.Int32 b>.b { get; } (OperationKind.PropertyReference, Type: System.Int32) (Syntax: 'b')
                                    Instance Receiver: 
                                      IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: <anonymous type: System.Int32 a, System.Int32 b>, IsImplicit) (Syntax: 'new { a = i ... i2, b = j }')
                                Right: 
                                  IFlowCaptureReferenceOperation: 3 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'j')

        Next (Regular) Block[B6]
            Leaving: {R1}
}

Block[B6] - Exit
    Predecessors: [B5]
    Statements (0)
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(source, expectedFlowGraph, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void AnonymousObjectCreation_ControlFlowInSecondInitializer()
        {
            string source = @"
using System;

class C
{
    void M(object p, int? i1, int i2, int j)
    /*<bind>*/{
        p = new { a = j, b = i1 ?? i2 };
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
    CaptureIds: [0] [1] [3]
    Block[B1] - Block
        Predecessors: [B0]
        Statements (2)
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'p')
              Value: 
                IParameterReferenceOperation: p (OperationKind.ParameterReference, Type: System.Object) (Syntax: 'p')

            IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'j')
              Value: 
                IParameterReferenceOperation: j (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'j')

        Next (Regular) Block[B2]
            Entering: {R2}

    .locals {R2}
    {
        CaptureIds: [2]
        Block[B2] - Block
            Predecessors: [B1]
            Statements (1)
                IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'i1')
                  Value: 
                    IParameterReferenceOperation: i1 (OperationKind.ParameterReference, Type: System.Int32?) (Syntax: 'i1')

            Jump if True (Regular) to Block[B4]
                IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsImplicit) (Syntax: 'i1')
                  Operand: 
                    IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: System.Int32?, IsImplicit) (Syntax: 'i1')
                Leaving: {R2}

            Next (Regular) Block[B3]
        Block[B3] - Block
            Predecessors: [B2]
            Statements (1)
                IFlowCaptureOperation: 3 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'i1')
                  Value: 
                    IInvocationOperation ( System.Int32 System.Int32?.GetValueOrDefault()) (OperationKind.Invocation, Type: System.Int32, IsImplicit) (Syntax: 'i1')
                      Instance Receiver: 
                        IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: System.Int32?, IsImplicit) (Syntax: 'i1')
                      Arguments(0)

            Next (Regular) Block[B5]
                Leaving: {R2}
    }

    Block[B4] - Block
        Predecessors: [B2]
        Statements (1)
            IFlowCaptureOperation: 3 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'i2')
              Value: 
                IParameterReferenceOperation: i2 (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'i2')

        Next (Regular) Block[B5]
    Block[B5] - Block
        Predecessors: [B3] [B4]
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'p = new { a ... i1 ?? i2 };')
              Expression: 
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Object) (Syntax: 'p = new { a ...  i1 ?? i2 }')
                  Left: 
                    IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Object, IsImplicit) (Syntax: 'p')
                  Right: 
                    IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Object, IsImplicit) (Syntax: 'new { a = j ...  i1 ?? i2 }')
                      Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
                        (ImplicitReference)
                      Operand: 
                        IAnonymousObjectCreationOperation (OperationKind.AnonymousObjectCreation, Type: <anonymous type: System.Int32 a, System.Int32 b>) (Syntax: 'new { a = j ...  i1 ?? i2 }')
                          Initializers(2):
                              ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32) (Syntax: 'a = j')
                                Left: 
                                  IPropertyReferenceOperation: System.Int32 <anonymous type: System.Int32 a, System.Int32 b>.a { get; } (OperationKind.PropertyReference, Type: System.Int32) (Syntax: 'a')
                                    Instance Receiver: 
                                      IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: <anonymous type: System.Int32 a, System.Int32 b>, IsImplicit) (Syntax: 'new { a = j ...  i1 ?? i2 }')
                                Right: 
                                  IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'j')
                              ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32) (Syntax: 'b = i1 ?? i2')
                                Left: 
                                  IPropertyReferenceOperation: System.Int32 <anonymous type: System.Int32 a, System.Int32 b>.b { get; } (OperationKind.PropertyReference, Type: System.Int32) (Syntax: 'b')
                                    Instance Receiver: 
                                      IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: <anonymous type: System.Int32 a, System.Int32 b>, IsImplicit) (Syntax: 'new { a = j ...  i1 ?? i2 }')
                                Right: 
                                  IFlowCaptureReferenceOperation: 3 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'i1 ?? i2')

        Next (Regular) Block[B6]
            Leaving: {R1}
}

Block[B6] - Exit
    Predecessors: [B5]
    Statements (0)
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(source, expectedFlowGraph, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void AnonymousObjectCreation_ControlFlowInMultipleInitializers()
        {
            string source = @"
using System;

class C
{
    void M(object p, int? i1, int i2, int? j1, int j2)
    /*<bind>*/{
        p = new { a = i1 ?? i2, b = j1 ?? j2 };
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
    CaptureIds: [0] [2] [4]
    Block[B1] - Block
        Predecessors: [B0]
        Statements (1)
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'p')
              Value: 
                IParameterReferenceOperation: p (OperationKind.ParameterReference, Type: System.Object) (Syntax: 'p')

        Next (Regular) Block[B2]
            Entering: {R2}

    .locals {R2}
    {
        CaptureIds: [1]
        Block[B2] - Block
            Predecessors: [B1]
            Statements (1)
                IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'i1')
                  Value: 
                    IParameterReferenceOperation: i1 (OperationKind.ParameterReference, Type: System.Int32?) (Syntax: 'i1')

            Jump if True (Regular) to Block[B4]
                IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsImplicit) (Syntax: 'i1')
                  Operand: 
                    IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.Int32?, IsImplicit) (Syntax: 'i1')
                Leaving: {R2}

            Next (Regular) Block[B3]
        Block[B3] - Block
            Predecessors: [B2]
            Statements (1)
                IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'i1')
                  Value: 
                    IInvocationOperation ( System.Int32 System.Int32?.GetValueOrDefault()) (OperationKind.Invocation, Type: System.Int32, IsImplicit) (Syntax: 'i1')
                      Instance Receiver: 
                        IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.Int32?, IsImplicit) (Syntax: 'i1')
                      Arguments(0)

            Next (Regular) Block[B5]
                Leaving: {R2}
                Entering: {R3}
    }

    Block[B4] - Block
        Predecessors: [B2]
        Statements (1)
            IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'i2')
              Value: 
                IParameterReferenceOperation: i2 (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'i2')

        Next (Regular) Block[B5]
            Entering: {R3}

    .locals {R3}
    {
        CaptureIds: [3]
        Block[B5] - Block
            Predecessors: [B3] [B4]
            Statements (1)
                IFlowCaptureOperation: 3 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'j1')
                  Value: 
                    IParameterReferenceOperation: j1 (OperationKind.ParameterReference, Type: System.Int32?) (Syntax: 'j1')

            Jump if True (Regular) to Block[B7]
                IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsImplicit) (Syntax: 'j1')
                  Operand: 
                    IFlowCaptureReferenceOperation: 3 (OperationKind.FlowCaptureReference, Type: System.Int32?, IsImplicit) (Syntax: 'j1')
                Leaving: {R3}

            Next (Regular) Block[B6]
        Block[B6] - Block
            Predecessors: [B5]
            Statements (1)
                IFlowCaptureOperation: 4 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'j1')
                  Value: 
                    IInvocationOperation ( System.Int32 System.Int32?.GetValueOrDefault()) (OperationKind.Invocation, Type: System.Int32, IsImplicit) (Syntax: 'j1')
                      Instance Receiver: 
                        IFlowCaptureReferenceOperation: 3 (OperationKind.FlowCaptureReference, Type: System.Int32?, IsImplicit) (Syntax: 'j1')
                      Arguments(0)

            Next (Regular) Block[B8]
                Leaving: {R3}
    }

    Block[B7] - Block
        Predecessors: [B5]
        Statements (1)
            IFlowCaptureOperation: 4 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'j2')
              Value: 
                IParameterReferenceOperation: j2 (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'j2')

        Next (Regular) Block[B8]
    Block[B8] - Block
        Predecessors: [B6] [B7]
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'p = new { a ... j1 ?? j2 };')
              Expression: 
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Object) (Syntax: 'p = new { a ...  j1 ?? j2 }')
                  Left: 
                    IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Object, IsImplicit) (Syntax: 'p')
                  Right: 
                    IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Object, IsImplicit) (Syntax: 'new { a = i ...  j1 ?? j2 }')
                      Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
                        (ImplicitReference)
                      Operand: 
                        IAnonymousObjectCreationOperation (OperationKind.AnonymousObjectCreation, Type: <anonymous type: System.Int32 a, System.Int32 b>) (Syntax: 'new { a = i ...  j1 ?? j2 }')
                          Initializers(2):
                              ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32) (Syntax: 'a = i1 ?? i2')
                                Left: 
                                  IPropertyReferenceOperation: System.Int32 <anonymous type: System.Int32 a, System.Int32 b>.a { get; } (OperationKind.PropertyReference, Type: System.Int32) (Syntax: 'a')
                                    Instance Receiver: 
                                      IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: <anonymous type: System.Int32 a, System.Int32 b>, IsImplicit) (Syntax: 'new { a = i ...  j1 ?? j2 }')
                                Right: 
                                  IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'i1 ?? i2')
                              ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32) (Syntax: 'b = j1 ?? j2')
                                Left: 
                                  IPropertyReferenceOperation: System.Int32 <anonymous type: System.Int32 a, System.Int32 b>.b { get; } (OperationKind.PropertyReference, Type: System.Int32) (Syntax: 'b')
                                    Instance Receiver: 
                                      IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: <anonymous type: System.Int32 a, System.Int32 b>, IsImplicit) (Syntax: 'new { a = i ...  j1 ?? j2 }')
                                Right: 
                                  IFlowCaptureReferenceOperation: 4 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'j1 ?? j2')

        Next (Regular) Block[B9]
            Leaving: {R1}
}

Block[B9] - Exit
    Predecessors: [B8]
    Statements (0)
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(source, expectedFlowGraph, expectedDiagnostics);
        }

        [Fact]
        public void AnonymousObjectCreation_NullableEnabled_PropertyMatches()
        {
            var source = @"
#nullable enable
class C
{
    void M(int i1, object o1)
    {
        var obj = /*<bind>*/new { a = i1, o1, b = ""Hello world!"" }/*</bind>*/;
    }
}";

            var expectedOperationTree = @"
IAnonymousObjectCreationOperation (OperationKind.AnonymousObjectCreation, Type: <anonymous type: System.Int32 a, System.Object o1, System.String b>) (Syntax: 'new { a = i ... o world!"" }')
  Initializers(3):
      ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32) (Syntax: 'a = i1')
        Left: 
          IPropertyReferenceOperation: System.Int32 <anonymous type: System.Int32 a, System.Object o1, System.String b>.a { get; } (OperationKind.PropertyReference, Type: System.Int32) (Syntax: 'a')
            Instance Receiver: 
              IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: <anonymous type: System.Int32 a, System.Object o1, System.String b>, IsImplicit) (Syntax: 'new { a = i ... o world!"" }')
        Right: 
          IParameterReferenceOperation: i1 (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'i1')
      ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Object, IsImplicit) (Syntax: 'o1')
        Left: 
          IPropertyReferenceOperation: System.Object <anonymous type: System.Int32 a, System.Object o1, System.String b>.o1 { get; } (OperationKind.PropertyReference, Type: System.Object, IsImplicit) (Syntax: 'o1')
            Instance Receiver: 
              IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: <anonymous type: System.Int32 a, System.Object o1, System.String b>, IsImplicit) (Syntax: 'new { a = i ... o world!"" }')
        Right: 
          IParameterReferenceOperation: o1 (OperationKind.ParameterReference, Type: System.Object) (Syntax: 'o1')
      ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.String, Constant: ""Hello world!"") (Syntax: 'b = ""Hello world!""')
        Left: 
          IPropertyReferenceOperation: System.String <anonymous type: System.Int32 a, System.Object o1, System.String b>.b { get; } (OperationKind.PropertyReference, Type: System.String) (Syntax: 'b')
            Instance Receiver: 
              IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: <anonymous type: System.Int32 a, System.Object o1, System.String b>, IsImplicit) (Syntax: 'new { a = i ... o world!"" }')
        Right: 
          ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: ""Hello world!"") (Syntax: '""Hello world!""')";

            VerifyOperationTreeAndDiagnosticsForTest<AnonymousObjectCreationExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics: DiagnosticDescription.None);
        }
    }
}
