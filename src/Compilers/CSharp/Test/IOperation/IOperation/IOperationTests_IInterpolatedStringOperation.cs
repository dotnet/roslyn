// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    public class IOperationTests_IInterpolatedStringExpression : SemanticModelTestBase
    {
        private static CSharpTestSource GetSource(string code, bool hasDefaultHandler)
            => hasDefaultHandler
                ? new[] { code, GetInterpolatedStringHandlerDefinition(includeSpanOverloads: false, useDefaultParameters: false, useBoolReturns: false) }
                : code;

        [CompilerTrait(CompilerFeature.IOperation)]
        [Theory, WorkItem(18300, "https://github.com/dotnet/roslyn/issues/18300")]
        [CombinatorialData]
        public void InterpolatedStringExpression_Empty(bool hasDefaultHandler)
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
IInterpolatedStringOperation (OperationKind.InterpolatedString, Type: System.String, Constant: """") (Syntax: '$""""')
  Parts(0)
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<InterpolatedStringExpressionSyntax>(GetSource(source, hasDefaultHandler), expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Theory, WorkItem(18300, "https://github.com/dotnet/roslyn/issues/18300")]
        [CombinatorialData]
        public void InterpolatedStringExpression_OnlyTextPart(bool hasDefaultHandler)
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
IInterpolatedStringOperation (OperationKind.InterpolatedString, Type: System.String, Constant: ""Only text part"") (Syntax: '$""Only text part""')
  Parts(1):
      IInterpolatedStringTextOperation (OperationKind.InterpolatedStringText, Type: null) (Syntax: 'Only text part')
        Text: 
          ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: ""Only text part"", IsImplicit) (Syntax: 'Only text part')
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<InterpolatedStringExpressionSyntax>(GetSource(source, hasDefaultHandler), expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Theory, WorkItem(18300, "https://github.com/dotnet/roslyn/issues/18300")]
        [CombinatorialData]
        public void InterpolatedStringExpression_OnlyInterpolationPart(bool hasDefaultHandler)
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

            VerifyOperationTreeAndDiagnosticsForTest<InterpolatedStringExpressionSyntax>(GetSource(source, hasDefaultHandler), expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Theory, WorkItem(18300, "https://github.com/dotnet/roslyn/issues/18300")]
        [CombinatorialData]
        public void InterpolatedStringExpression_EmptyInterpolationPart(bool hasDefaultHandler)
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

            VerifyOperationTreeAndDiagnosticsForTest<InterpolatedStringExpressionSyntax>(GetSource(source, hasDefaultHandler), expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Theory, WorkItem(18300, "https://github.com/dotnet/roslyn/issues/18300")]
        [CombinatorialData]
        public void InterpolatedStringExpression_TextAndInterpolationParts(bool hasDefaultHandler)
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

            VerifyOperationTreeAndDiagnosticsForTest<InterpolatedStringExpressionSyntax>(GetSource(source, hasDefaultHandler), expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Theory, WorkItem(18300, "https://github.com/dotnet/roslyn/issues/18300")]
        [CombinatorialData]
        public void InterpolatedStringExpression_FormatAndAlignment(bool hasDefaultHandler)
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

            VerifyOperationTreeAndDiagnosticsForTest<InterpolatedStringExpressionSyntax>(GetSource(source, hasDefaultHandler), expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Theory, WorkItem(18300, "https://github.com/dotnet/roslyn/issues/18300")]
        [CombinatorialData]
        public void InterpolatedStringExpression_InterpolationAndFormatAndAlignment(bool hasDefaultHandler)
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

            VerifyOperationTreeAndDiagnosticsForTest<InterpolatedStringExpressionSyntax>(GetSource(source, hasDefaultHandler), expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Theory, WorkItem(18300, "https://github.com/dotnet/roslyn/issues/18300")]
        [CombinatorialData]
        public void InterpolatedStringExpression_InvocationInInterpolation(bool hasDefaultHandler)
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

            VerifyOperationTreeAndDiagnosticsForTest<InterpolatedStringExpressionSyntax>(GetSource(source, hasDefaultHandler), expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Theory, WorkItem(18300, "https://github.com/dotnet/roslyn/issues/18300")]
        [CombinatorialData]
        public void InterpolatedStringExpression_NestedInterpolation(bool hasDefaultHandler)
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

            VerifyOperationTreeAndDiagnosticsForTest<InterpolatedStringExpressionSyntax>(GetSource(source, hasDefaultHandler), expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Theory, WorkItem(18300, "https://github.com/dotnet/roslyn/issues/18300")]
        [CombinatorialData]
        public void InterpolatedStringExpression_InvalidExpressionInInterpolation(bool hasDefaultHandler)
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

            VerifyOperationTreeAndDiagnosticsForTest<InterpolatedStringExpressionSyntax>(GetSource(source, hasDefaultHandler), expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Theory]
        [CombinatorialData]
        public void InterpolatedStringExpression_Empty_Flow(bool hasDefaultHandler)
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
                IInterpolatedStringOperation (OperationKind.InterpolatedString, Type: System.String, Constant: """") (Syntax: '$""""')
                  Parts(0)

    Next (Regular) Block[B2]
Block[B2] - Exit
    Predecessors: [B1]
    Statements (0)
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(GetSource(source, hasDefaultHandler), expectedFlowGraph, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Theory]
        [CombinatorialData]
        public void InterpolatedStringExpression_OnlyTextPart_Flow(bool hasDefaultHandler)
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
                IInterpolatedStringOperation (OperationKind.InterpolatedString, Type: System.String, Constant: ""Only text part"") (Syntax: '$""Only text part""')
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

            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(GetSource(source, hasDefaultHandler), expectedFlowGraph, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Theory]
        [CombinatorialData]
        public void InterpolatedStringExpression_OnlyInterpolationPart_Flow(bool hasDefaultHandler)
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

            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(GetSource(source, hasDefaultHandler), expectedFlowGraph, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Theory]
        [CombinatorialData]
        public void InterpolatedStringExpression_MultipleInterpolationParts_Flow(bool hasDefaultHandler)
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

            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(GetSource(source, hasDefaultHandler), expectedFlowGraph, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Theory]
        [CombinatorialData]
        public void InterpolatedStringExpression_TextAndInterpolationParts_Flow(bool hasDefaultHandler)
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

            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(GetSource(source, hasDefaultHandler), expectedFlowGraph, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Theory]
        [CombinatorialData]
        public void InterpolatedStringExpression_FormatAndAlignment_Flow(bool hasDefaultHandler)
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

            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(GetSource(source, hasDefaultHandler), expectedFlowGraph, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Theory]
        [CombinatorialData]
        public void InterpolatedStringExpression_FormatAndAlignment_Flow_02(bool hasDefaultHandler)
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

            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(GetSource(source, hasDefaultHandler), expectedFlowGraph, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Theory]
        [CombinatorialData]
        public void InterpolatedStringExpression_FormatAndAlignment_Flow_03(bool hasDefaultHandler)
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

            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(GetSource(source, hasDefaultHandler), expectedFlowGraph, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Theory]
        [CombinatorialData]
        public void InterpolatedStringExpression_NestedInterpolation_Flow(bool hasDefaultHandler)
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

            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(GetSource(source, hasDefaultHandler), expectedFlowGraph, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Theory]
        [CombinatorialData]
        public void InterpolatedStringExpression_ConditionalCodeInAlignment_Flow(bool hasDefaultHandler)
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

            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(GetSource(source, hasDefaultHandler), expectedFlowGraph, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Theory]
        [CombinatorialData]
        public void InterpolatedStringExpression_ConditionalCodeInAlignment_Flow_02(bool hasDefaultHandler)
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

            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(GetSource(source, hasDefaultHandler), expectedFlowGraph, expectedDiagnostics);
        }

        [Fact]
        public void InterpolatedStringHandlerConversion_01()
        {
            var code = @"
int i = 0;
CustomHandler /*<bind>*/c = $""literal{i,1:test}""/*</bind>*/;
";

            var handler = GetInterpolatedStringCustomHandlerType("CustomHandler", "struct", useBoolReturns: false);

            var expectedDiagnostics = DiagnosticDescription.None;

            string expectedOperationTree = @"
IVariableDeclaratorOperation (Symbol: CustomHandler c) (OperationKind.VariableDeclarator, Type: null) (Syntax: 'c = $""literal{i,1:test}""')
  Initializer: 
    IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null) (Syntax: '= $""literal{i,1:test}""')
      IOperation:  (OperationKind.None, Type: CustomHandler, IsImplicit) (Syntax: '$""literal{i,1:test}""')
        Children(1):
            IInterpolatedStringOperation (OperationKind.InterpolatedString, Type: System.String) (Syntax: '$""literal{i,1:test}""')
              Parts(2):
                  IInterpolatedStringTextOperation (OperationKind.InterpolatedStringText, Type: null) (Syntax: 'literal')
                    Text: 
                      ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: ""literal"", IsImplicit) (Syntax: 'literal')
                  IInterpolationOperation (OperationKind.Interpolation, Type: null) (Syntax: '{i,1:test}')
                    Expression: 
                      IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Object, IsImplicit) (Syntax: 'i')
                        Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        Operand: 
                          ILocalReferenceOperation: i (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'i')
                    Alignment: 
                      ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
                    FormatString: 
                      ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: ""test"") (Syntax: ':test')
";

            VerifyOperationTreeAndDiagnosticsForTest<VariableDeclaratorSyntax>(new[] { code, handler }, expectedOperationTree, expectedDiagnostics, parseOptions: TestOptions.RegularPreview);
        }

        [Fact]
        public void InterpolatedStringHandlerConversion_02()
        {
            var code = @"
int i = 0;
CustomHandler /*<bind>*/c = $""literal{i,1}""/*</bind>*/;
";

            var handler = GetInterpolatedStringCustomHandlerType("CustomHandler", "struct", useBoolReturns: false);

            var expectedDiagnostics = DiagnosticDescription.None;

            string expectedOperationTree = @"
IVariableDeclaratorOperation (Symbol: CustomHandler c) (OperationKind.VariableDeclarator, Type: null) (Syntax: 'c = $""literal{i,1}""')
  Initializer: 
    IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null) (Syntax: '= $""literal{i,1}""')
      IOperation:  (OperationKind.None, Type: CustomHandler, IsImplicit) (Syntax: '$""literal{i,1}""')
        Children(1):
            IInterpolatedStringOperation (OperationKind.InterpolatedString, Type: System.String) (Syntax: '$""literal{i,1}""')
              Parts(2):
                  IInterpolatedStringTextOperation (OperationKind.InterpolatedStringText, Type: null) (Syntax: 'literal')
                    Text: 
                      ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: ""literal"", IsImplicit) (Syntax: 'literal')
                  IInterpolationOperation (OperationKind.Interpolation, Type: null) (Syntax: '{i,1}')
                    Expression: 
                      IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Object, IsImplicit) (Syntax: 'i')
                        Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        Operand: 
                          ILocalReferenceOperation: i (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'i')
                    Alignment: 
                      ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
                    FormatString: 
                      null
";

            VerifyOperationTreeAndDiagnosticsForTest<VariableDeclaratorSyntax>(new[] { code, handler }, expectedOperationTree, expectedDiagnostics, parseOptions: TestOptions.RegularPreview);
        }

        [Fact]
        public void InterpolatedStringHandlerConversion_03()
        {
            var code = @"
int i = 0;
CustomHandler /*<bind>*/c = $""literal{i:test}""/*</bind>*/;
";

            var handler = GetInterpolatedStringCustomHandlerType("CustomHandler", "struct", useBoolReturns: false);

            var expectedDiagnostics = DiagnosticDescription.None;

            string expectedOperationTree = @"
IVariableDeclaratorOperation (Symbol: CustomHandler c) (OperationKind.VariableDeclarator, Type: null) (Syntax: 'c = $""literal{i:test}""')
  Initializer: 
    IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null) (Syntax: '= $""literal{i:test}""')
      IOperation:  (OperationKind.None, Type: CustomHandler, IsImplicit) (Syntax: '$""literal{i:test}""')
        Children(1):
            IInterpolatedStringOperation (OperationKind.InterpolatedString, Type: System.String) (Syntax: '$""literal{i:test}""')
              Parts(2):
                  IInterpolatedStringTextOperation (OperationKind.InterpolatedStringText, Type: null) (Syntax: 'literal')
                    Text: 
                      ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: ""literal"", IsImplicit) (Syntax: 'literal')
                  IInterpolationOperation (OperationKind.Interpolation, Type: null) (Syntax: '{i:test}')
                    Expression: 
                      IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Object, IsImplicit) (Syntax: 'i')
                        Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        Operand: 
                          ILocalReferenceOperation: i (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'i')
                    Alignment: 
                      null
                    FormatString: 
                      ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: ""test"") (Syntax: ':test')
";

            VerifyOperationTreeAndDiagnosticsForTest<VariableDeclaratorSyntax>(new[] { code, handler }, expectedOperationTree, expectedDiagnostics, parseOptions: TestOptions.RegularPreview);
        }

        [Fact]
        public void InterpolatedStringHandlerConversion_04()
        {
            var code = @"
int i = 0;
CustomHandler /*<bind>*/c = $""literal{i}""/*</bind>*/;
";

            var handler = GetInterpolatedStringCustomHandlerType("CustomHandler", "struct", useBoolReturns: false);

            var expectedDiagnostics = DiagnosticDescription.None;

            string expectedOperationTree = @"
IVariableDeclaratorOperation (Symbol: CustomHandler c) (OperationKind.VariableDeclarator, Type: null) (Syntax: 'c = $""literal{i}""')
  Initializer: 
    IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null) (Syntax: '= $""literal{i}""')
      IOperation:  (OperationKind.None, Type: CustomHandler, IsImplicit) (Syntax: '$""literal{i}""')
        Children(1):
            IInterpolatedStringOperation (OperationKind.InterpolatedString, Type: System.String) (Syntax: '$""literal{i}""')
              Parts(2):
                  IInterpolatedStringTextOperation (OperationKind.InterpolatedStringText, Type: null) (Syntax: 'literal')
                    Text: 
                      ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: ""literal"", IsImplicit) (Syntax: 'literal')
                  IInterpolationOperation (OperationKind.Interpolation, Type: null) (Syntax: '{i}')
                    Expression: 
                      IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Object, IsImplicit) (Syntax: 'i')
                        Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        Operand: 
                          ILocalReferenceOperation: i (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'i')
                    Alignment: 
                      null
                    FormatString: 
                      null
";

            VerifyOperationTreeAndDiagnosticsForTest<VariableDeclaratorSyntax>(new[] { code, handler }, expectedOperationTree, expectedDiagnostics, parseOptions: TestOptions.RegularPreview);
        }

        [Fact]
        public void InterpolatedStringHandlerConversion_05()
        {
            var code = @"
using System.Runtime.CompilerServices;
int i = 0;
CustomHandler /*<bind>*/c = $""literal{i}""/*</bind>*/;

[InterpolatedStringHandler]
public struct CustomHandler {}
";

            var expectedDiagnostics = new[]
            {
                // (4,29): error CS1729: 'CustomHandler' does not contain a constructor that takes 2 arguments
                // CustomHandler /*<bind>*/c = $"literal{i}"/*</bind>*/;
                Diagnostic(ErrorCode.ERR_BadCtorArgCount, @"$""literal{i}""").WithArguments("CustomHandler", "2").WithLocation(4, 29),
                // (4,29): error CS1729: 'CustomHandler' does not contain a constructor that takes 3 arguments
                // CustomHandler /*<bind>*/c = $"literal{i}"/*</bind>*/;
                Diagnostic(ErrorCode.ERR_BadCtorArgCount, @"$""literal{i}""").WithArguments("CustomHandler", "3").WithLocation(4, 29),
                // (4,31): error CS1061: 'CustomHandler' does not contain a definition for 'AppendLiteral' and no accessible extension method 'AppendLiteral' accepting a first argument of type 'CustomHandler' could be found (are you missing a using directive or an assembly reference?)
                // CustomHandler /*<bind>*/c = $"literal{i}"/*</bind>*/;
                Diagnostic(ErrorCode.ERR_NoSuchMemberOrExtension, "literal").WithArguments("CustomHandler", "AppendLiteral").WithLocation(4, 31),
                // (4,31): error CS8941: Interpolated string handler method '?.()' is malformed. It does not return 'void' or 'bool'.
                // CustomHandler /*<bind>*/c = $"literal{i}"/*</bind>*/;
                Diagnostic(ErrorCode.ERR_InterpolatedStringHandlerMethodReturnMalformed, "literal").WithArguments("?.()").WithLocation(4, 31),
                // (4,38): error CS1061: 'CustomHandler' does not contain a definition for 'AppendFormatted' and no accessible extension method 'AppendFormatted' accepting a first argument of type 'CustomHandler' could be found (are you missing a using directive or an assembly reference?)
                // CustomHandler /*<bind>*/c = $"literal{i}"/*</bind>*/;
                Diagnostic(ErrorCode.ERR_NoSuchMemberOrExtension, "{i}").WithArguments("CustomHandler", "AppendFormatted").WithLocation(4, 38),
                // (4,38): error CS8941: Interpolated string handler method '?.()' is malformed. It does not return 'void' or 'bool'.
                // CustomHandler /*<bind>*/c = $"literal{i}"/*</bind>*/;
                Diagnostic(ErrorCode.ERR_InterpolatedStringHandlerMethodReturnMalformed, "{i}").WithArguments("?.()").WithLocation(4, 38)
            };

            string expectedOperationTree = @"
IVariableDeclaratorOperation (Symbol: CustomHandler c) (OperationKind.VariableDeclarator, Type: null, IsInvalid) (Syntax: 'c = $""literal{i}""')
  Initializer: 
    IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null, IsInvalid) (Syntax: '= $""literal{i}""')
      IOperation:  (OperationKind.None, Type: CustomHandler, IsInvalid, IsImplicit) (Syntax: '$""literal{i}""')
        Children(1):
            IInterpolatedStringOperation (OperationKind.InterpolatedString, Type: System.String, IsInvalid) (Syntax: '$""literal{i}""')
              Parts(2):
                  IInterpolatedStringTextOperation (OperationKind.InterpolatedStringText, Type: null, IsInvalid) (Syntax: 'literal')
                    Text: 
                      ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: ""literal"", IsInvalid, IsImplicit) (Syntax: 'literal')
                  IInterpolationOperation (OperationKind.Interpolation, Type: null, IsInvalid) (Syntax: '{i}')
                    Expression: 
                      ILocalReferenceOperation: i (OperationKind.LocalReference, Type: System.Int32, IsInvalid) (Syntax: 'i')
                    Alignment: 
                      null
                    FormatString: 
                      null
";

            VerifyOperationTreeAndDiagnosticsForTest<VariableDeclaratorSyntax>(new[] { code, InterpolatedStringHandlerAttribute }, expectedOperationTree, expectedDiagnostics, parseOptions: TestOptions.RegularPreview);
        }

        [Fact]
        public void InterpolatedStringHandlerConversion_06()
        {
            var code = @"
using System.Runtime.CompilerServices;
int i = 0;
CustomHandler /*<bind>*/c = $""literal{i,1:test}""/*</bind>*/;

[InterpolatedStringHandler]
public struct CustomHandler
{
    public CustomHandler(int literalLength, int formattedCount) {}
    public void AppendFormatted(object o, object alignment, object format) {}
    public void AppendLiteral(object o) {}
}
";

            var expectedDiagnostics = DiagnosticDescription.None;

            string expectedOperationTree = @"
IVariableDeclaratorOperation (Symbol: CustomHandler c) (OperationKind.VariableDeclarator, Type: null) (Syntax: 'c = $""literal{i,1:test}""')
  Initializer: 
    IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null) (Syntax: '= $""literal{i,1:test}""')
      IOperation:  (OperationKind.None, Type: CustomHandler, IsImplicit) (Syntax: '$""literal{i,1:test}""')
        Children(1):
            IInterpolatedStringOperation (OperationKind.InterpolatedString, Type: System.String) (Syntax: '$""literal{i,1:test}""')
              Parts(2):
                  IInterpolatedStringTextOperation (OperationKind.InterpolatedStringText, Type: null) (Syntax: 'literal')
                    Text: 
                      IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Object, IsImplicit) (Syntax: 'literal')
                        Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
                        Operand: 
                          ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: ""literal"", IsImplicit) (Syntax: 'literal')
                  IInterpolationOperation (OperationKind.Interpolation, Type: null) (Syntax: '{i,1:test}')
                    Expression: 
                      IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Object, IsImplicit) (Syntax: 'i')
                        Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        Operand: 
                          ILocalReferenceOperation: i (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'i')
                    Alignment: 
                      IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Object, IsImplicit) (Syntax: '1')
                        Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        Operand: 
                          ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
                    FormatString: 
                      IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Object, IsImplicit) (Syntax: ':test')
                        Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
                        Operand: 
                          ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: ""test"") (Syntax: ':test')
";

            VerifyOperationTreeAndDiagnosticsForTest<VariableDeclaratorSyntax>(new[] { code, InterpolatedStringHandlerAttribute }, expectedOperationTree, expectedDiagnostics, parseOptions: TestOptions.RegularPreview);
        }

        [Fact]
        public void InterpolatedStringHandlerConversion_07()
        {
            var code = @"
using System.Runtime.CompilerServices;
CustomHandler /*<bind>*/c = $""{}""/*</bind>*/;

[InterpolatedStringHandler]
public struct CustomHandler
{
    public CustomHandler(int literalLength, int formattedCount) {}
    public void AppendFormatted(object o, object alignment, object format) {}
    public void AppendLiteral(object o) {}
}
";

            var expectedDiagnostics = new[] {
                // (3,32): error CS1733: Expected expression
                // CustomHandler /*<bind>*/c = $"{}"/*</bind>*/;
                Diagnostic(ErrorCode.ERR_ExpressionExpected, "").WithLocation(3, 32)
            };

            string expectedOperationTree = @"
IVariableDeclaratorOperation (Symbol: CustomHandler c) (OperationKind.VariableDeclarator, Type: null, IsInvalid) (Syntax: 'c = $""{}""')
  Initializer: 
    IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null, IsInvalid) (Syntax: '= $""{}""')
      IOperation:  (OperationKind.None, Type: CustomHandler, IsInvalid, IsImplicit) (Syntax: '$""{}""')
        Children(1):
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

            VerifyOperationTreeAndDiagnosticsForTest<VariableDeclaratorSyntax>(new[] { code, InterpolatedStringHandlerAttribute }, expectedOperationTree, expectedDiagnostics, parseOptions: TestOptions.RegularPreview);
        }

        [Fact]
        public void DynamicConstruction_01()
        {
            var code = @"
using System;
using System.Runtime.CompilerServices;
dynamic d = 1;
M(/*<bind>*/$""literal{d,1:format}""/*</bind>*/);

void M(CustomHandler c) {}

[InterpolatedStringHandler]
public struct CustomHandler
{
    public CustomHandler(int literalLength, int formattedCount)
    {
    }

    public void AppendLiteral(dynamic d)
    {
        Console.WriteLine(""AppendLiteral"");
    }

    public void AppendFormatted(dynamic d, int alignment, string format)
    {
        Console.WriteLine(""AppendFormatted"");
    }
}
";

            var expectedDiagnostics = DiagnosticDescription.None;

            string expectedOperationTree = @"
IInterpolatedStringOperation (OperationKind.InterpolatedString, Type: System.String) (Syntax: '$""literal{d,1:format}""')
  Parts(2):
      IInterpolatedStringTextOperation (OperationKind.InterpolatedStringText, Type: null) (Syntax: 'literal')
        Text:
          IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: dynamic, IsImplicit) (Syntax: 'literal')
            Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
            Operand:
              ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: ""literal"", IsImplicit) (Syntax: 'literal')
      IInterpolationOperation (OperationKind.Interpolation, Type: null) (Syntax: '{d,1:format}')
        Expression:
          ILocalReferenceOperation: d (OperationKind.LocalReference, Type: dynamic) (Syntax: 'd')
        Alignment:
          ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
        FormatString:
          ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: ""format"") (Syntax: ':format')
";

            VerifyOperationTreeAndDiagnosticsForTest<InterpolatedStringExpressionSyntax>(new[] { code, InterpolatedStringHandlerAttribute }, expectedOperationTree, expectedDiagnostics, parseOptions: TestOptions.RegularPreview);
        }

        [Fact]
        public void DynamicConstruction_02()
        {
            var code = @"
using System.Runtime.CompilerServices;
dynamic d = 1;
M(d, /*<bind>*/$""{1}literal""/*</bind>*/);

void M(dynamic d, [InterpolatedStringHandlerArgument(""d"")]CustomHandler c) {}

public partial struct CustomHandler
{
    public CustomHandler(int literalLength, int formattedCount, dynamic d) : this() {}
}
";

            var handler = GetInterpolatedStringCustomHandlerType("CustomHandler", "partial struct", useBoolReturns: false);

            var expectedDiagnostics = new[] {
                // (4,16): error CS8953: An interpolated string handler construction cannot use dynamic. Manually construct an instance of 'CustomHandler'.
                // M(d, /*<bind>*/$"{1}literal"/*</bind>*/);
                Diagnostic(ErrorCode.ERR_InterpolatedStringHandlerCreationCannotUseDynamic, @"$""{1}literal""").WithArguments("CustomHandler").WithLocation(4, 16)
            };

            string expectedOperationTree = @"
IInterpolatedStringOperation (OperationKind.InterpolatedString, Type: System.String, IsInvalid) (Syntax: '$""{1}literal""')
  Parts(2):
      IInterpolationOperation (OperationKind.Interpolation, Type: null, IsInvalid) (Syntax: '{1}')
        Expression:
          IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Object, IsInvalid, IsImplicit) (Syntax: '1')
            Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
            Operand:
              ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1, IsInvalid) (Syntax: '1')
        Alignment:
          null
        FormatString:
          null
      IInterpolatedStringTextOperation (OperationKind.InterpolatedStringText, Type: null, IsInvalid) (Syntax: 'literal')
        Text:
          ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: ""literal"", IsInvalid, IsImplicit) (Syntax: 'literal')
";

            VerifyOperationTreeAndDiagnosticsForTest<InterpolatedStringExpressionSyntax>(new[] { code, handler, InterpolatedStringHandlerArgumentAttribute }, expectedOperationTree, expectedDiagnostics, parseOptions: TestOptions.RegularPreview);
        }

        [Fact, WorkItem(54703, "https://github.com/dotnet/roslyn/issues/54703")]
        public void InterpolationEscapeConstantValue_WithDefaultHandler()
        {
            var code = @"
int i = 1;
System.Console.WriteLine(/*<bind>*/$""{{ {i} }}""/*</bind>*/);";

            var expectedDiagnostics = DiagnosticDescription.None;

            string expectedOperationTree = @"
IInterpolatedStringOperation (OperationKind.InterpolatedString, Type: System.String) (Syntax: '$""{{ {i} }}""')
  Parts(3):
      IInterpolatedStringTextOperation (OperationKind.InterpolatedStringText, Type: null) (Syntax: '{{ ')
        Text:
          ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: ""{ "", IsImplicit) (Syntax: '{{ ')
      IInterpolationOperation (OperationKind.Interpolation, Type: null) (Syntax: '{i}')
        Expression:
          ILocalReferenceOperation: i (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'i')
        Alignment:
          null
        FormatString:
          null
      IInterpolatedStringTextOperation (OperationKind.InterpolatedStringText, Type: null) (Syntax: ' }}')
        Text:
          ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: "" }"", IsImplicit) (Syntax: ' }}')
";

            VerifyOperationTreeAndDiagnosticsForTest<InterpolatedStringExpressionSyntax>(new[] { code, GetInterpolatedStringHandlerDefinition(includeSpanOverloads: false, useDefaultParameters: false, useBoolReturns: false) }, expectedOperationTree, expectedDiagnostics, parseOptions: TestOptions.RegularPreview);
        }

        [Fact, WorkItem(54703, "https://github.com/dotnet/roslyn/issues/54703")]
        public void InterpolationEscapeConstantValue_WithoutDefaultHandler()
        {
            var code = @"
int i = 1;
System.Console.WriteLine(/*<bind>*/$""{{ {i} }}""/*</bind>*/);";

            var expectedDiagnostics = DiagnosticDescription.None;

            // The difference between this test and the previous one is the constant value of the ILiteralOperations. When handlers are involved, the
            // constant value is { and }. When they are not involved, the constant values are {{ and }}
            string expectedOperationTree = @"
IInterpolatedStringOperation (OperationKind.InterpolatedString, Type: System.String) (Syntax: '$""{{ {i} }}""')
  Parts(3):
      IInterpolatedStringTextOperation (OperationKind.InterpolatedStringText, Type: null) (Syntax: '{{ ')
        Text:
          ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: ""{{ "", IsImplicit) (Syntax: '{{ ')
      IInterpolationOperation (OperationKind.Interpolation, Type: null) (Syntax: '{i}')
        Expression:
          ILocalReferenceOperation: i (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'i')
        Alignment:
          null
        FormatString:
          null
      IInterpolatedStringTextOperation (OperationKind.InterpolatedStringText, Type: null) (Syntax: ' }}')
        Text:
          ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: "" }}"", IsImplicit) (Syntax: ' }}')
";

            VerifyOperationTreeAndDiagnosticsForTest<InterpolatedStringExpressionSyntax>(new[] { code }, expectedOperationTree, expectedDiagnostics, parseOptions: TestOptions.RegularPreview);
        }

        [Fact]
        public void InterpolatedStringsAddedUnderObjectAddition_DefiniteAssignment()
        {
            var code = @"
#pragma warning disable CS0219 // Unused local
object o1;
object o2;
object o3;
_ = /*<bind>*/$""{o1 = null}"" + $""{o2 = null}"" + $""{o3 = null}"" + 1/*</bind>*/;
";

            var comp = CreateCompilation(new[] { code, GetInterpolatedStringHandlerDefinition(includeSpanOverloads: false, useDefaultParameters: false, useBoolReturns: true) });

            var expectedDiagnostics = DiagnosticDescription.None;

            string expectedOperationTree = @"
IBinaryOperation (BinaryOperatorKind.Add) (OperationKind.Binary, Type: System.String) (Syntax: '$""{o1 = nul ...  null}"" + 1')
  Left:
    IBinaryOperation (BinaryOperatorKind.Add) (OperationKind.Binary, Type: System.String) (Syntax: '$""{o1 = nul ... o3 = null}""')
      Left:
        IBinaryOperation (BinaryOperatorKind.Add) (OperationKind.Binary, Type: System.String) (Syntax: '$""{o1 = nul ... o2 = null}""')
          Left:
            IInterpolatedStringOperation (OperationKind.InterpolatedString, Type: System.String) (Syntax: '$""{o1 = null}""')
              Parts(1):
                  IInterpolationOperation (OperationKind.Interpolation, Type: null) (Syntax: '{o1 = null}')
                    Expression:
                      ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Object) (Syntax: 'o1 = null')
                        Left:
                          ILocalReferenceOperation: o1 (OperationKind.LocalReference, Type: System.Object) (Syntax: 'o1')
                        Right:
                          IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Object, Constant: null, IsImplicit) (Syntax: 'null')
                            Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
                            Operand:
                              ILiteralOperation (OperationKind.Literal, Type: null, Constant: null) (Syntax: 'null')
                    Alignment:
                      null
                    FormatString:
                      null
          Right:
            IInterpolatedStringOperation (OperationKind.InterpolatedString, Type: System.String) (Syntax: '$""{o2 = null}""')
              Parts(1):
                  IInterpolationOperation (OperationKind.Interpolation, Type: null) (Syntax: '{o2 = null}')
                    Expression:
                      ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Object) (Syntax: 'o2 = null')
                        Left:
                          ILocalReferenceOperation: o2 (OperationKind.LocalReference, Type: System.Object) (Syntax: 'o2')
                        Right:
                          IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Object, Constant: null, IsImplicit) (Syntax: 'null')
                            Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
                            Operand:
                              ILiteralOperation (OperationKind.Literal, Type: null, Constant: null) (Syntax: 'null')
                    Alignment:
                      null
                    FormatString:
                      null
      Right:
        IInterpolatedStringOperation (OperationKind.InterpolatedString, Type: System.String) (Syntax: '$""{o3 = null}""')
          Parts(1):
              IInterpolationOperation (OperationKind.Interpolation, Type: null) (Syntax: '{o3 = null}')
                Expression:
                  ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Object) (Syntax: 'o3 = null')
                    Left:
                      ILocalReferenceOperation: o3 (OperationKind.LocalReference, Type: System.Object) (Syntax: 'o3')
                    Right:
                      IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Object, Constant: null, IsImplicit) (Syntax: 'null')
                        Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
                        Operand:
                          ILiteralOperation (OperationKind.Literal, Type: null, Constant: null) (Syntax: 'null')
                Alignment:
                  null
                FormatString:
                  null
  Right:
    IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Object, IsImplicit) (Syntax: '1')
      Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
      Operand:
        ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')

";

            VerifyOperationTreeAndDiagnosticsForTest<BinaryExpressionSyntax>(new[] { code, GetInterpolatedStringHandlerDefinition(includeSpanOverloads: false, useDefaultParameters: false, useBoolReturns: true) },
                                                                             expectedOperationTree, expectedDiagnostics, parseOptions: TestOptions.RegularPreview);
        }

        [Fact]
        public void ParenthesizedInterpolatedStringsAdded()
        {
            var code = @"
int i1 = 1;
int i2 = 2;
int i3 = 3;
int i4 = 4;
int i5 = 5;
int i6 = 6;

string s = /*<bind>*/(($""{i1,1:d}"" + $""{i2,2:f}"") + $""{i3,3:g}"") + ($""{i4,4:h}"" + ($""{i5,5:i}"" + $""{i6,6:j}""))/*</bind>*/;
";

            var expectedDiagnostics = DiagnosticDescription.None;
            var expectedOperationTree = @"
IBinaryOperation (BinaryOperatorKind.Add) (OperationKind.Binary, Type: System.String) (Syntax: '(($""{i1,1:d ... {i6,6:j}""))')
  Left:
    IBinaryOperation (BinaryOperatorKind.Add) (OperationKind.Binary, Type: System.String) (Syntax: '($""{i1,1:d} ... $""{i3,3:g}""')
      Left:
        IBinaryOperation (BinaryOperatorKind.Add) (OperationKind.Binary, Type: System.String) (Syntax: '$""{i1,1:d}"" ... $""{i2,2:f}""')
          Left:
            IInterpolatedStringOperation (OperationKind.InterpolatedString, Type: System.String) (Syntax: '$""{i1,1:d}""')
              Parts(1):
                  IInterpolationOperation (OperationKind.Interpolation, Type: null) (Syntax: '{i1,1:d}')
                    Expression:
                      ILocalReferenceOperation: i1 (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'i1')
                    Alignment:
                      ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
                    FormatString:
                      ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: ""d"") (Syntax: ':d')
          Right:
            IInterpolatedStringOperation (OperationKind.InterpolatedString, Type: System.String) (Syntax: '$""{i2,2:f}""')
              Parts(1):
                  IInterpolationOperation (OperationKind.Interpolation, Type: null) (Syntax: '{i2,2:f}')
                    Expression:
                      ILocalReferenceOperation: i2 (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'i2')
                    Alignment:
                      ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 2) (Syntax: '2')
                    FormatString:
                      ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: ""f"") (Syntax: ':f')
      Right:
        IInterpolatedStringOperation (OperationKind.InterpolatedString, Type: System.String) (Syntax: '$""{i3,3:g}""')
          Parts(1):
              IInterpolationOperation (OperationKind.Interpolation, Type: null) (Syntax: '{i3,3:g}')
                Expression:
                  ILocalReferenceOperation: i3 (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'i3')
                Alignment:
                  ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 3) (Syntax: '3')
                FormatString:
                  ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: ""g"") (Syntax: ':g')
  Right:
    IBinaryOperation (BinaryOperatorKind.Add) (OperationKind.Binary, Type: System.String) (Syntax: '$""{i4,4:h}"" ... ""{i6,6:j}"")')
      Left:
        IInterpolatedStringOperation (OperationKind.InterpolatedString, Type: System.String) (Syntax: '$""{i4,4:h}""')
          Parts(1):
              IInterpolationOperation (OperationKind.Interpolation, Type: null) (Syntax: '{i4,4:h}')
                Expression:
                  ILocalReferenceOperation: i4 (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'i4')
                Alignment:
                  ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 4) (Syntax: '4')
                FormatString:
                  ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: ""h"") (Syntax: ':h')
      Right:
        IBinaryOperation (BinaryOperatorKind.Add) (OperationKind.Binary, Type: System.String) (Syntax: '$""{i5,5:i}"" ... $""{i6,6:j}""')
          Left:
            IInterpolatedStringOperation (OperationKind.InterpolatedString, Type: System.String) (Syntax: '$""{i5,5:i}""')
              Parts(1):
                  IInterpolationOperation (OperationKind.Interpolation, Type: null) (Syntax: '{i5,5:i}')
                    Expression:
                      ILocalReferenceOperation: i5 (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'i5')
                    Alignment:
                      ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 5) (Syntax: '5')
                    FormatString:
                      ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: ""i"") (Syntax: ':i')
          Right:
            IInterpolatedStringOperation (OperationKind.InterpolatedString, Type: System.String) (Syntax: '$""{i6,6:j}""')
              Parts(1):
                  IInterpolationOperation (OperationKind.Interpolation, Type: null) (Syntax: '{i6,6:j}')
                    Expression:
                      ILocalReferenceOperation: i6 (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'i6')
                    Alignment:
                      ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 6) (Syntax: '6')
                    FormatString:
                      ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: ""j"") (Syntax: ':j')
";

            VerifyOperationTreeAndDiagnosticsForTest<BinaryExpressionSyntax>(new[] { code, GetInterpolatedStringHandlerDefinition(includeSpanOverloads: false, useDefaultParameters: false, useBoolReturns: true) },
                                                                             expectedOperationTree, expectedDiagnostics, parseOptions: TestOptions.RegularPreview);
        }
    }
}
