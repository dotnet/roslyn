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
        [Fact, WorkItem(18300, "https://github.com/dotnet/roslyn/issues/18300")]
        public void InterpolatedStringExpression_Empty()
        {
            string source = @"
using System;

internal class Class
{
    public void M()
    {
        Console.WriteLine(/*<bind>*/$""""/*</bind>*/);
    }
}
";
            string expectedOperationTree = @"
IInterpolatedStringOperation (OperationKind.InterpolatedString, Type: System.String) (Syntax: '$""""')
  Parts(0)
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<InterpolatedStringExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact, WorkItem(18300, "https://github.com/dotnet/roslyn/issues/18300")]
        public void InterpolatedStringExpression_OnlyTextPart()
        {
            string source = @"
using System;

internal class Class
{
    public void M()
    {
        Console.WriteLine(/*<bind>*/$""Only text part""/*</bind>*/);
    }
}
";
            string expectedOperationTree = @"
IInterpolatedStringOperation (OperationKind.InterpolatedString, Type: System.String) (Syntax: '$""Only text part""')
  Parts(1):
      IInterpolatedStringTextOperation (OperationKind.InterpolatedStringText, Type: null) (Syntax: 'Only text part')
        Text: 
          ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: ""Only text part"", IsImplicit) (Syntax: 'Only text part')
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<InterpolatedStringExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact, WorkItem(18300, "https://github.com/dotnet/roslyn/issues/18300")]
        public void InterpolatedStringExpression_OnlyInterpolationPart()
        {
            string source = @"
using System;

internal class Class
{
    public void M()
    {
        Console.WriteLine(/*<bind>*/$""{1}""/*</bind>*/);
    }
}
";
            string expectedOperationTree = @"
IInterpolatedStringOperation (OperationKind.InterpolatedString, Type: System.String) (Syntax: '$""{1}""')
  Parts(1):
      IInterpolationOperation (OperationKind.Interpolation, Type: null) (Syntax: '{1}')
        Expression: 
          ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
        Alignment: 
          null
        FormatString: 
          null
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<InterpolatedStringExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact, WorkItem(18300, "https://github.com/dotnet/roslyn/issues/18300")]
        public void InterpolatedStringExpression_EmptyInterpolationPart()
        {
            string source = @"
using System;

internal class Class
{
    public void M()
    {
        Console.WriteLine(/*<bind>*/$""{}""/*</bind>*/);
    }
}
";
            string expectedOperationTree = @"
IInterpolatedStringOperation (OperationKind.InterpolatedString, Type: System.String, IsInvalid) (Syntax: '$""{}""')
  Parts(1):
      IInterpolationOperation (OperationKind.Interpolation, Type: null, IsInvalid) (Syntax: '{}')
        Expression: 
          IInvalidOperation (OperationKind.Invalid, Type: null, IsInvalid) (Syntax: '')
            Children(0)
        Alignment: 
          null
        FormatString: 
          null
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS1733: Expected expression
                //         Console.WriteLine(/*<bind>*/$"{}"/*</bind>*/);
                Diagnostic(ErrorCode.ERR_ExpressionExpected, "").WithLocation(8, 40)
            };

            VerifyOperationTreeAndDiagnosticsForTest<InterpolatedStringExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact, WorkItem(18300, "https://github.com/dotnet/roslyn/issues/18300")]
        public void InterpolatedStringExpression_TextAndInterpolationParts()
        {
            string source = @"
using System;

internal class Class
{
    public void M(int x)
    {
        Console.WriteLine(/*<bind>*/$""String {x} and constant {1}""/*</bind>*/);
    }
}
";
            string expectedOperationTree = @"
IInterpolatedStringOperation (OperationKind.InterpolatedString, Type: System.String) (Syntax: '$""String {x ... nstant {1}""')
  Parts(4):
      IInterpolatedStringTextOperation (OperationKind.InterpolatedStringText, Type: null) (Syntax: 'String ')
        Text: 
          ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: ""String "", IsImplicit) (Syntax: 'String ')
      IInterpolationOperation (OperationKind.Interpolation, Type: null) (Syntax: '{x}')
        Expression: 
          IParameterReferenceOperation: x (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'x')
        Alignment: 
          null
        FormatString: 
          null
      IInterpolatedStringTextOperation (OperationKind.InterpolatedStringText, Type: null) (Syntax: ' and constant ')
        Text: 
          ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: "" and constant "", IsImplicit) (Syntax: ' and constant ')
      IInterpolationOperation (OperationKind.Interpolation, Type: null) (Syntax: '{1}')
        Expression: 
          ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
        Alignment: 
          null
        FormatString: 
          null
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<InterpolatedStringExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact, WorkItem(18300, "https://github.com/dotnet/roslyn/issues/18300")]
        public void InterpolatedStringExpression_FormatAndAlignment()
        {
            string source = @"
using System;

internal class Class
{
    private string x = string.Empty;
    private int y = 0;

    public void M()
    {
        Console.WriteLine(/*<bind>*/$""String {x,20} and {y:D3} and constant {1}""/*</bind>*/);
    }
}
";
            string expectedOperationTree = @"
IInterpolatedStringOperation (OperationKind.InterpolatedString, Type: System.String) (Syntax: '$""String {x ... nstant {1}""')
  Parts(6):
      IInterpolatedStringTextOperation (OperationKind.InterpolatedStringText, Type: null) (Syntax: 'String ')
        Text: 
          ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: ""String "", IsImplicit) (Syntax: 'String ')
      IInterpolationOperation (OperationKind.Interpolation, Type: null) (Syntax: '{x,20}')
        Expression: 
          IFieldReferenceOperation: System.String Class.x (OperationKind.FieldReference, Type: System.String) (Syntax: 'x')
            Instance Receiver: 
              IInstanceReferenceOperation (ReferenceKind: ContainingTypeInstance) (OperationKind.InstanceReference, Type: Class, IsImplicit) (Syntax: 'x')
        Alignment: 
          ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 20) (Syntax: '20')
        FormatString: 
          null
      IInterpolatedStringTextOperation (OperationKind.InterpolatedStringText, Type: null) (Syntax: ' and ')
        Text: 
          ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: "" and "", IsImplicit) (Syntax: ' and ')
      IInterpolationOperation (OperationKind.Interpolation, Type: null) (Syntax: '{y:D3}')
        Expression: 
          IFieldReferenceOperation: System.Int32 Class.y (OperationKind.FieldReference, Type: System.Int32) (Syntax: 'y')
            Instance Receiver: 
              IInstanceReferenceOperation (ReferenceKind: ContainingTypeInstance) (OperationKind.InstanceReference, Type: Class, IsImplicit) (Syntax: 'y')
        Alignment: 
          null
        FormatString: 
          ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: ""D3"") (Syntax: ':D3')
      IInterpolatedStringTextOperation (OperationKind.InterpolatedStringText, Type: null) (Syntax: ' and constant ')
        Text: 
          ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: "" and constant "", IsImplicit) (Syntax: ' and constant ')
      IInterpolationOperation (OperationKind.Interpolation, Type: null) (Syntax: '{1}')
        Expression: 
          ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
        Alignment: 
          null
        FormatString: 
          null
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<InterpolatedStringExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact, WorkItem(18300, "https://github.com/dotnet/roslyn/issues/18300")]
        public void InterpolatedStringExpression_InterpolationAndFormatAndAlignment()
        {
            string source = @"
using System;

internal class Class
{
    private string x = string.Empty;
    private const int y = 0;

    public void M()
    {
        Console.WriteLine(/*<bind>*/$""String {x,y:D3}""/*</bind>*/);
    }
}
";
            string expectedOperationTree = @"
IInterpolatedStringOperation (OperationKind.InterpolatedString, Type: System.String) (Syntax: '$""String {x,y:D3}""')
  Parts(2):
      IInterpolatedStringTextOperation (OperationKind.InterpolatedStringText, Type: null) (Syntax: 'String ')
        Text: 
          ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: ""String "", IsImplicit) (Syntax: 'String ')
      IInterpolationOperation (OperationKind.Interpolation, Type: null) (Syntax: '{x,y:D3}')
        Expression: 
          IFieldReferenceOperation: System.String Class.x (OperationKind.FieldReference, Type: System.String) (Syntax: 'x')
            Instance Receiver: 
              IInstanceReferenceOperation (ReferenceKind: ContainingTypeInstance) (OperationKind.InstanceReference, Type: Class, IsImplicit) (Syntax: 'x')
        Alignment: 
          IFieldReferenceOperation: System.Int32 Class.y (Static) (OperationKind.FieldReference, Type: System.Int32, Constant: 0) (Syntax: 'y')
            Instance Receiver: 
              null
        FormatString: 
          ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: ""D3"") (Syntax: ':D3')
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<InterpolatedStringExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact, WorkItem(18300, "https://github.com/dotnet/roslyn/issues/18300")]
        public void InterpolatedStringExpression_InvocationInInterpolation()
        {
            string source = @"
using System;

internal class Class
{
    public void M()
    {
        string x = string.Empty;
        int y = 0;
        Console.WriteLine(/*<bind>*/$""String {x} and {M2(y)} and constant {1}""/*</bind>*/);
    }

    private string M2(int z) => z.ToString();
}
";
            string expectedOperationTree = @"
IInterpolatedStringOperation (OperationKind.InterpolatedString, Type: System.String) (Syntax: '$""String {x ... nstant {1}""')
  Parts(6):
      IInterpolatedStringTextOperation (OperationKind.InterpolatedStringText, Type: null) (Syntax: 'String ')
        Text: 
          ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: ""String "", IsImplicit) (Syntax: 'String ')
      IInterpolationOperation (OperationKind.Interpolation, Type: null) (Syntax: '{x}')
        Expression: 
          ILocalReferenceOperation: x (OperationKind.LocalReference, Type: System.String) (Syntax: 'x')
        Alignment: 
          null
        FormatString: 
          null
      IInterpolatedStringTextOperation (OperationKind.InterpolatedStringText, Type: null) (Syntax: ' and ')
        Text: 
          ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: "" and "", IsImplicit) (Syntax: ' and ')
      IInterpolationOperation (OperationKind.Interpolation, Type: null) (Syntax: '{M2(y)}')
        Expression: 
          IInvocationOperation ( System.String Class.M2(System.Int32 z)) (OperationKind.Invocation, Type: System.String) (Syntax: 'M2(y)')
            Instance Receiver: 
              IInstanceReferenceOperation (ReferenceKind: ContainingTypeInstance) (OperationKind.InstanceReference, Type: Class, IsImplicit) (Syntax: 'M2')
            Arguments(1):
                IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: z) (OperationKind.Argument, Type: null) (Syntax: 'y')
                  ILocalReferenceOperation: y (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'y')
                  InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        Alignment: 
          null
        FormatString: 
          null
      IInterpolatedStringTextOperation (OperationKind.InterpolatedStringText, Type: null) (Syntax: ' and constant ')
        Text: 
          ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: "" and constant "", IsImplicit) (Syntax: ' and constant ')
      IInterpolationOperation (OperationKind.Interpolation, Type: null) (Syntax: '{1}')
        Expression: 
          ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
        Alignment: 
          null
        FormatString: 
          null
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<InterpolatedStringExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact, WorkItem(18300, "https://github.com/dotnet/roslyn/issues/18300")]
        public void InterpolatedStringExpression_NestedInterpolation()
        {
            string source = @"
using System;

internal class Class
{
    public void M()
    {
        string x = string.Empty;
        int y = 0;
        Console.WriteLine(/*<bind>*/$""String {M2($""{y}"")}""/*</bind>*/);
    }

    private int M2(string z) => Int32.Parse(z);
}
";
            string expectedOperationTree = @"
IInterpolatedStringOperation (OperationKind.InterpolatedString, Type: System.String) (Syntax: '$""String {M2($""{y}"")}""')
  Parts(2):
      IInterpolatedStringTextOperation (OperationKind.InterpolatedStringText, Type: null) (Syntax: 'String ')
        Text: 
          ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: ""String "", IsImplicit) (Syntax: 'String ')
      IInterpolationOperation (OperationKind.Interpolation, Type: null) (Syntax: '{M2($""{y}"")}')
        Expression: 
          IInvocationOperation ( System.Int32 Class.M2(System.String z)) (OperationKind.Invocation, Type: System.Int32) (Syntax: 'M2($""{y}"")')
            Instance Receiver: 
              IInstanceReferenceOperation (ReferenceKind: ContainingTypeInstance) (OperationKind.InstanceReference, Type: Class, IsImplicit) (Syntax: 'M2')
            Arguments(1):
                IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: z) (OperationKind.Argument, Type: null) (Syntax: '$""{y}""')
                  IInterpolatedStringOperation (OperationKind.InterpolatedString, Type: System.String) (Syntax: '$""{y}""')
                    Parts(1):
                        IInterpolationOperation (OperationKind.Interpolation, Type: null) (Syntax: '{y}')
                          Expression: 
                            ILocalReferenceOperation: y (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'y')
                          Alignment: 
                            null
                          FormatString: 
                            null
                  InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        Alignment: 
          null
        FormatString: 
          null
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<InterpolatedStringExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact, WorkItem(18300, "https://github.com/dotnet/roslyn/issues/18300")]
        public void InterpolatedStringExpression_InvalidExpressionInInterpolation()
        {
            string source = @"
using System;

internal class Class
{
    public void M(int x)
    {
        Console.WriteLine(/*<bind>*/$""String {x1} and constant {Class}""/*</bind>*/);
    }
}
";
            string expectedOperationTree = @"
IInterpolatedStringOperation (OperationKind.InterpolatedString, Type: System.String, IsInvalid) (Syntax: '$""String {x ... nt {Class}""')
  Parts(4):
      IInterpolatedStringTextOperation (OperationKind.InterpolatedStringText, Type: null) (Syntax: 'String ')
        Text: 
          ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: ""String "", IsImplicit) (Syntax: 'String ')
      IInterpolationOperation (OperationKind.Interpolation, Type: null, IsInvalid) (Syntax: '{x1}')
        Expression: 
          IInvalidOperation (OperationKind.Invalid, Type: ?, IsInvalid) (Syntax: 'x1')
            Children(0)
        Alignment: 
          null
        FormatString: 
          null
      IInterpolatedStringTextOperation (OperationKind.InterpolatedStringText, Type: null) (Syntax: ' and constant ')
        Text: 
          ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: "" and constant "", IsImplicit) (Syntax: ' and constant ')
      IInterpolationOperation (OperationKind.Interpolation, Type: null, IsInvalid) (Syntax: '{Class}')
        Expression: 
          IInvalidOperation (OperationKind.Invalid, Type: Class, IsInvalid, IsImplicit) (Syntax: 'Class')
            Children(1):
                IOperation:  (OperationKind.None, Type: null, IsInvalid) (Syntax: 'Class')
        Alignment: 
          null
        FormatString: 
          null
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS0103: The name 'x1' does not exist in the current context
                //         Console.WriteLine(/*<bind>*/$"String {x1} and constant {Class}"/*</bind>*/);
                Diagnostic(ErrorCode.ERR_NameNotInContext, "x1").WithArguments("x1").WithLocation(8, 47),
                // CS0119: 'Class' is a type, which is not valid in the given context
                //         Console.WriteLine(/*<bind>*/$"String {x1} and constant {Class}"/*</bind>*/);
                Diagnostic(ErrorCode.ERR_BadSKunknown, "Class").WithArguments("Class", "type").WithLocation(8, 65)
            };

            VerifyOperationTreeAndDiagnosticsForTest<InterpolatedStringExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void InterpolatedStringExpression_Empty_Flow()
        {
            string source = @"
using System;

internal class Class
{
    public void M(string p)
    /*<bind>*/{
        p = $"""";
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
        IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'p = $"""";')
          Expression: 
            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.String) (Syntax: 'p = $""""')
              Left: 
                IParameterReferenceOperation: p (OperationKind.ParameterReference, Type: System.String) (Syntax: 'p')
              Right: 
                IInterpolatedStringOperation (OperationKind.InterpolatedString, Type: System.String) (Syntax: '$""""')
                  Parts(0)

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
        public void InterpolatedStringExpression_OnlyTextPart_Flow()
        {
            string source = @"
using System;

internal class Class
{
    public void M(string p)
    /*<bind>*/{
        p = $""Only text part"";
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
        IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'p = $""Only text part"";')
          Expression: 
            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.String) (Syntax: 'p = $""Only text part""')
              Left: 
                IParameterReferenceOperation: p (OperationKind.ParameterReference, Type: System.String) (Syntax: 'p')
              Right: 
                IInterpolatedStringOperation (OperationKind.InterpolatedString, Type: System.String) (Syntax: '$""Only text part""')
                  Parts(1):
                      IInterpolatedStringTextOperation (OperationKind.InterpolatedStringText, Type: null) (Syntax: 'Only text part')
                        Text: 
                          ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: ""Only text part"", IsImplicit) (Syntax: 'Only text part')

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
        public void InterpolatedStringExpression_OnlyInterpolationPart_Flow()
        {
            string source = @"
using System;

internal class Class
{
    public void M(bool a, string b, string c, string p)
    /*<bind>*/{
        p = $""{(a ? b : c)}"";
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
        Statements (1)
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'p')
              Value: 
                IParameterReferenceOperation: p (OperationKind.ParameterReference, Type: System.String) (Syntax: 'p')

        Jump if False (Regular) to Block[B3]
            IParameterReferenceOperation: a (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'a')

        Next (Regular) Block[B2]
    Block[B2] - Block
        Predecessors: [B1]
        Statements (1)
            IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'b')
              Value: 
                IParameterReferenceOperation: b (OperationKind.ParameterReference, Type: System.String) (Syntax: 'b')

        Next (Regular) Block[B4]
    Block[B3] - Block
        Predecessors: [B1]
        Statements (1)
            IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'c')
              Value: 
                IParameterReferenceOperation: c (OperationKind.ParameterReference, Type: System.String) (Syntax: 'c')

        Next (Regular) Block[B4]
    Block[B4] - Block
        Predecessors: [B2] [B3]
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'p = $""{(a ? b : c)}"";')
              Expression: 
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.String) (Syntax: 'p = $""{(a ? b : c)}""')
                  Left: 
                    IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.String, IsImplicit) (Syntax: 'p')
                  Right: 
                    IInterpolatedStringOperation (OperationKind.InterpolatedString, Type: System.String) (Syntax: '$""{(a ? b : c)}""')
                      Parts(1):
                          IInterpolationOperation (OperationKind.Interpolation, Type: null) (Syntax: '{(a ? b : c)}')
                            Expression: 
                              IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.String, IsImplicit) (Syntax: 'a ? b : c')
                            Alignment: 
                              null
                            FormatString: 
                              null

        Next (Regular) Block[B5]
            Leaving: {R1}
}

Block[B5] - Exit
    Predecessors: [B4]
    Statements (0)
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(source, expectedFlowGraph, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void InterpolatedStringExpression_MultipleInterpolationParts_Flow()
        {
            string source = @"
using System;

internal class Class
{
    public void M(bool a, string b, string c, string c2, string p)
    /*<bind>*/{
        p = $""{(a ? b : c)}{c2}"";
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
        Statements (1)
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'p')
              Value: 
                IParameterReferenceOperation: p (OperationKind.ParameterReference, Type: System.String) (Syntax: 'p')

        Jump if False (Regular) to Block[B3]
            IParameterReferenceOperation: a (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'a')

        Next (Regular) Block[B2]
    Block[B2] - Block
        Predecessors: [B1]
        Statements (1)
            IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'b')
              Value: 
                IParameterReferenceOperation: b (OperationKind.ParameterReference, Type: System.String) (Syntax: 'b')

        Next (Regular) Block[B4]
    Block[B3] - Block
        Predecessors: [B1]
        Statements (1)
            IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'c')
              Value: 
                IParameterReferenceOperation: c (OperationKind.ParameterReference, Type: System.String) (Syntax: 'c')

        Next (Regular) Block[B4]
    Block[B4] - Block
        Predecessors: [B2] [B3]
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'p = $""{(a ? ... : c)}{c2}"";')
              Expression: 
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.String) (Syntax: 'p = $""{(a ? b : c)}{c2}""')
                  Left: 
                    IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.String, IsImplicit) (Syntax: 'p')
                  Right: 
                    IInterpolatedStringOperation (OperationKind.InterpolatedString, Type: System.String) (Syntax: '$""{(a ? b : c)}{c2}""')
                      Parts(2):
                          IInterpolationOperation (OperationKind.Interpolation, Type: null) (Syntax: '{(a ? b : c)}')
                            Expression: 
                              IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.String, IsImplicit) (Syntax: 'a ? b : c')
                            Alignment: 
                              null
                            FormatString: 
                              null
                          IInterpolationOperation (OperationKind.Interpolation, Type: null) (Syntax: '{c2}')
                            Expression: 
                              IParameterReferenceOperation: c2 (OperationKind.ParameterReference, Type: System.String) (Syntax: 'c2')
                            Alignment: 
                              null
                            FormatString: 
                              null

        Next (Regular) Block[B5]
            Leaving: {R1}
}

Block[B5] - Exit
    Predecessors: [B4]
    Statements (0)
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(source, expectedFlowGraph, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void InterpolatedStringExpression_TextAndInterpolationParts_Flow()
        {
            string source = @"
using System;

internal class Class
{
    public void M(bool a, string b, string c, bool a2, string b2, string c2, string p)
    /*<bind>*/{
        p = $""String1 {(a ? b : c)} and String2 {(a2 ? b2 : c2)}"";
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
        Statements (1)
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'p')
              Value: 
                IParameterReferenceOperation: p (OperationKind.ParameterReference, Type: System.String) (Syntax: 'p')

        Jump if False (Regular) to Block[B3]
            IParameterReferenceOperation: a (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'a')

        Next (Regular) Block[B2]
    Block[B2] - Block
        Predecessors: [B1]
        Statements (1)
            IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'b')
              Value: 
                IParameterReferenceOperation: b (OperationKind.ParameterReference, Type: System.String) (Syntax: 'b')

        Next (Regular) Block[B4]
    Block[B3] - Block
        Predecessors: [B1]
        Statements (1)
            IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'c')
              Value: 
                IParameterReferenceOperation: c (OperationKind.ParameterReference, Type: System.String) (Syntax: 'c')

        Next (Regular) Block[B4]
    Block[B4] - Block
        Predecessors: [B2] [B3]
        Statements (0)
        Jump if False (Regular) to Block[B6]
            IParameterReferenceOperation: a2 (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'a2')

        Next (Regular) Block[B5]
    Block[B5] - Block
        Predecessors: [B4]
        Statements (1)
            IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'b2')
              Value: 
                IParameterReferenceOperation: b2 (OperationKind.ParameterReference, Type: System.String) (Syntax: 'b2')

        Next (Regular) Block[B7]
    Block[B6] - Block
        Predecessors: [B4]
        Statements (1)
            IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'c2')
              Value: 
                IParameterReferenceOperation: c2 (OperationKind.ParameterReference, Type: System.String) (Syntax: 'c2')

        Next (Regular) Block[B7]
    Block[B7] - Block
        Predecessors: [B5] [B6]
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'p = $""Strin ... b2 : c2)}"";')
              Expression: 
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.String) (Syntax: 'p = $""Strin ...  b2 : c2)}""')
                  Left: 
                    IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.String, IsImplicit) (Syntax: 'p')
                  Right: 
                    IInterpolatedStringOperation (OperationKind.InterpolatedString, Type: System.String) (Syntax: '$""String1 { ...  b2 : c2)}""')
                      Parts(4):
                          IInterpolatedStringTextOperation (OperationKind.InterpolatedStringText, Type: null) (Syntax: 'String1 ')
                            Text: 
                              ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: ""String1 "", IsImplicit) (Syntax: 'String1 ')
                          IInterpolationOperation (OperationKind.Interpolation, Type: null) (Syntax: '{(a ? b : c)}')
                            Expression: 
                              IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.String, IsImplicit) (Syntax: 'a ? b : c')
                            Alignment: 
                              null
                            FormatString: 
                              null
                          IInterpolatedStringTextOperation (OperationKind.InterpolatedStringText, Type: null) (Syntax: ' and String2 ')
                            Text: 
                              ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: "" and String2 "", IsImplicit) (Syntax: ' and String2 ')
                          IInterpolationOperation (OperationKind.Interpolation, Type: null) (Syntax: '{(a2 ? b2 : c2)}')
                            Expression: 
                              IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: System.String, IsImplicit) (Syntax: 'a2 ? b2 : c2')
                            Alignment: 
                              null
                            FormatString: 
                              null

        Next (Regular) Block[B8]
            Leaving: {R1}
}

Block[B8] - Exit
    Predecessors: [B7]
    Statements (0)
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(source, expectedFlowGraph, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void InterpolatedStringExpression_FormatAndAlignment_Flow()
        {
            string source = @"
using System;

internal class Class
{
    public void M(bool a, string b, string c, string p)
    /*<bind>*/{
        p = $""{(a ? b : c),20:D3}"";
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
        Statements (1)
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'p')
              Value: 
                IParameterReferenceOperation: p (OperationKind.ParameterReference, Type: System.String) (Syntax: 'p')

        Jump if False (Regular) to Block[B3]
            IParameterReferenceOperation: a (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'a')

        Next (Regular) Block[B2]
    Block[B2] - Block
        Predecessors: [B1]
        Statements (1)
            IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'b')
              Value: 
                IParameterReferenceOperation: b (OperationKind.ParameterReference, Type: System.String) (Syntax: 'b')

        Next (Regular) Block[B4]
    Block[B3] - Block
        Predecessors: [B1]
        Statements (1)
            IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'c')
              Value: 
                IParameterReferenceOperation: c (OperationKind.ParameterReference, Type: System.String) (Syntax: 'c')

        Next (Regular) Block[B4]
    Block[B4] - Block
        Predecessors: [B2] [B3]
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'p = $""{(a ? ... c),20:D3}"";')
              Expression: 
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.String) (Syntax: 'p = $""{(a ? ...  c),20:D3}""')
                  Left: 
                    IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.String, IsImplicit) (Syntax: 'p')
                  Right: 
                    IInterpolatedStringOperation (OperationKind.InterpolatedString, Type: System.String) (Syntax: '$""{(a ? b : c),20:D3}""')
                      Parts(1):
                          IInterpolationOperation (OperationKind.Interpolation, Type: null) (Syntax: '{(a ? b : c),20:D3}')
                            Expression: 
                              IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.String, IsImplicit) (Syntax: 'a ? b : c')
                            Alignment: 
                              ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 20) (Syntax: '20')
                            FormatString: 
                              ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: ""D3"") (Syntax: ':D3')

        Next (Regular) Block[B5]
            Leaving: {R1}
}

Block[B5] - Exit
    Predecessors: [B4]
    Statements (0)
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(source, expectedFlowGraph, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void InterpolatedStringExpression_FormatAndAlignment_Flow_02()
        {
            string source = @"
using System;

internal class Class
{
    public void M(bool a, string b, string b2, string c, string p)
    /*<bind>*/{
        p = $""{b2,20:D3}{(a ? b : c)}"";
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
    CaptureIds: [0] [1] [2] [3]
    Block[B1] - Block
        Predecessors: [B0]
        Statements (3)
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'p')
              Value: 
                IParameterReferenceOperation: p (OperationKind.ParameterReference, Type: System.String) (Syntax: 'p')

            IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'b2')
              Value: 
                IParameterReferenceOperation: b2 (OperationKind.ParameterReference, Type: System.String) (Syntax: 'b2')

            IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: '20')
              Value: 
                ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 20) (Syntax: '20')

        Jump if False (Regular) to Block[B3]
            IParameterReferenceOperation: a (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'a')

        Next (Regular) Block[B2]
    Block[B2] - Block
        Predecessors: [B1]
        Statements (1)
            IFlowCaptureOperation: 3 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'b')
              Value: 
                IParameterReferenceOperation: b (OperationKind.ParameterReference, Type: System.String) (Syntax: 'b')

        Next (Regular) Block[B4]
    Block[B3] - Block
        Predecessors: [B1]
        Statements (1)
            IFlowCaptureOperation: 3 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'c')
              Value: 
                IParameterReferenceOperation: c (OperationKind.ParameterReference, Type: System.String) (Syntax: 'c')

        Next (Regular) Block[B4]
    Block[B4] - Block
        Predecessors: [B2] [B3]
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'p = $""{b2,2 ... ? b : c)}"";')
              Expression: 
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.String) (Syntax: 'p = $""{b2,2 ...  ? b : c)}""')
                  Left: 
                    IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.String, IsImplicit) (Syntax: 'p')
                  Right: 
                    IInterpolatedStringOperation (OperationKind.InterpolatedString, Type: System.String) (Syntax: '$""{b2,20:D3 ...  ? b : c)}""')
                      Parts(2):
                          IInterpolationOperation (OperationKind.Interpolation, Type: null) (Syntax: '{b2,20:D3}')
                            Expression: 
                              IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.String, IsImplicit) (Syntax: 'b2')
                            Alignment: 
                              IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: System.Int32, Constant: 20, IsImplicit) (Syntax: '20')
                            FormatString: 
                              ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: ""D3"") (Syntax: ':D3')
                          IInterpolationOperation (OperationKind.Interpolation, Type: null) (Syntax: '{(a ? b : c)}')
                            Expression: 
                              IFlowCaptureReferenceOperation: 3 (OperationKind.FlowCaptureReference, Type: System.String, IsImplicit) (Syntax: 'a ? b : c')
                            Alignment: 
                              null
                            FormatString: 
                              null

        Next (Regular) Block[B5]
            Leaving: {R1}
}

Block[B5] - Exit
    Predecessors: [B4]
    Statements (0)
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(source, expectedFlowGraph, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void InterpolatedStringExpression_FormatAndAlignment_Flow_03()
        {
            string source = @"
using System;

internal class Class
{
    public void M(bool a, string b, string b2, string b3, string c, string p)
    /*<bind>*/{
        p = $""{b2,20:D3}{b3,21:D4}{(a ? b : c)}"";
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
    CaptureIds: [0] [1] [2] [3] [4] [5]
    Block[B1] - Block
        Predecessors: [B0]
        Statements (5)
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'p')
              Value: 
                IParameterReferenceOperation: p (OperationKind.ParameterReference, Type: System.String) (Syntax: 'p')

            IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'b2')
              Value: 
                IParameterReferenceOperation: b2 (OperationKind.ParameterReference, Type: System.String) (Syntax: 'b2')

            IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: '20')
              Value: 
                ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 20) (Syntax: '20')

            IFlowCaptureOperation: 3 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'b3')
              Value: 
                IParameterReferenceOperation: b3 (OperationKind.ParameterReference, Type: System.String) (Syntax: 'b3')

            IFlowCaptureOperation: 4 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: '21')
              Value: 
                ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 21) (Syntax: '21')

        Jump if False (Regular) to Block[B3]
            IParameterReferenceOperation: a (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'a')

        Next (Regular) Block[B2]
    Block[B2] - Block
        Predecessors: [B1]
        Statements (1)
            IFlowCaptureOperation: 5 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'b')
              Value: 
                IParameterReferenceOperation: b (OperationKind.ParameterReference, Type: System.String) (Syntax: 'b')

        Next (Regular) Block[B4]
    Block[B3] - Block
        Predecessors: [B1]
        Statements (1)
            IFlowCaptureOperation: 5 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'c')
              Value: 
                IParameterReferenceOperation: c (OperationKind.ParameterReference, Type: System.String) (Syntax: 'c')

        Next (Regular) Block[B4]
    Block[B4] - Block
        Predecessors: [B2] [B3]
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'p = $""{b2,2 ... ? b : c)}"";')
              Expression: 
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.String) (Syntax: 'p = $""{b2,2 ...  ? b : c)}""')
                  Left: 
                    IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.String, IsImplicit) (Syntax: 'p')
                  Right: 
                    IInterpolatedStringOperation (OperationKind.InterpolatedString, Type: System.String) (Syntax: '$""{b2,20:D3 ...  ? b : c)}""')
                      Parts(3):
                          IInterpolationOperation (OperationKind.Interpolation, Type: null) (Syntax: '{b2,20:D3}')
                            Expression: 
                              IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.String, IsImplicit) (Syntax: 'b2')
                            Alignment: 
                              IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: System.Int32, Constant: 20, IsImplicit) (Syntax: '20')
                            FormatString: 
                              ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: ""D3"") (Syntax: ':D3')
                          IInterpolationOperation (OperationKind.Interpolation, Type: null) (Syntax: '{b3,21:D4}')
                            Expression: 
                              IFlowCaptureReferenceOperation: 3 (OperationKind.FlowCaptureReference, Type: System.String, IsImplicit) (Syntax: 'b3')
                            Alignment: 
                              IFlowCaptureReferenceOperation: 4 (OperationKind.FlowCaptureReference, Type: System.Int32, Constant: 21, IsImplicit) (Syntax: '21')
                            FormatString: 
                              ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: ""D4"") (Syntax: ':D4')
                          IInterpolationOperation (OperationKind.Interpolation, Type: null) (Syntax: '{(a ? b : c)}')
                            Expression: 
                              IFlowCaptureReferenceOperation: 5 (OperationKind.FlowCaptureReference, Type: System.String, IsImplicit) (Syntax: 'a ? b : c')
                            Alignment: 
                              null
                            FormatString: 
                              null

        Next (Regular) Block[B5]
            Leaving: {R1}
}

Block[B5] - Exit
    Predecessors: [B4]
    Statements (0)
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(source, expectedFlowGraph, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void InterpolatedStringExpression_NestedInterpolation_Flow()
        {
            string source = @"
using System;

internal class Class
{
    public void M(string a, int? b, int c)
    /*<bind>*/{
        a = $""{$""{b ?? c}""}"";
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
    CaptureIds: [0] [2]
    Block[B1] - Block
        Predecessors: [B0]
        Statements (1)
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'a')
              Value: 
                IParameterReferenceOperation: a (OperationKind.ParameterReference, Type: System.String) (Syntax: 'a')

        Next (Regular) Block[B2]
            Entering: {R2}

    .locals {R2}
    {
        CaptureIds: [1]
        Block[B2] - Block
            Predecessors: [B1]
            Statements (1)
                IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'b')
                  Value: 
                    IParameterReferenceOperation: b (OperationKind.ParameterReference, Type: System.Int32?) (Syntax: 'b')

            Jump if True (Regular) to Block[B4]
                IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsImplicit) (Syntax: 'b')
                  Operand: 
                    IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.Int32?, IsImplicit) (Syntax: 'b')
                Leaving: {R2}

            Next (Regular) Block[B3]
        Block[B3] - Block
            Predecessors: [B2]
            Statements (1)
                IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'b')
                  Value: 
                    IInvocationOperation ( System.Int32 System.Int32?.GetValueOrDefault()) (OperationKind.Invocation, Type: System.Int32, IsImplicit) (Syntax: 'b')
                      Instance Receiver: 
                        IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.Int32?, IsImplicit) (Syntax: 'b')
                      Arguments(0)

            Next (Regular) Block[B5]
                Leaving: {R2}
    }

    Block[B4] - Block
        Predecessors: [B2]
        Statements (1)
            IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'c')
              Value: 
                IParameterReferenceOperation: c (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'c')

        Next (Regular) Block[B5]
    Block[B5] - Block
        Predecessors: [B3] [B4]
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'a = $""{$""{b ?? c}""}"";')
              Expression: 
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.String) (Syntax: 'a = $""{$""{b ?? c}""}""')
                  Left: 
                    IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.String, IsImplicit) (Syntax: 'a')
                  Right: 
                    IInterpolatedStringOperation (OperationKind.InterpolatedString, Type: System.String) (Syntax: '$""{$""{b ?? c}""}""')
                      Parts(1):
                          IInterpolationOperation (OperationKind.Interpolation, Type: null) (Syntax: '{$""{b ?? c}""}')
                            Expression: 
                              IInterpolatedStringOperation (OperationKind.InterpolatedString, Type: System.String) (Syntax: '$""{b ?? c}""')
                                Parts(1):
                                    IInterpolationOperation (OperationKind.Interpolation, Type: null) (Syntax: '{b ?? c}')
                                      Expression: 
                                        IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'b ?? c')
                                      Alignment: 
                                        null
                                      FormatString: 
                                        null
                            Alignment: 
                              null
                            FormatString: 
                              null

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
        public void InterpolatedStringExpression_ConditionalCodeInAlignment_Flow()
        {
            string source = @"
using System;

internal class Class
{
    public void M(bool a, int b, int c, string d, string p)
    /*<bind>*/{
        p = $""{d,(a ? b : c)}"";
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
        Statements (2)
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'p')
              Value: 
                IParameterReferenceOperation: p (OperationKind.ParameterReference, Type: System.String) (Syntax: 'p')

            IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'd')
              Value: 
                IParameterReferenceOperation: d (OperationKind.ParameterReference, Type: System.String) (Syntax: 'd')

        Jump if False (Regular) to Block[B3]
            IParameterReferenceOperation: a (OperationKind.ParameterReference, Type: System.Boolean, IsInvalid) (Syntax: 'a')

        Next (Regular) Block[B2]
    Block[B2] - Block
        Predecessors: [B1]
        Statements (1)
            IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsInvalid, IsImplicit) (Syntax: 'b')
              Value: 
                IParameterReferenceOperation: b (OperationKind.ParameterReference, Type: System.Int32, IsInvalid) (Syntax: 'b')

        Next (Regular) Block[B4]
    Block[B3] - Block
        Predecessors: [B1]
        Statements (1)
            IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsInvalid, IsImplicit) (Syntax: 'c')
              Value: 
                IParameterReferenceOperation: c (OperationKind.ParameterReference, Type: System.Int32, IsInvalid) (Syntax: 'c')

        Next (Regular) Block[B4]
    Block[B4] - Block
        Predecessors: [B2] [B3]
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null, IsInvalid) (Syntax: 'p = $""{d,(a ? b : c)}"";')
              Expression: 
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.String, IsInvalid) (Syntax: 'p = $""{d,(a ? b : c)}""')
                  Left: 
                    IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.String, IsImplicit) (Syntax: 'p')
                  Right: 
                    IInterpolatedStringOperation (OperationKind.InterpolatedString, Type: System.String, IsInvalid) (Syntax: '$""{d,(a ? b : c)}""')
                      Parts(1):
                          IInterpolationOperation (OperationKind.Interpolation, Type: null, IsInvalid) (Syntax: '{d,(a ? b : c)}')
                            Expression: 
                              IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.String, IsImplicit) (Syntax: 'd')
                            Alignment: 
                              IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: System.Int32, IsInvalid, IsImplicit) (Syntax: 'a ? b : c')
                            FormatString: 
                              null

        Next (Regular) Block[B5]
            Leaving: {R1}
}

Block[B5] - Exit
    Predecessors: [B4]
    Statements (0)
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // file.cs(8,18): error CS0150: A constant value is expected
                //         p = $"{d,(a ? b : c)}";
                Diagnostic(ErrorCode.ERR_ConstantExpected, "(a ? b : c)").WithLocation(8, 18)
            };

            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(source, expectedFlowGraph, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void InterpolatedStringExpression_ConditionalCodeInAlignment_Flow_02()
        {
            string source = @"
using System;

internal class Class
{
    public void M(bool a, string b, string c, string c2, string p)
    /*<bind>*/{
        p = $""{c2,(a ? b : c):D3}"";
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
        Statements (2)
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'p')
              Value: 
                IParameterReferenceOperation: p (OperationKind.ParameterReference, Type: System.String) (Syntax: 'p')

            IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'c2')
              Value: 
                IParameterReferenceOperation: c2 (OperationKind.ParameterReference, Type: System.String) (Syntax: 'c2')

        Jump if False (Regular) to Block[B3]
            IParameterReferenceOperation: a (OperationKind.ParameterReference, Type: System.Boolean, IsInvalid) (Syntax: 'a')

        Next (Regular) Block[B2]
    Block[B2] - Block
        Predecessors: [B1]
        Statements (1)
            IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsInvalid, IsImplicit) (Syntax: 'b')
              Value: 
                IParameterReferenceOperation: b (OperationKind.ParameterReference, Type: System.String, IsInvalid) (Syntax: 'b')

        Next (Regular) Block[B4]
    Block[B3] - Block
        Predecessors: [B1]
        Statements (1)
            IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsInvalid, IsImplicit) (Syntax: 'c')
              Value: 
                IParameterReferenceOperation: c (OperationKind.ParameterReference, Type: System.String, IsInvalid) (Syntax: 'c')

        Next (Regular) Block[B4]
    Block[B4] - Block
        Predecessors: [B2] [B3]
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null, IsInvalid) (Syntax: 'p = $""{c2,( ...  : c):D3}"";')
              Expression: 
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.String, IsInvalid) (Syntax: 'p = $""{c2,( ... b : c):D3}""')
                  Left: 
                    IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.String, IsImplicit) (Syntax: 'p')
                  Right: 
                    IInterpolatedStringOperation (OperationKind.InterpolatedString, Type: System.String, IsInvalid) (Syntax: '$""{c2,(a ? b : c):D3}""')
                      Parts(1):
                          IInterpolationOperation (OperationKind.Interpolation, Type: null, IsInvalid) (Syntax: '{c2,(a ? b : c):D3}')
                            Expression: 
                              IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.String, IsImplicit) (Syntax: 'c2')
                            Alignment: 
                              IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Int32, IsInvalid, IsImplicit) (Syntax: 'a ? b : c')
                                Conversion: CommonConversion (Exists: False, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                  (NoConversion)
                                Operand: 
                                  IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: System.String, IsInvalid, IsImplicit) (Syntax: 'a ? b : c')
                            FormatString: 
                              ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: ""D3"") (Syntax: ':D3')

        Next (Regular) Block[B5]
            Leaving: {R1}
}

Block[B5] - Exit
    Predecessors: [B4]
    Statements (0)
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // file.cs(8,20): error CS0029: Cannot implicitly convert type 'string' to 'int'
                //         p = $"{c2,(a ? b : c):D3}";
                Diagnostic(ErrorCode.ERR_NoImplicitConv, "a ? b : c").WithArguments("string", "int").WithLocation(8, 20)
            };

            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(source, expectedFlowGraph, expectedDiagnostics);
        }
    }
}
