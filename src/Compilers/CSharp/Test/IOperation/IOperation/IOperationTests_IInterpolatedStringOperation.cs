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
                IOperation:  (OperationKind.None, Type: Class, IsInvalid) (Syntax: 'Class')
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
      IInterpolatedStringHandlerCreationOperation (HandlerAppendCallsReturnBool: False, HandlerCreationHasSuccessParameter: False) (OperationKind.InterpolatedStringHandlerCreation, Type: CustomHandler, IsImplicit) (Syntax: '$""literal{i,1:test}""')
        Creation:
          IObjectCreationOperation (Constructor: CustomHandler..ctor(System.Int32 literalLength, System.Int32 formattedCount)) (OperationKind.ObjectCreation, Type: CustomHandler, IsImplicit) (Syntax: '$""literal{i,1:test}""')
            Arguments(2):
                IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: literalLength) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: '$""literal{i,1:test}""')
                  ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 7, IsImplicit) (Syntax: '$""literal{i,1:test}""')
                  InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: formattedCount) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: '$""literal{i,1:test}""')
                  ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1, IsImplicit) (Syntax: '$""literal{i,1:test}""')
                  InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
            Initializer:
              null
        Content:
          IInterpolatedStringOperation (OperationKind.InterpolatedString, Type: System.String) (Syntax: '$""literal{i,1:test}""')
            Parts(2):
                IInterpolatedStringAppendOperation (OperationKind.InterpolatedStringAppendLiteral, Type: null, IsImplicit) (Syntax: 'literal')
                  AppendCall:
                    IInvocationOperation ( void CustomHandler.AppendLiteral(System.String literal)) (OperationKind.Invocation, Type: System.Void, IsImplicit) (Syntax: 'literal')
                      Instance Receiver:
                        IInstanceReferenceOperation (ReferenceKind: InterpolatedStringHandler) (OperationKind.InstanceReference, Type: CustomHandler, IsImplicit) (Syntax: '$""literal{i,1:test}""')
                      Arguments(1):
                          IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: literal) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 'literal')
                            ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: ""literal"") (Syntax: 'literal')
                            InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                            OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                IInterpolatedStringAppendOperation (OperationKind.InterpolatedStringAppendFormatted, Type: null, IsImplicit) (Syntax: '{i,1:test}')
                  AppendCall:
                    IInvocationOperation ( void CustomHandler.AppendFormatted(System.Object o, [System.Int32 alignment = 0], [System.String format = null])) (OperationKind.Invocation, Type: System.Void, IsImplicit) (Syntax: '{i,1:test}')
                      Instance Receiver:
                        IInstanceReferenceOperation (ReferenceKind: InterpolatedStringHandler) (OperationKind.InstanceReference, Type: CustomHandler, IsImplicit) (Syntax: '$""literal{i,1:test}""')
                      Arguments(3):
                          IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: o) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 'i')
                            IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Object, IsImplicit) (Syntax: 'i')
                              Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                              Operand:
                                ILocalReferenceOperation: i (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'i')
                            InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                            OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                          IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: alignment) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: '1')
                            ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
                            InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                            OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                          IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: format) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: ':test')
                            ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: ""test"") (Syntax: ':test')
                            InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                            OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
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
      IInterpolatedStringHandlerCreationOperation (HandlerAppendCallsReturnBool: False, HandlerCreationHasSuccessParameter: False) (OperationKind.InterpolatedStringHandlerCreation, Type: CustomHandler, IsImplicit) (Syntax: '$""literal{i,1}""')
        Creation:
          IObjectCreationOperation (Constructor: CustomHandler..ctor(System.Int32 literalLength, System.Int32 formattedCount)) (OperationKind.ObjectCreation, Type: CustomHandler, IsImplicit) (Syntax: '$""literal{i,1}""')
            Arguments(2):
                IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: literalLength) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: '$""literal{i,1}""')
                  ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 7, IsImplicit) (Syntax: '$""literal{i,1}""')
                  InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: formattedCount) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: '$""literal{i,1}""')
                  ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1, IsImplicit) (Syntax: '$""literal{i,1}""')
                  InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
            Initializer:
              null
        Content:
          IInterpolatedStringOperation (OperationKind.InterpolatedString, Type: System.String) (Syntax: '$""literal{i,1}""')
            Parts(2):
                IInterpolatedStringAppendOperation (OperationKind.InterpolatedStringAppendLiteral, Type: null, IsImplicit) (Syntax: 'literal')
                  AppendCall:
                    IInvocationOperation ( void CustomHandler.AppendLiteral(System.String literal)) (OperationKind.Invocation, Type: System.Void, IsImplicit) (Syntax: 'literal')
                      Instance Receiver:
                        IInstanceReferenceOperation (ReferenceKind: InterpolatedStringHandler) (OperationKind.InstanceReference, Type: CustomHandler, IsImplicit) (Syntax: '$""literal{i,1}""')
                      Arguments(1):
                          IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: literal) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 'literal')
                            ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: ""literal"") (Syntax: 'literal')
                            InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                            OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                IInterpolatedStringAppendOperation (OperationKind.InterpolatedStringAppendFormatted, Type: null, IsImplicit) (Syntax: '{i,1}')
                  AppendCall:
                    IInvocationOperation ( void CustomHandler.AppendFormatted(System.Object o, [System.Int32 alignment = 0], [System.String format = null])) (OperationKind.Invocation, Type: System.Void, IsImplicit) (Syntax: '{i,1}')
                      Instance Receiver:
                        IInstanceReferenceOperation (ReferenceKind: InterpolatedStringHandler) (OperationKind.InstanceReference, Type: CustomHandler, IsImplicit) (Syntax: '$""literal{i,1}""')
                      Arguments(3):
                          IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: o) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 'i')
                            IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Object, IsImplicit) (Syntax: 'i')
                              Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                              Operand:
                                ILocalReferenceOperation: i (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'i')
                            InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                            OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                          IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: alignment) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: '1')
                            ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
                            InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                            OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                          IArgumentOperation (ArgumentKind.DefaultValue, Matching Parameter: format) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: '{i,1}')
                            IDefaultValueOperation (OperationKind.DefaultValue, Type: System.String, Constant: null, IsImplicit) (Syntax: '{i,1}')
                            InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                            OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
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
      IInterpolatedStringHandlerCreationOperation (HandlerAppendCallsReturnBool: False, HandlerCreationHasSuccessParameter: False) (OperationKind.InterpolatedStringHandlerCreation, Type: CustomHandler, IsImplicit) (Syntax: '$""literal{i:test}""')
        Creation:
          IObjectCreationOperation (Constructor: CustomHandler..ctor(System.Int32 literalLength, System.Int32 formattedCount)) (OperationKind.ObjectCreation, Type: CustomHandler, IsImplicit) (Syntax: '$""literal{i:test}""')
            Arguments(2):
                IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: literalLength) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: '$""literal{i:test}""')
                  ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 7, IsImplicit) (Syntax: '$""literal{i:test}""')
                  InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: formattedCount) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: '$""literal{i:test}""')
                  ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1, IsImplicit) (Syntax: '$""literal{i:test}""')
                  InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
            Initializer:
              null
        Content:
          IInterpolatedStringOperation (OperationKind.InterpolatedString, Type: System.String) (Syntax: '$""literal{i:test}""')
            Parts(2):
                IInterpolatedStringAppendOperation (OperationKind.InterpolatedStringAppendLiteral, Type: null, IsImplicit) (Syntax: 'literal')
                  AppendCall:
                    IInvocationOperation ( void CustomHandler.AppendLiteral(System.String literal)) (OperationKind.Invocation, Type: System.Void, IsImplicit) (Syntax: 'literal')
                      Instance Receiver:
                        IInstanceReferenceOperation (ReferenceKind: InterpolatedStringHandler) (OperationKind.InstanceReference, Type: CustomHandler, IsImplicit) (Syntax: '$""literal{i:test}""')
                      Arguments(1):
                          IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: literal) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 'literal')
                            ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: ""literal"") (Syntax: 'literal')
                            InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                            OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                IInterpolatedStringAppendOperation (OperationKind.InterpolatedStringAppendFormatted, Type: null, IsImplicit) (Syntax: '{i:test}')
                  AppendCall:
                    IInvocationOperation ( void CustomHandler.AppendFormatted(System.Object o, [System.Int32 alignment = 0], [System.String format = null])) (OperationKind.Invocation, Type: System.Void, IsImplicit) (Syntax: '{i:test}')
                      Instance Receiver:
                        IInstanceReferenceOperation (ReferenceKind: InterpolatedStringHandler) (OperationKind.InstanceReference, Type: CustomHandler, IsImplicit) (Syntax: '$""literal{i:test}""')
                      Arguments(3):
                          IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: o) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 'i')
                            IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Object, IsImplicit) (Syntax: 'i')
                              Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                              Operand:
                                ILocalReferenceOperation: i (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'i')
                            InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                            OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                          IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: format) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: ':test')
                            ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: ""test"") (Syntax: ':test')
                            InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                            OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                          IArgumentOperation (ArgumentKind.DefaultValue, Matching Parameter: alignment) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: '{i:test}')
                            ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 0, IsImplicit) (Syntax: '{i:test}')
                            InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                            OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
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
      IInterpolatedStringHandlerCreationOperation (HandlerAppendCallsReturnBool: False, HandlerCreationHasSuccessParameter: False) (OperationKind.InterpolatedStringHandlerCreation, Type: CustomHandler, IsImplicit) (Syntax: '$""literal{i}""')
        Creation:
          IObjectCreationOperation (Constructor: CustomHandler..ctor(System.Int32 literalLength, System.Int32 formattedCount)) (OperationKind.ObjectCreation, Type: CustomHandler, IsImplicit) (Syntax: '$""literal{i}""')
            Arguments(2):
                IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: literalLength) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: '$""literal{i}""')
                  ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 7, IsImplicit) (Syntax: '$""literal{i}""')
                  InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: formattedCount) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: '$""literal{i}""')
                  ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1, IsImplicit) (Syntax: '$""literal{i}""')
                  InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
            Initializer:
              null
        Content:
          IInterpolatedStringOperation (OperationKind.InterpolatedString, Type: System.String) (Syntax: '$""literal{i}""')
            Parts(2):
                IInterpolatedStringAppendOperation (OperationKind.InterpolatedStringAppendLiteral, Type: null, IsImplicit) (Syntax: 'literal')
                  AppendCall:
                    IInvocationOperation ( void CustomHandler.AppendLiteral(System.String literal)) (OperationKind.Invocation, Type: System.Void, IsImplicit) (Syntax: 'literal')
                      Instance Receiver:
                        IInstanceReferenceOperation (ReferenceKind: InterpolatedStringHandler) (OperationKind.InstanceReference, Type: CustomHandler, IsImplicit) (Syntax: '$""literal{i}""')
                      Arguments(1):
                          IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: literal) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 'literal')
                            ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: ""literal"") (Syntax: 'literal')
                            InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                            OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                IInterpolatedStringAppendOperation (OperationKind.InterpolatedStringAppendFormatted, Type: null, IsImplicit) (Syntax: '{i}')
                  AppendCall:
                    IInvocationOperation ( void CustomHandler.AppendFormatted(System.Object o, [System.Int32 alignment = 0], [System.String format = null])) (OperationKind.Invocation, Type: System.Void, IsImplicit) (Syntax: '{i}')
                      Instance Receiver:
                        IInstanceReferenceOperation (ReferenceKind: InterpolatedStringHandler) (OperationKind.InstanceReference, Type: CustomHandler, IsImplicit) (Syntax: '$""literal{i}""')
                      Arguments(3):
                          IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: o) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 'i')
                            IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Object, IsImplicit) (Syntax: 'i')
                              Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                              Operand:
                                ILocalReferenceOperation: i (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'i')
                            InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                            OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                          IArgumentOperation (ArgumentKind.DefaultValue, Matching Parameter: alignment) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: '{i}')
                            ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 0, IsImplicit) (Syntax: '{i}')
                            InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                            OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                          IArgumentOperation (ArgumentKind.DefaultValue, Matching Parameter: format) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: '{i}')
                            IDefaultValueOperation (OperationKind.DefaultValue, Type: System.String, Constant: null, IsImplicit) (Syntax: '{i}')
                            InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                            OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
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
      IInterpolatedStringHandlerCreationOperation (HandlerAppendCallsReturnBool: False, HandlerCreationHasSuccessParameter: False) (OperationKind.InterpolatedStringHandlerCreation, Type: CustomHandler, IsInvalid, IsImplicit) (Syntax: '$""literal{i}""')
        Creation:
          IInvalidOperation (OperationKind.Invalid, Type: CustomHandler, IsInvalid, IsImplicit) (Syntax: '$""literal{i}""')
            Children(2):
                ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 7, IsInvalid, IsImplicit) (Syntax: '$""literal{i}""')
                ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1, IsInvalid, IsImplicit) (Syntax: '$""literal{i}""')
        Content:
          IInterpolatedStringOperation (OperationKind.InterpolatedString, Type: System.String, IsInvalid) (Syntax: '$""literal{i}""')
            Parts(2):
                IInterpolatedStringAppendOperation (OperationKind.InterpolatedStringAppendInvalid, Type: null, IsInvalid, IsImplicit) (Syntax: 'literal')
                  AppendCall:
                    IInvalidOperation (OperationKind.Invalid, Type: ?, IsInvalid, IsImplicit) (Syntax: 'literal')
                      Children(2):
                          IInvalidOperation (OperationKind.Invalid, Type: ?, IsInvalid, IsImplicit) (Syntax: 'literal')
                            Children(1):
                                IInstanceReferenceOperation (ReferenceKind: InterpolatedStringHandler) (OperationKind.InstanceReference, Type: CustomHandler, IsInvalid, IsImplicit) (Syntax: '$""literal{i}""')
                          ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: ""literal"", IsInvalid) (Syntax: 'literal')
                IInterpolatedStringAppendOperation (OperationKind.InterpolatedStringAppendInvalid, Type: null, IsInvalid, IsImplicit) (Syntax: '{i}')
                  AppendCall:
                    IInvalidOperation (OperationKind.Invalid, Type: ?, IsInvalid, IsImplicit) (Syntax: '{i}')
                      Children(2):
                          IInvalidOperation (OperationKind.Invalid, Type: ?, IsInvalid, IsImplicit) (Syntax: '{i}')
                            Children(1):
                                IInstanceReferenceOperation (ReferenceKind: InterpolatedStringHandler) (OperationKind.InstanceReference, Type: CustomHandler, IsInvalid, IsImplicit) (Syntax: '$""literal{i}""')
                          ILocalReferenceOperation: i (OperationKind.LocalReference, Type: System.Int32, IsInvalid) (Syntax: 'i')
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
      IInterpolatedStringHandlerCreationOperation (HandlerAppendCallsReturnBool: False, HandlerCreationHasSuccessParameter: False) (OperationKind.InterpolatedStringHandlerCreation, Type: CustomHandler, IsImplicit) (Syntax: '$""literal{i,1:test}""')
        Creation:
          IObjectCreationOperation (Constructor: CustomHandler..ctor(System.Int32 literalLength, System.Int32 formattedCount)) (OperationKind.ObjectCreation, Type: CustomHandler, IsImplicit) (Syntax: '$""literal{i,1:test}""')
            Arguments(2):
                IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: literalLength) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: '$""literal{i,1:test}""')
                  ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 7, IsImplicit) (Syntax: '$""literal{i,1:test}""')
                  InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: formattedCount) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: '$""literal{i,1:test}""')
                  ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1, IsImplicit) (Syntax: '$""literal{i,1:test}""')
                  InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
            Initializer:
              null
        Content:
          IInterpolatedStringOperation (OperationKind.InterpolatedString, Type: System.String) (Syntax: '$""literal{i,1:test}""')
            Parts(2):
                IInterpolatedStringAppendOperation (OperationKind.InterpolatedStringAppendLiteral, Type: null, IsImplicit) (Syntax: 'literal')
                  AppendCall:
                    IInvocationOperation ( void CustomHandler.AppendLiteral(System.Object o)) (OperationKind.Invocation, Type: System.Void, IsImplicit) (Syntax: 'literal')
                      Instance Receiver:
                        IInstanceReferenceOperation (ReferenceKind: InterpolatedStringHandler) (OperationKind.InstanceReference, Type: CustomHandler, IsImplicit) (Syntax: '$""literal{i,1:test}""')
                      Arguments(1):
                          IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: o) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 'literal')
                            IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Object, IsImplicit) (Syntax: 'literal')
                              Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
                              Operand:
                                ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: ""literal"") (Syntax: 'literal')
                            InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                            OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                IInterpolatedStringAppendOperation (OperationKind.InterpolatedStringAppendFormatted, Type: null, IsImplicit) (Syntax: '{i,1:test}')
                  AppendCall:
                    IInvocationOperation ( void CustomHandler.AppendFormatted(System.Object o, System.Object alignment, System.Object format)) (OperationKind.Invocation, Type: System.Void, IsImplicit) (Syntax: '{i,1:test}')
                      Instance Receiver:
                        IInstanceReferenceOperation (ReferenceKind: InterpolatedStringHandler) (OperationKind.InstanceReference, Type: CustomHandler, IsImplicit) (Syntax: '$""literal{i,1:test}""')
                      Arguments(3):
                          IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: o) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 'i')
                            IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Object, IsImplicit) (Syntax: 'i')
                              Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                              Operand:
                                ILocalReferenceOperation: i (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'i')
                            InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                            OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                          IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: alignment) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: '1')
                            IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Object, IsImplicit) (Syntax: '1')
                              Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                              Operand:
                                ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
                            InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                            OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                          IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: format) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: ':test')
                            IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Object, IsImplicit) (Syntax: ':test')
                              Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
                              Operand:
                                ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: ""test"") (Syntax: ':test')
                            InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                            OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
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
      IInterpolatedStringHandlerCreationOperation (HandlerAppendCallsReturnBool: False, HandlerCreationHasSuccessParameter: False) (OperationKind.InterpolatedStringHandlerCreation, Type: CustomHandler, IsInvalid, IsImplicit) (Syntax: '$""{}""')
        Creation:
          IObjectCreationOperation (Constructor: CustomHandler..ctor(System.Int32 literalLength, System.Int32 formattedCount)) (OperationKind.ObjectCreation, Type: CustomHandler, IsInvalid, IsImplicit) (Syntax: '$""{}""')
            Arguments(2):
                IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: literalLength) (OperationKind.Argument, Type: null, IsInvalid, IsImplicit) (Syntax: '$""{}""')
                  ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 0, IsInvalid, IsImplicit) (Syntax: '$""{}""')
                  InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: formattedCount) (OperationKind.Argument, Type: null, IsInvalid, IsImplicit) (Syntax: '$""{}""')
                  ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1, IsInvalid, IsImplicit) (Syntax: '$""{}""')
                  InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
            Initializer:
              null
        Content:
          IInterpolatedStringOperation (OperationKind.InterpolatedString, Type: System.String, IsInvalid) (Syntax: '$""{}""')
            Parts(1):
                IInterpolatedStringAppendOperation (OperationKind.InterpolatedStringAppendFormatted, Type: null, IsInvalid, IsImplicit) (Syntax: '{}')
                  AppendCall:
                    IInvalidOperation (OperationKind.Invalid, Type: System.Void, IsInvalid, IsImplicit) (Syntax: '{}')
                      Children(2):
                          IInstanceReferenceOperation (ReferenceKind: InterpolatedStringHandler) (OperationKind.InstanceReference, Type: CustomHandler, IsInvalid, IsImplicit) (Syntax: '$""{}""')
                          IInvalidOperation (OperationKind.Invalid, Type: null, IsInvalid) (Syntax: '')
                            Children(0)
";

            VerifyOperationTreeAndDiagnosticsForTest<VariableDeclaratorSyntax>(new[] { code, InterpolatedStringHandlerAttribute }, expectedOperationTree, expectedDiagnostics, parseOptions: TestOptions.RegularPreview);
        }

        [Fact]
        public void InterpolatedStringHandlerConversion_08()
        {
            var code = @"
int i = 0;
CustomHandler /*<bind>*/c = $""literal{i,1:test}""/*</bind>*/;
";

            var handler = GetInterpolatedStringCustomHandlerType("CustomHandler", "struct", useBoolReturns: true);

            var expectedDiagnostics = DiagnosticDescription.None;

            string expectedOperationTree = @"
IVariableDeclaratorOperation (Symbol: CustomHandler c) (OperationKind.VariableDeclarator, Type: null) (Syntax: 'c = $""literal{i,1:test}""')
  Initializer:
    IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null) (Syntax: '= $""literal{i,1:test}""')
      IInterpolatedStringHandlerCreationOperation (HandlerAppendCallsReturnBool: True, HandlerCreationHasSuccessParameter: False) (OperationKind.InterpolatedStringHandlerCreation, Type: CustomHandler, IsImplicit) (Syntax: '$""literal{i,1:test}""')
        Creation:
          IObjectCreationOperation (Constructor: CustomHandler..ctor(System.Int32 literalLength, System.Int32 formattedCount)) (OperationKind.ObjectCreation, Type: CustomHandler, IsImplicit) (Syntax: '$""literal{i,1:test}""')
            Arguments(2):
                IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: literalLength) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: '$""literal{i,1:test}""')
                  ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 7, IsImplicit) (Syntax: '$""literal{i,1:test}""')
                  InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: formattedCount) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: '$""literal{i,1:test}""')
                  ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1, IsImplicit) (Syntax: '$""literal{i,1:test}""')
                  InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
            Initializer:
              null
        Content:
          IInterpolatedStringOperation (OperationKind.InterpolatedString, Type: System.String) (Syntax: '$""literal{i,1:test}""')
            Parts(2):
                IInterpolatedStringAppendOperation (OperationKind.InterpolatedStringAppendLiteral, Type: null, IsImplicit) (Syntax: 'literal')
                  AppendCall:
                    IInvocationOperation ( System.Boolean CustomHandler.AppendLiteral(System.String literal)) (OperationKind.Invocation, Type: System.Boolean, IsImplicit) (Syntax: 'literal')
                      Instance Receiver:
                        IInstanceReferenceOperation (ReferenceKind: InterpolatedStringHandler) (OperationKind.InstanceReference, Type: CustomHandler, IsImplicit) (Syntax: '$""literal{i,1:test}""')
                      Arguments(1):
                          IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: literal) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 'literal')
                            ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: ""literal"") (Syntax: 'literal')
                            InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                            OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                IInterpolatedStringAppendOperation (OperationKind.InterpolatedStringAppendFormatted, Type: null, IsImplicit) (Syntax: '{i,1:test}')
                  AppendCall:
                    IInvocationOperation ( System.Boolean CustomHandler.AppendFormatted(System.Object o, [System.Int32 alignment = 0], [System.String format = null])) (OperationKind.Invocation, Type: System.Boolean, IsImplicit) (Syntax: '{i,1:test}')
                      Instance Receiver:
                        IInstanceReferenceOperation (ReferenceKind: InterpolatedStringHandler) (OperationKind.InstanceReference, Type: CustomHandler, IsImplicit) (Syntax: '$""literal{i,1:test}""')
                      Arguments(3):
                          IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: o) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 'i')
                            IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Object, IsImplicit) (Syntax: 'i')
                              Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                              Operand:
                                ILocalReferenceOperation: i (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'i')
                            InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                            OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                          IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: alignment) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: '1')
                            ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
                            InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                            OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                          IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: format) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: ':test')
                            ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: ""test"") (Syntax: ':test')
                            InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                            OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
";

            VerifyOperationTreeAndDiagnosticsForTest<VariableDeclaratorSyntax>(new[] { code, handler }, expectedOperationTree, expectedDiagnostics, parseOptions: TestOptions.RegularPreview);
        }

        [Fact]
        public void InterpolatedStringHandlerConversion_09()
        {
            var code = @"
int i = 0;
CustomHandler /*<bind>*/c = $""literal{i,1:test}""/*</bind>*/;
";

            var handler = GetInterpolatedStringCustomHandlerType("CustomHandler", "struct", useBoolReturns: true, includeTrailingOutConstructorParameter: true);

            var expectedDiagnostics = DiagnosticDescription.None;

            string expectedOperationTree = @"
IVariableDeclaratorOperation (Symbol: CustomHandler c) (OperationKind.VariableDeclarator, Type: null) (Syntax: 'c = $""literal{i,1:test}""')
  Initializer:
    IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null) (Syntax: '= $""literal{i,1:test}""')
      IInterpolatedStringHandlerCreationOperation (HandlerAppendCallsReturnBool: True, HandlerCreationHasSuccessParameter: True) (OperationKind.InterpolatedStringHandlerCreation, Type: CustomHandler, IsImplicit) (Syntax: '$""literal{i,1:test}""')
        Creation:
          IObjectCreationOperation (Constructor: CustomHandler..ctor(System.Int32 literalLength, System.Int32 formattedCount, out System.Boolean success)) (OperationKind.ObjectCreation, Type: CustomHandler, IsImplicit) (Syntax: '$""literal{i,1:test}""')
            Arguments(3):
                IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: literalLength) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: '$""literal{i,1:test}""')
                  ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 7, IsImplicit) (Syntax: '$""literal{i,1:test}""')
                  InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: formattedCount) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: '$""literal{i,1:test}""')
                  ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1, IsImplicit) (Syntax: '$""literal{i,1:test}""')
                  InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: success) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: '$""literal{i,1:test}""')
                  IInterpolatedStringHandlerArgumentPlaceholderOperation (TrailingValidityArgument) (OperationKind.InterpolatedStringHandlerArgumentPlaceholder, Type: null, IsImplicit) (Syntax: '$""literal{i,1:test}""')
                  InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
            Initializer:
              null
        Content:
          IInterpolatedStringOperation (OperationKind.InterpolatedString, Type: System.String) (Syntax: '$""literal{i,1:test}""')
            Parts(2):
                IInterpolatedStringAppendOperation (OperationKind.InterpolatedStringAppendLiteral, Type: null, IsImplicit) (Syntax: 'literal')
                  AppendCall:
                    IInvocationOperation ( System.Boolean CustomHandler.AppendLiteral(System.String literal)) (OperationKind.Invocation, Type: System.Boolean, IsImplicit) (Syntax: 'literal')
                      Instance Receiver:
                        IInstanceReferenceOperation (ReferenceKind: InterpolatedStringHandler) (OperationKind.InstanceReference, Type: CustomHandler, IsImplicit) (Syntax: '$""literal{i,1:test}""')
                      Arguments(1):
                          IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: literal) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 'literal')
                            ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: ""literal"") (Syntax: 'literal')
                            InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                            OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                IInterpolatedStringAppendOperation (OperationKind.InterpolatedStringAppendFormatted, Type: null, IsImplicit) (Syntax: '{i,1:test}')
                  AppendCall:
                    IInvocationOperation ( System.Boolean CustomHandler.AppendFormatted(System.Object o, [System.Int32 alignment = 0], [System.String format = null])) (OperationKind.Invocation, Type: System.Boolean, IsImplicit) (Syntax: '{i,1:test}')
                      Instance Receiver:
                        IInstanceReferenceOperation (ReferenceKind: InterpolatedStringHandler) (OperationKind.InstanceReference, Type: CustomHandler, IsImplicit) (Syntax: '$""literal{i,1:test}""')
                      Arguments(3):
                          IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: o) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 'i')
                            IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Object, IsImplicit) (Syntax: 'i')
                              Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                              Operand:
                                ILocalReferenceOperation: i (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'i')
                            InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                            OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                          IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: alignment) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: '1')
                            ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
                            InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                            OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                          IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: format) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: ':test')
                            ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: ""test"") (Syntax: ':test')
                            InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                            OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
";

            VerifyOperationTreeAndDiagnosticsForTest<VariableDeclaratorSyntax>(new[] { code, handler }, expectedOperationTree, expectedDiagnostics, parseOptions: TestOptions.RegularPreview);
        }

        [Fact]
        public void InterpolatedStringHandlerConversion_10()
        {
            var code = @"
using System;
using System.Runtime.CompilerServices;
C c = new C();
/*<bind>*/c.M($""literal{c,1:format}"")/*</bind>*/;

public class C
{
    public void M([InterpolatedStringHandlerArgument("""")]CustomHandler c) {}
}

public partial struct CustomHandler
{
    public CustomHandler(int literalLength, int formattedCount, C c) : this()
    {
    }
}
";

            var expectedDiagnostics = DiagnosticDescription.None;

            string expectedOperationTree = @"
IInvocationOperation ( void C.M(CustomHandler c)) (OperationKind.Invocation, Type: System.Void) (Syntax: 'c.M($""liter ... 1:format}"")')
  Instance Receiver:
    ILocalReferenceOperation: c (OperationKind.LocalReference, Type: C) (Syntax: 'c')
  Arguments(1):
      IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: c) (OperationKind.Argument, Type: null) (Syntax: '$""literal{c,1:format}""')
        IInterpolatedStringHandlerCreationOperation (HandlerAppendCallsReturnBool: False, HandlerCreationHasSuccessParameter: False) (OperationKind.InterpolatedStringHandlerCreation, Type: CustomHandler, IsImplicit) (Syntax: '$""literal{c,1:format}""')
          Creation:
            IObjectCreationOperation (Constructor: CustomHandler..ctor(System.Int32 literalLength, System.Int32 formattedCount, C c)) (OperationKind.ObjectCreation, Type: CustomHandler, IsImplicit) (Syntax: '$""literal{c,1:format}""')
              Arguments(3):
                  IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: literalLength) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: '$""literal{c,1:format}""')
                    ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 7, IsImplicit) (Syntax: '$""literal{c,1:format}""')
                    InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: formattedCount) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: '$""literal{c,1:format}""')
                    ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1, IsImplicit) (Syntax: '$""literal{c,1:format}""')
                    InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: c) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 'c')
                    IInterpolatedStringHandlerArgumentPlaceholderOperation (CallsiteReceiver) (OperationKind.InterpolatedStringHandlerArgumentPlaceholder, Type: null, IsImplicit) (Syntax: 'c')
                    InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
              Initializer:
                null
          Content:
            IInterpolatedStringOperation (OperationKind.InterpolatedString, Type: System.String) (Syntax: '$""literal{c,1:format}""')
              Parts(2):
                  IInterpolatedStringAppendOperation (OperationKind.InterpolatedStringAppendLiteral, Type: null, IsImplicit) (Syntax: 'literal')
                    AppendCall:
                      IInvocationOperation ( void CustomHandler.AppendLiteral(System.String literal)) (OperationKind.Invocation, Type: System.Void, IsImplicit) (Syntax: 'literal')
                        Instance Receiver:
                          IInstanceReferenceOperation (ReferenceKind: InterpolatedStringHandler) (OperationKind.InstanceReference, Type: CustomHandler, IsImplicit) (Syntax: '$""literal{c,1:format}""')
                        Arguments(1):
                            IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: literal) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 'literal')
                              ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: ""literal"") (Syntax: 'literal')
                              InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                              OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  IInterpolatedStringAppendOperation (OperationKind.InterpolatedStringAppendFormatted, Type: null, IsImplicit) (Syntax: '{c,1:format}')
                    AppendCall:
                      IInvocationOperation ( void CustomHandler.AppendFormatted(System.Object o, [System.Int32 alignment = 0], [System.String format = null])) (OperationKind.Invocation, Type: System.Void, IsImplicit) (Syntax: '{c,1:format}')
                        Instance Receiver:
                          IInstanceReferenceOperation (ReferenceKind: InterpolatedStringHandler) (OperationKind.InstanceReference, Type: CustomHandler, IsImplicit) (Syntax: '$""literal{c,1:format}""')
                        Arguments(3):
                            IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: o) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 'c')
                              IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Object, IsImplicit) (Syntax: 'c')
                                Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
                                Operand:
                                  ILocalReferenceOperation: c (OperationKind.LocalReference, Type: C) (Syntax: 'c')
                              InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                              OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                            IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: alignment) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: '1')
                              ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
                              InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                              OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                            IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: format) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: ':format')
                              ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: ""format"") (Syntax: ':format')
                              InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                              OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
";

            VerifyOperationTreeAndDiagnosticsForTest<InvocationExpressionSyntax>(new[] { code, GetInterpolatedStringCustomHandlerType("CustomHandler", "partial struct", useBoolReturns: false), InterpolatedStringHandlerArgumentAttribute },
                                                                                 expectedOperationTree, expectedDiagnostics, parseOptions: TestOptions.RegularPreview);
        }

        [Fact]
        public void InterpolatedStringHandlerConversion_11()
        {
            var code = @"
using System;
using System.Runtime.CompilerServices;
C c = new C();
/*<bind>*/c.M(true, $""literal{c,1:format}"")/*</bind>*/;

public class C
{
    public void M(bool b, [InterpolatedStringHandlerArgument(""b"")]CustomHandler c) {}
}

public partial struct CustomHandler
{
    public CustomHandler(int literalLength, int formattedCount, bool b, out bool success) : this()
    {
        success = true;
    }
}
";

            var expectedDiagnostics = DiagnosticDescription.None;

            string expectedOperationTree = @"
IInvocationOperation ( void C.M(System.Boolean b, CustomHandler c)) (OperationKind.Invocation, Type: System.Void) (Syntax: 'c.M(true, $ ... 1:format}"")')
  Instance Receiver:
    ILocalReferenceOperation: c (OperationKind.LocalReference, Type: C) (Syntax: 'c')
  Arguments(2):
      IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: b) (OperationKind.Argument, Type: null) (Syntax: 'true')
        ILiteralOperation (OperationKind.Literal, Type: System.Boolean, Constant: True) (Syntax: 'true')
        InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
      IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: c) (OperationKind.Argument, Type: null) (Syntax: '$""literal{c,1:format}""')
        IInterpolatedStringHandlerCreationOperation (HandlerAppendCallsReturnBool: False, HandlerCreationHasSuccessParameter: True) (OperationKind.InterpolatedStringHandlerCreation, Type: CustomHandler, IsImplicit) (Syntax: '$""literal{c,1:format}""')
          Creation:
            IObjectCreationOperation (Constructor: CustomHandler..ctor(System.Int32 literalLength, System.Int32 formattedCount, System.Boolean b, out System.Boolean success)) (OperationKind.ObjectCreation, Type: CustomHandler, IsImplicit) (Syntax: '$""literal{c,1:format}""')
              Arguments(4):
                  IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: literalLength) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: '$""literal{c,1:format}""')
                    ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 7, IsImplicit) (Syntax: '$""literal{c,1:format}""')
                    InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: formattedCount) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: '$""literal{c,1:format}""')
                    ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1, IsImplicit) (Syntax: '$""literal{c,1:format}""')
                    InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: b) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 'true')
                    IInterpolatedStringHandlerArgumentPlaceholderOperation (ArgumentIndex: 0) (OperationKind.InterpolatedStringHandlerArgumentPlaceholder, Type: null, IsImplicit) (Syntax: 'true')
                    InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: success) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: '$""literal{c,1:format}""')
                    IInterpolatedStringHandlerArgumentPlaceholderOperation (TrailingValidityArgument) (OperationKind.InterpolatedStringHandlerArgumentPlaceholder, Type: null, IsImplicit) (Syntax: '$""literal{c,1:format}""')
                    InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
              Initializer:
                null
          Content:
            IInterpolatedStringOperation (OperationKind.InterpolatedString, Type: System.String) (Syntax: '$""literal{c,1:format}""')
              Parts(2):
                  IInterpolatedStringAppendOperation (OperationKind.InterpolatedStringAppendLiteral, Type: null, IsImplicit) (Syntax: 'literal')
                    AppendCall:
                      IInvocationOperation ( void CustomHandler.AppendLiteral(System.String literal)) (OperationKind.Invocation, Type: System.Void, IsImplicit) (Syntax: 'literal')
                        Instance Receiver:
                          IInstanceReferenceOperation (ReferenceKind: InterpolatedStringHandler) (OperationKind.InstanceReference, Type: CustomHandler, IsImplicit) (Syntax: '$""literal{c,1:format}""')
                        Arguments(1):
                            IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: literal) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 'literal')
                              ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: ""literal"") (Syntax: 'literal')
                              InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                              OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  IInterpolatedStringAppendOperation (OperationKind.InterpolatedStringAppendFormatted, Type: null, IsImplicit) (Syntax: '{c,1:format}')
                    AppendCall:
                      IInvocationOperation ( void CustomHandler.AppendFormatted(System.Object o, [System.Int32 alignment = 0], [System.String format = null])) (OperationKind.Invocation, Type: System.Void, IsImplicit) (Syntax: '{c,1:format}')
                        Instance Receiver:
                          IInstanceReferenceOperation (ReferenceKind: InterpolatedStringHandler) (OperationKind.InstanceReference, Type: CustomHandler, IsImplicit) (Syntax: '$""literal{c,1:format}""')
                        Arguments(3):
                            IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: o) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 'c')
                              IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Object, IsImplicit) (Syntax: 'c')
                                Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
                                Operand:
                                  ILocalReferenceOperation: c (OperationKind.LocalReference, Type: C) (Syntax: 'c')
                              InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                              OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                            IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: alignment) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: '1')
                              ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
                              InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                              OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                            IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: format) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: ':format')
                              ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: ""format"") (Syntax: ':format')
                              InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                              OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
";

            VerifyOperationTreeAndDiagnosticsForTest<InvocationExpressionSyntax>(new[] { code, GetInterpolatedStringCustomHandlerType("CustomHandler", "partial struct", useBoolReturns: false), InterpolatedStringHandlerArgumentAttribute },
                                                                                 expectedOperationTree, expectedDiagnostics, parseOptions: TestOptions.RegularPreview);
        }

        [Fact]
        public void InterpolatedStringHandlerConversionFlow_01()
        {
            var code = @"
using System;
using System.Runtime.CompilerServices;

public class C
{
    public void M1(C c, CustomHandler custom)
    /*<bind>*/{
        custom = $""literal{c,1:format}"";
    }/*</bind>*/
}
";

            string handlerType = GetInterpolatedStringCustomHandlerType("CustomHandler", "partial struct", useBoolReturns: false, includeTrailingOutConstructorParameter: false);

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
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'custom')
              Value:
                IParameterReferenceOperation: custom (OperationKind.ParameterReference, Type: CustomHandler) (Syntax: 'custom')
            IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: '$""literal{c,1:format}""')
              Value:
                IObjectCreationOperation (Constructor: CustomHandler..ctor(System.Int32 literalLength, System.Int32 formattedCount)) (OperationKind.ObjectCreation, Type: CustomHandler, IsImplicit) (Syntax: '$""literal{c,1:format}""')
                  Arguments(2):
                      IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: literalLength) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: '$""literal{c,1:format}""')
                        ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 7, IsImplicit) (Syntax: '$""literal{c,1:format}""')
                        InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                      IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: formattedCount) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: '$""literal{c,1:format}""')
                        ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1, IsImplicit) (Syntax: '$""literal{c,1:format}""')
                        InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  Initializer:
                    null
            IInvocationOperation ( void CustomHandler.AppendLiteral(System.String literal)) (OperationKind.Invocation, Type: System.Void, IsImplicit) (Syntax: 'literal')
              Instance Receiver:
                IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: CustomHandler, IsImplicit) (Syntax: '$""literal{c,1:format}""')
              Arguments(1):
                  IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: literal) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 'literal')
                    ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: ""literal"") (Syntax: 'literal')
                    InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
            IInvocationOperation ( void CustomHandler.AppendFormatted(System.Object o, [System.Int32 alignment = 0], [System.String format = null])) (OperationKind.Invocation, Type: System.Void, IsImplicit) (Syntax: '{c,1:format}')
              Instance Receiver:
                IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: CustomHandler, IsImplicit) (Syntax: '$""literal{c,1:format}""')
              Arguments(3):
                  IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: o) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 'c')
                    IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Object, IsImplicit) (Syntax: 'c')
                      Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
                        (ImplicitReference)
                      Operand:
                        IParameterReferenceOperation: c (OperationKind.ParameterReference, Type: C) (Syntax: 'c')
                    InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: alignment) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: '1')
                    ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
                    InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: format) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: ':format')
                    ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: ""format"") (Syntax: ':format')
                    InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'custom = $"" ... 1:format}"";')
              Expression:
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: CustomHandler) (Syntax: 'custom = $"" ... ,1:format}""')
                  Left:
                    IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: CustomHandler, IsImplicit) (Syntax: 'custom')
                  Right:
                    IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: CustomHandler, IsImplicit) (Syntax: '$""literal{c,1:format}""')
        Next (Regular) Block[B2]
            Leaving: {R1}
}
Block[B2] - Exit
    Predecessors: [B1]
    Statements (0)
";

            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(new[] { code, handlerType }, expectedFlowGraph, expectedDiagnostics, parseOptions: TestOptions.RegularPreview);
        }

        [Fact]
        public void InterpolatedStringHandlerConversionFlow_02()
        {
            var code = @"
using System;
using System.Runtime.CompilerServices;

public class C
{
    public void M1(C c, CustomHandler custom)
    /*<bind>*/{
        custom = $""literal{c,1:format}"";
    }/*</bind>*/
}
";

            string handlerType = GetInterpolatedStringCustomHandlerType("CustomHandler", "partial struct", useBoolReturns: true, includeTrailingOutConstructorParameter: false);

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
        Statements (2)
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'custom')
              Value:
                IParameterReferenceOperation: custom (OperationKind.ParameterReference, Type: CustomHandler) (Syntax: 'custom')
            IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: '$""literal{c,1:format}""')
              Value:
                IObjectCreationOperation (Constructor: CustomHandler..ctor(System.Int32 literalLength, System.Int32 formattedCount)) (OperationKind.ObjectCreation, Type: CustomHandler, IsImplicit) (Syntax: '$""literal{c,1:format}""')
                  Arguments(2):
                      IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: literalLength) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: '$""literal{c,1:format}""')
                        ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 7, IsImplicit) (Syntax: '$""literal{c,1:format}""')
                        InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                      IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: formattedCount) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: '$""literal{c,1:format}""')
                        ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1, IsImplicit) (Syntax: '$""literal{c,1:format}""')
                        InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  Initializer:
                    null
        Jump if False (Regular) to Block[B3]
            IInvocationOperation ( System.Boolean CustomHandler.AppendLiteral(System.String literal)) (OperationKind.Invocation, Type: System.Boolean, IsImplicit) (Syntax: 'literal')
              Instance Receiver:
                IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: CustomHandler, IsImplicit) (Syntax: '$""literal{c,1:format}""')
              Arguments(1):
                  IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: literal) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 'literal')
                    ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: ""literal"") (Syntax: 'literal')
                    InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        Next (Regular) Block[B2]
    Block[B2] - Block
        Predecessors: [B1]
        Statements (1)
            IInvocationOperation ( System.Boolean CustomHandler.AppendFormatted(System.Object o, [System.Int32 alignment = 0], [System.String format = null])) (OperationKind.Invocation, Type: System.Boolean, IsImplicit) (Syntax: '{c,1:format}')
              Instance Receiver:
                IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: CustomHandler, IsImplicit) (Syntax: '$""literal{c,1:format}""')
              Arguments(3):
                  IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: o) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 'c')
                    IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Object, IsImplicit) (Syntax: 'c')
                      Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
                        (ImplicitReference)
                      Operand:
                        IParameterReferenceOperation: c (OperationKind.ParameterReference, Type: C) (Syntax: 'c')
                    InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: alignment) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: '1')
                    ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
                    InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: format) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: ':format')
                    ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: ""format"") (Syntax: ':format')
                    InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        Next (Regular) Block[B3]
    Block[B3] - Block
        Predecessors: [B1] [B2]
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'custom = $"" ... 1:format}"";')
              Expression:
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: CustomHandler) (Syntax: 'custom = $"" ... ,1:format}""')
                  Left:
                    IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: CustomHandler, IsImplicit) (Syntax: 'custom')
                  Right:
                    IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: CustomHandler, IsImplicit) (Syntax: '$""literal{c,1:format}""')
        Next (Regular) Block[B4]
            Leaving: {R1}
}
Block[B4] - Exit
    Predecessors: [B3]
    Statements (0)
";

            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(new[] { code, handlerType }, expectedFlowGraph, expectedDiagnostics, parseOptions: TestOptions.RegularPreview);
        }

        [Fact]
        public void InterpolatedStringHandlerConversionFlow_03()
        {
            var code = @"
using System;
using System.Runtime.CompilerServices;

public class C
{
    public void M1(C c, CustomHandler custom)
    /*<bind>*/{
        custom = $""literal{c,1:format}"";
    }/*</bind>*/
}
";

            string handlerType = GetInterpolatedStringCustomHandlerType("CustomHandler", "partial struct", useBoolReturns: false, includeTrailingOutConstructorParameter: true);

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
        Statements (1)
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'custom')
              Value:
                IParameterReferenceOperation: custom (OperationKind.ParameterReference, Type: CustomHandler) (Syntax: 'custom')
        Next (Regular) Block[B2]
            Entering: {R2}
    .locals {R2}
    {
        CaptureIds: [2]
        Block[B2] - Block
            Predecessors: [B1]
            Statements (1)
                IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: '$""literal{c,1:format}""')
                  Value:
                    IObjectCreationOperation (Constructor: CustomHandler..ctor(System.Int32 literalLength, System.Int32 formattedCount, out System.Boolean success)) (OperationKind.ObjectCreation, Type: CustomHandler, IsImplicit) (Syntax: '$""literal{c,1:format}""')
                      Arguments(3):
                          IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: literalLength) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: '$""literal{c,1:format}""')
                            ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 7, IsImplicit) (Syntax: '$""literal{c,1:format}""')
                            InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                            OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                          IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: formattedCount) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: '$""literal{c,1:format}""')
                            ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1, IsImplicit) (Syntax: '$""literal{c,1:format}""')
                            InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                            OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                          IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: success) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: '$""literal{c,1:format}""')
                            IFlowCaptureReferenceOperation: 2 (IsInitialization) (OperationKind.FlowCaptureReference, Type: null, IsImplicit) (Syntax: '$""literal{c,1:format}""')
                            InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                            OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                      Initializer:
                        null
            Jump if False (Regular) to Block[B4]
                IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: null, IsImplicit) (Syntax: '$""literal{c,1:format}""')
                Leaving: {R2}
            Next (Regular) Block[B3]
                Leaving: {R2}
    }
    Block[B3] - Block
        Predecessors: [B2]
        Statements (2)
            IInvocationOperation ( void CustomHandler.AppendLiteral(System.String literal)) (OperationKind.Invocation, Type: System.Void, IsImplicit) (Syntax: 'literal')
              Instance Receiver:
                IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: CustomHandler, IsImplicit) (Syntax: '$""literal{c,1:format}""')
              Arguments(1):
                  IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: literal) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 'literal')
                    ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: ""literal"") (Syntax: 'literal')
                    InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
            IInvocationOperation ( void CustomHandler.AppendFormatted(System.Object o, [System.Int32 alignment = 0], [System.String format = null])) (OperationKind.Invocation, Type: System.Void, IsImplicit) (Syntax: '{c,1:format}')
              Instance Receiver:
                IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: CustomHandler, IsImplicit) (Syntax: '$""literal{c,1:format}""')
              Arguments(3):
                  IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: o) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 'c')
                    IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Object, IsImplicit) (Syntax: 'c')
                      Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
                        (ImplicitReference)
                      Operand:
                        IParameterReferenceOperation: c (OperationKind.ParameterReference, Type: C) (Syntax: 'c')
                    InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: alignment) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: '1')
                    ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
                    InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: format) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: ':format')
                    ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: ""format"") (Syntax: ':format')
                    InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        Next (Regular) Block[B4]
    Block[B4] - Block
        Predecessors: [B2] [B3]
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'custom = $"" ... 1:format}"";')
              Expression:
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: CustomHandler) (Syntax: 'custom = $"" ... ,1:format}""')
                  Left:
                    IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: CustomHandler, IsImplicit) (Syntax: 'custom')
                  Right:
                    IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: CustomHandler, IsImplicit) (Syntax: '$""literal{c,1:format}""')
        Next (Regular) Block[B5]
            Leaving: {R1}
}
Block[B5] - Exit
    Predecessors: [B4]
    Statements (0)
";

            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(new[] { code, handlerType }, expectedFlowGraph, expectedDiagnostics, parseOptions: TestOptions.RegularPreview);
        }

        [Fact]
        public void InterpolatedStringHandlerConversionFlow_04()
        {
            var code = @"
using System;
using System.Runtime.CompilerServices;

public class C
{
    public void M1(C c, CustomHandler custom)
    /*<bind>*/{
        custom = $""literal{c,1:format}"";
    }/*</bind>*/
}
";

            string handlerType = GetInterpolatedStringCustomHandlerType("CustomHandler", "partial struct", useBoolReturns: true, includeTrailingOutConstructorParameter: true);

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
        Statements (1)
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'custom')
              Value:
                IParameterReferenceOperation: custom (OperationKind.ParameterReference, Type: CustomHandler) (Syntax: 'custom')
        Next (Regular) Block[B2]
            Entering: {R2}
    .locals {R2}
    {
        CaptureIds: [2]
        Block[B2] - Block
            Predecessors: [B1]
            Statements (1)
                IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: '$""literal{c,1:format}""')
                  Value:
                    IObjectCreationOperation (Constructor: CustomHandler..ctor(System.Int32 literalLength, System.Int32 formattedCount, out System.Boolean success)) (OperationKind.ObjectCreation, Type: CustomHandler, IsImplicit) (Syntax: '$""literal{c,1:format}""')
                      Arguments(3):
                          IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: literalLength) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: '$""literal{c,1:format}""')
                            ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 7, IsImplicit) (Syntax: '$""literal{c,1:format}""')
                            InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                            OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                          IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: formattedCount) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: '$""literal{c,1:format}""')
                            ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1, IsImplicit) (Syntax: '$""literal{c,1:format}""')
                            InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                            OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                          IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: success) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: '$""literal{c,1:format}""')
                            IFlowCaptureReferenceOperation: 2 (IsInitialization) (OperationKind.FlowCaptureReference, Type: null, IsImplicit) (Syntax: '$""literal{c,1:format}""')
                            InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                            OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                      Initializer:
                        null
            Jump if False (Regular) to Block[B5]
                IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: null, IsImplicit) (Syntax: '$""literal{c,1:format}""')
                Leaving: {R2}
            Next (Regular) Block[B3]
                Leaving: {R2}
    }
    Block[B3] - Block
        Predecessors: [B2]
        Statements (0)
        Jump if False (Regular) to Block[B5]
            IInvocationOperation ( System.Boolean CustomHandler.AppendLiteral(System.String literal)) (OperationKind.Invocation, Type: System.Boolean, IsImplicit) (Syntax: 'literal')
              Instance Receiver:
                IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: CustomHandler, IsImplicit) (Syntax: '$""literal{c,1:format}""')
              Arguments(1):
                  IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: literal) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 'literal')
                    ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: ""literal"") (Syntax: 'literal')
                    InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        Next (Regular) Block[B4]
    Block[B4] - Block
        Predecessors: [B3]
        Statements (1)
            IInvocationOperation ( System.Boolean CustomHandler.AppendFormatted(System.Object o, [System.Int32 alignment = 0], [System.String format = null])) (OperationKind.Invocation, Type: System.Boolean, IsImplicit) (Syntax: '{c,1:format}')
              Instance Receiver:
                IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: CustomHandler, IsImplicit) (Syntax: '$""literal{c,1:format}""')
              Arguments(3):
                  IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: o) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 'c')
                    IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Object, IsImplicit) (Syntax: 'c')
                      Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
                        (ImplicitReference)
                      Operand:
                        IParameterReferenceOperation: c (OperationKind.ParameterReference, Type: C) (Syntax: 'c')
                    InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: alignment) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: '1')
                    ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
                    InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: format) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: ':format')
                    ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: ""format"") (Syntax: ':format')
                    InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        Next (Regular) Block[B5]
    Block[B5] - Block
        Predecessors: [B2] [B3] [B4]
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'custom = $"" ... 1:format}"";')
              Expression:
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: CustomHandler) (Syntax: 'custom = $"" ... ,1:format}""')
                  Left:
                    IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: CustomHandler, IsImplicit) (Syntax: 'custom')
                  Right:
                    IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: CustomHandler, IsImplicit) (Syntax: '$""literal{c,1:format}""')
        Next (Regular) Block[B6]
            Leaving: {R1}
}
Block[B6] - Exit
    Predecessors: [B5]
    Statements (0)
";

            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(new[] { code, handlerType }, expectedFlowGraph, expectedDiagnostics, parseOptions: TestOptions.RegularPreview);
        }

        [Fact]
        public void InterpolatedStringHandlerConversionFlow_05()
        {
            var code = @"
using System;
using System.Runtime.CompilerServices;

public class C
{
    public void M1(C c, CustomHandler custom)
    /*<bind>*/{
        custom = $""literal"";
    }/*</bind>*/
}
";

            string handlerType = GetInterpolatedStringCustomHandlerType("CustomHandler", "partial struct", useBoolReturns: true, includeTrailingOutConstructorParameter: false);

            var expectedDiagnostics = DiagnosticDescription.None;

            // Note that the blocks for the blocks for the AppendLiteral and the result have been merged in this result, even though the AppendLiteral call returns bool.
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
        Statements (4)
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'custom')
              Value:
                IParameterReferenceOperation: custom (OperationKind.ParameterReference, Type: CustomHandler) (Syntax: 'custom')
            IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: '$""literal""')
              Value:
                IObjectCreationOperation (Constructor: CustomHandler..ctor(System.Int32 literalLength, System.Int32 formattedCount)) (OperationKind.ObjectCreation, Type: CustomHandler, IsImplicit) (Syntax: '$""literal""')
                  Arguments(2):
                      IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: literalLength) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: '$""literal""')
                        ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 7, IsImplicit) (Syntax: '$""literal""')
                        InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                      IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: formattedCount) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: '$""literal""')
                        ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 0, IsImplicit) (Syntax: '$""literal""')
                        InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  Initializer:
                    null
            IInvocationOperation ( System.Boolean CustomHandler.AppendLiteral(System.String literal)) (OperationKind.Invocation, Type: System.Boolean, IsImplicit) (Syntax: 'literal')
              Instance Receiver:
                IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: CustomHandler, IsImplicit) (Syntax: '$""literal""')
              Arguments(1):
                  IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: literal) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 'literal')
                    ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: ""literal"") (Syntax: 'literal')
                    InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'custom = $""literal"";')
              Expression:
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: CustomHandler) (Syntax: 'custom = $""literal""')
                  Left:
                    IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: CustomHandler, IsImplicit) (Syntax: 'custom')
                  Right:
                    IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: CustomHandler, IsImplicit) (Syntax: '$""literal""')
        Next (Regular) Block[B2]
            Leaving: {R1}
}
Block[B2] - Exit
    Predecessors: [B1]
    Statements (0)
";

            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(new[] { code, handlerType }, expectedFlowGraph, expectedDiagnostics, parseOptions: TestOptions.RegularPreview);
        }

        [Fact]
        public void InterpolatedStringHandlerConversionFlow_06()
        {
            var code = @"
using System;
using System.Runtime.CompilerServices;

public class C
{
    public void M1(C c, CustomHandler custom)
    /*<bind>*/{
        custom = $""literal"";
    }/*</bind>*/
}
";

            string handlerType = GetInterpolatedStringCustomHandlerType("CustomHandler", "partial struct", useBoolReturns: true, includeTrailingOutConstructorParameter: true);

            var expectedDiagnostics = DiagnosticDescription.None;

            // Note that the blocks for the blocks for the AppendLiteral and the result have been merged in this result, even though the AppendLiteral call returns bool.
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
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'custom')
              Value:
                IParameterReferenceOperation: custom (OperationKind.ParameterReference, Type: CustomHandler) (Syntax: 'custom')
        Next (Regular) Block[B2]
            Entering: {R2}
    .locals {R2}
    {
        CaptureIds: [2]
        Block[B2] - Block
            Predecessors: [B1]
            Statements (1)
                IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: '$""literal""')
                  Value:
                    IObjectCreationOperation (Constructor: CustomHandler..ctor(System.Int32 literalLength, System.Int32 formattedCount, out System.Boolean success)) (OperationKind.ObjectCreation, Type: CustomHandler, IsImplicit) (Syntax: '$""literal""')
                      Arguments(3):
                          IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: literalLength) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: '$""literal""')
                            ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 7, IsImplicit) (Syntax: '$""literal""')
                            InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                            OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                          IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: formattedCount) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: '$""literal""')
                            ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 0, IsImplicit) (Syntax: '$""literal""')
                            InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                            OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                          IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: success) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: '$""literal""')
                            IFlowCaptureReferenceOperation: 2 (IsInitialization) (OperationKind.FlowCaptureReference, Type: null, IsImplicit) (Syntax: '$""literal""')
                            InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                            OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                      Initializer:
                        null
            Jump if False (Regular) to Block[B4]
                IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: null, IsImplicit) (Syntax: '$""literal""')
                Leaving: {R2}
            Next (Regular) Block[B3]
                Leaving: {R2}
    }
    Block[B3] - Block
        Predecessors: [B2]
        Statements (1)
            IInvocationOperation ( System.Boolean CustomHandler.AppendLiteral(System.String literal)) (OperationKind.Invocation, Type: System.Boolean, IsImplicit) (Syntax: 'literal')
              Instance Receiver:
                IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: CustomHandler, IsImplicit) (Syntax: '$""literal""')
              Arguments(1):
                  IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: literal) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 'literal')
                    ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: ""literal"") (Syntax: 'literal')
                    InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        Next (Regular) Block[B4]
    Block[B4] - Block
        Predecessors: [B2] [B3]
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'custom = $""literal"";')
              Expression:
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: CustomHandler) (Syntax: 'custom = $""literal""')
                  Left:
                    IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: CustomHandler, IsImplicit) (Syntax: 'custom')
                  Right:
                    IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: CustomHandler, IsImplicit) (Syntax: '$""literal""')
        Next (Regular) Block[B5]
            Leaving: {R1}
}
Block[B5] - Exit
    Predecessors: [B4]
    Statements (0)
";

            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(new[] { code, handlerType }, expectedFlowGraph, expectedDiagnostics, parseOptions: TestOptions.RegularPreview);
        }

        [Fact]
        public void InterpolatedStringHandlerConversionFlow_07()
        {
            var code = @"
using System;
using System.Runtime.CompilerServices;

public class C
{
    public void M(bool b, [InterpolatedStringHandlerArgument("""", ""b"")]CustomHandler c) {}

    public void M1(C c)
    /*<bind>*/{
        c.M(true, $""literal{c,1:format}"");
    }/*</bind>*/
}

public partial struct CustomHandler
{
    public CustomHandler(int literalLength, int formattedCount, C c, bool b, out bool success) : this()
    {
        success = true;
    }
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
    CaptureIds: [0] [1] [2]
    Block[B1] - Block
        Predecessors: [B0]
        Statements (2)
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'c')
              Value:
                IParameterReferenceOperation: c (OperationKind.ParameterReference, Type: C) (Syntax: 'c')
            IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'true')
              Value:
                ILiteralOperation (OperationKind.Literal, Type: System.Boolean, Constant: True) (Syntax: 'true')
        Next (Regular) Block[B2]
            Entering: {R2}
    .locals {R2}
    {
        CaptureIds: [3]
        Block[B2] - Block
            Predecessors: [B1]
            Statements (1)
                IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: '$""literal{c,1:format}""')
                  Value:
                    IObjectCreationOperation (Constructor: CustomHandler..ctor(System.Int32 literalLength, System.Int32 formattedCount, C c, System.Boolean b, out System.Boolean success)) (OperationKind.ObjectCreation, Type: CustomHandler, IsImplicit) (Syntax: '$""literal{c,1:format}""')
                      Arguments(5):
                          IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: literalLength) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: '$""literal{c,1:format}""')
                            ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 7, IsImplicit) (Syntax: '$""literal{c,1:format}""')
                            InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                            OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                          IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: formattedCount) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: '$""literal{c,1:format}""')
                            ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1, IsImplicit) (Syntax: '$""literal{c,1:format}""')
                            InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                            OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                          IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: c) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 'c')
                            IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: C, IsImplicit) (Syntax: 'c')
                            InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                            OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                          IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: b) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 'true')
                            IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.Boolean, Constant: True, IsImplicit) (Syntax: 'true')
                            InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                            OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                          IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: success) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: '$""literal{c,1:format}""')
                            IFlowCaptureReferenceOperation: 3 (IsInitialization) (OperationKind.FlowCaptureReference, Type: null, IsImplicit) (Syntax: '$""literal{c,1:format}""')
                            InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                            OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                      Initializer:
                        null
            Jump if False (Regular) to Block[B4]
                IFlowCaptureReferenceOperation: 3 (OperationKind.FlowCaptureReference, Type: null, IsImplicit) (Syntax: '$""literal{c,1:format}""')
                Leaving: {R2}
            Next (Regular) Block[B3]
                Leaving: {R2}
    }
    Block[B3] - Block
        Predecessors: [B2]
        Statements (2)
            IInvocationOperation ( void CustomHandler.AppendLiteral(System.String literal)) (OperationKind.Invocation, Type: System.Void, IsImplicit) (Syntax: 'literal')
              Instance Receiver:
                IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: CustomHandler, IsImplicit) (Syntax: '$""literal{c,1:format}""')
              Arguments(1):
                  IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: literal) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 'literal')
                    ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: ""literal"") (Syntax: 'literal')
                    InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
            IInvocationOperation ( void CustomHandler.AppendFormatted(System.Object o, [System.Int32 alignment = 0], [System.String format = null])) (OperationKind.Invocation, Type: System.Void, IsImplicit) (Syntax: '{c,1:format}')
              Instance Receiver:
                IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: CustomHandler, IsImplicit) (Syntax: '$""literal{c,1:format}""')
              Arguments(3):
                  IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: o) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 'c')
                    IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Object, IsImplicit) (Syntax: 'c')
                      Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
                        (ImplicitReference)
                      Operand:
                        IParameterReferenceOperation: c (OperationKind.ParameterReference, Type: C) (Syntax: 'c')
                    InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: alignment) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: '1')
                    ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
                    InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: format) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: ':format')
                    ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: ""format"") (Syntax: ':format')
                    InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        Next (Regular) Block[B4]
    Block[B4] - Block
        Predecessors: [B2] [B3]
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'c.M(true, $ ... :format}"");')
              Expression:
                IInvocationOperation ( void C.M(System.Boolean b, CustomHandler c)) (OperationKind.Invocation, Type: System.Void) (Syntax: 'c.M(true, $ ... 1:format}"")')
                  Instance Receiver:
                    IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: C, IsImplicit) (Syntax: 'c')
                  Arguments(2):
                      IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: b) (OperationKind.Argument, Type: null) (Syntax: 'true')
                        IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.Boolean, Constant: True, IsImplicit) (Syntax: 'true')
                        InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                      IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: c) (OperationKind.Argument, Type: null) (Syntax: '$""literal{c,1:format}""')
                        IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: CustomHandler, IsImplicit) (Syntax: '$""literal{c,1:format}""')
                        InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        Next (Regular) Block[B5]
            Leaving: {R1}
}
Block[B5] - Exit
    Predecessors: [B4]
    Statements (0)
";

            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(new[] { code, GetInterpolatedStringCustomHandlerType("CustomHandler", "partial struct", useBoolReturns: false), InterpolatedStringHandlerArgumentAttribute },
                                                              expectedFlowGraph, expectedDiagnostics, parseOptions: TestOptions.RegularPreview);
        }

        [Fact]
        public void InterpolatedStringHandlerConversionFlow_08()
        {
            var code = @"
using System;
using System.Runtime.CompilerServices;

public class C
{
    public void M(bool b, [InterpolatedStringHandlerArgument(""b"")]CustomHandler c) {}

    public void M1(C c)
    /*<bind>*/{
        c.M(true, $""literal{c,1:format}"");
    }/*</bind>*/
}

public partial struct CustomHandler
{
    public CustomHandler(int literalLength, int formattedCount, bool b, out bool success) : this()
    {
        success = true;
    }
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
    CaptureIds: [0] [1] [2]
    Block[B1] - Block
        Predecessors: [B0]
        Statements (2)
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'c')
              Value:
                IParameterReferenceOperation: c (OperationKind.ParameterReference, Type: C) (Syntax: 'c')
            IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'true')
              Value:
                ILiteralOperation (OperationKind.Literal, Type: System.Boolean, Constant: True) (Syntax: 'true')
        Next (Regular) Block[B2]
            Entering: {R2}
    .locals {R2}
    {
        CaptureIds: [3]
        Block[B2] - Block
            Predecessors: [B1]
            Statements (1)
                IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: '$""literal{c,1:format}""')
                  Value:
                    IObjectCreationOperation (Constructor: CustomHandler..ctor(System.Int32 literalLength, System.Int32 formattedCount, System.Boolean b, out System.Boolean success)) (OperationKind.ObjectCreation, Type: CustomHandler, IsImplicit) (Syntax: '$""literal{c,1:format}""')
                      Arguments(4):
                          IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: literalLength) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: '$""literal{c,1:format}""')
                            ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 7, IsImplicit) (Syntax: '$""literal{c,1:format}""')
                            InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                            OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                          IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: formattedCount) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: '$""literal{c,1:format}""')
                            ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1, IsImplicit) (Syntax: '$""literal{c,1:format}""')
                            InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                            OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                          IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: b) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 'true')
                            IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.Boolean, Constant: True, IsImplicit) (Syntax: 'true')
                            InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                            OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                          IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: success) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: '$""literal{c,1:format}""')
                            IFlowCaptureReferenceOperation: 3 (IsInitialization) (OperationKind.FlowCaptureReference, Type: null, IsImplicit) (Syntax: '$""literal{c,1:format}""')
                            InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                            OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                      Initializer:
                        null
            Jump if False (Regular) to Block[B4]
                IFlowCaptureReferenceOperation: 3 (OperationKind.FlowCaptureReference, Type: null, IsImplicit) (Syntax: '$""literal{c,1:format}""')
                Leaving: {R2}
            Next (Regular) Block[B3]
                Leaving: {R2}
    }
    Block[B3] - Block
        Predecessors: [B2]
        Statements (2)
            IInvocationOperation ( void CustomHandler.AppendLiteral(System.String literal)) (OperationKind.Invocation, Type: System.Void, IsImplicit) (Syntax: 'literal')
              Instance Receiver:
                IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: CustomHandler, IsImplicit) (Syntax: '$""literal{c,1:format}""')
              Arguments(1):
                  IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: literal) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 'literal')
                    ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: ""literal"") (Syntax: 'literal')
                    InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
            IInvocationOperation ( void CustomHandler.AppendFormatted(System.Object o, [System.Int32 alignment = 0], [System.String format = null])) (OperationKind.Invocation, Type: System.Void, IsImplicit) (Syntax: '{c,1:format}')
              Instance Receiver:
                IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: CustomHandler, IsImplicit) (Syntax: '$""literal{c,1:format}""')
              Arguments(3):
                  IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: o) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 'c')
                    IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Object, IsImplicit) (Syntax: 'c')
                      Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
                        (ImplicitReference)
                      Operand:
                        IParameterReferenceOperation: c (OperationKind.ParameterReference, Type: C) (Syntax: 'c')
                    InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: alignment) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: '1')
                    ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
                    InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: format) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: ':format')
                    ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: ""format"") (Syntax: ':format')
                    InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        Next (Regular) Block[B4]
    Block[B4] - Block
        Predecessors: [B2] [B3]
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'c.M(true, $ ... :format}"");')
              Expression:
                IInvocationOperation ( void C.M(System.Boolean b, CustomHandler c)) (OperationKind.Invocation, Type: System.Void) (Syntax: 'c.M(true, $ ... 1:format}"")')
                  Instance Receiver:
                    IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: C, IsImplicit) (Syntax: 'c')
                  Arguments(2):
                      IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: b) (OperationKind.Argument, Type: null) (Syntax: 'true')
                        IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.Boolean, Constant: True, IsImplicit) (Syntax: 'true')
                        InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                      IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: c) (OperationKind.Argument, Type: null) (Syntax: '$""literal{c,1:format}""')
                        IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: CustomHandler, IsImplicit) (Syntax: '$""literal{c,1:format}""')
                        InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        Next (Regular) Block[B5]
            Leaving: {R1}
}
Block[B5] - Exit
    Predecessors: [B4]
    Statements (0)
";

            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(new[] { code, GetInterpolatedStringCustomHandlerType("CustomHandler", "partial struct", useBoolReturns: false), InterpolatedStringHandlerArgumentAttribute },
                                                              expectedFlowGraph, expectedDiagnostics, parseOptions: TestOptions.RegularPreview);
        }

        [Fact]
        public void InterpolatedStringHandlerConversionFlow_09()
        {
            var code = @"
using System;
using System.Runtime.CompilerServices;

public class C
{
    public void M(bool b, [InterpolatedStringHandlerArgument("""")]CustomHandler c) {}

    public void M1(C c)
    /*<bind>*/{
        c.M(true, $""literal{c,1:format}"");
    }/*</bind>*/
}

public partial struct CustomHandler
{
    public CustomHandler(int literalLength, int formattedCount, C c, out bool success) : this()
    {
        success = true;
    }
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
    CaptureIds: [0] [1] [2]
    Block[B1] - Block
        Predecessors: [B0]
        Statements (2)
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'c')
              Value:
                IParameterReferenceOperation: c (OperationKind.ParameterReference, Type: C) (Syntax: 'c')
            IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'true')
              Value:
                ILiteralOperation (OperationKind.Literal, Type: System.Boolean, Constant: True) (Syntax: 'true')
        Next (Regular) Block[B2]
            Entering: {R2}
    .locals {R2}
    {
        CaptureIds: [3]
        Block[B2] - Block
            Predecessors: [B1]
            Statements (1)
                IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: '$""literal{c,1:format}""')
                  Value:
                    IObjectCreationOperation (Constructor: CustomHandler..ctor(System.Int32 literalLength, System.Int32 formattedCount, C c, out System.Boolean success)) (OperationKind.ObjectCreation, Type: CustomHandler, IsImplicit) (Syntax: '$""literal{c,1:format}""')
                      Arguments(4):
                          IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: literalLength) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: '$""literal{c,1:format}""')
                            ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 7, IsImplicit) (Syntax: '$""literal{c,1:format}""')
                            InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                            OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                          IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: formattedCount) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: '$""literal{c,1:format}""')
                            ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1, IsImplicit) (Syntax: '$""literal{c,1:format}""')
                            InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                            OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                          IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: c) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 'c')
                            IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: C, IsImplicit) (Syntax: 'c')
                            InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                            OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                          IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: success) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: '$""literal{c,1:format}""')
                            IFlowCaptureReferenceOperation: 3 (IsInitialization) (OperationKind.FlowCaptureReference, Type: null, IsImplicit) (Syntax: '$""literal{c,1:format}""')
                            InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                            OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                      Initializer:
                        null
            Jump if False (Regular) to Block[B4]
                IFlowCaptureReferenceOperation: 3 (OperationKind.FlowCaptureReference, Type: null, IsImplicit) (Syntax: '$""literal{c,1:format}""')
                Leaving: {R2}
            Next (Regular) Block[B3]
                Leaving: {R2}
    }
    Block[B3] - Block
        Predecessors: [B2]
        Statements (2)
            IInvocationOperation ( void CustomHandler.AppendLiteral(System.String literal)) (OperationKind.Invocation, Type: System.Void, IsImplicit) (Syntax: 'literal')
              Instance Receiver:
                IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: CustomHandler, IsImplicit) (Syntax: '$""literal{c,1:format}""')
              Arguments(1):
                  IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: literal) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 'literal')
                    ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: ""literal"") (Syntax: 'literal')
                    InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
            IInvocationOperation ( void CustomHandler.AppendFormatted(System.Object o, [System.Int32 alignment = 0], [System.String format = null])) (OperationKind.Invocation, Type: System.Void, IsImplicit) (Syntax: '{c,1:format}')
              Instance Receiver:
                IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: CustomHandler, IsImplicit) (Syntax: '$""literal{c,1:format}""')
              Arguments(3):
                  IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: o) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 'c')
                    IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Object, IsImplicit) (Syntax: 'c')
                      Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
                        (ImplicitReference)
                      Operand:
                        IParameterReferenceOperation: c (OperationKind.ParameterReference, Type: C) (Syntax: 'c')
                    InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: alignment) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: '1')
                    ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
                    InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: format) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: ':format')
                    ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: ""format"") (Syntax: ':format')
                    InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        Next (Regular) Block[B4]
    Block[B4] - Block
        Predecessors: [B2] [B3]
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'c.M(true, $ ... :format}"");')
              Expression:
                IInvocationOperation ( void C.M(System.Boolean b, CustomHandler c)) (OperationKind.Invocation, Type: System.Void) (Syntax: 'c.M(true, $ ... 1:format}"")')
                  Instance Receiver:
                    IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: C, IsImplicit) (Syntax: 'c')
                  Arguments(2):
                      IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: b) (OperationKind.Argument, Type: null) (Syntax: 'true')
                        IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.Boolean, Constant: True, IsImplicit) (Syntax: 'true')
                        InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                      IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: c) (OperationKind.Argument, Type: null) (Syntax: '$""literal{c,1:format}""')
                        IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: CustomHandler, IsImplicit) (Syntax: '$""literal{c,1:format}""')
                        InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        Next (Regular) Block[B5]
            Leaving: {R1}
}
Block[B5] - Exit
    Predecessors: [B4]
    Statements (0)
";

            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(new[] { code, GetInterpolatedStringCustomHandlerType("CustomHandler", "partial struct", useBoolReturns: false), InterpolatedStringHandlerArgumentAttribute },
                                                              expectedFlowGraph, expectedDiagnostics, parseOptions: TestOptions.RegularPreview);
        }

        [Fact]
        public void InterpolatedStringHandlerConversionFlow_10()
        {
            var code = @"
using System;
using System.Runtime.CompilerServices;

public class C
{
    public void M(bool b, [InterpolatedStringHandlerArgument("""", ""b"")]CustomHandler c, int d) {}

    public void M1(C c)
    /*<bind>*/{
        c.M(true, $""literal{c,1:format}"", 1);
    }/*</bind>*/
}

public partial struct CustomHandler
{
    public CustomHandler(int literalLength, int formattedCount, C c, bool b, out bool success) : this()
    {
        success = true;
    }
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
    CaptureIds: [0] [1] [2]
    Block[B1] - Block
        Predecessors: [B0]
        Statements (2)
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'c')
              Value:
                IParameterReferenceOperation: c (OperationKind.ParameterReference, Type: C) (Syntax: 'c')
            IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'true')
              Value:
                ILiteralOperation (OperationKind.Literal, Type: System.Boolean, Constant: True) (Syntax: 'true')
        Next (Regular) Block[B2]
            Entering: {R2}
    .locals {R2}
    {
        CaptureIds: [3]
        Block[B2] - Block
            Predecessors: [B1]
            Statements (1)
                IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: '$""literal{c,1:format}""')
                  Value:
                    IObjectCreationOperation (Constructor: CustomHandler..ctor(System.Int32 literalLength, System.Int32 formattedCount, C c, System.Boolean b, out System.Boolean success)) (OperationKind.ObjectCreation, Type: CustomHandler, IsImplicit) (Syntax: '$""literal{c,1:format}""')
                      Arguments(5):
                          IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: literalLength) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: '$""literal{c,1:format}""')
                            ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 7, IsImplicit) (Syntax: '$""literal{c,1:format}""')
                            InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                            OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                          IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: formattedCount) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: '$""literal{c,1:format}""')
                            ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1, IsImplicit) (Syntax: '$""literal{c,1:format}""')
                            InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                            OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                          IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: c) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 'c')
                            IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: C, IsImplicit) (Syntax: 'c')
                            InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                            OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                          IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: b) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 'true')
                            IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.Boolean, Constant: True, IsImplicit) (Syntax: 'true')
                            InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                            OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                          IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: success) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: '$""literal{c,1:format}""')
                            IFlowCaptureReferenceOperation: 3 (IsInitialization) (OperationKind.FlowCaptureReference, Type: null, IsImplicit) (Syntax: '$""literal{c,1:format}""')
                            InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                            OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                      Initializer:
                        null
            Jump if False (Regular) to Block[B4]
                IFlowCaptureReferenceOperation: 3 (OperationKind.FlowCaptureReference, Type: null, IsImplicit) (Syntax: '$""literal{c,1:format}""')
                Leaving: {R2}
            Next (Regular) Block[B3]
                Leaving: {R2}
    }
    Block[B3] - Block
        Predecessors: [B2]
        Statements (2)
            IInvocationOperation ( void CustomHandler.AppendLiteral(System.String literal)) (OperationKind.Invocation, Type: System.Void, IsImplicit) (Syntax: 'literal')
              Instance Receiver:
                IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: CustomHandler, IsImplicit) (Syntax: '$""literal{c,1:format}""')
              Arguments(1):
                  IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: literal) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 'literal')
                    ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: ""literal"") (Syntax: 'literal')
                    InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
            IInvocationOperation ( void CustomHandler.AppendFormatted(System.Object o, [System.Int32 alignment = 0], [System.String format = null])) (OperationKind.Invocation, Type: System.Void, IsImplicit) (Syntax: '{c,1:format}')
              Instance Receiver:
                IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: CustomHandler, IsImplicit) (Syntax: '$""literal{c,1:format}""')
              Arguments(3):
                  IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: o) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 'c')
                    IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Object, IsImplicit) (Syntax: 'c')
                      Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
                        (ImplicitReference)
                      Operand:
                        IParameterReferenceOperation: c (OperationKind.ParameterReference, Type: C) (Syntax: 'c')
                    InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: alignment) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: '1')
                    ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
                    InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: format) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: ':format')
                    ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: ""format"") (Syntax: ':format')
                    InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        Next (Regular) Block[B4]
    Block[B4] - Block
        Predecessors: [B2] [B3]
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'c.M(true, $ ... rmat}"", 1);')
              Expression:
                IInvocationOperation ( void C.M(System.Boolean b, CustomHandler c, System.Int32 d)) (OperationKind.Invocation, Type: System.Void) (Syntax: 'c.M(true, $ ... ormat}"", 1)')
                  Instance Receiver:
                    IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: C, IsImplicit) (Syntax: 'c')
                  Arguments(3):
                      IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: b) (OperationKind.Argument, Type: null) (Syntax: 'true')
                        IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.Boolean, Constant: True, IsImplicit) (Syntax: 'true')
                        InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                      IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: c) (OperationKind.Argument, Type: null) (Syntax: '$""literal{c,1:format}""')
                        IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: CustomHandler, IsImplicit) (Syntax: '$""literal{c,1:format}""')
                        InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                      IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: d) (OperationKind.Argument, Type: null) (Syntax: '1')
                        ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
                        InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        Next (Regular) Block[B5]
            Leaving: {R1}
}
Block[B5] - Exit
    Predecessors: [B4]
    Statements (0)
";

            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(new[] { code, GetInterpolatedStringCustomHandlerType("CustomHandler", "partial struct", useBoolReturns: false), InterpolatedStringHandlerArgumentAttribute },
                                                              expectedFlowGraph, expectedDiagnostics, parseOptions: TestOptions.RegularPreview);
        }

        [Fact]
        public void InterpolatedStringHandlerConversionFlow_11()
        {
            var code = @"
using System;
using System.Runtime.CompilerServices;

public class C
{
    public void M(bool b, [InterpolatedStringHandlerArgument("""", ""b"")]CustomHandler c1, int d, [InterpolatedStringHandlerArgument("""", ""d"")]CustomHandler c2) {}

    public void M1(C c)
    /*<bind>*/{
        c.M(true, $""literal{c,1:format}"", 1, $""literal{c,2:format}"");
    }/*</bind>*/
}

public partial struct CustomHandler
{
    public CustomHandler(int literalLength, int formattedCount, C c, bool b, out bool success) : this()
    {
        success = true;
    }

    public CustomHandler(int literalLength, int formattedCount, C c, int i, out bool success) : this()
    {
        success = true;
    }
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
    CaptureIds: [0] [1] [2] [4] [5]
    Block[B1] - Block
        Predecessors: [B0]
        Statements (2)
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'c')
              Value:
                IParameterReferenceOperation: c (OperationKind.ParameterReference, Type: C) (Syntax: 'c')
            IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'true')
              Value:
                ILiteralOperation (OperationKind.Literal, Type: System.Boolean, Constant: True) (Syntax: 'true')
        Next (Regular) Block[B2]
            Entering: {R2}
    .locals {R2}
    {
        CaptureIds: [3]
        Block[B2] - Block
            Predecessors: [B1]
            Statements (1)
                IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: '$""literal{c,1:format}""')
                  Value:
                    IObjectCreationOperation (Constructor: CustomHandler..ctor(System.Int32 literalLength, System.Int32 formattedCount, C c, System.Boolean b, out System.Boolean success)) (OperationKind.ObjectCreation, Type: CustomHandler, IsImplicit) (Syntax: '$""literal{c,1:format}""')
                      Arguments(5):
                          IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: literalLength) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: '$""literal{c,1:format}""')
                            ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 7, IsImplicit) (Syntax: '$""literal{c,1:format}""')
                            InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                            OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                          IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: formattedCount) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: '$""literal{c,1:format}""')
                            ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1, IsImplicit) (Syntax: '$""literal{c,1:format}""')
                            InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                            OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                          IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: c) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 'c')
                            IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: C, IsImplicit) (Syntax: 'c')
                            InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                            OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                          IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: b) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 'true')
                            IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.Boolean, Constant: True, IsImplicit) (Syntax: 'true')
                            InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                            OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                          IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: success) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: '$""literal{c,1:format}""')
                            IFlowCaptureReferenceOperation: 3 (IsInitialization) (OperationKind.FlowCaptureReference, Type: null, IsImplicit) (Syntax: '$""literal{c,1:format}""')
                            InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                            OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                      Initializer:
                        null
            Jump if False (Regular) to Block[B4]
                IFlowCaptureReferenceOperation: 3 (OperationKind.FlowCaptureReference, Type: null, IsImplicit) (Syntax: '$""literal{c,1:format}""')
                Leaving: {R2}
            Next (Regular) Block[B3]
                Leaving: {R2}
    }
    Block[B3] - Block
        Predecessors: [B2]
        Statements (2)
            IInvocationOperation ( void CustomHandler.AppendLiteral(System.String literal)) (OperationKind.Invocation, Type: System.Void, IsImplicit) (Syntax: 'literal')
              Instance Receiver:
                IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: CustomHandler, IsImplicit) (Syntax: '$""literal{c,1:format}""')
              Arguments(1):
                  IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: literal) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 'literal')
                    ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: ""literal"") (Syntax: 'literal')
                    InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
            IInvocationOperation ( void CustomHandler.AppendFormatted(System.Object o, [System.Int32 alignment = 0], [System.String format = null])) (OperationKind.Invocation, Type: System.Void, IsImplicit) (Syntax: '{c,1:format}')
              Instance Receiver:
                IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: CustomHandler, IsImplicit) (Syntax: '$""literal{c,1:format}""')
              Arguments(3):
                  IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: o) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 'c')
                    IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Object, IsImplicit) (Syntax: 'c')
                      Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
                        (ImplicitReference)
                      Operand:
                        IParameterReferenceOperation: c (OperationKind.ParameterReference, Type: C) (Syntax: 'c')
                    InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: alignment) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: '1')
                    ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
                    InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: format) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: ':format')
                    ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: ""format"") (Syntax: ':format')
                    InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        Next (Regular) Block[B4]
    Block[B4] - Block
        Predecessors: [B2] [B3]
        Statements (1)
            IFlowCaptureOperation: 4 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: '1')
              Value:
                ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
        Next (Regular) Block[B5]
            Entering: {R3}
    .locals {R3}
    {
        CaptureIds: [6]
        Block[B5] - Block
            Predecessors: [B4]
            Statements (1)
                IFlowCaptureOperation: 5 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: '$""literal{c,2:format}""')
                  Value:
                    IObjectCreationOperation (Constructor: CustomHandler..ctor(System.Int32 literalLength, System.Int32 formattedCount, C c, System.Int32 i, out System.Boolean success)) (OperationKind.ObjectCreation, Type: CustomHandler, IsImplicit) (Syntax: '$""literal{c,2:format}""')
                      Arguments(5):
                          IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: literalLength) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: '$""literal{c,2:format}""')
                            ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 7, IsImplicit) (Syntax: '$""literal{c,2:format}""')
                            InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                            OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                          IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: formattedCount) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: '$""literal{c,2:format}""')
                            ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1, IsImplicit) (Syntax: '$""literal{c,2:format}""')
                            InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                            OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                          IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: c) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 'c')
                            IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: C, IsImplicit) (Syntax: 'c')
                            InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                            OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                          IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: i) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: '1')
                            IFlowCaptureReferenceOperation: 4 (OperationKind.FlowCaptureReference, Type: System.Int32, Constant: 1, IsImplicit) (Syntax: '1')
                            InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                            OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                          IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: success) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: '$""literal{c,2:format}""')
                            IFlowCaptureReferenceOperation: 6 (IsInitialization) (OperationKind.FlowCaptureReference, Type: null, IsImplicit) (Syntax: '$""literal{c,2:format}""')
                            InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                            OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                      Initializer:
                        null
            Jump if False (Regular) to Block[B7]
                IFlowCaptureReferenceOperation: 6 (OperationKind.FlowCaptureReference, Type: null, IsImplicit) (Syntax: '$""literal{c,2:format}""')
                Leaving: {R3}
            Next (Regular) Block[B6]
                Leaving: {R3}
    }
    Block[B6] - Block
        Predecessors: [B5]
        Statements (2)
            IInvocationOperation ( void CustomHandler.AppendLiteral(System.String literal)) (OperationKind.Invocation, Type: System.Void, IsImplicit) (Syntax: 'literal')
              Instance Receiver:
                IFlowCaptureReferenceOperation: 5 (OperationKind.FlowCaptureReference, Type: CustomHandler, IsImplicit) (Syntax: '$""literal{c,2:format}""')
              Arguments(1):
                  IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: literal) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 'literal')
                    ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: ""literal"") (Syntax: 'literal')
                    InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
            IInvocationOperation ( void CustomHandler.AppendFormatted(System.Object o, [System.Int32 alignment = 0], [System.String format = null])) (OperationKind.Invocation, Type: System.Void, IsImplicit) (Syntax: '{c,2:format}')
              Instance Receiver:
                IFlowCaptureReferenceOperation: 5 (OperationKind.FlowCaptureReference, Type: CustomHandler, IsImplicit) (Syntax: '$""literal{c,2:format}""')
              Arguments(3):
                  IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: o) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 'c')
                    IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Object, IsImplicit) (Syntax: 'c')
                      Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
                        (ImplicitReference)
                      Operand:
                        IParameterReferenceOperation: c (OperationKind.ParameterReference, Type: C) (Syntax: 'c')
                    InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: alignment) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: '2')
                    ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 2) (Syntax: '2')
                    InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: format) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: ':format')
                    ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: ""format"") (Syntax: ':format')
                    InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        Next (Regular) Block[B7]
    Block[B7] - Block
        Predecessors: [B5] [B6]
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'c.M(true, $ ... :format}"");')
              Expression:
                IInvocationOperation ( void C.M(System.Boolean b, CustomHandler c1, System.Int32 d, CustomHandler c2)) (OperationKind.Invocation, Type: System.Void) (Syntax: 'c.M(true, $ ... 2:format}"")')
                  Instance Receiver:
                    IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: C, IsImplicit) (Syntax: 'c')
                  Arguments(4):
                      IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: b) (OperationKind.Argument, Type: null) (Syntax: 'true')
                        IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.Boolean, Constant: True, IsImplicit) (Syntax: 'true')
                        InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                      IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: c1) (OperationKind.Argument, Type: null) (Syntax: '$""literal{c,1:format}""')
                        IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: CustomHandler, IsImplicit) (Syntax: '$""literal{c,1:format}""')
                        InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                      IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: d) (OperationKind.Argument, Type: null) (Syntax: '1')
                        IFlowCaptureReferenceOperation: 4 (OperationKind.FlowCaptureReference, Type: System.Int32, Constant: 1, IsImplicit) (Syntax: '1')
                        InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                      IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: c2) (OperationKind.Argument, Type: null) (Syntax: '$""literal{c,2:format}""')
                        IFlowCaptureReferenceOperation: 5 (OperationKind.FlowCaptureReference, Type: CustomHandler, IsImplicit) (Syntax: '$""literal{c,2:format}""')
                        InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        Next (Regular) Block[B8]
            Leaving: {R1}
}
Block[B8] - Exit
    Predecessors: [B7]
    Statements (0)
";

            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(new[] { code, GetInterpolatedStringCustomHandlerType("CustomHandler", "partial struct", useBoolReturns: false), InterpolatedStringHandlerArgumentAttribute },
                                                              expectedFlowGraph, expectedDiagnostics, parseOptions: TestOptions.RegularPreview);
        }

        [Fact]
        public void InterpolatedStringHandlerConversionFlow_12()
        {
            var code = @"
using System;
using System.Runtime.CompilerServices;

public class C
{
    public void M(bool b, [InterpolatedStringHandlerArgument("""", ""b"")]CustomHandler c) {}

    public void M1(C c)
    /*<bind>*/{
        c.M(c: $""literal{c,1:format}"", b: true);
    }/*</bind>*/
}

public partial struct CustomHandler
{
    public CustomHandler(int literalLength, int formattedCount, C c, bool b, out bool success) : this()
    {
        success = true;
    }
}
";

            var expectedDiagnostics = new DiagnosticDescription[] {
                // (11,43): error CS8950: Parameter 'b' is an argument to the interpolated string handler conversion on parameter 'c', but the corresponding argument is specified after the interpolated string expression. Reorder the arguments to move 'b' before 'c'.
                //         c.M(c: $"literal{c,1:format}", b: true);
                Diagnostic(ErrorCode.ERR_InterpolatedStringHandlerArgumentLocatedAfterInterpolatedString, "true").WithArguments("b", "c").WithLocation(11, 43)
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
        Statements (1)
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'c')
              Value:
                IParameterReferenceOperation: c (OperationKind.ParameterReference, Type: C) (Syntax: 'c')
        Next (Regular) Block[B2]
            Entering: {R2}
    .locals {R2}
    {
        CaptureIds: [2]
        Block[B2] - Block
            Predecessors: [B1]
            Statements (1)
                IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: '$""literal{c,1:format}""')
                  Value:
                    IObjectCreationOperation (Constructor: CustomHandler..ctor(System.Int32 literalLength, System.Int32 formattedCount, C c, System.Boolean b, out System.Boolean success)) (OperationKind.ObjectCreation, Type: CustomHandler, IsImplicit) (Syntax: '$""literal{c,1:format}""')
                      Arguments(5):
                          IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: literalLength) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 'c: $""litera ... ,1:format}""')
                            ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 7, IsImplicit) (Syntax: '$""literal{c,1:format}""')
                            InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                            OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                          IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: formattedCount) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 'c: $""litera ... ,1:format}""')
                            ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1, IsImplicit) (Syntax: '$""literal{c,1:format}""')
                            InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                            OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                          IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: c) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 'c')
                            IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: C, IsImplicit) (Syntax: 'c')
                            InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                            OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                          IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: b) (OperationKind.Argument, Type: null, IsInvalid, IsImplicit) (Syntax: 'b: true')
                            IInvalidOperation (OperationKind.Invalid, Type: null, IsInvalid, IsImplicit) (Syntax: 'true')
                              Children(0)
                            InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                            OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                          IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: success) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 'c: $""litera ... ,1:format}""')
                            IFlowCaptureReferenceOperation: 2 (IsInitialization) (OperationKind.FlowCaptureReference, Type: null, IsImplicit) (Syntax: '$""literal{c,1:format}""')
                            InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                            OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                      Initializer:
                        null
            Jump if False (Regular) to Block[B4]
                IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: null, IsImplicit) (Syntax: '$""literal{c,1:format}""')
                Leaving: {R2}
            Next (Regular) Block[B3]
                Leaving: {R2}
    }
    Block[B3] - Block
        Predecessors: [B2]
        Statements (2)
            IInvocationOperation ( void CustomHandler.AppendLiteral(System.String literal)) (OperationKind.Invocation, Type: System.Void, IsImplicit) (Syntax: 'literal')
              Instance Receiver:
                IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: CustomHandler, IsImplicit) (Syntax: '$""literal{c,1:format}""')
              Arguments(1):
                  IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: literal) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 'literal')
                    ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: ""literal"") (Syntax: 'literal')
                    InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
            IInvocationOperation ( void CustomHandler.AppendFormatted(System.Object o, [System.Int32 alignment = 0], [System.String format = null])) (OperationKind.Invocation, Type: System.Void, IsImplicit) (Syntax: '{c,1:format}')
              Instance Receiver:
                IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: CustomHandler, IsImplicit) (Syntax: '$""literal{c,1:format}""')
              Arguments(3):
                  IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: o) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 'c')
                    IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Object, IsImplicit) (Syntax: 'c')
                      Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
                        (ImplicitReference)
                      Operand:
                        IParameterReferenceOperation: c (OperationKind.ParameterReference, Type: C) (Syntax: 'c')
                    InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: alignment) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: '1')
                    ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
                    InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: format) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: ':format')
                    ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: ""format"") (Syntax: ':format')
                    InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        Next (Regular) Block[B4]
    Block[B4] - Block
        Predecessors: [B2] [B3]
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null, IsInvalid) (Syntax: 'c.M(c: $""li ... , b: true);')
              Expression:
                IInvocationOperation ( void C.M(System.Boolean b, CustomHandler c)) (OperationKind.Invocation, Type: System.Void, IsInvalid) (Syntax: 'c.M(c: $""li ... "", b: true)')
                  Instance Receiver:
                    IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: C, IsImplicit) (Syntax: 'c')
                  Arguments(2):
                      IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: c) (OperationKind.Argument, Type: null) (Syntax: 'c: $""litera ... ,1:format}""')
                        IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: CustomHandler, IsImplicit) (Syntax: '$""literal{c,1:format}""')
                        InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                      IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: b) (OperationKind.Argument, Type: null, IsInvalid) (Syntax: 'b: true')
                        ILiteralOperation (OperationKind.Literal, Type: System.Boolean, Constant: True, IsInvalid) (Syntax: 'true')
                        InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        Next (Regular) Block[B5]
            Leaving: {R1}
}
Block[B5] - Exit
    Predecessors: [B4]
    Statements (0)

";

            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(new[] { code, GetInterpolatedStringCustomHandlerType("CustomHandler", "partial struct", useBoolReturns: false), InterpolatedStringHandlerArgumentAttribute },
                                                              expectedFlowGraph, expectedDiagnostics, parseOptions: TestOptions.RegularPreview);
        }

        [Fact]
        public void InterpolatedStringHandlerConversionFlow_13()
        {
            var code = @"
using System;
using System.Runtime.CompilerServices;


public class C
{

    public void M1(C c)
    /*<bind>*/{
        _ = new C1 { C2 = { [true, $""literal""] = { A = 1, B = 2 } } };
    }/*</bind>*/
}

class C1
{
    public C2 C2 { get => null; set { } }
}

public class C2
{
    public C3 this[bool b, [InterpolatedStringHandlerArgument("""", ""b"")] CustomHandler c]
    {
        get => new C3();
        set { }
    }
}

public class C3
{
    public int A
    {
        get => 0;
        set { }
    }
    public int B
    {
        get => 0;
        set { }
    }
}

public partial struct CustomHandler
{
    public CustomHandler(int literalLength, int formattedCount, C2 c, bool b) : this(literalLength, formattedCount)
    {
        Console.WriteLine(""CustomHandler ctor"");
    }
}
";

            var expectedDiagnostics = new[] {
                // (11,36): error CS8976: Interpolated string handler conversions that reference the instance being indexed cannot be used in indexer member initializers.
                //         _ = new C1 { C2 = { [true, $"literal"] = { A = 1, B = 2 } } };
                Diagnostic(ErrorCode.ERR_InterpolatedStringsReferencingInstanceCannotBeInObjectInitializers, @"$""literal""").WithLocation(11, 36)
            };

            string expectedFlowGraph = @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
        Entering: {R1}
.locals {R1}
{
    CaptureIds: [0]
    Block[B1] - Block
        Predecessors: [B0]
        Statements (1)
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsInvalid, IsImplicit) (Syntax: 'new C1 { C2 ... B = 2 } } }')
              Value:
                IObjectCreationOperation (Constructor: C1..ctor()) (OperationKind.ObjectCreation, Type: C1, IsInvalid) (Syntax: 'new C1 { C2 ... B = 2 } } }')
                  Arguments(0)
                  Initializer:
                    null
        Next (Regular) Block[B2]
            Entering: {R2}
    .locals {R2}
    {
        CaptureIds: [1] [2]
        Block[B2] - Block
            Predecessors: [B1]
            Statements (5)
                IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'true')
                  Value:
                    ILiteralOperation (OperationKind.Literal, Type: System.Boolean, Constant: True) (Syntax: 'true')
                IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsInvalid, IsImplicit) (Syntax: '$""literal""')
                  Value:
                    IObjectCreationOperation (Constructor: CustomHandler..ctor(System.Int32 literalLength, System.Int32 formattedCount, C2 c, System.Boolean b)) (OperationKind.ObjectCreation, Type: CustomHandler, IsInvalid, IsImplicit) (Syntax: '$""literal""')
                      Arguments(4):
                          IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: literalLength) (OperationKind.Argument, Type: null, IsInvalid, IsImplicit) (Syntax: '$""literal""')
                            ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 7, IsInvalid, IsImplicit) (Syntax: '$""literal""')
                            InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                            OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                          IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: formattedCount) (OperationKind.Argument, Type: null, IsInvalid, IsImplicit) (Syntax: '$""literal""')
                            ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 0, IsInvalid, IsImplicit) (Syntax: '$""literal""')
                            InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                            OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                          IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: c) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 'C2')
                            IInvalidOperation (OperationKind.Invalid, Type: null, IsImplicit) (Syntax: 'C2')
                              Children(0)
                            InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                            OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                          IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: b) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 'true')
                            IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.Boolean, Constant: True, IsImplicit) (Syntax: 'true')
                            InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                            OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                      Initializer:
                        null
                IInvocationOperation ( void CustomHandler.AppendLiteral(System.String literal)) (OperationKind.Invocation, Type: System.Void, IsInvalid, IsImplicit) (Syntax: 'literal')
                  Instance Receiver:
                    IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: CustomHandler, IsInvalid, IsImplicit) (Syntax: '$""literal""')
                  Arguments(1):
                      IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: literal) (OperationKind.Argument, Type: null, IsInvalid, IsImplicit) (Syntax: 'literal')
                        ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: ""literal"", IsInvalid) (Syntax: 'literal')
                        InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32) (Syntax: 'A = 1')
                  Left:
                    IPropertyReferenceOperation: System.Int32 C3.A { get; set; } (OperationKind.PropertyReference, Type: System.Int32) (Syntax: 'A')
                      Instance Receiver:
                        IPropertyReferenceOperation: C3 C2.this[System.Boolean b, CustomHandler c] { get; set; } (OperationKind.PropertyReference, Type: C3, IsInvalid) (Syntax: '[true, $""literal""]')
                          Instance Receiver:
                            IPropertyReferenceOperation: C2 C1.C2 { get; set; } (OperationKind.PropertyReference, Type: C2) (Syntax: 'C2')
                              Instance Receiver:
                                IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: C1, IsInvalid, IsImplicit) (Syntax: 'new C1 { C2 ... B = 2 } } }')
                          Arguments(2):
                              IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: b) (OperationKind.Argument, Type: null) (Syntax: 'true')
                                IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.Boolean, Constant: True, IsImplicit) (Syntax: 'true')
                                InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                              IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: c) (OperationKind.Argument, Type: null, IsInvalid) (Syntax: '$""literal""')
                                IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: CustomHandler, IsInvalid, IsImplicit) (Syntax: '$""literal""')
                                InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  Right:
                    ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32) (Syntax: 'B = 2')
                  Left:
                    IPropertyReferenceOperation: System.Int32 C3.B { get; set; } (OperationKind.PropertyReference, Type: System.Int32) (Syntax: 'B')
                      Instance Receiver:
                        IPropertyReferenceOperation: C3 C2.this[System.Boolean b, CustomHandler c] { get; set; } (OperationKind.PropertyReference, Type: C3, IsInvalid) (Syntax: '[true, $""literal""]')
                          Instance Receiver:
                            IPropertyReferenceOperation: C2 C1.C2 { get; set; } (OperationKind.PropertyReference, Type: C2) (Syntax: 'C2')
                              Instance Receiver:
                                IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: C1, IsInvalid, IsImplicit) (Syntax: 'new C1 { C2 ... B = 2 } } }')
                          Arguments(2):
                              IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: b) (OperationKind.Argument, Type: null) (Syntax: 'true')
                                IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.Boolean, Constant: True, IsImplicit) (Syntax: 'true')
                                InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                              IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: c) (OperationKind.Argument, Type: null, IsInvalid) (Syntax: '$""literal""')
                                IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: CustomHandler, IsInvalid, IsImplicit) (Syntax: '$""literal""')
                                InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  Right:
                    ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 2) (Syntax: '2')
            Next (Regular) Block[B3]
                Leaving: {R2}
    }
    Block[B3] - Block
        Predecessors: [B2]
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null, IsInvalid) (Syntax: '_ = new C1  ...  = 2 } } };')
              Expression:
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: C1, IsInvalid) (Syntax: '_ = new C1  ... B = 2 } } }')
                  Left:
                    IDiscardOperation (Symbol: C1 _) (OperationKind.Discard, Type: C1) (Syntax: '_')
                  Right:
                    IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: C1, IsInvalid, IsImplicit) (Syntax: 'new C1 { C2 ... B = 2 } } }')
        Next (Regular) Block[B4]
            Leaving: {R1}
}
Block[B4] - Exit
    Predecessors: [B3]
    Statements (0)
";

            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(new[] { code, GetInterpolatedStringCustomHandlerType("CustomHandler", "partial struct", useBoolReturns: false), InterpolatedStringHandlerArgumentAttribute },
                                                              expectedFlowGraph, expectedDiagnostics, parseOptions: TestOptions.RegularPreview);
        }

        [Fact]
        public void InterpolatedStringHandlerConversionFlow_14()
        {
            var code = @"
using System;
using System.Runtime.CompilerServices;

public class C
{
    public C(bool b, [InterpolatedStringHandlerArgument(""b"")]CustomHandler c1) {}

    public void M1(C c)
    /*<bind>*/{
        _ = new C(true, $""literal{c,1:format}"");
    }/*</bind>*/
}

public partial struct CustomHandler
{
    public CustomHandler(int literalLength, int formattedCount, bool b, out bool success) : this()
    {
        success = true;
    }
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
        Statements (1)
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'true')
              Value:
                ILiteralOperation (OperationKind.Literal, Type: System.Boolean, Constant: True) (Syntax: 'true')
        Next (Regular) Block[B2]
            Entering: {R2}
    .locals {R2}
    {
        CaptureIds: [2]
        Block[B2] - Block
            Predecessors: [B1]
            Statements (1)
                IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: '$""literal{c,1:format}""')
                  Value:
                    IObjectCreationOperation (Constructor: CustomHandler..ctor(System.Int32 literalLength, System.Int32 formattedCount, System.Boolean b, out System.Boolean success)) (OperationKind.ObjectCreation, Type: CustomHandler, IsImplicit) (Syntax: '$""literal{c,1:format}""')
                      Arguments(4):
                          IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: literalLength) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: '$""literal{c,1:format}""')
                            ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 7, IsImplicit) (Syntax: '$""literal{c,1:format}""')
                            InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                            OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                          IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: formattedCount) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: '$""literal{c,1:format}""')
                            ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1, IsImplicit) (Syntax: '$""literal{c,1:format}""')
                            InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                            OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                          IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: b) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 'true')
                            IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Boolean, Constant: True, IsImplicit) (Syntax: 'true')
                            InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                            OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                          IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: success) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: '$""literal{c,1:format}""')
                            IFlowCaptureReferenceOperation: 2 (IsInitialization) (OperationKind.FlowCaptureReference, Type: null, IsImplicit) (Syntax: '$""literal{c,1:format}""')
                            InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                            OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                      Initializer:
                        null
            Jump if False (Regular) to Block[B4]
                IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: null, IsImplicit) (Syntax: '$""literal{c,1:format}""')
                Leaving: {R2}
            Next (Regular) Block[B3]
                Leaving: {R2}
    }
    Block[B3] - Block
        Predecessors: [B2]
        Statements (2)
            IInvocationOperation ( void CustomHandler.AppendLiteral(System.String literal)) (OperationKind.Invocation, Type: System.Void, IsImplicit) (Syntax: 'literal')
              Instance Receiver:
                IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: CustomHandler, IsImplicit) (Syntax: '$""literal{c,1:format}""')
              Arguments(1):
                  IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: literal) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 'literal')
                    ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: ""literal"") (Syntax: 'literal')
                    InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
            IInvocationOperation ( void CustomHandler.AppendFormatted(System.Object o, [System.Int32 alignment = 0], [System.String format = null])) (OperationKind.Invocation, Type: System.Void, IsImplicit) (Syntax: '{c,1:format}')
              Instance Receiver:
                IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: CustomHandler, IsImplicit) (Syntax: '$""literal{c,1:format}""')
              Arguments(3):
                  IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: o) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 'c')
                    IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Object, IsImplicit) (Syntax: 'c')
                      Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
                        (ImplicitReference)
                      Operand:
                        IParameterReferenceOperation: c (OperationKind.ParameterReference, Type: C) (Syntax: 'c')
                    InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: alignment) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: '1')
                    ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
                    InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: format) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: ':format')
                    ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: ""format"") (Syntax: ':format')
                    InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        Next (Regular) Block[B4]
    Block[B4] - Block
        Predecessors: [B2] [B3]
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: '_ = new C(t ... :format}"");')
              Expression:
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: C) (Syntax: '_ = new C(t ... 1:format}"")')
                  Left:
                    IDiscardOperation (Symbol: C _) (OperationKind.Discard, Type: C) (Syntax: '_')
                  Right:
                    IObjectCreationOperation (Constructor: C..ctor(System.Boolean b, CustomHandler c1)) (OperationKind.ObjectCreation, Type: C) (Syntax: 'new C(true, ... 1:format}"")')
                      Arguments(2):
                          IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: b) (OperationKind.Argument, Type: null) (Syntax: 'true')
                            IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Boolean, Constant: True, IsImplicit) (Syntax: 'true')
                            InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                            OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                          IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: c1) (OperationKind.Argument, Type: null) (Syntax: '$""literal{c,1:format}""')
                            IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: CustomHandler, IsImplicit) (Syntax: '$""literal{c,1:format}""')
                            InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                            OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                      Initializer:
                        null
        Next (Regular) Block[B5]
            Leaving: {R1}
}
Block[B5] - Exit
    Predecessors: [B4]
    Statements (0)
";

            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(new[] { code, GetInterpolatedStringCustomHandlerType("CustomHandler", "partial struct", useBoolReturns: false), InterpolatedStringHandlerArgumentAttribute },
                                                              expectedFlowGraph, expectedDiagnostics, parseOptions: TestOptions.RegularPreview);
        }

        [Fact]
        public void InterpolatedStringHandlerConversionFlow_15()
        {
            var code = @"
using System;
using System.Runtime.CompilerServices;

public class C
{
    public C(bool b, [InterpolatedStringHandlerArgument("""", ""b"")]CustomHandler c1) {}

    public void M1(C c)
    /*<bind>*/{
        _ = new C(true, $""literal{c,1:format}"");
    }/*</bind>*/
}
";

            var expectedDiagnostics = new[] {
                // (7,23): error CS8944: 'C.C(bool, CustomHandler)' is not an instance method, the receiver cannot be an interpolated string handler argument.
                //     public C(bool b, [InterpolatedStringHandlerArgument("", "b")]CustomHandler c1) {}
                Diagnostic(ErrorCode.ERR_NotInstanceInvalidInterpolatedStringHandlerArgumentName, @"InterpolatedStringHandlerArgument("""", ""b"")").WithArguments("C.C(bool, CustomHandler)").WithLocation(7, 23),
                // (11,25): error CS8949: The InterpolatedStringHandlerArgumentAttribute applied to parameter 'CustomHandler c1' is malformed and cannot be interpreted. Construct an instance of 'CustomHandler' manually.
                //         _ = new C(true, $"literal{c,1:format}");
                Diagnostic(ErrorCode.ERR_InterpolatedStringHandlerArgumentAttributeMalformed, @"$""literal{c,1:format}""").WithArguments("CustomHandler c1", "CustomHandler").WithLocation(11, 25)
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
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'true')
              Value:
                ILiteralOperation (OperationKind.Literal, Type: System.Boolean, Constant: True) (Syntax: 'true')
            IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsInvalid, IsImplicit) (Syntax: '$""literal{c,1:format}""')
              Value:
                IObjectCreationOperation (Constructor: CustomHandler..ctor(System.Int32 literalLength, System.Int32 formattedCount)) (OperationKind.ObjectCreation, Type: CustomHandler, IsInvalid, IsImplicit) (Syntax: '$""literal{c,1:format}""')
                  Arguments(2):
                      IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: literalLength) (OperationKind.Argument, Type: null, IsInvalid, IsImplicit) (Syntax: '$""literal{c,1:format}""')
                        ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 7, IsInvalid, IsImplicit) (Syntax: '$""literal{c,1:format}""')
                        InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                      IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: formattedCount) (OperationKind.Argument, Type: null, IsInvalid, IsImplicit) (Syntax: '$""literal{c,1:format}""')
                        ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1, IsInvalid, IsImplicit) (Syntax: '$""literal{c,1:format}""')
                        InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  Initializer:
                    null
            IInvocationOperation ( void CustomHandler.AppendLiteral(System.String literal)) (OperationKind.Invocation, Type: System.Void, IsInvalid, IsImplicit) (Syntax: 'literal')
              Instance Receiver:
                IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: CustomHandler, IsInvalid, IsImplicit) (Syntax: '$""literal{c,1:format}""')
              Arguments(1):
                  IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: literal) (OperationKind.Argument, Type: null, IsInvalid, IsImplicit) (Syntax: 'literal')
                    ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: ""literal"", IsInvalid) (Syntax: 'literal')
                    InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
            IInvocationOperation ( void CustomHandler.AppendFormatted(System.Object o, [System.Int32 alignment = 0], [System.String format = null])) (OperationKind.Invocation, Type: System.Void, IsInvalid, IsImplicit) (Syntax: '{c,1:format}')
              Instance Receiver:
                IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: CustomHandler, IsInvalid, IsImplicit) (Syntax: '$""literal{c,1:format}""')
              Arguments(3):
                  IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: o) (OperationKind.Argument, Type: null, IsInvalid, IsImplicit) (Syntax: 'c')
                    IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Object, IsInvalid, IsImplicit) (Syntax: 'c')
                      Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
                        (ImplicitReference)
                      Operand:
                        IParameterReferenceOperation: c (OperationKind.ParameterReference, Type: C, IsInvalid) (Syntax: 'c')
                    InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: alignment) (OperationKind.Argument, Type: null, IsInvalid, IsImplicit) (Syntax: '1')
                    ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1, IsInvalid) (Syntax: '1')
                    InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: format) (OperationKind.Argument, Type: null, IsInvalid, IsImplicit) (Syntax: ':format')
                    ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: ""format"", IsInvalid) (Syntax: ':format')
                    InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null, IsInvalid) (Syntax: '_ = new C(t ... :format}"");')
              Expression:
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: C, IsInvalid) (Syntax: '_ = new C(t ... 1:format}"")')
                  Left:
                    IDiscardOperation (Symbol: C _) (OperationKind.Discard, Type: C) (Syntax: '_')
                  Right:
                    IObjectCreationOperation (Constructor: C..ctor(System.Boolean b, CustomHandler c1)) (OperationKind.ObjectCreation, Type: C, IsInvalid) (Syntax: 'new C(true, ... 1:format}"")')
                      Arguments(2):
                          IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: b) (OperationKind.Argument, Type: null) (Syntax: 'true')
                            IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Boolean, Constant: True, IsImplicit) (Syntax: 'true')
                            InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                            OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                          IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: c1) (OperationKind.Argument, Type: null, IsInvalid) (Syntax: '$""literal{c,1:format}""')
                            IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: CustomHandler, IsInvalid, IsImplicit) (Syntax: '$""literal{c,1:format}""')
                            InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                            OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                      Initializer:
                        null
        Next (Regular) Block[B2]
            Leaving: {R1}
}
Block[B2] - Exit
    Predecessors: [B1]
    Statements (0)
";

            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(new[] { code, GetInterpolatedStringCustomHandlerType("CustomHandler", "partial struct", useBoolReturns: false), InterpolatedStringHandlerArgumentAttribute },
                                                              expectedFlowGraph, expectedDiagnostics, parseOptions: TestOptions.RegularPreview);
        }

        [Fact]
        public void InterpolatedStringHandlerConversionFlow_16()
        {
            var code = @"
using System;
using System.Runtime.CompilerServices;

public class C
{
    public void M1(C c, CustomHandler custom)
    /*<bind>*/{
        custom = $"""";
    }/*</bind>*/
}
";

            string handlerType = GetInterpolatedStringCustomHandlerType("CustomHandler", "partial struct", useBoolReturns: false, includeTrailingOutConstructorParameter: true);

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
        Statements (1)
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'custom')
              Value:
                IParameterReferenceOperation: custom (OperationKind.ParameterReference, Type: CustomHandler) (Syntax: 'custom')
        Next (Regular) Block[B2]
            Entering: {R2}
    .locals {R2}
    {
        CaptureIds: [2]
        Block[B2] - Block
            Predecessors: [B1]
            Statements (1)
                IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: '$""""')
                  Value:
                    IObjectCreationOperation (Constructor: CustomHandler..ctor(System.Int32 literalLength, System.Int32 formattedCount, out System.Boolean success)) (OperationKind.ObjectCreation, Type: CustomHandler, IsImplicit) (Syntax: '$""""')
                      Arguments(3):
                          IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: literalLength) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: '$""""')
                            ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 0, IsImplicit) (Syntax: '$""""')
                            InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                            OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                          IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: formattedCount) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: '$""""')
                            ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 0, IsImplicit) (Syntax: '$""""')
                            InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                            OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                          IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: success) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: '$""""')
                            IFlowCaptureReferenceOperation: 2 (IsInitialization) (OperationKind.FlowCaptureReference, Type: null, IsImplicit) (Syntax: '$""""')
                            InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                            OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                      Initializer:
                        null
            Jump if False (Regular) to Block[B3]
                IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: null, IsImplicit) (Syntax: '$""""')
                Leaving: {R2}
            Next (Regular) Block[B3]
                Leaving: {R2}
    }
    Block[B3] - Block
        Predecessors: [B2*2]
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'custom = $"""";')
              Expression:
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: CustomHandler) (Syntax: 'custom = $""""')
                  Left:
                    IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: CustomHandler, IsImplicit) (Syntax: 'custom')
                  Right:
                    IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: CustomHandler, IsImplicit) (Syntax: '$""""')
        Next (Regular) Block[B4]
            Leaving: {R1}
}
Block[B4] - Exit
    Predecessors: [B3]
    Statements (0)
";

            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(new[] { code, handlerType }, expectedFlowGraph, expectedDiagnostics, parseOptions: TestOptions.RegularPreview);
        }

        [Fact]
        public void DynamicConstruction_01()
        {
            var code = @"
using System;
using System.Runtime.CompilerServices;
dynamic d = 1;
/*<bind>*/M($""literal{d,1:format}"")/*</bind>*/;

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

    public void AppendFormatted(int d, int alignment, string format)
    {
        Console.WriteLine(""AppendFormatted"");
    }
    public void AppendFormatted(long d, int alignment, string format)
    {
    }
}
";

            var expectedDiagnostics = DiagnosticDescription.None;

            string expectedOperationTree = @"
IInvocationOperation (void M(CustomHandler c)) (OperationKind.Invocation, Type: System.Void) (Syntax: 'M($""literal ... 1:format}"")')
  Instance Receiver:
    null
  Arguments(1):
      IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: c) (OperationKind.Argument, Type: null) (Syntax: '$""literal{d,1:format}""')
        IInterpolatedStringHandlerCreationOperation (HandlerAppendCallsReturnBool: False, HandlerCreationHasSuccessParameter: False) (OperationKind.InterpolatedStringHandlerCreation, Type: CustomHandler, IsImplicit) (Syntax: '$""literal{d,1:format}""')
          Creation:
            IObjectCreationOperation (Constructor: CustomHandler..ctor(System.Int32 literalLength, System.Int32 formattedCount)) (OperationKind.ObjectCreation, Type: CustomHandler, IsImplicit) (Syntax: '$""literal{d,1:format}""')
              Arguments(2):
                  IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: literalLength) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: '$""literal{d,1:format}""')
                    ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 7, IsImplicit) (Syntax: '$""literal{d,1:format}""')
                    InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: formattedCount) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: '$""literal{d,1:format}""')
                    ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1, IsImplicit) (Syntax: '$""literal{d,1:format}""')
                    InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
              Initializer:
                null
          Content:
            IInterpolatedStringOperation (OperationKind.InterpolatedString, Type: System.String) (Syntax: '$""literal{d,1:format}""')
              Parts(2):
                  IInterpolatedStringAppendOperation (OperationKind.InterpolatedStringAppendLiteral, Type: null, IsImplicit) (Syntax: 'literal')
                    AppendCall:
                      IInvocationOperation ( void CustomHandler.AppendLiteral(dynamic d)) (OperationKind.Invocation, Type: System.Void, IsImplicit) (Syntax: 'literal')
                        Instance Receiver:
                          IInstanceReferenceOperation (ReferenceKind: InterpolatedStringHandler) (OperationKind.InstanceReference, Type: CustomHandler, IsImplicit) (Syntax: '$""literal{d,1:format}""')
                        Arguments(1):
                            IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: d) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 'literal')
                              IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: dynamic, IsImplicit) (Syntax: 'literal')
                                Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
                                Operand:
                                  ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: ""literal"") (Syntax: 'literal')
                              InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                              OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  IInterpolatedStringAppendOperation (OperationKind.InterpolatedStringAppendFormatted, Type: null, IsImplicit) (Syntax: '{d,1:format}')
                    AppendCall:
                      IDynamicInvocationOperation (OperationKind.DynamicInvocation, Type: dynamic, IsImplicit) (Syntax: '{d,1:format}')
                        Expression:
                          IDynamicMemberReferenceOperation (Member Name: ""AppendFormatted"", Containing Type: null) (OperationKind.DynamicMemberReference, Type: null, IsImplicit) (Syntax: '{d,1:format}')
                            Type Arguments(0)
                            Instance Receiver:
                              IInstanceReferenceOperation (ReferenceKind: InterpolatedStringHandler) (OperationKind.InstanceReference, Type: CustomHandler, IsImplicit) (Syntax: '$""literal{d,1:format}""')
                        Arguments(3):
                            ILocalReferenceOperation: d (OperationKind.LocalReference, Type: dynamic) (Syntax: 'd')
                            ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
                            ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: ""format"") (Syntax: ':format')
                        ArgumentNames(3):
                          ""null""
                          ""alignment""
                          ""format""
                        ArgumentRefKinds(0)
        InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
";

            VerifyOperationTreeAndDiagnosticsForTest<InvocationExpressionSyntax>(new[] { code, InterpolatedStringHandlerAttribute }, expectedOperationTree, expectedDiagnostics, parseOptions: TestOptions.RegularPreview);
        }

        [Fact]
        public void DynamicConstruction_02()
        {
            var code = @"
using System.Runtime.CompilerServices;
dynamic d = 1;
/*<bind>*/M(d, $""{1}literal"")/*</bind>*/;

void M(dynamic d, [InterpolatedStringHandlerArgument(""d"")]CustomHandler c) {}

public partial struct CustomHandler
{
    public CustomHandler(int literalLength, int formattedCount, int d) : this() {}
    public CustomHandler(int literalLength, int formattedCount, long d) : this() {}
}
";

            var handler = GetInterpolatedStringCustomHandlerType("CustomHandler", "partial struct", useBoolReturns: false);

            var expectedDiagnostics = new[] {
                // (4,16): error CS8953: An interpolated string handler construction cannot use dynamic. Manually construct an instance of 'CustomHandler'.
                // M(d, /*<bind>*/$"{1}literal"/*</bind>*/);
                Diagnostic(ErrorCode.ERR_InterpolatedStringHandlerCreationCannotUseDynamic, @"$""{1}literal""").WithArguments("CustomHandler").WithLocation(4, 16)
            };

            string expectedOperationTree = @"
IInvocationOperation (void M(dynamic d, CustomHandler c)) (OperationKind.Invocation, Type: System.Void, IsInvalid) (Syntax: 'M(d, $""{1}literal"")')
  Instance Receiver:
    null
  Arguments(2):
      IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: d) (OperationKind.Argument, Type: null) (Syntax: 'd')
        ILocalReferenceOperation: d (OperationKind.LocalReference, Type: dynamic) (Syntax: 'd')
        InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
      IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: c) (OperationKind.Argument, Type: null, IsInvalid) (Syntax: '$""{1}literal""')
        IInterpolatedStringHandlerCreationOperation (HandlerAppendCallsReturnBool: False, HandlerCreationHasSuccessParameter: False) (OperationKind.InterpolatedStringHandlerCreation, Type: CustomHandler, IsInvalid, IsImplicit) (Syntax: '$""{1}literal""')
          Creation:
            IDynamicObjectCreationOperation (OperationKind.DynamicObjectCreation, Type: CustomHandler, IsInvalid, IsImplicit) (Syntax: '$""{1}literal""')
              Arguments(3):
                  ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 7, IsInvalid, IsImplicit) (Syntax: '$""{1}literal""')
                  ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1, IsInvalid, IsImplicit) (Syntax: '$""{1}literal""')
                  IInterpolatedStringHandlerArgumentPlaceholderOperation (ArgumentIndex: 0) (OperationKind.InterpolatedStringHandlerArgumentPlaceholder, Type: null, IsImplicit) (Syntax: 'd')
              ArgumentNames(0)
              ArgumentRefKinds(3):
                None
                None
                None
              Initializer:
                null
          Content:
            IInterpolatedStringOperation (OperationKind.InterpolatedString, Type: System.String, IsInvalid) (Syntax: '$""{1}literal""')
              Parts(2):
                  IInterpolatedStringAppendOperation (OperationKind.InterpolatedStringAppendFormatted, Type: null, IsInvalid, IsImplicit) (Syntax: '{1}')
                    AppendCall:
                      IInvocationOperation ( void CustomHandler.AppendFormatted(System.Object o, [System.Int32 alignment = 0], [System.String format = null])) (OperationKind.Invocation, Type: System.Void, IsInvalid, IsImplicit) (Syntax: '{1}')
                        Instance Receiver:
                          IInstanceReferenceOperation (ReferenceKind: InterpolatedStringHandler) (OperationKind.InstanceReference, Type: CustomHandler, IsInvalid, IsImplicit) (Syntax: '$""{1}literal""')
                        Arguments(3):
                            IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: o) (OperationKind.Argument, Type: null, IsInvalid, IsImplicit) (Syntax: '1')
                              IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Object, IsInvalid, IsImplicit) (Syntax: '1')
                                Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                Operand:
                                  ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1, IsInvalid) (Syntax: '1')
                              InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                              OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                            IArgumentOperation (ArgumentKind.DefaultValue, Matching Parameter: alignment) (OperationKind.Argument, Type: null, IsInvalid, IsImplicit) (Syntax: '{1}')
                              ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 0, IsInvalid, IsImplicit) (Syntax: '{1}')
                              InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                              OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                            IArgumentOperation (ArgumentKind.DefaultValue, Matching Parameter: format) (OperationKind.Argument, Type: null, IsInvalid, IsImplicit) (Syntax: '{1}')
                              IDefaultValueOperation (OperationKind.DefaultValue, Type: System.String, Constant: null, IsInvalid, IsImplicit) (Syntax: '{1}')
                              InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                              OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  IInterpolatedStringAppendOperation (OperationKind.InterpolatedStringAppendLiteral, Type: null, IsInvalid, IsImplicit) (Syntax: 'literal')
                    AppendCall:
                      IInvocationOperation ( void CustomHandler.AppendLiteral(System.String literal)) (OperationKind.Invocation, Type: System.Void, IsInvalid, IsImplicit) (Syntax: 'literal')
                        Instance Receiver:
                          IInstanceReferenceOperation (ReferenceKind: InterpolatedStringHandler) (OperationKind.InstanceReference, Type: CustomHandler, IsInvalid, IsImplicit) (Syntax: '$""{1}literal""')
                        Arguments(1):
                            IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: literal) (OperationKind.Argument, Type: null, IsInvalid, IsImplicit) (Syntax: 'literal')
                              ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: ""literal"", IsInvalid) (Syntax: 'literal')
                              InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                              OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
";

            VerifyOperationTreeAndDiagnosticsForTest<InvocationExpressionSyntax>(new[] { code, handler, InterpolatedStringHandlerArgumentAttribute }, expectedOperationTree, expectedDiagnostics, parseOptions: TestOptions.RegularPreview);
        }

        [Fact]
        public void DynamicConstruction_Flow()
        {
            var code = @"
using System.Runtime.CompilerServices;

class C
{
    void M()
    /*<bind>*/{
        dynamic d = 1;
        M1(d, $""{1}literal"");

        void M1(dynamic d, [InterpolatedStringHandlerArgument(""d"")]CustomHandler c) {}
    }/*</bind>*/

}

public partial struct CustomHandler
{
    public CustomHandler(int literalLength, int formattedCount, int d) : this() {}
    public CustomHandler(int literalLength, int formattedCount, long d) : this() {}
}
";

            var handler = GetInterpolatedStringCustomHandlerType("CustomHandler", "partial struct", useBoolReturns: false);

            var expectedDiagnostics = new DiagnosticDescription[] {
                // (9,15): error CS8953: An interpolated string handler construction cannot use dynamic. Manually construct an instance of 'CustomHandler'.
                //         M1(d, $"{1}literal");
                Diagnostic(ErrorCode.ERR_InterpolatedStringHandlerCreationCannotUseDynamic, @"$""{1}literal""").WithArguments("CustomHandler").WithLocation(9, 15)
            };

            string expectedFlowGraph = @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
        Entering: {R1}
.locals {R1}
{
    Locals: [dynamic d]
    Methods: [void M1(dynamic d, CustomHandler c)]
    Block[B1] - Block
        Predecessors: [B0]
        Statements (1)
            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: dynamic, IsImplicit) (Syntax: 'd = 1')
              Left:
                ILocalReferenceOperation: d (IsDeclaration: True) (OperationKind.LocalReference, Type: dynamic, IsImplicit) (Syntax: 'd = 1')
              Right:
                IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: dynamic, IsImplicit) (Syntax: '1')
                  Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    (Boxing)
                  Operand:
                    ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
        Next (Regular) Block[B2]
            Entering: {R2}
    .locals {R2}
    {
        CaptureIds: [0] [1]
        Block[B2] - Block
            Predecessors: [B1]
            Statements (5)
                IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'd')
                  Value:
                    ILocalReferenceOperation: d (OperationKind.LocalReference, Type: dynamic) (Syntax: 'd')
                IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsInvalid, IsImplicit) (Syntax: '$""{1}literal""')
                  Value:
                    IDynamicObjectCreationOperation (OperationKind.DynamicObjectCreation, Type: CustomHandler, IsInvalid, IsImplicit) (Syntax: '$""{1}literal""')
                      Arguments(3):
                          ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 7, IsInvalid, IsImplicit) (Syntax: '$""{1}literal""')
                          ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1, IsInvalid, IsImplicit) (Syntax: '$""{1}literal""')
                          IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: dynamic, IsImplicit) (Syntax: 'd')
                      ArgumentNames(0)
                      ArgumentRefKinds(3):
                        None
                        None
                        None
                      Initializer:
                        null
                IInvocationOperation ( void CustomHandler.AppendFormatted(System.Object o, [System.Int32 alignment = 0], [System.String format = null])) (OperationKind.Invocation, Type: System.Void, IsInvalid, IsImplicit) (Syntax: '{1}')
                  Instance Receiver:
                    IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: CustomHandler, IsInvalid, IsImplicit) (Syntax: '$""{1}literal""')
                  Arguments(3):
                      IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: o) (OperationKind.Argument, Type: null, IsInvalid, IsImplicit) (Syntax: '1')
                        IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Object, IsInvalid, IsImplicit) (Syntax: '1')
                          Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                            (Boxing)
                          Operand:
                            ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1, IsInvalid) (Syntax: '1')
                        InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                      IArgumentOperation (ArgumentKind.DefaultValue, Matching Parameter: alignment) (OperationKind.Argument, Type: null, IsInvalid, IsImplicit) (Syntax: '{1}')
                        ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 0, IsInvalid, IsImplicit) (Syntax: '{1}')
                        InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                      IArgumentOperation (ArgumentKind.DefaultValue, Matching Parameter: format) (OperationKind.Argument, Type: null, IsInvalid, IsImplicit) (Syntax: '{1}')
                        IDefaultValueOperation (OperationKind.DefaultValue, Type: System.String, Constant: null, IsInvalid, IsImplicit) (Syntax: '{1}')
                        InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                IInvocationOperation ( void CustomHandler.AppendLiteral(System.String literal)) (OperationKind.Invocation, Type: System.Void, IsInvalid, IsImplicit) (Syntax: 'literal')
                  Instance Receiver:
                    IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: CustomHandler, IsInvalid, IsImplicit) (Syntax: '$""{1}literal""')
                  Arguments(1):
                      IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: literal) (OperationKind.Argument, Type: null, IsInvalid, IsImplicit) (Syntax: 'literal')
                        ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: ""literal"", IsInvalid) (Syntax: 'literal')
                        InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null, IsInvalid) (Syntax: 'M1(d, $""{1}literal"");')
                  Expression:
                    IInvocationOperation (void M1(dynamic d, CustomHandler c)) (OperationKind.Invocation, Type: System.Void, IsInvalid) (Syntax: 'M1(d, $""{1}literal"")')
                      Instance Receiver:
                        null
                      Arguments(2):
                          IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: d) (OperationKind.Argument, Type: null) (Syntax: 'd')
                            IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: dynamic, IsImplicit) (Syntax: 'd')
                            InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                            OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                          IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: c) (OperationKind.Argument, Type: null, IsInvalid) (Syntax: '$""{1}literal""')
                            IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: CustomHandler, IsInvalid, IsImplicit) (Syntax: '$""{1}literal""')
                            InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                            OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
            Next (Regular) Block[B3]
                Leaving: {R2} {R1}
    }
   
    {   void M1(dynamic d, CustomHandler c)
   
        Block[B0#0R1] - Entry
            Statements (0)
            Next (Regular) Block[B1#0R1]
        Block[B1#0R1] - Exit
            Predecessors: [B0#0R1]
            Statements (0)
    }
}
Block[B3] - Exit
    Predecessors: [B2]
    Statements (0)

";

            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(new[] { code, handler, InterpolatedStringHandlerArgumentAttribute }, expectedFlowGraph, expectedDiagnostics, parseOptions: TestOptions.RegularPreview);
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
        public void InterpolationEscapeConstantValue_WithoutDefaultHandler1()
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

            VerifyOperationTreeAndDiagnosticsForTest<InterpolatedStringExpressionSyntax>(new[] { code }, expectedOperationTree, expectedDiagnostics);
        }

        [Fact, WorkItem(54703, "https://github.com/dotnet/roslyn/issues/54703")]
        public void RawInterpolationEscapeConstantValue_WithoutDefaultHandler1()
        {
            var code = @"
int i = 1;
System.Console.WriteLine(/*<bind>*/$$""""""{{i}}""""""/*</bind>*/);";

            var expectedDiagnostics = DiagnosticDescription.None;

            string expectedOperationTree = @"
IInterpolatedStringOperation (OperationKind.InterpolatedString, Type: System.String) (Syntax: '$$""""""{{i}}""""""')
  Parts(1):
      IInterpolationOperation (OperationKind.Interpolation, Type: null) (Syntax: '{{i}}')
        Expression:
          ILocalReferenceOperation: i (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'i')
        Alignment:
          null
        FormatString:
          null";

            VerifyOperationTreeAndDiagnosticsForTest<InterpolatedStringExpressionSyntax>(new[] { code }, expectedOperationTree, expectedDiagnostics);
        }

        [Fact, WorkItem(54703, "https://github.com/dotnet/roslyn/issues/54703")]
        public void RawInterpolationEscapeConstantValue_WithoutDefaultHandler2()
        {
            var code = @"
System.Console.WriteLine(/*<bind>*/$$$""""""{{i}}""""""/*</bind>*/);";

            var expectedDiagnostics = DiagnosticDescription.None;

            string expectedOperationTree = @"
IInterpolatedStringOperation (OperationKind.InterpolatedString, Type: System.String, Constant: ""{{i}}"") (Syntax: '$$$""""""{{i}}""""""')
  Parts(1):
      IInterpolatedStringTextOperation (OperationKind.InterpolatedStringText, Type: null) (Syntax: '{{i}}')
        Text:
          ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: ""{{i}}"", IsImplicit) (Syntax: '{{i}}')";

            VerifyOperationTreeAndDiagnosticsForTest<InterpolatedStringExpressionSyntax>(new[] { code }, expectedOperationTree, expectedDiagnostics);
        }

        [Fact, WorkItem(54703, "https://github.com/dotnet/roslyn/issues/54703")]
        public void RawInterpolationEscapeConstantValue_WithoutDefaultHandler3()
        {
            var code = @"
System.Console.WriteLine(/*<bind>*/$$$$""""""{{i}}""""""/*</bind>*/);";

            var expectedDiagnostics = DiagnosticDescription.None;

            string expectedOperationTree = @"
IInterpolatedStringOperation (OperationKind.InterpolatedString, Type: System.String, Constant: ""{{i}}"") (Syntax: '$$$$""""""{{i}}""""""')
  Parts(1):
      IInterpolatedStringTextOperation (OperationKind.InterpolatedStringText, Type: null) (Syntax: '{{i}}')
        Text:
          ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: ""{{i}}"", IsImplicit) (Syntax: '{{i}}')";

            VerifyOperationTreeAndDiagnosticsForTest<InterpolatedStringExpressionSyntax>(new[] { code }, expectedOperationTree, expectedDiagnostics);
        }

        [Fact, WorkItem(54703, "https://github.com/dotnet/roslyn/issues/54703")]
        public void RawInterpolationEscapeConstantValue_WithoutDefaultHandler4()
        {
            var code = @"
System.Console.WriteLine(/*<bind>*/$$$$""""""{{{i}}}""""""/*</bind>*/);";

            var expectedDiagnostics = DiagnosticDescription.None;

            string expectedOperationTree = @"
IInterpolatedStringOperation (OperationKind.InterpolatedString, Type: System.String, Constant: ""{{{i}}}"") (Syntax: '$$$$""""""{{{i}}}""""""')
  Parts(1):
      IInterpolatedStringTextOperation (OperationKind.InterpolatedStringText, Type: null) (Syntax: '{{{i}}}')
        Text:
          ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: ""{{{i}}}"", IsImplicit) (Syntax: '{{{i}}}')";

            VerifyOperationTreeAndDiagnosticsForTest<InterpolatedStringExpressionSyntax>(new[] { code }, expectedOperationTree, expectedDiagnostics);
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

        [Fact]
        public void ParenthesizedInterpolatedStringsAdded_CustomHandler()
        {
            var code = @"
int i1 = 1;
int i2 = 2;
int i3 = 3;
int i4 = 4;
int i5 = 5;
int i6 = 6;

CustomHandler /*<bind>*/s = (($""{i1,1:d}"" + $""{i2,2:f}"") + $""{i3,3:g}"") + ($""{i4,4:h}"" + ($""{i5,5:i}"" + $""{i6,6:j}""))/*</bind>*/;
";

            var handler = GetInterpolatedStringCustomHandlerType("CustomHandler", "struct", useBoolReturns: false);

            var expectedDiagnostics = DiagnosticDescription.None;
            var expectedOperationTree = @"
IVariableDeclaratorOperation (Symbol: CustomHandler s) (OperationKind.VariableDeclarator, Type: null) (Syntax: 's = (($""{i1 ... {i6,6:j}""))')
  Initializer:
    IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null) (Syntax: '= (($""{i1,1 ... {i6,6:j}""))')
      IInterpolatedStringHandlerCreationOperation (HandlerAppendCallsReturnBool: False, HandlerCreationHasSuccessParameter: False) (OperationKind.InterpolatedStringHandlerCreation, Type: CustomHandler, IsImplicit) (Syntax: '(($""{i1,1:d ... {i6,6:j}""))')
        Creation:
          IObjectCreationOperation (Constructor: CustomHandler..ctor(System.Int32 literalLength, System.Int32 formattedCount)) (OperationKind.ObjectCreation, Type: CustomHandler, IsImplicit) (Syntax: '(($""{i1,1:d ... {i6,6:j}""))')
            Arguments(2):
                IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: literalLength) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: '(($""{i1,1:d ... {i6,6:j}""))')
                  ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 0, IsImplicit) (Syntax: '(($""{i1,1:d ... {i6,6:j}""))')
                  InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: formattedCount) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: '(($""{i1,1:d ... {i6,6:j}""))')
                  ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 6, IsImplicit) (Syntax: '(($""{i1,1:d ... {i6,6:j}""))')
                  InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
            Initializer:
              null
        Content:
          IInterpolatedStringAdditionOperation (OperationKind.InterpolatedStringAddition, Type: null) (Syntax: '(($""{i1,1:d ... {i6,6:j}""))')
            Left:
              IInterpolatedStringAdditionOperation (OperationKind.InterpolatedStringAddition, Type: null) (Syntax: '($""{i1,1:d} ... $""{i3,3:g}""')
                Left:
                  IInterpolatedStringAdditionOperation (OperationKind.InterpolatedStringAddition, Type: null) (Syntax: '$""{i1,1:d}"" ... $""{i2,2:f}""')
                    Left:
                      IInterpolatedStringOperation (OperationKind.InterpolatedString, Type: System.String) (Syntax: '$""{i1,1:d}""')
                        Parts(1):
                            IInterpolatedStringAppendOperation (OperationKind.InterpolatedStringAppendFormatted, Type: null, IsImplicit) (Syntax: '{i1,1:d}')
                              AppendCall:
                                IInvocationOperation ( void CustomHandler.AppendFormatted(System.Object o, [System.Int32 alignment = 0], [System.String format = null])) (OperationKind.Invocation, Type: System.Void, IsImplicit) (Syntax: '{i1,1:d}')
                                  Instance Receiver:
                                    IInstanceReferenceOperation (ReferenceKind: InterpolatedStringHandler) (OperationKind.InstanceReference, Type: CustomHandler, IsImplicit) (Syntax: '(($""{i1,1:d ... {i6,6:j}""))')
                                  Arguments(3):
                                      IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: o) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 'i1')
                                        IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Object, IsImplicit) (Syntax: 'i1')
                                          Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                          Operand:
                                            ILocalReferenceOperation: i1 (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'i1')
                                        InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                        OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                      IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: alignment) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: '1')
                                        ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
                                        InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                        OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                      IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: format) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: ':d')
                                        ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: ""d"") (Syntax: ':d')
                                        InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                        OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    Right:
                      IInterpolatedStringOperation (OperationKind.InterpolatedString, Type: System.String) (Syntax: '$""{i2,2:f}""')
                        Parts(1):
                            IInterpolatedStringAppendOperation (OperationKind.InterpolatedStringAppendFormatted, Type: null, IsImplicit) (Syntax: '{i2,2:f}')
                              AppendCall:
                                IInvocationOperation ( void CustomHandler.AppendFormatted(System.Object o, [System.Int32 alignment = 0], [System.String format = null])) (OperationKind.Invocation, Type: System.Void, IsImplicit) (Syntax: '{i2,2:f}')
                                  Instance Receiver:
                                    IInstanceReferenceOperation (ReferenceKind: InterpolatedStringHandler) (OperationKind.InstanceReference, Type: CustomHandler, IsImplicit) (Syntax: '(($""{i1,1:d ... {i6,6:j}""))')
                                  Arguments(3):
                                      IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: o) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 'i2')
                                        IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Object, IsImplicit) (Syntax: 'i2')
                                          Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                          Operand:
                                            ILocalReferenceOperation: i2 (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'i2')
                                        InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                        OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                      IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: alignment) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: '2')
                                        ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 2) (Syntax: '2')
                                        InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                        OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                      IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: format) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: ':f')
                                        ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: ""f"") (Syntax: ':f')
                                        InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                        OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                Right:
                  IInterpolatedStringOperation (OperationKind.InterpolatedString, Type: System.String) (Syntax: '$""{i3,3:g}""')
                    Parts(1):
                        IInterpolatedStringAppendOperation (OperationKind.InterpolatedStringAppendFormatted, Type: null, IsImplicit) (Syntax: '{i3,3:g}')
                          AppendCall:
                            IInvocationOperation ( void CustomHandler.AppendFormatted(System.Object o, [System.Int32 alignment = 0], [System.String format = null])) (OperationKind.Invocation, Type: System.Void, IsImplicit) (Syntax: '{i3,3:g}')
                              Instance Receiver:
                                IInstanceReferenceOperation (ReferenceKind: InterpolatedStringHandler) (OperationKind.InstanceReference, Type: CustomHandler, IsImplicit) (Syntax: '(($""{i1,1:d ... {i6,6:j}""))')
                              Arguments(3):
                                  IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: o) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 'i3')
                                    IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Object, IsImplicit) (Syntax: 'i3')
                                      Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                      Operand:
                                        ILocalReferenceOperation: i3 (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'i3')
                                    InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                    OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                  IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: alignment) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: '3')
                                    ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 3) (Syntax: '3')
                                    InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                    OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                  IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: format) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: ':g')
                                    ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: ""g"") (Syntax: ':g')
                                    InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                    OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
            Right:
              IInterpolatedStringAdditionOperation (OperationKind.InterpolatedStringAddition, Type: null) (Syntax: '$""{i4,4:h}"" ... ""{i6,6:j}"")')
                Left:
                  IInterpolatedStringOperation (OperationKind.InterpolatedString, Type: System.String) (Syntax: '$""{i4,4:h}""')
                    Parts(1):
                        IInterpolatedStringAppendOperation (OperationKind.InterpolatedStringAppendFormatted, Type: null, IsImplicit) (Syntax: '{i4,4:h}')
                          AppendCall:
                            IInvocationOperation ( void CustomHandler.AppendFormatted(System.Object o, [System.Int32 alignment = 0], [System.String format = null])) (OperationKind.Invocation, Type: System.Void, IsImplicit) (Syntax: '{i4,4:h}')
                              Instance Receiver:
                                IInstanceReferenceOperation (ReferenceKind: InterpolatedStringHandler) (OperationKind.InstanceReference, Type: CustomHandler, IsImplicit) (Syntax: '(($""{i1,1:d ... {i6,6:j}""))')
                              Arguments(3):
                                  IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: o) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 'i4')
                                    IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Object, IsImplicit) (Syntax: 'i4')
                                      Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                      Operand:
                                        ILocalReferenceOperation: i4 (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'i4')
                                    InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                    OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                  IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: alignment) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: '4')
                                    ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 4) (Syntax: '4')
                                    InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                    OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                  IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: format) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: ':h')
                                    ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: ""h"") (Syntax: ':h')
                                    InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                    OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                Right:
                  IInterpolatedStringAdditionOperation (OperationKind.InterpolatedStringAddition, Type: null) (Syntax: '$""{i5,5:i}"" ... $""{i6,6:j}""')
                    Left:
                      IInterpolatedStringOperation (OperationKind.InterpolatedString, Type: System.String) (Syntax: '$""{i5,5:i}""')
                        Parts(1):
                            IInterpolatedStringAppendOperation (OperationKind.InterpolatedStringAppendFormatted, Type: null, IsImplicit) (Syntax: '{i5,5:i}')
                              AppendCall:
                                IInvocationOperation ( void CustomHandler.AppendFormatted(System.Object o, [System.Int32 alignment = 0], [System.String format = null])) (OperationKind.Invocation, Type: System.Void, IsImplicit) (Syntax: '{i5,5:i}')
                                  Instance Receiver:
                                    IInstanceReferenceOperation (ReferenceKind: InterpolatedStringHandler) (OperationKind.InstanceReference, Type: CustomHandler, IsImplicit) (Syntax: '(($""{i1,1:d ... {i6,6:j}""))')
                                  Arguments(3):
                                      IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: o) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 'i5')
                                        IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Object, IsImplicit) (Syntax: 'i5')
                                          Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                          Operand:
                                            ILocalReferenceOperation: i5 (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'i5')
                                        InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                        OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                      IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: alignment) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: '5')
                                        ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 5) (Syntax: '5')
                                        InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                        OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                      IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: format) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: ':i')
                                        ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: ""i"") (Syntax: ':i')
                                        InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                        OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    Right:
                      IInterpolatedStringOperation (OperationKind.InterpolatedString, Type: System.String) (Syntax: '$""{i6,6:j}""')
                        Parts(1):
                            IInterpolatedStringAppendOperation (OperationKind.InterpolatedStringAppendFormatted, Type: null, IsImplicit) (Syntax: '{i6,6:j}')
                              AppendCall:
                                IInvocationOperation ( void CustomHandler.AppendFormatted(System.Object o, [System.Int32 alignment = 0], [System.String format = null])) (OperationKind.Invocation, Type: System.Void, IsImplicit) (Syntax: '{i6,6:j}')
                                  Instance Receiver:
                                    IInstanceReferenceOperation (ReferenceKind: InterpolatedStringHandler) (OperationKind.InstanceReference, Type: CustomHandler, IsImplicit) (Syntax: '(($""{i1,1:d ... {i6,6:j}""))')
                                  Arguments(3):
                                      IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: o) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 'i6')
                                        IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Object, IsImplicit) (Syntax: 'i6')
                                          Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                          Operand:
                                            ILocalReferenceOperation: i6 (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'i6')
                                        InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                        OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                      IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: alignment) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: '6')
                                        ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 6) (Syntax: '6')
                                        InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                        OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                      IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: format) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: ':j')
                                        ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: ""j"") (Syntax: ':j')
                                        InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                        OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
";

            VerifyOperationTreeAndDiagnosticsForTest<VariableDeclaratorSyntax>(new[] { code, handler }, expectedOperationTree, expectedDiagnostics, parseOptions: TestOptions.RegularPreview);
        }

        [Fact]
        public void ExplicitCastToCustomHandler()
        {
            var code = @"
int i1 = 1;

CustomHandler s = /*<bind>*/(CustomHandler)$""{i1,1:d}""/*</bind>*/;
";

            var handler = GetInterpolatedStringCustomHandlerType("CustomHandler", "struct", useBoolReturns: false);

            var expectedDiagnostics = DiagnosticDescription.None;
            var expectedOperationTree = @"
IInterpolatedStringHandlerCreationOperation (HandlerAppendCallsReturnBool: False, HandlerCreationHasSuccessParameter: False) (OperationKind.InterpolatedStringHandlerCreation, Type: CustomHandler) (Syntax: '(CustomHand ... $""{i1,1:d}""')
  Creation:
    IObjectCreationOperation (Constructor: CustomHandler..ctor(System.Int32 literalLength, System.Int32 formattedCount)) (OperationKind.ObjectCreation, Type: CustomHandler, IsImplicit) (Syntax: '$""{i1,1:d}""')
      Arguments(2):
          IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: literalLength) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: '$""{i1,1:d}""')
            ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 0, IsImplicit) (Syntax: '$""{i1,1:d}""')
            InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
            OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
          IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: formattedCount) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: '$""{i1,1:d}""')
            ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1, IsImplicit) (Syntax: '$""{i1,1:d}""')
            InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
            OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
      Initializer:
        null
  Content:
    IInterpolatedStringOperation (OperationKind.InterpolatedString, Type: System.String) (Syntax: '$""{i1,1:d}""')
      Parts(1):
          IInterpolatedStringAppendOperation (OperationKind.InterpolatedStringAppendFormatted, Type: null, IsImplicit) (Syntax: '{i1,1:d}')
            AppendCall:
              IInvocationOperation ( void CustomHandler.AppendFormatted(System.Object o, [System.Int32 alignment = 0], [System.String format = null])) (OperationKind.Invocation, Type: System.Void, IsImplicit) (Syntax: '{i1,1:d}')
                Instance Receiver:
                  IInstanceReferenceOperation (ReferenceKind: InterpolatedStringHandler) (OperationKind.InstanceReference, Type: CustomHandler, IsImplicit) (Syntax: '$""{i1,1:d}""')
                Arguments(3):
                    IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: o) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 'i1')
                      IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Object, IsImplicit) (Syntax: 'i1')
                        Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        Operand:
                          ILocalReferenceOperation: i1 (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'i1')
                      InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                      OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: alignment) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: '1')
                      ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
                      InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                      OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: format) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: ':d')
                      ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: ""d"") (Syntax: ':d')
                      InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                      OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
";

            VerifyOperationTreeAndDiagnosticsForTest<CastExpressionSyntax>(new[] { code, handler }, expectedOperationTree, expectedDiagnostics, parseOptions: TestOptions.RegularPreview);
        }

        [Fact]
        public void HandlerInAppendFormatCall()
        {
            var code = @"
using System;
using System.Runtime.CompilerServices;


class C
{
    public static void M(int i, [InterpolatedStringHandlerArgument(""i"")]CustomHandler handler)
    {
        Console.WriteLine(handler.ToString());
    }

    public static void M()
    /*<bind>*/{
        C.M(1, $""{$""Inner string""}{2}"");
    }/*</bind>*/
}

public partial class CustomHandler
{
    private int I = 0;

    public CustomHandler(int literalLength, int formattedCount, int i) : this(literalLength, formattedCount)
    {
        Console.WriteLine(""int constructor"");
        I = i;
    }

    public CustomHandler(int literalLength, int formattedCount, CustomHandler c) : this(literalLength, formattedCount)
    {
        Console.WriteLine(""CustomHandler constructor"");
        _builder.AppendLine(""c.I:"" + c.I.ToString());
    }

    public void AppendFormatted([InterpolatedStringHandlerArgument("""")]CustomHandler c)
    {
        _builder.AppendLine(""CustomHandler AppendFormatted"");
        _builder.Append(c.ToString());
    }
}
";
            var handler = GetInterpolatedStringCustomHandlerType("CustomHandler", "partial class", useBoolReturns: false);

            var expectedDiagnostics = DiagnosticDescription.None;

            var expectedFlowGraph = @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
        Entering: {R1}
.locals {R1}
{
    CaptureIds: [0] [1]
    Block[B1] - Block
        Predecessors: [B0]
        Statements (2)
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: '1')
              Value:
                ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
            IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: '$""{$""Inner string""}{2}""')
              Value:
                IObjectCreationOperation (Constructor: CustomHandler..ctor(System.Int32 literalLength, System.Int32 formattedCount, System.Int32 i)) (OperationKind.ObjectCreation, Type: CustomHandler, IsImplicit) (Syntax: '$""{$""Inner string""}{2}""')
                  Arguments(3):
                      IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: literalLength) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: '$""{$""Inner string""}{2}""')
                        ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 0, IsImplicit) (Syntax: '$""{$""Inner string""}{2}""')
                        InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                      IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: formattedCount) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: '$""{$""Inner string""}{2}""')
                        ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 2, IsImplicit) (Syntax: '$""{$""Inner string""}{2}""')
                        InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                      IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: i) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: '1')
                        IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Int32, Constant: 1, IsImplicit) (Syntax: '1')
                        InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  Initializer:
                    null
        Next (Regular) Block[B2]
            Entering: {R2}
    .locals {R2}
    {
        CaptureIds: [2]
        Block[B2] - Block
            Predecessors: [B1]
            Statements (3)
                IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: '$""Inner string""')
                  Value:
                    IObjectCreationOperation (Constructor: CustomHandler..ctor(System.Int32 literalLength, System.Int32 formattedCount, CustomHandler c)) (OperationKind.ObjectCreation, Type: CustomHandler, IsImplicit) (Syntax: '$""Inner string""')
                      Arguments(3):
                          IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: literalLength) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: '$""Inner string""')
                            ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 12, IsImplicit) (Syntax: '$""Inner string""')
                            InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                            OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                          IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: formattedCount) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: '$""Inner string""')
                            ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 0, IsImplicit) (Syntax: '$""Inner string""')
                            InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                            OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                          IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: c) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: '$""{$""Inner string""}{2}""')
                            IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: CustomHandler, IsImplicit) (Syntax: '$""{$""Inner string""}{2}""')
                            InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                            OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                      Initializer:
                        null
                IInvocationOperation ( void CustomHandler.AppendLiteral(System.String literal)) (OperationKind.Invocation, Type: System.Void, IsImplicit) (Syntax: 'Inner string')
                  Instance Receiver:
                    IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: CustomHandler, IsImplicit) (Syntax: '$""Inner string""')
                  Arguments(1):
                      IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: literal) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 'Inner string')
                        ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: ""Inner string"") (Syntax: 'Inner string')
                        InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                IInvocationOperation ( void CustomHandler.AppendFormatted(CustomHandler c)) (OperationKind.Invocation, Type: System.Void, IsImplicit) (Syntax: '{$""Inner string""}')
                  Instance Receiver:
                    IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: CustomHandler, IsImplicit) (Syntax: '$""{$""Inner string""}{2}""')
                  Arguments(1):
                      IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: c) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: '$""Inner string""')
                        IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: CustomHandler, IsImplicit) (Syntax: '$""Inner string""')
                        InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
            Next (Regular) Block[B3]
                Leaving: {R2}
    }
    Block[B3] - Block
        Predecessors: [B2]
        Statements (2)
            IInvocationOperation ( void CustomHandler.AppendFormatted(System.Object o, [System.Int32 alignment = 0], [System.String format = null])) (OperationKind.Invocation, Type: System.Void, IsImplicit) (Syntax: '{2}')
              Instance Receiver:
                IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: CustomHandler, IsImplicit) (Syntax: '$""{$""Inner string""}{2}""')
              Arguments(3):
                  IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: o) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: '2')
                    IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Object, IsImplicit) (Syntax: '2')
                      Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        (Boxing)
                      Operand:
                        ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 2) (Syntax: '2')
                    InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  IArgumentOperation (ArgumentKind.DefaultValue, Matching Parameter: alignment) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: '{2}')
                    ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 0, IsImplicit) (Syntax: '{2}')
                    InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  IArgumentOperation (ArgumentKind.DefaultValue, Matching Parameter: format) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: '{2}')
                    IDefaultValueOperation (OperationKind.DefaultValue, Type: System.String, Constant: null, IsImplicit) (Syntax: '{2}')
                    InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'C.M(1, $""{$ ... ing""}{2}"");')
              Expression:
                IInvocationOperation (void C.M(System.Int32 i, CustomHandler handler)) (OperationKind.Invocation, Type: System.Void) (Syntax: 'C.M(1, $""{$ ... ring""}{2}"")')
                  Instance Receiver:
                    null
                  Arguments(2):
                      IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: i) (OperationKind.Argument, Type: null) (Syntax: '1')
                        IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Int32, Constant: 1, IsImplicit) (Syntax: '1')
                        InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                      IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: handler) (OperationKind.Argument, Type: null) (Syntax: '$""{$""Inner string""}{2}""')
                        IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: CustomHandler, IsImplicit) (Syntax: '$""{$""Inner string""}{2}""')
                        InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        Next (Regular) Block[B4]
            Leaving: {R1}
}
Block[B4] - Exit
    Predecessors: [B3]
    Statements (0)
";

            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(new[] { code, handler, InterpolatedStringHandlerArgumentAttribute }, expectedFlowGraph, expectedDiagnostics);
        }

        [Fact]
        public void InvalidConstructor()
        {
            var code = @"
using System.Runtime.CompilerServices;

public class C
{
    public static void M1(int i)
    /*<bind>*/{
        C.M(in i, $""literal{1}"");
    }/*</bind>*/

    public static void M(in int i, [InterpolatedStringHandlerArgumentAttribute(""i"")] CustomHandler c) { }
}

public partial struct CustomHandler
{
    public CustomHandler(int literalLength, int formattedCount, ref int i) : this() { }
}
";

            var handler = GetInterpolatedStringCustomHandlerType("CustomHandler", "partial struct", useBoolReturns: false);

            var expectedDiagnostics = new[] {
                // (8,16): error CS1620: Argument 3 must be passed with the 'ref' keyword
                //         C.M(in i, $"literal{1}");
                Diagnostic(ErrorCode.ERR_BadArgRef, "i").WithArguments("3", "ref").WithLocation(8, 16)
            };

            var expectedFlowGraph = @"
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
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsInvalid, IsImplicit) (Syntax: 'i')
              Value:
                IParameterReferenceOperation: i (OperationKind.ParameterReference, Type: System.Int32, IsInvalid) (Syntax: 'i')
            IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: '$""literal{1}""')
              Value:
                IInvalidOperation (OperationKind.Invalid, Type: CustomHandler, IsImplicit) (Syntax: '$""literal{1}""')
                  Children(3):
                      ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 7, IsImplicit) (Syntax: '$""literal{1}""')
                      ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1, IsImplicit) (Syntax: '$""literal{1}""')
                      IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Int32, IsInvalid, IsImplicit) (Syntax: 'i')
            IInvocationOperation ( void CustomHandler.AppendLiteral(System.String literal)) (OperationKind.Invocation, Type: System.Void, IsImplicit) (Syntax: 'literal')
              Instance Receiver:
                IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: CustomHandler, IsImplicit) (Syntax: '$""literal{1}""')
              Arguments(1):
                  IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: literal) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 'literal')
                    ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: ""literal"") (Syntax: 'literal')
                    InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
            IInvocationOperation ( void CustomHandler.AppendFormatted(System.Object o, [System.Int32 alignment = 0], [System.String format = null])) (OperationKind.Invocation, Type: System.Void, IsImplicit) (Syntax: '{1}')
              Instance Receiver:
                IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: CustomHandler, IsImplicit) (Syntax: '$""literal{1}""')
              Arguments(3):
                  IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: o) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: '1')
                    IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Object, IsImplicit) (Syntax: '1')
                      Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        (Boxing)
                      Operand:
                        ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
                    InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  IArgumentOperation (ArgumentKind.DefaultValue, Matching Parameter: alignment) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: '{1}')
                    ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 0, IsImplicit) (Syntax: '{1}')
                    InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  IArgumentOperation (ArgumentKind.DefaultValue, Matching Parameter: format) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: '{1}')
                    IDefaultValueOperation (OperationKind.DefaultValue, Type: System.String, Constant: null, IsImplicit) (Syntax: '{1}')
                    InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null, IsInvalid) (Syntax: 'C.M(in i, $ ... teral{1}"");')
              Expression:
                IInvocationOperation (void C.M(in System.Int32 i, CustomHandler c)) (OperationKind.Invocation, Type: System.Void, IsInvalid) (Syntax: 'C.M(in i, $""literal{1}"")')
                  Instance Receiver:
                    null
                  Arguments(2):
                      IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: i) (OperationKind.Argument, Type: null, IsInvalid) (Syntax: 'in i')
                        IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Int32, IsInvalid, IsImplicit) (Syntax: 'i')
                        InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                      IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: c) (OperationKind.Argument, Type: null) (Syntax: '$""literal{1}""')
                        IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: CustomHandler, IsImplicit) (Syntax: '$""literal{1}""')
                        InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        Next (Regular) Block[B2]
            Leaving: {R1}
}
Block[B2] - Exit
    Predecessors: [B1]
    Statements (0)
";

            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(new[] { code, handler, InterpolatedStringHandlerArgumentAttribute }, expectedFlowGraph, expectedDiagnostics, parseOptions: TestOptions.RegularPreview);
        }

        [Fact]
        public void HandlerConstructorWithDefaultArgument()
        {
            var code = @"
using System;
using System.Runtime.CompilerServices;


class C
{
    public static void M1()
    /*<bind>*/{
        C.M($""Literal"");
    }/*</bind>*/
    public static void M(CustomHandler c) => Console.WriteLine(c.ToString());
}

[InterpolatedStringHandler]
partial struct CustomHandler
{
    private string _s = null;
    public CustomHandler(int literalLength, int formattedCount, out bool isValid, int i = 1) { _s = i.ToString(); isValid = false; }
    public void AppendLiteral(string s) => _s += s;
    public override string ToString() => _s;
}
";

            var expectedDiagnostics = DiagnosticDescription.None;
            var expectedFlowGraph = @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
        Entering: {R1} {R2}
.locals {R1}
{
    CaptureIds: [0]
    .locals {R2}
    {
        CaptureIds: [1]
        Block[B1] - Block
            Predecessors: [B0]
            Statements (1)
                IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: '$""Literal""')
                  Value:
                    IObjectCreationOperation (Constructor: CustomHandler..ctor(System.Int32 literalLength, System.Int32 formattedCount, out System.Boolean isValid, [System.Int32 i = 1])) (OperationKind.ObjectCreation, Type: CustomHandler, IsImplicit) (Syntax: '$""Literal""')
                      Arguments(4):
                          IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: literalLength) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: '$""Literal""')
                            ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 7, IsImplicit) (Syntax: '$""Literal""')
                            InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                            OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                          IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: formattedCount) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: '$""Literal""')
                            ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 0, IsImplicit) (Syntax: '$""Literal""')
                            InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                            OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                          IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: isValid) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: '$""Literal""')
                            IFlowCaptureReferenceOperation: 1 (IsInitialization) (OperationKind.FlowCaptureReference, Type: null, IsImplicit) (Syntax: '$""Literal""')
                            InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                            OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                          IArgumentOperation (ArgumentKind.DefaultValue, Matching Parameter: i) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: '$""Literal""')
                            ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1, IsImplicit) (Syntax: '$""Literal""')
                            InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                            OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                      Initializer:
                        null
            Jump if False (Regular) to Block[B3]
                IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: null, IsImplicit) (Syntax: '$""Literal""')
                Leaving: {R2}
            Next (Regular) Block[B2]
                Leaving: {R2}
    }
    Block[B2] - Block
        Predecessors: [B1]
        Statements (1)
            IInvocationOperation ( void CustomHandler.AppendLiteral(System.String s)) (OperationKind.Invocation, Type: System.Void, IsImplicit) (Syntax: 'Literal')
              Instance Receiver:
                IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: CustomHandler, IsImplicit) (Syntax: '$""Literal""')
              Arguments(1):
                  IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: s) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 'Literal')
                    ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: ""Literal"") (Syntax: 'Literal')
                    InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        Next (Regular) Block[B3]
    Block[B3] - Block
        Predecessors: [B1] [B2]
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'C.M($""Literal"");')
              Expression:
                IInvocationOperation (void C.M(CustomHandler c)) (OperationKind.Invocation, Type: System.Void) (Syntax: 'C.M($""Literal"")')
                  Instance Receiver:
                    null
                  Arguments(1):
                      IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: c) (OperationKind.Argument, Type: null) (Syntax: '$""Literal""')
                        IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: CustomHandler, IsImplicit) (Syntax: '$""Literal""')
                        InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        Next (Regular) Block[B4]
            Leaving: {R1}
}
Block[B4] - Exit
    Predecessors: [B3]
    Statements (0)
";

            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(new[] { code, InterpolatedStringHandlerArgumentAttribute, InterpolatedStringHandlerAttribute }, expectedFlowGraph, expectedDiagnostics, parseOptions: TestOptions.RegularPreview);
        }

        [Fact]
        public void InterpolatedStringHandlerArgumentsAttribute_ConversionFromArgumentType()
        {
            var code = @"
using System;
using System.Globalization;
using System.Runtime.CompilerServices;

public class C
{
    public static void M1(int i)
    /*<bind>*/{
        C.M(i, $""literal{1}"");
    }/*</bind>*/

    public static implicit operator C(int i) => throw null;
    public static implicit operator C(double d)
    {
        Console.WriteLine(d.ToString(""G"", CultureInfo.InvariantCulture));
        return new C();
    }
    public override string ToString() => ""C"";

    public static void M(double d, [InterpolatedStringHandlerArgument(""d"")] CustomHandler handler) => Console.WriteLine(handler.ToString());
}

public partial struct CustomHandler
{
    public CustomHandler(int literalLength, int formattedCount, C c) : this(literalLength, formattedCount) { }
}
";

            var handler = GetInterpolatedStringCustomHandlerType("CustomHandler", "partial struct", useBoolReturns: true);

            var expectedDiagnostics = DiagnosticDescription.None;
            var expectedFlowGraph = @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
        Entering: {R1}
.locals {R1}
{
    CaptureIds: [0] [1]
    Block[B1] - Block
        Predecessors: [B0]
        Statements (2)
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'i')
              Value:
                IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Double, IsImplicit) (Syntax: 'i')
                  Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: True, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    (ImplicitNumeric)
                  Operand:
                    IParameterReferenceOperation: i (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'i')
            IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: '$""literal{1}""')
              Value:
                IObjectCreationOperation (Constructor: CustomHandler..ctor(System.Int32 literalLength, System.Int32 formattedCount, C c)) (OperationKind.ObjectCreation, Type: CustomHandler, IsImplicit) (Syntax: '$""literal{1}""')
                  Arguments(3):
                      IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: literalLength) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: '$""literal{1}""')
                        ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 7, IsImplicit) (Syntax: '$""literal{1}""')
                        InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                      IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: formattedCount) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: '$""literal{1}""')
                        ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1, IsImplicit) (Syntax: '$""literal{1}""')
                        InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                      IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: c) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 'i')
                        IConversionOperation (TryCast: False, Unchecked) (OperatorMethod: C C.op_Implicit(System.Double d)) (OperationKind.Conversion, Type: C, IsImplicit) (Syntax: 'i')
                          Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: True) (MethodSymbol: C C.op_Implicit(System.Double d))
                            (ImplicitUserDefined)
                          Operand:
                            IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Double, IsImplicit) (Syntax: 'i')
                        InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  Initializer:
                    null
        Jump if False (Regular) to Block[B3]
            IInvocationOperation ( System.Boolean CustomHandler.AppendLiteral(System.String literal)) (OperationKind.Invocation, Type: System.Boolean, IsImplicit) (Syntax: 'literal')
              Instance Receiver:
                IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: CustomHandler, IsImplicit) (Syntax: '$""literal{1}""')
              Arguments(1):
                  IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: literal) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 'literal')
                    ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: ""literal"") (Syntax: 'literal')
                    InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        Next (Regular) Block[B2]
    Block[B2] - Block
        Predecessors: [B1]
        Statements (1)
            IInvocationOperation ( System.Boolean CustomHandler.AppendFormatted(System.Object o, [System.Int32 alignment = 0], [System.String format = null])) (OperationKind.Invocation, Type: System.Boolean, IsImplicit) (Syntax: '{1}')
              Instance Receiver:
                IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: CustomHandler, IsImplicit) (Syntax: '$""literal{1}""')
              Arguments(3):
                  IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: o) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: '1')
                    IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Object, IsImplicit) (Syntax: '1')
                      Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        (Boxing)
                      Operand:
                        ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
                    InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  IArgumentOperation (ArgumentKind.DefaultValue, Matching Parameter: alignment) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: '{1}')
                    ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 0, IsImplicit) (Syntax: '{1}')
                    InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  IArgumentOperation (ArgumentKind.DefaultValue, Matching Parameter: format) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: '{1}')
                    IDefaultValueOperation (OperationKind.DefaultValue, Type: System.String, Constant: null, IsImplicit) (Syntax: '{1}')
                    InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        Next (Regular) Block[B3]
    Block[B3] - Block
        Predecessors: [B1] [B2]
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'C.M(i, $""literal{1}"");')
              Expression:
                IInvocationOperation (void C.M(System.Double d, CustomHandler handler)) (OperationKind.Invocation, Type: System.Void) (Syntax: 'C.M(i, $""literal{1}"")')
                  Instance Receiver:
                    null
                  Arguments(2):
                      IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: d) (OperationKind.Argument, Type: null) (Syntax: 'i')
                        IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Double, IsImplicit) (Syntax: 'i')
                        InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                      IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: handler) (OperationKind.Argument, Type: null) (Syntax: '$""literal{1}""')
                        IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: CustomHandler, IsImplicit) (Syntax: '$""literal{1}""')
                        InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        Next (Regular) Block[B4]
            Leaving: {R1}
}
Block[B4] - Exit
    Predecessors: [B3]
    Statements (0)
";

            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(new[] { code, InterpolatedStringHandlerArgumentAttribute, handler }, expectedFlowGraph, expectedDiagnostics, parseOptions: TestOptions.RegularPreview);
        }

        [Fact]
        public void DiscardsUsedAsParameters()
        {
            var code = @"
using System;
using System.Runtime.CompilerServices;

public class C
{
    public static void M1()
    /*<bind>*/{
        C.M(out _, $""literal{1}"");
    }/*</bind>*/

    public static void M(out int i, [InterpolatedStringHandlerArgument(""i"")]CustomHandler c) 
    {
        i = 0;
    }
}

public partial struct CustomHandler
{
    public CustomHandler(int literalLength, int formattedCount, out int i) : this(literalLength, formattedCount)
    {
        i = 1;
    }
}
";

            var handler = GetInterpolatedStringCustomHandlerType("CustomHandler", "partial struct", useBoolReturns: false);

            var expectedDiagnostics = DiagnosticDescription.None;
            var expectedFlowGraph = @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
        Entering: {R1}
.locals {R1}
{
    CaptureIds: [0]
    Block[B1] - Block
        Predecessors: [B0]
        Statements (4)
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: '$""literal{1}""')
              Value:
                IObjectCreationOperation (Constructor: CustomHandler..ctor(System.Int32 literalLength, System.Int32 formattedCount, out System.Int32 i)) (OperationKind.ObjectCreation, Type: CustomHandler, IsImplicit) (Syntax: '$""literal{1}""')
                  Arguments(3):
                      IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: literalLength) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: '$""literal{1}""')
                        ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 7, IsImplicit) (Syntax: '$""literal{1}""')
                        InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                      IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: formattedCount) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: '$""literal{1}""')
                        ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1, IsImplicit) (Syntax: '$""literal{1}""')
                        InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                      IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: i) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 'out _')
                        IDiscardOperation (Symbol: System.Int32 _) (OperationKind.Discard, Type: System.Int32) (Syntax: '_')
                        InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  Initializer:
                    null
            IInvocationOperation ( void CustomHandler.AppendLiteral(System.String literal)) (OperationKind.Invocation, Type: System.Void, IsImplicit) (Syntax: 'literal')
              Instance Receiver:
                IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: CustomHandler, IsImplicit) (Syntax: '$""literal{1}""')
              Arguments(1):
                  IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: literal) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 'literal')
                    ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: ""literal"") (Syntax: 'literal')
                    InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
            IInvocationOperation ( void CustomHandler.AppendFormatted(System.Object o, [System.Int32 alignment = 0], [System.String format = null])) (OperationKind.Invocation, Type: System.Void, IsImplicit) (Syntax: '{1}')
              Instance Receiver:
                IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: CustomHandler, IsImplicit) (Syntax: '$""literal{1}""')
              Arguments(3):
                  IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: o) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: '1')
                    IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Object, IsImplicit) (Syntax: '1')
                      Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        (Boxing)
                      Operand:
                        ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
                    InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  IArgumentOperation (ArgumentKind.DefaultValue, Matching Parameter: alignment) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: '{1}')
                    ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 0, IsImplicit) (Syntax: '{1}')
                    InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  IArgumentOperation (ArgumentKind.DefaultValue, Matching Parameter: format) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: '{1}')
                    IDefaultValueOperation (OperationKind.DefaultValue, Type: System.String, Constant: null, IsImplicit) (Syntax: '{1}')
                    InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'C.M(out _,  ... teral{1}"");')
              Expression:
                IInvocationOperation (void C.M(out System.Int32 i, CustomHandler c)) (OperationKind.Invocation, Type: System.Void) (Syntax: 'C.M(out _,  ... iteral{1}"")')
                  Instance Receiver:
                    null
                  Arguments(2):
                      IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: i) (OperationKind.Argument, Type: null) (Syntax: 'out _')
                        IDiscardOperation (Symbol: System.Int32 _) (OperationKind.Discard, Type: System.Int32) (Syntax: '_')
                        InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                      IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: c) (OperationKind.Argument, Type: null) (Syntax: '$""literal{1}""')
                        IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: CustomHandler, IsImplicit) (Syntax: '$""literal{1}""')
                        InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        Next (Regular) Block[B2]
            Leaving: {R1}
}
Block[B2] - Exit
    Predecessors: [B1]
    Statements (0)
";
            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(new[] { code, InterpolatedStringHandlerArgumentAttribute, handler }, expectedFlowGraph, expectedDiagnostics);
        }

        [Fact]
        public void OutVariableDeclarationAsParameter_01()
        {
            var code = @"
using System;
using System.Runtime.CompilerServices;

public class C
{
    public static void M1()
    /*<bind>*/{
        C.M(out int i, $""literal{1}"");
    }/*</bind>*/

    public static void M(out int i, [InterpolatedStringHandlerArgument(""i"")]CustomHandler c) 
    {
        i = 0;
    }
}

public partial struct CustomHandler
{
    public CustomHandler(int literalLength, int formattedCount, out int i) : this(literalLength, formattedCount)
    {
        i = 1;
    }
}
";

            var handler = GetInterpolatedStringCustomHandlerType("CustomHandler", "partial struct", useBoolReturns: false);

            var expectedDiagnostics = DiagnosticDescription.None;
            var expectedFlowGraph = @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
        Entering: {R1}
.locals {R1}
{
    Locals: [System.Int32 i]
    CaptureIds: [0] [1]
    Block[B1] - Block
        Predecessors: [B0]
        Statements (5)
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'i')
              Value:
                ILocalReferenceOperation: i (IsDeclaration: True) (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'i')
            IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: '$""literal{1}""')
              Value:
                IObjectCreationOperation (Constructor: CustomHandler..ctor(System.Int32 literalLength, System.Int32 formattedCount, out System.Int32 i)) (OperationKind.ObjectCreation, Type: CustomHandler, IsImplicit) (Syntax: '$""literal{1}""')
                  Arguments(3):
                      IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: literalLength) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: '$""literal{1}""')
                        ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 7, IsImplicit) (Syntax: '$""literal{1}""')
                        InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                      IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: formattedCount) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: '$""literal{1}""')
                        ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1, IsImplicit) (Syntax: '$""literal{1}""')
                        InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                      IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: i) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 'out int i')
                        IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'i')
                        InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  Initializer:
                    null
            IInvocationOperation ( void CustomHandler.AppendLiteral(System.String literal)) (OperationKind.Invocation, Type: System.Void, IsImplicit) (Syntax: 'literal')
              Instance Receiver:
                IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: CustomHandler, IsImplicit) (Syntax: '$""literal{1}""')
              Arguments(1):
                  IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: literal) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 'literal')
                    ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: ""literal"") (Syntax: 'literal')
                    InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
            IInvocationOperation ( void CustomHandler.AppendFormatted(System.Object o, [System.Int32 alignment = 0], [System.String format = null])) (OperationKind.Invocation, Type: System.Void, IsImplicit) (Syntax: '{1}')
              Instance Receiver:
                IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: CustomHandler, IsImplicit) (Syntax: '$""literal{1}""')
              Arguments(3):
                  IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: o) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: '1')
                    IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Object, IsImplicit) (Syntax: '1')
                      Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        (Boxing)
                      Operand:
                        ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
                    InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  IArgumentOperation (ArgumentKind.DefaultValue, Matching Parameter: alignment) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: '{1}')
                    ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 0, IsImplicit) (Syntax: '{1}')
                    InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  IArgumentOperation (ArgumentKind.DefaultValue, Matching Parameter: format) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: '{1}')
                    IDefaultValueOperation (OperationKind.DefaultValue, Type: System.String, Constant: null, IsImplicit) (Syntax: '{1}')
                    InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'C.M(out int ... teral{1}"");')
              Expression:
                IInvocationOperation (void C.M(out System.Int32 i, CustomHandler c)) (OperationKind.Invocation, Type: System.Void) (Syntax: 'C.M(out int ... iteral{1}"")')
                  Instance Receiver:
                    null
                  Arguments(2):
                      IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: i) (OperationKind.Argument, Type: null) (Syntax: 'out int i')
                        IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'i')
                        InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                      IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: c) (OperationKind.Argument, Type: null) (Syntax: '$""literal{1}""')
                        IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: CustomHandler, IsImplicit) (Syntax: '$""literal{1}""')
                        InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        Next (Regular) Block[B2]
            Leaving: {R1}
}
Block[B2] - Exit
    Predecessors: [B1]
    Statements (0)
";
            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(new[] { code, InterpolatedStringHandlerArgumentAttribute, handler }, expectedFlowGraph, expectedDiagnostics);
        }

        [Fact]
        public void OutVariableDeclarationAsParameter_02()
        {
            var code = @"
using System;
using System.Runtime.CompilerServices;

public class C
{
    public static void M1(object o1, object o2)
    /*<bind>*/{
        C.M(out int i, o1 ?? o2, $""literal{1}"");
    }/*</bind>*/

    public static void M(out int i, object o, [InterpolatedStringHandlerArgument(""i"")]CustomHandler c) 
    {
        i = 0;
    }
}

public partial struct CustomHandler
{
    public CustomHandler(int literalLength, int formattedCount, out int i) : this(literalLength, formattedCount)
    {
        i = 1;
    }
}
";

            var handler = GetInterpolatedStringCustomHandlerType("CustomHandler", "partial struct", useBoolReturns: false);

            var expectedDiagnostics = DiagnosticDescription.None;
            var expectedFlowGraph = @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
        Entering: {R1}
.locals {R1}
{
    Locals: [System.Int32 i]
    CaptureIds: [0] [2] [3]
    Block[B1] - Block
        Predecessors: [B0]
        Statements (1)
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'i')
              Value:
                ILocalReferenceOperation: i (IsDeclaration: True) (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'i')
        Next (Regular) Block[B2]
            Entering: {R2}
    .locals {R2}
    {
        CaptureIds: [1]
        Block[B2] - Block
            Predecessors: [B1]
            Statements (1)
                IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'o1')
                  Value:
                    IParameterReferenceOperation: o1 (OperationKind.ParameterReference, Type: System.Object) (Syntax: 'o1')
            Jump if True (Regular) to Block[B4]
                IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsImplicit) (Syntax: 'o1')
                  Operand:
                    IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.Object, IsImplicit) (Syntax: 'o1')
                Leaving: {R2}
            Next (Regular) Block[B3]
        Block[B3] - Block
            Predecessors: [B2]
            Statements (1)
                IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'o1')
                  Value:
                    IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.Object, IsImplicit) (Syntax: 'o1')
            Next (Regular) Block[B5]
                Leaving: {R2}
    }
    Block[B4] - Block
        Predecessors: [B2]
        Statements (1)
            IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'o2')
              Value:
                IParameterReferenceOperation: o2 (OperationKind.ParameterReference, Type: System.Object) (Syntax: 'o2')
        Next (Regular) Block[B5]
    Block[B5] - Block
        Predecessors: [B3] [B4]
        Statements (4)
            IFlowCaptureOperation: 3 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: '$""literal{1}""')
              Value:
                IObjectCreationOperation (Constructor: CustomHandler..ctor(System.Int32 literalLength, System.Int32 formattedCount, out System.Int32 i)) (OperationKind.ObjectCreation, Type: CustomHandler, IsImplicit) (Syntax: '$""literal{1}""')
                  Arguments(3):
                      IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: literalLength) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: '$""literal{1}""')
                        ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 7, IsImplicit) (Syntax: '$""literal{1}""')
                        InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                      IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: formattedCount) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: '$""literal{1}""')
                        ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1, IsImplicit) (Syntax: '$""literal{1}""')
                        InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                      IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: i) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 'out int i')
                        IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'i')
                        InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  Initializer:
                    null
            IInvocationOperation ( void CustomHandler.AppendLiteral(System.String literal)) (OperationKind.Invocation, Type: System.Void, IsImplicit) (Syntax: 'literal')
              Instance Receiver:
                IFlowCaptureReferenceOperation: 3 (OperationKind.FlowCaptureReference, Type: CustomHandler, IsImplicit) (Syntax: '$""literal{1}""')
              Arguments(1):
                  IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: literal) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 'literal')
                    ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: ""literal"") (Syntax: 'literal')
                    InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
            IInvocationOperation ( void CustomHandler.AppendFormatted(System.Object o, [System.Int32 alignment = 0], [System.String format = null])) (OperationKind.Invocation, Type: System.Void, IsImplicit) (Syntax: '{1}')
              Instance Receiver:
                IFlowCaptureReferenceOperation: 3 (OperationKind.FlowCaptureReference, Type: CustomHandler, IsImplicit) (Syntax: '$""literal{1}""')
              Arguments(3):
                  IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: o) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: '1')
                    IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Object, IsImplicit) (Syntax: '1')
                      Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        (Boxing)
                      Operand:
                        ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
                    InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  IArgumentOperation (ArgumentKind.DefaultValue, Matching Parameter: alignment) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: '{1}')
                    ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 0, IsImplicit) (Syntax: '{1}')
                    InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  IArgumentOperation (ArgumentKind.DefaultValue, Matching Parameter: format) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: '{1}')
                    IDefaultValueOperation (OperationKind.DefaultValue, Type: System.String, Constant: null, IsImplicit) (Syntax: '{1}')
                    InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'C.M(out int ... teral{1}"");')
              Expression:
                IInvocationOperation (void C.M(out System.Int32 i, System.Object o, CustomHandler c)) (OperationKind.Invocation, Type: System.Void) (Syntax: 'C.M(out int ... iteral{1}"")')
                  Instance Receiver:
                    null
                  Arguments(3):
                      IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: i) (OperationKind.Argument, Type: null) (Syntax: 'out int i')
                        IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'i')
                        InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                      IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: o) (OperationKind.Argument, Type: null) (Syntax: 'o1 ?? o2')
                        IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: System.Object, IsImplicit) (Syntax: 'o1 ?? o2')
                        InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                      IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: c) (OperationKind.Argument, Type: null) (Syntax: '$""literal{1}""')
                        IFlowCaptureReferenceOperation: 3 (OperationKind.FlowCaptureReference, Type: CustomHandler, IsImplicit) (Syntax: '$""literal{1}""')
                        InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        Next (Regular) Block[B6]
            Leaving: {R1}
}
Block[B6] - Exit
    Predecessors: [B5]
    Statements (0)
";
            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(new[] { code, InterpolatedStringHandlerArgumentAttribute, handler }, expectedFlowGraph, expectedDiagnostics);
        }

        [Fact]
        public void OutVariableDeclarationAsParameter_03()
        {
            var code = @"
using System;
using System.Runtime.CompilerServices;

public class C
{
    public static void M1()
    /*<bind>*/{
        C.M($"""", out int i, $""literal{1}"");
    }/*</bind>*/

    public static void M(CustomHandler c1, out int i, [InterpolatedStringHandlerArgument(""i"")]CustomHandler c2) 
    {
        i = 0;
    }
}

public partial struct CustomHandler
{
    public CustomHandler(int literalLength, int formattedCount, out int i) : this(literalLength, formattedCount)
    {
        i = 1;
    }
}
";

            var handler = GetInterpolatedStringCustomHandlerType("CustomHandler", "partial struct", useBoolReturns: false);

            var expectedDiagnostics = DiagnosticDescription.None;
            var expectedFlowGraph = @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
        Entering: {R1}
.locals {R1}
{
    Locals: [System.Int32 i]
    CaptureIds: [0] [1] [2]
    Block[B1] - Block
        Predecessors: [B0]
        Statements (6)
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: '$""""')
              Value:
                IObjectCreationOperation (Constructor: CustomHandler..ctor(System.Int32 literalLength, System.Int32 formattedCount)) (OperationKind.ObjectCreation, Type: CustomHandler, IsImplicit) (Syntax: '$""""')
                  Arguments(2):
                      IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: literalLength) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: '$""""')
                        ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 0, IsImplicit) (Syntax: '$""""')
                        InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                      IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: formattedCount) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: '$""""')
                        ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 0, IsImplicit) (Syntax: '$""""')
                        InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  Initializer:
                    null
            IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'i')
              Value:
                ILocalReferenceOperation: i (IsDeclaration: True) (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'i')
            IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: '$""literal{1}""')
              Value:
                IObjectCreationOperation (Constructor: CustomHandler..ctor(System.Int32 literalLength, System.Int32 formattedCount, out System.Int32 i)) (OperationKind.ObjectCreation, Type: CustomHandler, IsImplicit) (Syntax: '$""literal{1}""')
                  Arguments(3):
                      IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: literalLength) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: '$""literal{1}""')
                        ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 7, IsImplicit) (Syntax: '$""literal{1}""')
                        InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                      IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: formattedCount) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: '$""literal{1}""')
                        ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1, IsImplicit) (Syntax: '$""literal{1}""')
                        InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                      IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: i) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 'out int i')
                        IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'i')
                        InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  Initializer:
                    null
            IInvocationOperation ( void CustomHandler.AppendLiteral(System.String literal)) (OperationKind.Invocation, Type: System.Void, IsImplicit) (Syntax: 'literal')
              Instance Receiver:
                IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: CustomHandler, IsImplicit) (Syntax: '$""literal{1}""')
              Arguments(1):
                  IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: literal) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 'literal')
                    ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: ""literal"") (Syntax: 'literal')
                    InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
            IInvocationOperation ( void CustomHandler.AppendFormatted(System.Object o, [System.Int32 alignment = 0], [System.String format = null])) (OperationKind.Invocation, Type: System.Void, IsImplicit) (Syntax: '{1}')
              Instance Receiver:
                IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: CustomHandler, IsImplicit) (Syntax: '$""literal{1}""')
              Arguments(3):
                  IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: o) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: '1')
                    IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Object, IsImplicit) (Syntax: '1')
                      Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        (Boxing)
                      Operand:
                        ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
                    InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  IArgumentOperation (ArgumentKind.DefaultValue, Matching Parameter: alignment) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: '{1}')
                    ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 0, IsImplicit) (Syntax: '{1}')
                    InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  IArgumentOperation (ArgumentKind.DefaultValue, Matching Parameter: format) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: '{1}')
                    IDefaultValueOperation (OperationKind.DefaultValue, Type: System.String, Constant: null, IsImplicit) (Syntax: '{1}')
                    InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'C.M($"""", ou ... teral{1}"");')
              Expression:
                IInvocationOperation (void C.M(CustomHandler c1, out System.Int32 i, CustomHandler c2)) (OperationKind.Invocation, Type: System.Void) (Syntax: 'C.M($"""", ou ... iteral{1}"")')
                  Instance Receiver:
                    null
                  Arguments(3):
                      IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: c1) (OperationKind.Argument, Type: null) (Syntax: '$""""')
                        IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: CustomHandler, IsImplicit) (Syntax: '$""""')
                        InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                      IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: i) (OperationKind.Argument, Type: null) (Syntax: 'out int i')
                        IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'i')
                        InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                      IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: c2) (OperationKind.Argument, Type: null) (Syntax: '$""literal{1}""')
                        IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: CustomHandler, IsImplicit) (Syntax: '$""literal{1}""')
                        InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        Next (Regular) Block[B2]
            Leaving: {R1}
}
Block[B2] - Exit
    Predecessors: [B1]
    Statements (0)
";
            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(new[] { code, InterpolatedStringHandlerArgumentAttribute, handler }, expectedFlowGraph, expectedDiagnostics);
        }

        [Fact]
        public void OutVariableDeclarationAsParameter_04()
        {
            var code = @"
using System;
using System.Runtime.CompilerServices;

public class C
{
    public static void M1(object o1, object o2)
    /*<bind>*/{
        C.M($""literal{1}"", out int i, o1 ?? o2);
    }/*</bind>*/

    public static void M(CustomHandler c1, out int i, object o) 
    {
        i = 0;
    }
}

public partial struct CustomHandler
{
    public CustomHandler(int literalLength, int formattedCount, out int i) : this(literalLength, formattedCount)
    {
        i = 1;
    }
}
";

            var handler = GetInterpolatedStringCustomHandlerType("CustomHandler", "partial struct", useBoolReturns: false);

            var expectedDiagnostics = DiagnosticDescription.None;
            var expectedFlowGraph = @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
        Entering: {R1}
.locals {R1}
{
    Locals: [System.Int32 i]
    CaptureIds: [0] [2]
    Block[B1] - Block
        Predecessors: [B0]
        Statements (3)
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: '$""literal{1}""')
              Value:
                IObjectCreationOperation (Constructor: CustomHandler..ctor(System.Int32 literalLength, System.Int32 formattedCount)) (OperationKind.ObjectCreation, Type: CustomHandler, IsImplicit) (Syntax: '$""literal{1}""')
                  Arguments(2):
                      IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: literalLength) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: '$""literal{1}""')
                        ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 7, IsImplicit) (Syntax: '$""literal{1}""')
                        InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                      IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: formattedCount) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: '$""literal{1}""')
                        ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1, IsImplicit) (Syntax: '$""literal{1}""')
                        InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  Initializer:
                    null
            IInvocationOperation ( void CustomHandler.AppendLiteral(System.String literal)) (OperationKind.Invocation, Type: System.Void, IsImplicit) (Syntax: 'literal')
              Instance Receiver:
                IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: CustomHandler, IsImplicit) (Syntax: '$""literal{1}""')
              Arguments(1):
                  IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: literal) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 'literal')
                    ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: ""literal"") (Syntax: 'literal')
                    InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
            IInvocationOperation ( void CustomHandler.AppendFormatted(System.Object o, [System.Int32 alignment = 0], [System.String format = null])) (OperationKind.Invocation, Type: System.Void, IsImplicit) (Syntax: '{1}')
              Instance Receiver:
                IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: CustomHandler, IsImplicit) (Syntax: '$""literal{1}""')
              Arguments(3):
                  IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: o) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: '1')
                    IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Object, IsImplicit) (Syntax: '1')
                      Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        (Boxing)
                      Operand:
                        ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
                    InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  IArgumentOperation (ArgumentKind.DefaultValue, Matching Parameter: alignment) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: '{1}')
                    ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 0, IsImplicit) (Syntax: '{1}')
                    InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  IArgumentOperation (ArgumentKind.DefaultValue, Matching Parameter: format) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: '{1}')
                    IDefaultValueOperation (OperationKind.DefaultValue, Type: System.String, Constant: null, IsImplicit) (Syntax: '{1}')
                    InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        Next (Regular) Block[B2]
            Entering: {R2}
    .locals {R2}
    {
        CaptureIds: [1]
        Block[B2] - Block
            Predecessors: [B1]
            Statements (1)
                IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'o1')
                  Value:
                    IParameterReferenceOperation: o1 (OperationKind.ParameterReference, Type: System.Object) (Syntax: 'o1')
            Jump if True (Regular) to Block[B4]
                IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsImplicit) (Syntax: 'o1')
                  Operand:
                    IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.Object, IsImplicit) (Syntax: 'o1')
                Leaving: {R2}
            Next (Regular) Block[B3]
        Block[B3] - Block
            Predecessors: [B2]
            Statements (1)
                IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'o1')
                  Value:
                    IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.Object, IsImplicit) (Syntax: 'o1')
            Next (Regular) Block[B5]
                Leaving: {R2}
    }
    Block[B4] - Block
        Predecessors: [B2]
        Statements (1)
            IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'o2')
              Value:
                IParameterReferenceOperation: o2 (OperationKind.ParameterReference, Type: System.Object) (Syntax: 'o2')
        Next (Regular) Block[B5]
    Block[B5] - Block
        Predecessors: [B3] [B4]
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'C.M($""liter ...  o1 ?? o2);')
              Expression:
                IInvocationOperation (void C.M(CustomHandler c1, out System.Int32 i, System.Object o)) (OperationKind.Invocation, Type: System.Void) (Syntax: 'C.M($""liter ... , o1 ?? o2)')
                  Instance Receiver:
                    null
                  Arguments(3):
                      IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: c1) (OperationKind.Argument, Type: null) (Syntax: '$""literal{1}""')
                        IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: CustomHandler, IsImplicit) (Syntax: '$""literal{1}""')
                        InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                      IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: i) (OperationKind.Argument, Type: null) (Syntax: 'out int i')
                        IDeclarationExpressionOperation (OperationKind.DeclarationExpression, Type: System.Int32) (Syntax: 'int i')
                          ILocalReferenceOperation: i (IsDeclaration: True) (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'i')
                        InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                      IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: o) (OperationKind.Argument, Type: null) (Syntax: 'o1 ?? o2')
                        IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: System.Object, IsImplicit) (Syntax: 'o1 ?? o2')
                        InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        Next (Regular) Block[B6]
            Leaving: {R1}
}
Block[B6] - Exit
    Predecessors: [B5]
    Statements (0)
";
            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(new[] { code, InterpolatedStringHandlerArgumentAttribute, handler }, expectedFlowGraph, expectedDiagnostics);
        }
    }
}
