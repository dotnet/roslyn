// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Operations;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.VisualBasic;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    public partial class IOperationTests : SemanticModelTestBase
    {
        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void TranslatedQueryFlow_01()
        {
            string source = @"
using System;
using System.Collections.Generic;
using System.Linq;

class C
{
    void M(object p, List<int> a)
    /*<bind>*/{
        p = from x in a
            select 0;
    }/*</bind>*/
}
";
            string expectedOperationTree = @"
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
                        IInvocationOperation (System.Collections.Generic.IEnumerable<System.Int32> System.Linq.Enumerable.Select<System.Int32, System.Int32>(this System.Collections.Generic.IEnumerable<System.Int32> source, System.Func<System.Int32, System.Int32> selector)) (OperationKind.Invocation, Type: System.Collections.Generic.IEnumerable<System.Int32>, IsImplicit) (Syntax: 'select 0')
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
                              IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: selector) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: '0')
                                IDelegateCreationOperation (OperationKind.DelegateCreation, Type: System.Func<System.Int32, System.Int32>, IsImplicit) (Syntax: '0')
                                  Target: 
                                    IFlowAnonymousFunctionOperation (Symbol: lambda expression) (OperationKind.FlowAnonymousFunction, Type: null, IsImplicit) (Syntax: '0')
                                    {
                                        Block[B0#A0] - Entry
                                            Statements (0)
                                            Next (Regular) Block[B1#A0]
                                        Block[B1#A0] - Block
                                            Predecessors: [B0#A0]
                                            Statements (0)
                                            Next (Return) Block[B2#A0]
                                                ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 0) (Syntax: '0')
                                        Block[B2#A0] - Exit
                                            Predecessors: [B1#A0]
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

            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void TranslatedQueryFlow_02()
        {
            string source = @"
using System;
using System.Collections.Generic;
using System.Linq;

class C
{
    void M(List<int> d1, List<int> d2, object p)
    /*<bind>*/
    {
        p = from x in (d1 ?? d2)
            select 0;
    }/*</bind>*/
}
";
            string expectedOperationTree = @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
Block[B1] - Block
    Predecessors: [B0]
    Statements (2)
        IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'p')
          Value: 
            IParameterReferenceOperation: p (OperationKind.ParameterReference, Type: System.Object) (Syntax: 'p')

        IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'd1')
          Value: 
            IParameterReferenceOperation: d1 (OperationKind.ParameterReference, Type: System.Collections.Generic.List<System.Int32>) (Syntax: 'd1')

    Jump if True (Regular) to Block[B3]
        IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsImplicit) (Syntax: 'd1')
          Operand: 
            IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.Collections.Generic.List<System.Int32>, IsImplicit) (Syntax: 'd1')

    Next (Regular) Block[B2]
Block[B2] - Block
    Predecessors: [B1]
    Statements (1)
        IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'd1')
          Value: 
            IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.Collections.Generic.List<System.Int32>, IsImplicit) (Syntax: 'd1')

    Next (Regular) Block[B4]
Block[B3] - Block
    Predecessors: [B1]
    Statements (1)
        IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'd2')
          Value: 
            IParameterReferenceOperation: d2 (OperationKind.ParameterReference, Type: System.Collections.Generic.List<System.Int32>) (Syntax: 'd2')

    Next (Regular) Block[B4]
Block[B4] - Block
    Predecessors: [B2] [B3]
    Statements (1)
        IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'p = from x  ... select 0;')
          Expression: 
            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Object) (Syntax: 'p = from x  ... select 0')
              Left: 
                IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Object, IsImplicit) (Syntax: 'p')
              Right: 
                IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Object, IsImplicit) (Syntax: 'from x in ( ... select 0')
                  Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
                    (ImplicitReference)
                  Operand: 
                    ITranslatedQueryOperation (OperationKind.TranslatedQuery, Type: System.Collections.Generic.IEnumerable<System.Int32>) (Syntax: 'from x in ( ... select 0')
                      Expression: 
                        IInvocationOperation (System.Collections.Generic.IEnumerable<System.Int32> System.Linq.Enumerable.Select<System.Int32, System.Int32>(this System.Collections.Generic.IEnumerable<System.Int32> source, System.Func<System.Int32, System.Int32> selector)) (OperationKind.Invocation, Type: System.Collections.Generic.IEnumerable<System.Int32>, IsImplicit) (Syntax: 'select 0')
                          Instance Receiver: 
                            null
                          Arguments(2):
                              IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: source) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 'from x in (d1 ?? d2)')
                                IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Collections.Generic.IEnumerable<System.Int32>, IsImplicit) (Syntax: 'from x in (d1 ?? d2)')
                                  Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
                                    (ImplicitReference)
                                  Operand: 
                                    IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: System.Collections.Generic.List<System.Int32>, IsImplicit) (Syntax: 'd1 ?? d2')
                                InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                              IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: selector) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: '0')
                                IDelegateCreationOperation (OperationKind.DelegateCreation, Type: System.Func<System.Int32, System.Int32>, IsImplicit) (Syntax: '0')
                                  Target: 
                                    IFlowAnonymousFunctionOperation (Symbol: lambda expression) (OperationKind.FlowAnonymousFunction, Type: null, IsImplicit) (Syntax: '0')
                                    {
                                        Block[B0#A0] - Entry
                                            Statements (0)
                                            Next (Regular) Block[B1#A0]
                                        Block[B1#A0] - Block
                                            Predecessors: [B0#A0]
                                            Statements (0)
                                            Next (Return) Block[B2#A0]
                                                ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 0) (Syntax: '0')
                                        Block[B2#A0] - Exit
                                            Predecessors: [B1#A0]
                                            Statements (0)
                                    }
                                InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)

    Next (Regular) Block[B5]
Block[B5] - Exit
    Predecessors: [B4]
    Statements (0)
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }
    }
}
